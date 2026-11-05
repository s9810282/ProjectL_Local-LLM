using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class FinishingShotTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.FinishingShot;
        internal bool Qualifies(int attack, HealthStateSnapshot health, int threshold) =>
            !health.IsDead && (long)health.CurrentHealth * 100 < (long)attack * threshold;
    }
}
