namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>회복 옵션 선택과 노드 완료를 구분한다. 기본값 0은 명령이 아니다.</summary>
    public enum RecoveryCommandKind
    {
        Select = 1,
        Complete = 2
    }
}
