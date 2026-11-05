using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    public interface ICharacterRosterService : IServiceModule, ITraitGrowthAvailabilityRead
    {
        RosterAcquireResult InitializeRoster(int loadoutKey);
        RosterAcquireResult AcquireSub(EntityKey key, int initialLevel = 1);
        RosterAcquireResult Acquire(EntityKey key, int initialLevel = 1);
        InitialDrawPlan PlanInitialDraw(int seed);
        TraitGrowthProgressSnapshot PrepareNextTraitGrowthOffer();
    }

    /// <summary>DB와 Buffer의 로스터 사실을 Core 규칙에 전달하고 결정된 획득을 반영한다.</summary>
    internal sealed partial class CharacterRosterService : ICharacterRosterService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, ISystemShutdown, IExportSystemServices
    {
        private IUnitTableQuery _units;
        private IPlayerLoadoutTableQuery _loadouts;
        private IDataBaseQueryContext _database;
        private IInGameQuery _query;
        private IBufferQuery _buffers;

        public void InitData(IDataBaseQueryContext data)
        {
            var query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
            _units = query.UnitTableQuery;
            _loadouts = query.PlayerLoadoutTableQuery;
            _database = data;
            _query = query;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public RosterAcquireResult InitializeRoster(int loadoutKey)
        {
            RequireReady();
            if (RunEnded)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.RunEnded);
            // 현재 Run에 로스터만 채운다(빈 로스터 필요). 이전 Run을 비우고 시작하는 경로는 IRunStartService.StartNewRun이다.
            var expectedRun = _buffers.RunProgressReadSink.RunId;
            long expectedRevision = _buffers.CharacterReadSink.RosterRevision;
            var decision = PlayerLoadoutRosterPlanner.Plan(_loadouts, _units, loadoutKey,
                _buffers.CharacterReadSink.Owned.Count == 0, out _);
            if (_buffers.RunProgressReadSink.RunId != expectedRun || _buffers.CharacterReadSink.RosterRevision != expectedRevision)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidRequest);
            return Publish(decision);
        }

        public RosterAcquireResult Acquire(EntityKey key, int initialLevel = 1) => AcquireCore(key, initialLevel, false);

        public RosterAcquireResult AcquireSub(EntityKey key, int initialLevel = 1) => AcquireCore(key, initialLevel, true);

        public TraitGrowthProgressSnapshot ReadTraitGrowthProgress() => ReadTraitGrowthProgressCore(true);

        private TraitGrowthProgressSnapshot ReadTraitGrowthProgressCore(bool retry)
        {
            RequireGrowthReady();
            var run = _buffers.RunProgressReadSink.RunId;
            if (run.IsEmpty || RunEnded || InBattle)
                return new TraitGrowthProgressSnapshot(run, TraitGrowthProgressStatus.Unavailable, null);
            long revision = _buffers.CharacterReadSink.RosterRevision;
            var offer = _buffers.TraitGrowthStateReadSink.ActiveOffer;
            int handled = _buffers.TraitGrowthResultReadSink.HandledCommandNumber;
            int acknowledged = _buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber;
            if (acknowledged < 0 || handled < acknowledged ||
                _buffers.TraitGrowthResultReadSink.LatestResult != null && _buffers.TraitGrowthResultReadSink.LatestResult.RunId != run ||
                offer != null && offer.RunId != run)
                throw new InvalidOperationException("성장 진행의 Run 또는 결과 번호 관계가 올바르지 않습니다.");
            if (handled > acknowledged)
                return new TraitGrowthProgressSnapshot(run, TraitGrowthProgressStatus.AwaitingAcknowledgement, offer);
            var catalog = ReadValidatedGrowthCatalog();
            bool pending;
            if (offer != null)
            {
                if (!_buffers.CharacterReadSink.TryGetOwned(offer.OwnedId, out var target))
                    throw new InvalidOperationException("동결 제안의 대상 보유 기록이 없습니다.");
                var basis = UnitSpecAdapter.ReadUnit(_units.GetUnit(target.CharacterKey));
                var step = TraitGrowthRules.NextStep(target, EffectiveTraitProgression.Resolve(basis, target, catalog));
                if (step == null || step.StepId != offer.StepId || step.Kind != offer.Kind)
                    throw new InvalidOperationException("동결 제안의 대상 단계가 변경되었습니다.");
                pending = true;
            }
            else
                pending = FindNext(catalog, out _, out _);
            if (!DefaultsStillMatch(catalog) || !SameSource(run, revision, offer, handled, acknowledged))
            {
                if (retry)
                    return ReadTraitGrowthProgressCore(false);
                throw new InvalidOperationException("성장 진행 조회 중 원본이 반복해서 변경되었습니다.");
            }
            return new TraitGrowthProgressSnapshot(run, pending ? TraitGrowthProgressStatus.ChoiceRequired : TraitGrowthProgressStatus.ReadyToContinue, offer);
        }

        public TraitGrowthProgressSnapshot PrepareNextTraitGrowthOffer()
        {
            var progress = ReadTraitGrowthProgress();
            if (progress.Status != TraitGrowthProgressStatus.ChoiceRequired || progress.ActiveOffer != null)
                return progress;
            long revision = _buffers.CharacterReadSink.RosterRevision;
            int handled = _buffers.TraitGrowthResultReadSink.HandledCommandNumber;
            int acknowledged = _buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber;
            var catalog = ReadValidatedGrowthCatalog();
            if (!FindNext(catalog, out var target, out var step))
                return ReadTraitGrowthProgress();
            var profile = ReadProfile(target);
            var candidates = TraitGrowthRules.Candidates(target, profile, catalog);
            var keys = TraitGrowthOfferSampler.Sample(candidates, step.Kind, TraitGrowthOfferSampler.Seed(progress.RunId, target.Id, step.StepId));
            var choices = keys.Select(key =>
            {
                var definition = catalog.GetTrait(key);
                return new TraitGrowthChoice(key, step.Kind, step.Kind == TraitGrowthKind.Acquire ? definition.Name : definition.EnhancedName,
                    step.Kind == TraitGrowthKind.Acquire ? definition.Description : definition.EnhancedDescription);
            }).ToArray();
            if (!DefaultsStillMatch(catalog) || !SameSource(progress.RunId, revision, null, handled, acknowledged))
                return ReadTraitGrowthProgress();
            var offer = _buffers.TraitGrowthOperations.PublishOffer(progress.RunId, revision, target.Id, step.StepId, step.Kind, choices);
            return new TraitGrowthProgressSnapshot(progress.RunId, TraitGrowthProgressStatus.ChoiceRequired, offer);
        }

        internal TraitGrowthOperationPreparation PrepareTraitGrowthChoice(TraitGrowthChoiceRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            RequireGrowthReady();
            var run = _buffers.RunProgressReadSink.RunId;
            long revision = _buffers.CharacterReadSink.RosterRevision;
            var offer = _buffers.TraitGrowthStateReadSink.ActiveOffer;
            int handled = _buffers.TraitGrowthResultReadSink.HandledCommandNumber;
            int acknowledged = _buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber;
            TraitGrowthOperationPreparation Failure(TraitGrowthFailureReason reason) => new(_buffers.RunProgressReadSink.RunId,
                _buffers.CharacterReadSink.RosterRevision, _buffers.TraitGrowthStateReadSink.ActiveOffer, TraitGrowthDecision.Failure(reason));
            if (run.IsEmpty || request.RunId != run)
                return Failure(TraitGrowthFailureReason.SourceChanged);
            if (RunEnded)
                return Failure(TraitGrowthFailureReason.RunEnded);
            if (InBattle)
                return Failure(TraitGrowthFailureReason.InBattle);
            if (handled < acknowledged || acknowledged < 0)
                throw new InvalidOperationException("성장 결과 번호 관계가 올바르지 않습니다.");
            if (handled != acknowledged)
                return Failure(TraitGrowthFailureReason.AwaitingAcknowledgement);
            if (!_buffers.CharacterReadSink.TryGetOwned(request.OwnedId, out var record))
                return Failure(TraitGrowthFailureReason.NotOwned);
            if (offer == null || offer.RunId != request.RunId || offer.OfferId != request.OfferId || offer.OwnedId != request.OwnedId || offer.StepId != request.StepId)
                return Failure(TraitGrowthFailureReason.StaleOffer);
            var catalog = ReadValidatedGrowthCatalog();
            var profile = ReadProfile(record);
            var decision = TraitGrowthRules.PlanChoice(record, request, offer, profile, catalog);
            if (!DefaultsStillMatch(catalog))
                return Failure(TraitGrowthFailureReason.SourceChanged);
            if (_buffers.RunProgressReadSink.RunId != run || _buffers.CharacterReadSink.RosterRevision != revision ||
                !ReferenceEquals(_buffers.TraitGrowthStateReadSink.ActiveOffer, offer))
                return Failure(TraitGrowthFailureReason.SourceChanged);
            if (RunEnded)
                return Failure(TraitGrowthFailureReason.RunEnded);
            if (InBattle)
                return Failure(TraitGrowthFailureReason.InBattle);
            if (handled != _buffers.TraitGrowthResultReadSink.HandledCommandNumber || acknowledged != _buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber)
                return Failure(TraitGrowthFailureReason.SourceChanged);
            return new TraitGrowthOperationPreparation(run, revision, offer, decision, record);
        }

        private TraitGrowthCatalog ReadCatalog()
        {
            var tables = _query as IHasTraitGrowthTables;
            return TraitGrowthDefinitionAdapter.Build(tables?.CategoryTraitProgressions, tables?.TraitGrowthDefinitions,
                _query.CategoryConfigTableQuery.Config.DefaultTraits);
        }

        private TraitGrowthCatalog ReadValidatedGrowthCatalog()
        {
            var catalog = ReadCatalog();
            // 다른 보유자의 강화 원본까지 확인한 뒤에만 새 성장 결과를 게시한다.
            foreach (var record in _buffers.CharacterReadSink.Owned)
                if (!record.Growth.IsEmpty)
                    TraitGrowthRules.ValidateHistory(record, ReadProfile(record), catalog);
            return catalog;
        }

        private bool DefaultsStillMatch(TraitGrowthCatalog catalog) =>
            catalog.MatchesDefaultTraits(_query.CategoryConfigTableQuery.Config.DefaultTraits);

        private CombatAttackProfile ReadProfile(OwnedCharacterRecord owned)
        {
            var basis = UnitSpecAdapter.ReadUnit(_units.GetUnit(owned.CharacterKey));
            return BattleDefinitionAdapter.ReadAttackProfiles(_database, new[] { basis })[0];
        }

        private bool FindNext(TraitGrowthCatalog catalog, out OwnedCharacterRecord target, out TraitGrowthStepSpec step)
        {
            foreach (var record in _buffers.CharacterReadSink.Owned.OrderBy(o => o.Id.Value).ToArray())
            {
                var basis = UnitSpecAdapter.ReadUnit(_units.GetUnit(record.CharacterKey));
                step = TraitGrowthRules.NextStep(record, EffectiveTraitProgression.Resolve(basis, record, catalog));
                if (step != null)
                {
                    target = record;
                    return true;
                }
            }
            target = null;
            step = null;
            return false;
        }

        private bool SameSource(RunId run, long revision, TraitGrowthOffer offer, int handled, int acknowledged) =>
            _buffers.RunProgressReadSink.RunId == run && _buffers.CharacterReadSink.RosterRevision == revision && !RunEnded && !InBattle &&
            ReferenceEquals(_buffers.TraitGrowthStateReadSink.ActiveOffer, offer) && handled == _buffers.TraitGrowthResultReadSink.HandledCommandNumber &&
            acknowledged == _buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber;

        public void Shutdown() => _buffers?.TraitGrowthOperations.Clear();

        private RosterAcquireResult AcquireCore(EntityKey key, int initialLevel, bool subOnly)
        {
            if (initialLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(initialLevel));
            RequireReady();
            if (RunEnded)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.RunEnded);
            var expectedRun = _buffers.RunProgressReadSink.RunId;
            long expectedRevision = _buffers.CharacterReadSink.RosterRevision;
            var owned = _buffers.CharacterReadSink.Owned.ToArray();
            var definition = CharacterDefinitionAdapter.Read(_units, key);
            if (definition == null || subOnly && definition.Role != UnitRole.Regular)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidDefinition);
            var definitions = CharacterDefinitionAdapter.ReadAll(_units, owned);
            var decision = CharacterRosterRules.PlanAcquire(definition,
                owned.Select(x => definitions[x.CharacterKey]).ToArray(), initialLevel);
            if (_buffers.RunProgressReadSink.RunId != expectedRun || _buffers.CharacterReadSink.RosterRevision != expectedRevision)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidRequest);
            return Publish(decision);
        }
        public InitialDrawPlan PlanInitialDraw(int seed)
        {
            RequireReady();
            var candidates = new List<StageDrawCandidate>();
            if (!_buffers.RunDeckReadSink.TryGetCurrent(out var deck))
                throw new InvalidOperationException("초기 드로우에는 확정 선택이 필요합니다.");
            foreach (var id in deck.OrderedIds)
            {
                if (!_buffers.CharacterReadSink.TryGetOwned(id, out var record))
                    throw new InvalidOperationException("선택한 보유 기록이 없습니다.");
                if (record.Role == CharacterOwnershipRole.Sub)
                    candidates.Add(new StageDrawCandidate(record.Id, record.CharacterKey));
            }

            // 첫 손패 수는 새 Run 시작 때 로드아웃에서 Run에 기록된 값이다. 새 Run 시작을 거치지 않은 로스터는 기본 규칙(메인 1 + 서브 최대 4)을 쓴다.
            int startingHandSize = _buffers.RunProgressReadSink.StartingHandSize ?? InitialDrawPlan.DefaultStartingHandSize;
            return InitialDrawCalculator.Plan(candidates, seed, startingHandSize);
        }

        private RosterAcquireResult Publish(CharacterAcquisitionDecision decision)
        {
            if (RunEnded)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.RunEnded);
            return decision.Succeeded
                ? _buffers.CharacterWriteSink.Acquire(decision.Requests)
                : RosterAcquireResult.Failure(decision.FailureReason);
        }

        private bool RunEnded => _buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress;
        private bool InBattle => _buffers.CombatStateReadSink.HasBattle || _buffers.CharacterReadSink.Characters.Count != 0;

        private void RequireGrowthReady()
        {
            if (_units == null || _query == null || _buffers == null)
                throw new InvalidOperationException("특성 성장은 유닛·카테고리 설정 초기화와 서비스 바인딩이 필요합니다.");
        }

        private void RequireReady()
        {
            if (_units == null || _loadouts == null || _buffers == null)
                throw new InvalidOperationException("로스터 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICharacterRosterService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
