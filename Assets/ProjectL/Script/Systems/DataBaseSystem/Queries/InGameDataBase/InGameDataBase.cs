using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;

namespace ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase
{
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

    internal sealed class InGameDataBase : SystemGroup, IInGameQuery, IHasArtifactBoardConfig
    {
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

        public InGameDataBase(ISystemReader systemReader) : base(systemReader)
        {
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
        }
    }

    #endregion
}
