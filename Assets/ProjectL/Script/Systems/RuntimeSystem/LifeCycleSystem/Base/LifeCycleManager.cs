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
    /// 진입·Tick·이탈 수명을 제공하는 게임 진행 구간입니다.
    /// LifeCycleBase 구현은 대상 씬 로딩 완료 때 진입하고, 현재 씬 이탈 콜백에서 정리합니다.
    /// </summary>
    internal interface ILifeCycleModule
    {
        LifeCycleKey Key { get; }

        void Enter();
        void Tick(float deltaTime);
        void Exit();
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
            if (_activeModule == null)
                return;

            ILifeCycleModule exitingModule = _activeModule;
            _activeModule = null;
            try
            {
                exitingModule.Exit();
            }
            catch
            {
                _activeModule = exitingModule;
                throw;
            }
        }

        #endregion

        #region 시스템 모듈

        public void Tick(float deltaTime)
        {
            _activeModule?.Tick(deltaTime);
        }

        public void Shutdown()
        {
            // Manager는 먼저 등록되므로 그룹의 역순 종료에서 자식 Shutdown이 먼저 끝납니다.
            // 이미 정리한 자식에는 로딩 이탈을 다시 요청하지 않고 활성 참조만 해제합니다.
            if (_activeModule is ISystemShutdown)
            {
                _activeModule = null;
                return;
            }
            Exit();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ILifeCycleManager>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion
    }
}
