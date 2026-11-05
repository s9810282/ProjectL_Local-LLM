namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 이펙트 큐 하나가 언제 끝나는지입니다(이펙트 끝 규칙 설계 EL1). 기본값 0이 자연이라 이 칸이 없던 에셋은 지금과 같습니다.
    /// 오래 보이는 이펙트(상태 동안·보유 동안)는 큐 끝이 아니라 상태 외형·특성 상시 목록이 정합니다(설계 EL2·EL3).
    /// </summary>
    internal enum CombatEffectEnd
    {
        /// <summary>프리팹이 스스로 멈출 때입니다. 끝나지 않는 파티클(Looping·Ring Buffer)이 있으면 창구가 종류마다 한 번 경고합니다(설계 EL6).</summary>
        Natural = 0,

        /// <summary>낸 뒤 정한 시간(<c>endSeconds</c>)이 지나면 거둡니다. 거두는 모양은 회수와 같습니다(링 버퍼는 바로 지움, 아니면 방출만 멈춤).</summary>
        Seconds = 1,

        /// <summary>
        /// 그 발동이 연 유닛 행동이 모두 끝나면 거둡니다(설계 EL11, 계획 TP-E1·TP-E3). 특성 발동 줄의 큐에서만, 그 줄에 유닛 행동이 있을 때만 고를 수 있습니다(DB 검사, 계획 TP-E2).
        /// 연 행동이 하나도 없으면(바쁨·건너뜀) 그 자리에서 거두고, 늦어도 결과 끝에 거둡니다(계획 TP-E4).
        /// </summary>
        UnitAction = 2
    }
}
