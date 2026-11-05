using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    internal interface ITraitGrowthTransaction
    {
        TraitGrowthOffer PublishOffer(RunId expectedRun, long expectedRevision, CharacterInstanceId ownedId, int stepId, TraitGrowthKind kind, IReadOnlyList<TraitGrowthChoice> choices);
        PreparedTraitGrowthPublication PrepareChoicePublication(RunId expectedRun, long expectedRevision, TraitGrowthOffer expectedOffer, TraitGrowthChange change, TraitGrowthProcessedResult result);
        void InstallChoicePublication(PreparedTraitGrowthPublication prepared);
        void Clear();
    }
    internal sealed class PreparedTraitGrowthPublication
    {
        internal TraitGrowthTransactions Owner { get; }
        internal RunId RunId { get; }
        internal long Revision { get; }
        internal TraitGrowthOffer Offer { get; }
        internal TraitGrowthProcessedResult Previous { get; }
        internal int Acknowledged { get; }
        internal PreparedTraitGrowthChange Growth { get; }
        internal TraitGrowthProcessedResult Result { get; }
        internal PreparedTraitGrowthPublication(TraitGrowthTransactions owner, RunId runId, long revision, TraitGrowthOffer offer,
            TraitGrowthProcessedResult previous, int acknowledged, PreparedTraitGrowthChange growth, TraitGrowthProcessedResult result)
        {
            Owner = owner;
            RunId = runId;
            Revision = revision;
            Offer = offer;
            Previous = previous;
            Acknowledged = acknowledged;
            Growth = growth;
            Result = result;
        }
    }

    /// <summary>기존 로스터 준비와 제안·결과 게시를 같은 저장 경계에서 함께 검증하고 설치한다.</summary>
    internal sealed class TraitGrowthTransactions : ITraitGrowthTransaction
    {
        private readonly IRunProgressBufferReadSink _run;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICharacterBufferStore _characterStore;
        private readonly ITraitGrowthStateReadSink _state;
        private readonly ITraitGrowthResultReadSink _results;
        private readonly ITraitGrowthStateStore _store;
        private readonly ITraitGrowthViewReadSink _input;
        private readonly ITraitGrowthViewBufferStore _inputStore;
        internal TraitGrowthTransactions(IRunProgressBufferReadSink run, ICombatStateBufferReadSink combat, ICharacterBufferReadSink characters,
            ICharacterBufferStore characterStore, ITraitGrowthStateReadSink state, ITraitGrowthResultReadSink results, ITraitGrowthStateStore store,
            ITraitGrowthViewReadSink input, ITraitGrowthViewBufferStore inputStore)
        {
            _run = run;
            _combat = combat;
            _characters = characters;
            _characterStore = characterStore;
            _state = state;
            _results = results;
            _store = store;
            _input = input;
            _inputStore = inputStore;
        }
        public TraitGrowthOffer PublishOffer(RunId expectedRun, long expectedRevision, CharacterInstanceId ownedId, int stepId,
            TraitGrowthKind kind, IReadOnlyList<TraitGrowthChoice> choices)
        {
            ValidateSource(expectedRun, expectedRevision, null);
            RequireSafeChoice();
            var copied = (choices ?? throw new ArgumentNullException(nameof(choices))).ToArray();
            if (!_characters.TryGetOwned(ownedId, out _))
                throw new InvalidOperationException("제안 대상 보유 기록이 없습니다.");
            ValidateSource(expectedRun, expectedRevision, null);
            RequireSafeChoice();
            return _store.PublishOffer(expectedRun, ownedId, stepId, kind, copied);
        }
        public PreparedTraitGrowthPublication PrepareChoicePublication(RunId expectedRun, long expectedRevision,
            TraitGrowthOffer expectedOffer, TraitGrowthChange change, TraitGrowthProcessedResult result)
        {
            ValidatePublication(expectedRun, expectedRevision, expectedOffer, change, result);
            var growth = change == null ? null : _characterStore.PrepareGrowth(change);
            ValidatePublication(expectedRun, expectedRevision, expectedOffer, change, result);
            return new PreparedTraitGrowthPublication(this, expectedRun, expectedRevision, expectedOffer, _results.LatestResult,
                _input.AcknowledgedResultNumber, growth, result);
        }
        public void InstallChoicePublication(PreparedTraitGrowthPublication prepared)
        {
            if (prepared == null)
                throw new ArgumentNullException(nameof(prepared));
            if (prepared.Owner != this || !ReferenceEquals(prepared.Previous, _results.LatestResult) || prepared.Acknowledged != _input.AcknowledgedResultNumber)
                throw new InvalidOperationException("성장 게시 준비의 소유자 또는 결과 문맥이 변경되었습니다.");
            ValidateSource(prepared.RunId, prepared.Revision, prepared.Offer);
            ValidateResult(prepared.RunId, prepared.Result);
            if (prepared.Growth != null)
                RequireSafeChoice();
            // 모든 실패 가능 검사를 마친 뒤 기존 로스터와 결과를 순서대로 설치한다.
            if (prepared.Growth != null)
                _characterStore.InstallGrowth(prepared.Growth);
            _store.InstallResult(prepared.Result, prepared.Growth != null);
        }
        private void ValidatePublication(RunId run, long revision, TraitGrowthOffer offer, TraitGrowthChange change, TraitGrowthProcessedResult result)
        {
            ValidateSource(run, revision, offer);
            ValidateResult(run, result);
            if ((change != null) != result.Result.Succeeded)
                throw new InvalidOperationException("성장 변경과 결과의 성공 여부가 다릅니다.");
            if (change == null)
                return;
            RequireSafeChoice();
            if (offer == null || offer.RunId != run || offer.OfferId != result.Request.OfferId || offer.OwnedId != result.Request.OwnedId ||
                offer.StepId != result.Request.StepId || change.Added.StepId != offer.StepId || change.Added.TraitKey != result.Request.TraitKey ||
                change.Expected.Id != offer.OwnedId || !offer.Choices.Any(c => c.TraitKey == change.Added.TraitKey) ||
                !change.Final.SameValues(result.Result.Final))
                throw new InvalidOperationException("성장 변경·동결 제안·요청 결과가 다릅니다.");
        }
        private void ValidateResult(RunId run, TraitGrowthProcessedResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (result.RunId != run || result.Number <= _results.HandledCommandNumber ||
                !_input.TryGetCommand(result.Number, out var command) || !command.Request.SameValues(result.Request))
                throw new InvalidOperationException("실제 제출 입력과 증가한 현재 Run 결과가 필요합니다.");
        }
        private void ValidateSource(RunId run, long revision, TraitGrowthOffer offer)
        {
            int handled = _results.HandledCommandNumber;
            int acknowledged = _input.AcknowledgedResultNumber;
            if (run.IsEmpty || _run.RunId != run || _characters.RosterRevision != revision || !ReferenceEquals(_state.ActiveOffer, offer) ||
                acknowledged < 0 || acknowledged > handled || acknowledged != handled ||
                _results.LatestResult != null && _results.LatestResult.RunId != run)
                throw new InvalidOperationException("성장 원본이 변경되었거나 앞선 결과의 소비를 기다리고 있습니다.");
        }
        private void RequireSafeChoice()
        {
            if (_run.Outcome != RunOutcome.InProgress || _combat.HasBattle || _characters.Characters.Count != 0)
                throw new InvalidOperationException("성장 선택은 진행 중인 Run의 전투 밖에서만 게시할 수 있습니다.");
        }
        public void Clear()
        {
            _store.Clear();
            _inputStore.Clear();
        }
    }
}
