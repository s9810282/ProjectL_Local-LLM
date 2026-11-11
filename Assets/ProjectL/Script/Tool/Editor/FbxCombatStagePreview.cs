using System;
using System.Collections.Generic;
using Cinemachine;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>작업 중인 맵의 표시용 복사본과 타일을 별도 가상 씬에 놓고 공용 시점으로 그립니다.</summary>
    internal sealed class FbxCombatStagePreview : IDisposable
    {
        private readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest();
        private Scene _scene;
        private GameObject _source;
        private Vector3 _sourceScale;
        private GameObject _copy;
        private GameObject _backdrop;
        private readonly Dictionary<Transform, Transform> _sourceByCopy = new Dictionary<Transform, Transform>();
        private Camera _camera;
        private RenderTexture _texture;
        private int _width;
        private int _height;

        internal bool Sync(GameObject source, CombatStageSoView view, FbxCombatStageContext context)
        {
            if (source == null || view == null || context == null || !context.IsReady)
                throw new InvalidOperationException("전투 미리보기에는 열린 맵과 운영 스테이지 맥락이 필요합니다.");
            if (_source != source || _copy == null || _sourceScale != source.transform.localScale)
            {
                Rebuild(source, view, context);
                return true;
            }
            bool changed = false;
            if (!SyncTree(source.transform, _copy.transform, true, ref changed))
            {
                Rebuild(source, view, context);
                return true;
            }
            return changed;
        }

        internal RenderTexture Render(FbxCombatCameraSource.Frame frame, int width, int height)
        {
            if (_copy == null || _camera == null) return null;
            width = Mathf.Clamp(width, 64, 2048);
            height = Mathf.Clamp(height, 64, 2048);
            if (_texture == null || _width != width || _height != height)
            {
                if (_texture != null) Object.DestroyImmediate(_texture);
                var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
                {
                    msaaSamples = 1,
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                _texture = new RenderTexture(descriptor)
                {
                    name = "전투 공용 구도 미리보기",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (!_texture.Create()) throw new InvalidOperationException("전투 미리보기 RenderTexture 생성에 실패했습니다.");
                _width = width;
                _height = height;
            }

            _camera.transform.SetPositionAndRotation(frame.StartPosition, frame.StartRotation);
            _camera.orthographic = true;
            _camera.orthographicSize = frame.InitialSize;
            _camera.nearClipPlane = frame.NearClip;
            _camera.farClipPlane = frame.FarClip;
            _camera.aspect = (float)width / height;
            _camera.targetTexture = _texture;
            RenderTexture previous = RenderTexture.active;
            try
            {
                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                {
                    _request.destination = _texture;
                    RenderPipeline.SubmitRenderRequest(_camera, _request);
                }
                else _camera.Render();
                return _texture;
            }
            finally { RenderTexture.active = previous; }
        }

        internal void Rebuild(GameObject source, CombatStageSoView view, FbxCombatStageContext context)
        {
            ReleaseScene();
            _scene = EditorSceneManager.NewPreviewScene();
            try
            {
                _backdrop = new GameObject("전투 배경 미리보기");
                SceneManager.MoveGameObjectToScene(_backdrop, _scene);
                _copy = Object.Instantiate(source);
                SceneManager.MoveGameObjectToScene(_copy, _scene);
                _copy.name = source.name;
                _copy.transform.SetParent(_backdrop.transform, false);
                _copy.transform.localPosition = Vector3.zero;
                _copy.transform.localRotation = Quaternion.identity;
                _copy.transform.localScale = Vector3.one;
                MapTree(source.transform, _copy.transform);
                _backdrop.transform.localPosition = view.positionOffset;
                _backdrop.transform.localRotation = Quaternion.Euler(view.rotationOffset);
                _backdrop.transform.localScale = context.Mode == "PrefabWithTiles"
                    ? CombatMapScaleEditor.Resolve(source, context.FieldView, view.tiles)
                    : Vector3.Scale(source.transform.localScale, view.scaleOffset);
                foreach (Camera mapCamera in _copy.GetComponentsInChildren<Camera>(true)) mapCamera.enabled = false;
                foreach (CinemachineVirtualCamera virtualCamera in _copy.GetComponentsInChildren<CinemachineVirtualCamera>(true))
                    virtualCamera.enabled = false;
                if (context.Mode == "PrefabWithTiles") BuildTiles(view.tiles, context);

                var cameraObject = new GameObject("전투 구도 렌더 카메라", typeof(Camera), typeof(UniversalAdditionalCameraData));
                SceneManager.MoveGameObjectToScene(cameraObject, _scene);
                _camera = cameraObject.GetComponent<Camera>();
                _camera.enabled = false;
                _camera.cameraType = CameraType.Preview;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = new Color(.08f, .08f, .08f, 1f);
                _camera.scene = _scene;
                _camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(_scene);
                cameraObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                _source = source;
                _sourceScale = source.transform.localScale;
            }
            catch
            {
                ReleaseScene();
                throw;
            }
        }

        private void BuildTiles(CombatStageSoTileLayout layout, FbxCombatStageContext context)
        {
            if (layout == null || !layout.HasFullKinds || !layout.HasValidRotations)
                throw new InvalidOperationException("바닥 타일 배치의 칸·회전 수가 맞지 않습니다.");
            var kinds = new Dictionary<int, CombatStageSoTileKind>();
            foreach (CombatStageSoTileKind kind in CombatStageSoAssets.ReadTileSet(layout.tileSetGuid))
            {
                if (kinds.ContainsKey(kind.Value))
                    throw new InvalidOperationException($"바닥 타일 종류 {kind.Value}가 중복됩니다.");
                kinds.Add(kind.Value, kind);
            }
            var root = new GameObject("MapTileRoot").transform;
            SceneManager.MoveGameObjectToScene(root.gameObject, _scene);
            root.SetParent(_backdrop.transform, false);
            root.localPosition = layout.offset;
            int seed = context.StageKey; // 에디터 참고용 고정 시드. 런타임의 무작위 외형 선택과는 다를 수 있습니다.
            Vector3 corner = new Vector3(-layout.columns * layout.tileSize.x * .5f, 0f,
                -layout.rows * layout.tileSize.y * .5f);
            for (int row = 0; row < layout.rows; row++)
            for (int column = 0; column < layout.columns; column++)
            {
                if (!kinds.TryGetValue(layout.KindAt(column, row), out CombatStageSoTileKind kind))
                    throw new InvalidOperationException($"타일 ({column}, {row})의 종류가 타일 세트에 없습니다.");
                GameObject prefab = kind.PrefabFor(seed, column, row);
                if (prefab == null) throw new InvalidOperationException($"타일 ({column}, {row})의 프리팹이 없습니다.");
                GameObject tile = Object.Instantiate(prefab, root, false);
                tile.name = $"Tile_{column}_{row}";
                tile.transform.localPosition = corner + new Vector3((column + .5f) * layout.tileSize.x, 0f,
                    (row + .5f) * layout.tileSize.y);
                tile.transform.localRotation = Quaternion.Euler(0f,
                    layout.ResolvedRotationAt(seed, column, row) * 90f, 0f) * tile.transform.localRotation;
            }

            // 표시 씬의 필드 루트는 월드 원점입니다. 배경과 타일을 동일한 순서로 회전·이동합니다.
            Vector3 columnStep = root.TransformVector(new Vector3(layout.tileSize.x, 0f, 0f));
            Vector3 center = root.position;
            Vector3 rowStep = root.TransformVector(new Vector3(0f, 0f, layout.tileSize.y));
            CombatMapPlacementLayout placement = CombatMapPlacementLayout.Align(center, columnStep, rowStep, root.up,
                layout.columns, layout.rows, layout.anchorTile,
                new Vector2Int(context.Field.Columns, context.Field.Rows),
                context.FieldView.CellWidth, context.FieldView.CellLength);
            _backdrop.transform.position = placement.TransformPoint(_backdrop.transform.position);
            _backdrop.transform.rotation = placement.Rotation * _backdrop.transform.rotation;
        }

        private void MapTree(Transform source, Transform copy)
        {
            _sourceByCopy.Add(copy, source);
            for (int i = 0; i < source.childCount; i++)
                MapTree(source.GetChild(i), copy.GetChild(i));
        }

        private bool SyncTree(Transform source, Transform copy, bool root, ref bool changed)
        {
            if (source.childCount != copy.childCount || source.name != copy.name
                || !_sourceByCopy.TryGetValue(copy, out Transform expected) || expected != source) return false;
            if (source.gameObject.activeSelf != copy.gameObject.activeSelf)
            {
                copy.gameObject.SetActive(source.gameObject.activeSelf);
                changed = true;
            }
            if (!root)
            {
                if (copy.localPosition != source.localPosition) { copy.localPosition = source.localPosition; changed = true; }
                if (copy.localRotation != source.localRotation) { copy.localRotation = source.localRotation; changed = true; }
                if (copy.localScale != source.localScale) { copy.localScale = source.localScale; changed = true; }
            }
            for (int i = 0; i < source.childCount; i++)
                if (!SyncTree(source.GetChild(i), copy.GetChild(i), false, ref changed)) return false;
            return true;
        }

        private void ReleaseScene()
        {
            _source = null;
            _sourceByCopy.Clear();
            _copy = null;
            _backdrop = null;
            _camera = null;
            if (_scene.IsValid() && _scene.isLoaded) EditorSceneManager.ClosePreviewScene(_scene);
            _scene = default;
        }

        public void Dispose()
        {
            if (_texture != null) Object.DestroyImmediate(_texture);
            _texture = null;
            _width = _height = 0;
            ReleaseScene();
        }
    }
}
