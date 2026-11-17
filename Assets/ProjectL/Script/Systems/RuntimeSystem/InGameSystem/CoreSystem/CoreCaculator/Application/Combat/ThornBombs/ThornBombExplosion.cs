using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.ThornBombs
{
    /// <summary>한 폭발의 전체 피해를 확정한 뒤 넉백·도착을 끝낸다. 다음 폭발은 호출자의 FIFO가 실행한다.</summary>
    internal static class ThornBombExplosion
    {
        internal static IEnumerable<string> ExecuteSteps(MovementProjection projection, ThornBombSnapshot bomb,
            ThornBombTrigger trigger, ThornBombResolution bombs, IReadOnlyList<IMovementTileRule> rules)
        {
            var center = bombs.Center(projection, bomb);
            var origin = new CombatDamageOrigin(center, bomb.SourceId, projection.NextAttackGroup());
            var evidence = new CombatThornBombDetonatedEvent(bomb, center, trigger, origin.AttackGroup);
            projection.Events.Add(evidence);
            int damage = ThornBombRules.CalculateBaseDamage(bomb.CapturedAttackPower, bomb.DamagePercent);
            var targets = projection.Damage.Characters.Values.Where(c => !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field &&
                c.OccupiedCells.Any(cell => ThornBombRules.IsInBlast(cell, center)))
                .OrderBy(c => c.EntityId).Select(c => c.EntityId).ToArray();
            var hits = new List<(EntityId TargetId, int Damage)>();
            var knockbacks = new List<AttackKnockbackSnapshot>();
            var occupied = new Dictionary<EntityId, IReadOnlyList<CellCoord>>();
            var cause = new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.ThornBomb);
            // 승패 검사는 전체 피해 뒤에만 한다. 공통 HP 경계 자체는 Terminal을 검사하지 않는다.
            foreach (var id in targets)
            {
                bombs.TrySchedule(id, ThornBombTrigger.ChainExplosion);
                var target = projection.Character(id);
                int healthLoss = 0;
                int numeric = CombatDamageCalculator.Environment(projection.Input, damage, origin, target.Anchor.Value,
                    projection.Environment).FinalDamage;
                projection.EffectTargetIds.Add(id);
                foreach (var phase in projection.Damage.ApplySteps(id, CombatDamageKind.TraitExplosion, origin, numeric, result => healthLoss = result.ActualHealthLoss,
                    deferTraitKills: true, numericInput: numeric, heldDefenseGroups: projection.AttackGroups,
                    effectOrigin: TraitEffectOrigin.Create(bomb.SourceId, TraitKey.ThornBomb), contextEvent: evidence))
                    yield return phase;
                hits.Add((id, healthLoss));
            }
            bombs.ObserveDeaths(projection);
            foreach (var id in targets)
            {
                if (projection.Terminal) break;
                var target = projection.Character(id);
                if (!ImpactCenterGeometry.TryResolveStep(target, center, out var step)) continue;
                var start = target.Anchor.Value;
                // 미리보기 도착 칸 표시용 시작 점유 칸이다. 같은 폭발의 앞선 대상이 민 뒤의 위치다.
                occupied.Add(id, target.OccupiedCells.ToArray());
                if (KnockbackImmunityCheck.IsImmune(projection.Input, target, out bool steadfast))
                {
                    if (steadfast)
                        CombatCommandEvents.Trait(projection.Events, id, TraitKey.SteadfastWill,
                            TraitActivationKind.KnockbackPrevented, 1, new[] { id }, target.OccupiedCells, origin);
                    knockbacks.Add(new AttackKnockbackSnapshot(id, AttackKnockbackTargetKind.Character, start, step,
                        ThornBombRules.KnockbackDistance, Array.Empty<CellCoord>(), AttackKnockbackStop.Immune));
                    continue;
                }
                ForcedDisplacementPath path = null;
                var request = new ForcedMovementRequest(id, step, ThornBombRules.KnockbackDistance, cause,
                    deferLastObjects: false, publishMovedEvents: true);
                foreach (var phase in ForcedMovementExecution.ExecuteSteps(projection, request, rules, result => path = result.Path))
                    yield return phase;
                knockbacks.Add(path.ToKnockback(id, AttackKnockbackTargetKind.Character, start, step));
                if (path.Steps.Count > 0 && path.Stop != ForcedDisplacementStop.Interrupted &&
                    !projection.Character(id).Health.IsDead && projection.Character(id).Anchor == path.Steps[path.Steps.Count - 1])
                    foreach (var phase in projection.ResolveForcedArrivalSteps(id, _ => { })) yield return phase;
            }
            var state = projection.Input.State;
            bombs.Record(new ThornBombExplosionPreview(bomb, center, trigger, hits, knockbacks,
                ThornBombRules.BlastCells(center, state.BoardWidth, state.BoardHeight), occupied));
            projection.ResolveDeathBoundary();
            yield return "ThornBombExplosionComplete";
        }
    }
}
