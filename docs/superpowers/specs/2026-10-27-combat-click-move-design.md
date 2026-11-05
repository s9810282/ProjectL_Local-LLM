# 전투 클릭 이동 설계 (2026-10-27)

- **목표:** 지금의 Drag 이동(행위자를 집어 경로를 그리고 놓기)은 그대로 두고, **이동 범위 안의 칸을 클릭하기만 해도 그 칸까지 최단 경로로 이동**하는 입력을 더한다. 유저는 두 방식을 섞어 쓸 수 있다.
- **상태:** 설계 초안(코드 변경 없음). 정할 것 Q-CM1~Q-CM5(§4), 권장안 기준으로 썼다.
- **결정(2026-10-27 사용자):** Q-CM5 B — 이 세션(천명님, Core)은 Core 부분(CM-a)만 구현하고, View 부분(CM-b~CM-d)은 이 문서를 용근님께 넘겨 맡긴다. View만의 질문(Q-CM1·Q-CM4)은 용근님이 정한다(§4.1).
- **출발점:** 플레이 피드백 "원하는 위치를 클릭하기만 해도(범위 내) 최단 path로 이동했으면 좋겠다".
- **바꾸지 않는 규칙:** [이동 Drag 입력 설계](2026-10-13-combat-move-drag-input-design.md)의 Drag 시작·확정·취소(M1~M3), [이동 모드 범위 설계](2026-10-19-combat-move-mode-range-design.md)의 범위 수명(MR2~MR4), 공격 모드의 클릭 = 공격, [공격 Drag → 이동 전환](2026-10-16-attack-drag-to-move-design-Hojin.md).

## 0. 지금 있는지

**없다.** 경로는 Drag로만 만들어진다.

- 경로 입력은 **행위자 칸을 누를 때만** 시작하고 그 밖의 누름은 무시한다(`CombatPathInput.SelectPressed`, `CombatPathInput.cs:408-424`, Drag 설계 M2).
- 경로는 포인터가 지나간 칸을 한 칸씩 받아 만든다(`CombatMovementInteraction.PointerMoved`, `CombatMovementInteraction.cs:137-202`). 목적지 하나에서 경로를 만드는 코드는 View·Core 어디에도 없다.
- Core에 경로 탐색은 적 AI 전용 하나뿐이다(`EnemyMovementPathfinder`, 공격 위치까지, 알려진 지뢰 제외). 플레이어 이동에는 쓰이지 않는다.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | Drag 확정은 놓은 칸이 경로 끝점일 때만 된다. 시작 칸에서 놓으면 취소다. | `CombatMovementInteraction.Confirm` `:256-273`, Drag 설계 M3 |
| F2 | 전투 국면에서 누름은 **경로 입력보다 먼저 유닛 선택**으로 간다. 공격 모드·경로 지정 중이 아니면 `InspectUnit`이 돌고, 빈 칸을 누르면 선택이 풀린다. 그래서 지금 범위 칸을 클릭하면 **선택이 풀려 이동 모드 행위자가 사라진다.** | `CombatViewSystem.RouteInput` `:1016-1027`, `CombatSelectionState.SelectCell` `:35-54` |
| F3 | Core 이동 범위는 BFS로 칸별 거리만 계산한다. 결과 `MovementDestination`은 목적 칸·점유 칸(지금은 목적 칸 하나)·걸음 수·행동 비용만 갖고 **경로는 없다.** | `CombatMovementCalculator.Range` `:33-65`, `MovementDestination.cs` |
| F4 | 범위 BFS와 경로 검증은 같은 진입 판정(`CanEnter`)과 같은 허용 걸음(`MoveRange`)을 쓴다. 그래서 범위 BFS가 찾은 경로는 언제나 검증(상하좌우 한 칸·반복 없음·진입 가능·걸음 한도)을 통과하는 꼴이다. | `MovementRouteValidator.Validate` `:13-41`, `CombatActionAdmission.MovementAllowance` `:42` |
| F5 | 이동의 행동 비용은 걸음 수와 무관한 고정값이다(범위의 `ActionCost` = `MoveCost`). 범위 안에서 더 긴 경로를 골라도 비용이 같다. | `CombatMovementCalculator` 머리 주석·`Range` `:60-64` |
| F6 | 범위 BFS는 지나가는 칸의 진입 효과를 보지 않는다. 진입 효과는 타일 규칙 넷(지뢰·가시밭·깃발·보안관 배지)과 약한 지반 진입 등록이다. Drag에서는 유저가 경로로 고르지만, 클릭에서는 **경로 고르는 규칙이 이 효과를 밟을지 정한다.** | `IMovementTileRule` 구현 넷, `CombatMovementCalculator.ProjectRoute` `:100-110` |
| F7 | 제출은 Core 이동 견적(+ `MovementPreviewContext`)을 받은 뒤에만 된다. "끝점을 정하고 견적을 기다렸다가 승인되면 제출"하는 동결 확정 흐름이 이미 있다(놓은 뒤 승인 대기). | `CombatMovementInteraction.Confirm`·`OnPreview` `:204-254`·`SubmitFrozen` `:394-399` |
| F8 | 범위 응답은 범위 소유자가 한 번 받아 표시에 넘기고, 표시는 목적 칸만 남긴다. 경로 입력은 범위 응답 값을 갖고 있지 않다. | `CombatMovementRangeTracker.TryShowAnswer` `:85-99`, `CombatMovementPlanningPresenter.ShowRange` `:124-138` |
| F9 | 초안 표시(`ShowDraft`)는 편집 시작(고스트·원래 자리 투명)과 따로 부를 수 있다. 견적 없이(`preview == null`) 부르면 끝점을 중립색으로 그린다. | `CombatMovementPlanningPresenter.ShowDraft` `:45-55` |
| F10 | 미리보기 슬롯은 하나이고 배경 요청(이동 모드 범위·유닛 호버 범위·선택 가용성)이 같이 쓴다. | `CombatPreviewSlotRule`, 호버 설계 UR4 |

## 2. 설계

### CM1. 몸짓: 범위 칸 클릭

이동 모드(행위자가 있고 경로 지정 중이 아님, 조건 원본 `CombatPathInput.PlanningActor(Path)` 그대로)에서:

| 입력 | 결과 |
|---|---|
| 행위자 칸 누름 → 끌기 → 놓기 | 지금 Drag 그대로 |
| **보이는 범위의 목적 칸을 누름 → 같은 칸에서 뗌** | **클릭 이동**: 그 칸의 경로로 견적 → 승인되면 제출(CM5) |
| 범위 칸을 누른 뒤 다른 칸·필드 밖·UI에서 뗌 | 아무 일 없음(클릭 후보만 버림, 선택 유지) |
| 범위 밖 빈 칸 클릭 | 지금처럼 선택 해제(Q-CM4) |
| 다른 유닛 클릭 | 지금처럼 선택 바꿈(정보창) |
| 공격 모드에서 칸 클릭 | 지금처럼 공격 |
| 클릭 후 승인 대기 중 새 누름 | 무시(Drag 놓은 뒤와 같음) |

- 클릭 판정은 **칸 기준**이다(누른 칸 = 뗀 칸). 화면 픽셀 임계값은 두지 않는다. 공격 Drag 전환 설계 §3.1의 "누른 칸을 벗어나면 Drag" 기준과 같은 축이다.
- "범위 칸"은 **지금 보이는 범위 응답**의 목적 칸이다. 범위 응답을 아직 못 받았으면(요청 중) 클릭 이동은 되지 않는다.
- 우클릭·포커스 상실·턴 종료·공격 모드 진입은 클릭 후보와 승인 대기를 지금 Drag와 같은 경로(`CancelInputs`·`LeaveMoveMode`)로 취소한다.

### CM2. 선택과의 순서: 경로 입력이 누름을 가져간다

F2 때문에 그대로 두면 범위 칸 누름이 선택을 먼저 푼다. 규칙:

- 조정자의 선택 조건(`RouteInput`의 `!IsAttackActive && !IsPathPlanning`)에 **"경로 입력이 이 누름을 가져감"**을 더한다. 판정은 경로 입력의 질문 하나(가칭 `ClaimsPress(x, y)` = 이동 모드이고, 누른 칸이 보이는 범위의 목적 칸)이고, `CombatInteraction`이 위임한다.
- 선택 규칙(`CombatSelectionState`)은 이동을 모르게 둔다. "빈 칸은 해제" 규칙은 그대로이고, 누름이 선택으로 가지 않을 뿐이다.

### CM3. 경로의 원본은 Core 범위 응답

**Core 범위 BFS가 목적 칸마다 경로를 함께 낸다.** `MovementDestination`에 경로 칸 `Route`(`MovementPath`, 행위자 칸 ~ 목적 칸)를 더한다.

- 이유: 경로 고르기는 진입 효과(F6)를 아는 규칙이라 View가 정하면 규칙 지식이 View로 샌다(P6 위반). Core 범위가 이미 같은 BFS를 돌고(F3) 그 경로는 검증을 통과하는 꼴이라(F4) 추가 왕복 없이 낼 수 있다.
- 경로 고르는 규칙은 Q-CM2. 권장(A)은 **걸음 수 최소, 같으면 플레이어에게 공개된 진입 효과 칸을 적게 밟는 쪽, 그래도 같으면 방향 순서(오른쪽·위·왼쪽·아래)로 고정.** 걸음 수가 균일하므로 BFS 층마다 (걸음, 효과 수) 사전순으로 부모를 고르면 된다(Dijkstra 불필요).
- "공개된 진입 효과 칸"의 판정은 Core 한 곳에 둔다. 범위 BFS가 쓰는 공개 환경(`VisibleTo(owner)`)과 같은 시점으로 본다. 숨은 지뢰는 모르므로 피하지 않는다(Drag와 같다).
- 생성자는 `Route`를 필수 인자로 받는다(호환 인자 없음). 호출처: Core 1곳, Core 시험 1곳(`CombatTraitContractTests:399`), View 시험 약 10곳(§3).

### CM4. 범위 소유자가 받은 응답을 질문으로 낸다

- `CombatMovementRangeTracker`가 소비한 범위 응답(`MovementRangeResult`)을 보이는 사실(`_shownFacts`)과 함께 갖고, `TryGetRoute(facts, cell, out MovementPath route)`로 답한다. 사실이 다르거나 안 보이면 false다.
- 범위 칸 강조와 클릭 경로가 **같은 응답 하나**에서 나온다(SSOT). 지우는 시점은 지금의 `Clear` 그대로다.

### CM5. 확정: 기존 동결 확정 흐름을 탄다

- `CombatMovementInteraction`에 입구 하나(가칭 `BeginRoute(actor, route, allowed)`)를 더한다: 초안을 경로 전체로 채우고(`Draft.Begin` + 칸마다 `Accept`) 바로 동결 확정(`Confirm`과 같은 상태)으로 견적을 요청한다. 승인되면 `SubmitFrozen`, 불가면 Drag의 "놓은 뒤 불가"와 같게 취소 + 거절 안내(`MovementRejected`).
- 경로 입력은 클릭 이동을 "놓은 뒤 승인 대기"인 지정으로 연다(`_pathActor`·`_released = true`). 그래서 대기 중에는 `IsPlanning`이 참이라 다른 입력이 막히고 커서 스냅·결과 재생 전 취소 등 수명 규칙이 Drag와 같다.
- 확정 직전 검사는 Drag와 같다(`ValidatePlanning`·`AllowsInput`). 견적·context 검증은 Core가 그대로 한다. 새 Core 명령·새 Buffer 종류는 없다.

### CM6. 호버 미리보기 (Q-CM1 A일 때)

- 이동 모드에서 포인터가 범위 목적 칸 위에 있으면 그 칸의 경로를 **기존 초안 표시**로 그린다(`ShowDraft(route, null, true, cell)`, F9). 끝점은 중립색이다.
- 고스트·원래 자리 투명(`BeginMovementPlanning`)은 하지 않는다. 클릭해서 승인 대기에 들어갈 때 Drag처럼 켤지는 계획에서 정한다.
- **Core 견적은 요청하지 않는다**(슬롯을 쓰지 않음, F10). 견적은 클릭 순간 한 번이다. 범위 밖으로 나가면 초안을 지운다. 그린 것만 지우는 표지(`_draftShown`, RT8)를 그대로 쓴다.
- 다른 입력 소유자가 활성이거나 결과 재생 중이면 그리지 않는다(이동 모드 조건이 이미 막음).

### CM7. 바꾸지 않는 것

- Drag 이동·공격 Drag 전환·공격 클릭·배치 Drag·우클릭 회수.
- 이동 모드 범위의 요청·수명·슬롯 규칙(MR2~MR4). 클릭 이동은 범위를 새로 요청하지 않고 보이는 것만 읽는다.
- 튜토리얼 `MoveOnly` 제한: 클릭 이동도 같은 Move 명령이라 그대로 허용된다.

## 3. 바뀌는 곳 (권장안 기준)

| 영역 | 파일 | 내용 |
|---|---|---|
| Core(천명님) | `Contracts/Results/MovementDestination.cs` | `Route` 추가(필수) |
| Core | `Application/Combat/CombatMovementCalculator.cs` `Range` | BFS 부모 기록·경로 고르기(Q-CM2) |
| Core | 진입 효과 칸 판정(새 함수 또는 기존 규칙 곁) | Q-CM2 A일 때 |
| Core 시험 | `ProjectL.Tests.Core` | 범위 경로 시험(최단·효과 회피·방향 순서·검증 통과) |
| View(용근님 영역) | `Input/MovePath/CombatMovementRangeTracker.cs` | 응답 보관·`TryGetRoute` |
| View | `Input/MovePath/CombatPathInput.cs` | 클릭 후보·`ClaimsPress`·호버 초안·클릭 확정 |
| View | `Input/MovePath/CombatMovementInteraction.cs` | `BeginRoute` |
| View | `Input/Interaction/CombatInteraction.cs` | `ClaimsPress` 위임 |
| View | `CombatViewSystem.cs` `RouteInput` | 선택 조건 한 줄 |
| View 시험(컴파일만) | `MovementDestination` 생성 약 10곳 | 인자 맞춤(`CombatInteractionTests`·`CombatMovementRange*Tests`·`CombatMovementDraftDisplayTests`·`CombatSceneEventPresenterTests`) |
| 문서 | `viewDocs/stage-input-guide.md` §2.2 | 조작 설명에 클릭 이동 |

Factory·ServiceKeys·Framework·씬·프리팹·DB는 바뀌지 않는다.

## 4. 정할 것

| # | 질문 | 선택지 (권장 먼저) |
|---|---|---|
| Q-CM1 | 클릭 전에 경로를 보여 주나 | **A 호버하면 경로 초안 표시, 클릭 한 번으로 이동**(CM6) / B 미리보기 없이 클릭 즉시 이동 / C 첫 클릭 = 경로 표시, 같은 칸 다시 클릭 = 이동 |
| Q-CM2 | 경로 고르는 규칙 | **A 최단 걸음, 같으면 공개 진입 효과 칸을 적게, 그다음 방향 순서** / B 순수 최단(방향 순서만) / C 범위 안에서 효과 칸 최소, 같으면 최단(비용이 고정이라 가능, F5) |
| Q-CM3 | 경로를 누가 만드나 | **A Core 범위 응답에 경로를 실음**(CM3) / B View가 범위의 걸음 수로 되짚어 만듦(Core 변경 없음, 대신 효과 회피 불가·경로 규칙이 View에) |
| Q-CM4 | 범위 밖 빈 칸 클릭 | **A 지금처럼 선택 해제** / B 선택 유지(아무 일 없음) |
| Q-CM5 | View 쪽 구현 | View 경로 입력은 용근님 영역이다. A 이 세션에서 구현하고 용근님께 변경 정리를 넘김 / **B Core(CM3)만 구현하고 View는 이 문서를 넘겨 용근님이 구현 — 결정(2026-10-27)** |

### 4.1 Q-CM5 B에 따른 정리

| 질문 | 누가 정하나 | 상태 |
|---|---|---|
| Q-CM2 경로 규칙 | 천명님(Core) | 대기 — CM-a에 필요 |
| Q-CM3 경로를 누가 만드나 | 천명님(Core) | 대기 — B면 Core 작업이 없어진다. A 전제로 CM-a를 둔다 |
| Q-CM6 `Route` 필수 인자 때문에 깨지는 View 시험 줄 | 천명님 | 대기 — 아래 |
| Q-CM1 호버 미리보기 | 용근님(View) | CM-b~d에서 |
| Q-CM4 범위 밖 빈 칸 클릭 | 용근님(View) | CM-b~d에서 |

**Q-CM6.** `MovementDestination` 생성자를 부르는 시험 줄 9곳 중 8곳은 Droute(View 시험) 작성이고 1곳(`CombatTraitContractTests:399`)만 천명님 작성이다(`git blame`, 2026-10-27). `Route`를 필수로 하면 8곳이 컴파일되지 않아 시험 어셈블리 전체가 멈춘다.

- **A(권장)** 허락받고 CM-a에서 8곳의 생성 인자만 맞춘다(단언 그대로, 경로는 시작 칸~목적 칸 한 걸음). 넘기는 문서에 줄 목록을 적는다.
- B 고치지 않고 넘기는 문서에만 적는다. 용근님이 고칠 때까지 시험이 컴파일되지 않는다.

| 파일 | 줄 |
|---|---|
| `Framework/CombatInteractionTests.cs` | 1348 |
| `Framework/CombatMovementRangeTrackerTests.cs` | 195 |
| `Framework/CombatMovementRangeRequestTests.cs` | 36 |
| `Framework/CombatMovementRangeDisplayTests.cs` | 68·83·95 |
| `Framework/CombatMovementDraftDisplayTests.cs` | 151 |
| `Framework/CombatSceneEventPresenterTests.cs` | 664 |

## 5. Phase (권장안 기준)

1. **CM-a Core(이 세션, 천명님):** `MovementDestination.Route`, 범위 BFS 경로 고르기, Core 시험(`ProjectL.Tests.Core`). 호출처 컴파일 맞춤(Q-CM6). 화면 변화 없음. 끝나면 이 문서 + Core 계약(§3 Core 줄) + 고친 시험 줄 목록을 용근님께 넘긴다.
2. **CM-b View 클릭 확정(용근님):** 범위 소유자 질문, `BeginRoute`, 클릭 후보·`ClaimsPress`·선택 조건. 호버 없이 클릭 이동만 된다.
3. **CM-c View 호버 초안(용근님):** CM6, Q-CM1에 따라.
4. **CM-d 문서(용근님):** 입력 안내 §2.2, 튜토리얼 문구에 Drag만 적혀 있으면 호진님 공유.

Phase마다 멈추고 보고한다. CM-b~d의 진행 방식(시험·컴파일 확인)은 용근님 작업 규칙을 따른다.

## 6. 원칙 점검

- **SSOT:** 경로 규칙은 Core 범위 한 곳, 범위 칸과 클릭 경로는 같은 응답 하나, 이동 모드 조건은 `PlanningActor` 하나, 확정·제출은 기존 동결 흐름 하나.
- **SRP:** 범위 소유자는 응답 보관·질문만 더하고 언제 쓰는지 모른다. 선택 상태는 이동을 모른다. 클릭 판정은 경로 입력 안에서 끝난다.
- **OCP/ISP:** 새 입력 소유자·새 Layer·새 Buffer 종류 없이 경로 입력의 입구 둘(`ClaimsPress`·`BeginRoute`)과 질문 하나만 더한다.
- **FW:** UnityBridge → InputSystem → 수신 슬롯 → Router 경로 그대로. 새 서비스·모듈 없음.
- **기능 독립성:** Drag 경로·Drag 시험은 그대로 돈다(입구가 다름). Core 변경은 범위 응답에 값 하나를 더하는 것이라 이동 계산·검증은 바뀌지 않는다.
- **변경 영향:** `MovementDestination` 생성자 호출처(Core 2·View 시험 약 10)는 같이 고쳐야 한다. 범위 BFS가 부모를 기록해 응답 크기가 범위 칸 수 × 경로 길이만큼 커진다(허용 걸음이 작아 수백 칸 이하).

## 7. 확인 (사용자 Unity)

- 이동 모드에서 범위 칸 호버 → 경로 초안 → 클릭 → 이동. 행위자 Drag 이동은 전과 같음.
- 범위 칸 누른 뒤 다른 칸에서 떼기·우클릭 → 이동 없음, 선택 유지.
- 공개된 지뢰·가시밭을 돌아가는 같은 길이 경로가 있으면 그쪽으로 감(Q-CM2 A).
- 공격 모드 클릭은 공격, 다른 유닛 클릭은 선택 변경, 범위 밖 빈 칸 클릭은 선택 해제.
- 이동 뒤 남은 이동이 있으면 새 위치의 범위로 다시 클릭 이동 가능.
