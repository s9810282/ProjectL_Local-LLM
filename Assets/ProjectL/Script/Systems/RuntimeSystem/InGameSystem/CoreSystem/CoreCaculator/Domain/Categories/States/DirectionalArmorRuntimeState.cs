using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States
{
    /// <summary>
    /// Unit마다 따로 소유하는 Heavy 방어 상태입니다.
    /// 방어 Facing은 footprint Rotation과 독립적으로 관리합니다.
    /// </summary>
    public sealed class DirectionalArmorRuntimeState : CategoryRuntimeState
    {
        public Rotation4 DefenseFacing { get; private set; }
        public bool MovedThisTurn { get; private set; }

        public DirectionalArmorRuntimeState(Rotation4 initialFacing)
            : this(initialFacing, false)
        {
        }

        public DirectionalArmorRuntimeState(Rotation4 defenseFacing, bool movedThisTurn)
            : base(CategoryRuleKey.DirectionalArmor)
        {
            DefenseFacing = defenseFacing;
            MovedThisTurn = movedThisTurn;
        }

        public override CategoryRuntimeState Clone()
        {
            return new DirectionalArmorRuntimeState(DefenseFacing, MovedThisTurn);
        }

        public override IEffectStateSnapshot Capture()
        {
            return new DirectionalArmorEffectStateSnapshot(DefenseFacing, MovedThisTurn);
        }

        internal void ApplyFinalValues(DirectionalArmorEffectStateSnapshot finalState)
        {
            DefenseFacing = finalState.DefenseFacing;
            MovedThisTurn = finalState.MovedThisTurn;
        }

        internal override void CopyFinalValuesFrom(CategoryRuntimeState source)
        {
            if (source is not DirectionalArmorRuntimeState typed)
                throw new System.ArgumentException("DirectionalArmor state type mismatch.", nameof(source));
            DefenseFacing = typed.DefenseFacing;
            MovedThisTurn = typed.MovedThisTurn;
        }
    }
}
