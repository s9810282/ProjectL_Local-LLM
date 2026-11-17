namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 턴 계획 AI의 값(연구 시뮬레이터 <c>plan4:g1</c>와 같은 값). 조정 에셋으로 꺼내지 않는다(설계 TP-8).
    /// 원본 식: LLMTest/sim/evaluate.py·planner.py·reply.py.
    /// </summary>
    internal static class EnemyTurnPlannerConfig
    {
        /// <summary>빔 크기(단계마다 남기는 판 수).</summary>
        internal const int Beam = 4;
        /// <summary>유닛 하나에서 펼치는 후보 수(빠른 V 상위).</summary>
        internal const int PerUnit = 6;
        /// <summary>이동만 하는 후보 중 빠른 이동 점수 상위 몇 개를 매기나.</summary>
        internal const int MoveOnly = 3;
        /// <summary>빠른 V 상위 몇 개를 깊은 평가(엔진 응수)로 다시 매기나. 0이면 다시 매기지 않는다.</summary>
        internal const int Rerank = 4;

        internal const double Fear = 0.5;
        internal const double Opportunity = 0.5;
        /// <summary>칠 대상이 없을 때 걸음 하나의 벌점.</summary>
        internal const double StepPenalty = 10.0;
        /// <summary>메인 캐릭터 전력 배수.</summary>
        internal const double MainWeight = 3.0;
        /// <summary>남은 증원 전력 할인.</summary>
        internal const double ReinforcementDiscount = 0.5;
        /// <summary>G1: 승리 대상까지 모자란 걸음 하나에 승리 대상 가치의 몇 %를 빼나.</summary>
        internal const double GoalStep = 1.0;

        /// <summary>계획 안에서 전투가 끝난 판의 값.</summary>
        internal const double Win = 1e9;
    }
}
