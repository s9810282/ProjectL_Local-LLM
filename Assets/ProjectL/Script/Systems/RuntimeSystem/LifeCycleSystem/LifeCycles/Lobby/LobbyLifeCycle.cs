using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Plans;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby
{
    /// <summary>
    /// 로비 구간의 수명입니다. 로비 Job을 거는 트리거만 맡고, 무엇을 할지는 <see cref="LobbyPlan"/>이 정합니다
    /// (로비 재구성 설계 §4.1·§4.8).
    /// </summary>
    /// <remarks>
    /// Buffer·Core를 직접 읽지 않고 판단하지 않습니다. 들어올 때 아티팩트 도메인 정지를 풀고,
    /// 활성 동안 Runner가 비면 다음 판단을 한 번 걸고, 씬 이탈 노드에서 <see cref="LobbyPlan.CanLeave"/>로 이탈을 검사합니다.
    /// 출발 요청의 단계는 바꾸지 않습니다.
    /// </remarks>
    internal sealed class LobbyLifeCycle : LifeCycleBase
    {
        /// <summary>씬 이탈 검사 순서입니다. 로비 View 해제(<c>LobbyViewSystem.DetachPriority</c>)는 이 값에서 구해 늘 이 뒤에 돕니다.</summary>
        internal const int ExitPreparationPriority = -200;
        private readonly RuntimeFaultBoundary _faults;
        private readonly ILobbyProgressReader _progress;
        private readonly LobbyArtifactDomain _artifacts;
        private IJobRunner _runner;
        private IJobRegisterSystem _register;
        private ILodingRegisterService _loading;
        private bool _shutdown;

        /// <param name="artifacts">진입 때 정지를 풀 아티팩트 도메인입니다. 해제는 이 LifeCycle만 합니다(설계 P1).</param>
        internal LobbyLifeCycle(RuntimeFaultBoundary faults, ILobbyProgressReader progress, LobbyArtifactDomain artifacts)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
        }

        public override LifeCycleKey Key => LifeCycleKeys.Lobby;

        protected override int SceneIndex => SceneIndices.Lobby;

        protected override void OnBindServices(ISystemServiceStoreReader resolver)
        {
            var global = resolver.GetService(SystemGroupsService.Global);
            _runner = global.JobServiceCommand.JobRunner;
            _register = global.JobServiceCommand.JobRegister;
            _loading = global.LodingServiceCommand.LodingRegisterService;
        }

        protected override void OnInitialize()
        {
            _loading.RegisterSceneExitLoaded(SceneIndex,
                new LodingNode(ExitPreparationPriority, PrepareForSceneExit));
        }

        /// <summary>로비에 들어오면 아티팩트 도메인 정지를 풉니다. 정지·오류 상태에서는 들어올 수 없습니다(로비 리팩토링 2차 설계 N-79).</summary>
        protected override void OnEnter()
        {
            if (_shutdown || _faults.IsFaulted)
                throw new InvalidOperationException("[LobbyLifeCycle] 정상 런타임에서만 로비에 들어올 수 있습니다.");
            // 재진입하면 아티팩트 편집을 다시 받는다(D11).
            _artifacts.Reset();
        }

        /// <summary>Runner가 바빠도 예외가 아닙니다. 빌 때까지 다음 판단을 걸지 않을 뿐입니다(설계 R5·B5).</summary>
        protected override void OnTick(float deltaTime)
        {
            // 같은 Tick 앞쪽에서 오류가 보고됐으면 새 등록을 하지 않는다.
            if (_shutdown || _faults.IsFaulted)
                return;
            if (_runner.IsIdle)
                _register.Register(JobRootType.Lobby, JobType.Next);
        }

        /// <summary>할 일이 없습니다. 이탈 검사는 이탈 노드가, 출발 요청 마감은 Job과 View가 맡습니다(설계 B7).</summary>
        protected override void OnExit()
        {
        }

        protected override void OnShutdown()
        {
            _shutdown = true;
            _runner = null;
            _register = null;
            _loading = null;
        }

        private void PrepareForSceneExit()
        {
            if (_shutdown)
                return;
            // 동기 로딩 콜백에서는 기다리지 않는다. 막히면 예외로 이탈을 멈추고, 원인 자리를 가리킨다.
            LobbyProgressFacts facts = _progress.Read();
            if (!LobbyPlan.CanLeave(in facts))
                throw new InvalidOperationException(
                    "[LobbyLifeCycle] 처리 안 된 아티팩트 편집이나 진행 중인 출발이 남아 로비를 떠날 수 없습니다.");
        }
    }
}
