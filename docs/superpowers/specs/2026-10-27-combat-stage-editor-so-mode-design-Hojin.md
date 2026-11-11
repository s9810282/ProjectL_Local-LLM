# 전투 스테이지 편집 SO 모드 설계

- 작성자: 호진(이호진)
- 최신 재검토 보완(2026-11-09): 사용자 승인으로 검토 2번(초안 보호 재시도)·3번(목록 실패 시 검사/저장 차단)의 코드·문서를 반영했다. 컴파일·Unity 확인은 미확인이다. 범위는 §27.6, 실행 기록은 계획 §20.11이다. **1번 DT 모드 복원 Undo/Redo 초안 동기화는 DT 사용 여부 미정으로 보류**, 고지대 B1도 계속 보류한다.
- 최신 후속 반영(2026-11-09): 사용자 승인으로 **UXR-d 문서·정적 대조에 이어 D-01 본부 삭제 안내 코드·문서 수정 반영**(계획 §20.10). 실행 기록은 [계획 §20.9](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#209-uxr-d-문서기능-대조-기록-2026-11-09)다. UXR-F01~F27 진입점 누락은 발견하지 않았으나 본부 삭제 안내 D-01은 후속 승인으로 문구·주석 수정 반영했다(계획 §20.10). **컴파일 미확인·최신 Unity 확인·UXR-d Phase 인수 대기**, 공유 문서 미전달이다. 기존 DP/TXT/UXR-c·HAT/GP의 미확인 상태와 고지대 B1 보류를 유지한다. 이번 후속은 안내·주석만 수정했으며 삭제 로직·운영 에셋·초안은 유지했다.
- 낙석 터널 폭발 옵션(2026-11-02): 사용자 “전투 스테이지 편집 툴에도 적용해주세요”로 §24 RTE 승인. **계획 §17 RTE-a 코드·문서 반영, 컴파일·Unity 미확인**이며 GSE-a 구현과 구분한다. 고지대 B1은 계속 보류다.
- 기믹 모양 UX 후속(2026-11-02): 사용자 “이 설계대로 계획 문서에 반영하고 구현도 진행”으로 **§23 승인, 계획 §16 GSE-a 코드·문서 반영, 컴파일·Unity 미확인**. 자동 위치·기존 기능·고지대 B1 보류를 유지한다.
- 하붑 편집(2026-11-02): 사용자 설계 승인으로 **HBE-a 코드·문서 반영, 컴파일·Unity 미확인**(§22, 계획 §15.4). 기존 기믹 패널에 설정·저장·검사·Undo·복제를 연결했다. 고지대 B1 보류와 UXR-c/d 미착수 상태는 유지한다.
- 시안 적용 설계·구현(2026-11-02): **UXR-a·UXR-b와 선행 UX-c 코드 반영, 컴파일·Unity 미확인. UXR-c·UXR-d 미착수.** §21의 기존 기능 보존 기준으로 속성·설정·타일을 재배치하고 숨김 복구를 연결했다. 창 내부 도움말·사용법 전체 갱신은 후속 단계이며 고지대 B1은 계속 보류한다. 실행·확인 원본은 계획 §14.8·§12.9.
- UX 후속(2026-11-02): **UX-a~UX-c 코드·관련 안내 문서 반영, 컴파일·Unity 미확인**(§16). 고지대 보류 유지.
- 최신 구현 상태(2026-10-31): **FU-a·FU-b 코드 반영, 컴파일·Unity 확인 미완료. FU-c 안내·문서 반영, 고지대 B1은 보류.** 아래 문서 작성 당시의 미착수 기록과 구분한다.
- 상태: SO-a~SO-i·SO-e(`48e7b533`), MG(`86b447cd`), AZ(`ac4a8101`), 안내 줄 삭제(`abbdea4f`), UT(`ede23986a`, 공유 문서 후속 `f5c8109d4`), TT·TO(`f5c8109d4`) 구현·문서 반영. 기존 사용자 Unity 확인 기록과 미확인 항목은 계획 §1·§2.1을 따른다. **2026-10-31 후속 보완 FU-a·FU-b 코드 및 FU-c 안내·문서 반영 완료(§15). 컴파일·Unity 미확인. 고지대 이웃 모양 B1은 사용자 지시로 보류.**
- 초기 범위: Unity 메뉴 **ProjecL → 전투 스테이지 편집**(민형님 `Tool/Editor/CombatLayoutWindow*`)에 SO 모드를 더해 전투 스테이지 한 벌(Stage Root + CombatStageData + View Stage Data)을 열고·고치고·새로 만든다. 초기 구현은 DT(엑셀) 모드, DB SO 코드(`DataBaseSystem`), 게임 런타임을 바꾸지 않는다. 후속 §24 RTE는 스테이지별 View SO 표시 값과 낙석 View 런타임 연결까지 확장하는 **승인된 후속 범위**이며, Core 규칙 변경은 포함하지 않는다.
- 계획: [전투 스테이지 편집 SO 모드 계획](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md)
- 기획자용 사용법: [`Tool/SO모드 사용법.md`](../../../Assets/ProjectL/Script/Tool/SO모드%20사용법.md)
- 정리: 2026-10-27 `2026-10-27-combat-stage-editor-so-mode-design.md`(SM1~SM12)·`2026-10-27-combat-stage-editor-so-mode-additions-design.md`(AD1~AD5)·`2026-10-27-combat-deployment-zone-cells-design.md`(DZ1~DZ6, 보류)를 이 문서로 합쳤다. 옛 판은 git `10914570`. 2026-10-29 다시 줄임: §10·§11의 대안 비교·옛 규칙 대조표·점검 목록 원문은 git `a8612321e`.

## 1 목적과 범위

### 1.1 요청 (2026-10-27 사용자)

| 묶음 | 요청 |
|---|---|
| SO 모드 | "게임이 쓰는 CombatStageData·FieldDataSO를 도구에서 열고 저장할 수 있게." "필요한 SO들이 Stage Root Data SO랑 View Stage Data SO. 그 안에 든 SO와 수치 조정까지 표로." "DT용이 하나 있잖아? 그거랑 다르게 SO용으로 전환해서 SO를 만들 수 있게." |
| 추가 기능 | ① 3D로도 보이게, 유닛은 전투 유닛 프리팹으로 ② 적 공격 범위 미리보기(참고용) ③ 사각형 칠하기·복사/붙여넣기·좌우 대칭 ④ 고른 배치를 끌어서 옮기기 |
| 진영 | "적 진영이랑 아군 진영이 2줄씩 되어 있는데, 기획자가 수정할 수 있게. 적 진영을 한 칸만 가운데 놓을 수도 있고 … 아군도 가능할 수 있어요." → 줄 수만 하고 칸 단위는 보류(§7) → Core 변경 뒤 아군 칸 단위로 구현(§11) |
| 병합 대응 | "A1·A2(고지대 외형 회전 막기, 등록·빼기 맵 규칙 검사)부터 설계·계획 써줘"(§10) |
| 병합 최신화 | "툴을 최신화 해야 할 것 같은데 뭐 있는지" → "ViewProp의 Map Tiles(바닥 타일)는 무조건 넣어 달래요"(§12) |
| 후속 보완 | 2026-10-31 사용자 "고지대는 일단 보류하고 나머지 보완 내용을 설계·계획 문서에 반영해줘. 코드는 수정하지 말고."(§15) |
| 시안 적용 | 2026-11-02 사용자 "이 시안 기준으로 기존 기능이 빠지지 않게 설계·계획 문서에 반영해줘. 고지대는 계속 보류하고 코드는 아직 수정하지 말고."(§21) |

### 1.2 범위 밖

- 엑셀 배치를 SO로 바꾸는 변환. 유닛·장애물 키 체계가 다르다(§2.5).
- 상점·강화·이벤트 스테이지, 스테이지 맵(StageMap) 편집, 맵 SO 출현 가중치 편집.
- 맵 프리팹 자체의 배치(**ProjectL → FBX 배치** 도구가 만든 `MapDefinitionSO`를 고르는 것으로 대신).
- 게임 규칙 변경, 실제 전투 판정을 반영한 공격 범위, 애니메이션 재생, DT 모드 3D.

## 2 지금 구조

### 2.1 파일과 책임

모두 `Assets/ProjectL/Script/Tool/Editor/`다.

| 파일 | 책임 |
|---|---|
| `CombatLayoutWindow.SoMode.cs`(새) | SO 모드 화면: 목록·맵(2D/3D)·패널, 입력(`HandleSoPointer` 하나를 2D·3D가 같이 씀), 단축키, 범위 표시, 등록·저장 흐름 |
| `CombatStageSoDocument.cs`(새) | 작업본(순수 직렬화 값, 참조는 GUID), 층·칸 계산·바로 막기 `PlacementError`, 편집 연산(놓기·옮기기·지우기·상자·복사/붙이기·대칭·묶음 옮기기·구역 칠하기 `PaintZone`) |
| `CombatStageSoAssets.cs`(새) | 에셋 ↔ 작업본 읽고 쓰기, **필드 이름 원본**, 에셋 목록·공유 사용처, 외형 이름, Core 규칙 사본(`CanBeEnemy`·이웃 모양 판정·보스 역할·종류 고르기·벽/낭떠러지 이름), 아군 구역 해석 `ZoneOf` |
| `CombatStageSoValidation.cs`(새) | 임시 복제본 검사(`GetData()`)·키 짝·sourceId 짝·이웃 모양 외형 회전·맵 밖·공유 SO 조건 |
| `CombatStageSoCreation.cs`(새) | 새로 만들기·복제·짝 View 만들기·공유 SO 전용 복제·새 필드·표 등록/해제 |
| `CombatStageSoRange.cs`(새) | 공격 범위 모양(Core `AppointShapeCalculator` 호출) |
| `CombatStageSoVisuals.cs`(새) | 외형 표 읽기, 게임 배치 식 사본 `CombatStageSoSpace`(식마다 원본 자리 주석) |
| `CombatStageSoPreview.cs`(새) | 3D 미리보기(`PreviewRenderUtility`, 인스턴스·시점·고르기·정리) |
| `CombatStageSoChapter.cs`(새, §10) | 표 등록 상태의 맵 생성 규칙 판정(값·Core 계약 판정·바뀐 뒤 상태) |
| `CombatLayoutWindow.cs`(민형님, 바뀜) | SO일 때 갈라지는 한 줄씩 넷: `OnGUI`, `DrawHeader` 둘, `OnUndoRedo` |
| `ProjectL.Tool.Editor.asmdef`(바뀜) | 참조 셋: `Framework`(`DatabaseItem.GetData()`), `ProjectL.Core.Contracts`(장애물 키·맵 계약·아군 구역), `ProjectL.Core.Domain`(공격 모양·배치 정책) |

### 2.2 게임 SO 한 벌

```text
StageRootDataSO (Stage_*)        stageKey · mapKey · mapRole · stageData ─┐
                                                                         ▼
CombatStageData (Combat_*)       timelineSeed · objectiveKind · enemyLevel · cornerCutTicks
                                 참조: fieldDefinition · unitRoster · economy · enemyLoadout · headquarters · structureCatalog
                                 배치: 적 유닛 · 적 구조물 · 메인 캐릭터 칸(쓰이지 않음) · 적 본부 칸
                                 Deployment: playerDeploymentCells(비면 필드 아군 줄 수)
                                 environment: 지형(정의 + sourceId + 공개 대상) · 장애물 · 오브젝트
                                 railway: 켜기 · 레일 칸 · 첫 발동 · 반복(단위 = 진영 종료 횟수)
                                 (숨김 호환) initialWallCells · staticTerrainPlacements

ViewStageDataSO (StageViewProp_*) stageKey · 맵 프리팹/MapDefinitionSO · 위치·회전·배율 보정 · cameraSettings
                                  terrainRotations · objectRotations (sourceId별)
```

- 짝 맞춤 둘: Root와 View의 `stageKey`가 같아야 한다. View 회전의 `sourceId`는 CombatStageData 지형·오브젝트에 있어야 한다.
- 표: Root는 `InGameGroup/Stage/StageRootDataTable`, View는 `ViewGroup/StageViewProp/StageViewPropDataTable`의 `databaseItems`. CombatStageData는 표 없이 Root가 가리킨다. 두 표는 이미 Group에 들어 있어 Group은 바뀌지 않는다.

### 2.3 저장 흐름

작업본 → 임시 복제본(`Object.Instantiate`)에 쓰기 → 복제본 `GetData()`(전투 데이터 → Root → View) + 도구 검사 → 모두 통과해야 실제 에셋에 쓴다. 하나라도 실패하면 아무것도 쓰지 않는다. 쓰는 순서는 고친 공유 SO → Root·전투 데이터·View다. 이유: 잘못 저장된 SO 하나가 부트의 DB 생성, 곧 게임 시작 전체를 멈춘다.

<a id="24-검사가-잡는-곳-세-단계"></a>
### 2.4 검사가 잡는 곳

| 언제 | 무엇 | 원본 |
|---|---|---|
| 놓을 때 | 맵 밖, 같은 층 겹침 | 작업본 `PlacementError` |
| 검사·저장·등록 때 | 환경 배치(지형·장애물·오브젝트·레일), 아군 구역 칸(판 밖·중복), View 프랍·카메라·키, 스테이지 키 유일, 맵 밖(구역 칸 포함), 공유 SO 사용처, 이웃 모양 지형의 외형 회전(§10 MG4) | SO `GetData()` + `CombatStageSoValidation` |
| 등록·빼기, 등록된 스테이지의 맵 키·역할 저장 때 | 맵 생성 규칙(보스 하나, 출현 종류별 후보, 맵 모양) | Core 계약 + `CombatStageSoChapter`(§10 MG6~MG8) |
| '스테이지 값'에 늘(경고만) | 고정 적이 아군 구역 안, 출현 수 > 적이 놓일 칸, 아군이 놓일 칸 0, 고정 적 + 적 로드아웃 | `CombatStageSoZone` + Core `DeploymentZonePolicy`(§11 AZ6) |
| 전투 진입 때(Core) | 적 유닛·본부 칸(아군 구역 밖, 벽 위 금지), 다른 층 겹침, 적 자격, 로드아웃 출현 칸 수, 정의 표 등록 | Core 준비(`BattlePreparationCalculator`·`EnemySpawnCalculator`) |

`GetData()`는 **환경 입력과 구역 칸만** 검사한다(설계 중 정정). **FU1·FU2(§15)는 벽·낭떠러지 위 고정 적·구조물·본부만 검사·저장·등록의 오류로 앞당겼다(코드 반영, Unity 미확인).** 무작위 적 출현·그 밖의 진입 규칙은 계속 Core가 판정하고 PlacedStage로 확인한다.

### 2.5 설계 때 확인한 사실

- 기준: HEAD `8e5180eb`(2026-10-27). 지금(`10914570`) 전투 스테이지는 Boss·Normal_A·Normal_B·Normal_C·Test 다섯이다(용근님 `864a9d89`·`2805717b`, 재령님 `ba9b05fe`). 아래 수는 설계 당시 값이다.
- 공유(당시 13개 기준): `Field_15x15` 12, `UnitRoster_1`·`StageEconomy_1`·카메라 설정 13, 본부 12, 적 로드아웃 0.
- 정의 SO: 지형 7(Cliff·HighGround·PresenceModifier·Sandstorm·Tornado·Wall·WeakGround), 장애물 2(PowderBarrel·Supplies), 오브젝트 3(Flag·Mine·SheriffBadge), 구조물 1(Headquarters), 유닛 14. 정의 크기 칸(`footprintWidth`·`footprintHeight`)을 읽어 그린다.
- DT와 키가 다르다: CharacterMaster `Character_Key` 1001~ vs `UnitDataSO` 10~26, ObstacleMaster 키 vs 종류에서 나오는 장애물 키. 엑셀 Master SO는 전투가 읽지 않는다.
- 표에 등록하면 바로 Run 후보다: Root `mapKey`가 맵 키와 같고 역할이 Regular면 노드 후보(`ChapterProgressionCoordinator`). PlacedStage도 표 등록이 필요하다.
- 접근: Tool.Editor는 Systems를 참조하지 않는다. 대부분의 SO 형식(FieldDataSO·UnitDataSO·로스터·경제·로드아웃·구조물·ViewStageDataSO·카메라 설정·`CellInput`)이 internal이다. public은 StageRootDataSO·CombatStageData·지형/장애물/오브젝트 정의 SO. `InternalsVisibleTo`는 시험 어셈블리 둘뿐이다.
- 메인 캐릭터: 천명님 `4ae6a9b8`(2026-10-17) 뒤로 Core는 메인 캐릭터를 첫 손패 0번 슬롯으로 주고 위치는 플레이어 최초 배치가 정한다(`BattlePreparationCalculator.cs:110`). `mainCharacterAnchor`·`mainCharacterRotation`은 `BattleInitialization.MainAnchor`까지 가지만 그 뒤로 읽는 곳이 없고, `MainCharacterEntrySpec`도 쓰이지 않는다.
- 게임 배치 식(3D 사본의 원본): 칸 중심 `FieldGridMapper.CellCenterToLocal`, 방향 `RotationToWorld`(R0 = +Z), 칸 크기 GridOutline이면 `StageFieldViewData.outlineCellSpacing`, 맵 프랍 `StageViewPropPresenter.Show`(필드 루트 아래·보정 셋), 유닛 `FieldUnitPresenter.CreateView`·`CenterOf`(배율 × `fieldUnitScale`, 점유 칸 중심 평균 + 가장 높은 표면), 지형·오브젝트 `TerrainViewSet`·`FieldObjectPresenter`(오프셋·배율·sourceId별 회전, 고지대 `surfaceHeight`).

## 3 결정 기록

| ID | 결정 | 날짜·근거 |
|---|---|---|
| Q-SM1 | A: 도구는 `SerializedObject`·필드 이름으로 SO에 닿는다(SO 코드 무변경). 대안 B `InternalsVisibleTo`, C Systems 에디터 코드 | 2026-10-27 사용자 "모두 A" |
| Q-SM2 | A: 공유 수치는 읽기만 + "이 스테이지 전용으로 복제" | 같음 |
| Q-SM3 | A: 저장과 등록을 나누고 등록 버튼에서 확인 | 같음 |
| Q-SM4 | A: 맵 크기는 필드 SO 고르기 + 새 필드 만들기 | 같음 |
| Q-SM5 | A: 첫 판은 2D 격자, 3D는 뒤 Phase(→ AD1로 더함) | 같음 |
| Q-SM6 | A: 새로 만들기 기본 참조 = 지금 스테이지들이 쓰는 SO(계획 P2로 "틀 스테이지에서 이어받기") | 같음 |
| Q-SM7 | A: 전투 View와 같은 검증(MSBuild 안 돌림, 새 `[Test]` 없음, Unity 확인은 사용자) | 같음 |
| (전환안) | DT/SO 전환안 유지. 작업본을 SO 모양 하나로 합치는 안은 거절 | 2026-10-27 사용자 "지금 설계대로 DT/SO 전환안을 유지" |
| Q-AD1 | A: 3D 배치 식은 도구가 `CombatStageSoVisuals` 한 곳에 사본으로 두고 원본 자리를 주석으로 단다(대안 B: Systems `InternalsVisibleTo` + 게임 내부 클래스 호출) | 2026-10-27 사용자 "Q-AD1 A, Q-AD2 C, Q-AD3 A, Q-AD4 A" |
| Q-AD2 | C: 방향 있는 모양은 모든 방향 합친 모양이 기본이고 방향을 골라 볼 수 있다 | 같음 |
| Q-AD3 | A: 고른 적 하나 + "모든 적 겹쳐 보기" | 같음 |
| Q-AD4 | A: "좌우 뒤집기"와 "좌우 대칭 복사" 둘, 축은 늘 맵 가운데 세로줄 | 같음 |
| Q-AD5 | A: 상자 선택이 있으면 묶음으로 끌어 옮김 | 2026-10-27 사용자 "묶음으로 선택했으면 끌어 옮기게" |
| Q-AD6 | A: SO-e(문서·공유)는 네 기능 뒤 마지막에 한 번에 | 2026-10-27 사용자 "마지막에 한 번에" |
| (메인) | 메인 캐릭터 자리는 맵에서 빼고 값만 보존(계획 P-A1) | 2026-10-27, 쓰이지 않는 값을 배치처럼 그린 잘못 |
| (아군) | 아군을 스테이지에 미리 놓는 기능은 만들지 않는다. 목록에서 구분만 한다 | 2026-10-27 사용자 "아군은 미리 놓지 않아도 돼. 지금처럼 구분만 해 두고" |
| (진영) | 칸 단위 진영(DZ1~DZ6) 보류, 스테이지별 진영 줄 수만 도구에서 바꾼다 → §11이 대신함 | 2026-10-27 사용자 "일단 Core는 건드리지 말고 스테이지별 진영 줄 수만 도구에서 바꿀 수 있게 해줘" |
| Q-MG1~Q-MG4 | 모두 A(§10.3). MG-P1~MG-P7 그대로 | 2026-10-27 사용자 "Q-MG1~Q-MG4 모두 A, MG-P1~MG-P7·P-MG1~P-MG6 그대로 하고 MG-a 진행해" |
| Q-AZ1~Q-AZ4 | 모두 A(§11.3): 빈 목록 첫 편집은 줄 수 칸에서 시작, 마지막 칸 지우기는 막음, 경고만, 고정 적 + 적 로드아웃 경고 넣음. P-AZ1~P-AZ10 그대로 | 2026-10-27 사용자 "Q-AZ1~Q-AZ4 모두 A, P-AZ1~P-AZ10 그대로" |
| Q-AZ5 | A: 게임 화면의 칸 구역 표시는 전투 View 담당(Core 설계 PZ7). 도구는 칸 목록이 있는 스테이지에 안내 줄 → 2026-10-29 전투 View PV-a 병합으로 안내 줄을 지움(§11.9) | 2026-10-27 사용자 "Q-AZ5 A(게임 화면은 전투 View 담당, 도구는 안내 줄)" |

## 4 SO 모드 기본 (SM1~SM12)

### SM1 모드 전환

- 창 머리줄에 `DT | SO`. 메뉴와 창은 하나다. 모드마다 작업본이 따로 있고 전환해도 남는다.
- DT 모드 코드·동작은 바꾸지 않는다. 기존 창 파일 변경은 SO일 때 갈라지는 한 줄씩 넷이다(§2.1).
- SO 머리줄 버튼: 목록 새로고침·에셋에서 다시 읽기·검사·저장. 스테이지는 왼쪽 목록을 눌러 연다. 양쪽 패널 경계를 끌어 너비를 바꾼다(SO 모드만, DT는 고정 230·310).

### SM2 작업본 → 검사 → 쓰기

§2.3 흐름 그대로다. 도구가 `GetData()` 밖에서 더 보는 것: 스테이지 키 유일(모든 Root와 비교, 표 안 중복은 복제본 검사로 알 수 없어서), 맵 밖 배치(필드를 줄였을 때), 고친 공유 SO는 이 스테이지만 쓸 것. "검사" 버튼은 쓰기 없이 같은 검사만 돈다.

### SM3 에셋 읽고 쓰기는 한 곳에서, SO 코드는 그대로

- 필드 이름은 `CombatStageSoAssets.cs` 상수에만 둔다. 형식은 이름으로 찾는다(`FindAssets("t:UnitDataSO")`).
- 열 때 필요한 필드가 없으면 그 이름을 담아 바로 예외를 낸다. SO 쪽 이름이 바뀌면 조용히 틀리지 않는다.
- 공유 SO 값은 그 SO의 `PropertyField`로 그려 도구에 그 SO의 필드 이름을 두지 않는다. 열거 표시 이름(회전·승리 조건·맵 역할·공개 대상)은 저장하지 않는 복제본에서 읽는다(`FillNames`).

### SM4 새로 만들기·복제

- 새 셋: `Stage_<이름>`·`Combat_<이름>`·`StageViewProp_<이름>`, 틀과 같은 폴더(`AssetDatabase.CopyAsset`).
- 새로 만들기는 틀의 참조·View 값을 이어받고 배치와 아군 구역 칸 목록을 비운다(숨김 호환 배치도). 복제는 배치·칸 목록까지 복사한다.
- **파일을 만들기 전에** 틀 복제본에 새 작업본을 써서 검사한다. 걸리면 파일이 하나도 생기지 않는다.
- 키 제안은 틀 키의 백 단위 안 빈 값(일반 11xx, 보스 21xx). 같은 이름 파일이 있거나 이름에 못 쓰는 글자가 있으면 거절한다. 새 셋은 미등록으로 만든다.
- 짝 View가 없는 스테이지는 'View → 틀의 View로 만들기' 버튼으로 붙인다(저장 때 자동으로 만들지 않음).

### SM5 맵 위 층

- 층 열거: 레일·지형·오브젝트·장애물·구조물·본부·적 유닛(값이 클수록 위). 메인 캐릭터 층은 SO-f에서 뺐다(§2.5, 계획 P-A1). 그 값은 '스테이지 값'에 "전투에서 쓰이지 않음"으로 읽기만 보인다.
- 맵 크기는 필드 SO에서, 칸 바탕색(아군 구역 / 나머지)은 아군 구역 해석(§11 AZ2)에서 읽는다. 맵 좌표는 왼쪽 아래 (0,0).
- 정의 목록 이름은 View DB 외형 표 `displayName`(한국어)이다. 짝은 파일 이름이 아니라 키·종류로 맞춘다(`Unit_UndeadCannonCart`↔`UnitVisual_Cannon` 같은 이름 어긋남이 있어서). 장애물 키는 Core `ObstacleContentProfile.KeyFor`로 구한다. 목록·팝업은 "한국어 (짧은 에셋 이름)", 맵 칸은 한국어만.
- 적 유닛 목록은 Core 적 자격 조건의 사본 `CanBeEnemy`(Regular 역할이고 PlayerOnly 아님)로 거른다. 아군 정의는 회색 목록으로만 보인다.
- sourceId는 도구가 정한다(이 스테이지 지형·오브젝트의 최댓값 + 1).

### SM6 즉시 막기는 최소, 판정은 SO 검사

- 바로 막는 것은 맵 밖과 같은 층 겹침뿐이다. 같은 층 = 적 유닛끼리, 구조물·본부끼리, 장애물끼리, 레일끼리, 지형·오브젝트는 **같은 정의끼리만**.
- 현재 구현은 **벽·낭떠러지 위 고정 배치만 저장 오류로 추가했다(FU1·FU2, §15, Unity 미확인).** 그 밖의 다른 층끼리 겹침·아군 구역 규칙은 전투 진입 때 Core가 검사한다. 판정 원본은 Core이며, 도구의 제한된 사전 검사 사본과 원본을 한 곳에 기록한다. 아군 구역·출현 수 경고와 다른 배치 규칙은 바꾸지 않는다.

### SM7 공유 SO

- 필드·로스터·경제·적 로드아웃이 다른 스테이지와 공유되면 읽기만 하고 **이 스테이지 전용으로 복제** 버튼을 둔다. 복제 이름 `<원본>_<스테이지 이름>`, 새 키(필드 `fieldId`는 `<원본 fieldId>_<스테이지 키>`, 나머지는 같은 형식 키 최댓값 + 1), 원본이 든 표에 등록. 참조는 저장 때 바뀐다.
- 바로 고치기 조건은 "이 스테이지를 뺀 사용처 0"이다. 값은 작업본 JSON(`sharedEdits`)에 담고 편집 복제본은 창이 들고 있다(`HideFlags.DontSave`). 키 필드는 막는다.
- 정의 SO와 카메라 설정은 복제하지 않고 읽기만 한다(정의를 복제하면 새 종류가 생겨 키·외형 표가 따라 바뀌어야 함).
- 새 필드 만들기: 가로·세로·대기·아군 줄 수 → `Field_<가로>x<세로>`, `fieldId` `field_<가로>x<세로>`, 필드 표 등록. 적 배치 깊이는 틀 필드 값을 쓴다(§11 P-AZ8).

### SM8 표 등록

- 저장과 등록을 나눈다. 등록은 **저장된 에셋 기준**이고 등록 전에 같은 검사와 맵 생성 규칙 판정(§10)을 한다. 저장 안 한 변경이 있으면 먼저 저장을 요청한다.
- 확인 창("같은 맵 키의 Run 무작위 후보에 들어갑니다", 바뀌는 후보 수) 뒤 Root 표·View 표에 함께 넣고 함께 뺀다.
- 표는 형식으로 찾는다(`t:StageRootDataTableSO`·`t:ViewStageDataTableSO`). 같은 형식 표가 여럿이면 거절한다.

### SM9 미리보기

첫 판은 2D 격자였다. AD1로 3D를 더했다(§5).

### SM10 숨김 호환 필드

`initialWallCells`·`staticTerrainPlacements`는 편집하지 않고 저장 때 그대로 둔다(새로 만들기의 배치 비우기만 예외). FU2가 반영된 일반 검사·저장·배치 복제는 적용 후 임시 복제본에서 벽·낭떠러지 칸을 읽어 사전 검사에 포함하되 호환 필드를 보존한다. 새로 만들기의 배치 비우기만 검사 복제본과 생성 에셋 양쪽에서 비운다(§15.2.1).

### SM11 이력과 초안

창 Undo(`Undo.RecordObject(this)`), 초안은 EditorPrefs `DraftKey + ".So"`. 저장 뒤 되돌리려면 작업본에서 되돌리고 다시 저장한다. 보정 셋(Vector3) 입력은 값이 바뀔 때마다 Undo가 하나씩 쌓인다.

### SM12 파일 나누기

§2.1 표가 지금 파일 구조다. 설계 때는 새 파일 다섯이었고, 추가 기능(AD5)으로 셋, 병합 대응(§10)으로 하나가 더해졌다.

### 편집 대상 표

| 묶음 | 값 | SO | SO 모드에서 |
|---|---|---|---|
| A 맵 위 배치 | 적 유닛(정의·칸·회전·격파 목표·시작 아이템), 적 구조물, 적 본부, 지형(정의·sourceId·공개 대상·토네이도 무작위 시작), 장애물, 오브젝트, 레일 칸, 아군 구역 칸 | CombatStageData | 맵 위 + 고른 칸 패널 |
| | 지형·오브젝트 외형 회전(sourceId별, 이웃 모양 지형 제외) | ViewStageData | 고른 칸 패널 |
| B 스테이지 값 | 스테이지 키·맵 키·맵 역할 | Stage Root | 패널 |
| | 시드·승리 조건·적 레벨·벽 모서리·레일 켜기·첫 발동·반복, 참조(필드·로스터·경제·적 로드아웃·본부·구조물 목록) | CombatStageData | 패널 |
| | 맵 프리팹/`MapDefinitionSO`·보정 셋·카메라 설정 | ViewStageData | 패널 |
| C 공유 수치 | 필드(가로·세로·대기·아군 줄 수), 로스터, 경제, 적 로드아웃 | 각 SO | 공유면 읽기만 + 전용 복제, 이 스테이지만 쓰면 바로 고침 |
| | 카메라 조작 값, 유닛·구조물·지형·장애물·오브젝트 정의 수치 | 각 SO | 읽기만 |
| 표시 안 함 | 숨김 호환 벽·정적 지형, 메인 캐릭터 칸, 필드 적 배치 깊이(Core가 읽지 않음) | 각 SO | 보존(SM10, P-A1, P-AZ8) |

## 5 추가 기능 (AD1~AD5)

### AD1 3D 보기

- 맵 위 `2D / 3D` 전환과 "시점 초기화". 두 보기 모두 같은 입력으로 편집한다.
- `PreviewRenderUtility`의 따로 된 장면에 그린다(작업 씬에 오브젝트를 만들지 않음). 미리보기 공간은 게임의 필드 로컬이다.
- 놓는 것: 맵(맵 SO의 프리팹 또는 맵 프리팹, 보정 셋), 적 유닛·구조물·장애물·본부(유닛 외형 프리팹 × `fieldUnitScale`, 적은 반 바퀴), 지형·오브젝트(외형 프리팹·오프셋·배율·외형 회전, 고지대 표면 높이). 외형이 없는 정의는 층 색 상자.
- 인스턴스 안의 `Canvas`·`Camera`·`AudioListener`·`AudioSource`는 끄고 저장되지 않게 표시한다.
- 다시 만들기: 맵은 맵·보정이 바뀔 때만, 배치는 작업본 JSON이 바뀔 때만.
- 고르기는 바닥 평면(높이 0) 기준. 시점은 DT 모드와 같은 조작이지만 코드는 새 클래스에 둔다(DT `CombatLayoutPreview`는 그대로).
- 구현에서 정한 것:
  - 맵 SO에 `MapAssembler.Instantiate`를 쓰지 않고 프리팹만 놓는다. `MapAssembler`가 `MapBakedLighting.Apply`로 에디터 전역 `LightmapSettings`를 바꿔 작업 씬 조명이 바뀌기 때문이다. 조명은 참고용이다.
  - 칸 덧그리기(아군 구역·레일·범위·고른 칸, 상자·고스트 테두리)는 화면 좌표로 그려 늘 위에 보인다. 레일은 덧그리기로만 보인다. 이름 글자는 없다.
  - 정리는 `OnDestroy`(SO 모드 partial)와 `AssemblyReloadEvents`로 한다. DT 쪽 `OnDisable`은 고치지 않았다.
  - CellPrefab 보기 방식이면 1×1로 근사한다(지금 운영 값은 GridOutline).
- 현재 게임과 다른 곳: 고지대 이웃 모양은 늘 Center R0이다. **B1 고지대 이웃 모양 개선은 보류**다. 토네이도 위치·높이와 공개 대상 기준은 §15대로 반영했다(컴파일·Unity 미확인).

### AD2 적 공격 범위 미리보기 (참고용)

- 대상: 고른 칸의 적 유닛, 또는 "모든 적 겹쳐 보기".
- 계산: 유닛 SO `attackRange`·`targetingMethod`, 지정 방식 SO `shape`·`splash`를 읽고 Core `AppointShapeCalculator`(`CreateBaseMask`·`CreateDirectionMask`·`CreateSplashOffsets`)로 모양을 만든다. 점유 칸마다 더하고 맵 밖은 버린다. 모양 규칙은 Core 한 곳에만 있다.
- 방향 버튼은 Core가 받아 주는 방향만 화살표로 만든다(대각선 직선은 대각선 넷). 방향 없는 사각형(RadialSquare)은 버튼을 숨긴다. 고른 방향이 그 모양에 없으면 "전체"로 돌아간다.
- 색: 진한 주황 = 사거리(겹친 수가 많을수록 진함, `×n`), 옅은 주황 = 스플래시로만 닿는 칸.
- 맵 아래에 늘 "참고용: 벽·지형·고지대·모래폭풍·유닛 점유·대포 인접 금지는 반영하지 않습니다"를 띄운다.
- 모양은 (유닛 정의, 방향)마다 기억하고 목록 새로고침 때 비운다. 그래서 유닛 SO를 고치면 새로고침 전까지 예전 모양이 남는다.

### AD3 사각형 칠하기·복사/붙여넣기·좌우 대칭

- 칠하는 도구(지형·장애물·오브젝트·레일·지우기·아군 구역)의 Shift+끌기 = 사각형 칠하기. 배치 도구는 칸마다 바로 막기를 거쳐 막힌 칸은 건너뛰고 "n개 놓음, m개 건너뜀"을 알린다.
- 복사(Ctrl+C)는 상자 안 적 유닛·구조물·장애물·지형·오브젝트·레일(본부 제외)을 상자 왼쪽 아래 기준으로 담는다. 붙여넣기(Ctrl+V)는 고른 칸을 그 기준 자리로 붙인다. 지형·오브젝트는 새 sourceId를 받고 외형 회전도 따라간다. 복사본은 창에 있어 다른 스테이지에도 붙인다.
- 좌우 뒤집기·좌우 대칭 복사: 대상은 상자 안, 없으면 전체. 기준 칸 `x' = 가로 − 1 − 오른쪽 끝 x`, 회전 R90 ↔ R270, 외형 회전도 뒤집는다. 뒤집기는 하나라도 막히면 하지 않는다. 대칭 복사는 본부를 빼고 막힌 사본을 건너뛴다(가운데 줄 배치의 사본은 겹쳐서 건너뜀).
- 상자 편집은 아군 구역 칸을 옮기지 않는다(§11 P-AZ6).
- 묶음 연산은 작업본 **복제본에서 먼저** 돌려 결과를 보고, 바뀌는 것이 있을 때만 Undo를 남긴다. Ctrl+C·V는 키가 아니라 에디터 명령(Copy·Paste)으로 받는다. "상자 안" = 기준 칸(왼쪽 아래)이 상자 안.

### AD4 끌어서 옮기기

- 맵 입력은 맵 영역 하나 + `hotControl`로 받는다(끌기가 맵 밖으로 나가도 놓을 때까지 받음).
- 선택 도구: 배치 칸에서 끌기 = 맨 위 배치 옮기기(본부 포함, 레일 제외), 빈 칸에서 끌기 = 상자 선택, 상자 안에서 끌기 = 묶음 옮기기, **Shift+끌기 = 늘 상자 선택**(사용자 보고로 더함), 같은 칸에서 떼기 = 칸 고르기.
- 고스트는 초록(놓을 수 있음)·빨강(막힘). 빨강이면 그대로, Esc면 취소. 묶음은 하나라도 막히면 통째로 옮기지 않고, 옮기면 상자도 따라간다.
- 단축키: Delete(상자 안 배치 또는 고른 칸 맨 위, 본부 제외), R(시계 방향 회전), Esc. 입력 칸 편집 중에는 쓰지 않는다.
- 맵의 유효 칸을 왼쪽 클릭하면 입력 칸 포커스를 해제한다. 숫자·텍스트 편집 뒤 상자를 선택하거나 붙일 칸을 고르면 Ctrl+C/V를 다시 받을 수 있어야 한다. 검사 통과·저장 여부는 복사/붙여넣기의 선행 조건이 아니다.

### AD5 파일 나누기

새 파일 셋(`CombatStageSoRange`·`CombatStageSoVisuals`·`CombatStageSoPreview`)과 asmdef `ProjectL.Core.Domain` 참조 한 줄. §2.1 표에 반영했다.

## 6 진영 줄 수 (구현한 대안)

- 지금: '스테이지 값 → 아군 배치 구역'에서 칸 목록이 빌 때만 아군 줄 수 슬라이더(1~세로)를 보인다(§11 AZ5). 줄 수는 필드 SO 값이라, 공유 필드면 전용 복제 확인 창 뒤 복제본에만 쓰고, 이 스테이지만 쓰는 필드면 바로 고친다(SM7 방식).
- 바뀐 것: 옛 판은 아군·적 줄 수 슬라이더 둘과 "두 줄 수 합 < 세로" 사본(`MaxDepthBeside`), 적 진영 밖 경고였다 → 천명님 `5a1bfbe1f`(적 구역·합 규칙 삭제)에 맞춰 AZ-a에서 아군만 남기고 걷었다(2026-10-27).

## 7 보류: 배치 진영 칸 단위 (DZ1~DZ6)

- 상태: **§11이 대신함**(2026-10-27). 보류 이유였던 Core 변경을 천명님이 다른 규칙으로 했다(`5a1bfbe1f`: 적 구역 없음, 아군 칸 목록 하나). Q-DZ1~Q-DZ5는 정하지 않은 채 닫는다.
- 같은 기능을 용근님 쪽 세션이 Core 요청으로 따로 썼다: [배치 구역 칸 단위 Core 요청](../handoffs/2026-10-27-combat-deployment-zone-cells-core-request.md)(Core 전달 문서 §43). Core는 그 요청과도 다른 결정으로 구현했다.

<a id="71-당시-사실"></a>
<a id="72-초안"></a>
<a id="73-core-요청-43과-다른-점"></a>
### 7.1 당시 초안 요지

| ID | 초안(닫음) |
|---|---|
| DZ1 | `CombatStageData`에 칸 목록 둘(아군·적). 비면 필드 깊이 |
| DZ2 | Core 진영 값 `DeploymentZones` 하나를 정책·출현·소환이 읽음 |
| DZ3 | `BattlePreparation` → `CombatBattleFacts` → Buffer가 진영 칸을 나름 |
| DZ4 | 판 안·중복 없음·**아군/적 겹침 금지**(§43 요청은 겹침 허용) |
| DZ5 | 도구 "진영" 붓, 깊이 칸에서 시작, 경고 셋 |
| DZ6 | Fixture 개발 진입·DT 모드는 깊이 그대로 |

- Q-DZ1(칸 자리)·Q-DZ2(Core 구현 주체)·Q-DZ3(칸 규칙)·Q-DZ4(경고 무게)·Q-DZ5(첫 편집 시작점)는 결정하지 않았다. Q-DZ5의 권장 A는 §11 Q-AZ1 A로 이어졌다.
- 사실 정정(당시 찾음): 옛 초안은 `CombatPlacementCalculator.cs:164`를 "소환 칸 후보"로 적었다. 실제로는 전투 시작 때의 적 재배치이고, 소환수 자리는 구역을 보지 않는다(§43 요청 §6).

## 8 원칙 점검과 남은 약점

| 기준 | 점검 |
|---|---|
| SSOT | 배치 규칙 원본은 SO `GetData()`와 Core다(도구는 다시 쓰지 않음). 필드 이름 원본은 `CombatStageSoAssets` 하나, 공격 모양·아군 구역·맵 후보 묶음·맵 모양은 Core 함수·계약, 외형 값은 View DB 표다 |
| SOLID | 화면·작업본·읽고 쓰기·검사·만들기/등록·범위·외형·3D·맵 판정이 파일로 나뉜다. DT 코드와 섞지 않는다 |
| FW·DB 규칙 | 도구는 기획 입력 계층(SO)만 쓴다. 런타임 Data·Buffer·FW 계약은 그대로다 |
| 독립성 | DB SO 코드·게임 런타임·DT 모드는 바뀌지 않는다. SO 필드 이름이 바뀌면 열 때 드러난다 |

남은 약점:

- 필드 이름 문자열이 SO 코드와 도구 두 곳에 있다(Q-SM1 A의 대가). SO 배치 칸에 새 필드가 생기면 도구는 쓰지 않고, 배열을 늘릴 때 Unity가 앞 칸 값을 복사한다(바로 드러나지 않음).
- 도구에 사본이 있다. 원본이 바뀌면 함께 맞춰야 한다(공유 메모에 적음).
  - 3D 배치 식(`CombatStageSoSpace`), 적 자격 `CanBeEnemy`, RadialSquare 방향 없음.
  - §10: 이웃 모양 판정·회전 금지, 보스 역할·종류 고르기·연결 방식.
  - §11: 아군 구역 대체 규칙 한 줄, 벽·낭떠러지 종류 이름, 참고 수 빼기 식.
  - 맞추지 않아도 도구 표시·막기가 틀릴 뿐 게임 동작은 그대로다.
- 공유 SO의 깊은 규칙(경제·로드아웃 `Validate()`, 로스터 소속)은 `GetData()`가 던지는 것만 잡는다.
- 정의 목록은 표 등록과 상관없이 모든 정의 SO를 보인다. 표에 없는 정의를 놓으면 전투 진입 때 멈춘다.
- 후속 범위: 벽·낭떠러지 위 고정 배치 검사, 토네이도 위치·높이, 공개 대상 기준, 공격 범위 안내(§15·계획 §11). **고지대 이웃 모양 B1은 보류**이며, 기존 고지대 정의·높이·회전 검사를 삭제하거나 변경하지 않는다.

## 9 다른 작업자 영향

| 누구 | 무엇 | 공유 메모 |
|---|---|---|
| 민형님(도구 작성자) | 기존 창 파일 네 줄 분기, asmdef 참조 셋, `사용법.md`·`README.md` 링크 단락, 새 파일 아홉, SO 모드가 같이 쓰는 DT 멤버(`EndStroke`·`lastPainted`·`DraftKey`), DT 무변경, 기존 도구 시험 무변경, DT·엑셀 도구 줄 수 표시는 민형님 몫(Core 설계 PZ7) | [민형님 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-minhyeong-share.md) |
| 천명님(Core) | Core 코드 무변경. 도구가 부르는 Core 공개 API·계약, 도구에 있는 Core 규칙 사본, 보고만(메인 캐릭터 자리 미사용·낡은 툴팁 "Player 배치 구역 안이어야 합니다", PZ-d 때 메인 캐릭터 필드 삭제 주의) | Core 전달 문서 [§42](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)(§42.1~§42.4), §43 응답 끝 한 줄 |
| 전투 View(용근님) | 게임 코드 무변경. 3D 배치 식 사본(원본 ↔ 사본), 이웃 모양 사본, 도구가 이름으로 읽는 View DB 필드, 게임 화면 칸 구역 표시(10-29 PV-a로 풀림) | [전투 View 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) |
| 호진(StageMap·View DB) | 새 Regular 스테이지를 등록하면 그 맵 노드 후보가 된다, 보스 교체가 다른 스테이지를 표에서 뺀다, 도구가 만드는 에셋·이름, 도구가 이름으로 읽는 필드(View DB·맵 SO·Root 표·`playerDeploymentCells`·`spawnCount`·지형 `kind`) | [호진 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-hojin-share.md) |

- 모든 공유 메모는 미전달이다.
- 용근님이 `c40fd37b`(2026-10-27)에서 도구 두 곳을 고쳤다: 레일 필드 이름 상수를 `firstActivationAfterControllerTurns`·`repeatIntervalControllerTurns`로, 이름표를 "레일 첫 발동(진영 종료 횟수)"·"레일 반복(진영 종료 횟수)"로.

## 10 병합 대응 A1·A2 (MG1~MG9)

- 상태: 구현 완료. 결정 2026-10-27 사용자(Q-MG1~Q-MG4 모두 A, MG-P1~MG-P7 그대로). MG-a·MG-b Unity 확인 문제 없음(사용자), 커밋 `86b447cd`. 결과는 [계획 §6](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#6-병합-대응-a1a2-계획-mg-amg-c)이다.
- 요청: 2026-10-27 사용자 "A1·A2(고지대 외형 회전 막기, 등록·빼기 맵 규칙 검사)부터 설계·계획 써줘". 목록 원문은 계획 §2.2다.
- 범위: 도구의 SO 모드 파일만 고친다. 게임 런타임·DB SO 코드·맵 SO 값·DT 모드는 바꾸지 않는다.

### 10.1 확인한 사실 (HEAD `08fb9ace`)

**A1 이웃 모양 지형의 외형 회전**

- 지형 외형의 모양 칸(OneSide~FourSide) 중 하나라도 차 있으면 "이웃 모양 지형"이다(`TerrainEdgeShapePrefabs.HasAny` → `TerrainVisualData.UsesEdgeShapes`). 지금은 고지대(`TerrainVisual_HighGround`) 하나다.
- 이웃 모양 지형의 원본에 스테이지 외형 회전 **항목**이 있으면 전투 화면이 그 지형을 그리는 순간 예외로 멈춘다. 값이 R0이어도 멈춘다. 값이 아니라 "적었는지"를 보기 때문이다(`CombatTerrainLook.Resolve`, `StageEnvironmentVisualData.TryGetTerrainRotation`). 숨은 지형은 공개되는 순간 멈춘다.
- 저장과 부트는 잡지 않는다. View SO `GetData()`는 sourceId와 회전 값만 보고, 외형 표와의 짝은 그리기에서만 본다.
- 도구가 항목을 새로 만드는 길은 '외형 회전' 팝업과 정의 변경 둘이다. 복사·대칭·뒤집기는 원본에 있던 항목만 옮긴다.
- 당시 운영 View 다섯의 `terrainRotations`와 숨김 호환 `staticTerrainPlacements`는 모두 비어 있었다(깨진 스테이지 없음).

**A2 표 등록과 맵 생성 규칙**

Run이 챕터에 들어갈 때 `StageGeneratorSystem.PrepareGraph(맵 키)`가 맵을 만든다. 실패하면 예외가 나서 Run 시작이 멈춘다.

| 규칙 | 원본 | 도구가 부를 수 있나 |
|---|---|---|
| 그 맵 키의 Root는 **표에 든 것만** 센다. 하나도 없으면 예외 | `StageTable.GetStages` | — |
| 역할이 Regular면 일반, 그 밖은 보스다. 보스는 정확히 하나 | `PrepareGraph` | 아니오(Systems internal) |
| 일반 스테이지의 종류는 상세 SO 형식으로 정한다: 전투·상점·이벤트(=회복)·강화 | `StageGeneratorSystem.KindOf` | 아니오 |
| 종류 규칙이 활성(가중치 > 0 또는 최소 > 0)이면 후보가 1개 이상 있어야 한다 | `ChapterStagePool` 생성자 | **예**(Core.Contracts public) |
| 가중치 합 > 0, 최소 배치 자리 ≤ 층 수 − 2, 보스가 후보에 없음, 후보가 묶음끼리 겹치지 않음 | `ChapterGraphSpec` 생성자 | **예** |
| 맵 SO 연결 방식을 Core 연결 방식으로 바꾼다 | `StageGeneratorSystem.ToConnection` | 아니오 |

- DB 생성(`StageTable`)은 키 중복과 값 범위만 본다. 잘못 등록해도 부트는 통과하고 Run 시작에서 멈춘다.
- 생성기 안에 남은 예외 셋(최소 배치 자리 어긋남·최소 개수 미달·고정 노드 묶음 없음)은 spec 검사 뒤의 방어용이거나 개발 진입 전용이라 등록과 상관없다(`ChapterGraphGenerator`).
- 도구가 표 상태를 바꾸는 길은 둘이다: '표에 등록'·'표에서 빼기', 그리고 **등록된 스테이지의 '맵 키'·'맵 역할'을 바꿔 저장**(목록 원안에 없던 길). 새로 만들기·복제는 미등록이라 영향이 없다.
- 도구는 전투 스테이지만 열지만, 맵 판정에는 상점·회복·강화 Root도 함께 센다.
- 보스 바꾸기는 한 번에 해야 한다. 새 보스를 먼저 넣으면 보스가 둘이고, 옛 보스를 먼저 빼면 보스가 없다.
- 당시(10-27) 맵 1: 맵 SO `StageMap_Chapter1`(층 7, 출현 전투 80/1·상점 9/1·회복 7/1·강화 4/1), 보스 `Stage_Normal_A`(1101, 재령님 `ba9b05fe`가 Regular → Boss), 전투 후보 `Stage_Normal_B`·`Stage_Normal_C`, 미등록 `Stage_Boss`(2101)·`Test`(10000).

### 10.2 설계 (MG1~MG9)

| ID | 내용 | 코드 |
|---|---|---|
| MG1 | **이웃 모양 지형 판정은 한 곳.** 지형 외형 SO마다 모양 칸 다섯(`oneSidePrefab`·`twoSidePrefab`·`twoSideOppositePrefab`·`threeSidePrefab`·`fourSidePrefab`) 중 하나라도 찬 종류를 모은다(게임 `HasAny` 사본, 원본 주석). 정의 → 종류는 기존 `VisualId`. 화면은 목록 새로고침 때의 캐시, 저장 검사는 새로 읽는다 | `CombatStageSoAssets.EdgeShapeTerrainKinds`·`UsesEdgeShapes`, 카탈로그 `EdgeShapeKinds` |
| MG2 | **고른 칸 패널.** 이웃 모양 지형이면 '외형 회전' 팝업 대신 "외형 회전: 이웃 모양이 정함" 읽기 줄. 항목이 있으면 경고와 '외형 회전 지우기'(Undo 하나) | `DrawSoViewRotation`(셋째 인자 `edgeShaped`) |
| MG3 | **정의 변경.** 이웃 모양 지형으로 바꾸면 같은 변경에서 그 원본의 외형 회전 항목을 지우고 알린다(Q-MG1 A) | `DrawSoMarker`(`apply` 안에서 지움) |
| MG4 | **저장 검사.** 이웃 모양 지형 원본의 외형 회전 항목을 문제로 낸다: "이웃 모양 지형 {이름}(sourceId n)에 외형 회전이 있습니다. 모양이 회전을 정하므로 전투 화면이 멈춥니다." 저장·등록·검사가 같은 검사를 쓴다. 숨김 호환 지형은 종류를 몰라 보지 않는다 | `CombatStageSoValidation.CheckPairs` |
| MG5 | **맵 판정 재료.** 표에 든 모든 Root(스테이지 키·맵 키·역할 이름·상세 SO 형식 이름)와 표에 든 맵 SO(맵 키·층 수·폭·시드·연결 방식 이름·연결 가중치 둘·추가 연결 확률·출현 넷의 가중치와 최소)를 읽는다. 필드 이름은 이 파일에만(SM3). 맵 SO 하나를 읽지 못하면 그 맵만 "읽지 못함" | `CombatStageSoAssets.ReadChapterStages`·`ReadChapterStage`·`IsBossRole`·`KindOfDetail`·`ReadMapRules`, 카탈로그 `ChapterStages`·`MapRules` |
| MG6 | **맵 판정.** 한 맵의 Root 목록과 맵 규칙으로 결과(보스, 종류별 후보 수, 문제)를 낸다. 보스 수·종류 고르기·연결 방식은 도구 사본, 후보 묶음과 맵 모양은 Core 공개 계약 `ChapterStageDistributionRule`·`ChapterStagePool`·`ChapterGraphSpec`을 실제로 만들어 Core가 판정한다(Q-MG2 A). Core 예외 문구를 그대로 문제로 쓴다 | 새 `CombatStageSoChapter.cs`의 `Check`(`ProblemOf`) |
| MG7 | **바뀐 뒤 상태로 판정.** 변경(넣음·뺌·맵 키나 역할 바꿈)을 지금 표 상태에 덮어 영향 맵(바뀌기 전·뒤 맵 키)마다 지금과 바뀐 뒤를 판정한다(통과 / 이번 변경이 깨뜨림 / 이미 깨짐 / 맵 SO 없음). 등록·빼기는 저장된 에셋 값, 저장은 작업본 값. 판정 재료는 이때 새로 읽는다 | `CombatStageSoChapter.Judge(stages, maps, rootGuid, next, dropGuid)`, 판정 `CombatStageSoChapterVerdict` |
| MG8 | **등록·빼기·저장 흐름.** 등록·빼기는 확인 창 전에, 저장은 기존 검사 뒤에 MG7을 돈다(Q-MG4 A). 이번 변경이 깨뜨리면 막고, 보스가 둘이 되면 "기존 보스 X를 표에서 빼고 진행" 교체 선택지(Q-MG3 A). 이미 깨진 맵이면 경고 창 뒤 진행. 확인 창에 바뀌는 수(예: "맵 1 전투 후보 2 → 3") | 창 `JudgeSoChapter`·`ConfirmSoChapter`·`UnregisterSoBoss`·`SoChapterSaveChange`, `SetSoRegistered`·`SaveSoStage`·`CheckSoStage` |
| MG9 | **'스테이지 값'의 맵 생성 줄.** 등록 줄 아래에 작업본 맵 키의 지금 상태(예: "맵 1 생성: 보스 Stage_Normal_A(1101) · 전투 2(80·1) · …")와 이 스테이지의 자리(보스·전투 후보·미등록). 문제가 있으면 빨간 상자, 맵 SO가 없으면 "맵 키 N의 맵 SO가 없습니다(이 맵 키로는 Run이 만들어지지 않음)" | 창 `DrawSoChapterLine`, `CombatStageSoChapter.Summary`·`RoleOf` |

<a id="103-정할-것-q-mg1q-mg4"></a>
### 10.3 결정 (Q-MG1~Q-MG4)

모두 A(2026-10-27 사용자).

| ID | 질문 | 결정(A) | 버린 안 |
|---|---|---|---|
| Q-MG1 | 정의를 이웃 모양 지형으로 바꿀 때 외형 회전 항목은? | 같은 변경(Undo 하나)에서 지우고 알린다 | B 그대로 두고 패널·저장 검사가 잡는다 |
| Q-MG2 | 맵 규칙을 어떻게 판정하나? | Core 공개 계약 셋을 만들어 Core가 판정한다. 맵 SO 값이 원래 틀린 경우도 같이 드러난다 | B 도구 사본 두 규칙(보스 하나, 활성 종류 후보 1개 이상) |
| Q-MG3 | 바뀐 뒤 맵이 깨지면? | 이번 변경이 깨뜨리면 막는다. 보스가 둘이 되면 교체 선택지. 이미 깨진 맵이면 경고 창 뒤 허용 | B 경고만 / C 늘 막음(보스 교체는 도구 밖) |
| Q-MG4 | 언제 판정하나? | 등록·빼기, 그리고 등록된 스테이지를 저장하면서 맵 키·역할이 바뀔 때 | B 등록·빼기만 |

<a id="104-설계에서-정한-것-확인-대상"></a>
### 10.4 설계에서 정한 것 (MG-P1~MG-P7)

모두 그대로(2026-10-27 사용자).

| ID | 정한 것 |
|---|---|
| MG-P1 | 지형 외형은 형식으로 찾은 에셋 중 종류별 이름순 첫 것을 쓴다(`VisualTable` 규칙). 게임은 등록 표의 것을 쓰지만, 지금 지형 외형은 종류마다 하나라 결과가 같다 |
| MG-P2 | 붙여넣기·대칭 복사·뒤집기는 바꾸지 않는다. 새 항목을 만들지 않고, 원본에 있던 항목은 MG4가 잡는다. 목록 원안의 "붙여넣기에서 회전 빼기"와 다르다 |
| MG-P3 | 3D 미리보기의 외형 회전 적용(`CombatStageSoPreview.VisualRotation`)은 그대로 둔다. 이웃 모양 3D는 B1 범위이나 2026-10-31 사용자 지시로 보류한다 |
| MG-P4 | 맵 판정의 "표에 든 Root"는 Root 표 하나다(형식으로 찾고, 여럿이면 등록 거절, 계획 P5). View 표는 맵 판정에 쓰지 않는다 |
| MG-P5 | 종류를 고를 수 없는 Root(상세 SO가 넷 중 하나가 아니거나 비어 있음)가 있으면, 그 맵은 "판정 못 함" 문제로 보인다. 게임도 이 경우 예외를 낸다 |
| MG-P6 | 맵 SO가 없는 맵 키는 판정하지 않고 안내만 한다(막지 않음). 게임은 그 맵 키로 챕터에 들어갈 때만 맵 SO를 찾는다 |
| MG-P7 | 맵 생성 줄(MG9)은 목록 새로고침 때의 캐시다. 맵 SO나 다른 Root를 도구 밖에서 고치면 새로고침 전까지 예전 값이 보인다. 막을지는 늘 새로 읽어서 정한다(MG7) |

<a id="105-원칙-점검"></a>
### 10.5 남은 약점

- 사본 다섯(이웃 모양 판정·회전 금지, 보스 수·종류 고르기·연결 방식)은 원본 주석과 공유 메모로 맞춘다. 맞추지 않으면 도구가 덜 막을 뿐 게임 동작은 그대로다.
- Core가 새 생성 규칙을 계약 생성자 밖(생성기 안 예외 등)에 두면 도구가 잡지 못한다.

### 10.6 다른 작업자 영향

게임 코드는 바뀌지 않는다. MG-c(2026-10-27)에서 공유 메모 넷에 적었다(모두 미전달): 전투 View 공유 §3·§4(A1 사본 둘), Core 전달 문서 §42.1·§42.2(A2가 부르는 Core 계약 셋과 사본 셋), 호진 공유 §1·§3·§4(맵 SO·Root 표 필드 이름, 보스 교체가 다른 스테이지를 뺌), 민형님 공유 §2·§4(새 파일 하나).

<a id="107-구현-메모-2026-10-27-mg-amg-b-unity-확인-문제-없음--사용자"></a>
### 10.7 구현 메모

설계와 모양이 다른 것만 적는다. 동작 결정은 설계대로다.

- `Judge`는 변경 객체 대신 인자 셋 `(rootGuid, next, dropGuid)`을 받는다. next가 null이면 뺌, 아니면 넣음·바꿈이다.
- 보스 교체는 "깨뜨림"보다 먼저 본다. 기존 보스를 함께 뺀 판정이 깨지지 않을 때만 교체를 묻고, 깨지면 그냥 막는다.
- '검사' 버튼은 교체로 풀리는 경우를 문제로 넣지 않고 통과 알림에 "저장 때 기존 보스를 뺄지 묻습니다"를 적는다.
- 저장 때 맵 SO 없음은 창 없이 저장한 뒤 MG9 안내로 보인다.
- `CombatStageSoCreation.SetRegistered`의 빼기는 짝 View 없이 Root만 빼는 것도 받는다(계획 P-MG4).
- 맵 생성 줄과 판정은 매 화면 그릴 때 Core 계약 몇 개를 만든다. 고른 스테이지 하나라 비용은 작다.

## 11 아군 배치 구역 칸 (AZ1~AZ9)

- 상태: 구현 완료. 결정 2026-10-27 사용자(Q-AZ1~Q-AZ5 모두 A, P-AZ1~P-AZ10 그대로). AZ-a·AZ-b Unity 확인 문제 없음(사용자 2026-10-28), 커밋 `ac4a8101`. 2026-10-29 안내 줄 삭제는 `abbdea4f`(§11.9). 결과는 [계획 §7](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#7-아군-배치-구역-칸-계획-az-aaz-c)이다.
- 요청: 2026-10-27 사용자 "아군 배치 구역 변경에 맞춰 SO 모드 설계에 절 추가하고 정할 것은 권장안과 함께 질문으로 줘. 맞추기와 아군 칸 붓 둘 다 범위에 넣어." 이어서 "설계할 때 혹시 내가 빠뜨린 게 있는지 확인도 해줘"(§11.5).
- 배경: 천명님 `5a1bfbe1f`(아군 배치 구역 칸 단위 지정, 적 배치 제한 삭제, [Core 설계](2026-10-27-combat-player-deployment-zone-design.md) PZ1~PZ7). 그 설계 PZ7이 SO 모드 도구가 할 일(적 줄 수 슬라이더 걷기, 맵 색 "아군 구역 / 나머지", 아군 칸 붓)을 적었다.
- 대신하는 것: §6 진영 줄 수의 적 쪽, §7 DZ1~DZ6, 계획 §2.2의 C2·E1.

<a id="111-확인한-사실-head-354bd8d7"></a>
### 11.1 게임 규칙 (HEAD `354bd8d7`)

- 아군 구역은 `CombatStageData.playerDeploymentCells`(`CellInput[]`, 헤더 Deployment)다. 비면 필드 `playerDeploymentDepth`줄(아래부터)을 쓴다. 대체 규칙 원본은 `BattleDefinitionAdapter.cs:90-92`(Systems internal) 한 곳이다.
- 적 구역은 없다. 고정 적 유닛·구조물·본부, 가중치 출현, 전투 시작 재배치는 모두 "아군 구역 밖이면 어디든"이다(`DeploymentZonePolicy.CanDeploy`).
- 고정 적 칸이 아군 구역 안이면 전투 진입 때 예외("초기 배치가 아군 배치 구역 규칙에 맞지 않습니다.", `BattlePreparationCalculator.cs:243-245`).
- 출현 후보 = 판 − 아군 구역 − 구조물·장애물 예약 칸 − 벽·낭떠러지 칸. 출현 수가 많으면 예외("적 출현 수를 배치할 빈 칸이 부족합니다.", `EnemySpawnCalculator.cs:57-70`).
- 고정 적 배치와 적 로드아웃을 함께 쓰면 진입 예외("직접 저작한 적 배치와 EnemyLoadout은 동시에 사용할 수 없습니다.", `BattleDefinitionAdapter.cs:48-51`).
- 저작 검사: 칸 목록이 있으면 부트 DB 생성(`GetData()`)이 중복·판 밖을 거절한다. 빈 목록과 "1칸 이상"은 보지 않는다(빈 목록 = 줄 수 대체). 줄 수 대체는 판 높이 이하(`PlayerDeploymentZone.FitsBoard`), 필드 SO는 두 줄 수 모두 1 이상만 본다. "두 줄 수 합 < 세로"는 없어졌다.
- 적 줄 수 `enemyDeploymentDepth`는 필드 SO에 남고 1 이상 검사도 남지만 Core는 읽지 않는다.
- 장애물·지형·오브젝트·레일·소환·전투 국면은 구역을 보지 않는다.
- `PlayerDeploymentZone`(Contracts)·`DeploymentZonePolicy`(Domain)는 public이고 도구 asmdef가 이미 참조한다.
- 당시 도구는 옛 줄 규칙(적 구역 색·적 줄 수 슬라이더·합 규칙 사본·적 진영 밖 경고)으로 그렸고, 칸 목록을 몰라 새로 만들기가 틀의 칸을 말없이 이어받았다. AZ-a·AZ-b가 모두 고쳤다.

### 11.2 설계 (AZ1~AZ9)

지금 코드 기준이다. 설계와 이름이 다른 곳은 "바뀐 것"으로 적는다.

| ID | 내용 |
|---|---|
| AZ1 | **작업본 칸 목록.** 작업본 `playerDeploymentCells`. `CombatStageSoAssets`가 필드 이름 상수 하나로 읽고 쓴다(레일 칸과 같은 `CellInput` 모양, 쓸 때 y → x 정렬). 초안·Undo는 작업본 JSON에 같이 담긴다 |
| AZ2 | **구역 해석은 한 곳.** `CombatStageSoAssets.ZoneOf(cells, field)`가 도구 값 `CombatStageSoZone`(`IsListed`·`ListedCount`·`RowDepth`·`Contains`·`EnemyAllowed`)을 돌려준다. 칸 목록이 있으면 Core `PlayerDeploymentZone.Of`, 비면 `Rows(필드 아군 줄 수)`(어댑터 대체 규칙의 사본, 원본 주석). 적 판정은 `DeploymentZonePolicy.CanDeploy`를 그대로 부른다. 색·이름·수·경고·구역 붓이 모두 이 값 하나를 쓴다. 바뀐 것: 설계 이름 `PlayerZoneOf(document, field)` → `ZoneOf`(창 파일에 Core 형식 이름이 들어가지 않게) |
| AZ3 | **맵 표시.** 바탕색은 아군 구역과 나머지 둘. 범례 "아군 구역(칸 지정)" 또는 "아군 구역(필드 N줄)", "적 배치 가능". 고른 칸 이름 "아군 배치 구역"/"적 배치 가능". 3D 덧그리기는 아군 구역 칸만 옅게 칠한다(P-AZ2) |
| AZ4 | **'아군 구역' 도구.** 도구 열거 끝에 `PlayerZone`(P-AZT4), 도구 줄 아래 `칠하기 \| 지우기`. 누르기·끌기는 칸마다, Shift+끌기는 사각형. 판 밖만 막는다(P-AZ4). 기존 '지우기' 도구는 구역을 건드리지 않는다(P-AZ3). 연산은 작업본 `PaintZone(cells, erase, field, grid)`: 같은 `ZoneOf`로 지금 구역을 해석해, 줄 수 구역에서 처음 편집하면 보이던 칸을 칸 목록으로 옮긴 뒤 편집한다(Q-AZ1 A). 마지막 칸 지우기는 막는다(Q-AZ2 A). 효과 없는 편집은 Undo를 남기지 않는다(P-AZT7). 바뀐 것: 설계·계획의 `fallbackDepth` 인자 → `field` |
| AZ5 | **'스테이지 값 → 아군 배치 구역' 묶음**(`DrawSoPlayerZone`). 출처 줄("칸 n개 (직접 지정)" / "필드 아군 줄 수 N줄 (칸 목록 비어 있음)"). 칸 목록이 비면 아군 줄 수 슬라이더(1~세로, P-AZ7, 공유 필드면 전용 복제), 있으면 읽기 줄과 '칸 목록 비우기(필드 줄 수로)'(확인 창, Q-AZ2 A). 적 줄 수 슬라이더는 없다(P-AZ8) |
| AZ6 | **참고 수와 경고**(계획 C2 흡수). 수 줄 "아군 구역 n칸(놓을 수 있는 칸 약 a) · 적이 놓일 수 있는 칸 약 m · 적 로드아웃 출현 k". a = 구역 칸 − 벽·낭떠러지 − 장애물·구조물·본부 칸, m = 판 − 구역 − 벽·낭떠러지 − 막힌 칸(Core 출현 후보와 같은 빼기). 경고 ① 고정 적이 아군 구역 안 ② k > m ③ a = 0 ④ 고정 적 + 적 로드아웃. 모두 경고만(Q-AZ3 A) |
| AZ7 | **맵 밖.** 저장·검사·등록의 맵 밖 검사(`SoProblems`)와 맵 아래 '맵 밖 배치' 경고에 "맵 밖 아군 구역 칸 (x, y)". 최종 판정은 `GetData()` |
| AZ8 | **만들기·상자 편집.** 새로 만들기는 칸 목록을 비우고(`ClearPlacements`) 복제는 복사한다(P-AZ5). 상자 편집은 구역 칸을 옮기지 않는다(P-AZ6) |
| AZ9 | **문서·공유.** 사용법 §3·§4·§5.1·§5.3·§5.4·§5.8·§6·§7.4·§8.2·§8.5·§9, 공유 메모 넷(§11.7) |

<a id="113-정할-것-q-az1q-az5"></a>
### 11.3 결정 (Q-AZ1~Q-AZ5)

모두 A(2026-10-27 사용자).

| ID | 질문 | 결정(A) | 버린 안 |
|---|---|---|---|
| Q-AZ1 | 칸 목록이 빈 상태에서 처음 칠하거나 지우면? | 보이는 줄 수 칸을 칸 목록으로 옮긴 뒤 편집(옛 Q-DZ5 A) | B 빈 목록에서 시작(첫 칸에 보이던 구역이 통째로 바뀜) |
| Q-AZ2 | 마지막 칸을 지우면?(빈 목록 = 줄 수 대체) | 막고 알린다. 줄 수로 돌아가기는 '칸 목록 비우기'(확인 창)로만 | B 지우고 "필드 줄 수로 돌아갔습니다" |
| Q-AZ3 | 경고의 무게 | 모두 경고만(진영 경고·계획 P1과 같음, 중간 상태 저장 가능) | B 두 경고는 저장·등록 막음 / C 등록만 막음 |
| Q-AZ4 | "고정 적 + 적 로드아웃"(진입 예외)도 경고 묶음에? | 넣는다 | B 병합 대응 C1과 함께 |
| Q-AZ5 | 게임 화면이 칸 구역을 그리지 않는 것 | 전투 View 담당(Core 설계 PZ7). 도구는 안내 줄 → 2026-10-29 PV-a 병합으로 풀려 안내 줄을 지움 | B 이 작업에 함께 넣음 |

<a id="114-설계에서-정한-것-확인-대상"></a>
### 11.4 설계에서 정한 것 (P-AZ1~P-AZ10)

모두 그대로(2026-10-27 사용자).

| ID | 정한 것 |
|---|---|
| P-AZ1 | 칸 목록은 y → x 순서로 정렬해 저장한다. Core `Of`의 정렬과 같아 에셋 diff가 안정된다 |
| P-AZ2 | 줄 수 대체와 칸 목록은 같은 색으로 그리고, 출처는 글자로만 보인다 |
| P-AZ3 | 기존 '지우기' 도구는 "맨 위 배치"를 지우는 도구 그대로 두고 구역을 건드리지 않는다. 구역은 '아군 구역' 도구 안의 지우기로만 뺀다 |
| P-AZ4 | 구역 칠하기에는 판 밖 말고 바로 막기가 없다. 배치가 있는 칸도 칠한다. 문제는 AZ6 경고가 알린다 |
| P-AZ5 | 새로 만들기는 칸 목록을 비우고, 복제는 복사한다 |
| P-AZ6 | 상자 편집은 구역 칸을 옮기지 않는다. 구역은 배치가 아니라 판의 속성이다 |
| P-AZ7 | 아군 줄 수 슬라이더 범위는 1~세로다(Core `FitsBoard` + 필드 1 이상). 세로 전체면 적 칸 0이고 AZ6 경고가 알린다 |
| P-AZ8 | 적 줄 수는 도구에서 보이지 않고 값은 그대로 둔다. 새 필드 만들기는 틀 필드의 적 줄 수(없으면 1)를 쓴다. 공유 필드 편집 위에 "적 배치 깊이(Enemy Deployment Depth)는 Core가 읽지 않습니다" 한 줄 |
| P-AZ9 | 참고 수의 벽·낭떠러지는 지형 정의 종류로 고른다. 공개 대상·숨김 호환 벽·무작위 시작 지형은 보지 않는다. 그래서 "약"이다 |
| P-AZ10 | Core 공개 API `PlayerDeploymentZone.Of`·`Rows`·`Contains`, `DeploymentZonePolicy.CanDeploy`를 부른다. 도구 사본은 대체 규칙 한 줄이다. 옛 사본 `MaxDepthBeside`는 지웠다 |

<a id="115-빠뜨린-것-확인-2026-10-27-사용자-요청"></a>
### 11.5 빠뜨린 것 확인 — 남은 위험

2026-10-27 사용자 요청으로 15개를 점검했다. 아홉(빈 목록 대체, 새로 만들기 이어받기, 판 밖 칸, 상자 편집, 표시 셋, 새 필드 적 깊이, 콘솔 문구, 게임 화면 미표시 — 10-29 PV-a로 풀림, Fixture 적 줄 수 — 전투 View PV-b2가 Fixture 모드를 지움)은 끝났다. 남은 위험:

| 위험 | 지금 | 누가 |
|---|---|---|
| 출현 수 > 적이 놓일 칸, 아군 놓을 칸 0, 고정 적 + 적 로드아웃 | 도구 경고만(Q-AZ3 A). 등록된 스테이지도 Run 중 진입 예외를 막지 못한다 | — |
| 공유 필드 편집 칸에 적 배치 깊이가 그대로 보임 | 도구 안내 줄만. 필드 SO 툴팁·정리는 DB 담당과 정할 일(Core 설계 Q-PZ2) | 호진 공유 §4 |
| Core PZ-d의 메인 캐릭터 자리 필드 삭제 | 도구는 `mainCharacterAnchor`를 `Require`하고, 회전 열거 이름을 `mainCharacterRotation`에서 읽는다(`FillNames`). 필드가 먼저 지워지면 스테이지 열기가 예외로 멈춘다 | Core 전달 문서 §42.4 |
| DT 모드·엑셀 도구의 줄 수 표시 | 이 설계 밖 | 민형님(Core 설계 PZ7) |

<a id="116-원칙-점검"></a>
### 11.6 남은 약점

- 참고 수는 근사다(P-AZ9). 게임 규칙이 바뀌면 사본 한 줄과 참고 수 식을 맞춰야 하고, 맞추지 않아도 도구 표시만 틀린다.
- 이 변경 전에 저장된 SO 모드 초안(EditorPrefs)을 복원하면 칸 목록이 빈 채로 들어온다. 그대로 저장하면 인스펙터로 넣은 칸 목록이 지워진다(지금은 모든 칸 목록이 비어 영향 없음).
- 인스펙터로 넣은 중복 칸은 도구가 고치지 않고 저장 검사가 막는다(P-AZT1).

### 11.7 다른 작업자 영향

게임 코드는 바뀌지 않는다. AZ-c(2026-10-28)에서 적었다(모두 미전달):

- Core 전달 문서 §42(천명님): 머리줄, §42.1 Core API, §42.2 사본 표(옛 `MaxDepthBeside`·`EnemyStartRow` 걷음), §42.3 해소 표시, 새 §42.4(이름으로 읽는 칸·도구가 하는 일·경고 넷·PZ-d 주의·게임 화면). §43 Core 응답 끝 "도구 쪽 반영" 한 줄.
- 전투 View 공유(용근님): §2 두 줄, 새 §7(병합 전 §6, 게임 화면 칸 구역 미표시 → 10-29 "병합 뒤 바뀜" 머리 줄, `abbdea4f` 반영).
- 호진 공유: §2 아군 줄 수, §3 끝 InGame SO 세 필드(`playerDeploymentCells`·`spawnCount`·지형 `kind` 이름), §4 끝 필드 적 깊이 툴팁.
- 민형님 공유: 머리 상태, §2 작업본 줄, §4 끝 "바뀜"(새 파일 없음, DT·엑셀 줄 수 표시는 민형님 몫).

<a id="118-phase안"></a>
### 11.8 Phase

AZ-a 맞추기(AZ1~AZ3·AZ5·AZ7, 새 필드) → AZ-b 붓·경고(AZ4~AZ6·AZ8) → AZ-c 문서·공유(AZ9). 결과는 계획 §7이다.

<a id="119-구현-메모-2026-10-28-az-aaz-b-unity-확인-문제-없음--사용자"></a>
### 11.9 구현 메모

AZ1·AZ2·AZ4의 이름 차이는 §11.2에 적었다. 그 밖:

- **AZ5 칸 목록 비우기:** 작업본 연산을 따로 두지 않고 창에서 칸 목록 `Clear()` 한 줄을 `SoChange`로 부른다.
- **AZ6:** 벽·낭떠러지 판정 `CombatStageSoAssets.BlocksEntry`(지형 정의 `kind` 이름 `Wall`·`Cliff`, P-AZT5)는 카탈로그가 정의별로 캐시한다. 출현 수는 `SpawnCountOf`이고, 이 스테이지만 쓰는 로드아웃을 고치는 중이면 편집 복제본에서 읽는다. 경고 ①~④는 한 상자에 줄마다 묶었다. ④ 조건은 Core 어댑터와 같이 "로드아웃이 있고 고정 적 유닛이 하나 이상"이다(구조물은 보지 않음).
- **AZ7·AZ8:** 설계대로다. 새로 만들기의 칸 비우기는 AZ-a로 앞당겼다(계획 P-AZT2).
- **2026-10-29 안내 줄 삭제(`abbdea4f`, 당시 컴파일 미확인):** `a8612321e` 병합으로 전투 View PV-a가 들어와 게임 화면이 칸 구역을 그린다. 그래서 `DrawSoPlayerZone`의 안내 상자 "게임 화면은 아직 칸 구역을 그리지 않습니다…"와 주석을 지우고, 사용법 §5.8 "게임 화면" 문단과 문제 표 줄, 전투 View 공유 §7(병합 전 §6) 머리를 고쳤다.

## 12 병합 최신화: 배경 방식·바닥 타일 (UT1~UT10)

- 상태: 결정 완료(2026-10-29 사용자 "모두 권장안대로 하는데, 3D/2D에서도 타일이 바뀐 것을 알고 싶어요" → Q-UT1~Q-UT5 A, P-UT1~P-UT10 그대로, 2D·3D 타일 표시 UT10 추가). Task 계획·실행 기록은 [계획 §8](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#8-병합-최신화-계획-ut-aut-d). UT-a~UT-d 완료, Ua·Ub·Uc 확인 문제 없음(사용자), 구현·문서 `ede23986a`, 공유 문서 후속 `f5c8109d4`. 구현 메모는 §12.8.
- 문서 보완(2026-10-29): 사용자 "이 5가지 보완 내용을 설계·계획 문서에 직접 반영해줘. 코드는 수정하지 말고."에 따라 GridOutline 전제·빈 구멍·타일 복원·교체 외형 좌표계·명시적 새로고침을 P-UT13~P-UT17로 반영했다. 이번 요청은 두 문서 수정이며 코드 착수·Unity 확인 완료를 뜻하지 않는다.
- 요청: 2026-10-29 사용자 "전투 스테이지 편집 툴을 최신화 해야 할 것 같은데 뭐 있는지 알려줘" → 목록 보고 → "지금 머지한 내용 확인해 주세요. 전투 쪽 바닥 타일, ViewProp에 있는 Map Tiles도 있거든요(SO로). 그거는 무조건 넣어 달래요."
- 대조 범위: 도구 마지막 최신화 `ac4a8101` ~ HEAD `954527d9`(용근님 "전투 View 바닥 타일 세팅"). 게임 쪽 설계·기록은 [타일 맞춤 설계](2026-10-28-combat-map-tile-alignment-design.md)·[타일 높이 설계](2026-10-28-combat-map-tile-height-design.md)·[타일 교체 설계](2026-10-29-combat-terrain-tile-replace-design.md), 도구 사본과 어긋난 점은 [전투 View 사본 공유](../handoffs/2026-10-27-combat-stage-editor-so-mode-view-share.md) §5·§6이 이미 정리했다.
- 2026-10-29에 고친 것(코드 변경 없음): 사용법 §7.4·§8.5 "배경 맵 카메라"의 "맵 SO가 있으면 맵 SO의 프리팹" 문장은 이미 병합된 배경 방식 규칙과 달랐다(작성 때 잘못 읽음). 방식 기준으로 고쳤다.

### 12.1 확인한 사실 (HEAD `954527d9`)

**배경 방식**

- View `stageViewPropData.mode`(`StageViewPropMode`): `Prefab = 0`(`gameObject`)·`MapDefinition = 1`(`map`의 프리팹 + 베이크)·`PrefabWithTiles = 2`(`gameObject` + 바닥 타일 `mapTiles`). 게임은 방식만 보고 고르고, 방식이 쓰지 않는 칸은 값이 있어도 읽지 않는다(`StageViewPropData.GameObject`·`Map`).
- 검사 원본은 `StageViewPropData.Validate()` 하나다. Prefab·타일 방식은 `gameObject` 필수, MapDefinition은 `map` 필수 + 맵 검사, 타일 방식은 타일 배치 필수. 부트 `ViewStageData`가 부르고, **도구 저장 검사도 복제본 `ViewStageDataSO.GetData()`로 이미 이 검사를 거친다.**

**바닥 타일 값** (`mapTiles` = `CombatMapTilesInput`, 모두 비면 타일 없음)

| 칸 | 뜻 |
|---|---|
| `tileSet` | 타일 세트 `CombatMapTileSetSO`(표 없는 SO 직접 참조, 여러 스테이지가 함께 씀). 종류마다 `prefab`·`surfaceHeight`(필드 단위, 음수 = 바닥보다 들어간 타일), 같은 종류 두 번·빈 프리팹 거절 |
| `columns`·`rows` | 배경 구멍의 최대 크기(필드 크기와 따로) |
| `tileSize` | 타일 한 칸 크기(MapTileRoot 기준, 배경 배율이 곱해짐). 타일 한 장 = `tileSize × 배율`이 칸 간격의 정수배여야 함 |
| `offset` | MapTileRoot의 배경 로컬 위치 = 구멍 가운데, y = 타일 바닥 높이 |
| `anchorTile` | 게임 (0,0) 칸이 놓일 타일 칸. **화면(필드) 축 기준**, 좌하단 타일 = (0,0), 배경 회전과 무관 |
| `tiles` | 칸마다 종류(`CombatMapTileKind`, 코드 열거·0 없음). **MapTileRoot −Z 줄부터, 줄 안은 −X부터**(배경 회전 전 방향), 길이 = 가로 × 세로 |

**게임이 세우는 순서** (`StageViewPropPresenter.ShowTiles`, Combat 씬 로드)

선행 조건은 필드 표시 방식 `GridOutline`이다(`FieldCellSpacing.FromGridOutline`). `CellPrefab`에서는 칸 간격을 설정만으로 구할 수 없어 진입 예외가 난다.

1. MapTileRoot를 배경 자식으로 `offset`에(회전 0·배율 1) 만들고 칸마다 프리팹을 놓는다(`StageMapTiles.Build`).
2. 기울기 검사 → 타일 가로축이 가장 가까운 90°에 오도록 격자 가운데를 축으로 배경을 돌린다(`YawCorrection`). 결과적으로 배경 Y 회전은 90° 단위로 반올림된다.
3. 기준 타일 좌하단이 게임 (0,0) 칸 좌하단에 오게 배경을 수평 이동한다(`AnchorShift`). 위치 보정의 가로(x·z)는 여기서 덮이고 높이(y)만 남는다.
4. 필드 칸마다 그 칸 가운데가 놓인 타일의 윗면 높이·타일 오브젝트 표를 만든다(`CombatMapTileSurface`).

- **전투 진입 때만 나는 예외(F1):** 필드 표시 방식이 GridOutline이 아님, 회전 보정 X·Z(기울기), 타일 축이 필드 축과 평행하지 않음, 타일 한 장이 칸 간격 정수배가 아님, 기준 타일이 타일 맵 밖(회전에 따라 가로·세로가 바뀜). **경고만:** 필드가 타일 맵 밖으로 넘침, 타일 한 장에 필드 칸이 둘 이상.
- **칸 높이:** 바탕 = 타일 윗면(타일 밖 칸은 0). 지형 외형 방식 `OnTile`은 바탕 위로 올리고(0 이상), `ReplaceTile`(약한 지반·낭떠러지)은 바탕 자체를 그 값으로 바꾼다(음수 가능, 프리팹이 없어도). 바닥 교체 칸은 그 타일을 끄고 외형을 타일 자리에 둔다. 격자 덧그리기·호버·포인터는 지형 높이만 본다.
- **빈 구멍·복원:** 현재 `TerrainVisual_Cliff`는 ReplaceTile·프리팹 없음이다. 게임은 프리팹 유무와 별개로 타일을 숨긴다(`FieldTerrainPresenter.MarkShown`). `FieldTileReplacement.Apply`는 이번에 숨길 타일과 이전에 자신이 숨긴 타일을 비교해 복원하며, 여러 칸이 한 타일을 공유하면 교체 지형이 남은 동안 그 타일을 숨긴다.
- **교체 외형 좌표계:** `FieldTerrainPresenter.PlaceOnTile`은 타일과 같은 부모 아래 놓고 위치·크기는 타일 루트 기준, 회전은 필드 기준으로 계산한다. 타일이 없으면 `LocalPosition`·`PlaceInField`가 칸 중심 높이 0과 외형 오프셋을 쓴다. 상세 사본 계약은 P-UT16이다.

**지금 에셋**

| View | 방식 | 배경 |
|---|---|---|
| Test(10000, Root·View 표 등록 = Run 후보) | PrefabWithTiles | `Test/StageTest`, 회전 Y −90, 배율 0.5, 17 × 17·`tileSize` 2(→ 타일 한 장 1 = 칸 간격 1)·`offset` (−1, −2, 0.5)·`anchorTile` (4, 4), 모두 `Test3`, 세트 `CombatMapTileSet_Temp`(Test1~Test4, 높이 0·−0.1·−0.1·−0.1) |
| Normal A~C | Prefab | `Test/StageTest`(타일 없이) |
| Boss | MapDefinition | 맵 SO `Map_Map`(가상 카메라 없음 → 진입 때 멈춤, V3-g 결정 "임시 세팅이라 그대로") |

**도구 지금**

- `mode`·`mapTiles`를 읽지도 쓰지도 않는다. 저장해도 두 칸은 그대로 남고(지워지지 않음), 새로 만들기·복제(`CopyAsset`)는 이어받는다.
- 패널 문구 "맵 SO가 있으면 맵 SO의 프리팹을 씁니다"(`CombatLayoutWindow.SoMode.cs` `DrawSoView`)와 3D 맵 고르기(`CombatStageSoPreview.RebuildMap`, 맵 SO 우선)는 방식을 보지 않는다. 지금 에셋에서는 결과가 같지만, 방식 0인 스테이지에 맵 SO를 넣으면 도구 3D만 다르게 보인다.
- 3D 칸 높이·놓는 자리는 타일·지형 방식 이전 식이다(사본 공유 §5.2·§6.2 표: 타일 높이 없음, 바닥 교체 음수 무시, 오브젝트 높이 0, 유닛 높이 시작 0).
- `ReadFieldView`는 CellPrefab을 1×1로 근사하고, `RebuildPlacements`는 프리팹 없는 외형을 색 상자로 그린다. 타일 지원에서는 각각 P-UT13·P-UT14의 예외 처리가 필요하다. 목록 새로고침은 외형 표를 다시 읽지만 3D의 맵·배치 재생성 키는 비우지 않으므로, 같은 GUID의 외부 에셋 변경을 P-UT17로 반영한다.
- 초안(EditorPrefs JSON)에 형식 번호가 없다. 작업본에 타일 칸을 더한 뒤 옛 초안을 복원하면 타일이 빈 값으로 들어와, 그대로 저장하면 Test의 타일이 지워진다.

### 12.2 설계 (UT1~UT10)

| ID | 내용 |
|---|---|
| UT1 | **작업본 값.** `CombatStageSoView`에 `mode`(int)와 `tiles`(새 `CombatStageSoTiles`: `tileSetGuid`·`columns`·`rows`·`tileSize`·`offset`·`anchorTile`·`kinds` int 배열, 배열은 에셋과 같은 MapTileRoot 순서). `CombatStageSoAssets`가 필드 이름 상수(`mode`·`mapTiles`·`tileSet`·`columns`·`rows`·`tileSize`·`offset`·`anchorTile`·`tiles`, 세트의 `tiles`·`kind`·`prefab`·`surfaceHeight`)로 읽고 쓴다. 열거는 값으로 담고 이름은 `enumNames`로 읽는다(형식이 internal이라 SerializedObject). 작업본 JSON에 같이 담겨 Undo·초안이 따라온다 |
| UT2 | **'View → 배경' 패널.** 방식 고르기(프리팹 / 맵 SO / 프리팹 + 바닥 타일)와 방식이 쓰는 칸만 보인다. 쓰지 않는 칸 값은 그대로 둔다(게임과 같음, P-UT4). 위치·회전 보정 줄에 방식별 안내: 타일 방식이면 "가로(x·z)는 기준 타일이 정하고 높이(y)만 씀, Y 회전은 90° 단위로 맞춰지고 X·Z 회전은 진입 때 멈춤". 문구 "맵 SO가 있으면…"은 지운다 |
| UT3 | **'바닥 타일' 묶음**(타일 방식일 때). 타일 세트(에셋 목록 + Inspector 열기), 가로·세로 칸 수, 타일 크기, MapTileRoot 위치, 기준 타일. 비어 있으면 '타일 만들기'(기본값 P-UT3). 가로·세로를 바꾸면 같은 (가로, 세로) 칸의 종류는 지키고 새 칸은 세트 첫 종류(P-UT2). 세트 안 종류 목록(이름·프리팹·높이)을 읽기 줄로 보인다. Inspector 수정은 '목록 새로고침'으로 반영한다(P-UT17) |
| UT4 | **맵 '타일' 보기**(Q-UT1). 2D·3D 옆 셋째 보기. 타일 맵 전체를 **화면(필드) 방향**으로 그린다: 칸 색 = 종류(범례 = 종류 이름·프리팹·높이), 필드가 놓일 자리 사각형(기준 타일부터, 타일 맵 밖으로 넘치면 빨강), 기준 타일 표시. 도구 `칠하기 \| 기준 타일`: 칠하기는 고른 종류를 누름·끌기 칸마다, Shift+끌기 사각형, '모두 이 종류로'. 기준 타일은 누른 타일로 옮긴다. 화면 칸 ↔ 배열 칸 대응은 회전 보정 Y를 90° 단위로 반올림한 값으로 정한다(게임 `YawCorrection` + `AnchorShift` 축 대응의 사본, 원본 주석). 끌기 한 번 = Undo 한 번, 효과 없는 편집은 Undo 없음(AZ와 같음) |
| UT5 | **3D 배경.** 방식대로 고른다(Prefab·타일 → 맵 프리팹, MapDefinition → 맵 SO 프리팹). 타일 방식이고 모드·맞춤 조건이 성립하면 게임 순서 1~3(MapTileRoot·타일·회전·이동)을 사본으로 따른다. 맵 키가 바뀌거나 명시적으로 새로고침했을 때 다시 만든다(Test 289개, P-UT9·P-UT17) |
| UT6 | **3D 높이·자리**(Q-UT3). 사본 공유 §5.2·§6.2 표대로: 바탕 = 칸 가운데가 놓인 타일 윗면(`CombatMapTileSurface` 사본) → 지형 `OnTile` 올림·`ReplaceTile` 교체(프리팹 없어도, 음수 그대로) → 타일 위 지형 외형은 칸 바탕, 오브젝트는 칸 표면, 유닛은 첫 점유 칸 높이부터 최댓값, 덧그리기는 지형 높이만. 바닥 교체의 빈 구멍·타일 숨김과 복원·외형 좌표계는 P-UT14~P-UT16을 따른다 |
| UT7 | **모드·맞춤 문제 알리기**(Q-UT2). 같은 판단 결과로 GridOutline 전제(P-UT13)와 기존 맞춤 예외 넷(기울기·축·정수배·기준 타일 밖), 경고 둘(필드 넘침·한 타일에 칸 여럿), 세트에 없는 종류 칸 수를 '바닥 타일' 묶음·맵 아래·저장 알림에 보인다. 모드·맞춤 문제는 경고만 하며, 타일 배치 자체의 오류(길이·세트 없음·없는 종류)는 지금처럼 저장 검사(`GetData()`)가 막는다 |
| UT8 | **초안 형식 번호.** 작업본에 형식 번호를 두고, 번호가 다른 초안은 복원하지 않고 알린다(P-UT6) |
| UT9 | **문서·공유.** 사용법(§3 화면·새 '바닥 타일' 절·§7.4·§8.5 배경 방식·§9 모드·맞춤 진입 문제), 전투 View 사본 공유 §1·§3·§5·§6에 "도구가 맞춤"과 새 사본 줄, 호진 공유 §4 Test 다시 등록, 민형님 공유 §2 새 파일 |
| UT10 | **2D·3D에서도 타일 보이기**(2026-10-29 사용자 추가 요청). 타일 방식 스테이지에서 '타일' 보기로 고친 것이 다른 보기에도 바로 보인다. **2D:** 필드 칸마다 그 칸 가운데가 놓인 타일(게임 `CombatMapTileSurface`와 같은 대응)의 종류 색을 칸 아래쪽 띠로 그린다(배치 색·글자는 그대로). 타일 맵 밖 칸은 어두운 띠("타일 없음, 배경 위"). 범례에 판 안에 있는 종류만 더한다. 고른 칸 줄에 "바닥 타일 Test3(높이 −0.1)"을 보인다. 바닥 교체 지형(약한 지반·낭떠러지) 칸은 띠에 "교체" 표시를 겹친다. **3D:** UT5대로 타일을 그리고, 타일 값 변경·명시적 새로고침을 반영한다(P-UT9·P-UT17). 모드 전제 또는 맞춤 계산이 성립하지 않으면(UT7) 필드와 타일의 대응 표시(필드 사각형·2D 띠·고른 칸의 타일 정보)를 생략하고, 3D는 타일 없이 배경만 놓은 뒤 같은 문제를 맵 아래에 띄운다 |

**파일(P-UT8):** 순수 계산(작업본 타일 값·화면 방향 대응·맞춤·높이 표 사본)은 새 `CombatStageSoTiles.cs`, 타일 보기·'바닥 타일' 묶음 화면은 새 partial `CombatLayoutWindow.SoTiles.cs`. 창 파일 `SoMode.cs`(2192줄)가 더 커지지 않게 한다. 바뀌는 기존 파일은 작업본·에셋·3D(`CombatStageSoPreview`·`CombatStageSoVisuals`)·창.

### 12.3 결정 (Q-UT1~Q-UT5)

모두 A(2026-10-29 사용자). 추가 요청 "3D/2D에서도 타일이 바뀐 것을 알고 싶다"는 UT10으로 넣었다.

| ID | 질문 | 결정(A) | 버린 안 |
|---|---|---|---|
| Q-UT1 | 타일을 어디서 칠하나? | 맵 영역의 셋째 보기 '타일'(타일 맵 전체, 화면 방향, UT4) | B 필드 칸 위에서만 칠함(필드 밖 타일은 못 고침, 지금 Test는 17 × 17 중 9 × 9만) / C 칠하기 없이 숫자 칸만(Inspector와 같음) |
| Q-UT2 | 맞춤 문제의 무게(기존 진입 예외 넷, P-UT13 모드 조건도 같은 정책) | 경고만(아군 구역 경고 Q-AZ3 A와 같게, 중간 상태 저장 가능) | B 저장은 되고 표 등록·등록된 스테이지 저장은 막음 / C 저장도 막음 |
| Q-UT3 | 3D 높이·자리 사본(지형 방식·오브젝트·유닛·바닥 교체)도 이번에? | 함께(타일 높이를 넣으면 같은 식을 고쳐야 함, UT6) | B 타일 배치·배경만, 높이는 다음 |
| Q-UT4 | 남은 병합 목록 B1(고지대 이웃 모양 3D)·B2(토네이도 반 칸)·C1(벽 위 배치 알리기)도 이번에? | 다음 묶음으로(이번은 타일에 집중, 계획 §2.2에 남김) | B 함께(B1 이웃 기준은 게임과 같은 "Player 공개 칸") |
| Q-UT5 | 타일 세트(종류별 프리팹·높이) 편집 | 고르기만, 세트 내용은 Inspector(맵 SO 가중치와 같음, P-UT5) | B 도구에서 세트 줄도 고침 |

### 12.4 설계에서 정한 것 (P-UT1~P-UT17)

| ID | 정한 것 |
|---|---|
| P-UT1 | 타일 보기의 좌표는 `anchorTile`과 같은 화면 좌표(좌하단 = 필드 −X·−Z)다. 배열 순서로 바꾸는 것은 도구가 한다 |
| P-UT2 | 가로·세로를 바꾸면 같은 (가로, 세로) 배열 칸의 종류를 지키고 새 칸은 세트 첫 종류다. 줄이면 밖의 칸은 버린다(Undo로 되돌림) |
| P-UT3 | '타일 만들기' 기본값: 세트 = 찾은 첫 세트(이름순), 가로·세로 = 필드 가로·세로(회전 고려), 타일 크기 = 칸 간격 ÷ 배경 배율(타일 한 장 = 칸 하나), 위치 (0, 0, 0), 기준 타일 (0, 0), 모든 칸 첫 종류. 구멍과 맞추는 값은 배경 아트를 보고 기획자가 고친다 |
| P-UT4 | 방식을 바꿔도 쓰지 않게 된 칸(맵 SO·맵 프리팹·타일)은 지우지 않는다. 게임이 무시하고, 방식을 되돌리면 그대로 쓴다 |
| P-UT5 | 종류는 코드 열거(`CombatMapTileKind`, 용근님 임시 값)라 새 종류는 코드 변경이다. 도구는 열거 이름 + 프리팹 이름으로 보이고, 색은 종류 값으로 고정한다 |
| P-UT6 | 초안 형식 번호를 두고, 다른 번호의 초안은 복원하지 않고 "옛 초안은 복원하지 않았습니다"를 알린다. 이번 형식 번호 도입 전 초안도 복원하지 않는다 |
| P-UT7 | 화면 방향 대응·맞춤·높이 표는 게임 식의 사본이고 원본 자리를 주석으로 적는다(Q-AD1 A 유지). 게임 코드는 바꾸지 않는다 |
| P-UT8 | 새 파일 둘(§12.2 끝). `.meta`는 Unity가 만든다 |
| P-UT9 | 3D 타일 인스턴스는 배경·타일 값 등 맵 키가 바뀌거나 '목록 새로고침'을 눌렀을 때 다시 만든다(P-UT17). 칠하는 동안에는 바뀐 칸만 바꾸지 않고 통째로 다시 만든다(289개, 끌기 한 번에 한 번) |
| P-UT10 | 새로 만들기·복제는 지금처럼 방식·타일을 이어받는다(틀의 배경을 그대로 쓰는 것이 기본이라서) |
| P-UT11 | (UT10 추가 때 정함) 2D 타일 표시는 켜고 끄는 칸 없이 늘 그린다. 칸 아래쪽 띠라 배치 색·글자를 가리지 않는다. 칸 → 타일 대응은 화면 좌표로 "기준 타일 + 칸 번호 ÷ 타일 한 장의 칸 수(내림)"이고, 맞춤이 성립할 때 게임의 "칸 가운데가 놓인 타일"과 같다 |
| P-UT12 | (UT10 추가 때 정함) 2D 띠·고른 칸 줄·3D가 모두 같은 계산(`CombatStageSoTiles`) 하나를 쓴다. 저장 전 작업본 값으로 그린다 |
| P-UT13 | **GridOutline 전제.** PrefabWithTiles의 필드 대응·맞춤 판단은 실제 필드 표시 방식이 GridOutline일 때만 한다(원본 `FieldCellSpacing.FromGridOutline`). CellPrefab의 1×1 근삿값으로 성공을 판단하지 않는다. 지원하지 않는 모드는 원인을 알리고 UT10의 대응 표시·3D 타일을 생략하되, 이 조건만으로 저장을 막지 않는다(Q-UT2). 필드 표시 모드와 계산 결과는 모든 안내가 함께 쓴다 |
| P-UT14 | **프리팹 없는 바닥 교체.** 정상적인 ReplaceTile 외형 데이터의 프리팹이 없으면 해당 바닥 타일을 숨기고 높이를 교체하되 대체 색 상자는 그리지 않는다(현재 낭떠러지). 외형 데이터 자체의 누락·오류와 구분한다. 프리팹이 있으면 교체 외형을 그린다. 2D 지형 표식·선택 표시는 유지한다 |
| P-UT15 | **타일 숨김·복원.** 3D 미리보기 한 곳이 자신이 숨긴 타일을 소유한다. 작업본 배치가 바뀔 때 숨길 타일 집합을 다시 계산하고, 이전에 자신이 숨겼지만 이번 집합에 없는 타일만 복원한다. 원래 꺼져 있던 타일은 임의로 켜지 않는다. 한 타일을 여러 칸이 공유하면 교체 지형이 하나라도 남는 동안 숨긴다. 놓기·삭제·이동·정의 변경·Undo·Redo는 같은 갱신 경로를 쓰고, 배경 재생성·스테이지 교체·창 종료 때 기존 교체 외형과 숨김 기록·객체 참조를 정리한다(원본 `FieldTileReplacement.Apply`·`Restore`) |
| P-UT16 | **교체 외형 좌표계.** 대상 타일이 있으면 그 타일과 같은 부모(MapTileRoot) 아래 놓는다. 로컬 위치 = `tile.localPosition + 외형 LocalOffset`, 로컬 배율 = `프리팹 localScale × 외형 LocalScale`, 로컬 회전 = `Inverse(부모 월드 회전) × 필드 기준 외형 회전 × 프리팹 localRotation`이다. 위치·크기는 배경 변환을 따르고 방향은 필드 기준이다. 타일이 없는 칸은 필드 쪽 부모 아래 칸 중심 높이 0 + 외형 오프셋을 쓰며, 교체된 표면 높이를 다시 더하지 않는다. 계산은 도구 한 곳에 두고 원본 `FieldTerrainPresenter.PlaceOnTile`·`PlaceInField`·`LocalPosition`을 주석으로 남긴다 |
| P-UT17 | **명시적 새로고침.** Inspector에서 수정한 타일 세트 프리팹·높이는 '목록 새로고침'으로 반영한다(자동 변경 감지 없음). 외형·타일 세트·필드 표시 값을 다시 읽고 2D 대응·높이·3D 배경·배치의 캐시를 함께 무효화한다. GUID가 같아도 내부 변경을 반영한다. 저장하지 않은 작업본·Undo 기록·선택·3D 시점은 유지하며, 공유 에셋을 작업본 값으로 덮어쓰지 않는다 |

P-UT13~P-UT17은 2026-10-29 다섯 보완 문서 반영 요청에 따른다. 구현·Unity 확인은 [계획 §8.6 대응표](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#86-다섯-보완-문서-반영-대응)로 추적한다.

**원칙 점검(문서 기준):** SSOT는 에셋 읽기·필드 이름을 `CombatStageSoAssets`, 대응·높이 계산을 `CombatStageSoTiles`, 인스턴스·숨김 수명을 `CombatStageSoPreview` 한 곳에 둔다. SOLID의 S/O는 기능별 책임과 기존 호출 지점의 연결로, L/I/D는 기존 계약을 유지하고 필요한 값만 계산에 넘기는 것으로 점검한다. 새 서비스·인터페이스를 추가하지 않고 JMH/FW의 런타임 조립·등록 계약을 바꾸지 않는다. 대표 변경 범위는 타일 맞춤 = 계산과 관련 표시, 타일 복원 = 미리보기 수명, Inspector 갱신 = 읽기·캐시 무효화이며 DT·전투 런타임을 함께 수정할 이유가 없다. 게임 식 사본의 유지 부담(P-UT7)은 남고 원본 목록을 공유 문서에 기록한다.

### 12.5 남은 약점

- 방향 대응·맞춤·높이는 게임 식의 사본이다. 게임 식이 바뀌면 도구 표시·알림만 틀리고 게임 동작은 그대로다(사본 공유 문서에 줄을 남긴다).
- 배경 구멍과 타일 맵이 맞는지(`offset`)는 아트를 봐야 해서 도구가 판정하지 못한다. 3D로 눈으로 확인한다.
- 3D 조명은 게임과 다르다(맵 SO도 프리팹만 놓음, §5 AD1).
- 타일 세트 하나를 여러 스테이지가 함께 쓰므로 세트를 고치면 모두 바뀐다. 도구는 세트를 고치지 않는다(Q-UT5 A).

### 12.6 다른 작업자 영향

- 게임 코드·DB SO 코드는 바꾸지 않는다.
- 용근님(전투 View): 사본 공유 §5·§6의 어긋난 사본을 도구가 맞춘다는 표시와 새 사본 줄(화면 방향 대응·맞춤·타일 높이 표). 타일 종류 열거·세트 필드 이름이 바뀌면 도구 상수도 바꿔야 한다.
- 민형님: 도구 파일 목록에 새 파일 둘(공유 §2).
- 호진 공유 §4: Test가 다시 Root 표에 등록돼 Run 후보이고 타일 방식이다(10-27 정정 줄이 다시 틀림).

### 12.7 Phase(안)

UT-a 값·패널(UT1·UT2·UT3·UT8, 숫자 편집과 방식, 화면 변화는 패널만) → UT-b 타일 보기·붓·맞춤 알리기·2D 띠(UT4·UT7·UT10 2D) → UT-c 3D 배경·높이(UT5·UT6·UT10 3D) → UT-d 문서·공유(UT9). 당시 B1·B2·C1을 다음 묶음으로 남겼다(Q-UT4 A). 현재 B1은 보류, B2·C1은 후속 §15로 잇는다. Task는 계획 §8이다.

### 12.8 구현 메모 (2026-10-29, UT-a~UT-d)

자세한 파일·차이는 [계획 §8.1~§8.4 실행 기록](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#8-병합-최신화-계획-ut-aut-d)이다. 설계와 다르거나 더한 것만 적는다.

- **UT8 더함:** 초안뿐 아니라 창에 남은 작업본도 형식 번호를 본다(업데이트 전에 열어 둔 작업본이 타일 빈 값으로 돌아와 저장 때 Test 타일을 지우는 것을 막음). 번호가 다르면 저장 안 한 편집을 버리고 에셋에서 다시 읽는다(`ReopenSoOldFormat`).
- **UT1:** 타일 종류는 작업본에 열거 값으로 둔다(종류 열거에 0이 없어 순번과 값이 다름). 배경 방식은 순번으로 두고 뜻은 열거 이름으로 본다.
- **UT4·P-UT1·P-UTT10 바뀜:** 화면 ↔ 배열 방향은 "회전 Y 반올림" 대신 회전 보정 전체와 배율 부호를 적용한 타일 축이 어느 필드 축·방향에 가까운지로 정한다(게임 `AnchorShift`의 `columnsAlongX`와 같은 비교). X·Z가 0이면 같고, 뒤집힌 배율도 게임과 맞는다.
- **UT7:** 게임 `RequireAlong`(축 평행)은 기울기가 없으면 늘 성립해 기울기 검사 하나로 알린다. 알림은 맵 아래(2D·3D·타일)·바닥 타일 묶음·저장 뒤 한 줄이다.
- **UT10:** "교체" 표시는 6px 띠에 글자가 들어가지 않아 검은 줄무늬로 그렸다. 띠 범례는 2D에서만 보인다.
- **UT6·P-UT14~P-UT16:** 설계대로다. 칸 → 타일 대응·칸 바탕 높이·3D 타일 숨김이 같은 맞춤 결과(`TryTileOfCell`) 하나를 쓴다(P-UT12). 맵을 다시 만들면 숨김 기록을 비우고 배치를 반드시 다시 놓는다.
- **더함:** '타일' 보기에서는 배치 단축키(Delete·R·복사·붙여넣기)를 받지 않는다. Esc는 끄는 중인 타일 사각형만 취소한다.
- **남은 차이(게임과):** 공개 대상(knownTo)을 보지 않음(게임은 Player에게 보이는 지형만 높이·타일 숨김에 셈), 토네이도 반 칸·덮는 칸 최대 바탕(B2), 고지대 이웃 모양(B1). 당시 B1·B2·C1을 다음 묶음으로 남겼다(Q-UT4 A). 현재 B1은 보류, B2·C1은 후속 §15로 잇는다.

## 13 바닥 타일 칸별 회전 (TT1~TT9)

- 상태: **TT-a~TT-d 완료(2026-10-29, Ua·Ub·Uc 확인 문제 없음 — 사용자, 새 시험 없음, 커밋 `f5c8109d4`, 공유 미전달, 구현 메모 §13.6).** 결정(2026-10-29 사용자): Q-TT1 A("이 작업에서 고침", 파일은 §13.4), Q-TT2~Q-TT4 A, Q-TT5 바뀜 → 2D·3D에서도 회전을 넣음(TT9). Task 계획은 [계획 §9](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#9-바닥-타일-칸별-회전-계획-tt-att-d).
- 요청: 2026-10-29 사용자 "바닥 타일을 한 타일씩 회전값을 넣고 싶은데 하나씩 지정해서 하기에는 힘들잖아? 그래서 Ctrl이나 Shift를 눌러 마우스로 드래그하는 거랑 하나씩 지정해서 하는 게 필요해 보여. 그리고 배치판이 아닌 밖에 있는 타일들도 회전을 넣어야 해서 그것도 지정이 가능하게 해야 합니다."

### 13.1 확인한 사실

- **게임 데이터에 칸별 회전이 없다.** `CombatMapTilesInput`은 종류 배열 `tiles` 하나다. `StageMapTiles.Build`는 프리팹을 타일 루트 아래 `Instantiate(prefab, root, false)`로 만들고 위치만 `CenterOf`로 둔다. 회전은 프리팹 자기 값 그대로다. 그래서 도구만 고쳐서는 회전이 게임에 나오지 않는다.
- **게임 쪽 두 파일은 용근님 코드다.** `CombatMapTilesData.cs`(입력·불변 값)와 `StageMapTiles.cs`(만들기)이고, 커밋은 `232d14c7`·`73ed12be`·`fdf29f22`다.
- **타일 회전을 읽는 다른 곳은 없다.** 바닥 교체 외형(`FieldTerrainPresenter.PlaceOnTile`)은 타일의 부모와 `localPosition`만 읽고, 회전은 필드 기준(`RotationToWorld`)으로 따로 준다. 높이 표(`CombatMapTileSurface`)와 칸 → 타일 대응(`TryTileAt`)도 회전과 무관하다. 회전은 타일 겉모습만 바꾼다.
- **가로·세로 크기가 다른 타일**은 90°·270° 돌리면 칸에 맞지 않는다. 지금 데이터는 Test 하나이고 정사각이다.
- **회전 값 형식:** 지형 외형 회전(`StageEnvironmentVisualDataSO`)과 아군 Lobby 보드가 Core 공용 `Rotation4`(R0~R270)를 View DB에서 이미 쓴다. 게임은 `Quaternion.Euler(0, r × 90, 0)`로 바꾼다(`FieldGridMapper.RotationToWorld`). 위에서 보면 시계 방향이다.
- **필드 밖 타일:** 도구 '타일' 보기는 타일 맵 전체(필드 밖 포함)를 화면 방향으로 그린다. 그래서 밖 타일도 여기서 고를 수 있다. 2D·3D 보기는 필드 칸만 누를 수 있다.
- **지금 '타일' 보기 입력:** 좌클릭·끌기는 칠하기, Shift+끌기는 사각형 칠하기, '기준 타일' 모드는 누른 타일로 기준을 옮긴다. 맵 2D 선택 도구는 Shift+끌기가 상자 선택이다.

### 13.2 설계 (TT1~TT8)

| 번호 | 내용 |
|---|---|
| **TT1 데이터** | `CombatMapTilesInput`에 `Rotation4[] rotations`를 더한다. 순서는 `tiles`와 같고(타일 루트 −Z 줄부터, 줄 안 −X부터), 길이는 0 또는 가로 × 세로다. **빈 배열 = 모두 R0**이라 지금 에셋은 그대로 쓰고 옮기기 작업이 없다. 불변 값은 `RotationAt(column, row)`을 낸다. 뜻은 타일 루트 위에서 본 시계 방향 90° 단위다(지형 외형 회전과 같은 `Rotation4`, 규칙 P4). |
| **TT2 게임 놓기** | `StageMapTiles.Build` 한 줄: `localRotation = Euler(0, r × 90, 0) × 프리팹 localRotation`. 회전 중심은 타일 피벗(`CenterOf` 자리)이다. 피벗이 타일 가운데가 아닌 프리팹은 돌리면 칸에서 벗어나므로 프리팹 조건으로 안내한다(타일 세트 툴팁). |
| **TT3 검사** | 길이가 0도 가로 × 세로도 아니면 예외, 정의되지 않은 값도 예외(`CreateData`). 가로 ≠ 세로 타일의 R90·R270은 Q-TT3에 따른다. 도구 저장 검사는 `GetData()`를 부르므로 같은 검사를 따로 만들지 않는다. |
| **TT4 도구 작업본** | `CombatStageSoTileLayout.rotations`(int[], 값 0~3). 빈 배열이면 모두 0으로 읽는다. 가로·세로를 바꾸면 종류처럼 (가로, 세로) 자리의 값을 남기고 새 칸은 0이다. 모두 0이면 저장 때 빈 배열로 쓴다(에셋 변경 최소). 작업본 형식 번호를 올린다(UT8: 옛 작업본은 에셋에서 다시 읽음). |
| **TT5 고르기** | '타일' 보기 모드 줄을 [칠하기 \| 회전 \| 기준 타일]로 한다. 회전 모드 입력은 아래 표다. 고른 타일은 창에만 둔다(작업본·초안에 남기지 않음, 가로·세로·세트가 바뀌면 비움). |
| **TT6 돌리기** | 고른 타일에: 버튼 '↻ 90°'·'↺ 90°'와 '0°·90°·180°·270° 지정', 단축키 R(시계)·Shift+R(반시계). 한 번 = Undo 한 번이고, 바뀌는 타일이 없으면 Undo를 남기지 않는다. |
| **TT7 화면 방향** | 저장 값은 타일 루트 기준이고 화면(필드)은 배경 회전·배율 부호만큼 다르다. 도구는 맞춤 계산의 축(UT4)으로 바꾼다. 화면에서 시계 방향 버튼을 누르면, 배경이 뒤집힌 경우(배율 축 하나가 음수) 저장 값은 반대 방향으로 들어간다. 각 타일 칸에는 화면에서 본 회전 표지(타일의 R0 앞쪽(+Z) 변이 간 쪽에 굵은 띠)와 각도 글자를 그린다. |
| **TT8 3D 미리보기** | `BuildTiles` 사본에 TT2와 같은 회전 한 줄을 넣는다. 2D 띠는 회전을 그리지 않는다(종류·교체만). |
| **TT9 2D·3D에서 돌리기** (Q-TT5 바뀜) | 맵 도구 줄 끝에 '타일 회전' 도구를 더한다(도구 열거 끝, 옛 순번 그대로). 이 도구일 때 2D·3D의 칸 누름은 그 칸의 타일을 고른다(칸 → 타일 대응은 2D 띠·3D와 같은 맞춤 결과 `TryTileOfCell`, P-UT12). 입력은 TT5 표와 같고(끌기는 지나간 칸의 타일, Shift+끌기는 칸 사각형이 덮는 타일), 돌리기는 TT6과 같다. **고른 타일 목록은 '타일' 보기와 하나**라 보기를 바꿔도 남는다. 표시: 2D는 고른 타일이 덮는 칸을 테두리로, 3D는 같은 칸 테두리 + 실제로 돌아간 타일. 고른 칸 줄에 그 칸 타일의 각도를 더한다. 타일이 없는 칸(필드가 타일 맵을 넘음)이나 맞춤이 안 되는 상태면 누름은 안내만 한다. 필드 밖 타일은 '타일' 보기에서만 고른다. |

TT5 회전 모드 입력:

| 입력 | 결과 |
|---|---|
| 클릭 | 그 타일 하나만 고름 |
| 끌기 | 지나간 타일들로 새로 고름 |
| Ctrl+클릭 | 그 타일을 더하거나 뺌 |
| Ctrl+끌기 | 지나간 타일을 더함 |
| Shift+끌기 | 사각형 안 타일로 새로 고름 |
| Ctrl+Shift+끌기 | 사각형 안 타일을 더함 |
| Ctrl+A / Esc | 모두 고름 / 고르기 비움(끄는 중이면 그 끌기만 취소) |

필드 밖 타일도 같은 방식으로 고르고 돌린다(TT5는 타일 맵 전체가 대상). 고른 수는 붓 줄 옆에 보인다.

### 13.3 결정 (Q-TT1~Q-TT5)

| 번호 | 질문 | 결정(2026-10-29 사용자) |
|---|---|---|
| Q-TT1 | 게임 쪽 두 파일(용근님, §13.4)을 누가 고치나 | **A 이 작업에서 고치고 용근님께 변경 정리 공유**("이 작업에서 고침"). 다른 안은 B 용근님께 요청하고 도구는 게임 쪽이 들어온 뒤 |
| Q-TT2 | 회전 편집 방식 | **A 고르기 + 돌리기(TT5·TT6)**("권장안대로, 봐서 불편하면 말함"). 다른 안은 B 회전 붓 |
| Q-TT3 | 가로 ≠ 세로 타일의 90°·270° | **A 막음**(게임 검사 예외, 도구 저장 검사가 알림) |
| Q-TT4 | 고르기를 종류 칠하기에도 쓰나 | **A 아니오**(회전만) |
| Q-TT5 | 2D·3D 보기에서도 타일을 고르나 | 권장은 '타일' 보기만이었으나 **2D·3D에서도 넣음**("3D에서도 회전이 반영되는 거나 3D나 2D에서도 회전을 넣을 수 있으면 좋겠습니다") → TT9. 3D 반영은 TT8 그대로 |

### 13.4 다른 작업자 영향

**게임 파일 두 개(둘 다 용근님 `232d14c7`·`73ed12be`·`fdf29f22`):**

| 파일 | 하는 일 | 바꿀 것 |
|---|---|---|
| `Systems/DataBaseSystem/Data/View/Table/StageViewProp/Item/CombatMapTilesData.cs` | View DB 바닥 타일 값. 입력 `CombatMapTilesInput`(인스펙터 'Map Tiles' 칸: 세트·가로·세로·크기·위치·기준 타일·종류 목록)과 검사, 불변 값 `CombatMapTilesData`(칸 자리·종류) | 입력에 `rotations` 칸, 검사 둘(길이·가로 ≠ 세로의 90°), 불변 값에 `RotationAt` |
| `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Map/StageMapTiles.cs` | 전투 진입 때 배경 아래 `MapTileRoot`를 만들고 칸마다 타일 프리팹을 놓음 | 타일 회전 한 줄 |

지형 표시·높이 표·타일 맞춤·바닥 교체 코드는 바뀌지 않는다(§13.1). 시험 코드는 이 두 형식을 쓰지 않는다(2026-10-29 확인).

- 용근님: 위 두 파일의 변경 정리를 전투 View 사본 공유에 적고, 사본 표에 회전 줄을 더한다.
- 민형님: 도구 열거 `TileRotate`와 창 직렬화 필드 `soTileMode`(옛 `soTileAnchorMode`)를 민형님 공유 §2에 한 단락. 호진 공유: 새로 알릴 것 없음.

### 13.5 Phase

TT-a 게임 데이터·놓기(TT1~TT3) → TT-b 도구 작업본·'타일' 보기 고르기·돌리기·표지(TT4~TT7) → TT-c 2D·3D 도구·3D 회전(TT8·TT9) → TT-d 문서·공유. Task는 계획 §9다.

### 13.6 구현 메모 (2026-10-29, TT-a~TT-d)

자세한 파일·차이는 [계획 §9.1~§9.3 실행 기록](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#9-바닥-타일-칸별-회전-계획-tt-att-d)이다. 설계와 다르거나 더한 것만 적는다.

- **TT7 각의 뜻:** 화면 각은 화면 절대 방향이 아니라 "프리팹 원래 모습에서 화면에서 본 시계 방향으로 돈 양"이다(`ScreenAngleOf = (부호 × 저장 값) × 90`, 0° = 돌리지 않음). 절대 방향으로 두면 배경을 돌린 Test에서 돌리지 않은 타일이 모두 270°처럼 보였다. 앞쪽 띠는 그대로 화면 절대 방향(프리팹 +Z 변)을 가리킨다. 뒤집힘이 없으면 화면 각 = 저장 값이다.
- **TT4 더함:** 회전 목록 길이가 틀린 에셋은 툴이 0°로 보이고 경고하며, 타일 하나를 돌리면 0° 목록으로 다시 채운다(틀린 값은 버림). 가로·세로를 바꿀 때 길이가 틀린 목록은 손대지 않는다(저장 검사가 알림).
- **TT9 다른 점:** 회전 줄은 공격 범위 줄 다음(타일 대응을 만든 뒤)에 보인다. 고른 타일 테두리는 '타일 회전' 도구일 때만 그리고, 목록은 다른 도구에서도 남는다. 타일 없는 칸을 누르면 안내 + (Ctrl 없으면) 고르기 비움. 이 도구로 누른 칸이 오른쪽 고른 칸이 된다. 맞춤 문제가 있으면 2D·3D 도구는 안내만, '타일' 보기 회전 모드는 계속 된다(P-TTT7).
- **문서·공유:** 사용법 §5.10 "타일 돌리기"·§8.2 저장 검사·§9 막히는 경우 셋, 전투 View 사본 공유 §8(용근님 게임 파일 두 개)·§1 표 회전 줄·§3 `rotations`, 민형님 공유 §2 한 단락. 모두 미전달.
- **남은 것:** 바닥 교체 외형은 필드 기준 회전이라 돌린 타일 칸이 무너져도 교체 외형은 돌지 않는다(게임과 툴 같음, 원하면 따로 정할 것). 2D 띠에는 회전을 그리지 않는다(TT8).

## 14 3D에서 판 밖 타일 고르기 (TO1~TO6)

- 상태: **TO-a·TO-b 완료(2026-10-29, Ua 확인 문제 없음 — 사용자, 새 시험 없음, 커밋 `f5c8109d4`, 구현 메모 §14.6).** 결정(2026-10-29 사용자 "권장안대로 진행"): Q-TO1~Q-TO3 모두 A. Task·실행 기록은 계획 §10.
- 요청: 2026-10-29 사용자 "전투 판 밖에 있는 타일들은 3D나 2D에서 클릭해서 회전은 할 수 없나요?" → 지금은 안 됨(TT9는 필드 칸만) → 3D는 계산 하나로 되고 2D는 판 둘레 격자를 새로 그려야 해 일이 크다고 보고 → "3D만 설계". 2D는 그대로(판 밖은 '타일' 보기).

### 14.1 확인한 사실

- 3D 클릭은 `CombatStageSoPreview.PickGround`가 필드 바닥 평면(y = 0)과 만나는 점을 내고, `CombatStageSoSpace.CellAt`이 칸으로 바꾼다. **판 밖이면 null**이라 `HandleSoPointer`가 누름을 받지 않는다. 끄는 중에는 `ClampedCellAt`이 판 안으로 붙인다.
- 판 밖 타일은 3D에 이미 그려진다(`BuildTiles`가 타일 맵 전부를 놓음).
- 칸 → 타일 `CombatStageSoTileFit.TryScreenTileOfCell`은 칸 번호가 음수면 false다(기준 타일 + 칸 번호 ÷ 타일 한 장의 칸 수, 정수 나눗셈). 판 밖 칸(음수 포함)도 내림 나눗셈으로 바꾸면 같은 식으로 타일 맵 어디든 닿는다(판 안 칸 결과는 그대로).
- 3D 테두리는 `space.Corners(칸 사각형)`로 그리는데, 칸 번호가 판 밖이어도 위치를 계산할 수 있다(덧그리기 높이는 판 밖 = 0). 지금 타일 테두리는 `TryFieldCellsOfTile`이 판 안으로 잘라 낸다.
- 2D·3D는 테두리 목록(`SoFrames`)을 같이 쓴다. 2D는 판 밖 테두리를 그리면 격자 밖에 선이 생기므로 지금처럼 잘라야 한다.

### 14.2 설계

| 번호 | 내용 |
|---|---|
| **TO1 칸 → 타일 넓히기** | `TryScreenTileOfCell`을 내림 나눗셈으로 바꿔 판 밖 칸(음수·판 크기 이상)도 타일을 찾는다. 결과가 타일 맵 밖이면 false. 판 안 칸 결과는 그대로라 2D 띠·높이·3D 숨김은 바뀌지 않는다(원본 `CombatMapTileSurface.Build`는 판 안 칸만 묻는다는 주석). |
| **TO2 3D 클릭** | '타일 회전' 도구일 때만 3D가 `HandleSoPointer`에 넘기는 칸 함수를 **판 밖도 내는 칸**(`CombatStageSoSpace.RawCellAt`을 공개)으로 바꾼다. 다른 도구는 지금처럼 판 안만. 판 밖 누름은 '고른 칸'(오른쪽 칸 정보)을 바꾸지 않는다. |
| **TO3 끌기·사각형** | 끌기는 지나간 칸의 타일을 더한다(TT9 그대로, 판 밖 포함). Shift+끌기 사각형은 두 끝 칸을 타일 화면 좌표로 바꿔 **타일 맵 안으로 붙인 타일 사각형**을 고른다(판 밖 사각형이 커도 타일 수만큼만 돈다). 끄는 사각형 테두리도 그 타일 사각형이 덮는 칸으로 그린다. 2D는 지금처럼 판 안 칸 사각형이다. |
| **TO4 3D 테두리** | 3D에는 고른 타일이 덮는 칸 사각형을 **자르지 않고** 그린다(판 밖 타일도 하늘색 테두리). 2D는 지금처럼 판 안으로 자른다. `SoFrames`가 "판 안으로 자르기" 인자를 받는다. |
| **TO5 키** | Ctrl+A는 Q-TO1. R·Shift+R·Esc는 그대로. |
| **TO6 알림·문서** | 도구 설명 줄 "필드 밖 타일은 '타일' 보기에서" → "2D에서는 판 안만, 판 밖은 3D나 '타일' 보기에서". 사용법 §5.10 타일 돌리기 한 줄. |

### 14.3 정할 것 (Q-TO1~Q-TO3)

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-TO1 | 3D의 Ctrl+A | **A 타일 맵 전부**(판 밖도 고를 수 있게 된 보기라서). 2D는 지금처럼 판 위 타일. B 2D와 같게 판 위 타일만 |
| Q-TO2 | 3D 클릭 높이 | **A 지금처럼 바닥 평면(y = 0)으로 고름**. 타일 윗면 높이(지금 0·−0.1)만큼 비스듬히 볼 때 조금 어긋난다(칸 크기 1에 비해 작음). B 타일마다 윗면 평면으로 고름(정확하지만 계산이 늘고, 높이 다른 타일이 섞이면 평면을 여러 번 봐야 함) |
| Q-TO3 | 3D에서 마우스 아래 타일 미리 보이기 | **A 안 함**(누르면 테두리가 바로 보임, 가장 단순). B '타일 회전' 도구일 때 마우스 아래 타일에 옅은 테두리 |

### 14.4 다른 작업자 영향

없다. 도구 파일만 바뀌고(`CombatStageSoTiles.cs`·`CombatStageSoVisuals.cs`·`CombatLayoutWindow.SoTiles.cs`·`CombatLayoutWindow.SoMode.cs`), 게임 코드·에셋·DT 모드는 그대로다. 새 파일 없음.

### 14.5 Phase(안)

TO-a 코드(TO1~TO5, 한 번에) → TO-b 문서(TO6, 사용법·구현 메모). 작은 작업이라 계획은 계획 §10에 Task만 짧게 쓴다.

### 14.6 구현 메모 (2026-10-29, TO-a·TO-b)

- 설계대로다. 더한 것 하나: Shift+끌기 사각형은 2D도 같은 "두 끝 타일 사이 타일" 계산을 쓴다. 골라지는 타일은 전과 같고, 끄는 동안 테두리가 칸 사각형 대신 덮는 타일 경계(판 안으로 자름)를 따른다.
- 문서: 사용법 §5.10 타일 돌리기의 Ctrl+A 줄·판 밖 줄, §9 '타일 회전' 줄. 공유할 것은 없다(도구 파일만, 이름 바뀐 직렬화 필드 없음).

## 15 후속 보완: 배치 검사·토네이도·공개 대상 (FU1~FU6)

- 현재 상태: **2026-10-31 FU-a·FU-b 코드 및 FU-c 안내·문서 반영 완료, 컴파일·Unity 미확인.** 사용자 후속 구현 지시에 따라 반영했다. 고지대 B1은 보류이며 Phase 사용자 인수·공유 전달은 별도로 확인한다.
- 기준: 로컬 HEAD `bd0447662`. 코드 원본을 다시 대조했으며 최신 원격 브랜치를 가져오거나 Unity를 실행한 것은 아니다. Task는 [계획 §11](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#11-후속-보완-계획-fu-afu-c).
- **보류·범위 밖:** B1 고지대 이웃 모양, 고지대 삭제·규칙 변경, 스테이지 완전 삭제, 약한 지반 한도 숫자 표시(C3), 실제 전투 시뮬레이션·특성 계산·전투 되돌리기. 기존 고지대 정의·기본 외형·높이·외형 회전 금지는 유지한다. 보류 해제는 기획 확인 뒤 별도 요청으로 한다.
- 설계 당시 이력(2026-10-31, 구현 전 재검토): 사용자 요청으로 숨은 배치의 묶음 편집 경계와 생성 전 호환 배치 정리를 §15.2.1~§15.2.2에 추가했다. 문서만 반영했으며 고지대 보류·코드 미착수를 유지한다.
- 문서 작성 선택: 공개 대상은 아래 두 보기 방식으로 구분한다. 이는 사용자가 구체 UI를 확정한 기록이 아니라, 전체 배치를 편집할 수 있게 유지하면서 Player 표시를 대조하기 위한 이번 설계안이다.

### 15.1 현재 사실과 기존 목록 정정

| 사실 | 코드 원본·결론 |
|---|---|
| 벽·낭떠러지 위 초기 배치 | `CoreCaculator/Application/Combat/CellEnvironmentQuery.ValidateInitialPlacement`는 낭떠러지 위 살아 있는 캐릭터·구조물을 거절한다. 벽의 공존 금지는 `Domain/Placement/EnvironmentPlacementRules.Evaluate`가 맡는다. **옛 C1의 ‘낭떠러지 위 고정 적은 허용, 경고만’은 틀려 폐기한다.** |
| 저장 검사에서 빠지는 대상 | `DataBaseSystem/Data/InGame/Table/Stage/Combat/CombatStageEnvironmentAuthoringValidation.Validate`는 환경과 장애물을 만들고 캐릭터는 빈 목록으로 넘긴다. 적 유닛·적 구조물·본부는 포함하지 않는다. 벽 위 오브젝트·환경·장애물 등 기존 SO 검사가 잡는 것은 중복 구현하지 않는다. |
| 실제 진입 검사 | `BattleEnvironmentPreparation.Prepare`는 캐릭터·구조물을 포함해 같은 Core 검사를 호출한다. 도구 사전 검사는 이를 대체하지 않는다. |
| 토네이도 | `ViewSystem/Combat/Display/Map/FieldTerrainPresenter.LocalPosition`·`TornadoBaseHeight`: +X·+Z 반 칸 이동, 앵커와 +X/+Z 방향 2×2의 판 안 타일 원래 바탕 중 최대 높이. 바닥 교체 후 바탕과 지형 높이는 사용하지 않는다. |
| 공개 대상 | `TerrainViewSet.IsShownFact`·`CombatEnvironmentFacts.IsKnownToPlayer`: 활성 지형 중 `Owner.Player`가 공개 목록에 있어야 표시·높이·타일 숨김에 참여한다. `ObjectViewSet.Render`도 Player에게 공개된 활성 오브젝트만 표시한다. 현재 도구 `CombatStageSoSpace.RaiseSurfaces`는 공개 대상을 보지 않는다. |
| 생성 검사 | `CombatStageSoCreation.Create`도 `CheckDocument`를 호출한다. 현재 새로 만들기는 작업본 배치를 비운 뒤 검사하지만, 실제 호환 배치 배열은 에셋 복사 후 `ClearLegacyPlacements`로 비운다. FU2에서는 검사 복제본과 최종 생성 데이터의 상태를 일치시켜야 한다. |
| 화면 편집 | `DeleteSoSelection`·`CopySo`·`MoveGroup`·`MirrorFlip`·`MirrorCopy`는 현재 전체 배치에서 대상을 구한다. 클릭 후보만 거르면 숨은 배치가 묶음 연산에 포함되므로 FU5의 공통 대상 판정이 필요하다. |
| 공격 범위 | `CombatStageSoRange.ShapeOf`·`CombatStageSoAssets.ReadAttack`는 SO의 기본 사거리·모양·스플래시만 쓴다. 최근 특성으로 생긴 사거리 보정·상태 변화는 반영하지 않는다. |

### 15.2 동작 설계

| ID | 보완 계약 |
|---|---|
| FU1 | **벽·낭떠러지와 고정 배치 겹침은 오류.** 고정 적 유닛·적 구조물·참조가 있는 적 본부의 회전된 점유 칸 전체를 검사한다. 한 칸이라도 벽 또는 낭떠러지와 겹치면 검사 결과에 종류·배치 이름·좌표를 표시하고 저장·등록을 막는다. 앵커만 검사하지 않는다. 놓기·옮기기의 즉시 차단은 기존대로 두고 검사·저장·등록에서 같은 검사 함수를 사용한다. 같은 함수를 쓰는 새로 만들기·배치 복제의 입력 준비는 FU2를 따른다. |
| FU2 | **검사 입력과 한계.** 저장할 작업본을 반영한 임시 복제본에서 새 환경 배치와 숨김 호환 `initialWallCells`·`staticTerrainPlacements`를 읽는다. 공개 대상과 무관하게 검사하며, 3D 보기 선택도 검사 결과에 영향을 주지 않는다. 무작위 적·무작위 시작 토네이도의 자리를 추측하지 않고 미사용 메인 캐릭터 위치도 검사 대상에 넣지 않는다. 기존 SO 검사 오류는 유지한다. **새로 만들기의 ‘배치 비우기’는 임시 복제본의 호환 배치도 검사 전에 비우고, ‘배치까지 복제’와 일반 저장은 보존한다(§15.2.1).** 이 검사 통과를 전체 전투 진입 보장이라고 표시하지 않는다. |
| FU3 | **토네이도 위치·높이 일치.** 3D 위치 = 앵커 칸 중심 + `(CellWidth/2, 최대 원래 타일 바탕, CellLength/2)` + 외형 오프셋. 최대값은 `(x,y)`, `(x+1,y)`, `(x,y+1)`, `(x+1,y+1)` 중 판 안 칸으로 구한다. 판 밖 칸은 제외, 판 안이지만 타일 밖인 칸의 바탕은 0. 첫 유효 값에서 시작해 전부 음수일 때 0으로 끌어올리지 않는다. 프리팹·외형 SO의 기존 회전·배율을 유지하고 배율 2를 다시 곱하지 않는다. 2D 표식·선택·저장 앵커는 그대로, 무작위 시작은 위치 미확정으로 유지한다. |
| FU4 | **3D 공개 대상 두 보기.** `전체 배치`(기존 편집 기준, 기본값) / `Player 기준`을 3D 보기 설정으로 둔다. 전체 배치는 고정 위치의 지형·오브젝트를 모두 표시한다. Player 기준은 공개 목록에 Player가 포함된 것만 표시하고, 표시할 지형 집합 하나로 높이·바닥 교체·타일 숨김을 모두 계산한다. `knownTo`는 작업본에서 열거 순번이므로 `Names.owner`의 이름으로 Player를 판별하고 숫자 1/2를 고정하지 않는다. 빈 목록은 공개로 간주하지 않고, 잘못된 이름·참조는 오류를 알린다. 적·구조물·본부에 없는 공개 필드를 새로 만들지 않는다. |
| FU5 | **편집·캐시·수명.** 2D·타일 보기·배치 목록은 전체 배치를 유지하고, 3D에는 현재 보기 기준과 제외된 지형·오브젝트 수를 알린다. Player 기준의 화면 편집은 **클릭·지우개·단축키·상자/묶음 연산 모두 표시 대상만** 처리한다(§15.2.2). 숨은 배치는 덮어쓰지 않으며, 충돌 검사는 숨은 배치를 포함한 전체 데이터로 유지한다. 이미 선택된 숨은 배치는 선택을 보존하고 패널에서 ‘현재 3D 기준에서 숨김’으로 알린다. 보기 설정은 창 표시 상태만 바꾸고 SO·초안·Dirty·Undo 이력을 바꾸지 않는다. 기준 전환·knownTo 편집·Undo/Redo·명시적 새로고침은 높이/배치/숨길 타일을 함께 다시 계산한다. 기존 P-UT15의 자신이 숨긴 타일만 복원하는 계약과 창 종료·재컴파일 해제를 유지한다. |
| FU6 | **안내·기록.** 공격 범위 안내에 ‘특성·버프/디버프에 따른 사거리 보정은 반영하지 않은 기본 범위’를 명시한다. 범위 계산 자체는 확장하지 않는다. 사용법과 공유 문서에는 FU1의 저장 오류/나머지 Core 진입 검사, 토네이도 식, 3D 두 기준·한계, B1 보류를 기록한다. 완료 상태·커밋·사용자 검증·공유 전달 여부를 별개로 기록한다. |

### 15.2.1 FU2 보완 — 생성 목적에 맞는 검사 입력

| 경로 | 검사할 임시 복제본 | 실제 쓰기·보존 |
|---|---|---|
| 일반 검사·저장·등록 / 배치까지 복제 | 작업본을 적용하고 호환 벽·지형을 보존한 상태로 검사 | 일반 저장은 호환 필드 보존. 배치 복제는 호환 배치까지 그대로 복사하고, 오류면 새 에셋을 만들지 않음 |
| 새로 만들기 — 배치 비우기 | `ClearPlacements`를 적용한 작업본을 쓴 뒤, 임시 Combat 복제본의 `initialWallCells`·`staticTerrainPlacements`도 비우고 **기존 SO 검사와 새 배치 검사 모두 실행** | 통과 후에만 에셋을 복사하고 같은 배치 비우기를 반영. 틀 원본은 변경하지 않음. 기존 계약대로 본부 자리·참조는 이어받음 |

- `CheckDocument`의 검사 입력 준비에 명시적인 `clearLegacyPlacements` 인자(기본 false)를 둔다. `CombatStageSoCreation.Create`에서만 `!copyPlacements`로 전달한다. 일반 저장·등록에서는 false이며, 작업본의 호환 개수/ID 목록이 비었다는 이유로 실제 배열을 지우지 않는다.
- 비우기 구현은 기존 `CombatStageSoAssets.ClearLegacyPlacements`를 재사용한다. 임시 복제본에 작업본 반영 → 필요한 경우 호환 배치 비우기 → 검사 → 통과 후 새 파일 생성 순서다. 검사를 통과시키려고 틀 원본을 먼저 수정하지 않는다.
- 예: 틀의 호환 벽/낭떠러지가 본부 칸과 겹치면 배치까지 복제는 오류로 중단하지만, 배치 비우기는 그 지형을 제외한 최종 데이터로 검사한다. 나머지 SO 오류까지 무시하지 않는다.

### 15.2.2 FU5 보완 — 표시 대상과 편집 대상의 일치

- **적용 범위:** `3D + Player 기준`의 화면 편집에만 표시 필터를 적용한다. 2D·전체 배치 모드는 기존 전체 대상이며, 타일 보기·타일 회전은 지형 공개 여부와 독립적으로 기존 동작을 유지한다.
- **같은 대상 판정을 쓸 동작:** 클릭/끌기 시작, 지우개(한 칸·끌기·사각형), Delete·R, 상자 삭제·묶음 이동·복사·좌우 뒤집기·대칭 복사. 상자가 없을 때의 전체 뒤집기/대칭도 이 모드에서는 ‘표시 배치 전체’로 한정하고 버튼 문구에 범위를 알린다. 선택 테두리·고스트·대상 수 역시 실제 처리할 배치와 일치시킨다. 본부 삭제·복사 제외 등 기존 도구별 제한은 그대로다.
- **선택·Undo:** 기존 선택 칸·상자는 보존하되, 작업을 실행할 때 현재 모드/knownTo로 대상을 다시 구한다. 보기 전환이나 Undo 뒤의 옛 대상 목록을 재사용하지 않는다. 숨은 배치는 화면의 Delete·R로 바꾸지 않는다. 대상이 없거나 전부 숨김이면 안내만 하고 Dirty·Undo·초안을 만들지 않는다. 미확정 끌기는 기준 전환 시 취소한다.
- **패널 편집:** 전체 목록과 이미 선택된 숨은 배치의 패널은 유지한다. ‘현재 3D 기준에서 숨김’ 안내 아래 특정 배치를 명시적으로 편집하는 동작은 허용한다. 화면 단축키와 패널의 직접 편집을 구분한다. 숨은 배치를 포함한 화면 묶음 편집은 전체 배치 모드나 2D에서 한다.
- **충돌 검사:** 편집할 대상만 거르고, `PlacementError` 등 놓기/이동/붙여넣기 충돌 검사는 전체 배치를 본다. 숨은 동일 정의의 지형·오브젝트도 기존 겹침 규칙에 따라 막으며, 작업본에서 잠깐 삭제해 검사하지 않는다.
- **붙여넣기:** 복사된 데이터의 공개 목록은 보존한다. 다른 보기에서 복사한 내용 중 Player 기준에서 숨겨질 배치가 있으면 이 모드의 붙여넣기는 실행하지 않고 전체 배치/2D로 전환하도록 안내한다. 조용히 일부만 붙이거나 공개 목록을 자동으로 바꾸지 않는다. 모두 표시 대상이면 기존 붙여넣기·충돌 규칙을 쓴다.
- **새 배치 기본 공개 대상(2026-10-31 사용자 지정):** 클릭·드래그·사각형 칠하기로 새로 놓는 지형·오브젝트는 Player와 Enemy만 기본 선택한다(None·Neutral 제외). 공통 신규 배치 생성에서 `Names.owner`의 이름을 순번으로 바꾸며 숫자를 고정하지 않는다. 목록은 배치마다 새로 만든다. 기존 에셋/초안 읽기·복사/붙여넣기·대칭 복사·스테이지 복제는 원래 공개 목록(빈 목록 포함)을 보존하고, 배치 뒤 개별 수정도 허용한다.

### 15.3 구현 경계와 원칙 점검

- **선택한 접근:** 기존 `GetData()` 검사에 한정된 사전 배치 검사를 덧붙이고, 기존 3D 계산에 토네이도 식·공개 필터를 연결한다. 전체 전투 준비를 툴에서 실행하는 방식은 로스터·Run·서비스 조립까지 요구해 제외한다. Player 기준만 강제하면 비공개 배치를 편집하기 어려워 두 기준을 구분한다.
- **SO 접근:** `CombatStageSoAssets`가 정의 종류·회전된 점유 크기·숨김 호환 칸을 읽고 정규화한다. 복제본 기준 읽기이므로 저장 전 필드 수정도 반영한다. SO 직렬화 형식, Systems 공개 범위, asmdef 참조를 늘리지 않는다.
- **검사 책임:** 새 도구 내부 `CombatStageSoPlacementChecks.cs`는 이름·점유 칸·벽/낭떠러지 칸을 받아 문제 목록만 반환한다. `CombatStageSoValidation`이 기존 검사에 조립한다. Core 규칙 사본은 벽/낭떠러지와 고정 배치의 교차 두 조건에 한정하고 원본 이름·이 검토 기준을 주석과 공유 메모에 둔다.
- **생성 조립:** `CombatStageSoCreation`이 생성 목적만 전달하고, `CombatStageSoValidation`이 검사 복제본의 준비·해제를 맡는다. 호환 배열 비우기는 `Assets` 한 곳을 재사용한다.
- **편집 조립:** `CombatLayoutWindow.SoMode`가 보기 기준을 연결하고, `CombatStageSoDocument`의 묶음 연산은 명시적으로 전달받은 대상 판정을 사용한다(기존 호출의 기본은 전체). 공개 판정은 3D와 공유한다. `TrySoEdit` 등의 사전 확인 복제본에서도 그 복제본의 배치로 대상을 다시 구하며 원본 배치 객체 참조를 넘겨 쓰지 않는다. 전체 배치 기반의 충돌 판정은 별도로 유지한다.
- **3D 책임:** `CombatStageSoVisuals`의 공간/표시 계산이 토네이도 원래 바탕과 공개 여부를 제공하고, `CombatStageSoPreview`는 선택된 값으로 객체를 배치·복원한다. `CombatLayoutWindow.SoMode`는 보기 설정과 안내·입력 후보 연결만 맡는다. 새 기능을 전역 서비스로 등록하지 않는다.
- **SSOT·SOLID·JMH/FW:** 원본 규칙은 Core·전투 View, 저장 원본은 SO, 도구 계산은 파생값이다. 읽기/검사/표시/조립 책임을 나누고, 검사 함수는 값 입력·문제 목록 출력으로 상태를 쓰지 않는다(S·I·D). 기존 오류·반복 해제 보장을 유지한다(L). 고지대 보류에 토네이도 분기를 결합하지 않는다(O). FW 서비스 등록·수명·입력 계약은 변경하지 않고 PreviewRenderUtility의 기존 소유자가 자원을 정리한다.
- **기능 변경의 독립성:** 배치 오류 문구·교차 조건 변경은 검사 파일과 그 조립/관련 수동 확인으로 끝난다. 토네이도 자세 변경은 공간 계산·3D 배치 확인, 공개 기준 변경은 공유 표시/편집 대상 판정·캐시/복원 확인으로, 생성 목적 변경은 생성 조립·검사 복제본 준비와 두 생성 경로 확인으로 한정한다. 공유 높이·숨김 경계를 바꾸면 해당 3D 회귀는 필요하지만 DT·맵 생성·특성 실행은 함께 수정하지 않는다.

### 15.4 상태·추적

| 요구 | 계획 | 상태 |
|---|---|---|
| FU1·FU2 배치 검사·생성 입력 일치 | FU-a(Fa.1~Fa.5), Ua.1~Ua.8 | 반영 완료 · 컴파일/Unity 미확인 |
| FU3 토네이도·FU4/FU5 공개/편집 기준 | FU-b(Fb.1~Fb.5), Ub.1~Ub.11 | 반영 완료 · 컴파일/Unity 미확인 |
| FU6 안내·사용법·공유 | FU-c, Uc | 반영 완료 · 컴파일/Unity 미확인 |
| B1 고지대 | 이번 Task 없음 | 사용자 보류 |

이번 변경은 이 설계와 계획만 수정한다. 사용법·공유 메모의 후속 갱신은 FU-c의 미완료 작업이며, 코드나 검증이 끝났다고 적지 않는다.

### 15.5 FU-a 구현 기록 (2026-10-31)

설계 FU1·FU2와 생성 입력 보완을 도구 코드에 반영했다. 새 검사 파일 한 개와 `.meta`, Assets·Validation·Creation을 변경했다. Core·런타임·에셋 변경은 없다. 컴파일·Unity 확인 및 Phase 인수는 미완료이며, 실행 기록은 계획 §11.5를 따른다. FU3~FU6은 미착수, 고지대 B1은 계속 보류다.

### FU-b 구현 기록 (2026-10-31)

- 토네이도: 판 안 2×2 원래 타일 바탕의 최대 높이와 반 칸 이동을 Preview에 적용. 기존 배율·오프셋·회전·저장 앵커 유지.
- 공개 기준: 3D ‘Player 기준’ 토글(기본 꺼짐 = 전체 배치). 공통 IsShown 판정을 높이·외형·타일 숨김·화면 편집에 적용하고 배치 캐시 키에 기준을 포함. 작업본에 보기 설정을 저장하지 않음.
- 편집: 클릭·지우개·삭제·회전·상자 이동·복사·뒤집기·대칭·고스트는 표시 대상만 사용. 충돌은 전체 배치로 확인. 숨은 배치가 있는 붙여넣기는 전체 배치/2D 전환 안내. 패널의 명시적 개별 편집은 숨김 안내와 함께 유지.
- 변경: Tool/Editor의 Assets·Visuals·Preview·Document·SoMode. 새 파일·Core·런타임·에셋 변경 없음. FU-c는 미착수, 고지대 B1은 보류.
- 검증: 호출부·필터 전달·캐시 키 정적 대조. **컴파일 미확인, Unity Ub.1~Ub.11 미확인.** MSBuild·테스트·커밋·push 없음. FU-b Phase 보고 지점이며 사용자 인수 전.

### FU-c 반영 기록 (2026-10-31)

- 범위 안내: SoMode 화면에 특성·버프/디버프 사거리 보정 미반영을 명시. 범위 계산은 변경하지 않음.
- 사용법: 기본 범위의 한계, 토네이도 반 칸·2×2 원래 바탕 높이, 전체/Player 기준과 화면·패널 편집 범위, 숨은 배치 붙여넣기 제한, 벽/낭떠러지 저장 오류와 나머지 Core 진입 검사, 배치 비우기/복제 차이를 갱신.
- 공유: 전투 View §9, 민형님 §5, Core 전달 §42.2a에 원본·사본·책임·검사 한계를 기록. 모두 미전달. 고지대 B1은 계속 보류.
- 정적 대조: 문구·사용법·호출 경로 및 문서 차이 검사. 컴파일·Unity Ua/Ub/Uc의 실제 화면 확인은 미완료이며 기존 검증으로 대체하지 않음. 새 테스트·MSBuild·커밋·push 없음. 이번 코드는 안내 문자열만 변경했고 에셋·Core·런타임은 수정하지 않음.

### FU 검토 후 오류 복구·상태 정정 (2026-10-31)

Player 기준에서 정의·공개 대상 판정 오류가 발생하면 해당 배치를 표시·화면 편집 대상에서 제외하고 sourceId·좌표·원인을 안내한다. 오류가 오른쪽 개별 편집 패널 그리기를 중단하지 않도록 `TryIsShown` 경계를 둔다. 패널은 정상 비공개와 판정 오류를 구분하며, 잘못된 공개 목록은 사용자의 명시적 비우기로 복구한다. 안내만으로 저장값·Undo를 변경하지 않는다.

현재 상태 표와 본문을 FU-a~FU-c 반영 완료로 맞추고 이전 문서 작성·Phase 기록은 이력으로 남겼다. 실행·검증 상태 원본은 계획 §11.6이다. **고지대 B1은 계속 보류, 컴파일·Unity 미확인, 공유 미전달**이다.

## 16 편집 UX 보완: 오류 위치·작업 상태·숨김 안내 (UX1~UX3)

- 상태(2026-11-02): **UX-a~UX-c 코드·관련 안내 문서 반영, 컴파일·Unity 미확인.** UX-c는 UXR-b 뒤 사용자 요청으로 진행했다. FU-a~FU-c 및 UXR 단계와 구분하며 Task·확인 원본은 [계획 §12](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#12-편집-ux-보완-계획-uxauxc)다.
- 목적: 검사 오류를 찾아 수정하는 이동을 줄이고, 현재 작업본의 저장 여부와 보기 기준을 오해하지 않게 한다. 고지대 B1은 계속 보류한다. Core·런타임·DB SO·DT 모드·기존 배치 규칙·고지대 기능을 변경하지 않는다.
- 범위 밖: 패널 전면 재배치, 테마 변경, 자동 저장·자동 수정, 새 검사 규칙, 전투 시뮬레이션, 스테이지 삭제, 3D 카메라 자동 이동. 이번 세부 UI와 연결 방식은 기존 구조에 맞춘 설계 선택이며 사용자 Unity 인수 기록은 아니다.

### 16.1 접근과 현재 연결점

새 전용 오류 창은 기존 선택·패널 상태를 중복 소유하므로 만들지 않는다. 오류 문자열에서 좌표를 추출하는 방식은 문구·언어 변경에 취약하므로 쓰지 않는다. **기존 검사 결과에 선택적 위치 정보를 붙이고, 기존 상단 상태·고른 칸 패널·보기 토글을 확장한다.**

설계 당시 `soProblems`는 문자열 목록이고 `DrawSoCheckResult`는 읽기 전용 HelpBox였다. UX-a 반영 뒤에는 창 전용 `CombatStageSoCheckResult`와 위치 버튼을 사용한다. 저장 여부는 `soModified`, 선택은 `soSelectedCell`·`showSoCell`, 보기 기준은 `soMapView`·`soPlayerOnly`가 소유한다. 공개 여부는 `Visuals.IsShown`/`TryIsShown`이 판정하며, 판정 오류에서도 편집 패널을 유지하는 FU 후속 보완은 이미 코드에 반영되어 있다. UX3는 이 오류 복구를 다시 만들지 않는다.

### 16.2 UX1 — 오류 위치 바로 찾기

- 검사 결과의 **위치를 특정할 수 있는 행**에 `위치 보기 (2D)`를 둔다. 누르면 진행 중인 끌기·칠하기를 기존 종료/취소 경로로 정리하고 선택 도구·2D로 전환한다. 이전 묶음 상자는 해제하고, 문제 칸을 선택·강조하며 고른 칸 접기를 연다. 대상 배치가 있으면 해당 항목을 강조하고 오른쪽 스크롤을 그 항목으로 이동한다. 2D 맵 스크롤도 문제 칸이 보이는 영역에 들어오도록 한 번 이동한다. 맵 끝에서는 유효 스크롤 범위로 제한하고 이후 사용자 스크롤을 강제로 되돌리지 않는다. 자동 저장·데이터 수정·삭제는 하지 않는다.
- 여러 칸 배치는 **문제 칸과 배치 앵커를 구분**한다. 오류에는 겹친 칸을 표시하고, 패널에는 원인 배치를 지정한다. 문제 칸이 점유 범위 가장자리에 있어도 해당 배치를 찾아야 한다. 같은 칸의 여러 배치는 이름만으로 구분하지 않는다.
- 1차 위치 지원: FU-a 벽/낭떠러지 고정 배치 겹침, 도구가 직접 찾는 맵 밖 배치·아군 구역 칸, UX3의 고정 위치 공개 판정 오류. `GetData()`의 임의 예외·표/맵 규칙 오류는 기존 원문을 보존하고 위치 버튼을 만들지 않는다. 좌표가 없거나 판 밖이면 좌표/이유를 보여 주고, 잘못된 칸으로 보정해 이동하지 않는다. 배치 앵커가 판 안이면 앵커의 편집 패널로 이동하는 별도 `배치 보기`를 제공한다. 앵커도 판 밖이면 칸으로 이동하지 않고 `배치 수정`으로 대상 패널을 직접 연다. 새 전체 배치 목록은 만들지 않는다. 패널에 이동할 X·Y를 함께 입력한 뒤 적용하는 복구 동선을 둔다(기존 배치 검사·Undo 경로 사용). 양 축이 모두 판 밖이어도 한 축씩 수정하다 거절되는 막힘이 없어야 한다. 판 밖 아군 구역 칸은 배치가 아니므로 기존 스테이지 구역 설정을 안내한다.
- 대상 정보는 도구 내부 결과 값 `CombatStageSoIssue`로 전달한다: `Message`, 선택적 `ProblemCell`, 선택적 `Target`(배치 층·해당 검사 작업본의 목록 인덱스, 본부는 단일 대상). 결과 묶음이 Root GUID와 작업본 개정 번호를 소유한다. sourceId가 없는 유닛·구조물에 영구 ID를 추가하지 않는다. 문자열 파싱·복제본 객체 참조·에셋 GUID만으로 배치 식별을 하지 않는다.
- `PlacementChecks`는 입력 대상의 식별 정보를 결과로 돌려주고 `Assets`가 정의·점유 칸과 함께 이를 준비한다. 일반 오류는 위치 없는 결과로 감싼다. `Validation`의 상세 결과를 원본으로 삼고 기존 문자열 소비자(생성 등)는 `Message` 투영을 사용한다. 같은 오류를 얻으려고 검사를 두 번 실행하거나 규칙을 복제하지 않는다. 검사·저장·등록의 차단 기준과 생성 전 임시 복제본 경계는 유지한다.
- 작업본 수정·Undo/Redo·스테이지 전환·초안 복원·에셋 다시 읽기·목록 새로고침·재컴파일 뒤에는 옛 위치 결과를 폐기한다. 같은 Root라도 재검사 전에는 옛 인덱스나 좌표로 이동하지 않는다. 외부 에셋 변경의 실시간 감시는 범위 밖이며 기존 명시적 새로고침/재검사가 필요하다.

### 16.3 UX2 — 작업 상태 표시

상단에 짧은 상태 줄을 두고 색과 함께 텍스트로 구분한다. 폭이 좁으면 줄바꿈하며 기존 버튼을 가리지 않는다.

| 표시 | 원본·뜻 |
|---|---|
| 스테이지 이름 · 키 | 열린 Root 이름과 작업본 stageKey. 없으면 `열린 스테이지 없음` |
| `저장 안 됨` / `미저장 변경 없음` | 기존 `soModified`. 새 Dirty 비교·자동 저장을 만들지 않음. 초안 복원·Undo 뒤 기존 보수적 Dirty 정책 유지 |
| `2D · 전체 배치` / `3D · 전체 배치` / `3D · Player 기준` / `타일 보기` | 기존 보기 값에서 계산. 2D·타일 보기에서는 기억된 Player 토글을 현재 적용 중이라고 표시하지 않음 |
| `미검사` / `변경 후 재검사 필요` / `검사 통과` / `검사 오류 N건` | 현재 작업본에 대한 검사 결과 수명에서 파생. `검사 통과`는 전체 전투 진입 성공을 보장하지 않음 |

- 검사 실패와 저장 실패를 분리한다. 검사 통과 후 파일 쓰기가 실패해도 저장 완료로 표시하지 않으며 미저장 상태와 실패 안내를 유지한다. 등록/해제·보스 교체 등 별도 작업 실패도 기존 알림으로 보존한다.
- 검사 수명은 창 전용 결과 묶음 한 곳에서 소유한다. 기존 `soChecked`/`soProblems`와 별개의 성공 캐시를 만들지 않는다. 작업본이 바뀌면 결과를 비우고 재검사 필요로 전환하며, 새 스테이지 열기/재컴파일은 미검사로 시작한다. 보기 전환·위치 이동은 검사 결과와 Dirty를 바꾸지 않는다. 저장 성공 뒤 데이터가 추가 변경되면 결과도 무효화한다.
- 위치 이동·상태 표시·보기 전환은 SO·초안·Undo에 기록하지 않는다. Undo/Redo 시 데이터는 기존 흐름을 따르고 검사 결과만 무효화한다. 기존 창 표시 설정의 보관 방식은 유지하되 검사 스냅샷은 재컴파일 뒤 재사용하지 않는다.

### 16.4 UX3 — 숨김 안내와 복구 동선

| 상황 | 안내·동작 |
|---|---|
| 정상 비공개(빈 공개 목록 포함) | `Player에게 비공개 · 현재 3D에서 숨김`. `전체 배치로 보기` 버튼은 soPlayerOnly만 끈다. 선택 칸·대상 유지, 미확정 끌기 취소, 기존 높이/타일 숨김 캐시 갱신 경로 사용 |
| 정의/공개 대상 판정 오류 | 정상 비공개와 별개로 `표시 판정 오류`와 원인을 표시. 패널 유지·잘못된 공개 목록의 명시적 복구 버튼은 FU 후속 구현을 재사용. `위치 보기 (2D)` 또는 `2D에서 수정`을 제공. 전체 보기만으로 오류가 해결되었다고 표시하지 않음 |
| 무작위 시작 지형 | `무작위 시작 · 고정 위치 없음`으로 구분. 가짜 좌표로 점프하지 않고 기존 무작위 지형 패널을 연다. 전체 보기로 바꿔도 고정 외형이 생긴다고 약속하지 않음 |
| 숨은 배치가 든 붙여넣기 | 기존 중단 정책 유지. `전체 배치로 보기`로 전환해도 붙여넣기를 자동 재실행하지 않음. 사용자가 다시 붙여넣기를 실행 |

- 3D 요약에서 `비공개 N / 판정 오류 M`을 중복 없이 구분한다. 무작위 시작은 위치 미정이라는 별도 안내를 함께 두고 고정 외형 제외 수와 혼동하지 않게 한다. 대상 수·개별 안내·높이·타일 숨김·편집 필터는 같은 공개 판정을 소비한다.
- 공개 목록 자체를 자동으로 Player로 바꾸지 않는다. 화면 편집은 기존 표시 대상만, 패널 명시 편집은 숨김도 허용, 충돌 검사는 전체 배치를 유지한다. 타일 보기·타일 회전은 영향을 받지 않는다.

### 16.5 구조·원칙 점검

- SSOT: 배치 규칙은 기존 검사, 공개 판정은 Visuals, Dirty와 선택은 기존 창, 검사 수명은 결과 묶음 한 곳. 새로운 저장 필드·Core 규칙 사본 없음.
- SOLID: 검사 값/대상과 UI 이동을 분리(S); 일반 오류는 위치 없는 값으로 수용(O); 문자열 호환 경로도 같은 메시지·차단 결과 유지(L); 결과는 메시지·위치·대상만 전달(I); 검사 함수는 창/스크롤을 모르고 UI가 값에 의존(D).
- 기능 독립성: 위치 결과 값은 새 `CombatStageSoIssue.cs`, 상태·안내·이동 UI는 새 partial `CombatLayoutWindow.SoUx.cs`(각 `.meta` 포함)를 예정한다. SoMode는 기존 검사·수정·선택 수명에 연결만 한다. 상세 검사 변경은 Assets/PlacementChecks/Validation과 소비자까지 대조하되 토네이도 수식·Core·타일 생성 구현을 다시 고치지 않는다.
- JMH/FW: 새 서비스·Factory·asmdef 의존성 없음. 미리보기 소유자의 갱신·숨김 복원·해제 계약 유지. 표시 오류가 편집 패널을 중단하지 않는 경계 유지. 범위를 넘어서는 API/규칙 변경은 별도 설계 판단 대상이다.

### 16.6 재검토 보완 (2026-10-31)

- UX-a: 판 밖 대상의 직접 편집 패널과 X·Y 일괄 적용, 맵/패널 양쪽 스크롤을 §16.2에 반영했다. 대상 패널은 검사 시점의 Root·개정·대상으로 열고 작업본 변경/일반 칸 선택 때 해제한다. 원인 배치를 패널 맨 위에 강조해 보여 주되 동일 배치를 중복 편집하지 않는다.
- UX-b의 검사 상태 확정은 공통 SO 검사 뒤 맵 규칙 판정까지 완료한 시점이다. 아래 표는 상태 표현만 정하며 기존 저장·등록 허용/거절·보스 교체 정책을 바꾸지 않는다.

| 결과 | 검사 결과·작업 안내 |
|---|---|
| 공통 검사 또는 새 변경의 맵 규칙 위반 | 오류 목록에 포함, 검사 오류. 규칙 오류는 위치 없는 항목 |
| 맵 규칙 조회 실패 | 원인을 오류로 표시, 검사 통과로 확정하지 않음 |
| 보스 교체로만 가능/이미 깨진 맵 | 조건부 경고를 보존. 사용자의 확인이 필요함을 표시하고 무조건 통과로 단순화하지 않음 |
| 확인 창에서 사용자 취소 | 취소 안내. 데이터 오류를 새로 만들지 않으며 저장/등록 완료로 표시하지 않음 |
| 검사 완료 뒤 파일 쓰기 실패 | 검사와 별도 저장 실패. 기존 미저장 표시를 해제하지 않음. 부분 쓰기의 자동 롤백은 이번 UX 범위 밖 |
| 저장 후 표 변경 실패 | 에셋 저장 여부와 표 변경 실패를 각각 표시. 후속 새로고침으로 이전 검사 결과 무효화 |

- UX-c: 현재 보기 필터와 오류 진단을 구분한다. `TryIsShown(marker, true, owners, out problem)`로 Player 공개 여부/입력 오류를 진단하고, 현재 보기 여부는 그 진단을 사용할 표시 정책으로만 적용한다. 새 규칙을 복사하지 않는다. 전체/2D 전환만으로 진단 오류를 해제하지 않으며, 수정·Undo·새로고침 후 재평가한다. 표시 오류는 저장 검사의 차단 결과에 임의 합산하지 않는다. 잘못된 정의의 전체 3D 표시도 편집 패널을 중단하지 않도록 미리보기 오류 경계를 함께 확인한다.

### 16.7 UX-a 코드 반영 (2026-10-31)

검사 결과·대상 값과 위치 이동 partial을 추가하고 기존 검사/생성 메시지 호환, 저장·등록 결과 표시, 2D 맵·대상 패널 이동, 판 밖 좌표 함께 적용, 결과 무효화를 연결했다. 작업본 스키마·Core·런타임·에셋은 변경하지 않았다. UX-b 상태 전환표 및 UX-c 보기 독립 진단은 설계에만 반영된 다음 Phase다. 고지대 보류 유지. 실행·검증 상태 원본은 계획 §12.5다.

### 16.8 UX-b 코드 반영 (2026-10-31)

기존 검사 결과 객체에 검사 단계·조건부 경고를 모으고 상단 작업 상태를 연결했다. 검사 통과는 해당 경로의 맵 규칙 확인까지 끝난 뒤 표시하며, 맵 위반/조회 실패는 오류, 보스 교체/기존 맵 문제는 조건부 경고, 확인 취소/쓰기 실패/저장 후 표 실패는 작업 안내로 구분한다. 기존 저장/등록 정책과 Dirty·초안·Undo 경로는 유지한다. 실행·검증 기록은 계획 §12.6이 원본이다. **컴파일·Unity 미확인, UX-c 미착수, 고지대 B1 보류 유지.**

### 16.9 UX-c 코드·문서 반영 (2026-11-02)

속성 상단 `공개·숨김 안내`가 보기와 무관하게 Visuals의 Player 판정을 읽어 정상 비공개/판정 오류를 구분하고, 무작위 시작 지형 수를 별도 표시한다. 전체/2D/타일/설정에서도 오류 진단을 유지하며 저장 검사 결과에 합산하지 않는다. 고정 오류는 기존 층/인덱스 대상·Root/개정 검증과 UX-a 위치/판 밖 복구를 공유한다. 무작위 지형은 가짜 좌표 없이 기존 패널을 연다.

전체/2D 복구와 Player 토글은 공통 입력 취소를 사용하고 선택·클립보드·시점·검사 상태를 보존한다. 숨김 때문에 중단된 붙여넣기에 전환 버튼을 제공하되 자동 재실행하지 않는다. 기존 FU 명시 편집과 Preview 갱신·오류 경계를 유지한다. 사용법·View/민형님 공유 메모를 갱신했으며 공유는 **미전달**이다. 실행·정적 확인 원본은 계획 §12.9. **컴파일·Unity 미확인, UXR-c/d 미착수, 고지대 B1 보류 유지.**

## 17 낙석 값 패널·터널 자리 경고 (RF1~RF6)

- 상태(2026-11-02): **RF-a~RF-c 완료(Q-RF1~Q-RF3 A, RF-b Unity 확인 문제 없음 — 사용자, 새 시험 없음, 커밋 전).** 사용자 "낙석 패널·경고부터". 하붑 값은 범위 밖이다(하붑 View 계획과 함께).
- 목적: `CombatStageData.rockfall`을 도구에서 보고 고치고, 전투에서야 드러나는 터널 자리 문제를 저장 전에 알린다.
- 범위 밖: 하붑, 경고 칸 미리보기(칸은 전투 중 무작위), 터널 자리를 3D에 표시하거나 도구에서 옮기기, 낙석 칸 후보 수 계산.

### 17.1 확인한 사실

| 사실 | 근거 |
|---|---|
| 값 8개: 켜기, 쿨타임·경고 턴, 돌 수 최소·최대, 최대 체력 손실 %, 묶임 턴, 칸 오프셋 | `RockfallGimmickAuthoring` |
| 잘못된 값은 저장 검사가 이미 잡는다. 복제본 `GetData()` → `rockfall.ToData()` → `RockfallGimmickSpec` 생성자가 예외를 던진다. 꺼져 있으면 `ToData`가 null이라 검사하지 않는다 | `RockfallGimmickData`, `CombatStageSoValidation` |
| 오프셋 규칙: 비어 있으면 안 됨, 음수·중복 금지, 각 축 최솟값 0 | `StageGimmickMask.CopyNormalized`, `StageGimmickState.CopyCells` |
| 오프셋이 판보다 크거나 후보 칸이 없으면 예외 없이 경고 칸이 줄거나 0개가 된다 | `RockfallGimmickRule` |
| 터널 찾기는 프랍 아래 비활성 포함 이름 일치. 없으면 폭발 없이 경고 칸만 켜고(RP-12), 둘이면 전투 진입 때 예외. 프리팹 프랍을 세울 때마다 찾으므로 낙석이 꺼져 있어도 둘이면 멈춘다 | `StageViewPropPresenter.FindRockfallTunnel` |
| 지금 `Test`(켜짐)·`Stage_Test2`(꺼짐) 모두 `NewStageTest.prefab`을 쓰고 터널이 하나 있다. 지금 데이터에서는 경고가 나오지 않는다 | 에셋 대조 |
| 새로 만들기·복제는 틀의 전투 데이터를 복사하므로 낙석 값도 따라온다. 코드 변경이 필요 없다 | `CombatStageSoCreation` |
| 작업본 형식은 지금 3이다. 작업본이 낙석 값을 모른 채 저장되면 안 되므로 형식을 올려야 한다 | `CombatStageSoDocument.CurrentFormat` |

### 17.2 결정(안)

| 번호 | 내용 | 이유 |
|---|---|---|
| RF1 | 작업본에 값 묶음 `CombatStageSoRockfall`(값 7 + `List<Vector2Int>` 오프셋)을 더하고 형식을 4로 올린다. 옛 형식 초안은 지금 규칙대로 에셋에서 다시 읽는다 | 레일 값과 같은 길. 안 올리면 옛 초안 저장이 기본값으로 낙석을 덮어쓴다 |
| RF2 | 읽기·쓰기는 `CombatStageSoAssets.Read`·`Write`에 이름 상수로 더한다. SO 코드는 고치지 않는다 | 설계 SM 필드 이름 방식 |
| RF3 | 패널은 '스테이지 값'의 레일 줄 아래. "낙석 사용" 토글, 그 아래 값은 꺼지면 회색(값은 유지) | 끈 스테이지의 값을 지우지 않는다 |
| RF4 | 값 규칙을 도구에 베끼지 않는다. 입력은 그대로 받고 저장 검사가 Core 규칙으로 막는다 | SSOT. 사본을 두면 Core가 바뀔 때 어긋난다 |
| RF5 | 터널 검사는 `CombatStageSoValidation`의 한 함수. 프랍 프리팹 아래 Transform을 비활성 포함으로 세어 둘 이상이면 저장을 막는 문제(낙석 켜짐과 무관), 0개이면서 낙석이 켜져 있으면 막지 않는 조건부 경고 | 게임 결과와 같게: 둘은 멈춤, 없음은 허용된 동작(RP-12) |
| RF6 | 오브젝트 이름 `RockfallTunnelBoom`은 게임 쪽이 private 상수라 도구에 사본 한 줄을 두고 원본 자리를 주석으로 적는다 | 도구의 다른 게임 식 사본과 같은 규칙. 전투 View 사본 공유 메모 표에 한 줄 더한다 |

### 17.3 정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-RF1 | 칸 오프셋을 어떻게 고치나 | **A 줄 목록**: x·y 입력과 추가·지우기. 지금 값이 모두 (0,0) 하나라 이걸로 충분하고 가장 단순하다 | B 작은 격자를 눌러 칠함(모양은 잘 보이지만 코드가 늘고 축 최솟값 0 맞추기도 필요) |
| Q-RF2 | 저장 검사의 영어 예외 문구(`minimumRockCount` 등)를 어떻게 하나 | **A 문구 앞에 "낙석 값:"만 붙임**: 규칙은 Core 그대로이고 어느 값인지는 필드 이름으로 안다 | B 값마다 한국어 문구(규칙 사본이 생김) |
| Q-RF3 | 맵 SO 방식(`MapDefinition`) 스테이지의 터널 검사 | **A 검사하지 않고 패널에 "맵 SO 방식은 터널 자리를 확인하지 않습니다" 한 줄**: 지금 쓰는 스테이지가 없다. 맵 SO는 조립해 봐야 알 수 있다 | B 맵 SO 안의 프리팹 참조까지 따라가 검사 |

### 17.4 원칙 점검

- SSOT: 값 규칙은 Core 하나. 사본은 오브젝트 이름 한 줄이고, 어긋나면 경고가 틀리는 약점이 남는다(RF6).
- SOLID: 패널·입출력·검사가 기존 책임 자리에 들어가고 새 클래스는 값 묶음 하나다.
- FW 계약: 런타임·Factory 무변경.
- 독립성: 도구 파일 셋과 사용법만 바뀐다. 다른 작업자 파일과 에셋은 고치지 않는다.

### 17.5 Phase(안)

RF-a 값 입출력·패널(`CombatStageSoDocument`·`CombatStageSoAssets`·`CombatLayoutWindow.SoMode`) → RF-b 터널 검사(`CombatStageSoValidation`) → RF-c 사용법·공유 메모. 컴파일·재생은 사용자 Unity 확인이다.

### 17.6 구현 메모 (2026-11-02)

설계와 다른 점은 둘이다. 터널 수 세기와 이름 사본은 필드 이름 상수가 있는 `CombatStageSoAssets`에 두었고, 둘 이상 검사는 작업본을 받는 `CheckDocumentIssues`에 넣어 저장 전 프리팹 변경도 반영한다. "낙석 값:" 머리말은 계획 P-RF1대로 복제본의 낙석만 꺼서 만든다. 터널 없음 경고는 검사·저장 흐름에서만 내고, 저장 성공 뒤 화면에 남는지는 확인하지 않았다.

## 18 본부 지우기 (2026-11-02)

- 사용자 "본부를 Delete할 수 있게". 코드 반영, 컴파일 미확인, 새 시험 없음. 옛 규칙 "본부는 옮기기만"을 대신한다.
- 게임 근거: `BattleDefinitionAdapter`가 정의가 없으면 본부를 두지 않는다. 보스 격파 목표(`Test`·`Stage_Test2`)는 본부가 없어도 된다.
- 지우기 = 본부 정의만 비움. 자리·회전은 남아 정의를 다시 고르면 돌아온다. Delete·상자 지우기·지우기 도구·"이 배치 지우기"가 모두 같다.
- 본부 파괴 목표인데 본부가 없으면 저장을 막는다. 게임은 멈추지 않고 이길 수 없는 전투가 되기 때문이다(`BattleOutcomeCalculator`).

## 19 '타일' 보기 고르기·복사·붙여넣기 (TC1~TC6)

- 상태(2026-11-02): **TC-a 코드 완료(사용자 "모두 권장안 A, 진행" = Q-TC1~Q-TC3 A, 컴파일 미확인, 새 시험 없음, Unity 확인 대기, 커밋 전).** 사용자 "타일 칠하는 곳에 선택도 넣어 주면, 복사해서 다른 스테이지에 붙여넣을 수 있을 것 같다".
- 범위: '타일' 보기 안의 타일 배치(종류·회전). 범위 밖: 타일 세트·가로·세로·크기·위치·기준 타일 복사, 2D·3D '타일 회전' 도구의 복사, 작업본 밖(파일) 내보내기, 좌우 뒤집기.

### 19.1 확인한 사실

| 사실 | 근거 |
|---|---|
| 고르기는 이미 있다. '회전' 모드의 `soTileSelection`이 클릭·끌기·Ctrl·Shift·Ctrl+A·Esc를 처리한다. 값은 배열 칸(column, row)이다 | `CombatLayoutWindow.SoTiles.cs` |
| 배치 복사 `soClip`은 창이 들고 있어 스테이지를 바꿔도 남는다. 다른 스테이지 붙여넣기의 선례다 | `CombatLayoutWindow.SoMode.cs` |
| 고른 타일은 스테이지·가로·세로·세트가 바뀌면 비워진다. 복사본은 고르기와 따로 두어야 한다 | `SyncSoTileSelection` |
| 저장된 회전 값은 배열 기준이고, 배경 회전이 다른 스테이지에서는 같은 값이 화면에서 다른 각으로 보인다. 화면 각 변환은 `fit.ScreenAngleOf`·`fit.StoredOf`가 이미 한다 | `CombatStageSoTileFit` |
| 화면과 배열의 대응도 스테이지마다 다를 수 있다(`fit.ScreenToArray`). 복사 자리는 화면 기준이어야 눈에 보이는 모양이 유지된다 | 같음 |
| 종류 값은 세트에 없으면 저장 검사가 막는다. 가로 ≠ 세로 타일의 90°·270°도 막는다 | 설계 §13, 사용법 §8.2 |

### 19.2 결정(안)

| 번호 | 내용 | 이유 |
|---|---|---|
| TC1 | 복사 값 `CombatStageSoTileClip`: 칸마다 화면 상대 자리(고른 묶음의 왼쪽 아래 기준)·종류·회전. 회전은 화면 각(0~3) 또는 Random. 창이 `[SerializeField]`로 들고 있어 스테이지를 바꿔도 남고 Undo와 무관하다 | `soClip`과 같은 수명. 배경 회전이 다른 스테이지에서도 보이는 모양이 같다 |
| TC2 | Ctrl+C·복사 버튼: 고른 타일을 담는다. 고른 칸만 담아 떨어진 모양도 유지한다 | 사각형으로 넓히면 고르지 않은 칸을 덮는다 |
| TC3 | Ctrl+V·붙여넣기 버튼: **고른 타일의 왼쪽 아래**를 기준으로 붙인다. 고른 타일이 없으면 안내만 한다. 다 붙인 뒤 붙인 타일을 고른 상태로 둔다. 한 번 = Undo 한 번 | 배치 붙여넣기의 "붙일 칸(왼쪽 아래 기준)"과 같다. 전체 복사는 Ctrl+A → Ctrl+C, 다른 스테이지에서 왼쪽 아래 타일을 고르고 Ctrl+V |
| TC4 | 대상 세트에 없는 종류는 그 칸만 건너뛰고 알림에 수를 적는다. 가로 ≠ 세로 타일에 90°·270°가 들어오면 그 칸의 회전만 0°로 두고 수를 적는다 | 저장에서 막힐 값을 만들지 않는다. 규칙 사본이 아니라 지금 세트와 크기를 읽어 판단한다 |
| TC5 | 바뀌는 칸만 쓴다. 회전 목록이 빈 스테이지는 회전이 전부 0°면 목록을 만들지 않는다 | 에셋 차이를 줄인다 |
| TC6 | 복사·붙여넣기 키는 `HandleSoTileKeys`에서 받고 고른 타일이 있는 모드에서만 듣는다. 입력 칸에 커서가 있으면 받지 않는다 | 기존 Copy·Paste 명령 조건과 같다 |

### 19.3 정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-TC1 | 고르기를 어디에 넣나 | **A '회전' 모드를 '선택'으로 넓힘**: 같은 고르기로 돌리기·복사·붙여넣기를 다 함. 모드 수가 그대로이고 코드가 가장 적다 | B '선택' 모드를 새로 둠(회전 줄과 나뉘지만 고르기 갈래가 둘로 늘어남) |
| Q-TC2 | 붙일 때 타일 맵 밖으로 넘치는 칸 | **A 넘치는 칸만 버리고 알림에 수를 적음**: 작은 맵으로 옮길 때 그대로 쓸 수 있다 | B 하나라도 넘치면 붙이지 않음 |
| Q-TC3 | 회전도 함께 복사하나 | **A 함께(화면에서 본 각 유지, Random은 Random)** | B 종류만 복사 |

### 19.4 원칙 점검

- SSOT: 화면 ↔ 배열·각 변환은 있는 `CombatStageSoTileFit`를 쓰고, 복사 값은 작업본에 들어가지 않는다.
- SOLID: 복사 값 클래스 하나와 창의 함수 둘(복사·붙여넣기)만 더한다.
- FW 계약·게임 파일: 무변경. 도구 파일 둘과 사용법만 바뀐다.
- 약점: 고른 묶음의 왼쪽 아래가 기준이라, 떨어진 칸을 여럿 고른 채 붙이면 기준이 직관과 다를 수 있다. 붙인 뒤 Undo로 되돌리면 된다.

### 19.5 Phase(안)

TC-a 복사 값·복사·붙여넣기·버튼·키 → 사용법 §5.10 한 절. 한 Phase로 끝낸다. 컴파일·재생은 사용자 Unity 확인이다.

### 19.6 구현 메모 (2026-11-02)

바꾼 파일은 `CombatStageSoTiles.cs`(복사 값)·`CombatLayoutWindow.SoTiles.cs`(버튼·키·복사·붙여넣기)와 사용법이다. 설계와 다른 점은 없다. 내부 상수 이름 `SoTileRotate`는 2D·3D 도구 코드와 함께 쓰여 그대로 두고 표시 글자만 '선택'으로 바꿨다. 복사 버튼 줄은 '타일' 보기에만 있고 2D·3D '타일 회전' 도구에는 없다.

## 20 정의 목록에서 끌어 놓기 (2026-11-02)

- 사용자 "정의 목록에서 끌어당겨서 맵에 배치". 코드 반영, 컴파일 미확인, 새 시험 없음, Unity 확인 대기, 커밋 전.
- Unity `DragAndDrop`을 쓰고 끄는 값은 도구·정의 GUID다(에셋 참조를 넣지 않아 다른 창에는 놓이지 않음). 움직임 6픽셀 미만이면 클릭이다.
- 놓는 판단은 클릭과 같은 `HandleSoCell` 한 곳, 고스트 판단도 같은 `PlacementError`다. 맵 입력은 2D·3D가 함께 쓰는 `HandleSoPointer`, 고스트는 `SoFrames`에 넣어 두 보기에 같이 나온다.
- 놓은 뒤 그 정의가 붓으로 남는다. 맵 밖 놓기·취소는 붓을 바꾸지 않는다. 아군 유닛 목록은 그대로 끌 수 없다.

## 21 확장 UX 시안 적용 — 기존 기능 보존 (UXR)

### 21.1 기준과 범위

- 기준 시안: 이 대화에서 사용자가 선택한 **전투 스테이지 UX 확장 시안 `combat-stage-studio-v2.html`**. 채택 대상은 `배치 / 바닥 타일 / 스테이지 설정`, 상황별 속성, 공통 검사·저장 상태, 빠른 사용법·기능 찾기다. HTML·예시 데이터·검사 함수를 Unity로 이식하지 않는다. 시안 파일이 없는 작업자도 아래 대응표와 계약으로 구현할 수 있어야 한다.
- 구현 방식은 **현재 EditorWindow·IMGUI 안에서 화면을 재구성하고 기존 명령을 호출**하는 것으로 정한다. 접기 항목만 계속 늘리는 방식은 작업별 동선 개선이 작고, 새 웹 도구나 UI Toolkit 전면 교체는 입력·저장·3D 수명을 다시 연결해야 하므로 이번 범위에 넣지 않는다. 이 구현 방식은 기존 코드에 맞춘 설계 결정이다.
- 실제 동작 원본은 현재 Tool 코드와 이 문서의 기능별 계약이다. 특히 §18 본부 삭제, §19 타일 복사, §20 정의 끌어 놓기와 §15~§17·계획 §12.7~§12.8 후속 기능을 보존한다. **기능을 없애거나 새 저장 차단 규칙을 만드는 재설계가 아니다.**
- 최초 요청에서는 설계와 계획 §14만 작성했다. 이후 각 사용자 요청으로 **UXR-a·b·c 코드와 UXR-d 문서·정적 대조를 순서대로 반영**했다. 기존 기능의 확인 기록, 코드 반영, 사용자 Unity 확인을 구분한다. 다음 단계 요청을 이전 단계의 Unity 확인 완료로 기록하지 않는다. UXR-d 결과와 남은 안내 불일치·확인은 계획 §20.9를 따른다.
- 고지대 B1 이웃 모양 개선은 계속 보류한다. 기존 고지대 배치·기본 외형·높이·외형 회전 제한과 Core 규칙은 그대로 유지한다. 하붑 편집은 UXR와 별도 승인된 §22 HBE-a에서 추가했다. 스테이지 에셋 완전 삭제, 실제 전투 시뮬레이션, DT 기능 변경은 범위 밖이다.

### 21.2 기존 기능 → 새 화면 대응표 (누락 확인 원본)

아래 UXR-F 번호가 기능 보존 목록의 원본이다. 계획은 번호를 인수하며 같은 목록을 다시 정의하지 않는다. `Tool/Editor/`의 파일 이름은 접두 경로를 생략했다. 화면에서 접거나 다른 영역으로 옮겨도 접근 경로와 편집 능력은 남긴다.

| ID | 보존할 기능·세부 항목 | 새 화면 위치 | 기존 연결점 / 원본 |
|---|---|---|---|
| UXR-F01 | DT/SO 모드, 전투 스테이지 목록·이름/키/맵/역할/등록 상태, 열기, 목록 새로고침, 에셋에서 다시 읽기 | 공통 머리줄·스테이지 목록 펼침 | `DrawSoStageList`, `OpenSoStage`, `RefreshSoCatalog`, `ReloadSoFromAssets`; SM1 |
| UXR-F02 | 틀 선택, 이름·키 제안·중복 방지, 배치 비우고 만들기/배치까지 복제, 호환 배치 처리, 미등록 생성 | 공통 `새로 만들기 / 복제` 영역 | `DrawSoCreate`, `CombatStageSoCreation`; SM4·FU2 |
| UXR-F03 | 짝 View 없음 안내·틀의 View로 만들기, 같은 키 View 중복 거절, 한 벌 참조 유지 | 설정 → 배경·View, 열기/저장 오류 안내 | `DrawSoView`, `OpenSoStage`; SM2·계획 P12 |
| UXR-F04 | 한국어 이름·에셋 이름, 적 자격별 목록, 아군 유닛 읽기 전용, 붓 선택, 정의 목록에서 2D/3D로 끌어 놓기·고스트·취소 | 배치 → 왼쪽 정의 팔레트 | `DrawSoPalette`, `DrawSoPlayerUnits`, `HandleSoPaletteDrag`; §20 |
| UXR-F05 | 선택·적 유닛·구조물·본부·지형·장애물·오브젝트·레일·지우기·아군 구역·타일 회전, 후속 GP의 하붑 시작·낙석, 클릭/끌기/사각형 칠하기, 층별 즉시 막기 | 배치 → 도구/팔레트·맵 | `SoTool`, `HandleSoPointer`, `HandleSoCell`, Document 편집 연산; AD3·GP |
| UXR-F06 | 한 칸의 여러 배치, X·Y 함께 적용, 정의·방향·회전된 여러 칸 점유, 적 격파 목표·시작 아이템 | 배치 → 선택 속성 | `DrawSoCell`, `DrawSoPlaced`, `DrawSoPlacement`, `DrawSoUnit`; UX1 |
| UXR-F07 | 단일/상자 선택·끌어 이동, 묶음 이동 전체 거절, 복사·다른 스테이지 붙여넣기, 좌우 뒤집기·좌우 대칭 복사, 건너뛴 수·Undo | 배치 → 선택 도구·편집 명령 줄 | `CopySo`, `PasteSo`, `FlipSo`, Document `MirrorCopy`/`MoveGroup`; AD3·AD4 |
| UXR-F08 | 본부 이동·회전·삭제·정의 재지정 복구, 복사 제외, 승리 조건별 본부 필요 여부 | 배치 → 본부·선택 속성 / 설정 → 본부 정의 | Document `Remove`, `DrawSoDeleteButton`; **§18이 옛 삭제 금지보다 우선** |
| UXR-F09 | 공개 대상 전체 열거, 새 지형·오브젝트 Player+Enemy 기본값, 기존/복사 공개 값 보존, 자동 sourceId·View 회전 짝 | 선택 속성 → 공개 대상·외형 | `DrawSoOwners`, Document `Add`/`NextSourceId`; §15·계획 §12.8 |
| UXR-F10 | 토네이도 무작위 시작·좌표 없는 목록, 가로 방향(자동/왼쪽/오른쪽), 중심·크기·바탕 높이, 외형 회전 금지 | 배치 → 선택 속성·`무작위 지형` 목록 | `DrawSoMarker`, `DrawSoViewRotation`, `CombatStageSoVisuals`·Validation; FU3·현재 토네이도 입력 계약 |
| UXR-F11 | 아군 구역 칠하기/지우기·사각형·마지막 칸 보호, 칸 목록 비우기·필드 줄 수 복귀, 공유 필드 전용 복제, 참고 수·경고 ①~④ | 배치 → 아군 구역 / 설정 → 필드·아군 구역 | `DrawSoPlayerZone`, Document `PaintZone`; AZ1~AZ9 |
| UXR-F12 | 2D/실제 프리팹 3D 편집, 카메라 회전·이동·휠·초기화, 맵/객체 높이·바닥 교체, 외형 누락 상자, 토네이도 보정, 미리보기 해제 | 배치 → 2D/3D | `DrawSoMap2D/3D`, `CombatStageSoPreview`·Visuals; AD1·UT·FU |
| UXR-F13 | 기본 전체 배치/Player 기준, 숨김 대상의 충돌 유지·묶음 편집 제한·개별 패널 복구, 판정 오류에서도 편집 가능 | 3D 보기 기준·선택 속성·숨김 안내 | `SoPlayerFilter`, `SoShown`, `TryIsShown`; FU5·UX3 |
| UXR-F14 | 공격 범위 끄기/고른 적/모든 적 겹쳐 보기, 전체/네 방향, 사거리·스플래시·겹침 수, 실제 전투와의 차이 안내 | 배치 → 공격 범위 펼침·범례 | `DrawSoRange`, `DrawSoDirectionButton`, `CombatStageSoRange`; AD2·FU6 |
| UXR-F15 | 키·맵 키·역할·시드·승리 조건·적 레벨·벽 모서리 값, 적 본부/구조물 목록 참조 | 설정 → 기본 정보·참조 | `DrawSoStageValues`, `DrawSoReferences` |
| UXR-F16 | 필드·로스터·경제·적 로드아웃의 참조 선택/현재 SO 전체 편집 필드·공유 사용처·읽기 전용/전용 복제, 새 필드의 크기·대기 칸·아군 줄 수, 키 수정 금지 | 설정 → 공유 SO·필드/경제/로스터/로드아웃 | `DrawSoReference`, `DrawSoSharedEditor`, `DrawSoNewField`; SM7. 본부·카메라 정의는 읽기 전용 |
| UXR-F17 | 프리팹/맵 SO/프리팹+바닥 타일, 비활성 방식 값 보존, 위치·회전·배율·카메라 참조, 짝 View 생성 | 설정 → 배경·View; 바닥 타일에서 바로 열기 | `DrawSoView`, `DrawSoBackground`; UT1~UT3 |
| UXR-F18 | TileSet 참조·Inspector 열기, 종류별 후보/공유 높이/사용 칸 수, 타일 수 1~128·소수 크기·원점·기준 타일, 타일 만들기·맞춤 경고 | 바닥 타일 → 속성 / 설정 → 배경·View의 동일 패널 | `DrawSoTileLayout`, `DrawSoTileKinds`, `DrawSoTileFitMessages`; UT |
| UXR-F19 | 타일 칠하기/선택/기준 타일, 단일·끌기·Shift 사각형·전체 칠하기, 화면↔배열 변환, 필드/기준 타일 경계·앞쪽 표시 | 바닥 타일 → 전용 맵·도구 | `DrawSoTileView`, `HandleSoTilePointer`, `CombatStageSoTileFit` |
| UXR-F20 | 타일 선택 Ctrl/Shift 조합·전체 선택·해제, 고정 각/90°/Random, 직사각 타일 제한, 2D 판 안·3D 판 밖 회전 도구와 선택 공유 | 바닥 타일 → 선택·회전 / 배치 → `타일 회전` | `DrawSoTileRotateBar`, `HandleSoTileToolKeys`; TT·TO |
| UXR-F21 | 선택 타일만 종류·화면 각 복사, 다른 스테이지에 선택 위치 기준 붙여넣기, 대상 종류/범위 밖 건너뜀·직사각 각 보정, 별도 클립보드 | 바닥 타일 → 선택·복사/붙여넣기 | `DrawSoTileClipBar`, `CopySoTiles`, `PasteSoTiles`; TC1~TC6 |
| UXR-F22 | 프리팹 후보·Random 각 예시, 명시적 다시 뽑기, 카메라/보기 전환 중 난수 유지, 실제 게임 난수와 구분 | 바닥 타일/3D → 랜덤 미리보기 다시 뽑기 | `DrawSoTileRandomPreviewButton`, `_soTilePreviewSeed`; 현행 SoTiles |
| UXR-F23 | 레일 사용·칸·첫 발동/반복, 낙석 사용·대기/경고·돌 수·손실 %·묶임·오프셋, 터널 없음 경고/중복 오류/맵 SO 검사 제외. 하붑 설정·모양·시작 위치와 낙석 터널 폭발 선택은 후속 HBE/GSE/RTE/HAT/GP 계약을 따른다 | 배치 → 레일·하붑 시작·낙석 / 설정 → 기믹 | `DrawSoRockfall`, `DrawSoRockfallFootprint`, `SoHaboob.DrawSoHaboob`, Assets/Validation; RF1~RF6·§22~26 |
| UXR-F24 | 작업본→검사→공유 편집본/Root/Combat/View 쓰기, 오류·조건부 경고·쓰기 실패·부분 실패, 미저장/검사 개정·위치 이동·판 밖 수정 | 공통 검사·저장·상태 / 검사 결과 패널 | `CheckSoStage`, `SaveSoStage`, `CombatLayoutWindow.SoUx`, `CombatStageSoIssue`; SM2·UX1/UX2 |
| UXR-F25 | 등록/빼기, 두 표 동시 처리, 맵 생성 줄·가중치/최소 수 참고·새 위반/기존 위반 구분, 보스 교체, PlacedStage 영향 | 설정 → 등록·맵 생성 규칙 | `SetSoRegistered`, `DrawSoChapterLine`, `CombatStageSoChapter`; MG |
| UXR-F26 | Unity Undo/Redo·입력 포커스 보호, 이 PC 마지막 초안·형식 호환, 열기/다시 읽기 버리기 확인, 패널 폭/스크롤, DP 원문·충돌 보호와 내보내기/재시도 | 공통 작업 관리·상태 / 각 영역 | `SoChange`, `SaveSoDraft`, `RestoreSoDraft`, `AllowSoReplace`, `HandleSoKeys`, SoDrafts/DraftStore; SM11·UX2·§27·계획 §12.7 |
| UXR-F27 | 현재 도구 빠른 사용법, 기능 위치 찾기, 검사→저장→등록 안내, 실패 원인과 복구 경로, Unity에서 확인할 한계 | 공통 `사용법`·도구별 사용법·등록 사용법 → 창 내부 여섯 주제 → 상세 사용법/기능 위치 | UXR-c 코드 반영(계획 §20.8), 컴파일·Unity 미확인; §21.5 |

### 21.3 화면과 상태 전환

**공통 머리줄**에 DT/SO, 스테이지 선택(이름·키·등록 상태), 새로 만들기/복제, 목록 새로고침, 에셋에서 다시 읽기, 검사, 저장, 사용법을 둔다. 이름만 있는 드롭다운으로 현재 목록의 맵·역할·미등록 정보를 없애지 않는다. 새로 만들기 영역에는 기존 틀·키 제안·두 생성 방식을 모두 둔다. 미저장 상태와 검사 상태는 모든 작업 영역에서 읽을 수 있다.

`배치 / 바닥 타일 / 스테이지 설정`은 자유롭게 오가는 작업 영역이다. 순서대로 통과해야 하는 마법사나 검사 통과를 요구하는 단계가 아니다.

| 영역 | 왼쪽 | 가운데 | 속성·보조 영역 |
|---|---|---|---|
| 배치 | 층별 정의 팔레트·아군 읽기 목록 | 2D/3D·도구·선택 명령·공격 범위·범례·맵 | 선택한 칸/배치, 아군 구역, 위치 없는 지형. 겹친 대상 목록에서 하나를 명확히 고름 |
| 바닥 타일 | 현재 TileSet 종류 | 칠하기/선택/기준 타일·선택 회전/복사·타일 맵 | 타일 구성·선택 정보·맞춤 경고. `2D/3D에서 타일 회전`으로 기존 도구에 이동 가능 |
| 스테이지 설정 | 기본 정보/필드·아군 구역/공유 SO/배경·View/기믹/등록 분류 | 선택한 설정 분류 | 필요한 참조·주의·현재 값. 같은 값을 두 군데에서 동시에 편집하지 않음 |

- 검사 결과는 공통 펼침 영역으로 옮기되 `CombatStageSoCheckResult` 한 개를 표시한다. UX-a `위치 보기 (2D)`는 배치 영역·2D·대상 속성을 함께 연다. 위치 없는 오류는 임의 좌표나 메시지 문자열 추측으로 이동하지 않는다. 현재 대상 계약으로 분류를 특정할 수 없으면 원문과 설정 분류 선택을 제공한다.
- 설정 진입은 마지막 지도 보기·선택·두 클립보드를 보존하며 지도 입력을 받지 않는다. 바닥 타일 진입은 기존 `soMapView = SoViewTiles`, 배치 복귀는 마지막 2D/3D 보기로 연결한다. 표시 상태만 바꿀 때 Dirty·초안·검사 개정·Undo를 만들지 않는다. 타일 선택 초기화 조건(스테이지·타일 수·세트 변경)은 기존 그대로다.
- 영역/보기 전환 직전 미확정 이동·상자·정의 DragAndDrop·타일 사각형 고스트를 취소하고 hotControl을 해제한다. 이미 적용된 연속 칠하기는 기존 `EndStroke`로 Undo 묶음을 닫으며, 보기 전환을 이유로 추가 편집하거나 적용분을 임의 복원하지 않는다. 취소 후 같은 입력이 다른 영역에 전달되지 않는다.
- 키 처리는 한 번만 분배한다. 숫자/텍스트 입력 중 Unity 기본 복사를 보호하고, 지도 유효 클릭 시 포커스 해제 경로를 유지한다. 설정/도움말에서는 숨은 배치·타일에 Delete/R/Ctrl+C/V가 전달되지 않는다. 타일 보기와 2D/3D 타일 회전 도구의 기존 분기를 유지한다.
- 3D 시점·미리보기·자원 정리는 기존 소유자에 남긴다. 설정 화면을 보여 준다는 이유만으로 새 Preview를 만들거나 시점을 초기화하지 않는다. 표시 오류·참조 누락 때문에 지도 렌더링을 못 해도 공통 버튼과 복구용 속성 패널은 남긴다.
- 기존 창 최소 크기 **1100×650**와 패널 경계 끌기를 유지한다(DT 공용 설정 변경 없음). 이 크기와 큰 창에서 버튼 줄을 나누고 각 영역을 스크롤할 수 있어야 한다. 웹 시안의 모바일 크기·CSS·색·픽셀 치수는 이식 요구가 아니다. Unity 밝은/어두운 테마에서 상태를 글자로도 구분한다.

### 21.4 시안과 실제 도구가 다른 부분 — 기존 계약 우선

| 시안의 단순화·예시 | 실제 적용 기준 |
|---|---|
| 적 본부를 항상 하나 남기고 삭제 금지 | §18대로 삭제 가능. 정의만 비우고 자리·회전 보존, 보스 격파면 본부 없이 가능, 본부 파괴면 본부 없을 때 저장 거절 |
| 복사 후 Ctrl+V → 나중에 클릭, 선택 묶음 중심으로 반전 | 기존 배치는 **목적지 칸을 고르고 붙여넣기**, 반전/대칭은 **맵 가운데 세로선** 기준·상자 없으면 전체. 묶음 이동/뒤집기는 기존 전체 거절, 붙여넣기/대칭 복사는 기존 건너뜀 정책 |
| 타일 붙일 위치를 추가 클릭으로 지정 | §19대로 선택 타일들의 왼쪽 아래가 목적지. 고른 칸만 복사하고 화면 각을 변환. 2D/3D 타일 회전 도구에 타일 복사를 새로 추가하지 않음 |
| 지형·오브젝트 sourceId를 직접 수정, 모든 대상 회전 | sourceId 자동 발급·재매핑, 고지대/토네이도 외형 회전 금지 유지. 토네이도 가로 방향·무작위 시작은 현재 패널 전체를 옮김 |
| 아군 구역 마지막 칸을 지우면 줄 수로 복귀 | 마지막 칸 지우기 거절, 명시적 `칸 목록 비우기` 확인으로만 줄 수 방식에 복귀(AZ) |
| 고정 적의 아군 구역 점유·적 로드아웃 병용을 저장 오류로 취급 | 현재는 참고 경고/전투 진입 검사인 항목이다. UX 변경으로 경고를 저장 차단 오류로 승격하지 않음. 적 로드아웃을 모든 스테이지의 필수 참조로 만들지 않음 |
| 낙석 오프셋에 (0,0) 필수, 일부 턴 0 허용 | 현행 Core 값 검사 재사용: 각 축 최솟값 0 등의 규칙을 UI에 재구현하지 않음. 터널 수는 실제 프리팹에서 검사하며 숫자 입력으로 조작하지 않음 |
| 일반 후보 최소 1개·보스 최대 1개로 고정 | MG의 실제 맵 SO 계약·보스 정확히 1개·종류별 가중치/최소 수·기존 위반 허용·보스 교체를 그대로 사용 |
| 공유 사용처 2개, 예시 경제·유닛·TileSet 목록 | 실제 Catalog·공유 사용처·SerializedProperty 전체 필드를 사용. 예시 몇 칸으로 축소하지 않음. 필드 표현 설정은 실제 필드 View SO의 책임을 유지하며 Stage View에 새 필드로 저장하지 않음 |
| 필드 24칸·타일 표시 20칸 등 시안 상한 | 현재 필드 입력/검사 범위와 타일 1~128·스크롤을 유지. 시안의 제한은 이식하지 않음 |
| 3D 구성 도형·거리만 계산한 공격 범위 | 기존 실제 프리팹 Preview·Core 공격 모양·전체 적/방향/스플래시/겹침 표시를 재사용. 전투 판정 완전 재현으로 안내하지 않음 |
| V/B/E, 배치 Shift+R 등 예시 단축키 | 현행 `HandleSoKeys`·타일 키 처리에 있는 명령만 안내. 새 단축키 추가는 이번 재배치의 전제가 아님 |
| 여러 스테이지별 웹 임시본·수동 저장/복원 버튼 | 마지막 SO 초안 자동 복원을 유지. §27 DP-a에서 복원 불가 원문 보호 슬롯 하나만 추가 예정이며 다중 편집 초안·버전 기록·자동 SO 저장·정상 전환마다 자동 백업은 도입하지 않음 |
| 해제 시 PlacedStage 전용 참조를 별도로 지움 | 실제로는 Root/View 표에서 제외해 PlacedStage 진입이 불가능해지는 효과. 별도 참조 삭제나 에셋 삭제를 추가하지 않음 |

### 21.5 사용법·저장 안내와 오류 복구

2026-11-09 구현: 초안 형식 불일치 때의 자동 폐기/다시 읽기는 DP-a에서 §27.2 보호 정책으로 교체했다. UXR-c는 TXT-a와 DP-a를 인수해 아래 도움말을 코드에 반영했다(계획 §20.8, 컴파일·Unity 미확인). 정상 작업본의 명시적 버리기/취소는 유지한다. 전체 문서/공유 메모는 UXR-d에서 정리했으며 결과와 남은 확인은 계획 §20.9를 따른다.

- `사용법`은 작업 영역과 관계없이 열린다. `처음 만들기`, `배치 복사`, `바닥 타일`, `공유 SO`, `검사·저장·등록`, `안 될 때`를 고를 수 있게 하고, 해당 작업 영역/속성 열기와 기존 `SO모드 사용법.md` 열기를 제공한다. 도움말 이동만으로 편집 명령을 자동 실행하지 않는다.
- 빠른 사용법은 현재 영역·도구에 맞는 내용만 보인다. 배치와 타일 복사 대상·붙일 기준, 입력 칸 포커스, R/Shift+R 차이를 구분한다. Ctrl+C/V가 검사 통과를 필요로 한다는 안내를 만들지 않는다. 기능 이름·단축키·비활성 이유는 해당 명령 상태에서 얻으며 안내용 허용 규칙을 따로 만들지 않는다.
- 초보자 흐름은 `틀 선택 → 새로 만들기/복제 → 배치 → 필요한 경우 바닥/참조 설정 → 검사 → 저장 → 필요한 경우 등록 → Unity 진입 확인`이다. 등록은 Run 후보 편입이며 저장과 같은 뜻이 아니다. 새 스테이지/전용 SO 복제는 **그 작업에서 에셋을 만들 수 있음**을 기존 확인창과 함께 안내한다. “저장을 누르기 전에는 어떤 파일도 생기지 않는다”는 설명을 하지 않는다.
- 작업 상태는 기존 UX2의 저장/검사 축을 재사용한다. 미검사·검사 중·수정 후 재검사·오류·조건부 경고·완료, 확인 취소, 쓰기 실패, 에셋 저장 후 표 변경 실패를 구분한다. 저장 버튼은 매번 현재 작업본의 기존 검사를 실행한다. 검사 거절과 파일 쓰기 중 실패를 구분하며, 자동 롤백이나 원자적 저장을 새로 보장하지 않는다.
- 등록/빼기는 기존 미저장 제한을 유지한다. 등록은 공통 검사+맵 규칙을 거치지만 **빼기에는 공통 저장 검사를 새로 강제하지 않는다**. 기존 확인창·보스 교체·조회 실패 처리를 UI에서 우회하지 않는다.
- 초안은 `이 PC에 자동 보관되는 마지막 편집 작업본`, 저장은 `실제 SO 에셋 반영`, 공유는 `사용자 Git 커밋·전달`로 구분한다. 다른 스테이지 열기/에셋 다시 읽기는 현재 `버리기 / 취소` 확인을 유지한다. 새로고침과 다시 읽기를 같은 동작으로 설명하지 않는다.
- 숨김 안내는 §16 UX3의 `정상 비공개 / 판정 오류 / 무작위 위치 미정`을 구분한다. UX-c 코드와 기존 속성 영역 연결은 반영됐으며 계획 §12.9가 실행·확인 원본이다. UXR-c에서 같은 기능을 다시 작성하지 않는다.
- 사용할 수 없는 버튼에는 이유와 가능한 다음 행동을 글자로 보여 준다(선택 없음, 클립보드 없음, 바닥 방식 아님, 공유 참조, 짝 View 없음 등). 실제 명령의 거절 문구·판정을 재사용하고, 상세 원문을 짧은 성공 문구로 덮지 않는다.

### 21.6 구현 책임과 표시 상태 계약

아래 책임은 UXR-a/b와 UXR-c에 반영했다. Workspace·Help partial은 각각 `.meta`를 포함한다. 코드 반영과 Unity 확인은 구분하며 실행·미확인 항목은 계획 §14.7~14.8·§20.8을 따른다.

| 위치 | 맡는 일 / 기존 기능과의 경계 |
|---|---|
| 새 `CombatLayoutWindow.SoWorkspace.cs` | 작업 영역 탭·화면 조립·속성 분류·공통 영역 이동. 편집/검사/저장 계산을 구현하지 않음 |
| 새 `CombatLayoutWindow.SoHelp.cs` | 작업별 도움말 표시·주제 선택·기능 위치 이동. 자체 문서 작업본이나 저장 상태를 소유하지 않음 |
| `CombatLayoutWindow.SoMode.cs` | 기존 명령·입력·SO 작업본·공유 편집 수명 유지. `DrawSoMode`가 새 화면 조립을 호출하고 기존 묶인 패널을 표시 단위로만 나눔 |
| `CombatLayoutWindow.SoTiles.cs` | 타일 패널/선택/키/클립보드/난수 수명의 원본 유지. 새 바닥 영역과 기존 2D/3D 도구가 같은 구현 호출 |
| `CombatLayoutWindow.SoUx.cs`·`CombatStageSoIssue.cs` | 상태·검사 결과·대상 개정·이동 원본 유지. 위치 이동에 작업 영역/속성 열기를 연결 |
| Document·Assets·Creation·Validation·Chapter·Preview·Visuals·Range | 동작 원본을 재사용. 화면을 옮기기 위해 쓰기 형식·검사·좌표식·서비스를 변경하지 않음 |

화면 조립용 계약(창 private):

- `SoWorkspace { Layout, Tiles, Settings }`, `GetSoWorkspace()`와 `SetSoWorkspace(SoWorkspace next)`. 현재 영역은 **설정 표시 여부 + 기존 soMapView**에서 계산하며 별도의 직렬화된 `현재 영역` 사본을 두지 않는다. 설정을 떠날 때 쓸 마지막 배치 보기(2D/3D)만 표시용으로 기억한다.
- `SoInspectorSection { Selection, Stage, PlayerZone, References, Background, Gimmicks, Registration, RandomTerrain }`, `OpenSoInspector(SoInspectorSection section)`. UI 분류를 고를 뿐 데이터 변경/검사/저장을 실행하지 않는다. 다른 영역이 필요하면 `SetSoWorkspace`를 거친다.
- `DrawSoWorkspaceTabs()`·`DrawSoWorkspaceBody()`는 기존 패널을 조립한다. 배경/공유 SO/기믹 값은 기존 편집기 한 곳을 호출하며, 같은 대상의 독립 편집기를 둘 만들지 않는다.
- `SoHelpTopic { GettingStarted, Placement, Tiles, SharedAssets, SaveAndRegister, Troubleshooting }`, `ShowSoHelp(SoHelpTopic topic)`·`DrawSoHelp()`. 창 내부 도움말 영역을 사용하고 별도 도구 창이나 웹 런타임을 도입하지 않는다.
- 설정/도움말 표시·속성 선택은 작업본 스키마·EditorPrefs 초안에 넣지 않는다. 기존 `soMapView`·`SoTool` 숫자와 타일 모드 숫자는 재정렬하지 않는다. 문서 형식 번호를 UX만을 위해 올리지 않는다. 창 재생성으로 없어진 표시값은 배치/선택 기본값을 사용한다.

### 21.7 원칙 점검·변경 독립성

기준은 [View 설계 원칙 §4, 특히 §4.6](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙)이다.

| 원칙 | 적용 |
|---|---|
| SSOT | 저장 사실은 기존 SO 작업본/`soModified`, 검사 사실은 `soCheck`, 규칙은 기존 Core·도구 검사, 보기 변환은 TileFit/Visuals. UI는 읽고 호출하며 시안의 예시 데이터를 새 원본으로 삼지 않음 |
| S | Workspace는 화면 조립, Help는 안내, SoTiles는 타일 편집, SoUx는 검사 상태·이동. 저장과 규칙을 새 화면 파일에 모으지 않음 |
| O | 새 속성 분류·도움말은 해당 표시 파일에서 확장. 타일·낙석·등록의 내부 연산을 함께 수정할 필요가 없어야 함 |
| L | 기존 버튼·키가 같은 명령/거절/Undo 결과를 보장. 지도 표시 실패에도 기존 패널 복구 보장 유지 |
| I | 표시 함수는 필요한 작업 영역/분류/도움말 주제만 받음. 새로운 통합 서비스나 모든 편집 상태를 복제한 DTO를 만들지 않음 |
| D | 기존 편집·검사·저장 진입점을 사용하고 새 UI가 에셋 직렬화/Core 판정을 직접 구현하지 않음. 추상화를 위한 새 인터페이스는 추가하지 않음 |
| JMH/FW | Framework·Factory·서비스 등록·asmdef·게임 입력 경로는 변경하지 않음. PreviewRenderUtility·공유 복제본·이벤트의 기존 해제 소유권 유지 |

대표 변경의 범위: 도움말 문구 수정은 SoHelp+해당 사용법, 타일 회전 표현 수정은 SoTiles+관련 회귀, 작업 영역 조립 수정은 SoWorkspace+해당 연결에 머문다. 공통 `SetSoWorkspace`·입력 포커스·취소 계약을 바꾸면 배치/타일/검사 이동을 함께 확인한다. partial 분리만으로 독립성이 완성됐다고 보지 않으며, 무관한 낙석 계산·Core·저장 포맷을 읽거나 고쳐야 하는 구조는 피한다.

### 21.8 추적·검토 상태

- 구현·확인 Task 원본: [계획 §14](../plans/2026-10-27-combat-stage-editor-so-mode-plan-Hojin.md#14-확장-ux-시안-적용-계획-uxr). **UXR-a·UXR-b·UXR-c 코드 반영, Ua.1~Ua.3·Ub.1~Ub.4·Uc.1~Uc.4 사용자 확인 대기. UXR-d 문서·정적 대조 반영, D-01 문구 수정 반영·Phase 인수 대기.** UXR-c 실행 기록은 계획 §20.8, UXR-d는 §20.9다. 선행 UX-c는 별도 요청으로 코드 반영했으며 실행·사용자 확인 대기는 계획 §12.9를 따른다.
- §21.4의 실제 기능 차이는 유지한다. 사용법의 옛 `타일 보기 Ctrl+C/V 불가` 문구는 TXT-a에서 현재 선택 모드 기준으로 고쳤고 UXR-c가 인수했다. 기존의 미확인 기능을 시안에서 클릭해 봤다는 이유로 Unity 확인 완료로 바꾸지 않는다.
- UXR-a·b는 Workspace와 기존 표시·입력 연결을 변경했다. 에셋 값/런타임/Core/새 시험/MSBuild/Unity 실행/커밋·push/공유 전달은 이번 범위에 넣지 않았다. **컴파일 미확인·Unity 미확인, 고지대 개선 보류**를 유지한다.

### 21.9 UXR-a 반영 이력 (2026-11-02)

아래는 UXR-a 당시의 화면 상태다. 이어진 속성 재배치는 §21.10을 따른다.

- 공통 머리줄에서 스테이지 선택·전체 목록·새로 만들기/복제·기존 검사/저장/다시 읽기·사용법에 접근한다. 목록의 이름·키·맵·역할·등록 정보와 기존 틀/키 제안/두 생성 명령을 유지한다. 목록·생성은 본문을 사용하며 닫기 또는 작업 영역 탭으로 편집에 돌아간다.
- `배치 / 바닥 타일 / 스테이지 설정` 조립을 연결했다. 배치는 기존 팔레트·2D/3D·속성, 타일은 기존 타일 지도·편집과 배경 설정/2D·3D 타일 회전 이동을 제공한다. 설정은 기존 `스테이지 값 / 참조 / View` 전체 패널을 재사용한다. 기믹·아군 구역·등록 분리와 타일 종류/속성의 최종 배치는 **UXR-b**에 남겼다. 이번 단계에서 오른쪽의 기존 접기 항목을 제거하지 않는다.
- 전환은 표시값만 변경하며, 적용된 칠하기의 Undo 묶음을 닫고 미확정 이동·상자·팔레트·타일 사각형을 취소한다. 기존 지도 보기 숫자·도구 순번·선택·두 클립보드·3D 시점·랜덤 미리보기 시드를 보존한다. 설정·목록·생성 화면은 지도 키를 받지 않는다. 타일 유효 클릭도 기존 배치 클릭처럼 입력 칸 포커스를 해제한다.
- 기존 오류 위치 이동은 배치/2D/선택 속성을 열도록 연결했다. 설정에서도 현재 필드를 읽어 검사 위치 버튼을 제공하며, 필드 읽기 실패 시 설정 패널은 계속 표시한다. 검사 개정·대상·판 밖 수정 정책은 기존 SoUx가 맡는다.
- `사용법`은 현재 기존 Markdown 파일을 연다. 여섯 주제의 창 내부 도움말은 **UXR-c 미착수**이며 완료로 보지 않는다. 최소 창 크기·패널 경계·스크롤은 유지하고, 배치 도구 열 수와 안내 줄바꿈을 맵 폭에 맞췄다. 실제 밝은/어두운 테마와 크기별 확인은 Ua.3 대기다.
- 구조 대조: 화면 조립은 Workspace, 데이터 변경은 기존 명령, 검사 상태는 SoUx, 타일 선택/클립보드는 SoTiles가 계속 소유한다. SSOT·SOLID 5원칙·JMH/FW·변경 독립성은 §21.7 기준으로 점검했으며 새 서비스·저장 형식·Core 규칙은 도입하지 않았다. 정적 확인 결과와 사용자 확인 항목은 계획 §14.7에 기록한다.

### 21.10 UXR-b 반영 범위 (2026-11-02)

- **배치 속성:** `선택 / 아군 구역 / 무작위 지형`으로 나눴다. 같은 칸에 여러 층이 있으면 `속성 대상`에서 하나를 골라 기존 편집기를 연다. 이 선택은 속성 표시용이며 맵 끌기·Delete/R의 기존 칸 기준 대상 규칙을 바꾸지 않는다. 대상별 좌표·회전·정의·공개 대상·시작 아이템·삭제는 기존 함수를 그대로 호출한다. 무작위 시작 지형의 전체 편집기는 별도 목록에 있고, 아군 구역은 기존 경고·줄 수 복귀·전용 복제 명령을 사용한다.
- **스테이지 설정:** `기본 정보 / 필드·아군 구역 / 공유 SO·본부·구조물 / 배경·타일·카메라 / 기믹 · 레일·낙석 / 등록·맵 생성 규칙`의 여섯 분류다. 원래 `DrawSoStageValues`의 기본 값·통계, 레일/낙석, 등록 표시와 `DrawSoReferences`의 필드 참조 부분을 표시 단위로 나눴다. 공유 SO의 전체 SerializedProperty·키 잠금·전용 복제·새 필드, 본부 정의/구조물 목록, 비활성 배경 값, 짝 View 생성·카메라 참조를 모두 유지한다.
- **바닥 타일:** 왼쪽에서 현재 세트의 종류를 고르고, 가운데에서 칠하기/선택/기준 타일·회전·복사 명령을 쓴다. 오른쪽은 선택/복사 수와 기존 타일 구성 전체 패널이다. 후보 프리팹·공유 높이·사용 칸 수·맞춤 경고·Inspector 열기는 기존 원본을 사용한다. 선택/클립보드/화면 각/판 밖 선택/직사각 제한/랜덤 시드는 기존 SoTiles가 소유한다. 긴 회전·복사 줄은 나눴으며 연산은 바꾸지 않는다.
- **공통 검사·복구:** 기존 검사 결과 하나를 각 영역의 속성/목록 상단에서 펼쳐 본다. 검사 실행은 결과를 펼치고 해당 스크롤을 위로 옮기며 위치 버튼은 기존 Root/개정 확인 후 배치·2D·선택 속성을 연다. 별도 하단 고정 패널로 지도 높이를 줄이지 않는다. 검사 대상이 있으면 일반 속성 선택보다 우선 표시하고, 편집으로 강조가 풀려도 같은 대상의 속성을 이어서 볼 수 있게 한다. 좌표 없는 오류에 임의 위치 이동을 추가하지 않는다.
- **표시 실패:** 지도/속성 표시 오류를 해당 영역에 보여 주고 설정 복구 경로를 남긴다. `ExitGUIException`은 그대로 전달하며 지도·타일 스크롤은 scope로 닫는다. 표시 오류를 저장 검사나 자동 복구·롤백으로 바꾸지 않는다. 공격 범위 접기는 조작 줄만 접고 기존 범위 계산/표시는 유지한다.
- **구조·검증:** Workspace가 분류·화면·복구 동선을 조립하고, 각 기존 명령은 SoMode/SoTiles/SoUx가 맡는다. 저장/등록 판정·데이터 형식·Core·런타임·Preview 해제 수명은 변경하지 않았다. 기능별 실제 경로와 정적 대조 결과는 계획 §14.8이 원본이다. UXR-F27의 창 내부 도움말과 사용법 전체 갱신은 UXR-c/d에 남긴다. **컴파일·Unity 미확인, 고지대 B1 계속 보류.**

## 22 하붑 기믹 편집 추가안 (HBE)

- 상태(2026-11-02): 사용자 “이 설계대로 HBE-a 구현 진행” 승인에 따라 **Ha.1~Ha.6 코드·문서 반영, 컴파일·Unity 미확인, 커밋 전**. 기존 §17·§21의 하붑 제외는 당시 RF/UXR 범위이며 이번 별도 HBE로 추가했다. 실행·확인 원본은 계획 §15.4다.
- 목표: `스테이지 설정 → 기믹`에서 하붑 설정을 편집하고 기존 검사·저장·초안·Undo·스테이지 복제에 연결한다. Core·DB SO·런타임을 바꾸지 않으며 고지대 개선은 계속 보류한다.
- 접근: **기존 낙석과 같은 작업본 편집 경로를 확장하는 안을 사용자 승인으로 반영**했다. 기존 SO 필드를 사용해 저장 전 검사/취소를 유지한다. SO Inspector만 여는 안은 작업본/Undo 흐름과 달라 채택하지 않았고, 맵에 하붑을 칠하는 안은 Core에 고정 위치 입력이 없어 이번 범위에 넣지 않았다.

### 22.1 입력과 화면

원본은 `Data/InGame/Table/Stage/Combat/HaboobGimmickAuthoring.cs`의 네 필드와 `CoreCaculator/Contracts/Commands/StageGimmicks/HaboobGimmickSpec.cs`다.

| 화면 항목 | SO 필드 | 편집 계약 |
|---|---|---|
| 하붑 사용 | `haboob.enabled` | 끄면 하위 값 비활성, 값은 보존 |
| 이동 주기(진영 종료 횟수) | `relocationIntervalControllerTurns` | 숫자 입력. 초 단위가 아님 |
| 이동 전 경고(진영 종료 횟수) | `warningLeadControllerTurns` | 숫자 입력. 별도 경고 이펙트 제공을 약속하지 않음 |
| 하붑 영역 모양(상대 좌표) | `footprintOffsets[].x/y` | X/Y 행의 추가·수정·삭제. 스테이지의 고정 위치 좌표가 아님 |

- 화면 분류 이름은 짧은 `기믹`으로 하고 안에 레일·낙석·하붑 제목을 둔다. 기존 레일·낙석 편집기는 유지한다.
- 읽은 값을 그대로 보여 준다. 열기/켜기/끄기로 기본값을 덮거나 좌표를 정렬·보정하지 않는다. 신규 작업본의 기본값이 필요하면 공개 `HaboobGimmickSpec()`에서 얻고, 기본 12칸을 도구에 다시 하드코딩하지 않는다.
- 안내: “하붑은 전투 시작과 이동 시 위치가 정해집니다. 여기서는 사용 여부·주기·영역 모양을 설정하며, 맵의 현재 위치나 다음 이동 위치는 미리 보여 주지 않습니다.”
- 값 검사는 기존 공개 Spec을 호출한다. 현재 계약은 이동 주기 ≥ 1, 경고 1~이동 주기, 유효한 상대 칸 목록이다. 각 축 최소값 0은 같은 칸 (0,0)을 반드시 넣으라는 뜻이 아니며 기본 모양에도 (0,0)은 없다. Core 규칙을 UI에 다시 구현하지 않는다.

### 22.2 데이터·검사·수명

- Document에 직렬화 가능한 `CombatStageSoHaboob` 값 묶음(enabled·이동/경고 횟수·Vector2Int 목록)을 둔다. SO 필드 이름과 SerializedObject 접근은 Assets 한 곳에 두며 `ReadCombat`/`WriteCombat`에서 하붑 전용 읽기/쓰기를 호출한다. 꺼진 값과 목록 순서도 그대로 왕복한다.
- 작업본 `CurrentFormat`은 구현 시 4 → 5로 올린다. 구형 초안/재컴파일 후 열린 구형 작업본은 기존 거절·에셋 다시 읽기 정책을 따른다. 하붑이 없는 초안을 새 기본값으로 받아 원본 설정을 지우지 않는다. 업데이트 전 미저장 작업을 저장하도록 구현 보고에 안내한다. SO 에셋 형식이나 운영 데이터 일괄 이전은 하지 않는다.
- 모든 값 편집은 기존 `SoChange`를 사용한다. 따라서 Undo/Redo·초안·저장 안 됨·검사 결과 무효화는 기존 경로가 소유한다. 끄기도 하나의 명시적 변경이며 다른 하붑 값은 유지한다.
- Validation의 `TryCheckHaboob`는 사용 중일 때 공개 `HaboobGimmickSpec`으로 검사한다. 실패는 위치 없는 `하붑 값: 원문` 오류 한 건으로 보존하고, **검사용 combatCopy에서만** 하붑을 꺼서 중복 예외 없이 다른 검사를 계속한다. 원본·작업본의 사용 여부는 바꾸지 않으며 낙석 오류와 동시에 표시할 수 있어야 한다.
- 검사·저장·등록·생성은 같은 Validation 경로를 소비한다. 정상 하붑은 기존 GetData 경로도 통과해야 한다. 꺼진 하붑은 현행 ToData와 같이 상세 값을 검사하지 않되 저장 시 보존한다. 표 등록/보스 교체·파일 쓰기 실패 정책은 바꾸지 않는다.
- `ClearPlacements`는 하붑 설정을 지우지 않는다. 배치 비우고 새로 만들기·배치까지 복제 모두 틀의 하붑 설정을 보존한다. 선택 영역 Ctrl+C/V는 고정 배치 복사이므로 하붑 설정을 복사하지 않는다.
- **검사 한계:** 모양 전체가 필드에 들어갈 수 있는지와 실제 위치 선정은 기존 Core 전투 진입에서 확인한다. 도구에 후보 계산 사본·새 자동 수정·고정 앵커를 만들지 않는다. 검사 통과가 전투 진입 성공을 보장한다고 안내하지 않는다.

### 22.3 구조·기존 기능 보존

- 새 `CombatLayoutWindow.SoHaboob.cs`(+ `.meta`)는 하붑 값/목록 UI만 맡는다. SoMode의 `DrawSoGimmicks`는 기존 레일/낙석 뒤에 한 번 호출하고, Workspace는 분류 이름만 바꾼다. 낙석 편집기나 타일/공개 진단 구현을 옮기지 않는다.
- SSOT: 입력은 기존 SO/작업본, 규칙은 공개 Core Spec, 저장 경계는 Assets/Validation, 작업 상태는 SoChange/SoUx다. Core 로직·DB SO 필드·런타임 View·UI 서비스·asmdef를 추가/수정하지 않는다.
- SOLID: UI/값/입출력/검사/조립 책임을 구분(S), 기존 기믹에 하붑 호출만 확장(O), 기존 저장/복제/비활성 값 보존 계약 유지(L), 좁은 값 묶음과 함수 사용(I), UI가 기존 값 변경·검사 계약에 의존(D). 불필요한 서비스/인터페이스는 만들지 않는다.
- JMH/FW: 기존 IMGUI EditorWindow 내부 기능으로 런타임 UISystem 등록 대상이 아니다. 서비스·Factory·입력 Buffer·이벤트·Preview 생성/해제 수명은 그대로다. 기능 독립성은 UI 문구 변경 → SoHaboob, 필드 변환 변경 → Assets 하붑 함수, 검사 메시지 변경 → Validation 하붑 함수로 제한한다. 공통 저장/Undo/복제는 값 추가의 영향 범위이므로 확인한다.
- 범위 밖: 하붑 고정 위치/3D 구름/다음 위치 미리보기, 기본 모양 복원 버튼·모양 칠하기, 전투 규칙·이펙트/사운드 설정, 스테이지 에셋 변경, UXR-c/d, 고지대 B1 개선. 기존 고지대·토네이도·낙석·레일·타일·공개 대상·복사/붙여넣기는 보존한다.
- 설계 당시 자체 검토에서는 네 SO 필드·꺼짐 값 보존·구형 초안·동시 오류·복제/배치 비우기·위치 없는 검사·Core 진입 한계를 Task에 연결했다. 이후 사용자 승인으로 아래 HBE-a를 반영했다. Unity 확인은 별도 대기다.

### 22.4 HBE-a 코드 반영 (2026-11-02)

- Document에 하붑 값과 공개 Spec 기본값 복사를 추가하고 형식은 5다. Assets 전용 읽기/쓰기에서 네 SO 필드·비활성 값·목록 순서를 보존한다. 기존 생성/복제/배치 비우기 구현은 변경하지 않았다.
- 사용 중 값은 Validation의 공개 Spec 검사로 확인한다. 오류는 위치 없는 “하붑 값:” 한 건이며 검사 복제본에서만 꺼서 낙석 등 나머지 검사를 계속한다. 원본·작업본 사용 여부를 바꾸지 않는다.
- 새 SoHaboob UI는 SoChange·DelayedIntField·기존 Undo/초안/검사 수명을 사용한다. 기존 기믹 조립에 호출 한 줄을 연결하고 메뉴 이름을 “기믹”으로 바꿨다. 현재/다음 위치·필드 수용 여부·경고 이펙트의 한계를 화면과 사용법에 명시했다.
- 사용법 §5.12·§8.2와 민형님 §7·View §12 공유 메모를 갱신했다(미전달). UI/입출력/검사/조립 책임은 §22.3대로 나누고 서비스·Core·런타임·DB SO·운영 에셋·고지대는 변경하지 않았다.
- 정적 대조와 실행 상태 원본은 계획 §15.4다. **컴파일·Unity 미확인, 커밋 전, HBE-a 끝 보고 후 정지**이며 UXR-c/d를 진행하지 않는다.

## 23 낙석·하붑 모양 편집 UX 제안 (GSE)

- 상태(2026-11-02): **사용자 설계·계획/구현 승인, GSE-a 코드·문서 반영, 컴파일·Unity 미확인.** 요청은 “토네이도나 기차처럼 간편하게 배치”, 후속 선택은 “자동 위치는 유지하고, 모양·설정을 클릭/드래그로 쉽게 편집”이다. 이후 사용자가 아래 설계대로 계획 반영과 구현을 승인했다. 실행 원본은 계획 §16.3이다.
- 목표: 상대 좌표를 줄마다 입력하지 않고 눈으로 모양을 그린다. 기존 사용 여부·수치·고급 좌표 입력·검사·저장·Undo·초안·복제를 유지한다. Core·DB 스키마·런타임·운영 에셋·고지대 개선은 변경하지 않는다.
- 기존 §17 Q-RF1의 줄 목록과 §22의 모양 칠하기 제외는 RF/HBE-a 당시 범위다. 승인된 GSE에서 시각 편집을 추가하고 줄 목록은 고급 편집으로 보존한다. HBE-a의 완료/검증 상태를 다시 쓰지 않는다.

### 23.1 선택한 방향과 접근 비교

| 접근 | 장점과 제약 | 결정 |
|---|---|---|
| 자동 위치를 유지하고 상대 모양을 격자로 편집 | 기존 네트워크 없는 Editor 작업본/저장 경로와 Core 규칙을 유지한다. 실제 등장 위치는 지정하지 않는다 | 사용자 선택한 방향 |
| 맵에서 실제 등장 위치를 지정 | 토네이도처럼 특정 칸에 놓을 수 있지만 Core 입력·위치 선정 규칙과 DB 계약까지 바뀐다 | 사용자 선택에서 제외 |

자동 위치 방식의 화면은 **기존 기믹 패널에 전용 모양 격자를 넣는 안**을 제안한다. 실제 맵 위에 그려 모양으로만 저장하는 안은 칠한 위치에 등장한다고 오해하기 쉽다. 별도 창을 여는 안보다 기존 패널 안에서 수치·모양을 함께 보는 편이 이동과 창 수를 줄인다. 배치 화면에는 `낙석 모양·설정` / `하붑 모양·설정` 바로가기를 두고 같은 설정 패널의 해당 제목으로 이동한다. 바로가기는 새 배치 도구·새 기믹 인스턴스를 만들거나 사용 여부를 자동 변경하지 않는다.

### 23.2 작업자가 보는 화면과 조작

1. `스테이지 설정 → 기믹` 또는 배치 화면의 바로가기에서 낙석/하붑을 연다. 레일·낙석·하붑 기존 설정에 모두 접근할 수 있어야 한다.
2. `사용` 토글 아래에 **모양 격자**, `칠하기 / 지우기`, `기본 모양` 버튼을 둔다. 선택한 도구로 클릭하거나 드래그한다. 수치 입력과 원래 좌표 목록은 각각 `세부 설정`·`고급 좌표`에서 유지한다.
3. 낙석 제목은 **돌 하나의 모양**, 하붑 제목은 **구름 영역의 모양**이다. `위치: 전투 중 자동 결정` 안내와 사용 칸 수·가로/세로 크기를 표시한다. 낙석의 돌 수 최소/최대는 모양의 칸 수와 다른 항목이다.
4. 드래그가 끝나면 작업본에 반영하고 기존 `검사 → 저장`을 쓴다. 별도 모양 저장 버튼이나 다른 에셋 저장 경로는 만들지 않는다. 꺼짐 상태에서는 모양·상세 값 편집을 비활성화하되 값을 보존한다.

| 조작 | 계약 |
|---|---|
| 칠하기 | 지나간 칸을 추가한다. 같은 칸을 다시 지나도 반전하거나 중복 추가하지 않는다. 빠른 이동으로 칸을 건너뛰지 않도록 두 입력 지점 사이를 연결한다 |
| 지우기 | 지나간 칸만 제거한다. 빈 칸에 대한 입력은 변경 없음이다. 마지막 칸도 지울 수 있으며 빈 모양 안내와 기존 검사 실패를 유지한다 |
| 드래그 확정 | 한 번의 클릭/드래그 = Undo 한 번. 드래그 중에는 임시 미리보기만 바꾸고, 끝날 때 실제 차이가 있을 때만 SoChange로 반영한다 |
| 취소 | Esc·포커스 상실·도구/기믹/스테이지 전환·창 해제·Undo/Redo에서 미완료 드래그를 취소한다. 검사/저장 등 다른 명령 전에 미완료 드래그를 취소하며, 숨은 미리보기를 나중에 확정하지 않는다 |
| 기본 모양 | 해당 공개 Spec의 기본 FootprintOffsets를 새 목록으로 복사한다. 모양만 바꾸며 사용 여부·주기·피해 등 다른 값을 초기화하지 않는다. Undo 한 번으로 복구한다 |
| 고급 좌표 | 현재 X/Y 줄 추가·수정·삭제 기능을 유지한다. 단순 열기나 보기 전환으로 정렬·정규화·중복 제거하지 않는다. 잘못된 값도 원본대로 표시하고 기존 검사로 알린다 |

격자는 맵 좌표가 아닌 모양 전용 좌표계다. 드래그 중 좌표 원점과 화면 위치를 고정한다. 사용자가 격자로 새 모양을 확정할 때만 각 축 최소값을 빼 상대 모양을 (0부터) 맞춘다. 고급 좌표는 기존처럼 입력 그대로 저장 검사를 받는다. `(0,0)` 칸 자체를 강제로 추가하지 않는다. 이는 UI 입력 변환이며 유효성의 최종 원본은 공개 Spec이다. 화면에 `그린 모양은 상대 좌표에 자동으로 맞춥니다`를 안내한다.

기존 데이터의 모양·목록 순서는 읽기만 해서는 바뀌지 않는다. 화면에는 기존 큰 모양·음수 좌표도 잘라 버리지 않고 볼 수 있도록 이동/확대 범위를 제공한다. 표시 범위를 바꾸는 동작은 데이터 변경이 아니다. 칠하기 이후 좌표가 맞춰져도 편집 화면의 모양이 갑자기 이동하지 않도록 화면 원점은 임시 보기 상태로 보정한다. 신규 시안의 작은 격자는 체험용이며 제품의 저장 가능한 크기를 제한하는 근거로 삼지 않는다.

### 23.3 상태·구조와 기존 기능 보존

- **SSOT:** 저장 값은 기존 Document의 rockfall/haboob. 격자용 영구 배열·추가 SO 필드·새 CurrentFormat은 만들지 않는다. 칸 수와 크기는 기존 목록에서 계산한다. 드래그 임시 목록과 화면 원점은 편집 수명 동안만 소유한다. 재컴파일/창 닫기 시 미완료 드래그는 버리고 마지막 확정 작업본과 기존 초안을 유지한다.
- **기본값·규칙:** RockfallGimmickSpec/HaboobGimmickSpec을 원본으로 사용한다. 위치 후보·충돌·실제 배치 계산을 도구에 복제하지 않는다. 격자에 맞는 모양이어도 실제 필드에 들어가는지는 기존 전투 진입 확인이며 화면 안내를 유지한다.
- **책임 분리(S):** 새 `CombatStageSoGimmickShapeEditor`는 격자 표시·입력·드래그 후보·보기 상태, 별도 `CombatLayoutWindow.SoGimmickShapes` partial은 기믹 값 선택과 SoChange 연결을 맡는 안이다. 기존 낙석/하붑 UI는 호출과 수치 편집만 맡는다. Assets/Validation/Creation의 저장·규칙 구현은 복제하지 않는다.
- **확장(O)·계약 보존(L):** 두 기믹이 같은 격자 편집기를 사용한다. 레일·토네이도·타일·공개 대상·기존 영역 Ctrl+C/V와 새 격자의 입력 영역을 분리하고 기존 도구 enum 순서를 바꾸지 않는다. 기존 비활성 값 보존·저장 전 검사·복제 계약도 유지한다.
- **작은 경계(I)·의존(D):** 격자는 상대 좌표 목록과 확정/취소 결과만 다룬다. 전체 스테이지·Catalog·SerializedObject·Core 실행 상태를 받지 않으며 Assets에 직접 쓰지 않는다. 창의 조립이 확정 결과를 기존 SoChange로 넘긴다. 필요한 실제 변경 이유 없이 서비스/인터페이스 계층을 만들지 않는다.
- **JMH/FW:** 기존 IMGUI EditorWindow 내부 편집이므로 런타임 UISystem 등록 단위가 아니다. Factory/Group/Builder·Buffer·입력 ActionMap·Preview 수명은 변경하지 않는다. IMGUI 포인터 캡처는 새 격자가 소유하고 완료/취소/해제 때 반드시 반환한다. 기존 전역 입력 취소 경계에 새 격자의 취소만 조립한다.
- **변경 독립성:** 격자 표시/입력 변경은 새 편집기와 관련 조작 확인, 기믹 연결은 partial/기존 패널 호출, 바로가기는 Workspace/Sidebar 조립에 한정한다. 이전에는 낙석 목록 UI는 SoMode, 하붑 목록 UI는 SoHaboob을 각각 고쳐야 했지만 공통 칠하기 동작은 새 편집기 한 곳에서 바뀐다. 남는 공통 확인 경계는 SoChange·Undo·취소·저장/초안이며 관련 없는 Core·타일 수식·3D 배치 구현을 수정하지 않는다.

### 23.4 구현 전 확인 기준과 다음 단계

- 기존 켜짐/꺼짐 값·순서·잘못된 고급 입력이 열기만 해서는 달라지지 않는다. 보기 이동/확대·바로가기만으로 저장 안 됨 상태가 생기지 않는다.
- 클릭/빠른 드래그/되짚기/격자 밖 MouseUp/취소/포커스 상실이 중복 입력 없이 동작하고, 한 획의 Undo/Redo가 모양·초안·검사 상태와 함께 움직인다. 좌표 재맞춤으로 화면이 튀거나 다음 획이 다른 칸에 들어가지 않는다.
- 기본 모양 복원은 낙석 1칸·하붑의 (0,0)이 없는 기본 12칸을 공개 Spec에서 얻는다. 다른 설정은 그대로다. 빈 모양·잘못된 주기 오류 및 낙석/하붑 동시 오류를 기존 검사가 계속 차단한다.
- 좁은 창·큰/음수 모양·고급 좌표에서 접근 불가능한 칸이나 숨은 오류가 없다. 실제 맵의 고정 위치 편집으로 보이지 않는다.
- 확정 후 저장/다시 열기·켜기/끄기·초안·배치 비우기/스테이지 복제를 확인한다. 기존 레일/토네이도/영역·타일 복사·공개 대상 편집이 유지된다.
- 사용자 승인으로 계획 §16 GSE-a 한 Phase를 수행한다. 사용자 제한에 따라 새 시험 코드·MSBuild·검토 에이전트·커밋·push는 하지 않는다. 실행/검증 상태 원본은 계획 §16.3이며 **고지대 B1은 계속 보류**다.

### 23.5 GSE-a 코드 반영 (2026-11-02)

공통 격자 편집기와 창의 기믹 조립 partial(+ 각 .meta)을 추가하고 기존 두 기믹 UI/Sidebar/취소 경계에 연결했다. 모양 그리기·보기 이동/확대·기본 모양·한 획 Undo와 기존 세부 수치/고급 좌표를 함께 제공한다. 작업본 형식·Assets/Validation/Creation·자동 위치·기존 배치/타일/3D·Core/런타임은 변경하지 않았다. 사용법 §5.13과 공유 메모를 갱신했으며 공유는 미전달이다. 정적 근거와 미확인 항목은 계획 §16.3·§16.2를 따른다. **컴파일·Unity 미확인, 커밋 전, GSE-a 보고 후 정지, 고지대 B1 보류 유지.**

## 24 낙석 터널 폭발 켜기/끄기 설계안 (RTE)

- 상태(2026-11-02): **사용자 “전투 스테이지 편집 툴에도 적용해주세요”로 설계 승인, 계획 §17 RTE-a 코드·문서 반영, 컴파일·Unity 미확인.** 요청은 낙석에서 폭발을 사용하지 않을 수 있는 설정이며, 후속 선택은 “터널 폭발만 켜고 끄기 — 낙하·착지 효과는 유지”다. 아래 저장 위치·호환 방식·경고음 유지 등은 Codex가 제안해 함께 승인된 세부 결정이다.
- 목표: 같은 맵 프리팹을 쓰더라도 스테이지마다 터널 폭발 연출을 선택한다. 자동 낙석 위치·경고 시간·낙하·착지·피해·묶임과 하붑/GSE 기능·고지대 B1 보류를 유지한다.
- 현재 근거: `CombatRockfallEventHandler.ApplyFacts`는 경고 칸을 켠 뒤 `TryGetTunnelPose` 성공 시 `WarningCues`를 재생하고, 경고음은 그 뒤에서 독립 재생한다. `WarningCues`는 현재 터널 Boom 세 개와 화면 흔들림이다. `ImpactCues`·돌 능선/낙하 조각·피해는 별도 경로다. 낙석 View 설계 §3·§10은 이 기존 계약의 원본이며, 그 문서의 다른 진행 중 작업은 이번에 수정하지 않는다.

### 24.1 접근과 화면

| 접근 | 장점/제약 | 선택 |
|---|---|---|
| 스테이지별 View 값으로 터널 폭발 사용 여부 저장 | 프리팹 공유와 무관하게 스테이지별 선택, 기존 낙석 규칙/공용 외형 유지 | 승인 |
| 공용 RockfallVisual의 경고 큐를 비움 | 모든 사용 스테이지가 함께 바뀌어 스테이지 편집 도구의 개별 선택에 맞지 않음 | 제외 |
| 터널 오브젝트 제거/이름 변경/비활성화 | 설정 의도가 저장되지 않고 누락 경고가 나며, 능선 끝점이 터널 자식인 맵에서는 배경 구성에도 영향을 줄 수 있음 | 제외 |

- 위치: `스테이지 설정 → 기믹 → 낙석 → 세부 설정`에 **터널 폭발 사용** 체크박스를 둔다. 배치 화면의 기존 `낙석 모양·설정` 바로가기로도 접근한다.
- 기본값은 켜짐이다. 체크 해제 시 터널 폭발 이펙트와 그 폭발에 붙은 화면 흔들림만 생략한다. 경고 칸·느낌표·경고음, 돌의 능선/낙하, 착지 이펙트·착지 흔들림·착지음, 피해·묶임은 유지한다. 경고음은 현재 폭발 유무와 별도로 재생되므로 이 계약을 유지한다.
- 안내: “터널 폭발과 폭발 흔들림만 끕니다. 경고 표시·경고음·돌 낙하·착지 효과·피해는 유지됩니다.” 낙석을 꺼도 이 설정값을 지우지 않는다. 짝 View가 없으면 토글을 비활성화하고 View가 필요함을 표시한다.

### 24.2 값·호환·저장

- 표현 값 원본은 스테이지별 `StageViewPropData`다. 직렬화 필드는 `disableRockfallTunnelExplosion`(기본 false), 파생 읽기 속성은 `RockfallTunnelExplosionEnabled => !disableRockfallTunnelExplosion`이다. 화면은 긍정형 체크로 표시하고 저장 값만 반전한다. 기존 에셋에 새 필드가 없어도 false = 기존 폭발 켜짐을 유지한다. 운영 에셋 일괄 수정이나 공용 RockfallVisual 값 변경은 하지 않는다.
- Tool 작업본은 `CombatStageSoView`에 같은 bool을 두고, Assets의 View 읽기/쓰기에만 매핑한다. InGame의 RockfallGimmickSpec/Authoring/전투 데이터에 표시 옵션을 중복 저장하지 않는다.
- 작업본 형식은 5→6으로 올린다. 구형 초안/열린 작업본은 기존 거절·에셋 다시 읽기 정책으로 처리해 새 옵션이 없는 작업본이 저장된 설정을 덮어쓰지 않게 한다. 구현 적용 전에 미저장 작업을 저장하도록 안내한다. 외부 SO 형식 일괄 이전은 하지 않는다.
- 토글은 SoChange를 사용해 기존 Undo/Redo·초안·Dirty·재검사를 따른다. 저장/재열기·스테이지 복제·배치 비우고 만들기에도 View 값을 보존한다. 모양 기본값 복원이나 영역/타일 복사에는 이 옵션을 섞지 않는다.

### 24.3 전투 표시와 검사

- `StageViewPropPresenter`가 배경을 세울 때 같은 StageViewPropData에서 옵션을 읽고, 해제 때 함께 초기화한다. 실제 표시와 특성 미리보기의 두 Factory 생성 경로가 `FieldRockfallPresenter`에 명시적 bool로 전달한다. 공용 RockfallVisual이나 Framework 서비스를 추가 조회하지 않는다.
- `FieldRockfallPresenter.TryGetTunnelPose`는 옵션이 꺼졌으면 터널 누락 경고 없이 false를 반환한다. 기존 “터널 없음”에 더해 “표현을 명시적으로 끔”도 정상 false임을 인터페이스/구현 주석에 기록한다. EventHandler는 기존 분기대로 터널 WarningCues만 생략하고 나머지를 진행한다. 터널 Transform/능선 Transform을 파괴하거나 GameObject 활성 상태를 바꾸지 않는다.
- Tool 검사에서도 **폭발을 껐을 때 터널 없음 경고만 제외**한다. 켜짐+터널 없음은 기존처럼 저장 가능한 경고다. 능선 누락 경고와 같은 이름 중복 오류는 유지한다. 런타임도 같은 이름 중복 검사를 계속하므로, 옵션을 껐다는 이유로 도구에서 중복 오류를 숨기지 않는다. MapDefinition 방식의 기존 앵커 검사 한계도 유지한다.
- 프리팹이 없거나 View 짝이 잘못된 경우 등의 기존 필수 구성 검사는 완화하지 않는다. 옵션은 낙석 자체의 활성 여부나 Core 전투 진입을 우회하지 않는다.

### 24.4 구조와 확인 기준

- **SSOT/S:** 스테이지별 표현 선택은 StageViewPropData, 전역 연출 값은 RockfallVisual, 낙석 규칙은 Core다. Tool은 입력, Assets는 변환, Presenter는 표시 가능 여부, Factory는 연결을 맡는다.
- **O/L:** 기존 WarningCues 실행 분기를 이용한다. TryGetTunnelPose의 정상 false에 명시적 꺼짐을 포함하되 기존 켜짐/누락 동작을 보존한다. 착지/자동 위치 코드는 바꾸지 않는다.
- **I/D:** 이미 쓰는 ICombatRockfallField 경계를 유지하고 별도 거대 설정/서비스를 추가하지 않는다. Factory가 읽은 bool만 넘기며 낙석 표시가 DB/배경 전체를 검색하지 않는다.
- **JMH/FW·독립성:** 새로운 서비스 등록·상위 Export·Builder·Buffer·InputSystem·UISystem·FieldCamera 변경은 없다. 표시 정책 수정은 낙석 Presenter와 그 조립/검사 표시로 제한하고, 카메라 흔들림 구현·능선 낙하 계산·타일/하붑/고지대는 함께 수정하지 않는다. 공통 확인 경계는 View 직렬화/초안 형식과 두 Factory 연결이다.
- 사용자 Unity 확인 대상: 기존 에셋에서 켜짐 유지, 스테이지별 켜기/끄기·저장/재열기·Undo/초안/복제, 꺼짐 상태의 터널 누락 경고 제외, 켜짐 누락 경고/중복 오류/능선 경고 유지, 실제 경고 칸·경고음·낙하/착지/피해 보존, 스테이지 전환 후 옵션이 다음 스테이지에 남지 않음, 특성 미리보기 생성 경로.
- 승인한 설계를 계획 §17 RTE-a로 구현한다. 사용자 제한에 따라 새 시험 코드·MSBuild·검토 에이전트·커밋·push는 하지 않는다. 코드 반영/정적 확인과 사용자 Unity 확인은 계획에서 구분하며 **고지대 B1 계속 보류**다.

### 24.5 RTE-a 반영 기록 (2026-11-02)

스테이지별 View bool과 긍정형 속성, 배경 캐시/해제 및 두 Factory 연결, 낙석 Presenter의 폭발 생략 분기, Tool의 토글/저장/누락 안내를 반영했다. 작업본 형식은 6이며 기존 SoChange·검사 복제본·Creation 경로를 재사용한다. EventHandler·낙하/착지·Core·운영 에셋은 수정하지 않았다. 사용법/공유 메모와 낙석 View 설계의 계약 링크를 갱신했으며 공유 미전달이다. 실행/정적 근거는 계획 §17.3, 사용자 확인은 §17.2다. **컴파일 미확인, Unity 미확인, 커밋 전, RTE-a 보고 후 정지, 고지대 B1 계속 보류.**

## 25 하붑 시작 위치 지정 (HAT)

- 상태(2026-11-03): 사용자 "도구에 넣어"(SO 모드에 시작 위치 칸과 맵 미리 보기). 아래 HAT1~HAT6은 권장안으로 진행한 결정이며 **사용자 확인 대상**이다. 실행 원본은 계획 §18이다.
- 배경: 천명님 `cc4661896`(11-02, [하부브 시작 위치 설계](2026-11-02-haboob-initial-anchor-design.md))이 스테이지 전투 SO에 시작 위치 입력을 더했다. §22.2 "고정 앵커를 만들지 않는다"·§22.1 안내 "현재 위치는 미리 보여 주지 않는다"는 Core에 입력이 없던 때의 범위이며, 이 절이 그 부분을 대신한다. 무작위 이동·다음 위치는 계속 미리 보여 주지 않는다.

### 25.1 확인한 사실

- 입력: `HaboobGimmickAuthoring.fixedInitialAnchor`(bool, 기본 꺼짐)·`initialAnchor`(CellInput). 켜면 모양의 상대 좌표 (0,0) — 모양을 감싸는 상자의 왼쪽 아래 — 을 그 보드 칸에 놓는다. 전투 준비 때 한 번만 쓰고, 이동은 늘 무작위다.
- 음수는 `HaboobGimmickSpec` 생성(= DB 생성)에서 예외, 판 밖은 전투 준비(`HaboobGimmickRule.FixedCells`)에서 예외(F1)다. 판 안 범위는 Core `AnchorLimit`(X < 판 가로 − 모양 최대 X, Y도 같음)이고, 모양의 각 축 최솟값 0은 스펙이 보장한다.
- 지금 도구는 두 칸을 모른다. 하붑 읽기/쓰기가 알려진 칸만 다뤄 저장해도 값은 남는다.

### 25.2 결정(권장으로 진행, 사용자 확인 대상)

| 번호 | 내용 |
|---|---|
| HAT1 | 입력: 기믹 → 하붑 패널의 모양 다음에 **시작 위치 지정** 토글과 **시작 X·Y**. 하붑이 꺼졌거나 지정이 꺼지면 입력만 비활성·값 보존. 편집은 SoChange(Undo·초안·검사 무효화 그대로). |
| HAT2 | 미리 보기: 지정이 켜져 있으면 2D·3D 맵에 구름이 덮는 판 안 칸을 구름 색으로 덧칠하고 범례 한 줄을 더한다. 칸 = 시작 위치 + Core 공개 스펙이 검사한 모양(`HaboobGimmickSpec.FootprintOffsets`). 무작위면 그리지 않는다. 맵을 눌러 시작 위치를 고르는 입력은 이번 범위 밖. |
| HAT3 | 검사: 음수는 Validation이 시작 위치까지 넘긴 공개 스펙 검사로 "하붑 값:" 오류. 판 밖은 토네이도 고정 중심(`TornadoProblem`)과 같이 창의 `SoProblems`에서 저장·등록을 막는다(전투 진입 때에야 멈추는 것을 미리). 판 안 규칙은 "모든 칸이 판 안" 한 줄 사본이고 Core `AnchorLimit`과 같은 뜻이다(주석에 원본). |
| HAT4 | 작업본 형식 6 → 7. 옛 초안·열린 작업본은 기존 거절·에셋 다시 읽기 정책. |
| HAT5 | 배치 비우기·복제·Ctrl+C/V는 §22.2 그대로(하붑 설정 보존, 영역 복사에 섞지 않음). |
| HAT6 | 안내 문구: 무작위 이동·다음 위치는 미리 보여 주지 않음, 지정한 시작 위치만 맵에 그림, 판 안 여부는 지정했을 때 저장 검사가 봄. |

### 25.3 원칙 점검

- SSOT: 값은 SO·작업본 하나, 모양 규칙은 Core 공개 스펙, 칸 계산은 작업본 값 묶음의 함수 하나(`CombatStageSoHaboob.StartCells`)를 패널·맵·검사가 함께 부른다. 판 안 규칙만 사본 한 줄(Core 쪽 internal).
- SOLID: 값·칸 계산 = Document, SO 입출력 = Assets, 값 검사 = Validation, 판 크기 검사 = SoProblems, 입력 = SoHaboob, 그리기 = SoMode 맵 함수(S). 기존 하붑 경로에 칸 둘을 더할 뿐(O), 기존 꺼짐 값 보존·저장 전 검사 계약 유지(L).
- JMH/FW: Editor 창 내부 기능이라 런타임 서비스·Factory·UISystem 무변경. Core·DB SO·런타임 View 무변경.
- 독립성: 문구 → SoHaboob, 칸 계산 → Document, 저장 칸 이름 → Assets, 판 밖 문구 → SoProblems 한 곳씩.

## 26 배치 도구로 하붑 시작 위치·낙석 켜기 (GP)

- 상태(2026-11-03): **결정 완료(사용자: Q-GP1·Q-GP2·Q-GP3·Q-GP4 A + 보충 셋, Q-GP5 A), GP-a 구현은 계획 §19.** 결정은 §26.6. 사용자 "이거(하붑 시작 위치) 배치에도 있게 해줘요. 그리고 낙석도 배치에 있으면 눌러서 어느 노드에 클릭해도 활성화 되게".
- 목표: 맵 위 배치 도구 줄에서 하붑 시작 위치를 칸을 눌러 정하고, 낙석은 아무 칸이나 눌러 켠다. §25의 패널 입력·미리 보기·검사는 그대로 쓰고 값 원본도 같다(작업본 `haboob`·`rockfall`).

### 26.1 확인한 사실

- 배치 도구 줄은 `SoToolLabels`·`SoTool` 순서 그대로 `SelectionGrid`로 그린다. 맵 누르기는 2D·3D가 `HandleSoPointer` → `HandleSoCell` 한 곳을 지난다. 도구 열거 순서는 바꾸지 않는다는 기존 계약(§23.3)이 있어 새 도구는 끝에 붙인다.
- 지금 도구에는 마우스를 올린 칸을 따라가는 표시(호버)가 없다(창 `wantsMouseMove` 없음). 끌어 놓기 그림자는 정의 목록 끌기·옮기기에만 있다.
- 왼쪽 배치 패널의 "자동 위치 기믹 설정" 버튼 둘(§23)은 설정 패널로 가는 바로가기다. 하붑은 이제 시작 위치를 정할 수 있어 제목이 맞지 않는다.
- 낙석 위치는 Core가 무작위로 고르므로 칸에 저장할 값이 없다. 켜기/끄기는 작업본 `rockfall.enabled` 하나다.

### 26.2 결정(안)

| 번호 | 내용 |
|---|---|
| GP1 | 배치 도구 줄 끝에 **하붑 시작**·**낙석** 두 도구를 더한다(열거 끝, 기존 순서 그대로). 도구를 고르면 오른쪽에 그 기믹 설정 제목이 열린다(기존 바로가기와 같은 길). |
| GP2 | 하붑 시작 도구로 칸을 누르면 한 번의 Undo로 하붑 사용·시작 위치 지정을 켜고 시작 위치를 정한다. 누른 칸의 뜻은 Q-GP1. 다른 칸을 누르면 옮긴다. 끌기는 하지 않는다. |
| GP3 | 구름이 판 밖으로 나가는 칸은 Q-GP2대로 다룬다. 판 안 판단은 §25의 `StartCells`와 같은 식이다. |
| GP4 | 낙석 도구로 아무 칸이나 누르면 한 번의 Undo로 낙석을 켠다. 끄는 방법은 Q-GP4. 맵에는 그리지 않는다(위치 무작위). |
| GP5 | 왼쪽 패널 제목을 "기믹 설정"으로 바꾸고 버튼 둘은 그대로 둔다. 도구 안내 줄에 하붑 시작 위치·낙석 상태를 보인다. |
| GP6 | 값·검사·저장·초안·형식 번호는 §25 그대로(새 작업본 칸 없음 → 형식 7 유지). 기존 배치·지우기·선택·옮기기·타일 도구 동작은 바꾸지 않는다. |

### 26.3 정할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q-GP1 | 하붑 시작 도구로 누른 칸의 뜻 | **A 구름 가운데 근처**: 시작 위치 = 누른 칸 − (⌊(가로−1)/2⌋, ⌊(세로−1)/2⌋). 기본 4×4 모양이면 누른 칸이 구름 칸이고 시작 위치는 왼쪽·아래로 한 칸. / B 누른 칸 = 시작 X·Y(상자 왼쪽 아래 — 기본 모양이면 빈 귀퉁이). |
| Q-GP2 | 구름이 판 밖으로 나가는 칸을 누르면 | **A 놓지 않고 안내**(지금 배치 도구가 겹침·맵 밖을 막는 방식과 같음). / B 판 안으로 붙여 놓음. |
| Q-GP3 | 하붑 시작 위치 지우기 | **A 하붑 시작 도구 줄에 "시작 위치 비우기(무작위로)" 버튼**, 지우기 도구는 그대로. / B 지우기 도구로 구름 칸을 누르면 비움(그 칸의 다른 배치를 먼저 지움). |
| Q-GP4 | 낙석 도구로 누를 때 | **A 켜기만**, 도구 줄에 "낙석: 켜짐/꺼짐"과 "낙석 끄기" 버튼. / B 누를 때마다 켜기·끄기를 바꿈. |
| Q-GP5 | 마우스를 올린 칸에 구름 그림자(호버) | **A 넣지 않음**(누른 뒤 맵의 구름 칸으로 확인, 호버 추적을 새로 만들지 않음). / B 넣음(창이 마우스 이동을 받아 2D·3D에 그림자, 그리는 비용·상태가 늘어남). |

### 26.4 원칙 점검(권장안 기준)

- SSOT: 값은 작업본 `haboob`·`rockfall` 하나, 칸 계산은 §25 `StartCells`, 판 안 판단은 그 결과를 쓴다. 누른 칸 → 시작 위치 식(Q-GP1)은 도구 한 곳.
- SOLID: 도구 갈래는 `HandleSoCell` 앞쪽 두 줄, 실제 변경은 작업본 함수 둘(`PlaceHaboobStart`·`EnableRockfall` 꼴)이 맡는다(S). 새 도구는 열거 끝에 붙여 기존 도구 계약을 깨지 않는다(O·L).
- JMH/FW: Editor 창 내부. 런타임·Core·DB SO 무변경.
- 독립성: 도구 이름·안내 → SoMode 표, 누른 칸 해석 → 도구 함수, 값 변경 → 작업본 함수, 검사 → §25 그대로.

### 26.5 Phase(안)

GP-a 한 Phase: 도구 둘·누르기 갈래·작업본 함수·안내 줄·버튼·패널 제목, 사용법 §5.12·§5.11, 계획 §19. 결정 뒤 계획을 쓴다.

### 26.6 결정(2026-11-03 사용자)

| 번호 | 결정 |
|---|---|
| Q-GP1 | A 구름 가운데 근처. **보충:** 지금처럼 왼쪽 목록에서 맵으로 끌어 놓아 설치하는 것도 함께 있어야 한다 → 왼쪽 패널에 '하붑 시작 위치' 항목을 두고 정의 목록과 같은 끌어 놓기(§20)를 쓴다. 끄는 동안 놓일 구름 칸을 그림자로 보인다(기존 끌어 놓기 그림자, 호버 아님). |
| Q-GP2 | A 놓지 않고 안내. |
| Q-GP3 | A 도구 줄 "시작 위치 비우기(무작위로)" 버튼. **보충:** 선택 도구로 누른 칸이 하붑 구름 칸이면 Delete로 사라지게 한다 → 그 칸에 지울 배치가 없을 때 Delete가 시작 위치를 비운다(배치가 있으면 기존대로 배치부터). 하붑 사용은 켠 채이고 시작 위치만 무작위로 돌아간다. |
| Q-GP4 | A 켜기만 + 도구 줄 상태·"낙석 끄기". **보충:** 끌어 놓기 설치도 → 왼쪽 패널 '낙석 켜기' 항목을 맵 아무 칸에 끌어 놓으면 켠다. |
| Q-GP5 | A 마우스를 올린 칸의 그림자는 넣지 않음. |

- 구현 보충(GP1): 도구를 고를 때 오른쪽을 설정 작업 영역으로 바꾸면 배치 화면을 떠나므로, 오른쪽 대신 **도구 줄**(도구 버튼 아래)에 상태·버튼·"모양·세부 설정" 바로가기를 둔다. 왼쪽 패널의 기존 설정 버튼 둘도 그대로 둔다.

## 27 초안 보호·안내 정리와 UXR 마무리 (2026-11-09)

### 27.1 범위와 현재 근거

사용자 요청: “초안 보호랑 안내 문구 정리, UXR-c·d까지 설계·계획 문서에 반영해줘. 기존 기능은 유지하고 고지대 개선은 계속 보류해줘. 코드는 아직 수정하지 말고.” 구현 순서는 **DP-a → TXT-a → UXR-c → UXR-d**, Task 원본은 계획 §20 및 §14.4~14.5다. 각 구현 Phase는 별도 진행 요청 후 수행하고 보고·정지한다.

- DP-a 이전 `RestoreSoDraft`는 형식 불일치에서 EditorPrefs 초안을 삭제하고, `ReopenSoOldFormat`은 열린 구형 작업본을 버리고 에셋에서 다시 읽었다. DP-a는 이 두 경로를 아래 보호 정책으로 교체했다. 작업본 형식은 7을 유지하며 §12 UT8/P-UT6·§25 HAT4 등 이전 구현 기록은 당시 이력으로 남긴다.
- 이전 Unity 확인에서 Stage_Test2 초안 복원 후 재열기 때 “열린 스테이지 없음”이 관찰됐다. 원인은 확정되지 않았다. 이를 형식 불일치나 삭제 버그로 단정하지 않고 DP 수동 확인에 포함한다. 이미 없어진 초안의 복구를 보장하지 않는다.
- HAT/GP의 하붑 시작 위치·클릭/드래그 배치와 낙석 켜기는 이미 코드에 있다. 하붑의 **이동은 무작위**, 낙석의 **실제 낙하 위치는 자동**이라는 계약을 유지한다. 새 기믹·다중 독립 기믹·호버 그림자·고지대 B1 개선은 추가하지 않는다.
- 최초 요청은 설계·계획만 반영했고, 후속 사용자 지시로 DP-a·TXT-a·UXR-c를 차례로 반영했다. DP 저장소·보호 UI·수명 연결, TXT 화면 안내·사용법 해당 절, UXR-c 창 내부 도움말은 코드·문서 반영 상태다. UXR-d의 전체 문서·공유 메모 갱신과 정적 대조도 반영했다(계획 §20.9). 본부 삭제 안내 D-01은 후속 승인으로 문구·주석 수정 반영했다(계획 §20.10). 컴파일·Unity 확인은 별도로 남긴다.

### 27.2 DP — 초안 원문 보호 계약

**선택:** 기존 프로젝트별 `SoDraftKey`의 활성 초안 한 개를 유지하고, `SoDraftKey + ".Recovery"`에 **복원 불가 원문 한 개**를 보관한다. 같은 PC의 EditorPrefs 보관이며 프로젝트 에셋·Git 공유·영구 백업이 아니다. 정상 편집마다 이력을 쌓는 방식과 자동 형식 변환은 범위 밖이다. 보호 원문은 편집 가능한 두 번째 작업본이 아니다.

| 조건 | 예정 동작 |
|---|---|
| 현재 형식·읽을 수 있는 초안·Root 존재 | 기존 자동 복원·보수적 Dirty·검사 무효화 유지. 값이 검사에 실패하더라도 고칠 수 있도록 복원 자체에 전체 저장 검사를 강제하지 않음 |
| 형식 불일치, JSON 손상, Root 누락, 내용은 있지만 열린 작업본으로 해석 불가 | 활성 원문을 삭제/기본값 덮어쓰기하지 않고 보호 처리. 이유·확인 가능한 형식/Root·다음 행동을 표시하며 SO 저장·등록·복제에 넘기지 않음 |
| 재컴파일 뒤 창의 작업본이 구형 | 자동 에셋 재읽기/폐기 금지. 기존 EditorPrefs 원문을 우선 보존. 메모리 작업본만 있다면 직렬화한 사본임을 표시하고, 업데이트 이전의 알 수 없는 필드까지 복구했다고 주장하지 않음 |
| 보호 슬롯이 비어 있음 / 같은 원문임 | 원문과 보관 메타정보를 기록하고 재읽어 일치를 확인한 뒤 활성 슬롯을 정리. 같은 원문은 중복 보관하지 않음. 메타정보는 보관 시각(UTC)·원본 형식(읽을 수 있을 때)·Root GUID(읽을 수 있을 때)·이유·원문 출처, 보관 봉투 형식은 1 |
| 다른 원문이 이미 보호돼 있음, 쓰기/확인 실패 | 기존 보호본과 새 활성 원문 모두 유지. 자동 교체·정리하지 않음. 원문 내보내기 또는 명시적 폐기 후 재시도하도록 안내. 해결 전 새 편집/전환 등 활성 슬롯을 덮는 명령을 막고 보기·도움말·원문 내보내기는 허용 |

보호 UI는 공통 영역에 두어 **열린 스테이지 없음·카탈로그 로드 실패·미리보기 실패**에서도 접근할 수 있어야 한다. 상태는 저장/검사 축과 별개로 `보호된 초안 있음 / 초안 보관 실패 / 복원 불가 이유`를 표시한다. `SetSoNotice` 한 번으로 사라지는 알림에만 의존하지 않는다.

보호 필요 여부는 첫 화면의 편집/저장/열기 명령과 키 처리보다 먼저 확인한다. 정상 활성 초안과 기존 보호본이 함께 있는 것만으로 편집을 막지는 않는다. **새로 보호해야 할 원문을 보관하지 못한 상태**에서만 덮어쓰는 명령을 차단한다. 보호 기록 뒤 활성 정리 전에 종료돼 양쪽에 같은 원문이 남아도 다음 시작에서 중복/충돌로 잘못 처리하지 않고 일치를 재확인한다. 내보내기만으로 슬롯이 비워지지는 않으므로 다른 보호본과 충돌하면 대상별 명시적 폐기 또는 취소가 뒤따라야 한다.

- **원문 내보내기:** 사용자가 고른 경로에 저장된 JSON 문자열을 변형 없이 UTF-8로 쓴다. 취소/실패 시 모든 원문 유지. 내보내기 성공도 자동 폐기 사유가 아니며, 기존 파일 덮어쓰기는 파일 대화상자의 확인을 따른다. 손상된 보호 봉투도 남기고 그 원문을 내보낼 수 있게 한다.
- **보호 초안 복원:** 현재 형식·해석 가능·Root 존재일 때만 사용. 활성 미저장 작업본이 있으면 기존 교체 확인을 거치며 취소 시 모두 유지한다. 성공 시 메모리 작업본/활성 초안에만 반영하고 Dirty·재검사 상태로 만든다. 실패 시 현재 작업 유지와 성공 시 선택·캐시·Undo 정리는 §27.2.2를 따른다. 보호본은 남기며 에셋에 자동 저장하지 않는다. 구형/미래 형식의 형식 번호만 바꾸거나 자동 병합하지 않는다.
- **보호 초안 폐기:** 대상/보관 시각을 보여 주는 명시적 확인으로 해당 보호본만 지운다. 취소는 무변경. 보호 슬롯 충돌 중 새 활성 원문도 별도로 대상이 명확한 내보내기/폐기를 제공한다.
- **에셋에서 다시 열기:** 보호 보관이 성공한 뒤 사용자가 선택할 때만 기존 읽기 경로로 간다. 원문 보호와 에셋 읽기 성공을 구분하고, 읽기 실패·취소로 보호본을 지우지 않는다.
- `SoChange`·Undo/Redo의 기존 활성 초안 갱신을 유지한다. 빈 창·DT 모드·단순 보기 전환·초안 확인 실패가 활성 SO 초안을 빈 값으로 덮어쓰지 않게 한다. 종료/재컴파일에서는 §27.2.3의 조건을 충족한 **미저장 확정 작업본만** 보관한다. 저장 실패를 성공으로 표시하지 않으며 강제 종료나 EditorPrefs 손실까지 보장하지 않는다.
- 정상 형식의 다른 스테이지 열기/다시 읽기에는 기존 `버리기 / 취소`를 유지한다. 버리기는 사용자가 선택한 활성 작업본에만 적용한다. 기존 에셋 쓰기·표 변경 단계의 성공/부분 실패 구분을 유지하고, 저장 성공은 해당 활성 초안만 정리한다. **일반 열기·저장·등록·복제는 보호 슬롯을 삭제하지 않는다.**

#### 27.2.1 에셋 쓰기 실패와 저장 후 표 변경 실패 (재검토 보완)

현재 `SaveSoStage`는 에셋 쓰기 완료 후 `soModified`를 해제하고 활성 초안을 정리한 다음 보스 표 변경을 시도한다. 이 순서를 DP 때문에 바꾸지 않는다. 아래 상태의 원본은 기존 저장 단계 결과이며 새로운 Dirty 사본을 만들지 않는다.

| 결과 | 작업본·활성 초안·표시 |
|---|---|
| 검사 거절/확인 취소 | 기존 작업본·Dirty·초안 유지, 에셋 쓰기 없음 |
| 공유 SO 또는 스테이지 에셋 쓰기 중 실패 | 저장 성공으로 처리하지 않음. 저장 시작 전 확정 원문을 보존하고, 실패 시 메모리 작업본도 그 내용으로 복구해 중간에 비운 공유 편집 목록이 종료 보관 때 다시 소실되지 않게 함. 미저장 상태·복구 초안을 유지하되 일부 파일은 이미 반영됐을 수 있음을 알림. 이는 작업본 복구이며 이미 쓴 에셋의 자동 롤백은 아님 |
| 에셋 저장 성공·보스 표 변경 실패 | 기존대로 **저장됨 + 표 변경 실패**. `soModified = false`와 활성 초안 정리를 유지하며, 미저장으로 되돌리거나 표 변경을 자동 재시도하지 않음. 실제 표 문제와 기존 복구 경로를 알림 |
| 에셋·필요한 표 변경 모두 성공 | 기존 저장 완료 처리. 활성 초안만 정리 |

모든 경우 보호본은 유지한다. 활성 초안 정리 자체의 I/O 실패는 에셋 저장 실패와 구분해 알리고 오래된 초안을 무조건 안전하게 제거했다고 주장하지 않는다. UXR-c의 상태 문구·비활성 이유도 이 구분을 인수한다.

#### 27.2.2 보호 초안 복원 시 작업본 교체 (재검토 보완)

1. 보호 원문을 별도 후보로 읽고 형식·해석·Root를 확인한다. 현재 작업본·활성 원문·Dirty·선택·캐시·Undo는 아직 바꾸지 않는다. 보호본과 활성 원문의 식별값도 보관해 확인창 동안 대상이 바뀌면 재확인을 요구한다.
2. 기존 교체 확인을 통과한 뒤 미확정 입력을 취소하고, 기존 활성 원문을 기억한 상태에서 복원 후보의 활성 초안 쓰기/재읽기를 확인한다. 실패하면 후보를 현재 작업본으로 교체하지 않고 이전 활성 원문 복구를 시도한다. 그 복구까지 실패하면 현재 메모리 작업본과 이전 원문을 유지하고 내보내기·재시도 경로를 제공한다. 이때 원문을 덮는 전환/편집은 차단하며 보관 성공을 주장하지 않는다. 보호 슬롯은 이 임시 복구에 재사용하거나 덮어쓰지 않는다.
3. 쓰기 확인 성공 후 작업본을 교체한다. 기존 열기 동작에 준해 미확정 드래그/획·상자·선택 배치 참조를 해제하고, 공유 편집 복제본·필드 입력 캐시·기믹 모양 보기·타일 선택의 대상 적합성을 갱신한다. 3D Preview는 기존 소유자의 무효화/시점 초기화 경로로 새 대상을 표시하고 이전 Root/개정의 검사 결과를 폐기한다. 기존 배치/타일 클립보드는 각각의 교차 스테이지 재사용 계약을 유지한다. 에셋에서 다시 읽어 후보 값을 덮지 않는다.
4. **복원 성공 한 번을 이 창의 Undo 한 묶음으로 기록**한다. 실패/취소는 Undo를 추가하지 않고 전역 Undo나 DT 이력을 비우지 않는다. 복원에 대한 Undo/Redo에서도 위 작업본 교체 정리를 수행하고 기존 보수적 Dirty·재검사 정책을 적용한다. 빈 작업본이나 구형 작업본으로 돌아가면 초안을 빈 값으로 쓰거나 구형 값을 저장하지 않고 DP 보호 판단을 먼저 적용한다. 일반 편집/기존 열기의 Undo 정책까지 일괄 변경하지 않는다.

복원 후 표시 갱신에 실패하면 적용된 작업본/보호본을 유지하고 기존 복구 패널에 오류를 표시한다. 에셋 저장 완료나 복원 전 상태로의 자동 롤백으로 오인시키지 않는다. 원문 쓰기와 메모리 교체 중 강제 종료까지 원자적으로 처리한다고 보장하지 않는다.

**DP-a 재검토 보완(2026-11-09):** 복원 Undo는 창 전체 스냅샷 대신 작업본·Dirty·복원 경계의 실제 변경분만 기록하고 즉시 확정한다. 이후 별도로 복사한 배치/타일 클립보드는 복원 Undo/Redo에 포함하지 않는다. 빈 창으로 돌아가는 복원 Undo에서는 자동 복원을 건너뛰고, 직전 복원에서 이어진 활성 초안인지와 읽었던 원문 일치를 확인한 뒤 그 활성 초안만 정리한다. 일반 빈 창/DT 종료나 읽지 않은 다른 초안을 삭제하는 정책으로 넓히지 않는다. 일반 에셋 열기는 복원에서 이어진 작업이라는 표시를 해제한다. 다른 작업의 원문/충돌/정리 실패는 보존·차단·명시적 재시도 경로를 따르며, 보호본은 유지한다. Redo는 다시 작업본과 활성 초안을 구성한다.

#### 27.2.3 종료·재컴파일 시 보관 조건 (재검토 보완)

종료 보관은 **열린 SO 작업본 + `soModified == true` + 현재 형식으로 편집 가능한 상태 + 미해결 원문 충돌/보관 실패 없음**일 때만 마지막 확정분에 수행한다. 미확정 획은 기존대로 취소한다. 구형/손상/Root 누락은 일반 저장 대신 보호 경로를 따르며, 읽지 않은 SO 초안을 DT/빈 창 종료가 덮거나 지우지 않는다. SO 편집 뒤 DT로 전환해도 이미 보관한 확정 초안은 그대로 남긴다.

저장 완료·명시적 버리기 완료·변경 없는 에셋 열기는 종료 시 활성 초안을 새로 만들지 않는다. 에셋 저장 후 표 변경만 실패한 경우도 저장 완료 쪽에 해당한다. 반대로 저장 후 사용자가 다시 수정하거나 Undo한 경우에는 기존 Dirty 정책에 따라 미저장 초안을 보관한다. 보호 상태에서 발생한 외부 Unity Undo 콜백도 일반 `SaveSoDraft`를 무조건 호출하지 않도록 같은 보관 조건을 적용한다. 보호본은 종료 때 자동 복원/삭제하지 않는다.

**DP-a 재검토 보완(2026-11-09):** 재컴파일 직전 창에 마지막 확인 원문·키 없음 여부·확인 여부·차단 이유·복원에서 이어진 활성 초안의 식별값을 남긴다. 재초기화는 이 기준을 이어받고, 현재 EditorPrefs 원문이 다른 경우 이를 새 기준으로 자동 수락하거나 열린 메모리 작업본으로 덮지 않는다. 비교 기준이 있는 창은 정상 새 창의 자동 복원과 구분한다. 이 필드들은 재컴파일을 위한 창의 상태이며 새 EditorPrefs 키나 편집 가능한 두 번째 초안이 아니다. 일반 Undo가 직렬화된 과거 기준을 돌려놓아도, 종료 직전 현재 기준으로 다시 기록한다. 빈 창 Undo 후 정리 실패도 차단 상태를 유지해 재컴파일이 취소한 초안을 다시 열지 않게 한다.

### 27.3 TXT — 기존 조작을 정확히 설명하기

판정·단축키·배치 연산은 바꾸지 않는다. 아래 문구는 TXT-a에서 화면과 사용법의 해당 부분만 반영했다(계획 §20.7, 컴파일·Unity 미확인). UXR-c가 인수했고 전체 문서 정합성은 UXR-d에서 대조했다(계획 §20.9). 화면의 본부 삭제 안내 D-01은 후속 승인으로 수정 반영했다(계획 §20.10, 컴파일·Unity 미확인).

| 대상 | 통일할 설명 / 보존할 동작 |
|---|---|
| 타일 Ctrl+C/V | `바닥 타일의 선택 모드에서 타일 복사·붙여넣기를 사용할 수 있습니다. 배치 복사와 클립보드가 분리됩니다.` §19의 목적지와 키 분기를 따른다. 2D/3D 타일 회전 도구·입력 칸 포커스와 혼동하지 않으며 검사 통과를 조건으로 붙이지 않음 |
| 하붑 모양 패널 | 공통 “위치: 전투 중 자동 결정”을 하붑에 그대로 쓰지 않음. `구름 영역의 상대 모양입니다. 시작 위치는 지정할 수 있으며, 이후 이동은 무작위입니다.` 현재 켜짐/지정/좌표는 기존 값에서 읽음 |
| 하붑 클릭/드래그·비우기 | 클릭은 구름 가운데 근처, X/Y 입력은 모양 기준점이라는 차이를 설명. 드래그 중 그림자는 기존대로 유지. `시작 위치 비우기`와 빈 구름 칸 Delete는 무작위 시작으로 돌아가며 하붑을 끄지 않음. 배치가 있는 칸은 배치부터 삭제, 상자 Delete/지우개는 기존 계약 유지 |
| 낙석 도구 | `낙석 켜기입니다. 클릭한 칸은 낙하 위치가 아닙니다.` 돌 하나의 모양·돌 수·자동 위치를 구분하고, 터널 폭발 끄기는 낙하·착지 효과를 끄지 않는다고 설명 |
| 복사 범위 | 스테이지 복제는 하붑/낙석 설정을 보존. 영역/타일 Ctrl+C/V는 기믹 설정을 복사하지 않음. 배치 비우고 생성할 때 기존 기믹 설정 보존도 유지 |
| 초안·저장·등록·공유 | 초안/보호 원문은 이 PC, 저장은 SO 반영, 등록은 Run 후보, 공유는 사용자 Git 작업. 보호 원문 내보내기를 SO 저장이나 게임에서 사용 가능한 파일 내보내기로 설명하지 않음 |

사용법 §5.5의 “타일 보기 Ctrl+C/V 불가”와 §5.10의 타일 복사 설명을 함께 대조해 수정했다. 하붑의 “팔레트에서 칠하는 배치는 아님”은 상대 모양 편집과 맵 시작 위치 배치를 구분하는 문장으로 바꿨다. 과거 하붑 시작 위치 자동·구형 초안 재읽기 안내는 당시 이력으로 표시하고 현행 사용법 §5.12·§8.3으로 연결했다. 정확한 기존 낙석 폭발·돌 수 설명은 유지했다.

### 27.4 UXR-c/d 인수와 기능 보존

2026-11-09 UXR-c 코드 반영: 공통 사용법/현재 도구에서 여섯 주제로 진입하고 기존 검사 결과·공개 진단·초안 보호 UI를 도움말 스크롤 본문에 연결했다. 도움말 중 지도 본문/키는 호출하지 않으며 닫기는 기존 영역을 유지한다. 명시적 위치 이동만 기존 Workspace/Inspector/MapView를 호출한다. 실행·정적 대조는 계획 §20.8, Unity 확인은 UXR Uc.1~4로 남아 있다. UXR-d 문서·정적 대조 기록은 계획 §20.9이며 D-01 문구 수정은 §20.10에 반영했으며 Phase 인수는 대기다.

- UXR-c는 §21.5의 여섯 도움말 주제를 유지한다. 기믹은 `배치 복사` 주제 안의 기믹 배치 설명과 현재 기믹 도구 한 줄 안내에서 접근하고, 초안 보호는 `검사·저장·등록`·`안 될 때`에 연결한다. 별도 기믹 편집기/보호 저장소를 도움말에 재구현하지 않는다.
- 기존 `soModified`·`soCheck`·명령 거절 이유와 DP 보관 결과를 읽는다. 숨김 복구·위치 이동·짝 View 복구·상세 사용법 열기를 유지하며, 도움말 표시 중 지도 편집 키/드래그가 뒤의 맵에 전달되지 않는다. 닫으면 이전 영역/선택/클립보드/시점을 유지한다.
- UXR-d는 UXR-F01~F27 대응표를 원본으로 쓰고 **HBE/GSE/RTE/HAT/GP/DP/TXT를 추가 대조 대상으로 연결**한다. 새 기능 표를 중복 정의하지 않는다. 빠른 사용법·상세 사용법·공유 메모의 화면 이름/효과 범위/초안 정책을 맞추며 외부 발송은 하지 않는다.
- 기존 공개 대상 기본 Player|Enemy와 복사 시 원래 목록 보존, 토네이도 방향/무작위 시작, 레일, 본부 삭제, 아군 구역, 공유 SO 보호, 타일 회전/랜덤/복사, 검사 경고 등급·보스 교체·등록 해제를 유지한다. DP 외의 저장/검사 정책 변경, Core·FieldCamera·Architecture·DT·DB SO 스키마 변경, 작업본 형식 7의 불필요한 증가, 운영 에셋 수정은 범위 밖이다.
- 이전 Unity 확인은 당시 Stage_Test2의 검사·표시·보기 전환에 한정한다. 저장·복제·Undo·전투 연출과 11월 3일 HAT/GP 확인을 대신하지 않는다. 사용자 초안을 저장하지 않고 보존한다는 요청은 계속 유효하며, 쓰기 확인은 나중에 사용자가 허용한 비운영 대상에서만 수행한다.

### 27.5 책임·원칙과 완료 기준

파일은 `Assets/ProjectL/Script/Tool/Editor/` 아래다. DP-a에서 추가한 `CombatStageSoDraftStore.cs`는 활성/보호 원문 저장과 결과만, `CombatLayoutWindow.SoDrafts.cs`는 복원 판단·보호 UI·명령 연결만 맡는다(각 `.meta` 포함). `SoMode`는 기존 편집/저장 진입점, `SoWorkspace`는 조립, UXR-c에서 추가한 `SoHelp`는 도움말과 위치 이동을 맡는다. 구체 계약과 Task는 계획 §20.2·§14.4에 둔다.

- **SSOT:** 실제 값은 SO/작업본, 검사 결과는 `soCheck`, 보호 여부는 저장소 결과. 보호 원문은 기록일 뿐 두 번째 편집 상태가 아니며 새 Dirty/규칙 사본을 만들지 않는다.
- **SOLID S/O:** 저장소·복원/보호 화면·도움말을 분리한다. 초안 형식 대응은 DP 경계, 문구 변경은 Help/기능별 안내에서 끝나고 낙석·타일 연산을 함께 바꾸지 않는다.
- **SOLID L/I/D:** 정상 초안 복원/기존 명령 계약을 유지한다. UI는 필요한 보관 결과와 기존 명령만 사용하며 직접 EditorPrefs 삭제나 SO 쓰기를 중복 구현하지 않는다. 이 목적만을 위한 런타임 서비스/포괄 인터페이스는 추가하지 않는다.
- **JMH/FW·독립성:** 런타임 Factory/Builder·서비스 등록·Buffer·입력 계층·Preview 자원 소유권 변경 없음. 공통 경계인 열기/저장/Undo/재컴파일과 도움말 입력 차단은 영향을 받는 배치·타일·기믹에서 확인한다. 문구만 바꾸는 TXT 때문에 Core/전투 계산을 재작성하지 않는다.
- 완료 판정은 원문 보존·실패/취소·현재 안내 일치·기존 기능 접근·수동 확인 근거로 한다. 코드 완료/정적 대조/Unity 확인/사용자 인수는 따로 기록한다. **DP-a·TXT-a·UXR-c 코드·문서 반영, UXR-d 문서·정적 대조 반영. 컴파일·최신 Unity·사용자 인수 미확인, D-01 문구 수정 반영(계획 §20.10), 고지대 B1 계속 보류.** 실행 원본은 계획 §20.5~20.10이다.

### 27.6 재검토 2·3번 보완과 DT 보류 (2026-11-09)

사용자는 검토 2·3번만 승인했다. 1번의 DT 모드 보호 초안 복원 Undo/Redo 동기화는 DT 사용 여부를 정하기 전까지 보류하며, 일반 DT 종료가 SO 초안을 쓰지 않는 조건도 변경하지 않는다. 고지대 B1 보류를 유지한다.

- **2번:** 명시적 보관 재시도에서 활성 키가 이미 없으면, 마지막 확인한 원문과 보호 봉투 내부 원문이 정확히 같은지 읽어 대조한다. 동일 원문 보관이 확인된 경우에만 활성 초안 기준을 없음으로 갱신하고 기존 보호 판단을 이어간다. 보호 슬롯 없음·손상·다른 원문·읽기 실패는 차단을 유지하며 원문을 덮거나 버리지 않는다. 재확인 후 다른 활성 원문이 생기면 기존 충돌 검사로 차단한다. 구형 메모리 작업본은 기존의 활성 원문 우선 보호 정책을 유지한다. 새 보관 슬롯·저장 형식·자동 폐기를 도입하지 않는다.
- **3번:** 에셋 목록이 없으면 공통 검사/저장 불가 이유에 목록 새로고침 안내를 포함한다. 상단 버튼·도움말·검사/저장 명령 진입부가 같은 이유 함수를 사용해 목록을 참조하기 전에 중단한다. 목록 새로고침 성공 뒤 기존 조건이 충족되면 다시 허용한다. 도움말·초안 내보내기·목록 재시도는 계속 제공하며 기존 저장 검사와 경고/등록 정책은 유지한다.
- **경계:** 원문 입출력은 DraftStore, 보호 판단은 SoDrafts, 공통 사용 가능 이유는 SoUx, 명령은 SoMode가 소유한다. 기존 책임·계약을 재사용하며 Core/런타임/에셋/DT/복사/Undo 연산으로 수정 범위를 넓히지 않는다. 구현·정적 확인과 사용자 Unity 확인은 분리한다.
