using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>
    /// 처치 연쇄 공격 특성의 규칙 식별자입니다. 활성 여부만 알리고, 추가 공격 진행은 Application 계산기가 맡습니다.
    /// </summary>
    internal sealed class KillChainAttackTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.KillChainAttack;
    }
}
