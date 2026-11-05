using System;
using System.IO;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        [SerializeField] private bool showBakedResult;
        [SerializeField] private bool editingGrid = true;
        [SerializeField] private bool editingGizmos = true;

        private bool HasBakedResult => workspaceRoot != null &&
            workspaceRoot.TryGetComponent(out MapBakedLighting binding) && binding.AtlasCount > 0;

        private void ShowBakedResult()
        {
            if (!HasBakedResult) throw new InvalidOperationException("저장된 베이크 결과가 없습니다. 먼저 베이크하거나 베이크된 맵 SO를 불러오세요.");
            // 씬 전환 뒤에도 작업실 카메라가 이 가상 씬과 라이트맵을 사용하도록 다시 연결합니다.
            BindWorkspace();
            if (!showBakedResult)
            {
                editingGrid = showGrid;
                editingGizmos = drawGizmos;
            }
            showBakedResult = true;
            useWorkspaceLighting = sceneLighting = true;
            cameraMode = GetBuiltinCameraMode(DrawCameraMode.Textured);
            showGrid = drawGizmos = false;
            _showDropTarget = false;
            if (Owns(Selection.activeTransform)) Selection.activeObject = null;
            selected = null;
            Frame(workspaceRoot.transform);
            Repaint();
        }

        private void ShowEditingView()
        {
            if (!showBakedResult) return;
            showBakedResult = false;
            showGrid = editingGrid;
            drawGizmos = editingGizmos;
            Repaint();
        }

        private void LoadMapDefinition(MapDefinitionSO source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Validate();
            if (!ConfirmReplace()) return;
            ReplaceWorkspace(FbxPlacementMapAssets.Open(source));
            mapDefinition = source;
            mapPrefab = source.Prefab;
            savePath = AssetDatabase.GetAssetPath(source.Prefab);
            if (HasBakedResult) ShowBakedResult();
            SetNotice(HasBakedResult ? "베이크된 맵을 불러왔습니다. 완성본을 표시합니다." :
                "맵을 불러왔습니다. 아직 베이크된 결과는 없습니다.", MessageType.Info);
        }

        private void BrowseMapDefinition()
        {
            string folder = mapDefinition != null ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(mapDefinition)) : "Assets";
            string path = EditorUtility.OpenFilePanel("맵 SO 불러오기", Path.GetFullPath(folder), "asset");
            if (string.IsNullOrEmpty(path)) return;
            string relative = FileUtil.GetProjectRelativePath(path);
            MapDefinitionSO source = AssetDatabase.LoadAssetAtPath<MapDefinitionSO>(relative);
            if (source == null) throw new InvalidOperationException("이 프로젝트에서 저장한 맵 SO(_Map.asset)를 선택하세요.");
            LoadMapDefinition(source);
        }
    }
}
