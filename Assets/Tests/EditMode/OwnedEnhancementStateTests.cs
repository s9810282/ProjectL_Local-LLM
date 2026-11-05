using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class OwnedEnhancementStateTests
    {
        [Test]
        public void ExperienceGain_PreservesEverySelectionAndRejectsHistoryReplacement()
        {
            var p = TraitGrowthFixtures.Profile();
            var history = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.Knockback), new TraitGrowthSelection(20, TraitKey.Knockback) });
            var before = TraitGrowthFixtures.Owned(p, level: 1, growth: history);
            var after = ExperienceRules.Gain(before, 350, new LevelProgressionSpec(150, 100, 3, 1, 1)).Final;
            Assert.That((after.Level, after.Experience), Is.EqualTo((3, 57)));
            Assert.That(after.Growth, Is.SameAs(history));
            Assert.That(before.Level, Is.EqualTo(1));
            Assert.DoesNotThrow(() => new OwnedCharacterChange(before, after));
            var changed = new OwnedCharacterRecord(after.Id, after.CharacterKey, after.Role, after.Level, after.Experience,
                new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction), history.Selections[1] }));
            Assert.That(after.SameValues(changed), Is.False);
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(before, changed));
        }
    }
}
