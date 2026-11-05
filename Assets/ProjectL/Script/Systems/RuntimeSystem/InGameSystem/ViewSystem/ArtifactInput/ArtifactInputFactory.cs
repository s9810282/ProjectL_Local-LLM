using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput
{
    /// <summary>로비 아티팩트 팝업이 쓰는 입력 슬롯과 Controller를 조립합니다.</summary>
    internal static class ArtifactInputFactory
    {
        internal static ArtifactInputComposition Create()
        {
            var slot = new ArtifactInputSlot();
            var registration = new InputRegistrationSet(
                new IInputController[] { new ArtifactInputController(slot) },
                new[] { ArtifactInputLayers.Artifact }, ArtifactInputKeyBindings.Definitions);
            return new ArtifactInputComposition(slot, registration);
        }
    }

    internal sealed class ArtifactInputComposition
    {
        internal IArtifactInputSlot Slot { get; }
        internal InputRegistrationSet Registration { get; }

        internal ArtifactInputComposition(IArtifactInputSlot slot, InputRegistrationSet registration)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
