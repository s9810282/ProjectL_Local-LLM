using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>공격의 유닛 강제 이동에서 충돌·정규화·칸별 지형 처리를 공유합니다. 방향·면역·공격 순서는 호출자가 소유합니다.</summary>
    internal static class AttackForcedMovement
    {
        internal static bool CanEnterUnit(CombatCommandInput input, MovementEnvironmentFacts environment,
            IEnumerable<CharacterCombatFacts> characters, IEnumerable<StructureCombatFacts> structures,
            EntityId id, CellCoord from, CellCoord to) =>
            EnvironmentTerrainRules.CanKnockback(environment, input.EnvironmentBindings, from, to) &&
            input.State.IsInside(to) &&
            !characters.Any(c => c.EntityId != id && c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(to)) &&
            !structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(to));

        /// <summary>넉백은 마지막 오브젝트 도착을 정산 뒤로 미루며, 사전 강제 이동은 즉시 도착까지 처리합니다.</summary>
        internal static ForcedDisplacementPath ExecuteUnit(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, MovementProjection arrival, EntityId id, CellCoord step, int distance,
            bool deferLastObjects, bool publishMovedEvents, CombatMovementCause movementCause = null)
        {
            var input = plan.Input;
            var projected = world.Characters;
            var victim = projected[id];
            var footprint = Footprint(CombatCommandCalculator.RequiredProfile(input, victim).Definition);
            var rotation = victim.Rotation.Value;
            bool departed = false;
            movementCause ??= new CombatMovementCause(CombatMovementAgency.Forced);
            // 앞선 대상의 이동 결과가 projected에 반영되므로 뒤 대상의 점유 검사는 매 칸 현재 값을 본다.
            return ForcedDisplacementPathCalculator.Execute(victim.Anchor.Value, step, distance,
                (from, to) => CanEnterUnit(input, arrival.Environment, projected.Values, world.Structures.Values, id, from, to),
                (from, to, last) =>
                {
                    var current = projected[id];
                    if (!departed)
                    {
                        // 위치 보정은 출발할 때 한 번만 해제한다.
                        var normalized = PositionTileModifierCalculator.Replace(current, PositionTileModifier.None);
                        PositionTileModifierCalculator.RecordNormalization(world.Events, current, normalized);
                        ledger.AddNormalizationLoss(id, current.Health.CurrentHealth - normalized.Health.CurrentHealth);
                        current = normalized;
                        departed = true;
                    }
                    var relocated = CombatCommandCalculator.Copy(current, current.Health, current.Stats, to);
                    if (publishMovedEvents) world.Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, current, relocated,
                        null, null, CombatDamageCause.NumericDamage, movementCause));
                    projected[id] = relocated;
                    // S8: 지나가는 칸마다 낙사·약한 지반·가시를 즉시 정산한다(G8-1).
                    if (arrival.ResolveForcedTerrainEntry(id, to)) { return false; }
                    var consumedFields = new List<ThornFieldCellSnapshot>();
                    CombatCommandCalculator.ConsumeFields(world.ThornFields,
                        MovementPathCalculator.ResolvePathCells(from, to, footprint, rotation), consumedFields);
                    if (consumedFields.Count > 0) RoarHitCounter.Record(projected, id, world.HitCause, input.State.IsNight);
                    var origins = consumedFields.Select(f => new CombatDamageOrigin(f.Cell, null, world.AttackGroups.Next())).ToArray();
                    int thornDamage = 0;
                    var heldAbsorptions = new List<(CombatDamageOrigin Origin, int Input)>();
                    for (int j = 0; j < consumedFields.Count; j++)
                    {
                        int numeric = CombatDamageCalculator.Environment(input, consumedFields[j].ThornDamage, origins[j], to, arrival.Environment).FinalDamage;
                        int remaining = CombatDamageCalculator.ApplyHeldDefense(world.AttackGroups, projected, id, numeric, origins[j], world.Events);
                        if (remaining == 0) heldAbsorptions.Add((origins[j], numeric));
                        thornDamage = checked(thornDamage + remaining);
                    }
                    if (origins.Length > 0)
                    {
                        thornDamage = world.Damage.Apply(id, CombatDamageKind.EnvironmentDamage, origins[0], thornDamage, origins,
                            resolvedHeldAbsorptions: heldAbsorptions).RemainingDamage;
                        ThornFieldApplicationCalculator.ApplyContactStatuses(arrival, id, consumedFields);
                    }
                    ledger.AddDamage(id, thornDamage);
                    if (projected[id].Health.IsDead)
                    {
                        world.DeathAnchors[id] = to;
                        return false;
                    }
                    if (TornadoCalculator.ResolveGroupedEntry(arrival, id, to, plan.Rules)) return false;
                    if (arrival.Terminal || projected[id].Health.IsDead || projected[id].Anchor != to) return false;
                    // 요청 거리가 남았으면 오브젝트를 먼저 처리해 다음 칸 점유에 반영한다(G8-2a).
                    // 요청 거리를 모두 소진한 칸의 오브젝트만 기존처럼 S12에서 처리한다.
                    if (!last || !deferLastObjects)
                    {
                        MovementEnvironmentCalculator.ResolveObjectsAt(arrival, id, to, plan.Rules);
                        if (projected[id].Health.IsDead || arrival.Terminal) { return false; }
                    }
                    return true;
                });

        }

        private static TileFootprint Footprint(UnitSpec spec) => new(spec.FootprintWidth, spec.FootprintHeight, spec.FootprintLocalCells.ToArray());
    }
}
