using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TraitShieldTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ControllerStart_IsolationIncludesOpposingUnits(bool player)
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithPlacement(c.Area, null, new CellCoord(2, 3), c.Rotation, new[] { new CellCoord(2, 3) }) : c).ToArray());
            input = TraitCombatFixtures.Equip(input, player ? EntityKey.Unit_Akimbo : EntityKey.Unit_Demon, new SoloActionTraitSpec(10, 1, 0, 1));
            input = EnemyControllerPhaseTests.Input(input, player ? CombatControllerPhase.StartingPlayer : CombatControllerPhase.StartingEnemy);
            var result = CombatTurnCalculator.BeginControllerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Active(TraitCombatFixtures.Final(result, player ? 1 : 2)), Is.Zero);
        }

        [Test]
        public void StageCleanup_RemovesShieldHistoryWithoutAttackGrants()
        {
            var main = Character(Cohesion(), 10);
            Assert.That(main.TraitEffects.AttackGrants, Is.Empty);
            Assert.That(Shields(main), Is.Not.Empty);
            var cleared = CombatTemporaryStateCleanup.Clear(main);
            Assert.That(Shields(cleared), Is.Empty);
            Assert.That(Active(cleared), Is.Zero);
        }

        [TestCase(2, 3)]
        [TestCase(2, 4)]
        public void SoloAction_ExitAndReenterRetainsSpentBudget(int x, int y)
        {
            var input = Reconcile(TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new SoloActionTraitSpec(10, 1, 0, 1)));
            input = Damage(input, 1, 4);
            Assert.That(Character(input, 1).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Active(Character(input, 1)), Is.EqualTo(6));
            input = Move(input, 4, new CellCoord(x, y));
            Assert.That(Active(Character(input, 1)), Is.Zero);
            input = Move(input, 4, new CellCoord(5, 3));
            Assert.That(Active(Character(input, 1)), Is.EqualTo(6));
        }

        [Test]
        public void SoloAction_ResetsOnlyOnOwnSideAtConfiguredInterval(
            [Values(Owner.Player, Owner.Enemy)] Owner owner, [Values(1, 2, 3)] int interval,
            [Values(0, 6, 10)] int remaining, [Values(0, 3)] int initialCompleted)
        {
            int id = owner == Owner.Player ? 1 : 2;
            var input = SoloAction(owner, interval, initialCompleted);
            input = Damage(input, id, 10 - remaining);
            Assert.That(Character(input, id).TraitEffects.Shields.Single().LastResetControllerTurnCount, Is.EqualTo(initialCompleted));
            for (int end = initialCompleted + 1; end <= initialCompleted + 4 * interval; end++)
            {
                Owner side = end % 2 == 1 ? Owner.Player : Owner.Enemy;
                long ownEnds = owner == Owner.Player ? (end + 1) / 2 : end / 2;
                bool resetDue = side == owner && ownEnds % interval == 0;
                var before = Character(input, id).TraitEffects.Shields.Single();
                var events = new List<CombatEvent>();
                input = EndShieldTurn(input, side, events);
                var after = Character(input, id).TraitEffects.Shields.Single();
                Assert.That(after.RemainingBudget, Is.EqualTo(resetDue ? 10 : before.RemainingBudget), $"{owner}, end {end}");
                Assert.That(after.LastResetControllerTurnCount,
                    Is.EqualTo(resetDue ? end : before.LastResetControllerTurnCount), $"{owner}, end {end}");
                var resets = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                    .Where(t => t.TraitKey == TraitKey.SoloAction && t.TraitActivationKind == TraitActivationKind.ShieldReset).ToArray();
                Assert.That(resets.Length, Is.EqualTo(resetDue ? 1 : 0), $"{owner}, end {end}");
                if (resetDue)
                {
                    Assert.That(resets[0].SourceUnitId, Is.EqualTo(new EntityId(id)));
                    Assert.That(resets[0].Magnitude, Is.EqualTo(10 - before.RemainingBudget));
                    input = Damage(input, id, 10 - remaining);
                }
            }
        }

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void SoloAction_OpposingEndStillReconcilesEligibilityWithoutRefilling(Owner owner)
        {
            int id = owner == Owner.Player ? 1 : 2;
            var input = Damage(SoloAction(owner, 1, owner == Owner.Player ? 1 : 0), id, 4);
            var before = Character(input, id).TraitEffects.Shields.Single();
            var blocker = Character(input, 10);
            var cell = owner == Owner.Player ? new CellCoord(2, 3) : new CellCoord(3, 3);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == blocker.EntityId
                ? c.WithPlacement(c.Area, null, cell, c.Rotation, new[] { cell }) : c).ToArray());
            var events = new List<CombatEvent>();
            input = EndShieldTurn(input, owner == Owner.Player ? Owner.Enemy : Owner.Player, events);
            var after = Character(input, id).TraitEffects.Shields.Single();
            Assert.That(after.Eligible, Is.False);
            Assert.That(after.RemainingBudget, Is.EqualTo(6));
            Assert.That(after.LastResetControllerTurnCount, Is.EqualTo(before.LastResetControllerTurnCount));
            var activations = events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitKey == TraitKey.SoloAction).ToArray();
            Assert.That(activations.Select(t => t.TraitActivationKind), Is.EqualTo(new[] { TraitActivationKind.ShieldRemoved }));
            Assert.That(Active(Character(Move(input, 10, new CellCoord(7, 7)), id)), Is.EqualTo(6));
        }

        [Test]
        public void Cohesion_ExitRemovesOnlyItsSource()
        {
            var input = Damage(Cohesion(), 10, 4);
            Assert.That(Active(Character(input, 10)), Is.EqualTo(16));
            input = Move(input, 1, new CellCoord(1, 0));
            Assert.That(Active(Character(input, 10)), Is.EqualTo(10));
            input = Move(input, 1, new CellCoord(1, 3));
            Assert.That(Active(Character(input, 10)), Is.EqualTo(16));
        }

        [Test]
        public void MultipleSources_AddButDoNotRefill()
        {
            var input = Damage(Cohesion(), 10, 14);
            Assert.That(Character(input, 10).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Active(Character(input, 10)), Is.EqualTo(6));
            input = Reconcile(Reconcile(input));
            Assert.That(Active(Character(input, 10)), Is.EqualTo(6));
            input = Damage(input, 10, 9);
            Assert.That(Character(input, 10).Health.CurrentHealth, Is.EqualTo(97));
            Assert.That(Active(Character(input, 10)), Is.Zero);
        }

        [Test]
        public void SharedDefinition_IndependentBudgets()
        {
            var input = Damage(Cohesion(), 10, 4);
            var grants = Shields(Character(input, 10));
            Assert.That(grants.Length, Is.EqualTo(2));
            Assert.That(grants.Select(g => Get(g, "RemainingBudget")), Is.EquivalentTo(new[] { 6, 10 }));
            Assert.That(grants.Select(g => Get(Get(g, "Origin"), "OwnerInstanceId")).Distinct().Count(), Is.EqualTo(2));
        }

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void BothSideEnds_ResetBudgets(Owner side)
        {
            var input = Damage(Cohesion(), 10, 14);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            input = CombatCommandTestData.With(input, state: input.State.With(phase: CombatFlowPhase.Automatic, turn: new CombatTurnFacts(StagePhase.Combat,
                controllerPhase: CombatControllerPhase.StartingEnemy, completedControllerTurnCount: 1)));
            typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".TraitTurnLifecycle")
                .GetMethod("EndControllerTurn", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { input, projected, side });
            Assert.That(Active(projected[new EntityId(10)]), Is.EqualTo(20));
        }

        [Test]
        public void InTurnBudgetIncrease_IsNotClampedOrReplaced()
        {
            var input = Cohesion();
            var main = Character(input, 10);
            var grants = Shields(main);
            var first = grants[0];
            grants[0] = Activator.CreateInstance(first.GetType(), Get(first, "Origin"), 15, true, Get(first, "LastResetControllerTurnCount"));
            var array = Array.CreateInstance(first.GetType(), grants.Length);
            for (int i = 0; i < grants.Length; i++) array.SetValue(grants[i], i);
            var effects = (CharacterTraitEffects)Activator.CreateInstance(typeof(CharacterTraitEffects), main.TraitEffects.AttackGrants, array);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == main.EntityId ? c.With(traitEffects: effects) : c).ToArray());
            Assert.That(Active(Character(Reconcile(input), 10)), Is.EqualTo(25));
        }

        [TestCase(2, 3)]
        [TestCase(2, 4)]
        public void CohesionDefense_DerivesCurrentLivingAlliesOnly(int x, int y)
        {
            var input = Move(Cohesion(), 10, new CellCoord(x, y));
            Assert.That(Active(Character(input, 10)), Is.EqualTo(20));
            var method = typeof(CombatEffectiveStatsCalculator).GetMethod("PhysicalDefense", new[] { typeof(CombatCommandInput), typeof(CharacterCombatFacts) });
            Assert.That(method, Is.Not.Null);
            Assert.That(method.Invoke(null, new object[] { input, Character(input, 1) }), Is.EqualTo(3));
            input = Move(input, 10, new CellCoord(7, 7));
            Assert.That(method.Invoke(null, new object[] { input, Character(input, 1) }), Is.Zero);
        }

        internal static CombatCommandInput Cohesion()
        {
            var input = CombatCommandTestData.Input();
            var second = CombatCommandTestData.Character(5, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player, 20);
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { second }).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new CohesionTraitSpec(3, 10, 1, 1));
            return Move(input, 10, new CellCoord(2, 3));
        }

        internal static CombatCommandInput Reconcile(CombatCommandInput input)
        {
            var type = typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".TraitPresenceReconciler");
            Assert.That(type, Is.Not.Null);
            var changes = (CharacterChanges)type.GetMethod("Calculate", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { input, input.Characters });
            return CombatCommandTestData.With(input, characters: input.Characters.Select(c => changes.Characters.SingleOrDefault(v => v.Final.EntityId == c.EntityId)?.Final ?? c).ToArray());
        }

        private static CombatCommandInput SoloAction(Owner owner, int interval, int completed)
        {
            var input = CombatCommandTestData.Input();
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3 && c.EntityId.Value != 4).ToArray());
            input = CombatCommandTestData.With(input, state: input.State.With(
                phase: completed % 2 == 0 ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic, turn: new CombatTurnFacts(StagePhase.Combat,
                controllerPhase: completed % 2 == 0 ? CombatControllerPhase.Player : CombatControllerPhase.StartingEnemy,
                completedPlayerTurnCount: (completed + 1) / 2, completedControllerTurnCount: completed)));
            return Reconcile(TraitCombatFixtures.Equip(input, owner == Owner.Player ? EntityKey.Unit_Akimbo : EntityKey.Unit_Demon,
                new SoloActionTraitSpec(10, 1, 0, interval)));
        }

        private static CombatCommandInput EndShieldTurn(CombatCommandInput input, Owner side, List<CombatEvent> events)
        {
            var old = input.State.Turn;
            var turn = new CombatTurnFacts(StagePhase.Combat,
                side == Owner.Player ? CombatControllerPhase.StartingEnemy : CombatControllerPhase.StartingPlayer,
                old.ControllerTurnSequence, old.CompletedPlayerTurnCount + (side == Owner.Player ? 1 : 0), old.EnemyOrigins, old.Summons, old.CompletedControllerTurnCount + 1);
            input = CombatCommandTestData.With(input, state: input.State.With(phase: CombatFlowPhase.Automatic, turn: turn));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".TraitTurnLifecycle")
                .GetMethod("EndAndRecord", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { input, projected, side, events });
            return CombatCommandTestData.With(input, characters: projected.Values.ToArray());
        }

        private static CombatCommandInput Move(CombatCommandInput input, int id, CellCoord cell) => Reconcile(CombatCommandTestData.With(input,
            characters: input.Characters.Select(c => c.EntityId.Value == id
                ? c.WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell }) : c).ToArray()));

        private static CombatCommandInput Damage(CombatCommandInput input, int id, int amount)
        {
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            CombatDamageApplication.For(input, characters, new List<CombatEvent>()).Apply(new EntityId(id), CombatDamageKind.EnvironmentDamage,
                new CombatDamageOrigin(CellCoord.Zero, new EntityId(2), 1), amount);
            return CombatCommandTestData.With(input, characters: characters.Values.ToArray());
        }

        private static CharacterCombatFacts Character(CombatCommandInput input, int id) => input.Characters.Single(c => c.EntityId.Value == id);
        private static object Get(object value, string field) => value.GetType().GetProperty(field).GetValue(value);
        private static object[] Shields(CharacterCombatFacts character) => ((System.Collections.IEnumerable)Get(character.TraitEffects, "Shields")).Cast<object>().ToArray();
        private static int Active(CharacterCombatFacts character) => (int)Get(character.TraitEffects, "ActiveShieldTotal");
    }
}
