using System;
using System.Reflection;
using Cinemachine;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem.Event;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using UnityEngine;

public sealed class CameraEffectSystemTests
{
    private GameObject cameraObject;
    private CinemachineVirtualCamera camera;
    private ICameraCatalogService catalog;
    private ICameraEffectService effects;
    private IUtilitySystemServiceContext utility;
    private IDelayService delay;

    [SetUp]
    public void SetUp()
    {
        cameraObject = new GameObject("EffectCamera_" + Guid.NewGuid().ToString("N"));
        camera = cameraObject.AddComponent<CinemachineVirtualCamera>();
        var lens = camera.m_Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        lens.OrthographicSize = 10f;
        camera.m_Lens = lens;
        catalog = (ICameraCatalogService)Create("CameraCatalogSystem.CameraCatalogSystem");
        ((ISystemBindScene)catalog).BindScene();
        effects = (ICameraEffectService)Create("CameraEffectSystem.CameraEffectSystem", catalog);
        utility = (IUtilitySystemServiceContext)UtilitySystemFactory.Create();
        delay = utility.delayService;
        ((ISystemPostBindService)effects).PostBindService(Resolver());
        ((ISystemBindScene)effects).BindScene();
    }

    [TearDown]
    public void TearDown()
    {
        if (effects != null) ((ISystemShutdown)effects).Shutdown();
        if (catalog != null) ((ISystemShutdown)catalog).Shutdown();
        if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    [Test]
    public void ZoomDispatchInterpolatesAndCompletes()
    {
        effects.Dispatch(Zoom(10f, 4f, 1f));
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(10f));
        ((ISystemTick)delay).Tick(0.5f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(7f).Within(0.001f));
        ((ISystemTick)delay).Tick(0.5f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(4f));
        ((ISystemTick)delay).Tick(1f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(4f));
    }

    [Test]
    public void InstantZoomReplacesRunningZoom()
    {
        effects.Dispatch(Zoom(10f, 4f, 1f));
        effects.Dispatch(Zoom(0f, 8f, 0f));
        ((ISystemTick)delay).Tick(1f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(8f));
    }

    [Test]
    public void InvalidZoomDoesNotChangeCameraOrCancelRunningZoom()
    {
        effects.Dispatch(Zoom(10f, 4f, 1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.Dispatch(Zoom(0f, 5f, 1f)));
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(10f));
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.Dispatch(Zoom(8f, 0f, 1f)));
        ((ISystemTick)delay).Tick(1f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(4f));
    }

    [Test]
    public void RebindCancelsZoomAndShutdownRejectsDispatch()
    {
        effects.Dispatch(Zoom(10f, 4f, 1f));
        ((ISystemBindScene)catalog).BindScene();
        ((ISystemBindScene)effects).BindScene();
        ((ISystemTick)delay).Tick(1f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(10f));
        ((ISystemShutdown)effects).Shutdown();
        Assert.Throws<InvalidOperationException>(() => effects.Dispatch(Zoom(10f, 4f, 0f)));
    }

    [Test]
    public void DestroyedCameraCancelsRunningZoomSafely()
    {
        effects.Dispatch(Zoom(10f, 4f, 1f));
        UnityEngine.Object.DestroyImmediate(cameraObject);
        Assert.DoesNotThrow(() => ((ISystemTick)delay).Tick(1f));
    }

    [Test]
    public void DispatchUsesRegisteredDataTypesAndRejectsInvalidShake()
    {
        var methods = typeof(ICameraEffectService).GetMethods();
        Assert.That(methods.Length, Is.EqualTo(1));
        Assert.That(methods[0].GetParameters()[0].ParameterType, Is.EqualTo(typeof(ICameraEventData)));
        foreach (var method in methods) Assert.That(method.IsGenericMethod, Is.False);
        Assert.Throws<ArgumentNullException>(() => effects.Dispatch(null));
        Assert.Throws<NotSupportedException>(() => effects.Dispatch(new UnknownEffect()));
        Assert.That(effects, Is.Not.InstanceOf<ISystemTick>());
        foreach (var name in new[] { "CameraZoomEvent", "CameraShakeEvent" })
        {
            var type = typeof(ICameraEffectService).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraEffectSystem.Event." + name, true);
            Assert.That(type.IsAbstract, Is.False);
            Assert.That(typeof(ICameraEvent).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(CameraEventServiceContext) }, null), Is.Not.Null);
        }
        var data = CameraShakeData.Default;
        data.Channel = 0;
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.Dispatch(data));
        data = CameraShakeData.Default;
        data.Duration = float.NaN;
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.Dispatch(data));
    }

    [Test]
    public void ShakeNeedsNoSourceAndShutdownCancelsOnlyOwnedSignals()
    {
        const int ownChannel = 1 << 27;
        const int otherChannel = 1 << 28;
        var manager = CinemachineImpulseManager.Instance;
        var otherDefinition = new CinemachineImpulseDefinition
        {
            m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
            m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Recoil,
            m_ImpulseDuration = 10f,
            m_ImpulseChannel = otherChannel
        };
        var other = otherDefinition.CreateAndReturnEvent(Vector3.zero, Vector3.down);
        try
        {
            var data = CameraShakeData.Default;
            data.Channel = ownChannel;
            data.Shape = CinemachineImpulseDefinition.ImpulseShapes.Recoil;
            data.Duration = 10f;
            effects.Dispatch(data);
            Assert.That(cameraObject.GetComponent<CinemachineImpulseSource>(), Is.Null);
            Assert.That(manager.GetImpulseAt(Vector3.zero, false, ownChannel, out var offset, out _), Is.True);
            Assert.That(offset.sqrMagnitude, Is.GreaterThan(0f));
            ((ISystemShutdown)effects).Shutdown();
            manager.GetImpulseAt(Vector3.zero, false, ownChannel, out offset, out _);
            Assert.That(offset.sqrMagnitude, Is.EqualTo(0f));
            Assert.That(manager.GetImpulseAt(Vector3.zero, false, otherChannel, out offset, out _), Is.True);
            Assert.That(offset.sqrMagnitude, Is.GreaterThan(0f));
        }
        finally { other.Cancel(manager.CurrentTime, true); }
    }

    [Test]
    public void RenderFactoryExposesCameraGroupAndForwardsLifecycle()
    {
        var render = (IRenderServiceContext)RenderSystemFactory.Create();
        var group = render.CoreGroup.CameraGroup;
        Assert.That(group, Is.Not.InstanceOf<MonoBehaviour>());
        Assert.That(group.CameraCatalogSystem, Is.Not.Null);
        Assert.That(group.CameraEffectSystem, Is.Not.InstanceOf<MonoBehaviour>());
        Assert.That(typeof(ICameraGroupServiceContext).GetMethods().Length, Is.EqualTo(2));
        try
        {
            ((ISystemPostBindService)render).PostBindService(Resolver(render));
            ((ISystemBindScene)render).BindScene();
            group.CameraEffectSystem.Dispatch(Zoom(10f, 4f, 1f));
            ((ISystemTick)utility).Tick(1f);
            Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(4f));
        }
        finally { ((ISystemShutdown)render).Shutdown(); }
    }

    private CameraZoomData Zoom(float from, float to, float duration) => new CameraZoomData
    {
        CameraName = cameraObject.name, From = from, To = to, Duration = duration
    };

    private static object Create(string suffix, params object[] args)
    {
        var type = typeof(ICameraGroupServiceContext).Assembly.GetType(
            "ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup." + suffix, true);
        return Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, args, null);
    }

    [Test]
    public void OldDelayCallbacksCannotAffectNewZoomAfterShutdownAndRebind()
    {
        effects.Dispatch(Zoom(10f, 4f, 1f));
        ((ISystemShutdown)effects).Shutdown();
        ((ISystemPostBindService)effects).PostBindService(Resolver());
        ((ISystemBindScene)effects).BindScene();
        effects.Dispatch(Zoom(10f, 6f, 2f));
        ((ISystemTick)delay).Tick(1f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(8f));
        ((ISystemTick)delay).Tick(1f);
        Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(6f));
    }

    [Test]
    public void EventsReceiveContextAndDisposeIsTerminalAndIdempotent()
    {
        var context = new CameraEventServiceContext(catalog, delay);
        var zoom = (ICameraEvent)Create("CameraEffectSystem.Event.CameraZoomEvent", context);
        var shake = (ICameraEvent)Create("CameraEffectSystem.Event.CameraShakeEvent", context);
        try
        {
            Assert.Throws<ArgumentException>(() => zoom.Execute(CameraShakeData.Default));
            Assert.Throws<ArgumentException>(() => shake.Execute(Zoom(10f, 4f, 1f)));
            zoom.Execute(Zoom(10f, 4f, 1f));
            zoom.Dispose();
            Assert.DoesNotThrow(() => zoom.Dispose());
            Assert.Throws<ObjectDisposedException>(() => zoom.Execute(Zoom(10f, 4f, 1f)));
            ((ISystemTick)delay).Tick(1f);
            Assert.That(camera.m_Lens.OrthographicSize, Is.EqualTo(10f));
            shake.Dispose();
            Assert.DoesNotThrow(() => shake.Dispose());
            Assert.Throws<ObjectDisposedException>(() => shake.Execute(CameraShakeData.Default));
        }
        finally
        {
            zoom.Dispose();
            shake.Dispose();
        }
    }

    [Test]
    public void MissingDelayBindingRejectsEffects()
    {
        var unbound = (ICameraEffectService)Create("CameraEffectSystem.CameraEffectSystem", catalog);
        ((ISystemBindScene)unbound).BindScene();
        Assert.Throws<InvalidOperationException>(() => unbound.Dispatch(Zoom(10f, 4f, 1f)));
        ((ISystemShutdown)unbound).Shutdown();
    }

    private ISystemServiceStoreReader Resolver(IRenderServiceContext render = null)
    {
        var builder = new SystemBuilderHelper();
        builder.AddService<IGlobalSystemGroupService>(new GlobalContext(utility, render));
        return builder.BuildSystemReader().ServiceStoreReader;
    }

    private struct UnknownEffect : ICameraEventData { }

    private sealed class GlobalContext : IGlobalSystemGroupService
    {
        public IUtilitySystemServiceContext UtilityServiceCommand { get; }
        public IRenderServiceContext RenderServiceCommand { get; }
        public IInputSystemServiceContext InputServiceCommand => null;
        public IUISystemService UIServiceCommand => null;
        public ILodingSystemServiceContext LodingServiceCommand => null;
        public IJobSystemServiceContext JobServiceCommand => null;
        public ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem.IAudioSystemService AudioServiceCommand => null;

        internal GlobalContext(IUtilitySystemServiceContext utility, IRenderServiceContext render)
        {
            UtilityServiceCommand = utility;
            RenderServiceCommand = render;
        }
    }
}
