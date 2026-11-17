using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.Core
{
    public sealed class MagicBulletTraversalTests
    {
        [Test]
        public void BaseBendsOnceAndContinuesBeyondB()
        {
            var input = MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 4) });
            var path = MagicBulletCalculator.Path(input).ToArray();
            Assert.That(path.Select(p => p.Cell), Is.EqualTo(new[] { new CellCoord(4, 3), new CellCoord(5, 3),
                new CellCoord(5, 4), new CellCoord(5, 5), new CellCoord(5, 6), new CellCoord(5, 7) }));
            Assert.That(path[1].Incoming, Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(path[2].Incoming, Is.EqualTo(new CellCoord(0, 1)));
        }

        [Test]
        public void DistortionBendsAtAAndBButNotC()
        {
            var input = MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 5), new CellCoord(4, 5) },
                PlayerSkillUpgradeKey.Distortion);
            var cells = MagicBulletCalculator.Path(input).Select(p => p.Cell).ToArray();
            Assert.That(cells, Is.EqualTo(new[] { new CellCoord(4, 3), new CellCoord(5, 3), new CellCoord(5, 4), new CellCoord(5, 5),
                new CellCoord(4, 5), new CellCoord(3, 5), new CellCoord(2, 5), new CellCoord(1, 5), new CellCoord(0, 5) }));
            Assert.That(cells.Distinct().Count(), Is.EqualTo(cells.Length));
        }

        [Test]
        public void FinalRayStopsBeforeWall()
        {
            var wall = new StaticTerrainFact(71, new CellCoord(5, 6), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy });
            var input = MagicBulletTestData.Input(new[] { new CellCoord(5, 3), new CellCoord(5, 4) }, terrain: new[] { wall });
            Assert.That(MagicBulletCalculator.Path(input).Last().Cell, Is.EqualTo(new CellCoord(5, 5)));
        }
    }
}
