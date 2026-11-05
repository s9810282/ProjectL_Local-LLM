using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>
    /// 한 유닛이 실제로 쓰는 특성 설정값 묶음이다(설명 수치 공개 설계 SP1). 값은 등급·증강·전직이 반영된 최종 <see cref="TraitSpec"/> 그대로다.
    /// 중첩·잔량·표적·남은 턴 같은 현재 진행값은 담지 않는다. 그것은 <see cref="TraitStatusSnapshot"/>이 원본이다.
    /// <see cref="Traits"/> 순서는 특성 키 오름차순으로, 정의·<see cref="CharacterBaselineStatsSnapshot.TraitKeys"/>·전투 특성 상태와 같다.
    /// 화면에 보일 순서와 출처는 <see cref="Display"/>다(특성 표시 출처 설계 TO4).
    /// </summary>
    public sealed class TraitSettingsSet
    {
        public static TraitSettingsSet Empty { get; } = new(Array.Empty<(TraitSpec, TraitOrigin)>());

        /// <summary>설정값이다. 특성 키 오름차순이다.</summary>
        public IReadOnlyList<TraitSpec> Traits { get; }

        /// <summary>표시 순서의 특성 키와 출처다. 키 집합은 <see cref="Traits"/>와 같다. 계산에는 쓰지 않는다.</summary>
        public IReadOnlyList<TraitDisplayEntry> Display { get; }

        /// <param name="entries">표시 순서대로 넣는다. 설정과 출처를 한 항목에 묶어 키 집합이 어긋나지 않게 한다.</param>
        public TraitSettingsSet(IEnumerable<(TraitSpec Spec, TraitOrigin Origin)> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));
            var copy = entries.ToArray();
            var keys = new HashSet<TraitKey>();
            var display = new TraitDisplayEntry[copy.Length];
            int categoryCount = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                var trait = copy[i].Spec;
                if (trait == null)
                    throw new ArgumentException("특성 설정에 null 항목을 넣을 수 없습니다.", nameof(entries));
                if (!keys.Add(trait.TraitKey))
                    throw new ArgumentException($"같은 특성 설정을 두 번 넣을 수 없습니다: {trait.TraitKey}.", nameof(entries));
                if (copy[i].Origin == TraitOrigin.Category && ++categoryCount > 1)
                    throw new ArgumentException("병과 고유 특성은 하나뿐이어야 합니다.", nameof(entries));
                display[i] = new TraitDisplayEntry(trait.TraitKey, copy[i].Origin);
            }
            var specs = copy.Select(entry => entry.Spec).ToArray();
            Array.Sort(specs, (left, right) => left.TraitKey.CompareTo(right.TraitKey));
            Traits = Array.AsReadOnly(specs);
            Display = Array.AsReadOnly(display);
        }

        /// <summary>
        /// 최종 유닛 명세의 특성 설정을 표시 순서·출처와 함께 담는다. 보유 개체는 <c>OwnedUnitSpecResolver.Resolve</c>,
        /// 그 밖은 <c>ResolveBase</c>가 같은 호출에서 낸 명세와 표시 목록을 넘긴다.
        /// </summary>
        internal static TraitSettingsSet From(UnitSpec unit, IReadOnlyList<TraitDisplayEntry> display)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));
            if (display == null)
                throw new ArgumentNullException(nameof(display));
            var specs = unit.Traits.ToArray();
            if (display.Count != specs.Length || display.Any(entry => entry == null))
                throw new InvalidOperationException($"표시 목록과 특성 설정의 개수가 다릅니다: {unit.Key}.");
            var entries = new (TraitSpec, TraitOrigin)[display.Count];
            for (int i = 0; i < display.Count; i++)
            {
                var spec = specs.SingleOrDefault(trait => trait.TraitKey == display[i].Key)
                    ?? throw new InvalidOperationException($"표시 목록의 특성이 설정에 없습니다: {unit.Key}, {display[i].Key}.");
                entries[i] = (spec, display[i].Origin);
            }
            return entries.Length == 0 ? Empty : new TraitSettingsSet(entries);
        }

        public bool TryGet(TraitKey key, out TraitSpec spec)
        {
            foreach (var trait in Traits)
            {
                if (trait.TraitKey != key) continue;
                spec = trait;
                return true;
            }
            spec = null;
            return false;
        }

        /// <summary>특성 형식으로 설정을 꺼낸다. 예: <c>TryGet&lt;StationaryFireTraitSpec&gt;(out var fire)</c>.</summary>
        public bool TryGet<T>(out T spec) where T : TraitSpec
        {
            foreach (var trait in Traits)
            {
                if (trait is not T typed) continue;
                spec = typed;
                return true;
            }
            spec = null;
            return false;
        }

        /// <summary>키 순서와 모든 설정 값이 같은지 비교한다. 표시 순서·출처(<see cref="Display"/>)는 보지 않는다.</summary>
        public bool SameValues(TraitSettingsSet other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null || Traits.Count != other.Traits.Count) return false;
            for (int i = 0; i < Traits.Count; i++)
                if (!Traits[i].SameValues(other.Traits[i])) return false;
            return true;
        }
    }
}
