using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>실제 스테이지 화면 부모의 수명 안에서 최초 선택 표시와 팝업 종료 후 복귀를 조정합니다.</summary>
    internal sealed class StageMapSelectionPresentation : IDisposable
    {
        private readonly IUISystemService _ui;
        private readonly StageScreenRootUI _root;
        private readonly StageSelectPanelUI _selection;
        private readonly Func<bool> _isCurrent;
        private readonly Func<bool> _canWork;
        private readonly Func<bool> _canShowSelection;
        private object _lifetime;
        private int _hiddenVersion;
        private StageMapScreenPresentation _pendingScreen;
        private object _pendingDisplay;
        private object _pendingLifetime;
        private bool _started;
        private bool _initialPending;
        private bool _disposed;

        internal StageMapSelectionPresentation(IUISystemService ui, StageScreenRootUI root, StageSelectPanelUI selection,
            Func<bool> isCurrent, Func<bool> canWork, Func<bool> canShowSelection)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _selection = selection != null ? selection : throw new ArgumentNullException(nameof(selection));
            _isCurrent = isCurrent ?? throw new ArgumentNullException(nameof(isCurrent));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            _canShowSelection = canShowSelection ?? throw new ArgumentNullException(nameof(canShowSelection));
        }

        internal bool CanOpenScreens => !_disposed && _lifetime != null && _canWork();

        internal object Lifetime => CanOpenScreens ? _lifetime : null;

        /// <summary>
        /// 선택 화면(StageSelect)이 실제로 보이는지입니다. 덱 편집은 이 화면을 부모로 열리므로 노드 진입의 조건입니다.
        /// UISystem에는 공개 열림 조회가 없고, 등록된 UI는 닫혀 있으면 숨겨지므로 화면 객체의 표시 상태를 읽습니다.
        /// 열고 닫은 호출을 따로 기억하지 않습니다(외부 CloseUI·HideAllUI가 그 기록을 거치지 않음).
        /// </summary>
        internal bool IsSelectionShown => CanOpenScreens && _selection.isActiveAndEnabled;

        internal void Start()
        {
            if (_disposed || !_canWork() || _lifetime != null) return;
            if (!_started)
            {
                _started = true;
                _root.Hidden += OnRootHidden;
            }
            int version = _hiddenVersion;
            _ui.OpenUI(UIId.StageScreenRoot, true);
            // Open/OnEnable 알림 안에서는 부모의 서비스 등록이 아직 완료되지 않았습니다.
            if (_disposed || !_canWork() || version != _hiddenVersion) return;
            if (_root == null || !_root.gameObject.activeInHierarchy)
                throw new InvalidOperationException("[StageMap] StageScreenRoot가 서비스에서 열리지 않았습니다.");
            _lifetime = new object();
            _initialPending = true;
        }

        internal StageMapScreenPresentation Observe(IPopupViewScreen screen) => new(this, screen);

        internal void ScreenOpened(object lifetime)
        {
            if (!Owns(lifetime)) return;
            _initialPending = false;
            CancelPendingReturn();
            _ui.CloseUI(UIId.StageSelect);
        }

        internal void RequestReturn(StageMapScreenPresentation screen, object display, object lifetime)
        {
            if (!Owns(lifetime) || !screen.CanReturn(display, lifetime)) return;
            _pendingScreen = screen;
            _pendingDisplay = display;
            _pendingLifetime = lifetime;
        }

        internal void Tick()
        {
            // 선택 화면 부모가 밖에서 숨으면 이 연결 동안 팝업·선택 화면이 모두 멈춥니다. 맵이 한가해지면 부모를 다시 엽니다(평가 N-04).
            // 로딩 등으로 맵이 바쁜 동안에는 다시 열지 않습니다.
            if (_started && _lifetime == null && !_disposed && _canWork() && _canShowSelection()) Start();
            if (!CanOpenScreens) return;
            if (_pendingScreen != null && (!Owns(_pendingLifetime) ||
                !_pendingScreen.CanReturn(_pendingDisplay, _pendingLifetime)))
                CancelPendingReturn();
            if (!_initialPending && _pendingScreen == null) return;
            if (!_canShowSelection() || !CanOpenScreens) return;
            // Open의 동기 콜백이 새 화면을 열어도 이전 복귀를 다시 소비하지 않습니다.
            _initialPending = false;
            CancelPendingReturn();
            _ui.OpenUI(UIId.StageSelect, true);
        }

        internal void HideSelection()
        {
            _initialPending = false;
            CancelPendingReturn();
            if (!_disposed && _isCurrent()) _ui.CloseUI(UIId.StageSelect);
        }

        internal void CancelPendingReturn()
        {
            _pendingScreen = null;
            _pendingDisplay = null;
            _pendingLifetime = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime = null;
            _initialPending = false;
            CancelPendingReturn();
            System.Collections.Generic.List<Exception> errors = null;
            if (_started) CleanupErrors.Try(() => _root.Hidden -= OnRootHidden, ref errors);
            CleanupErrors.Try(() => { if (_isCurrent()) _ui.CloseUI(UIId.StageSelect); }, ref errors);
            CleanupErrors.Try(() => { if (_isCurrent()) _ui.CloseUI(UIId.StageScreenRoot); }, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 선택 화면 부모 정리에 실패했습니다.");
        }

        private bool Owns(object lifetime) => lifetime != null && CanOpenScreens && ReferenceEquals(_lifetime, lifetime);

        private void OnRootHidden()
        {
            _hiddenVersion++;
            _lifetime = null;
            _initialPending = false;
            CancelPendingReturn();
        }
    }
}
