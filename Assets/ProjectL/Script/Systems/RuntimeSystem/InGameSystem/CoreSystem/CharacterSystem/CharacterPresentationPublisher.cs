using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>Core Tick에서 보유 능력치를 계산하고 표시 결과를 Buffer에 게시한다.</summary>
    internal sealed class CharacterPresentationPublisher : ISystemPostBindService, ISystemTick, IExportSystemServices
    {
        private readonly ICharacterStatsReadSink _stats;
        private ICharacterStatsStore _statsStore;

        internal CharacterPresentationPublisher(ICharacterStatsReadSink stats)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _statsStore = buffers.CharacterStatsStore;
        }

        public void Tick(float deltaTime)
        {
            if (_statsStore == null)
                throw new InvalidOperationException("[CharacterPresentationPublisher] Buffer 바인딩 후 Tick해야 합니다.");
            if (_statsStore.PublicationRequested && !_statsStore.HasCurrent)
            {
                CharacterStatsSource source = _statsStore.Source;
                _statsStore.TryPublish(source, _stats.ReadOwned());
            }
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddModule<ISystemModule>(this);
        }
    }
}
