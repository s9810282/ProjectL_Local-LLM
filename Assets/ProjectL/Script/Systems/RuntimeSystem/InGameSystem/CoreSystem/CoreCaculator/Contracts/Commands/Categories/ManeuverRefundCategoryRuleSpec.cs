using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories
{
    /// <summary>
    /// Rider의 이동 비용 환급 규칙입니다. 고정 규칙이므로 추가 설정을 받지 않습니다.
    /// </summary>
    public sealed class ManeuverRefundCategoryRuleSpec : CategoryRuleSpec
    {
        public ManeuverRefundCategoryRuleSpec()
            : base(global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CategoryRuleKey.ManeuverRefund)
        {
        }
    }
}
