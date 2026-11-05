using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>고정 의존성으로 연결마다 덱 편집 팝업을 만들고 부모 Scope에 소유권을 넘깁니다.</summary>
    internal sealed class DeckEditPopupViewFactory
    {
        private readonly IRunDeckService _deck;
        private readonly IUISystemService _ui;
        private readonly RuntimeFaultBoundary _faults;

        internal DeckEditPopupViewFactory(IRunDeckService deck, IUISystemService ui, RuntimeFaultBoundary faults)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        internal DeckEditPopupController Create(IDeckEditDisplay screen, PopupInputSession input,
            IPopupViewHost host, Func<bool> canOpen)
        {
            if (_faults.IsFaulted)
                throw new InvalidOperationException("[DeckEditPopup] 정지한 런타임에는 팝업을 만들 수 없습니다.");
            return new DeckEditPopupController(_deck, _ui, screen, input, host, canOpen, _faults);
        }
    }
}
