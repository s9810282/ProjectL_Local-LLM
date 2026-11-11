# 전투 스테이지 편집 도구 SO 모드 — 빠른 사용법·초안 보호·기믹 입력 인계 (2026-11-09 갱신, 민형님, 미전달)

- 작성자: 호진(이호진)
- 최신 상태(2026-11-09): UXR-a~c·DP-a·TXT-a 코드 반영 뒤 **UXR-d 문서·정적 대조**를 반영했습니다. **컴파일 미확인, 최신 Unity 확인·Phase 인수 대기, 커밋 전, 공유 미전달**입니다. 현재 안내는 §10, 전체 기능/후속 확인은 [계획 §20.9](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#209-uxr-d-문서기능-대조-기록-2026-11-09)가 원본입니다. D-01 본부 삭제 안내는 후속 승인으로 수정 반영했습니다(계획 §20.10, 컴파일·Unity 미확인). 아래 날짜별 기록은 당시 이력이며 형식 4~6·구형 초안 자동 재읽기는 현행 정책이 아닙니다.
- 터널 폭발 후속: RTE-a 스테이지별 View 옵션과 편집 툴/Factory 연결 반영(§9), 컴파일·Unity 미확인. 공유 미전달입니다.
- 모양 편집 후속: GSE-a 코드 반영(§8), 컴파일·Unity 미확인, 커밋 전. 자동 위치와 고지대 B1 보류를 유지합니다. 공유 미전달입니다.

- 하붑 후속: HBE-a 코드 반영(§7), 컴파일·Unity 미확인, 커밋 전. 기존 UX 검증과 별개이며 공유는 미전달입니다.

- 최신 후속: UX-a~UX-c·UXR-a/b 코드 반영, 컴파일·Unity 미확인, 커밋 전. 아래 과거 검증은 새 UX 확인을 대신하지 않습니다. UX 후속은 §6, 실행 상태 원본은 계획 §12.9입니다. 공유는 발송하지 않았습니다.

민형님 도구 **ProjecL → 전투 스테이지 편집**(`Tool/Editor/CombatLayoutWindow*`)에 **SO 모드**를 더했습니다. 창 머리줄의 `DT | SO`로 바꿉니다. **DT 모드는 동작이 바뀌지 않았습니다.**

- SO 모드: 게임이 쓰는 전투 스테이지 SO(Stage Root·전투 데이터·View Stage Data와 참조 SO)를 열고, 고치고, 새로 만들고, 표에 등록합니다.
- 기획자용 사용법: [`Tool/SO모드 사용법.md`](../../../Assets/ProjectL/Script/Tool/SO모드%20사용법.md)
- 설계·계획:
  - [SO 모드 설계](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md)·[계획](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md)(SO-a~SO-d)
  - 추가 기능(SO-f~SO-i, 진영 줄 수, SO-e)의 설계·계획은 위 두 문서에 합쳐졌습니다(옛 추가 기능 설계·계획은 git `10914570`).
  - 아군 배치 구역 대응(AZ-a·AZ-b, 2026-10-28)은 설계 §11·계획 §7입니다. 진영 줄 수의 적 쪽은 이것으로 바뀌었습니다(§4 끝).
- 검증: MSBuild·새 시험 없이 Unity에서 사용자가 확인했습니다. SO-a~SO-h는 확인 완료, SO-i(3D)·적/아군 구분·진영 줄 수는 확인 대기입니다. 병합 대응 A1·A2(MG-a·MG-b)와 아군 배치 구역 대응(AZ-a·AZ-b), 배경 방식·바닥 타일 대응(UT-a~UT-c, 2026-10-29)은 확인 완료입니다. 기존 도구 시험(`Assets/Tests/EditMode/Tool/`)은 바꾸지 않았습니다.

## 1. 바뀐 기존 파일

| 파일 | 바뀐 것 |
|---|---|
| `CombatLayoutWindow.cs` | 기존 DT/SO 머리줄·본문·Undo 분기. DP-a에서 `OnEnable` 보호 초기화, `OnDisable` 초안 보관/확인 기준 기록, `OnGUI` 보호 준비, `OnUndoRedo` 복원 경계 처리를 연결했습니다. 최초 네 분기만 있던 상태에서 확장됐으므로 수명 변경 시 §10을 함께 확인하세요. |
| `ProjectL.Tool.Editor.asmdef` | `references`에 셋: `Framework`(DB 항목 `GetData()` 검사), `ProjectL.Core.Contracts`(장애물 키 `ObstacleContentProfile.KeyFor`, 유닛 역할·소유 열거), `ProjectL.Core.Domain`(공격 모양 `AppointShapeCalculator`) |
| `Tool/사용법.md`·`Tool/README.md` | 머리에 SO 모드 안내·링크 단락 하나씩. 나머지 DT 설명은 그대로입니다. |

## 2. 도구 파일 (모두 `Tool/Editor/`, C# 파일과 `.meta` 함께 관리)

| 파일 | 역할 |
|---|---|
| `CombatLayoutWindow.SoMode.cs` | 기존 맵·배치/설정 편집기·입력·저장 흐름. 영역 조립은 Workspace가 호출 |
| `CombatLayoutWindow.SoWorkspace.cs` | UXR-a/b의 작업 영역·목록·상황별 속성 조립과 공통 입력 취소/보기 전환 |
| `CombatLayoutWindow.SoUx.cs` | 검사 수명·상태 표시·대상 위치 이동·UX-c 공개 진단 안내. 저장 검사와 표시 진단을 분리 |
| `CombatLayoutWindow.SoHelp.cs` | UXR-c 여섯 주제·상세 문서·기능 위치 이동. 도움말 중 지도 입력 차단, 기존 진단/보호 UI 호출. 편집/저장 규칙은 소유하지 않음 |
| `CombatLayoutWindow.SoDrafts.cs` | DP-a 복원 판단·보호 UI·재컴파일 확인 기준·복원 Undo 연결 |
| `CombatStageSoDraftStore.cs` | 활성/보호 원문 읽기·쓰기·일치 확인·보호·원문 내보내기 결과 |
| `CombatLayoutWindow.SoGimmickShapes.cs`·`CombatStageSoGimmickShapeEditor.cs` | 모양 UI/Undo 조립과 상대 모양 격자 입력. 맵 배치는 기존 HaboobStart/Rockfall 도구에 연결 |
| `CombatLayoutWindow.SoHaboob.cs` | 하붑 사용·주기·경고·상대 칸 목록·시작 기준점, 맵 클릭/드롭·시작 비우기. 값 변경은 기존 SoChange 경로 사용 |
| `CombatStageSoIssue.cs` | 메시지·선택적 좌표·층/인덱스 대상, Root/개정 기반 검사 결과 값 |
| `CombatStageSoDocument.cs` | 작업본(참조는 GUID 문자열, 열거는 값). 배치 계산·바로 막기·상자 편집(칠하기·복사·대칭·묶음 옮기기)·아군 구역 칸 편집(`PaintZone`) |
| `CombatStageSoAssets.cs` | SO 읽기·쓰기 창구. **SO 직렬화 필드 이름은 이 파일에만 있습니다**(DB SO 형식 대부분이 internal이라 SerializedObject로 접근) |
| `CombatStageSoValidation.cs` | 저장 전 검사(저장하지 않는 복제본에 써 보고 `GetData()` + Root·View 짝 확인) |
| `CombatStageSoCreation.cs` | 새 스테이지·View·전용 복제·새 필드 만들기, Root 표·View 표 등록 |
| `CombatStageSoRange.cs` | 적 공격 범위 미리보기(Core `AppointShapeCalculator`를 부름) |
| `CombatStageSoVisuals.cs` | 3D용 외형 표 읽기 + 게임 배치 식 사본(식마다 원본 자리 주석) |
| `CombatStageSoPreview.cs` | 3D 미리보기(`PreviewRenderUtility`, 작업 씬에 아무것도 만들지 않음) |
| `CombatStageSoChapter.cs` | 표 등록 상태가 맵 생성 규칙(보스 하나, 출현 종류별 후보)을 지키는지 판정. Core `ChapterStagePool`·`ChapterGraphSpec`을 만들어 봅니다(병합 대응 A2, 2026-10-27 추가) |
| `CombatStageSoTiles.cs` | 배경 방식·바닥 타일 값과 계산(작업본 타일 배치, 배경 방식 규칙, 타일 맞춤·칸 → 타일 대응·3D 타일 묶음). 게임 타일 맞춤 식의 사본입니다(2026-10-29 추가) |
| `CombatLayoutWindow.SoTiles.cs` | 같은 `partial class`의 배경 방식 패널·'바닥 타일' 묶음·맵 '타일' 보기(붓·기준 타일)·2D 타일 띠·맞춤 알림(2026-10-29 추가) |

2026-10-29 병합 최신화(배경 방식·바닥 타일, [SO 모드 설계 §12](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#12-병합-최신화-배경-방식바닥-타일-ut1ut10))는 위 두 새 파일과 SO 파일(`SoMode`·`Document`·`Assets`·`Visuals`·`Preview`)만 바꿨습니다. DT 모드·기존 창 파일·asmdef는 그대로입니다. SO 창의 보기 선택 직렬화 필드가 `soThreeD`(bool)에서 `soMapView`(int)로 바뀌었습니다.

같은 날 바닥 타일 칸별 회전([SO 모드 설계 §13](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#13-바닥-타일-칸별-회전-tt1tt9))도 SO 파일만 바꿨습니다(새 파일 없음). SO 맵 도구 열거 끝에 `TileRotate`("타일 회전")가 생겼고(옛 순번 그대로), '타일' 보기 모드 직렬화 필드가 `soTileAnchorMode`(bool)에서 `soTileMode`(int)로 바뀌었습니다. DT 모드는 그대로입니다.

## 3. 함께 쓰는 DT 쪽 멤버 (바꾸실 때 주의)

SO 코드가 DT 파일의 멤버 셋을 그대로 씁니다. 이름이나 동작을 바꾸시면 SO 모드도 함께 봐야 합니다.

- `EndStroke()`: 끌기 한 번을 Undo 한 묶음으로 끝내는 데 같이 씁니다.
- `lastPainted`: 끌기 칠하기에서 같은 칸을 다시 칠하지 않게 같이 씁니다.
- `DraftKey`: 활성 SO 초안은 `DraftKey + ".So"`, 보호 원문 한 개는 여기에 `".Recovery"`를 붙여 보관합니다. 직접 삭제/덮어쓰기 대신 DraftStore/SoDrafts 계약을 사용합니다. 둘 다 PC 로컬이며 작업본 형식 7과 보호 봉투 형식 1은 별개입니다.

그 밖에 SO partial에 정의된 것은 다음과 같습니다.

- `OnDestroy()`: 3D 미리보기 정리용입니다. DT 쪽에 `OnDestroy`를 만드시면 겹치니 한 곳으로 합쳐야 합니다.
- 직렬화 필드: `editMode`와 `so…` 필드. 창을 다시 열어도 남습니다.

## 4. 알아 두실 것

- **도구가 게임 데이터를 직접 씁니다.** SO 모드 저장은 DB 에셋을, 등록은 `StageRootDataTable`·`StageViewPropDataTable`을 바꿉니다. 검사를 통과해야만 씁니다.
- 병합 대응(2026-10-27 추가, [SO 모드 설계 §10](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#10-병합-대응-a1a2-mg1mg9)):
  - 고지대처럼 이웃 모양을 쓰는 지형에는 외형 회전을 넣지 못하게 했습니다. 남은 회전은 저장 검사가 막습니다(있으면 전투 화면이 멈춤).
  - 등록·빼기와 등록된 스테이지의 맵 키·역할 저장 때 맵 생성 규칙을 봅니다. 보스 교체를 고르면 **다른 스테이지(옛 보스)의 Root·View를 표에서 뺍니다.**
  - `CombatStageSoCreation.SetRegistered`의 빼기는 이제 짝 View 없이 Root만 빼는 것도 받습니다(넣기는 그대로 둘 다 필요).
  - DT 모드와 기존 창 파일은 이번에도 바뀌지 않았습니다.
- 새 스테이지·전용 복제·새 필드는 파일을 만듭니다. 시험으로 만든 에셋이 남을 수 있습니다.
- DB SO 필드 이름이 바뀌면 `CombatStageSoAssets.cs`의 상수도 바꿔야 합니다. 없는 필드는 이름을 담은 예외로 알려 줍니다.
- ~~진영 칸을 칸 단위로 고치는 기능은 Core 변경이 필요해 보류했습니다.~~ 바뀜(2026-10-28): 천명님 `5a1bfbe1f`로 **적 배치 구역이 없어지고**, 아군 배치 구역을 스테이지 칸 목록(`CombatStageData.playerDeploymentCells`, 비면 필드 아군 줄 수)으로 정하게 되었습니다(Core 전달 문서 §43 응답).
  - SO 모드는 여기에 맞췄습니다: 맵을 "아군 구역 / 적 배치 가능" 둘로 칠하고, 도구 줄 끝에 '아군 구역' 칠하기·지우기 도구를 더하고, 적 줄 수 슬라이더를 걷고, 참고 수·경고를 더했습니다([SO 모드 설계 §11](../specs/2026-10-27-combat-stage-editor-so-mode-design-Hojin.md#11-아군-배치-구역-칸-az1az9)). 새 파일은 없고 위 §2의 SO 파일만 바뀌었습니다. 아군 구역 도구도 끌기 묶음에 `EndStroke()`·`lastPainted`를 같이 씁니다(§3).
  - **DT 모드·엑셀 도구가 보이는 진영 줄 수(아군·적 배치 깊이)는 민형님 몫으로 남깁니다**(Core 설계 PZ7). 적 줄 수는 이제 게임이 읽지 않고, 아군 구역은 스테이지 칸 목록이 있으면 그것이 우선입니다.

## 5. FU-a~FU-c 후속 (2026-10-31, 미전달)

- 새 파일: `Tool/Editor/CombatStageSoPlacementChecks.cs`와 `.meta`. 값으로 받은 점유 칸과 벽/낭떠러지 칸의 교차만 검사합니다. Assets는 임시 복제본 입력을 읽고 Validation이 검사에 조립합니다.
- Creation은 ‘배치 비우기’에만 `clearLegacyPlacements: true`를 전달합니다. 일반 저장·배치 복제는 호환 배열을 보존합니다. 검사를 통과하기 전에 원본이나 새 파일에 쓰지 않습니다.
- FU-b는 Assets·Visuals·Preview·Document·SoMode를 변경했습니다. Document 묶음 연산의 대상 판정은 선택 인자이며 기본은 기존 전체 대상입니다. Player 기준은 표시/편집 필터만 적용하고 충돌 검사는 전체 데이터를 사용합니다. DT·asmdef·런타임 변경 없음.
- FU-c는 SoMode의 범위 안내에 특성·버프/디버프 사거리 보정 미반영을 명시했습니다. 계산 코드는 확장하지 않았습니다.
- 이전 변경 보완: 용근님 `c40fd37b`에서 `CombatStageSoAssets.cs`·`CombatLayoutWindow.SoMode.cs`의 레일 필드/표시를 진영 턴 단위에 맞췄습니다. 이번 FU 작업의 신규 변경과 구분합니다.
- 고지대 B1 보류. 컴파일·Unity 확인 미완료, 공유 미전달, 커밋·push는 사용자 담당입니다.

## 6. UX-a~UX-c 및 화면 조립 후속 (2026-11-02, 미전달)

- `공개·숨김 안내`는 `TryIsShown(..., true, ...)`를 재사용해 정상 비공개/판정 오류를 구분합니다. 무작위 시작 지형 수는 공개 여부와 별도의 위치 미정 수입니다. 전체/2D 전환으로 오류를 지우거나 공개 목록을 자동 수정하지 않습니다. 같은 진단은 타일/설정의 속성에도 남습니다.
- 고정 오류는 Issue의 층/인덱스와 Root/개정을 확인한 뒤 기존 2D 선택·강조·스크롤·판 밖 좌표 일괄 적용으로 갑니다. 무작위 시작은 기존 무작위 지형 패널을 열며 좌표를 만들지 않습니다. 위치 없는 GetData/맵 규칙 오류에는 위치 버튼이 없습니다.
- `전체 배치로 보기`/`2D에서 수정`은 선택·클립보드를 유지하고 미확정 입력을 취소합니다. 위치 찾기는 기존 UX-a처럼 원인 대상으로 선택을 바꿉니다. 붙여넣기 거절 안내에서도 전환만 제공하고 사용자가 Ctrl+V를 다시 누르도록 합니다. Preview 기준 키·표면 높이·자기 소유 타일 복원과 FU 개별 편집기는 유지합니다.
- 저장 여부는 `soModified`, 검사 상태는 `soCheck` 한 곳입니다. 표시 진단을 검사 오류에 합산하지 않고, 검사 통과를 저장 완료로 취급하지 않습니다. 변경/Undo 뒤 재검사 필요, 보기 전환은 검사 상태 유지입니다. 기록은 [계획 §12.9](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#129-ux-c-구현-기록-2026-11-02)를 따릅니다.
- UX-c 변경은 SoUx/SoMode/SoWorkspace와 관련 문서입니다. DT·Core·런타임·DB SO·고지대는 변경하지 않았습니다. UXR-c/d는 미착수이며 새 시험·MSBuild·커밋·push 없음. **컴파일·Unity 미확인, 고지대 B1 보류, 공유 미전달.**

## 7. HBE-a 하붑 설정 (2026-11-02, 미전달)

- 위치는 **스테이지 설정 → 기믹 → 하붑**입니다. 기존 “기믹 · 레일·낙석” 분류 이름을 “기믹”으로 줄였고 레일/낙석 편집기는 유지했습니다. 새 SoHaboob partial과 .meta는 사용 여부·이동/경고 횟수·상대 칸 목록 UI만 소유합니다.
- Document에 하붑 값 묶음을 더하고 작업본 형식은 **4→5**입니다. 구형 초안/열린 작업본은 기존 거절·에셋 다시 읽기 정책을 사용합니다. 적용 전 미저장 편집을 저장해야 하며, 새 기본값은 공개 HaboobGimmickSpec에서 얻습니다.
- Assets의 ReadHaboob/WriteHaboob이 기존 SO 네 필드를 그대로 왕복합니다. 꺼진 값·목록 순서를 보존하고 자동 정규화하지 않습니다. 모든 UI 변경은 SoChange로 Undo/초안/Dirty/재검사에 연결합니다.
- Validation은 사용 중 값만 기존 공개 Spec으로 검사합니다. 오류를 “하붑 값:” 한 건으로 알리고 검사용 복제본에서만 하붑을 꺼서 낙석 등 나머지 검사도 수행합니다. 원본·작업본은 그대로이며 검사/저장/등록/생성은 같은 경로입니다.
- 배치 비우기/스테이지 복제는 틀의 하붑 설정을 보존합니다. 영역·타일 복사는 하붑 설정을 옮기지 않습니다. 위치/모양의 필드 수용 여부는 전투 진입 확인이며 3D 구름·다음 위치·경고 이펙트는 추가하지 않았습니다.
- Core·DB SO·런타임·DT·기존 에셋·고지대 변경 없음. 실행·정적 확인 기록은 계획 §15.4, 사용자 Unity 확인은 §15.2(미확인)입니다. **컴파일 미확인, 공유 미전달, 커밋 전**입니다.

## 8. GSE-a 낙석·하붑 모양 편집 (2026-11-02, 미전달)

- 배치 Sidebar에 `낙석 모양·설정`/`하붑 모양·설정` 바로가기를 더했습니다. 같은 기믹 패널의 제목으로 이동하며 기믹을 자동 활성화하지 않습니다. 레일과 기존 기믹 값/경고는 유지합니다.
- 새 `CombatStageSoGimmickShapeEditor`(+ .meta)는 칠하기/지우기·선분 입력·임시 획·확대/이동/전체 보기를 소유합니다. 새 `CombatLayoutWindow.SoGimmickShapes` partial(+ .meta)은 두 값 묶음과 SoChange/Undo·공개 Spec 기본 모양·취소·바로가기를 연결합니다. 기존 수치는 세부 설정, 기존 목록은 고급 좌표에 남깁니다.
- 한 획이 끝나고 실제 칸이 바뀔 때만 상대 좌표를 맞춰 반영합니다. 빈 칸 지우기·단순 열기/보기는 값/순서/원점을 바꾸지 않습니다. 정규화 시 보기 원점도 함께 옮깁니다. 기본 모양은 해당 공개 Spec에서 읽으며 다른 수치를 초기화하지 않습니다.
- Esc/명령/포커스 상실/작업 전환/Undo/해제는 새 편집기의 미완료 획을 취소합니다. 창의 OnDisable에는 취소 한 줄만 추가했습니다. 기존 맵/타일 입력·Preview 구현을 바꾸지 않았으며 런타임 UISystem 등록 대상이 아닙니다.
- Document/Assets/Validation/Creation·CurrentFormat 5는 그대로입니다. 기존 SO/작업본·저장 전 검사·자동 위치·초안·복제 계약을 유지합니다. 실제 필드 수용/위치/연출 확인은 전투 진입에서 합니다. 설계 §23·계획 §16이 원본이고 사용법은 §5.13입니다.
- **컴파일·Unity Gu.1~Gu.5 미확인, 고지대 B1 보류, 공유 미전달, 커밋 전.** 새 시험·MSBuild·에이전트·커밋·push는 하지 않았습니다.

## 9. RTE-a 낙석 터널 폭발 옵션 (2026-11-02, 미전달)

- `StageViewPropData.disableRockfallTunnelExplosion`(기본 false)을 추가했습니다. 런타임은 파생 `RockfallTunnelExplosionEnabled`로 읽어 기존 에셋의 폭발 켜짐을 유지합니다. 공용 RockfallVisual과 운영 에셋은 바꾸지 않았습니다.
- 조립 변경은 `CombatSceneDisplayFactory`·`CombatTraitPreviewFactory`의 `FieldRockfallPresenter` 생성자 마지막 bool 인자입니다. `StageViewPropPresenter`가 같은 View SO에서 읽은 값을 넘기고 배경 해제 시 기본값으로 돌립니다. 서비스 등록·Export·Builder·Framework 계약은 변경하지 않았습니다.
- Tool의 낙석 세부 설정에 **터널 폭발 사용**을 연결했습니다. View 읽기/쓰기와 SoChange의 Undo·초안·Dirty·검사 수명을 재사용합니다. 작업본 형식 5→6이므로 업데이트 전 미저장 편집을 저장해야 합니다.
- 꺼짐에서는 터널 WarningCues와 터널 누락 안내만 생략합니다. 경고음·낙하·착지·피해 및 앵커 중복 오류/능선 안내는 유지합니다. Transform이나 자식 능선은 비활성화하지 않습니다.
- 원본은 편집 설계 §24·계획 §17, 사용법 §5.11입니다. **컴파일·Unity Ru.1~Ru.5 미확인, 고지대 B1 보류, 커밋 전, 공유 미전달.**

## 10. 현행 화면·수명과 UXR-d 대조 (2026-11-09, 미전달)

- 화면은 **배치 / 바닥 타일 / 스테이지 설정**입니다. 배치 속성은 선택/아군 구역/무작위 지형, 설정은 기본 정보/필드·아군 구역/공유 SO·본부·구조물/배경·타일·카메라/기믹/등록·맵 생성 규칙입니다. 상단 `사용법`은 창 내부 여섯 주제를 열고 `상세 사용법 파일`이 Markdown을 엽니다. 기존 속성의 전체 편집 필드는 축소하지 않았습니다.
- 입력 소유권: 배치·지도 키는 SoMode, 타일 선택/복사/회전/난수는 SoTiles, 모양 획은 GimmickShapeEditor, 화면 전환/입력 취소는 Workspace입니다. Help는 표시 중 지도 본문/키를 호출하지 않습니다. 단순 닫기는 기존 영역/선택/클립보드/시점 유지, 위치 이동은 기존 Workspace/Inspector/MapView를 호출하며 편집/붙여넣기/저장을 실행하지 않습니다.
- HAT/GP: 하붑 클릭/드롭은 클릭한 칸을 구름 가운데 근처로 놓고 X/Y는 왼쪽 아래 기준점입니다. 시작 비우기는 하붑을 끄지 않습니다. 낙석은 클릭 위치와 무관하게 사용만 켭니다. 두 모양 편집·낙석 폭발 옵션·하붑 시작 설정은 스테이지 복제/배치 비우기에 포함되지만 영역/타일 클립보드에는 포함되지 않습니다.
- DP: 정상 현행 초안 복원, 복원 불가 원문 보호, 충돌 시 덮어쓰기 차단, 대상별 내보내기/복원/확인 폐기를 연결했습니다. 재컴파일 확인 기준과 빈 창 복원 Undo/복원 이후 클립보드 보완도 코드 반영입니다. 원문 내보내기는 SO 저장이 아니며 자동 형식 변환/다중 초안 이력은 없습니다.
- 저장·등록: Dirty는 `soModified`, 검사/개정은 `soCheck`, 보관 판단은 DraftStore/SoDrafts 한 곳입니다. 쓰기 실패는 미저장 유지하되 이미 쓴 에셋을 모두 롤백하지 않습니다. 에셋 저장 뒤 표만 실패하면 저장 상태 유지·표 오류 표시, 초안 정리 실패는 별도입니다. 등록/빼기의 기존 맵 규칙·보스 교체·확인창은 그대로입니다.
- PreviewRenderUtility·공유 작업 복제본·이벤트 해제는 기존 소유자 그대로입니다. Framework/Factory/서비스 등록·게임 입력·DB 스키마·DT·Core·FieldCamera·Architecture 변경은 UXR 범위가 아닙니다. 새 Help/Drafts는 기존 EditorWindow 내부 기능이며 런타임 UISystem 등록 대상이 아닙니다. **고지대 B1은 계속 보류**합니다.
- UXR-F01~F27의 정적 진입점 누락은 발견하지 않았습니다. D-01은 후속 사용자 승인으로 `SoToolHint`를 본부 포함·아군 구역 제외로 맞추고 상자 삭제 주석을 정정했습니다(계획 §20.10). 도움말도 같은 함수를 사용합니다. 삭제 로직은 유지했으며 컴파일·Unity 확인 및 UXR-d Phase 인수는 대기입니다.
- 확인 원본은 계획 §20.9입니다. UXR Ua/Ub/Uc, HBE/GSE/RTE/HAT/GP, DP Du.1~10·TXT Tu.1~2를 미확인으로 연결했습니다. 기존 확인 기록을 최신 코드 전체 성공으로 확대하지 않았습니다. **공유 미전달, 컴파일 미확인, 최신 Unity 미확인, 커밋 전**입니다.
