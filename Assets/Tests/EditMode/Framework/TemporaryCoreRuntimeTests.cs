using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.Factory.Runtime.Development;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class TemporaryCoreRuntimeTests
    {
        private FrameworkRuntime _runtime;
        private IBufferQuery _buffers;
        private IInGameCoreSystemGroupService _core;

        /// <summary>
        /// 개발 런타임은 운영처럼 Run 재화 0으로 시작한다(Run 시작 재화 DB 미정). 상점·회복 시나리오는 테스트 준비에서 이 값을 넣는다.
        /// 스테이지 골드는 전투 준비 때 스테이지 경제에서 따로 들어온다.
        /// </summary>
        internal const int StartingRunCurrency = 50;

        [SetUp]
        public void SetUp()
        {
            _runtime = Create();
            _buffers = _runtime.GetService(BufferSystemKey.Buffer);
            _buffers.CurrencyWriteSink.SetAmount(StartingRunCurrency);
            _core = _runtime.GetService(SystemGroupsService.InGame).CoreGroup;
        }

        [TearDown]
        public void TearDown() => _runtime?.Shutdown();

        [Test]
        public void Create_ProvidesInitializedCoreWithExplicitTemporaryChapter()
        {
            Assert.DoesNotThrow(() => _runtime.Tick(0));
            Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(50));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(1));
            Assert.That(_buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(_buffers.StageProgressReadSink.GetStages(1).Select(x => x.NodeId), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.Throws<KeyNotFoundException>(() => _runtime.GetService(InGameSystemGroupServiceKey.Core));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TemporaryDefinitions_RunTwoBattlesThroughRecoveryAndChapterClear(bool skip)
        {
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(4));
            // 피해 입력만 기존 API로 주고, 승리·정산·진행은 실제 전투 명령으로 처리한다.
            _core.Combat.ApplyHealth(MainId(), 4);
            WinBattle();
            _buffers.CharacterReadSink.TryGetMainState(out var settled);
            Assert.That(settled.CurrentHealth, Is.EqualTo(4));
            var retained = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => _core.Recovery.EnterRecovery(1, new StageNodeId(3)));
            retained.AssertUnchanged(_buffers);
            _core.Combat.EndBattle();
            Assert.That(_core.Recovery.EnterRecovery(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.Recovery.Options.Count, Is.EqualTo(2));
            if (!skip)
                Assert.That(_core.Recovery.Recover(1, new StageNodeId(3), new RecoveryOptionId(1)), Is.True);
            Assert.That(_core.Recovery.CompleteRecovery(1, new StageNodeId(3)).Accepted, Is.True);
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(4)).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(5));
            Assert.That(_buffers.CurrentProgressReadSink.Recovery, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(_buffers.CharacterReadSink.Characters.Single(x => x.EntityId == MainId()).ToFacts().Health.CurrentHealth,
                Is.EqualTo(skip ? 4 : 7));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(skip ? 50 : 45));
            WinBattle();
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(_buffers.StageProgressReadSink.GetStages(1).Where(x => x.NodeId > 1).All(x => x.IsClear), Is.True);
            _core.Combat.EndBattle();
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void FirstPreparation_RetainsManualDrawRestrictionAndTemporaryEconomy()
        {
            Assert.That(_core.Characters.AcquireSub(EntityKey.Unit_Akimbo).Succeeded, Is.True);
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
            int before = _buffers.CharacterReadSink.Characters.Count(x => x.Area == CharacterRuntimeArea.Hand);
            Assert.That(_buffers.CombatStateReadSink.Current.ManualDrawGoldCost, Is.EqualTo(2));
            _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            Assert.That(_core.Combat.ProcessNextCommand(), Is.True);
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var rejected), Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("FirstPreparation"));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(50));
            Assert.That(_buffers.CharacterReadSink.Characters.Count(x => x.Area == CharacterRuntimeArea.Hand), Is.EqualTo(before));
        }

        [Test]
        public void InvalidEntry_DoesNotPublishOrFallBackToAnotherStage()
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(4)).Accepted, Is.False);
            before.AssertUnchanged(_buffers);
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
        }

        [Test]
        public void TemporarySource_RejectsOtherDatabasesAndUnknownStages()
        {
            var source = new TemporaryBattleInitializationSource(_buffers.CharacterReadSink);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => source.Read(new CharacterRosterDatabase(), 1, new StageKey(101)));
            Assert.Throws<KeyNotFoundException>(() => source.Read(new TemporaryCoreDatabase(), 1, new StageKey(999)));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void SeparateRuntimes_DoNotShareProgressOrCurrency()
        {
            var other = Create();
            try
            {
                other.GetService(BufferSystemKey.Buffer).CurrencyWriteSink.SetAmount(StartingRunCurrency);
                _buffers.CurrencyWriteSink.SetAmount(0);
                Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
                var second = other.GetService(BufferSystemKey.Buffer);
                Assert.That(second.CurrencyReadSink.Amount, Is.EqualTo(50));
                Assert.That(second.CurrentProgressReadSink.HasSelection, Is.False);
                Assert.That(second.CombatStateReadSink.HasBattle, Is.False);
                Assert.That(other.GetService(SystemGroupsService.InGame).CoreGroup.Combat, Is.Not.SameAs(_core.Combat));
            }
            finally { other.Shutdown(); }
        }

        private EntityId MainId() => _buffers.CharacterReadSink.Characters
            .Single(x => x.OwnershipRole == CharacterOwnershipRole.Main).EntityId;

        private void WinBattle()
        {
            Command(CombatViewCommandData.EnterCombat());
            for (int i = 0; i < 8 && _buffers.CombatStateReadSink.Phase == CombatFlowPhase.Automatic; i++)
                Assert.That(_core.Combat.AdvanceCombat(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(MainId()));
            Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, MainId(), _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == MainId()).Anchor.Value, new CellCoord(1, 2)));
            Command(CombatViewCommandData.Attack(MainId(), AttackAim.ForCell(new CellCoord(1, 3))));
            Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
        }

        private void Command(CombatViewCommandData command)
        {
            int number = _buffers.CombatViewWriteSink.Submit(command);
            Assert.That(_core.Combat.ProcessNextCommand(), Is.True);
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
        }

        private static FrameworkRuntime Create() => TemporaryCoreRuntimeFactory.Create();
    }
}
