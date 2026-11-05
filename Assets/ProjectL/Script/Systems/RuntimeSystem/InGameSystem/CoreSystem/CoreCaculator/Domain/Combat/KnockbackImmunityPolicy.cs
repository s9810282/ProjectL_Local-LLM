using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>대상 카테고리와 명시적 강제 이동 면역으로 공격 넉백 면역만 판정한다.</summary>
    internal static class KnockbackImmunityPolicy
    {
        private const bool HeavyCategoryImmunityEnabled = false;

        internal static bool IsImmuneWithTrait(CategoryKey targetCategory, bool isForcedMovementImmune, bool traitImmunity)
            => traitImmunity || IsImmune(targetCategory, isForcedMovementImmune);

        internal static bool IsImmune(CategoryKey targetCategory, bool isForcedMovementImmune)
        {
            return IsImmune(targetCategory, isForcedMovementImmune, HeavyCategoryImmunityEnabled);
        }

        internal static bool IsImmune(CategoryKey targetCategory, bool isForcedMovementImmune,
            bool heavyCategoryImmunityEnabled)
        {
            return isForcedMovementImmune || (heavyCategoryImmunityEnabled && targetCategory == CategoryKey.Heavy);
        }
    }
}
