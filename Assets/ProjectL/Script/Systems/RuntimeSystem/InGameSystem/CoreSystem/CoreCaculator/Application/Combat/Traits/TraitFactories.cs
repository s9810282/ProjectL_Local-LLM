using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    public interface ITraitDefinitionFactory
    {
        TraitKey TraitKey { get; }
        Type SpecType { get; }
        Type DefinitionType { get; }

        TraitDefinition BuildDefinition(TraitSpec spec);
    }

    public interface ITraitRuntimeStateFactory
    {
        TraitKey TraitKey { get; }
        Type DefinitionType { get; }
        Type RuntimeStateType { get; }

        TraitRuntimeState BuildRuntimeState(TraitDefinition definition);
    }

    public sealed class FocusTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Focus;
        public Type SpecType => typeof(FocusTraitSpec);
        public Type DefinitionType => typeof(FocusTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not FocusTraitSpec focusTraitSpec)
            {
                throw new ArgumentException(
                    "Focus Definition을 만들려면 FocusTraitSpec이 필요합니다.",
                    nameof(spec));
            }

            return new FocusTraitDefinition(focusTraitSpec.DamagePerStack, focusTraitSpec.MaxStacks);
        }
    }

    public sealed class MoveAttackDiscountTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.MoveAttackDiscount;
        public Type SpecType => typeof(MoveAttackDiscountTraitSpec);
        public Type DefinitionType => typeof(MoveAttackDiscountTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not MoveAttackDiscountTraitSpec moveAttackDiscountTraitSpec)
            {
                throw new ArgumentException(
                    "MoveAttackDiscount Definition을 만들려면 MoveAttackDiscountTraitSpec이 필요합니다.",
                    nameof(spec));
            }

            return new MoveAttackDiscountTraitDefinition(moveAttackDiscountTraitSpec.DiscountAmount, moveAttackDiscountTraitSpec.MinimumAttackCost);
        }
    }

    public sealed class FocusRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Focus;
        public Type DefinitionType => typeof(FocusTraitDefinition);
        public Type RuntimeStateType => typeof(FocusRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not FocusTraitDefinition)
            {
                throw new ArgumentException(
                    "Focus RuntimeState를 만들려면 FocusTraitDefinition이 필요합니다.",
                    nameof(definition));
            }

            return new FocusRuntimeState();
        }
    }

    public sealed class MoveAttackDiscountRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.MoveAttackDiscount;
        public Type DefinitionType => typeof(MoveAttackDiscountTraitDefinition);
        public Type RuntimeStateType => typeof(MoveAttackDiscountRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not MoveAttackDiscountTraitDefinition)
            {
                throw new ArgumentException(
                    "MoveAttackDiscount RuntimeState를 만들려면 MoveAttackDiscountTraitDefinition이 필요합니다.",
                    nameof(definition));
            }

            return new MoveAttackDiscountRuntimeState();
        }
    }

    public sealed class PiercingFalloffTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.PiercingFalloff;
        public Type SpecType => typeof(PiercingFalloffTraitSpec);
        public Type DefinitionType => typeof(PiercingFalloffTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not PiercingFalloffTraitSpec falloffSpec)
            {
                throw new ArgumentException(
                    "PiercingFalloff definition requires a matching spec.",
                    nameof(spec));
            }

            return new PiercingFalloffTraitDefinition(
                falloffSpec.ReductionPercentPerTarget, falloffSpec.MaxTargetsPerRay);
        }
    }

    public sealed class PiercingFalloffRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.PiercingFalloff;
        public Type DefinitionType => typeof(PiercingFalloffTraitDefinition);
        public Type RuntimeStateType => typeof(PiercingFalloffRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not PiercingFalloffTraitDefinition)
            {
                throw new ArgumentException(
                    "PiercingFalloff runtime state requires a matching definition.",
                    nameof(definition));
            }

            return new PiercingFalloffRuntimeState();
        }
    }

    public sealed class ThornFieldTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.ThornField;
        public Type SpecType => typeof(ThornFieldTraitSpec);
        public Type DefinitionType => typeof(ThornFieldTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not ThornFieldTraitSpec thornFieldSpec)
            {
                throw new ArgumentException(
                    "ThornField definition requires a matching spec.",
                    nameof(spec));
            }

            return new ThornFieldTraitDefinition(
                thornFieldSpec.ThornDamage,
                thornFieldSpec.LifetimeUnitTurns);
        }
    }

    public sealed class ThornFieldRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.ThornField;
        public Type DefinitionType => typeof(ThornFieldTraitDefinition);
        public Type RuntimeStateType => typeof(ThornFieldRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not ThornFieldTraitDefinition)
            {
                throw new ArgumentException(
                    "ThornField runtime state requires a matching definition.",
                    nameof(definition));
            }

            return new ThornFieldRuntimeState();
        }
    }

    public sealed class KnockbackTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.Knockback;
        public Type SpecType => typeof(KnockbackTraitSpec);
        public Type DefinitionType => typeof(KnockbackTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not KnockbackTraitSpec knockbackSpec)
            {
                throw new ArgumentException(
                    "Knockback definition requires a matching spec.",
                    nameof(spec));
            }

            return new KnockbackTraitDefinition(knockbackSpec.DistanceCells);
        }
    }

    public sealed class KnockbackRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.Knockback;
        public Type DefinitionType => typeof(KnockbackTraitDefinition);
        public Type RuntimeStateType => typeof(KnockbackRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not KnockbackTraitDefinition)
            {
                throw new ArgumentException(
                    "Knockback runtime state requires a matching definition.",
                    nameof(definition));
            }

            return new KnockbackRuntimeState();
        }
    }

    public sealed class DistanceDamageScaleTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.DistanceDamageScale;
        public Type SpecType => typeof(DistanceDamageScaleTraitSpec);
        public Type DefinitionType => typeof(DistanceDamageScaleTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not DistanceDamageScaleTraitSpec scaleSpec)
            {
                throw new ArgumentException(
                    "DistanceDamageScale Definition을 만들려면 DistanceDamageScaleTraitSpec이 필요합니다.",
                    nameof(spec));
            }

            return new DistanceDamageScaleTraitDefinition(scaleSpec.PercentByDistance);
        }
    }

    public sealed class DistanceDamageScaleRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.DistanceDamageScale;
        public Type DefinitionType => typeof(DistanceDamageScaleTraitDefinition);
        public Type RuntimeStateType => typeof(DistanceDamageScaleRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not DistanceDamageScaleTraitDefinition)
            {
                throw new ArgumentException(
                    "DistanceDamageScale RuntimeState를 만들려면 DistanceDamageScaleTraitDefinition이 필요합니다.",
                    nameof(definition));
            }

            return new DistanceDamageScaleRuntimeState();
        }
    }

    public sealed class KillChainAttackTraitDefinitionFactory : ITraitDefinitionFactory
    {
        public TraitKey TraitKey => TraitKey.KillChainAttack;
        public Type SpecType => typeof(KillChainAttackTraitSpec);
        public Type DefinitionType => typeof(KillChainAttackTraitDefinition);

        public TraitDefinition BuildDefinition(TraitSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec is not KillChainAttackTraitSpec)
            {
                throw new ArgumentException(
                    "KillChainAttack Definition을 만들려면 KillChainAttackTraitSpec이 필요합니다.",
                    nameof(spec));
            }

            return new KillChainAttackTraitDefinition();
        }
    }

    public sealed class KillChainAttackRuntimeStateFactory : ITraitRuntimeStateFactory
    {
        public TraitKey TraitKey => TraitKey.KillChainAttack;
        public Type DefinitionType => typeof(KillChainAttackTraitDefinition);
        public Type RuntimeStateType => typeof(KillChainAttackRuntimeState);

        public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (definition is not KillChainAttackTraitDefinition)
            {
                throw new ArgumentException(
                    "KillChainAttack RuntimeState를 만들려면 KillChainAttackTraitDefinition이 필요합니다.",
                    nameof(definition));
            }

            return new KillChainAttackRuntimeState();
        }
    }
}
