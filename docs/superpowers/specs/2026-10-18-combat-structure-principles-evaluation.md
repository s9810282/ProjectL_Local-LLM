# 전투 골격·전투 View 구조 원칙 평가 (2026-10-18, A 단계 뒤)

- 작성: View 작업, 2026-10-18. 사용자 요청: "지금 작업한거 전투 골격 및 전투 View 구조를 SSOT(한 사실은 한 곳에만)와 SOLID, JMH Framework 개발 지침·FW 계약, 기능 변경의 독립성 평가진행해".
- **평가만 한다. 코드 변경 없음.** 처리 방향은 후보이며 결정이 아니다.
- 기준 코드: HEAD `ec34d932`(S-1) + 미커밋 S-2~S-5·A-1~A-5. Phase마다 Unity 묶음은 사용자가 확인했다([분리 계획](../plans/2026-10-18-combat-scene-event-presenter-split-plan.md) §8.3, [연출 바탕 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md) §12.1~§12.8). B-0은 설계 문서만 있다.
- 판단 기준: [설계 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md)(§4.1 SSOT, §4.2 SOLID, §4.3 FW, §4.6 변경 독립성), [프로그래머 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-001~016.
- 선행 문서:
  - [10-15 재평가](2026-10-15-combat-view-structure-reevaluation.md)(N1~N10): SV 뒤 구조의 절대 판정.
  - [연출 재생 구조 평가](https://claude.ai/artifact/BakhQ6UaWPhRTAaChkLvDX)(P1~P8)와 [연출 조립 설계 §9](2026-10-17-combat-presentation-composition-design.md)(원인 C1~C5, 개선 I-1~I-6): 이번 A 단계의 출발점.
  - 이 문서는 지금 구조의 절대 판정이다. 이전 항목이 어떻게 됐는지(§4)도 함께 적는다.

## 0. 범위

- **전투 골격:** `LifeCycleSystem/LifeCycles/Combat/`(수명·진입 서비스·Plan·진행 읽기·Job, 18개 파일 1,751줄)와 그 Plan을 거는 `JobRegisterSystem`.
- **전투 View:** `ViewSystem/Combat/`(`.cs` 98개)·`Display/`(67개), `Factory/Runtime/InGame/View/`, View DB 설정 표 `CombatPresentationSettings`.
- **제외:** Core·Buffer·FieldCamera·Architecture·씬·프리팹·DB 값. Audio 정책(R4 결정으로 제외)은 판정 근거로만 적는다.

## 1. 결론

**전투 골격은 양호하다. 전투 View는 재생 쪽이 크게 좋아졌고, 입력·조정자 쪽은 10-15 상태 그대로다.**

- **골격:** 판단 원본 `CombatPlan.Decide` 하나, 사실 읽기 `CombatProgressReader` 하나다. View는 골격이 정의한 좁은 계약(`ICombatProgressView`)만 구현한다. Job은 호출 한 번과 그 확인만 한다. Framework 위반은 없다.
- **View 재생(이번 작업 범위):**
  - 사건 Presenter가 759줄 → 330줄이 됐다. 남은 것은 공통 수명, 사건 하나 안의 순서 틀, 공개 진입점, 조립이다.
  - 한 곳으로 모인 사실: 시작 조건(`FindBlock` 하나, A-1·A-2b), 사건 경로(`CombatEventRoutes`, A-4), 유닛 표시 채널 소유(창구, A-3a), HUD 일시 피드백(A-3b), 전역 연출 값과 그 조건(View DB 표, A-5).
  - 재생기 계약과 고정 실행 목록으로 "순서 = 겹침 규칙"이 한 곳에 있다(A-2).
  - 표시 계약이 재생 계약을 상속하지 않게 되어 재생 사슬이 한 단계 줄었다(A-4b).
- **남은 문제는 두 갈래다.**
  1. **재생 쪽에 사건 종류 지식이 아직 여러 곳에 있다.** 포효 순서(다섯 곳, R1), 소리·배경음 프리셋(세 클래스, R2), 사건 행위자 해석(세 곳, R3). R1은 B-2, R2의 효과음 부분은 B-3 대상이다. R2의 배경음과 R3은 B 계획에 없다.
  2. **입력·조정자 허브는 10-15 그대로다(N1~N5).** 이번 작업은 재생 쪽만 다뤘다.
- **새로 생긴 것:** 10-16 포기 버튼의 Option 이관(현강님) 뒤, 포기 표시 계약이 빈 구현이 됐다. 컨트롤러의 확인창 경로가 닿지 않는 코드로 남아 있다(R9).

| 기준 | 10-15 | 지금 | 요지 |
|---|---|---|---|
| S 단일 책임 | 부분 | 부분(재생 양호) | 재생: Presenter는 수명·틀·조립만, 재생기·처리기·창구·HUD 피드백이 각자 규칙. 남음: 포효 지식이 공격 재생에(R1), 창구가 대여 규칙과 외형 설정 조회를 함께(R6), 조정자(N1·N2), 경로 입력(N3), 공격 조준(N4) |
| O 개방-폐쇄 | 부분 | 부분(좋아짐) | 사실만 반영하는 새 사건 = 경로 표 한 줄, 새 재생기 = `Create` 자리 하나, 새 전역 값 = 설정 표. 연출 있는 새 사건 형식은 4~6곳(R4), 포효 순서 변경은 3~4파일(R1) |
| L 리스코프 | 양호 | 부분 | 재생기 계약의 뜻(BeginFrame·Tick false·읽기 전용)이 문서화돼 있다. 포기 표시 계약이 빈 구현이라 컨트롤러가 믿는 동작(확인창)이 일어나지 않는다(R9) |
| I 인터페이스 분리 | 부분 | 부분(좋아짐) | 표시 계약 ↔ 재생 계약 분리, 유닛 계약 셋(표시 대상·대상 조회·HUD 대상), 대기 칸·출처 겹침 계약. 남음: 피드백 계약 18멤버(N5), 쓰이지 않는 포기 표시 계약(R9) |
| D 의존성 역전 | 양호 | 양호 | 구체 선택은 Factory 둘과 Presenter 생성자(Q-S3 A). 처리기가 같은 범위의 내부 구체(공격 재생·능력 동작·창구)를 직접 받는 것은 지침 §27로 수용. B-1에서 출처 수명이 계약(`ICombatSourceLifetime`)이 된다 |
| SSOT | 양호(잔여) | 양호(잔여, 줄어듦) | Buffer 사실 복제 없음. 재생 쪽 원본 다섯이 새로 한 곳씩. 잔여: R1·R2·R3, 경로 입력 표시 거울(N3), 전투 정체성 사본(N8) |
| Framework | 양호 | 양호 | 위반 없음. 새 DB 표가 Query 규칙을 따른다. 대여 자원은 모두 반환 손잡이가 있다. 공유 문서 미전달은 절차 항목 |
| 변경 독립성 | 부분 | 부분(재생 양호) | 재생 시나리오 대부분이 한 파일 또는 한 묶음으로 끝난다(§6.4). 남은 "부분"은 입력·조정자 시나리오와 포효·연출 있는 새 사건 형식 |

## 2. 현재 구조 지도

```text
골격 (LifeCycle)
CombatLifeCycle(트리거) ─ Runner가 비면 Register(Combat, Next) / 나갈 때 ExitCheck
CombatEntryService(진입 요청 수명) ─ JobRegisterSystem.RegisterCombatEntry → CombatPlan.RegisterEntry
CombatPlan.Decide(판단 원본) ◄─ CombatProgressReader.Read()(Buffer 싱크 5 + ICombatProgressView)
  └ Next → Job 하나(제출 정지·완료 표시·포기·명령·미리보기·자동 진행) 또는 이탈 연쇄(EndBattle → Run 종료 → 새 Run → 전환)

View
InGameViewSystemFactory ─► CombatViewSystem(1,005, 조정자, ICombatProgressView 구현)
  ├ 입력: Receiver → Router → CombatInteraction ─ Placement(366) · AttackTargeting(462) · PathInput(637)
  ├ 표시: CombatSceneDisplay(549, 생성자 인자 26) ─ 피드백·정적·동적 Presenter
  │        └ FieldUnitPresenter(489) ─ 창구 CombatUnitPresentationWindow(367) · HUD 피드백 CombatUnitHudFeedback
  └ 재생: CombatResultPlayback → OrderedCombatPresentation(경로 표 읽음)
           → ICombatBattleDisplay.EventPlayback = CombatSceneEventPlayback(재생 계약, D9 거절)
           → CombatSceneEventPresenter(330: 수명·사건 틀·진입점·조립)
               ├ 실행 목록 CombatPresentationPlayers: Attack → Ability → Step → Reaction (FindBlock·Tick·완료·정리)
               ├ 처리기 표 CombatEventHandlerTable(경로 표 형식마다): Attack·Roar·Summon·Character·Turn·Fact
               └ 보인 사실 · 대기 · 강조 · 소리 · 이동 표시
설정: View DB CombatPresentationSettings(값 11개와 조건 원본) → 표시 Factory
```

## 3. 전투 골격

### 3.1 판정

| 기준 | 판정 | 근거 |
|---|---|---|
| S | 양호 | 수명(트리거)·진입 요청 수명·판단·사실 읽기·실행(Job)이 각각 한 클래스다. Job은 "호출 한 번 + 결과 확인"만 한다 |
| O | 수용 | 새 진행 단계는 `CombatStepKind` 한 줄 + `Decide` 한 줄 + `Next` 분기 + Job 하나다. 모두 `Combat/` 폴더 안이고 판단은 한 곳이다 |
| L | 양호 | Job은 `IsCompleted` 뜻을 지킨다(정상 false도 완료). `ICombatProgressView`의 각 멤버 보장(여러 번 불러도 됨, 한 번 표시)이 문서화돼 있다 |
| I | 양호 | View 계약 7멤버, 읽기 계약 3멤버. Reader는 `IBufferQuery`에서 필요한 싱크만 꺼내 보관한다 |
| D | 양호 | 골격이 계약을 정의하고 View가 구현한다(의존 방향이 골격 쪽). Plan은 그룹 서비스의 계약만 받는다 |
| SSOT | 양호 | 판단 = `Decide` 하나. 사실 = `CombatProgressFacts`(판단 시점 사본, 저장하지 않음). 이탈 목적지·새 Run 로드아웃 = `CombatPlan` 한 곳 |
| Framework | 양호 | `ICombatProgressReader : IServiceModule`, PostBindService로 받기, 오류는 F1 경계(`RuntimeFaultBoundary`), 오류 뒤 새 등록 없음 |

### 3.2 남은 점

- **K1 공용 Job 등록에 전투 전용 입구 — 낮음, 수용.**
  - `IJobRegisterSystem.RegisterCombatEntry(request)`는 Global 서비스에 있는 전투 전용 메서드다. 다른 루트가 "대상이 있는 진입"을 요구하면 메서드가 늘어난다(O·I).
  - 골격 설계 §6.3(K20)의 결정이다. **재검토 조건:** 두 번째 루트가 대상 있는 등록을 요구할 때 `Register(root, request)` 형태로 일반화한다.
- **K2 Run 종료 판정이 Job 안에 있다 — 낮음, 수용.**
  - `CombatRunEndJob.EndsRun`은 보스 승리면 챕터 완료를 요청하고 그 결과로 Run 종료를 정한다. 판단 원본이 Plan 하나라는 J2와 어긋나 보인다.
  - 하지만 판정에 쓰는 값이 같은 Job의 Core 호출 결과라서 Plan이 미리 정할 수 없다. "호출 결과 해석"으로 보고 수용한다.
- **K3 임시 값 — 수용.** `NewRunLoadoutKey = 1`(Title 새 게임 선택 전), 연결 정의 없음 → Run 종료(K21 임시), `RunEndDestination = Stage`(Title 대기). 모두 한 곳씩이고 주석에 임시라고 적혀 있다.
- **K4 골격 ↔ View 경계 — 양호.** 진행 사실 중 View 상태 다섯(연결·재생 중·제출 정지·완료 표시·포기 요청)은 View가 원본이고 Reader가 매번 새로 읽는다. 완료 표시 상태를 조정자가 가진 것은 View 쪽 문제(N1)이며, 그것을 옮겨도 골격 계약(J12)은 그대로다.

## 4. 이전 평가 항목의 현재 상태

| 항목 | 이전 | 지금 | 근거 |
|---|---|---|---|
| P·C1 표시 지식을 Presenter 층에서만 정리 | 부분 | **해소** | 경로 표(A-4)가 스케줄러·재생 조정·처리기 표의 공통 원본. 재생 계약 사슬 축소(A-4b) |
| C5 질의·단정 차이 | 부분 | **해소** | `Start` 프로필 하나(A-2b) |
| C3 유닛 입구 하나 | 부분 | **해소** | 창구(A-3a)·HUD 피드백(A-3b)·계약 셋 |
| C4 값 배치 규칙 없음 | 부분 | **해소(전역 값)** | 설정 표(A-5). 외형별 값은 기존 View 표 |
| C2 합성 연출 수단 없음 | 부분 | 설계 완료(B-0) | 포효·소리는 아직 그대로(R1·R2) |
| N1 조정자의 진행 표시 | 중간 | 그대로 | `CombatViewSystem.PresentCompletion`(배경음 줄 포함). 조정자 972 → 1,005줄 |
| N2 사실 조립·표시 값 읽기 | 낮음~중간 | 그대로 | `ReadFacts`·`ReadDisplayValues`(`GetTimeline`) |
| N3 경로 입력 내부 혼합 | 중간 | 그대로 | `CombatPathInput` 584 → 637줄, `_shownPath`·세대 번호 3종 |
| N4 공격 조준 기하 | 중간~낮음 | 그대로 | `AimFor`·`NearestDirectionAim`·`TryGetDirectionVector` |
| N5 피드백 계약 18멤버 | 중간 | 그대로 | `ICombatInteractionFeedback` |
| N6 표시 입구 전달 비용 | 수용 | 수용(줄어듦) | 재생 위임 11개 제거(A-4b). 생성자 인자 26, Clear 13·Release 20항목 |
| N7 재생 전달 사슬 | 수용 | **줄어듦** | 표시 입구 가드 단계가 빠짐(A-4b) |
| N8 전투 정체성 사본 | 낮음 | 그대로 | 조정자·경로 입력·이동 상호작용 |
| N9 평면 폴더 | 낮음 | **낮음→중간** | `Combat/` 61 → 98개(R10) |
| N10 검증 안전망 | 절차 | 나아짐 | Phase마다 Unity 묶음 결과를 받았다. 결과 XML은 여전히 없다 |

## 5. 남은 문제

심각도는 변경 빈도와 잘못 고칠 위험을 함께 본 판단이다. 처리 방향은 후보다.

### R1 포효 지식 다섯 곳 — 중간 (B-2가 해소)

- **위치:** `CombatRoarEventHandler`, `CombatAbilityMotionPlayback`(포효 단계 상태), `CombatAttackPlayback.BeginRoar·EndRoar·AbortRoar·TryBeginFacing`, `CombatAbilityPlayer`(겹침·O4), `CombatStartBlock.AbilityBusy`. 머묾(`CombatEnemyPresentationPacing.HoldAfter`)과 히트 후속(`CombatHitFollowUps`)도 포효 형식을 안다.
- **영향:** 포효 순서를 바꾸면 처리기·능력 동작·공격 재생을 함께 읽고 고친다. 공격 재생(497줄)이 포효를 안다(S).
- **방향:** [조각·조립 설계](2026-10-18-combat-presentation-pieces-design.md) BP6·BP7(B-2). 순서는 포효 조립 한 곳, 출처 수명은 Gate + 실행 한 쌍이 된다.

### R2 소리·배경음 정책이 세 클래스에 — 중간 (효과음은 B-3, 배경음은 계획 밖)

- **위치:**
  - 결과 사건 효과음 "무엇을 내나": `CombatEventAudio.Play`의 `switch` 한 곳. 호출 자리는 넷(틀 ③, 사망 처리기, 공격 콜백, 걸음 콜백).
  - **배경음 프리셋 숫자:** `CombatEventAudio`(전투 진입 3·밤 끝 2), `CombatSceneDisplay.Refresh`(밤 3·낮 2), `CombatViewSystem.PresentCompletion`(확인 뒤 1).
  - 승리 효과음: `CombatSceneDisplay.ShowCompletion`.
- **SSOT:** "국면 → 배경음 프리셋" 규칙(밤 3·낮 2)이 사건 소리와 표시 갱신 두 곳에 있다. 숫자는 이름 없는 상수다. 한쪽만 바꾸면 사건 재생 중과 결과 끝의 배경음이 달라진다.
- **방향 후보:**
  - B-3(Q-B4 A)은 결과 사건 효과음만 표로 모은다. 배경음은 [조각·조립 설계](2026-10-18-combat-presentation-pieces-design.md) BP8에서 "범위 밖(기록)"으로 두었다.
  - (a) B-3 범위에 "국면 → 배경음 프리셋" 함수 하나를 더해 세 곳이 그것을 부르게 한다. 값·Audio 로직은 그대로라 Q-S2 A(위치만 이동)와 같은 성격이다.
  - (b) 현강님 소리 메모에 중복을 적고 그대로 둔다.
  - 어느 쪽이든 Audio 정책(R4 제외)을 바꾸지 않는다. **사용자 결정 필요.**

### R3 사건 행위자 해석 세 곳 — 낮음~중간

- **위치:** 처리기 `ActorOf`(공격·포효·소환), `CombatEnemyPresentationPacing.BeforeEvent`(같은 세 형식에서 `AttackerId`·`BossId`·`SummonerId`를 다시 꺼냄), `CombatHitFollowUps`(공격·포효).
- **무엇:** Presenter는 `handler.ActorOf(e)`로 행위자의 Owner를 찾아 간격 계산에 넘긴다. 그런데 간격 계산은 같은 사건에서 행위자 ID를 다시 꺼낸다. "이 사건의 행위자는 누구인가"가 두 클래스에 있다.
- **영향:** 행위자가 있는 새 형식(예: 새 보스 능력)을 더하면 처리기와 간격 계산을 함께 고쳐야 하고, 한쪽을 빠뜨리면 적 행동 간격이 조용히 0이 된다.
- **방향 후보:** 간격 계산이 행위자 ID를 인자로 받는다(`BeforeEvent(e, actorId, owner)`). 처리기 `ActorOf`가 원본이 된다. 히트 후속은 Gate 출처의 가해자이므로 같은 질문인지 착수 때 확인한다. 동작 동일, 비용 작음. B-1 또는 B-2에 작은 정리로 넣을 수 있다. **사용자 결정 필요.**

### R4 연출 있는 새 사건 형식의 변경 범위 — 낮음, 수용

- 사실만 반영하는 새 형식은 경로 표 한 줄이다(좋아짐).
- 연출이 있는 새 형식은 경로 표 + 처리기 클래스 + `CombatEventHandlerTable.Create` 인자 + Presenter 생성자 조립이다. 행동 경계면 `CombatActionBoundary.IsAction`, 적 행동이면 간격 계산(R3)도 고친다.
- `CombatActionBoundary`(행동 경계)와 경로 표(어떻게 보이나)는 다른 질문이라 중복이 아니다. 행동 경계는 처리기 표가 없는 스케줄러·Gate·접촉 해석도 쓰므로 그대로 둔다.
- **재검토 조건:** R3 정리 뒤에도 새 형식마다 4곳 넘게 고치는 일이 반복될 때.

### R5 Presenter 생성자 인자 순서 — 낮음

- `CombatSceneEventPresenter` 생성자는 인자 14개다. 그중 float 셋(적 행동 간격·적 턴 간격·회전 시간)이 연달아 있고, 표시 Factory가 자리 순서로 넘긴다. 둘이 바뀌어도 컴파일된다.
- 조립을 생성자 안에서 하는 것은 Q-S3 A 결정이라 그대로 둔다.
- **방향 후보:** Factory 호출을 이름 있는 인자로 바꾼다(구조 변경 없음, 한 줄 단위). 또는 설정 표 Data를 통째로 넘긴다(A-5 뒤에 가능). 앞쪽이 싸다.

### R6 창구가 대여 규칙과 외형 설정 조회를 함께 — 낮음

- `CombatUnitPresentationWindow`(367줄)는 채널 대여 다섯 종(자세·크기·배치·사망 유지·이동 투명도)과 함께 외형 설정 조회 넷(`TryGetAttackPresentation`·`TryGetMortarPresentation`·`GetBallistaWeapon`·`TryGetReactionSettings`)을 갖는다.
- 변경 이유가 다르다. 대여는 겹침·반환 규칙, 조회는 View 표·프리팹 구성이 바꾼다.
- B-4에서 외형 층이 더해지면 창구가 더 커진다.
- **방향:** 지금은 그대로 둔다. B-4 뒤 창구가 계속 자라면 조회를 읽기 전용 계약으로 뺀다.

### R7 표시 입구 — 수용 유지(N6)

- 생성자 인자 26개, Clear 13·Release 20항목, `_isReleased` 확인 여러 곳. 해제 뒤 규칙(D9)과 해제 목록(D8)을 한 곳에 두는 대가로 수용한다.
- 표시 입구가 배경음·승리 효과음을 직접 낸다(R2).

### R8 시험이 private 필드에 묶임 — 중간 (변경 독립성·절차)

- Presenter 주석이 "부품 필드를 시험이 리플렉션으로 읽습니다"라고 적고 필드를 유지한다(`_attacks`·`_reaction`·`_motion`). 창구의 `_deathHolds`·`_movementOpacity`, 표시의 `_events`·`_units`도 시험이 읽는다. 시험 폴더의 87개 파일이 `BindingFlags.NonPublic`을 쓴다.
- **영향:** 내부 구조를 바꾸면 관련 없는 시험 파일까지 고친다. A-3a에서 리플렉션 시험 셋을 고쳤다. 운영 코드의 필드가 시험 때문에 남는다.
- **방향 후보:** 새 시험은 계약으로 검증한다(A 단계 새 시험은 이미 이 방식). 옛 시험은 그 기능을 고칠 때 계약 검증으로 바꾼다. 시험만을 위한 운영 필드는 새로 두지 않는다.

### R9 포기 표시 계약이 빈 구현 — 중간 (L·I, 다른 작업자 변경)

- **위치:** `CombatSceneDisplay`의 `ICombatAbandonDisplay` 멤버 넷이 빈 본문이다(현강님 `9354c522`, 2026-10-16: 전투 화면 포기 버튼 삭제, Option에서 요청).
- **무엇:**
  - `CombatAbandonController`의 확인창 경로(`Open`·`Confirm`·`Cancel`)에 닿는 입구가 없다. `IsConfirmOpen`은 늘 false다.
  - 그래서 조정자 `CanReceiveDeckInput`의 확인창 조건, 컨트롤러 `Sync`·`Stop`·`Close`의 표시 쓰기가 아무 일도 하지 않는다.
  - 실제 요청은 Option UI → `ICombatViewService.TryRequestAbandon` → 컨트롤러다. 가능 조건과 요청 상태는 여기서 그대로 쓰인다.
- **L:** 계약을 구현하면서 뜻(확인창을 보이고 입력을 전달)을 무시한다. 컨트롤러는 확인창이 열린다고 믿는 코드를 유지한다.
- **방향 후보:**
  - (a) 컨트롤러를 "가능 조건 + 요청 상태"로 줄이고, 확인창 상태와 포기 표시 계약을 걷어낸다.
  - (b) 전투 화면 버튼이 돌아올 가능성이 있으면 빈 구현이 의도라고 계약 문서에 적고 그대로 둔다.
  - 현강님 변경에서 생긴 일이라 **현강님과 사용자 확인 뒤** 정한다. 지우는 쪽이면 CLAUDE.md 규칙상 사용자 지시가 필요하다.

### R10 평면 폴더 — 낮음→중간 (N9)

- `Combat/`에 `.cs` 98개(10-15에 61개)다. A 단계가 재생기·처리기·계약을 더했다. 재생 묶음(재생기 넷·처리기 여섯·경로 표·시작 조건·실행 목록·계약 셋)은 위치로 보이지 않는다.
- **방향:** B 단계가 끝나고 큰 병합이 없는 시점에 `Playback/` 하위 폴더를 검토한다. 네임스페이스가 바뀌므로 .meta GUID를 유지하고 다른 작업자 브랜치와 시점을 맞춘다.

## 6. 원칙별 판정 상세

### 6.1 SSOT

| 사실 | 원본 | 판정 |
|---|---|---|
| 전투 진행 판단 | `CombatPlan.Decide` | 양호 |
| 진행 판단에 쓰는 사실 읽기 | `CombatProgressReader` | 양호 |
| 연출 시작 가능 여부 | 실행 목록 `FindBlock`(질의·단정 같은 판단) | 양호(A-1·A-2b) |
| 사건이 어떻게 보이나 | `CombatEventRoutes` | 양호(A-4) |
| 유닛 표시 채널의 주인 | `CombatUnitPresentationWindow` | 양호(A-3a) |
| HUD 일시 피드백 상태 | `CombatUnitHudFeedback` | 양호(A-3b) |
| 전역 연출 값과 값 조건 | View DB `CombatPresentationSettings` | 양호(A-5) |
| 지금 보인 사실 | `CombatShownFacts` | 양호(S-1) |
| 재생 세대 | Presenter `_presentationVersion`(부품은 읽기만) | 양호 |
| 포효 순서 | 처리기·능력 동작·공격 재생 | 잔여(R1, B-2) |
| 효과음 "무엇을 내나" | `CombatEventAudio` | 양호 |
| 국면 → 배경음 프리셋 | 사건 소리·표시 갱신·조정자 | **잔여(R2)** |
| 사건 행위자 | 처리기 `ActorOf`·간격 계산·히트 후속 | **잔여(R3)** |
| 초안 경로 표시 상태 | 경로 입력의 거울 | 잔여(N3) |
| 전투 정체성 | 조정자·경로 입력·이동 상호작용 | 잔여(N8, 수용) |
| 포기 확인창 열림 | 컨트롤러 `IsConfirmOpen`(늘 false) + Option UI | **잔여(R9)** |

### 6.2 SOLID

- **S:** 재생 쪽은 "누가 무엇의 규칙을 갖나"에 답이 한 곳씩 있다. 시작·진행·완료·정리 = 재생기, 형식별 표시 = 처리기, 채널 = 창구, 사건 하나 안의 순서 = Presenter 틀. 남은 혼합은 R1(공격 재생의 포효), R6(창구), 입력·조정자(N1~N4)다.
- **O:** 확장 지점이 이름 있는 자리로 생겼다. 실행 목록 `Create` 자리, 처리기 표 `Create` 인자, 경로 표 형식 목록, 설정 표 필드. 자리를 더하는 것은 공용 파일 수정이지만, 순서 규칙상 의도된 것이다(설계 B12 보완 2).
- **L:** 재생기 계약과 대기 칸·출처 겹침 계약은 뜻이 문서화돼 있고, 가짜 구현도 같은 계약을 따른다. 예외는 R9의 빈 구현이다.
- **I:** 소비자마다 좁은 계약을 받는다. 처리기·재생기는 Presenter를 모른다. 남은 넓은 계약은 피드백 18멤버(N5)와 조정자 전용 합성 계약 `ICombatBattleDisplay`(수용)다.
- **D:** 조립 자리가 셋이다: 표시 Factory, 입력 Factory, Presenter 생성자(Q-S3 A). 재생 부품은 조립 자리 밖에서 서로를 만들지 않는다. 지침 §27에 따라 구현이 하나인 내부 부품에는 계약을 만들지 않았다.

### 6.3 Framework 계약·개발 지침

| 항목 | 판정 | 근거 |
|---|---|---|
| FW-001 서비스 등록 | 양호 | 새 DB 표 `CombatPresentationSettingsTable : ICombatPresentationSettingsTableQuery, IExportSystemServices`, 표 키 `ViewTableKey` |
| FW-002·003, 지침 §11 | 양호 | 실행 목록·처리기 표는 만든 뒤 바뀌지 않는다. 창구·HUD 피드백은 처음 읽힐 때 한 번 만든다(View 내부 객체, Framework 구조 아님) |
| FW-004 우회 금지 | 양호 | 색은 `IColorSystemService` 공개 Handle로만 바꾼다 |
| FW-010~012 대여 자원 | 양호 | 창구 대여 다섯 종 모두 반환 손잡이(`Restore`·`Release`)와 `IsCurrent`가 있다. 이동 투명도는 DR-004 Scope. B-4 합성은 설계 BP9 |
| 지침 §20·21 구독 해제 | 양호 | 새 구독 없음. 콜백은 재생 세대로 지난 수명을 무시한다 |
| 지침 §25 씬 검색 | 양호 | `FindObjectsByType`는 조정자의 진입 한 곳. 나머지 `GetComponent`는 자기 프리팹 인스턴스 안 |
| 지침 §14 문자열 조회 | 양호(시험 예외) | 운영 코드에 없음. 시험 리플렉션(R8) |
| 지침 §26 God Class | 부분 | 조정자 1,005줄(N1·N2). Presenter는 해소(330줄) |
| 지침 §27 과도한 추상화 | 양호 | 계약은 구현이 있는 것만(재생기 넷, 처리기 여섯, 유닛 계약 셋) |
| DB P1~P7 | 양호 | 설정 Data는 View 표에 float만, 파생 필드 없음(P7), 조건 원본 하나 |
| 공용 파일 공유(인계 §4.5) | 미완(절차) | 민형님 Factory 공유 §9·§10, 호진님 설정 표, 현강님 소리, Core 전달 §5.45 모두 미전달 |

### 6.4 변경 독립성 (대표 시나리오)

열 뜻: **허브** = 그 기능 밖이면서 여러 기능이 함께 쓰는 지점. 허브 0이면 양호, 수용한 경계뿐이면 수용, 허브의 기능 지식을 고쳐야 하면 부분.

| 시나리오 | 고칠 곳 | 허브 | 10-15 | 지금 |
|---|---|---|---|---|
| 새 Core 사건(사실만) | 경로 표 한 줄 | 0 | 양호(Presenter 매핑) | 양호 |
| 새 Core 사건(연출 있음) | 경로 표 + 처리기 + 처리기 표 `Create` + Presenter 조립 | 조립 2곳 | — | 수용(R4) |
| 사건 연출 시작 조건 변경 | 해당 재생기 `FindBlock` | 0 | 부분(Presenter 다섯 곳) | 양호 |
| 포효 순서 변경 | 처리기 + 능력 동작 + 공격 재생 | 공격 재생 | 부분 | 부분(R1, B-2 뒤 조립 한 곳) |
| 사건 효과음 하나 추가 | `CombatEventAudio` switch | 0 | — | 양호 |
| 배경음 국면 규칙 변경 | 사건 소리 + 표시 갱신 + 조정자 | 표시·조정자 | — | 부분(R2) |
| 적 행동 간격 규칙 변경 | 간격 계산 | 0 | — | 양호 |
| 행위자 있는 새 형식 | 처리기 `ActorOf` + 간격 계산 | 0 | — | 부분(R3) |
| 전역 연출 값 추가 | 설정 SO 필드 + 조건 + 소비자 + 에셋 | 표시 Factory | 부분(씬 값) | 양호 |
| 유닛 표시 채널 추가(플래시) | 창구 + 표시 계약 + `FieldUnitView` + 가짜 | 창구 | — | 수용(B-4) |
| HUD 일시 피드백 변경 | `CombatUnitHudFeedback` + HUD 계약 | 0 | 부분(`FieldUnitPresenter`) | 양호 |
| 새 재생기 | 계약 구현 + `Create` 자리 + Presenter 조립 + 막는 이유 | 실행 목록 | — | 수용(순서 = 규칙) |
| 공격 피드백 표시 추가 | 피드백 계약 + 입구 + 공격 피드백 + 조준 + 가짜 2 | 계약·입구 | 부분 | 부분(N5) |
| 이동 범위 규칙 변경 | `CombatPathInput` 내부 | 0 | 부분 | 부분(N3) |
| 완료 표시 단계 추가 | 조정자 `PresentCompletion` + 표시 + HUD | 조정자·입구 | 부분 | 부분(N1) |
| 전투 진행 단계 추가 | `CombatStepKind` + `Decide` + `Next` + Job + View 계약 | 0(골격 폴더) | — | 수용 |
| 이탈 목적지 변경 | `CombatPlan` 한 곳 | 0 | — | 양호 |

요약: 재생 쪽 시나리오는 대부분 양호 또는 수용이다. "부분"은 포효(R1)·소리(R2)·행위자(R3)와 입력·조정자(N1·N3·N5)에 모인다.

## 7. 다음 작업 후보

> **결정(2026-10-18, 사용자):** "Q-B1~B7은 추천대로, R2,3,5 도 추천대로". R2 (a)는 B-3, R3·R5는 B-1에 넣었다([연출 바탕 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md) §10, 기대값 §10.0). R9는 현강님 확인 대기로 남는다.

| 순서 | 후보 | 효과 | 비용·조건 |
|---|---|---|---|
| — | B-1~B-4(결정 대기 Q-B1~Q-B7) | R1 해소(B-2), 효과음 표(B-3), 플래시(B-4) | 계획 §10대로 |
| 결정 | R2 (a) 배경음 프리셋 함수를 B-3에 포함 | 배경음 규칙 한 곳 | 작음. 값·Audio 로직 그대로, 현강님 공유 |
| 결정 | R3 행위자 ID를 처리기에서 받기 | 행위자 원본 하나 | 작음. B-1 또는 B-2에 포함 |
| 결정 | R9 포기 확인창 경로 정리 | 죽은 계약·상태 제거 | 현강님 확인 필요, 삭제는 사용자 지시 |
| 아무 때 | R5 Factory 호출을 이름 있는 인자로 | 인자 자리 바뀜 방지 | 아주 작음 |
| 정책 | R8 새 시험은 계약으로, 옛 시험은 고칠 때 바꿈 | 내부 변경 때 관련 없는 시험 수정 감소 | 절차 |
| B 뒤 | 10-15 §5.3 순서: N5 → N4 → N1 → N3(a) → N2(a) | 입력·조정자 허브 축소 | 새 계획으로 |
| B 뒤 | R10 `Playback/` 하위 폴더 | 기능 경계가 위치로 보임 | 병합 시점 조율 |

## 8. 확인하지 않은 것

- 입력 쪽(Placement·AttackTargeting·PathInput·MovementInteraction)과 조정자는 10-15 이후 다른 작업자 병합으로 바뀐 부분을 줄 단위로 다시 읽지 않았다. 크기와 N 항목 위치만 확인했다.
- `Display/`의 큰 파일(`CombatProjectilePool` 555·`FieldOutlineRenderer` 546·`FieldRailwayPresenter` 469·`FieldThornPresenter` 471) 내부 책임은 이번 범위 밖이다.
- `CombatTimelineViewSystem`(독립 시스템)과 비전투 View는 보지 않았다.
- 동작 근거는 Phase별 Unity 묶음 결과(사용자 확인)와 관리 실행이다. 이 평가를 위해 새로 실행한 것은 없다.
