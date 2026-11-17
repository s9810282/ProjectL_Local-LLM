using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    internal sealed class PlayerSkillUpgradeTransactions
    {
        private readonly IBufferQuery _buffers;
        private readonly ICharacterBufferStore _characters;

        internal PlayerSkillUpgradeTransactions(IBufferQuery buffers, ICharacterBufferStore characters)
        {
            _buffers = buffers;
            _characters = characters;
        }

        internal PlayerSkillUpgradeFailureReason TryApply(RosterSourceVersion source, OwnedCharacterRecord expected,
            OwnedCharacterRecord final)
        {
            var failure = Validate(source, expected);
            if (failure != PlayerSkillUpgradeFailureReason.None) return failure;
            var prepared = _characters.PreparePlayerSkillUpgrade(expected, final);
            failure = Validate(source, expected);
            if (failure != PlayerSkillUpgradeFailureReason.None) return failure;
            _characters.InstallPlayerSkillUpgrade(prepared);
            return PlayerSkillUpgradeFailureReason.None;
        }

        private PlayerSkillUpgradeFailureReason Validate(RosterSourceVersion source, OwnedCharacterRecord expected)
        {
            if (!source.IsCurrent(_buffers)) return PlayerSkillUpgradeFailureReason.SourceChanged;
            if (_buffers.CombatStateReadSink.HasBattle || _buffers.CharacterReadSink.Characters.Count != 0)
                return PlayerSkillUpgradeFailureReason.InBattle;
            if (source.RunId.IsEmpty || _buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress)
                return PlayerSkillUpgradeFailureReason.NoActiveRun;
            return source.Deck?.MainId == expected.Id && source.Deck.RunId == source.RunId
                ? PlayerSkillUpgradeFailureReason.None : PlayerSkillUpgradeFailureReason.InvalidMain;
        }
    }
}
