using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed partial class RecoveryStageServiceTests
    {
        [Test]
        public void Recover_PaysAndHealsOnceUsingInstalledOptions()
        {
            PrepareOperations();
            int reads = _db.OptionReads.Count;
            _db.Options[2] = new RecoveryOptionData(2, 99, RecoveryHealMode.Flat, 1);
            _db.OnOptionRead = _ => throw new InvalidOperationException("설치 후 선택지를 다시 읽으면 안 됩니다.");
            Assert.That(ApplyRecovery(), Is.True);
            AssertRecoveryFacts(10, 5, 2, 3, 3);
            var committed = new RecoveryBufferSnapshot(_buffers);
            Assert.That(ApplyRecovery(), Is.False);
            committed.AssertUnchanged(_buffers);
            Assert.That(_db.OptionReads.Count, Is.EqualTo(reads));
        }

        [Test]
        public void Recover_OtherOptionAfterConsumptionRejects()
        {
            PrepareOperations();
            Assert.That(ApplyRecovery(), Is.True);
            var committed = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery(option: 1));
            committed.AssertUnchanged(_buffers);
        }

        [TestCase("dead")]
        [TestCase("missing-main")]
        [TestCase("unknown-option")]
        [TestCase("full-health")]
        [TestCase("flat-zero")]
        [TestCase("insufficient-funds")]
        public void Recover_InvalidApplicationPreservesAllFacts(string fault)
        {
            if (fault == "missing-main")
            {
                _buffers.RunProgressWriteSink.ResetRunProgress();
                Register();
                Assert.That(_core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                _buffers.CurrencyWriteSink.SetAmount(8);
            }
            PrepareOperations(fault == "flat-zero" ? RecoveryHealMode.Flat : RecoveryHealMode.Full);
            if (fault == "dead") SetPersistentHealth(0);
            if (fault == "full-health") SetPersistentHealth(10);
            if (fault == "insufficient-funds") _buffers.CurrencyWriteSink.SetAmount(2);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery(option: fault == "unknown-option" ? 99 : 2));
            before.AssertUnchanged(_buffers);
            Assert.That(_buffers.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
        }

        [Test]
        public void CompleteRecovery_AfterApplyClearsOnlyNode()
        {
            PrepareOperations();
            Assert.That(ApplyRecovery(), Is.True);
            var before = new RecoveryBufferSnapshot(_buffers);
            var nodes = _buffers.StageProgressReadSink.GetStages(1).ToArray();
            AssertCompletedOnlyTarget(Complete(), nodes);
            before.AssertUnchanged(_buffers, progress: false);
            AssertRecoveryFacts(10, 5, 2, 3, 3);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CompleteRecovery_WithoutPurchaseAllowsExit(bool fullHealth)
        {
            PrepareOperations();
            if (fullHealth) SetPersistentHealth(10);
            else _buffers.CurrencyWriteSink.SetAmount(0);
            var before = new RecoveryBufferSnapshot(_buffers);
            var nodes = _buffers.StageProgressReadSink.GetStages(1).ToArray();
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery());
            before.AssertUnchanged(_buffers);
            // 사용자가 승인한 미회복 종료는 효과나 지불 없이 같은 완료 API를 사용한다.
            AssertCompletedOnlyTarget(Complete(), nodes);
            before.AssertUnchanged(_buffers, progress: false);
            AssertRecoveryFacts(fullHealth ? 10 : 7, fullHealth ? 8 : 0, null, 0, 0);
        }

        [Test]
        public void CompleteRecovery_DuplicateRejectsAndRecoveryAfterCompletionRejects()
        {
            PrepareOperations();
            Assert.That(ApplyRecovery(), Is.True);
            Assert.That(Complete().Accepted, Is.True);
            var committed = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Complete().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageAlreadyCleared));
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery());
            committed.AssertUnchanged(_buffers);
        }

        [Test]
        public void CompleteRecovery_PreWriteFailurePreservesAppliedRecoveryAndRetriesOnlyCompletion()
        {
            PrepareOperations();
            Assert.That(ApplyRecovery(), Is.True);
            var receipt = _buffers.CurrentProgressReadSink.Recovery;
            var committed = new RecoveryBufferSnapshot(_buffers);
            _db.OnStageRead = (_, __) => throw new InvalidOperationException("완료 전 정의 조회 실패");
            Assert.Throws<InvalidOperationException>(() => Complete());
            committed.AssertUnchanged(_buffers);
            AssertRecoveryFacts(10, 5, 2, 3, 3);
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
            _db.OnStageRead = null;
            var nodes = _buffers.StageProgressReadSink.GetStages(1).ToArray();
            AssertCompletedOnlyTarget(Complete(), nodes);
            committed.AssertUnchanged(_buffers, progress: false);
            Assert.That(_buffers.CurrentProgressReadSink.Recovery, Is.SameAs(receipt));
            AssertRecoveryFacts(10, 5, 2, 3, 3);
            var completed = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Complete().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageAlreadyCleared));
            completed.AssertUnchanged(_buffers);
        }

        [Test]
        public void CompleteRecovery_AllowsLegalSuccessorAndStaleRequestsDoNotAffectIt()
        {
            PrepareOperations();
            Assert.That(Complete().Accepted, Is.True);
            Assert.That(Enter(1, 3).Accepted, Is.True);
            var successor = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Complete(node: 2).FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageMismatch));
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery(node: 2));
            successor.AssertUnchanged(_buffers);
            Assert.That(Complete(node: 3).Accepted, Is.True);
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
        }

        [TestCase("chapter", ChapterProgressionFailureReason.ChapterMismatch)]
        [TestCase("node", ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("missing-selection", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("missing-node", ChapterProgressionFailureReason.StageNotFound)]
        [TestCase("binding", ChapterProgressionFailureReason.StageMismatch)]
        [TestCase("map-clear", ChapterProgressionFailureReason.ChapterCompleted)]
        [TestCase("node-clear", ChapterProgressionFailureReason.StageAlreadyCleared)]
        public void Operation_InvalidIdentityOrProgressPreservesFacts(string fault, ChapterProgressionFailureReason reason)
        {
            PrepareOperations();
            int chapter = fault == "chapter" ? 2 : 1, node = fault == "node" ? 4 : 2;
            if (fault == "missing-selection") { _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1); }
            if (fault == "missing-node") _buffers.StageProgressWriteSink.Clear();
            if (fault == "binding") ReplaceSelectedNode(stageKey: 21);
            if (fault == "map-clear") _buffers.MapProgressWriteSink.SetClear(1, true);
            if (fault == "node-clear") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery(chapter, node));
            Assert.That(Complete(chapter, node).FailureReason, Is.EqualTo(reason));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("unprepared")]
        [TestCase("combat")]
        [TestCase("shop")]
        [TestCase("boss")]
        [TestCase("root-key")]
        [TestCase("root-chapter")]
        [TestCase("graph-combat")]
        [TestCase("graph-shop")]
        [TestCase("graph-boss")]
        public void Operation_RejectsNonRecoveryOrUnpreparedStageWithoutWrites(string fault)
        {
            PrepareOperations();
            if (fault == "unprepared")
            {
                _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1);
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Unprepared");
            }
            if (fault == "shop") _db.Stages[20] = _db.Stages[100];
            if (fault == "combat") _db.Stages[20] = new StageRootData(20, 1, StageMapRole.Regular, new NonRecoveryOperationDetail());
            if (fault == "boss" || fault == "root-key" || fault == "root-chapter")
                _db.Stages[20] = new StageRootData(fault == "root-key" ? 21 : 20, fault == "root-chapter" ? 2 : 1,
                    fault == "boss" ? StageMapRole.Boss : StageMapRole.Regular, new EventStageDetail(new[] { 1 }));
            if (fault == "graph-combat") ReplaceSelectedNode(type: StageType.Combat);
            if (fault == "graph-shop") ReplaceSelectedNode(type: StageType.Shop);
            if (fault == "graph-boss") ReplaceSelectedNode(role: StageMapRole.Boss);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery());
            Assert.Throws<InvalidOperationException>(() => Complete());
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Operation_RetainedActiveOrTerminalBattleDoesNotCleanup(bool terminal)
        {
            PrepareOperations();
            PrepareRetainedBattle(terminal);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery());
            if (terminal) Assert.That(Complete().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
            else Assert.Throws<InvalidOperationException>(() => Complete());
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CompleteRecovery_BattleInstalledDuringRootLookupRejectsWithoutClearing(bool terminal)
        {
            PrepareOperations();
            RecoveryBufferSnapshot afterExternalChange = null;
            _db.OnStageRead = (_, __) => {
                PrepareRetainedBattle(terminal);
                afterExternalChange = new RecoveryBufferSnapshot(_buffers);
            };
            if (terminal) Assert.That(Complete().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
            else Assert.Throws<InvalidOperationException>(() => Complete());
            Assert.That(afterExternalChange, Is.Not.Null);
            afterExternalChange.AssertUnchanged(_buffers);
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.False);
        }

        private void PrepareRetainedBattle(bool terminal)
        {
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.BattleWriteSink.PrepareBattle(BattlePreparationCalculator.Prepare(
                CharacterBattlePreparationTests.Initialization(),
                _buffers.CharacterReadSink.Owned.Where(x => x.Role == CharacterOwnershipRole.Main).ToArray(), main,
                _buffers.CharacterReadSink.Owned.ToDictionary(x => x.Id, x => 0),
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>()));
            if (terminal)
                _buffers.BattleWriteSink.CompleteBattle(new BattleCompletionCalculator(
                    new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>())).Complete(
                    _buffers.CharacterReadSink.Characters.Single().ToFacts(), main,
                    new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.VoluntaryAbandonment),
                    ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(20))));
        }

        [TestCase(0, 2, 2)]
        [TestCase(1, 0, 2)]
        [TestCase(1, 2, 0)]
        public void Operation_InvalidArgumentsDoNotWrite(int chapter, int node, int option)
        {
            PrepareOperations();
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Catch<ArgumentException>(() => ApplyRecovery(chapter, node, option));
            if (option != 0) Assert.Catch<ArgumentException>(() => Complete(chapter, node));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("none")]
        [TestCase("init-only")]
        [TestCase("bind-only")]
        public void Operation_RequiresBothLifecycleStages(string lifecycle)
        {
            PrepareOperations();
            var service = new RecoveryStageService(_core.Combat, (ChapterProgressionCoordinator)_core.ChapterProgression);
            if (lifecycle == "init-only") service.InitData(_db);
            if (lifecycle == "bind-only")
            {
                var builder = new SystemBuilderHelper(); builder.AddService<IBufferQuery>(_buffers);
                service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => service.Recover(1, new StageNodeId(2), new RecoveryOptionId(2)));
            Assert.Throws<InvalidOperationException>(() => service.CompleteRecovery(1, new StageNodeId(2)));
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Operation_FrameworkLifecycleSupportsApplicationAndCompletion(bool bindFirst)
        {
            _runtime.Shutdown(); CreateRuntime(bindFirst);
            PrepareOperations();
            Assert.That(ApplyRecovery(), Is.True);
            Assert.That(Complete().Accepted, Is.True);
            AssertRecoveryFacts(10, 5, 2, 3, 3);
        }

        [Test]
        public void Recover_UsesCurrentSoulsForPercentRounding()
        {
            PrepareOperations(RecoveryHealMode.PercentOfMax, 10);
            SetPersistentHealth(1);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(main, CharacterBufferTestData.MainState(1, 9))));
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => ApplyRecovery());
            before.AssertUnchanged(_buffers);
            var ownedMain = _buffers.CharacterReadSink.Owned.Single(x => x.Role == CharacterOwnershipRole.Main);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(ownedMain.Id, 0, 1) }));
            Assert.That(ApplyRecovery(), Is.True);
            AssertRecoveryFacts(2, 5, 2, 3, 1);
            Assert.That(_buffers.CharacterReadSink.GetSoulCount(ownedMain.Id), Is.EqualTo(1));
        }

        [TestCase("chapter")]
        [TestCase("node")]
        [TestCase("key")]
        public void Coordinator_RejectsStaleExpectedIdentityWithoutClearing(string stale)
        {
            PrepareOperations();
            var before = new RecoveryBufferSnapshot(_buffers);
            var result = ((ChapterProgressionCoordinator)_core.ChapterProgression).CompleteRecoveryStage(
                stale == "chapter" ? 2 : 1, new StageNodeId(stale == "node" ? 4 : 2), new StageKey(stale == "key" ? 21 : 20));
            Assert.That(result.FailureReason, Is.EqualTo(stale == "chapter"
                ? ChapterProgressionFailureReason.ChapterMismatch : ChapterProgressionFailureReason.StageMismatch));
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Operation_ChangedCurrentDuringRootLookupRejectsStaleTarget(bool apply)
        {
            PrepareOperations();
            RecoveryBufferSnapshot afterExternalChange = null;
            _db.OnStageRead = (_, __) => {
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(4), new StageKey(21), "Changed externally");
                afterExternalChange = new RecoveryBufferSnapshot(_buffers);
            };
            if (apply) Assert.Throws<InvalidOperationException>(() => ApplyRecovery());
            else Assert.That(Complete().FailureReason, Is.EqualTo(ChapterProgressionFailureReason.StageMismatch));
            Assert.That(afterExternalChange, Is.Not.Null);
            afterExternalChange.AssertUnchanged(_buffers);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void EnterRecovery_InvalidGraphMetadataRejectsWithoutPublication(bool boss, bool duringLookup)
        {
            RecoveryBufferSnapshot before = null;
            Action invalidate = () => {
                ReplaceSelectedNode(type: boss ? StageType.Event : StageType.Combat,
                    role: boss ? StageMapRole.Boss : StageMapRole.Regular);
                before = new RecoveryBufferSnapshot(_buffers);
            };
            if (duringLookup) _db.OnOptionRead = key => { if (key == 1) invalidate(); };
            else invalidate();
            Assert.Throws<InvalidOperationException>(() => Enter());
            Assert.That(before, Is.Not.Null);
            before.AssertUnchanged(_buffers);
            _db.OnOptionRead = null;
            ReplaceSelectedNode();
            Assert.That(Enter().Accepted, Is.True);
            Assert.That(ApplyRecovery(), Is.True);
            Assert.That(Complete().Accepted, Is.True);
        }

        private void PrepareOperations(RecoveryHealMode mode = RecoveryHealMode.Full, int value = 0)
        {
            _db.Options[2] = new RecoveryOptionData(2, 3, mode, value);
            Assert.That(Enter().Accepted, Is.True);
        }
        private void SetPersistentHealth(int hp)
        {
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(main, CharacterBufferTestData.MainState(hp))));
        }
        private void ReplaceSelectedNode(int stageKey = 20, StageType type = StageType.Event, StageMapRole role = StageMapRole.Regular)
        {
            var nodes = _buffers.StageProgressReadSink.GetStages(1).Select(n => n.NodeId == 2
                ? new StageProgressData(n.NodeId, stageKey, n.Depth, n.WidthIndex, n.NextNodeIds, n.IsClear, type, role) : n).ToArray();
            _buffers.StageProgressWriteSink.Clear(); _buffers.StageProgressWriteSink.RegisterStages(1, nodes);
        }
        private void AssertRecoveryFacts(int health, int balance, int? option, int paid, int healed)
        {
            Assert.That(_buffers.CharacterReadSink.TryGetMainState(out var main), Is.True);
            Assert.That(main.CurrentHealth, Is.EqualTo(health));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(balance));
            var receipt = _buffers.CurrentProgressReadSink.Recovery;
            Assert.That(receipt.AppliedOptionId?.Value, Is.EqualTo(option));
            Assert.That(receipt.PaidAmount, Is.EqualTo(paid));
            Assert.That(receipt.HealedAmount, Is.EqualTo(healed));
        }
        private void AssertCompletedOnlyTarget(ChapterProgressionResult result, StageProgressData[] previous)
        {
            Assert.That(result.Accepted && result.ClearStage, Is.True);
            Assert.That(result.ClearMap || result.EndAttempt || result.SelectMap || result.SelectStage, Is.False);
            Assert.That(result.ChapterKey, Is.EqualTo(1)); Assert.That(result.NodeId.Value, Is.EqualTo(2));
            Assert.That(result.StageKey.Value, Is.EqualTo(20));
            var after = _buffers.StageProgressReadSink.GetStages(1);
            Assert.That(after.Count, Is.EqualTo(previous.Length));
            for (int i = 0; i < previous.Length; i++)
            {
                var node = previous[i];
                Assert.That(after[i].NodeId, Is.EqualTo(node.NodeId));
                Assert.That(after[i].StageKey, Is.EqualTo(node.StageKey));
                Assert.That(after[i].IsClear, Is.EqualTo(node.NodeId == 2 || node.IsClear));
                Assert.That(after[i].Depth, Is.EqualTo(node.Depth));
                Assert.That(after[i].WidthIndex, Is.EqualTo(node.WidthIndex));
                Assert.That(after[i].StageType, Is.EqualTo(node.StageType));
                Assert.That(after[i].MapRole, Is.EqualTo(node.MapRole));
                Assert.That(after[i].NextNodeIds, Is.EqualTo(node.NextNodeIds));
            }
        }
        private sealed class NonRecoveryOperationDetail : IStageData
        {
            public StageType StageType => StageType.Combat;
            public void Validate() { }
        }
        private bool ApplyRecovery(int chapter = 1, int node = 2, int option = 2)
            => _core.Recovery.Recover(chapter, new StageNodeId(node), new RecoveryOptionId(option));
        private ChapterProgressionResult Complete(int chapter = 1, int node = 2)
            => _core.Recovery.CompleteRecovery(chapter, new StageNodeId(node));
    }
}
