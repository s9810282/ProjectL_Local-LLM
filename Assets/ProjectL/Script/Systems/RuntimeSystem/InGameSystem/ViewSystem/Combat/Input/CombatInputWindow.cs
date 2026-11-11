using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>안내 표시가 읽는 현재 전투·국면·입력 가능 여부입니다. 입력 허가나 상태를 소유하지 않습니다.</summary>
    internal readonly struct CombatInputWindow
    {
        internal CombatInputWindow(long battleInstanceId, StagePhase stagePhase, bool canShowGuidance)
        {
            BattleInstanceId = battleInstanceId;
            StagePhase = stagePhase;
            CanShowGuidance = canShowGuidance;
        }

        internal long BattleInstanceId { get; }
        internal StagePhase StagePhase { get; }
        internal bool CanShowGuidance { get; }
    }
}
