using System;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>등록된 화면 내부에서 사용하는 여정 전환 위젯. 씬 로딩과 UI 등록은 소유하지 않는다.</summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class SandstormTransitionVisual : MonoBehaviour
    {
        [SerializeField] private RawImage wind;
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private CanvasGroup journeyVisibility;
        [SerializeField] private RectTransform journeyLayout;
        [SerializeField] private JourneyRoadGraphic road;
        [SerializeField] private JourneyWagonGraphic wagon;
        [SerializeField] private JourneyHorseGraphic horse;
        [SerializeField, Min(0.01f)] private float coverDuration = 0.95f;
        [SerializeField, Min(0.01f)] private float revealDuration = 1.05f;
        [SerializeField] private bool useExternalClock;
        private readonly SandstormPlayback _playback = new SandstormPlayback();
        private Material _instanceMaterial;
        private Material _templateMaterial;
        private static readonly int PhaseId = UnityEngine.Shader.PropertyToID("_Phase");
        private static readonly int ProgressId = UnityEngine.Shader.PropertyToID("_Progress");
        private static readonly int TimeId = UnityEngine.Shader.PropertyToID("_PresentationTime");
        private static readonly int AspectId = UnityEngine.Shader.PropertyToID("_Aspect");

        public SandstormPhase Phase => _playback.Phase;
        public bool IsCovered => _playback.IsCovered;
        /// <summary>모래가 덮이고 기존 여정 페이드인까지 끝난 씬 교체 준비 상태.</summary>
        public bool IsReadyForSceneChange => IsCovered && _playback.JourneyOpacity >= 1f;
        public bool IsHidden => _playback.IsHidden;
        public double PresentationTime => _playback.PresentationTime;
        public bool UseExternalClock { get => useExternalClock; set => useExternalClock = value; }

        public void Cover()
        {
            Prepare();
            _playback.Cover(coverDuration);
            Apply();
        }

        public void Reveal()
        {
            Prepare();
            _playback.Reveal(revealDuration);
            Apply();
        }

        public void ShowCovered()
        {
            Prepare();
            _playback.ShowCovered();
            Apply();
        }

        /// <summary>외부 Tick을 선택했을 때만 호출한다. 단위는 초이다.</summary>
        public void Advance(double unscaledDeltaTime)
        {
            if (!useExternalClock)
            {
                throw new InvalidOperationException($"{name}: Advance를 직접 호출하려면 UseExternalClock을 켜야 한다.");
            }
            Prepare();
            Tick(unscaledDeltaTime);
        }

        public void ResetVisual()
        {
            _playback.Reset();
            if (visibility != null)
            {
                visibility.alpha = 0f;
                visibility.blocksRaycasts = false;
                visibility.interactable = false;
            }
            if (journeyVisibility != null)
            {
                journeyVisibility.alpha = 0f;
            }
            if (_instanceMaterial != null)
            {
                SetMaterialState(_instanceMaterial, 16f / 9f);
            }
        }

        public void ValidateConfiguration()
        {
            if (wind == null || visibility == null || journeyVisibility == null || journeyLayout == null || road == null || wagon == null || horse == null)
            {
                throw new InvalidOperationException($"{name}: SandstormTransitionVisual의 필수 표시 참조가 누락됐다.");
            }
            Material source = _instanceMaterial != null ? _instanceMaterial : wind.material;
            if (source == null || !source.HasProperty(PhaseId) || !source.HasProperty(TimeId) || !source.HasProperty(ProgressId))
            {
                throw new InvalidOperationException($"{name}: wind에 SandstormUI 머티리얼이 필요하다.");
            }
            if (float.IsNaN(coverDuration) || float.IsInfinity(coverDuration) || coverDuration <= 0f ||
                float.IsNaN(revealDuration) || float.IsInfinity(revealDuration) || revealDuration <= 0f)
            {
                throw new InvalidOperationException($"{name}: 덮기·걷기 시간은 양수인 유한 값이어야 한다.");
            }
            road.ValidateConfiguration();
            wagon.ValidateConfiguration();
            horse.ValidateConfiguration();
        }

        private void OnEnable() => ResetVisual();
        private void OnDisable() => ResetVisual();

        private void Update()
        {
            if (Application.IsPlaying(gameObject) && !useExternalClock && !IsHidden)
            {
                Tick(Time.unscaledDeltaTime);
            }
        }

        private void Tick(double deltaTime)
        {
            _playback.Advance(deltaTime);
            Apply();
        }

        private void Prepare()
        {
            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException($"{name}: 활성화된 여정 위젯에서만 재생할 수 있다.");
            }
            ValidateConfiguration();
            if (_instanceMaterial != null)
            {
                return;
            }
            _templateMaterial = wind.material;
            _instanceMaterial = new Material(_templateMaterial)
            {
                name = _templateMaterial.name + " (Journey Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };
            wind.material = _instanceMaterial;
        }

        private void Apply()
        {
            bool visible = !IsHidden;
            visibility.alpha = _playback.VisibilityOpacity;
            visibility.blocksRaycasts = visible;
            visibility.interactable = false;
            journeyVisibility.alpha = _playback.JourneyOpacity;
            Rect rect = ((RectTransform)transform).rect;
            float scale = Mathf.Min(rect.width / 1000f, rect.height / 563f);
            journeyLayout.localScale = Vector3.one * Mathf.Max(scale, 0.001f);
            float aspect = rect.height > 0f ? rect.width / rect.height : 16f / 9f;
            SetMaterialState(_instanceMaterial, aspect);
            Material rendered = wind.materialForRendering;
            if (rendered != _instanceMaterial)
            {
                SetMaterialState(rendered, aspect);
            }
            road.SetPresentationTime(_playback.PresentationTime);
            wagon.SetPresentationTime(_playback.PresentationTime);
            horse.SetPresentationTime(_playback.PresentationTime);
        }

        private void SetMaterialState(Material target, float aspect)
        {
            target.SetFloat(PhaseId, (float)_playback.Phase);
            target.SetFloat(ProgressId, _playback.Progress);
            target.SetFloat(TimeId, (float)_playback.PresentationTime);
            target.SetFloat(AspectId, aspect);
        }

        private void OnDestroy()
        {
            if (_instanceMaterial == null)
            {
                return;
            }
            if (wind != null)
            {
                wind.material = _templateMaterial;
            }
            if (Application.isPlaying)
            {
                Destroy(_instanceMaterial);
            }
            else
            {
                DestroyImmediate(_instanceMaterial);
            }
            _instanceMaterial = null;
        }
    }
}
