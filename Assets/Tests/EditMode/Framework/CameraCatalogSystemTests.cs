using System;
using System.Collections.Generic;
using System.Reflection;
using Cinemachine;
using NUnit.Framework;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem;
using UnityEngine;

public sealed class CameraCatalogSystemTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private ICameraCatalogService catalog;
    private CinemachineVirtualCamera play;
    private CinemachineVirtualCamera cinematic;
    private string playName;
    private string cinematicName;

    [SetUp]
    public void SetUp()
    {
        playName = "CatalogPlay_" + Guid.NewGuid().ToString("N");
        cinematicName = "CatalogCinematic_" + Guid.NewGuid().ToString("N");
        play = Create(playName);
        cinematic = Create(cinematicName);
        play.Priority = 3;
        cinematic.Priority = 4;
        cinematic.gameObject.SetActive(false);
        var lens = play.m_Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
        lens.OrthographicSize = 10f;
        play.m_Lens = lens;
        var type = typeof(ICameraCatalogService).Assembly.GetType(
            "ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem.CameraCatalogSystem", true);
        catalog = (ICameraCatalogService)Activator.CreateInstance(type, true);
        ((ISystemBindScene)catalog).BindScene();
    }

    [TearDown]
    public void TearDown()
    {
        if (catalog != null) ((ISystemShutdown)catalog).Shutdown();
        for (int i = objects.Count - 1; i >= 0; i--)
            if (objects[i] != null) UnityEngine.Object.DestroyImmediate(objects[i]);
        objects.Clear();
    }

    [Test]
    public void CollectsInactiveCamerasAndHandleControlsOnlyRequestedCamera()
    {
        using (var handle = catalog.GetHandle(cinematicName))
        {
            handle.SetPriority(20);
        }
        Assert.That(cinematic.Priority, Is.EqualTo(20));
        Assert.That(play.Priority, Is.EqualTo(3));
        Assert.That(cinematic.gameObject.activeSelf, Is.False, "Priority changes must not auto-activate a camera.");
    }

    [Test]
    public void OrthographicZoomChangesOnlyOrthographicSize()
    {
        var original = play.m_Lens;
        using (var handle = catalog.GetHandle(playName)) handle.SetZoom(5f);
        Assert.That(play.m_Lens.OrthographicSize, Is.EqualTo(5f));
        Assert.That(play.m_Lens.FieldOfView, Is.EqualTo(original.FieldOfView));
        Assert.That(play.m_Lens.NearClipPlane, Is.EqualTo(original.NearClipPlane));
    }

    [Test]
    public void PerspectiveZoomChangesFieldOfView()
    {
        var lens = play.m_Lens;
        lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        play.m_Lens = lens;
        using (var handle = catalog.GetHandle(playName)) handle.SetZoom(35f);
        Assert.That(play.m_Lens.FieldOfView, Is.EqualTo(35f));
        Assert.That(play.m_Lens.OrthographicSize, Is.EqualTo(10f));
    }

    [Test]
    public void CustomizeAppliesStructSettingsByValue()
    {
        var data = CameraCatalogData.Default;
        data.Position = new Vector3(3f, 4f, 5f);
        data.Rotation = Quaternion.Euler(20f, 30f, 0f);
        data.Follow = cinematic.transform;
        data.LookAt = cinematic.transform;
        data.Priority = 21;
        data.Lens.Dutch = 15f;
        data.StandbyUpdate = CinemachineVirtualCameraBase.StandbyUpdateMode.Never;
        data.Transitions.m_InheritPosition = true;
        using (var handle = catalog.GetHandle(playName))
        {
            handle.Customize(data);
            data.Position = Vector3.zero;
            data.Lens.Dutch = 0f;
        }
        Assert.That(play.transform.position, Is.EqualTo(new Vector3(3f, 4f, 5f)));
        Assert.That(Quaternion.Angle(play.transform.rotation, data.Rotation), Is.LessThan(0.001f));
        Assert.That(play.Follow, Is.SameAs(cinematic.transform));
        Assert.That(play.LookAt, Is.SameAs(cinematic.transform));
        Assert.That(play.Priority, Is.EqualTo(21));
        Assert.That(play.m_Lens.Dutch, Is.EqualTo(15f));
        Assert.That(play.m_StandbyUpdate, Is.EqualTo(data.StandbyUpdate));
        Assert.That(play.m_Transitions.m_InheritPosition, Is.True);
    }

    [Test]
    public void CustomizeCanClearTargetsAndSetActivation()
    {
        play.Follow = cinematic.transform;
        play.LookAt = cinematic.transform;
        var data = new CameraCatalogData(play) { Follow = null, LookAt = null, Active = false, Enabled = false };
        using (var handle = catalog.GetHandle(playName)) handle.Customize(data);
        Assert.That(play.Follow, Is.Null);
        Assert.That(play.LookAt, Is.Null);
        Assert.That(play.gameObject.activeSelf, Is.False);
        Assert.That(play.enabled, Is.False);
        Assert.That(play.Priority, Is.EqualTo(3));
        Assert.That(play.m_Lens.OrthographicSize, Is.EqualTo(10f));
    }

    [Test]
    public void ReturnedNodeIsReusedButOldHandleCannotControlOrReturnIt()
    {
        var first = catalog.GetHandle(playName);
        object firstNode = NodeOf(first);
        first.Dispose();
        using (var second = catalog.GetHandle(cinematicName))
        {
            Assert.That(NodeOf(second), Is.SameAs(firstNode));
            Assert.Throws<ObjectDisposedException>(() => first.SetPriority(100));
            Assert.Throws<ObjectDisposedException>(() => first.Customize(CameraCatalogData.Default));
            first.Dispose();
            second.SetPriority(25);
            Assert.That(cinematic.Priority, Is.EqualTo(25));
        }
    }

    [Test]
    public void RebindInvalidatesOldHandlesAndAcceptsNewSceneData()
    {
        var oldHandle = catalog.GetHandle(playName);
        UnityEngine.Object.DestroyImmediate(play.gameObject);
        play = Create(playName);
        ((ISystemBindScene)catalog).BindScene();
        using (var next = catalog.GetHandle(playName))
        {
            Assert.Throws<ObjectDisposedException>(() => oldHandle.SetPriority(100));
            oldHandle.Dispose();
            next.SetPriority(18);
            Assert.That(play.Priority, Is.EqualTo(18));
        }
    }

    [Test]
    public void DuplicateNameRebindPreservesExistingHandles()
    {
        using (var handle = catalog.GetHandle(playName))
        {
            Create(playName);
            Assert.Throws<InvalidOperationException>(() => ((ISystemBindScene)catalog).BindScene());
            handle.SetPriority(12);
            Assert.That(play.Priority, Is.EqualTo(12));
        }
    }

    [Test]
    public void ShutdownReturnsNodesWithoutRestoringCameraSettings()
    {
        var handle = catalog.GetHandle(playName);
        handle.SetPriority(15);
        handle.SetZoom(4f);
        ((ISystemShutdown)catalog).Shutdown();
        Assert.That(play.Priority, Is.EqualTo(15));
        Assert.That(play.m_Lens.OrthographicSize, Is.EqualTo(4f));
        Assert.Throws<ObjectDisposedException>(() => handle.SetZoom(8f));
        Assert.Throws<KeyNotFoundException>(() => catalog.GetHandle(playName));
        handle.Dispose();
        ((ISystemBindScene)catalog).BindScene();
        using (var next = catalog.GetHandle(playName)) next.SetPriority(6);
        Assert.That(play.Priority, Is.EqualTo(6));
    }

    [Test]
    public void RejectsMissingNameInvalidZoomAndDestroyedCamera()
    {
        Assert.Throws<ArgumentException>(() => catalog.GetHandle(""));
        Assert.Throws<KeyNotFoundException>(() => catalog.GetHandle("Missing_" + Guid.NewGuid()));
        Assert.Throws<KeyNotFoundException>(() => catalog.GetHandle(playName.ToLowerInvariant()));
        using (var handle = catalog.GetHandle(playName))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => handle.SetZoom(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => handle.SetZoom(0f));
            UnityEngine.Object.DestroyImmediate(play.gameObject);
            Assert.Throws<ObjectDisposedException>(() => handle.SetPriority(3));
            Assert.Throws<InvalidOperationException>(() => catalog.GetHandle(playName));
        }
    }

    [Test]
    public void CatalogIsNotAMonoBehaviourAndExportsOnlyHandleRequests()
    {
        Assert.That(catalog, Is.Not.InstanceOf<MonoBehaviour>());
        Assert.That(catalog, Is.Not.InstanceOf<ISystemTick>());
        Assert.That(typeof(ICameraCatalogService).GetMethods().Length, Is.EqualTo(1));
        Assert.That(typeof(ICameraCatalogService).GetMethods()[0].Name, Is.EqualTo("GetHandle"));
    }

    private CinemachineVirtualCamera Create(string name)
    {
        var obj = new GameObject(name);
        objects.Add(obj);
        return obj.AddComponent<CinemachineVirtualCamera>();
    }

    private static object NodeOf(ICameraCatalogHandle handle)
        => handle.GetType().GetField("node", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(handle);
}
