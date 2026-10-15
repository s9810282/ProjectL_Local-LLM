using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    internal sealed class TraitRegistry
    {
        private readonly Dictionary<TraitKey, TraitRegistration> _registrations;

        internal IReadOnlyList<TraitRegistration> OrderedRegistrations { get; }

        internal TraitRegistry(TraitRegistration[] registrations)
        {
            if (registrations == null)
                throw new ArgumentNullException(nameof(registrations));

            _registrations = new Dictionary<TraitKey, TraitRegistration>(registrations.Length);

            for (int i = 0; i < registrations.Length; i++)
            {
                TraitRegistration registration = registrations[i];

                if (registration == null)
                {
                    throw new ArgumentException(
                        $"Registration 목록의 {i}번 항목은 null일 수 없습니다.",
                        nameof(registrations));
                }

                if (registration.TraitKey == TraitKey.None ||
                    Enum.IsDefined(typeof(TraitKey), registration.TraitKey) == false)
                {
                    throw new ArgumentException(
                        $"Registration 목록의 {i}번 항목에 유효하지 않은 TraitKey가 있습니다.",
                        nameof(registrations));
                }

                if (registration.TraitKey != registration.Rule.Key)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration의 Rule Key가 {registration.Rule.Key}로 일치하지 않습니다.",
                        nameof(registrations));
                }

                if (typeof(TraitSpec).IsAssignableFrom(registration.SpecType) == false ||
                    registration.SpecType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration에는 구체적인 TraitSpec 타입이 필요합니다.",
                        nameof(registrations));
                }

                if (typeof(TraitDefinition).IsAssignableFrom(registration.DefinitionType) == false ||
                    registration.DefinitionType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration에는 구체적인 TraitDefinition 타입이 필요합니다.",
                        nameof(registrations));
                }

                if (typeof(TraitRuntimeState).IsAssignableFrom(registration.RuntimeStateType) == false ||
                    registration.RuntimeStateType.IsAbstract)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration에는 구체적인 TraitRuntimeState 타입이 필요합니다.",
                        nameof(registrations));
                }

                ITraitDefinitionFactory definitionFactory =
                    registration.TraitDefinitionFactory;
                if (definitionFactory.TraitKey != registration.TraitKey ||
                    definitionFactory.SpecType != registration.SpecType ||
                    definitionFactory.DefinitionType != registration.DefinitionType)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration의 Definition Factory 조합이 선언 타입과 일치하지 않습니다.",
                        nameof(registrations));
                }

                ITraitRuntimeStateFactory runtimeStateFactory =
                    registration.TraitRuntimeStateFactory;
                if (runtimeStateFactory.TraitKey != registration.TraitKey ||
                    runtimeStateFactory.DefinitionType != registration.DefinitionType ||
                    runtimeStateFactory.RuntimeStateType != registration.RuntimeStateType)
                {
                    throw new ArgumentException(
                        $"{registration.TraitKey} Registration의 RuntimeState Factory 조합이 선언 타입과 일치하지 않습니다.",
                        nameof(registrations));
                }

                if (_registrations.TryAdd(registration.TraitKey, registration) == false)
                {
                    throw new ArgumentException(
                        $"TraitKey {registration.TraitKey}가 중복 등록되었습니다.",
                        nameof(registrations));
                }
            }

            TraitRegistration[] orderedRegistrations = registrations
                .OrderBy(registration => registration.TraitKey)
                .ToArray();

            OrderedRegistrations = Array.AsReadOnly(orderedRegistrations);
        }

        internal TraitRegistration GetRequiredRegistration(TraitKey key)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));

            if (_registrations.TryGetValue(key, out TraitRegistration registration) == false)
            {
                throw new InvalidOperationException(
                    $"TraitKey {key}에 해당하는 Registration이 없습니다.");
            }

            return registration;
        }

        internal static TraitRegistry CreateDefault()
        {
            var focusRule = new FocusTraitRule();
            TraitRegistration focusRegistration = new TraitRegistration(
                TraitKey.Focus,
                typeof(FocusTraitSpec),
                typeof(FocusTraitDefinition),
                typeof(FocusRuntimeState),
                new FocusTraitDefinitionFactory(),
                new FocusRuntimeStateFactory(),
                focusRule,
                definition => new FocusEffectRule((FocusTraitDefinition)definition, focusRule));

            var discountRule = new MoveAttackDiscountTraitRule();
            TraitRegistration moveAttackDiscountRegistration = new TraitRegistration(
                TraitKey.MoveAttackDiscount,
                typeof(MoveAttackDiscountTraitSpec),
                typeof(MoveAttackDiscountTraitDefinition),
                typeof(MoveAttackDiscountRuntimeState),
                new MoveAttackDiscountTraitDefinitionFactory(),
                new MoveAttackDiscountRuntimeStateFactory(),
                discountRule,
                definition => new MoveAttackDiscountEffectRule(
                    (MoveAttackDiscountTraitDefinition)definition,
                    discountRule));

            var falloffRule = new PiercingFalloffTraitRule();
            TraitRegistration piercingFalloffRegistration = new TraitRegistration(
                TraitKey.PiercingFalloff,
                typeof(PiercingFalloffTraitSpec),
                typeof(PiercingFalloffTraitDefinition),
                typeof(PiercingFalloffRuntimeState),
                new PiercingFalloffTraitDefinitionFactory(),
                new PiercingFalloffRuntimeStateFactory(),
                falloffRule,
                definition => new PiercingFalloffEffectRule(
                    (PiercingFalloffTraitDefinition)definition,
                    falloffRule));

            var thornFieldRule = new ThornFieldTraitRule();
            TraitRegistration thornFieldRegistration = new TraitRegistration(
                TraitKey.ThornField,
                typeof(ThornFieldTraitSpec),
                typeof(ThornFieldTraitDefinition),
                typeof(ThornFieldRuntimeState),
                new ThornFieldTraitDefinitionFactory(),
                new ThornFieldRuntimeStateFactory(),
                thornFieldRule,
                definition => new ThornFieldEffectRule(
                    (ThornFieldTraitDefinition)definition,
                    thornFieldRule));

            var knockbackRule = new KnockbackTraitRule();
            TraitRegistration knockbackRegistration = new TraitRegistration(
                TraitKey.Knockback,
                typeof(KnockbackTraitSpec),
                typeof(KnockbackTraitDefinition),
                typeof(KnockbackRuntimeState),
                new KnockbackTraitDefinitionFactory(),
                new KnockbackRuntimeStateFactory(),
                knockbackRule,
                definition => new KnockbackEffectRule(
                    (KnockbackTraitDefinition)definition,
                    knockbackRule));

            var distanceDamageScaleRule = new DistanceDamageScaleTraitRule();
            TraitRegistration distanceDamageScaleRegistration = new TraitRegistration(
                TraitKey.DistanceDamageScale,
                typeof(DistanceDamageScaleTraitSpec),
                typeof(DistanceDamageScaleTraitDefinition),
                typeof(DistanceDamageScaleRuntimeState),
                new DistanceDamageScaleTraitDefinitionFactory(),
                new DistanceDamageScaleRuntimeStateFactory(),
                distanceDamageScaleRule,
                definition => new DistanceDamageScaleEffectRule(
                    (DistanceDamageScaleTraitDefinition)definition,
                    distanceDamageScaleRule));

            var killChainAttackRule = new KillChainAttackTraitRule();
            TraitRegistration killChainAttackRegistration = new TraitRegistration(
                TraitKey.KillChainAttack,
                typeof(KillChainAttackTraitSpec),
                typeof(KillChainAttackTraitDefinition),
                typeof(KillChainAttackRuntimeState),
                new KillChainAttackTraitDefinitionFactory(),
                new KillChainAttackRuntimeStateFactory(),
                killChainAttackRule,
                definition => new KillChainAttackEffectRule(
                    (KillChainAttackTraitDefinition)definition,
                    killChainAttackRule));

            TraitRegistration[] traitRegistrations =
            {
                focusRegistration,
                moveAttackDiscountRegistration,
                piercingFalloffRegistration,
                thornFieldRegistration,
                knockbackRegistration,
                distanceDamageScaleRegistration,
                killChainAttackRegistration,
            };

            return new TraitRegistry(traitRegistrations);
        }
    }
}
