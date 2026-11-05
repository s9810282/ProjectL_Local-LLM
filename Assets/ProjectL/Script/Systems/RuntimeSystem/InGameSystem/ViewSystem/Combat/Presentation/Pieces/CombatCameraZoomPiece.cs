using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 맵 카메라 줌을 배율로 옮기는 조각입니다(카메라 연출 조각 설계 K2). 보간은 카메라 효과가 하고, 끝은 이 조각이 자기 시계로 셉니다.
    /// 줌 대여는 실행이 빌리고 되돌리므로(장면 채널, 설계 K3) 조립이 <see cref="CombatComposition.LeaseCameraZoom"/>을 적어야 합니다.
    /// </summary>
    /// <remarks>
    /// 카메라 효과에는 끝 알림이 없어 끝을 자기 시계로 셉니다. 효과의 보간 시계와 최대 한 프레임 어긋날 수 있지만,
    /// 다음 줌은 기록한 배율(앞 줌의 목표)에서 시작하므로 튀지 않습니다. 끝 판정은 크기 조각과 같은 식이라,
    /// 같은 시간으로 함께 시작한 크기 조각과 같은 프레임에 끝납니다. 같은 실행의 줌 항목은 앞 줌의 끝 뒤에 둡니다.
    /// </remarks>
    internal sealed class CombatCameraZoomPiece : ICombatPresentationPiece
    {
        private readonly float _ratio;
        private readonly float _duration;
        private float _elapsed;
        private bool _began;

        /// <param name="ratio">보기 크기에 곱하는 목표 배율입니다(작을수록 확대, 1 = 보기 크기 그대로, 카메라 조작 설계 CC4).</param>
        /// <param name="duration">옮기는 시간(초)입니다. 0이면 시작하자마자 끝납니다.</param>
        internal CombatCameraZoomPiece(float ratio, float duration)
        {
            // 값 조건은 설정 Data 한 곳을 씁니다(연출 조립 설계 §9 I-5).
            CombatPresentationSettingsData.ValidateZoomRatio(ratio, nameof(ratio));
            CombatPresentationSettingsData.ValidateSeconds(duration, nameof(duration));
            _ratio = ratio;
            _duration = duration;
        }

        public bool IsEnded => _began && _elapsed >= _duration;

        public void Begin(CombatPieceContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.CameraZoom().ZoomTo(_ratio, _duration);
            _elapsed = 0f;
            _began = true;
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
        }

        public void Cancel()
        {
        }
    }
}
