using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class PullTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Pull;
        public Type SpecType => typeof(PullTraitSpec);
        public Type DefinitionType => typeof(PullTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not PullTraitSpec mark)
                throw new ArgumentException("끌어오기 정의에는 끌어오기 명세가 필요합니다.", nameof(spec));
            return new PullTraitDefinition(mark.ExtraRangeCells);
        }
    }

    public sealed class PullRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Pull;
        public Type DefinitionType => typeof(PullTraitDefinition);
        public Type RuntimeStateType => typeof(PullRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not PullTraitDefinition)
                throw new ArgumentException("끌어오기 상태에는 끌어오기 정의가 필요합니다.", nameof(definition));
            return new PullRuntimeState();
        }
    }
}
