using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>자동 진행 국면의 다음 단계를 처리한다.</summary>
    public interface ICombatAdvanceService : IServiceModule
    {
        /// <summary>처리해 게시했으면 true다. 자동 국면이 아니거나 Run이 끝났으면 정상 false다.</summary>
        bool AdvanceCombat();
    }
}
