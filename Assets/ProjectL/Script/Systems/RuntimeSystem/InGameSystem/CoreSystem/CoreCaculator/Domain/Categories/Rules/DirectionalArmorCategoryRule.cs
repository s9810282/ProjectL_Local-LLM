using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules
{
    /// <summary>
    /// Heavy의 전방 피해 경감 규칙입니다.
    /// 경감 평가는 읽기 전용이며 Facing 전이는 이동/공격 성공 정산에서 따로 계획합니다.
    /// </summary>
    internal sealed class DirectionalArmorCategoryRule :
        ICombatEffectRule,
        IEffectIncomingDamageRule,
        IEffectMoveSucceededRule,
        IEffectAttackSucceededRule,
        IEffectTurnStartedRule
    {
        private readonly DirectionalArmorCategoryRuleDefinition _definition;

        internal DirectionalArmorCategoryRule(DirectionalArmorCategoryRuleDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>
        /// 보호 영역에서 들어온 피해만 경감합니다. 경감량은 floor입니다.
        /// </summary>
        public EffectIncomingDamageContribution EvaluateIncomingDamage(
            in EffectRuleInput input,
            in IncomingDamageEffectContext context)
        {
            RequireState(input);

            bool isProtected;
            if (context.RayTravelDirection.HasValue)
            {
                // A cardinal ray enters the front when it travels opposite the defense facing.
                // This remains valid at the first hit cell and for multi-cell targets.
                isProtected = ((int)context.RayTravelDirection.Value + 2) % 4 == (int)context.DefenseFacing;
            }
            else
            {
                if (!context.ImpactOrigin.HasValue)
                    return new EffectIncomingDamageContribution(input.Origin, 0);
                isProtected = DirectionalArmorCoverageCalculator.IsProtected(
                    context.DefenderAnchor, context.DefenseFacing, context.ImpactOrigin.Value);
            }
            if (isProtected == false)
                return new EffectIncomingDamageContribution(input.Origin, 0);

            long reduction = ((long)context.IncomingDamage * _definition.DamageReductionPercent) / 100L;
            if (reduction > context.IncomingDamage)
                reduction = context.IncomingDamage;

            return new EffectIncomingDamageContribution(input.Origin, (int)reduction);
        }

        /// <summary>
        /// [사용자 확정] 이동 방향이 방어 Facing보다 우선합니다.
        /// </summary>
        public EffectRuleResult PlanMoveSucceeded(
            in EffectRuleInput input,
            in MoveSucceededEffectContext context)
        {
            DirectionalArmorEffectStateSnapshot state = RequireState(input);

            if (context.AnchorChanged == false)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            var finalState = new DirectionalArmorEffectStateSnapshot(context.MoveDirection, true);
            if (finalState.ValueEquals(state))
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            return new EffectRuleResult(
                new EffectStateChange(input.Origin, state, finalState),
                new[]
                {
                    new EffectActivation(
                        input.Origin,
                        (int)CategoryActivationKind.DefenseFacingChanged,
                        0,
                        global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId.Empty),
                });
        }

        /// <summary>
        /// [사용자 확정] 이번 턴에 이동하지 않은 경우에만 공격 방향으로 Facing을 갱신합니다.
        /// 공격 방향은 사용자가 선택한 조준 Cell 방향과 같은 개념입니다.
        /// </summary>
        public EffectRuleResult PlanAttackFacingUpdate(in EffectRuleInput input, Rotation4 attackDirection)
        {
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection));

            DirectionalArmorEffectStateSnapshot state = RequireState(input);

            // 이동한 턴의 공격은 이동 Facing을 보존한다.
            if (state.MovedThisTurn)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());
            if (state.DefenseFacing == attackDirection)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            return new EffectRuleResult(
                new EffectStateChange(
                    input.Origin,
                    state,
                    new DirectionalArmorEffectStateSnapshot(attackDirection, state.MovedThisTurn)),
                new[]
                {
                    new EffectActivation(
                        input.Origin,
                        (int)CategoryActivationKind.DefenseFacingChanged,
                        0,
                        global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId.Empty),
                });
        }

        public EffectRuleResult PlanAttackSucceeded(
            in EffectRuleInput input,
            in AttackSucceededEffectContext context)
        {
            if (context.HasAttackDirection == false)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());
            return PlanAttackFacingUpdate(input, context.AttackDirection);
        }

        /// <summary>턴 시작은 Facing을 유지하고 이동 여부만 초기화합니다.</summary>
        public EffectRuleResult PlanTurnStarted(in EffectRuleInput input)
        {
            DirectionalArmorEffectStateSnapshot state = RequireState(input);
            if (state.MovedThisTurn == false)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            return new EffectRuleResult(
                new EffectStateChange(
                    input.Origin,
                    state,
                    new DirectionalArmorEffectStateSnapshot(state.DefenseFacing, false)),
                Array.Empty<EffectActivation>());
        }

        private static DirectionalArmorEffectStateSnapshot RequireState(in EffectRuleInput input)
        {
            if (input.State is DirectionalArmorEffectStateSnapshot snapshot)
                return snapshot;

            throw new InvalidOperationException(
                "DirectionalArmor 규칙에는 DirectionalArmorEffectStateSnapshot이 필요합니다.");
        }
    }
}
