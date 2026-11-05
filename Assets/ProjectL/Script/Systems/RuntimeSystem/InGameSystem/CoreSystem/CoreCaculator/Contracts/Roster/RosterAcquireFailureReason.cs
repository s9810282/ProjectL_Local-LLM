namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public enum RosterAcquireFailureReason
    {
        None = 0,
        InvalidRequest = 1,
        InvalidDefinition = 2,
        /// <summary>
        /// 더 이상 발생하지 않는다. 같은 정의(메인·서브)의 중복 보유는 소유 규칙의 <see cref="OwnershipLimitReached"/>로 거절한다.
        /// 기존 호출자(표시 문구 등)의 컴파일을 위해 값은 남긴다. 제거는 View 전달 문서의 이관 항목이다.
        /// </summary>
        MainAlreadyOwned = 3,
        InvalidMainState = 4,
        IdentityOverflow = 5,
        RunEnded = 6,
        OwnershipLimitReached = 7,
        /// <summary>시작 로드아웃의 초기 덱 선택이 덱 규칙을 어겼다. 구체 사유는 RosterAcquireResult.InitialDeckFailure다.</summary>
        InvalidInitialDeck = 8
    }
}
