using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 카메라 흔들림을 내는 시간 없는 조각입니다(카메라 연출 조각 설계 K2). 사건 소리 조각과 같은 모양으로, 시작에서 내고 바로 끝납니다.
    /// 흔들림 값은 카메라 효과의 <see cref="CameraShakeData"/>를 그대로 받고, 채널만 창구가 맵 카메라 수신기로 채웁니다.
    /// </summary>
    /// <remarks>
    /// 흔들림 시간만큼 실행을 붙잡지 않습니다. 기다리지 않는 항목도 실행을 살려 두어 최종 사실·다음 결과가 늦어지기 때문입니다.
    /// 보낸 흔들림은 개별 취소가 없어 취소돼도 그 시간만큼 갑니다(카메라 효과 규칙). 수신기가 없으면 창구가 아무것도 하지 않으므로,
    /// 조각을 넣는 쪽(큐 붙이기 <see cref="CombatCueComposer"/>·포효 처리기)이 <see cref="CombatCameraWindow.CanShake"/>를 보고 이 조각을 넣습니다(설계 K7). 값 검사는 카메라 효과가 보낼 때 합니다.
    /// </remarks>
    internal sealed class CombatCameraShakePiece : ICombatPresentationPiece
    {
        private readonly CombatCameraWindow _camera;
        private readonly CameraShakeData _shake;

        internal CombatCameraShakePiece(CombatCameraWindow camera, CameraShakeData shake)
        {
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _shake = shake;
        }

        public bool IsEnded { get; private set; }

        public void Begin(CombatPieceContext context)
        {
            _camera.Shake(_shake);
            IsEnded = true;
        }

        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}
