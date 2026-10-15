using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    public enum CombatExitStatus { Idle, Draining, Abandoning, Presenting, Ending, PreparingNewRun, Transitioning, Failed }

    /// <summary>
    /// 포기 버튼이 쓰는 좁은 계약입니다(View 포기 UI 설계 P2). View는 정상 이탈 요청을 보지 않고 이 계약만 보관합니다.
    /// </summary>
    public interface ICombatAbandonRequest
    {
        CombatExitStatus ExitStatus { get; }

        /// <summary>
        /// 진행 중 전투를 포기합니다(설계 §6.5 K22). 남은 처리·표시가 끝난 뒤 Core가 패배를 기록하고,
        /// 완료 확인 → 정리 → 새 Run 준비 → Run 종료 목적지로 이동합니다. 받아들일 수 없으면 false입니다.
        /// </summary>
        bool RequestAbandon();
    }

    /// <summary>확정된 정상 승패의 이탈과 진행 중 전투의 자발적 포기를 요청합니다.</summary>
    public interface ICombatExitService : IServiceModule, ICombatAbandonRequest
    {
        bool RequestNormalExit(LifeCycleKey destination);
    }

    /// <summary>F2가 필요로 하는 표시 수명입니다. 새 제출 정지는 기존 결과 재생을 취소하지 않습니다.</summary>
    public interface ICombatExitPresentation
    {
        bool IsAttached { get; }
        bool HasPendingPresentation { get; }
        void SuspendSubmissions();
        void PresentCompletion(BattleCompletionChanges completion, Action completed);
    }

    /// <summary>
    /// 정상 이탈·포기 한 건의 순서만 소유합니다. 승패·재화·진행 정산은 Core에 맡깁니다.
    /// Run이 끝난 결과는 EndBattle 뒤 새 Run 준비 Job이 끝난 다음 전환합니다(설계 §6.4 K21).
    /// 포기는 남은 처리·표시를 끝낸 뒤 Core에 한 번 요청합니다(설계 §6.5 K22).
    /// </summary>
    internal sealed class CombatExitSequence
    {
        private readonly ICombatStateBufferReadSink _state;
        private readonly ICombatResultReadSink _results;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatViewBufferReadSink _input;
        private readonly IJobRunner _runner;
        private readonly ICombatProcessingService _core;
        private readonly IChapterProgressionCoordinator _progression;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<CombatEntryRequest> _entry;
        private readonly Func<ICombatExitPresentation> _getView;
        private readonly Action _stopFlow;
        private readonly Action _registerNewRun;
        private readonly Action<LifeCycleKey> _change;
        private ICombatExitPresentation _view;
        private BattleCompletionChanges _completion;
        private CombatResult _finalResult;
        private int _finalSequence;
        private LifeCycleKey _destination;
        private long _battleInstance;
        private int _generation;
        private bool _abandon;
        private bool _confirmed;
        private bool _authorizingTransition;

        internal CombatExitStatus Status { get; private set; }

        internal CombatExitSequence(ICombatStateBufferReadSink state, ICombatResultReadSink results,
            ICharacterBufferReadSink characters, ICombatViewBufferReadSink input, IJobRunner runner,
            ICombatProcessingService core, IChapterProgressionCoordinator progression, RuntimeFaultBoundary faults,
            Func<CombatEntryRequest> entry, Func<ICombatExitPresentation> getView, Action stopFlow,
            Action registerNewRun, Action<LifeCycleKey> change)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _getView = getView ?? throw new ArgumentNullException(nameof(getView));
            _stopFlow = stopFlow ?? throw new ArgumentNullException(nameof(stopFlow));
            _registerNewRun = registerNewRun ?? throw new ArgumentNullException(nameof(registerNewRun));
            _change = change ?? throw new ArgumentNullException(nameof(change));
        }

        internal bool Request(LifeCycleKey destination)
        {
            ValidateDestination(destination);
            if (_faults.IsFaulted || Status == CombatExitStatus.Failed)
                return false;
            if (Status != CombatExitStatus.Idle)
                return !_abandon && destination == _destination;
            CombatEntryRequest entry = _entry();
            if (entry?.Status != CombatEntryStatus.Active || !_state.HasBattle || _state.Completion == null ||
                _state.Completion.Outcome.Reason == StageOutcomeReason.VoluntaryAbandonment)
                return false;
            ICombatExitPresentation view = _getView();
            if (view == null || !view.IsAttached)
                return false;
            ValidateTarget(_state.Completion, entry);
            return Begin(view, _state.Completion, destination, abandon: false);
        }

        /// <summary>진행 중 전투의 포기를 시작합니다. 같은 포기가 진행 중이면 true, 다른 이탈이 진행 중이면 false입니다.</summary>
        internal bool RequestAbandon(LifeCycleKey destination)
        {
            ValidateDestination(destination);
            if (_faults.IsFaulted || Status == CombatExitStatus.Failed)
                return false;
            if (Status != CombatExitStatus.Idle)
                return _abandon && destination == _destination;
            // 이미 승패가 기록된 전투는 포기가 아니라 정상 이탈 대상이다(포기로 덮어쓰지 않음).
            CombatEntryRequest entry = _entry();
            if (entry?.Status != CombatEntryStatus.Active || !_state.HasBattle || _state.Completion != null)
                return false;
            ICombatExitPresentation view = _getView();
            if (view == null || !view.IsAttached)
                return false;
            return Begin(view, null, destination, abandon: true);
        }

        private bool Begin(ICombatExitPresentation view, BattleCompletionChanges completion, LifeCycleKey destination,
            bool abandon)
        {
            _view = view;
            _completion = completion;
            _battleInstance = _state.BattleInstanceId;
            _destination = destination;
            _abandon = abandon;
            Status = CombatExitStatus.Draining;
            try
            {
                _stopFlow();
                _view.SuspendSubmissions();
                return !_faults.IsFaulted;
            }
            catch (Exception error)
            {
                Fail(error);
                return false;
            }
        }

        internal void Tick()
        {
            if (_faults.IsFaulted || Status == CombatExitStatus.Idle || Status == CombatExitStatus.Failed ||
                Status == CombatExitStatus.Transitioning)
                return;
            try
            {
                if (Status == CombatExitStatus.PreparingNewRun)
                {
                    // 새 Run 준비 Job이 끝난 뒤에만 전환한다.
                    if (!_runner.IsIdle)
                        return;
                    if (_progression.Outcome != RunOutcome.InProgress || _state.HasBattle)
                        throw new InvalidOperationException("Combat 이탈: 새 Run 준비 뒤 Run 상태가 시작 상태가 아닙니다.");
                    Transition();
                    return;
                }

                // 포기 대기 중에는 남은 처리가 승패를 먼저 기록할 수 있다. 그 판단은 Core 포기 결과로 한다.
                bool awaitingAbandon = _abandon && _completion == null;
                if (!_view.IsAttached || _state.BattleInstanceId != _battleInstance ||
                    (!awaitingAbandon && !ReferenceEquals(_completion, _state.Completion)))
                    throw new InvalidOperationException("Combat 이탈: 대기 중 표시 수명 또는 전투가 바뀌었습니다.");
                if (!_runner.IsIdle || _view.HasPendingPresentation ||
                    _input.PresentedResultSequence < _state.LatestResultSequence)
                    return;
                if (!_state.HasBattle)
                    throw new InvalidOperationException("Combat 이탈: 표시 완료 전에 전투가 정리됐습니다.");

                if (Status == CombatExitStatus.Draining)
                {
                    if (awaitingAbandon)
                        Abandon();
                    else
                        ConfirmFinalResult();
                    if (_faults.IsFaulted)
                        return;
                    Status = CombatExitStatus.Presenting;
                    int generation = _generation;
                    _view.PresentCompletion(_completion, () =>
                    {
                        if (!_faults.IsFaulted && generation == _generation && Status == CombatExitStatus.Presenting)
                            _confirmed = true;
                    });
                    // 동기 완료 뒤 표시 코드가 실패해도 같은 호출 스택에서 EndBattle을 실행하지 않는다.
                    return;
                }
                if (Status != CombatExitStatus.Presenting || !_confirmed)
                    return;
                Status = CombatExitStatus.Ending;
                _core.EndBattle();
                if (_faults.IsFaulted)
                    return;
                bool hasRetained = _results.TryGetLatest(out CombatResult retained);
                if (_state.HasBattle || _characters.Characters.Count != 0 || !_runner.IsIdle ||
                    !ReferenceEquals(_state.Completion, _completion) ||
                    !ReferenceEquals(hasRetained ? retained : null, _finalResult) ||
                    _state.LatestResultSequence != _finalSequence || _input.PresentedResultSequence < _finalSequence)
                    throw new InvalidOperationException("Combat 이탈: EndBattle 뒤 정리·결과 보존 조건이 맞지 않습니다.");

                if (EndsRun())
                {
                    Status = CombatExitStatus.PreparingNewRun;
                    _registerNewRun();
                    return;
                }
                Transition();
            }
            catch (Exception error) { Fail(error); }
        }

        /// <summary>정상 이탈: 최종 일반 결과가 Completion의 승패와 같은지 확인합니다.</summary>
        private void ConfirmFinalResult()
        {
            if (!_results.TryGetLatest(out _finalResult) || !_finalResult.Accepted ||
                _finalResult.Sequence != _state.LatestResultSequence || !_finalResult.Outcome.HasValue ||
                !_finalResult.Outcome.Value.Equals(_completion.Outcome))
                throw new InvalidOperationException("Combat 이탈: 확인한 최종 결과와 Completion이 다릅니다.");
            _finalSequence = _finalResult.Sequence;
        }

        /// <summary>
        /// 포기: 남은 처리·표시가 끝난 뒤 Core에 한 번 요청합니다(설계 §6.5 K22).
        /// 포기는 일반 결과를 게시하지 않으므로 직전 결과·순번이 그대로인지 확인합니다.
        /// </summary>
        private void Abandon()
        {
            Status = CombatExitStatus.Abandoning;
            bool hadResult = _results.TryGetLatest(out CombatResult before);
            int sequence = _state.LatestResultSequence;
            BattleAbandonResult result = _core.AbandonBattle();
            if (_faults.IsFaulted)
                return;

            switch (result)
            {
                case BattleAbandonResult.Abandoned:
                    var completion = _state.Completion;
                    bool hasAfter = _results.TryGetLatest(out CombatResult after);
                    if (completion == null || completion.Outcome.Outcome != StageOutcome.Defeat ||
                        completion.Outcome.Reason != StageOutcomeReason.VoluntaryAbandonment ||
                        _state.LatestResultSequence != sequence ||
                        !ReferenceEquals(hasAfter ? after : null, hadResult ? before : null))
                        throw new InvalidOperationException("Combat 포기: 기록된 포기 Completion 또는 결과 보존이 맞지 않습니다.");
                    _completion = completion;
                    _finalResult = hadResult ? before : null;
                    _finalSequence = sequence;
                    break;
                case BattleAbandonResult.AlreadyCompleted:
                    // 대기 중 마지막 처리가 실제 승패를 기록했다. 그 결과로 정상 이탈한다.
                    _completion = _state.Completion ?? throw new InvalidOperationException(
                        "Combat 포기: AlreadyCompleted인데 Completion이 없습니다.");
                    ConfirmFinalResult();
                    break;
                default:
                    throw new InvalidOperationException($"Combat 포기: 활성 전투에서 받을 수 없는 결과입니다({result}).");
            }
            ValidateTarget(_completion, _entry());
        }

        /// <summary>
        /// EndBattle 뒤 Core 값으로 Run 종료 여부를 정합니다(설계 §6.4 K21).
        /// 보스 승리는 챕터 완료를 한 번 요청하고, 그 밖의 결과는 Core의 Run 결과를 읽습니다.
        /// </summary>
        private bool EndsRun()
        {
            var progress = _completion.Progression;
            if (_completion.Outcome.Outcome == StageOutcome.Victory && progress.ClearMap)
            {
                ChapterContinuationResult chapter = _progression.CompleteChapter(progress.ChapterKey, progress.NodeId);
                if (chapter.Accepted)
                    return chapter.Kind == ChapterContinuationKind.RunCompleted;
                // 임시(K21): 연결 정의가 없으면 이어갈 챕터가 없으므로 Run을 끝낸다.
                if (chapter.FailureReason == ChapterContinuationFailureReason.DefinitionUnavailable)
                    return true;
                throw new InvalidOperationException($"Combat 이탈: 챕터 완료가 거절됐습니다({chapter.FailureReason}).");
            }
            return _progression.Outcome != RunOutcome.InProgress;
        }

        private static void ValidateDestination(LifeCycleKey destination)
        {
            if (destination != LifeCycleKeys.Stage && destination != LifeCycleKeys.Lobby && destination != LifeCycleKeys.Artifact)
                throw new ArgumentException("등록된 비전투 목적지가 필요합니다.", nameof(destination));
        }

        private static void ValidateTarget(BattleCompletionChanges completion, CombatEntryRequest entry)
        {
            var progress = completion.Progression;
            if (entry == null || progress.ChapterKey != entry.ChapterKey || progress.NodeId != entry.NodeId ||
                progress.StageKey != entry.Result.StageKey)
                throw new InvalidOperationException("Combat 이탈: 현재 진입과 Completion의 대상이 다릅니다.");
        }

        private void Transition()
        {
            Status = CombatExitStatus.Transitioning;
            _authorizingTransition = true;
            try { _change(_destination); }
            finally { _authorizingTransition = false; }
        }

        internal void ValidateTransition(LifeCycleKey? destination)
        {
            if (_faults.IsFaulted || !_authorizingTransition || !destination.HasValue || destination.Value != _destination)
                throw new InvalidOperationException("Combat 이탈은 CombatExit 서비스의 이탈·포기 경계를 거쳐야 합니다.");
        }

        internal void Reset()
        {
            _generation++;
            _view = null;
            _completion = null;
            _finalResult = null;
            _finalSequence = 0;
            _abandon = false;
            _confirmed = false;
            _authorizingTransition = false;
            Status = CombatExitStatus.Idle;
        }

        private void Fail(Exception error)
        {
            Status = CombatExitStatus.Failed;
            _faults.Report(error);
        }
    }
}
