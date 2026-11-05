using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Players
{
    /// <summary>
    /// 유닛별로 진행하는 연출(반응, 유닛 실행, 걸음 레인)의 지금 상태입니다. 주체 기준 막기 규칙(<see cref="CombatSubjectBlocking"/>)이 읽습니다.
    /// </summary>
    internal interface ICombatSubjectActivity
    {
        /// <summary>진행 중인 유닛 연출이 하나라도 있는지입니다.</summary>
        bool IsPlaying { get; }

        /// <summary>그 유닛의 연출이 진행 중인지입니다.</summary>
        bool IsPlayingFor(EntityId subject);

        /// <summary>진행 중인 연출이 모두 사망이라 다른 사망과 겹쳐도 되는지입니다. 진행 중인 것이 없으면 true입니다.</summary>
        bool CanOverlapDeaths { get; }
    }

    /// <summary>
    /// 사건 완료를 알린 뒤에도 남은 연출(걸음 레인의 남은 보간·미룬 방향 복귀)이 다음 사건과 겹칠 수 있는지의 답입니다(View 3라운드 W12-3).
    /// 주체 막기 규칙이 인자로 받습니다. 사건 완료 뒤 겹침이 없는 연출(반응·유닛 실행)은 이것을 넘기지 않습니다.
    /// </summary>
    internal interface ICombatSubjectOverlap
    {
        /// <summary>진행 중인 모든 연출이 사건 완료 뒤라 다음 사건과 겹쳐도 되는지입니다. 진행 중인 것이 없으면 true입니다.</summary>
        bool AllowsOverlap { get; }

        /// <summary>그 유닛의 연출이 사건 완료 뒤라 다음 사건과 겹쳐도 되는지입니다. 진행 중이 아니면 true입니다.</summary>
        bool AllowsOverlapFor(EntityId subject);
    }

    /// <summary>
    /// 주체 기준 막기 규칙의 원본입니다(동시 처리 설계 C5·C7, 조각·조립 설계 BP5). 반응 재생기, 조각 실행 재생기의 유닛 실행,
    /// 걸음 재생기의 레인이 함께 씁니다(View 3라운드 W12-3). 걸음 레인만 겹침 답을 넘깁니다.
    /// </summary>
    internal static class CombatSubjectBlocking
    {
        /// <summary>
        /// 진행 중인 유닛 연출이 이 시작을 막는지입니다. 기차 동시 사망은 사망끼리만 겹칩니다.
        /// 주체가 있는 사건은 그 유닛의 연출만 보고, 주체가 없는 사건·행동 사건·출처를 여는 사건·사건 없이 묻는 결과 시작은 모든 유닛 연출을 봅니다.
        /// </summary>
        /// <remarks>
        /// 행동 실행이 히트 전인 동안 걸음·반응 재생기는 진행하지 않습니다(조각 실행 재생기 Tick). 그래서 행동 실행을 열 수 있는 사건은 모든 유닛 연출이 끝난 뒤에만
        /// 시작해야 합니다. 행동 사건과 함께, 행동 시작은 아니지만 공격 연출을 다시 여는 사건(<see cref="CombatEventTypeTable.OpensAttackSource"/> — 특성
        /// 추가 공격 시작 발동)도 그렇습니다. 아니면 다른 유닛의 피격·사망 반응·넉백이 다시 친 공격의 맞는 순간까지 멈춥니다(특성 발동 연출 묶음 끝 검토 M1).
        /// 유닛 연출을 기다리지 않는 사건(<see cref="CombatEventTypeTable.OverlapsUnitRuns"/>, 움직이는 토네이도)은 Tick을 멈추지 않는 필드 행동만 엽니다.
        /// </remarks>
        /// <param name="overlap">
        /// 사건 완료 뒤 겹침 답입니다. 겹침 허용 프로필(<see cref="CombatStartQuery.AllowsOverlap"/>)이면 사건 완료를 알린 연출은 막지 않습니다.
        /// 없으면(반응·유닛 실행) 진행 중인 연출은 끝날 때까지 막습니다.
        /// </param>
        internal static bool Blocks(in CombatStartQuery query, ICombatSubjectActivity activity, ICombatSubjectOverlap overlap = null)
        {
            if (activity == null) throw new ArgumentNullException(nameof(activity));
            if (query.AllowsDeaths) return !activity.CanOverlapDeaths;
            if (!activity.IsPlaying) return false;
            if (CombatEventTypeTable.OverlapsUnitRuns(query.Item)) return false;
            ICombatSubjectOverlap allowed = query.AllowsOverlap ? overlap : null;
            CombatEvent item = query.Item;
            if (item == null || CombatEventTypeTable.IsActionStart(item) || CombatEventTypeTable.OpensAttackSource(item) ||
                !CombatEventTypeTable.TryGetSubject(item, out EntityId subject))
                return allowed == null || !allowed.AllowsOverlap;
            return activity.IsPlayingFor(subject) && (allowed == null || !allowed.AllowsOverlapFor(subject));
        }
    }
}
