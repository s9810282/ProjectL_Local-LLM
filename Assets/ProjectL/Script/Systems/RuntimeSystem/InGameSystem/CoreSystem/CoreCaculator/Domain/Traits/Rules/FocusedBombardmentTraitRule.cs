using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class FocusedBombardmentTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.FocusedBombardment;
        internal int Numerator(int? capturedEnemies, int required, int extra) => capturedEnemies == required ? checked(100 + extra) : 100;
    }
}
