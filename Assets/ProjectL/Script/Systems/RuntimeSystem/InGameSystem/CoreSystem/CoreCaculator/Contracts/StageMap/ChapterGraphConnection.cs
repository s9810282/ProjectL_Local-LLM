namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>챕터 그래프에서 한 층의 노드가 다음 층의 어느 노드로 이어질지 고르는 방식이다.</summary>
    public enum ChapterGraphConnection
    {
        /// <summary>폭 비율로 가장 가까운 노드.</summary>
        Nearest,
        /// <summary>다음 층의 노드 중 균등 무작위.</summary>
        Random,
        /// <summary>가까운 노드와 나머지 노드의 가중치로 무작위.</summary>
        WeightedRandom
    }
}
