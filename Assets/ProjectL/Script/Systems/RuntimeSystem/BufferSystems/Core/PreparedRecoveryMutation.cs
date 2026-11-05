using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>같은 동기 호출 안에서 한 번만 설치할 회복 변경과 원본 비교값이다.</summary>
    internal sealed class PreparedRecoveryMutation
    {
        internal StageServiceTransactions Owner { get; }
        internal int Chapter { get; }
        internal StageSelectionData Selection { get; }
        internal CurrentRecoveryData ExpectedRecovery { get; }
        internal MainCharacterPersistentState ExpectedMain { get; }
        internal CharacterInstanceId SoulSource { get; }
        internal int ExpectedSouls { get; }
        internal int ExpectedGold { get; }
        internal long ExpectedRosterRevision { get; }
        internal IReadOnlyList<StageProgressData> ExpectedNodes { get; }
        internal bool ExpectedMapClear { get; }
        internal PreparedCharacterChanges Characters { get; }
        internal int FinalGold { get; }
        internal CurrentRecoveryData Recovery { get; }
        internal PreparedStageClear Clear { get; }
        internal bool Installed { get; set; }

        internal PreparedRecoveryMutation(StageServiceTransactions owner, int chapter, StageSelectionData selection,
            CurrentRecoveryData expectedRecovery, MainCharacterPersistentState expectedMain, CharacterInstanceId soulSource,
            int expectedSouls, int expectedGold, long expectedRosterRevision, StageProgressData[] expectedNodes, bool expectedMapClear,
            PreparedCharacterChanges characters, int finalGold, CurrentRecoveryData recovery, PreparedStageClear clear)
        {
            Owner = owner;
            Chapter = chapter;
            Selection = selection;
            ExpectedRecovery = expectedRecovery;
            ExpectedMain = expectedMain;
            SoulSource = soulSource;
            ExpectedSouls = expectedSouls;
            ExpectedGold = expectedGold;
            ExpectedRosterRevision = expectedRosterRevision;
            ExpectedNodes = Array.AsReadOnly(expectedNodes);
            ExpectedMapClear = expectedMapClear;
            Characters = characters;
            FinalGold = finalGold;
            Recovery = recovery;
            Clear = clear;
        }
    }
}
