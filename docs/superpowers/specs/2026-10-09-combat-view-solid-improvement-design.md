# 전투 View SOLID·SSOT 개선 설계 (2026-10-09)

- **근거:** [SOLID·SSOT 평가](2026-10-09-combat-view-solid-ssot-evaluation.md) §6의 개선 우선순위 1~5와 §5의 가독성 지적, [골격·View 평가](2026-10-09-combat-skeleton-view-evaluation.md)의 M1~M4.
- **기준 코드:** `2b25f64a`.
- **사용자 결정(2026-10-09):** 평가 항목을 모두 진행하고 Phase를 나눈다. 버튼 입력 규칙([Assets/AGENTS.md](../../../Assets/AGENTS.md))을 전투 View 버튼에도 적용한다. 구현은 Claude가 한다.
- **Task·실행 기록:** [View 계획](../plans/2026-10-01-combat-view-plan.md) §7.68.
- **상태:** 리뷰 완료(2026-10-09, Q1~Q3 추천안 확정). P1 완료, P2·P3 코드 완료(Unity 확인 대기). **2차 검토(§5)로 남은 Phase를 재계획 완료(D9·D11~D13, D10 철회). P4~P7 코드 완료(2026-10-10, D12는 "계약대로 이벤트만"으로 확정). 남은 것: Unity 확인, 보류 항목(VR6+R5·R6·평가 항목 5).**

## 0. 목표와 범위

| 평가 항목 | 이번에 하는 것 | 하지 않는 것 |
|---|---|---|
| 1 타임라인 출처·시점 통일 | Core 요청 메모 갱신(`CommandState.Timeline` 존재 반영, 확인할 질문 정리) | View 소비자 교체(VR6)는 Core 답변 뒤. Core 파일 수정 없음 |
| 2 전투 버튼 입력 규칙 | 버튼 7개를 `UIInputBinding`·`IUIInputReceiver`로 교체, 테스트를 EventSystem 전달로 변경 | 공용 `UIInputBinding` 수정 |
| 3 포기 UI·개발 패널 분리 | 두 컨트롤러로 분리, 결과 후처리 순서는 조정자에 유지 | 골격 이탈 순서 변경 |
| 4 표시기 해제 실패·상태 접근 | 해제 실패를 모으며 나머지 정리 계속, 강조 상태를 읽기 전용 계약으로 노출 | 표시 모드 추가 |
| 5 사건별 연출 분배·최적화 | **보류.** 착수 조건(연출 종류 증가 또는 측정 결과)이 아직 없다. 측정 절차만 계획에 남긴다 | 측정 없는 최적화 |
| 가독성(평가 §5, M1~M4) | 낡은 주석, 한 줄 다중 문장, 생성자 null 검사 | 동작 변경 |

**동작은 바꾸지 않는다.** 모든 Phase는 리팩터링이다. 사용자가 보는 버튼·확인창·완료 문구·입력 차단 동작은 같아야 한다.

## 1. 결정

### D1. 버튼 입력: Presenter가 수신자가 되고 슬롯으로 구분한다

| 버튼 | 소유 | 슬롯 | 받는 쪽 |
|---|---|---|---|
| 포기·확인·취소 | `CombatAbandonPanel`(MonoBehaviour) | 0·1·2 | 패널 → `Bind`로 받은 입구 |
| 복귀 | `CombatHud` | 0 | HUD → `BindReturn`으로 받은 입구 |
| 완료 확인 | `CombatHud` | 1 | HUD → `ShowCompletion`의 확인 콜백(세대 확인 유지) |
| 개발 배치·전투 | `CombatSceneDisplay`(일반 클래스) | 0·1 | 표시 → `ShowDevLayerPanel`로 받은 입구 |

- Presenter가 `IUIInputReceiver`를 **명시적 인터페이스 구현**으로 갖는다. 바깥에는 보이지 않는다.
- `UIInputBinding.Attach(button, this, slot)`를 버튼마다 **처음 연결할 때 한 번** 부른다. 실행 중 컴포넌트를 붙이므로 **Combat.unity 씬 작업이 필요 없다.** `CombatLogUI`가 같은 방식이다. 씬의 버튼 7개는 Inspector 리스너가 없음을 확인했다(`m_Calls: []`).
- `Button.onClick`에는 아무것도 연결하지 않는다. Hover는 쓰지 않으므로 빈 구현이다.
- 바깥 계약(`ICombatBattleDisplay.BindAbandon`·`BindReturn`·`ShowCompletion`·`ShowDevLayerPanel`)은 **바꾸지 않는다.** 그래서 `CombatViewSystem`과 테스트의 가짜 표시는 영향이 없다.
- 해제는 Presenter가 보관한 입구를 null로 두는 것이다. `Click`은 입구가 null이면 아무것도 하지 않는다. `UIInputBinding`은 버튼이 꺼졌거나 `interactable == false`면 전달하지 않는다(기존 `onClick`과 같은 조건).
- **해석 확인 필요(리뷰 질문 Q1):** AGENTS.md는 "`Button.onClick.AddListener`, delegate closures, Inspector UnityEvent로 화면 동작을 연결하지 말라"고 한다. 이 설계는 버튼 → Presenter는 `IUIInputReceiver`로, Presenter → 소유자(ViewSystem·골격)는 기존처럼 `Action` 입구로 잇는다. 버튼 이벤트에 대리자를 구독하지는 않는다. 이 해석이 규칙에 맞는지 확인이 필요하다. 아니라면 대안은 소유자(포기 컨트롤러 등)가 직접 `IUIInputReceiver`가 되고 표시 계약이 `IUIInputReceiver`를 받는 것이다. 이 경우 표시 계약과 가짜 표시가 바뀐다.

### D2. 포기 UI는 `CombatAbandonController`로 분리한다

- **새 파일:** `V/CombatAbandonController.cs`(internal sealed, 순수 C#).
- **소유 상태:** 확인창 열림 하나(`IsConfirmOpen`).
- **받는 것(생성자):** 실행 모드, 오류 경계, 사실 공급자(`Func<CombatInputFacts>`), 활성 확인(`Func<bool>`: 오류 아님·진입 활성·표시 연결), 입력 표시 정리(`Action`: 경로 취소 + Interaction 표시 정리).
- **표시 계약:** `ICombatBattleDisplay`에서 포기 메서드 네 개를 `ICombatAbandonDisplay`로 뽑는다. `ICombatBattleDisplay`가 이를 상속하므로 기존 구현·가짜는 그대로 컴파일된다(ISP).
- **입구:** `Attach(ICombatAbandonDisplay, ICombatAbandonRequest)`, `Sync()`(매 Tick), `Close()`(제출 정지), `Stop()`(오류 정지: 닫고 비활성), `Detach()`. 버튼 콜백 `Open`·`Confirm`·`Cancel`은 내부이며 지금처럼 try/catch로 오류 경계에 보고한다.
- **ViewSystem에 남는 것:** 생성·Attach·Tick에서 `Sync()` 호출·`SuspendSubmissions`/`StopInput`/`Detach`에서 정리 호출, 그리고 `Receive`의 "확인창이 열린 동안 입력 무시" 검사(`_abandon.IsConfirmOpen`을 읽는다). 입력 차단은 입력 입구의 책임이라 조정자에 둔다.
- `ICombatAbandonRequest`는 `BindServices`에서 받아 컨트롤러로 넘긴다. `BindServices` 서명은 바뀌지 않는다.

### D3. 개발 Layer 패널은 `CombatDevLayerController`로 분리한다

- **새 파일:** `V/CombatDevLayerController.cs`(internal sealed). 상태는 없다.
- **받는 것:** 실행 모드, 오류 경계, 사실 공급자, 입력 연결 확인(`Func<bool>`: 오류 아님·진입 활성·입력 연결·Interaction 있음), `IInputLayerService`, 표시 정리(`Action`).
- **표시 계약:** `ShowDevLayerPanel`·`HideDevLayerPanel`을 `ICombatDevLayerDisplay`로 뽑는다(D2와 같은 방식).
- **입구:** `Attach(display)`(RecordInputOnly면 보이고 연결, Live면 숨김), `Hide()`(오류 정지), 버튼 콜백 `Deployment`·`Combat` → 기존 `ReceiveDevLayer` 동작 그대로.
- **판단:** 분리해도 대리자 두세 개를 받으므로 줄어드는 양은 약 35줄로 작다. 그래도 RecordInputOnly 전용 기능을 조정자에서 빼서 Live 코드 경로를 읽기 쉽게 하는 효과가 있어 분리한다. 리뷰에서 과하다고 판단하면 D3만 빼도 다른 Phase에 영향이 없다(리뷰 질문 Q2).

### D4. 강조 상태는 읽기 계약으로만 노출한다

- **새 계약:** `IFieldHighlightStateReader`(`VD/`). 읽기 멤버만 둔다: `ColumnCount`·`RowCount`·`CellCount`·`HoverIndex`·`HoverState`·`IsPlacementActive`·`IsPathPlanning`·`IsActing`·`LayersAt`·`ZoneAt`·`HasAnyCell`·`TryGetIndex`.
- `FieldHighlightState`가 구현한다. `FieldView.HighlightState`의 반환 타입을 이 계약으로 바꾼다.
- `IFieldHighlightRenderer.Build(mapper, state)`도 이 계약을 받는다. 두 표시기는 지금도 읽기만 한다(사용 멤버 확인 완료).
- **변경 권한은 `FieldView`만 갖는다.** 단독 상태 테스트(`FieldHighlightStateTests`·`FieldOutlineRendererTests`)는 구체 타입을 직접 만들므로 영향이 없다.

### D5. 표시기 해제는 실패를 모으며 끝까지 진행한다

- `FieldOutlineRenderer.ReleaseHandles`: 핸들마다 try/catch, 실패를 모은다. 실패해도 그 슬롯의 핸들 참조·표시 캐시는 비운다.
- `FieldOutlineRenderer.Release`: 핸들 반환 → 데칼 숨김 → 상태 초기화(`_isBuilt`·`_state`·`_mapper`)를 각각 시도하고, 모은 실패를 마지막에 `AggregateException`으로 한 번 던진다. 상태 초기화는 실패와 관계없이 반드시 한다.
- `FieldCellColorRenderer.Release`도 같은 규칙: 기본·대체 풀 반환을 각각 시도한다.
- 기존 연출 정리(`CombatSceneDisplay.RunAll`, `OrderedCombatPresentation.Cancel`)와 같은 수준이 된다.

### D6. 타임라인은 Core 요청 메모만 갱신한다

- [타임라인 요청 메모](../handoffs/2026-10-06-combat-timeline-core-request-memo.md)에 추가한다: `CommandState.Timeline`이 이미 Buffer에 있고 `TimelineUI`가 읽는다는 사실, HUD는 아직 `GetTimeline()`을 부른다는 사실, 확인할 질문(같은 예측인지, 범위·길이, 게시·무효화 시점, 빈 값, 종료·재진입).
- 줄 번호를 현재 코드로 고친다. Core 경계 인계 §13.9에 갱신 사실을 한 줄 적는다.
- **전달은 사용자가 한다.** View 소비자 교체(VR6)는 답변 뒤 별도 Phase다.

### D7. 가독성 정리

| 대상 | 변경 |
|---|---|
| `CombatViewSystem` 클래스 주석 | 현재 구조(Router·Gate·Interaction·PathInput·포기/개발 컨트롤러)로 고침 |
| `CombatLifeCycle.cs:233` 주석 | 포기는 K22로 연결됨을 반영. **골격 파일**이라 주석만 고친다 |
| `ICombatPresentation.cs:15` | 요약 주석을 `Play`로 옮김 |
| `OrderedCombatPresentation`·`CombatMovementPlayback` | 한 줄 다중 문장 → Allman. 동작 변경 없음 |
| `CombatPathInput` 생성자 | 인자 7개 모두 `ArgumentNullException` 검사 |

`CombatSceneEventPresenter`의 한 줄 다중 문장도 같은 대상이지만, D7은 실행 경로를 건드리지 않는 범위로만 한다.

### D8. 평가 항목 5는 보류하고 측정 절차만 남긴다

- 착수 조건: 비이동 연출이 두 종류 이상 추가되거나, 아래 측정에서 사건·결과 처리 비용이 문제로 확인될 때.
- 측정 절차: Unity Profiler(Deep Profile 끔)로 유휴·배치 Drag·공격 호버·경로 드래그·자동 진행 10걸음 연속 구간의 프레임당 GC Alloc과 CPU 시간을 기록한다. 대상은 `CombatSceneEventPresenter.RenderEvents`, `ReadDisplayValues`, `TimelineUI.Update`, `FieldOutlineRenderer.Refresh`, `CombatPointerMapper.TryMapPointer`.

## 2. Phase 계획

> **2026-10-09 재계획:** 아래 표의 P4·P5는 §5.3(P4~P7)으로 대체됐다. P1~P3 기록은 그대로 둔다.

순서 근거: 문서·작은 수정부터 해서 회귀 위험이 낮은 것을 먼저 닫는다. 포기·개발 컨트롤러 분리(P3)를 버튼 교체(P4)보다 먼저 해서, P4의 EventSystem 테스트가 최종 구조를 대상으로 쓰이게 한다. P4는 바깥 계약을 바꾸지 않으므로 P3 뒤에 독립적으로 할 수 있다.

| Phase | 내용 | 바뀌는 파일 | 검증 |
|---|---|---|---|
| **P1 문서** | D6 메모 갱신. 평가 문서 오류 수정(골격·View 평가·상세 UML의 `CombatLogUI` 설명, SOLID 평가 §7 줄 번호). 상세 UML 머리에 "격자 아웃라인 이전 기준" 표시 | docs만 | 링크·줄 번호 대조 |
| **P2 표시기 안전** | D4·D5 | `VD/IFieldHighlightStateReader.cs`(새), `VD/FieldHighlightState.cs`, `VD/IFieldHighlightRenderer.cs`, `VD/FieldView.cs`, `VD/FieldOutlineRenderer.cs`, `VD/FieldCellColorRenderer.cs`, 테스트 | 새 테스트: 핸들 Dispose 중간 실패를 주입해도 나머지 반환·상태 초기화·예외 1회. 기존 `FieldOutlineRendererTests`·`FieldHighlightStateTests`·`CombatPointerMappingTests`·`WallAttackInteractionTests` 통과 |
| **P3 컨트롤러 분리** | D2·D3 | `V/CombatAbandonController.cs`(새), `V/CombatDevLayerController.cs`(새), `V/ICombatBattleDisplay.cs`(계약 분리), `V/CombatViewSystem.cs` | 기존 `CombatViewSystemTests`의 포기 4건·개발 패널 테스트를 **수정 없이** 통과(동작 보존 증거). 컨트롤러 단위 테스트 추가: 확인창 열림 중 이탈 시작 → 닫힘, 확인 시 요청 1회, 불가 시 무시, Stop 뒤 콜백 무시 |
| **P4 버튼 입력 규칙** | D1 | `VD/CombatAbandonPanel.cs`, `VD/CombatHud.cs`, `V/CombatSceneDisplay.cs`, `CombatDisplayReuseTests` | `onClick.Invoke()` 2곳 → `ExecuteEvents` pointerClick. 추가: Submit 전달, `interactable == false`면 무시, 재연결 시 이전 입구 무시, Release 뒤 무시, `onClick` 리스너 수 0(영구·런타임) |
| **P5 가독성** | D7 | 위 표 | 동작 변경 없음. 관련 테스트 통과 |
| (보류) VR6 | 타임라인 소비자 교체 | — | Core 답변 뒤 별도 설계 |

각 Phase가 끝나면 멈추고 보고한다. 컴파일 확인은 MSBuild(새 파일은 `CustomAfterMicrosoftCommonTargets`)로 하고, Unity EditMode와 재생 확인은 사용자에게 요청한다.

## 3. 위험과 지킬 것

- **P3:** 확인창 닫기 호출 지점 5곳(Attach·Detach·StopInput·SuspendSubmissions·Sync)을 하나라도 빠뜨리면 이탈 중 확인창이 남는다. 기존 테스트가 이 경로를 덮으므로 테스트를 고치지 않고 통과시키는 것을 완료 조건으로 둔다.
- **P3:** `Receive`의 입력 차단은 조정자에 남긴다. 컨트롤러로 옮기면 입력 경로가 컨트롤러에 의존하게 된다.
- **P4:** 완료 확인은 골격의 한 번만 확인 계약이 걸려 있다. 세대 확인(`_completionVersion`)과 "한 번 누르면 버튼 비활성"을 그대로 유지한다.
- **P4:** `UIInputBinding`은 `isActiveAndEnabled`·`Button.IsInteractable()`을 본다. 씬 비활성화·패널 숨김 중에는 전달하지 않으므로 기존 `onClick`과 같다.
- **P2:** `IFieldHighlightRenderer` 계약 변경은 View 소유 파일 안에서 끝난다. 격자 아웃라인 작업(View 계획 §7.62)이 Unity 확인 대기라 그 확인과 겹치지 않게 P2 결과를 따로 보고한다.
- 골격 파일은 D7의 주석 한 줄만 고친다. Core 파일은 고치지 않는다.

## 4. 리뷰 질문 (2026-10-09 사용자 결정: 모두 추천안)

| # | 질문 | 추천 |
|---|---|---|
| Q1 | D1의 해석(버튼 → Presenter는 `IUIInputReceiver`, Presenter → 소유자는 기존 `Action` 입구)이 AGENTS.md 규칙에 맞는가 | 이 해석. 표시 계약과 테스트 가짜를 바꾸지 않는다. 규칙의 대상은 버튼 이벤트 구독이다 |
| Q2 | D3(개발 패널 분리)를 할 것인가 | 한다. 효과는 작지만 Live 경로 가독성이 나아진다 |
| Q3 | P5에 `CombatSceneEventPresenter`의 형식 정리를 넣을 것인가 | 넣지 않는다. 사건 표시 경로라 연출 확장(보류 항목 5) 때 함께 본다 |

## 5. 2차 검토: `CombatViewSystem` 집중 검토와 재계획 (2026-10-09)

P3 뒤 사용자 요청으로 `CombatViewSystem`(895줄)을 다시 검토했다. 사용자는 **P4 이후 작업을 미루고 이 검토를 바탕으로 다시 계획**하기로 했다. 아래 §5.3이 남은 Phase의 원본이며 §2 표의 P4·P5를 대체한다.

### 5.1 발견 사항

| # | 내용 | 근거 | 판정 |
|---|---|---|---|
| R1 | **입력 수신 창구가 조정자에 섞여 있다.** `IStageInputReceiver` 10개 + `FocusLost`·`CaptureLost`·`RequestReturn`(모두 public), 입구 검사 `Receive`, 슬롯 연결 상태 `_isInputBound`와 `_slot.Bind(this)`/`Unbind(this)` | `CombatViewSystem.cs:43`, `:640-669`, `:725-752`, `:829-883` | 변경 이유가 다르다(새 키 추가가 조정자를 바꾼다). 분리 |
| R2 | **연결 해제 때 슬롯이 보내는 `SelectCanceled`가 버려진다.** `_isInputBound = false`를 먼저 하고 `Unbind`하므로 `Receive`가 반환한다. 주석("표현만 정리하도록")과 다르다 | `:723-733`, `StageInputReceiverSlot.Unbind` | 지금은 호출자 5곳이 먼저 표시를 정리해서 무해. 잠재 함정. R1과 함께 고친다 |
| R3 | **`BattleCleared` 분기는 도달할 수 없다.** 한 번 그린 뒤에는 `_battleInstance != 0`이라 `:232` 검사가 먼저 `Detach`한다. 테스트 `BattleCleared_ReleasesInput`도 실제로는 Detach 경로로 통과 | `:232-238`, `:267-275`, `CombatViewSystemTests.cs:237` | **사용자 결정: 지금 동작(전투가 사라지면 Detach) 유지.** 죽은 분기·신호를 지우고 설계를 현재 동작으로 고친다 |
| R4 | **`ICombatViewService`가 너무 넓다.** 바깥 사용자는 LifeCycle 하나(`ICombatExitPresentation`으로만). `Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn`은 서비스로 쓰이지 않고, `CaptureLost`는 운영 호출자가 없다 | `ICombatViewService.cs`, `CombatLifeCycle.cs:88` | 줄이자고 제안했으나 **사용자 결정으로 그대로 둔다**(D10 철회) |
| R5 | 타임라인 UI 열고 닫기(`SyncTimelineUI`)와 Core `GetTimeline` 직접 호출은 조정자의 일이 아니다 | `:607-634` | VR6과 함께(Core 답변 대기). 이번 계획에 넣지 않는다 |
| R6 | 경로 입력만 수명(Tick의 전투 설치 때 생성)과 응답 전달(매 Tick `OnPreview`·`RefreshReturnPreview`)이 다르다. `FinalizeResult` 끝의 `RefreshReturnPreview`(+`IsMoveMode`)는 매 Tick 호출과 겹친다 | `:282`, `:292-297`, `:603-604` | 동작 문제 아님. 이동 입력 회귀 위험이 있어 **선택 항목으로 보류** |
| R7 | "경로 취소 + 표시 정리" 두 줄이 `SyncInputToCore`에 두 번 반복. 클래스 주석이 옛 구조 | `:650-651`, `:665-666`, `:31-41` | 가독성 Phase에서 정리 |

**유지할 것:** `FinalizeResult`의 순서와 외부 호출 뒤 토큰 재확인, `StopInput`(대기까지 지움)과 `SuspendSubmissions`(대기·재생 보존)의 구분, 씬 탐색 1회, 정리 실패 수집.

### 5.2 결정

사용자 결정(2026-10-09): R3은 지금 동작 유지(D9), Q4는 추천안(`View.Input`), Q5는 **`ICombatViewService`를 그대로 둔다**(D10 철회). 이어진 질문("Tick에서 이렇게 많이 검사해야 하나", "전투 설치 처리는 왜 Tick에서 하나")에서 D12·D13을 더했다.

**D9. 전투가 사라지면 Detach한다(R3).**

- `CombatViewSystem.Tick`의 `BattleCleared` 분기와 `CombatRefreshSignal.BattleCleared`·`CombatViewRefresh`의 계산을 지운다.
- [View 설계](2026-09-30-combat-view-design.md) §5.3과 [상세 UML](2026-10-09-combat-skeleton-view-detailed-uml.md) §4.4 흐름도를 현재 동작으로 고친다: "전투가 사라지거나 다른 전투로 바뀌면 표시를 비우고 씬 연결을 끊는다. 같은 씬에서 다음 전투를 다시 붙이는 경로는 없다(전투마다 씬을 다시 로드)."
- 테스트: `BattleCleared_ReleasesInput`·`BattleCleared_ClearsDisplay_AndDetachReleasesItOnce`는 실제 동작(Detach)에 맞게 이름을 바꾸고, `CombatViewRefreshTests`의 정리 신호 단언을 지운다.
- 결과: 이탈 중 NewRun 대기 동안 필드가 비어 보이는 현상(상세 UML §5.9, 인계 O9)은 **의도한 동작**으로 기록한다.

**D10. (철회) 서비스 계약 축소.** 사용자 결정으로 `ICombatViewService`(`Attach`·`Detach`·`FocusLost`·`CaptureLost`·`RequestReturn` + `ICombatExitPresentation`)를 그대로 둔다. D11 뒤에도 이 세 입력 멤버는 `CombatViewSystem`에 남고 수신 객체로 넘기기만 한다.

**D12. 매 Tick 하던 상태 맞추기를 이벤트 시점으로 옮긴다.**

| 지금(매 Tick) | 옮길 곳 | 근거 |
|---|---|---|
| 포기 버튼 `_abandon.Sync()` | Attach(첫 표시 뒤), 새 결과를 처음 관측할 때, `SuspendSubmissions`, `StopInput`(이미 `Stop`) | 이탈 상태가 Idle을 벗어나는 곳은 `CombatExitSequence.Begin`뿐이고 Begin은 같은 호출 안에서 `SuspendSubmissions`를 부른다. Idle로 돌아오는 것은 다음 진입(새 Attach)뿐이다. 종료(Completion·Finished)는 Core 결과 게시(S 증가) 때만 바뀐다(포기는 Begin 뒤) |
| 종료 판정 `IsTerminal(ReadFacts())` → `SuspendSubmissions` | 새 결과를 처음 관측할 때(재생 시작 시, 결과당 1회)와 첫 표시 때 | 같은 이유. 첫 표시의 종료 상태는 지금도 `GetDesiredLayer`가 입력을 연결하지 않는다 |
| `CanContinue()` 6회 | 이름 있는 헬퍼 하나로 묶고 단계별 조기 반환 구조를 유지 | 외부 호출 안의 오류 보고·Detach가 재생 세대를 바꾸므로 방어 자체는 필요하다. 의미는 바꾸지 않는다 |

- 경로 입력의 매 Tick `OnPreview`·`RefreshReturnPreview`(R6)는 **이번에도 유지**한다. 복귀 미리보기는 다른 소유자가 슬롯을 비운 뒤 다시 요청하는 데 폴링을 쓴다.
- 완료 조건: 기존 `CombatViewSystemTests`의 포기(이탈 시작 → 버튼 비활성·확인창 닫힘)·종료(남은 결과는 소비, 새 입력만 차단) 테스트가 단언을 고치지 않고 통과.

**D13. 첫 그리기를 Attach에서 한다.**

- 근거: Combat 씬의 Enter 노드는 LifeCycle `OnEnter`(0)가 먼저 돈다. 그 안의 `Flow.ActivatePreparedBattle`이 `HasBattle`을 요구하고, View `OnCombatSceneEntered`(100)는 진입 활성을 요구한다. 그래서 **Attach 시점에는 항상 전투가 있다.** 지금의 `BattleInstalled` 처리는 Attach한 그 프레임의 첫 View Tick에서 한 번만 일어난다(Loading은 Global, View는 InGame 그룹). 이후에는 D9의 Detach 때문에 다시 일어나지 않는다. 씬이 먼저 뜨고 준비가 나중에 끝나던 옛 흐름(K20 이전)의 흔적이다. View 설계 §5.1도 "Attach → 최초 전체 그림"이라고 적고 있다.
- 변경: `AttachDisplay`가 진입 활성·`HasBattle`을 확인하고(아니면 `InvalidOperationException`) 끝에서 **Install → 경로 입력 생성 → Layer·입력 연결 → 포기 동기화** 순서로 첫 표시를 한다. "처음 그리기에 성공한 뒤에만 입력 연결"은 같은 메서드 안의 순서로 지킨다. Install 안의 재진입(오류·Detach)은 D12의 헬퍼로 확인한다.
- `CombatViewRefresh`에서 설치 신호(`BattleInstalled`)와 표시 이력(`_displayedBattle`)을 지운다. 남는 것은 결과 순번 일관성 검사와 미리보기 응답 번호뿐이다. facts의 `IsBattleDisplayed`는 "Attach(첫 표시) 성공"으로 계산한다.
- 예외 경로는 같다. Attach도 Runtime Tick 안(Loading 노드)에서 불려 같은 오류 경계로 간다.
- 테스트:
  - `Rig.AttachAndShowBattle()`(49곳 사용)의 순서를 "전투 설치 → Attach"로 바꾼다. 쓰는 쪽은 고치지 않는다.
  - 전투가 나중에 생기는 상황을 전제한 테스트는 계약에 맞게 고친다. "Attach 전 전투 없음/진입 비활성이면 거부"로 바꾸거나 첫 표시를 Attach 기준으로 바꾼다. 대상: `BeforePreparation_NoInputIsBound`, `PreparedBuffer_BeforeEntryActivation_DoesNotDisplayOrReceiveInput`, `ActiveEntry_DoesNotReceiveInputUntilFirstDisplayReturns`, `EntryClosedDuringFirstDisplay_DoesNotBindInput`, `FirstDisplay_OfFinishedBattle_DoesNotBindInput`, `InstallFailure_Propagates_WithoutBindingInput`, `Detach_CancelsAndReleases_AndOldCompletionDoesNotFinishTheNextAttach`, `Attach_Twice_IsRejected`, `Attach_AfterFault_DoesNothing`.
  - `CombatMovementViewTests`의 Attach 도우미 3곳, `CombatDisplayReuseTests`의 `view.Attach(root); view.Tick(0)`도 순서를 맞춘다.
  - `CombatViewRefreshTests`의 설치·정리 신호 테스트는 지운다.
- 이 테스트들은 대부분 "의미가 바뀐 테스트"다. 기존 동작 보존의 증거는 결과 소비·입력·포기·종료 테스트가 단언 변경 없이 통과하는 것으로 둔다.

**D11. 입력 수신을 `CombatInputReceiver`로 분리한다(R1·R2).**

- **새 파일:** `V/CombatInputReceiver.cs`(internal sealed, `IStageInputReceiver`). 소유: 슬롯 연결 상태(`IsBound`), `Bind()`·`Unbind()`, 입구 검사와 오류 보고.
- **받는 것:** `IStageInputReceiverSlot`, 오류 경계, 입구 허용(`Func<bool>`: 진입 활성·Router 있음), 포기 확인창 열림(`Func<bool>`), 분배(`Action<Request, float, float>`: 조정자가 `_router.Route(..., _characters.Characters)`로 연결).
- **입구 검사(지금 `Receive`와 같음):** 오류 아님 ∧ 연결됨 ∧ 입구 허용, 확인창이 열려 있으면 `FocusLost`·`SelectCanceled`만 통과, 분배 예외는 오류 경계로.
- **R2 수정:** `Unbind()`는 슬롯의 `Unbind(this)`가 보내는 `SelectCanceled`를 **연결 상태인 채로** 받아 Router가 표현을 정리하게 하고, `finally`에서 `IsBound = false`로 내린다. 주석을 실제 동작에 맞춘다.
- **조정자에 남는 것:** 목표 Layer 계산과 `ChangeLayer`/`ClearLayer`(`SyncInputToCore`·`ReleaseInput`), facts의 `IsInputAvailable`(= `_receiver.IsBound ∧ …`), **`ICombatViewService` 멤버 `FocusLost`·`CaptureLost`·`RequestReturn`(수신 객체로 넘김, D10 철회)**. `ReleaseInput`은 `_receiver.Unbind()` 호출로 바뀐다.
- **씬·HUD 연결:** `CombatSceneRoot.FocusLost`·`display.BindReturn`은 지금처럼 조정자의 서비스 멤버를 가리키고, 그 멤버가 수신 객체로 넘긴다.
- **public 표면:** `CombatViewSystem`에서 `IStageInputReceiver` 전용 메서드 10개가 빠진다. 테스트 입구는 `internal CombatInputReceiver Input`(Q4).
- **테스트:** `IStageInputReceiver` 메서드 직접 호출을 `View.Input.X()`로 기계적으로 바꾼다. `FocusLost`·`CaptureLost`·`RequestReturn` 호출은 서비스 멤버로 남으므로 그대로다. 새 `CombatInputReceiverTests`: 미연결·오류·확인창 필터, 분배 예외 보고, **Unbind 중 `SelectCanceled`가 분배됨**, `Unbind` 실패해도 `IsBound == false`.

**R6은 보류한다.** 이동 입력은 Unity 재생 확인이 많이 필요한 영역이다. 필요해지면 별도 설계로 다룬다.

### 5.3 남은 Phase (재계획, §2의 P4·P5를 대체)

| Phase | 내용 | 바뀌는 파일 | 검증 |
|---|---|---|---|
| **P4 Tick·변경 신호 정리** | D9(도달 불가 정리 분기 제거), D12(포기 동기화·종료 판정을 이벤트 시점으로, 재진입 확인 헬퍼), D13(첫 그리기를 Attach로, 설치 신호 제거) | `V/CombatViewSystem.cs`, `V/CombatViewRefresh.cs`, `V/CombatRefreshSignal.cs`, `CombatViewSystemTests`·`CombatViewRefreshTests`·`CombatMovementViewTests`·`CombatDisplayReuseTests`의 Attach 순서와 의미가 바뀐 테스트, View 설계 §5.1·§5.3, 상세 UML §4.4 | 사용자에게 보이는 동작 변경 없음. 결과 소비·입력·포기·종료 테스트는 단언 변경 없이 통과. 의미가 바뀐 테스트는 목록대로 수정 |
| **P5 입력 수신 분리** | D11(R1·R2) | `V/CombatInputReceiver.cs`(새), `V/CombatViewSystem.cs`, 테스트 입구 변경, `CombatInputReceiverTests`(새) | 동작 변경은 R2 하나(Unbind 중 표현 정리가 실제로 실행됨). 기존 입력 회귀 테스트 통과 |
| **P6 버튼 입력 규칙** | D1(옛 P4) | `VD/CombatAbandonPanel.cs`, `VD/CombatHud.cs`, `V/CombatSceneDisplay.cs`, `CombatDisplayReuseTests` | §2의 옛 P4 검증과 같음 |
| **P7 가독성** | D7(옛 P5) + R7 | 옛 P5 대상 + `SyncInputToCore`의 반복을 `CancelInputPresentation()`으로, 클래스 주석을 **최종 구조**로 | 동작 변경 없음 |
| 보류 | R6 경로 입력 수명·응답 일관화(매 Tick 폴링 포함) | — | 필요할 때 별도 설계 |
| 보류 | R5 + VR6 타임라인 | — | Core 답변 뒤 |
| 보류 | 평가 항목 5 사건별 연출 분배 | — | D8 조건 |
| 철회 | D10 서비스 계약 축소 | — | 사용자 결정(Q5) |

**순서 근거:** P4는 `Tick`과 `CombatViewRefresh`를 같이 고치는 세 결정을 한 번에 끝내 조정자의 흐름을 먼저 단순하게 만든다. P5는 입력 경로를 바꾸므로 버튼 교체(P6)보다 먼저 한다. 클래스 주석은 구조가 확정된 뒤 P7에서 한 번만 고친다.

**P4 위험:** 첫 표시를 Attach로 옮기면 Attach가 실패할 때 씬 연결 상태가 반쯤 남을 수 있다. 첫 표시 실패 시 지금처럼 오류 경계로 가고 `StopInput`·`Detach`가 정리하는지(`InstallFailure_Propagates_WithoutBindingInput`의 새 형태)를 완료 조건에 포함한다.

### 5.4 리뷰 질문 (2026-10-09 결정)

| # | 질문 | 결정 |
|---|---|---|
| Q4 | D11의 테스트 입구 | `View.Input.X()`(internal 접근자) |
| Q5 | `CaptureLost` 삭제·서비스 축소 | **하지 않는다.** `ICombatViewService`를 그대로 둔다(D10 철회) |
