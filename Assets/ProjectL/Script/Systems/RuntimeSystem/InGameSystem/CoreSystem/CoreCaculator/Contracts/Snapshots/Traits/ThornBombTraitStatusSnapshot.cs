using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class ThornBombTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int DamagePercent { get; }

        public ThornBombTraitStatusSnapshot(int damagePercent) : base(TraitKey.ThornBomb)
        {
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            DamagePercent = damagePercent;
        }

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is ThornBombTraitStatusSnapshot bomb && DamagePercent == bomb.DamagePercent;
    }
}
