using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class PositionSwapTraitDefinition : TraitDefinition
    {
        public int ShieldAmount { get; }

        public PositionSwapTraitDefinition() : this(0) { }

        public PositionSwapTraitDefinition(int shieldAmount) : base(TraitKey.PositionSwap)
        {
            ShieldAmount = new PositionSwapTraitSpec(shieldAmount).ShieldAmount;
        }
    }
}
