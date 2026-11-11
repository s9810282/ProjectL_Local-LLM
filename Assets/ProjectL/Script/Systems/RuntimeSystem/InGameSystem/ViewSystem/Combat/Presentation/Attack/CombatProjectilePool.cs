#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
using CombatEventProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatEventProfiler;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>View Tick으로 직선·곡사 발사체를 진행하고 대여·반환·최종 파괴를 소유합니다.</summary>
    /// <remarks>전달받은 루트는 소유하지 않습니다. 생성한 비활성 보관 루트와 모든 복제본만 Release에서 파괴합니다.</remarks>
    internal sealed class CombatProjectilePool
    {
        private sealed class Instance
        {
            internal readonly GameObject Object;
            // 꼬리(TrailRenderer)는 만들 때 한 번 모읍니다. 다시 쏠 때 지난 착탄점에서 이어지지 않게 비웁니다(평가 G1-03).
            private readonly TrailRenderer[] _trails;

            internal Instance(GameObject gameObject)
            {
                Object = gameObject;
                _trails = gameObject != null ? gameObject.GetComponentsInChildren<TrailRenderer>(true) : Array.Empty<TrailRenderer>();
            }

            internal void ClearTrails()
            {
                for (int i = 0; i < _trails.Length; i++)
                    if (_trails[i] != null) _trails[i].Clear();
            }
        }

        private sealed class Flight
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            internal CombatPlaybackProfile Profile;
#endif
            internal Instance Instance;
            internal CombatProjectileLease Lease;
            internal Vector3 From;
            internal Vector3 To;
            internal Vector3 Up;
            internal Vector3 Spin;
            internal CombatArcRotationMode RotationMode;
            internal Quaternion StartRotation;
            internal float Duration;
            internal float Height;
            internal CombatProjectileEasing Easing;
            internal float Elapsed;
            internal Action Completed;
            internal Action<float> Advanced;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal CombatPlaybackProfile ProfilingProfile { get; set; } = CombatEventProfiler.Unknown;

#endif
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Vector3 _prefabScale;
        private readonly Quaternion _prefabRotation;
        private readonly List<Instance> _instances = new();
        private readonly Dictionary<CombatProjectileLease, Flight> _active = new();
        private readonly List<Flight> _tickSnapshot = new();
        private readonly Queue<GameObject> _pendingDestruction = new();
        private CustomPool<Instance> _pool;
        private GameObject _inactiveRoot;
        private bool _released;
        private bool _clearing;
        private bool _ticking;
        private bool _destroying;
        private int _visibilityDepth;
        private long _cancellationVersion;

        internal CombatProjectilePool(GameObject prefab, Transform root)
        {
            _prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _prefabScale = prefab.transform.localScale;
            _prefabRotation = prefab.transform.localRotation;
            _inactiveRoot = new GameObject("CombatProjectilePool_Inactive");
            SetActiveSafely(_inactiveRoot, false);
            _inactiveRoot.transform.SetParent(root, false);
            _pool = new CustomPool<Instance>(new PoolContext<Instance>(CreateInstance, 0));
        }

        internal int ActiveCount => _active.Count;

        /// <summary>
        /// 만든 투사체가 <paramref name="count"/>보다 적으면 모자란 만큼 꺼진 채 만들어 풀에 넣습니다(GC 끊김 설계 GH6). 날고 있는 것도 만든 수에 듭니다.
        /// 공격 중 처음 쏠 때 생성하지 않게 전투 설치 때 부릅니다. 정리·해제 중이면 발사와 같이 예외입니다.
        /// </summary>
        internal void Prewarm(int count)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            while (_instances.Count < count)
                _pool.Return(CreateInstance());
        }

        /// <summary>월드 거리와 속도로 비행 시간을 정합니다. 거리가 0인 발사도 다음 Tick에서 완료합니다.</summary>
        internal CombatProjectileLease FireLinear(Vector3 from, Vector3 to, float speed, Action completed)
        {
            ValidatePositive(speed, nameof(speed));
            float distance = ValidatePath(from, to);
            float duration = distance / speed;
            if (!IsFinite(duration))
                throw new ArgumentOutOfRangeException(nameof(speed), "발사체 비행 시간이 유한해야 합니다.");

            return Start(from, to, duration, 0f, Vector3.up, Vector3.zero, completed);
        }

        /// <summary>
        /// 경로 진행률(0~1)을 <paramref name="advanced"/>로 알립니다. 진행률은 완급 곡선을 지난 값이라 경로 위 접촉 지점과 그대로 비교할 수 있습니다.
        /// </summary>
        /// <param name="speed">평균 속도입니다. 비행 시간 = 거리 ÷ 속도이고, 완급 곡선은 그 시간 안의 빠르기만 나눕니다.</param>
        /// <param name="easing">완급 곡선입니다(투사체 완급 설계 EZ1). <c>default</c>는 등속입니다.</param>
        internal CombatProjectileLease FireLinear(Vector3 from, Vector3 to, float speed, Vector3 up,
            Action<float> advanced, Action completed, CombatProjectileEasing easing = default)
        {
            ValidatePositive(speed, nameof(speed));
            float distance = ValidatePath(from, to);
            ValidateVector(up, nameof(up));
            if (!(up.magnitude > 0f) || !IsFinite(up.magnitude)) throw new ArgumentOutOfRangeException(nameof(up));
            float duration = distance / speed;
            if (!IsFinite(duration)) throw new ArgumentOutOfRangeException(nameof(speed));
            return Start(from, to, duration, 0f, up.normalized, Vector3.zero, completed, advanced, easing);
        }

        /// <summary>높이는 월드 길이이며, 곡사 변위는 정규화한 필드 법선 방향으로 적용합니다.</summary>
        /// <param name="easing">완급 곡선입니다(투사체 완급 설계 EZ1). 높이도 같은 진행률을 따라 곡선 모양은 그대로입니다. <c>default</c>는 등속입니다.</param>
        internal CombatProjectileLease FireArc(Vector3 from, Vector3 to, float duration, float height,
            Vector3 up, Vector3 spin, Action completed, CombatProjectileEasing easing = default,
            CombatArcRotationMode rotationMode = CombatArcRotationMode.Spin)
        {
            if (!System.Enum.IsDefined(typeof(CombatArcRotationMode), rotationMode))
                throw new ArgumentOutOfRangeException(nameof(rotationMode));
            ValidatePath(from, to);
            ValidatePositive(duration, nameof(duration));
            if (!IsFinite(height) || height < 0f)
                throw new ArgumentOutOfRangeException(nameof(height));
            ValidateVector(up, nameof(up));
            ValidateVector(spin, nameof(spin));
            float upLength = up.magnitude;
            if (!(upLength > 0f) || !IsFinite(upLength))
                throw new ArgumentOutOfRangeException(nameof(up));

            return Start(from, to, duration, height, up / upLength, spin, completed, easing: easing, rotationMode: rotationMode);
        }

        /// <summary>음수 시간은 0으로 취급합니다. 이번 Tick의 시작 이후 빌린 발사체는 다음 Tick부터 진행합니다.</summary>
        internal void Tick(float deltaTime)
        {
            if (!IsFinite(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (_released || _clearing || _active.Count == 0)
                return;
            if (_ticking)
                throw new InvalidOperationException("[CombatProjectilePool] Tick을 중첩 호출할 수 없습니다.");

            _ticking = true;
            List<Exception> errors = null;
            try
            {
                _tickSnapshot.AddRange(_active.Values);
                for (int i = 0; i < _tickSnapshot.Count; i++)
                {
                    Flight flight = _tickSnapshot[i];
                    if (!IsCurrent(flight))
                        continue;
                    try
                    {
                        Advance(flight, Mathf.Max(0f, deltaTime));
                    }
                    catch (Exception exception)
                    {
                        errors ??= new List<Exception>();
                        errors.Add(exception);
                        TryRecall(flight, errors);
                    }
                }
            }
            finally
            {
                _tickSnapshot.Clear();
                _ticking = false;
            }

            ThrowCleanupErrors(errors, "발사체 진행 중 실패했습니다.");
        }

        /// <summary>이 풀의 현재 대여만 회수합니다. 취소·이전 대여·다른 풀의 토큰은 완료를 알리지 않습니다.</summary>
        internal void Recall(CombatProjectileLease lease)
        {
            if (lease == null || !_active.TryGetValue(lease, out Flight flight))
                return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var cancelSample = flight.Profile.ProjectileCancel.Auto();
#endif
            Close(flight);
            Return(flight.Instance);
        }

        /// <summary>완료 통지를 먼저 무효화하고 모든 활성 표시를 반환합니다. 풀은 다음 발사에서 재사용합니다.</summary>
        internal void Clear()
        {
            if (_released || _clearing)
                return;
            _clearing = true;
            _cancellationVersion++;
            var errors = new List<Exception>();
            try
            {
                Flight[] flights = CloseAll();
                for (int i = 0; i < flights.Length; i++)
                {
                    try
                    {
                        Return(flights[i].Instance);
                    }
                    catch (Exception exception)
                    {
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                _clearing = false;
            }

            ThrowCleanupErrors(errors, "발사체 반환 중 실패했습니다.");
        }

        /// <summary>다시 빌릴 수 없게 한 뒤 활성·보관 인스턴스와 보관 루트를 모두 파괴합니다.</summary>
        internal void Release()
        {
            if (_released)
                return;
            _released = true;
            _cancellationVersion++;
            CloseAll();
            Instance[] instances = _instances.ToArray();
            var errors = new List<Exception>();
            for (int i = 0; i < instances.Length; i++)
            {
                try
                {
                    DestroyObject(instances[i].Object);
                }
                catch (Exception exception)
                {
                    errors.Add(exception);
                }
            }

            _instances.Clear();
            _pool = null;
            GameObject inactiveRoot = _inactiveRoot;
            _inactiveRoot = null;
            try
            {
                DestroyObject(inactiveRoot);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }

            ThrowCleanupErrors(errors, "발사체 최종 해제 중 실패했습니다.");
        }

        private CombatProjectileLease Start(Vector3 from, Vector3 to, float duration, float height,
            Vector3 up, Vector3 spin, Action completed, Action<float> advanced = null, CombatProjectileEasing easing = default,
            CombatArcRotationMode rotationMode = CombatArcRotationMode.Spin)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var profile = ProfilingProfile;
            using var projectileSample = profile.ProjectileBegin.Auto();
#endif

            EnsureCanStart();
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            Vector3 direction = rotationMode == CombatArcRotationMode.FollowPath
                ? CombatShotPath.ArcTangent(from, to, up, height, 0f) : to - from;
            Quaternion rotation = FaceAlong(direction, up, _prefabRotation);
            Instance instance = Rent();
            var lease = new CombatProjectileLease(instance.Object);

            var flight = new Flight
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Profile = profile,
#endif
                Instance = instance,
                Lease = lease,
                From = from,
                To = to,
                Up = up,
                Spin = spin,
                RotationMode = rotationMode,
                StartRotation = rotation,
                Duration = duration,
                Height = height,
                Easing = easing,
                Completed = completed,
                Advanced = advanced
            };
            _active.Add(lease, flight);
            try
            {
                Transform transform = instance.Object.transform;
                transform.SetParent(_root, false);
                EnsureCurrent(flight);
                transform.localScale = _prefabScale;
                transform.SetPositionAndRotation(from, rotation);
                SetActiveSafely(instance.Object, true);
                // 새 자리로 옮긴 뒤 같은 프레임 그리기 전에 꼬리를 비웁니다(평가 G1-03).
                instance.ClearTrails();
                EnsureCurrent(flight);
                return lease;
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };
                TryRecall(flight, errors);
                ThrowCleanupErrors(errors, "발사체 시작 중 실패했습니다.");
                throw;
            }
        }

        private void Advance(Flight flight, float deltaTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var projectileSample = flight.Profile.ProjectileTick.Auto();
#endif

            if (flight.Instance.Object == null)
                throw new InvalidOperationException("[CombatProjectilePool] 활성 발사체가 외부에서 파괴됐습니다.");
            flight.Elapsed = Mathf.Min(flight.Elapsed + deltaTime, flight.Duration);
            float t = flight.Duration > 0f ? flight.Elapsed / flight.Duration : 1f;
            // 완급 곡선은 시간 비율을 경로 진행률로 바꾸는 한 곳입니다(투사체 완급 설계 EZ1). 위치와 진행 알림이 같은 진행률을 쓰고,
            // Spin은 시간으로 회전하고 FollowPath는 같은 경로 진행률의 접선을 향합니다. 끝(t = 1)에서 진행률은 정확히 1입니다.
            float progress = flight.Easing.Evaluate(t);
            // 곡선 식 원본은 발사 기하 한 곳입니다(궤적 미리보기와 공유, 공격 궤적 설계 TJ3). 직선은 높이 0입니다.
            Vector3 position = CombatShotPath.ArcPoint(flight.From, flight.To, flight.Up, flight.Height, progress);
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
                throw new InvalidOperationException("[CombatProjectilePool] 발사체 표시 위치가 유한하지 않습니다.");
            Transform transform = flight.Instance.Object.transform;
            Quaternion rotation;
            if (flight.RotationMode == CombatArcRotationMode.FollowPath)
            {
                Vector3 tangent = CombatShotPath.ArcTangent(flight.From, flight.To, flight.Up, flight.Height, progress);
                rotation = FaceAlong(tangent, flight.Up, transform.rotation);
            }
            else
            {
                Vector3 angles = new(
                    SpinAngle(flight.Spin.x, flight.Elapsed),
                    SpinAngle(flight.Spin.y, flight.Elapsed),
                    SpinAngle(flight.Spin.z, flight.Elapsed));
                rotation = flight.StartRotation * Quaternion.Euler(angles);
            }
            transform.SetPositionAndRotation(position, rotation);
            flight.Advanced?.Invoke(progress);
            if (!IsCurrent(flight) || t < 1f)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var endSample = flight.Profile.ProjectileEnd.Auto();
#endif
            Action completed = flight.Completed;
            long cancellationVersion = _cancellationVersion;
            Close(flight);
            Return(flight.Instance);
            if (!_released && _cancellationVersion == cancellationVersion)
                completed();
        }

        private Quaternion FaceAlong(Vector3 direction, Vector3 up, Quaternion fallback)
        {
            ValidateVector(direction, nameof(direction));
            // 경로가 한 점이거나 수직 곡사의 꼭대기에서 접선이 0이면 마지막 자세를 유지합니다.
            if (direction.sqrMagnitude <= 0f) return fallback;
            Vector3 facingUp = Vector3.Cross(direction, up).sqrMagnitude > 0f
                ? up : Vector3.Cross(direction, Vector3.right).sqrMagnitude > 0f ? Vector3.right : Vector3.forward;
            return Quaternion.LookRotation(direction, facingUp) * _prefabRotation;
        }

        private Instance Rent()
        {
            while (true)
            {
                Instance instance = _pool.Get();
                if (instance.Object != null)
                    return instance;
                _instances.Remove(instance);
            }
        }

        private Instance CreateInstance()
        {
            EnsureCanStart();
            GameObject gameObject = UnityEngine.Object.Instantiate(_prefab, _inactiveRoot.transform, false);
            var instance = new Instance(gameObject);
            _instances.Add(instance);
            try
            {
                if (gameObject == null)
                    throw new InvalidOperationException("[CombatProjectilePool] 프리팹 복제본이 없습니다.");
                SetActiveSafely(gameObject, false);
                EnsureCanStart();
                return instance;
            }
            catch (Exception exception)
            {
                var errors = new List<Exception> { exception };
                TryDestroy(instance, errors);
                ThrowCleanupErrors(errors, "발사체 생성 중 실패했습니다.");
                throw;
            }
        }

        private void Return(Instance instance)
        {
            if (_released)
                return;
            // 투사체가 닿을 때마다 돌아, 실패 목록은 풀에 넣지 못한 길에서만 만듭니다(GC 끊김 설계 GH4).
            List<Exception> errors = null;
            try
            {
                GameObject gameObject = instance.Object;
                if (gameObject != null)
                {
                    SetActiveSafely(gameObject, false);
                    if (_released)
                        return;
                    if (gameObject != null)
                    {
                        Transform transform = gameObject.transform;
                        transform.SetParent(_inactiveRoot.transform, false);
                        if (_released)
                            return;
                        transform.SetLocalPositionAndRotation(Vector3.zero, _prefabRotation);
                        transform.localScale = _prefabScale;
                        _pool.Return(instance);
                        return;
                    }
                }
            }
            catch (Exception exception)
            {
                errors = new List<Exception> { exception };
            }

            errors ??= new List<Exception>();
            TryDestroy(instance, errors);
            ThrowCleanupErrors(errors, "발사체 회수 중 실패했습니다.");
        }

        private void Close(Flight flight)
        {
            _active.Remove(flight.Lease);
            flight.Completed = null;
            flight.Advanced = null;
            flight.Lease.Invalidate();
        }

        private Flight[] CloseAll()
        {
            var flights = new Flight[_active.Count];
            _active.Values.CopyTo(flights, 0);
            _active.Clear();
            for (int i = 0; i < flights.Length; i++)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var cancelSample = flights[i].Profile.ProjectileCancel.Auto();
#endif
                flights[i].Completed = null;
                flights[i].Lease.Invalidate();
            }
            return flights;
        }

        private bool IsCurrent(Flight flight)
            => _active.TryGetValue(flight.Lease, out Flight current) && ReferenceEquals(current, flight);

        private void EnsureCurrent(Flight flight)
        {
            if (!IsCurrent(flight) || flight.Instance.Object == null)
                throw new InvalidOperationException("[CombatProjectilePool] 시작 중 발사체가 취소·해제되거나 파괴됐습니다.");
        }

        private void EnsureCanStart()
        {
            if (_released || _clearing)
                throw new InvalidOperationException("[CombatProjectilePool] 정리·해제 중에는 발사할 수 없습니다.");
            if (_prefab == null || _root == null || _inactiveRoot == null)
                throw new InvalidOperationException("[CombatProjectilePool] 프리팹 또는 표시 루트가 파괴됐습니다.");
        }

        private void TryRecall(Flight flight, List<Exception> errors)
        {
            if (!IsCurrent(flight))
                return;
            try
            {
                Recall(flight.Lease);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        private void TryDestroy(Instance instance, List<Exception> errors)
        {
            try
            {
                DestroyObject(instance.Object);
                _instances.Remove(instance);
            }
            catch (Exception exception)
            {
                errors.Add(exception);
            }
        }

        private void DestroyObject(GameObject gameObject)
        {
            if (gameObject == null)
                return;
            _pendingDestruction.Enqueue(gameObject);
            DrainPendingDestruction();
        }

        /// <summary>가시성 콜백의 Release는 즉시 수명을 닫고, 실제 파괴는 가장 바깥 SetActive 반환 직후 수행합니다.</summary>
        private void SetActiveSafely(GameObject gameObject, bool active)
        {
            _visibilityDepth++;
            Exception visibilityError = null;
            try
            {
                gameObject.SetActive(active);
            }
            catch (Exception exception)
            {
                visibilityError = exception;
            }
            finally
            {
                _visibilityDepth--;
            }

            try
            {
                DrainPendingDestruction();
            }
            catch (Exception exception)
            {
                if (visibilityError != null)
                    throw new InvalidOperationException("[CombatProjectilePool] 가시성 변경과 파괴 중 실패했습니다.",
                        new AggregateException(visibilityError, exception));
                throw;
            }

            if (visibilityError != null)
                throw new InvalidOperationException("[CombatProjectilePool] 가시성 변경 중 실패했습니다.", visibilityError);
        }

        private void DrainPendingDestruction()
        {
            if (_visibilityDepth > 0 || _destroying || _pendingDestruction.Count == 0)
                return;
            _destroying = true;
            List<Exception> errors = null;
            try
            {
                while (_pendingDestruction.Count > 0)
                {
                    GameObject gameObject = _pendingDestruction.Dequeue();
                    try
                    {
                        if (gameObject == null)
                            continue;
                        SetActiveSafely(gameObject, false);
                        if (gameObject == null)
                            continue;
                        if (Application.isPlaying)
                            UnityEngine.Object.Destroy(gameObject);
                        else
                            UnityEngine.Object.DestroyImmediate(gameObject);
                    }
                    catch (Exception exception)
                    {
                        errors ??= new List<Exception>();
                        errors.Add(exception);
                    }
                }
            }
            finally
            {
                _destroying = false;
            }

            ThrowCleanupErrors(errors, "발사체 파괴 중 실패했습니다.");
        }

        private static float ValidatePath(Vector3 from, Vector3 to)
        {
            ValidateVector(from, nameof(from));
            ValidateVector(to, nameof(to));
            float distance = Vector3.Distance(from, to);
            if (!IsFinite(distance))
                throw new ArgumentOutOfRangeException(nameof(to), "발사체 경로의 월드 거리가 유한해야 합니다.");
            return distance;
        }

        private static void ValidatePositive(float value, string name)
        {
            if (!IsFinite(value) || !(value > 0f))
                throw new ArgumentOutOfRangeException(name);
        }

        private static void ValidateVector(Vector3 value, string name)
        {
            if (!IsFinite(value.x) || !IsFinite(value.y) || !IsFinite(value.z))
                throw new ArgumentOutOfRangeException(name);
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float SpinAngle(float speed, float elapsed) => (float)(((double)speed * elapsed) % 360d);

        private static void ThrowCleanupErrors(List<Exception> errors, string message)
        {
            if (errors != null && errors.Count > 0)
                throw new InvalidOperationException("[CombatProjectilePool] " + message, new AggregateException(errors));
        }
    }
}
