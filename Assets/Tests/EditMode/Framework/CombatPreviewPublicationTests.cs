using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class CombatPreviewPublicationTests
    {
        [Test]
        public void Service_PublishesMatchingPreviewOnly_WithoutGameplayOrCommandResultWrites()
        {
            var b = Create(out var service);
            var original = Main(b);
            int request = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(original.EntityId,
                AttackAim.ForCell(new CellCoord(2, 1))));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.RequestNumber, Is.EqualTo(request));
            Assert.That(preview.Attack.Value.CanAttack, Is.True);
            Assert.That(preview.Attack.Value.Targets.Span[0].ExpectedDamage, Is.EqualTo(5));
            Assert.That(b.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(request));
            Assert.That(Main(b).Health, Is.EqualTo(original.Health));
            Assert.That(Main(b).Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.Current.Structures.Single().Health.CurrentHealth, Is.EqualTo(20));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.Zero);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.Zero);
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.CombatStateReadSink.SoulField.Entries, Is.Empty);
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.False);
            Assert.That(b.CombatViewReadSink.HasPreview, Is.True);
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.Zero);
        }

        [Test]
        public void SameAimReissuedAfterApChange_RecalculatesAndOldResponseDoesNotMatch()
        {
            var b = Create(out var service);
            var data = CombatViewPreviewData.Attack(Main(b).EntityId, AttackAim.ForCell(new CellCoord(2, 1)));
            int first = b.CombatViewWriteSink.SetPreview(data);
            service.ProcessLatestPreview();
            var original = Main(b); var s = original.Stats;
            var final = CharacterBufferTestData.WithSnapshot(original, stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange,
                s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost,
                s.AttackCost, s.RemainingMoveRange, 0, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category));
            var input = CombatCommandPublicationTests.Input(b);
            b.BattleWriteSink.Apply(new CombatCommandTransition(input, true, "",
                new CharacterChanges(new[] { new CharacterStateChange(original, final) }, Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None)));
            b.CombatViewWriteSink.ClearPreview();
            Assert.That(b.CombatViewReadSink.HasPreview, Is.False);
            int second = b.CombatViewWriteSink.SetPreview(data);
            b.CombatStateReadSink.TryGetLatestPreview(out var old);
            Assert.That(old.RequestNumber, Is.EqualTo(first));
            Assert.That(old.RequestNumber, Is.Not.EqualTo(second));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            b.CombatStateReadSink.TryGetLatestPreview(out var current);
            Assert.That(current.RequestNumber, Is.EqualTo(second));
            Assert.That(current.Attack.Value.FailureReason, Is.EqualTo(AttackFailureReason.ActionCostInsufficient));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(original.EntityId));
            service.ProcessLatestPreview();
            b.CombatStateReadSink.TryGetLatestPreview(out var range);
            Assert.That(range.Range.Value.RangeCells.Length, Is.GreaterThan(0));
            Assert.That(range.Range.Value.CanAffordAttack, Is.False);
        }

        [Test]
        public void ReplacementClearAndBattleExit_KeepPreviewIdentitySeparateFromGameplayResults()
        {
            var b = Create(out var service);
            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(Main(b).EntityId));
            int latest = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(Main(b).EntityId,
                AttackAim.ForCell(new CellCoord(99, 99))));
            service.ProcessLatestPreview();
            b.CombatStateReadSink.TryGetLatestPreview(out var preview);
            Assert.That(preview.RequestNumber, Is.EqualTo(latest));
            Assert.That(preview.Attack.Value.CanAttack, Is.False);
            b.CombatViewWriteSink.ClearPreview();
            Assert.That(service.ProcessLatestPreview(), Is.False);
            Assert.That(b.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(latest));
            b.BattleWriteSink.EndBattle();
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            Assert.That(service.ProcessLatestPreview(), Is.False);
        }

        internal static IBufferQuery Create(out CombatProcessingService service)
            => Create(out service, Array.AsReadOnly(new IMovementTileRule[] { new ThornMovementTileRule() }));

        internal static IBufferQuery Create(out CombatProcessingService service, System.Collections.Generic.IReadOnlyList<IMovementTileRule> rules,
            CellCoord[] wallCells = null, CellCoord? targetCell = null)
        {
            var b = CharacterBattleSettlementTests.Create();
            var original = Main(b); var s = original.Stats;
            var main = CharacterBufferTestData.WithSnapshot(original, stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange,
                s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost,
                s.AttackCost, s.RemainingMoveRange, 4, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category));
            var target = targetCell ?? new CellCoord(2, 1);
            var walls = (wallCells ?? Array.Empty<CellCoord>()).Select((c, i) => new StaticTerrainFact(71 + i, c,
                StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy })).ToArray();
            var environment = new MovementEnvironmentFacts(walls);
            var bindings = new EnvironmentSemanticBindings(walls.Select(w => w.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>());
            var hq = new StructureCombatFacts(new EntityId(2), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 20, false, 0), new HealthStateSnapshot(20, 20),
                target, Rotation4.R0, new[] { target });
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, main.EntityId, false,
                new[] { hq }, Array.Empty<ThornFieldCellSnapshot>(),
                new[] { CombatTimelineEntrySnapshot.ForUnit(main.EntityId, 1, 0, true) }, environment: environment);
            b.BattleWriteSink.EndBattle();
            var combat = new CombatPreparation(8, 6, 4, 100, 73, CombatFlowPhase.PlayerInput, Array.Empty<CharacterInstanceId>(),
                environment: environment, environmentBindings: bindings);
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, combat, new[] { hq }, commandState: state));
            var source = CharacterBattlePreparationTests.Initialization();
            var definition = new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation, source.Units,
                source.Structures, source.Categories, source.Units.Select(u => new CombatAttackProfile(u,
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray());
            service = new CombatProcessingService(new Source(definition), new Roster(), rules);
            service.InitData(new CharacterRosterDatabase());
            var root = new SystemBuilderHelper(); root.AddService<IBufferQuery>(b);
            service.PostBindService(root.BuildSystemReader().ServiceStoreReader);
            return b;
        }
        private static CharacterCombatFacts Main(IBufferQuery b) => b.CharacterReadSink.Characters.Single().ToFacts();
        private sealed class Source : IBattleInitializationSource
        {
            private readonly BattleInitialization _definition;
            internal Source(BattleInitialization definition) { _definition = definition; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey) => _definition;
        }
        private sealed class Roster : ICharacterRosterService
        {
            public RosterAcquireResult InitializeRoster(int key) => throw new NotSupportedException();
            public RosterAcquireResult AcquireSub(EntityKey key) => throw new NotSupportedException();
            public InitialDrawPlan PlanInitialDraw(int seed) => throw new NotSupportedException();
        }
    }
}
