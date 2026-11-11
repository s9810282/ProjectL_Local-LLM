using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// 유닛 목록·생성 요청·규칙·외형·로스터 편집을 한 창에서 제공합니다.
    /// 화면은 카탈로그(<see cref="UnitAuthoringCatalog"/>)만 읽고, 에셋을 읽는 일은 새로고침 때만 합니다(설계 UA1).
    /// </summary>
    internal sealed partial class UnitAuthoringWindow : EditorWindow
    {
        [SerializeField] private Object templateUnit;
        [SerializeField] private Object templateVisual;
        [SerializeField] private Object unitTable;
        [SerializeField] private Object visualTable;
        [SerializeField] private DefaultAsset unitFolder;
        [SerializeField] private DefaultAsset visualFolder;
        [SerializeField] private string keyName = "Unit_";
        [SerializeField] private int keyValue;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private int eligibilityIndex = -1;
        [SerializeField] private bool addKey = true;
        [SerializeField] private bool creating;
        [SerializeField] private string search = "";
        [SerializeField] private int sideFilter;
        [SerializeField] private bool problemsOnly;
        [SerializeField] private float listWidth = 260;
        [SerializeField] private List<Object> rosterSelection = new();
        private UnitAuthoringCatalog _catalog;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private readonly SortedDictionary<string, string> _errors = new();
        private string _catalogMessage;
        private double _refreshAt;
        private bool _splitDrag;
        private bool _sharedFold;
        private bool _unusableFold;
        private string[] _eligibilityNames = Array.Empty<string>();

        [MenuItem("ProjectL/유닛 통합 에디터")]
        private static void Open()
        {
            var window = GetWindow<UnitAuthoringWindow>("유닛 통합 에디터");
            window.minSize = new Vector2(760, 520);
            window.Show();
        }

        private void OnEnable()
        {
            saveChangesMessage = "저장하지 않은 유닛 변경이 있습니다. 저장할까요?";
            Run("목록", RefreshCatalog);
            RestoreEditing();
            Undo.undoRedoPerformed += OnUndo;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
            EditorApplication.projectChanged -= OnProjectChanged;
            DisposeEditing();
        }

        private void OnProjectChanged() => _refreshAt = EditorApplication.timeSinceStartup + 0.5;

        private void OnInspectorUpdate()
        {
            if (_refreshAt > 0 && EditorApplication.timeSinceStartup >= _refreshAt && !Busy())
            {
                _refreshAt = 0;
                Run("목록", RefreshCatalog);
            }
            var request = UnitAuthoringOperation.Current;
            if (request != null && request.phase == UnitAuthoringOperation.Complete && !request.resultOpened)
            {
                // 먼저 기록해 실패해도 다시 시도하지 않습니다(오류 E-R1).
                UnitAuthoringOperation.MarkResultOpened();
                Run("생성", () =>
                {
                    RefreshCatalog();
                    creating = false;
                    SetEditing(UnitAuthoringAssets.Load(request.createdUnit, UnitAuthoringAssets.UnitType));
                });
            }
            Repaint();
        }

        public override void SaveChanges()
        {
            Run("저장", SaveEditing);
            if (!IsEditingDirty()) base.SaveChanges();
        }

        public override void DiscardChanges()
        {
            Run("저장", RevertEditing);
            base.DiscardChanges();
        }

        private void OnGUI()
        {
            hasUnsavedChanges = IsEditingDirty();
            try
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("유닛 통합 에디터", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("목록 새로고침", EditorStyles.toolbarButton, GUILayout.Width(100))) Run("목록", RefreshCatalog);
                }
                DrawOperation();
                foreach (var error in _errors) EditorGUILayout.HelpBox(error.Key + ": " + error.Value, MessageType.Error);
                if (_catalog == null) return;
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(listWidth))) DrawList();
                    DrawSplitter();
                    using (new EditorGUILayout.VerticalScope())
                    {
                        if (creating) DrawCreation();
                        else DrawEditing();
                    }
                }
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { _errors["화면"] = exception.Message; }
        }

        private void RefreshCatalog()
        {
            _catalog = UnitAuthoringCatalog.Build();
            if (templateUnit != null)
            {
                UnitAuthoringRow row = _catalog.Find(templateUnit);
                if (row == null || !row.SourceUsable) ClearTemplate();
                else templateVisual = row.Visual;
            }
            if (keyValue < 1) keyValue = _catalog.SuggestKey();
            rosterSelection.RemoveAll(roster => roster == null);
            _catalogMessage = UnitAuthoringAssets.Catalogs(unitTable, visualTable);
            if (editingUnit == null) return;
            UnitAuthoringRow editing = _catalog.Find(editingUnit);
            if (editing == null) SetEditing(null);
            else if (editing.Visual != editingVisual && !IsEditingDirty()) SetEditing(editingUnit);
            else
            {
                _unitEditor?.Update();
                _visualEditor?.Update();
            }
        }

        #region 목록

        private void DrawList()
        {
            if (GUILayout.Button("+ 새 유닛 (복제하여 생성)", GUILayout.Height(24)) && ConfirmLeaveEditing()) creating = true;
            GUIStyle searchStyle = GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.toolbarTextField;
            search = EditorGUILayout.TextField(search ?? "", searchStyle);
            sideFilter = GUILayout.Toolbar(sideFilter, new[] { "전체", "아군", "적" });
            problemsOnly = EditorGUILayout.ToggleLeft("문제 있는 것만", problemsOnly);
            if (!string.IsNullOrEmpty(_catalog.KeySourceError)) EditorGUILayout.HelpBox(_catalog.KeySourceError, MessageType.Error);
            using var scroll = new EditorGUILayout.ScrollViewScope(_listScroll);
            _listScroll = scroll.scrollPosition;
            foreach (UnitAuthoringRow row in _catalog.Rows.Where(Matches)) DrawRow(row);
        }

        private bool Matches(UnitAuthoringRow row)
        {
            if (sideFilter == 1 && !row.IsAlly) return false;
            if (sideFilter == 2 && !row.IsEnemy) return false;
            if (problemsOnly && row.Problems.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(search)) return true;
            string text = search.Trim();
            return Has(row.DisplayName, text) || Has(row.KeyName, text) || Has(row.Unit.name, text) || row.Key.ToString() == text;
        }

        private static bool Has(string value, string text) => value != null && value.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;

        private void DrawRow(UnitAuthoringRow row)
        {
            Texture icon = row.Problems.Count > 0 ? EditorGUIUtility.IconContent("console.warnicon.sml").image
                : row.Icon != null ? AssetPreview.GetAssetPreview(row.Icon) : null;
            string tooltip = row.Problems.Count > 0 ? string.Join("\n", row.Problems) : AssetDatabase.GetAssetPath(row.Unit);
            var content = new GUIContent(row.Label, icon, tooltip);
            Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.label, GUILayout.Height(20), GUILayout.ExpandWidth(true));
            if (!creating && row.Unit == editingUnit) EditorGUI.DrawRect(rect, new Color(0.24f, 0.48f, 0.9f, 0.35f));
            GUI.Label(rect, content);
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition)) return;
            current.Use();
            if (!creating && row.Unit == editingUnit) return;
            Run("편집", () =>
            {
                if (!ConfirmLeaveEditing()) return;
                creating = false;
                SetEditing(row.Unit);
            });
        }

        private void DrawSplitter()
        {
            Rect rect = GUILayoutUtility.GetRect(6, 6, GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y, 1, rect.height), new Color(0, 0, 0, 0.3f));
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition))
            {
                _splitDrag = true;
                current.Use();
            }
            else if (_splitDrag && current.type == EventType.MouseDrag)
            {
                listWidth = Mathf.Clamp(listWidth + current.delta.x, 180, Mathf.Max(180, position.width - 420));
                current.Use();
            }
            else if (current.rawType == EventType.MouseUp) _splitDrag = false;
        }

        #endregion

        #region 생성

        private void ClearTemplate()
        {
            templateUnit = null;
            templateVisual = null;
            _eligibilityNames = Array.Empty<string>();
        }

        private void SelectTemplate(Object unit)
        {
            if (unit == null)
            {
                ClearTemplate();
                return;
            }
            // 새 상태를 모두 만든 뒤에 한꺼번에 바꿉니다(오류 E-R4).
            UnitAuthoringRow row = _catalog.Find(unit) ?? throw new InvalidOperationException("목록에 없는 유닛입니다. 새로고침해 주세요.");
            if (!row.SourceUsable) throw new InvalidOperationException(string.Join("\n", row.Problems));
            Object nextUnitTable = UnitAuthoringAssets.DefaultTable(UnitAuthoringAssets.UnitTableType, unit);
            Object nextVisualTable = UnitAuthoringAssets.DefaultTable(UnitAuthoringAssets.VisualTableType, row.Visual);
            string[] eligibility;
            using (var serialized = new SerializedObject(unit))
                eligibility = serialized.FindProperty("ownerEligibility")?.enumDisplayNames ?? Array.Empty<string>();
            templateUnit = unit;
            templateVisual = row.Visual;
            unitTable = nextUnitTable;
            visualTable = nextVisualTable;
            unitFolder = FolderOf(unit);
            visualFolder = FolderOf(row.Visual);
            _eligibilityNames = eligibility;
            eligibilityIndex = -1;
            // 원본이 든 로스터를 따라갑니다. 아군 전용은 로스터 없이도 전투에 나오므로 비웁니다(Q-UA1 A).
            rosterSelection = row.Eligibility == "PlayerOnly" ? new List<Object>() : new List<Object>(row.Rosters);
            _catalogMessage = UnitAuthoringAssets.Catalogs(unitTable, visualTable);
        }

        private void DrawCreation()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("새 유닛 — 원본을 복제하여 생성", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("편집으로 돌아가기", GUILayout.Width(120))) creating = false;
            }
            using var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll);
            _detailScroll = scroll.scrollPosition;
            EditorGUILayout.HelpBox("원본 유닛의 규칙·외형을 함께 복제합니다. 프리팹·연출·특성 참조는 원본과 공유하며 생성 후 편집할 수 있습니다.", MessageType.Info);
            using (new EditorGUI.DisabledScope(UnitAuthoringOperation.HasPending || Busy()))
            {
                DrawSourcePicker();
                if (templateUnit == null) return;
                using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("짝 Visual", templateVisual, typeof(Object), false);
                if (EditorUtility.IsDirty(templateUnit) || EditorUtility.IsDirty(templateVisual))
                {
                    EditorGUILayout.HelpBox("원본에 저장하지 않은 변경이 있습니다. 저장해야 생성할 수 있습니다.", MessageType.Warning);
                    if (GUILayout.Button("원본 저장")) Run("생성", () =>
                    {
                        UnitAuthoringAssets.ValidateData(templateUnit);
                        UnitAuthoringAssets.ValidateData(templateVisual);
                        AssetDatabase.SaveAssetIfDirty(templateUnit);
                        AssetDatabase.SaveAssetIfDirty(templateVisual);
                    });
                }
                DrawShared();
                EditorGUILayout.Space();
                DrawKeyFields();
                displayName = EditorGUILayout.TextField(new GUIContent("표시 이름", "비우면 키 이름에서 Unit_를 뺀 값을 씁니다."), displayName);
                EditorGUILayout.LabelField("설명 (비우면 원본 그대로)");
                description = EditorGUILayout.TextArea(description ?? "", GUILayout.MinHeight(40));
                DrawEligibility();
                EditorGUILayout.Space();
                unitFolder = (DefaultAsset)EditorGUILayout.ObjectField("Unit 저장 폴더", unitFolder, typeof(DefaultAsset), false);
                visualFolder = (DefaultAsset)EditorGUILayout.ObjectField("Visual 저장 폴더", visualFolder, typeof(DefaultAsset), false);
                Object nextUnitTable = AssetPopup("Unit 등록 테이블", unitTable, _catalog.UnitTables);
                Object nextVisualTable = AssetPopup("Visual 등록 테이블", visualTable, _catalog.VisualTables);
                if (nextUnitTable != unitTable || nextVisualTable != visualTable)
                {
                    unitTable = nextUnitTable;
                    visualTable = nextVisualTable;
                    Run("생성", () => _catalogMessage = UnitAuthoringAssets.Catalogs(unitTable, visualTable));
                }
                EditorGUILayout.HelpBox(_catalogMessage ?? "테이블을 선택해 주세요.", MessageType.Info);
                DrawRosterChoice();
                string name = (keyName ?? "").Trim();
                EditorGUILayout.LabelField("생성할 파일", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(unitFolder) + "/" + name + ".asset", GUILayout.Height(18));
                EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(visualFolder) + "/" + VisualName() + ".asset", GUILayout.Height(18));
                if (addKey) EditorGUILayout.HelpBox("생성 시 EntityKey.cs에 항목을 추가하고 Unity 재컴파일을 기다립니다. 기존 항목의 번호는 유지합니다.", MessageType.Info);
                string problem = CreationProblem();
                if (problem != null) EditorGUILayout.HelpBox(problem, MessageType.Warning);
                using (new EditorGUI.DisabledScope(problem != null))
                    if (GUILayout.Button("생성 · 등록", GUILayout.Height(32))) Run("생성", Create);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("출현은 스테이지 배치·EnemyLoadout·증원 설정에서 따로 연결합니다. EnemyLoadout·증원에 쓰는 적은 그 스테이지의 UnitRoster에 있어야 전투에 들어갈 수 있습니다.", EditorStyles.wordWrappedLabel);
        }

        private void DrawSourcePicker()
        {
            var usable = _catalog.Rows.Where(row => row.SourceUsable).ToList();
            var unusable = _catalog.Rows.Where(row => !row.SourceUsable).ToList();
            string[] labels = new[] { "선택…" }.Concat(usable.Select(row => row.Label)).ToArray();
            int index = usable.FindIndex(row => row.Unit == templateUnit);
            int chosen = EditorGUILayout.Popup("원본 Unit", index + 1, labels) - 1;
            if (chosen != index) Run("생성", () => SelectTemplate(chosen >= 0 ? usable[chosen].Unit : null));
            if (unusable.Count == 0) return;
            _unusableFold = EditorGUILayout.Foldout(_unusableFold, $"원본으로 쓸 수 없는 유닛 {unusable.Count}개", true);
            if (!_unusableFold) return;
            foreach (UnitAuthoringRow row in unusable)
                EditorGUILayout.HelpBox(row.Label + "\n" + string.Join("\n", row.Problems), MessageType.None);
        }

        private void DrawShared()
        {
            _sharedFold = EditorGUILayout.Foldout(_sharedFold, "원본과 함께 쓰게 되는 참조 (생성 뒤 바꿀 후보)", true);
            if (!_sharedFold) return;
            using (new EditorGUI.DisabledScope(true))
            using (new EditorGUI.IndentLevelScope())
            {
                using (var visual = new SerializedObject(templateVisual))
                    foreach (string field in new[] { "prefab", "attackPresentation", "reactionPresentation", "animationPresentation" })
                    {
                        SerializedProperty property = visual.FindProperty(UnitAuthoringAssets.VisualRoot + "." + field);
                        if (property != null) EditorGUILayout.ObjectField(property.displayName, property.objectReferenceValue, typeof(Object), false);
                    }
                using var unit = new SerializedObject(templateUnit);
                SerializedProperty method = unit.FindProperty("targetingMethod");
                if (method != null) EditorGUILayout.ObjectField(method.displayName, method.objectReferenceValue, typeof(Object), false);
                SerializedProperty traits = unit.FindProperty("traits");
                if (traits != null && traits.isArray) EditorGUILayout.LabelField("특성", traits.arraySize + "개");
            }
        }

        private void DrawKeyFields()
        {
            bool nextMode = EditorGUILayout.Toggle("새 enum 항목 추가", addKey);
            if (nextMode != addKey)
            {
                addKey = nextMode;
                var unused = _catalog.UnusedKeys();
                if (addKey)
                {
                    // 기존 키 모드에서 고른 이름이 남아 충돌하지 않게 다시 채웁니다(C9).
                    keyName = "Unit_";
                    keyValue = _catalog.SuggestKey();
                }
                else if (unused.Length > 0)
                {
                    keyName = unused[0].Key;
                    keyValue = unused[0].Value;
                }
            }
            if (addKey)
            {
                keyName = EditorGUILayout.TextField("새 키 이름", keyName);
                using (new EditorGUILayout.HorizontalScope())
                {
                    keyValue = EditorGUILayout.IntField("새 키 번호", keyValue);
                    if (GUILayout.Button("빈 번호 제안", GUILayout.Width(100))) keyValue = _catalog.SuggestKey();
                }
                return;
            }
            var keys = _catalog.UnusedKeys();
            if (keys.Length == 0)
            {
                EditorGUILayout.HelpBox("SO에서 사용하지 않은 Unit_ 키가 없습니다. 새 enum 항목을 추가해 주세요.", MessageType.Warning);
                return;
            }
            int index = Array.FindIndex(keys, pair => pair.Key == keyName && pair.Value == keyValue);
            string[] labels = new[] { "선택…" }.Concat(keys.Select(pair => $"{pair.Key} = {pair.Value}")).ToArray();
            int chosen = EditorGUILayout.Popup("기존 미사용 키", index + 1, labels) - 1;
            if (chosen < 0) return;
            keyName = keys[chosen].Key;
            keyValue = keys[chosen].Value;
        }

        private void DrawEligibility()
        {
            if (_eligibilityNames.Length == 0) return;
            string[] labels = new[] { "원본과 같게" }.Concat(_eligibilityNames).ToArray();
            eligibilityIndex = EditorGUILayout.Popup("진영 자격", eligibilityIndex + 1, labels) - 1;
        }

        private void DrawRosterChoice()
        {
            EditorGUILayout.LabelField("등록할 UnitRoster", EditorStyles.boldLabel);
            if (_catalog.Rosters.Count == 0)
            {
                EditorGUILayout.HelpBox("UnitRoster SO가 없습니다.", MessageType.Info);
                return;
            }
            UnitAuthoringRow source = _catalog.Find(templateUnit);
            if (source != null && source.Eligibility == "PlayerOnly")
                EditorGUILayout.HelpBox("아군은 로스터 없이도 전투에 나옵니다. EnemyLoadout·증원에 쓸 때만 넣으면 됩니다.", MessageType.Info);
            foreach (Object roster in _catalog.Rosters)
            {
                _catalog.RosterStageCounts.TryGetValue(roster, out int stages);
                bool on = rosterSelection.Contains(roster);
                bool next = EditorGUILayout.ToggleLeft($"{roster.name} (키 {UnitAuthoringRosters.RosterKey(roster)}, 스테이지 {stages}곳)", on);
                if (next == on) continue;
                if (next) rosterSelection.Add(roster);
                else rosterSelection.Remove(roster);
            }
        }

        /// <summary>생성을 막는 이유입니다. 입력하는 동안 바로 보입니다(C8). 없으면 null.</summary>
        private string CreationProblem()
        {
            if (templateUnit == null || templateVisual == null) return "원본 Unit을 선택해 주세요.";
            if (unitTable == null || visualTable == null) return "등록할 두 테이블을 선택해 주세요.";
            if (EditorUtility.IsDirty(templateUnit) || EditorUtility.IsDirty(templateVisual)) return "원본을 먼저 저장해 주세요.";
            if (_catalog.KeySource == null) return _catalog.KeySourceError;
            string name = (keyName ?? "").Trim();
            try { _catalog.KeySource.Validate(name, keyValue, addKey); }
            catch (Exception exception) { return exception.Message; }
            if (_catalog.UsedKeys.Contains(keyValue)) return $"Key {keyValue}를 이미 쓰는 SO가 있습니다.";
            try
            {
                UnitAuthoringAssets.RequireVacant(UnitAuthoringAssets.OutputPath(unitFolder, name));
                UnitAuthoringAssets.RequireVacant(UnitAuthoringAssets.OutputPath(visualFolder, VisualName()));
            }
            catch (Exception exception) { return exception.Message; }
            return null;
        }

        private void Create()
        {
            string problem = CreationProblem();
            if (problem != null) throw new InvalidOperationException(problem);
            string name = keyName.Trim();
            UnitAuthoringOperation.Start(new UnitAuthoringRequest
            {
                unitSource = UnitAuthoringAssets.GuidOf(templateUnit),
                visualSource = UnitAuthoringAssets.GuidOf(templateVisual),
                unitFingerprint = UnitAuthoringAssets.Fingerprint(templateUnit),
                visualFingerprint = UnitAuthoringAssets.Fingerprint(templateVisual),
                unitTable = UnitAuthoringAssets.GuidOf(unitTable),
                visualTable = UnitAuthoringAssets.GuidOf(visualTable),
                keyName = name, keyValue = keyValue, addKey = addKey,
                displayName = string.IsNullOrWhiteSpace(displayName) ? name.Substring("Unit_".Length) : displayName.Trim(),
                description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                eligibilityIndex = eligibilityIndex,
                rosters = rosterSelection.Where(roster => roster != null).Select(UnitAuthoringAssets.GuidOf).ToArray(),
                unitPath = UnitAuthoringAssets.OutputPath(unitFolder, name),
                visualPath = UnitAuthoringAssets.OutputPath(visualFolder, VisualName())
            });
        }

        #endregion

        #region 진행 중 요청

        private void DrawOperation()
        {
            if (!string.IsNullOrEmpty(UnitAuthoringOperation.StorageError))
            {
                EditorGUILayout.HelpBox(UnitAuthoringOperation.StorageError, MessageType.Error);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("기록 파일 열기")) EditorUtility.RevealInFinder(Path.GetFullPath(UnitAuthoringPendingStore.FilePath));
                    if (GUILayout.Button("기록 버리기") && EditorUtility.DisplayDialog("기록 버리기",
                            "작업 기록 파일만 지웁니다. 이미 만든 SO·테이블 등록·enum 백업은 그대로 남으니 직접 확인해 주세요.", "버리기", "취소"))
                        Run("생성", UnitAuthoringOperation.DiscardRecord);
                }
            }
            var request = UnitAuthoringOperation.Current;
            if (request == null) return;
            EditorGUILayout.LabelField($"생성 요청: {request.keyName} ({request.keyValue}) — {request.phase}", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(request.error))
                EditorGUILayout.HelpBox(request.error, request.phase == UnitAuthoringOperation.Failed ? MessageType.Error : MessageType.Info);
            if (!string.IsNullOrEmpty(request.sourceBackup))
                EditorGUILayout.LabelField("enum 원본 백업", request.sourceBackup, EditorStyles.wordWrappedLabel);
            if (UnitAuthoringOperation.HasPending)
            {
                if (UnitAuthoringOperation.IsStalled)
                    EditorGUILayout.HelpBox("Unity를 다시 켜서 자동 진행이 멈췄습니다. [재검사 · 재개]를 눌러 이어 가세요.", MessageType.Warning);
                else if (!UnitAuthoringOperation.IsAutomatic)
                    EditorGUILayout.HelpBox(request.cancelRequested
                        ? "중단된 취소 요청입니다. 재개하면 남은 생성물 삭제를 이어갑니다. 추가된 enum은 유지됩니다."
                        : "재개는 기존 SO와 GUID를 보존하며 남은 단계를 이어갑니다. 취소는 이번 요청의 생성물과 테이블·로스터 참조를 삭제합니다. 추가된 enum은 유지됩니다.", MessageType.Info);
                using (new EditorGUI.DisabledScope(Busy()))
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (request.sourceChanged)
                    {
                        if (GUILayout.Button("바뀐 원본으로 계속")) Run("생성", UnitAuthoringOperation.AcceptSourceChange);
                    }
                    else if (GUILayout.Button("재검사 · 재개")) Run("생성", UnitAuthoringOperation.Retry);
                    if (GUILayout.Button("요청 취소 · 생성물 삭제")) Run("생성", UnitAuthoringOperation.Cancel);
                }
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (request.phase == UnitAuthoringOperation.Complete)
                {
                    if (GUILayout.Button("생성한 유닛 편집")) Run("생성", () =>
                    {
                        if (!ConfirmLeaveEditing()) return;
                        creating = false;
                        SetEditing(UnitAuthoringAssets.Load(request.createdUnit, UnitAuthoringAssets.UnitType));
                    });
                    if (GUILayout.Button("Unit 테이블 선택")) Run("생성", () => Selection.activeObject = UnitAuthoringAssets.Load(request.unitTable, UnitAuthoringAssets.UnitTableType));
                    if (GUILayout.Button("Visual 테이블 선택")) Run("생성", () => Selection.activeObject = UnitAuthoringAssets.Load(request.visualTable, UnitAuthoringAssets.VisualTableType));
                }
                if (GUILayout.Button("닫기", GUILayout.Width(60))) Run("생성", UnitAuthoringOperation.DiscardRecord);
            }
        }

        #endregion

        private string VisualName()
        {
            string name = (keyName ?? "").Trim();
            return "UnitVisual_" + (name.StartsWith("Unit_", StringComparison.Ordinal) ? name.Substring(5) : name);
        }

        private static DefaultAsset FolderOf(Object asset) => AssetDatabase.LoadAssetAtPath<DefaultAsset>(
            Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset)).Replace('\\', '/'));

        private static Object AssetPopup(string label, Object selected, List<Object> assets)
        {
            string[] names = new[] { "선택…" }.Concat(assets.Select(asset => asset.name)).ToArray();
            int index = EditorGUILayout.Popup(label, assets.IndexOf(selected) + 1, names) - 1;
            return index >= 0 ? assets[index] : null;
        }

        private static bool Busy() => EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>동작을 실행하고 실패는 출처 이름으로 배너에 남깁니다. 성공하면 같은 출처의 배너만 지웁니다(E15).</summary>
        private void Run(string source, Action action)
        {
            try
            {
                action();
                _errors.Remove(source);
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { _errors[source] = exception.Message; }
        }
    }
}
