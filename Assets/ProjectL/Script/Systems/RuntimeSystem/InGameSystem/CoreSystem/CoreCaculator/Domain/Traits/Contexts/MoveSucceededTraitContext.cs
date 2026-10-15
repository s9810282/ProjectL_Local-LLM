using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts
{
    /// <summary>
    /// 이동이 성공했을 때 반응하는 특성에게 전달
    /// </summary>
    internal readonly struct MoveSucceededTraitContext
    {
        /// <summary>
        /// 성공한 이동에 반응할 현재 Trait의 변경되지 않는 설정입니다.
        /// </summary>
        public TraitDefinition TraitDefinition { get; }

        /// <summary>
        /// 이동 성공 직후 상태 변경을 계획하기 위해 읽는 현재 Trait 상태의 복사본입니다.
        /// Definition과 같은 TraitKey를 가집니다.
        /// </summary>
        public TraitRuntimeState TraitRuntimeState { get; }

        /// <summary>
        /// 성공한 이동에서 실제로 이동한 거리입니다.
        /// </summary>
        public int MoveDistance { get; }

        /// <summary>
        /// 이동 전후로 Unit의 기준 위치가 실제로 달라졌는지를 나타냅니다.
        /// </summary>
        public bool AnchorChanged { get; }

        /// <summary>
        /// 성공한 이동을 확정하면서 실제로 소비한 행동 비용입니다.
        /// </summary>
        public int ActionCostSpent { get; }

        public MoveSucceededTraitContext(
            TraitDefinition traitDefinition,
            TraitRuntimeState traitRuntimeState,
            int moveDistance,
            bool anchorChanged,
            int actionCostSpent)
        {
            if (traitDefinition == null)
                throw new ArgumentNullException(nameof(traitDefinition));
            if (traitRuntimeState == null)
                throw new ArgumentNullException(nameof(traitRuntimeState));
            if (traitDefinition.TraitKey != traitRuntimeState.TraitKey)
                throw new ArgumentException("Trait definition and runtime state keys must match.", nameof(traitRuntimeState));
            if (moveDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(moveDistance));
            if (actionCostSpent < 0)
                throw new ArgumentOutOfRangeException(nameof(actionCostSpent));

            TraitDefinition = traitDefinition;
            TraitRuntimeState = traitRuntimeState.Clone();
            MoveDistance = moveDistance;
            AnchorChanged = anchorChanged;
            ActionCostSpent = actionCostSpent;
        }
    }
}
