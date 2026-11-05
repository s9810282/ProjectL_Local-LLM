using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    internal sealed class PreparedTraitGrowthChange
    {
        internal long Revision { get; }
        internal long ExpectedRevision { get; }
        internal int Index { get; }
        internal OwnedCharacterRecord Final { get; }

        internal PreparedTraitGrowthChange(int index, OwnedCharacterRecord final, long revision, long expectedRevision)
        {
            Revision = revision;
            ExpectedRevision = expectedRevision;
            Index = index;
            Final = final;
        }
    }
}
