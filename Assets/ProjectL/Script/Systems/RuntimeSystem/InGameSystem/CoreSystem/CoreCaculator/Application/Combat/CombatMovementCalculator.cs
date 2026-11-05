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
            var distances = Reach(input, actor, environment, CombatActionAdmission.MovementAllowance(input, actor).Value);
            return MovementRangeResult.Success(input.ActorId, distances.Where(p => p.Value > 0 && CanLand(input, p.Key, environment))
                .OrderBy(p => p.Key.Y).ThenBy(p => p.Key.X)
                .Select(p => new MovementDestination(p.Key, new[] { p.Key }, p.Value)).ToArray());
        }

        /// <summary>
        /// 필드 유닛 하나가 지금 자리에서 스스로 걸어 닿을 수 있는 칸이다(자기 칸 제외, Y·X 순서, 호버 이동 범위 설계 EM1).
        /// 차례·국면·비용·행동 사용은 보지 않고, 유닛이 스스로 움직일 수 있는지와 진입 규칙만 본다. 움직일 수 없으면 빈 목록이다.
        /// 지형 시점은 부르는 쪽이 넘긴다(유닛 조회는 플레이어 시점). 칸 탐색은 행동 범위(<see cref="Range"/>)와 같은 <see cref="Reach"/>다.
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
            return Reach(query, unit, environment, allowance.Value).Where(p => p.Value > 0 && CanLand(query, p.Key, environment)).Select(p => p.Key)
                .OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
        }

        /// <summary>
        /// 자리에서 네 방향으로 한 칸씩 나아가 <paramref name="allowance"/> 걸음 안에 닿는 칸과 그 걸음 수다(자리 = 0).
        /// 지나갈 수 있는지는 <see cref="MovementRouteValidator.CanTraverse"/>가 정하고, 멈출 수 있는지(<see cref="MovementRouteValidator.CanEnter"/>)는
        /// 부르는 쪽이 거른다. 행동 범위와 유닛 이동 칸 조회가 함께 쓴다.
        /// 적 AI의 길 찾기(<c>EnemyMovementPathfinder</c>)는 따로 탐색하며 아는 지뢰 칸을 빼고 실제 지형을 쓴다. 그래서 유닛 이동 칸에는
        /// 적이 실제로는 고르지 않는 칸이 들어 있을 수 있다(규칙상 닿는 칸, 호버 이동 범위 계획 §7.6 L3).
        /// </summary>
        private static Dictionary<CellCoord, int> Reach(CombatCommandInput input, CharacterCombatFacts actor,
            MovementEnvironmentFacts environment, int allowance)
        {
            var distances = new Dictionary<CellCoord, int> { [actor.Anchor.Value] = 0 };
            var queue = new Queue<CellCoord>();
            queue.Enqueue(actor.Anchor.Value);
            var directions = new[] { CellCoord.Right, CellCoord.Up, CellCoord.Left, CellCoord.Down };
            while (queue.Count != 0)
            {
                var current = queue.Dequeue();
                int distance = distances[current];
                if (distance == allowance) continue;
                foreach (var direction in directions)
                {
                    var cell = current + direction;
                    if (distances.ContainsKey(cell)) continue;
                    if (!MovementRouteValidator.CanTraverse(input, cell, false, environment))
                    {
                        if (CanLand(input, cell, environment)) distances.Add(cell, distance + 1);
                        continue;
                    }
                    distances.Add(cell, distance + 1);
                    queue.Enqueue(cell);
                }
            }
            return distances;
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            var projection = ProjectRoute(input, rules, false, out var reason, out _);
            return projection == null ? CombatCommandCalculator.Reject(input, reason.ToString()) : projection.Transition();
        }

        internal static MovementProjection ProjectRoute(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool preview, out MovementFailureReason failure, out int validStepCount, CombatProfileReadSet profileReads = null)
        {
            validStepCount = 0;
            failure = Actor(input, out var actor, profileReads: profileReads);
            if (failure != MovementFailureReason.None) return null;
            MovementPath path;
            if (input.MovementContext != null)
            {
                path = input.MovementContext.Path;
                if (!MovementPathNormalizer.MatchesDraft(input.Path, path))
                { failure = MovementFailureReason.StaleContext; return null; }
            }
            else
            {
                failure = MovementPathNormalizer.TryNormalize(input, input.Path, actor.Owner, out path);
                if (failure != MovementFailureReason.None) return null;
            }
            // 모서리는 공개 기하로 고정한다. 숨은 효과는 그 경로를 실행할 때만 해석한다.
            failure = MovementRouteValidator.Validate(input, path, out validStepCount, input.State.Environment.VisibleTo(actor.Owner));
            if (failure != MovementFailureReason.None) return null;
            if (!CanLand(input, path.Endpoint, input.State.Environment.VisibleTo(actor.Owner), profileReads))
            { failure = MovementFailureReason.DestinationOccupied; return null; }
            var normalizedInput = input.WithIntent(input.Kind, input.Aim, path.Endpoint, path, input.MovementContext);
            var expectedPassenger = MovementRouteValidator.SwapPassenger(normalizedInput, path.Endpoint);
            var projection = new MovementProjection(normalizedInput, preview, profileReads);
            projection.BeginOrdinaryMove();
            projection.ReplacePositionModifier(PositionTileModifier.None);
            bool leap = LeapTraitRule.AllowsIntermediate(actor) && CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor, profileReads))
                .Any(binding => binding.Rule is LeapEffectRule rule && rule.AllowsIntermediate(new EffectRuleInput(binding.Origin, binding.ExpectedState), actor));
            var reconContacts = new HashSet<Contracts.Ids.EntityId>();
            for (int i = 1; i < path.Cells.Count; i++)
            {
                if (i == path.Cells.Count - 1 && expectedPassenger != null &&
                    (MovementRouteValidator.SwapPassenger(projection.CurrentInput(), path.Endpoint)?.EntityId != expectedPassenger.EntityId ||
                     projection.Character(expectedPassenger.EntityId).Anchor != expectedPassenger.Anchor ||
                     projection.Character(expectedPassenger.EntityId).Rotation != expectedPassenger.Rotation))
                { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                if (!MovementRouteValidator.CanTraverse(projection.CurrentInput(), path.Cells[i], i == path.Cells.Count - 1, projection.Environment.VisibleTo(actor.Owner)))
                { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                if (i == 1) ReconSquadApplication.Scan(projection, path.Origin, reconContacts);
                if (leap && i < path.Cells.Count - 1)
                {
                    var visible = projection.Environment.VisibleTo(actor.Owner);
                    if (!MovementRouteValidator.CanEnter(projection.CurrentInput(), path.Cells[i], visible) ||
                        visible.Tiles.Any(t => t.Active && t.IsMine && t.Cell == path.Cells[i]))
                        CombatCommandEvents.Trait(projection.Events, actor.EntityId, TraitKey.Leap, TraitActivationKind.MovementChanged,
                            1, new[] { actor.EntityId }, new[] { path.Cells[i] });
                }
                var passenger = i == path.Cells.Count - 1 ? MovementRouteValidator.SwapPassenger(projection.CurrentInput(), path.Cells[i]) : null;
                if (passenger != null)
                {
                    if (!CanLand(projection.CurrentInput(), path.Cells[i], projection.Environment.VisibleTo(actor.Owner), profileReads))
                    { projection.Stop(MovementStopReason.NextStepInvalid); break; }
                    projection.ExchangeEndpoint(passenger, path.Cells[i]);
                    foreach (var id in new[] { actor.EntityId, passenger.EntityId }.OrderBy(id => id))
                    {
                        if (projection.Terminal) break;
                        var arrival = projection.Character(id);
                        if (arrival.Health.IsDead || arrival.Area != CharacterRuntimeArea.Field) continue;
                        var cell = id == actor.EntityId ? path.Endpoint : path.Origin;
                        if (arrival.Anchor != cell) continue;
                        WeakGroundCalculator.RegisterEntry(projection, id, cell);
                        MovementEnvironmentCalculator.ResolveEntry(projection, cell, rules, mover: id);
                        if (!projection.Terminal && !projection.Character(id).Health.IsDead && projection.Character(id).Anchor == cell &&
                            (!projection.ArrivalCells.TryGetValue(id, out var settled) || settled != cell))
                        {
                            if (id == actor.EntityId) projection.ResolvePathArrival();
                            else projection.ResolveForcedArrival(id);
                        }
                    }
                }
                else
                {
                    projection.Enter(path.Cells[i]);
                    WeakGroundCalculator.RegisterEntry(projection, input.ActorId, path.Cells[i]);
                    MovementEnvironmentCalculator.ResolveEntry(projection, path.Cells[i], rules, skipMines: leap && i < path.Cells.Count - 1);
                }
                ReconSquadApplication.Scan(projection, path.Cells[i], reconContacts);
                if (projection.Actor.Stats.MoveMethod != MoveMethod.LiftAndPlace)
                    projection.Stop(MovementStopReason.MovementBlocked);
                if (projection.StopReason != MovementStopReason.Completed) break;
            }
            if (leap && !projection.Actor.Health.IsDead && projection.Actor.Area == CharacterRuntimeArea.Field &&
                !MovementRouteValidator.CanEnter(projection.CurrentInput(), projection.Actor.Anchor.Value, projection.Environment))
            { failure = MovementFailureReason.DestinationOccupied; return null; }
            projection.ResolvePathArrival();
            if (projection.DistanceSpent > 0 && !projection.Actor.Health.IsDead && !projection.Terminal)
            {
                var bindings = CombatCommandEffects.Capture(projection.Actor, CombatCommandCalculator.RequiredProfile(input, projection.Actor, profileReads));
                var direction = CombatCommandCalculator.AttackRotation(AttackAim.ForCell(path.Cells[projection.DistanceSpent]),
                    path.Cells[projection.DistanceSpent - 1]);
                var effect = new CombatEffectEngine().PlanMoveSucceeded(bindings,
                    new MoveSucceededEffectContext(projection.DistanceSpent,true,direction));
                CombatCommandEvents.Effects(projection.Events, effect);
                var before = projection.Actor;
                var after = CharacterSettlementCalculator.Copy(before, before.Health, CombatCommandEffects.Apply(before.Stats, effect));
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
            return projection;
        }

        internal static CharacterCombatFacts MoveTo(CharacterCombatFacts actor, CellCoord cell) =>
            actor.WithPlacement(actor.Area, actor.ReserveSlotIndex, cell, actor.Rotation, new[] { cell });

        private static bool CanLand(CombatCommandInput input, CellCoord cell, MovementEnvironmentFacts environment,
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
