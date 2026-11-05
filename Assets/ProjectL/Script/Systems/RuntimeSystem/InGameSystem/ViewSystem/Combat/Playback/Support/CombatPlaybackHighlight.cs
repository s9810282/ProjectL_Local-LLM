using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>
    /// 결과 재생 중 재생 강조 층(<c>FieldHighlightLayer.PresentationHighlight</c>)에 둔 칸입니다(Presenter 분리 설계 S4).
    /// 빈 목록을 넘기면 지우기입니다(보스 활성화 설계 D6·D7). 어떤 칸을 둘지는 사건 처리기가 고릅니다.
    /// 강조는 결과 하나 안에서만 보입니다: 새 행동, 포효·소환 끝, 자발적 이동, 최종 사실 표시, 재생 취소가 지웁니다.
    /// </summary>
    internal sealed class CombatPlaybackHighlight
    {
        private readonly Action<IReadOnlyList<CellCoord>> _present;
        private readonly List<CellCoord> _cells = new();
        private bool _shown;

        /// <param name="present">재생 강조 표시입니다. 없으면 아무것도 하지 않습니다.</param>
        internal CombatPlaybackHighlight(Action<IReadOnlyList<CellCoord>> present) => _present = present;

        /// <summary>
        /// 사건 처리기가 고른 칸을 보입니다. 고른 칸이 없으면 표시를 부르지 않습니다(이미 보인 강조는 남음).
        /// 표시 콜백이 없으면 칸을 고르지도 않습니다.
        /// </summary>
        internal void Show(CombatEvent e, CombatEventHandler handler)
        {
            if (_present == null)
                return;
            _cells.Clear();
            handler.CollectHighlight(e, _cells);
            if (_cells.Count == 0)
                return;
            _present(_cells);
            _shown = true;
        }

        /// <summary>보인 재생 강조만 지웁니다. 보인 것이 없으면 표시를 부르지 않습니다.</summary>
        internal void Clear()
        {
            if (!_shown)
                return;
            _shown = false;
            _cells.Clear();
            _present(_cells);
        }

        /// <summary>유닛이 차지한 칸을 더합니다. 차지 칸이 없으면 기준 칸, 그것도 없으면 더하지 않습니다.</summary>
        internal static void AddOccupiedCells(List<CellCoord> cells, CharacterCombatFacts facts)
        {
            if (facts == null)
                return;
            IReadOnlyList<CellCoord> occupied = facts.OccupiedCells;
            if (occupied != null && occupied.Count > 0)
            {
                for (int i = 0; i < occupied.Count; i++)
                    cells.Add(occupied[i]);
            }
            else if (facts.Anchor.HasValue)
            {
                cells.Add(facts.Anchor.Value);
            }
        }
    }
}
