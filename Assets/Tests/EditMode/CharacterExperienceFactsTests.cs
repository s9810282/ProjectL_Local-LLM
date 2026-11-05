using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    /// <summary>경험치·레벨업 설계 §4.1·§4.3: 적 생성 레벨과 아군 획득 경험치는 전투 사실 한 곳에 있고 사본 경로에서 잃지 않는다.</summary>
    public sealed class CharacterExperienceFactsTests
    {
        [Test]
        public void Contract_DefaultsByOwner_AndRejectsWrongSideOrRange()
        {
            var enemy = CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy);
            var ally = CombatCommandTestData.Character(6, EntityKey.Unit_Akimbo, new CellCoord(3, 2), Owner.Player);

            Assert.That((enemy.Experience.EnemyLevel, enemy.Experience.Earned), Is.EqualTo(((int?)1, 0)));
            Assert.That((ally.Experience.EnemyLevel, ally.Experience.Earned), Is.EqualTo(((int?)null, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterExperienceFacts.ForEnemy(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterExperienceFacts.ForAlly(-1));
            Assert.Throws<ArgumentException>(() => With(enemy, CharacterExperienceFacts.ForAlly(3)));
            Assert.Throws<ArgumentException>(() => With(ally, CharacterExperienceFacts.ForEnemy(2)));
            Assert.That(CharacterExperienceFacts.ForEnemy(3).SameValues(CharacterExperienceFacts.ForEnemy(3)), Is.True);
            Assert.That(CharacterExperienceFacts.ForAlly(3).SameValues(CharacterExperienceFacts.ForAlly(4)), Is.False);
        }

        [TestCase(1)]
        [TestCase(3)]
        public void CreateEnemy_RecordsCreationLevel_OwnedStartsWithoutEarnings(int level)
        {
            var spec = CombatCommandTestData.Spec(EntityKey.Unit_Demon, 20, CategoryKey.Rider);
            UnitDefinition definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(),
                new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog())).GetRequired(spec.Key);
            var cell = new CellCoord(2, 2);

            var enemy = BattleCharacterCalculator.CreateEnemy(new EntityId(9), definition, level, cell, Rotation4.R0,
                new[] { cell }, TraitRegistry.CreateDefault());
            var owned = BattleCharacterCalculator.CreateOwned(new EntityId(10), definition,
                new OwnedCharacterRecord(new CharacterInstanceId(3), spec.Key, CharacterOwnershipRole.Sub, level), null, 0,
                null, cell, Rotation4.R0, new[] { cell }, TraitRegistry.CreateDefault());

            Assert.That(enemy.Experience.EnemyLevel, Is.EqualTo(level));
            Assert.That(owned.Experience.EnemyLevel, Is.Null);
            Assert.That(owned.Experience.Earned, Is.Zero);
        }

        [Test]
        public void DeferredSummon_DoesNotCreateExperienceOrChangeExistingFacts()
        {
            var input = CombatSummonTests.Input(new CellCoord(3, 3));
            var result = CombatSummonTests.Summon(input);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DeferredUnitTurnTiming"));
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        private static IEnumerable<TestCaseData> CopyPaths()
        {
            yield return Case("CombatCommandCalculator.Copy", c => CombatCommandCalculator.Copy(c, c.Health, c.Stats));
            yield return Case("CharacterSettlementCalculator.Copy", c => CharacterSettlementCalculator.Copy(c, c.Health, c.Stats));
            yield return Case("CombatDebuffApplication.WithDebuffs", c => CombatDebuffApplication.WithDebuffs(c, CharacterDebuffState.Empty));
            yield return Case("CharacterCombatFacts.WithPlacement", c => c.WithPlacement(c.Area, c.ReserveSlotIndex,
                new CellCoord(5, 5), c.Rotation, new[] { new CellCoord(5, 5) }, actionUse: UnitActionUse.Moved));
            yield return Case("CombatPlacementCalculator.Location", c => CombatPlacementCalculator.Location(c,
                CharacterRuntimeArea.Field, null, new CellCoord(5, 5), Rotation4.R0));
            yield return Case("CharacterCombatFacts.WithActionUse", c => c.With(actionUse: UnitActionUse.Available));
            yield return Case("HeldEnvironmentEffects.WithHeldState", c => HeldEnvironmentEffects.WithHeldState(c,
                CharacterHeldEnvironmentState.Empty));
            yield return Case("ItemCharacterChanges.WithItems", c => ItemCharacterChanges.WithItems(c, CharacterItemState.Empty));
            yield return Case("PositionTileModifierCalculator.Replace", c => PositionTileModifierCalculator.Replace(c,
                new PositionTileModifier(c.Anchor, new long[] { 1 }, 0, 2)));
        }

        private static TestCaseData Case(string name, Func<CharacterCombatFacts, CharacterCombatFacts> copy) =>
            new TestCaseData(copy).SetName("CopyPath_PreservesExperience_" + name);

        [TestCaseSource(nameof(CopyPaths))]
        public void CopyPath_PreservesExperience(Func<CharacterCombatFacts, CharacterCombatFacts> copy)
        {
            var enemy = With(CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy),
                CharacterExperienceFacts.ForEnemy(3));
            var ally = With(CombatCommandTestData.Character(6, EntityKey.Unit_Akimbo, new CellCoord(3, 2), Owner.Player),
                CharacterExperienceFacts.ForAlly(7));

            var copiedEnemy = copy(enemy);
            var copiedAlly = copy(ally);

            Assert.That(copiedEnemy, Is.Not.SameAs(enemy));
            Assert.That(copiedAlly, Is.Not.SameAs(ally));
            Assert.That(copiedEnemy.Experience.SameValues(enemy.Experience), Is.True);
            Assert.That(copiedAlly.Experience.SameValues(ally.Experience), Is.True);
        }

        internal static CharacterCombatFacts With(CharacterCombatFacts c, CharacterExperienceFacts experience) =>
            new(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole, c.Area, c.Health, c.Stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus, c.PositionModifier, c.HeldEnvironment, c.Debuffs,
                c.Items, experience);
    }
}
