using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories
{
    /// <summary>
    /// 한 Category의 공통 규칙 설정입니다.
    /// 공격 모양은 지정 방식이, 관통 대상 수 제한은 관통 특성이 소유합니다.
    /// </summary>
    public sealed class CategorySpec
    {
        public CategoryKey Key { get; }
        public int MovementAllowance { get; }
        public ReadOnlyMemory<CategoryRuleSpec> RuleSpecs { get; }
        /// <summary>이 Category 유닛의 레벨당 능력치 상승치입니다. 생략하면 성장하지 않습니다.</summary>
        public CategoryLevelGrowth LevelGrowth { get; }

        public CategorySpec(CategoryKey key,IReadOnlyList<CategoryRuleSpec> ruleSpecs,            int movementAllowance,CategoryLevelGrowth levelGrowth = default)
        {
            if (Enum.IsDefined(typeof(CategoryKey), key) == false || key == CategoryKey.None)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (ruleSpecs == null)
                throw new ArgumentNullException(nameof(ruleSpecs));
            if (movementAllowance < 0)
                throw new ArgumentOutOfRangeException(nameof(movementAllowance));

            CategoryRuleSpec[] copied = new CategoryRuleSpec[ruleSpecs.Count];
            HashSet<CategoryRuleKey> seenKeys = new HashSet<CategoryRuleKey>();

            for (int i = 0; i < ruleSpecs.Count; i++)
            {
                CategoryRuleSpec ruleSpec = ruleSpecs[i];
                if (ruleSpec == null)
                {
                    throw new ArgumentException(
                        "Category 규칙 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(ruleSpecs));
                }

                if (seenKeys.Add(ruleSpec.Key) == false)
                {
                    throw new ArgumentException(
                        $"CategoryRuleKey {ruleSpec.Key}가 중복되었습니다.",
                        nameof(ruleSpecs));
                }

                copied[i] = ruleSpec;
            }

            Key = key;
            MovementAllowance = movementAllowance;

            RuleSpecs = copied;
            LevelGrowth = levelGrowth;
        }
    }
}
