using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>덱 내용을 표시하고 팝업 수명에 입력 연결을 맡깁니다. 로비 팝업 목록의 덱 항목입니다(로비 리팩토링 2차 설계 M-14).</summary>
    internal sealed class LobbyDeckPopupController : IPopupViewController, ILobbyPopup
    {
        private readonly ILobbyDeckDisplay _view;
        private readonly LobbyDeckPresenter _presenter;
        private readonly IPopupViewHost _host;
        private readonly PopupViewScope _scope;
        // 화면에 거는 수신자는 입력 콜백 예외를 오류 경계로 보내는 감싸개입니다(로비 리팩토링 2차 설계 M-01).
        private readonly IUIInputReceiver _receiver;
        private bool _disposed;

        internal LobbyDeckPopupController(IUISystemService ui, ILobbyDeckDisplay view,
            LobbyDeckPresenter presenter, PopupInputSession input,
            IPopupViewHost host, RuntimeFaultBoundary faults)
        {
            if (ui == null) throw new ArgumentNullException(nameof(ui));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            if (input == null) throw new ArgumentNullException(nameof(input));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _scope = new PopupViewScope(ui, new LobbyDeckPopupViewScreen(view), input,
                LobbyInputLayers.DeckPopup, this, host, faults);
            _receiver = new FaultReportingUIInputReceiver(_presenter, faults);
            _presenter.SetInputEnabled(false);
            if (_host.IsCurrent) _view.BindInput(_receiver);
        }

        internal bool CanOpen => _scope.CanOpen;

        // 부모가 기본 Layer를 연결한 뒤 이미 열린 화면을 인수합니다.
        internal void Start() => _scope.Start();
        internal void Open() => _scope.Open();
        internal void Close() => _scope.Close();
        internal void Tick() => _scope.Tick();

        LobbyFacilityKind ILobbyPopup.Facility => LobbyFacilityKind.Deck;
        bool ILobbyFacilityAction.CanOpen => CanOpen;
        void ILobbyFacilityAction.Open() => Open();
        void ILobbyPopup.Start() => Start();
        void ILobbyPopup.Tick() => Tick();
        // 덱 팝업은 공유 출력 설정을 바꾸지 않습니다.
        void ILobbyPopup.RestoreOutput()
        {
        }

        void IPopupViewController.Show()
        {
            if (_disposed || !_host.IsCurrent) return;
            // 운영 후보 계약 전에는 View가 후보를 만들어 내지 않습니다.
            _presenter.SetModel(new LobbyDeckDisplayModel(Array.Empty<LobbyDeckCardContent>()));
            if (_disposed || !_host.IsCurrent) return;
            _presenter.SetInputEnabled(true);
        }

        void IPopupViewController.Hide(bool explicitClose)
        {
            if (!_host.IsCurrent) return;
            // Clear가 입력 끄기·호버 해제·상세 비우기까지 합니다(N-24).
            _presenter.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            List<Exception> errors = null;
            CleanupErrors.Try(_scope.Dispose, ref errors);
            CleanupErrors.Try(UnbindInput, ref errors);
            CleanupErrors.Try(ClearPresenter, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 덱 팝업 정리 중 실패했습니다.");
        }

        private void UnbindInput()
        {
            if (_host.IsCurrent) _view.BindInput(null);
        }

        private void ClearPresenter()
        {
            if (_host.IsCurrent) _presenter.Clear();
        }
    }
}
