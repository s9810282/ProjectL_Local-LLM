using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts
{
    /// <summary>
    /// 공격이 실제로 성공한 뒤 상태가 바뀌어야 하는 특성에게 전달
    /// </summary>
    internal readonly struct AttackSucceededTraitContext
    {
        /// <summary>
        /// 성공한 공격에 반응할 현재 Trait의 변경되지 않는 설정입니다.
        /// </summary>
        public TraitDefinition TraitDefinition { get; }

        /// <summary>
        /// 공격 성공 후 상태 변경을 계획하기 위해 읽는 현재 Trait 상태의 복사본입니다.
        /// Definition과 같은 TraitKey를 가집니다.
        /// </summary>
        public TraitRuntimeState TraitRuntimeState { get; }

        /// <summary>
        /// 이번 공격 비용 계산에서 양수 비용 감소를 실제로 제공한 일회성 Trait의 정렬된 Key 목록입니다.
        /// 공격 성공 후 어떤 할인 상태를 소비해야 하는지 판단할 때 사용합니다.
        /// </summary>
        public IReadOnlyList<TraitKey> PositiveCostContributors { get; }

        public AttackSucceededTraitContext(
            TraitDefinition traitDefinition,
            TraitRuntimeState traitRuntimeState,
            IReadOnlyList<TraitKey> positiveCostContributors)
        {
            if (traitDefinition == null)
                throw new ArgumentNullException(nameof(traitDefinition));
            if (traitRuntimeState == null)
                throw new ArgumentNullException(nameof(traitRuntimeState));
            if (traitDefinition.TraitKey != traitRuntimeState.TraitKey)
                throw new ArgumentException("Trait definition and runtime state keys must match.", nameof(traitRuntimeState));
            if (positiveCostContributors == null)
                throw new ArgumentNullException(nameof(positiveCostContributors));

            TraitKey[] contributors = new TraitKey[positiveCostContributors.Count];
            for (int i = 0; i < positiveCostContributors.Count; i++)
            {
                TraitKey contributor = positiveCostContributors[i];
                if (contributor == TraitKey.None || Enum.IsDefined(typeof(TraitKey), contributor) == false)
                    throw new ArgumentOutOfRangeException(nameof(positiveCostContributors));

                contributors[i] = contributor;
            }

            Array.Sort(contributors);

            TraitDefinition = traitDefinition;
            TraitRuntimeState = traitRuntimeState.Clone();
            PositiveCostContributors = Array.AsReadOnly(contributors);
        }
    }
}
