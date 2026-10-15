namespace ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView
{
    /// <summary>View가 CombatViewBuffer에 기록하는 미리보기 요청의 종류입니다.</summary>
    public enum CombatViewPreviewKind
    {
        /// <summary>기본값입니다. 보관되는 요청에는 쓰이지 않습니다.</summary>
        None = 0,
        /// <summary>메인 캐릭터를 그 칸·방향에 배치할 수 있는지.</summary>
        MainCharacterPlacement = 1,
        /// <summary>유닛을 그 칸·방향에 배치할 수 있는지.</summary>
        UnitPlacement = 2,
        /// <summary>유닛의 이동 가능 범위.</summary>
        MoveRange = 3,
        /// <summary>유닛을 그 칸으로 옮겼을 때의 예상 결과.</summary>
        Move = 4,
        /// <summary>유닛의 공격 가능 범위.</summary>
        AttackRange = 5,
        /// <summary>그 조준으로 공격했을 때의 예상 결과.</summary>
        Attack = 6,
        ReturnToCheckpoint = 7
    }
}
