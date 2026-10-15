using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Framework.Composition
{
    public interface ISystemRuntime
    {
        #region 시스템 모듈

        void BindScene();

        void InitData(IServiceModule systemDataQuery);

        void PostBindService();

        void Initialize();

        void Tick(float deltaTime);

        void Shutdown();

        #endregion
    }

    public interface ISystemServiceResolver
    {
        #region 서비스

        TService GetService<TService>(
            SystemGroupKey<TService> key)
            where TService : class, IServiceModule;

        #endregion
    }

    public sealed class FrameworkRuntime :
        ISystemRuntime,
        ISystemServiceResolver
    {
        private readonly ISystemReader _systemReader;

        private bool _isDataInitialized;
        private bool _isPostBound;
        private bool _isInitialized;
        private bool _isShutdown;

        public FrameworkRuntime(ISystemReader systemReader)
        {
            _systemReader = systemReader
                ?? throw new ArgumentNullException(nameof(systemReader));
        }

        #region 시스템 모듈

        public void BindScene()
        {
            ThrowIfShutdown();

            IReadOnlyList<ISystemBindScene> modules =
                _systemReader.SystemModuleStoreReader.BindSceneModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].BindScene();
            }
        }

        public void InitData(
            IServiceModule systemDataQuery)
        {
            ThrowIfShutdown();

            if (systemDataQuery == null)
                throw new ArgumentNullException(nameof(systemDataQuery));

            if (_isDataInitialized)
            {
                throw new InvalidOperationException(
                    "[SystemManager] InitData has already been called.");
            }

            IReadOnlyList<ISystemInitData> modules =
                _systemReader.SystemModuleStoreReader.InitDataModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].InitData(systemDataQuery);
            }

            _isDataInitialized = true;
        }

        public void PostBindService()
        {
            ThrowIfShutdown();

            if (_isPostBound)
            {
                throw new InvalidOperationException(
                    "[SystemManager] PostBindService has already been called.");
            }

            IReadOnlyList<ISystemPostBindService> modules =
                _systemReader.SystemModuleStoreReader.PostBindServiceModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].PostBindService(_systemReader.ServiceStoreReader);
            }

            _isPostBound = true;
        }

        public void Initialize()
        {
            ThrowIfShutdown();

            if (_isInitialized)
            {
                throw new InvalidOperationException(
                    "[SystemManager] Initialize has already been called.");
            }

            if (_systemReader.SystemModuleStoreReader.InitDataModules.Count > 0 &&
                !_isDataInitialized)
            {
                throw new InvalidOperationException(
                    "[SystemManager] InitData must be completed before Initialize.");
            }

            if (_systemReader.SystemModuleStoreReader.PostBindServiceModules.Count > 0 &&
                !_isPostBound)
            {
                throw new InvalidOperationException(
                    "[SystemManager] PostBindService must be completed before Initialize.");
            }

            IReadOnlyList<ISystemInitialize> modules =
                _systemReader.SystemModuleStoreReader.InitializeModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].Initialize();
            }

            _isInitialized = true;
        }

        public void Tick(float deltaTime)
        {
            ThrowIfShutdown();

            if (!_isInitialized)
            {
                throw new InvalidOperationException(
                    "[SystemManager] Initialize must be completed before Tick.");
            }

            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    deltaTime,
                    "Delta time cannot be negative.");
            }

            IReadOnlyList<ISystemTick> modules =
                _systemReader.SystemModuleStoreReader.TickModules;

            for (int i = 0; i < modules.Count; i++)
            {
                modules[i].Tick(deltaTime);
            }
        }

        public void Shutdown()
        {
            if (_isShutdown)
                return;

            IReadOnlyList<ISystemShutdown> modules =
                _systemReader.SystemModuleStoreReader.ShutdownModules;

            for (int i = modules.Count - 1; i >= 0; i--)
            {
                modules[i].Shutdown();
            }

            _isShutdown = true;
        }

        #endregion

        #region 서비스

        public TService GetService<TService>(
            SystemGroupKey<TService> key)
            where TService : class, IServiceModule
        {
            ThrowIfShutdown();

            return _systemReader.ServiceStoreReader.GetService(key);
        }

        #endregion

        private void ThrowIfShutdown()
        {
            if (_isShutdown)
            {
                throw new ObjectDisposedException(
                    nameof(FrameworkRuntime),
                    "The system runtime has already been shut down.");
            }
        }

    }
}
