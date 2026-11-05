using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>끌어오기로 실제 진입한 칸입니다. 지형 중단 뒤의 미도달 목적지는 기록하지 않습니다.</summary>
    public sealed class AttackPullSnapshot
    {
        public EntityId TargetId { get; }
        public CellCoord StartAnchor { get; }
        public CellCoord FinalAnchor { get; }
        public IReadOnlyList<CellCoord> Path { get; }
        public IReadOnlyList<AttackPullStepSnapshot> Steps { get; }

        public AttackPullSnapshot(EntityId targetId, CellCoord startAnchor, CellCoord finalAnchor, IReadOnlyList<CellCoord> path)
            : this(targetId, startAnchor, CreateSteps(startAnchor, path))
        {
            if (FinalAnchor != finalAnchor)
                throw new ArgumentException("실제 끌어오기 경로는 마지막 도착 칸을 포함해야 합니다.", nameof(finalAnchor));
        }

        private AttackPullSnapshot(EntityId targetId, CellCoord startAnchor, IReadOnlyList<AttackPullStepSnapshot> steps)
        {
            if (targetId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(targetId));
            var copy = (steps ?? throw new ArgumentNullException(nameof(steps))).ToArray();
            if (copy.Length == 0 || copy.Any(step => step == null))
                throw new ArgumentException("끌어오기 경로에는 실제 걸음이 필요합니다.", nameof(steps));
            var previous = startAnchor;
            foreach (var step in copy)
            {
                if (step.From != previous)
                    throw new ArgumentException("끌어오기 걸음은 시작 칸부터 이어져야 합니다.", nameof(steps));
                previous = step.To;
            }
            TargetId = targetId;
            StartAnchor = startAnchor;
            FinalAnchor = previous;
            Steps = Array.AsReadOnly(copy);
            Path = Array.AsReadOnly(copy.Select(step => step.To).ToArray());
        }

        public static AttackPullSnapshot FromSteps(EntityId targetId, CellCoord startAnchor,
            IReadOnlyList<AttackPullStepSnapshot> steps) => new AttackPullSnapshot(targetId, startAnchor, steps);

        private static AttackPullStepSnapshot[] CreateSteps(CellCoord start, IReadOnlyList<CellCoord> path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            var cause = new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.Pull);
            var steps = new AttackPullStepSnapshot[path.Count];
            for (int i = 0; i < steps.Length; i++)
                steps[i] = new AttackPullStepSnapshot(i == 0 ? start : path[i - 1], path[i], cause);
            return steps;
        }
    }
}
