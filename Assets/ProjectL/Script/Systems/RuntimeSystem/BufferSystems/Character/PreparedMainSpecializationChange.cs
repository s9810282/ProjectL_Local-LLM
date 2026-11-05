using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    /// <summary>해당 저장소의 원본과 revision에 묶인 일회용 전직 설치 증거다.</summary>
    internal sealed class PreparedMainSpecializationChange
    {
        internal CharacterBuffer Owner { get; }
        internal MainSpecializationChange Change { get; }
        internal int Index { get; }
        internal long ExpectedRevision { get; }
        internal long Revision { get; }
        internal bool Installed { get; private set; }

        internal PreparedMainSpecializationChange(CharacterBuffer owner, MainSpecializationChange change, int index,
            long expectedRevision, long revision)
        {
            Owner = owner;
            Change = change;
            Index = index;
            ExpectedRevision = expectedRevision;
            Revision = revision;
        }

        internal void MarkInstalled() => Installed = true;
    }
}
