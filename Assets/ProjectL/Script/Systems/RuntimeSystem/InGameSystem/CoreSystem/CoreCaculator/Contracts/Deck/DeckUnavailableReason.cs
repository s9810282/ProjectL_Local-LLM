using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    public enum DeckUnavailableReason
    {
        None = 0,
        NotReady = 1,
        InactiveStage = 2,
        Stopped = 3,
        Faulted = 4,
        Loading = 5,
        JobPending = 6,
        RequestPending = 7,
        RunEnded = 8,
        BattleRetained = 9,
        InteractionOpen = 10,
        /// <summary>Stage 흐름의 편집 가능 시점 읽기가 덱 서비스에 연결되지 않았다. Core만 반환한다.</summary>
        AuthorityUnbound = 11,
    }
}
