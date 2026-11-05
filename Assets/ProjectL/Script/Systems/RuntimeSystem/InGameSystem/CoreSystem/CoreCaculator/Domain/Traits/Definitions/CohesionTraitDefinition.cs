using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class CohesionTraitDefinition : TraitDefinition
    {
        public int DefensePerAdjacentAlly { get; }
        public int ShieldAmountPerAlly { get; }
        public int RadiusCells { get; }
        public int ResetEveryControllerTurnEnds { get; }

        public CohesionTraitDefinition(int defensePerAdjacentAlly, int shieldAmountPerAlly, int radiusCells, int resetEveryControllerTurnEnds) : base(TraitKey.Cohesion)
        {
            var spec = new CohesionTraitSpec(defensePerAdjacentAlly, shieldAmountPerAlly, radiusCells, resetEveryControllerTurnEnds);
            DefensePerAdjacentAlly = spec.DefensePerAdjacentAlly;
            ShieldAmountPerAlly = spec.ShieldAmountPerAlly;
            RadiusCells = spec.RadiusCells;
            ResetEveryControllerTurnEnds = spec.ResetEveryControllerTurnEnds;
        }
    }
}
