using System;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 전투 진행 사실의 읽기 자리가 Buffer·View 값을 사본 없이 그대로 묶는지 확인한다(재구성 설계 J13).
    /// Core 상태·결과는 가짜, 입력·캐릭터·현재 진행은 실제 Buffer를 쓴다.
    /// </summary>
    public sealed class CombatProgressReaderTests
    {
        private static readonly EntityId Unit = new(7);

        private FakeState _state;
        private FakeResults _results;
        private CombatViewBuffer _input;
        private IBufferQuery _buffers;
        private FakeView _view;
        private CombatProgressReader _reader;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            _buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _state = new FakeState();
            _results = new FakeResults();
            _input = new CombatViewBuffer();
            _view = new FakeView();
            _reader = new CombatProgressReader();
            _reader.Bind(_state, _results, _input, _buffers.CharacterReadSink, _buffers.CurrentProgressReadSink, _view);
        }

        [Test]
        public void Read_CopiesCoreStateAndLatestResult()
        {
            var completion = Completion();
            var latest = new CombatResult(8, CombatProcessingKind.Advance, 0, true, "");
            _state.HasBattle = true;
            _state.Phase = CombatFlowPhase.Automatic;
            _state.HandledCommandNumber = 3;
            _state.AnsweredPreviewNumber = 5;
            _state.LatestResultSequence = 8;
            _state.Completion = completion;
            _results.Latest = latest;

            CombatProgressFacts facts = _reader.Read();

            Assert.That(facts.HasBattle, Is.True);
            Assert.That(facts.BattleInstanceId, Is.EqualTo(FakeState.Instance));
            Assert.That(facts.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            Assert.That(facts.HandledCommandNumber, Is.EqualTo(3));
            Assert.That(facts.AnsweredPreviewNumber, Is.EqualTo(5));
            Assert.That(facts.LatestResultSequence, Is.EqualTo(8));
            Assert.That(facts.Completion, Is.SameAs(completion));
            Assert.That(facts.HasCompletion, Is.True);
            Assert.That(facts.LatestResult, Is.SameAs(latest));
        }

        [Test]
        public void Read_WithoutResult_HasNullLatestResult()
        {
            Assert.That(_reader.Read().LatestResult, Is.Null);
            Assert.That(_reader.Read().HasCompletion, Is.False);
        }

        [Test]
        public void Read_NextCommandIsFirstAfterHandled_AndZeroWhenAllHandled()
        {
            int first = _input.Submit(CombatViewCommandData.EndUnitTurn());
            int second = _input.Submit(CombatViewCommandData.EndUnitTurn());

            _state.HandledCommandNumber = 0;
            Assert.That(_reader.Read().NextCommandNumber, Is.EqualTo(first));
            _state.HandledCommandNumber = first;
            Assert.That(_reader.Read().NextCommandNumber, Is.EqualTo(second));
            Assert.That(_reader.Read().HasNextCommand, Is.True);
            _state.HandledCommandNumber = second;
            Assert.That(_reader.Read().NextCommandNumber, Is.Zero);
            Assert.That(_reader.Read().HasNextCommand, Is.False);
        }

        [Test]
        public void Read_PreviewNumberFollowsCurrentRequest_AndPresentedSequenceIsP()
        {
            Assert.That(_reader.Read().PreviewNumber, Is.Zero);
            int number = _input.SetPreview(CombatViewPreviewData.MoveRange(Unit));
            Assert.That(_reader.Read().PreviewNumber, Is.EqualTo(number));
            _input.ClearPreview();
            Assert.That(_reader.Read().PreviewNumber, Is.Zero);

            _state.LatestResultSequence = 4;
            Assert.That(_reader.Read().HasUnpresentedResult, Is.True);
            _input.AcknowledgePresented(4);
            Assert.That(_reader.Read().PresentedResultSequence, Is.EqualTo(4));
            Assert.That(_reader.Read().HasUnpresentedResult, Is.False);
        }

        [Test]
        public void Read_CopiesViewState()
        {
            _view.Attached = true;
            _view.Busy = true;
            _view.Suspended = true;
            _view.Display = CombatCompletionDisplay.Confirmed;
            _view.Abandon = true;

            CombatProgressFacts facts = _reader.Read();

            Assert.That(facts.IsViewAttached, Is.True);
            Assert.That(facts.IsPlaybackBusy, Is.True);
            Assert.That(facts.IsPresenting, Is.True, "재생 중이면 확인하지 않은 결과가 없어도 연출 중이다.");
            Assert.That(facts.IsSubmissionSuspended, Is.True);
            Assert.That(facts.CompletionDisplay, Is.EqualTo(CombatCompletionDisplay.Confirmed));
            Assert.That(facts.IsAbandonRequested, Is.True);
        }

        [Test]
        public void Read_ReadsAgainEveryCall()
        {
            _state.HasBattle = false;
            Assert.That(_reader.Read().HasBattle, Is.False);
            _state.HasBattle = true;
            Assert.That(_reader.Read().HasBattle, Is.True);
        }

        [Test]
        public void Read_RequiresBindingAndView()
        {
            Assert.Throws<InvalidOperationException>(() => new CombatProgressReader().Read());
            var noView = new CombatProgressReader();
            noView.Bind(_state, _results, _input, _buffers.CharacterReadSink, _buffers.CurrentProgressReadSink, null);
            Assert.Throws<InvalidOperationException>(() => noView.Read());
        }

        [Test]
        public void Bind_RejectsMissingSinks()
        {
            var reader = new CombatProgressReader();
            Assert.Throws<ArgumentNullException>(() =>
                reader.Bind(null, _results, _input, _buffers.CharacterReadSink, _buffers.CurrentProgressReadSink, _view));
            Assert.Throws<ArgumentNullException>(() =>
                reader.Bind(_state, null, _input, _buffers.CharacterReadSink, _buffers.CurrentProgressReadSink, _view));
            Assert.Throws<ArgumentNullException>(() =>
                reader.Bind(_state, _results, null, _buffers.CharacterReadSink, _buffers.CurrentProgressReadSink, _view));
            Assert.Throws<ArgumentNullException>(() =>
                reader.Bind(_state, _results, _input, null, _buffers.CurrentProgressReadSink, _view));
            Assert.Throws<ArgumentNullException>(() =>
                reader.Bind(_state, _results, _input, _buffers.CharacterReadSink, null, _view));
        }

        [Test]
        public void TryReadSelection_IsFalseWithoutSelection_ThenReadsCurrentStage()
        {
            Assert.That(_reader.TryReadSelection(out _, out _), Is.False);

            _buffers.MapProgressWriteSink.RegisterMap(1);
            _buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 100, 1, 0, Array.Empty<int>(), stageType: StageType.Combat)
            });
            _buffers.CurrentProgressWriteSink.SelectMap(1);
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(1), new StageKey(100), "Test combat");

            Assert.That(_reader.TryReadSelection(out int chapterKey, out var stage), Is.True);
            Assert.That(chapterKey, Is.EqualTo(1));
            Assert.That(stage.NodeId, Is.EqualTo(1));
            Assert.That(stage.StageKey, Is.EqualTo(100));
        }

        [Test]
        public void ActiveCharacterCount_ReadsCharacterBuffer()
        {
            Assert.That(_reader.ActiveCharacterCount, Is.Zero);
        }

        private static BattleCompletionChanges Completion()
        {
            return new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1), false));
        }

        private sealed class FakeState : ICombatStateBufferReadSink
        {
            internal const long Instance = 42;
            public long BattleInstanceId => Instance;
            public bool TryGetLatestPreview(out CombatPreviewResult preview) { preview = null; return false; }
            public bool HasBattle { get; set; }
            public CombatStateData Current => null;
            public CombatFlowPhase Phase { get; set; }
            public SoulFieldSnapshot SoulField => SoulFieldSnapshot.Empty;
            public BattleCompletionChanges Completion { get; set; }
            public int LatestResultSequence { get; set; }
            public int HandledCommandNumber { get; set; }
            public int AnsweredPreviewNumber { get; set; }
        }

        private sealed class FakeResults : ICombatResultReadSink
        {
            internal CombatResult Latest;

            public bool TryGetLatest(out CombatResult result)
            {
                result = Latest;
                return result != null;
            }
        }

        private sealed class FakeView : ICombatProgressView
        {
            internal bool Attached;
            internal bool Busy;
            internal bool Suspended;
            internal CombatCompletionDisplay Display;
            internal bool Abandon;

            public bool IsAttached => Attached;
            public bool IsPlaybackBusy => Busy;
            public bool IsSubmissionSuspended => Suspended;
            public void SuspendSubmissions() => Suspended = true;
            public CombatCompletionDisplay CompletionDisplay => Display;
            public void PresentCompletion(BattleCompletionChanges completion) => Display = CombatCompletionDisplay.Showing;
            public bool IsAbandonRequested => Abandon;
        }
    }
}
