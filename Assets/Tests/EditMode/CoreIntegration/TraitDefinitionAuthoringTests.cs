using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitDefinitionAuthoringTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void Cleanup()
        {
            foreach (var item in _objects) Object.DestroyImmediate(item);
            _objects.Clear();
        }

        [TestCase("Vanguard", "attackBonus,durationControllerTurnEnds", "3,2")]
        [TestCase("CleanSweep", "attackBonusPerAllyAttack,durationControllerTurnEnds", "4,2")]
        [TestCase("SoloAction", "shieldAmount,radiusCells,requiredOccupiedNeighborCount,resetEveryControllerTurnEnds", "13,2,3,2")]
        [TestCase("CallOfBlood", "enemyMaxHealthHealPercent", "20")]
        [TestCase("MoraleBoost", "defeatedEnemyBaseAttackPercent,durationControllerTurnEnds", "20,2")]
        [TestCase("Disarm", "durationAffectedSideTurnEnds", "2")]
        [TestCase("AnkleBreaker", "durationAffectedSideTurnEnds", "2")]
        [TestCase("Cohesion", "defensePerAdjacentAlly,shieldAmountPerAlly,radiusCells,resetEveryControllerTurnEnds", "3,7,2,2")]
        [TestCase("Sacrifice", "radiusCells,maxTriggersPerEnemyTurn", "3,2")]
        [TestCase("Taunt", "radiusCells", "3")]
        [TestCase("Entrenchment", "radiusCells,damageReductionPercent", "2,20")]
        [TestCase("SteadfastWill", "", "")]
        [TestCase("GroundCompaction", "movementRangeBonus,opposingTurnEndsToExpire", "3,2")]
        [TestCase("DustScreen", "attackRangeReduction,attackDamageReductionPercent,opposingTurnEndsToExpire", "3,20,2")]
        [TestCase("Leap", "", "")]
        [TestCase("Acceleration", "attackPerMovedCell", "3")]
        [TestCase("StationaryFire", "extraDamagePercent", "120")]
        [TestCase("ArmorBreaker", "defenseReductionPercentPerHit", "20")]
        [TestCase("FinishingShot", "attackThresholdPercent", "25")]
        [TestCase("TrackingShot", "targetHealthThresholdPercent,extraDamagePercent", "60,20")]
        [TestCase("Hunter", "attackPerTargetKill,maxRewardsPerStage", "2,4")]
        [TestCase("FocusedBombardment", "requiredEnemyCount,extraDamagePercent", "2,70")]
        public void Definitions_RoundTripEveryTunable(string stem, string fields, string values)
        {
            var authoring = Authoring(stem);
            string[] names = fields.Length == 0 ? Array.Empty<string>() : fields.Split(',');
            int[] numbers = values.Length == 0 ? Array.Empty<int>() : values.Split(',').Select(int.Parse).ToArray();
            // 이 수치는 변환 누락을 드러내기 위한 테스트 전용 값이다.
            for (int i = 0; i < names.Length; i++) Set(authoring, names[i], numbers[i]);
            string json = JsonUtility.ToJson(authoring);
            var restored = (UnitTraitAuthoring)JsonUtility.FromJson(json, authoring.GetType());
            var definition = Definition(restored);
            var first = (TraitGrowthDefinitionData)definition.GetData();
            var second = (TraitGrowthDefinitionData)definition.GetData();
            var spec = TraitSpecAdapter.Read(first.Normal);
            Assert.That(second.Normal, Is.Not.SameAs(first.Normal));
            Assert.That(first.Enhanced, Is.Null);
            Assert.That(first.Key.ToString(), Is.EqualTo(stem));
            Assert.That(spec.TraitKey, Is.EqualTo(first.Key));
            for (int i = 0; i < names.Length; i++)
            {
                string property = char.ToUpperInvariant(names[i][0]) + names[i].Substring(1);
                Assert.That(first.Normal.GetType().GetProperty(property)?.GetValue(first.Normal), Is.EqualTo(numbers[i]), property);
                Assert.That(spec.GetType().GetProperty(property)?.GetValue(spec), Is.EqualTo(numbers[i]), property);
            }
        }

        [TestCase("Vanguard", "attackBonus")]
        [TestCase("CleanSweep", "attackBonusPerAllyAttack")]
        [TestCase("Cohesion", "defensePerAdjacentAlly")]
        [TestCase("Cohesion", "shieldAmountPerAlly")]
        [TestCase("Acceleration", "attackPerMovedCell")]
        [TestCase("ArmorBreaker", "defenseReductionPercentPerHit")]
        [TestCase("TrackingShot", "extraDamagePercent")]
        [TestCase("Hunter", "attackPerTargetKill")]
        public void InvalidOrUnsetMagnitude_RejectsWithFieldContext(string stem, string field)
        {
            var authoring = Authoring(stem);
            if (stem == "Cohesion")
            {
                Set(authoring, "defensePerAdjacentAlly", 3);
                Set(authoring, "shieldAmountPerAlly", 4);
            }
            Set(authoring, field, -1);
            var definition = Definition(authoring);
            definition.name = "UnsetTraitFixture";
            var failure = Assert.Throws<InvalidOperationException>(() => definition.GetData());
            Assert.That(failure.Message, Does.Contain("UnsetTraitFixture").And.Contain(stem).And.Contain(field));
        }

        [Test]
        public void CategoryPoolMembership_IsEditableWithoutCopyingDefinition()
        {
            var definition = Definition(new KnockbackTraitAuthoring());
            var heavy = Row(CategoryKey.Heavy, definition);
            var rider = Row(CategoryKey.Rider, definition);
            Assert.That(((CategoryTraitProgressionData)heavy.GetData()).Pool, Is.EqualTo(new[] { TraitKey.Knockback }));
            Assert.That(((CategoryTraitProgressionData)rider.GetData()).Pool, Is.EqualTo(new[] { TraitKey.Knockback }));
            Set(heavy, "pool", new List<TraitGrowthDefinitionDataSO>());
            Assert.That(((CategoryTraitProgressionData)heavy.GetData()).Pool, Is.Empty);
            Assert.That(((CategoryTraitProgressionData)rider.GetData()).Pool, Is.EqualTo(new[] { TraitKey.Knockback }));
        }

        [Test]
        public void ImplementedHunter_NativeAndPoolPayloadsAreAccepted()
        {
            var authoring = Authoring("Hunter");
            Set(authoring, "attackPerTargetKill", 2);
            var definition = Definition(authoring);
            Assert.DoesNotThrow(() => definition.GetData());
            var category = Row(CategoryKey.Cannon, definition);
            category.name = "ConfiguredPoolFixture";
            Assert.That(((CategoryTraitProgressionData)category.GetData()).Pool, Is.EqualTo(new[] { TraitKey.Hunter }));
            var unit = Create<UnitDataSO>();
            unit.name = "ConfiguredNativeFixture";
            Set(unit, "key", EntityKey.Unit_Sniper);
            Set(unit, "category", CategoryKey.Cannon);
            Set(unit, "attackPower", 0);
            Set(unit, "traits", new List<UnitTraitAuthoring> { authoring });
            var data = (UnitData)unit.GetData();
            var specification = (HunterTraitSpec)TraitSpecAdapter.Read(data.Traits.Single());
            Assert.That(specification.AttackPerTargetKill, Is.EqualTo(2));
        }

        private static UnitTraitAuthoring Authoring(string stem)
        {
            Type type = typeof(UnitTraitData).Assembly.GetType(typeof(UnitTraitData).Namespace + "." + stem + "TraitAuthoring");
            Assert.That(type, Is.Not.Null, "The selectable typed authoring payload is required: " + stem);
            return (UnitTraitAuthoring)Activator.CreateInstance(type, true);
        }

        private TraitGrowthDefinitionDataSO Definition(UnitTraitAuthoring authoring)
        {
            var result = Create<TraitGrowthDefinitionDataSO>();
            Set(result, "normal", authoring);
            return result;
        }

        private CategoryTraitProgressionDataSO Row(CategoryKey key, TraitGrowthDefinitionDataSO definition)
        {
            var result = Create<CategoryTraitProgressionDataSO>();
            Set(result, "category", key);
            Set(result, "pool", new List<TraitGrowthDefinitionDataSO> { definition });
            return result;
        }

        private T Create<T>() where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            _objects.Add(value);
            return value;
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
