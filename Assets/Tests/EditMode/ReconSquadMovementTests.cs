using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ReconSquadMovementTests
    {
        [Test]
        public void MovementProducers_AreAvailable()
        {
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(TraitKey.ReconSquad), Is.True);
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(TraitKey.PositionSwap), Is.True);
        }

        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void StartAndReachedPath_GrantOncePerEnemy_WithCrossGeometry(int percent)
        {
            var input = Route(percent);
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            foreach (int id in new[] { 2, 3, 4 })
            {
                var grant = Grants(TraitCombatFixtures.Final(move, id)).Single();
                Assert.That(grant.IncreasePercent, Is.EqualTo(percent));
                Assert.That(grant.LifetimeKind, Is.EqualTo(WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath));
                Assert.That(grant.RemainingAffectedSideTurnEnds, Is.Null);
                Assert.That(grant.Movement.Value.ActorId, Is.EqualTo(input.ActorId));
            }
            Assert.That(Grants(TraitCombatFixtures.Final(move, 5)), Is.Empty);
            Assert.That(Grants(MovementTestData.Actor(move)), Is.Empty);
            Assert.That(move.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.ReconSquad).SelectMany(t => t.TargetIds),
                Is.EquivalentTo(new[] { new EntityId(2), new EntityId(3), new EntityId(4) }));
            Assert.That(input.Characters.SelectMany(Grants), Is.Empty);
        }

        [Test]
        public void ArrivalDeath_StopsScanButPreservesStartingGrant()
        {
            var input = Route();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithHealth(new HealthStateSnapshot(3, 100)) : c).ToArray(), state: input.State.With(
                thornFields: new[] { new ThornFieldCellSnapshot(new CellCoord(2, 1), 3, 4) }));
            var move = MovementUndoTestData.Run(input, new ThornMovementTileRule());
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(Grants(TraitCombatFixtures.Final(move, 2)).Count(), Is.EqualTo(1));
            Assert.That(Grants(TraitCombatFixtures.Final(move, 3)), Is.Empty);
            Assert.That(Grants(TraitCombatFixtures.Final(move, 4)), Is.Empty);
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void LeapIntermediateCell_GrantsToLivingHostileOnly()
        {
            var input = Route();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, Spec(), new LeapTraitSpec());
            input = TraitTrailTestData.Environment(input, new StaticTerrainFact(70, new CellCoord(2, 1),
                StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }));
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(Grants(TraitCombatFixtures.Final(move, 3)).Count(), Is.EqualTo(1));
        }

        [Test]
        public void EnemyVoluntaryMove_GrantsWithoutPlayerUndoToken()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(4, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, Spec());
            var move = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(move, Is.Not.Null);
            var grants = CombatCommandComposition.Project(move).Characters.SelectMany(Grants).ToArray();
            Assert.That(grants, Is.Not.Empty);
            Assert.That(grants.All(g => !g.Movement.HasValue), Is.True);
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void ArrivalDamagePrecedesGrant_AndIrreversibleMoveStillGrants()
        {
            var input = TraitTrailTestData.Environment(Route(), new ReconHazard(new CellCoord(2, 1)));
            var result = MovementUndoTestData.Run(input, new ReconHazardRule());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(Grants(TraitCombatFixtures.Final(result, 3)).Count(), Is.EqualTo(1));
            Assert.That(result.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.ExposureDamage), Is.False);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void ForcedDisplacementAndArrival_DoNotGrant()
        {
            var input = Route();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            var before = projection.Actor;
            projection.RecordForcedMove(before, CombatMovementCalculator.MoveTo(before, new CellCoord(3, 1)));
            projection.ResolveForcedArrival(before.EntityId);
            var result = projection.Complete();
            Assert.That(CombatCommandComposition.Project(result).Characters.SelectMany(Grants), Is.Empty);
        }

        private sealed class ReconHazard : MovementTileFact
        {
            internal ReconHazard(CellCoord cell) : base(91, cell, "recon-fixture", 0, true, new[] { Owner.Player, Owner.Enemy })
            {
            }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class ReconHazardRule : IMovementTileRule
        {
            public string Key => "recon-fixture";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { new EntityId(3) };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => input.Phase == MovementEffectPhase.Hazard
                ? new MovementStepEffects(damage: new[] { new MovementHealthRequest(new EntityId(3), 10) }) : new MovementStepEffects();
        }

        internal static CombatCommandInput Route(int percent = 10, int radius = 1)
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1));
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(1, 2), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy),
                CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(4, 2), Owner.Enemy)
            }).ToArray());
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, Spec(percent, radius));
        }

        internal static TraitSpec Spec(int percent = 10, int radius = 1) => new ReconSquadTraitSpec(percent, radius);

        internal static System.Collections.Generic.IEnumerable<WeaknessExposureStatus> Grants(CharacterCombatFacts c) =>
            c.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Where(s => s.Origin.Value.DefinitionKey == (int)TraitKey.ReconSquad);
    }
}