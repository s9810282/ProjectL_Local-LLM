using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects
{
    internal sealed class CombatEffectEngine : ICombatEffectEngine
    {
        public EffectAttackSweepEvaluation EvaluateAttackSweep(IReadOnlyList<EffectRuleBinding> bindings, in AttackSweepEffectContext context)
        {
            if (!Enum.IsDefined(typeof(CombatDamageKind), context.DamageKind))
                throw new ArgumentOutOfRangeException(nameof(context));
            EffectAttackSweepContribution? active = null;
            var expectations = new List<EffectStateExpectation>();
            foreach (var binding in CopyValidateAndSortBindings(bindings))
            {
                if (binding.Rule is not IEffectAttackSweepRule rule) continue;
                var contribution = rule.EvaluateAttackSweep(new EffectRuleInput(binding.Origin, binding.ExpectedState), context);
                if (!contribution.HasValue) continue;
                if (active.HasValue || contribution.Value.Origin != binding.Origin || contribution.Value.DamagePercent < 0)
                    throw new InvalidOperationException("공격 쓸기는 올바른 출처의 단일 기여만 허용합니다.");
                active = contribution;
                expectations.Add(new EffectStateExpectation(binding.Origin, binding.ExpectedState));
            }
            return new EffectAttackSweepEvaluation(active, expectations.ToArray());
        }

        public bool RequiresOrderedHits(IReadOnlyList<EffectRuleBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            foreach (var binding in bindings)
                if (binding.Rule is IEffectOrderedHitRule { RequiresOrderedHits: true }) return true;
            return false;
        }

        public EffectAttackTargetingEvaluation EvaluateAttackTargeting(IReadOnlyList<EffectRuleBinding> bindings)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var contributions = new List<EffectAttackTargetingContribution>();
            var expectations = new List<EffectStateExpectation>();
            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectAttackTargetingRule rule) continue;
                var contribution = rule.EvaluateAttackTargeting(
                    new EffectRuleInput(binding.Origin, binding.ExpectedState));
                if (contribution.Origin != binding.Origin)
                    throw new InvalidOperationException("Invalid targeting contribution origin.");
                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(binding.Origin, binding.ExpectedState));
            }
            return new EffectAttackTargetingEvaluation(contributions.ToArray(), expectations.ToArray());
        }


        public EffectAttackDamageEvaluation EvaluateAttackDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in AttackDamageEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            if (context.BaseDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(context));

            int bonus = 0;
            var contributions = new List<EffectAttackDamageContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectAttackDamageRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectAttackDamageContribution contribution = rule.EvaluateAttackDamage(input, context);
                if (contribution.Origin != binding.Origin || contribution.FlatBonus < 0)
                    throw new InvalidOperationException("Invalid damage contribution");

                bonus = checked(bonus + contribution.FlatBonus);
                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(binding.Origin, binding.ExpectedState));
            }

            int effectiveDamage = Math.Max(0, checked(context.BaseDamage + bonus));
            return new EffectAttackDamageEvaluation(
                effectiveDamage,
                contributions.ToArray(),
                expectations.ToArray());
        }

        public EffectPierceFalloffEvaluation EvaluatePierceFalloff(
            IReadOnlyList<EffectRuleBinding> bindings,
            in PierceFalloffEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            if (context.DamageAfterBonuses < 0 || context.EncounterIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(context));

            int remainingPercent = 100;
            int? targetBudget = null;
            var contributions = new List<EffectPierceFalloffContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectPierceFalloffRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectPierceFalloffContribution contribution =
                    rule.EvaluatePierceFalloff(input, context);
                if (contribution.Origin != binding.Origin ||
                    contribution.RemainingPercent < 0 ||
                    contribution.RemainingPercent > 100 ||
                    contribution.TargetBudget < 0)
                {
                    throw new InvalidOperationException(
                        "Invalid Pierce falloff contribution.");
                }

                remainingPercent = Math.Min(
                    remainingPercent,
                    contribution.RemainingPercent);
                targetBudget = targetBudget.HasValue
                    ? Math.Min(targetBudget.Value, contribution.TargetBudget)
                    : contribution.TargetBudget;
                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(
                    binding.Origin,
                    binding.ExpectedState));
            }

            return new EffectPierceFalloffEvaluation(
                remainingPercent,
                targetBudget,
                contributions.ToArray(),
                expectations.ToArray());
        }

        public EffectTargetDistanceDamageEvaluation EvaluateTargetDistanceDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in TargetDistanceDamageEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            if (context.Distance < 1)
                throw new ArgumentOutOfRangeException(nameof(context));

            var contributions = new List<EffectTargetDistanceDamageContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectTargetDistanceDamageRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectTargetDistanceDamageContribution contribution =
                    rule.EvaluateTargetDistanceDamage(input, context);
                if (contribution.Origin != binding.Origin || contribution.Percent < 0)
                {
                    throw new InvalidOperationException(
                        "Invalid target distance damage contribution.");
                }

                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(
                    binding.Origin,
                    binding.ExpectedState));
            }

            return new EffectTargetDistanceDamageEvaluation(
                contributions.ToArray(),
                expectations.ToArray());
        }

        public EffectTargetDamageEvaluation EvaluateTargetDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in TargetDamageEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            if (context.Distance < 0 || context.ActorAttack < 0 || context.RecipientAttack < 0 || context.ActualRecipientIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(context));

            var contributions = new List<EffectTargetDamageContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectTargetDamageRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectTargetDamageContribution contribution =
                    rule.EvaluateTargetDamage(input, context);
                if (contribution.Origin != binding.Origin || contribution.Numerator < 1 || contribution.Denominator < 1)
                {
                    throw new InvalidOperationException(
                        "Invalid target distance damage contribution.");
                }

                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(
                    binding.Origin,
                    binding.ExpectedState));
            }

            return new EffectTargetDamageEvaluation(
                contributions.ToArray(),
                expectations.ToArray());
        }

        public EffectKnockbackEvaluation EvaluateKnockback(
            IReadOnlyList<EffectRuleBinding> bindings)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var contributions = new List<EffectKnockbackContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectKnockbackRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectKnockbackContribution contribution = rule.EvaluateKnockback(input);
                if (contribution.Origin != binding.Origin ||
                    contribution.DistanceCells < 1 ||
                    !Enum.IsDefined(typeof(EffectKnockbackMode), contribution.Mode))
                {
                    throw new InvalidOperationException(
                        "Invalid Knockback contribution.");
                }

                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(
                    binding.Origin,
                    binding.ExpectedState));
            }

            return new EffectKnockbackEvaluation(
                contributions.ToArray(),
                expectations.ToArray());
        }

        public EffectKillChainAttackEvaluation EvaluateKillChainAttack(
            IReadOnlyList<EffectRuleBinding> bindings)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var contributions = new List<EffectKillChainAttackContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectKillChainAttackRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectKillChainAttackContribution contribution = rule.EvaluateKillChainAttack(input);
                if (contribution.Origin != binding.Origin)
                {
                    throw new InvalidOperationException(
                        "Invalid kill chain attack contribution.");
                }

                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(
                    binding.Origin,
                    binding.ExpectedState));
            }

            return new EffectKillChainAttackEvaluation(
                contributions.ToArray(),
                expectations.ToArray());
        }

        public EffectDeathTraitEvaluation EvaluateDeathTraits(IReadOnlyList<EffectRuleBinding> bindings)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var contributions = new List<EffectDeathTraitContribution>();
            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectDeathTraitRule rule) continue;
                var contribution = rule.EvaluateDeath(new EffectRuleInput(binding.Origin, binding.ExpectedState));
                if (contribution.Origin != binding.Origin)
                    throw new InvalidOperationException("사망 특성 기여의 원본이 바인딩과 다릅니다.");
                contributions.Add(contribution);
            }
            return new EffectDeathTraitEvaluation(contributions.ToArray());
        }

        /// <summary>
        /// 경감 기여를 EffectOriginKey 순으로 모아 피해량을 넘지 않게 배분합니다.
        /// </summary>
        public EffectIncomingDamageEvaluation EvaluateIncomingDamage(
            IReadOnlyList<EffectRuleBinding> bindings,
            in IncomingDamageEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);

            int remaining = context.IncomingDamage;
            int allocatedTotal = 0;
            var allocations = new List<EffectIncomingDamageContribution>();
            var expectations = new List<EffectStateExpectation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectIncomingDamageRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectIncomingDamageContribution contribution = rule.EvaluateIncomingDamage(input, context);
                if (contribution.Origin != binding.Origin || contribution.FlatReduction < 0)
                    throw new InvalidOperationException("Invalid incoming damage contribution");

                int granted = Math.Min(contribution.FlatReduction, remaining);
                remaining = checked(remaining - granted);
                allocatedTotal = checked(allocatedTotal + granted);

                allocations.Add(new EffectIncomingDamageContribution(binding.Origin, granted, contribution.RemainingDamagePercent));
                expectations.Add(new EffectStateExpectation(binding.Origin, binding.ExpectedState));
            }

            return new EffectIncomingDamageEvaluation(
                context.IncomingDamage,
                allocatedTotal,
                allocations.ToArray(),
                expectations.ToArray());
        }

        public CombatEffectPlan PlanMoveSucceeded(
            IReadOnlyList<EffectRuleBinding> bindings,
            in MoveSucceededEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            if (context.Distance < 0) throw new ArgumentOutOfRangeException(nameof(context));

            var expectations = new List<EffectStateExpectation>();
            var changes = new List<EffectStateChange>();
            var activations = new List<EffectActivation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectMoveSucceededRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectRuleResult result = rule.PlanMoveSucceeded(input, context);
                AppendValidatedResult(binding, result, expectations, changes, activations);
            }

            return CreatePlan(expectations, changes, activations);
        }

        public CombatEffectPlan PlanAttackSucceeded(
            IReadOnlyList<EffectRuleBinding> bindings,
            in AttackSucceededEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            ValidateAttackSucceededContext(context);

            var expectations = new List<EffectStateExpectation>();
            var changes = new List<EffectStateChange>();
            var activations = new List<EffectActivation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectAttackSucceededRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectRuleResult result = rule.PlanAttackSucceeded(input, context);
                AppendValidatedResult(binding, result, expectations, changes, activations);
            }

            return CreatePlan(expectations, changes, activations);
        }

        public CombatEffectPlan PlanTurnStarted(IReadOnlyList<EffectRuleBinding> bindings)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var expectations = new List<EffectStateExpectation>();
            var changes = new List<EffectStateChange>();
            var activations = new List<EffectActivation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectTurnStartedRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectRuleResult result = rule.PlanTurnStarted(input);
                AppendValidatedResult(binding, result, expectations, changes, activations);
            }

            return CreatePlan(expectations, changes, activations);
        }

        public CombatEffectPlan PlanTurnEnded(IReadOnlyList<EffectRuleBinding> bindings)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var expectations = new List<EffectStateExpectation>();
            var changes = new List<EffectStateChange>();
            var activations = new List<EffectActivation>();

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectTurnEndedRule rule) continue;

                var input = new EffectRuleInput(binding.Origin, binding.ExpectedState);
                EffectRuleResult result = rule.PlanTurnEnded(input);
                AppendValidatedResult(binding, result, expectations, changes, activations);
            }

            return CreatePlan(expectations, changes, activations);
        }

        public EffectStageClearedEvaluation EvaluateStageCleared(
            IReadOnlyList<EffectRuleBinding> bindings,
            in StageClearedEffectContext context)
        {
            EffectRuleBinding[] ordered = CopyValidateAndSortBindings(bindings);
            var contributions = new List<EffectHealContribution>();
            var expectations = new List<EffectStateExpectation>();
            long requestedHealing = 0;

            foreach (EffectRuleBinding binding in ordered)
            {
                if (binding.Rule is not IEffectStageClearedRule rule) continue;

                EffectHealContribution contribution = rule.EvaluateStageCleared(
                    new EffectRuleInput(binding.Origin, binding.ExpectedState),
                    context);
                if (contribution.Origin != binding.Origin ||
                    contribution.TargetId != context.MainCharacterId ||
                    contribution.RequestedAmount <= 0)
                {
                    throw new InvalidOperationException("Invalid StageCleared healing contribution.");
                }

                requestedHealing = checked(requestedHealing + contribution.RequestedAmount);
                contributions.Add(contribution);
                expectations.Add(new EffectStateExpectation(binding.Origin, binding.ExpectedState));
            }

            long uncapped = checked((long)context.ProjectedCurrentHealth + requestedHealing);
            int finalHealth = (int)Math.Min(context.MaxHealth, uncapped);
            return new EffectStageClearedEvaluation(
                context.MainCharacterId,
                context.ProjectedCurrentHealth,
                finalHealth,
                requestedHealing,
                contributions.ToArray(),
                expectations.ToArray());
        }

        private static EffectRuleBinding[] CopyValidateAndSortBindings(
            IReadOnlyList<EffectRuleBinding> bindings)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));

            var copied = new EffectRuleBinding[bindings.Count];
            var seen = new HashSet<EffectOriginKey>();
            for (int index = 0; index < copied.Length; index++)
            {
                EffectRuleBinding binding = bindings[index];
                if (binding == null) throw new ArgumentException("Null binding", nameof(bindings));
                if (binding.Origin.IsValid == false)
                    throw new ArgumentException("Invalid binding origin", nameof(bindings));
                if (binding.Rule == null) throw new ArgumentException("Null binding rule", nameof(bindings));
                if (binding.ExpectedState == null)
                    throw new ArgumentException("Null binding state", nameof(bindings));
                if (seen.Add(binding.Origin) == false)
                    throw new ArgumentException("Duplicate binding origin", nameof(bindings));

                copied[index] = binding;
            }

            Array.Sort(copied, (left, right) => left.Origin.CompareTo(right.Origin));
            return copied;
        }

        private static void ValidateAttackSucceededContext(
            in AttackSucceededEffectContext context)
        {
            if (!Enum.IsDefined(typeof(Rotation4), context.AttackDirection))
                throw new ArgumentException("공격 성공 방향이 잘못되었습니다.", nameof(context));
        }

        private static void AppendValidatedResult(
            EffectRuleBinding binding,
            EffectRuleResult result,
            ICollection<EffectStateExpectation> expectations,
            ICollection<EffectStateChange> changes,
            ICollection<EffectActivation> activations)
        {
            if (result == null) throw new InvalidOperationException("Null rule result");

            EffectStateChange? optionalChange = result.Change;
            if (optionalChange is EffectStateChange change)
                ValidateChange(binding, change);

            foreach (EffectActivation activation in result.Activations)
                ValidateActivation(binding, activation);

            expectations.Add(new EffectStateExpectation(binding.Origin, binding.ExpectedState));
            if (optionalChange is EffectStateChange validatedChange)
                changes.Add(validatedChange);
            foreach (EffectActivation activation in result.Activations)
                activations.Add(activation);
        }

        private static void ValidateChange(
            EffectRuleBinding binding,
            in EffectStateChange change)
        {
            if (change.Origin != binding.Origin ||
                change.ExpectedState == null ||
                change.FinalState == null ||
                change.ExpectedState.GetType() != binding.ExpectedState.GetType() ||
                binding.ExpectedState.ValueEquals(change.ExpectedState) == false ||
                change.FinalState.GetType() != binding.ExpectedState.GetType())
            {
                throw new InvalidOperationException("Invalid state change");
            }
        }

        private static void ValidateActivation(
            EffectRuleBinding binding,
            in EffectActivation activation)
        {
            if (activation.Origin != binding.Origin ||
                activation.Code <= 0 ||
                activation.Magnitude < 0)
            {
                throw new InvalidOperationException("Invalid activation");
            }
        }

        private static CombatEffectPlan CreatePlan(
            List<EffectStateExpectation> expectations,
            List<EffectStateChange> changes,
            List<EffectActivation> activations)
        {
            return new CombatEffectPlan(
                expectations.ToArray(),
                changes.ToArray(),
                activations.ToArray());
        }
    }
}
