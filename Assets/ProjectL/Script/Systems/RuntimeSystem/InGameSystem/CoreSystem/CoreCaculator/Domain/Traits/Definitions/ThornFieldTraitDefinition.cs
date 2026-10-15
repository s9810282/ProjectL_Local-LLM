using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class ThornFieldTraitDefinition : TraitDefinition
    {
        public int ThornDamage { get; }
        public int LifetimeUnitTurns { get; }

        public ThornFieldTraitDefinition(int thornDamage, int lifetimeUnitTurns)
            : base(TraitKey.ThornField)
        {
            if (thornDamage < 1)
                throw new ArgumentOutOfRangeException(nameof(thornDamage));
            if (lifetimeUnitTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(lifetimeUnitTurns));

            ThornDamage = thornDamage;
            LifetimeUnitTurns = lifetimeUnitTurns;
        }
    }
}
