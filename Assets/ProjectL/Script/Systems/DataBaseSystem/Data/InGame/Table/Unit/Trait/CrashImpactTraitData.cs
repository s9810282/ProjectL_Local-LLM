using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 작렬 설정입니다.</summary>
    public sealed class CrashImpactTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.CrashImpact;
        public int DistanceCells { get; }
        public int WallDamagePercent { get; }
        public int UnitDamagePercent { get; }

        public CrashImpactTraitData(int distanceCells, int wallDamagePercent, int unitDamagePercent)
        {
            DistanceCells = distanceCells;
            WallDamagePercent = wallDamagePercent;
            UnitDamagePercent = unitDamagePercent;
        }

        public override void Validate()
        {
            if (DistanceCells < 1)
                throw new InvalidOperationException("작렬의 날아가는 거리는 1 이상이어야 합니다.");
            if (WallDamagePercent < 0 || WallDamagePercent > 100 || UnitDamagePercent < 0 || UnitDamagePercent > 100)
                throw new InvalidOperationException("작렬의 부딪힘 피해 비율은 0~100이어야 합니다.");
        }
    }
}
