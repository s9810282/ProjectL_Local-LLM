#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem
{
    #region 서비스

    public interface IInGameViewSystemGroupService : IServiceModule
    {
        IInGameViewInputContext Input { get; }
        ICombatViewService Combat { get; }
        IRecoveryRequestSubmitter RecoveryRequests { get; }
        IRecoveryRequestExecution RecoveryExecution { get; }
        IRecoveryRequestStatus RecoveryStatus { get; }

        /// <summary>증강 선택 화면이 고른 것을 제출하는 창구입니다(설계 §14 TG4). 소유자가 없는 조립에서는 null입니다.</summary>
        ITraitGrowthRequestSubmitter TraitGrowthRequests { get; }

        /// <summary>Stage Plan·증강 처리 Job이 쓰는 증강 요청 예약·종료 창구입니다. 소유자가 없는 조립에서는 null입니다(Stage는 필수로 받음).</summary>
        ITraitGrowthRequestExecution TraitGrowthExecution { get; }

        /// <summary>TutorialPlan·Job이 쓰는 튜토리얼 실행 소유자의 창구입니다(튜토리얼 설계 §9.1). 실행 소유자가 없는 조립에서는 null입니다.</summary>
        ITutorialViewExecution TutorialExecution { get; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>개발 기능을 포함한 조립에서만 제공하는 명령 창구입니다.</summary>
        IDevelopmentTools DevelopmentTools { get; }
#endif
    }

    #endregion

    #region 시스템 모듈

    internal sealed class InGameViewSystemGroup :
        SystemGroup,
        IInGameViewSystemGroupService,
        ISystemBindScene,
        ISystemInitData,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown
    {
        /// <summary>소유한 화면 슬롯입니다. 입력을 쓰지 않는 최소 조립에서는 생략할 수 있습니다.</summary>
        public IInGameViewInputContext Input { get; }
        public ICombatViewService Combat { get; }
        public IRecoveryRequestSubmitter RecoveryRequests => _recovery;
        public IRecoveryRequestExecution RecoveryExecution => _recovery;
        public IRecoveryRequestStatus RecoveryStatus => _recovery;
        public ITraitGrowthRequestSubmitter TraitGrowthRequests => _traitGrowth;
        public ITraitGrowthRequestExecution TraitGrowthExecution => _traitGrowth;
        public ITutorialViewExecution TutorialExecution { get; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public IDevelopmentTools DevelopmentTools { get; }
#endif
        private readonly RecoveryRequestLifetime _recovery;
        private readonly TraitGrowthRequestLifetime _traitGrowth;
        private readonly InGameViewFaultCleanup _faultCleanup;
        private bool _shutdownStarted;

        public InGameViewSystemGroup(ISystemReader systemReader, ICombatViewService combat,
            InGameViewFaultCleanup faultCleanup = null, RecoveryRequestLifetime recovery = null,
            IInGameViewInputContext input = null, ITutorialViewExecution tutorial = null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , IDevelopmentTools developmentTools = null
#endif
            , TraitGrowthRequestLifetime traitGrowth = null
            ) : base(systemReader)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentTools = developmentTools;
#endif
            Input = input;
            TutorialExecution = tutorial;
            Combat = combat;
            _faultCleanup = faultCleanup;
            _recovery = recovery;
            _traitGrowth = traitGrowth;
        }

        public void BindScene()
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.BindSceneModules)
                module.BindScene();
        }

        public void InitData(IServiceModule query)
        {
            foreach (var module in SystemReader.SystemModuleStoreReader.InitDataModules)
                module.InitData(query);
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
            if (_shutdownStarted) return;
            _shutdownStarted = true;
            List<Exception> errors = null;
            try
            {
                try
                {
                    _faultCleanup?.StopInput();
                }
                catch (Exception error)
                {
                    (errors ??= new()).Add(error);
                }
                var modules = SystemReader.SystemModuleStoreReader.ShutdownModules;
                for (int i = modules.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        modules[i].Shutdown();
                    }
                    catch (Exception error)
                    {
                        (errors ??= new()).Add(error);
                    }
                }
            }
            finally
            {
                _faultCleanup?.Disconnect();
            }
            if (errors != null) throw new AggregateException("[InGameView] 종료 정리 중 실패했습니다.", errors);
        }

        public override void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IInGameViewSystemGroupService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
