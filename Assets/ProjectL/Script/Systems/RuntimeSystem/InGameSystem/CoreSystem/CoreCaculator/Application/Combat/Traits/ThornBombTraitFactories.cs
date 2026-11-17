using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    internal sealed class ThornBombTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.ThornBomb;
        public Type SpecType => typeof(ThornBombTraitSpec);
        public Type DefinitionType => typeof(ThornBombTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not ThornBombTraitSpec bomb)
                throw new ArgumentException("가시 폭탄 특성 명세가 필요합니다.", nameof(spec));
            return new ThornBombTraitDefinition(bomb.DamagePercent);
        }
    }

    internal sealed class ThornBombRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.ThornBomb;
        public Type DefinitionType => typeof(ThornBombTraitDefinition);
        public Type RuntimeStateType => typeof(ThornBombRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not ThornBombTraitDefinition)
                throw new ArgumentException("가시 폭탄 특성 정의가 필요합니다.", nameof(definition));
            return new ThornBombRuntimeState();
        }
    }
}
