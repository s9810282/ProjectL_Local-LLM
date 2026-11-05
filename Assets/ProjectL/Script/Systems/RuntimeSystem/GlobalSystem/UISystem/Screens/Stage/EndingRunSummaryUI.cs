using System;
using System.Globalization;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>최종 승리한 Run의 통계를 표시하고 전투 완료 확인을 한 번 전달합니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby", sourceAssembly: "ProjectL.Systems", sourceClassName: "LobbyRunSummaryUI")]
    [DisallowMultipleComponent]
    public sealed class EndingRunSummaryUI : UIBase, IUIInputReceiver
    {
        private const int CloseSlot = 0;

        [SerializeField] private TMP_Text goldSpentText;
        [SerializeField] private TMP_Text fameEarnedText;
        [SerializeField] private TMP_Text enemiesDefeatedText;
        [SerializeField] private UnityEngine.UI.Button closeButton;

        private UIInputBinding _closeBinding;
        private IUISystemService _ui;
        private Action _confirmed;
        private RunSummary _lastShown;
        private bool _initialized;
        private bool _isOpen;
        private bool _hideInProgress;

        public override UIId ID => UIId.EndingRunSummary;
        public bool IsOpen => _isOpen;

        private bool CanReceive => _isOpen && !_hideInProgress && _confirmed != null && isActiveAndEnabled;

        public EndingRunSummaryUI() => Policy = UIOpenPolicy.Parallel;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        /// <summary>현재 Core가 확정한 요약을 표시합니다. RunId 대신 요약 객체 참조로 중복 표시를 막습니다.</summary>
        internal void Present(RunSummary summary, Action confirmed)
        {
            if (summary == null)
                throw new ArgumentNullException(nameof(summary));
            if (confirmed == null)
                throw new ArgumentNullException(nameof(confirmed));
            if (summary.Outcome != RunOutcome.Victory)
                throw new InvalidOperationException("[EndingRunSummaryUI] 최종 승리 요약만 표시할 수 있습니다.");
            if (_ui == null || _isOpen || _confirmed != null || ReferenceEquals(_lastShown, summary))
                throw new InvalidOperationException("[EndingRunSummaryUI] UI 연결 뒤 아직 표시하지 않은 요약을 한 번만 열 수 있습니다.");
            SetValues(summary.GoldSpent, summary.FameEarned, summary.EnemiesDefeated);
            _confirmed = confirmed;
            // 결과 확인은 뒤로가기로 건너뛸 수 없는 고정 UI이며, 실제 개폐는 UISystem이 맡습니다.
            _ui.OpenUI(ID, true);
            if (!_isOpen)
                throw new InvalidOperationException("[EndingRunSummaryUI] EndingPanel 표시가 완료되지 않았습니다.");
            _lastShown = summary;
        }

        internal void Dismiss()
        {
            if (_ui != null)
                _ui.CloseUI(ID);
        }

        private void Awake() => EnsureInitialized();

        private void OnDisable()
        {
            if (_initialized)
                Hide();
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));
        }

        public override void Open()
        {
            if (_isOpen || _hideInProgress)
                return;
            Validate();
            if (_confirmed == null)
                throw new InvalidOperationException("[EndingRunSummaryUI] 최종 승리 요약과 확인 동작을 준비한 뒤 열어야 합니다.");
            if (!enabled || (transform.parent != null && !transform.parent.gameObject.activeInHierarchy))
                throw new InvalidOperationException("[EndingRunSummaryUI] 컴포넌트와 부모 UI 계층이 활성 상태여야 합니다.");
            EnsureInitialized();
            _isOpen = true;
            gameObject.SetActive(true);
            if (!_isOpen)
                return;
            RefreshInputState();
            RefreshSelection();
        }

        public override void Hide()
        {
            if (_hideInProgress)
                return;
            EnsureInitialized();
            _isOpen = false;
            _confirmed = null;
            _hideInProgress = true;
            try
            {
                _closeBinding.OnHoverExit();
                closeButton.interactable = false;
                ClearSelection();
                gameObject.SetActive(false);
            }
            finally
            {
                _hideInProgress = false;
            }
        }

        /// <summary>창을 열지 않고 전달받은 음이 아닌 세 수치를 표시합니다.</summary>
        public void SetValues(int goldSpent, int fameEarned, int enemiesDefeated)
        {
            if (goldSpent < 0)
                throw new ArgumentOutOfRangeException(nameof(goldSpent), "쓴 골드는 음수일 수 없습니다.");
            if (fameEarned < 0)
                throw new ArgumentOutOfRangeException(nameof(fameEarned), "획득 명성은 음수일 수 없습니다.");
            if (enemiesDefeated < 0)
                throw new ArgumentOutOfRangeException(nameof(enemiesDefeated), "처치 수는 음수일 수 없습니다.");
            EnsureInitialized();
            goldSpentText.text = goldSpent.ToString("N0", CultureInfo.InvariantCulture);
            fameEarnedText.text = fameEarned.ToString("N0", CultureInfo.InvariantCulture);
            enemiesDefeatedText.text = enemiesDefeated.ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>표시나 입력 상태를 바꾸기 전에 필수 참조와 패널 계층을 검사합니다.</summary>
        public void Validate()
        {
            if (goldSpentText == null)
                throw new InvalidOperationException("[EndingRunSummaryUI] EndingRunSummary의 goldSpentText 참조가 필요합니다.");
            if (fameEarnedText == null)
                throw new InvalidOperationException("[EndingRunSummaryUI] EndingRunSummary의 fameEarnedText 참조가 필요합니다.");
            if (enemiesDefeatedText == null)
                throw new InvalidOperationException("[EndingRunSummaryUI] EndingRunSummary의 enemiesDefeatedText 참조가 필요합니다.");
            if (closeButton == null)
                throw new InvalidOperationException("[EndingRunSummaryUI] EndingRunSummary의 closeButton 참조가 필요합니다.");
            if (!goldSpentText.transform.IsChildOf(transform) ||
                !fameEarnedText.transform.IsChildOf(transform) ||
                !enemiesDefeatedText.transform.IsChildOf(transform) ||
                !closeButton.transform.IsChildOf(transform))
                throw new InvalidOperationException("[EndingRunSummaryUI] 결과 텍스트와 closeButton은 EndingPanel 안에 있어야 합니다.");
            if (goldSpentText == fameEarnedText || goldSpentText == enemiesDefeatedText ||
                fameEarnedText == enemiesDefeatedText)
                throw new InvalidOperationException("[EndingRunSummaryUI] 세 결과 텍스트는 서로 다른 컴포넌트여야 합니다.");
            if (closeButton.onClick.GetPersistentEventCount() > 0)
                throw new InvalidOperationException("[EndingRunSummaryUI] closeButton 입력은 Inspector onClick 대신 UIInputBinding으로 연결해야 합니다.");
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot != CloseSlot || !CanReceive || !closeButton.IsActive() || !closeButton.IsInteractable())
                return;
            Action confirmed = _confirmed;
            _confirmed = null;
            closeButton.interactable = false;
            _ui.CloseUI(ID);
            confirmed();
        }

        // 확인 버튼에는 별도 호버 동작이 없습니다. 시각·오디오 상태는 Button과 UIInputBinding이 맡습니다.
        void IUIInputReceiver.HoverEnter(int slot)
        {
        }

        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        private void EnsureInitialized()
        {
            Validate();
            if (_initialized)
                return;
            _closeBinding = UIInputBinding.Attach(closeButton, this, CloseSlot);
            _initialized = true;
            closeButton.interactable = false;
            gameObject.SetActive(false);
        }

        private void RefreshInputState() => closeButton.interactable = CanReceive;

        private void RefreshSelection()
        {
            if (!CanReceive || !closeButton.IsActive() || !closeButton.IsInteractable())
            {
                ClearSelection();
                return;
            }
            EventSystem events = EventSystem.current;
            if (events == null || events.alreadySelecting)
                return;
            events.SetSelectedGameObject(closeButton.gameObject);
            // 선택 콜백이 창을 닫거나 입력을 해제했다면 방금 지정한 선택도 정리합니다.
            if (!CanReceive)
                ClearSelection();
        }

        private void ClearSelection()
        {
            EventSystem events = EventSystem.current;
            if (events == null || events.alreadySelecting)
                return;
            GameObject selected = events.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(transform))
                events.SetSelectedGameObject(null);
        }

    }
}
