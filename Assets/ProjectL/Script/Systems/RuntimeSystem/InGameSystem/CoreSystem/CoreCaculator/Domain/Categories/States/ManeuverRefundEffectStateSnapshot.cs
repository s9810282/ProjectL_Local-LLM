using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States
{
    /// <summary>
    /// 공통 CombatEffects에 전달하는 불변 환급 상태입니다.
    /// </summary>
    public sealed class ManeuverRefundEffectStateSnapshot : IEffectStateSnapshot
    {
        public bool RefundUsedThisTurn { get; }

        public ManeuverRefundEffectStateSnapshot(bool refundUsedThisTurn)
        {
            RefundUsedThisTurn = refundUsedThisTurn;
        }

        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is ManeuverRefundEffectStateSnapshot snapshot &&
                   snapshot.RefundUsedThisTurn == RefundUsedThisTurn;
        }
    }
}
