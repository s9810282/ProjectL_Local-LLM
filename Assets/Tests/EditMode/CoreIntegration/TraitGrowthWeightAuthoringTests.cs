using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthWeightAuthoringTests
    {
        [TestCase(0, 2)]
        [TestCase(1, 0)]
        [TestCase(-1, 2)]
        [TestCase(1, -2)]
        [TestCase(int.MaxValue, 1)]
        public void Authoring_InvalidWeightsIdentifyCategoryAsset(int commonWeight, int categoryWeight)
        {
            using var fixture = new CoreAuthoringFixture();
            var row = fixture.Create<CategoryTraitProgressionDataSO>();
            row.name = "Invalid weight category fixture";
            CoreAuthoringFixture.Set(row, "category", CategoryKey.Heavy);
            CoreAuthoringFixture.Set(row, "commonWeight", commonWeight);
            CoreAuthoringFixture.Set(row, "categoryWeight", categoryWeight);

            var error = Assert.Throws<InvalidOperationException>(() => row.GetData());
            Assert.That(error.Message, Does.Contain(row.name));
            Assert.That(error.InnerException, Is.InstanceOf<ArgumentException>());
        }

        [TestCase(0)]
        [TestCase(999)]
        public void ExplicitGroup_RejectsUnspecifiedAndUnknown(int group)
        {
            Assert.That(() => new TraitGrowthDefinitionData((TraitGrowthGroup)group, new KnockbackTraitData(1),
                null, "knockback", "", "", ""), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void LegacyConstructors_PreserveExistingCalls()
        {
            var definition = new TraitGrowthDefinitionData(new KnockbackTraitData(1), null, "knockback", "", "", "");
            var row = new CategoryTraitProgressionData(CategoryKey.Heavy, Array.Empty<TraitGrowthStepSpec>(),
                new[] { TraitKey.Knockback });

            Assert.That(definition.Key, Is.EqualTo(TraitKey.Knockback));
            Assert.That(definition.Group, Is.EqualTo(TraitGrowthGroup.Common));
            Assert.That((row.CommonWeight, row.CategoryWeight), Is.EqualTo((1, 2)));
        }

        [Test]
        public void Authoring_RequiresGroupAndPreservesItAcrossSerialization()
        {
            using var fixture = new CoreAuthoringFixture();
            var definition = fixture.Create<TraitGrowthDefinitionDataSO>();
            definition.name = "Unclassified growth test definition";
            CoreAuthoringFixture.Set(definition, "normal", new KnockbackTraitAuthoring());

            Assert.That(() => definition.GetData(), Throws.InvalidOperationException.With.Message.Contains(definition.name));

            using var serialized = new SerializedObject(definition);
            var group = serialized.FindProperty("group");
            group.intValue = (int)TraitGrowthGroup.CategorySpecific;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var captured = (TraitGrowthDefinitionData)definition.GetData();
            var clone = fixture.Clone(definition);
            Assert.That(((TraitGrowthDefinitionData)clone.GetData()).Group, Is.EqualTo(TraitGrowthGroup.CategorySpecific));

            group.intValue = (int)TraitGrowthGroup.Common;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(((TraitGrowthDefinitionData)definition.GetData()).Group, Is.EqualTo(TraitGrowthGroup.Common));
            Assert.That(captured.Group, Is.EqualTo(TraitGrowthGroup.CategorySpecific));
        }

        [Test]
        public void Authoring_PropagatesWeightsToCore()
        {
            using var fixture = new CoreAuthoringFixture();
            var definition = fixture.Create<TraitGrowthDefinitionDataSO>();
            CoreAuthoringFixture.Set(definition, "normal", new KnockbackTraitAuthoring());
            CoreAuthoringFixture.Set(definition, "group", TraitGrowthGroup.CategorySpecific);

            var heavyRow = fixture.Create<CategoryTraitProgressionDataSO>();
            CoreAuthoringFixture.Set(heavyRow, "category", CategoryKey.Heavy);
            CoreAuthoringFixture.Set(heavyRow, "pool", new List<TraitGrowthDefinitionDataSO> { definition });
            using (var serialized = new SerializedObject(heavyRow))
            {
                serialized.FindProperty("commonWeight").intValue = 3;
                serialized.FindProperty("categoryWeight").intValue = 7;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var roundTripped = fixture.Clone(heavyRow);
            var data = (CategoryTraitProgressionData)roundTripped.GetData();
            Assert.That((data.CommonWeight, data.CategoryWeight), Is.EqualTo((3, 7)));

            var rows = new[] { CategoryKey.Main, CategoryKey.Rider, CategoryKey.Cannon }.Select(key =>
            {
                var row = fixture.Create<CategoryTraitProgressionDataSO>();
                CoreAuthoringFixture.Set(row, "category", key);
                return row;
            }).Append(roundTripped).ToArray();
            var categoryTable = fixture.Create<CategoryTraitProgressionDataTableSO>();
            SetItems(categoryTable, rows);
            var definitionTable = fixture.Create<TraitGrowthDefinitionDataTableSO>();
            SetItems(definitionTable, definition);

            var catalog = TraitGrowthDefinitionAdapter.Build(new CategoryTraitProgressionTable(categoryTable),
                new TraitGrowthDefinitionTable(definitionTable));
            var heavy = catalog.GetCategory(CategoryKey.Heavy);
            Assert.That((heavy.CommonWeight, heavy.CategoryWeight), Is.EqualTo((3, 7)));
            Assert.That(catalog.GetTrait(TraitKey.Knockback).Group, Is.EqualTo(TraitGrowthGroup.CategorySpecific));
        }

        private static void SetItems(DatabaseTable table, params DatabaseItem[] items)
        {
            using var serialized = new SerializedObject(table);
            var rows = serialized.FindProperty("databaseItems");
            rows.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                rows.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
