using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem
{
    public interface IShopService : IServiceModule
    {
        /// <summary>합법적인 대상 선택과 DB 상품을 함께 게시한다. 화면 전환은 호출자가 담당한다.</summary>
        ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed);
        /// <summary>지정된 현재 노드와 고정 상품을 검증하여 구매한다.</summary>
        ShopPurchaseResult Purchase(ShopVisitContext context, ShopOfferId offerId);
        /// <summary>구매 여부와 관계없이 현재 상점 노드만 완료한다. 구매 반영은 다시 실행하지 않는다.</summary>
        ChapterProgressionResult CompleteShop(ShopVisitContext context);
        ShopPurchaseResult PurchaseTraining(ShopVisitContext context, CharacterInstanceId ownedId, TraitKey traitKey, int expectedRank);
    }

    /// <summary>현재 상점의 사실을 Core 규칙에 전달하고 Buffer 그룹에 구매 반영을 요청한다.</summary>
    internal sealed class ShopService : IShopService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly ChapterProgressionCoordinator _progression;
        private IInGameQuery _query;
        private IUnitTableQuery _units;
        private IStageTableQuery _stages;
        private IConsumableItemTableQuery _items;
        private ICharacterBufferReadSink _characters;
        private ICombatStateBufferReadSink _combat;
        private ICurrencyBufferReadSink _currency;
        private ICurrentProgressBufferReadSink _current;
        private IItemInventoryReadSink _inventory;
        private IMapProgressBufferReadSink _maps;
        private IRunProgressBufferReadSink _run;
        private IRunDeckReadSink _deck;
        private IShopPurchaseBufferWriteSink _purchaseWrite;
        private IShopStageBufferWriteSink _entryWrite;
        private IStageProgressBufferReadSink _stagesRead;

        internal ShopService(ChapterProgressionCoordinator progression)
            => _progression = progression ?? throw new ArgumentNullException(nameof(progression));

        public void InitData(IDataBaseQueryContext data)
        {
            var query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
            _query = query;
            _units = query.UnitTableQuery;
            _stages = query.StageTableQuery;
            _items = (query as IHasConsumableItemTable)?.ConsumableItems;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffers.CharacterReadSink;
            _combat = buffers.CombatStateReadSink;
            _currency = buffers.CurrencyReadSink;
            _current = buffers.CurrentProgressReadSink;
            _inventory = buffers.ItemInventoryReadSink;
            _maps = buffers.MapProgressReadSink;
            _run = buffers.RunProgressReadSink;
            _deck = buffers.RunDeckReadSink;
            _purchaseWrite = buffers.ShopPurchaseWriteSink;
            _entryWrite = buffers.ShopStageWriteSink;
            _stagesRead = buffers.StageProgressReadSink;
        }

        public ChapterProgressionResult EnterShop(int chapterKey, StageNodeId targetNodeId, int seed)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            RequireReady();
            if (RunEnded) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_combat.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 상점에 진입해야 합니다.");

            var current = _current;
            if (!_deck.TryGetCurrent(out var deck))
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RosterNotInitialized);
            int originBalance = _currency.Amount;
            var originOwned = _characters.Owned.ToArray();
            // 진입이 포착하는 메인 ID는 확정 덱에서 나오므로 덱 인스턴스까지 같은 원본 묶음으로 대조한다.
            var originSource = RosterSourceVersion.Capture(_run, _characters, _deck);
            int originChapter = current.CurrentMapKey;
            bool originSelected = current.HasSelection;
            StageSelectionData originStage = current.CurrentStage;
            ChapterProgressionResult decision = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!decision.Accepted) return decision;

            var root = _stages.GetStage(decision.StageKey.Value)
                ?? throw new InvalidOperationException("상점 스테이지 정의가 필요합니다.");
            root.Validate();
            if (!TryGetShopDetail(root, decision.ChapterKey, decision.StageKey, out var shop))
                throw new InvalidOperationException("대상은 지정된 챕터의 일반 상점 스테이지여야 합니다.");
            TrainingVisitDefinition training = null;
            IReadOnlyList<ShopPurchaseSnapshot> offers;
            var visitKind = ShopVisitKind.Ordinary;
            if (shop is EnhancementStageDetail enhancement)
            {
                if (enhancement.TrainingPrices == null)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.EnhancementShopUnavailable);
                var tables = _query as IHasTraitGrowthTables;
                var catalog = TraitGrowthDefinitionAdapter.Build(tables?.CategoryTraitProgressions,
                    tables?.TraitGrowthDefinitions, _query.CategoryConfigTableQuery.Config.DefaultTraits);
                training = new TrainingVisitDefinition(enhancement.TrainingPrices, catalog, originOwned.Select(o => o.CharacterKey)
                    .Distinct().Select(key => UnitSpecAdapter.ReadUnit(_units.GetUnit(key))).ToArray());
                offers = Array.Empty<ShopPurchaseSnapshot>();
                visitKind = ShopVisitKind.Training;
            }
            else
            {
                var ordinary = (ShopStageDetail)shop;
                var candidates = ShopDefinitionAdapter.Build(ordinary, _units, _items);
                IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions = candidates.Any(x => x.Kind == ShopProductKind.Unit)
                    ? CharacterDefinitionAdapter.ReadAll(_units, originOwned, candidates.Where(x => x.Kind == ShopProductKind.Unit).Select(x => x.CharacterKey))
                    : new Dictionary<EntityKey, CharacterDefinitionFacts>();
                offers = ShopOfferPlanner.Plan(candidates, originOwned,
                    new Random(seed), ordinary.ItemCategoryWeight, ordinary.UnitCategoryWeight, definitions);
            }

            // 마지막 DB 조회 이후 원본과 대상이 그대로일 때만 Buffer 게시를 요청한다.
            ChapterProgressionResult revalidated = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            // 사전 검사 자체의 DB 조회 중 진행 상태가 바뀐 경우도 다시 판정한다.
            revalidated = _progression.RevalidateStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            if (current.CurrentMapKey != originChapter || current.HasSelection != originSelected ||
                current.CurrentStage.NodeId != originStage.NodeId || current.CurrentStage.StageKey != originStage.StageKey ||
                current.CurrentStage.StageName != originStage.StageName ||
                revalidated.ChapterKey != decision.ChapterKey || revalidated.NodeId != decision.NodeId ||
                revalidated.StageKey != decision.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);

            if (!originSource.IsCurrent(_run, _characters, _deck) || !ReferenceEquals(originSource.Deck, deck))
                throw new InvalidOperationException("상점 진입 정의를 읽는 동안 보유 원본이 변경되었습니다.");
            if (training != null && _currency.Amount != originBalance)
                throw new InvalidOperationException("훈련 선택지 계산 중 재화 원본이 변경되었습니다.");
            _entryWrite.PrepareShop(new ShopEntryPreparation(originChapter,
                originSelected, originStage, revalidated.NodeId, revalidated.StageKey,
                $"Stage {revalidated.StageKey.Value}", offers, StageType.Shop, root.MapRole, originOwned, visitKind, deck.MainId, training, originSource.RosterRevision, originBalance));
            return revalidated;
        }

        public ShopPurchaseResult PurchaseTraining(ShopVisitContext context, CharacterInstanceId ownedId, TraitKey traitKey, int expectedRank)
        {
            ValidateContext(context);
            RequireReady();
            var failure = ValidatePurchaseStorage(context.ChapterKey, context.NodeId, out var stageKey);
            if (failure != ShopFailureReason.None) return ShopPurchaseResult.Failure(failure);
            var shop = _current.Shop;
            if (shop.EntryId != context.EntryId || stageKey != context.StageKey)
                return ShopPurchaseResult.Failure(ShopFailureReason.StaleEntry);
            if (shop.VisitKind == ShopVisitKind.Enhancement)
                return ShopPurchaseResult.Failure(ShopFailureReason.SubTraitPurchaseUnavailable);
            if (shop.VisitKind != ShopVisitKind.Training)
                return ShopPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            if (shop.HasPurchase) return ShopPurchaseResult.Failure(ShopFailureReason.PurchaseAlreadyUsed);
            var source = RosterSourceVersion.Capture(_run, _characters, _deck);
            int balance = _currency.Amount;
            if (!IsShopDefinition(context.ChapterKey, stageKey))
                return ShopPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            failure = RevalidatePurchase(context.ChapterKey, context.NodeId, stageKey, shop);
            if (failure != ShopFailureReason.None) return ShopPurchaseResult.Failure(failure);
            if (!source.IsCurrent(_run, _characters, _deck) || source.RosterRevision != shop.TrainingRosterRevision)
                return ShopPurchaseResult.Failure(ShopFailureReason.StaleRoster);
            return _purchaseWrite.PublishTraining(new TrainingPurchasePreparation(context, shop, balance,
                source.RosterRevision, ownedId, traitKey, expectedRank));
        }

        public ShopPurchaseResult Purchase(ShopVisitContext context, ShopOfferId offerId)
        {
            ValidateContext(context);
            int chapterKey = context.ChapterKey;
            StageNodeId nodeId = context.NodeId;
            RequireReady();
            ShopFailureReason failure = ValidatePurchaseStorage(chapterKey, nodeId, out StageKey stageKey);
            if (failure != ShopFailureReason.None) return ShopPurchaseResult.Failure(failure);
            var shop = _current.Shop;
            if (shop.EntryId != context.EntryId || stageKey != context.StageKey)
                return ShopPurchaseResult.Failure(ShopFailureReason.StaleEntry);
            if (shop.HasPurchase) return ShopPurchaseResult.Failure(ShopFailureReason.PurchaseAlreadyUsed);
            if (shop.VisitKind == ShopVisitKind.Enhancement)
                return ShopPurchaseResult.Failure(ShopFailureReason.SubTraitPurchaseUnavailable);
            if (shop.VisitKind == ShopVisitKind.Training)
                return ShopPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            var owned = _characters.Owned.ToArray();
            var expectedSource = RosterSourceVersion.Capture(_run, _characters, _deck);
            _characters.TryGetMainState(shop.MainId, out var main);
            int balance = _currency.Amount;
            var inventory = _inventory.Current;
            if (!IsShopDefinition(chapterKey, stageKey))
                return ShopPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            failure = RevalidatePurchase(chapterKey, nodeId, stageKey, shop);
            if (failure != ShopFailureReason.None) return ShopPurchaseResult.Failure(failure);

            shop.TryGetOffer(offerId, out var offer);
            // 유닛 상품이 아니면 소유 한도 판정이 없으므로 정의를 읽지 않고 빈 표를 넘긴다.
            IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions = offer?.Kind == ShopProductKind.Unit
                ? CharacterDefinitionAdapter.ReadAll(_units, owned, new[] { offer.CharacterKey })
                : new Dictionary<EntityKey, CharacterDefinitionFacts>();
            var definition = offer?.Kind == ShopProductKind.Unit
                ? definitions[offer.CharacterKey] : null;
            IReadOnlyList<ItemDefinition> items = Array.Empty<ItemDefinition>();
            if (offer?.Kind == ShopProductKind.ConsumableItem)
            {
                if (_items == null || !_items.TryGet(offer.ItemKey, out var row) || row == null || row.Key != offer.ItemKey)
                    return ShopPurchaseResult.Failure(ShopFailureReason.InvalidItem);
                items = ItemDefinitionAdapter.Build(new[] { row });
            }
            // 마지막 정의 조회가 선택이나 전투를 바꾸었으면 결제와 획득을 게시하지 않는다.
            failure = RevalidatePurchase(chapterKey, nodeId, stageKey, shop);
            if (failure != ShopFailureReason.None) return ShopPurchaseResult.Failure(failure);
            var decision = ShopPurchaseRules.Plan(new ShopPurchaseInput(offer, shop.HasPurchase, balance, owned, definition, main, inventory, items, definitions));
            if (!expectedSource.IsCurrent(_run, _characters, _deck))
                throw new InvalidOperationException("상점 결제 정의를 읽는 동안 보유 원본이 변경되었습니다.");
            return decision.Succeeded
                ? _purchaseWrite.PublishPurchase(new ShopPurchasePreparation(context, shop, balance,
                    owned, main, offerId, decision))
                : ShopPurchaseResult.Failure(decision.FailureReason);
        }

        public ChapterProgressionResult CompleteShop(ShopVisitContext context)
        {
            ValidateContext(context);
            int chapterKey = context.ChapterKey;
            StageNodeId nodeId = context.NodeId;
            RequireReady();
            ChapterProgressionResult target = ValidateCompletionStorage(chapterKey, nodeId);
            if (!target.Accepted) return target;
            var shop = _current.Shop;
            if (shop.EntryId != context.EntryId || target.StageKey != context.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            if (!IsShopDefinition(chapterKey, target.StageKey))
                throw new InvalidOperationException("현재 선택은 해당 챕터의 일반 상점 스테이지여야 합니다.");
            // 정의 조회 후에도 같은 준비 기록과 미완료 노드여야 완료를 게시한다.
            ChapterProgressionResult current = ValidateCompletionStorage(chapterKey, nodeId);
            if (!current.Accepted) return current;
            if (current.StageKey != target.StageKey || !ReferenceEquals(shop, _current.Shop))
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            return _progression.CompleteShopStage(chapterKey, nodeId, target.StageKey);
        }

        private static void ValidateContext(ShopVisitContext context)
        {
            _ = new ShopVisitContext(context.ChapterKey, context.NodeId, context.StageKey, context.EntryId);
        }

        private bool IsShopDefinition(int chapterKey, StageKey stageKey)
        {
            var root = _stages.GetStage(stageKey.Value)
                ?? throw new InvalidOperationException("현재 상점 스테이지의 정의가 필요합니다.");
            root.Validate();
            if (!TryGetShopDetail(root, chapterKey, stageKey, out var detail)) return false;
            if (detail is not EnhancementStageDetail enhancement) return _current.Shop.VisitKind == ShopVisitKind.Ordinary;
            // 방문 때 게시한 가격·정의를 사용한다. 가격을 미설정으로 바꿔도 활성 방문은 유지한다.
            if (_current.Shop.VisitKind == ShopVisitKind.Training) return true;
            return enhancement.TrainingPrices == null && _current.Shop.VisitKind == ShopVisitKind.Enhancement;
        }

        /// <summary>상점 노드의 지도 역할이다. 상점 진입·준비·구매·완료가 모두 이 값 하나로 판정한다.</summary>
        private const StageMapRole ShopMapRole = StageMapRole.Regular;

        /// <summary>DB 정의가 지정된 챕터·스테이지의 상점인지 판정하고, 맞으면 상점 상세를 돌려준다.</summary>
        private static bool TryGetShopDetail(StageRootData root, int chapterKey, StageKey stageKey, out IStageData shop)
        {
            shop = root.StageKey == stageKey.Value && root.MapKey == chapterKey && root.MapRole == ShopMapRole
                && (root.StageData is ShopStageDetail || root.StageData is EnhancementStageDetail) ? root.StageData : null;
            return shop != null;
        }

        /// <summary>진행 기록의 노드가 상점 노드인지 판정한다. 정의 판정(TryGetShopDetail)과 같은 역할 값을 쓴다.</summary>
        private static bool IsShopNode(StageProgressData node)
            => node.StageType == StageType.Shop && node.MapRole == ShopMapRole;

        private ShopFailureReason RevalidatePurchase(int chapterKey, StageNodeId nodeId, StageKey expectedKey, CurrentShopData expectedShop)
        {
            var failure = ValidatePurchaseStorage(chapterKey, nodeId, out StageKey key);
            if (failure != ShopFailureReason.None) return failure;
            if (key != expectedKey) return ShopFailureReason.EntryMismatch;
            return ReferenceEquals(expectedShop, _current.Shop)
                ? ShopFailureReason.None : ShopFailureReason.StaleEntry;
        }

        private ShopFailureReason ValidatePurchaseStorage(int chapterKey, StageNodeId nodeId, out StageKey stageKey)
        {
            stageKey = StageKey.Empty;
            if (RunEnded) return ShopFailureReason.RunEnded;
            if (_combat.HasBattle) return ShopFailureReason.WrongPhase;
            var current = _current;
            if (current.CurrentMapKey != chapterKey) return ShopFailureReason.EntryMismatch;
            if (!current.HasSelection) return ShopFailureReason.WrongPhase;
            if (current.CurrentStage.NodeId != nodeId.Value) return ShopFailureReason.EntryMismatch;
            stageKey = new StageKey(current.CurrentStage.StageKey);
            if (_maps.GetMap(chapterKey).IsClear) return ShopFailureReason.StaleEntry;
            foreach (var node in _stagesRead.GetStages(chapterKey))
            {
                if (node.NodeId != nodeId.Value) continue;
                if (node.StageKey != stageKey.Value) return ShopFailureReason.EntryMismatch;
                if (node.IsClear) return ShopFailureReason.StaleEntry;
                if (current.Shop == null || current.Recovery != null || !IsShopNode(node))
                    return ShopFailureReason.WrongPhase;
                return ShopFailureReason.None;
            }
            return ShopFailureReason.EntryMismatch;
        }

        private ChapterProgressionResult ValidateCompletionStorage(int chapterKey, StageNodeId nodeId)
        {
            if (RunEnded) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_combat.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 상점을 완료해야 합니다.");
            var current = _current;
            if (current.CurrentMapKey != chapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!current.HasSelection)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            if (current.CurrentStage.NodeId != nodeId.Value)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            if (_maps.GetMap(chapterKey).IsClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterCompleted);
            foreach (var node in _stagesRead.GetStages(chapterKey))
            {
                if (node.NodeId != nodeId.Value) continue;
                if (node.StageKey != current.CurrentStage.StageKey)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
                if (node.IsClear)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageAlreadyCleared);
                if (current.Shop == null || current.Recovery != null || !IsShopNode(node))
                    throw new InvalidOperationException("준비된 일반 상점 노드가 필요합니다.");
                return ChapterProgressionResult.EnterStage(chapterKey, nodeId, new StageKey(node.StageKey));
            }
            return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
        }

        private bool RunEnded => _run.Outcome != RunOutcome.InProgress;

        private void RequireReady()
        {
            if (_units == null || _stages == null || _current == null)
                throw new InvalidOperationException("상점 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IShopService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
