using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class OwnedCharacterRecord
    {
        public CharacterInstanceId Id { get; }
        public EntityKey CharacterKey { get; }
        public CharacterOwnershipRole Role { get; }
        /// <summary>
        /// Run 동안 유지하는 보유 개체의 레벨이다. 서브의 기준 능력치는 이 값으로 성장한다.
        /// 메인의 성장 결과는 메인 지속 상태가 가지므로 메인 능력치는 이 값으로 계산하지 않는다. 시작 레벨은 획득 요청이 정한다.
        /// </summary>
        public int Level { get; }
        /// <summary>
        /// 현재 레벨 안의 진행 경험치다. 다음 레벨 요구량 미만이라는 상한은 규칙(ExperienceRules)이 지키며 기록은 규칙 값을 모른다.
        /// 획득 직후는 0이고, 전투 승리 때 그 전투에서 번 경험치가 반영된다.
        /// </summary>
        public int Experience { get; }
        /// <summary>특성 성장 칸이다. 레벨·경험치 성장과 따로 바뀐다.</summary>
        public OwnedTraitGrowth Growth { get; }
        /// <summary>이번 Run에서 얻은 사냥꾼 공격 기여분입니다. 새로 획득하면 0으로 시작합니다.</summary>
        public int HunterAttackBonus { get; }
        /// <summary>이번 Run에서 고른 메인의 추가 병과다. 미전직이면 null이다.</summary>
        public CategoryKey? SpecializationCategory { get; }
        public PlayerSkillUpgradeKey PlayerSkillUpgrade { get; }

        public OwnedCharacterRecord(CharacterInstanceId id, EntityKey characterKey, CharacterOwnershipRole role,
            int level = 1, int experience = 0, OwnedTraitGrowth growth = null, int hunterAttackBonus = 0,
            CategoryKey? specializationCategory = null, PlayerSkillUpgradeKey playerSkillUpgrade = PlayerSkillUpgradeKey.None)
        {
            if (id.IsEmpty)
                throw new ArgumentException("보유 ID는 비어 있을 수 없습니다.", nameof(id));
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            if (role != CharacterOwnershipRole.Main && role != CharacterOwnershipRole.Sub)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));
            if (experience < 0)
                throw new ArgumentOutOfRangeException(nameof(experience));
            if (hunterAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(hunterAttackBonus));
            if (!Enum.IsDefined(typeof(PlayerSkillUpgradeKey), playerSkillUpgrade) ||
                (role != CharacterOwnershipRole.Main && playerSkillUpgrade != PlayerSkillUpgradeKey.None))
                throw new ArgumentOutOfRangeException(nameof(playerSkillUpgrade));
            if (specializationCategory.HasValue && (role != CharacterOwnershipRole.Main ||
                specializationCategory != CategoryKey.Rider && specializationCategory != CategoryKey.Heavy &&
                specializationCategory != CategoryKey.Cannon))
                throw new ArgumentException("메인만 Rider·Heavy·Cannon 중 하나로 전직할 수 있습니다.", nameof(specializationCategory));
            growth ??= OwnedTraitGrowth.Empty;
            Id = id;
            CharacterKey = characterKey;
            Role = role;
            Level = level;
            Experience = experience;
            Growth = growth;
            HunterAttackBonus = hunterAttackBonus;
            SpecializationCategory = specializationCategory;
            PlayerSkillUpgrade = playerSkillUpgrade;
        }

        /// <summary>식별·레벨·진행 경험치·특성 성장이 모두 같은지 비교한다. 명령 입력 원본 대조와 합성이 쓴다.</summary>
        public bool SameValues(OwnedCharacterRecord other) => other != null && Id == other.Id &&
            CharacterKey == other.CharacterKey && Role == other.Role && Level == other.Level && Experience == other.Experience &&
            HunterAttackBonus == other.HunterAttackBonus && SpecializationCategory == other.SpecializationCategory &&
            PlayerSkillUpgrade == other.PlayerSkillUpgrade && SameGrowth(other);

        public OwnedCharacterRecord WithGrowth(OwnedTraitGrowth growth) =>
            new(Id, CharacterKey, Role, Level, Experience, growth ?? throw new ArgumentNullException(nameof(growth)),
                HunterAttackBonus, SpecializationCategory, PlayerSkillUpgrade);

        public bool SameGrowth(OwnedCharacterRecord other) => other != null && Growth.SameValues(other.Growth);

        public OwnedCharacterRecord WithPlayerSkillUpgrade(PlayerSkillUpgradeKey upgrade) =>
            new(Id, CharacterKey, Role, Level, Experience, Growth, HunterAttackBonus, SpecializationCategory, upgrade);
    }
}
