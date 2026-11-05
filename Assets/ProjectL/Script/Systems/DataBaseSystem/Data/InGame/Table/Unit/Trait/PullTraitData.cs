using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 끌어오기 설정입니다.</summary>
    public sealed class PullTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Pull;
        public int ExtraRangeCells { get; }

        public PullTraitData(int extraRangeCells) => ExtraRangeCells = extraRangeCells;

        public override void Validate()
        {
            if (ExtraRangeCells < 0)
                throw new InvalidOperationException("끌어오기의 추가 사거리은 음수일 수 없습니다.");
        }
    }
}
