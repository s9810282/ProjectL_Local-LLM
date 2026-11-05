using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class ImpactBoltTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.ImpactBolt;
        public Type SpecType => typeof(ImpactBoltTraitSpec);
        public Type DefinitionType => typeof(ImpactBoltTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not ImpactBoltTraitSpec bolt)
                throw new ArgumentException("충격 볼트 정의에는 충격 볼트 명세가 필요합니다.", nameof(spec));
            return new ImpactBoltTraitDefinition(bolt.DistanceCells);
        }
    }

    public sealed class ImpactBoltRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.ImpactBolt;
        public Type DefinitionType => typeof(ImpactBoltTraitDefinition);
        public Type RuntimeStateType => typeof(ImpactBoltRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not ImpactBoltTraitDefinition)
                throw new ArgumentException("충격 볼트 상태에는 충격 볼트 정의가 필요합니다.", nameof(definition));
            return new ImpactBoltRuntimeState();
        }
    }
}
