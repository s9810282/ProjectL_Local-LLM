using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Transitions
{
    internal sealed class CategoryTransitionBatch
    {
        internal IReadOnlyList<CategoryStateChange> StateChanges { get; }

        internal CategoryTransitionBatch(CategoryStateChange[] stateChanges)
        {
            if (stateChanges == null) throw new ArgumentNullException(nameof(stateChanges));
            var seen = new HashSet<CategoryRuleKey>();
            var copied = new CategoryStateChange[stateChanges.Length];
            for (int i = 0; i < copied.Length; i++)
            {
                if (seen.Add(stateChanges[i].Key) == false)
                    throw new ArgumentException(
                        $"Duplicate Category state change: {stateChanges[i].Key}.",
                        nameof(stateChanges));
                copied[i] = stateChanges[i];
            }

            Array.Sort(copied, (left, right) => left.Key.CompareTo(right.Key));
            StateChanges = Array.AsReadOnly(copied);
        }
    }
}
