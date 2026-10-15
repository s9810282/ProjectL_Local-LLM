using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.StageView
{
    /// <summary>사용자가 선택한 값만 담습니다. 진입 가능 여부는 Core가 판단합니다.</summary>
    public readonly struct StageViewCommandData
    {
        public StageViewCommandKind Kind { get; }
        public StageNodeId NodeId { get; }

        private StageViewCommandData(StageViewCommandKind kind, StageNodeId nodeId)
        {
            Kind = kind;
            NodeId = nodeId;
        }

        public static StageViewCommandData SelectStageNode(StageNodeId nodeId)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentException("선택한 노드 ID가 필요합니다.", nameof(nodeId));

            return new StageViewCommandData(StageViewCommandKind.SelectStageNode, nodeId);
        }

        internal void Validate()
        {
            if (Kind != StageViewCommandKind.SelectStageNode || NodeId.IsEmpty)
                throw new ArgumentException("유효한 스테이지 선택 요청이 필요합니다.");
        }
    }

    /// <summary>Buffer 인스턴스 내에서 재사용하지 않는 요청 번호와 입력 데이터입니다.</summary>
    public readonly struct StageViewCommand
    {
        public int Number { get; }
        public StageViewCommandData Data { get; }

        internal StageViewCommand(int number, StageViewCommandData data)
        {
            if (number <= 0)
                throw new ArgumentOutOfRangeException(nameof(number));

            data.Validate();
            Number = number;
            Data = data;
        }
    }
}
