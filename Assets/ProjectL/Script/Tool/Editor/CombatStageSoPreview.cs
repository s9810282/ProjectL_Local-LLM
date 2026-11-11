using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    // SO 모드 3D 미리보기(추가 설계 AD1)입니다. 작업 씬에 오브젝트를 만들지 않도록 PreviewRenderUtility의 따로 된 장면에 놓습니다.
    // 맵 SO는 프리팹만 놓습니다. MapAssembler는 베이크 조명을 붙이며 에디터 전역 LightmapSettings를 바꾸므로 쓰지 않습니다(조명은 참고용).
    // DT 모드 CombatLayoutPreview와 시점 조작은 같지만 코드는 따로 둡니다(추가 계획 P-A15).
    internal sealed class CombatStageSoPreview : IDisposable
    {
        private readonly PreviewRenderUtility preview = new PreviewRenderUtility();
        private readonly GameObject root;
        private readonly Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly List<GameObject> placements = new List<GameObject>();
        private readonly List<(Matrix4x4 Matrix, Color Color)> boxes = new List<(Matrix4x4 Matrix, Color Color)>();
        private GameObject map;
        private string mapKey, placementKey;
        // 바닥 타일 방식이면 필드 칸 → 그 칸 가운데가 놓인 타일입니다(맵을 만들 때 채움). 바닥 교체 외형이 이 타일 자리에 놓입니다.
        private readonly Dictionary<Vector2Int, Transform> cellTiles = new Dictionary<Vector2Int, Transform>();
        // 이 미리보기가 숨긴 타일입니다. 이것만 다시 켭니다(게임 FieldTileReplacement, 설계 P-UT15).
        private readonly HashSet<Transform> hiddenTiles = new HashSet<Transform>();
        private float yaw = 35f, pitch = 55f, distance = 20f;
        private Vector3 pan;

        public CombatStageSoPreview()
        {
            preview.camera.nearClipPlane = .1f;
            preview.camera.farClipPlane = 500f;
            preview.camera.fieldOfView = 40f;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.09f, .11f, .14f);
            preview.lights[0].intensity = 1.1f;
            preview.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            preview.lights[1].intensity = .6f;
            preview.ambientColor = new Color(.4f, .4f, .4f);
            root = new GameObject("CombatStageSoPreview") { hideFlags = HideFlags.HideAndDontSave };
            preview.AddSingleGO(root);
        }

        // 맵을 그리지 못한 이유입니다. 없으면 null입니다.
        public string Problem { get; private set; }

        public void ResetView(CombatStageSoSpace space)
        {
            yaw = 35f; pitch = 55f; pan = Vector3.zero;
            distance = Mathf.Max(8f, Mathf.Max(space.Columns * space.CellWidth, space.Rows * space.CellLength) * 1.5f);
        }

        // 우클릭 끌기 회전, 휠 줌, Shift+우클릭·가운데 버튼 이동, Alt+좌클릭 회전(DT 모드와 같음).
        public bool Navigate(Event e, Rect rect)
        {
            if (!rect.Contains(e.mousePosition)) return false;
            if (e.type == EventType.ScrollWheel)
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(e.delta.y * .07f), 2f, 300f);
                e.Use();
                return true;
            }
            if (e.type != EventType.MouseDrag || !(e.button == 1 || e.button == 2 || (e.button == 0 && e.alt))) return false;
            if (e.shift || e.button == 2)
                pan += (preview.camera.transform.right * -e.delta.x + preview.camera.transform.up * e.delta.y) * distance * .0015f;
            else
            {
                yaw += e.delta.x * .4f;
                pitch = Mathf.Clamp(pitch + e.delta.y * .4f, 10f, 89f);
            }
            e.Use();
            return true;
        }

        // 마우스 광선이 필드 바닥 평면(높이 0)에 닿는 필드 로컬 위치입니다(추가 계획 P-A13).
        public Vector3? PickGround(Rect rect, Vector2 mouse)
        {
            Aim(rect);
            Ray ray = preview.camera.ViewportPointToRay(new Vector3((mouse.x - rect.x) / rect.width, 1f - (mouse.y - rect.y) / rect.height, 0f));
            if (Mathf.Abs(ray.direction.y) < 1e-5f) return null;
            float along = -ray.origin.y / ray.direction.y;
            return along < 0f ? (Vector3?)null : ray.origin + ray.direction * along;
        }

        // 맵과 배치 인스턴스를 작업본에 맞춥니다. 맵은 배경 방식·맵·보정이 바뀔 때만, 배치는 작업본이 바뀔 때만 다시 만듭니다(추가 계획 P-A12).
        // backgroundMode는 배경 방식 이름입니다(CombatStageSoBackground). 이름을 모르면 맵 SO가 있을 때 맵 SO를 씁니다.
        // 바닥 타일은 space.Tiles(맞춤이 성립한 타일 방식)일 때만 놓습니다(설계 P-UT13). 맵 키에 타일 값·필드 크기·칸 간격이 들어갑니다(계획 P-UTT7).
        public void Sync(CombatStageSoDocument document, CombatStageSoGrid grid, CombatStageSoVisuals visuals, CombatStageSoSpace space,
            Func<CombatStageSoLayer, Color> layerColor, string backgroundMode, bool playerOnly = false, Func<object, bool> isShown = null)
        {
            var view = document.view;
            string nextMap = string.Join("|", backgroundMode, view.mapGuid, view.gameObjectGuid, view.positionOffset.ToString("F4"),
                view.rotationOffset.ToString("F4"), view.scaleOffset.ToString("F4"), space.Signature);
            if (nextMap != mapKey)
            {
                mapKey = nextMap;
                RebuildMap(view, backgroundMode, space, visuals.FieldView);
                // 맵과 함께 타일·그 아래 교체 외형이 없어졌으므로 배치를 다시 놓습니다(설계 P-UT15).
                placementKey = null;
            }
            string nextPlacements = JsonUtility.ToJson(document) + space.Signature + playerOnly;
            if (nextPlacements == placementKey) return;
            placementKey = nextPlacements;
            RebuildPlacements(document, grid, visuals, space, layerColor, isShown);
        }

        // '목록 새로고침' 뒤 같은 GUID 에셋의 바뀐 내용(타일 세트 프리팹·높이, 외형)을 다음 그리기에서 다시 만들게 합니다(설계 P-UT17).
        // 시점(회전·거리·이동)은 그대로 둡니다.
        public void Invalidate()
        {
            mapKey = null;
            placementKey = null;
        }

        // 3D를 그리고 그 위에 칸 덧그리기(채우기·테두리)를 화면 좌표로 얹습니다. 덧그리기는 깊이와 상관없이 위에 보입니다.
        public void Draw(Rect rect, CombatStageSoSpace space, IReadOnlyList<(RectInt Cells, Color Color)> fills, IReadOnlyList<(Rect Cells, Color Color)> frames)
        {
            if (Event.current.type != EventType.Repaint) return;
            Aim(rect);
            preview.BeginPreview(rect, GUIStyle.none);
            foreach (var (matrix, color) in boxes) preview.DrawMesh(cube, matrix, MaterialOf(color), 0);
            preview.Render(true);
            GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
            GUI.BeginClip(rect);
            try
            {
                foreach (var (cells, color) in fills) DrawCells(rect, space.Corners(cells, .02f), color, true);
                foreach (var (cells, color) in frames) DrawCells(rect, space.Corners(cells, .03f), color, false);
            }
            finally
            {
                Handles.color = Color.white;
                GUI.EndClip();
            }
        }

        public void Dispose()
        {
            if (root != null) Object.DestroyImmediate(root);
            preview.Cleanup();
            foreach (var material in materials.Values) Object.DestroyImmediate(material);
            materials.Clear();
        }

        private void Aim(Rect rect)
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            preview.camera.transform.SetPositionAndRotation(pan - rotation * Vector3.forward * distance, rotation);
            preview.camera.aspect = rect.width / Mathf.Max(1f, rect.height);
        }

        // StageViewPropPresenter.Show와 같이 타일 방식은 공통 자동 배율, 나머지는 원본 배율 × 보정을 적용합니다.
        // 배경은 방식이 고릅니다(게임 StageViewPropData.GameObject, 설계 §12 UT5): MapDefinition이면 맵 SO의 프리팹, 그 밖은 맵 프리팹입니다.
        // 타일 방식이고 맞춤이 성립하면(space.Tiles) 바닥 타일을 놓고 배경을 맞춥니다(BuildTiles).
        private void RebuildMap(CombatStageSoView view, string backgroundMode, CombatStageSoSpace space, CombatStageSoFieldView fieldView)
        {
            if (map != null) Object.DestroyImmediate(map);
            map = null;
            Problem = null;
            cellTiles.Clear();
            hiddenTiles.Clear();
            bool usesMap = CombatStageSoBackground.Known(backgroundMode)
                ? CombatStageSoBackground.UsesMap(backgroundMode)
                : !string.IsNullOrEmpty(view.mapGuid);
            GameObject prefab = usesMap
                ? (CombatStageSoAssets.Load(view.mapGuid) as MapDefinitionSO)?.Prefab
                : CombatStageSoAssets.Load(view.gameObjectGuid) as GameObject;
            if (prefab == null)
            {
                Problem = usesMap ? "View에 맵 SO(또는 그 프리팹)가 없어 맵을 그리지 않습니다." : "View에 맵 프리팹이 없어 맵을 그리지 않습니다.";
                return;
            }
            Vector3 scale;
            try
            {
                scale = CombatStageSoBackground.UsesTiles(backgroundMode)
                    ? CombatMapScaleEditor.Resolve(prefab, fieldView, view.tiles)
                    : Vector3.Scale(prefab.transform.localScale, view.scaleOffset);
            }
            catch (Exception exception)
            {
                Problem = exception.Message;
                return;
            }
            map = Object.Instantiate(prefab, root.transform, false);
            map.transform.localPosition = view.positionOffset;
            map.transform.localRotation = Quaternion.Euler(view.rotationOffset);
            map.transform.localScale = scale;
            if (space.Tiles != null && CombatStageSoBackground.UsesTiles(backgroundMode)) BuildTiles(space);
            Quiet(map);
        }

        // 타일 생성은 프리뷰가 소유하고, 배경/타일의 축·위치 보정은 운영과 같은 공통 계산을 씁니다.
        // 1) MapTileRoot를 배경 자식으로 offset에 두고 칸마다 타일 → 2) 남은 각만큼 격자 가운데 기준으로 배경 회전 → 3) 기준 타일이 게임 (0,0) 칸에 오게 배경 이동
        // → 4) 필드 칸 → 타일 표. 칸 → 타일 대응은 2D와 같은 맞춤 계산(TryTileOfCell)을 씁니다(설계 P-UT12).
        private void BuildTiles(CombatStageSoSpace space)
        {
            CombatStageSoTileScene tiles = space.Tiles;
            CombatStageSoTileLayout layout = tiles.Layout;
            Transform backdrop = map.transform;
            // 작업 씬을 더럽히지 않게 숨김으로 만들고 바로 미리보기 장면의 배경 아래로 옮깁니다.
            Transform tileRoot = new GameObject("MapTileRoot") { hideFlags = HideFlags.HideAndDontSave }.transform;
            tileRoot.SetParent(backdrop, false);
            tileRoot.localPosition = layout.offset;
            var placed = new Transform[layout.columns, layout.rows];
            var corner = new Vector3(-layout.columns * layout.tileSize.x * .5f, 0f, -layout.rows * layout.tileSize.y * .5f);
            for (int row = 0; row < layout.rows; row++)
            {
                for (int column = 0; column < layout.columns; column++)
                {
                    GameObject prefab = tiles.PrefabAt(column, row);
                    // 세트에 없는 종류·빈 프리팹은 저장 검사가 막습니다. 미리보기는 그 칸만 비웁니다.
                    if (prefab == null) continue;
                    GameObject tile = Object.Instantiate(prefab, tileRoot, false);
                    tile.name = $"Tile_{column}_{row}";
                    tile.transform.localPosition = corner + new Vector3((column + .5f) * layout.tileSize.x, 0f, (row + .5f) * layout.tileSize.y);
                    // StageMapTiles.Build: 칸 회전은 프리팹 자기 회전 뒤에 타일 루트 위 방향으로 곱합니다(설계 §13 TT2·TT8).
                    tile.transform.localRotation = Quaternion.Euler(0f, layout.ResolvedRotationAt(tiles.Seed, column, row) * 90f, 0f) * tile.transform.localRotation;
                    placed[column, row] = tile.transform;
                }
            }
            Transform field = root.transform;
            Vector3 ToField(Vector3 tileLocal) => field.InverseTransformVector(tileRoot.TransformVector(tileLocal));
            var columnLocal = new Vector3(layout.tileSize.x, 0f, 0f);
            var rowLocal = new Vector3(0f, 0f, layout.tileSize.y);
            Vector3 center = field.InverseTransformPoint(tileRoot.position);
            CombatMapPlacementLayout placement = CombatMapPlacementLayout.Align(center, ToField(columnLocal), ToField(rowLocal),
                field.InverseTransformDirection(tileRoot.up), layout.columns, layout.rows, layout.anchorTile,
                new Vector2Int(space.Columns, space.Rows), space.CellWidth, space.CellLength);
            backdrop.localPosition = placement.TransformPoint(backdrop.localPosition);
            backdrop.localRotation = placement.Rotation * backdrop.localRotation;
            for (int y = 0; y < space.Rows; y++)
            {
                for (int x = 0; x < space.Columns; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (tiles.Fit.TryTileOfCell(cell, out int column, out int row) && placed[column, row] != null) cellTiles[cell] = placed[column, row];
                }
            }
        }

        private void RebuildPlacements(CombatStageSoDocument document, CombatStageSoGrid grid, CombatStageSoVisuals visuals,
            CombatStageSoSpace space, Func<CombatStageSoLayer, Color> layerColor, Func<object, bool> isShown)
        {
            foreach (var instance in placements)
                if (instance != null) Object.DestroyImmediate(instance);
            placements.Clear();
            boxes.Clear();
            // 이번 배치에서 숨길 타일입니다. 놓기·지우기·옮기기·정의 변경·Undo가 모두 이 다시 놓기를 거칩니다(설계 P-UT15).
            var wantedHidden = new HashSet<Transform>();
            foreach (var placed in document.Placements(grid))
            {
                if (isShown != null && !isShown(placed.Item)) continue;
                // 레일은 칸 덧그리기로 보입니다.
                if (placed.Layer == CombatStageSoLayer.Rail) continue;
                var visual = visuals.Of(placed.Layer, document.DefinitionOf(placed.Item));
                if (placed.Layer == CombatStageSoLayer.Terrain && visual != null && visual.ReplacesTile)
                {
                    PlaceReplacement(document, placed, visual, space, wantedHidden);
                    continue;
                }
                bool standing = placed.Layer != CombatStageSoLayer.Terrain && placed.Layer != CombatStageSoLayer.Object;
                Vector3 position;
                Quaternion rotation;
                Vector3 scale;
                if (standing)
                {
                    // FieldUnitPresenter.CenterOf·ModelRotation·CreateView: 점유 칸에 서고, 적 유닛은 반 바퀴 돌며, 배율은 프리팹 배율 × fieldUnitScale입니다.
                    position = space.StandingPoint(placed.Cells);
                    rotation = CombatStageSoSpace.Facing(document.RotationOf(placed.Item)) *
                        (placed.Layer == CombatStageSoLayer.Unit ? CombatStageSoSpace.EnemyFacing : Quaternion.identity);
                    scale = visual?.Prefab != null ? visual.Prefab.transform.localScale * visuals.FieldView.UnitScale : Vector3.one;
                }
                else
                {
                    // FieldTerrainPresenter.LocalPosition·FieldObjectPresenter.LocalPosition: 타일 위 지형은 칸 바탕(CellBaseToLocal), 오브젝트는 칸 표면
                    // (CellSurfaceToLocal) + 외형 오프셋입니다. 회전은 외형 회전 × 프리팹 회전, 배율은 프리팹 배율 × 외형 배율입니다.
                    Vector2Int anchor = document.AnchorOf(placed.Item);
                    float height = placed.Layer == CombatStageSoLayer.Terrain ? space.BaseAt(anchor) : space.SurfaceAt(anchor);
                    // 토네이도는 FieldTornadoPresenter.BodyPosition·Create: 중심 기준 자리이고, 프리팹 기준 칸 수에 맞춰 보정과 배율을 함께 키웁니다.
                    string definition = document.DefinitionOf(placed.Item);
                    bool tornado = placed.Layer == CombatStageSoLayer.Terrain && CombatStageSoAssets.IsTornado(definition);
                    float bodyRatio = tornado ? CombatStageSoAssets.TornadoBodyRatio(definition) : 1f;
                    position = (tornado ? space.TornadoPosition(anchor, CombatStageSoAssets.TornadoSideLength(definition))
                        : space.CellCenter(anchor) + Vector3.up * height) + (visual?.LocalOffset ?? Vector3.zero) * bodyRatio;
                    rotation = CombatStageSoSpace.Facing(VisualRotation(document, placed.Item)) *
                        (visual?.Prefab != null ? visual.Prefab.transform.localRotation : Quaternion.identity);
                    scale = visual?.Prefab != null ? Vector3.Scale(visual.Prefab.transform.localScale, visual.LocalScale) * bodyRatio : Vector3.one;
                }
                if (visual?.Prefab == null)
                {
                    // 외형이 없는 정의는 층 색 상자로 그립니다(추가 계획 P-A14).
                    boxes.Add((Matrix4x4.TRS(position + Vector3.up * .3f, rotation, new Vector3(space.CellWidth * .7f, .6f, space.CellLength * .7f)),
                        layerColor(placed.Layer)));
                    continue;
                }
                GameObject instance = Object.Instantiate(visual.Prefab, root.transform, false);
                instance.transform.localPosition = position;
                instance.transform.localRotation = rotation;
                instance.transform.localScale = scale;
                Quiet(instance);
                placements.Add(instance);
            }
            ApplyHiddenTiles(wantedHidden);
        }

        // 바닥 교체 지형(약한 지반·낭떠러지)입니다(설계 P-UT14·P-UT16). 그 칸에 타일이 있으면 숨기고, 외형은 타일과 같은 부모(MapTileRoot) 아래 그 타일 자리에 둡니다
        // (FieldTerrainPresenter.PlaceOnTile: 위치·크기는 타일 루트 기준, 회전은 필드 기준). 타일이 없는 칸은 칸 가운데 바닥면 + 오프셋이고 교체 높이를 더하지 않습니다
        // (LocalPosition·PlaceInField). 프리팹이 없는 교체 외형은 빈 구멍이라 색 상자를 그리지 않습니다. 높이 교체는 칸 공간이 프리팹과 상관없이 합니다.
        private void PlaceReplacement(CombatStageSoDocument document, CombatStageSoPlaced placed, CombatStageSoVisual visual, CombatStageSoSpace space,
            HashSet<Transform> wantedHidden)
        {
            Vector2Int anchor = document.AnchorOf(placed.Item);
            cellTiles.TryGetValue(anchor, out Transform tile);
            if (tile != null) wantedHidden.Add(tile);
            if (visual.Prefab == null) return;
            Quaternion fieldRotation = CombatStageSoSpace.Facing(VisualRotation(document, placed.Item)) * visual.Prefab.transform.localRotation;
            Vector3 scale = Vector3.Scale(visual.Prefab.transform.localScale, visual.LocalScale);
            GameObject instance;
            if (tile != null)
            {
                Transform parent = tile.parent;
                instance = Object.Instantiate(visual.Prefab, parent, false);
                instance.transform.localPosition = tile.localPosition + visual.LocalOffset;
                instance.transform.localRotation = Quaternion.Inverse(parent.rotation) * root.transform.rotation * fieldRotation;
            }
            else
            {
                instance = Object.Instantiate(visual.Prefab, root.transform, false);
                instance.transform.localPosition = space.CellCenter(anchor) + visual.LocalOffset;
                instance.transform.localRotation = fieldRotation;
            }
            instance.transform.localScale = scale;
            Quiet(instance);
            placements.Add(instance);
        }

        // FieldTileReplacement.Apply: 이번에 숨길 타일과 전에 이 미리보기가 숨긴 타일을 비교해, 빠진 것만 다시 켜고 새로 숨길 것만 끕니다.
        // 원래 꺼져 있던 타일은 숨긴 목록에 넣지 않아 켜지 않습니다. 한 타일을 여러 칸이 나누면 교체 지형이 하나라도 남는 동안 숨깁니다.
        private void ApplyHiddenTiles(HashSet<Transform> wanted)
        {
            foreach (var tile in hiddenTiles.Where(tile => !wanted.Contains(tile)).ToList())
            {
                hiddenTiles.Remove(tile);
                if (tile != null) tile.gameObject.SetActive(true);
            }
            foreach (var tile in wanted)
            {
                if (hiddenTiles.Contains(tile) || !tile.gameObject.activeSelf) continue;
                tile.gameObject.SetActive(false);
                hiddenTiles.Add(tile);
            }
        }

        private static int VisualRotation(CombatStageSoDocument document, object item)
        {
            if (!(item is CombatStageSoMarker marker)) return 0;
            var rotations = document.terrains.Contains(marker) ? document.view.terrainRotations : document.view.objectRotations;
            return rotations.Find(entry => entry.sourceId == marker.sourceId)?.rotation ?? 0;
        }

        // 미리보기 인스턴스 안의 HUD 캔버스·카메라·소리를 끄고, 저장되지 않게 표시합니다(추가 계획 P-A11).
        private static void Quiet(GameObject instance)
        {
            foreach (var canvas in instance.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
            foreach (var camera in instance.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
            foreach (var listener in instance.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
            foreach (var source in instance.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
            foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        private void DrawCells(Rect rect, Vector3[] corners, Color color, bool fill)
        {
            // 테두리는 닫힌 선이라 첫 점을 끝에 한 번 더 둡니다.
            var points = new Vector3[corners.Length + 1];
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 viewport = preview.camera.WorldToViewportPoint(corners[i]);
                if (viewport.z <= 0f) return;
                points[i] = new Vector3(viewport.x * rect.width, (1f - viewport.y) * rect.height, 0f);
            }
            points[corners.Length] = points[0];
            Handles.color = color;
            if (fill) Handles.DrawAAConvexPolygon(points[0], points[1], points[2], points[3]);
            else Handles.DrawAAPolyLine(3f, points);
        }

        private Material MaterialOf(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Hidden/Internal-Colored");
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            materials.Add(color, material);
            return material;
        }
    }
}
