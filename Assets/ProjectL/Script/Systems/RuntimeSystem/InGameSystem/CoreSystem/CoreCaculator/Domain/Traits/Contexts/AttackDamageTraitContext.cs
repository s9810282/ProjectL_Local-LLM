using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts
{
    /// <summary>
    /// 공격 피해를 변경하는 특성에게 전달
    /// </summary>
    internal readonly struct AttackDamageTraitContext
    {
        /// <summary>
        /// 이번 피해 계산에 참여하는 Trait의 변경되지 않는 설정입니다.
        /// </summary>
        public TraitDefinition TraitDefinition { get; }

        /// <summary>
        /// 피해 계산 시점에 해당 Unit이 가진 현재 Trait 상태의 복사본입니다.
        /// </summary>
        public TraitRuntimeState TraitRuntimeState { get; }

        /// <summary>
        /// Trait 피해 보너스를 더하기 전, 이번 대상에게 적용될 기본 피해입니다.
        /// </summary>
        public int BaseDamage { get; }

        public AttackDamageTraitContext(
            TraitDefinition traitDefinition,
            TraitRuntimeState traitRuntimeState,
            int baseDamage)
        {
            if (traitDefinition == null)
                throw new ArgumentNullException(nameof(traitDefinition));
            if (traitRuntimeState == null)
                throw new ArgumentNullException(nameof(traitRuntimeState));
            if (traitDefinition.TraitKey != traitRuntimeState.TraitKey)
                throw new ArgumentException(
                    "Trait definition and runtime state keys must match.",
                    nameof(traitRuntimeState));
            if (baseDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(baseDamage));

            TraitDefinition = traitDefinition;
            TraitRuntimeState = traitRuntimeState.Clone();
            BaseDamage = baseDamage;
        }
    }
}
