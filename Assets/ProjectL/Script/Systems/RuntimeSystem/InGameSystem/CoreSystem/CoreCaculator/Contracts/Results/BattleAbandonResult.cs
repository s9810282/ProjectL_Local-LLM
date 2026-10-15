namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>
    /// 자발적 포기 요청의 결과다. Abandoned만 이번 호출로 완료(패배·자발적 포기)를 기록한 경우다.
    /// 나머지는 아무것도 바꾸지 않는다. DB 정의 오류는 결과가 아니라 예외이며 그때도 아무것도 바꾸지 않는다.
    /// </summary>
    public enum BattleAbandonResult
    {
        /// <summary>이번 호출로 포기 완료를 기록했다. 호출자는 완료 표시 뒤 EndBattle로 정리한다.</summary>
        Abandoned,
        /// <summary>전투가 이미 완료(승리·패배·포기)를 기록했다. 다시 정산하지 않는다.</summary>
        AlreadyCompleted,
        /// <summary>Run이 이미 끝났다.</summary>
        RunEnded,
        /// <summary>포기할 활성 전투가 없다.</summary>
        NoActiveBattle
    }
}
