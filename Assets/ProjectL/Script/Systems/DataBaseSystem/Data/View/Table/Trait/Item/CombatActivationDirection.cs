namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item
{
    /// <summary>
    /// 발동 연출 줄이 받는 값의 방향입니다(이펙트 끝 규칙 설계 EL7). Core 발동 값은 크기만 있어서, 방향은 View가 그 발동 뒤 상태 사건의 앞뒤 값으로 정합니다.
    /// 지금은 결속 방어력(<c>DefenseChanged</c>) 줄에서만 Any가 아닐 수 있습니다(계획 TP-E18, DB 검사).
    /// </summary>
    internal enum CombatActivationDirection
    {
        /// <summary>방향을 가리지 않습니다. 방향 줄이 없거나 방향을 모를 때 이 줄을 씁니다.</summary>
        Any = 0,

        /// <summary>값이 오를 때입니다(결속: 주변 아군이 늘어 방어력 보너스가 오름).</summary>
        Up = 1,

        /// <summary>값이 내릴 때입니다(결속: 주변 아군이 줄거나 정리로 방어력 보너스가 내림).</summary>
        Down = 2
    }
}
