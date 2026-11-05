using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class ConnectedWallCombatTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(false)]
        [TestCase(true)]
        public void Movement_AdmitsBentWallVoidAndRejectsOccupiedArm(bool intoWall)
        {
            var start = new CellCoord(1, 3);
            var target = intoWall ? new CellCoord(3, 3) : new CellCoord(2, 3);
            var path = intoWall ? new[] { start, new CellCoord(2, 3), target } : new[] { start, target };
            var input = MovementTestData.SafeRoute(path);
            input = WallMovementTests.WithWalls(input, BentWalls());
            Assert.That(MovementRouteValidator.Validate(input, input.Path, out _),
                Is.EqualTo(intoWall ? MovementFailureReason.DestinationOccupied : MovementFailureReason.None));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.EqualTo(!intoWall), result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(intoWall ? start : target));
            if (intoWall) Assert.That(result.FinalState, Is.SameAs(input.State));
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void AttackRangePreviewAndExecution_AgreeForVoidAndArm(bool blocked, bool reverse)
        {
            var a = new CellCoord(1, 3);
            var b = blocked ? new CellCoord(4, 3) : new CellCoord(2, 3);
            var actorCell = reverse ? b : a;
            var targetCell = reverse ? a : b;
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForCell(targetCell), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], position: actorCell);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, targetCell, Owner.Enemy);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1], target });
            input = WallMovementTests.WithWalls(input, BentWalls());
            var range = CombatCommandCalculator.GetAttackRange(input);
            var preview = CombatCommandCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(range.AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(!blocked));
            Assert.That(preview.CanAttack, Is.EqualTo(!blocked));
            Assert.That(result.Accepted, Is.EqualTo(!blocked), result.Reason);
            if (blocked)
            {
                Assert.That(preview.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
                Assert.That(preview.BlockedContacts.Single().WallSourceIds, Is.EqualTo(new long[] { 71, 72, 73 }));
                Assert.That(result.BlockedContacts.Single().WallSourceIds, Is.EqualTo(new long[] { 71, 72, 73 }));
                Assert.That(result.FinalState, Is.SameAs(input.State));
                Assert.That(result.Events, Is.Empty);
            }
            else
            {
                Assert.That(preview.BlockedContacts, Is.Empty);
                Assert.That(result.Attack.Value.Hits.ToArray().Single().TargetId, Is.EqualTo(target.EntityId));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PropagationPolicy_DistinguishesVoidArmAndOccupiedWall(bool ignore)
        {
            var geometry = ConnectedWallGeometryTests.Build(BentWalls());
            var cells = new[] { new CellCoord(2, 3), new CellCoord(4, 3), new CellCoord(3, 3) };
            var policy = new AttackOcclusionSpec(AttackWallDelivery.IgnoreWalls,
                ignore ? AttackWallPropagation.IgnoreWalls : AttackWallPropagation.FromImpact);
            var result = AttackOcclusionResolver.ResolvePropagationCells(new CellCoord(1, 3), cells, geometry, policy);
            Assert.That(result.Cells, Is.EqualTo(ignore ? cells.Take(2).ToArray() : cells.Take(1).ToArray()));
            Assert.That(result.BlockedContacts.Count, Is.EqualTo(ignore ? 1 : 2));
            foreach (var contact in result.BlockedContacts)
            {
                Assert.That(contact.Phase, Is.EqualTo(AttackOcclusionPhase.Propagation));
                Assert.That(contact.WallSourceIds, Is.EqualTo(new long[] { 71, 72, 73 }));
            }
            Assert.That(AttackOcclusionResolver.CheckDirect(new CellCoord(1, 3), new CellCoord(4, 3), geometry, new AttackElevation(Array.Empty<CellCoord>()), policy), Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resolver_SelectsFirstBentComponentBeforeMinimumId(bool reverse)
        {
            var facts = new[]
            {
                ConnectedWallGeometryTests.Wall(200, new CellCoord(2, 1)),
                ConnectedWallGeometryTests.Wall(201, new CellCoord(3, 1)),
                ConnectedWallGeometryTests.Wall(202, new CellCoord(3, 2)),
                ConnectedWallGeometryTests.Wall(10, new CellCoord(6, 1)),
                ConnectedWallGeometryTests.Wall(11, new CellCoord(7, 1)),
                ConnectedWallGeometryTests.Wall(12, new CellCoord(7, 2))
            };
            var geometry = ConnectedWallGeometryTests.Build(facts);
            var a = new CellCoord(0, 1);
            var b = new CellCoord(9, 1);
            var contact = AttackOcclusionResolver.FindBlock(reverse ? b : a, reverse ? a : b, geometry, AttackOcclusionPhase.Direct, false);
            Assert.That(contact.WallSourceIds, Is.EqualTo(reverse ? new long[] { 10, 11, 12 } : new long[] { 200, 201, 202 }));
            Assert.That(contact.WallCells.Count, Is.EqualTo(3));
        }

        [Test]
        public void EqualBentEntry_UsesComponentKeyAndWholeComponentEvidence()
        {
            var run = ConnectedWallGeometryTests.Build(BentWalls()).Runs.Single();
            var duplicate = new WallRun(run.XMin, run.XMax, run.YMin, run.YMax, run.CornerCutTicks,
                new long[] { 1, 2, 3 }, run.Cells, run.Parts);
            var result = AttackOcclusionResolver.FindBlock(new CellCoord(1, 3), new CellCoord(4, 3),
                new WallGeometry(new[] { run, duplicate }), AttackOcclusionPhase.Direct, false);
            Assert.That(result.WallSourceIds, Is.EqualTo(new long[] { 1, 2, 3 }));
            Assert.That(result.WallCells, Is.EqualTo(run.Cells));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PierceAcrossHollowComponent_KeepsWholeEvidenceWithoutPartDuplicates(bool ignore)
        {
            var cells = Enumerable.Range(0, 9).Select(i => new CellCoord(i % 3 + 1, i / 3 + 1)).Where(cell => cell != new CellCoord(2, 2)).ToArray();
            var geometry = ConnectedWallGeometryTests.Build(cells.Select((cell, i) => ConnectedWallGeometryTests.Wall(71 + i, cell)).ToArray());
            var origin = new CellCoord(0, 2);
            var targets = new[]
            {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 2), Owner.Enemy)
            };
            var policy = ignore ? new AttackOcclusionSpec(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.FromImpact) : AttackOcclusionSpec.Default;
            var trace = PierceTargetCollector.CollectSingle(origin, origin + CellCoord.Right, new[] { origin }, 8, 8, targets,
                Array.Empty<StructureCombatFacts>(), Array.Empty<StructureCombatFacts>(), default, geometry, new AttackElevation(Array.Empty<CellCoord>()), policy);
            Assert.That(trace.Encounters.Count, Is.EqualTo(ignore ? 2 : 0));
            Assert.That(trace.BlockedContacts.Count, Is.EqualTo(ignore ? 0 : 2));
            foreach (var contact in trace.BlockedContacts)
            {
                Assert.That(contact.WallSourceIds, Is.EqualTo(Enumerable.Range(71, 8).Select(id => (long)id).ToArray()));
                Assert.That(contact.WallCells.Count, Is.EqualTo(8));
            }
        }

        [Test]
        public void EnemyAi_MovesAroundBentWallThenAttacksOnNextEnemyTurn()
        {
            var input = EnemyMovementPathTests.Input(actorCell: new CellCoord(1, 1), targetCell: new CellCoord(3, 1));
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, appointRange: 4)
                : c).ToArray());
            input = WallMovementTests.WithWalls(input,
                ConnectedWallGeometryTests.Wall(71, new CellCoord(2, 1)),
                ConnectedWallGeometryTests.Wall(72, new CellCoord(2, 2)),
                ConnectedWallGeometryTests.Wall(73, new CellCoord(3, 2)));
            Assert.That(CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(3, 1))), Rules).CanAttack, Is.False);
            Assert.That(EnemyMovementPathfinder.TryFind(input, new EntityId(2), out var path), Is.True);
            Assert.That(path.Cells.Intersect(input.State.Environment.Tiles.Select(tile => tile.Cell)), Is.Empty);
            var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: path), Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var attack = CombatCommandComposition.Intent(CombatCommandTestData.NextEnemyTurn(CombatCommandComposition.Project(move)), CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(3, 1)));
            Assert.That(CombatCommandCalculator.Preview(attack, Rules).CanAttack, Is.True);
            Assert.That(CombatCommandCalculator.Calculate(attack, Rules).Accepted, Is.True);
        }

        private static StaticTerrainFact[] BentWalls() => new[]
        {
            ConnectedWallGeometryTests.Wall(71, new CellCoord(2, 2)),
            ConnectedWallGeometryTests.Wall(72, new CellCoord(3, 2)),
            ConnectedWallGeometryTests.Wall(73, new CellCoord(3, 3))
        };
    }
}
