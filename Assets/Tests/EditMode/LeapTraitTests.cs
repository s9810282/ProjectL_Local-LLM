using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class LeapTraitTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void TornadoEntry_UsesEntrantAfterPresenceReconciliation(bool leap)
        {
            var input = leap ? Barrier(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)), 2)
                : MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right);
            if (!leap)
                input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                    CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 0), Owner.Enemy)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo,
                leap ? new TraitSpec[] { new LeapTraitSpec(), new SoloActionTraitSpec(10, 1, 0, 1) }
                    : new TraitSpec[] { new SoloActionTraitSpec(10, 1, 0, 1) });
            input = TraitTrailTestData.Environment(input, TornadoEnvironmentTests.Source(cell: CellCoord.Right));
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var relocation = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated);
            Assert.That(relocation.Expected.EntityId, Is.EqualTo(input.ActorId));
            Assert.That(relocation.Expected.TraitEffects.ActiveShieldTotal, Is.EqualTo(leap ? 10 : 0));
            Assert.That(preview.ProjectedActor.SameValues(MovementTestData.Actor(result)), Is.True);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.ForcedRelocation));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Leap_OccupiedTornado_UsesExplicitEntrant(bool hidden)
        {
            var input = Barrier(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)), 2);
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec(),
                new GroundCompactionTraitSpec(1, 1), new AccelerationTraitSpec(2));
            input = TraitTrailTestData.Environment(input, new TornadoSourceFact(31, CellCoord.Right,
                hidden ? new[] { Owner.Enemy } : new[] { Owner.Player, Owner.Enemy }));
            var before = input.State;
            var crossed = input.Characters.Single(c => c.EntityId == new EntityId(2));
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var repeated = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(input.State.SameValues(before), Is.True);
            Assert.That(input.State.EnvironmentProgress.RandomState, Is.EqualTo(before.EnvironmentProgress.RandomState));
            Assert.That(input.Characters.Single(c => c.EntityId == crossed.EntityId).SameValues(crossed), Is.True);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == crossed.EntityId).SameValues(crossed), Is.True);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated).Final.EntityId,
                Is.EqualTo(input.ActorId));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.EnteredCells, Is.EqualTo(new[] { CellCoord.Right }));
            // 토네이도가 (1,0)에서 옮겨 이동이 끝났으므로 지나간 칸이 없어 흔적도 없다.
            Assert.That(TraitTrailTestData.Trails(result.FinalState.Environment), Is.Empty);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(repeated.CanMove, Is.EqualTo(preview.CanMove));
            Assert.That(repeated.PathMovement.FinalStop, Is.EqualTo(preview.PathMovement.FinalStop));
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.ForcedRelocation));
            Assert.That(preview.PathMovement.FinalStop, Is.EqualTo(hidden ? new CellCoord(2, 0) : result.PathMovement.FinalStop));
            Assert.That(preview.PublicEffects.OfType<CombatTornadoEntryEvent>().Count(), Is.EqualTo(hidden ? 0 : 1));
            Assert.That(MovementTestData.Actor(result).Stats.TraitStatuses.ToArray()
                .OfType<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.AccelerationTraitStatusSnapshot>()
                .Single().HeldAttackBonus, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void InterruptedOnUnlandableCell_RejectsWholeProposedMove(int obstacle)
        {
            var input = Barrier(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)), obstacle);
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec(), new GroundCompactionTraitSpec(1, 1));
            input = TraitTrailTestData.Environment(input, input.State.Environment.Tiles.Append(new StopTile(80, CellCoord.Right, new[] { Owner.Player })).ToArray());
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new StopRule() });
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DestinationOccupied"));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void CrossesWallCliffAndUnit_WithNormalLanding(int obstacle)
        {
            var normal = Barrier(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)), obstacle);
            Assert.That(CombatMovementCalculator.Preview(normal, Array.Empty<IMovementTileRule>()).CanMove, Is.False);
            var input = TraitCombatFixtures.Equip(normal, EntityKey.Unit_Akimbo, new LeapTraitSpec());
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Destinations.ToArray().Select(d => d.TargetAnchor), Has.Member(new CellCoord(2, 0)));
            Assert.That(range.Destinations.ToArray().Select(d => d.TargetAnchor), Has.No.Member(CellCoord.Right));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(2, 0)));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(2));
            Assert.That(result.FinalState.Environment.SameValues(normal.State.Environment), Is.True);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void CannotLandOnWallCliffOrUnit(int obstacle)
        {
            var input = Barrier(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right), obstacle);
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec());
            Assert.That(CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanMove, Is.False);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void IntermediateMine_IgnoredAndDestinationMineTriggers(bool hidden)
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec());
            input = TraitTrailTestData.Environment(input,
                MineEnvironmentTests.Mine(id: 11, ordinal: 1, damage: 40, hidden: hidden, cell: CellCoord.Right),
                MineEnvironmentTests.Mine(id: 12, ordinal: 2, damage: 7, cell: new CellCoord(2, 0)));
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 11).Active, Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 12).Active, Is.False);
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Trail_SkipsWallCliffCells(int obstacle)
        {
            var input = Barrier(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0),new CellCoord(3, 0)), obstacle);
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec(), new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var trails = TraitTrailTestData.Trails(result.FinalState.Environment);
            // 지나간 칸은 (1,0)·(2,0)이고, 벽·낭떠러지인 (1,0)은 건너뛴다. 출발·도착 칸에는 없다.
            Assert.That(trails.Length, Is.EqualTo(2));
            Assert.That(trails.Select(t => t.Cell), Has.No.Member(CellCoord.Right));
            Assert.That(trails.Select(t => t.Cell), Is.EquivalentTo(new[] { new CellCoord(2, 0), new CellCoord(2, 0) }));
        }

        [Test]
        public void NonMineHazard_InterruptsAndRecordsOnlyCompletedPath()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0),new CellCoord(3, 0));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec(), new GroundCompactionTraitSpec(1, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithHealth(new HealthStateSnapshot(3, 100)) : c).ToArray(), state: EnvironmentDamageTests.WithDamageTiles(input.State, (CellCoord.Right, 4)));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.EnteredCells, Is.EqualTo(new[] { CellCoord.Right }));
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
            // 멈춘 칸 (1,0)은 도착 칸이라 흔적이 없다.
            Assert.That(TraitTrailTestData.Trails(result.FinalState.Environment), Is.Empty);
        }

        [Test]
        public void RepeatedCells_StillRejectWithoutAllocating()
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,CellCoord.Zero);
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new LeapTraitSpec(), new GroundCompactionTraitSpec(1, 1));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("RepeatedCell"));
            Assert.That(result.FinalState.EnvironmentProgress.SameValues(input.State.EnvironmentProgress), Is.True);
        }

        private sealed class StopTile : MovementTileFact
        {
            internal StopTile(long id, CellCoord cell, IReadOnlyList<Owner> knownTo) : base(id, cell, "fixture-leap-stop", 0, true, knownTo) { }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => new StopTile(Id, Cell, knownTo);
        }

        private sealed class StopRule : IMovementTileRule
        {
            public string Key => "fixture-leap-stop";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) => input.Phase == MovementEffectPhase.Hazard
                ? new MovementStepEffects(movementMethods: new[] { new MovementMethodChange(input.MoverId, input.Stats.MoveMethod, MoveMethod.None) })
                : new MovementStepEffects();
        }

        private static CombatCommandInput Barrier(CombatCommandInput input, int obstacle)
        {
            if (obstacle == 2) return TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, CellCoord.Right, Owner.Enemy)).ToArray());
            return TraitTrailTestData.Environment(input, new StaticTerrainFact(70, CellCoord.Right,
                obstacle == 0 ? StaticTerrainKind.Wall : StaticTerrainKind.Cliff, true, new[] { Owner.Player, Owner.Enemy }));
        }
    }
}
