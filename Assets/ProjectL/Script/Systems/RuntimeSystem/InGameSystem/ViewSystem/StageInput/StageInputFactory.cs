using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>배치·전투·카메라 조작 Controller를 동일한 스테이지 슬롯에 연결합니다.</summary>
    internal static class StageInputFactory
    {
        internal static StageInputComposition Create()
        {
            var slot = new StageInputReceiverSlot();
            var registration = new InputRegistrationSet(new IInputController[]
            {
                new DeploymentInputController(slot),
                new CombatInputController(slot),
                new CombatCameraInputController(slot)
            }, StageInputLayers.All, StageInputKeyBindings.Definitions);
            return new StageInputComposition(slot, registration);
        }
    }

    internal sealed class StageInputComposition
    {
        internal IStageInputReceiverSlot Slot { get; }
        internal InputRegistrationSet Registration { get; }

        internal StageInputComposition(IStageInputReceiverSlot slot, InputRegistrationSet registration)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
