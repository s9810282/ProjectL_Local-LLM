using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 한 적 진영 턴 동안 보관하는 계획: 행동마다 "행동 전 판 서명"을 붙인 목록과 마지막 판의 서명.
    /// 세션은 행동마다 새로 생기므로 옆 캐시(<see cref="EnemyAi.EnemyDecisionSideCache"/>)가 이것을 들고 있다(설계 CP4).
    /// </summary>
    internal sealed class EnemyTurnPlan
    {
        private readonly IReadOnlyList<(EnemyDecision decision, string before)> _steps;
        private readonly string _final;

        internal EnemyTurnPlan(IReadOnlyList<(EnemyDecision decision, string before)> steps, string final, EnemyTurnPlanStats stats)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
            _final = final ?? throw new ArgumentNullException(nameof(final));
            Stats = stats;
        }

        internal int Count => _steps.Count;
        /// <summary>이 계획을 만들 때의 수치(측정용, 결정에는 쓰지 않는다).</summary>
        internal EnemyTurnPlanStats Stats { get; }
        internal IReadOnlyList<EnemyDecision> Decisions => _steps.Select(step => step.decision).ToArray();

        /// <summary>
        /// 지금 판 서명에 맞는 다음 행동. 마지막 판이면 적 턴 끝. 맞는 것이 없으면(예측과 다른 판) false.
        /// 행동마다 판이 바뀌므로(이동은 칸, 공격은 행동 단계) 같은 서명이 두 번 나오지 않는다.
        /// </summary>
        internal bool TryNext(string signature, out EnemyDecision decision)
        {
            for (int index = 0; index < _steps.Count; index++)
            {
                if (_steps[index].before != signature) continue;
                decision = _steps[index].decision;
                return true;
            }
            if (signature == _final)
            {
                decision = EnemyDecision.EndEnemyPhase();
                return true;
            }
            decision = null;
            return false;
        }
    }

    /// <summary>
    /// 계획 하나의 수치: 빔에서 돌린 행동 수, 2단 평가에서 돌린 행동 수, 빠른 V로 매긴 판 수, 빔 단계 수,
    /// 예외로 거절로 본 행동 수와 마지막 문구, 다시 매겼는지, 다시 매김이 빠른 V 1등과 다른 계획을 골랐는지, 계획에 걸린 시간.
    /// </summary>
    internal readonly struct EnemyTurnPlanStats
    {
        internal int Simulations { get; }
        internal int ReplySimulations { get; }
        internal int Evaluations { get; }
        internal int BeamSteps { get; }
        internal int Failures { get; }
        internal string LastFailure { get; }
        internal bool Reranked { get; }
        internal bool Flipped { get; }
        /// <summary>계획 계산에 걸린 시간(밀리초, 측정용).</summary>
        internal double ElapsedMilliseconds { get; }

        internal EnemyTurnPlanStats(int simulations, int replySimulations, int evaluations, int beamSteps,
            int failures, string lastFailure, bool reranked, bool flipped, double elapsedMilliseconds)
        {
            Simulations = simulations;
            ReplySimulations = replySimulations;
            Evaluations = evaluations;
            BeamSteps = beamSteps;
            Failures = failures;
            LastFailure = lastFailure;
            Reranked = reranked;
            Flipped = flipped;
            ElapsedMilliseconds = elapsedMilliseconds;
        }
    }
}
