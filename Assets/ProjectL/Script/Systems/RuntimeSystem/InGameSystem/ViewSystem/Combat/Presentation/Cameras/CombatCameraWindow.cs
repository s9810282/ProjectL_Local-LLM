using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras
{
    /// <summary>
    /// 전투 연출이 쓰는 카메라 창구입니다(카메라 연출 조각 설계 K1). 연결된 카메라 사실을 갖고, 줌 대여 하나와 흔들림을 내줍니다.
    /// Framework 쪽에는 카메라 효과의 공개 <see cref="ICameraEffectService.Dispatch"/>만 부릅니다(결정 Q-K1 A, Framework 수정 없음).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 카메라 효과에는 끝 알림·취소·지금 값 조회가 없습니다(설계 §1.1). 그래서 이 창구가 표시 연결 전에 읽은 기준 렌즈값,
    /// 플레이어 줌이 정한 카메라마다의 보기 크기, 대여가 마지막으로 보낸 배율을 기억하고, 되돌릴 때는 보기 크기 0초 줌을 보내 진행 중인 줌을 덮어씁니다.
    /// 끝은 줌 조각이 자기 시계로 셉니다. 그래서 **연결된 카메라 줌은 이 창구로만 바꿉니다.** 다른 곳이 효과로 같은 카메라를 줌하면
    /// 기록한 크기·배율이 실제 렌즈와 어긋납니다(원칙 검사 C2).
    /// </para>
    /// <para>
    /// 보기 크기(카메라 조작 설계 CC4)는 처음에 기준 렌즈값이고, 카메라 조작 시점이 <see cref="TrySetViewSize"/>로만 바꿉니다.
    /// 연출 줌 배율은 보기 크기에 곱하고 되돌림은 보기 크기로 갑니다. 줌 대여가 있는 동안에는 보기 크기를 바꾸지 않습니다(결정 Q-CC6 A).
    /// </para>
    /// <para>
    /// 씬 바인딩에서 읽은 기준 사실을 Factory가 AttachCameras로 넘기고, Session이 복원 뒤 DetachCameras로 비웁니다.
    /// 효과 서비스가 없으면 줌·흔들림을 모두 내주지 않습니다(운영·특성 확인 표시는 표시 서비스 묶음의 필수 서비스를 넘김).
    /// </para>
    /// <para>
    /// 흔들림 겹침 규칙(설계 K8, 결정 Q-K8 B)도 이 창구 한 곳에 있습니다. 흔들림은 Cinemachine에서 더해지므로, 진행 중인 흔들림이 있으면
    /// 그보다 센 흔들림만 보냅니다. 그래서 같은 순간의 여러 피격, 기차 연쇄 사망, 포효 박치기와 그 대상들의 피격이 하나로 모이고, 센 사건은 막히지 않습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatCameraWindow
    {
        private readonly ICameraEffectService _effects;
        private readonly Func<float> _clock;
        private readonly List<CombatCameraSnapshot> _cameras = new();
        // 카메라마다 지금 보기 크기입니다(_cameras와 같은 순서). 처음은 기준 렌즈값입니다(카메라 조작 설계 CC4).
        private readonly List<float> _viewLens = new();
        private int _shakeChannel;
        private CameraZoomLease _zoom;
        private int _zoomVersion;
        // 진행 중인 흔들림의 끝 시각과 세기입니다(겹침 규칙). 끝 시각이 지나면 진행 중이 아닙니다.
        private float _shakeUntil = float.NegativeInfinity;
        private float _shakeStrength;

        /// <param name="effects">카메라 효과 서비스입니다. null이면 카메라 연출이 없는 창구입니다.</param>
        /// <param name="clock">흔들림 겹침을 판단하는 시계(초)입니다. 운영은 Cinemachine Impulse와 같은 Unity 시간을 넘깁니다.</param>
        internal CombatCameraWindow(ICameraEffectService effects, Func<float> clock)
        {
            _effects = effects;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <summary>줌을 빌릴 수 있는지입니다. 효과 서비스와 연결된 카메라가 있고, 빌린 줌이 없어야 합니다(설계 K3·Q-K6 A).</summary>
        internal bool CanZoom => _effects != null && _cameras.Count > 0 && _zoom == null;

        /// <summary>흔들림을 낼 수 있는지입니다. 효과 서비스가 있고 연결된 카메라에 흔들림 수신기가 있어야 합니다(설계 K7).</summary>
        internal bool CanShake => _effects != null && _shakeChannel != 0;

        /// <summary>세운 카메라 기준 사실을 받습니다. 앞 맵은 먼저 비웁니다(빌린 줌이 있으면 취소).</summary>
        internal void AttachCameras(IReadOnlyList<CombatCameraSnapshot> cameras)
        {
            if (cameras == null) throw new ArgumentNullException(nameof(cameras));
            DetachCameras();
            for (int i = 0; i < cameras.Count; i++)
            {
                _cameras.Add(cameras[i]);
                _viewLens.Add(cameras[i].BaseLens);
                _shakeChannel |= cameras[i].ShakeChannel;
            }
        }

        /// <summary>
        /// 연결된 카메라 사실을 비웁니다. 빌린 줌이 있으면 그 대여를 취소하고 받은 취소 콜백을 부릅니다.
        /// 렌즈는 되돌리지 않습니다. 배경이 남는 표시 해제는 그 전에 카메라 조작 시점이 <see cref="RestoreView"/>로 되돌립니다(카메라 조작 설계 CC6).
        /// 여러 번 불려도 안전합니다.
        /// </summary>
        internal void DetachCameras()
        {
            CameraZoomLease zoom = _zoom;
            _zoom = null;
            ++_zoomVersion;
            _cameras.Clear();
            _viewLens.Clear();
            _shakeChannel = 0;
            _shakeUntil = float.NegativeInfinity;
            _shakeStrength = 0f;
            zoom?.TakeCancellation()?.Invoke();
        }

        /// <summary>줌을 빌립니다. 빌릴 수 없으면 false입니다. 대여가 밖에서 취소되면(<see cref="DetachCameras"/>) <paramref name="cancelled"/>를 부릅니다.</summary>
        internal bool TryBeginZoom(Action cancelled, out ICombatCameraZoom zoom)
        {
            zoom = null;
            if (!CanZoom)
                return false;
            _zoom = new CameraZoomLease(this, ++_zoomVersion, cancelled);
            zoom = _zoom;
            return true;
        }

        /// <summary>
        /// 연결된 카메라 전부의 보기 크기를 <paramref name="size"/>로 옮깁니다(카메라 조작 설계 CC4). 카메라마다 기록한 보기 크기에서 시작해
        /// <paramref name="seconds"/> 동안 카메라 효과가 보간합니다. 0이면 바로 옮깁니다.
        /// 줌을 빌릴 수 없으면(<see cref="CanZoom"/>가 false — 효과 서비스·연결된 카메라가 없거나 연출 줌 대여 중) 아무 일도 하지 않고 false입니다(결정 Q-CC6 A).
        /// </summary>
        /// <param name="size">새 보기 크기입니다(직교 = 크기). 유한한 양수입니다. 제한은 부르는 쪽(카메라 조작 시점)이 맞춥니다.</param>
        internal bool TrySetViewSize(float size, float seconds)
        {
            if (!(size > 0f) || float.IsInfinity(size))
                throw new ArgumentOutOfRangeException(nameof(size), "보기 크기는 유한한 양수여야 합니다.");
            CombatPresentationSettingsData.ValidateSeconds(seconds, nameof(seconds));
            if (!CanZoom)
                return false;
            // 보내기 전에 기록합니다. 일부 카메라에만 나간 뒤 실패해도 다음 되돌림이 같은 크기를 씁니다(대여 ZoomTo와 같은 규칙).
            for (int i = 0; i < _cameras.Count; i++)
            {
                float from = _viewLens[i];
                _viewLens[i] = size;
                _effects.Dispatch(new CameraZoomData
                {
                    CameraName = _cameras[i].Name,
                    From = from,
                    To = size,
                    Duration = seconds,
                });
            }
            return true;
        }

        /// <summary>
        /// 보기 크기를 기준 렌즈값으로 바로(0초) 되돌립니다(카메라 조작 설계 CC6, 계획 P6). 표시 해제가 맵을 비우기 전에 부릅니다.
        /// 빌린 줌이 남아 있으면 그 렌즈도 기준값으로 덮습니다. 효과 서비스나 연결된 카메라가 없으면 아무 일도 하지 않습니다.
        /// 정리라 한 카메라가 실패해도 나머지를 계속하고 끝에 모아 던집니다.
        /// </summary>
        internal void RestoreView()
        {
            if (_effects == null)
                return;
            bool leased = _zoom != null;
            List<Exception> errors = null;
            for (int i = 0; i < _cameras.Count; i++)
            {
                CombatCameraSnapshot camera = _cameras[i];
                bool moved = _viewLens[i] != camera.BaseLens;
                _viewLens[i] = camera.BaseLens;
                if (!moved && !leased)
                    continue;
                CleanupErrors.Try(() => _effects.Dispatch(new CameraZoomData
                {
                    CameraName = camera.Name,
                    From = camera.BaseLens,
                    To = camera.BaseLens,
                    Duration = 0f,
                }), ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[CombatCameraWindow] 카메라 보기 크기를 기준값으로 되돌리지 못했습니다.");
        }

        /// <summary>
        /// 흔들림을 카메라 효과에 보냅니다. <see cref="CameraShakeData.Channel"/>만 이 창구가 연결된 카메라 수신기 채널로 채우고, 넘긴 값의 채널은 쓰지 않습니다.
        /// 나머지 값(방향·세기·시간·모양)과 그 검사는 카메라 효과가 원본입니다. 잘못된 값이면 효과가 보낼 때 예외를 던집니다.
        /// 낼 수 없으면(<see cref="CanShake"/>가 false) 아무 일도 하지 않습니다. 보낸 흔들림은 개별 취소가 없어 그 시간만큼 갑니다(카메라 효과 규칙).
        /// 진행 중인 흔들림이 있으면 그보다 센 흔들림만 보냅니다(겹침 규칙, 세기 = 속도 크기). 보낸 흔들림이 새 진행 중 흔들림이 됩니다.
        /// </summary>
        /// <returns>보냈으면 true, 낼 수 없거나 겹침 규칙으로 막혔으면 false입니다.</returns>
        internal bool Shake(CameraShakeData shake)
        {
            if (!CanShake)
                return false;
            float now = _clock();
            float strength = shake.Velocity.magnitude;
            if (now < _shakeUntil && strength <= _shakeStrength)
                return false;
            shake.Channel = _shakeChannel;
            _effects.Dispatch(shake);
            // 효과가 받아들인 뒤에만 기억합니다(잘못된 값은 위에서 예외).
            _shakeUntil = now + shake.Duration;
            _shakeStrength = strength;
            return true;
        }

        /// <summary>연결된 카메라마다 보기 크기에 배율을 곱해 줌을 보냅니다(카메라 조작 설계 CC4). 시간이 0이면 효과가 <c>From</c>을 쓰지 않습니다.</summary>
        private void DispatchZoom(float fromRatio, float toRatio, float seconds)
        {
            for (int i = 0; i < _cameras.Count; i++)
            {
                float view = _viewLens[i];
                _effects.Dispatch(new CameraZoomData
                {
                    CameraName = _cameras[i].Name,
                    From = view * fromRatio,
                    To = view * toRatio,
                    Duration = seconds,
                });
            }
        }

        /// <summary>연출 줌을 보기 크기로 되돌립니다. 정리라 한 카메라가 실패해도 나머지를 계속하고 끝에 모아 던집니다.</summary>
        private void RestoreAllZoom()
        {
            List<Exception> errors = null;
            for (int i = 0; i < _cameras.Count; i++)
            {
                string name = _cameras[i].Name;
                float view = _viewLens[i];
                CleanupErrors.Try(() => _effects.Dispatch(new CameraZoomData
                {
                    CameraName = name,
                    From = view,
                    To = view,
                    Duration = 0f,
                }), ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[CombatCameraWindow] 카메라 줌을 보기 크기로 되돌리지 못했습니다.");
        }

        /// <summary>줌 대여 하나입니다. 창구의 대여·버전이 자신일 때만 씁니다(유닛 창구 대여와 같은 늦은 복원 막기).</summary>
        private sealed class CameraZoomLease : ICombatCameraZoom
        {
            private readonly CombatCameraWindow _window;
            private readonly int _version;
            private Action _cancelled;
            private float _ratio = 1f;

            internal CameraZoomLease(CombatCameraWindow window, int version, Action cancelled)
            {
                _window = window;
                _version = version;
                _cancelled = cancelled;
            }

            private bool IsCurrent => ReferenceEquals(_window._zoom, this) && _window._zoomVersion == _version;

            public bool IsAvailable => IsCurrent;

            internal Action TakeCancellation()
            {
                Action cancelled = _cancelled;
                _cancelled = null;
                return cancelled;
            }

            public void ZoomTo(float ratio, float seconds)
            {
                if (!IsCurrent)
                    throw new InvalidOperationException("[CombatCameraWindow] 카메라 줌 대여가 취소·반환됐습니다.");
                CombatPresentationSettingsData.ValidateZoomRatio(ratio, nameof(ratio));
                CombatPresentationSettingsData.ValidateSeconds(seconds, nameof(seconds));
                // 지금 배율에서 시작합니다. 앞 줌이 마지막 프레임 전이어도 기록 배율과 거의 같아 튀지 않습니다(설계 K2).
                // 보내기 전에 기록합니다. 일부 카메라에만 나간 뒤 실패해도 되돌림이 모든 카메라를 기준값으로 돌립니다.
                float from = _ratio;
                _ratio = ratio;
                _window.DispatchZoom(from, ratio, seconds);
            }

            public void RestoreZoom()
            {
                if (!IsCurrent)
                    return;
                _cancelled = null;
                _window._zoom = null;
                bool moved = _ratio != 1f;
                _ratio = 1f;
                if (moved)
                    _window.RestoreAllZoom();
            }
        }
    }
}
