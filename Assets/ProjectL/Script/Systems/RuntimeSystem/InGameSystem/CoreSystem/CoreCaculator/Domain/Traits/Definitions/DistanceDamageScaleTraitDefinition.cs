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
        /// 거리별 피해 백분율입니다. 0번이 거리 1이며, 공격이 닿는 모든 거리를 덮어야 합니다(목록 밖 거리는 계산 때 예외).
        /// </summary>
        public IReadOnlyList<int> PercentByDistance { get; }

        public DistanceDamageScaleTraitDefinition(IReadOnlyList<int> percentByDistance)
            : base(TraitKey.DistanceDamageScale)
        {
            PercentByDistance = DistanceDamageScalePercents.Copy(percentByDistance, nameof(percentByDistance));
        }
    }
}
