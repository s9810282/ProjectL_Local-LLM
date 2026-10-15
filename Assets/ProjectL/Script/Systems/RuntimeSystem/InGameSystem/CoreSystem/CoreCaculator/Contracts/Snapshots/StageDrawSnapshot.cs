using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class StageDrawSnapshot
    {
        public int RemainingCount { get; }
        public bool CanManualDraw { get; }
        public CharacterDrawFailureReason ManualDrawFailureReason { get; }

        public StageDrawSnapshot(int remainingCount, bool canManualDraw, CharacterDrawFailureReason manualDrawFailureReason)
        {
            if (remainingCount < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingCount));
            if (!Enum.IsDefined(typeof(CharacterDrawFailureReason), manualDrawFailureReason))
                throw new ArgumentOutOfRangeException(nameof(manualDrawFailureReason));
            if (canManualDraw != (manualDrawFailureReason == CharacterDrawFailureReason.None) ||
                (canManualDraw && remainingCount == 0) ||
                (manualDrawFailureReason == CharacterDrawFailureReason.DeckEmpty && remainingCount != 0))
                throw new ArgumentException("뽑기 가능 여부, 잔여 수량과 실패 이유가 일치하지 않습니다.");

            RemainingCount = remainingCount;
            CanManualDraw = canManualDraw;
            ManualDrawFailureReason = manualDrawFailureReason;
        }
    }
}
