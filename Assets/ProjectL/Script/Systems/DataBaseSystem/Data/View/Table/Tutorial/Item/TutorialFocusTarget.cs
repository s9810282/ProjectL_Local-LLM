namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item
{
    /// <summary>
    /// 설명을 보여주는 동안 덮개에서 보이게 둘 화면 대상의 이름입니다(튜토리얼 설계 §15.11).
    /// 실제 화면 위치로 바꾸는 일은 튜토리얼 표시 코드가 합니다(구현 계획 T-6).
    /// </summary>
    internal enum TutorialFocusTarget
    {
        /// <summary>기본값입니다. 정의에는 쓰지 않습니다.</summary>
        None = 0,
        /// <summary>손패 전체입니다(슬라이드 8).</summary>
        Hand = 1,
        /// <summary>손패의 메인 캐릭터 카드입니다(슬라이드 9).</summary>
        MainCharacterCard = 2,
        /// <summary>전투 진입 버튼입니다(슬라이드 10).</summary>
        EnterCombatButton = 3,
        /// <summary>적 넥서스입니다(슬라이드 13).</summary>
        EnemyNexus = 4,
        /// <summary>필드의 메인 캐릭터 유닛입니다(슬라이드 14).</summary>
        MainCharacterUnit = 5,
        /// <summary>화면 상단에 보이는 메인 캐릭터 체력입니다(슬라이드 14).</summary>
        MainCharacterHealth = 6,
        /// <summary>현재 행동 유닛입니다(슬라이드 15·16·19·20).</summary>
        CurrentActorUnit = 7,
        /// <summary>행동 코스트 표시입니다(슬라이드 16·17·20·21).</summary>
        ActionCost = 8,
        /// <summary>공격 모드로 바꾸는 버튼입니다(슬라이드 19).</summary>
        AttackButton = 9,
        /// <summary>공격 대상이 될 적 유닛입니다(슬라이드 20). 어느 적을 보일지는 표시 코드에서 정합니다(T-6).</summary>
        EnemyUnits = 10
        // 11은 삭제된 UnitInfo 비용 강조 값입니다. 다른 대상으로 재사용하지 않습니다.
    }
}
