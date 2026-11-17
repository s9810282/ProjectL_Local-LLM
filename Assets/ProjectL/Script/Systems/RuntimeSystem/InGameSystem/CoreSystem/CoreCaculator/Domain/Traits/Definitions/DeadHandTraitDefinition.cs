using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class DeadHandTraitDefinition : TraitDefinition
    {
        public int DamagePercent { get; }

        public DeadHandTraitDefinition(int damagePercent) : base(TraitKey.DeadHand)
        {
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            DamagePercent = damagePercent;
        }
    }
}
