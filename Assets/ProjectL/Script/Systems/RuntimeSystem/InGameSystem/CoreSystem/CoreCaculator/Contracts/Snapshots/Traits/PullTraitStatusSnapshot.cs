using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>정의에서 전달된 추가 사거리입니다. 대상의 끌어오기 상태와 분리합니다.</summary>
    public sealed class PullTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int ExtraRangeCells { get; }

        public PullTraitStatusSnapshot(int extraRangeCells) : base(TraitKey.Pull)
        {
            if (extraRangeCells < 0) throw new ArgumentOutOfRangeException(nameof(extraRangeCells));
            ExtraRangeCells = extraRangeCells;
        }

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is PullTraitStatusSnapshot mark && ExtraRangeCells == mark.ExtraRangeCells;
    }
}
