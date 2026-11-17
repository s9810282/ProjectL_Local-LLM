# 로비 메인 선택 View 연결 Task 계획

날짜: 2026-11-16
상태: 계획 2판(Codex 계획 검수 반영, §7). LM-a 완료(§8.1, Ua 문제 없음 — 사용자). LM-b 완료(§8.2, Ub 문제 없음 — 사용자). LM-c C# 완료(§8.3, 컴파일 미확인), 프리팹 연결 완료(§8.3.3, Codex), Uc 문제 없음 — 사용자. LM-d 완료(§8.4·§8.5). 남은 것: 사용자 커밋.
설계: [lobby-main-selection-view-design](../specs/2026-11-16-lobby-main-selection-view-design-Hojin.md) 3판, 결정 Q-LM1~Q-LM5 모두 A(2026-11-16 사용자 "권장안대로 계획").
받은 인계: [Core → View 인계](../handoffs/2026-11-15-run-start-main-selection-core-view-handoff.md).

## 0. 진행 규칙

- Phase 순서: LM-a → LM-b → LM-c → LM-d. Phase마다 멈추고 보고한다(리뷰 지점).
- Phase 끝마다 Codex 검수(읽기 전용) 뒤 지적별 수용·반려·사용자 판단을 보고에 넣는다.
- 새 시험 코드·시험 도우미 없음, MSBuild 없음. C#을 바꾼 보고에는 "컴파일 미확인". 컴파일·재생은 사용자가 Unity에서 한다.
- 기존 시험은 우리 변경으로 깨지는 곳만 최소로 맞춘다(설계 §2 시험 줄: 지금 검색으로는 직접 참조 없음).
- 새 `.cs`의 `.meta`는 Unity가 만든다. 커밋 때 함께 넣는다.
- 커밋·push는 사용자가 한다.
- 다른 작업자 줄(Twinsgm·JMH 등)을 바꾸면 구현 기록에 적고 LM-d에서 메모로 모은다.

## 1. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 내용 | 설계와 다른가 |
|---|---|---|
| TP-1 | `LobbyMainSelection`·계약 둘·`ILobbyDepartureHold`·`LobbyMainCards`·`LobbyDeckServices`는 모두 `ViewSystem/Lobby/` 아래에 둔다. 선택·출발 계약은 `Lobby/`, 덱 전용(`LobbyMainCards`·`LobbyDeckServices`)은 `Lobby/Deck/`. 네임스페이스 = 폴더 경로 | 아니오(위치를 구체화) |
| TP-2 | 계약 셋(`ILobbyMainSelectionReader`·`ILobbyMainSelectionWriter`·`ILobbyDepartureHold`)은 파일 하나에 하나씩 둔다 | 아니오 |
| TP-3 | `LobbyMainSelection`은 `ILobbyMainSelectionWriter`를 구현하고, Writer가 Reader를 상속한다 | 아니오(구체화) |
| TP-4 | `RunStartJob` 생성자의 새 인자 `EntityKey? mainCharacterKey`는 `outcome` 바로 뒤에 둔다(선택 인자로 하지 않음 — 호출 둘이 모두 명시) | 아니오(자리 구체화) |
| TP-5 | 설치 확인(Q-LM3)은 `RequireInstalledRoster` 안 메인 갈래에서 `_mainCharacterKey.HasValue`일 때만 `installed.CharacterKey != _mainCharacterKey.Value`면 예외. 키 없는 요청(Stage·기본 경로)은 비교하지 않고, 기존 로스터·메인 지속 상태 검사는 지정 여부와 무관하게 그대로 | 아니오 |
| TP-6 | 선택 슬롯 상수 `SelectSlot = -2`는 `LobbyDeckUI`에 `CloseSlot` 옆으로 둔다(Presenter도 이 상수를 읽음, 지금 `CloseSlot`과 같은 방식) | 아니오 |
| TP-7 | 선택 표시는 `ILobbyDeckDisplay.ShowSelected(int slot)`이 모든 보이는 항목의 `SetSelected(i == slot)`을 부른다. `-1`이면 모두 끔(모델이 비었을 때) | 아니오(`-1` 뜻 추가) |
| TP-8 | 버튼 문구는 프리팹 글자(Codex 몫)로 둔다. C#은 문구를 쓰지 않는다 | 아니오 |
| TP-9 | LM-b에서 덱 팝업 Factory 생성자가 카드를 만들지만, Presenter·컨트롤러가 그 카드를 쓰는 것은 LM-c다. LM-b는 카드를 만들어 검사만 하고 Factory 필드에 보관한다(외형 실패가 LM-b부터 진입을 막는지 확인 가능) | 아니오(Phase 나눔) |

## 2. Phase LM-a 전달 길 (화면 변화 없음)

목표: 출발 요청이 메인 키를 실을 수 있고, Job이 그 키로 Core를 부른다. 아직 아무도 키를 싣지 않는다.

| Task | 파일 | 할 일 |
|---|---|---|
| a.1 | `BufferSystems/LobbyView/LobbyDepartureRequest.cs` | `public EntityKey? MainCharacterKey { get; }`. 생성자 `internal LobbyDepartureRequest(int number, EntityKey? mainCharacterKey)`. 클래스 요약에 "키는 생성 때 한 번, 이후 불변" 한 줄 |
| a.2 | `BufferSystems/LobbyView/LobbyViewBuffer.cs` | `ILobbyViewBufferWriteSink`에 `ILobbyDepartureState Submit(EntityKey mainCharacterKey)`. 구현은 공통 private `Submit(EntityKey? key)`로 모으고, 기존 `Submit()`은 `Submit(null)`. 진행 중이면 지금처럼 그 요청 반환(키 덮어쓰기 없음). 문서 주석에 `EntityKey.None`도 그대로 싣는다고 적음 |
| a.3 | `LifeCycleSystem/LifeCycles/RunStart/RunStartJob.cs` | 필드 `_mainCharacterKey`, 생성자 인자(TP-4). `Execute`: 값이 있으면 `StartNewRun(DefaultLoadoutKey, key)`, 없으면 지금 호출. 설치 확인(TP-5). 클래스 요약의 "기본 로드아웃으로" 문구를 "기본 로드아웃(메인은 지정하면 그 메인)으로"로 |
| a.4 | `LifeCycleSystem/LifeCycles/Lobby/Plans/LobbyPlan.cs` | `new RunStartJob(..., new LobbyRunStartOutcome(request), request.MainCharacterKey, ...)` |
| a.5 | `LifeCycleSystem/LifeCycles/Stage/Plans/StagePlan.cs` | `new RunStartJob(..., StageRunStartOutcome.Instance, null, ...)` |
| a.6 | 검색 | `new RunStartJob(`·`new LobbyDepartureRequest(`·`ILobbyViewBufferWriteSink` 구현을 저장소 전체(시험 포함)에서 다시 찾아 빠진 호출이 없는지 확인 |

Unity 확인 Ua (사용자):
1. 컴파일 오류 없음.
2. 로비 출발 → Stage 진입이 지금처럼 된다(기본 메인).
3. 로비를 거치지 않는 Stage 첫 진입이 지금처럼 된다.

## 3. Phase LM-b 선택 원본·조립 (화면 변화 없음)

목표: 로비 연결마다 선택 객체가 생기고, 출발이 늘 그 값을 싣는다(값 = 기본 메인이라 화면은 그대로).

| Task | 파일 | 할 일 |
|---|---|---|
| b.1 | 새 `InGameSystem/ViewSystem/Lobby/ILobbyMainSelectionReader.cs` | `EntityKey Selected { get; }` |
| b.2 | 새 `…/Lobby/ILobbyMainSelectionWriter.cs` | `: ILobbyMainSelectionReader`, `IReadOnlyList<EntityKey> Candidates`, `bool IsLocked`, `bool Select(EntityKey key)` |
| b.3 | 새 `…/Lobby/ILobbyDepartureHold.cs` | `bool IsHolding { get; }` |
| b.4 | 새 `…/Lobby/LobbyMainSelection.cs` | 설계 LM1 그대로. 생성자 `(RunStartMainOptions options, int expectedLoadoutKey)`: `options == null`이면 `ArgumentNullException(nameof(options))`, 빈 후보·기본값이 후보 밖·`options.LoadoutKey != expectedLoadoutKey`면 구성 오류 `InvalidOperationException`(원인 키 포함). `BindDepartureHold(ILobbyDepartureHold hold)`: null이면 `ArgumentNullException`, 두 번째면 `InvalidOperationException`. `IsLocked`는 바인딩 전이면 `InvalidOperationException`. `Select`: 후보 밖 `ArgumentException`, 잠김 `InvalidOperationException`, 같은 값 false |
| b.5 | 새 `…/Lobby/Deck/LobbyMainCards.cs` | `internal static IReadOnlyList<LobbyDeckCardContent> Build(IReadOnlyList<EntityKey> candidates, Func<EntityKey, UnitVisualData> visuals)`. 후보마다 `visuals(key)`(없으면 표가 `KeyNotFoundException`), `InfoIcon == null`이면 후보 키를 담은 `InvalidOperationException`, `new LobbyDeckCardContent(key, DisplayName, InfoIcon)`. 결과는 `Array.AsReadOnly` |
| b.6 | 새 `…/Lobby/Deck/LobbyDeckServices.cs` | `UI`(`IUISystemService`), `UnitVisuals`(`Func<EntityKey, UnitVisualData>`). 생성자에서 null만 검사 |
| b.7 | `…/Lobby/LobbyViewServices.cs` | 생성자 인자 둘 추가: `Func<RunStartMainOptions> readMainOptions`, `Func<EntityKey, UnitVisualData> unitVisuals`. 프로퍼티 `ReadMainOptions`, `Deck = new LobbyDeckServices(ui, unitVisuals)`. 클래스 요약에 두 묶음 한 줄 |
| b.8 | `…/Lobby/LobbyViewSystem.cs` | `PostBindService`: InGame 그룹을 지역 변수로 한 번 받아 `ViewGroup.Input`·`CoreGroup.RunStart`를 같이 꺼냄. `readMainOptions: () => runStart.ReadMainOptions(RunStartJob.DefaultLoadoutKey)`, `unitVisuals: view.UnitVisualTableQuery.GetUnitVisual`(`_view = null` 앞). `AttachFromScene`: `root.Validate()` 다음 `var selection = new LobbyMainSelection(Services.ReadMainOptions(), RunStartJob.DefaultLoadoutKey)` → `FindPopups(root, Services, selection, _faults)` → `Attach(root, popups, selection, ReadOutputCamera())`. `Attach`가 `selection`을 `Create`에 넘김 |
| b.9 | `…/Lobby/ILobbySceneFactory.cs` | `FindPopups`·`Create`에 `LobbyMainSelection selection` 인자(`faults` 앞). 문서 주석에 "선택은 이전 연결을 떼기 전에 만들어 넘긴다" |
| b.10 | `Factory/Runtime/InGame/View/LobbySceneFactory.cs` | `FindPopups`: `new LobbyDeckPopupFactory(services.Deck, root.UI.DeckScreen, selection, faults)`. `Create`: `new LobbyViewScope(root, output, services, popups, selection, faults)`. null 검사 한 줄씩 |
| b.11 | `…/Lobby/Deck/LobbyDeckPopupFactory.cs` | 생성자 `(LobbyDeckServices deck, ILobbyDeckDisplay screen, LobbyMainSelection selection, RuntimeFaultBoundary faults)`. 생성자에서 `_cards = LobbyMainCards.Build(selection.Candidates, deck.UnitVisuals)`(TP-9: 보관만). `Create`는 아직 지금 그대로(`deck.UI` 사용) |
| b.12 | `…/Lobby/LobbyDepartureFlow.cs` | `: ILobbyDepartureHold`. 생성자에 `ILobbyMainSelectionReader selection`(text 다음). `Submit`이 `_write.Submit(_selection.Selected)`. `IsHolding` 판정식은 그대로 두고, 지금 `internal` 프로퍼티라 인터페이스를 채우지 못하므로 명시적 구현 `bool ILobbyDepartureHold.IsHolding => IsHolding;`을 더한다(기존 사용처 그대로) |
| b.13 | `…/Lobby/LobbyViewScope.cs` | 생성자 인자 `LobbyMainSelection selection`(`popups` 다음). `_departure = new LobbyDepartureFlow(services.Departure, services.Text, selection, _canWork)` 직후 `selection.BindDepartureHold(_departure)` |
| b.14 | 검색 | `ILobbySceneFactory` 구현·`LobbyViewScope`·`LobbyDepartureFlow`·`LobbyDeckPopupFactory`·`LobbyViewServices` 생성 호출을 시험 포함 저장소 전체에서 다시 찾음 |

Unity 확인 Ub (사용자):
1. 컴파일 오류 없음.
2. 로비 진입·덱 팝업 열고 닫기(아직 빈 목록)·아티팩트 팝업·출발이 지금처럼 된다.
3. 출발하면 기본 메인(에블린)으로 들어간다.
4. Run이 끝나 로비로 돌아와 다시 출발이 된다.

## 4. Phase LM-c 덱 팝업 (C#, 이어서 Codex 프리팹 작업)

목표: 팝업에 후보가 보이고, 고정과 따로 선택을 바꿀 수 있다.

| Task | 파일 | 할 일 |
|---|---|---|
| c.1 | `GlobalSystem/UISystem/Screens/Lobby/LobbyDeckListItemView.cs` | `[SerializeField] GameObject selectedMark`, `internal void SetSelected(bool)`(바뀔 때만 `SetActive`), `Validate`에 null·항목 안 검사 |
| c.2 | `…/Screens/Lobby/ILobbyDeckDisplay.cs` | `void ShowSelected(int slot)`, `void SetSelectEnabled(bool enabled)` |
| c.3 | `…/Screens/Lobby/LobbyDeckUI.cs` | `SelectSlot = -2`(TP-6), `[SerializeField] Button selectButton`, 필드 `_selectBinding`·`_selectEnabled`. `EnsureSelectBinding`(`EnsureCloseBinding` 옆, `BindInput`·`SetModel`에서 같이 부름). `Click`이 `SelectSlot`을 수신자에 넘김. `ClearControlInput`이 선택 버튼 끄기·호버 해제. `RefreshInputStates`가 `selectButton.interactable = enabled && _selectEnabled`. `ShowSelected`(TP-7), `SetSelectEnabled`(값 저장 뒤 `RefreshInputStates`). `Validate`에 `selectButton` null·패널 안 검사. `Hide`·닫힘 경로에서 `_selectEnabled = false`. `BindInput`(재바인딩)도 입력 정리 단계에서 `_selectEnabled = false`로 되돌린 뒤 수신자를 바꾸고, 새 Presenter가 `RefreshSelect`로 다시 판정한다(이전 값으로 버튼이 켜지지 않게, 기존 `_version` 재진입 보호 유지) |
| c.4 | `InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPresenter.cs` | 생성자 `(ILobbyDeckDisplay view, ILobbyMainSelectionWriter selection, IReadOnlyList<LobbyDeckCardContent> cards, Action requestClose)`. `ShowCandidates()`(새): `SetModel(new LobbyDeckDisplayModel(cards))` 뒤 `ShowSelected(IndexOf(selection.Selected))`, `RefreshSelect()`. `CanSelect` = 입력 가능 ∧ `_pinnedSlot >= 0` ∧ `cards[_pinnedSlot].Key != selection.Selected` ∧ `!selection.IsLocked`. `RefreshSelect()` = `SetSelectEnabled(CanSelect)`. `Click`: `SelectSlot`이면 `CanSelect`를 다시 보고 통과 시 `selection.Select(key)` → `ShowSelected` → `RefreshSelect`. 고정·고정 해제 뒤 `RefreshSelect`. `Clear`·`SetInputEnabled(false)`에서 `SetSelectEnabled(false)` |
| c.5 | `…/Lobby/Deck/LobbyDeckPopupController.cs` | `Show`의 빈 모델 줄을 `_presenter.ShowCandidates()`로. "운영 후보 계약 전" 주석 삭제 |
| c.6 | `…/Lobby/Deck/LobbyDeckPopupFactory.cs` | `Create`: `new LobbyDeckPresenter(_screen, _selection, _cards, () => controller?.Close())` |
| c.7 | 문서 | Codex에 넘길 Unity 작업 목록(설계 §8): `LobbyDeckUI.selectButton`(문구 "다음 Run 메인으로 선택")·카드 템플릿 `selectedMark`. 둘 다 필수 참조라 연결 전에는 로비 진입이 `Validate`에서 멈춤 |

Unity 확인 Uc (사용자, Codex 프리팹 작업 뒤): 설계 §10 "사용자 재생 확인 목록" 1~12 전부. 더해서 ① 팝업이 열린 채 다시 연결(같은 씬 재진입)되거나 고정을 푼 뒤 선택 버튼이 꺼져 있음 ② 카드를 고정한 채 다른 카드에 호버해도 고정·선택 버튼 상태가 그대로 ③ 키보드/게임패드 Submit으로 선택 버튼이 눌림. 정적 대조 항목은 LM-d에서 Claude가 한다.

커밋 주의: c.1·c.3의 필수 칸과 Codex 프리팹 연결은 한 커밋(따로면 로비 진입이 멈춤).

## 5. Phase LM-d 문서

| Task | 할 일 |
|---|---|
| d.1 | 설계 §10 정적 대조 항목을 구현 코드로 대조해 결과를 설계 끝 "구현 메모"에 적는다 |
| d.2 | 받은 인계 문서에 "View 대응(2026-11-xx)" 절 추가: 담당 지정 맵 요약, 바뀐 계약(요청 키·`Submit(EntityKey)`·`RunStartJob` 인자), 확인 상태. Core 요청 없음 |
| d.3 | 다른 작업자 줄을 바꿨으면 그 사람에게 넘길 메모(미전달) |
| d.4 | CLAUDE.md "현재 상태"에 한 줄(팀 공유 작업 상태만, 개인 규칙 아님) |
| d.5 | 묶음 끝 Codex 검수 |

## 6. 영향 범위 요약

- 새 `.cs` 6개(계약 셋·`LobbyMainSelection`·`LobbyMainCards`·`LobbyDeckServices`), 고치는 기존 `.cs` 17개. 새 시험 없음.
- 다른 작업자 줄이 있는 파일: `LobbyDeckUI`·`LobbyViewSystem`·`LobbyPlan`·`LobbyDeckPopupController`·`StagePlan`·`LobbySceneFactory`·`LobbyViewServices`(8·45·68행 Twinsgm). 바꾼 줄이 그 사람 줄이면 d.3.
- Core·Framework 계약 변경 없음. GlobalSystem에서는 로비 덱 화면 계약 `ILobbyDeckDisplay`에 메서드 둘을 더한다(c.2, 구현은 `LobbyDeckUI` 하나). Factory는 우리 조립 줄만(`LobbySceneFactory`).

## 7. Codex 계획 검수 기록 (2026-11-16)

판단: TP-5를 보완하면 LM-a 착수 가능. 호출처·구현체 누락 없음(기존 17개·새 6개 맞음), `Assets/Tests` 563개에서 변경 대상 직접 참조 0건, Phase 사이 서명 충돌 없음. 각 Phase 컴파일·재생은 미확인.

| 지적 | 심각도 | 판단 | 반영 |
|---|---|---|---|
| 1 TP-5가 키 없는 요청에서 `.Value`를 읽음(LM-a 로비·Stage 출발이 성공 뒤 예외) | 높음 | 수용 | TP-5에 `HasValue` 조건 |
| 2 `LobbyDepartureFlow.IsHolding`이 `internal`이라 인터페이스를 채우지 못함 | 중간 | 수용 | b.12 명시적 구현 |
| 3 재바인딩 때 이전 `_selectEnabled`가 남음 | 중간 | 수용 | c.3 `BindInput`에서 초기화, Uc에 셋 추가 |
| 4 `options == null` 예외 종류 | 낮음 | 수용 | b.4 `ArgumentNullException` |
| 5 다른 작업자 줄 파일 목록에 `LobbyViewServices` 빠짐 | 낮음 | 수용 | §6 |
| 6 "GlobalSystem 계약 변경 없음"이 c.2와 모순 | 낮음 | 수용 | §6 문구 |

## 8. 실행 기록

### 8.1 LM-a 코드 완료 (2026-11-16, 사용자 "LM-a 진행", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

| Task | 결과 |
|---|---|
| a.1 | `LobbyDepartureRequest`에 `EntityKey? MainCharacterKey`(생성자에서만 설정), 생성자 `(int number, EntityKey? mainCharacterKey)`. 클래스 요약에 불변 한 줄 |
| a.2 | `ILobbyViewBufferWriteSink.Submit(EntityKey)` 추가, 두 입구가 private `Submit(EntityKey?)`로 모임. 진행 중이면 그 요청을 키째 반환, `EntityKey.None`도 그대로 실음(문서 주석) |
| a.3 | `RunStartJob` 생성자 셋째 인자 `EntityKey? mainCharacterKey`(TP-4). 값이 있으면 `StartNewRun(DefaultLoadoutKey, key)`. 설치 확인은 메인 갈래에서 `HasValue`일 때만 키 비교(TP-5), 기존 검사·`Fail` 경로 그대로 |
| a.4 | `LobbyPlan`이 `request.MainCharacterKey`를 넘김(주석 한 줄: 실행 순간 화면 선택은 읽지 않음) |
| a.5 | `StagePlan`이 `mainCharacterKey: null`(이름 있는 인자) |
| a.6 | `new RunStartJob(` 2곳·`new LobbyDepartureRequest(` 1곳·`ILobbyViewBufferWriteSink` 구현 1개, 시험 참조 0건 |

- 계획과 다른 점 없음. 아직 아무도 `Submit(EntityKey)`를 부르지 않아 지금 동작은 그대로다(로비 = `Submit()`, Stage = `null`).
- 다른 작업자 줄: `LobbyPlan`·`StagePlan`은 우리가 바꾼 줄이 RunStartJob 생성 호출 한 줄씩이며, 작성자 확인·메모는 LM-d(d.3)에서.
- Codex 검수(읽기 전용): 높음·중간·낮음 0건. 정적 컴파일(명시적 구현 → private 오버로드 해석, nullable enum, C# 9 이름 있는 인자 위치), 요청 키 보존, TP-5 조건, 호출처·시험 참조를 확인. 컴파일·재생은 미확인.
- Unity 확인 Ua 1~3 문제 없음(2026-11-16 사용자 "Ua 2·3 문제 없음, LM-b"). 첫 Play 때 스크립트 컴파일이 도중에 끊겨(BeeDriver connection terminated) Play가 안 됐다가 다시 컴파일한 뒤 됨 — 코드 원인 아님.

### 8.2 LM-b 코드 완료 (2026-11-16, 사용자 "Ua 2·3 문제 없음, LM-b", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

| Task | 결과 |
|---|---|
| b.1~b.3 | 새 계약 `ILobbyMainSelectionReader`(`Selected`)·`ILobbyMainSelectionWriter`(Reader 상속, `Candidates`·`IsLocked`·`Select`)·`ILobbyDepartureHold`(`IsHolding`), 파일 하나에 하나씩(TP-2·TP-3) |
| b.4 | 새 `LobbyMainSelection`: 생성자 `(RunStartMainOptions, int expectedLoadoutKey)` — null은 `ArgumentNullException`, 로드아웃 불일치·빈 후보·기본값이 후보 밖은 `InvalidOperationException`. 처음 값 = 응답의 기본 메인. `BindDepartureHold` 한 번, `IsLocked`는 연결 전이면 예외, `Select`는 후보 밖 → 잠김 → 같은 값 순서 |
| b.5 | 새 `Deck/LobbyMainCards.Build`: 후보 순서대로 외형 조회, 외형 null·`InfoIcon` 없음은 원인 키를 담은 `InvalidOperationException`, 결과 `Array.AsReadOnly` |
| b.6 | 새 `Deck/LobbyDeckServices`(`UI`·`UnitVisuals`) |
| b.7 | `LobbyViewServices` 끝 인자 `readMainOptions`·`unitVisuals`, 속성 `ReadMainOptions`·`Deck` |
| b.8 | `LobbyViewSystem.PostBindService`가 InGame 그룹을 한 번 받아 `CoreGroup.RunStart`의 후보 조회만 대리자로(`RunStartJob.DefaultLoadoutKey`), 외형은 `view.UnitVisualTableQuery.GetUnitVisual`(`_view = null` 앞). `AttachFromScene`이 `root.Validate()` 뒤 선택을 만들어 `FindPopups`·`Attach`에 넘김 |
| b.9·b.10 | `ILobbySceneFactory`·`LobbySceneFactory` 두 입구에 `LobbyMainSelection selection`(`faults` 앞), null 검사 |
| b.11 | `LobbyDeckPopupFactory` 생성자 `(LobbyDeckServices, ILobbyDeckDisplay, LobbyMainSelection, faults)`, 생성자에서 카드를 만들어 보관(TP-9, `Create`는 그대로) |
| b.12 | `LobbyDepartureFlow : ILobbyDepartureHold`, 생성자에 선택 읽기 계약, `Submit`이 `_write.Submit(_selection.Selected)`, `bool ILobbyDepartureHold.IsHolding => IsHolding;` |
| b.13 | `LobbyViewScope` 생성자 인자 `selection`, 출발 흐름을 만든 직후 `selection.BindDepartureHold(_departure)` |
| b.14 | `ILobbySceneFactory` 구현 1개, 바뀐 생성 호출 각 1곳, `Assets/Tests` 직접 참조 0건 |

- 계획과 다른 점 없음. 새 `.cs` 여섯의 줄바꿈은 주변 파일과 같은 CRLF(BOM 없음), `.meta`는 Unity가 만든다.
- 이 단계부터 로비 출발은 늘 기본 메인 키를 명시해 Core 지정 시작을 부른다. 기본 메인을 명시하면 로스터·재화·손패·초기 덱은 기본 경로와 같다(Codex가 `RunStartService`·`PlayerLoadoutRosterPlanner`로 확인). 덱 팝업은 아직 빈 목록.
- `LobbyDeckPopupFactory`의 `_selection`·`_cards`는 LM-c까지 읽히지 않는다(TP-9). Unity에서 "할당했으나 쓰지 않는 필드" 경고가 날 수 있으며 LM-c에서 없어진다.
- 다른 작업자 줄: `LobbyViewSystem`·`LobbySceneFactory`·`LobbyViewServices`에 우리가 더한 줄이 있다. 기존 줄 중 고친 것은 서명 줄(`FindPopups`·`Create`·`Attach`·생성자 인자 목록)과 `PostBindService`의 InGame 그룹 조회 한 줄. 작성자 확인·메모는 LM-d(d.3).
- Codex 검수(읽기 전용): 높음·중간·낮음 0건. 정적 컴파일, LM2 순서(선택·카드 실패가 이전 연결을 떼기 전), 재진입·교체 때 선택 원본이 둘이 되지 않음, 서비스 조립 경로(Core·View DB·UI 모두 Factory 등록 → Group → 소비자, 루트 재등록 없음)를 확인. 참고로 Codex는 "오류 경계는 기존 연결까지 정리하므로, 검사 실패 뒤 기존 화면을 계속 쓰는 것을 보장하지는 않는다"고 적었다 — 설계 LM2의 "이전 연결을 그대로 둔 채"는 "불완전한 새 연결로 바꿔 끼우지 않는다"는 뜻이며 기존 루트 검사와 같은 규칙. 컴파일·재생·오류 재현은 미확인.
- Unity 확인 Ub 1~4 문제 없음(2026-11-16 사용자 "Ub OK, LM-c").

### 8.3 LM-c C# 완료 (2026-11-16, 사용자 "Ub OK, LM-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

| Task | 결과 |
|---|---|
| c.1 | `LobbyDeckListItemView`: 필수 칸 `selectedMark`(GameObject, 툴팁), `SetSelected(bool)`(바뀔 때만), `Validate`가 null·항목 루트 아님·항목 자식을 검사 |
| c.2 | `ILobbyDeckDisplay`: `ShowSelected(int slot)`(-1이면 모두 끔, TP-7)·`SetSelectEnabled(bool)` |
| c.3 | `LobbyDeckUI`: `SelectSlot = -2`(TP-6), 필수 칸 `selectButton`(툴팁), `_selectBinding`·`_selectEnabled`, `EnsureSelectBinding`, `Click`의 `SelectSlot` 갈래, `ClearControlInput`이 선택 버튼 끄기·호버 해제(버전 확인 포함), 선택 버튼 켜짐 = `_selectEnabled ∧ 입력 수신 가능`(`RefreshSelectButton`, 꺼지면 호버 한 번 해제), `ShowSelected`, `SetSelectEnabled`, `Validate`가 null·패널 안 검사. `_selectEnabled`는 닫기(`Hide`)·목록 교체(`SetModel`)·재바인딩(`BindInput`)에서 거짓으로 되돌림 |
| c.4 | `LobbyDeckPresenter`: 생성자 `(view, ILobbyMainSelectionWriter, cards, requestClose)` — 카드 수·키·순서가 후보와 다르면 구성 오류, 후보 모델은 생성자에서 한 번. `ShowCandidates()`(목록 → 선택 표시 → 버튼 판정, 버전 검사), 판정 하나 `CanSelect`, 선택 클릭 `SelectPinned`(클릭 순간 다시 판정), 카드 고정·해제 뒤와 `SetInputEnabled` 끝에서 `RefreshSelect`, `Clear`가 버튼 끔 |
| c.5 | `LobbyDeckPopupController.Show`가 `_presenter.ShowCandidates()`(빈 모델 줄·"운영 후보 계약 전" 주석 삭제) |
| c.6 | `LobbyDeckPopupFactory.Create`가 선택·카드를 Presenter에 넘김(TP-9 끝: LM-b의 미사용 필드 없어짐) |

- 계획과 다른 점(Codex가 타당하다고 봄):
  - `EnsureSelectBinding`을 `BindInput`·`SetModel` 외에 `EnsureCloseBinding`이 불리는 `Awake`·`Open`에서도 부름(null 검사로 중복 없음).
  - 계획은 재바인딩·닫기에서만 `_selectEnabled`를 버리기로 했으나 목록 교체(`SetModel`)에서도 버림(이전 목록의 고정 카드 판정이 남지 않게).
  - `SetSelectEnabled`는 `RefreshInputStates` 전체 대신 선택 버튼만 갱신(`RefreshSelectButton`). `RefreshInputStates`도 끝에서 같은 함수를 부름.
  - Presenter의 `SetModel`을 `private`로 좁힘(후보 밖 목록이 들어오지 않게). `SetInputEnabled`는 켜고 끌 때 모두 `RefreshSelect`.
  - Presenter 생성자에서 카드·후보 대조(조립 오류 검사, 자격 재판정 아님).
- 다른 작업자 줄: `LobbyDeckUI`·`LobbyDeckPopupController`에 우리가 더한 줄과 고친 줄(컨트롤러 `Show`의 빈 모델 줄)이 있다. 작성자 확인·메모는 LM-d(d.3).
- Codex 검수(읽기 전용): 높음·중간 0건, 낮음 1건 — 프리팹 `Panel/Frame/Empty`(활성, 문구 "현재 메인 캐릭터는 Akimbo로 고정되어 있습니다.")가 새 선택과 모순 → **수용, 프리팹 작업 목록 6번**. 재바인딩·닫기·목록 교체 뒤 선택 버튼이 이전 판정으로 켜진 채 남는 경로 없음, `IsLocked`가 바인딩 전에 불리는 경로 없음(단락 평가·조립 순서), Deck 고정 규칙·UIInputBinding·Submit 유지, `ILobbyDeckDisplay` 구현 1개·시험 참조 0건을 확인. 컴파일·재생·재진입 재현은 미확인.
- **지금 상태**: 새 필수 칸 둘이 프리팹에 연결되지 않아 로비 진입이 `LobbyDeckUI.Validate`에서 멈춘다(의도된 상태). 아래 프리팹 작업 뒤 Uc.

#### 8.3.1 Codex 프리팹 작업 목록 (c.7, Codex가 Unity에서, C#은 고치지 않음)

대상: `Assets/ProjectL/Prefabs/UI/Lobby/LobbyDeckPanel.prefab`(원본 프리팹). `Assets/Scenes/Lobby.unity`의 인스턴스는 오버라이드가 Panel 비활성·루트 이름 둘뿐이라 원본을 고치면 상속한다.

```text
LobbyDeckPanel                 ← LobbyDeckUI(등록 루트)
└─ Panel                       ← panel
   ├─ Frame
   │  ├─ Cards                 ← cardsRoot
   │  ├─ Details               ← details
   │  ├─ CloseButton           ← closeButton
   │  └─ Empty                 ← 6번에서 끔
   └─ Templates
      └─ CardTemplate          ← cardTemplate(비활성 유지)
         ├─ Icon
         └─ Name
```

1. **선택 버튼**: `Panel/Frame` 아래에 새 버튼(`SelectButton`)을 만들고 루트 `LobbyDeckUI.selectButton`에 Button 컴포넌트를 연결한다. `Details`·`CardTemplate`과 독립된 형제(Panel 안이면 `Validate` 통과). 글자는 프리팹 TMP에 "다음 Run 메인으로 선택". GameObject는 활성, `interactable`은 false로 두고 코드가 켠다.
2. **배치·입력**: Frame 1160×790, 닫기 버튼 중심 (0, −315)·224×70과 겹치지 않게 아래쪽에 둔다. Button Target Graphic·Raycast Target 연결, TMP는 Raycast Target 끔, Navigation은 기존 버튼처럼 유지, `OnClick` 목록은 비움(`UIInputBinding`은 C#이 붙임).
3. **선택 표시**: `CardTemplate` 아래에 표시 전용 자식(`SelectedMark`)을 만들고 템플릿의 `LobbyDeckListItemView.selectedMark`에 그 GameObject를 연결한다. 항목 루트는 안 됨. 이름·아이콘·입력 버튼과 별개, 처음 비활성, 그래픽 Raycast Target 끔. 템플릿은 계속 비활성. 런타임 복제본은 만들지 않는다.
4. **씬**: `Lobby.unity` 인스턴스·`LobbyCanvasView.deckUI` 연결은 그대로. 등록 루트 활성, Panel 닫힘, Details·CardTemplate·선택 표시 비활성으로 시작.
5. **필드 이름**: 새 칸 이름은 `selectButton`(LobbyDeckUI), `selectedMark`(LobbyDeckListItemView). 다른 필드나 UnityEvent로 우회 연결하지 않는다.
6. **옛 안내 끄기**: `Panel/Frame/Empty`(문구 "현재 메인 캐릭터는 Akimbo로 고정되어 있습니다.")를 비활성으로 둔다(지우지 않음).

커밋 주의: C# 필수 칸(c.1·c.3)과 이 프리팹 연결은 한 커밋.

#### 8.3.2 로비 서브 편집 논의 (2026-11-16, 코드 변경 없음)

- 사용자: 로그라이트라 서브 유닛이 바뀔 수 있으니 로비 팝업도 덱 편집이 필요하다고 봄.
- 확인한 사실: Run 도중 서브 획득은 Core `ICharacterRosterService.AcquireSub`가 있고, 현재 Run 덱 편집은 StageSelect `DeckEditUI`가 한다. Run 시작 전 시작 서브를 고르는 Core 입력은 없다(`StartNewRun(loadout)`·`StartNewRun(loadout, main)`뿐, 시작 서브는 로드아웃 고정). 로비 덱의 뜻(LC0)·덱 후보/확정(LC2)은 로비 설계 §10에서 미합의 그대로다.
- 결정(사용자 "메인 먼저, Core 요청은 하지말고"): 이번 작업은 메인 선택만 끝낸다. 로비 시작 서브 편집의 Core 요청 문서는 쓰지 않는다. 로비 팝업을 편성 화면(메인 + 서브)으로 넓히는 일은 나중에 따로 정한다.

#### 8.3.3 프리팹 연결 (2026-11-16, 사용자 "Codex 돌려줘", Codex가 YAML 직접 수정, Unity에서 열어 보지 않음)

- 첫 실행은 사용자 지시("UI의 규격을 맞춰야 합니다 무조건")로 멈추고, 쓰다 만 프리팹을 HEAD로 되돌린 뒤(작업 전 변경 없음 확인) 조건을 넣어 다시 돌렸다: 시각 값은 새로 정하지 않고 기존 UI 값만 쓰며, 값마다 출처를 적고, 기준이 없으면 "기준 없음"으로 보고.
- 바뀐 파일: `Assets/ProjectL/Prefabs/UI/Lobby/LobbyDeckPanel.prefab` 하나(씬·C#·.meta 무변경).
  - `Panel/Frame/SelectButton`(+ `Label`) 추가 → `LobbyDeckUI.selectButton`. 활성, `m_Interactable: 0`, Target Graphic = 자기 Image, OnClick 비움, TMP Raycast 끔. 문구 "다음 Run 메인으로 선택".
  - `CardTemplate/SelectedMark` 추가(아이콘 뒤, 처음 비활성, Raycast 끔) → `LobbyDeckListItemView.selectedMark`.
  - `Panel/Frame/Empty` 비활성(지우지 않음).
- 값과 출처(Codex 보고, Claude가 YAML에서 필드 연결·부모/자식·자리 값을 대조):

| 값 | 출처 |
|---|---|
| 버튼 348×84, Label 332×68, 글자 32·Auto Size 24~32 | `StageEventOfferCard.prefab` PurchaseButton(30pt 기준 문구 폭 약 313 > 닫기 버튼 폭 224라 닫기 크기를 못 씀) |
| 버튼 Image 색·Sprite 없음, ColorBlock·SpriteState·Navigation, 폰트·글자색·정렬 | 같은 프리팹 `CloseButton`·`CloseButton/Label` |
| 앵커 중앙, y −315 | 같은 프리팹 `CloseButton` |
| 선택 버튼 왼쪽·닫기 오른쪽, 가장자리 간격 20 → x −306 | `GlobalUICanvas.prefab` ConfirmPannel Y·N 버튼 간격 |
| 표시 200×160·색·Sliced 스프라이트 | `Deck/OwnedUnitSlot.prefab` Outline(덱 교체 강조) |
| 표시 배율 0.5(실제 100×80) | `GlobalUICanvas.prefab` NoticePannel 배율 |
| 표시 위치 (−211, 0) | 같은 프리팹 `CardTemplate/Icon`(68×68) |

- 기준 없음: 로비 메인 선택 전용 표시가 없어 가장 가까운 덱 교체 강조(`OwnedUnitSlot/Outline`)를 재사용.
- Claude 확인: 두 필드 연결 fileID가 새 Button·GameObject를 가리킴, `SelectButton`은 Frame `m_Children`에·`SelectedMark`는 CardTemplate `m_Children` 맨 앞(아이콘 뒤에 그려짐), Empty `m_IsActive: 0`, 기존 오브젝트 값 무변경(diff는 자식 목록 두 줄·필드 두 줄·Empty 한 줄 + 새 오브젝트). `Lobby.unity` 인스턴스 오버라이드는 Panel 비활성·루트 이름뿐(Codex 확인).
- 사용자 판단이 필요할 수 있는 점: ① 같은 줄의 선택 버튼(높이 84)과 닫기 버튼(높이 70)의 높이가 다르다(닫기 크기에 문구가 안 들어가 다른 팝업 버튼 규격을 가져온 결과) ② 표시 배율 0.5는 다른 패널의 배율을 빌린 값이다. Unity 화면에서 보고 정한다.
- Unity 확인 Uc 문제 없음(2026-11-16 사용자 "기능은 되는거니깐 Uc OK"). 버튼 높이 차이·표시 배율은 사용자가 따로 지적하지 않음.
- 관찰(보고만): 로즈(Sniper, 키 12)로 출발하면 전투에 로즈가 나오지만 크기가 크다. 원인은 `UnitVisual_Sniper`가 옛 `Prefabs/Characters/Colored/Playerable/Sniper.prefab`(옛 `Colored/Models/Playerble/Sniper.fbx`, 모델 배율 1.5)을 가리키기 때문이다. 다른 아군은 모두 `New/Playerable` 프리팹이고 New 폴더에 Sniper 모델이 없다. 사용자 결정 "그냥 쓰는걸로" — 지금 그대로 둔다(이번 작업 범위 밖).

### 8.4 LM-d 문서 (2026-11-16, 사용자 "LM-d 진행", 코드 변경 없음)

| Task | 결과 |
|---|---|
| d.1 | 설계 §12 구현 메모: 설계와 다르게 된 곳 다섯, §10 정적 대조 항목 다섯 모두 성립(근거 표), 남은 것·관찰. 설계 제목의 "(초안)"을 떼고 상태 줄에 구현 완료 |
| d.2 | 받은 인계 문서에 §8 "View/LifeCycle 대응"(담당 지정 맵·바뀐 계약·§6 체크리스트 상태, Core 요청 없음, 미전달) |
| d.3 | 바꾼 줄의 작성자를 `git blame`으로 다시 셈(HEAD 기준, 우리가 지우거나 바꾼 줄만). 다른 작업자 줄은 현강님 한 줄뿐 → 아래 메모. 나머지는 호진 줄(`Leehojin`·`CCRC_Hojin`). `LobbyDeckUI`의 현강님 줄 10개·프리팹의 현강님 변경은 그대로 두고 사이에 넣기만 했다 |
| d.4 | CLAUDE.md "현재 상태"에 한 줄(팀 공유 작업 상태만) |
| d.5 | 묶음 끝 Codex 검수 — 아래 §8.5 |

#### 8.4.1 현강님 메모 (미전달, 기록만)

- `InGameSystem/ViewSystem/Lobby/Deck/LobbyDeckPopupController.cs`의 `IPopupViewController.Show` 안 빈 목록 줄(`_presenter.SetModel(new LobbyDeckDisplayModel(Array.Empty<LobbyDeckCardContent>()))`, 현강님 `b0656cb00` "레이아웃 세부수정 및 전투 진입 시 Deck 자동 열림")을 `_presenter.ShowCandidates()`로 바꿨다. 로비 덱 팝업이 이제 Core 메인 후보(에블린·로즈)를 보이고 "다음 Run 메인으로 선택" 버튼으로 출발 메인을 고른다.
- 덱 팝업 화면 `LobbyDeckUI`·`LobbyDeckListItemView`에 필수 칸 `selectButton`·`selectedMark`가 생겼고, `LobbyDeckPanel.prefab`에 연결했다(선택 버튼·선택 표시, 옛 안내 `Panel/Frame/Empty` 비활성). 이 화면·프리팹을 고칠 때 두 칸을 비우면 로비 진입이 `Validate`에서 멈춘다.

### 8.5 묶음 끝 Codex 검수 (2026-11-16, 읽기 전용, LM-a~LM-d 전체)

판단: 높음·중간 0건, 낮음 2건. 사용자 커밋으로 넘겨도 되는 묶음. 대상 C# 23개(기존 17·새 6)·새 `.meta` 6개(GUID 중복 없음), d.3 작성자 셈 맞음(다른 작업자 줄은 현강님 한 줄), 선택 생성 → 잠김 연결 → 팝업 생성 순서·제출 키 고정·중복/취소/거절에서 추가 결함 없음, 프리팹 참조·계층·초기 상태가 기록과 같음, 혼합 줄바꿈·잘못된 UTF-8 없음, CLAUDE.md 새 줄에 개인 규칙 없음. 컴파일·Unity 재생은 미확인(사용자 Ua·Ub·Uc와 구분).

| 지적 | 심각도 | 판단 | 반영 |
|---|---|---|---|
| 1 계획 머리가 "LM-d 착수 전", 인계 머리가 "View 구현이 남음"으로 완료 기록과 어긋남 | 낮음 | 수용 | 계획 상태 줄 갱신. 인계는 천명님 상태 줄을 고치지 않고 바로 아래에 "Core 인계 당시 기록, View 쪽 완료 — §8" 한 줄을 더함 |
| 2 "외형 실패 때 이전 연결을 그대로 둔다"는 표현이 오해를 줌(예외가 오류 경계에 닿으면 기존 연결도 정리됨) | 낮음 | 수용 | `LobbyDeckPopupFactory` 생성자 주석·설계 LM2 문장을 "불완전한 새 연결로 바꿔 끼우지 않고 예외, 오류 경계에 닿으면 기존 연결도 정리"로(코드 동작 무변경) |

- 문서 줄바꿈: 이번 `sed -i`가 문서 넷(CLAUDE.md·설계·계획·인계)을 LF로 바꿔 둔 것을 CRLF로 되돌렸다(저장소는 `core.autocrlf=true`라 내용 차이는 없음). C# 파일은 모두 CRLF 그대로.
- 남은 것: 사용자 커밋. 묶음: C# 23개 + 새 `.meta` 6개 + `LobbyDeckPanel.prefab`(C# 필수 칸 `selectButton`·`selectedMark`와 반드시 한 커밋) + 문서 넷. `FMODStudioCache.asset`·`SCDream7 SDF.asset`·`New Fmod Event Catalog.asset`은 에디터가 바꾼 것이라 이 묶음이 아니다.
