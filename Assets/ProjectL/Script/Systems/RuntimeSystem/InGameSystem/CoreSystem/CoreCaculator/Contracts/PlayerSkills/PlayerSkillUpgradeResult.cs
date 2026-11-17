using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills
{
    public enum PlayerSkillUpgradeFailureReason
    {
        None = 0,
        NoActiveRun,
        InBattle,
        InvalidMain,
        NoSkillConfigured,
        InvalidBranch,
        AlreadyUpgraded,
        SourceChanged
    }

    public sealed class PlayerSkillUpgradeAvailability
    {
        public RunId RunId { get; }
        public long RosterRevision { get; }
        public CharacterInstanceId MainId { get; }
        public PlayerSkillDefinition Definition { get; }
        public PlayerSkillUpgradeKey CurrentBranch { get; }
        public bool CanApply => FailureReason == PlayerSkillUpgradeFailureReason.None;
        public PlayerSkillUpgradeFailureReason FailureReason { get; }

        public PlayerSkillUpgradeAvailability(RunId runId, long rosterRevision, CharacterInstanceId mainId,
            PlayerSkillDefinition definition, PlayerSkillUpgradeKey currentBranch, PlayerSkillUpgradeFailureReason failureReason)
        {
            if (rosterRevision < 0) throw new ArgumentOutOfRangeException(nameof(rosterRevision));
            RunId = runId;
            RosterRevision = rosterRevision;
            MainId = mainId;
            Definition = definition;
            CurrentBranch = currentBranch;
            FailureReason = failureReason;
        }
    }

    public sealed class PlayerSkillUpgradeApplyResult
    {
        public bool Succeeded => FailureReason == PlayerSkillUpgradeFailureReason.None;
        public PlayerSkillUpgradeFailureReason FailureReason { get; }
        public RunId RunId { get; }
        public CharacterInstanceId MainId { get; }
        public PlayerSkillKey SkillKey { get; }
        public PlayerSkillUpgradeKey Branch { get; }
        public long RosterRevision { get; }

        public PlayerSkillUpgradeApplyResult(PlayerSkillUpgradeFailureReason failureReason, RunId runId,
            CharacterInstanceId mainId, PlayerSkillKey skillKey, PlayerSkillUpgradeKey branch, long rosterRevision)
        {
            if (rosterRevision < 0) throw new ArgumentOutOfRangeException(nameof(rosterRevision));
            FailureReason = failureReason;
            RunId = runId;
            MainId = mainId;
            SkillKey = skillKey;
            Branch = branch;
            RosterRevision = rosterRevision;
        }
    }
}
