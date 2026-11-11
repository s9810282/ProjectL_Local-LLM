using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class SacrificeTraitDefinition : TraitDefinition
    {
        public int RadiusCells { get; }
        public int MaxTriggersPerEnemyTurn { get; }
        public bool PreventsLethalSacrificeDamage { get; }
        public int ShieldOnActivation { get; }

        public SacrificeTraitDefinition(int radiusCells, int maxTriggersPerEnemyTurn,
            bool preventsLethalSacrificeDamage = false, int shieldOnActivation = 0) : base(TraitKey.Sacrifice)
        {
            var spec = new SacrificeTraitSpec(radiusCells, maxTriggersPerEnemyTurn, preventsLethalSacrificeDamage, shieldOnActivation);
            RadiusCells = spec.RadiusCells;
            MaxTriggersPerEnemyTurn = spec.MaxTriggersPerEnemyTurn;
            PreventsLethalSacrificeDamage = spec.PreventsLethalSacrificeDamage;
            ShieldOnActivation = spec.ShieldOnActivation;
        }
    }
}
