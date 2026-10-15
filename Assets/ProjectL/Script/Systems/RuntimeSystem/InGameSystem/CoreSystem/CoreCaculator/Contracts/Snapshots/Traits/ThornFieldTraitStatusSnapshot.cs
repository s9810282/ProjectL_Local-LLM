using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class ThornFieldTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int ThornDamage { get; }
        public int LifetimeUnitTurns { get; }

        public ThornFieldTraitStatusSnapshot(int thornDamage, int lifetimeUnitTurns)
            : base(TraitKey.ThornField)
        {
            if (thornDamage < 1)
                throw new ArgumentOutOfRangeException(nameof(thornDamage));
            if (lifetimeUnitTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(lifetimeUnitTurns));

            ThornDamage = thornDamage;
            LifetimeUnitTurns = lifetimeUnitTurns;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is ThornFieldTraitStatusSnapshot b &&
            ThornDamage == b.ThornDamage && LifetimeUnitTurns == b.LifetimeUnitTurns;
    }
}
