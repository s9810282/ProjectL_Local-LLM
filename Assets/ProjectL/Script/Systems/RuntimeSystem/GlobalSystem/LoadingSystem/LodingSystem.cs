using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem
{
    public interface ILodingSystemService : IServiceModule
    {
        int CurrentSceneIndex { get; }
        bool IsLoading { get; }
        float LoadingProgress { get; }

        void Load(int sceneIndex);
        void Unload();
    }

    /// <summary>
    /// Scene Build Index를 상태 키로 사용합니다.
    /// 로딩 요청은 BeforeLoad → SceneExitLoaded → 비동기 씬 로드 →
    /// SceneEnterLoaded → AfterLoad 순서로 실행합니다.
    /// </summary>
    internal sealed class LodingSystem :
        ILodingSystemService,

        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly ILodingRegisterReader _registerReader;

        private AsyncOperation _loadingOperation;
        private int _pendingSceneIndex = -1;

        internal LodingSystem(ILodingRegisterReader registerReader)
        {
            _registerReader = registerReader
                ?? throw new ArgumentNullException(nameof(registerReader));
        }

        #region 서비스

        public int CurrentSceneIndex { get; private set; } = -1;
        public bool IsLoading => _loadingOperation != null;
        public float LoadingProgress { get; private set; } = 1f;

        public void Load(int sceneIndex)
        {
            ValidateSceneIndex(sceneIndex);

            if (IsLoading)
            {
                throw new InvalidOperationException(
                    "A scene loading operation is already in progress.");
            }

            LoadingProgress = 0f;
            ExecuteNodes(_registerReader.GetBeforeLoadNodes());

            bool isCurrentScene = CurrentSceneIndex == sceneIndex;
            ExitCurrentScene();

            if (isCurrentScene)
            {
                EnterLoadedScene(sceneIndex);
                return;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(
                sceneIndex,
                LoadSceneMode.Single);
            if (operation == null)
            {
                ExecuteNodes(_registerReader.GetAfterLoadNodes());
                throw new InvalidOperationException(
                    $"Could not load scene. Build index: {sceneIndex}");
            }

            _pendingSceneIndex = sceneIndex;
            _loadingOperation = operation;
        }

        public void Unload()
        {
            if (IsLoading)
            {
                throw new InvalidOperationException(
                    "The active scene cannot be unloaded while another scene is loading.");
            }

            ExitCurrentScene();
        }

        #endregion

        #region 시스템 모듈

        public void Initialize()
        {
            CurrentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        }

        public void Tick(float deltaTime)
        {
            if (!IsLoading || !_loadingOperation.isDone)
            {
                if (IsLoading)
                    LoadingProgress = _loadingOperation.progress;

                return;
            }

            int loadedSceneIndex = _pendingSceneIndex;
            _loadingOperation = null;
            _pendingSceneIndex = -1;
            LoadingProgress = 1f;
            CurrentSceneIndex = loadedSceneIndex;

            EnterLoadedScene(loadedSceneIndex);
        }

        public void Shutdown()
        {
            _loadingOperation = null;
            _pendingSceneIndex = -1;
            CurrentSceneIndex = -1;
            LoadingProgress = 0f;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<ILodingSystemService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        private void ExitCurrentScene()
        {
            if (CurrentSceneIndex < 0)
                return;

            ExecuteNodes(_registerReader.GetSceneExitLoadedNodes(CurrentSceneIndex));
            CurrentSceneIndex = -1;
        }

        private void EnterLoadedScene(int sceneIndex)
        {
            CurrentSceneIndex = sceneIndex;
            LoadingProgress = 1f;
            try
            {
                ExecuteNodes(_registerReader.GetSceneEnterLoadedNodes(sceneIndex));
            }
            finally
            {
                ExecuteNodes(_registerReader.GetAfterLoadNodes());
            }
        }

        private static void ExecuteNodes(IReadOnlyList<LodingNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
                nodes[i].Execute();
        }

        private static void ValidateSceneIndex(int sceneIndex)
        {
            if (sceneIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sceneIndex),
                    sceneIndex,
                    "Scene build index must be zero or greater.");
            }
        }

    }
}
