using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 한 전투 동안 바뀌지 않는 유닛별 특성 설정값이다(설명 수치 공개 설계 SP3). 전투 준비가 한 번 만들고 전투 상태가 끝까지 들고 간다.
    /// 값은 명령 입력의 공격 프로필과 같은 resolver 결과다(<c>OwnedUnitSpecResolver.BuildTraitSettings</c>).
    /// </summary>
    public sealed class BattleTraitSettings
    {
        private readonly Dictionary<EntityKey, TraitSettingsSet> _units;
        private readonly Dictionary<CharacterInstanceId, (EntityKey Key, TraitSettingsSet Settings)> _owned;

        /// <param name="units">유닛 키마다 보유가 아닌 개체(적·소환)의 설정이다.</param>
        /// <param name="owned">보유 개체마다 그 개체의 유닛 키와 성장이 반영된 설정이다.</param>
        public BattleTraitSettings(IReadOnlyDictionary<EntityKey, TraitSettingsSet> units,
            IReadOnlyDictionary<CharacterInstanceId, (EntityKey Key, TraitSettingsSet Settings)> owned)
        {
            if (units == null)
                throw new ArgumentNullException(nameof(units));
            if (owned == null)
                throw new ArgumentNullException(nameof(owned));
            _units = new Dictionary<EntityKey, TraitSettingsSet>();
            foreach (var pair in units)
            {
                if (pair.Key == EntityKey.None || pair.Value == null)
                    throw new ArgumentException("유닛 키와 특성 설정이 필요합니다.", nameof(units));
                _units.Add(pair.Key, pair.Value);
            }
            _owned = new Dictionary<CharacterInstanceId, (EntityKey, TraitSettingsSet)>();
            foreach (var pair in owned)
            {
                if (pair.Key.IsEmpty || pair.Value.Settings == null)
                    throw new ArgumentException("보유 ID와 특성 설정이 필요합니다.", nameof(owned));
                if (!_units.ContainsKey(pair.Value.Key))
                    throw new ArgumentException($"보유 개체의 유닛 키가 전투 유닛 목록에 없습니다: {pair.Value.Key}.", nameof(owned));
                _owned.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 전투 캐릭터 하나의 설정이다. 보유 개체는 그 개체의 설정, 그 밖은 유닛 키 기본 설정이다(명령의 프로필 선택과 같은 규칙).
        /// </summary>
        public TraitSettingsSet For(CharacterCombatFacts character)
        {
            if (character == null)
                throw new ArgumentNullException(nameof(character));
            if (character.OwnedId is CharacterInstanceId ownedId && _owned.TryGetValue(ownedId, out var entry))
            {
                if (entry.Key != character.CharacterKey)
                    throw new InvalidOperationException($"보유 개체 {ownedId}의 유닛 키가 전투 준비 때와 다릅니다.");
                return entry.Settings;
            }
            return ForUnit(character.CharacterKey);
        }

        /// <summary>아직 필드에 없는 유닛(소환 예고 등)의 기본 설정이다. 전투 유닛 목록에 없으면 <see cref="KeyNotFoundException"/>.</summary>
        public TraitSettingsSet ForUnit(EntityKey key) => _units.TryGetValue(key, out var settings)
            ? settings : throw new KeyNotFoundException($"전투 유닛 목록에 없는 유닛입니다: {key}.");
    }
}
