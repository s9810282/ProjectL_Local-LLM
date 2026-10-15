using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// 16개 방향성 상성 항목의 권위 있는 데이터입니다.
    /// 누락/중복/미등록 Category는 bootstrap 오류이며 1.0으로 숨기지 않습니다.
    /// </summary>
    public sealed class CategoryMatchupSpec
    {
        private static readonly CategoryKey[] OrderedCategories =
        {
            CategoryKey.Base, CategoryKey.Rider, CategoryKey.Heavy, CategoryKey.Cannon,
        };

        private readonly Dictionary<int, CategoryMatchupEntry> _entries;

        public CategoryMatchupSpec(IReadOnlyList<CategoryMatchupEntry> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            _entries = new Dictionary<int, CategoryMatchupEntry>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                CategoryMatchupEntry entry = entries[i];
                int key = CreateKey(entry.Attacker, entry.Defender);
                if (_entries.ContainsKey(key))
                {
                    throw new ArgumentException(
                        $"상성 항목 {entry.Attacker}->{entry.Defender}가 중복되었습니다.",
                        nameof(entries));
                }

                _entries.Add(key, entry);
            }

            // 네 Category의 모든 조합 16개가 반드시 있어야 한다.
            foreach (CategoryKey attacker in OrderedCategories)
            {
                foreach (CategoryKey defender in OrderedCategories)
                {
                    if (_entries.ContainsKey(CreateKey(attacker, defender)) == false)
                    {
                        throw new ArgumentException(
                            $"상성 항목 {attacker}->{defender}가 누락되었습니다.",
                            nameof(entries));
                    }
                }
            }
        }

        /// <summary>
        /// 기획서 상성표입니다. 공격자 행, 피격자 열이며 공통 분모는 10입니다.
        /// </summary>
        public static CategoryMatchupSpec CreateDefault()
        {
            return new CategoryMatchupSpec(new[]
            {
                new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Base, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Rider, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Heavy, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Base, CategoryKey.Cannon, 10, 10),

                new CategoryMatchupEntry(CategoryKey.Rider, CategoryKey.Base, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Rider, CategoryKey.Rider, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Rider, CategoryKey.Heavy, 7, 10),
                new CategoryMatchupEntry(CategoryKey.Rider, CategoryKey.Cannon, 12, 10),

                new CategoryMatchupEntry(CategoryKey.Heavy, CategoryKey.Base, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Heavy, CategoryKey.Rider, 15, 10),
                new CategoryMatchupEntry(CategoryKey.Heavy, CategoryKey.Heavy, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Heavy, CategoryKey.Cannon, 9, 10),

                new CategoryMatchupEntry(CategoryKey.Cannon, CategoryKey.Base, 10, 10),
                new CategoryMatchupEntry(CategoryKey.Cannon, CategoryKey.Rider, 9, 10),
                new CategoryMatchupEntry(CategoryKey.Cannon, CategoryKey.Heavy, 11, 10),
                new CategoryMatchupEntry(CategoryKey.Cannon, CategoryKey.Cannon, 10, 10),
            });
        }

        public bool TryGetEntry(CategoryKey attacker, CategoryKey defender, out CategoryMatchupEntry entry)
        {
            return _entries.TryGetValue(CreateKey(attacker, defender), out entry);
        }

        private static int CreateKey(CategoryKey attacker, CategoryKey defender)
        {
            return ((int)attacker * 1000) + (int)defender;
        }
    }
}
