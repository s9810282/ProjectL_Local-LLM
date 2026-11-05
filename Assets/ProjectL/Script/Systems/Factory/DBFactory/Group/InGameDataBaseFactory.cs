using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item;
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
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
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

            var items = new ConsumableItemTable(dataBaseGroup.GetDataBase<ConsumableItemDataTableSO>());
            items.ExportServices(builder);
            // 콘텐츠 등록 전에는 선택 기능을 제공하지 않는다. 강화 진입은 누락을 명시적으로 거부한다.
            var enhancementSource = data.GetAllData().OfType<EnhancementDataTableSO>().SingleOrDefault();
            if (enhancementSource != null) new EnhancementTable(enhancementSource).ExportServices(builder);
            bool hasTraitGrowth = RegisterTraitGrowth(data, builder, unitTable, targetingMethodTable, categoryConfigTable.Config.DefaultTraits);
            return new InGameDataBase(builder.BuildSystemReader(), enhancementSource != null, hasTraitGrowth);
        }

        private static bool RegisterTraitGrowth(InGameGroup data, ISystemWriteSink builder,
            IUnitTableQuery units, ITargetingMethodTableQuery methods, IReadOnlyDictionary<CategoryKey, TraitKey> defaults)
        {
            var categorySource = data.GetAllData().OfType<CategoryTraitProgressionDataTableSO>().SingleOrDefault();
            var definitionSource = data.GetAllData().OfType<TraitGrowthDefinitionDataTableSO>().SingleOrDefault();
            if (categorySource == null && definitionSource == null && defaults.Count == 0) return false;
            if (categorySource == null || definitionSource == null)
                throw new InvalidOperationException("성장 병과·특성 테이블은 함께 등록해야 합니다.");
            var categories = new CategoryTraitProgressionTable(categorySource);
            var definitions = new TraitGrowthDefinitionTable(definitionSource);
            var catalog = TraitGrowthDefinitionAdapter.Build(categories, definitions, defaults);
            var profiles = new List<CombatAttackProfile>();
            foreach (var row in units.GetAllUnits())
            {
                bool hasGrowth = row.OwnerEligibility != UnitOwnerEligibility.EnemyOnly &&
                    catalog.Categories.Any(c => c.Category == row.Category && c.Steps.Count > 0);
                bool hasDefault = defaults.ContainsKey(row.Category) || row.Role == UnitRole.MainCharacter &&
                    (defaults.ContainsKey(CategoryKey.Rider) || defaults.ContainsKey(CategoryKey.Heavy) || defaults.ContainsKey(CategoryKey.Cannon));
                if (!hasGrowth && !hasDefault) continue;
                if (!methods.TryGetTargetingMethod(row.TargetingMethodKey, out var method))
                    throw new InvalidOperationException($"성장 유닛의 공격 지정 방식이 없습니다: {row.Key}, Method={row.TargetingMethodKey}.");
                try
                {
                    profiles.Add(new CombatAttackProfile(UnitSpecAdapter.ReadUnit(row), UnitSpecAdapter.ReadMethod(method)));
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException($"성장 유닛의 공격 프로필이 올바르지 않습니다: {row.Key}.", exception);
                }
            }
            TraitGrowthCatalogValidator.Validate(catalog, profiles);
            categories.ExportServices(builder);
            definitions.ExportServices(builder);
            return true;
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
