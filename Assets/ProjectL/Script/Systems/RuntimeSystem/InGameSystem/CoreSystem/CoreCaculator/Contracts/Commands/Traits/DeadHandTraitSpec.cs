using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class DeadHandTraitSpec : TraitSpec
    {
        public int DamagePercent { get; }

        public DeadHandTraitSpec(int damagePercent) : base(TraitKey.DeadHand)
        {
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            DamagePercent = damagePercent;
        }

        protected override bool SameValuesCore(TraitSpec other) =>
            DamagePercent == ((DeadHandTraitSpec)other).DamagePercent;
    }
}
