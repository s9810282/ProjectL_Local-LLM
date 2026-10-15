using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum SoulArrivalKind { SettledVoluntary, ResolvedPath, Forced }

    public sealed class SoulCollectorInput
    {
        public CharacterCombatFacts Character { get; }
        public OwnedCharacterRecord Owned { get; }
        public int SoulCount { get; }
        public int BaseMaxHealth { get; }
        public MainCharacterPersistentState Main { get; }
        public SoulArrivalKind ArrivalKind { get; }

        public SoulCollectorInput(CharacterCombatFacts character, OwnedCharacterRecord owned,
            int soulCount, int baseMaxHealth, MainCharacterPersistentState main = null,
            SoulArrivalKind arrivalKind = SoulArrivalKind.SettledVoluntary)
        {
            Character = character ?? throw new ArgumentNullException(nameof(character));
            Owned = owned ?? throw new ArgumentNullException(nameof(owned));
            if (character.Owner != Owner.Player || character.OwnedId != owned.Id ||
                character.CharacterKey != owned.CharacterKey || character.OwnershipRole != owned.Role)
                throw new ArgumentException("수집자는 보유한 플레이어 카드에 연결되어야 합니다.");
            if (soulCount < 0) throw new ArgumentOutOfRangeException(nameof(soulCount));
            if (baseMaxHealth < 1) throw new ArgumentOutOfRangeException(nameof(baseMaxHealth));
            if (!Enum.IsDefined(typeof(SoulArrivalKind), arrivalKind))
                throw new ArgumentOutOfRangeException(nameof(arrivalKind));
            if ((owned.Role == CharacterOwnershipRole.Main) != (main != null))
                throw new ArgumentException("메인 수집자에는 지속 원본이 필요합니다.");
            if (main != null && (main.PersistentMaxHealth != baseMaxHealth ||
                main.CurrentHealth != character.Health.CurrentHealth ||
                main.PersistentAttack != character.Stats.AttackPower))
                throw new ArgumentException("메인 수집 입력이 지속 원본과 다릅니다.");
            SoulCount = soulCount;
            BaseMaxHealth = baseMaxHealth;
            Main = main;
            ArrivalKind = arrivalKind;
        }
    }
}
