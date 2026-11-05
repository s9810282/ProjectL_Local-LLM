namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public enum MainSpecializationFailureReason
    {
        None = 0,
        InvalidTarget = 1,
        InvalidCategory = 2,
        AlreadySpecialized = 3,
        SourceChanged = 4,
        RunUnavailable = 5,
        InBattle = 6,
        GrowthPending = 7,
        InvalidDefinition = 8,
        UnsupportedProgression = 9,
    }
}
