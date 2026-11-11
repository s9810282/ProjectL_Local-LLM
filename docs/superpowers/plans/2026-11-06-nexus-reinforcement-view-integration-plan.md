# 적 본부 증원 전투 View 연결 계획

- 작성: 2026-11-06
- 상태: N1~N3 및 사용자 변경 N4~N7 코드 반영(2026-11-06). 컴파일·Unity 재생·Profiler 확인 대기
- 입력 계약: [Core → View 인계](../handoffs/2026-11-05-nexus-enemy-reinforcement-core-view-handoff.md), [Core 설계](../specs/2026-11-05-nexus-enemy-reinforcement-core-design.md)
- 범위: 적 본부 증원의 사건 순서 표시, 본부 주변 기하 범위 표시, 필요한 유닛 표시 준비, Unity 인수 확인. 생산 스테이지의 증원 활성화와 수량·밸런스 결정은 별도 콘텐츠 작업이다.

## 1. 계획 수립 당시 코드와 연결 경계

| 원본 / 소비자 | 현재 | 연결할 책임 |
|---|---|---|
| `CombatReinforcementEvent` | Core가 `Reinforced = 48`, `NexusId`, `Record`, `Created`를 발행 | View는 `Created.EntityId`를 새 유닛의 주체로 사용한다. `NexusId`는 출처 식별이며 유닛 ID가 아니다. |
| `CombatEventTypeTable` → `CombatEventHandlerTable` → `CombatSceneEventPresenter` | 새 형식이 표에 없어 `OrderedCombatPresentation`이 경고 후 건너뜀 | 형식·처리기 칸을 등록하고 사건 시점 `Created`를 보인 사실에 적용한다. 이후 효과·사망 사건은 기존 처리기로 둔다. |
| `CombatStateData.ReadNexusReinforcementArea()` | 기하 범위가 Buffer에 공개됨. View 호출 없음 | 적 본부 호버 시 읽어 필드 강조 층에 전달한다. 후보 칸을 View에서 다시 판정하지 않는다. |
| `FieldHighlightLayers` / `FieldView` | 증원 전용 층·색 없음 | 낮은 우선순위의 전용 범위 층을 추가한다. 기존 공격·이동·경고·호버 층과 삭제 수명을 분리한다. |
| `CombatSceneDisplay.Prewarm` | 초기 캐릭터만 세어 유닛/HUD/공격 표시를 준비함 | 고정된 증원 풀의 키와 수량을 합쳐, 첫 증원에서 만드는 비용을 설치 시점으로 옮긴다. |

`CombatSceneDisplay`는 받은 `CombatDisplayValues.State`만 읽는다. Core, InGame DB, View→Core 입력, Framework 등록, `CombatViewSystem`의 흐름 규칙은 바꾸지 않는다. 결과 끝 최종 상태 갱신은 기존 경로를 그대로 사용한다.

## 2. 표시 설계 결정

1. **생성 시점:** 증원 사건은 새 행동 시작/공격으로 분류하지 않는다. `CombatEventTypeTable`에는 `subject = Created.EntityId`, 별도 `Reinforcement` 처리기 칸을 둔다. 처리기는 `CombatShownFacts.ApplyCharacter(Created)` 뒤 기존 소환 등장 조립(`CombatSummonEventHandler.Compose`)을 실행한다. 따라서 후속 Taunt·SoloAction·이동·사망이 같은 ID를 찾는다. `Created` 대신 결과 끝 Character Buffer를 앞당겨 읽지 않는다.
2. **범위 의미:** 증원이 설정된 살아 있는 적 본부의 **점유 칸에 마우스를 올린 동안만** 범위를 보인다. 낮은 우선순위의 별도 색으로 테두리와 칸 윗면의 반투명 채우기를 함께 표시한다. 현재 점유·방해물·남은 풀·적 상한 때문에 색을 바꾸지 않는다. 이는 생성 가능 표시가 아니라 건물의 **기하학적 공급 범위**다. 본부 이탈·UI 가림·결과 재생·파괴·전투 종료·해제 때 지운다. `CellPrefab`과 `GridOutline`에서 같은 의미로 보이는지 확인한다.
3. **수명과 성능:** 범위 API는 호출마다 컬렉션을 만든다. 같은 칸·같은 결과를 매 Tick 다시 조회하지 않고, 호버 진입 또는 게시 결과 뒤 재평가 때만 읽는다. `FieldView.SetLayerCells`의 기존 변경 칸 갱신을 사용하며 별도 범위 사실·View 재고를 만들지 않는다. 새 전투 연결 시 이전 강조와 준비된 유닛은 기존 Clear/Release 수명을 따른다.
4. **표시 준비:** 풀은 전투 동안 고정된 `NexusReinforcements.Pool`에서 읽는다. 기존 키별 초기 인원에 `min(풀의 해당 키 수량, FieldEnemyLimit)`을 더한 값을 유닛 준비 목표로 사용한다. 풀 키가 여럿이면 준비 비용도 늘 수 있으므로 Phase N3에서 키별 생성 수와 진입 비용을 확인한다. 과도하면 실제 측정 근거로 준비 상한을 다시 설계한다. 소환용 캐릭터 준비 규칙은 변경하지 않는다.

## 3. Phase별 Task와 리뷰 지점

### N1 — 사건 순서에 맞춘 새 유닛 표시

- `CombatEventTypeTable`에 봉인된 증원 사건 한 줄을 추가한다. 새 처리기 칸은 `CombatEventHandlerTable`에서 누락 시 조용히 `Facts`로 떨어지지 않게 명시적으로 연결한다.
- 증원 전용 처리기는 `Created`를 `CombatShownFacts`에 적용하고 기존 소환 등장 조립을 재사용한다. `CombatSceneEventPresenter`의 조립 지점에만 새 처리기를 넣고 일반 틀·순서 재생기를 고치지 않는다.
- 확인 기대값: 한 결과의 증원 1~2명이 각 사건에서 등장하고, 바로 뒤 특성 효과·사망이 해당 표시를 대상으로 진행된다. 마지막에는 최종 Buffer 사실과 일치한다. 새 이벤트 건너뜀 경고가 없다.
- **여기서 멈추고 변경 파일·컴파일 미확인·Unity 확인 항목을 보고한다.**

### N2 — 적 본부 공급 범위 표시

- `FieldHighlightLayer`와 표에 전용 호버 층을 추가한다. 새 색은 `FieldView`의 직렬화 값 → `FieldHighlightPalette` → 색 슬롯 순서로 연결한다. 기본 색은 기존 공격/이동/낙석과 구분되는 보라 계열이고, 칸 채우기 없이 테두리로 시작한다.
- 적 본부 호버에서만 `ReadNexusReinforcementArea()`를 읽어 이 층을 갱신한다. 이탈·UI 가림·재생·해제에는 증원 층만 비운다. `Combat.unity`의 다른 변경은 수정하지 않으며, Inspector 기본 색 확인 뒤 씬 직렬화 변경이 필요할 때만 범위를 다시 확인한다.
- 확인 기대값: 배치/전투에서 본부 호버 동안 공급 범위가 보이고 점유 칸은 빠진다. 기본 padding 2인 2×2 본부의 예시는 32칸이며 실제 스테이지 설정을 우선한다. 공격·이동·호버가 겹치면 높은 층이 우선한다. 적 상한/풀 소진/일시적인 방해물은 기하 범위 내용을 바꾸지 않는다. 본부 파괴·승리·전투 교체·해제에는 지워진다.
- **여기서 멈추고 리뷰를 받는다.**

### N3 — 증원 표시 준비와 Unity 인수

- `CombatSceneDisplay.Prewarm`의 기존 키별 합산에 고정 증원 풀을 병합한다. `FieldUnitPresenter.Prewarm`과 사건 Presenter 준비 경로를 재사용한다. 풀 전체 수량만큼 무제한으로 미리 만들지 않는다.
- 생산 스테이지를 임의로 수정하지 않는다. 증원 조건을 만족하는 **검증 전용 스테이지** 또는 기획자가 승인한 스테이지에서 사용자가 Unity로 확인한다. 계획 작성 당시 에셋 검색에는 명시적으로 활성화된 스테이지가 없었다. 작업 중 다른 변경으로 `Combat/Test.asset`에 증원 설정이 추가된 것을 확인했으며, 이 View 작업에서는 해당 에셋을 수정하지 않았다. 사용 가능 여부는 Unity에서 확인한다.
- Unity 확인: 배치/전투 범위, 한 명/두 명 증원, 생성 직후 특성 효과, 생성 직후 사망, 밤 회수와 다음 밤 `Placed`(동일 ID), 승리 시 범위 제거, 비증원 스테이지의 기존 표시. Profiler로 첫 증원 프레임 생성/HUD 비용과 진입 준비 비용을 비교한다.
- **여기서 멈추고 결과와 남은 검증을 보고한다.**

## 4. 경계·원칙 점검

- **SSOT:** 생성 규칙·풀 소진·기하 범위는 Core/Buffer가 원본이다. View는 규칙을 계산하거나 남은 수량을 별도 보관하지 않는다.
- **SRP / ISP / DIP:** 사건 형식 분류, 증원 사건 처리, 필드 강조, 조립을 각 기존 역할에 둔다. Display는 받은 값만 읽고 새 Core 서비스에 의존하지 않는다.
- **OCP / LSP:** 새 사건 형식은 표와 처리기 칸을 확장하며 기존 사건 처리기를 조건문으로 오염시키지 않는다. 기존 `CombatEventHandler` 계약을 그대로 따른다.
- **기능 독립성:** 증원 연출 수정은 증원 처리기에, 범위의 색·우선순위 변경은 필드 강조에, 의존성 연결은 사건 Presenter 조립에 모인다. 일반 소환·적 AI·이동·공격 규칙 수정은 필요하지 않다.
- **Framework:** 새 System/Service/Builder/Job/EventBuse/Framework Pool 등록이 없다. 유닛 View의 기존 Pool 수명과 `FieldView`의 자원 정리를 따른다. 기존 FW-001~016 계약을 우회하지 않는다.
- **검증 제한:** View 시험 코드는 작성하지 않고 MSBuild는 실행하지 않는다. Unity 컴파일·재생은 사용자가 수행하며 Codex Phase 보고에는 **컴파일 미확인**을 적는다. 다른 사람이 작성한 기존 시험 수정이 필요하면 먼저 사용자에게 묻는다.

## 5. 변경 범위와 외부 의존

- 대상 View 파일: `Playback/Ordering/CombatEventTypeTable.cs`, `Playback/Handlers/CombatEventHandlerTable.cs`, 새 `CombatReinforcementEventHandler.cs`와 `.meta`, `Playback/CombatSceneEventPresenter.cs`, `Display/Field/FieldHighlightLayers.cs`, `FieldHighlightPalette.cs`, `FieldView.cs`, `Display/CombatSceneDisplay.cs`, `Input/Assist/CombatEnvironmentHover.cs`, 새 `CombatNexusRangeHover.cs`와 `.meta`, 새 `Input/Contracts/ICombatNexusRangeFeedback.cs`와 `.meta`, View 조립 `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`.
- 예상하지 않는 변경: Core 계산·Buffer 계약, FieldCamera, Architecture, 생산 스테이지 에셋, 전투 씬의 다른 작업자 변경.
- 검증용 스테이지 콘텐츠 또는 생산 스테이지 활성화는 이 View 연결 계획의 외부 조건이다. 스테이지의 `nexusReinforcements.enabled`와 풀은 기획 입력이며 View가 임의의 값을 채우지 않는다.
- 커밋·push는 사용자가 수행한다.

## 6. 구현 기록 (2026-11-06)

| Phase | 코드 결과 | 확인 상태 |
|---|---|---|
| N1/N4 | `CombatReinforcementEvent`를 전용 처리기 칸에 등록했다. 처리기는 `Created`를 보인 사실에 반영하고 기존 소환 등장 조립을 실행한다. 공통 사건 틀·순서 재생기는 변경하지 않았다. | 정적 연결 확인. 실제 생성 → 소환 동작 → 후속 효과의 Unity 재생은 미확인. |
| N2/N5 | 증원 범위 전용 강조 층·색을 추가했다. 살아 있는 묶인 적 본부에 호버한 동안만 Buffer 범위를 표시한다. 씬 직렬화 값과 다른 강조 층은 수정하지 않았다. | 정적 연결 확인. 두 필드 표시 모드와 이탈/승리/교체 때 제거는 Unity 미확인. |
| N3 | 설치 시 초기 캐릭터 수와 증원 풀의 키별 `min(Count, FieldEnemyLimit)`을 합쳐 기존 유닛/HUD/사건 표시 미리 생성에 전달한다. Core·생산 스테이지는 수정하지 않았다. | 정적 연결 확인. 설치 비용·첫 증원 프레임 Profiler 측정과 PlayMode 인수는 미확인. |

`git diff --check`에서 변경 코드의 공백 오류는 없었다. 이번 작업은 View 시험 코드를 작성하지 않았고 MSBuild를 실행하지 않았다. **컴파일 미확인.**

### 사용자 Unity 확인 순서

1. 전투 View 스크립트가 컴파일되는지 확인한다. 새 사건 처리기 `.cs`와 `.meta`가 함께 들어 있다.
2. 증원이 활성화된 검증 전용 스테이지 또는 승인된 스테이지에서 적 본부 호버 범위를 확인한다. 작업 중 다른 변경으로 `Combat/Test.asset`에 설정이 추가됐지만 Unity 실행 가능 여부는 미확인이다. `CellPrefab`과 `GridOutline` 모두 확인한다.
3. 기본 설정의 1명/2명 생성, 바로 뒤 Taunt·SoloAction, 생성 직후 사망이 순서대로 보이는지 확인한다. 콘솔에 `CombatReinforcementEvent` 건너뜀 경고가 없어야 한다.
4. 밤 회수 후 다음 전투의 `Placed`가 같은 ID로 보이는지, 본부 파괴/승리/전투 교체에서 범위가 사라지는지 확인한다. 적 상한·풀 소진·방해물 때문에 기하 범위가 사라지면 안 된다.
5. Profiler에서 전투 설치 시 준비 비용과 첫 증원 프레임의 유닛 모델/HUD 생성 비용을 확인한다. 풀 키가 많은 스테이지의 메모리 비용도 함께 본다. 측정 없이 준비 상한을 임의로 더 줄이지 않는다.

## 7. 사용자 표시 변경 (2026-11-06)

사용자 요청: 증원 시 기존 소환 등장 액션을 사용하고, 본부 주변 범위는 적 건물에 마우스를 올린 동안만 표시한다. 위 §2~§6은 이 요청을 반영한 현재 계획이다. N3의 미리 생성과 Core 계약은 유지한다.

### 설계

- 증원 사건 처리기가 기존 `CombatSummonEventHandler.Compose`를 재사용한다. `Created`를 먼저 보인 사실에 반영한 뒤, 기존 `CombatRisePiece`의 바닥 아래에서 올라오는 동작을 실행한다. 새 유닛은 Core의 `Deferred` 상태이므로 이 연출이 같은 턴의 행동권을 주지는 않는다. 소환 이벤트 자체로 변환하거나 Core 이벤트 순서를 바꾸지 않는다.
- 범위는 본부에 묶인 `NexusId`의 살아 있는 적 구조물 **점유 칸**에 포인터가 있을 때만 표시한다. `CombatEnvironmentHover`의 기존 포인터 집기·UI 가림·표시 허용·결과 후 재평가 수명에 증원 전용 `CombatNexusRangeHover`를 연결한다. 이 객체는 호버 대상만 찾고 Core Buffer의 `ReadNexusReinforcementArea()`를 받아 전용 표시 계약에 전달한다. 범위의 실제 기하학은 계산하지 않는다.
- 적 본부를 떠나거나 UI 위로 가거나 호버/입력 수명이 끝나면 범위를 지운다. 같은 칸·같은 결과를 매 Tick 다시 조회하지 않는다. 본부가 파괴되거나 전투가 끝나면 Core 조회가 빈 범위를 주고 표시도 내려간다. 기믹 호버 층과 증원 층은 서로의 Clear를 호출하지 않는다.
- 별도 `ICombatNexusRangeFeedback`을 Display가 구현한다. Factory는 좁은 계약을 환경 호버 조립에만 주입한다. 일반 유닛 범위·환경 설명·기믹 범위의 기능 구현은 수정하지 않는다.

### Task

| Phase | 수정 범위 | Unity 확인 기대값 |
|---|---|---|
| N4 — 소환 액션 | 증원 처리기와 사건 Presenter 조립 | 적 턴 시작에 생성 칸에서 기존 소환 액션이 끝난 뒤 후속 효과가 순서대로 보인다. 1명/2명 배치와 생성 직후 사망을 확인한다. |
| N5 — 본부 호버 범위 | 전용 범위 표시 계약·호버 처리기·환경 호버 연결·Factory 조립·Display의 상시 표시 제거 | 배치/전투에서 살아 있는 적 본부 점유 칸에 호버한 동안만 범위가 보인다. 이탈·UI 가림·결과 재생·본부 파괴·승리·씬 해제에서 지워지고, 다른 호버 범위와 간섭하지 않는다. |

각 Phase 코드를 반영한 뒤 변경 내용을 점검하고 보고한다. View 시험 코드는 작성하지 않으며 MSBuild는 실행하지 않는다. Unity 컴파일·재생과 Profiler 확인은 사용자가 수행한다.

### N4~N5 구현 기록 (2026-11-06)

- N4: `CombatReinforcementEventHandler`가 기존 `CombatSummonEventHandler.Compose`를 호출한다. 증원 칸 강조 → `Created` 표시 → 기존 소환 등장 액션 → 강조 해제 순서다. 신규 VFX 설정이나 Core 변경은 없다.
- N5: `CombatSceneDisplay.RefreshCore`의 상시 범위 갱신을 제거했다. `CombatNexusRangeHover`가 묶인 살아 있는 적 본부 점유 칸을 확인하고 Buffer 범위를 읽는다. `CombatEnvironmentHover`가 기존 포인터 수명과 함께 이 기능을 호출·정리한다. 새 `ICombatNexusRangeFeedback`은 증원 층만 쓰며, Factory가 Display와 연결한다.
- 정적 검색으로 조립 호출자와 `.meta` 쌍을 확인했고 `git diff --check`에서 변경 코드 공백 오류는 없었다. **컴파일·Unity 재생 미확인.**
- Unity 확인 추가: 마우스가 본부에 들어갈 때만 범위 표시, 본부의 각 점유 칸 간 이동 시 유지, 이탈·UI 가림·결과 재생·파괴·승리·씬 해제 시 제거. 유닛/기믹 호버 범위와 서로의 지우기 동작이 독립적인지 확인한다.

## 8. 적 턴 안내 → 증원 → 적 행동 순서 (2026-11-06)

### 원인과 결정

Core의 `BeginControllerTurn` 결과는 이미 `ControllerTurnStarted`를 증원 사건보다 먼저 발행한다. 현재 `CombatTurnDisplayPresenter`는 결과 재생이 끝난 `CombatSceneDisplay.RefreshCore`에서만 호출되어 화면에서는 소환 뒤에 “적 턴”이 열린다. Core 사건 순서를 바꾸지 않고, 적 턴 시작 사건의 재생 시점에도 같은 TurnDisplay Presenter를 호출한다. Presenter의 `_shownTurnSequence`가 결과 끝 재호출을 중복 표시하지 않는다.

적 턴 시작 사건은 현재 `EnemyTurnGapSeconds`(운영 기본값 1초)를 **안내를 먼저 보이는 간격**으로 사용한다. `CombatTurnEventHandler.ShowsBeforeGap`에서 적 턴 시작만 선표시하고, 사건 처리기가 TurnDisplay Presenter에 최종 턴 사실을 전달한다. 간격이 끝난 뒤 다음 증원 사건이 시작된다. DB에서 간격을 0으로 설정하면 사건 순서는 유지되지만 화면에 머무는 시간은 없다. UI 자체의 자동 닫힘 시간은 변경하지 않으며 증원과 일부 겹칠 수 있다.

### N6 Task·확인

- `CombatTurnDisplayPresenter`에 적 턴 시작 사건을 받는 좁은 입구를 둔다. 기존 `Render`를 재사용해 안내 순번 원본을 하나로 유지한다.
- `CombatTurnEventHandler`에 표시 콜백을 주입하고 적 `ControllerTurnStarted`만 간격 전에 보인다. 일반 Player/EnemyPhaseEnded 표시 정책은 유지한다.
- `CombatEventPlaybackFactory` 조립에서 운영 화면의 TurnDisplay 콜백만 넘긴다. 특성 미리보기에는 안내 UI가 없으므로 콜백을 연결하지 않는다. `CombatSceneEventPresenter`는 UI 객체를 직접 알지 않는다.
- Unity 확인 기대값: 적 턴 안내가 먼저 열린 뒤 현재 턴 간격을 지나 증원 등장 액션이 시작하고, 그 다음 기존 적 행동이 진행된다. 증원이 없는 적 턴과 플레이어 턴의 안내 중복·누락이 없다. 취소·종료·다음 턴에서 늦은 안내가 다시 열리지 않는다.
- View 시험 코드는 작성하지 않고 MSBuild는 실행하지 않는다. Phase N6 코드 뒤 멈춰 보고한다. **컴파일·Unity 재생은 사용자 확인.**

### N6 구현 기록 (2026-11-06)

- `CombatTurnEventHandler`가 적 `ControllerTurnStarted`를 간격 전에 표시하고, 같은 사건에서 TurnDisplay Presenter의 안내를 연다. 운영 `CombatSceneDisplayFactory`만 콜백을 연결하며 특성 미리보기 조립은 기존처럼 안내 UI가 없다.
- `CombatTurnDisplayPresenter`의 기존 턴 순번 중복 방지를 그대로 쓴다. 결과 끝 Refresh에서 동일한 적 턴 안내를 다시 열지 않는다. Core 사건 순서·TurnDisplayUI 자원 수명·DB 간격 값은 변경하지 않았다.
- 코드 검색으로 두 playback Factory 호출(운영·특성 미리보기)과 한 사건 Presenter 생성 입구를 확인했다. Unity에서 실제 표시 순서와 종료/취소 수명은 미확인이다. **컴파일 미확인.**

## 9. N7 — 적 본부 증원 공급 범위 윗면 채우기 (2026-11-06)

### 설계·Task

- `FieldHighlightLayers`의 증원 범위 행에서 채우기만 활성화한다. 테두리·색·낮은 그리기 우선순위·평소 표시·호버 수명은 같은 행과 기존 `CombatNexusRangeHover`를 원본으로 유지한다. `CellPrefab`은 기존 칸 색, `GridOutline`은 기존 `FieldCellFillStack`의 지형 표면 높이·공용 `fillAlpha`로 윗면을 반투명하게 칠한다.
- `FieldView`의 색 설명을 실제 채우기 동작과 맞춘다. 공급 범위의 기하 칸은 Core Buffer에서 받은 목록 그대로 사용하며, View에서 생성 가능 여부를 다시 판정하지 않는다.
- 책임·외부 경계: 호버는 표시 수명, 층 표는 표현 속성, 채우기 표시기는 그리기만 소유한다. Core·Framework·다른 강조 층의 조건과 색은 수정하지 않는다. 기존 표시 계약에 의존하므로 기능별 변경이 독립적이고, 새 객체·매 Tick 조회·별도 메시 생성은 없다. 단, 표시되는 칸의 채우기 갱신 비용은 기존 채우기 경로에서 늘 수 있어 규모가 큰 스테이지에서 Profiler로 확인한다.
- Unity 확인 기대값: 살아 있는 적 본부에 호버하면 공급 범위 칸의 테두리와 윗면 채우기가 함께 보인다. 지형이 높은 칸도 해당 윗면에 붙고, 이탈·UI 가림·결과 재생·본부 파괴·전투 종료 시 둘 다 사라진다. 공격·이동·낙석 강조가 겹치면 기존 우선순위로 보인다. 두 필드 표시 모드를 확인한다.
- View 시험 코드는 작성하지 않고 MSBuild는 실행하지 않는다. Phase N7 코드 뒤 멈춰 보고한다. **컴파일·Unity 재생은 사용자 확인.**

### N7 구현 기록 (2026-11-06)

- 증원 범위 층의 `Fills`를 켜 기존 `FieldCellFillStack`과 `IdleFillLayers`가 호버 중 공급 범위 칸을 채우도록 했다. `FieldView` 툴팁을 수정했다. 색·불투명도·호버 조회·Core 계약은 바꾸지 않았다.
- 정적 검색으로 층 표와 툴팁을 확인했고 변경 코드의 `git diff --check`는 통과했다. **컴파일·Unity 재생 미확인.**
