using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class RampageTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int BaseAttackBonus { get; }
        public int RankAttackBonus { get; }
        public int MovementBonus { get; }
        public int KillAttackBonus { get; }

        public RampageTraitStatusSnapshot(int baseAttackBonus, int rankAttackBonus, int movementBonus, int killAttackBonus = 0)
            : base(TraitKey.Rampage)
        {
            var spec = new RampageTraitSpec(baseAttackBonus, rankAttackBonus, movementBonus);
            if (killAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(killAttackBonus));
            _ = checked(baseAttackBonus + rankAttackBonus + killAttackBonus);
            BaseAttackBonus = spec.BaseAttackBonus;
            RankAttackBonus = spec.RankAttackBonus;
            MovementBonus = spec.MovementBonus;
            KillAttackBonus = killAttackBonus;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is RampageTraitStatusSnapshot status &&
            BaseAttackBonus == status.BaseAttackBonus && RankAttackBonus == status.RankAttackBonus &&
            MovementBonus == status.MovementBonus && KillAttackBonus == status.KillAttackBonus;
    }
}
