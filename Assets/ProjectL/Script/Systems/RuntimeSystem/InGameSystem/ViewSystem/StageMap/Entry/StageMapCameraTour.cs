using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>공통 예약 시각으로 전진·정지·복귀 진행률만 계산합니다.</summary>
    internal sealed class StageMapCameraTour
    {
        private IStageMapCameraTourControl _camera;
        private StageMapIntroTimeline.CameraTiming _timing;
        private long _generation;
        private long _epoch;
        private bool _restoreOnCancel;
        internal bool IsComplete { get; private set; } = true;

        internal void Prepare(IStageMapCameraTourControl camera, Vector3 playerPosition,
            Vector3 bossPosition, StageMapIntroTimeline.CameraTiming timing)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            Cancel(true);
            if (!camera.IsReady) throw new InvalidOperationException("[StageMapCameraTour] 카메라 준비가 먼저 필요합니다.");
            long epoch = _epoch;
            _camera = camera;
            _timing = timing;
            IsComplete = false;
            long generation = camera.BeginTour(playerPosition, bossPosition);
            if (_epoch != epoch)
            {
                camera.EndTour(generation, _restoreOnCancel);
                return;
            }
            _generation = generation;
        }

        internal void Tick(double elapsedSeconds)
        {
            if (IsComplete || _camera == null || elapsedSeconds < _timing.RevealStartSeconds) return;
            if (elapsedSeconds >= _timing.EndSeconds)
            {
                Cancel(true);
                return;
            }
            float progress;
            if (elapsedSeconds < _timing.RevealEndSeconds)
            {
                progress = (float)((elapsedSeconds - _timing.RevealStartSeconds) /
                    (_timing.RevealEndSeconds - _timing.RevealStartSeconds));
            }
            else if (elapsedSeconds < _timing.ReturnStartSeconds)
            {
                progress = 1f;
            }
            else
            {
                double u = (elapsedSeconds - _timing.ReturnStartSeconds) /
                    (_timing.EndSeconds - _timing.ReturnStartSeconds);
                progress = (float)(1d - (3d * u * u - 2d * u * u * u));
            }
            _camera.SetTourProgress(_generation, progress);
        }

        internal void Cancel(bool restoreStart)
        {
            long epoch = ++_epoch;
            _restoreOnCancel = restoreStart;
            IStageMapCameraTourControl camera = _camera;
            long generation = _generation;
            _camera = null;
            _generation = 0;
            IsComplete = false;
            camera?.EndTour(generation, restoreStart);
            if (_epoch == epoch) IsComplete = true;
        }
    }
}
