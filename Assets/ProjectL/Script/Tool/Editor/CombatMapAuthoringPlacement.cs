using System;
using System.Collections.Generic;
using Cinemachine;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>선택한 운영 배경을 씬의 프리팹 인스턴스로 세우고 공용 타일 정렬 계산을 적용합니다.</summary>
    internal static class CombatMapAuthoringPlacement
    {
        internal const string TileRootName = "MapTileRoot";

        internal readonly struct Preview
        {
            internal Preview(GameObject container, GameObject map)
            {
                Container = container;
                Map = map;
            }

            internal GameObject Container { get; }
            internal GameObject Map { get; }
        }

        internal static Preview Create(Transform fieldRoot, GameObject prefab, CombatStageSoView view,
            FbxCombatStageContext context)
        {
            if (fieldRoot == null) throw new ArgumentNullException(nameof(fieldRoot));
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (context == null || !context.IsReady)
                throw new InvalidOperationException("전투 스테이지 맥락이 준비되지 않았습니다.");
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                throw new InvalidOperationException($"{prefab.name}: 배경 원본은 프리팹 에셋이어야 합니다.");

            GameObject container = null;
            GameObject map = null;
            try
            {
                container = new GameObject("StageBackdropPreview");
                SceneManager.MoveGameObjectToScene(container, fieldRoot.gameObject.scene);
                container.transform.SetParent(fieldRoot, false);
                map = PrefabUtility.InstantiatePrefab(prefab, fieldRoot.gameObject.scene) as GameObject;
                if (map == null) throw new InvalidOperationException("배경 프리팹 인스턴스를 만들 수 없습니다.");
                map.transform.SetParent(container.transform, false);
                map.transform.localPosition = view.positionOffset;
                map.transform.localRotation = Quaternion.Euler(view.rotationOffset);
                map.transform.localScale = context.Mode == "PrefabWithTiles"
                    ? CombatMapScaleEditor.Resolve(prefab, context.FieldView, view.tiles)
                    : Vector3.Scale(prefab.transform.localScale, view.scaleOffset);
                foreach (Camera camera in map.GetComponentsInChildren<Camera>(true))
                {
                    camera.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                }
                foreach (CinemachineVirtualCameraBase camera in map.GetComponentsInChildren<CinemachineVirtualCameraBase>(true))
                {
                    camera.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
                }
                foreach (CinemachineImpulseListener listener in map.GetComponentsInChildren<CinemachineImpulseListener>(true))
                {
                    listener.enabled = false;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(listener);
                }
                if (context.Mode == "PrefabWithTiles") BuildTiles(container.transform, map.transform, view.tiles, context);
                Reposition(container.transform, map.transform, fieldRoot, prefab, view, context,
                    view.positionOffset, view.rotationOffset, view.scaleOffset, view.tiles.offset, view.tiles.anchorTile);
                return new Preview(container, map);
            }
            catch
            {
                if (map != null && (container == null || map.transform.parent != container.transform))
                    Object.DestroyImmediate(map);
                if (container != null) Object.DestroyImmediate(container);
                throw;
            }
        }

        private static void BuildTiles(Transform container, Transform backdrop, CombatStageSoTileLayout layout,
            FbxCombatStageContext context)
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

            var tileRoot = new GameObject(TileRootName).transform;
            SceneManager.MoveGameObjectToScene(tileRoot.gameObject, container.gameObject.scene);
            tileRoot.SetParent(container, false);
            tileRoot.SetPositionAndRotation(backdrop.TransformPoint(layout.offset), backdrop.rotation);
            tileRoot.localScale = backdrop.localScale;
            int seed = context.StageKey;
            Vector3 corner = new Vector3(-layout.columns * layout.tileSize.x * .5f, 0f,
                -layout.rows * layout.tileSize.y * .5f);
            for (int row = 0; row < layout.rows; row++)
            for (int column = 0; column < layout.columns; column++)
            {
                if (!kinds.TryGetValue(layout.KindAt(column, row), out CombatStageSoTileKind kind))
                    throw new InvalidOperationException($"타일 ({column}, {row})의 종류가 타일 세트에 없습니다.");
                GameObject tilePrefab = kind.PrefabFor(seed, column, row);
                if (tilePrefab == null)
                    throw new InvalidOperationException($"타일 ({column}, {row})의 프리팹이 없습니다.");
                GameObject tile = Object.Instantiate(tilePrefab, tileRoot, false);
                tile.name = $"Tile_{column}_{row}";
                tile.transform.localPosition = corner + new Vector3((column + .5f) * layout.tileSize.x, 0f,
                    (row + .5f) * layout.tileSize.y);
                tile.transform.localRotation = Quaternion.Euler(0f,
                    layout.ResolvedRotationAt(seed, column, row) * 90f, 0f) * tile.transform.localRotation;
            }

        }

        internal static Transform FindTileRoot(Transform container)
        {
            if (container == null) return null;
            for (int i = 0; i < container.childCount; i++)
                if (container.GetChild(i).name == TileRootName) return container.GetChild(i);
            return null;
        }

        /// <summary>고정된 타일을 기준으로 배경의 월드 X/Z를 옮기는 데 필요한 타일 오프셋입니다.</summary>
        internal static Vector3 TileOffsetForBackdropPosition(Transform backdrop, Vector3 tileOffset, Vector2 worldXZ)
        {
            if (backdrop == null) throw new ArgumentNullException(nameof(backdrop));
            Vector3 current = backdrop.position;
            Vector3 delta = new Vector3(worldXZ.x - current.x, 0f, worldXZ.y - current.z);
            Vector3 local = backdrop.InverseTransformVector(delta);
            float threshold = Mathf.Max(Mathf.Abs(local.x), Mathf.Max(Mathf.Abs(local.y), Mathf.Abs(local.z))) * 0.000001f;
            if (Mathf.Abs(local.x) < threshold) local.x = 0f;
            if (Mathf.Abs(local.y) < threshold) local.y = 0f;
            if (Mathf.Abs(local.z) < threshold) local.z = 0f;
            Vector3 result = tileOffset - local;
            return new Vector3(NormalizeNearZero(result.x), NormalizeNearZero(result.y), NormalizeNearZero(result.z));
        }

        internal static float NormalizeNearZero(float value) => Mathf.Abs(value) < 0.00001f ? 0f : value;

        /// <summary>표시 타일의 실제 월드 간격을 필드 X/Z 축으로 읽습니다.</summary>
        internal static Vector2 TileSpacing(Transform tileRoot, CombatStageSoTileLayout layout)
        {
            if (tileRoot == null) throw new ArgumentNullException(nameof(tileRoot));
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            Vector3 column = tileRoot.TransformVector(new Vector3(layout.tileSize.x, 0f, 0f));
            Vector3 row = tileRoot.TransformVector(new Vector3(0f, 0f, layout.tileSize.y));
            bool columnAlongX = Mathf.Abs(column.x) >= Mathf.Abs(column.z);
            return columnAlongX
                ? new Vector2(new Vector2(column.x, column.z).magnitude, new Vector2(row.x, row.z).magnitude)
                : new Vector2(new Vector2(row.x, row.z).magnitude, new Vector2(column.x, column.z).magnitude);
        }

        /// <summary>프랍 자식 override는 보존하면서 씬 작업 값으로 배경과 표시 타일만 다시 놓습니다.</summary>
        internal static void Reposition(Transform container, Transform backdrop, Transform fieldRoot, GameObject prefab,
            CombatStageSoView view, FbxCombatStageContext context, Vector3 position, Vector3 rotation, Vector3 scale,
            Vector3 tileOffset, Vector2Int anchorTile)
        {
            if (container == null || backdrop == null || fieldRoot == null || prefab == null || view == null || context == null)
                throw new ArgumentNullException(nameof(container));
            if (!Finite(position) || !Finite(rotation) || !Finite(scale) || !Finite(tileOffset))
                throw new InvalidOperationException("배경 보정 값은 유한한 수여야 합니다.");
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
            backdrop.localPosition = position;
            backdrop.localRotation = Quaternion.Euler(rotation);
            backdrop.localScale = context.Mode == "PrefabWithTiles"
                ? CombatMapScaleEditor.Resolve(prefab, context.FieldView, view.tiles)
                : Vector3.Scale(prefab.transform.localScale, scale);
            PrefabUtility.RecordPrefabInstancePropertyModifications(backdrop);
            if (context.Mode != "PrefabWithTiles") return;
            Transform tileRoot = FindTileRoot(container);
            if (tileRoot == null)
                throw new InvalidOperationException("표시용 MapTileRoot가 없습니다. 스테이지 배경을 다시 불러오세요.");
            tileRoot.SetPositionAndRotation(backdrop.TransformPoint(tileOffset), backdrop.rotation);
            tileRoot.localScale = backdrop.localScale;
            ApplyTileAlignment(container, tileRoot, view.tiles, context, fieldRoot, anchorTile);
        }

        private static void ApplyTileAlignment(Transform container, Transform tileRoot, CombatStageSoTileLayout layout,
            FbxCombatStageContext context, Transform fieldRoot, Vector2Int anchorTile)
        {
            // 배경과 타일의 부모만 보정하여 프리팹 루트의 작업 값과 저장 제외 경계를 보존합니다.
            Vector3 columnLocal = new Vector3(layout.tileSize.x, 0f, 0f);
            Vector3 rowLocal = new Vector3(0f, 0f, layout.tileSize.y);
            Vector3 center = fieldRoot.InverseTransformPoint(tileRoot.position);
            Vector3 columnStep = fieldRoot.InverseTransformVector(tileRoot.TransformVector(columnLocal));
            Vector3 rowStep = fieldRoot.InverseTransformVector(tileRoot.TransformVector(rowLocal));
            CombatMapPlacementLayout placement = CombatMapPlacementLayout.Align(center, columnStep, rowStep,
                fieldRoot.InverseTransformDirection(tileRoot.up),
                layout.columns, layout.rows, anchorTile,
                new Vector2Int(context.Field.Columns, context.Field.Rows),
                context.FieldView.CellWidth, context.FieldView.CellLength);
            container.localPosition = placement.Position;
            container.localRotation = placement.Rotation;
            if (placement.FieldExceedsTiles)
                Debug.LogWarning($"[CombatMapAuthoring] 필드가 바닥 타일 맵 밖으로 넘칩니다. StageKey: {context.StageKey}", container);
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
