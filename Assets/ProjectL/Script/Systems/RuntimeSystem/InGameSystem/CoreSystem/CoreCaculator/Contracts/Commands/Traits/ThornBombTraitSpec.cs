using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class ThornBombTraitSpec : TraitSpec
    {
        public int DamagePercent { get; }

        public ThornBombTraitSpec(int damagePercent) : base(TraitKey.ThornBomb)
        {
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            DamagePercent = damagePercent;
        }

        protected override bool SameValuesCore(TraitSpec other) =>
            DamagePercent == ((ThornBombTraitSpec)other).DamagePercent;
    }
}
