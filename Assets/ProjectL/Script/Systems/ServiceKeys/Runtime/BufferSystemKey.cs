using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;

namespace ProjectL.Systems.ServiceKeys.Runtime
{
    internal static class BufferSystemKey
    {
        public static readonly SystemGroupKey<IBufferQuery> Buffer = new(nameof(Buffer));
    }
}
