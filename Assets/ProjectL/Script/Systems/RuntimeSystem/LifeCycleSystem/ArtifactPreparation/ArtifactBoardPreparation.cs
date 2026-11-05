using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation
{
    /// <summary>
    /// 최초 보드 준비를 다른 구간의 Job과 분리하고 게시·배출 완료를 확인합니다.
    /// 보드는 로비 화면 상태가 아니라 전투 효과도 쓰는 Core 게임 데이터라, 부트가 첫 구간(로비·스테이지·전투 직행) 전에 한 번 요청합니다(로비 설계 §9.2).
    /// 그때는 아직 어떤 구간 Plan도 돌지 않으므로, 빈 Runner에 준비 Job 하나를 직접 겁니다(Plan 밖, 검사 후속 정리 R-f J8).
    /// </summary>
    internal sealed class ArtifactBoardPreparation : IArtifactBoardPreparation,
        ISystemInitData, ISystemPostBindService, ISystemTick, ISystemShutdown
    {
        private readonly RuntimeFaultBoundary _faults;
        private IArtifactBoardConfigTableQuery _config;
        private IJobRunner _runner;
        private IArtifactEditingService _artifacts;
        private IArtifactBufferReadSink _board;
        private IJobService _initialization;
        private bool _requested;
        private bool _ready;
        private bool _shutdown;

        internal ArtifactBoardPreparation(RuntimeFaultBoundary faults) =>
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));

        public bool IsReady => _ready && !_shutdown && !_faults.IsFaulted;

        public void Request()
        {
            if (!_shutdown && !_faults.IsFaulted)
                _requested = true;
        }

        public void InitData(IServiceModule data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _config = (data as IHasArtifactBoardConfig)?.ArtifactBoardConfig ??
                throw new InvalidOperationException("[ArtifactBoardPreparation] DB에 아티팩트 보드 설정(IHasArtifactBoardConfig)이 필요합니다.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            BindServices(resolver.GetService(SystemGroupsService.Global).JobServiceCommand.JobRunner,
                resolver.GetService(SystemGroupsService.InGame).CoreGroup.Artifacts,
                resolver.GetService(BufferSystemKey.Buffer).ArtifactReadSink);
        }

        public void Tick(float deltaTime)
        {
            if (!_requested || _ready || _shutdown || _faults.IsFaulted)
                return;
            try
            {
                if (_runner == null || _board == null || _artifacts == null)
                    throw new InvalidOperationException("[ArtifactBoardPreparation] 준비 서비스를 먼저 연결해야 합니다.");
                if (!_runner.IsIdle)
                    return;
                if (_initialization != null)
                {
                    if (!_initialization.IsCompleted || !_board.Current.IsInitialized)
                        throw new InvalidOperationException("[ArtifactBoardPreparation] 초기화 Job 완료와 보드 게시가 필요합니다.");
                    _ready = true;
                    return;
                }
                if (_board.Current.IsInitialized)
                {
                    _ready = true;
                    return;
                }
                var config = _config.Config;
                // 공용 Runner가 빈 상태에서 자기 배치 하나만 등록합니다. 구간 진입은 Ready 뒤에 시작합니다.
                _initialization = ArtifactBoardInitializeJob.Create(_artifacts, _board,
                    config.InitialWidth, config.InitialHeight);
                _runner.Register(_initialization);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        public void Shutdown()
        {
            _shutdown = true;
            _ready = false;
            _initialization = null;
            _config = null;
            _runner = null;
            _artifacts = null;
            _board = null;
        }

        internal void BindServices(IJobRunner runner, IArtifactEditingService artifacts,
            IArtifactBufferReadSink board)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _board = board ?? throw new ArgumentNullException(nameof(board));
        }
    }
}
