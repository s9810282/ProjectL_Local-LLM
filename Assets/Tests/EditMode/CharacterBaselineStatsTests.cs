using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterBaselineStatsTests
    {
        [TestCase(1)]
        [TestCase(3)]
        public void ConfiguredMovement_LeveledBaselineAndBattleCopiesKeepCapturedValues(int level)
        {
            var categories = UnitSpecTestFactory.CreateGrowthCategoryCatalog().Select(c => c.Key == CategoryKey.Rider
                ? new CategorySpec(c.Key,c.RuleSpecs.ToArray(),5,c.LevelGrowth) : c).ToArray();
            var spec = new UnitSpec(EntityKey.Unit_Akimbo, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 20,
                MoveMethod.LiftAndPlace, 4, CategoryKey.Rider, 10, 1, 7, Array.Empty<TraitSpec>(), UnitOwnerEligibility.Both);
            var definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(), new CategoryRegistry(categories))
                .GetRequired(spec.Key);
            var baseline = CharacterBaselineStatsCalculator.ForOwned(definition, Sub(level), null, 0);
            var owned = BattleCharacterCalculator.CreateOwned(new EntityId(1), definition, Sub(level), null, 0,
                null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, TraitRegistry.CreateDefault());
            var enemy = BattleCharacterCalculator.CreateEnemy(new EntityId(2), definition, level,
                CellCoord.Right, Rotation4.R0, new[] { CellCoord.Right }, TraitRegistry.CreateDefault());
            Assert.That(baseline.MoveRange, Is.EqualTo(5));
            Assert.That(baseline.BaseAttackPower, Is.EqualTo(10 + level - 1));
            foreach (var facts in new[] { owned, enemy, owned.With(stats: owned.Stats.With(physicalDefense: 9)),
                enemy.WithPlacement(CharacterRuntimeArea.Field, null, new CellCoord(2, 0), Rotation4.R90, new[] { new CellCoord(2, 0) }) })
                Assert.That(facts.Stats.MoveRange, Is.EqualTo(5));
        }

        [Test]
        public void Sub_AddsSoulsToDefinitionAttackAndMaxHealth()
        {
            var baseline = CharacterBaselineStatsCalculator.ForOwned(SubDefinition(), Sub(), null, 3);

            Assert.That((baseline.BaseAttackPower, baseline.SoulAttackBonus, baseline.EffectiveAttackPower),
                Is.EqualTo((10, 3, 13)));
            Assert.That((baseline.BaseMaxHealth, baseline.SoulMaxHealthBonus, baseline.MaxHealth,
                baseline.InitialCurrentHealth), Is.EqualTo((20, 3, 23, 23)));
            Assert.That((baseline.MoveMethod, baseline.MoveRange),
                Is.EqualTo((MoveMethod.LiftAndPlace, 3)));
            Assert.That((baseline.AttackRange, baseline.PhysicalDefense,
                baseline.Priority), Is.EqualTo((4, 1, 7)));
        }

        [Test]
        public void Main_UsesPersistentStateInsteadOfDefinition()
        {
            var state = new MainCharacterPersistentState(18, 30, 15);

            var baseline = CharacterBaselineStatsCalculator.ForOwned(MainDefinition(), MainRecord(), state, 2);

            Assert.That((baseline.BaseAttackPower, baseline.SoulAttackBonus, baseline.EffectiveAttackPower),
                Is.EqualTo((15, 2, 17)));
            Assert.That((baseline.BaseMaxHealth, baseline.SoulMaxHealthBonus, baseline.MaxHealth,
                baseline.InitialCurrentHealth), Is.EqualTo((30, 2, 32, 18)));
            Assert.That(baseline.MoveRange, Is.EqualTo(3));
            var health = CharacterHealthRules.PersistentMainHealth(state, 2);
            Assert.That((baseline.InitialCurrentHealth, baseline.MaxHealth),
                Is.EqualTo((health.CurrentHealth, health.MaxHealth)));
        }

        [Test]
        public void Main_PhysicalDefenseComesFromPersistentState()
        {
            // 메인 방어력은 랜덤 성장 결과(지속 상태)가 원본이다(경험치 설계 §3.4).
            var state = new MainCharacterPersistentState(18, 30, 15, 4);

            var baseline = CharacterBaselineStatsCalculator.ForOwned(MainDefinition(), MainRecord(), state, 0);

            Assert.That(baseline.PhysicalDefense, Is.EqualTo(4));
        }

        [Test]
        public void Main_WithZeroHealth_IsAllowed()
        {
            var state = new MainCharacterPersistentState(0, 30, 15);

            var baseline = CharacterBaselineStatsCalculator.ForOwned(MainDefinition(), MainRecord(), state, 1);

            Assert.That((baseline.InitialCurrentHealth, baseline.MaxHealth), Is.EqualTo((0, 31)));
        }

        [Test]
        public void Unowned_UsesDefinitionWithoutSoulBonus()
        {
            var baseline = CharacterBaselineStatsCalculator.ForUnowned(SubDefinition(), 1);

            Assert.That((baseline.BaseAttackPower, baseline.SoulAttackBonus, baseline.EffectiveAttackPower),
                Is.EqualTo((10, 0, 10)));
            Assert.That((baseline.SoulMaxHealthBonus, baseline.MaxHealth, baseline.InitialCurrentHealth),
                Is.EqualTo((0, 20, 20)));
        }

        [Test]
        public void SubLevel_GrowsDefinitionValuesBeforeSouls()
        {
            var rider = SubDefinition(CategoryKey.Rider);

            var first = CharacterBaselineStatsCalculator.ForOwned(rider, Sub(level: 1), null, 2);
            var third = CharacterBaselineStatsCalculator.ForOwned(rider, Sub(level: 3), null, 2);

            Assert.That((first.BaseAttackPower, first.SoulAttackBonus, first.EffectiveAttackPower), Is.EqualTo((10, 2, 12)));
            Assert.That((third.BaseAttackPower, third.SoulAttackBonus, third.EffectiveAttackPower), Is.EqualTo((12, 2, 14)));
            Assert.That((third.BaseMaxHealth, third.SoulMaxHealthBonus, third.MaxHealth, third.InitialCurrentHealth),
                Is.EqualTo((22, 2, 24, 24)));
        }

        [Test]
        public void MainLevel_KeepsPersistentGrowthWithoutCategoryGrowth()
        {
            // 메인 Category(Base)에 상승치가 있어도 메인 성장 결과는 지속 상태가 원본이다.
            var definition = MainDefinition(new CategoryLevelGrowth(5, 5));
            var state = new MainCharacterPersistentState(18, 30, 15);

            var first = CharacterBaselineStatsCalculator.ForOwned(definition, MainRecord(level: 1), state, 2);
            var third = CharacterBaselineStatsCalculator.ForOwned(definition, MainRecord(level: 3), state, 2);

            Assert.That((third.BaseAttackPower, third.EffectiveAttackPower, third.BaseMaxHealth, third.MaxHealth,
                third.InitialCurrentHealth), Is.EqualTo((15, 17, 30, 32, 18)));
            Assert.That((third.EffectiveAttackPower, third.MaxHealth), Is.EqualTo((first.EffectiveAttackPower, first.MaxHealth)));
        }

        [Test]
        public void Unowned_GrowsWithGivenLevel()
        {
            var baseline = CharacterBaselineStatsCalculator.ForUnowned(SubDefinition(CategoryKey.Cannon), 3);

            Assert.That((baseline.BaseAttackPower, baseline.SoulAttackBonus, baseline.EffectiveAttackPower),
                Is.EqualTo((14, 0, 14)));
            Assert.That((baseline.BaseMaxHealth, baseline.MaxHealth, baseline.InitialCurrentHealth), Is.EqualTo((20, 20, 20)));
        }

        [Test]
        public void InvalidInputs_AreRejected()
        {
            var state = new MainCharacterPersistentState(10, 10, 5);

            Assert.Throws<ArgumentNullException>(() =>
                CharacterBaselineStatsCalculator.ForOwned(null, Sub(), null, 0));
            Assert.Throws<ArgumentNullException>(() =>
                CharacterBaselineStatsCalculator.ForOwned(SubDefinition(), null, null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CharacterBaselineStatsCalculator.ForOwned(SubDefinition(), Sub(), null, -1));
            Assert.Throws<InvalidOperationException>(() =>
                CharacterBaselineStatsCalculator.ForOwned(MainDefinition(), MainRecord(), null, 0));
            Assert.Throws<InvalidOperationException>(() =>
                CharacterBaselineStatsCalculator.ForOwned(SubDefinition(), Sub(), state, 0));
            Assert.Throws<OverflowException>(() => CharacterBaselineStatsCalculator.ForOwned(MainDefinition(),
                MainRecord(), new MainCharacterPersistentState(10, 10, int.MaxValue), 1));
            Assert.Throws<ArgumentNullException>(() => CharacterBaselineStatsCalculator.ForUnowned(null, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterBaselineStatsCalculator.ForUnowned(SubDefinition(), 0));
        }

        [Test]
        public void Create_SubMapsBaselineToCombatStats()
        {
            var character = BattleCharacterCalculator.CreateOwned(new EntityId(1), SubDefinition(),
                Sub(), null, 3, 0, null, null, Array.Empty<CellCoord>(), TraitRegistry.CreateDefault());
            var stats = character.Stats;

            Assert.That((stats.AttackPower, stats.SoulAttackBonus, stats.ArtifactAttackBonus,
                stats.EffectiveAttackPower), Is.EqualTo((10, 3, 3, 13)));
            Assert.That((stats.SoulMaxHealthBonus, stats.PositionAttackBonus), Is.EqualTo((3, 0)));
            Assert.That((character.Health.CurrentHealth, character.Health.MaxHealth), Is.EqualTo((23, 23)));
            Assert.That(character.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That((character.Owner, character.OwnedId, character.OwnershipRole),
                Is.EqualTo((Owner.Player, (CharacterInstanceId?)Sub().Id, (CharacterOwnershipRole?)CharacterOwnershipRole.Sub)));
        }

        [Test]
        public void Create_LeveledSubMapsGrownBaseToCombatStats()
        {
            var character = BattleCharacterCalculator.CreateOwned(new EntityId(1), SubDefinition(CategoryKey.Rider),
                Sub(level: 3), null, 1, 0, null, null, Array.Empty<CellCoord>(), TraitRegistry.CreateDefault());

            Assert.That((character.Stats.AttackPower, character.Stats.SoulAttackBonus, character.Stats.EffectiveAttackPower),
                Is.EqualTo((12, 1, 13)));
            Assert.That((character.Health.CurrentHealth, character.Health.MaxHealth), Is.EqualTo((23, 23)));
        }

        [Test]
        public void Create_MainMapsPersistentBaselineToCombatStats()
        {
            var state = new MainCharacterPersistentState(18, 30, 15);

            var character = BattleCharacterCalculator.CreateOwned(new EntityId(1), MainDefinition(),
                MainRecord(), state, 2, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero },
                TraitRegistry.CreateDefault());

            Assert.That((character.Stats.AttackPower, character.Stats.EffectiveAttackPower), Is.EqualTo((15, 17)));
            Assert.That((character.Health.CurrentHealth, character.Health.MaxHealth), Is.EqualTo((18, 32)));
            Assert.That(character.Stats.MoveRange, Is.EqualTo(3));
        }

        [Test]
        public void CreateEnemy_MapsLeveledBaselineWithoutOwnershipOrSouls()
        {
            var character = BattleCharacterCalculator.CreateEnemy(new EntityId(1), SubDefinition(CategoryKey.Rider), 3,
                CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, TraitRegistry.CreateDefault());

            Assert.That((character.Owner, character.OwnedId, character.OwnershipRole),
                Is.EqualTo((Owner.Enemy, (CharacterInstanceId?)null, (CharacterOwnershipRole?)null)));
            Assert.That((character.Stats.AttackPower, character.Stats.SoulAttackBonus, character.Stats.ArtifactAttackBonus,
                character.Stats.EffectiveAttackPower), Is.EqualTo((12, 0, 0, 12)));
            Assert.That((character.Health.CurrentHealth, character.Health.MaxHealth), Is.EqualTo((22, 22)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BattleCharacterCalculator.CreateEnemy(new EntityId(1),
                SubDefinition(), 0, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, TraitRegistry.CreateDefault()));
        }

        private static OwnedCharacterRecord MainRecord(int level = 1) =>
            new(new CharacterInstanceId(1), EntityKey.Unit_Gravekeeper, CharacterOwnershipRole.Main, level);

        private static OwnedCharacterRecord Sub(int level = 1) =>
            new(new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub, level);

        // 메인 정의는 1x1·공격 범위 1이어야 한다(UnitDefinition 규칙).
        private static UnitDefinition MainDefinition(CategoryLevelGrowth baseGrowth = default) =>
            Definition(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, maxHealth: 10, attack: 5, attackRange: 1,
                CategoryKey.Main, baseGrowth);

        private static UnitDefinition SubDefinition(CategoryKey category = CategoryKey.Main) =>
            Definition(EntityKey.Unit_Akimbo, UnitRole.Regular, maxHealth: 20, attack: 10, attackRange: 4, category);

        /// <summary>Category 상승치는 레벨 성장 설계 §3.1 값(Base는 인자)이다. Heavy는 보호 범위가 필요해 쓰지 않는다.</summary>
        private static UnitDefinition Definition(EntityKey key, UnitRole role, int maxHealth, int attack,
            int attackRange, CategoryKey category = CategoryKey.Main, CategoryLevelGrowth baseGrowth = default)
        {
            var spec = new UnitSpec(key, role, 1, 1, new[] { CellCoord.Zero }, maxHealth, MoveMethod.LiftAndPlace, attackRange, category, attack, 1, 7, Array.Empty<TraitSpec>(),
                UnitOwnerEligibility.PlayerOnly);
            return new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(),
                new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog(baseGrowth))).GetRequired(key);
        }
    }
}
