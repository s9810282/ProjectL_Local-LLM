using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal sealed partial class CharacterRosterService
    {
        public PlayerSkillUpgradeAvailability ReadPlayerSkillUpgradeAvailability()
        {
            RequireReady();
            var source = RosterSourceVersion.Capture(_buffers);
            return ReadPlayerSkillUpgrade(source, out _);
        }

        public PlayerSkillUpgradeApplyResult TryApplyPlayerSkillUpgrade(RunId expectedRunId, long expectedRosterRevision,
            CharacterInstanceId mainId, PlayerSkillUpgradeKey selectedBranch)
        {
            RequireReady();
            var source = RosterSourceVersion.Capture(_buffers);
            var availability = ReadPlayerSkillUpgrade(source, out var main);
            var failure = availability.FailureReason;
            if (!source.RunId.IsEmpty && (expectedRunId != source.RunId || expectedRosterRevision != source.RosterRevision))
                failure = PlayerSkillUpgradeFailureReason.SourceChanged;
            if (availability.MainId != mainId &&
                failure is not (PlayerSkillUpgradeFailureReason.SourceChanged or PlayerSkillUpgradeFailureReason.NoActiveRun or PlayerSkillUpgradeFailureReason.InBattle))
                failure = PlayerSkillUpgradeFailureReason.InvalidMain;

            if (failure == PlayerSkillUpgradeFailureReason.None)
            {
                failure = PlayerSkillUpgradeRules.Plan(main, availability.Definition, selectedBranch, out var final);
                if (failure == PlayerSkillUpgradeFailureReason.None)
                    failure = _buffers.PlayerSkillUpgrades.TryApply(source, main, final);
            }

            return new PlayerSkillUpgradeApplyResult(failure, _buffers.RunProgressReadSink.RunId, availability.MainId,
                availability.Definition?.Key ?? PlayerSkillKey.None,
                failure == PlayerSkillUpgradeFailureReason.None ? selectedBranch : availability.CurrentBranch,
                _buffers.CharacterReadSink.RosterRevision);
        }

        private PlayerSkillUpgradeAvailability ReadPlayerSkillUpgrade(RosterSourceVersion source, out OwnedCharacterRecord main)
        {
            var id = source.Deck != null && source.Deck.RunId == source.RunId ? source.Deck.MainId : CharacterInstanceId.Empty;
            _buffers.CharacterReadSink.TryGetOwned(id, out main);
            var definition = main?.Role == CharacterOwnershipRole.Main
                ? PlayerActiveSkillTable.FindForMain(_query, main.CharacterKey) : null;
            var failure = InBattle ? PlayerSkillUpgradeFailureReason.InBattle
                : source.RunId.IsEmpty || RunEnded ? PlayerSkillUpgradeFailureReason.NoActiveRun
                : PlayerSkillUpgradeRules.Availability(main, definition);
            if (!source.IsCurrent(_buffers))
                failure = PlayerSkillUpgradeFailureReason.SourceChanged;
            return new PlayerSkillUpgradeAvailability(source.RunId, source.RosterRevision, id, definition,
                main?.PlayerSkillUpgrade ?? PlayerSkillUpgradeKey.None, failure);
        }
    }
}
