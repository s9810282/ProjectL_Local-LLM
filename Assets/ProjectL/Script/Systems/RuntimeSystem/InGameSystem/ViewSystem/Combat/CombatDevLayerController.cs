using System;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// RecordInputOnly 개발 Layer 패널입니다(설계 §8.1, W20, SOLID·SSOT 개선 설계 D3).
    /// 버튼으로 실제 입력 Layer만 배치/전투로 바꿉니다. Core 국면은 바꾸지 않습니다.
    /// </summary>
    /// <remarks>
    /// 상태를 갖지 않습니다. 허용 여부는 <see cref="CombatInputPolicy.CanChangeDevLayer"/>가 정합니다.
    /// Live에서는 패널을 끄고 연결하지 않습니다. 버튼 콜백은 Unity EventSystem에서 오므로 예외를 오류 경계로 보고합니다.
    /// </remarks>
    internal sealed class CombatDevLayerController
    {
        private readonly CombatExecutionMode _executionMode;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<CombatInputFacts> _readFacts;
        // 오류 아님·진입 활성·입력 연결·Interaction 있음을 조정자가 판단해 돌려준다.
        private readonly Func<bool> _canReceive;
        // Layer를 바꾸기 전에 Drag·조준·미리보기 표시를 정리한다.
        private readonly Action _cancelPresentation;
        // 버튼 연결·해제에 같은 대리자를 쓰도록 한 번만 만든다.
        private readonly Action _onDeployment;
        private readonly Action _onCombat;
        private ICombatDevLayerDisplay _display;
        private IInputLayerService _layers;

        internal CombatDevLayerController(
            CombatExecutionMode executionMode,
            RuntimeFaultBoundary faults,
            Func<CombatInputFacts> readFacts,
            Func<bool> canReceive,
            Action cancelPresentation)
        {
            _executionMode = executionMode;
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _canReceive = canReceive ?? throw new ArgumentNullException(nameof(canReceive));
            _cancelPresentation = cancelPresentation ?? throw new ArgumentNullException(nameof(cancelPresentation));
            _onDeployment = () => Receive(StagePhase.Deployment);
            _onCombat = () => Receive(StagePhase.Combat);
        }

        /// <summary>씬 연결 때 RecordInputOnly면 패널을 켜고 연결하고, Live면 끕니다.</summary>
        internal void Attach(ICombatDevLayerDisplay display, IInputLayerService layers)
        {
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            if (_executionMode == CombatExecutionMode.RecordInputOnly)
                display.ShowDevLayerPanel(_onDeployment, _onCombat);
            else
                display.HideDevLayerPanel();
        }

        /// <summary>오류 정지·입력 정리: 패널을 끄고 연결을 끊습니다. 연결돼 있지 않으면 아무것도 하지 않습니다.</summary>
        internal void Hide()
        {
            _display?.HideDevLayerPanel();
        }

        /// <summary>씬 연결 해제: 참조만 놓습니다. 버튼 연결은 표시의 Release가 끊습니다.</summary>
        internal void Detach()
        {
            _display = null;
            _layers = null;
        }

        /// <summary>
        /// 패널 버튼의 입구입니다. UI EventSystem으로 들어오므로 입력 Layer와 무관하게 눌리며,
        /// 수명·대기·오류·종료·후처리 검사(Policy)를 통과할 때만 표시를 정리하고 Layer를 바꿉니다.
        /// </summary>
        private void Receive(StagePhase stage)
        {
            if (_layers == null || !_canReceive())
                return;

            try
            {
                CombatInputFacts facts = _readFacts();
                if (!CombatInputPolicy.CanChangeDevLayer(facts, facts.HasPendingCommand))
                    return;

                InputLayer target = CombatInputPolicy.LayerOf(stage);
                if (_layers.CurrentLayer == target)
                    return;

                _cancelPresentation();
                _layers.ChangeLayer(target);
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }
    }
}
