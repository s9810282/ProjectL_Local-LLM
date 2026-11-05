using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;

namespace ProjectL.Systems.ServiceKeys.Runtime.InGame
{
    internal static class InGameSystemGroupServiceKey
    {
        public static readonly SystemGroupKey<IInGameCoreSystemGroupService> Core =
            new(nameof(Core));

        public static readonly SystemGroupKey<IInGameViewSystemGroupService> View =
            new(nameof(View));
    }
}
