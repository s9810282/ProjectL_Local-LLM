using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PositionTileModifierTests
    {
        [Test]
        public void ApplyMaxHp_DoesNotHeal()
        {
            var input = RouteWithTiles(8, new DestinationTileModifierFact(7, new CellCoord(2, 1),
                true, new[] { Owner.Player }, 3, 5));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = MovementTestData.Actor(result);
            Assert.That(final.Health, Is.EqualTo(new HealthStateSnapshot(8, 15)));
            Assert.That(final.PositionModifier.SourceIds, Is.EqualTo(new long[] { 7 }));
            Assert.That(final.Stats.EffectiveAttackPower, Is.EqualTo(final.Stats.AttackPower + 3));
            Assert.That(final.TemporaryAttackBonus, Is.Zero);
            Assert.That(final.TemporaryMaxHealthBonus, Is.Zero);
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.False);
        }

        [Test]
        public void RemoveMaxHp_CapsWithoutDamage()
        {
            var first = CombatCommandCalculator.Calculate(RouteWithTiles(8, Tile(7, new CellCoord(2, 1), 3, 5)), System.Array.Empty<IMovementTileRule>());
            var after = MovementTestData.After(first);
            var actor = MovementTestData.Actor(first);
            var healed = CharacterSettlementCalculator.SetHealth(actor, after.Main, 14).Characters.Single().Final;
            after = CombatCommandTestData.With(after,
                characters: after.Characters.Select(c => c.EntityId == actor.EntityId ? healed : c).ToArray());
            var result = CombatCommandCalculator.Calculate(MovementTestData.Route(after,
                new CellCoord(2, 1), new CellCoord(3, 1)), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health, Is.EqualTo(new HealthStateSnapshot(10, 10)));
            Assert.That(result.Events.First().Kind, Is.EqualTo(CombatEventKind.HealthNormalized));
            Assert.That(result.Events.Select(e => e.Kind), Has.No.Member(CombatEventKind.HealthChanged));
            Assert.That(result.PathMovement.ReturnBarrierOccurred, Is.False, "The cap itself is not damage or a barrier.");
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Moved)
                .Expected.Health.CurrentHealth, Is.EqualTo(10), "Normalize before entering the next cell.");
        }

        [Test]
        public void PassThrough_DoesNotApplyModifier()
        {
            var input = MovementTestData.Route(RouteWithTiles(8, Tile(7, new CellCoord(2, 1), 3, 5)),
                new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.MaxHealth, Is.EqualTo(10));
            Assert.That(MovementTestData.Actor(result).PositionModifier.SameValues(PositionTileModifier.None), Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().All(e => e.Final.Health.MaxHealth == 10), Is.True);
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
        }

        [Test]
        public void Return_RecomputesModifierWithoutRestoringHp()
        {
            var input = RouteWithTiles(8, Tile(7, new CellCoord(1, 1), 3, 5),
                Tile(8, new CellCoord(2, 1), -2, -3));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var origin = PositionTileModifierCalculator.Replace(actor,
                PositionTileModifierCalculator.ResolveAt(actor.Anchor.Value, input.State.Environment));
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? origin : c).ToArray());
            var moved = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(MovementTestData.Actor(moved).Health, Is.EqualTo(new HealthStateSnapshot(7, 7)));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(MovementTestData.After(moved)), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            var final = MovementTestData.Actor(returned);
            Assert.That(final.Health, Is.EqualTo(new HealthStateSnapshot(7, 15)));
            Assert.That(final.PositionModifier.SourceIds, Is.EqualTo(new long[] { 7 }));
            Assert.That(final.Stats.PositionAttackBonus, Is.EqualTo(3));
            Assert.That(returned.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(returned.Field.Final.SameValues(input.Souls), Is.True);
            Assert.That(returned.Events.Select(e => e.Kind),
                Is.EqualTo(new[] { CombatEventKind.Returned, CombatEventKind.HealthNormalized }));
        }

        [TestCase(3, 5, 15)]
        [TestCase(-2, -3, 7)]
        public void RepeatedReplacement_IsIdempotentAndIndependent(int attack, int maxHealth, int expectedMax)
        {
            var input = RouteWithTiles(6, Tile(7, new CellCoord(1, 1), attack, maxHealth));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var next = PositionTileModifierCalculator.ResolveAt(actor.Anchor.Value, input.State.Environment);
            var once = PositionTileModifierCalculator.Replace(actor, next);
            Assert.That(PositionTileModifierCalculator.Replace(once, next), Is.SameAs(once));
            Assert.That(once.Health, Is.EqualTo(new HealthStateSnapshot(6, expectedMax)));
            Assert.That(once.Stats.EffectiveAttackPower, Is.EqualTo(actor.Stats.EffectiveAttackPower + attack));
            var removed = PositionTileModifierCalculator.Replace(once, PositionTileModifier.None);
            Assert.That(removed.Health, Is.EqualTo(actor.Health));
            Assert.That(removed.Stats.EffectiveAttackPower, Is.EqualTo(actor.Stats.EffectiveAttackPower));
        }

        [TestCase(0, -10)]
        [TestCase(-100, 0)]
        public void InvalidEffectiveStats_RejectWithoutChangingInputs(int attack, int health)
        {
            var input = RouteWithTiles(8, Tile(7, new CellCoord(2, 1), attack, health));
            Assert.Catch<ArgumentException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Health.MaxHealth, Is.EqualTo(10));
        }

        [Test]
        public void MissingCombinationPolicy_GatesOverlappingActiveSources()
        {
            var cell = new CellCoord(2, 1);
            var input = RouteWithTiles(8, Tile(7, cell, 1, 2), Tile(8, cell, 2, 3));
            Assert.Throws<NotSupportedException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                new DestinationTileModifierFact(7, cell, false, new[] { Owner.Player }, 1, 2) });
            Assert.That(PositionTileModifierCalculator.ResolveAt(cell, environment), Is.SameAs(PositionTileModifier.None));
        }

        [Test]
        public void PersistentMainRecalculation_PreservesTileSourceAndIndependentStats()
        {
            var input = CombatCommandTestData.Input(empty: true);
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                Tile(7, main.Anchor.Value, 3, 5) });
            main = PositionTileModifierCalculator.Replace(main,
                PositionTileModifierCalculator.ResolveAt(main.Anchor.Value, environment));
            var persistent = new MainCharacterPersistentState(100, 102, 3, 5, 4, 1);
            var changes = CharacterSettlementCalculator.SetPersistentMain(main, input.Main, persistent);
            var final = changes.Characters.Single().Final;
            Assert.That(final.Health, Is.EqualTo(new HealthStateSnapshot(100, 107)));
            Assert.That(final.Stats.AttackPower, Is.EqualTo(3));
            Assert.That(final.Stats.EffectiveAttackPower, Is.EqualTo(6));
            Assert.That(final.PositionModifier.SameValues(main.PositionModifier), Is.True);
            Assert.That(changes.Main.Final.PersistentMaxHealth, Is.EqualTo(102));
            var removed = PositionTileModifierCalculator.Replace(final, PositionTileModifier.None);
            Assert.That(removed.Health.MaxHealth, Is.EqualTo(102));
            Assert.That(removed.Stats.EffectiveAttackPower, Is.EqualTo(3));
        }

        [Test]
        public void Preview_FiltersHiddenDestinationFacts_AndLeavesInputsIntact()
        {
            var cell = new CellCoord(2, 1);
            var input = RouteWithTiles(8,
                new DestinationTileModifierFact(7, cell, true, new[] { Owner.Enemy }, 1, 2),
                new DestinationTileModifierFact(8, cell, true, new[] { Owner.Enemy }, 2, 3));
            var preview = CombatMovementCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Health.MaxHealth, Is.EqualTo(10));
            Assert.That(input.State.Environment.Tiles.Count, Is.EqualTo(2));
            Assert.Throws<NotSupportedException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
        }

        [Test]
        public void CheckedModifierArithmetic_RejectsBeforePublication()
        {
            var input = RouteWithTiles(8, Tile(7, new CellCoord(2, 1), 0, int.MaxValue));
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).Health.MaxHealth, Is.EqualTo(10));
        }

        [Test]
        public void NightRecallAndPlacement_ReconcileOnlyCurrentPositionContribution()
        {
            var input = CombatCommonCommandTests.Command(CombatCommandKind.EndUnitTurn);
            var actor = input.Characters[0];
            var tiles = new MovementEnvironmentFacts(new MovementTileFact[] {
                Tile(7, actor.Anchor.Value, 5, 50), Tile(8, new CellCoord(2, 1), 2, -10) });
            actor = PositionTileModifierCalculator.Replace(actor,
                PositionTileModifierCalculator.ResolveAt(actor.Anchor.Value, tiles));
            actor = CombatCommandTestData.With(actor, hp: 140);
            var s = input.State;
            input = new CombatCommandInput(1, input.Kind, input.ActorId, default,
                new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, true,
                    s.Structures, s.ThornFields, s.Timeline, s.Turn, tiles),
                new[] { actor, input.Characters[1] }, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, battle: new CombatBattleFacts(10, 10, 37, 2, 2,
                    Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            var recalled = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var hand = recalled.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final;
            Assert.That(hand.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(hand.Health, Is.EqualTo(new HealthStateSnapshot(100, 100)));
            Assert.That(hand.Stats.PositionAttackBonus, Is.Zero);
            Assert.That(recalled.Events.Any(e => e.Kind == CombatEventKind.HealthNormalized), Is.True);
            var next = CombatCommandComposition.Project(recalled);
            var placement = CombatCommandComposition.Intent(next, CombatCommandKind.PlaceUnit, cell: new CellCoord(2, 1));
            var placed = CombatCommandCalculator.Calculate(placement, Array.Empty<IMovementTileRule>());
            Assert.That(placed.Accepted, Is.True, placed.Reason);
            var final = placed.Characters.Characters.Single().Final;
            Assert.That(final.Health, Is.EqualTo(new HealthStateSnapshot(90, 90)));
            Assert.That(final.PositionModifier.SourceIds, Is.EqualTo(new long[] { 8 }));
            Assert.That(final.Stats.PositionAttackBonus, Is.EqualTo(2));
            var returned = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(
                CombatCommandComposition.Project(placed), CombatCommandKind.ReturnUnit), Array.Empty<IMovementTileRule>());
            Assert.That(returned.Characters.Characters.Single().Final.Health, Is.EqualTo(new HealthStateSnapshot(90, 100)));
            Assert.That(returned.Events.Any(e => e.Kind == CombatEventKind.HealthChanged), Is.False);
        }

        [Test]
        public void MainPlacement_NormalizationIncludesPersistentCurrentHealth()
        {
            var input = CombatCommonCommandTests.Command(CombatCommandKind.PlaceMainCharacter);
            var s = input.State;
            var cell = new CellCoord(2, 1);
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, CombatFlowPhase.PlayerInput, EntityId.Empty,
                false, s.Structures, s.ThornFields, s.Timeline, new CombatTurnFacts(StagePhase.Deployment),
                new MovementEnvironmentFacts(new MovementTileFact[] { Tile(7, cell, 0, -10) }));
            input = new CombatCommandInput(1, input.Kind, input.ActorId, default, state, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                targetCell: cell, battle: new CombatBattleFacts(10, 10, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            var placed = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(placed.Accepted, Is.True, placed.Reason);
            Assert.That(placed.Characters.Characters.Single().Final.Health, Is.EqualTo(new HealthStateSnapshot(90, 90)));
            Assert.That(placed.Characters.Main.Expected, Is.SameAs(input.Main));
            Assert.That(placed.Characters.Main.Final.CurrentHealth, Is.EqualTo(90));
            Assert.That(placed.Characters.Main.Final.PersistentMaxHealth, Is.EqualTo(100));
            Assert.That(placed.Events.Any(e => e.Kind == CombatEventKind.HealthChanged), Is.False);
        }

        private static DestinationTileModifierFact Tile(long id, CellCoord cell, int attack, int health) =>
            new(id, cell, true, new[] { Owner.Player }, attack, health);

        private static CombatCommandInput RouteWithTiles(int hp, params MovementTileFact[] tiles)
        {
            var input = MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 1), new CellCoord(2, 1));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var changed = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey,
                actor.Owner, actor.OwnershipRole, actor.Area, new HealthStateSnapshot(hp, 10), actor.Stats,
                actor.ReserveSlotIndex, actor.Anchor, actor.Rotation, actor.OccupiedCells,
                actor.MovementCheckpointAnchor, actor.CommittedMoveDistance, actor.PendingMoveActionCost);
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                s.Structures, s.ThornFields, s.Timeline, s.Turn, new MovementEnvironmentFacts(tiles));
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state,
                input.Characters.Select(c => c.EntityId == actor.EntityId ? changed : c).ToArray(),
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                input.TargetCell, input.Rotation, input.Battle, input.ProcessingKind, input.Path);
        }
    }
}
