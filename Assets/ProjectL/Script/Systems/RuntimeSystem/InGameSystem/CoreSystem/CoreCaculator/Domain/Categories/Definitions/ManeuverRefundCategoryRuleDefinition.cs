using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions
{
    /// <summary>
    /// Rider의 이동 비용 환급 규칙 설정입니다. 고정 규칙이라 추가 값이 없습니다.
    /// </summary>
    public sealed class ManeuverRefundCategoryRuleDefinition : CategoryRuleDefinition
    {
        public ManeuverRefundCategoryRuleDefinition()
            : base(CategoryRuleKey.ManeuverRefund)
        {
        }
    }
}
