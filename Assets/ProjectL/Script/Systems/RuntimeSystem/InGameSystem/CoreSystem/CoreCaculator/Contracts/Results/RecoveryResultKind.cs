namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>회복 수락, 거래 거절, 진입 상태 불일치를 구분한다.</summary>
    public enum RecoveryResultKind
    {
        Accepted = 1,
        TradeRejected = 2,
        StateMismatch = 3
    }
}
