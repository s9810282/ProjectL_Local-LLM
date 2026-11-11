using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>적 전용 공포 특성의 입력입니다. 설정값이 없습니다.</summary>
    public sealed class FearTraitSpec : TraitSpec
    {
        public FearTraitSpec()
            : base(TraitKey.Fear)
        {
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other) => true;
    }
}
