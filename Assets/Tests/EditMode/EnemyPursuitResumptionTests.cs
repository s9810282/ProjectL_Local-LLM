using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyPursuitResumptionTests
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void EveryResumePartitionMatchesReference(int width)
        {
            var target = new EntityId(2);
            foreach (var category in new[] { CategoryKey.Main, CategoryKey.Rider })
            {
                var input = EnemyMovementPathTests.Input(category, EnemyMovementPathTests.Maze(),
                    actorCell: new CellCoord(0, 1), targetCell: new CellCoord(3, 1));
                bool expected = EnemyPursuitReference.TryFind(input, target, out var reference);
                using var search = new EnemyPursuitSearch(input, target);
                Assert.Throws<InvalidOperationException>(() => search.TryGetPath(out _));
                int steps = 0;
                while (!search.IsComplete && steps < 100_000)
                    for (int i = 0; i < width && !search.IsComplete; i++, steps++) search.Step();
                Assert.That(search.IsComplete, Is.True);
                Assert.That(steps, Is.GreaterThan(1));
                Assert.That(search.TryGetPath(out var actual), Is.EqualTo(expected));
                if (expected) Assert.That(actual.Cells, Is.EqualTo(reference.Cells));
            }
        }

        [Test]
        public void NoGoalTerminates()
        {
            using var search = new EnemyPursuitSearch(EnemyMovementPathTests.Input(), new EntityId(999));
            for (int i = 0; i < 20_000 && !search.IsComplete; i++) search.Step();
            Assert.That(search.IsComplete, Is.True);
            Assert.That(search.TryGetPath(out _), Is.False);
        }
    }
}
