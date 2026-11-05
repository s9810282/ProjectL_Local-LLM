using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    public enum DeckFailureReason
    {
        None = 0,
        EmptyDeck = 1,
        UnknownOwnedId = 2,
        DuplicateOwnedId = 3,
        UnsupportedUnit = 4,
        MissingMain = 5,
        MultipleMains = 6,
        TooManyCards = 7,
        StaleRun = 8,
        StaleRoster = 9,
        StaleDeck = 10,
        WrongPhase = 11,
        StateChanged = 12,
        NotReady = 13,
    }
}
