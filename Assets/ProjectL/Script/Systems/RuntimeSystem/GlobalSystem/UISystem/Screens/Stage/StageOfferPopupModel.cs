using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>공통 레이아웃 한 번 그리기의 값 묶음입니다. 호출자가 같은 객체를 다시 채우고, 화면은 Render 안에서만 읽습니다.</summary>
    public sealed class StageOfferPopupModel
    {
        private readonly List<StageOfferCardContent> _cards = new();

        public string EventName { get; private set; } = string.Empty;
        /// <summary>대상 유닛·훈련 상태 등 선택에 필요한 안내입니다. 일반 스테이지 설명은 넣지 않으며, 비면 안내 줄과 여백을 접습니다.</summary>
        public string EventDescription { get; private set; } = string.Empty;
        /// <summary>보유 골드 글입니다. 비어 있으면 골드 칸을 끕니다.</summary>
        public string GoldText { get; private set; } = string.Empty;
        /// <summary>나가기 버튼을 보여 주는지입니다. 증강처럼 반드시 골라야 하는 화면은 끕니다.</summary>
        public bool ShowExit { get; private set; }
        /// <summary>보이는 나가기 버튼을 누를 수 있는지입니다.</summary>
        public bool CanExit { get; private set; }
        /// <summary>나가기 버튼 글입니다. 나가기가 없는 화면은 비어 있습니다.</summary>
        public string ExitText { get; private set; } = string.Empty;
        /// <summary>화면 위쪽 유닛 얼굴입니다. 증강 화면만 채우고, 비어 있으면 얼굴 칸을 끕니다.</summary>
        public Sprite Portrait { get; private set; }
        public IReadOnlyList<StageOfferCardContent> Cards => _cards;

        internal void Reset(string eventName, string eventDescription, string goldText, bool showExit, bool canExit, string exitText = null)
        {
            _cards.Clear();
            EventName = eventName ?? string.Empty;
            EventDescription = eventDescription ?? string.Empty;
            GoldText = goldText ?? string.Empty;
            ShowExit = showExit;
            CanExit = canExit;
            ExitText = exitText ?? string.Empty;
            Portrait = null;
        }

        /// <summary><see cref="Reset"/> 뒤에 부릅니다. 다시 채울 때마다 비워지므로 얼굴을 쓰지 않는 화면은 부르지 않습니다.</summary>
        internal void SetPortrait(Sprite portrait) => Portrait = portrait;

        internal void Add(StageOfferCardContent card) => _cards.Add(card);
    }
}
