using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 전투 진행 판단에 쓰는 사실을 읽는 유일한 자리입니다(재구성 설계 J13).
    /// Plan·Job·진입 서비스는 Buffer를 직접 받지 않고 이것으로 읽습니다. 같은 값을 Core 서비스로 다시 노출하지 않습니다(설계 U8).
    /// </summary>
    public interface ICombatProgressReader : IServiceModule
    {
        /// <summary>지금 사실을 한 번 읽습니다. 부를 때마다 새로 읽습니다.</summary>
        CombatProgressFacts Read();

        /// <summary>현재 선택(챕터·노드·스테이지)을 읽습니다. 선택이 없으면 false입니다. Combat 활성화 확인에 씁니다(설계 J6).</summary>
        bool TryReadSelection(out int chapterKey, out StageSelectionData stage);
    }

    /// <summary>필요한 읽기 싱크와 View 계약만 받고, 쓰거나 저장하지 않습니다.</summary>
    internal sealed class CombatProgressReader : ICombatProgressReader, ISystemPostBindService
    {
        private ICombatStateBufferReadSink _state;
        private ICombatResultReadSink _results;
        private ICombatViewBufferReadSink _input;
        private ICurrentProgressBufferReadSink _current;
        private IRunProgressBufferReadSink _run;
        private ICombatProgressView _view;

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            // IBufferQuery는 모든 싱크를 노출하므로 여기서 필요한 것만 꺼내 보관한다.
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            // 전투 View가 없는 조립(View 그룹 없음)도 바인딩은 통과하고, 실제로 읽을 때 확인한다.
            ICombatProgressView view = resolver.GetService(SystemGroupsService.InGame).ViewGroup?.Combat?.Progress;
            Bind(buffers.CombatStateReadSink, buffers.CombatResultReadSink, buffers.CombatViewReadSink,
                buffers.CurrentProgressReadSink, buffers.RunProgressReadSink, view);
        }

        /// <summary>필요한 읽기 싱크와 View를 보관합니다(테스트 입구이기도 합니다). View는 없을 수 있습니다.</summary>
        internal void Bind(
            ICombatStateBufferReadSink state,
            ICombatResultReadSink results,
            ICombatViewBufferReadSink input,
            ICurrentProgressBufferReadSink current,
            IRunProgressBufferReadSink run,
            ICombatProgressView view)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _view = view;
        }

        public CombatProgressFacts Read()
        {
            RequireBound();
            if (_view == null)
                throw new InvalidOperationException("[CombatProgressReader] 전투 View가 조립되지 않았습니다.");

            int handled = _state.HandledCommandNumber;
            _results.TryGetLatest(out CombatResult latest);
            return new CombatProgressFacts(
                hasBattle: _state.HasBattle,
                battleInstanceId: _state.BattleInstanceId,
                phase: _state.Phase,
                handledCommandNumber: handled,
                answeredPreviewNumber: _state.AnsweredPreviewNumber,
                latestResultSequence: _state.LatestResultSequence,
                completion: _state.Completion,
                latestResult: latest,
                nextCommandNumber: _input.TryGetNextCommand(handled, out CombatViewCommand command) ? command.Number : 0,
                previewNumber: _input.HasPreview ? _input.Preview.Number : 0,
                presentedResultSequence: _input.PresentedResultSequence,
                isViewAttached: _view.IsAttached,
                isSubmissionSuspended: _view.IsSubmissionSuspended,
                completionDisplay: _view.CompletionDisplay,
                isAbandonRequested: _view.IsAbandonRequested,
                runOutcome: _run.Outcome,
                isEntryPresentationPending: _view.IsEntryPresentationPending);
        }

        public bool TryReadSelection(out int chapterKey, out StageSelectionData stage)
        {
            RequireBound();
            chapterKey = _current.CurrentMapKey;
            stage = _current.HasSelection ? _current.CurrentStage : default;
            return _current.HasSelection;
        }

        private void RequireBound()
        {
            if (_state == null)
                throw new InvalidOperationException("[CombatProgressReader] 서비스를 받기 전에 읽을 수 없습니다.");
        }
    }
}
