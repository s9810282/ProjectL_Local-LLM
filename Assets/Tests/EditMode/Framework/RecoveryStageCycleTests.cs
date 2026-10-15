using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed partial class CoreStageCombatCycleTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RecoveryCycle_TransfersPersistentHealthToNextBattle(bool skipRecovery)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 101, 1, 0, new[] { 3 }),
                new StageProgressData(3, 20, 2, 0, new[] { 4 }, false, StageType.Event, StageMapRole.Regular),
                new StageProgressData(4, 102, 3, 0, Array.Empty<int>(), false, StageType.Combat, StageMapRole.Boss) });
            var recovery = _core.Recovery;
            EnterStage(1, 2);
            BeginMainTurn();
            Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, Main().EntityId, _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == Main().EntityId).Anchor.Value, new CellCoord(1, 2)));
            ReadAttackRange();
            var field = _buffers.CombatStateReadSink.SoulField;
            _buffers.BattleWriteSink.Apply(new SoulActionChanges(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(field, new SoulFieldSnapshot(2, new[] {
                    new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 100, 1, "fixture"),
                        new CellCoord(3, 3), 5, 1) })), Array.Empty<long>()));
            Command(CombatViewCommandData.Attack(Main().EntityId, AttackAim.ForCell(new CellCoord(1, 3))));
            var completion = _buffers.CombatStateReadSink.Completion;
            Assert.That(completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            AssertPersistent(10);
            Assert.That(Main().Health.MaxHealth, Is.EqualTo(12));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(_buffers.CombatStateReadSink.SoulField.Entries.Count, Is.EqualTo(1));
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.True);
            _buffers.CombatResultReadSink.TryGetLatest(out var result);
            int sequence = _buffers.CombatStateReadSink.LatestResultSequence;
            int handled = _buffers.CombatStateReadSink.HandledCommandNumber;
            int answered = _buffers.CombatStateReadSink.AnsweredPreviewNumber;
            Assert.That(sequence, Is.GreaterThan(0));
            Assert.That(handled, Is.GreaterThan(0));
            Assert.That(answered, Is.GreaterThan(0));
            // 표시 확인과 남은 입력은 View 소유자의 상태이며 회복 서비스가 지우지 않는다.
            _buffers.CombatViewWriteSink.AcknowledgePresented(sequence);
            _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            var commands = _buffers.CombatViewReadSink.Commands.ToArray();
            var viewPreview = _buffers.CombatViewReadSink.Preview;
            var artifact = _buffers.ArtifactReadSink.Current;
            Action retained = () => {
                _buffers.CombatResultReadSink.TryGetLatest(out var latest);
                Assert.That(latest, Is.SameAs(result));
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(answered));
                Assert.That(_buffers.CombatViewReadSink.Commands, Is.EqualTo(commands));
                Assert.That(_buffers.CombatViewReadSink.Preview, Is.EqualTo(viewPreview));
                Assert.That(_buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
                Assert.That(_buffers.CharacterReadSink.Owned, Is.EqualTo(_owned));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(_mainId), Is.EqualTo(2));
                Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            };
            var blocked = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => recovery.EnterRecovery(1, new StageNodeId(3)));
            blocked.AssertUnchanged(_buffers);
            // 전투 정리는 기존 계약으로 호출자가 수행한다. 회복 진입이 정산을 재실행하면 HP가 달라진다.
            _core.Combat.EndBattle();
            AssertCleaned();
            retained();
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            AssertPersistent(10);
            var beforeEntry = new RecoveryBufferSnapshot(_buffers);
            Assert.That(recovery.EnterRecovery(1, new StageNodeId(3)).Accepted, Is.True);
            beforeEntry.AssertUnchanged(_buffers, selection: false);
            Assert.That(_core.Recovery, Is.SameAs(recovery));
            Assert.That(_buffers.CurrentProgressReadSink.Recovery.Options.Select(x => x.Id.Value), Is.EqualTo(new[] { 2, 1 }));
            if (!skipRecovery)
                Assert.That(recovery.Recover(1, new StageNodeId(3), new RecoveryOptionId(2)), Is.True);
            int health = skipRecovery ? 10 : 12;
            int currency = skipRecovery ? 91 : 88;
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            Assert.That(main.CurrentHealth, Is.EqualTo(health));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(currency));
            var receipt = _buffers.CurrentProgressReadSink.Recovery;
            Assert.That(receipt.AppliedOptionId?.Value, Is.EqualTo(skipRecovery ? (int?)null : 2));
            Assert.That(receipt.HealedAmount, Is.EqualTo(skipRecovery ? 0 : 2));
            Assert.That(receipt.PaidAmount, Is.EqualTo(skipRecovery ? 0 : 3));
            var beforeComplete = new RecoveryBufferSnapshot(_buffers);
            var completed = recovery.CompleteRecovery(1, new StageNodeId(3));
            Assert.That(completed.Accepted && completed.ClearStage, Is.True);
            Assert.That(completed.ClearMap || completed.EndAttempt || completed.SelectMap || completed.SelectStage, Is.False);
            beforeComplete.AssertUnchanged(_buffers, progress: false);
            Assert.That(_buffers.StageProgressReadSink.GetStages(1).Where(x => x.IsClear).Select(x => x.NodeId),
                Is.EqualTo(new[] { 2, 3 }));
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            retained();
            EnterStage(1, 4);
            Assert.That(_source.Reads, Is.EqualTo(new[] { (1, 101), (1, 102) }));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CurrentProgressReadSink.Recovery, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(_buffers.CombatStateReadSink.SoulField.Entries, Is.Empty);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            Assert.That(Main().Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(Main().Health.MaxHealth, Is.EqualTo(12));
            _buffers.CharacterReadSink.TryGetMainState(out var after);
            Assert.That(after, Is.SameAs(main));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(currency));
            retained();
        }
    }
}
