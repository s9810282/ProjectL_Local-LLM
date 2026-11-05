using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem
{
    /// <summary>공용 안내 패널의 확인 응답을 받는 명시적 수신 대상입니다.</summary>
    public interface IUIConfirmationReceiver
    {
        void Respond(bool accepted);
    }

    /// <summary>기존 텍스트 표시 서비스가 제공하는 확인 표시 계약입니다. 별도 서비스로 등록하지 않습니다.</summary>
    public interface IUIConfirmationPresentation
    {
        void ShowConfirmation(string text, IUIConfirmationReceiver receiver);
        bool IsConfirmationOpen(IUIConfirmationReceiver receiver);
        void HideConfirmation(IUIConfirmationReceiver receiver);
    }

    /// <summary>공용 안내와 호버 설명의 텍스트 표시 요청입니다. 내용의 해석은 호출자가 맡습니다.</summary>
    public interface IUITextPresentation : IServiceModule
    {
        /// <summary>실제 호버 패널이 숨겨진 수명 번호입니다. 표시 캐시 무효화에만 사용합니다.</summary>
        uint HoverHideVersion { get; }

        void ShowNotice(string text);
        /// <summary>게임 시간 time초 뒤 닫습니다. 새 안내와 수동 숨김은 이전 예약을 무효화합니다.</summary>
        void ShowNotice(string text, float time);
        void HideNotice();
        void ShowHover(string name, string description);
        void MoveHover(Vector2 screenPosition);
        void UnHover();
    }

    /// <summary>텍스트 전달과 UISystem의 열기·닫기를 연결합니다.</summary>
    internal sealed class UITextPresentation : IUITextPresentation, IUIConfirmationPresentation, ISystemShutdown
    {
        private readonly IUISystemService _ui;
        private readonly IReadOnlyDictionary<UIId, GlobalUIBase> _globals;
        private readonly IDelayService _delay;
        private ulong _noticeVersion;
        private bool _shutdown;

        internal UITextPresentation(IUISystemService ui, IReadOnlyDictionary<UIId, GlobalUIBase> globals,
            IDelayService delay)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _globals = globals ?? throw new ArgumentNullException(nameof(globals));
            _delay = delay ?? throw new ArgumentNullException(nameof(delay));
        }

        public void ShowNotice(string text) => ShowNoticeCore(text);

        private ulong ShowNoticeCore(string text)
        {
            ThrowIfShutdown();
            if (text == null) throw new ArgumentNullException(nameof(text));
            // 응답을 기다리는 확인창은 일반 안내와 이전 안내 타이머가 덮어쓰지 않습니다.
            if (GetView(UIId.Notice).HasConfirmation) return _noticeVersion;
            ulong version = ++_noticeVersion;
            Show(UIId.Notice, text);
            return version;
        }

        public void ShowNotice(string text, float time)
        {
            if (float.IsNaN(time) || float.IsInfinity(time) || time < 0)
                throw new ArgumentOutOfRangeException(nameof(time));
            ThrowIfShutdown();
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (GetView(UIId.Notice).HasConfirmation) return;
            ulong version = ShowNoticeCore(text);
            _delay.AddDelayEvent(new DelayDataHandler(() =>
            {
                if (!_shutdown && version == _noticeVersion)
                    HideNotice();
            }, null, time));
        }

        public uint HoverHideVersion => GetView(UIId.Hover).HoverHideVersion;
        public void HideNotice()
        {
            ThrowIfShutdown();
            if (GetView(UIId.Notice).HasConfirmation) return;
            ++_noticeVersion;
            _ui.CloseUI(UIId.Notice);
        }

        public void ShowConfirmation(string text, IUIConfirmationReceiver receiver)
        {
            ThrowIfShutdown();
            GetView(UIId.Notice).PrepareConfirmation(text, receiver);
            ++_noticeVersion;
            try { _ui.OpenUI(UIId.Notice); }
            catch { GetView(UIId.Notice).ClearConfirmation(); throw; }
        }

        public bool IsConfirmationOpen(IUIConfirmationReceiver receiver) =>
            !_shutdown && GetView(UIId.Notice).IsConfirmationFor(receiver);

        public void HideConfirmation(IUIConfirmationReceiver receiver)
        {
            if (!IsConfirmationOpen(receiver)) return;
            ++_noticeVersion;
            _ui.CloseUI(UIId.Notice);
        }

        public void Shutdown()
        {
            _shutdown = true;
            ++_noticeVersion;
        }

        private void ThrowIfShutdown()
        {
            if (_shutdown)
                throw new InvalidOperationException("[UITextPresentation] 종료된 서비스에 안내를 요청할 수 없습니다.");
        }
        public void ShowHover(string name, string description)
        {
            GetView(UIId.Hover).SetHoverText(name, description);
            _ui.OpenUI(UIId.Hover);
        }
        public void UnHover() => _ui.CloseUI(UIId.Hover);
        public void MoveHover(Vector2 screenPosition) => GetView(UIId.Hover).MoveTo(screenPosition);

        private void Show(UIId id, string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            GetView(id).SetText(text);
            _ui.OpenUI(id);
        }

        private TextNoticeUI GetView(UIId id)
        {
            if (_globals.TryGetValue(id, out GlobalUIBase view) && view is TextNoticeUI textView)
                return textView;
            throw new InvalidOperationException($"[UITextPresentation] {id} TextNoticeUI가 등록되어야 합니다.");
        }
    }
}
