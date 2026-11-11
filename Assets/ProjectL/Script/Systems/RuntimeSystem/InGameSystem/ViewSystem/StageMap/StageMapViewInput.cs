using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 연결 한 번의 맵 입력 부품입니다. 노드 집기·맵 선택 입력·수신 슬롯·선택 Layer를 갖습니다.
    /// 맵 선택 입력은 안에만 두고, 바깥에는 의도 메서드(포인터 내려놓기·호버 Tick 등)만 냅니다(평가 N-03).
    /// </summary>
    internal sealed class StageMapViewInput
    {
        private readonly StageMapViewInputServices _services;
        private readonly Func<bool> _isCurrent;
        private readonly RuntimeFaultBoundary _faults;
        private StageMapInteraction _interaction;
        private IStageSelectionInputReceiver _receiver;
        private StageNodeHoverFeedback _hover;

        internal StageMapViewInput(StageMapViewInputServices services, Func<bool> isCurrent, RuntimeFaultBoundary faults)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        /// <summary>맵 선택 입력을 만들었습니다. 팝업 입력 세션이 맵 입력이 있는 동안만 Layer를 잡습니다.</summary>
        internal bool IsCreated => _interaction != null;

        /// <summary>지금 입력 Layer가 맵 선택인지입니다. 팝업·설정 창이 Layer를 가져가면 거짓입니다. 쓰는 순간 원본을 읽습니다.</summary>
        internal bool IsSelectionLayer => _services.Layers.CurrentLayer.Equals(StageSelectionInputLayers.Selection);

        /// <summary>표시된 노드를 집는 맵 선택 입력을 만듭니다. 수신 슬롯 연결은 <see cref="Connect"/>에서 합니다.</summary>
        /// <param name="blocksEntry">노드를 눌렀을 때 진입 대신 다른 일을 했으면 참입니다(증강 선택이 남았을 때, 계획 P-TG1).</param>
        /// <param name="canEnterNode">노드 진입 요청을 받아도 되는지입니다(맵이 한가하고 선택 화면이 열려 있음).</param>
        /// <param name="isMapIdle">노드 호버·배경 끌기·휠처럼 맵을 만져도 되는지입니다.</param>
        /// <param name="interceptEntry">노드 명령 대신 출전 미리 이동이 받았으면 참입니다.</param>
        internal void Create(StageMapViewScene scene, Func<bool> blocksEntry, Func<bool> canEnterNode,
            Func<bool> isMapIdle, Func<int, int, bool> interceptEntry)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            StageNodePicker picker = scene.CreatePicker(_services.MapData.PickDistance);
            _hover = new StageNodeHoverFeedback(_services.Feedback, _services.NodeAvailability,
                scene.Display, _services.MapData.HoverStyle);
            _interaction = new StageMapInteraction(picker, _services.Requests, _services.RequestWriter,
                _hover, canEnterNode, isMapIdle, scene.CameraDrag, new StageMapPointerBlocker(), blocksEntry, interceptEntry);
            // 입력 콜백은 런타임 Tick 밖에서 오므로 예외를 오류 경계로 보내는 감싸개를 슬롯에 겁니다(평가 C-18).
            _receiver = new FaultReportingStageSelectionReceiver(_interaction, _faults);
        }

        /// <summary>입력과 기본 윤곽선에 같은 표시 문맥을 전달합니다. 포인터 이동 여부와 관계없이 호출합니다.</summary>
        internal void SetDisplayedChapter(int chapterKey)
        {
            _interaction.SetDisplayedChapter(chapterKey);
            _hover.SetDisplayedChapter(chapterKey);
        }

        /// <summary>누름·끌기·호버를 내려놓습니다. 맵 입력을 만들기 전이면 아무것도 하지 않습니다.</summary>
        internal void ClearPointer() => _interaction?.ClearPointer();

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
            StageMapInteraction interaction = _interaction;
            if (interaction == null || interaction.HoveredNodeId == 0 || IsSelectionLayer) return;
            interaction.ClearPointer();
        }

        /// <summary>포인터가 멈춰 있어도 진행 사실과 강조 전환을 갱신합니다. 차단 중에는 이전 대상을 되살리지 않습니다.</summary>
        /// <param name="preserveBlockedPress">인트로가 맵을 붙잡은 동안이면 참입니다. 차단된 누름은 떼기·취소까지 남기고 호버만 비웁니다.</param>
        internal void TickHover(bool enabled, float deltaTime, bool preserveBlockedPress)
        {
            if (!enabled)
            {
                if (preserveBlockedPress) _interaction.ClearNodeHover();
                else _interaction.ClearPointer();
            }
            _hover.Tick(enabled, deltaTime);
        }

        /// <summary>새 입력을 먼저 막고 슬롯을 풉니다. 현재 연결이면 자기 선택 Layer도 지웁니다.</summary>
        internal void Dispose()
        {
            StageMapInteraction interaction = _interaction;
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
