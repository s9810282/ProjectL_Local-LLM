using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class PositionSwapMovementTests
    {
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(18)]
        public void SuccessfulSwap_GrantsAuthoredAmountToBothParticipants(int amount)
        {
            var input = TraitCombatFixtures.Equip(Route(), EntityKey.Unit_Akimbo, new PositionSwapTraitSpec(amount));
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            foreach (int id in new[] { 1, 20 })
                Assert.That(TraitCombatFixtures.Final(move, id).TraitEffects.ActiveShieldTotal, Is.EqualTo(amount));
        }

        [Test]
        public void EnemySwap_ExpiresOnEnemyStartAndPreservesOtherShield()
        {
            var input = EnemyMovementPathTests.Input();
            var cell = new CellCoord(2, 1);
            var passenger = CombatCommandTestData.Character(20, EntityKey.Unit_Demon, cell, Owner.Enemy);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(passenger).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new PositionSwapTraitSpec(18));
            var path = new MovementPath(new[] { new CellCoord(1, 1), cell });
            var move = CombatMovementCalculator.Calculate(input.WithIntent(CombatCommandKind.Move, default, cell, path, null),
                Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            var current = CombatCommandComposition.Project(move);
            var projected = current.Characters.ToDictionary(c => c.EntityId);
            foreach (int id in new[] { 1, 20 })
                Assert.That(projected[new EntityId(id)].TraitEffects.ActiveShieldTotal, Is.EqualTo(18));
            TraitTurnLifecycle.BeginControllerTurn(current, projected, Owner.Player);
            var shielded = projected[passenger.EntityId];
            Assert.That(shielded.TraitEffects.ActiveShieldTotal, Is.EqualTo(18));
            var unrelated = new EffectOriginKey(EffectOwnerKind.Trait, 99, (int)TraitKey.Cohesion, "main");
            projected[passenger.EntityId] = shielded.WithStatusEffects(ShieldStatusRules.Restore(shielded.StatusEffects,
                unrelated, new ShieldStatus(unrelated, 5, true, 0)));
            var returned = AppliedStatusRules.ClearForReserve(projected[passenger.EntityId].StatusEffects, keepShieldBudget: true);
            Assert.That(returned.Entries.OfType<ShieldStatus>().Single().Origin, Is.EqualTo(unrelated));
            TraitTurnLifecycle.BeginControllerTurn(current, projected, Owner.Enemy);
            Assert.That(projected[passenger.EntityId].TraitEffects.ActiveShieldTotal, Is.EqualTo(5));
            Assert.That(projected[new EntityId(1)].TraitEffects.ActiveShieldTotal, Is.Zero);
        }

        [Test]
        public void AllyEndpoint_PreviewMatchesExecute_AndPassengerKeepsFacingActionAndDistance()
        {
            var input = Route();
            var passenger = input.Characters.Single(c => c.EntityId.Value == 20);
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(CombatMovementCalculator.Range(input).Destinations.ToArray().Any(d => d.TargetAnchor == new CellCoord(3, 1)), Is.True);
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(MovementTestData.Actor(move).Anchor, Is.EqualTo(new CellCoord(3, 1)));
            var after = TraitCombatFixtures.Final(move, 20);
            Assert.That(after.SameValues(passenger.WithPlacement(passenger.Area, null, new CellCoord(1, 1), Rotation4.R90,
                new[] { new CellCoord(1, 1) })), Is.True);
            Assert.That(MovementTestData.Actor(move).StageRecord.DistanceMoved, Is.EqualTo(2));
            var forced = move.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId.Value == 20);
            Assert.That(forced.MovementCause.Agency, Is.EqualTo(CombatMovementAgency.Forced));
            Assert.That(forced.MovementCause.TraitKey, Is.EqualTo(TraitKey.PositionSwap));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EnemyOrUnsupportedFootprint_RejectsBeforeSpend(bool enemy)
        {
            var input = Route();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value != 20 ? c : enemy
                ? CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(3, 1), Owner.Enemy)
                : c.WithPlacement(c.Area, null, c.Anchor, c.Rotation, new[] { new CellCoord(3, 1), new CellCoord(3, 2) })).ToArray());
            Assert.That(MovementUndoTestData.Run(input).Accepted, Is.False);
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void ReplacementPassenger_RejectsStaleContext()
        {
            var input = Route();
            var context = MovementContextCalculator.Capture(input, Owner.Player, Array.Empty<IMovementTileRule>());
            Assert.That(context, Is.Not.Null);
            var replaced = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 20).Append(
                CombatCommandTestData.Character(21, EntityKey.Unit_Demon, new CellCoord(3, 1), Owner.Player)).ToArray());
            Assert.That(MovementContextCalculator.Matches(context, replaced, Owner.Player, Array.Empty<IMovementTileRule>()), Is.False);
            var accepted = MovementUndoTestData.Run(input.WithIntent(input.Kind, input.Aim, input.TargetCell, input.Path, context));
            Assert.That(accepted.Accepted, Is.True, accepted.Reason);
        }

        [Test]
        public void BlockedReversePlacement_RejectsBeforeSpend()
        {
            var input = TraitTrailTestData.Environment(Route(), new StaticTerrainFact(80, new CellCoord(1, 1),
                StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }));
            Assert.That(MovementUndoTestData.Run(input).Accepted, Is.False);
        }

        [Test]
        public void IntermediateStop_LeavesPassengerUntouched()
        {
            var input = TraitTrailTestData.Environment(Route(), new SwapHazard(new CellCoord(2, 1)));
            var move = MovementUndoTestData.Run(input, new SwapHazardRule(stop: true));
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(move).Anchor, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(TraitCombatFixtures.Final(move, 20).SameValues(input.Characters.Single(c => c.EntityId.Value == 20)), Is.True);
        }

        [Test]
        public void OnlyInitiatorTriggersVoluntaryTraits()
        {
            var input = Route();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, Spec(), new AccelerationTraitSpec(3), new GroundCompactionTraitSpec(1, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new AccelerationTraitSpec(7), new ReconSquadTraitSpec(), new DustScreenTraitSpec(1, 20, 1));
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(MovementTestData.Actor(move).Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().Single().HeldAttackBonus, Is.EqualTo(6));
            Assert.That(TraitCombatFixtures.Final(move, 20).Stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().Single().HeldAttackBonus, Is.Zero);
            Assert.That(move.FinalState.Environment.Tiles.OfType<TraitTrailFact>().All(t => t.Origin.OwnerInstanceId == input.ActorId.Value), Is.True);
            Assert.That(move.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.TraitKey == TraitKey.ReconSquad), Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EndpointEffects_RunOnceInEntityOrder_AfterBothPlacements(bool terminal)
        {
            var input = Route();
            var origin = new CellCoord(1, 1);
            var endpoint = new CellCoord(3, 1);
            input = TraitTrailTestData.Environment(input, new SwapHazard(origin), new SwapHazard(endpoint));
            var rule = new SwapHazardRule(terminal: terminal);
            var move = MovementUndoTestData.Run(input, rule);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(rule.Arrivals, Is.EqualTo(terminal ? new[] { 1 } : new[] { 1, 20 }));
            Assert.That(rule.SawSimultaneousPositions, Is.True);
            Assert.That(TraitCombatFixtures.Final(move, 20).Anchor, Is.EqualTo(origin));
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void EarlierEndpointRelocation_DoesNotProcessPassengerAtAnotherCellAgain()
        {
            var input = TraitTrailTestData.Environment(Route(), new SwapHazard(new CellCoord(3, 1)), new SwapHazard(new CellCoord(1, 2)));
            var rule = new RelocationRule();
            var result = MovementUndoTestData.Run(input, rule);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 20).Anchor, Is.EqualTo(new CellCoord(1, 2)));
            Assert.That(rule.OtherCellVisits, Is.Zero);
        }

        internal static TraitSpec Spec() => new PositionSwapTraitSpec();

        [Test]
        public void PassengerEndpointMine_UsesFinalPairPositions_AndRemainsIrreversible()
        {
            var input = TraitTrailTestData.Environment(Route(), MineEnvironmentTests.Mine(cell: new CellCoord(1, 1)));
            var result = MovementUndoTestData.Run(input, new MineMovementTileRule());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(TraitCombatFixtures.Final(result, 20).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Single().ActivatorId, Is.EqualTo(new EntityId(20)));
            Assert.That(result.FinalState.Environment.Tiles.Single().Active, Is.False);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        internal static CombatCommandInput Route(UnitActionUse passengerAction = UnitActionUse.Attacked)
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1));
            var passenger = CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(3, 1), Owner.Player,
                actionUse: passengerAction).WithPlacement(CharacterRuntimeArea.Field, null, new CellCoord(3, 1), Rotation4.R90, new[] { new CellCoord(3, 1) })
                .With(stageRecord: CharacterStageRecord.Empty.Add(distanceMoved: 7));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(passenger).ToArray());
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, Spec());
        }

        private sealed class SwapHazard : MovementTileFact
        {
            internal SwapHazard(CellCoord cell) : base(90 + cell.X, cell, "swap-hazard", 0, true, new[] { Owner.Player, Owner.Enemy }) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class SwapHazardRule : IMovementTileRule
        {
            private readonly bool _stop;
            private readonly bool _terminal;
            internal readonly List<int> Arrivals = new();
            internal bool SawSimultaneousPositions = true;
            internal SwapHazardRule(bool stop = false, bool terminal = false) { _stop = stop; _terminal = terminal; }
            public string Key => "swap-hazard";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput input)
            {
                if (!_stop)
                    SawSimultaneousPositions &= input.Characters.Single(c => c.EntityId.Value == 1).Anchor == new CellCoord(3, 1) &&
                        input.Characters.Single(c => c.EntityId.Value == 20).Anchor == new CellCoord(1, 1);
                return new[] { _terminal ? new EntityId(10) : mover };
            }
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                if (input.Phase != MovementEffectPhase.Hazard) return new MovementStepEffects();
                Arrivals.Add(input.MoverId.Value);
                return _stop ? new MovementStepEffects(movementMethods: new[] { new MovementMethodChange(input.MoverId, input.Stats.MoveMethod, MoveMethod.None) })
                    : new MovementStepEffects(damage: new[] { new MovementHealthRequest(_terminal ? new EntityId(10) : input.MoverId, _terminal ? 100 : 1) });
            }
        }

        private sealed class RelocationRule : IMovementTileRule
        {
            internal int OtherCellVisits;
            public string Key => "swap-hazard";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { new EntityId(20) };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                if (input.Phase != MovementEffectPhase.Hazard) return new MovementStepEffects();
                if (tile.Cell == new CellCoord(3, 1))
                    return new MovementStepEffects(forcedMovement: new[] { new MovementForcedMoveRequest(new EntityId(20), Rotation4.R0, 1) });
                OtherCellVisits++;
                return new MovementStepEffects();
            }
        }
    }
}
