using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 공포 특성입니다. 적 전용이며 설정값이 없습니다.</summary>
    public sealed class FearTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Fear;

        public override void Validate()
        {
        }
    }
}
