using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private bool showMaterialSettings = true;
        [SerializeField] private bool materialIncludeChildren = true;
        [SerializeField] private Material materialToApply;
        private readonly HashSet<int> _expandedMaterialRenderers = new HashSet<int>();

        private void DrawMaterialSettings()
        {
            List<Transform> selection = GetWorkspaceSelection();
            if (selection.Count == 0) return;
            GUILayout.Space(8);
            showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "머티리얼 입히기", true, EditorStyles.foldoutHeader);
            if (!showMaterialSettings) return;

            materialIncludeChildren = EditorGUILayout.ToggleLeft("자식 메시에도 적용", materialIncludeChildren);
            List<Renderer> renderers = GetMaterialRenderers(selection);
            if (renderers.Count == 0)
            {
                EditorGUILayout.HelpBox("머티리얼을 입힐 메시가 없습니다. FBX나 메시 오브젝트를 선택하세요.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("적용 범위: " + renderers.Count + "개 렌더러", EditorStyles.miniLabel);
            materialToApply = (Material)EditorGUILayout.ObjectField("일괄 머티리얼", materialToApply, typeof(Material), false);
            using (new EditorGUI.DisabledScope(materialToApply == null))
            {
                if (GUILayout.Button("선택 범위의 모든 슬롯에 입히기"))
                    Run(() => ApplyMaterialToRenderers(renderers, materialToApply));
            }
            EditorGUILayout.LabelField("아래 슬롯에 Project 창의 머티리얼을 끌어 놓으면 해당 부분만 바뀝니다. Ctrl+Z로 되돌릴 수 있습니다.", EditorStyles.wordWrappedMiniLabel);

            foreach (Renderer renderer in renderers)
            {
                int id = renderer.GetInstanceID();
                string path = renderer.transform == workspaceRoot.transform ? workspaceRoot.name :
                    AnimationUtility.CalculateTransformPath(renderer.transform, workspaceRoot.transform);
                if (renderers.Count == 1)
                {
                    EditorGUILayout.LabelField(path, EditorStyles.boldLabel);
                }
                else
                {
                    bool expanded = EditorGUILayout.Foldout(_expandedMaterialRenderers.Contains(id), path, true);
                    if (expanded) _expandedMaterialRenderers.Add(id);
                    else _expandedMaterialRenderers.Remove(id);
                    if (!expanded) continue;
                }

                Material[] materials = renderer.sharedMaterials;
                int slotCount = GetMaterialSlotCount(renderer, materials);
                for (int i = 0; i < slotCount; i++)
                {
                    Material current = i < materials.Length ? materials[i] : null;
                    EditorGUI.BeginChangeCheck();
                    Material next = (Material)EditorGUILayout.ObjectField("슬롯 " + i, current, typeof(Material), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        int slot = i;
                        Run(() => ApplyMaterialToSlot(renderer, slot, next));
                    }
                }
            }
            EditorGUILayout.LabelField("배치된 메시의 연결만 바꾸며 원본 FBX와 머티리얼 속성은 수정하지 않습니다. 맵 저장 시 프리팹에 반영됩니다. 베이크한 맵은 재질 변경 후 다시 베이크하세요.", EditorStyles.wordWrappedMiniLabel);
        }

        private List<Renderer> GetMaterialRenderers(List<Transform> selection)
        {
            var renderers = new List<Renderer>();
            var seen = new HashSet<Renderer>();
            foreach (Transform target in selection)
            {
                Renderer[] candidates = materialIncludeChildren ? target.GetComponentsInChildren<Renderer>(true) : target.GetComponents<Renderer>();
                foreach (Renderer renderer in candidates)
                {
                    if ((renderer is MeshRenderer || renderer is SkinnedMeshRenderer) && Owns(renderer.transform) && seen.Add(renderer))
                        renderers.Add(renderer);
                }
            }
            return renderers;
        }

        private static int GetMaterialSlotCount(Renderer renderer, Material[] materials)
        {
            if (materials.Length > 0) return materials.Length;
            Mesh mesh = renderer is SkinnedMeshRenderer skinned ? skinned.sharedMesh :
                renderer.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
            return mesh != null ? Mathf.Max(1, mesh.subMeshCount) : 1;
        }

        private void ApplyMaterialToSlot(Renderer renderer, int slot, Material material)
        {
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length == 0) materials = new Material[GetMaterialSlotCount(renderer, materials)];
            Undo.RecordObject(renderer, "맵 머티리얼 슬롯 변경");
            materials[slot] = material;
            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
            Changed();
        }

        private void ApplyMaterialToRenderers(List<Renderer> renderers, Material material)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("맵 머티리얼 일괄 적용");
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials.Length == 0) materials = new Material[GetMaterialSlotCount(renderer, materials)];
                Undo.RecordObject(renderer, "맵 머티리얼 일괄 적용");
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
            Undo.FlushUndoRecordObjects();
            Undo.CollapseUndoOperations(group);
            Undo.IncrementCurrentGroup();
            Changed();
        }
    }
}
