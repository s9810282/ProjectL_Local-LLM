using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>포효의 밤 피격 횟수다. 낮에도 보존하고 포효가 발동할 때 0으로 되돌린다(G6-2).</summary>
    public sealed class RoarRuntimeState : TraitRuntimeState
    {
        public int HitCount { get; private set; }

        public RoarRuntimeState()
            : this(hitCount: 0)
        {
        }

        public RoarRuntimeState(int hitCount)
            : base(TraitKey.Roar)
        {
            if (hitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(hitCount));
            HitCount = hitCount;
        }

        internal override TraitRuntimeState Clone()
        {
            return new RoarRuntimeState(HitCount);
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not RoarRuntimeState roarSource)
            {
                throw new ArgumentException("Roar 상태에는 Roar 최종값만 복사할 수 있습니다.", nameof(source));
            }

            HitCount = roarSource.HitCount;
        }
    }
}
