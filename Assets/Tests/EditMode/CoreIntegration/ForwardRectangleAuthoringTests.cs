using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ForwardRectangleAuthoringTests
    {
        [Test]
        public void WidthSurvivesSOAndInvalidatesPreparedMethodIdentity()
        {
            var so = ScriptableObject.CreateInstance<TargetingMethodDataSO>();
            try
            {
                var serialized = new SerializedObject(so);
                serialized.FindProperty("shape").intValue = (int)TargetingShape.ForwardRectangle;
                serialized.FindProperty("choiceMode").intValue = (int)TargetingChoiceMode.Direction;
                var width = serialized.FindProperty("width");
                width.intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var three = UnitSpecAdapter.ReadMethod((TargetingMethodData)so.GetData());
                width.intValue = 2;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var two = UnitSpecAdapter.ReadMethod((TargetingMethodData)so.GetData());
                Assert.That(three.Width, Is.EqualTo(3));
                Assert.That(two.Width, Is.EqualTo(2));

                var config = new CategoryConfigData(3, 4, 3, 2, 0);
                var basis = UnitSpecAdapter.ReadUnit(new CoreIntegrationTestData(config).GetUnit(EntityKey.Unit_Akimbo));
                MainSpecializationDefinitionSnapshot Snapshot(TargetingMethodSpec method) =>
                    new MainSpecializationDefinitionSnapshot(new CombatAttackProfile(basis, method),
                        TraitGrowthCatalog.Disabled, config, new[] { basis });
                Assert.That(Snapshot(three).SameValues(Snapshot(two)), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(so);
            }
        }
    }
}
