using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class CategoryTraitProgressionSpec
    {
        public const int DefaultCommonWeight = 1;
        public const int DefaultCategoryWeight = 2;

        public CategoryKey Category { get; }
        public IReadOnlyList<TraitGrowthStepSpec> Steps { get; }
        public IReadOnlyList<TraitKey> Pool { get; }
        public int CommonWeight { get; }
        public int CategoryWeight { get; }

        public CategoryTraitProgressionSpec(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps, IReadOnlyList<TraitKey> pool)
            : this(category, steps, pool, DefaultCommonWeight, DefaultCategoryWeight)
        {
        }

        public CategoryTraitProgressionSpec(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps,
            IReadOnlyList<TraitKey> pool, int commonWeight, int categoryWeight)
        {
            if (category != CategoryKey.Main && category != CategoryKey.Heavy &&
                category != CategoryKey.Rider && category != CategoryKey.Cannon)
                throw new ArgumentException("성장을 지원하는 플레이어 병과가 아닙니다.", nameof(category));
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            ValidateWeights(commonWeight, categoryWeight);
            if (steps.Any(s => s == null) || steps.Select(s => s.StepId).Distinct().Count() != steps.Count)
                throw new ArgumentException("성장 단계가 비어 있거나 식별자가 중복됩니다.", nameof(steps));
            for (int i = 1; i < steps.Count; i++)
                if (steps[i].UnlockLevel < steps[i - 1].UnlockLevel)
                    throw new ArgumentException("성장 단계의 레벨은 오름차순이어야 합니다.", nameof(steps));
            if (pool.Any(k => k == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), k)) ||
                pool.Distinct().Count() != pool.Count)
                throw new ArgumentException("성장 후보 키가 올바르지 않거나 중복됩니다.", nameof(pool));
            Category = category;
            CommonWeight = commonWeight;
            CategoryWeight = categoryWeight;
            Steps = Array.AsReadOnly(steps.ToArray());
            Pool = Array.AsReadOnly(pool.ToArray());
        }

        internal static void ValidateWeights(int commonWeight, int categoryWeight)
        {
            if (commonWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(commonWeight), "공용 특성 가중치는 양수여야 합니다.");
            if (categoryWeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(categoryWeight), "병과 특성 가중치는 양수여야 합니다.");
            if ((long)commonWeight + categoryWeight > int.MaxValue)
                throw new ArgumentException("특성 성장 가중치의 합이 정수 범위를 벗어났습니다.", nameof(categoryWeight));
        }
    }
}
