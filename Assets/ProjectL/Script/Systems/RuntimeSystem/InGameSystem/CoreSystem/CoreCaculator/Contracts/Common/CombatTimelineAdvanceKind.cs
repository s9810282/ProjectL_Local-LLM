using System;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>
    /// AdvanceTimeline이 호출에서 무엇을 처리했는지 구분
    /// </summary>
    public enum CombatTimelineAdvanceKind
    {
        None = 0,
        UnitTurnStarted = 1,
        StageGimmickActivated = 2,
    }
}
