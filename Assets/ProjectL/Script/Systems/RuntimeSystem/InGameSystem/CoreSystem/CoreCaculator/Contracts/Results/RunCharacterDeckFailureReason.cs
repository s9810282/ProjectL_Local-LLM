namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum RunCharacterDeckFailureReason
    {
        None = 0,
        Disposed = 1,
        RunMismatch = 2,
        RunEnded = 3,
        WrongPhase = 4,
        InvalidCount = 5,
        InvalidCharacter = 6,
        CountOverflow = 7,
        RevisionOverflow = 8,
    }
}
