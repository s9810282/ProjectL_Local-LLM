using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput
{
    /// <summary>로비 시설 입력과 덱 팝업 Layer를 조립합니다.</summary>
    internal static class LobbyInputFactory
    {
        internal static LobbyInputComposition Create()
        {
            var slot = new LobbyReceiverSlot();
            var registration = new InputRegistrationSet(
                new IInputController[] { new LobbyInputController(slot) },
                new[] { LobbyInputLayers.Lobby, LobbyInputLayers.DeckPopup });
            return new LobbyInputComposition(slot, registration);
        }
    }

    internal sealed class LobbyInputComposition
    {
        internal ILobbyReceiverSlot Slot { get; }
        internal InputRegistrationSet Registration { get; }

        internal LobbyInputComposition(ILobbyReceiverSlot slot, InputRegistrationSet registration)
        {
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Registration = registration ?? throw new ArgumentNullException(nameof(registration));
        }
    }
}
