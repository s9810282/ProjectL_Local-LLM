namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum MovementFailureReason
    {
        None = 0,
        EntityNotFound,
        NotUnit,
        WrongPhase,
        NotInCombat,
        Dead,
        UnsupportedMoveMethod = 7, // 삭제한 활성 유닛 오류의 번호 6은 재사용하지 않는다.
        SameAnchor = 8,
        OutOfBounds = 9,
        DestinationOccupied = 10,
        MoveRangeExceeded = 11,
        ActionCostInsufficient = 12,
        StateInconsistent = 13,
        StageEnded = 14, // 스테이지 종료
        PathOriginMismatch = 15,
        InvalidPath = 16,
        RepeatedCell = 17,
        StaleContext = 19, // 삭제한 이동 대기 오류의 번호 18은 재사용하지 않는다.
        VoluntaryMoveDisabled = 20, // 정의상 자발 이동을 하지 않는 유닛
        WrongController = 30,
        ActionAlreadyUsed,
        ActionDeferred,
        StaleTurn,
        Disarmed = 50,
        AnkleBroken = 51,
        Reconstructing = 52, // 적 전용 재구성 중(이동·공격 불가)
        Stunned = 53,
    }
}
