namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 맵 입력 결과를 표현 쪽에 알리는 계약입니다. 건물 호버 정보·이펙트를 붙이는 자리입니다.
    /// 판단은 하지 않고 이미 정해진 사실만 받습니다.
    /// </summary>
    /// <remarks>구현: <see cref="StageNodeHoverInfo"/>(호버 제목·설명, 진입 요청 에디터 로그).</remarks>
    internal interface IStageMapInteractionFeedback
    {
        /// <summary>포인터 아래 노드가 바뀌었습니다. 노드가 없으면 0입니다. 챕터는 표시를 마친 맵의 키이고, 아직 없으면 0입니다.</summary>
        void HoverChanged(int chapterKey, int nodeId);

        /// <summary>호버 중인 노드 위에서 포인터가 움직였습니다. 대상이 바뀐 이동이면 <see cref="HoverChanged"/> 다음에 옵니다.</summary>
        void HoverMoved(float screenX, float screenY);

        /// <summary>노드를 누르기 시작했거나, 누른 노드 위에서 뗐습니다(고름). 진입 요청을 뜻하지 않습니다.</summary>
        void NodePressed(int nodeId);

        /// <summary>진입 요청을 StageView Buffer에 기록했습니다. 진입 승인을 뜻하지 않습니다.</summary>
        void EntryRequested(int nodeId, int requestNumber);
    }
}
