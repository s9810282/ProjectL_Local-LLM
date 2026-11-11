using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    /// <summary>
    /// 다른 UI(Option)가 확인한 포기 요청의 입구입니다(포기 UI 설계 C2·C4, C 단계 설계 C-1·C-2).
    /// 지금 받을 수 있는지 판단하고, 받으면 진행 사실(<see cref="CombatViewProgress"/>)에 요청을 세웁니다.
    /// 요청을 세우면 진행 사실이 새 입력을 닫고, 입력 중 표시 정리는 그 호스트(조정자)가 합니다(View 단계 설계 W2).
    /// </summary>
    /// <remarks>
    /// 상태를 갖지 않습니다. 사실 조건은 <see cref="CombatInputPolicy.CanRequestAbandon"/>, "이탈 전" 조건과 요청 깃발은
    /// <see cref="CombatViewProgress"/> 한 곳입니다. 남은 처리 뒤 Core에 포기를 요청하는 일은 전투 Plan이 그 깃발을 읽고 정합니다(재구성 설계 J8).
    /// UI 입구라 Runtime Tick 밖에서 오므로 예외를 오류 경계로 보고합니다.
    /// </remarks>
    internal sealed class CombatAbandonController
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIAbandonRequest = new("Combat.Action.UI.Abandon.Request");
#endif

        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<CombatInputFacts> _readFacts;
        // 오류 아님·진입 활성·씬 연결을 조정자가 판단해 돌려준다.
        private readonly Func<bool> _canAct;
        private readonly CombatViewProgress _progress;

        internal CombatAbandonController(
            RuntimeFaultBoundary faults,
            Func<CombatInputFacts> readFacts,
            Func<bool> canAct,
            CombatViewProgress progress)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _canAct = canAct ?? throw new ArgumentNullException(nameof(canAct));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        /// <summary>
        /// Option 등 외부 UI에서 확인된 포기 요청입니다. 받을 수 없으면 아무것도 바꾸지 않고 false입니다.
        /// 요청을 세운 뒤 입력 정리(호스트)가 예외를 내면 요청은 선 채 오류 경계에 보고하고 false입니다(곧 F1로 멈춤).
        /// </summary>
        internal bool TryRequestAbandon()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIAbandonRequestSample = _profileUIAbandonRequest.Auto();
#endif

            if (!_canAct() || !CombatInputPolicy.CanRequestAbandon(_readFacts(), _progress.CanMarkAbandon))
                return false;

            try
            {
                _progress.MarkAbandonRequested();
                return true;
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
                return false;
            }
        }
    }
}
