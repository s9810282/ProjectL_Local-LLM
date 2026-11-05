using ProjectL.Systems.ServiceKeys.Runtime.Global;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem
{
    #region 서비스

    public interface IInputSystemServiceContext :
        IServiceModule
    {
        IInputSystemService InputSystemServices { get; }

        IInputLayerService InputLayerServices { get; }
        IInputLayerOverrideService InputLayerOverrides { get; }
        IKeyBindingService KeyBindings { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InputSystemGroup :
        SystemGroup,
        IInputSystemServiceContext,
        ISystemInitData,
        ISystemInitialize,
        ISystemShutdown
    {
        public IInputSystemService InputSystemServices { get; }

        public IInputLayerService InputLayerServices =>
            InputSystemServices;
        public IInputLayerOverrideService InputLayerOverrides { get; }
        public IKeyBindingService KeyBindings { get; }

        public InputSystemGroup(
            ISystemReader systemReader)
            : base(systemReader)
        {
            InputSystemServices =
                SystemReader.ServiceStoreReader.GetService(
                    InputSystemServiceKey.Input);
            InputLayerOverrides = SystemReader.ServiceStoreReader.GetService(
                InputSystemServiceKey.LayerOverride);
            KeyBindings = SystemReader.ServiceStoreReader.GetService(InputSystemServiceKey.KeyBindings);
        }

        public void InitData(IServiceModule data)
        {
            var modules =
                SystemReader.SystemModuleStoreReader
                    .InitDataModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].InitData(data);
            }
        }

        public void Initialize()
        {
            var modules =
                SystemReader.SystemModuleStoreReader
                    .InitializeModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].Initialize();
            }
        }

        public void Shutdown()
        {
            var modules =
                SystemReader.SystemModuleStoreReader
                    .ShutdownModules;

            for (int i = modules.Count - 1; i >= 0; i--)
            {
                modules[i].Shutdown();
            }
        }

        public override void ExportServices(
            ISystemWriteSink sink)
        {
            sink.AddService<IInputSystemServiceContext>(
                this);

            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
