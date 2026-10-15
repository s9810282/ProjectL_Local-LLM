using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.GameFlow;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class StageStartCatalogTests
    {
        [Test]
        public void Constructor_FixesDefinitionsWhenSourceListIsChanged()
        {
            StageStartSpec original = CreateStageStartSpec(new StageKey(1));
            var source = new List<StageStartSpec> { original };
            var catalog = new StageStartCatalog(source);

            source[0] = CreateStageStartSpec(new StageKey(2));
            source.Clear();

            Assert.That(catalog.TryGet(new StageKey(1), out StageStartSpec stored), Is.True);
            Assert.That(stored, Is.SameAs(original));
            Assert.That(catalog.Contains(new StageKey(2)), Is.False);
        }

        [Test]
        public void Constructor_RejectsDuplicateStageKey()
        {
            Assert.Throws<ArgumentException>(() => new StageStartCatalog(new[]
            {
                CreateStageStartSpec(new StageKey(1)),
                CreateStageStartSpec(new StageKey(1)),
            }));
        }

        [Test]
        public void Constructor_PreservesTheExplicitMatchupCatalog()
        {
            CategoryMatchupSpec matchups = CategoryMatchupSpec.CreateDefault();
            StageStartSpec spec = CreateStageStartSpec(new StageKey(1), matchups);
            var catalog = new StageStartCatalog(new[] { spec });

            Assert.That(catalog.TryGet(new StageKey(1), out StageStartSpec stored), Is.True);
            Assert.That(stored.CategoryMatchups, Is.SameAs(matchups));
            Assert.Throws<ArgumentNullException>(() => new StageStartSpec(
                new StageKey(2),
                CreateConfig(),
                new[] { CreateUnitSpec() },
                Array.Empty<StructureSpec>(),
                null));
        }

        [Test]
        public void Constructor_RejectsInvalidUnitCatalogDefinition()
        {
            UnitSpec duplicatedUnit = CreateUnitSpec();
            var invalid = new StageStartSpec(
                new StageKey(1),
                CreateConfig(),
                new[] { duplicatedUnit, duplicatedUnit },
                Array.Empty<StructureSpec>(),
                CategoryMatchupSpec.CreateDefault());

            Assert.Throws<ArgumentException>(() => new StageStartCatalog(new[] { invalid }));
        }

        [Test]
        public void ValidateRequiredKeys_AllowsDuplicateRegularKeysAndRejectsMissingKeys()
        {
            var catalog = new StageStartCatalog(new[]
            {
                CreateStageStartSpec(new StageKey(1)),
                CreateStageStartSpec(new StageKey(2)),
            });

            Assert.DoesNotThrow(() => catalog.ValidateRequiredKeys(
                new[] { new StageKey(1), new StageKey(1) },
                new StageKey(2)));
            Assert.Throws<ArgumentException>(() => catalog.ValidateRequiredKeys(
                new[] { new StageKey(3) },
                new StageKey(2)));
            Assert.Throws<ArgumentException>(() => catalog.ValidateRequiredKeys(
                new[] { new StageKey(1) },
                new StageKey(3)));
        }

        private static StageStartSpec CreateStageStartSpec(
            StageKey key,
            CategoryMatchupSpec matchups = null)
        {
            return new StageStartSpec(
                key,
                CreateConfig(),
                new[] { CreateUnitSpec() },
                Array.Empty<StructureSpec>(),
                matchups ?? CategoryMatchupSpec.CreateDefault());
        }

        private static StageConfig CreateConfig()
        {
            return new StageConfig(
                mapWidth: 7,
                mapHeight: 7,
                reserveCapacity: 2,
                playerDeploymentDepth: 2,
                enemyDeploymentDepth: 2);
        }

        private static UnitSpec CreateUnitSpec()
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                footprintWidth: 1,
                footprintHeight: 1,
                footprintLocalCells: new[] { CellCoord.Zero },
                maxHealth: 10,
                MoveMethod.LiftAndPlace,
                moveRange: 3,
                moveCost: 1,
                1,
                attackPower: 2,
                maxActionCost: 8,
                attackCost: 2,
                priority: 30,
                delay: 20);
        }
    }
}
