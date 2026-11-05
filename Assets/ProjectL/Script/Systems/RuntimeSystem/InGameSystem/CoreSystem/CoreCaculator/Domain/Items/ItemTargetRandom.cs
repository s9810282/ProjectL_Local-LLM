using System;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items
{
    public static class ItemTargetRandom
    {
        public static int NextIndex(uint state, int exclusiveMaximum, out uint finalState)
        {
            if (exclusiveMaximum < 1) throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
            finalState = state;
            if (exclusiveMaximum == 1) return 0;
            uint bound = (uint)exclusiveMaximum;
            uint threshold = unchecked(0u - bound) % bound;
            // 0을 포함한 전 상태를 순회하며, 불완전한 나머지 구간은 버려 선택 편향을 없앤다.
            do
            {
                finalState = unchecked(finalState * 1664525u + 1013904223u);
            }
            while (finalState < threshold);
            return (int)(finalState % bound);
        }
    }
}
