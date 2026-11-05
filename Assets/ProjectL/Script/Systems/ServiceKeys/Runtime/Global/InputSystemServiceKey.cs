using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;

namespace ProjectL.Systems.ServiceKeys.Runtime.Global
{
    internal static class InputSystemServiceKey
    {
        public static readonly SystemGroupKey<IKeyBindingService> KeyBindings =
            new(nameof(KeyBindings));
        public static readonly SystemGroupKey<IInputSystemService> Input =
            new(nameof(Input));
        public static readonly SystemGroupKey<IInputLayerOverrideService> LayerOverride =
            new(nameof(LayerOverride));
        public static readonly SystemGroupKey<IOptionCloseRequestService> OptionClose =
            new(nameof(OptionClose));
    }
}
