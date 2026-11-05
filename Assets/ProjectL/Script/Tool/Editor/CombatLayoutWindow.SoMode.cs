using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    internal enum CombatLayoutEditMode { Dt, So }

    // SO 모드 화면입니다(설계 SM1). DT 모드 코드와 섞지 않고, 기존 파일에는 머리줄 전환·OnGUI 분기·Undo 분기만 둡니다.
    // 편집은 작업본에서 하고, 저장은 복제본 검사를 통과할 때만 에셋에 씁니다(설계 SM2).
    public sealed partial class CombatLayoutWindow
    {
        // 메인 캐릭터 도구는 없습니다. 전투가 메인 캐릭터 자리를 읽지 않기 때문입니다(추가 계획 P-A1).
        // 창의 soTool은 숫자로 저장되므로 새 도구는 끝에 더합니다(아군 구역, 계획 P-AZT4).
        // TileRotate는 끝에 더했습니다(옛 순번 그대로, 계획 P-TTT9).
        // 새 도구는 끝에 붙입니다(설계 §23.3). 하붑 시작·낙석은 설계 §26입니다.
        private enum SoTool { Select, EnemyUnit, Structure, Headquarters, Terrain, Obstacle, Object, Rail, Erase, PlayerZone, TileRotate, HaboobStart, Rockfall }

        private static readonly string[] EditModes = { "DT", "SO" };
        private static readonly string[] SoToolLabels = { "선택", "적 유닛", "구조물", "적 본부", "지형", "장애물", "오브젝트", "레일", "지우기", "아군 구역", "타일 회전", "하붑 시작", "낙석" };
        private static readonly string[] SoZoneModes = { "칠하기", "지우기" };
        // 적 배치 구역은 없습니다(Core `5a1bfbe1f`). 칸 바탕은 아군 구역과 나머지(적 배치 가능) 둘입니다(설계 §11 AZ3).
        private static readonly Color PlayerZone = new Color(.17f, .24f, .33f), NeutralZone = new Color(.2f, .22f, .24f);
        private static readonly Color RailColor = new Color(.5f, .4f, .22f), TerrainColor = new Color(.24f, .45f, .3f), ObjectColor = new Color(.62f, .55f, .2f);
        private static readonly Color ObstacleColor = new Color(.45f, .45f, .45f), StructureColor = new Color(.55f, .22f, .25f);
        private static readonly Color EnemyUnitColor = new Color(.8f, .35f, .35f);
        private static readonly Color SelectedButton = new Color(.5f, .85f, .8f);
        private static readonly Color BoxColor = new Color(.35f, .8f, 1f), GhostValid = new Color(.3f, 1f, .45f), GhostInvalid = new Color(1f, .3f, .3f);
        private static readonly (string Label, Color Color)[] SoLegend =
        {
            ("레일", RailColor), ("지형", TerrainColor), ("오브젝트", ObjectColor),
            ("장애물", ObstacleColor), ("구조물·본부", StructureColor), ("적 유닛", EnemyUnitColor),
        };
        [SerializeField] private CombatLayoutEditMode editMode;
        [SerializeField] private CombatStageSoDocument soDocument = new CombatStageSoDocument();
        [SerializeField] private bool soModified;
        [SerializeField] private SoTool soTool;
        // 아군 구역 도구가 지우기인지입니다. 기본은 칠하기입니다(설계 §11 AZ4).
        [SerializeField] private bool soZoneErase;
        [SerializeField] private string soUnitBrush = "", soStructureBrush = "", soTerrainBrush = "", soObstacleBrush = "", soObjectBrush = "";
        [SerializeField] private Vector2Int soSelectedCell = new Vector2Int(-1, -1);
        [SerializeField] private bool showSoPalette = true, showSoCell = true;
        [SerializeField] private string soTemplateGuid = "", soNewName = "";
        [SerializeField] private int soNewKey;
        [SerializeField] private float soLeftWidth = 230, soRightWidth = 310;
        [SerializeField] private RectInt soBox;
        [SerializeField] private bool soHasBox;
        // 끌기 상태는 창에 남기지 않습니다. 도메인 리로드나 Undo로 끌던 배치가 사라지면 끌기를 그만둡니다.
        private SoDragMode soDragMode;
        private Vector2Int soDragStart, soDragCurrent;
        private object soDragItem;
        private bool soDragGroup;
        // 묶음 끌기 고스트 색은 거리가 바뀔 때만 복제본으로 다시 확인합니다.
        private Vector2Int? soGroupGhostOffset;
        private bool soGroupGhostValid;
        [SerializeField] private CombatStageSoClip soClip = new CombatStageSoClip();
        // 공격 범위 보기: 0 끄기, 1 고른 적, 2 모든 적 겹쳐 보기. 방향 0은 합친 모양입니다(추가 설계 AD2).
        [SerializeField] private int soRangeMode, soRangeDirection;
        private readonly Dictionary<(string, int), CombatStageSoRangeShape> soRangeShapes = new Dictionary<(string, int), CombatStageSoRangeShape>();
        private static readonly string[] SoRangeModes = { "끄기", "고른 적", "모든 적 겹쳐 보기" };
        private static readonly string[] SoViews = { "2D", "3D" };
        // 맵 보기: 2D, 3D(추가 설계 AD1), 바닥 타일(설계 §12 UT4). 미리보기와 외형 표는 창에 남기지 않고 필요할 때 만듭니다.
        // 옛 bool soThreeD를 대신합니다(계획 P-UTT1). 바꾼 뒤 처음 열 때는 2D로 시작합니다.
        private const int SoView2D = 0, SoView3D = 1, SoViewTiles = 2;
        [SerializeField] private int soMapView;
        // 표시 설정은 작업본·Undo에 저장하지 않습니다.
        private bool soPlayerOnly;
        private bool SoPlayerFilter => soMapView == SoView3D && soPlayerOnly;
        private bool SoShown(object item) => CombatStageSoVisuals.TryIsShown(item, SoPlayerFilter, soCatalog.Names.owner, out _);

        private CombatStageSoPreview soPreview;
        private CombatStageSoVisuals soVisuals;
        private bool soPreviewNeedsReset;
        private static readonly Color ReachColor = new Color(1f, .55f, .1f), SplashColor = new Color(1f, .8f, .5f);
        // 지정한 하붑 시작 위치의 구름 칸 색입니다(설계 §25 HAT2).
        private static readonly Color HaboobColor = new Color(.78f, .82f, .9f);

        // Paint는 칠하는 도구의 Shift+끌기 사각형입니다(추가 설계 AD3).
        private enum SoDragMode { None, Pending, Box, Move, Paint }
        private readonly HashSet<string> soExpanded = new HashSet<string>();
        private readonly Dictionary<string, SerializedObject> soShared = new Dictionary<string, SerializedObject>();
        private readonly Dictionary<string, SoWorkingCopy> soSharedCopies = new Dictionary<string, SoWorkingCopy>();
        [SerializeField] private bool showSoNewField;
        [SerializeField] private int soFieldColumns, soFieldRows, soFieldReserve, soFieldPlayerDepth;

        // 이 스테이지만 쓰는 공유 SO를 고칠 때 쓰는 저장하지 않는 복제본입니다. Json은 마지막으로 맞춘 작업본 값입니다.
        private sealed class SoWorkingCopy
        {
            public Object Copy;
            public string Json;
        }
        private CombatStageSoCatalog soCatalog;
        private bool soCatalogFailed, soDraftChecked;
        private CombatStageSoField soField;
        // 맵을 그릴 때마다 작업본 칸 목록과 soField로 다시 만듭니다(설계 §11 AZ2). 2D·3D·범례가 같이 씁니다.
        private CombatStageSoZone soZone;
        private Dictionary<Vector2Int, List<SoOccupant>> soOccupants;
        private string soNotice = "";
        private MessageType soNoticeType = MessageType.Info;
        private Vector2 soSidebarScroll, soGridScroll, soDetailsScroll;

        private string SoDraftKey => DraftKey + ".So";

        // 맵 한 칸에 그리는 배치 하나입니다. 위 층(Layer 값이 큰 것)이 칸 색을 정합니다.
        private readonly struct SoOccupant
        {
            public readonly string Label;
            public readonly Color Color;

            public SoOccupant(string label, Color color)
            {
                Label = label; Color = color;
            }
        }

        #region 머리줄 · 수명

        private void DrawEditModeToggle()
        {
            HandleSoGimmickShapeCancellation();
            // 이전 판에서 복원된 창은 새 필드가 비어 있을 수 있습니다.
            if (soDocument == null) soDocument = new CombatStageSoDocument();
            EditorGUI.BeginChangeCheck();
            int mode = GUILayout.Toolbar((int)editMode, EditModes, EditorStyles.toolbarButton, GUILayout.Width(80));
            if (!EditorGUI.EndChangeCheck()) return;
            if (editMode == CombatLayoutEditMode.So) CancelSoWorkspaceInput();
            else { EndStroke(); GUI.FocusControl(null); }
            InvalidateSoCheck();
            editMode = (CombatLayoutEditMode)mode;
        }

        private void DrawSoHeaderButtons()
        {
            if (GUILayout.Button("목록 새로고침", EditorStyles.toolbarButton)) { RefreshSoCatalog(); GUIUtility.ExitGUI(); }
            using (new EditorGUI.DisabledScope(!soDocument.IsOpen))
            {
                if (GUILayout.Button("에셋에서 다시 읽기", EditorStyles.toolbarButton)) { ReloadSoFromAssets(); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("검사", EditorStyles.toolbarButton)) { CheckSoStage(); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("저장", EditorStyles.toolbarButton)) { SaveSoStage(); GUIUtility.ExitGUI(); }
            }
            if (GUILayout.Button("사용법", EditorStyles.toolbarButton)) { OpenSoUsageGuide(); GUIUtility.ExitGUI(); }
        }

        private void DrawSoHeaderStatus()
        {
            DrawSoWorkspaceHeader();
            DrawSoWorkStatus();
            if (!string.IsNullOrEmpty(soNotice)) EditorGUILayout.HelpBox(soNotice, soNoticeType);
            if (soPasteVisibilityBlocked) DrawSoPasteVisibilityRecovery();
        }

        private void DrawSoMode()
        {
            if (soCatalog == null && !soCatalogFailed) RefreshSoCatalog();
            if (!soDraftChecked) { soDraftChecked = true; RestoreSoDraft(); }
            DrawSoWorkspaceTabs();
            if (soCatalog == null)
            {
                EditorGUILayout.HelpBox("에셋 목록이 없습니다. 위 알림을 확인하고 목록 새로고침을 눌러 주세요.", MessageType.Warning);
                return;
            }
            if (soDocument.IsOpen && soDocument.format != CombatStageSoDocument.CurrentFormat) ReopenSoOldFormat();
            DrawSoWorkspaceBody();
            HandleSoKeys();
            if (Event.current.rawType == EventType.MouseUp || Event.current.type == EventType.Ignore) EndStroke();
        }

        private const float SoMinLeftWidth = 160, SoMinRightWidth = 220, SoMinMapWidth = 300;

        // 패널 사이 경계입니다. 끌면 패널 너비가 바뀝니다. 오른쪽 패널은 경계를 왼쪽으로 끌 때 넓어지므로 direction이 -1입니다.
        private float DrawSoSplitter(float width, int direction)
        {
            Rect rect = GUILayoutUtility.GetRect(6, 6, GUILayout.Width(6), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(new Rect(rect.x + 2, rect.y, 2, rect.height), new Color(0f, 0f, 0f, .35f));
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            switch (e.GetTypeForControl(id))
            {
                case EventType.MouseDown when e.button == 0 && rect.Contains(e.mousePosition):
                    GUIUtility.hotControl = id;
                    e.Use();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == id:
                    width += e.delta.x * direction;
                    e.Use(); Repaint();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    e.Use();
                    break;
            }
            return width;
        }

        private void OnSoUndoRedo()
        {
            ResetSoGimmickShapeViews();
            InvalidateSoCheck(changed: true);
            soModified = true; SaveSoDraft(); Repaint();
        }

        private void SetSoNotice(string message, MessageType type = MessageType.Info)
        {
            soPasteVisibilityBlocked = false;
            soNotice = message; soNoticeType = type; Repaint();
        }

        // 작업본만 바꿉니다. 창 Undo로 되돌리고 초안을 남깁니다(설계 SM11).
        private void SoChange(string label, Action action)
        {
            CancelSoGimmickShapes();
            Undo.RecordObject(this, label);
            action(); InvalidateSoCheck(changed: true); soModified = true; EditorUtility.SetDirty(this); SaveSoDraft(); Repaint();
        }

        private void SaveSoDraft() => EditorPrefs.SetString(SoDraftKey, JsonUtility.ToJson(soDocument));

        private void RestoreSoDraft()
        {
            if (soDocument.IsOpen) return;
            string draft = EditorPrefs.GetString(SoDraftKey, "");
            if (string.IsNullOrEmpty(draft)) return;
            try
            {
                var restored = JsonUtility.FromJson<CombatStageSoDocument>(draft);
                if (restored == null || !restored.IsOpen) return;
                // 옛 형식 초안은 새 칸(바닥 타일 등)이 빈 채라 저장하면 에셋 값을 지웁니다. 복원하지 않고 버립니다(설계 P-UT6).
                if (restored.format != CombatStageSoDocument.CurrentFormat)
                {
                    EditorPrefs.DeleteKey(SoDraftKey);
                    SetSoNotice("도구가 바뀌기 전에 남은 SO 편집 초안이라 복원하지 않았습니다.", MessageType.Warning);
                    return;
                }
                soDocument = restored; InvalidateSoCheck(changed: true); soModified = true;
                SetSoNotice("저장하지 않은 마지막 SO 편집 초안을 복원했습니다.");
            }
            catch (Exception exception) { SetSoNotice("SO 초안을 복원하지 못했습니다: " + exception.Message, MessageType.Warning); }
        }

        // 도구가 바뀌기 전에 창에 남은 작업본은 새 칸이 빈 채라 저장하면 에셋 값을 지웁니다(설계 P-UT6).
        // 저장하지 않은 편집은 버리고 같은 스테이지를 에셋에서 다시 읽습니다. 찾지 못하면 닫습니다.
        private void ReopenSoOldFormat()
        {
            InvalidateSoCheck(reset: true);
            var entry = soCatalog.Stages.Find(stage => stage.RootGuid == soDocument.rootGuid);
            soModified = false; EditorPrefs.DeleteKey(SoDraftKey);
            if (entry == null)
            {
                soDocument = new CombatStageSoDocument();
                SetSoNotice("도구가 바뀌어 열려 있던 스테이지를 닫았습니다. 목록에서 다시 여세요.", MessageType.Warning);
                return;
            }
            OpenSoStage(entry);
            if (soDocument.format == CombatStageSoDocument.CurrentFormat)
            {
                SetSoNotice($"도구가 바뀌어 {entry.Root.name}을(를) 에셋에서 다시 읽었습니다. 저장하지 않은 편집은 버렸습니다.", MessageType.Warning);
                return;
            }
            // 다시 읽지 못했으면 OpenSoStage가 남긴 문구를 두고 옛 작업본만 닫습니다.
            soDocument = new CombatStageSoDocument();
        }

        private bool AllowSoReplace() =>
            !soModified || EditorUtility.DisplayDialog("저장하지 않은 SO 편집", "저장하지 않은 변경이 있습니다. 버리고 계속할까요?", "버리기", "취소");

        private void RefreshSoCatalog()
        {
            CancelSoGimmickShapes();
            InvalidateSoCheck();
            soShared.Clear();
            soRangeShapes.Clear();
            soTileKinds.Clear();
            soVisuals = null;
            // 같은 GUID 에셋의 바뀐 내용도 3D에 반영하도록 다음 그리기에서 맵·배치를 다시 만듭니다. 시점·작업본·Undo는 그대로입니다(설계 P-UT17).
            soPreview?.Invalidate();
            ClearSharedCopies();
            try
            {
                soCatalog = CombatStageSoCatalog.Build();
                soCatalogFailed = false;
                // 예전에 고른 붓이 적이 될 수 없는 유닛(아군 유닛)이면 비웁니다. 그 정의로는 놓지 않습니다.
                if (soCatalog.EnemyUnits.IndexOf(soUnitBrush) < 0) soUnitBrush = "";
            }
            catch (Exception exception)
            {
                soCatalog = null; soCatalogFailed = true;
                SetSoNotice("에셋 목록을 읽지 못했습니다: " + exception.Message, MessageType.Error);
            }
        }

        private void ReloadSoFromAssets()
        {
            if (!AllowSoReplace()) return;
            RefreshSoCatalog();
            var entry = soCatalog?.Stages.Find(stage => stage.RootGuid == soDocument.rootGuid);
            if (entry == null)
            {
                SetSoNotice("열린 스테이지의 Stage Root를 찾을 수 없습니다.", MessageType.Error);
                return;
            }
            soModified = false;
            OpenSoStage(entry);
        }

        private void OpenSoStage(CombatStageSoEntry entry)
        {
            CancelSoGimmickShapes();
            if (!AllowSoReplace()) return;
            var views = soCatalog.ViewsFor(entry.StageKey);
            // 짝 View는 같은 스테이지 키로 찾고, 둘 이상이면 열지 않습니다(계획 P12).
            if (views.Count > 1)
            {
                SetSoNotice($"스테이지 키 {entry.StageKey}의 View가 {views.Count}개입니다({string.Join(", ", views.Select(view => view.name))}). 하나만 남겨 주세요.", MessageType.Error);
                return;
            }
            try
            {
                Object pair = views.Count == 1 ? views[0] : null;
                var opened = CombatStageSoAssets.Read(entry.Root, pair, soCatalog.Names);
                CancelSoWorkspaceInput();
                soDocument = opened;
                _soInspectedPlacement = null;
                _soShowStages = false; _soShowCreation = false;
                ClearSharedCopies(); soFieldColumns = 0;
                soHasBox = false;
                soPreviewNeedsReset = true;
                soModified = false; EditorPrefs.DeleteKey(SoDraftKey);
                soSelectedCell = new Vector2Int(-1, -1); soOccupants = null;
                InvalidateSoCheck(reset: true);
                if (pair == null) SetSoNotice($"짝 View(스테이지 키 {entry.StageKey})가 없습니다: {entry.Root.name}", MessageType.Warning);
                else if (soDocument.HasLegacyPlacements) SetSoNotice($"숨김 호환 벽·지형이 있습니다. SO 모드는 이 값을 편집하지 않고 그대로 둡니다: {entry.Root.name}", MessageType.Warning);
                else SetSoNotice($"스테이지를 열었습니다: {entry.Root.name}");
            }
            catch (Exception exception) { SetSoNotice(exception.Message, MessageType.Error); }
        }

        private HashSet<int> OtherStageKeys() =>
            new HashSet<int>(soCatalog.RootKeys.Where(pair => pair.Key != soDocument.rootGuid).Select(pair => pair.Value));

        // 저장·검사·등록이 함께 쓰는 검사입니다. SO 검사(복제본) 위에, 도구가 바로 막는 맵 밖(필드를 줄였을 때)과
        // "고친 공유 SO는 이 스테이지만 써야 한다"(계획 P4)를 더합니다.
        private List<CombatStageSoIssue> SoProblems(Object root, Object combat, Object view)
        {
            var problems = CombatStageSoValidation.CheckDocumentIssues(soDocument, root, combat, view, OtherStageKeys());
            foreach (var edit in soDocument.ActiveSharedEdits())
            {
                int others = soCatalog.OtherUsers(edit.guid, soDocument.combatGuid, soDocument.viewGuid);
                if (others > 0)
                    problems.Add(new CombatStageSoIssue($"다른 스테이지 {others}개도 쓰는 공유 SO는 고칠 수 없습니다: {CombatStageSoAssets.NameOf(edit.guid)}. 전용 복제 뒤 고치세요."));
            }
            CombatStageSoField field;
            try { field = SoFieldOf(); }
            catch (Exception exception)
            {
                problems.Add(new CombatStageSoIssue(exception.Message));
                return problems;
            }
            if (field == null) return problems;
            var grid = new CombatStageSoGrid(field.Columns, field.Rows, soCatalog.Footprint, soCatalog.Names.IsQuarterTurn);
            foreach (var placed in soDocument.Placements(grid))
            {
                var outside = placed.Cells.Where(cell => !grid.Contains(cell)).ToList();
                if (outside.Count > 0)
                    problems.Add(new CombatStageSoIssue($"맵 밖 배치: {CombatStageSoDocument.LayerName(placed.Layer)} ({outside[0].x}, {outside[0].y})",
                        outside[0], CombatStageSoIssueTarget.Of(soDocument, placed.Item)));
            }
            // 토네이도는 칸 하나로 놓이지만 게임에서는 중심 기준 정사각형이라, 맵 밖 검사와 따로 봅니다(토네이도 View 계획 Te.5).
            foreach (var terrain in soDocument.terrains.Where(marker => CombatStageSoAssets.IsTornado(marker.definitionGuid)))
            {
                string tornadoProblem = CombatStageSoAssets.TornadoProblem(terrain, field.Columns, field.Rows);
                if (tornadoProblem != null)
                    problems.Add(new CombatStageSoIssue(tornadoProblem, terrain.randomizeInitialPosition ? (Vector2Int?)null : new Vector2Int(terrain.x, terrain.y),
                        CombatStageSoIssueTarget.Of(soDocument, terrain)));
            }
            // 하붑 시작 위치는 Core가 전투 진입 때에야 판 밖을 막아(HaboobGimmickRule.FixedCells) 도구가 미리 막습니다(설계 §25 HAT3).
            // "모든 칸이 판 안"은 Core AnchorLimit과 같은 뜻입니다(모양의 각 축 최솟값 0은 Core 계약이 보장).
            var haboobCells = soDocument.haboob.StartCells();
            if (haboobCells != null && haboobCells.Any(cell => !grid.Contains(cell)))
            {
                Vector2Int anchor = soDocument.haboob.initialAnchor;
                problems.Add(new CombatStageSoIssue($"하붑 시작 위치 ({anchor.x}, {anchor.y})에서는 구름이 {field.Columns}×{field.Rows} 판 밖으로 나갑니다. 시작 위치를 안쪽으로 옮겨 주세요.", anchor));
            }
            // 아군 배치 구역 칸도 봅니다(설계 §11 AZ7). 최종 판정은 SO 검사(GetData → Core PlayerDeploymentZone)이고 여기는 칸을 바로 보이려는 것입니다.
            foreach (var cell in soDocument.playerDeploymentCells.Where(cell => !grid.Contains(cell)).Distinct())
                problems.Add(new CombatStageSoIssue($"맵 밖 아군 구역 칸 ({cell.x}, {cell.y})", cell));
            return problems;
        }

        private void CheckSoStage()
        {
            CancelSoGimmickShapes();
            Object root = CombatStageSoAssets.Load(soDocument.rootGuid);
            Object combat = CombatStageSoAssets.Load(soDocument.combatGuid);
            Object view = CombatStageSoAssets.Load(soDocument.viewGuid);
            SetSoCheck(SoProblems(root, combat, view));
            AddSoRockfallPropWarnings(view);
            // 공통 검사에 이어 맵 규칙의 오류/조건부 경고까지 표시합니다. 확인 창은 띄우지 않습니다.
            try
            {
                var next = SoChapterSaveChange(root);
                if (next != null)
                {
                    var (judgement, swap) = JudgeSoChapter(next);
                    RecordSoChapterCheck(judgement, swap);
                }
                else soCheck.Complete();
            }
            catch (Exception exception) { AddSoCheckError("맵 생성 규칙을 읽지 못했습니다: " + exception.Message); }
            SetSoNotice(SoCheckStatus() + ". '검사 결과'를 확인하세요.", SoCheckMessageType());
        }

        // 복제본 검사를 통과할 때만 에셋 셋에 씁니다. 실패하면 아무것도 쓰지 않습니다(설계 SM2).
        private void SaveSoStage()
        {
            CancelSoGimmickShapes();
            Object root = CombatStageSoAssets.Load(soDocument.rootGuid);
            Object combat = CombatStageSoAssets.Load(soDocument.combatGuid);
            Object view = CombatStageSoAssets.Load(soDocument.viewGuid);
            if (view == null)
            {
                SetSoCheck(new List<CombatStageSoIssue> { new CombatStageSoIssue("짝 View가 없어 저장할 수 없습니다.") });
                SetSoNotice("짝 View가 없어 저장할 수 없습니다. 같은 스테이지 키의 View Stage Data가 필요합니다.", MessageType.Error);
                return;
            }
            SetSoCheck(SoProblems(root, combat, view));
            AddSoRockfallPropWarnings(view);
            if (soCheck.Issues.Count > 0)
            {
                SetSoNotice($"검사에서 문제 {soCheck.Issues.Count}건을 찾아 저장하지 않았습니다. '검사 결과'를 확인하세요.", MessageType.Error);
                return;
            }
            // 표에 든 스테이지의 맵 키·역할이 바뀌면 저장 뒤 표 상태로 맵 생성 규칙을 봅니다(병합 대응 설계 MG7·MG8, Q-MG4 A).
            CombatStageSoChapterStage swap = null;
            CombatStageSoChapterStage next;
            try { next = SoChapterSaveChange(root); }
            catch (Exception exception)
            {
                AddSoCheckError("맵 생성 규칙을 읽지 못했습니다: " + exception.Message);
                SetSoNotice("맵 생성 규칙을 읽지 못해 저장하지 않았습니다: " + exception.Message, MessageType.Error);
                return;
            }
            if (next != null && !ConfirmSoChapter("저장", next, null, null, out swap)) return;
            if (next == null) soCheck.Complete();
            try
            {
                // 고친 공유 SO 값을 먼저 쓰고, 그다음 스테이지 셋을 씁니다(T17).
                foreach (var edit in soDocument.ActiveSharedEdits())
                {
                    Object shared = CombatStageSoAssets.Load(edit.guid);
                    EditorJsonUtility.FromJsonOverwrite(edit.json, shared);
                    EditorUtility.SetDirty(shared);
                    AssetDatabase.SaveAssetIfDirty(shared);
                }
                if (soDocument.sharedEdits.Count > 0)
                {
                    soDocument.sharedEdits.Clear();
                    InvalidateSoCheck(changed: true);
                }
                CombatStageSoAssets.Write(soDocument, root, combat, view);
                foreach (Object asset in new[] { root, combat, view })
                {
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                }
            }
            catch (Exception exception)
            {
                SetSoNotice("저장하지 못했습니다: " + exception.Message, MessageType.Error);
                return;
            }
            soModified = false; EditorPrefs.DeleteKey(SoDraftKey);
            // 바닥 타일 맞춤 문제는 저장을 막지 않고 저장 뒤 알림에 붙입니다(설계 Q-UT2 A, 계획 Tb.6).
            string tileNote = SoTileFitSummary();
            // 보스 교체는 스테이지를 쓴 뒤 표를 고칩니다. 스테이지 쓰기가 실패하면 표는 그대로입니다(계획 P-MG5).
            string swapError = swap != null ? UnregisterSoBoss(swap) : null;
            RefreshSoCatalog();
            if (swapError != null)
                SetSoNotice($"저장했지만 기존 보스 {swap.Label}를 표에서 빼지 못했습니다(맵에 보스가 둘인 상태): {swapError}", MessageType.Error);
            else if (swap != null) SetSoNotice($"저장했고, 기존 보스 {swap.Label}를 표에서 뺐습니다: {root.name}" + tileNote, tileNote == null ? MessageType.Info : MessageType.Warning);
            else SetSoNotice($"저장했습니다: {root.name}" + tileNote, tileNote == null ? MessageType.Info : MessageType.Warning);
        }

        #endregion

        #region 왼쪽 목록

        private void DrawSoSidebar()
        {
            DrawSoGimmickLinks();
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                showSoPalette = EditorGUILayout.Foldout(showSoPalette, "정의 목록", true, EditorStyles.foldoutHeader);
                if (!showSoPalette) return;
                EditorGUILayout.LabelField("누르면 그 정의를 놓는 도구가 됩니다.", EditorStyles.miniLabel);
                DrawSoPalette("적 유닛", soCatalog.EnemyUnits, SoTool.EnemyUnit, ref soUnitBrush);
                DrawSoPlayerUnits();
                DrawSoPalette("구조물", soCatalog.Structures, SoTool.Structure, ref soStructureBrush);
                DrawSoPalette("지형", soCatalog.Terrains, SoTool.Terrain, ref soTerrainBrush);
                DrawSoPalette("장애물", soCatalog.Obstacles, SoTool.Obstacle, ref soObstacleBrush);
                DrawSoPalette("오브젝트", soCatalog.Objects, SoTool.Object, ref soObjectBrush);
            }
        }

        private void DrawSoStageList()
        {
            if (soCatalog.Stages.Count == 0)
            {
                EditorGUILayout.LabelField("전투 스테이지가 없습니다.", EditorStyles.miniLabel);
                return;
            }
            EditorGUILayout.LabelField("누르면 그 스테이지를 엽니다.", EditorStyles.miniLabel);
            foreach (var stage in soCatalog.Stages)
            {
                Color previous = GUI.backgroundColor;
                if (stage.RootGuid == soDocument.rootGuid) GUI.backgroundColor = SelectedButton;
                string state = stage.Registered ? "" : " · 미등록";
                bool clicked = GUILayout.Button($"{stage.Root.name}\n키 {stage.StageKey} · 맵 {stage.MapKey} · {stage.MapRole}{state}", GUILayout.Height(34));
                GUI.backgroundColor = previous;
                if (!clicked) continue;
                OpenSoStage(stage);
                GUIUtility.ExitGUI();
            }
        }

        // 틀 스테이지에서 새 셋을 만듭니다(설계 SM4, 계획 P2·P3). 만든 셋은 표에 등록하지 않습니다(설계 SM8).
        private void DrawSoCreate()
        {
            var template = SoTemplate();
            if (template == null)
            {
                EditorGUILayout.LabelField("틀로 쓸 전투 스테이지가 없습니다.", EditorStyles.miniLabel);
                return;
            }
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 70;
            try
            {
                int index = soCatalog.Stages.IndexOf(template);
                EditorGUI.BeginChangeCheck();
                int next = EditorGUILayout.Popup("틀", index, soCatalog.Stages.Select(stage => $"{stage.Root.name} ({stage.StageKey})").ToArray());
                if (EditorGUI.EndChangeCheck() && next != index)
                {
                    template = soCatalog.Stages[next];
                    soTemplateGuid = template.RootGuid; soNewKey = 0;
                }
                if (soNewKey <= 0) soNewKey = ProposeSoKey(template);
                soNewName = EditorGUILayout.TextField("이름", soNewName);
                using (new EditorGUILayout.HorizontalScope())
                {
                    soNewKey = EditorGUILayout.IntField("스테이지 키", soNewKey);
                    if (GUILayout.Button("제안", GUILayout.Width(40))) soNewKey = ProposeSoKey(template);
                }
                string fileName = string.IsNullOrWhiteSpace(soNewName) ? "<이름>" : soNewName.Trim();
                EditorGUILayout.LabelField($"만들 파일: Stage_{fileName} · Combat_{fileName} · StageViewProp_{fileName}", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("배치 비우고 만들기")) { CreateSoStage(template, false); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("배치까지 복제")) { CreateSoStage(template, true); GUIUtility.ExitGUI(); }
                EditorGUILayout.LabelField("참조·View 값은 틀에서 이어받습니다. 새 스테이지는 표에 등록되지 않은 채 만들어집니다.", EditorStyles.wordWrappedMiniLabel);
            }
            finally { EditorGUIUtility.labelWidth = previousLabelWidth; }
        }

        // 틀은 고른 스테이지, 없으면 열린 스테이지, 그것도 없으면 등록된 첫 Regular 전투 스테이지입니다(계획 P2).
        private CombatStageSoEntry SoTemplate()
        {
            var stages = soCatalog.Stages;
            return stages.Find(stage => stage.RootGuid == soTemplateGuid)
                ?? stages.Find(stage => stage.RootGuid == soDocument.rootGuid)
                ?? stages.Find(stage => stage.Registered && stage.MapRole == "Regular")
                ?? stages.FirstOrDefault();
        }

        private int ProposeSoKey(CombatStageSoEntry template) =>
            CombatStageSoCreation.ProposeStageKey(template.StageKey, new HashSet<int>(soCatalog.RootKeys.Values));

        private void CreateSoStage(CombatStageSoEntry template, bool copyPlacements)
        {
            CancelSoGimmickShapes();
            if (!AllowSoReplace()) return;
            var views = soCatalog.ViewsFor(template.StageKey);
            var problems = new List<string>();
            if (views.Count > 1) problems.Add($"틀 스테이지 키 {template.StageKey}의 View가 {views.Count}개입니다. 하나만 남겨 주세요.");
            Object root = problems.Count > 0 ? null : CombatStageSoCreation.Create(template.Root, template.Combat, views.Count == 1 ? views[0] : null,
                soNewName, soNewKey, copyPlacements, new HashSet<int>(soCatalog.RootKeys.Values), problems);
            if (root == null)
            {
                SetSoNotice("새 스테이지를 만들지 못했습니다:\n" + string.Join("\n", problems), MessageType.Error);
                return;
            }
            soModified = false; soNewName = ""; soNewKey = 0;
            RefreshSoCatalog();
            var entry = soCatalog?.Stages.Find(stage => stage.Root == root);
            if (entry != null) OpenSoStage(entry);
            SetSoNotice($"새 스테이지를 만들었습니다(표 미등록): {root.name}");
        }

        // 표 등록·해제는 저장한 에셋 기준이라, 저장하지 않은 변경이 있으면 먼저 저장을 요청합니다(설계 SM8).
        private void SetSoRegistered(bool registered)
        {
            CancelSoGimmickShapes();
            if (soModified)
            {
                SetSoNotice("저장하지 않은 변경이 있습니다. 먼저 저장해 주세요.", MessageType.Warning);
                return;
            }
            Object root = CombatStageSoAssets.Load(soDocument.rootGuid);
            Object combat = CombatStageSoAssets.Load(soDocument.combatGuid);
            Object view = CombatStageSoAssets.Load(soDocument.viewGuid);
            if (root == null)
            {
                SetSoCheck(new List<CombatStageSoIssue> { new CombatStageSoIssue("Stage Root 에셋을 찾을 수 없습니다.") });
                SetSoNotice("Stage Root 에셋을 찾을 수 없습니다.", MessageType.Error);
                return;
            }
            string confirm;
            CombatStageSoChapterStage next = null;
            if (registered)
            {
                SetSoCheck(SoProblems(root, combat, view));
                if (soCheck.Issues.Count > 0)
                {
                    SetSoNotice($"검사에서 문제 {soCheck.Issues.Count}건을 찾아 등록하지 않았습니다. '검사 결과'를 확인하세요.", MessageType.Error);
                    return;
                }
                string role = CombatStageSoAssets.NameAt(soCatalog.Names.mapRole, soDocument.mapRole);
                confirm = $"맵 키 {soDocument.mapKey}의 {role} 스테이지로 Run 후보에 들어갑니다. 등록할까요?";
                try { next = CombatStageSoAssets.ReadChapterStage(root); }
                catch (Exception exception)
                {
                    AddSoCheckError("맵 생성 규칙 입력을 읽지 못했습니다: " + exception.Message);
                    SetSoNotice("등록하지 않았습니다: " + exception.Message, MessageType.Error);
                    return;
                }
            }
            else
            {
                // 해제는 공통 저장 검사를 실행하지 않습니다. 과거 통과 결과를 해제 검사로 재사용하지 않습니다.
                InvalidateSoCheck();
                confirm = "Run 후보에서 빠지고, 개발 진입(PlacedStage)으로도 열 수 없게 됩니다. 뺄까요?";
            }
            // 등록·빼기 뒤의 표 상태로 맵 생성 규칙을 봅니다(병합 대응 설계 MG7·MG8).
            if (!ConfirmSoChapter(registered ? "표에 등록" : "표에서 빼기", next, confirm, registered ? "등록" : "빼기", out var swap, recordCheck: registered)) return;
            string error = swap != null ? UnregisterSoBoss(swap) : null;
            if (error == null) error = CombatStageSoCreation.SetRegistered(root, view, registered);
            RefreshSoCatalog();
            if (error != null)
            {
                SetSoNotice(error, MessageType.Error);
                return;
            }
            SetSoNotice(!registered ? "Root 표와 View 표에서 뺐습니다."
                : swap != null ? $"기존 보스 {swap.Label}를 표에서 빼고, Root 표와 View 표에 등록했습니다."
                : "Root 표와 View 표에 등록했습니다.");
        }

        // 맵 생성 규칙 판정을 새로 읽어 합니다(병합 대응 설계 MG7, MG-P7). next가 null이면 이 Root를 빼는 변경입니다.
        // 보스가 둘이 되는데 기존 보스를 함께 빼면 풀리면, 교체한 판정과 그 보스를 냅니다(Q-MG3 A).
        private (CombatStageSoChapterJudgement Judgement, CombatStageSoChapterStage Swap) JudgeSoChapter(CombatStageSoChapterStage next)
        {
            var stages = CombatStageSoAssets.ReadChapterStages();
            var maps = CombatStageSoAssets.ReadMapRules();
            var judgement = CombatStageSoChapter.Judge(stages, maps, soDocument.rootGuid, next);
            if (judgement.SwapBoss == null) return (judgement, null);
            var swapped = CombatStageSoChapter.Judge(stages, maps, soDocument.rootGuid, next, judgement.SwapBoss.RootGuid);
            return swapped.Breaks ? (judgement, null) : (swapped, judgement.SwapBoss);
        }

        // 맵 생성 규칙을 보고 진행해도 되면 참입니다(Q-MG3 A). 이번 변경이 맵을 깨뜨리면 막고, 보스 교체로 풀리면 교체를 묻고,
        // 전부터 깨진 맵이면 경고 창 뒤에 진행합니다. confirm이 있으면(등록·빼기) 통과일 때도 바뀌는 수와 함께 확인 창을 띄웁니다.
        private bool ConfirmSoChapter(string title, CombatStageSoChapterStage next, string confirm, string ok, out CombatStageSoChapterStage swap, bool recordCheck = true)
        {
            swap = null;
            CombatStageSoChapterJudgement judgement;
            try { (judgement, swap) = JudgeSoChapter(next); }
            catch (Exception exception)
            {
                if (recordCheck) AddSoCheckError("맵 생성 규칙을 읽지 못했습니다: " + exception.Message);
                SetSoNotice($"{title}: 맵 생성 규칙을 읽지 못해 진행하지 않았습니다: {exception.Message}", MessageType.Error);
                swap = null;
                return false;
            }
            if (recordCheck) RecordSoChapterCheck(judgement, swap);
            string brokenNote = judgement.AlreadyBroken ? "\n\n이 맵은 이번 변경 전부터 Run 생성이 멈추는 상태입니다(이번 변경이 원인은 아님)." : "";
            if (swap != null)
            {
                string text = $"맵 {next.MapKey} 보스는 이미 {swap.Label}입니다. 보스는 맵마다 하나입니다.\n\n" +
                              $"기존 보스({swap.Label})를 표에서 빼고 진행하면:\n{judgement.Describe()}{brokenNote}";
                if (EditorUtility.DisplayDialog(title + " — 보스 교체", text, "기존 보스를 빼고 진행", "취소")) return true;
                SetSoNotice($"{title}을(를) 취소했습니다. 변경을 실행하지 않았습니다.");
                swap = null;
                return false;
            }
            if (judgement.Breaks)
            {
                SetSoNotice($"{title}: 맵 생성 규칙에 걸려 진행하지 않았습니다.\n{judgement.Describe()}", MessageType.Error);
                return false;
            }
            if (confirm == null && !judgement.AlreadyBroken) return true;
            string lines = judgement.Describe();
            string message = (confirm ?? $"{title}할까요?") + (lines.Length > 0 ? "\n\n" + lines : "") + brokenNote;
            bool accepted = EditorUtility.DisplayDialog(judgement.AlreadyBroken ? title + " — 맵이 이미 멈추는 상태" : title, message, ok ?? "진행", "취소");
            if (!accepted) SetSoNotice($"{title}을(를) 취소했습니다. 변경을 실행하지 않았습니다.");
            return accepted;
        }

        // 보스 교체: 옛 보스의 Root와 등록된 짝 View를 표에서 뺍니다. 짝 View가 없으면 Root만 뺍니다(계획 P-MG4).
        private string UnregisterSoBoss(CombatStageSoChapterStage boss)
        {
            Object bossRoot = CombatStageSoAssets.Load(boss.RootGuid);
            Object bossView = soCatalog.ViewsFor(boss.StageKey).FirstOrDefault(found => soCatalog.RegisteredViews.Contains(found));
            return CombatStageSoCreation.SetRegistered(bossRoot, bossView, false);
        }

        // 저장이 표 상태를 바꾸는 경우의 바뀐 뒤 값입니다(Q-MG4 A). 표에 없거나 맵 키·역할이 그대로면 null입니다.
        private CombatStageSoChapterStage SoChapterSaveChange(Object root)
        {
            if (root == null || !CombatStageSoAssets.RegisteredItems(CombatStageSoAssets.RootTableType).Contains(root)) return null;
            var saved = CombatStageSoAssets.ReadChapterStage(root);
            bool boss = CombatStageSoAssets.IsBossRole(CombatStageSoAssets.NameAt(soCatalog.Names.mapRole, soDocument.mapRole));
            return saved.MapKey == soDocument.mapKey && saved.IsBoss == boss ? null : saved.With(soDocument.stageKey, soDocument.mapKey, boss);
        }

        // 짝 View가 없는 스테이지에 틀 스테이지의 View를 복사해 붙이고, 그 값만 작업본에 읽어 옵니다(계획 P12).
        private void CreateSoView(CombatStageSoEntry template)
        {
            var views = soCatalog.ViewsFor(template.StageKey);
            var problems = new List<string>();
            if (views.Count != 1) problems.Add($"틀 스테이지 키 {template.StageKey}의 View가 {views.Count}개입니다. 하나인 틀을 고르세요.");
            if (soCatalog.ViewsFor(soDocument.stageKey).Count > 0) problems.Add($"스테이지 키 {soDocument.stageKey}의 View가 이미 있습니다. 목록 새로고침 뒤 스테이지를 다시 여세요.");
            Object view = problems.Count > 0 ? null : CombatStageSoCreation.CreateView(CombatStageSoAssets.Load(soDocument.rootGuid), views[0], soDocument.stageKey, problems);
            if (view == null)
            {
                SetSoNotice("View를 만들지 못했습니다:\n" + string.Join("\n", problems), MessageType.Error);
                return;
            }
            SoChange("View 만들기", () => CombatStageSoAssets.ReadViewInto(view, soDocument, soCatalog.Names));
            RefreshSoCatalog();
            SetSoNotice($"View를 만들었습니다(표 미등록): {view.name}");
        }

        private void DrawSoPalette(string label, CombatStageSoChoices choices, SoTool tool, ref string brush)
        {
            EditorGUILayout.LabelField($"{label} ({choices.Count})", EditorStyles.boldLabel);
            for (int i = 0; i < choices.Count; i++)
            {
                Color previous = GUI.backgroundColor;
                if (soTool == tool && choices.Guids[i] == brush) GUI.backgroundColor = SelectedButton;
                var content = new GUIContent(choices.Labels[i + 1]);
                Rect rect = GUILayoutUtility.GetRect(content, EditorStyles.miniButton);
                // 버튼이 끌기 입력을 삼키므로 버튼보다 먼저 봅니다.
                HandleSoPaletteDrag(rect, tool, choices.Guids[i], content.text);
                bool clicked = GUI.Button(rect, content, EditorStyles.miniButton);
                GUI.backgroundColor = previous;
                if (!clicked) continue;
                EndStroke();
                brush = choices.Guids[i]; soTool = tool;
                ShowSoPlacementProperties();
            }
            GUILayout.Space(4);
        }

        // 정의 목록에서 맵으로 끌어 놓기입니다(설계 §20). 움직이지 않고 떼면 지금처럼 클릭(붓 고르기)입니다.
        private sealed class SoPaletteDrag
        {
            public SoTool Tool;
            public string Guid;
        }

        private const string SoPaletteDragKey = "CombatStageSoPaletteDrag";
        private const float SoPaletteDragDistance = 6;
        private string soPalettePressGuid;
        private Vector2 soPalettePressAt;
        // 끄는 동안 마우스 아래 칸입니다. 맵 밖이면 null이고 고스트를 그리지 않습니다.
        private Vector2Int? soPaletteDropCell;

        private static SoPaletteDrag SoPaletteDragNow => DragAndDrop.GetGenericData(SoPaletteDragKey) as SoPaletteDrag;

        private void HandleSoPaletteDrag(Rect rect, SoTool tool, string guid, string label)
        {
            Event e = Event.current;
            if (e.rawType == EventType.MouseUp) soPalettePressGuid = null;
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                soPalettePressGuid = guid;
                soPalettePressAt = e.mousePosition;
            }
            else if (e.type == EventType.MouseDrag && soPalettePressGuid == guid
                     && Vector2.Distance(soPalettePressAt, e.mousePosition) >= SoPaletteDragDistance)
            {
                soPalettePressGuid = null;
                // 버튼이 쥔 입력을 풀어 뗄 때 클릭으로 처리되지 않게 합니다.
                GUIUtility.hotControl = 0;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = Array.Empty<Object>();
                DragAndDrop.paths = Array.Empty<string>();
                DragAndDrop.SetGenericData(SoPaletteDragKey, new SoPaletteDrag { Tool = tool, Guid = guid });
                DragAndDrop.StartDrag(label);
                e.Use();
            }
        }

        // 놓는 판단은 클릭과 같은 HandleSoCell 한 곳입니다. 놓은 뒤 그 정의가 붓으로 남습니다.
        private void DropSoPalette(SoPaletteDrag drag, Vector2Int cell, CombatStageSoGrid grid)
        {
            GUI.FocusControl(null);
            EndStroke();
            switch (drag.Tool)
            {
                case SoTool.EnemyUnit: soUnitBrush = drag.Guid; break;
                case SoTool.Structure: soStructureBrush = drag.Guid; break;
                case SoTool.Terrain: soTerrainBrush = drag.Guid; break;
                case SoTool.Obstacle: soObstacleBrush = drag.Guid; break;
                case SoTool.Object: soObjectBrush = drag.Guid; break;
                // 기믹 항목은 붓이 없고 도구만 바꿉니다(설계 §26.6).
                case SoTool.HaboobStart:
                case SoTool.Rockfall: break;
                default: throw new InvalidOperationException($"정의 목록에서 끌 수 없는 도구입니다: {drag.Tool}");
            }
            soTool = drag.Tool;
            ShowSoPlacementProperties();
            HandleSoCell(cell, false, grid);
            EndStroke();
        }

        // 아군 유닛은 구분해 보이기만 합니다. 스테이지 데이터에 아군을 미리 놓는 칸이 없어 붓으로 고를 수 없습니다.
        private void DrawSoPlayerUnits()
        {
            EditorGUILayout.LabelField($"아군 유닛 ({soCatalog.PlayerUnits.Count})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("스테이지에 미리 놓을 수 없습니다. 전투에서 플레이어가 손패로 놓습니다.", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(true))
                for (int i = 0; i < soCatalog.PlayerUnits.Count; i++)
                    GUILayout.Label(soCatalog.PlayerUnits.Labels[i + 1], EditorStyles.miniButton);
            GUILayout.Space(4);
        }

        #endregion

        #region 가운데 맵

        private void DrawSoMap()
        {
            soField = null; soOccupants = null; soTileBand = null;
            if (!soDocument.IsOpen)
            {
                EditorGUILayout.HelpBox("상단 스테이지 선택 또는 전체 목록에서 전투 스테이지를 고르세요.", MessageType.Info);
                return;
            }
            // '타일' 보기는 배치 도구 줄 대신 타일 붓 줄을 씁니다(계획 P-UTT2).
            bool tileView = soMapView == SoViewTiles;
            if (!tileView)
            {
                EditorGUI.BeginChangeCheck();
                int columns = Mathf.Clamp(Mathf.FloorToInt((position.width - soLeftWidth - soRightWidth - 32) / 85), 3, 5);
                int selectedTool = GUILayout.SelectionGrid((int)soTool, SoToolLabels, columns);
                if (EditorGUI.EndChangeCheck())
                {
                    CancelSoWorkspaceInput(); soTool = (SoTool)selectedTool;
                    ShowSoPlacementProperties(); GUIUtility.ExitGUI();
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (!tileView)
                {
                    EditorGUI.BeginChangeCheck();
                    int view = GUILayout.Toolbar(soMapView, SoViews, GUILayout.Width(120));
                    // 같은 전환 경로에서 끌기·포커스를 정리하고 시점·선택은 보존합니다.
                    if (EditorGUI.EndChangeCheck()) { SetSoMapView(view); GUIUtility.ExitGUI(); }
                }
                using (new EditorGUI.DisabledScope(soMapView != SoView3D))
                    if (GUILayout.Button("시점 초기화", GUILayout.Width(80))) soPreviewNeedsReset = true;
                GUILayout.FlexibleSpace();
            }
            if (!tileView && soTool == SoTool.PlayerZone)
                soZoneErase = GUILayout.Toolbar(soZoneErase ? 1 : 0, SoZoneModes, GUILayout.Width(120)) == 1;
            if (!tileView && soTool == SoTool.HaboobStart) DrawSoHaboobToolBar();
            if (!tileView && soTool == SoTool.Rockfall) DrawSoRockfallToolBar();
            if (!tileView) EditorGUILayout.LabelField(SoToolHint(), EditorStyles.wordWrappedMiniLabel);
            try { soField = SoFieldOf(); }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
                return;
            }
            if (soField == null)
            {
                EditorGUILayout.HelpBox("필드 SO가 비어 있어 맵을 그릴 수 없습니다. 스테이지 설정의 '필드·아군 구역'에서 필드를 고르세요.", MessageType.Warning);
                return;
            }
            if (tileView)
            {
                DrawSoTileView();
                return;
            }
            var grid = SoGrid();
            soZone = SoZoneOf(soField);
            DrawSoBoxButtons();
            var reachCounts = new Dictionary<Vector2Int, int>();
            var splashCells = new HashSet<Vector2Int>();
            List<string> rangeProblems = DrawSoRange(grid, reachCounts, splashCells);
            soOccupants = BuildSoOccupants(grid, out var outside);
            // 2D 띠·고른 칸 줄이 같이 쓰는 타일 대응입니다(설계 §12 UT10, P-UT12). 오른쪽 패널은 맵 다음에 그려 같은 값을 씁니다.
            soTileBand = BuildSoTileBand();
            if (soTool == SoTool.TileRotate) DrawSoTileToolBar();
            DrawSoLegend(soZone);
            if (soDocument.haboob.StartCells() != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawSoSwatch(HaboobColor, "하붑 시작 구름(지정한 시작 위치)");
                    GUILayout.FlexibleSpace();
                }
            }
            if (soMapView == SoView2D) DrawSoTileBandLegend();
            if (soMapView == SoView3D) DrawSoMap3D(grid, reachCounts, splashCells);
            else DrawSoMap2D(grid, reachCounts, splashCells);
            if (outside.Count > 0) EditorGUILayout.HelpBox("맵 밖 배치: " + string.Join(", ", outside), MessageType.Warning);
            DrawSoTileFitMessages(soTileBand?.Fit);
            if (soRangeMode != 0)
            {
                EditorGUILayout.HelpBox("공격 범위는 특성·버프/디버프에 따른 사거리 보정을 반영하지 않은 기본 범위입니다. 벽·지형·고지대·모래폭풍·유닛 점유·대포 인접 금지는 반영하지 않습니다. 실제 범위는 전투에서 확인하세요. " +
                    "진한 주황 = 사거리, 옅은 주황 = 스플래시로만 닿는 칸, ×n = 닿는 적 수.", MessageType.Info);
                if (rangeProblems.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", rangeProblems), MessageType.Warning);
            }
            EditorGUILayout.HelpBox("바로 막는 것은 맵 밖과 같은 층 겹침뿐입니다. 환경 배치는 저장 검사가, 유닛·본부 칸 규칙은 전투 진입(PlacedStage)이 판정합니다.", MessageType.Info);
        }

        private void DrawSoMap2D(CombatStageSoGrid grid, Dictionary<Vector2Int, int> reachCounts, HashSet<Vector2Int> splashCells)
        {
            const float size = 40;
            using (var scroll = new EditorGUILayout.ScrollViewScope(soGridScroll, GUILayout.ExpandHeight(true)))
            {
                soGridScroll = scroll.scrollPosition;
                Rect map = GUILayoutUtility.GetRect(soField.Columns * size, soField.Rows * size, GUILayout.ExpandWidth(false));
                var haboobCells = SoHaboobStartCells();
                for (int y = 0; y < soField.Rows; y++)
                {
                    for (int x = 0; x < soField.Columns; x++)
                    {
                        var position = new Vector2Int(x, y);
                        Rect rect = SoCellRect(map, size, new RectInt(x, y, 1, 1));
                        soOccupants.TryGetValue(position, out var here);
                        Color color = here != null ? here[0].Color : ZoneColor(soZone, position);
                        // 범위는 칸 색 위에 덧칠합니다. 여러 적이 닿는 칸일수록 진합니다(추가 계획 P-A9).
                        if (reachCounts.TryGetValue(position, out int hits)) color = Color.Lerp(color, ReachColor, Mathf.Min(.85f, .45f + .12f * (hits - 1)));
                        else if (splashCells.Contains(position)) color = Color.Lerp(color, SplashColor, .35f);
                        if (haboobCells.Contains(position)) color = Color.Lerp(color, HaboobColor, .45f);
                        if (position == soSelectedCell) color = Color.Lerp(color, Color.white, .4f);
                        EditorGUI.DrawRect(rect, color);
                        GUI.Label(rect, here == null ? $"{x},{y}" : string.Join("\n", here.Take(3).Select(occupant => occupant.Label)), EditorStyles.whiteMiniLabel);
                        if (hits > 1) GUI.Label(new Rect(rect.xMax - 20, rect.yMax - 14, 20, 14), $"×{hits}", EditorStyles.whiteMiniLabel);
                        DrawSoTileBandCell(rect, position);
                    }
                }
                DrawSoOverlays(map, size, grid);
                HandleSoPointer(map, mouse => SoCellAt(map, size, mouse), mouse => SoClampedCellAt(map, size, mouse), grid);
            }
            ScrollSoGridToIssue(size);
        }

        // 맵 입력은 칸마다가 아니라 맵 영역 하나로 받습니다. 끄는 동안 맵 밖으로 나가도 놓을 때까지 이 창이 받게 hotControl을 잡습니다.
        // 2D·3D가 같이 씁니다. cellAt은 마우스 아래 칸(맵 밖이면 null), clampedCellAt은 끄는 중에 맵 안으로 붙인 칸입니다.
        private void HandleSoPointer(Rect area, Func<Vector2, Vector2Int?> cellAt, Func<Vector2, Vector2Int?> clampedCellAt, CombatStageSoGrid grid)
        {
            int control = GUIUtility.GetControlID(FocusType.Passive);
            Event e = Event.current;
            switch (e.GetTypeForControl(control))
            {
                case EventType.MouseDown when e.button == 0 && !e.alt && area.Contains(e.mousePosition) && cellAt(e.mousePosition) is Vector2Int down:
                    // 맵 선택으로 입력 칸 포커스를 해제해야 복사·붙여넣기 등 배치 단축키를 받을 수 있습니다.
                    GUI.FocusControl(null);
                    GUIUtility.hotControl = control;
                    ShowSoPlacementProperties();
                    // '타일 회전' 도구는 칸의 바닥 타일을 고르기만 합니다(설계 §13 TT9). 2D·3D가 이 한 곳을 지납니다.
                    if (soTool == SoTool.TileRotate) PressSoTileCell(down, e.control || e.command, e.shift);
                    else if (soTool == SoTool.Select) BeginSoDrag(down, grid, e.shift);
                    else if (e.shift && IsPaintTool(soTool)) BeginSoPaintBox(down);
                    else HandleSoCell(down, false, grid);
                    e.Use(); GUIUtility.ExitGUI();
                    break;
                case EventType.MouseDrag when GUIUtility.hotControl == control:
                    if (soTool == SoTool.TileRotate) DragSoTileCell(cellAt(e.mousePosition), clampedCellAt(e.mousePosition));
                    else if (soDragMode != SoDragMode.None)
                    {
                        if (clampedCellAt(e.mousePosition) is Vector2Int current) UpdateSoDrag(current);
                    }
                    else if (cellAt(e.mousePosition) is Vector2Int over) HandleSoCell(over, true, grid);
                    e.Use(); Repaint();
                    break;
                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    if (soTool == SoTool.TileRotate) ReleaseSoTileCell();
                    else if (soDragMode != SoDragMode.None) FinishSoDrag(grid);
                    EndStroke();
                    e.Use(); GUIUtility.ExitGUI();
                    break;
                case EventType.DragUpdated when SoPaletteDragNow != null:
                    soPaletteDropCell = area.Contains(e.mousePosition) ? cellAt(e.mousePosition) : null;
                    DragAndDrop.visualMode = soPaletteDropCell.HasValue ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                    e.Use(); Repaint();
                    break;
                case EventType.DragPerform when SoPaletteDragNow != null:
                    DragAndDrop.AcceptDrag();
                    SoPaletteDrag drag = SoPaletteDragNow;
                    Vector2Int? drop = area.Contains(e.mousePosition) ? cellAt(e.mousePosition) : null;
                    soPaletteDropCell = null;
                    if (drop.HasValue) DropSoPalette(drag, drop.Value, grid);
                    e.Use(); GUIUtility.ExitGUI();
                    break;
                case EventType.DragExited:
                    if (soPaletteDropCell.HasValue) { soPaletteDropCell = null; Repaint(); }
                    break;
            }
        }

        // 공격 범위 조작 줄을 그리고 범위 칸을 채웁니다(추가 설계 AD2, Q-AD2 C·Q-AD3 A). 그리지 못한 이유를 돌려줍니다.
        // 고른 적: 고른 칸의 적 유닛 하나, 방향이 있는 모양이면 전체(합친 모양)·방향 버튼. 모든 적: 배치된 적 전부를 합친 모양으로 겹칩니다.
        private List<string> DrawSoRange(CombatStageSoGrid grid, Dictionary<Vector2Int, int> reachCounts, HashSet<Vector2Int> splashCells)
        {
            var problems = new List<string>();
            EditorGUI.BeginChangeCheck();
            _soShowRangeSettings = EditorGUILayout.Foldout(_soShowRangeSettings, "공격 범위(참고용) · " + SoRangeModes[soRangeMode], true);
            if (EditorGUI.EndChangeCheck()) { Repaint(); GUIUtility.ExitGUI(); }
            if (_soShowRangeSettings)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    int mode = GUILayout.Toolbar(soRangeMode, SoRangeModes);
                    // 보기 방식이 바뀌면 아래 줄 수가 달라지므로 이번 그리기를 여기서 끝냅니다.
                    if (EditorGUI.EndChangeCheck()) { soRangeMode = mode; GUIUtility.ExitGUI(); }
                }
            }
            if (soRangeMode == 1)
            {
                var unit = soDocument.PlacementsAt(soSelectedCell, grid).Select(placed => placed.Item).OfType<CombatStageSoUnit>().FirstOrDefault();
                if (unit == null)
                {
                    problems.Add("맵에서 적 유닛이 있는 칸을 고르세요.");
                    return problems;
                }
                // 고른 방향이 이 유닛 모양에 없으면(다른 유닛에서 고른 값) 합친 모양으로 돌아갑니다.
                var whole = SoRangeShape(unit.unitGuid, CombatStageSoRange.AllDirections);
                if (!whole.Directions.Contains(soRangeDirection)) soRangeDirection = CombatStageSoRange.AllDirections;
                if (_soShowRangeSettings && whole.Directions.Length > 0)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("방향", GUILayout.Width(100));
                        DrawSoDirectionButton(CombatStageSoRange.AllDirections);
                        foreach (int direction in whole.Directions) DrawSoDirectionButton(direction);
                        GUILayout.FlexibleSpace();
                    }
                }
                AddSoRange(unit, SoRangeShape(unit.unitGuid, soRangeDirection), grid, reachCounts, splashCells, problems);
            }
            else if (soRangeMode == 2)
            {
                foreach (var unit in soDocument.enemyUnits)
                    AddSoRange(unit, SoRangeShape(unit.unitGuid, CombatStageSoRange.AllDirections), grid, reachCounts, splashCells, problems);
            }
            return problems.Distinct().ToList();
        }

        private void DrawSoDirectionButton(int direction)
        {
            Color previous = GUI.backgroundColor;
            if (soRangeDirection == direction) GUI.backgroundColor = SelectedButton;
            if (GUILayout.Button(CombatStageSoRange.DirectionLabel(direction), EditorStyles.miniButton, GUILayout.MinWidth(28))) soRangeDirection = direction;
            GUI.backgroundColor = previous;
        }

        private void AddSoRange(CombatStageSoUnit unit, CombatStageSoRangeShape shape, CombatStageSoGrid grid,
            Dictionary<Vector2Int, int> reachCounts, HashSet<Vector2Int> splashCells, List<string> problems)
        {
            if (shape.Problem != null)
            {
                problems.Add(shape.Problem);
                return;
            }
            CombatStageSoRange.AddTo(shape, soDocument.CellsAt(unit, unit.x, unit.y, unit.rotation, grid), grid, reachCounts, splashCells);
        }

        // 유닛 정의·방향별 모양은 목록 새로고침 때까지 다시 쓰입니다.
        private CombatStageSoRangeShape SoRangeShape(string unitGuid, int direction)
        {
            var key = (unitGuid, direction);
            if (!soRangeShapes.TryGetValue(key, out var shape))
                soRangeShapes[key] = shape = CombatStageSoRange.ShapeOf(CombatStageSoAssets.Load(unitGuid), direction);
            return shape;
        }

        // 상자 편집 버튼 줄입니다. 상자 선택이 있으면 상자 안, 없으면 맵 전체가 대칭 대상입니다.
        private void DrawSoBoxButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("복사 (Ctrl+C)")) CopySo();
                bool hasClip = soClip != null && soClip.Count > 0;
                using (new EditorGUI.DisabledScope(!hasClip))
                    if (GUILayout.Button(hasClip ? $"붙여넣기 (Ctrl+V) · {soClip.Count}개" : "붙여넣기 (Ctrl+V)")) { PasteSo(); GUIUtility.ExitGUI(); }
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                string target = soHasBox ? "상자" : "전체";
                if (SoPlayerFilter) target += " · 표시 배치만";
                if (GUILayout.Button($"좌우 뒤집기 ({target})")) { FlipSo(); GUIUtility.ExitGUI(); }
                if (GUILayout.Button($"좌우 대칭 복사 ({target})")) { MirrorCopySo(); GUIUtility.ExitGUI(); }
            }
            // 아군 구역 도구는 도구 설명 줄에 Shift+끌기를 적습니다(막는 칸이 없음).
            if (IsPaintTool(soTool) && soTool != SoTool.PlayerZone)
                EditorGUILayout.LabelField("Shift+끌기: 사각형으로 한 번에 칠하기(막힌 칸은 건너뜀)", EditorStyles.miniLabel);
        }

        // 끌기·Shift+끌기 사각형으로 칸마다 칠하는 도구입니다.
        private static bool IsPaintTool(SoTool tool) =>
            tool == SoTool.Terrain || tool == SoTool.Obstacle || tool == SoTool.Object || tool == SoTool.Rail || tool == SoTool.Erase ||
            tool == SoTool.PlayerZone;

        // 선택 도구 끌기(추가 계획 P-A5): 배치가 있는 칸에서 끌면 옮기기, 빈 칸에서 끌면 상자 선택, 같은 칸에서 떼면 칸 고르기입니다.
        // 상자 선택 안의 칸에서 끌면 상자 안 묶음을 옮깁니다(Q-AD5).
        // Shift+끌기는 배치가 있는 칸에서 시작해도 늘 상자 선택입니다(2026-10-27 사용자 보고로 더함).
        private void BeginSoDrag(Vector2Int cell, CombatStageSoGrid grid, bool boxOnly)
        {
            ClearSoIssueFocus();
            soSelectedCell = cell; showSoCell = true;
            soDragStart = soDragCurrent = cell;
            soDragGroup = !boxOnly && soHasBox && soBox.Contains(cell) && soDocument.PlacementsIn(soBox, grid).Any(p => SoShown(p.Item));
            soDragItem = soDragGroup || boxOnly ? null : soDocument.PlacementsAt(cell, grid).Where(p => SoShown(p.Item)).FirstOrDefault(placed => placed.Layer != CombatStageSoLayer.Rail).Item;
            soGroupGhostOffset = null;
            soDragMode = SoDragMode.Pending;
        }

        private void BeginSoPaintBox(Vector2Int cell)
        {
            soSelectedCell = cell;
            soDragStart = soDragCurrent = cell;
            soDragMode = SoDragMode.Paint;
        }

        private void UpdateSoDrag(Vector2Int cell)
        {
            if (soDragMode == SoDragMode.None) return;
            soDragCurrent = cell;
            if (soDragMode == SoDragMode.Pending && cell != soDragStart)
                soDragMode = soDragGroup || soDragItem != null ? SoDragMode.Move : SoDragMode.Box;
        }

        private void FinishSoDrag(CombatStageSoGrid grid)
        {
            SoDragMode mode = soDragMode;
            object item = soDragItem;
            bool group = soDragGroup;
            CancelSoDrag();
            switch (mode)
            {
                case SoDragMode.Pending: soHasBox = false; break;
                case SoDragMode.Box: soBox = SoBoxOf(soDragStart, soDragCurrent); soHasBox = true; break;
                case SoDragMode.Move when group: MoveSoGroup(soDragCurrent - soDragStart, grid); break;
                case SoDragMode.Move: MoveSoDragged(item, grid); break;
                case SoDragMode.Paint: PaintSoBox(SoBoxOf(soDragStart, soDragCurrent), grid); break;
            }
        }

        private void CancelSoDrag()
        {
            soDragMode = SoDragMode.None; soDragItem = null; soDragGroup = false; soGroupGhostOffset = null;
        }

        // 상자 안 묶음을 같은 거리만큼 옮깁니다. 하나라도 막히면 옮기지 않습니다(Q-AD5). 상자도 함께 옮깁니다.
        private void MoveSoGroup(Vector2Int offset, CombatStageSoGrid grid)
        {
            if (offset == Vector2Int.zero) return;
            RectInt box = soBox;
            if (TrySoEdit("묶음 끌어서 옮기기", document => document.MoveGroup(box, offset, grid, SoShown)))
                soBox.position += offset;
        }

        // Shift+끌기 사각형: 칠하는 도구의 놓기·지우기를 상자 칸마다 하고, 막힌 칸은 건너뜁니다(T5). Undo는 한 번입니다.
        private void PaintSoBox(RectInt box, CombatStageSoGrid grid)
        {
            if (soTool == SoTool.PlayerZone)
            {
                var cells = new List<Vector2Int>();
                for (int y = box.yMin; y < box.yMax; y++)
                    for (int x = box.xMin; x < box.xMax; x++)
                        cells.Add(new Vector2Int(x, y));
                PaintSoZone(cells, true, false, grid);
                return;
            }
            string definition = BrushOf(soTool);
            if (soTool != SoTool.Erase && string.IsNullOrEmpty(definition) && soTool != SoTool.Rail)
            {
                SetSoNotice("왼쪽 '정의 목록'에서 놓을 정의를 먼저 고르세요.", MessageType.Warning);
                return;
            }
            CombatStageSoLayer? layer = soTool == SoTool.Erase ? (CombatStageSoLayer?)null : LayerOf(soTool);
            string verb = soTool == SoTool.Erase ? "지움" : "놓음";
            TrySoCount($"{SoToolLabels[(int)soTool]} 사각형 칠하기", verb, document => document.PaintBox(box, layer, definition, grid, soCatalog.Names.owner, SoShown));
        }

        // 놓을 자리가 바로 막기에 걸리면(고스트 빨강) 옮기지 않습니다. Undo는 한 번입니다.
        private void MoveSoDragged(object item, CombatStageSoGrid grid)
        {
            if (item == null || soDragCurrent == soDragStart || !soDocument.Contains(item, grid)) return;
            Vector2Int target = soDocument.AnchorOf(item) + (soDragCurrent - soDragStart);
            if (TryEditSo(item, target, soDocument.RotationOf(item), soDocument.DefinitionOf(item), "끌어서 옮기기", () => soDocument.Move(item, target)))
                soSelectedCell = soDragCurrent;
        }

        // 상자 선택(추가 계획 P-A2)과 끄는 중인 배치의 고스트(초록 = 놓을 수 있음, 빨강 = 막힘)를 칸 위에 테두리로 그립니다.
        private void DrawSoOverlays(Rect map, float size, CombatStageSoGrid grid)
        {
            if (Event.current.type != EventType.Repaint) return;
            foreach (var (cells, color) in SoFrames(grid, true)) DrawSoFrame(map, size, cells, color);
        }

        // 2D·3D가 같이 쓰는 테두리 목록입니다: 고른 바닥 타일, 상자 선택, 사각형 칠하기 상자, 끄는 배치·묶음의 고스트.
        // clipTiles면 바닥 타일 테두리를 판 안으로 자릅니다(2D, 설계 §14 TO4).
        private List<(Rect Cells, Color Color)> SoFrames(CombatStageSoGrid grid, bool clipTiles)
        {
            var frames = new List<(Rect Cells, Color Color)>();
            AddSoTileFrames(frames, clipTiles);
            // 정의 목록에서 끄는 동안 놓을 자리입니다. 판단은 놓을 때 쓰는 PlacementError와 같습니다.
            if (soPaletteDropCell.HasValue && SoPaletteDragNow is SoPaletteDrag gimmick && IsSoGimmickTool(gimmick.Tool))
                AddSoGimmickDropFrames(frames, gimmick.Tool, soPaletteDropCell.Value, grid);
            else if (soPaletteDropCell.HasValue && SoPaletteDragNow is SoPaletteDrag palette)
            {
                CombatStageSoLayer layer = LayerOf(palette.Tool);
                var dropCells = soDocument.CellsForNew(layer, palette.Guid, soPaletteDropCell.Value, grid);
                bool dropValid = soDocument.PlacementError(layer, null, palette.Guid, dropCells, grid) == null;
                frames.Add((SoArea(SoBoundsOf(dropCells)), dropValid ? GhostValid : GhostInvalid));
            }
            if (soDragMode == SoDragMode.Box) frames.Add((SoArea(SoBoxOf(soDragStart, soDragCurrent)), BoxColor));
            else if (soHasBox) frames.Add((SoArea(soBox), BoxColor));
            if (soDragMode == SoDragMode.Paint) frames.Add((SoArea(SoBoxOf(soDragStart, soDragCurrent)), GhostValid));
            if (soDragMode == SoDragMode.Move && soDragGroup)
            {
                Vector2Int offset = soDragCurrent - soDragStart;
                if (soGroupGhostOffset != offset)
                {
                    RectInt box = soBox;
                    soGroupGhostValid = offset == Vector2Int.zero || CombatStageSoDocument.Clone(soDocument).MoveGroup(box, offset, grid, SoShown) == null;
                    soGroupGhostOffset = offset;
                }
                foreach (var placed in soDocument.PlacementsIn(soBox, grid).Where(p => SoShown(p.Item)))
                    frames.Add((SoArea(SoBoundsOf(placed.Cells.Select(cell => cell + offset).ToList())), soGroupGhostValid ? GhostValid : GhostInvalid));
                return frames;
            }
            if (soDragMode != SoDragMode.Move || soDragItem == null || !soDocument.Contains(soDragItem, grid)) return frames;
            object item = soDragItem;
            if (!SoShown(item)) return frames;
            Vector2Int target = soDocument.AnchorOf(item) + (soDragCurrent - soDragStart);
            var cells = soDocument.CellsAt(item, target.x, target.y, soDocument.RotationOf(item), grid);
            bool valid = soDocument.PlacementError(soDocument.LayerOf(item), item, soDocument.DefinitionOf(item), cells, grid) == null;
            frames.Add((SoArea(SoBoundsOf(cells)), valid ? GhostValid : GhostInvalid));
            return frames;
        }

        // 3D 칸 채우기 목록입니다: 아군 배치 구역 옅게, 레일, 공격 범위, 하붑 시작 구름, 고른 칸(2D 칸 색과 같은 뜻).
        private List<(RectInt Cells, Color Color)> SoFills3D(CombatStageSoDocument document, Dictionary<Vector2Int, int> reachCounts, HashSet<Vector2Int> splashCells)
        {
            var fills = new List<(RectInt Cells, Color Color)>();
            for (int y = 0; y < soField.Rows; y++)
                for (int x = 0; x < soField.Columns; x++)
                    if (soZone.Contains(new Vector2Int(x, y)))
                        fills.Add((new RectInt(x, y, 1, 1), new Color(PlayerZone.r, PlayerZone.g, PlayerZone.b, .35f)));
            foreach (var rail in document.railCells) fills.Add((new RectInt(rail, Vector2Int.one), new Color(RailColor.r, RailColor.g, RailColor.b, .6f)));
            foreach (var cell in splashCells) fills.Add((new RectInt(cell, Vector2Int.one), new Color(SplashColor.r, SplashColor.g, SplashColor.b, .3f)));
            foreach (var pair in reachCounts)
                fills.Add((new RectInt(pair.Key, Vector2Int.one), new Color(ReachColor.r, ReachColor.g, ReachColor.b, Mathf.Min(.8f, .4f + .12f * (pair.Value - 1)))));
            foreach (var cell in SoHaboobStartCells()) fills.Add((new RectInt(cell, Vector2Int.one), new Color(HaboobColor.r, HaboobColor.g, HaboobColor.b, .45f)));
            if (soSelectedCell.x >= 0 && soSelectedCell.y >= 0 && soSelectedCell.x < soField.Columns && soSelectedCell.y < soField.Rows)
                fills.Add((new RectInt(soSelectedCell, Vector2Int.one), new Color(1f, 1f, 1f, .35f)));
            return fills;
        }

        // 3D 보기(추가 설계 AD1)입니다. 같은 도구로 편집하고, 칸은 마우스 광선이 바닥 평면에 닿는 자리입니다.
        private void DrawSoMap3D(CombatStageSoGrid grid, Dictionary<Vector2Int, int> reachCounts, HashSet<Vector2Int> splashCells)
        {
            try { if (soVisuals == null) soVisuals = CombatStageSoVisuals.Build(); }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox("외형 표를 읽지 못했습니다: " + exception.Message, MessageType.Error);
                return;
            }
            var space = new CombatStageSoSpace(soField.Columns, soField.Rows, soVisuals.FieldView, SoTileScene());
            bool player = EditorGUILayout.Toggle("Player 기준 (끄면 전체 배치)", soPlayerOnly);
            if (player != soPlayerOnly)
            {
                SetSoPlayerView(player);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField(soPlayerOnly ? "Player 기준 · 비공개/판정 오류는 속성의 '공개·숨김 안내'에서 확인" : "전체 배치 · 공개 판정 오류는 별도로 유지됩니다.", EditorStyles.wordWrappedMiniLabel);
            space.RaiseSurfaces(soDocument, soVisuals, SoShown);
            EnsureSoPreview(space);
            Rect rect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (soPreview.Navigate(Event.current, rect)) Repaint();
            if (Event.current.type == EventType.Repaint)
            {
                soPreview.Sync(soDocument, grid, soVisuals, space, LayerColor, SoBackgroundMode(soDocument.view), soPlayerOnly, SoShown);
                // 3D는 판 밖 타일 테두리도 자르지 않고 그립니다(설계 §14 TO4).
                soPreview.Draw(rect, space, SoFills3D(soDocument, reachCounts, splashCells), SoFrames(grid, false));
            }
            // '타일 회전' 도구는 판 밖 칸도 받아 그 자리의 타일을 고릅니다(설계 §14 TO2). 다른 도구는 판 안만입니다.
            bool rawCells = soTool == SoTool.TileRotate;
            HandleSoPointer(rect,
                mouse => soPreview.PickGround(rect, mouse) is Vector3 ground ? (rawCells ? space.RawCellAt(ground) : space.CellAt(ground)) : null,
                mouse => soPreview.PickGround(rect, mouse) is Vector3 ground ? (rawCells ? space.RawCellAt(ground) : space.ClampedCellAt(ground)) : (Vector2Int?)null,
                grid);
            EditorGUILayout.LabelField("좌클릭: 도구 · 우클릭 끌기·Alt+좌클릭 끌기: 회전 · 휠: 확대 · Shift+우클릭·가운데 버튼: 이동", EditorStyles.miniLabel);
            if (soVisuals.FieldView.Approximate)
                EditorGUILayout.HelpBox("필드 보기가 GridOutline이 아니라 칸 크기를 1×1로 근사했습니다.", MessageType.Info);
            if (soPreview.Problem != null) EditorGUILayout.HelpBox(soPreview.Problem, MessageType.Warning);
        }

        private void EnsureSoPreview(CombatStageSoSpace space)
        {
            if (soPreview == null)
            {
                soPreview = new CombatStageSoPreview();
                // 창을 닫을 때(OnDestroy)와 스크립트를 다시 불러오기 전에 미리보기 장면을 정리합니다.
                AssemblyReloadEvents.beforeAssemblyReload += DisposeSoPreview;
                soPreviewNeedsReset = true;
            }
            if (!soPreviewNeedsReset) return;
            soPreview.ResetView(space);
            soPreviewNeedsReset = false;
        }

        private void DisposeSoPreview()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= DisposeSoPreview;
            soPreview?.Dispose();
            soPreview = null;
        }

        private void OnDestroy() => DisposeSoPreview();

        private void DrawSoFrame(Rect map, float size, Rect cells, Color color)
        {
            Rect rect = SoCellRect(map, size, cells);
            const float line = 2;
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, line), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - line, rect.width, line), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, line, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - line, rect.y, line, rect.height), color);
        }

        // 칸 사각형(왼쪽 아래 기준)을 2D 격자의 화면 사각형으로 바꿉니다. 격자는 위가 큰 y입니다.
        private static Rect SoArea(RectInt cells) => new Rect(cells.x, cells.y, cells.width, cells.height);

        private Rect SoCellRect(Rect map, float size, RectInt cells) => SoCellRect(map, size, SoArea(cells));

        private Rect SoCellRect(Rect map, float size, Rect cells) =>
            new Rect(map.x + cells.xMin * size, map.y + (soField.Rows - cells.yMax) * size, cells.width * size - 2, cells.height * size - 2);

        private Vector2Int? SoCellAt(Rect map, float size, Vector2 mouse)
        {
            Vector2Int cell = SoRawCellAt(map, size, mouse);
            return cell.x >= 0 && cell.y >= 0 && cell.x < soField.Columns && cell.y < soField.Rows ? cell : (Vector2Int?)null;
        }

        private Vector2Int SoClampedCellAt(Rect map, float size, Vector2 mouse)
        {
            Vector2Int cell = SoRawCellAt(map, size, mouse);
            return new Vector2Int(Mathf.Clamp(cell.x, 0, soField.Columns - 1), Mathf.Clamp(cell.y, 0, soField.Rows - 1));
        }

        private Vector2Int SoRawCellAt(Rect map, float size, Vector2 mouse) =>
            new Vector2Int(Mathf.FloorToInt((mouse.x - map.x) / size), soField.Rows - 1 - Mathf.FloorToInt((mouse.y - map.y) / size));

        private static RectInt SoBoxOf(Vector2Int a, Vector2Int b) =>
            new RectInt(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Abs(a.x - b.x) + 1, Mathf.Abs(a.y - b.y) + 1);

        private static RectInt SoBoundsOf(List<Vector2Int> cells)
        {
            int xMin = cells.Min(cell => cell.x), yMin = cells.Min(cell => cell.y);
            return new RectInt(xMin, yMin, cells.Max(cell => cell.x) - xMin + 1, cells.Max(cell => cell.y) - yMin + 1);
        }

        // 단축키(추가 계획 P-A6). 입력 칸을 편집 중이면 쓰지 않습니다.
        // Ctrl+C·Ctrl+V는 에디터가 키 대신 Copy·Paste 명령으로 보내므로 명령으로 받습니다.
        private void HandleSoKeys()
        {
            if (GetSoWorkspace() == SoWorkspace.Settings || _soShowStages || _soShowCreation || soCatalog == null) return;
            Event e = Event.current;
            // '타일' 보기에서는 배치 단축키(복사·붙여넣기·지우기·회전)를 받지 않습니다. 보이지 않는 배치 선택이 바뀌지 않게 합니다.
            if (soMapView == SoViewTiles)
            {
                HandleSoTileKeys(e);
                return;
            }
            // '타일 회전' 도구도 배치 단축키를 받지 않고 타일 고르기·돌리기 키만 받습니다(계획 Tc.4).
            if (soTool == SoTool.TileRotate)
            {
                HandleSoTileToolKeys(e);
                return;
            }
            if ((e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && (e.commandName == "Copy" || e.commandName == "Paste")
                && GUIUtility.keyboardControl == 0 && soDocument.IsOpen)
            {
                if (e.type == EventType.ExecuteCommand)
                {
                    if (e.commandName == "Copy") CopySo();
                    else PasteSo();
                }
                e.Use(); Repaint();
                return;
            }
            if (e.type != EventType.KeyDown || GUIUtility.keyboardControl != 0 || !soDocument.IsOpen) return;
            var grid = SoGrid();
            switch (e.keyCode)
            {
                case KeyCode.Escape:
                    if (soDragMode != SoDragMode.None) { CancelSoDrag(); GUIUtility.hotControl = 0; }
                    else soHasBox = false;
                    break;
                case KeyCode.Delete when grid != null:
                    DeleteSoSelection(grid);
                    break;
                case KeyCode.R when grid != null && !e.control && !e.command && !e.alt:
                    RotateSoSelected(grid);
                    break;
                default:
                    return;
            }
            e.Use(); Repaint();
        }

        // 상자 선택이 있으면 상자 안 배치(본부 제외)를, 없으면 고른 칸 맨 위 배치를 지웁니다.
        private void DeleteSoSelection(CombatStageSoGrid grid)
        {
            if (!soHasBox)
            {
                // 고른 칸에 지울 배치가 없고 하붑 시작 구름 칸이면 시작 위치를 비웁니다(설계 §26.6 Q-GP3). 배치가 있으면 배치부터 지웁니다.
                if (grid.Contains(soSelectedCell) && soDocument.TopErasable(soSelectedCell, grid, SoShown).Item == null && SoHaboobStartCells().Contains(soSelectedCell))
                    ClearSoHaboobStart();
                else if (grid.Contains(soSelectedCell)) EraseSoCell(soSelectedCell, false, grid);
                EndStroke();
                return;
            }
            var targets = soDocument.PlacementsIn(soBox, grid).Where(p => SoShown(p.Item)).Select(placed => placed.Item).ToList();
            if (targets.Count == 0)
            {
                SetSoNotice("상자 안에 지울 배치가 없습니다.", MessageType.Warning);
                return;
            }
            SoChange($"상자 안 배치 {targets.Count}개 지우기", () => targets.ForEach(soDocument.Remove));
        }

        private void RotateSoSelected(CombatStageSoGrid grid)
        {
            object item = soDocument.PlacementsAt(soSelectedCell, grid).Where(p => SoShown(p.Item)).FirstOrDefault(placed => CombatStageSoDocument.Rotates(placed.Item)).Item;
            if (item == null)
            {
                SetSoNotice("고른 칸에 돌릴 배치가 없습니다.", MessageType.Warning);
                return;
            }
            RotateSo(item);
        }

        // 묶음 연산(추가 설계 AD3·AD4)은 작업본 복제본에서 먼저 돌려 결과를 보고, 쓸 만할 때만 Undo를 남기며 실제 작업본에 다시 돌립니다.
        private bool TrySoEdit(string label, Func<CombatStageSoDocument, string> edit)
        {
            string error = edit(CombatStageSoDocument.Clone(soDocument));
            if (error != null)
            {
                SetSoNotice($"{label} 안 함: {error}", MessageType.Warning);
                return false;
            }
            SoChange(label, () => edit(soDocument));
            SetSoNotice($"{label}: 끝났습니다.");
            return true;
        }

        private void TrySoCount(string label, string verb, Func<CombatStageSoDocument, (int Done, int Skipped)> edit)
        {
            var trial = edit(CombatStageSoDocument.Clone(soDocument));
            if (trial.Done == 0)
            {
                SetSoNotice($"{label}: 할 수 있는 칸이 없습니다(건너뜀 {trial.Skipped}).", MessageType.Warning);
                return;
            }
            SoChange(label, () => edit(soDocument));
            SetSoNotice($"{label}: {trial.Done}개 {verb}" + (trial.Skipped > 0 ? $", {trial.Skipped}개 건너뜀(맵 밖·겹침)" : ""));
        }

        // 상자 안 배치(본부 제외)를 상자 왼쪽 아래 기준으로 복사합니다(추가 계획 P-A3).
        private void CopySo()
        {
            var grid = SoGrid();
            if (grid == null || !soHasBox)
            {
                SetSoNotice("복사할 상자를 먼저 고르세요(선택 도구로 빈 칸부터 끌기).", MessageType.Warning);
                return;
            }
            soClip = soDocument.Copy(soDocument.PlacementsIn(soBox, grid).Where(p => SoShown(p.Item)), soBox.position);
            if (soClip.Count == 0) SetSoNotice("상자 안에 복사할 배치가 없습니다(본부는 복사하지 않습니다).", MessageType.Warning);
            else SetSoNotice($"{soClip.Count}개를 복사했습니다. 붙일 칸(왼쪽 아래 기준)을 고르고 붙여넣기(Ctrl+V)를 누르세요.");
        }

        // 고른 칸을 복사한 상자의 왼쪽 아래 자리로 삼아 붙입니다(추가 계획 P-A4).
        private void PasteSo()
        {
            soPasteVisibilityBlocked = false;
            var grid = SoGrid();
            if (grid == null || soClip == null || soClip.Count == 0)
            {
                SetSoNotice("붙일 복사본이 없습니다.", MessageType.Warning);
                return;
            }
            if (!grid.Contains(soSelectedCell))
            {
                SetSoNotice("붙일 칸(왼쪽 아래 기준)을 먼저 고르세요.", MessageType.Warning);
                return;
            }
            if (SoPlayerFilter && soClip.terrains.Concat(soClip.objects).Any(marker => !SoShown(marker)))
            {
                SetSoNotice("Player 기준에서 숨겨질 배치 또는 공개 판정 오류가 있어 붙여넣기를 중단했습니다.", MessageType.Warning);
                soPasteVisibilityBlocked = true;
                return;
            }
            CombatStageSoClip clip = soClip;
            Vector2Int origin = soSelectedCell;
            TrySoCount("붙여넣기", "붙임", document => document.Paste(clip, origin, grid));
        }

        // 좌우 뒤집기: 상자 안(없으면 전체)을 맵 가운데 세로줄 기준으로 그 자리에서 뒤집습니다. 하나라도 막히면 하지 않습니다(Q-AD4, P-A7).
        private void FlipSo()
        {
            var grid = SoGrid();
            if (grid == null) return;
            RectInt? box = soHasBox ? soBox : (RectInt?)null;
            if (!TrySoEdit("좌우 뒤집기", document => document.MirrorFlip(box, grid, MirrorRotation, SoShown)) || !box.HasValue) return;
            // 상자도 대칭 자리로 옮겨, 뒤집은 배치를 계속 고른 상태로 둡니다.
            soBox = new RectInt(grid.Columns - box.Value.xMax, box.Value.y, box.Value.width, box.Value.height);
        }

        // 좌우 대칭 복사: 원본은 두고 반대편에 사본을 더합니다. 막힌 사본은 건너뜁니다(Q-AD4, P-A7).
        private void MirrorCopySo()
        {
            var grid = SoGrid();
            if (grid == null) return;
            RectInt? box = soHasBox ? soBox : (RectInt?)null;
            TrySoCount("좌우 대칭 복사", "더함", document => document.MirrorCopy(box, grid, MirrorRotation, SoShown));
        }

        // R90 ↔ R270, R0·R180은 그대로입니다(Q-AD4). 이름은 SO 열거에서 읽습니다.
        private int MirrorRotation(int rotation)
        {
            string[] names = soCatalog.Names.rotation;
            if (names == null) return rotation;
            string name = CombatStageSoAssets.NameAt(names, rotation);
            string mirrored = name == "R90" ? "R270" : name == "R270" ? "R90" : name;
            int index = Array.IndexOf(names, mirrored);
            return index >= 0 ? index : rotation;
        }

        // 시계 방향으로 한 칸 돌립니다(R0 → R90 → R180 → R270). 크기가 바뀌면 바로 막기를 거칩니다.
        private void RotateSo(object item)
        {
            int count = soCatalog.Names.rotation?.Length ?? 4;
            int next = (soDocument.RotationOf(item) + 1) % count;
            TryEditSo(item, soDocument.AnchorOf(item), next, soDocument.DefinitionOf(item), "시계 방향 회전", () => soDocument.SetRotation(item, next));
        }

        private string SoToolHint()
        {
            switch (soTool)
            {
                case SoTool.Select: return "칸 누르기: 고르기 · 배치를 끌기: 옮기기 · 빈 칸부터 끌기 또는 Shift+끌기: 상자 선택 · Delete 지우기 · R 회전 · Esc 취소";
                case SoTool.Headquarters: return "누른 칸으로 적 본부를 옮깁니다(본부 정의는 스테이지 설정 → 공유 SO·본부·구조물).";
                case SoTool.Rail: return "누르거나 끌어서 레일 칸을 더합니다. 지우기는 '지우기' 도구로 합니다.";
                case SoTool.Erase: return "누른 칸의 맨 위 배치를 지웁니다. 본부는 지우지 않습니다.";
                case SoTool.PlayerZone:
                    return "누르거나 끌어서 아군 배치 구역 칸을 칠하거나 지웁니다 · Shift+끌기: 사각형 · 기존 '지우기' 도구는 구역을 건드리지 않습니다";
                case SoTool.HaboobStart:
                    return "누른 칸이 구름 가운데 근처가 되도록 하붑 시작 위치를 정합니다(하붑·시작 위치 지정도 켬) · 다른 칸을 누르면 옮김 · 구름이 판 밖으로 나가는 칸은 놓지 않음 · " +
                           "선택 도구로 구름 칸을 고르고 Delete: 시작 위치 비우기";
                case SoTool.Rockfall:
                    return "아무 칸이나 누르면 낙석을 켭니다. 낙석 위치는 전투 중 무작위로 정해져 맵에 그리지 않습니다.";
                case SoTool.TileRotate:
                    return "칸 누르기·끌기: 그 칸의 바닥 타일 고르기 · Ctrl: 더하기 · Shift+끌기: 사각형 · Ctrl+A: 2D = 판 위 타일·3D = 타일 맵 전부 · Esc: 비우기 · R / Shift+R: 돌리기 · " +
                           "판 밖 타일은 3D(판 밖을 누름)나 '바닥 타일' 작업 영역에서 고릅니다(2D는 판 안만)";
            }
            string brush = BrushOf(soTool);
            return string.IsNullOrEmpty(brush) ? "왼쪽 '정의 목록'에서 놓을 정의를 고르세요." : $"놓을 정의: {soCatalog.DefinitionLabel(brush)}";
        }

        private void HandleSoCell(Vector2Int position, bool dragging, CombatStageSoGrid grid)
        {
            if (dragging && (!IsPaintTool(soTool) || position == lastPainted)) return;
            lastPainted = position;
            ClearSoIssueFocus();
            soSelectedCell = position; showSoCell = true;
            if (soTool == SoTool.Select) { Repaint(); return; }
            if (soTool == SoTool.Erase) { EraseSoCell(position, dragging, grid); return; }
            if (soTool == SoTool.PlayerZone) { PaintSoZone(new[] { position }, false, dragging, grid); return; }
            if (soTool == SoTool.HaboobStart) { PlaceSoHaboobStart(position, grid); return; }
            if (soTool == SoTool.Rockfall) { EnableSoRockfall(); return; }
            CombatStageSoLayer layer = LayerOf(soTool);
            string definition = soTool == SoTool.Headquarters ? soDocument.headquartersGuid : BrushOf(soTool);
            if (NeedsBrush(soTool) && string.IsNullOrEmpty(definition))
            {
                SetSoNotice("왼쪽 '정의 목록'에서 놓을 정의를 먼저 고르세요.", MessageType.Warning);
                return;
            }
            if (soTool == SoTool.Headquarters && !soDocument.HasHeadquarters)
            {
                SetSoNotice("스테이지 설정의 '공유 SO·본부·구조물'에서 본부 정의를 먼저 고르세요.", MessageType.Warning);
                return;
            }
            object self = soTool == SoTool.Headquarters ? soDocument.enemyHeadquarters : null;
            string error = soDocument.PlacementError(layer, self, definition, soDocument.CellsForNew(layer, definition, position, grid), grid);
            if (error != null)
            {
                SetSoNotice(error, MessageType.Warning);
                return;
            }
            BeginSoStroke();
            SoChange($"{SoToolLabels[(int)soTool]} 놓기", () => soDocument.Add(layer, definition, position, soCatalog.Names.owner));
        }

        private void EraseSoCell(Vector2Int position, bool dragging, CombatStageSoGrid grid)
        {
            var target = soDocument.TopErasable(position, grid, SoShown);
            if (target.Item == null)
            {
                if (!dragging) SetSoNotice("지울 배치가 없습니다.", MessageType.Warning);
                return;
            }
            BeginSoStroke();
            SoChange($"{CombatStageSoDocument.LayerName(target.Layer)} 지우기", () => soDocument.Remove(target.Item));
        }

        // 아군 구역 칠하기·지우기입니다(설계 §11 AZ4). 칸마다(box 거짓)는 끌기 한 번이 Undo 하나이고, 사각형은 Undo 하나입니다.
        // 작업본 복제본에서 먼저 돌려 바뀌는 칸이 없으면 칸 목록으로 옮기지도, Undo를 남기지도 않습니다(계획 P-AZT7).
        private void PaintSoZone(IReadOnlyCollection<Vector2Int> cells, bool box, bool dragging, CombatStageSoGrid grid)
        {
            bool erase = soZoneErase;
            CombatStageSoField field = soField;
            var trial = CombatStageSoDocument.Clone(soDocument).PaintZone(cells, erase, field, grid);
            if (trial.Error != null)
            {
                SetSoNotice(trial.Error, MessageType.Warning);
                return;
            }
            string verb = erase ? "지움" : "칠함";
            if (trial.Done == 0)
            {
                if (box) SetSoNotice($"아군 구역 사각형: 바뀌는 칸이 없습니다({(erase ? "구역 칸 없음" : "모두 이미 구역")}).", MessageType.Warning);
                else if (!dragging) SetSoNotice(erase ? "아군 구역이 아닌 칸입니다." : "이미 아군 구역인 칸입니다.");
                return;
            }
            bool fromRows = soDocument.playerDeploymentCells.Count == 0;
            string label = box ? $"아군 구역 사각형 {(erase ? "지우기" : "칠하기")}" : $"아군 구역 {(erase ? "지우기" : "칠하기")}";
            if (!box) BeginSoStroke();
            SoChange(label, () => soDocument.PaintZone(cells, erase, field, grid));
            // 줄 수 구역에서 처음 편집하면 보이던 칸이 칸 목록으로 옮겨져 출처가 바뀝니다(Q-AZ1 A).
            if (fromRows) SetSoNotice($"칸 목록이 비어 있어 필드 아군 줄 수 {field.PlayerDepth}줄의 칸을 칸 목록으로 옮긴 뒤 {trial.Done}칸 {verb}. 되돌리려면 Ctrl+Z.");
            else if (box) SetSoNotice($"{label}: {trial.Done}칸 {verb}");
        }

        // 한 번 끌기를 Undo 한 번으로 묶습니다(DT 모드와 같은 방식).
        private void BeginSoStroke()
        {
            if (strokeGroup >= 0) return;
            Undo.IncrementCurrentGroup(); strokeGroup = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("SO 배치 편집");
        }

        private static CombatStageSoLayer LayerOf(SoTool tool)
        {
            switch (tool)
            {
                case SoTool.EnemyUnit: return CombatStageSoLayer.Unit;
                case SoTool.Structure: return CombatStageSoLayer.Structure;
                case SoTool.Headquarters: return CombatStageSoLayer.Headquarters;
                case SoTool.Terrain: return CombatStageSoLayer.Terrain;
                case SoTool.Obstacle: return CombatStageSoLayer.Obstacle;
                case SoTool.Object: return CombatStageSoLayer.Object;
                default: return CombatStageSoLayer.Rail;
            }
        }

        private static bool NeedsBrush(SoTool tool) =>
            tool == SoTool.EnemyUnit || tool == SoTool.Structure || tool == SoTool.Terrain || tool == SoTool.Obstacle || tool == SoTool.Object;

        private string BrushOf(SoTool tool)
        {
            switch (tool)
            {
                case SoTool.EnemyUnit: return soUnitBrush;
                case SoTool.Structure: return soStructureBrush;
                case SoTool.Terrain: return soTerrainBrush;
                case SoTool.Obstacle: return soObstacleBrush;
                case SoTool.Object: return soObjectBrush;
                default: return "";
            }
        }

        private CombatStageSoGrid SoGrid() =>
            soField == null ? null : new CombatStageSoGrid(soField.Columns, soField.Rows, soCatalog.Footprint, soCatalog.Names.IsQuarterTurn);

        // 구역 둘은 맨 앞에 두고, 아군 구역은 어디서 정했는지(칸 목록·필드 줄 수)를 글자로 붙입니다(설계 P-AZ2).
        private static void DrawSoLegend(CombatStageSoZone zone)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string playerLabel = zone.IsListed ? "아군 구역(칸 지정)" : $"아군 구역(필드 {zone.RowDepth}줄)";
                var zones = new (string Label, Color Color)[] { (playerLabel, PlayerZone), ("적 배치 가능", NeutralZone) };
                foreach (var (label, color) in zones.Concat(SoLegend))
                {
                    Rect swatch = GUILayoutUtility.GetRect(12, 12, GUILayout.Width(12), GUILayout.Height(12));
                    EditorGUI.DrawRect(swatch, color);
                    GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                }
                GUILayout.FlexibleSpace();
            }
        }

        // 작업본의 배치를 칸별로 모읍니다. 칸 계산과 층 순서는 작업본이 정합니다.
        private Dictionary<Vector2Int, List<SoOccupant>> BuildSoOccupants(CombatStageSoGrid grid, out List<string> outside)
        {
            var cells = new Dictionary<Vector2Int, List<SoOccupant>>();
            outside = new List<string>();
            foreach (var placed in soDocument.Placements(grid).OrderByDescending(item => item.Layer))
            {
                var occupant = new SoOccupant(ShortLabel(placed), LayerColor(placed.Layer));
                foreach (var cell in placed.Cells)
                {
                    if (!grid.Contains(cell))
                    {
                        outside.Add($"{occupant.Label}({cell.x}, {cell.y})");
                        continue;
                    }
                    if (!cells.TryGetValue(cell, out var list)) cells[cell] = list = new List<SoOccupant>();
                    list.Add(occupant);
                }
            }
            // 아군 배치 구역 칸은 배치가 아니라 칸 바탕이지만, 필드를 줄여 맵 밖이 되면 같은 경고에 보입니다(설계 §11 AZ7).
            foreach (var cell in soDocument.playerDeploymentCells.Where(cell => !grid.Contains(cell)).Distinct())
                outside.Add($"아군 구역({cell.x}, {cell.y})");
            return cells;
        }

        private string ShortLabel(CombatStageSoPlaced placed)
        {
            switch (placed.Layer)
            {
                case CombatStageSoLayer.Rail: return "레일";
                case CombatStageSoLayer.Headquarters: return "본부";
                default: return soCatalog.CellName(soDocument.DefinitionOf(placed.Item));
            }
        }

        private static Color LayerColor(CombatStageSoLayer layer)
        {
            switch (layer)
            {
                case CombatStageSoLayer.Rail: return RailColor;
                case CombatStageSoLayer.Terrain: return TerrainColor;
                case CombatStageSoLayer.Object: return ObjectColor;
                case CombatStageSoLayer.Obstacle: return ObstacleColor;
                case CombatStageSoLayer.Structure:
                case CombatStageSoLayer.Headquarters: return StructureColor;
                default: return EnemyUnitColor;
            }
        }

        #endregion

        #region 오른쪽 패널

        private void DrawSoCell()
        {
            var grid = SoGrid();
            // 검사 대상(판 밖 포함)은 다른 칸 정보보다 먼저 기존 복구 패널 한 곳에서 편집합니다.
            if (DrawSoFocusedIssue(grid) != null) return;
            if (grid != null && grid.Contains(soSelectedCell))
            {
                EditorGUILayout.LabelField("좌표 · 구역", $"({soSelectedCell.x}, {soSelectedCell.y}) · {ZoneName(SoZoneOf(soField), soSelectedCell)}");
                DrawSoCellTile(soSelectedCell);
                if (SoHaboobStartCells().Contains(soSelectedCell))
                    EditorGUILayout.LabelField("하붑 시작 구름 칸입니다. 이 칸에 배치가 없으면 Delete로 시작 위치를 비웁니다(무작위로).", EditorStyles.wordWrappedMiniLabel);
            }
            if (grid != null && grid.Contains(soSelectedCell))
            {
                var placements = soDocument.PlacementsAt(soSelectedCell, grid);
                if (placements.Count == 0) EditorGUILayout.LabelField("배치 없음", EditorStyles.miniLabel);
                else
                {
                    int index = Mathf.Max(0, placements.FindIndex(placed => Equals(placed.Item, _soInspectedPlacement)));
                    EditorGUI.BeginChangeCheck();
                    int next = EditorGUILayout.Popup("속성 대상", index, placements.Select((placed, i) =>
                        $"{i + 1}. {CombatStageSoDocument.LayerName(placed.Layer)} · {ShortLabel(placed)}").ToArray());
                    _soInspectedPlacement = placements[next].Item;
                    if (EditorGUI.EndChangeCheck()) { GUI.FocusControl(null); Repaint(); GUIUtility.ExitGUI(); }
                    if (placements.Count > 1)
                        EditorGUILayout.LabelField("맵 끌기·Delete/R은 선택 칸 기준입니다. 이 대상만 지우려면 아래 '이 배치 지우기'를 사용하세요.", EditorStyles.wordWrappedMiniLabel);
                    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        var placed = placements[next];
                        DrawSoVisibilityNotice(placed.Item);
                        DrawSoPlaced(placed);
                    }
                }
            }
            else EditorGUILayout.LabelField(grid == null ? "필드 설정을 확인한 뒤 맵에서 칸을 고르세요." : "맵에서 칸을 누르세요.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawSoRandomTerrain()
        {
            var randoms = soDocument.terrains.Where(marker => marker.randomizeInitialPosition).ToList();
            EditorGUILayout.LabelField($"위치 없는 지형 · 무작위 시작 ({randoms.Count})", EditorStyles.boldLabel);
            if (randoms.Count == 0) EditorGUILayout.HelpBox("무작위 시작을 켠 지형이 없습니다. 선택한 지형의 속성에서 설정할 수 있습니다.", MessageType.Info);
            foreach (var marker in randoms)
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawSoVisibilityNotice(marker);
                    DrawSoMarker(marker, true);
                }
        }

        private void DrawSoPlaced(CombatStageSoPlaced placed)
        {
            EditorGUILayout.LabelField(CombatStageSoDocument.LayerName(placed.Layer), EditorStyles.boldLabel);
            switch (placed.Item)
            {
                case CombatStageSoUnit unit: DrawSoUnit(unit); break;
                case CombatStageSoPiece piece: DrawSoPiece(piece, placed.Layer == CombatStageSoLayer.Structure ? soCatalog.Structures : soCatalog.Obstacles); break;
                case CombatStageSoMarker marker: DrawSoMarker(marker, placed.Layer == CombatStageSoLayer.Terrain); break;
                case CombatStageSoPose pose:
                    DrawSoPlacement(pose, pose.x, pose.y, pose.rotation, true);
                    DrawSoDeleteButton(pose);
                    break;
                default: DrawSoDeleteButton(placed.Item); break;
            }
        }

        private void DrawSoUnit(CombatStageSoUnit unit)
        {
            if (SoAsset("유닛", unit.unitGuid, soCatalog.EnemyUnits, out string picked))
                TryEditSo(unit, new Vector2Int(unit.x, unit.y), unit.rotation, picked, "유닛 변경", () => unit.unitGuid = picked);
            if (soCatalog.PlayerUnits.IndexOf(unit.unitGuid) > 0)
                EditorGUILayout.HelpBox("아군 유닛 정의입니다. 적 배치에는 쓸 수 없어 전투 진입 때 Core가 멈춥니다. 위에서 적 유닛으로 바꾸세요.", MessageType.Warning);
            DrawSoPlacement(unit, unit.x, unit.y, unit.rotation, true);
            if (SoToggle("격파 목표", unit.isVictoryTarget, out bool target)) SoChange("격파 목표 변경", () => unit.isVictoryTarget = target);
            if (SoAsset("시작 아이템", unit.startingItemGuid, soCatalog.Items, out string item)) SoChange("시작 아이템 변경", () => unit.startingItemGuid = item);
            DrawSoDeleteButton(unit);
        }

        private void DrawSoPiece(CombatStageSoPiece piece, CombatStageSoChoices choices)
        {
            if (SoAsset("정의", piece.definitionGuid, choices, out string picked))
                TryEditSo(piece, new Vector2Int(piece.x, piece.y), piece.rotation, picked, "정의 변경", () => piece.definitionGuid = picked);
            DrawSoPlacement(piece, piece.x, piece.y, piece.rotation, true);
            DrawSoDeleteButton(piece);
        }

        private void DrawSoMarker(CombatStageSoMarker marker, bool terrain)
        {
            if (SoAsset(terrain ? "지형" : "오브젝트", marker.definitionGuid, terrain ? soCatalog.Terrains : soCatalog.Objects, out string picked))
            {
                // 외형 회전을 쓰지 않는 지형(이웃 모양·토네이도)으로 바꾸면 같은 변경에서 외형 회전을 지웁니다. 남기면 전투 화면이 멈춥니다(병합 대응 설계 MG3).
                var rotations = soDocument.view.terrainRotations;
                bool dropRotation = terrain && SoFixedRotationReason(picked) != null && rotations.Exists(rotation => rotation.sourceId == marker.sourceId);
                if (TryEditSo(marker, new Vector2Int(marker.x, marker.y), 0, picked, "정의 변경", () =>
                    {
                        marker.definitionGuid = picked;
                        if (dropRotation) rotations.RemoveAll(rotation => rotation.sourceId == marker.sourceId);
                    }) && dropRotation)
                    SetSoNotice($"이 지형은 외형 회전을 쓰지 않아 지웠습니다({SoFixedRotationReason(picked)}).");
            }
            EditorGUILayout.LabelField("sourceId", marker.sourceId.ToString());
            if (!marker.randomizeInitialPosition) DrawSoPlacement(marker, marker.x, marker.y, 0, false);
            DrawSoOwners(marker);
            if (terrain && SoToggle("무작위 시작", marker.randomizeInitialPosition, out bool random))
            {
                SoChange("무작위 시작 변경", () => marker.randomizeInitialPosition = random);
                GUIUtility.ExitGUI();
            }
            if (terrain && CombatStageSoAssets.IsTornado(marker.definitionGuid))
            {
                EditorGUI.BeginChangeCheck();
                int sign = EditorGUILayout.IntPopup(SoTornadoSignLabel, marker.tornadoHorizontalSign, SoTornadoSignNames, SoTornadoSignValues);
                if (EditorGUI.EndChangeCheck() && sign != marker.tornadoHorizontalSign)
                    SoChange("토네이도 가로 방향 변경", () => marker.tornadoHorizontalSign = sign);
            }
            if (soDocument.HasView)
                DrawSoViewRotation(marker, terrain ? soDocument.view.terrainRotations : soDocument.view.objectRotations,
                    terrain ? SoFixedRotationReason(marker.definitionGuid) : null);
            DrawSoDeleteButton(marker);
        }

        // 토네이도 최초 가로 방향입니다(토네이도 View 계획 TP-12). 저장 값은 CombatStageEnvironmentInput.tornadoInitialHorizontalSign과 같습니다.
        private static readonly GUIContent SoTornadoSignLabel = new GUIContent("가로 방향",
            "처음 움직일 가로 방향입니다. 왼쪽·오른쪽 가장자리 중심은 안쪽으로 정해져 이 값을 쓰지 않고, 그 밖의 고정 중심은 자동으로 둘 수 없습니다.");
        private static readonly GUIContent[] SoTornadoSignNames = { new GUIContent("자동"), new GUIContent("왼쪽"), new GUIContent("오른쪽") };
        private static readonly int[] SoTornadoSignValues = { 0, -1, 1 };

        // 옮기기·회전은 바로 막기(맵 밖·같은 층 겹침)를 거칩니다. 옮기면 고른 칸도 따라갑니다.
        private void DrawSoPlacement(object item, int x, int y, int rotation, bool rotates)
        {
            if (SoInt("X", x, out int nextX) && TryEditSo(item, new Vector2Int(nextX, y), rotation, soDocument.DefinitionOf(item), "배치 옮기기",
                    () => soDocument.Move(item, new Vector2Int(nextX, y))))
                FollowSoMove(new Vector2Int(nextX, y));
            if (SoInt("Y", y, out int nextY) && TryEditSo(item, new Vector2Int(x, nextY), rotation, soDocument.DefinitionOf(item), "배치 옮기기",
                    () => soDocument.Move(item, new Vector2Int(x, nextY))))
                FollowSoMove(new Vector2Int(x, nextY));
            if (rotates && SoEnum("회전", rotation, soCatalog.Names.rotation, out int nextRotation))
                TryEditSo(item, new Vector2Int(x, y), nextRotation, soDocument.DefinitionOf(item), "회전 변경", () => soDocument.SetRotation(item, nextRotation));
            if (rotates && GUILayout.Button("시계 방향 회전 (R)")) RotateSo(item);
        }

        private void FollowSoMove(Vector2Int cell)
        {
            soSelectedCell = cell;
            GUIUtility.ExitGUI();
        }

        private bool TryEditSo(object item, Vector2Int cell, int rotation, string definition, string label, Action apply)
        {
            var grid = SoGrid();
            if (grid == null)
            {
                SetSoNotice("필드 SO가 없어 배치를 고칠 수 없습니다.", MessageType.Warning);
                return false;
            }
            var cells = soDocument.CellsFor(item, cell.x, cell.y, rotation, definition, grid);
            string error = soDocument.PlacementError(soDocument.LayerOf(item), item, definition, cells, grid);
            if (error != null)
            {
                SetSoNotice(error, MessageType.Warning);
                return false;
            }
            SoChange(label, apply);
            return true;
        }

        private void DrawSoOwners(CombatStageSoMarker marker)
        {
            string[] names = soCatalog.Names.owner;
            if (marker.knownTo == null || (names != null && marker.knownTo.Any(index => index < 0 || index >= names.Length)))
            {
                EditorGUILayout.HelpBox("공개 대상 목록에 잘못된 값이 있습니다. 목록을 비운 뒤 공개 대상을 다시 선택하세요.", MessageType.Error);
                if (GUILayout.Button("잘못된 공개 대상 목록 비우기"))
                {
                    SoChange("공개 대상 목록 복구", () => marker.knownTo = new List<int>());
                    GUIUtility.ExitGUI();
                }
                return;
            }
            if (names == null)
            {
                EditorGUILayout.LabelField("공개 대상", Owners(marker.knownTo));
                return;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("공개 대상");
                for (int i = 0; i < names.Length; i++)
                {
                    bool on = marker.knownTo.Contains(i);
                    bool next = GUILayout.Toggle(on, names[i], EditorStyles.miniButton);
                    if (next == on) continue;
                    int owner = i;
                    SoChange("공개 대상 변경", () =>
                    {
                        if (next) marker.knownTo.Add(owner);
                        else marker.knownTo.Remove(owner);
                    });
                }
            }
        }

        // 작업본 맵 키의 지금 맵 생성 상태와 이 스테이지의 자리입니다(병합 대응 설계 MG9).
        // 목록 새로고침 때의 캐시로 그립니다. 도구 밖에서 바꾼 표·맵 SO는 새로고침 뒤에 보입니다(MG-P7).
        private void DrawSoChapterLine()
        {
            int mapKey = soDocument.mapKey;
            var result = CombatStageSoChapter.Check(mapKey, CombatStageSoChapter.MapFor(soCatalog.MapRules, mapKey), soCatalog.ChapterStages);
            EditorGUILayout.LabelField("이 스테이지", CombatStageSoChapter.RoleOf(soCatalog.ChapterStages, soDocument.rootGuid));
            EditorGUILayout.LabelField($"맵 {mapKey} 생성", CombatStageSoChapter.Summary(result), EditorStyles.wordWrappedLabel);
            if (!result.HasMap)
                EditorGUILayout.HelpBox($"맵 키 {mapKey}의 맵 SO가 없습니다(이 맵 키로는 Run이 만들어지지 않음).", MessageType.Info);
            else if (result.Problem != null)
                EditorGUILayout.HelpBox($"이 맵은 지금 Run 생성이 멈춥니다: {result.Problem}", MessageType.Error);
        }

        private bool SoUsesEdgeShapes(string definitionGuid) => CombatStageSoAssets.UsesEdgeShapes(definitionGuid, soCatalog.EdgeShapeKinds);

        // 이웃 모양 지형은 이웃 칸이 모양과 회전을 정합니다. 외형 회전이 적혀 있으면(R0이어도) 전투 화면이 멈춰 팝업을 두지 않습니다(병합 대응 설계 MG2).
        // 지형 외형 회전을 쓰지 않는 까닭입니다. 쓸 수 있으면 null입니다. 토네이도는 Core가 지형 분류에서 빼서 회전이 있으면 전투 진입 때 멈춥니다.
        private string SoFixedRotationReason(string definitionGuid) =>
            SoUsesEdgeShapes(definitionGuid) ? "이웃 모양이 정함" :
            CombatStageSoAssets.IsTornado(definitionGuid) ? "토네이도는 회전 없음" : null;

        // fixedReason이 있으면 회전을 고를 수 없고, 이미 적힌 항목은 지우게만 합니다.
        private void DrawSoViewRotation(CombatStageSoMarker marker, List<CombatStageSoRotation> rotations, string fixedReason)
        {
            var entry = rotations.Find(rotation => rotation.sourceId == marker.sourceId);
            if (fixedReason != null)
            {
                EditorGUILayout.LabelField("외형 회전", fixedReason);
                if (entry == null) return;
                EditorGUILayout.HelpBox("이 지형에는 외형 회전을 쓸 수 없습니다. 이대로는 저장할 수 없습니다(전투 화면이 멈춤).", MessageType.Warning);
                if (GUILayout.Button("외형 회전 지우기"))
                {
                    SoChange("외형 회전 지우기", () => rotations.RemoveAll(rotation => rotation.sourceId == marker.sourceId));
                    GUIUtility.ExitGUI();
                }
                return;
            }
            string[] names = soCatalog.Names.rotation;
            if (names == null) return;
            int index = entry == null ? 0 : entry.rotation + 1;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup("외형 회전", index, new[] { "(지정 안 함)" }.Concat(names).ToArray());
            if (!EditorGUI.EndChangeCheck() || next == index) return;
            SoChange("외형 회전 변경", () =>
            {
                rotations.RemoveAll(rotation => rotation.sourceId == marker.sourceId);
                if (next > 0) rotations.Add(new CombatStageSoRotation { sourceId = marker.sourceId, rotation = next - 1 });
            });
        }

        private void DrawSoDeleteButton(object item)
        {
            if (!GUILayout.Button("이 배치 지우기")) return;
            SoChange($"{CombatStageSoDocument.LayerName(soDocument.LayerOf(item))} 지우기", () => soDocument.Remove(item));
            GUIUtility.ExitGUI();
        }

        private void DrawSoStageValues()
        {
            var document = soDocument;
            var names = soCatalog.Names;
            EditorGUILayout.LabelField("기본 정보", EditorStyles.boldLabel);
            if (SoInt("스테이지 키", document.stageKey, out int stageKey)) SoChange("스테이지 키 변경", () => document.stageKey = stageKey);
            if (SoInt("맵 키", document.mapKey, out int mapKey)) SoChange("맵 키 변경", () => document.mapKey = mapKey);
            if (SoEnum("맵 역할", document.mapRole, names.mapRole, out int role)) SoChange("맵 역할 변경", () => document.mapRole = role);
            if (SoInt("시드", document.timelineSeed, out int seed)) SoChange("시드 변경", () => document.timelineSeed = seed);
            if (SoEnum("승리 조건", document.objectiveKind, names.objective, out int objective)) SoChange("승리 조건 변경", () => document.objectiveKind = objective);
            if (SoInt("적 레벨", document.enemyLevel, out int level)) SoChange("적 레벨 변경", () => document.enemyLevel = level);
            if (SoInt("벽 모서리", document.cornerCutTicks, out int corner)) SoChange("벽 모서리 변경", () => document.cornerCutTicks = corner);
            // 메인 캐릭터는 첫 손패에서 시작하고 자리는 플레이어가 정합니다. SO에 남은 값을 보여만 줍니다(추가 계획 P-A1).
            var main = document.mainCharacter;
            EditorGUILayout.LabelField("메인 캐릭터 자리", $"({main.x}, {main.y}) · {CombatStageSoAssets.NameAt(names.rotation, main.rotation)} · 전투에서 쓰이지 않음");
            EditorGUILayout.LabelField("적 유닛 · 구조물", $"{document.enemyUnits.Count} · {document.structures.Count}");
            EditorGUILayout.LabelField("지형 · 장애물 · 오브젝트", $"{document.terrains.Count} · {document.obstacles.Count} · {document.objects.Count}");
            if (document.HasLegacyPlacements)
                EditorGUILayout.HelpBox($"숨김 호환 벽 {document.legacyWallCount}칸, 지형 {document.legacyTerrainSourceIds.Count}개가 있습니다. SO 모드는 이 값을 편집하지 않습니다.", MessageType.Warning);
        }

        private void DrawSoRegistration()
        {
            var document = soDocument;
            EditorGUILayout.LabelField("등록·맵 생성 규칙", EditorStyles.boldLabel);
            Object root = CombatStageSoAssets.Load(document.rootGuid), view = CombatStageSoAssets.Load(document.viewGuid);
            bool rootRegistered = root != null && soCatalog.RegisteredRoots.Contains(root);
            bool viewRegistered = view != null && soCatalog.RegisteredViews.Contains(view);
            EditorGUILayout.LabelField("Root 표", rootRegistered ? "등록" : "미등록");
            EditorGUILayout.LabelField("View 표", view == null ? "View 없음" : viewRegistered ? "등록" : "미등록");
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(view == null || (rootRegistered && viewRegistered)))
                    if (GUILayout.Button("표에 등록")) { SetSoRegistered(true); GUIUtility.ExitGUI(); }
                using (new EditorGUI.DisabledScope(!rootRegistered && !viewRegistered))
                    if (GUILayout.Button("표에서 빼기")) { SetSoRegistered(false); GUIUtility.ExitGUI(); }
            }
            DrawSoChapterLine();
        }

        private void DrawSoGimmicks()
        {
            var document = soDocument;
            EditorGUILayout.LabelField("레일", EditorStyles.boldLabel);
            if (SoToggle("레일 사용", document.railwayEnabled, out bool railway)) SoChange("레일 사용 변경", () => document.railwayEnabled = railway);
            if (SoInt("레일 첫 발동(진영 종료 횟수)", document.railFirstActivation, out int first)) SoChange("레일 첫 발동 변경", () => document.railFirstActivation = first);
            if (SoInt("레일 반복(진영 종료 횟수)", document.railRepeatInterval, out int repeat)) SoChange("레일 반복 변경", () => document.railRepeatInterval = repeat);
            EditorGUILayout.LabelField("레일 칸", document.railCells.Count.ToString());
            DrawSoRockfall(document);
            DrawSoHaboob(document);
        }

        // 낙석 값입니다(설계 §17 RF3). 꺼도 값은 지우지 않습니다. 값 규칙은 저장 검사가 Core로 판단합니다(RF4).
        private void DrawSoRockfall(CombatStageSoDocument document)
        {
            var rockfall = document.rockfall;
            DrawSoGimmickHeading("낙석", false);
            if (SoToggle("낙석 사용", rockfall.enabled, out bool enabled)) SoChange("낙석 사용 변경", () => rockfall.enabled = enabled);
            DrawSoGimmickShape(false);
            _soRockfallDetails = EditorGUILayout.Foldout(_soRockfallDetails, "세부 설정", true);
            if (_soRockfallDetails)
            {
                using (new EditorGUI.DisabledScope(!rockfall.enabled))
                {
                    if (SoInt("대기(진영 종료 횟수)", rockfall.cooldownTurns, out int cooldown)) SoChange("낙석 대기 변경", () => rockfall.cooldownTurns = cooldown);
                    if (SoInt("경고(진영 종료 횟수)", rockfall.warningTurns, out int warning)) SoChange("낙석 경고 변경", () => rockfall.warningTurns = warning);
                    if (SoInt("돌 수 최소", rockfall.minimumRocks, out int minimum)) SoChange("낙석 돌 수 변경", () => rockfall.minimumRocks = minimum);
                    if (SoInt("돌 수 최대", rockfall.maximumRocks, out int maximum)) SoChange("낙석 돌 수 변경", () => rockfall.maximumRocks = maximum);
                    if (SoInt("최대 체력 손실 %", rockfall.maximumHealthLossPercent, out int loss)) SoChange("낙석 체력 손실 변경", () => rockfall.maximumHealthLossPercent = loss);
                    if (SoInt("묶임 턴", rockfall.bindTurns, out int bind)) SoChange("낙석 묶임 턴 변경", () => rockfall.bindTurns = bind);
                }
                using (new EditorGUI.DisabledScope(!rockfall.enabled || !document.HasView))
                {
                    if (SoToggle("터널 폭발 사용", !document.view.disableRockfallTunnelExplosion, out bool explosionEnabled))
                        SoChange("낙석 터널 폭발 변경", () => document.view.disableRockfallTunnelExplosion = !explosionEnabled);
                }
                EditorGUILayout.HelpBox(document.HasView
                    ? "체크를 해제하면 터널 폭발과 폭발 흔들림만 끕니다. 경고 표시·경고음·돌 낙하·착지 효과·피해는 유지됩니다."
                    : "터널 폭발 설정을 저장하려면 짝 View가 필요합니다. 짝 View를 만든 뒤 설정해 주세요.", MessageType.Info);
            }
            _soRockfallCoordinates = EditorGUILayout.Foldout(_soRockfallCoordinates, "고급 좌표", true);
            if (_soRockfallCoordinates)
            {
                using (new EditorGUI.DisabledScope(!rockfall.enabled)) DrawSoRockfallFootprint(rockfall.footprintOffsets);
            }
            // 맵 SO는 조립해 봐야 터널·능선 자리를 알 수 있어 검사하지 않습니다(Q-RF3 A).
            if (rockfall.enabled && document.HasView && SoBackgroundMode(document.view) == CombatStageSoBackground.MapDefinition)
                EditorGUILayout.HelpBox("맵 SO 방식은 터널·능선 자리를 확인하지 않습니다.", MessageType.Info);
        }

        // 돌 하나가 차지하는 칸입니다(Q-RF1 A 줄 목록). 빈 목록도 막지 않고 저장 검사에 맡깁니다(계획 P-RF4).
        private void DrawSoRockfallFootprint(List<Vector2Int> offsets)
        {
            EditorGUILayout.LabelField("돌 하나의 칸(오프셋)");
            for (int i = 0; i < offsets.Count; i++)
            {
                int index = i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    int x = EditorGUILayout.DelayedIntField(offsets[i].x);
                    int y = EditorGUILayout.DelayedIntField(offsets[i].y);
                    if (EditorGUI.EndChangeCheck()) SoChange("낙석 칸 변경", () => offsets[index] = new Vector2Int(x, y));
                    if (GUILayout.Button("지우기", GUILayout.Width(52)))
                    {
                        SoChange("낙석 칸 지우기", () => offsets.RemoveAt(index));
                        GUIUtility.ExitGUI();
                    }
                }
            }
            if (GUILayout.Button("칸 추가"))
            {
                int x = 0;
                while (offsets.Contains(new Vector2Int(x, 0))) x++;
                SoChange("낙석 칸 추가", () => offsets.Add(new Vector2Int(x, 0)));
                GUIUtility.ExitGUI();
            }
        }

        // 아군 배치 구역입니다(설계 §11 AZ5). 칸 목록이 있으면 그 칸, 비면 필드 아군 줄 수(아래부터)입니다. 적 배치 구역은 없습니다(Core `5a1bfbe1f`).
        // 줄 수는 필드 SO 값이라, 다른 스테이지도 쓰는 필드면 이 스테이지 전용으로 복제한 뒤 바꿉니다. 값은 공유 SO 편집처럼 작업본에 담고 저장 때 씁니다(SO-d).
        private void DrawSoPlayerZone()
        {
            CombatStageSoField field;
            try { field = SoFieldOf(); }
            catch (InvalidOperationException) { field = null; }
            if (field == null) return;
            var zone = SoZoneOf(field);
            EditorGUILayout.LabelField("아군 배치 구역", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("정한 곳", zone.IsListed ? $"칸 {zone.ListedCount}개 (직접 지정)" : $"필드 아군 줄 수 {zone.RowDepth}줄 (칸 목록 비어 있음)");
            if (zone.IsListed)
            {
                EditorGUILayout.LabelField($"필드 아군 줄 수 {field.PlayerDepth} (칸 목록이 있어 이 스테이지에서는 쓰이지 않음)", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("칸 목록 비우기(필드 줄 수로)")) { ClearSoZoneCells(zone, field); GUIUtility.ExitGUI(); }
            }
            else
            {
                string guid = soDocument.fieldGuid;
                int others = soCatalog.OtherUsers(guid, soDocument.combatGuid, soDocument.viewGuid);
                EditorGUI.BeginChangeCheck();
                // 범위는 Core 규칙 그대로입니다: 필드 1 이상, 판 높이 이하(PlayerDeploymentZone.FitsBoard). 적 칸이 없어지는 것은 경고가 알립니다(설계 P-AZ7).
                int player = EditorGUILayout.IntSlider("필드 아군 줄 수", field.PlayerDepth, 1, Mathf.Max(1, field.Rows));
                if (EditorGUI.EndChangeCheck() && player != field.PlayerDepth)
                {
                    if (others > 0) guid = CloneSoShared("필드", guid, picked => soDocument.fieldGuid = picked);
                    if (!string.IsNullOrEmpty(guid)) SetSoPlayerDepth(guid, player);
                    GUIUtility.ExitGUI();
                }
                if (others > 0)
                    EditorGUILayout.LabelField($"필드를 다른 스테이지 {others}개가 함께 씁니다. 바꾸면 이 스테이지 전용 필드를 복제합니다.", EditorStyles.wordWrappedMiniLabel);
            }
            var grid = new CombatStageSoGrid(field.Columns, field.Rows, soCatalog.Footprint, soCatalog.Names.IsQuarterTurn);
            var placements = soDocument.Placements(grid);
            var warnings = new List<string>();
            // ① 고정 적 유닛·적 구조물·본부는 아군 구역 밖이어야 합니다(Core BattlePreparationCalculator.Place). 판정은 Core DeploymentZonePolicy 그대로입니다(계획 P-AZT3).
            int inside = placements.Count(placed =>
                (placed.Layer == CombatStageSoLayer.Unit || placed.Layer == CombatStageSoLayer.Structure || placed.Layer == CombatStageSoLayer.Headquarters) &&
                !zone.EnemyAllowed(placed.Cells));
            if (inside > 0) warnings.Add($"아군 배치 구역 안에 고정 적 배치(적 유닛·구조물·본부)가 {inside}개 있습니다. 전투 진입 때 Core가 멈춥니다.");
            // 참고 수(설계 §11 AZ6). 벽·낭떠러지는 지형 정의 종류로 고른 근사라 "약"입니다(P-AZ9). 적이 놓일 칸은 Core 출현 후보
            // (EnemySpawnCalculator: 판 − 아군 구역 − 구조물·장애물 예약 − 벽·낭떠러지)와 같은 빼기입니다.
            var walls = new HashSet<Vector2Int>(placements
                .Where(placed => placed.Layer == CombatStageSoLayer.Terrain && soCatalog.BlocksEntry(soDocument.DefinitionOf(placed.Item)))
                .SelectMany(placed => placed.Cells));
            var taken = new HashSet<Vector2Int>(placements
                .Where(placed => placed.Layer == CombatStageSoLayer.Obstacle || placed.Layer == CombatStageSoLayer.Structure || placed.Layer == CombatStageSoLayer.Headquarters)
                .SelectMany(placed => placed.Cells));
            int zoneCells = 0, playerOpen = 0, enemyOpen = 0;
            for (int y = 0; y < field.Rows; y++)
            {
                for (int x = 0; x < field.Columns; x++)
                {
                    var cell = new Vector2Int(x, y);
                    bool open = !walls.Contains(cell) && !taken.Contains(cell);
                    if (zone.Contains(cell)) { zoneCells++; if (open) playerOpen++; }
                    else if (open) enemyOpen++;
                }
            }
            int? spawn = SoSpawnCount();
            EditorGUILayout.LabelField($"아군 구역 {zoneCells}칸(놓을 수 있는 칸 약 {playerOpen}) · 적이 놓일 수 있는 칸 약 {enemyOpen}" +
                (spawn.HasValue ? $" · 적 로드아웃 출현 {spawn.Value}" : ""), EditorStyles.wordWrappedMiniLabel);
            // ② 출현 수가 후보보다 많으면 Core가 멈춥니다(EnemySpawnCalculator). ③ 아군을 하나도 놓을 수 없습니다.
            // ④ 고정 적 유닛과 적 로드아웃을 함께 쓰면 Core가 멈춥니다(BattleDefinitionAdapter, Q-AZ4 A).
            if (spawn > enemyOpen) warnings.Add($"적 로드아웃 출현 수({spawn.Value})가 적이 놓일 수 있는 칸(약 {enemyOpen})보다 많습니다. 전투 진입 때 멈출 수 있습니다.");
            if (playerOpen == 0) warnings.Add("아군 배치 구역에 아군을 놓을 수 있는 칸이 없습니다.");
            if (spawn.HasValue && soDocument.enemyUnits.Count > 0) warnings.Add("고정 적 배치와 적 로드아웃은 함께 쓸 수 없습니다. 전투 진입 때 멈춥니다.");
            // 모두 경고만 하고 저장·등록은 막지 않습니다(설계 Q-AZ3 A).
            if (warnings.Count > 0) EditorGUILayout.HelpBox(string.Join("\n", warnings), MessageType.Warning);
        }

        // 칸 목록을 비워 필드 아군 줄 수 구역으로 돌아갑니다(설계 §11 AZ5 (3)). 확인 창 뒤 Undo 하나입니다.
        // 마지막 칸 지우기는 막으므로(Q-AZ2 A) 줄 수로 돌아가는 길은 이것 하나입니다.
        private void ClearSoZoneCells(CombatStageSoZone zone, CombatStageSoField field)
        {
            string message = $"아군 배치 구역이 필드 아군 줄 수 {field.PlayerDepth}줄로 돌아갑니다.\n칸 목록 {zone.ListedCount}칸을 비울까요?";
            if (!EditorUtility.DisplayDialog("칸 목록 비우기", message, "비우기", "취소")) return;
            SoChange("아군 구역 칸 목록 비우기", () => soDocument.playerDeploymentCells.Clear());
            SetSoNotice($"칸 목록을 비웠습니다. 아군 배치 구역은 필드 아군 줄 수 {field.PlayerDepth}줄입니다.");
        }

        // 적 로드아웃 출현 수입니다. 로드아웃이 없으면 null입니다. 이 스테이지만 쓰는 로드아웃을 고치는 중이면 그 편집 값을 읽습니다(SoFieldOf와 같은 길).
        private int? SoSpawnCount()
        {
            string guid = soDocument.enemyLoadoutGuid;
            if (string.IsNullOrEmpty(guid)) return null;
            return CombatStageSoAssets.SpawnCountOf(soDocument.SharedEdit(guid) != null ? SharedCopy(guid) : CombatStageSoAssets.Load(guid));
        }

        // 필드 복제본에 아군 줄 수를 쓰고, 값은 작업본의 공유 편집 JSON에 담습니다(DrawSoSharedEditor와 같은 길). 적 줄 수는 그대로 둡니다(계획 P-AZT9).
        private void SetSoPlayerDepth(string fieldGuid, int player)
        {
            Object copy = SharedCopy(fieldGuid);
            if (copy == null) return;
            CombatStageSoAssets.WritePlayerDepth(copy, player);
            string json = EditorJsonUtility.ToJson(copy);
            SoChange("아군 줄 수 변경", () => soDocument.SetSharedEdit(fieldGuid, json));
        }

        private void DrawSoFieldReference()
        {
            var document = soDocument;
            DrawSoReference("필드", document.fieldGuid, soCatalog.Fields, picked => document.fieldGuid = picked, true);
            DrawSoNewField();
        }

        private void DrawSoReferences()
        {
            var document = soDocument;
            DrawSoReference("로스터", document.rosterGuid, soCatalog.Rosters, picked => document.rosterGuid = picked, true);
            DrawSoReference("경제", document.economyGuid, soCatalog.Economies, picked => document.economyGuid = picked, true);
            DrawSoReference("적 로드아웃", document.enemyLoadoutGuid, soCatalog.Loadouts, picked => document.enemyLoadoutGuid = picked, true);
            // 본부 정의를 바꾸면 본부 크기가 바뀌므로 바로 막기를 거칩니다.
            if (SoAsset("본부 정의", document.headquartersGuid, soCatalog.Structures, out string headquarters))
            {
                var pose = document.enemyHeadquarters;
                if (string.IsNullOrEmpty(headquarters)) SoChange("본부 정의 변경", () => document.headquartersGuid = "");
                else TryEditSo(pose, new Vector2Int(pose.x, pose.y), pose.rotation, headquarters, "본부 정의 변경", () => document.headquartersGuid = headquarters);
            }
            DrawSoShared(document.headquartersGuid, false);
            EditorGUILayout.LabelField("구조물 목록", EditorStyles.boldLabel);
            for (int i = 0; i < document.structureCatalog.Count; i++)
            {
                int index = i;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (SoAsset($"{i + 1}", document.structureCatalog[i], soCatalog.Structures, out string picked))
                        SoChange("구조물 목록 변경", () => document.structureCatalog[index] = picked);
                    if (GUILayout.Button("빼기", GUILayout.Width(40)))
                    {
                        SoChange("구조물 목록에서 빼기", () => document.structureCatalog.RemoveAt(index));
                        GUIUtility.ExitGUI();
                    }
                }
            }
            if (GUILayout.Button("구조물 목록에 더하기"))
            {
                SoChange("구조물 목록에 더하기", () => document.structureCatalog.Add(""));
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.LabelField("다른 스테이지와 함께 쓰는 SO는 읽기만 합니다. 이 스테이지만 쓰는 필드·로스터·경제·적 로드아웃은 바로 고치고, 스테이지를 저장할 때 검사 뒤 씁니다.", EditorStyles.wordWrappedMiniLabel);
        }

        // cloneable: 필드·로스터·경제·적 로드아웃처럼 전용 복제와 바로 고치기를 할 수 있는 공유 SO입니다(설계 SM7, 계획 P4).
        private void DrawSoReference(string label, string guid, CombatStageSoChoices choices, Action<string> apply, bool cloneable)
        {
            if (SoAsset(label, guid, choices, out string picked)) SoChange(label + " 변경", () => apply(picked));
            int others = soCatalog.OtherUsers(guid, soDocument.combatGuid, soDocument.viewGuid);
            if (cloneable && others > 0 && GUILayout.Button($"이 스테이지 전용으로 복제 (다른 스테이지 {others}개가 함께 씀)"))
            {
                CloneSoShared(label, guid, apply);
                GUIUtility.ExitGUI();
            }
            DrawSoShared(guid, cloneable);
        }

        // 참조한 SO의 값을 SO 자신의 필드 그대로 보여 줍니다. 그 SO의 필드 이름을 도구에 두지 않으려고 PropertyField를 씁니다.
        // 이 스테이지만 쓰는 SO면 저장하지 않는 복제본에서 고치고, 값은 작업본에 담습니다(T17).
        private void DrawSoShared(string guid, bool cloneable)
        {
            if (string.IsNullOrEmpty(guid)) return;
            int others = soCatalog.OtherUsers(guid, soDocument.combatGuid, soDocument.viewGuid);
            bool editable = cloneable && others == 0;
            string usage = others > 0 ? $"다른 스테이지 {others}개와 공유" : "이 스테이지만 씀";
            string edited = soDocument.SharedEdit(guid) != null ? " · 저장 안 한 변경" : "";
            bool open = soExpanded.Contains(guid);
            using (new EditorGUI.IndentLevelScope())
            {
                bool next = EditorGUILayout.Foldout(open, $"{CombatStageSoAssets.NameOf(guid)} 값 · {usage}{edited}", true);
                if (next != open)
                {
                    if (next) soExpanded.Add(guid);
                    else soExpanded.Remove(guid);
                }
                if (!next) return;
                // 필드 SO에는 적 배치 깊이 칸이 남아 있지만 Core가 읽지 않습니다(설계 P-AZ8). 필드 SO 툴팁은 DB 담당이 정할 일이라 여기서만 알립니다.
                if (CombatStageSoAssets.IsType(CombatStageSoAssets.Load(guid), CombatStageSoAssets.FieldType))
                    EditorGUILayout.LabelField("적 배치 깊이(Enemy Deployment Depth)는 Core가 읽지 않습니다. 아군 배치 구역은 '스테이지 설정 → 필드·아군 구역'에서 정합니다.",
                        EditorStyles.wordWrappedMiniLabel);
                if (editable)
                {
                    DrawSoSharedEditor(guid);
                    return;
                }
                if (!soShared.TryGetValue(guid, out var values) || values.targetObject == null)
                {
                    Object asset = CombatStageSoAssets.Load(guid);
                    if (asset == null)
                    {
                        EditorGUILayout.LabelField("에셋을 찾을 수 없습니다.", EditorStyles.miniLabel);
                        return;
                    }
                    soShared[guid] = values = new SerializedObject(asset);
                }
                values.Update();
                using (new EditorGUI.DisabledScope(true))
                {
                    SerializedProperty property = values.GetIterator();
                    for (bool enter = true; property.NextVisible(enter); enter = false)
                        if (property.propertyPath != "m_Script") EditorGUILayout.PropertyField(property, true);
                }
            }
        }

        // 저장하지 않는 복제본을 PropertyField로 고치고, 바뀐 값은 JSON으로 작업본에 담습니다. 키 필드는 표 안에서 유일해야 해 막습니다.
        private void DrawSoSharedEditor(string guid)
        {
            Object copy = SharedCopy(guid);
            if (copy == null)
            {
                EditorGUILayout.LabelField("에셋을 찾을 수 없습니다.", EditorStyles.miniLabel);
                return;
            }
            var values = new SerializedObject(copy);
            EditorGUI.BeginChangeCheck();
            SerializedProperty property = values.GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false)
            {
                if (property.propertyPath == "m_Script") continue;
                using (new EditorGUI.DisabledScope(CombatStageSoAssets.IsSharedKeyProperty(copy, property.propertyPath)))
                    EditorGUILayout.PropertyField(property, true);
            }
            if (!EditorGUI.EndChangeCheck()) return;
            values.ApplyModifiedPropertiesWithoutUndo();
            string json = EditorJsonUtility.ToJson(copy);
            SoChange("공유 값 변경", () => soDocument.SetSharedEdit(guid, json));
        }

        // 공유 SO 편집용 복제본입니다. 값은 작업본의 JSON(없으면 에셋 값)에 맞춰, Undo로 작업본이 바뀌어도 따라가게 합니다.
        private Object SharedCopy(string guid)
        {
            Object asset = CombatStageSoAssets.Load(guid);
            if (asset == null) return null;
            if (!soSharedCopies.TryGetValue(guid, out var working) || working.Copy == null)
            {
                Object copy = Object.Instantiate(asset);
                copy.name = asset.name;
                copy.hideFlags = HideFlags.DontSave;
                soSharedCopies[guid] = working = new SoWorkingCopy { Copy = copy, Json = null };
            }
            string json = soDocument.SharedEdit(guid)?.json ?? "";
            if (working.Json != json)
            {
                EditorJsonUtility.FromJsonOverwrite(json.Length > 0 ? json : EditorJsonUtility.ToJson(asset), working.Copy);
                working.Json = json;
            }
            return working.Copy;
        }

        private void ClearSharedCopies()
        {
            foreach (var working in soSharedCopies.Values)
                if (working.Copy != null) Object.DestroyImmediate(working.Copy);
            soSharedCopies.Clear();
        }

        // 맵 크기는 필드에 저장 안 한 편집이 있으면 그 값을 씁니다(T17).
        private CombatStageSoField SoFieldOf()
        {
            string guid = soDocument.fieldGuid;
            return soDocument.SharedEdit(guid) != null ? CombatStageSoAssets.ReadField(SharedCopy(guid)) : soCatalog.Field(guid);
        }

        private string SoStageName() => CombatStageSoAssets.ShortAssetName(CombatStageSoAssets.NameOf(soDocument.rootGuid));

        // 공유 SO를 복사해 이 스테이지 전용을 만들고 표에 등록합니다. 참조 바꾸기는 작업본에서 하므로 저장해야 반영됩니다(T16).
        // 만든 복제본의 GUID를 돌려줍니다. 취소하거나 못 만들면 null입니다.
        private string CloneSoShared(string label, string guid, Action<string> apply)
        {
            Object source = CombatStageSoAssets.Load(guid);
            if (source == null) return null;
            string message = $"공유 {label} SO를 복사해 이 스테이지만 쓰는 새 SO를 만들고 표에 등록합니다.\n원본: {source.name}\n\n" +
                "참조는 스테이지를 저장해야 바뀝니다. 저장하지 않으면 새 SO는 쓰는 곳 없이 남습니다.";
            if (!EditorUtility.DisplayDialog("전용 복제", message, "만들기", "취소")) return null;
            var problems = new List<string>();
            Object copy = CombatStageSoCreation.CloneShared(source, SoStageName(), soDocument.stageKey, problems);
            if (copy == null)
            {
                SetSoNotice("전용 복제를 하지 못했습니다:\n" + string.Join("\n", problems), MessageType.Error);
                return null;
            }
            string copyGuid = CombatStageSoAssets.GuidOf(copy);
            SoChange($"{label} 전용 복제", () => apply(copyGuid));
            RefreshSoCatalog();
            SetSoNotice($"전용 복제를 만들었습니다(저장해야 참조가 바뀝니다): {copy.name}");
            return copyGuid;
        }

        // 크기를 넣어 새 필드를 만들고 이 스테이지 필드로 겁니다(Q-SM4 A, T18). 입력 기본값은 지금 필드입니다.
        private void DrawSoNewField()
        {
            using (new EditorGUI.IndentLevelScope())
            {
                showSoNewField = EditorGUILayout.Foldout(showSoNewField, "새 필드 만들기", true);
                if (!showSoNewField) return;
                if (soFieldColumns <= 0)
                {
                    CombatStageSoField current;
                    try { current = SoFieldOf(); }
                    catch (InvalidOperationException) { current = null; }
                    soFieldColumns = current?.Columns ?? 8; soFieldRows = current?.Rows ?? 10; soFieldReserve = current?.Reserve ?? 4;
                    soFieldPlayerDepth = current?.PlayerDepth ?? 2;
                }
                soFieldColumns = EditorGUILayout.IntField("가로", soFieldColumns);
                soFieldRows = EditorGUILayout.IntField("세로", soFieldRows);
                soFieldReserve = EditorGUILayout.IntField("대기 칸", soFieldReserve);
                // 적 배치 깊이는 묻지 않습니다. Core가 읽지 않아 지금 필드(틀) 값을 그대로 둡니다(설계 P-AZ8).
                soFieldPlayerDepth = EditorGUILayout.IntField("아군 줄 수(칸 목록이 빌 때)", soFieldPlayerDepth);
                if (GUILayout.Button("만들고 이 스테이지 필드로 쓰기")) { CreateSoField(); GUIUtility.ExitGUI(); }
            }
        }

        private void CreateSoField()
        {
            var problems = new List<string>();
            Object field = CombatStageSoCreation.CreateField(CombatStageSoAssets.Load(soDocument.fieldGuid), soFieldColumns, soFieldRows,
                soFieldReserve, soFieldPlayerDepth, SoStageName(), problems);
            if (field == null)
            {
                SetSoNotice("새 필드를 만들지 못했습니다:\n" + string.Join("\n", problems), MessageType.Error);
                return;
            }
            string fieldGuid = CombatStageSoAssets.GuidOf(field);
            SoChange("새 필드로 바꾸기", () => soDocument.fieldGuid = fieldGuid);
            soFieldColumns = 0;
            RefreshSoCatalog();
            SetSoNotice($"새 필드를 만들어 이 스테이지 필드로 걸었습니다(저장해야 반영): {field.name}");
        }

        private void DrawSoView()
        {
            if (!soDocument.HasView)
            {
                EditorGUILayout.HelpBox("짝 View가 없습니다. 저장하려면 View가 필요합니다.", MessageType.Warning);
                var template = SoTemplate();
                if (template != null && GUILayout.Button($"틀의 View로 만들기 ({template.Root.name})")) { CreateSoView(template); GUIUtility.ExitGUI(); }
                return;
            }
            var view = soDocument.view;
            EditorGUILayout.LabelField("View 에셋", CombatStageSoAssets.NameOf(soDocument.viewGuid));
            // 배경은 방식이 고릅니다(설계 §12 UT2, 게임 StageViewPropData.GameObject·Map).
            DrawSoBackground(view);
            if (SoVector3("위치 보정", view.positionOffset, out Vector3 position)) SoChange("위치 보정 변경", () => view.positionOffset = position);
            if (SoVector3("회전 보정", view.rotationOffset, out Vector3 rotation)) SoChange("회전 보정 변경", () => view.rotationOffset = rotation);
            if (SoVector3("배율", view.scaleOffset, out Vector3 scale)) SoChange("배율 변경", () => view.scaleOffset = scale);
            DrawSoReference("카메라 설정", view.cameraGuid, soCatalog.Cameras, picked => view.cameraGuid = picked, false);
            EditorGUILayout.LabelField("외형 회전", $"지형 {view.terrainRotations.Count} · 오브젝트 {view.objectRotations.Count}");
            DrawSoTileLayout(view);
        }

        #endregion

        #region 입력 칸

        private static bool SoInt(string label, int value, out int next)
        {
            EditorGUI.BeginChangeCheck();
            next = EditorGUILayout.DelayedIntField(label, value);
            return EditorGUI.EndChangeCheck() && next != value;
        }

        private static bool SoToggle(string label, bool value, out bool next)
        {
            EditorGUI.BeginChangeCheck();
            next = EditorGUILayout.Toggle(label, value);
            return EditorGUI.EndChangeCheck() && next != value;
        }

        private static bool SoVector3(string label, Vector3 value, out Vector3 next)
        {
            EditorGUI.BeginChangeCheck();
            next = EditorGUILayout.Vector3Field(label, value);
            return EditorGUI.EndChangeCheck() && next != value;
        }

        // 열거 이름을 아직 모르면 값만 보입니다(계획 P10).
        private static bool SoEnum(string label, int index, string[] names, out int next)
        {
            next = index;
            if (names == null)
            {
                EditorGUILayout.LabelField(label, $"(값 {index})");
                return false;
            }
            EditorGUI.BeginChangeCheck();
            next = EditorGUILayout.Popup(label, index, names);
            return EditorGUI.EndChangeCheck() && next != index;
        }

        private static bool SoAsset(string label, string guid, CombatStageSoChoices choices, out string picked)
        {
            int index = choices.IndexOf(guid);
            EditorGUI.BeginChangeCheck();
            int next = EditorGUILayout.Popup(label, index, choices.Labels);
            bool changed = EditorGUI.EndChangeCheck() && next != index;
            picked = changed ? choices.GuidAt(next) : guid;
            if (index < 0) EditorGUILayout.LabelField(" ", $"목록 밖 에셋: {CombatStageSoAssets.NameOf(guid)}", EditorStyles.miniLabel);
            return changed;
        }

        private static bool SoObject<T>(string label, string guid, out string picked) where T : Object
        {
            var current = CombatStageSoAssets.Load(guid) as T;
            EditorGUI.BeginChangeCheck();
            var next = EditorGUILayout.ObjectField(label, current, typeof(T), false) as T;
            bool changed = EditorGUI.EndChangeCheck() && next != current;
            picked = changed ? CombatStageSoAssets.GuidOf(next) : guid;
            return changed;
        }

        #endregion

        // 지정한 하붑 시작 위치가 덮는 판 안 칸입니다(설계 §25 HAT2). 무작위이거나 값이 잘못되면 비어 있고, 판 밖 칸은 저장 검사가 알립니다.
        private HashSet<Vector2Int> SoHaboobStartCells() =>
            new HashSet<Vector2Int>((soDocument.haboob.StartCells() ?? new List<Vector2Int>())
                .Where(cell => cell.x >= 0 && cell.y >= 0 && cell.x < soField.Columns && cell.y < soField.Rows));

        private CombatStageSoZone SoZoneOf(CombatStageSoField field) => CombatStageSoAssets.ZoneOf(soDocument.playerDeploymentCells, field);

        private static Color ZoneColor(CombatStageSoZone zone, Vector2Int cell) => zone.Contains(cell) ? PlayerZone : NeutralZone;

        private static string ZoneName(CombatStageSoZone zone, Vector2Int cell) => zone.Contains(cell) ? "아군 배치 구역" : "적 배치 가능";

        private string Owners(List<int> owners) =>
            owners.Count == 0 ? "없음" : string.Join(", ", owners.Select(owner => CombatStageSoAssets.NameAt(soCatalog.Names.owner, owner)));
    }
}
