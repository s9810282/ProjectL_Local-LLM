using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem
{
    public interface IAreaOutlineHandle : IDisposable
    {
        void Update(AreaOutlineData data);
    }

    public interface IAreaOutlineSystemService : IServiceModule
    {
        IAreaOutlineHandle Create();
    }

    internal sealed class AreaOutlineSystem :
        ISystemBindScene,
        ISystemShutdown,
        IExportSystemServices,
        ISystemInitData<IHasMaterialQuery>,
        ISystemInitialize,
        ISystemPostBindService,
        IAreaOutlineSystemService,
        IAreaOutlineHandlePoolService
    {
        private readonly RenderRuntimeRoot runtimeRoot;
        private readonly HashSet<AreaOutlineHandle> activeHandles = new HashSet<AreaOutlineHandle>();
        private AreaOutlineHandlePool handlePool;
        private Transform areaOutlineRoot;
        private IHasMaterialQuery materialQuery;
        private IMeshSystemService meshSystem;
        private IColorSystemService colorSystem;

        internal AreaOutlineSystem(RenderRuntimeRoot runtimeRoot)
        {
            this.runtimeRoot = runtimeRoot ?? throw new ArgumentNullException(nameof(runtimeRoot));
        }

        public void BindScene()
        {
            areaOutlineRoot = runtimeRoot.GetOrCreateSystemRoot("AreaOutlineRoot");
        }

        public void InitData(IHasMaterialQuery data)
        {
            materialQuery = data ?? throw new ArgumentNullException(nameof(data));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var global = resolver.GetService(SystemGroupsService.Global);
            meshSystem = global.RenderServiceCommand.CoreGroup.MeshSystemServices;
            colorSystem = global.RenderServiceCommand.CoreGroup.ColorSystemServices;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IAreaOutlineSystemService>(this);
            sink.AddModule(this);
        }

        public void Initialize()
        {
            if (materialQuery?.MetrialTableQuery == null)
                throw new InvalidOperationException("AreaOutlineSystem requires a material query before initialization.");

            Material material = materialQuery.MetrialTableQuery.GetMaterial("_AreaOutline").Material;
            if (areaOutlineRoot == null)
                throw new InvalidOperationException("AreaOutlineSystem requires BindScene before initialization.");

            handlePool = new AreaOutlineHandlePool(this, material, areaOutlineRoot);
        }

        public IAreaOutlineHandle Create()
        {
            if (meshSystem == null || colorSystem == null || handlePool == null)
                throw new InvalidOperationException("AreaOutlineSystem has not completed initialization.");

            AreaOutlineHandle handle = handlePool.GetHandle();
            IMeshRenderHandle meshHandle = null;
            IColorHandle colorHandle = null;

            try
            {
                meshHandle = meshSystem.Register(handle.MeshFilter);
                colorHandle = colorSystem.CreateColorHandle(handle.MeshRenderer);
                if (colorHandle == null)
                    throw new InvalidOperationException("AreaOutline could not register its MeshRenderer with ColorSystem.");

                handle.Bind(meshHandle, colorHandle);
                activeHandles.Add(handle);
                return handle;
            }
            catch
            {
                colorHandle?.Dispose();
                meshHandle?.Dispose();
                handlePool.ReturnHandle(handle);
                throw;
            }
        }

        public void Shutdown()
        {
            var handlesToDispose = new List<AreaOutlineHandle>(activeHandles);
            foreach (var handle in handlesToDispose)
                handle.Dispose();

            activeHandles.Clear();
            handlePool?.Dispose();
            handlePool = null;
        }

        void IAreaOutlineHandlePoolService.Return(AreaOutlineHandle handle)
        {
            if (!activeHandles.Remove(handle))
                return;

            handlePool?.ReturnHandle(handle);
        }
    }
}
