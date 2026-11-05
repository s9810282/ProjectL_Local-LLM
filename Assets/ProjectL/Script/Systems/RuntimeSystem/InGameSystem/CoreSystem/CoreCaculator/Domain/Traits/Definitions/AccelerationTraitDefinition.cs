using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class AccelerationTraitDefinition : TraitDefinition
    {
        public int AttackPerMovedCell { get; }

        public AccelerationTraitDefinition(int attackPerMovedCell) : base(TraitKey.Acceleration)
        {
            var spec = new AccelerationTraitSpec(attackPerMovedCell);
            AttackPerMovedCell = spec.AttackPerMovedCell;
        }
    }
}
