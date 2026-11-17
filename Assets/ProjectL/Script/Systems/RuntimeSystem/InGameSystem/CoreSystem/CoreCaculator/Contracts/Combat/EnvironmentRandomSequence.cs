using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>계산과 공개 근거 검증이 공유하는 환경 난수의 한 단계 계약이다. 상태는 호출자가 소유한다.</summary>
    public static class EnvironmentRandomSequence
    {
        public static int NextIndex(ref uint state, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
            // 기존 환경 난수열의 LCG와 거절 샘플링을 그대로 유지한다.
            uint bound = (uint)count, threshold = unchecked(0u - bound) % bound;
            do { state = unchecked(state * 1664525u + 1013904223u); } while (state < threshold);
            return (int)(state % bound);
        }
    }
}
