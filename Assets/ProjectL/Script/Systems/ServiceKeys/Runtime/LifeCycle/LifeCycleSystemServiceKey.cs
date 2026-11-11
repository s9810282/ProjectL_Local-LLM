using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.ServiceKeys.Runtime.LifeCycle
{
    internal static class LifeCycleSystemServiceKey
    {
        public static readonly SystemGroupKey<IArtifactBoardPreparation> ArtifactBoardPreparation = new(nameof(ArtifactBoardPreparation));

        public static readonly SystemGroupKey<ICombatEntryService> CombatEntry = new(nameof(CombatEntry));

        public static readonly SystemGroupKey<ICombatProgressReader> CombatProgress = new(nameof(CombatProgress));

        public static readonly SystemGroupKey<IStageEntryAttemptRead> StageEntryAttempts = new(nameof(StageEntryAttempts));

        /// <summary>이번 Stage 방문의 진행 사실·상태 바꿈 입구. Stage Plan만 쓴다(검사 후속 정리 계획 TP-R9).</summary>
        public static readonly SystemGroupKey<IStageProgress> StageProgress = new(nameof(StageProgress));

        public static readonly SystemGroupKey<IStageRunAbandonRequestService> StageRunAbandon = new(nameof(StageRunAbandon));

        /// <summary>증강 선택이 남아 있는지(읽기 전용, 강화 View 설계 §14 TG2).</summary>
        public static readonly SystemGroupKey<IStageTraitGrowthFacts> StageTraitGrowthFacts = new(nameof(StageTraitGrowthFacts));

        /// <summary>로비 판단 사실 읽기(읽기 전용, 로비 재구성 설계 P1).</summary>
        public static readonly SystemGroupKey<ILobbyProgressReader> LobbyProgress = new(nameof(LobbyProgress));

        /// <summary>로비 아티팩트 확인 Job 입구. 도메인 정지는 이 입구가 만든 확인 Job만 한다(로비 재구성 설계 P1).</summary>
        public static readonly SystemGroupKey<ILobbyArtifactChecks> LobbyArtifactChecks = new(nameof(LobbyArtifactChecks));

        /// <summary>덱 편집 가능 시점(C1). 조립부가 같은 인스턴스를 Core 덱 서비스에도 넘깁니다.</summary>
        public static readonly SystemGroupKey<IDeckEditAvailabilityRead> DeckEditAvailability = new(nameof(DeckEditAvailability));

        public static readonly SystemGroupKey<ILifeCycleManager> Manager =
            new(nameof(Manager));
    }
}
