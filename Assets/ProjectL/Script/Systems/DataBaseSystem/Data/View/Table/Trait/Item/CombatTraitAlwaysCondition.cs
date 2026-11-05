namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item
{
    /// <summary>
    /// 특성 상시 이펙트가 보이는 조건입니다(이펙트 끝 규칙 설계 EL3). 끝은 조건이 정합니다 — 조건이 켜지면 내고 꺼지면 거둡니다.
    /// 새 조건 = 값 하나 + View 유지 이펙트 출처 하나입니다(계획 TP-E23).
    /// </summary>
    internal enum CombatTraitAlwaysCondition
    {
        /// <summary>보유 유닛이 필드에 살아 있고 그 특성을 지금 쓸 수 있는 동안입니다. 자리는 보유 유닛 몸입니다(설계 EL3, 계획 TP-E14).</summary>
        WhileOwned = 0,

        /// <summary>
        /// 보유 유닛이 공격 모드 행위자이고, 지금 공격 범위 응답이 준 즉시 처치 대상마다입니다(마무리 사격 FS-b, 계획 TP-E16). 자리는 그 대상 몸입니다.
        /// 지금 Core는 마무리 사격 보유자에게만 대상을 채웁니다. 다른 특성에 골라도 대상이 비어 아무것도 나오지 않습니다(설계 EL3).
        /// </summary>
        AttackModeTarget = 1
    }
}
