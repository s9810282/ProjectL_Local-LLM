using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Development;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
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
    public sealed class TemporaryCoreShopRuntimeTests
    {
        private FrameworkRuntime _runtime;
        private IBufferQuery _buffers;
        private IInGameCoreSystemGroupService _core;

        [SetUp]
        public void SetUp()
        {
            _runtime = TemporaryCoreRuntimeFactory.CreateWithShop();
            _buffers = _runtime.GetService(BufferSystemKey.Buffer);
            _buffers.CurrencyWriteSink.SetAmount(TemporaryCoreRuntimeTests.StartingRunCurrency);
            _core = _runtime.GetService(SystemGroupsService.InGame).CoreGroup;
        }

        [TearDown]
        public void TearDown() => _runtime?.Shutdown();

        [Test]
        public void CreateWithShop_ProvidesSeparateInitializedShopPath()
        {
            Assert.DoesNotThrow(() => _runtime.Tick(0));
            var nodes = _buffers.StageProgressReadSink.GetStages(1);
            Assert.That(nodes.Select(x => x.NodeId), Is.EqualTo(new[] { 1, 2, TemporaryCoreRuntimeFactory.ShopNodeId, 4 }));
            Assert.That(nodes.Select(x => x.StageKey), Is.EqualTo(new[] { 0, 101, 30, 102 }));
            Assert.That(nodes.Single(x => x.NodeId == 5).StageType, Is.EqualTo(StageType.Shop));
            Assert.That(nodes.Single(x => x.NodeId == 5).MapRole, Is.EqualTo(StageMapRole.Regular));
            Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(50));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(1));
            Assert.That(_buffers.CurrentProgressReadSink.HasSelection, Is.False);
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void BattleShopNextBattle_PreservesOwnedFacts(int purchaseCount)
        {
            var original = _buffers.CharacterReadSink.Owned.ToArray();
            var mainOwned = original.Single(x => x.Role == CharacterOwnershipRole.Main).Id;
            var subOwned = original.First(x => x.Role == CharacterOwnershipRole.Sub).Id;
            // 영혼과 피해만 기존 입력 포트로 준비하고, 승리와 정산은 실제 전투 명령으로 처리한다.
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(mainOwned, 0, 2), new SoulCountChange(subOwned, 0, 1) }));
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(4));
            _core.Combat.ApplyHealth(MainId(), 4);
            WinBattle();
            _buffers.CharacterReadSink.TryGetMainState(out var settled);
            Assert.That(settled.CurrentHealth, Is.EqualTo(4));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 5), Is.False);
            var completion = _buffers.CombatStateReadSink.Completion;
            _buffers.CombatResultReadSink.TryGetLatest(out var lastResult);
            int sequence = _buffers.CombatStateReadSink.LatestResultSequence;
            int handled = _buffers.CombatStateReadSink.HandledCommandNumber;
            int answered = _buffers.CombatStateReadSink.AnsweredPreviewNumber;
            Assert.That(sequence, Is.GreaterThan(0));
            Assert.That(handled, Is.GreaterThan(0));
            Assert.That(answered, Is.GreaterThan(0));
            _buffers.CombatViewWriteSink.AcknowledgePresented(sequence);
            _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
            var pending = _buffers.CombatViewReadSink.Commands.ToArray();
            var viewPreview = _buffers.CombatViewReadSink.Preview;
            var artifact = _buffers.ArtifactReadSink.Current;
            var shopService = _core.Shop;
            Action retained = () => {
                _buffers.CharacterReadSink.TryGetMainState(out var main);
                Assert.That(main, Is.SameAs(settled));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(mainOwned), Is.EqualTo(2));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(subOwned), Is.EqualTo(1));
                Assert.That(_buffers.CharacterReadSink.Owned.Take(4), Is.EqualTo(original));
                _buffers.CombatResultReadSink.TryGetLatest(out var result);
                Assert.That(result, Is.SameAs(lastResult));
                Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(answered));
                Assert.That(_buffers.CombatViewReadSink.Commands, Is.EqualTo(pending));
                Assert.That(_buffers.CombatViewReadSink.Preview, Is.EqualTo(viewPreview));
                Assert.That(_buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(sequence));
                Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
                Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.False);
                Assert.That(_core.Shop, Is.SameAs(shopService));
            };
            var blocked = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => shopService.EnterShop(1, new StageNodeId(5), 42));
            blocked.AssertUnchanged(_buffers);
            _core.Combat.EndBattle();
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(_buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(_buffers.CombatStateReadSink.SoulField.Entries, Is.Empty);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            retained();
            var beforeEntry = new RecoveryBufferSnapshot(_buffers);
            Assert.That(shopService.EnterShop(1, new StageNodeId(5), 42).Accepted, Is.True);
            beforeEntry.AssertUnchanged(_buffers, selection: false);
            var offers = _buffers.CurrentProgressReadSink.Shop.Offers;
            Assert.That(offers.Count, Is.EqualTo(3));
            Assert.That(offers.All(x => x.CharacterKey == EntityKey.Unit_Akimbo && x.Cost == 5), Is.True);
            var acquired = new List<CharacterInstanceId>();
            for (int i = 0; i < purchaseCount; i++)
            {
                var purchase = shopService.Purchase(1, new StageNodeId(5), offers[i].OfferId);
                Assert.That(purchase.Succeeded, Is.True);
                var owned = purchase.Acquisition.Acquired.Single();
                Assert.That(owned.Role, Is.EqualTo(CharacterOwnershipRole.Sub));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(owned.Id), Is.Zero);
                acquired.Add(owned.Id);
                Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 5), Is.False);
                retained();
            }
            Assert.That(acquired.Distinct().Count(), Is.EqualTo(purchaseCount));
            Assert.That(acquired.Intersect(original.Select(x => x.Id)), Is.Empty);
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(50 - 5 * purchaseCount));
            Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(4 + purchaseCount));
            var receipt = _buffers.CurrentProgressReadSink.Shop;
            Assert.That(receipt.PurchasedMask, Is.EqualTo((1 << purchaseCount) - 1));
            var beforeComplete = new RecoveryBufferSnapshot(_buffers);
            var complete = shopService.CompleteShop(1, new StageNodeId(5));
            Assert.That(complete.Accepted && complete.ClearStage, Is.True);
            Assert.That(complete.ClearMap || complete.EndAttempt || complete.SelectMap || complete.SelectStage, Is.False);
            beforeComplete.AssertUnchanged(_buffers, progress: false);
            Assert.That(_buffers.StageProgressReadSink.GetStages(1).Where(x => x.IsClear).Select(x => x.NodeId),
                Is.EqualTo(new[] { 2, 5 }));
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            retained();
            var duplicate = new RecoveryBufferSnapshot(_buffers);
            Assert.That(shopService.CompleteShop(1, new StageNodeId(5)).FailureReason,
                Is.EqualTo(ChapterProgressionFailureReason.StageAlreadyCleared));
            duplicate.AssertUnchanged(_buffers);
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(4)).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CurrentProgressReadSink.Shop, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(5));
            Assert.That(_buffers.CombatStateReadSink.SoulField.Entries, Is.Empty);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            var mainInBattle = _buffers.CharacterReadSink.Characters.Single(x => x.EntityId == MainId()).ToFacts();
            Assert.That(mainInBattle.Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(mainInBattle.Health.MaxHealth, Is.EqualTo(12));
            var draw = _buffers.CombatStateReadSink.Current.DrawOrder;
            Assert.That(draw, Is.EquivalentTo(_buffers.CharacterReadSink.Owned.Where(x => x.Role == CharacterOwnershipRole.Sub).Select(x => x.Id)));
            foreach (var id in acquired)
            {
                Assert.That(draw.Count(x => x == id), Is.EqualTo(1));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(id), Is.Zero);
            }
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(50 - 5 * purchaseCount));
            retained();
            // 보존 검증 뒤 View 소유자가 이전 전투 입력을 비우고 다음 전투 명령을 시작한다.
            _buffers.CombatViewWriteSink.Clear();
            Assert.That(_buffers.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(_buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(sequence));
            WinBattle();
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(_buffers.StageProgressReadSink.GetStages(1).Where(x => x.StageKey != 0).All(x => x.IsClear), Is.True);
            _core.Combat.EndBattle();
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
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
            _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(MainId()));
            Assert.That(_core.Combat.ProcessLatestPreview(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Range.Value.Succeeded, Is.True);
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
    }
}
