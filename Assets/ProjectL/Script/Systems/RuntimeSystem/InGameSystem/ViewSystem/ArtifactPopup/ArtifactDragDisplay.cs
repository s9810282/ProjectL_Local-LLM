using System;
using System.Collections.Generic;
using ProjectL.Framework.Utility;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>드래그 모델과 Core 프리뷰 셀의 표현만 담당합니다. 후보 변경 때만 갱신합니다.</summary>
    internal sealed class ArtifactDragDisplay
    {
        private readonly Transform _root;
        private readonly Transform _storage;
        private readonly ArtifactPopupConfigData _config;
        private readonly ArtifactBoardMapper _mapper;
        private readonly Dictionary<GameObject, CustomPool<GameObject>> _pools = new();
        private readonly List<UnityEngine.UI.Image> _cells = new();
        private readonly CustomPool<UnityEngine.UI.Image> _cellPool;
        private readonly RectTransform _canvas;
        private CustomPool<GameObject> _activePool;
        private GameObject _model;
        private GameObject _prefab;

        internal ArtifactDragDisplay(Transform root, Transform storage, ArtifactPopupConfigData config)
        {
            _root = root;
            _storage = storage;
            _config = config;
            _mapper = new ArtifactBoardMapper(root, config);
            var canvasObject = new GameObject("ArtifactPreviewCells", typeof(RectTransform), typeof(Canvas));
            _canvas = canvasObject.GetComponent<RectTransform>();
            _canvas.SetParent(root, false);
            _canvas.localRotation = Quaternion.Euler(90, 0, 0);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            canvasObject.SetActive(false);
            _cellPool = new CustomPool<UnityEngine.UI.Image>(new PoolContext<UnityEngine.UI.Image>(() =>
            {
                var obj = new GameObject("PreviewCell", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                obj.transform.SetParent(_canvas, false);
                var image = obj.GetComponent<UnityEngine.UI.Image>();
                image.raycastTarget = false;
                obj.SetActive(false);
                return image;
            }, 0));
        }

        internal void Prepare(GameObject prefab, int cellCount)
        {
            if (prefab != null && !_pools.ContainsKey(prefab))
                _pools.Add(prefab, new CustomPool<GameObject>(new PoolContext<GameObject>(() =>
                {
                    GameObject model = UnityEngine.Object.Instantiate(prefab, _storage, false);
                    model.SetActive(false);
                    return model;
                }, 1)));
            // 신규 항목 모양의 최대 셀 수만큼 준비하고 반환합니다. 이동 중에는 재사용합니다.
            while (_cells.Count < cellCount) _cells.Add(_cellPool.Get());
            ClearCells();
        }

        internal void Begin(GameObject prefab)
        {
            Hide();
            _prefab = prefab;
            if (prefab == null) return;
            Prepare(prefab, 0);
            _activePool = _pools[prefab];
            _model = _activePool.Get();
            _model.transform.SetParent(_root, false);
            _model.SetActive(false);
        }

        internal void Show(ArtifactPlacementPreview preview, Rotation4 rotation, int width, int height)
        {
            ClearCells();
            if (preview?.Shape == null)
            {
                if (_model != null) _model.SetActive(false);
                return;
            }
            var shape = preview.Shape.Cells;
            float x = 0, y = 0;
            Color color = preview.FailureReason == ArtifactActionFailureReason.None
                ? _config.PreviewValidColor : _config.PreviewInvalidColor;
            _canvas.gameObject.SetActive(true);
            foreach (CellCoord local in shape)
            {
                CellCoord cell = preview.Anchor + local;
                Vector3 position = _mapper.CellPosition(cell.X, cell.Y, width, height, .03f);
                UnityEngine.UI.Image image = _cellPool.Get();
                _cells.Add(image);
                image.color = color;
                image.rectTransform.localPosition = new Vector3(position.x, position.z, -position.y);
                image.rectTransform.sizeDelta = Vector2.one * (_config.CellSpacing * .9f);
                image.gameObject.SetActive(true);
                x += cell.X; y += cell.Y;
            }
            if (_model == null || shape.Count == 0) return;
            _model.transform.localPosition = _mapper.CellPosition(x / shape.Count, y / shape.Count, width, height, _config.ModelHeight);
            _model.transform.localRotation = Quaternion.Euler(0, (int)rotation * 90f, 0) * _prefab.transform.localRotation;
            _model.transform.localScale = _prefab.transform.localScale * _config.ModelScale;
            _model.SetActive(true);
        }

        internal void ClearPreview()
        {
            ClearCells();
            if (_model != null) _model.SetActive(false);
        }

        internal void Hide()
        {
            ClearPreview();
            GameObject model = _model;
            _model = null;
            if (model != null)
            {
                model.transform.SetParent(_storage, false);
                _activePool.Return(model);
            }
            _activePool = null;
            _prefab = null;
        }

        private void ClearCells()
        {
            for (int i = _cells.Count - 1; i >= 0; i--)
            {
                var image = _cells[i];
                _cells.RemoveAt(i);
                if (image == null) continue;
                image.gameObject.SetActive(false);
                _cellPool.Return(image);
            }
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }
    }
}
