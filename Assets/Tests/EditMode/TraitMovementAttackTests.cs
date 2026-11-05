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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class TraitMovementAttackTests
    {
        [Test]
        public void CompletedThreeSteps_AddSixWithoutChangingBaseAttack()
        {
            var input = Route();
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanMove, Is.True);
            var move = TraitCombatFixtures.Attack(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var actor = MovementTestData.Actor(move);
            Assert.That(Held(actor), Is.EqualTo(6));
            Assert.That(Moved(actor), Is.True);
            Assert.That(actor.Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(actor.Stats.AttackPower, Is.EqualTo(before.Stats.AttackPower));
            Assert.That(Held(before), Is.Zero);
            Assert.That(Moved(before), Is.False);
        }

        [Test]
        public void InterruptedMove_CountsOnlyCompletedSteps()
        {
            var input = TraitTrailTestData.Environment(Route(), new StopTile(80, new CellCoord(1, 3), new[] { Owner.Player }));
            var move = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new StopRule() });
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(Held(MovementTestData.Actor(move)), Is.EqualTo(2));
            Assert.That(Moved(MovementTestData.Actor(move)), Is.True);
        }

        [Test]
        public void RejectedMovement_AddsNothing()
        {
            var input = MovementTestData.Route(Route(), new CellCoord(0, 3), new CellCoord(1, 3), new CellCoord(0, 3));
            var move = TraitCombatFixtures.Attack(input);
            Assert.That(move.Accepted, Is.False);
            Assert.That(Held(input.Characters.Single(c => c.EntityId == input.ActorId)), Is.Zero);
            Assert.That(move.Characters.Characters, Is.Empty);
        }

        [Test]
        public void Acceleration_SurvivesTurnsAndOwnStartResetsOnlyStationary()
        {
            var input = MovementTestData.After(TraitCombatFixtures.Attack(Route()));
            var ended = CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>());
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            var next = CombatCommandTestData.NextPlayerTurn(MovementTestData.After(ended));
            var actor = next.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(Held(actor), Is.EqualTo(6));
            Assert.That(actor.Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(Moved(actor), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Stationary_IsDamageMultiplierAndRemembersMovementAfterAttack(bool moved)
        {
            var input = SingleTarget();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new StationaryFireTraitSpec(100));
            if (moved) input = WithState(input, 1, 0, true);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(moved ? 20 : 40));
            var actor = TraitCombatFixtures.Final(result, 1);
            Assert.That(Moved(actor), Is.EqualTo(moved));
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(actor.Stats.TraitAttackBonus, Is.Zero);
        }

        [Test]
        public void WholeMultiHitAction_UsesCapturedBonusThenConsumesOnce()
        {
            var input = TraitCombatFixtures.Equip(SingleTarget(), EntityKey.Unit_Akimbo,
                new AccelerationTraitSpec(2), new KillChainAttackTraitSpec());
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(26, 100)) : c).Append(
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(7, 0), Owner.Enemy)).ToArray());
            input = WithState(input, 1, 6, false);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(26));
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(74));
            Assert.That(Held(TraitCombatFixtures.Final(result, 1)), Is.Zero);
            Assert.That(TraitCombatFixtures.Final(result, 1).Stats.TraitAttackBonus, Is.Zero);
            // 처치 연쇄 추가 공격도 공격 행동으로 센다(2026-11-02 결정). 가속은 그래도 한 번만 소모된다.
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount,
                Is.EqualTo(1 + TraitActivationOutcomeTests.Activations(result.Events, TraitKey.KillChainAttack).Count()));
        }

        [Test]
        public void LaterOrdinaryAction_DoesNotReuseHeldBonus()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy).WithHealth(new HealthStateSnapshot(1, 100))).Append(
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new AccelerationTraitSpec(2));
            input = WithState(input, 1, 6, false);
            var first = TraitCombatFixtures.Attack(input);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(TraitCombatFixtures.Final(first, 3).Health.CurrentHealth, Is.EqualTo(74));
            Assert.That(Held(TraitCombatFixtures.Final(first, 1)), Is.Zero);
            var second = TraitCombatFixtures.Attack(CombatCommandTestData.NextPlayerTurn(MovementTestData.After(first)));
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(TraitCombatFixtures.Final(second, 3).Health.CurrentHealth, Is.EqualTo(54));
            Assert.That(Held(TraitCombatFixtures.Final(second, 1)), Is.Zero);
            Assert.That(second.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ForceMovement_AddsZeroAndDoesNotBreakStationary(bool friendly)
        {
            var input = TraitCombatFixtures.Equip(PullTestData.Input(owner: friendly ? Owner.Player : Owner.Enemy),
                EntityKey.Unit_Demon, new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100));
            var original = input.Characters.Single(c => c.EntityId == PullTestData.Target);
            var result = PullTestData.Run(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var target = PullTestData.Final(result);
            Assert.That(target.Anchor, Is.Not.EqualTo(original.Anchor));
            Assert.That(Held(target), Is.Zero);
            Assert.That(Moved(target), Is.False);
        }

        [Test]
        public void PreviewAndRejectedAttack_DoNotConsumeCommittedBonus()
        {
            var input = TraitCombatFixtures.Equip(SingleTarget(), EntityKey.Unit_Akimbo, new AccelerationTraitSpec(2));
            input = WithState(input, 1, 6, false);
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(26));
            var rejected = TraitCombatFixtures.Attack(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(Held(before), Is.EqualTo(6));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).SameValues(before), Is.True);
        }

        [Test]
        public void StageEnd_ClearsHeldBonusAndMovementButPreservesOtherAttackOwner()
        {
            var input = WithState(TraitCombatFixtures.Equip(SingleTarget(), EntityKey.Unit_Akimbo,
                new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100)), 1, 6, true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            actor = actor.With(stats: actor.Stats.With(traitAttackBonus: 13));
            var cleared = CombatTemporaryStateCleanup.Clear(actor);
            Assert.That(Held(cleared), Is.Zero);
            Assert.That(Moved(cleared), Is.False);
            Assert.That(cleared.Stats.TraitAttackBonus, Is.EqualTo(7));
        }

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void OwnStart_ResetsOnlyCurrentSideIncludingInactiveRecords(Owner side)
        {
            var input = TraitCombatFixtures.Equip(SingleTarget(), EntityKey.Unit_Akimbo,
                new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100));
            input = WithState(WithState(input, 1, 6, true), 2, 4, true);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()) : c).ToArray());
            var starting = EnemyControllerPhaseTests.Input(input, side == Owner.Player ? CombatControllerPhase.StartingPlayer : CombatControllerPhase.StartingEnemy);
            var result = CombatTurnCalculator.BeginControllerTurn(starting);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Moved(TraitCombatFixtures.Final(result, 1)), Is.EqualTo(side != Owner.Player));
            Assert.That(Moved(TraitCombatFixtures.Final(result, 2)), Is.EqualTo(side != Owner.Enemy));
            Assert.That(Held(TraitCombatFixtures.Final(result, 1)), Is.EqualTo(6));
            Assert.That(Held(TraitCombatFixtures.Final(result, 2)), Is.EqualTo(4));
        }

        [Test]
        public void EnemyVoluntaryMove_UsesSameCompletedStepOwner()
        {
            var input = TraitCombatFixtures.Equip(EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(4, 1)),
                EntityKey.Unit_Demon, new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100));
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(Held(MovementTestData.Actor(result)), Is.EqualTo(result.PathMovement.DistanceSpent * 2));
            Assert.That(Moved(MovementTestData.Actor(result)), Is.True);
        }

        [Test]
        public void Death_ClearsHeldOwnerAtExistingCommandCompletion()
        {
            var input = WithState(Route(), 1, 6, true);
            input = CombatCommandTestData.With(input, state: input.State.With(
                thornFields: new[] { new ThornFieldCellSnapshot(new CellCoord(1, 3), remainingUnitTurns: 1, thornDamage: 100) }));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new ThornMovementTileRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            var actor = MovementTestData.Actor(result);
            Assert.That(actor.Health.IsDead, Is.True);
            Assert.That(Held(actor), Is.Zero);
            Assert.That(Moved(actor), Is.False);
            Assert.That(actor.Stats.TraitAttackBonus, Is.Zero);
        }

        internal static CombatCommandInput Route() => TraitCombatFixtures.Equip(MovementTestData.SafeRoute(            new CellCoord(0, 3),new CellCoord(1, 3),new CellCoord(2, 3),new CellCoord(3, 3)), EntityKey.Unit_Akimbo,
            new AccelerationTraitSpec(2), new StationaryFireTraitSpec(100));

        internal static CombatCommandInput SingleTarget()
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            return TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3 && c.EntityId.Value != 4).ToArray());
        }

        internal static int Held(CharacterCombatFacts actor) => (int)Value(actor, TraitKey.Acceleration, "HeldAttackBonus");
        internal static bool Moved(CharacterCombatFacts actor) => (bool)Value(actor, TraitKey.StationaryFire, "MovedThisOwnTurn");
        private static object Value(CharacterCombatFacts actor, TraitKey key, string name)
        {
            var state = actor.Stats.TraitStatuses.ToArray().Single(t => t.Key == key);
            var property = state.GetType().GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return property.GetValue(state);
        }

        internal static CombatCommandInput WithState(CombatCommandInput input, int id, int held, bool moved)
        {
            return CombatCommandTestData.With(input, characters: input.Characters.Select(c =>
            {
                if (c.EntityId.Value != id) return c;
                var traits = c.Stats.TraitStatuses.ToArray().Select(t => t.Key == TraitKey.Acceleration
                    ? (TraitStatusSnapshot)Activator.CreateInstance(t.GetType(), held)
                    : t.Key == TraitKey.StationaryFire ? (TraitStatusSnapshot)Activator.CreateInstance(t.GetType(), new object[] { moved }) : t).ToArray();
                return c.With(stats: c.Stats.With(traitStatuses: traits, traitAttackBonus: held));
            }).ToArray());
        }

        private sealed class StopTile : MovementTileFact
        {
            internal StopTile(long id, CellCoord cell, IReadOnlyList<Owner> knownTo) : base(id, cell, "fixture-acceleration-stop", 0, true, knownTo) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new StopTile(Id, Cell, knownTo);
        }

        private sealed class StopRule : IMovementTileRule
        {
            public string Key => "fixture-acceleration-stop";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => input.Phase == MovementEffectPhase.Hazard
                ? new MovementStepEffects(movementMethods: new[] { new MovementMethodChange(input.MoverId, input.Stats.MoveMethod, MoveMethod.None) })
                : new MovementStepEffects();
        }
    }
}
