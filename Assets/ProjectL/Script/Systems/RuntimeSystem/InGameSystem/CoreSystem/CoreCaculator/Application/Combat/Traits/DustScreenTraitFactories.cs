using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class DustScreenTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.DustScreen;
        public Type SpecType => typeof(DustScreenTraitSpec);
        public Type DefinitionType => typeof(DustScreenTraitDefinition);
        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not DustScreenTraitSpec specification) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new DustScreenTraitDefinition(specification.AttackRangeReduction, specification.AttackDamageReductionPercent, specification.OpposingTurnEndsToExpire);
        }
    }
    public sealed class DustScreenRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.DustScreen;
        public Type DefinitionType => typeof(DustScreenTraitDefinition);
        public Type RuntimeStateType => typeof(DustScreenRuntimeState);
        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not DustScreenTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new DustScreenRuntimeState();
        }
    }
}
