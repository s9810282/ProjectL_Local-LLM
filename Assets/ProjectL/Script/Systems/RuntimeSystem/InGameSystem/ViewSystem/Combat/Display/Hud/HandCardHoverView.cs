using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Hud
{
    /// <summary>손패의 고정 집기 영역 안에서 카드 외형만 올립니다. 클릭·드래그 명령은 처리하지 않습니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class HandCardHoverView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUIHandCardHoverEnter = new("Combat.Action.UI.HandCard.HoverEnter");
        private static readonly Unity.Profiling.ProfilerMarker _profileUIHandCardHoverExit = new("Combat.Action.UI.HandCard.HoverExit");
#endif

        private RectTransform _rect;
        private RectTransform _visual;
        private EntityId _entityId;
        private bool _hovered;
        private bool _hoverEnabled;

        internal RectTransform Rect => _rect;
        internal UnitCardView Content { get; private set; }

        /// <summary>생성한 손패 인스턴스만 설정합니다. 공용 카드 프리팹의 판정 설정은 바꾸지 않습니다.</summary>
        internal void Configure(UnitCardView content, float scale)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));
            if (!(scale > 0f) || float.IsInfinity(scale))
                throw new ArgumentOutOfRangeException(nameof(scale));

            Content = content;
            _rect = (RectTransform)transform;
            _rect.anchorMin = _rect.anchorMax = _rect.pivot = new Vector2(0.5f, 0.5f);
            _visual = (RectTransform)content.transform;
            Vector2 size = _visual.rect.size;
            _visual.SetParent(_rect, false);
            _visual.anchorMin = _visual.anchorMax = _visual.pivot = new Vector2(0.5f, 0.5f);
            _visual.sizeDelta = size;
            _visual.localScale = Vector3.one * scale;
            _rect.sizeDelta = size * scale;

            Image hitArea = GetComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            foreach (Graphic graphic in content.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            content.SetInfoInputEnabled(false);
            ResetHover();
        }

        /// <summary>새 개체가 같은 카드를 빌릴 때 이전 호버 표시를 버립니다.</summary>
        internal void Bind(EntityId entityId)
        {
            if (_entityId == entityId)
                return;

            ResetHover();
            _entityId = entityId;
        }

        internal void SetHoverEnabled(bool enabled)
        {
            _hoverEnabled = enabled;
            if (!enabled)
                ResetHover();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIHandCardHoverEnterSample = _profileUIHandCardHoverEnter.Auto();
#endif

            if (isActiveAndEnabled && _hoverEnabled && !_entityId.IsEmpty)
                _hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUIHandCardHoverExitSample = _profileUIHandCardHoverExit.Auto();
#endif

            _hovered = false;
        }

        /// <summary>부모 UI 공간 단위로 외형만 보간합니다. 판정 Rect와 sibling 순서는 고정입니다.</summary>
        internal void AnimateHover(float lift, float speed, float deltaTime)
        {
            if (_visual == null)
                return;

            Vector2 target = _hovered && _hoverEnabled ? new Vector2(0f, lift) : Vector2.zero;
            _visual.anchoredPosition = speed == 0f
                ? target
                : Vector2.MoveTowards(_visual.anchoredPosition, target, speed * Mathf.Max(0f, deltaTime));
        }

        internal void ResetHover()
        {
            _hovered = false;
            if (_visual != null)
                _visual.anchoredPosition = Vector2.zero;
        }

        internal void ResetForPool()
        {
            SetHoverEnabled(false);
            _entityId = EntityId.Empty;
            Content.Clear();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            ResetHover();
        }
    }
}
