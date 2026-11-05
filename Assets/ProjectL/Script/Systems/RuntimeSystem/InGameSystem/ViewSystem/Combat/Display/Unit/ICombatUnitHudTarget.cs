namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 표시 하나의 HUD가 일시 피드백(<see cref="CombatUnitHudFeedback"/>)에 주는 것입니다(연출 조립 설계 §9 I-2). <see cref="FieldUnitView"/>가 구현합니다.
    /// HUD가 없는 표시에서 피해 숫자는 아무 일도 하지 않습니다. 예상 체력은 HUD가 없어도 체력 숫자에 보입니다.
    /// </summary>
    internal interface ICombatUnitHudTarget
    {
        /// <summary>켜져 있는지입니다.</summary>
        bool IsActive { get; }

        /// <summary>확정된 피해 숫자를 보입니다.</summary>
        void ShowDamage(int damage);

        /// <summary>예상 잔여 체력을 보입니다(체력 숫자 포함). null이면 지웁니다.</summary>
        void SetHealthPreview(int? health);
    }
}
