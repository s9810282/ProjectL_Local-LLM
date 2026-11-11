using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 입력 소유자 하나가 가진 호버 칸 표시입니다(구조 개선 VR3). 배치와 공격이 각자 하나씩 갖고 자기 표시만 그리고 지웁니다.
    /// 같은 칸·같은 상태면 다시 그리지 않습니다. 호버 종류(색 구분)는 소유자가 만들 때 한 번 정합니다.
    /// </summary>
    internal sealed class CombatHoverTracker
    {
        // 호버 칸 계약만 받습니다(씬 표시 나누기 설계 DS2, 결정 Q-DS2 A).
        private readonly ICombatHoverCellFeedback _feedback;
        private readonly CombatHoverKind _kind;
        private bool _hasHover;
        private CellCoord _cell;
        private CombatHoverState _state;

        internal CombatHoverTracker(ICombatHoverCellFeedback feedback, CombatHoverKind kind)
        {
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _kind = kind;
        }

        internal bool HasHover => _hasHover;

        internal CellCoord Cell => _cell;

        internal void Set(CellCoord cell, CombatHoverState state)
        {
            if (_hasHover && _cell == cell && _state == state)
                return;

            _hasHover = true;
            _cell = cell;
            _state = state;
            _feedback.SetHoverCell(cell, state, _kind);
        }

        internal void SetStateIfHovered(CombatHoverState state)
        {
            if (_hasHover)
                Set(_cell, state);
        }

        /// <summary>자기 호버를 표시하고 있을 때만 지웁니다. 다른 소유자의 호버는 건드리지 않습니다.</summary>
        internal void Clear()
        {
            if (!_hasHover)
                return;

            _hasHover = false;
            _state = CombatHoverState.Neutral;
            _feedback.ClearHoverCell();
        }
    }
}
