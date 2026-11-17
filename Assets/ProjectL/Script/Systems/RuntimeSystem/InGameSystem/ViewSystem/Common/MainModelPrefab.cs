using System;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>메인 캐릭터를 말로 표시할 때 필요한 외형 프리팹 조건을 검사합니다.</summary>
    internal static class MainModelPrefab
    {
        /// <summary>보유 목록의 수와 무관하게 확정 Run 덱의 메인 ID로 표시할 캐릭터 키를 읽습니다.</summary>
        internal static EntityKey FindRunMain(IRunDeckReadSink deck, ICharacterBufferReadSink characters)
        {
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (!deck.TryGetCurrent(out RunDeckSnapshot current) || current == null)
                throw new InvalidOperationException("[MainModelPrefab] 현재 Run의 확정 덱이 없습니다.");

            CharacterInstanceId mainId = current.MainId;
            if (!characters.TryGetOwned(mainId, out OwnedCharacterRecord main) || main == null)
                throw new InvalidOperationException($"[MainModelPrefab] Run {current.RunId}의 메인 {mainId}에 해당하는 보유 캐릭터가 없습니다.");
            if (main.Role != CharacterOwnershipRole.Main)
                throw new InvalidOperationException($"[MainModelPrefab] Run {current.RunId}의 메인 {mainId} 역할이 Main이 아닙니다: {main.Role}");
            return main.CharacterKey;
        }

        /// <summary>외형 표에서 메인 모델을 조회합니다. 행 누락은 표의 키를 담은 예외를 그대로 전달합니다.</summary>
        internal static GameObject Require(EntityKey key, Func<EntityKey, UnitVisualData> visuals)
        {
            if (visuals == null) throw new ArgumentNullException(nameof(visuals));
            return Require(key, visuals(key));
        }

        /// <summary>이미 조회한 외형 행을 검사해 중복 조회 없이 모델 프리팹을 반환합니다.</summary>
        internal static GameObject Require(EntityKey key, UnitVisualData visual)
        {
            string owner = $"[MainModelPrefab] 메인 {key}";
            if (visual == null)
                throw new InvalidOperationException($"{owner}: 유닛 외형이 없습니다.");
            GameObject prefab = visual.Prefab;
            ViewPrefabRules.RequirePrefabAsset(prefab, owner);
            ViewPrefabRules.RequireActiveRoot(prefab, owner);
            if (prefab.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException($"{owner}: 모델 프리팹에 Renderer가 필요합니다.");
            return prefab;
        }
    }
}
