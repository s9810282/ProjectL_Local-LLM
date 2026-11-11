using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>작렬: 넉백 거리와, 막혀 멈출 때의 부딪힘 피해 비율(날아간 대상 최대 체력의 %)입니다.</summary>
    public sealed class CrashImpactTraitSpec : TraitSpec
    {
        public int DistanceCells { get; }
        /// <summary>벽·맵 끝·고지대 턱·구조물에 부딪힐 때 날아간 대상이 받는 비율입니다.</summary>
        public int WallDamagePercent { get; }
        /// <summary>유닛에 부딪힐 때 날아간 대상과 부딪힌 유닛이 각각 받는 비율입니다.</summary>
        public int UnitDamagePercent { get; }

        public CrashImpactTraitSpec(int distanceCells, int wallDamagePercent, int unitDamagePercent)
            : base(TraitKey.CrashImpact)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));
            if (wallDamagePercent < 0 || wallDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(wallDamagePercent));
            if (unitDamagePercent < 0 || unitDamagePercent > 100)
                throw new ArgumentOutOfRangeException(nameof(unitDamagePercent));

            DistanceCells = distanceCells;
            WallDamagePercent = wallDamagePercent;
            UnitDamagePercent = unitDamagePercent;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (CrashImpactTraitSpec)other;
            return DistanceCells == otherSpec.DistanceCells && WallDamagePercent == otherSpec.WallDamagePercent &&
                UnitDamagePercent == otherSpec.UnitDamagePercent;
        }
    }
}
