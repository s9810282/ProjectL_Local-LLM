using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.InGame.Core
{
    internal static class InGameCoreSystemServiceKey
    {
        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ItemSystem.IItemInventoryService> Items = new(nameof(Items));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem.IStageCommandProcessingService> StageCommands = new(nameof(StageCommands));

        public static readonly SystemGroupKey<ICharacterRosterService> Characters = new(nameof(Characters));
        public static readonly SystemGroupKey<ITraitGrowthCommandProcessingService> TraitGrowthCommands = new(nameof(TraitGrowthCommands));
        public static readonly SystemGroupKey<IRunDeckService> Deck = new(nameof(Deck));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.BufferSystems.Character.ICharacterStatsReadSink> CharacterStats = new(nameof(CharacterStats));

        public static readonly SystemGroupKey<IShopService> Shop = new(nameof(Shop));

        public static readonly SystemGroupKey<IRecoveryStageService> Recovery = new(nameof(Recovery));

        public static readonly SystemGroupKey<IRecoveryCommandProcessingService> RecoveryCommands = new(nameof(RecoveryCommands));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery.IRecoveryStateReadSink> RecoveryState = new(nameof(RecoveryState));

        public static readonly SystemGroupKey<IBattleEntryService> BattleEntry = new(nameof(BattleEntry));

        public static readonly SystemGroupKey<ICombatCommandService> CombatCommands = new(nameof(CombatCommands));

        public static readonly SystemGroupKey<ICombatAdvanceService> CombatAdvance = new(nameof(CombatAdvance));

        public static readonly SystemGroupKey<ICombatPreviewService> CombatPreview = new(nameof(CombatPreview));



        public static readonly SystemGroupKey<IBattleCompletionService> BattleCompletion = new(nameof(BattleCompletion));

        public static readonly SystemGroupKey<IArtifactCommandProcessingService> ArtifactCommands = new(nameof(ArtifactCommands));

        public static readonly SystemGroupKey<IArtifactEditingService> Artifacts = new(nameof(Artifacts));

        public static readonly SystemGroupKey<IResultQueryService> Results = new(nameof(Results));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem.IStageGeneratorSystem> StageGenerator =
            new(nameof(StageGenerator));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem.IStageServiceManager> StageServiceManager =
            new(nameof(StageServiceManager));

        public static readonly SystemGroupKey<IChapterProgressionCoordinator> ChapterProgression =
            new(nameof(ChapterProgression));

        public static readonly SystemGroupKey<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem.IRunStartService> RunStart =
            new(nameof(RunStart));
    }
}
