using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 공격 출처 하나의 맞는 순간 뒤 후속 중 한 유닛 것이 모두 끝났는지의 답입니다(특성 시스템 View 계획 TP-13, 유닛 행동 설계 AA5).
    /// 대신 맞은 수호자가 돌아오기 전에 기다리는 조건입니다.
    /// </summary>
    /// <remarks>
    /// 후속 목록의 원본은 맞는 순간 대응(<c>CombatHitFollowUps</c>, 동시 묶음이 결과마다 만듦)이고, 완료의 원본은 순차 재생기 상태입니다.
    /// 그래서 순차 재생기(<see cref="OrderedCombatPresentation"/>)가 구현하고, 재생을 붙일 때(<see cref="ICombatEventPlayback.BindSettlement"/>) 표시 쪽에 넘깁니다.
    /// 맞는 순간에 풀리는 후속(<c>ReleasedByHit</c>)만 셉니다. 출처 전체 종료 때 풀리는 후속(관통 공격의 비접촉 대상)은 그 출처 실행이 끝나야 풀려,
    /// 세면 서로 기다리다 멈추기 때문입니다. 같은 까닭으로 차례 규칙 때문에 아직 시작할 수 없는 후속(앞에 출처 전체 종료를 기다리는 다른 묶음 사건이 있음)도
    /// 세지 않습니다(묶음 끝 검토 M1).
    /// </remarks>
    internal interface ICombatSourceSettlement
    {
        /// <summary>
        /// <paramref name="source"/>(공격 사건 또는 추가 공격 시작 발동 사건)의 맞는 순간 후속 중 주체가 <paramref name="unit"/>인 사건이 모두 완료됐는지입니다.
        /// 재생 중인 결과가 없으면 true입니다.
        /// </summary>
        bool IsSettled(CombatEvent source, EntityId unit);
    }
}
