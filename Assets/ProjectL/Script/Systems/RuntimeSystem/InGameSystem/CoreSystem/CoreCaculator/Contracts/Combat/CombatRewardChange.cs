using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 잔액과 보상 반영 후 잔액을 함께 게시하는 불변 제안이다.</summary>
    public sealed class CombatRewardChange
    {
        public int ExpectedGold { get; }
        public int FinalGold { get; }
        public int ExpectedFame { get; }
        public int FinalFame { get; }

        public CombatRewardChange(int expectedGold, int finalGold, int expectedFame, int finalFame)
        {
            if (expectedGold < 0) throw new ArgumentOutOfRangeException(nameof(expectedGold));
            if (finalGold < 0) throw new ArgumentOutOfRangeException(nameof(finalGold));
            if (expectedFame < 0) throw new ArgumentOutOfRangeException(nameof(expectedFame));
            if (finalFame < 0) throw new ArgumentOutOfRangeException(nameof(finalFame));
            ExpectedGold = expectedGold;
            FinalGold = finalGold;
            ExpectedFame = expectedFame;
            FinalFame = finalFame;
        }
    }
}
