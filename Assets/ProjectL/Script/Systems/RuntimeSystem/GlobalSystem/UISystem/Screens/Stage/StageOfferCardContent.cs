using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 카드 한 장에 그릴 값입니다. 상점·회복이 같이 쓰며, 카드 UI는 이 값을 그리기만 합니다(설계 §3.1).
    /// </summary>
    /// <remarks>
    /// 문구는 모델을 만들 때 한 번 만들어 둡니다(성능 규칙 R4). 값 형식이라 목록에 넣어도 할당이 없습니다.
    /// 상점: 제목 = 캐릭터 이름, 첫 줄 = 가격("2G"), 둘째 줄 = 보유 수("보유 1").
    /// 증강: 첫 줄은 비우고(가격 없음), 종류 글 = "신규 특성"·"특성 강화"입니다.
    /// </remarks>
    public readonly struct StageOfferCardContent
    {
        /// <summary>카드 그림입니다. View DB에 없으면 null이며, UI는 빈 그림으로 둡니다.</summary>
        public Sprite Icon { get; }
        public string Title { get; }
        public string PrimaryText { get; }
        public string SecondaryText { get; }
        /// <summary>증강 종류 글입니다. 비어 있으면 칸을 끕니다.</summary>
        public string KindText { get; }
        public string ButtonText { get; }
        /// <summary>모델이 전달한 행동 허용 상태입니다. 카드 UI는 거래 규칙을 다시 판단하지 않습니다.</summary>
        public bool IsButtonEnabled { get; }
        /// <summary>이미 구매·적용한 항목입니다. UI는 완료 표시를 합니다.</summary>
        public bool IsCompleted { get; }

        internal StageOfferCardContent(Sprite icon, string title, string primaryText, string secondaryText,
            string buttonText, bool isButtonEnabled, bool isCompleted, string kindText = null)
        {
            Icon = icon;
            Title = title ?? string.Empty;
            PrimaryText = primaryText ?? string.Empty;
            SecondaryText = secondaryText ?? string.Empty;
            ButtonText = buttonText ?? string.Empty;
            IsButtonEnabled = isButtonEnabled;
            IsCompleted = isCompleted;
            KindText = kindText ?? string.Empty;
        }
    }
}
