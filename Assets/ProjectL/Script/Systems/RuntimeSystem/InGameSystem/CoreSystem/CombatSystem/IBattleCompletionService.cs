using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>전투 포기와 완료 후 정리를 맡는다. 씬 전환·표시 재생은 하지 않는다.</summary>
    public interface IBattleCompletionService : IServiceModule
    {
        /// <summary>
        /// 활성 전투를 자발적 포기(패배)로 완료한다. 정산 입력(현재 Artifact 배치 효과 등)은 Core가 내부 상태에서 조립한다.
        /// 완료 기록까지만 하고 일반 전투 결과(CombatResult)는 새로 게시하지 않는다. 표시·EndBattle·전환은 호출하는 흐름이 한다.
        /// </summary>
        BattleAbandonResult AbandonBattle();

        /// <summary>완료가 기록된 전투만 정리한다. 완료 전 호출은 상태 오류다.</summary>
        void EndBattle();
    }
}
