using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Plans;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister
{
    #region 서비스

    public interface IJobRegisterSystem : IServiceModule
    {
        void Register(JobRootType rootType, JobType jobType);
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
        // Run seed R4 인수 전 실행 계층에서 한 번 만든 임시 공급원. Plan에는 상태를 두지 않습니다.
        private readonly Func<int> _nextShopSeed = new Random().Next;
        private readonly Dictionary<JobRootType, Func<IInGameSystemGroupService, JobType, List<IJobService>>> _plans = new();
        private IInGameSystemGroupService _inGameService;
        private IBufferQuery _buffers;
        // Combat·Lobby·Stage Plan이 전환·진입 요청·진행 사실 읽기에 쓴다(전투 재구성 설계 J14, 로비 재구성 설계 §4.9).
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
            _plans[JobRootType.Lobby] = (service, jobType) => LobbyPlan.Register(service, jobType, _buffers, _lifeCycles);
            // Stage의 상점·회복·증강 Job도 Stage Plan 하나가 판단해 만든다(검사 후속 정리 계획 Q-Rd3).
            _plans[JobRootType.Stage] = (service, jobType) =>
                StagePlan.Register(service, jobType, _buffers, _nextShopSeed, _lifeCycles);
            _plans[JobRootType.Combat] = (service, jobType) => CombatPlan.Register(service, jobType, _lifeCycles);
            _plans[JobRootType.Tutorial] = (service, jobType) => TutorialPlan.Register(service, jobType, _buffers);
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
