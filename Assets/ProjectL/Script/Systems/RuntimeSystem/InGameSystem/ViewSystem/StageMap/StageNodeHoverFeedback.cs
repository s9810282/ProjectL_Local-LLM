using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>기존 호버 설명을 유지하면서 건물·선 강조를 연결합니다. 입력 콜백은 대상만 바꾸고 조회·렌더는 Tick에서 합니다.</summary>
    internal sealed class StageNodeHoverFeedback : IStageMapInteractionFeedback
    {
        private readonly IStageMapInteractionFeedback _info;
        private readonly StageNodeAvailabilityReader _availability;
        private readonly StageMapViewDisplay _display;
        private readonly StageNodeHoverStyleData _style;
        private readonly List<int> _visibleNodes = new();
        private readonly HashSet<int> _availableNodes = new();
        private int _chapterKey;
        private int _nodeId;
        private bool _pressed;

        internal StageNodeHoverFeedback(IStageMapInteractionFeedback info, StageNodeAvailabilityReader availability,
            StageMapViewDisplay display, StageNodeHoverStyleData style)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _availability = availability ?? throw new ArgumentNullException(nameof(availability));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _style = style ?? throw new InvalidOperationException("[StageNodeHoverFeedback] Hover Style 설정이 필요합니다.");
        }

        public void HoverChanged(int chapterKey, int nodeId)
        {
            _nodeId = nodeId;
            _pressed = false;
            _info.HoverChanged(chapterKey, nodeId);
        }

        /// <summary>최초 호버 전에도 실제로 표시된 챕터를 사용합니다. 같은 챕터를 다시 그려도 캐시는 비웁니다.</summary>
        internal void SetDisplayedChapter(int chapterKey)
        {
            _chapterKey = chapterKey;
            _availability.Invalidate();
        }

        public void HoverMoved(float screenX, float screenY) => _info.HoverMoved(screenX, screenY);

        public void NodePressed(int nodeId)
        {
            if (nodeId == _nodeId) _pressed = true;
            _info.NodePressed(nodeId);
        }

        public void EntryRequested(int nodeId, int requestNumber) => _info.EntryRequested(nodeId, requestNumber);

        internal void Tick(bool enabled, float deltaTime)
        {
            int node = enabled && !_pressed && _display.HasVisibleBuilding(_nodeId) ? _nodeId : 0;
            _availableNodes.Clear();
            if (enabled)
            {
                _display.FillVisibleNodeIds(_visibleNodes);
                _availability.FillAvailable(_chapterKey, _visibleNodes, _availableNodes);
            }
            _display.TickHover(node, _availableNodes, _style, deltaTime);
        }
    }
}
