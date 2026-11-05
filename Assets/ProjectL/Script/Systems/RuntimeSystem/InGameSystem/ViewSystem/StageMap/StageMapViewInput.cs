using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>연결 한 번의 맵 입력 부품입니다. 노드 집기·맵 선택 입력·수신 슬롯·선택 Layer를 갖습니다.</summary>
    internal sealed class StageMapViewInput
    {
        private readonly StageMapViewServices _services;
        private readonly Func<bool> _isCurrent;

        internal StageMapViewInput(StageMapViewServices services, Func<bool> isCurrent)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
        }

        internal StageMapInteraction Interaction { get; private set; }
        private IStageSelectionInputReceiver _receiver;

        /// <summary>지금 입력 Layer가 맵 선택인지입니다. 팝업·설정 창이 Layer를 가져가면 거짓입니다. 쓰는 순간 원본을 읽습니다.</summary>
        internal bool IsSelectionLayer => _services.Layers.CurrentLayer.Equals(StageSelectionInputLayers.Selection);
#if UNITY_EDITOR
        private Editor.StageMapCameraFramingReceiver _cameraFraming;
        internal void TickCameraFraming() => _cameraFraming?.Tick();
#endif

        /// <summary>표시된 노드를 집는 맵 선택 입력을 만듭니다. 수신 슬롯 연결은 <see cref="Connect"/>에서 합니다.</summary>
        /// <param name="blocksEntry">노드를 눌렀을 때 진입 대신 다른 일을 했으면 참입니다(증강 선택이 남았을 때, 계획 P-TG1).</param>
        internal void Create(StageMapViewScene scene, Func<bool> blocksEntry,
            Func<int, int, bool> interceptEntry = null)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (blocksEntry == null) throw new ArgumentNullException(nameof(blocksEntry));
            var picker = new StageNodePicker(scene.Output, scene.Display, _services.MapData.PickDistance);
            Interaction = new StageMapInteraction(picker, _services.Requests, _services.RequestWriter,
                _services.Feedback, scene.CameraDrag, new StageMapPointerBlocker(), blocksEntry, interceptEntry);
            _receiver = Interaction;
#if UNITY_EDITOR
            _cameraFraming = new Editor.StageMapCameraFramingReceiver(Interaction, scene.CameraFraming, scene.Output);
            _receiver = _cameraFraming;
#endif
        }

        /// <summary>수신 슬롯에 걸고 선택 Layer로 바꿉니다. 도중에 연결이 끝나면 false이고, 끝난 연결이 남긴 Layer는 지웁니다.</summary>
        internal bool Connect(Func<bool> canWork)
        {
            if (canWork == null) throw new ArgumentNullException(nameof(canWork));
            _services.Slot.Bind(_receiver);
            if (!canWork()) return false;
            _services.Layers.ChangeLayer(StageSelectionInputLayers.Selection);
            if (canWork()) return true;
            if (_isCurrent() && IsSelectionLayer)
                _services.Layers.ClearLayer();
            return false;
        }

        /// <summary>
        /// 맵 선택 입력이 아닌 동안(상점·회복·덱·증강 팝업, 설정 창) 건물 호버를 지웁니다(스테이지 맵 설계 §5.7 구현 메모).
        /// 설정 창은 팝업 Scope를 거치지 않고 Layer만 덮어써 알림이 없으므로 Scope Tick이 부릅니다. 호버가 없으면 Layer를 읽지 않습니다.
        /// </summary>
        internal void ReleaseHoverOutsideSelection()
        {
            StageMapInteraction interaction = Interaction;
            if (interaction == null || interaction.HoveredNodeId == 0 || IsSelectionLayer) return;
            interaction.ClearPointer();
        }

        /// <summary>새 입력을 먼저 막고 슬롯을 풉니다. 현재 연결이면 자기 선택 Layer도 지웁니다.</summary>
        internal void Dispose()
        {
            StageMapInteraction interaction = Interaction;
            if (interaction == null) return;
            List<Exception> errors = null;
            CleanupErrors.Try(interaction.StopInput, ref errors);
            CleanupErrors.Try(() => _services.Slot.Unbind(_receiver), ref errors);
            CleanupErrors.Try(() =>
            {
                if (_isCurrent() && IsSelectionLayer)
                    _services.Layers.ClearLayer();
            }, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 맵 입력 정리 중 실패했습니다.");
        }
    }
}
