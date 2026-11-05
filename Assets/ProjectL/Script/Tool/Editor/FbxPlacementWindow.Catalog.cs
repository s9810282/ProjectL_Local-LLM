using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectL.Tool.Editor
{
    public sealed partial class FbxPlacementWindow
    {
        private float CatalogWidth => catalogPanelWidth;
        private const float CatalogRowHeight = 54;
        private const string MapCatalogPath = "Assets/ProjectL/Resources/Map";
        [SerializeField] private DefaultAsset catalogFolder;
        [SerializeField] private string catalogSearch = "";
        [SerializeField] private bool catalogInitialized;
        private IMGUIContainer _catalogPanel;
        private Vector2 _catalogScroll;
        private readonly List<string> _catalogPaths = new List<string>();
        private readonly List<string> _filteredCatalogPaths = new List<string>();
        private readonly List<string> _catalogFolders = new List<string>();
        private readonly List<string> _filteredCatalogFolders = new List<string>();
        private string _dragAssetPath;
        private Vector2 _dragStart;
        [SerializeField] private bool showHelpCatalog;
        private Vector2 _helpScroll;

        private void InitializeCatalog()
        {
            if (!catalogInitialized)
            {
                string folder = fbx != null ? Path.GetDirectoryName(AssetDatabase.GetAssetPath(fbx))?.Replace('\\', '/') :
                    MapCatalogPath;
                catalogFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folder ?? "Assets");
                catalogInitialized = true;
            }
            RefreshCatalog();
        }

        private void BuildCatalogPanel()
        {
            _catalogPanel?.RemoveFromHierarchy();
            _catalogPanel = new IMGUIContainer(DrawCatalog);
            _catalogPanel.style.width = CatalogWidth;
            _catalogPanel.style.flexShrink = 0;
            _catalogPanel.style.position = Position.Absolute;
            _catalogPanel.style.left = 0;
            _catalogPanel.style.top = PlacementToolbarHeight;
            _catalogPanel.style.bottom = 0;
            _catalogPanel.style.backgroundColor = new Color(.15f, .15f, .15f);
            rootVisualElement.Insert(0, _catalogPanel);
        }

        private void RefreshCatalog()
        {
            _catalogPaths.Clear();
            _catalogFolders.Clear();
            string folder = GetCatalogPath();
            _catalogFolders.AddRange(AssetDatabase.GetSubFolders(folder));
            // 현재 폴더만 읽습니다. 하위 폴더의 배치 에셋은 해당 폴더를 열었을 때 표시합니다.
            foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                if (FbxPlacement.IsSupportedAssetPath(file))
                    _catalogPaths.Add(file.Replace('\\', '/'));
            }
            _catalogFolders.Sort(StringComparer.OrdinalIgnoreCase);
            _catalogPaths.Sort((a, b) =>
            {
                int comparison = StringComparer.OrdinalIgnoreCase.Compare(Path.GetFileNameWithoutExtension(a), Path.GetFileNameWithoutExtension(b));
                return comparison != 0 ? comparison : StringComparer.OrdinalIgnoreCase.Compare(a, b);
            });
            FilterCatalog();
        }

        private void FilterCatalog()
        {
            _filteredCatalogPaths.Clear();
            _filteredCatalogFolders.Clear();
            string query = (catalogSearch ?? "").Trim();
            foreach (string path in _catalogFolders)
            {
                if (query.Length == 0 || Path.GetFileName(path).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    _filteredCatalogFolders.Add(path);
            }
            foreach (string path in _catalogPaths)
            {
                if (query.Length == 0 || Path.GetFileNameWithoutExtension(path).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    _filteredCatalogPaths.Add(path);
            }
            _catalogScroll = Vector2.zero;
            Repaint();
        }

        private void SetCatalogFolder(DefaultAsset folder)
        {
            string path = folder != null ? AssetDatabase.GetAssetPath(folder) : "Assets";
            if (!AssetDatabase.IsValidFolder(path) || (path != "Assets" && !path.StartsWith("Assets/", StringComparison.Ordinal)))
            {
                SetNotice("Project 창의 Assets 안에 있는 폴더를 지정하세요.", MessageType.Warning);
                return;
            }
            catalogFolder = folder;
            catalogSearch = "";
            RefreshCatalog();
        }

        private string GetCatalogPath()
        {
            string path = catalogFolder != null ? AssetDatabase.GetAssetPath(catalogFolder) : "Assets";
            if (AssetDatabase.IsValidFolder(path)) return path;
            path = AssetDatabase.IsValidFolder(MapCatalogPath) ? MapCatalogPath : "Assets";
            catalogFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            return path;
        }

        private void OpenCatalogFolder(string path)
        {
            SetCatalogFolder(AssetDatabase.LoadAssetAtPath<DefaultAsset>(path));
        }

        private float CatalogContentHeight => _catalogPanel.contentRect.height - 28;

        private void DrawCatalog()
        {
            if (_catalogPanel == null) return;
            int previousTab = showHelpCatalog ? 5 : showVolumeCatalog ? 4 : showBakeCatalog ? 3 : showLightCatalog ? 2 : showCameraCatalog ? 1 : 0;
            int tab = GUI.Toolbar(new Rect(4, 3, CatalogWidth - 8, 22), previousTab,
                new[] { "에셋", "카메라", "라이트", "베이크", "볼륨", "도움말" });
            showCameraCatalog = tab == 1;
            showLightCatalog = tab == 2;
            showBakeCatalog = tab == 3;
            showVolumeCatalog = tab == 4;
            showHelpCatalog = tab == 5;
            if (tab != previousTab)
            {
                Repaint();
                GUIUtility.ExitGUI();
            }
            using (new GUI.GroupScope(new Rect(0, 28, CatalogWidth, CatalogContentHeight)))
            {
                if (showHelpCatalog)
                {
                    DrawHelpCatalog();
                }
                else
                {
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
                    {
                        if (showBakeCatalog) DrawBakeCatalog();
                        else using (new EditorGUI.DisabledScope(FbxPlacementBaker.IsRunning))
                        {
                            if (showVolumeCatalog) DrawVolumeCatalog();
                            else if (showCameraCatalog) DrawCameraCatalog();
                            else if (showLightCatalog) DrawLightCatalog();
                            else DrawAssetCatalog();
                        }
                    }
                }
            }
        }

        private void DrawAssetCatalog()
        {
            if (_catalogPanel == null) return;
            float width = CatalogWidth - 16;
            GUI.Label(new Rect(8, 6, width, 22), "배치할 FBX · 프리팹", EditorStyles.boldLabel);
            GUI.Label(new Rect(8, 29, width, 18), "폴더를 열어 배치할 에셋을 찾으세요", EditorStyles.miniLabel);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 32;
            EditorGUI.BeginChangeCheck();
            DefaultAsset folder = (DefaultAsset)EditorGUI.ObjectField(new Rect(8, 52, width, 18), "폴더", catalogFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck()) SetCatalogFolder(folder);
            EditorGUIUtility.labelWidth = oldLabelWidth;
            if (GUI.Button(new Rect(8, 75, 103, 20), "맵 폴더")) OpenCatalogFolder(MapCatalogPath);
            if (GUI.Button(new Rect(117, 75, 105, 20), "선택 폴더"))
            {
                DefaultAsset chosen = Selection.activeObject as DefaultAsset;
                if (chosen != null) SetCatalogFolder(chosen);
                else SetNotice("Project 창에서 에셋이 있는 폴더를 먼저 선택하세요.", MessageType.Info);
            }
            EditorGUI.BeginChangeCheck();
            catalogSearch = EditorGUI.TextField(new Rect(8, 101, width, 20), catalogSearch, EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck()) FilterCatalog();
            string currentPath = GetCatalogPath();
            using (new EditorGUI.DisabledScope(currentPath == "Assets"))
            {
                if (GUI.Button(new Rect(8, 126, 45, 20), new GUIContent("상위", "상위 폴더 열기")))
                    OpenCatalogFolder(Path.GetDirectoryName(currentPath)?.Replace('\\', '/'));
            }
            GUI.Label(new Rect(59, 126, width - 51, 20), new GUIContent(Path.GetFileName(currentPath), currentPath), EditorStyles.miniLabel);
            GUI.Label(new Rect(8, 150, width, 18), "폴더 " + _filteredCatalogFolders.Count + "개 · 에셋 " + _filteredCatalogPaths.Count + "개", EditorStyles.miniLabel);

            float height = Mathf.Max(1, CatalogContentHeight - 204);
            Rect list = new Rect(4, 172, CatalogWidth - 8, height);
            int rowCount = _filteredCatalogFolders.Count + _filteredCatalogPaths.Count;
            Rect content = new Rect(0, 0, list.width - 16, Mathf.Max(height, rowCount * CatalogRowHeight));
            int dragId = GUIUtility.GetControlID("FbxCatalogDrag".GetHashCode(), FocusType.Passive);
            _catalogScroll = GUI.BeginScrollView(list, _catalogScroll, content);
            try
            {
                int first = Mathf.Max(0, Mathf.FloorToInt(_catalogScroll.y / CatalogRowHeight));
                int last = Mathf.Min(rowCount, first + Mathf.CeilToInt(height / CatalogRowHeight) + 1);
                for (int i = first; i < last; i++)
                {
                    Rect row = new Rect(0, i * CatalogRowHeight, content.width, CatalogRowHeight - 2);
                    if (i < _filteredCatalogFolders.Count) DrawCatalogFolderRow(row, _filteredCatalogFolders[i]);
                    else DrawCatalogRow(row, _filteredCatalogPaths[i - _filteredCatalogFolders.Count], dragId);
                }
                if (rowCount == 0)
                    GUI.Label(new Rect(8, 8, content.width - 16, 60), string.IsNullOrWhiteSpace(catalogSearch)
                        ? "이 폴더에는 하위 폴더나 FBX·프리팹이 없습니다."
                        : "현재 폴더에 검색 결과가 없습니다.", EditorStyles.wordWrappedMiniLabel);
                HandleCatalogDrag(dragId);
            }
            finally { GUI.EndScrollView(); }
            GUI.Label(new Rect(8, CatalogContentHeight - 26, width, 22), "에셋: 더블클릭 배치 · 드래그 배치", EditorStyles.miniLabel);
        }

        private void DrawCatalogFolderRow(Rect rect, string path)
        {
            if (GUI.Button(rect, new GUIContent("", path), GUIStyle.none))
            {
                OpenCatalogFolder(path);
                GUIUtility.ExitGUI();
            }
            if (Event.current.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, new Color(.23f, .23f, .23f));
            Texture icon = EditorGUIUtility.IconContent("Folder Icon").image;
            if (icon != null) GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 9, 34, 34), icon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(rect.x + 51, rect.y + 5, rect.width - 55, 20), new GUIContent(Path.GetFileName(path), path), EditorStyles.boldLabel);
            GUI.Label(new Rect(rect.x + 51, rect.y + 27, rect.width - 55, 18), "클릭하여 폴더 열기", EditorStyles.miniLabel);
        }

        private void DrawCatalogRow(Rect rect, string path, int dragId)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) return;
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                fbx = model;
                if (e.clickCount == 2)
                {
                    _dragAssetPath = null;
                    GUIUtility.hotControl = 0;
                    Run(() => Add(model, placementPosition));
                }
                else
                {
                    _dragAssetPath = path;
                    _dragStart = e.mousePosition;
                    GUIUtility.hotControl = dragId;
                }
                e.Use();
                Repaint();
            }
            if (e.type != EventType.Repaint) return;
            EditorGUI.DrawRect(rect, fbx == model ? new Color(.17f, .32f, .47f) : new Color(.20f, .20f, .20f));
            Texture icon = AssetPreview.GetAssetPreview(model) ?? AssetPreview.GetMiniThumbnail(model);
            if (icon != null) GUI.DrawTexture(new Rect(rect.x + 4, rect.y + 5, 42, 42), icon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(rect.x + 51, rect.y + 5, rect.width - 55, 20), new GUIContent(model.name, path), EditorStyles.label);
            string parentFolder = Path.GetFileName(Path.GetDirectoryName(path));
            GUI.Label(new Rect(rect.x + 51, rect.y + 27, rect.width - 55, 18), new GUIContent(parentFolder, path), EditorStyles.miniLabel);
            if (AssetPreview.IsLoadingAssetPreview(model.GetInstanceID())) Repaint();
        }

        private void HandleCatalogDrag(int dragId)
        {
            Event e = Event.current;
            if (GUIUtility.hotControl != dragId) return;
            if (e.type == EventType.MouseDrag && !string.IsNullOrEmpty(_dragAssetPath) && (e.mousePosition - _dragStart).sqrMagnitude > 36)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(_dragAssetPath);
                if (model != null)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { model };
                    DragAndDrop.paths = new[] { _dragAssetPath };
                    DragAndDrop.StartDrag(model.name);
                }
                GUIUtility.hotControl = 0;
                _dragAssetPath = null;
                e.Use();
            }
            else if (e.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                _dragAssetPath = null;
                e.Use();
            }
        }

        private void DrawHelpCatalog()
        {
            if (_catalogPanel == null) return;

            Rect area = new Rect(8, 6, CatalogWidth - 16, Mathf.Max(1, CatalogContentHeight - 12));
            using (new GUILayout.AreaScope(area))
            using (var scroll = new EditorGUILayout.ScrollViewScope(_helpScroll))
            {
                _helpScroll = scroll.scrollPosition;

                GUILayout.Label("맵 작업 도우미", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("이 창은 FBX·프리팹 배치, 조명, 베이크와 후처리 Volume을 한곳에서 관리합니다. 처음에는 아래 순서대로 진행하세요.", MessageType.Info);

                DrawHelpStep("1. 작업 영역 준비", "새 맵을 만들거나 기존 작업 영역을 연 뒤, 왼쪽 에셋 탭에서 FBX·프리팹이 있는 폴더를 선택합니다.");
                DrawHelpStep("2. 에셋 배치", "FBX·일반 프리팹·Variant를 더블클릭하거나 작업 화면으로 드래그하세요. 배치본은 중첩 프리팹까지 Unpack합니다. 원본은 유지되며 원본 변경이 배치본에 자동 반영되지 않습니다. W·E·R로 이동·회전·크기를 조절합니다.");
                DrawHelpStep("3. 화면 넓이 조절", "왼쪽 카탈로그와 오른쪽 작업 패널 사이의 얇은 경계선을 드래그하면 각 작업 영역의 폭을 조절할 수 있습니다. 설정한 폭은 창을 닫아도 유지됩니다.");

                GUILayout.Space(4);
                GUILayout.Label("머티리얼 입히기", EditorStyles.boldLabel);
                DrawHelpStep("부분별로 변경", "배치한 FBX를 선택한 뒤 작업 패널의 '머티리얼 입히기'를 펼치세요. 각 메시의 슬롯에 Project 창의 머티리얼을 끌어 놓거나 동그란 선택 버튼으로 지정하면 바로 적용됩니다. '자식 메시에도 적용'을 켜면 FBX 내부 메시도 표시됩니다.");
                DrawHelpStep("여러 메시를 한 번에 변경", "오브젝트를 여러 개 선택한 뒤 '일괄 머티리얼'을 지정하고 '선택 범위의 모든 슬롯에 입히기'를 누르세요. 선택한 범위의 모든 슬롯이 같은 머티리얼로 바뀝니다. Ctrl+Z로 되돌릴 수 있고 맵을 저장하면 프리팹에도 유지됩니다. 베이크한 맵은 재질을 바꾼 뒤 다시 베이크하세요.");

                GUILayout.Space(4);
                GUILayout.Label("버추얼 카메라", EditorStyles.boldLabel);
                DrawHelpStep("카메라 설치", "카메라 탭에서 Cinemachine 버추얼 카메라를 설치합니다. '현재 작업 화면으로 설치'를 누르면 지금 보고 있는 Scene 화면의 위치·방향·렌즈를 그대로 가져옵니다. 카메라를 선택하면 오른쪽에 해당 카메라 화면이 표시됩니다.");
                DrawHelpStep("프리뷰 크기 조절", "카메라 프리뷰 창 오른쪽 아래의 '크기 조절 ↘' 모서리를 드래그하면 너비와 높이를 바꿀 수 있습니다. 작업 화면 안에서 조절되며 설정한 크기는 유지됩니다.");
                DrawHelpStep("실행 카메라와의 관계", "이 카메라는 화면을 직접 그리지 않습니다. 게임 씬의 Output Camera에 있는 Cinemachine Brain이 선택해 보여 줍니다. 카메라 이름은 실행 중 함께 열린 다른 씬과도 겹치지 않게 정하세요.");
                DrawHelpStep("카메라별 후처리", "각 버추얼 카메라에는 Cinemachine Volume 설정이 붙습니다. 맵에 Volume 오브젝트를 만들지 않고, 카메라 설정에서 Volume Profile만 지정합니다. 해당 카메라가 활성일 때 그 후처리가 적용됩니다.");

                GUILayout.Space(4);
                GUILayout.Label("라이트와 베이크", EditorStyles.boldLabel);
                DrawHelpStep("라이트 추가", "라이트 탭에서 Directional, Point, Spot 라이트를 추가하고 색과 세기를 조절합니다. '씬 조명'을 켜면 편집 화면에서 추가한 라이트를 바로 확인합니다.");
                DrawHelpStep("베이크 전 확인", "고정 배경·건물처럼 움직이지 않는 오브젝트를 Static으로 설정하고, 라이트의 Mode를 Baked 또는 Mixed로 설정하세요. 동적 캐릭터와 이펙트는 베이크 대상이 아닙니다.");
                DrawHelpStep("베이크 결과 보기", "베이크 탭에서 실행한 뒤, 상단의 '베이크 완성본'을 누르면 격자를 숨기고 결과를 확인합니다. 다시 배치하려면 '배치 편집으로'를 누르세요.");

                GUILayout.Space(4);
                GUILayout.Label("후처리 Volume 가져오기", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("기존 씬의 후처리 설정을 이 맵에서 그대로 쓰려면, 볼륨 탭의 '기존 Volume' 칸에 Hierarchy의 Volume 컴포넌트를 끌어 놓고 '기존 Volume 가져오기'를 누르세요.", MessageType.None);
                DrawHelpStep("무엇이 복사되나요?", "Global/Local 여부, 우선순위, Blend Distance, Weight와 Volume Profile이 작업 영역으로 복사됩니다. 원본 Profile은 수정되지 않으므로 안전합니다.");
                DrawHelpStep("미리보기가 안 보일 때", "볼륨 탭의 '미리보기'를 켜고, Scene 뷰 상단의 Effects가 켜져 있는지 확인하세요. Local Volume은 카메라가 Collider 범위 안에 있어야 적용됩니다.");

                GUILayout.Space(4);
                GUILayout.Label("저장과 최종 연결", EditorStyles.boldLabel);
                DrawHelpStep("저장되는 파일", "저장 위치에는 맵 Prefab, Map Definition SO, 라이트와 Volume 정보가 함께 저장됩니다. 카탈로그가 사용할 Data 에셋은 DataBaseSO 쪽에 따로 저장됩니다.");
                DrawHelpStep("스테이지에 연결", "생성한 Map Definition SO를 ViewStage의 Map 항목에 연결한 뒤, ViewStage 테이블을 Catalog에 등록하면 스테이지 요청 시 화면용 정보를 조립해 사용할 수 있습니다.");

                GUILayout.Space(4);
                GUILayout.Label("자주 생기는 문제", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("• 에셋이 안 보임: 에셋 탭에서 올바른 폴더를 선택했는지 확인하세요.\n• 저장이 안 됨: 저장 위치와 에셋 이름이 비어 있지 않은지 확인하세요.\n• 베이크가 어두움: Directional Light가 있는지, 베이크 대상 오브젝트가 Static인지 확인하세요.", MessageType.Warning);
            }
        }

        private static void DrawHelpStep(string title, string description)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.Label(description, EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(7);
        }
    }
}
