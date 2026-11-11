using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Fame;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyDecisionSourceStampTests
    {
        [Test]
        public void UnchangedReadIsCurrentWithoutReassembly()
        {
            using var source = new SourceFixture();
            var input = source.Assembler.AssembleDecision(source.Request, out var stamp);
            Assert.That(input, Is.Not.Null);
            var before = source.Battle.CaptureStores();
            for (int i = 0; i < 100; i++) Assert.That(source.Assembler.IsCurrent(stamp), Is.True);
            Assert.That(source.Battle.CaptureStores(), Is.EqualTo(before));
        }

        [TestCase("Gold")]
        [TestCase("Fame")]
        [TestCase("Inventory")]
        [TestCase("CharacterFacts")]
        [TestCase("CombatStateAndResult")]
        [TestCase("ArtifactConfiguration")]
        [TestCase("InstalledDefinitions")]
        public void ChangedDependencyInvalidates(string dependency)
        {
            using var source = new SourceFixture();
            source.Assembler.AssembleDecision(source.Request, out var stamp);
            var b = source.Battle.Buffers;
            switch (dependency)
            {
                case "Gold": b.CurrencyWriteSink.SetAmount(b.CurrencyReadSink.Amount + 1); break;
                case "Fame":
                    var fame = (IFameBufferStore)b.FameReadSink;
                    fame.InstallAmount(fame.PrepareAmount(b.FameReadSink.Amount, b.FameReadSink.Amount + 1));
                    break;
                case "Inventory":
                    var items = (IItemInventoryStore)b.ItemInventoryReadSink;
                    items.Install(items.Prepare(new ItemInventoryChange(b.ItemInventoryReadSink.Current,
                        new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 1 }))));
                    break;
                case "CharacterFacts":
                    var actor = source.Battle.Actor;
                    b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor,
                        HeldEnvironmentEffects.AcquireBadge(actor, 91)) }, Array.Empty<SoulCountChange>()));
                    break;
                case "CombatStateAndResult": Assert.That(source.Battle.Core.CombatAdvance.AdvanceCombat(), Is.True); break;
                case "ArtifactConfiguration": source.Artifacts.InitData(new CoreIntegrationTestData(ConfiguredMovementBattle.Config)); break;
                case "InstalledDefinitions": source.Definitions.Value = source.Definitions.Read(source.Battle.Database, 1, new StageKey(1101)); break;
                default: throw new ArgumentOutOfRangeException(nameof(dependency));
            }
            Assert.That(source.Assembler.IsCurrent(stamp), Is.False, dependency);
        }

        [Test]
        public void SourceChangesDuringAssemblyDiscardCapture()
        {
            using var source = new SourceFixture();
            int calls = 0;
            source.Definitions.OnRead = () =>
            {
                if (++calls == 3) source.Battle.Buffers.CurrencyWriteSink.SetAmount(999);
            };
            Assert.That(source.Assembler.AssembleDecision(source.Request, out var stamp), Is.Null);
            Assert.That(stamp, Is.Null);
        }

        [Test]
        public void PreviewAnswerDoesNotInvalidateWorld()
        {
            using var source = new SourceFixture();
            source.Assembler.AssembleDecision(source.Request, out var stamp);
            source.Battle.Buffers.CombatViewWriteSink.SetPreview(
                ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView.CombatViewPreviewData.Move(
                    source.Battle.Actor.EntityId, new MovementPath(new[] { new CellCoord(0, 1), new CellCoord(1, 1) })));
            source.Battle.Core.CombatPreview.ProcessLatestPreview();
            Assert.That(source.Assembler.IsCurrent(stamp), Is.True);
        }

        [Test]
        public void ValidationDoesNotHashOrCopyBoard()
        {
            using var source = new SourceFixture();
            var input = source.Assembler.AssembleDecision(source.Request, out var stamp);
            var facts = source.Battle.Buffers.CharacterReadSink.Characters[0].ToFacts();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < 10000; i++)
                if (!source.Assembler.IsCurrent(stamp)) Assert.Fail("Unchanged source invalidated.");
            TestContext.WriteLine($"validation10000ms={(Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency:F4}");
            Assert.That(source.Battle.Buffers.CharacterReadSink.Characters[0].ToFacts(), Is.SameAs(facts));
            Assert.That(source.Assembler.IsCurrent(stamp), Is.True);
        }

        [TestCase("BattleId")]
        [TestCase("QueryRevision")]
        [TestCase("ResultSequence")]
        [TestCase("BattleEnded")]
        [TestCase("SoulField")]
        [TestCase("StateAndMembership")]
        [TestCase("RosterRevision")]
        [TestCase("OwnedRecord")]
        [TestCase("MainState")]
        [TestCase("SoulCount")]
        [TestCase("CharacterIdentity")]
        [TestCase("CharacterCount")]
        [TestCase("ArtifactSnapshot")]
        public void IndependentDependencyInvalidatesWithoutQueryRevisionChange(string changed)
        {
            using var f = new SourceFixture();
            var b = f.Battle.Buffers;
            var stamp = new EnemyDecisionSourceStamp(b.CombatStateReadSink, b.CharacterReadSink, f.Definitions,
                f.Artifacts, b.ArtifactReadSink.Current, b.ItemInventoryReadSink.Current, b.CurrencyReadSink.Amount, b.FameReadSink.Amount);
            var combat = new CombatRead(b.CombatStateReadSink, changed);
            var characters = new CharacterRead(b.CharacterReadSink, changed);
            var artifacts = changed == "ArtifactSnapshot" ? new ArtifactBufferSnapshot(new ArtifactState(2, 2,
                Array.Empty<ArtifactItemState>(), Array.Empty<ArtifactItemState>())) : b.ArtifactReadSink.Current;
            Assert.That(stamp.Matches(combat, characters, f.Definitions, f.Artifacts, artifacts,
                b.ItemInventoryReadSink.Current, b.CurrencyReadSink.Amount, b.FameReadSink.Amount), Is.False, changed);
        }

        [Test]
        public void RepeatedValidationAllocationProbeIsCalibrated()
        {
            using var f = new SourceFixture();
            f.Assembler.AssembleDecision(f.Request, out var stamp);
            for (int i = 0; i < 100; i++) f.Assembler.IsCurrent(stamp);
            long calibrationStart = GC.GetAllocatedBytesForCurrentThread();
            var allocation = new byte[4096];
            long calibration = GC.GetAllocatedBytesForCurrentThread() - calibrationStart;
            GC.KeepAlive(allocation);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) f.Assembler.IsCurrent(stamp);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            TestContext.WriteLine($"validationAllocation: calibration={calibration}, bytes={bytes}, supported={calibration >= 4096}");
            if (calibration >= 4096) Assert.That(bytes, Is.LessThanOrEqualTo(128));
        }

        [Test]
        public void ChangedArtifactDefinitionInvalidatesInsideSameDatabaseContext()
        {
            using var f = new SourceFixture();
            var b = f.Battle.Buffers;
            var database = new ArtifactDatabase();
            f.Artifacts.InitData(database);
            var artifacts = new ArtifactBufferSnapshot(new ArtifactState(2, 2, Array.Empty<ArtifactItemState>(),
                new[] { new ArtifactItemState(1, 1, true, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) }));
            var stamp = new EnemyDecisionSourceStamp(b.CombatStateReadSink, b.CharacterReadSink, f.Definitions,
                f.Artifacts, artifacts, b.ItemInventoryReadSink.Current, b.CurrencyReadSink.Amount, b.FameReadSink.Amount);
            bool Matches() => stamp.Matches(b.CombatStateReadSink, b.CharacterReadSink, f.Definitions, f.Artifacts,
                artifacts, b.ItemInventoryReadSink.Current, b.CurrencyReadSink.Amount, b.FameReadSink.Amount);
            Assert.That(Matches(), Is.True);
            database.Definition = new ArtifactDefinitionData(1, 2, new[] { CellCoord.Zero }, Array.Empty<ArtifactEffectData>());
            Assert.That(Matches(), Is.False);
        }

        private sealed class ArtifactDatabase : IDataBaseQueryContext, IInGameQuery, IArtifactDefinitionTableQuery
        {
            internal ArtifactDefinitionData Definition = new(1, 1, new[] { CellCoord.Zero }, Array.Empty<ArtifactEffectData>());
            public IInGameQuery InGameQuery => this;
            public IRenderQuery RenderQuery => throw new NotSupportedException();
            public IViewQuery ViewQuery => throw new NotSupportedException();
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => this;
            public ArtifactDefinitionData GetDefinition(int id) => id == 1 ? Definition : throw new ArgumentOutOfRangeException(nameof(id));
            public bool TryGetDefinition(int id, out ArtifactDefinitionData definition) { definition = id == 1 ? Definition : null; return definition != null; }
            public IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions() => new[] { Definition };
            public IStageMapTableQuery StageMapTableQuery => throw new NotSupportedException();
            public IStageTableQuery StageTableQuery => throw new NotSupportedException();
            public IFieldTableQuery FieldTableQuery => throw new NotSupportedException();
            public IUnitTableQuery UnitTableQuery => throw new NotSupportedException();
            public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
            public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => throw new NotSupportedException();
            public IUnitRosterTableQuery UnitRosterTableQuery => throw new NotSupportedException();
            public ICategoryConfigTableQuery CategoryConfigTableQuery => throw new NotSupportedException();
            public IStageEconomyTableQuery StageEconomyTableQuery => throw new NotSupportedException();
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => throw new NotSupportedException();
        }

        private sealed class CombatRead : ICombatStateBufferReadSink
        {
            private readonly ICombatStateBufferReadSink _source;
            private readonly string _changed;
            internal CombatRead(ICombatStateBufferReadSink source, string changed) { _source = source; _changed = changed; }
            public bool HasBattle => _changed != "BattleEnded" && _source.HasBattle;
            public long BattleInstanceId => _source.BattleInstanceId + (_changed == "BattleId" ? 1 : 0);
            public long QueryRevision => _source.QueryRevision + (_changed == "QueryRevision" ? 1 : 0);
            public int LatestResultSequence => _source.LatestResultSequence + (_changed == "ResultSequence" ? 1 : 0);
            public CombatStateData Current => _changed == "StateAndMembership"
                ? new CombatStateData(_source.Current, _source.Current.CommandState) : _source.Current;
            public SoulFieldSnapshot SoulField => _changed == "SoulField"
                ? new SoulFieldSnapshot(99, Array.Empty<SoulFieldFacts>()) : _source.SoulField;
            public CombatFlowPhase Phase => _source.Phase;
            public BattleCompletionChanges Completion => _source.Completion;
            public int HandledCommandNumber => _source.HandledCommandNumber;
            public int AnsweredPreviewNumber => _source.AnsweredPreviewNumber;
            public bool TryGetLatestPreview(out CombatPreviewResult preview) => _source.TryGetLatestPreview(out preview);
        }

        private sealed class CharacterRead : ICharacterBufferReadSink
        {
            private readonly ICharacterBufferReadSink _source;
            private readonly string _changed;
            internal CharacterRead(ICharacterBufferReadSink source, string changed) { _source = source; _changed = changed; }
            public long RosterRevision => _source.RosterRevision + (_changed == "RosterRevision" ? 1 : 0);
            public IReadOnlyList<OwnedCharacterRecord> Owned => _source.Owned;
            public IReadOnlyList<CharacterData> Characters => _changed == "CharacterCount" ? Array.Empty<CharacterData>() :
                _changed == "CharacterIdentity" ? _source.Characters.Select(c => new CharacterData(c.ToFacts())).ToArray() : _source.Characters;
            public bool TryGet(EntityId id, out CharacterData data) => _source.TryGet(id, out data);
            public bool TryGetOwned(CharacterInstanceId id, out OwnedCharacterRecord record)
            {
                bool found = _source.TryGetOwned(id, out record);
                if (found && _changed == "OwnedRecord") record = record.WithGrowth(record.Growth);
                return found;
            }
            public int GetSoulCount(CharacterInstanceId id) => _source.GetSoulCount(id) + (_changed == "SoulCount" ? 1 : 0);
            public bool TryGetMainState(CharacterInstanceId id, out MainCharacterPersistentState state)
            {
                bool found = _source.TryGetMainState(id, out state);
                if (found && _changed == "MainState") state = state.With(persistentAttack: state.PersistentAttack + 1);
                return found;
            }
            public bool TryGetOwnedInstance(EntityId id, out CharacterInstanceId ownedId) => _source.TryGetOwnedInstance(id, out ownedId);
        }

        internal sealed class SourceFixture : IDisposable
        {
            internal ConfiguredMovementBattle Battle { get; }
            internal MutableDefinitions Definitions { get; }
            internal ArtifactLoadoutReader Artifacts { get; } = new();
            internal CombatInputAssembler Assembler { get; }
            internal ISystemServiceStoreReader Reader { get; }
            internal CombatActionRequest Request => new(0, CombatCommandKind.CompleteEnemyTurn, default, default,
                processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: Battle.Buffers.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: Battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence);

            internal SourceFixture(bool hiddenTerrain = false, bool startEnemy = true, bool pendingRoar = false)
            {
                Definitions = new MutableDefinitions(hiddenTerrain);
                var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
                if (pendingRoar)
                {
                    database.SetTargetingMethod(new TargetingMethodData(2, TargetingShape.RadialSquare,
                        TargetingChoiceMode.All, TargetingSplash.None, 0));
                    database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                        1, 1, 100, 0, 1, CategoryKey.Boss, 1, 1, new UnitTraitData[] { new RoarTraitData(3) },
                        targetingMethodKey: 2, behavior: new UnitBehaviorSpec(false, false, false), killReward: UnitKillRewardSpec.Excluded));
                }
                Battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, Definitions, database: database);
                Battle.Submit(Battle.EndPlayerTurn());
                if (startEnemy) Assert.That(Battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                if (pendingRoar)
                {
                    var boss = Battle.Character(EntityKey.Unit_Demon);
                    var statuses = boss.Stats.TraitStatuses.ToArray().Select(s => s is RoarTraitStatusSnapshot
                        ? new RoarTraitStatusSnapshot(3, 3) : s).ToArray();
                    Battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(boss,
                        boss.With(stats: boss.Stats.With(traitStatuses: statuses))) }, Array.Empty<SoulCountChange>()));
                }
                var builder = new SystemBuilderHelper();
                builder.AddService<IBufferQuery>(Battle.Buffers);
                Reader = builder.BuildSystemReader().ServiceStoreReader;
                Artifacts.InitData(Battle.Database);
                Artifacts.PostBindService(Reader);
                Assembler = new CombatInputAssembler(Definitions, Artifacts);
                Assembler.PostBindService(Reader);
            }
            public void Dispose() => Battle.Dispose();
        }

        internal sealed class MutableDefinitions : IBattleInitializationSource, IBattleDefinitionRead
        {
            internal BattleInitialization Value;
            internal Action OnRead;
            private readonly bool _hiddenTerrain;
            internal MutableDefinitions(bool hiddenTerrain = false) => _hiddenTerrain = hiddenTerrain;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var d = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                Value = !_hiddenTerrain ? d : new BattleInitialization(d.Stage, d.MainAnchor, d.MainRotation, d.Units,
                    d.Structures, d.Categories, d.AttackProfiles,
                    new MovementEnvironmentFacts(new[] { new StaticTerrainFact(91, new CellCoord(0, 3),
                        StaticTerrainKind.Sandstorm, true, new[] { Owner.Player }) }),
                    new EnvironmentSemanticBindings(new long[] { 91 }, Array.Empty<long>(), Array.Empty<EntityId>()),
                    d.ObstaclePlacements, d.WallBlocking, d.ObjectiveKind, d.ItemDefinitions,
                    d.Progression, d.InitialRandomTornadoPlacements, d.ClearRewards, d.TraitGrowth, d.NexusReinforcements);
                return Value;
            }
            public BattleInitialization Require(long battleInstanceId)
            {
                OnRead?.Invoke();
                return Value;
            }
        }
    }
}
