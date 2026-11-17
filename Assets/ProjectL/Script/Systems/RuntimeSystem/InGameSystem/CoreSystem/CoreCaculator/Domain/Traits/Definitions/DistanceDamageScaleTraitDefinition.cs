using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>
    /// 거리별 피해 배율 특성의 변하지 않는 설정입니다.
    /// </summary>
    public sealed class DistanceDamageScaleTraitDefinition : TraitDefinition
    {
        /// <summary>
        /// 거리별 피해 백분율입니다. 0번이 거리 1입니다. 원래 공격이 닿는 거리는 모두 설정해야 합니다.
        /// 죽음의 손으로 목록 밖에 도달한 대상에는 마지막 백분율을 적용합니다.
        /// </summary>
        public IReadOnlyList<int> PercentByDistance { get; }

        public DistanceDamageScaleTraitDefinition(IReadOnlyList<int> percentByDistance)
            : base(TraitKey.DistanceDamageScale)
        {
            PercentByDistance = DistanceDamageScalePercents.Copy(percentByDistance, nameof(percentByDistance));
        }
    }
}
