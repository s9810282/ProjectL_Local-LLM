using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>Stage의 도메인입니다. 기록 누락으로 멈출 때 이 단위로 멈추고 다른 도메인은 계속합니다.</summary>
    public enum StageDomain
    {
        /// <summary>노드 진입(맵 명령·전투 진입).</summary>
        Entry,
        Shop,
        Recovery,
        /// <summary>레벨업 특성 성장(증강).</summary>
        Growth
    }

    /// <summary>진행 중인 전투 진입 시도의 지금 상태입니다.</summary>
    public enum StageCombatEntryState
    {
        /// <summary>시도가 없습니다.</summary>
        None,
        /// <summary>전투 진입 요청의 수명이나 View 입력 정리를 기다립니다. 다른 일을 걸지 않습니다.</summary>
        Waiting,
        /// <summary>활성·거절·취소로 끝났고 입력도 정리됐습니다. 시도를 닫습니다.</summary>
        Done,
        /// <summary>확인 전에 끝났거나 처리 기록이 없습니다. 진입 도메인을 멈춥니다.</summary>
        Broken
    }

    /// <summary>
    /// Stage의 다음 할 일을 정할 때 쓰는 사실 묶음입니다(검사 후속 정리 계획 TP-R9). <see cref="IStageProgress.Read"/>가 부를 때마다 새로 만들며 저장하지 않습니다.
    /// 판단은 <c>StagePlan.Decide</c> 한 곳이 합니다. 값 형식이라 매 Tick 읽어도 할당이 없습니다.
    /// </summary>
    public readonly struct StageProgressFacts
    {
        internal StageProgressFacts(
            bool isLoading = false,
            StageCombatEntryState combatEntry = StageCombatEntryState.None,
            string combatEntryProblem = null,
            bool isEntryStopped = false,
            bool isShopStopped = false,
            bool isRecoveryStopped = false,
            bool isGrowthStopped = false,
            bool hasMapCommand = false,
            StageViewCommand mapCommand = default,
            bool isMapReady = false,
            bool isCombatNode = false,
            int shopCommandNumber = 0,
            bool canReserveRecovery = false,
            bool canReserveGrowth = false,
            bool growthNeedsOffer = false,
            RunId abandonRunId = default)
        {
            IsLoading = isLoading;
            CombatEntry = combatEntry;
            CombatEntryProblem = combatEntryProblem;
            IsEntryStopped = isEntryStopped;
            IsShopStopped = isShopStopped;
            IsRecoveryStopped = isRecoveryStopped;
            IsGrowthStopped = isGrowthStopped;
            HasMapCommand = hasMapCommand;
            MapCommand = mapCommand;
            IsMapReady = isMapReady;
            IsCombatNode = isCombatNode;
            ShopCommandNumber = shopCommandNumber;
            CanReserveRecovery = canReserveRecovery;
            CanReserveGrowth = canReserveGrowth;
            GrowthNeedsOffer = growthNeedsOffer;
            AbandonRunId = abandonRunId;
        }

        /// <summary>씬 로딩 중입니다. 이때는 다른 사실을 읽지 않습니다.</summary>
        public bool IsLoading { get; }

        /// <summary>포기할 런의 ID입니다. 요청이 없으면 비어 있습니다.</summary>
        public RunId AbandonRunId { get; }

        public StageCombatEntryState CombatEntry { get; }

        /// <summary><see cref="StageCombatEntryState.Broken"/>일 때 멈춤 사유입니다.</summary>
        public string CombatEntryProblem { get; }

        public bool IsEntryStopped { get; }
        public bool IsShopStopped { get; }
        public bool IsRecoveryStopped { get; }
        public bool IsGrowthStopped { get; }

        /// <summary>Core가 처리하지 않은 첫 맵 명령이 있습니다(<see cref="MapCommand"/>).</summary>
        public bool HasMapCommand { get; }
        public StageViewCommand MapCommand { get; }

        /// <summary>맵 그래프가 준비됐습니다. 맵 명령이 있을 때만 읽습니다.</summary>
        public bool IsMapReady { get; }

        /// <summary>맵 명령의 노드가 전투 노드입니다. 맵 명령이 있을 때만 읽습니다.</summary>
        public bool IsCombatNode { get; }

        /// <summary>Core가 처리하지 않은 첫 상점 명령 번호입니다. 없으면 0입니다.</summary>
        public int ShopCommandNumber { get; }

        /// <summary>제출됐지만 예약하지 않은 회복 선택이 있습니다.</summary>
        public bool CanReserveRecovery { get; }

        /// <summary>제출됐지만 예약하지 않은 증강 선택이 있습니다.</summary>
        public bool CanReserveGrowth { get; }

        /// <summary>증강 선택이 필요한데 제안이 아직 없습니다(준비할 차례).</summary>
        public bool GrowthNeedsOffer { get; }
    }
}
