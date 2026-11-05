using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;

namespace ProjectL.Systems.ServiceKeys.Runtime.Buffer
{
    internal static class LobbyViewBufferKey
    {
        public static readonly SystemGroupKey<ILobbyViewBufferReadSink> Read = new(nameof(Read));

        public static readonly SystemGroupKey<ILobbyViewBufferWriteSink> Write = new(nameof(Write));
    }
}
