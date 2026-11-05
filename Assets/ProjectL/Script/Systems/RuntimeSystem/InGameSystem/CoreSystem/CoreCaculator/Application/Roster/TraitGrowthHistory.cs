using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>선택 이력을 현재 단계와 순서대로 연결한다. 저장 상태는 변경하지 않는다.</summary>
    public static class TraitGrowthHistory
    {
        public static IReadOnlyList<TraitGrowthStepSpec> ResolveSteps(OwnedCharacterRecord owned, CategoryTraitProgressionSpec progression)
        {
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            var result = new List<TraitGrowthStepSpec>();
            foreach (var selection in owned.Growth.Selections)
            {
                if (result.Count >= progression.Steps.Count)
                    throw InvalidHistory(owned, progression, selection);
                var step = progression.Steps[result.Count];
                if (step.StepId != selection.StepId || owned.Level < step.UnlockLevel ||
                    !progression.Pool.Contains(selection.TraitKey) ||
                    selection.Kind != TraitGrowthKind.None && selection.Kind != step.Kind)
                    throw InvalidHistory(owned, progression, selection);
                result.Add(step);
            }
            return result.AsReadOnly();
        }

        private static InvalidOperationException InvalidHistory(OwnedCharacterRecord owned,
            CategoryTraitProgressionSpec progression, TraitGrowthSelection selection) =>
            new($"성장 이력이 검증된 단계·후보와 다릅니다: Unit={owned.CharacterKey}, Category={progression.Category}, Step={selection.StepId}, Trait={selection.TraitKey}.");
    }
}
