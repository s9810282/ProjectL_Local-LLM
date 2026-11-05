using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class SacrificeTraitDefinition : TraitDefinition
    {
        public int RadiusCells { get; }
        public int MaxTriggersPerEnemyTurn { get; }

        public SacrificeTraitDefinition(int radiusCells, int maxTriggersPerEnemyTurn) : base(TraitKey.Sacrifice)
        {
            var spec = new SacrificeTraitSpec(radiusCells, maxTriggersPerEnemyTurn);
            RadiusCells = spec.RadiusCells;
            MaxTriggersPerEnemyTurn = spec.MaxTriggersPerEnemyTurn;
        }
    }
}
