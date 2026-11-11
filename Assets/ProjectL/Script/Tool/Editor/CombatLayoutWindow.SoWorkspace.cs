using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // SO 작업 영역의 조립과 보기 전환입니다. 값 편집·검사·저장은 기존 명령이 맡습니다.
    public sealed partial class CombatLayoutWindow
    {
        private enum SoWorkspace { Layout, Tiles, Settings }
        private enum SoInspectorSection { Selection, Stage, PlayerZone, References, Background, Gimmicks, Registration, RandomTerrain }

        private static readonly string[] SoWorkspaceLabels = { "배치", "바닥 타일", "스테이지 설정" };
        private static readonly string[] SoPlacementInspectorLabels = { "선택", "아군 구역", "무작위 지형" };
        private static readonly SoInspectorSection[] SoPlacementInspectorSections =
            { SoInspectorSection.Selection, SoInspectorSection.PlayerZone, SoInspectorSection.RandomTerrain };
        private const string SoGuidePath = "Assets/ProjectL/Script/Tool/SO모드 사용법.md";

        // 표시 상태는 작업본·초안·Undo에 넣지 않습니다. 지도 보기의 원본은 기존 soMapView입니다.
        private bool _soShowStageSettings;
        private bool _soShowStages, _soShowCreation;
        private int _soLastPlacementView = SoView2D;
        private SoInspectorSection _soInspectorSection = SoInspectorSection.Selection;
        private Vector2 _soLibraryScroll;
        private Vector2 _soSettingsScroll, _soCheckScroll;
        private bool _soShowCheckResult = true, _soShowRangeSettings = true;
        private object _soInspectedPlacement;
        private CombatStageSoCatalog _soPickerCatalog;
        private string[] _soStageLabels;

        private SoWorkspace GetSoWorkspace() => _soShowStageSettings ? SoWorkspace.Settings
            : soMapView == SoViewTiles ? SoWorkspace.Tiles : SoWorkspace.Layout;

        private void SetSoWorkspace(SoWorkspace next)
        {
            if (_soShowHelp) CloseSoHelp();
            if (GetSoWorkspace() == next && !_soShowStages && !_soShowCreation) return;
            CancelSoWorkspaceInput();
            _soShowStages = false; _soShowCreation = false;
            if (soMapView == SoView2D || soMapView == SoView3D) _soLastPlacementView = soMapView;
            _soShowStageSettings = next == SoWorkspace.Settings;
            if (next == SoWorkspace.Tiles) soMapView = SoViewTiles;
            else if (next == SoWorkspace.Layout) soMapView = _soLastPlacementView;
            NormalizeSoInspector();
            Repaint();
        }

        // 편집 값은 건드리지 않고 지도 보기만 바꿉니다.
        private void SetSoMapView(int view)
        {
            if (_soShowHelp) CloseSoHelp();
            if (!_soShowStageSettings && soMapView == view && !_soShowStages && !_soShowCreation) return;
            CancelSoWorkspaceInput();
            _soShowStages = false; _soShowCreation = false;
            if (soMapView == SoView2D || soMapView == SoView3D) _soLastPlacementView = soMapView;
            _soShowStageSettings = false;
            soMapView = view;
            if (view == SoView2D || view == SoView3D) _soLastPlacementView = view;
            NormalizeSoInspector();
            Repaint();
        }

        // 이미 칠한 값의 Undo 묶음을 닫고, 아직 확정하지 않은 이동·사각형만 취소합니다.
        private void CancelSoWorkspaceInput()
        {
            CancelSoGimmickShapes();
            EndStroke();
            CancelSoDrag();
            soTileBoxing = false;
            soTileCellBoxing = false;
            soTileLast = new Vector2Int(-1, -1);
            soTileCellLast = new Vector2Int(-1, -1);
            soPalettePressGuid = null;
            soPaletteDropCell = null;
            if (SoPaletteDragNow != null)
            {
                DragAndDrop.SetGenericData(SoPaletteDragKey, null);
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
            }
            GUIUtility.hotControl = 0;
            if (Event.current != null) GUI.FocusControl(null);
        }

        private void OpenSoInspector(SoInspectorSection section)
        {
            CancelSoGimmickShapes();
            _soGimmickJump = 0;
            bool selection = section == SoInspectorSection.Selection || section == SoInspectorSection.RandomTerrain;
            SetSoWorkspace(selection ? SoWorkspace.Layout : SoWorkspace.Settings);
            _soInspectorSection = section;
            soDetailsScroll = Vector2.zero;
            if (selection) showSoCell = true;
            Repaint();
        }

        private void NormalizeSoInspector()
        {
            bool placement = _soInspectorSection == SoInspectorSection.Selection || _soInspectorSection == SoInspectorSection.RandomTerrain;
            if (GetSoWorkspace() == SoWorkspace.Settings && placement) _soInspectorSection = SoInspectorSection.Stage;
            else if (GetSoWorkspace() == SoWorkspace.Layout && !placement && _soInspectorSection != SoInspectorSection.PlayerZone)
                _soInspectorSection = SoInspectorSection.Selection;
        }

        // 포인터 처리 중에는 표시 대상만 바꿉니다. 진행 중인 맵 입력을 취소하지 않습니다.
        private void ShowSoPlacementProperties()
        {
            _soInspectorSection = soTool == SoTool.PlayerZone ? SoInspectorSection.PlayerZone : SoInspectorSection.Selection;
        }

        private void DrawSoWorkspaceHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("스테이지", GUILayout.Width(60));
                using (new EditorGUI.DisabledScope(soCatalog == null)) DrawSoStagePicker();
                if (GUILayout.Button(_soShowStages ? "목록 닫기" : "전체 목록", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    CancelSoWorkspaceInput();
                    _soShowStages = !_soShowStages;
                    _soShowCreation = false;
                    Repaint();
                    GUIUtility.ExitGUI();
                }
                if (GUILayout.Button("새로 만들기 / 복제", EditorStyles.toolbarButton, GUILayout.Width(135)))
                {
                    CancelSoWorkspaceInput();
                    _soShowCreation = !_soShowCreation;
                    _soShowStages = false;
                    Repaint();
                    GUIUtility.ExitGUI();
                }
            }
        }

        private void DrawSoStagePicker()
        {
            if (soCatalog == null)
            {
                EditorGUILayout.LabelField("목록 새로고침이 필요합니다.");
                return;
            }
            if (_soPickerCatalog != soCatalog)
            {
                _soPickerCatalog = soCatalog;
                _soStageLabels = new string[soCatalog.Stages.Count + 1];
                _soStageLabels[0] = "스테이지 선택";
                for (int i = 0; i < soCatalog.Stages.Count; i++)
                {
                    var entry = soCatalog.Stages[i];
                    _soStageLabels[i + 1] = $"{entry.Root.name} · 키 {entry.StageKey} · 맵 {entry.MapKey} · {entry.MapRole} · {(entry.Registered ? "등록" : "미등록")}";
                }
            }
            int index = soCatalog.Stages.FindIndex(entry => entry.RootGuid == soDocument.rootGuid) + 1;
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup(index, _soStageLabels, GUILayout.MinWidth(160));
            if (!EditorGUI.EndChangeCheck() || picked == 0) return;
            CancelSoWorkspaceInput();
            OpenSoStage(soCatalog.Stages[picked - 1]);
            GUIUtility.ExitGUI();
        }

        private void DrawSoWorkspaceTabs()
        {
            EditorGUI.BeginChangeCheck();
            int selected = _soShowStages || _soShowCreation ? -1 : (int)GetSoWorkspace();
            int next = GUILayout.Toolbar(selected, SoWorkspaceLabels);
            if (!EditorGUI.EndChangeCheck()) return;
            SetSoWorkspace((SoWorkspace)next);
            GUIUtility.ExitGUI();
        }

        private void DrawSoWorkspaceBody()
        {
            // 목록·생성은 지도 위에 길게 쌓지 않고 같은 본문 영역을 사용합니다.
            if (_soShowStages || _soShowCreation)
            {
                if (GUILayout.Button("목록·생성 닫고 편집으로", GUILayout.Width(190)))
                {
                    CancelSoWorkspaceInput();
                    _soShowStages = false; _soShowCreation = false;
                    Repaint();
                    GUIUtility.ExitGUI();
                }
                using (var scroll = new EditorGUILayout.ScrollViewScope(_soLibraryScroll))
                {
                    _soLibraryScroll = scroll.scrollPosition;
                    DrawSoWorkspaceCheckResult();
                    if (_soShowCreation) DrawSoCreate();
                    else DrawSoStageList();
                }
                return;
            }
            // 기존 패널 폭·분할 경계와 맵 최소 폭을 유지합니다.
            soLeftWidth = Mathf.Clamp(soLeftWidth, SoMinLeftWidth, Mathf.Max(SoMinLeftWidth, position.width - soRightWidth - SoMinMapWidth));
            soRightWidth = Mathf.Clamp(soRightWidth, SoMinRightWidth, Mathf.Max(SoMinRightWidth, position.width - soLeftWidth - SoMinMapWidth));
            if (GetSoWorkspace() == SoWorkspace.Settings)
            {
                DrawSoWorkspaceSettings();
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(soLeftWidth)))
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(soSidebarScroll))
                    {
                        soSidebarScroll = scroll.scrollPosition;
                        if (GetSoWorkspace() == SoWorkspace.Layout) DrawSoSidebar();
                        else { DrawSoTilePalette(); DrawSoTileWorkspaceLinks(); }
                    }
                }
                soLeftWidth = DrawSoSplitter(soLeftWidth, 1);
                using (new EditorGUILayout.VerticalScope()) DrawSoWorkspaceMap();
                soRightWidth = DrawSoSplitter(soRightWidth, -1);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(soRightWidth))) DrawSoDetails();
            }
        }

        private void DrawSoWorkspaceMap()
        {
            try { DrawSoMap(); }
            catch (ExitGUIException) { throw; }
            catch (System.Exception exception)
            {
                EditorGUILayout.HelpBox("지도 표시 오류: " + exception.Message + "\n스테이지 설정에서 필드·참조·배경을 확인하세요.", MessageType.Error);
            }
        }

        private void DrawSoTileWorkspaceLinks()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("배치 복사와 타일 복사는 따로 유지됩니다.", EditorStyles.wordWrappedMiniLabel);
            if (GUILayout.Button("배경·타일 설정 열기"))
            {
                OpenSoInspector(SoInspectorSection.Background);
                GUIUtility.ExitGUI();
            }
            GUILayout.Space(8);
            EditorGUILayout.LabelField("맵에서 타일 회전", EditorStyles.boldLabel);
            if (GUILayout.Button("2D에서 타일 회전")) OpenSoTileRotationView(SoView2D);
            if (GUILayout.Button("3D에서 타일 회전")) OpenSoTileRotationView(SoView3D);
        }

        private void OpenSoTileRotationView(int view)
        {
            SetSoMapView(view);
            soTool = SoTool.TileRotate;
            ShowSoPlacementProperties();
            GUIUtility.ExitGUI();
        }

        private void DrawSoWorkspaceSettings()
        {
            // 지도를 그리지 않는 동안에도 검사 위치 이동은 현재 필드를 사용합니다.
            soField = null; soOccupants = null; soTileBand = null;
            if (soDocument.IsOpen)
            {
                try { soField = SoFieldOf(); }
                catch (System.Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(soLeftWidth)))
                {
                    using (var scroll = new EditorGUILayout.ScrollViewScope(_soSettingsScroll))
                    {
                        _soSettingsScroll = scroll.scrollPosition;
                        EditorGUILayout.LabelField("설정 분류", EditorStyles.boldLabel);
                        DrawSoSettingsLink("기본 정보", SoInspectorSection.Stage);
                        DrawSoSettingsLink("필드·아군 구역", SoInspectorSection.PlayerZone);
                        DrawSoSettingsLink("공유 SO·본부·구조물", SoInspectorSection.References);
                        DrawSoSettingsLink("배경·타일·카메라", SoInspectorSection.Background);
                        DrawSoSettingsLink("기믹", SoInspectorSection.Gimmicks);
                        DrawSoSettingsLink("등록·맵 생성 규칙", SoInspectorSection.Registration);
                    }
                }
                soLeftWidth = DrawSoSplitter(soLeftWidth, 1);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) DrawSoDetails();
            }
        }

        private void DrawSoSettingsLink(string label, SoInspectorSection section)
        {
            Color previous = GUI.backgroundColor;
            if (_soInspectorSection == section) GUI.backgroundColor = SelectedButton;
            bool clicked = GUILayout.Button(label);
            GUI.backgroundColor = previous;
            if (!clicked) return;
            OpenSoInspector(section);
            GUIUtility.ExitGUI();
        }

        private void DrawSoDetails()
        {
            SoWorkspace workspace = GetSoWorkspace();
            EditorGUILayout.LabelField(workspace == SoWorkspace.Settings ? "스테이지 설정"
                : workspace == SoWorkspace.Tiles ? "타일 구성·선택 정보" : "배치 속성", EditorStyles.boldLabel);
            if (!soDocument.IsOpen)
            {
                EditorGUILayout.HelpBox("열린 스테이지가 없습니다.", MessageType.Info);
                return;
            }
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = workspace == SoWorkspace.Settings ? 210 : 110;
            try
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(soDetailsScroll))
                {
                    soDetailsScroll = scroll.scrollPosition;
                    DrawSoWorkspaceCheckResult();
                    DrawSoVisibilitySummary();
                    try
                    {
                        if (workspace == SoWorkspace.Settings) DrawSoSettingsContent();
                        else if (workspace == SoWorkspace.Tiles) DrawSoTileInspector();
                        else DrawSoPlacementInspector();
                    }
                    catch (ExitGUIException) { throw; }
                    catch (System.Exception exception)
                    {
                        EditorGUILayout.HelpBox("속성 표시 오류: " + exception.Message, MessageType.Error);
                        DrawSoSettingsLink("필드 설정 열기", SoInspectorSection.PlayerZone);
                        DrawSoSettingsLink("공유 SO·본부·구조물 열기", SoInspectorSection.References);
                        DrawSoSettingsLink("배경 설정 열기", SoInspectorSection.Background);
                    }
                }
            }
            finally { EditorGUIUtility.labelWidth = previousLabelWidth; }
        }

        private void DrawSoSettingsContent()
        {
            switch (_soInspectorSection)
            {
                case SoInspectorSection.PlayerZone:
                    DrawSoFieldReference();
                    DrawSoPlayerZone();
                    break;
                case SoInspectorSection.References: DrawSoReferences(); break;
                case SoInspectorSection.Background: DrawSoView(); break;
                case SoInspectorSection.Gimmicks: DrawSoGimmicks(); break;
                case SoInspectorSection.Registration: DrawSoRegistration(); break;
                default: DrawSoStageValues(); break;
            }
        }

        private void DrawSoPlacementInspector()
        {
            int index = System.Array.IndexOf(SoPlacementInspectorSections, _soInspectorSection);
            EditorGUI.BeginChangeCheck();
            int next = GUILayout.Toolbar(Mathf.Max(0, index), SoPlacementInspectorLabels);
            if (EditorGUI.EndChangeCheck())
            {
                CancelSoWorkspaceInput();
                _soInspectorSection = SoPlacementInspectorSections[next];
                showSoCell = true;
                soDetailsScroll = Vector2.zero;
                Repaint(); GUIUtility.ExitGUI();
            }
            GUILayout.Space(6);
            if (_soInspectorSection == SoInspectorSection.PlayerZone)
            {
                DrawSoSettingsLink("필드 참조·크기 설정", SoInspectorSection.PlayerZone);
                DrawSoPlayerZone();
            }
            else if (_soInspectorSection == SoInspectorSection.RandomTerrain) DrawSoRandomTerrain();
            else
            {
                showSoCell = EditorGUILayout.Foldout(showSoCell, "선택한 칸·배치", true);
                if (showSoCell) DrawSoCell();
            }
            GUILayout.Space(8);
            DrawSoSettingsLink("스테이지 설정 열기", SoInspectorSection.Stage);
        }

        // 한 검사 결과를 각 영역의 스크롤 가능한 속성/목록 상단에서 공유합니다. 결과·개정은 SoUx가 소유합니다.
        private void DrawSoWorkspaceCheckResult()
        {
            if (!SoCurrentCheck) return;
            _soShowCheckResult = EditorGUILayout.Foldout(_soShowCheckResult, $"검사 결과 · 오류 {soCheck.Issues.Count} / 경고 {soCheck.Warnings.Count}", true);
            if (!_soShowCheckResult) return;
            float height = soCheck.Issues.Count == 0 && soCheck.Warnings.Count == 0 ? 70 : Mathf.Clamp(position.height * .18f, 80, 140);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_soCheckScroll, GUILayout.Height(height)))
            {
                _soCheckScroll = scroll.scrollPosition;
                // 목록·생성에는 지도/설정 본문이 없으므로 현재 필드를 여기서 읽습니다.
                if (_soShowStages || _soShowCreation)
                {
                    soField = null;
                    try { soField = SoFieldOf(); }
                    catch (System.Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
                }
                DrawSoCheckResult();
            }
        }

        private void OpenSoUsageGuide()
        {
            CancelSoWorkspaceInput();
            var guide = AssetDatabase.LoadMainAssetAtPath(SoGuidePath);
            if (guide == null || !AssetDatabase.OpenAsset(guide))
                SetSoNotice("사용법을 열지 못했습니다. Project 창에서 Tool/SO모드 사용법.md를 확인하세요.", MessageType.Warning);
        }
    }
}
