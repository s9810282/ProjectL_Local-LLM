using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 튜토리얼 화면입니다(튜토리얼 설계 §15.4). 설명 박스와 덮개·투명 막, 상단 실습 안내, 막힌 조작 안내를 그립니다.
    /// </summary>
    /// <remarks>
    /// Combat 씬의 튜토리얼 전용 프리팹(Screen Space Overlay, 정렬 100번대)에 하나 둡니다. UISystem이 씬 연결 때 찾아 등록하고,
    /// 다른 화면을 닫지 않도록 Parallel입니다. 무엇을 언제 보일지는 튜토리얼 표시(<see cref="TutorialViewUIDisplay"/>)가 정하고,
    /// 이 화면은 받은 문구·구멍·색을 그리기만 합니다. 튜토리얼 상태를 따로 갖지 않고 입력 장치를 읽지 않습니다.
    /// 투명 막은 클릭을 막기만 하고 진행시키지 않습니다. 설명 넘기기는 튜토리얼 입력 Layer가 맡습니다(설계 §15.4.3).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TutorialViewUI : UIBase
    {
        /// <summary>전역 UI(GlobalUICanvas, 정렬 5·99) 위에 오도록 요구하는 가장 낮은 Canvas 정렬입니다(설계 §15.4.3).</summary>
        internal const int MinimumSortingOrder = 100;

        [Tooltip("설명 중 보일 대상 밖을 덮는 그래픽입니다. 색은 정의 데이터의 덮개 색을 씁니다.")]
        [SerializeField] private TutorialViewOverlay overlay;
        [Tooltip("설명 중 화면 전체의 클릭을 막는 투명 막입니다. 보일 대상 위까지 덮고 Raycast Target을 켭니다.")]
        [SerializeField] private Graphic blocker;
        [SerializeField] private GameObject explanationRoot;
        [SerializeField] private TMP_Text explanationText;
        [Tooltip("배치·이동 실습 중 화면 상단 가운데에 띄우는 안내입니다.")]
        [SerializeField] private GameObject practiceRoot;
        [SerializeField] private TMP_Text practiceText;
        [Tooltip("이동 실습에서 막힌 조작이 있을 때 잠시 띄우는 안내입니다.")]
        [SerializeField] private GameObject blockedRoot;
        [SerializeField] private TMP_Text blockedText;
        [Tooltip("게임 창 활성 알림입니다. 늘 켜진 곳(튜토리얼 Canvas 루트)에 둡니다.")]
        [SerializeField] private TutorialViewFocusNotifier focusNotifier;
        [Tooltip("보일 대상 둘레에 더 비워 둘 여백(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float holePadding = 8f;
        [Tooltip("막힌 조작 안내를 띄워 두는 시간(초)입니다. 시간 배율과 상관없이 셉니다.")]
        [SerializeField, Min(0.1f)] private float blockedNoticeSeconds = 2f;
        [Tooltip("설명 박스와 대상 사이 간격(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float explanationGap = 24f;
        [Tooltip("설명 박스를 화면 안에 둘 여백(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float screenMargin = 24f;
        [Tooltip("실습 안내의 화면 상단 여백(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float practiceTopMargin = 40f;
        [Tooltip("실습 안내와 막힌 조작 안내 사이 간격(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float noticeGap = 16f;

        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Vector3[] _canvasCorners = new Vector3[4];
        private IUISystemService _ui;
        private bool _shown;
        private float _blockedHideAt = -1f;
        private bool _hasBaseLayout;
        private Vector2 _explanationSize;
        private Vector2 _practiceSize;
        private Vector2 _blockedSize;
        private Vector2 _explanationPadding;
        private Vector2 _practicePadding;
        private Vector2 _blockedPadding;

        public TutorialViewUI() => Policy = UIOpenPolicy.Parallel;

        public override UIId ID => UIId.Tutorial;

        internal TutorialViewFocusNotifier FocusNotifier => focusNotifier;

        internal bool IsShown => _shown;
        internal bool IsExplanationShown => _shown && explanationRoot.activeSelf;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _ui = service;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            ValidateReferences();
        }

        public override void GetUIData(IServiceModule query)
        {
        }

        /// <summary>설명 하나를 띄웁니다. 덮개는 보일 대상(화면 좌표)만 비우고, 투명 막이 클릭을 막습니다. 실습 안내는 닫습니다.</summary>
        internal void ShowExplanation(string text, IReadOnlyList<Rect> screenHoles, Color overlayColor)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            if (screenHoles == null)
                throw new ArgumentNullException(nameof(screenHoles));

            EnsureOpen();
            overlay.color = overlayColor;
            overlay.SetScreenHoles(screenHoles, holePadding);
            overlay.gameObject.SetActive(true);
            blocker.gameObject.SetActive(true);
            practiceRoot.SetActive(false);
            HideBlockedNotice();
            explanationText.text = text;
            explanationRoot.SetActive(true);
        }

        /// <summary>설명 박스만 닫습니다. 같은 구간의 다음 설명이 이어지므로 덮개와 막은 둡니다.</summary>
        internal void HideExplanation()
        {
            if (_shown)
                explanationRoot.SetActive(false);
        }

        /// <summary>실습을 통과해 실습 안내를 닫습니다(설계 §15.4).</summary>
        internal void HidePractice()
        {
            if (_shown)
                practiceRoot.SetActive(false);
        }

        /// <summary>강조 영역과 설명 박스 좌표만 갱신합니다. 열기·완료 알림·설명 시간은 건드리지 않습니다.</summary>
        internal void RefreshExplanationLayout(IReadOnlyList<Rect> holes, Rect? anchor,
            TutorialViewExplanationPlacement placement, IReadOnlyList<Rect> obstacles)
        {
            if (!IsExplanationShown)
                return;
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            float scale = canvas.scaleFactor;
            Rect viewport = canvas.pixelRect;
            Vector2 size = Measure(explanationText, _explanationSize, _explanationPadding, viewport, scale);
            Rect box = TutorialViewExplanationLayout.Place(viewport, size * scale, anchor, placement,
                obstacles, explanationGap * scale, screenMargin * scale);
            PlacePanel(explanationRoot, box, scale);
            overlay.SetScreenHoles(holes, holePadding);
        }

        /// <summary>실습 안내의 본문 높이와 창 크기를 반영해 두 안내가 겹치지 않게 배치합니다.</summary>
        internal void RefreshPracticeLayout()
        {
            if (!_shown || (!practiceRoot.activeSelf && !blockedRoot.activeSelf))
                return;
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            float scale = canvas.scaleFactor;
            Rect viewport = canvas.pixelRect;
            float top = viewport.yMax - practiceTopMargin * scale;
            if (practiceRoot.activeSelf)
            {
                Vector2 size = Measure(practiceText, _practiceSize, _practicePadding, viewport, scale) * scale;
                var rect = new Rect(viewport.center.x - size.x * 0.5f, top - size.y, size.x, size.y);
                PlacePanel(practiceRoot, rect, scale);
                top = rect.yMin - noticeGap * scale;
            }
            if (blockedRoot.activeSelf)
            {
                Vector2 size = Measure(blockedText, _blockedSize, _blockedPadding, viewport, scale) * scale;
                PlacePanel(blockedRoot, new Rect(viewport.center.x - size.x * 0.5f,
                    top - size.y, size.x, size.y), scale);
            }
        }

        private Vector2 Measure(TMP_Text text, Vector2 baseSize, Vector2 padding, Rect viewport, float scale)
        {
            float width = Mathf.Max(1f, Mathf.Min(baseSize.x, viewport.width / scale - screenMargin * 2f));
            float contentWidth = Mathf.Max(1f, width - padding.x);
            float height = Mathf.Max(baseSize.y, text.GetPreferredValues(text.text, contentWidth, float.PositiveInfinity).y + padding.y);
            return new Vector2(width, height);
        }

        private static void PlacePanel(GameObject panel, Rect screenRect, float scale)
        {
            var rect = (RectTransform)panel.transform;
            var parent = (RectTransform)rect.parent;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenRect.center, null, out Vector2 center);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = screenRect.size / scale;
            rect.anchoredPosition = center - parent.rect.center;
        }

        private void ReadBaseLayout()
        {
            if (_hasBaseLayout)
                return;
            _explanationSize = ((RectTransform)explanationRoot.transform).rect.size;
            _practiceSize = ((RectTransform)practiceRoot.transform).rect.size;
            _blockedSize = ((RectTransform)blockedRoot.transform).rect.size;
            _explanationPadding = Padding(explanationRoot, explanationText);
            _practicePadding = Padding(practiceRoot, practiceText);
            _blockedPadding = Padding(blockedRoot, blockedText);
            _hasBaseLayout = true;
        }

        private static Vector2 Padding(GameObject panel, TMP_Text text)
        {
            return Vector2.Max(Vector2.zero, ((RectTransform)panel.transform).rect.size - text.rectTransform.rect.size);
        }

        /// <summary>실습 안내를 띄웁니다. 덮개와 막을 닫아 기존 게임 UI를 그대로 쓰게 합니다(설계 §15.4).</summary>
        internal void ShowPractice(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            EnsureOpen();
            overlay.gameObject.SetActive(false);
            blocker.gameObject.SetActive(false);
            explanationRoot.SetActive(false);
            practiceText.text = text;
            practiceRoot.SetActive(true);
            RefreshPracticeLayout();
        }

        /// <summary>막힌 조작 안내를 잠시 띄웁니다. 실습 안내는 그대로 둡니다.</summary>
        internal void ShowBlockedNotice(string text) => ShowBlockedNotice(text, Time.unscaledTime);

        internal void ShowBlockedNotice(string text, float now)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            EnsureOpen();
            blockedText.text = text;
            blockedRoot.SetActive(true);
            _blockedHideAt = now + blockedNoticeSeconds;
            RefreshPracticeLayout();
        }

        /// <summary>열려 있으면 UISystem으로 닫습니다. 실제 숨김은 <see cref="Hide"/>가 합니다.</summary>
        internal void CloseAll()
        {
            if (!_shown)
                return;

            RequireUISystem();
            _ui.CloseUI(ID);
        }

        public override void Open()
        {
            ValidateReferences();
            ReadBaseLayout();
            ResetParts();
            _shown = true;
            gameObject.SetActive(true);
        }

        public override void Hide()
        {
            _shown = false;
            ResetParts();
            gameObject.SetActive(false);
        }

        /// <summary>막힌 조작 안내의 시간이 지났으면 닫습니다.</summary>
        internal void HideExpiredNotice(float now)
        {
            if (_blockedHideAt >= 0f && now >= _blockedHideAt)
                HideBlockedNotice();
        }

        private void HideBlockedNotice()
        {
            _blockedHideAt = -1f;
            if (blockedRoot != null)
                blockedRoot.SetActive(false);
        }

        private void Update()
        {
            HideExpiredNotice(Time.unscaledTime);
        }

        /// <summary>
        /// 필수 참조와 Canvas 조건을 확인합니다. 튜토리얼 Canvas는 화면 덮개 방식이고 정렬 100 이상이며 GraphicRaycaster가 있어야
        /// 전역 UI 버튼 위에서 클릭을 막습니다. 투명 막은 Canvas 전체를 덮어야 합니다. 실습·막힌 조작 안내는 클릭을 받지 않아야
        /// 그 아래 칸의 배치·이동 입력을 가리지 않습니다(전투 입력은 UI 위인지 GraphicRaycaster로 판정).
        /// </summary>
        internal void ValidateReferences()
        {
            Require(overlay, nameof(overlay));
            Require(blocker, nameof(blocker));
            Require(explanationRoot, nameof(explanationRoot));
            Require(explanationText, nameof(explanationText));
            Require(practiceRoot, nameof(practiceRoot));
            Require(practiceText, nameof(practiceText));
            Require(blockedRoot, nameof(blockedRoot));
            Require(blockedText, nameof(blockedText));
            Require(focusNotifier, nameof(focusNotifier));
            if (!blocker.raycastTarget)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 blocker는 클릭을 막도록 Raycast Target을 켜야 합니다.");

            Canvas canvas = GetComponentInParent<Canvas>(true);
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            if (root == null || root.renderMode != RenderMode.ScreenSpaceOverlay || root.sortingOrder < MinimumSortingOrder)
                throw new InvalidOperationException(
                    $"[TutorialViewUI] UIId.Tutorial은 Screen Space Overlay이고 정렬 {MinimumSortingOrder} 이상인 Canvas 아래에 있어야 합니다.");
            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster == null || !raycaster.enabled)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 Canvas에 켜진 GraphicRaycaster가 있어야 투명 막이 클릭을 막습니다.");
            if (!Covers(blocker.rectTransform, (RectTransform)root.transform))
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 blocker는 Canvas 전체를 덮어야 합니다(늘이기 앵커, 여백 0).");
            RequireNoRaycast(practiceRoot, nameof(practiceRoot));
            RequireNoRaycast(blockedRoot, nameof(blockedRoot));
        }

        private void EnsureOpen()
        {
            if (_shown)
                return;

            RequireUISystem();
            _ui.OpenUI(ID, true);
            if (!_shown)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial을 열지 못했습니다.");
        }

        private void RequireUISystem()
        {
            if (_ui == null)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 UISystem 연결이 필요합니다.");
        }

        // 막과 Canvas의 네 모서리가 같은지 본다. 늘이기 앵커가 중간에 하나라도 빠지면 다르다.
        private bool Covers(RectTransform target, RectTransform canvas)
        {
            target.GetWorldCorners(_corners);
            canvas.GetWorldCorners(_canvasCorners);
            for (int i = 0; i < 4; i++)
            {
                if ((_corners[i] - _canvasCorners[i]).sqrMagnitude > 1f)
                    return false;
            }

            return true;
        }

        private static void RequireNoRaycast(GameObject root, string field)
        {
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.raycastTarget)
                    throw new InvalidOperationException(
                        $"[TutorialViewUI] UIId.Tutorial의 {field} 아래 그래픽 {graphic.name}은 Raycast Target을 꺼야 아래 칸의 배치·이동 입력을 가리지 않습니다.");
            }
        }

        private void ResetParts()
        {
            _blockedHideAt = -1f;
            if (overlay != null)
                overlay.gameObject.SetActive(false);
            if (blocker != null)
                blocker.gameObject.SetActive(false);
            if (explanationRoot != null)
                explanationRoot.SetActive(false);
            if (practiceRoot != null)
                practiceRoot.SetActive(false);
            if (blockedRoot != null)
                blockedRoot.SetActive(false);
        }

        private static void Require(UnityEngine.Object value, string field)
        {
            if (value == null)
                throw new InvalidOperationException($"[TutorialViewUI] UIId.Tutorial의 필수 참조 {field}가 비어 있습니다.");
        }
    }
}
