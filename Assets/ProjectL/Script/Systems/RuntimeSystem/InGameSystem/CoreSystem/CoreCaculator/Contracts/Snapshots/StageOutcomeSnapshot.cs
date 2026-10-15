using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct StageOutcomeSnapshot
    {
        public StageOutcome Outcome { get; }
        public StageOutcomeReason Reason { get; }
        public bool IsStageEnded => Outcome != StageOutcome.InProgress;

        public StageOutcomeSnapshot(StageOutcome outcome, StageOutcomeReason reason)
        {
            if (Enum.IsDefined(typeof(StageOutcome), outcome) == false)
                throw new ArgumentOutOfRangeException(nameof(outcome));
            if (Enum.IsDefined(typeof(StageOutcomeReason), reason) == false)
                throw new ArgumentOutOfRangeException(nameof(reason));

            if (outcome == StageOutcome.InProgress && reason != StageOutcomeReason.None)
                throw new ArgumentException("InProgress의 유효 조합이 아닙니다.", nameof(reason));
            if (outcome == StageOutcome.Victory && reason != StageOutcomeReason.EnemyHeadquartersDestroyed)
                throw new ArgumentException("Victory의 유효 조합이 아닙니다.", nameof(reason));
            if (outcome == StageOutcome.Defeat &&
                reason != StageOutcomeReason.PlayerHeadquartersDestroyed &&
                reason != StageOutcomeReason.NoPlayerUnitsRemaining &&
                reason != StageOutcomeReason.PlayerMainCharacterDied &&
                reason != StageOutcomeReason.VoluntaryAbandonment)
                throw new ArgumentException("Defeat의 유효 조합이 아닙니다.", nameof(reason));

            Outcome = outcome;
            Reason = reason;
        }
    }
}
