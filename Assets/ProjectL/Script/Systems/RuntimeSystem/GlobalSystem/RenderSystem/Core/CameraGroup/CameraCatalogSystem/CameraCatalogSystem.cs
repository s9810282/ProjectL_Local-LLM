using System;
using System.Collections.Generic;
using Cinemachine;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.CameraGroup.CameraCatalogSystem
{
    public interface ICameraCatalogService : IServiceModule
    {
        ICameraCatalogHandle GetHandle(string objectName);
    }

    /// <summary>씬의 가상 카메라를 이름으로 수집하고 풀에서 제어 Node를 대여한다.</summary>
    internal sealed class CameraCatalogSystem : ICameraCatalogService,
        ICameraCatalogHandlePoolService, IExportSystemServices, ISystemBindScene, ISystemShutdown
    {
        private readonly Dictionary<string, CameraCatalogNode> cameras =
            new Dictionary<string, CameraCatalogNode>(StringComparer.Ordinal);
        private readonly HashSet<CameraCatalogNode> rented = new HashSet<CameraCatalogNode>();
        private readonly CameraCatalogNodePool pool = new CameraCatalogNodePool();

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICameraCatalogService>(this);
            sink.AddModule(this);
        }

        public void BindScene()
        {
            var found = UnityEngine.Object.FindObjectsByType<CinemachineVirtualCamera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var next = new Dictionary<string, CinemachineVirtualCamera>(found.Length, StringComparer.Ordinal);
            foreach (var camera in found)
            {
                var scene = camera.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded) continue;
                string name = camera.gameObject.name;
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException("A scene virtual camera has an empty object name.");
                if (!next.TryAdd(name, camera))
                    throw new InvalidOperationException(
                        $"Duplicate virtual camera object name '{name}'. Use unique names across loaded scenes.");
            }

            // Validate collection before invalidating the previous catalog and handles.
            ReleaseAll();
            foreach (var entry in next)
            {
                var node = pool.Get();
                node.Bind(entry.Key, entry.Value);
                cameras.Add(entry.Key, node);
            }
        }

        public ICameraCatalogHandle GetHandle(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                throw new ArgumentException("Virtual camera object name is required.", nameof(objectName));
            if (!cameras.TryGetValue(objectName, out var source))
                throw new KeyNotFoundException($"Virtual camera '{objectName}' is not in the scene catalog.");
            if (source.Camera == null)
                throw new InvalidOperationException($"Virtual camera '{objectName}' was destroyed. Bind the scene again.");

            var node = pool.Get();
            node.Bind(source.ObjectName, source.Camera);
            rented.Add(node);
            return new CameraCatalogHandle(this, node);
        }

        void ICameraCatalogHandlePoolService.Return(CameraCatalogNode node, ulong version)
        {
            // An old lease must never return a Node already reused by a newer lease.
            if (node.Version != version || !rented.Remove(node)) return;
            pool.Return(node);
        }

        public void Shutdown() => ReleaseAll();

        private void ReleaseAll()
        {
            foreach (var node in rented) pool.Return(node);
            rented.Clear();
            foreach (var node in cameras.Values) pool.Return(node);
            cameras.Clear();
        }
    }
}
