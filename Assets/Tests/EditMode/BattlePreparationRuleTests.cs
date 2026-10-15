using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
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
        public void InitialDeal_AlwaysCapsSubsAtThree(int subCount, int total)
        {
            var roster = Roster(subCount);
            var draw = new InitialDrawPlan(roster.Skip(1).Select(x => x.Id).ToArray());
            BattlePreparation prepared = Prepare(Initialization(), roster, draw);

            Assert.That(prepared.Characters.Count, Is.EqualTo(total));
            Assert.That(prepared.Characters.Select(x => x.OwnedId.Value),
                Is.EqualTo(roster.Take(total).Select(x => x.Id)));
            Assert.That(prepared.Characters[0].Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(prepared.Characters.Skip(1).All(x => x.Area == CharacterRuntimeArea.Hand), Is.True);
            Assert.That(prepared.Combat.DrawCursor, Is.EqualTo(Math.Min(3, subCount)));
            Assert.That(prepared.NextEntityId, Is.EqualTo(total + 1));
        }

        [Test]
        public void Preparation_UsesPersistentMainAndPerInstanceSouls()
        {
            var roster = Roster(2);
            var souls = roster.ToDictionary(x => x.Id, x => x.Id.Value == 2 ? 2 : 0);
            souls[roster[0].Id] = 3;
            var main = new MainCharacterPersistentState(4, 12, 7, 6, 5, 2);
            var prepared = BattlePreparationCalculator.Prepare(Initialization(), roster, main, souls,
                new InitialDrawPlan(roster.Skip(1).Select(x => x.Id).ToArray()), Array.Empty<CharacterCombatFacts>());

            Assert.That(prepared.Characters[0].Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(prepared.Characters[0].Health.MaxHealth, Is.EqualTo(15));
            Assert.That(prepared.Characters[0].Stats.EffectiveAttackPower, Is.EqualTo(10));
            Assert.That(prepared.Characters[0].Stats.MoveRange, Is.EqualTo(5));
            Assert.That(prepared.Characters[0].Stats.MaxActionCost, Is.EqualTo(6));
            Assert.That(prepared.Characters[1].Health.MaxHealth, Is.EqualTo(12));
            Assert.That(prepared.Characters[2].Health.MaxHealth, Is.EqualTo(10));
            Assert.That(main.CurrentHealth, Is.EqualTo(4));
            Assert.That(souls[roster[1].Id], Is.EqualTo(2));
        }

        [TestCase(-1, 0, 4)]
        [TestCase(0, 2, 4)]
        [TestCase(0, 0, 1)]
        public void InvalidPlacementOrCapacity_RejectsWithoutMutatingInputs(int x, int y, int capacity)
        {
            var roster = Roster(2);
            Assert.Throws<InvalidOperationException>(() => Prepare(
                Initialization(new CellCoord(x, y), capacity), roster,
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
                new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 3, 1,
                2,
                CategoryKey.Heavy, 5, 4, 1, 0,
                1, 1, true, new TraitSpec[] { new FocusTraitSpec(2, 3) }, UnitOwnerEligibility.PlayerOnly);
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
            Assert.That(stats.Category.DefenseFacing, Is.EqualTo(Rotation4.R0));
            Assert.That(stats.Category.DamageReductionPercent, Is.EqualTo(50));
            Assert.That(stats.Category.MovedThisTurn, Is.False);
        }

        [Test]
        public void HandLocation_RejectsMissingSlot()
        {
            var prepared = Prepare(Initialization(), Roster(1), new InitialDrawPlan(new[] { new CharacterInstanceId(2) }));
            var sub = prepared.Characters[1];
            Assert.Throws<ArgumentException>(() => new CharacterCombatFacts(sub.EntityId, sub.OwnedId,
                sub.CharacterKey, sub.Owner, sub.OwnershipRole, CharacterRuntimeArea.Hand,
                sub.Health, sub.Stats, null, null, null, Array.Empty<CellCoord>(), CellCoord.Zero, 0, 0));
        }

        [Test]
        public void EnemiesAndHeadquarters_PrecedeHandAllocation_AndMayExceedReserveCapacity()
        {
            var original = Initialization();
            var stage = new StageConfig(8, 6, 1, 2, 2, 42, new[]
            {
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R90),
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(2, 4), Rotation4.R180),
                new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(3, 4), Rotation4.R0)
            }, Array.Empty<CellCoord>(), Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(),
                100, new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(4, 4), Rotation4.R0) });
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
            Assert.That(result.Characters.Single(c => c.Area == CharacterRuntimeArea.Hand).EntityId.Value, Is.EqualTo(6));
            Assert.That(result.NextEntityId, Is.EqualTo(7));
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
            if (condition == "mainEnemy") placements = new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_MainCharacter, new CellCoord(1, 4), Rotation4.R0) };
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
                10, MoveMethod.LiftAndPlace, 3, 1, 1,
                CategoryKey.Base, 5, 4, 1, 0, 1, 1, true,
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
            Assert.That(subs.Select(c => c.ReserveSlotIndex.Value), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(subs.Select(c => c.Health.MaxHealth), Is.EqualTo(new[] { 12, 10 }));
            Assert.That(subs.Select(c => c.Stats.SoulAttackBonus), Is.EqualTo(new[] { 2, 0 }));
        }

        private sealed class InvalidFocus : TraitSpec
        {
            internal InvalidFocus() : base(TraitKey.Focus) { }
        }

        private static BattlePreparation Prepare(BattleInitialization initialization,
            OwnedCharacterRecord[] roster, InitialDrawPlan draw) =>
            BattlePreparationCalculator.Prepare(initialization, roster, Main(),
                roster.ToDictionary(x => x.Id, _ => 0), draw, Array.Empty<CharacterCombatFacts>());

        private static MainCharacterPersistentState Main() => new(10, 10, 5, 4, 3, 1);

        private static OwnedCharacterRecord[] Roster(int subs) => Enumerable.Range(1, subs + 1)
            .Select(id => new OwnedCharacterRecord(new CharacterInstanceId(id),
                id == 1 ? EntityKey.Unit_MainCharacter : EntityKey.Unit_Akimbo,
                id == 1 ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub)).ToArray();

        private static BattleInitialization Initialization(CellCoord? anchor = null, int capacity = 8,
            InitialEnemyPlacementSpec enemy = null, StructureSpec[] structures = null,
            InitialEnemyStructurePlacementSpec[] placements = null)
        {
            var stage = new StageConfig(8, 6, capacity, 2, 2, 42,
                enemy == null ? Array.Empty<InitialEnemyPlacementSpec>() : new[] { enemy },
                Array.Empty<CellCoord>(), Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.StageGimmickSpec>(),
                100, placements ?? Array.Empty<InitialEnemyStructurePlacementSpec>());
            return new BattleInitialization(stage, anchor ?? CellCoord.Zero, Rotation4.R0,
                new[] { Unit(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) },
                structures ?? Array.Empty<StructureSpec>(), UnitSpecTestFactory.CreateDefaultCategoryCatalog());
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility) =>
            new(key, role, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 3, 1,
                role == UnitRole.MainCharacter ? 1 : 2, CategoryKey.Base,
                5, 4, 1, 0, 1, 1, true, Array.Empty<TraitSpec>(), eligibility);
    }
}
