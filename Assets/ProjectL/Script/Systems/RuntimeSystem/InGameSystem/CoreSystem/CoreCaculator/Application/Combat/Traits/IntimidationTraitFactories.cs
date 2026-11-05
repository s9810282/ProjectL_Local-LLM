using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class IntimidationTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Intimidation;
        public Type SpecType => typeof(IntimidationTraitSpec);
        public Type DefinitionType => typeof(IntimidationTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not IntimidationTraitSpec intimidation)
                throw new ArgumentException("위압 정의에는 위압 명세가 필요합니다.", nameof(spec));

            return new IntimidationTraitDefinition(intimidation.AttackReductionPercent);
        }
    }

    public sealed class IntimidationRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Intimidation;
        public Type DefinitionType => typeof(IntimidationTraitDefinition);
        public Type RuntimeStateType => typeof(IntimidationRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not IntimidationTraitDefinition)
                throw new ArgumentException("위압 상태에는 위압 정의가 필요합니다.", nameof(definition));

            return new IntimidationRuntimeState();
        }
    }
}
