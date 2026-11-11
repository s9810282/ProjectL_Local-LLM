using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class RampageTraitSpec : TraitSpec
    {
        public int BaseAttackBonus { get; }
        public int RankAttackBonus { get; }
        public int MovementBonus { get; }

        public RampageTraitSpec(int baseAttackBonus, int rankAttackBonus, int movementBonus) : base(TraitKey.Rampage)
        {
            if (baseAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(baseAttackBonus));
            if (rankAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(rankAttackBonus));
            if (movementBonus < 0) throw new ArgumentOutOfRangeException(nameof(movementBonus));
            _ = checked(baseAttackBonus + rankAttackBonus);
            BaseAttackBonus = baseAttackBonus;
            RankAttackBonus = rankAttackBonus;
            MovementBonus = movementBonus;
        }

        protected override bool SameValuesCore(TraitSpec other) => other is RampageTraitSpec spec &&
            BaseAttackBonus == spec.BaseAttackBonus && RankAttackBonus == spec.RankAttackBonus && MovementBonus == spec.MovementBonus;
    }
}
