using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    internal sealed class PreparedPlayerSkillUpgrade
    {
        internal CharacterBuffer Owner { get; }
        internal OwnedCharacterRecord Expected { get; }
        internal OwnedCharacterRecord Final { get; }
        internal int Index { get; }
        internal long ExpectedRevision { get; }
        internal long Revision { get; }

        internal PreparedPlayerSkillUpgrade(CharacterBuffer owner, OwnedCharacterRecord expected, OwnedCharacterRecord final,
            int index, long expectedRevision, long revision)
        {
            Owner = owner;
            Expected = expected;
            Final = final;
            Index = index;
            ExpectedRevision = expectedRevision;
            Revision = revision;
        }
    }
}
