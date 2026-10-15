using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States
{
    internal sealed class UnitCategoryState
    {
        private readonly Dictionary<CategoryRuleKey, CategoryRuntimeState> _states;

        internal UnitCategoryState(CategoryRuntimeState[] runtimeStates)
        {
            if (runtimeStates == null) throw new ArgumentNullException(nameof(runtimeStates));
            _states = new Dictionary<CategoryRuleKey, CategoryRuntimeState>(runtimeStates.Length);
            for (int i = 0; i < runtimeStates.Length; i++)
            {
                CategoryRuntimeState state = runtimeStates[i];
                if (state == null)
                    throw new ArgumentException($"Category state {i} is null.", nameof(runtimeStates));
                if (_states.ContainsKey(state.Key))
                    throw new ArgumentException($"Duplicate Category state: {state.Key}.", nameof(runtimeStates));
                _states.Add(state.Key, state);
            }
        }

        internal bool TryGetRuntimeState(
            CategoryRuleKey key,
            out CategoryRuntimeState runtimeState)
        {
            if (key == CategoryRuleKey.None || Enum.IsDefined(typeof(CategoryRuleKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            return _states.TryGetValue(key, out runtimeState);
        }

        internal T GetRequired<T>(CategoryRuleKey key) where T : CategoryRuntimeState
        {
            if (TryGetRuntimeState(key, out CategoryRuntimeState state) == false)
                throw new InvalidOperationException($"Category state {key} was not found.");
            if (state is not T typed)
                throw new InvalidOperationException($"Category state {key} has an unexpected type.");
            return typed;
        }

        internal UnitCategoryState Clone()
        {
            var states = new CategoryRuntimeState[_states.Count];
            int index = 0;
            foreach (CategoryRuntimeState state in _states.Values)
                states[index++] = state.Clone();
            return new UnitCategoryState(states);
        }

        internal void Apply(CategoryTransitionBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            UnitCategoryState projected = Clone();
            projected.ApplyPrevalidated(batch);
            foreach (KeyValuePair<CategoryRuleKey, CategoryRuntimeState> pair in projected._states)
                _states[pair.Key].CopyFinalValuesFrom(pair.Value);
        }

        internal void ApplyPrevalidated(CategoryTransitionBatch batch)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            foreach (CategoryStateChange change in batch.StateChanges)
            {
                if (_states.TryGetValue(change.Key, out CategoryRuntimeState state) == false)
                    throw new InvalidOperationException($"Category state {change.Key} was not found.");
                state.CopyFinalValuesFrom(change.FinalState);
            }
        }
    }
}
