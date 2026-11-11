using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>한 Run의 맵과 노드를 식별하는 완료 연출 키입니다.</summary>
    internal readonly struct StageNodeClearKey : IEquatable<StageNodeClearKey>
    {
        internal StageNodeClearKey(RunId runId, int mapKey, int nodeId)
        {
            if (runId.IsEmpty) throw new ArgumentException("Run ID가 필요합니다.", nameof(runId));
            if (mapKey <= 0) throw new ArgumentOutOfRangeException(nameof(mapKey));
            if (nodeId <= 0) throw new ArgumentOutOfRangeException(nameof(nodeId));
            RunId = runId;
            MapKey = mapKey;
            NodeId = nodeId;
        }

        internal RunId RunId { get; }
        internal int MapKey { get; }
        internal int NodeId { get; }

        public bool Equals(StageNodeClearKey other) =>
            RunId == other.RunId && MapKey == other.MapKey && NodeId == other.NodeId;

        public override bool Equals(object obj) => obj is StageNodeClearKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(RunId, MapKey, NodeId);
        public static bool operator ==(StageNodeClearKey left, StageNodeClearKey right) => left.Equals(right);
        public static bool operator !=(StageNodeClearKey left, StageNodeClearKey right) => !left.Equals(right);
    }
}
