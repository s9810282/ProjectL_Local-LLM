using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
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
            FindBlocker(input, environment, characters, structures, id, from, to).IsNone;

        /// <summary>
        /// 유닛 강제 이동이 <paramref name="to"/>에 들어가지 못하는 이유를 돌려준다. 강제 이동 진입 규칙의 유일한 원본이다.
        /// 맵 밖·벽 지형·낮은 칸에서 고지대로 가는 턱은 벽, 살아 있는 필드 캐릭터·구조물은 그 개체다. 다칸 대상도 앵커 칸만 본다.
        /// </summary>
        internal static ForcedMovementBlocker FindBlocker(CombatCommandInput input, MovementEnvironmentFacts environment,
            IEnumerable<CharacterCombatFacts> characters, IEnumerable<StructureCombatFacts> structures,
            EntityId id, CellCoord from, CellCoord to)
        {
            if (!input.State.IsInside(to) || !EnvironmentTerrainRules.CanKnockback(environment, input.EnvironmentBindings, from, to))
                return ForcedMovementBlocker.Wall;
            var character = characters.Where(c => c.EntityId != id && c.Area == CharacterRuntimeArea.Field &&
                !c.Health.IsDead && c.OccupiedCells.Contains(to)).OrderBy(c => c.EntityId).FirstOrDefault();
            if (character != null) return ForcedMovementBlocker.Character(character.EntityId);
            var structure = structures.Where(s => !s.Health.IsDead && s.OccupiedCells.Contains(to))
                .OrderBy(s => s.EntityId).FirstOrDefault();
            return structure != null ? ForcedMovementBlocker.Structure(structure.EntityId) : ForcedMovementBlocker.None;
        }

        /// <summary>넉백은 마지막 오브젝트 도착을 정산 뒤로 미루며, 사전 강제 이동은 즉시 도착까지 처리합니다.</summary>
        internal static IEnumerable<string> ExecuteUnitSteps(CommandAttackPlan plan, CommandAttackProjection world,
            CommandAttackLedger ledger, MovementProjection arrival, EntityId id, CellCoord step, int distance,
            bool deferLastObjects, bool publishMovedEvents, Action<ForcedDisplacementPath> completed,
            CombatMovementCause movementCause = null)
        {
            var input = plan.Input;
            var projected = world.Characters;
            var victim = projected[id];
            var footprint = Footprint(CombatCommandCalculator.RequiredProfile(input, victim).Definition);
            var rotation = victim.Rotation.Value;
            bool departed = false;
            movementCause ??= new CombatMovementCause(CombatMovementAgency.Forced);
            yield return "AttackForcedPathSetup";

            using var path = ForcedDisplacementPathCalculator.ExecuteSteps(victim.Anchor.Value, step, distance,
                CanEnter, Enter, completed).GetEnumerator();
            while (true)
            {
                bool progressed;
                using (CoreProfiling.Sample(CoreProfileSection.AttackForcedPathStep)) progressed = path.MoveNext();
                if (!progressed) yield break;
                yield return path.Current;
            }

            IEnumerable<string> CanEnter(CellCoord from, CellCoord to, Action<bool> admitted)
            {
                // 앞선 이동을 반영한 현재 점유로 다음 칸을 검사한다.
                admitted(CanEnterUnit(input, arrival.Environment, projected.Values, world.Structures.Values, id, from, to));
                yield return "AttackForcedAdmission";
            }

            IEnumerable<string> Enter(CellCoord from, CellCoord to, bool last, Action<bool> continued)
            {
                var current = projected[id];
                if (!departed)
                {
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
                yield return "AttackForcedRelocation";
                // 지나가는 칸마다 낙사·약한 지반·가시를 즉시 정산한다(G8-1).
                if (arrival.ResolveForcedTerrainEntry(id, to)) yield break;
                var consumedFields = new List<ThornFieldCellSnapshot>();
                CombatCommandCalculator.ConsumeFields(world.ThornFields,
                    MovementPathCalculator.ResolvePathCells(from, to, footprint, rotation), consumedFields);
                if (consumedFields.Count > 0) RoarHitCounter.Record(projected, id, world.HitCause, input.State.InCombat);
                var origins = consumedFields.Select(f => new CombatDamageOrigin(f.Cell, null, world.AttackGroups.Next())).ToArray();
                int thornDamage = 0;
                var heldAbsorptions = new List<(CombatDamageOrigin Origin, int Input)>();
                yield return "AttackForcedThornContact";
                for (int j = 0; j < consumedFields.Count; j++)
                {
                    int numeric = CombatDamageCalculator.Environment(input, consumedFields[j].ThornDamage, origins[j], to, arrival.Environment).FinalDamage;
                    int remaining = CombatDamageCalculator.ApplyHeldDefense(world.AttackGroups, projected, id, numeric, origins[j], world.Events);
                    if (remaining == 0) heldAbsorptions.Add((origins[j], numeric));
                    thornDamage = checked(thornDamage + remaining);
                    yield return "AttackForcedThornDamage";
                }
                if (origins.Length > 0)
                {
                    foreach (var phase in world.Damage.ApplySteps(id, CombatDamageKind.EnvironmentDamage, origins[0],
                        thornDamage, value => thornDamage = value.RemainingDamage, origins, resolvedHeldAbsorptions: heldAbsorptions))
                        yield return phase;
                    ThornFieldApplicationCalculator.ApplyContactStatuses(arrival, id, consumedFields);
                    yield return "AttackForcedThornStatuses";
                }
                ledger.AddDamage(id, thornDamage);
                if (projected[id].Health.IsDead)
                {
                    world.DeathAnchors[id] = to;
                    yield break;
                }
                bool thrown = false;
                foreach (var phase in TornadoCalculator.ResolveGroupedEntrySteps(arrival, id, to, plan.Rules, value => thrown = value))
                    yield return phase;
                if (thrown || arrival.Terminal || projected[id].Health.IsDead || projected[id].Anchor != to) yield break;
                // 요청 거리가 남으면 오브젝트 효과가 다음 칸의 점유 검사에 먼저 반영된다(G8-2a).
                if (!last || !deferLastObjects)
                {
                    foreach (var phase in MovementEnvironmentCalculator.ResolveObjectsAtSteps(arrival, id, to, plan.Rules)) yield return phase;
                    if (projected[id].Health.IsDead || arrival.Terminal) yield break;
                }
                continued(true);
                yield return "AttackForcedEntryComplete";
            }
        }

        private static TileFootprint Footprint(UnitSpec spec) => new(spec.FootprintWidth, spec.FootprintHeight, spec.FootprintLocalCells.ToArray());
    }
}
