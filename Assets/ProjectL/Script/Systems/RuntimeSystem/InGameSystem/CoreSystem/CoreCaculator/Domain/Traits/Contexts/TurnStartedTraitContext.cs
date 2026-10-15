using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts
{
    /// <summary>
    /// 자기 턴이 시작될 때 특성이 상태를 갱신하기 위한 입력
    /// </summary>
    internal readonly struct TurnStartedTraitContext
    {
        /// <summary>
        /// 자기 턴 시작에 반응할 현재 Trait의 변경되지 않는 설정입니다.
        /// </summary>
        public TraitDefinition TraitDefinition { get; }

        /// <summary>
        /// 자기 턴 시작 직전의 현재 Trait 상태 복사본입니다.
        /// 예약된 효과 적용과 턴 단위 상태 초기화를 계획할 때 사용합니다.
        /// </summary>
        public TraitRuntimeState TraitRuntimeState { get; }

        public TurnStartedTraitContext(
            TraitDefinition traitDefinition,
            TraitRuntimeState traitRuntimeState)
        {
            if (traitDefinition == null)
                throw new ArgumentNullException(nameof(traitDefinition));
            if (traitRuntimeState == null)
                throw new ArgumentNullException(nameof(traitRuntimeState));
            if (traitDefinition.TraitKey != traitRuntimeState.TraitKey)
                throw new ArgumentException("Trait definition and runtime state keys must match.", nameof(traitRuntimeState));

            TraitDefinition = traitDefinition;
            TraitRuntimeState = traitRuntimeState.Clone();
        }
    }
}
