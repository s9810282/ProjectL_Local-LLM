# 런 덱 View·Flow 연결 설계 (RV1~RV9)

작성: 2026-10-20, View 작업자(용근) 세션. **설계 초안, 코드 변경 없음.**

사용자 요청은 둘이다.
- "Dev랑 Test 쪽 코드는 제외하고 다시 작업 나열해"
- "검증 과정 건너뛰고 빠르게 설계 계획 진행해"

이 문서는 그 목록을 설계로 바꾼다. 코드는 빠르게 훑어서만 대조했다. 그래서 착수 전에 확인할 사실을 §5에 모았다.

## 0. 범위와 원본

**정책 원본(SSOT)은 Core 쪽 문서다.** 이 문서는 그 규칙을 다시 쓰지 않는다. 각 담당 영역에서 어떻게 붙이는지만 정한다.
- 규칙·계약: Run 덱 설계 (Git history: `2026-10-19-run-deck-main-category-design.md`) §5.2·§5.3(초안·버전), §7.1(편집 창), §8(View 연결)
- View 계약: [Core → View 인계](../handoffs/2026-10-19-run-deck-core-view-handoff.md) §1~§7
- C1 계약: [C1 요청](../handoffs/2026-10-19-run-deck-stage-availability-request.md) §1~§4
- E2 콘텐츠: [E2 인계](../handoffs/2026-10-19-run-deck-content-e2-handoff.md), 실행 기록은 아킴보 메인 전환 계획 (Git history: `2026-10-20-akimbo-main-migration-plan.md`) §3.2

**포함:** C1 제공자와 조립, E1 덱 편집 팝업, View 호출처 이관 넷, `PlayerLoadoutData` 검사, 작업 트리 로드아웃 확인, 문서 정리, E3 수동 인수.

**제외(사용자 지시):**
- 개발 코드: `DevBattleInitializationSource`(key 14 fixture)
- 시험 코드: `InitializeRoster` 시험 이전, 천명님이 바꾼 픽스처 줄 검토, `OwnedStatsUITestServices`, `DataBaseAssetTests` 권장 시험
- 예외 하나: RV4는 시험 한 줄과 컴파일이 묶여 있다(§2 RV4, Q-RV7).

## 1. 확인한 사실 (2026-10-20 HEAD `3eb6be57`, 빠르게 훑음)

**Core·E2**
- Core 계약은 모두 들어와 있다: `IRunDeckService`, `IDeckEditAvailabilityRead`, `DeckEditorSnapshot`, `DeckRuleState`.
- `RunDeckService.ReadAvailability`는 순서대로 판단한다.
  1. 전투 유지 → `BattleRetained`
  2. Run 종료 → `RunEnded`
  3. 제공자가 없으면 → `AuthorityUnbound`
  4. 그 밖에는 제공자의 답
- E2 에셋은 `8a4350e6`으로 커밋됐다: 아킴보 key 10, 로드아웃(아킴보·Cactus Cannon·ShotGun, `[0,1,2]`), Sniper 경로 제거, `CombatEventAudio`.

**C1이 비어 있는 곳**
- `LifeCycleSystemFactory`에는 `Create`만 있다. 제공자도 서비스 키도 없다.
- `RunTimeSystemFactory.Create`는 InGame을 먼저 만들고, `InGameSystemFactory.CreateComposition(battleSource, faults)`에 제공자를 넘기지 않는다.

**Stage 흐름의 사실이 있는 곳 (C1 제공자가 읽을 원본)**
- `StageLifeCycle`
  - `_flow`, `_pendingCombatReturn`, `_isShutdown`
  - `IsMapReady()`
  - `TryGetCombatEntry`(요청 상태)
  - `_manager.CurrentKey`
- `StageFlow.Tick` 조건: `_isStopped`, `_faults.IsFaulted`, `_loading.IsLoading`, `!_runner.IsIdle`
- `StageNodeEntryFlow.HasPendingCheck`, `ShopFlow.HasPendingCheck`, `RecoveryFlow._waiting`(밖에 공개되지 않음)
- 제출됐지만 아직 등록되지 않은 요청
  - 맵·상점: 입력 Buffer에 처리 번호보다 큰 명령이 있음(`TryGetNextCommand`)
  - 회복: `IRecoveryRequestExecution.CanReserveExecution`
- 노드 상호작용: `ICurrentProgressBufferReadSink.HasSelection`·`CurrentStage`·`Shop`·`Recovery`, `IStageProgressBufferReadSink.IsCleared(map, node)`

**View 호출처**
- `DeckCardList.RefreshDeck`(현강님)이 `Owned`에서 `Role == Main`을 찾는다. 매 프레임 `Update`에서 부른다.
- `OwnedCharacterStatsReader`의 `deck`은 선택 인자다.
  - 운영 호출처 둘(`MainCharactorUI`·`UnitCardUI`)은 이미 넘긴다.
  - 시험 도우미 `OwnedStatsUITestServices.cs:146` 한 곳은 넘기지 않는다.
- `LobbyDepartureText`(호진님)에 `MainAlreadyOwned` 분기가 있고 7·8번 문구가 없다. 기본 문구는 "출발을 완료하지 못했습니다."다.
- `CombatDeploymentConfirmation.CheckMainPlacement`(현강님)는 `Owned`에서 역할로 메인을 찾는다. 생성자는 `ICharacterBufferReadSink`만 받는다.

**덱 편집 화면을 둘 자리**
- 덱 편집 화면은 없다. 로비 덱 팝업(`LobbyDeckPopupController`, 호진님)은 "준비 중" 빈 모델만 보여 준다. 설계 §8에 따라 로비 편집은 범위 밖이다.
- StageSelect 팝업은 `StageMapViewScope`(호진님)가 `PopupViewScope`로 연다. 상점·회복이 예다.

**담당**
- `StageLifeCycle`·`StageFlow`·`StageMapViewScope`·`LobbyDepartureText`·DB 표: 호진님
- `DeckCardList`·`OwnedCharacterStatsReader`·`CombatDeploymentConfirmation`: 현강님
- 런타임 조립(`RunTimeSystemFactory`): 민형님
- 우리 파일은 이 목록에 없다.

## 2. 설계

### RV1 C1 편집 창 제공자 (Flow, 호진님 영역)

**자리 (Q-RV2 권장 A):** 새 읽기 전용 클래스 `StageDeckEditWindow`를 둔다(`LifeCycles/Stage/`, `IDeckEditAvailabilityRead` 구현).
- 이 클래스는 **판단 순서 하나만** 가진다. 사실은 갖지 않는다.
- 사실은 지금처럼 `StageLifeCycle`·`StageFlow`·Buffer가 소유한다. `StageLifeCycle`이 좁은 사실 계약 `IStageDeckEditFacts`(internal)를 구현해 창에 넘긴다.
- 새로 쓸 수 있는 불리언은 만들지 않는다(요청 §2). 매 호출마다 원본을 읽는다.

**판단 순서** (`Read()` 하나. 앞에서 걸리면 그 이유를 돌려준다)

| 순서 | 조건 | 이유 |
|---|---|---|
| 1 | 아직 Bind 안 됨 | `NotReady` |
| 2 | Shutdown 뒤, 또는 Flow 정지 | `Stopped` |
| 3 | 오류 경계가 오류 | `Faulted` |
| 4 | 활성 수명이 Stage가 아님 | `InactiveStage` |
| 5 | 로딩 중 | `Loading` |
| 6 | 맵 준비 안 됨(`IsMapReady` 거짓), 또는 로스터 없음 | `NotReady` |
| 7 | 전투 복귀 배출 대기(`_pendingCombatReturn`), Runner 바쁨, Flow 셋 중 하나라도 처리 확인 대기 | `JobPending` |
| 8 | 제출됐지만 미등록인 맵·상점 명령, 회복 예약 가능, 또는 전투 진입 요청이 Created·Queued·Activating | `RequestPending` |
| 9 | 현재 노드 상호작용이 끝나지 않음(§5 F1 확인) | `InteractionOpen` |
| — | 모두 통과 | `None` |

- `BattleRetained`·`RunEnded`는 Core가 먼저 판단하므로 넣지 않는다.
- `AuthorityUnbound`는 Core 전용이라 넣지 않는다.

**공개할 것 (읽기 전용, 동작 변화 없음)**
- `StageFlow.HasPendingWork`: 셋의 `HasPendingCheck` 또는 회복 `_waiting`
- `StageFlow.HasUnregisteredRequest`: 입력 Buffer 비교와 회복 `CanReserveExecution`
- `StageNodeEntryFlow`·`ShopFlow`: "등록 안 된 입력 있음" 판정을 `TryRegister`와 같은 식 하나로 꺼낸다. 판정이 두 벌이 되지 않게 `TryRegister`도 이 식을 부른다.

**수명**
- 창은 Factory에서 만든다.
- `StageLifeCycle.OnBindServices`에서 `Bind(facts)`를 부른다. `OnShutdown`에서 `Unbind`하고, 그 뒤로는 `Stopped`를 답한다.
- 생명주기 콜백이 없으므로 `AddService`만 하고 `AddModule`은 하지 않는다(FW-001/002).

### RV1b C1 조립 (Framework 조립, 민형님 영역)

요청 §3을 그대로 따른다.
- `LifeCycleSystemFactory.CreateComposition(faults)`가 `LifeCycleSystemComposition { Group, DeckEditAvailability }`를 돌려준다. 기존 `Create`는 `CreateComposition(...).Group`으로 남긴다(시험 호환).
- 새 키 `LifeCycleSystemServiceKey.DeckEditAvailability`.
- `RunTimeSystemFactory.Create`는 LifeCycle 조립을 InGame **보다 먼저** 만든다. 그 값을 `InGameSystemFactory.CreateComposition(battleSource, faults, deckEditAvailability)` → `Compose` → `InGameCoreSystemFactory.Create(..., deckEditAvailability)`로 넘긴다.
- 내보내는 순서(Buffer → Global → InGame → LifeCycle)와 공개 `FrameworkRuntimeFactory.Create` 서명은 그대로 둔다.

### RV2 E1 덱 편집 팝업 (StageSelect View, 호진님 영역)

**구성:** 상점·회복 팝업과 같은 꼴로 `StageMapViewScope`가 연다.

| 부품 | 책임 | 비고 |
|---|---|---|
| `DeckEditDraft` (순수 C#) | 초안 규칙 하나 | 기준 스냅샷·후보 ID·마지막 평가·상태(`Fresh`·`Dirty`·`Valid`·`Invalid`·`Stale`·`Unavailable`). 규칙은 Run 덱 설계 §5.3 그대로 |
| `DeckEditPresenter` | 초안 + 스냅샷 → 표시 모델 | 보유 목록, 슬롯 넷, 메인 표시, 병과 수, 이유 문구, 확정 가능 여부 |
| `DeckEditText` | 사유 → 한국어 문구 한 곳 | `DeckFailureReason`·`DeckUnavailableReason` |
| `DeckEditPopupController` | 팝업 수명 | `PopupViewScope`·`IPopupViewController`. 열기·숨기기·해제. `IRunDeckService` 호출은 이곳 한 곳뿐 |
| `DeckEditUI` (MonoBehaviour) | 그리기만 | 카드 그림 부품 재사용(Q-RV4) |

**초안 규칙 (Run 덱 설계 §5.3을 옮긴 것. 원본은 그쪽)**
- 열 때 `Read()`를 부르고 확정 덱으로 초안을 채운다.
- 초안을 바꿀 때마다 `Evaluate`를 부른다. 결과의 `Current`가 기준 버전과 다르면 `Stale`로 두고 확정을 끈다.
- 새 버전을 초안에 몰래 붙이지 않는다. 다시 평가하는 버튼을 눌러야만 새 스냅샷 기준으로 다시 평가한다.
- 확정은 마지막 평가가 `Valid`이고 `Availability.CanEdit`일 때만 켠다.
- `Confirm`이 수락되면 결과의 `Current.Deck`이 새 기준이 된다. 그 뒤 `Read()`를 한 번 부른다. **이 `Read()`가 실패해도 `Confirm`을 다시 부르지 않는다.**
- 거절되면 이유를 보여 준다. 옛 버전 거절(`Stale*`)이면 다시 평가하라고 안내한다.
- 닫기·취소는 로컬 초안만 버린다. 닫힌 뒤에 도착한 결과는 표시만 무시한다(Core 확정은 남음).

**새로 읽는 시점:** 열 때, 호스트가 다시 현재가 될 때, 평가·확정 결과가 올 때뿐이다.
- 매 프레임 Tick 조회는 하지 않는다(7기준 "무의미한 Tick"). 변경 이벤트가 없어도 이 세 시점이면 충분하다.
- 덱 팝업이 열려 있는 동안에는 팝업 입력이 다른 노드 상호작용을 막는다. 열린 동안 바뀌는 길은 Job 배출뿐이고, 그것도 다음 평가에서 `Stale`로 드러난다.

**쓰기 경로:** `Confirm`은 Core가 정한 동기 서비스 호출이다(Run 덱 설계 §7.1).
- 다른 View 쓰기처럼 View 입력 Buffer → Job 경로가 아니다. Core 계약이 정한 예외다.
- View는 Buffer에 쓰지 않는다. `RunDeckWriteSink`는 부르지 않는다.

**진입 (Q-RV3 권장 A):** StageSelect 전용 버튼을 StageMap 루트에 둔다.
- 버튼은 맵 입력이 받을 수 있을 때만 켠다.
- 열 수 있는지는 Core `Availability`가 판단한다. 열 수 없는 상태도 팝업을 열어 이유를 보여 준다(인계 체크리스트 "오류 반복 금지").

**UI 식별:** 새 `UIId.DeckEdit = 32`와 새 팝업 입력 Layer. 프리팹·씬 연결은 사용자 에디터 작업이다.

**카드 내용 (Q-RV4 권장 A):** 카드 정보는 `DeckEditorSnapshot.Stats`로만 만들어 그림 부품에 넣는다.
- 기존 `UnitCardUI`는 Buffer에서 능력치를 따로 읽는다. 그대로 쓰면 "한 번 읽은 묶음만 쓴다"는 계약을 어긴다.

### RV3 `DeckCardList` 참가 목록 표시 (현강님 영역)

- **전투 밖:** `IRunDeckReadSink.TryGetCurrent(out d)`가 참이면 `d.OrderedIds`를 슬롯 0부터 채우고 나머지는 비운다. 거짓이면 모두 비운다.
- **전투 중:** `ICombatStateBufferReadSink.Current?.Membership`의 `OrderedIds`를 쓴다.
- 고르는 규칙은 private 함수 하나(`CurrentDeckIds`)로 둔다. 역할 검색은 지운다.
- `ShowCard(slot, OwnedCharacterRecord)`를 `ShowCard(slot, CharacterInstanceId)`로 바꾼다. 지금도 ID만 쓴다.
- 매 프레임 `Update`는 기존 동작이라 그대로 둔다(범위 밖). 슬롯 수는 4로 유지한다.

### RV4 `OwnedCharacterStatsReader`의 `deck` 필수화 (현강님 영역)

- 매개변수를 필수로 바꾸고 null이면 `ArgumentNullException`을 던진다. 운영 호출처 둘은 이미 넘긴다.
- **시험 도우미 한 줄(`OwnedStatsUITestServices.cs:146`)이 같이 바뀌어야 컴파일된다** → Q-RV7.

### RV5 `LobbyDepartureText` 문구 (호진님 영역)

- 추가: `OwnershipLimitReached`(7) "보유 한도에 도달했습니다.", `InvalidInitialDeck`(8) "시작 덱 데이터가 올바르지 않습니다."
- `MainAlreadyOwned` 분기를 지운다. 기본 문구는 그대로 둔다.
- 끝나면 천명님께 enum 값 제거를 알린다(인계 §6).

### RV6 `CombatDeploymentConfirmation`의 메인 찾기 (현강님 영역)

- `CheckMainPlacement`가 `ICombatStateBufferReadSink.Current.Membership.MainId`로 메인을 정하고 `IsPlaced(FindCharacter(mainId))`를 판단한다.
- 동작은 지금과 같다(필드에는 덱 안 메인만 올라감). 생성자에 읽기 하나가 늘어나므로 조립하는 쪽도 한 줄 바뀐다(§5 F4).

### RV7 `PlayerLoadoutData`의 초기 덱 인덱스 검사 (DB, 호진님 영역)

- **충돌:** Core `PlayerLoadoutRosterPlanner`가 새 Run 때 같은 `Validate()`를 부른다. 인덱스 검사를 `Validate()`에 넣으면 Core 시험 `RunDeckInitializationTests.NewRun_InvalidSelectionOrOverflow_PreservesPreviousRun`이 기대하는 구체 사유(`EmptyDeck` 등)보다 예외가 먼저 난다(아킴보 계획 §3.2).
- **권장 (Q-RV5 A):** 검사를 별도 함수 `ValidateInitialDeckIndices()`로 두고, DB 표 생성 때만 부른다.
  - 검사 내용: 비어 있지 않음, 서로 다름, `[0, 1 + Σcount)` 안.
  - 운영 데이터는 DB 생성에서 이미 걸러지므로 런타임에 이 검사가 다시 걸릴 일은 없다.
  - 덱 규칙(메인 하나·최대 4장)은 넣지 않는다. 원본은 Core `DeckRules`다.

### RV8 작업 트리 로드아웃 확인 (콘텐츠)

- 커밋 안 된 `PlayerLoadout_1.asset`: 아킴보 + ShotGun·Ballista·Cowboy, `[0,1,2]`.
  - 이대로면 덱은 아킴보·ShotGun·Ballista이고, Cowboy는 보유만 한다.
  - 설계 D9(아킴보·Cactus Cannon·ShotGun)와 다르다 → Q-RV6.

### RV9 문서 정리와 E3

**문서**
- E2 인계 §2 "옛 메인 능력치를 복사하지 않는다"에 사용자 결정(그대로 옮김, 아킴보 계획 §0)을 덧붙인다. 천명님 문서라 공유 문구만 쓴다.
- Core 데이터 경계 문서 §5에 다른 작업자 파일을 바꾼 기록을 남긴다(Q-RV1 결과에 따라).

**E3 수동 인수 (C1·E1 뒤)**
1. Play From Boot → 로비 → 새 Run → StageSelect에서 덱 세 장이 보인다.
2. 덱 편집을 열어 순서를 바꾸고 확정 → 닫았다 다시 연다.
3. 상점 노드로 들어가면 편집 불가 이유가 보인다.
4. 전투에 들어가면 `DeckCardList`가 참가 목록을 보인다.

## 3. 원칙 점검 (7기준, 예상치)

| 기준 | 판단 | 예상 |
|---|---|---|
| SSOT | 덱 규칙은 Core 하나, 편집 창 순서는 `StageDeckEditWindow` 하나, 초안 규칙은 `DeckEditDraft` 하나, 문구는 `DeckEditText` 하나. "등록 안 된 입력" 판정은 `TryRegister`와 같은 식을 공유 | 9 |
| SOLID | 창(순서) / 사실(LifeCycle) / 초안(순수) / 표시(Presenter) / 수명(Controller)을 나눔. `StageLifeCycle`에는 사실 계약 구현만 더함 | 8.5 |
| FW 계약 | 서비스 등록 FW-001/002, Factory 조립 순서 유지, View는 Buffer에 쓰지 않음, 팝업은 `PopupViewScope` | 9 |
| 독립성 | C1·E1·호출처 넷·DB 검사는 서로 따로 착수 가능(E1 화면 확인은 C1 뒤) | 8.5 |
| 캡슐화 | Flow 내부 필드는 읽기 전용 속성으로만 공개, 창은 internal | 8.5 |
| C#/Unity | 순수 초안·Presenter는 MonoBehaviour 없음, 그리기만 UI | 8.5 |
| If/Tick | 편집 팝업 Tick 조회 없음, 창은 호출 때만 계산 | 9 |

예상 약 **8.7/10**. 남는 것은 `DeckCardList`의 매 프레임 `Update`(기존, 범위 밖)와 `Confirm`의 동기 쓰기 예외(Core 계약)다.

## 4. 정할 것

| 번호 | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-RV1 | 다른 작업자 영역(호진: RV1·RV2·RV5·RV7, 현강: RV3·RV4·RV6, 민형: RV1b)을 누가 구현하나 | **A. 사용자 지시로 이 세션이 구현하고 담당마다 공유 메모.** 이유: 병합 게이트상 C1·E1이 같이 들어와야 확정이 동작하고, 담당이 갈리면 순서 맞추기가 길어짐. CLAUDE.md의 "명시 지시 예외"에 해당 | B. 요청 문서만 쓰고 각 담당에게 넘김 / C. View 화면(RV2~RV6)만 우리가, Flow·조립(RV1·RV1b)은 요청 |
| Q-RV2 | C1 제공자 자리 | **A. 새 `StageDeckEditWindow`(순서만) + `StageLifeCycle`이 사실 계약 구현** | B. `StageLifeCycle`이 직접 구현(281줄에 판단 9단계가 더해짐) |
| Q-RV3 | 편집 화면 진입 | **A. StageSelect 전용 새 버튼** | B. 기존 `DeckButtonUI`(Global, 전투 배치 목록과 같이 씀) 재사용 |
| Q-RV4 | 편집 카드 내용 출처 | **A. 스냅샷 `Stats`로 내용을 만들어 그림 부품에 넣음** | B. 기존 `UnitCardUI` 재사용(Buffer를 따로 읽음, 계약 위반) |
| Q-RV5 | 로드아웃 인덱스 검사 | **A. 별도 함수, DB 표 생성 때만** | B. `Validate()`에 넣고 Core 시험 기대를 바꿈(천명님 합의 필요) / C. 넣지 않음 |
| Q-RV6 | 작업 트리 로드아웃(ShotGun·Ballista·Cowboy) | 사용자 의도 확인. 의도가 아니면 커밋된 E2 값으로 되돌림 | 의도라면 `[0,1,2,3]`(덱 넷)인지 Cowboy를 덱 밖에 둘지 |
| Q-RV7 | RV4와 같이 바뀌어야 하는 시험 도우미 한 줄 | **A. 컴파일용 한 줄만 예외로 고침**(단정 무변경) | B. RV4를 시험 정리 때로 미룸 |

## 5. 착수 전에 확인할 사실 (검증을 건너뛴 부분)

**RD-0 결과(2026-10-20, 계획 §7.1):**
- F1·F2·F5·F6은 설계와 같다. RV1 판단 9단계 식은 `HasSelection && !IsCleared(CurrentMapKey, CurrentStage.NodeId)`로 확정한다.
- F3: 새 Layer 대신 상점·회복 공통 모달 Layer를 재사용하는 안(Q-RD1)이 생겼다.
- F4: RV6의 생성자를 바꾸면 현강님 시험이 깨진다. 전투 참가자 목록에서 메인을 찾는 안(Q-RD2)이 생겼다.
- 원래 F 목록은 아래에 그대로 둔다.

- **F1** 노드 상호작용 끝 판정: 상점·회복을 마치고 나왔을 때 `IsCleared(map, node)`가 참이 되는지, 그리고 `HasSelection`이 남는지. 아니면 `Shop.HasPurchase`·`Recovery.AppliedOptionId`·처리 번호 중 무엇을 써야 하는지(요청 §2 "완료된 기록은 잠금이 아니다").
- **F2** `StageNodeEntryFlow.HasInput`과 `ShopFlow.TryRegister`의 "등록 안 된 입력" 식을 그대로 꺼낼 수 있는지.
- **F3** StageMap 팝업 입력 Layer 이름 규칙과 `StageMapViewScope`가 팝업 셋째를 받을 자리.
- **F4** `CombatDeploymentConfirmation`을 조립하는 곳과 그 파일 담당.
- **F5** 카드 그림 부품(`UnitCardView`·`UnitCardTraitContent`)이 Buffer 없이 내용 값만 받을 수 있는지.
- **F6** `UIId` 값 29가 비어 있는지, enum 담당(UISystem).
