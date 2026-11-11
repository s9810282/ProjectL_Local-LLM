using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi
{
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

        public EnemyDecisionSettings(int attackCandidateLimit = 4, int moveCandidateLimit = 6, int refinedMoveLimit = 2,
            int followupAimLimit = 1, int actorPreviewLimit = 8, int decisionPreviewLimit = 16,
            int alternateRouteNodeLimit = 256, int deathTradeThresholdPercent = DefaultDeathTradeThresholdPercent,
            int threatAversionPercent = DefaultThreatAversionPercent, int nextTurnApproachPercent = DefaultNextTurnApproachPercent)
        {
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
