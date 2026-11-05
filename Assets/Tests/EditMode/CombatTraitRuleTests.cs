using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Tests.Core
{
    public sealed class CombatTraitRuleTests
    {
        [TestCase(0, 3)]
        [TestCase(-1, 3)]
        [TestCase(2, 0)]
        [TestCase(2, -1)]
        public void FocusTraitDefinition_RejectsNonPositiveValues(
            int damagePerStack,
            int maxStacks)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FocusTraitDefinition(damagePerStack, maxStacks));
        }

        [Test]
        public void RuntimeStates_DefaultConstructors_CreateInitialState()
        {
            FocusRuntimeState focus = new FocusRuntimeState();
            RoarRuntimeState roar = new RoarRuntimeState();

            Assert.That(focus.TraitKey, Is.EqualTo(TraitKey.Focus));
            Assert.That(focus.ActiveStacks, Is.Zero);
            Assert.That(focus.GainScheduled, Is.False);
            Assert.That(focus.MovedThisTurn, Is.False);
            Assert.That(roar.TraitKey, Is.EqualTo(TraitKey.Roar));
            Assert.That(roar.HitCount, Is.Zero);
        }

        [Test]
        public void FocusRuntimeState_RejectsNegativeActiveStacks()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FocusRuntimeState(-1, gainScheduled: false, movedThisTurn: false));
        }

        [Test]
        public void RuntimeState_CloneCopiesValuesWithoutSharingLaterAssignments()
        {
            FocusRuntimeState focus =
                new FocusRuntimeState(1, gainScheduled: true, movedThisTurn: false);
            RoarRuntimeState roar =
                new RoarRuntimeState(1);

            FocusRuntimeState focusClone = (FocusRuntimeState)focus.Clone();
            RoarRuntimeState roarClone =
                (RoarRuntimeState)roar.Clone();
            focus.CopyFinalValuesFrom(
                new FocusRuntimeState(2, gainScheduled: false, movedThisTurn: true));
            roar.CopyFinalValuesFrom(
                new RoarRuntimeState(0));

            Assert.That(focusClone, Is.Not.SameAs(focus));
            Assert.That(focusClone.ActiveStacks, Is.EqualTo(1));
            Assert.That(focusClone.GainScheduled, Is.True);
            Assert.That(focusClone.MovedThisTurn, Is.False);
            Assert.That(focus.ActiveStacks, Is.EqualTo(2));
            Assert.That(focus.GainScheduled, Is.False);
            Assert.That(focus.MovedThisTurn, Is.True);
            Assert.That(roarClone, Is.Not.SameAs(roar));
            Assert.That(roarClone.HitCount, Is.EqualTo(1));
            Assert.That(roar.HitCount, Is.Zero);
        }

        [Test]
        public void StateSet_ApplyToCloneChangesProjectedStatesOnly()
        {
            var live = new UnitTraitStateSet(new TraitRuntimeState[]
            {
                new FocusRuntimeState(1, true, false),
                new RoarRuntimeState(1)
            });
            var liveFocus = live.GetRequired<FocusRuntimeState>(TraitKey.Focus);
            var liveRoar = live.GetRequired<RoarRuntimeState>(TraitKey.Roar);
            UnitTraitStateSet projected = live.Clone();
            var projectedFocus = projected.GetRequired<FocusRuntimeState>(TraitKey.Focus);
            var projectedRoar = projected.GetRequired<RoarRuntimeState>(TraitKey.Roar);

            Assert.That(projected, Is.Not.SameAs(live));
            Assert.That(projectedFocus, Is.Not.SameAs(liveFocus));
            Assert.That(projectedRoar, Is.Not.SameAs(liveRoar));

            projected.Apply(new TraitTransitionBatch(new[]
            {
                TraitStateChange.Change(TraitKey.Focus, new FocusRuntimeState(0, false, true)),
                TraitStateChange.Change(TraitKey.Roar, new RoarRuntimeState(0))
            }));

            Assert.That(projectedFocus.ActiveStacks, Is.Zero);
            Assert.That(projectedFocus.GainScheduled, Is.False);
            Assert.That(projectedFocus.MovedThisTurn, Is.True);
            Assert.That(projectedRoar.HitCount, Is.Zero);
            Assert.That(live.GetRequired<FocusRuntimeState>(TraitKey.Focus), Is.SameAs(liveFocus));
            Assert.That(live.GetRequired<RoarRuntimeState>(TraitKey.Roar), Is.SameAs(liveRoar));
            Assert.That(liveFocus.ActiveStacks, Is.EqualTo(1));
            Assert.That(liveFocus.GainScheduled, Is.True);
            Assert.That(liveFocus.MovedThisTurn, Is.False);
            Assert.That(liveRoar.HitCount, Is.EqualTo(1));
        }

        [Test]
        public void RuntimeState_CopyRejectsDifferentTraitType()
        {
            FocusRuntimeState focus = new FocusRuntimeState();

            Assert.Throws<ArgumentException>(
                () => focus.CopyFinalValuesFrom(new RoarRuntimeState()));
        }


        [Test]
        public void Focus_AttackDamage_UsesOnlyActiveStacks()
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState(
                activeStacks: 2,
                gainScheduled: true,
                movedThisTurn: false);

            AttackDamageContribution contribution = rule.EvaluateAttackDamage(
                new AttackDamageTraitContext(definition, state, baseDamage: 10));

            Assert.That(contribution.Source, Is.EqualTo(TraitKey.Focus));
            Assert.That(contribution.FlatBonus, Is.EqualTo(4));
        }

        [Test]
        public void Focus_MultipleSuccessfulAttacksInOneTurn_SchedulesOnlyOnce()
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState();

            TraitStateChange first = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,                    state));
            FocusRuntimeState scheduled = (FocusRuntimeState)first.FinalState;
            TraitStateChange second = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,                    scheduled));

            Assert.That(first.HasChange, Is.True);
            Assert.That(first.HasActivation, Is.True);
            Assert.That(first.ActivationKind, Is.EqualTo(TraitActivationKind.FocusGainScheduled));
            Assert.That(first.ActivationMagnitude, Is.EqualTo(1));
            Assert.That(scheduled.ActiveStacks, Is.Zero);
            Assert.That(scheduled.GainScheduled, Is.True);
            Assert.That(scheduled.MovedThisTurn, Is.False);
            Assert.That(state.GainScheduled, Is.False, "Rule must not mutate the input state.");
            Assert.That(second.HasChange, Is.False);
            Assert.That(second.HasActivation, Is.False);
        }

        [Test]
        public void Focus_TurnStarted_AppliesScheduledStackAndClearsTurnFlags()
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState(
                activeStacks: 1,
                gainScheduled: true,
                movedThisTurn: false);

            TraitStateChange change = rule.PlanTurnStarted(
                new TurnStartedTraitContext(definition, state));
            FocusRuntimeState finalState = (FocusRuntimeState)change.FinalState;

            Assert.That(change.HasChange, Is.True);
            Assert.That(change.HasActivation, Is.True);
            Assert.That(change.ActivationKind, Is.EqualTo(TraitActivationKind.FocusStackApplied));
            Assert.That(change.ActivationMagnitude, Is.EqualTo(1));
            Assert.That(finalState.ActiveStacks, Is.EqualTo(2));
            Assert.That(finalState.GainScheduled, Is.False);
            Assert.That(finalState.MovedThisTurn, Is.False);
        }

        [Test]
        public void Focus_TurnStarted_WithoutSchedule_ClearsMovedFlagWithoutActivation()
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState(
                activeStacks: 1,
                gainScheduled: false,
                movedThisTurn: true);

            TraitStateChange change = rule.PlanTurnStarted(
                new TurnStartedTraitContext(definition, state));
            FocusRuntimeState finalState = (FocusRuntimeState)change.FinalState;

            Assert.That(change.HasChange, Is.True);
            Assert.That(change.HasActivation, Is.False);
            Assert.That(finalState.ActiveStacks, Is.EqualTo(1));
            Assert.That(finalState.GainScheduled, Is.False);
            Assert.That(finalState.MovedThisTurn, Is.False);
        }

        [TestCase(true, false, 0)]
        [TestCase(false, true, 0)]
        [TestCase(false, false, 3)]
        public void Focus_AttackSucceeded_WhenIneligible_DoesNotSchedule(
            bool movedThisTurn,
            bool gainScheduled,
            int activeStacks)
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState(
                activeStacks,
                gainScheduled,
                movedThisTurn);

            TraitStateChange change = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,                    state));

            Assert.That(change.HasChange, Is.False);
            Assert.That(change.HasActivation, Is.False);
        }

        [Test]
        public void Focus_MoveSucceeded_ResetsActiveAndScheduledFocus()
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState(
                activeStacks: 2,
                gainScheduled: true,
                movedThisTurn: false);

            TraitStateChange change = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,                    state,                    moveDistance: 1,                    anchorChanged: true));
            FocusRuntimeState finalState = (FocusRuntimeState)change.FinalState;

            Assert.That(change.HasChange, Is.True);
            Assert.That(change.HasActivation, Is.True);
            Assert.That(change.ActivationKind, Is.EqualTo(TraitActivationKind.FocusReset));
            Assert.That(change.ActivationMagnitude, Is.EqualTo(2));
            Assert.That(finalState.ActiveStacks, Is.Zero);
            Assert.That(finalState.GainScheduled, Is.False);
            Assert.That(finalState.MovedThisTurn, Is.True);
        }

        [Test]
        public void Focus_FirstMoveWithoutStoredFocus_OnlyMarksMoved()
        {
            FocusTraitRule rule = new FocusTraitRule();
            FocusTraitDefinition definition = new FocusTraitDefinition(2, 3);
            FocusRuntimeState state = new FocusRuntimeState();

            TraitStateChange change = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,                    state,                    moveDistance: 1,                    anchorChanged: true));
            FocusRuntimeState finalState = (FocusRuntimeState)change.FinalState;

            Assert.That(change.HasChange, Is.True);
            Assert.That(change.HasActivation, Is.False);
            Assert.That(finalState.ActiveStacks, Is.Zero);
            Assert.That(finalState.GainScheduled, Is.False);
            Assert.That(finalState.MovedThisTurn, Is.True);
        }


        [Test]
        public void TurnEndedContext_RejectsMismatchedTraitKeys()
        {
            var definition = new FocusTraitDefinition(3, 1);
            var runtimeState = new RoarRuntimeState();

            Assert.Throws<ArgumentException>(() =>
                new TurnEndedTraitContext(definition, runtimeState));
        }

        [Test]
        public void Registry_RejectsRuntimeStateTypeThatDisagreesWithFactory()
        {
            var registration = new TraitRegistration(
                TraitKey.Focus,
                typeof(FocusTraitSpec),
                typeof(FocusTraitDefinition),
                typeof(RoarRuntimeState),
                new FocusTraitDefinitionFactory(),
                new FocusRuntimeStateFactory(),
                new PassiveTraitRule(TraitKey.Focus),
                _ => new PassiveEffectRule());

            Assert.Throws<ArgumentException>(() =>
                new TraitRegistry(new[] { registration }));
        }

        [Test]
        public void Registry_RejectsDefinitionFactoryFromDifferentTrait()
        {
            var registration = new TraitRegistration(
                TraitKey.Focus,
                typeof(FocusTraitSpec),
                typeof(FocusTraitDefinition),
                typeof(FocusRuntimeState),
                new RoarTraitDefinitionFactory(),
                new FocusRuntimeStateFactory(),
                new FocusTraitRule(),
                _ => new PassiveEffectRule());

            Assert.Throws<ArgumentException>(() =>
                new TraitRegistry(new[] { registration }));
        }

        [Test]
        public void Registry_RejectsRuntimeStateFactoryFromDifferentTrait()
        {
            var registration = new TraitRegistration(
                TraitKey.Focus,
                typeof(FocusTraitSpec),
                typeof(FocusTraitDefinition),
                typeof(FocusRuntimeState),
                new FocusTraitDefinitionFactory(),
                new RoarRuntimeStateFactory(),
                new FocusTraitRule(),
                _ => new PassiveEffectRule());

            Assert.Throws<ArgumentException>(() =>
                new TraitRegistry(new[] { registration }));
        }


        [Test]
        public void Provider_CreateStatuses_RejectsRegisteredStateWithoutSnapshotMapping()
        {
            var registration = new TraitRegistration(
                TraitKey.Focus, typeof(FocusTraitSpec), typeof(FocusTraitDefinition),
                typeof(UnsupportedFocusRuntimeState), new FocusTraitDefinitionFactory(),
                new UnsupportedFocusRuntimeStateFactory(), new PassiveTraitRule(TraitKey.Focus),
                _ => new PassiveEffectRule());
            var provider = new TraitEffectProvider(new TraitRegistry(new[] { registration }));
            var definition = CreateResolverDefinition(new TraitDefinition[] { new FocusTraitDefinition(3, 1) });
            var states = new UnitTraitStateSet(new[]
            {
                registration.TraitRuntimeStateFactory.BuildRuntimeState(definition.TraitDefinitions.Span[0]),
            });
            var error = Assert.Throws<InvalidOperationException>(() => provider.CreateStatuses(definition, states));
            Assert.That(error.Message, Does.Contain("StatusSnapshot"));
        }

        private static UnitDefinition CreateResolverDefinition(TraitDefinition[] traitDefinitions)
        {
            return UnitStateTestFactory.CreateDefinition(
                EntityKey.Unit_Akimbo,
                new TileFootprint(1, 1, new[] { CellCoord.Zero }),
                maxHealth: 10,
                MoveMethod.LiftAndPlace,
                4,
                attackPower: 3,
                priority: 30,
                traits: traitDefinitions);
        }

        private sealed class UnsupportedFocusRuntimeState : TraitRuntimeState
        {
            public UnsupportedFocusRuntimeState()
                : base(TraitKey.Focus)
            {
            }

            internal override TraitRuntimeState Clone()
            {
                return new UnsupportedFocusRuntimeState();
            }

            internal override void CopyFinalValuesFrom(TraitRuntimeState source)
            {
                if (source is not UnsupportedFocusRuntimeState)
                    throw new ArgumentException(nameof(source));
            }
        }

        private sealed class UnsupportedFocusRuntimeStateFactory : ITraitRuntimeStateFactory
        {
            public TraitKey TraitKey => TraitKey.Focus;
            public Type DefinitionType => typeof(FocusTraitDefinition);
            public Type RuntimeStateType => typeof(UnsupportedFocusRuntimeState);

            public TraitRuntimeState BuildRuntimeState(TraitDefinition definition)
            {
                if (definition is not FocusTraitDefinition)
                    throw new ArgumentException(nameof(definition));

                return new UnsupportedFocusRuntimeState();
            }
        }

        private sealed class PassiveTraitRule : ICombatTraitRule
        {
            public TraitKey Key { get; }

            public PassiveTraitRule(TraitKey key)
            {
                Key = key;
            }
        }

        private sealed class PassiveEffectRule : ICombatEffectRule
        {
        }
    }
}
