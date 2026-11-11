using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 선택 화면의 공용 안내입니다. 맵 진입 거절과 덱 편집 안내가 같은 표시 시간을 씁니다.
    /// 문구는 기능마다 자기 문구 클래스(<see cref="StageMapEntryText"/>, 덱 편집 문구)에 둡니다.
    /// 안내 서비스는 Global Context의 <see cref="IUITextPresentation"/>을 받습니다(로비 출발 거절과 같은 경로).
    /// </summary>
    internal sealed class StageMapViewNotice
    {
        // 기획 확인 전 임시 값입니다. 로비 출발 거절 안내와 같습니다.
        internal const float Seconds = 3f;
        private readonly IUITextPresentation _text;

        internal StageMapViewNotice(IUITextPresentation text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        internal void Show(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new ArgumentException("안내 문구가 필요합니다.", nameof(text));
            _text.ShowNotice(text, Seconds);
        }
    }
}
