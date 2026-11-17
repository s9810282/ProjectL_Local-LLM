using System;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using UnityEditor;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class DeadHandRankAuthoringTests
    {
        [Test]
        public void AuthoredEqualRanksAndPolicyReachCoreDefinition()
        {
            var asset = ScriptableObject.CreateInstance<TraitGrowthDefinitionDataSO>();
            try
            {
                var serialized = new SerializedObject(asset);
                serialized.FindProperty("group").intValue = (int)TraitGrowthGroup.Common;
                serialized.FindProperty("normal").managedReferenceValue = new DeadHandTraitAuthoring();
                serialized.FindProperty("rank2").managedReferenceValue = new DeadHandRankAuthoring();
                serialized.FindProperty("rank3").managedReferenceValue = new DeadHandRankAuthoring();
                serialized.FindProperty("allowUnchangedRankValues").boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var data = (TraitGrowthDefinitionData)asset.GetData();
                var normal = (DeadHandTraitSpec)TraitSpecAdapter.Read(data.Normal);
                Assert.That(normal.DamagePercent, Is.EqualTo(100));
                Assert.That(data.Rank2.Amount, Is.EqualTo(100));
                Assert.That(data.Rank3.Amount, Is.EqualTo(100));
                Assert.That(data.AllowUnchangedRankValues, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void PreparedGrowthIdentityIncludesPolicyAndRankValues()
        {
            var config = new CategoryConfigData(3, 4, 3, 2, 0);
            var query = new CoreIntegrationTestData(config);
            var basis = UnitSpecAdapter.ReadUnit(query.GetUnit(EntityKey.Unit_Akimbo));
            var profile = UnitSpecAdapter.ReadProfile(query, basis);
            MainSpecializationDefinitionSnapshot Snapshot(bool allowUnchanged, int rank3)
            {
                var definition = new TraitGrowthDefinition(new DeadHandTraitSpec(100), null, "", "", "", "",
                    new TraitRankValues(TraitKey.DeadHand, 125),
                    new TraitRankValues(TraitKey.DeadHand, rank3),
                    allowUnchanged);
                var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { definition });
                return new MainSpecializationDefinitionSnapshot(profile, catalog, config, new[] { basis });
            }

            Assert.That(Snapshot(true, 150).SameValues(Snapshot(true, 150)), Is.True);
            Assert.That(Snapshot(true, 150).SameValues(Snapshot(false, 150)), Is.False);
            Assert.That(Snapshot(true, 150).SameValues(Snapshot(true, 151)), Is.False);
        }
    }
}
