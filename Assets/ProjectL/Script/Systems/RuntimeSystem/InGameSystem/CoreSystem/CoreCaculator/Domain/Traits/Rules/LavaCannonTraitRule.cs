using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class LavaCannonTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.LavaCannon;
        internal StatusEffectSet Apply(Owner attackerSide, CharacterCombatFacts recipient, EffectOriginKey origin, CombatTurnFacts turn)
        {
            if (recipient.Health.IsDead || recipient.Area != CharacterRuntimeArea.Field ||
                !new HostilityPolicy().AreHostile(attackerSide, recipient.Owner)) return recipient.StatusEffects;
            return PeriodicStatusRules.ApplySlow(PeriodicStatusRules.ApplyFire(recipient.StatusEffects, origin),
                origin, recipient.Owner, turn);
        }
    }
}
