using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.ServiceKeys
{
    internal static class InputSystemServiceKey
    {
        public static readonly SystemGroupKey<IInputSystemService> Input =
            new(nameof(Input));
    }
}
