namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>
    /// 챕터 그래프 생성에서 일반 노드에 배정하는 스테이지 종류다. 출현 가중치·최소 개수를 정하는 단위이며,
    /// 실행 흐름용 StageType을 대신하지 않는다(일반 상점과 강화는 StageType이 같아도 여기서는 다른 종류다).
    /// </summary>
    public enum ChapterStageKind
    {
        /// <summary>보스가 아닌 전투.</summary>
        Combat = 1,
        /// <summary>일반 상점. 강화는 포함하지 않는다.</summary>
        Shop = 2,
        /// <summary>회복.</summary>
        Recovery = 3,
        /// <summary>강화.</summary>
        Enhancement = 4
    }
}
