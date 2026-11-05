using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 레벨에서 파생하는 기준 공격력·최대 체력 공식의 유일한 위치다. 정의 값은 레벨 1 값이고,
    /// 레벨이 1 오를 때마다 유닛 Category의 레벨당 상승치를 더한다. 이전 능력치에 누적하지 않고 (정의, 레벨)에서 매번 다시 계산한다.
    /// 메인은 무작위 성장 결과를 지속 상태에 저장하므로 이 공식을 쓰지 않는다. 영혼·위치·전투 임시 보정은 다루지 않는다.
    /// </summary>
    internal static class UnitLevelGrowthRules
    {
        internal static int AttackPower(UnitDefinition definition, int level)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return Grow(definition.AttackPower, definition.Category.LevelGrowth.AttackPerLevel, level);
        }

        internal static int MaxHealth(UnitDefinition definition, int level)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return Grow(definition.MaxHealth, definition.Category.LevelGrowth.MaxHealthPerLevel, level);
        }

        private static int Grow(int levelOneValue, int perLevel, int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));

            return checked(levelOneValue + (level - 1) * perLevel);
        }
    }
}
