using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Plans;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister
{
    #region 서비스

    public interface IJobRegisterSystem : IServiceModule
    {
        void Register(JobRootType rootType, JobType jobType);
        void RegisterCombatEntry(CombatEntryRequest request);
    }

    /// <summary>루트별 static Plan에 인게임 서비스를 전달하고 반환된 Job 목록을 등록합니다.</summary>
    #endregion

    #region 시스템 모듈

    internal sealed class JobRegisterSystem :
        IJobRegisterSystem,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly IJobRunner _runner;
        private readonly Dictionary<JobRootType, Func<IInGameSystemGroupService, JobType, List<IJobService>>> _plans = new();
        private IInGameSystemGroupService _inGameService;
        private IBufferQuery _buffers;
        // Combat Plan이 전환·진입 요청·진행 사실 읽기에 쓴다(전투 재구성 설계 J14).
        private ILifeCycleSystemGroupService _lifeCycles;

        internal JobRegisterSystem(IJobRunner runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _inGameService = resolver.GetService(SystemGroupsService.InGame);
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
            _lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
        }

        public void Initialize()
        {
            _plans[JobRootType.Lobby] = LobbyPlan.Register;
            _plans[JobRootType.Stage] = (service, jobType) => StagePlan.Register(service, jobType, _buffers);
            _plans[JobRootType.Artifact] = ArtifactPlan.Register;
            _plans[JobRootType.Combat] = (service, jobType) => CombatPlan.Register(service, jobType, _lifeCycles);
        }

        public void Register(JobRootType rootType, JobType jobType)
        {
            if (_inGameService == null)
                throw new InvalidOperationException("JobRegisterSystem must bind in-game services before use.");

            if (!_plans.TryGetValue(rootType, out var plan))
                throw new KeyNotFoundException($"Job Plan is not registered: {rootType}. Initialize the system before use.");

            var jobs = plan(_inGameService, jobType);
            _runner.RegisterRange(jobs);
        }

        /// <summary>캡처한 진입 대상만 전달합니다. 기존 루트별 등록 계약은 유지합니다.</summary>
        public void RegisterCombatEntry(CombatEntryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (_inGameService == null || !_plans.ContainsKey(JobRootType.Combat))
                throw new InvalidOperationException("진입 등록 전에 서비스 바인딩과 초기화가 필요합니다.");

            var jobs = CombatPlan.RegisterEntry(_inGameService, request, _lifeCycles);
            request.MarkQueued();
            try
            {
                _runner.RegisterRange(jobs);
            }
            catch
            {
                request.Fail();
                throw;
            }
        }

        public void Shutdown()
        {
            _plans.Clear();
            _inGameService = null;
            _buffers = null;
            _lifeCycles = null;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<IJobRegisterSystem>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
