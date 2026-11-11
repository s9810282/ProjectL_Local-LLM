using System;
using System.Collections.Generic;
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
            DeckEditorSnapshot basis = draft.Basis;
            if (basis == null)
                return new DeckEditDisplayModel(Array.Empty<DeckEditCardModel>(), new DeckEditCardModel[DeckEditSlotLayout.Count],
                    false, false, draft.CanReevaluate, true);

            IReadOnlyList<CharacterInstanceId> candidates = draft.Candidates;
            var inDeck = new HashSet<CharacterInstanceId>(candidates);
            var owned = new List<DeckEditCardModel>();
            for (int i = 0; i < basis.Owned.Count; i++)
            {
                OwnedCharacterRecord record = basis.Owned[i];
                if (!inDeck.Contains(record.Id)) owned.Add(Card(basis, record, false));
            }

            var deck = new DeckEditCardModel[DeckEditSlotLayout.Count];
            bool fitsLayout = true;
            for (int i = 0; i < candidates.Count; i++)
            {
                // 다시 평가로 사라진 후보도 Draft에서는 유지합니다. Core의 거절 사유는 컨트롤러가 공용 안내로 알립니다.
                if (!TryFindOwned(basis, candidates[i], out OwnedCharacterRecord record)) continue;
                DeckEditCardModel card = Card(basis, record, true);
                int slot = DeckEditSlotLayout.IndexOf(card.Category);
                // Core가 받은 덱이 화면 칸(병과당 한 칸)에 맞지 않으면 멈추지 않고 편집만 막습니다(평가 C-09). 출전은 Core 덱 그대로 됩니다.
                if (slot < 0 || deck[slot] != null)
                {
                    fitsLayout = false;
                    continue;
                }
                deck[slot] = card;
            }

            return new DeckEditDisplayModel(owned, deck, draft.CanEdit && fitsLayout, draft.CanConfirm,
                draft.CanReevaluate, fitsLayout);
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
    }
}
