using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 전투 카메라 조작 맵(CombatCamera)의 입력을 수신 대상에 전달합니다(카메라 조작 설계 CC2). 씬·카메라·Buffer를 알지 못합니다.
    /// 배치·전투 두 Layer가 이 맵을 함께 켜므로 두 단계에서 같은 Controller가 받습니다.
    /// </summary>
    internal sealed class CombatCameraInputController : InputController
    {
        private readonly IStageInputReceiver _receiver;

        public override string ActionMap => StageInputNames.CombatCamera.Map;

        internal CombatCameraInputController(IStageInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        protected override void Configure(InputBindings input)
        {
            input.Vector2(StageInputNames.CombatCamera.Pan, _receiver.CameraPanChanged);
            input.Float(StageInputNames.CombatCamera.Zoom, _receiver.CameraZoomScrolled);
            input.Float(StageInputNames.CombatCamera.Rotate, _receiver.CameraRotateChanged);
            // 가운데 버튼 드래그 회전은 임시로 연결하지 않습니다.
        }
    }
}
