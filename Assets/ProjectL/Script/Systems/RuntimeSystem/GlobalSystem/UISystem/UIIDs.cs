namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem
{
    public enum UIId
    {
        None = 0,
        Setting = 1,
        Loading = 2,
        Stage = 3,
        StageSelect = 4,
        Architecture = 5,
        StageResult = 6,
        TestGlobal = 7,
        MainCharactor = 8,
        Deck = 9,
        CombatLog = 10,
        UnitCard = 11,
        Artifact = 13,
        Shop = 14,
        LobbyDeck = 15,
        Confirm = 16,
        KeySetting = 17,
        SoundSetting = 18,
        Recovery = 19,
        Notice = 20,
        Hover = 21,
        NightChecker = 22,
        BattleChanger = 23,
        Pack = 24,
        Reputation = 25,
        CombatEnd = 26,
        /// <summary>Boot 씬의 게임 시작 화면입니다.</summary>
        Title = 27,
        /// <summary>타이틀 종료 후 공통 장식입니다.</summary>
        AfterTitle = 28,
        CombatCategoryInfo = 29,
        CombatCostInfo = 30,
        /// <summary>Combat 씬의 튜토리얼 설명·안내 화면입니다.</summary>
        Tutorial = 31,
        /// <summary>StageSelect의 덱 편집 팝업입니다(런 덱 연결 설계 RV2). 70aa3e5 병합에서 29~31이 먼저 쓰여 32로 옮겼습니다.</summary>
        DeckEdit = 32,
        CombatHud = 35,
        CombatActionPanel = 36,
        CombatUnitInfo = 37,
        UnitHUDLayer = 38,
        UnitHUDInfoPanel = 44,
        UnitHUDHoverPanel = 45,
        /// <summary>덱 편집 슬롯이 공유하는 상세 정보창입니다.</summary>
        DeckSlotInfo = 46,
        /// <summary>Combat 덱 카드의 공용 상세창입니다.</summary>
        CombatCardInfo = 47,
        /// <summary>개별 Cost 위젯을 함께 표시하는 전투 HUD입니다.</summary>
        TurnCost = 39,
        /// <summary>비전투 스테이지 화면들의 부모입니다.</summary>
        StageScreenRoot = 40,
        /// <summary>전투 턴 전환을 잠시 알리는 화면입니다.</summary>
        TurnDisplay = 41,
        /// <summary>레벨업 특성 성장(증강) 선택 화면입니다. 다른 팝업을 닫지 않게 Parallel로 엽니다.</summary>
        TraitGrowth = 42,
        /// <summary>덱 편집 Canvas 내부의 변경 적용 후 출전 확인창입니다.</summary>
        DeckCombatConfirm = 43,
        /// <summary>로비 시설 안내의 기본 화면입니다.</summary>
        Clicker = 34,
        /// <summary>Lobby 도착 시 최종 승리의 지난 Run 요약을 표시합니다.</summary>
        EndingRunSummary = 33,
    }
}
