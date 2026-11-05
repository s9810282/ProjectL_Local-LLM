using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class AnkleBreakerTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.AnkleBreaker;
        internal AnkleBreakerState Apply(AnkleBreakerTraitDefinition definition, EffectOriginKey origin) =>
            new AnkleBreakerState(origin, definition.DurationAffectedSideTurnEnds);
        internal static bool IsActive(AnkleBreakerState state) => state != null;
        internal static AnkleBreakerState EndAffectedTurn(AnkleBreakerState state)
        {
            var next = AppliedStatusRules.EndRestriction(state?.ToStatus());
            return next == null ? null : CharacterStatusCompatibility.ProjectDebuffs(new StatusEffectSet(new AppliedStatus[] { next })).AnkleBreaker;
        }
    }
}
