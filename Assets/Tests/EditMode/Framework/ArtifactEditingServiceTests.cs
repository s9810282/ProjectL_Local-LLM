using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class ArtifactEditingServiceTests
    {
        [Test]
        public void FactoryRegistersServiceAndBindingModule()
        {
            var f = Create();
            var service = (ArtifactEditingService)f.Core.Artifacts;
            var builder = new SystemBuilderHelper();
            service.ExportServices(builder);
            var reader = builder.BuildSystemReader();
            Assert.That(reader.ServiceStoreReader.GetService(InGameSystemServiceKey.Artifacts), Is.SameAs(service));
            Assert.That(reader.SystemModuleStoreReader.Modules.Count, Is.EqualTo(1));
            Assert.That(reader.SystemModuleStoreReader.InitDataModules.Count, Is.EqualTo(1));
            Assert.That(reader.SystemModuleStoreReader.PostBindServiceModules.Count, Is.EqualTo(1));
            Assert.That(f.Core.Artifacts.InitializeBoard(4, 4), Is.True);
            Assert.That(f.Buffers.ArtifactReadSink.Current.PoolItems.Select(i => i.ArtifactId), Is.EqualTo(new[] { 100, 100, 200 }));
        }

        [Test]
        public void BindingDoesNotInitializeOrGrantInventory()
        {
            var f = Create();
            Assert.That(f.Buffers.ArtifactReadSink.Current.IsInitialized, Is.False);
            Assert.That(f.Table.AllReads, Is.Zero);
            ((ISystemShutdown)f.Core).Shutdown();
            Assert.That(f.Buffers.ArtifactReadSink.Current.IsInitialized, Is.False);
        }

        [Test]
        public void InitializeTwiceAndRecreateService_PreservesCurrentInventory()
        {
            var f = Create();
            f.Core.Artifacts.InitializeBoard(4, 4);
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            ArtifactBufferSnapshot before = f.Buffers.ArtifactReadSink.Current;
            f.Table.ThrowOnRead = true;
            Assert.That(f.Core.Artifacts.InitializeBoard(99, 99), Is.False);
            var recreated = Create(f.Buffers, f.Table);
            Assert.That(recreated.Core.Artifacts.InitializeBoard(2, 2), Is.False);
            Assert.That(f.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
            ((ISystemShutdown)recreated.Core).Shutdown();
            Assert.That(f.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void BeforeReady_AllEntryPointsThrow(int stage)
        {
            var f = Create(bind: false);
            var service = new ArtifactEditingService(new ArtifactEditCalculator());
            if (stage == 1) service.InitData(f.Database);
            if (stage == 2) service.PostBindService(f.Resolver);
            Assert.Throws<InvalidOperationException>(() => service.InitializeBoard(4, 4));
            foreach (Action<IArtifactEditingService> command in Commands())
                Assert.Throws<InvalidOperationException>(() => command(service));
            Assert.Throws<InvalidOperationException>(() => service.PreviewPlace(100, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<InvalidOperationException>(() => service.PreviewMove(0, CellCoord.Zero, Rotation4.R0));
            Assert.That(f.Buffers.ArtifactReadSink.Current.IsInitialized, Is.False);
        }

        [Test]
        public void BeforeBoardInitialization_EditsAndPreviewsThrow()
        {
            var f = Create();
            foreach (Action<IArtifactEditingService> command in Commands())
                Assert.Throws<InvalidOperationException>(() => command(f.Core.Artifacts));
            Assert.Throws<InvalidOperationException>(() => f.Core.Artifacts.PreviewPlace(100, CellCoord.Zero, Rotation4.R0));
            Assert.Throws<InvalidOperationException>(() => f.Core.Artifacts.PreviewMove(0, CellCoord.Zero, Rotation4.R0));
            Assert.That(f.Table.Lookups, Is.Zero);
        }

        [Test]
        public void ServiceUsesInjectedCalculationPort()
        {
            var f = Create();
            var probe = new CalculationProbe();
            var service = new ArtifactEditingService(probe);
            service.InitData(f.Database);
            service.PostBindService(f.Resolver);
            service.InitializeBoard(4, 4);
            service.Place(100, CellCoord.Zero, Rotation4.R0);
            service.Move(0, new CellCoord(1, 0), Rotation4.R90);
            service.PreviewPlace(100, CellCoord.Zero, Rotation4.R0);
            service.PreviewMove(0, new CellCoord(2, 0), Rotation4.R180);
            service.ExpandBoard(5, 5);
            service.ReturnToPool(0);
            Assert.That(probe.Calls, Is.EqualTo(new[] { "Initialize", "Place", "Move", "PreviewPlace", "PreviewMove", "Expand", "Return" }));
            Assert.That(probe.InitialDefinitionIds, Is.EqualTo(new[] { 100, 200 }));
            Assert.Throws<ArgumentNullException>(() => new ArtifactEditingService(null));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryMutationWhileHasBattle_PublishesLayoutLocked(int phase)
        {
            var buffers = CharacterBattleSettlementTests.Create();
            buffers.BattleWriteSink.EndBattle();
            var f = Create(buffers);
            f.Core.Artifacts.InitializeBoard(4, 4);
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            ArtifactBufferSnapshot before = buffers.ArtifactReadSink.Current;
            PrepareBattle(buffers, phase);
            f.Table.ThrowOnRead = true;
            foreach (Action<IArtifactEditingService> command in Commands())
            {
                f.Core.Artifacts.PreviewMove(999, CellCoord.Zero, Rotation4.R0);
                command(f.Core.Artifacts);
                Assert.That(buffers.ArtifactReadSink.Current, Is.SameAs(before));
                Assert.That(buffers.ArtifactEditResultReadSink.LastEditResult.Value.FailureReason, Is.EqualTo(ArtifactActionFailureReason.LayoutLocked));
                Assert.That(buffers.ArtifactPreviewReadSink.LastPreview, Is.Null);
            }
            Assert.That(f.Core.Artifacts.InitializeBoard(2, 2), Is.False);
            f.Table.ThrowOnRead = false;
            buffers.BattleWriteSink.EndBattle();
            f.Core.Artifacts.Place(100, new CellCoord(1, 0), Rotation4.R0);
            Assert.That(buffers.ArtifactEditResultReadSink.LastEditResult.Value.Succeeded, Is.True);
        }

        [Test]
        public void FirstInitializationDuringBattle_ThrowsBeforeReadingDefinitions()
        {
            var f = Create(CharacterBattleSettlementTests.Create());
            f.Table.ThrowOnRead = true;
            Assert.Throws<InvalidOperationException>(() => f.Core.Artifacts.InitializeBoard(4, 4));
            Assert.That(f.Buffers.ArtifactReadSink.Current.IsInitialized, Is.False);
            Assert.That(f.Table.AllReads, Is.Zero);
        }

        [Test]
        public void ChapterAndBattleTransitions_PreserveArtifactAggregate()
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.EndBattle();
            var f = Create(b);
            f.Core.Artifacts.InitializeBoard(4, 4);
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            ArtifactBufferSnapshot before = b.ArtifactReadSink.Current;
            b.MapProgressWriteSink.RegisterMap(2);
            b.CurrentProgressWriteSink.SelectMap(2);
            PrepareBattle(b, 1);
            b.BattleWriteSink.EndBattle();
            b.RunProgressWriteSink.ResetRunProgress();
            Assert.That(b.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(b.ArtifactReadSink.Current.PoolItems.Count, Is.EqualTo(2));
            Assert.That(b.ArtifactReadSink.Current.PlacedItems.Single().InstanceId, Is.Zero);
        }

        [Test]
        public void PreviewPublishesOnlyDisplayOutput()
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.EndBattle();
            var f = Create(b);
            f.Core.Artifacts.InitializeBoard(4, 4);
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            ArtifactBufferSnapshot before = b.ArtifactReadSink.Current;
            ArtifactActionResult result = b.ArtifactEditResultReadSink.LastEditResult.Value;
            b.CurrencyWriteSink.SetAmount(10);
            b.CharacterReadSink.TryGetMainState(out var main);
            f.Core.Artifacts.PreviewPlace(200, new CellCoord(1, 1), Rotation4.R90);
            Assert.That(b.ArtifactPreviewReadSink.LastPreview.FailureReason, Is.EqualTo(ArtifactActionFailureReason.None));
            Assert.That(b.ArtifactPreviewReadSink.LastPreview.Shape.Cells, Is.EqualTo(new[] { new CellCoord(0, 1), CellCoord.Zero }));
            f.Core.Artifacts.PreviewMove(0, CellCoord.Zero, Rotation4.R0);
            Assert.That(b.ArtifactPreviewReadSink.LastPreview.FailureReason, Is.EqualTo(ArtifactActionFailureReason.None));
            Assert.That(b.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(b.ArtifactEditResultReadSink.LastEditResult.Value, Is.EqualTo(result));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(10));
            b.CharacterReadSink.TryGetMainState(out var after);
            Assert.That(after, Is.EqualTo(main));
            Assert.That(b.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void AcceptedEdit_PublishesMatchingStateResultAndClearsPreview()
        {
            var f = Initialized();
            f.Core.Artifacts.PreviewPlace(100, CellCoord.Zero, Rotation4.R0);
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            AssertSuccess(f, 100, 0);
            f.Core.Artifacts.Move(0, new CellCoord(2, 1), Rotation4.R90);
            AssertSuccess(f, 100, 0);
            Assert.That(f.Buffers.ArtifactReadSink.Current.PlacedItems.Single().Anchor, Is.EqualTo(new CellCoord(2, 1)));
            f.Core.Artifacts.ExpandBoard(6, 5);
            Assert.That(f.Buffers.ArtifactReadSink.Current.BoardWidth, Is.EqualTo(6));
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult.Value.InstanceId, Is.Null);
            f.Core.Artifacts.ReturnToPool(0);
            AssertSuccess(f, 100, 0);
            Assert.That(f.Buffers.ArtifactReadSink.Current.PlacedItems, Is.Empty);
            Assert.That(f.Buffers.ArtifactReadSink.Current.PoolItems.Last().InstanceId, Is.EqualTo(3));
        }

        [TestCase(0, ArtifactActionFailureReason.OutOfBounds)]
        [TestCase(1, ArtifactActionFailureReason.CellOccupied)]
        [TestCase(2, ArtifactActionFailureReason.NotInPool)]
        [TestCase(3, ArtifactActionFailureReason.InstanceNotFound)]
        [TestCase(4, ArtifactActionFailureReason.DefinitionNotFound)]
        [TestCase(5, ArtifactActionFailureReason.BoardShrinkNotAllowed)]
        public void RejectedEdit_PublishesFailureWithoutChangingCurrent(int command, ArtifactActionFailureReason reason)
        {
            var f = Initialized();
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            f.Core.Artifacts.Place(100, new CellCoord(1, 0), Rotation4.R0);
            ArtifactBufferSnapshot before = f.Buffers.ArtifactReadSink.Current;
            f.Core.Artifacts.PreviewPlace(200, new CellCoord(1, 1), Rotation4.R0);
            switch (command)
            {
                case 0: f.Core.Artifacts.Place(200, new CellCoord(4, 4), Rotation4.R0); break;
                case 1: f.Core.Artifacts.Move(0, new CellCoord(1, 0), Rotation4.R0); break;
                case 2: f.Core.Artifacts.Place(100, new CellCoord(2, 0), Rotation4.R0); break;
                case 3: f.Core.Artifacts.ReturnToPool(999); break;
                case 4: f.Core.Artifacts.Place(999, CellCoord.Zero, Rotation4.R0); break;
                case 5: f.Core.Artifacts.ExpandBoard(2, 2); break;
            }
            Assert.That(f.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult.Value.FailureReason, Is.EqualTo(reason));
            Assert.That(f.Buffers.ArtifactPreviewReadSink.LastPreview, Is.Null);
        }

        [Test]
        public void DefinitionAndCalculationFaults_PreserveCurrentAndOutputs()
        {
            var f = Initialized();
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            ArtifactBufferSnapshot before = f.Buffers.ArtifactReadSink.Current;
            ArtifactActionResult result = f.Buffers.ArtifactEditResultReadSink.LastEditResult.Value;
            f.Core.Artifacts.PreviewMove(0, CellCoord.Zero, Rotation4.R0);
            var preview = f.Buffers.ArtifactPreviewReadSink.LastPreview;
            f.Table.ThrowOnRead = true;
            foreach (Action<IArtifactEditingService> command in Commands())
                Assert.Throws<InvalidOperationException>(() => command(f.Core.Artifacts));
            f.Table.ThrowOnRead = false;
            Assert.Throws<ArgumentOutOfRangeException>(() => f.Core.Artifacts.Move(0, CellCoord.Zero, (Rotation4)99));
            Assert.That(f.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult.Value, Is.EqualTo(result));
            Assert.That(f.Buffers.ArtifactPreviewReadSink.LastPreview, Is.SameAs(preview));
        }

        [Test]
        public void ReturnIdOverflow_PreservesCurrentAndOutputs()
        {
            var f = Create();
            Assert.That(f.Buffers.ArtifactInitializationWriteSink.TryInitialize(new ArtifactState(4, 4,
                new[] { new ArtifactItemState(100, int.MaxValue, false, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) },
                new[] { new ArtifactItemState(100, 0, true, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) })), Is.True);
            ArtifactBufferSnapshot before = f.Buffers.ArtifactReadSink.Current;
            Assert.Throws<OverflowException>(() => f.Core.Artifacts.ReturnToPool(0));
            Assert.That(f.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult, Is.Null);
        }

        [Test]
        public void InitializationOverflow_PublishesNothing()
        {
            var f = Create(table: new ArtifactTable(Definition(100, int.MaxValue), Definition(200, 1)));
            Assert.Throws<OverflowException>(() => f.Core.Artifacts.InitializeBoard(4, 4));
            Assert.That(f.Buffers.ArtifactReadSink.Current.IsInitialized, Is.False);
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult, Is.Null);
        }

        [Test]
        public void PublicationSinkFault_PropagatesWithoutPublishingSuccess()
        {
            var f = Initialized();
            var writer = new ThrowingEditSink();
            var root = new SystemBuilderHelper();
            root.AddService<IBufferQuery>(new EditFaultQuery(f.Buffers, writer));
            var service = new ArtifactEditingService(new ArtifactEditCalculator());
            service.InitData(f.Database);
            service.PostBindService(root.BuildSystemReader().ServiceStoreReader);
            ArtifactBufferSnapshot before = f.Buffers.ArtifactReadSink.Current;
            Assert.Throws<InvalidOperationException>(() => service.Place(100, CellCoord.Zero, Rotation4.R0));
            Assert.That(writer.Calls, Is.EqualTo(1));
            Assert.That(f.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult, Is.Null);
        }

        [TestCase(0, 7)]
        [TestCase(1, 10)]
        [TestCase(2, 13)]
        public void AcceptedPlacementFeedsExistingCombatEffectsOncePerInstance(int placed, int expectedHealth)
        {
            var b = CharacterBattleSettlementTests.Create();
            var main = b.CharacterReadSink.Characters.Single().ToFacts();
            b.CharacterReadSink.TryGetMainState(out var persistent);
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetPersistentMain(main, persistent, CharacterBufferTestData.MainState(7, 20)));
            main = b.CharacterReadSink.Characters.Single().ToFacts();
            b.BattleWriteSink.EndBattle();
            // 기존 진행 서비스도 실제 테스트 DB의 StageKey 100을 조회하도록 준비한다.
            b.StageProgressWriteSink.Clear();
            b.StageProgressWriteSink.RegisterStages(1, new[] { new StageProgressData(2, 100, 1, 0, Array.Empty<int>(), false) });
            b.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Fixture battle");
            var f = Create(b, new ArtifactTable(Definition(100, 3, true)), battleSource: new BattleSource());
            f.Core.Artifacts.InitializeBoard(4, 4);
            for (int i = 0; i < placed; i++) f.Core.Artifacts.Place(100, new CellCoord(i, 0), Rotation4.R0);
            main = WithActionCost(main);
            var hq = new StructureCombatFacts(new EntityId(2),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 5, false, 0),
                new HealthStateSnapshot(5, 5), new CellCoord(2, 1), Rotation4.R0, new[] { new CellCoord(2, 1) });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, main.EntityId, false,
                new[] { hq }, Array.Empty<ThornFieldCellSnapshot>(), new[] { CombatTimelineEntrySnapshot.ForUnit(main.EntityId, 1, 0, true) });
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, CharacterBufferTestData.Combat(), new[] { hq }, commandState: state));
            b.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(main.EntityId, AttackAim.ForCell(new CellCoord(2, 1))));
            Assert.That(f.Core.Combat.ProcessNextCommand(), Is.True);
            Assert.That(f.Core.Combat.ProcessNextCommand(), Is.False);
            Assert.That(b.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            b.CharacterReadSink.TryGetMainState(out var healed);
            Assert.That(healed.CurrentHealth, Is.EqualTo(expectedHealth));
            Assert.That(b.ArtifactReadSink.Current.PlacedItems.Count, Is.EqualTo(placed));
            Assert.That(b.ArtifactReadSink.Current.PoolItems.Count, Is.EqualTo(3 - placed));
        }

        [Test]
        public void ExpansionAssemblesOneDefinitionPerPlacedKind()
        {
            var f = Initialized();
            f.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            f.Core.Artifacts.Place(100, new CellCoord(1, 0), Rotation4.R0);
            int reads = f.Table.Lookups;
            f.Core.Artifacts.ExpandBoard(6, 6);
            Assert.That(f.Table.Lookups - reads, Is.EqualTo(1));
            Assert.That(f.Buffers.ArtifactEditResultReadSink.LastEditResult.Value.Succeeded, Is.True);
            Assert.That(f.Buffers.ArtifactReadSink.Current.PlacedItems.Select(i => i.InstanceId), Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void RebindingUsesNewCanonicalBuffer()
        {
            var first = Initialized();
            var second = Initialized();
            var before = first.Buffers.ArtifactReadSink.Current;
            ((ISystemPostBindService)first.Core).PostBindService(second.Resolver);
            first.Core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            Assert.That(first.Buffers.ArtifactReadSink.Current, Is.SameAs(before));
            Assert.That(second.Buffers.ArtifactReadSink.Current.PlacedItems.Single().InstanceId, Is.Zero);
        }

        private static Action<IArtifactEditingService>[] Commands() => new Action<IArtifactEditingService>[] {
            s => s.Place(100, new CellCoord(1, 0), Rotation4.R0),
            s => s.Move(0, new CellCoord(2, 0), Rotation4.R0),
            s => s.ReturnToPool(0), s => s.ExpandBoard(6, 6) };
        private static Fixture Initialized()
        {
            var f = Create(); f.Core.Artifacts.InitializeBoard(4, 4); return f;
        }
        private static void AssertSuccess(Fixture f, int artifactId, int instanceId)
        {
            var result = f.Buffers.ArtifactEditResultReadSink.LastEditResult.Value;
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.ArtifactId, Is.EqualTo(artifactId));
            Assert.That(result.InstanceId, Is.EqualTo(instanceId));
            Assert.That(f.Buffers.ArtifactPreviewReadSink.LastPreview, Is.Null);
        }
        private static Fixture Create(IBufferQuery buffers = null, ArtifactTable table = null, bool bind = true, IBattleInitializationSource battleSource = null)
        {
            var builder = new SystemBuilderHelper();
            if (buffers == null) BufferSystemFactory.Create().ExportServices(builder);
            else builder.AddService<IBufferQuery>(buffers);
            var core = InGameCoreSystemFactory.Create(battleSource);
            core.ExportServices(builder);
            var resolver = builder.BuildSystemReader().ServiceStoreReader;
            var f = new Fixture { Core = (IInGameCoreSystemGroupService)core, Buffers = resolver.GetService(BufferSystemKey.Buffer),
                Resolver = resolver, Table = table ?? new ArtifactTable(Definition(200, 1), Definition(100, 2)) };
            f.Database = new CharacterRosterDatabase(artifacts: f.Table);
            if (bind) { ((ISystemInitData)core).InitData(f.Database); ((ISystemPostBindService)core).PostBindService(resolver); }
            return f;
        }
        private static ArtifactDefinitionData Definition(int id, int count, bool healing = false)
        {
            // 실제 자산이 아닌 명시적인 테스트 정의로 기존 지원 효과만 검증한다.
            var effects = healing ? new[] { new ArtifactEffectData { entryId = "chalice", trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData { faction = ArtifactFactionFilter.Any },
                target = new ArtifactTargetData { type = ArtifactTargetType.MainCharacter, faction = ArtifactFactionFilter.Ally },
                effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal }, value = new ArtifactValueData { amount = 3 } } }
                : Array.Empty<ArtifactEffectData>();
            return new ArtifactDefinitionData(id, count, id == 200 ? new[] { CellCoord.Zero, new CellCoord(1, 0) } : new[] { CellCoord.Zero }, effects);
        }
        private static void PrepareBattle(IBufferQuery b, int phase)
        {
            b.CharacterReadSink.TryGetMainState(out var main);
            var owned = b.CharacterReadSink.Owned.Single();
            var facts = CharacterBufferTestData.MainFacts(owned.Id, main);
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput,
                phase == 0 ? EntityId.Empty : facts.EntityId, false, Array.Empty<StructureCombatFacts>(),
                Array.Empty<ThornFieldCellSnapshot>(), Array.Empty<CombatTimelineEntrySnapshot>());
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { facts }, CharacterBufferTestData.Combat(), commandState: state));
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.StagePhase,
                Is.EqualTo(phase == 0 ? StagePhase.Deployment : StagePhase.Combat));
            if (phase == 2)
                b.BattleWriteSink.CompleteBattle(new BattleCompletionCalculator(new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()))
                    .Complete(facts, main, new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                        ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true)));
        }
        private static CharacterCombatFacts WithActionCost(CharacterCombatFacts main)
        {
            var s = main.Stats;
            return CharacterBufferTestData.WithSnapshot(main, stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost,
                s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost, s.AttackCost,
                s.RemainingMoveRange, 4, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category));
        }
        private sealed class Fixture
        {
            internal IInGameCoreSystemGroupService Core;
            internal IBufferQuery Buffers;
            internal ISystemServiceStoreReader Resolver;
            internal CharacterRosterDatabase Database;
            internal ArtifactTable Table;
        }
        private sealed class ArtifactTable : IArtifactDefinitionTableQuery
        {
            private readonly ArtifactDefinitionData[] definitions;
            internal bool ThrowOnRead;
            internal int Lookups, AllReads;
            internal ArtifactTable(params ArtifactDefinitionData[] definitions) => this.definitions = definitions;
            public ArtifactDefinitionData GetDefinition(int id) => TryGetDefinition(id, out var data) ? data : throw new KeyNotFoundException();
            public bool TryGetDefinition(int id, out ArtifactDefinitionData data)
            {
                Lookups++; if (ThrowOnRead) throw new InvalidOperationException("테스트 DB 조회 실패");
                data = definitions.FirstOrDefault(d => d.ArtifactId == id); return data != null;
            }
            public IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions()
            {
                AllReads++; if (ThrowOnRead) throw new InvalidOperationException("테스트 DB 조회 실패"); return definitions;
            }
        }
        private sealed class BattleSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var initial = CharacterBattlePreparationTests.Initialization();
                var profiles = initial.Units.Select(u => new CombatAttackProfile(u,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
                return new BattleInitialization(initial.Stage, initial.MainAnchor, initial.MainRotation,
                    initial.Units, initial.Structures, initial.Categories, profiles);
            }
        }
        private sealed class CalculationProbe : IArtifactEditCalculation
        {
            private readonly IArtifactEditCalculation inner = new ArtifactEditCalculator();
            internal readonly List<string> Calls = new();
            internal int[] InitialDefinitionIds;
            public ArtifactState CreateInitialState(int w, int h, IReadOnlyList<ArtifactDefinition> d) { Calls.Add("Initialize"); InitialDefinitionIds = d.Select(x => x.ArtifactId).ToArray(); return inner.CreateInitialState(w, h, d); }
            public ArtifactEditProposal Place(ArtifactState s, int id, ArtifactDefinition d, CellCoord a, Rotation4 r) { Calls.Add("Place"); return inner.Place(s, id, d, a, r); }
            public ArtifactEditProposal Move(ArtifactState s, int id, ArtifactDefinition d, CellCoord a, Rotation4 r) { Calls.Add("Move"); return inner.Move(s, id, d, a, r); }
            public ArtifactEditProposal ReturnToPool(ArtifactState s, int id, ArtifactDefinition d) { Calls.Add("Return"); return inner.ReturnToPool(s, id, d); }
            public ArtifactEditProposal ExpandBoard(ArtifactState s, int w, int h, IReadOnlyList<ArtifactDefinition> d) { Calls.Add("Expand"); return inner.ExpandBoard(s, w, h, d); }
            public ArtifactPlacementPreview PreviewPlace(ArtifactState s, int id, ArtifactDefinition d, CellCoord a, Rotation4 r) { Calls.Add("PreviewPlace"); return inner.PreviewPlace(s, id, d, a, r); }
            public ArtifactPlacementPreview PreviewMove(ArtifactState s, int id, ArtifactDefinition d, CellCoord a, Rotation4 r) { Calls.Add("PreviewMove"); return inner.PreviewMove(s, id, d, a, r); }
        }
        private sealed class ThrowingEditSink : IArtifactEditWriteSink
        {
            internal int Calls;
            public void PublishEdit(ArtifactEditProposal proposal) { Calls++; throw new InvalidOperationException("테스트 게시 실패"); }
        }
        private sealed class EditFaultQuery : IBufferQuery
        {
            private readonly IBufferQuery inner;
            internal EditFaultQuery(IBufferQuery inner, IArtifactEditWriteSink writer) { this.inner = inner; ArtifactEditWriteSink = writer; }
            public IArtifactEditWriteSink ArtifactEditWriteSink { get; }
            public IArtifactBufferReadSink ArtifactReadSink => inner.ArtifactReadSink;
            public IArtifactInitializationWriteSink ArtifactInitializationWriteSink => inner.ArtifactInitializationWriteSink;
            public IArtifactPreviewWriteSink ArtifactPreviewWriteSink => inner.ArtifactPreviewWriteSink;
            public IArtifactEditResultReadSink ArtifactEditResultReadSink => inner.ArtifactEditResultReadSink;
            public IArtifactPreviewReadSink ArtifactPreviewReadSink => inner.ArtifactPreviewReadSink;
            public ICharacterPurchaseBufferWriteSink CharacterPurchaseWriteSink => inner.CharacterPurchaseWriteSink;
            public IChapterContinuationBufferWriteSink ChapterContinuationWriteSink => inner.ChapterContinuationWriteSink;
            public IChapterProgressBufferWriteSink ChapterProgressWriteSink => inner.ChapterProgressWriteSink;
            public ProjectL.Systems.RuntimeSystem.BufferSystems.Run.IRunProgressBufferReadSink RunProgressReadSink => inner.RunProgressReadSink;
            public IRunProgressBufferWriteSink RunProgressWriteSink => inner.RunProgressWriteSink;
            public IBattleBufferWriteSink BattleWriteSink => inner.BattleWriteSink;
            public IRecoveryStageBufferWriteSink RecoveryStageWriteSink => inner.RecoveryStageWriteSink;
            public IShopStageBufferWriteSink ShopStageWriteSink => inner.ShopStageWriteSink;
            public ICurrentProgressBufferReadSink CurrentProgressReadSink => inner.CurrentProgressReadSink;
            public ICurrentProgressBufferWriteSink CurrentProgressWriteSink => inner.CurrentProgressWriteSink;
            public IMapProgressBufferReadSink MapProgressReadSink => inner.MapProgressReadSink;
            public IMapProgressBufferWriteSink MapProgressWriteSink => inner.MapProgressWriteSink;
            public ICharacterBufferReadSink CharacterReadSink => inner.CharacterReadSink;
            public ICharacterBufferWriteSink CharacterWriteSink => inner.CharacterWriteSink;
            public ICombatStateBufferReadSink CombatStateReadSink => inner.CombatStateReadSink;
            public ICombatResultReadSink CombatResultReadSink => inner.CombatResultReadSink;
            public ICombatResultHistoryReadSink CombatResultHistoryReadSink => inner.CombatResultHistoryReadSink;
            public ICurrencyBufferReadSink CurrencyReadSink => inner.CurrencyReadSink;
            public ICurrencyBufferWriteSink CurrencyWriteSink => inner.CurrencyWriteSink;
            public IStageProgressBufferReadSink StageProgressReadSink => inner.StageProgressReadSink;
            public IStageProgressBufferWriteSink StageProgressWriteSink => inner.StageProgressWriteSink;
            public ICombatViewBufferReadSink CombatViewReadSink => inner.CombatViewReadSink;
            public ICombatViewBufferWriteSink CombatViewWriteSink => inner.CombatViewWriteSink;
            public ProjectL.Systems.RuntimeSystem.BufferSystems.StageView.IStageViewBufferReadSink StageViewReadSink => inner.StageViewReadSink;
            public ProjectL.Systems.RuntimeSystem.BufferSystems.StageView.IStageViewBufferWriteSink StageViewWriteSink => inner.StageViewWriteSink;
            public ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView.IArtifactViewBufferReadSink ArtifactViewReadSink => inner.ArtifactViewReadSink;
            public ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView.IArtifactViewBufferWriteSink ArtifactViewWriteSink => inner.ArtifactViewWriteSink;
        }
    }
}
