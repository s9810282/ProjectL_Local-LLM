using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// StageOfferUI 프리팹의 그리기와 입력 전달을 맡는 부품입니다. 등록 화면 안의 부품이라 UIBase가 아니고 규칙·결과 판정을 갖지 않습니다.
    /// 카드는 프리팹에 미리 놓인 것만 쓰고 런타임에 만들지 않습니다(공통 레이아웃 설계 SO1·SO4).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageOfferLayoutView : MonoBehaviour, IUIInputReceiver
    {
        private const int ExitSlot = -1;
        [Tooltip("두루마리를 사용하는 이벤트·증강 화면에 연결합니다. 필수 여부는 소유 화면이 검사합니다.")]
        [SerializeField] private ScrollPanelPresentation scrollPresentation;

        internal ScrollPanelPresentation ScrollPresentation => scrollPresentation;

        [SerializeField] private TMP_Text eventNameText;
        [Tooltip("대상 유닛·훈련 상태 안내입니다. 비어 있으면 숨기고 제목 아래의 빈 공간으로 카드 영역을 위로 당깁니다.")]
        [SerializeField] private TMP_Text eventDescText;
        [Tooltip("위쪽 유닛 얼굴입니다. 증강 화면(TraitUI)만 연결하고 이벤트 스테이지는 비워 둡니다. 그릴 얼굴이 없으면 꺼집니다.")]
        [SerializeField] private Image unitFace;
        [SerializeField] private TMP_Text goldText;
        [Tooltip("GoldText가 비면 함께 꺼집니다.")]
        [SerializeField] private GameObject goldBackground;
        [Tooltip("선택 장식선입니다. 연결한 경우 골드와 나가기가 모두 없으면 꺼집니다.")]
        [SerializeField] private GameObject underLine;
        [SerializeField] private Button exitButton;
        [Tooltip("나가기 버튼 글입니다. 글은 SO에서 옵니다.")]
        [SerializeField] private TMP_Text exitLabel;
        [Tooltip("미리 놓은 카드입니다. 그릴 카드가 이보다 많으면 예외입니다. 훈련소 유닛 단계 때문에 보유 최대 7장입니다.")]
        [SerializeField] private List<StageOfferCardView> cards = new();
        [SerializeField] private float cardSpacing = 60f;
        [Tooltip("카드가 칸을 넘칠 때만 켜지는 스크롤입니다. Content는 카드들의 부모입니다. 넘치지 않는 화면(증강)은 비워도 됩니다.")]
        [SerializeField] private ScrollRect cardScroll;
        [SerializeField] private GameObject errorPanel;
        [Tooltip("ErrorPanel의 CanvasGroup입니다. 알파로 사라지며, Blocks Raycasts를 꺼서 켜진 동안에도 뒤의 카드를 누를 수 있습니다.")]
        [SerializeField] private CanvasGroup errorGroup;
        [SerializeField] private TMP_Text errorText;
        [Tooltip("안내를 그대로 보여 주는 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float errorHoldSeconds = 2f;
        [Tooltip("안내가 천천히 사라지는 시간(초)입니다.")]
        [SerializeField, Min(0f)] private float errorFadeSeconds = 0.5f;

        private IStageOfferInput _input;
        private UIInputBinding _exitBinding;
        private RectTransform _cardArea;
        private Vector2 _cardAreaPosition;
        private int _visibleCardCount;
        private int _fittedCardCount = -1;
        private float _errorElapsed;
        private bool _prepared;
        private bool _interactable;
        private bool _canExit;

        private bool HasRequiredReferences => eventNameText != null && eventDescText != null && goldText != null &&
            goldBackground != null && exitButton != null && exitLabel != null && HasAllCards &&
            errorPanel != null && errorGroup != null && errorText != null;

        private bool HasAllCards
        {
            get
            {
                if (cards == null || cards.Count == 0) return false;
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] == null) return false;
                }
                return true;
            }
        }

        // 시간은 안내가 켜진 동안만 잽니다.
        private void Awake() => enabled = false;

        /// <summary>소유 화면의 Awake가 부릅니다. 참조가 빠졌으면 씬 연결 때 구성 검사(<see cref="Validate"/>)가 F1로 알리도록 미룹니다.</summary>
        internal void TryPrepare(UIBase owner)
        {
            if (_prepared || !HasRequiredReferences) return;
            PrepareCore(owner);
        }

        /// <summary>
        /// 소유 화면의 Open이 부릅니다. 준비 전에 필수 구성을 검사하고, 빠졌으면 바로 예외입니다.
        /// 구성 검사를 거치지 않은 경로에서도 그리기의 원인 없는 NRE 대신 누락을 알립니다(평가 N-44).
        /// </summary>
        internal void Prepare(UIBase owner)
        {
            if (_prepared) return;
            Validate();
            PrepareCore(owner);
        }

        private void PrepareCore(UIBase owner)
        {
            // 스크롤은 Viewport를 옮겨 Content의 가로 스크롤 위치와 분리합니다.
            _cardArea = cardScroll != null ? cardScroll.viewport : cards[0].transform.parent as RectTransform;
            if (_cardArea == null || _cardArea.parent == null)
                throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'의 카드 영역에는 부모가 있는 RectTransform이 필요합니다.");
            _cardAreaPosition = _cardArea.anchoredPosition;
            _prepared = true;
            _exitBinding = UIInputBinding.Attach(exitButton, this, ExitSlot);
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Bind(this, i);
                cards[i].BindOwner(owner);
            }
            HideError();
            SetVisible(false);
        }

        /// <summary>필수 참조와 구성을 검사합니다. 상태 변경 전에 부릅니다.</summary>
        internal void Validate()
        {
            if (!HasRequiredReferences)
                throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'의 글·골드·나가기·카드·안내 참조가 필요합니다.");
            if (errorGroup.gameObject != errorPanel || !errorText.transform.IsChildOf(errorPanel.transform))
                throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'의 errorGroup은 ErrorPanel에, errorText는 그 안에 있어야 합니다.");
            for (int i = 0; i < cards.Count; i++) cards[i].Validate();
            if (cardScroll == null) return;
            if (cardScroll.content == null || cardScroll.viewport == null)
                throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'의 스크롤에 Content·Viewport가 필요합니다.");
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].transform.parent != cardScroll.content)
                    throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'의 카드 {i}가 스크롤 Content 아래에 없습니다.");
            }
        }

        internal void SetVisible(bool visible)
        {
            bool wasVisible = gameObject.activeSelf;
            if (!visible)
            {
                HideError();
                _fittedCardCount = -1;
                if (scrollPresentation != null) scrollPresentation.ResetHidden();
            }
            gameObject.SetActive(visible);
            if (visible && !wasVisible && scrollPresentation != null) scrollPresentation.PlayOpen();
        }

        internal void BindInput(IStageOfferInput input) => _input = input;

        internal void Render(StageOfferPopupModel model)
        {
            if (model.Cards.Count > cards.Count)
                throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'에 놓인 카드 {cards.Count}장보다 많은 {model.Cards.Count}장을 그릴 수 없습니다.");
            if (model.Portrait != null && unitFace == null)
                throw new InvalidOperationException($"[StageOfferLayoutView] '{name}'에 unitFace 참조가 없어 유닛 얼굴을 그릴 수 없습니다.");

            eventNameText.text = model.EventName;
            RenderSelectionPrompt(model.EventDescription);
            if (unitFace != null)
            {
                unitFace.sprite = model.Portrait;
                unitFace.gameObject.SetActive(model.Portrait != null);
            }
            goldText.text = model.GoldText;
            bool showGold = model.GoldText.Length > 0;
            goldBackground.SetActive(showGold);
            exitButton.gameObject.SetActive(model.ShowExit);
            exitLabel.text = model.ExitText;
            if (underLine != null) underLine.SetActive(showGold || model.ShowExit);
            _canExit = model.ShowExit && model.CanExit;
            RenderCards(model.Cards);
            RefreshInteractability();
        }

        private void RenderSelectionPrompt(string prompt)
        {
            bool hasPrompt = !string.IsNullOrWhiteSpace(prompt);
            eventDescText.text = prompt;
            eventDescText.gameObject.SetActive(hasPrompt);

            // 매번 프리팹의 기준 위치에서 계산하므로 안내 유무 전환과 다시 열기 때 이동량이 누적되지 않습니다.
            _cardArea.anchoredPosition = _cardAreaPosition;
            if (hasPrompt) return;

            RectTransform promptRect = eventDescText.rectTransform;
            Vector3 offset = _cardArea.parent.InverseTransformVector(
                promptRect.TransformVector(Vector3.up * promptRect.rect.height));
            RectTransform titleRect = eventNameText.rectTransform;
            float titleBottom = _cardArea.parent.InverseTransformPoint(
                titleRect.TransformPoint(new Vector3(0f, titleRect.rect.yMin, 0f))).y;
            float cardTop = _cardArea.parent.InverseTransformPoint(
                _cardArea.TransformPoint(new Vector3(0f, _cardArea.rect.yMax, 0f))).y;
            // 프리팹에서 제목을 낮춘 경우에도 카드가 제목 영역까지 올라가지 않도록 제한합니다.
            float shift = Mathf.Clamp(offset.y, 0f, Mathf.Max(0f, titleBottom - cardTop));
            _cardArea.anchoredPosition = _cardAreaPosition + Vector2.up * shift;
        }

        internal void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            RefreshInteractability();
        }

        /// <summary>키보드·컨트롤러 Submit이 같은 수신자로 오도록 첫 카드에 초점을 둡니다. 카드가 없으면 그대로입니다.</summary>
        internal void FocusFirstCard()
        {
            EventSystem events = EventSystem.current;
            if (events != null && _visibleCardCount > 0) events.SetSelectedGameObject(cards[0].ActionButton.gameObject);
        }

        internal void ShowError(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                HideError();
                return;
            }

            errorText.text = message;
            errorGroup.alpha = 1f;
            errorPanel.SetActive(true);
            _errorElapsed = 0f;
            enabled = true;
        }

        private void Update()
        {
            _errorElapsed += Time.unscaledDeltaTime;
            float fading = _errorElapsed - errorHoldSeconds;
            if (fading < 0f) return;
            if (fading >= errorFadeSeconds)
            {
                HideError();
                return;
            }
            errorGroup.alpha = 1f - fading / errorFadeSeconds;
        }

        private void HideError()
        {
            if (errorPanel != null) errorPanel.SetActive(false);
            enabled = false;
        }

        private void RenderCards(IReadOnlyList<StageOfferCardContent> contents)
        {
            _visibleCardCount = contents.Count;
            float step = ((RectTransform)cards[0].transform).rect.width + cardSpacing;
            for (int i = 0; i < _visibleCardCount; i++)
            {
                StageOfferCardView card = cards[i];
                card.Render(contents[i]);
                var rect = (RectTransform)card.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2((i - (_visibleCardCount - 1) * .5f) * step, 0f);
                card.Open();
            }

            for (int i = _visibleCardCount; i < cards.Count; i++)
            {
                cards[i].SetCanSubmit(false);
                cards[i].Hide();
            }

            FitScroll(step);
        }

        // 넘칠 때만 스크롤을 켭니다. 카드 수가 같으면(거절 안내 뒤 다시 그림) 보던 자리를 유지합니다.
        private void FitScroll(float step)
        {
            if (cardScroll == null) return;
            RectTransform content = cardScroll.content;
            float width = Mathf.Max(0f, _visibleCardCount * step - cardSpacing);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            bool overflows = width * content.localScale.x > cardScroll.viewport.rect.width;
            bool countChanged = _fittedCardCount != _visibleCardCount;
            _fittedCardCount = _visibleCardCount;
            cardScroll.StopMovement();
            cardScroll.enabled = overflows;
            if (!overflows) content.anchoredPosition = Vector2.zero;
            else if (countChanged) cardScroll.horizontalNormalizedPosition = 0f;
        }

        private void RefreshInteractability()
        {
            bool canExit = _interactable && _canExit;
            if (exitButton != null) exitButton.interactable = canExit;
            if (!canExit) _exitBinding?.OnHoverExit();
            for (int i = 0; i < cards.Count; i++)
                cards[i].SetCanSubmit(_interactable && i < _visibleCardCount);
        }

        void IUIInputReceiver.Click(int slot)
        {
            // 수신자 연결 전·해제 뒤 입력은 정상적으로 무시합니다(바인딩 전 대기).
            if (!_interactable || _input == null) return;
            if (slot == ExitSlot)
            {
                if (_canExit) _input.Exit();
                return;
            }
            if (slot < 0 || slot >= _visibleCardCount || !cards[slot].IsButtonEnabled) return;
            _input.SelectCard(slot);
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }
    }
}
