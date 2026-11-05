using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class PullRangeTests
    {
        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void ConfiguredThreePlusTwo_SelectsThroughFiveInEveryRay(int x, int y)
        {
            for (int distance = 1; distance <= 6; distance++)
            {
                var input = PullTestData.Input(3, 2, distance, new CellCoord(x, y));
                var result = CombatCommandCalculator.GetAttackRange(input);
                Assert.That(result.Succeeded, Is.True);
                var evaluation = CombatAttackQueries.Selection(input, input.Characters[0], input.Profiles.Last());
                if (distance <= 5)
                    Assert.That(evaluation.Pull.DestinationAnchor,
                        Is.EqualTo(new CellCoord(7 + x * Math.Min(distance, 3), 7 + y * Math.Min(distance, 3))));
                else Assert.That(evaluation.FailureReason, Is.Not.EqualTo(AttackFailureReason.None));
                Assert.That(result.RangeCells.ToArray().Contains(input.Aim.Cell.Value), Is.EqualTo(distance <= 5));
                Assert.That(result.AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(distance <= 5));
            }
        }

        [TestCase(1, 1, 2)]
        [TestCase(3, 2, 5)]
        [TestCase(3, 0, 3)]
        public void BasicAndTotalCells_AreDistinctConfiguredFacts(int range, int extra, int edge)
        {
            var input = PullTestData.Input(range, extra, edge);
            var result = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(result.RangeCells.Length, Is.EqualTo(edge * 4));
            var property = typeof(AttackRangeResult).GetProperty("BasicAttackCells");
            Assert.That(property, Is.Not.Null, "Basic and selectable range need separate published facts.");
            var basic = (ReadOnlyMemory<CellCoord>)property.GetValue(result);
            Assert.That(basic.Length, Is.EqualTo(range * 4));
        }

        [TestCase(EntityKey.Unit_Akimbo)]
        [TestCase(EntityKey.Unit_ShotGun)]
        public void EquivalentPullTraitOnDifferentUnitKey_UsesSameRangeAndDestination(EntityKey key)
        {
            var input = PullTestData.Input(3, 2, 5, key: key);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray().Contains(input.Aim), Is.True);
        }

        [Test]
        public void SameSyntheticUnitWithoutPull_UsesBaseRange()
        {
            var input = PullTestData.Input(3, 2, 5, trait: false, key: EntityKey.Unit_ShotGun);
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.Length, Is.EqualTo(12));
            Assert.That(range.AttackAims.ToArray().Contains(input.Aim), Is.False);
        }

        [Test]
        public void EffectiveRangeModifier_DrivesQueryDestinationAndUnitRange()
        {
            var input = StaticTerrainIntegrationTests.Terrain(PullTestData.Input(3, 2, 4),
                StaticTerrainIntegrationTests.Tile(71, PullTestData.Origin, StaticTerrainKind.Sandstorm));
            var range = CombatCommandCalculator.GetAttackRange(input);
            Assert.That(range.RangeCells.Length, Is.EqualTo(16));
            Assert.That(range.BasicAttackCells.Length, Is.EqualTo(8));
            Assert.That(CombatCommandCalculator.GetUnitAttackRange(input).RangeCells.ToArray(), Is.EqualTo(range.RangeCells.ToArray()));
            var result = PullTestData.Run(input);
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(9, 7)));
            Assert.That(input.Characters[0].Stats.AppointRange, Is.EqualTo(3));
        }

        [Test]
        public void InvalidConfiguration_RejectsWithoutOrdinaryAttackFallback()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PullTraitSpec(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PullTraitStatusSnapshot(-1));
            Assert.Throws<OverflowException>(() => PullTraitRule.SelectionRange(1, int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => PullTraitRule.SelectionRange(0, 1));
            Assert.Throws<ArgumentException>(() => PullTraitRule.Destination(CellCoord.Zero, new CellCoord(1, 1), 1));
            Assert.Throws<ArgumentException>(() => PullTraitRule.Destination(CellCoord.Zero, CellCoord.Zero, 1));
            var input = PullTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new PullTraitStatusSnapshot(2) }) : c).ToArray());
            Assert.Throws<InvalidOperationException>(() => CombatCommandCalculator.GetAttackRange(input));
        }

        [TestCase(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, TargetingChoiceAngle.Unrestricted)]
        [TestCase(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, TargetingChoiceAngle.Unrestricted)]
        [TestCase(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.Radial, TargetingChoiceAngle.Unrestricted)]
        [TestCase(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, TargetingChoiceAngle.Front)]
        public void UnsupportedTargetingConfiguration_IsAnError(TargetingShape shape, TargetingChoiceMode mode,
            TargetingSplash splash, TargetingChoiceAngle angle)
        {
            Assert.Throws<ArgumentException>(() => PullTestData.Input(method: new TargetingMethodSpec(shape, mode, splash, 0, angle)));
        }

        [Test]
        public void PullProfile_RejectsZeroAttackRange()
        {
            Assert.Throws<ArgumentException>(() => PullTestData.Input(range: 0));
            Assert.DoesNotThrow(() => PullTestData.Input(range: 1));
        }
    }
}
