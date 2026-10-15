using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base
{
    /// <summary>큰 게임 진행 구간의 시작·종료와 단일 Tick 대상을 통제합니다.</summary>
    public interface ILifeCycleManager : IServiceModule
    {
        bool HasActiveLifeCycle { get; }
        LifeCycleKey CurrentKey { get; }

        void Change(LifeCycleKey nextKey);
        void Exit();
    }

    /// <summary>
    /// Flow를 소유하는 게임 진행 구간입니다.
    /// Module은 로딩 완료 후 실제 Enter/Exit 훅을 실행합니다.
    /// </summary>
    internal interface ILifeCycleModule
    {
        LifeCycleKey Key { get; }

        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>로딩 요청 전에 현재 구간이 정상 전환 사전조건을 검사합니다.</summary>
    internal interface ILifeCycleTransitionGuard
    {
        void ValidateTransition(LifeCycleKey? destination);
    }

    internal sealed class LifeCycleManager :
        ILifeCycleManager,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly Dictionary<LifeCycleKey, ILifeCycleModule> _modules = new();

        private ILifeCycleModule _activeModule;

        internal void Register(ILifeCycleModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (!_modules.TryAdd(module.Key, module))
                throw new InvalidOperationException(
                    $"LifeCycle is already registered. Key: {module.Key}");
        }

        #region 서비스

        public bool HasActiveLifeCycle => _activeModule != null;
        public LifeCycleKey CurrentKey => _activeModule == null
            ? default
            : _activeModule.Key;

        public void Change(LifeCycleKey nextKey)
        {
            if (!_modules.TryGetValue(nextKey, out ILifeCycleModule nextModule))
                throw new KeyNotFoundException(
                    $"LifeCycle is not registered. Key: {nextKey}");

            if (ReferenceEquals(_activeModule, nextModule))
                return;

            (_activeModule as ILifeCycleTransitionGuard)?.ValidateTransition(nextKey);

            ILifeCycleModule previousModule = _activeModule;
            _activeModule = nextModule;
            try
            {
                _activeModule.Enter();
            }
            catch
            {
                _activeModule = previousModule;
                throw;
            }
        }

        public void Exit()
        {
            (_activeModule as ILifeCycleTransitionGuard)?.ValidateTransition(null);
            ExitCore();
        }

        private void ExitCore()
        {
            if (_activeModule == null)
                return;

            ILifeCycleModule exitingModule = _activeModule;
            _activeModule = null;
            exitingModule.Exit();
        }

        #endregion

        #region 시스템 모듈

        public void Tick(float deltaTime)
        {
            _activeModule?.Tick(deltaTime);
        }

        public void Shutdown()
        {
            // Runtime 종료는 재Tick하지 않는 F1 정리다. 정상 씬 이탈의 완료 대기를 적용하지 않는다.
            ExitCore();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILifeCycleManager>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion
    }
}
