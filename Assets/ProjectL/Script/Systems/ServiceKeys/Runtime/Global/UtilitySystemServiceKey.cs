using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.DelaySystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.EventBuse.View;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global
{
    internal static class UtilitySystemServiceKey
    {
        public static readonly SystemGroupKey<IDelayService> Delay =
            new(nameof(Delay));

        public static readonly SystemGroupKey<IEventSystemService> Event =
            new(nameof(Event));
    }
}
