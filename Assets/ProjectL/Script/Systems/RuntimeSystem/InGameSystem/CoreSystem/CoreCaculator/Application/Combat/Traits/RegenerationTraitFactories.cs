using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    /// <summary>재생 명세의 회복량을 도메인 정의로 변환합니다.</summary>
    public sealed class RegenerationTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Regeneration;
        public Type SpecType => typeof(RegenerationTraitSpec);
        public Type DefinitionType => typeof(RegenerationTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not RegenerationTraitSpec regeneration)
                throw new ArgumentException("재생 정의에는 재생 명세가 필요합니다.", nameof(spec));

            return new RegenerationTraitDefinition(regeneration.HealAmount, regeneration.HealEveryOwnSideTurnEnds);
        }
    }

    /// <summary>재생 주기 진행값이 0인 런타임 항목을 생성합니다.</summary>
    public sealed class RegenerationRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Regeneration;
        public Type DefinitionType => typeof(RegenerationTraitDefinition);
        public Type RuntimeStateType => typeof(RegenerationRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not RegenerationTraitDefinition)
                throw new ArgumentException("재생 상태에는 재생 정의가 필요합니다.", nameof(definition));

            return new RegenerationRuntimeState();
        }
    }
}
