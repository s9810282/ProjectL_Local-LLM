namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public enum RosterAcquireFailureReason
    {
        None = 0,
        InvalidRequest = 1,
        InvalidDefinition = 2,
        MainAlreadyOwned = 3,
        InvalidMainState = 4,
        IdentityOverflow = 5,
        RunEnded = 6
    }
}
