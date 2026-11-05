using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.DrawPointLineSystem
{
    internal interface ILinePoolService : IDisposable
    {
        LineHandle GetHandle();
    }

    internal interface ILineHandlePoolService
    {
        void Return(LineHandle handle);
    }

    /// <summary>선 핸들 자체를 보관하고 하위 핸들을 조립해 반환합니다.</summary>
    internal sealed class LinePool : ILinePoolService, ILineHandlePoolService
    {
        private sealed class Lifecycle : IPoolLifecycle<LineHandle>
        {
            public void OnGet(LineHandle handle) => handle.Validate();
            public void OnBeforeReturn(LineHandle handle) => handle.Reset();
            public void OnReturn(LineHandle handle) { }
        }

        private readonly Material _material;
        private readonly Transform _root;
        private readonly IMeshSystemService _meshSystem;
        private readonly IColorSystemService _colorSystem;
        private readonly IEffectSystemServices _effectSystem;
        private readonly CustomPool<LineHandle> _pool;
        private readonly HashSet<LineHandle> _handles = new HashSet<LineHandle>();
        private readonly HashSet<LineHandle> _active = new HashSet<LineHandle>();
        private bool _disposed;

        internal LinePool(Material material, Transform root, IMeshSystemService meshSystem,
            IColorSystemService colorSystem, IEffectSystemServices effectSystem)
        {
            _material = material != null ? material : throw new ArgumentNullException(nameof(material));
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _meshSystem = meshSystem ?? throw new ArgumentNullException(nameof(meshSystem));
            _colorSystem = colorSystem ?? throw new ArgumentNullException(nameof(colorSystem));
            _effectSystem = effectSystem ?? throw new ArgumentNullException(nameof(effectSystem));
            // 하위 시스템 초기화 전에는 자원을 대여하지 않습니다.
            _pool = new CustomPool<LineHandle>(new PoolContext<LineHandle>(CreateHandle, 0, null, new Lifecycle()));
        }

        public LineHandle GetHandle()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(LinePool));

            LineHandle handle = _pool.Get();
            IMeshRenderHandle mesh = null;
            IColorHandle color = null;
            try
            {
                mesh = _meshSystem.Register(handle.MeshFilter);
                if (mesh == null)
                    throw new InvalidOperationException("DrawPointLine could not acquire a mesh handle.");
                color = _colorSystem.CreateColorHandle(handle.MeshRenderer);
                if (color == null)
                    throw new InvalidOperationException("DrawPointLine could not acquire a color handle.");

                handle.Bind(mesh, color);
                // Bind 뒤 하위 핸들의 반환 책임은 LineHandle.Reset으로 넘어갑니다.
                mesh = null;
                color = null;
                _active.Add(handle);
                return handle;
            }
            catch
            {
                try { color?.Dispose(); }
                finally
                {
                    try { mesh?.Dispose(); }
                    finally { _pool.Return(handle); }
                }
                throw;
            }
        }

        void ILineHandlePoolService.Return(LineHandle handle)
        {
            if (!_active.Remove(handle))
                return;
            _pool.Return(handle);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Exception failure = null;
            foreach (var handle in new List<LineHandle>(_active))
            {
                try { handle.Dispose(); }
                catch (Exception exception) { failure = failure ?? exception; }
            }
            foreach (var handle in _handles)
            {
                try { handle.DestroyGameObject(); }
                catch (Exception exception) { failure = failure ?? exception; }
            }
            _active.Clear();
            _handles.Clear();
            if (failure != null)
                throw new InvalidOperationException("DrawPointLine could not release every line resource.", failure);
        }

        private LineHandle CreateHandle()
        {
            var gameObject = new GameObject("DrawPointLine");
            gameObject.SetActive(false);
            gameObject.transform.SetParent(_root, false);
            try
            {
                var filter = gameObject.AddComponent<MeshFilter>();
                var renderer = gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.enabled = false;
                var anchor = new GameObject("EndPoint").transform;
                anchor.SetParent(gameObject.transform, false);
                EffectScope effect = _effectSystem.GetEffectHandle();
                if (effect == null)
                    throw new InvalidOperationException("DrawPointLine could not acquire an effect scope.");

                var handle = new LineHandle(this, gameObject, filter, renderer, anchor, effect);
                _handles.Add(handle);
                return handle;
            }
            catch
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(gameObject);
                throw;
            }
        }
    }
}
