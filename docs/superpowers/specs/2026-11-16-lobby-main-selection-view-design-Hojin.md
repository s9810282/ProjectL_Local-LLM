# 로비 메인 선택 View 연결 설계

날짜: 2026-11-16
상태: 설계 3판(Codex 검수 두 번 반영, §11). **구현 완료(LM-a~LM-c, Unity 확인 Ua·Ub·Uc 문제 없음 — 사용자, 구현 메모 §12).** **결정 완료(2026-11-16 사용자 "권장안대로 계획" = Q-LM1~Q-LM5 모두 A).** Task 계획: [lobby-main-selection-view-plan](../plans/2026-11-16-lobby-main-selection-view-plan-Hojin.md).

받은 문서: [Core → View 인계](../handoffs/2026-11-15-run-start-main-selection-core-view-handoff.md)(천명님, Core Phase A~C 완료).
이 설계는 그 문서 §3.1이 요구한 "담당 지정·주입·수명 맵"과 §3 표의 View·LifeCycle 몫을 다룬다.

## 1. 요구와 범위

- 로비 덱 팝업에서 다음 Run의 메인(지금 후보: 아킴보 10 "에블린", 스나이퍼 12 "로즈")을 고른다.
- 출발하면 고른 메인으로 새 Run이 시작된다. 서브·시작 골드·손패 수·초기 덱은 로드아웃 그대로다.
- 로드아웃의 메인은 지우지 않는다. **처음 고를 값(기본 선택)**으로만 쓴다(2026-11-16 대화 결정).
- 로비 출발은 늘 메인을 지정해서 시작한다. 키 없는 시작은 Stage 첫 진입·개발 진입처럼 선택 화면이 없는 곳에만 남는다.
- 범위 밖: 지금 Run의 메인 교체, StageSelect 덱 편집, 로비 판 위 캐릭터 모습 바꾸기(Q-LM5), 저장·재시작 뒤 기억.

## 2. 지금 코드에서 확인한 사실

| 자리 | 사실 |
|---|---|
| Core | `IRunStartService.ReadMainOptions(loadoutKey)`·`StartNewRun(loadoutKey)`·`StartNewRun(loadoutKey, mainKey)`가 있다(`RunStartService.cs`). 후보 = 유닛 표의 `Role == MainCharacter`, 기본값 = 로드아웃 메인 |
| Core 접근 | View·LifeCycle은 `service.CoreGroup.RunStart`로 닿는다(`LobbyPlan.cs:79`) |
| 덱 팝업 | `LobbyDeckPopupController.Show`가 빈 목록을 넣는다(`Array.Empty`, "운영 후보 계약 전"). 클릭 = 상세 고정만 한다(`LobbyDeckPresenter.Click`) |
| 팝업 수명 | Presenter는 팝업을 닫으면 `Clear`로 모델·고정을 비운다. 그래서 선택을 여기에 두면 닫을 때 사라진다 |
| 연결 수명 | `LobbyViewSystem`이 Lobby 씬 EnterLoaded(100)에서 `AttachFromScene` → `LobbyViewScope`를 만들고, ExitLoaded(−100)에서 `Detach`한다. 보통은 로비 씬 방문마다 한 번이다. 다만 로딩 시스템이 **지금과 같은 씬 번호**를 받으면 씬을 바꾸지 않고 Exit → Enter 콜백을 다시 돌린다(`LodingSystem.BeginSceneLoad`). 이때 연결이 떼였다 다시 붙는다. LifeCycle의 같은 키 전환은 무시되므로(`LifeCycleManager.Change`) 일반 흐름에서 이 경로를 여는 곳은 찾지 못했다(실제 발생 여부 미확인) |
| 조립 순서 | `AttachFromScene`이 루트 검사·`FindPopups`(팝업 Factory 목록)를 **이전 연결을 떼기 전에** 하고, 그 뒤 `Attach` → `LobbySceneFactory.Create` → `LobbyViewScope` 생성자가 `LobbyDepartureFlow`를 만든다 |
| 출발 | `LobbyDepartureFlow.Submit` → `ILobbyViewBufferWriteSink.Submit()`(진행 중 요청이 있으면 그것을 돌려줌) → `LobbyPlan.Decide`가 `Depart` → `RunStartJob`이 `StartNewRun(DefaultLoadoutKey)` |
| 로드아웃 키 | 원본은 `RunStartJob.DefaultLoadoutKey = 1` 한 곳이다. Stage 첫 진입(`StagePlan.cs:107`)도 같은 Job을 쓴다 |
| 겹침 막기 | 출발 처리 중(`IsHolding`)이면 시설 경로로는 팝업을 못 연다(`LobbyViewScope.IsInteractionBlocked`). 팝업이 열려 있으면 출발 시설도 못 연다. **UI 서비스로 바깥에서 연 화면**은 `PopupViewScope.Tick`이 인수하려 하지만, 인수 때 입력 수명을 잡는 `PopupInputSession.Begin`이 출발 붙잡음(`CanWork && !IsHolding`, `LobbyViewScope.cs:70`)을 보고 `null`을 돌려주면 `PopupViewScope.BeginInput`이 `AbortOpen`으로 화면을 닫는다. 그래서 처리 중에는 팝업이 잠깐 보일 수는 있어도 입력을 받는 상태로 남지 않는다(바깥 개방 재현은 미확인) |
| 이름·아이콘 | View DB `UnitVisual` 표에 `DisplayName`·`InfoIcon`이 있다. 아킴보·스나이퍼 모두 이름·`infoIcon`이 채워져 있다 |
| 시험 | `Assets/Tests`에는 C# 파일 563개가 있다(Core `RunStartMainSelectionTests` 포함). 이번에 바꿀 계약(`LobbyViewBuffer`·`LobbyDepartureFlow`·`LobbyDeckPresenter`·`LobbyDeckPopup*`·`ILobbySceneFactory`·`LobbyViewServices`·`RunStartJob` 생성자)을 직접 부르는 시험은 검색에서 찾지 못했다 |
| UI 등록 | 덱 화면 `LobbyDeckUI`는 `UIId.LobbyDeck`, 부모 없음, `Exclusive` 정책의 `UIBase`다. 로비 씬 진입의 `UISystem.BindScene`으로 등록·바인딩되고 `PopupViewScope`가 UI 서비스로 연다. 새 버튼·선택 표시는 화면 안 위젯이라 따로 등록하지 않는다 |
| 작성자 | 고칠 파일은 대부분 호진 줄이다. `LobbyDeckUI`·`LobbyViewSystem`·`LobbyPlan`·`LobbyDeckPopupController`에 Twinsgm·JMH 줄이 몇 줄 있다(구현 때 바꾼 줄만 다시 보고 메모) |

## 3. 원본 표 (SSOT)

| 사실 | 원본 | 읽는 쪽 |
|---|---|---|
| 고를 수 있는 메인·기본 메인 | Core `ReadMainOptions` | 선택 객체가 연결 시작 때 한 번 |
| 이름·아이콘 | View DB `UnitVisual` | 덱 팝업 Factory가 연결 시작 때 한 번 카드 내용으로 |
| 로드아웃 키 | `RunStartJob.DefaultLoadoutKey` | 후보 조회(View 조립 한 줄)·시작(Job) |
| 아직 제출하지 않은 선택 | 새 `LobbyMainSelection` 하나 | 덱 팝업(표시·변경)·출발(읽기) |
| 제출한 선택 | `LobbyDepartureRequest.MainCharacterKey`(불변) | `LobbyPlan` → `RunStartJob` |
| 실제로 설치된 메인 | 로스터 Buffer | 출발 뒤 화면들(선택 객체는 근거가 아님) |

## 4. 설계

### LM1 선택 원본 `LobbyMainSelection` (새 클래스, `ViewSystem/Lobby/`)

- 순수 C#, 상태 셋: `LoadoutKey`, `Candidates`(불변 목록), `Selected`.
- 생성자가 `RunStartMainOptions`를 받는다. 후보가 비었거나 기본값이 후보에 없으면 예외다(설정 오류 → 오류 경계).
- 처음 `Selected` = `DefaultMainCharacterKey`. 후보 첫 번째를 기본으로 보지 않는다.
- `Select(EntityKey key)`: 후보 밖이면 `ArgumentException`, 출발이 붙잡고 있으면(`IsLocked`) `InvalidOperationException`. 같은 값이면 아무것도 안 하고 false.
- `IsLocked` = 바인딩한 `ILobbyDepartureHold.IsHolding`. 처리 중에는 팝업이 입력을 받는 상태로 남지 않으므로(§2 겹침 막기) 이 잠김은 화면 표시용이 아니라 **방어용**이다. 화면은 늘 `Selected`를 표시한다.
- 읽기 계약 `ILobbyMainSelectionReader`(`Selected`)와 쓰기 계약 `ILobbyMainSelectionWriter`(`Candidates`·`Selected`·`IsLocked`·`Select`)로 나눈다. 출발은 읽기만, 덱 팝업은 쓰기까지 받는다.
- 정리할 자원이 없어 `IDisposable`이 아니다.
- **로비 세션의 정의**: Lobby 씬 Enter 콜백(연결 시작)부터 Exit 콜백(연결 해제)까지다. 같은 씬 번호를 다시 불러 Exit → Enter가 다시 돌면 Exit 콜백이 이미 돈 것이므로 **새 세션**으로 본다(선택은 기본값으로). 이 경로에서 선택을 이어 가야 한다면 소유자를 System으로 옮겨야 하며, 그 경우는 Q-LM1 B로 다룬다.

### LM2 주입 경로

```text
LobbyViewSystem.PostBindService            (_view를 비우기 전)
  └ new LobbyViewServices(...,
        readMainOptions: () => inGame.CoreGroup.RunStart.ReadMainOptions(RunStartJob.DefaultLoadoutKey),
        unitVisuals:     view.UnitVisualTableQuery.GetUnitVisual)
      → 프로퍼티 Services.ReadMainOptions (Func<RunStartMainOptions>)
      → 프로퍼티 Services.Deck = new LobbyDeckServices(ui, unitVisuals)   (덱 팝업 묶음)
LobbyViewSystem.AttachFromScene            (지금 순서 유지: 루트 검사 → 팝업 찾기 → 이전 연결 떼기)
  ├ root.Validate()
  ├ selection = new LobbyMainSelection(Services.ReadMainOptions())      ← 새로, 이전 연결을 떼기 전
  ├ _sceneFactory.FindPopups(root, Services, selection, faults)
  │    └ new LobbyDeckPopupFactory(Services.Deck, root.UI.DeckScreen, selection, faults)
  │         └ 생성자에서 cards = LobbyMainCards.Build(selection.Candidates, unitVisuals)   ← 이름·아이콘 검사, 이전 연결을 떼기 전
  │         └ Create(host, input, ...)                                                      (Scope.Start 안)
  │              ├ presenter  = new LobbyDeckPresenter(screen, selection, cards, close)
  │              └ controller = new LobbyDeckPopupController(ui, screen, presenter, input, host, faults)
  └ Attach(root, popups, selection, output)
       └ _sceneFactory.Create(root, output, Services, popups, selection, faults)
            └ LobbyViewScope(..., selection)
                 ├ _departure = new LobbyDepartureFlow(departure, text, selection, canWork)
                 └ selection.BindDepartureHold(_departure)      ← 생성자에서 한 번, 팝업 생성(Start)보다 먼저
```

- 출발이 붙잡고 있는지의 원본은 `LobbyDepartureFlow.IsHolding` 하나다. 이 판정은 연결이 소유한 요청(`_owned != null && !_owned.CanResubmit`)까지 보므로 Buffer 창구만으로는 같은 답을 낼 수 없다. 그래서 Flow가 좁은 읽기 계약 `ILobbyDepartureHold`(`IsHolding` 하나)를 구현하고, Scope 생성자가 `selection.BindDepartureHold(_departure)`로 한 번 넘긴다. 두 번 부르거나 바인딩 전에 선택을 바꾸면 `InvalidOperationException`이다.
- 덱 팝업 컨트롤러·Presenter는 Scope의 `Start` 안(`_popups.Create` → Factory `Create`)에서 만들어지므로 그때는 바인딩이 끝나 있다(Codex 재검수에서 코드 순서 확인).
- 카드 내용(이름·아이콘)은 새 정적 함수 `LobbyMainCards.Build`가 덱 팝업 Factory 생성자에서 한 번 만든다. `FindPopups`는 이전 연결을 떼기 전에 돌므로 외형 행·아이콘이 없으면 불완전한 새 연결로 바꿔 끼우지 않고 예외를 던진다(오류 경계에 닿으면 기존 연결도 정리된다). 결과는 불변 `LobbyDeckCardContent` 목록이고, 팝업을 열 때마다 새로 만들지 않는다.

- Core 후보 조회는 대리자 하나로 넘긴다. View가 `IRunStartService` 전체(시작 명령 포함)를 받지 않게 한다. 같은 꼴의 선례는 `artifactVisuals.GetArtifactView` 대리자다.
- 로드아웃 키는 View 조립 한 줄에서 `RunStartJob.DefaultLoadoutKey`를 읽는다. 팝업에 두 번째 `1`을 적지 않는다. 받은 `options.LoadoutKey`가 다르면 예외다.
- 조회 시점은 씬 연결 시작(Enter 100)이다. 이때는 Core DB·서비스 연결이 끝나 있다. `PostBindService`에서 부르지 않는 이유는 다른 시스템의 바인딩 순서에 기대지 않기 위해서다.
- 조회 실패(설정 오류)는 이전 연결을 떼기 전에 터지므로, 불완전한 새 연결로 바꿔 끼우지 않는다(지금 루트 검사와 같은 규칙).
- 새 전역 상태·이벤트 사슬은 없다. 모두 생성자 인자다.

### LM3 덱 팝업 표시·고르기

- 목록 = `selection.Candidates` 순서의 카드 내용(`LobbyMainCards`, LM2). 이름·아이콘은 `UnitVisual`의 `DisplayName`·`InfoIcon`. 외형 행이 없으면 `GetUnitVisual`이 `KeyNotFoundException`을, 아이콘이 없으면 후보 키를 담은 `InvalidOperationException`을 연결 시작 때 던진다(Q-LM4 A).
- 클릭 = 상세 고정(지금 규칙, `Assets/AGENTS.md`의 Deck 고정 규칙 그대로). 클릭만으로는 선택이 바뀌지 않는다.
- 고른 메인은 목록 항목에 표시(`LobbyDeckListItemView`의 새 칸 `selectedMark`)한다. 상세 고정과 따로 보인다.
- 팝업에 "다음 Run 메인으로 선택" 버튼(`LobbyDeckUI`의 새 칸 `selectButton`, 슬롯 `SelectSlot = -2`)을 둔다. 버튼은 출발하지 않고 선택만 바꾼다.
- 버튼을 누를 수 있는 조건 = 입력 가능 ∧ 고정한 카드 있음 ∧ 그 카드 ≠ `Selected` ∧ `!IsLocked`. 이 판정은 Presenter 한 곳(`CanSelect`)에 둔다. 버튼 켜짐을 그릴 때와 **선택 클릭을 받은 순간** 둘 다 이 판정을 다시 부른다. 통과하면 `selection.Select(고정한 키)`를 부르고 선택 표시·버튼 상태를 다시 그린다.
- 처리 중에는 팝업이 입력을 받는 상태로 남지 않으므로(§2 겹침 막기) 잠김을 Tick마다 보지 않는다. 처리 중 팝업을 열어 두게 하려면 입력 허용 정책을 바꾸는 별도 설계가 필요하다(이번 범위 밖).
- 화면 계약 `ILobbyDeckDisplay`에 `ShowSelected(int slot)`·`SetSelectEnabled(bool)` 둘을 더한다. UI 층(GlobalSystem UISystem)은 View 선택 객체를 모르고 칸 번호만 받는다.
- `LobbyDeckUI`에서 바꿀 곳: 버튼 바인딩(`EnsureCloseBinding` 옆에 `EnsureSelectBinding`, `UIInputBinding.Attach(selectButton, this, SelectSlot)`), `IUIInputReceiver.Click`이 `SelectSlot`도 수신자에 넘김, `ClearControlInput`이 선택 버튼 호버 해제, `RefreshInputStates`가 선택 버튼의 켜짐(`interactable` = 수신 가능 ∧ `SetSelectEnabled` 값), `Validate`가 두 새 칸 검사.
- `LobbyDeckListItemView`: 새 칸 `selectedMark`(GameObject), `SetSelected(bool)`, `Validate` 검사.
- `LobbyDeckPresenter`: `Click`에 `SelectSlot` 갈래, `Clear`·`SetInputEnabled(false)`·고정 해제 때 `SetSelectEnabled(false)`. 모델이 바뀌면 `ShowSelected(Selected의 칸)`. `Selected`는 늘 후보 안이므로 칸이 없는 경우는 없다.
- 팝업을 닫으면 Presenter는 지금처럼 `Clear`한다. 선택은 `LobbyMainSelection`에 남는다. 다시 열면 `Show`가 선택을 읽어 표시한다.
- 버튼은 `UIInputBinding`으로 붙인다(`onClick` 금지 규약).

### LM4 출발 요청에 선택 싣기

- `ILobbyViewBufferWriteSink`에 `Submit(EntityKey mainCharacterKey)` 오버로드를 더한다. 기존 `Submit()`은 "지정 없음" 그대로 남는다.
- `LobbyDepartureRequest`에 `EntityKey? MainCharacterKey`(생성자에서 한 번, 이후 불변)를 더한다. 읽는 쪽은 실행 계층(`LobbyPlan`)뿐이다. 처리 중 요청의 키를 화면에 보일 곳이 없으므로(§2 겹침 막기) 읽기 계약 `ILobbyDepartureState`에는 더하지 않는다(쓰는 곳 없는 계약을 만들지 않음).
- 키의 뜻: `null` = 지정 없음(기본 메인으로 시작). 값 = 그 메인. 명시한 `EntityKey.None`도 그대로 싣는다(기본값으로 바꾸지 않음 — Core가 `InvalidDefinition`으로 거절, 화면에는 그런 키를 만들 길이 없다). 로비 출발은 늘 값을 싣고, `null`은 `Submit()`을 부르는 다른 경로에만 남는다.
- 진행 중 요청이 있으면 지금처럼 그 요청을 돌려준다. 새 키로 덮어쓰지 않는다.
- 단계 전이 표·질문은 바꾸지 않는다.
- 스냅샷 자리: `LobbyDepartureFlow.Submit`이 `_write.Submit(_selection.Selected)`를 부르는 한 줄이다. 돌려받은 요청이 진행 중인 다른 요청이어도 그 요청의 원래 키로 출발한다(Buffer 중복 반환 계약 그대로).
- `LobbyDepartureFlow`가 `ILobbyDepartureHold`를 구현한다: `IsHolding` 하나(지금 판정 그대로).

### LM5 Plan·Job 전달

- `RunStartJob` 생성자에 `EntityKey? mainCharacterKey`를 더한다. 값이 있으면 `StartNewRun(DefaultLoadoutKey, key)`, 없으면 지금처럼 `StartNewRun(DefaultLoadoutKey)`.
- `LobbyPlan`은 `request.MainCharacterKey`를 넘긴다. Job은 실행 때 화면의 선택을 다시 읽지 않는다.
- `StagePlan`의 첫 진입은 `null`을 넘긴다(동작 같음). 개발 진입·임시 Core 조립은 Job을 쓰지 않아 그대로다.
- 성공 결과 확인(`RequireInstalledRoster`)에 "지정한 키가 있으면 설치된 메인의 키가 그 키인지"를 더한다(Q-LM3).

## 5. 수명 표 (§3.1 "Lifetime and cleanup")

| 상황 | 선택(`LobbyMainSelection`) | 출발 요청 |
|---|---|---|
| 팝업 닫기·다시 열기 | 그대로 남음(연결이 가짐) | 영향 없음 |
| 출발 제출 | 그대로 남음 | 제출 순간의 키를 불변으로 담음 |
| 처리 중(Pending)·성공 뒤 전환 중·오류로 끝난 소유 요청 | 잠김(`IsLocked`). 시설 경로로는 팝업이 안 열리고, 바깥 경로로 열려도 입력 수명을 못 잡아 닫힌다(§2). 선택은 바뀌지 않음 | 원래 키 유지 |
| 다른 키로 중복 제출 | — | 진행 중 요청을 그대로 돌려받음(참조·번호·원래 키 유지, Buffer 계약). 로비에서는 처리 중 출발 입력이 막혀 이 경로에 닿지 않음 |
| 정상 거절 | 잠김 풀림, 선택 그대로 → 다른 메인을 골라 다시 출발 가능 | 거절로 끝남, 다음 제출은 새 번호·새 키 |
| 실행 전 취소 — 연결 폐기(씬 이탈·오류 정리의 입력 정리) | 연결과 함께 버려짐 | `Cancelled`(Job은 Core를 부르지 않음) |
| 실행 전 취소 — 연결 유지(제출 직후 연결이 끝난 것을 알아챈 `Submit`의 자기 취소) | 연결이 끝난 상태라 쓰이지 않음 | `Cancelled`. 같은 연결에서 다시 제출할 일은 없음 |
| 후보 조회 실패(Core 설정 오류) | 선택 객체가 만들어지지 않음(`AttachFromScene`, 이전 연결을 떼기 전) | 요청 없음. 오류 경계가 런타임을 정지 |
| 외형·아이콘 조회 실패 | 선택 객체는 만들어졌지만 덱 팝업 Factory 생성(`FindPopups`, 이전 연결을 떼기 전)에서 멈춰 새 연결은 만들어지지 않음 | 요청 없음. 오류 경계가 런타임을 정지 |
| Job 실행 중 예외 | — | `Faulted`, 오류 경계가 정지. 정상 거절과 달리 다시 출발하지 않음 |
| 이미 끝난 요청(이전 방문의 `Completed` 등) | 영향 없음(`IsHolding`은 소유 요청과 진행 중 요청만 봄) | 그대로 |
| 로비 씬을 떠났다 다시 들어옴(Run 끝 복귀 포함) | 새 연결 = 새로 조회 → 기본 메인 | 이전 요청과 무관 |
| 같은 씬 번호 재로딩(Exit → Enter 재실행) | 새 세션으로 봄 → 새로 조회, 기본 메인(LM1 정의) | 이전 연결 정리에서 실행 전 요청은 취소 |

세션 = Lobby 씬 Enter~Exit 콜백 사이 = 연결 하나다. 인계 문서가 요구한 "새 로비 세션은 기본값, 팝업 재개방은 유지"를 따로 장치 없이 만족하고, 선택 원본은 늘 하나다.

## 6. 변경 독립성 점검 (§3.1 세 가지 확인)

| 바꾸는 것 | 고칠 곳 | 읽어야 할 곳 | 확인할 곳 |
|---|---|---|---|
| 팝업 모습(배치·표시) | `LobbyDeckUI`·`LobbyDeckListItemView`·프리팹, 필요하면 Presenter | `ILobbyDeckDisplay`, `ILobbyMainSelectionWriter` | 팝업 재생 확인. 로스터·Run 시작 내부는 안 봄 |
| 선택 전달 방식 | `LobbyDepartureFlow`·`LobbyViewBuffer`·`LobbyDepartureRequest`·`LobbyPlan`·`RunStartJob` | `ILobbyMainSelectionReader`, 요청 계약, `IRunStartService` | 출발·거절·재출발 재생 확인. 팝업 코드는 안 봄 |
| 공용 계약(`RunStartJob` 생성자·요청 계약) | 위 + `StagePlan` 한 줄 | Stage 첫 진입 경로 | Stage 첫 진입(새 Run) 재생 확인 추가 |
| 로비 공통 조립(`ILobbySceneFactory`·`LobbyViewScope`·`LobbyViewServices`·`AttachFromScene`) | 위 넷 | 팝업 목록 순서, 공통 팝업 입력 세션, 이전 연결을 떼기 전 검사 순서 | 덱·아티팩트 팝업 열고 닫기와 로비 입력 복귀, 출발, 로비 재진입 재생 확인 |

## 7. 원칙 점검

- SSOT: 후보·기본 = Core, 이름·아이콘 = View DB, 선택 = 선택 객체 하나, 제출 = 요청, 설치 = 로스터. 사본 없음.
- SRP: 선택 객체는 선택만, 팝업은 표시·입력만, 흐름은 제출만, Job은 Core 호출만.
- ISP: View는 Core의 후보 조회 대리자만 받는다. 출발 흐름은 선택 읽기만 받는다.
- FW·UI 규약: 덱 팝업은 이미 `UIBase` 등록 화면이다(§2 UI 등록 줄). 새 버튼은 `UIInputBinding`. 필수 참조 누락은 예외.
- 서비스 경로: Core 후보 조회 = `InGameCoreSystemFactory` 생성·내부 등록 → `InGameCoreSystemGroup.RunStart` → `InGameSystemGroup.CoreGroup` → `LobbyViewSystem.PostBindService`의 InGame 그룹 조회(지금 `LobbyPlan`과 같은 길). 유닛 외형 = View DB `ViewDataBase.UnitVisualTableQuery`(`InitData`로 받은 `IViewQuery`). 하위 서비스를 루트에 다시 등록하지 않는다.
- 로드아웃 키: View가 LifeCycle 상수 `RunStartJob.DefaultLoadoutKey`를 읽는 곳은 `LobbyViewSystem` 조립 한 줄뿐이다(인계 §4.7 "조립으로 넘김"에 맞음).
- 성능: 후보 조회와 카드 내용 만들기는 연결마다 한 번이다. 팝업을 열 때 새로 만들지 않는다. Tick 할당·Tick 판정 없음.
- 독립성: §6 표.

## 8. Unity 쪽 할 일 (Codex 몫, 코드 계약이 나온 뒤)

- `LobbyDeckUI`에 `selectButton` 연결, 버튼 배치.
- 덱 카드 템플릿(`LobbyDeckListItemView`)에 `selectedMark` 오브젝트 만들고 연결.
- 두 칸 모두 필수 참조라, 연결 전에는 로비 진입이 `Validate` 예외로 멈춘다(커밋 순서 주의).

## 9. 결정 대기

- **Q-LM1 선택 원본 자리·세션 정의** — A(권장): 세션 = Lobby 씬 Enter~Exit 콜백 사이(같은 씬 재로딩도 새 세션), `LobbyMainSelection`을 연결마다 만들고 `LobbyViewScope`가 갖는다(LM1·§5). B: 같은 씬 재로딩을 넘어 선택을 이어 가야 하면 `LobbyViewSystem`이 하나를 갖고, "다른 씬에서 로비로 들어올 때만" 기본값으로 다시 만든다(이전 씬 번호를 봐야 해서 규칙이 하나 더 생김). 지금 일반 흐름에서 같은 씬 재로딩을 여는 곳이 없어 A를 권장.
- **Q-LM2 고르는 방식** — A(권장): 클릭 = 상세 고정, "다음 Run 메인으로 선택" 버튼으로 선택(§LM3). B: 클릭하면 바로 선택(고정 규칙과 섞여 인계 문서 §3 "클릭은 상세 고정만"과 어긋남).
- **Q-LM3 설치된 메인 확인** — A(권장): `RunStartJob`이 지정 키와 설치된 메인 키가 같은지 한 줄 더 확인한다. B: Core 약속만 믿고 확인하지 않는다.
- **Q-LM4 아이콘이 비었을 때** — A(권장): 로비 후보 카드를 만드는 곳(`LobbyMainCards.Build`, 덱 팝업 Factory 생성 = 연결 시작, 이전 연결을 떼기 전)에서 메인 후보의 `InfoIcon`이 없으면 후보 키를 담은 예외(오류 경계). 외형 표 전체의 공통 필수 조건으로는 넓히지 않는다. B: 이름만 보이고 아이콘 칸을 끈다(지금 `Render` 동작).
- **Q-LM5 로비 판 위 캐릭터** — A(권장): 이번에는 그대로(판 위 말은 `LobbyModelData.CharacterPrefab` 하나로 고정). B: 고른 메인의 모습으로 바꾸는 작업을 함께(외형 표에 로비용 프리팹 필요, 범위 커짐).
- 이어짐: [2026-11-17 메인 모델 설계](2026-11-17-lobby-stageselect-main-model-design-Hojin.md)에서 로비·스테이지 선택 모두 선택한 메인의 전투 외형 표 프리팹을 사용하도록 연결했다. Q-LM5의 고정 모델 설명은 당시 범위 결정의 이력이다.

## 10. Phase (안)

- **LM-a 전달 길**: 요청 키·`Submit(EntityKey)`·`RunStartJob` 인자·`LobbyPlan`·`StagePlan`. 화면 변화 없음(아직 아무도 키를 안 실음).
- **LM-b 선택 원본·조립**: `LobbyMainSelection`·계약 둘·`LobbyViewServices` 대리자·`AttachFromScene`·`ILobbySceneFactory`·`LobbyViewScope`·`LobbyDepartureFlow`. 이 단계부터 출발이 늘 키를 싣지만 값은 기본 메인이라 화면 변화 없음.
- **LM-c 덱 팝업**: 후보 표시·선택 표시·선택 버튼(C#), 이어서 Codex 프리팹 연결.
- **LM-d 문서**: 인계 문서에 "View 대응" 절, 다른 작업자 줄 메모, 사용자 재생 확인 목록.

Phase마다 Codex 검수 뒤 보고. 새 시험 코드 없음, 컴파일은 사용자 확인.

### 사용자 재생 확인 목록 (LM-c 뒤)

1. 팝업에 에블린·로즈 둘이 보이고, 처음에는 로드아웃 기본 메인에 선택 표시가 있다.
2. 다른 카드를 클릭하면 상세만 바뀌고 선택 표시는 그대로다. 같은 카드를 다시 누르면 상세가 닫힌다.
3. 고정한 카드로 "다음 Run 메인으로 선택"을 누르면 선택 표시가 옮겨 간다. 이미 선택된 카드를 고정했을 때는 버튼이 꺼져 있다.
4. 팝업을 닫았다 열어도 선택이 남는다.
5. 에블린으로 출발 → 에블린으로 Stage에 들어간다. 로비로 돌아와 로즈로 출발 → 로즈로 들어간다. 두 번 모두 서브·골드·손패가 같다.
6. 전투에서 고른 메인의 스킬·특성·외형이 맞다(두 메인 모두).
7. Run이 끝나 로비로 돌아오면 선택이 기본 메인으로 돌아와 있다.
8. Stage 첫 진입(로비를 거치지 않는 경로)과 개발 진입(`GameBootstrap`)은 지금처럼 기본 메인으로 시작한다.
9. 출발을 빠르게 두 번 눌러도 한 번만 출발하고 처음 고른 메인으로 들어간다.
10. 아티팩트 팝업을 열고 닫은 뒤 로비 입력이 돌아오고, 덱 팝업·출발이 그대로 된다.
11. 외형 누락(메인 후보의 외형 행 또는 아이콘이 비었을 때)은 로비 진입 순간 오류 경계로 멈춘다. 일부러 에셋을 비워 보는 확인이라 하지 않으면 미확인.
12. 정상 거절 뒤 재출발은 정상 데이터로 거절을 일으킬 조작이 없어 재생 확인은 미확인으로 남긴다(잘못된 로드아웃은 후보 조회에서 먼저 예외라 거절 재현이 아니다).

### 정적 대조 항목 (구현 뒤, 실행 아님)

- 다른 키로 중복 제출해도 Buffer가 같은 요청 참조·번호·원래 키를 돌려준다(`LobbyViewBuffer.Submit`).
- 실행 전 취소된 요청 뒤 새 제출은 새 번호·새 키를 받는다.
- `Submit()`·Stage 첫 진입·개발 진입·임시 Core 조립은 키 없는 기본 경로를 그대로 쓴다.
- 정상 거절 뒤 `IsHolding`이 풀리고 선택이 남는다.
- 같은 씬 재로딩·실행 전 취소의 수명 처리(§5).

## 11. Codex 검수 기록 (2026-11-16, 1판 대상, 읽기 전용 정적 대조)

전체 판단: "보완 후 재검수". 지적 7개를 모두 수용해 2판에 반영했다.

| 번호 | 심각도 | 지적 | 판단 | 반영 |
|---|---|---|---|---|
| 1 | 높음 | 같은 씬 번호 재로딩은 Exit → Enter를 다시 돌려 연결이 바뀐다. "연결 = 방문" 단정 불가 | 수용(코드 확인: `LodingSystem.BeginSceneLoad`) | §2 연결 수명, LM1 세션 정의, §5 마지막 줄, Q-LM1 |
| 2 | 중간 | 돌려받은 요청의 키를 읽고 표시하는 계약 없음. 바깥 경로로 연 팝업은 출발 상태를 안 보고 인수됨 | 수용(코드 확인: `PopupViewScope.CanContinue`) | §2 겹침 막기, LM1 `IsLocked`·`Shown`, LM2 `BindDepartureHold`, LM3 버튼 조건, LM4 읽기 계약·키의 뜻, Q-LM6 |
| 3 | 중간 | `UnitVisual` 주입 경로가 비었음. Presenter 생성 주체는 Factory | 수용 | LM2 그림(Services → 덱 묶음 → Factory → 컨트롤러) |
| 4 | 중간 | 오류가 늘 `Faulted`는 아님(오류 정리의 입력 정리는 실행 전 요청을 취소) | 수용 | §5 취소·조회 실패·Job 예외·끝난 요청 줄 |
| 5 | 중간 | 독립성 표·확인 목록에 공통 조립 영향, 재연결·중복·취소·거절·외형 누락·아티팩트 복귀 빠짐 | 수용 | §6 공통 조립 줄, §10 확인 9~13 |
| 6 | 낮음 | 선택 슬롯 수신·호버 해제·켜짐 처리 자리 명시 필요 | 수용 | LM3 `LobbyDeckUI`·`LobbyDeckListItemView`·Presenter 줄 |
| 7 | 낮음 | `Assets/Tests`는 지워지지 않았음(563개) | 수용(1판의 내 잘못) | §2 시험 줄 |

Q 의견: Q-LM2는 버튼 문구를 "다음 Run 메인으로 선택"으로(수용), Q-LM3·Q-LM4·Q-LM5는 A 동의, Q-LM1은 세션 정의를 먼저 정하라는 의견(2판 Q-LM1에 반영).
실행 기록: Codex 세션은 저장됨, 결과 원문은 이 세션의 스크래치패드 `codex-lm-design/result.txt`(저장소 밖).

### 11.1 재검수 (2026-11-16, 2판 대상)

1판 지적 해소: 1·3·6·7 해소, 2·4·5 일부 해소(아래 8~11로 이어짐). 전체 판단: "보완 후 구현 계획으로 진행". 새 지적 4개를 모두 수용해 3판에 반영했다.

| 번호 | 심각도 | 지적 | 판단 | 반영 |
|---|---|---|---|---|
| 8 | 중간 | 처리 중 바깥 경로로 연 팝업도 입력 수명을 못 잡아 `AbortOpen`으로 닫힌다. 2판의 "열려서 제출 키를 표시" 전제가 틀림 | 수용(코드 확인: `PopupInputSession.Begin`·`PopupViewScope.BeginInput`). 2판에서 1판 검수 2번을 과하게 받아들인 내 잘못 | §2 겹침 막기, LM1 `IsLocked`은 방어용·`Shown` 삭제, LM3 잠김 문단, §5, Q-LM6 삭제 |
| 9 | 중간 | 외형 조회를 팝업 `Show`에서 하면서 수명 표는 "연결 전 실패"라고 적어 모순 | 수용. 검사 시점을 연결 시작으로 옮기는 쪽을 택함 | LM2 `LobbyMainCards.Build`를 덱 팝업 Factory 생성자(이전 연결을 떼기 전)로, §5 실패 줄 둘로 나눔, Q-LM4, 확인 11 |
| 10 | 중간 | 키 없는 요청을 draft로 표시하면 실제 출발 메인과 다를 수 있음 | 수용. 처리 중 요청 키를 표시할 곳이 없어 `HeldMainKey`·`Shown`을 걷고, 읽기 계약 `ILobbyDepartureState`에도 키를 더하지 않음 | LM1, LM4 |
| 11 | 낮음 | 중복 반환·기본 호출 경로 확인 부족, "잘못된 로드아웃"은 거절 재현이 아님 | 수용 | §10 확인 8·12, 정적 대조 항목 절 |

Q 의견: Q-LM1 A 가능(세션 정의를 사용자 결정으로 확정), Q-LM2~Q-LM5 A 권장, Q-LM6은 현행 차단 유지 시 A 가능 → 3판에서는 잠김을 Tick마다 볼 이유가 없어져 질문을 지움(선택 클릭 순간에도 `CanSelect`를 다시 보는 의견은 LM3에 반영).
결과 원문: 이 세션 스크래치패드 `codex-lm-design-r2/result.txt`(저장소 밖).

## 12. 구현 메모 (2026-11-16, LM-a~LM-c 뒤)

구현 기록의 원본은 [Task 계획 §8](../plans/2026-11-16-lobby-main-selection-view-plan-Hojin.md)이다. 여기에는 설계와 다르게 된 곳과 정적 대조 결과만 적는다.

### 12.1 설계와 다르게 된 곳

- `LobbyMainSelection` 생성자가 출발 로드아웃 키를 함께 받는다(`(RunStartMainOptions, int expectedLoadoutKey)`). LM2의 "받은 `options.LoadoutKey`가 다르면 예외"를 생성자 한 곳에서 한다.
- 덱 Presenter의 `SetModel`은 `private`이고, 바깥 입구는 `ShowCandidates()` 하나다. Presenter 생성자가 카드 수·키·순서를 후보와 대조한다(조립 오류 검사, 자격 재판정 아님).
- 덱 화면은 선택 버튼 판정(`_selectEnabled`)을 닫기·재바인딩뿐 아니라 목록 교체(`SetModel`) 때도 버린다. Presenter가 새 목록으로 다시 넘긴다.
- 선택 버튼의 시각 값은 Codex가 기존 UI 값만 가져와 정했다(계획 §8.3.3 출처 표). 닫기 버튼 크기에 문구가 들어가지 않아 다른 팝업 구매 버튼 규격(348×84)을 써서, 같은 줄의 닫기 버튼(224×70)과 높이가 다르다. 선택 표시는 덱 교체 강조(`OwnedUnitSlot/Outline`)를 다시 썼다.
- 프리팹의 옛 안내 `Panel/Frame/Empty`("현재 메인 캐릭터는 Akimbo로 고정")는 비활성으로 돌렸다(지우지 않음).

### 12.2 정적 대조 항목 결과 (§10, 실행 아님)

| 항목 | 결과 | 근거 |
|---|---|---|
| 다른 키로 중복 제출해도 같은 요청 참조·번호·원래 키 | 성립 | `LobbyViewBuffer`의 공통 `Submit(EntityKey?)`가 진행 중(`IsPending`) 요청이면 번호 발급·생성 전에 그 요청을 돌려준다. 키는 `LobbyDepartureRequest` 생성자에서만 정해진다 |
| 실행 전 취소 뒤 새 제출은 새 번호·새 키 | 성립 | 취소된 요청은 `IsPending`이 거짓이라 새 요청을 만든다(`++_lastIssuedNumber`, 그때의 키) |
| `Submit()`·Stage 첫 진입·개발 진입·임시 Core 조립은 키 없는 기본 경로 | 성립 | `Submit()` → `Submit(null)`, `StagePlan`은 `mainCharacterKey: null`, `RunStartJob`은 키가 없으면 `StartNewRun(DefaultLoadoutKey)`. `GameBootstrap`·`TemporaryCoreRuntimeFactory`는 `StartNewRun(key)`를 직접 부르며 바뀌지 않았다. 로비 출발은 이제 늘 `Submit(EntityKey)`를 부르므로 `Submit()`을 부르는 운영 코드는 없다(호환 입구로 남김) |
| 정상 거절 뒤 `IsHolding`이 풀리고 선택이 남음 | 성립 | 거절 요청은 `IsPending` 거짓·`CanResubmit` 참이라 `IsHolding` 거짓. 선택은 출발 흐름과 다른 객체(`LobbyMainSelection`)라 그대로 |
| 같은 씬 재로딩·실행 전 취소의 수명(§5) | 성립 | 재로딩의 Exit → `Detach` → `LobbyViewScope.Dispose`가 `CancelOwnedRequest`. Enter → `AttachFromScene`이 새 선택을 만든다(기본 메인). 이전 연결의 선택은 이전 출발 흐름에만 묶여 있고 새 연결에 넘어가지 않는다 |

### 12.3 남은 것·관찰

- 재생 확인 11(외형 누락)·12(정상 거절 뒤 재출발)는 일부러 에셋을 비우거나 거절을 일으킬 조작이 필요해 미확인이다.
- 로즈(Sniper, 키 12)는 옛 프리팹(`Prefabs/Characters/Colored/Playerable/Sniper.prefab`, 옛 FBX·모델 배율 1.5)이라 전투에서 크게 보인다. 사용자 결정 "그냥 쓰는걸로"로 이번 범위 밖이다(New 폴더에 Sniper 모델 없음).
- 로비에서 시작 서브를 고르는 편집은 사용자도 필요하다고 보지만, 이번 작업은 메인 선택만 하고 Core 요청은 하지 않는다(사용자 결정, 계획 §8.3.2). 로비 덱 의미(LC0)·덱 후보/확정(LC2)은 로비 설계 §10 그대로 미합의다.
