using System;
using System.Collections.Generic;
using Cinemachine;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 플레이어가 움직이는 전투 시점입니다(카메라 조작 설계 CC1·CC3·CC4). 팬 상태는 여기서만 소유하고, 자세 사용 맵은 pipeline 어댑터로 적용합니다.
    /// 렌즈(보기 크기)는 카메라 창구(<see cref="CombatCameraWindow.TrySetViewSize"/>)에 맡깁니다. 입력 종류는 모릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 표시 연결마다 Factory가 하나 만들고 표시가 해제 때 <see cref="Restore"/>합니다(계획 P6·P8). 배경은 씬 수명이라 카메라가 남기 때문입니다.
    /// 맵 카메라가 없으면(배경 없는 시험 조립, 카메라 없는 맵) 모든 조작이 아무 일도 하지 않습니다.
    /// </para>
    /// <para>
    /// 이동은 카메라마다 프랍을 세운 순간의 회전으로 오른쪽·위쪽 축을 잡고, 같은 오프셋을 기준 월드 위치에 더합니다(설계 CC3, 계획 P2).
    /// 그래서 Day·Night 카메라가 함께 움직여 낮↔밤 전환에서 튀지 않습니다. 보기 크기는 모든 맵 카메라가 하나를 함께 씁니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatCameraView : ICombatCameraControl
    {
        private readonly CombatCameraWindow _window;
        private readonly CombatMapCamera[] _cameras;
        private readonly CombatCameraSettingsData _settings;
        // 처음 보기 크기 = 자세 모드는 SO ZoomMax, 기존 모드는 프리팹 렌즈값을 제한 안으로 맞춘 값입니다(계획 P4). 이동 속도 비율의 바탕이기도 합니다(P3).
        private readonly float _initialSize;
        private readonly CombatCameraZoomPosePath[] _posePaths;
        private readonly CombatCameraZoomPose[] _poseAdapters;
        private Vector2 _offset;
        private float _size;
        private bool _begun;

        /// <param name="window">렌즈를 바꾸는 카메라 창구입니다. 같은 맵 카메라를 이미 받았어야 합니다.</param>
        /// <param name="cameras">세운 맵 프랍의 카메라 사실입니다. 비어 있으면 조작이 없습니다.</param>
        /// <param name="settings">카메라 조작 값입니다. 맵 카메라가 있으면 필수입니다(운영 DB가 스테이지마다 필수로 검사, 결정 Q-CC8 A).</param>
        internal CombatCameraView(CombatCameraWindow window, IReadOnlyList<CombatMapCamera> cameras, CombatCameraSettingsData settings)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            if (cameras == null) throw new ArgumentNullException(nameof(cameras));
            _cameras = new CombatMapCamera[cameras.Count];
            _posePaths = new CombatCameraZoomPosePath[cameras.Count];
            _poseAdapters = new CombatCameraZoomPose[cameras.Count];
            for (int i = 0; i < _cameras.Length; i++)
                _cameras[i] = cameras[i];
            if (_cameras.Length == 0)
                return;

            _settings = settings ?? throw new InvalidOperationException("[CombatCameraView] 맵 카메라가 있는데 카메라 조작 값이 없습니다(StageViewPropData.cameraSettings).");
            for (int i = 0; i < _cameras.Length; i++)
            {
                if (_cameras[i].Transform == null)
                    throw new ArgumentException($"[CombatCameraView] 맵 카메라 사실에 카메라 오브젝트가 없습니다: {_cameras[i].Name}", nameof(cameras));
                // 보기 크기는 하나를 함께 쓰므로 처음 크기가 같아야 합니다. 다르면 첫 줌에서 한쪽이 튑니다.
                if (!_settings.UseZoomPose && _cameras[i].BaseLens != _cameras[0].BaseLens)
                    throw new InvalidOperationException(
                        $"[CombatCameraView] 맵 카메라의 처음 렌즈값이 다릅니다: {_cameras[0].Name}={_cameras[0].BaseLens}, {_cameras[i].Name}={_cameras[i].BaseLens}");
            }
            if (_settings.UseZoomPose)
            {
                for (int i = 0; i < _cameras.Length; i++)
                {
                    CombatMapCamera camera = _cameras[i];
                    CombatCameraZoomPose.ValidateCamera(camera.Transform.GetComponent<CinemachineVirtualCamera>());
                    _posePaths[i] = CombatCameraZoomPosePath.FromOffsets(new Pose(camera.BasePosition, camera.BaseRotation),
                        _settings.ZoomOutPositionOffset, _settings.ZoomOutRotationOffset,
                        _settings.ZoomInPositionOffset, _settings.ZoomInRotationOffset, _settings.ZoomMin, _settings.ZoomMax);
                }
            }
            _initialSize = _settings.UseZoomPose
                ? _settings.ZoomMax
                : CombatCameraLimits.ClampSize(_cameras[0].BaseLens, _settings);
            _size = _initialSize;
        }

        private bool HasCameras => _cameras.Length > 0;

        /// <inheritdoc />
        public float EdgeThickness => HasCameras ? _settings.EdgeThickness : 0f;

        /// <inheritdoc />
        public float ZoomSeconds => HasCameras ? _settings.ZoomSeconds : 0f;

        /// <summary>
        /// 연결 때 시점을 처음으로 맞춥니다(설계 CC6). 카메라를 기준 위치에 두고, 처음 렌즈값이 제한 밖이면 제한 안으로 바로 옮깁니다(계획 P4).
        /// </summary>
        internal void Begin()
        {
            if (!HasCameras)
                return;
            if (_begun)
                throw new InvalidOperationException("[CombatCameraView] 시점이 이미 연결됐습니다.");
            // 모든 기존 어댑터를 검사한 다음에만 추가·연결합니다.
            if (_settings.UseZoomPose)
            {
                if (!_window.CanZoom)
                    throw new InvalidOperationException("[CombatCameraView] 자세 줌 연결에 사용 가능한 카메라 효과 서비스가 필요합니다.");
                for (int i = 0; i < _cameras.Length; i++)
                {
                    _poseAdapters[i] = _cameras[i].Transform.GetComponent<CombatCameraZoomPose>();
                    if (_poseAdapters[i] != null && _poseAdapters[i].IsBound)
                        throw new InvalidOperationException($"[CombatCameraView] 이전 자세 연결이 남았습니다: {_cameras[i].Name}");
                }
            }
            _offset = Vector2.zero;
            _size = _initialSize;
            _begun = true;
            try
            {
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if (!_settings.UseZoomPose)
                        continue;
                    if (_poseAdapters[i] == null)
                        _poseAdapters[i] = _cameras[i].Transform.gameObject.AddComponent<CombatCameraZoomPose>();
                    _poseAdapters[i].Bind(_posePaths[i]);
                }
                Place();
                if (_settings.UseZoomPose || _initialSize != _cameras[0].BaseLens)
                    _window.TrySetViewSize(_initialSize, 0f);
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(Restore, ref errors);
                CleanupErrors.ThrowIfAny(errors, "[CombatCameraView] 시점 연결 실패 및 복구 오류입니다.");
                throw;
            }
        }

        /// <summary>
        /// 이번 프레임의 이동을 더합니다(설계 CC3). 이동량 = (키 방향 × 키 속도 + 가장자리 방향 × 가장자리 속도) × 시간 × (보기 크기 ÷ 처음 크기).
        /// 방향은 길이가 1을 넘으면 1로 줄입니다(대각선). 오프셋은 제한 안으로 맞춥니다.
        /// </summary>
        /// <param name="keyDirection">키 이동 방향입니다(x = 오른쪽, y = 위).</param>
        /// <param name="edgeDirection">화면 가장자리 이동 방향입니다(x = 오른쪽, y = 위).</param>
        /// <param name="deltaTime">지난 프레임 뒤의 초입니다. 0 이하면 움직이지 않습니다.</param>
        /// <returns>실제로 움직였으면 true입니다(입력이 포인터를 다시 집는 기준, 평가 C10-01).</returns>
        public bool Pan(Vector2 keyDirection, Vector2 edgeDirection, float deltaTime)
        {
            if (!HasCameras || !(deltaTime > 0f))
                return false;
            Vector2 velocity = Vector2.ClampMagnitude(keyDirection, 1f) * _settings.KeyPanSpeed
                + Vector2.ClampMagnitude(edgeDirection, 1f) * _settings.EdgePanSpeed;
            if (velocity == Vector2.zero)
                return false;
            // 크게 볼수록 더 빨리 움직여, 화면에서 느끼는 속도가 줌과 무관하게 같습니다(계획 P3).
            Vector2 offset = CombatCameraLimits.ClampOffset(_offset + velocity * (deltaTime * (_size / _initialSize)), _settings);
            if (offset == _offset)
                return false;
            _offset = offset;
            Place();
            return true;
        }

        /// <summary>
        /// 보기 크기를 휠 칸 수만큼 바꿉니다(설계 CC4). 양수 = 줌인(크기가 작아짐), 음수 = 줌아웃입니다(계획 P1).
        /// 줌 기준점은 화면 가운데입니다(결정 Q-CC4 A). 제한 끝이면 아무 일도 하지 않습니다.
        /// 연출 줌 대여 중이거나 효과 서비스가 없으면 창구가 거절하고 그 입력은 버립니다(결정 Q-CC6 A).
        /// </summary>
        /// <returns>보기 크기를 바꿨으면 true입니다.</returns>
        public bool Zoom(int steps)
        {
            if (!HasCameras || steps == 0)
                return false;
            float target = CombatCameraLimits.ClampSize(_size - steps * _settings.ZoomStep, _settings);
            if (target == _size || !_window.TrySetViewSize(target, _settings.ZoomSeconds))
                return false;
            _size = target;
            return true;
        }

        /// <summary>
        /// 카메라를 기준 위치·기준 렌즈값으로 바로 되돌립니다(설계 CC6, 계획 P6). 표시 해제가 창구의 맵을 비우기 전에 부릅니다.
        /// 정리라 한 카메라가 실패해도 나머지를 계속하고 끝에 모아 던집니다. 여러 번 불려도 안전합니다.
        /// </summary>
        internal void Restore()
        {
            if (!HasCameras || !_begun)
                return;
            _begun = false;
            _offset = Vector2.zero;
            _size = _initialSize;
            List<Exception> errors = null;
            for (int i = 0; i < _cameras.Length; i++)
            {
                CombatMapCamera camera = _cameras[i];
                if (_poseAdapters[i] != null)
                    CleanupErrors.Try(_poseAdapters[i].Unbind, ref errors);
                CleanupErrors.Try(() => camera.Transform.SetPositionAndRotation(camera.BasePosition, camera.BaseRotation), ref errors);
            }
            CleanupErrors.Try(_window.RestoreView, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatCameraView] 카메라를 처음 위치·크기로 되돌리지 못했습니다.");
        }

        /// <summary>자세 어댑터에 팬을 전달하거나 기존 맵 카메라를 기준 위치에서 자기 화면 축으로 옮깁니다.</summary>
        private void Place()
        {
            var local = new Vector3(_offset.x, _offset.y, 0f);
            for (int i = 0; i < _cameras.Length; i++)
            {
                CombatMapCamera camera = _cameras[i];
                if (_settings.UseZoomPose)
                    _poseAdapters[i].SetOffset(_offset);
                else
                    camera.Transform.position = camera.BasePosition + camera.BaseRotation * local;
            }
        }
    }
}
