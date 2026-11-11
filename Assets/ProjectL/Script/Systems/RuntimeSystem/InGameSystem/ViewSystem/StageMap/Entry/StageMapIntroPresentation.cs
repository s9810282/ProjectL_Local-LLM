using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>씬 연결 한 번의 진입 연출 상태와 유일한 경과 시간을 소유합니다.</summary>
    internal sealed class StageMapIntroPresentation
    {
        private readonly StageMapIntroData _settings;
        private readonly StageMapIntroHistory _history;
        private readonly StageNodeDropPresentation _drops;
        private readonly StageLinkDrawPresentation _links;
        private readonly StageMapCameraTour _camera;
        private State _state = State.Preparing;
        private StageMapIntroTimeline _timeline;
        private Func<bool> _isCurrent;
        private StageNodeDropTarget[] _borrowedDrops = Array.Empty<StageNodeDropTarget>();
        private IStageLinkDrawTarget[] _borrowedLinks = Array.Empty<IStageLinkDrawTarget>();
        private RunId _runId;
        private int _mapKey;
        private long _epoch;
        private double _elapsed;
        private bool _started;

        internal StageMapIntroPresentation(StageMapIntroData settings, StageMapIntroHistory history,
            StageNodeDropPresentation drops, StageLinkDrawPresentation links, StageMapCameraTour camera)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _drops = drops ?? throw new ArgumentNullException(nameof(drops));
            _links = links ?? throw new ArgumentNullException(nameof(links));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _settings.Validate();
        }

        internal bool IsPending => _state == State.Preparing || _state == State.PreparedWaitingForLoading || _state == State.Playing;
        internal bool IsPrepared => _state == State.PreparedWaitingForLoading || _state == State.Playing || _state == State.Complete;
        internal bool WasSkippedAtPreparation { get; private set; }

        internal void BeginPreparation()
        {
            Cancel(true);
            WasSkippedAtPreparation = false;
            _state = State.Preparing;
        }

        internal void Prepare(RunId runId, int mapKey, Transform mapRoot, StageMapIntroTimeline timeline,
            IReadOnlyList<StageNodeDropTarget> dropTargets, IReadOnlyList<IStageLinkDrawTarget> linkTargets,
            IStageMapCameraTourControl cameraControl, Func<bool> isCurrent)
        {
            if (_state != State.Preparing) throw new InvalidOperationException("[StageMapIntroPresentation] 준비 중에만 대상을 인수할 수 있습니다.");
            if (runId.IsEmpty || mapKey <= 0) throw new InvalidOperationException("[StageMapIntroPresentation] 유효한 Run과 맵이 필요합니다.");
            if (mapRoot == null) throw new ArgumentNullException(nameof(mapRoot));
            if (dropTargets == null) throw new ArgumentNullException(nameof(dropTargets));
            if (linkTargets == null) throw new ArgumentNullException(nameof(linkTargets));
            if (cameraControl == null) throw new ArgumentNullException(nameof(cameraControl));
            _timeline = timeline ?? throw new ArgumentNullException(nameof(timeline));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
            _runId = runId;
            _mapKey = mapKey;
            _elapsed = 0d;
            _started = false;
            _borrowedDrops = new List<StageNodeDropTarget>(dropTargets).ToArray();
            _borrowedLinks = new List<IStageLinkDrawTarget>(linkTargets).ToArray();
            long epoch = _epoch;
            try
            {
                if (!CheckCurrent(epoch)) return;
                if (!_settings.Enabled || !timeline.HasTargets || _history.HasStarted(runId, mapKey))
                {
                    Finish(State.Complete, false);
                    WasSkippedAtPreparation = _state == State.Complete;
                    return;
                }
                _drops.Prepare(mapRoot, dropTargets, timeline.Nodes, timeline.EffectiveDropSeconds);
                if (!CheckCurrent(epoch)) return;
                _links.Prepare(linkTargets, timeline.Links);
                if (!CheckCurrent(epoch)) return;
                _camera.Prepare(cameraControl, timeline.PlayerPosition, timeline.BossPosition, timeline.Camera);
                if (!CheckCurrent(epoch)) return;
                _state = State.PreparedWaitingForLoading;
            }
            catch (Exception error)
            {
                Fail(error, epoch);
            }
        }

        internal void Tick(float scaledDeltaTime, bool isLoading, bool isPaused)
        {
            if (!IsPending || _state == State.Preparing) return;
            long epoch = _epoch;
            try
            {
                if (!CheckCurrent(epoch)) return;
                if (isLoading || isPaused) return;
                if (!float.IsFinite(scaledDeltaTime) || scaledDeltaTime < 0f)
                    throw new ArgumentOutOfRangeException(nameof(scaledDeltaTime));
                // 로딩 마지막 프레임의 경과를 재사용하지 않고 첫 표시 시각 0부터 시작합니다.
                if (_state == State.PreparedWaitingForLoading) _state = State.Playing;
                else _elapsed += scaledDeltaTime;
                if (!_started)
                {
                    if (_elapsed < _timeline.FirstChangeSeconds) return;
                    if (!_history.TryMarkStarted(_runId, _mapKey))
                    {
                        Finish(State.Complete, true);
                        return;
                    }
                    _started = true;
                }
                _drops.Tick(_elapsed);
                if (!CheckCurrent(epoch)) return;
                _links.Tick(_elapsed);
                if (!CheckCurrent(epoch)) return;
                _camera.Tick(_elapsed);
                if (!CheckCurrent(epoch)) return;
                if (!_drops.IsComplete || !_links.IsComplete || !_camera.IsComplete) return;
                _state = State.Complete;
                ReleaseReferences();
            }
            catch (Exception error)
            {
                Fail(error, epoch);
            }
        }

        internal void Cancel(bool restoreCamera) => Finish(State.Cancelled, restoreCamera);

        private bool CheckCurrent(long epoch)
        {
            if (_epoch != epoch) return false;
            if (_isCurrent != null && _isCurrent()) return true;
            Cancel(false);
            return false;
        }

        private void Finish(State state, bool restoreCamera)
        {
            WasSkippedAtPreparation = false;
            long epoch = ++_epoch;
            _state = state;
            StageNodeDropTarget[] drops = _borrowedDrops;
            IStageLinkDrawTarget[] links = _borrowedLinks;
            ReleaseReferences();
            List<Exception> errors = null;
            CleanupErrors.Try(_drops.Cancel, ref errors);
            CleanupErrors.Try(_links.Cancel, ref errors);
            foreach (StageNodeDropTarget target in drops) CleanupErrors.Try(target.Restore, ref errors);
            foreach (IStageLinkDrawTarget target in links) CleanupErrors.Try(target.Restore, ref errors);
            CleanupErrors.Try(() => _camera.Cancel(restoreCamera), ref errors);
            if (errors != null && _epoch == epoch) _state = State.Failed;
            CleanupErrors.ThrowIfAny(errors, "[StageMapIntroPresentation] 진입 연출 정리 실패.");
        }

        private void ReleaseReferences()
        {
            _timeline = null;
            _isCurrent = null;
            _borrowedDrops = Array.Empty<StageNodeDropTarget>();
            _borrowedLinks = Array.Empty<IStageLinkDrawTarget>();
        }

        private void Fail(Exception error, long epoch)
        {
            List<Exception> errors = new() { error };
            if (_epoch == epoch) CleanupErrors.Try(() => Finish(State.Failed, true), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMapIntroPresentation] 진입 연출 실패.");
        }

        private enum State
        {
            Preparing,
            PreparedWaitingForLoading,
            Playing,
            Complete,
            Cancelled,
            Failed,
        }
    }
}
