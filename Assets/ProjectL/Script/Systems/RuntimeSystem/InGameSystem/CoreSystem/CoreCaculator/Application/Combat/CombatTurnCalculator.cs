using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>실제 턴 사실을 입력으로 받아 다음 턴·밤 정산과 조회값을 계산한다.</summary>
    public static class CombatTurnCalculator
    {
        internal static CombatCommandTransition EnterCombat(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            if (input.State.Turn.StagePhase != StagePhase.Deployment) return CombatCommandCalculator.Reject(input, "WrongPhase");
            var main = input.Characters.Single(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main);
            if (main.Health.IsDead || main.Area != CharacterRuntimeArea.Field) return CombatCommandCalculator.Reject(input, "InvalidMainCharacter");
            var objective = input.State.Objective;
            if (objective.Kind == BattleObjectiveKind.EnemyHeadquartersDestroyed)
            {
                if (input.State.Structures.Count(s => s.Owner == Owner.Enemy && s.Definition.Key == EntityKey.Structure_Headquarters && !s.Health.IsDead) != 1)
                    return CombatCommandCalculator.Reject(input, "InvalidEnemyHeadquarters");
            }
            else if (!input.Characters.Any(c => c.EntityId == objective.TargetEntityId && c.Owner == Owner.Enemy && !c.Health.IsDead))
                return CombatCommandCalculator.Reject(input, "InvalidBattleObjective");
            RequiredBattle(input);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, projected, events);
            projection.Rewards = CombatParticipationCalculator.Capture(input.State.Rewards, input.Characters);
            var allocation = input.State.Allocation;
            if (!CombatPlacementCalculator.RedeployEnemies(input, projected, projection, rules, ref allocation)) return CombatCommandCalculator.Reject(input, "EnemyRedeploymentBlocked");
            if (projection.Terminal) return projection.Complete();
            foreach (var actor in projected.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).ToArray())
            {
                if (actor.OccupiedCells.Count != 1) return CombatCommandCalculator.Reject(input, "DeferredMultiCellUnit");
                projected[actor.EntityId] = actor.With(actionUse: UnitActionUse.Deferred);
            }
            var old = input.State.Turn;
            var turn = new CombatTurnFacts(StagePhase.Combat, old.PreparationIndex, true,
                CombatControllerPhase.StartingPlayer, old.ControllerTurnSequence, old.CompletedPlayerTurnCount,
                old.CompletedPlayerTurnCount, old.EnemyOrigins, old.Summons, old.CompletedControllerTurnCount);
            events.Add(new CombatTurnEvent(CombatEventKind.CombatEntered, EntityId.Empty, old, turn));
            return projection.Complete(turn: turn, phase: CombatFlowPhase.Automatic, isNight: true,
                allocation: allocation);
        }

        internal static CombatCommandTransition EndPlayerTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            var before = input.State.Turn;
            if (before.StagePhase != StagePhase.Combat || before.ControllerPhase != CombatControllerPhase.Player ||
                input.State.Phase != CombatFlowPhase.PlayerInput) return CombatCommandCalculator.Reject(input, "WrongPhase");
            if (input.ExpectedBattleInstanceId != input.BattleInstanceId ||
                input.ExpectedControllerTurnSequence != before.ControllerTurnSequence)
                return CombatCommandCalculator.Reject(input, "StaleTurn");
            var turn = new CombatTurnFacts(StagePhase.Combat, before.PreparationIndex, before.MainPlacementLocked,
                CombatControllerPhase.StartingEnemy, before.ControllerTurnSequence,
                checked(before.CompletedPlayerTurnCount + 1), before.NightStartPlayerTurnCount, before.EnemyOrigins, before.Summons,
                checked(before.CompletedControllerTurnCount + 1));
            // 수락한 진영 종료와 누계 증가를 먼저 기록한 뒤 공통 기믹 순서로 정산한다.
            var events = new List<CombatEvent> { new CombatTurnEvent(CombatEventKind.PlayerTurnEnded, EntityId.Empty, before, turn) };
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), events);
            var ended = projection.Complete(turn: turn, phase: CombatFlowPhase.Automatic);
            return CleanupControllerTurn(ResolveEndGimmick(ended, Owner.Player, rules), Owner.Player);
        }

        internal static CombatCommandTransition BeginControllerTurn(CombatCommandInput input)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            var before = input.State.Turn;
            bool player = before.ControllerPhase == CombatControllerPhase.StartingPlayer;
            if (before.StagePhase != StagePhase.Combat || input.State.Phase != CombatFlowPhase.Automatic ||
                (!player && before.ControllerPhase != CombatControllerPhase.StartingEnemy))
                return CombatCommandCalculator.Reject(input, "WrongPhase");
            var owner = player ? Owner.Player : Owner.Enemy;
            var projected = input.Characters.Where(c => c.Owner == owner && c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead)
                .ToDictionary(c => c.EntityId, c => c.With(actionUse: UnitActionUse.Available));
            var traitEvents = new List<CombatEvent>();
            TraitTurnLifecycle.BeginAndRecord(input, projected, owner, traitEvents);
            var turn = new CombatTurnFacts(StagePhase.Combat, before.PreparationIndex, before.MainPlacementLocked,
                player ? CombatControllerPhase.Player : CombatControllerPhase.Enemy,
                checked(before.ControllerTurnSequence + 1), before.CompletedPlayerTurnCount,
                before.NightStartPlayerTurnCount, before.EnemyOrigins, before.Summons, before.CompletedControllerTurnCount);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            foreach (var pair in projected) characters[pair.Key] = pair.Value;
            var events = new List<CombatEvent> { new CombatTurnEvent(CombatEventKind.ControllerTurnStarted, EntityId.Empty, before, turn) };
            events.AddRange(traitEvents);
            var state = State(input.State,turn,                player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic)
                .With(traitTurn: input.State.TraitTurn.BeginTurn(owner));
            state = HunterApplication.Reconcile(input, characters, state, events);
            return Changed(input, characters, state, events);
        }

        internal static CombatCommandTransition EndEnemyPhase(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) return CombatCommandCalculator.Reject(input, "StageEnded");
            var before = input.State.Turn;
            if (before.StagePhase != StagePhase.Combat || before.ControllerPhase != CombatControllerPhase.Enemy ||
                input.State.Phase != CombatFlowPhase.Automatic) return CombatCommandCalculator.Reject(input, "WrongPhase");
            // 적 AI가 현재 세계에서 가능한 행동이 없음을 확인한 뒤에만 호출한다.
            bool endsNight = before.CompletedPlayerTurnsThisNight == PlayerTurnRules.PlayerTurnsPerNight;
            var turn = new CombatTurnFacts(StagePhase.Combat, before.PreparationIndex, before.MainPlacementLocked,
                CombatControllerPhase.StartingPlayer,
                before.ControllerTurnSequence, before.CompletedPlayerTurnCount, before.NightStartPlayerTurnCount,
                before.EnemyOrigins, before.Summons, checked(before.CompletedControllerTurnCount + 1));
            // 기믹 착지로 승패가 나더라도 이미 수락한 적 국면 종료와 누계 증가는 유지한다.
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), events);
            events.Add(new CombatTurnEvent(CombatEventKind.EnemyPhaseEnded, EntityId.Empty, before, turn));
            var ended = projection.Complete(turn: turn, phase: CombatFlowPhase.Automatic);
            ended = CleanupControllerTurn(ResolveEndGimmick(ended, Owner.Enemy, rules), Owner.Enemy);
            if (ended.Outcome.IsStageEnded || !endsNight) return ended;

            // 밤의 마지막 적 종료도 기차·사망 정산을 먼저 한다. 끝난 전투를 회수하거나 다음 배치로 옮기지 않는다.
            var afterEnd = CombatCommandComposition.Project(ended);
            var nightEnd = EndNight(afterEnd);
            return CombatCommandComposition.Merge(input, new[] { ended, nightEnd });
        }

        private static CombatCommandTransition CleanupControllerTurn(CombatCommandTransition ended, Owner owner)
        {
            if (ended.Outcome.IsStageEnded) return ended;
            var after = CombatCommandComposition.Project(ended);
            var characters = after.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            TraitTurnLifecycle.EndAndRecord(after, characters, owner, events);
            var state = TraitTrailCalculator.EndControllerTurn(after.State, owner, events);
            RegenerationTurnApplication.EndOwnSide(after, CombatDamageApplication.For(after, characters, events), owner);
            if (after.Characters.All(c => ReferenceEquals(c, characters[c.EntityId])) && ReferenceEquals(state, after.State)) return ended;
            var cleaned = Changed(after, characters, state, events);
            return CombatCommandComposition.Merge(ended.Input, new[] { ended, cleaned });
        }

        private static CombatCommandTransition ResolveEndGimmick(CombatCommandTransition ended, Owner endedSide, IReadOnlyList<IMovementTileRule> rules)
        {
            var gimmick = CombatGimmickCalculator.TryAdvance(CombatCommandComposition.Project(ended),
                new StageGimmickBoundary(endedSide, ended.FinalState.Turn.CompletedControllerTurnCount), rules);
            return gimmick == null ? ended : CombatCommandComposition.Merge(ended.Input, new[] { ended, gimmick });
        }

        private static CombatCommandTransition EndNight(CombatCommandInput input)
        {
            var before = input.State.Turn;
            var turn = new CombatTurnFacts(StagePhase.Deployment, checked(before.PreparationIndex + 1), before.MainPlacementLocked,
                CombatControllerPhase.None, before.ControllerTurnSequence, before.CompletedPlayerTurnCount,
                before.NightStartPlayerTurnCount, before.EnemyOrigins, before.Summons, before.CompletedControllerTurnCount);
            var events = new List<CombatEvent>();
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            foreach (var character in projected.Values.OrderBy(c => c.EntityId).ToArray())
            {
                var expired = HeldEnvironmentEffects.ExpireSupplies(character);
                if (!ReferenceEquals(character, expired)) events.Add(new CombatHeldEnvironmentEvent(character, expired));
                projected[character.EntityId] = ItemLifetimeCalculator.ClearRoundItems(expired, events, ItemEventReason.RoundEnded);
            }
            events.Add(new CombatTurnEvent(CombatEventKind.NightEnded, EntityId.Empty, before, turn));
            CombatPlacementCalculator.RecallForNight(input, projected, events);
            var state = State(input.State, turn, CombatFlowPhase.PlayerInput, isNight: false);
            state = HunterApplication.Reconcile(input, projected, state, events);
            return Changed(input, projected, state, events);
        }

        internal static CombatBattleFacts RequiredBattle(CombatCommandInput input) => input.Battle ??
            throw new InvalidOperationException("전투 설정·드로우 Buffer 사실이 필요합니다.");

        internal static CombatCommandState State(CombatCommandState source,CombatTurnFacts turn,CombatFlowPhase phase,            bool? isNight = null,CombatAllocationFacts allocation = null) => new(source.BoardWidth,source.BoardHeight,phase,isNight ?? source.IsNight,source.Structures,                source.ThornFields.ToArray(),turn,source.Environment,source.EnvironmentProgress,                source.WallBlocking,source.Objective,allocation ?? source.Allocation,source.ItemRandomState,source.Rewards,source.TraitTurn,gimmicks: source.Gimmicks);

        internal static CombatCommandTransition Changed(CombatCommandInput input,
            IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected, CombatCommandState state, IReadOnlyList<CombatEvent> events = null)
        {
            var reconciled = input.Characters.ToDictionary(c => c.EntityId);
            foreach (var pair in projected) reconciled[pair.Key] = pair.Value;
            var orderedEvents = (events ?? Array.Empty<CombatEvent>()).ToList();
            TraitPresenceReconciler.ApplyAndRecord(input, reconciled, orderedEvents);
            projected = reconciled;
            var main = projected.Values.SingleOrDefault(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main);
            return new(input, true, "", new CharacterChanges(input.Characters.Where(c => projected.ContainsKey(c.EntityId) &&
                !ReferenceEquals(c, projected[c.EntityId])).Select(c => new CharacterStateChange(c, projected[c.EntityId])).ToArray(),
                Array.Empty<Contracts.Roster.SoulCountChange>(), main == null ? null : PositionTileModifierCalculator.MainHealthChange(input, main)),
                new SoulFieldChange(input.Souls, input.Souls), state,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), events: orderedEvents);
        }
    }
}
