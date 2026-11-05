using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Run
{
    public interface IRunDeckReadSink : IServiceModule
    {
        bool TryGetCurrent(out RunDeckSnapshot deck);
    }

    internal interface IRunDeckStore : IServiceModule
    {
        RunDeckSnapshot Prepare(RunId runId, IReadOnlyList<CharacterInstanceId> canonicalIds);
        void Install(RunDeckSnapshot prepared);
        void Reset();
    }

    internal sealed class RunDeckBuffer : IRunDeckReadSink, IRunDeckStore, IExportSystemServices
    {
        private RunDeckSnapshot _current;
        private long _revision;
        internal RunDeckBuffer(long revision = 0)
        {
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            _revision = revision;
        }
        public bool TryGetCurrent(out RunDeckSnapshot deck)
        {
            deck = _current;
            return deck != null;
        }
        public RunDeckSnapshot Prepare(RunId runId, IReadOnlyList<CharacterInstanceId> canonicalIds)
        {
            if (canonicalIds == null) throw new ArgumentNullException(nameof(canonicalIds));
            if (_current != null && _current.RunId == runId && _current.OrderedIds.SequenceEqual(canonicalIds))
                return _current;
            return new RunDeckSnapshot(runId, checked(_revision + 1), canonicalIds);
        }

        public void Install(RunDeckSnapshot prepared)
        {
            _current = prepared;
            _revision = prepared.DeckRevision;
        }
        public void Reset() => _current = null;
        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunDeckReadSink>(this);
            sink.AddService<IRunDeckStore>(this);
        }
    }
}
