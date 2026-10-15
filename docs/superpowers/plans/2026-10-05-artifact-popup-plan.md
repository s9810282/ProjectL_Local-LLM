# 아티팩트 팝업 계획

> **2026-10-06 최신 변경:** 사용자 요청으로 Artifact/Close와 아티팩트 ESC 닫기 연결을 제거했습니다. 팝업은 화면의 뒤로가기 버튼으로 닫습니다. Common/OpenSettings의 ESC 바인딩은 유지합니다. 아래 ESC 구현·검증 기록은 변경 전 이력입니다. 이번 변경 후 실제 Play 재검증은 별도입니다.

- 작성일: 2026-10-05
- 작성: View 작업(스테이지 선택·아티팩트 담당)
- 브랜치: `feature/Hojin/Stage_Shop_Event`
- 근거
  - 천명님 [아티팩트 Framework·View 인계](../handoffs/2026-10-03-artifact-framework-view-handoff-ko.md)(E3-1~E3-8)
  - [Framework 프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~003·014·016·101·204, DR-008·DR-010·DR-011
  - [런타임과 씬 연결 안내](../viewDocs/runtime-scene-binding-guide.md) §2·§6
  - 전투 진입 요청 선례: `LifeCycleSystem/LifeCycles/Combat/CombatEntryRequest.cs`
  - [스테이지 선택 View 계획](2026-10-03-stage-map-view-plan.md)(같은 씬, 입력 Layer 규칙 D6)
- 진행·검증 상태는 이 문서 §9 한 곳에만 적는다.

## 1. 범위

**포함**
- StageSelect 화면의 버튼으로 아티팩트 팝업을 열고 닫는다.
- 보드·Pool·배치 아이템을 Buffer 기준으로 표시한다.
- 배치·이동·반환·Q/E 회전·프리뷰 입력을 Plan·Job으로 Core에 전달하고 결과를 표시한다.
- 첫 보드 초기화(처음 크기는 InGame DB).

**제외**
- 보상으로 보드 확장(`ExpandBoard` 호출 경로). 보상 작업에서 연결한다. 표시는 Buffer의 현재 크기를 따르므로 확장 후에도 그대로 동작해야 한다.
- 이펙트·연출, 아이템 효과 수치 표시.
- UISystem 등록은 처음에 제외했으나 2026-10-06 D1 변경으로 범위에 들어온다. 옮기는 설계·Task는 아직 없다.

## 2. 결정 (사용자 결정, 2026-10-05)

| 번호 | 결정 | 근거 |
|---|---|---|
| D1 | **2026-10-06 변경(사용자 결정): 모든 UI는 현강 작업자의 UI 구조·문서를 따른다.** 3D 보드·모델·World Space 배경은 View(WorldRoot)에 그대로 둔다. 팝업 UI(열기·닫기 버튼, 보유 목록, 이름·효과·상태 문구, 클릭 차단)는 `UIBase`(FW-202·DR-010)와 `UIInputBinding`/`IUIInputReceiver`([Assets/AGENTS.md](../../../Assets/AGENTS.md): `Button.onClick` 리스너 금지)로 옮긴다. 옮기는 설계는 [UI 이전 설계](../specs/2026-10-06-artifact-popup-ui-migration-design.md)(초안, 검토 대기)이며, 현재 코드는 아직 아래 옛 방식이다. **옛 결정:** UI 목록·버튼·문구·투명 클릭 차단은 StageSelect Overlay Canvas에 두고 UISystem에 등록하지 않는다 | **변경 근거:** 현강 `efb04eea`(2026-10-05)부터 GameBootstrap이 Stage·Combat 씬 로드 때(SceneEnterLoaded, -200) `BindScene()`을 호출한다([Script README](../../../Assets/ProjectL/Script/README.md) 56행). 아래 옛 근거의 "호출하는 곳이 없다"는 더 이상 사실이 아니고, 옛 근거가 정한 UIBase 전환 조건도 충족됐다. **옛 근거:** `UISystem.BindScene()`은 일반 UI 재수집을 지원하지만(DR-010), 씬 로드 뒤 이를 호출하는 곳이 없다(현재 호출은 GameBootstrap의 Boot 1회). 전투 HUD와 같은 방식(씬 연결 안내 §6). UISystem이 Loading 노드(SceneEnterLoaded·AfterLoad)에서 씬 UI를 다시 찾게 되면 `UIBase`로 옮긴다(가이드 §10 "LifeCycle과 로딩": 화면별 바인딩은 Loading 노드가 담당) |
| D2 | StageSelect의 **버튼**으로 연다. **열기:** `Artifact` Layer로 바꾼다. **닫기:** 현재 Layer가 `Artifact`일 때만 `StageSelection`으로 되돌린다(다른 화면이 가져간 Layer는 덮어쓰지 않음, 2026-10-05 보완). **씬 이탈(Detach):** 현재 Layer가 `Artifact`일 때만 `ClearLayer`하고 `StageSelection`을 다시 켜지 않는다 | 팝업 중 맵 클릭 차단은 Layer로 한다(스테이지 계획 D6). UISystem은 Layer를 바꾸지 않는다. Detach에서 복귀하면 다음 화면의 Layer를 덮어쓴다 |
| D3 | Job은 `ArtifactPlan`이 만들고 `JobRunner.Register`로 등록한다. **공용 `JobRegisterSystem`은 고치지 않는다.** `JobRegisterSystem`이 등록한 기존 `ArtifactPlan.Register(service, jobType)` 서명은 유지하고(현재 `JobRootType.Artifact`를 등록하는 호출자는 없다), 요청용 생성 메서드를 따로 추가한다 | `JobRunner.Register`는 가이드의 공개 API(§5). 다른 작업자 파일과 충돌하지 않고 기존 컴파일 경로도 깨지지 않는다 |
| D4 | **공용 EventBuse를 쓰지 않는다.** 요청 결과는 요청 쪽에서, 화면 상태는 Buffer에서 읽는다. UI 슬롯의 포인터 의도만 Presenter → 시스템의 지역 콜백으로 전달하고 Attach/Detach에서 연결·해제한다 | 구독 해제가 없고 예외가 격리되지 않는다(DR-008). 열린 다른 화면이 아티팩트 변경에 즉시 반응해야 할 때 다시 검토한다 |
| D5 | 아이템·칸 View는 **CustomPool**로 재사용한다. 빌려준 목록은 소유자가 관리하고 닫을 때 모두 반환한다 | FW-204 허용, FW-014 입력 계약, 중복 반환 검사 없음(DR-011) |
| D6 | 처음 보드 크기는 InGame DB `ArtifactBoardConfig`(기본 4×4)에서 읽는다. 현재 크기는 항상 Buffer(`Current.BoardWidth/Height`)에서 읽는다. 코드에 크기 상수를 두지 않는다 | 보상으로 늘어날 수 있다. 규칙 값은 InGame DB(P1·P6), 한 사실은 Buffer 한 곳 |
| D7 | StageSelect에 들어올 때마다 초기화 Job이 `InitializeBoard`를 부른다. **설정은 실행 계층이 읽는다:** `StageLifeCycle`이 InitData로 받은 DB에서 **별도 인터페이스 `IHasArtifactBoardConfig`**(설정 조회 하나만)를 꺼내 보관하고, `OnEnter`에서 그 값으로 초기화 Job을 등록한다. **`IInGameQuery`에는 추가하지 않는다.** 설정이 없는지는 InitData가 아니라 **`OnEnter`에서** 확인하고 없으면 `InvalidOperationException`. View 시스템은 InGame DB를 읽지 않는다 | 두 번째 호출부터 `false`를 돌려주고 상태를 보존한다(인계 §7). 지금 실제 게임 경로에서 호출하는 곳이 없다(`TemporaryCoreRuntimeFactory`의 개발용 4×4만 있음). LifeCycle 그룹은 InitData를 모듈에 전달한다. `IInGameQuery` 구현체가 9개(테스트용 DB 7개 포함, 다른 작업자 파일)라 추가하면 모두 고쳐야 한다. `CombatEntryIntegrationTests`가 LifeCycle 그룹에 테스트 DB를 넣으므로 InitData에서 요구하면 그 테스트가 깨진다. P6·FW-101 |
| D7b | **초기화 Job:** Priority 2(맵 준비 `StageJob` 0/1 다음). `InitializeBoard`가 `true`면 완료. `false`면 `Current.IsInitialized`를 확인해 true면 정상 완료(이미 초기화됨), false면 `InvalidOperationException` | `JobRunner`는 Priority로 정렬한다(`JobPriorityComparer`) |
| D7a | `Current.IsInitialized`가 false인 동안 팝업은 열 수 있지만 편집·프리뷰 입력은 막는다(요청을 만들지 않는다) | 초기화 Job은 등록 뒤 다음 Tick에 실행된다. 초기화 전 편집·프리뷰는 Core 예외다(인계 §7) |
| D8 | 프리뷰는 최신 하나만 쓴다. **대기 중인 프리뷰 Job은 최대 1개:** 대기 중 프리뷰가 있으면 새 Job을 쌓지 않고 "다음에 실행할 대상"만 최신 값으로 바꾼다(그 Job이 실행될 때 최신 대상을 쓴다). **무효화 조건:** 편집 요청 생성·편집 완료, 회전 변경, 드래그 종료·취소, 닫기, 다시 열기, Detach 때 이전 프리뷰 결과는 표시하지 않는다. 드래그 대상 인스턴스가 반환·이동된 뒤 남은 `PreviewMove`는 실행하지 않는다 | 인계 §6: 프리뷰에 요청 번호가 없다. 개별 Job 제거가 없어 교체마다 Job을 쌓으면 취소된 Job이 큐에 남는다. 반환된 인스턴스의 `PreviewMove`는 예외는 아니지만(`InstanceNotFound` 프리뷰) 의미 없는 호출이다 |
| D8a | **취소 규칙:** Runner에는 개별 Job 제거가 없으므로, 취소된 요청의 Job은 실행될 때 Core를 부르지 않고 완료한다. `JobRunner.Clear()`는 쓰지 않는다(다른 화면 Job까지 지운다). 상황별 처리: **대기 중 요청** → 취소 표시. **실행 중 요청** → Job은 동기로 끝나므로 결과를 받은 뒤 화면 반영만 버린다. **닫기** → 대기 중 프리뷰는 취소, 대기 중 편집은 그대로 실행하고 결과는 다음에 열 때 Buffer로 반영. **Detach** → 대기 중 프리뷰·편집 모두 취소 | `IJobRunner`는 `Register`·`RegisterRange`·`IsIdle`·`Clear`만 제공한다 |
| D8b | 요청은 생성 후 대상(종류·ID·칸·회전)을 바꾸지 않는다. 상태와 결과만 Job·소유자가 바꾼다. Job에는 `IArtifactEditingService`와 필요한 ReadSink(`ArtifactEditResultReadSink`·`ArtifactPreviewReadSink`·`ArtifactReadSink`)만 전달한다 | 전투 `CombatEntryRequest`와 같은 구조, FW-101 |
| D9 | 편집 가능 여부(전투 보존 중 잠금 등)는 Core가 판단한다. View는 결과의 실패 사유만 표시한다 | 인계 §7, P6 |
| D10 | 편집 요청은 한 번에 하나만 처리한다. 처리 중 들어온 편집 입력은 무시한다 | `LastEditResult`는 마지막 값 하나다(인계 §5) |
| D11 | **오류 상태 거부:** 런타임 오류 경계가 오류 상태(`RuntimeFaultBoundary.IsFaulted`)면 팝업 열기 버튼, 입력 수신, 요청 생성·등록을 모두 무시한다. `ArtifactViewSystem`은 `InGameViewSystemFactory`가 이미 받는 같은 경계 인스턴스를 읽기용으로 받는다(콜백 연결은 하지 않는다) | 오류 시 Bootstrap은 Tick만 멈춘다. 입력은 Unity `actionTriggered` 콜백, 버튼은 EventSystem으로 Tick과 무관하게 계속 불려 실행되지 않을 Job이 쌓인다 |
| D12 | **UI 클릭 규칙:** 알파 0인 Overlay 차단 Image(Raycast Target)가 뒤쪽 StageSelect UI 클릭을 막는다. 3D 배경은 별도 World Space Canvas로 그린다. 열린 상태에서 열기 버튼은 무시한다. 포인터가 팝업의 버튼 위에 있을 때 끝난 드래그는 배치·이동·반환 요청을 만들지 않는다(닫기 버튼 위에서 놓으면 닫기만 된다) | 입력 Layer는 Input System 맵만 켜고 끈다. Canvas 버튼은 EventSystem 경로라 Layer로 막히지 않는다 |
| D13 | **씬 활성 상태:** 팝업 Canvas와 `ArtifactSceneRoot`는 항상 켜 두고, 안쪽 표시 Panel만 켜고 끈다 | `FindFirstObjectByType`은 기본값으로 비활성 오브젝트를 찾지 않는다 |
| D14 | **두 갱신을 따로 검사:** (1) 보드·아이템은 `Current` 스냅샷 참조가 바뀌었을 때만 다시 그린다. (2) 결과 문구는 `CurrentEdit`이 아직 표시하지 않은 끝난 요청일 때 갱신한다. 표시한 요청을 기억하므로 닫혀 있는 동안 끝난 편집도 다시 열 때 표시된다 | 편집이 거절되면 Buffer는 기존 스냅샷을 그대로 두고 결과만 게시한다(`ArtifactBuffer.PublishEdit`) |
| D15 | **닫기와 Detach 정리 분리:** 닫기는 `InvalidatePreview()`(대기 편집은 계속 실행), Detach·Shutdown은 `CancelPending()`(대기 편집까지 취소). 반복 닫기·Detach는 아무것도 하지 않는다 | D8a 계약 유지 |
| D16 | **풀 규칙:** 풀은 씬 Presenter(`ArtifactPopupView`)가 소유하고 씬과 함께 사라진다. 빌릴 때·돌려줄 때 아이콘·부모·활성 상태를 초기화한다. 빌려준 목록을 Presenter가 관리해 중복 반환을 막고, 그리는 중 예외가 나도 이미 빌린 View를 모두 돌려준다 | `CustomPool.Return`은 비활성화·초기화·중복 검사를 하지 않는다(FW-014, DR-011) |
| D17 | **아이콘 조회 위치:** `ArtifactViewSystem`이 View DB(`ArtifactViewTable`)에서 아이콘을 조회해 표시 값에 담아 넘긴다. Presenter는 DB·Buffer를 읽지 않고 받은 값으로 그리기만 한다 | 역할 일관성. View DB 조회는 허용(P5) |
| D18 | **초기화 전 표시:** `Current.IsInitialized`가 false면 칸을 만들지 않고 "준비 중" 문구만 보인다. 초기화된 스냅샷이 게시되면 D14 (1)로 그린다 | D7a |
| D19 | **프리뷰 요청 생략과 갱신 범위(2026-10-06):** 드래그 중 칸·회전·대상이 직전 요청과 같으면 프리뷰를 다시 요청하지 않는다. 드래그 중에는 움직이는 모델과 프리뷰 표시만 갱신하고 보드·목록 전체를 다시 그리지 않는다. 매 프레임 검색·생성을 하지 않고 카메라·DB 참조는 연결 때 보관한다 | 성능 기본 원칙. 스케줄러는 대기 Job 수만 제한하므로 같은 대상 반복 요청은 입력 쪽에서 막는다 |

## 3. 결정 기록과 협의 대기

- **오류 시 입력 정리(협의 대기):** `RuntimeFaultBoundary.ConnectStopInput`은 콜백 하나만 받고, 이미 `CombatViewSystem.StopInput`이 연결한다(`InGameViewSystemFactory`). 팝업·맵이 따로 연결하면 예외다. 경계가 여러 정리를 받거나 공용 정리로 묶는 방식은 용근님(B1 소유)과 정한다. 그 전까지 팝업은 닫기·Detach·Shutdown 정리를 구현하고, 오류 상태에서는 열기·입력·요청 등록을 스스로 거부한다(D11). 콜백 협의 전까지 남는 제약은 오류 정지 순간 화면에 있던 드래그·프리뷰 표시가 지워지지 않는 것뿐이다(스테이지 계획 §8.5와 같은 문제).
- **Q1 요청 전달 수단 → A(요청 객체)로 결정(2026-10-05).** 아래는 검토 기록이다. 9/30에 만든 `ArtifactViewBuffer`(Place·Move·Return 번호 요청)가 있지만 아직 쓰는 곳이 없다. 두 수단을 함께 두면 요청 경로가 둘이 되므로 하나만 쓴다.
  - **A. 요청 객체(권장):** 전투 `CombatEntryRequest`처럼 요청이 대상·상태·결과를 함께 가진다. Job이 Core 호출 직후 `LastEditResult`/`LastPreview`를 읽어 그 요청에 넣으므로 결과와 요청이 바로 짝지어진다. 프리뷰 교체(D8)도 요청 단위로 처리된다. 이 경우 `ArtifactViewBuffer`는 사용하지 않으며, 지울지는 따로 정한다(지금 지우지 않는다).
  - B. `ArtifactViewBuffer`: 스테이지 `StageViewBuffer`와 모양이 같다. 대신 결과를 둘 곳이 없어 번호→결과 표를 따로 만들어야 하고, 프리뷰 종류도 Buffer에 추가해야 한다.

## 4. 구조

```text
[StageSelect 씬]                                  [런타임, Boot에서 한 번 생성]
ArtifactSceneRoot ── 팝업 Canvas, 열기·닫기 버튼    ArtifactViewSystem (InGameViewSystemFactory 등록)
  └─ ArtifactPopupView (그리기만)   ◄── 표시 값 ──   ├─ ArtifactInteraction (순수 C#, IArtifactInputReceiver)
                                                     ├─ ArtifactBoardMapper (화면 좌표 → CellCoord)
                                                     └─ 요청 생성 → ArtifactPlan → Job → JobRunner.Register
Job ──► CoreGroup.Artifacts (Initialize/Place/Move/ReturnToPool/Preview*) ──► ArtifactBuffer
```

| 구성 | 종류·위치 | 맡는 일 | 하지 않는 일 |
|---|---|---|---|
| `ArtifactSceneRoot` | MonoBehaviour, `ViewSystem/ArtifactPopup/` | 팝업 View·버튼·출력 Camera 참조 | 동작 |
| `ArtifactPopupView` | MonoBehaviour | 칸·Pool·배치 아이템·실패 문구 그리기, 풀에서 View 대여·반환 | 규칙 판단, Buffer·Core 접근 |
| `ArtifactViewSystem` | 일반 C#, 모듈 등록 | 씬 Attach/Detach, 버튼 연결, 열기·닫기와 Layer 전환, 입력 슬롯 연결, 요청 등록, 요청 상태를 보고 표시 갱신 | 배치 계산 |
| `ArtifactInteraction` | 순수 C# | 클릭·드래그·회전·취소를 배치·이동·반환·프리뷰 의도로 바꾼다 | 칸 계산, Core 호출 |
| `ArtifactBoardMapper` | 일반 C# | 보관한 출력 Camera의 Ray → WorldRoot 로컬 XZ 평면 → 논리 칸. Presenter와 중심·높이·간격 공유 | 배치 규칙 계산 |
| `ArtifactEditRequest` | 일반 C# (`LifeCycles/Artifact/`) | 생성 후 불변인 대상(종류, 정의 ID 또는 인스턴스 ID, 칸, 회전) + 상태·결과 | 대상 변경 |
| `ArtifactPlan` | static (기존 파일에 추가) | 기존 `Register(service, jobType)` 유지. 요청·초기화용 Job 생성 메서드 추가 | — |
| `StageLifeCycle` (기존) | 실행 계층 | `ArtifactBoardConfig` 조회를 받아 Enter 때 초기화 Job 등록(D7) | 표시 |
| Job 3개 | `LifeCycles/Artifact/Jobs/` | 초기화·편집·프리뷰. Core를 한 번 호출하고 결과를 기록. 정상 거절도 완료 | 재시도 |

- `ArtifactLifeCycle`(Artifact 씬 4번 예약)은 쓰지 않는다. 팝업은 StageSelect 안에 있다.
- 입력 부품(`ArtifactInputController`·`ArtifactInputSlot`·`ArtifactInputLayers`, Q/E 키)은 이미 있다.
- 아이콘 등 외형은 View DB `ArtifactViewTable`(ArtifactId, DisplayName, Icon, Prefab)에서 읽는다.
- 정리 규칙(D2·D8a): 닫기 때 드래그·프리뷰 정리, 풀 반환, `StageSelection` 복귀. Detach 때 입력 슬롯 해제, 대기 요청 취소, 풀 반환, 현재 Layer가 `Artifact`일 때만 `ClearLayer`(복귀하지 않음).

## 5. 요청 하나의 흐름

1. 입력 → `ArtifactInteraction`이 의도를 만든다(예: 정의 3을 칸 (1,2)에 R90으로 배치). `Current.IsInitialized`가 false면 만들지 않는다(D7a).
2. `ArtifactViewSystem`이 요청을 만들고 `ArtifactPlan`으로 Job을 만들어 `JobRunner.Register`.
3. Job이 실행되면 요청이 취소 상태인지 먼저 본다. 취소면 Core를 부르지 않고 완료한다(D8a). 아니면 Core를 한 번 호출하고, 정상 반환이면 `LastEditResult`(편집) 또는 `LastPreview`(프리뷰)를 그 요청에 기록하고 완료한다. 예외면 요청을 오류로 표시하고 예외를 그대로 전파한다(런타임 오류 경계).
4. `ArtifactViewSystem`은 Tick에서 요청 상태를 보고, 끝났으면 Buffer의 최신 `Current`로 팝업을 다시 그리고 실패 사유를 표시한다.

요청 상태: 생성 → 대기 → 실행 → 완료(성공/거절) · 오류 · 취소(실행 전 프리뷰 교체, 팝업 닫기).

## 6. DB

- `ArtifactBoardConfig`(InGame, 설정형: 항목 정확히 1개, `CategoryConfig`와 같은 방식): `initialWidth`, `initialHeight`(기본 4, 1 이상).
- 순수 Data·SO 파일 분리(P1·P2), `ArtifactBoardConfigTable : IArtifactBoardConfigTableQuery`.
- 노출은 **`IInGameQuery`가 아닌 별도 인터페이스** `IHasArtifactBoardConfig { IArtifactBoardConfigTableQuery ArtifactBoardConfig { get; } }`로 한다(D7). 실제 DB(`InGameDataBase`)와 DB 그룹이 이를 구현한다. 테스트용 DB는 고치지 않는다.
- 고치는 공용 DB 등록 파일(로직 변경 없음, 테이블 추가 때 늘 고치는 곳): `InGameDataBase`, `InGameDataBaseFactory`, `DataBaseGroupServiceKey`(키 한 줄), `InGameGroup.asset`. 다른 작업자의 로직·테스트 파일은 고치지 않는다.
- 검증: `DataBaseAssetTests`, 새 Table 테스트, 실제 런타임 조립에서 `StageLifeCycle`까지 값 전달.

## 7. Phase·Task

Phase가 끝나면 멈추고 보고한다.

**Phase A — 보드 초기화**
- [x] A0 `ArtifactViewSystem` 골격: StageSelect Enter(100)/Exit(-100), SceneRoot 연결·해제(표시·입력 없음), Factory 등록
- [x] A1 `ArtifactBoardConfig` Data·SO·Table·Query·키, `IHasArtifactBoardConfig`(D7·§6), InGame 그룹 등록, 에셋 생성(4×4)
- [x] A2 초기화 Job(Priority 2, `false` 처리 D7b), `ArtifactPlan`에 초기화 Job 생성 추가(기존 `Register(service, jobType)` 서명 유지)
- [x] A3 `StageLifeCycle`: InitData에서 `IHasArtifactBoardConfig` 보관(없어도 통과), `OnEnter`에서 없으면 예외·있으면 초기화 Job 등록(기존 맵 준비 등록 유지)
- [x] A4 테스트: 초기화 1회 성공, 재진입 시 `false`이고 상태 보존 후 정상 완료, `false`인데 미초기화면 예외, 맵 준비보다 뒤에 실행, DB 값 사용, 설정 없는 DB로 조립해도 InitData 통과·Stage 진입 때만 예외, View 쪽이 InGame 조회를 받지 않음
- [x] A5 실제 런타임 조립 검증: `FrameworkRuntimeFactory`로 조립한 런타임에 실제 DB를 넣었을 때 `StageLifeCycle`이 설정을 받는지. 기존 `CombatEntryIntegrationTests`·`ChapterGraphPreparationTests`·`FrameworkRuntimeCompositionTests` 회귀

**Phase B — 요청과 Job (Unity 없이 검증)**
- [x] B1 `ArtifactEditRequest`(대상 불변, 상태·결과)와 상태 전이
- [x] B2 편집 Job(Place·Move·Return), 프리뷰 Job(PreviewPlace·PreviewMove). Job에는 `IArtifactEditingService`와 필요한 ReadSink만 전달
- [x] B3 프리뷰 최신 하나·대기 Job 최대 1개·무효화 조건(D8), 편집 한 번에 하나(D10), 취소된 요청은 Core 호출 없이 완료(D8a), 오류 상태면 요청 생성·등록 거부(D11)
- [x] B4 테스트: 성공·정상 거절 1회 완료, 예외 시 오류 상태, 취소된 요청은 Core 미호출, 연속 프리뷰에도 대기 Job 1개, 편집 후·회전 후 이전 프리뷰 무시, 반환된 인스턴스의 `PreviewMove` 미실행, 오류 상태에서 등록 0, 프리뷰 교체·늦은 결과 무시, 0 ID·같은 정의 여러 개·반환 뒤 새 인스턴스 ID·절대/로컬 셀(E3-6)

**Phase C — 팝업 표시**
- [x] C1 `ArtifactSceneRoot`·`ArtifactPopupView`, StageSelect 씬 배치(Overlay UI + World Space 배경 + 3D WorldRoot; 에셋·씬 연결 포함)
- [x] C2 버튼 열기·닫기, Layer 전환(D2), 맵 입력 차단, 초기화 전 입력 차단(D7a), 오류 상태 거부(D11), UI 클릭 규칙(D12: 배경 Raycast 차단, 열린 상태 열기 무시)
- [x] C3 CustomPool로 칸·아이템 View 대여·반환, Buffer 기준 그리기
- [x] C4 닫기·Detach·Shutdown 정리: 드래그·프리뷰 표시 제거, 대기 요청 취소(D8a), 풀 전부 반환, 입력 슬롯 해제
- [x] C5 테스트(2026-10-05 보완 포함): 편집 거절로 스냅샷이 유지돼도 결과 문구 갱신, 닫힌 동안 끝난 편집이 다시 열 때 표시, 다른 Layer로 바뀐 뒤 닫기·Detach가 그 Layer 유지, 그리는 중 예외·반복 닫기·Shutdown에서 풀 반환 누락·중복 없음, 초기화 전 "준비 중". 닫기 → `StageSelection` 복귀, Detach → `Artifact` Layer일 때만 Clear·복귀 없음, 다른 화면 Layer 유지, 풀 전부 반환, 닫힌 동안 끝난 편집은 다음 열기 때 Buffer로 반영, 열린 상태에서 열기 연타 무시, 오류 상태에서 열기 무시, 반환 뒤 Buffer의 새 Pool 인스턴스 ID로 다시 그림·배치 아이템은 절대 셀, Pool·프리뷰는 로컬 셀 + Anchor로 그림(B4에서 옮김, E3-6)

**Phase D — 입력**
- [x] D1 `ArtifactBoardMapper`(Attach 때 받은 카메라 Ray → WorldRoot 보드 평면 → 칸, 배치 아이템은 Buffer 절대 셀 비교; UI 차단 배경은 드래그 차단 대상에서 제외)
- [x] D2 `ArtifactInteraction`: Pool에서 끌어 배치, 배치된 아이템 이동, 하단 보유 목록 영역으로 끌어 반환(그 밖의 화면·버튼 위 해제는 취소), Q/E 회전, 우클릭·강제 종료 취소, 끄는 동안 프리뷰 요청. **2026-10-06 변경:** 우클릭은 회수로 바뀌었다(배치물 드래그 중이면 그 배치물, 아니면 포인터 아래 배치물 Return; 보유 목록 드래그 중이면 임시 선택만 정리). 강제 종료·닫기는 계속 취소다. 원본은 [상호작용 설계 §4](../specs/2026-10-06-artifact-popup-interaction-design.md)
- [x] D3 테스트: 입력 순서별 의도, 회전 누적, 취소 시 요청 없음, 버튼 위에서 놓은 드래그는 요청 없음(D12), 오류 상태에서 입력 무시(D11)

- [ ] D4 공용 오류 정리 연결: 전투 작업자와 단일 정리 콜백의 소유권·해제·Shutdown 순서 협의 후 조립. 아티팩트 자체 `StopInput`은 구현·검증됐지만 자동 연결은 보류

**Phase E — Play 확인**
- [ ] E1 버튼으로 열기·닫기, 열린 동안 맵 클릭 안 됨
- [ ] E2 배치·이동·반환·회전, 충돌·범위 밖 실패 문구
- [ ] E3 StageSelect 재진입 뒤 보드·배치 유지
- [ ] E4 Player 빌드에서 CPU/GPU frame time·GC.Alloc·GC spike 측정(기기·CPU·해상도·VSync 조건 기록). GTX 1060 250~300 FPS는 검증 전 목표
- [ ] E4 성능 측정(2026-10-06 추가): 실제 빌드에서 팝업 열기·드래그·닫기 구간의 CPU/GPU 시간과 GC 할당을 Profiler로 기록한다. 목표 FPS(GTX 1060 기준 250~300)는 참고값이며 보장 기준이 아니다. 팝업만이 아니라 CPU·해상도·렌더 설정을 함께 기록한다

## 8. 인계 E3 대응

| 인계 항목 | 처리 |
|---|---|
| E3-1 초기화 주체·시점·크기 출처 | D6·D7, Phase A |
| E3-2 의도·ID·좌표·회전 전달 | Q1, Phase B·D |
| E3-3 한 번 실행, 성공·거절·예외 구분 | Phase B |
| E3-4 결과와 요청의 대응, 프리뷰 교체 | Q1·D8·D10, Phase B |
| E3-5 팝업 열기·닫기, 입력 중단, 지연 Job | D1·D2, Phase C |
| E3-6 최신 Buffer로 표시, ID·셀 주의 | Phase B·C |
| E3-7 잠금·재고 없음·범위·충돌 실제 경로 | Phase E |
| E3-8 실제 DB·화면 검증 | Phase E |

남은 협의: 카메라 목록(민형님)이 Loading 노드에서 자기 씬 오브젝트를 다시 찾도록 요청(런타임 전체 BindScene 재호출이 아님, DR-001·DR-010; UISystem 쪽은 현강 `efb04eea`로 해결됨), 팝업 UI의 UIBase 이전 방식(D1 변경, 현강님 UI 규칙 기준), 오류 시 입력 정리 콜백(§3, 용근님), 보상 확장 연결 시점, 쓰지 않는 `ArtifactViewBuffer`의 유지·삭제.

## 9. 진행 기록

| 날짜 | 내용 |
|---|---|
| 2026-10-05 | 계획 작성. Q1 결정 대기 |
| 2026-10-05 | 외부 검토 반영(9건 모두 코드로 확인): `ArtifactPlan.Register` 서명 유지(D3), 닫기·Detach Layer 규칙 분리(D2), 초기화 전 입력 차단(D7a), 개별 Job 제거 없음에 따른 취소 규칙(D8a), DB 조회 주체를 `StageLifeCycle`로(D7), D1 표현 정정, Canvas Overlay 명시, 요청 불변·Job 최소 의존(D8b), 오류 시 입력 정리 협의 대기(§3). Q1은 A(요청 객체)로 결정. 구현 전 |
| 2026-10-05 | 2차 외부 검토 반영(7건 코드로 확인): 설정 조회를 `IInGameQuery`가 아닌 `IHasArtifactBoardConfig`로 분리하고 OnEnter에서 확인(D7, §6), 초기화 Job Priority 2·`false` 처리(D7b), 프리뷰 대기 Job 최대 1개·무효화 조건(D8), 오류 상태 거부(D11), UI 클릭 규칙(D12), Phase A에 런타임 조립 검증(A5) 추가. 현황 정정: 실패 테스트 2개는 `75460fc8`에서 수정됨(재실행으로 확인 필요), 전투 진입 전환은 `CombatLifeCycle`이 이미 `Change(Combat)` 호출. 구현 전 |
| 2026-10-05 | Phase A 일부 구현: A0 `ArtifactSceneRoot`(참조 없음)·`ArtifactViewSystem`(StageSelect Enter 100/Exit -100, 오류 상태면 연결 안 함, Factory 등록), A2 `ArtifactBoardInitializeJob`·`ArtifactPlan.CreateBoardInitialization`(Priority 2, 기존 `Register` 유지), 테스트 `ArtifactBoardInitializeJobTests`·`ArtifactViewSystemTests`. MSBuild 컴파일 통과(오류 0), Unity 미실행. **A1·A3~A5 보류:** 새 설정 테이블을 InGame 그룹 필수로 추가하면 테이블 목록으로 그룹을 만드는 `ChapterGraphPreparationTests`(천명님)·`InGameDataTableTests`가 `GetDataBase` 예외로 깨진다. 처리 방식 결정 대기 |
| 2026-10-05 | **Phase A 코드 완료.** DB 방식은 계획대로 A(새 설정 테이블)로 진행. A1: `ArtifactBoardConfigData`·`ArtifactBoardConfigDataSO`·`ArtifactBoardConfigDataTableSO`·`ArtifactBoardConfigTable`, 키 `InGameTableKey.ArtifactBoardConfig`·`DataBaseGroupKey.ArtifactBoardConfig`, `IHasArtifactBoardConfig`(InGameDataBase·DataBaseGroupSystem 구현, `IInGameQuery` 변경 없음), 에셋 `DataBaseSO/InGameGroup/ArtifactBoard/`(4×4) InGame 그룹 등록. 테이블 목록을 직접 조립하는 `ChapterGraphPreparationTests`·`InGameDataTableTests`에 새 테이블 한 줄씩 추가(Core 전달 문서 §5.7). A3: `StageLifeCycle`이 `ISystemInitData`로 설정을 `as`로 받아 보관(없어도 통과), `RegisterEntryJobs`에서 설정이 없으면 등록 없이 예외, 있으면 맵 준비 + 보드 초기화 Job(`JobRunner.Register`) 등록. A4·A5: `ArtifactBoardConfigTests`(테이블 규칙 5, 진입 등록 3, 실제 DB 4×4, LifeCycle 그룹 조립으로 실제 값 전달). MSBuild 컴파일 통과(오류 0). **Unity EditMode·Play 미실행.** 에디터 작업 필요: StageSelect 씬에 `ArtifactSceneRoot` 배치 |
| 2026-10-05 | **Phase B 코드 완료.** `LifeCycles/Artifact/`: `ArtifactEditRequest`·`ArtifactPreviewRequest`(대상 불변, 상태 Queued→Running→Completed/Cancelled/Faulted, 결과), `ArtifactPreviewSlot`(최신 요청 하나, 대기 Job 최대 하나), `ArtifactEditScheduler`(오류 상태·미초기화 거부, 편집 한 번에 하나, 편집 요청 시 프리뷰 무효화, `InvalidatePreview`·`CancelPending`, `Runner.Clear` 미사용), Job `ArtifactEditJob`(취소면 Core 미호출, 호출 직후 `LastEditResult`를 요청에 기록, 예외 시 Faulted 후 전파)·`ArtifactPreviewJob`(실행 시점 최신 요청, 배치되지 않은 인스턴스의 `PreviewMove` 미실행), `ArtifactPlan.CreateEditJob`·`CreatePreviewJob`(Priority 3). 테스트 `ArtifactEditSchedulerTests` 19개. MSBuild 컴파일 통과(오류 0). Unity 미실행. B4 중 표시 관련(반환 뒤 새 Pool ID로 갱신, 절대/로컬 셀 변환)은 Buffer를 그리는 Phase C 테스트로 옮긴다. 스케줄러의 소유·생성은 Phase C에서 `ArtifactViewSystem`이 맡는다 |
| 2026-10-05 | 사용자가 Unity Test Runner에서 Phase A·B 테스트(`ArtifactBoardConfigTests`·`ArtifactBoardInitializeJobTests`·`ArtifactViewSystemTests`·`ArtifactEditSchedulerTests`) 통과를 확인했다. Phase A·B 완료 |
| 2026-10-05 | **Phase C 코드 완료(씬 작업 대기).** `ViewSystem/ArtifactPopup/`: `ArtifactSceneRoot`(팝업 View·열기·닫기 버튼 참조와 검사, Panel 밖 위치 검사), `IArtifactPopupPresenter`·`ArtifactPopupView`(Panel 표시·숨김, 4×4 등 현재 크기 칸·배치 아이템(절대 셀 경계)·Pool 아이템 그리기, CustomPool 소유·빌린 목록 관리·반환 때 아이콘/부모/활성 초기화·그리는 중 예외 시 전부 반환), `ArtifactPopupDisplay`(표시 값), `ArtifactPopupText`(대기·실패 문구), `ArtifactViewSystem`(View DB 아이콘 조회, 스케줄러 생성·소유, 버튼 연결·해제, 열기: 오류·이미 열림 무시·Artifact Layer·프리뷰 무효화, 닫기: 프리뷰만 취소·Artifact일 때만 StageSelection 복귀, Detach: 대기 요청 취소·Artifact일 때만 ClearLayer, Tick: 스냅샷 참조 변경 시 다시 그림 + 끝난 편집 결과 문구 따로 갱신, 미초기화 시 대기 문구). 테스트 `ArtifactViewSystemTests` 18개(교체), `ArtifactPopupViewTests` 5개. MSBuild 컴파일 통과(오류 0), Unity 미실행. **C1 씬 배치는 사용자 에디터 작업**(배치 전에는 StageSelect 진입 때 `ArtifactSceneRoot` 검사 오류) |
| 2026-10-06 | **Phase C 3D 표현·씬 연결 완료.** View 설정 DB, 프리팹별 모델 풀, 하단 모양 목록, 오른쪽 상세 문구, World Space 배경 및 투명 UI 차단 연결. Unity 관련 EditMode 64/64 통과, 실제 Boot→StageSelect 팝업 재생 smoke 1/1 통과(열기·닫기·상세·맵 복원·재열기). 배치 테스트는 화면 픽셀 외형과 실제 마우스 클릭을 검증하지 않음. Phase D 및 E2·E3는 미완료. |
| 2026-10-06 | Floor 표시 오류 수정: 전체 배치판 프리팹을 논리 칸마다 16개 만들던 코드를 중앙 1개 생성으로 변경. 논리 셀·아이템 좌표 유지. Presenter 및 Boot smoke 테스트의 단일 Floor 검증 수정. MSBuild 컴파일 통과. 이번 수정 후 Unity 테스트·Play 재실행은 미확인(앞 행의 통과 결과는 수정 전 결과). |
| 2026-10-06 | **Phase D 입력·표시 구현 및 회귀 검증 완료, D4 협의 대기.** 일반 C# Mapper·Interaction, 기존 Slot/Layer 및 Scheduler 연결, 배치·회전 이동·목록 반환·우클릭/버튼 위 취소·닫기 정리, 풀을 사용하는 모델/셀 프리뷰. 배경은 Inspector 카메라 참조와 View DB 설정으로 카메라 렌더 직전에 시야를 맞추며 Pivot을 중앙으로 보정. 실제 Floor 중앙 및 4×4 칸의 배치 면 높이를 검증. Unity 관련 회귀 355/355(실제 Boot·DB·Core와 Framework 입력 통합 2개 포함) 통과. 고정 후보 View Tick 1,000회 관리 할당 0B. 물리 장치의 마우스·키보드 조작 및 Player/GTX 1060 FPS는 미검증. Core·전투·공용 오류 경계 코드는 이번 Phase D에서 수정하지 않음. 자동 오류 정리는 D4 미완료로 남기며 Phase E를 완료 처리하지 않음. |
| 2026-10-06 | 회수 실패 조사: 실제 StageSelect 씬의 하단 Pool 영역과 팝업 뒤 열기 버튼 영역이 겹치며, `IsButtonOrDetail`이 가려진 열기 버튼까지 검사해 해당 영역에서 Return 요청을 취소함을 확인. 열린 팝업의 조작 차단 대상에서 열기 버튼을 제외하고 닫기·상세 영역 검사는 유지. Unity Roslyn으로 Systems 컴파일 통과, diff 공백 검사 통과. 실제 마우스 재검증·Unity 테스트 재실행은 미완료(Pipeline 패키지 없는 실행 중 Editor). 다른 영역의 회수 실패까지 해결됐다고 단정하지 않음. |
| 2026-10-06 | 문서 정정(코드 변경 없음): 우클릭이 회수로 바뀐 사용자 결정(상호작용 설계 §4)을 D2 Task와 [입력 안내](../viewDocs/artifact-input-guide.md)에 반영. 위 Phase D 행의 "우클릭 취소"는 변경 전 기록이다. 입력 안내의 회전 키 표를 실제 바인딩(Q/E)으로 정정. 이 표의 행 사이 빈 줄을 지워 표가 끊기지 않게 했다. |
| 2026-10-06 | **D1 변경(사용자 결정): 모든 UI는 현강 작업자의 UI 구조·문서를 따른다.** 확인한 어긋남 2건: ① D1의 "씬 로드 뒤 BindScene 호출 없음"은 현강 `efb04eea`(GameBootstrap SceneEnterLoaded -200)로 사실이 아니게 됐다. ② 열기·닫기 버튼이 `Button.onClick.AddListener`로 연결돼 있다(`ArtifactViewSystem` 197·198행). `Assets/AGENTS.md`는 `UIInputBinding`/`IUIInputReceiver`를 쓰라고 정한다. 현재 팝업 클래스는 `UIBase`가 아니라서 BindScene의 영향은 없다(동작 이상 아님). 문서(D1·범위·§8 남은 협의·§10)만 갱신했고 코드는 바꾸지 않았다. 이전 설계·Task는 결정 대기 |
| 2026-10-06 | UI 이전 사용자 결정 U1~U3: UI 전체를 `UIBase`로, `UIId`는 직접 추가 후 공유(값 13, 12는 현강 미병합 `Timeline`), 열고 닫기는 UISystem. [UI 이전 설계](../specs/2026-10-06-artifact-popup-ui-migration-design.md) 초안 작성. 구현 전, 설계 검토 대기 |
| 2026-10-06 | 사용자 결정 U4·U5 반영: ESC로 닫기를 이번 범위에 넣는다(`Artifact` 맵 `Close` 액션 → `BackCloseUI`, `Common/OpenSettings` ESC 겹침은 입력 담당에 공유). 열기 버튼은 현강 `DeckButtonUI.md` 구조(바깥 버튼 필드 + 자식 Panel)를 따라 `ArtifactPopupUI`가 소유한다. 설계 §0·§4.3·§6~9 갱신. 구현 전 |
| 2026-10-06 | **Phase U1 코드·EditMode 테스트 완료(씬 연결 전).** `UIIDs.cs` `Artifact = 13`; 새 `ArtifactPopupUI`(`UIBase`·`IUIInputReceiver`, 열기·닫기·목록 슬롯 `UIInputBinding`, 루트 유지·Panel 토글, Buffer·View DB 직접 조회), `IArtifactPopupScreen`·`IArtifactPopupHost`; `ArtifactPopupView` 3D 전용, `ArtifactPoolItemView` 모양 그리기만, `ArtifactSceneRoot`(`popupView`·`popupUI`·`outputCamera`); `ArtifactViewSystem` host 구현·onClick/이벤트 구독 제거·씬 이탈 Disconnect→`CloseUI`→정리·ESC→`BackCloseUI`; 입력 `Artifact/Close`(ESC). 테스트: 새 `ArtifactPopupUITests` 10개, `ArtifactViewSystemTests` 7개 추가·씬 연결 테스트 교체, `ArtifactPopupViewTests` 3D 기준으로 갱신, Boot smoke는 컴파일만 맞춤. **검증(Unity 6000.0.73f1 배치):** 관련 76개 중 70 통과, 실패 6개는 현강 기존 UI 테스트(CombatLog 2·Deck 3·GlobalUI 1). EditMode 전체 2790개 중 2761 통과·28 실패·1 결론 없음. 실패 28개는 HEAD 기준 worktree(사용자 미커밋 변경 제외)에서도 모두 실패해 이번 변경으로 새로 생긴 실패는 없다(기준 쪽은 7개 더 실패). Boot smoke 2개는 씬 연결 전이라 이번 실행에서 실패했고, HEAD 기준에서도 Boot→StageSelect 미도달로 실패한다(U2에서 재검증). 참고: 배치 EditMode에서는 `Awake`가 호출되지 않아 새 테스트는 `Awake`를 직접 호출한다(현강 기존 UI 테스트 실패와 같은 원인으로 보임, 미확인). `ArtifactPopupUI.cs.meta`는 `.cs`보다 먼저 만들어 Unity가 GUID를 새로 만들었다(`1708932e…`). 첫 배치 실행 시도 직후 열려 있던 에디터가 종료됐다. 씬·실제 Play는 미검증 |
| 2026-10-06 | **Phase U2 씬 연결·Boot smoke 완료(사용자 Play 대기).** `StageSelect.unity`: `ArtifactPopupCanvas`에 `ArtifactPopupUI`(fileID 9700000007) 추가, 열기·닫기 버튼·Panel·PoolArea·문구 3개 참조를 `ArtifactPopupView`에서 옮김, `ArtifactSceneRoot`는 `popupView`·`popupUI`·`outputCamera`만. `ArtifactItem.prefab`에 `Button`(Transition None, Navigation None, OnClick 없음) 추가. Boot smoke 첫 테스트에 `UIInputBinding` 연결·OnClick 0개·ESC(`Artifact/Close`) 닫기·맵 복원·Layer 복귀 확인 추가. **검증(Unity 6000.0.73f1 배치, 에디터 종료 상태):** `ArtifactPopupBootSmokeTests` 2개·`ArtifactPopupUITests`·`ArtifactViewSystemTests`·`ArtifactPopupViewTests`·`DataBaseAssetTests` 합계 56개 모두 통과. 실제 마우스·키보드 Play(U2.4)는 미검증 |
| 2026-10-06 | U3.1 화면 문서 `Screens/Stage/ArtifactPopupUI.md`(현강 형식) 작성. U3.3 중 현강님 몫: [현강님 공유 문서](../handoffs/2026-10-06-artifact-popup-ui-hyeongang-share.md) 작성(전달 미실시, 회신 표 §7). 용근님 ESC 겹침 공유와 U3.2는 남음 |
| 2026-10-06 | **배치 면 정렬(사용자 결정 안 B).** 임시 에디터 스크립트로 실제 메시를 측정했다(배치 코드와 같은 배치 규칙, 측정 후 스크립트 삭제). 측정: Floor 안쪽 배치 면 한 변 7.161·중심 (−0.180, 0.207)·높이 y=0(클릭 평면과 일치), 회복 성배·가시심장 받침은 칸 2.2 기준으로 정확, 영혼 드랍은 가운데 줄 칸을 원점으로 모델링돼 코드의 무게중심 배치와 0.25칸(0.55) 어긋나고 세로 받침 간격이 0.3 짧음. 변경: View DB `ArtifactPopupConfig_Default` `cellSpacing` 2.2→1.79, `modelScale` 1→0.8136(받침:칸 비율 유지), `ArtifactFloor.prefab` `Model` 자식 위치 → (−0.0139, −2.4294567, −0.0073)(Floor 크기·카메라 구도 유지), `SoulDrop.prefab` `Model` 자식 z 0→−0.55. 재측정: Floor 안쪽 면 중심 (0.000, 0.000)·한 변 7.161 = 4×1.79, 회복 성배·가시심장(4방향) 받침 중심 일치, 영혼 드랍 가로 3칸 일치(z −0.45, 기대 −0.448). **남은 것:** 영혼 드랍 세로 받침이 0.24(현재 배율) 짧음 → Blender 수정 필요. `modelHeight` 0.15(받침이 배치 면보다 0.15 위)는 사용자 선택 대기로 유지. 검증: Boot smoke 2·DataBaseAsset·팝업 관련 합계 54개 통과(Unity 배치). 실제 Play 화면은 미확인 |

## 10. 2026-10-06 3D 표현 변경 (사용자 승인)

- 기준 화면: 사용자 제공 2026-09-08 스크린샷. 왼쪽 사선 3D 보드, 하단 보유 목록, 오른쪽 이름·효과 설명, 오른쪽 위 뒤로가기.
- 시스템·Scheduler·Plan·Job·Buffer 계약은 유지한다. 새 Presenter는 데이터 값만 받아 그리며 Core/InGame DB를 조회하지 않는다.
- 모델은 기존 View DB `ArtifactViewData.Prefab`, 정적 문구는 `Description`에 둔다. 수치를 중복 저장하지 않는다. 논리 크기와 목록 모양은 Buffer의 현재 크기·로컬 셀이다.
- 새 설정형 View 테이블 `ArtifactPopupConfig`에는 칸 간격·칸 크기 배율·모델 배율·모델 높이를 둔다. 실제 ViewGroup에 필수 등록하고 `IHasArtifactPopupConfig`로 조회한다. 기존 `IViewQuery`의 테스트 구현 계약은 늘리지 않는다.
- 셀·모델은 프리팹별 CustomPool, 보유 목록과 목록 안의 모양 셀도 CustomPool로 재사용한다. Hide/Detach 시 대여 반환과 콜백 해제를 수행한다.
- Presenter/SceneRoot는 항상 활성. 숨길 대상은 Overlay Panel, WorldRoot, World Space 배경이다. 열 때 활성 맵 표시를 숨기고 닫을 때 자신이 숨긴 경우에만 복원한다.
- 슬롯은 포인터 진입·클릭 의도만 전달하고 시스템이 상세 문구를 조회한다. 입력 Layer는 시스템 소유다. UISystem 이력을 사용하는 화면으로 등록하지 않는다(FW-008/009·DR-010). → 2026-10-06 D1 변경으로 UI 부분은 현강 UI 구조(UIBase·UIInputBinding)로 옮길 예정이다. 입력 Layer가 시스템 소유라는 점은 유지한다(UISystem은 Layer를 바꾸지 않는다).
- 씬에서 카메라 시야를 바꾸지 않고 현재 Main Camera와 보드·배경 위치를 연결했다. Cinemachine 전환 및 동적 보드 확장은 별도 검증 항목이다.
- 재사용 에셋: `2baf80fd`의 모델·머티리얼·텍스처만 현재 Assets에 추가했다. 예전 Core·View 코드·씬 전체는 가져오지 않는다.
- Phase D에서는 카메라를 Attach 때 한 번 보관해 Mapper와 공유한다. 모델 Collider 의존 없이 Buffer의 배치 셀로 인스턴스를 찾는다. 투명 전체 화면 차단 Image를 이유로 모든 포인터 입력을 무시하면 안 된다.
- 기존 규칙 에셋의 Core 효과 계약 불일치(`ArtifactDataTableTests.CurrentSoulDropAuthoring_IsRejectedByCanonicalCompiler`)는 별도 인계 사항이다. 이번 변경은 표현 전용이며 규칙 데이터를 변경하지 않았다. 정적 설명은 현재 Core 규칙의 안내 문구이고 실제 규칙 에셋의 완성도를 보증하지 않는다.

## 11. UI 이전 Phase·Task (2026-10-06, 설계: [UI 이전 설계](../specs/2026-10-06-artifact-popup-ui-migration-design.md))

진행·검증 기록은 §9에 적는다. 각 Phase가 끝나면 멈추고 보고한다.

**Phase U1 — 코드·EditMode 테스트** (끝나도 씬 연결 전이라 Boot 재생은 Phase U2까지 동작하지 않는다)
- [x] U1.1 `UIIDs.cs`에 `Artifact = 13` 한 줄(12는 현강 미병합 `Timeline`)
- [x] U1.2 `IArtifactPopupScreen`(View → UI)·`IArtifactPopupHost`(UI → View) 좁은 계약
- [x] U1.3 `ArtifactPopupUI`(`UIBase`·`IUIInputReceiver`): 열기·닫기 버튼과 목록 슬롯은 `UIInputBinding`, 루트는 켜 둔 채 Panel만 토글, `BindUIService`에서 `ArtifactReadSink`·`UIServiceCommand`, `GetUIData`에서 View DB, 열린 동안 스냅샷이 바뀌면 목록 다시 그림, 화면 좌표 판정
- [x] U1.4 `ArtifactPopupView`를 3D 전용으로 축소(UI 필드·`DetailRequested`·상태/상세 문구 제거), `IArtifactPopupPresenter`에서 `ShowStatus` 제거, `ArtifactPoolItemView`는 모양 그리기만
- [x] U1.5 `ArtifactSceneRoot`: `popupView`·`popupUI`·`outputCamera`(버튼 참조 제거)
- [x] U1.6 `ArtifactViewSystem`: host 구현(`CanOpen`·`PopupOpened`·`PopupHidden`), 문구·판정은 screen으로, onClick·이벤트 구독 제거, 씬 이탈 때 Disconnect → `CloseUI` → 자체 정리, ESC → `BackCloseUI`
- [x] U1.7 입력: `Artifact` 맵 `Close`(ESC), `ArtifactInputNames.Close`, Controller `Press`, `IArtifactInputReceiver.Close()`, Slot 전달
- [x] U1.8 테스트: `ArtifactPopupUITests`(새), `ArtifactViewSystemTests`·`ArtifactPopupViewTests` 갱신, `ArtifactPopupBootSmokeTests`는 컴파일만 맞춤(실행은 U2)
- [x] U1.9 검증: Unity 배치 컴파일, 관련 EditMode 테스트 실행

**Phase U2 — 씬 연결·재생 확인**
- [x] U2.1 `StageSelect.unity`: `ArtifactPopupCanvas`에 `ArtifactPopupUI` 추가, UI 참조를 옮겨 연결, `ArtifactSceneRoot`에 `popupUI` 연결
- [x] U2.2 목록 슬롯 프리팹에 `Button`(Transition None)
- [x] U2.3 Boot smoke 2개를 새 경로(UIInputBinding 클릭·UISystem·ESC)로 실행
- [ ] U2.4 사용자 Play: 열기·닫기·ESC·Hover 상세·배치·회수 회귀

**Phase U3 — 문서**
- [x] U3.1 현강 형식 `Screens/Stage/ArtifactPopupUI.md`(Hierarchy·Inspector 표·입력)
- [ ] U3.2 입력 안내 상단·액션 표(ESC), 팝업 계획 D1·§9
- [ ] U3.3 공유: 현강님(UIId 13), 용근님(`Artifact/Close`와 `Common/OpenSettings`의 ESC 겹침)
