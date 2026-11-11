using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    internal static class RampageApplication
    {
        internal static CharacterCombatFacts Reconcile(CharacterCombatFacts character, int hunterAttackBonus)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (hunterAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(hunterAttackBonus));
            if (RampageTraitRule.Status(character) == null) return character;
            var final = ClearInactiveHistory(character);
            int bonus = CombatEffectiveStatsCalculator.TraitAttackBonus(final, hunterAttackBonus);
            return bonus == final.Stats.TraitAttackBonus ? final : final.With(stats: final.Stats.With(traitAttackBonus: bonus));
        }

        /// <summary>체력·배치 변경이 보존한 이전 폭주 몫만 교체합니다. 다른 특성·영구 성장의 투영값은 유지합니다.</summary>
        internal static CharacterCombatFacts ReconcileChange(CharacterCombatFacts before, CharacterCombatFacts after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            if (RampageTraitRule.Status(before) == null && RampageTraitRule.Status(after) == null) return after;
            var final = ClearInactiveHistory(after);
            int bonus = checked(final.Stats.TraitAttackBonus - RampageTraitRule.AttackBonus(before) + RampageTraitRule.AttackBonus(final));
            return bonus == final.Stats.TraitAttackBonus ? final : final.With(stats: final.Stats.With(traitAttackBonus: bonus));
        }

        internal static CharacterCombatFacts AddKill(CharacterCombatFacts character, int baseAttack, int hunterAttackBonus)
        {
            if (baseAttack < 0) throw new ArgumentOutOfRangeException(nameof(baseAttack));
            if (!RampageTraitRule.IsActive(character) || baseAttack == 0) return character;
            var status = RampageTraitRule.Status(character);
            return Reconcile(WithHistory(character, checked(status.KillAttackBonus + baseAttack)), hunterAttackBonus);
        }

        /// <summary>전투 종료에는 처치 이력만 비웁니다. 조건 자체는 남은 체력·배치에서 계속 유도하며 보유 기록에는 저장하지 않습니다.</summary>
        internal static CharacterCombatFacts ResetHistory(CharacterCombatFacts character)
        {
            var status = RampageTraitRule.Status(character);
            return status == null || status.KillAttackBonus == 0 ? character : ReconcileChange(character, WithHistory(character, 0));
        }

        private static CharacterCombatFacts ClearInactiveHistory(CharacterCombatFacts character)
        {
            var status = RampageTraitRule.Status(character);
            return status == null || status.KillAttackBonus == 0 || RampageTraitRule.IsActive(character)
                ? character : WithHistory(character, 0);
        }

        private static CharacterCombatFacts WithHistory(CharacterCombatFacts character, int killAttackBonus) =>
            character.With(stats: character.Stats.With(traitStatuses: character.Stats.TraitStatuses.ToArray().Select(t =>
                t is RampageTraitStatusSnapshot status ? new RampageTraitStatusSnapshot(status.BaseAttackBonus,
                    status.RankAttackBonus, status.MovementBonus, killAttackBonus).WithAvailability(status.UnavailableReason,
                        status.PartialUnavailableReason) : t).ToArray()));
    }
}
