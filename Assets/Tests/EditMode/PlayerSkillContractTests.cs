using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillContractTests
    {
        private static PlayerSkillDefinition Definition(int limit = 2) =>
            new(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, limit);

        [Test]
        public void RequestCopiesPointsWithoutChangingTheirOrder()
        {
            var points = new[] { new CellCoord(1, 2), new CellCoord(1, 3) };
            var request = new PlayerSkillRequest(points);
            points[0] = CellCoord.Zero;

            Assert.That(request.Points, Is.EqualTo(new[] { new CellCoord(1, 2), new CellCoord(1, 3) }));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)request.Points).Clear());
        }

        [Test]
        public void AttackResultCopiesPerRecipientDamageFacts()
        {
            var actor = new EntityId(10);
            var recipient = new EntityId(3);
            var breakdown = new Dictionary<EntityId, PlayerSkillDamageBreakdown>
            {
                [recipient] = new PlayerSkillDamageBreakdown(12, 1, 1, 12, 9, 1, 8)
            };
            var attack = new PlayerSkillAttackResult(actor, 0, 1, Array.Empty<CellCoord>(),
                new[] { new AttackHitResult(recipient, 15, 7) }, damageByRecipient: breakdown);
            breakdown.Clear();

            Assert.That(attack.DamageByRecipient[recipient].DamageAfterPhysicalDefense, Is.EqualTo(9));
            Assert.That(attack.DamageByRecipient[recipient].FinalDamage, Is.EqualTo(8));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DefinitionRejectsNonpositiveStageLimit(int limit)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Definition(limit));
        }

        [Test]
        public void BattleUsageRejectsIncompatibleUpgradeAndInvalidBalances()
        {
            var actor = new EntityId(10);
            var main = new CharacterInstanceId(1);
            var definition = Definition();
            Assert.Throws<ArgumentException>(() =>
                new PlayerSkillBattleState(main, actor, definition, PlayerSkillUpgradeKey.Distortion));
            var usage = new PlayerSkillBattleState(main, actor, definition, PlayerSkillUpgradeKey.Bloom);
            Assert.That(usage.RemainingUses, Is.EqualTo(2));
            Assert.That(usage.LastUsedControllerTurnSequence, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => usage.WithUsage(3, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => usage.WithUsage(0, -1));
        }

        [Test]
        public void BothStateComparisonsDetectUsageAndCopiesKeepBattleBinding()
        {
            var input = PlayerSkillTestData.Input();
            var state = input.State;
            var usage = state.PlayerSkill;
            var same = state.With(playerSkill: usage.WithUsage(usage.RemainingUses, 0));
            var changedUses = state.With(playerSkill: usage.WithUsage(0, 0));
            var changedTurn = state.With(playerSkill: usage.WithUsage(usage.RemainingUses, 1));

            Assert.That(state.SameValues(same), Is.True);
            Assert.That(state.SameValues(changedUses), Is.False);
            Assert.That(state.SameGameplayValues(changedTurn), Is.False);
            Assert.That(state.With(itemRandomState: 17).PlayerSkill, Is.SameAs(usage));
            Assert.That(CombatTurnCalculator.State(state, state.Turn, state.Phase).PlayerSkill, Is.SameAs(usage));
            Assert.That(new MovementProjection(input).CurrentInput().State.PlayerSkill, Is.SameAs(usage));
        }

        [Test]
        public void EveryInputCopyKeepsIntentAndCapturedAndExpectedSources()
        {
            var source = PlayerSkillTestData.Input();
            var copies = new[]
            {
                source.WithActor(new EntityId(77)),
                source.WithIntent(CombatCommandKind.Attack, default, default, null, null),
                source.WithIntent(CombatCommandKind.Move, default, default, null, null, null),
                source.WithWorld(source.State, source.Characters, source.Main, source.SoulCounts, source.Souls, source.EnvironmentBindings),
                source.WithPersistentFacts(source.Owned, source.ItemInventory, 19, 23)
            };
            foreach (var copy in copies)
            {
                Assert.That(copy.PlayerSkillRequest, Is.SameAs(source.PlayerSkillRequest));
                Assert.That(copy.PlayerSkillSource, Is.EqualTo(source.PlayerSkillSource));
                Assert.That(copy.PlayerSkillRequest.ExpectedSource, Is.EqualTo(source.PlayerSkillSource));
            }
        }

        [Test]
        public void BattlePreparationBindsOnlyTheSelectedMainWithAnUnspentBudget()
        {
            var input = PlayerSkillTestData.Input();
            var settings = new BattleTraitSettings(new Dictionary<EntityKey, TraitSettingsSet>(),
                new Dictionary<CharacterInstanceId, (EntityKey Key, TraitSettingsSet Settings)>());
            var combat = new CombatPreparation(8, 8, 4, 17, CombatFlowPhase.PlayerInput,
                Array.Empty<CharacterInstanceId>(), settings);
            var skill = input.State.PlayerSkill;

            var prepared = new BattlePreparation(input.Characters, combat, membership: input.Membership, playerSkill: skill);
            Assert.That(prepared.CommandState.PlayerSkill, Is.SameAs(skill));
            Assert.Throws<ArgumentException>(() => new BattlePreparation(input.Characters, combat,
                membership: input.Membership, playerSkill: skill.WithUsage(1, 1)));
            var sub = new PlayerSkillBattleState(new CharacterInstanceId(2), new EntityId(1), Definition(), PlayerSkillUpgradeKey.None);
            Assert.Throws<ArgumentException>(() => new BattlePreparation(input.Characters, combat,
                membership: input.Membership, playerSkill: sub));
        }

        [Test]
        public void OtherRosterChangesCannotPublishAnUpgradeChange()
        {
            var id = new CharacterInstanceId(1);
            var expected = new OwnedCharacterRecord(id, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main);
            var unrelatedUpgrade = new OwnedCharacterRecord(id, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main,
                specializationCategory: CategoryKey.Rider, playerSkillUpgrade: PlayerSkillUpgradeKey.Bloom);

            Assert.Throws<ArgumentException>(() => new MainSpecializationChange(expected, unrelatedUpgrade));
            var upgraded = expected.WithPlayerSkillUpgrade(PlayerSkillUpgradeKey.Bloom);
            Assert.Throws<ArgumentException>(() => new OwnedCharacterChange(expected, upgraded));
            var growth = upgraded.Growth.Append(new TraitGrowthSelection(1, TraitKey.CleanSweep));
            Assert.Throws<ArgumentException>(() => new TraitGrowthChange(expected, upgraded.WithGrowth(growth)));
        }

        [Test]
        public void OwnedMainUpgradeSurvivesGrowthExperienceSpecializationAndHunterCopies()
        {
            var main = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main);
            Assert.That(main.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.None));
            var upgraded = main.WithPlayerSkillUpgrade(PlayerSkillUpgradeKey.Bloom);
            Assert.That(upgraded.SameValues(main), Is.False);
            var growth = upgraded.WithGrowth(OwnedTraitGrowth.Empty);
            var xp = ExperienceRules.Gain(growth, 1, new LevelProgressionSpec(150, 100, 3, 1, 1)).Final;
            var specialized = MainSpecializationRules.Plan(xp, CategoryKey.Rider).Change.Final;
            var hunter = new HunterTraitRule().Award(specialized, 2);

            Assert.That(growth.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            Assert.That(xp.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            Assert.That(specialized.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            Assert.That(hunter.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            Assert.That(hunter.SpecializationCategory, Is.EqualTo(CategoryKey.Rider));
            Assert.That(hunter.HunterAttackBonus, Is.EqualTo(2));
        }
    }
}
