using System;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 SO에서 넣는 공포 특성입니다. 적 전용(EnemyOnly) 유닛에만 넣을 수 있고 설정값이 없습니다.</summary>
    [Serializable]
    internal sealed class FearTraitAuthoring : UnitTraitAuthoring
    {
        internal override UnitTraitData ToData() => new FearTraitData();
    }
}
