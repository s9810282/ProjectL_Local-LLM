using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item
{
    [Serializable]
    internal sealed class TraitGrowthStepAuthoring
    {
        [SerializeField, Min(1)] private int stepId = 1;
        [SerializeField, Min(2)] private int unlockLevel = 2;
        [SerializeField] private TraitGrowthKind kind = TraitGrowthKind.Acquire;

        internal TraitGrowthStepSpec ToSpec() => new(stepId, unlockLevel, kind);
    }

    [CreateAssetMenu(fileName = "CategoryTraitProgression", menuName = "ProjectL/Database/InGame/CategoryTraitProgression")]
    internal sealed class CategoryTraitProgressionDataSO : DatabaseItem
    {
        [SerializeField] private CategoryKey category;
        [SerializeField, Min(1)] private int commonWeight = CategoryTraitProgressionSpec.DefaultCommonWeight;
        [SerializeField, Min(1)] private int categoryWeight = CategoryTraitProgressionSpec.DefaultCategoryWeight;
        [SerializeField] private List<TraitGrowthStepAuthoring> steps = new();
        [SerializeField] private List<TraitGrowthDefinitionDataSO> pool = new();
        public override IData GetData()
        {
            if (steps == null || pool == null || steps.Any(s => s == null) || pool.Any(p => p == null))
                throw new InvalidOperationException($"성장 단계 또는 후보 참조가 비어 있습니다: {name}.");
            foreach (var definition in pool)
            {
                var trait = (TraitGrowthDefinitionData)definition.GetData();
                if (ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.DeferredCombatFeatures.GetTraitUnavailableReason(trait.Key) == "ImplementationPending")
                    throw new InvalidOperationException($"{name}, Trait {trait.Key}: ImplementationPending.");
            }
            try
            {
                return new CategoryTraitProgressionData(category, steps.Select(s => s.ToSpec()).ToArray(),
                    pool.Select(p => p.Key).ToArray(), commonWeight, categoryWeight);
            }
            catch (ArgumentException exception) when (
                exception.ParamName == nameof(commonWeight) || exception.ParamName == nameof(categoryWeight))
            {
                throw new InvalidOperationException(
                    $"병과 성장 가중치 설정이 올바르지 않습니다: {name} (commonWeight={commonWeight}, categoryWeight={categoryWeight}).",
                    exception);
            }
        }
    }
}
