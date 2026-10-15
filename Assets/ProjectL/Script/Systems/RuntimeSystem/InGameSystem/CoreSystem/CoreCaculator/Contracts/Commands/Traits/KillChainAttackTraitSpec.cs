using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>
    /// 적을 처치하면 맵 전체의 랜덤한 적 캐릭터를 추가 공격하는 특성의 입력입니다. 설정값이 없습니다.
    /// </summary>
    public sealed class KillChainAttackTraitSpec : TraitSpec
    {
        public KillChainAttackTraitSpec()
            : base(TraitKey.KillChainAttack)
        {
        }
    }
}
