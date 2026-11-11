using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>실제 턴 사실을 입력으로 받아 전투 진입·진영 턴 시작과 종료 정산, 조회값을 계산한다.</summary>
    public static class CombatTurnCalculator
    {
        internal static CombatCommandTransition EnterCombat(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) 
                return CombatCommandCalculator.Reject(input, "StageEnded");

            if (input.State.Turn.StagePhase != StagePhase.Deployment) 
                return CombatCommandCalculator.Reject(input, "WrongPhase");

            var main = input.Characters.Single(c => c.OwnershipRole == Contracts.Roster.CharacterOwnershipRole.Main);

            if (main.Health.IsDead || main.Area != CharacterRuntimeArea.Field) 
                return CombatCommandCalculator.Reject(input, "InvalidMainCharacter");

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

            if (!CombatPlacementCalculator.RedeployEnemies(input, projected, projection, rules, ref allocation)) 
                return CombatCommandCalculator.Reject(input, "EnemyRedeploymentBlocked");

            if (projection.Terminal) 
                return projection.Complete();

            foreach (var actor in projected.Values.Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead).ToArray())
            {
                if (actor.OccupiedCells.Count != 1) 
                    return CombatCommandCalculator.Reject(input, "DeferredMultiCellUnit");

                projected[actor.EntityId] = actor.With(actionUse: UnitActionUse.Deferred);
            }

            var old = input.State.Turn;

            var turn = new CombatTurnFacts(StagePhase.Combat,
                CombatControllerPhase.StartingPlayer, old.ControllerTurnSequence, old.CompletedPlayerTurnCount,
                old.EnemyOrigins, old.Summons, old.CompletedControllerTurnCount);

            events.Add(new CombatTurnEvent(CombatEventKind.CombatEntered, EntityId.Empty, old, turn));

            return projection.Complete(turn: turn, phase: CombatFlowPhase.Automatic, allocation: allocation);
        }

        internal static CombatCommandTransition EndPlayerTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) 
                return CombatCommandCalculator.Reject(input, "StageEnded");

            var before = input.State.Turn;

            if (before.StagePhase != StagePhase.Combat || before.ControllerPhase != CombatControllerPhase.Player || input.State.Phase != CombatFlowPhase.PlayerInput) 
                return CombatCommandCalculator.Reject(input, "WrongPhase");

            if (input.ExpectedBattleInstanceId != input.BattleInstanceId || input.ExpectedControllerTurnSequence != before.ControllerTurnSequence)
                return CombatCommandCalculator.Reject(input, "StaleTurn");

            var turn = new CombatTurnFacts(StagePhase.Combat,
                CombatControllerPhase.StartingEnemy, before.ControllerTurnSequence,
                checked(before.CompletedPlayerTurnCount + 1), before.EnemyOrigins, before.Summons,
                checked(before.CompletedControllerTurnCount + 1));

            // 수락한 진영 종료와 누계 증가를 먼저 기록한 뒤 공통 기믹 순서로 정산한다.
            var events = new List<CombatEvent> { new CombatTurnEvent(CombatEventKind.PlayerTurnEnded, EntityId.Empty, before, turn) };

            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), events);

            var ended = projection.Complete(turn: turn, phase: CombatFlowPhase.Automatic);

            return CleanupControllerTurn(ResolveEndGimmick(ended, Owner.Player, rules), Owner.Player);
        }

        /// <param name="rules">재구성 Lv3 부활 넉백이 밀린 칸의 가시·오브젝트·토네이도를 정산할 때 씁니다.</param>
        internal static CombatCommandTransition BeginControllerTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (input.State.Phase == CombatFlowPhase.Finished)
                return CombatCommandCalculator.Reject(input, "StageEnded");

            var before = input.State.Turn;

            bool player = before.ControllerPhase == CombatControllerPhase.StartingPlayer;

            if (before.StagePhase != StagePhase.Combat || input.State.Phase != CombatFlowPhase.Automatic ||
                (!player && before.ControllerPhase != CombatControllerPhase.StartingEnemy))
                return CombatCommandCalculator.Reject(input, "WrongPhase");

            var owner = player ? Owner.Player : Owner.Enemy;
            var projected = input.Characters.Where(c => c.Owner == owner && c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead)
                .ToDictionary(c => c.EntityId, c => c.With(actionUse: UnitActionUse.Available));

            int livingAtStart = projected.Count;
            var traitEvents = new List<CombatEvent>();

            TraitTurnLifecycle.BeginAndRecord(input, projected, owner, traitEvents);

            var turn = new CombatTurnFacts(StagePhase.Combat,
                player ? CombatControllerPhase.Player : CombatControllerPhase.Enemy,
                checked(before.ControllerTurnSequence + 1), before.CompletedPlayerTurnCount,
                before.EnemyOrigins, before.Summons, before.CompletedControllerTurnCount);

            var characters = input.Characters.ToDictionary(c => c.EntityId);

            foreach (var pair in projected) 
                characters[pair.Key] = pair.Value;

            var events = new List<CombatEvent> { new CombatTurnEvent(CombatEventKind.ControllerTurnStarted, EntityId.Empty, before, turn) };
            events.AddRange(traitEvents);

            var state = State(input.State,turn, player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic)
                .With(traitTurn: input.State.TraitTurn.BeginTurn(owner));

            var projection = MovementProjection.ForArrival(input, characters, events, state);

            // 재구성 중인 유닛은 지속 피해 앞에서 부활 여부를 정한다(적 전용 특성 설계 P3).
            ReconstructionTurnApplication.BeginSide(projection, owner, rules);

            PeriodicStatusApplication.BeginSide(input, projection, owner);

            var started = projection.Complete();
            if (started.Outcome.IsStageEnded || player || input.Battle?.NexusReinforcements == null)
                return started;

            var reinforcement = NexusReinforcementCalculator.TryCalculate(CombatCommandComposition.Project(started), livingAtStart);

            return reinforcement == null ? started : CombatCommandComposition.Merge(input, new[] { started, reinforcement });
        }

        internal static CombatCommandTransition EndEnemyPhase(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input.State.Phase == CombatFlowPhase.Finished) 
                return CombatCommandCalculator.Reject(input, "StageEnded");

            var before = input.State.Turn;
            if (before.StagePhase != StagePhase.Combat || before.ControllerPhase != CombatControllerPhase.Enemy ||
                input.State.Phase != CombatFlowPhase.Automatic) return CombatCommandCalculator.Reject(input, "WrongPhase");

            // 적 AI가 모든 행위자의 대기 또는 허용된 대안 소진을 확인한 뒤 호출한다.
            var turn = new CombatTurnFacts(StagePhase.Combat,
                CombatControllerPhase.StartingPlayer,
                before.ControllerTurnSequence, before.CompletedPlayerTurnCount,
                before.EnemyOrigins, before.Summons, checked(before.CompletedControllerTurnCount + 1));

            // 기믹 착지로 승패가 나더라도 이미 수락한 적 국면 종료와 누계 증가는 유지한다.
            var events = new List<CombatEvent>();

            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), events);

            events.Add(new CombatTurnEvent(CombatEventKind.EnemyPhaseEnded, EntityId.Empty, before, turn));

            var ended = projection.Complete(turn: turn, phase: CombatFlowPhase.Automatic);

            return CleanupControllerTurn(ResolveEndGimmick(ended, Owner.Enemy, rules), Owner.Enemy);
        }

        private static CombatCommandTransition CleanupControllerTurn(CombatCommandTransition ended, Owner owner)
        {
            if (ended.Outcome.IsStageEnded) 
                return ended;

            var after = CombatCommandComposition.Project(ended);
            var characters = after.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();

            TraitTurnLifecycle.EndAndRecord(after, characters, owner, events);

            var state = TraitTrailCalculator.EndControllerTurn(after.State, owner, events);

            RegenerationTurnApplication.EndOwnSide(after, CombatDamageApplication.For(after, characters, events), owner);

            RoarTurnApplication.EndControllerTurn(after, characters, owner, events);

            if (after.Characters.All(c => ReferenceEquals(c, characters[c.EntityId])) && ReferenceEquals(state, after.State)) 
                return ended;

            var cleaned = Changed(after, characters, state, events);

            return CombatCommandComposition.Merge(ended.Input, new[] { ended, cleaned });
        }

        private static CombatCommandTransition ResolveEndGimmick(CombatCommandTransition ended, Owner endedSide, IReadOnlyList<IMovementTileRule> rules)
        {
            var gimmick = CombatGimmickCalculator.TryAdvance(CombatCommandComposition.Project(ended),
                new StageGimmickBoundary(endedSide, ended.FinalState.Turn.CompletedControllerTurnCount), rules);

            return gimmick == null ? ended : CombatCommandComposition.Merge(ended.Input, new[] { ended, gimmick });
        }

        internal static CombatBattleFacts RequiredBattle(CombatCommandInput input) => input.Battle ??
            throw new InvalidOperationException("전투 설정·드로우 Buffer 사실이 필요합니다.");

        internal static CombatCommandState State(CombatCommandState source,CombatTurnFacts turn,CombatFlowPhase phase,CombatAllocationFacts allocation = null) => new(source.BoardWidth,source.BoardHeight,phase,source.Structures,                source.ThornFields.ToArray(),turn,source.Environment,source.EnvironmentProgress,                source.WallBlocking,source.Objective,allocation ?? source.Allocation,source.ItemRandomState,source.Rewards,source.TraitTurn,gimmicks: source.Gimmicks,
            reinforcements: source.Reinforcements);

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
