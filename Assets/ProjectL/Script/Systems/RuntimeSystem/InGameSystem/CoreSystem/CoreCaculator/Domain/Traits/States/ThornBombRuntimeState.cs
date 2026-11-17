using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>특성 장착 표식이다. 실제 부착 폭탄은 명령 상태에만 저장한다.</summary>
    public sealed class ThornBombRuntimeState : TraitRuntimeState
    {
        public ThornBombRuntimeState() : base(TraitKey.ThornBomb)
        {
        }

        internal override TraitRuntimeState Clone() => new ThornBombRuntimeState();

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not ThornBombRuntimeState)
                throw new ArgumentException("가시 폭탄 특성 상태가 필요합니다.", nameof(source));
        }
    }
}
