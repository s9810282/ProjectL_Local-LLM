using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>수신자의 정규 상태와 이미 확정된 기여분만 공개합니다. 대상·지형 문맥이 필요한 효과는 상시 상태로 추정하지 않습니다.</summary>
    public static class StatusEffectReadModel
    {
        public static IReadOnlyList<StatusEffectSnapshot> Capture(CharacterCombatFacts character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var current = character.StatusEffects;
            var result = new List<StatusEffectSnapshot>();
            var grants = Sources<AttackIncreaseStatus>(current);
            int held = AccelerationTraitRule.Held(character.Stats);
            if (grants.Count != 0 || held != 0) result.Add(new AttackIncreaseEffectSnapshot(AppliedStatusRules.AttackIncrease(current), held, grants));
            var attacks = Sources<AttackDecreaseStatus>(current);
            if (attacks.Count != 0) result.Add(new AttackDecreaseEffectSnapshot(((AttackDecreaseStatus)attacks[0].Status).ReductionPercent, attacks));
            if (character.TraitEffects.CohesionDefenseBonus != 0) result.Add(new DefenseIncreaseEffectSnapshot(character.TraitEffects.CohesionDefenseBonus));
            var defense = Sources<DefenseDecreaseStatus>(current);
            if (defense.Count != 0) result.Add(new DefenseDecreaseEffectSnapshot(CombatEffectiveStatsCalculator.PhysicalDefense(character), defense));
            var shields = Sources<ShieldStatus>(current);
            if (shields.Count != 0) result.Add(new ShieldEffectSnapshot(character.TraitEffects.ActiveShieldTotal, shields));
            foreach (var group in current.Entries.OfType<ActionRestrictionStatus>().GroupBy(s => s.Key).OrderBy(g => g.Key))
            {
                var restrictions = group.Select(s => new StatusEffectSourceSnapshot(s)).ToArray();
                result.Add(new ActionRestrictionEffectSnapshot(group.Key, restrictions));
            }
            var exposures = Sources<WeaknessExposureStatus>(current);
            if (exposures.Count != 0) result.Add(new WeaknessExposureEffectSnapshot(WeaknessExposureRules.EffectivePercent(current), exposures));
            var taunts = Sources<TauntedStatus>(current);
            if (taunts.Count != 0) result.Add(new TauntedEffectSnapshot(taunts));
            var fire = current.Entries.OfType<FireStatus>().SingleOrDefault();
            if (fire != null) result.Add(new FireEffectSnapshot(fire));
            var bleeding = current.Entries.OfType<BleedingStatus>().SingleOrDefault();
            if (bleeding != null) result.Add(new BleedingEffectSnapshot(bleeding));
            var slow = current.Entries.OfType<MovementRangeDecreaseStatus>().SingleOrDefault();
            if (slow != null) result.Add(new MovementRangeDecreaseEffectSnapshot(slow));
            var reconstruction = current.Entries.OfType<ReconstructionStatus>().SingleOrDefault();
            if (reconstruction != null) result.Add(new ReconstructionEffectSnapshot(reconstruction));
            if (RampageTraitRule.IsActive(character))
                result.Add(new RampageEffectSnapshot(RampageTraitRule.AttackBonus(character), RampageTraitRule.MovementBonus(character),
                    RampageTraitRule.Status(character).KillAttackBonus));
            return Array.AsReadOnly(result.OrderBy(s => s.Classification).ThenBy(s => s.Key).ToArray());
        }

        private static IReadOnlyList<StatusEffectSourceSnapshot> Sources<T>(StatusEffectSet current) where T : AppliedStatus =>
            Array.AsReadOnly(current.Entries.OfType<T>().Select(s => new StatusEffectSourceSnapshot(s)).ToArray());
    }
}
