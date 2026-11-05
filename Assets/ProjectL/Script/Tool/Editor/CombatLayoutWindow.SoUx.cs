using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    // 검사 결과의 수명·위치 이동·공개 진단 안내입니다. 규칙은 검사/Document/Visuals가 소유합니다.
    public sealed partial class CombatLayoutWindow
    {
        private long soRevision;
        private CombatStageSoCheckResult soCheck;
        private string soFocusRootGuid, soFocusLabel;
        private long soFocusRevision;
        private CombatStageSoIssue soFocusedIssue;
        private Vector2Int soRepairCell;
        private Vector2Int? soPendingGridCell;
        private bool soPendingDetailScroll;
        private bool soPasteVisibilityBlocked, soShowVisibilityDetails = true;
        private Vector2 soVisibilityScroll;

        private bool SoCheckMatches => soCheck != null && soCheck.Matches(soDocument.rootGuid, soRevision);
        private bool SoCurrentCheck => SoCheckMatches && soCheck.Phase == CombatStageSoCheckPhase.Complete;

        private void InvalidateSoCheck(bool changed = false, bool reset = false)
        {
            bool needsRecheck = !reset && (changed || soCheck != null);
            soRevision++;
            soCheck = needsRecheck
                ? new CombatStageSoCheckResult(soDocument.rootGuid, soRevision, new List<CombatStageSoIssue>(), CombatStageSoCheckPhase.NeedsRecheck)
                : null;
            soNotice = "";
            soPasteVisibilityBlocked = false;
            ClearSoIssueFocus();
        }

        private void ClearSoIssueFocus()
        {
            soFocusedIssue = null;
            soFocusRootGuid = null;
            soPendingGridCell = null;
            soPendingDetailScroll = false;
        }

        private void SetSoCheck(List<CombatStageSoIssue> issues)
        {
            ClearSoIssueFocus();
            _soShowCheckResult = true;
            _soCheckScroll = Vector2.zero;
            soDetailsScroll = Vector2.zero;
            _soLibraryScroll = Vector2.zero;
            soCheck = new CombatStageSoCheckResult(soDocument.rootGuid, soRevision, issues);
            if (issues.Count > 0) soCheck.Complete();
        }

        private void AddSoCheckError(string message)
        {
            if (!SoCheckMatches || soCheck.Phase == CombatStageSoCheckPhase.NeedsRecheck)
                SetSoCheck(new List<CombatStageSoIssue>());
            soCheck.Issues.Add(new CombatStageSoIssue(message));
            soCheck.Complete();
        }

        // 터널이 없으면 게임은 폭발 없이 경고 칸만 켭니다(설계 §17 RF5, 낙석 View 계획 RP-12). 능선 끝점이 빠지면 돌이 칸 위에서 곧게 떨어집니다(낙석 View 계획 RGP-2).
        // 모두 허용된 동작이라 막지 않고 알리기만 합니다. 폭발을 명시적으로 끈 스테이지에서는 터널 누락 안내만 생략합니다(설계 RTE).
        private void AddSoRockfallPropWarnings(Object view)
        {
            if (!soDocument.rockfall.enabled) return;
            if (!soDocument.view.disableRockfallTunnelExplosion
                && CombatStageSoAssets.RockfallObjectCount(soDocument, view, CombatStageSoAssets.RockfallTunnelObjectName) == 0)
                soCheck.Warnings.Add($"맵 프리팹에 {CombatStageSoAssets.RockfallTunnelObjectName}가 없어 낙석 폭발 없이 경고 칸만 나옵니다.");
            if (CombatStageSoAssets.RockfallObjectCount(soDocument, view, CombatStageSoAssets.RockfallRidgeStartObjectName) == 0
                || CombatStageSoAssets.RockfallObjectCount(soDocument, view, CombatStageSoAssets.RockfallRidgeEndObjectName) == 0)
                soCheck.Warnings.Add($"맵 프리팹에 {CombatStageSoAssets.RockfallRidgeStartObjectName}·{CombatStageSoAssets.RockfallRidgeEndObjectName}가 모두 있지 않아 낙석 돌이 칸 위에서 곧게 떨어집니다.");
        }

        // 맵 규칙의 판단은 기존 JudgeSoChapter가 맡고, 여기서는 그 결과만 표시합니다.
        private void RecordSoChapterCheck(CombatStageSoChapterJudgement judgement, CombatStageSoChapterStage swap)
        {
            if (judgement.Breaks)
                soCheck.Issues.Add(new CombatStageSoIssue("맵 생성 규칙에 걸립니다.\n" + judgement.Describe()));
            else
            {
                if (swap != null)
                    soCheck.Warnings.Add($"기존 보스 {swap.Label}를 표에서 빼는 조건으로 진행할 수 있습니다. 실행 시 확인이 필요합니다.");
                if (judgement.AlreadyBroken)
                    soCheck.Warnings.Add("이번 변경 전부터 맵 생성 규칙에 문제가 있습니다. 실행 시 확인이 필요합니다.\n" + judgement.Describe());
            }
            soCheck.Complete();
        }

        private string SoCheckStatus()
        {
            if (!SoCheckMatches) return "미검사";
            if (soCheck.Phase == CombatStageSoCheckPhase.NeedsRecheck) return "변경 후 재검사 필요";
            if (soCheck.Phase == CombatStageSoCheckPhase.Checking) return "검사 확인 중";
            if (soCheck.Issues.Count > 0) return $"검사 오류 {soCheck.Issues.Count}건";
            return soCheck.Warnings.Count > 0 ? $"조건부 확인 · 경고 {soCheck.Warnings.Count}건" : "검사 통과";
        }

        private MessageType SoCheckMessageType() => !SoCheckMatches ? MessageType.Info
            : soCheck.Issues.Count > 0 ? MessageType.Error
            : soCheck.Warnings.Count > 0 || soCheck.Phase == CombatStageSoCheckPhase.NeedsRecheck ? MessageType.Warning
            : MessageType.Info;

        private void DrawSoWorkStatus()
        {
            if (!soDocument.IsOpen)
            {
                EditorGUILayout.LabelField("열린 스테이지 없음", EditorStyles.wordWrappedLabel);
                return;
            }
            EditorGUILayout.LabelField($"{CombatStageSoAssets.NameOf(soDocument.rootGuid)} · 스테이지 키 {soDocument.stageKey}", EditorStyles.wordWrappedLabel);
            string view = soMapView == SoViewTiles ? "타일 보기" : soMapView == SoView2D ? "2D · 전체 배치"
                : soPlayerOnly ? "3D · Player 기준" : "3D · 전체 배치";
            if (GetSoWorkspace() == SoWorkspace.Settings) view = "스테이지 설정 · 지도 입력 꺼짐";
            if (_soShowStages) view = "스테이지 목록 · 지도 입력 꺼짐";
            else if (_soShowCreation) view = "새로 만들기·복제 · 지도 입력 꺼짐";
            EditorGUILayout.HelpBox($"{(soModified ? "저장 안 됨" : "미저장 변경 없음")} · {view} · {SoCheckStatus()}", SoCheckMessageType());
        }

        private void DrawSoCheckResult()
        {
            if (!SoCurrentCheck) return;
            if (soCheck.Issues.Count == 0) EditorGUILayout.HelpBox(SoCheckStatus(), SoCheckMessageType());
            foreach (string warning in soCheck.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            var grid = SoGrid();
            foreach (var issue in soCheck.Issues)
            {
                EditorGUILayout.HelpBox(issue.Message, MessageType.Error);
                object item = issue.Target?.Resolve(soDocument);
                if (SoHasNoFixedPosition(item))
                {
                    if (GUILayout.Button("무작위 지형에서 수정")) FocusSoIssue(issue);
                }
                else if (grid != null && issue.ProblemCell is Vector2Int cell && grid.Contains(cell))
                {
                    if (GUILayout.Button("위치 보기 (2D)")) FocusSoIssue(issue);
                }
                else if (grid != null && item != null)
                {
                    string label = grid.Contains(soDocument.AnchorOf(item)) ? "배치 보기 (2D)" : "배치 수정 (맵 밖)";
                    if (GUILayout.Button(label)) FocusSoIssue(issue);
                }
                else if (issue.ProblemCell.HasValue)
                    EditorGUILayout.LabelField("판 밖 좌표입니다. 아군 구역 칸은 스테이지 설정의 '필드·아군 구역'에서 수정하세요.", EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.LabelField("벽·낭떠러지 고정 배치 검사 외의 전투 진입 규칙은 Play From Boot PlacedStage로 확인하세요.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(6);
        }

        private void FocusSoIssue(CombatStageSoIssue issue)
        {
            if (!SoCurrentCheck || !soCheck.Issues.Contains(issue)) return;
            FocusSoTarget(issue, soCheck.RootGuid, soCheck.Revision, "검사에서 선택한 대상");
        }

        // 검사와 공개 진단이 대상 이동만 공유합니다. 공개 진단으로 검사 결과/통과 상태를 만들지 않습니다.
        private void FocusSoTarget(CombatStageSoIssue issue, string rootGuid, long revision, string label)
        {
            if (rootGuid != soDocument.rootGuid || revision != soRevision) return;
            object item = issue.Target?.Resolve(soDocument);
            if (SoHasNoFixedPosition(item))
            {
                CancelSoWorkspaceInput();
                ClearSoIssueFocus();
                OpenSoInspector(SoInspectorSection.RandomTerrain);
                GUIUtility.ExitGUI();
                return;
            }
            var grid = SoGrid();
            if (grid == null) return;
            _soInspectedPlacement = item;
            CancelSoWorkspaceInput();
            OpenSoInspector(SoInspectorSection.Selection);
            soTool = SoTool.Select;
            SetSoMapView(SoView2D);
            soHasBox = false;
            soFocusedIssue = issue;
            soFocusRootGuid = rootGuid;
            soFocusRevision = revision;
            soFocusLabel = label;
            showSoCell = true;
            Vector2Int? destination = issue.ProblemCell;
            if (!destination.HasValue || !grid.Contains(destination.Value))
                destination = item != null ? soDocument.AnchorOf(item) : (Vector2Int?)null;
            soSelectedCell = destination.HasValue && grid.Contains(destination.Value) ? destination.Value : new Vector2Int(-1, -1);
            soPendingGridCell = grid.Contains(soSelectedCell) ? soSelectedCell : (Vector2Int?)null;
            soRepairCell = item != null ? soDocument.AnchorOf(item) : soSelectedCell;
            soPendingDetailScroll = true;
            Repaint();
            GUIUtility.ExitGUI();
        }

        // Document.Placements가 고정 칸에서 제외하는 무작위 시작 지형만 해당합니다.
        private bool SoHasNoFixedPosition(object item) => item is CombatStageSoMarker marker
            && marker.randomizeInitialPosition && soDocument.terrains.Contains(marker);

        // 공개 판정은 Visuals 한 곳에서 읽습니다. 위치 미정은 공개 분류와 별도로 셉니다.
        private List<CombatStageSoIssue> SoVisibilityIssues(out int hidden, out int random)
        {
            hidden = 0; random = 0;
            var issues = new List<CombatStageSoIssue>();
            foreach (var marker in soDocument.terrains.Concat(soDocument.objects))
            {
                bool shown = CombatStageSoVisuals.TryIsShown(marker, true, soCatalog.Names.owner, out string problem);
                bool randomStart = SoHasNoFixedPosition(marker);
                if (randomStart) random++;
                if (problem != null)
                {
                    var target = CombatStageSoIssueTarget.Of(soDocument, marker);
                    Vector2Int? cell = randomStart ? (Vector2Int?)null : new Vector2Int(marker.x, marker.y);
                    string location = cell.HasValue ? $"({cell.Value.x}, {cell.Value.y})" : "무작위 시작 · 고정 위치 없음";
                    issues.Add(new CombatStageSoIssue($"표시 판정 오류 · {CombatStageSoDocument.LayerName(target.Layer)} · sourceId {marker.sourceId} · {location}\n{problem}", cell, target));
                }
                else if (!shown) hidden++;
            }
            return issues;
        }

        private void DrawSoVisibilitySummary()
        {
            if (!soDocument.IsOpen || soCatalog == null) return;
            string rootGuid = soDocument.rootGuid;
            long revision = soRevision;
            var issues = SoVisibilityIssues(out int hidden, out int random);
            soShowVisibilityDetails = EditorGUILayout.Foldout(soShowVisibilityDetails, "공개·숨김 안내", true);
            EditorGUILayout.LabelField($"Player에게 비공개 {hidden} / 판정 오류 {issues.Count}", EditorStyles.wordWrappedMiniLabel);
            if (!soShowVisibilityDetails) return;
            EditorGUILayout.LabelField(SoPlayerFilter
                ? "3D Player 기준: 비공개·판정 오류 배치는 숨깁니다. 충돌 검사에는 남습니다."
                : "현재 공개 필터는 적용하지 않습니다. 전체/2D로 바꿔도 판정 오류는 해결되지 않습니다.", EditorStyles.wordWrappedMiniLabel);
            DrawSoVisibilityViewActions();
            if (random > 0)
            {
                EditorGUILayout.LabelField($"무작위 시작 · 고정 위치 없음 {random}개 (공개 분류와 별도). 전체 보기에서도 고정 외형은 없습니다.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("무작위 지형 열기"))
                {
                    CancelSoWorkspaceInput();
                    ClearSoIssueFocus();
                    OpenSoInspector(SoInspectorSection.RandomTerrain);
                    GUIUtility.ExitGUI();
                }
            }
            if (issues.Count > 0) DrawSoVisibilityIssues(issues, rootGuid, revision);
        }

        private void DrawSoVisibilityIssues(List<CombatStageSoIssue> issues, string rootGuid, long revision)
        {
            EditorGUILayout.LabelField("표시 진단이며 저장 검사 결과에 합산하지 않습니다. 정의·공개 대상을 직접 수정하세요.", EditorStyles.wordWrappedMiniLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(soVisibilityScroll, GUILayout.Height(Mathf.Clamp(position.height * .2f, 110, 170))))
            {
                soVisibilityScroll = scroll.scrollPosition;
                var grid = SoGrid();
                foreach (var issue in issues)
                {
                    EditorGUILayout.HelpBox(issue.Message, MessageType.Error);
                    DrawSoVisibilityIssueAction(issue, grid, rootGuid, revision);
                }
            }
        }

        private void DrawSoVisibilityIssueAction(CombatStageSoIssue issue, CombatStageSoGrid grid, string rootGuid, long revision)
        {
            object item = issue.Target.Resolve(soDocument);
            bool randomStart = SoHasNoFixedPosition(item);
            if (randomStart || grid != null)
            {
                string button = randomStart ? "무작위 지형에서 수정"
                    : issue.ProblemCell.HasValue && grid.Contains(issue.ProblemCell.Value) ? "위치 보기 (2D)" : "배치 수정 (맵 밖)";
                if (GUILayout.Button(button)) FocusSoTarget(issue, rootGuid, revision, "공개 진단에서 선택한 대상");
            }
            else DrawSoSettingsLink("필드 설정 열기", SoInspectorSection.PlayerZone);
        }

        private void DrawSoVisibilityNotice(object item)
        {
            if (!(item is CombatStageSoMarker marker)) return;
            bool shown = CombatStageSoVisuals.TryIsShown(marker, true, soCatalog.Names.owner, out string problem);
            if (problem != null)
                EditorGUILayout.HelpBox("표시 판정 오류: " + problem + " 아래에서 정의·공개 대상을 수정하세요. 전체/2D 전환만으로 오류가 해결되지는 않습니다.", MessageType.Error);
            else if (!shown)
                EditorGUILayout.LabelField(SoPlayerFilter ? "Player에게 비공개 · 현재 3D에서 숨김" : "Player에게 비공개 · 현재 공개 필터 미적용", EditorStyles.wordWrappedLabel);
            if (SoHasNoFixedPosition(marker))
                EditorGUILayout.LabelField("무작위 시작 · 고정 위치 없음. 전체 보기에서도 고정 외형은 없습니다.", EditorStyles.wordWrappedMiniLabel);
            if (problem != null || !shown) DrawSoVisibilityViewActions();
        }

        private void SetSoPlayerView(bool player)
        {
            if (soPlayerOnly == player) return;
            CancelSoWorkspaceInput();
            soPlayerOnly = player;
            Repaint();
        }

        private void DrawSoVisibilityViewActions()
        {
            if (!SoPlayerFilter) return;
            if (GUILayout.Button("전체 배치로 보기"))
            {
                SetSoPlayerView(false);
                SetSoMapView(SoView3D);
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button("2D에서 수정"))
            {
                SetSoMapView(SoView2D);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSoPasteVisibilityRecovery()
        {
            EditorGUILayout.LabelField("보기 전환은 붙여넣기를 실행하지 않습니다. 전환 후 목적지 칸을 확인하고 붙여넣기(Ctrl+V)를 다시 누르세요.", EditorStyles.wordWrappedMiniLabel);
            DrawSoVisibilityViewActions();
        }

        // 레이아웃이 확보된 다음 한 번만 스크롤합니다. 이후 수동 스크롤은 그대로 둡니다.
        private void ScrollSoGridToIssue(float cellSize)
        {
            if (!soPendingGridCell.HasValue || Event.current.type != EventType.Repaint) return;
            Vector2Int cell = soPendingGridCell.Value;
            soGridScroll = new Vector2(Mathf.Max(0, cell.x * cellSize), Mathf.Max(0, (soField.Rows - 1 - cell.y) * cellSize));
            soPendingGridCell = null;
            Repaint();
        }

        // 오류 대상은 선택 칸과 별개로 직접 엽니다. 판 밖 앵커도 같은 편집기를 사용합니다.
        private object DrawSoFocusedIssue(CombatStageSoGrid grid)
        {
            if (soFocusedIssue == null || soFocusRootGuid != soDocument.rootGuid || soFocusRevision != soRevision) return null;
            object item = soFocusedIssue.Target?.Resolve(soDocument);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(soFocusLabel, EditorStyles.boldLabel);
                if (soPendingDetailScroll && Event.current.type == EventType.Repaint)
                {
                    soDetailsScroll.y = Mathf.Max(0, GUILayoutUtility.GetLastRect().y);
                    soPendingDetailScroll = false;
                    Repaint();
                }
                EditorGUILayout.LabelField(soFocusedIssue.Message, EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("대상 강조 닫기"))
                {
                    ClearSoIssueFocus();
                    GUIUtility.ExitGUI();
                }
                if (item != null && grid != null)
                {
                    var target = soFocusedIssue.Target;
                    var cells = soDocument.CellsAt(item, soDocument.AnchorOf(item).x, soDocument.AnchorOf(item).y, soDocument.RotationOf(item), grid);
                    if (cells.Exists(cell => !grid.Contains(cell)))
                    {
                        EditorGUILayout.HelpBox("맵 밖 배치입니다. 이동할 X·Y를 함께 정한 뒤 적용하세요.", MessageType.Warning);
                        soRepairCell = EditorGUILayout.Vector2IntField("이동할 좌표", soRepairCell);
                        if (GUILayout.Button("좌표 함께 적용"))
                        {
                            Vector2Int destination = soRepairCell;
                            if (TryEditSo(item, destination, soDocument.RotationOf(item), soDocument.DefinitionOf(item), "맵 밖 배치 복구", () =>
                                {
                                    if (target.Layer == CombatStageSoLayer.Rail) soDocument.railCells[target.Index] = destination;
                                    else soDocument.Move(item, destination);
                                }))
                                FollowSoMove(destination);
                        }
                    }
                    DrawSoVisibilityNotice(item);
                    DrawSoPlaced(new CombatStageSoPlaced(target.Layer, item, cells));
                }
            }
            return item;
        }
    }
}
