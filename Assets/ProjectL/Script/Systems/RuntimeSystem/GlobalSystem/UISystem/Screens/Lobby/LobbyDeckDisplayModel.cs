using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>전달받은 목록의 표현 스냅샷입니다. 후보나 덱 확정 규칙을 소유하지 않습니다.</summary>
    public sealed class LobbyDeckDisplayModel
    {
        public LobbyDeckDisplayModel(IReadOnlyList<LobbyDeckCardContent> cards)
        {
            if (cards == null)
                throw new ArgumentNullException(nameof(cards));

            var snapshot = new LobbyDeckCardContent[cards.Count];
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i] = cards[i] ?? throw new ArgumentException("표시 카드에 null 항목을 넣을 수 없습니다.", nameof(cards));
            }
            Cards = Array.AsReadOnly(snapshot);
        }

        public IReadOnlyList<LobbyDeckCardContent> Cards { get; }
    }
}
