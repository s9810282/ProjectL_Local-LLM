using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;

namespace ProjectL.Systems.ServiceKeys.Runtime
{
    public static class SystemGroupsService
    {
        public static readonly SystemGroupKey<IGlobalSystemGroupService> Global =
            new(nameof(Global));

        public static readonly SystemGroupKey<IInGameSystemGroupService> InGame =
            new(nameof(InGame));

        public static readonly SystemGroupKey<ILifeCycleSystemGroupService> LifeCycle =
            new(nameof(LifeCycle));
    }
}
