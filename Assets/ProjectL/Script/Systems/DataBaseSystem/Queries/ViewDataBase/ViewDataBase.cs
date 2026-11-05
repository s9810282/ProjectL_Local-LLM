using ProjectL.Systems.ServiceKeys.DBFactory.Group;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase
{
    /// <summary>ViewGroup 테이블 조회를 묶은 쿼리입니다. View 시스템과 UI가 사용합니다.</summary>
    internal interface IViewQuery : IServiceModule
    {
        IStageViewPropTableQuery StageViewPropTableQuery { get; }
        IStageFieldViewTableQuery StageFieldViewTableQuery { get; }
        IStageMapViewTableQuery StageMapViewTableQuery { get; }
        IStageMapBackgroundTableQuery StageMapBackgroundTableQuery { get; }
        IStageNodeVisualTableQuery StageNodeVisualTableQuery { get; }
        IUnitVisualTableQuery UnitVisualTableQuery { get; }
        IObjectVisualTableQuery ObjectVisualTableQuery { get; }
        ITerrainVisualTableQuery TerrainVisualTableQuery { get; }
        IStageEnvironmentVisualTableQuery StageEnvironmentVisualTableQuery { get; }
        IArtifactViewTableQuery ArtifactViewTableQuery { get; }
        IArtifactPopupConfigTableQuery ArtifactPopupConfigTableQuery { get; }
        ILobbyVisualTableQuery LobbyVisualTableQuery { get; }
        ITraitVisualTableQuery TraitVisualTableQuery { get; }
        ICategoryVisualTableQuery CategoryVisualTableQuery { get; }
        IRecoveryOptionVisualTableQuery RecoveryOptionVisualTableQuery { get; }
        IRailwayVisualTableQuery RailwayVisualTableQuery { get; }
        IThornFieldVisualTableQuery ThornFieldVisualTableQuery { get; }
        IRockfallVisualTableQuery RockfallVisualTableQuery { get; }
        IHaboobVisualTableQuery HaboobVisualTableQuery { get; }
        IInputSettingsTableQuery InputSettingsTableQuery { get; }
        ICombatPresentationSettingsTableQuery CombatPresentationSettingsTableQuery { get; }
        IConsumableItemVisualTableQuery ConsumableItemVisualTableQuery { get; }
        IUnitStatusVisualTableQuery UnitStatusVisualTableQuery { get; }
        IStageOfferTextTableQuery StageOfferTextTableQuery { get; }
        ITutorialDefinitionTableQuery TutorialDefinitionTableQuery { get; }
    }

    internal sealed class ViewDataBase : SystemGroup, IViewQuery
    {
        public IStageViewPropTableQuery StageViewPropTableQuery { get; }
        public IStageFieldViewTableQuery StageFieldViewTableQuery { get; }
        public IStageMapViewTableQuery StageMapViewTableQuery { get; }
        public IStageMapBackgroundTableQuery StageMapBackgroundTableQuery { get; }
        public IStageNodeVisualTableQuery StageNodeVisualTableQuery { get; }
        public IUnitVisualTableQuery UnitVisualTableQuery { get; }
        public IObjectVisualTableQuery ObjectVisualTableQuery { get; }
        public ITerrainVisualTableQuery TerrainVisualTableQuery { get; }
        public IStageEnvironmentVisualTableQuery StageEnvironmentVisualTableQuery { get; }
        public IArtifactViewTableQuery ArtifactViewTableQuery { get; }
        public ITraitVisualTableQuery TraitVisualTableQuery { get; }
        public ICategoryVisualTableQuery CategoryVisualTableQuery { get; }
        public IRecoveryOptionVisualTableQuery RecoveryOptionVisualTableQuery { get; }
        public IRailwayVisualTableQuery RailwayVisualTableQuery { get; }
        public IThornFieldVisualTableQuery ThornFieldVisualTableQuery { get; }
        public IRockfallVisualTableQuery RockfallVisualTableQuery { get; }
        public IHaboobVisualTableQuery HaboobVisualTableQuery { get; }
        public IInputSettingsTableQuery InputSettingsTableQuery { get; }
        public IArtifactPopupConfigTableQuery ArtifactPopupConfigTableQuery { get; }
        public ILobbyVisualTableQuery LobbyVisualTableQuery { get; }
        public ICombatPresentationSettingsTableQuery CombatPresentationSettingsTableQuery { get; }
        public IConsumableItemVisualTableQuery ConsumableItemVisualTableQuery { get; }
        public IUnitStatusVisualTableQuery UnitStatusVisualTableQuery { get; }
        public IStageOfferTextTableQuery StageOfferTextTableQuery { get; }
        public ITutorialDefinitionTableQuery TutorialDefinitionTableQuery { get; }

        public ViewDataBase(ISystemReader systemReader) : base(systemReader)
        {
            StageViewPropTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageViewProp);
            StageFieldViewTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageFieldView);
            StageMapViewTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageMapView);
            StageMapBackgroundTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageMapBackground);
            StageNodeVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageNodeVisual);
            UnitVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.UnitVisual);
            ObjectVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.ObjectVisual);
            TerrainVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.TerrainVisual);
            StageEnvironmentVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageEnvironmentVisual);
            ArtifactViewTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.ArtifactView);
            TraitVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.TraitVisual);
            CategoryVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.CategoryVisual);
            RecoveryOptionVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.RecoveryOptionVisual);
            RailwayVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.RailwayVisual);
            ThornFieldVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.ThornFieldVisual);
            RockfallVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.RockfallVisual);
            HaboobVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.HaboobVisual);
            InputSettingsTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.InputSettings);
            ArtifactPopupConfigTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.ArtifactPopupConfig);
            LobbyVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.LobbyVisual);
            CombatPresentationSettingsTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.CombatPresentationSettings);
            ConsumableItemVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.ConsumableItemVisual);
            UnitStatusVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.UnitStatusVisual);
            StageOfferTextTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageOfferText);
            TutorialDefinitionTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.TutorialDefinition);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IViewQuery>(this);
        }
    }
}
