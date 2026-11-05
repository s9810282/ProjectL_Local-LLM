namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 연출 큐의 기준 시점입니다(범용 연출 구조 설계 G3). 큐는 이 시점 + 지연에 시작합니다. 0은 쓰지 않아 기본값 칸을 검사가 거절합니다.
    /// 사건마다 고를 수 있는 시점은 큐 규칙(<see cref="CombatPresentationCueRules"/>) 한 곳이 정합니다.
    /// </summary>
    internal enum CombatCueAnchor
    {
        /// <summary>실행 시작입니다. 사실 사건 칸은 사건 순간, 토네이도는 휩쓸리는 순간입니다.</summary>
        Start = 1,

        /// <summary>동작 기준 순간입니다. 클립이면 이벤트 타이밍, 코드 모션이면 타격(근접)·발사(원거리) 순간입니다.</summary>
        Cue = 2,

        /// <summary>대상이 맞은 순간입니다(대상 해제 직후).</summary>
        Hit = 3,

        /// <summary>투사체가 닿는 순간입니다. 이펙트만 두고, 지연은 0만 받습니다(계획 P5).</summary>
        Contact = 4,

        /// <summary>뼈대 동작의 끝입니다(클립 끝·코드 모션 끝·반응 끝·토네이도 들어 올림 끝 = 착지).</summary>
        End = 5,

        /// <summary>원거리 공격 클립의 선택 두 번째 이벤트입니다. 해당 클립에 이벤트가 있어야 합니다.</summary>
        Cue2 = 6,

        /// <summary>원거리 공격 클립의 선택 세 번째 이벤트입니다. 해당 클립에 이벤트가 있어야 합니다.</summary>
        Cue3 = 7,
    }
}
