using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class CombatLayoutWindow : EditorWindow
    {
        private static readonly string[] Tools = { "선택 / 이동", "유닛 배치", "높이", "바닥", "벽", "레일", "지우기", "타일 선택" };
        private static readonly string[] CellTypes = { "Floor", "Blocked", "Rail" };
        private static readonly string[] Directions = { "North", "East", "South", "West" };
        private static readonly string[] Roles = { "미지정", "일반", "기병", "수비병", "포병", "메인" };
        [SerializeField] private CombatLayoutDocument document;
        [SerializeField] private CombatLayoutStage settings;
        [SerializeField] private Vector2Int selectedCell = new Vector2Int(-1, -1);
        [SerializeField] private int selectedUnitKey, tool, characterKey, brushHeight = 1, rangeMode, direction;
        [SerializeField] private bool twoDimensional, labels = true, modified;
        [SerializeField] private string lastPath = "", search = "";
        [SerializeField] private string characterSourcePath = "", methodSourcePath = "";
        [SerializeField] private string obstacleSourcePath = "", obstacleSearch = "";
        [SerializeField] private int obstacleKey;
        [SerializeField, HideInInspector] private ScriptableObject inspectorSelection; // Clean up the earlier Inspector selection on reload.
        private bool characterCatalogLoaded;
        private bool obstacleCatalogLoaded;
        private Vector2 sidebarScroll, gridScroll;
        private CombatLayoutPreview preview;
        private string notice = "";
        private MessageType noticeType = MessageType.Info;
        private int strokeGroup = -1;
        private Vector2Int lastPainted = new Vector2Int(-1, -1);
        private readonly HashSet<Vector2Int> moveRange = new HashSet<Vector2Int>();
        private readonly HashSet<Vector2Int> attackRange = new HashSet<Vector2Int>();
        private string DraftKey => "ProjectL.CombatLayout." + Hash128.Compute(Application.dataPath);

        [MenuItem("ProjecL/전투 스테이지 편집", false, 10)]
        [MenuItem("Assets/ProjecL/전투 스테이지 편집", false, 2000)]
        public static void Open()
        {
            var window = GetWindow<CombatLayoutWindow>();
            window.titleContent = new GUIContent("ProjecL · 스테이지");
            window.minSize = new Vector2(1100, 650);
            window.Show();
            window.ShowDetails(window.detailsMode);
        }

        [MenuItem("ProjecL/데이터 폴더", false, 30)]
        private static void ShowDataFolder()
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/DataTable");
            if (folder != null) { Selection.activeObject = folder; EditorGUIUtility.PingObject(folder); }
        }

        private void OnEnable()
        {
            if (inspectorSelection != null)
            {
                if (Selection.activeObject == inspectorSelection) Selection.activeObject = null;
                DestroyImmediate(inspectorSelection); inspectorSelection = null;
            }
            characterCatalogLoaded = false;
            titleContent = new GUIContent("ProjecL · 스테이지");
            minSize = new Vector2(1100, 650);
            if (document == null)
            {
                try
                {
                    string draft = EditorPrefs.GetString(DraftKey, "");
                    document = string.IsNullOrEmpty(draft) ? CombatLayoutDocument.Create() : JsonUtility.FromJson<CombatLayoutDocument>(draft);
                    document.Validate();
                    if (!string.IsNullOrEmpty(draft)) { modified = true; notice = "이 프로젝트의 마지막 편집 초안을 복원했습니다."; }
                }
                catch (Exception exception) { document = CombatLayoutDocument.Create(); SetNotice("초안을 복원하지 못했습니다: " + exception.Message, MessageType.Warning); }
            }
            settings = document.stage.Copy();
            if (document.obstacles == null) document.obstacles = new CombatReferenceSheet();
            LoadDefaultReferences(false);
            preview = new CombatLayoutPreview();
            preview.Reset(document.stage);
            Undo.undoRedoPerformed += OnUndoRedo;
            RefreshDetails();
        }

        private void OnDisable()
        {
            EndStroke();
            Undo.undoRedoPerformed -= OnUndoRedo;
            SaveDraft();
            preview?.Dispose(); preview = null;
        }

        private void OnUndoRedo()
        {
            settings = document.stage.Copy();
            modified = true; SaveDraft(); Repaint();
            RefreshDetails();
        }

        private void SaveDraft() => EditorPrefs.SetString(DraftKey, JsonUtility.ToJson(document));
        private void SetNotice(string message, MessageType type = MessageType.Info) { notice = message; noticeType = type; Repaint(); RefreshDetails(); }
        private void Change(string label, Action action)
        {
            Undo.RecordObject(this, label);
            action(); modified = true; EditorUtility.SetDirty(this); SaveDraft(); Repaint();
            RefreshDetails();
        }

        private void OnGUI()
        {
            if (document == null) return;
            DrawHeader();
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(230)))
                {
                    sidebarScroll = EditorGUILayout.BeginScrollView(sidebarScroll);
                    DrawSidebarSections();
                    EditorGUILayout.EndScrollView();
                }
                using (new EditorGUILayout.VerticalScope()) DrawMap();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(310))) DrawDetailsPanel();
            }
            if (Event.current.rawType == EventType.MouseUp || Event.current.type == EventType.Ignore) EndStroke();
            if (Event.current.type == EventType.KeyDown && GUIUtility.keyboardControl == 0)
            {
                if (Event.current.keyCode == KeyCode.Escape) { selectedCell = new Vector2Int(-1, -1); selectedUnitKey = 0; ShowDetails(CombatLayoutDetailsMode.Map); Event.current.Use(); }
                else if (Event.current.keyCode == KeyCode.Delete && selectedUnitKey != 0) { DeleteSelected(); Event.current.Use(); }
            }
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ProjecL / 전투 스테이지", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("맵 정보", EditorStyles.toolbarButton))
                { tool = 7; selectedUnitKey = 0; ShowDetails(CombatLayoutDetailsMode.Map); }
                if (GUILayout.Button("새 맵", EditorStyles.toolbarButton)) NewDocument();
                if (GUILayout.Button("엑셀 열기", EditorStyles.toolbarButton)) OpenWorkbook();
                if (GUILayout.Button("엑셀 내보내기", EditorStyles.toolbarButton)) ExportWorkbook();
            }
            EditorGUILayout.LabelField(document.stage.name + (modified ? " *" : ""), string.IsNullOrEmpty(lastPath) ? "새 배치" : Path.GetFileName(lastPath));
            if (!string.IsNullOrEmpty(notice)) EditorGUILayout.HelpBox(notice, noticeType);
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("맵 설정", EditorStyles.boldLabel);
            settings.key = EditorGUILayout.IntField("Stage 키", settings.key);
            settings.name = EditorGUILayout.TextField("이름", settings.name);
            settings.seed = EditorGUILayout.IntField("Seed", settings.seed);
            settings.width = EditorGUILayout.IntField("가로 타일", settings.width);
            settings.height = EditorGUILayout.IntField("세로 타일", settings.height);
            settings.playerDepth = EditorGUILayout.IntField("아군 배치 깊이", settings.playerDepth);
            settings.enemyDepth = EditorGUILayout.IntField("적군 배치 깊이", settings.enemyDepth);
            settings.reserve = EditorGUILayout.IntField("대기 슬롯", settings.reserve);
            settings.night = EditorGUILayout.LongField("밤 길이", settings.night);
            if (GUILayout.Button("맵 설정 적용"))
            {
                try
                {
                    var candidate = JsonUtility.FromJson<CombatLayoutDocument>(JsonUtility.ToJson(document));
                    candidate.Resize(settings); candidate.Validate();
                    Change("맵 설정 변경", () => document = candidate);
                    if (!document.Contains(selectedCell.x, selectedCell.y)) selectedCell = new Vector2Int(-1, -1);
                    preview.Reset(document.stage); SetNotice("맵 설정을 적용했습니다.");
                }
                catch (Exception exception) { SetNotice(exception.Message, MessageType.Error); }
            }
            GUILayout.Space(12);
        }

        private void DrawCharacters()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("유닛 엑셀 읽기")) LoadReferenceDialog(true);
                if (GUILayout.Button("Method 엑셀")) LoadReferenceDialog(false);
            }
            if (GUILayout.Button("프로젝트 DT 다시 읽기")) LoadDefaultReferences(true);
            int count = characterCatalogLoaded ? document.characters.rows.Count : 0;
            EditorGUILayout.LabelField($"등록 유닛 {count}개", EditorStyles.miniLabel);
            search = EditorGUILayout.TextField("검색", search);
            EditorGUILayout.HelpBox("목록에서 유닛 선택 → 맵 클릭으로 배치. 진형은 DT 값을 사용합니다.", MessageType.Info);
            if (!characterCatalogLoaded)
            {
                EditorGUILayout.HelpBox("CharacterMaster 원본 엑셀을 읽어 주세요.", MessageType.Warning);
                return;
            }
            if (count == 0)
            {
                EditorGUILayout.HelpBox("현재 CharacterMaster에 데이터 행이 없습니다. 엑셀에 등록된 유닛이 목록에 표시됩니다.", MessageType.Info);
                return;
            }
            foreach (var row in document.characters.rows)
            {
                int key = document.characters.Number(row, "Character_Key");
                string name = CharacterName(document.characters, key);
                string faction = CombatCharacterCatalog.OwnerLabel(document.characters, key);
                int role = document.characters.Number(row, "Character_RoleType");
                string roleName = role >= 0 && role < Roles.Length ? Roles[role] : "직업 " + role;
                if (!string.IsNullOrEmpty(search) && (name + key + faction + roleName).IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Color previous = GUI.backgroundColor;
                if (characterKey == key) GUI.backgroundColor = new Color(.5f, .85f, .8f);
                bool clicked = GUILayout.Button($"{name}  [{key}]\n{faction} · {roleName}", GUILayout.Height(42));
                GUI.backgroundColor = previous;
                if (clicked)
                {
                    characterKey = key; selectedUnitKey = 0; tool = 1;
                    ShowDetails(CombatLayoutDetailsMode.Character);
                }
            }
            GUILayout.Space(12);
        }

        internal static string CharacterName(CombatReferenceSheet sheet, int key)
        {
            var row = sheet.Find("Character_Key", key);
            if (row == null) return "Character " + key;
            string name = sheet.Value(row, "Character_NameKOR");
            if (string.IsNullOrEmpty(name)) name = sheet.Value(row, "Character_Name");
            return string.IsNullOrEmpty(name) ? "Character " + key : name;
        }

        private void DrawCharacterInfo(CombatReferenceRow row)
        {
            var sheet = document.characters;
            int role = sheet.Number(row, "Character_RoleType");
            string roleName = sheet.Value(row, "EditorCharacter_RoleName");
            EditorGUILayout.LabelField("직업", !string.IsNullOrEmpty(roleName) ? roleName : role >= 0 && role < Roles.Length ? Roles[role] : "직업 " + role);
            foreach (var pair in new[] { ("생명력", "HealthValue"), ("방어력", "DefenseValue"), ("공격력", "AttackValue"), ("이동 거리", "MoveRange"), ("공격 거리", "AttackRange") })
            {
                string value = sheet.Value(row, "Character_" + pair.Item2);
                EditorGUILayout.LabelField(pair.Item1, string.IsNullOrEmpty(value) ? "—" : value);
            }
        }

        private void DrawTileSelection()
        {
            EditorGUILayout.LabelField("선택한 타일", EditorStyles.boldLabel);
            var cell = document.Cell(selectedCell.x, selectedCell.y);
            if (cell == null) { EditorGUILayout.LabelField("맵에서 타일을 선택하세요."); return; }
            EditorGUILayout.LabelField("좌표", $"({cell.x}, {cell.y})");
            EditorGUI.BeginChangeCheck();
            int elevation = EditorGUILayout.IntSlider("높이", cell.elevation, 0, 20);
            string type = CellTypes[EditorGUILayout.Popup("타일 종류", Array.IndexOf(CellTypes, cell.type), CellTypes)];
            int terrain = EditorGUILayout.IntField("Terrain 키", cell.terrainKey);
            int obstacle = DrawObstaclePicker(cell.obstacleKey);
            if (EditorGUI.EndChangeCheck())
            {
                if (obstacle > 0 && obstacle != cell.obstacleKey) type = "Blocked";
                if (terrain < 0 || (type == "Blocked" && document.Unit(cell.x, cell.y) != null)) SetNotice("Terrain 키는 0 이상이어야 하며 유닛이 있는 타일은 벽으로 바꿀 수 없습니다.", MessageType.Warning);
                else Change("타일 속성 변경", () => { cell.elevation = elevation; cell.type = type; cell.terrainKey = terrain; cell.obstacleKey = type == "Blocked" ? obstacle : 0; });
            }
            DrawObstacleReference(cell.obstacleKey);
        }

        private void DrawPlacedUnitSelection()
        {
            var unit = document.units.Find(u => u.key == selectedUnitKey);
            if (unit == null) return;
            EditorGUILayout.LabelField("배치 정보", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Unit 키", unit.key.ToString());
            EditorGUILayout.LabelField("Character 키", unit.characterKey.ToString());
            EditorGUILayout.LabelField("DT 진형", CombatCharacterCatalog.OwnerLabel(document.characters, unit.characterKey));
            EditorGUILayout.LabelField("배치 타일 높이", document.Cell(unit.x, unit.y).elevation.ToString());
            EditorGUI.BeginChangeCheck();
            int x = EditorGUILayout.IntField("X", unit.x), y = EditorGUILayout.IntField("Y", unit.y);
            if (EditorGUI.EndChangeCheck())
            {
                if (!CanPlace(x, y, unit.key)) SetNotice("겹치지 않는 바닥 좌표가 필요합니다.", MessageType.Warning);
                else Change("유닛 위치 변경", () => { unit.x = x; unit.y = y; selectedCell = new Vector2Int(x, y); });
            }
            if (GUILayout.Button("선택 유닛 삭제")) DeleteSelected();
        }

        private void DrawUnitList()
        {
            if (document.units.Count == 0)
                EditorGUILayout.LabelField("배치된 유닛이 없습니다.", EditorStyles.miniLabel);
            foreach (var unit in document.units)
                if (GUILayout.Button($"#{unit.key} {CharacterName(document.characters, unit.characterKey)} ({unit.x}, {unit.y})"))
                { selectedUnitKey = unit.key; selectedCell = new Vector2Int(unit.x, unit.y); tool = 0; ShowDetails(CombatLayoutDetailsMode.Unit); }
        }

        private void DrawMap()
        {
            EditorGUI.BeginChangeCheck();
            tool = GUILayout.Toolbar(tool, Tools);
            if (EditorGUI.EndChangeCheck() && tool == 7)
            { selectedUnitKey = 0; ShowDetails(CombatLayoutDetailsMode.Map); }
            using (new EditorGUILayout.HorizontalScope())
            {
                brushHeight = EditorGUILayout.IntSlider("칠할 높이", brushHeight, 0, 20, GUILayout.MaxWidth(280));
                if (GUILayout.Button("되돌리기", GUILayout.Width(75))) Undo.PerformUndo();
                if (GUILayout.Button("다시 실행", GUILayout.Width(75))) Undo.PerformRedo();
                if (GUILayout.Button(twoDimensional ? "3D 보기" : "2D 보기", GUILayout.Width(75))) twoDimensional = !twoDimensional;
                if (GUILayout.Button("시점 초기화", GUILayout.Width(85))) preview.Reset(document.stage);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                labels = GUILayout.Toggle(labels, "유닛 이름", GUILayout.Width(90));
                rangeMode = EditorGUILayout.Popup(rangeMode, new[] { "이동 + 공격", "이동", "공격", "숨기기" }, GUILayout.Width(110));
                direction = EditorGUILayout.Popup(direction, Directions, GUILayout.Width(85));
                GUILayout.FlexibleSpace(); GUILayout.Label($"{document.stage.width} × {document.stage.height}");
            }
            CalculateRanges();
            if (twoDimensional) DrawGrid();
            else
            {
                Rect rect = GUILayoutUtility.GetRect(100, 100, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                preview.Draw(rect, document, selectedCell, selectedUnitKey, labels, moveRange, attackRange);
                Event e = Event.current;
                if (preview.Navigate(e, rect)) Repaint();
                if (rect.Contains(e.mousePosition) && e.button == 0 && !e.alt &&
                    (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
                {
                    Vector2Int cell = preview.Pick(document, rect, e.mousePosition);
                    if (document.Contains(cell.x, cell.y)) HandleCell(cell, e.type == EventType.MouseDrag);
                    e.Use();
                }
            }
            EditorGUILayout.LabelField("좌클릭 편집 · 우클릭 회전 · 휠 줌 · Shift+우클릭 이동 · Delete 삭제", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox("범위는 DT 형태 미리보기입니다. 장애물 경로·점유·단차·행동비용·시야에 따른 실제 전투 판정은 포함하지 않습니다.", MessageType.Info);
        }

        private void DrawGrid()
        {
            const float size = 43;
            gridScroll = EditorGUILayout.BeginScrollView(gridScroll, GUILayout.ExpandHeight(true));
            Rect map = GUILayoutUtility.GetRect(document.stage.width * size, document.stage.height * size, GUILayout.ExpandWidth(false));
            foreach (var cell in document.cells)
            {
                Rect rect = new Rect(map.x + cell.x * size, map.y + (document.stage.height - 1 - cell.y) * size, size - 2, size - 2);
                var position = new Vector2Int(cell.x, cell.y);
                Color color = cell.type == "Blocked" ? Color.gray : cell.type == "Rail" ? new Color(.6f, .5f, .25f) : new Color(.22f, .29f, .34f);
                if (moveRange.Contains(position)) color = new Color(.2f, .6f, .35f);
                if (attackRange.Contains(position)) color = new Color(.8f, .5f, .2f);
                if (moveRange.Contains(position) && attackRange.Contains(position)) color = new Color(.7f, .65f, .2f);
                if (position == selectedCell) color = Color.Lerp(color, Color.white, .4f);
                EditorGUI.DrawRect(rect, color);
                var unit = document.Unit(cell.x, cell.y);
                GUI.Label(rect, unit == null ? $"{cell.x},{cell.y}\nH{cell.elevation}" + (cell.obstacleKey > 0 ? $"\nO:{cell.obstacleKey}" : "") : $"#{unit.key}\n{unit.characterKey}", EditorStyles.whiteMiniLabel);
                Event e = Event.current;
                if (rect.Contains(e.mousePosition) && e.button == 0 && !e.alt && (e.type == EventType.MouseDown || e.type == EventType.MouseDrag))
                { HandleCell(position, e.type == EventType.MouseDrag); e.Use(); }
            }
            EditorGUILayout.EndScrollView();
        }

        private bool CanPlace(int x, int y, int except = 0) => document.Contains(x, y) && document.Cell(x, y).type != "Blocked" &&
            !document.units.Any(u => u.key != except && u.x == x && u.y == y);

        private void HandleCell(Vector2Int position, bool dragging)
        {
            if (dragging && (tool < 2 || tool == 7 || position == lastPainted)) return;
            lastPainted = position;
            var cell = document.Cell(position.x, position.y);
            var unit = document.Unit(position.x, position.y);
            if (tool == 7)
            {
                selectedCell = position; selectedUnitKey = 0;
                ShowDetails(CombatLayoutDetailsMode.Map); return;
            }
            if (tool == 0)
            {
                var selected = document.units.Find(u => u.key == selectedUnitKey);
                if (selected != null && unit == null && CanPlace(position.x, position.y, selected.key))
                    Change("유닛 이동", () => { selected.x = position.x; selected.y = position.y; });
                else selectedUnitKey = unit?.key ?? 0;
                selectedCell = position;
                ShowDetails(selectedUnitKey == 0 ? CombatLayoutDetailsMode.Map : CombatLayoutDetailsMode.Unit);
                return;
            }
            if (tool == 1 && (!characterCatalogLoaded || !CombatCharacterCatalog.TryOwner(document.characters, characterKey, out _) || !CanPlace(position.x, position.y) || document.nextUnitKey > int.MaxValue))
            { SetNotice("현재 DT 목록에서 진형이 지정된 유닛을 선택하고, 비어 있는 바닥에 배치해 주세요.", MessageType.Warning); return; }
            if (tool == 4 && unit != null) { SetNotice("유닛이 있는 타일은 벽으로 바꿀 수 없습니다.", MessageType.Warning); return; }
            if (tool == 4 && (!obstacleCatalogLoaded || document.obstacles.Find("Obstacle_Key", obstacleKey) == null || obstacleKey < 1))
            { SetNotice("ObstacleMaster 원본 엑셀을 읽고 현재 DT 장애물 목록에서 벽으로 배치할 항목을 선택해 주세요.", MessageType.Warning); return; }
            detailsMode = tool == 1 ? CombatLayoutDetailsMode.Unit : CombatLayoutDetailsMode.Map;
            if (tool != 1) selectedUnitKey = 0;
            if (strokeGroup < 0) { Undo.IncrementCurrentGroup(); strokeGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("배치 편집"); }
            Change("배치 편집", () =>
            {
                selectedCell = position;
                if (tool == 1)
                {
                    var placed = document.PlaceCharacter(characterKey, position.x, position.y);
                    selectedUnitKey = placed.key;
                }
                else if (tool == 2) cell.elevation = brushHeight;
                else if (tool == 4) document.PaintObstacle(obstacleKey, position.x, position.y);
                else if (tool == 3 || tool == 5) { cell.type = CellTypes[tool - 3]; cell.obstacleKey = 0; }
                else if (tool == 6)
                {
                    if (unit != null) { document.units.Remove(unit); if (unit.key == selectedUnitKey) selectedUnitKey = 0; }
                    else { cell.type = "Floor"; cell.elevation = 0; cell.terrainKey = 0; cell.obstacleKey = 0; }
                }
            });
            if (tool == 1) ShowDetails(CombatLayoutDetailsMode.Unit);
            else { selectedUnitKey = 0; ShowDetails(CombatLayoutDetailsMode.Map); }
        }

        private void EndStroke()
        {
            if (strokeGroup >= 0) Undo.CollapseUndoOperations(strokeGroup);
            strokeGroup = -1; lastPainted = new Vector2Int(-1, -1);
        }

        private void DeleteSelected()
        {
            int key = selectedUnitKey;
            Change("유닛 삭제", () => { document.units.RemoveAll(u => u.key == key); selectedUnitKey = 0; });
            ShowDetails(CombatLayoutDetailsMode.Map);
        }

        private void CalculateRanges()
        {
            moveRange.Clear(); attackRange.Clear();
            var unit = document.units.Find(u => u.key == selectedUnitKey);
            if (unit == null || rangeMode == 3) return;
            var character = document.characters.Find("Character_Key", unit.characterKey);
            if (character == null) return;
            if (rangeMode == 0 || rangeMode == 1) AddRange(unit, character, true, moveRange);
            if (rangeMode == 0 || rangeMode == 2) AddRange(unit, character, false, attackRange);
        }

        private void AddRange(CombatLayoutUnit unit, CombatReferenceRow character, bool move, HashSet<Vector2Int> result)
        {
            string prefix = move ? "Move" : "Attack";
            int range = document.characters.Number(character, "Character_" + prefix + "Range");
            int key = document.characters.Number(character, "Character_" + prefix + "Method");
            var method = document.methods.Find("Method_Key", key);
            if (method == null || range < 1) return;
            int type = document.methods.Number(method, "Method_Type");
            foreach (var cell in document.cells)
            {
                int dx = cell.x - unit.x, dy = cell.y - unit.y, ax = Math.Abs(dx), ay = Math.Abs(dy);
                if (Math.Max(ax, ay) == 0 || Math.Max(ax, ay) > range || (move && cell.type == "Blocked")) continue;
                bool matches = type == 1 ? dx == 0 || dy == 0 : type == 2 ? ax == ay : type == 3 ? dx == 0 || dy == 0 || ax == ay :
                    type == 5 && (long)dx * dx + (long)dy * dy <= (long)range * range;
                if (type == 4)
                {
                    if (direction == 1) { int previous = dx; dx = -dy; dy = previous; }
                    else if (direction == 2) { dx = -dx; dy = -dy; }
                    else if (direction == 3) { int previous = dx; dx = dy; dy = -previous; }
                    matches = dy > 0 && Math.Abs(dx) <= dy;
                }
                if (matches) result.Add(new Vector2Int(cell.x, cell.y));
            }
        }

        private bool AllowReplace() => !modified || EditorUtility.DisplayDialog("편집 중인 배치", "현재 배치를 새 맵/다른 파일로 바꿀까요? 필요한 내용은 먼저 엑셀로 내보내 주세요.", "바꾸기", "취소");
        private void ReplaceDocument(CombatLayoutDocument candidate, string path)
        {
            EndStroke(); Undo.ClearUndo(this);
            document = candidate; settings = document.stage.Copy(); lastPath = path; modified = false;
            selectedCell = new Vector2Int(-1, -1); selectedUnitKey = 0; preview.Reset(document.stage); SaveDraft(); Repaint();
            ShowDetails(CombatLayoutDetailsMode.Map);
        }
        private void NewDocument()
        {
            if (!AllowReplace()) return;
            var candidate = CombatLayoutDocument.Create(); candidate.characters = document.characters; candidate.methods = document.methods;
            candidate.obstacles = document.obstacles;
            ReplaceDocument(candidate, ""); SetNotice("새 전투 맵을 만들었습니다.");
        }
        private void OpenWorkbook()
        {
            string path = EditorUtility.OpenFilePanel("배치 엑셀 열기", DataDirectory, "xlsx");
            if (string.IsNullOrEmpty(path) || !AllowReplace()) return;
            try
            {
                var candidate = CombatLayoutWorkbook.Load(path);
                if (characterCatalogLoaded || candidate.characters.columns.Count == 0) candidate.characters = document.characters;
                if (document.methods.columns.Count > 0 || candidate.methods.columns.Count == 0) candidate.methods = document.methods;
                if (obstacleCatalogLoaded || candidate.obstacles.columns.Count == 0) candidate.obstacles = document.obstacles;
                int ownerChanges = CombatCharacterCatalog.RefreshUnitOwners(candidate);
                ReplaceDocument(candidate, path); modified = ownerChanges > 0;
                SetNotice("배치 엑셀을 불러왔습니다. 유닛 진형은 현재 DT에 맞췄습니다.");
            }
            catch (Exception exception) { SetNotice(exception.Message, MessageType.Error); }
        }
        private string DataDirectory => Path.Combine(Application.dataPath, "DataTable");
        private void ExportWorkbook()
        {
            try
            {
                document.Validate();
                if (document.units.Count > 0 && !characterCatalogLoaded) throw new InvalidOperationException("CharacterMaster 원본 DT를 읽은 뒤 내보내 주세요.");
                CombatCharacterCatalog.ValidateUnits(document);
                if (document.cells.Any(cell => cell.obstacleKey > 0) && !obstacleCatalogLoaded)
                    throw new InvalidOperationException("ObstacleMaster 원본 DT를 읽은 뒤 내보내 주세요.");
                document.ValidateObstacleReferences();
            }
            catch (Exception exception) { SetNotice(exception.Message, MessageType.Error); return; }
            string path = EditorUtility.SaveFilePanel("배치 엑셀 내보내기", string.IsNullOrEmpty(lastPath) ? DataDirectory : Path.GetDirectoryName(lastPath), "CombatLayout_" + document.stage.key, "xlsx");
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                CombatLayoutWorkbook.Save(path, document); lastPath = path; modified = false; SaveDraft();
                string full = Path.GetFullPath(path), assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
                if (full.StartsWith(assets, StringComparison.OrdinalIgnoreCase)) AssetDatabase.ImportAsset("Assets/" + full.Substring(assets.Length).Replace('\\', '/'));
                SetNotice("엑셀을 저장했습니다: " + path);
            }
            catch (Exception exception) { SetNotice("엑셀 저장 실패: " + exception.Message, MessageType.Error); }
        }
        private void LoadReferenceDialog(bool characters)
        {
            string path = EditorUtility.OpenFilePanelWithFilters("참조 DT 열기", DataDirectory, new[] { "Excel", "xlsx,xls" });
            if (string.IsNullOrEmpty(path)) return;
            try { LoadReference(path, characters); SetNotice("참조 DT를 불러왔습니다."); }
            catch (Exception exception) { SetNotice(exception.Message, MessageType.Error); }
        }
        private void LoadReference(string path, bool characters)
        {
            var reference = CombatLayoutWorkbook.LoadReferences(path, characters ? "CharacterMaster" : "MethodMaster", characters ? "Character_Key" : "Method_Key");
            if (characters)
            {
                foreach (var row in reference.rows)
                {
                    var previous = document.characters.Find("Character_Key", reference.Number(row, "Character_Key"));
                    if (previous != null)
                    {
                        string color = document.characters.Value(previous, "EditorCharacter_Color");
                        if (!string.IsNullOrEmpty(color)) reference.Set(row, "EditorCharacter_Color", color);
                    }
                }
            }
            Change("참조 DT 갱신", () =>
            {
                if (characters)
                {
                    document.characters = reference; characterCatalogLoaded = true; characterSourcePath = path;
                    CombatCharacterCatalog.RefreshUnitOwners(document);
                    if (reference.Find("Character_Key", characterKey) == null)
                    {
                        var first = reference.rows.Find(row => CombatCharacterCatalog.TryOwner(reference, reference.Number(row, "Character_Key"), out _));
                        characterKey = first == null ? 0 : reference.Number(first, "Character_Key");
                    }
                }
                else { document.methods = reference; methodSourcePath = path; }
            });
        }
        private void LoadDefaultReferences(bool force)
        {
            if (force) { characterSourcePath = ""; methodSourcePath = ""; obstacleSourcePath = ""; }
            string character = string.IsNullOrEmpty(characterSourcePath) ? Path.Combine(DataDirectory, "CharacterMaster.xlsx") : characterSourcePath;
            string method = string.IsNullOrEmpty(methodSourcePath) ? Path.Combine(DataDirectory, "MethodMaster.xlsx") : methodSourcePath;
            string obstacle = string.IsNullOrEmpty(obstacleSourcePath) ? Path.Combine(DataDirectory, "ObstacleMaster.xlsx") : obstacleSourcePath;
            characterCatalogLoaded = false; obstacleCatalogLoaded = false;
            var errors = new List<string>();
            string[] paths = { character, method, obstacle };
            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                try
                {
                    if (!File.Exists(path)) { errors.Add(Path.GetFileName(path) + ": 파일이 없습니다."); continue; }
                    if (i == 0) LoadReference(path, true);
                    else if (i == 1) LoadReference(path, false);
                    else LoadObstacleReference(path);
                }
                catch (Exception exception) { errors.Add(Path.GetFileName(path) + ": " + exception.Message); }
            }
            if (errors.Count > 0) SetNotice("원본 DT 갱신 실패: " + string.Join("\n", errors), MessageType.Warning);
            else if (force) SetNotice("프로젝트의 CharacterMaster / MethodMaster / ObstacleMaster를 갱신했습니다.");
        }
    }
}
