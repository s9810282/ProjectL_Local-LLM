namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 연출 큐의 기준 시점입니다(범용 연출 구조 설계 G3). 큐는 이 시점 + 지연에 시작합니다. 0은 쓰지 않아 기본값 칸을 검사가 거절합니다.
    /// 사건마다 고를 수 있는 시점은 큐 규칙(<see cref="CombatPresentationCueRules"/>) 한 곳이 정합니다.
    /// 클립 이벤트 시점의 이름은 클립 이벤트 번호와 같습니다(Event1~3, 클립 칸 다시 짜기 설계 CS3). 값은 옛 Cue·Cue2·Cue3(2·6·7)를 그대로 써 에셋은 바뀌지 않습니다.
    /// 옛 맞음(Hit = 3)은 접촉(<see cref="Contact"/>)에 합쳤습니다(설계 CS10, 결정 Q-B2) — 어느 접촉에 낼지는 큐 칸 <see cref="CombatContactFilter"/>가 고릅니다.
    /// </summary>
    internal enum CombatCueAnchor
    {
        /// <summary>실행 시작입니다. 사실 사건 칸은 사건 순간, 토네이도는 휩쓸리는 순간입니다.</summary>
        Start = 1,

        /// <summary>
        /// 첫 클립 이벤트(<c>ClipEvent</c>)입니다. 코드 모션이면 타격(근접)·발사(원거리) 순간입니다. 옛 이름 Cue.
        /// </summary>
        Event1 = 2,

        /// <summary>
        /// 공격 수단이 대상에 닿는 순간입니다(원거리 = 투사체 접촉, 곡사 = 도착 순간). 어느 접촉에 낼지는 <see cref="CombatContactFilter"/>가 고릅니다
        /// (<see cref="CombatContactFilter.Every"/> = 접촉마다·이펙트만·지연 0, <see cref="CombatContactFilter.FirstHit"/> = 공격당 한 번 첫 실제 피격 = 옛 Hit).
        /// </summary>
        Contact = 4,

        /// <summary>뼈대 동작의 끝입니다(클립 끝·코드 모션 끝·반응 끝·토네이도 들어 올림 끝 = 착지).</summary>
        End = 5,

        /// <summary>두 번째 클립 이벤트(<c>ClipEvent2</c>)입니다. 해당 클립에 이벤트가 있어야 합니다. 옛 이름 Cue2.</summary>
        Event2 = 6,

        /// <summary>세 번째 클립 이벤트(<c>ClipEvent3</c>)입니다. 해당 클립에 이벤트가 있어야 합니다. 옛 이름 Cue3.</summary>
        Event3 = 7,
    }

    /// <summary>
    /// 접촉(<see cref="CombatCueAnchor.Contact"/>) 시점에서 어느 접촉에 낼지입니다(클립 칸 다시 짜기 설계 CS10, 결정 Q-B2). 접촉이 아닌 시점에서는 보지 않습니다. 0은 쓰지 않습니다.
    /// 맞음(첫 실제 피격)은 접촉의 한 경우라 시점을 따로 두지 않습니다.
    /// </summary>
    internal enum CombatContactFilter
    {
        /// <summary>모든 접촉(앞 접촉 포함)입니다. 이펙트만, 자리는 접촉점, 지연은 0만 됩니다(옛 Contact).</summary>
        Every = 1,

        /// <summary>공격당 한 번, 첫 실제 피격 접촉(첫 대상 해제 직후)입니다. 옛 맞음(Hit) 시점과 같은 규칙입니다.</summary>
        FirstHit = 2,
    }
}
