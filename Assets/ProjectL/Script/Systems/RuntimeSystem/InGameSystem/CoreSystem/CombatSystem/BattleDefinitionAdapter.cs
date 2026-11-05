using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>기존 DB 사실만 Contracts로 변환한다. 상태 생성·저장은 담당하지 않는다. 공격 프로필은 ReadAttackProfiles가 따로 만든다.</summary>
    internal static class BattleDefinitionAdapter
    {
        internal static BattleInitialization Read(IDataBaseQueryContext database, int mapKey, int stageKey,
            IReadOnlyList<OwnedCharacterRecord> owned, IReadOnlyList<ItemDefinition> itemDefinitions = null)
        {
            if (mapKey < 1 || stageKey < 1)
                throw new InvalidOperationException("G7: 현재 챕터와 스테이지 선택이 필요합니다.");
            var query = database?.InGameQuery ?? throw new ArgumentNullException(nameof(database));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            var root = query.StageTableQuery.GetStage(stageKey);
            root.Validate();
            if (root.StageKey != stageKey || root.MapKey != mapKey || root.StageData is not CombatStageDetail combat)
                throw new InvalidOperationException("G7: 선택한 챕터의 전투 스테이지 정의가 아닙니다.");
            var field = query.FieldTableQuery.GetField(combat.FieldId);
            field.Validate();
            var roster = query.UnitRosterTableQuery.GetUnitRoster(combat.UnitRosterKey);
            roster.Validate();
            var economy = query.StageEconomyTableQuery.GetStageEconomy(combat.EconomyKey);
            economy.Validate();
            if (economy.ClearRewards == null)
                throw new InvalidOperationException("G7: 클리어 보상 정의가 없습니다.");
            var category = query.CategoryConfigTableQuery.Config;
            category.Validate();

            EnemyLoadoutData enemies = null;
            if (combat.EnemyLoadoutKey != 0)
            {
                if (combat.InitialEnemyPlacements.Count != 0)
                    throw new InvalidOperationException("직접 저작한 적 배치와 EnemyLoadout은 동시에 사용할 수 없습니다.");
                enemies = query.EnemyLoadoutTableQuery.GetEnemyLoadout(combat.EnemyLoadoutKey);
                enemies.Validate();
                // 기존 스테이지 로스터 소속 조건을 소유 카드와 합친 목록으로 완화하지 않는다.
                if (enemies.SpawnCount > 0)
                    foreach (EnemyLoadoutEntry entry in enemies.Entries)
                        if (!roster.UnitKeys.Contains(entry.UnitKey))
                            throw new InvalidOperationException("EnemyLoadout 후보가 스테이지 UnitRoster에 없습니다.");
            }

            var unitKeys = new HashSet<EntityKey>(roster.UnitKeys);
            foreach (var card in owned) unitKeys.Add(card.CharacterKey);
            foreach (var placement in combat.InitialEnemyPlacements) unitKeys.Add(placement.Key);
            var growth = query as IHasTraitGrowthTables;
            var catalog = TraitGrowthDefinitionAdapter.Build(growth?.CategoryTraitProgressions, growth?.TraitGrowthDefinitions, category.DefaultTraits);
            var units = ReadUnits(query.UnitTableQuery, unitKeys, owned, catalog);

            var placements = new List<InitialEnemyStructurePlacementSpec>();
            if (combat.HeadquartersKey != EntityKey.None)
                placements.Add(new InitialEnemyStructurePlacementSpec(combat.HeadquartersKey,
                    combat.EnemyHeadquartersAnchor, combat.EnemyHeadquartersRotation));
            placements.AddRange(combat.InitialEnemyStructurePlacements.Select(p =>
                new InitialEnemyStructurePlacementSpec(p.Key, p.Anchor, p.Rotation)));
            var structureKeys = new HashSet<EntityKey>(combat.StructureCatalog);
            foreach (var placement in placements) structureKeys.Add(placement.StructureKey);
            var structures = structureKeys.OrderBy(key => key).Select(key =>
            {
                var data = query.StructureTableQuery.GetStructure(key);
                data.Validate();
                return new StructureSpec(data.Key, data.FootprintWidth, data.FootprintHeight,
                    UnitSpecAdapter.FootprintCells(data.FootprintWidth, data.FootprintHeight), data.MaxHealth, data.IsAttackable, data.AttackPower);
            }).ToArray();

            // 최초 무작위 토네이도의 칸은 전투 준비가 정한다. 여기서는 ID만 예약하고 명세를 그대로 넘긴다.
            var environment = StageEnvironmentAdapter.Read(field.ColumnCount, field.RowCount,
                combat.TerrainPresencePlacements, combat.StaticTerrainPlacements, combat.ObjectPlacements,
                tornadoPlacements: combat.Tornado == null ? combat.TornadoPlacements : Array.Empty<TornadoSourceFact>(), wallCells: combat.WallCells,
                reservedSourceIds: combat.Tornado?.Members.Select(p => p.SourceId).ToArray() ?? combat.InitialRandomTornadoPlacements.Select(p => p.SourceId).ToArray());
            var obstacles = StageEnvironmentAdapter.ReadObstacles(combat.ObstaclePlacements);
            var railway = combat.Railway;
            var gimmicks = new List<StageGimmickSpec>();
            if (railway != null) gimmicks.Add(new RailwayGimmickSpec(railway.FirstActivationAfterControllerTurns, railway.RepeatIntervalControllerTurns));
            var rockfall = combat.Rockfall;
            var haboob = combat.Haboob;
            if (haboob != null) gimmicks.Add(new HaboobGimmickSpec(haboob.RelocationIntervalControllerTurns,
                haboob.WarningLeadControllerTurns, haboob.FootprintOffsets, haboob.InitialAnchor));
            if (rockfall != null) gimmicks.Add(new RockfallGimmickSpec(rockfall.CooldownControllerTurns, rockfall.WarningControllerTurns,
                rockfall.MinimumRockCount, rockfall.MaximumRockCount, rockfall.MaximumHealthLossPercent,
                rockfall.BindAffectedSideTurns, rockfall.FootprintOffsets));
            // 아군 배치 구역의 대체 규칙 원본: 스테이지 칸 목록이 있으면 그 칸, 비었으면 필드의 아군 줄 수(아래부터)다.
            if (combat.Tornado != null) gimmicks.Add(combat.Tornado);
            var playerZone = combat.PlayerDeploymentCells.Count > 0
                ? PlayerDeploymentZone.Of(combat.PlayerDeploymentCells)
                : PlayerDeploymentZone.Rows(field.PlayerDeploymentDepth);
            var stage = new StageConfig(field.ColumnCount, field.RowCount, field.ReserveCapacity,
                playerZone, combat.TimelineSeed,
                combat.InitialEnemyPlacements.Select(p => new InitialEnemyPlacementSpec(p.Key, p.Anchor, p.Rotation,
                    p.IsVictoryTarget, p.StartingItemKey, combat.EnemyLevel)).ToArray(),
                railway?.RailCells.ToArray() ?? Array.Empty<CellCoord>(),
                gimmicks.ToArray(),
                placements.ToArray());
            if (enemies != null)
            {
                var definitions = units.ToDictionary(unit => unit.Key);
                var candidates = enemies.SpawnCount == 0 ? Array.Empty<EnemySpawnCandidate>() :
                    enemies.Entries.Select(entry => new EnemySpawnCandidate(definitions[entry.UnitKey], entry.Weight)).ToArray();
                var generated = EnemySpawnCalculator.Calculate(stage, candidates,
                    enemies.SpawnCount, enemies.RandomSeed, structures, environment.Environment, environment.Bindings, obstacles,
                    combat.EnemyLevel);
                stage = new StageConfig(stage.MapWidth, stage.MapHeight, stage.ReserveCapacity,
                    stage.PlayerZone, stage.TimelineSeed,
                    generated, stage.RailCells.ToArray(), stage.StageGimmicks.ToArray(),
                    stage.InitialEnemyStructurePlacements.ToArray());
            }
            var categories = UnitSpecAdapter.ReadCategories(category);
            // 공격 프로필은 여기서 만들지 않는다. 지정 방식 테이블이 없는 개발용 DB도 이 변환을 쓰므로 ReadAttackProfiles로 분리했다.
            return new BattleInitialization(stage, combat.MainCharacterAnchor, combat.MainCharacterRotation,
                units, structures, categories,
                environment: environment.Environment, environmentBindings: environment.Bindings,
                obstaclePlacements: obstacles, wallBlocking: combat.WallBlocking, objectiveKind: combat.ObjectiveKind, itemDefinitions: itemDefinitions,
                progression: category.LevelProgression, initialRandomTornadoPlacements: combat.Tornado == null ? combat.InitialRandomTornadoPlacements : null,
                clearRewards: economy.ClearRewards, traitGrowth: catalog);
        }

        /// <summary>
        /// 유닛이 참조하는 공격 지정 방식(TargetingMethod)으로 Core 공격 프로필을 전투 유닛마다 하나씩 만든다.
        /// Core는 배치·피격·턴 처리에서도 유닛 정의를 공격 프로필로 조회하므로 모든 전투 유닛에 프로필이 있어야 한다.
        /// 유닛 하나의 읽기 규칙은 <see cref="UnitSpecAdapter.ReadProfile"/>이 정하고, 보유 능력치 조회도 같은 함수를 쓴다.
        /// </summary>
        internal static IReadOnlyList<CombatAttackProfile> ReadAttackProfiles(IDataBaseQueryContext database, IReadOnlyList<UnitSpec> units)
        {
            var query = database?.InGameQuery ?? throw new ArgumentNullException(nameof(database));
            if (units == null) throw new ArgumentNullException(nameof(units));
            var profiles = new List<CombatAttackProfile>();
            foreach (UnitSpec unit in units)
                profiles.Add(UnitSpecAdapter.ReadProfile(query, unit));
            return profiles.AsReadOnly();
        }

        /// <summary>전투에서 참조하는 유닛과 소환 대상 정의만 수집합니다. 실제 배치나 소유 상태는 바꾸지 않습니다.</summary>
        private static UnitSpec[] ReadUnits(IUnitTableQuery unitTable, IEnumerable<EntityKey> roots,
            IReadOnlyList<OwnedCharacterRecord> owned, TraitGrowthCatalog catalog)
        {
            var variants = owned.ToLookup(record => record.CharacterKey);
            var pending = new Queue<EntityKey>(roots.Distinct().OrderBy(key => key));
            var visited = new HashSet<EntityKey>();
            var units = new List<UnitSpec>();
            while (pending.Count > 0)
            {
                var key = pending.Dequeue();
                if (!visited.Add(key)) continue;
                var unit = UnitSpecAdapter.ReadUnit(unitTable.GetUnit(key));
                if (unit.OwnerEligibility != UnitOwnerEligibility.PlayerOnly && unit.KillReward == null)
                    throw new InvalidOperationException($"G7: 적 유닛의 처치 보상 정의가 없습니다: {key}.");
                units.Add(unit);
                EnqueueSummons(OwnedUnitSpecResolver.ResolveBase(unit, catalog));
                foreach (var record in variants[key])
                    if (!record.Growth.IsEmpty || record.SpecializationCategory.HasValue)
                        EnqueueSummons(OwnedUnitSpecResolver.Resolve(unit, record, catalog));
            }
            return units.OrderBy(unit => unit.Key).ToArray();

            void EnqueueSummons(UnitSpec effective)
            {
                foreach (var trait in effective.Traits.Span)
                    if (trait is NecromancyTraitSpec summon)
                        pending.Enqueue(summon.SummonedUnitKey);
            }
        }
    }
}
