using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>전투 스테이지 진입을 판단하고 준비된 전투를 게시한다.</summary>
    public interface IBattleEntryService : IServiceModule
    {
        /// <summary>진입 수락 시 선택과 준비된 전투가 함께 게시된다. Job 완료와 생명주기 활성화는 호출자가 담당한다.</summary>
        ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId);
    }
}
