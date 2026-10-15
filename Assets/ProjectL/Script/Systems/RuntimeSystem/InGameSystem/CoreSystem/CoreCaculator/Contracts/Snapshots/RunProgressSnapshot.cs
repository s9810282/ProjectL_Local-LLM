using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class RunProgressSnapshot
    {
        public RunId RunId { get; }
        public long Revision { get; }
        public GameFlowPhase Phase { get; }
        public RunOutcome RunOutcome { get; }
        public EntityKey MainCharacterKey { get; }
        public HealthStateSnapshot MainCharacterHealth { get; }
        public StageEntryId? CurrentEntryId { get; }
        public StageNodeId? CurrentNodeId { get; }
        public StageKey? CurrentStageKey { get; }
        public CompletedStageSnapshot LastCompletedStage { get; }
        public PlayerWalletSnapshot Wallet { get; }
        public NonCombatStageKind? CurrentNonCombatKind { get; }

        public RunProgressSnapshot(RunId runId, long revision, GameFlowPhase phase, RunOutcome runOutcome,
            EntityKey mainCharacterKey, HealthStateSnapshot mainCharacterHealth,
            StageEntryId? currentEntryId, StageNodeId? currentNodeId, StageKey? currentStageKey,
            CompletedStageSnapshot lastCompletedStage, PlayerWalletSnapshot wallet,
            NonCombatStageKind? currentNonCombatKind)
        {
            if (runId.IsEmpty) throw new ArgumentException(nameof(runId));
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (!Enum.IsDefined(typeof(GameFlowPhase), phase)) throw new ArgumentOutOfRangeException(nameof(phase));
            if (!Enum.IsDefined(typeof(RunOutcome), runOutcome)) throw new ArgumentOutOfRangeException(nameof(runOutcome));
            if (mainCharacterKey != EntityKey.Unit_MainCharacter) throw new ArgumentException(nameof(mainCharacterKey));
            if (mainCharacterHealth.MaxHealth < 1 || mainCharacterHealth.CurrentHealth < 0 ||
                mainCharacterHealth.CurrentHealth > mainCharacterHealth.MaxHealth)
                throw new ArgumentException(nameof(mainCharacterHealth));
            if (currentEntryId.HasValue && currentEntryId.Value.IsEmpty) throw new ArgumentException(nameof(currentEntryId));
            if (currentNodeId.HasValue && currentNodeId.Value.IsEmpty) throw new ArgumentException(nameof(currentNodeId));
            if (currentStageKey.HasValue && currentStageKey.Value.IsEmpty) throw new ArgumentException(nameof(currentStageKey));
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (currentNonCombatKind.HasValue &&
                !Enum.IsDefined(typeof(NonCombatStageKind), currentNonCombatKind.Value))
                throw new ArgumentOutOfRangeException(nameof(currentNonCombatKind));
            if (currentNonCombatKind.HasValue != (phase == GameFlowPhase.NonCombatStageActive))
                throw new ArgumentException("Only an active non-combat entry has a content kind.", nameof(currentNonCombatKind));

            bool hasAny = currentEntryId.HasValue || currentNodeId.HasValue || currentStageKey.HasValue;
            bool hasAll = currentEntryId.HasValue && currentNodeId.HasValue && currentStageKey.HasValue;
            bool needsCurrent = phase == GameFlowPhase.StageActive || phase == GameFlowPhase.StageResult ||
                phase == GameFlowPhase.NonCombatStageActive;
            if (hasAny != hasAll || hasAll != needsCurrent)
                throw new ArgumentException("현재 Entry/Node/Stage는 활성/결과 Phase에서만 함께 존재해야 합니다.");
            if ((phase == GameFlowPhase.RunEnded) != (runOutcome != RunOutcome.InProgress))
                throw new ArgumentException("Phase와 RunOutcome이 일치하지 않습니다.");

            // StageActive의 HP0은 마지막 전투 행동 후 Run 완료 인계 전에도 조회할 수 있다.
            if ((phase == GameFlowPhase.StageSelection || phase == GameFlowPhase.StageResult ||
                 phase == GameFlowPhase.NonCombatStageActive ||
                 runOutcome == RunOutcome.Victory) && mainCharacterHealth.IsDead)
                throw new ArgumentException("진행/승리 상태의 주인공은 생존해야 합니다.");
            if (runOutcome == RunOutcome.Defeat && !mainCharacterHealth.IsDead)
                throw new ArgumentException("패배 상태의 주인공 HP는 0이어야 합니다.");

            if (phase == GameFlowPhase.StageResult || phase == GameFlowPhase.RunEnded)
            {
                if (lastCompletedStage == null) throw new ArgumentNullException(nameof(lastCompletedStage));
                if (lastCompletedStage.ExecutionKind != StageExecutionKind.Combat)
                    throw new ArgumentException("Combat result and RunEnded require a combat receipt.", nameof(lastCompletedStage));
                StageOutcome expectedOutcome = runOutcome == RunOutcome.Defeat ? StageOutcome.Defeat : StageOutcome.Victory;
                if (lastCompletedStage.Outcome.Outcome != expectedOutcome)
                    throw new ArgumentException("완료 receipt와 Run 결과가 일치하지 않습니다.");
                if (phase == GameFlowPhase.StageResult &&
                    (currentEntryId.Value != lastCompletedStage.EntryId ||
                     currentNodeId.Value != lastCompletedStage.NodeId || currentStageKey.Value != lastCompletedStage.StageKey))
                    throw new ArgumentException("결과 Phase의 현재 식별자는 완료 receipt와 같아야 합니다.");
            }

            RunId = runId;
            Revision = revision;
            Phase = phase;
            RunOutcome = runOutcome;
            MainCharacterKey = mainCharacterKey;
            MainCharacterHealth = mainCharacterHealth;
            CurrentEntryId = currentEntryId;
            CurrentNodeId = currentNodeId;
            CurrentStageKey = currentStageKey;
            LastCompletedStage = lastCompletedStage;
            Wallet = wallet;
            CurrentNonCombatKind = currentNonCombatKind;
        }
    }
}
