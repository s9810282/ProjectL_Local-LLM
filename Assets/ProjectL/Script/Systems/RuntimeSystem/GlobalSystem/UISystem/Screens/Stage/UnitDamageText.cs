using TMPro;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>확정 피해 숫자 두 개의 확대·상승·페이드를 각각 재생한다. 피해 판정과 호출 시점은 호출자가 소유한다.</summary>
    [DisallowMultipleComponent]
    public sealed class UnitDamageText : MonoBehaviour
    {
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text damageText2;
        [Tooltip("각 숫자가 상승·페이드를 시작하기까지의 시간(초). 전체 연출 시간에 포함한다.")]
        [SerializeField, Min(0f)] private float consecutiveDelay = 0.2f;
        [Tooltip("숫자를 표시하고 숨기기까지의 전체 시간(초).")]
        [SerializeField, Min(0.01f)] private float duration = 0.7f;
        [Tooltip("Font Size가 0에서 목표 크기까지 커지는 시간(초). 전체 시간을 넘으면 전체 시간에 맞춘다.")]
        [SerializeField, Min(0f)] private float growDuration = 0.15f;
        [SerializeField, Min(0f)] private float baseFontSize = 16f;
        [SerializeField, Min(0f)] private float maxFontSize = 30f;
        [Tooltip("최대 Font Size에 도달하는 피해량. 이 이상은 최대 크기를 유지한다.")]
        [SerializeField, Min(1)] private int damageForMaxSize = 50;
        [Tooltip("시작 Pos Y(0)에서 상승할 최종 Pos Y(UI 단위).")]
        [SerializeField] private float maxY = 50f;
        [Tooltip("숨기기 직전 Alpha. 시작 Alpha 1에서 이 값까지 감소한다.")]
        [SerializeField, Range(0f, 1f)] private float endAlpha = 0f;

        private AnimationState _first;
        private AnimationState _second;
        private bool _useSecondText;
        private long _token;
        private System.Action<long> _completed;

        /// <summary>모델 밖에서 재생할 템플릿의 참조가 완결됐는지 검사합니다.</summary>
        internal void ValidateTemplate()
        {
            if (!(transform is RectTransform) || GetComponent<UnitHUD>() != null ||
                damageText == null || !damageText.transform.IsChildOf(transform) ||
                (damageText2 != null && !damageText2.transform.IsChildOf(transform)))
                throw new System.InvalidOperationException($"[UnitDamageText] '{name}': HUD 전체가 아닌 독립 RectTransform과 자식 숫자 TMP가 필요합니다.");
        }

        /// <summary>독립 대여 하나를 시작합니다. 취소·비활성화는 자연 완료를 알리지 않습니다.</summary>
        internal void PlayIndependent(int damage, long token, System.Action<long> completed)
        {
            ValidateTemplate();
            if (damage <= 0 || !isActiveAndEnabled)
                throw new System.InvalidOperationException("[UnitDamageText] 활성 위젯에 양수 피해가 필요합니다.");
            Clear();
            _token = token;
            _completed = completed ?? throw new System.ArgumentNullException(nameof(completed));
            Show(damage);
        }

        private struct AnimationState
        {
            internal bool Playing;
            internal float Elapsed;
            internal float TargetFontSize;
        }

        private void Awake()
        {
            Configure(damageText);
            Configure(damageText2);
            Clear();
        }

        private static void Configure(TMP_Text text)
        {
            if (text == null) return;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
        }

        /// <summary>두 텍스트를 번갈아 재생한다. 재사용하는 슬롯만 처음부터 시작하고 다른 슬롯은 계속 재생한다.</summary>
        public void Show(int damage)
        {
            if (damage <= 0 || damageText == null || !isActiveAndEnabled) return;
            if (_useSecondText && damageText2 != null && damageText2 != damageText)
                StartText(damageText2, ref _second, damage);
            else
                StartText(damageText, ref _first, damage);
            _useSecondText = !_useSecondText;
        }

        private void StartText(TMP_Text text, ref AnimationState state, int damage)
        {
            float minimum = Mathf.Max(0f, baseFontSize);
            state = new AnimationState
            {
                Playing = true,
                TargetFontSize = Mathf.Lerp(minimum, Mathf.Max(minimum, maxFontSize),
                    Mathf.Clamp01((float)damage / Mathf.Max(1, damageForMaxSize)))
            };
            text.SetText("{0}", damage);
            Apply(text, state, 0f);
            text.gameObject.SetActive(true);
        }

        private void Update() => TickAnimation(Time.deltaTime);

        internal void TickAnimation(float deltaTime)
        {
            TickText(damageText, ref _first, deltaTime);
            TickText(damageText2, ref _second, deltaTime);
            if (_first.Playing || _second.Playing) return;
            _useSecondText = false;
            System.Action<long> completed = _completed;
            long token = _token;
            _completed = null;
            _token = 0;
            completed?.Invoke(token);
        }

        private void TickText(TMP_Text text, ref AnimationState state, float deltaTime)
        {
            if (!state.Playing) return;
            if (text == null)
            {
                state = default;
                return;
            }
            state.Elapsed += deltaTime;
            float totalTime = Mathf.Max(0.01f, duration);
            float delay = Mathf.Clamp(consecutiveDelay, 0f, totalTime);
            float progress = state.Elapsed >= totalTime ? 1f
                : Mathf.Clamp01((state.Elapsed - delay) / Mathf.Max(0.0001f, totalTime - delay));
            Apply(text, state, progress);
            if (progress >= 1f)
            {
                state.Playing = false;
                text.gameObject.SetActive(false);
            }
        }

        private void Apply(TMP_Text text, AnimationState state, float progress)
        {
            float growTime = Mathf.Clamp(growDuration, 0f, Mathf.Max(0.01f, duration));
            text.fontSize = state.TargetFontSize * (growTime > 0f ? Mathf.Clamp01(state.Elapsed / growTime) : 1f);
            Vector2 position = text.rectTransform.anchoredPosition;
            position.y = Mathf.Lerp(0f, maxY, progress);
            text.rectTransform.anchoredPosition = position;
            text.alpha = Mathf.Lerp(1f, Mathf.Clamp01(endAlpha), progress);
        }

        /// <summary>HUD 재사용과 비활성화 시 두 슬롯의 피해 표시를 모두 정리한다.</summary>
        public void Clear()
        {
            _completed = null;
            _token = 0;
            _first = default;
            _second = default;
            _useSecondText = false;
            ResetText(damageText);
            ResetText(damageText2);
        }

        private static void ResetText(TMP_Text text)
        {
            if (text == null) return;
            text.gameObject.SetActive(false);
            text.SetText(string.Empty);
            text.fontSize = 0f;
            Vector2 position = text.rectTransform.anchoredPosition;
            position.y = 0f;
            text.rectTransform.anchoredPosition = position;
            text.alpha = 1f;
        }

        private void OnDisable() => Clear();
    }
}
