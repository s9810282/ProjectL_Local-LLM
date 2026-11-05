using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class AccelerationTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int HeldAttackBonus { get; }
        public AccelerationTraitStatusSnapshot() : this(0) { }
        public AccelerationTraitStatusSnapshot(int value) : base(TraitKey.Acceleration)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            HeldAttackBonus = value;
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is AccelerationTraitStatusSnapshot status && HeldAttackBonus == status.HeldAttackBonus;
    }
}
