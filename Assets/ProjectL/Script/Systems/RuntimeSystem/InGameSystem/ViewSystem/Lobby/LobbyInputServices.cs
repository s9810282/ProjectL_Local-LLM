using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>로비 입력(시설 고르기·호버·말 이동·수신 슬롯·Layer)이 쓰는 값의 묶음입니다(로비 리팩토링 2차 설계 N-01).</summary>
    internal sealed class LobbyInputServices
    {
        internal LobbyInputServices(IInputLayerService layers, IDelayService delay, LobbyMotionData motion,
            LobbyFacilityHoverData facilityHover, IAudioSystemService audio, ILobbyReceiverSlot slot)
        {
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
            Delay = delay ?? throw new ArgumentNullException(nameof(delay));
            Motion = motion ?? throw new ArgumentNullException(nameof(motion));
            FacilityHover = facilityHover ?? throw new ArgumentNullException(nameof(facilityHover));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
        }

        internal IInputLayerService Layers { get; }
        internal IDelayService Delay { get; }
        internal LobbyMotionData Motion { get; }
        internal LobbyFacilityHoverData FacilityHover { get; }
        internal IAudioSystemService Audio { get; }
        internal ILobbyReceiverSlot Slot { get; }
    }
}
