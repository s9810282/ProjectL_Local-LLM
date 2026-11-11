using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class SacrificeTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Sacrifice;
        public Type SpecType => typeof(SacrificeTraitSpec);
        public Type DefinitionType => typeof(SacrificeTraitDefinition);
        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not SacrificeTraitSpec specification) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new SacrificeTraitDefinition(specification.RadiusCells, specification.MaxTriggersPerEnemyTurn,
                specification.PreventsLethalSacrificeDamage, specification.ShieldOnActivation);
        }
    }
    public sealed class SacrificeRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Sacrifice;
        public Type DefinitionType => typeof(SacrificeTraitDefinition);
        public Type RuntimeStateType => typeof(SacrificeRuntimeState);
        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not SacrificeTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new SacrificeRuntimeState();
        }
    }
}
