using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>현재 선택한 상점의 고정 상품과 구매 표시다.</summary>
    public sealed class CurrentShopData
    {
        public CharacterInstanceId MainId { get; }
        public IReadOnlyList<ShopPurchaseSnapshot> Offers { get; }
        public int PurchasedMask { get; }
        public bool HasPurchase => PurchasedMask != 0 || TrainingReceipt != null;
        public StageEntryId EntryId { get; }
        public ShopVisitKind VisitKind { get; }

        internal TrainingVisitDefinition TrainingDefinition { get; }
        public long TrainingRosterRevision { get; }
        public IReadOnlyList<TrainingChoiceSnapshot> TrainingChoices { get; }
        public TrainingReceiptSnapshot TrainingReceipt { get; }
        /// <summary>이번 방문에 산 선택지의 구매 직전 값입니다. 구매 뒤 TrainingChoices에서는 등급이 오르거나 빠져 산 단계를 다시 그리는 데 씁니다.</summary>
        public TrainingChoiceSnapshot TrainingPurchase { get; }

        internal CurrentShopData(IReadOnlyList<ShopPurchaseSnapshot> offers, StageEntryId entryId, int purchasedMask = 0, ShopVisitKind visitKind = ShopVisitKind.Ordinary, CharacterInstanceId mainId = default,
            TrainingVisitDefinition training = null, IReadOnlyList<OwnedCharacterRecord> owned = null,
            int balance = 0, long rosterRevision = 0, TrainingReceiptSnapshot trainingReceipt = null,
            TrainingChoiceSnapshot trainingPurchase = null)
        {
            if (entryId.IsEmpty) throw new ArgumentException("상점 방문 식별자가 필요합니다.", nameof(entryId));
            if (!Enum.IsDefined(typeof(ShopVisitKind), visitKind)) throw new ArgumentOutOfRangeException(nameof(visitKind));
            if ((visitKind == ShopVisitKind.Training) != (training != null) ||
                training != null && (offers.Count != 0 || purchasedMask != 0 || owned == null || rosterRevision < 0) ||
                training == null && trainingReceipt != null)
                throw new ArgumentException("훈련 방문에는 훈련 정의와 보유 원본만 필요합니다.");
            TrainingDefinition = training;
            TrainingRosterRevision = rosterRevision;
            TrainingReceipt = trainingReceipt;
            TrainingPurchase = trainingPurchase;
            TrainingChoices = training == null ? Array.Empty<TrainingChoiceSnapshot>() :
                TrainingRules.Choices(training, owned, balance, trainingReceipt != null);
            MainId = mainId;
            VisitKind = visitKind;
            Offers = CopyOffers(offers);
            foreach (var offer in Offers)
                if ((offer.Kind == ShopProductKind.Enhancement) != (visitKind == ShopVisitKind.Enhancement))
                    throw new ArgumentException("방문 종류와 상품 종류가 다릅니다.", nameof(offers));
            if (purchasedMask < 0 || purchasedMask >= 1 << Offers.Count || (purchasedMask & (purchasedMask - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(purchasedMask));
            PurchasedMask = purchasedMask;
            EntryId = entryId;
        }

        internal static IReadOnlyList<ShopPurchaseSnapshot> CopyOffers(IReadOnlyList<ShopPurchaseSnapshot> offers)
        {
            if (offers == null || offers.Count > ShopOfferLimits.Maximum)
                throw new ArgumentException("상점 상품 슬롯 수가 계약과 다릅니다.", nameof(offers));
            var copy = new ShopPurchaseSnapshot[offers.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (offers[i] == null || offers[i].OfferId.Value != i + 1)
                    throw new ArgumentException("상점 상품은 슬롯 번호 순서로 지정해야 합니다.");
                copy[i] = offers[i];
            }

            return Array.AsReadOnly(copy);
        }

        public bool IsPurchased(ShopOfferId id)
        {
            return id.Value >= 1 && id.Value <= Offers.Count &&
                   (PurchasedMask & (1 << (id.Value - 1))) != 0;
        }

        public bool TryGetOffer(ShopOfferId id, out ShopPurchaseSnapshot offer)
        {
            offer = id.Value >= 1 && id.Value <= Offers.Count ? Offers[id.Value - 1] : null;
            return offer != null;
        }

        internal CurrentShopData WithTrainingPurchase(TrainingReceiptSnapshot receipt, IReadOnlyList<OwnedCharacterRecord> owned,
            int balance, long rosterRevision)
        {
            if (VisitKind != ShopVisitKind.Training || HasPurchase || receipt == null)
                throw new InvalidOperationException("훈련 방문의 구매 상태가 맞지 않습니다.");
            return new CurrentShopData(Offers, EntryId, visitKind: VisitKind, mainId: MainId,
                training: TrainingDefinition, owned: owned, balance: balance, rosterRevision: rosterRevision, trainingReceipt: receipt,
                trainingPurchase: TrainingChoices.FirstOrDefault(c => c.OwnedId == receipt.OwnedId && c.TraitKey == receipt.TraitKey)
                    ?? throw new InvalidOperationException("영수증의 선택지가 훈련 방문 목록에 없습니다."));
        }

        internal CurrentShopData WithPurchase(ShopOfferId id)
        {
            if (TryGetOffer(id, out _) == false || HasPurchase)
                throw new InvalidOperationException("구매할 상품 슬롯의 현재 상태가 맞지 않습니다.");
            return new CurrentShopData(Offers, EntryId, PurchasedMask | (1 << (id.Value - 1)), VisitKind, MainId);
        }
    }
}
