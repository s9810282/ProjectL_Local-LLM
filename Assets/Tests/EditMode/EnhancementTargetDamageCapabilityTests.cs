using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Tests.Core
{
    public sealed class EnhancementTargetDamageCapabilityTests
    {


        [Test]
        public void MinimumDistance_UsesNearestStructureCellIncludingCornerNeighbor()
        {
            var actor = CombatCommandTestData.Input().Characters[0];
            Assert.That(CombatDamageCalculator.Distance(actor, new[] { new CellCoord(5, 6), new CellCoord(2, 4) }), Is.EqualTo(1));
        }

        private static EffectTargetDamageEvaluation Multipliers(params (int Id, int Numerator, int Denominator)[] factors)
        {
            var origins = factors.Select(f => TraitEffectOrigin.Create(new EntityId(f.Id), TraitKey.StationaryFire)).ToArray();
            return new EffectTargetDamageEvaluation(factors.Select((f, i) => new EffectTargetDamageContribution(
                origins[i], false, f.Numerator, f.Denominator)).ToArray(), origins.Select(origin =>
                new EffectStateExpectation(origin, new StationaryFireEffectStateSnapshot(false))).ToArray());
        }

        [TestCase(400, 100)]
        [TestCase(4, 1)]
        public void EquivalentRatios_PreserveExactIntermediateAndFinalValues(int numerator, int denominator)
        {
            var result = Multipliers((1, numerator, denominator), (2, 3, 2));
            Assert.That(result.Scale(10, 1), Is.EqualTo(40));
            Assert.That(result.Scale(3, 2), Is.EqualTo(18));
        }

        [Test]
        public void Contributions_AreCopiedSortedAndMultiplyBeforeOneHalfUpRounding()
        {
            var first = TraitEffectOrigin.Create(new EntityId(1), TraitKey.StationaryFire);
            var second = TraitEffectOrigin.Create(new EntityId(2), TraitKey.TrackingShot);
            var contributions = new[] { new EffectTargetDamageContribution(second, false, 5, 3),
                new EffectTargetDamageContribution(first, false, 3, 2) };
            var expectations = new[] { new EffectStateExpectation(first, new StationaryFireEffectStateSnapshot(false)),
                new EffectStateExpectation(second, StatelessEffectStateSnapshot.Instance) };
            var result = new EffectTargetDamageEvaluation(contributions, expectations);
            Array.Reverse(contributions);
            contributions[0] = new EffectTargetDamageContribution(first, false, 99, 1);
            Assert.That(result.Contributions.Select(c => c.Origin.OwnerInstanceId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.Scale(3, result.Contributions.Count), Is.EqualTo(8));
            Assert.That(result.BypassPhysicalDefense, Is.False);
            var large = Multipliers((1, int.MaxValue, 1), (2, 2, 1));
            Assert.That(large.Scale(0, 2), Is.Zero);
            Assert.Throws<OverflowException>(() => large.Scale(1, 2));
            Assert.Throws<OverflowException>(() => CategoryMatchupDamageCalculator.Apply(int.MaxValue, 2, 1));
        }



        [Test]
        public void ExactScale_PreservesHalfUpAndPrefixValues()
        {
            var result = Multipliers((1, 1, 2), (2, 2, 1), (3, 3, 2));
            Assert.That(Enumerable.Range(0, 4).Select(i => result.Scale(3, i)), Is.EqualTo(new[] { 3, 2, 3, 5 }));
            Assert.That(result.Scale(0, 3), Is.Zero);
            Assert.That(result.Scale(int.MaxValue, 0), Is.EqualTo(int.MaxValue));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.Scale(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.Scale(1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.Scale(1, 4));
            var exact = Multipliers((1, int.MaxValue, int.MaxValue), (2, int.MaxValue, int.MaxValue), (3, 4, 1));
            Assert.That(exact.Scale(10, 3), Is.EqualTo(40));
            var cancellation = Multipliers((1, int.MaxValue, 1), (2, 2, 1), (3, 1, int.MaxValue));
            Assert.That(cancellation.Scale(1, 3), Is.EqualTo(2));
            Assert.That(cancellation.Scale(1, 1), Is.EqualTo(int.MaxValue));
            Assert.Throws<OverflowException>(() => cancellation.Scale(1, 2));
        }
    }
}
