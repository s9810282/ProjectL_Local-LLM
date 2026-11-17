using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>나머지 보유 값을 보존하며 미전직 메인의 병과를 한 번만 정한다.</summary>
    public sealed class MainSpecializationChange
    {
        public OwnedCharacterRecord Expected { get; }
        public OwnedCharacterRecord Final { get; }

        public MainSpecializationChange(OwnedCharacterRecord expected, OwnedCharacterRecord final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.Role != CharacterOwnershipRole.Main || expected.SpecializationCategory.HasValue ||
                !final.SpecializationCategory.HasValue ||
                final.SpecializationCategory != CategoryKey.Rider && final.SpecializationCategory != CategoryKey.Heavy &&
                final.SpecializationCategory != CategoryKey.Cannon ||
                expected.Id != final.Id || expected.CharacterKey != final.CharacterKey || expected.Role != final.Role ||
                expected.Level != final.Level || expected.Experience != final.Experience ||
                expected.HunterAttackBonus != final.HunterAttackBonus ||
                expected.PlayerSkillUpgrade != final.PlayerSkillUpgrade || !expected.SameGrowth(final))
                throw new ArgumentException("전직은 미전직 메인의 병과만 Rider·Heavy·Cannon 중 하나로 바꿀 수 있습니다.", nameof(final));
        }
    }
}
