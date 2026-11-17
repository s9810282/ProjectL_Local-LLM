using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 한 번 그릴 때 넘기는 표시 값 묶음입니다. <c>CombatDisplayValueReader</c>가 조회하고 표시는 값만 받습니다(설계 §4, ISP·DIP).
    /// 호출 중에만 쓰고 보관하지 않습니다.
    /// </summary>
    internal readonly struct CombatDisplayValues
    {
        internal CombatDisplayValues(CombatStateData state, IReadOnlyList<CharacterData> characters,
            int runGold, int stageKey = 0,
            IReadOnlyList<OwnedCharacterRecord> owned = null, ItemInventorySnapshot itemInventory = null,
            IReadOnlyList<OwnedCharacterStatsSnapshot> ownedStats = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            StageKey = stageKey;
            RunGold = runGold;
            Owned = owned ?? Array.Empty<OwnedCharacterRecord>();
            OwnedStats = ownedStats ?? Array.Empty<OwnedCharacterStatsSnapshot>();
            ItemInventory = itemInventory ?? ItemInventorySnapshot.Empty;
        }

        /// <summary>현재 선택된 스테이지 키입니다(Current Buffer). 0이면 배경 프랍이 없고 Object/Terrain 외형 회전은 R0입니다.</summary>
        internal int StageKey { get; }

        internal CombatStateData State { get; }

        internal IReadOnlyList<CharacterData> Characters { get; }

        /// <summary>아군 레벨 표시의 원본인 Character Buffer의 보유 기록입니다.</summary>
        internal IReadOnlyList<OwnedCharacterRecord> Owned { get; }

        /// <summary>보유 특성 등급을 조회한 덱 스냅샷의 능력치입니다.</summary>
        internal IReadOnlyList<OwnedCharacterStatsSnapshot> OwnedStats { get; }

        /// <summary>이번 표시에서 읽은 Run 골드다. 원본은 CurrencyBuffer에 있다.</summary>
        internal int RunGold { get; }


        /// <summary>이번 표시에서 읽은 Run 재고(장착하지 않은 아이템)입니다. 원본은 ItemInventoryBuffer에 있고, 없으면 빈 재고입니다(가방 설계 P1).</summary>
        internal ItemInventorySnapshot ItemInventory { get; }
    }
}
