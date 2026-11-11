using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class NewTraitAuthoringTests
    {
        private const string InGame = "Assets/ProjectL/DataBaseSO/InGameGroup";
        private readonly List<TraitGrowthDefinitionDataSO> _owned = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var so in _owned) UnityEngine.Object.DestroyImmediate(so);
            _owned.Clear();
        }

        [Test]
        public void GetDataReturnsFreshPureData()
        {
            var normal = new RampageTraitAuthoring();
            var so = Create(normal, TraitGrowthGroup.CategorySpecific);
            var before = Read(so);
            CoreAuthoringFixture.Set(normal, "baseAttackBonus", 20);
            var after = Read(so);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Normal, Is.Not.SameAs(before.Normal));
            Assert.That(((RampageTraitData)before.Normal).BaseAttackBonus, Is.Zero);
            Assert.That(((RampageTraitData)after.Normal).BaseAttackBonus, Is.EqualTo(20));
            Assert.That(typeof(RampageTraitData).GetProperties().Select(p => p.PropertyType),
                Is.EquivalentTo(new[] { typeof(TraitKey), typeof(int), typeof(int) }));
            Assert.That(Read(Create(new LavaCannonTraitAuthoring(), TraitGrowthGroup.CategorySpecific)).Normal,
                Is.TypeOf<LavaCannonTraitData>());
            Assert.That(Read(Create(new BleedingTraitAuthoring(), TraitGrowthGroup.Common)).Normal,
                Is.TypeOf<BleedingTraitData>());
        }

        [Test]
        public void AdaptersAndRankResolutionAgree()
        {
            var rampage = new RampageTraitAuthoring();
            CoreAuthoringFixture.Set(rampage, "baseAttackBonus", 20);
            var r = Definition(Read(Create(rampage, TraitGrowthGroup.CategorySpecific, RampageRank(1, 5), RampageRank(2, 7))));
            var b = Definition(Read(Create(new BleedingTraitAuthoring(), TraitGrowthGroup.Common, BleedingRank(2), BleedingRank(3))));
            var l = Definition(Read(Create(new LavaCannonTraitAuthoring(), TraitGrowthGroup.CategorySpecific)));
            Assert.That(l.Normal, Is.TypeOf<LavaCannonTraitSpec>());
            Assert.That(l.IsTrainable, Is.False);

            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            var query = database.InGameQuery;
            var units = query.UnitTableQuery.GetAllUnits().Select(UnitSpecAdapter.ReadUnit).ToArray();
            var profiles = BattleDefinitionAdapter.ReadAttackProfiles(database, units);
            var main = units.Single(unit => unit.Key == EntityKey.Unit_Akimbo);
            var steps = new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire),
                new TraitGrowthStepSpec(20, 2, TraitGrowthKind.Acquire) };
            var catalog = new TraitGrowthCatalog(new[]
            {
                new CategoryTraitProgressionSpec(CategoryKey.Main, steps, new[] { b.Key }),
                new CategoryTraitProgressionSpec(CategoryKey.Rider, Array.Empty<TraitGrowthStepSpec>(), new[] { r.Key }),
                new CategoryTraitProgressionSpec(CategoryKey.Cannon, Array.Empty<TraitGrowthStepSpec>(), new[] { l.Key }),
                new CategoryTraitProgressionSpec(CategoryKey.Heavy, Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>())
            }, new[] { r, b, l });
            var growth = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, r.Key), new TraitGrowthSelection(20, b.Key) })
                .WithPurchase(r.Key).WithPurchase(r.Key).WithPurchase(b.Key);
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), main.Key, CharacterOwnershipRole.Main,
                level: 2, growth: growth, specializationCategory: CategoryKey.Rider);
            Assert.That(EffectiveTraitProgression.Resolve(main, owned, catalog).Pool, Is.EquivalentTo(new[] { r.Key, b.Key }));
            var stats = OwnedCharacterStatsCalculator.CalculateForMains(profiles,
                UnitSpecAdapter.ReadCategories(query.CategoryConfigTableQuery.Config), new[] { owned },
                new Dictionary<CharacterInstanceId, MainCharacterPersistentState> { [owned.Id] = new(10, 10, 1) },
                new Dictionary<CharacterInstanceId, int> { [owned.Id] = 0 }, default, catalog).Single();
            var profile = OwnedUnitSpecResolver.BuildProfiles(profiles, new[] { owned }, catalog).Single(p => p.OwnedId == owned.Id);

            Assert.That(stats.TraitRanks[r.Key], Is.EqualTo(3));
            Assert.That(stats.TraitRanks[b.Key], Is.EqualTo(2));
            Assert.That(stats.TraitSettings.TryGet<RampageTraitSpec>(out var rankedRampage), Is.True);
            Assert.That(stats.TraitSettings.TryGet<BleedingTraitSpec>(out var rankedBleeding), Is.True);
            Assert.That((rankedRampage.BaseAttackBonus, rankedRampage.RankAttackBonus, rankedRampage.MovementBonus), Is.EqualTo((20, 7, 2)));
            Assert.That(rankedBleeding.DamagePerStack, Is.EqualTo(2));
            Assert.That(profile.Definition.Traits.ToArray().Single(t => t.TraitKey == r.Key).SameValues(rankedRampage), Is.True);
            Assert.That(profile.Definition.Traits.ToArray().Single(t => t.TraitKey == b.Key).SameValues(rankedBleeding), Is.True);
        }

        [Test]
        public void InvalidValuesFailBeforeActivation()
        {
            var rampage = new RampageTraitAuthoring();
            CoreAuthoringFixture.Set(rampage, "baseAttackBonus", -1);
            Assert.Throws<InvalidOperationException>(() => Read(Create(rampage, TraitGrowthGroup.CategorySpecific)));
            CoreAuthoringFixture.Set(rampage, "baseAttackBonus", int.MaxValue);
            Assert.Throws<OverflowException>(() => Definition(Read(Create(rampage, TraitGrowthGroup.CategorySpecific,
                RampageRank(1, 5), RampageRank(2, 7)))));
            var bleeding = new BleedingTraitAuthoring();
            CoreAuthoringFixture.Set(bleeding, "damagePerStack", 0);
            Assert.Throws<InvalidOperationException>(() => Read(Create(bleeding, TraitGrowthGroup.Common)));
            Assert.Throws<InvalidOperationException>(() => Read(Create(new BleedingTraitAuthoring(), TraitGrowthGroup.Common,
                BleedingRank(0), BleedingRank(3))));
            Assert.Throws<InvalidOperationException>(() => Read(Create(new BleedingTraitAuthoring(), TraitGrowthGroup.Common,
                null, BleedingRank(3))));
        }

        [TestCase(TraitKey.Rampage, TraitGrowthGroup.CategorySpecific)]
        [TestCase(TraitKey.LavaCannon, TraitGrowthGroup.CategorySpecific)]
        [TestCase(TraitKey.Bleeding, TraitGrowthGroup.Common)]
        public void DormantAssetsDoNotEnterLivePools(TraitKey key, TraitGrowthGroup group)
        {
            string path = $"{InGame}/TraitGrowth/Item/TraitGrowth_{key}.asset";
            var so = AssetDatabase.LoadAssetAtPath<TraitGrowthDefinitionDataSO>(path);
            Assert.That(so, Is.Not.Null, path);
            var definition = Definition(Read(so));
            Assert.That((definition.Key, definition.Group), Is.EqualTo((key, group)));
            Assert.That(definition.Enhanced, Is.Null);
            Assert.That(definition.Name, Is.Not.Empty);
            Assert.That(definition.Description, Is.Not.Empty);
            if (definition.Normal is RampageTraitSpec rampage)
            {
                Assert.That((rampage.BaseAttackBonus, rampage.RankAttackBonus, rampage.MovementBonus), Is.EqualTo((0, 0, 1)));
                Assert.That((definition.Rank2.Amount, definition.Rank2.SecondAmount), Is.EqualTo((1, (int?)5)));
                Assert.That((definition.Rank3.Amount, definition.Rank3.SecondAmount), Is.EqualTo((2, (int?)7)));
            }
            else if (definition.Normal is BleedingTraitSpec bleeding)
                Assert.That((bleeding.DamagePerStack, definition.Rank2.Amount, definition.Rank3.Amount), Is.EqualTo((1, 2, 3)));
            else Assert.That(definition.IsTrainable, Is.False);

            var roots = new[] { $"{InGame}/TraitGrowth/TraitGrowthDefinitionTable.asset",
                $"{InGame}/TraitGrowth/CategoryTraitProgressionTable.asset", $"{InGame}/Category/Item/CategoryConfig_Default.asset",
                $"{InGame}/Unit/UnitDataTable.asset" };
            foreach (string root in roots)
                Assert.That(AssetDatabase.LoadMainAssetAtPath(root), Is.Not.Null, root);
            Assert.That(AssetDatabase.GetDependencies(roots, true), Does.Not.Contain(path));
        }

        private TraitGrowthDefinitionDataSO Create(UnitTraitAuthoring normal, TraitGrowthGroup group,
            TraitRankAuthoring rank2 = null, TraitRankAuthoring rank3 = null)
        {
            var so = ScriptableObject.CreateInstance<TraitGrowthDefinitionDataSO>();
            _owned.Add(so);
            CoreAuthoringFixture.Set(so, "normal", normal);
            CoreAuthoringFixture.Set(so, "group", group);
            CoreAuthoringFixture.Set(so, "rank2", rank2);
            CoreAuthoringFixture.Set(so, "rank3", rank3);
            return so;
        }

        private static TraitGrowthDefinitionData Read(TraitGrowthDefinitionDataSO so) => (TraitGrowthDefinitionData)so.GetData();

        private static TraitGrowthDefinition Definition(TraitGrowthDefinitionData data) =>
            new(data.Group, TraitSpecAdapter.Read(data.Normal), data.Enhanced == null ? null : TraitSpecAdapter.Read(data.Enhanced),
                data.Name, data.Description, data.EnhancedName, data.EnhancedDescription, data.Rank2, data.Rank3);

        private static RampageRankAuthoring RampageRank(int movement, int offset)
        {
            var rank = new RampageRankAuthoring();
            CoreAuthoringFixture.Set(rank, "movementBonus", movement);
            CoreAuthoringFixture.Set(rank, "attackOffset", offset);
            return rank;
        }

        private static BleedingRankAuthoring BleedingRank(int damage)
        {
            var rank = new BleedingRankAuthoring();
            CoreAuthoringFixture.Set(rank, "damagePerStack", damage);
            return rank;
        }
    }
}
