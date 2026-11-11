using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>
    /// 공포 특성의 규칙입니다. 소유자가 피해로 죽으면 주변 8칸 캐릭터에게 무장 해제·속박을 함께 겁니다.
    /// 기간은 걸린 쪽의 다음 자기 턴 한 번(기본 1 + 종료 감소가 남은 진영이면 1)입니다. 실행은 명령 완료 정산의 사망 특성 실행자가 맡습니다.
    /// </summary>
    internal sealed class FearTraitRule : ICombatTraitRule
    {
        /// <summary>종료 감소 보정 전의 기본 기간(걸린 쪽 자기 턴 수)입니다.</summary>
        internal const int BaseAffectedSideTurns = 1;

        public TraitKey Key => TraitKey.Fear;
    }
}
