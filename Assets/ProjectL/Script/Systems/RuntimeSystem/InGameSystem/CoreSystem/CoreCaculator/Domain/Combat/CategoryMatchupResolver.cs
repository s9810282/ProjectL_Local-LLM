using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 상성 배율의 순수 조회입니다. 피해 계산 단계 연결은 Application이 담당합니다.
    /// </summary>
    public sealed class CategoryMatchupResolver
    {
        private readonly CategoryMatchupSpec _spec;

        public CategoryMatchupResolver(CategoryMatchupSpec spec)
        {
            _spec = spec ?? throw new ArgumentNullException(nameof(spec));
        }

        public CategoryMatchupMultiplier GetMultiplier(CategoryKey attacker, CategoryKey defender)
        {
            ValidateCategory(attacker, nameof(attacker));
            ValidateCategory(defender, nameof(defender));

            if (_spec.TryGetEntry(attacker, defender, out CategoryMatchupEntry entry) == false)
            {
                throw new InvalidOperationException(
                    $"상성 항목 {attacker}->{defender}를 찾을 수 없습니다.");
            }

            return new CategoryMatchupMultiplier(entry.Numerator, entry.Denominator);
        }

        private static void ValidateCategory(CategoryKey key, string parameterName)
        {
            if (key == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), key) == false)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
