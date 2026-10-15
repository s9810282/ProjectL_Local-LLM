using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.InGame.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.ServiceKeys;
using InputRouter = ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View.InputSystem;

namespace ProjectL.Tests.Framework
{
    public sealed class CombatMovementViewTests
    {
        [Test]
        public void ContentResult_IsPresentedAndAcknowledgedOnce()
        {
            using var r = new Rig(content: true);
            var result = r.Move(r.A, r.A + CellCoord.Up);
            var finalState = r.B.CombatStateReadSink.Current;
            var finalCharacters = r.B.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            r.Probe.DelayedKind = CombatEventKind.ObjectCreated;
            var runner = new JobRunnerSystem();
            var registry = CombatPlanTests.CreateRegistry(runner, new CombatPlanTests.FakeInGameService(r.Service), r.B);
            var flow = new CombatFlow(r.B.CombatStateReadSink, r.B.CombatResultReadSink, r.B.CombatViewReadSink,
                runner, registry, CombatExecutionMode.Live,
                new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            try
            {
                r.B.CombatViewWriteSink.Submit(r.LastMove);
                r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
                Assert.That(r.Probe.Callbacks.Count, Is.EqualTo(1));
                Assert.That(r.Writes.Acknowledgements, Is.Zero);
                Assert.That(r.Probe.Reconciles, Is.Zero);
                flow.Tick(); flow.Tick();
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(r.B.CombatStateReadSink.Current, Is.SameAs(finalState));
                r.Probe.Callbacks[0](); r.Probe.Callbacks[0]();
                r.View.Tick(0); r.View.Tick(0);
                Assert.That(r.Probe.Events, Is.EqualTo(result.Events));
                var kinds = r.Probe.Events.Select(e => e.Kind).ToList();
                Assert.That(kinds.IndexOf(CombatEventKind.Moved), Is.LessThan(kinds.IndexOf(CombatEventKind.ObjectActivated)));
                Assert.That(kinds.IndexOf(CombatEventKind.HeldEnvironmentChanged), Is.LessThan(kinds.IndexOf(CombatEventKind.HealthChanged)));
                Assert.That(kinds.IndexOf(CombatEventKind.HealthChanged), Is.LessThan(kinds.IndexOf(CombatEventKind.Died)));
                Assert.That(kinds.IndexOf(CombatEventKind.Died), Is.LessThan(kinds.IndexOf(CombatEventKind.ObjectCreated)));
                Assert.That(result.Events.OfType<CombatObjectCreatedEvent>().Count(), Is.EqualTo(1));
                Assert.That(r.Writes.Acknowledgements, Is.EqualTo(1));
                Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
                Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
                Assert.That(r.B.CombatStateReadSink.Current, Is.SameAs(finalState));
                foreach (var facts in finalCharacters)
                    Assert.That(CharacterCombatFactsComparer.SameFacts(facts,
                        r.B.CharacterReadSink.Characters.Single(c => c.EntityId == facts.EntityId).ToFacts()), Is.True);
            }
            finally { runner.Clear(); }
        }

        [Test]
        public void ConsumedSource_PresentationUsesCapturedFacts()
        {
            using var r = new Rig(content: true);
            var result = r.Move(r.A, r.A + CellCoord.Up);
            Assert.That(r.B.CombatStateReadSink.Current.Environment.Tiles.Where(t => t.Id == 31 || t.Id == 32)
                .All(t => !t.Active), Is.True);
            r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
            var mine = r.Probe.Events.OfType<CombatObjectActivationEvent>().Single(e => e.Source.Id == 32);
            Assert.That(mine.Source.Active, Is.True);
            Assert.That(mine.Source.Cell, Is.EqualTo(r.A + CellCoord.Up));
            Assert.That(((MineSourceFact)mine.Source).Damage, Is.EqualTo(5));
            var dead = r.Probe.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died);
            Assert.That(dead.Expected.Anchor, Is.EqualTo(r.A + CellCoord.Up + CellCoord.Right));
            Assert.That(dead.Final.Anchor, Is.Null);
            var drop = r.Probe.Events.OfType<CombatObjectCreatedEvent>().Single();
            Assert.That(drop.Source.Cell, Is.EqualTo(dead.Expected.Anchor));
            Assert.That(drop.Source.Id, Is.GreaterThan(33));
            Assert.That(r.Probe.Events, Is.EqualTo(result.Events));
            Assert.That(r.Writes.Acknowledgements, Is.EqualTo(1));
        }

        [Test]
        public void RejectedContentCommand_PublishesNoGameplayEvents_AndAcknowledgesRejection()
        {
            using var r = new Rig(content: true);
            var before = r.B.CombatStateReadSink.Current;
            var characters = r.B.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            r.B.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(new EntityId(999), AttackAim.ForCell(r.A)));
            Assert.That(r.Service.ProcessNextCommand(), Is.True);
            Assert.That(r.B.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Events, Is.Empty);
            Assert.That(r.B.CombatStateReadSink.Current.Environment.SameValues(before.Environment), Is.True);
            Assert.That(r.B.CombatStateReadSink.Current.CommandState.EnvironmentProgress.SameValues(
                before.CommandState.EnvironmentProgress), Is.True);
            r.View.Tick(0); r.View.Tick(0);
            Assert.That(r.Probe.Events, Is.Empty);
            Assert.That(r.Writes.Acknowledgements, Is.EqualTo(1));
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            foreach (var facts in characters)
                Assert.That(CharacterCombatFactsComparer.SameFacts(facts,
                    r.B.CharacterReadSink.Characters.Single(c => c.EntityId == facts.EntityId).ToFacts()), Is.True);
        }

        [Test]
        public void TargetingCancellation_KeepsReturn_WhileSuccessfulAttackSealsIt()
        {
            using var r = new Rig();
            r.Move(r.A, r.A + CellCoord.Up);
            r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True); r.View.Tick(0);
            Assert.That(r.Probe.ReturnPreview.CanReturn, Is.True);
            var before = r.B.CharacterReadSink.Characters.Single().ToFacts();
            r.View.Input.AttackModeRequested();
            Assert.That(r.Service.ProcessLatestPreview(), Is.True); r.View.Tick(0);
            r.View.Input.Cancel();
            Assert.That(CharacterCombatFactsComparer.SameFacts(before,
                r.B.CharacterReadSink.Characters.Single().ToFacts()), Is.True);
            r.View.Input.MoveModeRequested(); r.View.Input.Cancel();
            Assert.That(r.Service.ProcessLatestPreview(), Is.True); r.View.Tick(0);
            Assert.That(r.Probe.ReturnPreview.CanReturn, Is.True);
            r.B.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(before.EntityId, AttackAim.ForCell(new CellCoord(2, 1))));
            Assert.That(r.Service.ProcessNextCommand(), Is.True);
            r.B.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Attacked), Is.True);
            r.View.Tick(0); r.View.Tick(0);
            r.View.RequestReturn();
            Assert.That(r.Service.ProcessLatestPreview(), Is.True); r.View.Tick(0);
            Assert.That(r.Probe.ReturnPreview.CanReturn, Is.False);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.Writes.Acknowledgements, Is.EqualTo(2));
        }

        [Test]
        public void Playback_DoesNotSnapToFinalAnchorEarly()
        {
            using var r = new Rig();
            var result = r.Move(r.A, r.A + CellCoord.Up, r.A + CellCoord.Up + CellCoord.Right);
            r.View.Tick(0);
            Assert.That(r.Probe.VisualCell, Is.EqualTo(r.A));
            Assert.That(r.Probe.MovementStarts, Is.EqualTo(1));
            Assert.That(r.B.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(result.PathMovement.FinalStop));
            r.Probe.CompleteMovement(); r.View.Tick(0);
            Assert.That(r.Probe.VisualCell, Is.EqualTo(r.A + CellCoord.Up));
            Assert.That(r.Probe.MovementStarts, Is.EqualTo(2));
            Assert.That(r.Probe.Reconciles, Is.Zero);
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.Zero);
            r.Probe.CompleteMovement(); r.View.Tick(0); r.View.Tick(0);
            Assert.That(r.Probe.VisualCell, Is.EqualTo(result.PathMovement.FinalStop));
            Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
            Assert.That(r.Writes.Acknowledgements, Is.EqualTo(1));
        }

        [Test]
        public void DelayedPresentation_BlocksNextMutation()
        {
            using var r = new Rig();
            var result = r.Move(r.A, r.A + CellCoord.Up);
            r.B.CombatViewWriteSink.Submit(r.LastMove);
            var runner = new JobRunnerSystem();
            var register = CombatPlanTests.CreateRegistry(runner, new CombatPlanTests.FakeInGameService(r.Service), r.B);
            var flow = new CombatFlow(r.B.CombatStateReadSink, r.B.CombatResultReadSink, r.B.CombatViewReadSink, runner, register, CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            try
            {
                r.View.Tick(0); flow.Tick(); flow.Tick();
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(r.B.CombatStateReadSink.LatestResultSequence, Is.EqualTo(result.Sequence));
                r.Probe.CompleteMovement(); r.View.Tick(0); flow.Tick(); runner.Tick(0);
                Assert.That(r.B.CombatResultReadSink.TryGetLatest(out var rejected), Is.True);
                Assert.That(rejected.Accepted, Is.False);
                Assert.That(rejected.Sequence, Is.EqualTo(result.Sequence + 1));
            }
            finally { runner.Clear(); }
        }

        [Test]
        public void RejectedResult_IsAcknowledged()
        {
            using var r = new Rig();
            r.B.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(new EntityId(999), AttackAim.ForCell(r.A)));
            Assert.That(r.Service.ProcessNextCommand(), Is.True);
            r.B.CombatResultReadSink.TryGetLatest(out var result);
            Assert.That(result.Accepted, Is.False);
            r.View.Tick(0); r.View.Tick(0);
            Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
        }

        [Test]
        public void Return_SnapsWithoutTraversal()
        {
            using var r = new Rig();
            r.Move(r.A, r.A + CellCoord.Up);
            r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True);
            r.View.Tick(0);
            Assert.That(r.Probe.ReturnPreview.CanReturn, Is.True);
            r.View.RequestReturn(); r.View.RequestReturn();
            Assert.That(r.Service.ProcessLatestPreview(), Is.True);
            r.View.Tick(0);
            Assert.That(r.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Kind, Is.EqualTo(CombatViewCommandKind.ReturnToCheckpoint));
            Assert.That(r.Service.ProcessNextCommand(), Is.True);
            r.B.CombatResultReadSink.TryGetLatest(out var result);
            r.Probe.MovementStarts = 0; r.Probe.Reconciles = 0;
            r.View.Tick(0); r.View.Tick(0);
            Assert.That(r.Probe.ReturnSnaps, Is.EqualTo(1));
            Assert.That(r.Probe.MovementStarts, Is.Zero);
            Assert.That(r.Probe.VisualCell, Is.EqualTo(r.A));
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
            Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ComposedEnemyTurn_WaitsForAttackAfterMovement_ThenAcknowledgesExactlyOnce(bool endsNight)
        {
            using var r = new Rig(enemyTurn: true, endsNight: endsNight);
            var runner = new JobRunnerSystem();
            var registry = CombatPlanTests.CreateRegistry(runner, new CombatPlanTests.FakeInGameService(r.Service), r.B);
            var flow = new CombatFlow(r.B.CombatStateReadSink, r.B.CombatResultReadSink, r.B.CombatViewReadSink, runner, registry, CombatExecutionMode.Live, new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }));
            try
            {
                r.View.Input.EndTurnRequested();
                Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty, "자동 턴에는 플레이어 입력을 대기열에 남기지 않는다.");
                flow.Tick(); runner.Tick(0);
                Assert.That(r.B.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                Assert.That(result.Accepted, Is.True, result.Reason);
                Assert.That(result.Kind, Is.EqualTo(CombatProcessingKind.Advance));
                Assert.That(result.CommandKind, Is.Not.EqualTo(CombatCommandKind.Move));
                Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Moved), Is.True);
                Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Attacked), Is.True);
                r.Probe.DelayedKind = CombatEventKind.Attacked;
                var finalFacts = r.B.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
                var finalState = r.B.CombatStateReadSink.Current.CommandState;
                r.View.Tick(0);
                for (int i = 0; i < 10 && r.Probe.Callbacks.Count == 0; i++)
                {
                    r.Probe.CompleteMovement(); r.View.Tick(0);
                }
                Assert.That(r.Probe.Callbacks.Count, Is.EqualTo(1));
                Assert.That(r.Probe.Events.Last().Kind, Is.EqualTo(CombatEventKind.Attacked));
                Assert.That(r.Probe.Reconciles, Is.Zero);
                Assert.That(r.Writes.Acknowledgements, Is.Zero);
                Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.Zero);
                Assert.That(r.View.HasPendingPresentation, Is.True);
                flow.Tick(); flow.Tick();
                Assert.That(runner.IsIdle, Is.True);
                Assert.That(r.B.CombatStateReadSink.LatestResultSequence, Is.EqualTo(result.Sequence));
                r.Probe.Callbacks[0](); r.Probe.Callbacks[0]();
                r.View.Tick(0); r.View.Tick(0);
                Assert.That(r.Probe.Events.Select(e => e.Kind), Is.EqualTo(result.Events.Select(e => e.Kind)));
                Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
                Assert.That(r.Writes.Acknowledgements, Is.EqualTo(1));
                Assert.That(r.Probe.ReturnSnaps, Is.EqualTo(result.Events.OfType<CombatCharacterEvent>().Count(e =>
                    e.Kind == CombatEventKind.Returned && e.Expected.Area == CharacterRuntimeArea.Field && e.Final.Area == CharacterRuntimeArea.Field)));
                if (endsNight) Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e =>
                    e.Kind == CombatEventKind.Returned && e.Final.Area != CharacterRuntimeArea.Field), Is.True);
                Assert.That(finalState.SameValues(r.B.CombatStateReadSink.Current.CommandState), Is.True);
                foreach (var original in finalFacts)
                    Assert.That(ProjectL.Systems.RuntimeSystem.BufferSystems.Character.CharacterCombatFactsComparer.SameFacts(
                        original, r.B.CharacterReadSink.Characters.Single(c => c.EntityId == original.EntityId).ToFacts()), Is.True);
                Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
                flow.Tick();
                Assert.That(flow.IsStopped, Is.False);
            }
            finally { runner.Clear(); }
        }

        [Test]
        public void LateCallbackAfterDetach_CannotAcknowledgeNewBattle()
        {
            using var r = new Rig(enemyTurn: true);
            Assert.That(r.Service.AdvanceCombat(), Is.True);
            r.Probe.DelayedKind = CombatEventKind.Attacked;
            r.View.Tick(0);
            for (int i = 0; i < 10 && r.Probe.Callbacks.Count == 0; i++) { r.Probe.CompleteMovement(); r.View.Tick(0); }
            var late = r.Probe.Callbacks.Single();
            late(); r.View.Tick(0);
            int presentedBefore = r.B.CombatViewReadSink.PresentedResultSequence;
            r.View.Detach();
            var actor = r.B.CharacterReadSink.Characters.Single(c => c.Owner == Owner.Player).ToFacts();
            var old = r.B.CombatStateReadSink.Current.CommandState;
            var state = new CombatCommandState(old.BoardWidth, old.BoardHeight, CombatFlowPhase.PlayerInput,
                actor.EntityId, false, old.Structures, old.ThornFields,
                new[] { CombatTimelineEntrySnapshot.ForUnit(actor.EntityId, 1, 0, true) });
            r.B.BattleWriteSink.EndBattle();
            r.B.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { actor }, CharacterBufferTestData.Combat(),
                state.Structures, commandState: state));
            r.B.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(new EntityId(999), AttackAim.ForCell(r.A)));
            Assert.That(r.Service.ProcessNextCommand(), Is.True);
            r.B.CombatResultReadSink.TryGetLatest(out var current);
            r.View.AttachDisplay(r.Probe); r.Probe.Reconciles = 0;
            late();
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presentedBefore));
            r.View.Tick(0); late(); r.View.Tick(0);
            Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(current.Sequence));
        }

        [Test]
        public void MissingNonMovementHandler_BlocksReconcileAndAcknowledgement()
        {
            using var r = new Rig(enemyTurn: true);
            Assert.That(r.Service.AdvanceCombat(), Is.True);
            r.Probe.UnsupportedKind = CombatEventKind.Attacked;
            Assert.Throws<NotSupportedException>(() => r.View.Tick(0));
            r.View.StopInput();
            Assert.DoesNotThrow(() => r.View.Tick(0));
            Assert.That(r.Probe.Reconciles, Is.Zero);
            Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.Zero);
        }

        [Test]
        public void DeathCell_RemainsUntilDeathHandlerFinishes()
        {
            using var r = new Rig(enemyTurn: true, lethalEntry: true);
            Assert.That(r.Service.AdvanceCombat(), Is.True);
            r.B.CombatResultReadSink.TryGetLatest(out var result);
            var death = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died);
            Assert.That(death.Final.Anchor, Is.Null);
            Assert.That(death.Expected.Anchor, Is.EqualTo(new CellCoord(3, 3)));
            r.Probe.DelayedKind = CombatEventKind.Died;
            r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
            Assert.That(r.Probe.VisualCell, Is.EqualTo(death.Expected.Anchor));
            Assert.That(r.Probe.Reconciles, Is.Zero);
            Assert.That(r.Writes.Acknowledgements, Is.Zero);
            r.Probe.Callbacks.Single()(); r.View.Tick(0);
            Assert.That(r.Probe.VisualCell, Is.Null);
            Assert.That(r.Writes.Acknowledgements, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FocusOrCaptureLoss_CancelsFrozenDraft(bool focus)
        {
            using var r = new Rig();
            r.Slot.MoveModeRequested();
            r.Slot.PointerMoved(r.A.X + .5f, r.A.Y + .5f);
            r.Slot.PointerMoved(r.A.X + .5f, r.A.Y + 1.5f);
            r.Slot.SelectPressed(r.A.X + .5f, r.A.Y + 1.5f);
            r.Slot.SelectReleased(r.A.X + .5f, r.A.Y + 1.5f);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True);
            if (focus) r.View.FocusLost(); else r.View.CaptureLost();
            r.View.Tick(0);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.B.CombatViewReadSink.HasPreview, Is.False);
            Assert.That(r.Probe.Draft, Is.Null);
        }

        [Test]
        public void BattleEndBeforePointerCallback_CancelsWithoutMappingMissingState()
        {
            using var r = new Rig();
            r.Slot.MoveModeRequested();
            r.Slot.PointerMoved(r.A.X + .5f, r.A.Y + .5f);
            r.B.BattleWriteSink.EndBattle();
            Assert.DoesNotThrow(() => r.Slot.PointerMoved(2.5f, 2.5f));
            Assert.DoesNotThrow(() => r.Slot.SelectReleased(2.5f, 2.5f));
            r.View.Tick(0);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.Layers.CurrentLayer.IsValid, Is.False);
        }

        [Test]
        public void Factory_RegistersTicksRoutesInputAndShutsDownThroughExistingGroup()
        {
            using var r = new Rig(); r.View.Detach();
            var controllers = new IInputController[] { new DeploymentInputController(r.Slot), new CombatInputController(r.Slot) };
            var global = (GlobalSystemGroup)GlobalSystemFactory.Create(controllers, StageInputLayers.All);
            var router = (InputRouter)global.InputServiceCommand.InputSystemServices;
            router.Configure(r.Layers, InputConfiguration.Create(controllers));
            var group = (InGameViewSystemGroup)InGameViewSystemFactory.Create();
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(r.B); r.Slot.ExportServices(builder); global.ExportServices(builder); group.ExportServices(builder);
            var owner = (CombatViewSystem)group.Combat;
            BindView(owner, r.B, r.B.CombatViewWriteSink, r.Slot, r.Layers);
            try
            {
                Assert.Throws<ArgumentNullException>(() => group.Combat.Attach(null));
                owner.AttachDisplay(r.Probe); group.Tick(0); r.Probe.Reconciles = 0;
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.MoveMode, InputPhase.Performed,
                    InputValueData.FromButton(true), 0);
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.PointerPosition, InputPhase.Performed,
                    InputValueData.FromVector2(r.A.X + .5f, r.A.Y + .5f), 0);
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.PointerPosition, InputPhase.Performed,
                    InputValueData.FromVector2(r.A.X + .5f, r.A.Y + 1.5f), 0);
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.Select, InputPhase.Started, InputValueData.FromButton(true), 0);
                router.Submit(StageInputNames.Combat.Map, StageInputNames.Combat.Select, InputPhase.Performed, InputValueData.FromButton(false), 0);
                Assert.That(r.Probe.Draft.Cells, Is.EqualTo(new[] { r.A }), "Core 응답 전에는 유지 경로만 강조한다.");
                Assert.That(r.Service.ProcessLatestPreview(), Is.True); group.Tick(0);
                Assert.That(r.B.CombatViewReadSink.Commands.Count, Is.EqualTo(1));
                Assert.That(r.Service.ProcessNextCommand(), Is.True); group.Tick(0);
                Assert.That(r.Probe.Reconciles, Is.Zero);
                r.Probe.CompleteMovement(); group.Tick(0);
                Assert.That(r.Probe.Reconciles, Is.EqualTo(1));
                Assert.That(r.B.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(r.B.CombatStateReadSink.LatestResultSequence));
                group.Shutdown();
                Assert.That(r.Layers.CurrentLayer.IsValid, Is.False);
                Assert.That(r.Probe.Draft, Is.Null);
                Assert.DoesNotThrow(() => r.Slot.Bind(r.View.Input)); r.Slot.Unbind(r.View.Input);
            }
            finally { group.Shutdown(); global.Shutdown(); }
        }

        internal static void BindView(CombatViewSystem view, IBufferQuery b, ICombatViewBufferWriteSink write,
            IStageInputReceiverSlot slot, IInputLayerService layers)
        {
            view.BindServices(b.CombatStateReadSink, b.CombatResultReadSink, b.CharacterReadSink, b.CurrentProgressReadSink,
                b.CombatViewReadSink, write, slot, layers, new CombatViewSystemTests.FakeLoadingRegister(),
                new CombatViewSystemTests.FakeColors(), new CombatViewSystemTests.FakeAreaOutlines(), new CombatViewSystemTests.EntryRead(),
                new CombatViewSystemTests.AbandonProbe());
        }

        [Test]
        public void StaleMovementPreview_DoesNotClearAttackFeedback()
        {
            using var r = new Rig();
            r.Move(r.A, r.A + CellCoord.Up);
            r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True);
            r.View.Input.AttackModeRequested();
            int clears = r.Probe.DraftClears;
            r.View.Tick(0); r.View.Tick(0);
            Assert.That(r.Probe.DraftClears, Is.EqualTo(clears));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AttackCancellation_RequiresQBeforePathInput(bool focusLoss)
        {
            using var r = new Rig();
            r.View.Input.AttackModeRequested();
            if (focusLoss) r.View.FocusLost(); else r.View.Input.Cancel();
            r.View.Input.SelectPressed(r.A.X + .5f, r.A.Y + .5f);
            r.View.Input.PointerMoved(r.A.X + .5f, r.A.Y + 1.5f);
            r.View.Input.SelectReleased(r.A.X + .5f, r.A.Y + 1.5f);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.Probe.Draft, Is.Null);
            r.View.Input.MoveModeRequested();
            r.View.Input.PointerMoved(r.A.X + .5f, r.A.Y + .5f);
            r.View.Input.PointerMoved(r.A.X + .5f, r.A.Y + 1.5f);
            r.View.Input.SelectPressed(r.A.X + .5f, r.A.Y + 1.5f);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True);
            r.View.Tick(0);
            Assert.That(r.B.CombatViewReadSink.Commands.Single().Data.Path.Cells,
                Is.EqualTo(new[] { r.A, r.A + CellCoord.Up }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelledDraft_RestoresReturnPreview(bool outside)
        {
            using var r = new Rig();
            var stop = r.A + CellCoord.Up;
            r.Move(r.A, stop); r.View.Tick(0); r.Probe.CompleteMovement(); r.View.Tick(0);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True); r.View.Tick(0);
            Assert.That(r.Probe.ReturnPreview.CanReturn, Is.True);
            r.View.Input.MoveModeRequested();
            r.View.Input.PointerMoved(stop.X + .5f, stop.Y + .5f);
            r.View.Input.PointerMoved(outside ? -1f : stop.X + .5f, stop.Y + .5f);
            r.View.Input.Cancel();
            r.View.Tick(0);
            Assert.That(r.B.CombatViewReadSink.Commands, Is.Empty);
            Assert.That(r.Service.ProcessLatestPreview(), Is.True);
            r.View.Tick(0);
            Assert.That(r.Probe.ReturnPreview.CanReturn, Is.True);
            r.View.RequestReturn();
            Assert.That(r.Service.ProcessLatestPreview(), Is.True); r.View.Tick(0);
            Assert.That(r.Service.ProcessNextCommand(), Is.True); r.View.Tick(0);
            Assert.That(r.B.CharacterReadSink.Characters.Single().Anchor, Is.EqualTo(r.A));
        }

        private sealed class Rig : IDisposable
        {
            internal readonly IBufferQuery B;
            internal readonly CombatProcessingService Service;
            internal readonly CombatViewSystem View;
            internal readonly CountingInput Writes;
            internal readonly Probe Probe = new Probe();
            internal readonly StageInputReceiverSlot Slot = new StageInputReceiverSlot();
            internal readonly ProbeLayers Layers = new ProbeLayers();
            internal readonly CombatSceneRoot Root;
            internal readonly CellCoord A;
            internal CombatViewCommandData LastMove;
            internal Rig(bool enemyTurn = false, bool lethalEntry = false, bool endsNight = false, bool content = false)
            {
                B = content ? CombatPreviewPublicationTests.Create(out Service, new IMovementTileRule[] {
                    new ThornMovementTileRule(), new FlagMovementTileRule(), new MineMovementTileRule() }) :
                    CombatPreviewPublicationTests.Create(out Service);
                var main = B.CharacterReadSink.Characters.Single().ToFacts(); A = main.Anchor.Value;
                if (lethalEntry)
                {
                    var before = B.CombatStateReadSink.Current.CommandState;
                    var state = new CombatCommandState(before.BoardWidth, before.BoardHeight, before.Phase, main.EntityId,
                        before.IsNight, before.Structures, new[] { new ThornFieldCellSnapshot(enemyTurn ? new CellCoord(3, 3) : A + CellCoord.Up, 1, 99) },
                        before.Timeline, before.Turn);
                    B.BattleWriteSink.EndBattle();
                    B.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, CharacterBufferTestData.Combat(),
                        before.Structures, commandState: state));
                }
                if (enemyTurn)
                {
                    var e = CharacterBufferTestData.EnemyFacts(new EntityId(3));
                    var stats = e.Stats;
                    var enemy = new CharacterCombatFacts(e.EntityId, null, e.CharacterKey, e.Owner, null,
                        CharacterRuntimeArea.Field, e.Health,
                        new UnitStatsSnapshot(
                            stats.MoveMethod, stats.MoveRange, stats.MoveCost, stats.AppointRange,
                            stats.AttackPower, stats.PhysicalDefense, stats.MaxActionCost, stats.AttackCost,
                            stats.RemainingMoveRange, stats.MaxActionCost, stats.Priority, stats.Delay, stats.TraitStatuses.ToArray(), stats.Category),
                        null, new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) }, new CellCoord(4, 3), 0, 0);
                    var before = B.CombatStateReadSink.Current.CommandState;
                    var turn = new CombatTurnFacts(StagePhase.Combat, currentNightTime: endsNight ? 99 : 0, participants: new[] {
                        new CombatTurnParticipant(main.EntityId, 1, 1), new CombatTurnParticipant(enemy.EntityId, 1, 1) });
                    var state = new CombatCommandState(before.BoardWidth, before.BoardHeight, CombatFlowPhase.Automatic,
                        enemy.EntityId, true, before.Structures, before.ThornFields, new[] {
                            CombatTimelineEntrySnapshot.ForUnit(enemy.EntityId, 1, 0, true),
                            CombatTimelineEntrySnapshot.ForUnit(main.EntityId, 1, 0, false) }, turn);
                    B.BattleWriteSink.EndBattle();
                    var preparation = new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.Automatic,
                        Array.Empty<CharacterInstanceId>(), 0);
                    B.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main, enemy }, preparation,
                        before.Structures, commandState: state));
                }
                if (content)
                {
                    var cell = A + CellCoord.Up;
                    var enemyCell = cell + CellCoord.Right;
                    var flag = new FlagSourceFact(31, cell, 1, 7, true, new[] { Owner.Player, Owner.Enemy });
                    var mine = new MineSourceFact(32, cell, 2, 5, true, new[] { Owner.Player, Owner.Enemy });
                    var heldFlag = new FlagSourceFact(33, enemyCell, 3, 0, false, new[] { Owner.Player, Owner.Enemy });
                    var e = CharacterBufferTestData.EnemyFacts(new EntityId(3), 1);
                    var enemy = new CharacterCombatFacts(e.EntityId, null, e.CharacterKey, e.Owner, null,
                        CharacterRuntimeArea.Field, e.Health, e.Stats, null, enemyCell, Rotation4.R0,
                        new[] { enemyCell }, enemyCell, 0, 0, heldEnvironment: new CharacterHeldEnvironmentState(flag: HeldFlag.From(heldFlag)));
                    var before = B.CombatStateReadSink.Current.CommandState;
                    var env = new MovementEnvironmentFacts(new MovementTileFact[] { flag, mine, heldFlag });
                    var bindings = new EnvironmentSemanticBindings(Array.Empty<long>(), new[] { 31L, 32L, 33L }, Array.Empty<EntityId>());
                    var state = new CombatCommandState(before.BoardWidth, before.BoardHeight, before.Phase,
                        main.EntityId, before.IsNight, before.Structures, before.ThornFields, before.Timeline, before.Turn, env);
                    B.BattleWriteSink.EndBattle();
                    B.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main, enemy },
                        new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput,
                            Array.Empty<CharacterInstanceId>(), environment: env, environmentBindings: bindings),
                        before.Structures, commandState: state));
                }
                Root = new GameObject("Movement View test root").AddComponent<CombatSceneRoot>();
                if (content)
                    Probe.ScenePresenter = new CombatSceneEventPresenter(Root.gameObject.AddComponent<
                        ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.FieldUnitPresenter>(), .1f);
                Probe.VisualCell = A;
                Writes = new CountingInput(B.CombatViewWriteSink);
                View = new CombatViewSystem(CombatExecutionMode.Live,
                    new ProjectL.Systems.RuntimeSystem.Common.RuntimeFaultBoundary(_ => { }), new OrderedCombatPresentation());
                BindView(View, B, Writes, Slot, Layers);
                View.AttachDisplay(Probe); View.Tick(0); Probe.Reconciles = 0;
            }
            internal CombatResult Move(params CellCoord[] cells)
            {
                var actor = B.CharacterReadSink.Characters.Single(c => c.Owner == Owner.Player);
                LastMove = MovementPublicationTests.PreviewedMove(B, Service, actor.EntityId, cells);
                B.CombatViewWriteSink.Submit(LastMove);
                Assert.That(Service.ProcessNextCommand(), Is.True);
                B.CombatResultReadSink.TryGetLatest(out var result);
                Assert.That(result.Accepted, Is.True, result.Reason);
                return result;
            }
            public void Dispose() { View.Shutdown(); UnityEngine.Object.DestroyImmediate(Root.gameObject); }
        }

        internal sealed class Probe : CombatViewSystemTests.FakeDisplay
        {
            internal CellCoord? VisualCell;
            internal int MovementStarts, ReturnSnaps, Reconciles, DraftClears;
            internal MovementPath Draft;
            internal CheckpointReturnPreview ReturnPreview;
            internal readonly List<CombatEvent> Events = new List<CombatEvent>();
            internal readonly List<Action> Callbacks = new List<Action>();
            internal CombatEventKind? DelayedKind, UnsupportedKind;
            internal CombatSceneEventPresenter ScenePresenter;
            private CombatCharacterEvent _step;
            public override float InnerCellInset => .15f;
            private bool _movementComplete = true;
            public override bool IsMovementComplete => _movementComplete;
            public override bool TryMapPointer(Vector2 screen, out Vector2 grid, out bool cancel)
            { grid = screen; cancel = screen.x > 100; return screen.x >= 0 && screen.y >= 0; }
            public override void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null) { Draft = path; }
            public override void ClearDraft() { Draft = null; DraftClears++; }
            public override void SetReturnState(CheckpointReturnPreview preview) { ReturnPreview = preview; }
            public override void BeginStep(CombatCharacterEvent step)
            { _step = step; MovementStarts++; Events.Add(step); _movementComplete = false; }
            internal void CompleteMovement() { if (_step != null) VisualCell = _step.Final.Anchor; _movementComplete = true; }
            public override void ShowReturn(CombatCharacterEvent returned) { Events.Add(returned); ReturnSnaps++; VisualCell = returned.Final.Anchor; }
            public override void CancelMovement() { _step = null; _movementComplete = true; }
            public override bool CanPresent(CombatEvent e) => e.Kind != UnsupportedKind && (ScenePresenter?.CanPresent(e) ?? true);
            public override void BeginEvent(CombatEvent e, Action completed)
            { Events.Add(e); if (e.Kind == DelayedKind) Callbacks.Add(completed); else completed(); }
            public override void Refresh(in CombatDisplayValues values)
            {
                Reconciles++;
                VisualCell = _step == null ? values.Characters.First(c => c.Owner == Owner.Player).Anchor :
                    values.Characters.FirstOrDefault(c => c.EntityId == _step.Final.EntityId)?.Anchor;
            }
            public void Detach() { }
        }

        private sealed class CountingInput : ICombatViewBufferWriteSink
        {
            private readonly ICombatViewBufferWriteSink _inner;
            internal int Acknowledgements;
            internal CountingInput(ICombatViewBufferWriteSink inner) => _inner = inner;
            public int Submit(CombatViewCommandData command) => _inner.Submit(command);
            public int SetPreview(CombatViewPreviewData preview) => _inner.SetPreview(preview);
            public void ClearPreview() => _inner.ClearPreview();
            public void Clear() => _inner.Clear();
            public void RemoveCommandsThrough(int handled) => _inner.RemoveCommandsThrough(handled);
            public void AcknowledgePresented(int sequence) { Acknowledgements++; _inner.AcknowledgePresented(sequence); }
        }

        internal sealed class ProbeLayers : IInputLayerService, IInputLayerRuntime
        {
            public InputLayer CurrentLayer { get; private set; }
            public void ChangeLayer(InputLayer next) { CurrentLayer = next; }
            public void ClearLayer() { CurrentLayer = default; }
        }
    }
}
