using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class BattlePreparationRuleTests
    {
        [TestCase(0, 1)]
        [TestCase(2, 3)]
        [TestCase(4, 4)]
        [TestCase(6, 4)]
        public void InitialDeal_GuaranteesMainAndCapsSubsAtThree(int subCount, int total)
        {
            var roster = Roster(subCount);
            var draw = new InitialDrawPlan(roster.Skip(1).Select(x => x.Id).ToArray());
            BattlePreparation prepared = Prepare(Initialization(), roster, draw);

            Assert.That(prepared.Characters.Count, Is.EqualTo(total));
            Assert.That(prepared.Characters.Select(x => x.OwnedId.Value),
                Is.EqualTo(roster.Take(total).Select(x => x.Id)));
            Assert.That(prepared.Characters[0].Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(prepared.Characters.Skip(1).All(x => x.Area == CharacterRuntimeArea.Hand), Is.True);
            Assert.That(prepared.Combat.DrawCursor, Is.EqualTo(Math.Min(3, subCount)));
            Assert.That(prepared.CommandState.Allocation.NextEntityId, Is.EqualTo(total + 1));
        }

        [Test]
        public void Preparation_UsesPersistentMainAndPerInstanceSouls()
        {
            var roster = Roster(2);
            var souls = roster.ToDictionary(x => x.Id, x => x.Id.Value == 2 ? 2 : 0);
            souls[roster[0].Id] = 3;
            var main = new MainCharacterPersistentState(4, 12, 7);
            var prepared = BattlePreparationCalculator.Prepare(Initialization(), roster, main, souls,
                new InitialDrawPlan(roster.Skip(1).Select(x => x.Id).ToArray()), Array.Empty<CharacterCombatFacts>());

            Assert.That(prepared.Characters[0].Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(prepared.Characters[0].Health.MaxHealth, Is.EqualTo(15));
            Assert.That(prepared.Characters[0].Stats.EffectiveAttackPower, Is.EqualTo(10));
            Assert.That(prepared.Characters[0].Stats.MoveRange, Is.EqualTo(3));
            Assert.That(prepared.Characters[0].ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(prepared.Characters[1].Health.MaxHealth, Is.EqualTo(12));
            Assert.That(prepared.Characters[2].Health.MaxHealth, Is.EqualTo(10));
            Assert.That(main.CurrentHealth, Is.EqualTo(4));
            Assert.That(souls[roster[1].Id], Is.EqualTo(2));
        }

        [Test]
        public void Preparation_GrowsSubsByOwnedLevelAndEnemiesByPlacementLevel_MainKeepsPersistentStats()
        {
            var roster = new[]
            {
                new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Gravekeeper, CharacterOwnershipRole.Main, 2),
                new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, 3),
                new OwnedCharacterRecord(new CharacterInstanceId(3), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
            };
            var souls = roster.ToDictionary(x => x.Id, x => x.Id.Value == 2 ? 1 : 0);
            var headquarters = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            // Base 상승치 체력 +3·공격 +2. 정의 값은 체력 10·공격 5다.
            var initialization = Initialization(
                enemy: new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0, level: 2),
                structures: new[] { headquarters },
                placements: new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(4, 4), Rotation4.R0) },
                baseGrowth: new CategoryLevelGrowth(3, 2));

            var prepared = BattlePreparationCalculator.Prepare(initialization, roster, Main(), souls,
                new InitialDrawPlan(new[] { roster[1].Id, roster[2].Id }), Array.Empty<CharacterCombatFacts>());

            CharacterCombatFacts Owned(int id) => prepared.Characters.Single(c => c.OwnedId == new CharacterInstanceId(id));
            var enemy = prepared.Characters.Single(c => c.Owner == Owner.Enemy);
            Assert.That((Owned(1).Stats.AttackPower, Owned(1).Health.MaxHealth), Is.EqualTo((5, 10)), "메인은 지속 상태 그대로");
            Assert.That((Owned(2).Stats.AttackPower, Owned(2).Stats.EffectiveAttackPower, Owned(2).Health.CurrentHealth,
                Owned(2).Health.MaxHealth), Is.EqualTo((9, 10, 17, 17)), "레벨 3 서브: 성장 뒤 영혼 1");
            Assert.That((Owned(3).Stats.AttackPower, Owned(3).Health.MaxHealth), Is.EqualTo((5, 10)));
            Assert.That((enemy.Stats.AttackPower, enemy.Health.CurrentHealth, enemy.Health.MaxHealth), Is.EqualTo((7, 13, 13)));
            // 처치 경험치의 기준인 생성 레벨이 전투 사실에 남고, 아군은 번 경험치 없이 시작한다(경험치 설계 §4.1).
            Assert.That(enemy.Experience.EnemyLevel, Is.EqualTo(2));
            Assert.That(prepared.Characters.Where(c => c.Owner == Owner.Player).Select(c => c.Experience.Earned), Is.All.Zero);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void InsufficientHandCapacity_RejectsWithoutMutatingInputs(int capacity)
        {
            var roster = Roster(2);
            Assert.Throws<InvalidOperationException>(() => Prepare(
                Initialization(capacity: capacity), roster,
                new InitialDrawPlan(roster.Skip(1).Select(r => r.Id).ToArray())));
            Assert.That(roster.Select(r => r.Id.Value), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void DrawOrder_WithMainInsteadOfSub_IsRejectedByCore()
        {
            // 드로우 대상 역할(서브만)은 Core가 소유한다. Buffer는 중복과 보유 참조만 확인한다.
            var roster = Roster(2);
            Assert.Throws<InvalidOperationException>(() => Prepare(Initialization(), roster,
                new InitialDrawPlan(new[] { roster[0].Id, roster[1].Id })));
        }

        [Test]
        public void AlreadyLinkedCandidate_RejectsBeforeDrawCursorAdvance()
        {
            var roster = Roster(2);
            var draw = new InitialDrawPlan(roster.Skip(1).Select(x => x.Id).ToArray());
            var initial = Prepare(Initialization(), roster, draw);
            Assert.Throws<InvalidOperationException>(() => BattlePreparationCalculator.Prepare(
                Initialization(), roster, Main(), roster.ToDictionary(x => x.Id, _ => 0), draw,
                initial.Characters));
            Assert.That(draw.InitialCount, Is.EqualTo(2));
            Assert.That(initial.Combat.DrawCursor, Is.EqualTo(2));
        }

        [Test]
        public void EnemyPlacement_RejectsPlayerOnlyDefinitionsAndWrongZone()
        {
            var roster = Roster(0);
            var draw = new InitialDrawPlan(Array.Empty<CharacterInstanceId>());
            foreach (var placement in new[]
            {
                new InitialEnemyPlacementSpec(EntityKey.Unit_Akimbo, new CellCoord(1, 4), Rotation4.R0),
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 0), Rotation4.R0)
            })
                Assert.Throws<InvalidOperationException>(() => Prepare(Initialization(enemy: placement), roster, draw));
        }

        // 포효 + 사령술 보스(공격·이동 없음)는 포효로 행동하므로 준비된다.
        [TestCase(false, false, true, true, true)]
        // 공격·이동·포효가 모두 없으면 보류된 사령술만 남아 구성 오류다.
        [TestCase(false, false, false, true, false)]
        // 일반 공격 적은 이 검사의 영향을 받지 않는다.
        [TestCase(true, true, false, false, true)]
        public void InitialEnemy_RequiresAnAvailableBehavior(bool attack, bool move, bool roar, bool necromancy, bool accepted)
        {
            var traits = new List<TraitSpec>();
            if (necromancy) traits.Add(new NecromancyTraitSpec(EntityKey.Unit_Gravekeeper, 2, 7));
            if (roar) traits.Add(new RoarTraitSpec(5));
            var enemy = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Main, 5, 0, 1, traits, UnitOwnerEligibility.EnemyOnly,
                behavior: new UnitBehaviorSpec(move, attack, false));
            var original = Initialization(enemy: new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0));
            var initialization = new BattleInitialization(original.Stage, original.MainAnchor, original.MainRotation,
                original.Units.Select(u => u.Key == EntityKey.Unit_Demon ? enemy : u).ToArray(), original.Structures, original.Categories);
            var draw = new InitialDrawPlan(Array.Empty<CharacterInstanceId>());
            if (!accepted)
            {
                var error = Assert.Throws<InvalidOperationException>(() => Prepare(initialization, Roster(0), draw));
                Assert.That(error.Message, Does.Contain(EntityKey.Unit_Demon.ToString()));
                return;
            }
            var prepared = Prepare(initialization, Roster(0), draw);
            Assert.That(prepared.Characters.Any(c => c.CharacterKey == EntityKey.Unit_Demon && c.Owner == Owner.Enemy), Is.True);
        }

        [Test]
        public void RotatedStructures_UseDomainFootprintAndRejectOverlap()
        {
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 2, 1,
                new[] { CellCoord.Zero, new CellCoord(1, 0) }, 20, false, 0);
            var placement = new InitialEnemyStructurePlacementSpec(definition.Key, new CellCoord(2, 4), Rotation4.R90);
            var initialization = Initialization(structures: new[] { definition }, placements: new[] { placement });
            var prepared = Prepare(initialization, Roster(0), new InitialDrawPlan(Array.Empty<CharacterInstanceId>()));
            Assert.That(prepared.Structures.Single().OccupiedCells,
                Is.EquivalentTo(new[] { new CellCoord(2, 4), new CellCoord(2, 5) }));

            Assert.Throws<InvalidOperationException>(() => Prepare(
                Initialization(structures: new[] { definition }, placements: new[] { placement, placement }),
                Roster(0), new InitialDrawPlan(Array.Empty<CharacterInstanceId>())));
        }

        [Test]
        public void Preparation_InitializesTraitAndCategoryFromRegisteredRules()
        {
            var definition = new UnitSpec(EntityKey.Unit_Akimbo, UnitRole.Regular, 1, 1,
                new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace,
                2,
                CategoryKey.Heavy, 5, 0,
                1, new TraitSpec[] { new FocusTraitSpec(2, 3) }, UnitOwnerEligibility.PlayerOnly);
            var initialization = Initialization();
            initialization = new BattleInitialization(initialization.Stage, CellCoord.Zero, Rotation4.R0,
                initialization.Units.Select(x => x.Key == definition.Key ? definition : x).ToArray(),
                initialization.Structures, initialization.Categories);
            var prepared = Prepare(initialization, Roster(1), new InitialDrawPlan(new[] { new CharacterInstanceId(2) }));
            var stats = prepared.Characters[1].Stats;
            var focus = (FocusTraitStatusSnapshot)stats.TraitStatuses.Span[0];
            Assert.That(focus.ActiveStacks, Is.Zero);
            Assert.That(focus.GainScheduled, Is.False);
            Assert.That(focus.MovedThisTurn, Is.False);
            Assert.That(stats.Category.Key, Is.EqualTo(CategoryKey.Heavy));
        }

        [Test]
        public void HandLocation_RejectsMissingSlot()
        {
            var prepared = Prepare(Initialization(), Roster(1), new InitialDrawPlan(new[] { new CharacterInstanceId(2) }));
            var sub = prepared.Characters[1];
            Assert.Throws<ArgumentException>(() => new CharacterCombatFacts(sub.EntityId, sub.OwnedId,
                sub.CharacterKey, sub.Owner, sub.OwnershipRole, CharacterRuntimeArea.Hand,
                sub.Health, sub.Stats, null, null, null, Array.Empty<CellCoord>(), UnitActionUse.Available));
        }

        [Test]
        public void EnemiesAndHeadquarters_PrecedeHandAllocation_AndMayExceedReserveCapacity()
        {
            var original = Initialization();
            var stage = new StageConfig(8, 6, 2, 2, 2, 42, new[]
            {
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R90),
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(2, 4), Rotation4.R180),
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(3, 4), Rotation4.R0)
            }, Array.Empty<CellCoord>(), Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(4, 4), Rotation4.R0) });
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var initialization = new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, original.Units,
                new[] { definition }, original.Categories);
            var result = Prepare(initialization, Roster(1), new InitialDrawPlan(new[] { new CharacterInstanceId(2) }));
            var enemies = result.Characters.Where(c => c.Owner == Owner.Enemy).ToArray();
            Assert.That(enemies.Select(c => c.EntityId.Value), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(enemies.Select(c => c.Anchor.Value), Is.EqualTo(new[] { new CellCoord(1, 4), new CellCoord(2, 4), new CellCoord(3, 4) }));
            Assert.That(enemies.Select(c => c.Rotation.Value), Is.EqualTo(new[] { Rotation4.R90, Rotation4.R180, Rotation4.R0 }));
            Assert.That(enemies.All(c => c.OwnedId == null && c.Area == CharacterRuntimeArea.Field), Is.True);
            Assert.That(result.Structures.Single().EntityId.Value, Is.EqualTo(5));
            Assert.That(result.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Sub).EntityId.Value, Is.EqualTo(6));
            Assert.That(result.CommandState.Allocation.NextEntityId, Is.EqualTo(7));
            Assert.That(result.Combat.DrawCursor, Is.EqualTo(1));
        }

        [TestCase("outside")]
        [TestCase("overlap")]
        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("mainEnemy")]
        [TestCase("invalidUndrawn")]
        public void InvalidInitialDefinitions_RejectBeforeAllocatingOrLinking(string condition)
        {
            var original = Initialization();
            var units = original.Units.ToArray();
            var placements = new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0) };
            if (condition == "outside") placements = new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(8, 4), Rotation4.R0) };
            if (condition == "overlap") placements = new[] { placements[0], placements[0] };
            if (condition == "missing") units = units.Where(u => u.Key != EntityKey.Unit_Demon).ToArray();
            if (condition == "duplicate") units = units.Concat(new[] { units[0] }).ToArray();
            if (condition == "mainEnemy") placements = new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Gravekeeper, new CellCoord(1, 4), Rotation4.R0) };
            if (condition == "outside")
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new StageConfig(8, 6, 8, 2, 2, 42, placements));
                return;
            }
            var stage = new StageConfig(8, 6, 8, 2, 2, 42, placements);
            var roster = Roster(4);
            if (condition == "invalidUndrawn")
            {
                units = units.Concat(new[] { Unit(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) }).ToArray();
                roster[4] = new OwnedCharacterRecord(roster[4].Id, EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub);
            }
            var draw = new InitialDrawPlan(roster.Skip(1).Select(r => r.Id).ToArray());
            var initialization = new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, units, original.Structures, original.Categories);
            if (condition == "missing" || condition == "duplicate")
                Assert.Throws<ArgumentException>(() => Prepare(initialization, roster, draw));
            else
                Assert.Throws<InvalidOperationException>(() => Prepare(initialization, roster, draw));
            Assert.That(roster.Select(r => r.Id.Value), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(draw.Order.Count, Is.EqualTo(4));
            Assert.That(draw.InitialCount, Is.EqualTo(3));
        }

        [Test]
        public void InvalidTraitDefinition_PropagatesUnexpectedConfigurationFailure()
        {
            var original = Initialization();
            var broken = new UnitSpec(EntityKey.Unit_Akimbo, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 1,
                CategoryKey.Main, 5, 0, 1,
                new TraitSpec[] { new InvalidFocus() }, UnitOwnerEligibility.PlayerOnly);
            var initialization = new BattleInitialization(original.Stage, CellCoord.Zero, Rotation4.R0,
                original.Units.Select(u => u.Key == EntityKey.Unit_Akimbo ? broken : u).ToArray(), original.Structures, original.Categories);
            Assert.Throws<InvalidOperationException>(() => Prepare(initialization, Roster(1), new InitialDrawPlan(new[] { new CharacterInstanceId(2) })));
        }

        [Test]
        public void DuplicateCardKinds_HaveIndependentRuntimeValuesAndStableOwnedIds()
        {
            var roster = Roster(2);
            var souls = roster.ToDictionary(r => r.Id, r => r.Id.Value == 3 ? 2 : 0);
            var prepared = BattlePreparationCalculator.Prepare(Initialization(), roster, Main(), souls,
                new InitialDrawPlan(new[] { roster[2].Id, roster[1].Id }), Array.Empty<CharacterCombatFacts>());
            var subs = prepared.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Sub).ToArray();
            Assert.That(subs.Select(c => c.OwnedId.Value.Value), Is.EqualTo(new[] { 3, 2 }));
            Assert.That(subs.Select(c => c.EntityId.Value), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(subs.Select(c => c.ReserveSlotIndex.Value), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(subs.Select(c => c.Health.MaxHealth), Is.EqualTo(new[] { 12, 10 }));
            Assert.That(subs.Select(c => c.Stats.SoulAttackBonus), Is.EqualTo(new[] { 2, 0 }));
        }

        [Test]
        public void BossObjective_BindsSingleMarkedEnemy_WithoutHeadquarters()
        {
            var initialization = WithObjective(Initialization(enemy: new InitialEnemyPlacementSpec(
                EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0, isVictoryTarget: true)), BattleObjectiveKind.DesignatedBossDefeated);
            var prepared = Prepare(initialization, Roster(0), new InitialDrawPlan(Array.Empty<CharacterInstanceId>()));
            var boss = prepared.Characters.Single(c => c.Owner == Owner.Enemy);

            Assert.That(prepared.CommandState.Objective.Kind, Is.EqualTo(BattleObjectiveKind.DesignatedBossDefeated));
            Assert.That(prepared.CommandState.Objective.TargetEntityId, Is.EqualTo(boss.EntityId));
            Assert.That(prepared.Structures, Is.Empty);
        }

        [TestCase(BattleObjectiveKind.DesignatedBossDefeated, 0)]
        [TestCase(BattleObjectiveKind.DesignatedBossDefeated, 2)]
        [TestCase(BattleObjectiveKind.EnemyHeadquartersDestroyed, 1)]
        public void ObjectiveKindAndMarkers_MustAgree(BattleObjectiveKind kind, int markers)
        {
            var placements = Enumerable.Range(0, 2).Select(i => new InitialEnemyPlacementSpec(EntityKey.Unit_Demon,
                new CellCoord(1 + i, 4), Rotation4.R0, isVictoryTarget: i < markers)).ToArray();
            var original = Initialization();
            var stage = new StageConfig(8, 6, 8, 2, 2, 42, placements, Array.Empty<CellCoord>(),
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), Array.Empty<InitialEnemyStructurePlacementSpec>());
            var initialization = new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, original.Units,
                original.Structures, original.Categories, objectiveKind: kind);

            Assert.Throws<InvalidOperationException>(() => Prepare(initialization, Roster(0),
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>())));
        }

        [Test]
        public void HeadquartersObjective_IsDefault_AndSurvivesProfileCopy()
        {
            var initialization = Initialization();
            Assert.That(initialization.ObjectiveKind, Is.EqualTo(BattleObjectiveKind.EnemyHeadquartersDestroyed));
            var boss = WithObjective(initialization, BattleObjectiveKind.DesignatedBossDefeated);
            Assert.That(boss.WithAttackProfiles(Array.Empty<CombatAttackProfile>()).ObjectiveKind,
                Is.EqualTo(BattleObjectiveKind.DesignatedBossDefeated));
            Assert.That(Prepare(initialization, Roster(0), new InitialDrawPlan(Array.Empty<CharacterInstanceId>()))
                .CommandState.Objective.SameValues(BattleObjective.EnemyHeadquarters), Is.True);
        }

        [Test]
        public void ThreePlayerTraits_ArePreparedWithoutDroppingAnyRule()
        {
            var original = Initialization();
            var units = original.Units.Select(u => u.Key == EntityKey.Unit_Akimbo
                ? CombatCommandTestData.Spec(u.Key, 5, CategoryKey.Main, new FocusTraitSpec(1, 3),
                    new RoarTraitSpec(2), new KillChainAttackTraitSpec()) : u).ToArray();
            var init = new BattleInitialization(original.Stage, original.MainAnchor, original.MainRotation,
                units, original.Structures, original.Categories);
            var prepared = Prepare(init, Roster(1), new InitialDrawPlan(new[] { new CharacterInstanceId(2) }));
            Assert.That(prepared.Characters.Single(c => c.CharacterKey == EntityKey.Unit_Akimbo).Stats.TraitStatuses.ToArray()
                .Select(t => t.Key), Is.EquivalentTo(new[] { TraitKey.Focus, TraitKey.Roar, TraitKey.KillChainAttack }));
        }

        [Test]
        public void CategoryDefaultBattle_WeightedHeavyEnemiesReceiveTheBenefitOnce()
        {
            var raw = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Heavy, 5, 0, 1, Array.Empty<TraitSpec>(), UnitOwnerEligibility.EnemyOnly);
            var basis = Initialization();
            var placements = EnemySpawnCalculator.Calculate(basis.Stage, new[] { new EnemySpawnCandidate(raw, 1) }, 2, 17, basis.Structures);
            var stage = new StageConfig(8, 6, 8, 2, 2, 42, placements);
            var growth = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories,
                new[] { new TraitGrowthDefinition(new SteadfastWillTraitSpec(), null, "Heavy", "Heavy", "", "") },
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Heavy] = TraitKey.SteadfastWill });
            var init = new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0,
                basis.Units.Select(u => u.Key == raw.Key ? raw : u).ToArray(), basis.Structures, basis.Categories, traitGrowth: growth);
            var enemies = Prepare(init, Roster(0), new InitialDrawPlan(Array.Empty<CharacterInstanceId>())).Characters
                .Where(actor => actor.Owner == Owner.Enemy).ToArray();
            Assert.That(enemies.Length, Is.EqualTo(2));
            foreach (var enemy in enemies)
                Assert.That(enemy.Stats.TraitStatuses.ToArray().Select(t => t.Key), Is.EqualTo(new[] { TraitKey.SteadfastWill }));
        }

        private static BattleInitialization WithObjective(BattleInitialization i, BattleObjectiveKind kind) =>
            new BattleInitialization(i.Stage, i.MainAnchor, i.MainRotation, i.Units, i.Structures, i.Categories,
                i.AttackProfiles, i.Environment, i.EnvironmentBindings, i.ObstaclePlacements, i.WallBlocking, kind);

        private sealed class InvalidFocus : TraitSpec
        {
            internal InvalidFocus() : base(TraitKey.Focus) { }

            protected override bool SameValuesCore(TraitSpec other) => true;
        }

        private static BattlePreparation Prepare(BattleInitialization initialization,
            OwnedCharacterRecord[] roster, InitialDrawPlan draw) =>
            BattlePreparationCalculator.Prepare(initialization, roster, Main(),
                roster.ToDictionary(x => x.Id, _ => 0), draw, Array.Empty<CharacterCombatFacts>());

        private static MainCharacterPersistentState Main() => new(10, 10, 5);

        private static OwnedCharacterRecord[] Roster(int subs) => Enumerable.Range(1, subs + 1)
            .Select(id => new OwnedCharacterRecord(new CharacterInstanceId(id),
                id == 1 ? EntityKey.Unit_Gravekeeper : EntityKey.Unit_Akimbo,
                id == 1 ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub)).ToArray();

        private static BattleInitialization Initialization(CellCoord? anchor = null, int capacity = 8,
            InitialEnemyPlacementSpec enemy = null, StructureSpec[] structures = null,
            InitialEnemyStructurePlacementSpec[] placements = null, CategoryLevelGrowth baseGrowth = default)
        {
            var stage = new StageConfig(8, 6, capacity, 2, 2, 42,
                enemy == null ? Array.Empty<InitialEnemyPlacementSpec>() : new[] { enemy },
                Array.Empty<CellCoord>(), Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(), placements ?? Array.Empty<InitialEnemyStructurePlacementSpec>());
            return new BattleInitialization(stage, anchor ?? CellCoord.Zero, Rotation4.R0,
                new[] { Unit(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) },
                structures ?? Array.Empty<StructureSpec>(), UnitSpecTestFactory.CreateGrowthCategoryCatalog(baseGrowth));
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility) =>
            new(key, role, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace,
                role == UnitRole.MainCharacter ? 1 : 2, CategoryKey.Main,
                5, 0, 1, Array.Empty<TraitSpec>(), eligibility);
    }
}
