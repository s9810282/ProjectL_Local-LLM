namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>보유 능력치 조회의 정상 상태다. 오류는 상태가 아니라 예외로 보고한다.</summary>
    public enum CharacterStatsQueryStatus
    {
        /// <summary>전투가 없고 보유 유닛이 있다. 항목이 하나 이상이다.</summary>
        Ready = 0,
        /// <summary>전투가 없고 보유 유닛이 없다. 조회를 위해 Run이나 기본 로스터를 만들지 않는다.</summary>
        EmptyRoster = 1,
        /// <summary>전투가 있다. 전투 중 능력치는 기존 전투 조회를 쓴다.</summary>
        BattleActive = 2,
        /// <summary>Core의 최초 게시 또는 원본 변경 후 재게시를 기다린다.</summary>
        Pending = 3,
    }
}
