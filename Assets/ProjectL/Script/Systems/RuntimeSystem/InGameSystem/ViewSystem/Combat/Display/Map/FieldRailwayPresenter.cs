using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Railway.Item;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 받은 레일 칸과 Mapper로 선로를 설치하고, 발동 사건의 열차·그래프·이펙트 수명을 관리합니다.
    /// View 소유 인스턴스만 생성·제거하며, 발동과 피해 규칙은 판단하지 않습니다.
    /// </summary>
    internal sealed class FieldRailwayPresenter
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profilePlaybackSystemRailwayBegin = new("Combat.Action.Playback.System.Railway.Begin");
        private static readonly Unity.Profiling.ProfilerMarker _profilePlaybackSystemRailwayTick = new("Combat.Action.Playback.System.Railway.Tick");
        private static readonly Unity.Profiling.ProfilerMarker _profilePlaybackSystemRailwayEnd = new("Combat.Action.Playback.System.Railway.End");
        private static readonly Unity.Profiling.ProfilerMarker _profilePlaybackSystemRailwayCancel = new("Combat.Action.Playback.System.Railway.Cancel");
#endif

        private readonly Transform _cellsRoot;
        private readonly RailwayVisualData _config;
        private readonly IEffectSystemServices _effects;
        private readonly List<TrackVisual> _trains = new();
        private readonly List<IReadOnlyList<RailwaySegmentVisual>> _segmentTracks = new();
        private readonly HashSet<CellCoord> _railCells = new();
        private EffectScope _effectScope;
        private Action _completed;
        private Action<float, float> _advanced;
        private float _elapsed;
        private bool _isTicking;
        private int _version;
        private bool _isCleaning;
        private bool _isStarting;
        private bool _clearRequested;
        private bool _isClearing;
        private bool _hasUnreturnedEffects;
        private GameObject _installation;
        private bool _isReleased;

        /// <summary>설치 시 확정한 경로입니다. 이동과 예약 계산이 같은 값을 사용합니다.</summary>
        internal IReadOnlyList<RailwayTravelPath> Paths { get; private set; } = Array.Empty<RailwayTravelPath>();

        /// <summary>직선별 첫 외부 끝부터 정렬한 표시 조각입니다. 설치·해제를 반영하는 읽기 전용 목록입니다.</summary>
        internal IReadOnlyList<IReadOnlyList<RailwaySegmentVisual>> SegmentTracks { get; }

        internal FieldRailwayPresenter(Transform cellsRoot, RailwayVisualData config, IEffectSystemServices effects)
        {
            _cellsRoot = cellsRoot != null ? cellsRoot : throw new ArgumentNullException(nameof(cellsRoot));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
            SegmentTracks = _segmentTracks.AsReadOnly();
        }

        internal void Install(FieldGridMapper mapper, IReadOnlyList<CellCoord> railCells)
        {
            if (_isReleased || _isStarting || _isCleaning || _isClearing || _hasUnreturnedEffects)
                throw new InvalidOperationException("[FieldRailwayPresenter] 해제됐거나 기차 시작·정리 중이거나 이펙트를 다 돌려받지 못한 표시에는 설치할 수 없습니다.");
            Clear();
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            RailwayTrackLayout layout = RailwayTrackLayout.Create(mapper.ColumnCount, mapper.RowCount, railCells);
            foreach (CellCoord cell in railCells)
                _railCells.Add(cell);
            _config.Validate();
            if (layout.Tracks.Count == 0)
                return;

            try
            {
                // 설치가 끝날 때까지 숨긴다. 부분 설치 실패도 이 루트 하나를 정리하면 제거된다.
                _installation = new GameObject("FieldRailway");
                _installation.SetActive(false);
                _installation.transform.SetParent(_cellsRoot, false);
                float width = mapper.TotalWidth / mapper.ColumnCount;
                float length = mapper.TotalLength / mapper.RowCount;
                for (int i = 0; i < layout.Tracks.Count; i++)
                {
                    RailwayTrackLayout.Track track = layout.Tracks[i];
                    float along = track.IsVertical ? length : width;
                    float across = track.IsVertical ? width : length;
                    Vector3 step = (track.IsVertical ? Vector3.forward : Vector3.right) * along;
                    Quaternion rotation = Quaternion.Euler(0f, track.IsVertical ? 0f : 90f, 0f);
                    var segments = new List<RailwaySegmentVisual>();
                    Vector3 first = mapper.CellCenterToLocal(track.GetStart());
                    Vector3 last = mapper.CellCenterToLocal(track.GetEnd());
                    // 표시 연장 개수만 사용합니다. 열차의 바깥 이동 여유는 AddTrain이 따로 사용합니다.
                    for (int extra = _config.RailExtensionCells; extra > 0; extra--)
                        segments.Add(AddSegment(mapper, first - step * extra, rotation));
                    foreach (CellCoord cell in track.Cells)
                        segments.Add(AddSegment(mapper, mapper.CellCenterToLocal(cell), rotation));
                    for (int extra = 1; extra <= _config.RailExtensionCells; extra++)
                        segments.Add(AddSegment(mapper, last + step * extra, rotation));

                    _segmentTracks.Add(segments.AsReadOnly());
                    AddTrain(mapper, track, i, across, rotation);
                }
                var paths = new RailwayTravelPath[_trains.Count];
                for (int i = 0; i < paths.Length; i++) paths[i] = _trains[i].Path;
                Paths = Array.AsReadOnly(paths);
                _installation.SetActive(true);
            }
            catch (Exception error)
            {
                try { Clear(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
        }

        /// <summary>진입 때 설치 표시만 억제합니다. 경로 준비와 열차의 개별 활성 상태는 유지합니다.</summary>
        internal void SetEntryVisible(bool visible)
        {
            // 선로 없는 맵·설치 전·해제 뒤는 보여 줄 설치물이 없는 정상 경로입니다.
            if (_installation != null) _installation.SetActive(visible);
        }

        private RailwaySegmentVisual AddSegment(FieldGridMapper mapper, Vector3 center, Quaternion rotation)
        {
            Transform tile = _config.RailTilePrefab != null
                ? AddStatic(_config.RailTilePrefab, "RailTile", mapper, center, rotation, _config.RailTileYOffset)
                : null;
            Transform rail = AddStatic(_config.RailPrefab, "Rail", mapper, center, rotation, _config.RailYOffset);
            return new RailwaySegmentVisual(tile, rail);
        }

        private Transform AddStatic(GameObject prefab, string name, FieldGridMapper mapper, Vector3 center,
            Quaternion rotation, float offset)
        {
            // 프리팹의 로컬 스케일을 유지하고 배치 위치와 방향만 적용합니다.
            GameObject instance = Object.Instantiate(prefab, _installation.transform, false);
            instance.name = name;
            instance.transform.position = mapper.LocalToWorld(center + Vector3.up * offset);
            instance.transform.localRotation = rotation;
            DisableColliders(instance);
            instance.SetActive(true);
            return instance.transform;
        }

        private void AddTrain(FieldGridMapper mapper, RailwayTrackLayout.Track track, int index,
            float across, Quaternion rotation)
        {
            var movement = new GameObject($"RailwayTrain_{index}");
            movement.SetActive(false);
            movement.transform.SetParent(_installation.transform, false);
            movement.transform.position = mapper.CellCenterToWorld(track.GetStart(_config.Reverse));
            movement.transform.localRotation = rotation * Quaternion.Euler(0f, _config.Reverse ? 180f : 0f, 0f);
            var correction = new GameObject("ModelCorrection");
            correction.transform.SetParent(movement.transform, false);
            correction.transform.localRotation = Quaternion.Euler(0f, _config.TrainModelYaw, 0f);
            correction.transform.localScale = Vector3.one * (_config.TrainWidthInCells * across);
            // Head + Body 칸을 같은 보정 루트 아래에 잇는다. 경로·앞면은 Head 기준 그대로다(칸 분리 설계 TC4).
            RailwayTrainVisualBinding binding = RailwayTrainCars.Build(correction.transform, _config);
            DisableColliders(correction);
            // 설치 때는 숨겨 두고 Animator 평가를 막는다. Begin에서만 수동 그래프를 시작한다.
            if (binding.Animator != null)
                binding.Animator.enabled = false;
            binding.gameObject.SetActive(true);
            Vector3 direction = track.IsVertical ? Vector3.forward : Vector3.right;
            float pitch = track.IsVertical ? mapper.TotalLength / mapper.RowCount : mapper.TotalWidth / mapper.ColumnCount;
            CellCoord first = track.GetStart();
            CellCoord last = track.GetEnd();
            bool firstBoundary = track.IsVertical ? first.Y == 0 : first.X == 0;
            bool lastBoundary = track.IsVertical ? last.Y == mapper.RowCount - 1 : last.X == mapper.ColumnCount - 1;
            float padding = _config.OutsideCells + _config.TravelPaddingCells;
            Vector3 start = mapper.CellCenterToLocal(first) - direction * (pitch * (0.5f + (firstBoundary ? padding : 0f)));
            Vector3 end = mapper.CellCenterToLocal(last) + direction * (pitch * (0.5f + (lastBoundary ? padding : 0f)));
            Vector3 initial = _config.Reverse ? end : start;
            Vector3 frontOffset = _cellsRoot.InverseTransformVector(binding.FrontContact.position - movement.transform.position);
            var path = new RailwayTravelPath(track.Cells, initial, _config.Reverse ? start : end,
                initial + frontOffset, _config.Duration);
            _trains.Add(new TrackVisual(movement.transform, binding, path));
        }

        /// <summary>설치 집합과 같은 사건 스냅샷을 재생합니다. 피해 대상 목록은 사용하지 않습니다.</summary>
        /// <param name="advanced">이동 직후·정리 전의 이전/현재 경과 시간입니다. 시작 시 (0, 0)을 한 번 전달합니다.</param>
        internal void Begin(RailwayGimmickActivationSnapshot activation, Action completed, Action<float, float> advanced = null)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePlaybackSystemRailwayBeginSample = _profilePlaybackSystemRailwayBegin.Auto();
#endif

            if (activation == null) throw new ArgumentNullException(nameof(activation));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (_isReleased || _isClearing || _isCleaning || _isStarting || _hasUnreturnedEffects || _installation == null || _completed != null)
                throw new InvalidOperationException("[FieldRailwayPresenter] 설치·재생 수명이 올바르지 않습니다.");
            var cells = new HashSet<CellCoord>();
            foreach (CellCoord cell in activation.AffectedRailCells.Span)
                if (!cells.Add(cell))
                    throw new InvalidOperationException($"[FieldRailwayPresenter] 발동 칸이 중복됩니다: ({cell.X},{cell.Y})");
            if (!cells.SetEquals(_railCells))
                throw new InvalidOperationException("[FieldRailwayPresenter] 발동 칸 집합이 설치한 레일과 다릅니다.");

            _elapsed = 0f;
            _version++;
            int version = _version;
            _completed = completed;
            _advanced = advanced;
            _isStarting = true;
            try
            {
                // Scope는 발동마다 새로 받지 않고 이 표시에서 하나를 다시 씁니다(평가 C9-03). EffectSystem은 받은 Scope를 Shutdown까지 쥡니다.
                if (_config.Effect != EffectType.Empty)
                    _effectScope ??= _effects.GetEffectHandle()
                        ?? throw new InvalidOperationException("[FieldRailwayPresenter] EffectScope가 없습니다.");
                foreach (TrackVisual train in _trains)
                {
                    if (_version != version) break;
                    train.Begin(_config);
                    if (_version == version)
                        _effectScope?.Play(new EffectScopeRequest(_config.Effect, train.Movement, Vector3.zero, Quaternion.identity));
                }
                // 시작부터 겹친 대상도 같은 재생 시계의 0초로 전달합니다.
                if (_version == version) _advanced?.Invoke(0f, 0f);
            }
            catch (Exception error)
            {
                _isStarting = false;
                try { Cancel(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
            finally { _isStarting = false; }
            if (_clearRequested)
            {
                _clearRequested = false;
                Clear();
            }
            else if (_version != version)
                Cancel();
        }

        internal void Tick(float deltaTime)
        {
            if (_completed == null || _isCleaning || _isStarting)
                return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePlaybackSystemRailwayTickSample = _profilePlaybackSystemRailwayTick.Auto();
#endif
            if (_isTicking) throw new InvalidOperationException("열차 Tick을 중첩 호출할 수 없습니다.");
            Action completed = null;
            int version = _version;
            _isTicking = true;
            try
            {
                if (!float.IsFinite(deltaTime) || deltaTime < 0f)
                    throw new ArgumentOutOfRangeException(nameof(deltaTime));
                float previous = _elapsed;
                float duration = Paths[0].Duration;
                _elapsed = (float)Math.Min(duration, (double)_elapsed + deltaTime);
                foreach (TrackVisual train in _trains)
                    train.Evaluate(_config, _elapsed, _elapsed - previous);
                _advanced?.Invoke(previous, _elapsed);
                // 시간 전달 안에서 취소·해제·새 발동이 일어났으면 이전 Tick은 더 진행하지 않습니다.
                if (_version != version || _isReleased || _isClearing) return;
                if (_elapsed < duration)
                    return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var endSample = _profilePlaybackSystemRailwayEnd.Auto();
#endif
                completed = _completed;
                _completed = null;
                _advanced = null;
                CleanupPlayback();
            }
            catch (Exception error)
            {
                try { if (_version == version) Cancel(); }
                catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
                throw;
            }
            finally { _isTicking = false; }
            // 완료 콜백의 재진입/실패는 새 사건을 취소하는 catch 안에 넣지 않는다.
            if (_version == version && !_isReleased && !_isClearing)
                completed?.Invoke();
        }

        internal void Cancel()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var cancelSample = _completed != null ? _profilePlaybackSystemRailwayCancel.Auto() : default;
#endif

            _version++;
            _completed = null;
            _advanced = null;
            _elapsed = 0f;
            CleanupPlayback();
        }

        private void CleanupPlayback()
        {
            if (_isCleaning || _isStarting) return;
            _isCleaning = true;
            var scope = _effectScope;
            List<Exception> errors = null;
            try
            {
                try { scope?.StopAll(); }
                catch (Exception error)
                {
                    // StopAll이 중간에 실패한 Scope는 다시 쓰지 않는다(이미 돌려준 노드를 두 번 돌려줄 수 있음, 평가 C9-03).
                    _effectScope = null;
                    // 반환 실패 자식이 붙은 앵커는 직접 Destroy하지 않는다(FW-011).
                    _hasUnreturnedEffects = true;
                    (errors ??= new List<Exception>()).Add(error);
                }
                foreach (TrackVisual train in _trains)
                {
                    try { train.StopAnimation(); }
                    catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                    try { if (train.Movement != null) train.Movement.gameObject.SetActive(false); }
                    catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
                }
            }
            finally { _isCleaning = false; }
            if (_clearRequested)
            {
                _clearRequested = false;
                try { Clear(); }
                catch (Exception error) { (errors ??= new List<Exception>()).Add(error); }
            }
            if (errors != null)
                throw new AggregateException("[FieldRailwayPresenter] 열차 연출 정리에 실패했습니다.", errors);
        }

        /// <summary>설치 때 캐시한 한 경로의 표시와 사건 소유 그래프입니다.</summary>
        private sealed class TrackVisual
        {
            internal readonly Transform Movement;
            internal RailwayTravelPath Path { get; }
            private readonly RailwayTrainVisualBinding _binding;
            private readonly Transform _animationRoot;
            private readonly Vector3 _idlePosition;
            private readonly Quaternion _idleRotation;
            private readonly Vector3 _idleScale;
            private PlayableGraph _graph;
            private AnimationClipPlayable _clip;
            private bool _usesClip;

            internal TrackVisual(Transform movement, RailwayTrainVisualBinding binding, RailwayTravelPath path)
            {
                Movement = movement;
                _binding = binding;
                Path = path;
                _animationRoot = binding.Animator != null ? binding.Animator.transform : binding.ModelRoot;
                _idlePosition = _animationRoot.localPosition;
                _idleRotation = _animationRoot.localRotation;
                _idleScale = _animationRoot.localScale;
            }

            internal void Begin(RailwayVisualData config)
            {
                Movement.localPosition = Path.Start;
                Movement.gameObject.SetActive(true);
                if (config.Clip == null && config.Controller == null) return;
                Animator animator = _binding.Animator;
                animator.applyRootMotion = false;
                animator.fireEvents = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.runtimeAnimatorController = null;
                animator.enabled = true;
                _graph = PlayableGraph.Create("RailwayTrainAnimation");
                _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(_graph, "Train", animator);
                _usesClip = config.Clip != null;
                if (_usesClip)
                {
                    if (!float.IsFinite(config.Clip.length) || config.Clip.length <= 0f)
                        throw new InvalidOperationException("[FieldRailwayPresenter] Clip 길이는 양수여야 합니다.");
                    _clip = AnimationClipPlayable.Create(_graph, config.Clip);
                    _clip.SetSpeed(0);
                    _clip.SetApplyFootIK(false);
                    _clip.SetApplyPlayableIK(false);
                    output.SetSourcePlayable(_clip);
                    _graph.Play();
                    EvaluateClip(0);
                }
                else
                {
                    output.SetSourcePlayable(AnimatorControllerPlayable.Create(_graph, config.Controller));
                    _graph.Play();
                    _graph.Evaluate(0);
                    RestoreRoot();
                }
            }

            private void EvaluateClip(double time)
            {
                _clip.SetTime(time);
                _graph.Evaluate(0);
                RestoreRoot();
            }

            internal void Evaluate(RailwayVisualData config, float elapsed, float deltaTime)
            {
                if (_graph.IsValid())
                {
                    if (_usesClip)
                    {
                        EvaluateClip(config.Clip.length * (elapsed / Path.Duration));
                    }
                    else
                    {
                        _graph.Evaluate(deltaTime);
                        RestoreRoot();
                    }
                }
                Movement.localPosition = Path.PositionAt(elapsed);
            }

            private void RestoreRoot()
            {
                _animationRoot.localPosition = _idlePosition;
                _animationRoot.localRotation = _idleRotation;
                _animationRoot.localScale = _idleScale;
            }

            internal void StopAnimation()
            {
                try { if (_graph.IsValid()) _graph.Destroy(); }
                finally
                {
                    _graph = default;
                    _usesClip = false;
                    if (_binding.Animator != null) _binding.Animator.enabled = false;
                    if (_animationRoot != null) RestoreRoot();
                }
            }
        }

        private static void DisableColliders(GameObject instance)
        {
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
        }

        internal void Clear()
        {
            if (_isClearing) return;
            if (_isCleaning || _isStarting)
            {
                _version++;
                _completed = null;
                _advanced = null;
                _clearRequested = true;
                return;
            }
            _isClearing = true;
            Exception cleanup = null;
            try
            {
                try { Cancel(); }
                catch (Exception error) { cleanup = error; }
                GameObject installation = _installation;
                _installation = null;
                _trains.Clear();
                _segmentTracks.Clear();
                Paths = Array.Empty<RailwayTravelPath>();
                _railCells.Clear();
                if (installation != null)
                {
                    installation.SetActive(false);
                    if (_hasUnreturnedEffects)
                    {
                        // 반환 실패한 대여 자원은 Render 소유다. 씬에서 숨긴 앵커를 보존하고 오류로 중단한다.
                        cleanup ??= new InvalidOperationException("[FieldRailwayPresenter] 이펙트 반환 실패로 앵커를 보존했습니다.");
                    }
                    else
                    {
                        installation.transform.SetParent(null, false);
                        if (Application.isPlaying) Object.Destroy(installation);
                        else Object.DestroyImmediate(installation);
                    }
                }
            }
            finally { _isClearing = false; }
            if (cleanup != null) throw cleanup;
        }

        internal void Release()
        {
            if (_isReleased)
                return;
            _isReleased = true;
            Clear();
        }
    }
}
