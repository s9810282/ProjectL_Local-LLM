using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    /// <summary>순서와 중복 기여분을 보존하는 불변 효과 모음입니다. 중첩 정책은 각 효과 규칙이 정합니다.</summary>
    public sealed class StatusEffectSet
    {
        public static StatusEffectSet Empty { get; } = new(Array.Empty<AppliedStatus>());
        public IReadOnlyList<AppliedStatus> Entries { get; }

        public StatusEffectSet(IReadOnlyList<AppliedStatus> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var copy = new AppliedStatus[entries.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = entries[i] ?? throw new ArgumentException("효과 값은 null일 수 없습니다.", nameof(entries));
            Entries = Array.AsReadOnly(copy);
        }

        public bool SameValues(StatusEffectSet other)
        {
            if (other == null || Entries.Count != other.Entries.Count) return false;
            for (int i = 0; i < Entries.Count; i++)
                if (!Entries[i].SameValues(other.Entries[i])) return false;
            return true;
        }
    }
}
