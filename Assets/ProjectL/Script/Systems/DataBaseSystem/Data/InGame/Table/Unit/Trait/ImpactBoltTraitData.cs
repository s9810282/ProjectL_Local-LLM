using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 충격 볼트 설정입니다.</summary>
    public sealed class ImpactBoltTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.ImpactBolt;
        public int DistanceCells { get; }

        public ImpactBoltTraitData(int distanceCells) => DistanceCells = distanceCells;

        public override void Validate()
        {
            if (DistanceCells < 1)
                throw new InvalidOperationException("충격 볼트의 밀치기 거리는 1 이상이어야 합니다.");
        }
    }
}
