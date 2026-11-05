using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class CategoryTraitProgressionSpec
    {
        public CategoryKey Category { get; }
        public IReadOnlyList<TraitGrowthStepSpec> Steps { get; }
        public IReadOnlyList<TraitKey> Pool { get; }
        public CategoryTraitProgressionSpec(CategoryKey category, IReadOnlyList<TraitGrowthStepSpec> steps, IReadOnlyList<TraitKey> pool)
        {
            if (category != CategoryKey.Main && category != CategoryKey.Heavy &&
                category != CategoryKey.Rider && category != CategoryKey.Cannon)
                throw new ArgumentException("성장을 지원하는 플레이어 병과가 아닙니다.", nameof(category));
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            if (pool == null) throw new ArgumentNullException(nameof(pool));
            if (steps.Any(s => s == null) || steps.Select(s => s.StepId).Distinct().Count() != steps.Count)
                throw new ArgumentException("성장 단계가 비어 있거나 식별자가 중복됩니다.", nameof(steps));
            for (int i = 1; i < steps.Count; i++)
                if (steps[i].UnlockLevel < steps[i - 1].UnlockLevel)
                    throw new ArgumentException("성장 단계의 레벨은 오름차순이어야 합니다.", nameof(steps));
            if (pool.Any(k => k == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), k)) ||
                pool.Distinct().Count() != pool.Count)
                throw new ArgumentException("성장 후보 키가 올바르지 않거나 중복됩니다.", nameof(pool));
            Category = category;
            Steps = Array.AsReadOnly(steps.ToArray());
            Pool = Array.AsReadOnly(pool.ToArray());
        }
    }
}
