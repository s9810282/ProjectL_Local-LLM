using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>현재 선택을 추측하지 않고 지정된 챕터와 스테이지의 정적 정의를 제공한다.</summary>
    internal interface IBattleInitializationSource
    {
        BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey);
    }
}
