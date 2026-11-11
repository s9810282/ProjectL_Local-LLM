namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial
{
    /// <summary>전투 입력 검사가 읽는 튜토리얼 입력 제한입니다(튜토리얼 설계 §5·§15.10).</summary>
    public enum TutorialCombatInputRestriction
    {
        /// <summary>제한하지 않습니다.</summary>
        None = 0,
        /// <summary>이동과 재시도를 위한 턴 종료·재배치를 허용합니다.</summary>
        MoveOnly = 1,
        /// <summary>공격과 재시도를 위한 이동·되돌리기·턴 종료·재배치를 허용합니다.</summary>
        AttackPractice = 2,
        /// <summary>플레이어 턴 종료만 허용합니다.</summary>
        EndTurnOnly = 3
    }
}
