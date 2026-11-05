using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        private const float PlacementToolbarHeight = 58;
        [SerializeField] private Transform placementLayer;
        [SerializeField] private Transform surfaceLayer;
        [SerializeField] private bool surfaceLayerOnly;
        [SerializeField] private string newPlacementLayerName = "";
        private IMGUIContainer _placementToolbar;

        private Transform PlacementParent => FbxPlacementLayers.IsLayer(workspaceRoot, placementLayer)
            ? placementLayer : workspaceRoot.transform;

        private Transform SurfaceParent => FbxPlacementLayers.IsLayer(workspaceRoot, surfaceLayer)
            ? surfaceLayer : workspaceRoot.transform;

        private void BuildPlacementToolbar()
        {
            _placementToolbar?.RemoveFromHierarchy();
            _placementToolbar = new IMGUIContainer(DrawPlacementToolbar);
            _placementToolbar.style.position = Position.Absolute;
            _placementToolbar.style.left = 0;
            _placementToolbar.style.right = 0;
            _placementToolbar.style.top = 0;
            _placementToolbar.style.height = PlacementToolbarHeight;
            _placementToolbar.style.backgroundColor = new Color(.16f, .16f, .16f);
            rootVisualElement.Add(_placementToolbar);
            _placementToolbar.BringToFront();
        }

        private void DrawPlacementToolbar()
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(workspaceRoot)) return;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || FbxPlacementBaker.IsRunning))
            {
                float x = 6;
                if (GUI.Toggle(ToolbarRect(ref x, 57), Tools.current == UnityEditor.Tool.Move, "W 이동", EditorStyles.toolbarButton)) Tools.current = UnityEditor.Tool.Move;
                if (GUI.Toggle(ToolbarRect(ref x, 57), Tools.current == UnityEditor.Tool.Rotate, "E 회전", EditorStyles.toolbarButton)) Tools.current = UnityEditor.Tool.Rotate;
                if (GUI.Toggle(ToolbarRect(ref x, 57), Tools.current == UnityEditor.Tool.Scale, "R 크기", EditorStyles.toolbarButton)) Tools.current = UnityEditor.Tool.Scale;
                x += 8;
                if (GUI.Button(ToolbarRect(ref x, 73), "전체 보기", EditorStyles.toolbarButton)) Frame(workspaceRoot.transform);
                using (new EditorGUI.DisabledScope(!Owns(selected)))
                    if (GUI.Button(ToolbarRect(ref x, 73), "선택 보기", EditorStyles.toolbarButton)) FrameSelection();
                x += 8;
                placeOnSurface = GUI.Toggle(ToolbarRect(ref x, 90), placeOnSurface,
                    new GUIContent("표면에 배치", "FBX·프리팹 드래그 시 기존 오브젝트 표면에 배치합니다."), EditorStyles.toolbarButton);
                using (new EditorGUI.DisabledScope(!placeOnSurface))
                    alignToSurface = GUI.Toggle(ToolbarRect(ref x, 90), alignToSurface,
                        new GUIContent("경사면 회전", "모델의 위쪽을 표면 방향에 맞춥니다."), EditorStyles.toolbarButton);
                x += 8;
                GUI.Label(ToolbarRect(ref x, 58), "표면 간격", EditorStyles.miniLabel);
                surfaceOffset = EditorGUI.FloatField(ToolbarRect(ref x, 62), surfaceOffset);
                x += 8;
                using (new EditorGUI.DisabledScope(!CanMoveSelectedToLayer()))
                    if (GUI.Button(ToolbarRect(ref x, 58), "복제", EditorStyles.toolbarButton)) Run(Duplicate);
                x += 8;
                bool lit = GUI.Toggle(ToolbarRect(ref x, 80), useWorkspaceLighting,
                    new GUIContent("씬 조명", "배치한 Unity 라이트를 사용합니다. 끄면 편집용 기본 조명을 사용합니다."), EditorStyles.toolbarButton);
                if (lit != useWorkspaceLighting)
                {
                    ShowEditingView();
                    useWorkspaceLighting = sceneLighting = lit;
                    Repaint();
                }
                if (GUI.Button(ToolbarRect(ref x, 65), "베이크", EditorStyles.toolbarButton))
                {
                    showBakeCatalog = true;
                    showCameraCatalog = false;
                    showLightCatalog = false;
                    showVolumeCatalog = false;
                    showHelpCatalog = false;
                    Repaint();
                }
                DrawLayerToolbar();
                using (new EditorGUI.DisabledScope(!showBakedResult && !HasBakedResult))
                {
                    if (GUI.Button(new Rect(830, 32, 112, 22),
                        new GUIContent(showBakedResult ? "배치 편집으로" : "베이크 완성본", "베이크 결과를 조명과 함께 표시하고 편집용 격자와 표시를 숨깁니다."), EditorStyles.toolbarButton))
                    {
                        if (showBakedResult) DeferEditorAction(ShowEditingView);
                        else DeferEditorAction(ShowBakedResult);
                    }
                }
            }
        }

        private void DrawLayerToolbar()
        {
            List<Transform> layers = FbxPlacementLayers.GetLayers(workspaceRoot);
            GUI.Label(new Rect(8, 33, 70, 20), "배치 레이어", EditorStyles.miniLabel);
            placementLayer = LayerPopup(new Rect(80, 32, 140, 22), placementLayer, layers);
            GUI.SetNextControlName("NewPlacementLayerName");
            Rect nameRect = new Rect(228, 32, 110, 22);
            newPlacementLayerName = EditorGUI.TextField(nameRect, newPlacementLayerName);
            if (string.IsNullOrEmpty(newPlacementLayerName) && GUI.GetNameOfFocusedControl() != "NewPlacementLayerName")
                GUI.Label(new Rect(nameRect.x + 4, nameRect.y + 2, nameRect.width - 8, 18), "새 레이어 이름", EditorStyles.centeredGreyMiniLabel);
            if (GUI.Button(new Rect(342, 32, 56, 22), new GUIContent("추가", "입력한 이름으로 배치 레이어를 만듭니다."), EditorStyles.toolbarButton))
                Run(() =>
                {
                    placementLayer = FbxPlacementLayers.Create(workspaceRoot, newPlacementLayerName);
                    _expanded.Add(placementLayer.GetInstanceID());
                    newPlacementLayerName = "";
                    Changed();
                });
            using (new EditorGUI.DisabledScope(!CanMoveSelectedToLayer()))
            {
                if (GUI.Button(new Rect(402, 32, 86, 22), new GUIContent("선택 옮기기", "선택한 배치 오브젝트 전체를 배치 레이어로 옮깁니다. 월드 위치를 유지합니다."), EditorStyles.toolbarButton))
                    Run(() =>
                    {
                        List<Transform> moved = new List<Transform>();
                        Undo.IncrementCurrentGroup();
                        int group = Undo.GetCurrentGroup();
                        foreach (Transform target in GetWorkspaceSelection())
                        {
                            Transform placed = FbxPlacementLayers.MoveObject(workspaceRoot, target, PlacementParent);
                            if (!moved.Contains(placed)) moved.Add(placed);
                        }
                        Undo.CollapseUndoOperations(group);
                        _expanded.Add(PlacementParent.GetInstanceID());
                        SelectMany(moved);
                        Changed();
                    });
            }
            GUI.Label(new Rect(504, 33, 58, 20), "표면 대상", EditorStyles.miniLabel);
            using (new EditorGUI.DisabledScope(!placeOnSurface))
            {
                surfaceLayerOnly = EditorGUI.Popup(new Rect(564, 32, 110, 22), surfaceLayerOnly ? 1 : 0,
                    new[] { "모든 표면", "지정 레이어" }) == 1;
                using (new EditorGUI.DisabledScope(!surfaceLayerOnly))
                    surfaceLayer = LayerPopup(new Rect(680, 32, 140, 22), surfaceLayer, layers);
            }
        }

        private Transform LayerPopup(Rect rect, Transform current, List<Transform> layers)
        {
            string[] names = new string[layers.Count + 1];
            names[0] = "기본 (" + workspaceRoot.name + ")";
            int index = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                names[i + 1] = FbxPlacementLayers.GetName(layers[i]);
                if (layers[i] == current) index = i + 1;
            }
            int chosen = EditorGUI.Popup(rect, index, names);
            return chosen == 0 ? null : layers[chosen - 1];
        }

        private bool CanMoveSelectedToLayer()
        {
            List<Transform> targets = GetWorkspaceSelection();
            if (targets.Count == 0) return false;
            foreach (Transform target in targets)
                if (target == workspaceRoot.transform || FbxPlacementLayers.IsLayer(workspaceRoot, target)) return false;
            return true;
        }

        private static Rect ToolbarRect(ref float x, float width)
        {
            Rect rect = new Rect(x, 4, width, 22);
            x += width + 2;
            return rect;
        }
    }
}
