using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>거리와 행동 사용 규칙 안에서 한 번의 완결된 직교 이동을 계산한다.</summary>
    public static class CombatMovementCalculator
    {
        public static MovePreview Preview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            var projection = ProjectRoute(input, rules, true, out var reason, out int validStepCount);
            if (projection == null) return MovePreview.Failure(reason, validStepCount);
            var final = projection.Transition();
            var facts = CombatCommandComposition.Project(final);
            var projectedActor = facts.Characters.Single(c => c.EntityId == input.ActorId);
            return MovePreview.Success(projection.PathResult(),projectedActor.Stats.TraitStatuses.ToArray(),projectedActor.Stats.Category,                projectedActor,final.Events);
        }

        public static MovementRangeResult Range(CombatCommandInput input)
        {
            var reason = Actor(input, out var actor);
            if (reason != MovementFailureReason.None) return MovementRangeResult.Failure(reason, input.ActorId);
            reason = MovementRouteValidator.ValidateActorPlacement(actor);
            if (reason != MovementFailureReason.None) return MovementRangeResult.Failure(reason, input.ActorId);
            if (!input.State.IsInside(actor.Anchor.Value))
                return MovementRangeResult.Failure(MovementFailureReason.OutOfBounds, input.ActorId);
            var environment = input.State.Environment.VisibleTo(actor.Owner);
            var search = MovementRangeSearch.Calculate(input, actor, environment,
                CombatActionAdmission.MovementAllowance(input, actor).Value);
            return MovementRangeResult.Success(input.ActorId, search.Destinations.Select(cell => search.BuildRoute(cell)).ToArray());
        }

        /// <summary>
        /// 필드 유닛 하나가 지금 자리에서 스스로 걸어 닿을 수 있는 칸이다(자기 칸 제외, Y·X 순서, 호버 이동 범위 설계 EM1).
        /// 차례·국면·비용·행동 사용은 보지 않고, 유닛이 스스로 움직일 수 있는지와 진입 규칙만 본다. 움직일 수 없으면 빈 목록이다.
        /// 지형 시점은 부르는 쪽이 넘긴다(유닛 조회는 플레이어 시점). 칸 탐색은 행동 범위(<see cref="Range"/>)와 같은 <see cref="MovementRangeSearch"/>를 쓴다.
        /// 적 AI는 인지한 지뢰 등을 고려해 따로 경로를 고르므로 조회된 칸을 실제로 선택하지 않을 수 있다.
        /// </summary>
        internal static CellCoord[] UnitMoveCells(CombatCommandInput input, CharacterCombatFacts unit, MovementEnvironmentFacts environment)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            int? allowance = CombatActionAdmission.MovementAllowance(input, unit);
            if (unit.Area != CharacterRuntimeArea.Field || unit.Health.IsDead || !allowance.HasValue ||
                unit.Stats.MoveMethod != MoveMethod.LiftAndPlace || !UnitBehaviorRules.CanMoveVoluntarily(input, unit) ||
                MovementRouteValidator.ValidateActorPlacement(unit) != MovementFailureReason.None ||
                !input.State.IsInside(unit.Anchor.Value))
                return Array.Empty<CellCoord>();
            var query = input.WithActor(unit.EntityId).WithIntent(CombatCommandKind.Move, default, default, null, null);
            return MovementRangeSearch.Calculate(query, unit, environment, allowance.Value).Destinations.ToArray();
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            var projection = ProjectRoute(input, rules, false, out var reason, out _);
            return projection == null ? CombatCommandCalculator.Reject(input, reason.ToString()) : projection.Transition();
        }

        internal static RouteWork BeginProjectRoute(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool preview, CombatProfileReadSet profileReads = null, bool preservePublic = false) =>
            new RouteWork(input, rules, preview, profileReads, preservePublic);

        internal static bool IsLeapActive(CombatCommandInput input, CharacterCombatFacts actor,
            CombatProfileReadSet profileReads = null) =>
            LeapTraitRule.AllowsIntermediate(actor) && CombatCommandEffects.Capture(actor,
                CombatCommandCalculator.RequiredProfile(input, actor, profileReads))
                .Any(binding => binding.Rule is LeapEffectRule rule &&
                    rule.AllowsIntermediate(new EffectRuleInput(binding.Origin, binding.ExpectedState), actor));

        internal sealed class RouteWork : IDisposable
        {
            private IEnumerator<int> _steps;
            private MovementProjection _projection;
            private MovementFailureReason _failure;
            private int _validSteps;
            private long _version;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            internal bool IsComplete { get { RequireActive(); return _complete; } }
            internal long WorkVersion { get { RequireActive(); return _version; } }
            internal MovementProjection Projection { get { RequireComplete(); return _projection; } }
            internal MovementFailureReason Failure { get { RequireComplete(); return _failure; } }
            internal int ValidStepCount { get { RequireComplete(); return _validSteps; } }

            internal RouteWork(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules, bool preview, CombatProfileReadSet profileReads, bool preservePublic)
            {
                if (input == null) throw new ArgumentNullException(nameof(input));
                if (rules == null) throw new ArgumentNullException(nameof(rules));
                _steps = Build(input, rules, preview, profileReads, preservePublic).GetEnumerator();
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
                    _projection = null;
                    throw;
                }
            }
            private IEnumerable<int> Build(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
                bool preview, CombatProfileReadSet profileReads, bool preservePublic)
            {
                _validSteps = 0;
                yield return 0;
                _failure = Actor(input, out var actor, profileReads: profileReads);
                if (_failure != MovementFailureReason.None) yield break;
                MovementPath path;
                if (input.MovementContext != null)
                {
                    path = input.MovementContext.Path;
                    if (!MovementPathNormalizer.MatchesDraft(input.Path, path))
                    { _failure = MovementFailureReason.StaleContext; yield break; }
                }
                else
                {
                    _failure = MovementPathNormalizer.TryNormalize(input, input.Path, actor.Owner, out path);
                    if (_failure != MovementFailureReason.None) yield break;
                }
                // 모서리는 공개 기하로 고정한다. 숨은 효과는 그 경로를 실행할 때만 해석한다.
                MovementEnvironmentFacts visible = null;
                foreach (var _ in input.State.Environment.VisibleSteps(actor.Owner, value => visible = value, preservePublic)) yield return 0;
                foreach (var _ in MovementRouteValidator.ValidationSteps(input, path, visible,
                    (reason, count) => { _failure = reason; _validSteps = count; })) yield return 0;
                if (_failure != MovementFailureReason.None) yield break;
                if (!CanLand(input, path.Endpoint, visible, profileReads))
                { _failure = MovementFailureReason.DestinationOccupied; yield break; }
                var normalizedInput = input.WithIntent(input.Kind, input.Aim, path.Endpoint, path, input.MovementContext);
                var expectedPassenger = MovementRouteValidator.SwapPassenger(normalizedInput, path.Endpoint);
                var projection = new MovementProjection(normalizedInput, preview, profileReads, preview ? visible : null, rules);
                yield return 0;
                projection.BeginOrdinaryMove();
                projection.ReplacePositionModifier(PositionTileModifier.None);
                bool leap = IsLeapActive(input, actor, profileReads);
                var reconContacts = new HashSet<Contracts.Ids.EntityId>();
                for (int i = 1; i < path.Cells.Count; i++)
                {
                    CombatCommandInput current = null;
                    if (i == path.Cells.Count - 1 && expectedPassenger != null)
                    {
                        foreach (var _ in Observe(projection, value => current = value)) yield return 0;
                        if (MovementRouteValidator.SwapPassenger(current, path.Endpoint)?.EntityId != expectedPassenger.EntityId ||
                            projection.Character(expectedPassenger.EntityId).Anchor != expectedPassenger.Anchor ||
                            projection.Character(expectedPassenger.EntityId).Rotation != expectedPassenger.Rotation)
                        { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                    }
                    foreach (var _ in Observe(projection, value => current = value)) yield return 0;
                    foreach (var _ in projection.Environment.VisibleSteps(actor.Owner, value => visible = value)) yield return 0;
                    if (!MovementRouteValidator.CanTraverse(current, path.Cells[i], i == path.Cells.Count - 1, visible))
                    { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                    if (i == 1)
                        foreach (var _ in ReconSquadApplication.ScanSteps(projection, path.Origin, reconContacts)) yield return 0;
                    bool skipsMineEntry = LeapTraitRule.SkipsMineEntry(leap, i == path.Cells.Count - 1);
                    if (skipsMineEntry)
                    {
                        foreach (var _ in projection.Environment.VisibleSteps(actor.Owner, value => visible = value)) yield return 0;
                        foreach (var _ in Observe(projection, value => current = value)) yield return 0;
                        if (!MovementRouteValidator.CanEnter(current, path.Cells[i], visible) ||
                            visible.Tiles.Any(t => t.Active && t.IsMine && t.Cell == path.Cells[i]))
                            CombatCommandEvents.Trait(projection.Events, actor.EntityId, TraitKey.Leap, TraitActivationKind.MovementChanged,
                                1, new[] { actor.EntityId }, new[] { path.Cells[i] });
                    }
                    CharacterCombatFacts passenger = null;
                    if (i == path.Cells.Count - 1)
                    {
                        foreach (var _ in Observe(projection, value => current = value)) yield return 0;
                        passenger = MovementRouteValidator.SwapPassenger(current, path.Cells[i]);
                    }
                    if (passenger != null)
                    {
                        foreach (var _ in Observe(projection, value => current = value)) yield return 0;
                        foreach (var _ in projection.Environment.VisibleSteps(actor.Owner, value => visible = value)) yield return 0;
                        if (!CanLand(current, path.Cells[i], visible, profileReads))
                        { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                        projection.ExchangeEndpoint(passenger, path.Cells[i]);
                        yield return 0;
                        foreach (var id in new[] { actor.EntityId, passenger.EntityId }.OrderBy(id => id))
                        {
                            if (projection.Terminal) break;
                            var arrival = projection.Character(id);
                            if (arrival.Health.IsDead || arrival.Area != CharacterRuntimeArea.Field) continue;
                            var cell = id == actor.EntityId ? path.Endpoint : path.Origin;
                            if (arrival.Anchor != cell) continue;
                            WeakGroundCalculator.RegisterEntry(projection, id, cell);
                            foreach (var _ in MovementEnvironmentCalculator.ResolveEntrySteps(projection, cell, rules, mover: id)) yield return 0;
                            if (!projection.Terminal && !projection.Character(id).Health.IsDead && projection.Character(id).Anchor == cell &&
                                (!projection.ArrivalCells.TryGetValue(id, out var settled) || settled != cell))
                            {
                                if (id == actor.EntityId)
                                {
                                    foreach (var _ in projection.ResolvePathArrivalSteps()) yield return 0;
                                }
                                else
                                {
                                    foreach (var _ in projection.ResolveForcedArrivalSteps(id, null)) yield return 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        projection.Enter(path.Cells[i]);
                        WeakGroundCalculator.RegisterEntry(projection, input.ActorId, path.Cells[i]);
                        yield return 0;
                        foreach (var _ in MovementEnvironmentCalculator.ResolveEntrySteps(projection, path.Cells[i], rules,
                            skipMines: skipsMineEntry)) yield return 0;
                    }
                    foreach (var _ in ReconSquadApplication.ScanSteps(projection, path.Cells[i], reconContacts)) yield return 0;
                    if (projection.Actor.Stats.MoveMethod != MoveMethod.LiftAndPlace)
                        projection.Stop(MovementStopReason.MovementBlocked);
                    if (projection.StopReason != MovementStopReason.Completed) break;
                    yield return 0;
                }
                if (leap && !projection.Actor.Health.IsDead && projection.Actor.Area == CharacterRuntimeArea.Field)
                {
                    CombatCommandInput finalPosition = null;
                    foreach (var _ in Observe(projection, value => finalPosition = value)) yield return 0;
                    if (!MovementRouteValidator.CanEnter(finalPosition, projection.Actor.Anchor.Value, projection.Environment))
                    { _failure = MovementFailureReason.DestinationOccupied; yield break; }
                }
                foreach (var _ in projection.ResolvePathArrivalSteps()) yield return 0;
                if (projection.DistanceSpent > 0 && !projection.Actor.Health.IsDead && !projection.Terminal)
                {
                    var before = projection.Actor;
                    var after = ApplyMoveSucceeded(input, before, path, projection.DistanceSpent, out var effect, profileReads);
                    CombatCommandEvents.Effects(projection.Events, effect);
                    int gain = AccelerationTraitRule.Held(after.Stats) - AccelerationTraitRule.Held(before.Stats);
                    if (gain > 0) CombatCommandEvents.Trait(projection.Events, before.EntityId, TraitKey.Acceleration,
                        TraitActivationKind.AttackGranted, gain, new[] { before.EntityId }, before.OccupiedCells);
                    bool moved = before.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().Any(t => t.MovedThisOwnTurn);
                    bool movedAfter = after.Stats.TraitStatuses.ToArray().OfType<StationaryFireTraitStatusSnapshot>().Any(t => t.MovedThisOwnTurn);
                    if (moved != movedAfter) CombatCommandEvents.Trait(projection.Events, before.EntityId, TraitKey.StationaryFire,
                        TraitActivationKind.StatusApplied, 1, new[] { before.EntityId }, before.OccupiedCells);
                    projection.ReplaceActor(after);
                    CombatCommandEvents.TraitState(projection.Events, before, after);
                }
                _projection = projection;
                yield return 0;
            }

            private static IEnumerable<int> Observe(MovementProjection projection, Action<CombatCommandInput> completed)
            {
                using var input = projection.BeginCurrentInput();
                while (!input.IsComplete) { input.Step(); yield return 0; }
                completed(input.Result);
            }
            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(RouteWork));
                if (_faulted) throw new InvalidOperationException("실패한 경로 계산은 다시 진행할 수 없습니다.");
            }
            private void RequireComplete()
            {
                RequireActive();
                if (!_complete) throw new InvalidOperationException("경로 계산이 끝나지 않았습니다.");
            }
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _projection = null;
            }
        }

        internal static MovementProjection ProjectRoute(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool preview, out MovementFailureReason failure, out int validStepCount, CombatProfileReadSet profileReads = null)
        {
            using var work = BeginProjectRoute(input, rules, preview, profileReads);
            while (!work.IsComplete) work.Step();
            failure = work.Failure;
            validStepCount = work.ValidStepCount;
            return work.Projection;
        }

        internal static CharacterCombatFacts MoveTo(CharacterCombatFacts actor, CellCoord cell) =>
            actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, cell, actor.Rotation, new[] { cell });

        /// <summary>
        /// 자발 이동을 마친 행위자에게 이동 성공 효과(정지 사격의 이번 턴 이동 표시, 가속 공격력 등)를 적용한다.
        /// 경로 완료(<see cref="RouteWork"/>)와 적 AI의 기하 후속 공격 추정이 같은 규칙을 쓴다.
        /// </summary>
        internal static CharacterCombatFacts ApplyMoveSucceeded(CombatCommandInput input, CharacterCombatFacts actor, MovementPath path,
            int distanceSpent, out CombatEffectPlan effect, CombatProfileReadSet profileReads = null)
        {
            if (distanceSpent <= 0 || distanceSpent > path.StepCount) throw new ArgumentOutOfRangeException(nameof(distanceSpent));
            var bindings = CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor, profileReads));
            var direction = CombatCommandCalculator.AttackRotation(AttackAim.ForCell(path.Cells[distanceSpent]), path.Cells[distanceSpent - 1]);
            effect = new CombatEffectEngine().PlanMoveSucceeded(bindings, new MoveSucceededEffectContext(distanceSpent, true, direction));
            return CharacterSettlementCalculator.Copy(actor, actor.Health, CombatCommandEffects.Apply(actor.Stats, effect));
        }

        /// <summary>
        /// 경로를 끝까지 걸은 행위자의 사실이다: 종점 자리, 도착 칸 보정, 행동 사용 <see cref="UnitActionUse.Moved"/>(경로 시작의
        /// <c>MovementProjection.BeginOrdinaryMove</c>와 같다), 이동 성공 효과(<see cref="ApplyMoveSucceeded"/>).
        /// 경로 위 도착 연쇄(지뢰·약한 지반 등)는 넣지 않는다. 그런 경로는 부르는 쪽이 따로 거른다.
        /// </summary>
        internal static CharacterCombatFacts MovedActor(CombatCommandInput input, CharacterCombatFacts actor, MovementPath path,
            MovementEnvironmentFacts environment)
        {
            var departed = PositionTileModifierCalculator.Replace(actor, PositionTileModifier.None);
            var moved = MoveTo(departed, path.Endpoint).With(actionUse: UnitActionUse.Moved);
            moved = PositionTileModifierCalculator.Replace(moved, PositionTileModifierCalculator.ResolveAt(path.Endpoint, environment));
            return path.StepCount == 0 ? moved : ApplyMoveSucceeded(input, moved, path, path.StepCount, out _);
        }

        internal static bool CanLand(CombatCommandInput input, CellCoord cell, MovementEnvironmentFacts environment,
            CombatProfileReadSet profileReads = null)
        {
            if (!MovementRouteValidator.CanLand(input, cell, environment)) return false;
            var passenger = MovementRouteValidator.SwapPassenger(input, cell);
            if (passenger == null) return true;
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            if (UnitBehaviorRules.IsForcedMovementImmune(input, passenger, profileReads) ||
                CombatCommandCalculator.RequiredProfile(input, passenger, profileReads).Definition.FootprintLocalCells.Length != 1 ||
                CombatCommandCalculator.RequiredProfile(input, actor, profileReads).Definition.FootprintLocalCells.Length != 1) return false;
            return CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor, profileReads)).Any(binding =>
                binding.Rule is PositionSwapEffectRule rule && rule.AllowsExchange(new EffectRuleInput(binding.Origin, binding.ExpectedState), actor, passenger));
        }

        internal static MovementFailureReason Actor(CombatCommandInput input, out CharacterCombatFacts actor,
            bool requireMovement = true, CombatProfileReadSet profileReads = null)
        {
            var failure = CombatActionAdmission.Actor(input, CombatCommandKind.Move, out actor);
            if (failure != MovementFailureReason.None) return failure;
            if (requireMovement && actor.Stats.MoveMethod != MoveMethod.LiftAndPlace) return MovementFailureReason.UnsupportedMoveMethod;
            if (requireMovement && (!UnitBehaviorRules.CanMoveVoluntarily(input, actor, profileReads) ||
                !CombatActionAdmission.MovementAllowance(input, actor).HasValue)) return MovementFailureReason.VoluntaryMoveDisabled;
            if (actor.OccupiedCells.Count != 1) return MovementFailureReason.StateInconsistent;
            return MovementFailureReason.None;
        }

        internal static long Distance(CellCoord a, CellCoord b) => Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y);
    }
}
