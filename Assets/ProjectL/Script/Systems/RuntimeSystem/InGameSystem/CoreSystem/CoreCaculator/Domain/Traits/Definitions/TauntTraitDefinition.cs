using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class TauntTraitDefinition : TraitDefinition
    {
        public int RadiusCells { get; }
        public int DamageReductionPercent { get; }

        public TauntTraitDefinition(int radiusCells) : this(radiusCells, 0) { }

        public TauntTraitDefinition(int radiusCells, int damageReductionPercent) : base(TraitKey.Taunt)
        {
            var spec = new TauntTraitSpec(radiusCells, damageReductionPercent);
            RadiusCells = spec.RadiusCells;
            DamageReductionPercent = spec.DamageReductionPercent;
        }
    }
}
