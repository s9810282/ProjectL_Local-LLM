using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectL.Tool.Editor
{
    /// <summary>일반 씬과 분리된 가상 씬에서 FBX·프리팹을 배치하고 맵 프리팹으로 저장합니다.</summary>
    public sealed partial class FbxPlacementWindow : SceneView
    {
        [SerializeField] private GameObject fbx;
        [SerializeField] private GameObject workspaceRoot;
        [SerializeField] private Transform selected;
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private ProjectL.Tool.Runtime.MapDefinitionSO mapDefinition;
        [SerializeField] private string savePath = "";
        [SerializeField] private float catalogPanelWidth = 230;
        [SerializeField] private float sidebarPanelWidth = 300;
        [SerializeField] private Vector3 placementPosition;
        [SerializeField] private bool placeOnSurface = true;
        [SerializeField] private bool alignToSurface;
        [SerializeField] private float surfaceOffset;
        private bool _showDropTarget;
        private Vector3 _dropPoint;
        private Vector3 _dropNormal;
        private Vector2 _sidebarScroll;
        private IMGUIContainer _sidebar;
        private VisualElement _catalogResizer;
        private VisualElement _sidebarResizer;
        private string _notice;
        private MessageType _noticeType;
        private Action _pendingEditorAction;
        private readonly HashSet<int> _expanded = new HashSet<int>();
        private int _resizerPointerId = -1;
        private bool _resizingCatalog;

        private const float MinCatalogPanelWidth = 260;
        private const float MinSidebarPanelWidth = 240;
        private const float MinViewportWidth = 320;
        private const float ResizerWidth = 6;

        [MenuItem("ProjectL/FBX 배치")]
        public static void Open() => GetWindow<FbxPlacementWindow>().Show();

        protected override bool SupportsStageHandling() => false;

        protected override void OnSceneGUI()
        {
            if (FbxPlacementBaker.IsRunning) Tools.current = UnityEditor.Tool.View;
            Event e = Event.current;
            if (e.type == EventType.MouseDown && !Owns(Selection.activeTransform))
                Selection.activeObject = null;
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) &&
                (e.commandName == "Delete" || e.commandName == "SoftDelete" || e.commandName == "Duplicate"))
            {
                // 맵 루트와 다른 씬의 오브젝트는 작업 화면 단축키의 대상이 아닙니다.
                bool valid = Selection.transforms.Length > 0;
                foreach (Transform target in Selection.transforms)
                    valid &= Owns(target) && target != workspaceRoot.transform;
                if (!valid) { e.Use(); return; }
            }
            base.OnSceneGUI();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            titleContent = new GUIContent("FBX 맵 작업실");
            minSize = new Vector2(1000, 620);
            saveChangesMessage = "가상 씬의 변경 사항을 맵 프리팹으로 저장할까요?";
            bool created = !FbxPlacementWorkspace.IsWorkspaceRoot(workspaceRoot);
            if (created)
            {
                workspaceRoot = FbxPlacementWorkspace.CreateRoot();
                hasUnsavedChanges = false;
            }
            BindWorkspace();
            sceneLighting = useWorkspaceLighting;
            sceneViewState.showSkybox = false;
            sceneViewState.showFog = false;
            if (showBakedResult && HasBakedResult) ShowBakedResult();
            if (created) LookAt(Vector3.zero, Quaternion.Euler(45, -35, 0), 15, false, true);
            EditorApplication.delayCall += BuildSidebar;
            duringSceneGui += DrawWorkspaceGui;
            Selection.selectionChanged += UpdateSelection;
            Undo.postprocessModifications += TrackModifications;
            Undo.undoRedoPerformed += OnWorkspaceUndo;
            EditorApplication.projectChanged += RefreshCatalog;
            EditorApplication.update += UpdateVirtualCameraPreview;
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            InitializeCatalog();
        }

        public override void OnDisable()
        {
            _selectionPressed = false;
            _selectionMarquee = false;
            EditorApplication.delayCall -= BuildSidebar;
            EditorApplication.delayCall -= ExecutePendingEditorAction;
            _pendingEditorAction = null;
            ClearVolumeProfileEditor();
            ReleaseVirtualCameraPreview();
            duringSceneGui -= DrawWorkspaceGui;
            Selection.selectionChanged -= UpdateSelection;
            Undo.postprocessModifications -= TrackModifications;
            Undo.undoRedoPerformed -= OnWorkspaceUndo;
            EditorApplication.projectChanged -= RefreshCatalog;
            EditorApplication.update -= UpdateVirtualCameraPreview;
            rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            _catalogPanel?.RemoveFromHierarchy();
            _catalogPanel = null;
            _sidebar?.RemoveFromHierarchy();
            _sidebar = null;
            RemoveVirtualCameraPreviewPanel();
            RemovePanelResizers();
            _placementToolbar?.RemoveFromHierarchy();
            _placementToolbar = null;
            // 재컴파일 때는 씬과 직렬화된 루트를 유지하고 실제 창 종료 때만 닫습니다.
            base.OnDisable();
        }

        private void BuildSidebar()
        {
            if (this == null) return;
            overlayCanvas.overlaysEnabled = false;
            _sidebar?.RemoveFromHierarchy();
            rootVisualElement.style.flexDirection = FlexDirection.Row;
            _sidebar = new IMGUIContainer(DrawSidebar);
            _sidebar.style.flexShrink = 0;
            _sidebar.style.position = Position.Absolute;
            _sidebar.style.top = PlacementToolbarHeight;
            _sidebar.style.bottom = 0;
            _sidebar.style.backgroundColor = new Color(.19f, .19f, .19f);
            rootVisualElement.Insert(0, _sidebar);
            BuildCatalogPanel();
            BuildPanelResizers();
            BuildPlacementToolbar();
            BuildVirtualCameraPreviewPanel();
            ApplyPanelLayout();
            Repaint();
        }

        private void BuildPanelResizers()
        {
            RemovePanelResizers();
            _catalogResizer = CreatePanelResizer(true);
            _sidebarResizer = CreatePanelResizer(false);
            // 패널 경계 위에서 항상 포인터를 받도록 마지막 자식으로 둔다.
            rootVisualElement.Add(_catalogResizer);
            rootVisualElement.Add(_sidebarResizer);
        }

        private VisualElement CreatePanelResizer(bool catalog)
        {
            var resizer = new VisualElement();
            resizer.style.position = Position.Absolute;
            resizer.style.top = PlacementToolbarHeight;
            resizer.style.bottom = 0;
            resizer.style.width = ResizerWidth;
            resizer.style.backgroundColor = new Color(.08f, .08f, .08f, .85f);
            resizer.RegisterCallback<PointerDownEvent>(evt => BeginPanelResize(evt, catalog, resizer));
            resizer.RegisterCallback<PointerMoveEvent>(ResizePanels);
            resizer.RegisterCallback<PointerUpEvent>(EndPanelResize);
            resizer.RegisterCallback<PointerCaptureOutEvent>(_ => _resizerPointerId = -1);
            return resizer;
        }

        private void BeginPanelResize(PointerDownEvent evt, bool catalog, VisualElement resizer)
        {
            _resizingCatalog = catalog;
            _resizerPointerId = evt.pointerId;
            resizer.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void ResizePanels(PointerMoveEvent evt)
        {
            if (_resizerPointerId != evt.pointerId) return;
            float x = rootVisualElement.WorldToLocal(evt.position).x;
            if (_resizingCatalog) catalogPanelWidth = x;
            else sidebarPanelWidth = x - catalogPanelWidth;
            ApplyPanelLayout();
            evt.StopPropagation();
        }

        private void EndPanelResize(PointerUpEvent evt)
        {
            if (_resizerPointerId != evt.pointerId) return;
            if (evt.currentTarget is VisualElement resizer) resizer.ReleasePointer(evt.pointerId);
            _resizerPointerId = -1;
            evt.StopPropagation();
        }

        private void RemovePanelResizers()
        {
            _catalogResizer?.RemoveFromHierarchy();
            _catalogResizer = null;
            _sidebarResizer?.RemoveFromHierarchy();
            _sidebarResizer = null;
            _resizerPointerId = -1;
        }

        private void OnRootGeometryChanged(GeometryChangedEvent _) => ApplyPanelLayout();

        private void ApplyPanelLayout()
        {
            if (_catalogPanel == null || _sidebar == null) return;
            float availableWidth = rootVisualElement.contentRect.width;
            if (availableWidth <= 0) return;

            float maxCatalogWidth = Mathf.Max(MinCatalogPanelWidth,
                availableWidth - MinSidebarPanelWidth - MinViewportWidth);
            catalogPanelWidth = Mathf.Clamp(catalogPanelWidth, MinCatalogPanelWidth, maxCatalogWidth);
            float maxSidebarWidth = Mathf.Max(MinSidebarPanelWidth,
                availableWidth - catalogPanelWidth - MinViewportWidth);
            sidebarPanelWidth = Mathf.Clamp(sidebarPanelWidth, MinSidebarPanelWidth, maxSidebarWidth);

            _catalogPanel.style.width = catalogPanelWidth;
            _sidebar.style.left = catalogPanelWidth;
            _sidebar.style.width = sidebarPanelWidth;
            if (_catalogResizer != null) _catalogResizer.style.left = catalogPanelWidth - ResizerWidth * .5f;
            if (_sidebarResizer != null) _sidebarResizer.style.left = catalogPanelWidth + sidebarPanelWidth - ResizerWidth * .5f;

            VisualElement viewport = rootVisualElement.Q("unity-scene-view-camera-rect");
            if (viewport != null)
            {
                viewport.style.left = catalogPanelWidth + sidebarPanelWidth;
                viewport.style.right = 0;
                viewport.style.width = StyleKeyword.Auto;
                viewport.style.top = PlacementToolbarHeight;
            }
            _catalogPanel.MarkDirtyRepaint();
            _sidebar.MarkDirtyRepaint();
            ApplyVirtualCameraPreviewLayout();
            Repaint();
        }

        public new void OnDestroy()
        {
            if (FbxPlacementBaker.Current != null && FbxPlacementBaker.Current.Root == workspaceRoot) FbxPlacementBaker.Current.Cancel();
            base.OnDestroy();
            ReleaseWorkspaceProfiles();
            FbxPlacementWorkspace.Close(workspaceRoot);
        }

        private void BindWorkspace()
        {
            customScene = workspaceRoot.scene;
            customParentForDraggedObjects = workspaceRoot.transform;
            FbxPlacementPreviewLighting.Apply(workspaceRoot);
            BindWorkspaceVolumes();
        }

        private bool Owns(Transform target) => workspaceRoot != null && target != null &&
            (target == workspaceRoot.transform || target.IsChildOf(workspaceRoot.transform));

        private void UpdateSelection()
        {
            if (showBakedResult && Owns(Selection.activeTransform)) ShowEditingView();
            selected = null;
            if (Owns(Selection.activeTransform))
            {
                selected = Selection.activeTransform;
                if (FbxPlacementLayers.IsLayer(workspaceRoot, selected)) placementLayer = selected;
            }
            Repaint();
        }

        private void Select(Transform target)
        {
            selected = target;
            if (FbxPlacementLayers.IsLayer(workspaceRoot, target)) placementLayer = target;
            Selection.activeGameObject = target != null ? target.gameObject : null;
            Repaint();
        }

        private UndoPropertyModification[] TrackModifications(UndoPropertyModification[] changes)
        {
            foreach (UndoPropertyModification change in changes)
            {
                UnityEngine.Object target = change.currentValue.target;
                if ((target is UnityEngine.Rendering.Volume volume && Owns(volume.transform)) || IsWorkspaceProfileObject(target))
                {
                    MoodChanged();
                    break;
                }
                Transform transform = target is Component component ? component.transform :
                    target is GameObject go ? go.transform : null;
                if (Owns(transform)) { Changed(); break; }
            }
            return changes;
        }

        private void OnWorkspaceUndo()
        {
            if (workspaceRoot != null) Changed();
        }

        private void Changed()
        {
            ShowEditingView();
            hasUnsavedChanges = true;
            Repaint();
        }

        private void DrawSidebar()
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(workspaceRoot)) return;
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 85;
            using var scroll = new EditorGUILayout.ScrollViewScope(_sidebarScroll);
            _sidebarScroll = scroll.scrollPosition;
            try
            {
                GUILayout.Space(6);
                GUILayout.Label("가상 씬 · 맵 배치", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("FBX·프리팹을 Unpack하여 이 창 안에서 편집합니다.", EditorStyles.wordWrappedMiniLabel);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || FbxPlacementBaker.IsRunning))
                {
                    DrawFiles();
                    GUILayout.Space(8);
                    DrawAddAsset();
                    GUILayout.Space(8);
                    GUILayout.Label("오브젝트 목록", EditorStyles.boldLabel);
                    DrawHierarchy(workspaceRoot.transform, 0);
                    GUILayout.Space(8);
                    DrawTransform();
                }
                if (!string.IsNullOrEmpty(_notice)) EditorGUILayout.HelpBox(_notice, _noticeType);
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("왼쪽 드래그: 사각형 다중 선택\nShift: 선택 추가 · Ctrl: 선택 전환\nEsc: 드래그 선택 취소\nW 이동 · E 회전 · R 크기 · F 선택 보기\nAlt+왼쪽 드래그: 회전 · 휠: 확대/축소\n중간 버튼 드래그: 화면 이동\nFBX·프리팹을 작업 화면에 드래그해도 추가됩니다.", MessageType.None);
                EditorGUILayout.LabelField("프리팹에는 배치한 맵 오브젝트와 라이트가 함께 저장됩니다.", EditorStyles.wordWrappedMiniLabel);
            }
            finally
            {
                EditorGUIUtility.labelWidth = oldWidth;
            }
        }

        private void DrawFiles()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("새 맵")) DeferEditorAction(NewMap);
                if (GUILayout.Button("맵 SO 저장")) DeferEditorAction(() => TrySave(false));
                if (GUILayout.Button("불러오기…")) DeferEditorAction(BrowseMapDefinition);
            }
            if (GUILayout.Button("다른 이름으로 저장")) DeferEditorAction(() => TrySave(true));
            mapDefinition = (ProjectL.Tool.Runtime.MapDefinitionSO)EditorGUILayout.ObjectField("열 맵 SO", mapDefinition,
                typeof(ProjectL.Tool.Runtime.MapDefinitionSO), false);
            using (new EditorGUI.DisabledScope(mapDefinition == null))
                if (GUILayout.Button("맵 불러오기 (베이크 포함)")) DeferEditorAction(OpenMapDefinition);
            mapPrefab = (GameObject)EditorGUILayout.ObjectField("열 맵 프리팹", mapPrefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(mapPrefab == null))
            {
                if (GUILayout.Button("맵 프리팹을 가상 씬으로 열기")) DeferEditorAction(OpenMap);
            }
            if (!string.IsNullOrEmpty(savePath)) EditorGUILayout.LabelField(savePath, EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawAddAsset()
        {
            GUILayout.Label("FBX · 프리팹 추가", EditorStyles.boldLabel);
            fbx = (GameObject)EditorGUILayout.ObjectField("배치 에셋", fbx, typeof(GameObject), false);
            if (GUILayout.Button("Project에서 선택한 에셋 가져오기"))
                Run(() => fbx = FbxPlacement.GetPlacementAsset(Selection.activeGameObject));
            placementPosition = EditorGUILayout.Vector3Field("추가 위치", placementPosition);
            EditorGUILayout.LabelField("드래그 배치 옵션은 상단 도구 모음에서 설정합니다.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(fbx == null))
            {
                if (GUILayout.Button("가상 씬에 추가 + Unpack", GUILayout.Height(28))) Run(() => Add(fbx, placementPosition));
            }
        }

        private void DrawHierarchy(Transform node, int depth)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(depth * 12);
                int id = node.GetInstanceID();
                if (node.childCount > 0 && node != workspaceRoot.transform)
                {
                    Rect arrow = GUILayoutUtility.GetRect(14, 20, GUILayout.Width(14));
                    bool open = EditorGUI.Foldout(arrow, _expanded.Contains(id), GUIContent.none, true);
                    if (open) _expanded.Add(id);
                    else _expanded.Remove(id);
                }
                else GUILayout.Space(14);
                bool active = Selection.Contains(node.gameObject);
                string label = FbxPlacementLayers.IsLayer(workspaceRoot, node) ? "레이어 · " + FbxPlacementLayers.GetName(node) : node.name;
                if (GUILayout.Toggle(active, label, "Button", GUILayout.Height(20)) != active)
                {
                    Event e = Event.current;
                    if ((e.shift || e.control || e.command) && node != workspaceRoot.transform && !FbxPlacementLayers.IsLayer(workspaceRoot, node))
                        SelectMany(FbxPlacementSelection.Combine(workspaceRoot, Selection.transforms, new[] { node },
                            (e.control || e.command) ? FbxPlacementSelection.Mode.Toggle : FbxPlacementSelection.Mode.Add));
                    else Select(node);
                }
            }
            if (node == workspaceRoot.transform || _expanded.Contains(node.GetInstanceID()))
            {
                foreach (Transform child in node) DrawHierarchy(child, depth + 1);
            }
        }

        private void DrawTransform()
        {
            if (!Owns(selected)) return;
            int selectionCount = GetWorkspaceSelection().Count;
            if (selectionCount > 1)
            {
                GUILayout.Label(selectionCount + "개 선택됨", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("상단 이동·회전·크기 도구로 함께 편집합니다.\n선택 옮기기로 같은 레이어에 모을 수 있습니다.", MessageType.Info);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("선택 전체 복제")) Run(Duplicate);
                    if (GUILayout.Button("선택 전체 삭제"))
                    {
                        DeleteSelection();
                        GUIUtility.ExitGUI();
                    }
                }
                DrawMaterialSettings();
                DrawStaticSettings();
                return;
            }
            GUILayout.Label("선택 오브젝트", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            bool isLayer = FbxPlacementLayers.IsLayer(workspaceRoot, selected);
            string objectName = isLayer ? EditorGUILayout.DelayedTextField("레이어 이름", FbxPlacementLayers.GetName(selected)) :
                EditorGUILayout.TextField("이름", selected.name);
            bool active = EditorGUILayout.Toggle("활성", selected.gameObject.activeSelf);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selected.gameObject, "맵 오브젝트 설정");
                if (isLayer) Run(() => FbxPlacementLayers.Rename(workspaceRoot, selected, objectName));
                else selected.name = objectName;
                selected.gameObject.SetActive(active);
                Changed();
            }
            using (new EditorGUI.DisabledScope(selected == workspaceRoot.transform))
            {
                EditorGUI.BeginChangeCheck();
                Vector3 localPosition = EditorGUILayout.Vector3Field("로컬 위치", selected.localPosition);
                Vector3 localRotation = EditorGUILayout.Vector3Field("로컬 회전", selected.localEulerAngles);
                Vector3 localScale = EditorGUILayout.Vector3Field("로컬 크기", selected.localScale);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(selected, "맵 오브젝트 변환");
                    selected.localPosition = localPosition;
                    selected.localEulerAngles = localRotation;
                    selected.localScale = localScale;
                    Changed();
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("복제")) Run(Duplicate);
                    if (GUILayout.Button("삭제"))
                    {
                        DeleteSelection();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            DrawCameraSettings();
            DrawLightSettings();
            DrawMaterialSettings();
            if (selected.GetComponent<Cinemachine.CinemachineVirtualCamera>() == null)
                DrawStaticSettings();
        }

        private void DrawWorkspaceGui(SceneView view)
        {
            if (view != this || workspaceRoot == null || showBakedResult || EditorApplication.isPlayingOrWillChangePlaymode || FbxPlacementBaker.IsRunning) return;
            Event e = Event.current;
            DrawWorkspaceLights();
            DrawWorkspaceCameras();
            if (e.type == EventType.Repaint && _showDropTarget)
            {
                using (new Handles.DrawingScope(new Color(.3f, 1, .5f)))
                {
                    float size = HandleUtility.GetHandleSize(_dropPoint) * .15f;
                    Handles.DrawWireDisc(_dropPoint, _dropNormal, size);
                    Handles.DrawLine(_dropPoint, _dropPoint + _dropNormal * size * 2);
                }
            }
            if (e.type == EventType.DragExited)
            {
                _showDropTarget = false;
                DragAndDrop.SetGenericData(LightDragKey, null);
                Repaint();
            }
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && e.commandName == "Save")
            {
                if (e.type == EventType.ExecuteCommand) TrySave(false);
                e.Use();
                return;
            }
            if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
            {
                if (HandleLightDrop(e)) return;
                bool valid = DragAndDrop.objectReferences.Length > 0;
                var models = new List<GameObject>();
                foreach (UnityEngine.Object asset in DragAndDrop.objectReferences)
                {
                    try { models.Add(FbxPlacement.GetPlacementAsset(asset as GameObject)); }
                    catch (ArgumentException) { valid = false; break; }
                }
                _showDropTarget = false;
                if (valid)
                {
                    try { valid = TryGetDropTarget(HandleUtility.GUIPointToWorldRay(e.mousePosition), out _dropPoint, out _dropNormal); }
                    catch (Exception exception) { valid = false; SetNotice(exception.Message, MessageType.Error); }
                }
                _showDropTarget = valid;
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                Repaint();
                if (valid && e.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    Run(() =>
                    {
                        foreach (GameObject model in models)
                        {
                            GameObject instance = FbxPlacementWorkspace.AddAssetOnSurface(workspaceRoot, model,
                                _dropPoint, _dropNormal, placeOnSurface && alignToSurface, surfaceOffset, PlacementParent);
                            BindWorkspaceVolumes();
                            _expanded.Add(PlacementParent.GetInstanceID());
                            Select(instance.transform);
                            Changed();
                        }
                        Tools.current = UnityEditor.Tool.Move;
                        _notice = null;
                    });
                    _showDropTarget = false;
                }
                e.Use();
                return;
            }
            DrawWorkspaceSelection();
        }

        private bool TryGetDropTarget(Ray ray, out Vector3 point, out Vector3 normal)
        {
            if (placeOnSurface && FbxPlacementSurface.TryRaycast(workspaceRoot, ray, out RaycastHit hit,
                layer: surfaceLayerOnly ? SurfaceParent : null))
            {
                point = hit.point;
                normal = hit.normal;
                return true;
            }
            normal = Vector3.up;
            point = Vector3.zero;
            // 지정 레이어에 표면이 없을 때 다른 레이어나 가상 바닥으로 배치하지 않습니다.
            if (placeOnSurface && surfaceLayerOnly) return false;
            if (!new Plane(normal, point).Raycast(ray, out float distance)) return false;
            point = ray.GetPoint(distance);
            return true;
        }

        private void Add(GameObject model, Vector3 point)
        {
            GameObject instance = FbxPlacementWorkspace.AddAsset(workspaceRoot, model, point, PlacementParent);
            BindWorkspaceVolumes();
            _expanded.Add(PlacementParent.GetInstanceID());
            Select(instance.transform);
            Changed();
            Tools.current = UnityEditor.Tool.Move;
            Frame(instance.transform);
            _notice = null;
        }

        private void Duplicate()
        {
            if (!CanMoveSelectedToLayer()) throw new InvalidOperationException("복제할 배치 오브젝트를 선택하세요.");
            List<Transform> copies = new List<Transform>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            foreach (Transform target in GetWorkspaceSelection())
            {
                GameObject copy = Instantiate(target.gameObject, target.parent);
                copy.name = target.name;
                Undo.RegisterCreatedObjectUndo(copy, "맵 오브젝트 복제");
                copies.Add(copy.transform);
            }
            Undo.CollapseUndoOperations(group);
            SelectMany(copies);
            Changed();
        }

        private void Frame(Transform target)
        {
            if (!Owns(target)) return;
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(target.position, Vector3.one);
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            }
            Frame(bounds, false);
        }

        private bool ConfirmReplace()
        {
            if (!hasUnsavedChanges) return true;
            int answer = EditorUtility.DisplayDialogComplex("맵 배치", "현재 가상 씬의 변경 사항을 저장할까요?", "저장", "취소", "저장하지 않음");
            return answer == 2 || (answer == 0 && TrySave(false));
        }

        private void ReplaceWorkspace(GameObject root)
        {
            ShowEditingView();
            GameObject old = workspaceRoot;
            ReleaseVirtualCameraPreview();
            ReleaseWorkspaceProfiles();
            workspaceRoot = root;
            BindWorkspace();
            FbxPlacementWorkspace.Close(old);
            selected = null;
            placementLayer = null;
            surfaceLayer = null;
            _selectionPressed = false;
            _selectionMarquee = false;
            _expanded.Clear();
            hasUnsavedChanges = false;
            Frame(workspaceRoot.transform);
        }

        private void NewMap()
        {
            if (!ConfirmReplace()) return;
            ReplaceWorkspace(FbxPlacementWorkspace.CreateRoot());
            mapPrefab = null;
            mapDefinition = null;
            savePath = "";
        }

        private void OpenMap()
        {
            GameObject source = mapPrefab;
            if (!ConfirmReplace()) return;
            string path = AssetDatabase.GetAssetPath(source);
            var definition = AssetDatabase.LoadAssetAtPath<ProjectL.Tool.Runtime.MapDefinitionSO>(FbxPlacementMapAssets.DefinitionPath(path));
            GameObject root = definition != null && definition.Prefab == source ? FbxPlacementMapAssets.Open(definition) : FbxPlacementWorkspace.OpenPrefab(source);
            ReplaceWorkspace(root);
            mapPrefab = source;
            mapDefinition = definition;
            savePath = AssetDatabase.GetAssetPath(source);
            if (HasBakedResult) ShowBakedResult();
        }

        private void OpenMapDefinition()
        {
            LoadMapDefinition(mapDefinition);
        }

        private bool TrySave(bool saveAs, bool preparingBake = false)
        {
            try
            {
                string path = savePath;
                if (saveAs || string.IsNullOrEmpty(path))
                    path = EditorUtility.SaveFilePanelInProject("맵 저장", workspaceRoot.name, "prefab", "맵 프리팹과 맵 SO를 저장할 위치를 선택하세요.");
                if (string.IsNullOrEmpty(path)) return false;
                if (preparingBake)
                {
                    // 취소하거나 실패하면 기존 DB가 참조하는 맵 에셋은 그대로 둡니다.
                    savePath = path;
                    return true;
                }
                mapDefinition = FbxPlacementMapAssets.Save(workspaceRoot, path);
                mapPrefab = mapDefinition.Prefab;
                savePath = path;
                BindWorkspace();
                hasUnsavedChanges = false;
                SetNotice("맵 SO 저장 완료: " + AssetDatabase.GetAssetPath(mapDefinition), MessageType.Info);
                return true;
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                SetNotice(exception.Message, MessageType.Error);
                return false;
            }
        }

        public override void SaveChanges()
        {
            if (TrySave(false)) base.SaveChanges();
        }

        private void DeferEditorAction(Action action)
        {
            if (_pendingEditorAction != null) return;
            _pendingEditorAction = action;
            // 저장창·재임포트·씬 전환은 IMGUI의 레이아웃 스택을 닫은 뒤 실행합니다.
            EditorApplication.delayCall += ExecutePendingEditorAction;
        }

        private void ExecutePendingEditorAction()
        {
            EditorApplication.delayCall -= ExecutePendingEditorAction;
            Action action = _pendingEditorAction;
            _pendingEditorAction = null;
            if (this == null || action == null) return;
            Run(action);
        }

        private void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) when (!(exception is ExitGUIException)) { SetNotice(exception.Message, MessageType.Error); }
        }

        private void SetNotice(string message, MessageType type)
        {
            _notice = message;
            _noticeType = type;
            Repaint();
        }
    }
}
