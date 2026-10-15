using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.View;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem
{
    #region 서비스

    public interface IUtilitySystemServiceContext : IServiceModule
    {
        IDelayService delayService { get; }
        IEventSystemService eventSystemService { get; }
    }

    #endregion

    #region 시스템 모듈

    internal class UtilitySystemGroup : SystemGroup, 
        IUtilitySystemServiceContext, 
        ISystemTick, 
        ISystemShutdown
    {
        public IDelayService delayService { get; }

        public IEventSystemService eventSystemService { get; }

        public UtilitySystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            delayService = SystemReader.ServiceStoreReader.GetService(UtilitySystemServiceKey.Delay);

            eventSystemService = SystemReader.ServiceStoreReader.GetService(UtilitySystemServiceKey.Event);
        }

        public void Tick(float deltatime)
        {
            for(int i = 0; i < SystemReader.SystemModuleStoreReader.TickModules.Count; i++) 
            {
                SystemReader.SystemModuleStoreReader.TickModules[i].Tick(deltatime);
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
            sink.AddService<IUtilitySystemServiceContext>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
