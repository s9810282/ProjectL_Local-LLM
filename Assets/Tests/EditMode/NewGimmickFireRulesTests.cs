using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class NewGimmickFireRulesTests
    {
        [Test]
        public void LongerFireWinsAndEqualDurationUsesNewOrigin()
        {
            var ordinary = new EffectOriginKey(EffectOwnerKind.Trait, 1, 1, "ordinary");
            var meteor = new EffectOriginKey(EffectOwnerKind.StageGimmick, 1, 50, "meteor");
            var initial = PeriodicStatusRules.ApplyFire(StatusEffectSet.Empty, ordinary);
            Assert.That(initial.Entries.OfType<FireStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(2));
            var extended = PeriodicStatusRules.ApplyFire(initial, meteor, 3);
            var refreshed = extended.Entries.OfType<FireStatus>().Single();
            Assert.That(refreshed.RemainingAffectedSideStarts, Is.EqualTo(3));
            Assert.That(PeriodicStatusRules.ApplyFire(extended, ordinary).Entries.Single().Origin, Is.EqualTo(meteor));
            Assert.That(PeriodicStatusRules.ApplyFire(extended, ordinary, 3).Entries.Single().Origin,
                Is.EqualTo(ordinary));
        }
    }
}
