using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills
{
    public enum PlayerSkillKey
    {
        None = 0,
        PowderBlossom = 1,
        MagicBullet = 2
    }

    public enum PlayerSkillUpgradeKey
    {
        None = 0,
        Bloom = 1,
        Condemnation = 2,
        Distortion = 3,
        Judgment = 4
    }

    public sealed class PlayerSkillDefinition
    {
        public PlayerSkillKey Key { get; }
        public EntityKey MainCharacterKey { get; }
        public int StageUseLimit { get; }

        public PlayerSkillDefinition(PlayerSkillKey key, EntityKey mainCharacterKey, int stageUseLimit)
        {
            if (key is not (PlayerSkillKey.PowderBlossom or PlayerSkillKey.MagicBullet))
                throw new ArgumentOutOfRangeException(nameof(key));
            if (mainCharacterKey == EntityKey.None || !Enum.IsDefined(typeof(EntityKey), mainCharacterKey))
                throw new ArgumentOutOfRangeException(nameof(mainCharacterKey));
            if (stageUseLimit <= 0)
                throw new ArgumentOutOfRangeException(nameof(stageUseLimit));

            Key = key;
            MainCharacterKey = mainCharacterKey;
            StageUseLimit = stageUseLimit;
        }

        public bool SupportsUpgrade(PlayerSkillUpgradeKey upgrade) => Key switch
        {
            PlayerSkillKey.PowderBlossom => upgrade is PlayerSkillUpgradeKey.Bloom or PlayerSkillUpgradeKey.Condemnation,
            PlayerSkillKey.MagicBullet => upgrade is PlayerSkillUpgradeKey.Distortion or PlayerSkillUpgradeKey.Judgment,
            _ => false
        };

        /// <summary>스킬의 지정 방식으로 실행할 수 없는 특성 조합을 한 곳에서 판정한다.</summary>
        public bool SupportsTrait(TraitKey trait) => Key != PlayerSkillKey.PowderBlossom || trait != TraitKey.ImpactBolt;

        public void ValidateTraits(IEnumerable<TraitKey> traits)
        {
            if (traits == null) throw new ArgumentNullException(nameof(traits));
            foreach (var trait in traits)
                if (!SupportsTrait(trait))
                    throw new InvalidOperationException($"스킬과 함께 사용할 수 없는 특성입니다: Skill={Key}, Main={MainCharacterKey}, Trait={trait}.");
        }

        internal bool SameValues(PlayerSkillDefinition other) => other != null && Key == other.Key &&
            MainCharacterKey == other.MainCharacterKey && StageUseLimit == other.StageUseLimit;
    }
}
