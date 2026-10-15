using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public sealed class SoulFieldFacts
    {
        public long Id { get; }
        public EffectOriginKey Origin { get; }
        public CellCoord Cell { get; }
        public int RemainingUnitTurns { get; }
        public int HealAmount { get; }

        public SoulFieldFacts(long id, EffectOriginKey origin, CellCoord cell,
            int remainingUnitTurns, int healAmount)
        {
            if (id < 1) throw new ArgumentOutOfRangeException(nameof(id));
            if (!origin.IsValid) throw new ArgumentException("영혼 출처가 필요합니다.", nameof(origin));
            if (remainingUnitTurns < 1) throw new ArgumentOutOfRangeException(nameof(remainingUnitTurns));
            if (healAmount < 0) throw new ArgumentOutOfRangeException(nameof(healAmount));
            Id = id;
            Origin = origin;
            Cell = cell;
            RemainingUnitTurns = remainingUnitTurns;
            HealAmount = healAmount;
        }

        public bool SameValues(SoulFieldFacts other) => other != null && Id == other.Id &&
            Origin == other.Origin && Cell == other.Cell && RemainingUnitTurns == other.RemainingUnitTurns &&
            HealAmount == other.HealAmount;
    }

    /// <summary>전장 영혼의 불변 입력/최종값. 보유 카드의 영혼 개수와 별개다.</summary>
    public sealed class SoulFieldSnapshot
    {
        public static SoulFieldSnapshot Empty { get; } = new SoulFieldSnapshot(1, Array.Empty<SoulFieldFacts>());
        public long NextSoulId { get; }
        public IReadOnlyList<SoulFieldFacts> Entries { get; }

        public SoulFieldSnapshot(long nextSoulId, IReadOnlyList<SoulFieldFacts> entries)
        {
            if (nextSoulId < 1) throw new ArgumentOutOfRangeException(nameof(nextSoulId));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var copy = new SoulFieldFacts[entries.Count];
            var ids = new HashSet<long>();
            for (int i = 0; i < copy.Length; i++)
            {
                var entry = entries[i] ?? throw new ArgumentException("영혼 값에는 null을 넣을 수 없습니다.");
                if (entry.Id >= nextSoulId || !ids.Add(entry.Id))
                    throw new ArgumentException("영혼 ID가 중복되거나 발급 범위를 벗어났습니다.");
                copy[i] = entry;
            }
            Array.Sort(copy, (a, b) => a.Id.CompareTo(b.Id));
            NextSoulId = nextSoulId;
            Entries = Array.AsReadOnly(copy);
        }

        public bool SameValues(SoulFieldSnapshot other)
        {
            if (other == null || NextSoulId != other.NextSoulId || Entries.Count != other.Entries.Count)
                return false;
            for (int i = 0; i < Entries.Count; i++)
                if (!Entries[i].SameValues(other.Entries[i])) return false;
            return true;
        }
    }

    public sealed class SoulFieldChange
    {
        public SoulFieldSnapshot Expected { get; }
        public SoulFieldSnapshot Final { get; }

        public SoulFieldChange(SoulFieldSnapshot expected, SoulFieldSnapshot final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (final.NextSoulId < expected.NextSoulId)
                throw new ArgumentException("전투 중 영혼 ID 발급 번호를 되감을 수 없습니다.");
        }
    }

    /// <summary>캐릭터 성장/체력과 전장 영혼 소비를 함께 반영할 제안.</summary>
    public sealed class SoulActionChanges
    {
        public CharacterChanges Characters { get; }
        public SoulFieldChange Field { get; }
        public IReadOnlyList<long> ConsumedSoulIds { get; }

        public SoulActionChanges(CharacterChanges characters, SoulFieldChange field,
            IReadOnlyList<long> consumedSoulIds)
        {
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            if (consumedSoulIds == null) throw new ArgumentNullException(nameof(consumedSoulIds));
            var expectedIds = new HashSet<long>();
            var finalIds = new HashSet<long>();
            foreach (var entry in field.Expected.Entries) expectedIds.Add(entry.Id);
            foreach (var entry in field.Final.Entries) finalIds.Add(entry.Id);
            var seen = new HashSet<long>();
            var copy = new long[consumedSoulIds.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                long id = consumedSoulIds[i];
                if (!seen.Add(id) || !expectedIds.Contains(id) || finalIds.Contains(id))
                    throw new ArgumentException("소비한 영혼은 예상 필드에 있고 최종 필드에는 없어야 합니다.");
                copy[i] = id;
            }
            long growth = 0;
            foreach (var soul in characters.Souls)
            {
                growth = checked(growth + soul.FinalCount - soul.ExpectedCount);
                bool linked = false;
                foreach (var character in characters.Characters)
                    if (character.Expected.OwnedId == soul.Id && character.Final.OwnedId == soul.Id)
                        linked = true;
                if (!linked) throw new ArgumentException("성장할 보유 카드의 캐릭터 변경이 필요합니다.");
            }
            if (growth != copy.Length)
                throw new ArgumentException("영혼 개수 증가는 소비한 영혼 수와 같아야 합니다.");
            ConsumedSoulIds = Array.AsReadOnly(copy);
        }
    }
}
