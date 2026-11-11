using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>덱 카드 네 장(0번 = 메인)을 소유합니다. 배치 여부와 무관하게 덱(전투 중에는 참가 목록)을 표시합니다.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckCardList : UIGroup, IUIInputReceiver, IDeckCardHoverReceiver
    {
        private const int CardCount = 4;
        private const int MainSlot = 0;
        private const float RestY = 30f;
        private const float DragY = -215f;
        private const float SlideDuration = 0.2f;
        [SerializeField] private CombatCardInfoUI infoPanel;
        [SerializeField] private DeckCardFanLayout fanLayout = new();
        [SerializeField] private DeckCardEntrance entrance = new();
        private RectTransform _panelRect;
        private bool _placementDragging;
        private float _slideFromY;
        private float _slideTargetY = RestY;
        private float _slideElapsed = SlideDuration;
        private readonly List<UnitCardUI> _cards = new();
        private readonly List<Button> _buttons = new();
        private readonly CharacterInstanceId[] _shown = new CharacterInstanceId[CardCount];
        private IRunDeckReadSink _deck;
        private ICombatStateBufferReadSink _combatState;
        private ICombatViewService _combat;
        private ICombatDeckPlacementInput _assignedPlacementInput;
        private bool _prepared;
        private int _hoverSlot = -1;
        private ICombatDeckHoverInput _assignedHoverInput;
        private ICombatDeckHoverInput HoverInput => _assignedHoverInput ?? _combat?.DeckHover;

        void IDeckCardHoverReceiver.EnterCard(int slot)
        {
            if (!isActiveAndEnabled || IsEntrancePlaying || slot < 0 || slot >= CardCount) return;
            _hoverSlot = slot;
            SyncHover();
        }

        void IDeckCardHoverReceiver.ExitCard(int slot)
        {
            if (_hoverSlot == slot) ClearHover();
        }

        private void SyncHover()
        {
            fanLayout.SetHovered(_placementDragging ? -1 : _hoverSlot);
            if (_hoverSlot >= 0)
                HoverInput?.Hover(_placementDragging ? CharacterInstanceId.Empty : _shown[_hoverSlot]);
        }

        private void ClearHover()
        {
            if (_hoverSlot < 0) return;
            _hoverSlot = -1;
            fanLayout.SetHovered(-1);
            HoverInput?.Hover(CharacterInstanceId.Empty);
        }

        internal void BindHoverInput(ICombatDeckHoverInput input)
        {
            ClearHover();
            fanLayout.Reset();
            _assignedHoverInput = input;
        }


        public DeckCardList() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.UnitCard;
        public override UIId ParentID => UIId.Deck;

        internal bool IsEntrancePlaying => entrance?.IsPlaying == true;

        /// <summary>UISystem에서 열기 전에 선택 모드의 시작 자세·투명도를 준비합니다. 최초 진입 조합만 호출합니다.</summary>
        internal void PrepareEntrance()
        {
            PrepareCards();
            ResetSlide();
            ClearHover();
            fanLayout.Reset();
            entrance.Begin(PanelRect, CardCount);
            fanLayout.Tick(0f, entrance);
        }

        /// <summary>최초 진입 Tick이 시간 진행을 전담합니다. 일반 Update와 시간을 중복 소비하지 않습니다.</summary>
        internal int TickEntrance(float deltaTime)
        {
            if (!IsEntrancePlaying) return 0;
            int startedCards = entrance.Tick(deltaTime);
            fanLayout.Tick(0f, entrance);
            return startedCards;
        }

        internal void CancelEntrance()
        {
            entrance?.Reset();
            fanLayout?.Reset();
        }

        private ICombatDeckPlacementInput Placement => _assignedPlacementInput ?? _combat?.DeckPlacement;

        private void Awake() => PrepareCards();
#if UNITY_EDITOR
        /// <summary>전용 Inspector에서만 쓰는 편집 배치입니다. UIGroup 초기화·서비스 바인딩을 실행하지 않습니다.</summary>
        public bool ApplyEditorLayoutPreview(IReadOnlyList<UnitCardUI> cards)
        {
            if (Application.IsPlaying(gameObject))
                throw new InvalidOperationException($"[DeckCardList] UIId.UnitCard, '{name}': 편집 미리보기는 Play Mode 인스턴스에 적용할 수 없습니다.");
            if (fanLayout == null)
                throw new InvalidOperationException($"[DeckCardList] UIId.UnitCard, '{name}': fanLayout 설정이 필요합니다.");
            return fanLayout.ApplyEditorPreview(PanelRect, cards);
        }
#endif

        protected override void OnUIHidden()
        {
            ResetSlide();
            ClearHover();
            CancelEntrance();
            Placement?.Cancel();
        }
        private void Update()
        {
            RefreshDeck();
            if (IsEntrancePlaying)
            {
                fanLayout.Tick(0f, entrance);
                return;
            }
            AnimateSlide(Time.unscaledDeltaTime);
            fanLayout.Tick(Time.unscaledDeltaTime);
        }

        /// <summary>덱·필드 공통 배치 입력 상태에 맞춰 패널을 움직입니다.</summary>
        internal void AnimateSlide(float deltaTime)
        {
            bool dragging = Placement?.IsDragging == true;
            if (_placementDragging != dragging)
            {
                _placementDragging = dragging;
                SlideTo(dragging ? DragY : RestY);
                SyncHover();
            }
            if (_slideElapsed >= SlideDuration) return;
            _slideElapsed = Mathf.Min(_slideElapsed + deltaTime, SlideDuration);
            SetPanelY(Mathf.Lerp(_slideFromY, _slideTargetY,
                Mathf.SmoothStep(0f, 1f, _slideElapsed / SlideDuration)));
        }

        private void SlideTo(float targetY)
        {
            _slideFromY = PanelRect.anchoredPosition.y;
            _slideTargetY = targetY;
            _slideElapsed = 0f;
        }

        private RectTransform PanelRect
        {
            get
            {
                if (_panelRect == null)
                    _panelRect = transform as RectTransform ??
                        throw new InvalidOperationException("[DeckCardList] UIId.UnitCard 패널에 RectTransform이 필요합니다.");
                return _panelRect;
            }
        }

        private void SetPanelY(float y)
        {
            Vector2 position = PanelRect.anchoredPosition;
            position.y = y;
            PanelRect.anchoredPosition = position;
        }

        private void ResetSlide()
        {
            // 드래그하지 않은 패널은 기존 배치 값을 유지합니다.
            if (!_placementDragging && _slideElapsed >= SlideDuration && _slideTargetY == RestY) return;
            _placementDragging = false;
            _slideTargetY = RestY;
            _slideElapsed = SlideDuration;
            SetPanelY(RestY);
        }

        private void PrepareCards()
        {
            if (_prepared) return;
            if (infoPanel == null)
                throw new InvalidOperationException($"[DeckCardList] UIId.UnitCard, '{name}': 공용 infoPanel 참조가 필요합니다.");
            if (fanLayout == null)
                throw new InvalidOperationException($"[DeckCardList] UIId.UnitCard, '{name}': fanLayout 설정이 필요합니다.");
            if (entrance == null)
                throw new InvalidOperationException($"[DeckCardList] UIId.UnitCard, '{name}': entrance 설정이 필요합니다.");
            infoPanel.Validate();
            _cards.Clear();
            foreach (UIBase member in Instances)
            {
                if (!(member is UnitCardUI card))
                    throw new InvalidOperationException("[DeckCardList] 멤버는 UnitCardUI여야 합니다.");
                _cards.Add(card);
            }
            _cards.Reverse();
            if (_cards.Count != CardCount)
                throw new InvalidOperationException("[DeckCardList] 카드 멤버는 서브 세 장과 메인 한 장이어야 합니다.");
            for (int i = 0; i < _cards.Count; i++) _cards[i].View.ValidateInfoInput();
            // 부채꼴 설정·카드 참조는 입력을 연결하거나 부모를 변경하기 전에 전체 검증합니다.
            fanLayout.Initialize(PanelRect, _cards);
            _buttons.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                Button button = _cards[i].GetComponent<Button>();
                if (button == null)
                    throw new InvalidOperationException("[DeckCardList] 카드 본체에 Button이 필요합니다.");
                _buttons.Add(button);
                _cards[i].BindInfoReceiver(infoPanel);
                UIInputBinding.Attach(button, this, i);
                UIInputBinding.Attach(fanLayout.HitButton(i), this, i);
                fanLayout.HoverBinding(i).Bind(this, i);
                var drag = button.GetComponent<DeckCardDragBinding>();
                if (drag == null) drag = button.gameObject.AddComponent<DeckCardDragBinding>();
                drag.Bind(this, i);
                var hitDrag = fanLayout.HitButton(i).GetComponent<DeckCardDragBinding>();
                if (hitDrag == null) hitDrag = fanLayout.HitButton(i).gameObject.AddComponent<DeckCardDragBinding>();
                hitDrag.Bind(this, i);
            }
            _prepared = true;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            PrepareCards();
            entrance.Reset();
            ClearHover();
            fanLayout.Reset();
            base.BindUIService(resolver);
            var buffer = resolver.GetService(BufferSystemKey.Buffer);
            _deck = buffer.RunDeckReadSink;
            _combatState = buffer.CombatStateReadSink;
            _combat = resolver.GetService(SystemGroupsService.InGame).ViewGroup?.Combat;
        }

        public override void Open()
        {
            PrepareCards();
            base.Open();
            RefreshDeck();
            fanLayout.Tick(0f, entrance);
        }

        public override void Hide()
        {
            ClearHover();
            Placement?.Cancel();
            base.Hide();
        }

        /// <summary>덱 순서대로 슬롯을 채우고 남는 슬롯은 비웁니다. 메인은 Core가 0번(<see cref="MainSlot"/>)에 둡니다. 크기는 프리팹, 배치·호버는 fanLayout을 사용합니다.</summary>
        public void RefreshDeck()
        {
            PrepareCards();
            IReadOnlyList<CharacterInstanceId> ids = CurrentDeckIds();
            int count = ids == null ? 0 : Math.Min(ids.Count, CardCount);
            for (int i = 0; i < count; i++) ShowCard(i, ids[i]);
            for (int i = count; i < CardCount; i++) ClearCard(i);
            SyncHover();
        }

        /// <summary>
        /// 전투 중에는 진입 때 포착한 참가 목록을, 전투 밖에서는 확정 덱을 씁니다. 덱이 없으면 null(모두 비움)입니다.
        /// 보유 목록에서 역할로 메인을 찾지 않습니다(런 덱 Core → View 인계 §4).
        /// </summary>
        private IReadOnlyList<CharacterInstanceId> CurrentDeckIds()
        {
            RunDeckSnapshot membership = _combatState?.Current?.Membership;
            if (membership != null) return membership.OrderedIds;
            return _deck != null && _deck.TryGetCurrent(out RunDeckSnapshot deck) ? deck.OrderedIds : null;
        }

        private void ShowCard(int slot, CharacterInstanceId ownedId)
        {
            UnitCardUI card = _cards[slot];
            if (_shown[slot] != ownedId)
            {
                if (_hoverSlot == slot) ClearHover();
                fanLayout.ResetSlot(slot);
                _shown[slot] = ownedId;
                card.SetOwnedCharacter(ownedId);
            }
            if (!card.gameObject.activeSelf) card.Open();
            _buttons[slot].interactable = !IsEntrancePlaying && Placement?.CanSelect(ownedId) == true;
        }

        private void ClearCard(int slot)
        {
            if (_hoverSlot == slot) ClearHover();
            fanLayout.ResetSlot(slot);
            _shown[slot] = CharacterInstanceId.Empty;
            _cards[slot].View.Clear();
            _cards[slot].Hide();
            _buttons[slot].interactable = false;
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled || IsEntrancePlaying || slot < 0 || slot >= CardCount) return;
            ICombatDeckPlacementInput input = Placement;
            if (input?.CanSelect(_shown[slot]) == true)
            {
                // 고정 입력 영역에서 눌러도 이후 Submit은 같은 실제 카드 Button으로 이어집니다.
                EventSystem events = EventSystem.current;
                if (events != null && events.currentSelectedGameObject != _buttons[slot].gameObject)
                    events.SetSelectedGameObject(_buttons[slot].gameObject);
                input.Select(_shown[slot]);
            }
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
        }
        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        internal void BindPlacementInput(ICombatDeckPlacementInput input) => _assignedPlacementInput = input;
    }
}
