using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 맵 선택 입력을 해석합니다. 노드를 눌렀다가 같은 노드에서 떼면 진입 요청을 한 번 기록합니다.
    /// 카메라·Transform·Core를 모르는 순수 C#입니다.
    /// </summary>
    /// <remarks>
    /// 진입 가능 여부는 판단하지 않습니다(Core 몫). 처리 전 요청이 Buffer에 남아 있으면 새 요청을 기록하지 않습니다.
    /// 처리된 요청과 씬 이탈 시 남은 요청의 정리는 StageMapEntryFlow가 담당합니다.
    /// </remarks>
    internal sealed class StageMapInteraction : IStageSelectionInputReceiver
    {
        private readonly IStageNodePicker _picker;
        private readonly IStageViewBufferReadSink _requests;
        private readonly IStageViewBufferWriteSink _requestWriter;
        private readonly IStageMapInteractionFeedback _feedback;
        private int _hoveredNodeId;
        private int _pressedNodeId;

        internal StageMapInteraction(IStageNodePicker picker, IStageViewBufferReadSink requests,
            IStageViewBufferWriteSink requestWriter, IStageMapInteractionFeedback feedback)
        {
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _requestWriter = requestWriter ?? throw new ArgumentNullException(nameof(requestWriter));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        /// <summary>포인터 아래 노드입니다. 없으면 0입니다.</summary>
        internal int HoveredNodeId => _hoveredNodeId;

        public void PointerMoved(float screenX, float screenY)
        {
            int nodeId = Pick(screenX, screenY);
            if (nodeId == _hoveredNodeId)
                return;

            int previous = _hoveredNodeId;
            _hoveredNodeId = nodeId;
            _feedback.HoverChanged(previous, nodeId);
        }

        public void SelectPressed(float screenX, float screenY)
        {
            _pressedNodeId = Pick(screenX, screenY);
        }

        public void SelectReleased(float screenX, float screenY)
        {
            int pressed = _pressedNodeId;
            _pressedNodeId = 0;
            if (pressed == 0 || Pick(screenX, screenY) != pressed)
                return;

            RequestEntry(pressed);
        }

        public void SelectCanceled()
        {
            _pressedNodeId = 0;
        }

        public void ConfirmRequested()
        {
            // 클릭이 곧 진입 요청이므로 별도 확정 입력은 쓰지 않습니다(계획 §8.3 Q1).
        }

        public void CancelRequested()
        {
            _pressedNodeId = 0;
        }

        private void RequestEntry(int nodeId)
        {
            // 처리 전 요청은 Buffer 한 곳에서 판단합니다. 연타·중복 클릭으로 요청이 쌓이지 않습니다.
            if (_requests.Commands.Count > 0)
                return;

            int number = _requestWriter.Submit(StageViewCommandData.SelectStageNode(new StageNodeId(nodeId)));
            _feedback.EntryRequested(nodeId, number);
        }

        private int Pick(float screenX, float screenY)
        {
            return _picker.TryPickNode(screenX, screenY, out int nodeId) ? nodeId : 0;
        }
    }
}
