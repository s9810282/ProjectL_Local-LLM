using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>
    /// 재구성 특성의 런타임 상태입니다. 전투 시작 값(아직 쓰지 않음)만 만들며, 전투 중 사용 여부는 공개 상태
    /// <c>ReconstructionTraitStatusSnapshot.Used</c>가 원본입니다(광폭화 처치 이력과 같은 방식).
    /// </summary>
    public sealed class ReconstructionRuntimeState : TraitRuntimeState
    {
        public bool Used { get; private set; }

        public ReconstructionRuntimeState(bool used = false) : base(TraitKey.Reconstruction) => Used = used;

        internal override TraitRuntimeState Clone() => new ReconstructionRuntimeState(Used);

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is not ReconstructionRuntimeState state) throw new ArgumentException("같은 특성 상태가 필요합니다.", nameof(source));
            Used = state.Used;
        }
    }
}
