using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 건물에 마우스를 올리면 그 스테이지의 제목·설명을 전역 Hover 패널에 보입니다(스테이지 맵 설계 §5.7).
    /// 호버 대상은 맵 입력이, 패널 위치·열림 상태는 Hover 패널이 정합니다. 여기서는 글을 고르고 열고 닫기만 합니다.
    /// </summary>
    /// <remarks>
    /// 글 원본은 모든 스테이지가 공통 화면 글 표 한 곳입니다(§5.7 3판). 그 키의 줄이 있으면 그 줄(팝업 머리줄과 같은 글),
    /// 없으면 종류·역할 공통 줄입니다. 찾는 규칙은 표(<see cref="IStageOfferTextTableQuery.GetNodeText"/>)가 갖습니다.
    /// 시작 노드는 띄우지 않고(Q-NH3), 누른 노드는 대상이 바뀔 때까지 다시 띄우지 않습니다(Q-NH5).
    /// 글은 대상이 바뀔 때 한 번 찾고, 포인터 이동마다는 위치만 넘깁니다.
    /// 입력 콜백에서 불리므로 글·패널 오류는 던지지 않고 오류 경계에 보고합니다(맵 Scope의 입력 경로와 같은 규칙).
    /// </remarks>
    internal sealed class StageNodeHoverInfo : IStageMapInteractionFeedback
    {
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IStageOfferTextTableQuery _offerTexts;
        private readonly IUITextPresentation _text;
        private readonly Action<Exception> _report;
        private int _nodeId;
        private string _name;
        private string _description;
        private bool _pressed;
        private bool _shown;
        private uint _shownVersion;

        internal StageNodeHoverInfo(IStageProgressBufferReadSink stages, IStageOfferTextTableQuery offerTexts,
            IUITextPresentation text, Action<Exception> report)
        {
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _offerTexts = offerTexts ?? throw new ArgumentNullException(nameof(offerTexts));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public void HoverChanged(int chapterKey, int nodeId)
        {
            try
            {
                Hide();
                _pressed = false;
                _nodeId = 0;
                _name = null;
                _description = null;
                // 표시를 마치기 전의 문맥은 쓰지 않습니다(진입 요청과 같은 규칙).
                if (nodeId == 0 || chapterKey < 1)
                    return;
                if (TryFindText(chapterKey, nodeId, out _name, out _description))
                    _nodeId = nodeId;
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        public void HoverMoved(float screenX, float screenY)
        {
            if (_nodeId == 0 || _pressed)
                return;
            try
            {
                // 밖에서 닫혔으면(씬 재바인딩 등) 숨김 번호가 바뀌어 있어 다시 엽니다.
                if (!_shown || _text.HoverHideVersion != _shownVersion)
                {
                    _text.ShowHover(_name, _description);
                    _shown = true;
                    _shownVersion = _text.HoverHideVersion;
                }
                _text.MoveHover(new Vector2(screenX, screenY));
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        public void NodePressed(int nodeId)
        {
            // 포인터 이동 없이 다른 노드가 눌렸으면 다음 이동이 대상을 다시 맞추므로 억제하지 않습니다.
            if (nodeId == _nodeId)
                _pressed = true;
            try
            {
                Hide();
            }
            catch (Exception error)
            {
                Fail(error);
            }
        }

        public void EntryRequested(int nodeId, int requestNumber)
        {
#if UNITY_EDITOR
            Debug.Log($"[StageMap] 진입 요청 기록: 노드 {nodeId}, 요청 번호 {requestNumber}");
#endif
        }

        private void Hide()
        {
            if (!_shown)
                return;
            _shown = false;
            // 밖에서 이미 닫혔으면 지금 열린 패널은 다른 사용처의 것일 수 있어 닫지 않습니다.
            if (_text.HoverHideVersion == _shownVersion)
                _text.UnHover();
        }

        /// <summary>대상을 버려 같은 오류를 포인터 이동마다 되풀이하지 않고, 오류 경계에 보고합니다.</summary>
        private void Fail(Exception error)
        {
            _nodeId = 0;
            _report(error);
        }

        private bool TryFindText(int chapterKey, int nodeId, out string name, out string description)
        {
            IReadOnlyList<StageProgressData> nodes = _stages.GetStages(chapterKey);
            for (int index = 0; index < nodes.Count; index++)
            {
                StageProgressData node = nodes[index];
                if (node.NodeId != nodeId)
                    continue;

                if (node.IsStart)
                    break;

                // 키 줄도 공통 줄도 없으면 표가 StageKey·종류·역할을 담은 KeyNotFoundException을 던지고 구성 오류로 보고됩니다.
                StageOfferTextData text = _offerTexts.GetNodeText(node.StageKey, node.StageType, node.MapRole);
                name = text.EventName;
                description = text.EventDescription;
                return true;
            }

            // 시작 노드, 또는 맵을 다시 그리기 직전이라 목록에 없는 노드는 띄우지 않습니다.
            name = null;
            description = null;
            return false;
        }
    }
}
