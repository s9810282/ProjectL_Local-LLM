using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class GroundCompactionTraitDefinition : TraitDefinition
    {
        public int MovementRangeBonus { get; }
        public int OpposingTurnEndsToExpire { get; }

        public GroundCompactionTraitDefinition(int movementRangeBonus, int opposingTurnEndsToExpire) : base(TraitKey.GroundCompaction)
        {
            var spec = new GroundCompactionTraitSpec(movementRangeBonus, opposingTurnEndsToExpire);
            MovementRangeBonus = spec.MovementRangeBonus;
            OpposingTurnEndsToExpire = spec.OpposingTurnEndsToExpire;
        }
    }
}
