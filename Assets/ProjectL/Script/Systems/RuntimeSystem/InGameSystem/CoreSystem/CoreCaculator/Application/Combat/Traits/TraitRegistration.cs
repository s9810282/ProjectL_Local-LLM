using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    internal sealed class TraitRegistration
    {
        public TraitKey TraitKey { get; }
        public Type SpecType { get; }
        public Type DefinitionType { get; }
        public Type RuntimeStateType { get; }
        public ITraitDefinitionFactory TraitDefinitionFactory { get; }
        public ITraitRuntimeStateFactory TraitRuntimeStateFactory { get; }
        public ICombatTraitRule Rule { get; }
        internal Func<TraitDefinition, ICombatEffectRule> EffectRuleFactory { get; }

        public TraitRegistration(
            TraitKey key, 
            Type specType, 
            Type definitionType, 
            Type runtimeStateType, 
            ITraitDefinitionFactory definitionFactory, 
            ITraitRuntimeStateFactory runtimeStateFactory,
            ICombatTraitRule traitRule,
            Func<TraitDefinition, ICombatEffectRule> effectRuleFactory)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            if (specType == null)
                throw new ArgumentNullException(nameof(specType));
            if (definitionType == null)
                throw new ArgumentNullException(nameof(definitionType));
            if (runtimeStateType == null)
                throw new ArgumentNullException(nameof(runtimeStateType));

            if (definitionFactory == null)
                throw new ArgumentNullException(nameof(definitionFactory));
            if (runtimeStateFactory == null)
                throw new ArgumentNullException(nameof(runtimeStateFactory));
            if (traitRule == null)
                throw new ArgumentNullException(nameof(traitRule));
            if (effectRuleFactory == null)
                throw new ArgumentNullException(nameof(effectRuleFactory));

            TraitKey = key;
            SpecType = specType;
            DefinitionType = definitionType;
            RuntimeStateType = runtimeStateType;
            TraitDefinitionFactory = definitionFactory;
            TraitRuntimeStateFactory = runtimeStateFactory;
            Rule = traitRule;
            EffectRuleFactory = effectRuleFactory;
        }
    }
}
