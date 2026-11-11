using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 전투 설치 때 미리 만들 유닛 표시 수와 고스트 키입니다(씬 표시 나누기 설계 DS6, GC 끊김 설계 GH6·계획 TP-6).
    /// </summary>
    /// <remarks>
    /// 셀 캐릭터(살아 있고 제외되지 않은 것 — 필드·손패·적 대기 모두)는 여기 한 곳에서 정하고, 고스트는 그중 플레이어 캐릭터 키만 셉니다.
    /// 적 본부 증원 풀의 키는 준비 때 이미 고정돼 있으므로 키별 최대 필드 적 수까지만 더합니다.
    /// 전투 중 소환되는 유닛은 키를 미리 몰라 넣지 않습니다. 미리 만들기 호출 차례(유닛 → 고스트 → 사건 Presenter)는 표시가 지킵니다.
    /// </remarks>
    internal sealed class CombatPrewarmPlan
    {
        private readonly Dictionary<EntityKey, int> _counts;
        private readonly List<EntityKey> _playerKeys;

        private CombatPrewarmPlan(Dictionary<EntityKey, int> counts, List<EntityKey> playerKeys)
        {
            _counts = counts;
            _playerKeys = playerKeys;
        }

        /// <summary>키별로 미리 만들 유닛 표시 수입니다.</summary>
        internal IReadOnlyDictionary<EntityKey, int> Counts => _counts;

        /// <summary>미리 만들 유닛 표시의 키입니다(사건 Presenter가 공격 연출 값을 고르는 데 씀).</summary>
        internal IReadOnlyCollection<EntityKey> Keys => _counts.Keys;

        /// <summary>고스트를 미리 만들 플레이어 캐릭터 키입니다(처음 나온 차례).</summary>
        internal IReadOnlyList<EntityKey> PlayerKeys => _playerKeys;

        internal static CombatPrewarmPlan From(IReadOnlyList<CharacterData> characters, NexusReinforcementSpec reinforcements)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            var counts = new Dictionary<EntityKey, int>();
            var playerKeys = new List<EntityKey>();
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData character = characters[i];
                if (character.Health.IsDead || character.Area == CharacterRuntimeArea.Excluded)
                    continue;
                EntityKey key = character.CharacterKey;
                counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
                if (character.Owner == Owner.Player && !playerKeys.Contains(key))
                    playerKeys.Add(key);
            }
            if (reinforcements != null)
            {
                for (int i = 0; i < reinforcements.Pool.Count; i++)
                {
                    NexusReinforcementEntry entry = reinforcements.Pool[i];
                    int prepared = counts.TryGetValue(entry.UnitKey, out int count) ? count : 0;
                    counts[entry.UnitKey] = prepared + Math.Min(entry.Count, reinforcements.FieldEnemyLimit);
                }
            }
            return new CombatPrewarmPlan(counts, playerKeys);
        }
    }
}
