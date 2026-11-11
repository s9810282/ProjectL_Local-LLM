using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>DeckCardList 내부 네 카드의 부채꼴 배치·호버 보간만 소유합니다. 화면 등록과 입력 판단은 하지 않습니다.</summary>
    [Serializable]
    public sealed class DeckCardFanLayout
    {
        [Tooltip("체크하면 좌측 하단, 해제하면 중앙 하단에 배치합니다.")]
        [SerializeField] private bool alignBottomLeft = false;
        [Tooltip("UnitCard와 해당 입력 영역에만 적용하는 Y 오프셋입니다. 양수는 위, 음수는 아래로 이동합니다.")]
        [SerializeField] private float yOffset = 0f;
        [SerializeField, Range(0f, 40f)] private float outerAngle = 22f;
        [SerializeField, Range(0f, 0.6f)] private float overlap = 0.25f;
        [SerializeField, Range(0f, 0.3f)] private float archHeightRatio = 0.1f;
        [SerializeField, Range(0f, 0.6f)] private float hoverLiftRatio = 0.3f;
        [SerializeField, Range(1f, 1.5f)] private float hoverScale = 1.5f;
        [SerializeField, Min(0.01f)] private float hoverDuration = 0.16f;
        [SerializeField, Min(0f)] private float edgePadding = 12f;

        private sealed class Slot
        {
            internal RectTransform Card;
            internal RectTransform HitRect;
            internal Image HitImage;
            internal Button HitButton;
            internal Button CardButton;
            internal DeckCardHoverBinding Hover;
            internal Vector3 Scale;
            internal float Blend;
        }

        private RectTransform _panel;
        private RectTransform _root;
        private CanvasGroup _fanGroup;
        private Slot[] _slots;
        private float _leftX;
        private int _hovered = -1;

        internal Button HitButton(int slot) => _slots[slot].HitButton;
        internal DeckCardHoverBinding HoverBinding(int slot) => _slots[slot].Hover;

        internal void Initialize(RectTransform panel, IReadOnlyList<UnitCardUI> cards)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            if (_slots != null) return;
            ValidateSettings();
            ValidateCards(panel, cards, out float width, out float height);
            ValidatePanelWidth(panel, width, height);

            _panel = panel;
            _leftX = panel.anchoredPosition.x;
            int rootIndex = cards[0].transform.GetSiblingIndex();
            _root = new GameObject("CardFan", typeof(RectTransform)).GetComponent<RectTransform>();
            _root.gameObject.layer = panel.gameObject.layer;
            _root.SetParent(panel, false);
            _root.SetSiblingIndex(rootIndex);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;
            _fanGroup = _root.gameObject.AddComponent<CanvasGroup>();
            _slots = new Slot[cards.Count];

            for (int i = 0; i < cards.Count; i++)
            {
                RectTransform card = (RectTransform)cards[i].transform;
                var hitObject = new GameObject($"CardSlot_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                hitObject.layer = card.gameObject.layer;
                var hitRect = hitObject.GetComponent<RectTransform>();
                hitRect.SetParent(_root, false);
                hitRect.anchorMin = hitRect.anchorMax = Vector2.zero;
                var image = hitObject.GetComponent<Image>();
                image.color = Color.clear;
                image.canvasRenderer.cullTransparentMesh = false;
                var button = hitObject.GetComponent<Button>();
                button.targetGraphic = image;
                button.transition = Selectable.Transition.None;
                // 키 입력의 선택 대상은 기존 카드 Button입니다. 배경은 포인터 입력만 이어 줍니다.
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var slot = new Slot
                {
                    Card = card,
                    CardButton = card.GetComponent<Button>(),
                    HitRect = hitRect,
                    HitImage = image,
                    HitButton = button,
                    Hover = hitObject.AddComponent<DeckCardHoverBinding>(),
                    Scale = card.localScale
                };
                _slots[i] = slot;
                card.SetParent(hitRect, false);
                card.pivot = new Vector2(0.5f, 0f);
            }
            Tick(0f);
        }

        internal void SetHovered(int slot)
        {
            if (slot < -1 || slot > 3) throw new ArgumentOutOfRangeException(nameof(slot));
            if (_slots == null || _hovered == slot) return;
            _hovered = slot;
            for (int i = 0; i < _slots.Length; i++) _slots[i].HitRect.SetSiblingIndex(i);
            if (slot >= 0) _slots[slot].HitRect.SetAsLastSibling();
        }

        internal void ResetSlot(int slot)
        {
            if (_slots == null) return; // Awake 전 숨김은 정상적인 UI 초기화 경계입니다.
            _slots[slot].Hover.ResetHover();
            _slots[slot].Blend = 0f;
        }

        internal void Reset()
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++) ResetSlot(i);
            SetHovered(-1);
            Tick(0f);
        }

        internal void Tick(float deltaTime, DeckCardEntrance entrance = null)
        {
            if (_slots == null) return;
            bool entering = entrance?.IsPlaying == true;
            _fanGroup.interactable = _fanGroup.blocksRaycasts = !entering;
            _fanGroup.alpha = entrance?.Alpha ?? 1f;
            ValidateSettings();
            float width = 0f;
            float height = 0f;
            for (int i = 0; i < _slots.Length; i++)
            {
                width = Mathf.Max(width, _slots[i].Card.rect.width * _slots[i].Scale.x);
                height = Mathf.Max(height, _slots[i].Card.rect.height * _slots[i].Scale.y);
            }
            ValidatePanelWidth(_panel, width, height);
            CalculateArrangement(_panel, width, height, out float spacing, out float centerX, out float lowerExtent);
            ApplyAlignment();
            float radians = outerAngle * Mathf.Deg2Rad;
            float lift = height * hoverLiftRatio;
            // 배경 Graphic은 이동 전체를 덮되 실제 입력 영역은 원래 카드와 현재 카드의 합집합으로 제한합니다.
            Vector2 hitSize = new Vector2((width + height * 2f * Mathf.Sin(radians)) * hoverScale,
                height * hoverScale + lift + lowerExtent * 2f);
            Vector2 hitPivot = new Vector2(0.5f, lowerExtent / hitSize.y);
            var firstPose = new DeckCardEntrance.Pose(PositionForSlot(0, spacing, centerX, lowerExtent, height), AngleForSlot(0));
            Vector2 cardSize = new Vector2(width, height);

            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                float angle = AngleForSlot(i);
                if (slot.HitRect.sizeDelta != hitSize) slot.HitRect.sizeDelta = hitSize;
                if (slot.HitRect.pivot != hitPivot) slot.HitRect.pivot = hitPivot;
                Vector2 slotPosition = PositionForSlot(i, spacing, centerX, lowerExtent, height);
                if (slot.HitRect.anchoredPosition != slotPosition) slot.HitRect.anchoredPosition = slotPosition;
                if (slot.Card.anchorMin != hitPivot) slot.Card.anchorMin = hitPivot;
                if (slot.Card.anchorMax != hitPivot) slot.Card.anchorMax = hitPivot;
                bool visible = slot.Card.gameObject.activeInHierarchy;
                slot.HitImage.raycastTarget = visible;
                slot.HitButton.interactable = slot.CardButton.IsInteractable();
                float target = !entering && visible && _hovered == i ? 1f : 0f;
                slot.Blend = Mathf.MoveTowards(slot.Blend, target, Mathf.Max(0f, deltaTime) / hoverDuration);
                float blend = Mathf.SmoothStep(0f, 1f, slot.Blend);
                Vector2 position = new Vector2(0f, lift * blend);
                float displayAngle = angle * (1f - blend);
                if (entering)
                {
                    var restPose = new DeckCardEntrance.Pose(slotPosition, angle);
                    DeckCardEntrance.Pose pose = entrance.Evaluate(i, restPose, firstPose, cardSize);
                    position += pose.Position - slotPosition;
                    displayAngle = pose.Angle;
                }
                Quaternion rotation = Quaternion.Euler(0f, 0f, displayAngle);
                Vector3 scale = slot.Scale * Mathf.Lerp(1f, hoverScale, blend);
                if (slot.Card.anchoredPosition != position) slot.Card.anchoredPosition = position;
                if (slot.Card.localRotation != rotation) slot.Card.localRotation = rotation;
                if (slot.Card.localScale != scale) slot.Card.localScale = scale;
                slot.Hover.ConfigureHitArea(slot.Card, slot.HitRect, slot.Card.rect.size * (Vector2)slot.Scale, angle);
            }
        }

#if UNITY_EDITOR
        /// <summary>런타임 슬롯·입력·서비스를 만들지 않고 기존 카드의 기본 배치만 미리 적용합니다.</summary>
        internal bool ApplyEditorPreview(RectTransform panel, IReadOnlyList<UnitCardUI> cards)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            if (cards == null) throw new ArgumentNullException(nameof(cards));
            ValidateSettings();
            ValidateCards(panel, cards, out float width, out float height);
            ValidatePanelWidth(panel, width, height);
            if (!(panel.parent is RectTransform parent))
                throw new InvalidOperationException($"[DeckCardFanLayout] UIId.UnitCard, '{panel.name}': 배치 미리보기에는 부모 RectTransform이 필요합니다.");

            CalculateArrangement(panel, width, height, out float spacing, out float centerX, out float lowerExtent);
            // 편집 중 패널 X를 덮어쓰면 Left 복귀 위치가 사라집니다. 런타임 정렬 후 위치를 카드 좌표로 환산합니다.
            float targetPivotX = alignBottomLeft ? 0f : 0.5f;
            Vector3 targetPanelPosition = panel.localPosition;
            targetPanelPosition.x = Mathf.Lerp(parent.rect.xMin, parent.rect.xMax, targetPivotX) +
                (alignBottomLeft ? panel.anchoredPosition.x : 0f);
            Vector2 alignmentOffset = panel.InverseTransformPoint(parent.TransformPoint(targetPanelPosition));
            alignmentOffset.x += panel.rect.width * (panel.pivot.x - targetPivotX);
            bool changed = false;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = (RectTransform)cards[i].transform;
                Vector2 position = PositionForSlot(i, spacing, centerX, lowerExtent, height) + alignmentOffset;
                Quaternion rotation = Quaternion.Euler(0f, 0f, AngleForSlot(i));
                Vector2 pivot = new Vector2(0.5f, 0f);
                if (card.anchorMin == Vector2.zero && card.anchorMax == Vector2.zero && card.pivot == pivot &&
                    card.anchoredPosition == position && card.localRotation == rotation) continue;
                card.anchorMin = card.anchorMax = Vector2.zero;
                card.pivot = pivot;
                card.anchoredPosition = position;
                card.localRotation = rotation;
                changed = true;
            }
            return changed;
        }
#endif

        private void CalculateArrangement(RectTransform panel, float width, float height,
            out float spacing, out float centerX, out float lowerExtent)
        {
            float radians = outerAngle * Mathf.Deg2Rad;
            float outerExtent = width * 0.5f * Mathf.Cos(radians) + height * Mathf.Sin(radians);
            float available = panel.rect.width - edgePadding * 2f;
            spacing = Mathf.Min(width * (1f - overlap), Mathf.Max(0f, (available - outerExtent * 2f) / 3f));
            centerX = alignBottomLeft ? edgePadding + outerExtent + spacing * 1.5f : panel.rect.width * 0.5f;
            lowerExtent = width * 0.5f * Mathf.Sin(radians);
        }

        private Vector2 PositionForSlot(int slot, float spacing, float centerX, float lowerExtent, float height)
        {
            float t = slot / 3f * 2f - 1f;
            return new Vector2(centerX + t * spacing * 1.5f,
                yOffset + lowerExtent + height * archHeightRatio * (1f - t * t));
        }

        private float AngleForSlot(int slot) => -(slot / 3f * 2f - 1f) * outerAngle;

        private void ValidateCards(RectTransform panel, IReadOnlyList<UnitCardUI> cards, out float width, out float height)
        {
            if (cards.Count != 4 || panel.rect.width <= edgePadding * 2f)
                throw new InvalidOperationException($"[DeckCardFanLayout] UIId.UnitCard, '{panel.name}': 카드 네 장과 여백보다 넓은 패널이 필요합니다.");
            // 모든 참조를 검증한 뒤에만 카드의 부모·배치를 변경합니다.
            width = height = 0f;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || !(cards[i].transform is RectTransform rect) ||
                    rect.parent != panel || rect.rect.width <= 0f || rect.rect.height <= 0f ||
                    rect.localScale.x <= 0f || rect.localScale.y <= 0f || cards[i].GetComponent<Button>() == null)
                    throw new InvalidOperationException($"[DeckCardFanLayout] UIId.UnitCard, '{panel.name}', slot {i}: 패널의 직접 자식인 유효한 RectTransform·Button 카드가 필요합니다.");
                for (int previous = 0; previous < i; previous++)
                    if (cards[previous] == cards[i])
                        throw new InvalidOperationException($"[DeckCardFanLayout] UIId.UnitCard, '{panel.name}': slot {previous}와 {i}가 같은 카드 '{cards[i].name}'을 참조합니다.");
                width = Mathf.Max(width, rect.rect.width * rect.localScale.x);
                height = Mathf.Max(height, rect.rect.height * rect.localScale.y);
            }
        }

        private void ApplyAlignment()
        {
            float x = alignBottomLeft ? 0f : 0.5f;
            Vector2 min = _panel.anchorMin;
            Vector2 max = _panel.anchorMax;
            Vector2 pivot = _panel.pivot;
            min.x = max.x = pivot.x = x;
            if (_panel.anchorMin != min) _panel.anchorMin = min;
            if (_panel.anchorMax != max) _panel.anchorMax = max;
            if (_panel.pivot != pivot) _panel.pivot = pivot;
            Vector2 position = _panel.anchoredPosition;
            position.x = alignBottomLeft ? _leftX : 0f;
            if (_panel.anchoredPosition != position) _panel.anchoredPosition = position;
        }

        private void ValidatePanelWidth(RectTransform panel, float width, float height)
        {
            float radians = outerAngle * Mathf.Deg2Rad;
            float required = width * Mathf.Cos(radians) + height * 2f * Mathf.Sin(radians) + edgePadding * 2f;
            if (panel.rect.width < required)
                throw new InvalidOperationException($"[DeckCardFanLayout] UIId.UnitCard, '{panel.name}': 패널 너비 {panel.rect.width}가 outerAngle·카드 크기·edgePadding에 필요한 최소 너비 {required}보다 작습니다.");
        }

        private void ValidateSettings()
        {
            RequireRange(nameof(yOffset), yOffset, -float.MaxValue, float.MaxValue);
            RequireRange(nameof(outerAngle), outerAngle, 0f, 40f);
            RequireRange(nameof(overlap), overlap, 0f, 0.6f);
            RequireRange(nameof(archHeightRatio), archHeightRatio, 0f, 0.3f);
            RequireRange(nameof(hoverLiftRatio), hoverLiftRatio, 0f, 0.6f);
            RequireRange(nameof(hoverScale), hoverScale, 1f, 1.5f);
            RequireRange(nameof(hoverDuration), hoverDuration, 0.01f, float.MaxValue);
            RequireRange(nameof(edgePadding), edgePadding, 0f, float.MaxValue);
        }

        private static void RequireRange(string field, float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < min || value > max)
                throw new InvalidOperationException($"[DeckCardFanLayout] UIId.UnitCard: {field}={value}, 허용 범위는 {min}~{max}입니다.");
        }
    }
}
