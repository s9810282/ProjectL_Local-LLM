using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;

namespace ProjectL.Tests.Framework
{
    /// <summary>맵 클릭이 같은 노드에서 눌렀다 뗄 때만 진입 요청을 한 번 기록하는지 확인합니다.</summary>
    public sealed class StageMapInteractionTests
    {
        // 가짜 피커는 화면 X 좌표로 노드를 정합니다. 0은 빈 곳입니다.
        private const float Empty = 0f;
        private const float NodeA = 2f;
        private const float NodeB = 3f;

        private StageViewBuffer _buffer;
        private FeedbackSpy _feedback;
        private StageMapInteraction _interaction;

        [SetUp]
        public void SetUp()
        {
            _buffer = new StageViewBuffer();
            _feedback = new FeedbackSpy();
            var picker = new XPicker(new Dictionary<float, int> { [NodeA] = 2, [NodeB] = 3 });
            _interaction = new StageMapInteraction(picker, _buffer, _buffer, _feedback);
        }

        [Test]
        public void PressAndReleaseOnSameNode_RecordsOneEntryRequest()
        {
            Click(NodeA, NodeA);

            Assert.That(_buffer.Commands.Count, Is.EqualTo(1));
            Assert.That(_buffer.Commands[0].Data.Kind, Is.EqualTo(StageViewCommandKind.SelectStageNode));
            Assert.That(_buffer.Commands[0].Data.NodeId.Value, Is.EqualTo(2));
            Assert.That(_feedback.Requests, Is.EqualTo(new[] { (2, _buffer.Commands[0].Number) }));
        }

        [TestCase(NodeA, NodeB)]
        [TestCase(NodeA, Empty)]
        [TestCase(Empty, NodeA)]
        [TestCase(Empty, Empty)]
        public void PressAndReleaseNotOnSameNode_RecordsNothing(float pressX, float releaseX)
        {
            Click(pressX, releaseX);

            Assert.That(_buffer.Commands, Is.Empty);
            Assert.That(_feedback.Requests, Is.Empty);
        }

        [Test]
        public void CancelBetweenPressAndRelease_RecordsNothing()
        {
            _interaction.SelectPressed(NodeA, 0f);
            _interaction.CancelRequested();
            _interaction.SelectReleased(NodeA, 0f);

            Assert.That(_buffer.Commands, Is.Empty);
        }

        [Test]
        public void ForcedCancelBetweenPressAndRelease_RecordsNothing()
        {
            _interaction.SelectPressed(NodeA, 0f);
            _interaction.SelectCanceled();
            _interaction.SelectReleased(NodeA, 0f);

            Assert.That(_buffer.Commands, Is.Empty);
        }

        [Test]
        public void PendingRequest_BlocksNewRequest()
        {
            Click(NodeA, NodeA);
            Click(NodeA, NodeA);
            Click(NodeB, NodeB);

            Assert.That(_buffer.Commands.Count, Is.EqualTo(1), "처리 전 요청이 있으면 새로 기록하지 않습니다.");
            Assert.That(_feedback.Requests.Count, Is.EqualTo(1));
        }

        [Test]
        public void ProcessedRequest_AllowsNextRequest()
        {
            Click(NodeA, NodeA);
            _buffer.RemoveCommand(_buffer.Commands[0].Number);

            Click(NodeB, NodeB);

            Assert.That(_buffer.Commands.Count, Is.EqualTo(1));
            Assert.That(_buffer.Commands[0].Data.NodeId.Value, Is.EqualTo(3));
        }

        [Test]
        public void ConfirmRequested_DoesNotRecord()
        {
            _interaction.PointerMoved(NodeA, 0f);
            _interaction.ConfirmRequested();

            Assert.That(_buffer.Commands, Is.Empty);
        }

        [Test]
        public void PointerMoved_NotifiesOnlyWhenHoveredNodeChanges()
        {
            _interaction.PointerMoved(NodeA, 0f);
            _interaction.PointerMoved(NodeA, 0f);
            _interaction.PointerMoved(NodeB, 0f);
            _interaction.PointerMoved(Empty, 0f);

            Assert.That(_feedback.Hovers, Is.EqualTo(new[] { (0, 2), (2, 3), (3, 0) }));
            Assert.That(_interaction.HoveredNodeId, Is.Zero);
        }

        private void Click(float pressX, float releaseX)
        {
            _interaction.SelectPressed(pressX, 0f);
            _interaction.SelectReleased(releaseX, 0f);
        }

        private sealed class XPicker : IStageNodePicker
        {
            private readonly Dictionary<float, int> _nodes;

            public XPicker(Dictionary<float, int> nodes) => _nodes = nodes;

            public bool TryPickNode(float screenX, float screenY, out int nodeId) =>
                _nodes.TryGetValue(screenX, out nodeId);
        }

        private sealed class FeedbackSpy : IStageMapInteractionFeedback
        {
            public readonly List<(int, int)> Hovers = new();
            public readonly List<(int, int)> Requests = new();

            public void HoverChanged(int previousNodeId, int currentNodeId) => Hovers.Add((previousNodeId, currentNodeId));
            public void EntryRequested(int nodeId, int requestNumber) => Requests.Add((nodeId, requestNumber));
        }
    }
}
