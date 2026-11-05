using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    public sealed class DeckReadResult
    {
        public DeckReadStatus Status { get; }
        public DeckEditorSnapshot Snapshot { get; }

        public DeckReadResult(DeckReadStatus status, DeckEditorSnapshot snapshot = null)
        {
            if (!Enum.IsDefined(typeof(DeckReadStatus), status)) throw new ArgumentOutOfRangeException(nameof(status));
            if ((status == DeckReadStatus.Ready) != (snapshot != null))
                throw new ArgumentException("Ready만 일관된 스냅샷을 가져야 합니다.", nameof(snapshot));
            Status = status;
            Snapshot = snapshot;
        }
    }
}
