using ProjectL.Systems.DataBaseSystem;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem
{
    #region 서비스

    public interface IRenderServiceContext : IServiceModule
    {
        IRenderCoreServiceContext CoreGroup { get; }
        IRenderViewServiceContext ViewGroup { get; }
    }

    #endregion

    #region 시스템 모듈

    internal class RenderSystemGroup : 
        SystemGroup,
        IRenderServiceContext,

        ISystemBindScene,
        ISystemInitData<IHasRenderQuery>,
        ISystemInitialize,
        ISystemPostBindService,
        ISystemShutdown
    {
        private readonly RenderRuntimeRoot runtimeRoot;
        public IRenderCoreServiceContext CoreGroup { get; }
        public IRenderViewServiceContext ViewGroup { get; }

        public RenderSystemGroup(ISystemReader systemReader, RenderRuntimeRoot runtimeRoot) : base(systemReader)
        {
            this.runtimeRoot = runtimeRoot ?? throw new System.ArgumentNullException(nameof(runtimeRoot));
            CoreGroup = new RenderCoreGroup(systemReader);
            ViewGroup = new RenderViewGroup(systemReader);
        }

        public void BindScene()
        {
            runtimeRoot.BindScene();

            for (int i = 0; i < SystemReader.SystemModuleStoreReader.BindSceneModules.Count; i++)
            {
                SystemReader.SystemModuleStoreReader.BindSceneModules[i].BindScene();
            }
        }

        public void InitData(IHasRenderQuery systemDataQuery)
        {
            for (int i = 0; i < SystemReader.SystemModuleStoreReader.InitDataModules.Count; i++)
            {
                SystemReader.SystemModuleStoreReader.InitDataModules[i].InitData(systemDataQuery.RenderQuery);
            }
        }

        public void Initialize()
        {
            for (int i = 0; i < SystemReader.SystemModuleStoreReader.InitializeModules.Count; i++)
            {
                SystemReader.SystemModuleStoreReader.InitializeModules[i].Initialize();
            }
        }

        public void PostBindService(ISystemServiceStoreReader systemServiceResolver)
        {
            for (int i = 0; i < SystemReader.SystemModuleStoreReader.PostBindServiceModules.Count; i++)
            {
                SystemReader.SystemModuleStoreReader.PostBindServiceModules[i].PostBindService(systemServiceResolver);
            }
        }

        public void Shutdown()
        {
            for (int i = 0; i < SystemReader.SystemModuleStoreReader.ShutdownModules.Count; i++)
            {
                SystemReader.SystemModuleStoreReader.ShutdownModules[i].Shutdown();
            }
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRenderServiceContext>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
