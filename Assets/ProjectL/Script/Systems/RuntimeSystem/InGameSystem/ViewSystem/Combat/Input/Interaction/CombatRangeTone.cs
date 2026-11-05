namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 호버 범위를 어떤 뜻으로 그릴지입니다(호버 공격 범위 설계 Q-UR3 B·§8 HZ5). 무엇이 어느 색조인지는 호버 입력이 정하고,
    /// 색조가 어느 강조 층·색인지는 표시가 정합니다. 0은 두지 않아 기본값으로 그리지 않습니다.
    /// </summary>
    internal enum CombatRangeTone
    {
        /// <summary>아군 유닛의 공격 범위(공격 모드와 같은 공격 범위 색).</summary>
        Ally = 1,

        /// <summary>적 유닛의 공격 범위.</summary>
        Enemy = 2,

        /// <summary>지뢰·화약통처럼 진영을 가리지 않는 환경 위험의 피해 칸.</summary>
        Hazard = 3
    }
}
