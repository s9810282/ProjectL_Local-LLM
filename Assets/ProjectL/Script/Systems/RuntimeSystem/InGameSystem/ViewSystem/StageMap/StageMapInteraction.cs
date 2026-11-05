using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 맵 선택 입력을 해석합니다. 노드 클릭과 배경 드래그는 누르기 시작할 때 소유권을 정합니다.
    /// 카메라·Transform·Core를 모르는 순수 C#입니다.
    /// </summary>
    /// <remarks>
    /// 진입 가능 여부는 판단하지 않습니다(Core 몫). 처리 전 요청이 Buffer에 남아 있으면 새 요청을 기록하지 않습니다.
    /// 처리 결과의 입력 제거는 StageMapEntryObserver, 씬 이탈 정리는 소유 View가 담당합니다.
    /// </remarks>
    internal sealed class StageMapInteraction : IStageSelectionInputReceiver
    {
        private readonly IStageNodePicker _picker;
        private readonly IStageViewBufferReadSink _requests;
        private readonly IStageViewBufferWriteSink _requestWriter;
        private readonly IStageMapInteractionFeedback _feedback;
        private readonly IStageMapCameraDrag _cameraDrag;
        private readonly IStageMapPointerBlocker _pointerBlocker;
        private readonly Func<bool> _blocksEntry;
        private readonly Func<int, int, bool> _interceptEntry;
        private PointerOwner _pointerOwner;
        private int _hoveredNodeId;
        private int _pressedNodeId;
        private bool _stopped;
        private int _displayedChapter;

        /// <param name="blocksEntry">노드를 눌렀을 때 진입 대신 다른 일을 했으면 참을 돌려줍니다(증강 선택이 남았을 때 화면 다시 띄우기, 계획 P-TG1). 없으면 늘 진입합니다.</param>
        internal StageMapInteraction(IStageNodePicker picker, IStageViewBufferReadSink requests,
            IStageViewBufferWriteSink requestWriter, IStageMapInteractionFeedback feedback,
            IStageMapCameraDrag cameraDrag = null, IStageMapPointerBlocker pointerBlocker = null,
            Func<bool> blocksEntry = null, Func<int, int, bool> interceptEntry = null)
        {
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _requestWriter = requestWriter ?? throw new ArgumentNullException(nameof(requestWriter));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _cameraDrag = cameraDrag;
            _pointerBlocker = pointerBlocker;
            _blocksEntry = blocksEntry;
            _interceptEntry = interceptEntry;
        }

        /// <summary>포인터 아래 노드입니다. 없으면 0입니다.</summary>
        internal int HoveredNodeId => _hoveredNodeId;

        /// <summary>실제로 표시를 마친 맵의 문맥만 입력에 사용합니다.</summary>
        internal void SetDisplayedChapter(int chapterKey)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            ResetPointer();
            _displayedChapter = chapterKey;
        }

        public void PointerMoved(float screenX, float screenY)
        {
            if (_stopped) return;
            if (_pointerOwner == PointerOwner.Background)
            {
                _cameraDrag?.MoveDrag(screenX, screenY);
                return;
            }

            if (_pointerOwner == PointerOwner.Blocked)
                return;

            SetHover(IsBlocked(screenX, screenY) ? 0 : Pick(screenX, screenY));
            if (_hoveredNodeId != 0)
                _feedback.HoverMoved(screenX, screenY);
        }

        public void SelectPressed(float screenX, float screenY)
        {
            if (_stopped || _pointerOwner != PointerOwner.None) return;
            if (IsBlocked(screenX, screenY))
            {
                _pointerOwner = PointerOwner.Blocked;
                SetHover(0);
                return;
            }

            _pressedNodeId = Pick(screenX, screenY);
            if (_pressedNodeId != 0)
            {
                _pointerOwner = PointerOwner.Node;
                _feedback.NodePressed(_pressedNodeId);
                return;
            }

            _pointerOwner = PointerOwner.Background;
            SetHover(0);
            _cameraDrag?.BeginDrag(screenX, screenY);
        }

        public void SelectReleased(float screenX, float screenY)
        {
            if (_stopped) return;
            PointerOwner owner = _pointerOwner;
            int pressed = _pressedNodeId;
            EndSelection();
            if (owner != PointerOwner.Node)
                return;

            if (IsBlocked(screenX, screenY))
            {
                SetHover(0);
                return;
            }

            if (Pick(screenX, screenY) != pressed)
                return;

            // 누른 채 밖에 나갔다 돌아오면 호버가 다시 잡히므로, 고르는 순간 한 번 더 알립니다(묶음 끝 검토 L5).
            _feedback.NodePressed(pressed);
            RequestEntry(pressed);
        }

        public void SelectCanceled()
        {
            ResetPointer();
        }

        public void ConfirmRequested()
        {
            // 클릭이 곧 진입 요청이므로 별도 확정 입력은 쓰지 않습니다(계획 §8.3 Q1).
        }

        public void CancelRequested()
        {
            ResetPointer();
        }

        /// <summary>슬롯 해제가 실패해도 이전 수신자가 새 입력을 기록하지 못하게 먼저 차단합니다.</summary>
        internal void StopInput()
        {
            _stopped = true;
            ResetPointer();
        }

        /// <summary>
        /// 맵 선택 입력이 아닌 동안 누름·끌기·호버를 내려놓습니다(팝업·설정 창, 스테이지 맵 계획 §3.18 P-NH1).
        /// 입력은 막지 않으므로 맵 선택으로 돌아온 뒤의 포인터 이동이 호버를 다시 찾습니다.
        /// </summary>
        internal void ClearPointer() => ResetPointer();

        private void ResetPointer()
        {
            EndSelection();
            SetHover(0);
        }

        private void EndSelection()
        {
            PointerOwner owner = _pointerOwner;
            _pointerOwner = PointerOwner.None;
            _pressedNodeId = 0;
            if (owner == PointerOwner.Background)
                _cameraDrag?.EndDrag();
        }

        private void SetHover(int nodeId)
        {
            if (nodeId == _hoveredNodeId)
                return;

            _hoveredNodeId = nodeId;
            _feedback.HoverChanged(_displayedChapter, nodeId);
        }

        private bool IsBlocked(float screenX, float screenY)
        {
            return _pointerBlocker != null && _pointerBlocker.IsBlocked(screenX, screenY);
        }

        private void RequestEntry(int nodeId)
        {
            // 처리 전 요청은 Buffer 한 곳에서 판단합니다. 연타·중복 클릭으로 요청이 쌓이지 않습니다.
            if (_displayedChapter == 0 || _requests.Commands.Count > 0)
                return;
            // 증강 선택이 남아 있으면 노드 명령을 쓰지 않습니다. Core도 PendingTraitGrowth로 거절하지만 화면이 먼저 막습니다.
            if (_blocksEntry != null && _blocksEntry())
                return;
            if (_interceptEntry != null && _interceptEntry(_displayedChapter, nodeId)) return;

            int number = _requestWriter.Submit(StageViewCommandData.SelectStageNode(_displayedChapter, new StageNodeId(nodeId)));
            _feedback.EntryRequested(nodeId, number);
        }

        private int Pick(float screenX, float screenY)
        {
            return _picker.TryPickNode(screenX, screenY, out int nodeId) ? nodeId : 0;
        }

        private enum PointerOwner
        {
            None,
            Blocked,
            Node,
            Background,
        }
    }
}
