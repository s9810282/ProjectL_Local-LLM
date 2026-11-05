using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    /// <summary>덱 UI의 요청을 수명·오류 경계 안에서 기존 배치 기능으로 전달합니다.</summary>
    internal sealed class CombatDeckPlacementInput : ICombatDeckPlacementInput
    {
        private readonly CombatInteraction _interaction;
        private readonly ICharacterBufferReadSink _characters;
        private readonly Func<bool, CombatInputFacts> _readFacts;
        private readonly Func<bool> _canReceive;
        private readonly RuntimeFaultBoundary _faults;
        private bool _released;

        internal CombatDeckPlacementInput(CombatInteraction interaction, ICharacterBufferReadSink characters,
            Func<bool, CombatInputFacts> readFacts, Func<bool> canReceive, RuntimeFaultBoundary faults)
        {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
            _canReceive = canReceive ?? throw new ArgumentNullException(nameof(canReceive));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public bool IsDragging => !_released && !_faults.IsFaulted
            && _interaction.Drag == CombatInteraction.DragKind.Placement;

        public bool CanSelect(CharacterInstanceId ownedId)
        {
            if (_released || _faults.IsFaulted || !_canReceive()) return false;
            try
            {
                return _interaction.CanSelectDeckUnit(_readFacts(false), _characters.Characters, ownedId);
            }
            catch (Exception error)
            {
                _faults.Report(error);
                return false;
            }
        }

        public void Select(CharacterInstanceId ownedId)
        {
            if (_released || _faults.IsFaulted || !_canReceive()) return;
            try
            {
                _interaction.SelectDeckUnit(_readFacts(false), _characters.Characters, ownedId);
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
                _interaction.CancelDeckSelection();
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        internal void Release() => _released = true;
    }
}
