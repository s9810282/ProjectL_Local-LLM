using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput
{
    /// <summary>튜토리얼 입력 Controller와 Layer를 조립합니다. 결과는 View 그룹 Factory가 다른 입력과 함께 등록합니다(설계 §15.4.3 조립).</summary>
    internal static class TutorialInputFactory
    {
        internal static TutorialInputComposition Create()
        {
            var slot = new TutorialInputReceiverSlot();
            var registration = new InputRegistrationSet(
                new IInputController[] { new TutorialInputController(slot) },
                new[] { TutorialInputLayers.Tutorial });
            return new TutorialInputComposition(slot, registration);
        }
    }

    internal sealed class TutorialInputComposition
    {
        internal ITutorialInputReceiverSlot Slot { get; }
        internal InputRegistrationSet Registration { get; }

        internal TutorialInputComposition(ITutorialInputReceiverSlot slot, InputRegistrationSet registration)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
