using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime.LifeCycle;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;
using ProjectL.Systems.ServiceKeys;
using UnityEditor;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 아티팩트 보드 처음 크기 설정 테이블과, 실행 계층(StageLifeCycle)이 그 값으로 초기화 Job을 등록하는지 확인합니다(계획 Phase A).
    /// </summary>
    public sealed class ArtifactBoardConfigTests
    {
        private const string CatalogPath = "Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset";

        private readonly DataBaseTestAssets _assets = new();

        [TearDown]
        public void TearDown()
        {
            _assets.DestroyAll();
        }

        #region 설정 테이블

        [Test]
        public void Table_WithOneItem_ExposesConfiguredSize()
        {
            var table = new ArtifactBoardConfigTable(_assets.Table<ArtifactBoardConfigDataTableSO>(Item(4, 5)));

            Assert.That(table.Config.InitialWidth, Is.EqualTo(4));
            Assert.That(table.Config.InitialHeight, Is.EqualTo(5));
        }

        [Test]
        public void Table_WithoutItem_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ArtifactBoardConfigTable(_assets.Table<ArtifactBoardConfigDataTableSO>()));
        }

        [Test]
        public void Table_WithTwoItems_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ArtifactBoardConfigTable(_assets.Table<ArtifactBoardConfigDataTableSO>(Item(4, 4), Item(5, 5))));
        }

        [TestCase(0, 4)]
        [TestCase(4, 0)]
        public void Table_WithInvalidSize_Throws(int width, int height)
        {
            Assert.Throws<InvalidOperationException>(() =>
                new ArtifactBoardConfigTable(_assets.Table<ArtifactBoardConfigDataTableSO>(Item(width, height))));
        }

        #endregion

        #region StageLifeCycle 진입 등록

        [Test]
        public void Enter_RegistersMapPreparationAndBoardInitializationWithConfiguredSize()
        {
            var artifacts = new ArtifactsSpy();
            var register = new RegisterSpy();
            var runner = new RunnerSpy();
            StageLifeCycle stage = CreateStage(new ConfigDatabase(4, 5), register, runner, artifacts);

            stage.RegisterEntryJobs();

            Assert.That(register.Calls, Is.EqualTo(new[] { (JobRootType.Stage, JobType.Start) }));
            Assert.That(runner.Jobs.Count, Is.EqualTo(1));
            Assert.That(runner.Jobs[0].Priority, Is.EqualTo(ArtifactPlan.BoardInitializationPriority));
            runner.Jobs[0].Execute();
            Assert.That((artifacts.Width, artifacts.Height), Is.EqualTo((4, 5)));
        }

        [Test]
        public void Enter_WithoutRoster_RegistersNewRunInsteadOfStart()
        {
            // 로스터가 없는 첫 진입(타이틀 없이 StageSelect로 시작)은 새 Run부터 시작한다.
            var register = new RegisterSpy();
            var runner = new RunnerSpy();
            StageLifeCycle stage = CreateStage(new ConfigDatabase(4, 5), register, runner, new ArtifactsSpy(), hasRoster: false);

            stage.RegisterEntryJobs();

            Assert.That(register.Calls, Is.EqualTo(new[] { (JobRootType.Stage, JobType.NewRun) }));
            Assert.That(runner.Jobs.Count, Is.EqualTo(1));
        }

        [Test]
        public void InitData_WithoutConfig_Passes_AndOnlyEnterThrowsWithoutRegistering()
        {
            var register = new RegisterSpy();
            var runner = new RunnerSpy();
            StageLifeCycle stage = CreateStage(new NoConfigDatabase(), register, runner, new ArtifactsSpy());

            Assert.That(stage.ArtifactBoardConfig, Is.Null);
            Assert.Throws<InvalidOperationException>(stage.RegisterEntryJobs);
            Assert.That(register.Calls, Is.Empty);
            Assert.That(runner.Jobs, Is.Empty);
        }

        [Test]
        public void Enter_BeforeBinding_Throws()
        {
            var stage = new StageLifeCycle();
            stage.InitData(new ConfigDatabase(4, 4));

            Assert.Throws<InvalidOperationException>(stage.RegisterEntryJobs);
        }

        #endregion

        #region 실제 조립

        [Test]
        public void RealDatabase_ExposesDefaultFourByFourBoard()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var context = (IHasArtifactBoardConfig)DataBaseFactory.Create(catalog);

            Assert.That(context.ArtifactBoardConfig.Config.InitialWidth, Is.EqualTo(4));
            Assert.That(context.ArtifactBoardConfig.Config.InitialHeight, Is.EqualTo(4));
        }

        [Test]
        public void RuntimeLifeCycleGroup_DeliversRealBoardConfigToStageLifeCycle()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>(CatalogPath);
            // LifeCycle 그룹만 루트에 조립한다. 전체 런타임 InitData는 입력·UI 등 씬 의존 초기화까지 실행하므로 이 검증 범위가 아니다.
            var builder = new SystemBuilderHelper();
            LifeCycleSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());

            runtime.InitData(DataBaseFactory.Create(catalog));

            StageLifeCycle stage = GetStageLifeCycle(runtime);
            Assert.That(stage.ArtifactBoardConfig, Is.Not.Null);
            Assert.That(stage.ArtifactBoardConfig.Config.InitialWidth, Is.EqualTo(4));
            Assert.That(stage.ArtifactBoardConfig.Config.InitialHeight, Is.EqualTo(4));
        }

        #endregion

        private ArtifactBoardConfigDataSO Item(int width, int height)
        {
            var item = _assets.Create<ArtifactBoardConfigDataSO>();
            Set(item, "initialWidth", width);
            Set(item, "initialHeight", height);
            return item;
        }

        private static StageLifeCycle CreateStage(IServiceModule database, IJobRegisterSystem register,
            IJobRunner runner, IArtifactEditingService artifacts, bool hasRoster = true)
        {
            var stage = new StageLifeCycle();
            stage.InitData(database);
            stage.BindServices(register, runner, artifacts, new BoardStub(), Roster(hasRoster));
            return stage;
        }

        /// <summary>실제 캐릭터 Buffer로 로스터 유무만 만든다. 로스터가 있으면 진행 중인 Run이다.</summary>
        private static ProjectL.Systems.RuntimeSystem.BufferSystems.Character.ICharacterBufferReadSink Roster(bool hasRoster)
        {
            var builder = new SystemBuilderHelper();
            ProjectL.Systems.Factory.Runtime.Buffer.BufferSystemFactory.Create().ExportServices(builder);
            var buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            if (hasRoster)
                buffers.CharacterWriteSink.Acquire(new[] {
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.RosterAcquireRequest(
                        ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityKey.Unit_MainCharacter,
                        ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main)
                }, CharacterBufferTestData.MainState());
            return buffers.CharacterReadSink;
        }

        private static StageLifeCycle GetStageLifeCycle(FrameworkRuntime runtime)
        {
            var manager = (LifeCycleManager)runtime.GetService(SystemGroupsService.LifeCycle).LifeCycleManager;
            // 등록 목록은 공개하지 않으므로 테스트에서만 읽는다(FrameworkRuntimeCompositionTests와 같은 방식).
            var modules = (Dictionary<LifeCycleKey, ILifeCycleModule>)typeof(LifeCycleManager)
                .GetField("_modules", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(manager);
            return (StageLifeCycle)modules[LifeCycleKeys.Stage];
        }

        private sealed class ConfigDatabase : IHasArtifactBoardConfig
        {
            public ConfigDatabase(int width, int height) => ArtifactBoardConfig = new ConfigQuery(width, height);
            public IArtifactBoardConfigTableQuery ArtifactBoardConfig { get; }
        }

        private sealed class NoConfigDatabase : IServiceModule
        {
        }

        private sealed class ConfigQuery : IArtifactBoardConfigTableQuery
        {
            public ConfigQuery(int width, int height) => Config = new ArtifactBoardConfigData(width, height);
            public ArtifactBoardConfigData Config { get; }
        }

        private sealed class BoardStub : IArtifactBufferReadSink
        {
            public ArtifactBufferSnapshot Current => ArtifactBufferSnapshot.Empty;
        }

        private sealed class RegisterSpy : IJobRegisterSystem
        {
            public readonly List<(JobRootType, JobType)> Calls = new();
            public void Register(JobRootType rootType, JobType jobType) => Calls.Add((rootType, jobType));
            public void RegisterCombatEntry(CombatEntryRequest request) => throw new NotSupportedException();
        }

        private sealed class RunnerSpy : IJobRunner
        {
            public readonly List<IJobService> Jobs = new();
            public bool IsIdle => Jobs.Count == 0;
            public void Register(IJobService job) => Jobs.Add(job);
            public void RegisterRange(IReadOnlyList<IJobService> jobs) => Jobs.AddRange(jobs);
            public void Clear() => throw new NotSupportedException("공용 Runner를 비우면 안 됩니다.");
        }

        private sealed class ArtifactsSpy : IArtifactEditingService
        {
            public int Width;
            public int Height;

            public bool InitializeBoard(int width, int height)
            {
                Width = width;
                Height = height;
                return true;
            }

            public void Place(int artifactId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void Move(int instanceId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void ReturnToPool(int instanceId) => throw new NotSupportedException();
            public void ExpandBoard(int width, int height) => throw new NotSupportedException();
            public void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
        }
    }
}
