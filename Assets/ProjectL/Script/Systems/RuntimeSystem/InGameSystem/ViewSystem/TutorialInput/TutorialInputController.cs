using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput
{
    /// <summary>튜토리얼 액션을 "다음"의 누름·뗌·취소로 옮깁니다. 튜토리얼 상태와 Buffer를 모릅니다.</summary>
    internal sealed class TutorialInputController : InputController
    {
        private readonly ITutorialInputReceiver _receiver;

        public override string ActionMap => TutorialInputNames.Map;

        internal TutorialInputController(ITutorialInputReceiver receiver)
        {
            _receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        }

        protected override void Configure(InputBindings input)
        {
            input.Raw(TutorialInputNames.Next, InputValueKind.Button, OnNext);
        }

        // Press(ReleaseOnly): 누르면 Started, 떼면 Performed 다음 Canceled가 온다.
        // 누른 채 맵이 꺼지면(Layer 반환) Canceled만 온다. 맵이 켜지기 전부터 누르고 있던 버튼은 아무것도 오지 않는다.
        private void OnNext(InputSnapshot snapshot)
        {
            switch (snapshot.Phase)
            {
                case InputPhase.Started:
                    _receiver.OnNextPressed();
                    break;
                case InputPhase.Performed:
                    _receiver.OnNextReleased();
                    break;
                case InputPhase.Canceled:
                    _receiver.OnNextCanceled();
                    break;
            }
        }
    }
}
