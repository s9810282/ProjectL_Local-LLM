using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class ArmorBreakerTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.ArmorBreaker;
        public int DefenseReductionPercentPerHit { get; }

        public ArmorBreakerTraitData(int defenseReductionPercentPerHit)
        {
            DefenseReductionPercentPerHit = defenseReductionPercentPerHit;
        }

        public override void Validate()
        {
            try
            {
                _ = new ArmorBreakerTraitSpec(DefenseReductionPercentPerHit);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"ArmorBreaker.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
