using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>View가 읽는 실행 계층의 활성화 사실입니다. Core 전투 상태를 복제하지 않습니다.</summary>
    public interface ICombatEntryRead
    {
        bool IsActive { get; }
    }

    /// <summary>전투 대상의 진입 요청과 실행 전 취소만 제공합니다. 결과는 반환된 요청에서 읽습니다.</summary>
    public interface ICombatEntryService : IServiceModule, ICombatEntryRead
    {
        CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId);
        bool CancelEntry(CombatEntryRequest request);
    }

    public enum CombatEntryStatus
    {
        Created,
        Queued,
        Running,
        Prepared,
        Rejected,
        Cancelled,
        Activating,
        Active,
        Closed,
        Faulted
    }

    /// <summary>
    /// 요청 당시의 대상을 실행까지 유지하고 한 호출의 결과를 보관합니다.
    /// 외부는 읽기만 하며, Job과 LifeCycle만 자신의 실행 단계를 변경합니다.
    /// </summary>
    public sealed class CombatEntryRequest
    {
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public CombatEntryStatus Status { get; private set; }
        public ChapterProgressionResult Result { get; private set; }
        public bool IsJobCompleted { get; private set; }

        internal CombatEntryRequest(int chapterKey, StageNodeId nodeId)
        {
            if (chapterKey < 1)
                throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty)
                throw new ArgumentException("진입할 노드가 필요합니다.", nameof(nodeId));
            ChapterKey = chapterKey;
            NodeId = nodeId;
        }

        internal void MarkQueued()
        {
            Require(CombatEntryStatus.Created);
            Status = CombatEntryStatus.Queued;
        }

        internal bool TryCancel()
        {
            if (Status != CombatEntryStatus.Created && Status != CombatEntryStatus.Queued)
                return false;
            Status = CombatEntryStatus.Cancelled;
            return true;
        }

        internal bool TryStart()
        {
            if (Status == CombatEntryStatus.Cancelled)
                return false;
            Require(CombatEntryStatus.Queued);
            Status = CombatEntryStatus.Running;
            return true;
        }

        internal void PublishResult(ChapterProgressionResult result)
        {
            Require(CombatEntryStatus.Running);
            if (result == null || (result.Accepted &&
                (!result.SelectStage || result.ChapterKey != ChapterKey || result.NodeId != NodeId || result.StageKey.IsEmpty)))
                throw new InvalidOperationException("전투 진입 결과가 이번 요청과 대응하지 않습니다.");
            Result = result;
            Status = result.Accepted ? CombatEntryStatus.Prepared : CombatEntryStatus.Rejected;
        }

        internal void CompleteJob()
        {
            if (Status != CombatEntryStatus.Prepared && Status != CombatEntryStatus.Rejected && Status != CombatEntryStatus.Cancelled)
                throw new InvalidOperationException("전투 진입 결과 없이 Job을 완료할 수 없습니다.");
            IsJobCompleted = true;
        }

        internal void BeginActivation()
        {
            Require(CombatEntryStatus.Prepared);
            if (!IsJobCompleted)
                throw new InvalidOperationException("진입 Job 완료 전에 활성화할 수 없습니다.");
            Status = CombatEntryStatus.Activating;
        }

        internal void Activate()
        {
            Require(CombatEntryStatus.Activating);
            Status = CombatEntryStatus.Active;
        }

        internal void Close() => Status = CombatEntryStatus.Closed;
        internal void Fail() => Status = CombatEntryStatus.Faulted;

        private void Require(CombatEntryStatus expected)
        {
            if (Status != expected)
                throw new InvalidOperationException($"전투 진입 단계가 다릅니다. 기대: {expected}, 실제: {Status}");
        }
    }
}
