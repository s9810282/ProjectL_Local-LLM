#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
using CombatEventProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatEventProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 전투 투사체의 진행입니다(클립 설계 5차 AC12, 7기준 검사 K2, 계획 T29a·T29b). 프리팹마다 투사체 풀 하나를 만들어 두고,
    /// 공격 재생이 부를 때 모든 풀을 한 프레임 진행하며, 취소·해제 때 정리합니다. 클립 공격이 쏜 투사체(출처 발사)도 같은 풀로 날리고 기록합니다.
    /// </summary>
    /// <remarks>
    /// 무엇을 어디로 쏘고 언제 맞는지는 공격 재생(<see cref="CombatAttackPlayback"/>)과 코드 모션이 정합니다. 이 클래스는 풀을 내주고 날리기·진행·회수만 합니다.
    /// 같은 프리팹을 쓰는 공격은 풀 하나를 함께 쓰고, 진행하는 곳도 이 클래스 하나라 같은 투사체를 두 번 진행하지 않습니다.
    /// </remarks>
    internal sealed class CombatProjectileFlights
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal CombatPlaybackProfile Profile { get; set; } = CombatEventProfiler.Unknown;

#endif
        private readonly Transform _root;
        private readonly Func<int> _generation;
        private readonly Dictionary<GameObject, CombatProjectilePool> _pools = new();
        // 진행 중 콜백이 새 풀을 만들어도 이번 프레임 순회가 깨지지 않게 쓰는 사본입니다(할당은 처음 한 번).
        private readonly List<CombatProjectilePool> _tickSnapshot = new();
        // 출처 발사로 날고 있는 투사체입니다. 도착·회수 때 뺍니다.
        private readonly List<Shot> _shots = new();

        /// <param name="root">투사체 인스턴스를 둘 부모입니다.</param>
        /// <param name="generation">사건 Presenter가 소유한 현재 재생 세대입니다. 진행 중 콜백이 세대를 바꾸면 남은 풀을 진행하지 않습니다.</param>
        internal CombatProjectileFlights(Transform root, Func<int> generation)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        /// <summary>출처 발사로 날고 있는 투사체가 있는지입니다. 코드 모션이 없을 때 공격 재생이 진행할지 정합니다(계획 P12).</summary>
        internal bool HasShots => _shots.Count > 0;

        /// <summary>그 프리팹의 투사체 풀입니다. 처음 부를 때 만듭니다.</summary>
        internal CombatProjectilePool PoolOf(GameObject prefab)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (!_pools.TryGetValue(prefab, out CombatProjectilePool pool))
            {
                pool = new CombatProjectilePool(prefab, _root);
                _pools.Add(prefab, pool);
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            pool.ProfilingProfile = Profile;
#endif
            return pool;
        }

        /// <summary>그 프리팹의 풀에 투사체를 <paramref name="count"/>개까지 꺼진 채 만들어 둡니다(GC 끊김 설계 GH6).</summary>
        internal void Prewarm(GameObject prefab, int count) => PoolOf(prefab).Prewarm(count);

        /// <summary>
        /// 출처 발사 곡사 한 발입니다(클립 공격). 착탄 때 <paramref name="hit"/> 다음 <paramref name="arrived"/>를 부릅니다.
        /// 재생 세대가 <paramref name="version"/>과 다르면 부르지 않습니다.
        /// </summary>
        /// <param name="easing">완급 곡선입니다(투사체 완급 설계 EZ1). 비행 시간은 <paramref name="duration"/> 그대로입니다.</param>
        internal ICombatShot FireArc(GameObject prefab, Vector3 from, Vector3 to, float duration, float height, Vector3 up, Vector3 spin,
            CombatProjectileEasing easing, Action hit, Action arrived, int version,
            CombatArcRotationMode rotationMode = CombatArcRotationMode.Spin)
        {
            CombatProjectilePool pool = PoolOf(prefab);
            var shot = new Shot(this, pool, new[] { (1f, hit) }, arrived, version);
            shot.Hold(pool.FireArc(from, to, duration, height, up, spin, shot.Arrive, easing, rotationMode));
            return shot;
        }

        /// <summary>
        /// 출처 발사 직선 한 발입니다(클립 공격). 진행률이 <paramref name="hitAt"/>을 지나면 <paramref name="hit"/>을 한 번 부르고,
        /// 끝에 닿으면 (아직 맞지 않았으면 <paramref name="hit"/> 먼저) <paramref name="arrived"/>를 부릅니다. 재생 세대가 <paramref name="version"/>과 다르면 부르지 않습니다.
        /// </summary>
        /// <param name="hitAt">경로 위에서 맞는 정규화 진행률(0~1)입니다. 풀이 알리는 진행률도 경로 기준이라 완급 곡선과 상관없이 접촉 지점에서 맞습니다.</param>
        /// <param name="easing">완급 곡선입니다(투사체 완급 설계 EZ1).</param>
        internal ICombatShot FireLinear(GameObject prefab, Vector3 from, Vector3 to, float speed, Vector3 up, float hitAt,
            CombatProjectileEasing easing, Action hit, Action arrived, int version) =>
            FireThrough(prefab, from, to, speed, up, easing, new[] { (hitAt, hit) }, arrived, version);

        /// <summary>
        /// 출처 발사 직선 한 발이 경로 위 순간들에 닿는 경우입니다. 진행률이 각 순간을 지날 때마다
        /// 그 콜백을 한 번씩 순서대로 부르고, 끝에 닿으면 남은 순간을 먼저 부른 뒤 <paramref name="arrived"/>를 부릅니다.
        /// 재생 세대가 <paramref name="version"/>과 다르면 부르지 않습니다. 지금은 <see cref="FireLinear"/>(한 순간)만 부릅니다(옛 Ballista 관통이 여러 순간을 썼음).
        /// </summary>
        /// <param name="contacts">경로 위에서 닿는 정규화 진행률(0~1)과 그 순간의 콜백입니다. 진행률 오름차순이어야 합니다.</param>
        private ICombatShot FireThrough(GameObject prefab, Vector3 from, Vector3 to, float speed, Vector3 up, CombatProjectileEasing easing,
            IReadOnlyList<(float Time, Action Reached)> contacts, Action arrived, int version)
        {
            if (contacts == null) throw new ArgumentNullException(nameof(contacts));
            for (int i = 0; i < contacts.Count; i++)
            {
                float time = contacts[i].Time;
                if (!float.IsFinite(time) || time < 0f || time > 1f)
                    throw new ArgumentOutOfRangeException(nameof(contacts), time, "맞는 진행률은 0~1이어야 합니다.");
                if (i > 0 && time < contacts[i - 1].Time)
                    throw new ArgumentException("맞는 진행률은 오름차순이어야 합니다.", nameof(contacts));
            }
            CombatProjectilePool pool = PoolOf(prefab);
            var shot = new Shot(this, pool, contacts, arrived, version);
            shot.Hold(pool.FireLinear(from, to, speed, up, shot.Advance, shot.Arrive, easing));
            return shot;
        }

        /// <summary>
        /// 모든 풀을 한 프레임 진행합니다. 풀 진행 중 콜백이 재생 세대를 바꾸면(<paramref name="version"/>과 다름) 남은 풀을 진행하지 않고 false입니다.
        /// </summary>
        internal bool Tick(float deltaTime, int version)
        {
            try
            {
                _tickSnapshot.AddRange(_pools.Values);
                foreach (CombatProjectilePool pool in _tickSnapshot)
                {
                    pool.Tick(deltaTime);
                    if (version != _generation())
                        return false;
                }
                return true;
            }
            finally
            {
                _tickSnapshot.Clear();
            }
        }

        /// <summary>날고 있는 투사체를 모두 회수합니다. 출처 발사 기록도 비웁니다. 풀은 남겨 다시 씁니다.</summary>
        internal void Clear()
        {
            ForgetShots();
            var actions = new List<Action>();
            foreach (CombatProjectilePool pool in _pools.Values)
                actions.Add(pool.Clear);
            RunCleanup(actions.ToArray());
        }

        /// <summary>풀을 모두 최종 해제합니다. 출처 발사 기록도 비웁니다.</summary>
        internal void Release()
        {
            ForgetShots();
            var actions = new List<Action>();
            foreach (CombatProjectilePool pool in _pools.Values)
                actions.Add(pool.Release);
            _pools.Clear();
            RunCleanup(actions.ToArray());
        }

        // 풀 정리가 투사체를 회수하므로 기록만 끝냅니다(콜백을 부르지 않음).
        private void ForgetShots()
        {
            for (int i = 0; i < _shots.Count; i++)
                _shots[i].Forget();
            _shots.Clear();
        }

        private static void RunCleanup(params Action[] actions) =>
            CleanupErrors.RunAll("[CombatProjectileFlights] 투사체 정리에 실패했습니다.", actions);

        /// <summary>출처 발사 한 발입니다. 맞는 순간마다 한 번씩(진행률 순), 도착·회수 뒤에는 콜백이 없습니다.</summary>
        private sealed class Shot : ICombatShot
        {
            private readonly CombatProjectileFlights _owner;
            private readonly CombatProjectilePool _pool;
            private readonly IReadOnlyList<(float Time, Action Reached)> _contacts;
            private readonly Action _arrived;
            private readonly int _version;
            private CombatProjectileLease _lease;
            // 다음에 알릴 맞는 순간입니다. 같은 순간을 매 프레임 다시 알리지 않는 커서입니다.
            private int _nextContact;
            private bool _done;

            internal Shot(CombatProjectileFlights owner, CombatProjectilePool pool, IReadOnlyList<(float Time, Action Reached)> contacts,
                Action arrived, int version)
            {
                _owner = owner;
                _pool = pool;
                _contacts = contacts;
                _arrived = arrived;
                _version = version;
            }

            private bool IsCurrent => !_done && _version == _owner._generation();

            internal void Hold(CombatProjectileLease lease)
            {
                _lease = lease;
                _owner._shots.Add(this);
            }

            internal void Advance(float progress)
            {
                // 콜백이 재생 세대를 바꾸면 남은 순간을 알리지 않습니다.
                while (IsCurrent && _nextContact < _contacts.Count && progress >= _contacts[_nextContact].Time)
                    _contacts[_nextContact++].Reached?.Invoke();
            }

            internal void Arrive()
            {
                if (_done) return;
                // 지난 세대의 투사체도 도착하면 기록에서 뺍니다(콜백은 부르지 않음).
                bool current = _version == _owner._generation();
                Finish();
                if (!current) return;
                // 아직 알리지 않은 순간은 도착 때 먼저 알립니다. 콜백이 세대를 바꾸면 남은 순간과 도착을 알리지 않습니다.
                while (_nextContact < _contacts.Count)
                {
                    _contacts[_nextContact++].Reached?.Invoke();
                    if (_version != _owner._generation()) return;
                }
                _arrived?.Invoke();
            }

            public void Recall()
            {
                if (_done) return;
                CombatProjectileLease lease = _lease;
                Finish();
                _pool.Recall(lease);
            }

            internal void Forget()
            {
                _done = true;
                _lease = null;
            }

            private void Finish()
            {
                _done = true;
                _lease = null;
                _owner._shots.Remove(this);
            }
        }
    }
}
