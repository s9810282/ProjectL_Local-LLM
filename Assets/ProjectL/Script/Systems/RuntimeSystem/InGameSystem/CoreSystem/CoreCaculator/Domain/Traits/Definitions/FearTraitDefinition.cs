using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>적 전용 공포 특성의 설정입니다. 기간·범위가 고정이라 설정값이 없습니다.</summary>
    public sealed class FearTraitDefinition : TraitDefinition
    {
        public FearTraitDefinition() : base(TraitKey.Fear)
        {
        }
    }
}
