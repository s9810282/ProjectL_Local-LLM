#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
using CombatEventProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatEventProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>출처 사건 완료(순차 진행 허용)를 언제 알리는지입니다(히트 타이밍 설계 H3 정책 표).</summary>
    internal enum CombatHitCompletion
    {
        /// <summary>출처의 연출과 자원 정리가 모두 끝날 때(<see cref="CombatHitGate.EndAnimation"/>) 알립니다.</summary>
        AnimationEnd,
        /// <summary>첫 유효 해제(<see cref="CombatHitGate.Release"/>·<see cref="CombatHitGate.ReleaseAll"/>) 때 알립니다. 없으면 전체 종료 때입니다.</summary>
        FirstRelease,
        /// <summary>발사(<see cref="CombatHitGate.NotifyFired"/>) 때 알립니다. 첫 접촉과 무관합니다. 발사가 없으면 전체 종료 때입니다.</summary>
        Fired,
        // 옛 AnyRelease(후속을 출처에 맺지 못하는 특성 추가 공격 전용)는 추가 공격도 공격 그룹으로 후속을 맺게 되어 걷었습니다(특성 시스템 View 계획 R8).
    }

    /// <summary>
    /// 공격 출처(①②③)의 해제 원본입니다(히트 타이밍 설계 H1·H3). 활성 출처(공격 사건 또는 포효), 완료 정책, 그 출처에서 해제된 대상,
    /// 사건 완료 통지 여부를 소유하고 다음 사건이 남은 연출과 겹쳐도 되는지 답합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 세 시점을 구분합니다. 사건 완료 = 순차 진행을 넘겨도 됨(결과 완료·Buffer 확인 아님), 대상 해제 = 그 대상의 후속을 시작해도 됨,
    /// 전체 종료 = 출처의 연출(공격 애니메이션, 포효는 작아지기까지)과 자원 정리가 모두 끝남.
    /// 재생 시계·자세 완료는 기존 <see cref="CombatAttackPresenter"/>·조각 실행(<see cref="CombatPieceRun"/>)이 원본이며 여기 복제하지 않습니다.
    /// </para>
    /// <para>
    /// 신호는 출처 사건을 함께 받아 활성 출처와 같을 때만 반영합니다. 취소·교체 뒤의 늦은 신호와 반복 신호는 무시하고 완료는 최대 1회만 알립니다.
    /// 완료를 알리기 전에 상태를 확정하고, 알린 뒤에는 이전 출처 상태를 바꾸지 않습니다(콜백 재진입 대비).
    /// </para>
    /// <para>기차 사망의 해제는 <see cref="CombatRailwayContactPlayback"/>이 따로 소유합니다. 한 사건은 둘 중 한 곳에만 속합니다.</para>
    /// </remarks>
    internal sealed class CombatHitGate
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal CombatPlaybackProfile Profile { get; set; } = CombatEventProfiler.Unknown;

#endif
        private readonly CombatHitFollowUps _followUps;
        private readonly HashSet<EntityId> _released = new();
        private CombatEvent _source;
        private EntityId _attackerId;
        private CombatHitCompletion _completion;
        private Action _completed;
        private bool _completionNotified;

        internal CombatHitGate(CombatHitFollowUps followUps)
        {
            _followUps = followUps ?? throw new ArgumentNullException(nameof(followUps));
        }

        /// <summary>전체 종료 전의 활성 출처가 있는지입니다.</summary>
        internal bool IsActive => _source != null;

        /// <summary>사건 완료를 이미 알렸고 출처 전체 종료는 아직 남았는지입니다. 순차 진행 허용 뒤의 겹침입니다.</summary>
        internal bool AllowsOverlap => _source != null && _completionNotified;

        /// <summary>
        /// 출처 하나의 수명을 시작합니다. 연출 시작보다 먼저 부릅니다(발사 시점 0이면 시작 중 발사합니다).
        /// 이전 출처가 전체 종료되기 전에는 시작할 수 없습니다. 출처는 공격 사건만이 아니라 포효 등 어떤 사건이든 됩니다
        /// (공격 사건 전용 입구는 쓰는 곳이 없어 특성 발동 연출 계획 TA-a에서 걷었습니다).
        /// </summary>
        /// <param name="attackerId">규칙 1의 "공격자 자신"입니다. 포효는 보스입니다.</param>
        internal void Begin(CombatEvent source, EntityId attackerId, CombatHitCompletion completion, Action completed)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (!CombatEnumValues.IsDefined<CombatHitCompletion>((int)completion))
                throw new ArgumentOutOfRangeException(nameof(completion));
            if (_source != null)
                throw new InvalidOperationException("[CombatHitGate] 이전 공격이 전체 종료되기 전에 새 공격을 시작할 수 없습니다.");
            _released.Clear();
            _source = source;
            _attackerId = attackerId;
            _completion = completion;
            _completed = completed;
            _completionNotified = false;
        }

        /// <summary>발사를 알립니다. <see cref="CombatHitCompletion.Fired"/> 정책이면 사건 완료를 알립니다.</summary>
        internal void NotifyFired(CombatEvent source)
        {
            if (!IsCurrent(source)) return;
            if (_completion == CombatHitCompletion.Fired) NotifyCompleted();
        }

        /// <summary>대상 하나가 지금 맞았음을 알립니다. 그 출처에 대응한 히트 대상이 아니면 무시합니다.</summary>
        internal void Release(CombatEvent source, EntityId target)
        {
            if (!IsCurrent(source) || !_followUps.IsHitTarget(source, target) || !_released.Add(target)) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var hitSample = Profile.Hit.Auto();
#endif
            if (_completion == CombatHitCompletion.FirstRelease) NotifyCompleted();
        }

        /// <summary>
        /// 이 출처의 히트 대상이 모두 지금 맞았음을 알립니다. 대응한 대상이 없으면 첫 유효 해제로 세지 않습니다.
        /// </summary>
        internal void ReleaseAll(CombatEvent source)
        {
            if (!IsCurrent(source)) return;
            bool added = false;
            foreach (EntityId target in _followUps.HitTargetsOf(source))
                added |= _released.Add(target);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var hitSample = added ? Profile.Hit.Auto() : default;
#endif
            if (added && _completion == CombatHitCompletion.FirstRelease)
                NotifyCompleted();
        }

        /// <summary>
        /// 출처의 연출과 자원 반환이 정상으로 끝났음을 알립니다. 활성 출처를 끝내고, 아직 사건 완료를 알리지 않았으면 지금 알립니다.
        /// 취소·실패 경로에서는 부르지 않습니다(<see cref="Clear"/>).
        /// </summary>
        internal void EndAnimation(CombatEvent source)
        {
            if (!IsCurrent(source)) return;
            bool notify = !_completionNotified;
            Action completed = _completed;
            Clear();
            if (notify) completed();
        }

        /// <summary>
        /// 사건 완료 뒤 남은 연출과 겹쳐 이 사건을 시작해도 되는지입니다(설계 H3 겹침 판정 순서). 활성 출처가 없으면 true입니다.
        /// </summary>
        internal bool CanBegin(CombatEvent item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (_source == null) return true;
            if (!_completionNotified) return false;
            bool isFollowUp = _followUps.TryGet(item, out var followUp) && ReferenceEquals(followUp.Source, _source);
            // 1. 새 행동·출처를 여는 사건(특성 추가 공격, 결정 Q-TD1 A)·공격자 자신·대응되지 않은 이동은 전체 종료까지 기다립니다.
            //    출처와 행동 실행은 하나씩이라 새 출처는 이전 출처가 끝난 뒤에만 엽니다. 자세 대여가 배타적이라 공격자가 후속이어도 이 규칙이 먼저입니다.
            //    활성 출처의 이동 후속(넉백 등, 설계 §6 Q2)은 규칙 2로 판단합니다.
            if (CombatEventTypeTable.IsActionStart(item) || CombatEventTypeTable.OpensAttackSource(item) ||
                item is CombatCharacterEvent own && own.Final.EntityId == _attackerId ||
                CombatEventTypeTable.IsMovement(item) && !isFollowUp)
                return false;
            // 2. 활성 출처의 후속은 그 대상이 해제됐을 때만 시작합니다. 접촉 밖 관통 후속은 전체 종료까지 기다립니다.
            if (isFollowUp)
                return followUp.ReleasedByHit && _released.Contains(followUp.Target);
            // 3. 그 밖의 사건(영혼·효과·아티팩트 등)은 시작합니다.
            return true;
        }

        /// <summary>활성 출처와 해제 상태를 버립니다. 완료를 알리지 않습니다(취소·연결 해제·결과 준비).</summary>
        internal void Clear()
        {
            _source = null;
            _attackerId = EntityId.Empty;
            _completed = null;
            _completionNotified = false;
            _released.Clear();
        }

        /// <summary>그 출처가 아직 활성일 때만 버립니다. 시작 실패 정리가 콜백 재진입으로 시작된 새 출처를 지우지 않게 합니다.</summary>
        internal void ClearIf(CombatEvent source)
        {
            if (IsCurrent(source)) Clear();
        }

        private bool IsCurrent(CombatEvent source) => source != null && ReferenceEquals(_source, source);

        private void NotifyCompleted()
        {
            if (_completionNotified) return;
            _completionNotified = true;
            Action completed = _completed;
            _completed = null;
            completed();
        }
    }
}
