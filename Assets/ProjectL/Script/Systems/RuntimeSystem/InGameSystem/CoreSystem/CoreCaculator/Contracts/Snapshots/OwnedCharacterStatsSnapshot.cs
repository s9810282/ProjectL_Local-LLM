using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>보유 카드 한 장의 전투 외 조회값이다. 식별은 <see cref="OwnedId"/>로 하고 <see cref="CharacterKey"/>는 정의·외형 조회에만 쓴다.</summary>
    public sealed class OwnedCharacterStatsSnapshot
    {
        public CharacterInstanceId OwnedId { get; }
        public EntityKey CharacterKey { get; }
        public CharacterOwnershipRole Role { get; }
        /// <summary>보유 단계다. 현재 규칙에서는 능력치를 바꾸지 않는다.</summary>
        public int Level { get; }
        /// <summary>
        /// <see cref="Level"/>에서 다음 레벨까지의 진행값이다(현재·요구량·남은 양, 경험치 꺼짐 여부).
        /// 요구량은 Core 규칙이 계산하므로 View는 이 값을 그리기만 한다.
        /// </summary>
        public ExperienceProgressSnapshot Experience { get; }
        /// <summary>이 카드가 가진 영혼 수다. 영혼 기여분은 이미 <see cref="Stats"/>에 포함돼 있다.</summary>
        public int SoulCount { get; }
        public CharacterBaselineStatsSnapshot Stats { get; }

        /// <summary>지정 방식과 <see cref="CharacterBaselineStatsSnapshot.AttackRange"/>로 만든 전투 밖 공격 무늬다.</summary>
        public AttackPatternSnapshot AttackPattern { get; }

        /// <summary>
        /// 등급·증강·전직이 반영된 특성 설정값이다(설명 수치 공개 설계 SP2). 순서는 <see cref="CharacterBaselineStatsSnapshot.TraitKeys"/>와 같다.
        /// 설명 숫자는 이 묶음에서 읽는다. 아래 특성별 설명 속성 다섯은 View 이전 뒤 지울 옛 경로다(계획 SP-d).
        /// </summary>
        public TraitSettingsSet TraitSettings { get; }

        /// <summary>
        /// 강화가 반영된 정의의 가시 설정값이다. 전투 중 잔여 수명·상태가 아니며, 가시 특성이 없으면 null이다.
        /// </summary>
        public ThornFieldTraitStatusSnapshot ThornFieldDescription { get; }

        /// <summary>정의에서 얻은 표식 증가율입니다. 전투 대상의 현재 표식을 보관하지 않습니다.</summary>
        public HuntersMarkTraitStatusSnapshot HuntersMarkDescription { get; }

        /// <summary>정의에서 얻은 끌어오기 추가 사거리입니다.</summary>
        public PullTraitStatusSnapshot PullDescription { get; }

        /// <summary>강화가 반영된 정의에서 얻은 충격 볼트의 밀치기 거리입니다.</summary>
        public ImpactBoltTraitStatusSnapshot ImpactBoltDescription { get; }

        /// <summary>정의에서 얻은 정찰대 증가율과 십자 반경입니다. 수신자 상태는 보관하지 않습니다.</summary>
        public ReconSquadTraitStatusSnapshot ReconSquadDescription { get; }

        /// <summary>
        /// <see cref="CharacterBaselineStatsSnapshot.TraitKeys"/>와 같은 순서로 특성마다 하나씩 둔 전투 밖 사용 가능 여부다.
        /// 사유는 전투 중 특성 상태와 같은 원본에서 온다. 생산자가 넘기지 않으면 비어 있다(Core 계산기는 항상 채운다).
        /// </summary>
        public IReadOnlyList<TraitAvailabilitySnapshot> TraitAvailability { get; }

        public IReadOnlyDictionary<TraitKey, int> TraitRanks { get; }

        public OwnedCharacterStatsSnapshot(CharacterInstanceId ownedId, EntityKey characterKey,
            CharacterOwnershipRole role, int level, ExperienceProgressSnapshot experience, int soulCount, CharacterBaselineStatsSnapshot stats,
            AttackPatternSnapshot attackPattern, TraitSettingsSet traitSettings, ThornFieldTraitStatusSnapshot thornFieldDescription, HuntersMarkTraitStatusSnapshot huntersMarkDescription = null,
            PullTraitStatusSnapshot pullDescription = null, ImpactBoltTraitStatusSnapshot impactBoltDescription = null,
            IReadOnlyList<TraitAvailabilitySnapshot> traitAvailability = null, ReconSquadTraitStatusSnapshot reconSquadDescription = null,
            IReadOnlyDictionary<TraitKey, int> traitRanks = null)
        {
            if (ownedId.IsEmpty)
                throw new ArgumentException("보유 ID는 비어 있을 수 없습니다.", nameof(ownedId));
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            if (role != CharacterOwnershipRole.Main && role != CharacterOwnershipRole.Sub)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));
            if (experience == null)
                throw new ArgumentNullException(nameof(experience));
            if (soulCount < 0)
                throw new ArgumentOutOfRangeException(nameof(soulCount));
            if (stats == null)
                throw new ArgumentNullException(nameof(stats));
            if (attackPattern == null)
                throw new ArgumentNullException(nameof(attackPattern));
            if (traitSettings == null)
                throw new ArgumentNullException(nameof(traitSettings));
            var settingKeys = traitSettings.Traits.Select(t => t.TraitKey);
            if (!settingKeys.SequenceEqual(stats.TraitKeys.ToArray()))
                throw new ArgumentException("특성 설정은 특성 키와 같은 키·순서여야 합니다.", nameof(traitSettings));

            bool hasThornField = false;
            bool hasHuntersMark = false;
            bool hasPull = false;
            bool hasImpactBolt = false;
            foreach (TraitKey key in stats.TraitKeys.Span)
            {
                if (key == TraitKey.ThornField) hasThornField = true;
                if (key == TraitKey.HuntersMark) hasHuntersMark = true;
                if (key == TraitKey.Pull) hasPull = true;
                if (key == TraitKey.ImpactBolt) hasImpactBolt = true;
            }
            if (hasThornField != (thornFieldDescription != null))
                throw new ArgumentException("가시 특성 키와 가시 표시 설정값은 함께 있어야 합니다.", nameof(thornFieldDescription));

            if (hasHuntersMark != (huntersMarkDescription != null))
                throw new ArgumentException("표식 특성 키와 표식 표시 설정값은 함께 있어야 합니다.", nameof(huntersMarkDescription));

            if (hasPull != (pullDescription != null))
                throw new ArgumentException("끌어오기 특성 키와 표시 설정값은 함께 있어야 합니다.", nameof(pullDescription));

            if (hasImpactBolt != (impactBoltDescription != null))
                throw new ArgumentException("충격 볼트 특성 키와 표시 설정값은 함께 있어야 합니다.", nameof(impactBoltDescription));

            if (stats.TraitKeys.ToArray().Contains(TraitKey.ReconSquad) != (reconSquadDescription != null))
                throw new ArgumentException("정찰대 특성 키와 표시 설정값은 함께 있어야 합니다.", nameof(reconSquadDescription));

            var availability = traitAvailability?.ToArray() ?? Array.Empty<TraitAvailabilitySnapshot>();
            if (availability.Length != 0)
            {
                var keys = stats.TraitKeys.Span;
                if (availability.Length != keys.Length)
                    throw new ArgumentException("특성 사용 가능 여부는 특성 키마다 하나씩 있어야 합니다.", nameof(traitAvailability));
                for (int i = 0; i < availability.Length; i++)
                    if (availability[i] == null || availability[i].Key != keys[i])
                        throw new ArgumentException("특성 사용 가능 여부는 특성 키와 같은 순서여야 합니다.", nameof(traitAvailability));
            }

            var keysWithRanks = stats.TraitKeys.ToArray();
            var ranks = traitRanks?.ToDictionary(p => p.Key, p => p.Value) ?? keysWithRanks.ToDictionary(k => k, k => 1);
            if (ranks.Count != keysWithRanks.Length || keysWithRanks.Any(k => !ranks.TryGetValue(k, out int rank) || rank < 1 || rank > 3))
                throw new ArgumentException("보유 특성 등급이 올바르지 않습니다.", nameof(traitRanks));
            TraitRanks = new System.Collections.ObjectModel.ReadOnlyDictionary<TraitKey, int>(ranks);
            OwnedId = ownedId;
            CharacterKey = characterKey;
            Role = role;
            Level = level;
            Experience = experience;
            SoulCount = soulCount;
            Stats = stats;
            AttackPattern = attackPattern;
            TraitSettings = traitSettings;
            ThornFieldDescription = thornFieldDescription;
            HuntersMarkDescription = huntersMarkDescription;
            PullDescription = pullDescription;
            ImpactBoltDescription = impactBoltDescription;
            ReconSquadDescription = reconSquadDescription;
            TraitAvailability = Array.AsReadOnly(availability);
        }
    }
}
