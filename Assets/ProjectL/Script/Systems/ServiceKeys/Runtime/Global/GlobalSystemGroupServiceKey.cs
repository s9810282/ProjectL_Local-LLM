using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global
{
    public static class GlobalSystemGroupServiceKey
    {
        public static readonly SystemGroupKey<IJobSystemServiceContext> Job =
            new(nameof(Job));

        public static readonly SystemGroupKey<IRenderServiceContext> Render =
            new(nameof(Render));

        public static readonly SystemGroupKey<IUtilitySystemServiceContext> Utility =
            new(nameof(Utility));

        public static readonly SystemGroupKey<IInputSystemServiceContext> Input =
            new(nameof(Input));

        public static readonly SystemGroupKey<ILodingSystemServiceContext> Loding =
            new(nameof(Loding));
    }
}
