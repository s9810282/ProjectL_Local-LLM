using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base
{
    /// <summary>
    /// 게임 진행 구간의 공통 진입점입니다.
    /// 초기화 때 자신의 Scene Index 진입·종료 Node를 등록하고, 실제 흐름은 자식 훅으로만 위임합니다.
    /// </summary>
    internal abstract class LifeCycleBase :
        ILifeCycleModule,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown,
        IExportSystemServices
    {
        private ILodingSystemService _lodingSystem;
        private ILodingRegisterService _lodingRegisterSystem;
        private bool _isInitialized;
        private bool _isActive;

        public abstract LifeCycleKey Key { get; }

        /// <summary>이 LifeCycle이 담당하는 Unity Build Settings Scene Index입니다.</summary>
        protected abstract int SceneIndex { get; }

        #region 시스템 모듈

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            _lodingSystem = resolver.GetService(SystemGroupsService.Global)
                .LodingServiceCommand
                .LodingService;
            _lodingRegisterSystem = resolver.GetService(SystemGroupsService.Global)
                .LodingServiceCommand
                .LodingRegisterService;
            OnBindServices(resolver);
        }

        /// <summary>
        /// 템플릿 메서드: 공통 Scene Index Node를 먼저 등록한 뒤 자식 초기화를 호출합니다.
        /// </summary>
        public void Initialize()
        {
            if (_lodingSystem == null || _lodingRegisterSystem == null)
                throw new InvalidOperationException(
                    $"{GetType().Name} must bind loading services before initialization.");

            _lodingRegisterSystem.RegisterSceneEnterLoaded(SceneIndex,
                new LodingNode(priority: 0, HandleLodingEnter));
            _lodingRegisterSystem.RegisterSceneExitLoaded(SceneIndex,
                new LodingNode(priority: 0, HandleLodingExit));

            OnInitialize();
            _isInitialized = true;
        }

        public void Shutdown()
        {
            if (_isActive)
                HandleLodingExit();

            OnShutdown();
        }

        /// <summary>
        /// 자식은 필요할 때에만 별도 서비스를 내보냅니다.
        /// 공통 시스템 모듈 등록은 Base가 보장합니다.
        /// </summary>
        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            ExportLifeCycleServices(sink);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region 라이프사이클 모듈

        /// <summary>
        /// Manager는 실제 자식 훅을 직접 호출하지 않고, 이 요청만 보냅니다.
        /// 로딩 완료 후 등록된 SceneEnterLoaded Node가 OnEnter를 실행합니다.
        /// </summary>
        public void Enter()
        {
            EnsureInitialized();
            _lodingSystem.Load(SceneIndex);
        }

        public void Tick(float deltaTime)
        {
            if (_isActive)
                OnTick(deltaTime);
        }

        public void Exit()
        {
            EnsureInitialized();
            _lodingSystem.Unload();
        }

        #endregion

        private void HandleLodingEnter()
        {
            if (_isActive)
                return;

            _isActive = true;
            try
            {
                OnEnter();
            }
            catch
            {
                _isActive = false;
                throw;
            }
        }

        private void HandleLodingExit()
        {
            if (!_isActive)
                return;

            try
            {
                OnExit();
            }
            finally
            {
                _isActive = false;
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    $"{GetType().Name} must be initialized before it can change state.");
            }
        }

        /// <summary>공통 서비스 연결 뒤 각 라이프사이클의 서비스를 연결합니다.</summary>
        protected virtual void OnBindServices(ISystemServiceStoreReader resolver) { }

        /// <summary>로딩 콜백 등록 뒤, 자식 전용 초기화를 수행합니다.</summary>
        protected virtual void OnInitialize() { }

        /// <summary>로딩 시작이 완료되었을 때 Flow를 시작합니다.</summary>
        protected abstract void OnEnter();

        /// <summary>활성 LifeCycle에서만 호출됩니다.</summary>
        protected virtual void OnTick(float deltaTime) { }

        /// <summary>로딩 종료 단계에서 Flow를 정리합니다.</summary>
        protected abstract void OnExit();

        /// <summary>종료 시 자식이 소유한 자원을 해제합니다.</summary>
        protected virtual void OnShutdown() { }

        /// <summary>자식 LifeCycle이 외부로 제공할 서비스가 있을 때만 재정의합니다.</summary>
        protected virtual void ExportLifeCycleServices(ISystemWriteSink sink) { }
    }
}
