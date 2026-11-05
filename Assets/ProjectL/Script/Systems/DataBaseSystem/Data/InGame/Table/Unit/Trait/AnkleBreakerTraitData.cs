using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class AnkleBreakerTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.AnkleBreaker;
        public int DurationAffectedSideTurnEnds { get; }

        public AnkleBreakerTraitData(int durationAffectedSideTurnEnds)
        {
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new AnkleBreakerTraitSpec(DurationAffectedSideTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"AnkleBreaker.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
