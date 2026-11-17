using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class CombatGimmickCalculator
    {
        /// <summary>호출자의 이동 규칙으로 지정 단계에 참여하는 기믹을 진행한다. null 규칙은 허용하지 않는다.</summary>
        public static CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickBoundary boundary,
            System.Collections.Generic.IReadOnlyList<IMovementTileRule> rules) =>
            StageGimmickComposition.WithMovementRules(rules).TryAdvance(input, boundary);
        /// <summary>기차의 기존 직접 호출 경로다. 예약 기믹은 완료 진영을 명시해야 한다.</summary>
        public static CombatCommandTransition TryAdvance(CombatCommandInput input) => StageGimmickComposition.Default.TryAdvance(input, null);

        /// <summary>진영 종료 누계를 다시 증가시키지 않는다. 토네이도처럼 이동 규칙이 필요한 기믹은 규칙을 받는 오버로드를 사용한다.</summary>
        public static CombatCommandTransition TryAdvance(CombatCommandInput input, StageGimmickBoundary boundary) =>
            StageGimmickComposition.Default.TryAdvance(input, boundary);
    }
}
