using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 한 번 그릴 때 넘기는 Buffer 값 묶음입니다. CombatViewSystem이 읽어 만들고 표시는 값만 받습니다(설계 §4, ISP·DIP).
    /// 호출 중에만 쓰고 보관하지 않습니다.
    /// </summary>
    internal readonly struct CombatDisplayValues
    {
        internal CombatDisplayValues(CombatStateData state, IReadOnlyList<CharacterData> characters,
            IReadOnlyList<CombatTimelineEntrySnapshot> timeline = null, int stageKey = 0)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Timeline = timeline ?? Array.Empty<CombatTimelineEntrySnapshot>();
            StageKey = stageKey;
        }

        /// <summary>현재 선택된 스테이지 키입니다(Current Buffer). 선택이 없으면 0이며 배경 프랍을 두지 않습니다.</summary>
        internal int StageKey { get; }

        internal CombatStateData State { get; }

        internal IReadOnlyList<CharacterData> Characters { get; }

        /// <summary>
        /// 이 전투의 스테이지 골드(드로우 전용)입니다. Run 재화가 아닙니다(2026-10-06 Core 재화 분리).
        /// 전투 상태에서 바로 읽습니다. 따로 받지 않습니다(P7·SSOT).
        /// </summary>
        internal int StageGold => State.StageGold;

        internal IReadOnlyList<CombatTimelineEntrySnapshot> Timeline { get; }

        /// <summary>남은 드로우 수입니다. 저장값에서 바로 계산합니다(설계 §6, P7).</summary>
        internal int RemainingDraws => State.DrawOrder.Count - State.DrawCursor;
    }
}
