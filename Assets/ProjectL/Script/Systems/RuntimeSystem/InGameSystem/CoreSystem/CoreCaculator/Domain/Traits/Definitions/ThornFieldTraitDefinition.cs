using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class ThornFieldTraitDefinition : TraitDefinition
    {
        public int ThornDamage { get; }
        public int DefenseReductionPercent { get; }
        public UnitTurnTickPhase TickPhase { get; }
        public int LifetimeUnitTurns { get; }

        public ThornFieldTraitDefinition(int thornDamage, int lifetimeUnitTurns,
            int defenseReductionPercent = 0, UnitTurnTickPhase tickPhase = UnitTurnTickPhase.Ended)
            : base(TraitKey.ThornField)
        {
            if (thornDamage < 1)
                throw new ArgumentOutOfRangeException(nameof(thornDamage));
            if (lifetimeUnitTurns < 1)
                throw new ArgumentOutOfRangeException(nameof(lifetimeUnitTurns));

            if (defenseReductionPercent < 0 || defenseReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(defenseReductionPercent));
            if (!Enum.IsDefined(typeof(UnitTurnTickPhase), tickPhase))
                throw new ArgumentOutOfRangeException(nameof(tickPhase));
            DefenseReductionPercent = defenseReductionPercent;
            TickPhase = tickPhase;
            ThornDamage = thornDamage;
            LifetimeUnitTurns = lifetimeUnitTurns;
        }
    }
}
