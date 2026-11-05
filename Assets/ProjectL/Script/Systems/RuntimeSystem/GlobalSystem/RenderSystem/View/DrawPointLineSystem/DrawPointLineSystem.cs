using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.DrawPointLineSystem
{
    public interface IDrawPointLineSystemService : IServiceModule
    {
        ILineHandle Create();
    }

    /// <summary>반환한 핸들 참조는 폐기해야 합니다. 같은 핸들이 다음 대여에 재사용됩니다.</summary>
    public interface ILineHandle : IDisposable
    {
        void Update(DrawPointLineData data);
        void SetOverrideData<T>(TypeSettingKey<T> key, T value) where T : notnull;
    }

    /// <summary>메시·색·끝점 이펙트를 조합하는 상위 렌더 시스템입니다.</summary>
    internal sealed class DrawPointLineSystem :
        ISystemBindScene,
        ISystemInitData<IHasMaterialQuery>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices,
        IDrawPointLineSystemService
    {
        private const string MaterialKey = "_LineColor";
        private readonly RenderRuntimeRoot _runtimeRoot;
        private Transform _root;
        private Material _material;
        private IMeshSystemService _meshSystem;
        private IColorSystemService _colorSystem;
        private IEffectSystemServices _effectSystem;
        private ILinePoolService _pool;

        internal DrawPointLineSystem(RenderRuntimeRoot runtimeRoot)
        {
            _runtimeRoot = runtimeRoot ?? throw new ArgumentNullException(nameof(runtimeRoot));
        }

        public void BindScene()
        {
            _root = _runtimeRoot.GetOrCreateSystemRoot("DrawPointLineRoot");
        }

        public void InitData(IHasMaterialQuery data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (data.MetrialTableQuery == null)
                throw new InvalidOperationException("DrawPointLine requires a material query.");

            _material = data.MetrialTableQuery.GetMaterial(MaterialKey).Material;
            if (_material == null || !_material.HasProperty(MaterialBaseTypeKey.MainColor.Name))
                throw new InvalidOperationException("LineColor requires a material supporting _BaseColor.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var render = resolver.GetService(SystemGroupsService.Global).RenderServiceCommand.CoreGroup;
            _meshSystem = render.MeshSystemServices;
            _colorSystem = render.ColorSystemServices;
            _effectSystem = render.EffectSystemServices;
        }

        public void Initialize()
        {
            if (_pool != null)
                throw new InvalidOperationException("DrawPointLine is already initialized.");
            if (_root == null || _material == null ||
                _meshSystem == null || _colorSystem == null || _effectSystem == null)
                throw new InvalidOperationException("DrawPointLine requires BindScene, InitData and PostBindService.");

            _pool = new LinePool(_material, _root, _meshSystem, _colorSystem, _effectSystem);
        }

        public ILineHandle Create()
        {
            if (_pool == null)
                throw new InvalidOperationException("DrawPointLine has not completed initialization.");
            return _pool.GetHandle();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));
            sink.AddService<IDrawPointLineSystemService>(this);
            sink.AddModule(this);
        }

        public void Shutdown()
        {
            _pool?.Dispose();
            _pool = null;
        }
    }
}
