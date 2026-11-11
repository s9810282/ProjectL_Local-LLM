using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>
    /// 자폭 특성의 규칙입니다. 소유자가 피해로 죽으면 사망 칸 주변 8칸의 캐릭터·구조물에게 사망 직전 공격력의 단계 비율만큼 환경 피해를 줍니다.
    /// 방어는 적용되지 않고 보호막·엄폐·피해 감소는 적용됩니다. 실행은 명령 완료 정산의 사망 특성 실행자가 맡습니다.
    /// </summary>
    internal sealed class SelfDestructTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.SelfDestruct;

        /// <summary>사망 직전 공격력과 단계 비율로 정한 폭발 피해량입니다(내림).</summary>
        internal static int Damage(int attackPower, int percent) =>
            checked((int)((long)attackPower * percent / 100));
    }
}
