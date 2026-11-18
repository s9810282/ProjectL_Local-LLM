using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi
{
    /// <summary>
    /// 적 행동을 고르는 방식이다(연구 저장소 전용 연결, LLMTest/docs/2026-11-18-core-turn-planner-port-design.md CP2).
    /// Utility = 지금 게임 적 AI(기본), TurnPlanner = 연구용 턴 계획 AI(Assets/ResearchEnemyPlanner).
    /// </summary>
    public enum EnemyDecisionModel
    {
        Utility = 0,
        TurnPlanner = 1
    }

    /// <summary>
    /// 턴 계획 AI의 성향·계산 크기 값이다(연구 저장소 전용 연결, LLMTest/docs/2026-11-18-core-turn-planner-port-design.md §11).
    /// 조정 에셋의 프리셋 SO(EnemyTurnPlannerPresetSO)가 만들고, 비어 있으면 <see cref="Default"/>(연구 시뮬레이터 plan4:g1:l1과 같은 값)다.
    /// 판 점수 식과 값마다의 뜻은 프리셋 SO의 설명에 있다.
    /// </summary>
    public sealed class EnemyTurnPlannerStyle
    {
        public const double DefaultFear = 0.5;
        public const double DefaultOpportunity = 0.5;
        public const double DefaultMainWeight = 3.0;
        public const double DefaultGoalStepPercent = 1.0;
        public const double DefaultStepPenalty = 10.0;
        public const double DefaultReinforcementDiscount = 0.5;
        public const bool DefaultLookahead = true;
        public const int DefaultBeam = 4;
        public const int DefaultPerUnit = 6;
        public const int DefaultMoveOnly = 3;
        public const int DefaultRerank = 4;

        public static EnemyTurnPlannerStyle Default { get; } = new EnemyTurnPlannerStyle();

        /// <summary>겁: 위험(다음 플레이어 턴에 잃을 전력)에 곱하는 값.</summary>
        public double Fear { get; }
        /// <summary>기회: 다음 적 턴에 빼앗을 전력에 곱하는 값.</summary>
        public double Opportunity { get; }
        /// <summary>메인 캐릭터의 전력·피해 배수.</summary>
        public double MainWeight { get; }
        /// <summary>G1: 메인을 칠 수 없는 적 유닛의 모자란 걸음 하나에 빼는 메인 가치의 %.</summary>
        public double GoalStepPercent { get; }
        /// <summary>칠 대상이 닿지 않는 유닛의 모자란 걸음 하나의 벌점(전력 단위).</summary>
        public double StepPenalty { get; }
        /// <summary>아직 나오지 않은 증원을 적 전력에 세는 비율.</summary>
        public double ReinforcementDiscount { get; }
        /// <summary>한 턴 더 보기: 깊은 V가 플레이어 턴 전체를 차례대로 둬 본 뒤 판의 빠른 V다. false면 옛 깊은 V.</summary>
        public bool Lookahead { get; }
        /// <summary>빔 크기(행동 하나를 정할 때마다 남기는 판 수).</summary>
        public int Beam { get; }
        /// <summary>유닛 하나에서 펼치는 후보 수(빠른 V 상위).</summary>
        public int PerUnit { get; }
        /// <summary>이동만 하는 후보 중 빠른 이동 점수 상위 몇 개를 매기나.</summary>
        public int MoveOnly { get; }
        /// <summary>끝난 계획 중 빠른 V 상위 몇 개를 깊은 V로 다시 매기나. 2보다 작으면 다시 매기지 않는다.</summary>
        public int Rerank { get; }

        public EnemyTurnPlannerStyle(double fear = DefaultFear, double opportunity = DefaultOpportunity,
            double mainWeight = DefaultMainWeight, double goalStepPercent = DefaultGoalStepPercent,
            double stepPenalty = DefaultStepPenalty, double reinforcementDiscount = DefaultReinforcementDiscount,
            bool lookahead = DefaultLookahead, int beam = DefaultBeam, int perUnit = DefaultPerUnit,
            int moveOnly = DefaultMoveOnly, int rerank = DefaultRerank)
        {
            Fear = NonNegative(fear, nameof(fear));
            Opportunity = NonNegative(opportunity, nameof(opportunity));
            MainWeight = NonNegative(mainWeight, nameof(mainWeight));
            GoalStepPercent = NonNegative(goalStepPercent, nameof(goalStepPercent));
            StepPenalty = NonNegative(stepPenalty, nameof(stepPenalty));
            ReinforcementDiscount = NonNegative(reinforcementDiscount, nameof(reinforcementDiscount));
            Lookahead = lookahead;
            Beam = beam >= 1 ? beam : throw new ArgumentOutOfRangeException(nameof(beam));
            PerUnit = perUnit >= 1 ? perUnit : throw new ArgumentOutOfRangeException(nameof(perUnit));
            MoveOnly = moveOnly >= 0 ? moveOnly : throw new ArgumentOutOfRangeException(nameof(moveOnly));
            Rerank = rerank >= 0 ? rerank : throw new ArgumentOutOfRangeException(nameof(rerank));
        }

        /// <summary>같은 값에 깊은 V 종류만 바꾼 값(측정 도구가 두 종류를 같은 실행에서 비교할 때).</summary>
        public EnemyTurnPlannerStyle WithLookahead(bool lookahead) => new EnemyTurnPlannerStyle(Fear, Opportunity, MainWeight,
            GoalStepPercent, StepPenalty, ReinforcementDiscount, lookahead, Beam, PerUnit, MoveOnly, Rerank);

        private static double NonNegative(double value, string parameter) =>
            value >= 0 && !double.IsInfinity(value) ? value : throw new ArgumentOutOfRangeException(parameter);
    }

    /// <summary>결정을 시작할 때 고정하는 선택적 평가 한도와 기획 조정값이다.</summary>
    public sealed class EnemyDecisionSettings
    {
        /// <summary>목숨을 걸 만한 최소 피해(대상 최대 체력의 %)의 기본값이다. 기획 조정 에셋이 없을 때 쓴다.</summary>
        public const int DefaultDeathTradeThresholdPercent = 50;
        /// <summary>처치당하지 않는 칸의 예상 피해를 이동 점수에서 빼는 비율(%)의 기본값이다.</summary>
        public const int DefaultThreatAversionPercent = 50;
        /// <summary>다음 기회에 칠 수 있는 자리의 값 비율(%)의 기본값이다.</summary>
        public const int DefaultNextTurnApproachPercent = 50;
        /// <summary>세 조정값이 받을 수 있는 가장 큰 백분율이다.</summary>
        public const int MaxTuningPercent = 1000;

        public static EnemyDecisionSettings Default { get; } = new EnemyDecisionSettings();

        public int AttackCandidateLimit { get; }
        public int MoveCandidateLimit { get; }
        public int RefinedMoveLimit { get; }
        public int FollowupAimLimit { get; }
        public int ActorPreviewLimit { get; }
        public int DecisionPreviewLimit { get; }
        public int AlternateRouteNodeLimit { get; }
        public int DeathTradeThresholdPercent { get; }
        public int ThreatAversionPercent { get; }
        public int NextTurnApproachPercent { get; }
        /// <summary>적 행동을 고르는 방식이다(연구용). 기본은 지금 AI다.</summary>
        public EnemyDecisionModel DecisionModel { get; }
        /// <summary>턴 계획 AI의 성향·계산 크기 값이다(연구용, <see cref="DecisionModel"/>이 TurnPlanner일 때만 쓴다).</summary>
        public EnemyTurnPlannerStyle PlannerStyle { get; }

        public EnemyDecisionSettings(int attackCandidateLimit = 4, int moveCandidateLimit = 6, int refinedMoveLimit = 2,
            int followupAimLimit = 1, int actorPreviewLimit = 8, int decisionPreviewLimit = 16,
            int alternateRouteNodeLimit = 256, int deathTradeThresholdPercent = DefaultDeathTradeThresholdPercent,
            int threatAversionPercent = DefaultThreatAversionPercent, int nextTurnApproachPercent = DefaultNextTurnApproachPercent,
            EnemyDecisionModel decisionModel = EnemyDecisionModel.Utility, EnemyTurnPlannerStyle plannerStyle = null)
        {
            DecisionModel = Enum.IsDefined(typeof(EnemyDecisionModel), decisionModel)
                ? decisionModel : throw new ArgumentOutOfRangeException(nameof(decisionModel));
            PlannerStyle = plannerStyle ?? EnemyTurnPlannerStyle.Default;
            DeathTradeThresholdPercent = Percent(deathTradeThresholdPercent, nameof(deathTradeThresholdPercent));
            ThreatAversionPercent = Percent(threatAversionPercent, nameof(threatAversionPercent));
            NextTurnApproachPercent = Percent(nextTurnApproachPercent, nameof(nextTurnApproachPercent));
            AttackCandidateLimit = Positive(attackCandidateLimit, nameof(attackCandidateLimit));
            MoveCandidateLimit = Positive(moveCandidateLimit, nameof(moveCandidateLimit));
            RefinedMoveLimit = Positive(refinedMoveLimit, nameof(refinedMoveLimit));
            ActorPreviewLimit = Positive(actorPreviewLimit, nameof(actorPreviewLimit));
            DecisionPreviewLimit = Positive(decisionPreviewLimit, nameof(decisionPreviewLimit));
            AlternateRouteNodeLimit = Positive(alternateRouteNodeLimit, nameof(alternateRouteNodeLimit));
            if (refinedMoveLimit > moveCandidateLimit) throw new ArgumentOutOfRangeException(nameof(refinedMoveLimit));
            if (followupAimLimit != 1) throw new ArgumentOutOfRangeException(nameof(followupAimLimit));
            if (actorPreviewLimit > decisionPreviewLimit) throw new ArgumentOutOfRangeException(nameof(actorPreviewLimit));
            FollowupAimLimit = followupAimLimit;
        }

        private static int Positive(int value, string parameter) =>
            value > 0 ? value : throw new ArgumentOutOfRangeException(parameter);

        private static int Percent(int value, string parameter) =>
            value >= 0 && value <= MaxTuningPercent ? value : throw new ArgumentOutOfRangeException(parameter);
    }
}
