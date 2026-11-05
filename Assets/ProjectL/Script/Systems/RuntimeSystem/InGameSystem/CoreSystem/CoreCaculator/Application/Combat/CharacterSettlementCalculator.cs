using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>Buffer 사실로 계산하며 지속 상태와 전투 투영을 한 변경 제안으로 묶는다.</summary>
    public static class CharacterSettlementCalculator
    {
        public static CharacterChanges SetHealth(CharacterCombatFacts character,
            MainCharacterPersistentState main, int currentHealth)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var health = new HealthStateSnapshot(currentHealth, character.Health.MaxHealth);
            MainCharacterPersistentChange persistent = null;
            if (character.OwnershipRole == CharacterOwnershipRole.Main)
            {
                ValidateMain(character, main);
                persistent = new MainCharacterPersistentChange(character.OwnedId.Value, main, main.WithCurrentHealth(currentHealth));
            }
            return new CharacterChanges(new[] { new CharacterStateChange(character,
                Copy(character, health, character.Stats)) }, Array.Empty<SoulCountChange>(), persistent);
        }

        public static CharacterChanges SetPersistentMain(CharacterCombatFacts character,
            MainCharacterPersistentState expected, MainCharacterPersistentState final)
        {
            ValidateMain(character, expected);
            if (final == null) throw new ArgumentNullException(nameof(final));
            int maxHealth = checked(final.PersistentMaxHealth + character.Stats.SoulMaxHealthBonus +
                character.TemporaryMaxHealthBonus + character.PositionModifier.MaxHealthBonus);
            UnitStatsSnapshot s = character.Stats;
            var stats = s.With(attackPower: final.PersistentAttack, physicalDefense: final.PersistentDefense,
                effectiveAttackPower: checked(final.PersistentAttack + s.ArtifactAttackBonus + s.PositionAttackBonus + s.TraitAttackBonus));
            return new CharacterChanges(new[] { new CharacterStateChange(character,
                Copy(character, new HealthStateSnapshot(final.CurrentHealth, maxHealth), stats)) },
                Array.Empty<SoulCountChange>(), new MainCharacterPersistentChange(character.OwnedId.Value, expected, final));
        }

        internal static CharacterCombatFacts Recover(CharacterCombatFacts source, int amount, out int actualRecovery)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            actualRecovery = source.Health.IsDead ? 0 : (int)Math.Min(amount,
                (long)source.Health.MaxHealth - source.Health.CurrentHealth);
            return actualRecovery == 0 ? source : Copy(source,
                new HealthStateSnapshot(source.Health.CurrentHealth + actualRecovery, source.Health.MaxHealth), source.Stats);
        }

        internal static void ValidateMain(CharacterCombatFacts character, MainCharacterPersistentState main)
        {
            // 부분 비교(의도 미확인): 체력만 본다. 전체 대응(MatchesMainState)으로 바꾸면 동작이 바뀐다. 설계 Q-B2.
            if (character == null || main == null || character.OwnershipRole != CharacterOwnershipRole.Main ||
                character.Health.CurrentHealth != main.CurrentHealth)
                throw new ArgumentException("메인 사실과 지속 상태가 일치해야 합니다.");
        }

        internal static CharacterCombatFacts Copy(CharacterCombatFacts c, HealthStateSnapshot health,
            UnitStatsSnapshot stats) => c.With(health: health, stats: stats);
    }
}
