using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
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
                !EnvironmentTerrainRules.IsWallOccupied(input.State.Environment, input.EnvironmentBindings, cell) &&
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
            var final = Location(actor, CharacterRuntimeArea.Hand, slot, null, null);
            var events = new List<CombatEvent>();
            PositionTileModifierCalculator.RecordNormalization(events, actor, final);
            events.Add(new CombatCharacterEvent(CombatEventKind.Returned, actor, final));
            return CombatTurnCalculator.Changed(input, new Dictionary<EntityId, CharacterCombatFacts> { [actor.EntityId] = final }, input.State, events);
        }

        private static CharacterCombatFacts Actor(CombatCommandInput input) => input.Kind == CombatCommandKind.PlaceMainCharacter
            ? input.Characters.SingleOrDefault(c => c.OwnershipRole == CharacterOwnershipRole.Main)
            : input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
        private static DeploymentZonePolicy Zones(CombatCommandInput input)
        {
            var battle = CombatTurnCalculator.RequiredBattle(input);
            return new DeploymentZonePolicy(battle.PlayerDeploymentDepth, battle.EnemyDeploymentDepth, input.State.BoardHeight);
        }

        internal static int EmptySlot(CombatCommandInput input, IEnumerable<CharacterCombatFacts> characters)
        {
            var occupied = characters.Where(c => c.Area == CharacterRuntimeArea.Hand).Select(c => c.ReserveSlotIndex.Value).ToHashSet();
            for (int slot = 0; slot < CombatTurnCalculator.RequiredBattle(input).ReserveCapacity; slot++)
                if (!occupied.Contains(slot)) return slot;
            return -1;
        }

        internal static CharacterCombatFacts Location(CharacterCombatFacts c, CharacterRuntimeArea area, int? slot,
            CellCoord? anchor, Rotation4? rotation)
        {
            if (area != CharacterRuntimeArea.Field || anchor != c.Anchor)
                c = PositionTileModifierCalculator.Replace(c, PositionTileModifier.None);
            return new(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                area, c.Health, c.Stats, slot, anchor, rotation, anchor.HasValue ? new[] { anchor.Value } : Array.Empty<CellCoord>(),
                anchor ?? c.MovementCheckpointAnchor, c.CommittedMoveDistance, c.PendingMoveActionCost,
                c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PendingMoveDistance, c.AttackRefundEligibleMoveCost, c.PositionModifier, c.HeldEnvironment);
        }

        internal static void RecallForNight(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected)
        {
            foreach (var c in projected.Values.OrderBy(c => c.EntityId).ToArray())
            {
                if (c.Area != CharacterRuntimeArea.Field || c.Health.IsDead || c.OwnershipRole == CharacterOwnershipRole.Main) continue;
                int? slot = c.Owner == Owner.Player ? EmptySlot(input, projected.Values) : null;
                if (slot < 0) throw new InvalidOperationException("밤 종료 회수 공간이 부족합니다.");
                projected[c.EntityId] = Location(c, c.Owner == Owner.Player ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.EnemyWaiting,
                    slot, null, null);
            }
        }

        internal static bool RedeployEnemies(CombatCommandInput input, Dictionary<EntityId, CharacterCombatFacts> projected,
            MovementProjection projection, IReadOnlyList<IMovementTileRule> rules)
        {
            var enemies = projected.Values.Where(c => c.Owner == Owner.Enemy && c.Area == CharacterRuntimeArea.EnemyWaiting && !c.Health.IsDead)
                .OrderBy(c => c.EntityId).ToArray();
            var pending = new List<(CharacterCombatFacts Actor, CombatEnemyOrigin Origin)>();
            foreach (var actor in enemies)
            {
                if (projection.Terminal) return true;
                var origin = input.State.Turn.EnemyOrigins.Single(o => o.EntityId == actor.EntityId);
                if (Available(origin.Anchor)) Place(actor, origin, origin.Anchor); else pending.Add((actor, origin));
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
                Place(entry.Actor, entry.Origin, cell);
            }
            return true;
            bool Available(CellCoord c) => input.State.IsInside(c) &&
                !EnvironmentTerrainRules.IsWallOccupied(projection.Environment, projection.Bindings, c) &&
                !projected.Values.Any(actor => !actor.Health.IsDead && actor.OccupiedCells.Contains(c)) &&
                !projection.CurrentInput().State.Structures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(c)) &&
                Zones(input).CanDeploy(Owner.Enemy, new[] { c });
            void Place(CharacterCombatFacts actor, CombatEnemyOrigin origin, CellCoord cell)
            {
                var placed = Location(actor, CharacterRuntimeArea.Field, null, cell, origin.Rotation);
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
