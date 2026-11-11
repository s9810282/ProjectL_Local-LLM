using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 연결 한 번의 맵 진입 준비입니다. 배경·노드·카메라 범위를 그리고 인트로를 준비해 맵을 "보이는" 상태로 만들며,
    /// 준비 문맥(Run·맵·준비 버전)과 그 문맥으로 잡는 이동 문맥을 갖습니다(스테이지 선택 리팩토링 설계 §3.2 Scope 나누기).
    /// 로딩 준비와 일반 Tick이 같은 입구(<see cref="TryPrepare"/>)를 씁니다. 언제 부를지는 Scope가 정합니다.
    /// </summary>
    internal sealed class StageMapViewEntryPreparation
    {
        private readonly StageMapViewServices _services;
        private readonly StageMapViewReader _reader;
        private readonly StageMapViewScene _scene;
        private readonly StageMapViewInput _input;
        private readonly StageMapViewEntry _entry;
        private readonly StageNodeClearPresentation _completion;
        private readonly StageMapIntroPresentation _intro;
        private readonly StageMapCameraFocus _cameraFocus;
        private readonly StageMapViewNoncombatRestore _restore;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<bool> _canWork;
        private readonly Func<bool> _isCurrent;
        private readonly Func<bool> _isStarted;
        private readonly Action _cancelMovements;
        private PreparationOrigin _origin;
        private bool _preparing;
        private RunId _preparedRun;
        private int _preparedMap;

        private enum PreparationOrigin
        {
            AwaitingSceneEntry,
            CoveredSceneEntry,
            UncoveredSceneEntry,
            VisibleRedraw
        }

        /// <param name="isStarted">팝업까지 준비돼 Tick을 받기 시작했는지입니다. 그 전에는 준비를 미룹니다.</param>
        /// <param name="cancelMovements">준비를 취소할 때 이전 이동(출전·노드 이동)의 무효 문맥을 거둡니다.</param>
        internal StageMapViewEntryPreparation(StageMapViewServices services, StageMapViewReader reader,
            StageMapViewScene scene, StageMapViewInput input, StageMapViewEntry entry, StageMapIntroPresentation intro,
            StageMapCameraFocus cameraFocus, StageMapViewNoncombatRestore restore, RuntimeFaultBoundary faults,
            Func<bool> canWork, Func<bool> isCurrent, Func<bool> isStarted, Action cancelMovements,
            StageNodeClearPresentation completion)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _scene = scene ?? throw new ArgumentNullException(nameof(scene));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
            _intro = intro ?? throw new ArgumentNullException(nameof(intro));
            _cameraFocus = cameraFocus ?? throw new ArgumentNullException(nameof(cameraFocus));
            _restore = restore ?? throw new ArgumentNullException(nameof(restore));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
            _isStarted = isStarted ?? throw new ArgumentNullException(nameof(isStarted));
            _cancelMovements = cancelMovements ?? throw new ArgumentNullException(nameof(cancelMovements));
        }

        /// <summary>준비 버전입니다. 취소할 때마다 오르고, 로딩 준비와 이동 문맥이 이것으로 낡은 준비를 가립니다.</summary>
        internal long Version { get; private set; }

        /// <summary>맵을 다 그려 보이는 상태입니다.</summary>
        internal bool IsShown { get; private set; }

        /// <summary>보이는 상태이고, 준비한 Run·맵·버전이 지금과 같습니다.</summary>
        internal bool IsPreparedContext => IsShown && IsEntryContext(_preparedRun, _preparedMap, Version);

        internal StageMapMovementContext CaptureMovementContext()
        {
            if (!IsPreparedContext)
                throw new InvalidOperationException("[StageMap] 이동을 시작할 준비 문맥이 없습니다.");
            return new StageMapMovementContext(_preparedRun, _preparedMap, Version);
        }

        internal bool IsMovementContextCurrent(StageMapMovementContext context) => IsPreparedContext &&
            context.RunId == _preparedRun && context.MapKey == _preparedMap && context.PreparationVersion == Version;

        /// <summary>씬 진입 로딩이 덮개(가림) 여부를 알려 줍니다. 한 준비에 한 번만입니다.</summary>
        internal void BeginScenePreparation(long version, bool isCovered)
        {
            if (!_canWork() || version != Version) return;
            if (_origin != PreparationOrigin.AwaitingSceneEntry)
                throw new InvalidOperationException("[StageMap] 씬 진입 준비 원인은 한 번만 지정할 수 있습니다.");
            _origin = isCovered ? PreparationOrigin.CoveredSceneEntry : PreparationOrigin.UncoveredSceneEntry;
        }

        /// <summary>보이는 맵을 다시 그립니다. 지금 준비를 취소하고 인트로 준비부터 다시 시작합니다.</summary>
        internal void BeginVisibleRedraw()
        {
            Cancel();
            if (!_canWork()) return;
            _origin = PreparationOrigin.VisibleRedraw;
            _intro.BeginPreparation();
            _input.ClearPointer();
        }

        /// <summary>로딩과 일반 Tick이 공유하는 배치·숨김 준비입니다. 재생 완료는 기다리지 않습니다.</summary>
        internal LodingPreparationState TryPrepare()
        {
            if (_faults.IsFaulted) return LodingPreparationState.Failed;
            if (!_canWork() || (!_intro.IsPending && !_intro.IsPrepared)) return LodingPreparationState.Cancelled;
            if (!_isStarted() || _preparing) return LodingPreparationState.Pending;
            if (_origin == PreparationOrigin.AwaitingSceneEntry) return LodingPreparationState.Pending;
            if (IsShown)
            {
                if (IsPreparedContext) return LodingPreparationState.Complete;
                try
                {
                    Cancel();
                }
                catch (Exception error)
                {
                    _faults.Report(error);
                    return LodingPreparationState.Failed;
                }
                return LodingPreparationState.Cancelled;
            }
            return PrepareAttempt();
        }

        /// <summary>취소 상태를 먼저 게시하고 외형·효과를 노드 파괴 전에 정리합니다. 지금 준비 문맥이면 카메라를 되돌립니다.</summary>
        internal void Cancel() => Cancel(_isCurrent() &&
            (_preparedRun.IsEmpty || IsEntryContext(_preparedRun, _preparedMap, Version)));

        internal void Cancel(bool restoreCamera)
        {
            ++Version;
            _origin = PreparationOrigin.AwaitingSceneEntry;
            IsShown = false;
            _preparedRun = default;
            _preparedMap = 0;
            _restore.Clear();
            List<Exception> errors = null;
            CleanupErrors.Try(_completion.Cancel, ref errors);
            CleanupErrors.Try(_cancelMovements, ref errors);
            CleanupErrors.Try(() => _intro.Cancel(restoreCamera), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 준비와 이전 이동 정리 실패.");
        }

        private bool IsEntryContext(RunId runId, int mapKey, long version) => _canWork() &&
            Version == version && !runId.IsEmpty && _services.RunProgress.RunId == runId &&
            _services.Current.HasMap && _services.Current.CurrentMapKey == mapKey;

        /// <summary>배치가 필요한 때만 호출하여 준비 문맥의 캡처와 대상 배열을 만듭니다.</summary>
        private LodingPreparationState PrepareAttempt()
        {
            _preparing = true;
            List<StageNodeDropTarget> targets = null;
            List<IStageLinkDrawTarget> links = null;
            bool adopted = false;
            long version = Version;
            LodingPreparationState result = LodingPreparationState.Pending;
            try
            {
                result = PrepareCurrent();
            }
            catch (Exception error)
            {
                result = LodingPreparationState.Failed;
                _faults.Report(error);
            }
            finally
            {
                try
                {
                    // 정상 Pending은 어댑터가 잡은 버전을 보존하고 이번 시도의 표시만 복원합니다.
                    if (!adopted)
                    {
                        List<Exception> errors = null;
                        if (result != LodingPreparationState.Pending && Version == version)
                            CleanupErrors.Try(Cancel, ref errors);
                        if (targets != null)
                            foreach (StageNodeDropTarget target in targets) CleanupErrors.Try(target.Restore, ref errors);
                        if (links != null)
                            foreach (IStageLinkDrawTarget link in links) CleanupErrors.Try(link.Restore, ref errors);
                        if (errors != null)
                        {
                            result = LodingPreparationState.Failed;
                            _faults.Report(new AggregateException("[StageMap] 준비 외형 복원 실패.", errors));
                        }
                    }
                }
                finally
                {
                    _preparing = false;
                }
            }
            return result;

            LodingPreparationState PrepareCurrent()
            {
                RunId runId = _services.RunProgress.RunId;
                if (runId.IsEmpty || !_services.Current.HasMap) return LodingPreparationState.Pending;
                if (!_reader.TryRead(out int mapKey, out IReadOnlyList<StageProgressData> nodes))
                    return LodingPreparationState.Pending;
                if (!_reader.TryReadCurrentNode(out int initialNodeId))
                    throw new InvalidOperationException("[StageMap] 준비된 맵에 현재 노드 또는 시작 노드가 없습니다.");

                _completion.Prepare(new StageNodeClearKey(runId, mapKey, initialNodeId),
                    _services.Stages.IsCleared(mapKey, initialNodeId), version);
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                _input.ClearPointer();
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                _scene.ShowBackground(mapKey);
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                targets = new List<StageNodeDropTarget>();
                _scene.ShowNodes(mapKey, nodes, initialNodeId, targets);
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                _scene.Display.PlaceCurrentMarker(initialNodeId, _scene.Display.GetRestingLanding(initialNodeId));
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                _entry.SynchronizeCurrentNode(initialNodeId);
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                _input.SetDisplayedChapter(mapKey);
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;

                links = new List<IStageLinkDrawTarget>();
                _scene.CollectLinkDrawTargets(links);
                var positions = new Dictionary<int, Vector3>();
                foreach (StageProgressData node in nodes)
                {
                    if (!_scene.Display.TryGetNode(node.NodeId, out Transform root))
                        throw new InvalidOperationException($"[StageMap] 표시 루트가 없습니다: {node.NodeId}");
                    if (!positions.TryAdd(node.NodeId, root.position))
                        throw new InvalidOperationException($"[StageMap] 표시 ID가 중복됩니다: {node.NodeId}");
                }
                _scene.ConfigureCameraBounds(nodes);
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                if (!_scene.CameraDrag.IsReady) return LodingPreparationState.Pending;
                var dropIds = new List<int>(targets.Count);
                foreach (StageNodeDropTarget target in targets) dropIds.Add(target.NodeId);
                StageMapIntroTimeline timeline = StageMapIntroTimeline.Build(_services.MapData.MapIntro,
                    _services.MapData.NodeDrop, nodes, positions, initialNodeId, _scene.Root.MapRoot.forward, dropIds);
                _intro.Prepare(runId, mapKey, _scene.Root.MapRoot, timeline, targets, links, _scene.CameraDrag,
                    () => IsEntryContext(runId, mapKey, version));
                if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                if (!_intro.IsPrepared) return LodingPreparationState.Cancelled;
                if (_origin == PreparationOrigin.CoveredSceneEntry && _intro.WasSkippedAtPreparation)
                {
                    Vector3 markerPosition = _scene.Root.MapRoot.TransformPoint(
                        _scene.Display.GetMarkerDestination(initialNodeId, _scene.Display.CurrentLanding));
                    _cameraFocus.AlignImmediately(initialNodeId, markerPosition);
                    if (!IsEntryContext(runId, mapKey, version)) return LodingPreparationState.Cancelled;
                }
                adopted = true;
                _preparedRun = runId;
                _preparedMap = mapKey;
                IsShown = true;
                _restore.Request(mapKey);
                return LodingPreparationState.Complete;
            }
        }
    }
}
