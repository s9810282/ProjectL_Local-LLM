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
    /// 플레이어가 움직이는 전투 시점입니다(카메라 조작 설계 CC1·CC3·CC4). 팬 상태는 여기서만 소유하고, 자세 줌은 pipeline 어댑터로 적용합니다.
    /// 렌즈(보기 크기)는 카메라 창구(<see cref="CombatCameraWindow.TrySetViewSize"/>)에 맡깁니다. 입력 종류는 모릅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 표시 연결마다 Factory가 하나 만들고 표시가 해제 때 <see cref="Restore"/>합니다(계획 P6·P8). 공용 카메라는 씬 수명이라 표시보다 오래 남습니다.
    /// 연결된 카메라가 없으면(카메라 없는 맵) 모든 조작이 아무 일도 하지 않습니다.
    /// </para>
    /// <para>
    /// 이동은 카메라마다 연결 전의 회전으로 오른쪽·위쪽 축을 잡고, 같은 오프셋을 기준 월드 위치에 더합니다(설계 CC3, 계획 P2).
    /// 운영은 공용 카메라 하나를 넘기며, 보기 크기·연출 줌과 팬의 기존 계산을 유지합니다.
    /// </para>
    /// <para>
    /// 회전은 위 축 방위각 하나입니다(카메라 회전 설계 CR1·CR3). 중심은 화면 가운데가 보는 바닥 점이고 내려다보는 각은 그대로입니다.
    /// 이동 오프셋·제한은 처음 카메라 화면 축 그대로라 바닥에 고정된 사각형이 되고, 돌린 뒤의 이동 입력만 그 축으로 바꿉니다(<see cref="CombatCameraOrbit.ToBasePan"/>).
    /// 방위각 0이면 회전 전과 같은 수가 나옵니다(회전 계획 TP-4).
    /// </para>
    /// </remarks>
    internal sealed class CombatCameraView : ICombatCameraControl
    {
        private readonly CombatCameraWindow _window;
        private readonly CombatCameraSnapshot[] _cameras;
        private CombatCameraSettingsData _settings;
        // 처음 보기 크기 = 자세 모드는 SO ZoomMax, 기존 모드는 프리팹 렌즈값을 제한 안으로 맞춘 값입니다(계획 P4). 이동 속도 비율의 바탕이기도 합니다(P3).
        private float _initialSize;
        private CombatCameraZoomPosePath[] _posePaths;
        private readonly CombatCameraZoomPose[] _poseAdapters;
        // 회전 중심을 찾을 바닥 평면의 월드 높이(필드 Cells Root)와, 이동 기준 축의 내려다보는 정도(sin)입니다(회전 계획 TP-2·TP-11).
        private readonly float _groundY;
        private float _panPitchSin;
        // 이동 기준 축의 바닥 위 오른쪽·앞 방향입니다. 회전 중심 보정이 바닥 이동을 오프셋으로 바꿀 때 씁니다(검토 L1).
        private Vector3 _panFlatRight;
        private Vector3 _panFlatAhead;
        // 자세 모드에서 지금 렌즈 크기를 읽을 첫 카메라입니다(연출 줌 포함 실제 크기, 검토 L1). 기존 모드는 null입니다.
        private CinemachineVirtualCamera _lensCamera;
        private Vector2 _offset;
        private float _size;
        // 위 축 방위각(도, −180~180)입니다. 0 = 처음 방향입니다(회전 설계 CR1).
        private float _yaw;
        private float _remainingKeyRotation;
        private float _rotationSpan;
        private float _rotationProgress;
        private bool _begun;

        /// <param name="window">렌즈를 바꾸는 카메라 창구입니다. 같은 연결된 카메라를 이미 받았어야 합니다.</param>
        /// <param name="cameras">명시적으로 읽은 카메라 기준 사실입니다. 비어 있으면 조작이 없습니다.</param>
        /// <param name="settings">카메라 조작 값입니다. 연결된 카메라가 있으면 필수이며 씬 바인딩이 제공합니다.</param>
        /// <param name="groundY">회전 중심을 찾을 바닥 평면의 월드 높이입니다. Factory가 필드 Cells Root에서 읽습니다(회전 계획 TP-11).</param>
        internal CombatCameraView(CombatCameraWindow window, IReadOnlyList<CombatCameraSnapshot> cameras, CombatCameraSettingsData settings,
            float groundY)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            if (cameras == null) throw new ArgumentNullException(nameof(cameras));
            if (float.IsNaN(groundY) || float.IsInfinity(groundY))
                throw new ArgumentOutOfRangeException(nameof(groundY), "[CombatCameraView] 바닥 높이는 유한한 수여야 합니다.");
            _groundY = groundY;
            _cameras = new CombatCameraSnapshot[cameras.Count];
            _poseAdapters = new CombatCameraZoomPose[cameras.Count];
            for (int i = 0; i < _cameras.Length; i++)
                _cameras[i] = cameras[i];
            if (_cameras.Length == 0)
                return;

            ConfigureSettings(settings);
            _size = _initialSize;
        }

        private void ConfigureSettings(CombatCameraSettingsData settings)
        {
            if (settings == null) throw new InvalidOperationException("[CombatCameraView] 연결된 카메라가 있는데 카메라 조작 값이 없습니다(CombatSceneCameraBinding.settings).");
            var paths = new CombatCameraZoomPosePath[_cameras.Length];
            for (int i = 0; i < _cameras.Length; i++)
            {
                if (_cameras[i].Transform == null)
                    throw new ArgumentException($"[CombatCameraView] 연결된 카메라 사실에 카메라 오브젝트가 없습니다: {_cameras[i].Name}", nameof(settings));
                // 보기 크기는 하나를 함께 쓰므로 처음 크기가 같아야 합니다. 다르면 첫 줌에서 한쪽이 튑니다.
                if (!settings.UseZoomPose && _cameras[i].BaseLens != _cameras[0].BaseLens)
                    throw new InvalidOperationException(
                        $"[CombatCameraView] 연결된 카메라의 처음 렌즈값이 다릅니다: {_cameras[0].Name}={_cameras[0].BaseLens}, {_cameras[i].Name}={_cameras[i].BaseLens}");
            }
            if (settings.UseZoomPose)
            {
                for (int i = 0; i < _cameras.Length; i++)
                {
                    CombatCameraSnapshot camera = _cameras[i];
                    CombatCameraZoomPose.ValidateCamera(camera.Transform.GetComponent<CinemachineVirtualCamera>());
                    paths[i] = CombatCameraZoomPosePath.FromOffsets(new Pose(camera.BasePosition, camera.BaseRotation),
                        settings.ZoomOutPositionOffset, settings.ZoomOutRotationOffset,
                        settings.ZoomInPositionOffset, settings.ZoomInRotationOffset, settings.ZoomMin, settings.ZoomMax);
                }
            }
            float initialSize = settings.UseZoomPose
                ? settings.ZoomMax
                : CombatCameraLimits.ClampSize(_cameras[0].BaseLens, settings);

            // 이동 기준 축 = 지금 오프셋이 쓰는 축입니다. 자세 모드는 줌아웃 끝 자세, 기존 모드는 기준 자세입니다(회전 계획 TP-2).
            // 오프셋을 하나로 함께 쓰므로 첫 카메라의 축으로 입력을 바꿉니다. 롤이 있거나 바닥을 보지 않으면 거절합니다(TP-3).
            Quaternion panFrame = settings.UseZoomPose
                ? paths[0].Evaluate(settings.ZoomMax, Vector2.zero).rotation
                : _cameras[0].BaseRotation;
            string frameError = CombatCameraOrbit.FindFrameError(panFrame);
            if (frameError != null)
                throw new InvalidOperationException($"[CombatCameraView] '{_cameras[0].Name}': {frameError}.");
            _settings = settings;
            _posePaths = paths;
            _initialSize = initialSize;
            _panPitchSin = CombatCameraOrbit.PitchSinOf(panFrame);
            CombatCameraOrbit.FlatAxesOf(panFrame, out _panFlatRight, out _panFlatAhead);
            if (settings.UseZoomPose)
                _lensCamera = _cameras[0].Transform.GetComponent<CinemachineVirtualCamera>();
        }

#if UNITY_EDITOR
        internal void ApplyEditorSettings(CombatCameraSettingsData settings)
        {
            bool wasPose = _settings.UseZoomPose;
            bool wasFullyZoomedOut = _size == _settings.ZoomMax;
            ConfigureSettings(settings);
            for (int i = 0; i < _cameras.Length; i++)
            {
                _poseAdapters[i]?.Unbind();
                if (!settings.UseZoomPose)
                    continue;
                var camera = _cameras[i];
                if (!wasPose)
                    camera.Transform.SetPositionAndRotation(camera.BasePosition, camera.BaseRotation);
                if (_poseAdapters[i] == null)
                    _poseAdapters[i] = camera.Transform.GetComponent<CombatCameraZoomPose>()
                        ?? camera.Transform.gameObject.AddComponent<CombatCameraZoomPose>();
                _poseAdapters[i].Bind(_posePaths[i], _groundY);
            }
            _offset = CombatCameraLimits.ClampOffset(_offset, settings);
            float size = wasFullyZoomedOut ? settings.ZoomMax : CombatCameraLimits.ClampSize(_size, settings);
            if (size != _size && _window.TrySetViewSize(size, 0f))
                _size = size;
            Place();
        }
#endif

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
            _yaw = 0f;
            _remainingKeyRotation = 0f;
            _rotationSpan = 0f;
            _begun = true;
            try
            {
                for (int i = 0; i < _cameras.Length; i++)
                {
                    if (!_settings.UseZoomPose)
                        continue;
                    if (_poseAdapters[i] == null)
                        _poseAdapters[i] = _cameras[i].Transform.gameObject.AddComponent<CombatCameraZoomPose>();
                    _poseAdapters[i].Bind(_posePaths[i], _groundY);
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
        /// 방향은 지금 화면 기준이라, 돌린 뒤에는 처음 카메라 화면 축으로 바꿔 더합니다(회전 설계 CR3).
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
            Vector2 screenDelta = velocity * (deltaTime * (_size / _initialSize));
            Vector2 offset = CombatCameraLimits.ClampOffset(
                _offset + CombatCameraOrbit.ToBasePan(screenDelta, _yaw, _panPitchSin), _settings);
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
        /// Z·C 키 회전입니다(회전 설계 CR4). −1 = 카메라가 왼쪽으로(Z), +1 = 오른쪽으로(C)입니다.
        /// 키 입력 한 번마다 90도 회전합니다.
        /// 위 축 양의 각이 카메라를 왼쪽으로 보내므로 방향의 반대 부호로 더합니다(회전 계획 TP-1). 실제 회전은 <see cref="TurnBy"/> 하나가 합니다.
        /// </summary>
        /// <returns>실제로 돌았으면 true입니다(입력이 포인터를 다시 집는 기준).</returns>
        public bool Rotate(int steps, float deltaTime)
        {
            if (!HasCameras || _settings.RotateSpeed <= 0f)
                return false;

            if (steps != 0)
            {
                _remainingKeyRotation -= 90f * steps;
                _rotationSpan = _remainingKeyRotation;
                _rotationProgress = 0f;
            }
            if (_rotationSpan == 0f || deltaTime <= 0f)
                return false;

            _rotationProgress = Mathf.Min(1f, _rotationProgress + deltaTime * _settings.RotateSpeed / Mathf.Abs(_rotationSpan));
            float remaining = 0f;
            if (_rotationProgress < 1f)
                remaining = _rotationSpan * (1f - _settings.EvaluateRotation(_rotationProgress));
            else
                _rotationSpan = 0f;

            float degrees = _remainingKeyRotation - remaining;
            _remainingKeyRotation = remaining;
            return degrees != 0f && TurnBy(degrees);
        }

        /// <summary>
        /// 가운데 버튼 끌기 회전입니다(회전 설계 CR8). <paramref name="screenWidths"/>는 이번 프레임에 끈 가로 거리(화면 너비 비율, 오른쪽 = 양수)이고,
        /// 각 = 비율 × 설정 <see cref="CombatCameraSettingsData.RotateDragDegrees"/>입니다. 양의 각은 Z와 같은 방향(화면 아래쪽 바닥이 커서를 따라감)이고,
        /// 설정이 0이면 돌지 않습니다. 실제 회전은 <see cref="TurnBy"/> 하나가 합니다.
        /// </summary>
        /// <returns>실제로 돌았으면 true입니다(입력이 포인터를 다시 집는 기준).</returns>
        public bool RotateDrag(float screenWidths)
        {
            // 카메라가 없으면 설정을 받지 않으므로 설정보다 먼저 봅니다(검토 L1).
            if (!HasCameras)
                return false;
            float degrees = screenWidths * _settings.RotateDragDegrees;
            if (degrees == 0f || float.IsNaN(degrees) || float.IsInfinity(degrees))
                return false;
            return TurnBy(degrees);
        }

        /// <summary>
        /// 화면 가운데가 보는 바닥 점 둘레로 위 축 회전을 <paramref name="degrees"/>도 더합니다(회전 설계 CR1). Z·C와 가운데 버튼 끌기가 함께 쓰는 회전 규칙 원본입니다.
        /// 내려다보는 각은 바꾸지 않습니다. 방위각은 −180~180에 감아 둡니다(TP-8).
        /// 회전 중심은 지금 줌의 화면 가운데입니다(검토 L1, 2026-11-14 사용자 결정 "돌릴 때 이동 보정"). 자세는 늘 줌아웃 끝 자세의 바닥 점 둘레로 돌리므로,
        /// 줌인 자세 때문에 생기는 화면 가운데 어긋남만큼 이동 오프셋을 반대로 옮겨 상쇄합니다(<see cref="HoldCenterShift"/>). 이동 제한 끝에서는 제한이 먼저입니다.
        /// 부르는 쪽이 카메라가 있는지 먼저 봅니다.
        /// </summary>
        private bool TurnBy(float degrees)
        {
            float previous = _yaw;
            _yaw = Mathf.DeltaAngle(0f, _yaw + degrees);
            if (_settings.UseZoomPose)
                _offset = CombatCameraLimits.ClampOffset(_offset + HoldCenterShift(previous, _yaw), _settings);
            Place();
            return true;
        }

        /// <summary>
        /// 방위각이 <paramref name="from"/> → <paramref name="to"/>로 바뀔 때 지금 줌의 화면 가운데를 제자리에 두는 오프셋 변화입니다(검토 L1).
        /// 화면 가운데 = 줌아웃 중심 + R(방위각)·d이고, d는 줌아웃 대비 지금 렌즈 크기에서 화면 가운데가 바닥 위로 옮겨 간 양입니다(이동과 무관).
        /// 그래서 방위각이 바뀌면 가운데가 (R(to) − R(from))·d만큼 움직이고, 그만큼 반대로 옮깁니다. 줌아웃 끝이면 d = 0이라 변화가 없습니다.
        /// </summary>
        private Vector2 HoldCenterShift(float from, float to)
        {
            CombatCameraZoomPosePath path = _posePaths[0];
            Pose zoomOut = path.Evaluate(_settings.ZoomMax, Vector2.zero);
            Pose current = path.Evaluate(_lensCamera.m_Lens.OrthographicSize, Vector2.zero);
            // 줌인 자세가 바닥을 보지 않는 설정이면 붙잡을 가운데가 없어 보정하지 않습니다.
            if (!CombatCameraOrbit.TryGroundPoint(zoomOut, _groundY, out Vector3 outCenter)
                || !CombatCameraOrbit.TryGroundPoint(current, _groundY, out Vector3 currentCenter))
                return Vector2.zero;
            Vector3 zoomShift = currentCenter - outCenter;
            if (zoomShift.sqrMagnitude < 0.000001f)
                return Vector2.zero;
            Vector3 moved = Quaternion.AngleAxis(to, Vector3.up) * zoomShift - Quaternion.AngleAxis(from, Vector3.up) * zoomShift;
            return CombatCameraOrbit.GroundToBasePan(-moved, _panFlatRight, _panFlatAhead, _panPitchSin);
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
            _yaw = 0f;
            _remainingKeyRotation = 0f;
            _rotationSpan = 0f;
            List<Exception> errors = null;
            for (int i = 0; i < _cameras.Length; i++)
            {
                CombatCameraSnapshot camera = _cameras[i];
                if (_poseAdapters[i] != null)
                    CleanupErrors.Try(_poseAdapters[i].Unbind, ref errors);
                CleanupErrors.Try(() => camera.Transform.SetPositionAndRotation(camera.BasePosition, camera.BaseRotation), ref errors);
            }
            CleanupErrors.Try(_window.RestoreView, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatCameraView] 카메라를 처음 위치·크기로 되돌리지 못했습니다.");
        }

        /// <summary>
        /// 자세 어댑터에 팬·방위각을 전달하거나, 기존 모드면 기준 자세에서 자기 화면 축으로 옮긴 뒤 바닥 점 둘레로 돌려 씁니다(회전 설계 CR2).
        /// 기존 모드는 위치와 회전을 함께 씁니다(회전 계획 TP-6). 방위각 0이면 회전은 기준 회전 그대로입니다.
        /// </summary>
        private void Place()
        {
            var local = new Vector3(_offset.x, _offset.y, 0f);
            for (int i = 0; i < _cameras.Length; i++)
            {
                CombatCameraSnapshot camera = _cameras[i];
                if (_settings.UseZoomPose)
                {
                    _poseAdapters[i].SetView(_offset, _yaw);
                    continue;
                }
                var panned = new Pose(camera.BasePosition + camera.BaseRotation * local, camera.BaseRotation);
                Pose pose = _yaw == 0f
                    ? panned
                    : CombatCameraOrbit.Apply(panned, CombatCameraOrbit.GroundPoint(panned, _groundY), _yaw);
                camera.Transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }
    }
}
