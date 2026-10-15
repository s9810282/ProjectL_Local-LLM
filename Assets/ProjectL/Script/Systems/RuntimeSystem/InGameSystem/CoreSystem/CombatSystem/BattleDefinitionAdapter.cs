using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
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
            IReadOnlyList<OwnedCharacterRecord> owned)
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
            var units = unitKeys.OrderBy(key => key).Select(key => ReadUnit(query.UnitTableQuery.GetUnit(key))).ToArray();

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
                    Cells(data.FootprintWidth, data.FootprintHeight), data.MaxHealth, data.IsAttackable, data.AttackPower);
            }).ToArray();

            var environment = StageEnvironmentAdapter.Read(field.ColumnCount, field.RowCount,
                combat.TerrainPresencePlacements, combat.StaticTerrainPlacements, combat.ObjectPlacements,
                wallCells: combat.WallCells);
            var railway = combat.Railway;
            var stage = new StageConfig(field.ColumnCount, field.RowCount, field.ReserveCapacity,
                field.PlayerDeploymentDepth, field.EnemyDeploymentDepth, combat.TimelineSeed,
                combat.InitialEnemyPlacements.Select(p => new InitialEnemyPlacementSpec(p.Key, p.Anchor, p.Rotation)).ToArray(),
                railway?.RailCells.ToArray() ?? Array.Empty<CellCoord>(),
                railway == null ? Array.Empty<StageGimmickSpec>() : new StageGimmickSpec[] {
                    new RailwayGimmickSpec(railway.FirstActivationAfterUnitTurns, railway.RepeatIntervalUnitTurns) },
                combat.NightLength, placements.ToArray());
            if (enemies != null)
            {
                var definitions = units.ToDictionary(unit => unit.Key);
                var candidates = enemies.SpawnCount == 0 ? Array.Empty<EnemySpawnCandidate>() :
                    enemies.Entries.Select(entry => new EnemySpawnCandidate(definitions[entry.UnitKey], entry.Weight)).ToArray();
                var generated = EnemySpawnCalculator.Calculate(stage, candidates,
                    enemies.SpawnCount, enemies.RandomSeed, structures, environment.Environment, environment.Bindings);
                stage = new StageConfig(stage.MapWidth, stage.MapHeight, stage.ReserveCapacity,
                    stage.PlayerDeploymentDepth, stage.EnemyDeploymentDepth, stage.TimelineSeed,
                    generated, stage.RailCells.ToArray(), stage.StageGimmicks.ToArray(), stage.NightLength,
                    stage.InitialEnemyStructurePlacements.ToArray());
            }
            var categories = new[]
            {
                new CategorySpec(CategoryKey.Base, Array.Empty<CategoryRuleSpec>()),
                new CategorySpec(CategoryKey.Rider, new CategoryRuleSpec[] { new ManeuverRefundCategoryRuleSpec() }),
                new CategorySpec(CategoryKey.Heavy, new CategoryRuleSpec[] {
                    new DirectionalArmorCategoryRuleSpec(category.HeavyDamageReductionPercent, category.HeavyInitialFacing) }),
                new CategorySpec(CategoryKey.Cannon, new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() })
            };
            // 공격 프로필은 여기서 만들지 않는다. 지정 방식 테이블이 없는 개발용 DB도 이 변환을 쓰므로 ReadAttackProfiles로 분리했다.
            return new BattleInitialization(stage, combat.MainCharacterAnchor, combat.MainCharacterRotation,
                units, structures, categories, economy: new StageEconomySpec(economy.InitialGold, economy.ManualDrawGoldCost),
                environment: environment.Environment, environmentBindings: environment.Bindings,
                obstaclePlacements: StageEnvironmentAdapter.ReadObstacles(combat.ObstaclePlacements), wallBlocking: combat.WallBlocking);
        }

        /// <summary>
        /// 유닛이 참조하는 공격 지정 방식(TargetingMethod)으로 Core 공격 프로필을 전투 유닛마다 하나씩 만든다.
        /// Core는 배치·피격·턴 처리에서도 유닛 정의를 공격 프로필로 조회하므로 모든 전투 유닛에 프로필이 있어야 한다.
        /// 그래서 공격하지 않는 유닛(지정 방식 키 0)은 전투 설치 전에 거절한다(정의 조회와 공격 가능 여부 분리는 후속 과제).
        /// 지정 방식 값의 해석과 특성·지정 방식 결합은 Core 계약(TargetingMethodSpec·CombatAttackProfile)이 검사한다.
        /// </summary>
        internal static IReadOnlyList<CombatAttackProfile> ReadAttackProfiles(IDataBaseQueryContext database, IReadOnlyList<UnitSpec> units)
        {
            var query = database?.InGameQuery ?? throw new ArgumentNullException(nameof(database));
            if (units == null) throw new ArgumentNullException(nameof(units));
            var profiles = new List<CombatAttackProfile>();
            foreach (UnitSpec unit in units)
            {
                UnitData data = query.UnitTableQuery.GetUnit(unit.Key);
                if (data.TargetingMethodKey == 0)
                    throw new InvalidOperationException(
                        $"공격하지 않는 유닛(지정 방식 키 0)은 아직 전투에 넣을 수 없습니다: {unit.Key}.");
                var method = query.TargetingMethodTableQuery.GetTargetingMethod(data.TargetingMethodKey);
                method.Validate();
                try
                {
                    profiles.Add(new CombatAttackProfile(unit,
                        new TargetingMethodSpec(method.Shape, method.ChoiceMode, method.Splash, method.SplashPercent)));
                }
                catch (ArgumentException exception)
                {
                    throw new InvalidOperationException(
                        $"DB 공격 지정 방식을 Core 공격 프로필로 바꿀 수 없습니다: {unit.Key}, Method {data.TargetingMethodKey}.", exception);
                }
            }
            return profiles.AsReadOnly();
        }

        internal static UnitSpec ReadUnit(UnitData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            data.Validate();
            var traits = new List<TraitSpec>();
            foreach (var trait in data.Traits)
            {
                traits.Add(trait switch
                {
                    FocusTraitData t => new FocusTraitSpec(t.DamagePerStack, t.MaxStacks),
                    MoveAttackDiscountTraitData t => new MoveAttackDiscountTraitSpec(t.DiscountAmount, t.MinimumAttackCost),
                    PiercingFalloffTraitData t => new PiercingFalloffTraitSpec(t.ReductionPercentPerTarget, t.MaxTargetsPerRay),
                    ThornFieldTraitData t => new ThornFieldTraitSpec(t.ThornDamage, t.LifetimeUnitTurns),
                    KnockbackTraitData t => new KnockbackTraitSpec(t.DistanceCells),
                    DistanceDamageScaleTraitData t => new DistanceDamageScaleTraitSpec(t.PercentByDistance),
                    KillChainAttackTraitData => new KillChainAttackTraitSpec(),
                    _ => throw new InvalidOperationException($"DB 특성 정의를 변환할 수 없습니다: {data.Key}.")
                });
            }
            return new UnitSpec(data.Key, data.Role, data.FootprintWidth, data.FootprintHeight,
                Cells(data.FootprintWidth, data.FootprintHeight), data.MaxHealth, MoveMethod.LiftAndPlace,
                data.MoveRange, data.MoveCost, data.AttackRange, data.Category,
                data.AttackPower, data.MaxActionCost, data.AttackCost, data.PhysicalDefense,
                data.Priority, data.Delay, true, traits, data.OwnerEligibility, data.Occlusion);
        }

        private static CellCoord[] Cells(int width, int height)
        {
            var cells = new CellCoord[checked(width * height)];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++) cells[y * width + x] = new CellCoord(x, y);
            return cells;
        }
    }
}
