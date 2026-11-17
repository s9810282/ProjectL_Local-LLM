namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 공격 동작(앞 단계가 있으면 앞 단계부터)을 언제 시작하는지입니다(클립 칸 다시 짜기 설계 CS11). 특성 종류가 아니라 공격 하나의 공통 타이밍입니다.
    /// 선행 구간 = 이 공격 결과의 발동 중 공격 사건보다 앞에 있는 것(+ 휩쓸기 머리) 가운데 가장 앞 사건부터 공격 사건 직전까지입니다.
    /// 선행 구간이 없는 공격은 두 값 모두 공격 사건에서 바로 시작합니다.
    /// </summary>
    internal enum CombatAttackMotionStart
    {
        /// <summary>선행 구간이 끝나고 공격 사건이 시작할 때입니다(기본, 옛 시작 모드 0).</summary>
        AfterLead = 0,

        /// <summary>
        /// 선행 구간 사건 중 연출이 처음 시작되는 순간 + 지연입니다(옛 시작 모드 1·2를 합침). 연출 없이 바로 끝나는 사건은 세지 않습니다(설계 P19).
        /// 지연이 음수면 선행 구간의 첫 사건이 닿는 순간 동작을 시작하고 그 사건 연출을 그 초만큼 붙잡습니다(앞당김, 11-17 사용자 결정 A).
        /// </summary>
        WithLead = 1,
    }
}
