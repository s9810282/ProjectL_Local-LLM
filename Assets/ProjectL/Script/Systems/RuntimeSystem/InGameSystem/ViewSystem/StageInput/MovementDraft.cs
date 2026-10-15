using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    internal enum DraftEditResult { Unchanged, Added, Removed, Rejected }
    internal sealed class MovementDraft
    {
        private readonly List<CellCoord> _cells = new List<CellCoord>();
        private MovementPath _snapshot;
        internal EntityId Actor { get; private set; }
        internal long Revision { get; private set; }
        internal bool PointerValid { get; private set; }
        internal bool IsActive => _cells.Count > 0;
        internal int StepCount => Math.Max(0, _cells.Count - 1);
        internal bool IsSuspended { get; private set; }
        internal CellCoord Endpoint => _cells[_cells.Count - 1];

        /// <summary>경로 한 걸음입니다. 상하좌우 1칸만 허용합니다(Core 경로 규칙과 같습니다).</summary>
        internal static bool IsStep(CellCoord from, CellCoord to) =>
            Math.Abs((long)to.X - from.X) + Math.Abs((long)to.Y - from.Y) == 1;

        /// <summary>경로 지정을 시작할 수 있는 칸입니다: 행위자 칸 또는 상하좌우로 붙은 칸.</summary>
        internal static bool CanStartAt(CellCoord origin, CellCoord cell) => cell == origin || IsStep(origin, cell);

        internal void Begin(EntityId actor, CellCoord origin)
        {
            if (actor.IsEmpty) throw new ArgumentException("이동 주체가 필요합니다.", nameof(actor));
            _cells.Clear(); _cells.Add(origin); Actor = actor;
            Revision++; _snapshot = null; PointerValid = true; IsSuspended = false;
        }

        internal DraftEditResult Accept(CellCoord cell)
        {
            if (!IsActive) return DraftEditResult.Rejected;
            if (cell == Endpoint) { PointerValid = true; IsSuspended = false; return DraftEditResult.Unchanged; }
            // 자기 경로의 앞 칸으로 돌아오면 그 칸까지 자른다(직전 칸이면 한 칸). 경로 밖 칸을 이어 붙이지는 않는다.
            int index = _cells.IndexOf(cell);
            if (index >= 0)
            {
                _cells.RemoveRange(index + 1, _cells.Count - index - 1); Revision++; _snapshot = null;
                PointerValid = true; IsSuspended = false; return DraftEditResult.Removed;
            }
            if (IsSuspended || !IsStep(Endpoint, cell))
            {
                Suspend(); return DraftEditResult.Rejected;
            }
            _cells.Add(cell); Revision++; _snapshot = null; PointerValid = true; return DraftEditResult.Added;
        }

        // 멈춘 뒤에는 자기 경로의 칸(그 칸까지 잘림) 또는, 허용되면 경로의 어느 칸에 붙은 칸에서 다시 잇는다.
        // 붙은 칸이면 그 경로 칸까지 자른 뒤 한 걸음 더한다. 이동 유닛 주변 4칸은 시작 칸에 붙어 있어 언제나 이어진다.
        internal bool CanResumeAt(CellCoord cell, bool allowAdjacent = false) => IsActive &&
            (_cells.Contains(cell) || (allowAdjacent && LatestNeighborIndex(cell) >= 0));

        internal void ResumeAt(CellCoord cell, bool allowAdjacent)
        {
            if (!CanResumeAt(cell, allowAdjacent)) return;
            IsSuspended = false;
            if (!_cells.Contains(cell))
                Accept(_cells[LatestNeighborIndex(cell)]);
            Accept(cell);
        }

        // 붙은 경로 칸이 여럿이면 경로를 가장 많이 남기는 뒤쪽 칸을 고른다.
        private int LatestNeighborIndex(CellCoord cell)
        {
            for (int i = _cells.Count - 1; i >= 0; i--)
            {
                if (IsStep(_cells[i], cell))
                    return i;
            }

            return -1;
        }

        internal void Suspend() { PointerValid = false; IsSuspended = true; }
        internal void MarkPointerInvalid() { PointerValid = false; }

        // Core가 확인한 접두 경로만 남긴다. 포인터의 재진입 전까지 유효 드롭으로 취급하지 않는다.
        internal void RetainPrefix(int validStepCount)
        {
            if (!IsActive || validStepCount < 0 || validStepCount >= _cells.Count)
                throw new ArgumentOutOfRangeException(nameof(validStepCount));
            if (_cells.Count > validStepCount + 1)
            {
                _cells.RemoveRange(validStepCount + 1, _cells.Count - validStepCount - 1); Revision++; _snapshot = null;
            }
            Suspend();
        }

        internal void Cancel()
        {
            _cells.Clear(); Actor = EntityId.Empty; Revision++; _snapshot = null;
            PointerValid = false; IsSuspended = false;
        }

        // Revision을 바꾸는 편집에서만 폐기한다. 포인터 유효성과 응답 context는 캐시에 포함하지 않는다.
        internal MovementPath Snapshot()
        {
            if (!IsActive)
                throw new InvalidOperationException("진행 중인 드래그가 없습니다.");
            return _snapshot ??= new MovementPath(_cells);
        }
    }
}
