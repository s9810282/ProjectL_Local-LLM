using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>맵 배치·숨김만 로딩에 연결합니다. 낙하 완료와 로딩 종료를 기다리지 않습니다.</summary>
    internal sealed class StageMapLoadingPreparation : ILodingPreparation
    {
        private StageMapViewScope _scope;
        private readonly RuntimeFaultBoundary _faults;
        private readonly long _version;
        private bool _started;

        internal StageMapLoadingPreparation(StageMapViewScope scope, RuntimeFaultBoundary faults)
        {
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _version = scope.EntryPreparationVersion;
        }

        public LodingPreparationState State { get; private set; } = LodingPreparationState.Pending;

        public void Begin(bool isCovered)
        {
            if (_started || State != LodingPreparationState.Pending)
                throw new InvalidOperationException("[StageMapLoadingPreparation] 준비는 한 번만 시작할 수 있습니다.");
            _started = true;
            _scope.BeginScenePreparation(_version, isCovered);
        }

        public void Tick(float deltaTime)
        {
            if (State != LodingPreparationState.Pending) return;
            StageMapViewScope scope = _scope;
            try
            {
                if (!_started)
                    throw new InvalidOperationException("[StageMapLoadingPreparation] Begin 뒤에 Tick해야 합니다.");
                if (_faults.IsFaulted) State = LodingPreparationState.Failed;
                else if (scope.EntryPreparationVersion != _version) State = LodingPreparationState.Cancelled;
                else
                {
                    LodingPreparationState result = scope.TryPrepareEntry();
                    if (State != LodingPreparationState.Pending) return;
                    State = result;
                }
            }
            catch (Exception error)
            {
                State = LodingPreparationState.Failed;
                _faults.Report(error);
            }
            finally
            {
                if (State != LodingPreparationState.Pending)
                {
                    _scope = null;
                    if (State != LodingPreparationState.Complete && scope.EntryPreparationVersion == _version)
                    {
                        try { scope.CancelEntryPreparation(); }
                        catch (Exception error) { _faults.Report(error); }
                    }
                }
            }
        }

        public void Cancel()
        {
            if (State != LodingPreparationState.Pending) return;
            State = LodingPreparationState.Cancelled;
            StageMapViewScope scope = _scope;
            _scope = null;
            if (scope.EntryPreparationVersion == _version) scope.CancelEntryPreparation();
        }
    }
}
