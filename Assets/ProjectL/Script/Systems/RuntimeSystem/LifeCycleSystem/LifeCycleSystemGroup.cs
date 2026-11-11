using ProjectL.Systems.ServiceKeys.Runtime.LifeCycle;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem
{
    #region 서비스

    /// <summary>게임 실행 순서에 반응하는 시스템을 담는 전용 그룹입니다.</summary>
    public interface ILifeCycleSystemGroupService : IServiceModule
    {
        ILifeCycleManager LifeCycleManager { get; }
        ICombatEntryService CombatEntry { get; }
        IArtifactBoardPreparation ArtifactBoardPreparation { get; }

        /// <summary>전투 진행 판단에 쓰는 사실을 읽는 유일한 자리입니다(재구성 설계 J13·D5).</summary>
        ICombatProgressReader CombatProgress { get; }
        IStageEntryAttemptRead StageEntryAttempts { get; }

        /// <summary>이번 Stage 방문의 진행 사실과 상태 바꿈 입구입니다. Stage Plan만 씁니다(검사 후속 정리 계획 TP-R9).</summary>
        IStageProgress StageProgress { get; }
        IStageRunAbandonRequestService StageRunAbandon { get; }

        /// <summary>증강 선택이 남아 있는지 읽는 자리입니다(강화 View 설계 §14 TG2). 읽기만 합니다.</summary>
        IStageTraitGrowthFacts StageTraitGrowthFacts { get; }

        /// <summary>로비 판단에 쓰는 사실을 읽는 유일한 자리입니다(로비 재구성 설계 §4.6). 읽기만 합니다.</summary>
        ILobbyProgressReader LobbyProgress { get; }

        /// <summary>로비 아티팩트 처리 기록 확인 Job을 만드는 입구입니다. 도메인 정지는 이 확인 Job만 합니다(로비 재구성 설계 P1).</summary>
        ILobbyArtifactChecks LobbyArtifactChecks { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class LifeCycleSystemGroup :
        SystemGroup,
        ILifeCycleSystemGroupService,
        ISystemBindScene,
        ISystemInitData,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        public ILifeCycleManager LifeCycleManager { get; }
        public ICombatEntryService CombatEntry { get; }
        public IArtifactBoardPreparation ArtifactBoardPreparation { get; }
        public ICombatProgressReader CombatProgress { get; }
        public IStageEntryAttemptRead StageEntryAttempts { get; }
        public IStageProgress StageProgress { get; }
        public IStageRunAbandonRequestService StageRunAbandon { get; }
        public IStageTraitGrowthFacts StageTraitGrowthFacts { get; }
        public ILobbyProgressReader LobbyProgress { get; }
        public ILobbyArtifactChecks LobbyArtifactChecks { get; }

        internal LifeCycleSystemGroup(ISystemReader systemReader) : base(systemReader)
        {
            LifeCycleManager = systemReader.ServiceStoreReader.GetService(
                LifeCycleSystemServiceKey.Manager);
            CombatEntry = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.CombatEntry);
            ArtifactBoardPreparation = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.ArtifactBoardPreparation);
            CombatProgress = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.CombatProgress);
            StageEntryAttempts = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.StageEntryAttempts);
            StageProgress = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.StageProgress);
            StageRunAbandon = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.StageRunAbandon);
            StageTraitGrowthFacts = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.StageTraitGrowthFacts);
            LobbyProgress = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.LobbyProgress);
            LobbyArtifactChecks = systemReader.ServiceStoreReader.GetService(LifeCycleSystemServiceKey.LobbyArtifactChecks);
        }

        public void BindScene()
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.BindSceneModules)
                module.BindScene();
        }

        public void InitData(IServiceModule data)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitDataModules)
                module.InitData(data);
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.PostBindServiceModules)
                module.PostBindService(resolver);
        }

        public void Initialize()
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitializeModules)
                module.Initialize();
        }

        public void Tick(float deltaTime)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.TickModules)
                module.Tick(deltaTime);
        }

        public void Shutdown()
        {
            var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
            for (int i = modules.Count - 1; i >= 0; i--)
                modules[i].Shutdown();
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILifeCycleSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
    #endregion
}
