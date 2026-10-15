using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// 공격자/피격자 방향성을 가진 상성표의 한 항목입니다.
    /// </summary>
    public readonly struct CategoryMatchupEntry
    {
        public CategoryKey Attacker { get; }
        public CategoryKey Defender { get; }
        public int Numerator { get; }
        public int Denominator { get; }

        public CategoryMatchupEntry(
            CategoryKey attacker,
            CategoryKey defender,
            int numerator,
            int denominator)
        {
            ValidateCategory(attacker, nameof(attacker));
            ValidateCategory(defender, nameof(defender));
            if (numerator < 0)
                throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(denominator));

            Attacker = attacker;
            Defender = defender;
            Numerator = numerator;
            Denominator = denominator;
        }

        private static void ValidateCategory(CategoryKey key, string parameterName)
        {
            if (key == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), key) == false)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
