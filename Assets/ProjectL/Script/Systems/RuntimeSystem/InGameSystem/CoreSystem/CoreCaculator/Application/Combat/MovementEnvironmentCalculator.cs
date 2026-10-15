using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 이동 명령 안의 진입 계산을 조정한다. 규칙에 장부·사망·도착·게시 권한을 주지 않는다.</summary>
    internal static class MovementEnvironmentCalculator
    {
        internal static void ResolveObjectsAt(MovementProjection projection, EntityId arriver, CellCoord cell,
            IReadOnlyList<IMovementTileRule> rules)
        {
            var sources = ObjectArrivalCalculator.OrderedAt(projection.Environment, projection.Bindings, cell);
            foreach (var source in sources)
            {
                var current = projection.Character(arriver);
                if (current.Health.IsDead || current.Area != CharacterRuntimeArea.Field || current.Anchor != cell || projection.Terminal) return;
                var tile = (ObjectSourceFact)projection.Source(source.Id);
                if (!tile.Active) continue;
                var rule = rules.SingleOrDefault(r => r.Key == tile.RuleKey) ??
                    throw new NotSupportedException("Object의 진입 규칙이 연결되지 않았습니다: " + tile.RuleKey);
                var targets = rule.TargetIds(tile, arriver, projection.CurrentInput())
                    ?? throw new InvalidOperationException("Object 후보 대상은 null일 수 없습니다.");
                var effects = rule.Evaluate(projection.StepInput(cell, MovementEffectPhase.Hazard, targets, arriver), tile) ??
                    throw new InvalidOperationException("Object 효과는 null일 수 없습니다.");
                if (effects.Damage.Select(r => r.TargetId).Concat(effects.Healing.Select(r => r.TargetId))
                    .Concat(effects.MovementMethods.Select(r => r.TargetId)).Concat(effects.HeldChanges.Select(r => r.TargetId))
                    .Any(id => !targets.Contains(id)))
                    throw new InvalidOperationException("Object 효과가 선언한 후보 대상 밖을 변경할 수 없습니다.");
                if (effects.ForcedMovement.Count != 0)
                    throw new NotSupportedException("Object 자체의 강제 이동 요청은 아직 지원하지 않습니다.");
                // 원본 변경이 없으면 규칙이 발동하지 않기로 판단한 것이다. 다른 효과 없이 원본을 남긴다.
                if (effects.ExpectedSource == null)
                {
                    if (effects.Damage.Count != 0 || effects.Healing.Count != 0 || effects.MovementMethods.Count != 0 ||
                        effects.HeldChanges.Count != 0)
                        throw new InvalidOperationException("발동하지 않은 Object는 다른 효과를 낼 수 없습니다.");
                    continue;
                }
                if (effects.FinalSource.Active)
                    throw new InvalidOperationException("발동한 Object는 선택된 원본만 비활성화해야 합니다.");
                projection.Events.Add(new CombatObjectActivationEvent(tile, arriver));
                foreach (var held in effects.HeldChanges) projection.ChangeHeld(held);
                projection.ChangeSource(tile, effects);
                var origin = effects.Damage.Count == 0 ? null : new CombatDamageOrigin(cell, null, projection.NextAttackGroup());
                foreach (var damage in effects.Damage) projection.ChangeHealth(damage, false, origin);
                foreach (var method in effects.MovementMethods) projection.ChangeMethod(method);
                projection.ResolveDeathBoundary();
                if (projection.Character(arriver).Health.IsDead || projection.Terminal) return;
                foreach (var heal in effects.Healing) projection.ChangeHealth(heal, true);
            }
        }

        internal static void RequireRules(IReadOnlyList<IMovementTileRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (rules.Any(r => r == null || string.IsNullOrWhiteSpace(r.Key)) ||
                rules.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != rules.Count)
                throw new ArgumentException("규칙 키는 유효하며 중복될 수 없습니다.", nameof(rules));
        }

        internal static void ResolveEntry(MovementProjection projection, CellCoord enteredCell, IReadOnlyList<IMovementTileRule> rules,
            EntityId? mover = null, bool consumeThorn = true)
        {
            var arriver = mover ?? projection.Input.ActorId;
            bool Interrupted() => projection.Character(arriver).Health.IsDead || projection.Terminal ||
                projection.Character(arriver).Area != CharacterRuntimeArea.Field || projection.Character(arriver).Anchor != enteredCell;
            if (Interrupted()) return;
            if (consumeThorn && projection.HasThorn(enteredCell))
            {
                var thorn = RequiredThorn(rules);
                var effects = thorn.EvaluateThorn(projection.StepInput(enteredCell, MovementEffectPhase.Hazard, mover: arriver), projection.Thorn(enteredCell));
                projection.ConsumeThorn(enteredCell);
                var origin = new CombatDamageOrigin(enteredCell, null, projection.NextAttackGroup());
                foreach (var damage in effects.Damage)
                    projection.ChangeHealth(damage, false, origin);
                projection.ResolveDeathBoundary();
            }
            foreach (var phase in new[] { MovementEffectPhase.Hazard, MovementEffectPhase.Recovery })
            {
                if (Interrupted()) return;
                if (phase == MovementEffectPhase.Recovery)
                {
                    ResolveObjectsAt(projection, arriver, enteredCell, rules);
                    if (Interrupted()) return;
                }
                var ids = projection.Environment.Tiles.Where(t => t.Active && t.Cell == enteredCell &&
                        t.TriggersArrivalRule &&
                        !projection.Bindings.ObjectSourceIds.Contains(t.Id))
                    .OrderBy(t => t.Priority).ThenBy(t => t.Id).Select(t => t.Id).ToArray();
                foreach (var id in ids)
                {
                    if (Interrupted()) return;
                    var tile = projection.Source(id);
                    if (!tile.Active) continue;
                    var rule = rules.SingleOrDefault(r => r.Key == tile.RuleKey) ??
                        throw new NotSupportedException("활성 이동 원본의 작성 규칙이 연결되지 않았습니다: " + tile.RuleKey);
                    var targetIds = rule.TargetIds(tile, arriver, projection.CurrentInput()) ??
                        throw new InvalidOperationException("규칙 후보 대상은 null일 수 없습니다.");
                    var effects = rule.Evaluate(projection.StepInput(enteredCell, phase, targetIds, arriver), tile) ??
                        throw new InvalidOperationException("진입 규칙은 null 결과를 반환할 수 없습니다.");
                    if (effects.Damage.Select(r => r.TargetId).Concat(effects.Healing.Select(r => r.TargetId))
                        .Concat(effects.ForcedMovement.Select(r => r.TargetId)).Concat(effects.MovementMethods.Select(r => r.TargetId))
                        .Any(id => !targetIds.Contains(id)))
                        throw new InvalidOperationException("규칙 결과가 선언한 후보 대상 밖을 변경할 수 없습니다.");
                    if (phase == MovementEffectPhase.Hazard && effects.Healing.Count != 0 ||
                        phase == MovementEffectPhase.Recovery && (effects.Damage.Count != 0 || effects.ForcedMovement.Count != 0))
                        throw new InvalidOperationException("위험 및 생존 후 회복 단계의 효과를 섞을 수 없습니다.");
                    if (effects.HeldChanges.Count != 0)
                        throw new NotSupportedException("보유 효과 변경은 진입 순서를 가진 Object 규칙만 지원합니다.");
                    projection.ChangeSource(tile, effects);
                    var origin = effects.Damage.Count == 0 ? null : new CombatDamageOrigin(tile.Cell, null, projection.NextAttackGroup());
                    foreach (var damage in effects.Damage)
                        projection.ChangeHealth(damage, false, origin);
                    foreach (var method in effects.MovementMethods) projection.ChangeMethod(method);
                    projection.ResolveDeathBoundary();
                    if (Interrupted()) return;
                    foreach (var forced in effects.ForcedMovement)
                    {
                        ResolveForcedMovement(projection, forced, rules);
                        if (Interrupted()) return;
                    }
                    foreach (var heal in effects.Healing) projection.ChangeHealth(heal, true);
                }
            }
        }

        private static ThornMovementTileRule RequiredThorn(IReadOnlyList<IMovementTileRule> rules) =>
            rules.OfType<ThornMovementTileRule>().SingleOrDefault() ??
            throw new NotSupportedException("가시밭 진입 규칙이 구성되지 않았습니다.");

        private static void ResolveForcedMovement(MovementProjection projection, MovementForcedMoveRequest request,
            IReadOnlyList<IMovementTileRule> rules)
        {
            if (request.Distance == 0) return;
            if (request.Distance != 1) throw new NotSupportedException("기존 강제 이동 계약은 한 칸입니다.");
            var before = projection.Character(request.TargetId);
            if (before.Health.IsDead || before.Area != CharacterRuntimeArea.Field) return;
            if (before.OccupiedCells.Count != 1) throw new NotSupportedException("다중 셀 강제 이동은 연결되지 않았습니다.");
            var input = projection.CurrentInput();
            var destination = ForcedDisplacementGeometry.ResolveDestinationAnchor(before.Anchor.Value,
                ImpactPatternCalculator.GetForwardStep(request.Direction), request.Distance);
            if (request.Cause == MovementDisplacementCause.Knockback)
            {
                projection.TerrainQueryCells.Add(before.Anchor.Value);
                projection.TerrainQueryCells.Add(destination);
                if (!EnvironmentTerrainRules.CanKnockback(input.State.Environment, input.EnvironmentBindings,
                    before.Anchor.Value, destination)) return;
            }
            if (!input.State.IsInside(destination) ||
                EnvironmentTerrainRules.IsWallOccupied(input.State.Environment, input.EnvironmentBindings, destination) ||
                input.Characters.Any(c => c.EntityId != before.EntityId && !c.Health.IsDead && c.OccupiedCells.Contains(destination)) ||
                input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(destination))) return;
            var departed = PositionTileModifierCalculator.Replace(before, PositionTileModifier.None);
            PositionTileModifierCalculator.RecordNormalization(projection.Events, before, departed);
            var after = CombatMovementCalculator.MoveTo(departed, destination);
            if (after.EntityId != projection.Input.ActorId)
                after = CombatMovementCalculator.SealPendingMovement(after);
            projection.RecordForcedMove(before, after);
            var profile = CombatCommandCalculator.RequiredProfile(input, before);
            foreach (var cell in MovementPathCalculator.ResolvePathCells(before.Anchor.Value, destination,
                new TileFootprint(profile.Definition.FootprintWidth, profile.Definition.FootprintHeight,
                    profile.Definition.FootprintLocalCells.ToArray()), before.Rotation.Value))
            {
                if (!projection.HasThorn(cell)) continue;
                var effect = RequiredThorn(rules).EvaluateThorn(
                    new MovementStepInput(cell, after), projection.Thorn(cell));
                projection.ConsumeThorn(cell);
                var origin = new CombatDamageOrigin(cell, null, projection.NextAttackGroup());
                foreach (var damage in effect.Damage)
                    projection.ChangeHealth(new MovementHealthRequest(before.EntityId, damage.Amount, damage.DamageOrigin),
                        false, origin);
                projection.ResolveDeathBoundary();
                if (projection.Terminal || projection.Character(before.EntityId).Health.IsDead) return;
            }
            ResolveObjectsAt(projection, before.EntityId, destination, rules);
            projection.ResolveForcedArrival(before.EntityId);
        }
    }
}
