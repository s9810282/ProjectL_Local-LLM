using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillUpgradeTests
    {
        private static readonly PlayerSkillDefinition Definition = new(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, 1);

        [TestCase(PlayerSkillUpgradeKey.Bloom)]
        [TestCase(PlayerSkillUpgradeKey.Condemnation)]
        public void CompatibleBranchOnlyChangesTheOwningRecord(PlayerSkillUpgradeKey branch)
        {
            var main = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 4, 5);
            Assert.That(PlayerSkillUpgradeRules.Plan(main, Definition, branch, out var final), Is.EqualTo(PlayerSkillUpgradeFailureReason.None));
            Assert.That(final.SameValues(main.WithPlayerSkillUpgrade(branch)), Is.True);
            Assert.That(main.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.None));
            Assert.That(PlayerSkillUpgradeRules.Plan(final, Definition, branch, out _), Is.EqualTo(PlayerSkillUpgradeFailureReason.AlreadyUpgraded));
            var alternative = branch == PlayerSkillUpgradeKey.Bloom ? PlayerSkillUpgradeKey.Condemnation : PlayerSkillUpgradeKey.Bloom;
            Assert.That(PlayerSkillUpgradeRules.Plan(final, Definition, alternative, out _), Is.EqualTo(PlayerSkillUpgradeFailureReason.AlreadyUpgraded));
        }

        [Test]
        public void MainSpecializationAndGrowthCopiesKeepTheAcquiredBranch()
        {
            var main = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main)
                .WithPlayerSkillUpgrade(PlayerSkillUpgradeKey.Bloom);
            var specialized = MainSpecializationRules.Plan(main, CategoryKey.Cannon).Change.Final;
            var grown = specialized.WithGrowth(specialized.Growth.WithPurchase(TraitKey.SoloAction));
            Assert.That(grown.PlayerSkillUpgrade, Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            Assert.That(grown.CharacterKey, Is.EqualTo(EntityKey.Unit_Akimbo));
        }

        [Test]
        public void SubMissingDefinitionWrongOwnerAndWrongBranchDoNotProposeAChange()
        {
            var main = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main);
            var sub = new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub);
            var other = new OwnedCharacterRecord(new CharacterInstanceId(3), EntityKey.Unit_Sniper, CharacterOwnershipRole.Main);
            Assert.That(PlayerSkillUpgradeRules.Plan(sub, Definition, PlayerSkillUpgradeKey.Bloom, out _), Is.EqualTo(PlayerSkillUpgradeFailureReason.InvalidMain));
            Assert.That(PlayerSkillUpgradeRules.Plan(main, null, PlayerSkillUpgradeKey.Bloom, out _), Is.EqualTo(PlayerSkillUpgradeFailureReason.NoSkillConfigured));
            Assert.That(PlayerSkillUpgradeRules.Plan(other, Definition, PlayerSkillUpgradeKey.Bloom, out _), Is.EqualTo(PlayerSkillUpgradeFailureReason.NoSkillConfigured));
            Assert.That(PlayerSkillUpgradeRules.Plan(main, Definition, PlayerSkillUpgradeKey.Distortion, out var final), Is.EqualTo(PlayerSkillUpgradeFailureReason.InvalidBranch));
            Assert.That(final, Is.Null);
        }
    }
}
