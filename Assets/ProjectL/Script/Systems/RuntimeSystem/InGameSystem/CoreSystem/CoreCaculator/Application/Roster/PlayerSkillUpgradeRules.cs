using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class PlayerSkillUpgradeRules
    {
        public static PlayerSkillUpgradeFailureReason Availability(OwnedCharacterRecord main, PlayerSkillDefinition definition)
        {
            if (main == null || main.Role != CharacterOwnershipRole.Main)
                return PlayerSkillUpgradeFailureReason.InvalidMain;
            if (definition == null || definition.MainCharacterKey != main.CharacterKey)
                return PlayerSkillUpgradeFailureReason.NoSkillConfigured;
            return main.PlayerSkillUpgrade == PlayerSkillUpgradeKey.None
                ? PlayerSkillUpgradeFailureReason.None : PlayerSkillUpgradeFailureReason.AlreadyUpgraded;
        }

        public static PlayerSkillUpgradeFailureReason Plan(OwnedCharacterRecord main, PlayerSkillDefinition definition,
            PlayerSkillUpgradeKey branch, out OwnedCharacterRecord final)
        {
            final = null;
            var failure = Availability(main, definition);
            if (failure != PlayerSkillUpgradeFailureReason.None) return failure;
            if (!definition.SupportsUpgrade(branch)) return PlayerSkillUpgradeFailureReason.InvalidBranch;
            final = main.WithPlayerSkillUpgrade(branch);
            return PlayerSkillUpgradeFailureReason.None;
        }
    }
}
