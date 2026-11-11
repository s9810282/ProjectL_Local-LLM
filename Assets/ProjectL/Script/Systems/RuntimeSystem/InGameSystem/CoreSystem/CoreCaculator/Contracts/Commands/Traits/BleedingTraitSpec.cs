using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class BleedingTraitSpec : TraitSpec
    {
        public int DamagePerStack { get; }

        public BleedingTraitSpec(int damagePerStack) : base(TraitKey.Bleeding)
        {
            if (damagePerStack < 1) throw new ArgumentOutOfRangeException(nameof(damagePerStack));
            DamagePerStack = damagePerStack;
        }

        protected override bool SameValuesCore(TraitSpec other) => DamagePerStack == ((BleedingTraitSpec)other).DamagePerStack;
    }
}
