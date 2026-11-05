using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>Stage 소유자가 제공할 불변 편집 가능 여부다. null을 준비 완료로 간주하지 않는다.</summary>
    public sealed class DeckEditAvailability
    {
        public DeckUnavailableReason Reason { get; }
        public bool CanEdit => Reason == DeckUnavailableReason.None;

        public DeckEditAvailability(DeckUnavailableReason reason)
        {
            if (!Enum.IsDefined(typeof(DeckUnavailableReason), reason)) throw new ArgumentOutOfRangeException(nameof(reason));
            Reason = reason;
        }
    }
}
