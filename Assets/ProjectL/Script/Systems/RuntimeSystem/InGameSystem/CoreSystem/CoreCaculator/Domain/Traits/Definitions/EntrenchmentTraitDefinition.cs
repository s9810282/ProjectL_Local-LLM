using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class EntrenchmentTraitDefinition : TraitDefinition
    {
        public int RadiusCells { get; }
        public int DamageReductionPercent { get; }

        public EntrenchmentTraitDefinition(int radiusCells, int damageReductionPercent) : base(TraitKey.Entrenchment)
        {
            var spec = new EntrenchmentTraitSpec(radiusCells, damageReductionPercent);
            RadiusCells = spec.RadiusCells;
            DamageReductionPercent = spec.DamageReductionPercent;
        }
    }
}
