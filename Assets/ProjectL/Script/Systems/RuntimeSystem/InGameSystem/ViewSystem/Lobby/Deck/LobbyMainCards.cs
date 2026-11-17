using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>
    /// Core 메인 후보 키를 덱 팝업 카드 내용(이름·아이콘)으로 바꿉니다(로비 메인 선택 설계 LM2).
    /// 이름·아이콘의 원본은 View DB 유닛 외형이고, 후보 자격은 받은 키 목록 그대로입니다.
    /// </summary>
    internal static class LobbyMainCards
    {
        /// <summary>
        /// 후보마다 카드 하나를 같은 순서로 만듭니다. 외형 행이 없으면 표가 원인 키를 담은 예외를 던지고,
        /// 정보 아이콘이나 표시 가능한 모델 프리팹이 없으면 구성 오류로 멈춥니다. 덱 팝업 Factory가 연결마다 한 번 부릅니다.
        /// </summary>
        internal static IReadOnlyList<LobbyDeckCardContent> Build(IReadOnlyList<EntityKey> candidates,
            Func<EntityKey, UnitVisualData> visuals)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (visuals == null) throw new ArgumentNullException(nameof(visuals));
            var cards = new LobbyDeckCardContent[candidates.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                EntityKey key = candidates[i];
                UnitVisualData visual = visuals(key)
                    ?? throw new InvalidOperationException($"[Lobby] 메인 후보 {key}의 유닛 외형이 없습니다.");
                if (visual.InfoIcon == null)
                    throw new InvalidOperationException($"[Lobby] 메인 후보 {key}의 유닛 외형에 정보 아이콘(infoIcon)이 없습니다.");
                MainModelPrefab.Require(key, visual);
                cards[i] = new LobbyDeckCardContent(key, visual.DisplayName, visual.InfoIcon);
            }
            return Array.AsReadOnly(cards);
        }
    }
}
