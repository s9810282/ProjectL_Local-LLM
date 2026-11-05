# 전투 View 구조 평가 (2026-10-15)

- 작성: View 작업, 2026-10-15. 사용자 요청: 현재 전투 View 구조를 SOLID 5원칙·SSOT·Framework·조립 분리·기능 변경의 독립성으로 평가하고 구현 계획을 세운다.
- 기준 코드: HEAD `4cfceaa4`(작업 트리 깨끗). R0~R4·D12와 SD-A~SD-D5가 반영된 상태다.
- 판단 기준: [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)(§4.1 SSOT, §4.2 SOLID, §4.3 FW, §4.6 기능 변경의 독립성), [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~016.
- 검토: 초안을 읽기 전용 검토 에이전트 3개(조정자·조립·Framework / 입력 기능 경로 / 표시·재생 경로)가 코드와 대조했다. 이 문서는 그 지적을 반영한 판이다. 검토 요지는 [구조 개선 계획 §12](../plans/2026-10-15-combat-view-structure-improvement-plan.md)에 있다.
- 구현 계획·Task: [구조 개선 계획](../plans/2026-10-15-combat-view-structure-improvement-plan.md). 이 문서는 평가와 개선 방향만 적는다.
- 선행 평가(중복하지 않음): [10-09 SOLID·SSOT 평가](2026-10-09-combat-view-solid-ssot-evaluation.md), [10-14 리팩토링 설계](2026-10-14-combat-view-structure-refactor-design.md), [SD-D 인수 평가](2026-10-15-combat-scene-display-composition-acceptance.md).

## 0. 범위

**대상:** `ViewSystem/Combat/`(조정자·입력·재생·표시 입구)와 `Display/`, `Factory/Runtime/InGame/View/`의 전투 조립, View 그룹 등록.

**제외(기존 사용자 결정·외부 대기, 기록만):**

| 항목 | 근거 |
|---|---|
| Audio 호출 위치·프리셋 정책 | 리팩토링 설계 §0 사용자 결정(현강님 코드). 발견 사항은 §6에만 적는다 |
| `GetTimeline` Core 직접 조회 | VR6, Core 타임라인 계약 대기 |
| `ICombatViewService` 축소 | 개선 설계 D10 철회(사용자 Q5) |
| 전체 입력 피드백 이전(SD-C1) | 후속 계획에서 보류 |
| 미리보기 응답 전달 재설계 | 개선 설계 R6 보류. 이번 평가에서도 별도 설계 대상으로 둔다(E4) |
| Core·Buffer·FieldCamera·Architecture, 씬·프리팹·DB 값 | CLAUDE.md 경계 |

## 1. 결론 요약

SD-D로 **표시 쪽**은 조립(Factory)과 이동·배치·공격 표시 구현이 분리됐다. 남은 구조 문제는 주로 **조정자 `CombatViewSystem`(869줄)의 입력 쪽**과 **입력 기능 사이의 비대칭**이다. 표시 쪽에서 이미 고친 "조정자가 협력 객체를 직접 만들고 하나하나 부른다"는 형태가 입력 쪽에는 남아 있다. 여기에 **조립·설치 실패 경로의 소유권 공백**(E7·E15·E23)이 있다.

| 기준 | 판정 | 요지 |
|---|---|---|
| S 단일 책임 | 부분 | 조정자가 Framework 수명·결과 순서 외에 입력 객체 생성, 입력 기능별 호출, 행동 버튼 갱신 시점을 함께 소유(E1·E2·E3) |
| O 개방-폐쇄 | 부분 | 결과 전후처리에 참여하는 새 입력 기능은 조정자·Router·Interaction 세 곳을 고쳐야 함(E4). 새 사건·새 표·새 공격 종류는 한 곳(R0·D4·D10) |
| L 리스코프 | 양호 | 해제 뒤 규칙(D9)·재생 계약(D6) 명시. Gate는 facts 사본 대신 현재 값을 보도록 문서화돼 있음(E14 수용) |
| I 인터페이스 분리 | 양호(비용 있음) | 소비자는 좁은 계약을 받는다. 다만 재생 계약을 소비하지 않는 내부 객체 둘이 구현을 선언해 수정 범위가 넓다(E9) |
| D 의존성 역전 | 부분 | 표시는 Factory가 선택. 입력은 조정자와 `CombatInteraction` 생성자가 하위 기능 객체를 `new`(E1). Router는 후설정 setter로 연결(E18) |
| SSOT | 부분 | Buffer 사실 복제 없음(양호). 원자 값의 원본은 하나지만 "연결 활성" **판정식**이 여러 변형으로 흩어짐(E5), 포인터 대상 판정 결과가 facts 사본으로 전달됨(E19), 공격 방향 벡터가 Core 정의를 복제(E20, 기록) |
| Framework | 부분 | FW-001/002/003·입력 Submit·씬 검색 한 곳은 지킨다. 조용한 반환 경로에서 씬 루트 구독이 해제되지 않는 예외 경로 1건(E15, 지침 §20·21) |
| 조립 분리 | 부분 | 표시 조립은 Factory, 입력 조립은 조정자 안(E1). 표시 서비스 묶음도 조정자가 `new`(E16). Factory 안에 검증 순서 때문에 생긴 클로저 결합(E17) |
| 기능 변경의 독립성 | 부분 | 표시 기능은 해당 기능 중심으로 수정 가능(SD-D). 입력 기능·행동 버튼 변경은 조정자를 함께 읽어야 함(§4.4) |

## 2. 현재 구조 지도

```text
InGameViewSystemFactory ──► CombatViewSystem(869) ── ICombatViewService / ICombatProgressView
                         └► CombatTimelineViewSystem(181, Buffer 파생, 독립)

CombatViewSystem
├─ 생성자: CombatResultPlayback, CombatAbandonController(람다 4), CombatDevLayerController(람다 3)
├─ PostBindService: new CombatDisplayServices(...) / BindServices: CombatInputReceiver(람다 3)
├─ Attach → CombatSceneDisplayFactory.Create(...) ─► CombatSceneDisplay(616, 26 참조)
├─ AttachDisplay: new Gate / Interaction / Router, Router.SetCursorSnap(new CursorSnap), SetPointerOcclusion
├─ ShowBattle: Install 성공 뒤 new CombatPathInput → Router.SetPathInput
├─ Tick: Refresh 신호 → PathInput.OnPreview·RefreshReturnPreview(매 Tick) → Interaction.PreviewAnswered(응답 때) → ConsumeResult
└─ FinalizeResult: Display.Refresh → Gate.Resolve → Layer 맞춤 → Interaction.AfterResult → 처리 번호 삭제·확인 → PathInput.RefreshReturnPreview → 행동 버튼

입력: Receiver → Router ─┬─ Path   → CombatPathInput(566) ─ new CombatMovementInteraction, CombatMovementRangeRequest
                         └─ 그 밖 → CombatInteraction ─ new PlacementDragInteraction / new AttackTargetingInteraction
재생: CombatResultPlayback → OrderedCombatPresentation → CombatSceneDisplay(가드) → CombatSceneEventPlayback → CombatSceneEventPresenter
      (Playback·Presenter도 ICombatEventPlayback 선언)                                ├ new CombatAttackPlayback / CombatUnitMotionPlayback / Pacing
                                                                                     └ new CombatRailwayContactPlayback(Playback 안)
```

## 3. 발견 사항

판정: **개선**(계획에 넣음) / **조건부**(사용자 결정 필요) / **수용**(남는 공통 경계로 인정) / **기록**(범위 밖·후속).

### 3.1 조정자·조립·실패 경로

| ID | 원칙 | 위치 | 문제 | 대표 변경 시나리오의 영향 | 판정 |
|---|---|---|---|---|---|
| E1 | 조립 분리·D·S | `CombatViewSystem.cs:371-377·397`, `CombatInteraction.cs:47-48` | Attach 수명 입력 객체를 조정자가 직접 생성·연결하고, `CombatInteraction` 생성자가 배치·공격 기능 객체를 같은 인자로 직접 만든다. 표시 쪽(SD-D1)과 비대칭. **기능 내부 부품**(경로 안의 Movement·RangeRequest, 배치·공격 안의 HoverTracker)은 상위 기능 수명에 묶여 있어 E10과 같은 이유로 수용한다 | `AttackTargetingInteraction`에 의존성 하나 추가 → 공격 구현 + `CombatInteraction` 생성자(전달만) + 조정자 Attach + 테스트 직접 생성 4곳. 단, 조정자가 아직 받지 않은 새 Global 서비스면 PostBindService·BindServices·테스트 Rig도 바뀐다 | 개선 |
| E3 | SSOT(시점)·기능 독립성 | `CombatViewSystem.cs:136·601·624·794·840·851` | 행동 버튼 값의 규칙(`CombatActionButtons.Read`)과 갱신 깔때기(`SyncActionButtons:858`)는 한 곳이지만 "언제 부르나"가 6곳에 흩어져 있다. Layer 변경 때는 :651→:840→:624 순서로 한 프레임에 중간값을 두 번 계산한다. 같은 패널의 복귀 버튼은 이미 매 Tick 경로 입력이 갱신한다(`CombatPathInput.cs:491-517`) | 버튼에 영향을 주는 새 입력 상태 추가 → 규칙 + 조정자의 호출 지점 누락 여부 전수 확인 | 조건부(Q2) |
| E5 | SSOT(판정식) | `CombatViewSystem.cs:132·135·220·751·777·799`(부분 조건 :146·806·860·685-686) | 원자 값(`_faults`·`_entry`·`_display`·`_playback`·`IsBound`)의 원본은 하나지만 "씬에 연결된 활성 전투" **판정식**이 6가지 변형으로 반복된다 | 수명 조건 하나 추가(예: 포커스) → 6곳을 일관되게 수정해야 하고 빠뜨려도 컴파일이 잡지 못함 | 개선 |
| E7 | 소유권·수명 | `CombatViewSystem.cs:333·343-360` | Factory가 만든 Display를 `_display`에 저장하기 전(`_inputWrite.Clear`·`_presentation.Attach`·`_playback.Attach`) 예외가 나면 그 Display를 해제할 주인이 없다. 운영에서는 로딩 노드 → Bootstrap 오류 정지로 재진입하지 않지만, Shutdown의 Detach가 `_display == null`(:431)로 바로 돌아가 Release가 불리지 않는다. 재Attach하는 테스트 환경에서는 `FieldThorns_Inactive`가 남아 쌓인다 | 조립 실패 뒤 대여 자원·씬 Configure 상태가 반환되지 않음 | 개선 |
| E15 | §20·21·소유권 | `CombatViewSystem.cs:350·334-336·431` | `AttachDisplay`의 오류 상태 조용한 반환은 Display를 인수하지 않은 채 정상 반환한다. 이어서 `Attach`가 씬 루트 이벤트를 구독하지만, Detach는 :431에서 먼저 돌아가 구독 해제가 빠진다 | 오류 상태에서 씬 진입 → 구독 누수·Display 미반환 | 개선 |
| E16 | 조립 분리 | `CombatViewSystem.cs:183-188` | 표시 서비스 묶음을 조정자가 Global 그룹에서 골라 `new`한다. 표시 표 묶음은 `CombatDisplayTables.From`(:157)이 고른다 | 표시에 Render 서비스 하나 추가 → 묶음 + 조정자 + Factory + Rig | 개선(저비용) |
| E23 | 소유권·수명 | `CombatViewSystem.cs:390-404·242-298·777` | Install(첫 그리기) 실패 뒤에도 `_display`·`_playback`이 남아 `CanContinue`가 참이다. 결과가 게시되면 반쯤 설치된 표시에서 재생·후처리를 시도한다. 운영은 오류 정지로 Tick이 멈추고, 테스트(`CombatViewSystemTests.cs:1174-1190`)는 결과를 게시하지 않아 드러나지 않는다 | 입력 객체 생성을 Install 뒤로 옮기면(E2 정리) 이 경로가 NRE가 된다 | 개선(동작 변경, Q5) |
| E13 | SSOT(경미) | `CombatTimelineViewSystem.cs:41-42` | 씬 Enter/Exit 우선순위 값이 전투 View와 같다. 그러나 둘 다 LifeCycle 0 기준의 상대 위치일 뿐 같은 사실은 아니고, 같다는 사실은 `CombatTimelineViewSystemTests.cs:66-68`이 고정한다. Timeline이 전투 View 상수를 참조하면 D11 독립성을 깬다 | — | 수용 |

### 3.2 입력 기능 경로

| ID | 원칙 | 위치 | 문제 | 대표 변경 시나리오의 영향 | 판정 |
|---|---|---|---|---|---|
| E2 | S·기능 독립성 | `CombatViewSystem.cs:283-284·397-398·490-494·539·599·837`, 간접 `:752` | 조정자가 입력 기능을 둘로 나눠 안다: 경로(`_pathInput` 직접 호출 6곳)와 배치·공격(`_interaction`). facts의 경로 대상 판정도 조정자가 Router 판정식과 `_interaction.IsAttackActive`로 만든다 | 경로 입력의 결과 후처리 변경 → 조정자 Tick/Consume/Finalize/Stop/취소를 함께 읽어야 함 | 개선 |
| E4 | O·기능 독립성 | `CombatInputRouter.cs:92-111·171-197`, `CombatInteraction.cs`, `CombatViewSystem.cs:280-294` | 입력 기능 수명 계약이 비대칭이다. 배치·공격은 합성 객체의 수명 메서드, 경로는 Router 후설정 + 조정자 직접 호출. 경로는 **매 Tick 호출 하나에 응답 소비·수명 감시·복귀 견적 재요청을 함께 싣는다.** 응답 번호 필터는 이미 소비자마다 한다 | 새 입력 기능(예: 사용 아이템) 추가 → 조정자·Router·Interaction 수정. 전달 방식 자체를 바꾸는 것은 R6 재설계(제외) | 개선(구조) |
| E6 | S·기능 독립성 | `CombatPathInput.cs:491-566`(관련 :75-136·163-174·354-383, 약 150줄) | 복귀 요청·견적이 경로 입력 안에 있다. 결합: 경로 취소(`CancelInputs`)가 언제나 복귀 요청을 지우고, 복귀 견적 갱신은 지정 중·Movement 활성·국면·단일 미리보기 슬롯·이동 누적값을 함께 본다. 입구 판정(`AllowsInput`)도 공유한다 | 복귀 견적 조건 변경 → 약 150줄을 읽고 슬롯 경쟁 검증이 따라붙음. 객체로 분리하면 양방향 의존(정리 통지·지정 중 질의)이 생기고 읽을 범위는 약 100줄로만 준다 | 개선(축소: 값 객체·이름 분리) |
| E18 | D·조립 | `CombatInputRouter.cs:92-103`, `CombatViewSystem.cs:375-377·398` | Router가 커서 스냅·UI 가림·경로를 후설정 setter로 받는다. 스냅·가림은 Router 생성 시점에 이미 값이 있다 | 스냅 의존성 추가 → Router + 조정자 + 테스트 3곳 | 개선 |
| E19 | SSOT | `CombatViewSystem.cs:752`, `CombatInputRouter.cs:173-174`, `CombatPathInput.cs:81` | 포인터 대상 규칙(`ResolvePointerTarget`)은 한 곳이지만, 조정자가 그 결과를 facts의 `IsPathTarget`으로 만들어 경로에 넘긴다. 경로를 입력 묶음에 넣으면 경로 → facts → Interaction의 순환 읽기가 생긴다 | 포인터 대상 규칙 변경 → 세 곳 확인 | 개선(facts에는 원 사실, 판정은 소비자가 규칙 호출) |
| E21 | 명명 | `CombatPathInput.cs:493-498` | `RefreshReturnPreview` 안에 경로 지정 수명 감시가 숨어 있어, 복귀만 고치다 경로 감시를 깰 수 있다 | — | 개선(E6과 함께) |
| E22 | 탐색성 | `CombatPathInput.cs:125-127·343`, `CombatMovementInteraction.cs:99-101` | 운영 호출자가 없는 public 메서드가 읽을 API를 늘린다(테스트 사용 여부 확인 필요) | — | 개선(저비용) |
| E14 | SSOT(경미) | `CombatCommandGate.cs:35-39`, Placement·Attack 호출부 | facts에 있는 `HasPendingCommand`와 별도 인자가 함께 넘어간다. Gate 쪽은 "facts가 입력 시작 시점의 사본이어도 대기 여부는 현재 값으로" 확인하도록 **의도적으로** 설계됐고 테스트가 그 의미에 기댄다(`CombatInputRouterTests.cs:96-103`) | — | 수용 |

### 3.3 표시·재생 경로

| ID | 원칙 | 위치 | 문제 | 대표 변경 시나리오의 영향 | 판정 |
|---|---|---|---|---|---|
| E8 | I·기능 독립성 | `ICombatBattleDisplay.cs:18`, `CombatSceneDisplay.cs:262-591` | 계약 10개 직접 합성(전이 포함 18개), Display public 멤버 63개 중 전달 58개. 직접 구현 가짜는 `CombatViewSystemTests` FakeDisplay 1개지만 이를 상속한 테스트 클래스가 8파일 10개 | 멤버 **추가**는 계약 + Display + 기능 + 가짜 1곳. **서명 변경**은 상속 10곳까지 퍼진다. 해제 뒤 규칙(D9) 원본이 한 곳이라는 이득과 교환 | 수용(재검토 조건 §5) |
| E9 | I·L | `CombatSceneEventPlayback.cs:18`, `CombatSceneEventPresenter.cs:19` | 두 내부 객체가 `ICombatEventPlayback`을 선언하지만 운영·테스트 어디서도 그 계약으로 쓰이지 않는다(Playback은 Presenter를 구체 타입으로 들고 계약 밖 `BeginConcurrentDeath`도 부른다). 계약 멤버 하나 추가에 컴파일러가 강제하는 수정이 5파일(선언·Display·Playback·Presenter·가짜). 또 "가드 없는 내부 객체도 타입상 연출 `Attach`에 넘길 수 있다"는 위험을 주석으로만 막는다(SD-B) | 선언을 거두면 강제 수정 3파일, 잘못된 연결은 컴파일 오류 | 개선 |
| E10 | 조립 분리 | `CombatSceneEventPresenter.cs:40-57`, `CombatSceneEventPlayback.cs` 생성자 | 사건 Presenter가 공격·이동·간격 재생 부품을 직접 만든다. 생성자 인자 9개 중 5개가 부품 전달용. 재생 세대(`Func<int>`)를 Presenter가 소유해 Factory로 옮기면 생성 순서가 꼬인다. 유닛별 공격 설정은 DB → `FieldUnitPresenter.TryGetAttackPresentation`으로 오므로 생성자를 거치지 않는다 | 씬 전역 연출 설정 추가일 때만 재생 부품 + Presenter 생성자 + Factory | 수용 |
| E11 | S | `CombatSceneDisplay.cs:193-211` | Refresh의 활성 행위자 칸 변환(19줄). Phase E 이후 바뀐 적이 없고 직접 테스트도 없다. 옮기면 Display 참조가 26 → 27 | — | 수용(SD-D5 §4 후속 후보 유지) |
| E12 | 탐색성·접근 | `ICombatMovementPresenter.cs`(3개), `ICombatPresentationControl.cs`(4개), `ICombatMovementPlaybackPresenter.cs`(2개), `ICombatMovementFeedback.cs` | 파일 이름과 다른 계약이 섞여 있고 이 4파일의 10개 계약만 `public`이다. 재생 계약 조각 6개(`ICombatResultPresenter`·`ICombatPresentationControl`·`ICombatMovementStartPresenter`·`ICombatMovementEndPresenter`·`ICombatPresentationSequencePresenter`·`ICombatPresentationBarrier`)는 `ICombatEventPlayback` 합성 외 소비자가 없다. `ICombatMovementPresenter`도 소비자 0 | "읽어야 할 코드"를 찾는 비용. 동작 영향 없음 | 개선(저비용) |
| E17 | 조립 순서·SSOT(검증) | `CombatSceneRoot.cs:94-99`, `CombatSceneEventPresenter.cs:47-48`, `CombatSceneDisplayFactory.cs:70-77` | step 값 검증이 사건 Presenter 생성자에만 있어(간격·회전은 루트와 Presenter 이중 검증), 가시밭 표시를 Presenter 뒤에 만들려고 할당 전 클로저 캡처가 생겼다. step이 잘못되면 유닛·손패·고스트 Configure 뒤에 실패한다 | 조립 순서를 바꿀 때 클로저 결합을 알아야 함 | 개선(저비용) |

### 3.4 기록(범위 밖·후속)

| ID | 위치 | 내용 |
|---|---|---|
| E20 | `AttackTargetingInteraction.cs:427-441` | 조준 방향 → 칸 변화 벡터를 "Core의 방향 정의와 같은"이라는 주석과 함께 View가 복제한다(P6·SSOT). 조준 기하(`:371-442`)와 기록 모드 행위자(`:116-133·492-498`)가 500줄 클래스에 함께 있다. Core Contracts에 방향 벡터가 공개되면 그것을 쓰도록 Core 전달 문서에 요청 후보로 적는다. 기하 함수 분리는 조준 종류가 늘 때 검토 |
| — | `CombatViewSystem.cs:136` vs `:835-841` | 개발 Layer 전환의 표현 취소에는 경로 취소가 빠져 있다(일관성). 입력 묶음 정리 때 현재 동작을 보존하고 이유를 확인한다 |
| — | `CombatViewSystem.cs:280-293` | 경로 폴링 뒤 `CanContinue` 재확인 없이 응답 신호 처리로 넘어간다. 현재 동작 유지 |
| — | `CombatSceneDisplay.cs:126-130·216·228-232`, Factory | 새 필드 표시 하나를 더하면 Factory·Display 생성자·필드·`_fieldBound`/Clear·Release·Install/Refresh 6지점을 고친다(시나리오 S-H). 수용, 재검토 조건: Clear와 Release 목록 사이 누락이 실제로 생길 때 |
| — | 사건 Presenter `_shownCharacters`(:91-93) | Refresh마다 Buffer에서 다시 만들고 사건 스냅샷으로만 바뀌는 **파생 표현 상태**다. SSOT 위반 아님 |
| — | 재생 세대 카운터 6종 | Playback 토큰·Ordered·사건 Playback·사건 Presenter·기차 접촉·반응 시작 순번은 서로 다른 범위의 늦은 콜백을 막는다. 하나로 대체할 수 없다(후속 설계 "취소 범위가 다른 세대 번호는 유지"). 다만 사건 Playback과 Presenter의 필드 이름이 같고 Presenter 주석이 "이 값 하나만 원본"이라 오독하기 쉽다 |
| — | `FieldUnitPresenter`(560줄) | 대여·HUD 슬롯·회전·연출 조회가 같은 ID별 View 대여 원본 위에 있다. 나누면 매핑을 복제한다. 수용 |
| — | `CombatSceneRoot.cs:26-32` 연출 시간 값 | 값마다 원본이 하나라 SSOT 위반 아님. D12(View DB) 방식과 배치만 다르다. 씬·DB 변경 제외 범위 |
| — | `_movementStarted` | Ordered(유닛별)와 Pacing(행위자)에서 같은 이름이 다른 사실을 뜻한다. 이름 주의 |
| — | D9 "연결 때 전투만" | 조정자 :246·365, Timeline :105·144, 경로 :68·79 세 곳. D11 독립성의 대가로 수용. 입력 판정식에 더하지 않는다 |
| — | `CombatInputFacts` 선택 인자 17개 | 새 사실을 추가해도 테스트가 조용히 기본값을 쓴다. 조립 위치는 한 곳이므로 수용, 새 인자는 기본값 없이 추가 권고 |

## 4. 원칙별 세부

### 4.1 SOLID

- **S:** 조정자의 변경 이유는 ① Framework·씬 수명 ② 결과 소비 순서 ③ 입력 Layer·수신 연결 ④ 입력 객체 조립 ⑤ 입력 기능별 호출 ⑥ 행동 버튼 갱신 시점 ⑦ facts 조립 ⑧ 진행 계약(완료 표시 포함) ⑨ 표시 값 읽기다. ①②⑧은 조정자에 남는 것이 맞다. ③⑦은 입력 정책 원본과 함께 바뀌어 옮길 이득이 작다. **④⑤는 개선 대상**(E1·E2), **⑥은 조건부**(E3, Q2)다.
- **O:** 표시 쪽은 새 사건(R0)·새 표(D4)·새 공격 종류(D10)가 한 곳이다. 입력 쪽은 결과 전후처리에 참여하는 새 기능이 조정자·Router·Interaction에 분기를 더해야 한다(E4).
- **L:** 해제 뒤 규칙과 재생 계약은 명시돼 있다. Gate의 "현재 값 확인"은 문서화된 계약이다.
- **I:** 소비자별 계약은 좁다. 소비하지 않는 계약을 선언한 내부 객체(E9)와 소비자 없는 계약 조각(E12)이 수정 범위를 넓힌다.
- **D:** 전역 서비스는 인터페이스로 받는다. 구체 협력 객체 선택이 표시는 Factory, 입력은 조정자·생성자 내부로 나뉜다(E1). Router 후설정(E18).

### 4.2 SSOT

| 사실 | 원본 | 판정 |
|---|---|---|
| 결과 순번 S·확인 P·처리 번호 | Buffer | 양호(조정자는 읽기만, 확인은 마지막) |
| 재생 중 결과·후처리 | `CombatResultPlayback` | 양호 |
| 시작/턴 종료 대기 | `CombatCommandGate` | 양호(현재 값 확인은 의도, E14) |
| 포기 요청·확인창 | `CombatAbandonController` | 양호 |
| 타임라인 보임 | Buffer 파생, `CombatTimelineViewSystem` | 양호 |
| 행동 버튼 값 | 규칙 `CombatActionButtons.Read`·깔때기 `SyncActionButtons` | 규칙 양호, 호출 시점 분산(E3) |
| 연결 활성 판정 | 원자 값은 하나, 판정식 6변형 | **문제**(E5) |
| 포인터 대상 | 규칙 `ResolvePointerTarget` 한 곳 | 결과 사본이 facts로 전달(E19) |
| 사건 Presenter 표시 사실 | Refresh마다 Buffer에서 파생 | 양호(파생 표현 상태) |
| 공격 방향 벡터 | Core | View 복제(E20, 기록) |
| BGM 프리셋 | 3곳 | 제외(Audio) |

### 4.3 Framework·JMH

- FW-001/002: `ICombatViewService`·Timeline 모두 제공 인터페이스 + `AddModule`(`CombatViewSystem.cs:308-309`). 내부 협력 객체를 System·Service로 등록하지 않음(§4.6 준수).
- FW-003·지침 §11: Builder 한 번(`InGameViewSystemFactory.cs:44`). 조립 뒤 구조 변경 없음.
- FW-005~007: 입력은 `IStageInputReceiverSlot`·`IInputLayerService` 경로만 사용.
- 지침 §20·21·DR-008: `CombatSceneRoot.FocusLost/Disabled` 구독은 정상 경로에서 Detach가 해제한다. **오류 상태 조용한 반환 경로에서 해제가 빠진다(E15).**
- 지침 §25: 씬 검색은 `OnCombatSceneEntered`(:510) 한 곳.
- 지침 §27·28: 계획은 새 계층·범용 등록·무타입 묶음을 만들지 않는다. 입력 기능 묶음(`CombatInteraction`)이 세 기능의 분배자가 될 때 Router와 분기가 이중으로 생기는 것이 God class 위험이다. 묶음은 상태·정책 없이 고정 순서 위임만 갖는다.
- §4.5 공용 파일: 새 Factory를 `Factory/Runtime/InGame/View/`에 두면 민형님 공유가 필요하다. 미전달인 `handoffs/2026-10-15-combat-scene-display-factory-framework-share.md`에 절을 더해 한 번에 전달하는 것을 권한다.
- 프레임 순서(행동 버튼 판단 근거): 키·마우스 입력은 `UnityInputBridge`의 `actionTriggered`로 Runtime Tick보다 먼저 동기 분배된다. UI 버튼(EventSystem.Update)과 `GameBootstrap.Update`의 순서는 정해져 있지 않다. Tick 안 그룹 순서는 Buffer → Global → InGame(Core → View) → LifeCycle이다.

### 4.4 조립 분리·기능 변경의 독립성 — 대표 시나리오

| 시나리오 | 지금 수정·이해·검증 범위 | 개선 뒤 목표 |
|---|---|---|
| S-A 공격 조준 입력에 새 의존성(조정자가 이미 받는 것) | 공격 구현 + Interaction 생성자 + 조정자 + 테스트 생성 4곳 | 공격 구현 + 입력 Factory + 해당 fixture |
| S-B 경로 입력의 결과 후처리 변경 | 경로 입력 + 조정자 Tick/Consume/Finalize/Stop/취소 | 경로 입력 + 입력 묶음의 같은 수명 메서드 |
| S-C 행동 버튼에 영향 주는 새 상태 | 규칙 + 조정자 6곳 점검 | Q2=A면 규칙 한 곳 |
| S-D 복귀 견적 조건 변경 | 경로 입력 약 150줄 + 슬롯 경쟁 검증 | 이름 붙은 복귀 메서드·값 객체 + 같은 검증(슬롯 경쟁은 남음) |
| S-E 연결 수명 조건 추가 | 조정자 6곳 | 이름 있는 판정식 한 곳 |
| S-F 재생 계약 멤버 추가 | 선언 + Display + Playback + Presenter + 가짜 | 선언 + Display + 가짜(+ 실제 구현 위치) |
| S-G 새 표시 의도 API | 계약 + Display 전달 + 구현 + 가짜 | 변화 없음(E8 수용) |
| S-H 새 필드 표시 추가 | Factory + Display 6지점 | 변화 없음(수용, §3.4) |

## 5. 수용하는 공통 경계

- **단일 표시 입구와 해제 가드(E8):** 해제 뒤 규칙(D9)을 한 곳에 두는 대가로 전달 메서드를 유지한다. 재검토 조건: `ICombatBattleDisplay`를 **직접 구현**하는 클래스가 2개 이상이 되거나, 한 기능의 API 추가가 Display의 다른 기능 영역 수정을 요구할 때.
- **사건 Presenter·기능 내부 부품 생성(E10, E1의 내부 부품):** 재생 세대·상위 기능 수명과 묶여 있다. 재검토 조건: 씬 전역 연출 설정이 반복해서 생성자를 거칠 때, 또는 부품 교체 요구가 생길 때.
- **facts 조립(⑦)·Layer 동기화(③):** 입력 정책의 원본(Policy)과 함께 바뀌므로 조정자에 둔다.
- **결과 소비 순서:** "그린 뒤 대기 해제 → Layer → 재요청 → 처리 번호 삭제 → 확인 → 확인 뒤 갱신"과 각 외부 호출 뒤 재진입 확인(토큰)은 조정자가 소유한다. 입력 기능 쪽으로 옮기지 않는다.
- **Gate의 현재 값 확인(E14)·우선순위 상수(E13)·재생 세대 카운터·`FieldUnitPresenter`·새 필드 표시 6지점(S-H):** §3 근거대로 수용.

## 6. 범위 밖 기록

- **Audio:** BGM 프리셋이 `CombatViewSystem.PresentCompletion:689`(1)·`CombatSceneDisplay.Refresh:174`(밤 3·낮 2)·`CombatSceneEventPresenter:425-430`(CombatEntered 3·NightEnded 2)에 있고 `ShowCompletion:238`에 승리음이 있다. 유닛별 효과음 프리셋은 `CombatSceneEventPresenter.cs:444-465`의 `EntityKey` switch 하드코딩이다(기획 값이므로 View DB 원본 후보). Interaction·Placement·Attack도 `IAudioSystemService`를 받고, 입력 기능이 재생 쪽 `CombatSceneEventPresenter.UnitPreset`에 의존한다(Placement:187, Attack:130·139). 현강님과 정리할 때 참고한다.
- **VR6:** `ReadDisplayValues:615`의 `GetTimeline` 직접 조회와 길이 상수.
- **R6:** 미리보기 응답 전달 통일은 "전달 규칙 통일"이 아니라 경로의 응답 소비·수명 감시·복귀 재요청을 나누는 재설계다. 단일 미리보기 슬롯에 "비워짐" 신호가 없는 것이 폴링의 근본 이유다.
- **Core 요청 후보:** E20 공격 방향 벡터 공개.
