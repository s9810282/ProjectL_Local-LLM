using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ItemSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.Development;
#endif

namespace ProjectL.Systems.Factory.Runtime.InGame.Core
{
    /// <summary>인게임 규칙·데이터 처리 Core 그룹을 생성합니다.</summary>
    #region 시스템 모듈

    internal static class InGameCoreSystemFactory
    {
        /// <param name="deckEditAvailability">
        /// Stage 흐름 소유자가 구현하는 덱 편집 가능 시점 읽기다. 조립부가 생성 때 넘긴다.
        /// 아직 공급자가 없는 조립(운영 C1 미연결·다른 기능 테스트)은 생략하며, 이때 덱 편집은 AuthorityUnbound로 막힌다.
        /// </param>
        internal static IExportSystemServices Create(IBattleInitializationSource battleSource = null,
            IChapterContinuationSource continuationSource = null, IDeckEditAvailabilityRead deckEditAvailability = null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , bool includeDevelopmentTools = false
#endif
            , bool usePreparedEnemyAdvance = false
            )
        {
            SystemBuilderHelper builder = new();

            var stageGenerator = new StageGeneratorSystem();
            stageGenerator.ExportServices(builder);
            var stageService = new StageServiceManager();
            stageService.ExportServices(builder);
            var characters = new CharacterRosterService();
            characters.ExportServices(builder);
            var progression = new ChapterProgressionCoordinator(stageGenerator, stageService, characters, continuationSource);
            progression.ExportServices(builder);
            new ItemInventoryService().ExportServices(builder);
            new TraitGrowthCommandProcessingService(characters).ExportServices(builder);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentStateService development = null;
            if (includeDevelopmentTools)
            {
                development = new DevelopmentStateService(characters, deckEditAvailability, stageService);
                development.ExportServices(builder);
            }
#endif
            var characterStats = new CharacterStatsQueryService();
            characterStats.ExportServices(builder);
            new RunDeckService(deckEditAvailability).ExportServices(builder);
            var shop = new ShopService(progression);
            shop.ExportServices(builder);
            IArtifactEditCalculation artifactCalculation = new ArtifactEditCalculator();
            new ArtifactEditingService(artifactCalculation).ExportServices(builder);
            // 전투 역할 서비스와 공유 협력 객체의 조립은 전투 전용 Factory가 맡는다.
            IBattleEntryService battleEntry = CombatSystemFactory.Register(builder, battleSource, characters, progression, usePreparedEnemyAdvance);
            var recoveryProjection = new RecoveryStateProjection();
            var recovery = new RecoveryStageService(progression, recoveryProjection);
            recovery.ExportServices(builder);
            new StageCommandProcessingService(progression, shop, battleEntry, recovery, new ShopCommandExecutor(shop)).ExportServices(builder);
            new RecoveryCommandProcessingService(recovery, recoveryProjection).ExportServices(builder);
            new RecoveryStateQueryService(recoveryProjection).ExportServices(builder);
            new RunStartService().ExportServices(builder);
            new RunAbandonService().ExportServices(builder);
            new ResultQueryService().ExportServices(builder);
            new CharacterPresentationPublisher(characterStats).ExportServices(builder);

            return new InGameCoreSystemGroup(builder.BuildSystemReader()
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , development
#endif
                );
        }
    }

    #endregion
}
