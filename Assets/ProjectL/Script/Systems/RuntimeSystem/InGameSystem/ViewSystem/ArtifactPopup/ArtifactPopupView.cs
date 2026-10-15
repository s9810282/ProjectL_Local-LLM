using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 아티팩트 팝업의 3D 보드·모델·드래그 프리뷰·World Space 배경을 그립니다. DB·Buffer 조회는 시스템이 맡습니다.
    /// </summary>
    /// <remarks>UI 목록·버튼·문구는 현강 UI 구조의 <c>ArtifactPopupUI</c>가 맡습니다.</remarks>
    [DisallowMultipleComponent]
    public sealed class ArtifactPopupView : MonoBehaviour, IArtifactPopupPresenter
    {
        [SerializeField] private Transform worldRoot;
        [SerializeField] private GameObject backgroundRoot;
        [SerializeField] private GameObject stageMapRoot;
        [SerializeField] private GameObject cellPrefab;
        private readonly Dictionary<GameObject, CustomPool<GameObject>> _modelPools = new();
        private readonly List<(CustomPool<GameObject> pool, GameObject view)> _models = new();
        private Transform _storage;
        private ArtifactPopupConfigData _config;
        private bool _suppressedMap;
        private ArtifactBoardMapper _mapper;
        private ArtifactDragDisplay _drag;
        private ArtifactBackgroundLayout _backgroundLayout;
        private readonly Dictionary<int, GameObject> _placedModels = new();
        internal Transform WorldRoot => worldRoot;
        internal int BorrowedCount => _models.Count;

        internal void Configure(ArtifactPopupConfigData config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            config.Validate();
            _mapper = new ArtifactBoardMapper(worldRoot, config);
        }
        internal ArtifactPopupConfigData Configuration => _config;
        internal void ConnectCamera(Camera camera)
        {
            _backgroundLayout = new ArtifactBackgroundLayout(camera, backgroundRoot.transform as RectTransform, _config);
            _drag ??= new ArtifactDragDisplay(worldRoot, Storage, _config);
        }
        internal void UpdateBackground(bool force = false) => _backgroundLayout?.Update(force);
        internal void BeginDrag(GameObject prefab, int instanceId, bool placed)
        {
            _drag?.Begin(prefab);
            if (placed && _placedModels.TryGetValue(instanceId, out var model)) model.SetActive(false);
        }
        internal void ClearPreview() => _drag?.ClearPreview();
        internal void ShowPreview(ArtifactPlacementPreview preview, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Rotation4 rotation, int width, int height)
            => _drag?.Show(preview, rotation, width, height);
        internal void EndDrag()
        {
            _drag?.Hide();
            foreach (var model in _placedModels.Values) if (model != null) model.SetActive(true);
        }
        internal void Validate()
        {
            if (worldRoot == null || backgroundRoot == null || cellPrefab == null)
                throw new InvalidOperationException("[ArtifactPopupView] World root, background and floor prefab are required.");
            if (transform.IsChildOf(worldRoot) || transform.IsChildOf(backgroundRoot.transform))
                throw new InvalidOperationException("[ArtifactPopupView] Presenter must stay outside hidden roots.");
        }
        public void Show()
        {
            worldRoot.gameObject.SetActive(true);
            backgroundRoot.SetActive(true);
            UpdateBackground(true);
            if (!_suppressedMap && stageMapRoot != null && stageMapRoot.activeSelf)
            {
                _suppressedMap = true;
                stageMapRoot.SetActive(false);
            }
        }
        public void Hide()
        {
            try { EndDrag(); ClearBoard(); }
            finally
            {
                if (worldRoot != null) worldRoot.gameObject.SetActive(false);
                if (backgroundRoot != null) backgroundRoot.SetActive(false);
                if (_suppressedMap && stageMapRoot != null) stageMapRoot.SetActive(true);
                _suppressedMap = false;
            }
        }
        void IArtifactPopupPresenter.Render(ArtifactPopupBoard board) => Render(board);
        internal void Render(ArtifactPopupBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (_config == null) throw new InvalidOperationException("[ArtifactPopupView] View DB layout must be configured.");
            ClearBoard();
            try
            {
                // 전체 배치판 모델은 논리 칸과 별개로 중앙에 하나만 표시합니다.
                GameObject floor = PlaceModel(cellPrefab, board, (board.Width - 1) * .5f, (board.Height - 1) * .5f,
                    0, Quaternion.identity, _config.CellScale);
                floor.transform.localPosition = Vector3.zero;
                foreach (ArtifactPopupItem item in board.PlacedItems)
                {
                    if (item.Cells.Count == 0) throw new InvalidOperationException("Placed item requires absolute cells.");
                    if (item.ModelPrefab == null) continue;
                    float x = 0, y = 0;
                    foreach (var cell in item.Cells) { x += cell.X; y += cell.Y; }
                    _placedModels[item.InstanceId] = PlaceModel(item.ModelPrefab, board, x / item.Cells.Count, y / item.Cells.Count,
                        _config.ModelHeight, Quaternion.Euler(0, (int)item.Rotation * 90f, 0), _config.ModelScale);
                    _drag?.Prepare(item.ModelPrefab, item.Cells.Count);
                }
                // 보유 목록 UI는 ArtifactPopupUI가 그립니다. 여기서는 드래그 모델만 미리 준비합니다.
                foreach (ArtifactPopupItem item in board.PoolItems)
                    _drag?.Prepare(item.ModelPrefab, item.Cells.Count);
            }
            catch { ClearBoard(); throw; }
        }
        private GameObject PlaceModel(GameObject prefab, ArtifactPopupBoard board, float x, float y, float height, Quaternion rotation, float scale)
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
            view.transform.localPosition = _mapper.CellPosition(x, y, board.Width, board.Height, height);
            view.transform.localRotation = rotation * prefab.transform.localRotation;
            view.transform.localScale = prefab.transform.localScale * scale;
            view.SetActive(true);
            return view;
        }
        public void ClearBoard()
        {
            var models = _models.ToArray();
            _models.Clear();
            _placedModels.Clear();
            List<Exception> failures = null;
            foreach (var model in models)
            {
                try { if (model.view != null) model.pool.Return(model.view); }
                catch (Exception error) { (failures ??= new()).Add(error); }
            }
            if (failures != null) throw new AggregateException("Artifact pool cleanup failed.", failures);
        }
        private Transform Storage
        {
            get
            {
                if (_storage == null)
                {
                    var obj = new GameObject("ArtifactPopupPoolStorage", typeof(RectTransform));
                    obj.transform.SetParent(transform, false);
                    obj.SetActive(false);
                    _storage = obj.transform;
                }
                return _storage;
            }
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
            public void OnGet(GameObject item) { }
            public void OnBeforeReturn(GameObject item)
            {
                item.SetActive(false);
                item.transform.SetParent(_storage, false);
                item.transform.localPosition = Vector3.zero;
                item.transform.localRotation = Quaternion.identity;
            }
            public void OnReturn(GameObject item) { }
        }
    }
}
