using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// Category 상성 배율입니다. 실수 대신 정수 비율로 보존합니다.
    /// </summary>
    public readonly struct CategoryMatchupMultiplier
    {
        public int Numerator { get; }
        public int Denominator { get; }

        public CategoryMatchupMultiplier(int numerator, int denominator)
        {
            if (numerator < 0)
                throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(denominator));

            Numerator = numerator;
            Denominator = denominator;
        }
    }
}
