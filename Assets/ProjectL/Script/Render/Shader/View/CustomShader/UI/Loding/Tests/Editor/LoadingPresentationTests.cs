using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Render.Shader.View.CustomShader.UI.Editor;
using ProjectL.Render.Shader.View.CustomShader.UI.Integration;
using ProjectL.Render.Shader.View.CustomShader.UI.Runtime;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    public sealed class LoadingPresentationTests
    {
        private IExportSystemServices _group;
        private ILodingSystemServiceContext _context;
        private ILodingSystemService _loading;
        private List<string> _events;
        private Presentation _presentation;

        [SetUp]
        public void SetUp()
        {
            _group = LodingFactory.Create();
            _context = (ILodingSystemServiceContext)_group;
            _loading = _context.LodingService;
            ((ISystemInitialize)_group).Initialize();
            // 동일 씬 분기는 실제 씬을 바꾸지 않는다. 사용자 편집 씬은 건드리지 않는다.
            _loading.GetType().GetProperty("CurrentSceneIndex").SetValue(_loading, 0);
            _events = new List<string>();
            _presentation = new Presentation(_events);
            _loading.BindPresentation(_presentation);
            _context.LodingRegisterService.RegisterBeforeLoad(new LodingNode(10, () => _events.Add("before10")));
            _context.LodingRegisterService.RegisterBeforeLoad(new LodingNode(0, () => _events.Add("before0")));
            _context.LodingRegisterService.RegisterSceneExitLoaded(0, new LodingNode(0, () => _events.Add("exit")));
            _context.LodingRegisterService.RegisterSceneEnterLoaded(0, new LodingNode(0, () => _events.Add("enter")));
            _context.LodingRegisterService.RegisterAfterLoad(new LodingNode(0, () => _events.Add("after")));
        }

        [TearDown]
        public void TearDown() => ((ISystemShutdown)_group).Shutdown();

        [Test]
        public void CoverAndCoveredFrameMustFinishBeforeAnyLoadingNode()
        {
            _loading.Load(0);
            Assert.That(_loading.IsLoading, Is.True);
            Tick();
            Assert.That(_events, Is.EqualTo(new[] { "cover" }));
            _presentation.Covered = true;
            Tick();
            Assert.That(_events, Is.EqualTo(new[] { "cover" }));
            Tick();
            Assert.That(_events, Is.EqualTo(new[] { "cover", "before0", "before10", "exit", "enter", "after", "reveal" }));
            Assert.That(_loading.IsLoading, Is.True, "걷어내기 중에도 다음 로딩은 차단한다");
            Assert.That(_loading.LoadingProgress, Is.EqualTo(1f));
            _presentation.Hidden = true;
            Tick();
            Assert.That(_events[_events.Count - 1], Is.EqualTo("end"));
            Assert.That(_loading.IsLoading, Is.False);
        }

        [Test]
        public void SameUiCanBeReusedForThreeConsecutiveLoads()
        {
            for (int i = 0; i < 3; i++)
            {
                _loading.Load(0);
                _presentation.Covered = true;
                Tick();
                Tick();
                _presentation.Hidden = true;
                Tick();
                Assert.That(_loading.IsLoading, Is.False);
            }
            Assert.That(_events.FindAll(value => value == "cover").Count, Is.EqualTo(3));
            Assert.That(_events.FindAll(value => value == "reveal").Count, Is.EqualTo(3));
            Assert.That(_events.FindAll(value => value == "end").Count, Is.EqualTo(3));
        }

        [Test]
        public void RequestsAndPresentationReplacementAreRejectedUntilRevealEnds()
        {
            _loading.Load(0);
            Assert.Throws<InvalidOperationException>(() => _loading.Load(0));
            Assert.Throws<InvalidOperationException>(() => _loading.Unload());
            Assert.Throws<InvalidOperationException>(() => _loading.UnbindPresentation(_presentation));
            Assert.Throws<InvalidOperationException>(() => _loading.BindPresentation(new Presentation(_events)));
            _presentation.Covered = true;
            Tick();
            Tick();
            Assert.Throws<InvalidOperationException>(() => _loading.Load(0));
        }

        [Test]
        public void BeforeFailureClosesUiAndAllowsRetry()
        {
            bool fail = true;
            _context.LodingRegisterService.RegisterBeforeLoad(new LodingNode(-1, () =>
            {
                if (fail) throw new InvalidOperationException("주입한 로딩 실패");
            }));
            _loading.Load(0);
            _presentation.Covered = true;
            Tick();
            Assert.Throws<InvalidOperationException>(() => Tick());
            Assert.That(_loading.IsLoading, Is.False);
            Assert.That(_presentation.Hidden, Is.True);
            fail = false;
            Assert.DoesNotThrow(() => _loading.Load(0));
        }

        [Test]
        public void SceneEnterFailureStillRunsAfterNodesAndClosesUi()
        {
            _context.LodingRegisterService.RegisterSceneEnterLoaded(0, new LodingNode(1, () => throw new InvalidOperationException("진입 실패")));
            _loading.Load(0);
            _presentation.Covered = true;
            Tick();
            Assert.Throws<InvalidOperationException>(() => Tick());
            Assert.That(_events, Does.Contain("after"));
            Assert.That(_events[_events.Count - 1], Is.EqualTo("end"));
            Assert.That(_loading.IsLoading, Is.False);
        }

        [Test]
        public void LegacyCompositionWithoutPresentationKeepsImmediateSameSceneLoad()
        {
            _loading.UnbindPresentation(_presentation);
            _loading.Load(0);
            Assert.That(_events, Is.EqualTo(new[] { "before0", "before10", "exit", "enter", "after" }));
            Assert.That(_loading.IsLoading, Is.False);
        }

        [Test]
        public void ShutdownDoesNotCallAnAlreadyShutdownUiSystem()
        {
            _loading.Load(0);
            ((ISystemShutdown)_group).Shutdown();
            Assert.That(_events, Is.EqualTo(new[] { "cover" }));
            Assert.That(_loading.IsLoading, Is.False);
            Assert.DoesNotThrow(() => _loading.UnbindPresentation(_presentation));
        }

        private void Tick() => ((ISystemTick)_group).Tick(0f);

        private sealed class Presentation : ILodingPresentation
        {
            private readonly List<string> _events;
            public bool Covered;
            public bool Hidden;
            public bool IsCovered => Covered;
            public bool IsHidden => Hidden;
            public Presentation(List<string> events) => _events = events;
            public void BeginCover() { Covered = Hidden = false; _events.Add("cover"); }
            public void BeginReveal() => _events.Add("reveal");
            public void End() { Hidden = true; Covered = false; _events.Add("end"); }
        }
    }
}

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    /// <summary>실제 로딩 Factory·UISystem·프리팹을 연결하는 검증 전용 구성.</summary>
    internal sealed class JourneyLoadingFixture : IDisposable
    {
        private readonly IExportSystemServices _group;
        private readonly GameObject _canvasHost;
        public ILodingSystemServiceContext Context { get; }
        public ILodingSystemService Loading => Context.LodingService;
        public IUISystemService UI { get; }
        public SandstormLoadingUI Screen { get; }
        public SandstormTransitionVisual Visual { get; }

        public JourneyLoadingFixture(bool bindScene)
        {
            _group = LodingFactory.Create();
            Context = (ILodingSystemServiceContext)_group;
            ((ISystemInitialize)_group).Initialize();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(JourneyLoadingInstaller.GlobalPrefabPath);
            var source = prefab.GetComponentInChildren<SandstormLoadingUI>(true);
            if (source == null) throw new InvalidOperationException("먼저 공용 로딩 UI 설치 메뉴를 실행하세요.");
            // 운영과 같은 Overlay 부모를 유지한다. 하위 Canvas만 루트로 복제하면 WorldSpace 설정이 적용된다.
            _canvasHost = new GameObject("LoadingVerificationHost", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _canvasHost.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = _canvasHost.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            Screen = UnityEngine.Object.Instantiate(source.gameObject, _canvasHost.transform, false).GetComponent<SandstormLoadingUI>();
            Visual = Screen.GetComponentInChildren<SandstormTransitionVisual>(true);
            UI = (IUISystemService)Activator.CreateInstance(typeof(IUISystemService).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.UISystem", true));
            var global = new TestGlobal(Context, UI);
            var builder = new SystemBuilderHelper();
            builder.AddService<IGlobalSystemGroupService>(global);
            ISystemServiceStoreReader resolver = builder.BuildSystemReader().ServiceStoreReader;
            if (bindScene)
            {
                // Play 검증의 빈 씬에서는 실제 최초 탐색/재바인딩 경로도 확인한다.
                ((ISystemBindScene)UI).BindScene();
            }
            else
            {
                // EditMode에서는 사용자 편집 씬의 UI를 재바인딩하지 않는다.
                Screen.ExportUIService((IUIRegistry)UI);
                Screen.BindUISystemService(UI);
                Screen.BindUIService(resolver);
            }
            ((ISystemPostBindService)UI).PostBindService(resolver);
            ((ISystemInitialize)UI).Initialize();
        }

        public void Tick() => ((ISystemTick)_group).Tick(0f);

        public void AdvanceVisual(float seconds)
        {
            Visual.UseExternalClock = true;
            try { Visual.Advance(seconds); }
            finally { Visual.UseExternalClock = false; }
        }

        public void Dispose()
        {
            ((ISystemShutdown)UI).Shutdown();
            ((ISystemShutdown)_group).Shutdown();
            if (Screen != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(_canvasHost);
                else UnityEngine.Object.DestroyImmediate(_canvasHost);
            }
        }

        private sealed class TestGlobal : IGlobalSystemGroupService
        {
            public TestGlobal(ILodingSystemServiceContext loading, IUISystemService ui) { LodingServiceCommand = loading; UIServiceCommand = ui; }
            public ILodingSystemServiceContext LodingServiceCommand { get; }
            public IUISystemService UIServiceCommand { get; }
            public IInputSystemServiceContext InputServiceCommand => null;
            public IOptionCloseRequestService OptionCloseRequests => null;
            public IRenderServiceContext RenderServiceCommand => null;
            public IGlobalUIPresentation GlobalUIPresentation => null;
            public IUITextPresentation UITextPresentation => null;
            public IUtilitySystemServiceContext UtilityServiceCommand => null;
            public IJobSystemServiceContext JobServiceCommand => null;
            public IAudioSystemService AudioServiceCommand => null;
        }
    }
}

namespace ProjectL.Render.Shader.View.CustomShader.UI.Tests.Editor
{
    public sealed class JourneyLoadingIntegrationTests
    {
        [Test]
        public void RealUiRegistryAndVisualCanLoadTwiceWithoutStartupOrBackHistory()
        {
            using (var fixture = new JourneyLoadingFixture(false))
            {
                fixture.Loading.GetType().GetProperty("CurrentSceneIndex").SetValue(fixture.Loading, 0);
                Assert.That(fixture.Screen, Is.InstanceOf<GlobalUIBase>());
                Assert.That(fixture.Screen.OpenWithGlobalUI, Is.False);
                Assert.That(fixture.Screen.Policy, Is.EqualTo(UIOpenPolicy.Parallel));
                fixture.UI.OpenGlobalUI();
                Assert.That(fixture.Screen.gameObject.activeSelf, Is.False);
                for (int i = 0; i < 2; i++)
                {
                    fixture.Loading.Load(0);
                    Assert.That(fixture.Visual.Phase, Is.EqualTo(SandstormPhase.Covering));
                    fixture.UI.BackCloseUI();
                    Assert.That(fixture.Screen.gameObject.activeSelf, Is.True, "로딩은 뒤로가기 이력에 포함되지 않는다");
                    fixture.AdvanceVisual(2f);
                    Assert.That(fixture.Screen.IsCovered, Is.False, "빠른 로딩도 여정의 페이드인이 끝나기를 기다린다");
                    fixture.AdvanceVisual(0.3f);
                    fixture.Tick();
                    fixture.Tick();
                    Assert.That(fixture.Visual.Phase, Is.EqualTo(SandstormPhase.Revealing));
                    fixture.AdvanceVisual(0.525f);
                    fixture.Tick();
                    Assert.That(fixture.Visual.GetComponent<CanvasGroup>().alpha, Is.EqualTo(0.5f).Within(0.001f));
                    Assert.That(fixture.Loading.IsLoading, Is.True, "페이드 완료까지 로딩 상태를 유지한다");
                    Assert.That(fixture.Screen.gameObject.activeSelf, Is.True);
                    Assert.That(fixture.Visual.GetComponent<CanvasGroup>().blocksRaycasts, Is.True);
                    fixture.AdvanceVisual(2f);
                    fixture.Tick();
                    Assert.That(fixture.Loading.IsLoading, Is.False);
                    Assert.That(fixture.Screen.gameObject.activeSelf, Is.False);
                    Assert.That(fixture.Visual.IsHidden, Is.True);
                }
            }
        }
    }
}
