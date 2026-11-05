using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;

namespace ProjectL.Systems.ServiceKeys.DBFactory.Group
{
    internal static class InGameTableKey
    {
        public static readonly SystemGroupKey<ICategoryTraitProgressionTableQuery> CategoryTraitProgression = new(nameof(CategoryTraitProgression));
        public static readonly SystemGroupKey<ITraitGrowthDefinitionTableQuery> TraitGrowthDefinition = new(nameof(TraitGrowthDefinition));
        public static readonly SystemGroupKey<IEnhancementTableQuery> Enhancement = new(nameof(Enhancement));
        public static readonly SystemGroupKey<IConsumableItemTableQuery> ConsumableItem = new(nameof(ConsumableItem));

        public static readonly SystemGroupKey<IStageMapTableQuery> StageMap =
            new(nameof(StageMap));

        public static readonly SystemGroupKey<IStageTableQuery> Stage =
            new(nameof(Stage));

        public static readonly SystemGroupKey<IFieldTableQuery> Field =
            new(nameof(Field));

        public static readonly SystemGroupKey<IUnitTableQuery> Unit =
            new(nameof(Unit));

        public static readonly SystemGroupKey<ITargetingMethodTableQuery> TargetingMethod =
            new(nameof(TargetingMethod));

        public static readonly SystemGroupKey<IStructureTableQuery> Structure =
            new(nameof(Structure));

        public static readonly SystemGroupKey<IEnemyLoadoutTableQuery> EnemyLoadout =
            new(nameof(EnemyLoadout));

        public static readonly SystemGroupKey<IPlayerLoadoutTableQuery> PlayerLoadout =
            new(nameof(PlayerLoadout));

        public static readonly SystemGroupKey<IUnitRosterTableQuery> UnitRoster =
            new(nameof(UnitRoster));

        public static readonly SystemGroupKey<IArtifactDefinitionTableQuery> ArtifactDefinition =
            new(nameof(ArtifactDefinition));

        public static readonly SystemGroupKey<ICategoryConfigTableQuery> CategoryConfig =
            new(nameof(CategoryConfig));

        public static readonly SystemGroupKey<IArtifactBoardConfigTableQuery> ArtifactBoardConfig =
            new(nameof(ArtifactBoardConfig));

        public static readonly SystemGroupKey<IStageEconomyTableQuery> StageEconomy =
            new(nameof(StageEconomy));

        public static readonly SystemGroupKey<IRecoveryOptionTableQuery> RecoveryOption =
            new(nameof(RecoveryOption));
    }
}
