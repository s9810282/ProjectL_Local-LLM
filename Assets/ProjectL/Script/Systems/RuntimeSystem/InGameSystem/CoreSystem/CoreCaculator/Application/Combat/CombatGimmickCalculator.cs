using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class CombatGimmickCalculator
    {
        public static CombatCommandTransition TryAdvance(CombatCommandInput input)
        {
            var turn = input.State.Turn;
            var due = turn.Gimmicks.Where(g => g.NextActivationTurn <= turn.CompletedUnitTurnCount)
                .OrderBy(g => g.Id.Value).FirstOrDefault();
            if (due.Id.IsEmpty) return null;
            var battle = CombatTurnCalculator.RequiredBattle(input);
            if (battle.GimmickDefinitions[due.Id.Value - 1] is not RailwayGimmickSpec railway)
                throw new InvalidOperationException("발동할 기믹 계산이 연결되지 않았습니다.");
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var deathAnchors = new Dictionary<EntityId, CellCoord>();
            foreach (var actor in input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead &&
                c.OccupiedCells.Any(cell => battle.RailCells.Contains(cell))))
            {
                deathAnchors[actor.EntityId] = actor.Anchor.Value;
                projected[actor.EntityId] = CombatCommandCalculator.Copy(actor, new HealthStateSnapshot(0, actor.Health.MaxHealth), actor.Stats);
            }
            var gimmicks = turn.Gimmicks.Select(g => g.Id == due.Id ? new CombatGimmickFacts(g.Id,
                checked(g.NextActivationTurn + railway.RepeatIntervalUnitTurns), checked(g.ActivationCount + 1)) : g).ToArray();
            var finalTurn = new CombatTurnFacts(turn.StagePhase, turn.PreparationIndex, turn.MainPlacementLocked, turn.CurrentTime,
                turn.CurrentNightTime, turn.CompletedUnitTurnCount, turn.Participants, turn.EnemyOrigins, gimmicks);
            var state = CombatTurnCalculator.State(input.State, finalTurn, CombatFlowPhase.Automatic);
            var events = new List<CombatEvent> { new CombatGimmickEvent(new Contracts.Snapshots.StageGimmicks.RailwayGimmickActivationSnapshot(
                due.Id, battle.RailCells.ToArray(), deathAnchors.Keys.OrderBy(id => id).ToArray())) };
            foreach (var actor in input.Characters.OrderBy(c => c.EntityId))
                CombatCommandEvents.Health(events, actor, projected[actor.EntityId]);
            return CombatCommandCalculator.Finish(input, projected, input.SoulCounts.ToDictionary(p => p.Key, p => p.Value), input.Souls,
                input.State.ThornFields.ToDictionary(f => f.Cell), input.State.Structures.ToArray(), new List<long>(), deathAnchors,
                new SoulEffectCalculator(input.Artifacts), null, state, events: events);
        }
    }
}
