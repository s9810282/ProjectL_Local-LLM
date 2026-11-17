using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class ThornBombTraitDefinition : TraitDefinition
    {
        public int DamagePercent { get; }

        public ThornBombTraitDefinition(int damagePercent) : base(TraitKey.ThornBomb)
        {
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            DamagePercent = damagePercent;
        }
    }
}
