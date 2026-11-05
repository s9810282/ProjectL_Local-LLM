# 런 덱 View·Flow 연결 Task 계획 (RD-0~RD-5)

작성: 2026-10-20, View 작업자(용근) 세션. **계획만, 코드 변경 없음.**
- 설계: [run-deck-view-integration-design](../specs/2026-10-20-run-deck-view-integration-design.md)(RV1~RV9, 정할 것 Q-RV1~Q-RV7)
- 사용자 지시: "검증 과정 건너뛰고 빠르게". 그래서 Phase마다 MSBuild·관리 실행·검토 에이전트를 기본으로 두지 않는다. 컴파일과 시험은 사용자가 Unity에서 확인한다.
- 착수 조건: **Q-RV1~Q-RV7 결정.** 아래 Task는 권장안(모두 A) 기준이다. Q-RV1이 B·C면 해당 Phase는 "요청 문서 작성"으로 바뀐다.

## 0. 순서와 독립성

```text
RD-0 착수 확인(F1~F6, 읽기만)
 ├─ RD-1 C1 제공자·조립 (RV1·RV1b)        ← 확정 동작의 전제
 ├─ RD-2 View 호출처 이관 (RV3·RV4·RV5·RV6) ← RD-1과 독립
 ├─ RD-3 DB 검사·로드아웃 (RV7·RV8)          ← 독립
 └─ RD-4 E1 덱 편집 팝업 (RV2)               ← 코드는 독립, 화면 확인은 RD-1 뒤
RD-5 문서·공유·E3 안내 (RV9)
```

- RD-1~RD-4는 서로 다른 파일을 고친다. 겹치는 파일은 없다.
- 한 Phase가 끝나면 멈추고 보고한다(CLAUDE.md). 커밋은 사용자가 한다.

## RD-0 착수 확인 (읽기만, 짧게)

설계 §5의 F1~F6을 코드로 확인하고, 설계와 다르면 구현 전에 묻는다.
- F1: 상점·회복을 마친 노드의 `IsCleared`·`HasSelection` 값. 이것으로 RV1 판단 9단계의 식이 정해진다.
- F2: "등록 안 된 입력" 식을 꺼낼 수 있는지.
- F3·F6: 팝업 Layer와 `UIId`.
- F4·F5: 확정 조립 위치, 카드 그림 부품.

산출: 계획 §7 실행 기록에 결과 표.

## RD-1 C1 편집 창 제공자와 조립 (호진님·민형님 영역, Q-RV1 A일 때)

| Task | 파일 | 내용 |
|---|---|---|
| T1 | `LifeCycles/Stage/StageNodeEntryFlow.cs`·`ShopFlow.cs`·`RecoveryFlow.cs` | "등록 안 된 입력 있음"을 `TryRegister`와 같은 식 하나로 꺼내 읽기 전용 속성으로 공개(`TryRegister`도 그 식을 부름). 회복은 `IsWaiting`만 공개. 동작 변화 없음 |
| T2 | `LifeCycles/Stage/StageFlow.cs` | 읽기 전용 `IsStopped`·`HasPendingWork`·`HasUnregisteredRequest` |
| T3 | 새 `LifeCycles/Stage/IStageDeckEditFacts.cs` (internal) | 창이 읽을 사실만: 바인드·종료·오류·활성 수명·로딩·맵 준비·로스터·Runner·복귀 대기·Flow 셋·전투 요청 상태·노드 상호작용(F1 결과) |
| T4 | 새 `LifeCycles/Stage/StageDeckEditWindow.cs` | `IDeckEditAvailabilityRead` 구현, 설계 RV1 순서표 하나, `Bind`·`Unbind` |
| T5 | `LifeCycles/Stage/StageLifeCycle.cs` | 생성자에 창을 받음(선택 인자, 기존 시험 호환). `IStageDeckEditFacts` 명시 구현, `OnBindServices` 끝에 `Bind`, `OnShutdown`에 `Unbind` |
| T6 | `ServiceKeys/Runtime/LifeCycle/LifeCycleSystemServiceKey.cs` | `DeckEditAvailability` 키 |
| T7 | `Factory/Runtime/LifeCycle/LifeCycleSystemFactory.cs` + 새 `LifeCycleSystemComposition.cs` | `CreateComposition(faults)`: 창을 만들어 `StageLifeCycle`에 넘기고 `AddService`만(AddModule 없음). `Create`는 `.Group` 위임 |
| T8 | `Factory/Runtime/RunTimeSystemFactory.cs`, `Factory/Runtime/InGame/InGameSystemFactory.cs` | LifeCycle 조립을 먼저 만들어 InGame에 넘김. `Compose` → `InGameCoreSystemFactory.Create(..., deckEditAvailability)`. 내보내는 순서·공개 서명 유지 |

- 새 파일 셋은 `.meta`와 함께 둔다.
- 시험: 요청 §2의 제공자 시험 여섯과 §4의 조립 시험은 시험 코드라 이번 범위 밖이다. RD-5 공유 메모에 "남은 시험"으로 적는다.
- 사용자 확인: Unity 컴파일, StageSelect에서 `CoreGroup.Deck.Read().Snapshot.Availability.Reason`이 `AuthorityUnbound`가 아님(RD-4 화면으로 확인 가능).
- 공유: 호진님(Flow 공개 속성·창), 민형님(조립 순서 두 줄).

## RD-2 View 호출처 이관 (현강님·호진님 영역)

| Task | 파일 | 내용 |
|---|---|---|
| T9 | `GlobalSystem/UISystem/Screens/Global/DeckCardList.cs` | `CurrentDeckIds()` 하나(전투 중 `Membership`, 밖 `RunDeckReadSink`), 역할 검색 삭제, `ShowCard(slot, id)`. `BindUIService`에서 `RunDeckReadSink`·`CombatStateReadSink`를 받음 |
| T10 | `.../Global/OwnedCharacterStatsReader.cs` | `deck` 필수, null 검사. Q-RV7 A면 `Tests/.../OwnedStatsUITestServices.cs:146`에 `deck: buffers.RunDeckReadSink` 한 줄(컴파일용, 단정 무변경) |
| T11 | `ViewSystem/Lobby/LobbyDepartureText.cs` | 7·8 문구 추가, `MainAlreadyOwned` 분기 삭제 |
| T12 | `ViewSystem/Combat/CombatDeploymentConfirmation.cs` + 조립하는 곳(F4) | 전투 상태 읽기를 받아 `Membership.MainId`로 판단. 동작 동일 |

- 사용자 확인: Unity 컴파일. 전투 배치에서 덱 카드 순서(메인이 0번), 메인 미배치 경고가 그대로인지.
- 공유: 현강님(T9·T10·T12), 호진님(T11), 천명님(T11 뒤 enum 제거 가능).

## RD-3 DB 검사와 로드아웃 (호진님 영역·콘텐츠)

| Task | 파일 | 내용 |
|---|---|---|
| T13 | `DataBaseSystem/Data/InGame/Table/Loadout/Item/PlayerLoadoutData.cs` | `ValidateInitialDeckIndices()`: 비어 있지 않음·중복 없음·`[0, 1 + Σcount)` 안. `Validate()`에는 넣지 않음(Q-RV5 A) |
| T14 | 로드아웃 표 생성자(`DataBaseSystem/Queries/.../PlayerLoadoutTable` 계열, RD-0에서 위치 확인) | 항목마다 `Validate()` 뒤에 T13 호출 |
| T15 | `PlayerLoadout_1.asset` | Q-RV6 결과대로(의도가 아니면 E2 커밋 값으로 되돌림. 되돌리기는 사용자 확인 뒤) |

- 사용자 확인: `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`(Unity), Play From Boot에서 새 Run 덱.
- 공유: 호진님(T13·T14), 천명님(Core 시험 기대 그대로라는 점).

## RD-4 E1 덱 편집 팝업 (호진님 영역, StageSelect View)

| Task | 파일 | 내용 |
|---|---|---|
| T16 | 새 `ViewSystem/StageMap/Deck/DeckEditDraft.cs` | 순수 초안: 기준 스냅샷·후보·마지막 평가·상태, 설계 RV2 규칙 |
| T17 | 새 `.../Deck/DeckEditText.cs` | 실패·불가 사유 문구 표 |
| T18 | 새 `.../Deck/DeckEditDisplayModel.cs`·`DeckEditPresenter.cs` | 보유 목록·슬롯 넷·메인 표시·병과 수·이유·확정 가능 여부. 카드 내용은 스냅샷 `Stats`로만(Q-RV4 A) |
| T19 | 새 `.../Deck/DeckEditPopupController.cs`·`DeckEditPopupViewScreen.cs` | `PopupViewScope` 수명, `IRunDeckService` 호출 한 곳, 확정 재시도 없음, 닫힌 뒤 결과 무시 |
| T20 | 새 `GlobalSystem/UISystem/Screens/.../DeckEditUI.cs` (MonoBehaviour) | 그리기와 입력 연결만(보유 카드 클릭 = 넣기/빼기, 확정·취소·다시 평가·닫기) |
| T21 | `UIId.cs`(`DeckEdit = 32`), 팝업 입력 Layer(F3), `StageMapViewScope`·`StageMapRoot`(진입 버튼, Q-RV3 A) | 팝업 셋째를 상점·회복과 같은 꼴로 등록 |

- 새 파일은 모두 `.meta`와 함께 둔다.
- **사용자 에디터 작업:** `DeckEditUI` 프리팹(보유 목록 칸·슬롯 넷·버튼 넷·이유 글자), StageSelect 씬에 진입 버튼과 화면 연결.
  - 연결 전에 StageSelect가 멈출지는 F3에서 확인한다. 필수 참조로 둘지 선택 참조로 둘지 그때 정한다.
- 사용자 확인: 설계 RV9 E3 1~3단계.
- 공유: 호진님(StageMap 팝업 셋째·입력 Layer), UISystem 담당(`UIId` 값).

## RD-5 문서·공유·E3 안내 (RV9)

- 공유 메모: 호진님·현강님·민형님에게 각 하나씩, 천명님에게는 채팅 문구. 바꾼 파일, 남은 시험(제공자 여섯·조립·호출처), enum 제거 가능 시점을 적는다.
- Core 데이터 경계 문서 §5에 다른 작업자 파일 변경을 기록한다.
- E2 인계의 능력치 문구 차이는 천명님 공유 문구로 넘긴다(문서는 천명님 소유).
- CLAUDE.md 현재 상태 줄을 갱신한다.
- E3 수동 인수 목록(설계 RV9)을 사용자에게 넘긴다.

## 6. 남는 위험

- 검증을 건너뛰므로 컴파일 오류는 사용자의 Unity 확인에서 처음 드러난다. Phase를 작게 끊은 이유다.
- F1이 설계 가정과 다르면 RV1 판단 9단계가 바뀐다. RD-0 결과로 다시 묻는다.
- 제공자 시험 여섯이 없는 채로 C1이 연결된다. 병합 게이트(L3)의 완료 조건 1·3은 시험 담당이 채워야 한다.

## 7. 실행 기록

### 7.0 결정 (2026-10-20, 사용자)

"Q-RV1~Q-RV7 모두 A로 해. Q-RV6은 내가 따로 답할게. RD-0부터 진행해."
- 다른 작업자 영역은 사용자 지시로 이 세션이 구현하고, 담당마다 공유 메모를 쓴다(CLAUDE.md 명시 지시 예외).
- Q-RV6(작업 트리 로드아웃)은 답을 받기 전까지 T15를 하지 않는다.

### 7.1 RD-0 착수 확인 (2026-10-20, 읽기만, 코드 변경 없음)

| 항목 | 결과 | 설계와 |
|---|---|---|
| F1 노드 상호작용 끝 | Core는 노드에 들어갈 때 `SelectStage`(`ChapterProgressionCoordinator.ConfirmStageEntry`), 끝낼 때 `ResolveStage` → `SetClear(true)`를 부른다. 맵이 바뀌면 `InstallMapSelection`이 선택·상점·회복 기록을 지운다. 상점 팝업도 같은 판정을 쓴다(`ShopPopupController.CanPermit` = 선택 맞음 && `!IsCleared`). → **판정식은 `HasSelection && !IsCleared(CurrentMapKey, CurrentStage.NodeId)`이면 `InteractionOpen`.** 완료된 상점·회복 기록은 노드가 클리어되면 잠금이 아니고, 숨긴 팝업은 노드가 클리어되지 않았으니 잠금이 유지된다(요청 §2 두 조건 충족). 등록되지 않은 노드는 `IsCleared`가 false라서 잠금 쪽으로 안전하게 떨어진다 | 같음 |
| F2 등록 안 된 입력 | 진입: `_input.TryGetNextCommand(_handledNumber(), out _)`. 상점: `_input.TryGetNextCommand(0, out c) && c.Number > _handledNumber()`. 회복: `IRecoveryRequestExecution.CanReserveExecution`(제출됐고 예약 전)과 `RecoveryFlow._waiting`(예약 뒤 Job 대기). 셋 다 `TryRegister`와 같은 식으로 꺼낼 수 있다 | 같음 |
| F3 팝업 입력 Layer·자리 | 상점·회복이 **공통 모달 Layer `StageMapPopupInputLayers.Popup` 하나**와 `PopupInputSession` 하나를 같이 쓴다. `StageMapSceneRoot`의 화면 칸(`shopUI`·`recoveryUI`)은 선택 참조라 비어 있으면 그 팝업을 만들지 않는다. → 덱 팝업도 같은 Layer·세션을 쓰고, 화면 칸을 선택 참조로 두면 씬 연결 전에도 StageSelect가 멈추지 않는다 | **다름:** 설계는 "새 팝업 입력 Layer" → 기존 공통 모달 재사용(Q-RD1) |
| F4 배치 확인 조립 | 운영 조립은 **우리 파일** `CombatViewSystem.cs:224`. 시험 `CombatDeploymentConfirmationTests.cs:33`(현강님)도 같은 생성자를 부른다 → 생성자에 전투 상태 읽기를 더하면 시험이 컴파일되지 않는다(시험 코드 제외 범위와 충돌) | **다름**(Q-RD2) |
| F5 카드 그림 부품 | `UnitCardView`(MonoBehaviour)가 값만 받는 입구를 가진다: `ShowCharacter(key, name, portrait, level)`, `ShowDeckDetails(level, health, maxHealth, defense, attack, attackCost, moveCost, priority)`, `ShowAbilityContent`. 스냅샷 `OwnedCharacterStatsSnapshot`에 `CharacterKey`·`Level`·`Stats`가 있다 → 덱 편집 카드는 `UnitCardView`에 스냅샷 값만 넣는다 | 같음 |
| F6 `UIId` | (RD-0 당시) 29는 비어 있었다 — 70aa3e5 병합 뒤 32로 옮김(12도 비어 있지만 재사용하지 않음). enum은 주로 현강님이 고친 파일이라 공유 대상은 현강님 | 같음 |
| 추가 | T14 자리는 `DataBaseSystem/Queries/InGameDataBase/InGameTable/PlayerLoadoutTable.cs:32`(`data.Validate()` 다음 줄) | — |

**정할 것 (RD-1 착수 전):**
- **Q-RD1** 덱 팝업 입력 Layer
  - **A(권장): 기존 공통 모달 `StageMapPopupInputLayers.Popup`과 같은 `PopupInputSession`을 재사용.** 상점·회복과 동시에 열릴 일이 없다(노드 상호작용 중이면 편집 불가). Layer 이름이 "ShopPopup"인 점만 어색하다.
  - B: 새 Layer.
- **Q-RD2** RV6 배치 확인의 메인 판단
  - **A(권장): 생성자를 그대로 두고, 전투 참가자 목록(`_characters.Characters` — 참가 목록으로만 만들어짐)에서 메인을 찾는다.** `Owned` 검색이 아니고, 시험·조립 변경이 없다.
  - B: 설계대로 전투 상태 읽기를 받는다. 우리 조립 한 줄에 현강님 시험 한 줄 예외(Q-RV7 같은 꼴)가 붙는다.
  - C: RV6을 미룬다.

### 7.2 RD-1 C1 편집 창 제공자·조립 코드 완료 (2026-10-20, 검증 건너뜀 — 사용자 지시)

사용자: "고속모드 유지하고 바로 진행해". 고속 모드는 이 세션의 추가 사용량이 꺼져 있어 켜지지 않았다. Q-RD1·Q-RD2는 권장 A로 진행했다. MSBuild·관리 실행·검토 에이전트는 돌리지 않았다. **컴파일은 확인하지 않았다.**

| Task | 결과 |
|---|---|
| T1 | `StageNodeEntryFlow`·`ShopFlow`: `HasUnregisteredInput` + private `TryGetUnregistered`(`TryRegister`도 이 식을 씀). `RecoveryFlow`: `IsWaiting`·`HasUnregisteredRequest`(`TryRegister`가 이 속성을 씀) |
| T2 | `StageFlow`: `IsStopped`(전체 또는 도메인)·`HasPendingWork`·`HasUnregisteredRequest` |
| T3 | 새 `IStageDeckEditFacts.cs`(+ `.meta` `9de68098…`): 사실 11개 |
| T4 | 새 `StageDeckEditWindow.cs`(+ `.meta` `eed6828a…`): 판단 순서 11단계(공유 메모 표), 결과는 이유별 인스턴스 재사용, `Bind`·`Unbind` |
| T5 | `StageLifeCycle`: 생성자 선택 인자 `deckEditWindow`, 사실 명시 구현, `OnBindServices` 끝 `Bind`, `OnShutdown` 처음 `Unbind`, `_loading` 필드 |
| T6 | `LifeCycleSystemServiceKey.DeckEditAvailability` |
| T7 | `LifeCycleSystemFactory.CreateComposition` + 새 `LifeCycleSystemComposition.cs`(+ `.meta` `6b3ac891…`). `Create`는 `.Group` 위임 |
| T8 | `RunTimeSystemFactory`: LifeCycle을 먼저 만들어 InGame에 창을 넘김(내보내는 순서 그대로). `InGameSystemFactory.CreateComposition`·`Compose`에 선택 인자, `InGameCoreSystemFactory.Create`의 셋째 인자로 넘김 |

**계획과 다른 점:** 판단표가 설계 RV1의 9단계에서 11단계가 됐다.
- 전투 복귀 배출 동안 Flow가 정지 상태라서 "복귀 대기·Runner 바쁨"을 "Flow 정지"보다 앞에 뒀다.
- "종료 뒤"와 "바인드 전"을 따로 나눴다.

**공유(미전달):**
- 호진님: [C1 제공자 메모](../handoffs/2026-10-20-run-deck-c1-provider-hojin-share.md)
- 민형님: Factory 공유 §20

**사용자 확인:**
- Unity 컴파일
- StageSelect에서 `CoreGroup.Deck.Read().Snapshot.Availability.Reason`이 `AuthorityUnbound`가 아니어야 함(화면 확인은 RD-4 뒤)
- 기존 Stage·Lobby·Combat 진입이 그대로인지(조립 순서만 바뀜)

**커밋 묶음:** 바뀐 9개 + 새 파일 셋(`.meta`와 함께) + 문서(설계·계획·공유 둘·CLAUDE.md)

### 7.3 RD-2 View 호출처 이관 코드 완료 (2026-10-20, 검증 건너뜀 — 컴파일 미확인)

| Task | 결과 |
|---|---|
| T9 | `DeckCardList`: `CurrentDeckIds()` 하나(전투 중 `Membership`, 밖 `RunDeckReadSink`, 없으면 모두 비움), 역할 검색 삭제, `ShowCard(slot, id)`, `_characters` → `_deck`·`_combatState`, 클래스 요약 고침 |
| T10 | `OwnedCharacterStatsReader`: 생성자 `(characters, combat, stats, deck, faults = null)`, `deck` 필수, `Main`의 null 검사 제거. 호출처 `MainCharactorUI`·`UnitCardUI`, 시험 `OwnedStatsUITestServices.cs:146` 한 줄(Q-RV7 A) |
| T11 | `LobbyDepartureText`: 7·8 문구 추가, `MainAlreadyOwned` 분기 삭제 |
| T12 | `CombatDeploymentConfirmation.CheckMainPlacement`: 전투 캐릭터 목록에서 메인을 찾음(Q-RD2 A, 생성자·조립 무변경) |

**계획과 다른 점:** T10에서 필수 인자를 선택 인자 앞으로 옮기느라 생성자 인자 순서가 바뀌었다. 그래서 운영 호출처 둘도 같이 바꿨다(계획은 "서명만").

**시험 영향(고치지 않음, 공유 메모에 적음):**
- `DeckButtonUITests` 메인·서브 카드 시험: 덱 미설치라 실패할 것
- `LobbyDepartureViewTests:121`: 옛 문구 기대

**새로 찾은 것 — Q-RD3:** `CombatDeploymentConfirmation.HasUnplacedUnits`가 `Owned` 전체를 돈다.
- 덱 밖 보유 유닛이 있으면 "미배치 유닛" 확인창이 늘 뜰 것이다.
- **A(권장):** 참가 목록 ID를 돌게 고친다. 생성자에 `ICombatStateBufferReadSink`를 더하고, 우리 `CombatViewSystem.cs:224` 한 줄과 현강님 시험 `CombatDeploymentConfirmationTests.cs:33` 한 줄을 같이 고친다.
- B: 이번엔 두고 공유만 한다.

**공유(미전달):**
- 현강님: [호출처 메모](../handoffs/2026-10-20-run-deck-view-callers-hyeongang-share.md)
- 호진님: C1 메모 "추가(RD-2)" 절
- 천명님(채팅 문구): View 쪽 `MainAlreadyOwned` 사용처가 없어졌다. 다만 시험 셋(`CharacterOwnershipLimitTests:90`, `CharacterBufferTestData:58`, `LobbyDepartureViewTests:121`)이 enum 값을 아직 쓴다.

**사용자 확인:**
- Unity 컴파일
- 전투 배치에서 덱 카드 0번이 메인이고 덱 순서대로 나오는지
- 메인 미배치 경고가 그대로인지
- 덱이 3장이면 넷째 칸이 비는지

### 7.4 Q-RD3 수정과 RD-3 DB 검사 코드 완료 (2026-10-20, 검증 건너뜀 — 컴파일 미확인)

사용자: "Q-RD3는 A로 고치고 RD-3 진행해."

**Q-RD3 A — `CombatDeploymentConfirmation`**
- 생성자 둘째 인자로 참가 목록 `Func<RunDeckSnapshot> membership`을 받는다.
  - `ICombatStateBufferReadSink` 대신 이 형태를 고른 이유: 시험 가짜가 전투 상태 전체를 만들 필요가 없다.
- `HasUnplacedUnits`는 `membership.OrderedIds`를 돈다(덱 밖 보유 유닛 제외).
- `CheckMainPlacement`는 `membership.MainId`의 배치를 본다. RD-2 T12의 "전투 캐릭터에서 역할로 찾기"를 이것으로 대신했다(설계 RV6 원안과 같아짐).
- 전투가 없으면(null) 미배치 없음, 메인 미배치로 본다.
- 조립: 우리 `CombatViewSystem.cs:224`에서 `() => _state.Current?.Membership`
- 시험: `CombatDeploymentConfirmationTests.cs:33` 생성자 호출 한 줄에 참가 목록 `[3, 1, 2]`(옛 보유 목록과 같은 ID). 사례 단언은 그대로다.

**RD-3**

| Task | 결과 |
|---|---|
| T13 | `PlayerLoadoutData.ValidateInitialDeckIndices()`: 비어 있으면 거절, 펼친 지급 수 `1 + Σcount` 범위 밖이면 거절, 중복이면 거절. `Validate()`에는 넣지 않음(Q-RV5 A) |
| T14 | `PlayerLoadoutTable` 생성자: `data.Validate()` 다음에 호출 |
| T15 | 보류(Q-RV6 답 대기). 확인: 작업 트리 `PlayerLoadout_1`이 지금 `[0,1,2,3]`(아킴보 + ShotGun·Ballista·Cowboy, 덱 4장)으로 바뀌어 있다. T13 검사는 통과한다 |

**운영 에셋:** 플레이어 로드아웃은 `PlayerLoadout_1` 하나뿐이고 번호가 채워져 있다. DB 생성에는 영향이 없다.

**시험 영향(고치지 않음) — Q-RD4:** `InGameDataTableTests`의 `PlayerLoadout(...)` 도우미(`:447`)가 `initialDeckGrantIndices`를 넣지 않는다.
- SetUp(`:89`)에서 만드는 InGame DB가 이제 "must not be empty"로 실패한다. 그래서 이 묶음의 시험 대부분이 실패할 것이다.
- `:201`·`:222`처럼 다른 이유의 예외를 기대하는 사례도 먼저 이 예외를 받는다.
- **A(권장):** 도우미에 `Set(loadout, "initialDeckGrantIndices", new[] { 0 });` 한 줄. 메인만 덱에 넣어도 모양 검사는 통과한다.
- B: 그대로 두고 시험 정리 때 고친다.
- `ChapterGraphPreparationTests`는 `[0,1,2,3]`(지급 4)이라 통과한다. `CharacterRosterDatabase`·`RunDeckInitializationTests`는 표를 거치지 않아 영향이 없다.

**공유(미전달):**
- 호진님 C1 메모 "추가(RD-3)" 절
- 현강님 호출처 메모의 Q-RD3 절을 "고침"으로 갱신

**사용자 확인:**
- Unity 컴파일
- `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- 전투 배치에서 덱 밖 유닛이 있어도 "미배치" 확인창이 덱 유닛 기준으로만 뜨는지

### 7.5 Q-RD4와 RD-4 E1 덱 편집 팝업 코드 완료 (2026-10-20, 검증 건너뜀 — 컴파일 미확인)

사용자: "Q-RD4는 A(도우미 한 줄)로 하고 RD-4 진행해."

**Q-RD4 A:** `InGameDataTableTests.PlayerLoadout(...)` 도우미에 `initialDeckGrantIndices = [0]` 한 줄.

**RD-4 새 파일** (`.meta`와 함께. 일부는 열려 있던 Unity가 만들었고, 나머지 넷은 직접 만듦)

| Task | 파일 | 내용 |
|---|---|---|
| T16 | `ViewSystem/StageMap/Deck/DeckEditDraft.cs` | 순수 초안 |
| T17 | `.../Deck/DeckEditText.cs` | 사유 → 문구 한 곳. 규칙 수치는 문구에 넣지 않음 |
| T18 | `.../Deck/DeckEditDisplayModel.cs`·`DeckEditModelBuilder.cs` | public 표시 값(카드·덱·상태·병과 수·편집/확정 가능). 능력치는 같은 스냅샷 `Stats`만 씀(Q-RV4 A). 사라진 후보는 빈 정의로 남김 |
| T19 | `.../Deck/DeckEditPopupController.cs`·`DeckEditPopupViewScreen.cs`·`DeckEditPopupViewFactory.cs` | `PopupViewScope` 수명. Core 호출 한 곳. 입력 예외는 오류 경계로 보고 |
| T20 | `GlobalSystem/UISystem/Screens/Stage/DeckEditUI.cs`·`IDeckEditDisplay.cs`·`IDeckEditInput.cs` | 그리기·입력 의도만. 카드는 `UnitCardView`에 값만 넣음. 이름·아이콘·특성은 View DB로 찾음(`UnitCardUI`와 같은 방식) |
| T21 | `UIId.DeckEdit = 32`, `StageMapSceneRoot.deckEditUI`(선택 참조), `StageMapViewScope`(셋째 팝업·공통 모달 Layer·`CanOpenDeckEdit`·Tick·정리), `StageMapViewSystem`(`CoreGroup.Deck`이 있을 때 Factory 생성, Scope 두 생성 경로에 전달) | — |

**`DeckEditDraft` 동작**
- 상태 여섯: NotReady·Unevaluated·Valid·Invalid·Stale·Unavailable
- 열 때: 확정 덱으로 후보를 채운다.
- 넣기·빼기 → 평가 대기 → 바로 `Evaluate`
- 수락되면 후보를 Core 정규 순서로 바꾼다.
- 결과의 `Current` 버전이 기준과 다르면 Stale이고, 옛 버전 거절도 Stale이다. `WrongPhase`는 Unavailable이다.
- 다시 평가: 새 스냅샷을 기준으로 삼되 후보는 그대로 둔다.
- 확정 가능 조건: Valid이고, 편집 가능하고, 바로 앞이 확정이 아님.

**계획과 다른 점**
- 진입 버튼을 StageMap 루트에 따로 두지 않고 `DeckEditUI`의 열기 버튼(Panel 밖)으로 뒀다. 프리팹 하나로 끝나고 Scope는 버튼을 모른다.
- 다른 모달·노드 이동 중에는 열지 않도록 Scope가 `CanOpenDeckEdit`를 넘긴다(열기 버튼이 uGUI라 공통 모달 입력 세션으로 막히지 않음).
- 매 프레임 조회는 없다. 다시 읽는 때는 셋뿐이다: 열 때, 다시 평가, 수락된 확정 직후.

**사용자 에디터 작업 (연결 전에는 팝업을 만들지 않으므로 StageSelect는 그대로 동작)**
1. StageSelect 씬 Canvas 아래에 `DeckEditUI` 루트를 만든다(항상 켜 둠).
   - 열기 버튼(루트 직속)
   - Panel(자식): 닫기·확정·다시 평가 버튼, 덱 칸 `UnitCardView` 4개, 보유 칸 `UnitCardView` 7개 이상
   - 카드 본체마다 `Button`이 필요하다.
   - 상태 TMP, 선택으로 병과 TMP·덱 포함 표지·메인 표지
2. `StageMapSceneRoot.deckEditUI`에 연결한다.

**공유(미전달)**
- 호진님: C1 메모 "추가(RD-4)" 절 — StageMap Scope·System·SceneRoot
- 현강님: 호출처 메모 "추가(RD-4)" 절 — `UIId.DeckEdit`, 새 UI `DeckEditUI`, `UnitCardView`·`UnitCardTraitContent` 재사용

**사용자 확인**
- Unity 컴파일
- 설계 RV9 E3:
  1. StageSelect에서 열기 → 덱 세 장(지금 작업 트리는 네 장)
  2. 카드 넣고 빼기 → 이유 문구
  3. 확정 → 닫고 다시 열기
  4. 상점 노드에 들어간 뒤 "지금 노드를 마친 뒤…"(C1)

### 7.6 RD-4 보충 — 덱 편집 UI 생성 도구 (2026-10-20, 검증 건너뜀 — 실행·컴파일 미확인)

사용자: "RD-5 진행 전에 너가 UI 까지도 만들어줄래". Unity 에디터 조작은 세션이 직접 할 수 없다. 그래서 기존 상점 UI 도구(`Editor/Stage/StageShopUISetup.cs`)와 같은 꼴의 에디터 도구를 만들었다. 사용자가 메뉴를 한 번 누르면 된다.

- **새 파일:** `Assets/ProjectL/Editor/Stage/StageDeckEditUISetup.cs`(+ `.meta`)
- **메뉴:** `ProjectL > Stage > Ensure StageSelect Deck Edit UI`
- **동작**
  - 실행 조건: Play 중이 아니고, 열린 씬에 저장 안 된 변경이 없어야 한다.
  - 프리팹 `Prefabs/UI/Deck/DeckEditUI.prefab`이 없으면 만든다.
    - 자체 Canvas(Overlay, 정렬 100 — 상점 110보다 아래), 1920×1080 기준 크기 조정
    - 우상단 "덱 편집" 열기 버튼
    - Panel: 제목, 덱 칸 4개(`UnitCard.prefab` 중첩, 0.6배), 보유 칸 7개(0.45배), 칸마다 "덱에 있음"·"메인" 표지, 상태·병과 글자, 다시 평가·확정·닫기 버튼
    - 글꼴은 함렡 Extra Bold(없으면 Hahmlet Bold)
    - `DeckEditUI` 칸을 모두 연결한다.
  - StageSelect 씬을 열고, `DeckEditUI`가 없으면 프리팹 인스턴스를 놓고, `StageMapSceneRoot.deckEditUI`가 비어 있으면 연결한 뒤 씬을 저장한다.
  - 이미 있는 프리팹·인스턴스·참조는 그대로 둔다. 다른 오브젝트를 가리키고 있으면 멈춘다. 다시 만들려면 프리팹을 지우고 다시 실행한다.
- **카드:** `UnitCard.prefab`은 `UnitCardView` + 본체 `Button`만 있고 `UnitCardUI`는 없다. 그래서 UISystem에 따로 등록되지 않는다.
- **커밋 묶음:** 도구, 생성된 프리팹(+ 폴더 `.meta`), `StageSelect.unity`를 RD-4 코드와 함께 묶는다.

**사용자 확인:** 메뉴 실행 → 콘솔에 "[StageDeckEditUISetup] … 추가했습니다." → Play From Boot → StageSelect 우상단 "덱 편집" → 설계 RV9 E3.

**첫 실행 결함과 고침(2026-10-20, 사용자 보고):**
- **증상:** `MissingReferenceException: … DeckEditUI has been destroyed`
- **원인:** 도구가 글꼴·카드·프리팹을 먼저 불러온 뒤 `EditorSceneManager.OpenScene(…, Single)`을 불렀다. 씬을 Single로 열면 Unity가 쓰이지 않는 에셋을 내리면서 불러 둔 참조가 파괴된다.
- **고침:** 상점 도구처럼 씬을 먼저 열고 그 뒤에 에셋을 불러오게 순서를 바꿨다. 글꼴 대체 검사는 `??` 대신 Unity null 검사로 바꿨다.
- **첫 실행이 남긴 것:** `Prefabs/UI/Deck/DeckEditUI.prefab`은 저장됐다. `DeckEditUI` 참조는 모두 연결돼 있다(덱 4·보유 7·표지 7+7·버튼 4·글자 2). 씬은 바뀌지 않았다. 다시 실행하면 이 프리팹을 그대로 쓰고 씬 배치·연결만 한다.

### 7.7 `70aa3e5` 병합 (2026-10-20, 사용자 지시 "70aa3e5랑 병합 먼저 진행해줘", 커밋 없음)

- **병합 방식:** `70aa3e5`(호진님 StageSelect ← 천명님 core-system 병합)는 HEAD `9a5cd0f6`의 자손이라 **빨리 감기**로 끝났다(병합 커밋 없음, 30커밋·428파일). 커밋 안 된 RD-1~RD-4 변경은 `git stash` → `merge --ff-only` → `stash pop`으로 다시 얹었고, `git reset`으로 스테이지는 비웠다(전과 같이 작업 트리 변경만).
- **자동으로 합쳐짐:** `MainCharactorUI`·`UnitCardUI`(상대: HP 영역 접근자·배치 표시, 우리: `deck` 인자 순서), `CombatViewSystem`(상대 +40줄, 우리 배치 확인 참가 목록 한 줄), `CLAUDE.md`.
- **충돌 셋과 처리**
  - `UIIDs.cs`: 상대가 29~31(`CombatCategoryInfo`·`CombatCostInfo`·`Tutorial`)을 먼저 썼다 → **`DeckEdit = 32`**. 문서의 29 표기도 32로 고쳤다.
  - `StageSelect.unity`: 덱 UI 도구가 넣은 인스턴스·연결과 상대 변경이 겹쳤다. 씬 YAML 자동 병합은 위험하므로(ShopUI fileID 중복 선례) **상대 판을 받았다.** 덱 UI 도구를 다시 실행해야 한다. 프리팹은 이미 있으므로 배치·연결만 한다.
  - `함렡_Extra Bold SDF.asset`: Unity가 동적 글리프를 넣은 것이라 상대 판을 받았다. 쓰면 Unity가 다시 채운다.
- **새 호출처 대조:** 바꾼 서명(`OwnedCharacterStatsReader`·`CombatDeploymentConfirmation`·`InGameCoreSystemFactory.Create`)의 호출처가 병합 뒤에도 모두 맞는다. 로드아웃 SO를 만드는 시험은 둘 그대로다(`InGameDataTableTests`·`ChapterGraphPreparationTests`).
- **남긴 것:** `stash@{0}` "run-deck RD-1~RD-4 before merging 70aa3e5"(옛 StageSelect·글꼴 로컬 변경 포함, 필요 없으면 사용자가 지움).
- 검증은 건너뜀(사용자 지시) — Unity 컴파일·씬 확인 대기.

### 7.8 RD-5 마무리 — 남은 일 한 곳 (2026-10-20)

사용자: "메뉴 다시 실행했고 컴파일도 문제 없어. RD-5 진행해." RD-1~RD-4는 Unity 컴파일 문제 없음(사용자). Unity Test Runner·재생(E3)은 아직이다.

**A. 사용자 확인·작업**
1. **StageSelect 씬 배치 확인:** RD-5 시점 디스크의 `StageSelect.unity`에는 `DeckEditUI` 인스턴스와 `deckEditUI` 연결이 없다(병합 때 상대 판을 받은 그대로). 도구가 "기존 화면·참조를 유지했습니다"라고 냈다면 Unity가 병합 전 씬을 메모리에 들고 있는 것이다. 그 씬을 저장하면 상대(`70aa3e5`)의 씬 변경을 덮어쓰니, **저장하지 말고 디스크에서 다시 연 뒤 도구를 다시 실행한다.**
2. **E3 수동 인수**(C1·E1)
   1. Play From Boot → 로비 → 새 Run → StageSelect
   2. 우상단 "덱 편집" → 확정 덱이 보인다(작업 트리 로드아웃이면 4장)
   3. 보유 카드 넣기·빼기 → 이유 문구(빈 덱·메인 없음·수 초과)
   4. 확정 → "덱을 확정했습니다." → 닫고 다시 열면 같은 순서
   5. 상점 노드에 들어가면 열기 버튼이 막히거나, 열려 있으면 "지금 노드를 마친 뒤…"(C1 `InteractionOpen`)
   6. 상점을 마치고 나오면 다시 편집할 수 있다(완료된 기록은 잠금 아님)
   7. 전투 진입 → 배치 화면 덱 카드가 참가 목록 순서(0번 메인)
   8. 덱 밖 유닛이 있어도 "미배치" 확인창이 덱 유닛 기준으로만 뜬다
3. **Q-RV6:** 작업 트리 `PlayerLoadout_1`(아킴보 + ShotGun·Ballista·Cowboy, `[0,1,2,3]`)이 의도인지 → T15
4. **커밋 묶음** — 새 파일은 `.meta`와 함께
   - 코드: 스테이지되지 않은 변경 전체(`git status`)
   - 새 파일: RD-1의 셋, RD-4의 열(`StageMap/Deck/` 일곱 + `Screens/Stage/` 셋), 도구 하나, 프리팹 `Prefabs/UI/Deck/`(+ 폴더 `.meta`)
   - 씬: `StageSelect.unity`(A-1 뒤)
   - 문서: 설계·계획·공유 둘·Factory 공유 §20·CLAUDE.md

**B. 남긴 시험 (사용자 지시로 범위 밖, 담당 시험 작성자)**

| 시험 | 이유 | 담당 |
|---|---|---|
| C1 제공자 시험 여섯(요청 §2) + 조립 시험 하나(요청 §4) | 새 제공자 | Flow(호진님) 또는 천명님 |
| `DeckButtonUITests` 메인·서브 카드 | 덱을 설치하지 않아 카드가 안 보임 | 현강님 |
| `LobbyDepartureViewTests:121` | 옛 `MainAlreadyOwned` 문구 기대 | 호진님 |
| `InitializeRoster` 시험 → `StartNewRun` 이전: `CombatEntryIntegrationTests`·`CombatProgressIntegrationTests`·`CombatSceneEventPresenterTests`·`TerrainOperationalDisplayTests` | 인계 §6 | 용근(우리) |
| 같은 이전: `ArtifactHealingChaliceIntegrationTests`·`HojinCombatTornadoTerrainAssetTests` | 인계 §6 | 호진님 |
| `DataBaseAssetTests` E2 권장 셋(실제 카탈로그 DB 생성·로드아웃 초기 덱 `None`·PlayerOnly 메인 하나) | E2 §3 | DB(호진님) |
| `DataBaseAssetTests.MixedShopProduction_…` | E2 §6, 같이 정할 일 | 천명님·호진님 |

**C. 개발 코드 (사용자 지시로 범위 밖)**
- `DevBattleInitializationSource`가 아직 key 14를 `PlayerOnly` 메인으로 정의한다 → Battle Definition = Fixture로 전투를 바로 시작하면 `UnitDefinition` 생성에서 예외가 날 것이다(코드 대조, 미실행). 아킴보를 메인으로 옮기고 덱 유닛(Cactus Cannon 등)도 넣어야 한다. 담당은 용근(우리).

**D. 공유 (모두 미전달)**
- 호진님: [C1 제공자 메모](../handoffs/2026-10-20-run-deck-c1-provider-hojin-share.md)(C1·출발 문구·DB 검사·StageMap 팝업·씬 도구)
- 현강님: [호출처 메모](../handoffs/2026-10-20-run-deck-view-callers-hyeongang-share.md)(`DeckCardList`·`OwnedCharacterStatsReader`·배치 확인·`DeckEditUI`·`UIId.DeckEdit = 32`·프리팹)
- 민형님: Factory 공유 §20(C1 조립 순서)
- 천명님: 채팅 문구(아래 E)

**E. 천명님 쪽 후속 (채팅 문구로 전달)**
- C1이 연결됐다. `Availability.Reason`이 이제 `AuthorityUnbound`가 아니다.
- View 쪽 `MainAlreadyOwned` 사용처가 없어졌다. 다만 시험 셋이 enum 값을 아직 쓴다.
- 한 인자 `PrepareBattle`을 부르는 우리 시험 둘은 참가 목록을 넘긴다.
- E2 §2 "옛 메인 능력치를 복사하지 않는다"가 사용자 결정(그대로 옮김)과 다르다.
- `PlayerLoadoutData.ValidateInitialDeckIndices()`는 `Validate()`와 따로 두고 표 생성 때만 부른다.

**F. 낮음**
- 공통 모달 Layer 이름 "ShopPopup"(호진님 판단)
- `DeckEditUI` 배치·색은 임시(현강님)
- 열기 버튼 위치(우상단)
- `DeckCardList`의 매 프레임 `Update`(기존)
