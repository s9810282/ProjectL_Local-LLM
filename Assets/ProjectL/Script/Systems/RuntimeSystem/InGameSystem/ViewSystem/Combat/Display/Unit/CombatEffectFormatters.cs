using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>효과 하나의 공개 요약값만 표시 인자로 바꿉니다.</summary>
    internal interface ICombatEffectFormatter
    {
        StatusEffectKey Key { get; }
        int ValueCount { get; }
        object[] Values(StatusEffectSnapshot snapshot);
    }

    internal sealed class CombatEffectFormatter<T> : ICombatEffectFormatter where T : StatusEffectSnapshot
    {
        private readonly Func<T, object>[] _values;
        internal CombatEffectFormatter(StatusEffectKey key, params Func<T, object>[] values)
        {
            Key = key;
            _values = (Func<T, object>[])(values ?? throw new ArgumentNullException(nameof(values))).Clone();
            foreach (var value in _values)
                if (value == null) throw new ArgumentException("효과 값 변환이 비었습니다.", nameof(values));
        }
        public StatusEffectKey Key { get; }
        public int ValueCount => _values.Length;
        public object[] Values(StatusEffectSnapshot snapshot)
        {
            if (snapshot is not T typed || snapshot.Key != Key)
                throw new InvalidOperationException($"[CombatEffectFormatter] {Key}의 Snapshot 형식이 다릅니다: {snapshot?.GetType().Name}");
            var result = new object[_values.Length];
            for (int i = 0; i < result.Length; i++) result[i] = _values[i](typed);
            return result;
        }
    }

    /// <summary>생성 뒤에는 변경할 수 없는 효과별 표시 등록입니다.</summary>
    internal sealed class CombatEffectFormatters
    {
        private readonly Dictionary<StatusEffectKey, ICombatEffectFormatter> _entries = new();
        internal CombatEffectFormatters(IEnumerable<ICombatEffectFormatter> entries, IUnitStatusVisualTableQuery visuals)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (visuals == null) throw new ArgumentNullException(nameof(visuals));
            foreach (var entry in entries)
            {
                if (entry == null || !_entries.TryAdd(entry.Key, entry))
                    throw new InvalidOperationException("[CombatEffectFormatters] 비어 있거나 중복된 효과 등록입니다.");
                var visual = visuals.Get(entry.Key);
                if (!visual.AcceptsValueCount(entry.ValueCount))
                    throw new InvalidOperationException($"[CombatEffectFormatters] {entry.Key} 설명 틀의 인자는 {entry.ValueCount}개입니다: {visual.DescriptionTemplate}");
            }
        }
        internal ICombatEffectFormatter Get(StatusEffectKey key) => _entries.TryGetValue(key, out var entry)
            ? entry : throw new InvalidOperationException($"[CombatEffectFormatters] 표시 변환이 등록되지 않았습니다: {key}");
    }
}
