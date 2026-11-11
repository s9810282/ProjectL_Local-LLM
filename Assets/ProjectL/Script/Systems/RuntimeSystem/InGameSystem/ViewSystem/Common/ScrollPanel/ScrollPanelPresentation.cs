using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel
{
    /// <summary>등록된 화면 내부에서 두루마리와 자식 UI의 표시 연출만 담당합니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class ScrollPanelPresentation : MonoBehaviour
    {
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private Image dim;
        [SerializeField] private RectTransform slideRoot;
        [SerializeField] private CanvasGroup slideGroup;
        [SerializeField] private RawImage shadow;
        [SerializeField] private ScrollPaperGraphic paper;
        [SerializeField] private RectMask2D revealViewport;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Vector2 preferredSize = new Vector2(1440f, 900f);
        [SerializeField] private Vector2 screenMargin = new Vector2(24f, 24f);
        [SerializeField] private Vector2 contentInset = new Vector2(73.76f, 170f);
        [SerializeField, Range(0f, 1f)] private float dimAlpha = 0.46f;
        [SerializeField, Min(0.01f)] private float openDuration = 1.1f;
        [SerializeField, Min(0.01f)] private float closeDuration = 0.8f;

        private RectTransform _rect;
        private IAudioSystemService _audio;
        private float _progress;
        private bool _requestedOpen;
        private bool _initialized;
        private Vector2 _panelSize;
        private Vector2 _contentSize;
        private float _insetX;

        /// <summary>정상적인 펼침이 완료됐을 때만 알립니다.</summary>
        public event Action Opened;
        /// <summary>정상적인 말림과 퇴장이 완료됐을 때만 알립니다.</summary>
        public event Action Closed;

        public RectTransform ContentRoot => contentRoot;
        public bool IsFullyOpen => _requestedOpen && _progress >= 1f;
        public bool IsVisible => _progress > 0f;
        public bool IsAnimating => _progress != (_requestedOpen ? 1f : 0f);

        private void Awake()
        {
            EnsureInitialized();
        }

        internal void BindAudio(IAudioSystemService audio)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        /// <summary>현재 위치에서 엽니다. 화면 등록과 입력 Layer는 변경하지 않습니다.</summary>
        public void PlayOpen()
        {
            EnsureInitialized();
            EnsureCanAnimate();
            if (GetComponentInParent<Canvas>(true) == null)
                throw new InvalidOperationException("[ScrollPanelPresentation] 기존 화면 Canvas 아래에 배치해야 합니다.");
            ValidateContent();
            if (!_requestedOpen) PlaySound(UiEvent.ScrollOpen);
            _requestedOpen = true;
            Apply();
        }

        /// <summary>현재 위치에서 닫습니다. 이미 숨겨졌다면 false를 반환하며 완료 알림은 발생하지 않습니다.</summary>
        public bool PlayClose()
        {
            EnsureInitialized();
            if (_progress <= 0f)
            {
                _requestedOpen = false;
                return false;
            }
            EnsureCanAnimate();
            if (_requestedOpen) PlaySound(UiEvent.ScrollClose);
            _requestedOpen = false;
            Apply();
            return true;
        }

        /// <summary>강제 Hide, 부모 비활성화, 재사용 준비에서 완료 알림 없이 초기화합니다.</summary>
        public void ResetHidden()
        {
            EnsureInitialized();
            _requestedOpen = false;
            _progress = 0f;
            Apply();
        }

        private void Update()
        {
            if (!IsAnimating) return;
            float target = _requestedOpen ? 1f : 0f;
            float duration = _requestedOpen ? openDuration : closeDuration;
            _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime / duration);
            Apply();
            if (_progress != target) return;
            // 알림 수신자가 반대 방향 재생을 요청해도 나중에 그 요청을 덮어쓰지 않습니다.
            if (target > 0f) Opened?.Invoke();
            else Closed?.Invoke();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_initialized) Apply();
        }

        private void OnDisable()
        {
            _requestedOpen = false;
            _progress = 0f;
            // 파괴 순서로 자식 참조가 먼저 사라져도 남은 배경과 입력은 해제합니다.
            if (rootGroup != null)
            {
                rootGroup.alpha = 0f;
                rootGroup.blocksRaycasts = false;
                rootGroup.interactable = false;
            }
            if (_initialized && dim != null && slideRoot != null && slideGroup != null &&
                shadow != null && paper != null && revealViewport != null &&
                contentRoot != null && contentGroup != null) Apply();
        }

        private void OnDestroy()
        {
            Opened = null;
            Closed = null;
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;
            ValidateComposition();
            _rect = (RectTransform)transform;
            _requestedOpen = false;
            _progress = 0f;
            _initialized = true;
            Apply();
        }

        private void EnsureCanAnimate()
        {
            if (!isActiveAndEnabled)
                throw new InvalidOperationException("[ScrollPanelPresentation] 소유 화면을 UI 서비스로 연 뒤 연출을 요청해야 합니다.");
        }

        private void PlaySound(UiEvent cue)
        {
            if (_audio == null)
                throw new InvalidOperationException($"[ScrollPanelPresentation] '{name}'에 {cue} 재생을 위한 오디오 서비스가 연결되지 않았습니다.");
            _audio.Play(cue);
        }

        private void ValidateComposition()
        {
            if (rootGroup == null || dim == null || slideRoot == null || slideGroup == null ||
                shadow == null || paper == null || revealViewport == null || contentRoot == null || contentGroup == null)
                throw new InvalidOperationException("[ScrollPanelPresentation] 배경, 종이, 마스크, ContentRoot의 필수 참조가 부족합니다.");
            if (rootGroup.gameObject != gameObject || dim.transform.parent != transform ||
                slideRoot.parent != transform || slideGroup.transform != slideRoot ||
                paper.transform.parent != slideRoot || shadow.transform.parent != slideRoot ||
                revealViewport.transform.parent != slideRoot || contentRoot.parent != revealViewport.transform ||
                contentGroup.transform != contentRoot)
                throw new InvalidOperationException("[ScrollPanelPresentation] 배경/SlideRoot/RevealViewport/ContentRoot 프리팹 계층이 잘못됐습니다.");
            if (paper.material == null || paper.material.shader == null ||
                paper.material.shader.name != "ProjectL/UI/ScrollPaper" ||
                paper.mainTexture == Texture2D.whiteTexture || shadow.texture == null)
                throw new InvalidOperationException("[ScrollPanelPresentation] 종이 Material, 종이 Texture, 그림자 Texture 연결이 필요합니다.");
            if (preferredSize.x <= 0f || preferredSize.y <= 0f || screenMargin.x < 0f || screenMargin.y < 0f ||
                contentInset.x < 0f || contentInset.y < 0f || contentInset.x * 2f >= preferredSize.x * ScrollPaperGraphic.BodyWidthRatio ||
                contentInset.y * 2f >= preferredSize.y || openDuration <= 0f || closeDuration <= 0f ||
                dimAlpha < 0f || dimAlpha > 1f)
                throw new InvalidOperationException("[ScrollPanelPresentation] 크기, 여백, 재생 시간, 배경 알파 설정이 잘못됐습니다.");
        }

        private void ValidateContent()
        {
            foreach (Canvas child in contentRoot.GetComponentsInChildren<Canvas>(true))
                if (child.overrideSorting)
                    throw new InvalidOperationException($"[ScrollPanelPresentation] ContentRoot 아래의 overrideSorting Canvas는 지원하지 않습니다: {child.name}");
            foreach (CanvasGroup child in contentRoot.GetComponentsInChildren<CanvasGroup>(true))
                if (child.ignoreParentGroups)
                    throw new InvalidOperationException($"[ScrollPanelPresentation] 자식 입력 차단을 우회하는 ignoreParentGroups는 지원하지 않습니다: {child.name}");
            foreach (Graphic child in contentRoot.GetComponentsInChildren<Graphic>(true))
                if (!(child is MaskableGraphic maskable) || !maskable.maskable)
                    throw new InvalidOperationException($"[ScrollPanelPresentation] 자식 Graphic은 마스크를 지원해야 합니다: {child.name}");
        }

        private void Apply()
        {
            Rect rect = _rect.rect;
            float scale = Mathf.Min(1f, Mathf.Min(Mathf.Max(1f, rect.width - screenMargin.x * 2f) / preferredSize.x,
                Mathf.Max(1f, rect.height - screenMargin.y * 2f) / preferredSize.y));
            Vector2 size = preferredSize * scale;
            if (_panelSize != size)
            {
                _panelSize = size;
                slideRoot.sizeDelta = size;
                paper.rectTransform.sizeDelta = size;
                shadow.rectTransform.anchoredPosition = new Vector2(0f, -12f * scale);
                _insetX = contentInset.x * scale;
                _contentSize = new Vector2(Mathf.Max(0f, paper.BodyWidth - _insetX * 2f),
                    Mathf.Max(0f, size.y - contentInset.y * scale * 2f));
                contentRoot.sizeDelta = _contentSize;
            }

            bool visible = _progress > 0f;
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.blocksRaycasts = visible;
            rootGroup.interactable = visible;
            dim.color = new Color(0f, 0f, 0f, dimAlpha * Smooth(0f, 0.18f, _progress));
            dim.raycastTarget = visible;
            float slide = Smooth(0.12f, 0.56f, _progress);
            float below = rect.height * 0.5f + size.y * 0.5f + screenMargin.y;
            slideRoot.anchoredPosition = new Vector2(0f, -below * (1f - slide));
            slideGroup.alpha = Smooth(0.12f, 0.24f, _progress);
            slideGroup.blocksRaycasts = true;
            slideGroup.interactable = true;
            float reveal = Smooth(0.42f, 0.92f, _progress);
            paper.SetReveal(reveal);
            shadow.rectTransform.sizeDelta = new Vector2(Mathf.Lerp(size.x * 0.11f, size.x * 1.07f, reveal), size.y * 1.09f);
            float width = Mathf.Clamp(paper.BodyWidth * reveal - _insetX * 2f, 0f, _contentSize.x);
            revealViewport.rectTransform.sizeDelta = new Vector2(width, _contentSize.y);
            contentGroup.alpha = width > 0.01f ? 1f : 0f;
            contentGroup.interactable = IsFullyOpen;
            contentGroup.blocksRaycasts = IsFullyOpen;
        }

        private static float Smooth(float start, float end, float value)
        {
            float t = Mathf.InverseLerp(start, end, value);
            return t * t * (3f - 2f * t);
        }
    }
}
