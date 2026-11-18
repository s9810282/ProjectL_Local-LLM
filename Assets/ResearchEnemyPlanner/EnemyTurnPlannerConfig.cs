namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 턴 계획 AI의 고정 값. 성향·계산 크기 값(겁·기회·메인 배수·G1·걸음 벌점·증원 할인·한 턴 더 보기·빔·후보 수·다시 매김)은
    /// 조정 에셋의 프리셋(<c>EnemyTurnPlannerStyle</c>, LLMTest/docs/2026-11-18-core-turn-planner-port-design.md §11)으로 옮겼다.
    /// 원본 식: LLMTest/sim/evaluate.py·planner.py·reply.py.
    /// </summary>
    internal static class EnemyTurnPlannerConfig
    {
        /// <summary>계획 안에서 전투가 끝난 판의 값.</summary>
        internal const double Win = 1e9;

        /// <summary>
        /// 우선순위 실험(LLMTest/docs/2026-11-18-planner-background-parallel-design.md §14): 계획 계산을 도는 일 스레드의 우선순위.
        /// 그 계산 동안만 바꾸고 끝나면 되돌린다. Normal이면 바꾸지 않는다.
        /// </summary>
        internal const System.Threading.ThreadPriority WorkerPriority = System.Threading.ThreadPriority.Lowest;
    }
}
