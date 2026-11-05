#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.Development
{
    internal interface IDevelopmentStateCommands : IServiceModule
    {
        DevelopmentContext ReadContext();
        DevelopmentGoldResult GrantGold(DevelopmentContext expected, int amount);
        DevelopmentUnitResult GrantUnit(DevelopmentContext expected, EntityKey key);
        DevelopmentNavigationResult SkipNextNode(DevelopmentContext expected, StageNodeId nodeId);
        DevelopmentNavigationResult PrepareJumpToNode(DevelopmentContext expected, StageNodeId targetNodeId);
    }

    /// <summary>기존 Core 그룹에서 선택적으로 조립한 개발 기능만 조회한다.</summary>
    internal interface IDevelopmentStateProvider
    {
        IDevelopmentStateCommands DevelopmentState { get; }
    }
}
#endif
