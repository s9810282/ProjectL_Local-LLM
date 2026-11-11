# Stage Map Camera Focus Implementation Plan — 2026-11-07

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking. 이 세션에서 이어 온 Codex 직접 수행 방식을 따른다. 프로젝트 사용자 규칙이 스킬의 시험·커밋 절차보다 우선한다.

**Goal:** 노드 클릭 이동의 상승부터 착지까지 카메라가 함께 이동하고, 전투 복귀는 로딩 가림 아래에서 현재 플레이어의 앞쪽이 보이는 구도로 정렬한다.

**Architecture:** `StageMapCameraDrag`가 Catalog 핸들과 offset을 계속 단독 소유한다. 새 정렬 부품은 좁은 제어 계약으로 구도 계산·진행률 소비·취소를 맡고, 기존 두 마커 이동 주체가 공통 시간과 완료를 연결한다. Scope는 로딩 준비 원인·세대·수명만 연결하며 Factory는 생성과 주입을 맡는다.

**Tech Stack:** Unity 6000.0.73f1, C#, View DB SO/Data, 기존 `ChessPieceMotion`·`IDelayService`, Camera Catalog Handle, `ILodingPreparation`.

**Spec:** [카메라 정렬 설계](../specs/2026-11-06-stage-map-camera-focus-design-Hojin.md). 실행자는 두 문서를 함께 읽는다. 동작의 원본은 설계이며 이 문서는 Task·연결 계약·확인 기록을 소유한다.

## Global Constraints

- 상태: **Phase 1 CF1~CF7 코드·정적 검토·문서 인계 완료**. 독립 검토 지적 2건을 보완했다. **컴파일 미확인**, 사용자 Unity U1~U8 인수 대기다. Run 문맥 검사 연결과 Inspector 직접 편집 경로를 보완했다. 이 계획은 선행 [맵 진입 연출 계획](2026-11-06-stage-map-intro-plan-Hojin.md)과 별도의 후속 작업이며, 그 계획의 Phase 2가 아니다.
- **구현 Phase는 1개, Task는 CF1~CF7의 7개**다. Phase 1 실행 지시 후 모두 연결하고 보고·중단한다. Task 중간은 사용자 재생 확인을 위한 완료 지점이 아니다. Unity 확인은 별도 인수 절차이며 자동으로 다음 Phase를 추가하지 않는다.
- 작업 루트는 `C:/Users/Hyeon2-400/Project_L`. 사용자가 이번 작업의 Codex 작성을 허용했다. 선행 작업과 다른 작업자의 미커밋 변경을 보존하며 커밋·push는 사용자가 직접 한다.
- **View 시험 코드·시험 도우미 작성, MSBuild·Unity 실행, 커밋·push를 하지 않는다.** 정적 확인과 사용자 Unity 확인을 구분하고 구현 보고에는 **컴파일 미확인**을 적는다. 기존 시험의 수정은 AGENTS.md에서 허용한 최소 호환 범위만 따른다.
- 수정 범위는 StageMap View, 해당 설정 SO/Data·운영 에셋, 기존 View Factory와 관련 문서다. Core·Buffer 구현·FieldCamera·Architecture·공용 Camera/Loading/Delay·전투 View·`ChessPieceMotion`과 경로 구현은 수정하지 않는다.
- 최초 낙하·선·보스 왕복의 표현과 이력을 유지한다. **낙하 높이는 20을 유지**하며 이번 작업으로 기존 낙하·Intro 시간이나 원본 메시·머티리얼·프리팹을 변경하지 않는다. 선행 Intro의 사용자 확인 대기를 이 기능의 완료 증거로 바꾸지 않는다.
- 새 서비스·입력 Layer·UI·페이드·상시 추적·카메라 선행 대기·별도 카메라 시간을 추가하지 않는다. 유효한 클릭의 전 구간 동행이며 **마커 실제 완료 + 카메라 최종 적용 성공** 뒤에만 다음 화면으로 넘어간다.
- 설정 초기 제안값은 `Camera Focus.Enabled=true`, `Player Viewport Y=0.35`. 범위 제한이 우선하며 렌즈·회전·가로 구도를 바꾸지 않는다. 기능을 꺼도 최초 Intro의 설정·차단은 유지한다.
- 네임스페이스는 폴더 경로, 블록 namespace, Allman, 공백 4칸, UTF-8/LF, `sealed` 기본, 한국어 주석. 새 `.cs`에는 `.meta`를 함께 만들고 기존 GUID는 유지한다.

## Review Focus

자동 시험 대신 아래 조건을 담당 Task의 정적 추적과 사용자 Unity 확인에 배정한다. 재현하지 않은 경로는 통과로 기록하지 않는다.

| 조건 | 기대 동작 | 담당 / 사용자 확인 |
|---|---|---|
| 상승·하강 중 일시정지, 큰 프레임 지연, 이동 거리/카메라 이동량 0 | 같은 시간으로 진행, 실제 마커 완료 전 진행 금지, 최종 적용·완료 전달 한 번 | CF3·CF4 / U1·U2·U7 |
| 덱 취소, Submit 후 거절/취소, 수락 중복 관찰 | Pending까지 복원 가능, 실제 수락 뒤 복원·이중 이동 없음 | CF4 / U3 |
| 로딩 준비와 일반 Tick 경합, 핸들 지연, 보이는 맵 재그리기 | 가림 문맥 확인 후 1회 정렬, Pending 버전 유지, 재그리기에서 수동 구도 유지 | CF5 / U4 |
| 최초 Intro, 먼 곳을 본 뒤 클릭, 자동 이동 뒤 첫 Alt/휠·Inspector 편집 | 자동 소유자 한 명, 직접 편집도 현재 구도에서 시작, 읽기만 하거나 자동 이동할 때 저장 구도 오염 없음 | CF2·CF6 / U5·U6 |
| 화면 비율·경계·투영 실패, 같은 챕터/노드 번호를 재사용하는 새 Run·준비 버전 교체 | 대상 표시 또는 명시적 오류, Delay 콜백·카메라 적용·완료/복원 전에 문맥 확인, 이전 이동의 쓰기·잠금 없음 | CF2·CF3·CF4·CF6 / U6·U7 |

## 1 파일 지도와 책임

아래 접두사와 파일명을 합친 경로가 정확한 대상이다. 모든 새 C#의 `.meta`도 같은 Task에 포함한다.

| 접두사 | 저장소 상대 경로 |
|---|---|
| D | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageMap/Item/` |
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/` |
| F | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/` |
| A | `Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView/Item/` |

| 구분 | 파일 | 책임 / Task |
|---|---|---|
| 신규 | D`StageMapCameraFocusData.cs` | 불변 설정과 검증 / CF1 |
| 수정 | D`StageMapViewData.cs`, D`StageMapViewDataSO.cs`, A`StageMapView_Default.asset` | 설정 변환·운영 값 연결 / CF1 |
| 신규 | V`IStageMapCameraFocusControl.cs`, V`StageMapCameraFocusFrame.cs` | 자동 조작의 좁은 계약과 구도 값 snapshot / CF2 |
| 수정 | V`StageMapCameraDrag.cs` | Intro와 정렬의 배타 소유권·offset 적용·수동 인계 / CF2·CF6 |
| 신규 | V`StageMapCameraFocus.cs` | 목표 계산·진행률·완료/취소 / CF3 |
| 신규 | V`StageMapMovementContext.cs`, V`StageMapMovementDelay.cs` | 이동 시작의 Run/맵/준비 버전 값과 기존 Delay 콜백의 문맥 검사 / CF4 |
| 수정 | V`StageCombatDeparture.cs`, V`StageMapNodeMovement.cs` | 선택 전/수락 후 두 이동 경로 연결 / CF4 |
| 수정 | V`StageMapViewEntry.cs`, V`StageMapViewScope.cs`, F`StageMapSceneFactory.cs` | 정렬 부품 전달·준비 원인·수명 조립 / CF4·CF5·CF6 |
| 수정 | V`Entry/StageMapLoadingPreparation.cs`, V`Entry/StageMapIntroPresentation.cs` | 가림 정보 전달·Intro 준비 결과 공개 / CF5 |
| 수정 | V`StageMapViewInput.cs`, V`Editor/StageMapCameraFramingReceiver.cs` | 명시적인 사용자 조정 때 현재 구도 인계 / CF6 |
| 신규 | V`Editor/IStageMapCameraFramingAccess.cs` | Inspector와 현재 Scope 사이의 편집 가능·현재 구도·편집 시작 계약 / CF6 |
| 수정 | V`StageMapSceneRoot.cs`, V`Editor/StageMapSceneRootEditor.cs`, V`Editor/StageMapCameraFramingSettings.cs` | Editor 전용 연결 슬롯·직접 편집·잠금 해제/확정의 현재 구도 인계 / CF6 |
| 수정 | `docs/superpowers/viewDocs/stage-map-guide-Hojin.md`, 이 계획·위 설계 | 구현 상태·조절 방법·확인 기록 / CF7 |
| 신규 | `docs/superpowers/handoffs/2026-11-07-stage-map-camera-focus-framework-share-Hojin.md` | 공용 구현을 바꾸지 않은 연결 사항과 확인 잔여, **미전달** 표시 / CF7 |

파일 지도 밖의 수정이나 설계 변경이 필요하면 구현 전에 사용자에게 확인한다. 단순 호출부 검색은 허용하지만 보호 영역을 수정하거나 무관한 기능을 함께 리팩터링하지 않는다.

## 2 Phase 1 — 클릭 동행과 복귀 정렬 연결

의존 순서는 **CF1 → CF2 → CF3 → CF4 → CF5 → CF6 → CF7**이다. 하나의 카메라 소유권과 완료 계약을 공유하므로 전체 연결을 한 Phase에서 마친다.

### CF1 설정 Data·SO·운영 에셋

**Files:** 파일 지도 D·A의 CF1 대상.

**Interfaces:** `StageMapCameraFocusData(bool enabled, float playerViewportY)`와 읽기 전용 `bool Enabled`, `float PlayerViewportY`, `void Validate()`. `StageMapViewData.CameraFocus` 및 생성자에서 `mapIntro` 뒤·선택 인수 `hoverStyle` 앞의 필수 `StageMapCameraFocusData cameraFocus`를 제공한다.

- [x] 새 불변 Data와 검증을 작성한다. Y는 유한한 `0 < Y < 1`이어야 하며 null 설정은 정상 기본값으로 숨기지 않는다.
- [x] SO에 직렬화 입력 `cameraFocus`와 변환을 연결한다. 초기값 true/0.35를 제공하고 `StageMapViewData.Validate()`에서 검증한다.
- [x] 운영 에셋에 같은 값을 명시한다. 기존 `nodeDrop.height=20`과 다른 필드·참조·GUID가 보존됐는지 diff로 대조한다.
- [x] `rg -n 'StageMapViewData\(|new StageMapViewData|StageMapCameraFocusData' Assets/ProjectL/Script Assets/Tests`로 생성·변환 경로를 확인한다. 기존 시험이 호출 변경으로 깨지는 곳은 프로젝트 제한에 따라 최소 호환 여부만 판정한다. U8의 설정·DB 에셋 확인을 인계한다.

### CF2 카메라 제어 계약과 공통 자동 소유권

**Files:** V의 CF2 대상.

**Interfaces:** `IStageMapCameraFocusControl`은 `bool IsReady { get; }`, `StageMapCameraFocusFrame BeginFocus()`, `bool IsFocusCurrent(long generation)`, `bool TrySetFocusOffset(long generation, float offset)`, `void EndFocus(long generation, bool restoreStart)`를 제공한다. 읽기 전용 값 형식 `StageMapCameraFocusFrame`은 생성자로 받는 `long Generation`, `CameraCatalogData Framing`, `Vector3 Axis`, `float MinimumOffset`, `float MaximumOffset`, `float StartOffset`, `float Aspect`를 보관한다. `Framing.Position`은 offset을 더하기 전 기준 위치다.

- [x] 계약·값 형식을 만들고 `StageMapCameraDrag`가 구현하게 한다. Intro의 기존 `IStageMapCameraTourControl` 서명은 유지하며 내부 자동 소유 종류와 단조 증가 세대만 공유한다.
- [x] `BeginFocus`에서 준비·배타 소유를 확인하고 수동 드래그를 끝낸 뒤 현재 offset을 캡처한다. 시작할 때 목표 구도로 순간 이동하지 않는다. Intro 또는 다른 Focus 소유 중 중복 획득은 오류다.
- [x] `TrySetFocusOffset`은 유효한 자기 세대에서만 유한 값·범위를 확인해 기존 Catalog Handle의 `Customize` 경로로 적용한다. 오래된 세대는 false, 적용 예외는 오류로 전달한다. 성공 전 완료로 간주하지 않는다.
- [x] `EndFocus`는 자기 세대를 먼저 해제하고 필요할 때만 캡처한 offset으로 복원한다. 핸들을 잃어도 자기 소유권 값은 정리하며 새 핸들에 대신 복원하지 않는다. 오래된 세대의 종료는 무해하다. 수동 드래그·휠·PreviewFraming·범위 갱신의 자동 조작 가드를 Intro/Focus 공통으로 바꾼다.
- [x] 획득 실패·중복 종료·이전 세대·Intro와의 경합을 정적으로 추적한다. Catalog 대여가 한 곳이고 기존 Intro 시작/복귀 계산은 그대로인지 확인한다. U5~U7을 인계한다.

### CF3 정렬 계산·진행률·취소 부품

**Files:** V`StageMapCameraFocus.cs`.

**Interfaces:** `StageMapCameraFocus(StageMapCameraFocusData settings)`, `void Bind(IStageMapCameraFocusControl control)`, `long Begin(int nodeId, Vector3 targetPosition)`, `bool SetProgress(long operation, float normalizedTime)`, `bool Commit(long operation)`, `void Cancel(long operation, bool restoreStart)`, `void AlignImmediately(int nodeId, Vector3 targetPosition)`, `void Release(bool restoreStart)`.

- [x] Scope마다 한 부품을 만들며 Bind는 같은 수명의 카메라 한 번만 받는다. Begin은 부품 자체의 새 operation 번호를 반환하고 카메라 세대·시작 offset·목표 offset을 보관한다. 미종료 operation이 있으면 Disabled에서도 중복 Begin을 거부한다. 서비스 묶음·Buffer·시간 서비스는 받지 않는다.
- [x] 목적 노드의 바닥 위치를 CF2 snapshot으로 투영한다. Dutch를 포함한 카메라 좌표에서 `q=R⁻¹(target-basePosition)`, `a=R⁻¹(axis)`, `v=2Y-1`로 두고 직교는 `(q.y-v*OrthographicSize)/a.y`, 원근은 `(q.y-k*q.z)/(a.y-k*a.z)` (`k=v*tan(FOV/2)`)로 축 offset을 계산한다. 기존 지원 구도 안에서만 사용하며 렌즈 모드를 전환하지 않는다.
- [x] 분모의 퇴화·비유한 값·잘못된 렌즈/화면 비율을 거부하고 기존 min/max로 제한한 결과의 x/y 및 near/far 가시성을 확인한다. 범위 제한 뒤 화면 밖인 대상은 nodeId·구도를 포함한 구성 오류다. 획득 뒤 계산 실패 시에도 자기 토큰을 해제한다.
- [x] SetProgress는 유한한 진행률을 Clamp01하고 `3u²-2u³`으로 offset을 적용한다. **유효한 operation에서 최종 적용에 성공한 경우만 true**다. 완료 상태에서도 Commit/Cancel까지 snapshot을 유지하고, 같은 최종 적용은 반복하지 않되 소유권 유효성은 확인한다. 오래된 operation 호출은 false로 무시하지만 현재 operation의 카메라 소유권 유실은 구성/수명 오류로 전달해 무한 대기로 숨기지 않는다.
- [x] Commit은 최종 적용·유효 소유권을 확인한 뒤 현재 구도를 유지하며 해제하고 성공 여부를 반환한다. Cancel은 자기 operation만 복원/해제하고 Release는 먼저 부품 세대를 무효화한 뒤 캡처한 자기 카메라 토큰을 정리한다. 해제된 operation은 새 작업에 영향을 주지 못한다.
- [x] Disabled도 유효한 operation 수명은 유지하되 카메라 획득/쓰기를 생략하고 SetProgress는 즉시 true로 처리한다. AlignImmediately는 Begin→최종 적용→Commit을 한 호출에서 끝내며 실패 정리를 보장한다. 매 Tick 할당·씬 검색·별도 시계가 없는지 정적으로 확인하고 U2·U6·U8을 인계한다.

### CF4 전투·비전투 이동과 실제 완료 연결

**Files:** V`StageCombatDeparture.cs`, V`StageMapNodeMovement.cs`, V`StageMapViewEntry.cs`, V`StageMapMovementContext.cs`, V`StageMapMovementDelay.cs`. Scope의 계약 연결은 CF6에서 마친다.

**Interfaces:** 읽기 전용 값 형식 `StageMapMovementContext(RunId runId, int mapKey, long preparationVersion)`는 `RunId`, `MapKey`, `PreparationVersion`을 보관한다. Scope가 제공할 `StageMapMovementContext CaptureMovementContext()`와 `bool IsMovementContextCurrent(StageMapMovementContext context)`를 각각 `Func<StageMapMovementContext> captureMovementContext`, `Func<StageMapMovementContext, bool> isMovementContextCurrent`로 받는다. Entry·NodeMovement·Departure 생성자에 이 두 delegate와 필수 `StageMapCameraFocus cameraFocus`를 추가하고 Entry가 같은 의존성을 NodeMovement에 전달한다. 세 부품의 `void CancelInvalidMovement()`는 이미 무효인 자기 이동만 정리하고, Entry는 NodeMovement로 위임한다. 기존 선택 요청·표시 lease·`CompleteAccepted`의 외부 서명은 유지한다.

**Delay contract:** `StageMapMovementDelay(IDelayService inner, Func<bool> isCurrent) : IDelayService, IDisposable`는 기존 `float CurrentTime { get; }`와 `void AddDelayEvent(DelayDataHandler data)`를 전달한다. 한 이동의 예약만 감싸며 `void Dispose()`로 보관한 원본 Tick/End delegate와 문맥 참조를 끊는다. 서비스 등록·새 큐·새 시계는 만들지 않는다.

- [x] 두 이동 주체가 시작 시 Context와 자기 이동 세대를 캡처한다. `CanWork`는 Scope 폐기/오류만 검사하므로 Run 검증으로 대신 쓰지 않는다. 같은 맵·노드 번호라도 RunId 또는 PreparationVersion이 다르면 이전 이동이다. 캡처값은 진행 원본의 복제가 아니라 유효성 비교용 불변 값이며 Buffer를 수정하지 않는다.
- [x] StageMapMovementDelay는 원본과 동일한 CurrentTime·DelayTime을 사용하고 **매 TickAction/EndAction 전달 직전** 캡처한 문맥과 자기 이동 세대를 검사한다. 무효이면 원본 콜백을 호출하지 않고 참조를 끊는다. 정상 End도 한 번만 전달하고 등록 실패·Dispose 뒤 지연 콜백은 무해하게 한다. 문맥 delegate는 시작 시 한 번 만들고 다음 이동의 가변 문맥을 읽게 재사용하지 않는다.
- [x] 두 이동 시작점에서 기존 `ChessPieceMotion`에 넘길 **같은 settings 객체의 `TotalSeconds`**를 D로 잡는다. `Begin` 바로 직전 Delay의 `CurrentTime`을 S로 읽고 같은 동기 호출에서 마커 Begin과 Focus Begin을 연결한다. Begin 실패·문맥 상실 시 생성한 표시 자원을 정리한다.
- [x] ChessPieceMotion에는 위 어댑터를 넘긴다. 동기 Begin 전후, 이동 Tick 시작, Focus SetProgress/Commit, CompleteAccepted, Submit 및 취소 복원 직전에 같은 문맥을 확인한다. 무효이면 CancelInvalidMovement로 자기 이동 세대를 먼저 무효화하고 정상 도착·팝업·완료를 게시하지 않는다. 자기 어댑터/마커 예약을 해제한 뒤 Focus를 복원 없이 취소하고 이동 상태를 비워 다음 준비가 새 이동을 받을 수 있게 한다. 기존 Motion.Dispose의 자기 Transform 정리는 새 표시의 생성·재사용 전에 끝내며, 새 맵에 `SetCurrentNode`나 클릭 전 카메라 복원을 실행하지 않는다. 정리 예외는 기존 오류 경계에 전달한다.
- [x] 매 이동 Tick에서 `u=Clamp01((Delay.CurrentTime-S)/D)`를 CF3에 전달한다. S/D/현재 시각은 유한하며 실제 이동의 D는 양수여야 한다. 별도 누적 deltaTime·평면 거리 진행률을 만들지 않는다.
- [x] `TryConsumeCompletion()`의 실제 마커 완료는 소모성 결과이므로 이동 문맥에 보존한다. 즉시 완료·큰 프레임 건너뛰기를 포함해 마커 완료 시 진행률 1을 적용하고, 카메라 성공까지 두 완료를 확인한다. 시간 경과만으로 마커 완료를 만들어내지 않는다.
- [x] NodeMovement는 두 완료 뒤 Commit 성공까지 확인하고 기존 `CompleteMarkerMove`·표시 lease 완료·팝업 허용을 한 번 진행한다. Disabled/카메라 이동량 0도 마커 완료를 기다린다. 거절된 비전투 요청에는 Begin하지 않는다.
- [x] Departure는 두 완료 뒤에만 도착 표시와 덱 출전 화면을 연다. **덱 열림·Submit·Pending에서는 Commit하지 않는다.** 같은 요청의 실제 수락·준비를 확인하는 `CompleteAccepted`에서만 Commit한 뒤 표시 완료를 게시한다. 후속 수락 관찰에서 NodeMovement가 같은 이동을 다시 시작하지 않게 기존 인수 경계를 유지한다.
- [x] 같은 유효 문맥의 덱 취소·거절·실행 전 취소는 기존 마커 복원과 함께 Cancel(operation, true)한다. 수락 확정 뒤 정상 해제는 복원하지 않는다. 씬/Run/맵/준비 버전 상실은 위 무효화 경로로 보내며 일반 취소 복원과 구분한다. 기존 취소 감지 경로마다 이 소유권 종료를 대조한다.
- [x] 상승/하강 중 정지, 거리 0, 카메라 이동량 0, 완료 중복, Submit 후 요청 소실·수락의 호출 순서를 정적으로 추적한다. 외부 호출 전후 캡처한 문맥을 재확인하고 완료 게시 전 두 표시와 Commit이 필요한지 확인한다. **같은 챕터·노드의 새 Run, 같은 Run의 새 준비 버전, View Tick보다 먼저 도는 이전 Delay 콜백**을 U7에 포함한다.

### CF5 로딩 준비 원인과 복귀 시 1회 정렬

**Files:** V`StageMapViewScope.cs`, V`Entry/StageMapLoadingPreparation.cs`, V`Entry/StageMapIntroPresentation.cs`.

**Interfaces:** Scope에 `void BeginScenePreparation(long version, bool isCovered)`를 추가하고 기존 `TryPrepareEntry()`·`EntryPreparationVersion` 계약을 유지한다. Scope 내부 준비 원인은 `AwaitingSceneEntry`, `CoveredSceneEntry`, `UncoveredSceneEntry`, `VisibleRedraw`로 구분한다. Intro는 읽기 전용 `bool WasSkippedAtPreparation`을 공개한다.

- [x] 초기 Scope는 AwaitingSceneEntry로 두고 어댑터 Begin이 캡처한 version과 isCovered를 Scope에 전달하게 한다. 버전 불일치는 완료로 처리하지 않는다. 일반 Tick은 이 원인 확정 전에는 Pending이며 준비를 먼저 끝내지 못한다.
- [x] Begin(true)는 CoveredSceneEntry, Begin(false)는 UncoveredSceneEntry로 기록한다. **false만으로 개발 모드라고 추정하지 않는다.** 가림 없는 개발 직접 진입도 이 경로로 최초 준비를 완료하되 이번 구현에서는 자동 복귀 정렬을 생략한다. 설계의 개발 직접 진입 허용을 필수 정렬로 확대하거나 새 진입 API를 만들지 않는다.
- [x] `RequestMapRedraw`는 이전 준비를 취소한 새 버전에 VisibleRedraw를 기록한다. Cancel/실패/Scope 종료에서 가림 문맥을 지우고 정상 Pending에서는 원인·버전을 유지한다. `IsLoading`을 가림의 증거로 쓰지 않는다.
- [x] Intro는 BeginPreparation에서 결과를 초기화하고, 기존 Prepare가 설정/대상/이력에 따라 생략을 성공적으로 결정한 경우만 WasSkippedAtPreparation을 설정한다. 재생이 나중에 끝난 사실을 생략으로 바꾸거나 이력 판단을 Scope에 복제하지 않는다.
- [x] 기존 핸들 연결→범위→에디터 초기 구도→Intro 준비 순서 뒤, **CoveredSceneEntry + Intro 준비 시 생략**인 유효 Run/맵/버전에서만 현재 노드의 바닥 위치로 AlignImmediately를 호출한다. 성공한 뒤에만 `_isShown`·준비 완료·팝업 복원 요청을 게시한다. 정상 Intro는 기존 준비 완료 후 Reveal·왕복 흐름을 유지한다.
- [x] 같은 버전의 반복 Tick은 완료된 준비를 재사용한다. VisibleRedraw/일반 RefreshDisplay에서 정렬하지 않으며 현재 offset을 불필요하게 초기화하지 않는지 기존 범위·Preview 경로도 대조한다. U4·U5의 가림 첫 화면, 핸들 지연, 상점/회복·개발 재그리기를 인계한다.

### CF6 Factory·Scope 수명·수동 입력 인계

**Files:** 파일 지도 CF6 대상 및 V`StageMapViewScope.cs`, F`StageMapSceneFactory.cs`.

**Interfaces:** Factory는 `StageMapCameraFocus(services.MapData.CameraFocus)`를 만들고 Scope 생성자에 필수 `StageMapCameraFocus cameraFocus`로 전달한다. Scope는 Entry·Departure에 같은 인스턴스와 CF4의 문맥 delegate를 전달하고 `AdoptCameraDrag` 직후 `Bind(scene.CameraDrag)`한다. Editor 전용 Drag 메서드는 `CameraCatalogData GetCurrentFraming()`과 `bool PrepareManualFraming(StageMapCameraFramingSettings settings)`다. FramingReceiver에는 `IStageMapCameraFramingAccess framingAccess`를 주입해 Inspector와 같은 편집 경로를 사용한다.

**Editor contract:** `IStageMapCameraFramingAccess`는 `bool CanEdit { get; }`, `CameraCatalogData ReadCurrent()`, `bool PrepareEdit()`, `bool ApplyEdit(Vector3 position, Vector3 angles, float size)`를 제공하고 Scope가 Editor 빌드에서 구현한다. SceneRoot에는 Editor 전용 비직렬화 `IStageMapCameraFramingAccess CameraFramingAccess { get; }`와 `void BindCameraFraming(IStageMapCameraFramingAccess access)`, `void UnbindCameraFraming(IStageMapCameraFramingAccess access)`를 둔다. Scope가 자기 연결을 게시하고 같은 참조일 때만 해제한다. 정적 등록소·서비스 등록·Inspector의 Scope 검색은 추가하지 않는다.

- [x] 생성·주입을 연결한다. StartMovement가 Drag 생성보다 먼저 호출되는 기존 순서에서도 실제 Begin은 Bind 이후여야 한다. 계산·진행률·완료 판정을 Factory에 넣거나 두 번째 카메라 lease를 만들지 않는다.
- [x] CaptureMovementContext는 유효하게 준비된 `_preparedRun`·`_preparedMap`·현재 EntryPreparationVersion을 반환한다. IsMovementContextCurrent는 Scope 수명과 준비 상태, 캡처값, 현재 Buffer의 RunId·HasMap·CurrentMapKey를 비교한다. 현재 노드가 목적지로 바뀌는 정상 수락을 문맥 상실로 오인하지 않으며 선택 번호/목적지 확인은 기존 이동 주체에 둔다. 초기 조립·해제의 CanWork는 이 이동 전용 판정으로 교체하지 않는다.
- [x] Scope Tick에서는 **이미 준비된 문맥이 있을 때** `_departure.Tick()`·`_entry.Tick()`·RefreshDisplay보다 먼저 변경을 확인한다. 변경 시 준비 버전을 한 번 무효화→두 부품의 CancelInvalidMovement→기존 준비 자원 정리 순서로 처리하고 그 Tick의 이전 이동 처리를 끝낸다. 이 문맥 상실 경로에서는 기존 Intro의 Cancel에도 카메라 복원 false를 전달한다. 초기 조립·로딩 Pending을 문맥 상실로 취소하지 않는다. 재그리기·Dispose도 표시를 새로 만들기 전에 이전 예약을 해제한다. Global Delay가 먼저 실행될 수 있으므로 이 검사만으로 CF4의 콜백 검사를 대체하지 않는다.
- [x] Scope의 이동·출전·로딩·Intro·팝업 가드를 통해 노드 선택과 드래그/휠/Alt를 함께 차단한다. 자동 이동 중 조작으로 취소하는 기능을 추가하지 않는다. 완료 뒤 눌림/드래그 잔여 입력이 새 선택으로 이어지지 않도록 기존 ClearPointer/Cancel 경계를 사용한다.
- [x] 자동 offset 적용·완료·취소는 EditorPrefs와 저장 구도, 에디터 작업값에 쓰지 않는다. 변경 없는 PreviewFraming 재적용은 offset을 유지한다. 준비된 frame snapshot이 자동 이동 중 다른 에디터 구도로 바뀌지 않게 한다.
- [x] GetCurrentFraming은 실제 `_framing.Position + _axis * _offset`과 현재 회전·렌즈를 값으로 반환하고 상태를 쓰지 않는다. PrepareManualFraming은 편집 가능·소유권 해제를 확인한 뒤 이 구도를 settings의 작업값으로 인계하고 offset을 재기준화한다. 회전/Size를 오래된 작업값에서 가져오지 않는다. FramingReceiver의 유효한 Alt 드래그/휠 직전과 Inspector의 명시적인 편집 시작이 이 경로를 공유하며 false이면 조작하지 않는다.
- [x] Scope의 CanEdit는 현재 SceneRoot 연결·준비 문맥과 CanInteractWithMap을 확인한다. Inspector의 Play 중 Position/Rotation/Size·잠금 해제·확정·취소를 같은 가드로 차단한다. 편집 모드는 기존 경로를 유지하고 Play 중 연결 전/이탈 후에는 편집을 허용하지 않는다. Scope 해제는 자기 Editor 슬롯만 비우므로 이전 Scope가 새 연결을 해제하지 못한다.
- [x] Inspector는 Play 중 ReadCurrent의 실제 구도를 표시하고 **변경 검사로 사용자가 값을 바꾼 경우만** ApplyEdit로 전달한다. ApplyEdit는 값 검증·CanEdit 확인→PrepareEdit→작업값 반영→기존 PreviewFraming 적용을 같은 호출에서 끝낸다. Alt의 실제 이동/휠 변화도 이 경로로 적용하여 다음 Tick 전 확정해도 방금 편집한 값이 사라지지 않게 한다. Size나 회전만 바꾸면 현재 위치를 유지한다. 매 Repaint의 기존 TrySetPose/TrySetSize 쓰기를 제거하고 읽기/표시·자동 완료·단순 Alt 누름에는 저장하지 않는다. 정상 입력 거절은 false, 카메라 적용 예외는 기존 오류 경계로 전달한다.
- [x] Play 중 잠금 해제는 현재 구도로 작업을 시작하며 과거 Confirmed 위치로 순간 이동하지 않는다. 명시적 확정은 PrepareEdit 뒤 현재 편집 구도를 기존 Confirm 경로로 저장한다. 명시적 **조정 취소 및 잠금**만 기존 확정 구도로 돌아가는 의도적 동작으로 유지한다. Settings의 잠금 전환과 PreviewFraming 사이에서 이 순서를 보장하고 자동 이동 도중 누른 편집/확정은 종료 뒤 지연 적용하지 않는다.
- [x] Scope Supersede/Dispose/준비 실패에서 먼저 새 입력·세대 쓰기를 막고, 두 이동의 operation과 Focus를 정리한 뒤 표시·Catalog lease를 해제한다. Supersede는 Release(false)이며 이전 Scope가 새 카메라에 복원하지 않는다. 정리 중 예외는 기존 CleanupErrors/RuntimeFaultBoundary로 전달하며 다른 해제를 계속한다.
- [x] U5~U7에 대응해 최초 Intro, 첫 수동 조작과 Inspector 직접 편집, 자동 이동 중 편집 차단, 같은 번호의 새 Run, 재진입·부분 생성 실패를 추적한다. 설정 창에서는 마커와 카메라가 같은 Delay 시각 정지로 함께 멈추고 이어지는지 사용자 확인에 남긴다.

### CF7 통합 정적 검토와 사용자 확인 인계

**Files:** 이번 Phase 변경 파일, 파일 지도 문서 대상. 게임 코드의 새 기능을 추가하는 Task가 아니다.

- [x] `git diff --check`와 이번 파일들의 diff를 확인한다. 신규 untracked 파일도 별도로 읽고 `.cs`/`.meta` 짝·UTF-8/LF·GUID 보존을 확인한다. `git status`의 Git LFS 실행이 환경에서 실패하면 문서/코드 경로로 조회를 좁혀 확인하며 프로젝트 설정을 바꾸지 않는다.
- [x] `rg -n 'StageMapCameraFocus|StageMapMovementContext|StageMapMovementDelay|IsMovementContextCurrent|BeginScenePreparation|WasSkippedAtPreparation|PrepareManualFraming|IStageMapCameraFramingAccess' Assets/ProjectL/Script`로 계약·호출부를 대조한다. 문맥/Editor 연결 인수 누락, 이전 콜백 쓰기, 완료보다 먼저 열리는 팝업, 보호 영역 변경이 없어야 한다. 이 검색은 컴파일 증거가 아니다.
- [x] 설계 §6의 SSOT·SOLID 5원칙·JMH/FW·변경 독립성을 실제 변경으로 점검한다. 특히 시간/현재 노드/핸들 원본 중복, Factory의 기능 구현, 광범위 서비스 전달, lease 반환 누락, 바인딩 전 사용을 확인한다.
- [x] 사용 가이드에 Camera Focus 설정·전체 이동 시간과의 관계·U1~U8 확인 방법을 추가하고 설계/계획에 코드 완료와 Unity 미확인을 분리해 기록한다. 공용 연결 사항은 공유 메모에 **미전달**로 작성한다.
- [x] 아래 사용자 확인표의 미실행 상태와 **컴파일 미확인**을 보고하고 Phase 1을 멈춘다. 최초 Intro의 기존 미확인 항목도 유지하며 테스트·Unity·커밋·push를 대신 실행하지 않는다.

## 3 사용자 Unity 확인과 Phase 인수

현재 아래 항목은 모두 **미실행**이다. 구현 후 Play From Boot의 기존 정상 진입 설정과 같은 Run/챕터를 이용한다. 기존 개발 조작이나 디버거로만 재현 가능한 조건은 방법과 결과를 별도로 기록하고 시험 도우미를 추가하지 않는다.

| ID | 확인 방법 | 기대 결과 |
|---|---|---|
| U1 | 최초 Intro 종료 후 전투·상점·회복 노드를 각각 선택. 누르기만 하기·배경 드래그·무효 노드도 비교 | 상승부터 하강까지 카메라 동행. 유효 이동에서만 시작하고 두 표시 완료 뒤 덱/팝업 표시 |
| U2 | 기존 개발 상황에서 동일 위치 이동·이미 맞춘 카메라를 확인하고, 디버거 일시 중단 등으로 프레임 지연 관찰 | 카메라 최종 적용과 완료 전달 한 번. 마커 실제 완료 전에 팝업/전투로 넘어가지 않음. 재현 불가 항목은 미확인 |
| U3 | 전투 노드 이동→덱 취소, 출전 확정→수락을 확인. 기존 수단으로 Pending 중 거절/실행 전 취소·중복 관찰도 구분 | 취소 시 클릭 전 마커·카메라 복원. Submit만으로 snapshot 폐기 금지. 수락 뒤 이중 이동·원위치 복원 없음 |
| U4 | 일반 전투 승리→같은 챕터 복귀를 녹화해 가림 해제 첫 프레임 확인. 상점/회복 종료·개발 재그리기·가림 없는 직접 진입도 비교 | 복귀 첫 화면부터 정렬. 보이는 재그리기는 수동 구도 유지. 가림 없는 경로는 자동 복귀 정렬 생략. 핸들 지연은 Pending 유지 |
| U5 | 새 Run/챕터 최초 진입, 같은 챕터 재진입, Camera Focus를 끈 경우를 비교 | 최초 낙하 높이 20·선·보스 왕복·시작/복귀 구도와 입력 차단 유지. 최초 Intro와 Focus가 동시에 카메라를 쓰지 않음 |
| U6 | 먼 곳으로 수동 이동 후 클릭, 완료 후 첫 드래그·휠·Alt와 **Inspector의 Position/Rotation/Size 직접 편집**을 각각 확인. 조정 모드를 미리 연 경우/완료 후 연 경우, 과거 Confirmed 값이 있는 경우, 확정/취소, 창 비율·맵 경계도 비교 | Size/회전만 바꾸면 현재 위치 유지. 잠금 해제·편집 시작은 현재 구도에서 이어짐. 읽기/Repaint/자동 이동은 저장하지 않음. 자동 이동 중 Inspector 조작은 차단되고 뒤늦게 적용되지 않음. 명시적 조정 취소만 확정 구도로 복귀. 범위 제한 뒤에도 대상이 보이거나 구체적 구성 오류가 보고됨 |
| U7 | 상승·수평 이동·하강 중 설정 창 열기/닫기. 기존 경로·디버거로 **같은 챕터/노드 번호를 재사용하는 새 Run**, 같은 Run의 준비 버전 교체, Submit 대기 중 교체, Scope 재연결을 확인. 문맥 변경 뒤 첫 Delay Tick/End와 View Tick 순서를 관찰 | 같은 번호여도 이전 이동은 무효. 이전 Delay 콜백·카메라 적용·완료/취소 복원이 새 표시를 건드리지 않음. 새 표시 재사용 전 이전 예약 정리, 잠금 잔존 없음. 일반 일시정지는 둘 다 함께 정지/재개. 미재현 경로는 미확인 |
| U8 | 운영 SO에서 Enabled와 Player Viewport Y를 바꿔 확인한 뒤 의도한 값 유지. 사용자가 기존 `DataBaseAssetTests` 실행 | Focus 비활성화 시 기존 마커/Intro 동작 유지. Y 변경 반영과 잘못된 값 검증. DB 교차 참조 오류 없음 |

CF1~CF7 완료는 코드 작성·정적 검토·문서 인계 완료를 뜻한다. 사용자 Unity에서 컴파일과 위 동작을 확인한 뒤 기능 인수를 기록한다. 계획 작성만으로 오류 없음이나 구현 완료를 보장하지 않는다.

## 4 계획 자체 대조 기록 — 2026-11-07

- 설계 §1~3의 적용 시점·설정·전 구간 시간·두 완료 조건은 CF1·CF3·CF4와 U1·U2·U8에 배정했다.
- 설계 §4~5의 두 이동 경로·복귀 가림·준비 원인·Pending 복원·세대 정리는 CF2·CF4~CF6과 U3~U7에 배정했다. 가림 없는 개발 직접 진입의 선택적 정렬은 이번 계획에서 생략한다.
- 설계 §6의 범위·SSOT·SOLID·JMH/FW·변경 독립성은 Global Constraints와 CF7에 배정했다. 설계 §7의 사용자 확인은 U1~U8에 대응한다.
- Task 사이의 설정·operation·카메라 세대·준비 버전·메서드 서명을 대조했다. Review Focus 다섯 조건에 담당 Task와 사용자 확인이 있으며 자동 시험·컴파일 성공으로 바꾸지 않았다.
- 사용자 요청으로 검토 2건을 반영했다. **R1:** CF4·CF6에 이동 문맥 캡처/검사 주체와 주입 계약, View Tick 전 확인 및 Delay 콜백 차단을 명시했다. **R2:** CF6에 Inspector 연결 파일·좁은 편집 계약·실제 구도 인계·Repaint 쓰기 제거·잠금/확정/취소 정책을 추가했다. U6·U7과 파일 지도·정적 검색도 함께 갱신했다. Phase 1개·Task 7개와 공용 구현 비수정 범위는 유지한다.
- 위 기록은 구현 전 계획 검토 이력이다. 당시 게임 코드·SO 변경은 없었다. 사용자 Phase 1 승인 뒤 실행 기록은 §5를 따른다.

## 5 Phase 1 실행 기록 — 2026-11-07

- CF1~CF7 코드·정적 검토·문서 인계 완료. 독립 검토 지적 2건을 보완했다. **컴파일 미확인**, U1~U8은 미실행이다. 시험 코드·시험 도우미·MSBuild·Unity 실행·커밋·push는 하지 않았다.
- 작업 시작 시 실제 파일을 별도 로컬 기록과 대조한다. HEAD에는 선행 Intro와 다른 작업자의 변경이 있으므로 이번 Phase 차이로 혼동하지 않는다.
- 설정 Data/SO/운영 에셋, 단일 Catalog 소유권, 두 이동 주체의 전체 시간·두 완료 조건, Delay 콜백 문맥 검사, 가림 여부·Intro 생략 결과, Inspector/Alt 수동 인계를 연결했다.
- `nodeDrop.height=20`, 낙하 0.5초, Intro 시작 대기 0.6초·전개 4초·보스 대기 0.5초·복귀 1.2초, 마커 이동 0.6초는 유지했다. 운영 에셋에는 Camera Focus true/0.35만 추가했다.
- 사용 가이드 §12와 공유 메모를 작성했다. 공유 메모는 **미전달**이다. 최종 검토 결과와 검증 증거는 아래에 이어 기록한다.

### 5.1 정적 확인과 구조 대조

| 확인 | 확인한 내용 / 한계 |
|---|---|
| 파일 | Phase 시작 전 파일과 대조해 코드·에셋·문서 34개가 변경됐다. 새 C# 7개와 `.meta` 7개가 짝을 이루며 기존 `.meta` 변경은 없다. UTF-8/LF와 새 GUID의 중복 없음, `git diff --check` 종료 코드 0을 확인했다. 컴파일 증거는 아니다. |
| 설정·시간 | 운영 에셋 diff는 `cameraFocus.enabled=1`, `playerViewportY=0.35` 추가뿐이다. 두 이동 주체는 같은 `ChessPieceMotionSettings.TotalSeconds`와 기존 `Delay.CurrentTime`으로 카메라 진행률을 전달한다. 별도 시계는 없다. |
| 완료·취소 | 실제 마커 완료를 보존하고 카메라 최종 적용까지 기다린다. Departure는 덱 열림·Submit·Pending에서 토큰을 유지하고 `CompleteAccepted`에서만 확정한다. 기존 Observer 인수 경로가 두 번째 표시 이동을 막는다. |
| 준비·수명 | 로딩 Begin이 가림 원인을 정하기 전에는 Pending이다. Covered + 준비 시 Intro 생략에서만 정렬한다. Run/맵/준비 버전은 View Tick과 Delay Tick/End 직전에 확인하며 무효화 후 자기 자원을 정리한다. |
| Editor | Inspector의 변경 검사와 Alt 조정은 같은 Access를 사용한다. 현재 표시 구도 읽기는 저장하지 않고 명시적인 편집 시작만 작업값을 갱신한다. 자동 이동 중 편집/확정을 차단한다. |
| 연결 검색 | 설정 변환·Factory→Scope→Entry/Departure→Movement의 생성자와 Focus/Context/Delay/준비/Editor 계약 호출부를 `rg`로 대조했다. 새 호출 인수를 요구하는 기존 시험은 검색에서 발견되지 않았다. |

| 구조 기준 | 실제 연결 대조 |
|---|---|
| SSOT | 진행 원본은 Buffer, 선택 전 목적지는 Departure, 시간은 기존 Delay, 설정은 SO, Catalog 핸들/offset은 CameraDrag다. Context는 원본 진행을 변경하는 저장소가 아니라 시작 문맥 비교값이다. |
| SRP·OCP | Focus는 계산·진행률·토큰 수명, 이동 주체는 두 표시의 완료 결합, Scope는 준비·수명·입력 연결, Factory는 생성·주입을 맡는다. 공용 서비스에 분기를 추가하지 않았다. |
| LSP·ISP·DIP | 기존 마커 실제 완료·요청 수락·로딩 Pending 의미를 유지한다. Focus에는 좁은 카메라 계약, 이동에는 문맥 delegate를 주입한다. Focus가 Services·Buffer 전체를 받거나 직접 씬을 검색하지 않는다. |
| JMH/FW·등록 계층 | StageMapSceneFactory가 내부 부품을 생성해 Scope에 주입한다. 새 Builder 등록·Module·상위 Export·루트 resolver 조회가 없다. 기존 Global 경로의 Delay와 CameraDrag의 Catalog Handle을 사용하며 바인딩 후 실행·해제 후 반환 순서를 유지한다(FW-004/011/016). |
| 변경 독립성 | 목표 높이와 보간은 Focus/Data, 클릭 전 수락 정책은 Departure, 준비 시점은 Scope/LoadingPreparation에 한정한다. Core·Buffer·FieldCamera·Architecture·공용 Camera/Loading/Delay·ChessPieceMotion·전투 View·노드/선 표현 구현은 이번 Phase에서 수정하지 않았다. |

이 표는 코드 경로의 정적 대조다. 프레임 지연·실제 로딩 첫 화면·씬 재연결·Inspector 조작의 재생 결과는 U1~U8에서 별도로 확인한다.

### 5.2 독립 검토와 수정

새 문맥의 읽기 전용 검토 결과는 **Important 2건, Critical·Minor 없음**이었다. 작성자가 두 지적의 호출 순서를 확인해 수용하고 한 차례 보완했다. 아래 수정 후 확인은 작성자의 정적 대조이며 독립 재검토나 실행 시험 결과가 아니다.

| 지적 | 원인 | 보완 및 정적 확인 |
|---|---|---|
| R1 무효 이동의 표시 잠금 잔존 | Motion/Focus만 해제하면 Display의 이동 목적지가 남아 같은 표시의 다음 이동/구조 교체가 실패한다. | NodeMovement와 Departure가 진행 중이던 자기 이동을 무효화/폐기할 때 Delay→Motion→Focus 정리 후 기존 `Display.Clear`를 호출한다. 새 정상 착지·SetCurrentNode는 게시하지 않는다. 일반 덱 취소는 기존 `CompleteMarkerMove`·클릭 전 복원 경로를 유지한다. `Display.Clear`의 이동 목적지 초기화와 다음 `Show` 경로를 대조했다. |
| R2 준비 완료 전 수락 관찰 | 재그리기 후 남아 있는 수락 결과를 Observer가 먼저 전달해, 아직 준비되지 않은 문맥 캡처가 실패한다. | Scope의 `_isShown=false` Tick은 준비만 진행하고 반환한다. 다음 유효 Tick에서 관찰을 재개하며 정상 표시 중의 관찰→이동→갱신 순서는 유지한다. 외부 호출 뒤 준비 문맥이 바뀌면 같은 Tick의 후속 이동도 중단한다. Pending·준비 완료·다음 Tick의 순서를 대조했다. |

검토자가 범위 밖으로 둔 항목은 다음처럼 처리했다.

- 기존 FMOD·FBX와 다른 작업자의 변경은 이번 Phase 이전 기준선에 해당하므로 검토 범위에서 제외한다. 이들 변경의 기존 문제는 이번 검토로 발견됐다고 보장하지 않는다.
- 선행 Intro·공용 Camera/Loading/Delay/ChessPieceMotion은 이번 연결 경계를 검토하고 전체 구현·재생 품질은 재검증하지 않는다. 기존 기능의 전체 정상 여부는 이번 정적 검토로 보장하지 않으며 선행 확인 대기와 U1~U8을 유지한다.

남겨 둔 Minor 지적은 없다. 보호 파일·시험 코드를 추가 수정하지 않았고 수정 후 `git diff --check`도 종료 코드 0이었다. **CF1~CF7 코드·정적 검토·문서 인계 완료**, **컴파일 미확인**, 사용자 U1~U8 인수 대기 상태에서 Phase 1을 종료한다. 공유 메모는 미전달이며 커밋·push는 사용자가 직접 한다.
