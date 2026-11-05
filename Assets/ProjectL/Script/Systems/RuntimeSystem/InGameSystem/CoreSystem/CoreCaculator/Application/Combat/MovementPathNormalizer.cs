using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>전체 경로를 직교 진입으로 펼친다. 대각선의 첫 후보가 뒤 경로와 충돌하면 두 번째 후보를 확인한다.</summary>
    internal static class MovementPathNormalizer
    {
        internal static MovementFailureReason TryNormalize(CombatCommandInput input, MovementPath draft,
            Owner observer, out MovementPath normalized)
        {
            normalized = null;
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (draft == null) return MovementFailureReason.InvalidPath;
            var actor = input.Characters.SingleOrDefault(c => c.EntityId == input.ActorId);
            if (actor == null) return MovementFailureReason.EntityNotFound;
            if (actor.Anchor != draft.Origin) return MovementFailureReason.PathOriginMismatch;
            if (!input.State.IsInside(draft.Origin)) return MovementFailureReason.OutOfBounds;
            if (draft.StepCount == 0) return MovementFailureReason.SameAnchor;
            long weightedCost = 0;
            for (int i = 1; i < draft.Cells.Count; i++)
            {
                long x = Math.Abs((long)draft.Cells[i].X - draft.Cells[i - 1].X);
                long y = Math.Abs((long)draft.Cells[i].Y - draft.Cells[i - 1].Y);
                if (x + y != 1 && (x != 1 || y != 1)) return MovementFailureReason.InvalidPath;
                weightedCost += x + y;
                var allowance = MovementRouteValidator.StepBudget(actor, weightedCost, input.State.Environment.VisibleTo(observer));
                if (allowance != MovementFailureReason.None) return allowance;
            }
            var visible = input.State.Environment.VisibleTo(observer);
            var cells = new List<CellCoord> { draft.Origin };
            var visited = new HashSet<CellCoord> { draft.Origin };
            MovementFailureReason failure = MovementFailureReason.InvalidPath;
            if (!Expand(1)) return failure;
            normalized = new MovementPath(cells.ToArray());
            return MovementFailureReason.None;

            bool Expand(int index)
            {
                if (index == draft.Cells.Count) return true;
                var from = draft.Cells[index - 1];
                var to = draft.Cells[index];
                long x = Math.Abs((long)to.X - from.X);
                long y = Math.Abs((long)to.Y - from.Y);
                if (x + y == 1) return TryBranch(index, new[] { to });
                if (x != 1 || y != 1) { failure = MovementFailureReason.InvalidPath; return false; }
                return TryBranch(index, new[] { new CellCoord(to.X, from.Y), to }) ||
                    TryBranch(index, new[] { new CellCoord(from.X, to.Y), to });
            }

            bool TryBranch(int index, CellCoord[] branch)
            {
                int before = cells.Count;
                foreach (var cell in branch)
                {
                    if (!input.State.IsInside(cell)) { failure = MovementFailureReason.OutOfBounds; Rollback(before); return false; }
                    if (visited.Contains(cell)) { failure = MovementFailureReason.RepeatedCell; Rollback(before); return false; }
                    if (!MovementRouteValidator.CanTraverse(input, cell, index == draft.Cells.Count - 1 && cell == branch[branch.Length - 1], visible))
                    { failure = MovementFailureReason.DestinationOccupied; Rollback(before); return false; }
                    visited.Add(cell);
                    cells.Add(cell);
                }
                if (Expand(index + 1)) return true;
                Rollback(before);
                return false;
            }

            void Rollback(int count)
            {
                while (cells.Count > count)
                {
                    visited.Remove(cells[cells.Count - 1]);
                    cells.RemoveAt(cells.Count - 1);
                }
            }
        }

        /// <summary>선택된 직교 경로가 요청 초안을 그대로 펼친 것인지 확인한다. 다른 모서리로 재탐색하지 않는다.</summary>
        internal static bool MatchesDraft(MovementPath draft, MovementPath normalized)
        {
            if (draft == null || normalized == null || draft.Origin != normalized.Origin) return false;
            int next = 1;
            for (int i = 1; i < draft.Cells.Count; i++)
            {
                var from = draft.Cells[i - 1];
                var to = draft.Cells[i];
                long x = Math.Abs((long)to.X - from.X), y = Math.Abs((long)to.Y - from.Y);
                if (x == 1 && y == 1)
                {
                    if (next >= normalized.Cells.Count) return false;
                    var middle = normalized.Cells[next++];
                    if (middle != new CellCoord(to.X, from.Y) && middle != new CellCoord(from.X, to.Y)) return false;
                }
                else if (x + y != 1) return false;
                if (next >= normalized.Cells.Count || normalized.Cells[next++] != to) return false;
            }
            return next == normalized.Cells.Count;
        }
    }
}
