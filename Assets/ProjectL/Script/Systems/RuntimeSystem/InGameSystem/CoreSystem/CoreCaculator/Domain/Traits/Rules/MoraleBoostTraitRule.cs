using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class MoraleBoostTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.MoraleBoost;
        internal int Amount(MoraleBoostTraitDefinition definition, int basis) =>
            checked((int)(((long)basis * definition.DefeatedEnemyBaseAttackPercent + 50) / 100));
    }
}
