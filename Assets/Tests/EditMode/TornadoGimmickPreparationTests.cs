using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TornadoGimmickPreparationTests
    {
        private static readonly Owner[] Both = { Owner.Player, Owner.Enemy };
        private static readonly StageGimmickId Id = new StageGimmickId(4);

        [Test]
        public void CombinedHaboobAndTwoRandomStormsShareOneCursorInDefinitionOrder()
        {
            var input = TornadoGimmickResolutionTests.Input();
            var prepared = StageGimmickComposition.Initialize(input.State, new StageGimmickSpec[] {
                new HaboobGimmickSpec(), new TornadoGimmickSpec(new[] {
                    new TornadoMemberSpec(32, Both), new TornadoMemberSpec(31, Both) }) });
            Assert.That(prepared.EnvironmentProgress.RandomState, Is.EqualTo(217083232u));
            Assert.That(prepared.Gimmicks.Entries.Select(e => e.Id.Value), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(((TornadoGimmickState)prepared.Gimmicks.Entries[1]).Members.Select(m => m.SourceId), Is.EqualTo(new long[] { 31, 32 }));
            Assert.That(input.State.EnvironmentProgress.RandomState, Is.EqualTo(1u));
        }
        [TestCase(1, 1)]
        [TestCase(6, -1)]
        public void FixedEdgesPointInwardWithoutRandomDraw(int x, int sign)
        {
            uint random = 71;
            var state = Prepare(new TornadoMemberSpec(31, Both, new CellCoord(x, 2)), ref random);
            Assert.That(state.Members.Single().Center, Is.EqualTo(new CellCoord(x, 2)));
            Assert.That(state.Members.Single().HorizontalSign, Is.EqualTo(sign));
            Assert.That(state.NextBoundaryCount, Is.EqualTo(1));
            Assert.That(state.Members.Single().LockedDirection, Is.Null);
            Assert.That(random, Is.EqualTo(71u));
        }

        [Test]
        public void InteriorRequiresExplicitDirectionAndInvalidFixedCenterIsDiagnosed()
        {
            uint random = 9;
            Assert.Throws<InvalidOperationException>(() => Prepare(new TornadoMemberSpec(31, Both, new CellCoord(2, 2)), ref random));
            var state = Prepare(new TornadoMemberSpec(31, Both, new CellCoord(2, 2), -1), ref random);
            Assert.That(state.Members.Single().HorizontalSign, Is.EqualTo(-1));
            var error = Assert.Throws<InvalidOperationException>(() => Prepare(new TornadoMemberSpec(31, Both, CellCoord.Zero), ref random));
            Assert.That(error.Message, Does.Contain("31").And.Contain("stage-test"));
            Assert.That(random, Is.EqualTo(9u));
        }

        [Test]
        public void RandomStartsUseEdgesAndStableSourceOrderWithImmutableVisibility()
        {
            var owners = new[] { Owner.Enemy };
            var a = new TornadoMemberSpec(31, owners);
            owners[0] = Owner.Player;
            var b = new TornadoMemberSpec(32, Both);
            var array = new[] { b, a };
            var spec = new TornadoGimmickSpec(array);
            array[0] = a;
            uint first = 71, second = 71;
            var state = TornadoGimmickPreparationCalculator.Prepare(spec, Id, 8, 8, ref first);
            var replay = TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] { a, b }), Id, 8, 8, ref second);
            Assert.That(state.SameValues(replay), Is.True);
            Assert.That(first, Is.EqualTo(second).And.Not.EqualTo(71u));
            Assert.That(state.Members.Select(m => m.SourceId), Is.EqualTo(new long[] { 31, 32 }));
            Assert.That(spec.Members[0].KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            foreach (var member in state.Members)
            {
                Assert.That(member.Center.X, Is.EqualTo(1).Or.EqualTo(6));
                Assert.That(member.Center.Y, Is.InRange(1, 6));
                Assert.That(member.HorizontalSign, Is.EqualTo(member.Center.X == 1 ? 1 : -1));
            }
        }

        [Test]
        public void InvalidDefinitionsAndSmallBoardFailBeforeSpendingRandomness()
        {
            var member = new TornadoMemberSpec(31, Both);
            Assert.Throws<ArgumentException>(() => new TornadoGimmickSpec(new[] { member, member }));
            Assert.Throws<ArgumentException>(() => new TornadoGimmickSpec(Array.Empty<TornadoMemberSpec>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TornadoMemberSpec(0, Both));
            Assert.Throws<ArgumentException>(() => new TornadoMemberSpec(31, new[] { Owner.None }));
            uint random = 19;
            Assert.Throws<InvalidOperationException>(() => TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] {
                member, new TornadoMemberSpec(32, Both, CellCoord.Zero) }), Id, 8, 8, ref random));
            Assert.Throws<InvalidOperationException>(() => TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] { member }), Id, 2, 8, ref random));
            Assert.That(random, Is.EqualTo(19u));
        }

        [TestCase(0, 3, 2, 2, 1)]
        [TestCase(2, 3, 2, 2, 1)]
        [TestCase(3, 0, 2, 2, 1)]
        [TestCase(3, 3, 0, 2, 1)]
        [TestCase(3, 3, 2, 0, 1)]
        [TestCase(3, 3, 2, 2, 0)]
        [TestCase(3, 3, 2, 4, 3)]
        [TestCase(3, 3, 4, 2, 3)]
        public void InvalidSquareDistanceOrScheduleIsRejected(int side, int distance, int first, int repeat, int lead)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TornadoMemberSpec(31, Both,
                sideLength: side, diagonalDistance: distance, firstActivationAfterControllerTurns: first,
                repeatIntervalControllerTurns: repeat, warningLeadControllerTurns: lead));
        }

        [Test]
        public void NoTornadoConsumesNoDrawsAndNarrowBoardCanRemainStationary()
        {
            uint random = 71;
            Assert.That(TornadoGimmickPreparationCalculator.Prepare(null, Id, 8, 8, ref random), Is.Null);
            Assert.That(random, Is.EqualTo(71u));
            var spec = new TornadoMemberSpec(31, Both, new CellCoord(1, 1), -1,
                firstActivationAfterControllerTurns: 1, repeatIntervalControllerTurns: 1);
            var state = TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] { spec }), Id, 3, 3, ref random);
            Assert.That(state.Members[0].Phase, Is.EqualTo(TornadoGimmickPhase.Warning));
            Assert.That(state.Members[0].LockedDirection, Is.EqualTo(CellCoord.Zero));
            Assert.That(TornadoGeometry.Travel(state.Members[0].Center, 3, 3, 3, 3, CellCoord.Zero),
                Is.EqualTo(new[] { new CellCoord(1, 1) }));
            Assert.That(random, Is.EqualTo(71u));
            uint other = 71;
            var seeded = TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] {
                new TornadoMemberSpec(32, Both, new CellCoord(1, 1)) }), Id, 3, 3, ref other);
            Assert.That(seeded.Members[0].HorizontalSign, Is.EqualTo(1).Or.EqualTo(-1));
            Assert.That(other, Is.Not.EqualTo(71u));
        }

        [Test]
        public void GroupDeadlineAndValidationMatchEachMemberDefinition()
        {
            uint random = 7;
            var spec = new TornadoGimmickSpec(new[] {
                new TornadoMemberSpec(31, Both, new CellCoord(1, 2), firstActivationAfterControllerTurns: 4),
                new TornadoMemberSpec(32, Both, new CellCoord(6, 2)) });
            var state = TornadoGimmickPreparationCalculator.Prepare(spec, new StageGimmickId(1), 8, 8, ref random);
            Assert.That(state.NextBoundaryCount, Is.EqualTo(1));
            var set = new StageGimmickStateSet(new[] { state });
            Assert.DoesNotThrow(() => set.ValidateDefinitions(new[] { spec }, 8, 8));
            Assert.Throws<InvalidOperationException>(() => set.ValidateDefinitions(new[] {
                new TornadoGimmickSpec(new[] { spec.Members[0] }) }, 8, 8));
            Assert.Throws<InvalidOperationException>(() => set.ValidateDefinitions(new[] { spec }, 3, 3));
            Assert.That(state.SameValues(new TornadoGimmickState(state.Id, state.Members.Reverse().ToArray())), Is.True);
            Assert.That(state.SameValues(new TornadoGimmickState(state.Id, new[] {
                new TornadoMemberState(31, new CellCoord(1, 3), 1, TornadoGimmickPhase.Cooldown, 3, null), state.Members[1] })), Is.False);
            Assert.Throws<ArgumentException>(() => new TornadoMemberState(31, new CellCoord(1, 2), 1,
                TornadoGimmickPhase.Cooldown, 1, new CellCoord(1, 1)));
        }

        [TestCase(0, 0)]
        [TestCase(-1, -1)]
        public void WarningStateCannotPublishAStationaryOrOutwardDirectionWhenTravelIsPossible(int dx, int dy)
        {
            var member = new TornadoMemberSpec(31, Both, new CellCoord(1, 1));
            var state = new TornadoGimmickState(new StageGimmickId(1), new[] {
                new TornadoMemberState(31, new CellCoord(1, 1), dx == 0 ? 1 : dx,
                    TornadoGimmickPhase.Warning, 2, new CellCoord(dx, dy)) });
            Assert.Throws<InvalidOperationException>(() => new StageGimmickStateSet(new[] { state })
                .ValidateDefinitions(new[] { new TornadoGimmickSpec(new[] { member }) }, 8, 8));
        }

        private static TornadoGimmickState Prepare(TornadoMemberSpec member, ref uint random) =>
            TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] { member }), Id, 8, 8, ref random, stageContext: "stage-test");
    }
}
