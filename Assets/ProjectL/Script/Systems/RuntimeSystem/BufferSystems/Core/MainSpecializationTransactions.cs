using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    internal interface IMainSpecializationTransaction
    {
        PreparedMainSpecializationApplication Prepare(RunId expectedRun, long expectedRosterRevision, MainSpecializationChange change);
        void Install(PreparedMainSpecializationApplication prepared);
    }

    // 기존 불변 조회값만 보존한다. 로스터·덱·성장의 별도 상태 원본이 아니다.
    internal sealed class MainSpecializationSource
    {
        internal RunId RunId { get; }
        internal long Revision { get; }
        internal RunDeckSnapshot Deck { get; }
        internal MainCharacterPersistentState Main { get; }
        internal TraitGrowthOffer Offer { get; }
        internal TraitGrowthProcessedResult Result { get; }
        internal int Handled { get; }
        internal int Acknowledged { get; }
        private readonly long _battleRevision;
        private readonly long _battleInstanceId;

        internal MainSpecializationSource(IBufferQuery buffers)
        {
            RunId = buffers.RunProgressReadSink.RunId;
            Revision = buffers.CharacterReadSink.RosterRevision;
            buffers.RunDeckReadSink.TryGetCurrent(out var deck);
            Deck = deck;
            if (deck != null && buffers.CharacterReadSink.TryGetMainState(deck.MainId, out var main)) Main = main;
            Offer = buffers.TraitGrowthStateReadSink.ActiveOffer;
            Result = buffers.TraitGrowthResultReadSink.LatestResult;
            Handled = buffers.TraitGrowthResultReadSink.HandledCommandNumber;
            Acknowledged = buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber;
            _battleRevision = buffers.CombatStateReadSink.QueryRevision;
            _battleInstanceId = buffers.CombatStateReadSink.BattleInstanceId;
        }

        internal bool Matches(IBufferQuery b)
        {
            b.RunDeckReadSink.TryGetCurrent(out var deck);
            return b.RunProgressReadSink.RunId == RunId && b.RunProgressReadSink.Outcome == RunOutcome.InProgress &&
                b.CharacterReadSink.RosterRevision == Revision && !b.CombatStateReadSink.HasBattle &&
                b.CharacterReadSink.Characters.Count == 0 && b.CombatStateReadSink.QueryRevision == _battleRevision &&
                b.CombatStateReadSink.BattleInstanceId == _battleInstanceId &&
                deck != null && Deck != null && deck.RunId == Deck.RunId && deck.DeckRevision == Deck.DeckRevision &&
                deck.OrderedIds.SequenceEqual(Deck.OrderedIds) && Main != null &&
                b.CharacterReadSink.TryGetMainState(deck.MainId, out var main) && Main.SameValues(main) &&
                ReferenceEquals(Offer, b.TraitGrowthStateReadSink.ActiveOffer) &&
                ReferenceEquals(Result, b.TraitGrowthResultReadSink.LatestResult) &&
                Handled == b.TraitGrowthResultReadSink.HandledCommandNumber &&
                Acknowledged == b.TraitGrowthViewReadSink.AcknowledgedResultNumber;
        }

        internal bool Allows(MainSpecializationChange change) => !RunId.IsEmpty && Deck != null && Deck.RunId == RunId &&
            Main != null && Main.CurrentHealth > 0 && change.Expected.Role == CharacterOwnershipRole.Main &&
            change.Expected.Id == Deck.MainId && Offer == null && Handled >= 0 && Handled == Acknowledged &&
            (Result == null || Result.RunId == RunId);
    }

    internal sealed class PreparedMainSpecializationApplication
    {
        internal MainSpecializationTransactions Owner { get; }
        internal MainSpecializationSource Source { get; }
        internal PreparedMainSpecializationChange Character { get; }
        internal PreparedMainSpecializationApplication(MainSpecializationTransactions owner, MainSpecializationSource source,
            PreparedMainSpecializationChange character)
        {
            Owner = owner;
            Source = source;
            Character = character;
        }
    }

    internal sealed class MainSpecializationTransactions : IMainSpecializationTransaction
    {
        private readonly IBufferQuery _buffers;
        private readonly ICharacterBufferStore _characters;

        internal MainSpecializationTransactions(IBufferQuery buffers, ICharacterBufferStore characters)
        {
            _buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        public PreparedMainSpecializationApplication Prepare(RunId expectedRun, long expectedRosterRevision, MainSpecializationChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            var source = new MainSpecializationSource(_buffers);
            if (source.RunId != expectedRun || source.Revision != expectedRosterRevision || !source.Allows(change) || !source.Matches(_buffers))
                throw new InvalidOperationException("전직 대상 또는 원본 문맥이 올바르지 않습니다.");
            var character = _characters.PrepareMainSpecialization(change);
            if (!source.Matches(_buffers)) throw new InvalidOperationException("전직 준비 중 원본이 변경되었습니다.");
            return new PreparedMainSpecializationApplication(this, source, character);
        }

        public void Install(PreparedMainSpecializationApplication prepared)
        {
            if (prepared == null) throw new ArgumentNullException(nameof(prepared));
            if (prepared.Owner != this || prepared.Character.Installed || !prepared.Source.Matches(_buffers) ||
                !prepared.Source.Allows(prepared.Character.Change))
                throw new InvalidOperationException("전직 준비의 소유자·수명 또는 원본이 올바르지 않습니다.");
            _characters.InstallMainSpecialization(prepared.Character);
        }
    }
}
