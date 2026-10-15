using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct EnemyAiTurnResult
    {
        public bool Succeeded { get; }
        public EnemyAiTurnFailureReason FailureReason { get; }
        public EntityId? UnitId { get; }
        public MovementResult? Movement { get; }
        public AttackResult? Attack { get; }
        public CombatTurnResult? EndTurn { get; }
        public StageSnapshot CurrentStage { get; }

        private EnemyAiTurnResult(
            bool succeeded,
            EnemyAiTurnFailureReason failureReason,
            EntityId? unitId,
            MovementResult? movement,
            AttackResult? attack,
            CombatTurnResult? endTurn,
            StageSnapshot stageSnapshot)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            UnitId = unitId;
            Movement = movement;
            Attack = attack;
            EndTurn = endTurn;
            CurrentStage = stageSnapshot;
        }

        /// <summary>
        /// AI 턴을 시작 못함
        /// </summary>
        public static EnemyAiTurnResult Rejected(EnemyAiTurnFailureReason failureReason, EntityId? unitId, StageSnapshot currentStage)
        {
            if (failureReason != EnemyAiTurnFailureReason.NoActiveUnit &&
                failureReason != EnemyAiTurnFailureReason.ActiveUnitNotEnemy &&
                failureReason != EnemyAiTurnFailureReason.StageEnded)
                throw new ArgumentOutOfRangeException(nameof(failureReason), failureReason, 
                    "Rejected는 NoActiveUnit 또는 ActiveUnitNotEnemy, StageEnded만 사용할 수 있습니다.");

            if (failureReason == EnemyAiTurnFailureReason.NoActiveUnit && unitId.HasValue)
                throw new ArgumentException("NoActiveUnit에서는 UnitId가 없어야 합니다.", nameof(unitId));

            if (failureReason == EnemyAiTurnFailureReason.ActiveUnitNotEnemy && (!unitId.HasValue || unitId.Value == EntityId.Empty))
                throw new ArgumentException("ActiveUnitNotEnemy에서는 유효한 UnitId가 필요합니다.", nameof(unitId));

            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));

            return new EnemyAiTurnResult(false, failureReason, unitId, null, null, null, currentStage);
        }

        /// <summary>
        /// AI가 자신의 턴을 인수해서 끝까지 처리한 경우 (정상적 종료)
        /// </summary>
        public static EnemyAiTurnResult Completed(EntityId unitId, MovementResult? movement, AttackResult? attack, CombatTurnResult endTurn)
        {
            if (unitId == EntityId.Empty)
                throw new ArgumentException("UnitId는 Empty일 수 없습니다.", nameof(unitId));

            if (endTurn.CurrentStage == null) 
                throw new ArgumentException( "EndTurn의 CurrentStage는 null일 수 없습니다.", nameof(endTurn));

            if (endTurn.Succeeded == true)
            {
                return new EnemyAiTurnResult(true, EnemyAiTurnFailureReason.None, unitId, movement, attack, endTurn, endTurn.CurrentStage);
            }
            else
            {
                return new EnemyAiTurnResult(false, EnemyAiTurnFailureReason.EndTurnFailed, unitId, movement, attack, endTurn, endTurn.CurrentStage);
            }
        }

        public static EnemyAiTurnResult CompletedByStageEnd(EntityId actorId, MovementResult? movement, AttackResult? attack, StageSnapshot currentStage)
        {
            if (actorId == EntityId.Empty)
                throw new ArgumentException("UnitId는 Empty일 수 없습니다.", nameof(actorId));

            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));

            return new EnemyAiTurnResult(true, EnemyAiTurnFailureReason.None, actorId, movement, attack, null, currentStage);
        }

        public static EnemyAiTurnResult CompletedByActorDeath(
            EntityId actorId,
            MovementResult? movement,
            AttackResult? attack,
            StageSnapshot currentStage)
        {
            if (actorId == EntityId.Empty)
                throw new ArgumentException("UnitId는 Empty일 수 없습니다.", nameof(actorId));
            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage));

            return new EnemyAiTurnResult(
                true,
                EnemyAiTurnFailureReason.None,
                actorId,
                movement,
                attack,
                null,
                currentStage);
        }
    }
}
