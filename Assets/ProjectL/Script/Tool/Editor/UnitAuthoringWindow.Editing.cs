using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    internal sealed partial class UnitAuthoringWindow
    {
        private static readonly string[] BasicFields = { "prefab", "displayName", "description", "timelineIcon", "infoIcon" };
        private static readonly string[] PresentationFields = { "attackPresentation", "reactionPresentation", "animationPresentation" };
        private static readonly string[] SoundFields = { "unitSoundPreset", "attackSoundPreset" };
        private static readonly string[] CueFields = { "structureHitCues" };
        /// <summary>장애물 외형에서만 쓰는 칸입니다. 유닛 행에는 장애물이 없어 값이 있을 때만 보입니다.</summary>
        private static readonly string[] ObstacleCueFields = { "obstacleHitCues", "obstacleDestroyedCues" };

        [SerializeField] private Object editingUnit;
        [SerializeField] private Object editingVisual;
        [SerializeField] private List<Object> touchedRosters = new();
        [SerializeField] private Object pairSource;
        [SerializeField] private bool deleteKey;
        private SerializedObject _unitEditor;
        private SerializedObject _visualEditor;
        private string _editMessage;
        private List<KeyValuePair<Object, string>> _usages = new();
        private bool _cueFold;
        private bool _otherFold = true;

        private void SetEditing(Object unit)
        {
            // 새 상태를 먼저 만들고, 성공하면 한꺼번에 바꿉니다(오류 E-R4).
            Object visual = null;
            List<KeyValuePair<Object, string>> usages = new();
            if (unit != null)
            {
                UnitAuthoringRow row = _catalog?.Find(unit) ?? throw new InvalidOperationException("목록에 없는 유닛입니다. 새로고침해 주세요.");
                if (row.DuplicateUnitKey) throw new InvalidOperationException($"{unit.name}: 같은 키의 Unit SO가 중복입니다. 잘못된 짝을 고치지 않도록 편집을 막습니다.");
                visual = row.Visual;
                usages = UnitAuthoringRosters.Usages(unit);
            }
            DisposeEditing();
            editingUnit = unit;
            editingVisual = visual;
            touchedRosters.Clear();
            _usages = usages;
            _editMessage = null;
            if (unit != null) _unitEditor = new SerializedObject(unit);
            if (visual != null) _visualEditor = new SerializedObject(visual);
        }

        private void RestoreEditing()
        {
            if (editingUnit == null) return;
            var touched = new List<Object>(touchedRosters);
            Run("편집", () => SetEditing(editingUnit));
            touchedRosters.AddRange(touched.Where(roster => roster != null));
        }

        private void DisposeEditing()
        {
            _unitEditor?.Dispose();
            _visualEditor?.Dispose();
            _unitEditor = null;
            _visualEditor = null;
        }

        private void OnUndo()
        {
            _unitEditor?.Update();
            _visualEditor?.Update();
            _editMessage = "Undo/Redo를 반영했습니다. 검증 후 저장할 수 있습니다.";
            Repaint();
        }

        private bool IsEditingDirty() =>
            (editingUnit != null && EditorUtility.IsDirty(editingUnit)) ||
            (editingVisual != null && EditorUtility.IsDirty(editingVisual)) ||
            touchedRosters.Any(roster => roster != null && EditorUtility.IsDirty(roster));

        /// <summary>저장하지 않은 변경이 있으면 저장·버리기·취소를 묻습니다(B6). 계속해도 되면 참입니다.</summary>
        private bool ConfirmLeaveEditing()
        {
            if (!IsEditingDirty()) return true;
            int choice = EditorUtility.DisplayDialogComplex("저장하지 않은 변경",
                $"{editingUnit.name}에 저장하지 않은 변경이 있습니다.", "저장", "취소", "버리기");
            if (choice == 1) return false;
            if (choice == 0) SaveEditing();
            else RevertEditing();
            return true;
        }

        private void DrawEditing()
        {
            if (editingUnit == null || _unitEditor == null)
            {
                EditorGUILayout.HelpBox("왼쪽 목록에서 유닛을 고르거나 [+ 새 유닛]을 눌러 주세요.", MessageType.Info);
                return;
            }
            UnitAuthoringRow row = _catalog.Find(editingUnit);
            // 고정 머리줄 — 저장 버튼이 스크롤에 밀려 사라지지 않습니다(B4).
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label((row?.Label ?? editingUnit.name) + (IsEditingDirty() ? "  *" : ""), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUI.DisabledScope(UnitAuthoringOperation.HasPending || Busy()))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("검증 · 저장")) Run("저장", SaveEditing);
                using (new EditorGUI.DisabledScope(!IsEditingDirty()))
                    if (GUILayout.Button("되돌리기") && EditorUtility.DisplayDialog("되돌리기", "저장하지 않은 변경을 디스크 판으로 되돌립니다.", "되돌리기", "취소"))
                        Run("저장", RevertEditing);
                if (GUILayout.Button("Unit 선택")) Selection.activeObject = editingUnit;
                using (new EditorGUI.DisabledScope(editingVisual == null))
                    if (GUILayout.Button("Visual 선택")) Selection.activeObject = editingVisual;
            }
            if (row != null && row.Problems.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", row.Problems), MessageType.Warning);
            if (!string.IsNullOrEmpty(_editMessage)) EditorGUILayout.HelpBox(_editMessage, MessageType.Info);
            using var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll);
            _detailScroll = scroll.scrollPosition;
            using (new EditorGUI.DisabledScope(UnitAuthoringOperation.HasPending || Busy()))
            {
                if (row != null)
                {
                    EditorGUILayout.LabelField("Key (변경 불가)", $"{row.KeyName} = {row.Key}");
                    EditorGUILayout.LabelField("Unit 테이블", Names(row.UnitTables));
                    EditorGUILayout.LabelField("Visual 테이블", editingVisual == null ? "-" : Names(row.VisualTables));
                }
                EditorGUILayout.Space();
                GUILayout.Label("규칙 — " + editingUnit.name, EditorStyles.boldLabel);
                DrawUnitProperties();
                EditorGUILayout.Space();
                if (_visualEditor == null) DrawMissingPair();
                else
                {
                    GUILayout.Label("외형 — " + editingVisual.name, EditorStyles.boldLabel);
                    DrawVisualProperties();
                }
                EditorGUILayout.Space();
                DrawRosterMembership();
                EditorGUILayout.Space();
                DrawUsages();
                EditorGUILayout.Space();
                DrawDelete();
            }
        }

        private void DrawDelete()
        {
            GUILayout.Label("유닛 삭제", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("쓰는 곳(로드아웃·스테이지·로스터)과 테이블 등록을 함께 빼고, Unit·Visual SO를 휴지통으로 옮깁니다. 실행 전에 바뀌는 곳을 보여 줍니다.", EditorStyles.wordWrappedMiniLabel);
            deleteKey = EditorGUILayout.ToggleLeft("EntityKey.cs의 enum 항목도 지우기 (재컴파일)", deleteKey);
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);
            if (GUILayout.Button("유닛 삭제…")) Run("삭제", DeleteEditing);
            GUI.backgroundColor = previous;
        }

        private void DeleteEditing()
        {
            if (!ConfirmLeaveEditing()) return;
            UnitAuthoringRow row = _catalog.Find(editingUnit) ?? throw new InvalidOperationException("목록에 없는 유닛입니다. 새로고침해 주세요.");
            UnitAuthoringDeletion.Plan plan = UnitAuthoringDeletion.Prepare(row, _catalog);
            bool removeKey = deleteKey;
            if (removeKey && plan.CodeUses.Count > 0)
            {
                if (!EditorUtility.DisplayDialog("enum을 지울 수 없음",
                        "이 키를 쓰는 곳이 있어 EntityKey 항목은 지울 수 없습니다:\n" + string.Join("\n", plan.CodeUses.Take(10)),
                        "enum은 남기고 계속", "취소"))
                    return;
                removeKey = false;
            }
            if (!EditorUtility.DisplayDialog("유닛 삭제", plan.Describe(removeKey) + "\n\n되돌리려면 휴지통·버전 관리에서 복구해야 합니다.", "삭제", "취소"))
                return;
            string name = plan.Unit.name;
            // 지울 SO를 잡고 있는 편집 상태를 먼저 놓습니다.
            SetEditing(null);
            try { UnitAuthoringDeletion.Execute(plan, removeKey); }
            finally { RefreshCatalog(); }
            _errors.Remove("삭제");
            Debug.Log($"[유닛 통합 에디터] {name}을(를) 삭제했습니다. 뺀 참조 {plan.References.Count}칸" + (removeKey ? ", enum 항목 제거" : ""));
            // 모달 창과 에셋 삭제 뒤 이번 프레임의 레이아웃을 끝냅니다.
            GUIUtility.ExitGUI();
        }

        private static string Names(List<Object> assets) => assets.Count == 0 ? "없음" : string.Join(", ", assets.Select(asset => asset.name));

        private void DrawUnitProperties()
        {
            _unitEditor.Update();
            SerializedProperty iterator = _unitEditor.GetIterator();
            bool next = iterator.NextVisible(true);
            while (next)
            {
                if (iterator.name != "m_Script" && iterator.name != UnitAuthoringAssets.UnitKey) EditorGUILayout.PropertyField(iterator, true);
                next = iterator.NextVisible(false);
            }
            if (_unitEditor.ApplyModifiedProperties()) _editMessage = "변경을 반영했습니다. [검증 · 저장]으로 저장해 주세요.";
        }

        private void DrawVisualProperties()
        {
            _visualEditor.Update();
            SerializedProperty root = UnitAuthoringAssets.Require(_visualEditor, UnitAuthoringAssets.VisualRoot);
            DrawGroup("기본", root, BasicFields);
            DrawGroup("연출", root, PresentationFields);
            DrawGroup("소리", root, SoundFields);
            _cueFold = EditorGUILayout.Foldout(_cueFold, "큐", true);
            if (_cueFold)
                using (new EditorGUI.IndentLevelScope())
                    foreach (string field in CueFields) DrawField(root, field);
            foreach (string field in ObstacleCueFields)
            {
                SerializedProperty property = root.FindPropertyRelative(field);
                if (property == null || !HasValues(property)) continue;
                EditorGUILayout.HelpBox($"{property.displayName}는 장애물 외형에서만 씁니다. 값이 있으면 저장 검증이 실패합니다.", MessageType.Warning);
                EditorGUILayout.PropertyField(property, true);
            }
            DrawOthers(root);
            if (_visualEditor.ApplyModifiedProperties()) _editMessage = "변경을 반영했습니다. [검증 · 저장]으로 저장해 주세요.";
        }

        private static void DrawGroup(string title, SerializedProperty root, string[] fields)
        {
            GUILayout.Label(title, EditorStyles.miniBoldLabel);
            using (new EditorGUI.IndentLevelScope())
                foreach (string field in fields) DrawField(root, field);
        }

        private static void DrawField(SerializedProperty root, string field)
        {
            SerializedProperty property = root.FindPropertyRelative(field);
            if (property != null) EditorGUILayout.PropertyField(property, true);
        }

        /// <summary>묶음에 없는 새 칸도 잃지 않게 '기타'로 그립니다.</summary>
        private void DrawOthers(SerializedProperty root)
        {
            var known = new HashSet<string>(BasicFields.Concat(PresentationFields).Concat(SoundFields).Concat(CueFields).Concat(ObstacleCueFields)) { "key" };
            var others = new List<SerializedProperty>();
            SerializedProperty iterator = root.Copy();
            SerializedProperty end = root.GetEndProperty();
            bool next = iterator.NextVisible(true);
            while (next && !SerializedProperty.EqualContents(iterator, end))
            {
                if (iterator.depth == root.depth + 1 && !known.Contains(iterator.name)) others.Add(iterator.Copy());
                next = iterator.NextVisible(false);
            }
            if (others.Count == 0) return;
            _otherFold = EditorGUILayout.Foldout(_otherFold, "기타", true);
            if (!_otherFold) return;
            using (new EditorGUI.IndentLevelScope())
                foreach (SerializedProperty property in others) EditorGUILayout.PropertyField(property, true);
        }

        private static bool HasValues(SerializedProperty property)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            while (iterator.Next(true) && !SerializedProperty.EqualContents(iterator, end))
                if (iterator.isArray && iterator.propertyType != SerializedPropertyType.String && iterator.arraySize > 0) return true;
            return false;
        }

        private void DrawMissingPair()
        {
            GUILayout.Label("외형 — 짝 Visual 없음", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("이 유닛에는 짝 Visual이 없습니다. 다른 Visual을 복제해 이 키로 만들 수 있습니다(같은 폴더, Visual 테이블에 등록).", MessageType.Info);
            pairSource = AssetPopup("복제할 Visual", pairSource, _catalog.Visuals);
            using (new EditorGUI.DisabledScope(pairSource == null))
                if (GUILayout.Button("짝 Visual 만들기")) Run("편집", () =>
                {
                    Object table = UnitAuthoringAssets.DefaultTable(UnitAuthoringAssets.VisualTableType, pairSource)
                        ?? throw new InvalidOperationException("복제할 Visual이 든 Visual 테이블을 하나로 정할 수 없습니다.");
                    Object created = UnitAuthoringAssets.CreatePair(pairSource, editingUnit, table);
                    RefreshCatalog();
                    SetEditing(editingUnit);
                    _editMessage = $"{created.name}을 만들고 {table.name}에 등록했습니다.";
                });
        }

        private void DrawRosterMembership()
        {
            GUILayout.Label("UnitRoster", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("넣기·빼기는 바로 반영되고(Undo 가능) [검증 · 저장]으로 함께 저장합니다. EnemyLoadout·증원에 쓰는 적은 그 스테이지의 로스터에 있어야 합니다.", EditorStyles.wordWrappedMiniLabel);
            foreach (Object roster in _catalog.Rosters)
            {
                bool has = UnitAuthoringRosters.Contains(roster, editingUnit);
                _catalog.RosterStageCounts.TryGetValue(roster, out int stages);
                bool next = EditorGUILayout.ToggleLeft($"{roster.name} (스테이지 {stages}곳)" + (EditorUtility.IsDirty(roster) ? " *" : ""), has);
                if (next != has) Run("로스터", () => ToggleRoster(roster, next));
            }
        }

        private void ToggleRoster(Object roster, bool add)
        {
            if (!add)
            {
                List<string> uses = UnitAuthoringRosters.LoadoutUses(roster, editingUnit);
                if (uses.Count > 0 && !EditorUtility.DisplayDialog("로스터에서 빼기",
                        "이 로스터를 쓰는 스테이지에서 이 유닛을 씁니다:\n" + string.Join("\n", uses) +
                        "\n\n빼면 그 스테이지는 전투 진입에서 멈춥니다. 그래도 뺄까요?", "빼기", "취소"))
                    return;
                UnitAuthoringRosters.Remove(roster, editingUnit, true);
            }
            else UnitAuthoringRosters.Add(roster, editingUnit, true);
            if (!touchedRosters.Contains(roster)) touchedRosters.Add(roster);
            _editMessage = "로스터를 바꿨습니다. [검증 · 저장]으로 저장해 주세요.";
            var touched = new List<Object>(touchedRosters);
            RefreshCatalog();
            touchedRosters.Clear();
            touchedRosters.AddRange(touched);
        }

        private void DrawUsages()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("쓰는 곳 (읽기만)", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("다시 찾기", GUILayout.Width(80))) Run("편집", () => _usages = UnitAuthoringRosters.Usages(editingUnit));
            }
            if (_usages.Count == 0)
            {
                EditorGUILayout.LabelField("이 유닛을 참조하는 로드아웃·스테이지·로스터가 없습니다.", EditorStyles.miniLabel);
                return;
            }
            foreach (var usage in _usages)
            {
                if (usage.Key == null) continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(usage.Key.name, usage.Value);
                    if (GUILayout.Button("찾기", GUILayout.Width(50))) EditorGUIUtility.PingObject(usage.Key);
                }
            }
        }

        private void SaveEditing()
        {
            UnitAuthoringAssets.ValidateData(editingUnit);
            if (editingVisual != null) UnitAuthoringAssets.ValidateData(editingVisual);
            var rosters = touchedRosters.Where(roster => roster != null).ToList();
            foreach (Object roster in rosters) UnitAuthoringAssets.ValidateData(roster);
            AssetDatabase.SaveAssetIfDirty(editingUnit);
            if (editingVisual != null) AssetDatabase.SaveAssetIfDirty(editingVisual);
            foreach (Object roster in rosters) AssetDatabase.SaveAssetIfDirty(roster);
            touchedRosters.Clear();
            RefreshCatalog();
            _editMessage = "규칙·외형·로스터의 검증과 저장을 완료했습니다.";
        }

        /// <summary>저장하지 않은 변경을 디스크 판으로 되돌립니다(B6). Undo로 다시 살릴 수 있습니다.</summary>
        private void RevertEditing()
        {
            foreach (Object asset in new[] { editingUnit, editingVisual }.Concat(touchedRosters))
                if (asset != null && EditorUtility.IsDirty(asset)) RevertFromDisk(asset);
            touchedRosters.Clear();
            _unitEditor?.Update();
            _visualEditor?.Update();
            RefreshCatalog();
            _editMessage = "저장하지 않은 변경을 되돌렸습니다.";
        }

        private static void RevertFromDisk(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            Object[] loaded = InternalEditorUtility.LoadSerializedFileAndForget(path);
            try
            {
                Object disk = loaded.FirstOrDefault(candidate => candidate != null && candidate.GetType() == asset.GetType())
                    ?? throw new InvalidOperationException($"디스크 판을 읽지 못했습니다: {path}");
                Undo.RecordObject(asset, "유닛 변경 되돌리기");
                EditorUtility.CopySerialized(disk, asset);
                EditorUtility.ClearDirty(asset);
            }
            finally
            {
                foreach (Object candidate in loaded)
                    if (candidate != null) DestroyImmediate(candidate);
            }
        }
    }
}
