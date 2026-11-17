using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Object;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ViewStage;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.ConsumableItem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Haboob;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.InputSettings;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.UnitStatus;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Category;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial;
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

            var viewStageTable = new ViewStageTable(
                dataBaseGroup.GetDataBase<ViewStageDataTableSO>());
            viewStageTable.ExportServices(builder);

            var stageViewPropTable = new StageViewPropTable(viewStageTable);
            stageViewPropTable.ExportServices(builder);

            var stageFieldViewTable = new StageFieldViewTable(
                dataBaseGroup.GetDataBase<StageFieldViewDataTableSO>());
            stageFieldViewTable.ExportServices(builder);

            var stageMapViewTable = new StageMapViewTable(
                dataBaseGroup.GetDataBase<StageMapViewDataTableSO>());
            stageMapViewTable.ExportServices(builder);

            new StageMapBackgroundTable(
                dataBaseGroup.GetDataBase<StageMapBackgroundDataTableSO>()).ExportServices(builder);

            var stageNodeVisualTable = new StageNodeVisualTable(
                dataBaseGroup.GetDataBase<StageNodeVisualDataTableSO>());
            stageNodeVisualTable.ExportServices(builder);

            var unitVisualTable = new UnitVisualTable(
                dataBaseGroup.GetDataBase<UnitVisualDataTableSO>());
            unitVisualTable.ExportServices(builder);

            new ObjectVisualTable(dataBaseGroup.GetDataBase<ObjectVisualDataTableSO>()).ExportServices(builder);
            new TerrainVisualTable(dataBaseGroup.GetDataBase<TerrainVisualDataTableSO>()).ExportServices(builder);
            new StageEnvironmentVisualTable(viewStageTable).ExportServices(builder);

            var artifactViewTable = new ArtifactViewTable(
                dataBaseGroup.GetDataBase<ArtifactViewDataTableSO>());
            artifactViewTable.ExportServices(builder);

            new ArtifactPopupConfigTable(dataBaseGroup.GetDataBase<ArtifactPopupConfigDataTableSO>()).ExportServices(builder);

            var traitVisualTable = new TraitVisualTable(
                dataBaseGroup.GetDataBase<TraitVisualDataTableSO>());
            traitVisualTable.ExportServices(builder);
            new CategoryVisualTable(dataBaseGroup.GetDataBase<CategoryVisualDataTableSO>()).ExportServices(builder);

            new ConsumableItemVisualTable(dataBaseGroup.GetDataBase<ConsumableItemVisualDataTableSO>()).ExportServices(builder);
            new UnitStatusVisualTable(dataBaseGroup.GetDataBase<UnitStatusVisualDataTableSO>()).ExportServices(builder);
            new PlayerSkillVisualTable(dataBaseGroup.GetDataBase<PlayerSkillVisualDataTableSO>()).ExportServices(builder);
            new StageOfferTextTable(dataBaseGroup.GetDataBase<StageOfferTextDataTableSO>()).ExportServices(builder);
            new TutorialDefinitionTable(dataBaseGroup.GetDataBase<TutorialDefinitionDataTableSO>()).ExportServices(builder);

            var recoveryOptionVisualTable = new RecoveryOptionVisualTable(
                dataBaseGroup.GetDataBase<RecoveryOptionVisualDataTableSO>());
            recoveryOptionVisualTable.ExportServices(builder);

            var railwayVisualTable = new RailwayVisualTable(
                dataBaseGroup.GetDataBase<RailwayVisualDataTableSO>());
            railwayVisualTable.ExportServices(builder);

            new RockfallVisualTable(dataBaseGroup.GetDataBase<RockfallVisualDataTableSO>()).ExportServices(builder);

            new HaboobVisualTable(dataBaseGroup.GetDataBase<HaboobVisualDataTableSO>()).ExportServices(builder);

            var inputSettingsTable = new InputSettingsTable(
                dataBaseGroup.GetDataBase<InputSettingsDataTableSO>());
            inputSettingsTable.ExportServices(builder);

            var lobbyVisualTable = new LobbyVisualTable(
                dataBaseGroup.GetDataBase<LobbyVisualDataTableSO>());
            lobbyVisualTable.ExportServices(builder);

            var combatPresentationSettingsTable = new CombatPresentationSettingsTable(
                dataBaseGroup.GetDataBase<CombatPresentationSettingsDataTableSO>());
            combatPresentationSettingsTable.ExportServices(builder);

            return new ViewDataBase(builder.BuildSystemReader());
        }
    }
}
