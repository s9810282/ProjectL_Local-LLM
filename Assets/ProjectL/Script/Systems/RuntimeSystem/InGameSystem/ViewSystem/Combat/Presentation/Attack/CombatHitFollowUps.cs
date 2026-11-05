using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 결과의 각 출처(공격 사건, 반응 공격 결과가 있는 포효, 추가 공격 시작 발동 사건) 뒤에서 그 출처·대상의 후속 사건을 Core 사실만으로 대응합니다
    /// (히트 타이밍 설계 H2, 옛 <c>CombatBallistaImpactMap</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 피해 후속 = 피해 근원(<see cref="CombatEventTypeTable.TryGetOrigins"/>가 답하는 근원 목록) 중
    /// 공격 그룹과 가해자(<c>CreditedCharacterId</c>)가 그 출처와 같은 것이 있는 사건입니다. 칸·이름으로 추측하지 않습니다.
    /// 체력·사망 사건뿐 아니라 그 공격이 낳은 특성 발동·걸린 상태 바뀜도 근원이 있으면 후속입니다(특성 시스템 View 설계 TS1·TS3).
    /// 주체가 출처 공격자 자신인 후속은 해제 대상이 아닙니다(<see cref="AddFollowUp"/> 설명).
    /// </para>
    /// <para>
    /// 이동 후속 = 가장 최근에 연 출처부터 다음 새 행동 전까지의 이동 사건(<see cref="CombatEventTypeTable.IsMovement"/>: <c>Moved</c>·<c>Returned</c>·<c>Relocated</c>)입니다(설계 §6 Q2 재결정).
    /// 넉백 이동에는 근원이 없어 Core 사건 순서(공격 처리 안에서 피해 → 넉백 → 도착 정산 기록)로 묶습니다.
    /// </para>
    /// <para>
    /// 대응은 다음 새 행동 사건(<see cref="CombatEventTypeTable.IsActionStart"/>)에서 끊깁니다. 한 행동 구간 안에 출처가 여럿일 수 있습니다
    /// (추가 공격 시작, <see cref="Begin"/>). 출처가 될 수 있는 사건은
    /// <see cref="CombatEventTypeTable.TryGetAttackSource"/>가 답하고, 대상 없는 포효는 출처가 아닙니다.
    /// 관통 공격은 <c>PiercingShot.Encounters</c>의 대상만 히트(접촉)로 해제되고, 대응은 되지만 접촉 대상이 아닌 후속은
    /// 출처 전체 종료 때 풀립니다(<see cref="FollowUp.ReleasedByHit"/> false). 해제 상태는 <see cref="CombatHitGate"/>가 소유합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatHitFollowUps
    {
        /// <summary>후속 사건 하나의 대응입니다.</summary>
        internal readonly struct FollowUp
        {
            internal FollowUp(CombatEvent source, EntityId target, bool releasedByHit)
            {
                Source = source;
                Target = target;
                ReleasedByHit = releasedByHit;
            }

            /// <summary>후속을 만든 출처 사건(공격·포효·추가 공격 시작)입니다.</summary>
            internal CombatEvent Source { get; }
            internal EntityId Target { get; }
            /// <summary>히트 신호(대상 해제·모두 해제)로 풀리는지입니다. false면 출처 전체 종료까지 기다립니다.</summary>
            internal bool ReleasedByHit { get; }
        }

        private static readonly IReadOnlyCollection<EntityId> NoTargets = Array.Empty<EntityId>();

        private readonly Dictionary<CombatEvent, FollowUp> _followUps = new();
        private readonly Dictionary<CombatEvent, HashSet<EntityId>> _hitTargets = new();
        // 지금 행동 구간에서 연 출처들입니다(근원 열쇠 → 출처 사건·사실). 재생 준비 때만 쓰는 작업 표라 Begin이 비우고 채웁니다.
        private readonly Dictionary<CombatEventSubjects.OriginKey, (CombatEvent Event, CombatAttackSource Source)> _openSources = new();

        /// <summary>
        /// 결과의 사건을 처음부터 훑어 후속을 대응합니다(특성 시스템 View 계획 TP-2). 행동 시작에서 출처 목록을 비우고 그 사건이 출처면 엽니다.
        /// 추가 공격 시작(<see cref="CombatEventTypeTable.OpensAttackSource"/>)은 목록을 비우지 않고 출처를 하나 더 엽니다(원래 공격의 뒤 후속도 그대로 대응).
        /// 피해 후속은 근원 열쇠로 출처를 찾고, 이동 후속은 가장 최근에 연 출처에 붙입니다. Core가 원래 공격의 넉백을 처치 연쇄·추가 공격보다 먼저 기록하기 때문입니다
        /// (<c>AttackDeliveryExecutor</c>: 넉백 → 도착 정산 → 처치 연쇄 → 추가 공격).
        /// </summary>
        internal void Begin(CombatResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            Clear();
            CombatEvent latest = null;
            CombatAttackSource latestSource = default;
            foreach (CombatEvent item in result.Events)
            {
                bool actionStart = CombatEventTypeTable.IsActionStart(item);
                if (actionStart || CombatEventTypeTable.OpensAttackSource(item))
                {
                    if (actionStart)
                    {
                        _openSources.Clear();
                        latest = null;
                    }
                    // 공격 그룹 0(근원 미지정)은 출처로 열지 않습니다. 대상 없는 포효·소환·턴은 출처 사실이 없습니다.
                    if (CombatEventTypeTable.TryGetAttackSource(item, out CombatAttackSource opened) && opened.AttackGroup != 0)
                    {
                        _openSources[opened.Key] = (item, opened);
                        latest = item;
                        latestSource = opened;
                    }
                    continue;
                }
                if (latest == null) continue;
                // 이동 사건·근원을 가질 수 있는 사건 종류·대상(주체) 판정은 사건 형식 표 한 곳을 씁니다(View 2라운드 W7-2).
                if (item is CombatCharacterEvent character && CombatEventTypeTable.IsMovement(character))
                    AddFollowUp(item, latest, latestSource, character.Final.EntityId);
                else if (CombatEventTypeTable.TryGetOrigins(item, out IReadOnlyList<CombatDamageOrigin> origins) &&
                         CombatEventTypeTable.TryGetSubject(item, out EntityId target) &&
                         TryFindSource(origins, out (CombatEvent Event, CombatAttackSource Source) found))
                    AddFollowUp(item, found.Event, found.Source, target);
            }
            _openSources.Clear();
        }

        internal bool TryGet(CombatEvent item, out FollowUp followUp) => _followUps.TryGetValue(item, out followUp);

        /// <summary>그 출처에서 히트 신호로 풀리는 대상들입니다. 모두 해제(<see cref="CombatHitGate.ReleaseAll"/>)의 범위입니다.</summary>
        internal IReadOnlyCollection<EntityId> HitTargetsOf(CombatEvent source) =>
            source != null && _hitTargets.TryGetValue(source, out var targets) ? targets : NoTargets;

        internal bool IsHitTarget(CombatEvent source, EntityId target) =>
            source != null && _hitTargets.TryGetValue(source, out var targets) && targets.Contains(target);

        internal void Clear()
        {
            _followUps.Clear();
            _hitTargets.Clear();
            _openSources.Clear();
        }

        // 근원 목록에서 지금 열린 출처와 열쇠(공격 그룹 + 가해자)가 같은 첫 근원의 출처입니다. 칸·이름으로 추측하지 않습니다.
        private bool TryFindSource(IReadOnlyList<CombatDamageOrigin> origins, out (CombatEvent Event, CombatAttackSource Source) found)
        {
            foreach (CombatDamageOrigin origin in origins)
            {
                if (origin != null && _openSources.TryGetValue(new CombatEventSubjects.OriginKey(origin.AttackGroup, origin.CreditedCharacterId), out found))
                    return true;
            }
            found = default;
            return false;
        }

        // 공격자 자신의 후속(강제 이동, 그 공격이 낳은 자기 발동·걸린 상태)은 대응하되 히트 해제 대상에 넣지 않습니다(특성 시스템 View 계획 TP-5).
        // 맞은 대상이 없는데 사건 완료가 앞당겨지지 않게 합니다. 그 후속은 해제되지 않으므로 출처 전체 종료 뒤에 시작합니다(이동은 Gate 규칙 1도 막음).
        private void AddFollowUp(CombatEvent item, CombatEvent source, in CombatAttackSource fact, EntityId target)
        {
            bool byHit = fact.PiercingShot == null || IsEncounter(fact.PiercingShot, target);
            // 같은 사건 인스턴스가 결과에 두 번 있으면 첫 위치를 씁니다(재생 준비가 예외로 멈추지 않게, 동시 처리 계획 C-1).
            if (!_followUps.TryAdd(item, new FollowUp(source, target, byHit))) return;
            if (!byHit || target == fact.AttackerId) return;
            if (!_hitTargets.TryGetValue(source, out var targets))
                _hitTargets.Add(source, targets = new HashSet<EntityId>());
            targets.Add(target);
        }

        private static bool IsEncounter(PiercingShotDelivery shot, EntityId target)
        {
            foreach (var encounter in shot.Encounters.Span)
                if (encounter.TargetId == target) return true;
            return false;
        }
    }
}
