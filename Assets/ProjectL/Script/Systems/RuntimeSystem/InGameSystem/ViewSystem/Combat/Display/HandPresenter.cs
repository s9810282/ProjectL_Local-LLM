using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 플레이어 손패를 화면 아래의 유닛 카드로 그립니다(손패 카드화 설계 C5~C9, 계획 H2).
    /// 카드 외형은 <see cref="UnitCardView"/>(Prefabs/UI/UnitCard.prefab)이고, 순서는 Core가 정한 ReserveSlotIndex입니다.
    /// 카드는 Card Root 안에서 가운데 정렬하고, 폭이 모자라면 간격을 줄여 겹칩니다.
    /// </summary>
    /// <remarks>
    /// Buffer·Core·UISystem을 모르고 값만 받습니다. ID별 기존 카드를 다시 쓰고 새 대상만 빌립니다(W19).
    /// 집기·Drop 영역은 화면 좌표로 판정하며, 입력은 Router → PlacementDragInteraction 경로가 맡습니다(카드의 UI 이벤트는 쓰지 않음).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class HandPresenter : MonoBehaviour
    {
        [Tooltip("손패 카드를 둘 영역입니다. Canvas 아래 RectTransform이며, 이 영역 전체가 필드 유닛을 되돌리는 Drop 영역입니다.")]
        [SerializeField] private RectTransform cardRoot;
        [Tooltip("손패 카드 프리팹입니다(Prefabs/UI/UnitCard.prefab). UIBase가 붙은 덱용 Variant는 쓰지 않습니다.")]
        [SerializeField] private UnitCardView cardPrefab;
        [Tooltip("카드 프리팹 크기에 곱할 배율입니다.")]
        [SerializeField, Range(0.05f, 2f)] private float cardScale = 0.35f;
        [Tooltip("카드 사이 간격입니다(Card Root 기준 단위). 폭이 모자라면 카드가 겹칩니다.")]
        [SerializeField] private float cardGap = 12f;
        [Tooltip("Card Root 아래 가장자리에서 카드 아래 가장자리까지의 거리입니다.")]
        [SerializeField] private float bottomMargin = 20f;
        [Tooltip("Card Root 좌우 가장자리에서 비워 둘 거리입니다.")]
        [SerializeField, Min(0f)] private float sideMargin = 20f;

        private CustomPool<UnitCardView> _pool;
        private readonly List<CharacterData> _ordered = new();
        private readonly Comparison<CharacterData> _bySlot = (a, b) =>
            a.ReserveSlotIndex.GetValueOrDefault().CompareTo(b.ReserveSlotIndex.GetValueOrDefault());
        private EntityViewSet<UnitCardView> _views;
        private IUnitVisualTableQuery _visuals;
        private Camera _eventCamera;
        private EntityId _hiddenEntityId = EntityId.Empty;

        /// <summary>외형 표를 연결합니다. Card Root가 화면 공간 Canvas 아래면 집기에 쓸 카메라를 정합니다.</summary>
        internal void Configure(IUnitVisualTableQuery visuals)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _pool ??= new CustomPool<UnitCardView>(new PoolContext<UnitCardView>(CreateCard, 0, null, new CardLifecycle()));
            _views ??= new EntityViewSet<UnitCardView>(_ => _pool.Get(), (_, card) => _pool.Return(card));

            Canvas canvas = cardRoot != null ? cardRoot.GetComponentInParent<Canvas>() : null;
            _eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        }

        /// <summary>
        /// 플레이어 손패의 살아 있는 캐릭터를 카드로 그립니다. <paramref name="visible"/>이 false면 카드 영역 전체를 숨깁니다
        /// (배치 단계에서만 표시, 설계 C8). 숨겨도 카드 내용은 갱신해 다시 보일 때 바로 맞습니다.
        /// </summary>
        internal void Render(IReadOnlyList<CharacterData> characters, bool visible)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (_views == null)
                throw new InvalidOperationException("[HandPresenter] Configure가 먼저 필요합니다.");

            _ordered.Clear();
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData character = characters[i];
                if (character.Owner != Owner.Player || character.Area != CharacterRuntimeArea.Hand || character.Health.IsDead)
                    continue;
                if (!character.ReserveSlotIndex.HasValue)
                    throw new InvalidOperationException($"[HandPresenter] 손패 캐릭터에 슬롯 번호가 없습니다. ID: {character.EntityId.Value}");

                _ordered.Add(character);
            }

            _ordered.Sort(_bySlot);
            _views.Begin();
            try
            {
                for (int i = 0; i < _ordered.Count; i++)
                {
                    CharacterData character = _ordered[i];
                    UnitCardView card = _views.Acquire(character.EntityId, character.CharacterKey);
                    ApplyContent(card, character);
                    ApplyLayout(card, i, _ordered.Count);
                    SetCardActive(card, character.EntityId != _hiddenEntityId);
                }
            }
            finally
            {
                _views.End();
            }

            if (cardRoot.gameObject.activeSelf != visible)
                cardRoot.gameObject.SetActive(visible);
        }

        /// <summary>지정한 개체의 카드만 숨깁니다(Drag 중 원본 숨김 등).</summary>
        internal void SetHiddenEntity(EntityId entityId)
        {
            if (_hiddenEntityId == entityId)
                return;

            SetActive(_hiddenEntityId, true);
            _hiddenEntityId = entityId;
            SetActive(_hiddenEntityId, false);
        }

        internal void ClearHiddenEntity()
        {
            SetHiddenEntity(EntityId.Empty);
        }

        internal void Release()
        {
            _hiddenEntityId = EntityId.Empty;
            _ordered.Clear();
            _views?.Clear();
        }

        /// <summary>
        /// 화면 좌표가 가리키는 손패 카드의 개체입니다. 겹친 카드는 앞에 그려진(뒤 순서) 카드가 이깁니다.
        /// 카드 영역이 꺼져 있거나 숨긴(Drag 중) 카드는 집지 않습니다.
        /// </summary>
        internal bool TryPickUnit(Vector2 screen, out EntityId entityId)
        {
            entityId = EntityId.Empty;
            if (!IsShown())
                return false;

            for (int i = _ordered.Count - 1; i >= 0; i--)
            {
                EntityId candidate = _ordered[i].EntityId;
                if (candidate == _hiddenEntityId || !_views.TryGet(candidate, out UnitCardView card) || card == null)
                    continue;
                if (!card.gameObject.activeSelf)
                    continue;
                if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)card.transform, screen, _eventCamera))
                {
                    entityId = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>화면 좌표가 손패로 되돌리는 Drop 영역(Card Root) 안인지입니다. 손패가 비어도 영역은 그대로이고, 숨겨져 있으면 false입니다.</summary>
        internal bool ContainsDropPoint(Vector2 screen)
        {
            return IsShown() && RectTransformUtility.RectangleContainsScreenPoint(cardRoot, screen, _eventCamera);
        }

        internal string FindMissingReference()
        {
            if (cardRoot == null)
                return $"{nameof(HandPresenter)}.{nameof(cardRoot)}";
            return cardPrefab == null ? $"{nameof(HandPresenter)}.{nameof(cardPrefab)}" : null;
        }

        private bool IsShown() => cardRoot != null && cardRoot.gameObject.activeInHierarchy;

        private void ApplyContent(UnitCardView card, CharacterData character)
        {
            string fallbackName = null;
            Sprite portrait = null;
            if (_visuals.TryGetUnitVisual(character.CharacterKey, out var visual))
            {
                fallbackName = visual.DisplayName;
                portrait = visual.InfoIcon;
            }

            // 레벨은 Buffer에 없으므로 '-'다(덱과 같음). 특성·시너지 패널은 손패에서 숨긴다(설계 U5).
            card.ShowCharacter(character.CharacterKey, fallbackName, portrait, null);
            card.ShowDetails(character);
        }

        /// <summary>순서 <paramref name="index"/>의 카드를 아래쪽 가운데 기준으로 놓습니다. 폭이 모자라면 간격을 줄입니다(옛 CardHandPresenter.Layout).</summary>
        private void ApplyLayout(UnitCardView card, int index, int count)
        {
            var rect = (RectTransform)card.transform;
            Vector2 size = rect.rect.size * cardScale;
            Rect area = cardRoot.rect;
            float usable = Mathf.Max(0f, area.width - (sideMargin * 2f) - size.x);
            float spacing = count > 1 ? Mathf.Min(size.x + cardGap, usable / (count - 1)) : 0f;
            // 카드 앵커가 Card Root 가운데이므로 가운데 기준 오프셋이다.
            var position = new Vector2(
                (index - ((count - 1) * 0.5f)) * spacing,
                (-area.height * 0.5f) + bottomMargin + (size.y * 0.5f));

            if (rect.anchoredPosition != position)
                rect.anchoredPosition = position;
            if (rect.GetSiblingIndex() != index)
                rect.SetSiblingIndex(index);
        }

        private static void SetCardActive(UnitCardView card, bool active)
        {
            if (card.gameObject.activeSelf != active)
                card.gameObject.SetActive(active);
        }

        private void SetActive(EntityId entityId, bool active)
        {
            if (entityId.IsEmpty || _views == null || !_views.TryGet(entityId, out UnitCardView card) || card == null)
                return;

            SetCardActive(card, active);
        }

        private UnitCardView CreateCard()
        {
            UnitCardView card = Instantiate(cardPrefab, cardRoot, false);
            var rect = (RectTransform)card.transform;
            // 위치 계산을 Card Root의 로컬 좌표(가운데 원점)로 맞춘다. 크기는 프리팹 그대로 두고 배율만 바꾼다.
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * cardScale;
            card.SetPanelsEnabled(false);
            return card;
        }

        private sealed class CardLifecycle : IPoolLifecycle<UnitCardView>
        {
            public void OnGet(UnitCardView item)
            {
            }

            public void OnBeforeReturn(UnitCardView item)
            {
                if (item == null)
                    return;

                item.Clear();
                if (item.gameObject.activeSelf)
                    item.gameObject.SetActive(false);
            }

            public void OnReturn(UnitCardView item)
            {
            }
        }
    }
}
