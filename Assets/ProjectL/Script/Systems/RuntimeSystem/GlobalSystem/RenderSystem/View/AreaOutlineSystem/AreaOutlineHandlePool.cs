using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem
{
    internal sealed class AreaOutlineHandlePool : IDisposable
    {
        private sealed class Lifecycle : IPoolLifecycle<AreaOutlineHandle>
        {
            public void OnGet(AreaOutlineHandle item) => item.Validate();
            public void OnBeforeReturn(AreaOutlineHandle item) => item.Reset();
            public void OnReturn(AreaOutlineHandle item) { }
        }

        private readonly CustomPool<AreaOutlineHandle> pool;
        private readonly HashSet<AreaOutlineHandle> handles = new HashSet<AreaOutlineHandle>();
        private readonly IAreaOutlineHandlePoolService poolService;
        private readonly Material material;
        private readonly Transform root;

        public AreaOutlineHandlePool(
            IAreaOutlineHandlePoolService poolService,
            Material material,
            Transform root,
            int initialSize = 1)
        {
            this.poolService = poolService ?? throw new ArgumentNullException(nameof(poolService));
            this.material = material != null ? material : throw new ArgumentNullException(nameof(material));
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            pool = new CustomPool<AreaOutlineHandle>(new PoolContext<AreaOutlineHandle>(
                CreateHandle,
                initialSize,
                null,
                new Lifecycle()));
        }

        public AreaOutlineHandle GetHandle() => pool.Get();
        public void ReturnHandle(AreaOutlineHandle handle) => pool.Return(handle);

        public void Dispose()
        {
            foreach (AreaOutlineHandle handle in handles)
                handle.DestroyGameObject();
            handles.Clear();
        }

        private AreaOutlineHandle CreateHandle()
        {
            var gameObject = new GameObject("AreaOutline");
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            gameObject.SetActive(false);
            gameObject.transform.SetParent(root, false);

            var handle = new AreaOutlineHandle(poolService, gameObject, meshFilter, meshRenderer);
            handles.Add(handle);
            return handle;
        }
    }
}
