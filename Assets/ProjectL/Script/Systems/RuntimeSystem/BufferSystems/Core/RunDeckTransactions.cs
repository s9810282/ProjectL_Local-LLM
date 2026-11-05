using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>Core가 정규화한 제안의 버전·참조를 재검증한 뒤 선택만 게시한다.</summary>
    internal sealed class RunDeckTransactions : IRunDeckWriteSink
    {
        private readonly IRunProgressBufferReadSink _run;
        private readonly ICharacterBufferReadSink _characters;
        private readonly IRunDeckReadSink _read;
        private readonly IRunDeckStore _store;
        private readonly ICombatStateBufferReadSink _combat;

        internal RunDeckTransactions(IRunProgressBufferReadSink run, ICharacterBufferReadSink characters,
            IRunDeckReadSink read, IRunDeckStore store, ICombatStateBufferReadSink combat)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _read = read ?? throw new ArgumentNullException(nameof(read));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        public DeckFailureReason TryPublish(DeckEditRequest expected, IReadOnlyList<CharacterInstanceId> canonicalIds)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (canonicalIds == null) throw new ArgumentNullException(nameof(canonicalIds));
            var reason = CheckExpected(expected, out var current);
            if (reason != DeckFailureReason.None) return reason;
            var ids = canonicalIds.ToArray();
            // 장수·메인 수·정규 순서는 Core 덱 규칙(DeckRules)이 판정한 결과다. 여기서는 제안이 요청 후보와 같은 ID 집합이고
            // 모두 현재 보유이며, 저장할 메인 ID(0번)가 보유 메인을 가리키는지만 확인한다.
            if (ids.Length == 0 || ids.Length != expected.CandidateIds.Count || ids.Distinct().Count() != ids.Length ||
                !new HashSet<CharacterInstanceId>(ids).SetEquals(expected.CandidateIds))
                return DeckFailureReason.StateChanged;
            foreach (var id in ids)
                if (!_characters.TryGetOwned(id, out _))
                    return DeckFailureReason.StateChanged;
            if (!_characters.TryGetOwned(ids[0], out var main) || main.Role != CharacterOwnershipRole.Main)
                return DeckFailureReason.StateChanged;
            var prepared = _store.Prepare(expected.ExpectedRunId, ids);
            reason = CheckExpected(expected, out var rechecked);
            if (reason != DeckFailureReason.None) return reason;
            if (!ReferenceEquals(current, rechecked)) return DeckFailureReason.StateChanged;
            _store.Install(prepared);
            return DeckFailureReason.None;
        }

        private DeckFailureReason CheckExpected(DeckEditRequest expected, out RunDeckSnapshot current)
        {
            current = null;
            if (_run.RunId.IsEmpty || !_read.TryGetCurrent(out current)) return DeckFailureReason.NotReady;
            if (_run.RunId != expected.ExpectedRunId || current.RunId != _run.RunId) return DeckFailureReason.StaleRun;
            if (_characters.RosterRevision != expected.ExpectedRosterRevision) return DeckFailureReason.StaleRoster;
            if (current.DeckRevision != expected.ExpectedDeckRevision) return DeckFailureReason.StaleDeck;
            if (_combat.HasBattle || _run.Outcome != RunOutcome.InProgress) return DeckFailureReason.WrongPhase;
            return DeckFailureReason.None;
        }
    }
}
