using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>기존 특성 호출을 공통 방어막 정책에 연결하는 무상태 호환 어댑터입니다.</summary>
    internal static class ShieldGrantRules
    {
        internal static TraitShieldGrant Reconcile(TraitShieldGrant prior, EffectOriginKey origin, int capacity,
            bool eligible, long completedControllerTurns, int resetInterval, bool resetDue)
        {
            var status = ShieldStatusRules.Reconcile(prior?.ToStatus(), origin, capacity, eligible, completedControllerTurns, resetInterval, resetDue);
            return status == null ? null : CharacterStatusCompatibility.ProjectTraitEffects(
                new StatusEffectSet(new AppliedStatus[] { status }), 0).Shields[0];
        }

        internal static CharacterTraitEffects Absorb(CharacterTraitEffects effects, int incoming, out int remaining)
        {
            if (effects == null) throw new ArgumentNullException(nameof(effects));
            var statuses = CharacterStatusCompatibility.Import(null, effects);
            var after = ShieldStatusRules.Absorb(statuses, incoming, out remaining);
            return ReferenceEquals(statuses, after) ? effects : CharacterStatusCompatibility.ProjectTraitEffects(after, effects.CohesionDefenseBonus);
        }
    }
}
