using System;
using System.Collections.Generic;
using System.Text;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>초안과 그 기준 스냅샷으로 표시 값을 만듭니다. 덱 규칙을 계산하거나 순서를 바꾸지 않습니다.</summary>
    internal static class DeckEditModelBuilder
    {
        internal static DeckEditDisplayModel Build(DeckEditDraft draft)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));
            string status = DeckEditText.Status(draft);
            DeckEditorSnapshot basis = draft.Basis;
            if (basis == null)
                return new DeckEditDisplayModel(Array.Empty<DeckEditCardModel>(), new DeckEditCardModel[DeckEditSlotLayout.Count],
                    status, string.Empty, false, false);

            IReadOnlyList<CharacterInstanceId> candidates = draft.Candidates;
            var inDeck = new HashSet<CharacterInstanceId>(candidates);
            var owned = new List<DeckEditCardModel>();
            for (int i = 0; i < basis.Owned.Count; i++)
            {
                OwnedCharacterRecord record = basis.Owned[i];
                if (!inDeck.Contains(record.Id)) owned.Add(Card(basis, record, false));
            }

            var deck = new DeckEditCardModel[DeckEditSlotLayout.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                // 다시 평가로 사라진 후보도 Draft에서는 유지하고 Core의 거절 사유는 표시 모델의 Status에 남깁니다.
                if (!TryFindOwned(basis, candidates[i], out OwnedCharacterRecord record)) continue;
                DeckEditCardModel card = Card(basis, record, true);
                int slot = DeckEditSlotLayout.IndexOf(card.Category);
                if (slot < 0 || deck[slot] != null)
                    throw new InvalidOperationException($"[DeckEditModelBuilder] UIId.DeckEdit: {card.Category} 병과의 " +
                        $"보유 유닛 '{card.OwnedId}'을 고정 슬롯에 표시할 수 없습니다. 병과별 편성을 확인하세요.");
                deck[slot] = card;
            }

            return new DeckEditDisplayModel(owned, deck, status, Categories(deck), draft.CanEdit, draft.CanConfirm);
        }

        private static DeckEditCardModel Card(DeckEditorSnapshot basis, OwnedCharacterRecord record, bool inDeck)
        {
            OwnedCharacterStatsSnapshot stats = FindStats(basis, record.Id);
            return new DeckEditCardModel(record.Id, record.CharacterKey, stats?.Level ?? record.Level, stats?.Stats,
                record.Role == CharacterOwnershipRole.Main, inDeck, stats);
        }

        private static bool TryFindOwned(DeckEditorSnapshot basis, CharacterInstanceId id, out OwnedCharacterRecord record)
        {
            foreach (OwnedCharacterRecord owned in basis.Owned)
            {
                if (owned.Id != id) continue;
                record = owned;
                return true;
            }
            record = null;
            return false;
        }

        private static OwnedCharacterStatsSnapshot FindStats(DeckEditorSnapshot basis, CharacterInstanceId id)
        {
            foreach (OwnedCharacterStatsSnapshot stats in basis.Stats)
                if (stats.OwnedId == id) return stats;
            return null;
        }

        private static string Categories(IReadOnlyList<DeckEditCardModel> deck)
        {
            var counts = new SortedDictionary<CategoryKey, int>();
            foreach (DeckEditCardModel card in deck)
            {
                if (card?.Stats == null) continue;
                counts.TryGetValue(card.Stats.CategoryKey, out int count);
                counts[card.Stats.CategoryKey] = count + 1;
            }
            var text = new StringBuilder();
            foreach (KeyValuePair<CategoryKey, int> pair in counts)
            {
                if (text.Length > 0) text.Append(" · ");
                text.Append(pair.Key).Append(' ').Append(pair.Value);
            }
            return text.ToString();
        }
    }
}
