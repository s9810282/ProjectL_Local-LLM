using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 창 내부 도움말의 표시와 위치 이동입니다. 편집·판정·보관은 기존 명령이 소유합니다.
    public sealed partial class CombatLayoutWindow
    {
        private enum SoHelpTopic { GettingStarted, Placement, Tiles, SharedAssets, SaveAndRegister, Troubleshooting }

        private static readonly string[] SoHelpTopics =
            { "처음 만들기", "배치 복사", "바닥 타일", "공유 SO", "검사·저장·등록", "안 될 때" };

        // 도움말을 열어도 기존 영역·선택·클립보드·시점을 교체하지 않습니다. 초안/Undo에 넣지 않습니다.
        private bool _soShowHelp;
        private SoHelpTopic _soHelpTopic;
        private Vector2 _soHelpScroll;

        private SoHelpTopic CurrentSoHelpTopic()
        {
            if (SoDraftBlocked || _soDraftInvalidDocument || soCatalog == null) return SoHelpTopic.Troubleshooting;
            if (!soDocument.IsOpen || _soShowStages || _soShowCreation) return SoHelpTopic.GettingStarted;
            if (GetSoWorkspace() == SoWorkspace.Tiles || soTool == SoTool.TileRotate && GetSoWorkspace() == SoWorkspace.Layout)
                return SoHelpTopic.Tiles;
            if (GetSoWorkspace() != SoWorkspace.Settings) return SoHelpTopic.Placement;
            if (_soInspectorSection == SoInspectorSection.References || _soInspectorSection == SoInspectorSection.PlayerZone)
                return SoHelpTopic.SharedAssets;
            if (_soInspectorSection == SoInspectorSection.Gimmicks) return SoHelpTopic.Placement;
            if (_soInspectorSection == SoInspectorSection.Background) return SoHelpTopic.Tiles;
            return SoHelpTopic.SaveAndRegister;
        }

        private void ShowSoHelp(SoHelpTopic topic)
        {
            CancelSoWorkspaceInput();
            _soHelpTopic = topic;
            _soHelpScroll = Vector2.zero;
            _soShowHelp = true;
            Repaint();
        }

        private void CloseSoHelp()
        {
            _soShowHelp = false;
            GUIUtility.hotControl = 0;
            GUI.FocusControl(null);
            Repaint();
        }

        private void DrawSoHelpLink(string label, SoHelpTopic topic)
        {
            if (!GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(false))) return;
            ShowSoHelp(topic);
            GUIUtility.ExitGUI();
        }

        private void DrawSoHelp()
        {
            // 본문을 도움말로 대체합니다. 뒤의 지도·팔레트·단축키 처리 함수를 호출하지 않습니다.
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                CloseSoHelp();
                Event.current.Use();
                GUIUtility.ExitGUI();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("빠른 사용법", EditorStyles.boldLabel);
                if (GUILayout.Button("상세 사용법 파일", GUILayout.Width(140))) { OpenSoUsageGuide(); GUIUtility.ExitGUI(); }
                if (GUILayout.Button("닫고 돌아가기 (Esc)", GUILayout.Width(160))) { CloseSoHelp(); GUIUtility.ExitGUI(); }
            }
            EditorGUI.BeginChangeCheck();
            int topic = GUILayout.SelectionGrid((int)_soHelpTopic, SoHelpTopics, 3);
            if (EditorGUI.EndChangeCheck()) { ShowSoHelp((SoHelpTopic)topic); GUIUtility.ExitGUI(); }
            using (var scroll = new EditorGUILayout.ScrollViewScope(_soHelpScroll))
            {
                _soHelpScroll = scroll.scrollPosition;
                if (!string.IsNullOrEmpty(soNotice)) EditorGUILayout.HelpBox(soNotice, soNoticeType);
                DrawSoCommandAvailability();
                // 빈 창·카탈로그 실패에도 동일한 보호 UI/명령을 호출합니다. 원문 상태나 정책을 복제하지 않습니다.
                DrawSoDraftProtection();
                GUILayout.Space(8);
                EditorGUILayout.LabelField(SoHelpTopics[(int)_soHelpTopic], EditorStyles.boldLabel);
                switch (_soHelpTopic)
                {
                    case SoHelpTopic.GettingStarted: DrawSoGettingStartedHelp(); break;
                    case SoHelpTopic.Placement: DrawSoPlacementHelp(); break;
                    case SoHelpTopic.Tiles: DrawSoTilesHelp(); break;
                    case SoHelpTopic.SharedAssets: DrawSoSharedAssetsHelp(); break;
                    case SoHelpTopic.SaveAndRegister: DrawSoSaveHelp(); break;
                    case SoHelpTopic.Troubleshooting: DrawSoTroubleshootingHelp(); break;
                }
            }
            Event e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.R)
                || (e.type == EventType.ValidateCommand || e.type == EventType.ExecuteCommand) && (e.commandName == "Copy" || e.commandName == "Paste")
                || e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
                e.Use();
        }

        private static void SoHelpText(string text) => EditorGUILayout.LabelField(text, EditorStyles.wordWrappedLabel);

        // 위치 이동만 제공합니다. 생성·편집·검사·저장·등록은 이동한 화면에서 사용자가 실행합니다.
        private void DrawSoHelpWorkspaceLink(string label, SoWorkspace workspace)
        {
            using (new EditorGUI.DisabledScope(soCatalog == null || !soDocument.IsOpen || !SoDraftEditable))
            {
                if (!GUILayout.Button(label)) return;
                SetSoWorkspace(workspace);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSoHelpInspectorLink(string label, SoInspectorSection section)
        {
            using (new EditorGUI.DisabledScope(soCatalog == null || !soDocument.IsOpen || !SoDraftEditable))
            {
                if (!GUILayout.Button(label)) return;
                OpenSoInspector(section);
                GUIUtility.ExitGUI();
            }
        }

        private void DrawSoGettingStartedHelp()
        {
            SoHelpText("1. 상단 스테이지 선택/전체 목록에서 틀을 고릅니다.\n2. 새로 만들기 / 복제에서 이름·키를 정하고 생성 방식을 고릅니다. 이 단계에서 에셋 파일이 생깁니다.\n3. 배치 → 필요한 경우 바닥 타일·공유 SO 설정 → 검사 → 저장 → 필요한 경우 등록 → Unity 전투 진입 확인 순서로 진행하세요.");
            SoHelpText("배치 비우고 만들기와 배치까지 복제 모두 틀의 참조·View·낙석·하붑 설정을 이어받습니다. 새 스테이지는 표 미등록으로 만들어집니다.");
            using (new EditorGUI.DisabledScope(soCatalog == null || !SoDraftEditable))
            {
                if (GUILayout.Button("전체 스테이지 목록 위치 열기")) OpenSoHelpLibrary(false);
                if (GUILayout.Button("새로 만들기 / 복제 위치 열기")) OpenSoHelpLibrary(true);
            }
            DrawSoHelpWorkspaceLink("배치 작업 영역 열기", SoWorkspace.Layout);
            DrawSoHelpLink("저장·등록 차이 보기", SoHelpTopic.SaveAndRegister);
        }

        private void OpenSoHelpLibrary(bool creation)
        {
            CloseSoHelp();
            CancelSoWorkspaceInput();
            _soShowCreation = creation;
            _soShowStages = !creation;
            GUIUtility.ExitGUI();
        }

        private void DrawSoPlacementHelp()
        {
            if (GetSoWorkspace() == SoWorkspace.Layout && soDocument.IsOpen && soCatalog != null && !_soDraftInvalidDocument)
                EditorGUILayout.HelpBox(SoToolHint(), MessageType.Info);
            SoHelpText("선택 도구에서 빈 칸부터 끌거나 Shift+끌기로 상자를 만듭니다. 복사(Ctrl+C) 후 목적지 칸을 고르고 붙여넣기(Ctrl+V)를 누릅니다. 목적지 칸은 복사한 상자의 왼쪽 아래입니다. 다른 스테이지에도 붙일 수 있고, 막힌 칸은 기존 규칙대로 건너뜁니다.");
            SoHelpText("복사에 검사 통과는 필요하지 않습니다. 숫자·글자 입력 중 Ctrl+C/V는 입력 칸에 적용됩니다. 맵을 눌러 포커스를 옮긴 뒤 영역을 고르세요. 영역 복사는 본부·아군 구역·바닥 타일·낙석/하붑 설정을 담지 않습니다. 타일 클립보드는 별도입니다.");
            SoHelpText("기믹 배치: 하붑 시작 도구는 클릭/끌어 놓은 칸을 구름 가운데 근처로 맞춥니다. 설정의 시작 X/Y는 왼쪽 아래 기준점입니다. 이후 이동은 무작위입니다. 시작 위치 비우기는 하붑 사용 여부를 유지합니다. 상자 없이 구름 칸에서 Delete하면 배치를 먼저 지우고, 배치가 없을 때 시작 위치를 비웁니다.");
            SoHelpText("낙석 도구는 낙석을 켭니다. 클릭한 칸이 낙하 위치는 아닙니다. 모양은 돌 하나의 영역이며 돌 수와 다릅니다. 터널 폭발 사용을 꺼도 낙하·착지는 유지됩니다. 토네이도·레일과 기존 기믹 설정은 배치/기믹 화면에서 편집합니다.");
            DrawSoHelpWorkspaceLink("배치·복사 버튼 위치 열기", SoWorkspace.Layout);
            DrawSoHelpInspectorLink("낙석·하붑 설정 위치 열기", SoInspectorSection.Gimmicks);
            DrawSoHelpLink("숨김·붙여넣기가 막힐 때", SoHelpTopic.Troubleshooting);
        }

        private void DrawSoTilesHelp()
        {
            if (GetSoWorkspace() == SoWorkspace.Tiles) EditorGUILayout.HelpBox(SoTileToolHint(), MessageType.Info);
            else if (soTool == SoTool.TileRotate && GetSoWorkspace() == SoWorkspace.Layout)
                EditorGUILayout.HelpBox(SoToolHint(), MessageType.Info);
            SoHelpText("바닥 타일 작업 영역의 선택 모드에서 Ctrl+C/V로 종류·회전을 복사합니다. 붙일 선택 영역의 왼쪽 아래가 목적지 기준이며 다른 스테이지에도 붙일 수 있습니다. 배치·기믹 설정·타일 세트·맵 크기·기준 타일은 복사하지 않습니다.");
            SoHelpText("선택: 클릭/끌기, Ctrl로 더하기, Shift+끌기로 사각형. R은 시계 방향, Shift+R은 반시계 방향입니다. 숫자·글자 입력 후에는 격자를 눌러 포커스를 옮기세요. 검사 통과는 복사의 조건이 아닙니다. 맞춤/데이터 문제로 복사 버튼이 없으면 타일 화면의 원인 안내를 먼저 확인하세요.");
            SoHelpText("2D·3D의 타일 회전 도구는 고르기·돌리기용입니다. Ctrl+C/V는 바닥 타일 → 선택에서 사용하세요. 판 밖 타일은 3D 또는 바닥 타일 화면에서 고릅니다. 직사각형 타일은 0°/180°만 허용하며 Random도 이 범위를 따릅니다.");
            DrawSoHelpWorkspaceLink("바닥 타일 작업 영역 열기", SoWorkspace.Tiles);
            DrawSoHelpInspectorLink("배경 방식·타일 설정 위치 열기", SoInspectorSection.Background);
        }

        private void DrawSoSharedAssetsHelp()
        {
            SoHelpText("스테이지 설정 → 공유 SO·본부·구조물에서 참조와 상세 값을 확인합니다. 다른 스테이지도 사용하는 공유 SO는 직접 편집할 수 없습니다. 기존 전용 복제 기능을 사용하세요. 전용 SO 복제·새 필드 만들기는 그 단계에서 에셋 파일을 만들 수 있습니다.");
            SoHelpText("필드·아군 구역에서 필드 참조와 크기를, 배경·타일·카메라에서 짝 View와 배경을 확인합니다. 짝 View가 없으면 기존 짝 View 생성 동선을 이용하세요. 목록 새로고침은 정의 목록을 다시 찾고 미저장 편집은 유지합니다. 에셋에서 다시 읽기는 현재 편집을 버릴지 확인한 뒤 저장된 값을 엽니다.");
            DrawSoHelpInspectorLink("공유 SO·본부·구조물 위치 열기", SoInspectorSection.References);
            DrawSoHelpInspectorLink("필드·아군 구역 위치 열기", SoInspectorSection.PlayerZone);
            DrawSoHelpInspectorLink("배경·짝 View 복구 위치 열기", SoInspectorSection.Background);
        }

        private void DrawSoSaveHelp()
        {
            SoHelpText("검사는 현재 작업본의 문제를 확인하며 에셋을 쓰지 않습니다. 저장은 매번 현재 작업본을 검사한 뒤 SO에 반영합니다. 검사 통과는 저장 완료나 실제 전투 진입 성공을 뜻하지 않습니다. 변경 후에는 재검사가 필요합니다. 경고·조건부 확인은 오류와 구분하고 기존 확인창을 따릅니다.");
            SoHelpText("등록은 저장된 스테이지를 Run 후보 표에 넣는 작업입니다. 미저장 변경이 있으면 먼저 저장하세요. 표에서 빼기도 미저장 제한·기존 맵 규칙 확인을 따르며, 등록 때의 공통 저장 검사를 새로 요구하지 않습니다. 보스 교체·기존 맵 문제·확인 취소는 기존 안내를 따르세요.");
            SoHelpText("쓰기 도중 실패하면 작업본은 유지하지만 이미 쓴 에셋을 모두 되돌리지는 않습니다. 에셋 저장 뒤 표 반영만 실패하면 에셋은 저장된 상태이며 표 오류를 별도로 확인해야 합니다. 초안 정리 실패도 에셋 저장 실패와 다릅니다. 상단의 실제 결과 문구를 확인하세요.");
            SoHelpText("초안·보호 원문은 이 PC에만 보관됩니다. 원문 내보내기는 SO 저장이나 게임용 스테이지 생성이 아닙니다. 보호 초안 복원도 미저장 작업본으로 열며 원문은 남깁니다. 팀 공유는 SO 저장 뒤 사용자가 Git으로 커밋·push합니다. 보호 UI의 대상별 폐기는 확인 후 필요한 원문만 지웁니다.");
            DrawSoHelpDiagnostics(false);
            DrawSoHelpInspectorLink("등록·맵 생성 규칙 위치 열기", SoInspectorSection.Registration);
        }

        private void DrawSoTroubleshootingHelp()
        {
            SoHelpText("복사가 안 될 때: 검사부터 할 필요는 없습니다. 입력 칸의 포커스, 상자/타일 선택, 복사한 내용, 현재 도구를 확인하세요. 바닥 타일 Ctrl+C/V는 선택 모드에서 사용합니다. 버튼도 실행되지 않으면 해당 명령의 알림과 초안 보호 이유를 확인하세요.");
            SoHelpText("안 보일 때: 정상 비공개, 표시 판정 오류, 무작위 시작의 위치 미정은 서로 다릅니다. 아래 기존 공개·숨김 안내에서 확인하세요. 전체/2D 보기는 공개 값을 바꾸거나 오류를 해결하지 않습니다. 보기 전환 후 붙여넣기는 목적지를 확인하고 직접 다시 실행하세요.");
            SoHelpText("초안 충돌/보관 실패가 있으면 위 보호 UI에서 원문을 내보내고 대상별로 확인하세요. 내보내기만으로 원문이 삭제되지는 않습니다. 복원·에셋 다시 열기는 현재 작업 교체 확인을 따릅니다. 보호 원문이 있다는 이유만으로 정상 편집이 차단되지는 않습니다.");
            DrawSoHelpDiagnostics(true);
            DrawSoHelpInspectorLink("필드·참조 문제 확인", SoInspectorSection.PlayerZone);
            DrawSoHelpInspectorLink("배경·짝 View 복구 위치 열기", SoInspectorSection.Background);
        }

        private void DrawSoHelpDiagnostics(bool visibility)
        {
            if (!soDocument.IsOpen || soCatalog == null || _soDraftInvalidDocument) return;
            soField = null;
            try { soField = SoFieldOf(); }
            catch (System.Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
            // 기존 검사/진단과 위치 이동을 그대로 사용합니다. 현재 개정 검증도 기존 함수가 맡습니다.
            DrawSoWorkspaceCheckResult();
            if (!visibility) return;
            if (soPasteVisibilityBlocked) DrawSoPasteVisibilityRecovery();
            DrawSoVisibilitySummary();
        }
    }
}
