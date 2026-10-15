using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>
    /// 처치 연쇄 공격 특성의 공개 상태입니다. 설정값과 바뀌는 값이 모두 없습니다.
    /// </summary>
    public sealed class KillChainAttackTraitStatusSnapshot : TraitStatusSnapshot
    {
        public KillChainAttackTraitStatusSnapshot()
            : base(TraitKey.KillChainAttack)
        {
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is KillChainAttackTraitStatusSnapshot;
    }
}
