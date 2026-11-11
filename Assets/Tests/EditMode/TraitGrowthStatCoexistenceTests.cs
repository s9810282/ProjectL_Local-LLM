using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class TraitGrowthStatCoexistenceTests
    {
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Main)]
        [TestCase(CategoryKey.Cannon)]
        public void StatLevelUps_ArePreservedWithAndWithoutTraitSteps(CategoryKey category)
        {
            var profile = TraitGrowthFixtures.Profile(category);
            var catalog = TraitGrowthFixtures.Catalog(category: category);
            var before = TraitGrowthFixtures.Owned(profile, level: 1);
            var spec = new LevelProgressionSpec(150, 100, 3, 1, 1);
            var grown = ExperienceRules.Gain(before, 950, spec).Final;
            Assert.That(grown.Level, Is.GreaterThan(2));
            var selections = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.Knockback), new TraitGrowthSelection(20, TraitKey.Knockback) });
            var selected = new OwnedCharacterRecord(grown.Id, grown.CharacterKey, grown.Role, grown.Level, grown.Experience, selections);
            var pending = OwnedCharacterStatsCalculator.Calculate(new[] { profile }, UnitSpecTestFactory.CreateGrowthCategoryCatalog(), new[] { grown },
                category == CategoryKey.Main ? new MainCharacterPersistentState(10, 10, 5) : null, new Dictionary<CharacterInstanceId, int> { [grown.Id] = 0 }, default, catalog)[0];
            var baseline = OwnedCharacterStatsCalculator.Calculate(new[] { profile }, UnitSpecTestFactory.CreateGrowthCategoryCatalog(), new[] { grown },
                category == CategoryKey.Main ? new MainCharacterPersistentState(10, 10, 5) : null, new Dictionary<CharacterInstanceId, int> { [grown.Id] = 0 }, default, TraitGrowthCatalog.Disabled)[0];
            var enhanced = OwnedCharacterStatsCalculator.Calculate(new[] { profile }, UnitSpecTestFactory.CreateGrowthCategoryCatalog(), new[] { selected },
                category == CategoryKey.Main ? new MainCharacterPersistentState(10, 10, 5) : null, new Dictionary<CharacterInstanceId, int> { [grown.Id] = 0 }, default, catalog)[0];
            Assert.That((pending.Stats.AttackPower, pending.Stats.MaxHealth), Is.EqualTo((baseline.Stats.AttackPower, baseline.Stats.MaxHealth)));
            Assert.That((enhanced.Stats.AttackPower, enhanced.Stats.MaxHealth), Is.EqualTo((baseline.Stats.AttackPower, baseline.Stats.MaxHealth)));
            Assert.That((selected.Level, selected.Experience), Is.EqualTo((grown.Level, grown.Experience)));
            Assert.That(baseline.Stats.AttackPower, Is.EqualTo(category == CategoryKey.Cannon ? 5 + (grown.Level - 1) * 2 : 5));
            Assert.That(baseline.Stats.MaxHealth, Is.EqualTo(category == CategoryKey.Heavy ? 10 + (grown.Level - 1) * 3 : 10));
            if (category == CategoryKey.Main)
            {
                MainCharacterPersistentState Settle(OwnedCharacterRecord record)
                {
                    var state = new MainCharacterPersistentState(10, 10, 5);
                    for (int level = 2; level <= record.Level; level++)
                        state = MainGrowthRules.Apply(state, MainGrowthRules.Choose(42, record.Id, level), spec);
                    return state;
                }
                var a = Settle(grown);
                var b = Settle(selected);
                Assert.That((a.PersistentMaxHealth, a.PersistentAttack, a.PersistentDefense, a.CurrentHealth), Is.EqualTo((b.PersistentMaxHealth, b.PersistentAttack, b.PersistentDefense, b.CurrentHealth)));
            }
        }
    }
}
