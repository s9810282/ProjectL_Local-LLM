using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class VanguardTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Vanguard;
        internal TraitAttackGrant Evaluate(VanguardTraitDefinition definition, EffectOriginKey origin, int actionOrdinal, bool actorIsOwner) =>
            actorIsOwner && actionOrdinal == 1
                ? new TraitAttackGrant(origin, definition.AttackBonus, definition.DurationControllerTurnEnds) : null;
    }
}
