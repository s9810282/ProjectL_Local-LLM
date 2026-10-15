using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 전투 단계 맵(Combat)의 입력을 수신 대상에 전달합니다. 씬·카메라·Buffer를 알지 못합니다.
    /// </summary>
    internal sealed class CombatInputController : InputController
    {
        private readonly IStageInputReceiver _receiver;
        private readonly StageSelectInput _select;

        public override string ActionMap => StageInputNames.Combat.Map;

        internal CombatInputController(IStageInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            _select = new StageSelectInput(receiver);
        }

        protected override void Configure(InputBindings input)
        {
            _select.Configure(input,
                StageInputNames.Combat.PointerPosition,
                StageInputNames.Combat.Select);

            input.Press(StageInputNames.Combat.Cancel, _receiver.Cancel);
            input.Press(StageInputNames.Combat.EndTurn, _receiver.EndTurnRequested);
            input.Press(StageInputNames.Combat.MoveMode, _receiver.MoveModeRequested);
            input.Press(StageInputNames.Combat.AttackMode, _receiver.AttackModeRequested);
            input.Press(StageInputNames.Combat.Return, _receiver.RequestReturn);
        }
    }
}
