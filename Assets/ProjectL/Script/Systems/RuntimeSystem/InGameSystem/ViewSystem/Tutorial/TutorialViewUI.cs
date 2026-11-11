using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
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
        [Tooltip("UI 강조 영역의 점선입니다. 덮개 위, 설명 박스 아래에 둡니다.")]
        [SerializeField] private TutorialFocusBorderGraphic focusBorder;
        [Tooltip("각 카드·버튼 자체의 가장자리에 추가하는 점선입니다. 기존 흰 영역 테두리와 별개입니다.")]
        [SerializeField] private TutorialTargetBorderGraphic targetBorder;
        [SerializeField] private TutorialHighlightStyle highlightStyle = new();
        [Tooltip("설명 중 화면 전체의 클릭을 막는 투명 막입니다. 보일 대상 위까지 덮고 Raycast Target을 켭니다.")]
        [SerializeField] private Graphic blocker;
        [SerializeField] private GameObject explanationRoot;
        [SerializeField] private TMP_Text explanationText;
        [Tooltip("실습 중 화면 오른쪽 위 배경 영역에 띄우는 안내입니다.")]
        [SerializeField] private GameObject practiceRoot;
        [SerializeField] private TMP_Text practiceText;
        [Tooltip("이동 실습에서 막힌 조작이 있을 때 잠시 띄우는 안내입니다.")]
        [SerializeField] private GameObject blockedRoot;
        [SerializeField] private TMP_Text blockedText;
        [Tooltip("보일 대상 둘레에 더 비워 둘 여백(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float holePadding = 8f;
        [Tooltip("막힌 조작 안내를 띄워 두는 시간(초)입니다. 시간 배율과 상관없이 셉니다.")]
        [SerializeField, Min(0.1f)] private float blockedNoticeSeconds = 2f;
        [Tooltip("설명 박스와 대상 사이 간격(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float explanationGap = 24f;
        [Tooltip("설명 박스의 화면 여백과 실습 안내의 오른쪽 여백(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float screenMargin = 24f;
        [Tooltip("실습 안내의 화면 상단 여백(Canvas 단위)입니다. 오른쪽 설정 버튼 아래에 배치합니다.")]
        [SerializeField, Min(0f)] private float practiceTopMargin = 136f;
        [Tooltip("실습 안내와 막힌 조작 안내 사이 간격(Canvas 단위)입니다.")]
        [SerializeField, Min(0f)] private float noticeGap = 16f;
        [Header("같은 문장일 때의 주의")]
        [Tooltip("막힌 조작 안내가 실습 안내와 같은 문장이면 상자를 하나 더 띄우지 않고 실습 안내를 흔듭니다. 흔드는 시간(초)입니다.")]
        [SerializeField, Min(0.05f)] private float practiceWarningSeconds = 0.4f;
        [Tooltip("실습 안내를 좌우로 흔드는 폭(Canvas 단위)입니다. 흔들림은 끝으로 갈수록 줄어듭니다.")]
        [SerializeField, Min(0f)] private float practiceWarningShake = 12f;
        [Tooltip("흔드는 동안 실습 안내 글자 색입니다. 끝나면 원래 색으로 돌아갑니다.")]
        [SerializeField] private Color practiceWarningColor = new Color(0.85f, 0.15f, 0.1f, 1f);

        private readonly Vector3[] _corners = new Vector3[4];
        private readonly Vector3[] _canvasCorners = new Vector3[4];
        private readonly List<Rect> _layoutObstacles = new();
        private Action _clearModelHighlights;
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
        // 실습 안내 주의(흔들기)입니다. 끝나는 시각이 음수면 진행 중이 아닙니다. 원래 글자 색은 시작 때 한 번 기억합니다.
        private float _warningStartedAt;
        private float _warningEndsAt = -1f;
        private Color _practiceTextColor;

        public TutorialViewUI() => Policy = UIOpenPolicy.Parallel;

        public override UIId ID => UIId.Tutorial;

        internal bool IsShown => _shown && isActiveAndEnabled;
        internal bool IsExplanationShown => IsShown && explanationRoot.activeSelf;
        internal bool IsPracticeShown => IsShown && practiceRoot.activeSelf;
        internal TutorialHighlightStyle HighlightStyle => highlightStyle;

        /// <summary>화면 숨김에서도 표시 소유자의 모델 대여를 즉시 반환하는 명시적 정리 접점입니다.</summary>
        internal void BindHighlightCleanup(Action cleanup)
        {
            if (cleanup != null && _clearModelHighlights != null)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 모델 강조 정리가 이미 연결됐습니다.");
            _clearModelHighlights = cleanup;
        }

        internal void ClearHighlights()
        {
            List<Exception> errors = null;
            if (focusBorder != null)
                CleanupErrors.Try(focusBorder.Clear, ref errors);
            if (targetBorder != null)
                CleanupErrors.Try(targetBorder.Clear, ref errors);
            if (_clearModelHighlights != null)
                CleanupErrors.Try(_clearModelHighlights, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[TutorialViewUI] UIId.Tutorial의 강조 정리에 실패했습니다.");
        }

        protected override void OnUIHidden() => ClearHighlights();

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
            ClearHighlights();
            overlay.color = overlayColor;
            overlay.SetScreenHoles(screenHoles, holePadding);
            overlay.gameObject.SetActive(true);
            blocker.gameObject.SetActive(true);
            EndPracticeWarning();
            practiceRoot.SetActive(false);
            HideBlockedNotice();
            explanationText.text = text;
            explanationRoot.SetActive(true);
        }

        /// <summary>설명 박스만 닫습니다. 같은 구간의 다음 설명이 이어지므로 덮개와 막은 둡니다.</summary>
        internal void HideExplanation()
        {
            ClearHighlights();
            if (_shown)
                explanationRoot.SetActive(false);
        }

        /// <summary>실습을 통과해 실습 안내를 닫습니다(설계 §15.4).</summary>
        internal void HidePractice()
        {
            ClearHighlights();
            EndPracticeWarning();
            if (_shown)
                practiceRoot.SetActive(false);
        }

        /// <summary>강조 영역과 설명 박스 좌표만 갱신합니다. 열기·완료 알림·단계 진행은 건드리지 않습니다.</summary>
        internal void RefreshExplanationLayout(TutorialFocusFrame frame,
            TutorialViewExplanationPlacement placement, IReadOnlyList<Rect> obstacles)
        {
            if (!IsExplanationShown)
                return;
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            float scale = canvas.scaleFactor;
            Rect viewport = canvas.pixelRect;
            _layoutObstacles.Clear();
            _layoutObstacles.AddRange(obstacles);
            foreach (Rect hole in frame.Holes)
            {
                // UI와 모델 모두 같은 밝은 영역 바깥에 흰 점선이 있으므로 설명도 그 두께를 피합니다.
                float padding = holePadding + highlightStyle.BorderThickness;
                float pixels = padding * scale;
                _layoutObstacles.Add(Rect.MinMaxRect(hole.xMin - pixels, hole.yMin - pixels,
                    hole.xMax + pixels, hole.yMax + pixels));
            }
            Vector2 size = Measure(explanationText, _explanationSize, _explanationPadding, viewport, scale);
            Rect box = TutorialViewExplanationLayout.Place(viewport, size * scale, frame.Anchor, placement,
                _layoutObstacles, explanationGap * scale, screenMargin * scale);
            PlacePanel(explanationRoot, box, scale);
            overlay.SetScreenHoles(frame.Holes, holePadding);
            focusBorder.SetScreenRects(frame.Holes, holePadding, highlightStyle);
            targetBorder.SetTargets(frame.UiTargets, highlightStyle);
            targetBorder.SetBlink(false, highlightStyle, Time.unscaledTime);
        }

        /// <summary>실습 조작 대상만 표시합니다. 덮개·흰 영역·모델·입력은 바꾸지 않습니다.</summary>
        internal void RefreshPracticeHighlights(TutorialFocusFrame frame)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (!IsPracticeShown) return;
            focusBorder.Clear();
            targetBorder.SetTargets(frame.UiTargets, highlightStyle);
            targetBorder.SetBlink(true, highlightStyle, Time.unscaledTime);
        }

        /// <summary>중앙 턴 표시를 가리지 않도록 실습·조작 제한 안내를 오른쪽 위에 쌓습니다.</summary>
        internal void RefreshPracticeLayout()
        {
            if (!_shown || (!practiceRoot.activeSelf && !blockedRoot.activeSelf))
                return;
            Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
            float scale = canvas.scaleFactor;
            Rect viewport = canvas.pixelRect;
            float top = viewport.yMax - practiceTopMargin * scale;
            float right = viewport.xMax - screenMargin * scale;
            if (practiceRoot.activeSelf)
            {
                Vector2 size = Measure(practiceText, _practiceSize, _practicePadding, viewport, scale) * scale;
                var rect = new Rect(right - size.x, top - size.y, size.x, size.y);
                PlacePanel(practiceRoot, rect, scale);
                // 주의 흔들림은 배치 뒤에 더합니다. 배치가 매 갱신 자리를 다시 정하므로 흔들림이 쌓이지 않습니다.
                float shake = PracticeWarningOffset(Time.unscaledTime);
                if (shake != 0f)
                    ((RectTransform)practiceRoot.transform).anchoredPosition += new Vector2(shake, 0f);
                top = rect.yMin - noticeGap * scale;
            }
            if (blockedRoot.activeSelf)
            {
                Vector2 size = Measure(blockedText, _blockedSize, _blockedPadding, viewport, scale) * scale;
                PlacePanel(blockedRoot, new Rect(right - size.x,
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
            ClearHighlights();
            overlay.gameObject.SetActive(false);
            blocker.gameObject.SetActive(false);
            explanationRoot.SetActive(false);
            EndPracticeWarning();
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
            // 위에 떠 있는 실습 안내와 같은 문장이면 같은 상자를 하나 더 쌓지 않고 실습 안내를 흔들어 주의를 줍니다(2026-11-11 사용자).
            if (practiceRoot.activeSelf && string.Equals(text, practiceText.text, StringComparison.Ordinal))
            {
                StartPracticeWarning(now);
                return;
            }
            blockedText.text = text;
            blockedRoot.SetActive(true);
            _blockedHideAt = now + blockedNoticeSeconds;
            RefreshPracticeLayout();
        }

        private void StartPracticeWarning(float now)
        {
            if (_warningEndsAt < 0f)
                _practiceTextColor = practiceText.color;
            _warningStartedAt = now;
            _warningEndsAt = now + practiceWarningSeconds;
            practiceText.color = practiceWarningColor;
            RefreshPracticeLayout();
        }

        // 지금 시각의 좌우 흔들림(Canvas 단위)입니다. 세 번 왕복하며 끝으로 갈수록 줄어듭니다.
        private float PracticeWarningOffset(float now)
        {
            if (_warningEndsAt < 0f)
                return 0f;
            float progress = Mathf.Clamp01((now - _warningStartedAt) / practiceWarningSeconds);
            return Mathf.Sin(progress * Mathf.PI * 6f) * practiceWarningShake * (1f - progress);
        }

        /// <summary>실습 안내 주의의 시간이 지났으면 끝냅니다.</summary>
        internal void EndExpiredPracticeWarning(float now)
        {
            if (_warningEndsAt >= 0f && now >= _warningEndsAt)
                EndPracticeWarning();
        }

        // 글자 색을 되돌립니다. 위치는 다음 배치가 흔들림 없이 다시 정합니다.
        private void EndPracticeWarning()
        {
            if (_warningEndsAt < 0f)
                return;
            _warningEndsAt = -1f;
            if (practiceText != null)
                practiceText.color = _practiceTextColor;
        }

        /// <summary>열려 있으면 UISystem으로 닫습니다. 실제 숨김은 <see cref="Hide"/>가 합니다.</summary>
        internal void CloseAll()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(ClearHighlights, ref errors);
            if (_shown)
            {
                CleanupErrors.Try(() =>
                {
                    RequireUISystem();
                    _ui.CloseUI(ID);
                }, ref errors);
            }
            CleanupErrors.ThrowIfAny(errors, "[TutorialViewUI] UIId.Tutorial을 닫는 중 정리에 실패했습니다.");
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
            try { ResetParts(); }
            finally { gameObject.SetActive(false); }
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
            EndExpiredPracticeWarning(Time.unscaledTime);
            if (targetBorder != null) targetBorder.TickBlink(Time.unscaledTime);
        }

        /// <summary>
        /// 필수 참조와 Canvas 조건을 확인합니다. 튜토리얼 Canvas는 화면 덮개 방식이고 정렬 100 이상이며 GraphicRaycaster가 있어야
        /// 전역 UI 버튼 위에서 클릭을 막습니다. 투명 막은 Canvas 전체를 덮어야 합니다. 실습·막힌 조작 안내는 클릭을 받지 않아야
        /// 그 아래 칸의 배치·이동 입력을 가리지 않습니다(전투 입력은 UI 위인지 GraphicRaycaster로 판정).
        /// </summary>
        internal void ValidateReferences()
        {
            Require(overlay, nameof(overlay));
            Require(focusBorder, nameof(focusBorder));
            Require(targetBorder, nameof(targetBorder));
            if (highlightStyle == null)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 highlightStyle이 비어 있습니다.");
            highlightStyle.Validate();
            if (float.IsNaN(holePadding) || float.IsInfinity(holePadding) || holePadding < 0f)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 holePadding은 유한한 0 이상 값이어야 합니다.");
            if (float.IsNaN(practiceWarningSeconds) || float.IsInfinity(practiceWarningSeconds) || practiceWarningSeconds <= 0f
                || float.IsNaN(practiceWarningShake) || float.IsInfinity(practiceWarningShake) || practiceWarningShake < 0f)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 practiceWarningSeconds는 유한한 양수, practiceWarningShake는 유한한 0 이상 값이어야 합니다.");
            if (practiceWarningColor.a <= 0f)
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 practiceWarningColor는 알파가 0보다 커야 글자가 보입니다.");
            Require(blocker, nameof(blocker));
            Require(explanationRoot, nameof(explanationRoot));
            Require(explanationText, nameof(explanationText));
            Require(practiceRoot, nameof(practiceRoot));
            Require(practiceText, nameof(practiceText));
            Require(blockedRoot, nameof(blockedRoot));
            Require(blockedText, nameof(blockedText));
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
            if (focusBorder.transform.parent != overlay.transform.parent
                || focusBorder.transform.parent != explanationRoot.transform.parent
                || focusBorder.transform.GetSiblingIndex() <= overlay.transform.GetSiblingIndex()
                || focusBorder.transform.GetSiblingIndex() >= explanationRoot.transform.GetSiblingIndex()
                || !focusBorder.enabled || !focusBorder.gameObject.activeSelf
                || !Covers(focusBorder.rectTransform, (RectTransform)root.transform))
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 focusBorder는 같은 부모의 Overlay 위/설명 아래에서 Canvas 전체를 덮는 활성 위젯이어야 합니다.");
            RequireNoRaycast(practiceRoot, nameof(practiceRoot));
            if (targetBorder.transform.parent != focusBorder.transform.parent
                || targetBorder.transform.GetSiblingIndex() <= focusBorder.transform.GetSiblingIndex()
                || targetBorder.transform.GetSiblingIndex() >= explanationRoot.transform.GetSiblingIndex()
                || !targetBorder.enabled || !targetBorder.gameObject.activeSelf
                || !Covers(targetBorder.rectTransform, (RectTransform)root.transform))
                throw new InvalidOperationException("[TutorialViewUI] UIId.Tutorial의 targetBorder는 같은 부모의 FocusBorder 위/설명 아래에서 Canvas 전체를 덮는 활성 위젯이어야 합니다.");
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
            try { ClearHighlights(); }
            finally
            {
                EndPracticeWarning();
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
        }

        private static void Require(UnityEngine.Object value, string field)
        {
            if (value == null)
                throw new InvalidOperationException($"[TutorialViewUI] UIId.Tutorial의 필수 참조 {field}가 비어 있습니다.");
        }
    }
}
