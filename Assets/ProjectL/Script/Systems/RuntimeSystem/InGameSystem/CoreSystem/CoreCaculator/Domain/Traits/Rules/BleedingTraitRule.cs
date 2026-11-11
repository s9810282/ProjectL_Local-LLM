using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class BleedingTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Bleeding;
        internal StatusEffectSet Apply(BleedingTraitDefinition definition, CharacterCombatFacts attacker,
            CharacterCombatFacts recipient, CombatDamageKind kind, EffectOriginKey origin)
        {
            if (recipient.Health.IsDead || recipient.Area != CharacterRuntimeArea.Field ||
                !new HostilityPolicy().AreHostile(attacker.Owner, recipient.Owner) ||
                kind != CombatDamageKind.OrdinaryAttack && kind != CombatDamageKind.ExtraAttack && kind != CombatDamageKind.ReactionAttack)
                return recipient.StatusEffects;
            return PeriodicStatusRules.ApplyBleeding(recipient.StatusEffects, origin, definition.DamagePerStack);
        }
    }
}
