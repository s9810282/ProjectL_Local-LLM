#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>검사를 마친 사건과 경계 대기의 불변 목록입니다.</summary>
    internal sealed class CompiledTraitPreviewScenario
    {
        internal CompiledTraitPreviewScenario(IEnumerable<CharacterCombatFacts> initial, IEnumerable<CompiledPreviewEntry> entries)
        {
            Initial = Array.AsReadOnly(initial.ToArray());
            Entries = Array.AsReadOnly(entries.ToArray());
        }

        internal IReadOnlyList<CharacterCombatFacts> Initial { get; }
        internal IReadOnlyList<CompiledPreviewEntry> Entries { get; }
    }

    internal sealed class CompiledPreviewEntry
    {
        internal CompiledPreviewEntry(PreviewScenarioEntry input, CombatResult result, IEnumerable<CharacterCombatFacts> before, IEnumerable<CharacterCombatFacts> after, string diagnostic)
        {
            Name = input.name;
            Kind = input.kind;
            Seconds = input.seconds;
            Result = result;
            Before = Array.AsReadOnly(before.ToArray());
            After = Array.AsReadOnly(after.ToArray());
            Diagnostic = diagnostic;
        }

        internal string Name { get; }
        internal PreviewEntryKind Kind { get; }
        internal float Seconds { get; }
        internal CombatResult Result { get; }
        internal IReadOnlyList<CharacterCombatFacts> Before { get; }
        internal IReadOnlyList<CharacterCombatFacts> After { get; }
        internal string Diagnostic { get; }
    }
}
#endif
