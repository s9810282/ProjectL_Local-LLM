using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories
{
    /// <summary>
    /// 한 Category의 레벨당 능력치 상승치입니다. 레벨이 1 오를 때마다 더하는 양이며 기본값은 성장 없음입니다.
    /// 레벨에 적용하는 공식은 Application의 UnitLevelGrowthRules가 가집니다.
    /// </summary>
    public readonly struct CategoryLevelGrowth
    {
        public int MaxHealthPerLevel { get; }
        public int AttackPerLevel { get; }

        public CategoryLevelGrowth(int maxHealthPerLevel, int attackPerLevel)
        {
            if (maxHealthPerLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealthPerLevel));
            if (attackPerLevel < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPerLevel));

            MaxHealthPerLevel = maxHealthPerLevel;
            AttackPerLevel = attackPerLevel;
        }
    }
}
