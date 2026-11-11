using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Integration
{
    /// <summary>유지 여정 UI를 로딩 서비스의 덮기/걷어내기 수명에 연결한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class SandstormLoadingUI : GlobalUIBase, ILodingPresentation
    {
        [SerializeField] private SandstormTransitionVisual visual;
        private ILodingSystemService _loading;
        private IAudioSystemService _audio;

        public override UIId ID => UIId.Loading;
        public override bool OpenWithGlobalUI => false;
        public bool IsCovered => visual.IsReadyForSceneChange;
        public bool IsHidden => visual.IsHidden;

        public SandstormLoadingUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public void ValidateConfiguration()
        {
            if (visual == null || !visual.transform.IsChildOf(transform))
            {
                throw new InvalidOperationException($"{name} (UIId.Loading): 하위 SandstormJourney의 visual 참조가 필요하다.");
            }
            if (visual.UseExternalClock)
            {
                throw new InvalidOperationException($"{name} (UIId.Loading): visual은 자동 시간원을 사용해야 한다.");
            }
            visual.ValidateConfiguration();
        }

        public override void Open()
        {
            ValidateConfiguration();
            IAudioSystemService audio = _audio
                ?? throw new InvalidOperationException($"{name} (UIId.Loading): AudioSystem 바인딩이 필요합니다.");
            base.Open();
            // 비활성 상태에서 만든 중첩 Canvas도 활성화된 뒤 정렬 오버라이드를 확정한다.
            GetComponent<Canvas>().overrideSorting = true;
            // Open이 돌아온 뒤에는 하위 OnEnable의 초기화도 끝나 있다.
            visual.Cover();
            if (!audio.IsPlaying(AmbgmEvent.Loading)) audio.Play(AmbgmEvent.Loading);
        }

        private void Awake() => ValidateConfiguration();

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            base.BindUIService(resolver);
            ValidateConfiguration();
            var global = resolver.GetService(SystemGroupsService.Global);
            ILodingSystemService loading = global.LodingServiceCommand.LodingService;
            IAudioSystemService audio = global.AudioServiceCommand
                ?? throw new InvalidOperationException($"{name} (UIId.Loading): AudioSystem 서비스가 필요합니다.");
            if (_loading != null && !ReferenceEquals(_loading, loading))
                throw new InvalidOperationException(name + ": 로딩 서비스가 이미 연결되어 있습니다.");
            loading.BindPresentation(this);
            _loading = loading;
            _audio = audio;
        }

        public void BeginCover()
        {
            if (UISystemService == null)
                throw new InvalidOperationException(name + " (UIId.Loading): UISystem 바인딩이 필요합니다.");
            UISystemService.OpenUI(ID, true);
        }

        public void BeginReveal() => visual.Reveal();

        public void End() => UISystemService.CloseUI(ID);

        protected override void OnUIHidden()
        {
            _audio?.Stop(AmbgmEvent.Loading);
            if (visual != null)
            {
                visual.ResetVisual();
            }
        }

        private void OnDestroy()
        {
            _loading?.UnbindPresentation(this);
            _loading = null;
        }
    }
}
