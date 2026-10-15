using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>
    /// 거리별 피해 배율 특성의 런타임 상태입니다. 전투 중 바뀌는 값이 없습니다.
    /// </summary>
    public sealed class DistanceDamageScaleRuntimeState : TraitRuntimeState
    {
        public DistanceDamageScaleRuntimeState()
            : base(TraitKey.DistanceDamageScale)
        {
        }

        internal override TraitRuntimeState Clone()
        {
            return new DistanceDamageScaleRuntimeState();
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not DistanceDamageScaleRuntimeState)
            {
                throw new ArgumentException(
                    "DistanceDamageScale 상태에는 같은 타입의 최종 상태가 필요합니다.",
                    nameof(source));
            }
        }
    }
}
