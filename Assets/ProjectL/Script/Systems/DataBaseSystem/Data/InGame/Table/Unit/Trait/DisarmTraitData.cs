using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class DisarmTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Disarm;
        public int DurationAffectedSideTurnEnds { get; }

        public DisarmTraitData(int durationAffectedSideTurnEnds)
        {
            DurationAffectedSideTurnEnds = durationAffectedSideTurnEnds;
        }

        public override void Validate()
        {
            try
            {
                _ = new DisarmTraitSpec(DurationAffectedSideTurnEnds);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"Disarm.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
