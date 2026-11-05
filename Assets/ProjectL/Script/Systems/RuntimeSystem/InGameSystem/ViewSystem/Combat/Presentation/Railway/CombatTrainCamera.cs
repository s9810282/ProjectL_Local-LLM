using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway
{
    /// <summary>
    /// 기차가 필드를 지나는 동안의 카메라 연출입니다(카메라 연출 조각 설계 K9·K10). 필드 진입에 흔들림과 줌인, 필드 이탈에 줌 복귀를 냅니다.
    /// 언제(진입·이탈)는 기차 접촉 예약(<see cref="CombatRailwayContactPlayback"/>)이 알리고, 이 객체는 무엇을 낼지만 정합니다.
    /// 줌 대여·되돌림·흔들림 겹침 규칙은 카메라 창구, 값은 카메라 연출 값이 원본입니다.
    /// </summary>
    /// <remarks>
    /// 기차는 조각 조립이 아니라 전용 재생이라(동시 처리 결정 Q-C3 A) 줌 조각 대신 같은 규칙을 여기서 씁니다. 보간은 카메라 효과가 하고 끝은 자기 시계로 셉니다(K2).
    /// 돌아가는 중에 기차가 끝나거나 취소되면 남은 줌을 기준값으로 바로 되돌리고 대여를 반환합니다. 맵이 빠지면 창구가 대여를 취소합니다.
    /// 줌을 빌릴 수 없으면(다른 대여가 있음·효과 없음) 줌 없이 흔들림만 냅니다(결정 Q-K6 A와 같은 규칙).
    /// </remarks>
    internal sealed class CombatTrainCamera
    {
        private readonly CombatCameraWindow _camera;
        private readonly CombatCameraMotionSettings _motion;
        private ICombatCameraZoom _zoom;
        // 기준 배율로 돌아가는 중이면 남은 시간(초)입니다. 돌아가는 중이 아니면 음수입니다.
        private float _returnLeft = -1f;

        /// <param name="camera">맵 카메라 창구입니다(사건 Presenter와 같은 것).</param>
        /// <param name="motion">카메라 연출 값입니다. 기차 흔들림·줌 값을 읽습니다.</param>
        internal CombatTrainCamera(CombatCameraWindow camera, CombatCameraMotionSettings motion)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
        }

        /// <summary>낼 연출 값이 있는지입니다. 없으면 재생 조정이 진입·이탈 알림을 받지 않습니다(필드 구간도 계산하지 않음).</summary>
        internal bool HasMotion => _motion.HasTrainShake || _motion.HasTrainZoom;

        /// <summary>줌 대여를 들고 있는지입니다(줌인 중이거나 돌아가는 중).</summary>
        internal bool IsZooming => _zoom != null;

        /// <summary>기차 앞면이 필드에 들어온 순간입니다. 흔들림을 보내고, 줌을 빌릴 수 있으면 줌인합니다.</summary>
        internal void Entered()
        {
            if (_motion.HasTrainShake && _camera.CanShake)
                _camera.Shake(_motion.TrainShake);
            if (!_motion.HasTrainZoom || _zoom != null || !_camera.TryBeginZoom(ZoomCancelled, out ICombatCameraZoom zoom))
                return;
            _zoom = zoom;
            _returnLeft = -1f;
            zoom.ZoomTo(_motion.TrainZoomRatio, _motion.TrainZoomInSeconds);
        }

        /// <summary>기차 앞면이 필드를 빠져나간 순간입니다. 기준 배율로 돌아가기 시작하고, 그 시간이 끝나면 대여를 반환합니다.</summary>
        internal void Left()
        {
            if (_zoom == null || _returnLeft >= 0f)
                return;
            _zoom.ZoomTo(1f, _motion.TrainZoomOutSeconds);
            _returnLeft = _motion.TrainZoomOutSeconds;
            if (_returnLeft <= 0f)
                End();
        }

        /// <summary>결과 재생 프레임마다 부릅니다. 돌아가는 시간이 끝나면 대여를 반환합니다(배율이 이미 1이라 더 보내지 않음).</summary>
        internal void Tick(float deltaTime)
        {
            if (_zoom == null || _returnLeft < 0f)
                return;
            _returnLeft -= deltaTime;
            if (_returnLeft <= 0f)
                End();
        }

        /// <summary>
        /// 기차가 끝났거나 취소됐습니다. 남은 줌을 기준값으로 바로 되돌리고 대여를 반환합니다. 줌이 없으면 아무 일도 하지 않고, 여러 번 불려도 안전합니다.
        /// 보낸 흔들림은 개별 취소가 없어 그 시간만큼 갑니다(카메라 효과 규칙).
        /// </summary>
        internal void End()
        {
            ICombatCameraZoom zoom = _zoom;
            _zoom = null;
            _returnLeft = -1f;
            zoom?.RestoreZoom();
        }

        // 맵이 빠져 창구가 대여를 취소했습니다. 카메라가 곧 사라지므로 되돌리지 않습니다.
        private void ZoomCancelled()
        {
            _zoom = null;
            _returnLeft = -1f;
        }
    }
}
