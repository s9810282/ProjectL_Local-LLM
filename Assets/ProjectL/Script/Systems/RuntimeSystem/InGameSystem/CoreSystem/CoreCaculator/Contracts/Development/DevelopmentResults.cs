#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development
{
    public enum DevelopmentFailureReason
    {
        None,
        Unavailable,
        Busy,
        NoActiveRun,
        RunEnded,
        BattleRetained,
        StaleContext,
        InvalidAmount,
        Overflow,
        RosterRejected,
        InvalidTarget,
        AlreadyPassed,
        UnsupportedNode,
        NoSupportedPath
    }

    public sealed class DevelopmentNavigationResult
    {
        public bool Applied => Reason == DevelopmentFailureReason.None;
        public bool Changed => Applied && BypassedNodes.Count > 0;
        public DevelopmentFailureReason Reason { get; }
        public ChapterSelectionFailureReason? NavigationFailureReason { get; }
        public StageNodeId TargetNodeId { get; }
        public StageKey TargetStageKey { get; }
        public IReadOnlyList<StageNodeId> BypassedNodes { get; }
        public DevelopmentContext Current { get; }

        public DevelopmentNavigationResult(DevelopmentFailureReason reason, StageNodeId targetNodeId,
            StageKey targetStageKey, IReadOnlyList<StageNodeId> bypassedNodes, DevelopmentContext current,
            ChapterSelectionFailureReason? navigationFailureReason = null)
        {
            Current = current ?? throw new ArgumentNullException(nameof(current));
            if (bypassedNodes == null) throw new ArgumentNullException(nameof(bypassedNodes));
            var copy = bypassedNodes.ToArray();
            if (reason != DevelopmentFailureReason.None && copy.Length != 0)
                throw new ArgumentException("거절된 명령에는 적용한 통과 노드를 담을 수 없습니다.", nameof(bypassedNodes));
            Reason = reason;
            TargetNodeId = targetNodeId;
            TargetStageKey = targetStageKey;
            BypassedNodes = Array.AsReadOnly(copy);
            NavigationFailureReason = navigationFailureReason;
        }
    }

    public sealed class DevelopmentGoldResult
    {
        public bool Applied => Reason == DevelopmentFailureReason.None;
        public DevelopmentFailureReason Reason { get; }
        public int? Before { get; }
        public int? After { get; }

        private DevelopmentGoldResult(DevelopmentFailureReason reason, int? before, int? after)
        {
            Reason = reason;
            Before = before;
            After = after;
        }

        public static DevelopmentGoldResult Success(int before, int after)
        {
            if (before < 0 || after <= before) throw new ArgumentOutOfRangeException(nameof(after));
            return new DevelopmentGoldResult(DevelopmentFailureReason.None, before, after);
        }

        public static DevelopmentGoldResult Refused(DevelopmentFailureReason reason)
        {
            if (reason == DevelopmentFailureReason.None || !Enum.IsDefined(typeof(DevelopmentFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new DevelopmentGoldResult(reason, null, null);
        }
    }

    public sealed class DevelopmentUnitResult
    {
        public bool Applied => Reason == DevelopmentFailureReason.None;
        public DevelopmentFailureReason Reason { get; }
        public RosterAcquireResult Acquisition { get; }

        private DevelopmentUnitResult(DevelopmentFailureReason reason, RosterAcquireResult acquisition)
        {
            Reason = reason;
            Acquisition = acquisition;
        }

        public static DevelopmentUnitResult FromAcquisition(RosterAcquireResult acquisition)
        {
            if (acquisition == null) throw new ArgumentNullException(nameof(acquisition));
            return new DevelopmentUnitResult(acquisition.Succeeded
                ? DevelopmentFailureReason.None : DevelopmentFailureReason.RosterRejected, acquisition);
        }

        public static DevelopmentUnitResult Refused(DevelopmentFailureReason reason)
        {
            if (reason == DevelopmentFailureReason.None || !Enum.IsDefined(typeof(DevelopmentFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new DevelopmentUnitResult(reason, null);
        }
    }
}
#endif
