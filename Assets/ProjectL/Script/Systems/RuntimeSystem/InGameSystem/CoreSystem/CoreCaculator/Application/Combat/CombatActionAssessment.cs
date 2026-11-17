using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공개 입력만 기존 계산에 전달한다. 지원 범위와 예산이 확정되기 전에는 정밀 자식을 만들지 않는다.</summary>
    internal static class CombatActionAssessment
    {
        internal static CombatAssessmentGap GetCoverage(CombatCommandInput input, EnemyDecision candidate, EnemyDecisionContext context)
        {
            using var work = new Work(input, candidate, Array.Empty<IMovementTileRule>(), context, true);
            while (!work.IsComplete) work.Step();
            return work.Facts.Gaps;
        }

        internal static Work Begin(CombatCommandInput input, EnemyDecision candidate,
            IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext context, bool allowPrecise = true) =>
            new Work(input, candidate, rules, context, false, allowPrecise);

        internal static bool TryAssess(CombatCommandInput input, EnemyDecision candidate,
            IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext context,
            out EnemyScoreFacts facts, out CombatCommandInput movedFacts)
        {
            using var work = Begin(input, candidate, rules, context);
            while (!work.IsComplete) work.Step();
            facts = work.Facts;
            movedFacts = work.MovedFacts;
            return work.Admitted;
        }

        internal sealed class Work : IDisposable
        {
            private IEnumerator<int> _steps;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            private bool _admitted;
            private EnemyScoreFacts _facts;
            private CombatCommandInput _moved;
            private CombatAssessmentGap _routeGaps;
            private string _phase = "Begin";
            internal bool IsComplete { get { RequireActive(); return _complete; } }
            internal long WorkVersion { get { RequireActive(); return _version; } }
            internal string Phase { get { RequireActive(); return _phase; } }
            internal bool Admitted { get { RequireComplete(); return _admitted; } }
            internal EnemyScoreFacts Facts { get { RequireComplete(); return _facts; } }
            /// <summary>후속 공격을 위한 공개 이동 투영이다. 값이 있어도 Facts.IsPrecise가 참인 것은 아니다.</summary>
            internal CombatCommandInput MovedFacts { get { RequireComplete(); return _moved; } }
            /// <summary>이동 경로가 만난 도착 원본의 공백이다. 공백이 있으면 기하 후속 공격 추정을 하지 않는다(설계 6.4).</summary>
            internal CombatAssessmentGap RouteGaps { get { RequireComplete(); return _routeGaps; } }

            internal Work(CombatCommandInput input, EnemyDecision candidate, IReadOnlyList<IMovementTileRule> rules,
                EnemyDecisionContext context, bool coverageOnly, bool allowPrecise = true)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                if (candidate == null) throw new ArgumentNullException(nameof(candidate));
                if (rules == null) throw new ArgumentNullException(nameof(rules));
                if (context == null) throw new ArgumentNullException(nameof(context));
                _steps = Run(input, candidate, rules, context, coverageOnly, allowPrecise).GetEnumerator();
            }

            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete) { _steps.Dispose(); _steps = null; }
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _facts = null;
                    _moved = null;
                    throw;
                }
            }

            private IEnumerable<int> Run(CombatCommandInput input, EnemyDecision candidate,
                IReadOnlyList<IMovementTileRule> rules, EnemyDecisionContext context, bool coverageOnly, bool allowPrecise)
            {
                // 관측할 수 없는 환경의 부재를 증명할 계약이 없으므로 항상 같은 불확실성을 보존한다.
                var gaps = CombatAssessmentGap.HiddenEnvironment;
                _phase = "Actor";
                _facts = new EnemyScoreFacts(gaps);
                CharacterCombatFacts actor = null;
                foreach (var unit in input.Characters)
                {
                    if (unit.EntityId == candidate.ActorId) actor = unit;
                    yield return 0;
                }
                if (actor == null || candidate.Kind == EnemyDecisionKind.EndEnemyPhase) yield break;
                MovementEnvironmentFacts visible = null;
                _phase = "Visibility";
                foreach (var _ in context.AssessmentVisibilitySteps(input.State.Environment, actor.Owner, value => visible = value))
                    yield return 0;
                var publicInput = input.WithWorld(input.State.With(environment: visible), input.Characters,
                    input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings).WithActor(candidate.ActorId);
                publicInput = candidate.Kind == EnemyDecisionKind.Attack
                    ? publicInput.WithIntent(CombatCommandKind.Attack, candidate.Aim, default, null, null, sweepSide: null)
                    : publicInput.WithIntent(CombatCommandKind.Move, default, candidate.Path.Endpoint, candidate.Path, null, sweepSide: null);
                yield return 0;
                // 이 공격의 강제 이동이 닿을 수 있는 칸이다. 강제 이동이 없으면 null이다.
                HashSet<CellCoord> forcedReach = null;
                var forcedMovers = new List<CharacterCombatFacts>();
                AttackSelectionEvaluation selection = null;
                _phase = "Admission";
                if (candidate.Kind == EnemyDecisionKind.Attack)
                {
                    if (CombatAttackQueries.Actor(publicInput, out actor) != AttackFailureReason.None) yield break;
                    yield return 0;
                    var profile = CombatCommandCalculator.RequiredProfile(publicInput, actor);
                    foreach (var _ in CombatAttackQueries.SelectionSteps(publicInput, actor, profile, candidate.Aim, visible,
                        null, null, value => selection = value)) yield return 0;
                    if (selection.FailureReason != AttackFailureReason.None) yield break;
                    var bindings = new List<EffectRuleBinding>();
                    foreach (var binding in CombatCommandEffects.CaptureSteps(actor, profile))
                    {
                        if (binding != null) bindings.Add(binding);
                        yield return 0;
                    }
                    int knockback = KnockbackDistance(bindings);
                    if (selection.Pull?.RequiresPull == true || selection.Sweep != null || knockback > 0)
                    {
                        forcedReach = new HashSet<CellCoord>();
                        foreach (var _ in ForcedReachSteps(publicInput, selection, knockback, forcedReach, forcedMovers)) yield return 0;
                    }
                }
                else
                {
                    if (CombatMovementCalculator.Actor(publicInput, out actor) != MovementFailureReason.None) yield break;
                    yield return 0;
                    var reason = MovementFailureReason.None;
                    foreach (var _ in MovementRouteValidator.ValidationSteps(publicInput, candidate.Path, visible,
                        (failure, count) => reason = failure)) yield return 0;
                    if (reason != MovementFailureReason.None || !CombatMovementCalculator.CanLand(publicInput, candidate.Path.Endpoint, visible))
                        yield break;
                }
                _admitted = true;
                yield return 0;
                bool unboundedHarm = false;
                bool pillarCollateral = false;
                bool moveProjectionBlocked = false;
                _phase = "EffectCoverage";
                foreach (var gap in CombatCommandEffects.AssessmentCoverageSteps(publicInput, () => unboundedHarm = true,
                    () => moveProjectionBlocked = true, enemyAssessment: true))
                {
                    gaps |= gap;
                    yield return 0;
                }
                var exposed = new HashSet<EntityId>();
                if (candidate.Kind == EnemyDecisionKind.Attack)
                {
                    // 이동은 구조물 칸에 들어가지 못해 장애물을 건드리지 않는다. 공격만 맞히는 장애물을 본다.
                    _phase = "ObstacleCoverage";
                    foreach (var _ in ObstacleExposureSteps(publicInput, actor, selection, forcedReach, exposed,
                        () => gaps |= CombatAssessmentGap.UnsupportedEffect,
                        MarkPillarCollateral)) yield return 0;
                }
                var routeGaps = CombatAssessmentGap.None;
                _phase = "EnvironmentCoverage";
                if (candidate.Kind == EnemyDecisionKind.Move)
                {
                    foreach (var gap in MovementEnvironmentCalculator.RouteAssessmentCoverageSteps(publicInput, actor, rules,
                        exposed, () => unboundedHarm = true, MarkPillarCollateral))
                    {
                        routeGaps |= gap;
                        gaps |= gap;
                        yield return 0;
                    }
                }
                else if (forcedReach != null)
                {
                    // 강제 이동이 닿는 칸의 도착 원본만 본다(경로 검사와 같은 규칙). 한정할 수 없는 도착이 그 안에 있을 때만 보수적 상한을 남긴다.
                    foreach (var gap in MovementEnvironmentCalculator.ForcedEntryAssessmentCoverageSteps(publicInput, forcedMovers,
                        forcedReach, rules, exposed, () => unboundedHarm = true, MarkPillarCollateral))
                    {
                        gaps |= gap;
                        yield return 0;
                    }
                }
                _routeGaps = routeGaps;
                void MarkPillarCollateral()
                {
                    pillarCollateral = true;
                    unboundedHarm = true;
                    gaps |= CombatAssessmentGap.UnsupportedEffect;
                }

                long possibleHarm = 0;
                _phase = "PossibleHarm";
                if (unboundedHarm || exposed.Count != 0)
                {
                    foreach (var unit in publicInput.Characters)
                    {
                        if (unit.Owner == actor.Owner && !unit.Health.IsDead && unit.Area == CharacterRuntimeArea.Field &&
                            (unboundedHarm || exposed.Contains(unit.EntityId)))
                            possibleHarm = checked(possibleHarm + Loss(unit.Health.CurrentHealth, unit.Health.MaxHealth));
                        yield return 0;
                    }
                    if (pillarCollateral || exposed.Count != 0)
                        foreach (var structure in publicInput.State.Structures)
                        {
                            if (structure.ObstacleDurability == null && structure.Owner == actor.Owner && !structure.IsDestroyed &&
                                (pillarCollateral || exposed.Contains(structure.EntityId)))
                                possibleHarm = checked(possibleHarm + Loss(structure.Health.CurrentHealth, structure.Health.MaxHealth));
                            yield return 0;
                        }
                }
                _facts = new EnemyScoreFacts(gaps, possibleAlliedHpLoss: possibleHarm);
                if (coverageOnly) yield break;
                bool precise = (gaps & ~CombatAssessmentGap.HiddenEnvironment) == CombatAssessmentGap.None;
                bool conditionalMove = candidate.Kind == EnemyDecisionKind.Move && !moveProjectionBlocked &&
                    !unboundedHarm && routeGaps == CombatAssessmentGap.None;
                if (!allowPrecise || !precise && !conditionalMove) yield break;
                if (!context.TryChargePreview(candidate.ActorId))
                {
                    _facts = new EnemyScoreFacts(gaps, budgetExhausted: true, possibleAlliedHpLoss: possibleHarm);
                    yield break;
                }
                CombatCommandTransition transition;
                if (candidate.Kind == EnemyDecisionKind.Attack)
                {
                    using var attack = CombatCommandCalculator.BeginPreview(publicInput, rules);
                    yield return 0;
                    string previousPhase = null;
                    while (!attack.IsComplete)
                    {
                        string phase = attack.Phase;
                        if (phase != previousPhase)
                        {
                            _phase = "Attack/" + phase;
                            previousPhase = phase;
                        }
                        attack.Step();
                        yield return 0;
                    }
                    transition = attack.Transition;
                }
                else
                {
                    using var route = CombatMovementCalculator.BeginProjectRoute(publicInput, rules, true, preservePublic: true);
                    _phase = "MoveRoute";
                    yield return 0;
                    while (!route.IsComplete) { route.Step(); yield return 0; }
                    if (route.Projection == null) { _admitted = false; yield break; }
                    using var completion = route.Projection.BeginComplete();
                    _phase = "MoveCompletion";
                    while (!completion.IsComplete) { completion.Step(); yield return 0; }
                    transition = route.Projection.TransitionFromCompleted(completion.Result);
                }
                if (transition == null || !transition.Accepted) { _admitted = false; yield break; }
                if (!precise)
                {
                    // 간이 이동 점수의 진입 피해는 호출자가 한 번만 계산한다. 투영은 후속 입력에만 사용한다.
                    if (!transition.Outcome.IsStageEnded)
                    {
                        CombatCommandInput moved = null;
                        _phase = "ConditionalMovedInput";
                        foreach (var _ in CombatCommandComposition.ProjectSteps(transition, value => moved = value)) yield return 0;
                        foreach (var unit in moved.Characters)
                        {
                            if (unit.EntityId == actor.EntityId && !unit.Health.IsDead &&
                                unit.Area == CharacterRuntimeArea.Field && unit.Anchor.HasValue)
                                _moved = moved;
                            yield return 0;
                        }
                    }
                    yield break;
                }
                long opponentLoss = 0, alliedLoss = 0, restriction = 0;
                int opponentDeaths = 0, alliedDeaths = 0;
                CellCoord? endpoint = actor.Anchor;
                var postActionActor = actor;
                bool canFollowUp = !transition.Outcome.IsStageEnded;
                _phase = "CharacterFacts";
                foreach (var change in transition.Characters.Characters)
                {
                    var before = change.Expected;
                    var after = change.Final;
                    bool allied = before.Owner == actor.Owner;
                    long loss = Loss(Math.Max(0, before.Health.CurrentHealth - after.Health.CurrentHealth), before.Health.MaxHealth);
                    if (allied) alliedLoss = checked(alliedLoss + loss);
                    else opponentLoss = checked(opponentLoss + loss);
                    if (!before.Health.IsDead && after.Health.IsDead)
                    {
                        if (allied) alliedDeaths++; else opponentDeaths++;
                    }
                    else if (!after.Health.IsDead)
                    {
                        int delta = Restrictions(after) - Restrictions(before);
                        restriction = checked(restriction + (allied ? -1L : 1L) * EnemyUtilityScorer.RestrictionUnitPermille * delta);
                    }
                    if (after.EntityId == actor.EntityId)
                    {
                        endpoint = after.Anchor;
                        postActionActor = after;
                        canFollowUp &= !after.Health.IsDead && after.Area == CharacterRuntimeArea.Field && after.Anchor.HasValue;
                    }
                    yield return 0;
                }
                var finalStructures = new Dictionary<EntityId, StructureCombatFacts>();
                _phase = "StructureFacts";
                foreach (var structure in transition.FinalState.Structures)
                { finalStructures.Add(structure.EntityId, structure); yield return 0; }
                foreach (var before in publicInput.State.Structures)
                {
                    if (before.ObstacleDurability != null || before.Owner == Owner.Neutral) { yield return 0; continue; }
                    int hp = finalStructures.TryGetValue(before.EntityId, out var after) ? after.Health.CurrentHealth : 0;
                    long loss = Loss(Math.Max(0, before.Health.CurrentHealth - hp), before.Health.MaxHealth);
                    if (before.Owner == actor.Owner) alliedLoss = checked(alliedLoss + loss);
                    else opponentLoss = checked(opponentLoss + loss);
                    if (!before.Health.IsDead && hp == 0)
                    {
                        if (before.Owner == actor.Owner) alliedDeaths++; else opponentDeaths++;
                    }
                    yield return 0;
                }
                if (candidate.Kind == EnemyDecisionKind.Move && canFollowUp)
                {
                    _phase = "MovedInput";
                    foreach (var _ in CombatCommandComposition.ProjectSteps(transition, value => _moved = value)) yield return 0;
                }
                _phase = "Seal";
                _facts = new EnemyScoreFacts(gaps, true, opponentHpLoss: opponentLoss, alliedHpLoss: alliedLoss,
                    opponentEliminations: opponentDeaths, alliedEliminations: alliedDeaths, restrictionDelta: restriction,
                    possibleAlliedHpLoss: alliedLoss, endpoint: endpoint,
                    projectedOutcome: transition.Outcome.Outcome, postActionActor: postActionActor);
            }

            /// <summary>
            /// 후보가 건드리는 장애물의 아군 피해 범위다(적 AI 근간 개편 설계 6.2). 아군을 다치게 하는 장애물은 화약통 폭발뿐이다.
            /// 이 공격이 맞히는 화약통과, 강제 이동이 있으면 그 이동이 닿을 수 있는 칸(<paramref name="forcedReach"/>)의 화약통에서 시작해,
            /// 폭발 칸 안의 다른 화약통으로 연쇄를 따라간다.
            /// 폭발 칸 안의 살아 있는 아군 유닛·구조물을 <paramref name="exposed"/>에 넣고, 건드린 화약통이 있으면 <paramref name="touched"/>를 부른다.
            /// 폭발 칸은 실제 반응과 같은 원본(<see cref="CombatHazardAreas.PowderBarrelCells"/>)이다.
            /// </summary>
            private static IEnumerable<int> ObstacleExposureSteps(CombatCommandInput input, CharacterCombatFacts actor,
                AttackSelectionEvaluation selection, ISet<CellCoord> forcedReach, HashSet<EntityId> exposed,
                Action touched, Action pillarReachable)
            {
                var barrelIds = new HashSet<EntityId>();
                var pillarIds = new HashSet<EntityId>();
                foreach (var profile in input.ObstacleContents)
                {
                    if (profile.Kind == ObstacleContentKind.PowderBarrel) barrelIds.Add(profile.EntityId);
                    if (profile.Kind == ObstacleContentKind.Pillar) pillarIds.Add(profile.EntityId);
                    yield return 0;
                }
                if (barrelIds.Count == 0 && pillarIds.Count == 0) yield break;
                var barrels = new List<StructureCombatFacts>();
                var pillars = new List<StructureCombatFacts>();
                foreach (var structure in input.State.Structures)
                {
                    if (!structure.IsDestroyed)
                    {
                        if (barrelIds.Contains(structure.EntityId)) barrels.Add(structure);
                        if (pillarIds.Contains(structure.EntityId)) pillars.Add(structure);
                    }
                    yield return 0;
                }
                var hit = new HashSet<EntityId>();
                if (selection?.Selection != null)
                    foreach (var target in selection.Selection.Targets)
                    {
                        hit.Add(target.Key);
                        yield return 0;
                    }
                foreach (var pillar in pillars)
                {
                    // 낙하·연쇄 방향을 간이 점수에서는 재계산하지 않는다. 노출되면 아군 피해 상한을 유지한다.
                    if (hit.Contains(pillar.EntityId) || forcedReach != null && pillar.OccupiedCells.Any(forcedReach.Contains))
                        MarkPillar();
                    yield return 0;
                }
                var queue = new Queue<StructureCombatFacts>();
                var reached = new HashSet<EntityId>();
                foreach (var barrel in barrels)
                {
                    bool seed = hit.Contains(barrel.EntityId) || forcedReach != null && barrel.OccupiedCells.Any(forcedReach.Contains);
                    if (seed && reached.Add(barrel.EntityId)) queue.Enqueue(barrel);
                    yield return 0;
                }
                if (queue.Count == 0) yield break;
                touched();
                var blast = new HashSet<CellCoord>();
                while (queue.Count != 0)
                {
                    var source = queue.Dequeue();
                    var cells = new HashSet<CellCoord>();
                    CombatHazardAreas.PowderBarrelCells(source.Anchor, input.State.BoardWidth, input.State.BoardHeight, cells);
                    blast.UnionWith(cells);
                    foreach (var barrel in barrels)
                    {
                        if (!reached.Contains(barrel.EntityId) && barrel.OccupiedCells.Any(cells.Contains) && reached.Add(barrel.EntityId))
                            queue.Enqueue(barrel);
                        yield return 0;
                    }
                }
                foreach (var pillar in pillars)
                {
                    if (pillar.OccupiedCells.Any(blast.Contains)) MarkPillar();
                    yield return 0;
                }
                foreach (var unit in input.Characters)
                {
                    if (unit.Owner == actor.Owner && unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead &&
                        unit.OccupiedCells.Any(blast.Contains)) exposed.Add(unit.EntityId);
                    yield return 0;
                }
                foreach (var structure in input.State.Structures)
                {
                    if (structure.Owner == actor.Owner && !structure.Health.IsDead && structure.OccupiedCells.Any(blast.Contains))
                        exposed.Add(structure.EntityId);
                    yield return 0;
                }

                void MarkPillar()
                {
                    touched();
                    pillarReachable();
                }
            }

            // 실제 공격과 같은 평가기로 넉백 거리를 구한다(AttackKnockbackCalculator: 기여 중 가장 먼 거리). 넉백이 없으면 0이다.
            private static int KnockbackDistance(IReadOnlyList<EffectRuleBinding> bindings)
            {
                var knockback = new CombatEffectEngine().EvaluateKnockback(bindings);
                return knockback.Contributions.Count == 0 ? 0 : knockback.Contributions.Max(contribution => contribution.DistanceCells);
            }

            /// <summary>
            /// 강제 이동이 닿을 수 있는 칸과 이동할 수 있는 유닛이다. 당기기는 계산된 경로(대상 발자국 포함)이고,
            /// 쓸기는 포착한 대상에서 행의 목적지까지 보드 안의 칸이다.
            /// 넉백은 맞는 대상의 칸에서 넉백 거리 안(체비셰프)이다. 넉백 방향은 다시 계산하지 않고 거리로 넉넉히 잡는다
            /// (방향 규칙은 실제 공격 계산 한 곳에 둔다). 실제 충돌·도착 결과는 정밀 미리보기가 센다.
            /// </summary>
            private static IEnumerable<int> ForcedReachSteps(CombatCommandInput input, AttackSelectionEvaluation selection, int knockback,
                ISet<CellCoord> reach, List<CharacterCombatFacts> movers)
            {
                var pull = selection.Pull;
                if (pull?.RequiresPull == true)
                {
                    var pulled = input.Characters.FirstOrDefault(unit => unit.EntityId == pull.TargetId);
                    if (pulled != null && pulled.Anchor.HasValue) movers.Add(pulled);
                    foreach (var anchor in pull.Path.Append(pull.DestinationAnchor))
                    {
                        if (pulled == null || !pulled.Anchor.HasValue) reach.Add(anchor);
                        else
                            foreach (var cell in pulled.OccupiedCells)
                                reach.Add(new CellCoord(anchor.X + cell.X - pulled.Anchor.Value.X, anchor.Y + cell.Y - pulled.Anchor.Value.Y));
                        yield return 0;
                    }
                }
                if (selection.Sweep != null)
                    foreach (var _ in DeadHandAttackCalculator.PotentialEntrySteps(input, selection.Sweep, reach, movers))
                        yield return 0;
                if (knockback <= 0 || selection.Selection == null) yield break;
                foreach (var target in selection.Selection.Targets)
                {
                    IReadOnlyList<CellCoord> cells = null;
                    var unit = input.Characters.FirstOrDefault(c => c.EntityId == target.Key);
                    if (unit != null && unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead)
                    {
                        cells = unit.OccupiedCells;
                        if (!movers.Contains(unit)) movers.Add(unit);
                    }
                    else
                    {
                        var structure = input.State.Structures.FirstOrDefault(s => s.EntityId == target.Key);
                        if (structure != null && !structure.Health.IsDead) cells = structure.OccupiedCells;
                    }
                    if (cells != null)
                        foreach (var cell in cells)
                            for (int dx = -knockback; dx <= knockback; dx++)
                            for (int dy = -knockback; dy <= knockback; dy++)
                            {
                                var near = new CellCoord(cell.X + dx, cell.Y + dy);
                                if (input.State.IsInside(near)) reach.Add(near);
                            }
                    yield return 0;
                }
            }

            private static long Loss(int amount, int maxHealth) => maxHealth <= 0 ? 0 : checked(amount * 1000L / maxHealth);
            private static int Restrictions(CharacterCombatFacts unit) =>
                (CombatActionAdmission.IsAttackRestricted(unit) ? 1 : 0) + (CombatActionAdmission.IsMovementRestricted(unit) ? 1 : 0);
            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(Work));
                if (_faulted) throw new InvalidOperationException("실패한 평가 작업은 재개할 수 없습니다.");
            }
            private void RequireComplete()
            {
                RequireActive();
                if (!_complete) throw new InvalidOperationException("평가가 아직 완료되지 않았습니다.");
            }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _facts = null;
                _moved = null;
            }
        }
    }
}

