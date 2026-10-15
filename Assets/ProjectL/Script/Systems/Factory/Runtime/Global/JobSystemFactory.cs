using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.Factory.Runtime.Global
{
    #region 시스템 모듈

    internal static class JobSystemFactory
    {
        internal static IExportSystemServices Create()
        {
            SystemBuilderHelper builder = new();
            var runner = new JobRunnerSystem();
            runner.ExportServices(builder);
            new JobRegisterSystem(runner).ExportServices(builder);
            return new JobSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
