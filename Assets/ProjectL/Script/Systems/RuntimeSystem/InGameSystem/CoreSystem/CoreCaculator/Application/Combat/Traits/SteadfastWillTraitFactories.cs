using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class SteadfastWillTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.SteadfastWill;
        public Type SpecType => typeof(SteadfastWillTraitSpec);
        public Type DefinitionType => typeof(SteadfastWillTraitDefinition);
        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not SteadfastWillTraitSpec specification) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new SteadfastWillTraitDefinition();
        }
    }
    public sealed class SteadfastWillRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.SteadfastWill;
        public Type DefinitionType => typeof(SteadfastWillTraitDefinition);
        public Type RuntimeStateType => typeof(SteadfastWillRuntimeState);
        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not SteadfastWillTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new SteadfastWillRuntimeState();
        }
    }
}
