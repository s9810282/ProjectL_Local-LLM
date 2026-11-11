using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class FearTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Fear;
        public Type SpecType => typeof(FearTraitSpec);
        public Type DefinitionType => typeof(FearTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not FearTraitSpec) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new FearTraitDefinition();
        }
    }

    public sealed class FearRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Fear;
        public Type DefinitionType => typeof(FearTraitDefinition);
        public Type RuntimeStateType => typeof(FearRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not FearTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new FearRuntimeState();
        }
    }
}
