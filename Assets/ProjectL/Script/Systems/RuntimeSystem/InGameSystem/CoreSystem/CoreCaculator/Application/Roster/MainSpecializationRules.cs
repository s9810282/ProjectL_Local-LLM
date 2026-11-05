using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>획득 경로와 무관하게 메인 전직의 순수 전이와 유효 전투 병과를 판정한다.</summary>
    public static class MainSpecializationRules
    {
        public static CategoryKey ResolveCombatCategory(CategoryKey identity, CategoryKey? specialization)
        {
            if (identity == CategoryKey.None || !Enum.IsDefined(typeof(CategoryKey), identity))
                throw new ArgumentOutOfRangeException(nameof(identity));
            if (!specialization.HasValue)
                return identity;
            if (identity != CategoryKey.Main || !IsSpecialization(specialization.Value))
                throw new ArgumentException("Main만 Rider·Heavy·Cannon 전투 병과를 선택할 수 있습니다.", nameof(specialization));
            return specialization.Value;
        }

        public static MainSpecializationDecision Plan(OwnedCharacterRecord expected, CategoryKey selected)
        {
            if (expected == null)
                throw new ArgumentNullException(nameof(expected));
            if (expected.Role != CharacterOwnershipRole.Main)
                return MainSpecializationDecision.Failure(MainSpecializationFailureReason.InvalidTarget);
            if (!IsSpecialization(selected))
                return MainSpecializationDecision.Failure(MainSpecializationFailureReason.InvalidCategory);
            if (expected.SpecializationCategory.HasValue)
                return MainSpecializationDecision.Failure(MainSpecializationFailureReason.AlreadySpecialized);
            var final = new OwnedCharacterRecord(expected.Id, expected.CharacterKey, expected.Role, expected.Level,
                expected.Experience, expected.Growth, expected.HunterAttackBonus, selected);
            return MainSpecializationDecision.Success(new MainSpecializationChange(expected, final));
        }

        private static bool IsSpecialization(CategoryKey category) =>
            category == CategoryKey.Rider || category == CategoryKey.Heavy || category == CategoryKey.Cannon;
    }
}
