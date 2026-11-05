using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.UnityCameraCatalogSystem
{
    /// <summary>런타임 동안 유지되는 Unity 출력 카메라를 오브젝트 이름으로 조회합니다.</summary>
    public interface IUnityCameraCatalogService : IServiceModule
    {
        IUnityCameraCatalogHandle GetHandle(string objectName);
    }

    /// <summary>최초 씬 연결에서 출력 카메라를 수집·영속화하고 이후 씬 연결에서는 참조와 핸들을 유지합니다.</summary>
    internal sealed class UnityCameraCatalogSystem :
        IUnityCameraCatalogService,
        IUnityCameraCatalogHandlePoolService,
        IExportSystemServices,
        ISystemBindScene,
        ISystemShutdown
    {
        private readonly Dictionary<string, Camera> _cameras = new(StringComparer.Ordinal);
        private readonly HashSet<UnityCameraCatalogHandle> _rented = new();
        private readonly UnityCameraCatalogHandlePool _pool = new();
        private bool _isBound;

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<IUnityCameraCatalogService>(this);
            sink.AddModule(this);
        }

        public void BindScene()
        {
            if (_isBound)
                return;

            var found = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var next = new Dictionary<string, Camera>(found.Length, StringComparer.Ordinal);
            foreach (Camera camera in found)
            {
#if UNITY_EDITOR
                // 미리보기 씬의 카메라도 Game 타입일 수 있으므로 이름 검사 전에 제외합니다.
                if (EditorSceneManager.IsPreviewSceneObject(camera.gameObject))
                    continue;
#endif

                var scene = camera.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded || camera.cameraType != CameraType.Game)
                    continue;

                string objectName = camera.gameObject.name;
                if (string.IsNullOrWhiteSpace(objectName))
                    throw new InvalidOperationException("A Unity camera has an empty object name.");
                if (!next.TryAdd(objectName, camera))
                    throw new InvalidOperationException(
                        $"Duplicate Unity camera object name '{objectName}'. Use unique names across loaded scenes. " +
                        $"First: {DescribeCamera(next[objectName])}; Second: {DescribeCamera(camera)}");
            }

            if (Application.isPlaying)
            {
                var roots = new HashSet<Transform>();
                foreach (Camera camera in next.Values)
                {
                    // 계층을 보존하며 자식 카메라도 함께 유지하도록 최상위 루트에 적용합니다.
                    Transform root = camera.transform.root;
                    if (roots.Add(root))
                        UnityEngine.Object.DontDestroyOnLoad(root.gameObject);
                }
            }

            foreach (var entry in next)
                _cameras.Add(entry.Key, entry.Value);

            _isBound = true;
        }

        private static string DescribeCamera(Camera camera)
        {
            string hierarchy = camera.gameObject.name;
            for (Transform parent = camera.transform.parent; parent != null; parent = parent.parent)
                hierarchy = parent.name + "/" + hierarchy;

            var scene = camera.gameObject.scene;
            string sceneName = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            return $"scene='{sceneName}', hierarchy='{hierarchy}', instanceId={camera.GetInstanceID()}";
        }

        public IUnityCameraCatalogHandle GetHandle(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Unity camera object name is required.", nameof(objectName));
            if (!_cameras.TryGetValue(objectName, out Camera camera))
                throw new KeyNotFoundException($"Unity camera '{objectName}' is not in the catalog.");
            if (camera == null)
                throw new InvalidOperationException($"Unity camera '{objectName}' was destroyed.");

            UnityCameraCatalogHandle handle = _pool.Get();
            handle.Bind(this, camera);
            _rented.Add(handle);
            return handle;
        }

        void IUnityCameraCatalogHandlePoolService.Return(UnityCameraCatalogHandle handle)
        {
            if (!_rented.Remove(handle))
                return;

            _pool.Return(handle);
        }

        public void Shutdown()
        {
            _isBound = false;
            foreach (UnityCameraCatalogHandle handle in _rented)
                _pool.Return(handle);
            _rented.Clear();
            _cameras.Clear();
        }
    }
}
