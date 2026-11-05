using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>보유자의 등록용 상태입니다. 피격 대상이나 디버프 값을 보관하지 않습니다.</summary>
    public sealed class IntimidationRuntimeState : TraitRuntimeState
    {
        public IntimidationRuntimeState()
            : base(TraitKey.Intimidation)
        {
        }

        internal override TraitRuntimeState Clone() => new IntimidationRuntimeState();

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not IntimidationRuntimeState)
                throw new ArgumentException("위압 상태에는 같은 타입의 최종 상태가 필요합니다.", nameof(source));
        }
    }
}
