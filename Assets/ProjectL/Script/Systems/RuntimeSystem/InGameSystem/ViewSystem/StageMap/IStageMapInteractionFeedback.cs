namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 맵 입력 결과를 표현 쪽에 알리는 계약입니다. 강조 색·이펙트를 붙이는 자리입니다.
    /// 판단은 하지 않고 이미 정해진 사실만 받습니다.
    /// </summary>
    /// <remarks>구현: <see cref="StageMapInteractionLog"/>(현재 에디터 로그만), 테스트 가짜.</remarks>
    internal interface IStageMapInteractionFeedback
    {
        /// <summary>포인터 아래 노드가 바뀌었습니다. 노드가 없으면 0입니다.</summary>
        void HoverChanged(int previousNodeId, int currentNodeId);

        /// <summary>진입 요청을 StageView Buffer에 기록했습니다. 진입 승인을 뜻하지 않습니다.</summary>
        void EntryRequested(int nodeId, int requestNumber);
    }
}
