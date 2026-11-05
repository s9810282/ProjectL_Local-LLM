using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    public readonly struct ShopVisitContext : IEquatable<ShopVisitContext>
    {
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public StageEntryId EntryId { get; }

        public ShopVisitContext(int chapterKey, StageNodeId nodeId, StageKey stageKey, StageEntryId entryId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty) throw new ArgumentException("상점 노드가 필요합니다.", nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException("상점 스테이지가 필요합니다.", nameof(stageKey));
            if (entryId.IsEmpty) throw new ArgumentException("상점 방문 식별자가 필요합니다.", nameof(entryId));
            ChapterKey = chapterKey;
            NodeId = nodeId;
            StageKey = stageKey;
            EntryId = entryId;
        }

        public bool Equals(ShopVisitContext other) => ChapterKey == other.ChapterKey &&
            NodeId == other.NodeId && StageKey == other.StageKey && EntryId == other.EntryId;
        public override bool Equals(object obj) => obj is ShopVisitContext other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ChapterKey;
                hash = hash * 397 ^ NodeId.GetHashCode();
                hash = hash * 397 ^ StageKey.GetHashCode();
                return hash * 397 ^ EntryId.GetHashCode();
            }
        }
        public static bool operator ==(ShopVisitContext left, ShopVisitContext right) => left.Equals(right);
        public static bool operator !=(ShopVisitContext left, ShopVisitContext right) => !left.Equals(right);
    }
}
