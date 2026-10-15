using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.Factory.Runtime.InGame.Core
{
    /// <summary>인게임 규칙·데이터 처리 Core 그룹을 생성합니다.</summary>
    #region 시스템 모듈

    internal static class InGameCoreSystemFactory
    {
        internal static IExportSystemServices Create(IBattleInitializationSource battleSource = null,
            IChapterContinuationSource continuationSource = null)
        {
            SystemBuilderHelper builder = new();

            var stageGenerator = new StageGeneratorSystem();
            stageGenerator.ExportServices(builder);
            var stageService = new StageServiceManager();
            stageService.ExportServices(builder);
            var progression = new ChapterProgressionCoordinator(stageGenerator, stageService, continuationSource);
            progression.ExportServices(builder);
            var characters = new CharacterRosterService();
            characters.ExportServices(builder);
            new CharacterShopService(progression).ExportServices(builder);
            IArtifactEditCalculation artifactCalculation = new ArtifactEditCalculator();
            new ArtifactEditingService(artifactCalculation).ExportServices(builder);
            if (battleSource == null)
            {
                var databaseSource = new DatabaseBattleInitializationSource();
                builder.AddModule<ISystemModule>(databaseSource);
                battleSource = databaseSource;
            }
            var movementRules = System.Array.AsReadOnly(new IMovementTileRule[] {
                new ThornMovementTileRule(), new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() });
            var combat = new CombatProcessingService(battleSource, characters, movementRules, progression);
            combat.ExportServices(builder);
            new RecoveryStageService(combat, progression).ExportServices(builder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem.RunStartService().ExportServices(builder);

            return new InGameCoreSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
