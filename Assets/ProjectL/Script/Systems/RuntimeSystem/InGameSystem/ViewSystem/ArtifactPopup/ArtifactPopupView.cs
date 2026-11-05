using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using UnityEngine;
using UnityEngine.Serialization;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 아티팩트 팝업의 3D 보드·모델·드래그 프리뷰를 그립니다. DB·Buffer 조회는 시스템이 맡습니다.
    /// </summary>
    /// <remarks>UI 목록·버튼·문구는 현강 UI 구조의 <c>ArtifactPopupUI</c>가 맡습니다.</remarks>
    [DisallowMultipleComponent]
    public sealed class ArtifactPopupView : MonoBehaviour, IArtifactPopupPresenter, IArtifactDragPresenter
    {
        [SerializeField] private Transform worldRoot;
        // 팝업 동안 숨길 로비 공간 루트입니다. 저작 칸을 두지 않고 로비 연결이 Configure 때 넘깁니다(로비 리팩토링 2차 설계 M-12).
        private GameObject _lobbyRoot;
        // 보드 전체 바닥 모델 하나입니다. 옛 이름 cellPrefab의 씬 값을 그대로 읽습니다(로비 리팩토링 2차 설계 N-67).
        [FormerlySerializedAs("cellPrefab")]
        [SerializeField] private GameObject floorPrefab;
        private readonly Dictionary<GameObject, CustomPool<GameObject>> _modelPools = new();
        private readonly List<(CustomPool<GameObject> pool, GameObject view)> _models = new();
        private Transform _storage;
        private ArtifactPopupConfigData _config;
        private bool _suppressedLobby;
        private ArtifactBoardMapper _mapper;
        private ArtifactDragDisplay _drag;
        private readonly Dictionary<int, GameObject> _placedModels = new();
        internal Transform WorldRoot => worldRoot;

        internal void Configure(ArtifactPopupConfigData config, GameObject lobbyRoot)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (_config != null && !ReferenceEquals(_config, config))
                throw new InvalidOperationException("[ArtifactPopupView] 표시 설정은 실행 중 바뀌지 않아야 합니다.");
            _config = config;
            _lobbyRoot = lobbyRoot != null ? lobbyRoot : throw new ArgumentNullException(nameof(lobbyRoot));
            if (transform.IsChildOf(lobbyRoot.transform))
                throw new InvalidOperationException("[ArtifactPopupView] 팝업 표시는 숨기는 로비 공간 루트 밖에 있어야 합니다.");
            // 보드 좌표 변환기는 여기서 하나만 만들고 드래그 표시·포인터 판정이 함께 씁니다. 드래그 표시도 이 입구 하나에서 준비합니다
            // (로비 리팩토링 2차 설계 N-02·N-03). 설정은 실행 중 DB 값 하나라 연결이 바뀌어도 같은 객체입니다.
            _mapper ??= new ArtifactBoardMapper(worldRoot, config);
            // 풀 저장소는 조회 멤버가 아니라 이 준비 입구에서 만듭니다(N-64).
            if (_storage == null) _storage = CreateStorage();
            _drag ??= new ArtifactDragDisplay(worldRoot, _storage, config, _mapper);
        }
        /// <summary>보드 칸 ↔ 위치 변환기입니다. <see cref="Configure"/> 뒤에만 있습니다.</summary>
        internal ArtifactBoardMapper BoardMapper => _mapper
            ?? throw new InvalidOperationException("[ArtifactPopupView] Configure 전에는 보드 변환기가 없습니다.");
        private ArtifactDragDisplay Drag => _drag
            ?? throw new InvalidOperationException("[ArtifactPopupView] Configure 전에는 드래그 표시가 없습니다.");
        // 드래그 계약은 명시적 구현 한 방식으로 둡니다(N-68). 숨김이 함께 쓰는 EndDrag만 private 본문을 둡니다.
        void IArtifactDragPresenter.BeginDrag(GameObject prefab, int instanceId, bool placed)
        {
            Drag.Begin(prefab);
            if (placed && _placedModels.TryGetValue(instanceId, out var model)) model.SetActive(false);
        }
        void IArtifactDragPresenter.ClearPreview() => Drag.ClearPreview();
        void IArtifactDragPresenter.ShowPreview(ArtifactPlacementPreview preview, Rotation4 rotation, int width, int height)
            => Drag.Show(preview, rotation, width, height);
        void IArtifactDragPresenter.EndDrag() => EndDrag();
        private void EndDrag()
        {
            // 숨김은 Configure 전(정리 중)에도 불릴 수 있어 드래그 표시가 없으면 건너뜁니다.
            _drag?.Hide();
            foreach (var model in _placedModels.Values) if (model != null) model.SetActive(true);
        }
        internal void Validate()
        {
            if (worldRoot == null || floorPrefab == null)
                throw new InvalidOperationException("[ArtifactPopupView] worldRoot와 바닥 프리팹(floorPrefab) 참조가 필요합니다.");
            if (transform.IsChildOf(worldRoot))
                throw new InvalidOperationException("[ArtifactPopupView] 팝업 표시는 숨기는 보드 루트(worldRoot) 밖에 있어야 합니다.");
        }
        public void Show()
        {
            worldRoot.gameObject.SetActive(true);
            if (_lobbyRoot == null) throw new InvalidOperationException("[ArtifactPopupView] Configure 전에는 표시할 수 없습니다.");
            if (!_suppressedLobby && _lobbyRoot.activeSelf)
            {
                _suppressedLobby = true;
                _lobbyRoot.SetActive(false);
            }
        }
        public void Hide()
        {
            try
            {
                EndDrag();
                ClearBoard();
            }
            finally
            {
                if (worldRoot != null) worldRoot.gameObject.SetActive(false);
                if (_suppressedLobby && _lobbyRoot != null) _lobbyRoot.SetActive(true);
                _suppressedLobby = false;
            }
        }
        void IArtifactPopupPresenter.Render(ArtifactPopupBoard board) => Render(board);
        private void Render(ArtifactPopupBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (_config == null) throw new InvalidOperationException("[ArtifactPopupView] Configure로 View DB 표시 설정을 먼저 받아야 합니다.");
            ClearBoard();
            try
            {
                // 바닥 모델은 4×4 판에 손으로 맞춘 고정 아트입니다. 논리 칸·boardCenter와 상관없이 판 루트 원점에 하나만 놓습니다.
                // boardCenter는 그 위 격자(칸 위치·입력 평면)만 옮깁니다(로비 리팩토링 2차 설계 §4.1, Q-R2-5 C).
                GameObject floor = RentModel(floorPrefab);
                floor.transform.localPosition = Vector3.zero;
                floor.transform.localRotation = floorPrefab.transform.localRotation;
                floor.transform.localScale = floorPrefab.transform.localScale * _config.CellScale;
                floor.SetActive(true);
                for (int i = 0; i < board.PlacedItems.Count; i++)
                {
                    ArtifactPopupItem item = board.PlacedItems[i];
                    if (item.Cells.Count == 0) throw new InvalidOperationException("[ArtifactPopupView] 배치된 아티팩트는 보드 절대 칸이 하나 이상 있어야 합니다.");
                    GameObject model = RentModel(item.ModelPrefab);
                    // 끄는 동안의 드래그 표시와 같은 자세 식 하나를 씁니다(로비 리팩토링 2차 설계 M-09).
                    _mapper.ApplyModelPose(model.transform, item.ModelPrefab, item.Cells, default, item.Rotation,
                        board.Width, board.Height);
                    model.SetActive(true);
                    _placedModels[item.InstanceId] = model;
                    Drag.Prepare(item.ModelPrefab, item.Cells.Count);
                }
                // 보유 목록 UI는 ArtifactPopupUI가 그립니다. 여기서는 드래그 모델만 미리 준비합니다.
                for (int i = 0; i < board.PoolItems.Count; i++)
                    Drag.Prepare(board.PoolItems[i].ModelPrefab, board.PoolItems[i].Cells.Count);
            }
            catch
            {
                ClearBoard();
                throw;
            }
        }
        /// <summary>프리팹별 풀에서 모델을 빌려 판 루트 아래에 둡니다. 꺼진 채 돌려주며, 부르는 쪽이 자세를 정한 뒤 켭니다(옛 순서 유지).</summary>
        private GameObject RentModel(GameObject prefab)
        {
            if (!_modelPools.TryGetValue(prefab, out CustomPool<GameObject> pool))
            {
                pool = new CustomPool<GameObject>(new PoolContext<GameObject>(
                    () => Instantiate(prefab, Storage, false), 0, null, new ModelLifecycle(Storage)));
                _modelPools.Add(prefab, pool);
            }
            GameObject view = pool.Get();
            _models.Add((pool, view));
            view.transform.SetParent(worldRoot, false);
            return view;
        }
        public void ClearBoard()
        {
            // 사본 배열 없이 뒤에서부터 꺼내 돌려줍니다(로비 리팩토링 2차 설계 N-38).
            _placedModels.Clear();
            List<Exception> failures = null;
            for (int i = _models.Count - 1; i >= 0; i--)
            {
                var model = _models[i];
                _models.RemoveAt(i);
                try
                {
                    if (model.view != null) model.pool.Return(model.view);
                }
                catch (Exception error)
                {
                    (failures ??= new()).Add(error);
                }
            }
            if (failures != null) throw new AggregateException("[ArtifactPopupView] 아티팩트 모델 풀 정리 중 실패했습니다.", failures);
        }
        private Transform Storage => _storage
            ?? throw new InvalidOperationException("[ArtifactPopupView] Configure 전에는 모델 풀 저장소가 없습니다.");

        private Transform CreateStorage()
        {
            var obj = new GameObject("ArtifactPopupPoolStorage", typeof(RectTransform));
            obj.transform.SetParent(transform, false);
            obj.SetActive(false);
            return obj.transform;
        }
        private void OnDestroy()
        {
            _models.Clear();
            _modelPools.Clear();
        }
        private sealed class ModelLifecycle : IPoolLifecycle<GameObject>
        {
            private readonly Transform _storage;
            internal ModelLifecycle(Transform storage) => _storage = storage;
            public void OnGet(GameObject item)
            {
            }
            public void OnBeforeReturn(GameObject item)
            {
                item.SetActive(false);
                item.transform.SetParent(_storage, false);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;
            }
            public void OnReturn(GameObject item)
            {
            }
        }
    }
}
