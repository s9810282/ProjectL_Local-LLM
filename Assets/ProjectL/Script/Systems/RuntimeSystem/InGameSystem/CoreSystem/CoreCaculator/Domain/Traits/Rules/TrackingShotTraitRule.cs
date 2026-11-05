using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class TrackingShotTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.TrackingShot;
        internal int Numerator(HealthStateSnapshot? health, int threshold, int extra) => health.HasValue &&
            !health.Value.IsDead && (long)health.Value.CurrentHealth * 100 >= (long)health.Value.MaxHealth * threshold ? checked(100 + extra) : 100;
    }
}
