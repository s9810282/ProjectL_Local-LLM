using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;

namespace ProjectL.Systems.Factory.DBFactory.Group
{
    public static class ViewDataBaseFactory
    {
        internal static IExportSystemServices Create(ViewGroup data)
        {
            DataBaseRegistry<IDatabaseTable> dataBaseGroup = new(data);
            SystemBuilderHelper builder = new();

            var stageViewPropTable = new StageViewPropTable(
                dataBaseGroup.GetDataBase<StageViewPropDataTableSO>());
            stageViewPropTable.ExportServices(builder);

            var stageFieldViewTable = new StageFieldViewTable(
                dataBaseGroup.GetDataBase<StageFieldViewDataTableSO>());
            stageFieldViewTable.ExportServices(builder);

            var stageMapViewTable = new StageMapViewTable(
                dataBaseGroup.GetDataBase<StageMapViewDataTableSO>());
            stageMapViewTable.ExportServices(builder);

            var stageNodeVisualTable = new StageNodeVisualTable(
                dataBaseGroup.GetDataBase<StageNodeVisualDataTableSO>());
            stageNodeVisualTable.ExportServices(builder);

            var unitVisualTable = new UnitVisualTable(
                dataBaseGroup.GetDataBase<UnitVisualDataTableSO>());
            unitVisualTable.ExportServices(builder);

            var artifactViewTable = new ArtifactViewTable(
                dataBaseGroup.GetDataBase<ArtifactViewDataTableSO>());
            artifactViewTable.ExportServices(builder);

            new ArtifactPopupConfigTable(dataBaseGroup.GetDataBase<ArtifactPopupConfigDataTableSO>()).ExportServices(builder);

            var traitVisualTable = new TraitVisualTable(
                dataBaseGroup.GetDataBase<TraitVisualDataTableSO>());
            traitVisualTable.ExportServices(builder);

            var recoveryOptionVisualTable = new RecoveryOptionVisualTable(
                dataBaseGroup.GetDataBase<RecoveryOptionVisualDataTableSO>());
            recoveryOptionVisualTable.ExportServices(builder);

            var railwayVisualTable = new RailwayVisualTable(
                dataBaseGroup.GetDataBase<RailwayVisualDataTableSO>());
            railwayVisualTable.ExportServices(builder);

            var inputSettingsTable = new InputSettingsTable(
                dataBaseGroup.GetDataBase<InputSettingsDataTableSO>());
            inputSettingsTable.ExportServices(builder);

            return new ViewDataBase(builder.BuildSystemReader());
        }
    }
}
