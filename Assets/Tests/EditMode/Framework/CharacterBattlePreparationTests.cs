using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class CharacterBattlePreparationTests
    {
        [TestCase(0, 1)]
        [TestCase(2, 3)]
        [TestCase(4, 4)]
        public void InitialDeal_StartJobCreatesMainPlusAtMostThreeLinkedSubs(int subs, int total)
        {
            var fixture = Create(subs);
            Assert.That(fixture.Buffers.CharacterReadSink.Characters, Is.Empty);
            fixture.Start();

            var characters = fixture.Buffers.CharacterReadSink.Characters;
            Assert.That(characters.Count, Is.EqualTo(total));
            Assert.That(characters.Count(x => x.OwnershipRole == CharacterOwnershipRole.Main), Is.EqualTo(1));
            foreach (var character in characters)
                Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwnedInstance(character.EntityId, out var id) &&
                    id == character.OwnedId, Is.True);
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(total - 1));
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(total + 1));
            Assert.That(fixture.Jobs.JobRunner.IsIdle, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void InvalidPlacementOrCapacity_ChangesNoBufferOrAllocator(bool invalidPlacement)
        {
            var fixture = Create(2);
            fixture.Source.Initialization = Initialization(
                invalidPlacement ? new CellCoord(0, 2) : CellCoord.Zero, invalidPlacement ? 8 : 1);
            Assert.Throws<InvalidOperationException>(() => fixture.Core.Combat.PrepareBattle());
            Assert.That(fixture.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Select(x => x.Id.Value), Is.EqualTo(new[] { 1, 2, 3 }));

            fixture.Source.Initialization = Initialization();
            fixture.Start();
            Assert.That(fixture.Buffers.CharacterReadSink.Characters[0].EntityId.Value, Is.EqualTo(1));
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.DrawCursor, Is.EqualTo(2));
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(4));
        }

        [Test]
        public void AlreadyLinkedCandidate_RejectsBeforeDrawCursorAdvance()
        {
            var fixture = Create(4);
            fixture.Start();
            var before = fixture.Buffers.CombatStateReadSink.Current;
            Assert.Throws<InvalidOperationException>(() => fixture.Core.Combat.PrepareBattle());
            Assert.That(fixture.Buffers.CombatStateReadSink.Current, Is.SameAs(before));
            Assert.That(before.DrawCursor, Is.EqualTo(3));
            Assert.That(fixture.Source.ReadCount, Is.EqualTo(1));
        }

        [Test]
        public void EnemiesAndStructures_HaveNoOwnedLinkWhenNumbersMatch()
        {
            var fixture = Create(4, withEnemies: true);
            fixture.Start();
            var enemy = fixture.Buffers.CharacterReadSink.Characters.Single(x => x.Owner == Owner.Enemy);
            var structure = fixture.Buffers.CombatStateReadSink.Current.Structures.Single();
            Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwned(new CharacterInstanceId(enemy.EntityId.Value), out _), Is.True);
            Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwned(new CharacterInstanceId(structure.EntityId.Value), out _), Is.True);
            Assert.That(enemy.OwnedId, Is.Null);
            Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwnedInstance(enemy.EntityId, out _), Is.False);
            Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwnedInstance(structure.EntityId, out _), Is.False);
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(7));
        }

        [Test]
        public void LinkSurvivesDeployDeathAndReturn_UntilBattleCleanup()
        {
            var fixture = Create(1);
            fixture.Start();
            var first = fixture.Buffers.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Sub);
            CharacterCombatFacts current = first.ToFacts();
            foreach (var area in new[] { CharacterRuntimeArea.Field, CharacterRuntimeArea.Hand,
                CharacterRuntimeArea.Field, CharacterRuntimeArea.Excluded })
            {
                var next = Relocate(current, area);
                fixture.Buffers.BattleWriteSink.Apply(new CharacterChanges(
                    new[] { new CharacterStateChange(current, next) }, Array.Empty<SoulCountChange>()));
                Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwnedInstance(first.EntityId, out var link), Is.True);
                Assert.That(link, Is.EqualTo(first.OwnedId.Value));
                current = next;
            }

            fixture.Buffers.BattleWriteSink.EndBattle();
            Assert.That(fixture.Buffers.CharacterReadSink.TryGetOwnedInstance(first.EntityId, out _), Is.False);
            Assert.That(fixture.Buffers.CharacterReadSink.Owned.Count, Is.EqualTo(2));
            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void StartPreparation_UsesBufferStateWithoutLegacyFactory()
        {
            var fixture = Create(1);
            var mainId = fixture.Buffers.CharacterReadSink.Owned[0].Id;
            fixture.Buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(mainId, 0, 2) }));
            fixture.Start();
            var main = fixture.Buffers.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main);
            Assert.That(main.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(main.Health.MaxHealth, Is.EqualTo(12));
            Assert.That(main.Stats.EffectiveAttackPower, Is.EqualTo(7));
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.BoardWidth, Is.EqualTo(8));
            Assert.That(fixture.Source.LastDatabase, Is.SameAs(fixture.Database));

            var job = new CombatPrepareJob(new InGameServices(fixture.Core), 0);
            fixture.Buffers.BattleWriteSink.EndBattle();
            job.Execute();
            var before = fixture.Buffers.CombatStateReadSink.Current;
            job.Execute();
            Assert.That(job.IsCompleted, Is.True);
            Assert.That(fixture.Buffers.CombatStateReadSink.Current, Is.SameAs(before));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void BufferRejectsInvalidAllocatorOrDrawCursor_BeforeEitherStoreInstalls(bool allocator)
        {
            var fixture = Create(1);
            fixture.Start();
            var characters = fixture.Buffers.CharacterReadSink.Characters.Select(x => x.ToFacts()).ToArray();
            var state = fixture.Buffers.CombatStateReadSink.Current;
            var combat = new CombatPreparation(8, 6, 8, 100, 42, CombatFlowPhase.PlayerInput,
                state.DrawOrder, allocator ? 1 : 0, 2, 2);
            fixture.Buffers.BattleWriteSink.EndBattle();

            Assert.Throws<InvalidOperationException>(() => fixture.Buffers.BattleWriteSink.PrepareBattle(
                new BattlePreparation(characters, combat, nextEntityId: allocator ? 2 : 3)));
            Assert.That(fixture.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
            fixture.Start();
            Assert.That(fixture.Buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(3));
        }

        [Test]
        public void MissingProductionSource_RejectsWithoutPublishingBattle()
        {
            var fixture = Create(1, missingSource: true);
            var error = Assert.Throws<InvalidOperationException>(() => fixture.Core.Combat.PrepareBattle());
            Assert.That(error.Message, Does.Contain("G7"));
            Assert.That(fixture.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(fixture.Buffers.CombatStateReadSink.HasBattle, Is.False);
        }

        private static CharacterCombatFacts Relocate(CharacterCombatFacts source, CharacterRuntimeArea area)
        {
            bool field = area == CharacterRuntimeArea.Field;
            return new CharacterCombatFacts(source.EntityId, source.OwnedId, source.CharacterKey,
                source.Owner, source.OwnershipRole, area,
                new HealthStateSnapshot(area == CharacterRuntimeArea.Excluded ? 0 : source.Health.CurrentHealth, source.Health.MaxHealth),
                source.Stats, area == CharacterRuntimeArea.Hand ? (int?)0 : null,
                field ? (CellCoord?)new CellCoord(1, 0) : null, field ? (Rotation4?)Rotation4.R0 : null,
                field ? new[] { new CellCoord(1, 0) } : Array.Empty<CellCoord>(),
                new CellCoord(1, 0), 0, 0);
        }

        private static Fixture Create(int subs, bool withEnemies = false, bool missingSource = false)
        {
            var fixture = new Fixture();
            fixture.Source.Initialization = Initialization(withEnemies: withEnemies);
            var core = InGameCoreSystemFactory.Create(missingSource ? null : fixture.Source);
            fixture.Core = (IInGameCoreSystemGroupService)core;
            var root = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(root);
            root.AddService<IInGameSystemGroupService>(new InGameServices(fixture.Core));
            TestLifeCycleGroup.Add(root);
            var jobs = JobSystemFactory.Create();
            jobs.ExportServices(root);
            var resolver = root.BuildSystemReader().ServiceStoreReader;
            fixture.Buffers = resolver.GetService(BufferSystemKey.Buffer);
            fixture.Jobs = resolver.GetService(SystemGroupsService.Job);
            fixture.JobTick = (ISystemTick)jobs;
            ((ISystemInitData)core).InitData(fixture.Database);
            ((ISystemPostBindService)core).PostBindService(resolver);
            ((ISystemPostBindService)jobs).PostBindService(resolver);
            ((ISystemInitialize)jobs).Initialize();
            fixture.Buffers.CharacterWriteSink.Acquire(Enumerable.Range(0, subs + 1).Select(i =>
                new RosterAcquireRequest(i == 0 ? EntityKey.Unit_MainCharacter : EntityKey.Unit_Akimbo,
                    i == 0 ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub)).ToArray(),
                CharacterBufferTestData.MainState(health: 7));
            if (!missingSource)
            {
                fixture.Buffers.MapProgressWriteSink.RegisterMap(1);
                fixture.Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                    new ProjectL.Systems.RuntimeSystem.BufferSystems.Stage.StageProgressData(
                        2, 101, 1, 0, Array.Empty<int>()) });
                fixture.Buffers.CurrentProgressWriteSink.SelectMap(1);
                fixture.Buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Compatibility fixture");
            }
            return fixture;
        }

        internal static BattleInitialization Initialization(CellCoord? anchor = null, int capacity = 8,
            bool withEnemies = false)
        {
            var stage = new StageConfig(8, 6, capacity, 2, 2, 42,
                withEnemies ? new[] { new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(1, 4), Rotation4.R0) }
                    : Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), 100,
                withEnemies ? new[] { new InitialEnemyStructurePlacementSpec(EntityKey.Structure_Headquarters, new CellCoord(2, 4), Rotation4.R0) }
                    : Array.Empty<InitialEnemyStructurePlacementSpec>());
            return new BattleInitialization(stage, anchor ?? CellCoord.Zero, Rotation4.R0,
                new[] { Unit(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly),
                    Unit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly) },
                withEnemies ? new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                    new[] { CellCoord.Zero }, 20, false, 0) } : Array.Empty<StructureSpec>(),
                new[]
                {
                    new CategorySpec(CategoryKey.Base, Array.Empty<CategoryRuleSpec>()),
                    new CategorySpec(CategoryKey.Rider, new CategoryRuleSpec[] { new ManeuverRefundCategoryRuleSpec() }),
                    new CategorySpec(CategoryKey.Heavy, new CategoryRuleSpec[] { new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0) }),
                    new CategorySpec(CategoryKey.Cannon, new CategoryRuleSpec[] { new SafeBombardmentCategoryRuleSpec() })
                });
        }

        private static UnitSpec Unit(EntityKey key, UnitRole role, UnitOwnerEligibility eligibility) =>
            new(key, role, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 3, 1,
                role == UnitRole.MainCharacter ? 1 : 2, CategoryKey.Base,
                5, 4, 1, 0, 1, 1, true, Array.Empty<TraitSpec>(), eligibility);

        private sealed class Fixture
        {
            internal readonly CharacterRosterDatabase Database = new();
            internal readonly DefinitionSource Source = new();
            internal IInGameCoreSystemGroupService Core;
            internal IBufferQuery Buffers;
            internal IJobSystemServiceContext Jobs;
            internal ISystemTick JobTick;

            internal void Start()
            {
                Jobs.JobRegister.Register(JobRootType.Combat, JobType.Start);
                JobTick.Tick(0);
            }
        }

        private sealed class DefinitionSource : IBattleInitializationSource
        {
            internal BattleInitialization Initialization;
            internal IDataBaseQueryContext LastDatabase;
            internal int ReadCount;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                LastDatabase = database;
                ReadCount++;
                return Initialization;
            }
        }

        private sealed class InGameServices : IInGameSystemGroupService
        {
            public IInGameCoreSystemGroupService CoreGroup { get; }
            public IInGameViewSystemGroupService ViewGroup => null;

            internal InGameServices(IInGameCoreSystemGroupService core)
            {
                CoreGroup = core;
            }
        }
    }
}
