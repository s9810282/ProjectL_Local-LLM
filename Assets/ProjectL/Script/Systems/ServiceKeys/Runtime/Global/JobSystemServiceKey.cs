using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global
{
    internal static class JobSystemServiceKey
    {
        public static readonly SystemGroupKey<IJobRunner> JobRunner =
            new(nameof(JobRunner));

        public static readonly SystemGroupKey<IJobRegisterSystem> JobRegister =
            new(nameof(JobRegister));
    }
}
