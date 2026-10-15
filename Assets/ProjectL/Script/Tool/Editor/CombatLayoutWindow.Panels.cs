using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal enum CombatLayoutDetailsMode { Map, Unit, Character, Obstacle }

    public sealed partial class CombatLayoutWindow
    {
        [SerializeField] private CombatLayoutDetailsMode detailsMode;
        private Vector2 detailsScroll;
        private bool showRawCharacter;
        [SerializeField] private bool showUnitSection = true, showCharacterCatalog = true, showPlacedUnits = true;
        [SerializeField] private bool showTileSection = true, showTileTools = true, showPlacedWalls;
        [SerializeField] private bool showObstacleCatalog = true;
        [SerializeField] private bool showMapOverview = true, showTileDetails = true, showMapSettings = true;

        private void DrawSidebarSections()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showUnitSection = EditorGUILayout.Foldout(showUnitSection, "유닛", true, EditorStyles.foldoutHeader);
                if (showUnitSection)
                {
                    int count = characterCatalogLoaded ? document.characters.rows.Count : 0;
                    showCharacterCatalog = EditorGUILayout.Foldout(showCharacterCatalog, $"현재 DT 유닛 목록 ({count})", true);
                    if (showCharacterCatalog) DrawCharacters();
                    GUILayout.Space(6);
                    showPlacedUnits = EditorGUILayout.Foldout(showPlacedUnits, $"배치 목록 ({document.units.Count})", true);
                    if (showPlacedUnits) DrawUnitList();
                }
            }
            GUILayout.Space(6);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showTileSection = EditorGUILayout.Foldout(showTileSection, "장애물 · 벽 / 타일", true, EditorStyles.foldoutHeader);
                if (showTileSection)
                {
                    int count = obstacleCatalogLoaded ? document.obstacles.rows.Count : 0;
                    showObstacleCatalog = EditorGUILayout.Foldout(showObstacleCatalog, $"현재 DT 장애물 목록 ({count})", true);
                    if (showObstacleCatalog) DrawObstacleCatalog();
                    GUILayout.Space(6);
                    showTileTools = EditorGUILayout.Foldout(showTileTools, "벽 · 타일 편집 도구", true);
                    if (showTileTools) DrawTileTools();
                    GUILayout.Space(6);
                    int wallCount = 0;
                    foreach (var cell in document.cells)
                        if (cell.type == "Blocked") wallCount++;
                    showPlacedWalls = EditorGUILayout.Foldout(showPlacedWalls, $"배치된 벽 목록 ({wallCount})", true);
                    if (showPlacedWalls) DrawWallList(wallCount);
                }
            }
        }

        private void DrawTileTools()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawToolButton(3); DrawToolButton(4); DrawToolButton(5);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawToolButton(2); DrawToolButton(6); DrawToolButton(7);
            }
            brushHeight = EditorGUILayout.IntSlider("칠할 높이", brushHeight, 0, 20);
            EditorGUILayout.LabelField("도구 선택 → 맵 클릭·드래그", EditorStyles.miniLabel);
        }

        private void DrawToolButton(int index)
        {
            Color previous = GUI.backgroundColor;
            if (tool == index) GUI.backgroundColor = new Color(.5f, .85f, .8f);
            bool clicked = GUILayout.Button(Tools[index], GUILayout.MinWidth(0));
            GUI.backgroundColor = previous;
            if (!clicked) return;
            EndStroke();
            tool = index; selectedUnitKey = 0; showTileDetails = true;
            ShowDetails(CombatLayoutDetailsMode.Map);
        }

        private void DrawWallList(int count)
        {
            if (count == 0)
            {
                EditorGUILayout.LabelField("배치된 벽이 없습니다.", EditorStyles.miniLabel);
                return;
            }
            EditorGUILayout.LabelField("좌표를 누르면 해당 벽 타일 선택", EditorStyles.miniLabel);
            foreach (var cell in document.cells)
            {
                if (cell.type != "Blocked") continue;
                Color previous = GUI.backgroundColor;
                if (selectedCell.x == cell.x && selectedCell.y == cell.y)
                    GUI.backgroundColor = new Color(.5f, .85f, .8f);
                string name = cell.obstacleKey == 0 ? "벽 (미지정)" : ObstacleName(document.obstacles, cell.obstacleKey);
                bool clicked = GUILayout.Button($"{name}\n({cell.x}, {cell.y}) · 높이 {cell.elevation}", GUILayout.Height(38));
                GUI.backgroundColor = previous;
                if (!clicked) continue;
                EndStroke();
                selectedCell = new Vector2Int(cell.x, cell.y); selectedUnitKey = 0; tool = 7;
                showTileDetails = true;
                ShowDetails(CombatLayoutDetailsMode.Map);
            }
        }

        internal static string ObstacleName(CombatReferenceSheet sheet, int key)
        {
            var row = sheet.Find("Obstacle_Key", key);
            if (row == null) return "Obstacle " + key;
            string name = sheet.Value(row, "Obstacle_NameKOR");
            if (string.IsNullOrEmpty(name)) name = sheet.Value(row, "Obstacle_Name");
            return string.IsNullOrEmpty(name) ? "Obstacle " + key : name;
        }

        private void DrawObstacleCatalog()
        {
            if (GUILayout.Button("장애물 엑셀 읽기"))
            {
                string path = EditorUtility.OpenFilePanelWithFilters("ObstacleMaster 원본 DT 열기", DataDirectory, new[] { "Excel", "xlsx,xls" });
                if (!string.IsNullOrEmpty(path))
                    try { LoadObstacleReference(path); SetNotice("ObstacleMaster 원본 DT를 불러왔습니다."); }
                    catch (Exception exception) { SetNotice(exception.Message, MessageType.Error); }
            }
            if (GUILayout.Button("프로젝트 DT 다시 읽기")) LoadDefaultReferences(true);
            obstacleSearch = EditorGUILayout.TextField("검색", obstacleSearch);
            if (!obstacleCatalogLoaded)
            {
                EditorGUILayout.HelpBox("ObstacleMaster 원본 엑셀을 읽어 주세요.", MessageType.Warning);
                return;
            }
            if (document.obstacles.rows.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 ObstacleMaster에 데이터 행이 없습니다. 등록된 장애물만 목록에 표시됩니다.", MessageType.Info);
                return;
            }
            EditorGUILayout.LabelField("장애물 선택 → 맵 클릭·드래그로 벽 배치", EditorStyles.miniLabel);
            foreach (var row in document.obstacles.rows)
            {
                int key = document.obstacles.Number(row, "Obstacle_Key");
                string name = ObstacleName(document.obstacles, key);
                if (!string.IsNullOrEmpty(obstacleSearch) && (name + key).IndexOf(obstacleSearch, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Color previous = GUI.backgroundColor;
                if (obstacleKey == key) GUI.backgroundColor = new Color(.5f, .85f, .8f);
                bool clicked = GUILayout.Button($"{name} [{key}]\n생명력 {document.obstacles.Value(row, "Obstacle_HealthValue")}", GUILayout.Height(42));
                GUI.backgroundColor = previous;
                if (clicked) SelectObstacle(key);
            }
        }

        private void SelectObstacle(int key)
        {
            EndStroke();
            obstacleKey = key; selectedUnitKey = 0; tool = 4;
            ShowDetails(CombatLayoutDetailsMode.Obstacle);
        }

        private void LoadObstacleReference(string path)
        {
            var reference = CombatLayoutWorkbook.LoadReferences(path, "ObstacleMaster", "Obstacle_Key");
            Change("장애물 DT 갱신", () =>
            {
                document.obstacles = reference; obstacleCatalogLoaded = true; obstacleSourcePath = path;
                if (reference.Find("Obstacle_Key", obstacleKey) == null) obstacleKey = 0;
            });
        }

        private int DrawObstaclePicker(int current)
        {
            var keys = new List<int> { 0 };
            var names = new List<string> { "미지정 (기존 단순 벽)" };
            if (obstacleCatalogLoaded)
                foreach (var row in document.obstacles.rows)
                {
                    int key = document.obstacles.Number(row, "Obstacle_Key");
                    keys.Add(key); names.Add(ObstacleName(document.obstacles, key) + " [" + key + "]");
                }
            if (current > 0 && !keys.Contains(current))
            {
                keys.Add(current); names.Add("Obstacle " + current + " (원본 DT 확인 필요)");
            }
            int index = EditorGUILayout.Popup("Obstacle", keys.IndexOf(current), names.ToArray());
            return index >= 0 && index < keys.Count ? keys[index] : current;
        }

        private void DrawObstacleReference(int key)
        {
            if (key == 0) return;
            var row = document.obstacles.Find("Obstacle_Key", key);
            if (!obstacleCatalogLoaded) EditorGUILayout.HelpBox("ObstacleMaster 원본 DT를 읽어 최신 장애물 정보를 확인해 주세요.", MessageType.Warning);
            if (row == null)
            {
                EditorGUILayout.HelpBox($"현재 DT에 Obstacle 키 {key}가 없습니다. 배치 키는 유지됩니다.", MessageType.Warning);
                return;
            }
            EditorGUILayout.LabelField(ObstacleName(document.obstacles, key), EditorStyles.boldLabel);
            foreach (var pair in new[] { ("생명력", "HealthValue"), ("피격 효과 키", "HitEffect"), ("파괴 효과 키", "DestroyEffect"), ("효과 적용 지점", "EffectApplicationPoint"), ("설명 키", "Description") })
            {
                string value = document.obstacles.Value(row, "Obstacle_" + pair.Item2);
                EditorGUILayout.LabelField(pair.Item1, string.IsNullOrEmpty(value) ? "—" : value);
            }
        }

        private void DrawObstacleDetails()
        {
            if (obstacleKey == 0) { EditorGUILayout.HelpBox("왼쪽의 현재 DT 장애물 목록에서 항목을 선택하세요.", MessageType.Info); return; }
            EditorGUILayout.LabelField("Obstacle 키", obstacleKey.ToString());
            DrawObstacleReference(obstacleKey);
            using (new EditorGUI.DisabledScope(!obstacleCatalogLoaded || document.obstacles.Find("Obstacle_Key", obstacleKey) == null))
                if (GUILayout.Button("이 장애물로 벽 배치하기")) SelectObstacle(obstacleKey);
        }

        private void ShowDetails(CombatLayoutDetailsMode mode)
        {
            detailsMode = mode;
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (document == null) return;
            var unit = document.units.Find(u => u.key == selectedUnitKey);
            if (detailsMode == CombatLayoutDetailsMode.Unit && unit == null) detailsMode = CombatLayoutDetailsMode.Map;
            if (detailsMode == CombatLayoutDetailsMode.Unit) selectedCell = new Vector2Int(unit.x, unit.y);
            Repaint();
        }

        private void DrawDetailsPanel()
        {
            EditorGUILayout.LabelField("선택 정보", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            int tab = GUILayout.Toolbar(detailsMode == CombatLayoutDetailsMode.Map ? 0 : detailsMode == CombatLayoutDetailsMode.Obstacle ? 2 : 1, new[] { "맵 · 타일 정보", "유닛 정보", "장애물 정보" });
            if (EditorGUI.EndChangeCheck())
                ShowDetails(tab == 0 ? CombatLayoutDetailsMode.Map : tab == 2 ? CombatLayoutDetailsMode.Obstacle : selectedUnitKey != 0 ? CombatLayoutDetailsMode.Unit : CombatLayoutDetailsMode.Character);
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 125;
            try
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(detailsScroll))
                {
                    detailsScroll = scroll.scrollPosition;
                    if (detailsMode == CombatLayoutDetailsMode.Map)
                    {
                        EditorGUILayout.LabelField(document.stage.name, EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"{document.stage.width} × {document.stage.height} / 배치 유닛 {document.units.Count}");
                        showMapOverview = EditorGUILayout.Foldout(showMapOverview, "전체 맵", true);
                        if (showMapOverview) DrawMapOverview();
                        GUILayout.Space(6);
                        showTileDetails = EditorGUILayout.Foldout(showTileDetails, "선택 타일 정보", true);
                        if (showTileDetails) DrawTileSelection();
                        GUILayout.Space(6);
                        showMapSettings = EditorGUILayout.Foldout(showMapSettings, "맵 설정", true);
                        if (showMapSettings) DrawSettings();
                    }
                    else if (detailsMode == CombatLayoutDetailsMode.Obstacle) DrawObstacleDetails();
                    else DrawUnitDetails();
                }
            }
            finally { EditorGUIUtility.labelWidth = previousLabelWidth; }
        }

        private void DrawUnitDetails()
        {
            var unit = detailsMode == CombatLayoutDetailsMode.Unit ? document.units.Find(u => u.key == selectedUnitKey) : null;
            int key = unit?.characterKey ?? characterKey;
            var row = document.characters.Find("Character_Key", key);
            if (key == 0) { EditorGUILayout.HelpBox("왼쪽의 현재 DT 목록에서 유닛을 선택하세요.", MessageType.Info); return; }
            EditorGUILayout.LabelField(CharacterName(document.characters, key), EditorStyles.boldLabel);
            if (unit != null) DrawPlacedUnitSelection();
            else
            {
                EditorGUILayout.LabelField("Character 키", key.ToString());
                EditorGUILayout.LabelField("DT 진형", CombatCharacterCatalog.OwnerLabel(document.characters, key));
                using (new EditorGUI.DisabledScope(!characterCatalogLoaded || !CombatCharacterCatalog.TryOwner(document.characters, key, out _)))
                    if (GUILayout.Button("이 유닛 배치하기")) { tool = 1; Repaint(); }
            }
            if (!characterCatalogLoaded) EditorGUILayout.HelpBox("원본 유닛 DT를 읽어 최신 정보와 배치 진형을 확인해 주세요.", MessageType.Warning);
            if (row == null)
            {
                EditorGUILayout.HelpBox("현재 DT에 이 Character 키가 없습니다.", MessageType.Warning);
                return;
            }
            if (!CombatCharacterCatalog.TryOwner(document.characters, key, out _))
                EditorGUILayout.HelpBox("Character_PositionType을 DT에서 1=아군 또는 2=적군으로 설정해야 배치할 수 있습니다.", MessageType.Warning);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("DT 유닛 정보", EditorStyles.boldLabel);
            DrawCharacterInfo(row);
            foreach (var pair in new[] { ("레벨", "Level"), ("최대 레벨", "MaxLevel"), ("우선순위", "Priority"), ("딜레이", "Delay"), ("최대 비용", "MaxCost"), ("이동 비용", "MoveCost"), ("공격 비용", "AttackCost") })
            {
                string value = document.characters.Value(row, "Character_" + pair.Item2);
                EditorGUILayout.LabelField(pair.Item1, string.IsNullOrEmpty(value) ? "—" : value);
            }
            DrawMethodDetails(row, "Move", "이동 방식");
            DrawMethodDetails(row, "Attack", "공격 방식");
            string html = document.characters.Value(row, "EditorCharacter_Color");
            Color current = ColorUtility.TryParseHtmlString(html, out var color) ? color : new Color(.34f, .77f, .72f);
            EditorGUI.BeginChangeCheck();
            current = EditorGUILayout.ColorField("표시 색상", current);
            if (EditorGUI.EndChangeCheck()) Change("유닛 표시 색상 변경", () => document.characters.Set(row, "EditorCharacter_Color", "#" + ColorUtility.ToHtmlStringRGB(current)));
            showRawCharacter = EditorGUILayout.Foldout(showRawCharacter, "원본 Character DT 전체 정보", true);
            if (!showRawCharacter) return;
            using (new EditorGUI.IndentLevelScope())
                foreach (string column in document.characters.columns)
                {
                    if (column.StartsWith("EditorCharacter_", StringComparison.Ordinal)) continue;
                    EditorGUILayout.LabelField(column);
                    string value = document.characters.Value(row, column);
                    EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(value) ? "—" : value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
        }

        private void DrawMethodDetails(CombatReferenceRow character, string prefix, string label)
        {
            int key = document.characters.Number(character, "Character_" + prefix + "Method");
            var method = document.methods.Find("Method_Key", key);
            if (method == null) { EditorGUILayout.LabelField(label, key == 0 ? "미지정" : $"Method {key} / 참조 데이터 없음"); return; }
            int type = document.methods.Number(method, "Method_Type");
            string[] names = { "미지정", "십자 직선", "대각 직선", "8방향 직선", "부채", "원형" };
            EditorGUILayout.LabelField(label, $"{(type >= 0 && type < names.Length ? names[type] : "종류 " + type)} / 키 {key}");
            EditorGUILayout.LabelField("대상 선택 방식", document.methods.Value(method, "Method_ChoiceRange"));
            EditorGUILayout.LabelField("추가 지정 형태", document.methods.Value(method, "Method_Splash"));
            EditorGUILayout.LabelField("추가 지정 배율", document.methods.Value(method, "Method_SplashValue"));
        }

        private void DrawMapOverview()
        {
            Rect area = GUILayoutUtility.GetRect(100, 160, GUILayout.ExpandWidth(true));
            float size = Mathf.Min(area.width / document.stage.width, area.height / document.stage.height);
            Vector2 origin = new Vector2(area.x + (area.width - size * document.stage.width) * .5f, area.y + (area.height - size * document.stage.height) * .5f);
            foreach (var cell in document.cells)
            {
                Rect rect = new Rect(origin.x + cell.x * size, origin.y + (document.stage.height - 1 - cell.y) * size, size, size);
                Color color = cell.type == "Blocked" ? Color.gray : cell.type == "Rail" ? new Color(.6f, .5f, .25f) : new Color(.22f, .29f, .34f);
                var unit = document.Unit(cell.x, cell.y);
                if (unit != null) color = unit.owner == "Player" ? new Color(.27f, .64f, .8f) : new Color(.87f, .42f, .43f);
                if (selectedCell.x == cell.x && selectedCell.y == cell.y) color = Color.Lerp(color, Color.white, .6f);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, Mathf.Max(1, size - 1), Mathf.Max(1, size - 1)), color);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                {
                    selectedCell = new Vector2Int(cell.x, cell.y); selectedUnitKey = 0; tool = 7;
                    ShowDetails(CombatLayoutDetailsMode.Map); Event.current.Use(); GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.LabelField("전체 맵 · 클릭하면 타일 선택", EditorStyles.miniLabel);
        }
    }
}
