using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class TraitGrowthChangeTests
    {
        [Test]
        public void Change_AppendsExactlyOneAndPreservesPrefixIdentityLevelAndExperience()
        {
            var p = TraitGrowthFixtures.Profile();
            var before = TraitGrowthFixtures.Owned(p);
            var first = TraitGrowthFixtures.Owned(p, growth: before.Growth.Append(new TraitGrowthSelection(20, TraitKey.Knockback)));
            var next = TraitGrowthFixtures.Owned(p, growth: first.Growth.Append(new TraitGrowthSelection(10, TraitKey.Knockback)));
            Assert.That(new TraitGrowthChange(first, next).Added.StepId, Is.EqualTo(10));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(before, next));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, before));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, first));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, TraitGrowthFixtures.Owned(p, level: 9, growth: next.Growth)));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, TraitGrowthFixtures.Owned(p, id: 3, growth: next.Growth)));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, new OwnedCharacterRecord(first.Id,
                EntityKey.Unit_ShotGun, first.Role, first.Level, first.Experience, next.Growth)));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, new OwnedCharacterRecord(first.Id,
                first.CharacterKey, CharacterOwnershipRole.Main, first.Level, first.Experience, next.Growth)));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, new OwnedCharacterRecord(first.Id, first.CharacterKey, first.Role, first.Level, 8, next.Growth)));
            var replaced = TraitGrowthFixtures.Owned(p, growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(20, TraitKey.SoloAction), new TraitGrowthSelection(10, TraitKey.Knockback) }));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(first, replaced));
        }
    }
}
