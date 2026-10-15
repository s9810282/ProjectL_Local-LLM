using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem
{
    /// <summary>로딩 단계 안에서 실행되는 우선순위 작업입니다.</summary>
    public sealed class LodingNode
    {
        private readonly Action _action;

        public int Priority { get; }

        public LodingNode(int priority, Action action)
        {
            Priority = priority;
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        internal void Execute()
        {
            _action();
        }
    }

    /// <summary>
    /// LoadingSystem 전용 노드 등록소입니다.
    /// Global JobRunner와 인스턴스를 공유하지 않으며, 등록 순서는 같은 우선순위에서 유지됩니다.
    /// </summary>
    internal sealed class LodingNodeQueue
    {
        private readonly JobQueue<LodingNode> _nodes = new(
            Comparer<LodingNode>.Create(
                (left, right) => left.Priority.CompareTo(right.Priority)));

        internal void Register(LodingNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            _nodes.Enqueue(node);
        }

        internal IReadOnlyList<LodingNode> GetSortedSnapshot()
        {
            return _nodes.GetSortedSnapshot();
        }

        internal void Clear()
        {
            _nodes.Clear();
        }
    }
}
