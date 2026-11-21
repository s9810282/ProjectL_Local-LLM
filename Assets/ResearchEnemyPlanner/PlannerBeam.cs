using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 빔 탐색(설계 CP7). 연구 시뮬레이터 <c>PlannerPolicy._make_plan</c>과 같은 절차:
    /// 노드 = (판, 아직 안 움직인 유닛, 지금까지 행동). 단계마다 남은 유닛의 후보를 엔진으로 돌려 V로 매기고,
    /// 같은 판은 V가 가장 높은 것 하나만, 상위 K개만 다음 단계로 넘긴다. 시작 판("아무것도 안 함")도 후보다.
    /// 다시 매김 후보 모음(<see cref="Pool"/>)은 시작 판 + 단계마다 남긴 판(서명이 같으면 처음 것)이다.
    /// 계획을 고를 때(<see cref="Best"/> 갱신, 다시 매김 후보·깊은 값 비교)는 판 값에서 행동 규칙 벌점(<see cref="IdleCost"/>)을 뺀 값으로 견준다.
    /// 빔 안에서 판을 줄 세우는 값은 판 값 그대로다.
    /// </summary>
    internal sealed class PlannerBeam
    {
        internal sealed class Node
        {
            internal PlannerState State { get; }
            internal IReadOnlyCollection<int> Remaining { get; }
            /// <summary>[(행동, 그 뒤 판 서명)]</summary>
            internal IReadOnlyList<(EnemyDecision decision, string after)> Steps { get; }
            internal double Value { get; }

            internal Node(PlannerState state, IReadOnlyCollection<int> remaining, IReadOnlyList<(EnemyDecision, string)> steps, double value)
            {
                State = state;
                Remaining = remaining;
                Steps = steps;
                Value = value;
            }
        }

        private readonly PlannerState _root;
        private readonly PlannerEvaluator _evaluator;
        private readonly PlannerSimulator _simulator;

        internal Node Start { get; private set; }
        internal Node Best { get; private set; }
        internal List<Node> Pool { get; } = new List<Node>();
        internal int BeamSteps { get; private set; }

        internal PlannerBeam(PlannerState root, PlannerEvaluator evaluator, PlannerSimulator simulator)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        }

        /// <summary>
        /// 행동 규칙 벌점(시뮬레이터 <c>idle_cost</c>): 그 판에서 아직 이동도 공격도 하지 않은 우리 유닛마다
        /// 행동 규칙 값(<see cref="EnemyTurnPlannerStyle.MustAct"/>) × 공격력 × 최대 HP(메인은 × 메인 배수).
        /// 이번 턴에 이미 이동한 유닛과 필드에 없는 유닛은 세지 않는다. 값이 0이면 0이다(끔).
        /// 뜻: 행동해서 판 값이 유닛 전력 × 행동 규칙 값보다 많이 떨어질 때만 제자리에 있는다.
        /// </summary>
        internal double IdleCost(Node node)
        {
            double rate = _evaluator.Style.MustAct;
            if (rate <= 0) return 0.0;
            double total = 0.0;
            var characters = node.State.Input.Characters;
            foreach (int id in node.Remaining)
            {
                var unit = characters.SingleOrDefault(c => c.EntityId.Value == id);
                if (unit == null || !PlannerEvaluator.OnField(unit) || unit.ActionUse != UnitActionUse.Available) continue;
                double power = (double)unit.Stats.EffectiveAttackPower * unit.Health.MaxHealth;
                total += rate * (PlannerEvaluator.IsMain(unit) ? power * _evaluator.Style.MainWeight : power);
            }
            return total;
        }

        /// <summary>계획을 고를 때의 값: 빠른 판 값 − 행동 규칙 벌점(시뮬레이터 <c>_plan_value</c>).</summary>
        internal double PlanValue(Node node) => node.Value - IdleCost(node);

        internal IEnumerable<int> Run()
        {
            var movable = PlannerEvaluator.Living(_root.Input, Owner.Enemy)
                .Where(u => u.ActionUse == UnitActionUse.Available || u.ActionUse == UnitActionUse.Moved)
                .Select(u => u.EntityId.Value).ToArray();
            Start = new Node(_root, movable, Array.Empty<(EnemyDecision, string)>(), _evaluator.Value(_root));
            Best = Start;
            Pool.Add(Start);
            var seen = new HashSet<string> { PlannerBoard.Signature(_root.Input) };
            var frontier = new List<Node> { Start };
            yield return 0;
            while (frontier.Count > 0)
            {
                var children = new Dictionary<string, Node>();
                var order = new List<string>();
                foreach (var node in frontier)
                {
                    if (node.State.IsEnded) continue;
                    foreach (int id in node.Remaining.OrderBy(id => id))
                    {
                        var unit = node.State.Input.Characters.SingleOrDefault(c => c.EntityId.Value == id);
                        if (unit == null || !PlannerEvaluator.OnField(unit)) continue;
                        var scored = new List<(PlannerOption option, PlannerState after, double value, string moved)>();
                        foreach (var _ in CandidateSteps(node.State, unit, scored)) yield return 0;
                        var remaining = node.Remaining.Where(r => r != id).ToArray();
                        foreach (var (option, after, value, moved) in scored)
                        {
                            var steps = new List<(EnemyDecision, string)>(node.Steps);
                            string afterSignature = PlannerBoard.Signature(after.Input);
                            if (option.Path != null && option.HasAttack) steps.Add((option.MoveDecision, moved));
                            steps.Add((option.HasAttack ? option.AttackDecision : option.MoveDecision, afterSignature));
                            string key = afterSignature + (after.IsEnded ? "#" + after.Outcome : "");
                            if (!children.TryGetValue(key, out var existing))
                            {
                                order.Add(key);
                                children[key] = new Node(after, remaining, steps, value);
                            }
                            else if (existing.Value < value) children[key] = new Node(after, remaining, steps, value);
                        }
                    }
                }
                // 같은 값이면 먼저 만든 것(시뮬레이터의 안정 정렬과 같다).
                frontier = order.Select(k => children[k]).OrderByDescending(n => n.Value).Take(_evaluator.Style.Beam).ToList();
                if (frontier.Count > 0) BeamSteps++;
                foreach (var node in frontier)
                    if (seen.Add(PlannerBoard.Signature(node.State.Input) + (node.State.IsEnded ? "#" + node.State.Outcome : "")))
                        Pool.Add(node);
                // 깊이가 같은 판은 남은 유닛 수가 같아 행동 규칙 벌점도 거의 같다. 깊이끼리 견줄 때 벌점을 뺀 값으로 본다.
                if (frontier.Count > 0 && PlanValue(frontier[0]) > PlanValue(Best)) Best = frontier[0];
                if (frontier.Count > 0 && frontier[0].State.IsEnded) break;
                yield return 0;
            }
        }

        /// <summary>
        /// 유닛 하나의 확장 후보: 공격이 든 조합 전부 + 이동만은 빠른 이동 점수 상위 몇 개 → V 상위 N개.
        /// 시뮬레이터 <c>_candidates</c>와 같은 꼴. 결과 = (옵션, 뒤 판, V, 이동 + 공격이면 이동 뒤 판 서명).
        /// </summary>
        private IEnumerable<int> CandidateSteps(PlannerState board, CharacterCombatFacts unit,
            List<(PlannerOption, PlannerState, double, string)> output)
        {
            var options = new List<PlannerOption>();
            foreach (var _ in PlannerCandidates.EnemySteps(board, unit, _simulator, options)) yield return 0;
            var attacks = options.Where(o => o.HasAttack).ToList();
            var moves = options.Where(o => !o.HasAttack)
                .Select(o => (option: o, score: _evaluator.QuickMoveScore(board.Input, unit, o.Path.Endpoint)))
                .OrderByDescending(p => p.score).Take(_evaluator.Style.MoveOnly).Select(p => p.option);
            var scored = new List<(PlannerOption, PlannerState, double, string)>();
            foreach (var option in attacks.Concat(moves))
            {
                PlannerState after;
                string moved = null;
                if (option.HasAttack && option.Moved != null)
                {
                    after = _simulator.ApplyEnemy(option.Moved, option.AttackDecision);
                    moved = PlannerBoard.Signature(option.Moved.Input);
                }
                else if (option.MoveSimulated) after = option.Moved;
                else after = _simulator.ApplyEnemy(board, option.HasAttack ? option.AttackDecision : option.MoveDecision);
                if (after != null) scored.Add((option, after, _evaluator.Value(after), moved));
                yield return 0;
            }
            output.AddRange(scored.OrderByDescending(s => s.Item3).Take(_evaluator.Style.PerUnit));
        }
    }
}
