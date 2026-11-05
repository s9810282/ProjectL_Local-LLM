using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class ReconSquadTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.ReconSquad;
        public int IncreasePercent { get; }
        public int Radius { get; }

        public ReconSquadTraitData(int increasePercent, int radius)
        {
            IncreasePercent = increasePercent;
            Radius = radius;
        }

        public override void Validate()
        {
            try
            {
                _ = new ReconSquadTraitSpec(IncreasePercent, Radius);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"ReconSquad.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
