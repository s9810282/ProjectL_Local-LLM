# 전투 골격·View 구조 평가 (2026-10-09)

- **기준 코드:** `feature/Yonggeun/Stage` HEAD `88c845d0`. 구조는 [상세 UML](2026-10-09-combat-skeleton-view-detailed-uml.md)에 그렸다.
- **평가 기준:** [설계 세션 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)(SSOT·SOLID·FW 계약·DB 규칙·경계).
- **방법:** 코드를 정적으로 읽었다. Unity에서 실행하거나 프로파일링하지 않았다. 성능 항목은 **호출 경로와 할당 지점**까지만 확인했고 실제 비용·병목 순위는 정하지 않았다.
- **이전 평가와의 관계:** [10-06 구조 평가](2026-10-06-combat-view-structure-review.md)의 권고 2~5(이동 허용 조건 통합, 입력 Router, 배치/공격 분리, PointerMapper)는 VR1~VR5로 반영됐다. 이 문서는 그 뒤 추가된 골격 K21·K22, 포기 UI(4-V), 손패 카드, 벽 차단 공격까지 포함한 **현재 상태**를 다시 평가한다.
- **이 문서는 판정과 권고까지다.** 코드는 바꾸지 않았다. 권고를 구현하려면 별도 설계·계획을 거친다.

---

## 1. 요약 판정

| 영역 | 판정 | 한 줄 근거 |
|---|---|---|
| 진행 흐름의 명확성 | **양호** | 진행의 주인은 `CombatFlow` 하나다. 번호 계약(N·H·A·S·P)으로만 동기화하고 Tick당 등록은 하나다 |
| 책임 경계(골격) | **양호** | LifeCycle(수명)·Flow(등록)·ExitSequence(이탈 순서)가 나뉘었다. ExitSequence의 Run 종료 판정은 커질 여지가 있다 |
| 책임 경계(View 입력) | **양호** | VR2·VR3 뒤 Router·Gate·배치·공격·경로 소유자가 나뉘었다. 제출 경로는 두 갈래로 남았다 |
| 책임 경계(View 조정자) | **주의** | `CombatViewSystem`이 다시 1009줄·필드 40개로 커졌다. 포기 UI·개발 패널·타임라인 UI 제어가 순서 조정과 섞였다 |
| 외부 경계 | **보통** | Buffer 경계는 잘 지켜진다. 남은 예외는 타임라인 직접 호출(+출처 이중화), Core 정리 세부에 기대는 검사, 넓은 Core 서비스 |
| 유지보수성 | **양호** | 한국어 XML 주석·테스트가 넓다. 낡은 주석 2곳, 스타일 불일치 3파일, 테스트 전용 운영 코드 2곳이 있다 |
| 확장성 | **보통** | 비이동 연출·챕터 계속·스테이지 배경은 쉽다. 새 명령 종류는 3개 작업자 영역 7~9파일을 함께 고쳐야 한다 |
| 성능 | **미측정** | 구조상 큰 위험은 없다. 결과·사건마다 전체 다시 그리기, 결과마다 타임라인 계산, 포인터마다 UI Raycast가 규모에 따라 커질 수 있다 |
| 오류 처리 | **양호** | 모든 진입점이 같은 `RuntimeFaultBoundary`로 모인다. 정리 실패를 모아 한 번 보고한다 |

---

## 2. 책임 경계 (한 객체에 책임이 몰렸는가)

### 2.1 클래스별 현황

필드 수는 `private` 인스턴스 필드를 센 값이다(상수·정적 제외).

| 클래스 | 줄 | 필드 | 맡은 일 | 판정 |
|---|---|---|---|---|
| `CombatLifeCycle` | 258 | 11 | 진입 요청 수명, 씬 Enter/Exit, Flow·Exit Tick 호출, Completion 감지, 전환 guard | 양호. 수명과 진입 확인만 한다 |
| `CombatFlow` | 282 | 10 | 등록 조건·확인 조건(Preparation·Command·Preview·Advance) | **양호.** 상태가 "기다리는 처리 하나 + 정지"뿐이다 |
| `CombatExitSequence` | 347 | 23 | 대기 → (포기 요청) → 최종 결과 확인 → Completion 표시 → EndBattle → 정리 검증 → Run 종료 판정 → 새 Run → 허가된 전환 | 보통. 순서 하나가 책임이지만 `EndsRun`(챕터 완료 요청·Run 결과 판정)이 정책이라 S3·Title이 붙으면 커진다 |
| **`CombatViewSystem`** | **1009** | **40** | Framework 수명, 씬 Attach/Detach·정리, 결과 소비 순서, Layer 동기화, facts 조립, 골격 계약(제출 정지·Completion), **포기 UI**, **개발 Layer 패널**, **타임라인 UI 열고 닫기** | **주의.** 아래 R1 |
| `CombatInputRouter` | 167 | 4 | 입력 의도별 순서와 포인터 대상 판정 | 양호 |
| `CombatCommandGate` | 80 | 2 | 마지막 제출 검사, 시작/턴 종료 대기 | 양호 |
| `PlacementDragInteraction` | 281 | 15 | 배치 Drag·고스트·배치 미리보기 | 양호 |
| `AttackTargetingInteraction` | 430 | 13 | 공격 모드·범위 응답·조준 선택·호버 미리보기·기록 모드 행위자 | 보통. 기록 모드 분기 5곳이 있다 |
| `CombatPathInput` | 380 | 22 | 경로 지정 수명, 표시 캐시, 복귀 미리보기·제출 | 보통. 이동 입력은 가장 복잡한 상태 기계다 |
| `CombatMovementInteraction` | 367 | 20 | 경로 초안 편집·견적·확정 제출 | 보통. 위와 합쳐 747줄·필드 42개 |
| `CombatSceneDisplay` | 483 | 18 | Presenter 수명, 표시 의도를 Presenter로 위임 | 양호. 실질 로직은 위임이다 |
| `CombatResultPlayback` | 173 | 7 | 결과 하나의 재생 수명·토큰 | **양호** |
| `OrderedCombatPresentation` | 102 | 11 | 사건 순서 진행 | 양호 |
| Presenter들(`FieldView` 395, `FieldUnitPresenter` 285, `HandPresenter` 249) | — | 6~7 | 각자 하나의 화면 요소 | 양호 |

### 2.2 발견 사항

**R1. `CombatViewSystem`이 다시 커졌다** (`V/CombatViewSystem.cs`)

- 10-06 평가 때 895줄에서 VR2~VR4로 줄었다가, 포기 UI(4-V, `:884-970` 약 90줄, 필드 6개)와 개발 패널(`:972-1007`)이 더해져 1009줄이 됐다.
- 포기 UI는 **독립된 작은 기능**이다. 필요한 것은 facts, `ICombatAbandonRequest`, 표시 계약(`BindAbandon`·`SetAbandonAvailable`·`Show/HideAbandonConfirm`), 확인창 열림 상태 하나다. 결과 소비 순서와는 `SuspendSubmissions`·`StopInput`·`Detach`에서 "확인창 닫기"로만 만난다.
- `SyncTimelineUI`(`:604-617`)는 UI 화면을 언제 열고 닫을지 정한다. 같은 조건을 `TimelineUI.RefreshTimeline`도 다시 판단한다(§3 E5).
- **권고:** 다음에 이 파일을 크게 고칠 때 포기 UI를 `CombatAbandonController`(가칭)로 뽑는다. 상태는 확인창 열림 하나, 입구는 `Sync(facts, exitIdle)`·`Open`·`Confirm`·`Cancel`·`Close`. ViewSystem은 생성·Tick 호출·정리 호출만 한다. 이것만으로 약 100줄·필드 6개가 빠진다. 개발 패널도 같은 방식으로 뽑을 수 있다. **지금 단독 작업으로 할 만큼 급하지는 않다.**

**R2. `CombatExitSequence`의 Run 종료 판정이 커질 자리다** (`LC/CombatExitSequence.cs:284-298`)

- `EndsRun`은 보스 승리면 `CompleteChapter`를 부르고, 그 결과(다음 챕터·Run 완료·정의 없음)를 해석한다. 정의 없음은 임시로 Run 종료다(K21).
- S3(챕터 계속)과 Title 화면이 오면 "어디로 갈지"까지 이 메서드가 정하게 된다. 지금은 목적지가 `RunEndDestination`(LifeCycle)과 `_destination`(요청 인자)로 나뉘어 있다.
- **권고:** S3·Title 작업 때 "완료 결과 → (Run 종료 여부, 목적지)"를 돌려주는 작은 판정 객체로 뽑는다. ExitSequence는 순서만 남긴다.

**R3. 이동 입력이 가장 복잡하다** (`V/CombatPathInput.cs`, `V/CombatMovementInteraction.cs`)

- 두 클래스 합계 747줄·필드 42개. 시작 칸 대기 → 초안 → 견적 → 동결 → 제출, 복귀 요청·응답·제출, 표시 캐시, 수명 세대가 섞여 있다.
- 책임은 "경로 수명(PathInput)"과 "초안 편집·견적(MovementInteraction)"으로 이미 나뉘어 있다. 더 쪼개기보다 **상태 전이 표와 테스트를 유지**하는 쪽이 낫다(상세 UML §6.5).

**잘 된 점**

- `CombatResultPlayback`은 재생 수명만 갖고 Buffer·Presenter·오류 경계를 모른다. 완료 번호를 따로 두지 않는다(P가 원본).
- `CombatInputPolicy`는 무상태 정적 판단이다. 종료·Layer·제출·미리보기·경로·포기·개발 Layer 허용이 모두 여기 한 곳이다.
- Router가 포인터 대상 판정의 유일한 식(`ResolvePointerTarget`)을 갖는다. 10-06의 암묵적 라우팅 문제가 해소됐다.
- 표시는 "부르는 쪽은 원하는 상태만 말하고 View 찾기는 Presenter가 한다"는 규칙(`SetHiddenEntity` 등)을 지킨다.

---

## 3. 외부 경계

### 3.1 지켜지는 것

- View가 쓰는 Buffer는 `CombatViewBuffer` 하나다. 입력 Buffer 전체 비우기는 LifeCycle만 한다.
- 규칙 판정(배치 가능·이동 견적·공격 범위·AP·차단 벽)은 모두 Core 미리보기 응답으로 받는다. View는 계산하지 않는다(P6). 벽 차단도 Core가 준 `AttackBlockedContact`만 표시한다.
- 배경 프랍 키는 Core가 게시한 `CurrentProgressBuffer.CurrentStage.StageKey`로 고른다. InGame DB를 읽지 않는다.
- 골격 → View는 골격이 정의한 `ICombatExitPresentation`에만 의존한다(DIP). View → 골격은 `ICombatEntryRead`·`ICombatAbandonRequest` 두 좁은 계약만 보관한다.
- Unity 콜백(입력·버튼·포커스·비활성화)의 예외는 모두 오류 경계로 모인다. 씬 탐색은 로드 때 한 번뿐이다.

### 3.2 발견 사항

**E1. 턴 순서의 출처가 둘이다(SSOT 위반 위험)** — 담당: Core + View

| 소비자 | 읽는 값 | 방식 | 코드 |
|---|---|---|---|
| 유닛 HUD의 턴 순서 숫자 | `ICombatProcessingService.GetTimeline(max(10, 캐릭터 수 × 4))` | **View가 Core 서비스를 직접 호출**, 결과마다 Core가 예측 계산 | `V/CombatViewSystem.cs:162·630`, `VD/FieldUnitPresenter.cs:149-162` |
| `TimelineUI`(화면 위 타임라인) | `CombatStateData.CommandState.Timeline` | Buffer 읽기, 매 프레임 | `GlobalSystem/UISystem/Screens/Stage/TimelineUI.cs:116` |

- 두 값이 같은 예측인지, 길이·갱신 시점이 같은지 코드로 보장되지 않는다. 다르면 HUD 숫자와 타임라인 그림이 어긋난다.
- VR6(타임라인 Buffer 소비)의 시작 조건은 "Core가 타임라인을 Buffer에 공개"였다. `CommandState.Timeline`이 이미 Buffer에 있으므로 **조건이 충족됐을 수 있다.** 이 값이 VR0에서 요청한 예측 계약(범위·무효화·빈 값)과 같은지 Core(천명님)에 확인이 필요하다.
- **권고:** 확인되면 HUD도 `CommandState.Timeline`을 읽게 바꾸고 `CombatViewSystem._combatQueries`와 `CoreSystem.CombatSystem` 의존을 지운다. 그러면 B7 예외 경계가 사라지고 결과마다 하던 예측 계산도 없어진다.

**E2. 이탈 정리 검증이 Core 정리 세부에 기대고 있다** — 담당: 골격 + Core

- `EndBattle` 뒤 ExitSequence는 `!HasBattle ∧ 캐릭터 0명 ∧ Runner 유휴 ∧ Completion·최종 결과 참조·S·P 보존`을 확인하고, 하나라도 다르면 예외로 런타임을 멈춘다(`LC/CombatExitSequence.cs:210-218`).
- "EndBattle은 전투 캐릭터를 모두 비운다", "최종 결과 객체를 같은 참조로 남긴다"는 Core의 구현 세부다. Core가 결과 화면을 위해 캐릭터를 남기도록 바꾸면 이탈이 예외로 멈춘다.
- **권고:** 이 조건을 Core 전달 문서에 **명시적 계약**으로 적어 둔다. 또는 골격이 꼭 필요한 조건(전투 없음·결과 보존)만 남기도록 줄인다.

**E3. `ICombatProcessingService`가 넓다(ISP)** — 담당: Core

- 멤버 14개다. Job은 4개, ExitSequence는 2개, View는 1개(`GetTimeline`)만 쓴다. View가 이 인터페이스를 갖고 있으면 `EndBattle`·`ApplyHealth`도 부를 수 있다.
- E1이 해결되면 View 쪽은 사라진다. 골격 쪽은 Core 소유라 요청 사항으로만 남긴다.

**E4. 명령 제출 경로가 두 갈래다** — 담당: View

- 배치·공격·시작·드로우·턴 종료는 `CombatCommandGate.TrySubmit`, 이동·복귀는 `CombatMovementInteraction`·`CombatPathInput`이 Buffer에 직접 `Submit`한다(`V/CombatMovementInteraction.cs:360`, `V/CombatPathInput.cs:365`).
- 이동·복귀도 `CanEditPath` → `CanSubmit(Move)`로 같은 Policy를 지나므로 **허용 조건 자체는 하나**다. 다만 이동은 Core가 준 `MovementPreviewContext`를 함께 제출해야 해서 Gate를 거치지 않는다(Gate 주석에 명시).
- 위험은 새 명령을 추가하는 사람이 어느 경로를 쓸지 헷갈리는 것이다. **권고:** 인계 문서에 규칙을 적는다("Core 미리보기 context가 필요한 명령만 직접 제출, 나머지는 Gate"). 지금 코드를 바꿀 필요는 없다.

**E5. UI 화면이 Runtime Tick 밖에서 Buffer를 직접 읽는다** — 담당: UI 작업자

- `TimelineUI.Update`·`CombatLogUI.Update`가 `MonoBehaviour.Update`에서 Buffer를 읽는다. `TimelineUI`는 매 프레임 다시 그리고, `CombatLogUI`는 기록 수가 그대로면 바로 반환한다(2026-10-09 정정). 둘 다 Runtime Tick·오류 경계 밖이다. 여기서 난 예외는 Unity 로그만 남고 런타임은 계속 돈다.
- Timeline UI를 여는 시점은 `CombatViewSystem.SyncTimelineUI`가, 내용을 지우는 조건(Completion·Finished·배치 단계)은 `TimelineUI.RefreshTimeline`이 각각 판단한다. 같은 표시 조건이 두 곳에 있다.
- **권고:** VR6 때 함께 정리한다. 열고 닫기와 내용 표시 조건 중 하나를 원본으로 정한다.

**E6. 테스트에서만 쓰는 호환 경로가 운영 코드에 남아 있다** — 담당: 골격 + Core

- `CombatFlow.BeginPreparation`(`LC/CombatFlow.cs:113`) → `JobType.Start` → `CombatPrepareJob`의 대상 없는 생성자 → Core `PrepareBattle()`. 운영 호출자는 없고 `CombatFlowTests`·`CombatFlowIntegrationTests`만 쓴다.
- **권고:** 테스트를 `RequestEntry` 경로로 옮긴 뒤 Flow·Plan·Job의 호환 경로를 지운다. Core `PrepareBattle()` 제거는 Core에 요청한다.

**E7. Unity UI 버튼 콜백의 시점이 정해져 있지 않다** — 참고

- 포기·복귀·Completion·개발 패널 버튼은 `EventSystem.Update`에서 불린다. `GameBootstrap.Update`(Runtime Tick)보다 먼저일지 나중일지는 Script Execution Order로 고정되지 않았다.
- 모든 버튼 입구가 콜백 안에서 조건을 다시 검사하고(`OnAbandonConfirm`의 재검사, Completion의 세대 확인), ExitSequence는 확인 다음 Tick에 `EndBattle`을 하므로 **지금은 문제없다.** 새 버튼을 추가할 때도 같은 재검사 규칙을 지켜야 한다.

---

## 4. 유지보수성

### 4.1 좋은 점

- 거의 모든 멤버에 한국어 XML 주석과 설계 절 참조가 있다.
- 테스트가 넓다. 전투 골격·View 관련 EditMode 테스트 파일만 30개가 넘는다(`Assets/Tests/EditMode/Framework/`: `CombatViewSystemTests` 1419줄, `CombatEntryIntegrationTests` 929줄, `CombatInteractionTests` 823줄, `CombatFlowTests` 798줄 등). 순수 C# 객체는 가짜 표시(`FakeDisplay`)로 검증된다.
- 정리 경로가 일관된다. 하나가 실패해도 나머지를 시도하고 모아서 던진다(`errors ??=`, `RunAll`).
- 늦은 콜백 방어가 한 가지 방식(세대·토큰)으로 통일돼 있다.

### 4.2 발견 사항

| # | 내용 | 위치 | 권고 |
|---|---|---|---|
| M1 | 낡은 주석: "포기는 Core 포기 API 인수 뒤 별도 경로로 연결한다" — 이미 K22로 연결됐다 | `LC/CombatLifeCycle.cs:233` | 주석 갱신 |
| M2 | 요약 주석 위치 오류: "결과 하나를 재생합니다"가 `Attach`에 붙어 있다 | `V/ICombatPresentation.cs:15` | `Play`로 옮김 |
| M3 | 스타일 불일치: 한 줄 여러 문장, 중괄호 없는 블록, 멤버 사이 빈 줄 없음 | `V/OrderedCombatPresentation.cs`, `V/CombatMovementPlayback.cs`, `V/CombatSceneEventPresenter.cs` 일부 | 병합 충돌 위험이 낮을 때 Allman 정리 |
| M4 | 생성자 인자 null 검사 누락(7개 중 1개만 검사). 잘못 조립하면 `ArgumentNullException` 대신 나중에 `NullReferenceException` | `V/CombatPathInput.cs:42-56` | 코딩 규칙대로 검사 추가 |
| M5 | 테스트에서만 쓰는 운영 API: LINQ로 배열을 만드는 `Render` | `VD/FieldUnitPresenter.cs:64-65` | 테스트를 `RenderFacts`로 옮기고 삭제 |
| M6 | 임시 상수 3개가 흩어져 있다: Run 종료 목적지 `Stage`, 이탈 허용 목적지 목록, 새 Run 로드아웃 `1` | `LC/CombatLifeCycle.cs:112`, `LC/CombatExitSequence.cs:300-304`, `LC/Plans/CombatPlan.cs:19` | Title·로드아웃 선택 작업 때 함께 교체(인계 문서 R6) |
| M7 | `RecordInputOnly` 분기가 운영 파일 7개, 약 19곳에 흩어져 있다(Flow 2, LifeCycle 2, Policy 4, Router 1, Attack 5, ViewSystem 4, Bootstrap 1) | 상세 UML §4.5 | 지금은 개발 전용이라 허용. 세 번째 모드가 생기면 모드별 정책 객체로 모은다 |
| M8 | 다른 작업자 파일에 한국어가 아닌 주석 한 줄 | `ViewSystem/StageMap/StageMapEntryFlow.cs:54` | 담당자에게 알림(이 범위 밖) |

---

## 5. 확장성

### 5.1 변경 영향표

| 확장 | 바꿔야 할 곳 | 난이도 | 비고 |
|---|---|---|---|
| **비이동 연출**(공격 이펙트·피격·사망) | `CombatSceneEventPresenter.BeginEvent`에서 연출이 끝날 때 `completed` 호출. 취소는 `_presentationVersion` 증가로 무효화 | **쉬움** | 순서·확인·입력 코드는 그대로. 연출 종류가 3개를 넘으면 사건 종류별 Presenter로 나눈다 |
| 챕터 계속(S3) | Core 연결 정의 원본만 | **쉬움** | 골격은 이미 `CompleteChapter` 결과로 분기한다 |
| 스테이지별 배경 | `StageViewProp` 데이터 | **쉬움** | 데이터 주도 |
| 운영 DB 전투 | 없음(View) | **쉬움** | T1/G7 해소됨 |
| 새 미리보기 종류 | `CombatViewPreviewKind`·`CombatViewPreviewData`(Buffer 공용), `CombatInputPolicy.StageOf`, Core 응답, 소유 입력 객체의 `PreviewAnswered` | 보통 | 소유자별 번호 일치 패턴을 따르면 된다 |
| 새 HUD·표시 요소 | `CombatSceneRoot` 필드·검증, `CombatSceneDisplay` 생성·Install·Refresh·Clear·Release, 필요하면 `ICombatBattleDisplay` 메서드, 테스트 `FakeDisplay` | 보통 | 넓은 표시 계약 때문에 가짜 구현을 같이 고쳐야 한다 |
| Title 화면·새 목적지 | `LifeCycleKeys`, 새 LifeCycle, `SceneIndices`·Build Settings, `ExitSequence.ValidateDestination`, `RunEndDestination`, `GameBootstrap.StartLifeCycle` | 보통 | 인계 문서 R6 |
| **새 명령 종류**(예: 스킬 사용) | Buffer `CombatViewCommandKind`·생성 함수(공용), Core 처리(Core), `CombatInputPolicy.StageOf`, `IStageInputReceiver`·Controller·`StageInputNames`·Input Actions(입력), `CombatInputRouter.Request`·`Route`, 소유 입력 객체(새로 만들거나 공격 쪽 확장), `CombatInteraction` 조합 | **어려움** | 7~9파일, 3개 작업자 영역. 10-06보다 나아졌다(분배는 Router 한 곳) |
| 연속 입력 예약(대기 중 다음 명령 받기) | `CombatCommandGate`(대기 하나), `CombatInputPolicy.PassesCommon`, Flow 확인 | **어려움** | "대기 하나" 불변식(I6)을 바꾸는 설계 변경 |
| 세 번째 실행 모드 | M7의 19곳 | **어려움** | 모드 정책 객체 선행 |
| 보드·유닛 규모 확대 | §6의 전체 다시 그리기 경로 | 측정 필요 | 계획 §8 X4 |

### 5.2 열려 있는 확장 지점 (바꾸지 않고 더하는 곳)

- 연출: `ICombatPresentation`(교체 가능), `ICombatResultPresenter.BeginEvent`의 완료 콜백.
- 표시: `ICombatBattleDisplay` 구현 교체(테스트는 이미 가짜로 교체한다).
- Job: `CombatPlan.Register`의 `JobType` 분기(공용 enum이라 추가는 공유 필요).
- LifeCycle: `LifeCycleSystemFactory.Register`에 구간 추가.
- 입력: Controller·Layer 등록 목록(`FrameworkRuntimeFactory`).

---

## 6. 성능 (정적 분석, 미측정)

### 6.1 빈도별 경로

| 빈도 | 경로 | 비용 성격 | 판단 |
|---|---|---|---|
| 매 프레임 | `CombatViewSystem.Tick`: `ReadFacts` 4~6회(`IsTerminal`·`SyncAbandon`·`PathInput.OnPreview`·`RefreshReturnPreview`), `Observe`, 포기 버튼 값 비교 | 구조체 생성·Buffer 필드 읽기. 경로 판단 때만 명령 목록 앞에서부터 찾기 | 할당은 보이지 않는다. 호출 횟수만 많다 |
| 매 프레임 | `TimelineUI.Update`(UI 작업자 영역) | 타임라인 순회, 슬롯마다 Sprite 찾기 | **측정 필요.** 변경이 없어도 매 프레임 다시 계산한다. `CombatLogUI.Update`는 기록 수가 같으면 바로 반환하므로 해당하지 않는다(2026-10-09 정정) |
| 포인터 이동(경로) | `CombatPointerMapper.TryMapPointer`: `EventSystem.RaycastAll` + 평면 Ray | UI Graphic 수에 비례 | 이벤트 데이터·결과 목록은 재사용한다(V1). Raycast 자체 비용은 남는다 |
| 포인터 이동(경로) | `CombatMovementInteraction.OwnsLatestRequest` → `SamePath`의 LINQ `SequenceEqual` | 호출마다 열거자 할당 가능 | 작음. 루프 비교로 바꾸면 0 |
| 미리보기 응답 | 공격 범위·영향 칸 `ToArray()` 3곳 | 응답 크기만큼 복사 | 빈도가 낮다 |
| **결과 1건** | `ReadDisplayValues` → Core `GetTimeline` 예측 계산 + 새 배열 | Core 계산 + 할당 | E1이 해결되면 사라진다 |
| 결과 1건 | `CombatSceneEventPresenter.Refresh`: 캐릭터마다 `ToFacts()` | 캐릭터 수만큼 객체 생성 가능 | 측정 필요 |
| 결과 1건 | `FieldUnitPresenter.RenderFacts` 전체 순회, 유닛 HUD 특성마다 `status.Key.ToString()` | 유닛 수 × 특성 수 문자열 | 측정 필요 |
| 결과 1건 | `RenderActiveActor` → `FieldView.SetLayerCells` → `ClearLayer`가 전체 칸 순회 | 보드 칸 수 | 현재 보드 크기에서는 작다 |
| **사건 1건** | `BeginEvent` → `RemoveAll`(람다) → `RenderEvents` 전체 순회 | 사건 수 × 유닛 수 | 규모가 커지면 바뀐 개체만 갱신(10-06 M1) |
| Job 등록 | `List` 생성 + 구조체 boxing | 명령마다 한두 번 | 무시 가능 |

### 6.2 지연

- **입력 → 화면:** 최소 1프레임. Flow가 LifeCycle Tick(프레임 끝)에서 등록하고, Runner가 다음 프레임 Global Tick에서 실행하고, 같은 프레임 View Tick에서 표시한다(상세 UML §3.2).
- **자동 진행:** 걸음마다 최소 1프레임 + 연출 시간. 적 10걸음이면 연출이 즉시 끝나도 60fps에서 약 0.17초다. 지금 규모에서는 문제가 되지 않는다. 걸음 수가 크게 늘면 "연출이 없는 걸음은 같은 프레임에 이어서 등록" 같은 최적화를 검토할 수 있지만 Flow의 Tick당 1등록 불변식(I1)을 바꾸는 일이라 설계가 필요하다.
- **미리보기:** 명령과 같이 1프레임. 연출 중에도 처리된다.

### 6.3 측정 계획(계획 §8 X4와 같음)

Unity Profiler(Deep Profile 끔)로 다음 구간의 **프레임당 GC Alloc·CPU 시간**을 잰다: 유휴, 배치 Drag, 공격 호버, 경로 드래그, 자동 진행 10걸음 연속, 전투 종료. 목표는 유휴 0 B/frame, 입력 중 할당은 응답 크기에 비례하는 것만이다. 측정 전에는 위 표의 어느 것도 병목이라고 단정하지 않는다.

---

## 7. 권고 (우선순위)

| 순위 | 항목 | 근거 | 담당 | 착수 조건 |
|---|---|---|---|---|
| 1 | **타임라인 출처 하나로**: `CommandState.Timeline`이 VR0 계약인지 확인 → HUD도 Buffer 읽기 → `GetTimeline` 의존 삭제 | E1(SSOT·경계), 결과마다 계산 제거 | Core 확인 → View | Core 답변 |
| 2 | EndBattle 정리 조건을 Core 계약으로 명시 | E2(숨은 계약) | 골격 → Core 문서 | 바로 가능(문서) |
| 3 | 낡은 주석·요약 위치·null 검사 정리 | M1·M2·M4 | View·골격 | 바로 가능(작은 수정) |
| 4 | 테스트 전용 운영 코드 제거(`BeginPreparation` 경로, `FieldUnitPresenter.Render`) | E6·M5 | 골격·View(+Core `PrepareBattle`) | 테스트 이전 뒤 |
| 5 | 포기 UI를 `CombatViewSystem`에서 분리 | R1 | View | 다음에 이 파일을 크게 고칠 때 |
| 6 | Run 종료 판정·목적지를 작은 판정 객체로 | R2·M6 | 골격 | S3 또는 Title 착수 때 |
| 7 | 타임라인 UI 열기/표시 조건 원본 정하기 | E5 | View + UI 작업자 | 1번과 함께 |
| 8 | 성능 측정 후 사건별 증분 렌더 | §6 | View | X4 측정 결과 |
| 9 | 스타일 정리 | M3 | View | 충돌 위험이 낮을 때 |

1·2·4번은 Core 영역에 닿는다. 구현하지 말고 [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 요청으로 적는 것부터 한다(CLAUDE.md 규칙).
