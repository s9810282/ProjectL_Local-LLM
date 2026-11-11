using System;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>운영 스테이지 선택·검사와 씬 프리뷰 교체를 맡습니다. 저장 UI는 별도 partial에서 구성합니다.</summary>
    [CustomEditor(typeof(CombatMapAuthoringRoot))]
    internal sealed partial class CombatMapAuthoringRootEditor : UnityEditor.Editor
    {
        private static Type _viewStageType;
        private string _problem;
        private ScriptableObject _contextStage;
        private GameObject _contextPrefab;
        private CombatStageSoView _contextView;
        private FbxCombatStageContext _context;
        private string _contextProblem;
        private bool _contextDirty = true;

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        public override void OnInspectorGUI()
        {
            var root = (CombatMapAuthoringRoot)target;
            if (!root.gameObject.scene.IsValid() || !root.gameObject.scene.isLoaded)
            {
                EditorGUILayout.HelpBox("열린 전투 배경 편집 씬의 루트를 선택하세요.", MessageType.Warning);
                return;
            }

            serializedObject.Update();
            SerializedProperty selected = serializedObject.FindProperty("viewStage");
            EditorGUI.BeginChangeCheck();
            Object next = EditorGUILayout.ObjectField("운영 ViewStage", selected.objectReferenceValue, ViewStageType(), false);
            if (EditorGUI.EndChangeCheck())
            {
                selected.objectReferenceValue = next;
                serializedObject.ApplyModifiedProperties();
                _problem = null;
                _contextDirty = true;
            }

            if (_contextDirty || _contextStage != root.ViewStage) RefreshContext(root.ViewStage);
            GameObject prefab = _contextPrefab;
            CombatStageSoView view = _contextView;
            FbxCombatStageContext context = _context;
            bool ready = context != null && context.IsReady && view != null;
            if (ready)
            {
                EditorGUILayout.LabelField("StageKey", context.StageKey.ToString());
                EditorGUILayout.LabelField("전투 Root", context.RootName);
                EditorGUILayout.LabelField("배경 방식", context.Mode);
                EditorGUILayout.LabelField("맵 프리팹", prefab.name);
                EditorGUILayout.LabelField("필드", $"{context.Field.Columns} × {context.Field.Rows}");
                EditorGUILayout.LabelField("칸 간격", $"{context.FieldView.CellWidth:0.###} × {context.FieldView.CellLength:0.###}");
            }
            else EditorGUILayout.HelpBox(_contextProblem, MessageType.Warning);

            bool current = root.PreviewContainer != null && root.PreviewMap != null
                && root.LoadedViewStage == root.ViewStage && ready
                && root.PreviewMap.transform.IsChildOf(root.PreviewContainer.transform)
                && PrefabUtility.GetCorrespondingObjectFromSource(root.PreviewMap) == prefab;
            if (root.PreviewContainer != null && root.PreviewMap != null)
            {
                EditorGUILayout.LabelField("씬 프리뷰", root.PreviewMap.name);
                if (!current)
                    EditorGUILayout.HelpBox("현재 씬 인스턴스는 선택한 ViewStage의 프리뷰가 아닙니다. 불러오기로 교체하세요.", MessageType.Warning);
                EditorGUILayout.HelpBox("프랍 자식 변경은 씬 override로 남습니다. 아래 '프랍 프리팹 저장'으로 원본에 적용하세요. StageBackdropPreview·MapTileRoot와 맵 루트 Transform은 표시 전용입니다.", MessageType.Info);
                EditorGUILayout.HelpBox("Unity의 Apply All은 표시용 루트 보정과 옛 카메라 비활성화도 저장합니다. 이 Inspector의 저장 버튼을 사용하세요.", MessageType.Warning);
            }
            else EditorGUILayout.LabelField("씬 프리뷰", "아직 불러오지 않음");

            if (current)
            {
                DrawGrid(root, context);
                DrawUnit(root, view, context);
                DrawSaving(root, prefab, view, context);
            }
            else CombatMapAuthoringUnitPreview.Hide(root);

            bool origin = root.transform.position == Vector3.zero && root.transform.rotation == Quaternion.identity
                && root.transform.localScale == Vector3.one;
            if (!origin)
                EditorGUILayout.HelpBox("편집 루트는 월드 원점·회전 0·배율 1이어야 런타임 필드 좌표와 일치합니다.", MessageType.Error);
            using (new EditorGUI.DisabledScope(!ready || !origin || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button(root.PreviewContainer == null ? "스테이지 배경 불러오기" : "스테이지 배경 다시 불러오기"))
                    Load(root, prefab, view, context);
            }
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("공용 카메라 시작 구도 다시 읽기")) RefreshCamera(root);
            }
            CombatMapAuthoringCameraPose pose = CombatMapAuthoringCameraSetup.FindPose(root);
            if (pose != null && !string.IsNullOrEmpty(pose.Problem))
                EditorGUILayout.HelpBox(pose.Problem, MessageType.Warning);
            if (!string.IsNullOrEmpty(_problem))
                EditorGUILayout.HelpBox(_problem, MessageType.Error);
        }

        private static bool TryRead(ScriptableObject stage, out GameObject prefab, out CombatStageSoView view,
            out FbxCombatStageContext context, out string problem)
        {
            prefab = null;
            view = null;
            context = null;
            problem = null;
            if (stage == null)
            {
                problem = "운영 ViewStageDataSO를 선택하세요.";
                return false;
            }
            try
            {
                var source = CombatStageSoAssets.ReadStageViewPropSource(stage);
                prefab = source.Prefab;
                context = FbxCombatStageContext.Read(stage, prefab);
                if (!context.IsReady)
                {
                    problem = context.Problem;
                    return false;
                }
                view = CombatStageSoAssets.ReadStagePreviewView(stage);
                return true;
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                problem = exception.Message;
                return false;
            }
        }

        private void RefreshContext(ScriptableObject stage)
        {
            _contextStage = stage;
            _contextDirty = false;
            TryRead(stage, out _contextPrefab, out _contextView, out _context, out _contextProblem);
        }

        private void Load(CombatMapAuthoringRoot root, GameObject prefab, CombatStageSoView view,
            FbxCombatStageContext context)
        {
            if (root.PreviewContainer != null && !EditorUtility.DisplayDialog("스테이지 프리뷰 교체",
                "현재 씬의 프리뷰 인스턴스와 그 override를 지우고 다시 불러옵니다. Undo로 되돌릴 수 있습니다.",
                "다시 불러오기", "취소")) return;
            _problem = null;
            CombatMapAuthoringPlacement.Preview preview = default;
            try
            {
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("전투 배경 프리뷰 불러오기");
                CombatMapAuthoringCameraSetup.Ensure(root);
                preview = CombatMapAuthoringPlacement.Create(root.transform, prefab, view, context);
                Undo.RegisterCreatedObjectUndo(preview.Container, "전투 배경 프리뷰 만들기");
                if (root.PreviewContainer != null) Undo.DestroyObjectImmediate(root.PreviewContainer);
                Undo.RecordObject(root, "전투 배경 프리뷰 선택");
                root.SetPreview(root.ViewStage, preview.Container, preview.Map, view.positionOffset,
                    view.rotationOffset, view.scaleOffset, view.tiles.offset, view.tiles.anchorTile);
                CombatMapAuthoringGridSetup.Ensure(root, context);
                _unitDirty = true;
                EditorUtility.SetDirty(root);
                Undo.CollapseUndoOperations(group);
                Selection.activeGameObject = root.gameObject;
                EditorApplication.QueuePlayerLoopUpdate();
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                if (preview.Container != null && root.PreviewContainer != preview.Container)
                    Object.DestroyImmediate(preview.Container);
                _problem = exception.Message;
            }
        }

        private void RefreshCamera(CombatMapAuthoringRoot root)
        {
            _problem = null;
            try
            {
                CombatMapAuthoringCameraSetup.Ensure(root);
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                _problem = exception.Message;
            }
        }

        private void OnProjectChanged()
        {
            if (target is CombatMapAuthoringRoot root && root != null && root.gameObject.scene.isLoaded)
            {
                _contextDirty = true;
                _unitVisuals = null;
                _unitDirty = true;
                CombatMapAuthoringCameraSetup.FindPose(root)?.Refresh();
                EditorApplication.QueuePlayerLoopUpdate();
                Repaint();
            }
        }

        private void OnUndoRedo()
        {
            _contextDirty = true;
            _unitDirty = true;
            if (target is CombatMapAuthoringRoot root && root != null && root.gameObject.scene.isLoaded)
            {
                CombatMapAuthoringCameraSetup.FindPose(root)?.Refresh();
                root.GetComponent<CombatMapAuthoringGridPreview>()?.Refresh();
            }
            Repaint();
        }

        private static Type ViewStageType()
        {
            if (_viewStageType != null) return _viewStageType;
            foreach (Type candidate in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
                if (candidate.Name == CombatStageSoAssets.ViewType) return _viewStageType = candidate;
            return typeof(ScriptableObject);
        }
    }
}
