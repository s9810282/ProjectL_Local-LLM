using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>View가 제출한 다음 전투 명령 하나를 수락·처리한다.</summary>
    public interface ICombatCommandService : IServiceModule
    {
        /// <summary>처리해 게시했으면 true다. 처리할 명령이 없거나 Run이 끝났으면 정상 false다.</summary>
        bool ProcessNextCommand();
    }
}
