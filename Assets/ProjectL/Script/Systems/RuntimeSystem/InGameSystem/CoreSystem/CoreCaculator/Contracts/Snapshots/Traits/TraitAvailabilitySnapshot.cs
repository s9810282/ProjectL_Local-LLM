using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>
    /// 전투 밖에서 보는 특성 하나의 사용 가능 여부다. 사유 문자열은 전투 중 <see cref="TraitStatusSnapshot"/>과 같은 원본에서 온다.
    /// 빈 사유는 "제한 없음"이다.
    /// </summary>
    public sealed class TraitAvailabilitySnapshot
    {
        public TraitKey Key { get; }
        public bool IsAvailable => UnavailableReason.Length == 0;
        /// <summary>특성 전체를 쓸 수 없는 이유다. 비어 있으면 사용할 수 있다.</summary>
        public string UnavailableReason { get; }
        /// <summary>특성은 쓸 수 있지만 일부 구성 요소가 보류된 이유다.</summary>
        public string PartialUnavailableReason { get; }

        public TraitAvailabilitySnapshot(TraitKey key, string unavailableReason, string partialUnavailableReason)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            Key = key;
            UnavailableReason = unavailableReason ?? throw new ArgumentNullException(nameof(unavailableReason));
            PartialUnavailableReason = partialUnavailableReason ?? throw new ArgumentNullException(nameof(partialUnavailableReason));
        }
    }
}
