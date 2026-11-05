using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class DisarmTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Disarm;
        internal DisarmState Apply(DisarmTraitDefinition definition, EffectOriginKey origin) =>
            new DisarmState(origin, definition.DurationAffectedSideTurnEnds);
        internal static bool IsActive(DisarmState state) => state != null;
        internal static DisarmState EndAffectedTurn(DisarmState state)
        {
            var next = AppliedStatusRules.EndRestriction(state?.ToStatus());
            return next == null ? null : CharacterStatusCompatibility.ProjectDebuffs(new StatusEffectSet(new AppliedStatus[] { next })).Disarm;
        }
    }
}
