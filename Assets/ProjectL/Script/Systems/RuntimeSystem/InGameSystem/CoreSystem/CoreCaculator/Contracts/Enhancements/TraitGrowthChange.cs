using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    /// <summary>식별·레벨·경험치와 기존 접두 이력을 보존하며 선택 하나만 추가한다.</summary>
    public sealed class TraitGrowthChange
    {
        public OwnedCharacterRecord Expected { get; }
        public OwnedCharacterRecord Final { get; }
        public TraitGrowthSelection Added { get; }
        public TraitGrowthChange(OwnedCharacterRecord expected, OwnedCharacterRecord final)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.Id != final.Id || expected.CharacterKey != final.CharacterKey || expected.Role != final.Role ||
                expected.Level != final.Level || expected.Experience != final.Experience ||
                expected.HunterAttackBonus != final.HunterAttackBonus ||
                expected.SpecializationCategory != final.SpecializationCategory ||
                expected.PlayerSkillUpgrade != final.PlayerSkillUpgrade ||
                !expected.Growth.SamePurchases(final.Growth) ||
                final.Growth.Selections.Count != expected.Growth.Selections.Count + 1 ||
                expected.Growth.Selections.Where((s, i) => !s.SameValues(final.Growth.Selections[i])).Any())
                throw new ArgumentException("기존 값과 이력을 보존하며 선택 하나만 추가해야 합니다.", nameof(final));
            Added = final.Growth.Selections.Last();
        }
    }
}
