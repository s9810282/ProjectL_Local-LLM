using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    internal interface ICombatDeckHoverDisplay
    {
        void SetDeckHoveredUnit(EntityId id);
    }

    /// <summary>보유 카드 ID를 현재 필드 EntityId로 매핑해 HUD Arrow만 표시합니다.</summary>
    internal sealed class CombatDeckHoverInput : ICombatDeckHoverInput
    {
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICombatDeckHoverDisplay _display;
        private readonly RuntimeFaultBoundary _faults;
        private bool _released;

        internal CombatDeckHoverInput(ICharacterBufferReadSink characters, ICombatDeckHoverDisplay display, RuntimeFaultBoundary faults)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public void Hover(CharacterInstanceId ownedId)
        {
            if (_released || _faults.IsFaulted) return;
            try
            {
                EntityId target = EntityId.Empty;
                if (!ownedId.IsEmpty)
                {
                    var characters = _characters.Characters;
                    for (int i = 0; i < characters.Count; i++)
                    {
                        CharacterData unit = characters[i];
                        if (unit.OwnedId == ownedId && unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead)
                        {
                            target = unit.EntityId;
                            break;
                        }
                    }
                }
                _display.SetDeckHoveredUnit(target);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        internal void Release()
        {
            if (_released) return;
            _released = true;
            _display.SetDeckHoveredUnit(EntityId.Empty);
        }
    }
}
