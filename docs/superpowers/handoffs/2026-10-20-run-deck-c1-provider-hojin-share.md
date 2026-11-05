# 런 덱 C1 편집 창 제공자 — 호진님 공유 (Stage 흐름)

작성: 2026-10-20, View 작업자(용근) 세션. **미전달.**

천명님 C1 요청([stage-availability-request](2026-10-19-run-deck-stage-availability-request.md))의 Flow 쪽을 사용자 지시로 이 세션이 구현했습니다. 호진님 파일을 고쳤으니 확인 부탁드립니다.
- 설계: [run-deck-view-integration-design](../specs/2026-10-20-run-deck-view-integration-design.md) RV1
- 계획: [run-deck-view-integration-plan](../plans/2026-10-20-run-deck-view-integration-plan.md) RD-1

## 바뀐 것

**Flow 셋 — 읽기 전용 속성만 추가, 동작은 그대로입니다.**
- 등록 판단(`TryRegister`)이 새 속성과 같은 식을 부르도록 묶었습니다.
- `StageNodeEntryFlow.HasUnregisteredInput`: `TryGetNextCommand(handled)`
- `ShopFlow.HasUnregisteredInput`: `TryGetNextCommand(0) && Number > handled`
- `RecoveryFlow.IsWaiting`, `RecoveryFlow.HasUnregisteredRequest`: `!_waiting && CanReserveExecution`

**`StageFlow` — 읽기 전용 속성 셋 추가.**
- `IsStopped`: 전체 또는 도메인 하나가 정지
- `HasPendingWork`: 처리 확인 대기
- `HasUnregisteredRequest`: 제출됐지만 미등록

**새 파일**
- `IStageDeckEditFacts`(internal): 창이 읽는 사실 11개
- `StageDeckEditWindow`: `IDeckEditAvailabilityRead` 구현. 사실은 갖지 않고 판단 순서표만 가집니다.

**`StageLifeCycle`**
- 생성자에 선택 인자 `deckEditWindow`가 생겼습니다(기존 시험 호출은 그대로).
- `IStageDeckEditFacts`를 명시 구현합니다.
- `OnBindServices` 끝에서 `Bind(this)`, `OnShutdown`에서 `Unbind`를 부릅니다.
- 로딩 서비스를 필드 `_loading`으로 보관합니다(전에는 Flow에만 넘김).

## 판단 순서 (앞에서 걸리면 그 이유)

| 순서 | 조건 | 이유 |
|---|---|---|
| 1 | 종료 뒤 | `Stopped` |
| 2 | 바인드 전 | `NotReady` |
| 3 | 오류 | `Faulted` |
| 4 | 활성 수명이 Stage가 아님 | `InactiveStage` |
| 5 | 로딩 중 | `Loading` |
| 6 | 전투 복귀 배출 대기, 또는 Runner 바쁨 | `JobPending` |
| 7 | Flow 또는 도메인 정지 | `Stopped` |
| 8 | 맵 미준비, 또는 로스터 없음 | `NotReady` |
| 9 | 처리 확인 대기 | `JobPending` |
| 10 | 제출됐지만 미등록인 맵·상점·회복 요청 | `RequestPending` |
| 11 | `HasSelection && !IsCleared(현재 맵, 선택 노드)` | `InteractionOpen` |
| — | 모두 통과 | `None` |

- 노드는 들어갈 때 `SelectStage`, 끝날 때 `ResolveStage` → `SetClear`가 됩니다. 그래서 끝난 상점·회복 기록은 잠금이 아니고, 숨겨 둔 팝업은 노드가 클리어되지 않았으니 잠금이 유지됩니다.
- 전투 복귀 배출 동안은 Flow가 정지 상태라서, 6번을 7번보다 먼저 봅니다.

## 확인 부탁

- 6번·7번·11번이 Stage 흐름의 의도와 맞는지.
- 도메인 하나가 기록 누락으로 정지했을 때도 편집을 막는 것(`Stopped`)이 괜찮은지.

## 남은 시험 (시험 코드는 이번 범위 밖)

C1 요청 §2의 제공자 시험 여섯:
- `IdleRunner_WithSubmittedUnregisteredMapInput_IsUnavailable`
- `CompletedShopRecord_DoesNotKeepWindowLocked`
- `HiddenUnfinishedInteraction_RemainsUnavailable`
- `RecoveryPendingConsumption_RemainsUnavailable`
- `PendingCombatReturnDrain_RemainsUnavailable`
- `DeckOwnPopupBlock_IsNotInteractionLock`

C1 요청 §4의 조립 시험 하나(`FrameworkRuntimeFactory.Create` 경유, `AuthorityUnbound`가 아님).

## 추가 (RD-2) — `LobbyDepartureText` 문구

[Core → View 인계](2026-10-19-run-deck-core-view-handoff.md) §6에 맞춰 출발 실패 문구를 고쳤습니다.
- 추가: `OwnershipLimitReached`(7) "보유 한도에 도달했습니다.", `InvalidInitialDeck`(8) "시작 덱 데이터가 올바르지 않습니다."
- 삭제: `MainAlreadyOwned` 분기(Core가 더 이상 내지 않음). 기본 문구는 그대로입니다.
- **시험 영향(고치지 않음):** `LobbyDepartureViewTests.cs:121`의 `[TestCase(MainAlreadyOwned, "메인 캐릭터가 이미 등록되어 있습니다.")]`가 이제 기본 문구를 받아 실패할 것입니다. 지우거나 7·8번 사례로 바꿔야 합니다.

## 추가 (RD-3) — 로드아웃 초기 덱 번호 검사 (DB)

E2 인계 §3을 사용자 지시로 구현했습니다.
- `PlayerLoadoutData.ValidateInitialDeckIndices()`: 비어 있지 않음, 서로 다름, 펼친 지급 순서(메인 1 + 항목 Count 합) 범위 안.
- `PlayerLoadoutTable` 생성자가 `Validate()` 다음에 부릅니다.
- **`Validate()`에는 넣지 않았습니다.** Core `PlayerLoadoutRosterPlanner`가 새 Run 때 `Validate()`를 부르기 때문입니다. 넣으면 Core 시험이 기대하는 구체 덱 사유(`EmptyDeck` 등)보다 예외가 먼저 납니다.
- 덱 규칙(메인 하나·최대 4장)은 넣지 않았습니다. 원본은 Core `DeckRules`입니다.
- **시험 영향(고치지 않음):** `InGameDataTableTests`의 `PlayerLoadout(...)` 도우미가 덱 번호를 넣지 않아서, 이 묶음 시험 대부분이 DB 생성에서 실패할 것입니다. 도우미에 한 줄(`initialDeckGrantIndices = [0]`)을 넣으면 됩니다.

## 추가 (RD-4) — StageSelect 덱 편집 팝업 (StageMap)

E1 덱 편집 화면을 상점·회복과 같은 꼴의 셋째 팝업으로 붙였습니다.
- **`StageMapViewSystem`:** `PostBindService`에서 `CoreGroup.Deck`이 있으면 `DeckEditPopupViewFactory`를 만듭니다(없으면 팝업 없음). Scope 두 생성 경로에 `deck:`로 넘깁니다.
- **`StageMapViewScope`:**
  - 회복 다음에 덱 편집 컨트롤러를 만듭니다(같은 `PopupInputSession`, 같은 공통 모달 Layer `StageMapPopupInputLayers.Popup`).
  - Tick 끝에서 `_deckEdit?.Tick()`, 정리 때 `Dispose`를 부릅니다.
  - `CanOpenDeckEdit`: 상점·회복 팝업이 열려 있거나 노드로 이동 중이면 열지 않습니다(열기 버튼이 uGUI라 입력 세션으로 막히지 않음).
  - 편집 가능 여부 자체는 C1 창과 Core가 판단합니다.
- **`StageMapSceneRoot`:** 선택 참조 `deckEditUI`. 비어 있으면 팝업을 만들지 않으므로, 씬을 연결하기 전에도 StageSelect는 그대로입니다.
- 새 파일은 `ViewSystem/StageMap/Deck/` 일곱 개입니다. Core 덱 서비스 호출은 `DeckEditPopupController` 한 곳뿐입니다.
- 공통 모달 Layer 이름이 "ShopPopup"이라 덱 팝업에는 어색합니다. 이름을 바꿀지는 호진님 판단에 맡깁니다.

## 추가 (RD-4 보충) — StageSelect 씬 변경 도구

`ProjectL > Stage > Ensure StageSelect Deck Edit UI`(`Editor/Stage/StageDeckEditUISetup.cs`, 상점 도구와 같은 꼴)를 실행하면 `StageSelect.unity`가 바뀝니다.
- 씬 루트에 `DeckEditUI` 프리팹 인스턴스가 생깁니다(자체 Canvas, 정렬 100 — 상점 110보다 아래).
- `StageMapSceneRoot.deckEditUI`가 연결됩니다.
- 열기 버튼은 우상단입니다. 맵 UI와 겹치면 위치를 옮겨 주세요.
