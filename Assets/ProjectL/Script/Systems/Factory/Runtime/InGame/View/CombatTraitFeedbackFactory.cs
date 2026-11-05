using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>특성 피드백의 상태 근거 처리기를 한 곳에서 조립합니다.</summary>
    internal static class CombatTraitFeedbackFactory
    {
        internal static CombatTraitStateEvidenceRegistry CreateEvidence() => new(new ICombatTraitStateEvidence[] { new CombatAccelerationEvidence(), new CombatAttackGrantEvidence(), new CombatWeaknessExposureEvidence() });
    }
}
