using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public abstract class TraitStatusSnapshot
    {
        public TraitKey Key { get; }
        protected TraitStatusSnapshot(TraitKey key)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            Key = key;
        }

        /// <summary>같은 특성의 현재 상태 값이 모두 같은지 비교한다. 새 특성은 자기 값만 비교하면 되며 비교기를 고치지 않는다.</summary>
        public abstract bool SameValues(TraitStatusSnapshot other);
    }
}