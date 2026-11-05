using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    internal readonly struct CharacterStatsSource : IEquatable<CharacterStatsSource>
    {
        private readonly RunId _run;
        private readonly long _roster;
        private readonly bool _battle;
        internal RunOutcome Outcome { get; }

        internal CharacterStatsSource(IRunProgressBufferReadSink run, ICharacterBufferReadSink characters,
            ICombatStateBufferReadSink combat)
        {
            _run = run.RunId;
            _roster = characters.RosterRevision;
            _battle = combat.HasBattle;
            Outcome = run.Outcome;
        }

        public bool Equals(CharacterStatsSource other) => _run == other._run && _roster == other._roster &&
            _battle == other._battle && Outcome == other.Outcome;
    }

    internal interface ICharacterStatsStore : IServiceModule
    {
        CharacterStatsSource Source { get; }
        bool HasCurrent { get; }
        bool PublicationRequested { get; }
        bool TryPublish(CharacterStatsSource source, CharacterStatsQueryResult result);
    }

    public interface ICharacterStatsRequestSink : IServiceModule
    {
        void RequestRefresh();
    }

    /// <summary>Core가 게시한 불변 표시값만 반환한다. 읽기 중 Core 호출이나 DB 계산을 하지 않는다.</summary>
    internal sealed class CharacterStatsBuffer : ICharacterStatsReadSink, ICharacterStatsRequestSink,
        ICharacterStatsStore, IExportSystemServices
    {
        private IRunProgressBufferReadSink _run;
        private ICharacterBufferReadSink _characters;
        private ICombatStateBufferReadSink _combat;
        private CharacterStatsSource _source;
        private CharacterStatsQueryResult _current;
        private CharacterStatsQueryResult _pending;
        public bool PublicationRequested { get; private set; }
        public void RequestRefresh() => PublicationRequested = true;

        internal void BindSources(IRunProgressBufferReadSink run, ICharacterBufferReadSink characters,
            ICombatStateBufferReadSink combat)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (combat == null) throw new ArgumentNullException(nameof(combat));
            _run = run;
            _characters = characters;
            _combat = combat;
            _current = null;
        }

        public CharacterStatsSource Source => _run != null
            ? new CharacterStatsSource(_run, _characters, _combat)
            : throw new InvalidOperationException("[CharacterStatsBuffer] 원본 Buffer 연결이 필요합니다.");
        public bool HasCurrent => _current != null && _source.Equals(Source);

        public CharacterStatsQueryResult ReadOwned()
        {
            if (HasCurrent) return _current;
            // 첫 Core Tick 전 또는 원본 변경 뒤 재게시를 기다리는 정상 상태다. 낡은 수치를 반환하지 않는다.
            RunOutcome outcome = Source.Outcome;
            if (_pending == null || _pending.RunOutcome != outcome)
                _pending = new CharacterStatsQueryResult(CharacterStatsQueryStatus.Pending, outcome,
                    Array.Empty<OwnedCharacterStatsSnapshot>());
            return _pending;
        }

        public bool TryPublish(CharacterStatsSource source, CharacterStatsQueryResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Status == CharacterStatsQueryStatus.Pending || result.RunOutcome != source.Outcome)
                throw new ArgumentException("[CharacterStatsBuffer] 원본과 일치하는 완료 결과가 필요합니다.", nameof(result));
            // 계산 중 재진입으로 원본이 교체되면 다음 Core Tick에서 다시 계산한다.
            if (!source.Equals(Source)) return false;
            _source = source;
            _current = result;
            PublicationRequested = false;
            return true;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<ICharacterStatsReadSink>(this);
            sink.AddService<ICharacterStatsRequestSink>(this);
            sink.AddService<ICharacterStatsStore>(this);
        }
    }
}
