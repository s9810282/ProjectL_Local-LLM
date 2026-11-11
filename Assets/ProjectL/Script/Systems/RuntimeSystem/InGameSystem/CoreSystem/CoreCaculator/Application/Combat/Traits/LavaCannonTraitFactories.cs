using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class LavaCannonTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.LavaCannon;
        public Type SpecType => typeof(LavaCannonTraitSpec);
        public Type DefinitionType => typeof(LavaCannonTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not LavaCannonTraitSpec specification) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new LavaCannonTraitDefinition();
        }
    }

    public sealed class LavaCannonRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.LavaCannon;
        public Type DefinitionType => typeof(LavaCannonTraitDefinition);
        public Type RuntimeStateType => typeof(LavaCannonRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not LavaCannonTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new LavaCannonRuntimeState();
        }
    }
}
