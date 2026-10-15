using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 전투 진행의 유일한 주인입니다. 매 Tick Buffer를 읽고, Runner가 비어 있을 때만 다음 작업 하나를 등록합니다.
    /// 규칙·표시·연출과 Buffer 쓰기는 소유하지 않습니다.
    /// 자신의 상태는 "지금 기다리는 처리 하나"와 정지 여부뿐이며, 국면·번호는 기록하지 않고 매번 읽습니다.
    /// </summary>
    /// <remarks>
    /// Automatic 국면의 자동 진행은 F3 결과 읽기로 수락 여부를 확인합니다(설계 §4.3·K8).
    /// 처리 입력은 실행 시점까지 유지된다고 보고, Job의 처리 여부를 따로 받지 않습니다(설계 K14).
    /// 실행 모드와 공유 오류 경계는 조립 때 받아 고정합니다. 분기 순서의 단일 기준은 골격 설계 §4.1입니다
    /// (0행: 공유 오류면 아무것도 하지 않음, 4a행: RecordInputOnly면 준비 확인 뒤 처리 Job을 등록하지 않음).
    /// </remarks>
    internal sealed class CombatFlow
    {
        /// <summary>등록한 뒤 Runner가 비었을 때 확인할 처리입니다.</summary>
        private enum PendingCheck
        {
            None = 0,
            Preparation = 1,
            Command = 2,
            Preview = 3,
            Advance = 4
        }

        private readonly ICombatStateBufferReadSink _state;
        private readonly ICombatResultReadSink _results;
        private readonly ICombatViewBufferReadSink _input;
        private readonly IJobRunner _runner;
        private readonly IJobRegisterSystem _register;
        private readonly CombatExecutionMode _executionMode;
        private readonly RuntimeFaultBoundary _faults;

        private PendingCheck _pending;

        /// <summary>명령·미리보기는 기대 번호, 자동 진행은 등록 때의 결과 순번 S입니다.</summary>
        private int _expectedNumber;
        private bool _isStopped;

        internal CombatFlow(
            ICombatStateBufferReadSink state,
            ICombatResultReadSink results,
            ICombatViewBufferReadSink input,
            IJobRunner runner,
            IJobRegisterSystem register,
            CombatExecutionMode executionMode,
            RuntimeFaultBoundary faultBoundary)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _register = register ?? throw new ArgumentNullException(nameof(register));
            _faults = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));

            if (!System.Enum.IsDefined(typeof(CombatExecutionMode), executionMode))
                throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null);
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            // 기록 모드는 개발 진단 전용이다. 비개발 빌드에서는 조립 단계에서 명시적으로 거부한다.
            if (executionMode == CombatExecutionMode.RecordInputOnly)
                throw new InvalidOperationException("CombatFlow: RecordInputOnly는 에디터·개발 빌드에서만 조립할 수 있습니다.");
#endif
            _executionMode = executionMode;
        }

        /// <summary>정상 Stop 또는 실패 감지로 새 등록을 멈춘 상태입니다.</summary>
        internal bool IsStopped => _isStopped;
        internal bool HasPendingCheck => _pending != PendingCheck.None;

        /// <summary>이번 진입 요청이 준비한 전투만 활성화합니다. 준비 Job을 다시 등록하지 않습니다.</summary>
        internal void ActivatePreparedBattle(CombatEntryRequest entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (_faults.IsFaulted || entry.Status != CombatEntryStatus.Activating ||
                !entry.IsJobCompleted || entry.Result == null || !entry.Result.Accepted ||
                !_runner.IsIdle || !_state.HasBattle || HasPendingCheck)
                throw new InvalidOperationException("CombatFlow: 수락된 진입과 Job 완료를 확인한 뒤 활성화해야 합니다.");
            Reset();
        }

        /// <summary>새 전투를 위해 확인 조건과 정지를 초기화합니다.</summary>
        internal void Reset()
        {
            ClearPending();
            _isStopped = false;
        }

        /// <summary>
        /// 새 등록만 멈춥니다. 이미 등록된 Job을 취소하거나 Buffer를 비우지 않습니다.
        /// 정상 이탈에도 쓰므로 오류를 기록하지 않으며, 반복 호출해도 안전합니다.
        /// </summary>
        internal void Stop()
        {
            _isStopped = true;
        }

        /// <summary>
        /// 확정 선택을 사용하는 호환 준비 경로입니다. 새 LifeCycle 진입은 ActivatePreparedBattle을 사용합니다.
        /// 전투 준비를 한 번 등록하고 그 결과(HasBattle)를 기다립니다.
        /// 런타임 오류가 이미 보고됐으면 등록·로그 없이 반환합니다(§4.1 0행). 기록 모드에서도 실제 준비는 등록합니다.
        /// 멈춘 상태이거나, Runner가 일하는 중이거나, 이전 전투가 남았거나, 기다리는 처리가 있으면
        /// 등록하지 않고 예외를 던집니다. 멈춤은 <see cref="Reset"/>으로만 풀립니다.
        /// </summary>
        internal void BeginPreparation()
        {
            if (_faults.IsFaulted)
                return;

            ValidateEntryRegistration();
            RegisterAndExpect(JobType.Start, PendingCheck.Preparation, 0);
        }

        /// <summary>새 진입과 호환 준비에 공통으로 적용하는 등록 전 조건입니다.</summary>
        internal void ValidateEntryRegistration()
        {
            if (_isStopped)
                throw new InvalidOperationException("CombatFlow: 멈춘 상태라 전투 준비를 등록할 수 없습니다. Reset이 먼저 필요합니다.");
            if (!_runner.IsIdle)
                throw new InvalidOperationException("CombatFlow: Runner가 일하는 중이라 전투 준비를 등록할 수 없습니다.");
            if (_state.HasBattle)
                throw new InvalidOperationException("CombatFlow: 이전 전투가 남아 있어 전투 준비를 등록할 수 없습니다.");
            if (_pending != PendingCheck.None)
                throw new InvalidOperationException(
                    $"CombatFlow: 확인을 기다리는 처리({_pending})가 있어 전투 준비를 등록할 수 없습니다.");

        }

        /// <summary>진행 판단 한 번입니다. 한 Tick에 최대 하나만 등록합니다(설계 §4.1).</summary>
        internal void Tick()
        {
            // 0행: 같은 Tick 앞쪽(예: View 입력 콜백)에서 런타임 오류가 보고됐으면 확인·로그·등록을 모두 하지 않는다.
            if (_faults.IsFaulted)
                return;

            if (_isStopped)
                return;

            if (!_runner.IsIdle)
                return;

            if (_pending != PendingCheck.None && !TryConfirmPending())
                return;

            if (!_state.HasBattle)
                return;

            // 4a행: 기록 모드는 준비 확인까지만 실제로 하고, 이후 처리 Job은 등록하지 않는다(입력은 Buffer에 쌓인다).
            if (_executionMode == CombatExecutionMode.RecordInputOnly)
                return;

            if (_state.Phase == CombatFlowPhase.PlayerInput)
            {
                // 연출이 끝났으면 확정 명령을 미리보기보다 먼저 처리해 명령이 밀리지 않게 한다.
                if (IsPresentationDone()
                    && _input.TryGetNextCommand(_state.HandledCommandNumber, out CombatViewCommand command))
                {
                    RegisterAndExpect(JobType.ProcessCommand, PendingCheck.Command, command.Number);
                    return;
                }

                // 미리보기는 상태를 바꾸지 않으므로 연출 중에도 받는다.
                if (_input.HasPreview && _input.Preview.Number > _state.AnsweredPreviewNumber)
                {
                    RegisterAndExpect(JobType.ProcessPreview, PendingCheck.Preview, _input.Preview.Number);
                    return;
                }
            }

            // 연출이 남았으면 상태를 바꾸는 처리를 등록하지 않는다. 최신 결과 슬롯이 확인 전에 덮이지 않게 한다.
            if (!IsPresentationDone())
                return;

            if (_state.Phase == CombatFlowPhase.Automatic)
            {
                RegisterAndExpect(JobType.AdvanceCombat, PendingCheck.Advance, _state.LatestResultSequence);
                return;
            }

            // PlayerInput 대기, Finished, None은 등록하지 않는다.
        }

        private bool IsPresentationDone()
        {
            return _input.PresentedResultSequence >= _state.LatestResultSequence;
        }

        private void RegisterAndExpect(JobType jobType, PendingCheck check, int expectedNumber)
        {
            // 등록이 실패하면 기다릴 처리도 없으므로 등록이 끝난 뒤에 기록한다.
            _register.Register(JobRootType.Combat, jobType);
            _pending = check;
            _expectedNumber = expectedNumber;
        }

        /// <summary>
        /// 정상 반환한 처리가 기대한 상태·번호를 남겼는지 확인합니다(되묻지 않기, 설계 §4.3).
        /// 남기지 않았으면 원인을 한 번 기록하고 멈춥니다.
        /// </summary>
        private bool TryConfirmPending()
        {
            string failure = _pending switch
            {
                PendingCheck.Preparation => _state.HasBattle
                    ? null
                    : "전투 준비가 정상 반환했지만 전투가 설치되지 않았습니다.",
                PendingCheck.Command => _state.HandledCommandNumber >= _expectedNumber
                    ? null
                    : $"명령 {_expectedNumber}의 처리가 기록되지 않았습니다(처리 번호 {_state.HandledCommandNumber}).",
                PendingCheck.Preview => IsPreviewSettled()
                    ? null
                    : $"미리보기 요청 {_expectedNumber}에 대한 응답이 기록되지 않았습니다(응답 번호 {_state.AnsweredPreviewNumber}).",
                PendingCheck.Advance => FindAdvanceFailure(),
                _ => throw new InvalidOperationException($"CombatFlow: 알 수 없는 확인 조건입니다: {_pending}")
            };

            if (failure != null)
            {
                Fail(failure);
                return false;
            }

            ClearPending();
            return true;
        }

        /// <summary>
        /// 자동 진행은 순번 증가만으로 성공을 판단하지 않습니다(설계 K8).
        /// S 이후의 최신 결과가 자동 진행의 수락일 때만 null을 돌려줍니다. 거절도 다시 등록하지 않고 멈춥니다.
        /// </summary>
        private string FindAdvanceFailure()
        {
            int latest = _state.LatestResultSequence;
            if (latest <= _expectedNumber)
                return $"자동 진행의 결과 순번이 오르지 않았습니다(등록 때 {_expectedNumber}, 현재 {latest}).";

            if (!_results.TryGetLatest(out CombatResult result))
                return $"자동 진행의 결과가 기록되지 않았습니다(결과 순번 {latest}).";

            if (result.Sequence != latest)
                return $"자동 진행의 결과 순번이 상태 순번과 다릅니다(결과 {result.Sequence}, 상태 {latest}).";

            if (result.Kind != CombatProcessingKind.Advance)
                return $"결과 {result.Sequence}는 자동 진행이 아닌 {result.Kind} 처리의 결과입니다.";

            if (!result.Accepted)
                return $"자동 진행이 거절됐습니다(결과 {result.Sequence}, 사유 {result.Reason}).";

            return null;
        }

        private bool IsPreviewSettled()
        {
            if (_state.AnsweredPreviewNumber >= _expectedNumber)
                return true;

            // View가 요청을 비우거나 바꿨으면 이 요청의 응답을 더 기다리지 않는다.
            return !_input.HasPreview || _input.Preview.Number != _expectedNumber;
        }

        private void Fail(string reason)
        {
            ClearPending();
            _isStopped = true;
            UnityEngine.Debug.LogError($"[CombatFlow] {reason} 전투 진행을 멈춥니다.");
        }

        private void ClearPending()
        {
            _pending = PendingCheck.None;
            _expectedNumber = 0;
        }
    }
}
