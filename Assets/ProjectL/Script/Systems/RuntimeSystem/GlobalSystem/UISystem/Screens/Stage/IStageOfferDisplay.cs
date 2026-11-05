using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>공통 레이아웃 화면의 표시·입력 허용·동기 수명 계약입니다. 제출과 "제출 중" 사실은 수신자가 갖습니다.</summary>
    public interface IStageOfferDisplay
    {
        UIId ID { get; }
        bool IsOpen { get; }
        event Action<bool> OpenStateChanged;
        /// <summary>입력 의도를 받을 수신자를 연결합니다. null이면 끊고, 끊긴 동안 입력은 무시합니다.</summary>
        void BindInput(IStageOfferInput input);
        void Render(StageOfferPopupModel model);
        /// <summary>ErrorPanel에 안내를 냅니다. 몇 초 뒤 스스로 사라집니다.</summary>
        void ShowError(string message);
        void SetCanSubmit(bool canSubmit);
    }
}
