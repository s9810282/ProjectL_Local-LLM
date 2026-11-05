# 전투 걸음 모양(칸마다 뛰어 이동) 설계 (2026-10-28)

- **상태:** 설계 결정 완료. **결정(2026-10-28 사용자 "권장안대로"): Q-ST1~Q-ST7 모두 A**(§3). Task 계획은 [step-motion-style-plan](../plans/2026-10-28-combat-step-motion-style-plan.md). **ST-a·ST-b 코드·에셋 완료, ST-c 문서 완료(2026-10-28, 컴파일 확인은 사용자 — Ua·Ub 문제 없음), 구현 메모 §7.**
- **2026-10-30 바뀜(특성 시스템 View TS-c, [계획](../plans/2026-10-30-combat-trait-system-view-plan.md) §7.4):** 이동 종류 판정 원본은 추론 `IsVoluntaryMovement`(이번 결과에서 이동한 행위자이고 공격 전)에서 `CombatMovementKinds.Of(걸음 사건)`(Core 걸음 사실의 이동 원인 `MovementCause` — Forced면 밀림, 비거나 Voluntary면 스스로)로 옮겼다. 걸음 표시·적 행동 간격이 같은 함수를 부르고, 추론 상태는 걷었다. 아래 F6·ST1의 `IsVoluntaryMovement` 문장은 그 전 기록이다.
- **요청(사용자):** 지금은 유닛이 걸어서 이동한다. 이것을 점프(뛰어서 이동)로 바꾼다. 나중에 장애물을 건너뛰는 등 이동 방식이 늘 수 있으니, 이동 연출이 "현재 칸, 도착 칸, 이동 방식"을 받아 처리하는 구조로 만든다.
- **점검 기준:** 사용자 7기준 — SSOT, SOLID, JMH Framework 지침·FW 계약, 기능 변경의 독립성, 캡슐화, C#·Unity 권장 구조, 무의미한 If·Tick 검사(§5).
- **바꾸지 않는 것:** Core 규칙과 사건, 걸음 레인의 순서·겹침 규칙([이동 접촉 설계](2026-10-17-combat-movement-contact-design.md) M3·M4, [동시 처리 설계](2026-10-17-combat-concurrent-presentation-design.md) C5·C6), 경로 입력, 이동 미리보기·고스트, Framework·Factory.

## 1. 확인한 사실 (코드 대조)

| # | 사실 | 근거 |
|---|---|---|
| F1 | Core는 칸 하나마다 걸음 사건 `Moved` 하나를 낸다. 넉백도 같은 사건이다. 끌어오기는 사건 대신 경로 값 `AttackPullSnapshot`으로 따로 온다. | `MovementProjection.Enter` `:135`, `AttackForcedMovement` `:54`, `CombatPullEventHandler` `:29` |
| F2 | "현재 칸·도착 칸"은 이미 걸음 사건마다 들어온다. 사건 전 사실 `Expected`와 사건 뒤 사실 `Final`이고, 칸 표면 높이도 거기서 나온다. | `CombatCharacterEvent` |
| F3 | 위치 식은 걸음 배치 대여 하나에 고정돼 있다. 가로는 직선이고, 높이는 `CombatStepHeight.At`이 정한다. 같은 높이면 평평하고, 높이가 다르면 칸 경계에서 높은 칸 + `climbHop`을 지나는 호다. | `CombatUnitPresentationWindow.Placement.Interpolate` `:329`, `CombatStepHeight` |
| F4 | 시간도 하나로 고정돼 있다. 모든 걸음과 끌어오기 한 칸이 설정 값 `movementStepSeconds`(0.2초)를 쓴다. 걸음 전 회전은 `movementTurnSeconds`다. | `CombatUnitMotionPlayback` `:151`·`:230` |
| F5 | 접촉 시점은 "이동자 몸이 직선으로 지나며 원본에 처음 닿는 진행률"이다. 첫 접촉 묶음이 해제되면 걸음 사건이 완료된다. | `CombatUnitMotionPlayback.ArmContacts`, `CombatStepContacts.ContactProgress`, 이동 접촉 설계 M2 |
| F6 | 자발 이동(경로 걸음)인지 밀린 것인지는 Core 사실에 없다. `IsVoluntaryMovement` 하나가 판정한다(이번 결과에서 이동한 행위자이고 공격 전). 그 결과 bool 하나가 `rotate`(레인)와 `voluntary`(접촉 게이트 겹침 규칙 1) 두 이름으로 내려간다. | `CombatEnemyPresentationPacing` `:54`, `CombatMovementPresentation.BeginStep`, `CombatStepContactGate.BeginStep` |
| F7 | 값 흐름: 설정 표 → `CombatEventPresentationValues.From` → 사건 Presenter → `CombatUnitMotionSet` → 레인. 걸음 값 셋(`stepSeconds`·`turnSeconds`·`stepClimbHop`)이 float로 생성자 셋을 지난다. | Factory `CombatSceneDisplayFactory.cs:125`, `CombatSceneEventPresenter.cs:100` |
| F8 | 천명님 시험 `PullAndMarkPresentationTests.cs:414`가 `CombatEventPresentationValues` 생성자를 위치 인자로 직접 부른다. 앞 인자를 바꾸면 그 시험이 컴파일에서 깨진다. | 시험 파일 |
| F9 | Core 경로 검증은 상하좌우 한 칸 걸음만 받는다. 장애물 건너뛰기는 Core 규칙 변경이 필요하고, View만으로는 만들 수 없다. | `MovementRouteValidator`, [클릭 이동 설계](2026-10-27-combat-click-move-design.md) F4 |
| F10 | 유닛 외형은 16개다. 그중 본부·화약통·보급품은 스스로 움직이지 않고 밀리기만 한다. | `DataBaseSO/ViewGroup/UnitVisual/Item/` |
| F11 | Core `MoveMethod`(`LiftAndPlace`)는 "이동할 수 있나"를 정하는 규칙 값이다. 모양 값으로 쓰면 안 된다. | `Contracts/Common/MoveMethod.cs` |
| F12 | 새 모양을 넣으려면 지금은 레인(시간·접촉)과 배치 대여(높이)를 함께 고쳐야 한다. 모양마다 바뀌는 지식 셋이 두 클래스에 흩어져 있다. | F3~F5 |
| F13 | 이름 겹침: `CombatStepKind`는 골격(`LifeCycles/Combat/Plans/CombatStep.cs`)에 이미 있다. 그래서 걸음 종류 이름은 `CombatMovementKind`로 한다. | 저장소 검색 |

## 2. 설계

### ST1. 걸음 하나의 입력 = 현재 칸, 도착 칸, 이동 종류

- **현재 칸·도착 칸**은 Core 걸음 사건 그대로(`Expected`·`Final`) 쓴다. 칸 값을 따로 만들지 않는다(SSOT, F2).
- **이동 종류** 새 열거 `CombatMovementKind`: `Voluntary`(경로 걸음, 아군·적 모두) · `Forced`(넉백·끌어오기처럼 밀림).
  - 정하는 곳은 지금 판정 `IsVoluntaryMovement` 하나다. `CombatMovementPresentation.BeginStep`이 이 결과를 종류 값으로 바꿔 레인에 넘긴다. 끌어오기는 `Forced`다.
  - 종류가 정하는 것은 셋이다: 걸음 전 회전 여부(자발만), 접촉 게이트 겹침 규칙 1(자발만), 걸음 모양(ST2). 지금 `rotate`·`voluntary` 두 이름으로 내려가던 bool을 종류 하나로 바꾼다(F6).
  - 접촉 게이트(`CombatStepContactGate.BeginStep(moved, voluntary)`)는 서명을 바꾸지 않는다. 레인이 `kind == Voluntary`를 넘긴다. 게이트는 겹침 규칙만 알면 되기 때문이다.
- 나중 종류(장애물 건너뛰기 등)는 Core가 사실로 알려 줄 때 열거 값 하나와 표 한 줄을 더한다(ST6).

### ST2. 걸음 모양 전략: 모양마다 다른 지식은 모양 클래스 하나에

`Presentation/Movement/`에 둔다. 공격 종류 전략(`CombatAttackStyle` + 표 `CombatAttackStyles`, 폴더 안내 §2)과 같은 꼴이다.

| 형식 | 맡는 것 |
|---|---|
| `CombatStepStyle`(추상) | 걸음 하나의 시간 `Seconds`, 높이 `HeightAt(fromHeight, toHeight, progress)`, 접촉 시점 `ContactProgress(moverStart, moverEndCenter, source)` |
| `CombatWalkStepStyle` | 지금 걸음 그대로. 시간 = `movementStepSeconds`, 높이 = `CombatStepHeight.At`(같은 높이 평평, 다르면 `stepClimbHop` 호), 접촉 = 몸이 지나며 처음 닿는 순간(M2) |
| `CombatHopStepStyle` | 뛰기. 시간 = `hopSeconds`, 높이 = 늘 호(꼭대기 = 두 칸 중 높은 칸 표면 + `hopHeight`), 접촉 = 착지(진행률 1, ST5) |
| `CombatStepStyles` | 종류 → 모양 표. `Forced` → 걷기, `Voluntary` → 설정이 고른 모양(ST4). 표를 만들 때 열거의 모든 값이 있는지 검사한다(빠지면 만들 때 실패) |

- **가로는 지금처럼 직선·등속이다.** 완급 곡선은 넣지 않는다(Q-ST4).
- **높이 식 원본은 `CombatStepHeight` 하나다.** 지금 식에서 호 부분을 `Arc(from, to, progress, peakAboveHigher)`로 꺼낸다. `At`은 "같은 높이면 평평, 아니면 `Arc`"로 남긴다. 걷기는 `At`을, 뛰기는 `Arc`를 부른다. 지금 식 `from + rise·t + (|rise|/2 + hop)·4t(1−t)`은 같은 높이에서 꼭대기 `from + hop`인 포물선이 되므로, 뛰기가 같은 식을 그대로 쓴다. 수식 사본은 없다.
- 모양 객체는 상태 없는 불변 객체다. 전투 연결마다 한 번 만든다(매 프레임 할당 없음).

### ST3. 레인·배치 대여는 모양에 묻기만 한다

- 배치 대여 계약 `ICombatUnitPlacement.Interpolate(from, to, mapper, progress, float climbHop)`의 마지막 인자를 `CombatStepStyle style`로 바꾼다. Placement는 지금처럼 두 칸 표면 높이를 구하고, 높이는 `style.HeightAt`에 묻는다.
- 걸음 레인 `CombatUnitMotionPlayback`:
  - 생성자의 `stepSeconds`·`stepClimbHop`을 표 `CombatStepStyles` 하나로 바꾼다. `turnSeconds`는 남긴다(회전은 종류의 일이고 모양의 일이 아님).
  - `BeginStep(step, bool rotate, …)`를 `BeginStep(step, CombatMovementKind kind, …)`로 바꾼다. 걸음을 시작할 때 모양을 한 번 고른다(`_style = _styles.For(kind)`).
  - 보간 시간은 `_style.Seconds`, `ArmContacts`는 `_style.ContactProgress`를 쓴다. 회전 여부와 게이트 인자는 `kind == Voluntary`다.
  - 끌어오기 `TickPull`은 `_styles.For(Forced)`의 시간·높이를 쓴다.
- 레인 집합 `CombatUnitMotionSet`도 생성자·`BeginStep`을 같은 꼴로 바꾼다. 사건 Presenter의 생성 줄 하나가 표를 넘긴다.
- **Tick에 새 판정이 없다.** 모양 고르기는 걸음 시작 때 한 번이고, Tick은 고른 모양에 묻기만 한다.

### ST4. 값 자리: 전투 연출 설정 표 (Q-ST1)

설정 표 `CombatPresentationSettingsData`의 "유닛 이동 연출" 묶음:

| 칸 | 기본값 | 뜻 |
|---|---|---|
| `movementStepSeconds` | 0.2 (그대로) | 걷기 걸음 시간. 밀림도 이 값을 쓴다 |
| `stepClimbHop` | 0.1 (그대로) | 걷기에서 높이가 다른 칸을 오르내릴 때의 추가 뜀 |
| 새 `voluntaryStepLook` | 에셋에서 `Hop` | 자발 이동의 모양. 새 열거 `CombatStepLook { Walk = 0, Hop = 1 }` |
| 새 `hopHeight` | 0.3 (조정 대상) | 뛰기 꼭대기가 두 칸 중 높은 칸 표면보다 올라가는 높이(필드 로컬 단위) |
| 새 `hopSeconds` | 0.25 (조정 대상) | 뛰기 한 번의 시간 |

- 값 조건의 원본은 설정 Data 한 곳이다. 지금 `ValidateStepSeconds`·`ValidateStepClimbHop` 옆에 둔다. 뛰기 칸은 `Walk`일 때 쓰지 않지만 검사는 늘 한다(잘못된 값이 남아 있다가 `Hop`으로 바꿀 때 터지지 않게).
- 열거 0 = `Walk`이므로, 값을 넣지 않은 에셋은 지금처럼 걷는다. 기본 에셋 `CombatPresentationSettings_Default.asset`에 `Hop`을 넣는다. **(정정 §7.2: 칸 코드 기본값이 `Hop`이라 칸이 없는 에셋도 `Hop`이다. `Walk`는 설정 없이 만든 값 묶음뿐이다.)**
- View 값 흐름:
  - 새 값 객체 `Playback/Support/CombatStepLookSettings`(모양·뛰기 높이·뛰기 시간, `From(settings)`)를 둔다. 추락 연출 `CombatFallMotionSettings`와 같은 꼴이다.
  - `CombatEventPresentationValues` 생성자의 끝 선택 인자로 받는다. 없으면 걷기이고, 지금과 같다.
  - 그래서 천명님 시험(F8)과 Factory는 그대로다.
- 유닛마다 다른 모양이 필요해지면 유닛 외형에 덮어쓰기 칸 하나를 두고, 표의 고르기 한 줄만 바꾼다. 모양 클래스·레인은 바뀌지 않는다(지금은 하지 않음).

### ST5. 접촉 시점

- 걷기는 이동 접촉 설계 M2 그대로다.
- 뛰기는 착지 순간(진행률 1)이다. 몸이 공중을 지나므로 가시·오브젝트에 닿는 것은 내려앉을 때다. 걸음 사건 완료(첫 묶음 해제)도 착지 순간이 된다. 겹침 규칙과 Core 순서 해제는 그대로다.
- 구현 때 이동 접촉 설계 M2에 "접촉 진행률은 걸음 모양이 정한다(걷기 = 지나며 닿는 순간, 뛰기 = 착지)" 보충을 단다.

### ST6. 장애물 건너뛰기 등 나중 이동 방식 (지금 만들지 않음)

- **필요한 것(Core):** Core가 "두 칸 걸음"(가운데 칸 진입 효과 없음)이나 이동 종류를 사건 사실로 내야 한다. 생길 때 Core 요청 문서를 쓴다.
- **View에서 더할 것:**
  - `CombatMovementKind.Leap` 값 하나
  - `CombatLeapStepStyle` 하나(거리에 비례한 호, 착지 접촉)
  - 표 `CombatStepStyles`에 한 줄
  - 종류 판정에 갈래 하나
- **바뀌지 않는 것:** 레인, 배치 대여, 접촉 게이트, 경로 입력.
- 이번 구조가 "현재 칸·도착 칸·방식을 받아 처리"하는 자리다. 현재·도착 칸은 사건 사실이고, 방식은 종류 → 모양이다.

### ST7. 범위 밖

- 뛰기 클립(애니메이션), 착지 소리, 착지 이펙트: 지금 없다. 붙일 자리는 모양(착지 시점) + 클립 채널·소리 표·큐다. 다음 설계에서 다룬다.
- 경로 입력, 이동 범위 표시, 미리보기 고스트: 바뀌지 않는다(고스트는 포인터를 따라가는 연출이라 걸음 모양과 무관).
- 토네이도 들어 올림(호진님 `Relocated`): 따로 재생하므로 바뀌지 않는다.
- 보고만: 끌어오기 `TickPull`이 걸음 Tick과 비슷한 보간 반복을 따로 갖는다(이번에는 모양만 같이 쓰게 하고, 반복 합치기는 하지 않음).
- 보고만: 자발 판정이 이름이 "적 연출 간격"인 `CombatEnemyPresentationPacing`에 있다(아군에도 씀). 이번에는 옮기지 않는다.

## 3. 정할 것 (권장안 먼저)

| 질문 | 권장 | 다른 안 | 이유 |
|---|---|---|---|
| **Q-ST1** 모양 값을 어디에 둘까 | **A** 전투 연출 설정 표 하나(모든 유닛 같은 모양) | B 유닛 외형마다 모양 SO 참조(종류 = SO 타입, 투사체 부품 꼴) · C 둘 다(설정 = 기본, 유닛 외형 = 덮어쓰기) | 요청은 "걷기 → 뛰기" 전체 변경이다. 유닛 외형 16개 중 본부·화약통·보급품은 스스로 움직이지 않고, 밀림은 모양과 무관하게 걷기다(Q-ST2). 그래서 B는 지금 쓰임 없이 에셋 16개 연결만 늘린다. 유닛별이 필요해지면 C로 칸 하나와 고르기 한 줄만 더하면 된다(ST4). **앞 채팅 답에서는 유닛 외형을 권했는데, 이 확인 뒤 A로 바꿨다.** |
| **Q-ST2** 넉백·끌어오기의 모양 | **A** 걷기(미끄러짐) 그대로 | B 같이 뛰기 | 밀림은 맞아서 떠밀리는 것이라 뛰면 어색하다. 지금 화면도 그대로다. |
| **Q-ST3** 여러 칸 경로 | **A** 칸마다 한 번 뛰기 | B 경로 전체를 큰 호 하나로 | Core가 칸마다 사건을 내고, 지나는 칸의 진입 효과(지뢰·가시)도 그 칸에서 일어난다. B는 공중에서 지뢰가 터지는 식으로 화면과 사실이 어긋난다(F1). |
| **Q-ST4** 뛰기 값 | **A** 높이·시간 둘(가로 등속, 포물선) | B 완급 곡선·착지 멈춤도 | A로 먼저 보고, 손맛이 모자라면 모양 안에 칸을 더한다. 레인은 바뀌지 않는다. |
| **Q-ST5** 뛰기의 접촉 시점 | **A** 착지 순간 | B 지금처럼 몸이 지나며 처음 닿는 순간 | B면 몸이 공중에 있을 때 가시가 반응할 수 있다. |
| **Q-ST6** 뛰기 전 회전 | **A** 지금처럼 먼저 돌고 뛴다 | B 돌면서 뛴다 | 지금 순서(회전 → 보간)를 그대로 써서 바뀌는 곳이 없다. |
| **Q-ST7** 장애물 건너뛰기 | **A** 지금은 자리만(ST6) | B 이번에 Core 요청 문서까지 | 규칙(어떤 장애물을 누가 넘나, 비용)이 아직 없다. 규칙이 정해지면 요청을 쓴다. |

## 4. 바꾸는 곳 (예상, 권장안 기준)

| 파일 | 바뀌는 것 | 영역 |
|---|---|---|
| `DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs` | 칸 셋·검사 둘·읽기 셋 | View DB(호진님 설정 표 메모 대상) |
| 새 `…/CombatPresentationSettings/Item/CombatStepLook.cs` | 열거 | View DB |
| `DataBaseSO/ViewGroup/CombatPresentationSettings/…/CombatPresentationSettings_Default.asset` | `Hop`·기본값 | 에셋 |
| `Playback/Support/CombatEventPresentationValues.cs` | 끝 선택 인자, `From` | 우리 |
| 새 `Playback/Support/CombatStepLookSettings.cs` | 값 객체 | 우리 |
| `Presentation/Movement/CombatStepHeight.cs` | `Arc` 꺼내기(값 같음) | 우리 |
| 새 `Presentation/Movement/CombatMovementKind.cs`·`CombatStepStyle.cs`·`CombatWalkStepStyle.cs`·`CombatHopStepStyle.cs`·`CombatStepStyles.cs` | 종류·전략·표 | 우리 |
| `Presentation/Movement/CombatUnitMotionPlayback.cs`·`CombatUnitMotionSet.cs` | 생성자·`BeginStep`·시간·접촉 | 우리(천명님 `4bd6b574` 끌어오기 줄 포함 → 변경 정리로 알림) |
| `Playback/Support/CombatMovementPresentation.cs` | 종류로 넘김 | 우리 |
| `Playback/CombatSceneEventPresenter.cs` | 생성 줄 하나 | 우리 |
| `Display/Unit/ICombatUnitPlacement.cs`·`CombatUnitPresentationWindow.cs` | 마지막 인자 | 우리 |

- Core·Framework·Factory·입력·경로 입력: 무변경.
- 새 `.cs`는 일곱(+ `.meta`)이다. 설정 SO 코드와 에셋은 한 커밋이어야 한다.
- 시험: 새 시험 없음(View 규칙). 천명님 `PullAndMarkPresentationTests`는 끝 선택 인자라 그대로다. 남은 시험 폴더에 이동 보간·레인을 부르는 시험은 없다(검색).

## 5. 7기준 점검 (예상, 알려진 문제 기준)

| 기준 | 지금 | 권장안 뒤 | 근거 |
|---|---|---|---|
| SSOT | 8 | 9 | 지금도 높이 식·자발 판정은 한 곳이지만, 같은 사실(자발 여부)이 `rotate`·`voluntary` 두 이름으로 내려간다. 뒤에는 종류 하나, 모양 지식은 모양 클래스 하나, 높이 식은 `CombatStepHeight` 하나, 값 조건은 설정 Data 하나다. |
| SOLID | 7 | 8.5 | 지금은 새 모양 = 레인·배치 대여 수정이다(OCP, F12). 뒤에는 새 모양 = 클래스 하나 + 표 한 줄이다. 레인은 시간·순서만, 모양은 모양만 맡고(SRP), 레인은 추상 모양에 기댄다(DIP). 남는 것: 레인이 걸음·끌어오기·방향 복귀 셋을 맡는다(이번 범위 밖). |
| FW 지침·계약 | 9 | 9 | Framework·Factory 무변경, DB는 SO 입력 → Data(P2), 값 조건은 Data 쪽, View DB만 쓴다(P5). |
| 기능 변경의 독립성 | 7 | 9 | 뛰기 느낌 조정은 설정 칸만 바꾸면 된다. 새 방식은 `Movement/` 폴더 안에서 끝난다. Core·입력·미리보기와 무관하고, 다른 사람 시험 영향도 없다. |
| 캡슐화 | 8 | 8.5 | float 셋이 생성자 셋을 지나던 것이 표 하나가 된다. 모양 안의 식은 바깥에 보이지 않는다. 남는 것: 끌어오기 반복이 레인 안에 따로 있다. |
| C#·Unity 권장 구조 | 8.5 | 9 | 상태 없는 sealed 전략, 할당 없는 Tick, 기존 `SerializeField`·`Min`·`Tooltip` 꼴, 열거 0 = 안전한 기본(Walk)이다. |
| 무의미한 If·Tick | 8 | 9 | bool 갈래가 종류 → 전략으로 바뀌고, 매 프레임 판정은 없다. 남는 갈래는 종류 판정 if 하나와 "값 객체 없으면 걷기" 하나다. |
| **합** | **약 7.9** | **약 8.9** | |

## 6. Phase (결정 뒤 Task 계획에서 자세히)

1. **ST-a 값:** 열거·설정 칸·검사·읽기, 값 객체, 끝 선택 인자, 기본 에셋 `Hop`. 아직 아무도 읽지 않아 화면 변화는 없다.
2. **ST-b 모양:**
   - 넣을 것: 종류·전략 둘·표, `CombatStepHeight.Arc`, 배치 대여 인자, 레인·집합·이동 표시·Presenter 한 줄.
   - 결과: 화면에서 칸마다 뛴다.
3. **ST-c 문서:**
   - 이동 접촉 설계 M2 보충, 고지대 설계 HG-3에 "높이 식 원본 그대로, 뛰기는 `Arc`" 한 줄.
   - 폴더 안내 §1.1 `Movement/` 줄, 호진님 설정 표 메모 새 절, 천명님 끌어오기 줄 변경 알림.
   - 묶음 끝 검토 하나.

Unity 확인(안):
- 아군·적 경로 이동이 칸마다 뛴다.
- 고지대를 오르내릴 때 꼭대기가 높은 칸 위로 지난다.
- 넉백·끌어오기는 지금처럼 미끄러진다.
- 가시·지뢰 칸에 뛰어들 때 착지 순간에 반응한다.
- 설정을 `Walk`로 바꾸면 지금과 같다.
- 마지막 걸음 뒤 방향 복귀와 다음 행동 간격은 그대로다.

## 7. 구현 메모 (2026-10-28, ST-a~ST-c 뒤)

### 7.1 설계대로 된 것

- ST1: 이동 종류 `CombatMovementKind`(Voluntary·Forced)를 `CombatMovementPresentation.BeginStep` 한 곳에서 `IsVoluntaryMovement`로 정한다. 레인·집합은 `BeginStep(step, kind, …)`로 받고, 접촉 게이트에는 `kind == Voluntary`를 넘긴다(게이트 서명은 그대로). 걸음 끝 방향 복귀 판정(`EndMovement`)은 `IsVoluntaryMovement`를 그대로 쓴다(계획 P11).
- ST2: 추상 모양 `CombatStepStyle`(`Seconds`·`HeightAt`·`ContactProgress`), 걷기 `CombatWalkStepStyle`·뛰기 `CombatHopStepStyle`, 인스턴스 표 `CombatStepStyles`(종류 배열, 생성자에서 모든 종류 검사, 정적 `From(values)`). 높이 식은 `CombatStepHeight.At`·`Arc` 한 곳이다.
- ST3: 배치 대여 `ICombatUnitPlacement.Interpolate` 끝 인자가 모양이다. 레인은 걸음을 시작할 때 고른 모양에 시간·높이·접촉을 묻는다. 끌어오기는 밀림 모양이다(계획 P10).
- ST4: 설정 칸 셋(`voluntaryStepLook`·`hopHeight`·`hopSeconds`), 값 객체 `CombatStepLookSettings`, 값 묶음 끝 선택 인자. 천명님 `PullAndMarkPresentationTests:414`·Factory는 그대로다.
- ST5: 뛰기의 접촉은 착지(1)다. 걷기는 이동 접촉 설계 M2 그대로다(M2에 보충 한 줄).

### 7.2 설계·계획과 다르게 한 것

- **ST4 정정(§2 ST4 끝 문장):** 설계는 "열거 0 = `Walk`라 값을 넣지 않은 에셋은 걷는다"고 적었다. 그런데 계획 Ta.2가 칸 코드 기본값을 `Hop`으로 정했고, Unity는 칸이 없는 에셋을 코드 기본값으로 읽는다. 그래서 칸이 없는 에셋도 `Hop`이다. `Walk`가 되는 것은 설정 없이 만든 값 묶음(`CombatStepLookSettings.Walk`, 시험의 7인자 생성)뿐이다. 운영 에셋은 하나이고 `Hop`을 적어 두어 화면 차이는 없다. `CombatStepLook.cs` 문서 주석도 이 내용으로 고쳤다(ST-c).
- 설정 검사 자리(계획 Ta.2 ③): "`stepClimbHop` 검사 뒤"가 아니라 `Validate()` 맨 뒤다. 파일 규칙 "새 값은 맨 뒤"를 따랐다(계획 §5.1).
- 레인(계획 Tb.8)에서 작은 것 셋(계획 §5.2): 이동 종류 값도 모양처럼 취소 때 비우지 않는다(걸음·끌어오기를 시작할 때마다 정함). 걸음을 시작할 때 모양을 먼저 골라 모르는 종류면 상태를 바꾸기 전에 예외가 난다. 표 생성자는 음수 종류 값을 거절한다.
- **묶음 끝 검토 반영(계획 §6.3):** ST1·ST6 보강 — 종류의 성질("스스로 하는 이동인가": 걸음 전 회전·접촉 Gate 겹침 규칙 1·앞 행동 강조 지우기·경로 끝 방향 복귀)을 새 `CombatMovementKinds.IsSelfDriven` 한 곳에 두었다. 그래서 새 이동 방식은 "종류 값 하나 + 성질 한 줄 + 모양 클래스 + 표 한 줄"이고, 레인·이동 표시는 바뀌지 않는다(ST6의 "값·모양·표 한 줄"에 성질 한 줄이 더해짐). 레인은 종류 대신 성질 하나만 들고, 끌어오기는 모양만 정한다. ST2 보강 — 모양에 `TryGetFixedContact`를 더해 뛰기(착지 고정)는 몸을 구하지 않는다. 문서의 "밀림 = 늘 걷기" 사본을 걷어 원본을 `CombatStepStyles.From` 하나로 두었다(설정 SO 툴팁만 기획자용으로 남김).

### 7.3 남은 것

- 뛰기 전용 클립·착지 소리·이펙트는 범위 밖이다(ST7). 착지 소리를 넣으면 소리 창구(소리 표 한 줄)와 함께 정한다.
- 장애물 건너뛰기는 자리만 열었다(ST6). Core가 두 칸 걸음을 사실로 낼 때 종류 값 하나·모양 클래스 하나·표 한 줄을 더한다.
- 공유(모두 미전달): 호진님 설정 표 메모 §9, Core 전달 문서 §47(천명님 끌어오기 줄).
- **추가 요청(2026-10-28, 계획 §7):** 끌어오기 대상은 먼저 끌려가는 방향으로 돌고, 도착하면 원래 방향으로 돌아온다(걸음 경로와 같은 회전·복귀 장치, 회전 계산은 레인 `TurnToward` 하나). 옮기는 모양은 Q-ST2대로 걷기이고, 넉백은 돌지 않는다.
