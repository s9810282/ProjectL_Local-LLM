using ProjectL.Framework.Composition;

namespace ProjectL.Framework.SystemBase
{
    #region 서비스

    public interface IServiceModule { }

    #endregion

    #region 시스템 모듈

    public interface ISystemModule { }

    public interface ISystemBindScene : ISystemModule
    {
        void BindScene();
    }

    public interface ISystemInitData : ISystemModule
    {
        void InitData(IServiceModule service);
    }

    public interface ISystemInitData<T> : ISystemInitData
    where T : IServiceModule
    {
        void InitData(T data);

        void ISystemInitData.InitData(IServiceModule service)
        {
            InitData((T)service);
        }
    }

    public interface ISystemPostBindService : ISystemModule
    {
        void PostBindService(ISystemServiceStoreReader systemServiceResolver);
    }

    public interface ISystemInitialize : ISystemModule
    {
        void Initialize();
    }

    public interface ISystemTick : ISystemModule
    {
        void Tick(float deltatime);
    }

    public interface ISystemShutdown : ISystemModule
    {
        void Shutdown();
    }

    public interface IExportSystemServices : ISystemModule
    {
        void ExportServices(ISystemWriteSink sink);
    }

    public interface IExportSystemLodingServices : ISystemModule
    {
        void ExportLoadingServices();
    }

    #endregion
}
