# 스테이지 선택 리팩토링 설계

- 작성자: 호진(이호진)
- 상태: 설계 2판, 결정 완료(2026-11-09). Q-SR7·Q-SR8은 사용자 결정(구도 도구 지움, Core 요청 대신 지금 할 수 있는 해법), 나머지 Q-SR1~Q-SR6·Q-SR9~Q-SR12는 사용자 "권장대로" = 모두 A. Task 계획: [스테이지 선택 리팩토링 계획](../plans/2026-11-09-stage-select-refactor-plan-Hojin.md). 코드는 아직 바꾸지 않았다.
- 근거: [스테이지 선택 View 평가 보고서](2026-11-05-stage-select-view-evaluation-Hojin.md)·[문제 목록](2026-11-05-stage-select-view-evaluation-issues-Hojin.md). 기준 판 `ecfd52b7b`, 13기준 평균 83.3, 중간 14·낮음 102.
- 대신하는 문서: [스테이지 맵 동작 위험 정리 설계](2026-11-04-stage-map-behavior-risk-design-Hojin.md)(R1~R4, BR1~BR4, Q-BR1~Q-BR4). 그 해법은 이 문서 §2·§3에 옮겼고, 질문은 §4의 Q-SR1~Q-SR4로 다시 묻는다.
- 문제 ID(C-xx·N-xx)는 문제 목록의 ID다. 위치·실패 시나리오는 그 문서를 본다. 이 문서의 줄 번호는 HEAD `c995dd455` 기준이다.

## 0 평가 뒤 바뀐 코드 (11-09 다시 확인)

- 기준 판 뒤 범위 파일 27개가 바뀌었다(+1434 −143줄). 맵 쪽 주요 커밋은 셋이다.
  - `42db0ba33` 호버 아웃라인
  - `3f7dd9c75` 진입 가능 노드 아웃라인
  - `b7d55e449` 맵 진입 연출과 노드 선택 카메라 동행
  - 이 밖에 `adc025575`(현강님 InfoPanel UI 변경)가 `DeckSlotInfoStats`를 바꿨다.
- 읽기 전용 검토자 셋이 지금 코드로 중간 14개를 다시 봤다.
  - **14개 모두 그대로 있다.** 11-04 해법 BR1~BR4도 하나도 들어가지 않았다.
  - 증상이 커진 것: C-05·C-10. 바쁨·대기 조건이 새 `CanInteractWithMap`을 막아, 걸리면 노드 클릭뿐 아니라 호버·끌기·휠까지 죽는다. C-10은 출전 부품이 카메라 정렬 작업을 놓지 않아 카메라도 자동 모드에 묶인다.
  - 커진 것: C-02(카메라 동행 줄도 두 곳에 사본), C-19(묶음 칸 37 → 43), C-22(`#if UNITY_EDITOR` 갈래 17 → 23곳).
  - 바뀐 것: C-01. 판정이 공통 바탕 `CanInteractWithMap` 위로 모여 "판정마다 다른 부분집합" 차이는 거의 사라졌다. 남은 것은 팝업 이름 나열·공통 계약 없음·중복 재나열이다.
  - 풀린 것: 낮음 C-17(덱 열기 조건 누락). 바탕 조건이 처리 전 요청을 본다.
- 평가가 놓친 사실 셋.
  - 덱 편집의 **다시 평가 버튼이 씬에서 꺼져 있고 켜는 코드가 없다**(`StageSelect.unity` m_IsActive 0, 기준 판도 같음). Stale·NotReady면 편집이 막히는데 탈출구가 안 보인다. C-06과 함께 다룬다.
  - C-09의 "Core가 Boss·None 병과를 막지 않음"은 틀렸다. Core 지원 정의 검사가 막는다. Core가 막지 않는 것은 **같은 서브 병과 둘**이다(보유 한도 병과당 2).
  - `Prefabs/UI/Deck/DeckEditUI.prefab`은 런타임 참조가 없다. 현강님 시험 `DeckMainChangePolicyTests`만 경로로 읽는다. 지우지 않는다.
- 새로 커진 책임: `StageMapViewScope`가 311 → 616줄이다. 진입 준비 상태 기계·인트로 Tick·카메라 동행 묶기·비전투 복원 대기·에디터 구도 접근을 새로 맡았다. 재평가에서 C2 중간이 될 수 있다(Q-SR11). 에디터 구도 접근은 구도 도구를 지우면서 빠진다(§2.12).

## 1 원칙

- **범위(평가 Q-SE1 B):** 평가 범위 160개 파일 전부다. 작성자와 상관없다. 다른 사람 줄을 고치면 변경을 정리해 그 사람에게 넘긴다.
- **현강님 UI(평가 설계 §2.3):** 현강님만 쓴 UI 파일 10개는 결함과 우리 호출 맞춤만 고친다. 그 밖의 낮음은 남긴다(§5).
  - 이 설계에서 현강님 줄에 닿는 곳은 셋이다. 모두 결함·규약 수정이다.
    - `DeckEditUI`(여러 작성자): 다시 평가 버튼 켜기(C-06), 상태를 바꾸지 않는 `Validate()` 떼어내기(C-26)
    - `DeckSlotInfoAbilities`(현강 UI): 넷째 이후 특성 슬롯 순서 결함(N-48)
    - 덱 칸 구조(`DeckEditSlotLayout`, 현강 줄)는 바꾸지 않는다. C-09는 Builder·Draft 쪽에서 푼다(§2.14).
- **보호 영역은 그대로다.** Framework·GlobalSystem 내부·Core·`ServiceKeys` 계약은 바꾸지 않는다. Factory는 연결 줄만 바꾸고 민형님 공유 문서에 적는다.
- **Core 요청은 하지 않는다(2026-11-09 사용자).** Core가 있어야 깔끔해지는 셋(C-04·C-09·C-13)도 지금 우리 영역에서 할 수 있는 최선으로 푼다(§2.11·§2.13·§2.14). Core 파일은 고치지 않는다.
- **설계 기준:** 새 참조·검사·매 Tick 계산은 꼭 필요할 때만 넣는다. 이미 있는 공용 기능(`IUITextPresentation.ShowNotice` 등)을 먼저 쓴다. 가장 단순한 안을 먼저 권한다. 새 View 타입 이름에는 View를 넣는다.
- **순서:** 결함(C10·C9) → 구조 → SSOT → 정리 → 성능 → 재평가. 앞 묶음일수록 실제 버그에 가깝다.
- 새 시험 코드와 MSBuild는 없다. 보고에는 "컴파일 미확인"을 쓴다. Phase마다 Codex 검수를 받고, 사용자 Unity 확인을 둔다.

## 2 중간 14개 설계

### 2.1 C-05 Stage 도메인 정지는 오류 경계로 (11-04 BR2)

- **문제:** 진입·상점·회복 도메인이 멈추면 `StageLifeCycle.ReportStoppedDomain`(`:216`)이 `Debug.LogError`만 한다. 증강만 팝업이 `IsGrowthStopped`를 읽어 F1로 올린다.
- **설계(Q-SR2 A):**
  - `StageLifeCycle`이 `StageProgress`에 넘기는 보고를 `_faults.Report(new InvalidOperationException(reason))`로 바꾼다. 필드 초기화(`:32`)를 생성자 안으로 옮겨 `_faults`를 쓴다.
  - 확인 Job의 기록 누락과 전투 진입 시도가 깨진 경우(`StagePlan.Next` `:128-130`)가 같은 보고를 지난다.
  - 증강 팝업의 정지 갈래(`TraitGrowthPopupController.cs:95-96`)와 계약 멤버 `IStageTraitGrowthFacts.IsGrowthStopped`, 구현 한 줄(`StageLifeCycle.cs:213`)을 걷는다. 오류 상태면 `CanWork`가 먼저 거짓이라 닿지 않는다.
  - 도메인 정지 깃발과 `IStageDeckEditFacts.IsFlowStopped`(용근님 계약)는 그대로 둔다. 같은 Tick의 다음 등록을 막는 데 쓴다.
  - 주석 넷을 고친다: `IStageProgress.cs:26`, `StagePlan.cs:51`, `StageCheckJob.cs:8`, `StageLifeCycle.cs:215`.
  - N-64(Broken 진입 시도가 남아 씬 이탈 검사가 예외)는 F1 뒤의 일이 된다. 정지 뒤 이탈 검사가 F1 상태를 먼저 보는지 구현 때 확인한다.
- **바뀌는 동작:** 지금은 로그만 남고 맵이 잠긴다. 바꾸면 F1로 멈춘다(FW 계약). 로비 아티팩트는 그대로 둔다(1차 D11).

### 2.2 C-10 출전은 덱 창이 닫힌 사실로 스스로 맞춘다 (11-04 BR1)

- **문제:** 출전 확정 뒤 두루마리 닫기가 밖에서 끊기면 닫기 후속이 버려진다(`StagePopupViewLifetime.cs:96-111`). 취소 콜백은 이미 비워져 있다(`DeckEditPopupController.cs:221-222`). 출전 부품이 바쁜 채로 남는다.
- **설계(Q-SR1 A):**
  - `StageCombatDeparture.Tick`의 제출 갈래(`:136-141`) 뒤, `if (_arrived) return;`(`:142`) 앞에 한 줄을 더한다. 도착했고 제출하지 않았는데 덱 창이 닫혀 있으면(`!_deck.IsOpen`) 취소한다.
  - 정상 출전은 영향이 없다. `CloseImmediately`가 화면을 닫은 직후 같은 호출에서 `Submit`을 부르므로 다음 Tick에는 제출 번호가 있다.
  - 취소는 지금 있는 `Cancel()` 경로를 그대로 쓴다. 전투 착지 확정을 게시하기 전(`!_landingPublished`)이면 카메라도 출발 위치로 되돌리고 자동 모드를 푼다(`StageCombatDeparture.cs:198`).
  - 수명 부품은 바꾸지 않는다. 출전 쪽은 이미 있는 `IsOpen`을 읽기만 한다. 비용은 출전 중 bool 하나다.
- **같이 고칠 덱 편집 출전 줄 둘(Q-SR10 A):** 11-04에는 "담당 아님"으로 두었다. 평가 범위가 넓어져(Q-SE1 B) 이번에 넣는 것을 권한다.
  - N-33: `_departureRun`을 `Show`에서 늘 다시 정한다. Basis가 없으면 비운다.
  - N-11: `IDeckCombatDisplay`를 조용한 `as` 캐스트 대신 Factory가 넘기는 필수 인자로 받는다.

### 2.3 C-15 불일치로 닫으면 선택 화면으로 돌아오고, 노드 진입은 선택 화면이 열려 있을 때만

- **문제:**
  - 상점은 컨트롤러가 닫는 경로(`CloseAndRefresh`)마다 `_skipNextRestore`를 건다(`ShopPopupController.cs:382`). 밖에서 숨겨진 경우(`:138`)는 걸지 않는다. 불일치 닫기는 `returnToSelection = false`라 선택 화면도 안 돌아온다.
  - 회복은 표시 중 숨겨질 때마다 `_hiddenContext`를 걸어(`RecoveryPopupController.cs:104`) 같은 방문을 다시 열지 못한다. 상태 바뀜 닫기(`:123`)·불일치 닫기(`:185`)는 맵 다시 그리기를 부르지 않아 복원 예약(`StageMapViewScope.cs:585`의 `_restoreMap`)도 생기지 않는다.
  - 노드 진입 판정이 선택 화면이 열렸는지 보지 않는다. 그래서 전투 노드를 누르면 덱 편집의 부모(StageSelect)가 닫힌 채 `OpenChildUI`가 예외 → F1.
- **확인한 사실:**
  - 완료로 끝난 방문은 복원이 이미 거른다. 상점 `TryCaptureContext`는 클리어된 노드를 받지 않고(`:228`), 회복 `TryRestore`는 `state.IsCompleted`를 본다(`:76`). 그래서 상점의 1회 억제 깃발은 완료 닫기에는 필요 없고, 불일치 닫기에서만 효과가 있다. 그 효과가 이 결함이다.
  - 회복의 나가기(`Exit`)는 완료 제출이다. 플레이어가 완료하지 않고 닫는 길은 없다.
  - `Hide(bool explicitClose)`는 "플레이어가 닫음"이 아니다. 컨트롤러가 부른 `Close()`도 참이다(`PopupViewScope.cs:93`·`:253`).
- **설계(Q-SR5 A):**
  - 상점 `_skipNextRestore`를 걷는다(N-22). 완료 방문은 클리어 사실로 걸러진다. 억제할 방문이 다시 필요해지면 깃발 대신 방문 식별 값에 묶는다.
  - 회복 `_hiddenContext`는 **밖에서 숨겨진 경우에만** 건다. 컨트롤러가 스스로 닫을 때는 닫는 이유를 자기 필드로 먼저 정해 두고, `Hide`에서 그 값을 보고 억제 여부를 고른다(`explicitClose` 인자는 쓰지 않음).
  - 불일치 닫기는 선택 화면으로 돌아온다(`returnToSelection = true`). 회복도 상점처럼 닫힌 뒤 맵 다시 그리기(`_mapChanged`)를 불러 복원을 예약한다. 다음 복원 때 Buffer의 지금 방문 상태로 다시 연다. 방문이 끝났으면 아무것도 열지 않는다.
  - 닫고 다시 여는 고리를 막으려고, 다시 여는 조건(`TryRestore`)과 표시 중 닫는 조건(Tick의 불일치 판정)이 같은 사실을 보게 맞춘다. 다시 열어도 자동 제출은 없다.
  - 노드 진입(누름·뗌 제출·출전 가로채기)은 "맵 한가함 + 선택 화면 열림"일 때만 받는다(§2.6). F1 경로가 닿지 않는다.
  - 상점 완료가 끝내 거절되는 경우(완료 거절 + 허용 불가)는 다시 열려도 완료할 수 없다. 이것은 지금도 Run이 막히는 Core 쪽 상황이라 이 설계는 화면에 갇히지 않게만 한다.
- **안내(Q-SR4 A):** 불일치로 닫힐 때 플레이어용 안내는 넣지 않는다. 바깥에서 상태가 바뀐 경우에만 닿는 갈래라 진단 로그로 원인을 찾는다. 선택 화면이 돌아오므로 갇히지 않는다.

### 2.4 C-18 맵 선택 입력 콜백의 예외를 오류 경계로

- **문제:** InputSystem 콜백(Tick 밖) → 컨트롤러 → 슬롯 → `StageMapInteraction` 경로에 오류 경계 감싸개가 없다. `InterceptEntry`·`BlocksEntryForGrowth`·에디터 접근만 자체 try가 있다.
- **설계:**
  - 새 `ViewSystem/StageSelectionInput/FaultReportingStageSelectionReceiver`(`IStageSelectionInputReceiver` 감싸개)를 둔다. 로비 `FaultReportingLobbyReceiver`와 같은 꼴이다. 이미 정지했으면 부르지 않고, 예외는 경계에 보고한다.
  - `StageMapViewInput`이 경계를 받아 `Interaction`을 감싸 슬롯에 건다(`Connect` `:64`). Bind/Unbind는 같은 감싸개 하나로 한다.
  - 감싼 뒤 `InterceptEntry`·`BlocksEntryForGrowth`의 자체 try는 걷는다(판단 원본 하나).
  - 에디터 구도 수신자는 §2.12에서 먼저 지운다. 그래서 감싸개는 `IStageSelectionInputReceiver` 하나만 구현하면 되고, 슬롯의 에디터 캐스트를 신경 쓰지 않는다.
- **바뀌는 동작:** 입력 콜백 예외가 콘솔 한 줄이 아니라 F1이 된다.

### 2.5 C-06 덱 편집의 거절·편집 불가 이유를 보이고, 다시 평가 길을 연다

- **문제:** `DeckEditText.Status`·`Categories`를 만들지만 아무도 그리지 않는다. 글 칸은 11-02 레이아웃 수정(`4b3b1cc45`)에서 빠졌다. Stale·NotReady면 편집이 막히는데 다시 평가 버튼이 씬에서 꺼져 있다. 출전 모드에서는 닫기 버튼도 숨긴다.
- **설계(Q-SR6 A):**
  - 거절·편집 불가·낡음 이유(§2.14의 칸 밖 덱 포함)는 공용 안내 `IUITextPresentation.ShowNotice`로 바뀔 때 한 번 띄운다. 새 글 칸·씬 편집이 없다. 안내 서비스는 덱 Factory가 Global Context 경로로 받는다(로비와 같음).
  - 정상 글(확정 등)과 `Categories` 글은 걷는다. 병과는 칸 자체가 보인다. `DeckEditText`에는 안내 문구만 남긴다(용근님 줄).
  - 다시 평가 버튼은 편집이 막힌 이유가 "다시 평가하면 풀리는 것"(Stale·NotReady)일 때만 켠다. `DeckEditUI`에 켜고 끄는 입구 하나를 둔다(현강님 UI 결함 수정).
  - Builder 주석 "Core의 거절 사유는 Status에 남깁니다"(`:36`)를 고친다.
- 다른 안: 화면에 상태 글 칸을 다시 둔다(씬·프리팹 편집, 현강님 UI 레이아웃 변경). 또는 글을 걷기만 한다(이유가 여전히 안 보임).

### 2.6 C-01 팝업 공통 계약과 맵 한가함 판정 하나

- **문제:** 팝업 넷이 공통 계약 없이 Popups·Scope·Factory·Services·System·화면 루트에 나열된다. Scope 판정은 바탕 위에 이미 든 조건을 다시 나열한다(`CanOpenDeckEdit` `:328`, `OpenTraitGrowthWhenIdle` `:337-344`, `CanUseDevelopmentTools` `:91`). `CancelReturnWhileBusy`(`:268`)는 `HasPendingRequest`를 인라인으로 베낀다. 설명 주석(`:324-336`)은 사라진 차이를 말한다.
- **설계:**
  - 새 좁은 계약 `IStageMapViewPopup`(`IsOpen`·`IsWaiting`·`Tick()`·`IDisposable`)을 팝업 컨트롤러 넷이 구현한다. 결과 대기가 없는 덱·증강은 `IsWaiting`이 늘 거짓이다.
  - `StageMapViewPopups`는 만든 순서대로 목록에 넣고 Tick·Dispose를 목록으로 돈다. `IsAnyOpen`·`IsAnyWaiting`을 낸다. 만들기는 Factory 서명이 달라 지금처럼 하나씩 둔다. 구체 타입 속성 넷은 남긴다(복원·증강 열기·출전이 쓴다).
  - Scope 판정은 둘로 정리한다.
    - **맵 한가함** `IsMapIdle`: 지금 `CanInteractWithMap` 내용 + `!_popups.IsAnyOpen && !_popups.IsAnyWaiting`. 선택 화면 표시·노드 호버·끌기·휠·덱 열기·증강 열기·개발 도구·복원이 모두 이것만 본다.
    - **노드 진입 가능** `CanEnterNode`: `IsMapIdle` + 선택 화면이 열려 있음(§2.3). 누름·뗌 제출·출전 가로채기만 본다.
    - `StageMapInteraction`은 지금 `_canInteractWithNodes` 하나를 호버(`:83`·`:199`)와 진입에 함께 쓴다. 호버는 `IsMapIdle`, 진입은 `CanEnterNode`로 나눠 넘긴다.
    - 선택 화면은 `IsMapIdle`이 되면 다음 Tick에 열린다(`StageMapSelectionPresentation.cs:87`). 그래서 첫 표시와 팝업에서 돌아올 때 선택 화면이 열리기 전 짧은 동안은 노드 클릭을 받지 않는다. 의도한 입력 제한이다. 선택 화면 표시는 `IsMapIdle`만 보므로 서로 기다리는 고리는 없다.
  - 각 판정에는 자기 고유 조건만 적는다(복원은 `_selection.CanOpenScreens`, 증강은 `TryOpen` 안의 보일 것). 다시 나열한 조건·`CanInteractWithNodes` 별명·낡은 주석을 걷는다.
  - 인트로를 두 객체로 두 번 보는 것(`_intro.IsPrepared && !_introPolicy.BlocksInteraction`)은 이름 있는 판정 하나로 묶는다. 인트로 차단 정책 객체는 인트로 설계대로 바꿔 끼울 수 있는 임시 정책으로 남긴다.
  - `TickHover` 셋째 인자(`:195-196`)도 이름 있는 판정으로 둔다.
  - N-06(맵·상점 입력 Buffer 비우기 조건 두 벌)도 여기서 주인을 하나로 정한다.
- **팝업 하나를 더할 때:** 컨트롤러·Factory, Popups 만들기 한 덩이, SceneFactory, 팝업 묶음(§2.9), 화면 루트. Scope 판정은 바뀌지 않는다.
- **바뀌는 동작:** 노드 진입에 "선택 화면 열림"이 더해진다(위 입력 제한). 호버를 포함한 나머지 판정은 지금 실제 조건과 같다(재확인).

### 2.7 C-12 요청 수명 공통 부품과 "확인 안 한 결과" 식 하나

- **문제:** 회복·증강 요청 소유자(`RecoveryRequestLifetime` 259줄·`TraitGrowthRequestLifetime` 171줄)가 예약·실행 시작/끝·정지·종료가 거의 같은 사본이다. Job 둘도 사본이다. View 안에 결과 번호 비교(Handled > Acknowledged)가 다섯 곳이다(증강 컨트롤러 `:66`, 증강 소유자 `:45`·`:136`, 회복 소유자 `:107`·`:199`).
- **설계(Q-SR9 A):**
  - 공통 부품 `ViewRequestExecution`(조합, 상속 아님)을 둔다. 보관 번호·실행 예약·`TryBeginExecution`·`NotifyExecutionFinished` 앞부분·`RequireAvailable`·`Fail`·`Shutdown`(Runner Clear)을 맡는다.
  - 차이는 두 소유자에 남긴다: 예약 조건, 결과 확인 방식(회복은 스스로 ack·입력 제거, 증강은 화면이 ack), 회복만의 취소·입력 정지.
  - Job 둘의 공통 몸통도 한 꼴로 둔다(어떻게 묶을지는 계획에서 정함).
  - 결과 번호 비교는 소유자마다 이름 있는 속성 하나로 둔다(증강 `HasUnacknowledgedResult`, 회복도 같은 꼴). 증강 컨트롤러 `:66`은 소유자의 속성을 읽는다(컨트롤러는 이미 소유자를 받는다).
  - `StageTraitGrowthProgress`(`:37-38`)는 번호를 비교하지 않고 Core 진행 상태(`ChoiceRequired`·`AwaitingAcknowledgement`)를 읽는다. Core는 Run·전투 조건을 먼저 보고(`Unavailable`) 그 뒤 번호를 본다. 그래서 이것은 바꾸지 않는다. 증강 컨트롤러 `IsPending`의 Core 상태 읽기도 그대로다.
  - Buffer·Core 쪽 식은 기반이라 그대로다.
  - 상점은 씬 수명 컨트롤러가 직접 제출하는 지금 꼴을 둔다(씬 이탈 = 취소가 의도된 정책). 그 이유를 주석에 적는다.
- 생성은 `InGameViewSystemFactory.cs:75-79`(연결 줄)이다. 서비스 키는 없다. 생성자 서명이 바뀌면 민형님 공유 문서에 적는다.

### 2.8 C-02 마커 점프 이동 부품 하나

- **문제:** `StageMapNodeMovement`와 `StageCombatDeparture`가 마커 이동 시작·Tick·완료·소리와 카메라 동행 진행률·문맥 무효화를 같은 줄로 갖는다. 시간 비율 12/59·35/59에 이름이 없다.
- **설계:**
  - 새 `StageMapViewMarkerJump`가 시작(노드 찾기·로컬 좌표·이동 설정·지연 감싸개·소리), 진행(카메라 진행률·유한값 검사), 완료(정리 순서), 문맥 확인을 맡는다. 비율은 이름 있는 상수다.
  - 두 부품은 이것을 하나씩 갖고 차이만 자기 쪽에 둔다.
    - 카메라 확정 시점: 일반 이동은 착지 때, 출전은 출전 수락 때.
    - 취소 때 카메라 되돌리기: 출전만 되돌린다. 조건은 지금처럼 "전투 착지 확정 게시 전의 유효한 취소"(`!_landingPublished`)이고, 문맥 무효화·폐기는 되돌리지 않는다.
    - 정리 범위: 출전은 덱 창 닫기·현재 노드 다시 놓기까지.
  - 정렬 부품 `StageMapCameraFocus`는 하나를 둘이 같이 쓰는 지금 구조 그대로다.

### 2.9 C-19 서비스 묶음을 쓰는 쪽별로

- **문제:** `StageMapViewServices`(생성자 인자 43개)를 Scene·Entry·Input·Scope·SceneFactory가 통째로 받는다. 각자 쓰는 칸은 6~12개다.
- **설계:** 로비 2차(R2-c)와 같이 쓰는 쪽별 묶음으로 나눈다.
  - 씬 표시(6), 노드 진입(9), 맵 입력(7), 팝업 조립(SceneFactory용), Scope 자신이 쓰는 칸.
  - `StageMapViewSystem`이 묶음들을 만든다. 큰 묶음은 걷는다.
  - 연결 사이에 남는 상태 객체 셋(`StageNodeAvailabilityReader`·`StageMapIntroHistory`·`StageNodeDropEffects`)은 System이 갖고, 필요한 묶음에만 넣는다.
  - N-03(Scene·Input이 안쪽 부품 공개, `Entry.Departure` 사후 setter)도 함께 줄인다. `Departure`는 Entry 생성 인자로 받는다.
  - N-07(11-04 BR4): 맵 입력의 운영 의존(`cameraDrag`·`pointerBlocker`·`blocksEntry`·`interceptEntry`)을 필수로 받고 null 갈래를 걷는다.
- `StageMapSceneFactory`(Factory) 서명이 바뀐다. 연결 줄이라 민형님 공유 문서에 적는다.

### 2.10 C-26 덱 편집 화면 구성 검사를 씬 연결 때로

- **문제:** 상점·회복·증강 화면은 씬 연결 때 `Validate()`로 안쪽까지 검사된다. 덱 편집은 부모·Canvas만 보고, 안쪽 검사는 `Awake→Prepare`·첫 Open·첫 출전에서 돈다. 빠지면 F1 밖 콘솔 오류 뒤 버튼이 조용히 죽는다.
- **설계:**
  - `DeckEditUI`에 상태를 바꾸지 않는 `Validate()`를 둔다. `Prepare`의 검사부(`:322-343`·`ValidateOwnedList`)와 출전 버튼·확인창 위치, 보유 슬롯 프리팹 검사를 모은다. `Prepare`는 그것을 부른 뒤 바인딩만 한다.
  - `DeckCombatConfirmUI`에도 `Validate()`(버튼 둘)를 둔다.
  - `StageSelectPanelUI.ValidateComposition`이 `deckScreen.Validate()`를 부른다. 상점·회복의 `StageOfferScreenUI.Validate()` 꼴과 같다.
  - 출전 버튼·확인창은 출전에서 늘 쓰므로 필수로 본다(지금 `:148`·`:351`은 null 허용, 출전 때는 필수라 어긋남).

### 2.11 C-13 회복 팝업은 바뀐 때만 Core 투영 (View만)

- **문제:** 열린 동안 매 Tick Core 회복 상태 전체를 투영한다(배열·스냅숏 할당). 결과 프레임에는 두 번이다. 쓰는 값은 유효·문맥·완료뿐이다.
- **확인:**
  - Core 투영은 Run 진행 중·전투 없음·선택 있음을 먼저 보고(`RecoveryStateProjection.cs:25`), 완료는 맵 클리어 또는 노드 클리어다(`:105`). View가 Buffer 값 몇 개로 이 판정을 대신하면 Core 조건을 손으로 베끼게 된다. 그래서 판정은 계속 Core 투영이 한다.
  - 회복 Buffer 값 `CurrentRecoveryData`는 바꿀 수 없는 객체이고, Core가 회복 상태를 바꿀 때마다 새 객체로 갈아 끼운다(`CurrentProgressBuffer.cs:67`).
  - 회복 팝업이 열린 동안은 맵 입력과 다른 팝업이 막혀 있다. 이때 회복 상태(체력·골드·클리어 포함)를 바꾸는 길은 우리 회복 요청의 결과뿐이다.
- **설계(사용자 11-09 "Core 요청 대신 지금 할 수 있는 최선"):**
  - Core 투영은 **열 때·결과를 받을 때·현재 회복 객체가 바뀐 때**만 한다. 매 Tick은 `_current.Recovery` 참조 하나만 견준다(할당 없음).
  - 판정 자체(유효·완료)는 투영 결과를 그대로 쓴다. View는 "다시 물어볼 때"만 정하고 Core 조건은 베끼지 않는다.
  - 결과 프레임의 두 번 읽기는 첫 읽기 값을 다시 써서 하나로 줄인다.
  - 위 "회복 결과 밖에서 바뀌는 길이 없다"는 전제를 클래스 주석에 적는다. 나중에 회복 팝업 위에서 다른 기능이 같은 사실을 바꾸게 되면 그 기능이 다시 그리기를 요청해야 한다.
  - 빌더 주석 "열기와 요청 완료 때만"(`RecoveryPopupModelBuilder.cs:31`)과 동작이 맞게 된다.

### 2.12 C-22 에디터 카메라 구도 도구를 지운다

- **사용자 결정(11-09):** 스테이지 선택의 그래픽용 카메라 구도 수정 도구는 이제 필요 없다. 떼어 내지 않고 지운다.
- **지금 구조:** 구도 도구는 Play 중 Alt로 맵 카메라를 돌리고·당기고·줌한 뒤 확정하면, 편집 모드로 돌아갈 때 그 값을 씬의 맵 카메라에 써 준다(사용자가 씬 저장). 빌드는 씬에 저장된 맵 카메라 자세만 쓴다. 그래서 도구를 지워도 이미 저장한 구도는 그대로다.
- **지우는 것:**
  - `ViewSystem/StageMap/Editor/` 파일 다섯(+ `.meta`): `IStageMapCameraFramingAccess`, `StageMapCameraFramingPending`, `StageMapCameraFramingReceiver`, `StageMapCameraFramingSettings`, `StageMapSceneRootEditor`
  - 런타임 갈래: `StageMapViewScope`(접근 구현·연결·Tick 미리보기·준비 미리보기·해제), `StageMapCameraDrag`(미리보기 갈래·`_previewSettings`·`_dragging` 우회·`PrepareManualFraming`·`PreviewFraming`, 에디터만 쓰던 `GetCurrentFraming`), `StageMapViewScene`(`CameraFraming`·`PreviewCameraFraming`), `StageMapViewInput`(구도 수신자), `StageMapSceneRoot`(접근 Bind/Unbind·`CameraFramingKey`)
  - 입력: `IStageCameraFramingInputReceiver` 계약, 컨트롤러의 에디터 갈래 셋(Cancel은 빌드와 같은 `Press` 하나), 슬롯의 구도 전달, 이름 표의 `CameraModifier`·`CameraDolly`
- **남기는 것(보고만):** `PlayerInputActions.inputactions`의 `CameraModifier`·`CameraDolly` 액션 둘은 코드가 더는 쓰지 않지만 입력 에셋이라 지우지 않는다. 에디터 `SessionState`에 남은 구도 값도 해가 없어 그대로 둔다.
- **함께 풀리는 것:** C-22, B8 낮음 여섯(N-13·N-19·N-20·N-21·N-35·N-88). `IsSuperseded`·`Dispose` 정리 목록이 짧아지고, 휠은 늘 맵 이동만 한다.
- 구도 도구 기록이 있는 문서([스테이지 맵 설계](2026-10-10-stage-map-design-Hojin.md))에는 "지움" 한 줄을 남긴다.
- 순서: 결함 작업(C-18 감싸개, C-01 판정 정리)이 구도 갈래를 피해 가지 않도록 **SR-a 맨 앞**에서 지운다.

### 2.13 C-04 증강 진행 열쇠 (View만)

- **문제:** `StageTraitGrowthProgress.ProgressKey`가 Core 진행 읽기 원본 8개를 손으로 베낀다. Core 안에도 같은 목록이 `SameSource`(private)로 있다. Core가 원본을 더하면 열쇠가 조용히 낡는다.
- **확인:** 지금 두 목록은 같다. Buffer에 증강 진행 전체를 덮는 버전 값은 없다. Core 진행 읽기는 DB 카탈로그 검사·단계 계산까지 해서 매 프레임 부르기엔 무겁다(맵이 열린 동안 노드 호버가 매 프레임 묻는다).
- **설계(사용자 11-09 "Core 요청 대신 지금 할 수 있는 최선"):**
  - 열쇠 사본은 Core를 고치지 않고는 없앨 수 없다. 그래서 **"조용히 낡음"을 없앤다.**
  - 에디터·개발 빌드(`UNITY_EDITOR || DEVELOPMENT_BUILD`)에서만, 열쇠가 같아 캐시를 쓴 경우 1초에 한 번 Core를 다시 읽어 캐시 결과와 견준다. 다르면 "증강 진행 열쇠가 Core 진행 읽기 원본과 어긋났습니다(원본 `CharacterRosterService.ReadTraitGrowthProgressCore`·`SameSource`)"로 오류 경계에 보고한다.
  - 운영 빌드에는 비용이 없다. Core가 원본을 바꾸면 개발 중 첫 플레이에서 바로 드러난다.
  - 열쇠 주석에 Core 원본 위치 둘을 적는다.
- 재평가에서 사본은 남는다. 다만 "예외 없이 조용히 틀림"이 없어져 낮음으로 볼 가능성이 크다(불확실).

### 2.14 C-09 표시할 수 없는 덱은 F1 대신 편집만 막는다 (View만)

- **문제:** View는 덱을 병과별 고정 네 칸(Main·Heavy·Rider·Cannon)으로만 그린다. Core는 같은 서브 병과 둘을 막지 않는다(보유 한도는 병과당 2). 그런 덱이 들어오면 덱 편집을 여는 순간 `DeckEditModelBuilder.cs:40-42`가 예외 → F1이다.
- **확인:**
  - 덱이 설치되는 길은 새 Run 로드아웃과 덱 편집 확정 둘뿐이다. 덱 편집은 칸 단위로만 놓아서 같은 병과 둘을 만들지 못한다. 그래서 그런 덱은 로드아웃 데이터로만 들어온다.
  - 운영 로드아웃 `PlayerLoadout_1`은 병과당 한 장이라 **지금 데이터로는 닿지 않는다.**
  - 로드아웃 검사(`PlayerLoadoutData.ValidateInitialDeckIndices`)는 "덱 규칙은 Core `DeckRules`가 원본이라 다시 검사하지 않는다"고 적혀 있다. 거기에 병과 규칙을 더하면 Core 밖에 덱 규칙 사본을 하나 더 만든다. 그래서 넣지 않는다.
- **설계(사용자 11-09 "Core 요청 대신 지금 할 수 있는 최선"):**
  - 네 칸 구조는 **화면 표시 규칙**으로 본다("이 화면은 병과당 한 칸으로 보여 준다"). 덱 규칙(Core)과 이름·주석으로 구분한다.
  - 표시 모델 Builder는 칸에 놓을 수 없는 카드를 만나면 예외 대신 그 카드를 "칸 밖"으로 세고, 모델의 편집 가능을 끈다(`CanEdit && 칸에 맞음`). 공용 안내로 "덱 편성이 병과 칸(병과당 한 장)과 맞지 않아 여기서는 편집할 수 없습니다" 한 번(§2.5의 안내 길).
  - 출전은 막지 않는다. Core가 받은 덱이라 바꾸지 않고 그대로 출전한다(`HasChanges`가 거짓이라 확정 없이 출전, `DeckEditPopupController.cs:194`·`:201`). 편집이 꺼져 있어 `CreatePlacementRequest`의 중복 예외(`DeckEditDraft.cs:111`)에는 닿지 않는다.
  - `DeckEditDraft` 머리 주석 "덱 규칙은 판단하지 않고"는 지금 사실(칸 표시 규칙은 있음)대로 고친다.
  - 칸 구조(`DeckEditSlotLayout`)는 현강님 줄이다. 구조는 바꾸지 않고 Builder·Draft 쪽만 고친다.
- **설계에서 정한 것(확인 대상) P-SR1:** 덱 의도는 "병과당 한 장"으로 본다(화면 글 "덱 (Main · Heavy · Rider · Cannon)"). 같은 병과 둘을 정식으로 허용하려면 칸 구조(현강님 UI)를 바꿔야 해서 이 설계 밖이다.
- 결함(F1)은 사라진다. View 표시 규칙이 Core 규칙과 따로 있다는 사본만 낮음으로 남는다.

## 3 낮음 102개 — 작업 묶음

중간과 함께 고치는 것은 그 자리에서 다룬다. 고칠 방향은 문제 목록에 있다. 여기에는 묶음과 설계에서 정한 것만 적는다.

### 3.1 SR-a 구도 도구 지우기 + 결함(C10·C9·C13 중간)

- 맨 앞: C-22 카메라 구도 도구 지우기(§2.12)와 B8 낮음 N-13·N-19·N-20·N-21·N-35·N-88.
- 중간: C-05, C-10, C-15, C-18, C-06, C-09(§2.14), C-26.
- C10 낮음: C-08, C-14, C-16, C-27, N-04, N-22, N-23, N-32, N-33, N-44, N-48(현강 UI 결함), N-52, N-58, N-81.
  - **C-16(11-04 BR3, Q-SR3 A):** 맵 진입 거절을 공용 안내로 한 번 알린다. 문구 원본 `StageMapEntryText`(코드 임시 문구, 3초, 로비 출발 거절과 같은 꼴). 진입 관찰자에 거절 콜백 하나(`StageMapEntryObserver.cs:91-92`), 출전 시작 결과를 열거로 바꿔 "연결 안 됨"도 안내.
    - 새 `StageNodeAvailabilityReader`(노드마다 진입 가능 여부, 표시용)와 거절 사유 출처가 겹친다. 안내는 Core 거절 결과(관찰자)를 원본으로 하고, 표시 캐시는 윤곽선에만 쓴다.
  - C-17은 이미 풀렸다(§0). 재평가에서 확인만 한다.
- C9 낮음: N-64(C-05와 함께), N-80.
- 평가 뒤 사실: 다시 평가 버튼(§0)은 C-06에서 다룬다.

### 3.2 SR-b 구조(C1 중간·C2·C4·C8)

- 중간: C-01, C-19, C-12, C-02.
- C2 낮음: C-21, N-63, N-70, N-79. N-11은 SR-a(C-10)에서 한다.
- C4 낮음: C-20, C-23, N-16, N-61, N-78. N-34는 현강님 UI 몫을 뺀 우리 쪽만.
- C8 낮음: N-03, N-65, N-67.
- B1·B13 낮음: N-06, N-07.
- **Scope 나누기(Q-SR11 A):** 평가 뒤 늘어난 책임 중 둘을 떼어 낸다.
  - 진입 준비(`TryPrepareEntry`·`PrepareEntryAttempt`·`CancelEntryPreparation`·준비 원인) → `StageMapViewEntryPreparation`
  - 비전투 복원 대기(`RequestNoncombatRestore`·`TryRestorePendingNoncombat`) → `StageMapViewNoncombatRestore`
  - Scope에는 시작·Tick·정리 순서와 판정만 남긴다.

### 3.3 SR-c SSOT(C1 낮음)

- C-03, C-28, C-29, N-02, N-18, N-24, N-25, N-27, N-29, N-30, N-38, N-40, N-41, N-57, N-71, N-72, N-73, N-74, N-75.
- C-24는 현강님 UI 몫(필터·칸 순서)을 남기고 우리 쪽 사본만 줄인다.
- N-57(증강 Buffer 소비 확인 불변식)은 천명님 Buffer 줄이다. 고친 뒤 공유한다.
- C-04(§2.13): 열쇠 사본은 남기고 개발 빌드 대조 검사로 "조용히 낡음"을 없앤다.

### 3.4 SR-d 정리(C3·C5·C6·C12·C13 낮음)

- C3: N-10, N-66.
- C5: C-11, N-01, N-05, N-14, N-15, N-17, N-26, N-31, N-36, N-42, N-43, N-46, N-49, N-53, N-54, N-55, N-68, N-76.
- C6: N-09, N-28, N-51, N-56, N-60, N-69, N-86. N-87은 현강님 UI라 남긴다.
- C12: C-30, N-08, N-50, N-59, N-77, N-85. 평가 뒤 생긴 낡은 주석(Scope `:140` "세 팝업", `StageSelectionInputNames.cs:14-15` 휠 이름)도 함께.
- C13: C-07, N-37, N-39, N-45.
- 평가 뒤 생긴 죽은 멤버: `StageMapViewScene.ClearBackground`(부르는 곳 없음).

### 3.5 SR-e 성능(C7·C11)

- C-13(§2.11), N-82, N-83, N-84.
- 평가 뒤 늘어난 것: `StageNodeHoverFeedback.Tick`이 매 프레임 `StageNodeAvailabilityReader.FillAvailable` → 증강 `IsPending`(열쇠 읽기)을 부른다. 열쇠가 바뀌면 보이는 노드마다 Core `PrepareStageEntry`를 부른다. N-83과 함께 "프레임당 한 번·바뀐 때만"으로 줄인다.

### 3.6 SR-f 재평가

- 같은 평가 설계로 다시 잰다. 범위는 지금 범위 + 평가 뒤 새 파일, 기준 판은 SR 끝 커밋이다.
- 1차와 점수를 견준다. 새로 찾은 문제는 따로 센다.

## 4 결정 (권장은 굵게)

| ID | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-SR1 (옛 Q-BR1) | C-10: 출전을 누른 뒤 닫힘 연출 중에 창이 밖에서 닫히면 | **A 출전 취소.** 말·카메라가 돌아오고 다시 누르면 된다 | B 그대로 출전 제출. 외부 닫기가 씬 이탈일 때도 제출하게 되어 위험 |
| Q-SR2 (옛 Q-BR2) | C-05: 진입·상점·회복 도메인 정지 | **A 오류 경계(F1)로 올림.** 증강과 같은 규칙 | B 정지 유지 + 팝업마다 정지 사실을 읽고 닫으며 안내. 새 계약이 필요하고 Run은 여전히 막힘 |
| Q-SR3 (옛 Q-BR3) | C-16: 맵 진입 거절 안내의 문구·시간 | **A 코드 임시 문구 `StageMapEntryText`, 3초** | B View DB 글 표에 새 칸(표 SO 코드 변경·에셋 값) |
| Q-SR4 (옛 Q-BR4) | C-15: 상점·회복이 불일치로 닫힐 때 안내 | **A 넣지 않음.** 선택 화면이 돌아오므로 갇히지 않음 | B 공통 문구 하나("상태가 바뀌어 창을 닫았습니다.") |
| Q-SR5 | C-15: 불일치로 닫은 뒤 | **A 선택 화면으로 돌아오고, 다음 복원 때 그 방문을 다시 열 수 있게(억제는 완료 닫기·직접 닫기만)** | B 선택 화면만 돌아오고 그 방문 자동 복원은 막음 |
| Q-SR6 | C-06: 덱 편집 거절·편집 불가 이유 | **A 공용 안내로 바뀔 때 한 번 + 다시 평가 버튼을 필요할 때 켬 + 정상 글·병과 수 글은 걷음** | B 화면에 상태 글 칸을 다시 둠(씬·프리팹 편집, 현강님 레이아웃 변경) / C 글을 걷기만 함 |
| Q-SR7 | C-04·C-09·C-13: Core가 있어야 깔끔해지는 셋 | **정함(11-09 사용자): Core 요청 없이 지금 할 수 있는 최선.** C-13 바뀐 때만 투영(§2.11), C-04 개발 빌드 대조 검사(§2.13), C-09 표시 못 하는 덱은 편집만 막음(§2.14). 덱 의도는 "병과당 한 장"으로 봄(P-SR1) | — |
| Q-SR8 | C-22: 에디터 카메라 구도 도구 | **정함(11-09 사용자): 필요 없으니 지움**(§2.12). 이미 씬에 저장한 맵 카메라 구도는 그대로 | — |
| Q-SR9 | C-12: 요청 수명 | **A 회복·증강 공통 부품 + "확인 안 한 결과" 원본 하나, 상점은 씬 수명 그대로** | B 상점도 런타임 소유자로 옮김(상점 흐름 전체 변경) |
| Q-SR10 | 11-04에 "담당 아님"으로 둔 덱 편집 출전 줄 둘(N-33·N-11) | **A 이번 범위에 넣음(Q-SE1 B), 변경은 작성자에게 정리** | B 그대로 둠 |
| Q-SR11 | 평가 뒤 616줄이 된 Scope | **A SR-b에서 진입 준비·비전투 복원 대기를 떼어 냄** | B 재평가 결과를 보고 판단 |
| Q-SR12 | 이번 범위 | **A SR-a~SR-f 전부** | B SR-a(결함)만 먼저 하고 나머지는 그 뒤 다시 정함 |

## 5 이번에 다루지 않는 낮음 (재평가에도 남음)

| ID | 기준 | 까닭 |
|---|---|---|
| C-24(일부) | C1 | 덱 편집 필터·칸 순서 사본 중 현강님 UI 몫(평가 설계 §2.3) |
| N-34(일부) | C4 | 덱 상세창 표시 모델의 GlobalSystem UI 쪽 몫(현강님 UI) |
| N-47 | C4 | 덱 상세창 부품이 전투 정보창과 공유(현강님 UI) |
| N-87 | C6 | 덱 슬롯 글·병과 이름·강조색 코드 고정(현강님 UI) |
| C-25 | C2 | `DeckEditUI` 한 클래스(659줄) 나누기. 현강님 줄이 대부분이라 UI 터치가 크다 |
| C-04(남는 몫) | C1 | 열쇠 사본 자체는 Core를 고치지 않고는 없앨 수 없다. 대조 검사로 조용히 낡지는 않는다(§2.13) |
| C-09(남는 몫) | C1 | 화면 칸 규칙이 Core 덱 규칙과 따로 있다. F1은 사라진다(§2.14) |

- 범위 밖: 여정 포기를 Stage로 넓히는 일(스테이지 맵 설계 §12, Core 전투 밖 Run 종료 입구가 없어 막힘), F1의 플레이어용 오류 화면.
- 보고만: `Prefabs/UI/Deck/DeckEditUI.prefab`은 런타임 참조가 없고 현강님 시험 하나만 읽는다(지우지 않음).

## 6 예상 점수 (알려진 문제 기준)

- 감점 방식이라 고치면 그 감점만 돌아온다. 낮음은 기준마다 −10이 상한이라, 그 기준의 낮음이 거의 다 풀려야 점수가 오른다.
- 재평가는 새 문제를 찾는다. 평가 뒤 늘어난 코드(+1434줄)에서 새 문제가 나올 수 있어 실제 점수는 더 낮을 수 있다.
- 각 행은 앞 Phase까지 모두 끝냈다고 보고 쌓은 값이다. C-04·C-09는 중간에서 C1 낮음으로 바뀐다고 셌다(§2.13·§2.14, C-04는 불확실).

| 끝낸 Phase | 바뀌는 기준 | 13기준 평균 |
|---|---|---|
| 지금 | — | 83.3 |
| SR-a | C10 52.5 → 100, C9 82.5 → 100, C4 75 → 82.5, C13 82.5 → 90, C11 90 → 92.5 | 약 89.6 |
| SR-b | C1 → 82.5, C2 → 97.5, C4 → 95, C8 → 100 | 약 93.5 |
| SR-c | C1 → 92.5(남는 낮음 C-24·C-04·C-09) | 약 94.2 |
| SR-d | C3·C5·C12·C13 → 100, C6 → 97.5 | 약 97.5 |
| SR-e | C7·C11 → 100 | 약 98.7 |

- 끝난 뒤 요청 7기준 약 97.5, 12기준 약 98.5.
- C-04가 재평가에서도 중간으로 남으면 C1이 87.5가 되어 13기준 약 98.3이다.
- 이 값은 배정한 항목을 모두 푼다고 본 것이다. 구현 때 해법이 모자라 남는 항목은 빼지 않는다.

## 7 Phase와 확인

- Phase마다 구현 → 실행 기록 → Codex 검수 → 보고 순서로 하고 멈춘다. 다음 Phase는 사용자 지시로 시작한다.
- Task 계획은 결정 뒤 따로 쓴다: `docs/superpowers/plans/2026-11-09-stage-select-refactor-plan-Hojin.md`.
- 사용자 Unity 확인은 Phase마다 둔다.
  - SR-a: 구도 도구를 지운 뒤 맵 카메라가 씬에 저장한 구도로 시작하는지·휠은 맵 이동만·우클릭 취소, 출전 중 덱 창 강제 닫힘, 상점·회복 불일치 닫기 뒤 선택 화면 복귀와 다시 열기, 입력 예외 F1, 덱 편집 거절 안내·다시 평가 버튼, 칸에 안 맞는 덱(같은 병과 둘)으로 덱 편집 열기·출전, 덱 편집 화면 참조 누락 시 씬 연결 실패.
  - SR-b: 맵 판정 정리 뒤 호버·끌기·휠·덱 열기·증강 열기·복원, 마커 이동·출전 이동·카메라 동행.
  - SR-c: 개발 빌드 대조 검사가 정상 플레이에서 오류를 내지 않는지.
  - SR-e: Profiler로 회복 팝업·노드 호버 할당.
- 공유(구현 뒤, 미전달로 시작): 현강님(덱 편집 UI 줄), 용근님(`DeckEditText`·덱 편집 계약), 천명님(증강 Buffer 줄, 증강 진행 열쇠가 Core 원본을 베낀다는 사실과 대조 검사), 민형님(Factory 연결 줄).

## 8 검수 기록

- 2026-11-09 Codex 설계 검수(읽기 전용, HEAD `c995dd455`): 높음 0·중간 6·낮음 3. 모두 **수용**해 이 판에 반영했다.
  - 중간: §2.3 `explicitClose` 뜻 잘못 씀 → 닫는 이유를 컨트롤러가 정함 / §2.3 회복 복원 예약 빠짐 → 불일치 닫기 뒤 `_mapChanged` / §2.11 Buffer 값으로 대신하면 Core 조건 사본 → Core 요청(Q-SR7에 C-13 더함, §6 다시 셈) / §2.12 장치 직접 읽기만으로는 운영 입력과 겹침 → 에디터 전용 입력 Layer / §2.8 카메라 되돌림 조건 `!_landingPublished`·실제 서명 / §2.7 LifeCycle의 Core 진행 상태를 번호 비교로 바꾸면 안 됨 → View 번호 비교만 정리. (§2.11·§2.12 처리는 2판에서 다시 바뀜, 아래)
  - 낮음: N-22는 억제 깃발을 완료에만 거는 것으로 안 풀림 → 깃발을 걷음(완료는 클리어 사실로 걸러짐) / 호버와 진입 판정을 나눠 넘김·선택 화면이 열리기 전 클릭 제한을 기록 / §0 커밋 수 표현.
  - 확인만: §2.2 BR1 위치 맞음, §2.1 F1 뒤 이탈 검사 순서 맞음, 서비스·UI 경로 적절, 낮음 102개 배정 누락·중복 없음, §6 산술 맞음.
- 같은 날 평가 결과의 Codex 검수는 실패했다(평가 보고서 §5).
- 2026-11-09 사용자 결정 둘(2판):
  - "에디터 카메라 구도 도구는 스테이지 선택의 그래픽용 카메라 수정이고 이제 필요 없으니 빼도 괜찮아" → §2.12를 떼어 내기에서 지우기로 바꿈, SR-a 맨 앞으로, Q-SR8 정함. 1판의 에디터 전용 입력 Layer 안(Codex 중간 지적 반영분)은 필요 없어짐.
  - "Core에서 요청할 사안으로 하지 않고 현재 할 수 있는 최선으로" → §2.11·§2.13·§2.14를 Core 없이 다시 씀, Q-SR7 정함, §5·§6 다시 셈. Core 파일을 직접 고치는 뜻으로는 읽지 않았다(사용자 명시 지시가 없어서).
- 2판은 Codex 검수를 다시 받지 않았다. Task 계획 뒤 Phase 검수 때 함께 본다.
