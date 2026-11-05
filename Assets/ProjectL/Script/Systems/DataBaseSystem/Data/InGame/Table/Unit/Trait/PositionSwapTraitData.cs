using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class PositionSwapTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.PositionSwap;

        public int ShieldAmount { get; }

        public PositionSwapTraitData() : this(0) { }

        public PositionSwapTraitData(int shieldAmount)
        {
            ShieldAmount = shieldAmount;
        }

        public override void Validate()
        {
            try
            {
                _ = new PositionSwapTraitSpec(ShieldAmount);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"PositionSwap.{exception.ParamName}: {exception.Message}", exception);
            }
        }
    }
}
