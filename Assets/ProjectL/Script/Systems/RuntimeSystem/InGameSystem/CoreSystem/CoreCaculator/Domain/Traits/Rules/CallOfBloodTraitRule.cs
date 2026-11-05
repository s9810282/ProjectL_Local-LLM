using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class CallOfBloodTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.CallOfBlood;
        internal int Amount(CallOfBloodTraitDefinition definition, int basis) =>
            checked((int)(((long)basis * definition.EnemyMaxHealthHealPercent + 50) / 100));
    }
}
