using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public abstract class TraitStatusSnapshot
    {
        public TraitKey Key { get; }
        public bool IsAvailable => UnavailableReason.Length == 0;
        public string UnavailableReason { get; private set; } = string.Empty;
        public string PartialUnavailableReason { get; private set; } = string.Empty;

        /// <summary>설정과 상태를 보존한 채 기능의 사용 가능 사유만 덧붙인 불변 사본이다.</summary>
        public TraitStatusSnapshot WithAvailability(string unavailableReason, string partialUnavailableReason = "")
        {
            if (unavailableReason == null) throw new ArgumentNullException(nameof(unavailableReason));
            if (partialUnavailableReason == null) throw new ArgumentNullException(nameof(partialUnavailableReason));
            var copy = (TraitStatusSnapshot)MemberwiseClone();
            copy.UnavailableReason = unavailableReason;
            copy.PartialUnavailableReason = partialUnavailableReason;
            return copy;
        }

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
