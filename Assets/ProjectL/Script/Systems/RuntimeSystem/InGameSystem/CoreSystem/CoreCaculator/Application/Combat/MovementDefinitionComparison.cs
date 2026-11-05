using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>이동 문맥의 정의 조합을 비교한다. 특성 설정의 비교는 각 특성이 소유한다.</summary>
    internal static class MovementDefinitionComparison
    {
        internal static bool SameDefinition(UnitSpec left, UnitSpec right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return left.Key == right.Key &&
                left.Role == right.Role &&
                left.MoveMethod == right.MoveMethod &&
                left.AttackRange == right.AttackRange &&
                left.AttackPower == right.AttackPower &&
                left.PhysicalDefense == right.PhysicalDefense &&
                left.MaxHealth == right.MaxHealth &&
                left.Priority == right.Priority &&
                left.CategoryKey == right.CategoryKey &&
                left.SpecializationCategory == right.SpecializationCategory &&
                left.OwnerEligibility == right.OwnerEligibility &&
                left.FootprintWidth == right.FootprintWidth &&
                left.FootprintHeight == right.FootprintHeight &&
                left.FootprintLocalCells.Span.SequenceEqual(right.FootprintLocalCells.Span) &&
                SameTraits(left.Traits.Span, right.Traits.Span) &&
                ReferenceEquals(left.Behavior, right.Behavior) &&
                ReferenceEquals(left.Occlusion, right.Occlusion) &&
                ReferenceEquals(left.KillReward, right.KillReward);
        }

        internal static bool SameProfile(CombatAttackProfile left, CombatAttackProfile right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return false;
            }

            return left.OwnedId == right.OwnedId &&
                ReferenceEquals(left.Method, right.Method) &&
                SameDefinition(left.Definition, right.Definition);
        }

        /// <summary>UnitSpec이 중복을 금지하는 TraitKey와 설정 값을 비교하며 입력 순서는 무시한다.</summary>
        private static bool SameTraits(ReadOnlySpan<TraitSpec> left, ReadOnlySpan<TraitSpec> right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            foreach (var trait in left)
            {
                TraitSpec matching = null;
                foreach (var candidate in right)
                {
                    if (trait.TraitKey == candidate.TraitKey)
                    {
                        matching = candidate;
                        break;
                    }
                }

                if (!trait.SameValues(matching))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
