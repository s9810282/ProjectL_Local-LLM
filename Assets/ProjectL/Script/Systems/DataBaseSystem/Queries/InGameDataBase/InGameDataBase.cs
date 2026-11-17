using ProjectL.Systems.ServiceKeys.DBFactory.Group;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase
{
    internal interface IHasTraitGrowthTables : IServiceModule
    {
        ICategoryTraitProgressionTableQuery CategoryTraitProgressions { get; }
        ITraitGrowthDefinitionTableQuery TraitGrowthDefinitions { get; }
    }

    /// <summary>적 AI 기획 조정 테이블입니다. 등록하지 않으면 없으며, 쓰는 쪽은 Core 기본값을 씁니다.</summary>
    internal interface IHasEnemyAiTuningTable : IServiceModule
    {
        IEnemyAiTuningTableQuery EnemyAiTuning { get; }
    }

    internal interface IHasEnhancementTable : IServiceModule
    {
        IEnhancementTableQuery Enhancements { get; }
    }

    internal interface IHasConsumableItemTable : IServiceModule
    {
        IConsumableItemTableQuery ConsumableItems { get; }
    }

    internal interface IHasPlayerActiveSkillTable : IServiceModule
    {
        IPlayerActiveSkillTableQuery PlayerActiveSkills { get; }
    }

    #region 서비스

    internal interface IInGameQuery : IServiceModule
    {
        IStageMapTableQuery StageMapTableQuery { get; }
        IStageTableQuery StageTableQuery { get; }
        IFieldTableQuery FieldTableQuery { get; }
        IUnitTableQuery UnitTableQuery { get; }
        ITargetingMethodTableQuery TargetingMethodTableQuery { get; }
        IStructureTableQuery StructureTableQuery { get; }
        IEnemyLoadoutTableQuery EnemyLoadoutTableQuery { get; }
        IPlayerLoadoutTableQuery PlayerLoadoutTableQuery { get; }
        IUnitRosterTableQuery UnitRosterTableQuery { get; }
        IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery { get; }
        ICategoryConfigTableQuery CategoryConfigTableQuery { get; }
        IStageEconomyTableQuery StageEconomyTableQuery { get; }
        IRecoveryOptionTableQuery RecoveryOptionTableQuery { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InGameDataBase : SystemGroup, IInGameQuery, IHasArtifactBoardConfig, IHasConsumableItemTable, IHasEnhancementTable, IHasTraitGrowthTables,
        IHasEnemyAiTuningTable, IHasPlayerActiveSkillTable
    {
        public IEnemyAiTuningTableQuery EnemyAiTuning { get; }
        public IPlayerActiveSkillTableQuery PlayerActiveSkills { get; }
        public ICategoryTraitProgressionTableQuery CategoryTraitProgressions { get; }
        public ITraitGrowthDefinitionTableQuery TraitGrowthDefinitions { get; }
        public IEnhancementTableQuery Enhancements { get; }
        public IConsumableItemTableQuery ConsumableItems { get; }
        public IStageMapTableQuery StageMapTableQuery { get; }
        public IStageTableQuery StageTableQuery { get; }
        public IFieldTableQuery FieldTableQuery { get; }
        public IUnitTableQuery UnitTableQuery { get; }
        public ITargetingMethodTableQuery TargetingMethodTableQuery { get; }
        public IStructureTableQuery StructureTableQuery { get; }
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery { get; }
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery { get; }
        public IUnitRosterTableQuery UnitRosterTableQuery { get; }
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery { get; }
        public ICategoryConfigTableQuery CategoryConfigTableQuery { get; }
        public IStageEconomyTableQuery StageEconomyTableQuery { get; }
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery { get; }
        /// <summary>IInGameQuery와 분리한 설정 조회입니다. 테스트용 InGame 조회 구현체를 바꾸지 않기 위해 따로 노출합니다.</summary>
        public IArtifactBoardConfigTableQuery ArtifactBoardConfig { get; }

        public InGameDataBase(ISystemReader systemReader, bool hasEnhancements = false, bool hasTraitGrowth = false,
            bool hasEnemyAiTuning = false, bool hasPlayerActiveSkills = false) : base(systemReader)
        {
            EnemyAiTuning = hasEnemyAiTuning ? systemReader.ServiceStoreReader.GetService(InGameTableKey.EnemyAiTuning) : null;
            PlayerActiveSkills = hasPlayerActiveSkills ? systemReader.ServiceStoreReader.GetService(InGameTableKey.PlayerActiveSkill) : null;
            CategoryTraitProgressions = hasTraitGrowth ? systemReader.ServiceStoreReader.GetService(InGameTableKey.CategoryTraitProgression) : null;
            TraitGrowthDefinitions = hasTraitGrowth ? systemReader.ServiceStoreReader.GetService(InGameTableKey.TraitGrowthDefinition) : null;
            Enhancements = hasEnhancements ? systemReader.ServiceStoreReader.GetService(InGameTableKey.Enhancement) : null;
            ConsumableItems = systemReader.ServiceStoreReader.GetService(InGameTableKey.ConsumableItem);
            StageMapTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.StageMap);
            StageTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.Stage);
            FieldTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.Field);
            UnitTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.Unit);
            TargetingMethodTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.TargetingMethod);
            StructureTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.Structure);
            EnemyLoadoutTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.EnemyLoadout);
            PlayerLoadoutTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.PlayerLoadout);
            UnitRosterTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.UnitRoster);
            ArtifactDefinitionTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.ArtifactDefinition);
            CategoryConfigTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.CategoryConfig);
            StageEconomyTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.StageEconomy);
            RecoveryOptionTableQuery = systemReader.ServiceStoreReader.GetService(InGameTableKey.RecoveryOption);
            ArtifactBoardConfig = systemReader.ServiceStoreReader.GetService(InGameTableKey.ArtifactBoardConfig);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IInGameQuery>(this);
            sink.AddService<IHasArtifactBoardConfig>(this);
            sink.AddService<IHasConsumableItemTable>(this);
            sink.AddService<IHasEnhancementTable>(this);
        }
    }

    #endregion
}
