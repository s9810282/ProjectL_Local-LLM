using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class FinishingShotTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.FinishingShot;
        public int AttackThresholdPercent { get; }

        public FinishingShotTraitData(int attackThresholdPercent)
        {
            AttackThresholdPercent = attackThresholdPercent;
        }

        public override void Validate()
        {
            try
            {
                _ = new FinishingShotTraitSpec(AttackThresholdPercent);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"FinishingShot.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
