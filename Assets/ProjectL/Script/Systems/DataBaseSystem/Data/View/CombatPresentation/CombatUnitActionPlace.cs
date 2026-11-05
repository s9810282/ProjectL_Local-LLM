namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 특성 발동의 유닛 행동이 어디로 가는지입니다(유닛 행동 설계 AA1·AA4). 끝 자리 계산은 View의 자리 계산 한 곳이 합니다.
    /// </summary>
    internal enum CombatUnitActionPlace
    {
        /// <summary>제자리입니다. 방향만 바꿉니다.</summary>
        Stay = 0,

        /// <summary>
        /// 지켜진 유닛 앞(공격자 쪽)입니다. 가로채기(대신 맞기)에서만 뜻이 있고, 가로채기가 없는 발동에 쓰이면 실행 때 경고 한 번 뒤 제자리로 합니다(설계 P10).
        /// </summary>
        GuardFront = 1,

        /// <summary>행동 유닛 자리에서 대상 쪽입니다.</summary>
        TowardTarget = 2,

        /// <summary>행동 유닛의 원래 자리에서 공격자 반대쪽입니다. 가로채기에서만 공격자 기준점이 있습니다.</summary>
        AwayFromAttacker = 3,

        /// <summary>
        /// 위치 이동입니다(이펙트 끝 규칙 설계 EL11, 계획 TP-E5). 그 유닛의 미뤄 둔 사실 자리(위치 교환 승객의 새 자리)로 한 번 가고 돌아오지 않습니다.
        /// 미룬 배치가 있는 유닛만 하고, 없으면(이동자 — 이미 사실 자리) 경고 없이 건너뜁니다(계획 TP-E6). 도착 방향은 사실 방향입니다.
        /// 쓰는 칸은 바깥 시간·걸음 모양뿐이고 거리·머묾·돌아오는 시간은 0, 보는 쪽은 Keep이어야 합니다(값 검사). 대신 맞기(가로채기)에서는 하지 않습니다.
        /// </summary>
        Relocate = 4
    }
}
