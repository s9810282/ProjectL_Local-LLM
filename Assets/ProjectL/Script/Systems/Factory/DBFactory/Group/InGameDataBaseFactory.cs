using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.DBFactory.Group
{
    #region 시스템 모듈

    public static class InGameDataBaseFactory
    {
        internal static IExportSystemServices Create(InGameGroup data)
        {
            DataBaseRegistry<IDatabaseTable> dataBaseGroup = new(data);
            SystemBuilderHelper builder = new();

            var stageMapTable = new StageMapTable(
                dataBaseGroup.GetDataBase<StageMapDataTableSO>());
            stageMapTable.ExportServices(builder);

            var stageTable = new StageTable(
                dataBaseGroup.GetDataBase<StageRootDataTableSO>());
            stageTable.ExportServices(builder);

            var fieldTable = new FieldTable(dataBaseGroup.GetDataBase<FieldDataTableSO>());
            fieldTable.ExportServices(builder);

            var targetingMethodTable = new TargetingMethodTable(dataBaseGroup.GetDataBase<TargetingMethodDataTableSO>());
            targetingMethodTable.ExportServices(builder);

            var unitTable = new UnitTable(dataBaseGroup.GetDataBase<UnitDataTableSO>());
            ValidateTargetingReferences(unitTable, targetingMethodTable);
            unitTable.ExportServices(builder);

            var structureTable = new StructureTable(dataBaseGroup.GetDataBase<StructureDataTableSO>());
            structureTable.ExportServices(builder);

            var enemyLoadoutTable = new EnemyLoadoutTable(dataBaseGroup.GetDataBase<EnemyLoadoutDataTableSO>());
            enemyLoadoutTable.ExportServices(builder);

            var playerLoadoutTable = new PlayerLoadoutTable(dataBaseGroup.GetDataBase<PlayerLoadoutDataTableSO>());
            playerLoadoutTable.ExportServices(builder);

            var unitRosterTable = new UnitRosterTable(dataBaseGroup.GetDataBase<UnitRosterDataTableSO>());
            unitRosterTable.ExportServices(builder);

            var artifactDefinitionTable = new ArtifactDefinitionTable(dataBaseGroup.GetDataBase<ArtifactDefinitionDataTableSO>());
            artifactDefinitionTable.ExportServices(builder);

            var categoryConfigTable = new CategoryConfigTable(dataBaseGroup.GetDataBase<CategoryConfigDataTableSO>());
            categoryConfigTable.ExportServices(builder);

            var artifactBoardConfigTable = new ArtifactBoardConfigTable(dataBaseGroup.GetDataBase<ArtifactBoardConfigDataTableSO>());
            artifactBoardConfigTable.ExportServices(builder);

            var stageEconomyTable = new StageEconomyTable(dataBaseGroup.GetDataBase<StageEconomyDataTableSO>());
            stageEconomyTable.ExportServices(builder);

            var recoveryOptionTable = new RecoveryOptionTable(dataBaseGroup.GetDataBase<RecoveryOptionDataTableSO>());
            recoveryOptionTable.ExportServices(builder);

            return new InGameDataBase(builder.BuildSystemReader());
        }

        // 테이블 자체는 서로 독립적으로 읽고, 그룹 조립 경계에서 참조 무결성을 한 번 검사합니다.
        private static void ValidateTargetingReferences(IUnitTableQuery units, ITargetingMethodTableQuery methods)
        {
            foreach (var unit in units.GetAllUnits())
            {
                if (unit.TargetingMethodKey != 0 && !methods.TryGetTargetingMethod(unit.TargetingMethodKey, out _))
                    throw new InvalidOperationException(
                        $"[InGameDataBaseFactory] Unit {unit.Key} references missing targeting method {unit.TargetingMethodKey}.");
            }
        }
    }

    #endregion
}
