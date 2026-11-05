using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>중첩하지 않는 방어력 감소와 피해자 자신의 남은 턴 종료 횟수입니다.</summary>
    public sealed class DefenseReductionDebuffState
    {
        public int ReductionPercent
        {
            get;
        }
        public int RemainingOwnTurnEnds
        {
            get;
        }
        public DefenseReductionDebuffState(int reductionPercent, int remainingOwnTurnEnds)
        {
            if (reductionPercent < 1 || reductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(reductionPercent));
            if (remainingOwnTurnEnds < 1 || remainingOwnTurnEnds > 2)
                throw new ArgumentOutOfRangeException(nameof(remainingOwnTurnEnds));
            ReductionPercent = reductionPercent;
            RemainingOwnTurnEnds = remainingOwnTurnEnds;
        }
        public bool SameValues(DefenseReductionDebuffState other) => other != null &&
            ReductionPercent == other.ReductionPercent && RemainingOwnTurnEnds == other.RemainingOwnTurnEnds;
    }
}
