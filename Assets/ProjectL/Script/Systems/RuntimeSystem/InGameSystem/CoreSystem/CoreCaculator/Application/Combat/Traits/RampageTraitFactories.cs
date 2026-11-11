using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public sealed class RampageTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Rampage;
        public Type SpecType => typeof(RampageTraitSpec);
        public Type DefinitionType => typeof(RampageTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec is not RampageTraitSpec rampage) throw new ArgumentException("같은 특성 명세가 필요합니다.", nameof(spec));
            return new RampageTraitDefinition(rampage.BaseAttackBonus, rampage.RankAttackBonus, rampage.MovementBonus);
        }
    }

    public sealed class RampageRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Rampage;
        public Type DefinitionType => typeof(RampageTraitDefinition);
        public Type RuntimeStateType => typeof(RampageRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (definition is not RampageTraitDefinition) throw new ArgumentException("같은 특성 정의가 필요합니다.", nameof(definition));
            return new RampageRuntimeState();
        }
    }
}
