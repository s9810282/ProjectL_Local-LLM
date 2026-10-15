using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions
{
    /// <summary>
    /// Unit 종류에 설정되며 전투 중 변하지 않는 Category 규칙 값입니다.
    /// TraitDefinition을 상속하지 않습니다.
    /// </summary>
    public abstract class CategoryRuleDefinition
    {
        public CategoryRuleKey Key { get; }

        protected CategoryRuleDefinition(CategoryRuleKey key)
        {
            if (key == CategoryRuleKey.None ||
                Enum.IsDefined(typeof(CategoryRuleKey), key) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            Key = key;
        }
    }
}
