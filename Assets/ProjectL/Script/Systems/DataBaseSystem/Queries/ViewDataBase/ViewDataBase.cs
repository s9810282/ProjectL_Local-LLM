using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.ServiceKeys;
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
        IStageNodeVisualTableQuery StageNodeVisualTableQuery { get; }
        IUnitVisualTableQuery UnitVisualTableQuery { get; }
        IArtifactViewTableQuery ArtifactViewTableQuery { get; }
        ITraitVisualTableQuery TraitVisualTableQuery { get; }
        IRecoveryOptionVisualTableQuery RecoveryOptionVisualTableQuery { get; }
        IRailwayVisualTableQuery RailwayVisualTableQuery { get; }
        IInputSettingsTableQuery InputSettingsTableQuery { get; }
    }

    internal interface IHasArtifactPopupConfig
    {
        IArtifactPopupConfigTableQuery ArtifactPopupConfig { get; }
    }

    internal sealed class ViewDataBase : SystemGroup, IViewQuery, IHasArtifactPopupConfig
    {
        public IStageViewPropTableQuery StageViewPropTableQuery { get; }
        public IStageFieldViewTableQuery StageFieldViewTableQuery { get; }
        public IStageMapViewTableQuery StageMapViewTableQuery { get; }
        public IStageNodeVisualTableQuery StageNodeVisualTableQuery { get; }
        public IUnitVisualTableQuery UnitVisualTableQuery { get; }
        public IArtifactViewTableQuery ArtifactViewTableQuery { get; }
        public ITraitVisualTableQuery TraitVisualTableQuery { get; }
        public IRecoveryOptionVisualTableQuery RecoveryOptionVisualTableQuery { get; }
        public IRailwayVisualTableQuery RailwayVisualTableQuery { get; }
        public IInputSettingsTableQuery InputSettingsTableQuery { get; }
        public IArtifactPopupConfigTableQuery ArtifactPopupConfig { get; }

        public ViewDataBase(ISystemReader systemReader) : base(systemReader)
        {
            StageViewPropTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageViewProp);
            StageFieldViewTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageFieldView);
            StageMapViewTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageMapView);
            StageNodeVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.StageNodeVisual);
            UnitVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.UnitVisual);
            ArtifactViewTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.ArtifactView);
            TraitVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.TraitVisual);
            RecoveryOptionVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.RecoveryOptionVisual);
            RailwayVisualTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.RailwayVisual);
            InputSettingsTableQuery = systemReader.ServiceStoreReader.GetService(ViewTableKey.InputSettings);
            ArtifactPopupConfig = systemReader.ServiceStoreReader.GetService(ViewTableKey.ArtifactPopupConfig);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IViewQuery>(this);
        }
    }
}
