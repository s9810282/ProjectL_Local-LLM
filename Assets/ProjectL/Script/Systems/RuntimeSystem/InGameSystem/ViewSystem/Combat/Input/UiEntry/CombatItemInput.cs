using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    /// <summary>가방 UI·유닛 HUD의 요청을 수명·오류 경계 안에서 아이템 입력으로 전달합니다(가방 설계 P4·P6).</summary>
    internal sealed class CombatItemInput : ICombatItemInput
    {
        private readonly CombatInteraction _interaction;
        private readonly ICharacterBufferReadSink _characters;
        private readonly Func<bool, CombatInputFacts> _readFacts;
        private readonly Func<bool> _canReceive;
        private readonly RuntimeFaultBoundary _faults;
        private bool _released;

        internal CombatItemInput(CombatInteraction interaction, ICharacterBufferReadSink characters,
            Func<bool, CombatInputFacts> readFacts, Func<bool> canReceive, RuntimeFaultBoundary faults)
        {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _canReceive = canReceive ?? throw new ArgumentNullException(nameof(canReceive));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public bool CanSelect(int itemKey)
        {
            if (_released || _faults.IsFaulted || !_canReceive()) return false;
            try
            {
                return _interaction.CanSelectItem(_readFacts(false), itemKey);
            }
            catch (Exception error)
            {
                _faults.Report(error);
                return false;
            }
        }

        public void Select(int itemKey)
        {
            if (_released || _faults.IsFaulted || !_canReceive()) return;
            try
            {
                _interaction.SelectItem(_readFacts(false), itemKey);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        public void Cancel()
        {
            if (_released) return;
            try
            {
                _interaction.CancelItemSelection();
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        public void RequestUnequip(EntityId unitId)
        {
            if (_released || _faults.IsFaulted || !_canReceive()) return;
            try
            {
                _interaction.RequestItemUnequip(_readFacts(false), _characters.Characters, unitId);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        internal void Release() => _released = true;
    }
}
