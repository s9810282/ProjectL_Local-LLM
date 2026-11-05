using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>프리팹의 배경 구역을 미리보고 편집하는 에디터 창입니다.</summary>
    public sealed class MapRevealPartitionWindow : EditorWindow
    {
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private float areaSize = 16;
        [SerializeField] private string containerNames = "Map, Props, rock";
        private MapRevealPartitionPlan _preview;
        private Vector2 _scroll;
        private string _notice;
        private MessageType _noticeType;

        [MenuItem("ProjectL/맵 배경 구역 분할")]
        private static void OpenWindow()
        {
            MapRevealPartitionWindow window = GetWindow<MapRevealPartitionWindow>();
            GameObject selected = Selection.activeGameObject;
            if (selected != null && EditorUtility.IsPersistent(selected))
                window.mapPrefab = selected.transform.root.gameObject;
            else window.UseCurrentPrefab();
            window.InvalidatePreview();
            window.Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("배경 구역 분할");
            minSize = new Vector2(460, 440);
            EditorApplication.hierarchyChanged += InvalidatePreview;
            Undo.undoRedoPerformed += InvalidatePreview;
            Undo.postprocessModifications += OnModifications;
            SceneView.duringSceneGui += DrawScenePreview;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= InvalidatePreview;
            Undo.undoRedoPerformed -= InvalidatePreview;
            Undo.postprocessModifications -= OnModifications;
            SceneView.duringSceneGui -= DrawScenePreview;
            _preview = null;
            SceneView.RepaintAll();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("맵 배경을 X·Z 위치별로 묶습니다. 중첩 프리팹과 소품 내부는 통째로 유지합니다.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            mapPrefab = (GameObject)EditorGUILayout.ObjectField("맵 프리팹", mapPrefab, typeof(GameObject), false);
            areaSize = EditorGUILayout.FloatField(new GUIContent("구역 크기", "배경 루트 기준 X·Z 길이입니다. 기본값은 16입니다."), areaSize);
            containerNames = EditorGUILayout.TextField(new GUIContent("풀어낼 빈 부모 이름", "쉼표로 구분합니다. 이 이름의 Transform 전용 부모 안에 있는 소품을 개별 분류합니다."), containerNames);
            if (EditorGUI.EndChangeCheck()) InvalidatePreview();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(mapPrefab == null || Busy))
                    if (GUILayout.Button("프리팹 열기")) OpenPrefab();
                using (new EditorGUI.DisabledScope(PrefabStageUtility.GetCurrentPrefabStage() == null || Busy))
                    if (GUILayout.Button("현재 프리팹 사용")) UseCurrentPrefab();
            }

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            bool matches = stage != null && mapPrefab != null && stage.assetPath == AssetDatabase.GetAssetPath(mapPrefab);
            if (_preview != null && (!matches || _preview.Root == null)) InvalidatePreview();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("프리팹을 연 뒤 미리보기 → 분할 적용 순서로 사용하세요. 저장은 Prefab Mode의 Auto Save 또는 Ctrl+S를 따릅니다. Ctrl+Z 한 번으로 분할 전체를 되돌립니다.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(!matches || Busy))
                    if (GUILayout.Button("구역 미리보기")) Preview();
                using (new EditorGUI.DisabledScope(_preview == null || _preview.ItemCount == 0 || !matches || Busy))
                    if (GUILayout.Button("분할 적용")) Apply();
            }

            if (!string.IsNullOrEmpty(_notice)) EditorGUILayout.HelpBox(_notice, _noticeType);
            if (_preview == null) return;
            EditorGUILayout.LabelField($"소품 {_preview.ItemCount}개 → 구역 {_preview.Areas.Count}개", EditorStyles.boldLabel);
            if (_preview.Excluded.Count > 0)
                EditorGUILayout.HelpBox($"카메라/조명을 포함한 {_preview.Excluded.Count}개 묶음은 기존 위치에 남깁니다.", MessageType.Info);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (MapRevealPartitionPlan.Area area in _preview.Areas)
                EditorGUILayout.LabelField(area.Name, $"{area.Items.Count}개");
            foreach (Transform excluded in _preview.Excluded)
                EditorGUILayout.LabelField("제외", excluded.name);
            EditorGUILayout.EndScrollView();
        }

        private static bool Busy => EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling;

        private void OpenPrefab()
        {
            try
            {
                MapRevealPartition.Open(mapPrefab);
                InvalidatePreview();
                _notice = "프리팹을 열었습니다. 구역 미리보기를 눌러 배치를 확인하세요.";
                _noticeType = MessageType.Info;
            }
            catch (ExitGUIException) { throw; }
            catch (Exception exception) { ShowError(exception); }
        }

        private void UseCurrentPrefab()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;
            mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
            InvalidatePreview();
        }

        private void Preview()
        {
            try
            {
                _preview = MapRevealPartitionPlan.Create(MapRevealPartition.CurrentRoot(), areaSize, containerNames);
                _notice = "Scene 뷰의 테두리는 소품 피벗을 분류한 구역입니다. 메시 자체는 절단하지 않습니다.";
                _noticeType = MessageType.Info;
                SceneView.RepaintAll();
            }
            catch (Exception exception) { ShowError(exception); }
        }

        private void Apply()
        {
            try
            {
                MapRevealPartitionPlan plan = _preview;
                MapRevealPartition.Apply(plan);
                InvalidatePreview();
                _notice = $"소품 {plan.ItemCount}개를 {plan.Areas.Count}개 구역으로 정리했습니다. Ctrl+Z로 되돌릴 수 있습니다.";
                _noticeType = MessageType.Info;
            }
            catch (Exception exception) { ShowError(exception); }
        }

        private void ShowError(Exception exception)
        {
            InvalidatePreview();
            _notice = exception.Message;
            _noticeType = MessageType.Error;
            Debug.LogException(exception);
        }

        private void InvalidatePreview()
        {
            _preview = null;
            Repaint();
            SceneView.RepaintAll();
        }

        private UndoPropertyModification[] OnModifications(UndoPropertyModification[] modifications)
        {
            InvalidatePreview();
            return modifications;
        }

        private void DrawScenePreview(SceneView sceneView)
        {
            if (_preview == null || _preview.Root == null || Busy) return;
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            // 별도 가상 씬을 소유하는 커스텀 SceneView에는 겹쳐 그리지 않습니다.
            if (stage == null || stage.scene != _preview.Root.gameObject.scene
                || sceneView.GetType() != typeof(SceneView)) return;
            using (new Handles.DrawingScope(new Color(0.2f, 1f, 0.65f, 0.9f), _preview.Root.localToWorldMatrix))
            {
                foreach (MapRevealPartitionPlan.Area area in _preview.Areas)
                    Handles.DrawWireCube(area.Center, new Vector3(_preview.Size, 0, _preview.Size));
            }
        }
    }
}
