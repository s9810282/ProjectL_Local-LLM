using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems
{
    /// <summary>Buffer의 타임라인 항목을 최대 8개의 재사용 Image로 표시합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class TimelineUI : UIBase, IUIInputReceiver
    {
        private const int MaxVisibleUnits = 8;

        [Tooltip("개별 유닛 Image 프리팹입니다. 크기는 프리팹의 RectTransform을 사용합니다.")]
        [SerializeField] private Image unitPrefab;
        [Tooltip("유닛들을 배치할 자식 영역입니다. 비워두면 TimelineUI의 RectTransform을 사용합니다.")]
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0f)] private float spacing;

        private sealed class Slot
        {
            internal Image Image;
            internal EntityId EntityId;
            internal Button Button;
            internal UIInputBinding Input;
        }

        private readonly List<Slot> _slots = new List<Slot>(MaxVisibleUnits);
        private ICombatStateBufferReadSink _combat;
        private ICharacterBufferReadSink _characters;
        private IUnitVisualTableQuery _unitVisuals;
        private long _battleInstanceId = -1;

        public override UIId ID => UIId.Timeline;
        public int VisibleUnitCount { get; private set; }

        private void Awake()
        {
            // 테두리는 입력을 받지 않습니다. 개별 유닛 Image만 Hover 대상입니다.
            var border = GetComponent<Image>();
            if (border != null) border.raycastTarget = false;
            HideTemplate();
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            ClearUnits();
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _combat = buffers.CombatStateReadSink;
            _characters = buffers.CharacterReadSink;
            _battleInstanceId = -1;
            RefreshTimeline();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _unitVisuals = (query as IHasViewQuery)?.ViewQuery.UnitVisualTableQuery;
            RefreshTimeline();
        }

        public override void Open()
        {
            gameObject.SetActive(true);
            RefreshTimeline();
        }

        public override void Hide()
        {
            ClearUnits();
            gameObject.SetActive(false);
        }

        private void Update() => RefreshTimeline();
        private void OnDisable() => ClearUnits();

        public void RefreshTimeline()
        {
            HideTemplate();
            if (_combat != null && _battleInstanceId != _combat.BattleInstanceId)
            {
                ClearUnits();
                _battleInstanceId = _combat.BattleInstanceId;
            }
            var state = _combat?.Current;
            if (!isActiveAndEnabled || state == null || _characters == null || _combat.Completion != null ||
                state.Phase == CombatFlowPhase.Finished || state.CommandState.Turn.StagePhase != StagePhase.Combat)
            {
                ClearUnits();
                return;
            }

            if (unitPrefab == null)
            {
                ClearUnits();
                return;
            }
            RectTransform root = content != null ? content : (RectTransform)transform;
            if (root != transform && !root.IsChildOf(transform))
                throw new InvalidOperationException("TimelineUI의 Content는 자신의 자식이어야 합니다.");
            int index = 0;
            // Buffer가 제공한 순서를 그대로 표시합니다. View에서 턴 예측이나 재정렬은 하지 않습니다.
            var timeline = state.CommandState.Timeline;
            for (int i = 0; i < timeline.Count && index < MaxVisibleUnits; i++)
            {
                var entry = timeline[i];
                if (entry.Kind != CombatTimelineEntryKind.UnitTurn || !entry.UnitId.HasValue ||
                    !_characters.TryGet(entry.UnitId.Value, out var character) ||
                    character.Health.IsDead || character.Area != CharacterRuntimeArea.Field) continue;

                Slot slot = GetOrCreateSlot(index, root);
                if (slot.EntityId != character.EntityId)
                {
                    slot.Input.OnHoverExit();
                    slot.EntityId = character.EntityId;
                }
                Sprite sprite = FindSprite(character.CharacterKey);
                slot.Image.sprite = sprite;
                // 미등록 Sprite를 흰 사각형으로 표시하지 않습니다. 슬롯 위치는 유지합니다.
                slot.Image.enabled = sprite != null;
                if (sprite == null) slot.Input.OnHoverExit();
                slot.Button.interactable = sprite != null;
                slot.Image.gameObject.SetActive(true);
                index++;
            }
            VisibleUnitCount = index;
            for (; index < _slots.Count; index++) ClearSlot(_slots[index]);
            ArrangeUnits(root);
        }

        private void ArrangeUnits(RectTransform root)
        {
            if (VisibleUnitCount == 0) return;
            float gap = Mathf.Max(0f, spacing);
            float width = gap * (VisibleUnitCount - 1);
            for (int i = 0; i < VisibleUnitCount; i++) width += _slots[i].Image.rectTransform.rect.width;
            // 이미지 너비와 사이 간격을 합친 전체 영역을 항상 Content 중앙에 배치합니다.
            float nextX = (root.rect.width - width) * 0.5f;
            for (int i = 0; i < VisibleUnitCount; i++)
            {
                RectTransform rect = _slots[i].Image.rectTransform;
                Vector2 size = rect.rect.size;
                rect.anchoredPosition = new Vector2(nextX + size.x * rect.pivot.x, size.y * (rect.pivot.y - 0.5f));
                nextX += size.x + gap;
            }
        }

        private Slot GetOrCreateSlot(int index, RectTransform root)
        {
            if (index < _slots.Count) return _slots[index];
            Image image = Instantiate(unitPrefab, root, false);
            image.name = $"TimelineUnit{index + 1}";
            image.raycastTarget = true;
            image.preserveAspect = true;
            RectTransform rect = image.rectTransform;
            Vector2 size = unitPrefab.rectTransform.rect.size;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>();
            if (button.targetGraphic == null) button.targetGraphic = image;
            var slot = new Slot
            {
                Image = image, Button = button,
                Input = UIInputBinding.Attach(button, this, index)
            };
            _slots.Add(slot);
            return slot;
        }

        private Sprite FindSprite(EntityKey key)
        {
            return _unitVisuals != null && _unitVisuals.TryGetUnitVisual(key, out var visual)
                ? visual.TimelineIcon : null;
        }

        private void HideTemplate()
        {
            if (unitPrefab == null) return;
            if (unitPrefab.gameObject == gameObject)
                throw new InvalidOperationException("TimelineUI의 테두리 Image를 Unit Prefab으로 사용할 수 없습니다.");
            if (unitPrefab.transform.IsChildOf(transform)) unitPrefab.gameObject.SetActive(false);
        }

        private static void ClearSlot(Slot slot)
        {
            slot.Input.OnHoverExit();
            slot.EntityId = EntityId.Empty;
            slot.Image.sprite = null;
            slot.Image.gameObject.SetActive(false);
        }

        private void ClearUnits()
        {
            VisibleUnitCount = 0;
            for (int i = 0; i < _slots.Count; i++) ClearSlot(_slots[i]);
        }

        void IUIInputReceiver.Click(int slot) { }
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }
    }
}
