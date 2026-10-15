using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem
{
    /// <summary>로딩 단계별 Node 목록을 등록하는 서비스입니다.</summary>
    public interface ILodingRegisterService : IServiceModule
    {
        void RegisterBeforeLoad(LodingNode node);
        void RegisterSceneExitLoaded(int sceneIndex, LodingNode node);
        void RegisterSceneEnterLoaded(int sceneIndex, LodingNode node);
        void RegisterAfterLoad(LodingNode node);
    }

    /// <summary>
    /// 등록된 로딩 Node를 조회하는 읽기 전용 계약입니다.
    /// Node 실행과 등록은 이 계약에 포함하지 않습니다.
    /// </summary>
    public interface ILodingRegisterReader : IServiceModule
    {
        IReadOnlyList<LodingNode> GetBeforeLoadNodes();
        IReadOnlyList<LodingNode> GetSceneExitLoadedNodes(int sceneIndex);
        IReadOnlyList<LodingNode> GetSceneEnterLoadedNodes(int sceneIndex);
        IReadOnlyList<LodingNode> GetAfterLoadNodes();
    }

    /// <summary>
    /// 로딩 작업 목록만 소유합니다.
    /// 실행 순서와 씬 전환은 <see cref="LodingSystem"/>이 담당합니다.
    /// </summary>
    internal sealed class LodingRegisterSystem :
        ILodingRegisterService,
        ILodingRegisterReader,
        ISystemShutdown,
        IExportSystemServices
    {
        private readonly LodingNodeQueue _beforeLoadNodes = new();
        private readonly LodingNodeQueue _afterLoadNodes = new();
        private readonly Dictionary<int, LodingStateNodes> _nodesBySceneIndex = new();

        #region 서비스

        public void RegisterBeforeLoad(LodingNode node)
        {
            _beforeLoadNodes.Register(node);
        }

        public void RegisterSceneExitLoaded(int sceneIndex, LodingNode node)
        {
            GetStateNodes(sceneIndex).SceneExitLoaded.Register(node);
        }

        public void RegisterSceneEnterLoaded(int sceneIndex, LodingNode node)
        {
            GetStateNodes(sceneIndex).SceneEnterLoaded.Register(node);
        }

        public void RegisterAfterLoad(LodingNode node)
        {
            _afterLoadNodes.Register(node);
        }

        #endregion

        #region 읽기 전용 서비스

        public IReadOnlyList<LodingNode> GetBeforeLoadNodes()
        {
            return _beforeLoadNodes.GetSortedSnapshot();
        }

        public IReadOnlyList<LodingNode> GetSceneExitLoadedNodes(int sceneIndex)
        {
            return TryGetStateNodes(sceneIndex, out LodingStateNodes nodes)
                ? nodes.SceneExitLoaded.GetSortedSnapshot()
                : Array.Empty<LodingNode>();
        }

        public IReadOnlyList<LodingNode> GetSceneEnterLoadedNodes(int sceneIndex)
        {
            return TryGetStateNodes(sceneIndex, out LodingStateNodes nodes)
                ? nodes.SceneEnterLoaded.GetSortedSnapshot()
                : Array.Empty<LodingNode>();
        }

        public IReadOnlyList<LodingNode> GetAfterLoadNodes()
        {
            return _afterLoadNodes.GetSortedSnapshot();
        }

        #endregion

        #region 시스템 모듈

        public void Shutdown()
        {
            _beforeLoadNodes.Clear();
            _afterLoadNodes.Clear();
            _nodesBySceneIndex.Clear();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddService<ILodingRegisterService>(this);
            sink.AddService<ILodingRegisterReader>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        private LodingStateNodes GetStateNodes(int sceneIndex)
        {
            ValidateSceneIndex(sceneIndex);

            if (_nodesBySceneIndex.TryGetValue(sceneIndex, out LodingStateNodes nodes))
                return nodes;

            nodes = new LodingStateNodes();
            _nodesBySceneIndex.Add(sceneIndex, nodes);
            return nodes;
        }

        private bool TryGetStateNodes(
            int sceneIndex,
            out LodingStateNodes nodes)
        {
            ValidateSceneIndex(sceneIndex);
            return _nodesBySceneIndex.TryGetValue(sceneIndex, out nodes);
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

        private sealed class LodingStateNodes
        {
            internal LodingNodeQueue SceneExitLoaded { get; } = new();
            internal LodingNodeQueue SceneEnterLoaded { get; } = new();
        }
    }
}
