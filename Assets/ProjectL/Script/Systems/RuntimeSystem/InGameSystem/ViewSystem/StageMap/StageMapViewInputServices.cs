using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵 선택 입력(노드 집기·호버·노드 요청·수신 슬롯·선택 Layer)이 쓰는 값의 묶음입니다(스테이지 선택 리팩토링 설계 §2.9 C-19).</summary>
    internal sealed class StageMapViewInputServices
    {
        internal StageMapViewInputServices(StageMapViewData mapData, IStageMapInteractionFeedback feedback,
            StageNodeAvailabilityReader nodeAvailability, IStageViewBufferReadSink requests,
            IStageViewBufferWriteSink requestWriter, IStageSelectionReceiverSlot slot, IInputLayerService layers)
        {
            MapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            Feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            NodeAvailability = nodeAvailability ?? throw new ArgumentNullException(nameof(nodeAvailability));
            Requests = requests ?? throw new ArgumentNullException(nameof(requests));
            RequestWriter = requestWriter ?? throw new ArgumentNullException(nameof(requestWriter));
            Slot = slot ?? throw new ArgumentNullException(nameof(slot));
            Layers = layers ?? throw new ArgumentNullException(nameof(layers));
        }

        internal StageMapViewData MapData { get; }
        /// <summary>
        /// 맵 입력 알림을 받는 호버 표시입니다. System이 한 번 만들어 연결 사이에도 남고, 호버 상태를 가집니다.
        /// 연결이 끝날 때 맵 입력 정리(<see cref="StageMapInteraction.StopInput"/>)가 호버를 0으로 알려 상태를 비웁니다.
        /// </summary>
        internal IStageMapInteractionFeedback Feedback { get; }
        /// <summary>연결 사이에 남는 노드 진입 가능 읽기입니다. System이 한 번 만듭니다.</summary>
        internal StageNodeAvailabilityReader NodeAvailability { get; }
        internal IStageViewBufferReadSink Requests { get; }
        internal IStageViewBufferWriteSink RequestWriter { get; }
        internal IStageSelectionReceiverSlot Slot { get; }
        internal IInputLayerService Layers { get; }
    }
}
