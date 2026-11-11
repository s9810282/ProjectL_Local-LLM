using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>공개 투영의 관측값이다. 점수 가중치나 실제 세계의 확정 보장을 소유하지 않는다.</summary>
    internal sealed class EnemyScoreFacts
    {
        internal CombatAssessmentGap Gaps { get; }
        internal bool IsPrecise { get; }
        internal bool BudgetExhausted { get; }
        internal long OpponentHpLossPermille { get; }
        internal long AlliedHpLossPermille { get; }
        internal int OpponentEliminations { get; }
        internal int AlliedEliminations { get; }
        internal long RestrictionDeltaPermille { get; }
        internal long PossibleAlliedHpLossPermille { get; }
        internal bool HasUnresolvedAlliedLoss { get; }
        internal CellCoord? Endpoint { get; }
        internal StageOutcome ProjectedOutcome { get; }
        internal bool ProjectedTerminal => ProjectedOutcome != StageOutcome.InProgress;
        internal bool GuaranteedTerminal => IsPrecise && Gaps == CombatAssessmentGap.None && ProjectedTerminal;
        internal CharacterCombatFacts PostActionActor { get; }
        internal long ActorHazardLossPermille { get; }
        internal long PlayerThreatPermille { get; }
        internal long EngagementProgress { get; }
        internal long SubsequentAttackBenefit { get; }
        internal bool AdditionalForecastBind { get; }
        /// <summary>종점에서 다음 기회에 칠 수 있는 가장 좋은 대상의 값이다(설계 6.5). 닿지 않거나 지정할 수 없으면 0이다.</summary>
        internal long NextTurnStrike { get; }
        /// <summary>종점에서 플레이어가 다음 기회에 이 유닛을 처치할 수 있다(위협 피해가 남은 체력 이상).</summary>
        internal bool PlayerThreatLethal { get; }

        internal EnemyScoreFacts(CombatAssessmentGap gaps, bool precise = false, bool budgetExhausted = false,
            long opponentHpLoss = 0, long alliedHpLoss = 0, int opponentEliminations = 0, int alliedEliminations = 0,
            long restrictionDelta = 0, long possibleAlliedHpLoss = 0, CellCoord? endpoint = null,
            StageOutcome projectedOutcome = StageOutcome.InProgress, CharacterCombatFacts postActionActor = null,
            long actorHazardLoss = 0, long playerThreat = 0, long engagementProgress = 0, long subsequentAttackBenefit = 0,
            bool? hasUnresolvedAlliedLoss = null, bool additionalForecastBind = false, long nextTurnStrike = 0,
            bool playerThreatLethal = false)
        {
            NextTurnStrike = nextTurnStrike;
            PlayerThreatLethal = playerThreatLethal;
            Gaps = gaps;
            IsPrecise = precise;
            BudgetExhausted = budgetExhausted;
            OpponentHpLossPermille = opponentHpLoss;
            AlliedHpLossPermille = alliedHpLoss;
            OpponentEliminations = opponentEliminations;
            AlliedEliminations = alliedEliminations;
            RestrictionDeltaPermille = restrictionDelta;
            PossibleAlliedHpLossPermille = possibleAlliedHpLoss;
            HasUnresolvedAlliedLoss = hasUnresolvedAlliedLoss ?? (!precise && !budgetExhausted &&
                (gaps & (CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.ActualOnlyReaction |
                    CombatAssessmentGap.UnsupportedEffect | CombatAssessmentGap.UnknownPosition)) != 0);
            Endpoint = endpoint;
            ProjectedOutcome = projectedOutcome;
            PostActionActor = postActionActor;
            ActorHazardLossPermille = actorHazardLoss;
            PlayerThreatPermille = playerThreat;
            EngagementProgress = engagementProgress;
            SubsequentAttackBenefit = subsequentAttackBenefit;
            AdditionalForecastBind = additionalForecastBind;
        }

        internal EnemyScoreFacts WithPosition(CombatAssessmentGap gaps, long hazard, long threat, long progress, long followup,
            long additionalAlliedLoss = 0, bool additionalForecastBind = false, long nextTurnStrike = 0, bool playerThreatLethal = false) =>
            new EnemyScoreFacts(Gaps | gaps, IsPrecise, BudgetExhausted, OpponentHpLossPermille,
                checked(AlliedHpLossPermille + additionalAlliedLoss), OpponentEliminations, AlliedEliminations, RestrictionDeltaPermille,
                PossibleAlliedHpLossPermille, Endpoint, ProjectedOutcome, PostActionActor, hazard, threat, progress, followup,
                HasUnresolvedAlliedLoss, additionalForecastBind, nextTurnStrike, playerThreatLethal);
    }

    /// <summary>
    /// 결정 설정에서 온 교환 모형 가중치다(설계 6.5·6.5.1). 기본값은 <see cref="EnemyDecisionSettings.Default"/>의 백분율이다(값의 주인은 설정 계약).
    /// 백분율 값은 정수 나눗셈으로 쓴다.
    /// </summary>
    internal readonly struct EnemyScoreWeights
    {
        internal static EnemyScoreWeights Default { get; } = From(EnemyDecisionSettings.Default);

        /// <summary>처치당할 수 있는 칸의 고정 값이다(상대 손실 천분율, 백분율 × 10).</summary>
        internal long DeathTradePermille { get; }
        internal long ThreatPercent { get; }
        internal long NextTurnPercent { get; }

        private EnemyScoreWeights(int deathTradePercent, int threatPercent, int nextTurnPercent)
        {
            DeathTradePermille = checked(deathTradePercent * 10L);
            ThreatPercent = threatPercent;
            NextTurnPercent = nextTurnPercent;
        }

        internal static EnemyScoreWeights From(EnemyDecisionSettings settings) => new EnemyScoreWeights(
            settings.DeathTradeThresholdPercent, settings.ThreatAversionPercent, settings.NextTurnApproachPercent);
    }

    /// <summary>
    /// 점수식의 항목별 값이다(설계 6.9). 모든 값은 점수에 더하는 부호다: 플레이어 위험·환경 위험·예보 속박은 0 이하다.
    /// <see cref="Total"/>이 <see cref="EnemyUtilityScorer.Score(in EnemyScoreFacts, in EnemyScoreWeights)"/>다.
    /// </summary>
    internal readonly struct EnemyScoreTerms
    {
        internal long Attack { get; }
        internal long Hazard { get; }
        internal long PlayerRisk { get; }
        internal long Progress { get; }
        internal long Followup { get; }
        internal long NextTurn { get; }
        internal long Bind { get; }
        internal long Total => checked(Attack + Hazard + PlayerRisk + Progress + Followup + NextTurn + Bind);

        internal EnemyScoreTerms(long attack, long hazard, long playerRisk, long progress, long followup, long nextTurn, long bind)
        {
            Attack = attack;
            Hazard = hazard;
            PlayerRisk = playerRisk;
            Progress = progress;
            Followup = followup;
            NextTurn = nextTurn;
            Bind = bind;
        }

        public override string ToString() =>
            $"attack={Attack} hazard={Hazard} playerRisk={PlayerRisk} progress={Progress} followup={Followup} nextTurn={NextTurn} bind={Bind} total={Total}";
    }

    internal static class EnemyUtilityScorer
    {
        internal const long RestrictionUnitPermille = 500;

        private const long OpposingLossWeight = 1;
        internal const long OpposingEliminationWeight = 2000;
        private const long AlliedLossWeight = -3;
        private const long AlliedEliminationWeight = -4000;
        private const long RestrictionWeight = 1;
        private const long HazardWeight = -4;
        private const long EngagementStepWeight = 20;
        private const long EngagementPointLimit = 100;

        internal static long Score(in EnemyScoreFacts facts) => Score(facts, EnemyScoreWeights.Default);

        /// <summary>
        /// 교환 모형 점수다(설계 6.5): 지금 공격 + 후속 공격 + 다음 기회 공격 + 접근 진척 − 플레이어 위험 − 환경 위험 − 예보 속박.
        /// 플레이어 위험은 처치당하지 않는 칸이면 위협 손실 × 위협 회피 백분율, 처치당할 수 있는 칸이면 목숨값(죽음 교환 기준) 하나다.
        /// 항목별 값은 <see cref="Terms"/> 하나가 만들고, 점수는 그 합이다(진단 기록과 같은 식).
        /// </summary>
        internal static long Score(in EnemyScoreFacts facts, in EnemyScoreWeights weights) => Terms(facts, weights).Total;

        /// <summary>점수식의 항목별 값이다(설계 6.9 진단 기록). 부호는 점수에 더하는 방향 그대로다.</summary>
        internal static EnemyScoreTerms Terms(in EnemyScoreFacts facts, in EnemyScoreWeights weights) => checked(new EnemyScoreTerms(
            AttackBenefit(facts),
            HazardWeight * facts.ActorHazardLossPermille,
            -PlayerRisk(facts, weights),
            System.Math.Clamp(EngagementStepWeight * facts.EngagementProgress, -EngagementPointLimit, EngagementPointLimit),
            EnemyAiTuning.FollowupWeight * System.Math.Max(0L, facts.SubsequentAttackBenefit),
            weights.NextTurnPercent * System.Math.Max(0L, facts.NextTurnStrike) / 100,
            facts.AdditionalForecastBind ? -RestrictionWeight * RestrictionUnitPermille : 0));

        internal static long PlayerRisk(in EnemyScoreFacts facts, in EnemyScoreWeights weights) => facts.PlayerThreatLethal
            ? weights.DeathTradePermille
            : checked(weights.ThreatPercent * facts.PlayerThreatPermille / 100);

        internal static long AttackBenefit(EnemyScoreFacts facts) => checked(
            OpposingLossWeight * facts.OpponentHpLossPermille +
            OpposingEliminationWeight * facts.OpponentEliminations +
            AlliedLossWeight * (facts.HasUnresolvedAlliedLoss
                ? System.Math.Max(facts.AlliedHpLossPermille, facts.PossibleAlliedHpLossPermille)
                : facts.AlliedHpLossPermille) +
            AlliedEliminationWeight * facts.AlliedEliminations +
            RestrictionWeight * facts.RestrictionDeltaPermille);

        internal static long LossPermille(long loss, int remainingHp, int maxHp) =>
            maxHp <= 0 ? 0 : checked(System.Math.Clamp(loss, 0, remainingHp) * 1000L / maxHp);

        internal static (int tier, long score) Rank(in EnemyScoreFacts facts) => Rank(facts, EnemyScoreWeights.Default);

        internal static (int tier, long score) Rank(in EnemyScoreFacts facts, in EnemyScoreWeights weights) =>
            (facts.GuaranteedTerminal ? facts.ProjectedOutcome == StageOutcome.Defeat ? 1 : -1 : 0, Score(facts, weights));

        internal static int CompareIntent(EnemyDecision left, EnemyDecision right)
        {
            int kind = left.Kind.CompareTo(right.Kind);
            if (kind != 0) return kind;
            if (left.Kind == EnemyDecisionKind.Attack)
            {
                var a = left.Aim.Cell.GetValueOrDefault();
                var b = right.Aim.Cell.GetValueOrDefault();
                return (a.Y, a.X, left.Aim.Direction, left.Aim.Kind).CompareTo((b.Y, b.X, right.Aim.Direction, right.Aim.Kind));
            }
            int endpoint = CompareCell(left.Path.Endpoint, right.Path.Endpoint);
            if (endpoint != 0) return endpoint;
            for (int i = 0; i < System.Math.Min(left.Path.Cells.Count, right.Path.Cells.Count); i++)
            {
                int cell = CompareCell(left.Path.Cells[i], right.Path.Cells[i]);
                if (cell != 0) return cell;
            }
            return left.Path.Cells.Count.CompareTo(right.Path.Cells.Count);
        }

        private static int CompareCell(CellCoord a, CellCoord b) => (a.Y, a.X).CompareTo((b.Y, b.X));

        internal static bool BeatsHold(in EnemyScoreFacts candidate, in EnemyScoreFacts hold) =>
            Rank(candidate).CompareTo(Rank(hold)) > 0;
    }
}

