using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using TMPro;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 턴 전환 문구를 표시합니다. 커진 크기에서 원래 크기로 줄어들며 선명해지고(등장), 잠시 보인 뒤 투명해지며 닫힙니다(퇴장).
    /// 전환 판단은 전투 표시가 담당합니다.
    /// </summary>
    /// <remarks>
    /// 투명도는 같은 오브젝트의 CanvasGroup으로 바꿉니다. 없으면 처음 쓸 때 붙입니다(배경·글자를 함께 흐리게).
    /// 다시 열면 등장부터 다시 하고, 바로 닫으면(<see cref="Dismiss"/>) 연출 없이 닫습니다. 시간은 시간 배율과 무관합니다.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TurnDisplayUI : UIBase
    {
        [SerializeField] private TMP_Text turnText;
        [Header("플레이어 턴·적 턴 연출")]
        [Tooltip("등장을 시작할 때의 크기 배율입니다. 이 크기에서 원래 크기로 줄어듭니다.")]
        [SerializeField, Min(1f)] private float introScale = 1.6f;
        [Tooltip("등장(커진 크기 → 원래 크기, 투명 → 선명)에 걸리는 초입니다. 크기와 투명도가 같은 속도로 바뀝니다.")]
        [SerializeField, Min(0f)] private float introSeconds = .35f;
        [Tooltip("등장이 끝난 뒤 그대로 보여 주는 초입니다.")]
        [SerializeField, Min(0f)] private float holdSeconds = 3f;
        [Tooltip("퇴장(선명 → 투명)에 걸리는 초입니다. 끝나면 닫힙니다.")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = .9f;
        [Header("배치 단계 연출")]
        [Tooltip("배치 단계 등장을 시작할 때의 크기 배율입니다. 플레이어·적 턴에는 적용하지 않습니다.")]
        [SerializeField, Min(1f)] private float deploymentIntroScale = 1.6f;
        [Tooltip("배치 단계가 원래 크기로 줄어들며 선명해지는 시간입니다.")]
        [SerializeField, Min(0f)] private float deploymentIntroSeconds = .35f;
        [Tooltip("배치 단계가 등장한 뒤 그대로 보여 주는 시간입니다.")]
        [SerializeField, Min(0f)] private float deploymentHoldSeconds = 3f;
        [Tooltip("배치 단계가 투명해지며 사라지는 시간입니다. 정상 종료 뒤 카드 등장으로 이어집니다.")]
        [SerializeField, Min(0f)] private float deploymentFadeOutSeconds = .9f;
        private CanvasGroup _group;
        private Vector3 _baseScale;
        private bool _hasBaseScale;
        private double _openedAt;
        private bool _isShowing;
        private bool _isDeployment;

        private float IntroScale => _isDeployment ? deploymentIntroScale : introScale;
        private float IntroSeconds => _isDeployment ? deploymentIntroSeconds : introSeconds;
        private float HoldSeconds => _isDeployment ? deploymentHoldSeconds : holdSeconds;
        private float FadeOutSeconds => _isDeployment ? deploymentFadeOutSeconds : fadeOutSeconds;

        public TurnDisplayUI() => Policy = UIOpenPolicy.Parallel;
        public override UIId ID => UIId.TurnDisplay;

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            base.BindUIService(resolver);
            ValidateReferences();
            turnText.raycastTarget = false;
        }

        public void ShowPlayerTurn() => ShowTurn("아군 턴", Color.black, false);
        public void ShowEnemyTurn() => ShowTurn("적군 턴", Color.red, false);
        public void ShowDeployment() => ShowTurn("배치 단계", Color.black, true);

        /// <summary>진입 순서는 이 화면의 실제 숨김을 기다립니다. 연출 시간을 외부에서 복제하지 않습니다.</summary>
        internal bool IsShowing => _isShowing;
        internal bool HasCompleted { get; private set; }

        /// <summary>환경요소 턴의 예약 진입점입니다. 현재는 표시·진행 동작을 하지 않습니다.</summary>
        public void ShowEnvironmentTurn() { }

        private void ShowTurn(string text, Color color, bool deployment)
        {
            ValidateReferences();
            IUISystemService ui = RequireUISystem();
            _isDeployment = deployment;
            turnText.SetText(text);
            turnText.color = color;
            // 자동으로 사라지는 안내는 뒤로가기 이력에 넣지 않습니다.
            ui.OpenUI(ID, true);
        }

        public override void Open()
        {
            ValidateReferences();
            RequireUISystem();
            ValidateAnimationSetting(IntroScale, 1f, "등장 배율");
            ValidateAnimationSetting(IntroSeconds, 0f, "등장 시간");
            ValidateAnimationSetting(HoldSeconds, 0f, "유지 시간");
            ValidateAnimationSetting(FadeOutSeconds, 0f, "퇴장 시간");
            ValidateAnimationSetting(IntroSeconds + HoldSeconds + FadeOutSeconds, 0f, "전체 시간");
            _openedAt = Time.unscaledTimeAsDouble;
            HasCompleted = false;
            _isShowing = true;
            // 켜지는 첫 프레임부터 커진 투명 상태로 보이게 먼저 맞춥니다.
            ApplyLook(0f);
            base.Open();
        }

        protected override void OnUIHidden()
        {
            _isShowing = false;
            _isDeployment = false;
            ResetLook();
        }

        private void Update()
        {
            if (!_isShowing) return;
            float elapsed = (float)(Time.unscaledTimeAsDouble - _openedAt);
            if (elapsed >= IntroSeconds + HoldSeconds + FadeOutSeconds)
            {
                HasCompleted = true;
                Dismiss();
                return;
            }
            ApplyLook(elapsed);
        }

        // 연 뒤 elapsed초의 모습입니다. 등장은 처음 빠르고 끝 느리게(ease-out) 크기·투명도를 함께, 퇴장은 일정하게 투명해집니다.
        private void ApplyLook(float elapsed)
        {
            float scale = 1f;
            float alpha = 1f;
            if (elapsed < IntroSeconds)
            {
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / IntroSeconds), 3f);
                scale = Mathf.Lerp(IntroScale, 1f, t);
                alpha = t;
            }
            else if (elapsed > IntroSeconds + HoldSeconds && FadeOutSeconds > 0f)
            {
                alpha = 1f - Mathf.Clamp01((elapsed - IntroSeconds - HoldSeconds) / FadeOutSeconds);
            }
            CaptureBaseScale();
            transform.localScale = _baseScale * scale;
            Group().alpha = alpha;
        }

        private void ResetLook()
        {
            if (_hasBaseScale) transform.localScale = _baseScale;
            if (_group != null) _group.alpha = 1f;
        }

        private void CaptureBaseScale()
        {
            if (_hasBaseScale) return;
            _baseScale = transform.localScale;
            _hasBaseScale = true;
        }

        private CanvasGroup Group()
        {
            if (_group != null) return _group;
            _group = GetComponent<CanvasGroup>();
            if (_group == null)
            {
                _group = gameObject.AddComponent<CanvasGroup>();
                // 안내 문구는 입력을 받지 않습니다(배경·글자도 Raycast를 끈 상태).
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }
            return _group;
        }

        internal void Dismiss() => RequireUISystem().CloseUI(ID);

        private void ValidateAnimationSetting(float value, float minimum, string field)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < minimum)
                throw new InvalidOperationException($"[TurnDisplayUI] UIId.{ID} '{name}': {(_isDeployment ? "배치 단계" : "플레이어·적 턴")} {field}는 유한한 {minimum} 이상이어야 합니다.");
        }

        internal void ValidateReferences()
        {
            if (turnText == null)
                throw new InvalidOperationException($"[TurnDisplayUI] UIId.{ID} '{name}': turnText 참조가 필요합니다.");
            if (turnText.transform == transform || !turnText.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[TurnDisplayUI] UIId.{ID} '{name}': turnText '{turnText.name}'는 TurnDisplay의 자식이어야 합니다.");
        }

        private IUISystemService RequireUISystem() => UISystemService
            ?? throw new InvalidOperationException($"[TurnDisplayUI] UIId.{ID} '{name}': UISystem 연결이 필요합니다.");
    }
}
