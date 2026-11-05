using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class HuntersMarkTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.HuntersMark;
        public Type SpecType => typeof(HuntersMarkTraitSpec);
        public Type DefinitionType => typeof(HuntersMarkTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not HuntersMarkTraitSpec mark)
                throw new ArgumentException("표식 정의에는 표식 명세가 필요합니다.", nameof(spec));
            return new HuntersMarkTraitDefinition(mark.BonusDamagePercent, mark.DurationAffectedSideTurnEnds);
        }
    }

    public sealed class HuntersMarkRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.HuntersMark;
        public Type DefinitionType => typeof(HuntersMarkTraitDefinition);
        public Type RuntimeStateType => typeof(HuntersMarkRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not HuntersMarkTraitDefinition)
                throw new ArgumentException("표식 상태에는 표식 정의가 필요합니다.", nameof(definition));
            return new HuntersMarkRuntimeState();
        }
    }
}
