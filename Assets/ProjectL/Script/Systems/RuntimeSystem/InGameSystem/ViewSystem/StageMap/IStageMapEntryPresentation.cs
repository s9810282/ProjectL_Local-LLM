using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>수락된 같은 진입 요청의 표시를 진행하고 완료 여부를 반환합니다.</summary>
    internal interface IStageMapEntryPresentation
    {
        /// <param name="combatRequest">비전투 노드 이동이면 null입니다.</param>
        /// <returns>해당 요청의 표시가 끝났으면 true, 진행 중이면 false입니다.</returns>
        bool PresentAccepted(StageViewCommand command, CombatEntryRequest combatRequest);
    }
}
