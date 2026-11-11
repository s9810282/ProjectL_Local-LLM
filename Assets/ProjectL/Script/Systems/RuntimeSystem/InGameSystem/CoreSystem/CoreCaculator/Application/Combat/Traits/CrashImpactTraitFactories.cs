using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class CrashImpactTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.CrashImpact;
        public Type SpecType => typeof(CrashImpactTraitSpec);
        public Type DefinitionType => typeof(CrashImpactTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not CrashImpactTraitSpec crash)
                throw new ArgumentException("작렬 정의에는 작렬 명세가 필요합니다.", nameof(spec));
            return new CrashImpactTraitDefinition(crash.DistanceCells, crash.WallDamagePercent, crash.UnitDamagePercent);
        }
    }

    public sealed class CrashImpactRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.CrashImpact;
        public Type DefinitionType => typeof(CrashImpactTraitDefinition);
        public Type RuntimeStateType => typeof(CrashImpactRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not CrashImpactTraitDefinition)
                throw new ArgumentException("작렬 상태에는 작렬 정의가 필요합니다.", nameof(definition));
            return new CrashImpactRuntimeState();
        }
    }
}
