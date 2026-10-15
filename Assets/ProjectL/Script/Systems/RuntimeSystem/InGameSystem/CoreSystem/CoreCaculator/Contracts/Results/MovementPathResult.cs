using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum MovementStopReason
    {
        Completed = 0, Dead, MovementBlocked, ForcedRelocation, BattleEnded, NextStepInvalid, InsufficientResources
    }

    /// <summary>실제 진입한 접두 경로와 최종 정지 칸을 보관한다. 강제 이동 후 정지 칸은 접두 끝과 다를 수 있다.</summary>
    public sealed class MovementPathResult
    {
        public MovementPath RequestedPath { get; }
        public IReadOnlyList<CellCoord> EnteredCells { get; }
        public CellCoord FinalStop { get; }
        public int ActionCostSpent { get; }
        public int DistanceSpent { get; }
        public MovementStopReason StopReason { get; }
        public bool ReturnBarrierOccurred { get; }

        public MovementPathResult(MovementPath requestedPath, IReadOnlyList<CellCoord> enteredCells,
            CellCoord finalStop, int actionCostSpent, int distanceSpent, MovementStopReason stopReason,
            bool returnBarrierOccurred)
        {
            RequestedPath = requestedPath ?? throw new ArgumentNullException(nameof(requestedPath));
            if (enteredCells == null) throw new ArgumentNullException(nameof(enteredCells));
            if (actionCostSpent < 0) throw new ArgumentOutOfRangeException(nameof(actionCostSpent));
            if (distanceSpent < 0 || distanceSpent != enteredCells.Count || distanceSpent > requestedPath.StepCount)
                throw new ArgumentOutOfRangeException(nameof(distanceSpent));
            if (!Enum.IsDefined(typeof(MovementStopReason), stopReason))
                throw new ArgumentOutOfRangeException(nameof(stopReason));
            var copied = enteredCells.ToArray();
            for (int i = 0; i < copied.Length; i++)
                if (copied[i] != requestedPath.Cells[i + 1])
                    throw new ArgumentException("진입 칸은 요청 경로의 접두 구간이어야 합니다.", nameof(enteredCells));
            if (stopReason == MovementStopReason.Completed &&
                (distanceSpent != requestedPath.StepCount || finalStop != requestedPath.Endpoint))
                throw new ArgumentException("완료된 이동은 요청 경로의 끝에 도달해야 합니다.");
            EnteredCells = Array.AsReadOnly(copied);
            FinalStop = finalStop;
            ActionCostSpent = actionCostSpent;
            DistanceSpent = distanceSpent;
            StopReason = stopReason;
            ReturnBarrierOccurred = returnBarrierOccurred;
        }
    }
}
