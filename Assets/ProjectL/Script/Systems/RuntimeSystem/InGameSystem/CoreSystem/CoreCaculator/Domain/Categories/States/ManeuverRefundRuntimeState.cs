using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States
{
    /// <summary>
    /// Unit마다 따로 소유하는 Rider 환급 상태입니다.
    /// 턴당 실제 양수 환급 1회라는 기회 소모만 기록하며 별도 이동 장부를 만들지 않습니다.
    /// </summary>
    public sealed class ManeuverRefundRuntimeState : CategoryRuntimeState
    {
        public bool RefundUsedThisTurn { get; private set; }

        public ManeuverRefundRuntimeState()
            : this(false)
        {
        }

        public ManeuverRefundRuntimeState(bool refundUsedThisTurn)
            : base(CategoryRuleKey.ManeuverRefund)
        {
            RefundUsedThisTurn = refundUsedThisTurn;
        }

        public override CategoryRuntimeState Clone()
        {
            return new ManeuverRefundRuntimeState(RefundUsedThisTurn);
        }

        public override IEffectStateSnapshot Capture()
        {
            return new ManeuverRefundEffectStateSnapshot(RefundUsedThisTurn);
        }

        internal void ApplyFinalValues(ManeuverRefundEffectStateSnapshot finalState)
        {
            RefundUsedThisTurn = finalState.RefundUsedThisTurn;
        }

        internal override void CopyFinalValuesFrom(CategoryRuntimeState source)
        {
            if (source is not ManeuverRefundRuntimeState typed)
                throw new System.ArgumentException("ManeuverRefund state type mismatch.", nameof(source));
            RefundUsedThisTurn = typed.RefundUsedThisTurn;
        }
    }
}
