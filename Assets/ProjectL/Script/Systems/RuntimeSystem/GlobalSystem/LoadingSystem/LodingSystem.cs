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
        void BindPresentation(ILodingPresentation presentation);
        void UnbindPresentation(ILodingPresentation presentation);
    }

    /// <summary>
    /// Scene Build Index를 상태 키로 사용합니다.
    /// 로딩 요청은 BeforeLoad → SceneExitLoaded → 비동기 씬 로드 →
    /// SceneEnterLoaded → AfterLoad → 선택적인 씬 준비 → Reveal 순서로 실행합니다.
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
        private ILodingPresentation _presentation;
        private Phase _phase;
        private IReadOnlyList<LodingPreparationNode> _preparationNodes = Array.Empty<LodingPreparationNode>();
        private int _preparationIndex;
        private ILodingPreparation _preparation;

        private enum Phase
        {
            Idle,
            Covering,
            CoveredFrame,
            Loading,
            Preparing,
            Revealing
        }

        internal LodingSystem(ILodingRegisterReader registerReader)
        {
            _registerReader = registerReader
                ?? throw new ArgumentNullException(nameof(registerReader));
        }

        #region 서비스

        public int CurrentSceneIndex { get; private set; } = -1;
        public bool IsLoading => _phase != Phase.Idle;
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
            _pendingSceneIndex = sceneIndex;
            _phase = _presentation == null ? Phase.Loading : Phase.Covering;
            try
            {
                if (_presentation != null)
                    _presentation.BeginCover();
                else
                    BeginSceneLoad();
            }
            catch (Exception exception)
            {
                Abort(exception);
                throw;
            }
        }

        public void BindPresentation(ILodingPresentation presentation)
        {
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));
            if (ReferenceEquals(_presentation, presentation)) return;
            if (IsLoading || _presentation != null)
                throw new InvalidOperationException("로딩 표현 소유자는 대기 상태에서 하나만 연결할 수 있습니다.");
            _presentation = presentation;
        }

        public void UnbindPresentation(ILodingPresentation presentation)
        {
            if (presentation == null) throw new ArgumentNullException(nameof(presentation));
            if (!ReferenceEquals(_presentation, presentation)) return;
            if (IsLoading)
                throw new InvalidOperationException("진행 중인 로딩 표현은 해제할 수 없습니다.");
            _presentation = null;
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
            if (!IsLoading) return;
            try
            {
                switch (_phase)
                {
                    case Phase.Covering:
                        if (_presentation.IsHidden)
                            throw new InvalidOperationException("덮기 중 로딩 UI가 외부에서 닫혔습니다.");
                        if (_presentation.IsCovered) _phase = Phase.CoveredFrame;
                        break;
                    case Phase.CoveredFrame:
                        // 완전 가림 상태가 한 프레임 그려진 뒤 씬을 교체한다.
                        _phase = Phase.Loading;
                        BeginSceneLoad();
                        break;
                    case Phase.Loading:
                        LoadingProgress = _loadingOperation.progress;
                        if (_loadingOperation.isDone) CompleteSceneLoad();
                        break;
                    case Phase.Revealing:
                        if (_presentation.IsHidden)
                        {
                            _presentation.End();
                            _phase = Phase.Idle;
                        }
                        break;
                    case Phase.Preparing:
                        TickPreparation(deltaTime);
                        break;
                }
            }
            catch (Exception exception)
            {
                Abort(exception);
                throw;
            }
        }

        public void Shutdown()
        {
            try { CancelPreparation(); }
            finally
            {
                _loadingOperation = null;
                _pendingSceneIndex = -1;
                _phase = Phase.Idle;
                // Global은 UI를 먼저 종료한다. 종료된 UI 서비스를 다시 호출하지 않는다.
                _presentation = null;
                CurrentSceneIndex = -1;
                LoadingProgress = 0f;
            }
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<ILodingSystemService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        private void BeginSceneLoad()
        {
            int sceneIndex = _pendingSceneIndex;
            ExecuteNodes(_registerReader.GetBeforeLoadNodes());
            bool isCurrentScene = CurrentSceneIndex == sceneIndex;
            ExitCurrentScene();
            if (isCurrentScene)
            {
                CompleteSceneLoad();
                return;
            }
            _loadingOperation = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Single);
            if (_loadingOperation == null)
            {
                ExecuteNodes(_registerReader.GetAfterLoadNodes());
                throw new InvalidOperationException($"Could not load scene. Build index: {sceneIndex}");
            }
        }

        private void CompleteSceneLoad()
        {
            int sceneIndex = _pendingSceneIndex;
            _loadingOperation = null;
            _pendingSceneIndex = -1;
            LoadingProgress = 1f;
            EnterLoadedScene(sceneIndex);
            // AfterLoad의 카메라 연결과 finally 실행을 모두 마친 뒤 준비합니다.
            _preparationNodes = _registerReader.GetScenePreparations(sceneIndex);
            _preparationIndex = 0;
            if (_preparationNodes.Count > 0)
            {
                _phase = Phase.Preparing;
                BeginNextPreparation();
                return;
            }
            BeginReveal();
        }

        private void BeginReveal()
        {
            if (_presentation == null)
                _phase = Phase.Idle;
            else
            {
                _presentation.BeginReveal();
                _phase = Phase.Revealing;
            }
        }

        private void BeginNextPreparation()
        {
            if (_presentation != null && !_presentation.IsCovered)
                throw new InvalidOperationException("씬 준비가 끝나기 전에 로딩 화면의 가림이 풀렸습니다.");
            _preparation = _preparationNodes[_preparationIndex].Create();
            if (_preparation.State != LodingPreparationState.Pending)
                throw new InvalidOperationException("로딩 준비에는 새 Pending 작업이 필요합니다.");
            _preparation.Begin(_presentation != null);
        }

        private void TickPreparation(float deltaTime)
        {
            if (_presentation != null && !_presentation.IsCovered)
                throw new InvalidOperationException("씬 준비 중 로딩 화면이 외부에서 열렸습니다.");
            ILodingPreparation preparation = _preparation;
            preparation.Tick(deltaTime);
            // 작업 콜백에서 Shutdown/취소된 연결의 완료를 소비하지 않습니다.
            if (_phase != Phase.Preparing || !ReferenceEquals(preparation, _preparation)) return;
            switch (preparation.State)
            {
                case LodingPreparationState.Pending:
                    return;
                case LodingPreparationState.Complete:
                    _preparation = null;
                    _preparationIndex++;
                    if (_preparationIndex < _preparationNodes.Count) BeginNextPreparation();
                    else
                    {
                        _preparationNodes = Array.Empty<LodingPreparationNode>();
                        BeginReveal();
                    }
                    return;
                default:
                    throw new InvalidOperationException($"씬 준비가 성공하지 못했습니다: {preparation.State}.");
            }
        }

        private void CancelPreparation()
        {
            ILodingPreparation preparation = _preparation;
            _preparation = null;
            _preparationNodes = Array.Empty<LodingPreparationNode>();
            _preparationIndex = 0;
            preparation?.Cancel();
        }

        private void Abort(Exception cause)
        {
            _phase = Phase.Idle;
            _loadingOperation = null;
            _pendingSceneIndex = -1;
            LoadingProgress = 0f;
            List<Exception> errors = null;
            try { CancelPreparation(); }
            catch (Exception cleanup) { (errors ??= new List<Exception>()).Add(cleanup); }
            try { _presentation?.End(); }
            catch (Exception cleanup) { (errors ??= new List<Exception>()).Add(cleanup); }
            if (errors == null) return;
            errors.Insert(0, cause);
            throw new AggregateException("로딩 실패 후 준비/UI 정리에도 실패했습니다.", errors);
        }

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
