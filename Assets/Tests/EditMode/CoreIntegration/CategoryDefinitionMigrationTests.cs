using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CategoryDefinitionMigrationTests
    {
        [Test]
        public void HeavyWithoutOffsets_ConstructsThroughAuthoringAndCorePreparation()
        {
            var unit = ScriptableObject.CreateInstance<UnitDataSO>();
            try
            {
                using (var serialized = new SerializedObject(unit))
                {
                    serialized.FindProperty("key").intValue = (int)EntityKey.Unit_ShotGun;
                    serialized.FindProperty("category").intValue = (int)CategoryKey.Heavy;
                    serialized.FindProperty("attackPower").intValue = 0;
                    serialized.FindProperty("physicalDefense").intValue = 5;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                var spec = UnitSpecAdapter.ReadUnit((UnitData)unit.GetData());
                var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
                var source = new ConfiguredMovementBattle.EncounterSource().Read(database, 1, new StageKey(1101));
                var categories = UnitSpecAdapter.ReadCategories(database.Config);
                Assert.That(categories.Single(c => c.Key == CategoryKey.Rider).RuleSpecs.Length, Is.Zero);
                Assert.That(categories.Single(c => c.Key == CategoryKey.Heavy).RuleSpecs.Length, Is.Zero);
                var definition = new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                    new[] { UnitSpecAdapter.ReadUnit(database.GetUnit(EntityKey.Unit_Akimbo)), spec,
                        UnitSpecAdapter.ReadUnit(database.GetUnit(EntityKey.Unit_Demon)) }, source.Structures, categories);
                var owned = new[]
                {
                    new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main),
                    new OwnedCharacterRecord(new CharacterInstanceId(2), spec.Key, CharacterOwnershipRole.Sub)
                };
                var prepared = BattlePreparationCalculator.Prepare(definition, owned,
                    new MainCharacterPersistentState(10, 10, 1), owned.ToDictionary(c => c.Id, _ => 0),
                    new InitialDrawPlan(new[] { owned[1].Id }), Array.Empty<CharacterCombatFacts>());
                var heavy = prepared.Characters.Single(c => c.CharacterKey == spec.Key);
                Assert.That(heavy.Stats.Category.Key, Is.EqualTo(CategoryKey.Heavy));
                Assert.That(heavy.Stats.PhysicalDefense, Is.EqualTo(5));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(unit);
            }
        }
    }
}
