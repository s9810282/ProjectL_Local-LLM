using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public readonly struct CombatTurnParticipant
    {
        public EntityId EntityId { get; }
        public int Priority { get; }
        public int Delay { get; }
        public long NextActionTime { get; }
        public bool OpeningTurnPending { get; }
        public int CompletedTurnCount { get; }
        public CombatTurnParticipant(EntityId id, int priority, int delay, long nextActionTime = 0,
            bool openingTurnPending = true, int completedTurnCount = 0)
        {
            if (id.IsEmpty || priority < 1 || delay < 1 || nextActionTime < 0 || completedTurnCount < 0)
                throw new ArgumentException("타임라인 참가자 값이 잘못되었습니다.");
            EntityId = id; Priority = priority; Delay = delay; NextActionTime = nextActionTime;
            OpeningTurnPending = openingTurnPending; CompletedTurnCount = completedTurnCount;
        }
    }

    public readonly struct CombatEnemyOrigin
    {
        public EntityId EntityId { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        public CombatEnemyOrigin(EntityId id, CellCoord anchor, Rotation4 rotation)
        { EntityId = id; Anchor = anchor; Rotation = rotation; }
    }

    public readonly struct CombatGimmickFacts
    {
        public StageGimmickId Id { get; }
        public long NextActivationTurn { get; }
        public int ActivationCount { get; }
        public CombatGimmickFacts(StageGimmickId id, long nextActivationTurn, int activationCount = 0)
        {
            if (nextActivationTurn < 1 || activationCount < 0) throw new ArgumentOutOfRangeException(nameof(nextActivationTurn));
            Id = id; NextActivationTurn = nextActivationTurn; ActivationCount = activationCount;
        }
    }

    /// <summary>기존 전투 Buffer가 보관하는 실제 턴·밤 값이다. Core는 호출 동안만 읽는다.</summary>
    public sealed class CombatTurnFacts
    {
        public StagePhase StagePhase { get; }
        public int PreparationIndex { get; }
        public bool MainPlacementLocked { get; }
        public long CurrentTime { get; }
        public long CurrentNightTime { get; }
        public long CompletedUnitTurnCount { get; }
        public IReadOnlyList<CombatTurnParticipant> Participants { get; }
        public IReadOnlyList<CombatEnemyOrigin> EnemyOrigins { get; }
        public IReadOnlyList<CombatGimmickFacts> Gimmicks { get; }
        public CombatTurnFacts(StagePhase stagePhase, int preparationIndex = 1, bool mainPlacementLocked = false,
            long currentTime = 0, long currentNightTime = 0, long completedUnitTurnCount = 0,
            IReadOnlyList<CombatTurnParticipant> participants = null, IReadOnlyList<CombatEnemyOrigin> enemyOrigins = null,
            IReadOnlyList<CombatGimmickFacts> gimmicks = null)
        {
            if (!Enum.IsDefined(typeof(StagePhase), stagePhase) || preparationIndex < 1 || currentTime < 0 ||
                currentNightTime < 0 || completedUnitTurnCount < 0) throw new ArgumentException("턴·밤 사실 값이 잘못되었습니다.");
            StagePhase = stagePhase; PreparationIndex = preparationIndex; MainPlacementLocked = mainPlacementLocked;
            CurrentTime = currentTime; CurrentNightTime = currentNightTime; CompletedUnitTurnCount = completedUnitTurnCount;
            Participants = Array.AsReadOnly((participants ?? Array.Empty<CombatTurnParticipant>()).ToArray());
            EnemyOrigins = Array.AsReadOnly((enemyOrigins ?? Array.Empty<CombatEnemyOrigin>()).ToArray());
            Gimmicks = Array.AsReadOnly((gimmicks ?? Array.Empty<CombatGimmickFacts>()).ToArray());
            if (Participants.Select(p => p.EntityId).Distinct().Count() != Participants.Count ||
                EnemyOrigins.Select(p => p.EntityId).Distinct().Count() != EnemyOrigins.Count ||
                Gimmicks.Select(g => g.Id).Distinct().Count() != Gimmicks.Count)
                throw new ArgumentException("턴 참가자·적 원본·기믹 식별자가 중복됩니다.");
        }
        public bool SameValues(CombatTurnFacts other) => other != null && StagePhase == other.StagePhase &&
            PreparationIndex == other.PreparationIndex && MainPlacementLocked == other.MainPlacementLocked &&
            CurrentTime == other.CurrentTime && CurrentNightTime == other.CurrentNightTime &&
            CompletedUnitTurnCount == other.CompletedUnitTurnCount && Participants.SequenceEqual(other.Participants) &&
            EnemyOrigins.SequenceEqual(other.EnemyOrigins) && Gimmicks.SequenceEqual(other.Gimmicks);
    }
}
