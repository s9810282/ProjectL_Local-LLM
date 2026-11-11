using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>Scene·Prefab Mode에서 덱 카드 배치 설정을 즉시 반영하고 Undo·프리팹 변경 기록을 연결합니다.</summary>
    [CustomEditor(typeof(DeckCardList)), CanEditMultipleObjects]
    public sealed class DeckCardListEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            EditorApplication.delayCall += ApplyPreview;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= ApplyPreview;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            ApplyPreview();
        }

        // OnSceneGUI는 Unity가 대상별로 호출하므로 targets 배열을 조회하지 않습니다.
        private void OnSceneGUI() => ApplyPreview(target as DeckCardList, true);
        private void ApplyPreview() => ApplyPreview(true);
        private void OnUndoRedo() => ApplyPreview(false);

        private void ApplyPreview(bool recordUndo)
        {
            foreach (UnityEngine.Object item in targets)
                ApplyPreview(item as DeckCardList, recordUndo);
        }

        private static void ApplyPreview(DeckCardList list, bool recordUndo)
        {
            // Play 전환 중·프로젝트 창의 원본 에셋은 대상이 아닙니다. Prefab Mode의 편집 인스턴스는 허용합니다.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (list == null || EditorUtility.IsPersistent(list) || !list.gameObject.scene.IsValid()) return;
            using var data = new SerializedObject(list);
            SerializedProperty members = data.FindProperty("initialInstances");
            if (members == null)
                throw new InvalidOperationException($"[DeckCardListEditor] UIId.UnitCard, '{list.name}': initialInstances 필드가 필요합니다.");
            var cards = new UnitCardUI[members.arraySize];
            var transforms = new RectTransform[members.arraySize];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = members.GetArrayElementAtIndex(i).objectReferenceValue as UnitCardUI;
                if (cards[i] == null || !(cards[i].transform is RectTransform rect))
                    throw new InvalidOperationException($"[DeckCardListEditor] UIId.UnitCard, '{list.name}': initialInstances[{i}]에 RectTransform을 가진 UnitCardUI가 필요합니다.");
                transforms[i] = rect;
            }
            if (recordUndo) Undo.RecordObjects(transforms, "덱 카드 배치 변경");
            if (!list.ApplyEditorLayoutPreview(cards)) return;
            foreach (RectTransform rect in transforms)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(rect))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
                EditorUtility.SetDirty(rect);
            }
            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);
            SceneView.RepaintAll();
        }
    }
}
