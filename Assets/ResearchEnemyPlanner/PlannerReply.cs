using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 깊은 판 평가 V_deep(설계 CP8). 연구 시뮬레이터 <c>sim/reply.py</c>와 같은 절차:
    /// 위험 판 = 적 턴을 끝내고 플레이어 턴을 연 판, 기회 판 = 거기서 플레이어가 아무것도 안 하고 턴을 넘긴 판.
    /// 그 편 유닛마다 공격이 든 후보를 엔진으로 돌려 순이득(빼앗은 자산 − 잃은 자산)이 가장 큰 것 하나를 고르고(0 이하면 없음),
    /// 고른 행동들의 대상별 잃은 HP를 더해 HP에서 자른 손실이 위험·기회다. 자산·G1은 빠른 V와 같다.
    /// 턴을 넘긴 판은 난수를 같은 씨앗으로 다시 맞춘다.
    /// </summary>
    internal static class PlannerReply
    {
        internal static IEnumerable<int> DeepSteps(PlannerState state, PlannerEvaluator evaluator, PlannerSimulator simulator,
            uint seed, Action<double> completed)
        {
            if (state.IsEnded)
            {
                completed(PlannerEvaluator.Terminal(state.Outcome));
                yield break;
            }
            var board = state.Input;
            double total = evaluator.Assets(board, Owner.Enemy) - evaluator.Assets(board, Owner.Player);
            double risk = 0.0, gain = 0.0, penalty = 0.0;
            var riskBoard = TurnToPlayer(state, simulator, seed);
            yield return 0;
            if (riskBoard != null && !riskBoard.IsEnded && IsPlayerInput(riskBoard.Input))
            {
                var command = PlannerBoard.Rebuild(riskBoard.Input, CombatProcessingKind.Command, CombatCommandKind.EndPlayerTurn);
                var losses = new List<Dictionary<long, int>>();
                foreach (var _ in ReplySteps(command, Owner.Player, evaluator, simulator, losses, null)) yield return 0;
                risk = CombinedLoss(command, losses, evaluator);
                var chanceBoard = TurnToEnemy(command, simulator, seed);
                yield return 0;
                if (chanceBoard != null && !chanceBoard.IsEnded && IsEnemyTurn(chanceBoard.Input))
                {
                    var gains = new List<Dictionary<long, int>>();
                    var idle = new List<int>();
                    foreach (var _ in ReplySteps(chanceBoard.Input, Owner.Enemy, evaluator, simulator, gains, idle)) yield return 0;
                    gain = CombinedLoss(chanceBoard.Input, gains, evaluator);
                    penalty = StepPenalty(chanceBoard.Input, idle, evaluator);
                }
            }
            total += -EnemyTurnPlannerConfig.Fear * risk + EnemyTurnPlannerConfig.Opportunity * gain - penalty;
            completed(total + evaluator.GoalTerm(board));
        }

        private static bool IsPlayerInput(CombatCommandInput input) =>
            input.State.Phase == CombatFlowPhase.PlayerInput && input.State.Turn.ControllerPhase == CombatControllerPhase.Player;

        private static bool IsEnemyTurn(CombatCommandInput input) =>
            input.State.Phase == CombatFlowPhase.Automatic && input.State.Turn.ControllerPhase == CombatControllerPhase.Enemy;

        /// <summary>적 턴을 끝내고 플레이어 턴 시작까지 엔진으로 넘긴 판. 거절되면 null.</summary>
        internal static PlannerState TurnToPlayer(PlannerState state, PlannerSimulator simulator, uint seed)
        {
            var ended = simulator.ApplyEnemy(state, EnemyDecision.EndEnemyPhase());
            if (ended == null) return null;
            var board = new PlannerState(PlannerBoard.Reseed(ended.Input, seed), ended.Outcome);
            return AdvanceStarting(board, CombatControllerPhase.StartingPlayer, simulator, seed);
        }

        /// <summary>플레이어 턴(명령 입력)을 끝내고 적 턴 시작까지 엔진으로 넘긴 판. 거절되면 null.</summary>
        internal static PlannerState TurnToEnemy(CombatCommandInput command, PlannerSimulator simulator, uint seed)
        {
            var ended = simulator.ApplyCommand(command);
            if (ended == null) return null;
            if (ended.IsEnded) return ended;
            var advance = PlannerBoard.Rebuild(PlannerBoard.Reseed(ended.Input, seed), CombatProcessingKind.Advance,
                CombatCommandKind.StartControllerTurn);
            return AdvanceStarting(new PlannerState(advance, ended.Outcome), CombatControllerPhase.StartingEnemy, simulator, seed);
        }

        /// <summary>그 진영의 시작 단계(자동 진행)를 엔진으로 넘긴다. 진영 시작 효과(재생·증원 등)가 엔진 순서대로 들어간다.</summary>
        private static PlannerState AdvanceStarting(PlannerState board, CombatControllerPhase starting, PlannerSimulator simulator, uint seed)
        {
            for (int guard = 0; guard < 4; guard++)
            {
                if (board.IsEnded) return board;
                var input = board.Input;
                if (input.State.Phase != CombatFlowPhase.Automatic || input.State.Turn.ControllerPhase != starting) return board;
                if (input.ProcessingKind != CombatProcessingKind.Advance)
                    input = PlannerBoard.Rebuild(input, CombatProcessingKind.Advance, CombatCommandKind.StartControllerTurn);
                var next = simulator.ApplyEnemy(new PlannerState(input, board.Outcome), EnemyDecision.EndEnemyPhase());
                if (next == null) return null;
                board = new PlannerState(PlannerBoard.Reseed(next.Input, seed), next.Outcome);
            }
            return board;
        }

        /// <summary>
        /// side 유닛마다(번호 순) 가장 좋은 공격 응수 하나의 "상대 편 대상별 잃은 HP"를 losses에 넣는다. 공격 후보가 하나도 없는 유닛은 idle.
        /// board는 side 차례여야 한다(플레이어 = 명령 입력, 적 = 자동 진행 입력).
        /// </summary>
        private static IEnumerable<int> ReplySteps(CombatCommandInput board, Owner side, PlannerEvaluator evaluator,
            PlannerSimulator simulator, List<Dictionary<long, int>> losses, List<int> idle)
        {
            var opponent = PlannerEvaluator.Other(side);
            double ownBefore = evaluator.Assets(board, side);
            double opponentBefore = evaluator.Assets(board, opponent);
            foreach (var unit in PlannerEvaluator.Living(board, side).ToArray())
            {
                var results = new List<PlannerState>();
                foreach (var _ in AttackResultSteps(board, unit, side, simulator, results)) yield return 0;
                if (results.Count == 0)
                {
                    idle?.Add(unit.EntityId.Value);
                    continue;
                }
                PlannerState best = null;
                double bestGain = 0.0;
                foreach (var after in results)
                {
                    double gain = (opponentBefore - evaluator.Assets(after.Input, opponent)) - (ownBefore - evaluator.Assets(after.Input, side));
                    if (gain > 0 && (best == null || gain > bestGain))
                    {
                        best = after;
                        bestGain = gain;
                    }
                }
                if (best != null) losses.Add(LostHealth(board, best.Input, opponent));
            }
        }

        /// <summary>공격이 들어 있는 조합(제자리 공격, 이동 + 공격)마다 엔진으로 돌린 뒤 판. 시뮬레이터 <c>attack_results</c>와 같은 꼴.</summary>
        private static IEnumerable<int> AttackResultSteps(CombatCommandInput board, CharacterCombatFacts unit, Owner side,
            PlannerSimulator simulator, List<PlannerState> output)
        {
            bool canAttack = (unit.ActionUse == UnitActionUse.Available || unit.ActionUse == UnitActionUse.Moved) &&
                UnitBehaviorRules.CanBasicAttack(board, unit) && !CombatActionAdmission.IsAttackRestricted(unit);
            if (!canAttack) yield break;
            foreach (var (aim, _) in PlannerCandidates.Aims(board, unit, null))
            {
                var after = Attack(board, unit, aim, side, simulator);
                if (after != null) output.Add(after);
                yield return 0;
            }
            foreach (var path in PlannerCandidates.Paths(board, unit))
            {
                if (!PlannerCandidates.TargetsNear(board, unit, path.Endpoint)) continue;
                var moved = side == Owner.Enemy
                    ? simulator.ApplyEnemy(new PlannerState(board, StageOutcome.InProgress), EnemyDecision.Move(unit.EntityId, path, default))
                    : simulator.ApplyCommand(CombatCommandComposition.Intent(board.WithActor(unit.EntityId), CombatCommandKind.Move,
                        cell: path.Endpoint, path: path));
                yield return 0;
                if (moved == null || moved.IsEnded) continue;
                var mover = moved.Input.Characters.SingleOrDefault(c => c.EntityId == unit.EntityId);
                if (mover == null || !PlannerEvaluator.OnField(mover)) continue;
                foreach (var (aim, _) in PlannerCandidates.Aims(moved.Input, mover, null))
                {
                    var after = Attack(moved.Input, mover, aim, side, simulator);
                    if (after != null) output.Add(after);
                    yield return 0;
                }
            }
        }

        /// <summary>그 편 차례 판에서 공격 하나를 정산한다(적 = 게임과 같은 자동 진행 입구, 플레이어 = 명령 입력). 거절이면 null.</summary>
        private static PlannerState Attack(CombatCommandInput board, CharacterCombatFacts unit, AttackAim aim, Owner side,
            PlannerSimulator simulator) => side == Owner.Enemy
            ? simulator.ApplyEnemy(new PlannerState(board, StageOutcome.InProgress), EnemyDecision.Attack(unit.EntityId, aim, default))
            : simulator.ApplyCommand(CombatCommandComposition.Intent(board.WithActor(unit.EntityId), CombatCommandKind.Attack, aim));

        /// <summary>before → after에서 side 편 대상(필드의 산 유닛·적 본진)이 잃은 HP. 죽거나 떨어지면 남은 HP 전부.</summary>
        private static Dictionary<long, int> LostHealth(CombatCommandInput before, CombatCommandInput after, Owner side)
        {
            var lost = new Dictionary<long, int>();
            foreach (var unit in PlannerEvaluator.Living(before, side))
            {
                var later = after.Characters.SingleOrDefault(c => c.EntityId == unit.EntityId);
                int remaining = later == null || !PlannerEvaluator.OnField(later) ? 0 : later.Health.CurrentHealth;
                int amount = unit.Health.CurrentHealth - remaining;
                if (amount > 0) lost[unit.EntityId.Value] = amount;
            }
            if (side == Owner.Enemy)
                foreach (var hq in PlannerEvaluator.Headquarters(before))
                {
                    var later = after.State.Structures.SingleOrDefault(s => s.EntityId == hq.EntityId);
                    int remaining = later == null || later.IsDestroyed ? 0 : later.Health.CurrentHealth;
                    int amount = hq.Health.CurrentHealth - remaining;
                    if (amount > 0) lost[-hq.EntityId.Value] = amount;
                }
            return lost;
        }

        /// <summary>응수들의 대상별 잃은 HP를 더하고, board에서 그 대상의 HP로 잘라 자산 손실로 바꾼 합.</summary>
        private static double CombinedLoss(CombatCommandInput board, IEnumerable<Dictionary<long, int>> losses, PlannerEvaluator evaluator)
        {
            var totals = new SortedDictionary<long, int>();
            foreach (var lost in losses)
                foreach (var pair in lost)
                    totals[pair.Key] = (totals.TryGetValue(pair.Key, out var sum) ? sum : 0) + pair.Value;
            double total = 0.0;
            foreach (var pair in totals)
            {
                if (pair.Key > 0)
                {
                    var unit = board.Characters.SingleOrDefault(c => c.EntityId.Value == pair.Key);
                    if (unit != null) total += evaluator.Loss(new PlannerTarget(unit), pair.Value);
                }
                else
                {
                    var hq = board.State.Structures.SingleOrDefault(s => s.EntityId.Value == -pair.Key);
                    if (hq != null) total += evaluator.Loss(new PlannerTarget(hq), pair.Value);
                }
            }
            return total;
        }

        /// <summary>공격 후보가 없는 우리 유닛: 칠 대상 옆까지 걸음 중 이동·사거리로 못 메우는 만큼 벌점(빠른 V와 같은 식).</summary>
        private static double StepPenalty(CombatCommandInput board, IReadOnlyCollection<int> idle, PlannerEvaluator evaluator)
        {
            var targets = PlannerEvaluator.Targets(board, Owner.Enemy);
            if (idle.Count == 0 || targets.Count == 0) return 0.0;
            var field = evaluator.DistanceField(board, targets);
            double penalty = 0.0;
            foreach (int id in idle)
            {
                var unit = board.Characters.SingleOrDefault(c => c.EntityId.Value == id);
                if (unit != null) penalty += EnemyTurnPlannerConfig.StepPenalty * PlannerEvaluator.Beyond(unit, field);
            }
            return penalty;
        }
    }
}
