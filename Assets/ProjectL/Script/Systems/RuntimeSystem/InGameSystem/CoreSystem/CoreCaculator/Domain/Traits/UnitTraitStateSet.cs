using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits
{
    public sealed class UnitTraitStateSet
    {
        private readonly Dictionary<TraitKey, TraitRuntimeState> _runtimeStates;

        public UnitTraitStateSet(TraitRuntimeState[] runtimeStates)
        {
            _runtimeStates = new Dictionary<TraitKey, TraitRuntimeState>();

            if (runtimeStates != null)
            {
                foreach (var runtimeState in runtimeStates)
                {
                    if (runtimeState == null)
                        throw new ArgumentNullException(nameof(runtimeState));
                    if (runtimeState.TraitKey == TraitKey.None)
                        throw new ArgumentException(nameof(runtimeState.TraitKey));
                    if (_runtimeStates.ContainsKey(runtimeState.TraitKey))
                        throw new ArgumentException(nameof(runtimeState.TraitKey));

                    _runtimeStates.Add(runtimeState.TraitKey, runtimeState);
                }
            }
        }

        internal bool TryGetRuntimeState(TraitKey traitKey, out TraitRuntimeState runtimeState)
        {
            if (traitKey == TraitKey.None)
                throw new ArgumentException(nameof(traitKey));

            return _runtimeStates.TryGetValue(traitKey, out runtimeState);
        }

        internal T GetRequired<T>(TraitKey traitKey) where T : TraitRuntimeState
        {
            if (traitKey == TraitKey.None)
                throw new ArgumentException(nameof(traitKey));

            if (_runtimeStates.ContainsKey(traitKey) == false)
                throw new InvalidOperationException(nameof(traitKey));

            if (_runtimeStates[traitKey] is not T required)
                throw new InvalidOperationException("키 값과 원하는 타입이 서로 다릅니다. ");

            return required;
        }

        internal UnitTraitStateSet Clone()
        {
            List<TraitRuntimeState> cloneStates = new List<TraitRuntimeState>(_runtimeStates.Count);

            foreach(TraitRuntimeState runtimeState in _runtimeStates.Values)
            {
                cloneStates.Add(runtimeState.Clone());
            }

            return new UnitTraitStateSet(cloneStates.ToArray());
        }

        internal void Apply(TraitTransitionBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            UnitTraitStateSet copiedStateSet = Clone();

            foreach (TraitStateChange change in batch.StateChanges)
            {
                if (change.HasChange == false)
                    continue;

                if (copiedStateSet._runtimeStates.TryGetValue(change.TraitKey, out TraitRuntimeState copiedState) == false)
                    throw new InvalidOperationException($"특성 상태를 찾을 수 없습니다. {change.TraitKey}");

                copiedState.CopyFinalValuesFrom(change.FinalState);
            }

            foreach (var pair in copiedStateSet._runtimeStates)
            {
                _runtimeStates[pair.Key].CopyFinalValuesFrom(pair.Value);
            }
        }

        internal void ApplyPrevalidated(TraitTransitionBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));

            foreach (TraitStateChange change in batch.StateChanges)
            {
                if (change.HasChange == false)
                    continue;

                if (_runtimeStates.TryGetValue(change.TraitKey, out TraitRuntimeState runtimeState) == false)
                    throw new InvalidOperationException($"특성 상태를 찾을 수 없습니다. {change.TraitKey}");

                runtimeState.CopyFinalValuesFrom(change.FinalState);
            }
        }
    }
}
