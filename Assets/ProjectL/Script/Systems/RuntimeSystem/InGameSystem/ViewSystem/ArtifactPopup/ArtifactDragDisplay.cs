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
        // 앞에서부터 켜 둔 미리보기 칸 수입니다. 칸 이미지는 반환하지 않고 다시 씁니다(로비 리팩토링 2차 설계 N-37).
        private int _shownCells;

        /// <param name="mapper">팝업 표시가 만든 보드 좌표 변환기 하나입니다(로비 리팩토링 2차 설계 N-02).</param>
        internal ArtifactDragDisplay(Transform root, Transform storage, ArtifactPopupConfigData config, ArtifactBoardMapper mapper)
        {
            // GameObject를 만들기 전에 인자를 먼저 검사합니다(로비 리팩토링 2차 설계 N-70).
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _storage = storage != null ? storage : throw new ArgumentNullException(nameof(storage));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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
            // 프리팹을 잡는 생성 함수는 풀이 없을 때만 만듭니다. 이미 있으면 보드 갱신마다 클로저를 할당하지 않습니다(R2-e Codex 검수).
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (!_pools.ContainsKey(prefab))
                _pools.Add(prefab, CreatePool(prefab));
            // 신규 항목 모양의 최대 셀 수만큼 미리 만들어 둡니다. 이동 중에는 다시 씁니다.
            EnsureCells(cellCount);
        }

        private CustomPool<GameObject> CreatePool(GameObject prefab) =>
            new(new PoolContext<GameObject>(() =>
            {
                GameObject model = UnityEngine.Object.Instantiate(prefab, _storage, false);
                model.SetActive(false);
                return model;
            }, 1));

        internal void Begin(GameObject prefab)
        {
            Hide();
            // 모델 프리팹은 외형 표의 필수 칸이라 null이면 Prepare가 예외입니다(로비 재평가 R-M-03).
            Prepare(prefab, 0);
            _prefab = prefab;
            _activePool = _pools[prefab];
            _model = _activePool.Get();
            _model.transform.SetParent(_root, false);
            _model.SetActive(false);
        }

        internal void Show(ArtifactPlacementPreview preview, Rotation4 rotation, int width, int height)
        {
            if (preview?.Shape == null)
            {
                ClearCells();
                if (_model != null) _model.SetActive(false);
                return;
            }
            var shape = preview.Shape.Cells;
            Color color = preview.FailureReason == ArtifactActionFailureReason.None
                ? _config.PreviewValidColor : _config.PreviewInvalidColor;
            // 있는 칸 이미지를 위치·색만 바꿔 다시 쓰고, 남는 칸만 끕니다. 인덱스로 돌아 열거자를 만들지 않습니다(N-37·N-38).
            EnsureCells(shape.Count);
            for (int i = 0; i < shape.Count; i++)
            {
                CellCoord cell = preview.Anchor + shape[i];
                Vector3 position = _mapper.CellPosition(cell.X, cell.Y, width, height, .03f);
                UnityEngine.UI.Image image = _cells[i];
                image.color = color;
                image.rectTransform.localPosition = new Vector3(position.x, position.z, -position.y);
                if (!image.gameObject.activeSelf) image.gameObject.SetActive(true);
            }
            for (int i = shape.Count; i < _shownCells; i++)
                _cells[i].gameObject.SetActive(false);
            _shownCells = shape.Count;
            SetCanvasActive(_shownCells > 0);
            if (_model == null || shape.Count == 0) return;
            // 놓은 뒤 배치 표시와 같은 자세 식 하나를 씁니다(로비 리팩토링 2차 설계 M-09).
            _mapper.ApplyModelPose(_model.transform, _prefab, shape, preview.Anchor, rotation, width, height);
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

        private void EnsureCells(int count)
        {
            while (_cells.Count < count)
            {
                UnityEngine.UI.Image image = _cellPool.Get();
                // 칸 크기는 설정 값 하나라 만들 때 한 번만 넣습니다(N-37).
                image.rectTransform.sizeDelta = Vector2.one * (_config.CellSpacing * .9f);
                image.gameObject.SetActive(false);
                _cells.Add(image);
            }
        }

        private void ClearCells()
        {
            for (int i = 0; i < _shownCells; i++)
            {
                UnityEngine.UI.Image image = _cells[i];
                if (image != null) image.gameObject.SetActive(false);
            }
            _shownCells = 0;
            SetCanvasActive(false);
        }

        private void SetCanvasActive(bool active)
        {
            if (_canvas != null && _canvas.gameObject.activeSelf != active) _canvas.gameObject.SetActive(active);
        }
    }
}
