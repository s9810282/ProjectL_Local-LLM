using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    public static class LodingFactory
    {
        public static IExportSystemServices Create()
        {
            SystemBuilderHelper builder = new();

            var lodingRegisterSystem = new LodingRegisterSystem();
            lodingRegisterSystem.ExportServices(builder);

            var lodingSystem = new LodingSystem(lodingRegisterSystem);
            lodingSystem.ExportServices(builder);

            return new LodingSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
