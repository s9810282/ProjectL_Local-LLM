#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>StageSelect 대기 중에만 사용하는 개발 명령 창구다.</summary>
    public interface IDevelopmentTools : IServiceModule
    {
        DevelopmentContext ReadContext();
        DevelopmentGoldResult GrantGold(DevelopmentContext expected, int amount);
        DevelopmentUnitResult GrantUnit(DevelopmentContext expected, EntityKey key);
        DevelopmentNavigationResult SkipNextNode(DevelopmentContext expected, StageNodeId nodeId);
        DevelopmentNavigationResult PrepareJumpToNode(DevelopmentContext expected, StageNodeId targetNodeId);
    }
}
#endif
