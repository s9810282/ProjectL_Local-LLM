using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 배치 단계 맵(Deployment)의 입력을 수신 대상에 전달합니다. 씬·카메라·Buffer를 알지 못합니다.
    /// </summary>
    internal sealed class DeploymentInputController : InputController
    {
        private readonly IStageInputReceiver _receiver;
        private readonly StageSelectInput _select;

        public override string ActionMap => StageInputNames.Deployment.Map;

        internal DeploymentInputController(IStageInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _select = new StageSelectInput(receiver);
        }

        protected override void Configure(InputBindings input)
        {
            _select.Configure(input,
                StageInputNames.Deployment.PointerPosition,
                StageInputNames.Deployment.Press);

            input.Press(StageInputNames.Deployment.Cancel, _receiver.Cancel);
            input.Press(StageInputNames.Deployment.EnterCombat, _receiver.EnterCombatRequested);
            input.Press(StageInputNames.Deployment.DrawCharacter, _receiver.DrawRequested);
        }
    }
}
