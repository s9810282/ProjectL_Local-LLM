using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공격과 특성 폭발이 공유하는 칸별 강제 이동이다. 공격별 기록은 호출자가 정산한다.</summary>
    internal static class ForcedMovementExecution
    {
        internal static IEnumerable<string> ExecuteSteps(MovementProjection arrival, ForcedMovementRequest request,
            IReadOnlyList<IMovementTileRule> rules, Action<ForcedMovementResult> completed)
        {
            var input = arrival.Input;
            var id = request.TargetId;
            var step = request.Step;
            var distance = request.Distance;
            var movementCause = request.Cause;
            int normalizationLoss = 0;
            var projected = arrival.Damage.Characters;
            var victim = projected[id];
            bool departed = false;
            yield return "AttackForcedPathSetup";

            using var path = ForcedDisplacementPathCalculator.ExecuteSteps(victim.Anchor.Value, step, distance,
                CanEnter, Enter, value => completed(new ForcedMovementResult(value, normalizationLoss))).GetEnumerator();
            while (true)
            {
                bool progressed;
                using (CoreProfiling.Sample(CoreProfileSection.AttackForcedPathStep)) progressed = path.MoveNext();
                if (!progressed) yield break;
                yield return path.Current;
            }

            IEnumerable<string> CanEnter(CellCoord from, CellCoord to, Action<bool> admitted)
            {
                // 앞선 이동과 같은 공격에서 새로 생긴 지형을 반영한 현재 점유·환경·바인딩으로 다음 칸을 검사한다.
                admitted(AttackForcedMovement.CanEnterUnit(input, arrival.Environment, arrival.Bindings, projected.Values, arrival.CurrentStructures,
                    id, from, to));
                yield return "AttackForcedAdmission";
            }

            IEnumerable<string> Enter(CellCoord from, CellCoord to, bool last, Action<bool> continued)
            {
                var current = projected[id];
                if (!departed)
                {
                    var normalized = PositionTileModifierCalculator.Replace(current, PositionTileModifier.None);
                    PositionTileModifierCalculator.RecordNormalization(arrival.Events, current, normalized, arrival.Damage.HealthExecutions);
                    normalizationLoss = checked(normalizationLoss + current.Health.CurrentHealth - normalized.Health.CurrentHealth);
                    current = normalized;
                    departed = true;
                }
                var relocated = CombatCommandCalculator.Copy(current, current.Health, current.Stats, to);
                if (request.PublishMovedEvents) arrival.Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, current, relocated,
                    null, null, CombatDamageCause.NumericDamage, movementCause));
                projected[id] = relocated;
                yield return "AttackForcedRelocation";
                // 지나가는 칸마다 낙사·약한 지반을 즉시 정산한다(G8-1).
                if (arrival.ResolveForcedTerrainEntry(id, to)) yield break;
                bool thrown = false;
                foreach (var phase in TornadoCalculator.ResolveGroupedEntrySteps(arrival, id, to, rules, value => thrown = value))
                    yield return phase;
                if (thrown || arrival.Terminal || projected[id].Health.IsDead || projected[id].Anchor != to) yield break;
                // 요청 거리가 남으면 오브젝트 효과가 다음 칸의 점유 검사에 먼저 반영된다(G8-2a).
                if (!last || !request.DeferLastObjects)
                {
                    foreach (var phase in MovementEnvironmentCalculator.ResolveObjectsAtSteps(arrival, id, to, rules)) yield return phase;
                    if (projected[id].Health.IsDead || arrival.Terminal) yield break;
                }
                continued(true);
                yield return "AttackForcedEntryComplete";
            }
        }
    }
}
