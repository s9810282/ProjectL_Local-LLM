using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // SO 모드의 배경 방식·바닥 타일 화면입니다(설계 §12 UT2·UT3). 값과 계산은 CombatStageSoTiles.cs, 에셋 읽기·쓰기는 CombatStageSoAssets가 맡습니다.
    public sealed partial class CombatLayoutWindow
    {
        // 한 축의 타일 수 상한입니다. 실수로 큰 값을 넣어 창이 멈추지 않게 합니다(게임에는 상한이 없음).
        private const int SoMaxTileCount = 128;
        private const float SoTileSize = 28, SoTileBandHeight = 6;
        private const int SoTilePaint = 0, SoTileRotate = 1, SoTileAnchor = 2;
        // 선택 모드(SoTileRotate)는 고른 타일을 돌리고 복사·붙여넣기합니다(설계 §19 Q-TC1 A).
        private static readonly string[] SoTileModes = { "칠하기", "선택", "기준 타일" };
        private static readonly string[] SoTileAngles = { "0°", "90°", "180°", "270°" };
        private static readonly Color SoTileOutside = new Color(.08f, .08f, .09f), SoTileFieldFrame = new Color(.95f, .95f, .95f);
        private static readonly Color SoTileExceedFrame = new Color(1f, .3f, .3f), SoTileAnchorFrame = new Color(1f, .85f, .2f);
        // 고른 타일 테두리(상자 선택 BoxColor와 구분되는 밝은 하늘색)와 타일 앞쪽 띠입니다(계획 P-TTT8).
        private static readonly Color SoTileSelectedFrame = new Color(.6f, .95f, 1f), SoTileFront = new Color(0f, 0f, 0f, .6f);
        // 타일 세트 GUID → 종류 줄입니다. '목록 새로고침'이 비웁니다(설계 P-UT17).
        private readonly Dictionary<string, List<CombatStageSoTileKind>> soTileKinds = new Dictionary<string, List<CombatStageSoTileKind>>();
        // 복사한 타일입니다(설계 §19 TC1). 스테이지를 바꿔도 남습니다.
        [SerializeField] private CombatStageSoTileClip soTileClip = new CombatStageSoTileClip();
        // 칠할 종류(열거 값)와 '타일' 보기 도구(칠하기 / 선택 / 기준 타일, 계획 P-TTT5)입니다.
        [SerializeField] private int soTileBrush;
        [SerializeField] private int soTileMode;
        private Vector2 soTileScroll;
        // 끌기 한 번의 마지막 타일과 Shift+끌기 사각형입니다. 창에 남기지 않습니다.
        private Vector2Int soTileLast = new Vector2Int(-1, -1);
        private bool soTileBoxing, soTileBoxAdd;
        private Vector2Int soTileBoxStart, soTileBoxEnd;
        // 고른 타일(배열 좌표)입니다. '타일' 보기와 2D·3D '타일 회전' 도구가 같이 씁니다(설계 TT5·TT9).
        // 창에만 두고 직렬화하지 않습니다. 스테이지·가로·세로·세트가 바뀌면 비웁니다(계획 P-TTT4).
        private readonly HashSet<Vector2Int> soTileSelection = new HashSet<Vector2Int>();
        private string soTileSelectionKey;
        // 2D 맵과 고른 칸 줄이 같이 쓰는 타일 대응입니다(설계 P-UT12). 맵을 그릴 때마다 만들고, 타일 방식이 아니면 null입니다.
        private SoTileBand soTileBand;
        // 표시 전용 시드입니다. 다시 그리기·보기 전환·작업본 저장과 독립적이며 명시적으로 다시 뽑을 때만 바뀝니다.
        private int _soTilePreviewSeed = Guid.NewGuid().GetHashCode();

        private sealed class SoTileBand
        {
            public CombatStageSoTileFit Fit;
            public CombatStageSoTileLayout Tiles;
            public Dictionary<int, CombatStageSoTileKind> Kinds;
            // 바닥 교체 지형이 놓인 칸입니다. 게임은 그 칸의 타일을 숨깁니다(FieldTileReplacement).
            public HashSet<Vector2Int> Replaced;
            public string Error;
        }

        private string SoBackgroundMode(CombatStageSoView view) => CombatStageSoAssets.NameAt(soCatalog.Names.viewMode, view.mode);

        // 바닥 작업 영역 왼쪽의 종류 선택입니다. 값 쓰기는 기존 칠하기 명령이 맡습니다.
        private void DrawSoTilePalette()
        {
            EditorGUILayout.LabelField("타일 종류", EditorStyles.boldLabel);
            if (!soDocument.IsOpen || !soDocument.HasView)
            {
                EditorGUILayout.HelpBox("스테이지와 짝 View가 필요합니다.", MessageType.Info);
                return;
            }
            if (!CombatStageSoBackground.UsesTiles(SoBackgroundMode(soDocument.view)))
            {
                EditorGUILayout.HelpBox("배경 설정에서 '프리팹 + 바닥 타일' 방식을 고르면 종류를 칠할 수 있습니다.", MessageType.Info);
                return;
            }
            var tiles = soDocument.view.tiles;
            EditorGUILayout.LabelField("현재 세트", CombatStageSoAssets.NameOf(tiles.tileSetGuid), EditorStyles.wordWrappedMiniLabel);
            var kinds = SoTileKinds(tiles.tileSetGuid);
            if (kinds.Count == 0)
            {
                EditorGUILayout.HelpBox("오른쪽 타일 구성에서 타일을 만들거나 타일 세트를 고르세요.", MessageType.Info);
                return;
            }
            if (kinds.All(kind => kind.Value != soTileBrush)) soTileBrush = kinds[0].Value;
            foreach (var kind in kinds)
            {
                Color previous = GUI.backgroundColor;
                if (soTileBrush == kind.Value) GUI.backgroundColor = SelectedButton;
                bool clicked = GUILayout.Button(kind.Name);
                GUI.backgroundColor = previous;
                if (!clicked) continue;
                CancelSoWorkspaceInput();
                soTileBrush = kind.Value; soTileMode = SoTilePaint;
                Repaint(); GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField("종류를 누르면 칠하기 도구로 전환합니다.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawSoTileInspector()
        {
            if (!soDocument.HasView) { DrawSoView(); return; }
            var view = soDocument.view;
            if (!CombatStageSoBackground.UsesTiles(SoBackgroundMode(view)))
            {
                EditorGUILayout.HelpBox("바닥 타일은 '프리팹 + 바닥 타일' 방식에서 편집합니다. 배경 설정에서 방식을 고르세요.", MessageType.Info);
                DrawSoSettingsLink("배경 설정 열기", SoInspectorSection.Background);
                return;
            }
            SyncSoTileSelection(view.tiles);
            EditorGUILayout.LabelField("선택 타일", $"{soTileSelection.Count}칸");
            EditorGUILayout.LabelField("복사한 타일", $"{soTileClip.cells.Count}칸");
            DrawSoTileLayout(view);
        }

        // 배경 방식과 그 방식이 쓰는 칸입니다. 방식이 쓰지 않는 칸은 지우지 않고, 값이 남아 있다고만 알립니다(설계 P-UT4).
        private void DrawSoBackground(CombatStageSoView view)
        {
            string[] names = soCatalog.Names.viewMode;
            if (names == null) EditorGUILayout.LabelField("배경 방식", $"(값 {view.mode})");
            else
            {
                EditorGUI.BeginChangeCheck();
                int next = EditorGUILayout.Popup("배경 방식", view.mode, names.Select(CombatStageSoBackground.Label).ToArray());
                if (EditorGUI.EndChangeCheck() && next != view.mode) SoChange("배경 방식 변경", () => view.mode = next);
            }
            string mode = SoBackgroundMode(view);
            bool known = CombatStageSoBackground.Known(mode);
            bool usesMap = CombatStageSoBackground.UsesMap(mode);
            bool usesTiles = CombatStageSoBackground.UsesTiles(mode);
            if (!known || usesMap)
                if (SoObject<MapDefinitionSO>("맵 SO", view.mapGuid, out string map)) SoChange("맵 SO 변경", () => view.mapGuid = map);
            if (!known || !usesMap)
                if (SoObject<GameObject>("맵 프리팹", view.gameObjectGuid, out string prefab)) SoChange("맵 프리팹 변경", () => view.gameObjectGuid = prefab);
            if (!known)
            {
                EditorGUILayout.HelpBox("배경 방식을 알 수 없어 맵 SO·맵 프리팹 칸을 모두 보입니다. 목록 새로고침을 눌러 보세요.", MessageType.Warning);
                return;
            }
            if (usesMap) SoNote("맵 SO의 프리팹과 베이크 정보로 배경을 세웁니다.");
            if (usesTiles) SoNote("맵 프리팹의 구멍을 아래 바닥 타일로 채웁니다. 위치 보정은 높이(y)만 쓰고 가로(x·z)는 기준 타일이 정합니다. " +
                                  "회전 보정 Y는 90° 단위로 맞춰지고, X·Z가 0이 아니면 전투에 들어갈 때 멈춥니다.");
            if (usesMap && !string.IsNullOrEmpty(view.gameObjectGuid))
                SoNote($"맵 프리팹 값({CombatStageSoAssets.NameOf(view.gameObjectGuid)})이 남아 있지만 이 방식은 쓰지 않습니다.");
            if (!usesMap && !string.IsNullOrEmpty(view.mapGuid))
                SoNote($"맵 SO 값({CombatStageSoAssets.NameOf(view.mapGuid)})이 남아 있지만 이 방식은 쓰지 않습니다.");
            if (!usesTiles && !view.tiles.IsEmpty) SoNote("바닥 타일 값이 남아 있지만 이 방식은 쓰지 않습니다.");
        }

        // '바닥 타일' 묶음입니다(설계 §12 UT3). 타일 방식일 때만 보입니다. 배치 자체의 오류(길이·세트 없음·없는 종류)는 저장 검사가 막습니다.
        private void DrawSoTileLayout(CombatStageSoView view)
        {
            if (!CombatStageSoBackground.UsesTiles(SoBackgroundMode(view))) return;
            GUILayout.Space(4);
            EditorGUILayout.LabelField("바닥 타일", EditorStyles.boldLabel);
            var tiles = view.tiles;
            if (tiles.IsEmpty)
            {
                EditorGUILayout.HelpBox("바닥 타일 배치가 비어 있습니다. 이 방식은 배치가 있어야 저장됩니다.", MessageType.Warning);
                if (GUILayout.Button("타일 만들기")) { CreateSoTiles(view); GUIUtility.ExitGUI(); }
                return;
            }
            if (SoAsset("타일 세트", tiles.tileSetGuid, soCatalog.TileSets, out string set)) SoChange("타일 세트 변경", () => tiles.tileSetGuid = set);
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(tiles.tileSetGuid)))
            {
                if (GUILayout.Button("타일 세트를 Inspector에서 보기"))
                {
                    var asset = CombatStageSoAssets.Load(tiles.tileSetGuid);
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                }
            }
            List<CombatStageSoTileKind> kinds = SoTileKinds(tiles.tileSetGuid);
            if (SoInt("가로 칸 수", tiles.columns, out int columns)) ResizeSoTiles(tiles, columns, tiles.rows, kinds);
            if (SoInt("세로 칸 수", tiles.rows, out int rows)) ResizeSoTiles(tiles, tiles.columns, rows, kinds);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Vector2Field("타일 제작 규격 (공통)", CombatMapAuthoringMetrics.TileSize);
            if (SoVector3("타일 위치(MapTileRoot)", tiles.offset, out Vector3 offset)) SoChange("타일 위치 변경", () => tiles.offset = offset);
            if (SoVector2Int("기준 타일", tiles.anchorTile, out Vector2Int anchor)) SoChange("기준 타일 변경", () => tiles.anchorTile = anchor);
            SoNote("가로·세로는 배경 구멍의 최대 크기(필드 크기와 따로)입니다. 공통 제작 규격의 타일과 배경을 운영 필드 간격에 같은 배율로 맞춥니다. 칸 높이·바닥 교체 대상은 칸 중심 아래 타일입니다. " +
                   "기준 타일은 게임 (0,0) 칸이 놓일 타일 칸이고, 화면 기준 좌하단 타일이 (0,0)입니다(배경 회전과 무관). 칸마다 종류는 '바닥 타일' 작업 영역에서 칠합니다.");
            DrawSoTileKinds(tiles, kinds);
            DrawSoTileFitMessages(SoTileFit(view, out _));
            // 자동 변경 감지는 없습니다(설계 P-UT17).
            SoNote("타일 세트·필드 보기를 Inspector에서 고쳤으면 위 '목록 새로고침'을 눌러야 반영됩니다. 저장하지 않은 편집은 그대로 남습니다.");
        }

        #region '타일' 보기

        // 맵의 '타일' 보기입니다(설계 §12 UT4). 타일 맵 전체를 화면(필드) 방향으로 그리고, 붓으로 종류를 칠하거나 기준 타일을 옮깁니다.
        private void DrawSoTileView()
        {
            var view = soDocument.view;
            if (!soDocument.HasView)
            {
                EditorGUILayout.HelpBox("짝 View가 없어 바닥 타일이 없습니다.", MessageType.Info);
                return;
            }
            string mode = SoBackgroundMode(view);
            // 타일 방식이 아니면 안내만 합니다(계획 P-UTT3). 방식은 배경 설정에서 바꿉니다.
            if (!CombatStageSoBackground.UsesTiles(mode))
            {
                EditorGUILayout.HelpBox($"이 스테이지의 배경 방식은 '{CombatStageSoBackground.Label(mode)}'입니다. 바닥 타일은 '프리팹 + 바닥 타일' 방식에서만 씁니다. '배경·타일 설정 열기'에서 바꿀 수 있습니다.",
                    MessageType.Info);
                return;
            }
            var tiles = view.tiles;
            if (tiles.IsEmpty)
            {
                EditorGUILayout.HelpBox("바닥 타일 배치가 비어 있습니다. 오른쪽 타일 구성의 '타일 만들기'를 누르세요.", MessageType.Info);
                return;
            }
            List<CombatStageSoTileKind> kinds = SoTileKinds(tiles.tileSetGuid);
            CombatStageSoTileFit fit = SoTileFit(view, out string error);
            if (fit == null)
            {
                EditorGUILayout.HelpBox(error ?? "바닥 타일을 계산하지 못했습니다.", MessageType.Error);
                return;
            }
            SyncSoTileSelection(tiles);
            DrawSoTileBrush(tiles, fit, kinds);
            DrawSoTileViewLegend(kinds);
            if (!fit.HasOrientation || !tiles.HasFullKinds)
            {
                EditorGUILayout.HelpBox("칸 종류 수가 가로 × 세로와 달라 타일 맵을 그릴 수 없습니다. 오른쪽 '바닥 타일'에서 가로·세로를 다시 넣으세요.", MessageType.Warning);
                return;
            }
            var names = SoTileKindNames(kinds);
            using (var scroll = new EditorGUILayout.ScrollViewScope(soTileScroll, GUILayout.ExpandHeight(true)))
            {
                soTileScroll = scroll.scrollPosition;
                Rect map = GUILayoutUtility.GetRect(fit.ScreenColumns * SoTileSize, fit.ScreenRows * SoTileSize, GUILayout.ExpandWidth(false));
                for (int y = 0; y < fit.ScreenRows; y++)
                {
                    for (int x = 0; x < fit.ScreenColumns; x++)
                    {
                        fit.ScreenToArray(x, y, out int column, out int row);
                        int kind = tiles.KindAt(column, row);
                        int rotationOption = tiles.RotationAt(column, row);
                        int rotation = tiles.ResolvedRotationAt(_soTilePreviewSeed, column, row);
                        Rect rect = SoTileRect(map, fit.ScreenRows, new RectInt(x, y, 1, 1));
                        EditorGUI.DrawRect(rect, SoTileColor(kind));
                        DrawSoTileFront(rect, fit.ScreenFrontOf(rotation));
                        // 화면 각은 프리팹 원래 모습에서 돈 양이라 0°면 글자를 붙이지 않습니다(설계 TT7).
                        int angle = fit.ScreenAngleOf(rotation);
                        string label = names.TryGetValue(kind, out string name) ? name : $"?{kind}";
                        bool randomRotation = rotationOption == CombatStageSoTileLayout.RandomRotation;
                        string rotationLabel = randomRotation ? "?" : $"{angle}°";
                        string tooltip = randomRotation ? $"{label} · Random (예시 {angle}°)" : $"{label} · 회전 {angle}°";
                        GUI.Label(rect, new GUIContent(angle == 0 && !randomRotation ? label : $"{label}\n{rotationLabel}", tooltip), EditorStyles.whiteMiniLabel);
                        if (soTileSelection.Contains(new Vector2Int(column, row))) DrawSoRectFrame(rect, SoTileSelectedFrame);
                    }
                }
                // 필드 자리는 맞춤이 성립할 때만 그립니다(설계 UT10·P-UT13). 넘치면 보이는 부분만 빨갛게 그립니다.
                if (fit.IsValid)
                {
                    Rect field = fit.FieldTiles;
                    var visible = Rect.MinMaxRect(field.xMin, field.yMin, Mathf.Min(field.xMax, fit.ScreenColumns), Mathf.Min(field.yMax, fit.ScreenRows));
                    DrawSoRectFrame(SoTileRect(map, fit.ScreenRows, visible), fit.FieldExceedsTiles ? SoTileExceedFrame : SoTileFieldFrame);
                }
                Vector2Int anchor = fit.Anchor;
                if (anchor.x >= 0 && anchor.y >= 0 && anchor.x < fit.ScreenColumns && anchor.y < fit.ScreenRows)
                    DrawSoRectFrame(SoTileRect(map, fit.ScreenRows, new RectInt(anchor.x, anchor.y, 1, 1)), SoTileAnchorFrame);
                if (soTileBoxing) DrawSoRectFrame(SoTileRect(map, fit.ScreenRows, SoTileBox(soTileBoxStart, soTileBoxEnd)), BoxColor);
                HandleSoTilePointer(map, fit, tiles, kinds);
            }
            if (soTileMode == SoTileRotate)
                SoNote("클릭: 하나 고르기 · 끌기: 지나간 타일 고르기 · Ctrl+클릭·끌기: 더하기(클릭은 빼기도) · Shift+끌기: 사각형 · Ctrl+Shift+끌기: 사각형 더하기 · " +
                       "Ctrl+A: 모두 · Esc: 비우기 · R / Shift+R: 고정 각만 시계 / 반시계 90°(Random 유지) · Ctrl+C: 복사 · Ctrl+V: 고른 타일의 왼쪽 아래를 기준으로 붙여넣기(다른 스테이지도 가능). " +
                       "화면 아래쪽이 필드 −Z, 왼쪽이 −X입니다.");
            else
                SoNote("좌클릭·끌기: 칠하기 · Shift+끌기: 사각형 칠하기 · '기준 타일': 누른 타일에 게임 (0,0) 칸이 놓입니다. 화면 아래쪽이 필드 −Z, 왼쪽이 −X입니다.");
            DrawSoTileFitMessages(fit);
        }

        private void DrawSoTileBrush(CombatStageSoTileLayout tiles, CombatStageSoTileFit fit, List<CombatStageSoTileKind> kinds)
        {
            EditorGUI.BeginChangeCheck();
            int mode = GUILayout.Toolbar(Mathf.Clamp(soTileMode, 0, SoTileModes.Length - 1), SoTileModes);
            if (EditorGUI.EndChangeCheck())
            {
                CancelSoWorkspaceInput(); soTileMode = mode;
                Repaint(); GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField(SoTileToolHint(), EditorStyles.wordWrappedMiniLabel);
            DrawSoHelpLink("바닥 타일 사용법", SoHelpTopic.Tiles);
            if (soTileMode == SoTilePaint)
                using (new EditorGUILayout.HorizontalScope())
                {
                    int index = Mathf.Max(0, kinds.FindIndex(kind => kind.Value == soTileBrush));
                    EditorGUILayout.LabelField(kinds.Count == 0 ? "타일 세트에 종류가 없습니다." : "붓 · " + kinds[index].Name, EditorStyles.wordWrappedMiniLabel);
                    using (new EditorGUI.DisabledScope(kinds.Count == 0))
                        if (GUILayout.Button("모두 이 종류로", GUILayout.Width(100))) FillSoTiles(tiles);
                }
            if (soTileMode != SoTileRotate)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSoTileRandomPreviewButton();
                    GUILayout.FlexibleSpace();
                }
            }
            if (soTileMode == SoTileRotate)
            {
                DrawSoTileRotateBar(tiles, fit);
                if (fit.HasOrientation && tiles.HasFullKinds) DrawSoTileClipBar(tiles, fit);
            }
        }

        private void DrawSoTileClipBar(CombatStageSoTileLayout tiles, CombatStageSoTileFit fit)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(soTileSelection.Count == 0))
                    if (GUILayout.Button("복사 (Ctrl+C)", GUILayout.Width(100))) CopySoTiles(fit, tiles);
                bool hasClip = soTileClip.cells.Count > 0;
                using (new EditorGUI.DisabledScope(!hasClip || soTileSelection.Count == 0))
                    if (GUILayout.Button(hasClip ? $"붙여넣기 (Ctrl+V) · {soTileClip.cells.Count}칸" : "붙여넣기 (Ctrl+V)", GUILayout.Width(170))) PasteSoTiles(fit, tiles);
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.LabelField("바닥 타일의 선택 모드에서는 타일 종류·회전만 복사합니다. 배치 복사와 클립보드가 분리되며 기믹 설정은 담지 않습니다. 검사는 필요 없습니다. 숫자·글자 입력 중에는 격자를 눌러 포커스를 옮긴 뒤 Ctrl+C/V를 사용하세요.", EditorStyles.wordWrappedMiniLabel);
            if (soTileSelection.Count == 0) EditorGUILayout.LabelField("선택한 타일이 없습니다. 격자에서 복사할 타일 또는 붙일 목적지를 고르세요.", EditorStyles.wordWrappedMiniLabel);
            if (soTileClip.cells.Count == 0) EditorGUILayout.LabelField("붙여넣을 타일이 없습니다. 선택한 타일을 먼저 복사하세요.", EditorStyles.wordWrappedMiniLabel);
        }

        // 도구 줄과 빠른 사용법이 같은 현재 타일 모드 안내를 사용합니다.
        private string SoTileToolHint()
        {
            switch (soTileMode)
            {
                case SoTilePaint: return "칠하기: 종류를 고른 뒤 클릭·끌기 · Shift+끌기: 사각형 · 종류·회전 복사는 선택 모드에서";
                case SoTileRotate: return "선택: 클릭·끌기 / Ctrl로 추가 / Shift+끌기로 사각형 · R/Shift+R: 회전 · Ctrl+C/V: 타일 복사·붙여넣기 · Ctrl+A: 전체 · Esc: 취소";
                default: return "기준 타일: 클릭한 타일에 게임 (0,0) 칸이 놓이도록 지정 · 타일 복사는 선택 모드에서";
            }
        }

        // 고른 칸만 화면 기준으로 담습니다. 작업본을 바꾸지 않아 Undo가 없습니다(설계 §19 TC1·TC2).
        private void CopySoTiles(CombatStageSoTileFit fit, CombatStageSoTileLayout tiles)
        {
            if (soTileSelection.Count == 0)
            {
                SetSoNotice("복사할 타일을 먼저 고르세요.", MessageType.Warning);
                return;
            }
            Vector2Int origin = SoTileSelectionOrigin(fit);
            var clip = new CombatStageSoTileClip();
            foreach (Vector2Int tile in soTileSelection)
            {
                int rotation = tiles.RotationAt(tile.x, tile.y);
                clip.cells.Add(new CombatStageSoTileClipCell
                {
                    offset = fit.ArrayToScreen(tile.x, tile.y) - origin,
                    kind = tiles.KindAt(tile.x, tile.y),
                    rotation = rotation == CombatStageSoTileLayout.RandomRotation ? rotation : fit.ScreenAngleOf(rotation) / 90
                });
            }
            soTileClip = clip;
            SetSoNotice($"타일 {clip.cells.Count}칸을 복사했습니다. 붙일 자리의 타일을 고르고 붙여넣으세요(다른 스테이지도 됩니다).");
        }

        // 고른 타일의 왼쪽 아래를 기준으로 붙입니다. 한 번 = Undo 한 번이고, 바뀌는 칸만 씁니다(설계 §19 TC3~TC5, Q-TC2 A).
        private void PasteSoTiles(CombatStageSoTileFit fit, CombatStageSoTileLayout tiles)
        {
            if (soTileClip.cells.Count == 0)
            {
                SetSoNotice("복사한 타일이 없습니다.", MessageType.Warning);
                return;
            }
            if (soTileSelection.Count == 0)
            {
                SetSoNotice("붙일 자리의 타일을 먼저 고르세요.", MessageType.Warning);
                return;
            }
            Vector2Int origin = SoTileSelectionOrigin(fit);
            var known = new HashSet<int>(SoTileKinds(tiles.tileSetGuid).Select(kind => kind.Value));
            bool square = Mathf.Approximately(tiles.tileSize.x, tiles.tileSize.y);
            var pasted = new List<(Vector2Int Tile, int Kind, int Rotation)>();
            int outside = 0, unknown = 0, flattened = 0;
            foreach (CombatStageSoTileClipCell cell in soTileClip.cells)
            {
                Vector2Int screen = origin + cell.offset;
                if (screen.x >= fit.ScreenColumns || screen.y >= fit.ScreenRows) { outside++; continue; }
                if (!known.Contains(cell.kind)) { unknown++; continue; }
                int rotation = cell.rotation == CombatStageSoTileLayout.RandomRotation ? cell.rotation : fit.StoredOf(cell.rotation);
                if (!square && rotation % 2 == 1) { rotation = 0; flattened++; }
                fit.ScreenToArray(screen.x, screen.y, out int column, out int row);
                pasted.Add((new Vector2Int(column, row), cell.kind, rotation));
            }
            var changed = pasted.Where(item => tiles.KindAt(item.Tile.x, item.Tile.y) != item.Kind || tiles.RotationAt(item.Tile.x, item.Tile.y) != item.Rotation).ToList();
            if (changed.Count > 0)
                SoChange("바닥 타일 붙여넣기", () =>
                {
                    foreach (var (tile, kind, rotation) in changed)
                    {
                        tiles.SetKind(tile.x, tile.y, kind);
                        if (tiles.RotationAt(tile.x, tile.y) != rotation) tiles.SetRotation(tile.x, tile.y, rotation);
                    }
                });
            PickSoTiles(pasted.Select(item => item.Tile), false);
            var notes = new List<string>();
            if (outside > 0) notes.Add($"타일 맵 밖 {outside}칸 버림");
            if (unknown > 0) notes.Add($"세트에 없는 종류 {unknown}칸 건너뜀");
            if (flattened > 0) notes.Add($"가로 ≠ 세로 타일이라 {flattened}칸을 0°로 둠");
            string message = changed.Count > 0 ? $"바닥 타일 붙여넣기: {changed.Count}칸 바뀜" : "바닥 타일 붙여넣기: 바뀌는 타일이 없습니다";
            SetSoNotice(notes.Count > 0 ? $"{message} ({string.Join(", ", notes)})" : message, notes.Count > 0 ? MessageType.Warning : MessageType.Info);
        }

        // 고른 타일 묶음의 왼쪽 아래 화면 자리입니다.
        private Vector2Int SoTileSelectionOrigin(CombatStageSoTileFit fit)
        {
            var screens = soTileSelection.Select(tile => fit.ArrayToScreen(tile.x, tile.y)).ToList();
            return new Vector2Int(screens.Min(screen => screen.x), screens.Min(screen => screen.y));
        }

        private void DrawSoTileViewLegend(List<CombatStageSoTileKind> kinds)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var kind in kinds)
                    DrawSoSwatch(SoTileColor(kind.Value), $"{kind.Name}({kind.SurfaceHeight:0.###})");
                DrawSoSwatch(SoTileFieldFrame, "필드 자리");
                DrawSoSwatch(SoTileAnchorFrame, "기준 타일");
                DrawSoSwatch(SoTileFront, "검은 띠 = 프리팹 앞(+Z) 쪽");
                GUILayout.Label("? = Random(검은 띠는 예시 각)", EditorStyles.miniLabel);
                if (soTileMode == SoTileRotate) DrawSoSwatch(SoTileSelectedFrame, "고른 타일");
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawSoTileRandomPreviewButton()
        {
            if (!GUILayout.Button("랜덤 다시 뽑기", GUILayout.Width(112))) return;
            _soTilePreviewSeed = Guid.NewGuid().GetHashCode();
            soPreview?.Invalidate();
            Repaint();
        }

        // 타일 칸 안쪽, 앞쪽(화면 방향) 변에 굵은 띠를 그립니다(계획 P-TTT8).
        private static void DrawSoTileFront(Rect rect, Vector2Int front)
        {
            const float thick = 4;
            Rect band = front.y > 0 ? new Rect(rect.x, rect.y, rect.width, thick)
                : front.y < 0 ? new Rect(rect.x, rect.yMax - thick, rect.width, thick)
                : front.x > 0 ? new Rect(rect.xMax - thick, rect.y, thick, rect.height)
                : new Rect(rect.x, rect.y, thick, rect.height);
            EditorGUI.DrawRect(band, SoTileFront);
        }

        #region 타일 고르기 · 돌리기 (설계 §13 TT5·TT6, '타일' 보기와 2D·3D '타일 회전' 도구가 같이 씀)

        // 고른 타일이 지금 배치와 맞는지 봅니다. 스테이지·가로·세로·세트가 바뀌면 비우고, Undo 등으로 범위 밖이 된 칸은 버립니다(계획 P-TTT4).
        private void SyncSoTileSelection(CombatStageSoTileLayout tiles)
        {
            string key = $"{soDocument.viewGuid}|{tiles.columns}x{tiles.rows}|{tiles.tileSetGuid}";
            if (key != soTileSelectionKey)
            {
                soTileSelectionKey = key;
                soTileSelection.Clear();
                return;
            }
            soTileSelection.RemoveWhere(tile => tile.x < 0 || tile.y < 0 || tile.x >= tiles.columns || tile.y >= tiles.rows);
        }

        // 고른 타일을 돌리는 줄입니다. 각은 모두 화면에서 본 시계 방향입니다(계획 P-TTT7).
        private void DrawSoTileRotateBar(CombatStageSoTileLayout tiles, CombatStageSoTileFit fit)
        {
            bool square = Mathf.Approximately(tiles.tileSize.x, tiles.tileSize.y);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"고른 타일 {soTileSelection.Count}개", GUILayout.Width(100));
                using (new EditorGUI.DisabledScope(soTileSelection.Count == 0))
                {
                    using (new EditorGUI.DisabledScope(!square))
                    {
                        if (GUILayout.Button("↺ 90°", GUILayout.Width(56))) TurnSoTiles(tiles, fit, -1);
                        if (GUILayout.Button("↻ 90°", GUILayout.Width(56))) TurnSoTiles(tiles, fit, 1);
                    }
                }
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUI.DisabledScope(soTileSelection.Count == 0))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("각 지정", GUILayout.Width(44));
                    for (int quarter = 0; quarter < SoTileAngles.Length; quarter++)
                        using (new EditorGUI.DisabledScope(!square && quarter % 2 == 1))
                            if (GUILayout.Button(SoTileAngles[quarter], GUILayout.Width(44))) SetSoTilesAngle(tiles, fit, quarter);
                    GUILayout.FlexibleSpace();
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(soTileSelection.Count == 0))
                    if (GUILayout.Button("Random", GUILayout.Width(68)))
                        ApplySoTileRotations(tiles, "바닥 타일 Random 지정", _ => CombatStageSoTileLayout.RandomRotation);
                GUILayout.FlexibleSpace();
                DrawSoTileRandomPreviewButton();
            }
            // 가로 ≠ 세로 타일의 90°·270°는 게임이 막습니다(Q-TT3 A).
            if (!square)
            {
                int odd = tiles.HasRotations ? tiles.rotations.Count(rotation => rotation == 1 || rotation == 3) : 0;
                EditorGUILayout.HelpBox($"타일 크기가 가로 ≠ 세로({tiles.tileSize})라 0°·180°만 쓸 수 있습니다. Random도 둘 중에서 고릅니다." +
                                        (odd > 0 ? $" 90°·270°인 타일 {odd}개는 저장 검사가 막습니다." : ""), odd > 0 ? MessageType.Warning : MessageType.Info);
            }
            if (!tiles.HasValidRotations)
                EditorGUILayout.HelpBox($"회전 목록 {tiles.rotations.Length}개가 가로 × 세로({tiles.columns} × {tiles.rows})와 달라 저장 검사가 막습니다. " +
                                        "타일을 하나 돌리면 0°로 다시 채워집니다.", MessageType.Warning);
        }

        // 고른 타일을 화면에서 본 시계 방향으로 quarters × 90° 돌립니다. 한 번 = Undo 한 번입니다.
        private void TurnSoTiles(CombatStageSoTileLayout tiles, CombatStageSoTileFit fit, int quarters) =>
            ApplySoTileRotations(tiles, quarters > 0 ? "바닥 타일 시계 방향 90°" : "바닥 타일 반시계 방향 90°",
                rotation => fit.Turned(rotation, quarters));

        // 고른 타일을 화면에서 본 각 quarter × 90°로 둡니다.
        private void SetSoTilesAngle(CombatStageSoTileLayout tiles, CombatStageSoTileFit fit, int quarter) =>
            ApplySoTileRotations(tiles, $"바닥 타일 {SoTileAngles[quarter]}", _ => fit.StoredOf(quarter));

        private void ApplySoTileRotations(CombatStageSoTileLayout tiles, string label, Func<int, int> next)
        {
            if (!Mathf.Approximately(tiles.tileSize.x, tiles.tileSize.y)
                && soTileSelection.Any(tile => next(tiles.RotationAt(tile.x, tile.y)) % 2 == 1))
            {
                SetSoNotice("타일 크기가 가로 ≠ 세로라 90°·270°로 돌릴 수 없습니다.", MessageType.Warning);
                return;
            }
            var changed = soTileSelection
                .Select(tile => (Tile: tile, Rotation: next(tiles.RotationAt(tile.x, tile.y))))
                .Where(pair => pair.Rotation != tiles.RotationAt(pair.Tile.x, pair.Tile.y))
                .ToList();
            if (changed.Count == 0)
            {
                SetSoNotice($"{label}: 바뀌는 타일이 없습니다.");
                return;
            }
            SoChange(label, () =>
            {
                foreach (var (tile, rotation) in changed) tiles.SetRotation(tile.x, tile.y, rotation);
            });
            SetSoNotice($"{label}: {changed.Count}개");
        }

        // 고르기 입력 하나입니다. add면 더하고(toggle이면 이미 있을 때 빼고), 아니면 이 타일만 남깁니다. 작업본을 바꾸지 않아 Undo가 없습니다.
        private void PickSoTile(Vector2Int tile, bool add, bool toggle)
        {
            if (!add) soTileSelection.Clear();
            if (toggle && soTileSelection.Contains(tile)) soTileSelection.Remove(tile);
            else soTileSelection.Add(tile);
        }

        private void PickSoTiles(IEnumerable<Vector2Int> tiles, bool add)
        {
            if (!add) soTileSelection.Clear();
            soTileSelection.UnionWith(tiles);
        }

        // 고른 타일 키 입력입니다(설계 TT5·TT6). Ctrl+A는 SelectAll 명령으로 받습니다(계획 P-TTT6). 처리하면 true입니다.
        private bool HandleSoTileSelectionKeys(Event e, CombatStageSoTileLayout tiles, CombatStageSoTileFit fit, Func<IEnumerable<Vector2Int>> all)
        {
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == "SelectAll" && GUIUtility.keyboardControl == 0)
            {
                if (e.type == EventType.ExecuteCommand) PickSoTiles(all(), false);
                e.Use(); Repaint();
                return true;
            }
            if (e.type != EventType.KeyDown || GUIUtility.keyboardControl != 0) return false;
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    soTileSelection.Clear();
                    break;
                case KeyCode.R when !e.control && !e.command && !e.alt && fit != null:
                    if (soTileSelection.Count == 0) SetSoNotice("돌릴 타일을 먼저 고르세요.");
                    else TurnSoTiles(tiles, fit, e.shift ? -1 : 1);
                    break;
                default:
                    return false;
            }
            e.Use(); Repaint();
            return true;
        }

        #endregion

        #region 2D·3D '타일 회전' 도구 (설계 §13 TT9)

        // 끌기 한 번의 마지막 칸과 Shift+끌기 칸 사각형입니다. '타일' 보기의 화면 타일 좌표와 섞이지 않게 따로 둡니다.
        private Vector2Int soTileCellLast = new Vector2Int(-1, -1);
        private bool soTileCellBoxing;
        private Vector2Int soTileCellBoxStart, soTileCellBoxEnd;

        // 도구 줄 아래 회전 줄입니다. 타일 방식이 아니거나 맞춤이 안 되면 안내만 합니다.
        private void DrawSoTileToolBar()
        {
            if (!SoTileToolReady(out string reason))
            {
                EditorGUILayout.HelpBox(reason, MessageType.Info);
                return;
            }
            SyncSoTileSelection(soTileBand.Tiles);
            DrawSoTileRotateBar(soTileBand.Tiles, soTileBand.Fit);
        }

        // 칸 → 타일 고르기를 할 수 있는지입니다(칸 → 타일 대응은 맞춤이 성립할 때만, 설계 P-UT13).
        private bool SoTileToolReady(out string reason)
        {
            reason = null;
            if (soTileBand == null) reason = "이 스테이지는 바닥 타일 방식이 아니거나 배치가 비어 있어 '타일 회전' 도구를 쓸 수 없습니다.";
            else if (soTileBand.Fit == null) reason = soTileBand.Error ?? "바닥 타일을 계산하지 못했습니다.";
            else if (!soTileBand.Fit.IsValid) reason = "바닥 타일 맞춤 문제가 있어 칸의 타일을 찾을 수 없습니다(맵 아래 알림). '타일' 보기에서는 고르고 돌릴 수 있습니다.";
            return reason == null;
        }

        private void PressSoTileCell(Vector2Int cell, bool ctrl, bool shift)
        {
            // 3D는 판 밖 칸도 옵니다(설계 §14 TO2). 판 밖이면 오른쪽 '고른 칸'은 바꾸지 않습니다.
            if (cell.x >= 0 && cell.y >= 0 && cell.x < soField.Columns && cell.y < soField.Rows) { soSelectedCell = cell; showSoCell = true; }
            if (!SoTileToolReady(out string reason))
            {
                SetSoNotice(reason);
                return;
            }
            SyncSoTileSelection(soTileBand.Tiles);
            if (shift)
            {
                soTileCellBoxing = true; soTileBoxAdd = ctrl;
                soTileCellBoxStart = soTileCellBoxEnd = cell;
                return;
            }
            soTileCellLast = cell;
            if (soTileBand.Fit.TryTileOfCell(cell, out int column, out int row)) PickSoTile(new Vector2Int(column, row), ctrl, ctrl);
            else
            {
                if (!ctrl) soTileSelection.Clear();
                SetSoNotice("이 자리에는 바닥 타일이 없습니다(타일 맵 밖).");
            }
        }

        private void DragSoTileCell(Vector2Int? cell, Vector2Int? clamped)
        {
            if (!SoTileToolReady(out _)) return;
            if (soTileCellBoxing)
            {
                if (clamped is Vector2Int end) soTileCellBoxEnd = end;
                return;
            }
            // 끌기는 지나간 칸의 타일을 더합니다. 타일 없는 칸은 건너뜁니다.
            if (cell is Vector2Int over && over != soTileCellLast)
            {
                soTileCellLast = over;
                if (soTileBand.Fit.TryTileOfCell(over, out int column, out int row)) PickSoTile(new Vector2Int(column, row), true, false);
            }
        }

        private void ReleaseSoTileCell()
        {
            soTileCellLast = new Vector2Int(-1, -1);
            if (!soTileCellBoxing) return;
            soTileCellBoxing = false;
            if (!SoTileToolReady(out _)) return;
            if (SoTileCellBox() is RectInt box) PickSoTiles(SoTileArraysIn(soTileBand.Fit, box), soTileBoxAdd);
        }

        // 끄는 칸 사각형의 두 끝을 타일 화면 좌표로 바꿔, 타일 맵 안으로 붙인 타일 사각형입니다(설계 §14 TO3).
        // 3D에서 판 밖으로 크게 끌어도 타일 수만큼만 돕니다. 사각형이 타일 맵과 겹치지 않으면 null입니다.
        private RectInt? SoTileCellBox()
        {
            CombatStageSoTileFit fit = soTileBand.Fit;
            Vector2Int a = fit.ScreenTileOfCell(soTileCellBoxStart), b = fit.ScreenTileOfCell(soTileCellBoxEnd);
            int xMin = Mathf.Max(0, Mathf.Min(a.x, b.x)), yMin = Mathf.Max(0, Mathf.Min(a.y, b.y));
            int xMax = Mathf.Min(fit.ScreenColumns - 1, Mathf.Max(a.x, b.x)), yMax = Mathf.Min(fit.ScreenRows - 1, Mathf.Max(a.y, b.y));
            if (xMax < xMin || yMax < yMin) return null;
            return new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
        }

        // '타일 회전' 도구 키입니다. Esc는 끄는 중인 사각형을 먼저 그만둡니다. Ctrl+A는 필드가 덮는 타일 전부입니다(계획 P-TTT10).
        private void HandleSoTileToolKeys(Event e)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && soTileCellBoxing)
            {
                soTileCellBoxing = false;
                GUIUtility.hotControl = 0;
                e.Use(); Repaint();
                return;
            }
            if (!soDocument.IsOpen || !SoTileToolReady(out _)) return;
            // Ctrl+A: 3D는 판 밖도 고를 수 있어 타일 맵 전부, 2D는 판 위 타일만입니다(설계 §14 Q-TO1 A).
            var tiles = soTileBand.Tiles;
            HandleSoTileSelectionKeys(e, tiles, soTileBand.Fit, () => soMapView == SoView3D ? AllSoTiles(tiles) : FieldSoTiles());
        }

        // 필드 칸이 놓인 타일 전부(배열 좌표)입니다.
        private IEnumerable<Vector2Int> FieldSoTiles()
        {
            var tiles = new HashSet<Vector2Int>();
            for (int y = 0; y < soField.Rows; y++)
                for (int x = 0; x < soField.Columns; x++)
                    if (soTileBand.Fit.TryTileOfCell(new Vector2Int(x, y), out int column, out int row)) tiles.Add(new Vector2Int(column, row));
            return tiles;
        }

        // 2D·3D 공용 테두리 목록에 고른 타일이 덮는 칸과 끄는 사각형을 더합니다. '타일 회전' 도구일 때만 보입니다(계획 P-TTT11).
        // clip이면 판 안으로 자르고(2D), 아니면 판 밖 타일도 그대로 그립니다(3D, 설계 §14 TO4).
        private void AddSoTileFrames(List<(Rect Cells, Color Color)> frames, bool clip)
        {
            if (soTool != SoTool.TileRotate || !SoTileToolReady(out _)) return;
            CombatStageSoTileFit fit = soTileBand.Fit;
            foreach (var tile in soTileSelection)
                AddSoTileFrame(frames, fit.CellsOfScreenTile(fit.ArrayToScreen(tile.x, tile.y)), SoTileSelectedFrame, clip);
            // 끄는 사각형은 타일 맵 안으로 붙인 타일 사각형이 덮는 칸입니다(TO3).
            if (soTileCellBoxing && SoTileCellBox() is RectInt box)
            {
                Rect first = fit.CellsOfScreenTile(box.min);
                Rect last = fit.CellsOfScreenTile(box.max - Vector2Int.one);
                AddSoTileFrame(frames, Rect.MinMaxRect(first.xMin, first.yMin, last.xMax, last.yMax), BoxColor, clip);
            }
        }

        private void AddSoTileFrame(List<(Rect Cells, Color Color)> frames, Rect cells, Color color, bool clip)
        {
            if (!clip) frames.Add((cells, color));
            else if (soTileBand.Fit.TryClipToField(cells, out Rect inside)) frames.Add((inside, color));
        }

        #endregion

        // '타일' 보기 입력입니다. 끌기 한 번 = Undo 한 번(BeginSoStroke·EndStroke), 바뀌는 칸이 없으면 Undo를 남기지 않습니다.
        private void HandleSoTilePointer(Rect map, CombatStageSoTileFit fit, CombatStageSoTileLayout tiles, List<CombatStageSoTileKind> kinds)
        {
            int control = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            switch (e.GetTypeForControl(control))
            {
                case EventType.MouseDown when e.button == 0 && !e.alt && SoTileAt(map, fit, e.mousePosition, false) is Vector2Int down:
                    GUI.FocusControl(null);
                    GUIUtility.hotControl = control;
                    bool ctrl = e.control || e.command;
                    if (soTileMode == SoTileAnchor) MoveSoTileAnchor(tiles, down);
                    else if (soTileMode == SoTileRotate)
                    {
                        // 회전 모드는 고르기만 합니다(설계 TT5 표). 작업본을 바꾸지 않아 Undo가 없습니다.
                        if (e.shift) { soTileBoxing = true; soTileBoxAdd = ctrl; soTileBoxStart = soTileBoxEnd = down; }
                        else
                        {
                            PickSoTile(SoTileArrayOf(fit, down), ctrl, ctrl);
                            soTileLast = down;
                        }
                    }
                    else if (kinds.Count == 0) SetSoNotice("타일 세트에 종류가 없어 칠할 수 없습니다.", MessageType.Warning);
                    else if (e.shift) { soTileBoxing = true; soTileBoxStart = soTileBoxEnd = down; }
                    else
                    {
                        BeginSoStroke();
                        PaintSoTile(fit, tiles, down);
                        soTileLast = down;
                    }
                    e.Use(); GUIUtility.ExitGUI();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    if (soTileBoxing)
                    {
                        if (SoTileAt(map, fit, e.mousePosition, true) is Vector2Int end) soTileBoxEnd = end;
                    }
                    else if (soTileMode == SoTileRotate && SoTileAt(map, fit, e.mousePosition, false) is Vector2Int passed && passed != soTileLast)
                    {
                        // 끌기는 지나간 타일을 더합니다(누른 타일에서 시작한 고르기에 이어 붙음).
                        PickSoTile(SoTileArrayOf(fit, passed), true, false);
                        soTileLast = passed;
                    }
                    else if (soTileMode == SoTilePaint && kinds.Count > 0 && SoTileAt(map, fit, e.mousePosition, false) is Vector2Int over && over != soTileLast)
                    {
                        PaintSoTile(fit, tiles, over);
                        soTileLast = over;
                    }
                    e.Use(); Repaint();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    if (soTileBoxing)
                    {
                        soTileBoxing = false;
                        RectInt box = SoTileBox(soTileBoxStart, soTileBoxEnd);
                        if (soTileMode == SoTileRotate) PickSoTiles(SoTileArraysIn(fit, box), soTileBoxAdd);
                        else PaintSoTileBox(fit, tiles, box);
                    }
                    EndStroke(); soTileLast = new Vector2Int(-1, -1);
                    e.Use(); GUIUtility.ExitGUI();
                    break;
            }
        }

        // '타일' 보기 키 입력입니다. Esc는 끄는 중인 사각형을 먼저 그만둡니다. 회전 모드는 고르기·돌리기 키를 받고(설계 TT5·TT6),
        // 배치 단축키는 받지 않습니다.
        private void HandleSoTileKeys(Event e)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape && soTileBoxing)
            {
                soTileBoxing = false;
                GUIUtility.hotControl = 0;
                e.Use(); Repaint();
                return;
            }
            if (soTileMode != SoTileRotate || !soDocument.HasView) return;
            var tiles = soDocument.view.tiles;
            if (!CombatStageSoBackground.UsesTiles(SoBackgroundMode(soDocument.view)) || !tiles.HasFullKinds) return;
            CombatStageSoTileFit fit = SoTileFit(soDocument.view, out _);
            // Ctrl+C·Ctrl+V는 에디터가 명령으로 보냅니다. 입력 칸에 커서가 있으면 그 칸의 복사입니다(설계 §19 TC6).
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && (e.commandName == "Copy" || e.commandName == "Paste")
                && GUIUtility.keyboardControl == 0 && fit != null && fit.HasOrientation)
            {
                if (e.type == EventType.ExecuteCommand)
                {
                    if (e.commandName == "Copy") CopySoTiles(fit, tiles);
                    else PasteSoTiles(fit, tiles);
                }
                e.Use(); Repaint();
                return;
            }
            HandleSoTileSelectionKeys(e, tiles, fit, () => AllSoTiles(tiles));
        }

        private static IEnumerable<Vector2Int> AllSoTiles(CombatStageSoTileLayout tiles)
        {
            for (int row = 0; row < tiles.rows; row++)
                for (int column = 0; column < tiles.columns; column++)
                    yield return new Vector2Int(column, row);
        }

        private static Vector2Int SoTileArrayOf(CombatStageSoTileFit fit, Vector2Int screen)
        {
            fit.ScreenToArray(screen.x, screen.y, out int column, out int row);
            return new Vector2Int(column, row);
        }

        private static IEnumerable<Vector2Int> SoTileArraysIn(CombatStageSoTileFit fit, RectInt screenBox)
        {
            for (int y = screenBox.yMin; y < screenBox.yMax; y++)
                for (int x = screenBox.xMin; x < screenBox.xMax; x++)
                    yield return SoTileArrayOf(fit, new Vector2Int(x, y));
        }

        private void PaintSoTile(CombatStageSoTileFit fit, CombatStageSoTileLayout tiles, Vector2Int screen)
        {
            fit.ScreenToArray(screen.x, screen.y, out int column, out int row);
            int brush = soTileBrush;
            if (tiles.KindAt(column, row) == brush) return;
            SoChange("바닥 타일 칠하기", () => tiles.SetKind(column, row, brush));
        }

        private void PaintSoTileBox(CombatStageSoTileFit fit, CombatStageSoTileLayout tiles, RectInt box)
        {
            int brush = soTileBrush;
            var changed = new List<(int Column, int Row)>();
            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    fit.ScreenToArray(x, y, out int column, out int row);
                    if (tiles.KindAt(column, row) != brush) changed.Add((column, row));
                }
            }
            if (changed.Count == 0)
            {
                SetSoNotice("바닥 타일 사각형: 바뀌는 타일이 없습니다.");
                return;
            }
            SoChange("바닥 타일 사각형 칠하기", () =>
            {
                foreach (var (column, row) in changed) tiles.SetKind(column, row, brush);
            });
            SetSoNotice($"바닥 타일 사각형: {changed.Count}칸 칠함");
        }

        private void FillSoTiles(CombatStageSoTileLayout tiles)
        {
            int brush = soTileBrush;
            int changed = tiles.kinds.Count(kind => kind != brush);
            if (changed == 0)
            {
                SetSoNotice("모든 타일이 이미 이 종류입니다.");
                return;
            }
            SoChange("바닥 타일 모두 칠하기", () =>
            {
                for (int i = 0; i < tiles.kinds.Length; i++) tiles.kinds[i] = brush;
            });
            SetSoNotice($"바닥 타일 {changed}칸을 칠했습니다.");
        }

        private void MoveSoTileAnchor(CombatStageSoTileLayout tiles, Vector2Int screen)
        {
            if (tiles.anchorTile == screen) return;
            SoChange("기준 타일 옮기기", () => tiles.anchorTile = screen);
        }

        // 화면 칸 사각형을 타일 보기 격자의 사각형으로 바꿉니다. 격자는 위가 큰 y(필드 +Z)입니다.
        private static Rect SoTileRect(Rect map, int screenRows, RectInt tiles) => SoTileRect(map, screenRows, SoArea(tiles));

        private static Rect SoTileRect(Rect map, int screenRows, Rect tiles) =>
            new Rect(map.x + tiles.xMin * SoTileSize, map.y + (screenRows - tiles.yMax) * SoTileSize, tiles.width * SoTileSize - 2, tiles.height * SoTileSize - 2);

        private static Vector2Int? SoTileAt(Rect map, CombatStageSoTileFit fit, Vector2 mouse, bool clamp)
        {
            int x = Mathf.FloorToInt((mouse.x - map.x) / SoTileSize);
            int y = fit.ScreenRows - 1 - Mathf.FloorToInt((mouse.y - map.y) / SoTileSize);
            if (clamp) return new Vector2Int(Mathf.Clamp(x, 0, fit.ScreenColumns - 1), Mathf.Clamp(y, 0, fit.ScreenRows - 1));
            return x >= 0 && y >= 0 && x < fit.ScreenColumns && y < fit.ScreenRows ? new Vector2Int(x, y) : (Vector2Int?)null;
        }

        private static RectInt SoTileBox(Vector2Int a, Vector2Int b) =>
            new RectInt(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);

        #endregion

        #region 2D 띠 · 고른 칸 · 맞춤 알림

        // 2D 띠와 고른 칸 줄에 쓸 타일 대응을 만듭니다(설계 §12 UT10). 타일 방식이 아니거나 배치가 비면 null입니다.
        private SoTileBand BuildSoTileBand()
        {
            var view = soDocument.view;
            if (!soDocument.HasView || !CombatStageSoBackground.UsesTiles(SoBackgroundMode(view)) || view.tiles.IsEmpty) return null;
            CombatStageSoTileFit fit = SoTileFit(view, out string error);
            var band = new SoTileBand { Tiles = view.tiles, Fit = fit, Error = error, Kinds = new Dictionary<int, CombatStageSoTileKind>() };
            foreach (var kind in SoTileKinds(view.tiles.tileSetGuid))
                if (!band.Kinds.ContainsKey(kind.Value)) band.Kinds.Add(kind.Value, kind);
            // 무작위 시작 지형은 칸이 없어 뺍니다. 공개 대상은 보지 않습니다(게임은 Player에게 보이는 지형만 표시, 계획 §8.2 실행 기록).
            band.Replaced = new HashSet<Vector2Int>();
            if (soVisuals != null)
                foreach (var terrain in soDocument.terrains.Where(marker => !marker.randomizeInitialPosition))
                    if (soVisuals.Of(CombatStageSoLayer.Terrain, terrain.definitionGuid)?.ReplacesTile == true)
                        band.Replaced.Add(new Vector2Int(terrain.x, terrain.y));
            return band;
        }

        // 칸 아래쪽 띠에 그 칸의 타일 종류 색을 그립니다. 타일 맵 밖 칸은 어두운 띠, 바닥 교체 칸은 줄무늬 띠입니다.
        private void DrawSoTileBandCell(Rect rect, Vector2Int cell)
        {
            CombatStageSoTileFit fit = soTileBand?.Fit;
            if (fit == null || !fit.IsValid) return;
            var band = new Rect(rect.x, rect.yMax - SoTileBandHeight, rect.width, SoTileBandHeight);
            bool tiled = fit.TryTileOfCell(cell, out int column, out int row);
            EditorGUI.DrawRect(band, tiled ? SoTileColor(soTileBand.Tiles.KindAt(column, row)) : SoTileOutside);
            if (!soTileBand.Replaced.Contains(cell)) return;
            for (float x = band.x; x < band.xMax; x += 6)
                EditorGUI.DrawRect(new Rect(x, band.y, Mathf.Min(3, band.xMax - x), band.height), Color.black);
        }

        private void DrawSoTileBandLegend()
        {
            CombatStageSoTileFit fit = soTileBand?.Fit;
            if (fit == null || !fit.IsValid) return;
            var used = new HashSet<int>();
            bool outside = false;
            for (int y = 0; y < soField.Rows; y++)
            {
                for (int x = 0; x < soField.Columns; x++)
                {
                    if (fit.TryTileOfCell(new Vector2Int(x, y), out int column, out int row)) used.Add(soTileBand.Tiles.KindAt(column, row));
                    else outside = true;
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("칸 아래 띠 = 바닥 타일:", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                foreach (int kind in used.OrderBy(value => value))
                    DrawSoSwatch(SoTileColor(kind), soTileBand.Kinds.TryGetValue(kind, out var known) ? known.Name : $"?{kind}");
                if (outside) DrawSoSwatch(SoTileOutside, "타일 없음(배경 위)");
                if (soTileBand.Replaced.Count > 0) DrawSoSwatch(Color.black, "줄무늬 = 바닥 교체(타일을 숨김)");
                GUILayout.FlexibleSpace();
            }
        }

        // 고른 칸의 바닥 타일 줄입니다(설계 UT10). 맞춤 문제가 있으면 대응을 내지 않습니다(P-UT13).
        private void DrawSoCellTile(Vector2Int cell)
        {
            if (soTileBand == null) return;
            CombatStageSoTileFit fit = soTileBand.Fit;
            string replaced = soTileBand.Replaced.Contains(cell) ? " · 바닥 교체로 숨김" : "";
            if (fit == null || !fit.IsValid)
                EditorGUILayout.LabelField("바닥 타일", "맞춤 문제로 계산하지 않음(맵 아래 참고)");
            else if (!fit.TryScreenTileOfCell(cell, out Vector2Int screen))
                EditorGUILayout.LabelField("바닥 타일", "없음(배경 위)" + replaced);
            else
            {
                fit.ScreenToArray(screen.x, screen.y, out int column, out int row);
                int kind = soTileBand.Tiles.KindAt(column, row);
                string text = soTileBand.Kinds.TryGetValue(kind, out var known) ? $"{known.Name} · 높이 {known.SurfaceHeight:0.###}" : $"세트에 없는 종류(값 {kind})";
                GameObject prefab = known?.PrefabFor(_soTilePreviewSeed, column, row);
                if (prefab != null) text += $" · 예시 {prefab.name}";
                // 회전은 화면에서 본 각입니다(설계 TT7·TT9).
                int option = soTileBand.Tiles.RotationAt(column, row);
                int resolved = soTileBand.Tiles.ResolvedRotationAt(_soTilePreviewSeed, column, row);
                string angle = option == CombatStageSoTileLayout.RandomRotation
                    ? $" · Random (예시 {fit.ScreenAngleOf(resolved)}°)" : $" · 회전 {fit.ScreenAngleOf(resolved)}°";
                EditorGUILayout.LabelField("바닥 타일", $"{text}{angle} · 타일 ({screen.x}, {screen.y}){replaced}");
            }
        }

        // 맞춤 문제(전투 진입 때 멈춤)와 경고를 보입니다. 저장은 막지 않습니다(설계 Q-UT2 A).
        private void DrawSoTileFitMessages(CombatStageSoTileFit fit)
        {
            if (soTileBand?.Error != null) EditorGUILayout.HelpBox(soTileBand.Error, MessageType.Error);
            if (fit == null) return;
            if (fit.Problems.Count > 0)
                EditorGUILayout.HelpBox("바닥 타일 — 이대로면 전투에 들어갈 때 멈춥니다(저장은 막지 않음):\n· " + string.Join("\n· ", fit.Problems), MessageType.Warning);
            if (fit.Warnings.Count > 0)
                EditorGUILayout.HelpBox("바닥 타일 — 경고:\n· " + string.Join("\n· ", fit.Warnings), MessageType.Info);
        }

        // 저장 뒤 알림에 붙이는 한 줄입니다. 문제·경고가 없으면 null입니다.
        private string SoTileFitSummary()
        {
            var view = soDocument.view;
            if (!soDocument.HasView || !CombatStageSoBackground.UsesTiles(SoBackgroundMode(view)) || view.tiles.IsEmpty) return null;
            CombatStageSoTileFit fit = SoTileFit(view, out _);
            if (fit == null || fit.Problems.Count + fit.Warnings.Count == 0) return null;
            return $"\n바닥 타일: 전투에 들어갈 때 멈춤 {fit.Problems.Count}건 · 경고 {fit.Warnings.Count}건(맵 아래 또는 View '바닥 타일' 참고).";
        }

        #endregion

        // 3D와 칸 높이가 쓰는 바닥 타일입니다. 맵을 그릴 때 만든 대응(soTileBand)에서 맞춤이 성립할 때만 만듭니다(설계 P-UT13).
        private CombatStageSoTileScene SoTileScene()
        {
            CombatStageSoTileFit fit = soTileBand?.Fit;
            return fit != null && fit.IsValid ? new CombatStageSoTileScene(fit, soTileBand.Tiles, soTileBand.Kinds, _soTilePreviewSeed) : null;
        }

        // 지금 작업본으로 맞춤을 계산합니다(설계 P-UT12). 필드·외형 표가 없으면 null이고 이유를 error로 냅니다.
        private CombatStageSoTileFit SoTileFit(CombatStageSoView view, out string error)
        {
            error = null;
            if (soField == null)
            {
                error = "필드가 없어 바닥 타일을 칸에 맞출 수 없습니다.";
                return null;
            }
            CombatStageSoVisuals visuals = SoVisualsOrNull(out error);
            if (visuals == null) return null;
            CombatStageSoFieldView fieldView = visuals.FieldView;
            try
            {
                return CombatStageSoTileFit.Of(view.tiles, !fieldView.Approximate, view.rotationOffset, SoBackdropScale(view),
                    soField.Columns, soField.Rows, fieldView.CellWidth, fieldView.CellLength);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return null;
            }
        }

        // 외형 표(필드 보기 포함)입니다. 3D와 같은 캐시를 쓰고 '목록 새로고침'이 비웁니다(설계 P-UT17).
        private CombatStageSoVisuals SoVisualsOrNull(out string error)
        {
            error = null;
            if (soVisuals != null) return soVisuals;
            try { soVisuals = CombatStageSoVisuals.Build(); }
            catch (Exception exception) { error = "외형 표를 읽지 못했습니다: " + exception.Message; }
            return soVisuals;
        }

        // 타일 방식은 캐시된 운영 필드 보기 값을 공통 배율 계산에 전달합니다.
        private Vector3 SoBackdropScale(CombatStageSoView view)
        {
            var prefab = CombatStageSoAssets.Load(view.gameObjectGuid) as GameObject;
            if (CombatStageSoBackground.UsesTiles(SoBackgroundMode(view)))
            {
                CombatStageSoVisuals visuals = SoVisualsOrNull(out string problem);
                if (visuals == null) throw new InvalidOperationException(problem);
                return CombatMapScaleEditor.Resolve(prefab, visuals.FieldView, view.tiles);
            }
            return prefab != null ? Vector3.Scale(prefab.transform.localScale, view.scaleOffset) : view.scaleOffset;
        }

        // 종류 값으로 정한 색입니다(설계 P-UT5). 같은 값은 어느 보기에서나 같은 색입니다.
        private static Color SoTileColor(int kind) =>
            kind <= 0 ? Color.black : Color.HSVToRGB(Mathf.Repeat(kind * .23f + .08f, 1f), .45f, .62f);

        private static Dictionary<int, string> SoTileKindNames(List<CombatStageSoTileKind> kinds)
        {
            var names = new Dictionary<int, string>();
            foreach (var kind in kinds)
                if (!names.ContainsKey(kind.Value)) names.Add(kind.Value, kind.Name);
            return names;
        }

        private static void DrawSoSwatch(Color color, string label)
        {
            Rect swatch = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12), GUILayout.Height(12));
            EditorGUI.DrawRect(swatch, color);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
        }

        private static void DrawSoRectFrame(Rect rect, Color color)
        {
            const float line = 2;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, line), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - line, rect.width, line), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, line, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - line, rect.y, line, rect.height), color);
        }

        // 세트의 종류 줄과 종류별 칸 수, 저장 검사가 막을 문제를 보입니다.
        private void DrawSoTileKinds(CombatStageSoTileLayout tiles, List<CombatStageSoTileKind> kinds)
        {
            if (string.IsNullOrEmpty(tiles.tileSetGuid))
                EditorGUILayout.HelpBox("타일 세트가 없습니다. 저장 검사가 막습니다.", MessageType.Warning);
            if (!tiles.HasFullKinds)
                EditorGUILayout.HelpBox($"칸 종류 {tiles.kinds?.Length ?? 0}개가 가로 × 세로({tiles.columns} × {tiles.rows})와 다릅니다. 저장 검사가 막습니다. " +
                                        "가로나 세로를 다시 넣으면 맞춰집니다.", MessageType.Warning);
            var counts = new Dictionary<int, int>();
            foreach (int kind in tiles.kinds ?? Array.Empty<int>())
                counts[kind] = counts.TryGetValue(kind, out int count) ? count + 1 : 1;
            foreach (var kind in kinds)
            {
                string candidates = string.Join(", ", Enumerable.Range(0, kind.PrefabCount).Select(index => kind.PrefabAt(index) != null ? kind.PrefabAt(index).name : "프리팹 없음"));
                EditorGUILayout.LabelField($"{kind.Name} · 후보 {kind.PrefabCount}개 ({candidates}) · 높이 {kind.SurfaceHeight:0.###} · {(counts.TryGetValue(kind.Value, out int used) ? used : 0)}칸",
                    EditorStyles.wordWrappedMiniLabel);
            }
            DrawSoTileRandomPreviewButton();
            var missing = counts.Where(pair => kinds.All(kind => kind.Value != pair.Key)).ToList();
            if (missing.Count > 0 && !string.IsNullOrEmpty(tiles.tileSetGuid))
                EditorGUILayout.HelpBox($"세트에 없는 종류 칸 {missing.Sum(pair => pair.Value)}개(값 {string.Join(", ", missing.Select(pair => pair.Key))}). 저장 검사가 막습니다.",
                    MessageType.Warning);
        }

        // 가로·세로를 바꿉니다. 새 칸은 세트 첫 종류입니다(설계 P-UT2). 칸 수가 1 미만이거나 상한을 넘으면 바꾸지 않습니다.
        private void ResizeSoTiles(CombatStageSoTileLayout tiles, int columns, int rows, List<CombatStageSoTileKind> kinds)
        {
            if (columns < 1 || rows < 1 || columns > SoMaxTileCount || rows > SoMaxTileCount)
            {
                SetSoNotice($"바닥 타일 가로·세로는 1~{SoMaxTileCount}칸입니다.", MessageType.Warning);
                return;
            }
            int fill = kinds.Count > 0 ? kinds[0].Value : 0;
            SoChange("바닥 타일 크기 변경", () => tiles.Resize(columns, rows, fill));
        }

        // '타일 만들기'입니다(설계 P-UT3). 세트는 이름순 첫 세트이고, 가로·세로·크기는 지금 필드·칸 간격·배경 배율로 정합니다.
        private void CreateSoTiles(CombatStageSoView view)
        {
            if (soField == null)
            {
                SetSoNotice("필드가 없어 바닥 타일을 만들 수 없습니다. '참조'에서 필드를 고르세요.", MessageType.Warning);
                return;
            }
            string set = soCatalog.TileSets.Count > 0 ? soCatalog.TileSets.Guids[0] : "";
            List<CombatStageSoTileKind> kinds = SoTileKinds(set);
            CombatStageSoFieldView fieldView = SoVisualsOrNull(out _)?.FieldView ?? CombatStageSoAssets.ReadFieldView();
            var layout = CombatStageSoTileLayout.CreateDefault(set, soField.Columns, soField.Rows, fieldView.CellWidth, fieldView.CellLength,
                SoBackdropScale(view), view.rotationOffset, kinds.Count > 0 ? kinds[0].Value : 0);
            SoChange("바닥 타일 만들기", () => view.tiles = layout);
            SetSoNotice(string.IsNullOrEmpty(set)
                ? "타일 세트가 없어 세트 없이 만들었습니다. 타일 세트를 만든 뒤 고르세요."
                : $"바닥 타일을 만들었습니다({CombatStageSoAssets.NameOf(set)}, {layout.columns} × {layout.rows}). 위치·기준 타일은 배경 구멍에 맞춰 고치세요.");
        }

        private List<CombatStageSoTileKind> SoTileKinds(string guid)
        {
            guid = guid ?? "";
            if (soTileKinds.TryGetValue(guid, out var kinds)) return kinds;
            try { kinds = CombatStageSoAssets.ReadTileSet(guid); }
            catch (Exception exception)
            {
                kinds = new List<CombatStageSoTileKind>();
                SetSoNotice("타일 세트를 읽지 못했습니다: " + exception.Message, MessageType.Error);
            }
            soTileKinds[guid] = kinds;
            return kinds;
        }

        private static void SoNote(string text) => EditorGUILayout.LabelField(text, EditorStyles.wordWrappedMiniLabel);

        private static bool SoVector2(string label, Vector2 value, out Vector2 next)
        {
            EditorGUI.BeginChangeCheck();
            next = EditorGUILayout.Vector2Field(label, value);
            return EditorGUI.EndChangeCheck() && next != value;
        }

        private static bool SoVector2Int(string label, Vector2Int value, out Vector2Int next)
        {
            EditorGUI.BeginChangeCheck();
            next = EditorGUILayout.Vector2IntField(label, value);
            return EditorGUI.EndChangeCheck() && next != value;
        }
    }
}
