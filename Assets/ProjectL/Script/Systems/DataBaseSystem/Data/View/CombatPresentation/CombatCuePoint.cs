namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 이펙트 큐를 내는 자리입니다(범용 연출 구조 설계 G3). 흔들림·소리·플래시는 자리를 보지 않습니다. 0은 쓰지 않습니다.
    /// 사건마다 고를 수 있는 자리는 큐 규칙(<see cref="CombatPresentationCueRules"/>) 한 곳이 정합니다.
    /// 자리마다 위치와 회전(z = 앞)이 있고, 이펙트 큐의 위치·회전 보정은 이 회전 기준입니다(이펙트 창구 확장 설계 EC1·EC2).
    /// </summary>
    internal enum CombatCuePoint
    {
        /// <summary>행위자의 이펙트 지점(<see cref="CombatAnimationEvents"/>의 effectPoint)과 그 회전입니다. 공격 클립이 있는 유닛만 씁니다(계획 P6).</summary>
        ActorEffectPoint = 1,

        /// <summary>행위자의 발사 지점(muzzle) 위치와 행위자 자세 방향입니다(지점 자체의 회전은 쓰지 않음). 원거리 공격 클립이 있는 유닛만 씁니다(계획 P6).</summary>
        ActorMuzzle = 2,

        /// <summary>행위자 몸 상자 중심과 모델 방향입니다.</summary>
        ActorBody = 3,

        /// <summary>
        /// 주체 몸 상자 중심입니다. 방향은 칸마다 다릅니다: 피격·사망 = 맞은 방향, 아이템 발동 = 소지자가 보인 사실로 서 있는 방향(재생 중인 모델 방향이 아님),
        /// 토네이도 = 그 시점에 서 있는 방향(휩쓸리기 전·내려앉은 뒤).
        /// </summary>
        SubjectBody = 4,

        /// <summary>투사체 접촉점과 진행 방향입니다. <see cref="CombatCueAnchor.Contact"/> 시점에서만 씁니다.</summary>
        Contact = 5,

        /// <summary>
        /// 사건 칸의 표면 가운데(고지대면 그 높이)와 필드 방향입니다(이펙트 창구 확장 설계 EC2, 결정 Q-EC6 A).
        /// 오브젝트 발동·장애물 피해/파괴·추락·약한 지반 붕괴 칸이 씁니다.
        /// </summary>
        EventCell = 6,
    }
}
