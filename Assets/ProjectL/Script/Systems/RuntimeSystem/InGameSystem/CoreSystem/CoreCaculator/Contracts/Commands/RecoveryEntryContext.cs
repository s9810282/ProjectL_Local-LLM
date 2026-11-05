using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>회복 진입 식별자와 위치를 담는다. 생성만으로 활성 진입 권한을 부여하지 않는다.</summary>
    public readonly struct RecoveryEntryContext : IEquatable<RecoveryEntryContext>
    {
        public StageEntryId EntryId { get; }
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }

        public RecoveryEntryContext(StageEntryId entryId, int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            if (entryId.IsEmpty)
            {
                throw new ArgumentException("회복 진입 식별자는 비어 있을 수 없습니다.", nameof(entryId));
            }
            if (chapterKey <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chapterKey));
            }
            if (nodeId.IsEmpty)
            {
                throw new ArgumentException("회복 노드 식별자는 비어 있을 수 없습니다.", nameof(nodeId));
            }
            if (stageKey.IsEmpty)
            {
                throw new ArgumentException("회복 스테이지 키는 비어 있을 수 없습니다.", nameof(stageKey));
            }

            EntryId = entryId;
            ChapterKey = chapterKey;
            NodeId = nodeId;
            StageKey = stageKey;
        }

        public bool Equals(RecoveryEntryContext other)
        {
            return EntryId == other.EntryId && ChapterKey == other.ChapterKey &&
                NodeId == other.NodeId && StageKey == other.StageKey;
        }

        public override bool Equals(object obj)
        {
            return obj is RecoveryEntryContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EntryId, ChapterKey, NodeId, StageKey);
        }

        public static bool operator ==(RecoveryEntryContext left, RecoveryEntryContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RecoveryEntryContext left, RecoveryEntryContext right)
        {
            return !left.Equals(right);
        }
    }
}
