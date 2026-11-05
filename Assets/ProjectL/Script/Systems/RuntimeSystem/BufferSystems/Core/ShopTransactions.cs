using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>상점의 고정 상품과 결제·획득·구매 표시를 원본 비교 후 게시한다.</summary>
    internal sealed class ShopTransactions : IShopStageBufferWriteSink, IShopPurchaseBufferWriteSink
    {
        private readonly BufferGuards _guards;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly ICombatStateBufferReadSink _combat;
        private readonly IMapProgressBufferReadSink _maps;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly ICharacterBufferReadSink _characters;
        private readonly ICharacterBufferStore _roster;
        private readonly ICurrencyBufferStore _currency;
        private readonly ICurrencyBufferReadSink _currencyRead;
        private readonly ICurrentProgressBufferStore _selection;
        private readonly IItemInventoryStore _items;

        internal ShopTransactions(BufferGuards guards, ICurrentProgressBufferReadSink current,
            ICombatStateBufferReadSink combat, IMapProgressBufferReadSink maps,
            IStageProgressBufferReadSink stages, ICharacterBufferReadSink characters,
            ICharacterBufferStore roster, ICurrencyBufferStore currency,
            ICurrentProgressBufferStore selection, IItemInventoryStore items, ICurrencyBufferReadSink currencyRead)
        {
            _currencyRead = currencyRead ?? throw new ArgumentNullException(nameof(currencyRead));
            _guards = guards ?? throw new ArgumentNullException(nameof(guards));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        public void PrepareShop(ShopEntryPreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            if (preparation.VisitKind == ShopVisitKind.Enhancement)
                throw new InvalidOperationException("서브 특성 강화 상점은 더 이상 게시할 수 없습니다.");
            _guards.EnsureRunInProgress();
            if (_combat.HasBattle) throw new InvalidOperationException("전투 정리 후 상점에 진입해야 합니다.");
            RequireOwned(preparation.ExpectedOwned);
            if (preparation.VisitKind == ShopVisitKind.Training &&
                (_characters.RosterRevision != preparation.ExpectedRosterRevision || _currencyRead.Amount != preparation.ExpectedBalance))
                throw new InvalidOperationException("훈련 진입의 보유·재화 원본이 변경되었습니다.");
            var selected = _selection.PrepareShopSelection(preparation);
            if (!BufferGuards.IsOpenTarget(_maps, _stages, preparation.ExpectedChapterKey,
                preparation.TargetNodeId, preparation.TargetStageKey,
                preparation.ExpectedTargetStageType, preparation.ExpectedTargetMapRole))
                throw new InvalidOperationException("상점 진입 대상이 계산 시점과 다릅니다.");
            _selection.InstallSelection(selected);
        }

        public ShopPurchaseResult PublishTraining(TrainingPurchasePreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();
            var context = preparation.Context;
            var shop = _current.Shop;
            if (_combat.HasBattle || _current.Recovery != null || !_current.HasSelection || shop == null ||
                _current.CurrentMapKey != context.ChapterKey || _current.CurrentStage.NodeId != context.NodeId.Value ||
                _current.CurrentStage.StageKey != context.StageKey.Value || shop.EntryId != context.EntryId ||
                !ReferenceEquals(shop, preparation.ExpectedShop) ||
                !BufferGuards.IsOpenTarget(_maps, _stages, context.ChapterKey, context.NodeId, context.StageKey,
                    StageType.Shop, StageMapRole.Regular))
                return ShopPurchaseResult.Failure(ShopFailureReason.StaleEntry);
            if (shop.VisitKind == ShopVisitKind.Enhancement)
                return ShopPurchaseResult.Failure(ShopFailureReason.SubTraitPurchaseUnavailable);
            if (shop.VisitKind != ShopVisitKind.Training)
                return ShopPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            if (shop.HasPurchase) return ShopPurchaseResult.Failure(ShopFailureReason.PurchaseAlreadyUsed);
            if (_characters.RosterRevision != preparation.ExpectedRosterRevision ||
                shop.TrainingRosterRevision != preparation.ExpectedRosterRevision)
                return ShopPurchaseResult.Failure(ShopFailureReason.StaleRoster);
            if (_currencyRead.Amount != preparation.ExpectedBalance)
                return ShopPurchaseResult.Failure(ShopFailureReason.StaleEntry);
            _characters.TryGetOwned(preparation.OwnedId, out var current);
            var decision = TrainingRules.Plan(shop.TrainingDefinition, current, preparation.TraitKey,
                preparation.ExpectedRank, preparation.ExpectedBalance);
            if (!decision.Succeeded) return ShopPurchaseResult.Failure(decision.FailureReason);
            var trained = _roster.PrepareTraining(current, preparation.TraitKey);
            int balance = _currency.PrepareAmount(preparation.ExpectedBalance, decision.FinalBalance);
            var owned = _characters.Owned.Select(o => o.Id == current.Id ? trained.Final : o).ToArray();
            var purchased = shop.WithTrainingPurchase(decision.Receipt, owned, balance, trained.Revision);
            var result = ShopPurchaseResult.ForTraining(decision.Receipt);
            // 이후에는 준비된 불변 값만 설치한다. 재화 넘침·조회·규칙 검사는 첫 쓰기 전에 끝낸다.
            _roster.InstallGrowth(trained);
            _currency.InstallAmount(balance);
            _selection.InstallPurchase(purchased);
            return result;
        }

        public ShopPurchaseResult PublishPurchase(ShopPurchasePreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();
            var context = preparation.Context;
            _guards.ValidateSelection(context.ChapterKey, context.NodeId, context.StageKey);
            var shop = _current.Shop;
            if (_combat.HasBattle || _current.Recovery != null || shop == null || shop.EntryId != context.EntryId ||
                !BufferGuards.IsOpenTarget(_maps, _stages, context.ChapterKey, context.NodeId, context.StageKey,
                    StageType.Shop, StageMapRole.Regular))
                throw new InvalidOperationException("구매할 상점 방문이 현재와 다릅니다.");
            if (shop.HasPurchase) return ShopPurchaseResult.Failure(ShopFailureReason.PurchaseAlreadyUsed);
            if (shop.VisitKind == ShopVisitKind.Training) return ShopPurchaseResult.Failure(ShopFailureReason.WrongPhase);
            if (!ReferenceEquals(shop, preparation.ExpectedShop))
                throw new InvalidOperationException("구매 상품 원본이 변경되었습니다.");
            RequireOwned(preparation.ExpectedOwned);
            if (!_characters.TryGetMainState(preparation.ExpectedShop.MainId, out var main) || !ReferenceEquals(main, preparation.ExpectedMain))
                throw new InvalidOperationException("구매 계산의 메인 원본이 변경되었습니다.");
            if (!shop.TryGetOffer(preparation.OfferId, out var offer))
                return ShopPurchaseResult.Failure(ShopFailureReason.UnknownOffer);
            if (offer.Kind == ShopProductKind.Enhancement || shop.VisitKind == ShopVisitKind.Enhancement)
                return ShopPurchaseResult.Failure(ShopFailureReason.SubTraitPurchaseUnavailable);
            var decision = preparation.Decision;
            if ((long)preparation.ExpectedBalance - offer.Cost != decision.FinalBalance)
                throw new InvalidOperationException("고정 가격과 결제 제안이 다릅니다.");

            int balance = _currency.PrepareAmount(preparation.ExpectedBalance, decision.FinalBalance);
            var purchased = shop.WithPurchase(preparation.OfferId);
            PreparedRosterAcquisition acquired = null;
            PreparedItemInventoryChange inventory = null;
            ShopPurchaseResult result;
            if (offer.Kind == ShopProductKind.Unit)
            {
                var request = decision.UnitRequest;
                if (request == null || request.CharacterKey != offer.CharacterKey || request.InitialLevel != offer.InitialLevel ||
                    (request.Role == CharacterOwnershipRole.Main ? request.InitialMain == null || request.InitialLevel != 1
                        : request.Role != CharacterOwnershipRole.Sub || request.InitialMain != null))
                    throw new InvalidOperationException("고정 유닛 상품과 획득 제안이 다릅니다.");
                acquired = _roster.PrepareAcquire(new[] { request });
                if (!acquired.Result.Succeeded)
                    return ShopPurchaseResult.Failure(acquired.Result.FailureReason == RosterAcquireFailureReason.IdentityOverflow
                        ? ShopFailureReason.IdentityOverflow : ShopFailureReason.InvalidCharacter);
                result = ShopPurchaseResult.ForUnit(acquired.Result);
            }
            else if (offer.Kind == ShopProductKind.ConsumableItem)
            {
                var change = decision.ItemChange;
                if (change == null || (long)change.Expected.GetCount(offer.ItemKey) + 1 != change.Final.GetCount(offer.ItemKey))
                    throw new InvalidOperationException("아이템 상품은 해당 재고 하나만 지급해야 합니다.");
                foreach (var stock in change.Expected.Counts)
                    if (stock.Key != offer.ItemKey && change.Final.GetCount(stock.Key) != stock.Value)
                        throw new InvalidOperationException("관련 없는 아이템 재고가 변경됩니다.");
                foreach (var stock in change.Final.Counts)
                    if (stock.Key != offer.ItemKey && change.Expected.GetCount(stock.Key) != stock.Value)
                        throw new InvalidOperationException("관련 없는 아이템 재고가 변경됩니다.");
                inventory = _items.Prepare(change);
                result = ShopPurchaseResult.ForItem(offer.ItemKey);
            }

            else throw new InvalidOperationException("지원하지 않는 상품 종류입니다.");

            // 첫 쓰기 이후에는 준비된 값만 설치한다. 조회·계산·결과 구성은 위에서 끝낸다.
            if (acquired != null) _roster.InstallAcquire(acquired);
            if (inventory != null) _items.Install(inventory);
            _currency.InstallAmount(balance);
            _selection.InstallPurchase(purchased);
            return result;
        }

        private void RequireOwned(IReadOnlyList<OwnedCharacterRecord> expected)
        {
            if (expected.Count != _characters.Owned.Count)
                throw new InvalidOperationException("상점 계산의 보유 목록이 변경되었습니다.");
            foreach (var record in expected)
                if (!_characters.TryGetOwned(record.Id, out var current) || !current.SameValues(record))
                    throw new InvalidOperationException("상점 계산의 보유 원본이 변경되었습니다.");
        }
    }
}
