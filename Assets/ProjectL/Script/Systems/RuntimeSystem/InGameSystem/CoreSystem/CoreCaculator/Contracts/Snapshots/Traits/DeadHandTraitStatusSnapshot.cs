using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>죽음의 손의 공개 상태입니다. 피해 계수는 현재 등급의 정의에서 읽습니다.</summary>
    public sealed class DeadHandTraitStatusSnapshot : TraitStatusSnapshot
    {
        public DeadHandTraitStatusSnapshot() : base(TraitKey.DeadHand)
        {
        }
        public override bool SameValues(TraitStatusSnapshot other) => other is DeadHandTraitStatusSnapshot;
    }
}
