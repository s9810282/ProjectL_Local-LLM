using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Summoning;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>배치·회수는 이동 자원을 소비하지 않는다. 위치의 원본은 캐릭터 Buffer다.</summary>
    public static class CombatPlacementCalculator
    {
        public static CombatPlacementPreview Preview(CombatCommandInput input)
        {
            var actor = Actor(input);
            bool eligible = input.State.Phase != CombatFlowPhase.Finished && input.State.Turn.StagePhase == StagePhase.Deployment &&
                actor != null && actor.Owner == Owner.Player && !actor.Health.IsDead &&
                (actor.Area == CharacterRuntimeArea.Field || actor.Area == CharacterRuntimeArea.Hand) &&
                (actor.OwnershipRole != CharacterOwnershipRole.Main || !input.State.Turn.MainPlacementLocked);
            if (!eligible) return new CombatPlacementPreview(false, Array.Empty<CellCoord>());
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            if (profile.Definition.FootprintLocalCells.Length != 1) return new CombatPlacementPreview(false, Array.Empty<CellCoord>());
            var cell = input.TargetCell;
            bool valid = input.State.IsInside(cell) && Zones(input).CanDeploy(actor.Owner, new[] { cell }) &&
                !EnvironmentTerrainRules.BlocksVoluntaryEntry(input.State.Environment, input.EnvironmentBindings, cell) &&
                !input.Characters.Any(c => c.EntityId != actor.EntityId && c.OccupiedCells.Contains(cell)) &&
                !input.State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));
            return new CombatPlacementPreview(valid, new[] { cell });
        }

        internal static CombatCommandTransition Place(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (!Preview(input).CanPlace) return CombatCommandCalculator.Reject(input, "InvalidPlacement");
            var actor = Actor(input);
            var departed = actor.Anchor == input.TargetCell ? actor : PositionTileModifierCalculator.Replace(actor, PositionTileModifier.None);
            var events = new List<CombatEvent>();
            PositionTileModifierCalculator.RecordNormalization(events, actor, departed);
            var relocated = Location(departed, CharacterRuntimeArea.Field, null, input.TargetCell, input.Rotation);
            events.Add(new CombatCharacterEvent(CombatEventKind.Placed, departed, relocated));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[actor.EntityId] = relocated;
            var projection = MovementProjection.ForArrival(input, projected, events);
            MovementEnvironmentCalculator.ResolveObjectsAt(projection, actor.EntityId, input.TargetCell, rules);
            if (!projected[actor.EntityId].Health.IsDead && !projection.Terminal)
            {
                var arrived = projected[actor.EntityId];
                var final = PositionTileModifierCalculator.Replace(arrived,
                    PositionTileModifierCalculator.ResolveAt(input.TargetCell, projection.Environment));
                PositionTileModifierCalculator.RecordNormalization(events, arrived, final);
                projected[actor.EntityId] = final;
            }
            return projection.Complete();
        }

        internal static CombatCommandTransition Return(CombatCommandInput input)
        {
            var actor = Actor(input);
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            if (input.State.Turn.StagePhase != StagePhase.Deployment) return CombatCommandCalculator.Reject(input, "WrongPhase");
            if (actor == null || actor.Owner != Owner.Player || actor.Health.IsDead || actor.Area != CharacterRuntimeArea.Field ||
                actor.OwnershipRole == CharacterOwnershipRole.Main) return CombatCommandCalculator.Reject(input, "InvalidReturn");
            int slot = EmptySlot(input, input.Characters);
            if (slot < 0) return CombatCommandCalculator.Reject(input, "ReserveFull");
            var events = new List<CombatEvent>();
            // 배치 단계 손 복귀는 자리 바꾸기라 방어막 예산을 남긴다. 다시 놓으면 끌어서 옮긴 것과 같다(밤 종료 회수는 0 그대로).
            var final = Location(actor, CharacterRuntimeArea.Hand, slot, null, null, events, keepShieldBudget: true);
            return TraitPresenceReconciler.AfterTransition(Items.ItemEquipmentCalculator.RefundReturn(input, actor, final, events));
        }

        private static CharacterCombatFacts Actor(CombatCommandInput input) => input.Kind == CombatCommandKind.PlaceMainCharacter
            ? input.Characters.SingleOrDefault(c => c.OwnershipRole == CharacterOwnershipRole.Main)
            : input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
        private static DeploymentZonePolicy Zones(CombatCommandInput input) =>
            new DeploymentZonePolicy(CombatTurnCalculator.RequiredBattle(input).PlayerZone);

        internal static int EmptySlot(CombatCommandInput input, IEnumerable<CharacterCombatFacts> characters)
        {
            var occupied = characters.Where(c => c.Area == CharacterRuntimeArea.Hand).Select(c => c.ReserveSlotIndex.Value).ToHashSet();
            for (int slot = 0; slot < CombatTurnCalculator.RequiredBattle(input).ReserveCapacity; slot++)
                if (!occupied.Contains(slot)) return slot;
            return -1;
        }

        /// <param name="keepShieldBudget">예비 구역 진입 때 방어막 예산을 남길지. 배치 단계 손 복귀(`Return`)만 참.</param>
        internal static CharacterCombatFacts Location(CharacterCombatFacts c, CharacterRuntimeArea area, int? slot,
            CellCoord? anchor, Rotation4? rotation, List<CombatEvent> events = null, bool keepShieldBudget = false)
        {
            bool returning = c.Area != area && (area == CharacterRuntimeArea.Hand || area == CharacterRuntimeArea.EnemyWaiting);
            if (area != CharacterRuntimeArea.Field || anchor != c.Anchor)
            {
                var normalized = PositionTileModifierCalculator.Replace(c, PositionTileModifier.None);
                if (returning && events != null) PositionTileModifierCalculator.RecordNormalization(events, c, normalized);
                c = normalized;
            }
            var relocated = c.WithPlacement(area: area, reserveSlotIndex: slot, anchor: anchor, rotation: rotation,
                occupiedCells: anchor.HasValue ? new[] { anchor.Value } : Array.Empty<CellCoord>(), actionUse: UnitActionUse.Deferred);
            if (returning)
            {
                events?.Add(new CombatCharacterEvent(CombatEventKind.Returned, c, relocated));
                return CombatReserveReturnCleanup.Clear(relocated, events, keepShieldBudget);
            }
            return c.Area != CharacterRuntimeArea.Field || area != CharacterRuntimeArea.Field
                ? RegenerationTurnApplication.Reset(relocated) : relocated;
        }

        internal static void RecallForNight(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected, List<CombatEvent> events = null)
        {
            foreach (var c in projected.Values.OrderBy(c => c.EntityId).ToArray())
            {
                if (c.Area != CharacterRuntimeArea.Field || c.Health.IsDead || c.OwnershipRole == CharacterOwnershipRole.Main) continue;
                int? slot = c.Owner == Owner.Player ? EmptySlot(input, projected.Values) : null;
                if (slot < 0) throw new InvalidOperationException("밤 종료 회수 공간이 부족합니다.");
                projected[c.EntityId] = Location(c, c.Owner == Owner.Player ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.EnemyWaiting,
                    slot, null, null, events);
            }
        }

        /// <param name="allocation">소환수 복귀 추첨이 소비한 소환 난수를 돌려준다. 개체 ID는 바꾸지 않는다.</param>
        internal static bool RedeployEnemies(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected,
            MovementProjection projection, IReadOnlyList<IMovementTileRule> rules, ref CombatAllocationFacts allocation)
        {
            var summons = input.State.Turn.Summons.ToDictionary(r => r.SummonedId);
            var enemies = projected.Values.Where(c => c.Owner == Owner.Enemy && c.Area == CharacterRuntimeArea.EnemyWaiting && !c.Health.IsDead)
                .OrderBy(c => c.EntityId).ToArray();
            var pending = new List<(CharacterCombatFacts Actor, CombatEnemyOrigin Origin)>();
            // 초기 배치 적(보스 포함)을 먼저 원위치로 둔다. 소환수는 원위치가 없으므로 이 경로로 보내지 않는다.
            foreach (var actor in enemies.Where(e => !summons.ContainsKey(e.EntityId)))
            {
                if (projection.Terminal) return true;
                var origin = input.State.Turn.EnemyOrigins.Single(o => o.EntityId == actor.EntityId);
                if (Available(origin.Anchor)) Place(actor, origin.Anchor, origin.Rotation); else pending.Add((actor, origin));
            }
            foreach (var entry in pending)
            {
                if (projection.Terminal) return true;
                var candidates = new List<CellCoord>();
                for (int y = 0; y < input.State.BoardHeight; y++)
                    for (int x = 0; x < input.State.BoardWidth; x++)
                        if (Available(new CellCoord(x, y))) candidates.Add(new CellCoord(x, y));
                if (candidates.Count == 0) return false;
                var cell = candidates.OrderBy(c => CombatMovementCalculator.Distance(c, entry.Origin.Anchor)).ThenBy(c => c.Y).ThenBy(c => c.X).First();
                Place(entry.Actor, cell, entry.Origin.Rotation);
            }
            // 같은 개체의 소환수는 소환자가 자리를 잡은 뒤 그 주변 8칸에서 다시 추첨한다(G4).
            // 자리가 없거나 소환자가 필드에 없으면 다음 밤까지 대기하며, 전투 진입은 막지 않는다.
            foreach (var actor in enemies.Where(e => summons.ContainsKey(e.EntityId)))
            {
                if (projection.Terminal) return true;
                if (!projected.TryGetValue(summons[actor.EntityId].SummonerId, out var summoner) ||
                    summoner.Health.IsDead || summoner.Area != CharacterRuntimeArea.Field) continue;
                var candidates = CombatSummonCalculator.Candidates(projection.CurrentInput().State, projected.Values,
                    projection.Environment, projection.Bindings, summoner.Anchor.Value);
                if (candidates.Count == 0) continue;
                if (!allocation.IsAvailable)
                    throw new InvalidOperationException("소환 기록이 있는 전투에는 할당 진행값이 필요합니다.");
                uint random = allocation.SummonRandomState;
                var cell = candidates[EnvironmentRandom.Choose(ref random, candidates.Count)];
                allocation = new CombatAllocationFacts(allocation.NextEntityId, random);
                Place(actor, cell, summoner.Rotation.Value);
            }
            return true;
            bool Available(CellCoord c) => input.State.IsInside(c) &&
                !EnvironmentTerrainRules.BlocksVoluntaryEntry(projection.Environment, projection.Bindings, c) &&
                !projected.Values.Any(actor => !actor.Health.IsDead && actor.OccupiedCells.Contains(c)) &&
                !projection.CurrentInput().State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(c)) &&
                Zones(input).CanDeploy(Owner.Enemy, new[] { c });
            void Place(CharacterCombatFacts actor, CellCoord cell, Rotation4 rotation)
            {
                var placed = Location(actor, CharacterRuntimeArea.Field, null, cell, rotation);
                projected[actor.EntityId] = placed;
                projection.Events.Add(new CombatCharacterEvent(CombatEventKind.Placed, actor, placed));
                MovementEnvironmentCalculator.ResolveObjectsAt(projection, actor.EntityId, cell, rules);
                if (projected[actor.EntityId].Health.IsDead || projection.Terminal) return;
                var arrived = projected[actor.EntityId];
                projected[actor.EntityId] = PositionTileModifierCalculator.Replace(arrived,
                    PositionTileModifierCalculator.ResolveAt(cell, projection.Environment));
                PositionTileModifierCalculator.RecordNormalization(projection.Events, arrived, projected[actor.EntityId]);
            }
        }
    }
}
