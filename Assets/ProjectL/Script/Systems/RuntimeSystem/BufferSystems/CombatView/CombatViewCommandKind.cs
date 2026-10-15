namespace ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView
{
    /// <summary>View가 CombatViewBuffer에 기록하는 실행 명령의 종류입니다.</summary>
    public enum CombatViewCommandKind
    {
        /// <summary>기본값입니다. 보관되는 명령에는 쓰이지 않습니다.</summary>
        None = 0,
        /// <summary>메인 캐릭터 최초 배치. 칸과 방향을 받습니다.</summary>
        PlaceMainCharacter = 1,
        /// <summary>손패의 유닛을 필드에 배치. 개체 ID, 칸, 방향을 받습니다.</summary>
        PlaceUnit = 2,
        /// <summary>필드의 유닛을 빈 손패 슬롯으로 회수. 개체 ID를 받습니다.</summary>
        ReturnUnit = 3,
        /// <summary>준비를 마치고 전투를 시작합니다.</summary>
        EnterCombat = 4,
        /// <summary>유닛 이동. 개체 ID와 목표 칸을 받습니다.</summary>
        Move = 5,
        /// <summary>유닛 공격. 개체 ID와 조준을 받습니다.</summary>
        Attack = 6,
        /// <summary>현재 유닛의 턴을 끝냅니다.</summary>
        EndUnitTurn = 7,
        /// <summary>수동 드로우.</summary>
        DrawCharacter = 8,
        ReturnToCheckpoint = 9
    }
}
