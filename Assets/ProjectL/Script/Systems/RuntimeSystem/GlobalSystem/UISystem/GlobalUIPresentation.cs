using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem
{
    /// <summary>씬 진입이 완료된 호출자가 글로벌 UI의 표시만 전환합니다. 씬 이동은 수행하지 않습니다.</summary>
    public interface IGlobalUIPresentation : IServiceModule
    {
        void ShowLobby();
        void ShowStageSelect();
        void ShowCombat();
        void ShowDevelopmentCombat();
        void ShowTitle();
    }

    /// <summary>씬별 표시 정책을 UI 등록·화면 이력 구현과 분리합니다.</summary>
    internal sealed class GlobalUIPresentation : IGlobalUIPresentation, IExportSystemServices
    {
        private readonly IUISystemService _ui;
        private readonly IReadOnlyDictionary<UIId, GlobalUIBase> _globals;

        internal GlobalUIPresentation(IUISystemService ui, IReadOnlyDictionary<UIId, GlobalUIBase> globals)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _globals = globals ?? throw new ArgumentNullException(nameof(globals));
        }

        public void ShowLobby()
        {
            var main = Main;
            if (!_globals.TryGetValue(UIId.AfterTitle, out var afterTitle) || afterTitle == null)
                throw new InvalidOperationException("[GlobalUIPresentation] Lobby 표시에는 AfterTitle UI 등록이 필요합니다.");
            foreach (var pair in _globals)
            {
                if (pair.Key != UIId.Setting && pair.Key != UIId.MainCharactor && pair.Key != UIId.AfterTitle && pair.Key != UIId.Loading)
                    _ui.CloseUI(pair.Key);
            }
            main.SetHealthBarVisible(false);
            main.SetGoldVisible(false);
            _ui.OpenUI(UIId.AfterTitle);
            _ui.OpenUI(UIId.MainCharactor);
            _ui.OpenChildUI(UIId.MainCharactor, UIId.Reputation);
        }

        public void ShowStageSelect() => ShowCombat();

        public void ShowCombat()
        {
            var main = Main;
            // Lobby에서 닫은 유지 UI를 기존 스테이지·전투 표시로 복원합니다.
            _ui.OpenGlobalUI();
            _ui.OpenUI(UIId.MainCharactor);
            main.SetHealthBarVisible(true);
            main.SetGoldVisible(true);
            _ui.CloseUI(UIId.Reputation);
        }

        public void ShowDevelopmentCombat()
        {
            // 요청형 패널은 제외하고 공통 장식·HP·두 재화 표시를 엽니다. 덱은 Combat 씬이 준비합니다.
            ShowCombat();
            if (_ui is IUIRuntimeFaultHandler faults && faults.IsFaulted) return;
            _ui.OpenChildUI(UIId.MainCharactor, UIId.Reputation);
        }

        public void ShowTitle()
        {
            foreach (var pair in _globals)
                if (pair.Key != UIId.Setting && pair.Key != UIId.Loading && pair.Value != null) _ui.CloseUI(pair.Key);
        }

        private MainCharactorUI Main => _globals.TryGetValue(UIId.MainCharactor, out var view) && view is MainCharactorUI main
            ? main : throw new InvalidOperationException("[GlobalUIPresentation] MainCharactor UI가 등록되어야 재화 표시를 전환할 수 있습니다.");

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<IGlobalUIPresentation>(this);
        }
    }
}
