#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 일반 총알의 발사 시점·반동과 실제 종점 도착을 함께 기다립니다(히트 타이밍 설계 H6). 총알이 접촉 목록의 진행률을 지날 때마다
    /// 그 접촉을 한 번씩 알립니다(대상, 접촉 자리, 이 탄의 마지막 접촉인지). 관통 탄은 조우마다 접촉이 하나씩이고 그 밖은 하나입니다
    /// (관통·죽음의 손 View 설계 §3.1). 접촉 목록은 발사 순간 경로가 정해질 때 호출자가 한 번 만들어 줍니다(몸·기하·대체 정책은 호출자 소유).
    /// 접촉 자리(<see cref="CombatProjectileContact"/>)는 그때 함께 만듭니다(투사체 접촉 이펙트 설계 IE4).
    /// </summary>
    internal sealed class CombatLinearAttackAnimation : CombatAttackMotion
    {
        private readonly CombatAttackPresentationData _settings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly CombatPlaybackProfile _profile;

#endif
        private readonly CombatProjectilePool _pool;
        private readonly Action _fired;
        private readonly Action<EntityId, CombatProjectileContact, bool> _contacted;
        private readonly Func<Vector3, Vector3, IReadOnlyList<CombatShotContact>> _contactsOf;
        // 스킬 사건 51의 꺾인 경로입니다. 있으면 한 구간 대신 이 경로로 쏩니다(전투 스킬 View 설계 SK3). 없으면 null입니다.
        private readonly CombatShotRoute _route;
        private readonly FieldGridMapper _mapper;
        private readonly float _fireAt;
        private CombatProjectileLease _lease;
        // 발사 때 정한 접촉 목록과 그 자리입니다. 다음에 알릴 접촉은 커서로 셉니다.
        private IReadOnlyList<CombatShotContact> _contacts;
        private CombatProjectileContact[] _points;
        private int _nextContact;
        private bool _hasFired;
        private bool _flightDone;

        /// <param name="fired">발사 순간의 효과음입니다.</param>
        /// <param name="contacted">총알이 대상에 닿는 순간입니다. (닿은 대상, 접촉 자리, 이 탄의 마지막 접촉인지)를 받습니다. 없으면 알리지 않습니다.</param>
        /// <param name="contactsOf">발사 경로(월드 from, to)의 접촉 목록입니다(진행률 오름차순, 하나 이상). 없으면 종점의 대상 없는 접촉 하나입니다.</param>
        /// <param name="route">스킬 사건의 꺾인 경로입니다. 있으면 접촉 목록도 경로가 정합니다. 없으면 null입니다.</param>
        internal CombatLinearAttackAnimation(CombatAttackPresentationData settings, FieldGridMapper mapper,
            CombatProjectilePool pool, ICombatAttackUnitPose unit, Vector3 target, Quaternion emptyFacing,
            Action fired, Action cancelOwner, Action<EntityId, CombatProjectileContact, bool> contacted,
            Func<Vector3, Vector3, IReadOnlyList<CombatShotContact>> contactsOf, CombatShotRoute route = null)
            : base(unit, target, mapper != null ? mapper.WorldUp : throw new ArgumentNullException(nameof(mapper)),
                emptyFacing, settings != null ? settings.ShootDuration : throw new ArgumentNullException(nameof(settings)), cancelOwner)
        {
            if (settings.Kind != CombatAttackPresentationKind.Linear)
                throw new InvalidOperationException("[CombatLinearAttackAnimation] 직선 총알 프로필이 필요합니다.");
            _settings = settings;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profile = pool.ProfilingProfile;
#endif
            _fired = fired ?? throw new ArgumentNullException(nameof(fired));
            _contacted = contacted;
            _contactsOf = contactsOf;
            _route = route;
            _mapper = mapper;
            _fireAt = settings.ShootDuration * settings.FireTime;
        }

        public override bool IsComplete => IsRunning && PoseDone && _flightDone;
        protected override bool ResourcesAvailable => !_hasFired || _flightDone || (_lease != null && _lease.IsActive);

        protected override void Render(float progress)
        {
            float recoil = Recoil(progress, _settings.FireTime);
            Unit.ApplyPose(Origin - Forward * (_settings.RecoilDistance * recoil),
                Facing * Quaternion.Euler(-_settings.RecoilAngle * recoil, 0f, 0f));
        }

        protected override void Advance(float previous, float next)
        {
            if (_hasFired || next < _fireAt) return;
            Render(_settings.FireTime);
            int version = Version;
            if (_route != null)
            {
                FireRoute(version);
                return;
            }
            // 발사 높이·출발 오프셋 원본은 발사 기하 한 곳입니다(궤적 미리보기와 공유, 공격 궤적 설계 TJ3).
            Vector3 to = CombatShotPath.Raise(Target, Up, _settings.ProjectileHeight);
            Vector3 from = CombatShotPath.LinearStart(CombatShotPath.Raise(Origin, Up, _settings.ProjectileHeight), to);
            IReadOnlyList<CombatShotContact> contacts = _contactsOf?.Invoke(from, to);
            if (!IsCurrent(version)) return;
            if (contacts == null || contacts.Count == 0)
                contacts = new[] { new CombatShotContact(EntityId.Empty, 1f) };
            _contacts = contacts;
            _nextContact = 0;
            // 접촉 자리는 경로가 정해지는 지금 한 번 만듭니다(투사체 접촉 이펙트 설계 IE3).
            _points = new CombatProjectileContact[contacts.Count];
            for (int i = 0; i < contacts.Count; i++)
                _points[i] = CombatProjectileContact.Along(from, to, contacts[i].Progress, Up, Facing);
            // 진행률 알림이 있는 발사입니다. 방향 기준 위쪽은 기존 직선 발사와 같은 월드 위쪽입니다(표시 회전 유지).
            // 진행률은 완급 곡선을 지난 경로 진행률이라 접촉 진행률과 그대로 비교합니다(투사체 완급 설계 EZ4).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pool.ProfilingProfile = _profile;
#endif
            var lease = _pool.FireLinear(from, to, _settings.ProjectileSpeed, Vector3.up, progress =>
            {
                if (IsCurrent(version)) SendContacts(progress, version);
            }, () =>
            {
                if (!IsCurrent(version)) return;
                _lease = null;
                _flightDone = true;
                // 아직 알리지 않은 접촉은 도착 때 모두 알립니다.
                SendContacts(float.PositiveInfinity, version);
            }, _settings.ProjectileEasing);
            if (!IsCurrent(version))
            {
                _pool.Recall(lease);
                return;
            }
            _lease = lease;
            _hasFired = true;
            _fired();
        }

        // 스킬 사건의 꺾인 경로로 쏩니다. 첫 구간은 한 구간 발사와 같은 출발 오프셋이고, 접촉·자리는 경로가 정합니다(전투 스킬 View 설계 SK3).
        private void FireRoute(int version)
        {
            Vector3 firstTo = _route.FirstTarget(_mapper, Up, _settings.ProjectileHeight);
            Vector3 from = CombatShotPath.LinearStart(CombatShotPath.Raise(Origin, Up, _settings.ProjectileHeight), firstTo);
            _route.Build(from, _mapper, Up, _settings.ProjectileHeight, Facing);
            _contacts = _route.Contacts;
            _points = _route.ContactPoints;
            _nextContact = 0;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pool.ProfilingProfile = _profile;
#endif
            var lease = _pool.FirePath(_route.Points, _settings.ProjectileSpeed, Vector3.up, progress =>
            {
                if (IsCurrent(version)) SendContacts(progress, version);
            }, () =>
            {
                if (!IsCurrent(version)) return;
                _lease = null;
                _flightDone = true;
                SendContacts(float.PositiveInfinity, version);
            }, _settings.ProjectileEasing);
            if (!IsCurrent(version))
            {
                _pool.Recall(lease);
                return;
            }
            _lease = lease;
            _hasFired = true;
            _fired();
        }

        // 진행률이 지난 접촉을 순서대로 한 번씩 알립니다. 콜백이 재생 세대를 바꾸면 남은 접촉을 알리지 않습니다.
        private void SendContacts(float progress, int version)
        {
            while (_contacts != null && _nextContact < _contacts.Count && progress >= _contacts[_nextContact].Progress)
            {
                int index = _nextContact++;
                _contacted?.Invoke(_contacts[index].Target, _points[index], index == _contacts.Count - 1);
                if (!IsCurrent(version)) return;
            }
        }

        protected override void ReleaseResources(ref List<Exception> errors)
        {
            var lease = _lease;
            _lease = null;
            CleanupErrors.Try(() => _pool.Recall(lease), ref errors);
        }
    }
}
