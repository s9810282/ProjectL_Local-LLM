using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ShopStageSnapshot
    {
        public RunId RunId { get; }
        public StageEntryId EntryId { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public long RunRevision { get; }
        public PlayerWalletSnapshot Wallet { get; }
        public RunCharacterDeckSnapshot Deck { get; }
        public ReadOnlyMemory<ShopOfferSnapshot> Offers { get; }
        public int PurchasedCount { get; }
        /// <summary>상점 한 곳의 상품 슬롯 수다. 슬롯 번호는 1부터 이 값까지다.</summary>
        public const int OfferCount = 3;
        public bool IsSoldOut => PurchasedCount == OfferCount;
        public bool CanComplete { get; }

        public ShopStageSnapshot(RunId runId, StageEntryId entryId, StageNodeId nodeId, StageKey stageKey,
            long runRevision, PlayerWalletSnapshot wallet, RunCharacterDeckSnapshot deck,
            IReadOnlyList<ShopOfferSnapshot> offers, bool canComplete)
        {
            if (runId.IsEmpty) throw new ArgumentException(nameof(runId));
            if (entryId.IsEmpty) throw new ArgumentException(nameof(entryId));
            if (nodeId.IsEmpty) throw new ArgumentException(nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (runRevision < 0) throw new ArgumentOutOfRangeException(nameof(runRevision));
            if (wallet == null) throw new ArgumentNullException(nameof(wallet));
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (deck.RunId != runId) throw new ArgumentException(nameof(deck));
            if (offers == null) throw new ArgumentNullException(nameof(offers));
            if (offers.Count != OfferCount) throw new ArgumentException("Exactly OfferCount offers are required.", nameof(offers));
            var copy = new ShopOfferSnapshot[OfferCount];
            int purchased = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                var offer = offers[i];
                if (offer == null || offer.Id.Value != i + 1)
                    throw new ArgumentException("Offers must be in slot order 1..3.", nameof(offers));
                copy[i] = offer;
                if (offer.IsPurchased) purchased++;
            }
            RunId = runId;
            EntryId = entryId;
            NodeId = nodeId;
            StageKey = stageKey;
            RunRevision = runRevision;
            Wallet = wallet;
            Deck = deck;
            Offers = copy;
            PurchasedCount = purchased;
            CanComplete = canComplete;
        }
    }
}
