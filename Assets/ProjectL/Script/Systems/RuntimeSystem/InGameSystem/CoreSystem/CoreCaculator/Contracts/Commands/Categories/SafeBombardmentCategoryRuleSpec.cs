using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories
{
    /// <summary>
    /// Cannon의 근접 직접 조준 제한 규칙입니다. 상태가 없는 고정 규칙입니다.
    /// </summary>
    public sealed class SafeBombardmentCategoryRuleSpec : CategoryRuleSpec
    {
        public SafeBombardmentCategoryRuleSpec()
            : base(global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CategoryRuleKey.SafeBombardment)
        {
        }
    }
}
