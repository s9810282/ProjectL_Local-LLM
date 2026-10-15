using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts
{
    /// <summary>
    /// 공격 비용을 변경하는 특성에게 전달하는 정보
    /// </summary>
    internal readonly struct AttackCostTraitContext
    {
        /// <summary>
        /// 이번 비용 계산에 참여하는 Trait의 변경되지 않는 설정입니다.
        /// </summary>
        public TraitDefinition TraitDefinition { get; }

        /// <summary>
        /// 비용 계산 시점에 해당 Unit이 가진 현재 Trait 상태의 복사본입니다.
        /// </summary>
        public TraitRuntimeState TraitRuntimeState { get; }

        /// <summary>
        /// Trait의 비용 감소 Contribution을 적용하기 전 공격의 기본 행동 비용입니다.
        /// </summary>
        public int BaseAttackCost { get; }

        public AttackCostTraitContext(
            TraitDefinition traitDefinition,
            TraitRuntimeState traitRuntimeState,
            int baseAttackCost)
        {
            if (traitDefinition == null)
                throw new ArgumentNullException(nameof(traitDefinition));
            if (traitRuntimeState == null)
                throw new ArgumentNullException(nameof(traitRuntimeState));
            if (traitDefinition.TraitKey != traitRuntimeState.TraitKey)
                throw new ArgumentException(
                    "Trait definition and runtime state keys must match.",
                    nameof(traitRuntimeState));
            if (baseAttackCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(baseAttackCost));

            TraitDefinition = traitDefinition;
            TraitRuntimeState = traitRuntimeState.Clone();
            BaseAttackCost = baseAttackCost;
        }
    }
}
