using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class LegacyEnhancementRetirementAuthoringTests
    {
        [Test]
        public void ConfiguredEmptyEnhancementInput_DoesNotProduceLegacyProducts()
        {
            using var fixture = new CoreAuthoringFixture();
            var table = fixture.Create<EnhancementDataTableSO>();
            using (var serialized = new SerializedObject(table))
            {
                serialized.FindProperty("databaseItems").ClearArray();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var query = new EnhancementTable(table);
            Assert.That(query.GetAll(), Is.Empty);
            Assert.That(query.TryGet(EnhancementKey.DoubleShot, out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => query.Get(EnhancementKey.DoubleShot));
            var detail = fixture.Create<EnhancementStageData>();
            var converted = (EnhancementStageDetail)detail.GetData();
            Assert.DoesNotThrow(() => converted.Validate());
            Assert.That(converted.TrainingPrices, Is.Null);
        }

        [TestCase(TraitKey.None)]
        [TestCase((TraitKey)9999)]
        public void UnknownAuthoring_PreservesExistingInvalidInputBehavior(TraitKey key)
        {
            var data = new ReservedTraitData(key);
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
            Assert.That(data.Validated, Is.True);
        }

        [TestCase(TraitKey.DoorBreaching)]
        [TestCase(TraitKey.ContemptForWeak)]
        [TestCase(TraitKey.PrecisionShot)]
        [TestCase(TraitKey.DoubleShot)]
        [TestCase(TraitKey.LuckyBullet)]
        [TestCase(TraitKey.ChainExplosion)]
        public void RetiredAuthoring_IsRejectedBeforePayloadValidation(TraitKey key)
        {
            var data = new ReservedTraitData(key);
            var failure = Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
            Assert.That(failure.Message, Does.Contain("Retired"));
            Assert.That(data.Validated, Is.False);
        }

        private sealed class ReservedTraitData : UnitTraitData
        {
            public override TraitKey TraitKey { get; }
            internal bool Validated { get; private set; }
            internal ReservedTraitData(TraitKey key) { TraitKey = key; }
            public override void Validate() { Validated = true; }
        }
    }
}
