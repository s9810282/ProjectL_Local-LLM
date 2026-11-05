using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput
{
    /// <summary>맵 선택 입력과 맵 공통 모달 Layer를 조립합니다.</summary>
    internal static class StageSelectionInputFactory
    {
        internal static StageSelectionInputComposition Create()
        {
            var slot = new StageSelectionReceiverSlot();
            var registration = new InputRegistrationSet(
                new IInputController[] { new StageSelectionInputController(slot) },
                new[] { StageSelectionInputLayers.Selection, StageSelectionInputLayers.Popup });
            return new StageSelectionInputComposition(slot, registration);
        }
    }

    internal sealed class StageSelectionInputComposition
    {
        internal IStageSelectionReceiverSlot Slot { get; }
        internal InputRegistrationSet Registration { get; }

        internal StageSelectionInputComposition(IStageSelectionReceiverSlot slot, InputRegistrationSet registration)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
