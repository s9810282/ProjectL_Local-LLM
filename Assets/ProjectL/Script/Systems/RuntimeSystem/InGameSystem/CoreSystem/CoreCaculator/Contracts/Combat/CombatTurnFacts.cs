using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public readonly struct CombatEnemyOrigin
    {
        public EntityId EntityId { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        public CombatEnemyOrigin(EntityId id, CellCoord anchor, Rotation4 rotation)
        { EntityId = id; Anchor = anchor; Rotation = rotation; }
    }

    /// <summary>기존 전투 Buffer가 보관하는 실제 턴 값이다. Core는 호출 동안만 읽는다.</summary>
    public sealed class CombatTurnFacts
    {
        public StagePhase StagePhase { get; }
        public CombatControllerPhase ControllerPhase { get; }
        public long ControllerTurnSequence { get; }
        public long CompletedPlayerTurnCount { get; }
        /// <summary>플레이어·적 진영의 수락된 종료 누계다. 새 전투에서 0으로 시작한다.</summary>
        public long CompletedControllerTurnCount { get; }
        public Owner? Controller => ControllerPhase == CombatControllerPhase.Player || ControllerPhase == CombatControllerPhase.StartingPlayer
            ? Owner.Player : ControllerPhase == CombatControllerPhase.Enemy || ControllerPhase == CombatControllerPhase.StartingEnemy
                ? Owner.Enemy : (Owner?)null;
        public IReadOnlyList<CombatEnemyOrigin> EnemyOrigins { get; }
        /// <summary>전투 중 소환으로 만든 개체의 출처다. 전투 동안 추가만 하며 Buffer가 삭제·변경을 거절한다.</summary>
        public IReadOnlyList<CombatSummonRecord> Summons { get; }

        /// <param name="summons">생략하면 빈 목록이다. 운영 생성 지점은 원본 기록을 그대로 넘긴다.</param>
        public CombatTurnFacts(StagePhase stagePhase,
            CombatControllerPhase controllerPhase = CombatControllerPhase.None, long controllerTurnSequence = 0,
            long completedPlayerTurnCount = 0,
            IReadOnlyList<CombatEnemyOrigin> enemyOrigins = null, IReadOnlyList<CombatSummonRecord> summons = null,
            long completedControllerTurnCount = 0)
        {
            if (!Enum.IsDefined(typeof(StagePhase), stagePhase) ||
                !Enum.IsDefined(typeof(CombatControllerPhase), controllerPhase) || controllerTurnSequence < 0 || completedControllerTurnCount < 0 ||
                completedPlayerTurnCount < 0)
                throw new ArgumentException("턴 사실 값이 잘못되었습니다.");
            if (stagePhase == StagePhase.Deployment && controllerPhase != CombatControllerPhase.None ||
                stagePhase == StagePhase.Combat && controllerPhase == CombatControllerPhase.None)
                throw new ArgumentException("배치·전투 국면과 제어자 단계가 일치해야 합니다.");
            StagePhase = stagePhase;
            ControllerPhase = controllerPhase; ControllerTurnSequence = controllerTurnSequence;
            CompletedPlayerTurnCount = completedPlayerTurnCount;
            CompletedControllerTurnCount = completedControllerTurnCount;
            EnemyOrigins = Array.AsReadOnly((enemyOrigins ?? Array.Empty<CombatEnemyOrigin>()).ToArray());
            Summons = Array.AsReadOnly((summons ?? Array.Empty<CombatSummonRecord>()).ToArray());
            if (EnemyOrigins.Select(p => p.EntityId).Distinct().Count() != EnemyOrigins.Count ||
                Summons.Select(s => s.SummonedId).Distinct().Count() != Summons.Count ||
                Summons.Any(s => EnemyOrigins.Any(o => o.EntityId == s.SummonedId)))
                throw new ArgumentException("적 원본·소환 기록이 중복되었거나 겹칩니다.");
        }
        public bool SameValues(CombatTurnFacts other) => other != null && StagePhase == other.StagePhase &&
            ControllerPhase == other.ControllerPhase && ControllerTurnSequence == other.ControllerTurnSequence &&
            CompletedPlayerTurnCount == other.CompletedPlayerTurnCount &&
            CompletedControllerTurnCount == other.CompletedControllerTurnCount &&
            EnemyOrigins.SequenceEqual(other.EnemyOrigins) && Summons.SequenceEqual(other.Summons);
    }
}
