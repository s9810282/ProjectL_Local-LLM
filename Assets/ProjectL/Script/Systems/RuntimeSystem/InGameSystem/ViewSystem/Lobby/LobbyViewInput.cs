using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>연결 한 번의 로비 입력 부품입니다. 시설 고르기·호버 강조·말 이동·수신 슬롯·로비 Layer를 갖습니다.</summary>
    internal sealed class LobbyViewInput
    {
        private readonly LobbyInputServices _services;
        private readonly RuntimeFaultBoundary _faults;
        // 수신 슬롯에 거는 것은 입력 콜백 예외를 오류 경계로 보내는 감싸개입니다(로비 리팩토링 2차 설계 M-01).
        private FaultReportingLobbyReceiver _receiver;
        // 이 연결의 Layer 창구입니다. 새 연결로 교체된 연결의 Layer 변경을 막는 규칙은 팝업 세션과 같은 창구 하나가 맡습니다(로비 리팩토링 2차 설계 N-52).
        private readonly ViewAttachmentInputLayers _layers;
        private LobbyInteraction _interaction;

        internal LobbyViewInput(LobbyInputServices services, Func<bool> isCurrent, RuntimeFaultBoundary faults)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _layers = new ViewAttachmentInputLayers(_services.Layers, isCurrent ?? throw new ArgumentNullException(nameof(isCurrent)));
        }

        /// <summary>지금 입력 Layer가 로비 Layer인지입니다. 로비 입력·Scope가 같은 판정을 이것 하나로 읽습니다.</summary>
        internal bool IsLobbyLayer => _services.Layers.CurrentLayer.Equals(LobbyInputLayers.Lobby);

        /// <summary>표시된 시설을 고르고 말을 옮기는 입력을 만듭니다. 수신 슬롯 연결은 <see cref="Connect"/>에서 합니다.</summary>
        /// <param name="targets">씬 표시가 넘긴 말·판·시설·출력 카메라입니다(로비 리팩토링 2차 설계 N-73).</param>
        /// <param name="isBlocked">시설 고르기·도착 실행을 막는지 묻는 Scope 판정입니다.</param>
        internal void Create(LobbyInputTargets targets, ILobbyFacilityActions actions, Func<bool> isBlocked)
        {
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            // 도착 칸까지 한 칸씩 뜁니다(로비 설계 §15). 위치는 월드 좌표입니다.
            var motion = new ChessPieceMotion(_services.Delay, targets.MovementRoot,
                new LobbyViewHopPath(targets.Board, _services.Motion));
            var mover = new LobbyPieceMover(targets.Board, motion, _services.Audio);
            // 빛남용 머티리얼 복사본은 처음 호버할 때 만들므로 여기서는 자원을 잡지 않습니다.
            var highlight = new LobbyFacilityHighlight(targets.Facilities, _services.FacilityHover);
            _interaction = new LobbyInteraction(new LobbyFacilityPicker(targets.Output, targets.Facilities),
                new ScreenUIPointerBlocker(), mover, actions, isBlocked, highlight, () => IsLobbyLayer);
            _receiver = new FaultReportingLobbyReceiver(_interaction, _faults);
        }

        /// <summary>수신 슬롯에 걸고 로비 Layer로 바꿉니다. 도중에 연결이 끝나면 false이고, 끝난 연결이 남긴 Layer는 지웁니다.</summary>
        internal bool Connect(Func<bool> canWork)
        {
            if (canWork == null) throw new ArgumentNullException(nameof(canWork));
            _services.Slot.Bind(_receiver);
            if (!canWork()) return false;
            _layers.ChangeLayer(LobbyInputLayers.Lobby);
            if (canWork()) return true;
            // 동기 Canceled가 폐기한 연결의 늦은 Layer 적용만 비웁니다.
            if (IsLobbyLayer) _layers.ClearLayer();
            return false;
        }

        /// <summary>시설 고르기·호버·도착 실행을 한 Tick 진행합니다. Scope는 내부 부품을 꺼내지 않고 이것만 부릅니다(N-73).</summary>
        internal void Tick() => _interaction?.Tick();

        /// <summary>진행 중인 고르기·이동을 멈추고 슬롯을 풉니다. 현재 연결이면 자기 로비 Layer도 지웁니다.</summary>
        internal void Dispose()
        {
            LobbyInteraction interaction = _interaction;
            FaultReportingLobbyReceiver receiver = _receiver;
            if (interaction == null) return;
            List<Exception> errors = null;
            CleanupErrors.Try(interaction.Dispose, ref errors);
            CleanupErrors.Try(() => _services.Slot.Unbind(receiver), ref errors);
            CleanupErrors.Try(() =>
            {
                if (IsLobbyLayer) _layers.ClearLayer();
            }, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 로비 입력 정리 중 실패했습니다.");
        }
    }
}
