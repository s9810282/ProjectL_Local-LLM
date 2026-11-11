using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitTurnConfigurationTests
    {
        [Test]
        public void RegenerationUnitCopy_FreezesConfiguredIntervalAndAmount()
        {
            using var fixture = new CoreAuthoringFixture();
            var originalTrait = new RegenerationTraitAuthoring();
            Set(originalTrait, "healAmount", 10);
            Set(originalTrait, "healEveryOwnSideTurnEnds", 1);
            var original = fixture.CreateUnit(EntityKey.Unit_UndeadHeavy, CategoryKey.Heavy, originalTrait);
            var copy = fixture.Clone(original);
            using var serialized = new SerializedObject(copy);
            var authored = serialized.FindProperty("traits").GetArrayElementAtIndex(0);
            authored.FindPropertyRelative("healAmount").intValue = 17;
            authored.FindPropertyRelative("healEveryOwnSideTurnEnds").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var data = (RegenerationTraitData)((UnitData)copy.GetData()).Traits.Single();
            var spec = (RegenerationTraitSpec)TraitSpecAdapter.Read(data);
            AssertChain(data, "HealEveryOwnSideTurnEnds", 2);
            Assert.That(spec.HealAmount, Is.EqualTo(17));
            authored.FindPropertyRelative("healEveryOwnSideTurnEnds").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(((RegenerationTraitData)((UnitData)copy.GetData()).Traits.Single()).HealEveryOwnSideTurnEnds, Is.EqualTo(1));
            Assert.That(data.HealEveryOwnSideTurnEnds, Is.EqualTo(2));
            Assert.That(spec.HealEveryOwnSideTurnEnds, Is.EqualTo(2));
            var source = (RegenerationTraitData)((UnitData)original.GetData()).Traits.Single();
            Assert.That((source.HealAmount, source.HealEveryOwnSideTurnEnds), Is.EqualTo((10, 1)));
        }

        [Test]
        public void RegenerationGrowthCopy_KeepsNormalAndEnhancedIntervalsIndependent()
        {
            var copy = ScriptableObject.CreateInstance<TraitGrowthDefinitionDataSO>();
            try
            {
                var normal = new RegenerationTraitAuthoring();
                var enhanced = new RegenerationTraitAuthoring();
                Set(normal, "healAmount", 10);
                Set(enhanced, "healAmount", 20);
                Set(normal, "healEveryOwnSideTurnEnds", 2);
                Set(enhanced, "healEveryOwnSideTurnEnds", 3);
                using (var serialized = new SerializedObject(copy))
                {
                    serialized.FindProperty("group").intValue = (int)TraitGrowthGroup.Common;
                    serialized.FindProperty("normal").managedReferenceValue = normal;
                    serialized.FindProperty("enhanced").managedReferenceValue = enhanced;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var data = (TraitGrowthDefinitionData)copy.GetData();
                AssertChain(data.Normal, "HealEveryOwnSideTurnEnds", 2);
                AssertChain(data.Enhanced, "HealEveryOwnSideTurnEnds", 3);
                Assert.That(((RegenerationTraitData)data.Normal).HealAmount, Is.EqualTo(10));
                Assert.That(((RegenerationTraitData)data.Enhanced).HealAmount, Is.EqualTo(20));
            }
            finally { Object.DestroyImmediate(copy); }
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveRegenerationInterval_FailsPureValidation(int interval)
        {
            var data = new RegenerationTraitData(10, interval);
            Assert.Throws<InvalidOperationException>(() => data.Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
        }

        [Test]
        public void UnitCopy_CarriesDurationAndFreezesAlreadyBuiltDefinitions()
        {
            using var fixture = new CoreAuthoringFixture();
            var originalTrait = new HuntersMarkTraitAuthoring();
            Set(originalTrait, "bonusDamagePercent", 30);
            Set(originalTrait, "durationAffectedSideTurnEnds", 1);
            var original = fixture.CreateUnit(EntityKey.Unit_ShotGun, CategoryKey.Heavy, originalTrait);
            var copy = fixture.Clone(original);
            using var serialized = new SerializedObject(copy);
            var authored = serialized.FindProperty("traits").GetArrayElementAtIndex(0);
            authored.FindPropertyRelative("bonusDamagePercent").intValue = 25;
            authored.FindPropertyRelative("durationAffectedSideTurnEnds").intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var data = (HuntersMarkTraitData)((UnitData)copy.GetData()).Traits.Single();
            var spec = (HuntersMarkTraitSpec)TraitSpecAdapter.Read(data);
            Assert.That(data.DurationAffectedSideTurnEnds, Is.EqualTo(3));
            Assert.That(spec.DurationAffectedSideTurnEnds, Is.EqualTo(3));
            AssertChain(data, "DurationAffectedSideTurnEnds", 3);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource(spec));
            var snapshot = battle.Actor.Stats.TraitStatuses.ToArray().OfType<HuntersMarkTraitStatusSnapshot>().Single();
            Assert.That(snapshot.BonusDamagePercent, Is.EqualTo(25));
            Assert.That(snapshot.DurationAffectedSideTurnEnds, Is.EqualTo(3));
            authored.FindPropertyRelative("durationAffectedSideTurnEnds").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(((HuntersMarkTraitData)((UnitData)copy.GetData()).Traits.Single()).DurationAffectedSideTurnEnds, Is.EqualTo(1));
            Assert.That(data.DurationAffectedSideTurnEnds, Is.EqualTo(3));
            Assert.That(spec.DurationAffectedSideTurnEnds, Is.EqualTo(3));
            Assert.That(snapshot.DurationAffectedSideTurnEnds, Is.EqualTo(3));
            var source = ((UnitData)original.GetData()).Traits.OfType<HuntersMarkTraitData>().Single();
            Assert.That((source.BonusDamagePercent, source.DurationAffectedSideTurnEnds), Is.EqualTo((30, 1)));
        }

        [Test]
        public void GrowthDefinitionCopy_KeepsNormalAndEnhancedDurationsIndependent()
        {
            using var fixture = new CoreAuthoringFixture();
            var original = fixture.Create<TraitGrowthDefinitionDataSO>();
            var sourceNormal = new HuntersMarkTraitAuthoring();
            var sourceEnhanced = new HuntersMarkTraitAuthoring();
            Set(sourceNormal, "bonusDamagePercent", 30);
            Set(sourceEnhanced, "bonusDamagePercent", 40);
            Set(sourceNormal, "durationAffectedSideTurnEnds", 1);
            Set(sourceEnhanced, "durationAffectedSideTurnEnds", 2);
            CoreAuthoringFixture.Set(original, "normal", sourceNormal);
            CoreAuthoringFixture.Set(original, "enhanced", sourceEnhanced);
            CoreAuthoringFixture.Set(original, "group", TraitGrowthGroup.Common);
            var copy = fixture.Clone(original);
            using (var serialized = new SerializedObject(copy))
            {
                serialized.FindProperty("normal").FindPropertyRelative("bonusDamagePercent").intValue = 25;
                serialized.FindProperty("enhanced").FindPropertyRelative("bonusDamagePercent").intValue = 25;
                serialized.FindProperty("enhanced").FindPropertyRelative("durationAffectedSideTurnEnds").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var data = (TraitGrowthDefinitionData)copy.GetData();
            AssertChain(data.Normal, "DurationAffectedSideTurnEnds", 1);
            AssertChain(data.Enhanced, "DurationAffectedSideTurnEnds", 3);
            Assert.That(((HuntersMarkTraitData)data.Normal).BonusDamagePercent, Is.EqualTo(25));
            Assert.That(((HuntersMarkTraitData)data.Enhanced).BonusDamagePercent, Is.EqualTo(25));
            Assert.That(data.Normal.TraitKey, Is.EqualTo(data.Enhanced.TraitKey));
            var source = (TraitGrowthDefinitionData)original.GetData();
            Assert.That(source.Key, Is.EqualTo(TraitKey.HuntersMark));
            Assert.That((((HuntersMarkTraitData)source.Normal).BonusDamagePercent,
                ((HuntersMarkTraitData)source.Enhanced).BonusDamagePercent), Is.EqualTo((30, 40)));
            Assert.That((((HuntersMarkTraitData)source.Normal).DurationAffectedSideTurnEnds,
                ((HuntersMarkTraitData)source.Enhanced).DurationAffectedSideTurnEnds), Is.EqualTo((1, 2)));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void NonpositiveExposureDuration_FailsPureValidation(int duration)
        {
            var data = new HuntersMarkTraitData(25, duration);
            Assert.Throws<InvalidOperationException>(() => data.Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkTraitSpec(25, duration));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkTraitStatusSnapshot(25, duration));
        }

        [TestCase("Vanguard", "durationControllerTurnEnds", "DurationControllerTurnEnds")]
        [TestCase("CleanSweep", "durationControllerTurnEnds", "DurationControllerTurnEnds")]
        [TestCase("MoraleBoost", "durationControllerTurnEnds", "DurationControllerTurnEnds")]
        [TestCase("Disarm", "durationAffectedSideTurnEnds", "DurationAffectedSideTurnEnds")]
        [TestCase("AnkleBreaker", "durationAffectedSideTurnEnds", "DurationAffectedSideTurnEnds")]
        [TestCase("SoloAction", "resetEveryControllerTurnEnds", "ResetEveryControllerTurnEnds")]
        [TestCase("Cohesion", "resetEveryControllerTurnEnds", "ResetEveryControllerTurnEnds")]
        [TestCase("GroundCompaction", "opposingTurnEndsToExpire", "OpposingTurnEndsToExpire")]
        [TestCase("DustScreen", "opposingTurnEndsToExpire", "OpposingTurnEndsToExpire")]
        public void ExistingProducerClock_RoundTripsNondefaultThree(string stem, string field, string property)
        {
            var type = typeof(UnitTraitAuthoring).Assembly.GetType(typeof(UnitTraitAuthoring).Namespace + "." + stem + "TraitAuthoring", true);
            var authoring = (UnitTraitAuthoring)Activator.CreateInstance(type);
            foreach (var numeric in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(f => f.FieldType == typeof(int)))
                if ((int)numeric.GetValue(authoring) < 0) numeric.SetValue(authoring, 5);
            Set(authoring, field, 3);
            var restored = (UnitTraitAuthoring)JsonUtility.FromJson(JsonUtility.ToJson(authoring), type);
            AssertChain(restored.ToData(), property, 3);
        }

        private static void AssertChain(UnitTraitData data, string property, int expected)
        {
            data.Validate();
            var spec = TraitSpecAdapter.Read(data);
            var factoryType = typeof(HuntersMarkTraitDefinitionFactory).Assembly.GetType(
                typeof(HuntersMarkTraitDefinitionFactory).Namespace + "." + data.TraitKey + "TraitDefinitionFactory", true);
            var definition = factoryType.GetMethod("BuildDefinition").Invoke(Activator.CreateInstance(factoryType), new object[] { spec });
            foreach (var value in new object[] { data, spec, definition })
                Assert.That(value.GetType().GetProperty(property)?.GetValue(value), Is.EqualTo(expected), value.GetType().Name + "." + property);
        }

        private static void Set(object value, string field, int amount) =>
            value.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(value, amount);
    }
}
