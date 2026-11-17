using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class GimmickOccupancyTests
    {
        [Test]
        public void FullFootprintIncludesHeadAndTransientBody()
        {
            var state = State(new CellCoord(2, 2));
            Assert.That(GimmickOccupancyRules.Overlaps(state, new[] { new CellCoord(1, 2), new CellCoord(2, 2) }, null), Is.True);
            Assert.That(GimmickOccupancyRules.Overlaps(state, new[] { new CellCoord(3, 2), new CellCoord(4, 2) },
                new[] { new CellCoord(4, 2) }), Is.True);
            Assert.That(GimmickOccupancyRules.Overlaps(state, new[] { new CellCoord(3, 2) }, null), Is.False);
        }

        [Test]
        public void NormalMoveCannotLandOnHead()
        {
            var input = WithHead(MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1)), new CellCoord(2, 1));
            Assert.That(MovementRouteValidator.CanEnter(input, new CellCoord(2, 1)), Is.False);
        }

        [Test]
        public void ForcedLandingChecksTheWholeFootprint()
        {
            var input = WithHead(MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1)), new CellCoord(3, 1));
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            actor = actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, actor.Anchor, actor.Rotation,
                new[] { new CellCoord(1, 1), new CellCoord(2, 1) });
            var characters = input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray();
            Assert.That(AttackForcedMovement.CanEnterUnit(input, input.State.Environment, input.EnvironmentBindings,
                characters, input.State.Structures, actor.EntityId, new CellCoord(1, 1), new CellCoord(2, 1)), Is.False);
        }

        [Test]
        public void RandomStandingCandidatesExcludeHead()
        {
            var input = WithHead(MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1)), new CellCoord(2, 1));
            Assert.That(TornadoCalculator.GroupedLandingCandidates(input, input.ActorId), Has.No.Member(new CellCoord(2, 1)));
        }

        [Test]
        public void HeadDoesNotChangeAttackReachOrLineOfSight()
        {
            var input = CombatCommandTestData.Input();
            var before = CombatCommandCalculator.GetAttackRange(input);
            var after = CombatCommandCalculator.GetAttackRange(WithHead(input, new CellCoord(2, 3)));
            Assert.That(after.RangeCells.ToArray(), Is.EqualTo(before.RangeCells.ToArray()));
            Assert.That(after.InRangeTargetIds.ToArray(), Is.EqualTo(before.InRangeTargetIds.ToArray()));
            Assert.That(after.AttackAims.ToArray(), Is.EqualTo(before.AttackAims.ToArray()));
        }

        [Test]
        public void HeadRelocationInvalidatesMovementPreviewWithoutChangingTurn()
        {
            var input = LavaChainGimmickTests.World(MovementTestData.SafeRoute(new CellCoord(2, 2), new CellCoord(2, 3)));
            var context = MovementContextCalculator.Capture(input, Owner.Player, Array.Empty<IMovementTileRule>());
            Assert.That(context, Is.Not.Null);
            var changed = WithHead(input, new CellCoord(6, 2));
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, Array.Empty<IMovementTileRule>()), Is.False);
        }

        internal static StageGimmickStateSet State(CellCoord head) => new(new StageGimmickState[]
            { new LavaChainGimmickState(new StageGimmickId(1), head, 2) });
        internal static CombatCommandInput WithHead(CombatCommandInput input, CellCoord head) =>
            CombatCommandTestData.With(input, state: input.State.With(gimmicks: State(head)));

        [TestCase(false)]
        [TestCase(true)]
        public void TileForcedStepsCannotEnterHeadOrTransientBody(bool transient)
        {
            var input = WithHead(MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1)),
                transient ? new CellCoord(7, 7) : new CellCoord(2, 1));
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId),
                new System.Collections.Generic.List<CombatEvent>());
            if (transient) projection.GimmickReservations = new System.Collections.Generic.HashSet<CellCoord> { new CellCoord(2, 1) };
            foreach (var _ in MovementEnvironmentCalculator.ResolveForcedStepsSteps(projection, input.ActorId,
                new CellCoord(1, 0), 1, false, null, Array.Empty<IMovementTileRule>(), null)) { }
            Assert.That(projection.Character(input.ActorId).Anchor, Is.EqualTo(new CellCoord(1, 1)));
        }

        [Test]
        public void PlatformCannotCarryItsRiderOntoRestingHead()
        {
            var platform = new MovingPlatformFact(new MovingPlatformSpec(100, 1, PlatformAxis.Horizontal,
                new CellCoord(1, 1), new CellCoord(3, 1), new CellCoord(1, 1), 1));
            var input = WithHead(PlatformMotionTests.Input(platform), new CellCoord(2, 1));
            var rider = input.Characters.First();
            rider = rider.WithPlacement(rider.Area, rider.ReserveSlotIndex, new CellCoord(1, 1), rider.Rotation,
                new[] { new CellCoord(1, 1) });
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == rider.EntityId ? rider : c).ToArray());
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            var motion = result.Events.OfType<CombatPlatformMotionEvent>().Single();
            Assert.That(motion.Final.Cell, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(motion.Steps, Is.Empty);
        }
    }
}
