using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class CleanSweepTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.CleanSweep;
        internal TraitAttackGrant Evaluate(CleanSweepTraitDefinition definition, EffectOriginKey origin, int actionOrdinal, bool actorIsOwner) =>
            !actorIsOwner
                ? new TraitAttackGrant(origin, definition.AttackBonusPerAllyAttack, definition.DurationControllerTurnEnds) : null;
    }
}
