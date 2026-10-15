using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class GameFlowSnapshot
    {
        public GameFlowPhase Phase { get; }
        public StageMapSnapshot Map { get; }
        public StageEntryId? CurrentEntryId { get; }
        public StageNodeId? CurrentNodeId { get; }
        public StageKey? CurrentStageKey { get; }
        public StageSnapshot CurrentStage { get; }
        public CompletedStageSnapshot LastCompletedStage { get; }
        public RunOutcome RunOutcome { get; }
        public PlayerWalletSnapshot Wallet { get; }

        public GameFlowSnapshot(
            GameFlowPhase phase,
            StageMapSnapshot map,
            StageEntryId? currentEntryId,
            StageNodeId? currentNodeId,
            StageKey? currentStageKey,
            StageSnapshot currentStage,
            CompletedStageSnapshot lastCompletedStage,
            RunOutcome runOutcome,
            PlayerWalletSnapshot wallet)
        {
            if (Enum.IsDefined(typeof(GameFlowPhase), phase) == false)
                throw new ArgumentOutOfRangeException(nameof(phase));
            if (phase == GameFlowPhase.NonCombatStageActive)
                throw new ArgumentException("GameFlowSnapshot does not support non-combat stages.", nameof(phase));
            if (Enum.IsDefined(typeof(RunOutcome), runOutcome) == false)
                throw new ArgumentOutOfRangeException(nameof(runOutcome));
            if (map == null)
                throw new ArgumentNullException(nameof(map));
            if (wallet == null)
                throw new ArgumentNullException(nameof(wallet));
            if (currentEntryId.HasValue && currentEntryId.Value.IsEmpty)
                throw new ArgumentException("CurrentEntryId는 Empty일 수 없습니다.", nameof(currentEntryId));
            if (currentNodeId.HasValue && currentNodeId.Value.IsEmpty)
                throw new ArgumentException("CurrentNodeId는 Empty일 수 없습니다.", nameof(currentNodeId));
            if (currentStageKey.HasValue && currentStageKey.Value.IsEmpty)
                throw new ArgumentException("CurrentStageKey는 Empty일 수 없습니다.", nameof(currentStageKey));

            bool hasAnyCurrent =
                currentEntryId.HasValue ||
                currentNodeId.HasValue ||
                currentStageKey.HasValue ||
                currentStage != null;
            bool hasAllCurrent =
                currentEntryId.HasValue &&
                currentNodeId.HasValue &&
                currentStageKey.HasValue &&
                currentStage != null;

            if (hasAnyCurrent != hasAllCurrent)
                throw new ArgumentException("현재 스테이지 식별자와 Snapshot은 모두 함께 존재해야 합니다.");

            switch (phase)
            {
                case GameFlowPhase.StageSelection:
                    RequireNoCurrentStage(hasAnyCurrent, phase);
                    RequireInProgressRun(runOutcome, phase);
                    break;

                case GameFlowPhase.StageActive:
                    RequireCurrentStage(hasAllCurrent, phase);
                    RequireInProgressRun(runOutcome, phase);
                    if (currentStage.Outcome.IsStageEnded)
                        throw new ArgumentException("StageActive에는 진행 중인 StageSnapshot이 필요합니다.", nameof(currentStage));
                    break;

                case GameFlowPhase.StageResult:
                    RequireCurrentStage(hasAllCurrent, phase);
                    RequireInProgressRun(runOutcome, phase);
                    if (currentStage.Outcome.IsStageEnded == false)
                        throw new ArgumentException("StageResult에는 종료된 StageSnapshot이 필요합니다.", nameof(currentStage));
                    break;

                case GameFlowPhase.RunEnded:
                    RequireNoCurrentStage(hasAnyCurrent, phase);
                    if (runOutcome == RunOutcome.InProgress)
                        throw new ArgumentException("RunEnded에는 종료 RunOutcome이 필요합니다.", nameof(runOutcome));
                    if (lastCompletedStage == null)
                        throw new ArgumentNullException(nameof(lastCompletedStage));
                    break;
            }

            Phase = phase;
            Map = map;
            CurrentEntryId = currentEntryId;
            CurrentNodeId = currentNodeId;
            CurrentStageKey = currentStageKey;
            CurrentStage = currentStage;
            LastCompletedStage = lastCompletedStage;
            RunOutcome = runOutcome;
            Wallet = wallet;
        }

        private static void RequireCurrentStage(bool hasAllCurrent, GameFlowPhase phase)
        {
            if (hasAllCurrent == false)
                throw new ArgumentException($"{phase}에는 현재 스테이지가 필요합니다.");
        }

        private static void RequireNoCurrentStage(bool hasAnyCurrent, GameFlowPhase phase)
        {
            if (hasAnyCurrent)
                throw new ArgumentException($"{phase}에는 현재 스테이지가 없어야 합니다.");
        }

        private static void RequireInProgressRun(RunOutcome runOutcome, GameFlowPhase phase)
        {
            if (runOutcome != RunOutcome.InProgress)
                throw new ArgumentException($"{phase}의 RunOutcome은 InProgress여야 합니다.", nameof(runOutcome));
        }
    }
}
