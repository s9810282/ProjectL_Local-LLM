using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>덱 편집 화면의 그리기·알림 계약입니다. 화면은 받은 표시 값을 그리고 입력 의도만 넘깁니다.</summary>
    public interface IDeckEditDisplay
    {
        bool IsOpen { get; }
        event Action<bool> OpenStateChanged;

        /// <summary>팝업이 닫혀 있을 때도 동작하는 열기 버튼의 처리기입니다. null이면 끊습니다.</summary>
        void BindOpenRequest(Action open);

        /// <summary>열린 동안의 입력 수신자입니다. null이면 끊습니다.</summary>
        void BindInput(IDeckEditInput input);

        void Render(DeckEditDisplayModel model);
        void Clear();
    }
}
