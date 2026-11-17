using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.Core
{
    public sealed class MagicBulletSelectionTests
    {
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void PrefixCandidatesUseRelativePerpendicularDirections(int dx, int dy)
        {
            var center = new CellCoord(3, 3);
            var a = new CellCoord(3 + dx, 3 + dy);
            var b = new CellCoord(a.X - dy, a.Y + dx);
            var input = MagicBulletTestData.Input(upgrade: PlayerSkillUpgradeKey.Distortion, range: 2);
            var initial = Query(input);
            Assert.That(initial.NextCandidateCells.Count, Is.EqualTo(8));
            Assert.That(initial.NextCandidateCells, Does.Contain(a));
            var second = Query(input, a);
            Assert.That(second.NextCandidateCells.Count, Is.EqualTo(4));
            Assert.That(second.NextCandidateCells, Does.Contain(b));
            Assert.That(second.NextCandidateCells, Has.No.Member(center));
            var third = Query(input, a, b);
            Assert.That(third.NextCandidateCells, Does.Contain(new CellCoord(b.X + dx, b.Y + dy)));
            Assert.That(third.NextCandidateCells, Has.No.Member(a));
            Assert.That(Query(input, a, b, new CellCoord(b.X + dx, b.Y + dy)).IsComplete, Is.True);
            Assert.That(input.State.PlayerSkill.RemainingUses, Is.EqualTo(2));
        }

        [Test]
        public void EmptyAllyAndEnemyCellsAreEquallyValidPoints()
        {
            var points = new[] { new CellCoord(5, 3), new CellCoord(5, 5) };
            foreach (var owner in new[] { Owner.Player, Owner.Enemy })
            {
                var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, points[0], owner);
                Assert.That(Query(MagicBulletTestData.Input(targets: new[] { target }), points).IsComplete, Is.True);
            }
            Assert.That(Query(MagicBulletTestData.Input(), points).IsComplete, Is.True);
        }

        [TestCase(3, 3, PlayerSkillFailureReason.InvalidSegmentDirection)]
        [TestCase(4, 4, PlayerSkillFailureReason.InvalidSegmentDirection)]
        [TestCase(7, 3, PlayerSkillFailureReason.PointOutOfRange)]
        [TestCase(8, 3, PlayerSkillFailureReason.PointOutsideBoard)]
        public void InvalidFirstPointsReturnTypedFailure(int x, int y, PlayerSkillFailureReason failure)
        {
            Assert.That(Query(MagicBulletTestData.Input(range: 2), new CellCoord(x, y)).FailureReason, Is.EqualTo(failure));
        }

        [Test]
        public void WallPointAndCrossedWallRejectAtEverySegment()
        {
            var wall = new StaticTerrainFact(71, new CellCoord(5, 4), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });
            var input = MagicBulletTestData.Input(terrain: new[] { wall });
            Assert.That(Query(input, new CellCoord(5, 3), new CellCoord(5, 4)).FailureReason, Is.EqualTo(PlayerSkillFailureReason.PointOnWall));
            Assert.That(Query(input, new CellCoord(5, 3), new CellCoord(5, 5)).FailureReason, Is.EqualTo(PlayerSkillFailureReason.SegmentBlockedByWall));
            Assert.That(Query(input, new CellCoord(5, 3)).NextCandidateCells, Has.No.Member(new CellCoord(5, 5)));
            var first = MagicBulletTestData.Input(center: new CellCoord(5, 2), terrain: new[] { wall });
            Assert.That(Query(first, new CellCoord(5, 5)).FailureReason, Is.EqualTo(PlayerSkillFailureReason.SegmentBlockedByWall));
            var third = MagicBulletTestData.Input(upgrade: PlayerSkillUpgradeKey.Distortion, terrain: new[] { wall });
            Assert.That(Query(third, new CellCoord(4, 3), new CellCoord(4, 4), new CellCoord(6, 4)).FailureReason,
                Is.EqualTo(PlayerSkillFailureReason.SegmentBlockedByWall));
        }

        [Test]
        public void CompleteAndExtraPointCountsAreExplicit()
        {
            var input = MagicBulletTestData.Input();
            Assert.That(Query(input, new CellCoord(5, 3)).IsComplete, Is.False);
            var result = Query(input, new CellCoord(5, 3), new CellCoord(5, 5));
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.NextCandidateCells, Is.Empty);
            Assert.That(Query(input, new CellCoord(5, 3), new CellCoord(5, 5), new CellCoord(3, 5)).FailureReason,
                Is.EqualTo(PlayerSkillFailureReason.WrongPointCount));
        }

        private static PlayerSkillSelectionResult Query(CombatCommandInput input, params CellCoord[] points) =>
            PlayerSkillQueries.Selection(MagicBulletTestData.Copy(input,
                request: new PlayerSkillRequest(points, input.PlayerSkillSource)), input.PlayerSkillSource.Value);
    }
}
