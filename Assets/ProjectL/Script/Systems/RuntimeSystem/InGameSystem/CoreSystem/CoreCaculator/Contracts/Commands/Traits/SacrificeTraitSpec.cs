using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class SacrificeTraitSpec : TraitSpec
    {
        public int RadiusCells { get; }
        public int MaxTriggersPerEnemyTurn { get; }
        public bool PreventsLethalSacrificeDamage { get; }
        public int ShieldOnActivation { get; }

        public SacrificeTraitSpec(int radiusCells, int maxTriggersPerEnemyTurn)
            : this(radiusCells, maxTriggersPerEnemyTurn, false, 0)
        {
        }

        public SacrificeTraitSpec(int radiusCells, int maxTriggersPerEnemyTurn,
            bool preventsLethalSacrificeDamage = false, int shieldOnActivation = 0) : base(TraitKey.Sacrifice)
        {
            if (radiusCells < 1)
                throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (maxTriggersPerEnemyTurn < 1)
                throw new ArgumentOutOfRangeException(nameof(maxTriggersPerEnemyTurn));
            if (shieldOnActivation < 0)
                throw new ArgumentOutOfRangeException(nameof(shieldOnActivation));
            RadiusCells = radiusCells;
            MaxTriggersPerEnemyTurn = maxTriggersPerEnemyTurn;
            PreventsLethalSacrificeDamage = preventsLethalSacrificeDamage;
            ShieldOnActivation = shieldOnActivation;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (SacrificeTraitSpec)other;
            return RadiusCells == otherSpec.RadiusCells &&
                MaxTriggersPerEnemyTurn == otherSpec.MaxTriggersPerEnemyTurn &&
                PreventsLethalSacrificeDamage == otherSpec.PreventsLethalSacrificeDamage &&
                ShieldOnActivation == otherSpec.ShieldOnActivation;
        }
    }
}
