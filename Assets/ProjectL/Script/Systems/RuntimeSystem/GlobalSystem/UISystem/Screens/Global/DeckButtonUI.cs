using System;
using System.Collections.Generic;
using TMPro;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems
{
    public sealed class DeckButtonUI : GlobalUIBase, IUIInputReceiver
    {
        [InspectorName("Deck Button")]
        [Tooltip("Global UI에서 덱 패널을 여는 버튼입니다. DeckButtonUI가 붙은 오브젝트의 Button도 연결할 수 있습니다.")]
        [SerializeField] private Button openButton;
        [Tooltip("DeckButtonUI 루트 아래의 별도 패널을 연결합니다.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private ScrollRect scrollView;
        [InspectorName("Character Button Prefab")]
        [Tooltip("목록에 복제할 캐릭터 Button입니다. 바깥 Deck Button과 별도로 연결하며 DeckButtonUI를 붙이지 않습니다. Content 안의 비활성 템플릿도 사용할 수 있습니다.")]
        [SerializeField] private Button deckButtonPrefab;
        [Tooltip("덱 항목의 Hover/Click으로 표시할 공용 UnitCardUI입니다. ScrollRect Content 바깥에 배치합니다.")]
        [SerializeField] private UnitCardUI unitCard;
        [SerializeField] private Vector2 spacing = new Vector2(8, 8);

        private ICharacterBufferReadSink _characters;
        private IUnitVisualTableQuery _unitVisuals;
        private readonly List<Button> _buttons = new();
        private readonly List<Image> _portraits = new();
        private readonly List<TMP_Text> _names = new();
        private readonly List<OwnedCharacterRecord> _displayed = new();
        private RectTransform _content;
        private bool _panelOpen;
        private CharacterInstanceId _shownCharacter;
        private bool _cardPinned;
        private Button _outsideClickArea;
        private Transform _deckLayer;
        private int _deckSiblingIndex = -1;

        public override UIId ID => UIId.Deck;

        private void Awake()
        {
            EnsureUnitCardInstance();
            UIInputBinding.Attach(openButton, this, -1);
            ClosePanel();
        }

        private void OnDisable() => ClosePanel();

        private void OnDestroy()
        {
            if (_outsideClickArea == null) return;
            if (Application.isPlaying) Destroy(_outsideClickArea.gameObject);
            else DestroyImmediate(_outsideClickArea.gameObject);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            EnsureUnitCardInstance();
            _characters = resolver.GetService(BufferSystemKey.Buffer).CharacterReadSink;
            if (unitCard != null) unitCard.BindUIService(resolver);
            if (_panelOpen) RefreshDeck();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            EnsureUnitCardInstance();
            _unitVisuals = (query as IHasViewQuery)?.ViewQuery.UnitVisualTableQuery;
            if (unitCard != null) unitCard.GetUIData(query);
            if (_panelOpen) RefreshDeck();
        }

        private void EnsureUnitCardInstance()
        {
            if (unitCard == null || unitCard.gameObject.scene.IsValid()) return;
            // 프리팹 에셋 참조는 표시 대상이 아니다. 유지 Canvas 아래에 한 번만 생성한다.
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                throw new InvalidOperationException("[DeckButtonUI] UnitCard prefab requires a parent Canvas.");
            unitCard = Instantiate(unitCard, canvas.transform, false);
            unitCard.Hide();
        }

        public override void Open() => gameObject.SetActive(true);

        public override void Hide()
        {
            ClosePanel();
            gameObject.SetActive(false);
        }

        public void OpenPanel()
        {
            if (panel == null || scrollView == null || scrollView.content == null || deckButtonPrefab == null)
            {
                Debug.LogWarning("[DeckButtonUI] Panel, Scroll View의 Content, Button 프리팹을 연결해주세요.", this);
                return;
            }
            if (panel == gameObject || !panel.transform.IsChildOf(transform))
            {
                Debug.LogWarning("[DeckButtonUI] Panel은 Global UI 루트 아래의 별도 오브젝트여야 합니다.", this);
                return;
            }

            bool wasOpen = _panelOpen;
            if (panel.GetComponent<UIPointerClickBoundary>() == null)
                panel.AddComponent<UIPointerClickBoundary>();
            var panelGraphic = panel.GetComponent<Graphic>();
            if (panelGraphic == null)
            {
                var background = panel.AddComponent<Image>();
                background.color = Color.clear;
                panelGraphic = background;
            }
            panelGraphic.raycastTarget = true;
            ConfigureContent();
            _panelOpen = true;
            panel.SetActive(true);
            ShowOutsideClickArea();
            RefreshDeck();
            if (!wasOpen) scrollView.verticalNormalizedPosition = 1f;
        }

        public void ClosePanel()
        {
            _panelOpen = false;
            if (_outsideClickArea != null) _outsideClickArea.gameObject.SetActive(false);
            if (_deckLayer != null && _deckSiblingIndex >= 0)
            {
                _deckLayer.SetSiblingIndex(_deckSiblingIndex);
                _deckSiblingIndex = -1;
            }
            CloseUnitCard();
            if (panel != null && panel != gameObject) panel.SetActive(false);
        }

        public void CloseUnitCard()
        {
            _shownCharacter = CharacterInstanceId.Empty;
            _cardPinned = false;
            if (unitCard != null) unitCard.Hide();
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == -1)
            {
                if (_panelOpen) ClosePanel();
                else OpenPanel();
            }
            else if (slot == -2) ClosePanel();
            else if (slot >= 0) ClickCharacter(slot);
        }

        private void ShowOutsideClickArea()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            if (_outsideClickArea == null)
            {
                var area = new GameObject("DeckOutsideClickArea", typeof(RectTransform), typeof(Image), typeof(Button));
                area.layer = gameObject.layer;
                var rect = (RectTransform)area.transform;
                rect.SetParent(canvas.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var image = area.GetComponent<Image>();
                image.color = Color.clear;
                _outsideClickArea = area.GetComponent<Button>();
                _outsideClickArea.targetGraphic = image;
                _outsideClickArea.transition = Selectable.Transition.None;
                _outsideClickArea.navigation = new Navigation { mode = Navigation.Mode.None };
                UIInputBinding.Attach(_outsideClickArea, this, -2);
            }

            // 투명한 전체 화면 클릭 영역을 덱 바로 뒤에 두어 패널 내부 입력은 유지한다.
            _deckLayer = transform;
            while (_deckLayer.parent != null && _deckLayer.parent != canvas.transform && _deckLayer != canvas.transform)
                _deckLayer = _deckLayer.parent;
            if (_deckLayer == canvas.transform)
                _outsideClickArea.transform.SetAsFirstSibling();
            else
            {
                if (_deckSiblingIndex < 0) _deckSiblingIndex = _deckLayer.GetSiblingIndex();
                _outsideClickArea.transform.SetAsLastSibling();
                _deckLayer.SetAsLastSibling();
            }
            _outsideClickArea.gameObject.SetActive(true);
        }
        void IUIInputReceiver.HoverEnter(int slot)
        {
            if (slot >= 0) HoverCharacter(slot);
        }
        void IUIInputReceiver.HoverExit(int slot)
        {
            if (slot >= 0) ExitCharacter(slot);
        }
        private void HoverCharacter(int index)
        {
            if (!_panelOpen || unitCard == null || index >= _displayed.Count) return;
            // 클릭으로 고정한 유닛은 다른 버튼의 Hover로 교체하지 않는다.
            if (_cardPinned) return;
            ShowCharacter(_displayed[index].Id, false);
        }

        private void ExitCharacter(int index)
        {
            if (index < _displayed.Count && _shownCharacter == _displayed[index].Id && !_cardPinned)
                CloseUnitCard();
        }

        private void ClickCharacter(int index)
        {
            if (!_panelOpen || unitCard == null || index >= _displayed.Count) return;
            var owned = _displayed[index];
            if (_shownCharacter == owned.Id && _cardPinned && unitCard.isActiveAndEnabled) CloseUnitCard();
            else ShowCharacter(owned.Id, true);
        }

        private void ShowCharacter(CharacterInstanceId id, bool pinned)
        {
            _shownCharacter = id;
            _cardPinned = pinned;
            unitCard.SetOwnedCharacter(id);
            unitCard.Open();
            // 유지 Canvas의 바깥 클릭 영역보다 위에서 카드의 버튼 입력을 받는다.
            unitCard.transform.SetAsLastSibling();
        }

        private void Update()
        {
            // 현재 CharacterBuffer에는 변경 이벤트가 없다. 패널이 보일 때만 원본을 비교한다.
            if (_panelOpen && panel != null && panel.activeInHierarchy && DeckChanged()) RefreshDeck();
        }

        private bool DeckChanged()
        {
            int index = 0;
            if (_characters != null)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    var role = pass == 0 ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub;
                    foreach (var owned in _characters.Owned)
                    {
                        if (owned.Role != role) continue;
                        if (index >= _displayed.Count || !ReferenceEquals(owned, _displayed[index])) return true;
                        index++;
                    }
                }
            }
            return index != _displayed.Count;
        }

        public void RefreshDeck()
        {
            if (!_panelOpen || _content == null) return;
            _displayed.Clear();
            // 전투용 Characters는 전투 종료 시 비워진다. 지속 보유 목록에서 Main을 먼저 표시한다.
            if (_characters != null)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    var role = pass == 0 ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub;
                    foreach (var owned in _characters.Owned)
                    {
                        if (owned.Role == role) _displayed.Add(owned);
                    }
                }
                for (int index = 0; index < _displayed.Count; index++)
                {
                    var owned = _displayed[index];
                    if (index == _buttons.Count)
                    {
                        Button created = Instantiate(deckButtonPrefab, _content, false);
                        _buttons.Add(created);
                        // 프리팹의 표시 대상은 생성 시 한 번만 찾고 풀 슬롯과 함께 재사용한다.
                        _portraits.Add(created.transform.Find("Charactor")?.GetComponent<Image>());
                        _names.Add(created.transform.Find("Name")?.GetComponent<TMP_Text>());
                        // 풀 슬롯과 수신자를 저장하고 현재 목록에서 보유 ID를 읽는다.
                        UIInputBinding.Attach(created, this, index);
                    }

                    Button button = _buttons[index];
                    button.name = $"Deck_{owned.CharacterKey}_{owned.Id}";
                    string displayName = null;
                    Sprite icon = null;
                    if (_unitVisuals != null && _unitVisuals.TryGetUnitVisual(owned.CharacterKey, out var visual))
                    {
                        displayName = visual.DisplayName;
                        icon = visual.InfoIcon;
                    }
                    Image portrait = _portraits[index];
                    if (portrait != null)
                    {
                        portrait.sprite = icon;
                        portrait.enabled = icon != null;
                    }
                    TMP_Text nameText = _names[index];
                    if (nameText != null)
                        nameText.text = string.IsNullOrWhiteSpace(displayName) ? owned.CharacterKey.ToString() : displayName;
                    button.gameObject.SetActive(true);
                }
            }

            for (int i = _displayed.Count; i < _buttons.Count; i++)
                _buttons[i].gameObject.SetActive(false);
            if (!_shownCharacter.IsEmpty && !_displayed.Exists(owned => owned.Id == _shownCharacter))
                CloseUnitCard();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
        }

        private void ConfigureContent()
        {
            _content = scrollView.content;
            if (deckButtonPrefab.transform.parent == _content)
                deckButtonPrefab.gameObject.SetActive(false);
            var grid = _content.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = _content.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperLeft;
            // 버튼 크기는 제작된 프리팹을 기준으로 유지한다.
            grid.cellSize = ((RectTransform)deckButtonPrefab.transform).rect.size;
            grid.spacing = spacing;

            var fitter = _content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = _content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollView.horizontal = false;
            scrollView.vertical = true;
        }
    }
}
