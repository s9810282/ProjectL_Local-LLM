using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text
{
    /// <summary>프리팹 구조를 유지하며 발동별 문구·색·크기·페이드·가시성을 표시합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CombatBodyTextView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI label;
        [SerializeField]
        private CanvasGroup fadeGroup;
        private RectTransform _rect;
        private float _baseAlpha;
        private float _opacity;
        private bool _visible;
        internal RectTransform Rect => _rect;

        internal static CombatBodyTextView RequirePrefab(GameObject prefab)
        {
            if (prefab == null)
                throw new InvalidOperationException("[CombatBodyText] Body Text Prefab 또는 Prefab Override 참조가 필요합니다.");
            if (prefab.scene.IsValid())
                throw new InvalidOperationException($"[CombatBodyText] '{prefab.name}': Scene 객체가 아닌 프리팹 에셋을 연결하세요.");
            CombatBodyTextView view = prefab.GetComponent<CombatBodyTextView>();
            if (view == null)
                throw new InvalidOperationException($"[CombatBodyText] '{prefab.name}': 루트에 CombatBodyTextView가 필요합니다.");
            view.ValidateReferences();
            return view;
        }

        private void ValidateReferences()
        {
            if (transform is not RectTransform || fadeGroup == null || fadeGroup.transform != transform || label == null || label.transform == transform || !label.transform.IsChildOf(transform) || label.font == null)
                throw new InvalidOperationException($"[CombatBodyText] '{name}': 루트 RectTransform/CanvasGroup, 내부 Label 및 Label Font 참조를 확인하세요.");
            var rect = (RectTransform)transform;
            var center = new Vector2(.5f, .5f);
            if (rect.anchorMin != center || rect.anchorMax != center || rect.pivot != center)
                throw new InvalidOperationException($"[CombatBodyText] '{name}': 루트 Anchor/Pivot은 중앙(0.5, 0.5)이어야 합니다. 시각 보정은 Content에서 편집하세요.");
            if (fadeGroup.interactable || fadeGroup.blocksRaycasts || GetComponentsInChildren<Canvas>(true).Length != 0 || GetComponentsInChildren<GraphicRaycaster>(true).Length != 0)
                throw new InvalidOperationException($"[CombatBodyText] '{name}': Canvas/Raycaster를 넣지 말고 CanvasGroup 입력을 꺼 두세요.");
            foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget)
                    throw new InvalidOperationException($"[CombatBodyText] '{name}/{graphic.name}': Raycast Target을 꺼 두세요.");
        }

        internal void InitializeInstance()
        {
            _rect = (RectTransform)transform;
            _baseAlpha = fadeGroup.alpha;
        }

        internal void Prepare(string text, Color color, float fontSize)
        {
            label.text = text;
            label.color = color;
            label.enableAutoSizing = false;
            label.fontSize = fontSize;
            _opacity = 1f;
            _visible = false;
            ApplyAlpha();
            gameObject.SetActive(true);
        }

        internal void SetOpacity(float opacity)
        {
            if (_opacity == opacity)
                return;
            _opacity = opacity;
            if (_visible)
                ApplyAlpha();
        }

        internal void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;
            _visible = visible;
            ApplyAlpha();
        }

        private void ApplyAlpha() => fadeGroup.alpha = _visible ? _baseAlpha * _opacity : 0f;
        internal void ResetForPool()
        {
            gameObject.SetActive(false);
            label.text = string.Empty;
            _visible = false;
            _opacity = 1f;
            fadeGroup.alpha = _baseAlpha;
        }
    }
}
