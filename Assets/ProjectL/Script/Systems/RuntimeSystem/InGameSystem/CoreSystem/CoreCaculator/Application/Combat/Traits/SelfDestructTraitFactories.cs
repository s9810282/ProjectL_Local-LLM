using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class SelfDestructTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.SelfDestruct;
        public Type SpecType => typeof(SelfDestructTraitSpec);
        public Type DefinitionType => typeof(SelfDestructTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not SelfDestructTraitSpec selfDestruct) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new SelfDestructTraitDefinition(selfDestruct.Level);
        }
    }

    public sealed class SelfDestructRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.SelfDestruct;
        public Type DefinitionType => typeof(SelfDestructTraitDefinition);
        public Type RuntimeStateType => typeof(SelfDestructRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not SelfDestructTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new SelfDestructRuntimeState();
        }
    }
}
