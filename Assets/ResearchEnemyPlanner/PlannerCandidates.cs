using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>유닛 하나가 이번에 할 수 있는 것: (이동) + (공격). 이동 + 공격이면 이동 뒤 판을 함께 든다.</summary>
    internal sealed class PlannerOption
    {
        internal CharacterCombatFacts Unit { get; }
        internal MovementPath Path { get; }
        internal AttackAim? Aim { get; }
        internal EntityId TauntSource { get; }
        /// <summary>
        /// 이동을 마친 판. 이동 + 공격이면 공격을 이 판에서 정산한다. 이동만이면 이미 돌린 그 이동의 결과다(<see cref="MoveSimulated"/>, 거절이면 null).
        /// </summary>
        internal PlannerState Moved { get; }
        /// <summary>이동만 후보인데 이동을 이미 엔진으로 돌렸나(할당 줄이기 AL-3). 그러면 <see cref="Moved"/>를 다시 쓴다.</summary>
        internal bool MoveSimulated { get; }

        internal PlannerOption(CharacterCombatFacts unit, MovementPath path, AttackAim? aim, EntityId tauntSource, PlannerState moved = null,
            bool moveSimulated = false)
        {
            Unit = unit;
            Path = path;
            Aim = aim;
            TauntSource = tauntSource;
            Moved = moved;
            MoveSimulated = moveSimulated;
        }

        internal bool HasAttack => Aim.HasValue;
        internal EnemyDecision MoveDecision => EnemyDecision.Move(Unit.EntityId, Path, default);
        internal EnemyDecision AttackDecision => EnemyDecision.Attack(Unit.EntityId, Aim.Value, TauntSource);
    }

    /// <summary>
    /// 유닛 하나의 후보 행동(설계 CP7). 시뮬레이터 <c>analysis.unit_options</c>와 같은 꼴:
    /// 제자리 공격 전부, 이동만(닿는 칸 전부), 이동 + 공격(도착 칸에서 사거리 + 1 안에 무언가 있을 때).
    /// 도발당한 유닛은 도발한 유닛을 칠 수 있으면 그 공격만, 못 치면 다른 대상 공격을 뺀다(기존 AI의 <see cref="TauntTargeting"/> 규칙).
    /// </summary>
    internal static class PlannerCandidates
    {
        /// <summary>
        /// 적 진영 턴 판(자동 진행 입력)에서 적 유닛 하나의 후보. 이동 + 공격 후보를 만들 때 이동을 엔진으로 돌린다.
        /// 단계마다 한 번 멈춘다(프레임 나눔). 결과는 output에 넣는다.
        /// </summary>
        internal static IEnumerable<int> EnemySteps(PlannerState board, CharacterCombatFacts unit, PlannerSimulator simulator,
            List<PlannerOption> output)
        {
            var input = board.Input;
            var taunters = TauntTargeting.Sources(input, unit).Select(s => s.EntityId).ToArray();
            bool canAttack = (unit.ActionUse == UnitActionUse.Available || unit.ActionUse == UnitActionUse.Moved) &&
                UnitBehaviorRules.CanBasicAttack(input, unit) && !CombatActionAdmission.IsAttackRestricted(unit);
            if (canAttack)
                foreach (var (aim, source) in Aims(input, unit, taunters))
                    output.Add(new PlannerOption(unit, null, aim, source));
            yield return 0;
            foreach (var path in Paths(input, unit))
            {
                if (!canAttack || !TargetsNear(input, unit, path.Endpoint))
                {
                    output.Add(new PlannerOption(unit, path, null, default));
                    continue;
                }
                var moved = simulator.ApplyEnemy(board, EnemyDecision.Move(unit.EntityId, path, default));
                // 이동만 후보도 이 판을 다시 쓴다(같은 이동을 두 번 돌리지 않게). 후보 순서는 전과 같다(이동만 → 그 칸의 공격들).
                output.Add(new PlannerOption(unit, path, null, default, moved, moveSimulated: true));
                yield return 0;
                if (moved == null || moved.IsEnded) continue;
                var mover = moved.Input.Characters.SingleOrDefault(c => c.EntityId == unit.EntityId);
                if (mover == null || !PlannerEvaluator.OnField(mover)) continue;
                foreach (var (aim, source) in Aims(moved.Input, mover, taunters))
                    output.Add(new PlannerOption(unit, path, aim, source, moved));
            }
        }

        /// <summary>
        /// 그 판에서 그 유닛이 고를 수 있는 공격 조준(게임 공격 범위 조회와 같은 입구). 도발 규칙을 적용한다.
        /// 맞는 대상이 하나도 없는 조준(빈 칸을 쏘는 대포 등)은 뺀다. 시뮬레이터 <c>rules.legal_attacks(useful_only=True)</c>와 같은 규칙이다
        /// (별도 스레드·병렬 설계 §12.1 — 이 거름이 빠져 Core 판에서만 헛공격 후보가 생겼다).
        /// </summary>
        internal static IEnumerable<(AttackAim aim, EntityId source)> Aims(CombatCommandInput board, CharacterCombatFacts unit,
            IReadOnlyCollection<EntityId> taunters)
        {
            var query = board.WithActor(unit.EntityId);
            var range = CombatCommandCalculator.GetAttackRange(query);
            if (!range.Succeeded) yield break;
            var aims = range.AttackAims.ToArray();
            var profile = CombatCommandCalculator.RequiredProfile(board, unit);
            var environment = board.State.Environment.VisibleTo(unit.Owner);
            bool taunted = taunters != null && taunters.Count > 0;
            foreach (var aim in aims)
            {
                var evaluation = CombatAttackQueries.Selection(query, unit, profile, aim, environment);
                if (evaluation?.FailureReason != AttackFailureReason.None || evaluation.Selection == null) continue;
                var targets = evaluation.Selection.Targets.Select(t => t.Key);
                if (!taunted)
                {
                    if (targets.Any()) yield return (aim, default);
                    continue;
                }
                var hit = targets.FirstOrDefault(id => taunters.Contains(id));
                if (!hit.IsEmpty) yield return (aim, hit);
            }
        }

        /// <summary>그 유닛이 스스로 걸어 닿는 칸마다 경로(게임 이동 범위와 같은 탐색). 움직일 수 없으면 없음.</summary>
        internal static IEnumerable<MovementPath> Paths(CombatCommandInput board, CharacterCombatFacts unit)
        {
            if (unit.ActionUse != UnitActionUse.Available || CombatActionAdmission.IsMovementRestricted(unit) ||
                !UnitBehaviorRules.CanMoveVoluntarily(board, unit))
                yield break;
            var allowance = CombatActionAdmission.MovementAllowance(board, unit);
            if (!allowance.HasValue) yield break;
            var query = CombatCommandComposition.Intent(board.WithActor(unit.EntityId), CombatCommandKind.Move);
            var search = MovementRangeSearch.Calculate(query, unit, board.State.Environment.VisibleTo(unit.Owner), allowance.Value);
            foreach (var destination in search.Destinations)
            {
                var path = search.BuildRoute(destination);
                if (path != null && path.StepCount > 0) yield return path;
            }
        }

        /// <summary>그 칸에서 칠 수 있는 무언가가 사거리 + 1 안에 있나. 후보를 줄이는 빠른 거름(시뮬레이터 <c>_targets_near</c>).</summary>
        internal static bool TargetsNear(CombatCommandInput board, CharacterCombatFacts unit, CellCoord cell)
        {
            var profile = CombatCommandCalculator.RequiredProfile(board, unit);
            int reach = CombatAttackQueries.SelectionRange(board, unit, profile, board.State.Environment) + 1;
            bool Near(CellCoord c) => Math.Max(Math.Abs((long)c.X - cell.X), Math.Abs((long)c.Y - cell.Y)) <= reach;
            return board.Characters.Any(c => c.EntityId != unit.EntityId && PlannerEvaluator.OnField(c) && c.OccupiedCells.Any(Near)) ||
                board.State.Structures.Any(s => !s.IsDestroyed && s.OccupiedCells.Any(Near));
        }
    }
}
