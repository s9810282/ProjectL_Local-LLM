using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Transitions
{
    internal readonly struct CategoryStateChange
    {
        internal CategoryRuleKey Key { get; }
        internal CategoryRuntimeState FinalState { get; }

        private CategoryStateChange(CategoryRuleKey key, CategoryRuntimeState finalState)
        {
            Key = key;
            FinalState = finalState;
        }

        internal static CategoryStateChange Change(
            CategoryRuleKey key,
            CategoryRuntimeState finalState)
        {
            if (key == CategoryRuleKey.None || Enum.IsDefined(typeof(CategoryRuleKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (finalState == null) throw new ArgumentNullException(nameof(finalState));
            if (finalState.Key != key)
                throw new ArgumentException("Category state key does not match the change key.", nameof(finalState));
            return new CategoryStateChange(key, finalState.Clone());
        }
    }
}
