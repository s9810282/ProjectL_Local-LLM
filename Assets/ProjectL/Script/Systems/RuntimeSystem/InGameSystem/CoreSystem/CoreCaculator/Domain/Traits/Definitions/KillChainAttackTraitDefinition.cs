using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>
    /// 처치 연쇄 공격 특성의 설정입니다. 횟수 제한과 피해 규칙이 고정이라 설정값이 없습니다.
    /// </summary>
    public sealed class KillChainAttackTraitDefinition : TraitDefinition
    {
        public KillChainAttackTraitDefinition()
            : base(TraitKey.KillChainAttack)
        {
        }
    }
}
