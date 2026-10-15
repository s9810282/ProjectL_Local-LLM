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

        [TestCase(0, 1)]
        [TestCase(-1, 1)]
        [TestCase(1, 0)]
        [TestCase(1, -1)]
        public void MoveAttackDiscountTraitDefinition_RejectsNonPositiveValues(
            int discountAmount,
            int minimumAttackCost)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new MoveAttackDiscountTraitDefinition(discountAmount, minimumAttackCost));
        }

        [Test]
        public void RuntimeStates_DefaultConstructors_CreateInitialState()
        {
            FocusRuntimeState focus = new FocusRuntimeState();
            MoveAttackDiscountRuntimeState discount = new MoveAttackDiscountRuntimeState();

            Assert.That(focus.TraitKey, Is.EqualTo(TraitKey.Focus));
            Assert.That(focus.ActiveStacks, Is.Zero);
            Assert.That(focus.GainScheduled, Is.False);
            Assert.That(focus.MovedThisTurn, Is.False);
            Assert.That(discount.TraitKey, Is.EqualTo(TraitKey.MoveAttackDiscount));
            Assert.That(discount.FirstMoveConsumed, Is.False);
            Assert.That(discount.DiscountArmed, Is.False);
        }

        [Test]
        public void FocusRuntimeState_RejectsNegativeActiveStacks()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new FocusRuntimeState(-1, gainScheduled: false, movedThisTurn: false));
        }

        [Test]
        public void MoveAttackDiscountRuntimeState_RejectsArmedWithoutConsumedMove()
        {
            Assert.Throws<ArgumentException>(
                () => new MoveAttackDiscountRuntimeState(
                    firstMoveConsumed: false,
                    discountArmed: true));
        }

        [Test]
        public void RuntimeState_CloneCopiesValuesWithoutSharingLaterAssignments()
        {
            FocusRuntimeState focus =
                new FocusRuntimeState(1, gainScheduled: true, movedThisTurn: false);
            MoveAttackDiscountRuntimeState discount =
                new MoveAttackDiscountRuntimeState(
                    firstMoveConsumed: true,
                    discountArmed: true);

            FocusRuntimeState focusClone = (FocusRuntimeState)focus.Clone();
            MoveAttackDiscountRuntimeState discountClone =
                (MoveAttackDiscountRuntimeState)discount.Clone();
            focus.CopyFinalValuesFrom(
                new FocusRuntimeState(2, gainScheduled: false, movedThisTurn: true));
            discount.CopyFinalValuesFrom(
                new MoveAttackDiscountRuntimeState(
                    firstMoveConsumed: true,
                    discountArmed: false));

            Assert.That(focusClone, Is.Not.SameAs(focus));
            Assert.That(focusClone.ActiveStacks, Is.EqualTo(1));
            Assert.That(focusClone.GainScheduled, Is.True);
            Assert.That(focusClone.MovedThisTurn, Is.False);
            Assert.That(focus.ActiveStacks, Is.EqualTo(2));
            Assert.That(focus.GainScheduled, Is.False);
            Assert.That(focus.MovedThisTurn, Is.True);
            Assert.That(discountClone, Is.Not.SameAs(discount));
            Assert.That(discountClone.FirstMoveConsumed, Is.True);
            Assert.That(discountClone.DiscountArmed, Is.True);
            Assert.That(discount.FirstMoveConsumed, Is.True);
            Assert.That(discount.DiscountArmed, Is.False);
        }

        [Test]
        public void StateSet_ApplyToCloneChangesProjectedStatesOnly()
        {
            var live = new UnitTraitStateSet(new TraitRuntimeState[]
            {
                new FocusRuntimeState(1, true, false),
                new MoveAttackDiscountRuntimeState(true, true)
            });
            var liveFocus = live.GetRequired<FocusRuntimeState>(TraitKey.Focus);
            var liveDiscount = live.GetRequired<MoveAttackDiscountRuntimeState>(TraitKey.MoveAttackDiscount);
            UnitTraitStateSet projected = live.Clone();
            var projectedFocus = projected.GetRequired<FocusRuntimeState>(TraitKey.Focus);
            var projectedDiscount = projected.GetRequired<MoveAttackDiscountRuntimeState>(TraitKey.MoveAttackDiscount);

            Assert.That(projected, Is.Not.SameAs(live));
            Assert.That(projectedFocus, Is.Not.SameAs(liveFocus));
            Assert.That(projectedDiscount, Is.Not.SameAs(liveDiscount));

            projected.Apply(new TraitTransitionBatch(new[]
            {
                TraitStateChange.Change(TraitKey.Focus, new FocusRuntimeState(0, false, true)),
                TraitStateChange.Change(TraitKey.MoveAttackDiscount, new MoveAttackDiscountRuntimeState(false, false))
            }));

            Assert.That(projectedFocus.ActiveStacks, Is.Zero);
            Assert.That(projectedFocus.GainScheduled, Is.False);
            Assert.That(projectedFocus.MovedThisTurn, Is.True);
            Assert.That(projectedDiscount.FirstMoveConsumed, Is.False);
            Assert.That(projectedDiscount.DiscountArmed, Is.False);
            Assert.That(live.GetRequired<FocusRuntimeState>(TraitKey.Focus), Is.SameAs(liveFocus));
            Assert.That(live.GetRequired<MoveAttackDiscountRuntimeState>(TraitKey.MoveAttackDiscount), Is.SameAs(liveDiscount));
            Assert.That(liveFocus.ActiveStacks, Is.EqualTo(1));
            Assert.That(liveFocus.GainScheduled, Is.True);
            Assert.That(liveFocus.MovedThisTurn, Is.False);
            Assert.That(liveDiscount.FirstMoveConsumed, Is.True);
            Assert.That(liveDiscount.DiscountArmed, Is.True);
        }

        [Test]
        public void RuntimeState_CopyRejectsDifferentTraitType()
        {
            FocusRuntimeState focus = new FocusRuntimeState();

            Assert.Throws<ArgumentException>(
                () => focus.CopyFinalValuesFrom(new MoveAttackDiscountRuntimeState()));
        }

        [Test]
        public void AttackCostContribution_ConstructorAssignsValues()
        {
            AttackCostContribution contribution = new AttackCostContribution(
                TraitKey.MoveAttackDiscount,
                flatReduction: 2,
                minimumCost: 1,
                consumeOnSuccessfulAttack: true);

            Assert.That(contribution.Source, Is.EqualTo(TraitKey.MoveAttackDiscount));
            Assert.That(contribution.FlatReduction, Is.EqualTo(2));
            Assert.That(contribution.MinimumCost, Is.EqualTo(1));
            Assert.That(contribution.ConsumeOnSuccessfulAttack, Is.True);
        }

        [TestCase(TraitKey.None)]
        [TestCase((TraitKey)999)]
        public void AttackCostContribution_RejectsInvalidSource(TraitKey source)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AttackCostContribution(source, 0, 1, false));
        }

        [TestCase(-1, 1)]
        [TestCase(0, 0)]
        [TestCase(0, -1)]
        public void AttackCostContribution_RejectsInvalidCosts(
            int flatReduction,
            int minimumCost)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new AttackCostContribution(
                    TraitKey.MoveAttackDiscount,
                    flatReduction,
                    minimumCost,
                    false));
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
                    definition,
                    state,
                    Array.Empty<TraitKey>()));
            FocusRuntimeState scheduled = (FocusRuntimeState)first.FinalState;
            TraitStateChange second = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,
                    scheduled,
                    Array.Empty<TraitKey>()));

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
                    definition,
                    state,
                    Array.Empty<TraitKey>()));

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
                    definition,
                    state,
                    moveDistance: 1,
                    anchorChanged: true,
                    actionCostSpent: 1));
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
                    definition,
                    state,
                    moveDistance: 1,
                    anchorChanged: true,
                    actionCostSpent: 1));
            FocusRuntimeState finalState = (FocusRuntimeState)change.FinalState;

            Assert.That(change.HasChange, Is.True);
            Assert.That(change.HasActivation, Is.False);
            Assert.That(finalState.ActiveStacks, Is.Zero);
            Assert.That(finalState.GainScheduled, Is.False);
            Assert.That(finalState.MovedThisTurn, Is.True);
        }

        [Test]
        public void MoveAttackDiscount_FirstQualifyingMove_ArmsOnlyOnce()
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);
            MoveAttackDiscountRuntimeState state = new MoveAttackDiscountRuntimeState();

            TraitStateChange first = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,
                    state,
                    moveDistance: 1,
                    anchorChanged: true,
                    actionCostSpent: 1));
            MoveAttackDiscountRuntimeState armed =
                (MoveAttackDiscountRuntimeState)first.FinalState;
            TraitStateChange second = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,
                    armed,
                    moveDistance: 1,
                    anchorChanged: true,
                    actionCostSpent: 1));

            Assert.That(first.HasChange, Is.True);
            Assert.That(first.HasActivation, Is.True);
            Assert.That(first.ActivationKind, Is.EqualTo(TraitActivationKind.AttackDiscountArmed));
            Assert.That(first.ActivationMagnitude, Is.EqualTo(2));
            Assert.That(armed.FirstMoveConsumed, Is.True);
            Assert.That(armed.DiscountArmed, Is.True);
            Assert.That(second.HasChange, Is.False);
            Assert.That(second.HasActivation, Is.False);
        }

        [TestCase(false, 1)]
        [TestCase(true, 0)]
        public void MoveAttackDiscount_NonQualifyingMove_DoesNotArm(
            bool anchorChanged,
            int actionCostSpent)
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);
            MoveAttackDiscountRuntimeState state = new MoveAttackDiscountRuntimeState();

            TraitStateChange change = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,
                    state,
                    moveDistance: anchorChanged ? 1 : 0,
                    anchorChanged,
                    actionCostSpent));

            Assert.That(change.HasChange, Is.False);
            Assert.That(change.HasActivation, Is.False);
        }

        [Test]
        public void MoveAttackDiscount_AttackCostEvaluation_IsPreviewSafeAndRespectsMinimum()
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);
            MoveAttackDiscountRuntimeState armed =
                new MoveAttackDiscountRuntimeState(true, true);

            AttackCostContribution positive = rule.EvaluateAttackCost(
                new AttackCostTraitContext(definition, armed, baseAttackCost: 4));
            AttackCostContribution zero = rule.EvaluateAttackCost(
                new AttackCostTraitContext(definition, armed, baseAttackCost: 1));

            Assert.That(positive.FlatReduction, Is.EqualTo(2));
            Assert.That(positive.MinimumCost, Is.EqualTo(1));
            Assert.That(positive.ConsumeOnSuccessfulAttack, Is.True);
            Assert.That(zero.FlatReduction, Is.Zero);
            Assert.That(zero.ConsumeOnSuccessfulAttack, Is.False);
            Assert.That(armed.DiscountArmed, Is.True, "Preview evaluation must not consume the discount.");
        }

        [Test]
        public void MoveAttackDiscount_AttackSucceeded_ConsumesOnlyWhenItContributed()
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);
            MoveAttackDiscountRuntimeState armed =
                new MoveAttackDiscountRuntimeState(true, true);

            TraitStateChange withoutContribution = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,
                    armed,
                    Array.Empty<TraitKey>()));
            TraitStateChange withUnrelatedContribution = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,
                    armed,
                    new[] { TraitKey.Focus }));
            TraitStateChange withContribution = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,
                    armed,
                    new[] { TraitKey.MoveAttackDiscount }));
            MoveAttackDiscountRuntimeState consumed =
                (MoveAttackDiscountRuntimeState)withContribution.FinalState;

            Assert.That(withoutContribution.HasChange, Is.False);
            Assert.That(withoutContribution.HasActivation, Is.False);
            Assert.That(withUnrelatedContribution.HasChange, Is.False);
            Assert.That(withUnrelatedContribution.HasActivation, Is.False);
            Assert.That(withContribution.HasChange, Is.True);
            Assert.That(withContribution.HasActivation, Is.True);
            Assert.That(withContribution.ActivationKind, Is.EqualTo(TraitActivationKind.AttackCostReduced));
            Assert.That(withContribution.ActivationMagnitude, Is.EqualTo(2));
            Assert.That(consumed.FirstMoveConsumed, Is.True);
            Assert.That(consumed.DiscountArmed, Is.False);
            Assert.That(armed.DiscountArmed, Is.True, "Planning must not mutate the input state.");
        }

        [Test]
        public void MoveAttackDiscount_AfterConsumption_SecondMoveDoesNotRearm()
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);

            TraitStateChange armedChange = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,
                    new MoveAttackDiscountRuntimeState(),
                    moveDistance: 1,
                    anchorChanged: true,
                    actionCostSpent: 1));
            TraitStateChange consumedChange = rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(
                    definition,
                    armedChange.FinalState,
                    new[] { TraitKey.MoveAttackDiscount }));
            TraitStateChange secondMove = rule.PlanMoveSucceeded(
                new MoveSucceededTraitContext(
                    definition,
                    consumedChange.FinalState,
                    moveDistance: 1,
                    anchorChanged: true,
                    actionCostSpent: 1));

            Assert.That(secondMove.HasChange, Is.False);
            Assert.That(secondMove.HasActivation, Is.False);
        }

        [Test]
        public void MoveAttackDiscount_TurnEnded_ExpiresOnlyUnusedArmedDiscount()
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);
            MoveAttackDiscountRuntimeState armed =
                new MoveAttackDiscountRuntimeState(true, true);
            MoveAttackDiscountRuntimeState consumed =
                new MoveAttackDiscountRuntimeState(true, false);

            TraitStateChange expired = rule.PlanTurnEnded(
                new TurnEndedTraitContext(definition, armed));
            TraitStateChange alreadyConsumed = rule.PlanTurnEnded(
                new TurnEndedTraitContext(definition, consumed));
            MoveAttackDiscountRuntimeState finalState =
                (MoveAttackDiscountRuntimeState)expired.FinalState;

            Assert.That(expired.HasChange, Is.True);
            Assert.That(expired.HasActivation, Is.True);
            Assert.That(expired.ActivationKind, Is.EqualTo(TraitActivationKind.AttackDiscountExpired));
            Assert.That(expired.ActivationMagnitude, Is.EqualTo(2));
            Assert.That(finalState.FirstMoveConsumed, Is.True);
            Assert.That(finalState.DiscountArmed, Is.False);
            Assert.That(alreadyConsumed.HasChange, Is.False);
            Assert.That(alreadyConsumed.HasActivation, Is.False);
        }

        [Test]
        public void TurnEndedContext_RejectsMismatchedTraitKeys()
        {
            var definition = new FocusTraitDefinition(3, 1);
            var runtimeState = new MoveAttackDiscountRuntimeState();

            Assert.Throws<ArgumentException>(() =>
                new TurnEndedTraitContext(definition, runtimeState));
        }

        [Test]
        public void MoveAttackDiscount_TurnStarted_ResetsFlagsWithoutActivation()
        {
            MoveAttackDiscountTraitRule rule = new MoveAttackDiscountTraitRule();
            MoveAttackDiscountTraitDefinition definition =
                new MoveAttackDiscountTraitDefinition(2, 1);
            MoveAttackDiscountRuntimeState state =
                new MoveAttackDiscountRuntimeState(true, true);

            TraitStateChange change = rule.PlanTurnStarted(
                new TurnStartedTraitContext(definition, state));
            MoveAttackDiscountRuntimeState finalState =
                (MoveAttackDiscountRuntimeState)change.FinalState;

            Assert.That(change.HasChange, Is.True);
            Assert.That(change.HasActivation, Is.False);
            Assert.That(finalState.FirstMoveConsumed, Is.False);
            Assert.That(finalState.DiscountArmed, Is.False);
        }

        [Test]
        public void Registry_RejectsRuntimeStateTypeThatDisagreesWithFactory()
        {
            var registration = new TraitRegistration(
                TraitKey.Focus,
                typeof(FocusTraitSpec),
                typeof(FocusTraitDefinition),
                typeof(MoveAttackDiscountRuntimeState),
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
                new MoveAttackDiscountTraitDefinitionFactory(),
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
                new MoveAttackDiscountRuntimeStateFactory(),
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
                moveRange: 5,
                moveCost: 1,
                4,
                attackPower: 3,
                maxActionCost: 8,
                attackCost: 5,
                priority: 30,
                delay: 35,
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
