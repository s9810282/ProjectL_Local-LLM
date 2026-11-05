using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class ArmorBreakerTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.ArmorBreaker;
        internal CharacterDebuffState ApplyAfterHit(CharacterDebuffState current, int reductionPercent, EffectOriginKey origin)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var statuses = AppliedStatusRules.Apply(CharacterStatusCompatibility.Import(current, null), new DefenseDecreaseStatus(origin, reductionPercent));
            return current.WithArmorBreak(CharacterStatusCompatibility.ProjectDebuffs(statuses).ArmorBreak);
        }

        internal static int EvaluateDefense(int existingEffectiveDefense, ArmorBreakState stacks) =>
            AppliedStatusRules.ApplyDefenseDecrease(existingEffectiveDefense, CharacterStatusCompatibility.Import(new CharacterDebuffState(armorBreak: stacks), null));
    }
}
