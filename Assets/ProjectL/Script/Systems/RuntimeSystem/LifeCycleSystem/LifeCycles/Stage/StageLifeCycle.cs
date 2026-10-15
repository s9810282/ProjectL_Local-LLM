using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 스테이지 맵 구간의 수명입니다. 진입 때 맵 준비 Job과 아티팩트 보드 초기화 Job을 한 번씩 등록합니다.
    /// </summary>
    /// <remarks>
    /// 맵 준비는 <c>StagePlan</c>이 담당합니다. 로스터가 없는 첫 진입은 <see cref="JobType.NewRun"/>(새 Run 시작 → 챕터 진입), 그 외에는 <see cref="JobType.Start"/>(<c>StageJob</c> → <c>EnterChapter</c>)입니다.
    /// 같은 챕터에 다시 들어오면 Core가 그래프를 다시 만들지 않고 진입만 수락합니다.
    /// 보드 처음 크기는 실행 계층인 이 LifeCycle이 InGame 설정에서 읽고, View는 읽지 않습니다(아티팩트 팝업 계획 D7).
    /// 노드 진입과 StageMapFlow는 스테이지 선택 View 계획 6번에서 연결합니다.
    /// </remarks>
    internal sealed class StageLifeCycle : LifeCycleBase, ISystemInitData
    {
        private IJobRegisterSystem _jobRegister;
        private IJobRunner _jobRunner;
        private IArtifactEditingService _artifacts;
        private IArtifactBufferReadSink _artifactBoard;
        private IArtifactBoardConfigTableQuery _artifactBoardConfig;
        private ICharacterBufferReadSink _roster;

        public override LifeCycleKey Key => LifeCycleKeys.Stage;

        protected override int SceneIndex => SceneIndices.Stage;

        /// <summary>받아 둔 보드 설정입니다. 설정을 제공하지 않는 DB면 null입니다.</summary>
        internal IArtifactBoardConfigTableQuery ArtifactBoardConfig => _artifactBoardConfig;

        public void InitData(IServiceModule data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // 설정이 없는 DB(테스트용 조회 등)도 통과시키고, 실제로 필요한 StageSelect 진입 때 확인합니다(D7).
            _artifactBoardConfig = (data as IHasArtifactBoardConfig)?.ArtifactBoardConfig;
        }

        protected override void OnBindServices(ISystemServiceStoreReader resolver)
        {
            var jobs = resolver.GetService(SystemGroupsService.Global).JobServiceCommand;
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            BindServices(jobs.JobRegister, jobs.JobRunner,
                resolver.GetService(SystemGroupsService.InGame).CoreGroup.Artifacts,
                buffers.ArtifactReadSink, buffers.CharacterReadSink);
        }

        protected override void OnEnter()
        {
            RegisterEntryJobs();
        }

        protected override void OnExit()
        {
            // TODO: StageMapFlow 종료 (6번). Global Runner는 다른 구간과 함께 쓰므로 여기서 비우지 않습니다.
        }

        /// <summary>필요한 서비스만 보관합니다. 아티팩트는 Core 서비스와 읽기 Sink만 받습니다(FW-101).</summary>
        internal void BindServices(IJobRegisterSystem jobRegister, IJobRunner jobRunner,
            IArtifactEditingService artifacts, IArtifactBufferReadSink artifactBoard, ICharacterBufferReadSink roster)
        {
            _jobRegister = jobRegister ?? throw new ArgumentNullException(nameof(jobRegister));
            _jobRunner = jobRunner ?? throw new ArgumentNullException(nameof(jobRunner));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _artifactBoard = artifactBoard ?? throw new ArgumentNullException(nameof(artifactBoard));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        /// <summary>
        /// 맵 준비와 보드 초기화를 등록합니다. 필요한 서비스·설정이 없으면 아무것도 등록하지 않고 예외를 던집니다.
        /// </summary>
        internal void RegisterEntryJobs()
        {
            if (_jobRegister == null || _jobRunner == null || _roster == null)
                throw new InvalidOperationException("[StageLifeCycle] Job services must be bound before entering.");
            if (_artifactBoardConfig == null)
                throw new InvalidOperationException("[StageLifeCycle] Artifact board config is required to enter the stage map.");

            ArtifactBoardConfigData config = _artifactBoardConfig.Config;

            // TODO: StageMapFlow 시작 (6번). 지금은 맵 준비와 보드 초기화만 등록합니다.
            // 로스터가 없으면 아직 Run이 시작되지 않은 첫 진입이므로 새 Run(로스터·Run 재화)부터 시작한다.
            // 로스터가 있으면(이어하기, 전투 뒤 복귀, 타이틀의 새 게임이 이미 NewRun을 등록한 경우) 맵 준비만 한다.
            // 끝난 Run(패배·최종 승리)을 자동으로 새로 시작하지 않는다. 그 목적지는 별도 결정이다.
            JobType entry = _roster.Owned.Count == 0 ? JobType.NewRun : JobType.Start;
            _jobRegister.Register(JobRootType.Stage, entry);
            _jobRunner.Register(ArtifactPlan.CreateBoardInitialization(
                _artifacts, _artifactBoard, config.InitialWidth, config.InitialHeight));
        }
    }
}
