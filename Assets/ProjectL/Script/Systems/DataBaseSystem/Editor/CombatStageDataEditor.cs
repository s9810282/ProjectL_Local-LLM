#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>
    /// 신규 환경 배치와 기존 호환용 입력을 분리하여 표시합니다.
    /// 아군 배치 구역 칸(<c>playerDeploymentCells</c>)은 필드 크기만 한 격자에서 드래그로 칠합니다(아군 배치 구역 View 설계 PV7).
    /// </summary>
    [CustomEditor(typeof(CombatStageData))]
    [CanEditMultipleObjects]
    internal sealed class CombatStageDataEditor : UnityEditor.Editor
    {
        private const string CellsField = "playerDeploymentCells";
        private const float MaxCellSize = 22f;
        private const float MinCellSize = 8f;
        private static readonly int GridControlHint = "CombatStageDataEditor.PlayerZoneGrid".GetHashCode();
        private static readonly Color ListedZoneColor = new(0.30f, 0.55f, 0.90f, 1f);
        private static readonly Color RowZoneColor = new(0.30f, 0.55f, 0.90f, 0.45f);
        private static readonly Color OtherCellColor = new(0.20f, 0.21f, 0.23f, 1f);
        private static readonly Color EnemyInZoneColor = new(0.95f, 0.25f, 0.25f, 1f);
        private static readonly Color ObjectInZoneColor = new(1f, 0.80f, 0.20f, 1f);
        // 배치 색 네모가 칸에서 차지하는 비율입니다. 바깥 테두리 쪽에는 구역 색이 남습니다.
        private const float MarkFraction = 0.55f;
        private const float LegendItemWidth = 116f;

        /// <summary>격자에 겹쳐 보이는 배치의 층입니다. 한 칸에 여럿이면 값이 큰 층의 색 하나만 보입니다.</summary>
        private enum MarkLayer
        {
            Terrain = 0,
            Object = 1,
            Obstacle = 2,
            Enemy = 3
        }

        /// <summary>격자에 색으로 보이는 배치 종류입니다. 이름·색·층은 <see cref="InfoOf"/> 한 곳에 있습니다.</summary>
        private enum GridMark
        {
            EnemyUnit,
            EnemyStructure,
            Headquarters,
            Obstacle,
            Object,
            Wall,
            Cliff,
            HighGround,
            WeakGround,
            Sandstorm,
            Tornado,
            PresenceModifier
        }

        // 배치 색 네모와 범례 배치 색 앞에 두르는 옅은 테두리입니다(검정·회색 배치가 어두운 칸에 묻히지 않게).
        private static readonly Color MarkBorderColor = new(0.92f, 0.92f, 0.92f, 0.9f);

        // 범례 순서입니다: 높은 층부터, 같은 층은 선언 순서. 층은 InfoOf 한 곳에서 읽습니다.
        private static readonly GridMark[] LegendOrder = ((GridMark[])global::System.Enum.GetValues(typeof(GridMark)))
            .OrderByDescending(mark => InfoOf(mark).Layer)
            .ThenBy(mark => mark)
            .ToArray();

        /// <summary>
        /// 배치 종류의 범례 이름·색·층입니다. 인스펙터 전용 값이며 게임 화면 색과는 따로입니다.
        /// 색은 구역 파랑·구역 밖 회색과, 구역 안 경고 테두리(빨강·노랑)와 겹치지 않게 골랐습니다.
        /// </summary>
        private static (string Label, Color Color, MarkLayer Layer) InfoOf(GridMark mark) => mark switch
        {
            GridMark.EnemyUnit => ("적 유닛", new Color(0.90f, 0.20f, 0.20f), MarkLayer.Enemy),
            GridMark.EnemyStructure => ("적 구조물", new Color(0.60f, 0.25f, 0.75f), MarkLayer.Enemy),
            GridMark.Headquarters => ("본부", new Color(1.00f, 0.45f, 0.75f), MarkLayer.Enemy),
            GridMark.Obstacle => ("장애물", new Color(0.50f, 0.30f, 0.12f), MarkLayer.Obstacle),
            GridMark.Object => ("오브젝트", new Color(0.96f, 0.96f, 0.96f), MarkLayer.Object),
            GridMark.Wall => ("벽", new Color(0.55f, 0.55f, 0.58f), MarkLayer.Terrain),
            GridMark.Cliff => ("낭떠러지", new Color(0.02f, 0.02f, 0.02f), MarkLayer.Terrain),
            GridMark.HighGround => ("고지대", new Color(0.55f, 0.62f, 0.22f), MarkLayer.Terrain),
            GridMark.WeakGround => ("약한 지반", new Color(0.95f, 0.55f, 0.15f), MarkLayer.Terrain),
            GridMark.Sandstorm => ("모래폭풍", new Color(0.92f, 0.82f, 0.55f), MarkLayer.Terrain),
            GridMark.Tornado => ("토네이도", new Color(0.40f, 0.85f, 0.95f), MarkLayer.Terrain),
            GridMark.PresenceModifier => ("위치 보정", new Color(0.15f, 0.60f, 0.60f), MarkLayer.Terrain),
            _ => throw new ArgumentOutOfRangeException(nameof(mark), mark, null)
        };

        private SerializedProperty _legacyWalls;
        private SerializedProperty _legacyTerrain;
        private bool _showLegacyEnvironment;

        // 연결된 필드 SO를 읽는 직렬화 창구입니다. 필드 참조가 바뀌면 새로 만듭니다.
        private SerializedObject _fieldObject;
        // 칸 목록을 중복 없이 읽은 것과, 겹친 칸 수입니다(같은 칸이 두 번 있으면 Core가 거절합니다).
        private readonly List<CellCoord> _listed = new();
        private int _duplicateCount;
        private readonly List<(CellCoord Cell, GridMark Mark)> _marks = new();
        // 판 안에 놓인 배치 종류입니다. 범례는 이것만 보입니다(DrawGrid가 채움).
        private readonly HashSet<GridMark> _presentMarks = new();
        // 배치 목록 중 필드 이름으로 찾지 못한 것이 있는지입니다(이름이 바뀌면 그 목록만 건너뛰고 알림).
        private bool _marksIncomplete;
        // 끌기 한 번의 상태입니다: 칠하기인지 지우기인지, 마지막으로 바꾼 칸, 하나로 묶을 Undo 묶음, 잡고 있는 컨트롤(0이면 끌기 없음).
        private bool _painting;
        private CellCoord _lastStrokeCell;
        private int _strokeGroup;
        private int _strokeControl;

        private void OnEnable()
        {
            _legacyWalls = serializedObject.FindProperty("initialWallCells");
            _legacyTerrain = serializedObject.FindProperty("staticTerrainPlacements");
        }

        private void OnDisable()
        {
            EndStroke();
            ReleaseFieldObject();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            DrawPropertiesExcluding(serializedObject, "m_Script", "initialWallCells", "staticTerrainPlacements", CellsField);

            DrawPlayerZone();

            EditorGUILayout.Space();
            if (HasLegacyPlacements())
            {
                EditorGUILayout.HelpBox(
                    "호환용 Wall/Terrain 배치가 남아 있고 전투 데이터에 함께 포함됩니다. Environment에 같은 배치를 중복 작성하지 마세요.",
                    MessageType.Info);
            }

            _showLegacyEnvironment = EditorGUILayout.Foldout(
                _showLegacyEnvironment, "Legacy Environment (호환용)", true);
            if (_showLegacyEnvironment)
            {
                EditorGUILayout.HelpBox(
                    "기존 에셋 확인·수정용 입력입니다. 신규 배치는 Environment에 작성하고, 벽은 Terrain Placements에 Wall 정의를 연결하세요.",
                    MessageType.Info);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(_legacyWalls, new GUIContent("Legacy Wall Cells"), true);
                    EditorGUILayout.PropertyField(_legacyTerrain, new GUIContent("Legacy Terrain Placements"), true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        #region 아군 배치 구역 격자

        private void DrawPlayerZone()
        {
            EditorGUILayout.Space();
            SerializedProperty cells = serializedObject.FindProperty(CellsField);
            // 머리 툴팁은 데이터 칸의 [Tooltip]을 그대로 씁니다(기본 배열 표시를 이 묶음이 대신해서).
            EditorGUILayout.LabelField(new GUIContent("아군 배치 구역", cells?.tooltip), EditorStyles.boldLabel);
            if (cells == null)
            {
                EditorGUILayout.HelpBox(
                    $"CombatStageData에서 칸 목록({CellsField})을 찾지 못했습니다. 필드 이름이 바뀌었으면 이 인스펙터의 이름도 맞추세요.",
                    MessageType.Warning);
                return;
            }
            if (serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("스테이지 하나를 고르면 격자로 아군 배치 칸을 칠할 수 있습니다.", MessageType.Info);
                return;
            }

            if (!TryReadField(out int columns, out int rows, out int rowDepth, out string fieldProblem, out MessageType problemType))
            {
                EditorGUILayout.HelpBox(fieldProblem, problemType);
                return;
            }

            ReadCells(cells);
            PlayerDeploymentZone zone = ZoneOf(rowDepth);
            EditorGUILayout.LabelField(_listed.Count > 0
                ? $"칸 지정 {_listed.Count}칸"
                : $"필드 줄 수 사용 중 (아래 {rowDepth}줄) — 칠하면 칸 지정으로 바뀝니다", EditorStyles.miniLabel);

            float size = Mathf.Clamp(Mathf.Floor((EditorGUIUtility.currentViewWidth - 48f) / columns), MinCellSize, MaxCellSize);
            Rect grid = GUILayoutUtility.GetRect(columns * size, rows * size, GUILayout.ExpandWidth(false));
            if (HandleGridInput(grid, size, columns, rows, rowDepth, zone, cells))
            {
                ReadCells(cells);
                zone = ZoneOf(rowDepth);
            }

            CollectMarks();
            (int enemiesInZone, int objectsInZone) = DrawGrid(grid, size, columns, rows, zone);
            DrawLegend();

            using (new EditorGUI.DisabledScope(cells.arraySize == 0))
            {
                if (GUILayout.Button("줄 구역으로 되돌리기 (칸 목록 비우기)"))
                    cells.arraySize = 0;
            }

            if (_duplicateCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"칸 목록에 같은 칸이 {_duplicateCount}번 겹쳐 있습니다. 이대로면 부트 DB 생성에서 멈춥니다. 격자를 한 번 칠하면 정리돼 저장됩니다.",
                    MessageType.Error);
            }
            // "판 안" 규칙은 Core 값(PlayerDeploymentZone.CellsOutside) 한 곳에서 판정합니다.
            int outside = zone.CellsOutside(columns, rows).Count;
            if (outside > 0)
            {
                EditorGUILayout.HelpBox(
                    $"판({columns}×{rows}) 밖 칸이 {outside}개 있습니다. 이대로면 부트 DB 생성에서 멈춥니다. 다음에 격자를 칠하면 판 밖 칸은 지워집니다.",
                    MessageType.Warning);
            }
            if (_marksIncomplete)
            {
                EditorGUILayout.HelpBox(
                    "배치 목록 일부를 필드 이름으로 찾지 못해 격자에 보이지 않습니다. CombatStageData 필드 이름이 바뀌었으면 이 인스펙터의 이름도 맞추세요.",
                    MessageType.Warning);
            }
            if (enemiesInZone > 0)
            {
                EditorGUILayout.HelpBox(
                    $"아군 배치 구역 안에 적 배치(앵커 칸 기준)가 {enemiesInZone}개 있습니다. 전투 진입 때 Core가 멈춥니다.",
                    MessageType.Warning);
            }
            if (objectsInZone > 0)
            {
                EditorGUILayout.HelpBox(
                    $"아군 배치 구역 안에 오브젝트가 {objectsInZone}개 있습니다. 그 칸에 아군을 놓으면 배치하는 순간 발동합니다.",
                    MessageType.Warning);
            }
            EditorGUILayout.LabelField(
                "칸 바탕 = 구역, 가운데 네모 = 놓인 배치입니다(색은 위 범례). 한 칸에 여럿이면 적 > 장애물 > 오브젝트 > 지형 순으로 하나만 칠합니다. " +
                "빨강 테두리 = 구역 안의 적, 노랑 테두리 = 구역 안의 오브젝트. 모두 놓인 칸(앵커)만 보고 크기·회전은 보지 않습니다. " +
                "벽·낭떠러지·장애물·적이 있는 칸에는 아군을 놓을 수 없습니다(정확한 판정은 Core). " +
                "칸 목록을 비우면 필드 줄 수 구역으로 돌아가므로 마지막 한 칸은 지울 수 없습니다.",
                EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>
        /// 색 범례입니다. 구역 색 둘(지금 구역 모양·구역 밖)과, 판 안에 실제로 놓인 배치 종류만 보입니다(<see cref="_presentMarks"/>).
        /// 배치 색은 격자와 같게 옅은 테두리를 두릅니다. 한 줄에 들어가는 만큼씩 나눠 그립니다.
        /// </summary>
        private void DrawLegend()
        {
            var items = new List<(string Label, Color Color, bool Bordered)>
            {
                _listed.Count > 0 ? ("칸 지정 구역", ListedZoneColor, false) : ("필드 줄 수 구역", RowZoneColor, false),
                ("구역 밖", OtherCellColor, false)
            };
            foreach (GridMark mark in LegendOrder)
            {
                if (!_presentMarks.Contains(mark))
                    continue;
                (string label, Color color, _) = InfoOf(mark);
                items.Add((label, color, true));
            }

            int perRow = Mathf.Max(1, Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 40f) / LegendItemWidth));
            for (int start = 0; start < items.Count; start += perRow)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = start; i < Math.Min(start + perRow, items.Count); i++)
                {
                    Rect item = GUILayoutUtility.GetRect(LegendItemWidth, EditorGUIUtility.singleLineHeight,
                        GUILayout.Width(LegendItemWidth));
                    if (items[i].Bordered)
                        EditorGUI.DrawRect(new Rect(item.x, item.y + 2f, 14f, 14f), MarkBorderColor);
                    EditorGUI.DrawRect(new Rect(item.x + 1f, item.y + 3f, 12f, 12f), items[i].Color);
                    GUI.Label(new Rect(item.x + 18f, item.y, item.width - 18f, item.height), items[i].Label, EditorStyles.miniLabel);
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// 연결된 필드 SO의 가로·세로·아군 줄 수를 읽습니다. GetData는 잘못된 값에서 예외를 던져 쓰지 않습니다(계획 TP-7).
        /// 읽지 못하면 이유와 안내 종류를 돌려줍니다. 필드 이름이 바뀌어도 인스펙터 전체가 멈추지 않게 합니다.
        /// </summary>
        private bool TryReadField(out int columns, out int rows, out int rowDepth, out string problem, out MessageType problemType)
        {
            columns = rows = rowDepth = 0;
            problem = null;
            problemType = MessageType.Info;
            SerializedProperty reference = serializedObject.FindProperty("fieldDefinition");
            UnityEngine.Object field = reference?.objectReferenceValue;
            if (field == null)
            {
                ReleaseFieldObject();
                problem = reference == null
                    ? "CombatStageData에서 fieldDefinition을 찾지 못했습니다. 필드 이름이 바뀌었으면 이 인스펙터의 이름도 맞추세요."
                    : "Field Definition을 연결하면 필드 크기만 한 격자가 보입니다.";
                problemType = reference == null ? MessageType.Warning : MessageType.Info;
                return false;
            }

            if (_fieldObject == null || _fieldObject.targetObject != field)
            {
                ReleaseFieldObject();
                _fieldObject = new SerializedObject(field);
            }

            _fieldObject.Update();
            SerializedProperty columnCount = _fieldObject.FindProperty("columnCount");
            SerializedProperty rowCount = _fieldObject.FindProperty("rowCount");
            SerializedProperty depth = _fieldObject.FindProperty("playerDeploymentDepth");
            if (columnCount == null || rowCount == null || depth == null)
            {
                problem = "필드 SO에서 columnCount·rowCount·playerDeploymentDepth를 찾지 못했습니다. 필드 이름이 바뀌었으면 이 인스펙터의 이름도 맞추세요.";
                problemType = MessageType.Warning;
                return false;
            }

            columns = columnCount.intValue;
            rows = rowCount.intValue;
            rowDepth = Math.Max(0, depth.intValue);
            if (columns > 0 && rows > 0)
                return true;
            problem = $"필드 크기({columns}×{rows})가 1칸보다 작아 격자를 그리지 않습니다.";
            problemType = MessageType.Warning;
            return false;
        }

        private void ReleaseFieldObject()
        {
            _fieldObject?.Dispose();
            _fieldObject = null;
        }

        private void ReadCells(SerializedProperty cells)
        {
            _listed.Clear();
            _duplicateCount = 0;
            var seen = new HashSet<CellCoord>();
            for (int i = 0; i < cells.arraySize; i++)
            {
                CellCoord cell = ReadCell(cells.GetArrayElementAtIndex(i));
                if (seen.Add(cell))
                    _listed.Add(cell);
                else
                    _duplicateCount++;
            }
        }

        /// <summary>보이는 구역입니다. 판정은 Core 값 <see cref="PlayerDeploymentZone"/>에 묻습니다.</summary>
        private PlayerDeploymentZone ZoneOf(int rowDepth) =>
            _listed.Count > 0 ? PlayerDeploymentZone.Of(_listed) : FallbackZone(rowDepth);

        /// <summary>
        /// 칸 목록이 비었을 때의 구역(필드 아군 줄 수, 아래부터)입니다.
        /// 이 대체 규칙의 원본은 <c>BattleDefinitionAdapter</c>이고 여기는 그 사본입니다. 이 파일에서는 이 함수 한 곳만 씁니다.
        /// </summary>
        private static PlayerDeploymentZone FallbackZone(int rowDepth) => PlayerDeploymentZone.Rows(rowDepth);

        /// <summary>
        /// 격자 누름·끌기·뗌을 받습니다. 칸 목록을 바꿨으면 true입니다. 끌기 한 번은 Undo 한 번입니다(계획 TP-5).
        /// 읽기 전용(GUI.enabled 꺼짐)이면 받지 않습니다. 빠르게 끌어 건너뛴 칸은 지난 칸에서 직선으로 이어 칠합니다.
        /// </summary>
        private bool HandleGridInput(Rect grid, float size, int columns, int rows, int rowDepth,
            PlayerDeploymentZone zone, SerializedProperty cells)
        {
            int id = GUIUtility.GetControlID(GridControlHint, FocusType.Passive, grid);
            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    if (!GUI.enabled || e.button != 0 || !TryCellAt(grid, size, columns, rows, e.mousePosition, out CellCoord pressed))
                        return false;
                    // 누른 칸이 구역 밖이면 이번 끌기는 칠하기, 안이면 지우기입니다.
                    _painting = !zone.Contains(pressed);
                    Undo.IncrementCurrentGroup();
                    Undo.SetCurrentGroupName(_painting ? "아군 배치 구역 칠하기" : "아군 배치 구역 지우기");
                    _strokeGroup = Undo.GetCurrentGroup();
                    _strokeControl = id;
                    GUIUtility.hotControl = id;
                    e.Use();
                    return ApplyStroke(new[] { pressed }, columns, rows, rowDepth, zone, cells);

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        return false;
                    e.Use();
                    if (!TryCellAt(grid, size, columns, rows, e.mousePosition, out CellCoord dragged) || dragged == _lastStrokeCell)
                        return false;
                    return ApplyStroke(CellsAfter(_lastStrokeCell, dragged), columns, rows, rowDepth, zone, cells);

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != id)
                        return false;
                    EndStroke();
                    e.Use();
                    return false;
            }

            return false;
        }

        /// <summary>끌기를 닫습니다. 뗄 때와, 끄는 도중 인스펙터가 사라질 때(OnDisable) 부릅니다. 끌기가 없으면 아무것도 하지 않습니다.</summary>
        private void EndStroke()
        {
            if (_strokeControl == 0)
                return;
            if (GUIUtility.hotControl == _strokeControl)
                GUIUtility.hotControl = 0;
            Undo.CollapseUndoOperations(_strokeGroup);
            _strokeControl = 0;
        }

        /// <summary>
        /// 이번 끌기의 동작을 칸들에 적용하고, 바뀌었으면 칸 목록을 한 번 씁니다.
        /// - 판 밖 칸은 Core가 거절하는 칸이라 칠할 때 함께 버립니다(중복 정리와 같은 방식, 검토 M1). 판정은 Core <c>CellsOutside</c>.
        /// - 남은 칸이 없으면 대체 구역(줄 구역)을 먼저 칸 목록으로 펼칩니다(설계 P3). 판정은 Core <c>Contains</c>.
        /// - 칸 목록을 비우면 줄 구역으로 돌아가 버리므로 마지막 한 칸은 지우지 않습니다.
        /// </summary>
        private bool ApplyStroke(IReadOnlyList<CellCoord> stroke, int columns, int rows, int rowDepth,
            PlayerDeploymentZone zone, SerializedProperty cells)
        {
            _lastStrokeCell = stroke[stroke.Count - 1];
            var outside = new HashSet<CellCoord>(zone.CellsOutside(columns, rows));
            var next = new HashSet<CellCoord>(_listed.Where(cell => !outside.Contains(cell)));
            bool cleaned = outside.Count > 0 || _duplicateCount > 0;
            bool expanded = next.Count == 0;
            if (expanded)
            {
                PlayerDeploymentZone fallback = FallbackZone(rowDepth);
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        var cell = new CellCoord(x, y);
                        if (fallback.Contains(cell))
                            next.Add(cell);
                    }
                }
            }

            bool changed = false;
            foreach (CellCoord cell in stroke)
                changed |= _painting ? next.Add(cell) : next.Count > 1 && next.Remove(cell);
            if (!changed && !expanded && !cleaned)
                return false;

            WriteCells(cells, next);
            return true;
        }

        /// <summary>두 칸 사이를 잇는 직선 위의 칸들입니다(시작 칸은 빼고 끝 칸은 넣음, 브레젠햄). 두 칸이 판 안이면 결과도 판 안입니다.</summary>
        private static List<CellCoord> CellsAfter(CellCoord from, CellCoord to)
        {
            var line = new List<CellCoord>();
            int x = from.X;
            int y = from.Y;
            int dx = Math.Abs(to.X - x);
            int dy = -Math.Abs(to.Y - y);
            int stepX = x < to.X ? 1 : -1;
            int stepY = y < to.Y ? 1 : -1;
            int error = dx + dy;
            while (x != to.X || y != to.Y)
            {
                int twice = 2 * error;
                if (twice >= dy)
                {
                    error += dy;
                    x += stepX;
                }
                if (twice <= dx)
                {
                    error += dx;
                    y += stepY;
                }
                line.Add(new CellCoord(x, y));
            }

            return line;
        }

        /// <summary>칸 목록을 y → x 순서로 씁니다(계획 TP-6, Core <c>PlayerDeploymentZone.Of</c>와 같은 순서).</summary>
        private static void WriteCells(SerializedProperty cells, IEnumerable<CellCoord> values)
        {
            CellCoord[] sorted = values.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();
            cells.arraySize = sorted.Length;
            for (int i = 0; i < sorted.Length; i++)
            {
                SerializedProperty element = cells.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("x").intValue = sorted[i].X;
                element.FindPropertyRelative("y").intValue = sorted[i].Y;
            }
        }

        /// <summary>
        /// 격자에 겹쳐 볼 배치의 칸을 모읍니다: 고정 적 유닛·적 구조물·본부, 장애물, 오브젝트, 지형.
        /// 모두 놓인 칸(앵커)만 봅니다. 크기·회전에 따른 점유 칸은 Core 규칙이라 베끼지 않습니다(계획 TP-3).
        /// 무작위 최초 위치 토네이도는 칸이 없어 빼고, 옛 호환 지형 입력(Legacy)도 넣지 않습니다.
        /// </summary>
        private void CollectMarks()
        {
            _marks.Clear();
            _marksIncomplete = false;
            AddMarks("initialEnemyPlacements", "unitDefinition", "anchor", GridMark.EnemyUnit);
            AddMarks("initialEnemyStructurePlacements", "structureDefinition", "anchor", GridMark.EnemyStructure);
            SerializedProperty headquarters = serializedObject.FindProperty("headquarters");
            SerializedProperty headquartersAnchor = serializedObject.FindProperty("enemyHeadquartersAnchor");
            if (headquarters == null || headquartersAnchor == null)
                _marksIncomplete = true;
            else if (headquarters.objectReferenceValue != null)
                _marks.Add((ReadCell(headquartersAnchor), GridMark.Headquarters));
            AddMarks("environment.obstaclePlacements", "definition", "anchor", GridMark.Obstacle);
            AddMarks("environment.objectPlacements", "definition", "cell", GridMark.Object);

            SerializedProperty terrains = serializedObject.FindProperty("environment.terrainPlacements");
            if (terrains == null)
            {
                _marksIncomplete = true;
                return;
            }
            for (int i = 0; i < terrains.arraySize; i++)
            {
                SerializedProperty placement = terrains.GetArrayElementAtIndex(i);
                SerializedProperty random = placement.FindPropertyRelative("randomizeInitialPosition");
                SerializedProperty definition = placement.FindPropertyRelative("definition");
                SerializedProperty cell = placement.FindPropertyRelative("cell");
                if (random == null || definition == null || cell == null)
                {
                    _marksIncomplete = true;
                    return;
                }
                if (random.boolValue || definition.objectReferenceValue is not TerrainDefinitionDataSO terrain)
                    continue;
                if (TryTerrainMark(terrain.Kind, out GridMark mark))
                    _marks.Add((ReadCell(cell), mark));
            }
        }

        /// <summary>배치 목록 하나의 앵커 칸을 모읍니다. 정의가 빈 항목은 건너뛰고, 필드 이름을 찾지 못하면 그 목록만 건너뛰고 알립니다.</summary>
        private void AddMarks(string listPath, string definitionField, string cellField, GridMark mark)
        {
            SerializedProperty list = serializedObject.FindProperty(listPath);
            if (list == null)
            {
                _marksIncomplete = true;
                return;
            }
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty placement = list.GetArrayElementAtIndex(i);
                SerializedProperty definition = placement.FindPropertyRelative(definitionField);
                SerializedProperty cell = placement.FindPropertyRelative(cellField);
                if (definition == null || cell == null)
                {
                    _marksIncomplete = true;
                    return;
                }
                if (definition.objectReferenceValue != null)
                    _marks.Add((ReadCell(cell), mark));
            }
        }

        private static bool TryTerrainMark(TerrainContentKind kind, out GridMark mark)
        {
            switch (kind)
            {
                case TerrainContentKind.HighGround: mark = GridMark.HighGround; return true;
                case TerrainContentKind.Sandstorm: mark = GridMark.Sandstorm; return true;
                case TerrainContentKind.Wall: mark = GridMark.Wall; return true;
                case TerrainContentKind.WeakGround: mark = GridMark.WeakGround; return true;
                case TerrainContentKind.Cliff: mark = GridMark.Cliff; return true;
                case TerrainContentKind.Tornado: mark = GridMark.Tornado; return true;
                case TerrainContentKind.PresenceModifier: mark = GridMark.PresenceModifier; return true;
                default: mark = default; return false;
            }
        }

        /// <summary>
        /// 격자를 그리고, 아군 구역 안에 앵커가 있는 적 배치 수·오브젝트 수를 돌려줍니다. 위가 큰 y입니다(계획 TP-4).
        /// 칸 바탕은 구역 색, 가운데 네모는 가장 높은 층 배치의 색입니다(<see cref="InfoOf"/>).
        /// 구역 안의 적은 빨강, 오브젝트(적이 없는 칸)는 노랑 테두리로 표시합니다.
        /// </summary>
        private (int EnemiesInZone, int ObjectsInZone) DrawGrid(Rect grid, float size, int columns, int rows, PlayerDeploymentZone zone)
        {
            Color zoneColor = _listed.Count > 0 ? ListedZoneColor : RowZoneColor;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Rect cell = Shrink(CellRect(grid, size, rows, x, y), 1f);
                    EditorGUI.DrawRect(cell, zone.Contains(new CellCoord(x, y)) ? zoneColor : OtherCellColor);
                }
            }

            int enemiesInZone = 0;
            int objectsInZone = 0;
            var shown = new Dictionary<CellCoord, GridMark>();
            var flagged = new Dictionary<CellCoord, Color>();
            _presentMarks.Clear();
            foreach ((CellCoord cell, GridMark mark) in _marks)
            {
                if (cell.X < 0 || cell.Y < 0 || cell.X >= columns || cell.Y >= rows)
                    continue;
                _presentMarks.Add(mark);
                MarkLayer layer = InfoOf(mark).Layer;
                if (!shown.TryGetValue(cell, out GridMark current) || layer > InfoOf(current).Layer)
                    shown[cell] = mark;
                if (!zone.Contains(cell))
                    continue;
                if (layer == MarkLayer.Enemy)
                {
                    enemiesInZone++;
                    flagged[cell] = EnemyInZoneColor;
                }
                else if (layer == MarkLayer.Object)
                {
                    objectsInZone++;
                    if (!flagged.ContainsKey(cell))
                        flagged[cell] = ObjectInZoneColor;
                }
            }

            float inset = size * (1f - MarkFraction) * 0.5f;
            foreach (KeyValuePair<CellCoord, GridMark> pair in shown)
            {
                Rect cellRect = CellRect(grid, size, rows, pair.Key.X, pair.Key.Y);
                EditorGUI.DrawRect(Shrink(cellRect, inset - 1f), MarkBorderColor);
                EditorGUI.DrawRect(Shrink(cellRect, inset), InfoOf(pair.Value).Color);
            }
            foreach (KeyValuePair<CellCoord, Color> pair in flagged)
                DrawOutline(CellRect(grid, size, rows, pair.Key.X, pair.Key.Y), pair.Value);

            return (enemiesInZone, objectsInZone);
        }

        private static bool TryCellAt(Rect grid, float size, int columns, int rows, Vector2 point, out CellCoord cell)
        {
            cell = default;
            if (!grid.Contains(point))
                return false;
            int x = Mathf.FloorToInt((point.x - grid.x) / size);
            int y = rows - 1 - Mathf.FloorToInt((point.y - grid.y) / size);
            if (x < 0 || y < 0 || x >= columns || y >= rows)
                return false;
            cell = new CellCoord(x, y);
            return true;
        }

        private static Rect CellRect(Rect grid, float size, int rows, int x, int y) =>
            new(grid.x + x * size, grid.y + (rows - 1 - y) * size, size, size);

        private static Rect Shrink(Rect rect, float by) =>
            new(rect.x + by, rect.y + by, rect.width - by * 2f, rect.height - by * 2f);

        private static void DrawOutline(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2f, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), color);
        }

        private static CellCoord ReadCell(SerializedProperty cellInput) =>
            new(cellInput.FindPropertyRelative("x").intValue, cellInput.FindPropertyRelative("y").intValue);

        #endregion

        private bool HasLegacyPlacements()
        {
            // 다중 선택에서 배열 길이가 가장 작은 대상만 확인하지 않도록 각 에셋을 대조합니다.
            if (!serializedObject.isEditingMultipleObjects)
            {
                return _legacyWalls.arraySize > 0 || _legacyTerrain.arraySize > 0;
            }

            foreach (var selected in targets)
            {
                using (var item = new SerializedObject(selected))
                {
                    if (item.FindProperty("initialWallCells").arraySize > 0 ||
                        item.FindProperty("staticTerrainPlacements").arraySize > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
#endif
