# 스테이지 맵 동작 위험 정리 설계

- 작성자: 호진(이호진)
- 상태: 초안, 결정 대기(§3 Q-BR1~Q-BR4). 코드는 아직 바꾸지 않았다.
- 범위: 2026-11-04 Lobby·StageSelect 검사(HEAD `22fd399ee`, 결과는 채팅)에서 나온 지적 중 플레이어가 화면에 갇히거나 아무 반응을 못 보는 넷. 구조 정리(조립·판정 중복·UI 패널 꼴)는 이 문서 범위 밖이다(§6).
- 관련: [검사 후속 정리 설계](2026-10-26-lobby-stage-review-cleanup-design-Hojin.md)(Stage 실행 계층·팝업 수명 구조의 원본), [공용 제안 팝업 설계](2026-11-02-stage-offer-common-ui-design-Hojin.md)(두루마리 닫힘 연출).

## 1 확인 사실

모두 지금 코드를 직접 읽어 확인했다. 줄 번호는 HEAD `22fd399ee` 기준이다.

### 1.1 R1 덱 출전 창이 닫힘 연출 중에 끊기면 맵이 바쁨 상태로 남는다

- `DeckEditPopupController.Depart`(`StageMap/Deck/DeckEditPopupController.cs:209-230`)는 출발·취소 콜백을 먼저 비운다(`_depart = null; _cancelDeparture = null;`). 그런 다음 출발을 `_lifetime.CloseThen(false, depart)`의 닫힌 뒤 후속으로 넘긴다.
- `StagePopupViewLifetime.Tick`(`StageMap/StagePopupViewLifetime.cs:96-111`)은 두루마리 닫힘을 기다리는 동안 다음 둘 중 하나가 생기면 후속을 버린다. 하나는 화면이 밖에서 닫힌 경우(`!_scope.IsOpen`)이고, 다른 하나는 두루마리가 꺼지거나 보이지 않게 된 경우다. "외부 동기 Hide는 정상 완료 후속을 실행하지 않습니다"가 의도된 규칙이다.
- 후속이 버려지면 `Hide`가 불리지만, 취소 콜백은 이미 비어 있어 출전 쪽에 아무 알림도 가지 않는다.
- `StageCombatDeparture`(`StageMap/StageCombatDeparture.cs`)는 이때 `_arrived == true`, `_submitted == 0`인 채 남는다. `Tick`(96-118)은 맵이 바뀌었거나 현재 노드가 출발점과 다를 때만 스스로 취소한다. 말은 도착 표시만 했고 현재 노드는 그대로라 취소되지 않는다.
- 그 뒤로는 `IsBusy`가 참으로 남는다. `TryBegin`은 바쁘면 참을 돌려 노드 클릭을 모두 삼키고(65), 덱 편집 열기(`CanOpenDeckEdit`)와 선택 화면 복귀(`CanShowSelection`)도 막힌다.
- 같은 수명 부품을 쓰는 상점(`ShopPopupController.ReconcileWithoutRequest`)과 회복은 후속이 버려져도 다음 Tick에 Buffer 사실로 다시 닫고 맵을 새로 그린다. 덱 출전만 이런 스스로 맞추기가 없다.
- 실제로 이 경로에 닿는 조작은 재생으로 재현하지 않았다(미확인). 닿는 조건은 닫힘 연출 중 외부 `Hide`나 부모 Canvas 끄기다.
- 같은 검사에서 덱 편집 컨트롤러 쪽 작은 둘도 나왔다(`_departureRun`을 열 때 비우지 않음, `IDeckCombatDisplay`를 조용히 캐스트함). 출전에 쓰는 덱 편집 UI 쪽은 호진님 담당이 아니어서 이 설계에서 고치지 않는다(2026-11-04 사용자, §6).

### 1.2 R2 맵 진입·상점·회복 도메인이 멈추면 화면에 갇힌다

- 처리 Job 바로 뒤의 확인 Job(`StageCheckJob`)은 처리 기록이 빠지면 `StageProgress.Stop(domain, reason)`을 부른다. `StageLifeCycle`은 이때 오류 로그만 남긴다(`ReportStoppedDomain` = `Debug.LogError`, `StageLifeCycle.cs:32`·`216`). 기록 누락은 Core와 View 사이 계약이 깨진 상태다.
- 멈춘 도메인은 `StagePlan.Decide`가 건너뛴다. 그래서 그 도메인의 명령은 Buffer에 남은 채 처리되지 않는다.
- 도메인별로 화면에서 생기는 일:
  - 증강: 팝업이 `IsGrowthStopped`를 보고 예외를 던져 오류 경계(F1)로 올린다(`TraitGrowthPopupController.cs:95`, 주석 "나갈 길이 없으므로").
  - 상점·회복: 팝업은 멈춘 줄 모르고 영수증을 기다린다. 영수증이 있는 동안 떠난다도 막혀 입력이 잠긴다.
  - 맵 진입: 노드 명령이 처리되지 않는다. 출전 중이면 `_submitted`를 계속 기다린다.
- 로비 아티팩트 도메인도 같은 꼴(오류 로그 + 그 도메인만 정지)이다. 로비 설계 Q-LJ2 A(D11·D20)가 "아티팩트가 멈춰도 출발·이탈은 된다"를 이유로 정했다(`2026-10-13-view-structure-design-Hojin.md` 약속 5).
- Stage는 사정이 다르다. 맵 진입·상점·회복은 노드를 끝내고 다음 노드로 가는 길에 있다. 셋 중 하나가 멈추면 Run을 이어 갈 길이 없다. 상점·회복 노드는 완료 명령이 처리돼야 끝나고, 끝나지 않은 노드에서는 Core가 다음 노드 이동을 거절한다(`CurrentStageUncleared`).

### 1.3 R3 맵 진입 거절을 플레이어에게 알리지 않는다

- `StageMapEntryObserver.Tick`(`StageMap/StageMapEntryObserver.cs:91-92`)은 노드 진입이 거절되면 입력을 지우고 `_report`만 부른다. 일반 노드와 전투 노드 모두 이 줄을 지난다. `_report`는 `StageMapViewScope.Report` = `Debug.Log`(309)다.
- 출전 쪽 `StageCombatDeparture.TryBegin`(72-77)은 연결되지 않은 전투 노드 클릭을 참으로 돌려 조용히 삼킨다.
- 그래서 플레이어가 갈 수 없는 노드를 눌러도 화면에는 아무 반응이 없다.
- 로비 출발 거절은 10-28 R-b(LM5)에서 공용 안내(`IUITextPresentation.ShowNotice`, 문구 원본 `LobbyDepartureText`, 3초 임시)로 바꿨다. 맵 쪽은 그대로다.
- 맵 View 서비스 묶음(`StageMapViewServices`)에는 안내 서비스가 없다. `StageMapViewSystem`이 노드 호버(`StageNodeHoverInfo`)에만 `global.UITextPresentation`을 넘긴다.
- 거절 사유는 Core의 `ChapterProgressionFailureReason`과 `ChapterSelectionFailureReason`(`NavigationFailureReason`)으로 온다.

### 1.4 R4 맵 입력이 운영에서 늘 있는 의존을 선택 인자로 받는다

- `StageMapInteraction` 생성자(`StageMap/StageMapInteraction.cs:33-46`)는 `cameraDrag`·`pointerBlocker`·`blocksEntry`·`interceptEntry`를 기본값 null로 받고, null이면 그 기능을 조용히 건너뛴다.
- 이 중 `interceptEntry`가 빠지면 전투 노드가 덱 확인 없이 바로 제출된다. `blocksEntry`가 빠지면 증강이 남아 있어도 노드 진입이 막히지 않는다.
- 운영 생성 경로는 `StageMapViewInput.Create` 하나다. 그 안에서 넷 다 늘 넘긴다(카메라 끌기는 `Scope.Start`가 `_input.Create` 전에 `AdoptCameraDrag`로 늘 넣고, 막기 부품은 `new StageMapPointerBlocker()`).
- `Create` 자체도 `interceptEntry`를 선택 인자로 받는다.
- 이 두 클래스를 만드는 시험은 없다(`Assets/Tests` 검색 0건).

## 2 설계

### 2.1 BR1 출전은 덱 창이 닫힌 사실로 스스로 맞춘다 (R1)

- `StageCombatDeparture.Tick`에 한 갈래를 더한다. 도착했고(`_arrived`) 아직 제출하지 않았는데(`_submitted == 0`) 덱 창이 열려 있지 않으면(`!_deck.IsOpen`) 취소한다. 취소하면 말을 원위치로 돌리고 노드를 다시 누를 수 있다.
  - 상점·회복과 같은 방식이다. 버려진 후속을 알림으로 되살리지 않고, 매 Tick 사실(창이 열려 있나)로 판단한다.
  - 정상 출전은 영향이 없다. 닫힘 연출이 끝나면 `CloseImmediately`가 화면을 닫은 직후 같은 호출 안에서 출발(`Submit`)을 부르므로, 다음 Tick에는 `_submitted != 0`이다.
  - 플레이어가 창을 그냥 닫으면 지금처럼 `Hide`의 취소 콜백이 먼저 취소한다.
  - 비용: 출전 중인 동안만 bool 하나를 더 읽는다. 할당은 없다.
  - 수명 부품(`StagePopupViewLifetime`)과 덱 편집 컨트롤러(`DeckEditPopupController`)는 바꾸지 않는다. 출전 쪽은 이미 있는 `IsOpen`을 읽기만 한다.

### 2.2 BR2 Stage 도메인 정지는 오류 경계로 올린다 (R2, Q-BR2 권장안)

- `StageLifeCycle`이 `StageProgress`에 넘기는 보고를 `Debug.LogError`에서 공용 오류 경계 보고로 바꾼다(`_faults.Report(new InvalidOperationException(reason))`). `_faults`를 생성자에서 받은 뒤 `StageProgress`를 만들도록 필드 초기화를 생성자 안으로 옮긴다.
  - 증강 팝업이 이미 하는 일을 출처 한 곳에서 한다. 네 도메인이 같은 규칙을 따른다.
  - 확인 Job의 기록 누락뿐 아니라 전투 진입 시도가 깨진 경우(`StagePlan` `StopCombatEntry` → `Stop(Entry, problem)`)도 같은 보고를 지난다.
  - 10-26 검사 후속 정리(TP-R9)가 Stage에 옮겨 온 "그 도메인만 멈춤"을 Stage에서는 바꾸는 결정이다. 그래서 Q-BR2로 묻는다.
  - 팝업마다 정지 사실을 따로 읽게 하는 사본을 만들지 않는다(SSOT).
- 증강 팝업의 `IsGrowthStopped` 갈래(`TraitGrowthPopupController.cs:95-96`)는 닿지 않게 된다. 오류 상태가 되면 `CanWork`가 먼저 거짓이 되기 때문이다. 이 갈래와 계약 멤버 `IStageTraitGrowthFacts.IsGrowthStopped`, `StageLifeCycle`의 구현 한 줄을 걷는다.
- 도메인 정지 깃발(`_entryStopped` 등)과 `IStageDeckEditFacts.IsFlowStopped`는 그대로 둔다. 같은 Runner Tick 안의 다음 등록을 막는 데 쓰이고, 덱 편집 창(용근님 계약)이 읽는다.
- `IStageProgress.Stop` 주석의 "다른 도메인은 계속합니다"는 "오류 경계로 보고합니다"로 고친다.
- 로비 아티팩트(D11·Q-LJ2 A)는 바꾸지 않는다. 아티팩트 편집은 Run 진행에 꼭 필요한 길이 아니어서 "멈춰도 출발할 수 있다"는 약속이 지금도 맞다.
- F1의 화면: 지금 오류 경계는 로그를 남기고 입력을 멈추며, 플레이어용 오류 화면은 없다(`GameBootstrap` = `Debug.LogException`). 이것은 이 문서 범위 밖이다. 지금 상태(로그만 남고 화면이 잠김)보다 원인이 예외로 남고 다른 처리가 함께 멈추는 쪽이 낫다.

### 2.3 BR3 맵 진입 거절을 공용 안내로 한 번 알린다 (R3)

- 문구 원본 `StageMapEntryText`(새 `.cs`, `ViewSystem/StageMap/`)를 둔다. 로비 `LobbyDepartureText`와 같은 꼴이다.
  - `Rejected(ChapterProgressionResult)`: 사유별 한 줄. 플레이어가 겪는 정상 거절은 따로 적는다.
    - `NavigationRejected` + `NotConnected`: "연결된 노드로만 이동할 수 있습니다."
    - `CurrentStageUncleared`: "현재 노드를 먼저 마쳐야 합니다."
    - `AlreadyCleared`·`StageAlreadyCleared`: "이미 지나온 노드입니다."
    - `PendingTraitGrowth`: "증강을 먼저 선택해야 합니다."
    - `RunEnded`: "종료된 Run에서는 이동할 수 없습니다."
    - `EnhancementShopUnavailable`: "아직 이용할 수 없는 시설입니다."
    - 나머지: "이동할 수 없습니다."
  - `NotConnected`: 위 첫 줄과 같은 문구 하나를 출전 쪽도 쓴다.
  - 표시 시간 상수 하나. 3초이고 로비와 같은 임시 값이다(Q-BR3).
- 안내 서비스는 `StageMapViewServices`에 `Text`(`IUITextPresentation`)로 넣는다. `StageMapViewSystem`이 `global.UITextPresentation`을 넘긴다(Global Context 경로, 로비와 같음).
- 진입 관찰자: `StageMapEntryObserver`에 거절 콜백(`Action<ChapterProgressionResult>`) 하나를 더한다. 거절 줄(92)에서 지금의 진단 로그와 함께 부른다. 콜백은 `Scope`가 넘기고, 콜백은 안내만 띄운다.
- 출전 쪽: `StageCombatDeparture.TryBegin`의 반환을 bool에서 결과 값(가로채지 않음·시작함·막음·연결 안 됨)으로 바꾼다. "연결 안 됨"이면 `Scope.InterceptEntry`가 안내를 띄우고 클릭을 삼킨다. 출전 부품은 UI를 모르는 채로 남는다.
- 상점·회복이 상태 불일치로 스스로 닫는 경우의 안내는 이번에 넣지 않는다(Q-BR4).
  - 이 갈래들(`ShopPopupController.cs:316-320`·`331-335`, `RecoveryPopupController.cs:117-123`·`183`)은 정상 흐름에서는 닿지 않는다. 팝업이 열린 동안 바깥에서 상태가 바뀌었을 때만 닿는다. 닫힘 연출 중에는 `CanWork`가 거짓이라 닿지 않는 것도 확인했다(`IsDisplay`).
  - 원인은 정상 거절이라기보다 계약 쪽 문제에 가까워 플레이어용 문구를 정하기 어렵다. 지금처럼 진단 로그로 원인을 찾는 편이 낫다.
- 진단 로그(`Debug.Log`)는 그대로 둔다.

### 2.4 BR4 맵 입력의 운영 의존은 필수로 받는다 (R4)

- `StageMapInteraction` 생성자의 네 인자를 기본값 없이 받고 null이면 `ArgumentNullException`을 던진다. 안의 `?.`·null 갈래(`_cameraDrag?.`, `_pointerBlocker` 검사, `_blocksEntry`·`_interceptEntry` null 검사)를 걷는다.
- `StageMapViewInput.Create`의 `interceptEntry`도 필수로 바꾼다.
- 다른 null 허용(F-17 그룹 생성자, 아티팩트 컨트롤러 등)은 이 문서 범위 밖이다(§6).

## 3 결정 대기

| ID | 질문 | 권장 | 다른 안 |
|---|---|---|---|
| Q-BR1 | 출전을 누른 뒤 닫힘 연출 중에 창이 밖에서 닫히면 | **A 출전 취소.** 말이 돌아오고 다시 누르면 된다(BR1) | B 그대로 출전 제출(눌렀던 의도대로). 외부 닫기가 씬 이탈 등일 때도 제출하게 되어 위험 |
| Q-BR2 | 맵 진입·상점·회복 도메인 정지(기록 누락) | **A 오류 경계로 올림**(BR2). 증강과 같은 규칙, 로비 아티팩트는 D11 그대로 | B 도메인 정지 유지, 팝업마다 정지 사실을 읽고 닫으며 안내. 새 사실 계약·서비스 키·팝업 셋 수정이 필요하고, 닫아도 노드를 끝낼 수 없어 Run은 여전히 막힘 |
| Q-BR3 | 거절 안내 문구·시간의 자리 | **A 코드 임시 문구 `StageMapEntryText`, 3초**(로비 출발 거절과 같은 꼴, 기획 확인 전) | B View DB 글 표에 새 칸(에셋 값은 Codex 작업, 표 SO 코드 변경) |
| Q-BR4 | 상점·회복이 불일치로 스스로 닫힐 때 안내 | **A 이번에 넣지 않음**(바깥에서 상태가 바뀐 경우에만 닿는 갈래라 진단 로그로 원인을 찾는 편이 낫다) | B 넣음("상태가 바뀌어 창을 닫았습니다." 같은 공통 문구 하나, 상점·회복 컨트롤러 생성 인자 하나씩) |

설계에서 정한 것(확인 대상):

- P-BR1: 출전 스스로 맞추기는 "도착·미제출·덱 창 닫힘"만 본다. 덱 창이 다른 이유로 열린 경우는 없다(출전 중에는 일반 열기가 막힘, `CanOpenDeckEdit`).
- P-BR2: Q-BR2 A면 `IStageTraitGrowthFacts.IsGrowthStopped`를 걷는다. 이 계약은 호진 줄이고, 쓰는 곳은 증강 팝업 하나다.
- P-BR3: 출전 시작 결과 값은 `StageMap` 안 internal 열거 하나로 둔다. 가로채지 않음은 지금의 false, 나머지는 지금의 true에 해당한다.
- P-BR4: 전투 노드 연결 판정이 View·Core 두 곳에 있는 것(검사 I-2)은 이번에 바꾸지 않는다. 출전 쪽은 연결 밖이면 말을 움직이지 않기 위한 표시 판단으로만 남기고, 안내 문구는 Core 거절과 같게 맞춘다.

## 4 바뀌는 곳

| 파일 | 바꾸는 것 | 줄 작성자 |
|---|---|---|
| `ViewSystem/StageMap/StageCombatDeparture.cs` | BR1 Tick 갈래, BR3 `TryBegin` 결과 값 | 호진 |
| `LifeCycleSystem/LifeCycles/Stage/StageLifeCycle.cs` | BR2 보고 대상, `StageProgress` 생성 자리, `IsGrowthStopped` 구현 한 줄 | 호진 |
| `LifeCycleSystem/LifeCycles/Stage/IStageProgress.cs` | BR2 주석 | 호진 |
| `LifeCycleSystem/LifeCycles/Stage/IStageTraitGrowthFacts.cs` | BR2 멤버 하나 걷음 | 호진 |
| `ViewSystem/StageMap/TraitGrowth/TraitGrowthPopupController.cs` | BR2 닿지 않는 갈래 걷음 | 호진 |
| `ViewSystem/StageMap/StageMapEntryText.cs`(새, + `.meta`는 Unity가 만듦) | BR3 문구 원본 | — |
| `ViewSystem/StageMap/StageMapEntryObserver.cs`·`StageMapViewEntry.cs` | BR3 거절 콜백 | 호진 |
| `ViewSystem/StageMap/StageMapViewScope.cs` | BR3 안내 콜백·출전 결과 처리, BR4 `Create` 호출 | 호진 |
| `ViewSystem/StageMap/StageMapViewServices.cs`·`StageMapViewSystem.cs` | BR3 `Text` 연결 | 호진 |
| `ViewSystem/StageMap/StageMapInteraction.cs`·`StageMapViewInput.cs` | BR4 필수 인자 | 호진 |

- 덱 편집(`StageMap/Deck/`·`DeckEditUI`), Factory·ServiceKeys·Framework·GlobalSystem·Core·UI 화면·씬·프리팹·SO는 바꾸지 않는다. 그래서 공유 메모가 필요 없다.
- 시험: 이 클래스들을 만드는 기존 시험이 없어 맞출 곳이 없다. 새 시험은 만들지 않는다.

## 5 원칙 점검

- SSOT:
  - BR1: 출전이 "덱 창이 열려 있나"를 수명 부품의 사실 하나(`IsOpen`)로 읽는다. 취소 사본을 더 만들지 않는다.
  - BR2: 정지의 결과를 출처 한 곳에서 정하고, 팝업마다 정지 판정을 두지 않는다.
  - BR3: 거절 문구를 한 클래스에 둔다. 출전의 "연결 안 됨" 문구도 같은 줄을 쓴다.
- SOLID:
  - 출전 부품은 UI를 모르고 결과 값만 돌려준다. 안내는 Scope가 띄운다.
  - 문구 원본과 표시를 나눈다(로비와 같음).
  - 맵 입력은 필요한 의존을 생성 때 모두 받는다.
- FW: 안내 서비스는 Global Context(`global.UITextPresentation`)로 받는다. 루트 재등록이나 직접 조회는 없다. 오류는 공용 경계(F1)로 보고한다.
- 기능 변경 독립성:
  - BR1은 출전 파일 하나만 건드린다. 덱 편집·상점·회복·증강은 바뀌지 않는다.
  - BR2는 Stage LifeCycle과 증강 팝업만 건드린다. 로비는 바뀌지 않는다.
  - BR3은 맵 진입 경로만 건드린다.
- 성능:
  - 매 Tick에 더하는 것은 출전 중일 때 bool 하나뿐이다.
  - 안내 문자열은 거절이 일어날 때만 만든다. 거절 콜백은 생성 때 한 번 만든 대리자를 쓴다.

## 6 범위 밖(검사에서 나왔지만 이 문서가 다루지 않는 것)

- 맵이 한가한지 판단하는 곳 여섯(검사 I-3), 요청 수명 방식 셋(I-5), 전투 노드 판정 두 벌(I-2).
- 에디터 카메라 구도 도구 분리(F-1), Factory 밖 조립(F-3·F-5), 다른 null 허용(F-17·I-22·I-25).
- 상점·회복 강제 닫힘 안내(Q-BR4 B를 고르면 이 문서에 더한다).
- 덱 편집 컨트롤러의 출전 줄 둘(2026-11-04 사용자: 출전에 쓰는 UI 쪽은 담당이 아니므로 두기). 담당자가 볼 때 참고할 내용:
  - `_departureRun`은 `Show`에서 `_draft.Basis`가 있을 때만 쓰고(`DeckEditPopupController.cs:77`) 열 때 비우지 않는다. 그런데 `RebaseDraft`(149-158)는 늘 이 값과 비교한다. Basis가 없던 창에서는 기본값이나 지난 출전의 값과 비교해 출전 창을 잘못 닫을 수 있다. 창이 닫히면 출전은 취소된다.
  - `_departureScreen = screen as IDeckCombatDisplay`(39)가 조용한 캐스트라 계약이 빠져도 출전할 때에야 예외가 난다.
- F1의 플레이어용 오류 화면.

## 7 사용자 Unity 확인 목록(구현 뒤)

1. 전투 노드 클릭 → 말 이동 → 덱 창 → 출전 → 전투 진입이 지금과 같다.
2. 출전 덱 창에서 닫기를 누르면 말이 원위치로 오고, 노드를 다시 누를 수 있다.
3. 연결되지 않은 전투 노드를 누르면 말이 움직이지 않고 "연결된 노드로만 이동할 수 있습니다."가 3초 동안 보인다.
4. 연결되지 않은 일반(상점·회복) 노드를 누르면 같은 안내가 보인다.
5. 증강이 남아 있을 때 노드를 누르면 지금처럼 증강 화면이 다시 뜬다(안내 없음).
6. 상점·회복·증강 팝업의 열기·구매·완료·닫기가 지금과 같다.
7. 재현하지 못하는 것(미확인으로 남김): 닫힘 연출 중 외부 닫기(BR1의 새 갈래), 처리 기록 누락(BR2).
