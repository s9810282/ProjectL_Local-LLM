using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Development;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class RunTerminationAdmissionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void DefeatPublication_CommitsRunReceiptWithExistingSettlement(bool command)
        {
            var b = CharacterBattleSettlementTests.Create();
            var completion = Defeat(b);
            if (command) b.BattleWriteSink.Apply(Transition(b, completion));
            else Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.True);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            var receipt = b.RunProgressReadSink.Completion;
            Assert.That(receipt.ChapterKey, Is.EqualTo(1));
            Assert.That(receipt.NodeId.Value, Is.EqualTo(2));
            Assert.That(receipt.StageKey.Value, Is.EqualTo(10));
            Assert.That(b.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.False);
            Assert.That(b.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(7));
            var snapshot = new RecoveryBufferSnapshot(b);
            Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.False);
            if (command) b.BattleWriteSink.Apply(Transition(b, completion));
            snapshot.AssertUnchanged(b);
            Assert.That(b.RunProgressReadSink.Completion, Is.SameAs(receipt));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void RejectedDefeatPublication_LeavesRunInProgress(bool command, bool invalidSelection)
        {
            var b = CharacterBattleSettlementTests.Create();
            var completion = Defeat(b, invalidSelection ? 999 : 2);
            if (!invalidSelection)
                b.CharacterWriteSink.Apply(CharacterSettlementCalculator.SetHealth(
                    b.CharacterReadSink.Characters.Single().ToFacts(), Persistent(b), 6));
            var snapshot = new RecoveryBufferSnapshot(b);
            Assert.Throws<InvalidOperationException>(() => {
                if (command) b.BattleWriteSink.Apply(Transition(b, completion));
                else b.BattleWriteSink.CompleteBattle(completion);
            });
            snapshot.AssertUnchanged(b);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(b.RunProgressReadSink.Completion, Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InvalidDefeatField_PublishesNeitherSettlementNorRunReceipt(bool command)
        {
            var b = CharacterBattleSettlementTests.Create();
            var completion = Defeat(b);
            var badField = new SoulFieldChange(b.CombatStateReadSink.SoulField,
                new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                    new EffectOriginKey(EffectOwnerKind.Artifact, 100, 1, "fixture"), new CellCoord(99, 99), 3, 1) }));
            completion = new BattleCompletionChanges(new SoulActionChanges(completion.Characters,
                badField, Array.Empty<long>()), completion.Outcome, completion.Progression);
            var snapshot = new RecoveryBufferSnapshot(b);
            Assert.Throws<InvalidOperationException>(() => {
                if (command) b.BattleWriteSink.Apply(Transition(b, completion));
                else b.BattleWriteSink.CompleteBattle(completion);
            });
            snapshot.AssertUnchanged(b);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(b.RunProgressReadSink.Completion, Is.Null);
        }

        [TestCase(RunOutcome.Victory)]
        [TestCase(RunOutcome.Defeat)]
        public void TerminalPublication_RejectsPreviouslyPreparedWork(RunOutcome outcome)
        {
            var b = CharacterBattleSettlementTests.Create();
            var completion = Defeat(b);
            var transition = Transition(b, completion);
            var preparation = new BattlePreparation(new[] { b.CharacterReadSink.Characters.Single().ToFacts() },
                CharacterBufferTestData.Combat());
            var selection = new BattleEntrySelection(1, true, b.CurrentProgressReadSink.CurrentStage,
                new StageNodeId(2), new StageKey(10), "Battle");
            var field = new SoulActionChanges(completion.Characters,
                new SoulFieldChange(b.CombatStateReadSink.SoulField, b.CombatStateReadSink.SoulField), Array.Empty<long>());
            var recovery = new RecoveryEntryPreparation(1, true, b.CurrentProgressReadSink.CurrentStage,
                new StageNodeId(2), new StageKey(10), "Recovery", new[] {
                    new RecoveryOptionSpec(new RecoveryOptionId(1), 0, new RecoveryHealSpec(RecoveryHealMode.Flat, 1)) },
                ProjectL.Systems.DataBaseSystem.Data.Common.StageType.Event, ProjectL.Systems.DataBaseSystem.Data.Common.StageMapRole.Regular);
            var shop = new ShopEntryPreparation(1, true, b.CurrentProgressReadSink.CurrentStage,
                new StageNodeId(2), new StageKey(10), "Shop", Offers(),
                ProjectL.Systems.DataBaseSystem.Data.Common.StageType.Shop, ProjectL.Systems.DataBaseSystem.Data.Common.StageMapRole.Regular);
            Seed(b, outcome);
            var snapshot = new RecoveryBufferSnapshot(b);
            ThrowsRunEnded(() => b.BattleWriteSink.PrepareBattle(preparation));
            ThrowsRunEnded(() => b.BattleWriteSink.PrepareBattle(preparation, selection));
            ThrowsRunEnded(() => b.BattleWriteSink.Apply(completion.Characters));
            ThrowsRunEnded(() => b.BattleWriteSink.Apply(field));
            ThrowsRunEnded(() => b.BattleWriteSink.Apply(transition));
            ThrowsRunEnded(() => b.BattleWriteSink.CompleteBattle(completion));
            ThrowsRunEnded(() => b.RecoveryStageWriteSink.PrepareRecovery(recovery));
            ThrowsRunEnded(() => b.ShopStageWriteSink.PrepareShop(shop));
            snapshot.AssertUnchanged(b);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(outcome));
        }

        [TestCase(RunOutcome.Victory, false)]
        [TestCase(RunOutcome.Defeat, false)]
        [TestCase(RunOutcome.Victory, true)]
        [TestCase(RunOutcome.Defeat, true)]
        public void TerminalNoncombatPublication_RejectsPreparedPayment(RunOutcome outcome, bool purchase)
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.EndBattle();
            b.CurrencyWriteSink.SetAmount(10);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 5, new RecoveryHealSpec(RecoveryHealMode.Flat, 1));
            CharacterPurchaseDecision purchaseDecision = null;
            CharacterRecoveryDecision recoveryDecision = null;
            if (purchase)
            {
                b.CurrentProgressWriteSink.SetShopOffers(Offers());
                purchaseDecision = CharacterShopRules.PlanPurchase(Offers()[0], false, 10,
                    CharacterDefinitionAdapter.Read(new CharacterRosterDatabase().UnitTableQuery, EntityKey.Unit_Akimbo), Persistent(b));
                Assert.That(purchaseDecision.Succeeded, Is.True);
            }
            else
            {
                b.CurrentProgressWriteSink.SetRecoveryOptions(new[] { option });
                recoveryDecision = CharacterRecoveryCalculator.Plan(Persistent(b),
                    b.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id, 0, option, 10);
            }
            Seed(b, outcome);
            var snapshot = new RecoveryBufferSnapshot(b);
            if (purchase) ThrowsRunEnded(() => b.CharacterPurchaseWriteSink.PublishPurchase(new StageNodeId(2),
                new StageKey(10), new ShopOfferId(1), 10, purchaseDecision));
            else ThrowsRunEnded(() => b.RecoveryStageWriteSink.PublishRecovery(new StageNodeId(2), new StageKey(10), recoveryDecision));
            snapshot.AssertUnchanged(b);
        }

        private static ShopPurchaseSnapshot[] Offers()
            => Enumerable.Range(1, 3).Select(id => new ShopPurchaseSnapshot(new ShopOfferId(id), EntityKey.Unit_Akimbo, 5)).ToArray();

        public static IEnumerable<TestCaseData> TerminalOperations()
        {
            string[] operations = { "EnterChapter", "PrepareStageEntry", "ConfirmStageEntry", "PlanStageResolution",
                "ResolveStage", "EnterBattle", "EnterShop", "CompleteShop", "EnterRecovery", "CompleteRecovery",
                "PrepareShop", "Purchase", "LegacyPurchase", "InitializeRoster", "AcquireSub", "PrepareBattle",
                "ApplyHealth", "ApplyPersistentMain", "FinalizeTurn", "Recover", "RecoveryRecover",
                "ProcessNextCommand", "AdvanceCombat", "ResolveBattle", "AbandonBattle", "PrepareGraph" };
            foreach (var outcome in new[] { RunOutcome.Victory, RunOutcome.Defeat })
                foreach (string operation in operations) yield return new TestCaseData(operation, outcome);
        }

        [TestCaseSource(nameof(TerminalOperations))]
        public void EndedRun_RejectsGameplayWithoutWrites(string operation, RunOutcome outcome)
        {
            var runtime = TemporaryCoreRuntimeFactory.Create();
            try
            {
                var b = runtime.GetService(BufferSystemKey.Buffer);
                var core = runtime.GetService(SystemGroupsService.InGame).CoreGroup;
                Assert.That(core.Combat.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
                var main = b.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main);
                var ended = outcome == RunOutcome.Defeat ? StageOutcome.Defeat : StageOutcome.Victory;
                var progress = ended == StageOutcome.Defeat
                    ? ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(101))
                    : ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(101), false);
                var completion = new BattleCompletionCalculator(default).Complete(main.ToFacts(), Persistent(b),
                    new StageOutcomeSnapshot(ended, ended == StageOutcome.Defeat
                        ? StageOutcomeReason.VoluntaryAbandonment : StageOutcomeReason.EnemyHeadquartersDestroyed), progress);
                b.BattleWriteSink.CompleteBattle(completion);
                if (b.RunProgressReadSink.Completion == null) Seed(b, outcome);
                b.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
                var snapshot = new RecoveryBufferSnapshot(b);
                var receipt = b.RunProgressReadSink.Completion;
                object definitions = Cache(core);
                var node = new StageNodeId(2);
                switch (operation)
                {
                    case "EnterChapter": RunEnded(core.ChapterProgression.EnterChapter(1)); break;
                    case "PrepareStageEntry": RunEnded(core.ChapterProgression.PrepareStageEntry(1, node)); break;
                    case "ConfirmStageEntry": RunEnded(core.ChapterProgression.ConfirmStageEntry(1, node)); break;
                    case "PlanStageResolution": RunEnded(core.ChapterProgression.PlanStageResolution(1, StageOutcome.Defeat)); break;
                    case "ResolveStage": RunEnded(core.ChapterProgression.ResolveStage(1, StageOutcome.Defeat)); break;
                    case "EnterBattle": RunEnded(core.Combat.EnterBattle(1, node)); break;
                    case "EnterShop": RunEnded(core.Shop.EnterShop(1, node, 42)); break;
                    case "CompleteShop": RunEnded(core.Shop.CompleteShop(1, node)); break;
                    case "EnterRecovery": RunEnded(core.Recovery.EnterRecovery(1, node)); break;
                    case "CompleteRecovery": RunEnded(core.Recovery.CompleteRecovery(1, node)); break;
                    case "PrepareShop": Assert.That(core.Shop.PrepareShop(42), Is.EqualTo(ShopFailureReason.RunEnded)); break;
                    case "Purchase": Assert.That(core.Shop.Purchase(1, node, new ShopOfferId(1)).FailureReason, Is.EqualTo(ShopFailureReason.RunEnded)); break;
                    case "LegacyPurchase": Assert.That(core.Shop.Purchase(new ShopOfferId(1)).FailureReason, Is.EqualTo(ShopFailureReason.RunEnded)); break;
                    case "InitializeRoster": Assert.That(core.Characters.InitializeRoster(1).FailureReason, Is.EqualTo(RosterAcquireFailureReason.RunEnded)); break;
                    case "AcquireSub": Assert.That(core.Characters.AcquireSub(EntityKey.Unit_Akimbo).FailureReason, Is.EqualTo(RosterAcquireFailureReason.RunEnded)); break;
                    case "PrepareBattle": ThrowsRunEnded(() => core.Combat.PrepareBattle()); break;
                    case "ApplyHealth": ThrowsRunEnded(() => core.Combat.ApplyHealth(main.EntityId, 1)); break;
                    case "ApplyPersistentMain": ThrowsRunEnded(() => core.Combat.ApplyPersistentMain(Persistent(b))); break;
                    case "FinalizeTurn": ThrowsRunEnded(() => core.Combat.FinalizeTurn(main.EntityId,
                        new SoulActionChanges(completion.Characters, new SoulFieldChange(b.CombatStateReadSink.SoulField,
                            b.CombatStateReadSink.SoulField), Array.Empty<long>()), default)); break;
                    case "Recover": ThrowsRunEnded(() => core.Combat.Recover(new RecoveryOptionId(1))); break;
                    case "RecoveryRecover": ThrowsRunEnded(() => core.Recovery.Recover(1, node, new RecoveryOptionId(1))); break;
                    case "ProcessNextCommand": Assert.That(core.Combat.ProcessNextCommand(), Is.False); break;
                    case "AdvanceCombat": Assert.That(core.Combat.AdvanceCombat(), Is.False); break;
                    case "ResolveBattle": Assert.That(core.Combat.ResolveBattle(default), Is.False); break;
                    case "AbandonBattle": Assert.That(core.Combat.AbandonBattle(), Is.Not.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned)); break;
                    case "PrepareGraph": ThrowsRunEnded(() => core.StageGeneratorSystem.PrepareGraph(1)); break;
                    default: Assert.Fail(operation); break;
                }
                snapshot.AssertUnchanged(b);
                Assert.That(b.RunProgressReadSink.Completion, Is.SameAs(receipt));
                Assert.That(core.ChapterProgression.Outcome, Is.EqualTo(outcome));
                Assert.That(core.ChapterProgression.Completion, Is.SameAs(receipt));
                Assert.That(Cache(core), Is.SameAs(definitions));
            }
            finally { runtime.Shutdown(); }
        }

        [TestCase(RunOutcome.Victory)]
        [TestCase(RunOutcome.Defeat)]
        public void TerminalCleanupAndQueries_DoNotResetOutcome(RunOutcome outcome)
        {
            var runtime = TemporaryCoreRuntimeFactory.Create();
            try
            {
                var b = runtime.GetService(BufferSystemKey.Buffer);
                var core = runtime.GetService(SystemGroupsService.InGame).CoreGroup;
                core.Combat.EnterBattle(1, new StageNodeId(2));
                core.Combat.AbandonBattle();
                if (outcome == RunOutcome.Victory) { ((IRunProgressBufferStore)b.RunProgressReadSink).Reset(); Seed(b, outcome); }
                var receipt = b.RunProgressReadSink.Completion;
                Assert.That(receipt, Is.Not.Null);
                var completion = b.CombatStateReadSink.Completion;
                int handled = b.CombatStateReadSink.HandledCommandNumber, answered = b.CombatStateReadSink.AnsweredPreviewNumber,
                    sequence = b.CombatStateReadSink.LatestResultSequence;
                core.Characters.PlanInitialDraw(42);
                b.CombatViewWriteSink.AcknowledgePresented(sequence);
                core.Combat.EndBattle();
                Assert.That(core.Combat.ResolveBattle(default), Is.False);
                Assert.That(core.Combat.AbandonBattle(), Is.Not.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
                Assert.That(core.Combat.ProcessLatestPreview(), Is.False);
                Assert.Throws<InvalidOperationException>(() => core.Combat.GetDrawAvailability());
                Assert.Throws<InvalidOperationException>(() => core.Combat.GetTimeline());
                b.CombatViewWriteSink.Clear();
                Assert.That(b.CombatStateReadSink.Completion, Is.SameAs(completion));
                Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(b.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(answered));
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(b.RunProgressReadSink.Completion, Is.SameAs(receipt));
            }
            finally { runtime.Shutdown(); }
        }

        private static void RunEnded(ChapterProgressionResult result)
            => Assert.That(result.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
        private static void ThrowsRunEnded(TestDelegate action)
            => Assert.That(Assert.Throws<InvalidOperationException>(action).Message, Does.Contain("RunEnded"));
        internal static void Seed(IBufferQuery b, RunOutcome outcome)
        {
            var store = (IRunProgressBufferStore)b.RunProgressReadSink;
            var selected = b.CurrentProgressReadSink.CurrentStage;
            store.InstallCompletion(store.PrepareCompletion(new RunCompletionReceipt(outcome,
                b.CurrentProgressReadSink.CurrentMapKey, new StageNodeId(b.CurrentProgressReadSink.HasSelection ? selected.NodeId : 2),
                new StageKey(b.CurrentProgressReadSink.HasSelection ? selected.StageKey : 101))));
        }
        private static object Cache(IInGameCoreSystemGroupService core)
            => core.Combat.GetType().GetField("_definitions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(core.Combat);
        private static MainCharacterPersistentState Persistent(IBufferQuery b)
        { b.CharacterReadSink.TryGetMainState(out var main); return main; }
        private static BattleCompletionChanges Defeat(IBufferQuery b, int node = 2)
            => new BattleCompletionCalculator(default).Complete(b.CharacterReadSink.Characters.Single().ToFacts(), Persistent(b),
                new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.VoluntaryAbandonment),
                ChapterProgressionResult.Defeat(1, new StageNodeId(node), new StageKey(10)));
        private static CombatCommandTransition Transition(IBufferQuery b, BattleCompletionChanges completion)
        {
            var input = CombatCommandPublicationTests.Input(b);
            var state = new CombatCommandState(input.State.BoardWidth, input.State.BoardHeight, CombatFlowPhase.Finished,
                input.State.ActiveActorId, false, input.State.Structures, input.State.ThornFields, input.State.Timeline);
            var field = completion.Settlement?.Field ?? new SoulFieldChange(input.Souls, input.Souls);
            return new CombatCommandTransition(input, true, "", completion.Characters, field, state,
                completion.Outcome, completion: completion);
        }
    }
}
