using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions
{
    internal sealed class TraitTransitionBatch
    {
        public IReadOnlyList<TraitStateChange> StateChanges { get; }

        public TraitTransitionBatch(TraitStateChange[] stateChanges)
        {
            if (stateChanges == null)
                throw new ArgumentNullException(nameof(stateChanges));

            TraitKey duplicationKey = TraitKey.None;
            for (int i = 0; i < stateChanges.Length; i++)
            {
                if (stateChanges[i].TraitKey != duplicationKey)
                    duplicationKey = stateChanges[i].TraitKey;
                else
                    throw new ArgumentException("중복 키가 발견되었습니다.", nameof(stateChanges));
            }

            TraitStateChange[] copiedChanges = new TraitStateChange[stateChanges.Length];
            Array.Copy(stateChanges, copiedChanges, stateChanges.Length);

            StateChanges = Array.AsReadOnly(copiedChanges);
        }
    }
}