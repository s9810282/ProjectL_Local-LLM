using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories
{
    /// <summary>
    /// Category가 소유한 규칙의 저작 입력입니다.
    /// TraitSpec을 상속하지 않으므로 Character Trait 입력 경계에 타입상 들어갈 수 없습니다.
    /// </summary>
    public abstract class CategoryRuleSpec
    {
        public CategoryRuleKey Key { get; }

        protected CategoryRuleSpec(CategoryRuleKey key)
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
