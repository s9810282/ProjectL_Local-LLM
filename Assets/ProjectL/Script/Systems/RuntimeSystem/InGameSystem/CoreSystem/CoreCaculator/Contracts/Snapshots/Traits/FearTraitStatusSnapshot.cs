using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>공포 특성의 공개 상태입니다. 설정값과 바뀌는 값이 모두 없습니다.</summary>
    public sealed class FearTraitStatusSnapshot : TraitStatusSnapshot
    {
        public FearTraitStatusSnapshot() : base(TraitKey.Fear)
        {
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is FearTraitStatusSnapshot;
    }
}
