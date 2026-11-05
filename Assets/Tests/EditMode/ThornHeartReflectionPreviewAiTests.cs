using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class ThornHeartReflectionPreviewAiTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [TestCase(false)]
        [TestCase(true)]
        public void SoleHeart_PreservesEveryPreviewAndAiChoice_ButActualAiDies(bool piercing)
        {
            var plain = ThornHeartTestData.Input(heart: false, hp: 2, piercing: piercing, splash: !piercing);
            var heart = CombatCommandTestData.With(plain, artifacts: ThornHeartTestData.Heart());
            var range = CombatCommandCalculator.GetAttackRange(plain);
            var heartRange = CombatCommandCalculator.GetAttackRange(heart);
            CollectionAssert.AreEqual(range.AttackAims.ToArray(), heartRange.AttackAims.ToArray());
            Assert.That(range.AttackAims.Length, Is.GreaterThan(1));
            foreach (var aim in range.AttackAims.ToArray())
            {
                var expected = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(plain, aim), NoRules, out var expectedWorld);
                var actual = CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(heart, aim), NoRules, out var actualWorld);
                Assert.That(actual.CanAttack, Is.EqualTo(expected.CanAttack));
                CollectionAssert.AreEqual(expected.Targets.ToArray().Select(t => (t.TargetId, t.ExpectedDamage, t.SecondaryDamage, t.TargetHealthAfter)),
                    actual.Targets.ToArray().Select(t => (t.TargetId, t.ExpectedDamage, t.SecondaryDamage, t.TargetHealthAfter)));
                Assert.That(actualWorld.FinalState.SameValues(expectedWorld.FinalState), Is.True);
                Assert.That(actualWorld.Events.OfType<CombatArtifactEvent>().Any(e => e.Reflection != null), Is.False);
                Assert.That(actualWorld.Events.OfType<CombatCharacterEvent>().Any(e => e.Final.EntityId == heart.ActorId), Is.False);
                Assert.That(ThornHeartTestData.Final(actualWorld, 1).Health.CurrentHealth, Is.EqualTo(2));
                CollectionAssert.AreEqual(expectedWorld.Events.Select(EventFact), actualWorld.Events.Select(EventFact));
            }
            var policy = new EnemyAiPolicy(new HostilityPolicy(), NoRules);
            Assert.That(policy.TrySelectAttackAim(plain, range, out var selected), Is.True);
            Assert.That(policy.TrySelectAttackAim(heart, heartRange, out var heartSelected), Is.True);
            Assert.That(heartSelected, Is.EqualTo(selected));
            var result = CombatCommandCalculator.Advance(heart, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Count(e => e.Reflection != null), Is.EqualTo(1));
            Assert.That(heart.Characters[0].Health.CurrentHealth, Is.EqualTo(2));
        }

        [TestCase(false, 1)]
        [TestCase(true, 2)]
        public void LiveAndPreviewDivergence_StopsOnlyNonPiercingLaterHits(bool piercing, int liveHits)
        {
            var input = ThornHeartTestData.Input(hp: 2, piercing: piercing, splash: !piercing);
            var preview = CombatCommandCalculator.Preview(input, NoRules, out var projection);
            var live = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(preview.Targets.Length, Is.EqualTo(2));
            Assert.That(live.Attack.Value.Hits.Length, Is.EqualTo(liveHits));
            Assert.That(ThornHeartTestData.Final(projection, 1).Health.CurrentHealth, Is.EqualTo(2));
            Assert.That(ThornHeartTestData.Final(live, 1).Health.IsDead, Is.True);
            Assert.That(preview.Targets.ToArray().All(t => t.SecondaryDamage == 0), Is.True);
            Assert.That(ThornHeartTestData.Final(live, 3).Health.CurrentHealth, Is.EqualTo(piercing ? 93 : 100));
            if (piercing)
            {
                CollectionAssert.AreEqual(preview.PiercingShot.TraveledCells.ToArray(), live.Attack.Value.PiercingShot.TraveledCells.ToArray());
                Assert.That(live.Attack.Value.PiercingShot.Endpoint, Is.EqualTo(preview.PiercingShot.Endpoint));
                Assert.That(live.Attack.Value.PiercingShot.StopReason, Is.EqualTo(preview.PiercingShot.StopReason));
            }
        }

        private static string EventFact(CombatEvent entry) => entry is CombatCharacterEvent c
            ? $"{c.Kind}:{c.Final.EntityId.Value}:{c.Expected.Health.CurrentHealth}:{c.Final.Health.CurrentHealth}"
            : entry.Kind.ToString();
    }
}
