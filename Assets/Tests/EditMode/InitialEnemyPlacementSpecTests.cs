using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class InitialEnemyPlacementSpecTests
    {
        [Test]
        public void Spec_HoldsFixedEnemyPlacement()
        {
            InitialEnemyPlacementSpec spec = new InitialEnemyPlacementSpec(
                EntityKey.Unit_Demon,
                new CellCoord(4, 7),
                Rotation4.R90);

            Assert.That(spec.UnitKey, Is.EqualTo(EntityKey.Unit_Demon));
            Assert.That(spec.Anchor, Is.EqualTo(new CellCoord(4, 7)));
            Assert.That(spec.Rotation, Is.EqualTo(Rotation4.R90));
        }

        [Test]
        public void Spec_RejectsNoneKeyAndInvalidRotation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InitialEnemyPlacementSpec(
                    EntityKey.None,
                    new CellCoord(4, 7),
                    Rotation4.R0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new InitialEnemyPlacementSpec(
                    EntityKey.Unit_Demon,
                    new CellCoord(4, 7),
                    (Rotation4)255));
        }

        [Test]
        public void StageConfig_CopiesInitialEnemyPlacements()
        {
            InitialEnemyPlacementSpec original = new InitialEnemyPlacementSpec(
                EntityKey.Unit_Demon,
                new CellCoord(4, 7),
                Rotation4.R0);
            InitialEnemyPlacementSpec[] source = { original };

            StageConfig config = new StageConfig(
                9, 9, 4, 3, 3, timelineSeed: 17,
                initialEnemyPlacements: source);

            source[0] = null;

            Assert.That(config.InitialEnemyPlacements.Length, Is.EqualTo(1));
            Assert.That(config.InitialEnemyPlacements.Span[0], Is.SameAs(original));
        }

        [Test]
        public void StageConfig_RejectsInvalidEnemyPlacementArray()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new StageConfig(9, 9, 4, 3, 3, 17, null));

            Assert.Throws<ArgumentException>(() =>
                new StageConfig(
                    9, 9, 4, 3, 3, 17,
                    new InitialEnemyPlacementSpec[] { null }));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StageConfig(
                    9, 9, 4, 3, 3, 17,
                    new[]
                    {
                        new InitialEnemyPlacementSpec(
                            EntityKey.Unit_Demon,
                            new CellCoord(9, 7),
                            Rotation4.R0)
                    }));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StageConfig(
                    9, 9, 4, 3, 3, 17,
                    new[]
                    {
                        new InitialEnemyPlacementSpec(
                            EntityKey.Unit_Demon,
                            new CellCoord(-1, 7),
                            Rotation4.R0)
                    }));
        }

        [Test]
        public void LegacyConstructors_UseNoInitialEnemies()
        {
            Assert.That(
                new StageConfig(9, 9, 4, 3, 3)
                    .InitialEnemyPlacements.Length,
                Is.Zero);
            Assert.That(
                new StageConfig(9, 9, 4, 3, 3, timelineSeed: 17)
                    .InitialEnemyPlacements.Length,
                Is.Zero);
        }
    }
}
