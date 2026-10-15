using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>
    /// 거리별 피해 배율 특성의 공개 상태입니다. 바뀌는 값이 없어 설정값만 싣습니다.
    /// </summary>
    public sealed class DistanceDamageScaleTraitStatusSnapshot : TraitStatusSnapshot
    {
        public IReadOnlyList<int> PercentByDistance { get; }

        public DistanceDamageScaleTraitStatusSnapshot(IReadOnlyList<int> percentByDistance)
            : base(TraitKey.DistanceDamageScale)
        {
            PercentByDistance = DistanceDamageScalePercents.Copy(percentByDistance, nameof(percentByDistance));
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is DistanceDamageScaleTraitStatusSnapshot b &&
            DistanceDamageScalePercents.SameValues(PercentByDistance, b.PercentByDistance);
    }
}
