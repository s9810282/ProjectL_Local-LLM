# 전투 View 구조 재평가 (2026-10-15, SV 이후)

- 작성: View 작업, 2026-10-15. 사용자 요청: "일단 리펙토링 여기까지 하고 현재구조 SOLID 5원칙, SSOT, Framwork, 조립 분리와 기능별 구현 분리 방향을 다시 평가해줘".
- **리팩토링은 SV7(코드)·SV8(인수 문서)에서 멈춘다(사용자 결정, 2026-10-15).** 이 문서는 구현하지 않는다. 평가와 다음 방향 후보만 적는다.
- 기준 코드: HEAD `4cfceaa4` + SV0~SV7 작업 트리(미커밋).
- 판단 기준: [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)(§4.1 SSOT, §4.2 SOLID, §4.3 FW, §4.6 기능 변경의 독립성), [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~016.
- 선행 문서와의 관계:
  - [SV 평가](2026-10-15-combat-view-structure-evaluation.md)(E1~E23)는 SV 이전 구조를 평가했다.
  - [SV 인수](2026-10-15-combat-view-structure-acceptance.md) §5는 SV 이전과 비교한 상대 판정이다.
  - 이 문서는 지금 구조의 절대 판정이다. 그리고 지금까지 써 온 분리 방향이 계속 쓸 만한지 본다. 이전 문서와 겹치는 근거는 링크로 대신한다.
- 검증 전제:
  - 동작 증거는 MSBuild, 관리 실행, Unity 에디터 컴파일까지다(인수 §6).
  - **Unity Test Runner 결과는 아직 받지 못했다.** 동작에 기대는 판정(L, 해제 규칙)은 그 결과에 따라 바뀔 수 있다.

## 0. 범위

- **대상:** SV 평가 §0과 같다. `ViewSystem/Combat/`, `Display/`, `Factory/Runtime/InGame/View/`의 전투 조립, View 그룹 등록.
- **제외:** SV 평가 §0과 같다. Audio 호출 위치·프리셋, `GetTimeline`(VR6), `ICombatViewService` 축소(D10 철회), SD-C1, R6 미리보기 응답 전달, 그리고 Core·Buffer·FieldCamera·Architecture·씬·프리팹·DB 값이다. 이 항목들은 판정 근거로 적을 수는 있다. 하지만 처리 방향을 정하지 않는다.

## 1. 결론

**방향은 맞고, 계속 써도 된다.** 지금 구조는 다음 세 규칙으로 나뉜다.

- 조립은 Factory가 한다.
- 기능 구현은 기능별 클래스에 둔다.
- 조정자는 수명과 순서만 맡는다.

이 형태가 표시 쪽(SD-D)과 입력 쪽(SV4·SV5)에 똑같이 적용됐다. 그래서 "새 기능을 어디에 두나"에 답이 있다. Framework 계약 위반은 없다. Buffer 사실의 복제도 없다.

**남은 문제는 계층마다 하나씩 있는 공유 허브에 모여 있다.** 분리 축은 기능이 아니라 기술 계층(입력·표시·재생)이다. 계층마다 여러 기능이 지나가는 허브가 하나씩 있다.

- 조정자 `CombatViewSystem`(972줄)
- 입력 묶음 `CombatInteraction`(274줄)
- 표시 입구 `CombatSceneDisplay`(616줄)
- 피드백 계약 `ICombatInteractionFeedback`(18멤버)

기능 하나를 바꾸면 그 기능의 파일들과 함께 허브 1~3개를 지나간다(§4). 허브를 지나는 것 자체는 설계상 대가다. 해제 규칙과 순서를 한 곳에 두기 위한 것이다. 다만 허브가 **자기 일이 아닌 기능 지식**을 갖는 지점이 남아 있다. 그 지점은 세 군데다.

1. **조정자가 아직 가진 진행 표시와 읽기 책임(N1·N2):** 완료 표시 수명, 사실(facts) 조립, 표시 값 읽기다. 조정자가 Buffer 읽기 싱크 7개를 모두 들고 있는 이유가 이 셋이다.
2. **큰 입력 기능 2개의 내부 혼합(N3·N4):**
   - 경로 입력(584줄)은 계획, 초안 표시 캐시, 이동 범위, 복귀 견적을 함께 가진다.
   - 공격 조준(500줄)은 모드, 범위 응답, 호버 미리보기, 조준 기하, 제출을 함께 가진다.
3. **피드백 계약의 ISP 위반(N5):** 배치·공격·호버가 겹치지 않는 멤버 묶음을 계약 하나로 공유한다. 표시 쪽 구현은 이미 `CombatPlacementFeedback`·`CombatAttackFeedback`으로 나뉘어 있다.

| 기준 | 판정 | 요지 |
|---|---|---|
| S 단일 책임 | 부분 | 조정자 변경 이유 9개 가운데 고유한 것 4개, 옮길 수 있는 것 3개(N1·N2), 외부 대기 2개. 큰 입력 기능 2개는 내부에 하위 기능이 섞여 있다(N3·N4) |
| O 개방-폐쇄 | 부분 | 새 Core 사건·새 View 표·새 공격 연출 종류는 한 곳. 새 입력 기능은 기능 + 묶음 + Factory. 새 표시 의도는 계약 + 입구 + 기능 + 가짜 2개(N5·N6). 새 행동 버튼·포인터 대상·진행 표시 단계는 조정자를 고친다 |
| L 리스코프 | 양호 | 해제 뒤 규칙(명령형은 던짐, 피드백형은 무시, D9)과 재생 계약(D6)이 명시돼 있고, 가짜 표시도 같은 계약을 따른다 |
| I 인터페이스 분리 | 부분 | 소비자 대부분은 좁은 역할 계약을 받는다. 예외는 `ICombatInteractionFeedback`이다. 배치 7·공격 9·호버 2로 서로 겹치지 않는 세 소비자가 18멤버 계약 하나를 받는다(N5). 합성 계약 `ICombatBattleDisplay`(10개)는 조정자 전용이라 수용 |
| D 의존성 역전 | 양호 | 구체 구현 선택은 Factory 2개가, 조정자는 Factory·묶음·역할 계약만 안다. 남은 직접 의존은 `GetTimeline`(VR6)과 오디오(제외)다. 조정자 소유 상태 객체(Gate·재생·포기·개발 Layer)는 소유자가 직접 만들어 수용 |
| SSOT | 양호(잔여) | Buffer 사실 복제 없음, 판정식(수명·포인터 대상·step·슬롯 소유)은 한 곳씩. 잔여: 경로 입력이 가진 표시 상태 거울(N3), 같은 연결 수명 안의 전투 정체성 사본(N8), 방향 벡터 Core 복제(E20), 오디오 프리셋·타임라인 길이(제외) |
| Framework | 양호 | FW-001/002/003, 입력은 Submit, 씬 검색 한 곳, 구독 해제(E15 해소). 공용 Factory 폴더 추가 공유 문서는 미전달(절차 항목) |
| 조립 분리 | 양호 | 표시·입력·표시 서비스 묶음 모두 Factory/`From`. Router 생성만 조정자에 있다(조정자의 사실 공급자를 받으므로 수용). 입력 Factory 서명이 기능 의존과 함께 자란다(인자 8개, §3 N2 참고) |
| 기능별 구현 분리 | 부분 | 계층 안에서는 기능별 클래스로 나뉘어 있다. 계층을 가로지르는 기능 변경은 허브를 지난다(§4). 큰 입력 기능 2개는 파일 안에서 하위 기능이 섞인다. `Combat/` 61개 파일이 평면 폴더라 기능 경계가 위치로 드러나지 않는다(N9) |

## 2. 현재 구조 지도

```text
InGameViewSystemFactory ──► CombatViewSystem(972) ── ICombatViewService / ICombatProgressView
                         └► CombatTimelineViewSystem(181, Buffer 파생, 독립)

CombatViewSystem (조정자)
├─ 생성자: CombatResultPlayback, CombatAbandonController, CombatDevLayerController (조정자 소유 상태, 수명 판정식만 넘김)
├─ PostBindService: CombatDisplayServices.From(global)
├─ Attach → CombatSceneDisplayFactory.Create → AttachCreated → TakeDisplay(소유권) → ContinueAttach → ShowBattle
│                                                                          └ CreateInput: new Gate, CombatSceneInputFactory.CreateSceneInteraction, new Router
├─ Tick: TickCore(전투 정체성 → 연출 Tick → 신호 → Poll → PreviewAnswered → ConsumeResult) → 행동 버튼 파생
├─ FinalizeResult: Display.Refresh → Gate.Resolve → Layer → Interaction.AfterResult → 삭제·확인 → AfterAcknowledged
└─ 진행 계약: SuspendSubmissions, PresentCompletion(+BGM), CompletionDisplay, IsAbandonRequested

입력: Receiver → Router(ResolvePointerTarget) → CombatInteraction ─┬ PlacementDragInteraction(288)
                                                                  ├ AttackTargetingInteraction(500)
                                                                  └ CombatPathInput(584) ─ CombatMovementInteraction(397), RangeRequest, ReturnRequest
표시: CombatSceneDisplay(616, 역할 계약 10개, 해제 가드 24곳, Clear 12·Release 22항목)
      ├ 피드백: CombatPlacementFeedback · CombatAttackFeedback · CombatMovementPlanningPresenter · Field(호버)
      └ 정적·동적: FieldView · FieldUnitPresenter(560) · HandPresenter · CombatHud · 지형·오브젝트·레일·가시밭·프랍
재생: CombatResultPlayback → OrderedCombatPresentation → CombatSceneDisplay(가드) → CombatSceneEventPlayback → CombatSceneEventPresenter(492)
                                                                                        ├ CombatAttackPlayback(335) · CombatUnitMotionPlayback · Pacing
                                                                                        └ CombatRailwayContactPlayback
```

## 3. 남은 문제

심각도는 변경 빈도와 잘못 고칠 위험을 함께 본 판단이다. 처리 방향은 후보이며 결정이 아니다.

### N1 조정자의 진행 표시 책임 — 중간

- **위치:** `CombatViewSystem.PresentCompletion`, `SuspendSubmissions`, `CompletionDisplay`, `_shownCompletion`.
- **무엇:** 전투 Plan이 읽는 진행 계약(`ICombatProgressView`)은 조정자 상태(제출 정지·완료 표시)의 공개 입구다. 이것이 결과 소비 순서와 같은 클래스에 있다.
  - 완료 표시는 수명 판정, 콜백 무효화 조건 4개, 확인 상태 전환, BGM 전환을 한 람다에 담는다.
  - 표시 쪽 `ShowCompletion`은 승리 효과음을 재생한다.
- **영향:** 완료 표시에 단계를 더하면 조정자를 고친다. 예를 들면 보상 화면, 패배 연출, 확인 전 지연이다. 포기는 같은 종류의 진행 표시인데 이미 `CombatAbandonController`로 나가 있다. 그래서 두 진행 표시의 형태가 비대칭이다.
- **방향 후보:**
  - 포기 컨트롤러와 같은 형태로 완료 표시 소유자(가칭 `CombatCompletionController`)를 둔다.
  - 조정자는 수명 판정식(`CanContinue`)과 "일반 결과 확인 뒤" 조건만 넘긴다.
  - BGM 줄은 Audio 제외 범위라 위치만 옮기고 정책은 바꾸지 않는다.
  - 선례가 있어 비용이 작다.

### N2 사실 조립과 표시 값 읽기 — 낮음~중간

- **위치:** `ReadFacts`(17값), `ReadDisplayValues`, `CreateInput`의 Factory 인자.
- **무엇:**
  - `ReadFacts`의 17값은 Buffer 출처 9개(국면·단계·종료·결과 번호 두 개·활성 행위자·미처리 명령·현재 응답 등)와 View 상태 8개(첫 그리기·실행 모드·Layer·후처리 중·대기·연출 중·입력 가용·공격 모드)를 섞어 만든다.
  - `ReadDisplayValues`는 Currency·CurrentProgress·Core 조회(`GetTimeline`)를 읽는다. **이 세 의존성은 이 메서드에서만 쓰인다.**
  - 입력 Factory는 Buffer 싱크 4개 + 표시 + Gate + 사실 공급자 + 오디오를 받는다(8개). 조정자는 받기만 하고 쓰지 않는 싱크를 Factory에 넘기려고 들고 있다.
- **영향:** 새 입력 사실이 생기면 `CombatInputFacts`·`ReadFacts`·소비자가 함께 바뀐다(인수 S-I ①). 새 표시 값이 생기면 조정자에 의존성이 하나 더 들어온다.
- **방향 후보:**
  - (a) 표시 값 읽기를 별도 읽기 객체로 옮긴다(Plan 쪽 `CombatProgressReader` 선례). VR6에서 `GetTimeline`이 Buffer로 바뀔 때 함께 하면 한 번에 끝난다.
  - (b) facts의 Buffer 부분만 읽기 객체로 옮긴다. View 상태 8개는 조정자 상태라 효과가 작다. 지금은 권하지 않는다.
  - (c) Factory 인자를 Buffer 싱크 묶음 하나로 받는다. 다만 무타입 가방이 되지 않게 하려면 이름 있는 값 묶음이어야 한다. 의존이 더 늘 때 검토한다.

### N3 경로 입력 내부 혼합 — 중간

- **위치:** `CombatPathInput`(584줄, 상태 필드 약 25개, 세대 번호 3종 `_planningVersion`·`_draftDisplayVersion`·`_rangeGeneration`).
- **무엇:** 네 하위 기능이 한 클래스에 있다.
  - ① 계획 입력: `CombatMovementInteraction`에 위임한다.
  - ② 초안 표시의 같은 값 생략: `_shownPath`·`_shownPreview`·`_shownPointerValid`·`_shownCandidate`와 세대 번호.
  - ③ 이동 범위 요청·표시: `ConsumeMovementRange`·`ClearMovementRange`.
  - ④ 복귀 견적: SV7에서 `CombatReturnRequest`로 상태를 줄였다.
- **SSOT:** ②는 "지금 무엇이 그려져 있나"의 거울을 입력 쪽이 가진 것이다.
  - 표시가 다른 경로로 지워지면 거울이 낡는다. 다른 경로란 Clear·Release·취소다.
  - 그래서 `InvalidateDraftDisplay`와 세대 번호로 거울을 무효화한다.
  - 원본은 표시 쪽(`CombatMovementPlanningPresenter`)에 있어야 자연스럽다.
- **영향:** 이동 범위 규칙을 바꾸면 계획·초안·복귀 코드와 같은 파일, 같은 세대 번호 규칙을 읽어야 한다.
- **방향 후보:**
  - (a) 이동 범위를 소유자 하나로 뺀다. `CombatMovementRangeRequest` 옆에 범위 요청·표시 수명을 두는 형태다.
  - (b) 같은 값 생략을 표시 쪽으로 옮긴다. 테스트가 리플렉션으로 `_shownPath`를 읽으므로 이름·위치 변경은 그 테스트와 함께 바꿔야 한다.
  - (a)가 먼저다. (b)는 표시 쪽 계약(같은 값이면 다시 그리지 않음)을 정해야 한다.
- **2026-10-19 실행([경로 입력 분리 계획](../plans/2026-10-19-combat-path-input-split-plan.md)):**
  - ③ → (a) `CombatMovementRangeTracker`
  - ④ → `CombatReturnEstimateTracker`
  - ② → (b)를 바꿔, 거울을 옮기지 않고 표시를 멱등으로 만든 뒤(HUD 비용 표시) 입력 거울을 지웠다
  - 그 사이 이동 모드 범위가 더해져 `CombatPathInput`은 781줄까지 컸다가 676줄이 됐다. ①과 입력 게이트·공격 Drag 넘겨받기가 남는다.

### N4 공격 조준 내부 혼합 — 중간~낮음

- **위치:** `AttackTargetingInteraction`(500줄).
- **무엇:** 다음이 한 클래스에 있다.
  - 모드 진입·이탈, 기록 모드 행위자
  - 범위 요청·응답 적용
  - 호버 미리보기 요청
  - 조준 기하(`AimFor`·`NearestDirectionAim`·`TryGetDirectionVector`)
  - 제출, 비용·체력 미리보기 판단
  - 조준 기하는 순수 계산이다. 방향 벡터는 Core 정의의 복제다(E20).
- **영향:** Direction 조준 규칙을 바꾸면 미리보기 요청·제출과 같은 파일을 읽는다. Core가 방향 벡터를 공개해도 교체 지점이 상태 있는 클래스 안에 있다.
- **방향 후보:**
  - 조준 기하를 순수 정적 클래스로 뺀다(가칭 `CombatAttackAim`). 입력은 칸·조준 목록·원점, 출력은 `AttackAim?`이다.
  - 상태가 없어 분리 비용이 가장 작다.
  - 관리 실행으로 바로 시험할 수 있다.
  - E20이 해소되면 그 한 곳만 바꾼다.

### N5 피드백 계약 ISP — 중간(비용 작음)

- **위치:** `ICombatInteractionFeedback`(18멤버). 소비자는 `PlacementDragInteraction`·`AttackTargetingInteraction`·`CombatHoverTracker`다.
- **무엇:** 세 소비자가 쓰는 멤버가 겹치지 않는다.

  | 소비자 | 쓰는 멤버 수 | 쓰는 멤버 |
  |---|---|---|
  | 배치 | 7 | 고스트·원본 |
  | 공격 | 9 | 범위·영향·차단·비용·체력 |
  | 호버 | 2 | — |

  표시 쪽 구현은 이미 `CombatPlacementFeedback`·`CombatAttackFeedback`으로 나뉘어 있다. 계약만 합쳐져 있다.
- **영향:** 공격 피드백 하나를 더하면 배치 기능이 의존하는 계약이 바뀐다. 그러면 테스트 가짜 `FakeFeedback`과 `FakeDisplay`를 둘 다 고친다. 배치와 공격이 계약을 통해 묶인다.
- **방향 후보:**
  - 계약을 `ICombatPlacementFeedback`·`ICombatAttackFeedback`·`ICombatHoverFeedback`으로 나눈다.
  - `ICombatInteractionFeedback`은 셋의 합성으로 남겨도 된다.
  - 그러면 `CombatSceneDisplay`·Factory 수정이 최소다.
  - 이 작업은 소비자 생성자 타입과 Factory 전달만 바뀌는 기계적 작업이다.

### N6 표시 입구 전달 비용 — 수용 유지(E8)

- `CombatSceneDisplay`는 역할 계약 10개를 구현한다. 해제 가드 24곳, Clear 목록 12항목, Release 목록 22항목이 있다.
- 새 표시 의도 API는 입구에 전달 메서드를 하나 더한다. 새 필드 표시는 Clear·Release 목록에 수동으로 넣는다(인수 S-G·S-H).
- 해제 뒤 규칙(D9)을 한 곳에 두는 대가로 유지한다.
- **재검토 조건:**
  - 목록 누락이 실제로 생긴 경우
  - 역할 계약별 가드 래퍼처럼 전달을 없애는 방법이 싸지는 경우
  - N5 분리 뒤 입구 크기가 계속 자라는 경우

### N7 재생 전달 사슬 — 수용

- 사슬: `OrderedCombatPresentation` → `CombatSceneDisplay`(가드) → `CombatSceneEventPlayback` → `CombatSceneEventPresenter` → 공격·이동 재생.
- 재생 계약 멤버 추가는 강제 3파일 + 실제 구현이다(SV3 뒤, 인수 S-F).
- 새 사건 종류는 Presenter 매핑 한 곳이다(R0). 그래서 계약 멤버 추가는 드물다.

### N8 같은 연결 수명 안의 전투 정체성 사본 — 낮음

- `_battle`을 가진 곳은 셋이다.
  - 조정자 `_battleInstance`
  - 경로 입력 `_battle`
  - 이동 상호작용 `_battle`
- 셋 다 Buffer `BattleInstanceId`를 거의 같은 순간에 찍는다.
- 조정자는 전투가 바뀌면 Detach한다. 그래서 경로 입력의 비교는 방어용 중복이다.
- 이동 상호작용의 값은 Core 응답 문맥(`MovementPreviewContext`) 대조에 쓰므로 정당하다.
- `CombatTimelineViewSystem`의 값은 독립 시스템이라 별개다.
- **방향:** 그대로 둔다. 경로 입력을 다시 고칠 일이 생기면 조정자가 연결 수명 값을 생성 인자로 넘긴다.

### N9 평면 폴더 — 낮음

- `Combat/`에 `.cs` 61개가 한 폴더에 있다. 조정자·입력·재생·계약·값 객체가 섞여 있다.
- 기능 경계가 클래스로는 있지만 위치로는 보이지 않는다.
- 폴더를 나누면 네임스페이스 규칙상 네임스페이스가 바뀐다. 다른 작업자 브랜치와 병합 충돌 비용도 크다.
- **방향:** 지금은 권하지 않는다. 큰 병합이 없는 시점에 `Input/`·`Playback/`·`Contracts/` 하위 폴더를 검토한다. 그때 .meta GUID를 유지하고, MonoBehaviour면 `MovedFrom`을 붙인다.

### N10 검증 안전망 — 구조 밖, 절차 항목

- 구조 변경의 안전망은 Unity Test Runner다. 그런데 결과가 창에만 남아 Claude가 대조할 수 없다(이번 SV8).
- `.meta` GUID 손상으로 fixture 하나가 2026-10-13부터 빠져 있었는데 아무도 몰랐다(인수 §7).
- 관리 실행으로 돌릴 수 없는 fixture(Unity 네이티브 경로)도 많다.
- **방향 후보:**
  - Unity 명령행 실행(`-runTests -testPlatform EditMode -testResults <경로>`)으로 결과 XML을 남긴다. 에디터가 열려 있으면 같은 프로젝트로 실행할 수 없으니, 사용자가 에디터를 닫은 시점에 한다.
  - 새 `.cs` 추가 때 `.meta` GUID 형식 검사(32자리 16진수)를 확인 목록에 넣는다(빌드 확인 메모에 이미 적음).

## 4. 기능 단위 변경 시나리오 (현재 기준)

인수 문서 §3의 S-A~S-I는 SV 전후 비교였다. 아래는 **기능 단위**로 다시 고른 시나리오다. 열의 뜻은 다음과 같다.

- **허브:** 해당 기능 밖이면서 여러 기능이 함께 쓰는 지점이다.
- **판정 기준:** 허브 0개면 양호, 허브가 수용한 경계뿐이면 수용, 허브가 기능 지식을 고쳐야 하면 부분이다.

| 시나리오 | 고칠 곳 | 허브 | 판정 |
|---|---|---|---|
| 새 공격 연출 종류(애니메이션) | `Display/Combat*Animation` + View 표 | 0 | 양호(D10) |
| 새 Core 사건 표시 | `CombatSceneEventPresenter` 매핑 | 0 | 양호(R0) |
| Direction 조준 규칙 변경 | `AttackTargetingInteraction` 내부 | 0 | 부분: 파일 안에서 미리보기·제출과 섞임(N4) |
| 공격 피드백 표시 하나 추가 | 피드백 계약 + `CombatSceneDisplay` 전달 + `CombatAttackFeedback` + 공격 조준 + 가짜 2개 | 2(계약·입구) | 부분: 계약 쪽은 N5, 입구 쪽은 수용(N6) |
| 배치 고스트 표시 변경(기존 API 안) | `CombatPlacementFeedback`·`PlacementGhost` | 0 | 양호 |
| 이동 범위 표시 규칙 변경 | `CombatPathInput` 내부 + `CombatMovementPlanningPresenter` | 0 | 부분: 계획·초안·복귀와 같은 파일(N3) |
| 복귀 조건 변경 | `CombatPathInput`의 복귀 메서드 3개 + `CombatReturnRequest` | 0 | 양호(SV7) |
| 결과 전후처리에 참여하는 새 입력 기능 | 기능 + `CombatInteraction` + 입력 Factory | 1~2 | 수용(인수 S-I, 조건부로 조정자) |
| 새 입력 사실(예: 모드에 따른 새 포인터 대상) | `CombatInputFacts` + `ReadFacts` + 판정식 호출자 | 조정자 | 부분(N2) |
| 새 행동 버튼 | `CombatActionButtons` + 패널 + 표시 계약 + Router 요청 + 수신 창구 + 조정자 `BindActions`·대리자 | 3 | 수용: UI 배선의 고유 비용, 판단은 `CombatActionButtons` 한 곳 |
| 완료 표시 단계 추가 | 조정자 `PresentCompletion` + 표시 계약 + `CombatHud` | 조정자·입구 | 부분(N1) |
| 새 필드 표시(정적 층) | Factory + 입구 Clear·Release 목록 | 입구 | 수용(N6) |
| 연결 수명 조건 추가 | `IsSceneActive` 한 곳 | 조정자(판정식) | 양호(SV2) |
| 재생 계약 멤버 추가 | 계약 + 입구 + 사건 재생 + 구현 + 가짜 | 입구 | 수용(N7) |

요약하면 표시·재생 기능은 대부분 양호하거나 수용이다. "부분"은 큰 입력 기능 2개의 내부(N3·N4), 피드백 계약(N5), 조정자의 진행 표시·사실(N1·N2)에 모인다.

## 5. 방향 평가

### 5.1 조립 분리 방향 — 유지

- Factory는 구체 구현을 고르고 연결만 한다(정적, 상태 없음). 조정자는 Factory 호출 결과의 소유권만 받는다(`AttachCreated`·`TakeDisplay`). 조립 실패 소유권도 이 경계에서 정해진다(SV1).
- 이 방향을 계속 쓴다. 새 표시는 표시 Factory에, 새 입력은 입력 Factory에 둔다.
- 주의할 점은 Factory 서명 성장이다(N2 (c)). 인자가 더 늘면 이름 있는 묶음으로 받는다. 무타입 Parts 가방과 후설정 setter는 쓰지 않는다(SV 평가 §4.6).

### 5.2 기능별 구현 분리 방향 — 유지하되 허브의 기능 지식을 줄인다

- 지금 축은 기술 계층별 분리다.
  - 입력 기능: Placement·Attack·Path
  - 표시 기능: Field·Unit·Hand·Hud·Attack·Placement·MovementPlanning
  - 재생 기능: Attack·Motion·Railway
- 이 축을 기능 수직 묶음으로 바꾸지 않는다. 예를 들어 공격의 입력·표시·재생을 한 묶음 클래스로 모으는 식이다. 그렇게 하면 계층마다 정해 둔 계약이 무너진다. 해제 규칙 D9, 재생 계약 D6, 입력 Submit이 그 계약이다.
- 대신 허브는 **순서와 전달만** 갖게 한다. 허브가 기능 규칙을 가지면 그 규칙을 기능 쪽으로 돌려보낸다.
  - 조정자의 완료 표시(N1)
  - 경로 입력의 표시 거울(N3)
  - 피드백 계약의 기능 혼합(N5)
- 큰 입력 기능의 내부는 **상태 수명이 다른 부분부터** 나눈다. 순수 계산(N4 조준 기하)과 독립 요청 수명(N3 이동 범위)이 해당한다.

### 5.3 다음 작업 후보 (재개할 때)

비용과 효과 순서다. 모두 Core·Buffer·씬을 건드리지 않는다.

| 순서 | 후보 | 효과 | 비용·위험 |
|---|---|---|---|
| 1 | N5 피드백 계약 3분할(합성 유지) | 배치·공격 결합 제거, ISP 해소 | 작음. 기계적, 관리 실행으로 확인 |
| 2 | N4 조준 기하 순수 분리 | 조준 규칙(약 70줄)을 상태 없는 한 곳으로, E20 교체 지점 한 곳 | 작음. 순수 함수 시험 추가 |
| 3 | N1 완료 표시 소유자 분리 | 조정자에서 진행 표시 제거, 포기와 대칭 | 중간. 진행 계약(J7·J12) 동작 보존, 오디오 줄 위치만 이동 |
| 4 | N3 (a) 이동 범위 소유자 분리 | 범위 요청·표시 수명(세대 번호 1종 포함)이 경로 입력 밖으로 | 중간. 경로 테스트가 많고 리플렉션 이름 제약 |
| 5 | N2 (a) 표시 값 읽기 분리 | 조정자 의존성 3개 제거 | VR6와 함께 하면 작음, 따로 하면 두 번 고친다 |
| 보류 | N3 (b)·N6·N8·N9 | — | 재검토 조건이 생길 때 |

재개하더라도 Unity Test Runner 결과로 SV를 인수하고 커밋한 **뒤**에 시작한다. 계획 문서는 새로 만든다. SV 계획 §12에 덧붙이지 않는다.

## 6. 이번 평가에서 확인하지 않은 것

- Unity Test Runner 결과(사용자 보고 대기)와 재생 확인(SV 계획 §12.11).
- `Display/` 안의 큰 파일 내부(`FieldUnitPresenter` 560·`CombatProjectilePool` 555·`CombatSceneEventPresenter` 492)는 이번 SV 범위가 아니어서 크기만 봤다. 내부 책임 혼합 여부는 따로 읽어야 판정할 수 있다.
- `CombatTimelineViewSystem`은 독립 시스템으로 보고 구조만 확인했다(R4 D11).
