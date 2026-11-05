using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// "같은 노드 진입"의 비교 원본입니다(맵 명령 번호·챕터·노드). 맵 명령·전투 진입 요청·처리 영수증에서 만들고 <c>==</c> 하나로 비교합니다.
    /// 다른 작업자 형식(<see cref="CombatEntryRequest"/>)에 멤버를 더하지 않고 View 쪽 값 하나에 둡니다(검사 후속 정리 설계 §12.2 B6).
    /// </summary>
    internal readonly struct StageEntryIdentity : IEquatable<StageEntryIdentity>
    {
        private StageEntryIdentity(int number, int chapterKey, StageNodeId nodeId)
        {
            Number = number;
            ChapterKey = chapterKey;
            NodeId = nodeId;
        }

        internal int Number { get; }
        internal int ChapterKey { get; }
        internal StageNodeId NodeId { get; }

        internal static StageEntryIdentity Of(StageViewCommand command) =>
            new(command.Number, command.Data.ChapterKey, command.Data.NodeId);

        internal static StageEntryIdentity Of(CombatEntryRequest request) =>
            new(request.MapCommandNumber, request.ChapterKey, request.NodeId);

        internal static StageEntryIdentity Of(StageEntryCommandResult receipt) =>
            new(receipt.RequestNumber, receipt.ChapterKey, receipt.NodeId);

        public bool Equals(StageEntryIdentity other) =>
            Number == other.Number && ChapterKey == other.ChapterKey && NodeId == other.NodeId;

        public override bool Equals(object obj) => obj is StageEntryIdentity other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Number, ChapterKey, NodeId);

        public static bool operator ==(StageEntryIdentity left, StageEntryIdentity right) => left.Equals(right);

        public static bool operator !=(StageEntryIdentity left, StageEntryIdentity right) => !left.Equals(right);
    }
}
