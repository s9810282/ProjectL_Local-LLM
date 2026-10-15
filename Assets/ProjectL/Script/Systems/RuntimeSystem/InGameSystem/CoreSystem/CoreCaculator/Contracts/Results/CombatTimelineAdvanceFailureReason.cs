using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum CombatTimelineAdvanceFailureReason
    {
        None = 0,
        WrongPhase = 1,
        ActiveUnitAlreadyExists = 2,
        NoTimelineEntry = 3,
        StageEnded = 4,
    }
}