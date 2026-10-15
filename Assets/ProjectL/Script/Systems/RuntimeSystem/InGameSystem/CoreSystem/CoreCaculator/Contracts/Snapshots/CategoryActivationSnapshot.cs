using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// Category 규칙이 실제로 발동했다는 표시용 결과입니다.
    /// TraitActivationSnapshot과 분리되어 전달됩니다.
    /// </summary>
    public sealed class CategoryActivationSnapshot
    {
        public EntityId SourceUnitId { get; }
        public EntityId? TargetId { get; }
        public CategoryKey CategoryKey { get; }
        public CategoryRuleKey CategoryRuleKey { get; }
        public CategoryActivationKind CategoryActivationKind { get; }
        public int Magnitude { get; }

        public CategoryActivationSnapshot(
            EntityId sourceUnitId,
            EntityId? targetId,
            CategoryKey categoryKey,
            CategoryRuleKey categoryRuleKey,
            CategoryActivationKind categoryActivationKind,
            int magnitude)
        {
            if (sourceUnitId == EntityId.Empty)
                throw new ArgumentException(nameof(sourceUnitId));
            if (targetId.HasValue && targetId.Value == EntityId.Empty)
                throw new ArgumentException(nameof(targetId));
            if (categoryKey == CategoryKey.None ||
                Enum.IsDefined(typeof(CategoryKey), categoryKey) == false)
                throw new ArgumentOutOfRangeException(nameof(categoryKey));
            if (categoryRuleKey == CategoryRuleKey.None ||
                Enum.IsDefined(typeof(CategoryRuleKey), categoryRuleKey) == false)
                throw new ArgumentOutOfRangeException(nameof(categoryRuleKey));
            if (categoryActivationKind == CategoryActivationKind.None ||
                Enum.IsDefined(typeof(CategoryActivationKind), categoryActivationKind) == false)
                throw new ArgumentOutOfRangeException(nameof(categoryActivationKind));
            if (magnitude < 0)
                throw new ArgumentOutOfRangeException(nameof(magnitude));

            SourceUnitId = sourceUnitId;
            TargetId = targetId;
            CategoryKey = categoryKey;
            CategoryRuleKey = categoryRuleKey;
            CategoryActivationKind = categoryActivationKind;
            Magnitude = magnitude;
        }
    }
}
