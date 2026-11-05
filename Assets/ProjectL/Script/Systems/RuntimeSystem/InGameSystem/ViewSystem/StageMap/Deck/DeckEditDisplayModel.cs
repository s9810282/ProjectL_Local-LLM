using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>편집 화면의 고정 병과 순서입니다. Core의 정규 저장 순서와 구분합니다.</summary>
    internal static class DeckEditSlotLayout
    {
        internal const int Count = 4;
        internal static CategoryKey CategoryAt(int index) => index switch
        {
            0 => CategoryKey.Main,
            1 => CategoryKey.Heavy,
            2 => CategoryKey.Rider,
            3 => CategoryKey.Cannon,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal static int IndexOf(CategoryKey category) => category switch
        {
            CategoryKey.Main => 0,
            CategoryKey.Heavy => 1,
            CategoryKey.Rider => 2,
            CategoryKey.Cannon => 3,
            _ => -1
        };
    }

    /// <summary>덱 편집 카드 한 장의 표시 값입니다. 능력치는 같은 읽기(DeckEditorSnapshot)의 값만 씁니다.</summary>
    public sealed class DeckEditCardModel
    {
        public CharacterInstanceId OwnedId { get; }

        /// <summary>정의 키입니다. 보유 목록에서 사라진 후보면 <see cref="EntityKey.None"/>입니다.</summary>
        public EntityKey CharacterKey { get; }
        public int? Level { get; }
        public CategoryKey Category => Stats?.CategoryKey ?? (IsMain ? CategoryKey.Main : CategoryKey.None);

        /// <summary>같은 읽기의 능력치입니다. 없으면 null입니다.</summary>
        public CharacterBaselineStatsSnapshot Stats { get; }

        /// <summary>같은 보유 조회의 특성 설정·가용성 원본입니다. 종류별 설정을 화면마다 따로 전달하지 않습니다.</summary>
        internal OwnedCharacterStatsSnapshot DescriptionSource { get; }

        /// <summary>메인 역할 카드인지입니다(표시 표지만, 메인 선택은 Core가 함).</summary>
        public bool IsMain { get; }
        public bool InDeck { get; }

        internal DeckEditCardModel(CharacterInstanceId ownedId, EntityKey characterKey, int? level,
            CharacterBaselineStatsSnapshot stats, bool isMain, bool inDeck,
            OwnedCharacterStatsSnapshot descriptionSource = null)
        {
            if (descriptionSource != null && (descriptionSource.OwnedId != ownedId ||
                descriptionSource.CharacterKey != characterKey || !ReferenceEquals(descriptionSource.Stats, stats)))
                throw new ArgumentException("카드 능력치와 특성 설명은 같은 보유 조회여야 합니다.", nameof(descriptionSource));
            DescriptionSource = descriptionSource;
            OwnedId = ownedId;
            CharacterKey = characterKey;
            Level = level;
            Stats = stats;
            IsMain = isMain;
            InDeck = inDeck;
        }
    }

    /// <summary>덱 편집 화면 한 번의 표시 값입니다. 화면은 이 값을 그리기만 합니다.</summary>
    public sealed class DeckEditDisplayModel
    {
        /// <summary>현재 초안에 포함되지 않은 보유 카드(읽기 순서)입니다.</summary>
        public IReadOnlyList<DeckEditCardModel> Owned { get; }

        /// <summary>Main·Heavy·Rider·Cannon 순서의 고정 네 칸입니다. 빈 슬롯은 null입니다.</summary>
        public IReadOnlyList<DeckEditCardModel> Deck { get; }
        public string Status { get; }

        /// <summary>덱 후보의 병과별 수입니다(표시용).</summary>
        public string Categories { get; }
        public bool CanEdit { get; }
        public bool CanConfirm { get; }

        internal DeckEditDisplayModel(IReadOnlyList<DeckEditCardModel> owned, IReadOnlyList<DeckEditCardModel> deck,
            string status, string categories, bool canEdit, bool canConfirm)
        {
            Owned = owned ?? throw new ArgumentNullException(nameof(owned));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Status = status ?? string.Empty;
            Categories = categories ?? string.Empty;
            CanEdit = canEdit;
            CanConfirm = canConfirm;
        }
    }
}
