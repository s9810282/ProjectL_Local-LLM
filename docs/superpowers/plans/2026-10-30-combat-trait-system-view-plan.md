# 전투 특성 시스템(새 Core 계약) — View 대응 Task 계획

- 날짜: 2026-10-30
- 상태: **계획 작성, 코드 착수 전.** 설계 결정 Q-TS1~Q-TS8은 권장안(모두 A)으로 적었다(사용자 확정 대기). 다른 답이 나오면 바뀌는 Task를 §1.3에 적었다.
- 요청: 사용자 "검토하면서 계획 작성해"(7기준: SSOT·SOLID·FW 계약·독립성·캡슐화·C#/Unity 권장 구조·무의미한 If/Tick).
- 설계: [특성 시스템 View 대응 설계](../specs/2026-10-29-combat-trait-system-view-design.md)(TS1~TS9). 이 계획의 §0 검토가 설계를 보완하고, 보완한 곳은 설계 §9에 옮겨 적는다(Ta.0).
- Core 원본: [Core → View 인계](../handoffs/2026-10-28-trait-system-core-view-handoff.md) §2~§10.
- 기준 판: `6b0755be2` 위 커밋 전 작업(겹친 강조 3판~6판, 특성 발동 연출 TA-a~TA-f 반영분 등). 커밋 전 작업은 `stash@{0}` 백업이 있다.
- 작업 규칙: View 작업이라 새 시험 코드는 쓰지 않고 MSBuild도 돌리지 않는다(보고에 "컴파일 미확인"). 기존 시험은 우리 변경으로 컴파일이 깨지는 곳만 최소로 맞춘다. 검토 에이전트는 묶음 끝(TS-g)에 한 번.
- 2026-10-31 추가: **TS-h 위치 교환 승객 이동**(§2 TS-g 뒤). **같은 날 대신함 — 하지 않음:** 사용자가 이펙트 구조 2판 Q-EL4에서 "위치 이동 유닛 액션으로 추가"·"유닛 행동(차례로)"를 골라, 위치 교환은 [이펙트 구조 계획](2026-10-31-combat-effect-structure-plan.md) EL-f가 맡는다(TP-31~TP-36 버림).

## 0. 검토 (7기준, 설계 대조)

설계 초안을 지금 코드와 Core 계약에 대어 본 결과다. "설계 그대로" 구현하면 생기는 문제와 계획에서 보완한 방법이다.

### 0.1 찾은 것

| | 기준 | 심각도 | 내용 | 계획의 보완 |
|---|---|---|---|---|
| R1 | SSOT·독립성 | **중간(결함)** | 설계 TS4는 "추가 공격도 첫 해제에 완료(`FirstRelease`), 맞는 대상은 공격 그룹으로 뒤따르는 체력 사건이 정함 — 따로 고칠 것 없음"이라 했다. 그러나 맞는 순간 대응(`CombatHitFollowUps.Begin`)은 **행동 시작 사건에서만 출처를 연다**(`:63-66`, 공격·포효). 추가 공격 시작은 특성 발동 사건이라 출처가 안 되어, 자식 공격의 체력 사건이 그 출처의 후속으로 대응되지 않는다. 그러면 `FirstRelease`의 유효 해제가 한 번도 없어 사건 완료가 **공격 연출 전체 종료까지 밀리고**, 피해 숫자·피격 반응이 맞는 순간이 아니라 공격이 다 끝난 뒤에 나온다. 동시 묶음(`CombatConcurrentGroups.TryGetKey:211`)도 이동 후속의 열쇠를 공격 사건에서만 얻는다 | 공격 출처를 값 하나(`CombatAttackSource`: 공격자·공격 그룹·관통 기록)로 일반화해 형식 표가 공격·포효·**추가 공격 시작** 셋에서 답하게 하고, 맞는 순간 대응이 한 행동 구간 안에서 출처를 여럿(열쇠별) 갖게 한다(Tb.1~Tb.2) |
| R2 | SSOT | 낮음 | 설계 TS4가 `CombatAttackShape`에 공격 그룹 칸을 다시 넣는다고 했는데, Gate·후속 대응은 출처 사건으로 찾고 그룹은 형식 표가 답한다. 모양에 그룹을 두면 같은 사실이 두 곳이다 | 모양에 그룹을 넣지 않는다(설계 TS4 한 줄 철회) |
| R3 | 독립성 | 낮음 | 발동·상태 사건에 근원을 주면(TS1·TS3) 그 주체가 **출처 공격자 자신**인 사건(가속 소모·사기 진작 부여 등)이 공격의 "맞은 대상"으로 세어진다(`CombatHitFollowUps.Add`는 공격자 이동만 빼고 있음, `:104-107`). 아무도 안 맞은 공격도 그 사건 하나로 첫 해제가 된 것처럼 완료가 당겨진다 | "출처 공격자 자신의 후속은 대응하되 해제 대상에 넣지 않는다"를 이동만이 아니라 모든 후속에 적용한다(Ta.4). 그 사건은 Gate 규칙대로 공격 전체 종료 뒤에 나온다 |
| R4 | SSOT | 낮음 | 설계 TS5는 걸음 종류 판정 한 곳만 바꾼다고 했지만, 같은 추론(`IsVoluntaryMovement`)을 **적 행동 간격**(`CombatEnemyPresentationPacing.BeforeStep:47`)도 쓴다. 한쪽만 바꾸면 두 규칙이 갈린다 | 판정 원본을 `CombatMovementKinds.Of(step)` 하나로 옮기고 간격·걸음 모양·경로 끝 방향 복귀가 모두 그것을 부른다(Tc.1) |
| R5 | 기능·캡슐화 | 낮음 | 설계 P7은 새 거절 사유(무장 해제·발목 꺾기) 안내를 "확인만"이라 했는데, 확인 결과 안내 문구가 **없다**(`CombatActionNotice.ShowActionFailure`에 두 사유가 없어 거절 결과가 와도 아무 말 없음, 행동 상태 짧은 글자 `CombatActionStateText`는 "사용불가"로만 보임) | 안내 두 줄 + 짧은 글자 두 줄(Td.4, 안내 파일은 현강님 줄이라 공유). 계획에서 정한 것 TP-9 |
| R6 | 기능 | 낮음 | 설계에 빠짐: TA-c에서 "Core 사실에 칸이 없어 비움"으로 둔 발동 큐의 **칸 자리·대상 여럿**이 이제 채울 수 있다(`TargetCells`·`TargetIds`). 지금 처리기는 `TargetId`(하나일 때만)와 빈 칸 목록을 넘긴다(`CombatTraitEventHandler.PointsOf:98-103`) | 대상 몸은 `TargetIds` 모두, 칸 자리는 `TargetCells` 모두로 넘긴다(Ta.5) |
| R7 | C#/Unity 권장 | 낮음 | 추가 공격 시작은 Core가 발동 하나짜리 사건으로 만든다(`AttackResultFactory.AttachChild`의 `Traits.Single()`). 지금 처리기는 "둘째 추가 공격은 경고 뒤 건너뜀"으로 받는다. 계약 위반을 경고로 삼키면 원인이 묻힌다 | 한 사건에 추가 공격 시작이 둘이면 계약 오류로 던진다(Tb.5, TP-6) |
| R8 | 무의미한 If | 낮음 | `CombatHitCompletion.AnyRelease`는 "후속을 출처에 맺지 못하는" 추가 공격 전용이었다. R1을 고치면 쓰는 곳이 없다 | 값과 Gate 갈래를 걷는다(Tb.4) |
| R9 | 독립성 | 낮음 | 흔적 표시(TS7)는 사실 처리기 창구에 새 칸이 필요하고, 창구 생성자를 천명님 `PullAndMarkPresentationTests:442`가 부른다 | 창구 끝에 칸을 더하고 그 시험 줄에 인자 하나를 맞춘다(컴파일 맞춤, Core 전달 문서 §5에 기록, Te.3) |
| R10 | 독립성 | 기록 | TS8(대신 맞기)은 설계가 "지금 구조에 없는 것 넷"을 적었을 뿐 조립 모양(두 유닛 실행·준비 조건·접촉 몸 바꾸기·나간 자리 피격)을 정하지 않았다. 이 상태로 Task를 쓰면 계획이 설계를 대신하게 된다 | TS-f 첫 Task를 상세 설계(결정 받기)로 두고, 코드 Task는 그 뒤에 쓴다(Tf.0) |
| R11 | 독립성 | 기록 | 대상이 여럿인 발동(넉백 적용·공격력 부여 묶음)은 주체가 발동 유닛(대개 공격자)이 되어, TP-5대로 공격 연출이 다 끝난 뒤에 큐가 나온다. 뒤따르는 넉백 걸음은 다른 주체라 먼저 간다(큐만 늦음) | 이번에는 그대로 둔다(그 특성들의 큐는 아직 비어 있음, §4). 거슬리면 "주체 없는 묶음 원소"를 다음 묶음에서 설계 |

설계에서 맞게 본 것(확인): 디버프 생성자는 선택 인자라 컴파일은 그대로다. 추가 공격 시작 사건은 늘 방향(`AttackDirection`)과 결과 근원(`ResultOrigin`, 그룹 > 0, 가해자 = 발동 유닛)을 갖는다(`FollowUpAttackExecutor.cs:108-112`·`:174-177`, `WithChildAttack` 검사). 이동 원본 사건의 `Expected` = null(흔적 생성)을 지금 View가 받아도 깨지지 않는다(`CombatTerrainChanges`·`CombatStepContacts:160`이 null을 거름). 우리 View 코드를 부르는 남은 시험은 천명님 `PullAndMarkPresentationTests` 하나다(`CombatAttackShape.Of`·`CombatFieldEventPorts`).

### 0.2 점수 (알려진 문제 기준, 예상)

| 기준 | 지금(병합 직후) | 설계 그대로 | 계획(보완 반영) |
|---|---|---|---|
| SSOT | 7.5 (추가 공격 종류 목록·이동 추론·디버프 칸 사본) | 8.75 (R1 출처 두 갈래, R4 추론 남음) | 9.5 (표식 칸은 Core 함수가 오기 전까지 사본) |
| SOLID | 8.0 | 9.0 | 9.25 |
| FW 계약 | 9.5 | 9.5 | 9.5 (Factory 연결 한 줄) |
| 독립성 | 7.0 (상태가 결과 끝에 몰림, 공격 중 발동이 피격을 끊음) | 8.0 (R1 추가 공격 피해가 늦음, R3) | 9.25 |
| 캡슐화 | 8.5 | 8.75 | 9.0 |
| C#/Unity 권장 구조 | 8.5 | 8.75 (R7) | 9.0 |
| 무의미한 If/Tick | 7.5 (A4 쓸데없는 기다림, 2연발 경고) | 8.5 (R8 남은 갈래) | 9.25 |
| **합** | **약 7.8** | **약 8.7** | **약 9.3**(TS-f까지) |

TS-f(대신 맞기)를 빼고 TS-e에서 멈추면 약 9.1이다(A7 "멀리 선 수호자가 맞음"이 기능 결함으로 남음).

## 1. 계획에서 정한 것 (사용자 확인 대상)

### 1.1 TP 목록

- **TP-1 공격 출처 값.** 새 값 `Playback/Ordering/CombatAttackSource`(공격자·공격 그룹·관통 기록, 관통은 없으면 null). 형식 표의 공격 출처 칸이 이 값을 돌려주고, 맞는 순간 대응·동시 묶음이 이것만 읽는다. 지금 `TryGetAttackSource(e, out attacker, out AttackResult)`의 `AttackResult`는 그룹·관통 둘만 읽혀서 값으로 줄인다.
- **TP-2 한 행동 구간의 출처 여럿.** 맞는 순간 대응은 행동 시작에서 출처 목록을 비우고, 추가 공격 시작(`OpensAttackSource`)에서 출처를 하나 더한다. 피해 후속은 근원 열쇠(그룹 + 가해자)로 출처를 찾고, 이동 후속(근원 없는 넉백)은 **가장 최근에 연 출처**에 붙인다. 근거는 Core 순서다: 원래 공격의 넉백(`AttackDeliveryExecutor.cs:34-35`) → 도착 정산(`:39`) → 처치 연쇄(`:42`) → 추가 공격(`:46`). 추가 공격 안에서도 같은 실행기를 다시 거치므로 순서가 같다.
- **TP-3 발동 사건의 근원.** 발동 사건(`CombatEffectsEvent`)의 근원 = 그 안 발동 **모두**의 `ResultOrigin`. 하나라도 비면 근원 없음(장벽, 지금처럼). 추가 공격 시작은 근원을 주지 않는다 — 그 사건은 출처를 여는 사건이라 공격 사건처럼 장벽이어야 한다.
- **TP-4 발동 사건의 주체(설계 P1 구체화).** ① 추가 공격 시작 → 발동 유닛. ② 발동이 모두 같은 대상 하나(`TargetId`) → 그 대상. ③ 그 밖에 발동 유닛이 모두 같으면 → 발동 유닛. ④ 아니면 주체 없음(장벽). 직군 발동은 보지 않는다(지금 그대로).
- **TP-5 공격자 자신의 후속(R3).** 근원으로 대응된 후속이든 이동 후속이든, 주체가 출처 공격자면 대응은 하되 해제 대상에 넣지 않는다. 그 사건은 공격 전체 종료 뒤에 나온다(지금 공격자 체력 사건과 같은 결과, Gate 규칙 1).
- **TP-6 추가 공격 시작 계약.** 추가 공격 시작 사건의 발동이 둘 이상이거나, 방향(`AttackDirection`)이 없거나, 근원이 없으면 `InvalidOperationException`(Core 계약 위반). 발동 유닛이 보인 사실에 없으면(몸이 사라짐, 인계 §3) 공격 연출 없이 큐만 내고 사건을 이어 간다.
- **TP-7 이동 종류 판정.** `CombatMovementKinds.Of(step)`: 원인이 Forced → Forced, 원인이 Voluntary 또는 없음 → Voluntary(인계 §5 "Ordinary voluntary Moved … null cause retains voluntary compatibility"). 걸음 경로(`Moved`)에만 묻는다(Relocated·Returned는 걸음 레인 경로가 아님). 간격의 "한 결과에서 첫 자발 걸음만 행동으로 셈"(`_movementStarted`)은 남긴다.
- **TP-8 HUD 값 줄.** 정수 값 상태(공격력 부여 합·보호막 합)는 새 줄 꼴 `StatusRow.OfAmount`(0이면 안 걸림)로 읽는다. 방어구 파괴는 상태가 있어도 겹친 원본이 0개면 안 걸림으로 본다.
- **TP-9 새 거절 안내(R5).** 안내: 공격 `Disarmed` → "무장 해제 상태라 공격할 수 없습니다.", 이동 `AnkleBroken` → "발목이 꺾여 이동할 수 없습니다."(문구 초안, 현강님 `CombatActionNotice` 임시 Notice 패널의 사유 표에 두 줄). 행동 상태 짧은 글자: "무장해제"·"발목꺾임"(`CombatActionStateText`).
- **TP-10 흔적 자리.** 흔적 외형은 칸 **표면** 높이(타일 + 지형, `CellSurfaceToLocal`) + 외형 높이 보정에 놓는다. 회전은 필드 기준 R0. 한 칸에 흔적이 여럿이면 원본 ID마다 따로 놓고 겹친 순서는 원본 ID 순서다.
- **TP-11 흔적 임시 프리팹.** 땅 다지기·먼지 연막 아트가 없어, 낭떠러지 임시 메시처럼 반투명 사각 메시 둘(색만 다름)을 `Prefabs/Default/Environment/Trail/`에 만들어 특성 외형 줄에 연결한다. 외형 줄에 프리팹이 비면 흔적을 그리지 않는다(경고 한 번).
- **TP-12 대신 맞기는 상세 설계 먼저(R10).** TS-f는 Tf.0 상세 설계에서 조립 모양을 정하고 사용자 결정을 받은 뒤 코드 Task를 쓴다. TS-a~TS-e만으로도 화면이 깨지지 않는다(지금 A7 모습 그대로).

#### TS-f에서 정한 것 (2026-10-30, 유닛 행동 설계 결정 뒤, 사용자 확인 대상)

- **TP-13 돌아오기 대기의 끝 조건.** **(2026-10-30 묶음 끝 검토 M1로 바뀜: 차례 규칙 때문에 지금 시작할 수 없는 후속은 세지 않음, §7.13)** "그 출처의 맞는 순간 뒤 후속 사건 중 그 유닛이 주체인 것이 모두 완료됐고, 그 유닛의 유닛 연출(반응·유닛 실행·걸음)이 비었을 때"다. 설계 AA5는 "유닛 연출이 비면"만 적었는데, 그것만 보면 두 틈에서 일찍 돌아온다. ① 해제가 일어난 프레임에는 후속 사건이 아직 시작 전이다(해제 → 다음 순차 재생 Tick에 시작). ② 피격 반응이 끝나고 사망 사건이 시작하기 전 한 프레임이 빈다. 후속 목록은 맞는 순간 대응(`CombatHitFollowUps`)이, 완료는 순차 재생기 상태가 원본이라 순차 재생기에 좁은 질문 하나(`ICombatSourceSettlement`)를 둔다. 그 출처의 후속만 세므로 행동 실행이 막는 사건을 기다리다 서로 멈추는 일은 없다(후속은 겹침 허용).
- **TP-14 공격 조립의 앞 항목.** 두 공격 조립(코드 모션·클립)이 끝 선택 인자로 "앞 항목"을 받아 조립을 만든 직후 부르고, 뼈대 항목(모션·방향·클립)과 공격 칸 큐의 Start를 그 시점 뒤로 미룬다. 큐 기준 순간표 `CombatCueAnchors`는 Start도 옮길 수 있게 한다(기본은 지금처럼 실행 시작). 인자가 없으면 조립은 지금과 같다.
- **TP-15 돌아오기 대기의 시작 = 뼈대의 Hit 신호 뒤.** 원거리 클립은 클립 끝이 착탄보다 이를 수 있어 End로 걸면 맞기 전에 돌아온다.
- **TP-16 Hit 신호가 끝내 안 나오면** 대기·돌아오기가 시작하지 않는다(조각 신호 규칙). 실행이 끝날 때 대여 반환이 서는 자리를 빌릴 때 자리로 되돌린다(한 프레임 튐, 경고 없음).
- **TP-17 서는 자리 반환 = 빌릴 때 자리.** 자세 대여와 같은 규칙이다. 돌아오기가 끝까지 가면 같은 자리라 반환이 보이지 않는다. 표시가 꺼졌거나 사망 붙잡기 중이면 쓰지 않는다(P8).
- **TP-18 사실 그리기.** 창구 `KeepsPlacement`가 서는 자리 대여도 센다(한 줄). 서는 자리를 빌린 동안 사실 그리기가 위치·방향을 쓰지 않는다.
- **TP-19 안전장치 자리.** 걸음 배치 시작(`CombatUnitPresentationWindow.BeginPlacement`)이 서는 자리를 보면 접고 경고를 한 번 남긴다. 걸음 레인·이동 표시는 바꾸지 않는다.
- **TP-20 몸 조회.** `FieldUnitPresenter`에 "몸 상자를 주어진 자세에 놓는" 함수 하나를 두고 지금 상자 배치 식을 같이 쓴다. ID로 묻는 `CombatShownFacts.TryGetBody`·`StandingBodyOf`만 서는 자리를 본다.
- **TP-21 뛰기 높이 = 걸음 모양 설정의 뛰기 높이**(`CombatStepLookSettings.HopHeight`). 행동 값에 높이 칸을 두지 않는다. Presenter가 공격 시작·특성 처리기에 넘긴다(Factory 무변경).
- **TP-22 반응 줄 찾기.** 공격 시작(`CombatAttackStart`)이 큐 값 답(`CombatCueSources`)을 생성 인자로 받아 (특성 키, Kind)로 찾는다. 찾는 함수는 특성 처리기와 같은 것이다.
- **TP-23 (가) 입구의 실행 완료 콜백은 빈 함수.** 사건은 바로 완료한다(P11).
- **TP-24 희생 에셋은 YAML로 넣는다**(큐는 빈 값, 행동은 P12 값). 값 다듬기는 사용자가 인스펙터에서 한다.
- **TP-25 공격 모양은 반응 발동 사실 목록을 그대로 든다**(`Reactions`). 접촉 대상(`ContactTargetId`)은 그 목록에서 파생한다(새 값 형식을 만들지 않음).
- **TP-26 가로채기 줄 행동 값이 맞지 않을 때**(TS-f4 구현 때 정함, §7.10). 행동 유닛이 `Source`가 아니면 경고 한 번 뒤 행동을 빼고(큐는 냄), 자리가 `GuardFront`가 아니면 경고 한 번 뒤 그 자리 그대로 간다.
- **TP-27 반응 줄 큐는 공격 조립 안에**(TS-f4, §7.10). 시작 = 나가기 시작, 창구는 이펙트·흔들림만(특성 발동 큐와 같음).
- **TP-28 뛰기 높이가 0일 수 있음**(TS-f4, §7.10). 자발 이동을 걷기로 두면 걸음 모양 설정의 뛰기 높이가 0이라 수호자가 미끄러지듯 나간다.
- **TP-29 사건 목록 안 행동의 "대상"**(TS-f5 구현 때 정함, §7.11). 행동 유닛이 `Source`면 대상 = 발동의 첫 대상(없으면 비어 있음 → 대상 쪽은 제자리, 대상 보기는 지금 방향), `Targets`면 대상마다 행동하고 그때 대상 = 발동 유닛.
- **TP-30 바쁨 판정**(TS-f5, §7.11). 행동 유닛의 유닛 실행이 이미 있으면 조각 실행 재생기가 예외를 내므로 먼저 묻는다(새 질문 `CombatPieceRunPlayer.HasUnitRun`). 그다음 실행이 시작하지 않으면(서는 자리를 못 빌림) 바쁨으로 본다. 행동 유닛이 보인 사실의 필드 위에 없으면 경고 없이 건너뛴다.

### 1.2 설계와 다른 것

- R1·TP-1·TP-2: 설계 TS4의 "후속 대응은 따로 고칠 것 없음"을 바꿈.
- R2: `CombatAttackShape`에 공격 그룹을 넣지 않음.
- R5·TP-9: 설계 P7 "확인만" → 두 줄 더함.
- R6: 큐 자리 채우기(설계에 없던 Task).
- R7·TP-6: 둘째 추가 공격 경고 → 계약 오류.
- TP-13: 유닛 행동 설계 AA5의 대기 끝 조건("유닛 연출이 비면")을 "그 출처의 그 유닛 후속이 모두 완료 + 연출 빔"으로 넓힘. 순차 재생기에 좁은 질문 하나가 더 생김(설계 §4 표에 없던 파일).

### 1.3 Q-TS 답이 A가 아니면 바뀌는 Task

| 질문 | B면 |
|---|---|
| Q-TS1 줄 없는 추가 공격 | Tb.5에서 줄이 없으면 공격을 안 그림. 그 사건은 출처를 열지 않아야 하므로 Tb.3의 `OpensAttackSource`가 연출 줄을 봐야 함(형식 표가 View DB를 읽게 됨 — 권장하지 않는 이유) |
| Q-TS2 공격 표시 덮어쓰기 | Tb.3의 `IsFollowUpAttackKind`·DB 검사 갈래를 남김 |
| Q-TS3 표식 칸 | Ta.2의 Core 요청 줄을 뺌 |
| Q-TS4 이동 판정 | Tc.1에서 원인 없음일 때 옛 추론을 남김(간격 상태 `_movementActor`·`_attackStarted` 유지) |
| Q-TS5 흔적 외형 자리 | Te.1이 새 표(SO·Data·조회·표 키·Factory 연결 줄)로 커짐 |
| Q-TS6 HUD 범위 | Td.1~Td.3에서 버프 두 줄을 뺌 |
| Q-TS7 대신 맞기 | TS-f를 빼고 TS-g로 끝냄 |
| Q-TS8 사건 시점 능력치 | Ta.2에서 `FinalStats`를 보인 사실 옆에 보관(읽는 곳 없음) |

## 2. Phase와 Task

TS-a → TS-b → TS-c는 같은 파일(`CombatEventTypeTable`·`CombatHitFollowUps`)을 고쳐 차례로 한다. TS-d·TS-e는 서로 독립이고 TS-a 뒤 아무 때나 할 수 있다. TS-f는 TS-b 뒤(추가 공격 출처 위에 가로채기를 얹음).

### TS-a 상태 사건·표식·발동 근원 (TS1·TS2·TS3, R3·R6)

| Task | 파일 | 할 일 |
|---|---|---|
| Ta.0 | 설계 문서 | 설계 §9 "계획 검토로 바뀐 것"에 §0.1 R1~R10 요지를 적는다(설계 본문은 그대로, 원본은 이 계획) |
| Ta.1 | `Playback/Ordering/CombatEventTypeTable.cs` | `CombatTraitStateEvent` 줄: 처리기 칸 Facts, 주체 `Final.EntityId`, 근원 `Origin`(있을 때 한 개 목록). 표 머리 주석 "사실만 반영하는 형식" 묶음에 둔다 |
| Ta.2 | `Playback/Handlers/CombatFactEventHandler.cs` | ① `CombatTraitStateEvent` → `_facts.ApplyCharacter(e.Final)`(설계 TS1, 능력치는 보관 안 함 Q-TS8). ② 표식(`:50-51`, 천명님 줄): 디버프 여섯 칸을 이름 있는 인자로 모두 옮기고 표식만 바꿈. Core `WithHuntersMark`가 오면 한 줄로 바꾼다(Q-TS3 A, 요청은 TS-g) |
| Ta.3 | `CombatEventTypeTable.cs` | 발동 사건 줄: 근원 = TP-3, 주체 = TP-4(옛 `TraitActivatorOf`를 대신하는 함수 하나). 줄 주석의 "피해 근원이 없어 묶음 밖" 고침 |
| Ta.4 | `Presentation/Attack/CombatHitFollowUps.cs` | TP-5: `Add`도 주체가 출처 공격자면 `countsAsHitTarget: false`(지금 `AddMovement`만 그 규칙). 클래스 설명 고침 |
| Ta.5 | `Playback/Handlers/CombatTraitEventHandler.cs`, `Playback/Support/CombatMomentCuePlayback.cs`, `Presentation/Pieces/CombatCuePoints.cs`, `CombatSceneEventPresenter.cs`(넘김) | R6: `PointsOf`가 `TargetIds` 모두의 서 있는 몸, `TargetCells` 모두의 칸 자리(`CombatCuePoints.ForActivation` 넷째 인자)를 넘긴다. 칸 자리 규칙(칸 표면 가운데·필드 R0 방향, 필드 밖이면 예외)은 지금 순간 큐의 `CombatMomentCuePlayback.CellPoseOf`(private) 하나뿐이라, `CombatCuePoints`의 정적 함수 하나로 옮겨 둘이 같이 부른다. 특성 처리기는 필드 좌표 변환(`Func<FieldGridMapper>`)을 생성 인자로 받는다(Presenter가 넘김, Factory 무변경) |

확인(Ua, 사용자 Unity):
1. 컴파일.
2. 무장 해제·발목 꺾기를 거는 특성(용근님 확인용 배치)이 맞는 순간 대상 HUD 상태가 그때 바뀐다(TS-d 전이라 아이콘은 없지만, 위압·방어 감소·표식처럼 이미 줄이 있는 상태는 사건 순간에 붙고 떨어짐).
3. 표식이 걸린 대상에 무장 해제가 함께 걸린 뒤 표식이 소모돼도 무장 해제가 남는다(사실 기준, HUD는 TS-d 뒤 눈으로 확인).
4. 여러 대상 공격에 피해 변화 특성(추적 사격·멸시 등)이 끼어도 대상들이 함께 맞는다(동시 피격이 차례 재생으로 끊기지 않음).
5. 발동 큐에 `SubjectBody`·`EventCell` 자리를 둔 줄이 대상마다·칸마다 나온다.
6. 콘솔에 "표시 계약이 없는 전투 사건을 건너뜁니다: CombatTraitStateEvent"가 더 나오지 않는다.

### TS-b 추가 공격을 `ChildAttack`으로 (TS4, R1·R2·R7·R8)

| Task | 파일 | 할 일 |
|---|---|---|
| Tb.1 | 새 `Playback/Ordering/CombatAttackSource.cs`(+ `.meta`), `CombatEventTypeTable.cs` | TP-1 값. 형식 표 공격 출처 칸이 이 값을 돌려준다: 공격 사건 = (공격자, 공격 그룹, 관통 기록), 반응 결과가 있는 포효 = 같음, **발동 사건 = 추가 공격 시작 발동의 (발동 유닛, `ResultOrigin.AttackGroup`, null)**. `TryGetAttackSource(e, out CombatAttackSource)` 하나 |
| Tb.2 | `Presentation/Attack/CombatHitFollowUps.cs`, `Playback/Ordering/CombatConcurrentGroups.cs` | TP-2. 맞는 순간 대응 `Begin`: 행동 시작에서 출처 목록을 비우고(옛 동작), 출처를 여는 사건이면 목록에 더함. 피해 후속은 근원 열쇠로 출처를 찾음, 이동 후속은 가장 최근 출처. 그룹 0인 출처는 열지 않음(옛 동작). 동시 묶음의 이동 열쇠(`TryGetKey:207-216`)도 같은 값으로 |
| Tb.3 | `CombatEventTypeTable.cs`, `DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs` | 출처를 여는 사건 = 발동에 `ChildAttack`이 있음. `IsReplayedFollowUp`·`HasTargetedFollowUp`을 `IsChildAttackStart(trait)` 하나로 바꿈. `TraitActivationVisual.IsFollowUpAttackKind`·`IsFollowUpAttack`과 "공격 표시는 추가 공격 줄에만" 검사·툴팁 문장을 걷음(Q-TS2 A — 어느 줄이든 덮어쓰기를 둘 수 있고, 공격을 안 내는 줄에서는 쓰이지 않을 뿐) |
| Tb.4 | `Presentation/Attack/CombatAttackShape.cs`, `CombatHitGate.cs` | `ForFollowUp` → `ForChildAttack(CombatEffectsEvent start, TraitActivationSnapshot trait)`: 공격자 = 발동 유닛, 방향 = `AttackDirection`, 타격 칸 = `TargetCell`, 영향 칸 = `TargetCells`, 주 대상 = `TargetId`(없으면 비움), 완료 = `FirstRelease`. 칸 차이 방향 계산(`DirectionOf`) 삭제. `CombatHitCompletion.AnyRelease`와 Gate 갈래 삭제(R8) |
| Tb.5 | `Playback/Handlers/CombatTraitEventHandler.cs`, `Playback/Ordering/CombatEventTypeTable.cs` 주석 | 발동마다: 줄이 있으면 큐. 추가 공격 시작이면 줄 유무와 상관없이 공격 시작(Q-TS1 A), 덮어쓰기는 줄에 있으면 그것. TP-6 계약 검사. `_facts.TryGetCell` 두 번 조회·"대상 없는 추가 공격" 경고·"둘째부터 건너뜀" 경고 삭제. 클래스 설명 고침 |

확인(Ub):
1. 컴파일.
2. 처치 연쇄: 처치 → 다음 대상에게 다시 치고, 피해 숫자·피격이 **맞는 순간**에 나온다(공격 동작이 다 끝날 때까지 기다리지 않음).
3. 연쇄 폭발·행운의 탄환도 같다.
4. 2연발: 대상이 있으면 그 쪽으로 두 번째 공격, 아무도 없으면 그 방향으로 쏘고 맞는 순간 없이 끝난다(P9).
5. 발동 연출 줄이 없는 추가 공격도 그 유닛의 일반 공격으로 그려지고, 모든 연출이 끝날 때까지 쓸데없이 멈추지 않는다(A4).
6. 추가 공격의 넉백(있는 특성이면)이 그 공격의 맞는 순간 뒤에 나온다.
7. 처치 연쇄 중 발동 유닛이 죽어 몸이 없으면 큐만 나오고 전투가 이어진다.

### TS-c 이동 원인 (TS5, R4)

| Task | 파일 | 할 일 |
|---|---|---|
| Tc.1 | `Presentation/Movement/CombatMovementKinds.cs` | TP-7 판정 `Of(CombatCharacterEvent step)`를 종류 성질(`IsSelfDriven`) 옆에 둔다. 판정 원본 = 이 함수 하나 |
| Tc.2 | `Playback/Support/CombatEnemyPresentationPacing.cs` | `BeforeStep`이 `CombatMovementKinds.Of`를 부름. `IsVoluntaryMovement`·`_movementActor`·`_attackStarted`와 `BeginResult`의 행위자 고르기 삭제(간격의 공격 갈래는 `_attackStarted` 없이 그대로 간격만 냄) |
| Tc.3 | `Playback/Support/CombatMovementPresentation.cs`, `Presentation/Movement/CombatMovementKind.cs` 주석 | `KindOf`가 `CombatMovementKinds.Of`를 부름. "판정 원본은 간격의 자발 이동 판정" 주석들을 고침 |

조건(Q-TS4): 밀림 걸음을 내는 Core 자리가 모두 원인을 채운다(§3.1). 그래서 Tc.2에서 추론 규칙을 걷는다.

확인(Uc):
1. 컴파일.
2. 아군·적 보통 이동은 뛰어 이동(Hop)이고, 적 첫 걸음 앞 행동 간격이 그대로 있다.
3. 넉백·충격 볼트·공격 강제 이동은 걷기(밀림) 모양이고 앞 간격이 없다.
4. 이동 뒤 공격하는 적: 이동 → 공격 → 넉백 순서가 지금과 같다.
5. 토네이도 재배치·기차·끌어오기는 지금과 같다(걸음 경로가 아님).

### TS-d HUD 새 상태·거절 안내 (TS6, R5)

| Task | 파일 | 할 일 |
|---|---|---|
| Td.1 | `DataBaseSystem/Data/View/Table/UnitStatus/Item/UnitStatusKind.cs` | 값 다섯 더함: `Disarm = 4`·`AnkleBreaker = 5`·`ArmorBreak = 6`·`AttackBonus = 7`·`Shield = 8`(있는 값 숫자 그대로) |
| Td.2 | `Display/Unit/CombatUnitStatuses.cs` | 줄 다섯(설계 TS6 표), TP-8 정수 줄 꼴 `OfAmount`. 머리 주석의 "지금 Core가 주는 걸린 상태는 디버프 셋뿐" 고침 |
| Td.3 | `DataBaseSO/ViewGroup/UnitStatusVisual/Item/` 다섯(+ `.meta`), 표 에셋 등록 | 이름·구분·설명 틀 초안(아이콘 비움, 지금 셋도 비어 있음). 틀 값 수: 무장 해제·발목 꺾기 {0} = 남은 턴 끝 수, 방어구 파괴 {0} = 겹친 수, 공격력 부여 {0}, 보호막 {0}. **코드와 한 커밋**(전투 진입 때 줄마다 외형을 확인해서, 없으면 멈춤) |
| Td.4 | `Input/Assist/CombatActionNotice.cs`(현강님 줄), `Display/Hud/CombatActionStateText.cs`(우리) | TP-9 두 줄. 같은 사유가 Core 가용성(`CombatActionAvailability.AttackFailure`·`MoveFailure`)으로도 오고, 지금 행동 상태 짧은 글자는 그 사유를 몰라 "사용불가"로 보인다 → 짧은 글자 둘("무장해제"·"발목꺾임")도 더한다 |

확인(Ud):
1. 컴파일, 전투 진입(외형 다섯 확인 통과).
2. 무장 해제·발목 꺾기: 걸리면 아이콘 칸·호버 설명에 남은 턴, 풀리면 사라짐.
3. 방어구 파괴: 겹칠 때마다 수가 오름.
4. 공격력 부여(선봉·청소·사기 진작·가속)·보호막(단독 행동·결속): 버프 칸에 합이 보이고 0이 되면 사라짐.
5. 무장 해제 유닛으로 공격, 발목 꺾인 유닛으로 이동 → 안내 문구.

### TS-e 흔적 지형 (TS7, R9)

| Task | 파일 | 할 일 |
|---|---|---|
| Te.1 | `DataBaseSystem/Data/View/Table/Trait/Item/TraitVisualDataSO.cs`·`TraitVisualData` | 흔적 칸 둘: `trailPrefab`(선택)·`trailLocalOffset`(Vector3). 읽기 `TryGetTrail` |
| Te.2 | 새 `Display/Map/FieldTrailPresenter.cs`(+ `.meta`) | 원본 ID마다 인스턴스(지형 표시의 풀 꼴). `Show(TraitTrailFact, mapper)`: 켜짐·플레이어 공개면 만들거나 옮기고, 아니면 지움. `Render(environment, mapper)`: 결과 끝에 Buffer 환경 원본 중 흔적으로 다시 맞춤(없는 ID는 지움). 외형은 흔적의 특성 키(`TraitTrailFact.Key`)로 특성 외형 표에서. TP-10 자리 |
| Te.3 | `Playback/CombatFieldEventPorts.cs`, `CombatFactEventHandler.cs`, `Playback/CombatSceneEventPresenter.cs`(넘김), 천명님 `Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs:442`(인자 하나) | 창구 끝 칸 `showTrail`. 사실 처리기 갈래: `CombatMovementSourceEvent`의 `Final`이 흔적이면 `showTrail`. 시험 줄은 컴파일 맞춤만 |
| Te.4 | `Display/CombatSceneDisplay.cs` | 생성 인자·결과 끝 `Render` 한 줄·해제 목록 |
| Te.5 | `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(민형님 연결 줄) | 흔적 표시 생성·창구 칸·표시 인자 |
| Te.6 | 새 프리팹 둘·재질 둘(+ `.meta`), `TraitVisual_GroundCompaction`·`_DustScreen` 에셋 | TP-11 임시 아트 연결. **Te.1과 한 커밋** |

확인(Ue):
1. 컴파일.
2. 땅 다지기·먼지 연막 유닛이 이동하면 시작 칸·지나간 칸에 흔적이 생긴다(이동 도중 그 걸음 뒤).
3. 상대 턴이 끝날 때마다 줄고, 다 되면 사라진다. 자기 턴 끝에는 그대로.
4. 같은 칸에 흔적 둘·고지대·타일 위에서 바닥 지형을 가리지 않는다.
5. 흔적을 만든 유닛이 죽어도 흔적은 남는다.
6. 보이지 않는 칸(공개 대상 밖)의 흔적은 나오지 않는다.

### TS-f 유닛 행동 (유닛 행동 설계 AA1~AA9, 대신 맞기 포함)

- Tf.0(상세 설계)은 끝났다: [유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) — 2026-10-30 사용자 "모두 A로"(Q-AA1~Q-AA4·Q-AA6·Q-AA7 A), Q-AA5 "대신 맞는 경우에는 밀리지 않음"(Core 요청 §53). 옛 Tf.0 ①~⑦은 그 설계가 답한다.
- 이 묶음에서 정한 것은 §1.1 TP-13~TP-25, 설계와 다른 것은 §1.2 끝 줄.

#### TS-f1 행동 값 (AA1)

| Task | 파일 | 할 일 |
|---|---|---|
| Tf1.1 | 새 `DataBaseSystem/Data/View/CombatPresentation/CombatUnitActionActor.cs`·`CombatUnitActionPlace.cs`·`CombatUnitActionFace.cs`(+ `.meta`) | 열거 셋: 행동 유닛 `None = 0`·`Source = 1`·`Targets = 2` / 자리 `Stay = 0`·`GuardFront = 1`·`TowardTarget = 2` / 보기 `Keep = 0`·`Target = 1`·`Attacker = 2` |
| Tf1.2 | 새 `…/CombatUnitActionInput.cs`·`CombatUnitActionData.cs`(+ `.meta`) | 직렬화 입력(칸 여덟, 툴팁)과 불변 값(읽을 때마다 새로 만듦, 큐 입력 `CombatVisualCueInput` 꼴). 값 검사는 값 생성자 하나: 거리 [0, 1.5], 시간 셋 ≥ 0·유한, 열거 정의, 행동 유닛 `None`이면 나머지는 안 봄(P10) |
| Tf1.3 | `DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs` | 칸 `unitAction`(기본 `None`)·읽기 `UnitAction`(행동 없으면 null)·`Validate`가 값을 한 번 만들어 검사. 툴팁·문서에 "세 번째 형태" |
| Tf1.4 | `DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_Sacrifice.asset` | 발동 줄 하나: `kind: 110`(`DamageRedirected`), 큐 빈 값, `unitAction` = P12 임시 값(TP-24). **Tf1.1~Tf1.3과 한 커밋** |

확인(Uf1): 컴파일, DB 생성 성공(부트), 화면 변화 없음(아직 읽는 곳 없음), 희생 외형 인스펙터에 행동 칸이 보임.

#### TS-f2 서는 자리 (AA2·AA3·AA8 안전장치)

| Task | 파일 | 할 일 |
|---|---|---|
| Tf2.1 | 새 `Display/Unit/ICombatUnitStand.cs`(+ `.meta`) | 대여 계약: `IsAvailable`·`Home`(빌릴 때 자리)·`Position`·`Rotation`·`CanMove`(자세 대여가 없음)·`ApplyStand(위치, 회전)`·`RestoreStand()`·`IsFolded` |
| Tf2.2 | `Display/Unit/FieldUnitView.cs`·`ICombatUnitPresentationTarget.cs` | 대여 칸 하나(`UnitPresentationLeaseSlot<…>`, 자세 칸과 따로), `BeginStand`·`HasStand`, 꺼질 때·풀 반환 때 다른 칸처럼 취소. 되돌리기 = 빌릴 때 자리, 표시가 꺼졌거나 사망 붙잡기면 쓰지 않음(TP-17) |
| Tf2.3 | `Display/Unit/CombatUnitPresentationWindow.cs` | `TryBeginStand`(켜진 표시만), `TryGetStandPose(id)`(빌린 동안 지금 자리), `KeepsPlacement`에 서는 자리 한 줄(TP-18), `BeginPlacement`가 서는 자리를 보면 접고 경고 한 번(TP-19, AA8) |
| Tf2.4 | `Presentation/Pieces/CombatComposition.cs`·`CombatPieceRunLeases.cs` | `CombatPieceChannel.Stand = 5`, 대여 한 칸(`TryBorrowOne`)·꺼내기 `Stand(unit)` |
| Tf2.5 | `Display/Unit/FieldUnitPresenter.cs`·`Playback/Support/CombatShownFacts.cs` | 몸 상자를 주어진 자세에 놓는 함수 하나(`BodyAt`, 상자 배치 식은 지금 것을 같이 씀, TP-20). `TryGetBody(id)`·`StandingBodyOf(id)`가 창구의 서는 자리를 먼저 봄. 사실로 묻는 `StandingPoseOf(facts)`는 그대로 |

확인(Uf2): 컴파일, 화면 변화 없음(서는 자리를 빌리는 곳 없음).

#### TS-f3 행동 조립 (AA4·AA5)

| Task | 파일 | 할 일 |
|---|---|---|
| Tf3.1 | 새 `Presentation/Pieces/CombatUnitActionPlaces.cs`(+ `.meta`) | 순수 계산 한 곳: `Stay`·`GuardFront`(지켜진 몸 가운데 → 공격자 몸 쪽, 높이 = 지켜진 칸 표면, P2)·`TowardTarget`, 보기 방향. 칸 크기는 Mapper에서 |
| Tf3.2 | 새 `Presentation/Pieces/CombatStandMovePiece.cs`(+ `.meta`) | 서는 자리 대여로 지금 자리 → 끝 자리 보간(뛰기는 `CombatStepHeight.Arc`, 높이 = 걸음 모양 설정의 뛰기 높이, TP-21). `CanMove`가 거짓이면 그 프레임은 기다림. 접혔으면(`IsFolded`) 바로 끝 |
| Tf3.3 | 새 `Presentation/Pieces/CombatUnitSettleWaitPiece.cs`(+ `.meta`) | 돌아오기 앞 대기: 끝 조건은 TP-13. 판정은 `Tick`에서만(시작 순간에 끝나지 않음) |
| Tf3.4 | 새 `Playback/Ordering/ICombatSourceSettlement.cs`(+ `.meta`), `OrderedCombatPresentation.cs` | 좁은 질문 하나 "이 출처의 맞는 순간 뒤 후속 사건 중 그 유닛이 주체인 것이 모두 끝났나"(후속 목록 = 맞는 순간 대응 `CombatHitFollowUps`, 완료 = 순차 재생기 상태). 순차 재생기가 구현하고 Presenter가 넘김(TP-13) |
| Tf3.5 | 새 `Presentation/Pieces/CombatUnitActionComposer.cs`(+ `.meta`) | 입구 둘: `AddGuards`(공격 조립에 수호자 나가기 항목들을 넣고 공격 뼈대 시작 시점을 돌려줌) · `AddReturns`(뼈대의 Hit 뒤 대기 → 돌아오기) / `ComposeMoment`(유닛 범위 조립: 나가기·돌기 → 지연 `holdSeconds` → 돌아오기). 반응 줄 큐는 행동 시작(P6) |

확인(Uf3): 컴파일.

#### TS-f4 공격 반응 행동 — 희생 (AA6)

| Task | 파일 | 할 일 |
|---|---|---|
| Tf4.1 | `Presentation/Attack/CombatAttackShape.cs` | 반응 발동 사실 목록 `Reactions`(`Of(attack)` = `AttackResult.TraitActivations`의 가로채기, `ForChildAttack` = `ChildAttack.Reactions`), 파생 `ContactTargetId`(주 대상이 가로채졌으면 그 수호자, 아니면 주 대상, TP-25) |
| Tf4.2 | `Presentation/Attack/CombatAttackPlayback.cs` | 직선 접촉 진행률이 `ContactTargetId` 몸(옮긴 자리)으로(P5) |
| Tf4.3 | `Presentation/Pieces/CombatCueAnchors.cs` | 시작(Start) 기준을 다른 항목 신호로 옮길 수 있게(기본은 지금처럼 실행 시작, TP-14) |
| Tf4.4 | `Presentation/Pieces/CombatCodeMotionAttack.cs`·`CombatClipAttack.cs` | 끝 선택 인자 "앞 항목"(`CombatAttackLead`, 조립을 만든 직후 부름): 뼈대 항목과 큐 Start를 그 시점 뒤로, 뼈대의 Hit 항목 번호를 돌려받아 돌아오기를 붙임(TP-14·TP-15). 인자가 없으면 지금과 같음 |
| Tf4.5 | `Playback/Support/CombatAttackStart.cs`·`Playback/CombatSceneEventPresenter.cs` | 가로채기마다 희생 줄(`TryGetActivation`)의 행동을 찾아 앞 항목을 만듦, 수호자 서는 자리를 못 빌리면 그 항목만 뺌(P7), `GuardFront` 아닌 자리·가로채기 없는 `GuardFront`는 경고 한 번(P10). 생성 인자: 큐 값 답·걸음 모양 설정·후속 정리 질문(TP-21·TP-22, Factory 무변경) |
| Tf4.6 | `Playback/Handlers/CombatRoarEventHandler.cs` | 포효 결과에 가로채기가 있으면 경고 한 번(P9, 조립 무변경) |

확인(Uf4):
1. 컴파일.
2. 적 턴, 희생 수호자 2칸 안 아군이 치명타를 받을 때: 수호자가 지켜진 유닛 앞으로 뛰어 나감 → 적 공격 → 수호자가 맞음 → 피격 반응이 끝나면 돌아옴. 다음 적 행동은 돌아온 뒤 시작.
3. 직선 투사체(저격 등)가 수호자 몸에서 맞음(지켜진 유닛까지 날아가지 않음). 근접·곡사도 수호자가 맞는 모습.
4. 수호자가 죽으면 그 자리에서 쓰러지고 돌아오지 않음.
5. 수호자 둘이 한 공격을 막으면 둘 다 나갔다가 돌아옴.
6. Gravekeeper(넉백) 공격: 수호자는 밀리지 않음. 지켜진 유닛이 밀리는 것은 Core 요청 §53 전까지 그대로.
7. 희생이 없는 공격·아군 턴은 지금과 같음.

#### TS-f5 발동 사건 행동 (AA7)

| Task | 파일 | 할 일 |
|---|---|---|
| Tf5.1 | `Playback/Handlers/CombatTraitEventHandler.cs` | 발동 줄에 행동이 있고 추가 공격·가로채기가 아니면 행동 유닛마다 `ComposeMoment` 실행을 염(완료 콜백 빈 함수, 사건은 바로 완료, TP-23·P11). 못 빌리면 건너뜀 + 경고 한 번(Q-AA6 A). 추가 공격이면서 행동 값이 있으면 경고 한 번 뒤 행동 건너뜀(P9) |

확인(Uf5): 컴파일. 사용자가 확인용으로 한 특성 줄(예: 선봉장 `AttackGranted`)에 `Source`·`Stay`·`Target`·머묾 0.3초를 임시로 넣고 → 발동 때 그 유닛이 돌아봤다가 원래 방향으로 돌아오는지 → 줄을 되돌림.

### TS-g 문서·공유·묶음 끝 검토

| Task | 할 일 |
|---|---|
| Tg.1 | 폴더 배치 안내(`CombatAttackSource`·`FieldTrailPresenter` 자리, §2 규칙 줄: "추가 공격은 `ChildAttack`이 판정", "이동 종류는 걸음 원인이 판정"), 설계 §9 구현 메모 |
| Tg.2 | 천명님(Core 전달 문서 §5·새 공유): 표식 줄(`CombatFactEventHandler:50-51`) 변경, `PullAndMarkPresentationTests:442` 인자 하나, **요청 `CharacterDebuffState.WithHuntersMark`**, 병합 때 `CombatMovementCalculator` 충돌 합친 내용(§5.59 덧붙임) |
| Tg.3 | 현강님: `CombatActionNotice` 두 줄 |
| Tg.4 | 호진님(View DB 표): 상태 외형 다섯, 특성 외형 흔적 칸, 발동 줄 공격 표시 덮어쓰기 제한 풀림, §4 에셋 작업 목록 |
| Tg.5 | 민형님 Factory 공유: 흔적 표시 연결 줄 |
| Tg.6 | 묶음 끝 검토(새 에이전트 하나, 7기준) → 결과를 사용자에게 보이고 반영 결정 |

### TS-h 위치 교환 승객 이동 (2026-10-31 추가, 착수 전)

> **대신함(2026-10-31):** 이 절은 하지 않는다. 사용자 결정(이펙트 구조 2판 Q-EL4 = 위치 이동 유닛 행동, 차례로)으로 [이펙트 구조 계획](2026-10-31-combat-effect-structure-plan.md) EL-f·설계 EL11이 대신한다. 아래 "지금 동작"의 사실 기록만 그쪽에서 씀.

- 요청: 사용자 "위치 교환은 View에서 두 유닛 위치를 서로 바꿔 보여야 하는데 지금 안 되어 있다" → 답: 승객이 순간 이동함 → 사용자 "A로 잡고 계획에 추가"(A = 승객도 이동 레인으로 움직임, 유닛 행동으로 만들지 않음).
- **지금 동작(코드 확인):**
  - Core(`MovementProjection.ExchangeEndpoint`)는 사실 셋을 연달아 냅니다.
    1. 이동자 걸음: 끝점으로, `Voluntary`, 원인 특성 `PositionSwap`
    2. 승객 걸음: 끝점 → 이동자의 출발 칸(`Input.Path.Origin`), `Forced`, `PositionSwap`. 경로가 길면 여러 칸 떨어진 곳입니다.
    3. 발동 `MovementChanged`: 대상 = 두 유닛, 칸 = 두 도착 칸
  - View는 이 둘을 쌍으로 묶습니다(`CombatPlacementBatchPlan`, 사용자 커밋 `107767b38`).
    - 이동자 걸음은 보통대로 레인에서 재생합니다. 그동안 승객이 끝점에 그대로 있어 둘이 겹쳐 보입니다.
    - 이동자가 도착하는 순간 `CombatShownFacts.DeferPlacementPair`로 승객 사실을 함께 반영합니다. 승객은 **순간 이동**하고, 승객 걸음 사건은 레인 없이 바로 완료합니다.
- **A 요지:** 이동자의 교환 걸음이 레인에서 시작되는 순간, 승객 걸음을 승객 레인에서 함께 시작합니다. 둘은 같은 시간에 서로의 자리로 가서 같이 도착합니다.
  - 실제 위치가 바뀌는 이동이라 지금 걸음·넉백·끌어오기와 같은 이동 레인이 맡습니다. 사실 반영은 레인 도착에서 하고, 취소 규칙은 레인 것을 따릅니다.
  - 순간 이펙트·글자는 지금처럼 발동 줄 큐가 냅니다.
- 새 Core 요청·Core 변경 없음. Factory 무변경.

#### 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 까닭 |
|---|---|---|
| TP-31 | 승객 걸음은 새 이동 종류 **`CombatMovementKind.Swap`**으로 봅니다. 판정은 `CombatMovementKinds.Of` 한 곳입니다: 원인이 `Forced`이고 특성이 `PositionSwap`이면 `Swap`. | 밀림 모양(늘 걷기)으로 여러 칸을 미끄러지면 다른 유닛·벽을 뚫고 지나가 보입니다. 종류를 따로 두면 모양만 바꾸고 레인·접촉·표시 규칙은 그대로입니다(걸음 모양 설계 ST6의 "새 종류 = 열거 하나 + 성질 한 줄 + 모양 하나 + 표 한 줄"). |
| TP-32 | `Swap` 모양 = **뛰기 호 한 번**입니다. 높이는 설정의 뛰기 높이(`hopHeight`)이고, **시간은 이동자 걸음 모양의 한 걸음 시간**(뛰기면 `hopSeconds`, 걷기면 `stepSeconds`)이라 둘이 같이 도착합니다. | 둘이 서로를 지나가는 가운데 지점에서 겹치는데, 승객이 넘어 뛰면 덜 어색합니다. 거리가 멀어도 한 번의 호입니다. |
| TP-33 | `IsSelfDriven(Swap) = false`입니다. 걸음 전 회전·걸음 접촉 Gate·재생 강조 지우기·경로 끝 방향 복귀를 하지 않습니다. 승객은 보던 방향 그대로 뜁니다. | 승객은 스스로 걷는 것이 아닙니다. Core도 동승자의 자발 이동 효과를 실행하지 않습니다. |
| TP-34 | 승객 레인은 `CombatMovementPresentation.BeginStep`이 쌍의 첫 걸음(이동자)을 시작할 때 함께 엽니다. 승객 걸음 사건은 지금처럼 바로 완료합니다(`IsPartner`). | 사건 순서대로 두면 자발 걸음이 다른 유닛 이동을 걸음 끝까지 막아(동시 처리 결정) 동시에 움직일 수 없습니다. |
| TP-35 | 승객 레인의 완료 통지는 비워 둡니다. 이동자 걸음 완료가 사건 완료입니다. 시간이 같아 둘이 같이 끝납니다. 이동자 걸음 모양이 걷기여도 TP-32로 맞춥니다. | 승객 걸음 사건은 이미 완료됐습니다. |
| TP-36 | `CombatShownFacts`의 도착 배치 예약(`DeferPlacementPair`·`ForgetPlacementPairs`·`_arrivingPlacements`)은 이 길에서 쓰지 않게 됩니다. 다른 쓰는 곳이 없으면 걷어 냅니다. 되돌리기 쌍(`Returned` 둘)은 지금처럼 바로 함께 반영합니다(되돌리기는 늘 즉시). | 승객 사실은 승객 레인 도착에서 반영하므로 예약이 필요 없습니다. |

#### Task

| Task | 할 일 |
|---|---|
| Th.1 | `Presentation/Movement/CombatMovementKind.cs`에 `Swap = 2`(문서 주석), `CombatMovementKinds.Of`에 TP-31 판정, `IsSelfDriven(Swap) = false`. |
| Th.2 | `CombatStepStyles`: 생성자에 swap 모양 인자, `From`이 TP-32대로 `CombatHopStepStyle(이동자 한 걸음 시간, hopHeight)`를 만듦. 표 검사가 빠진 종류를 잡으므로 표 한 줄을 더함. 뛰기 모양이 여러 칸 거리도 호 하나로 그리는지 확인(`CombatHopStepStyle`). |
| Th.3 | `CombatMovementPresentation.BeginStep`: `TryBegin(pair)`이면 `_motion.BeginStep(pair.Partner, Swap, true, mapper, 빈 완료)`로 승객 레인을 열고, `DeferPlacementPair` 호출을 지움. 승객 레인이 쉬고 있지 않으면(이전 걸음 진행 중) 경고 뒤 지금처럼 이동자 도착 때 바로 반영(안전장치). |
| Th.4 | `CombatShownFacts`: TP-36 정리. 승객 레인 도착이 `ShowCharacter(승객 끝 사실)`를 부르는지 확인. |
| Th.5 | `Swap`을 읽는 다른 곳 점검: 적 행동 간격(`CombatEnemyPresentationPacing` — `IsSelfDriven`이 거짓이라 간격 없음), 걸음 접촉(`CombatStepContacts` — 승객 도착 칸은 이동자가 비운 출발 칸이라 접촉 없음), 이동 효과음(`NotifyMovementStarted` — 승객은 부르지 않음). |
| Th.6 | 문서: 폴더 배치 안내 `Movement/` 줄, 걸음 모양 설계 한 줄(새 종류), 이 절 실행 기록. |

확인(Uh, 사용자 Unity):
1. 컴파일
2. 위치 교환 유닛이 아군 칸으로 이동을 끝내면, 이동자와 승객이 동시에 서로의 자리로 가고 같이 도착하는지. 승객은 한 번 뛰어 넘어야 합니다.
3. 경로가 여러 칸일 때 승객이 출발 칸까지 호 하나로 가는지
4. 걷기 설정(`voluntaryStepLook = Walk`)에서도 둘이 같이 도착하는지
5. 교환 직후 되돌리기를 하면 둘이 바로 원래 자리로 돌아오는지(지금과 같음)
6. 교환 뒤 다음 행동이 승객이 착지한 다음에 시작하는지

커밋: 새 파일 없음. Th.1~Th.4는 한 커밋.

## 3. 계획을 쓰며 확인한 Core 사실

경로 약칭 `C/` = `RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`, `B/` = `RuntimeSystem/BufferSystems/`. 읽기만 했다(조사 에이전트 하나 + 직접 대조).

### 3.1 이동 원인 (Q-TS4 조건 → 충족)

**밀림 걸음인데 원인이 빈 Moved·Relocated 사건은 없다.** 그래서 Q-TS4 A(원인 없음 = 스스로)가 성립한다.

| 이동 | 만드는 자리 | 원인 |
|---|---|---|
| 보통 스스로 이동(아군·적 AI), 도약(Leap)의 칸마다 진입 | `C/Application/Combat/MovementProjection.cs:148`(`CombatMovementCalculator.cs:141-149`) | **null** (`Voluntary` 값은 Core 어디서도 만들지 않음, 시험만 씀) |
| 넉백 | `AttackForcedMovement.cs:55-56`(원인은 `AttackKnockbackCalculator.cs:55-56·120-121`) | Forced + Knockback |
| 충격 볼트 | 같은 자리(ImpactCenter, `AttackKnockbackCalculator.cs:66·95-98`) | Forced + ImpactBolt |
| 끌어오기 | `PullAttackCalculator.cs:84-91` | **Moved 사건 없음**(`publishMovedEvents:false`). 원인은 `CombatPullEvent`의 `Steps[i].Cause` = Forced + Pull |
| 토네이도 | `MovementProjection.cs:393-398`(`TornadoCalculator.cs:66`) | **Relocated**, Forced + null(생성자가 Relocated에 Forced·키 없음만 받음, `CombatEvent.cs:136-138`) |
| 타일 규칙 강제 이동 | `MovementEnvironmentCalculator.cs:192` | Moved, Forced + null(지금 이 요청을 만드는 규칙은 없음) |
| 구조물 넉백 | `AttackKnockbackCalculator.cs:204-205` | `CombatStructureEvent`, Forced + 키(View는 사실만 반영, 걸음 레인 아님) |
| 배치·재배치·회수 | `CombatPlacementCalculator`·`CombatTurnCalculator` | null(Placed·Returned는 원인을 받지 않음) |

### 3.2 발동 사건 모양 (TP-3·TP-4)

- `CombatCommandEvents.Trait`은 사건 하나에 발동 하나, `DamageTraits`는 **한 수신자**의 기여 여럿을 사건 하나에 넣는다(`CombatCommandEvents.cs:22-38`). 그래서 피해 변화·방어 무시 사건은 발동이 여럿이어도 대상이 모두 같다(TP-4 ②로 주체 = 그 수신자). 희생으로 수신자가 바뀌면 대상은 수호자다.
- 근원(`ResultOrigin`): 피해 변화·방어 무시·위압·무장 해제·발목 꺾기·표식 적용·보호막 흡수는 채움. 고정 사격 상태 적용은 null.
- 대상 여럿: 넉백 적용(묶음), 사기 진작·선봉의 공격력 부여(`TraitKillSettlement.cs:51-52`, `TraitAttackNotifications.cs:46-47`).
- 순서(사건 목록 안):
  - 공격 사건이 피해 발동·체력 사건보다 앞이다(`CombatAttackCalculator.cs:143·175`). 끌어오기 발동·공격 시작 때 공격력 부여는 공격 사건보다 앞이다.
  - 피해·방어 무시 발동은 체력 사건보다 앞이다. 순서 민감 특성이 없으면 **모든 타격의 발동이 먼저 나오고 체력 사건이 뒤에 몰린다**(`AttackDirectDamageCalculator.cs:36-42·92·130·151`). 지금 View는 이 발동들을 장벽으로 두어 여러 대상 피격이 차례 재생으로 끊긴다(설계 A5 확인).
  - 보호막 흡수 → 상태 사건 → 체력(`CombatDamageApplication.cs:106·110·119`).
  - 체력 뒤: 표식·제한 상태 적용, 방어구 파괴, 넉백 발동, 위압.
- 상태 사건(`CombatTraitStateEvent`, 만드는 곳 `CombatCommandEvents.cs:40-45` 하나)의 근원은 피해·처치 길에서 채우고, 턴 끝 만료·정리·이동·공격 알림 길에서는 비운다(장벽).
- 추가 공격 시작은 발동 하나짜리 사건이고(`AttackResultFactory.AttachChild`의 `Traits.Single()`), 방향·지정 칸·근원을 늘 채운다(`FollowUpAttackExecutor.cs:108-112·174-177`). 대상 지정 추가 공격의 대상은 원래 대상이고 실제 수신자는 `ChildAttack.Hits`에 있다.

### 3.3 흔적 원본과 Buffer (Te.2)

- 만들기: `TraitTrailCalculator.cs:56-61`이 `CombatEffectsEvent(TrailCreated, 대상 칸 = [흔적 칸], 근원 없음)` 다음 `CombatMovementSourceEvent(null, 흔적)`. 출발 칸(첫 걸음 앞)과 들어간 칸마다(그 Moved 바로 뒤), 스스로 이동할 때만.
- 바꾸기·지우기: `:63-75`가 TrailChanged·TrailRemoved(크기 = 남은 턴) 다음 `(이전, 이후)`. 만든 쪽이 아닌 진영 턴 끝마다 1 줄고, 승패가 나면 0. **지운 흔적은 목록에서 빠지지 않고 `Active = false`로 남는다**(`B/Combat/CombatStateBuffer.cs:466-483`).
- Buffer: `CombatStateData.Environment`(명령마다 최종 상태)의 같은 원본 목록에 실려 온다. 지형·오브젝트 분류(`EnvironmentBindings`)에는 들지 않는다. ID는 오브젝트와 같은 할당기다.
- 공개 대상은 늘 양쪽이다(`GroundCompactionTraitRule.cs:16`, `DustScreenTraitRule.cs:40`). 그래도 Te.2는 공개 판정 함수(`CombatEnvironmentFacts.IsKnownToPlayer`)를 그대로 거친다(미리보기 투영 규칙과 같은 판정 한 곳).
- 흔적의 특성 키는 `TraitTrailFact.Key`(= 출처 정의 키)다.
- 지금 View는 흔적 사건에 깨지지 않는다: 걸음 경로는 중간 사건을 건너뛰어 이어 보고(`OrderedCombatPresentation.IsLastMovementStep`), 걸음 접촉은 `TriggersArrivalRule = false`라 흔적을 접촉 원본으로 보지 않는다.

### 3.4 표식·가로채기

- `CharacterDebuffState`에 `WithHuntersMark`는 없다(`WithRestrictions`·`WithArmorBreak` 둘뿐). Core도 표식은 생성자로 바꾼다(`HuntersMarkApplication.cs:67-68`).
- 표식 사건(`CombatHuntersMarkEvent`)은 `TargetId`·`Expected`·`Final`(표식 상태, null = 소모)이다. 사망·전투 끝 정리로 표식이 지워질 때는 이 사건 없이 발동(StatusRemoved)과 **상태 사건**만 나온다 → Ta.1·Ta.2가 받는다.
- 가로채기는 희생 하나가 만든다(`SacrificeDamagePlanner.cs:78-80`). 루트 공격·포효·추가 공격(`ChildAttack.Reactions`) 모두에 들어간다. **한 공격에 수호자가 여럿일 수 있고**, 같은 수호자가 여러 타격을 막을 수도 있다. `DeliveryIndex`는 그 공격 안 전달 순번이다(TS-f 상세 설계 입력).

## 4. 에셋 작업 목록 (설계 P6, 코드 밖)

> 2026-10-30: 특성 37개(+ 꺼 둔 5개)의 하는 일·발동 시기·발동 종류·남는 사실·View 길을 [전투 특성과 발동 시기](../viewDocs/combat-trait-activation-timing.md) 한 문서로 정리했다. 아래 표는 그 앞의 간단본이다.
> 정리 중 찾음: 가속의 쌓인 공격력은 `TraitEffects.AttackGrants`가 아니라 가속 보유 상태에 있어 Td.2 "공격력 부여" 줄에 안 들어간다. TS-d 착수 때 보유 상태 줄을 더할지 정한다.

새 특성 22종의 발동 큐 줄은 특성 외형 에셋에 넣는다(사용자·기획). 어떤 특성이 어떤 발동 종류를 내는지의 원본은 Core 인계 §7 표다. 아래는 그 표를 옮긴 작업용 목록이다(값·이펙트는 정하지 않음).

| 특성 | 내는 발동 종류 | 큐 후보 자리 |
|---|---|---|
| 선봉(Vanguard)·청소(CleanSweep)·사기 진작(MoraleBoost) | AttackGranted / StatusConsumed / StatusExpired·StatusRemoved | 받는 유닛 몸(대상 여럿) |
| 피의 부름(CallOfBlood) | HealthRecovered | 발동 유닛 몸 |
| 무장 해제(Disarm)·발목 꺾기(AnkleBreaker) | StatusApplied / StatusConsumed / StatusExpired·StatusRemoved | 대상 몸 |
| 단독 행동(SoloAction) | ShieldGranted·Reset·Absorbed·Removed | 발동 유닛 몸 |
| 결속(Cohesion) | 보호막 넷 + DefenseChanged | 발동 유닛 몸 |
| 참호(Entrenchment) | DamageModified | 대상(맞는 쪽) 몸 |
| 희생(Sacrifice) | DamageRedirected(공격 결과 안, TS-f) | 수호자 몸 |
| 도발(Taunt) | TauntSelected | 발동 유닛 몸 |
| 굳은 의지(SteadfastWill) | KnockbackPrevented | 발동 유닛 몸 |
| 땅 다지기(GroundCompaction) | TrailCreated·Changed·Removed, RangeChanged | 칸 + 흔적 외형(TS-e) |
| 먼지 연막(DustScreen) | 흔적 셋, DamageModified | 칸 + 흔적 외형(TS-e) |
| 도약(Leap) | MovementChanged | 칸(넘은 칸) |
| 가속(Acceleration) | AttackGranted / StatusConsumed | 발동 유닛 몸 |
| 고정 사격(StationaryFire) | StatusApplied·StatusReset / DamageModified | 발동 유닛·대상 몸 |
| 방어구 파괴자(ArmorBreaker) | ArmorBroken / StatusRemoved | 대상 몸 |
| 마무리 사격(FinishingShot) | InstantDefeat | 대상 몸 |
| 추적 사격(TrackingShot)·집중 포격(FocusedBombardment) | DamageModified | 대상 몸 |
| 사냥꾼(Hunter) | HunterTargetSelected / AttackGranted / StatusReset·StatusRemoved | 표적 몸(사냥 표적 표시는 다음 묶음) |

이미 있는 특성(넉백·포효·위압·충격 볼트·끌어오기·표식·연쇄 폭발·행운의 탄환·2연발·처치 연쇄·정밀 사격·멸시·도어 브리칭·관통 감쇠·거리 비례)도 새 발동 종류(KnockbackApplied·RoarTriggered·StatusApplied·PullApplied·DefenseBypassed·DamageModified 등)를 내므로, 큐를 넣으려면 같은 방식으로 줄을 더한다. 꺼 둔 특성(집중·이동 후 공격 할인·가시밭·강령술·재생)은 발동이 나오지 않는다.

## 5. 다른 사람 줄과 공유

| 파일·줄 | 작성자 | Task | 공유 |
|---|---|---|---|
| `CombatFactEventHandler.cs:50-51` 표식 | 천명님(`4bd6b574`) | Ta.2 | Tg.2 |
| `PullAndMarkPresentationTests.cs:442` | 천명님 | Te.3(컴파일 맞춤) | Tg.2(Core 전달 문서 §5) |
| `CombatActionNotice.cs` 사유 표 | 현강님(`80eb7481`) | Td.4 | Tg.3 |
| `CombatSceneDisplayFactory.cs` 표시 조립 | 민형님 영역(연결 줄) | Te.5 | Tg.5 |
| `TraitVisual_Sacrifice.asset` 발동 줄 | 호진님(`6b0755be2` 확인용 에셋) | Tf1.4 | Tg.4(행동 칸·희생 줄) |
| `FieldUnitView.cs` 대여 칸 | 우리 + 다른 작업자 커밋 섞임 | Tf2.2 | 바꾸는 줄을 구현 때 blame으로 확인, 남의 줄이면 Tg에 한 줄 |
| 지켜진 유닛 넉백(Core 규칙) | 천명님 | — | Core 전달 문서 §53 요청 R-AA1(사용자가 나중에 전달) |
| 그 밖 | 우리(용근님) | — | — |

## 6. 커밋 묶음 (사용자)

- TS-a: 형식 표·사실 처리기·후속 대응·특성 처리기(한 커밋).
- TS-b: 새 `CombatAttackSource.cs`(+ `.meta`)와 바뀐 파일 전부(한 커밋, 새 값이 없으면 컴파일 안 됨).
- TS-c: 이동 판정 셋(한 커밋).
- TS-d: 열거·줄 표 코드와 상태 외형 에셋 다섯·표 에셋(한 커밋), 안내 두 줄(같이 또는 따로).
- TS-e: 흔적 칸 코드·새 표시·창구·Factory·시험 한 줄과 프리팹·재질·특성 외형 에셋(한 커밋).
- TS-f1: 행동 값 코드(새 `.cs` 다섯 + `.meta`, 발동 줄)와 희생 에셋(한 커밋, 에셋만이면 Unity가 모르는 칸을 버림).
- TS-f2·TS-f3: 서는 자리·행동 조립(새 `.cs` + `.meta`, 각 Phase 한 커밋, 또는 TS-f4와 함께 — TS-f4 전까지 화면 변화 없음).
- TS-f4·TS-f5: 공격 반응·발동 사건 행동(각 한 커밋).
- 이 묶음 전에 커밋 전 작업(겹친 강조 3판~6판·TA 반영분 등)을 먼저 커밋하는 것을 권한다(같은 파일 `CombatTraitEventHandler`·`CombatEventTypeTable`).

## 7. 실행 기록

### 7.1 TS-a 코드 완료 (2026-10-30, 사용자 "진행해" = Q-TS1~Q-TS8 권장 A·TP-1~TP-12 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

- Ta.0: 설계 §9 "계획 검토로 바뀐 것" 절.
- Ta.1·Ta.3 `CombatEventTypeTable`: `CombatTraitStateEvent` 줄(사실 칸, 주체 `Final.EntityId`, 근원 `Origin`), 발동 사건 줄에 근원 `TraitOriginsOf`(TP-3)·주체 `TraitSubjectOf`(TP-4, 옛 `TraitActivatorOf` 대신), 주체·근원 질문 설명.
- Ta.2 `CombatFactEventHandler`: 상태 사건 → `ApplyCharacter(Final)`, 표식 → 디버프 여섯 칸을 이름 있는 인자로 옮기고 표식만 바꿈(천명님 줄).
- Ta.4 `CombatHitFollowUps.Add`: 주체가 출처 공격자면 해제 대상 아님(TP-5, 이동 후속과 같은 규칙).
- Ta.5: 칸 자리 규칙을 `CombatCuePoints.CellPose` 하나로 옮김(순간 큐 `CombatMomentCuePlayback.CellPoseOf`가 부름), 특성 처리기 큐 자리 = 대상 모두의 몸·영향 칸 모두, 처리기 생성 인자 `mapper`(Presenter 한 줄, Factory 무변경).
- 계획과 다른 점: 없음.
- 새 파일 없음. 바뀐 파일: `CombatEventTypeTable.cs`·`CombatFactEventHandler.cs`·`CombatHitFollowUps.cs`·`CombatTraitEventHandler.cs`·`CombatMomentCuePlayback.cs`·`CombatCuePoints.cs`·`CombatSceneEventPresenter.cs`(모두 커밋 전 TA 작업 hunk가 같이 있음 — `CombatTraitEventHandler.cs`는 새 파일째 커밋 전).
- 다른 사람 줄: 천명님 표식 줄 하나(공유는 TS-g Tg.2).

### 7.2 특성·발동 시기 표 (2026-10-30, 사용자 "TS-b 진행하는데 그 전에 현존 특성 및 발동 시기 표로 정리", 코드 변경 없음)

- 새 [전투 특성과 발동 시기](../viewDocs/combat-trait-activation-timing.md): 발동 시기 단계 A~N(공격 한 번 안 A~G, 공격 밖 H~N, Core 순서 근거), 실행되는 특성 37개(옛 15·새 22)의 하는 일·시기·발동 종류·남는 사실·View 길, Core가 꺼 둔 5개.
- 찾은 것: 가속의 쌓인 공격력·사냥꾼 Run 공격력은 `TraitEffects.AttackGrants` 밖이라 Td.2 "공격력 부여" 줄에 안 들어감(§4 메모, TS-d 착수 때 정함).

### 7.3 TS-b 코드 완료 (2026-10-30, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ub 대기)

- Tb.1: 새 `Playback/Ordering/CombatAttackSource.cs`(+ `.meta`, 공격자·공격 그룹·관통 기록·열쇠·`Of`). 형식 표 공격 출처 칸이 이 값을 돌려줌(공격 사건·포효·**추가 공격 시작** `ChildAttackSourceOf`), `TryGetAttackSource(e, out CombatAttackSource)` 하나.
- Tb.2: `CombatHitFollowUps.Begin`이 한 행동 구간에 출처를 여럿(근원 열쇠별 표 `_openSources`) 두고, 추가 공격 시작은 목록을 비우지 않고 더함. 피해 후속은 근원 열쇠로 출처를 찾고(`TryFindSource`), 이동 후속은 가장 최근 출처. 옛 `Add`·`AddMovement`는 `AddFollowUp` 하나(공격자 자신 = 해제 대상 아님). `CombatConcurrentGroups.TryGetKey`는 출처 값의 열쇠.
- Tb.3: 판정 `CombatEventTypeTable.IsChildAttackStart`(= `ChildAttack` 있음)·`HasChildAttackStart`, 옛 `IsReplayedFollowUp`·`HasTargetedFollowUp` 삭제. `TraitActivationVisual`의 `IsFollowUpAttack`·`IsFollowUpAttackKind`와 "공격 표시는 추가 공격 줄에만" 검사 삭제, 툴팁·설명 고침(Q-TS2 A). 형식 표의 DB 표 using 삭제.
- Tb.4: `CombatAttackShape.ForChildAttack(start, trait)`(칸·방향은 사실에서, 완료 `FirstRelease`, 방향 없으면 계약 예외), `ForFollowUp`·`DirectionOf` 삭제. `CombatHitCompletion.AnyRelease`와 Gate `ReleaseAll` 갈래 삭제.
- Tb.5: 특성 처리기 — 발동마다 줄이 있으면 큐, 추가 공격 시작이면 줄 유무와 상관없이 공격 시작(Q-TS1 A), 추가 공격 시작이 든 사건의 발동이 둘 이상이면 계약 예외, 발동 유닛이 보인 사실에 없으면 치지 않음(TP-6). 경고 둘·`_warned`·`WarnOnce` 삭제, 줄 값에서 "추가 공격 줄인지" 칸 삭제.
- 주석: Presenter·공격 시작·주체 막기의 "대상 있는 추가 공격" 문구.
- 계획과 다른 점: `CombatShownFacts.TryGetCell`(TA-d가 추가 공격 칸을 구하려고 넣음)을 부르는 곳이 없어져 메서드도 지움(우리 코드).
- 묶음 끝 검토 후보로 적어 둠: `CombatAttackShape.Completion`은 이제 공격 사건·추가 공격 모두 `FirstRelease`라 늘 같은 값(칸을 걷으면 조립 둘을 고침 — 설계 TA5 "모양이 완료를 정함"을 바꾸는 일이라 이번에는 그대로).
- 바뀐 파일: 새 `CombatAttackSource.cs`(+ `.meta`), `CombatEventTypeTable.cs`·`CombatHitFollowUps.cs`·`CombatConcurrentGroups.cs`·`TraitActivationVisual.cs`·`CombatAttackShape.cs`·`CombatHitGate.cs`·`CombatTraitEventHandler.cs`·`CombatShownFacts.cs`·`CombatSceneEventPresenter.cs`·`CombatAttackStart.cs`·`CombatSubjectBlocking.cs`(주석). `TraitActivationVisual.cs`는 View DB 파일이라 호진님 공유(Tg.4).
- Ua·Ub 문제 없음(10-30 사용자).

### 7.4 TS-c 코드 완료 (2026-10-30, 사용자 "문제 없음, TS-c", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uc 대기)

- Tc.1: `CombatMovementKinds.Of(CombatCharacterEvent step)` — `Moved`가 아니면 밀림, 이동 원인이 Forced면 밀림, Voluntary·없음이면 스스로(TP-7). `CombatMovementKind` 설명 고침.
- Tc.2: `CombatEnemyPresentationPacing` — `BeforeStep`이 `CombatMovementKinds.Of`를 부름. `IsVoluntaryMovement`·`_movementActor`·`_attackStarted`와 `BeginResult`의 행위자 고르기 삭제(공격 갈래는 간격만 냄).
- Tc.3: `CombatMovementPresentation.KindOf` = `CombatMovementKinds.Of`(정적), 주석.
- 계획과 다른 점: 옛 판정의 "소유 진영이 아군·적일 때만" 조건은 옮기지 않음(Core 원인이 진영과 무관하게 같은 뜻이라 — 중립 캐릭터의 스스로 걸음도 뛰어 이동으로 보임, 지금 중립 캐릭터 이동은 없음).
- 문서 남은 것: 걸음 모양 설계(`specs/2026-10-28-combat-step-motion-style-design.md`)의 "판정 원본 `IsVoluntaryMovement`" 문장은 TS-g Tg.1에서 고침.
- TS-d 착수 전 정할 것: 가속의 쌓인 공격력은 `CharacterCombatFacts`의 능력치 특성 보유 상태(`UnitStatsSnapshot.TraitStatuses`의 `AccelerationTraitStatusSnapshot.HeldAttackBonus`)에 있어 HUD 줄 하나로 읽을 수 있다(§7.2).

### 7.5 TS-d 코드·에셋 완료 (2026-10-30, 사용자 "넣어, TS-d 진행" = 가속 줄 더함, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ud 대기)

- Td.1 `UnitStatusKind`: `Disarm = 4`·`AnkleBreaker = 5`·`ArmorBreak = 6`·`AttackBonus = 7`·`Shield = 8`·**`Acceleration = 9`**(사용자 결정으로 계획보다 하나 더).
- Td.2 `CombatUnitStatuses`: 디버프 줄 셋(남은 그 진영 턴 끝 수·겹친 원본 수 — 원본이 0개면 안 걸림), 버프 줄 셋(공격력 부여 합·켜진 보호막 합·가속 쌓인 공격력 `HeldAccelerationOf`), 숫자 줄 꼴 `StatusRow.OfAmount`(0 이하 = 안 걸림, TP-8). 머리 주석(읽는 칸 셋, R5 보강은 아직).
- Td.3 에셋: `DataBaseSO/ViewGroup/UnitStatusVisual/Item/UnitStatusVisual_Disarm`·`_AnkleBreaker`·`_ArmorBreak`·`_AttackBonus`·`_Shield`·`_Acceleration`(+ `.meta`, 기존 꼴 CRLF·`\u` 이스케이프, 아이콘 비움), 표 `UnitStatusVisualDataTable.asset`에 여섯 줄. 문구는 초안(틀 칸 `{0}` 하나씩):
  - 무장 해제 "공격할 수 없습니다. 이 진영 턴이 {0}번 더 끝나면 풀립니다." / 발목 부수기 "스스로 이동할 수 없습니다. …" / 방어구 부수기 "방어력이 감소했습니다({0}회 중첩). 스테이지가 끝날 때까지 유지됩니다."
  - 공격력 증가 "특성으로 얻은 공격력 +{0}." / 보호막 "피해를 {0}만큼 먼저 막습니다." / 가속 "다음 공격의 공격력 +{0}. 공격하면 소모됩니다."
- Td.4: `CombatActionNotice`(현강님 파일) 사유 표에 `Disarmed`·`AnkleBroken` 안내 두 줄(결과 거절·미리보기 거절이 같은 함수를 지남, 이동 거절의 `Disarmed`도 같은 이름이라 함께 받음), `CombatActionStateText` 짧은 글자 "무장해제"·"발목꺾임".
- 계획과 다른 점: 가속 줄(사용자 결정). 그 밖 없음.
- 커밋: 열거·줄 표 코드와 에셋 여섯(+ `.meta`)·표 에셋은 **한 커밋**(전투 진입 때 줄마다 외형을 확인해, 하나라도 빠지면 멈춤). 안내 두 파일은 같이 또는 따로.
- 공유: 상태 외형 표 새 줄(호진님, Tg.4), 안내 두 줄(현강님, Tg.3).

### 7.6 TS-e 코드·에셋 완료 (2026-10-30, 사용자 "문제 없음, TS-e" = Uc·Ud 문제 없음, 컴파일 미확인, 새 시험 없음, 시험 변경 = 천명님 시험 인자 하나, Ue 대기)

- Te.1 `TraitVisualData`: 칸 둘 `trailPrefab`(선택)·`trailLocalOffset`(필드 기준 월드 단위), 읽기 `TryGetTrail`(프리팹이 없으면 false), 검사는 보정 값이 유한한 수인지만. 어느 특성이 흔적을 남기는지는 Core가 정하고 이 칸은 외형만 준다.
- Te.2 새 `Display/Map/FieldTrailPresenter.cs`(+ `.meta`): 원본 ID마다 인스턴스, 특성 키마다 풀. 보일지 = 켜짐 · 플레이어 공개(`CombatEnvironmentFacts.IsKnownToPlayer`, 지형·오브젝트와 같은 판정). 자리 = 칸 표면(`CellSurfaceToLocal`) + 보정 + 같은 칸에서 원본 ID가 더 작은 흔적 수 × 0.002(TP-10, 겹쳐 깜박이지 않게). 외형이 없으면 경고 한 번 뒤 그리지 않음(TP-11). 루트 `FieldTrails`는 필드 칸 루트 아래.
- Te.3 필드 창구 `CombatFieldEventPorts` 필수 칸 `showTrail`(지형 바뀜 다음 자리), 사실 처리기 갈래 "이동 원본 사건의 결과가 흔적이면 사건 시점에 그 원본 하나를 맞춤"(생성 = 이전 값 없음·남은 턴 바뀜·꺼짐 모두, 처리기 인자는 다른 필드 창구처럼 선택), Presenter가 자기 Mapper로 넘김. 천명님 `PullAndMarkPresentationTests.cs:445`(계획의 `:442`는 생성 시작 줄) 창구 생성에 `(_, _) => { }` 하나 — 컴파일 맞춤만.
- Te.4 `CombatSceneDisplay`: 생성 인자 `trails`(가시 다음, 필수), 결과 끝 `_trails.Render(state.Environment, _field.Mapper)`(지형 그리기 바로 뒤). 흔적은 Mapper를 붙잡지 않아 Object·Terrain처럼 `_fieldBound`에 넣지 않고, 설치 실패·`Clear` 목록에 `Clear`, `Release` 목록에 `Release`.
- Te.5 Factory(민형님 연결 줄): `new FieldTrailPresenter(field.CellsRoot, tables.Traits)`, 창구 `showTrail: trails.Show`, 표시 인자 `trails`.
- Te.6 임시 아트(TP-11): 새 폴더 `Prefabs/Default/Environment/Trail/`(+ `.meta`) — `Trail_GroundCompaction.prefab`·`Trail_DustScreen.prefab`(낭떠러지 꼴: 루트 아래 `Surface` 기본 Quad, X 90°, 높이 0.012, 크기 0.8 — 칸보다 작아 아래 타일이 둘레에 보임, 그림자 끔, 레이어 Default, 정렬 순서 −2 = 약한 지반 바닥과 같은 칸 채우기 아래 자리), 재질 `TrailGroundCompaction.mat`(갈색 0.6·0.42·0.22, α 0.45)·`TrailDustScreen.mat`(옅은 회색 0.82·0.8·0.72, α 0.55)(URP Unlit 반투명, 약한 지반 재질 꼴), 모두 `.meta` 함께. `TraitVisual_GroundCompaction`(340)·`TraitVisual_DustScreen`(350)에 `trailPrefab` 연결(두 에셋에 빈 `activations: []`도 적음 — Unity가 저장할 때 넣는 기본값과 같음). `FieldSortingOrders` 머리 주석에 흔적 프리팹 한 줄.
- 계획과 다른 점: 흔적 프리팹 정렬 순서 −2(계획에 없던 값 — 이동 범위 등 칸 채우기가 흔적 위에 덧칠되어 강조가 흔적에 가려지지 않게). 그 밖 없음.
- 커밋: `TraitVisualDataSO.cs`와 흔적 프리팹 둘·재질 둘(+ `.meta`, 폴더 `.meta`)·특성 외형 에셋 둘은 **한 커밋**(코드만이면 흔적이 안 그려지고 경고, 에셋만이면 Unity가 모르는 칸을 버림). 표시·창구·Factory·처리기·Presenter와 새 `FieldTrailPresenter.cs`(+ `.meta`), 천명님 시험 한 줄은 **한 커밋**(창구 생성 인자가 바뀜).
- 공유(Tg 묶음): 민형님 Factory 연결 줄 셋, 천명님 시험 인자 하나(Core 전달 문서 §5), 호진님 특성 외형 표 흔적 칸 둘·에셋 두 개 연결(두 에셋은 호진님 `6b0755be2` 확인용).

### 7.7 TS-f1 행동 값 코드·에셋 완료 (2026-10-30, 사용자 "그대로, TS-f1" = TP-13~TP-25 그대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uf1 대기)

- Tf1.1 새 `DataBaseSystem/Data/View/CombatPresentation/CombatUnitActionActor.cs`(`None`·`Source`·`Targets`)·`CombatUnitActionPlace.cs`(`Stay`·`GuardFront`·`TowardTarget`)·`CombatUnitActionFace.cs`(`Keep`·`Target`·`Attacker`)(+ `.meta`).
- Tf1.2 새 `CombatUnitActionData.cs`(불변 값, 검사는 생성자 하나: 행동 유닛 `None` 아님·열거 정의·거리 [0, `MaxDistance` 1.5]·시간 셋 0 이상 유한 — 수 규칙은 `CombatPresentationValueRules`)·`CombatUnitActionInput.cs`(직렬화 칸 여덟·툴팁, `ToData()`는 행동 유닛 `None`이면 null)(+ `.meta`). 걸음 모양은 기존 `CombatStepLook`을 같이 쓴다.
- Tf1.3 `TraitActivationVisual`: 칸 `unitAction`(기본 `None`)·읽기 `UnitAction`·`Validate`가 값을 한 번 만들어 검사(범위 밖이면 특성·Kind를 붙인 `InvalidOperationException`), 문서에 세 번째 형태.
- Tf1.4 `TraitVisual_Sacrifice.asset`: 발동 줄 하나 — `kind: 110`(`DamageRedirected`), 큐 빈 값, 행동 `Source`·`GuardFront`·0.6·`Hop`·나가기 0.2·머묾 0·돌아오기 0.25·`Attacker`(P12). 이 에셋에 없던 `trailPrefab`·`trailLocalOffset`(TS-e 칸)도 기본값으로 적었다(Unity가 저장할 때 넣는 값과 같음). 표 등록은 이미 돼 있음.
- 계획과 다른 점: 없음.
- 커밋: 새 `.cs` 다섯(+ `.meta`)·`TraitActivationVisual.cs`·`TraitVisual_Sacrifice.asset`은 **한 커밋**(TS-e의 `TraitVisualDataSO.cs`가 커밋 전이면 함께 — 이 에셋이 그 칸을 씀).

### 7.8 TS-f2 서는 자리 코드 완료 (2026-10-30, 사용자 "문제 없음, TS-f2" = Uf1 문제 없음, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uf2 대기)

- Tf2.1 새 `Display/Unit/ICombatUnitStand.cs`(+ `.meta`): `IsAvailable`·`IsFolded`·`CanMove`(자세 대여 없음)·`HomePosition`/`HomeRotation`(빌릴 때 자리)·`Position`/`Rotation`(마지막으로 쓴 자리)·`ApplyStand`·`RestoreStand`.
- Tf2.2 `FieldUnitView`: 대여 칸 `_standSlot`(자세 칸과 따로), 대여 클래스 `PresentationUnitStand`(쓰기는 위치 + `ApplyRotation`, 체력 숫자는 자식이라 따라감, 되돌리기 = 빌릴 때 자리이고 사망 모델 유지 중이면 쓰지 않음, `Fold` = 자리를 쓰지 않고 칸만 비움·취소 콜백 없음), `Apply`가 자세 또는 서는 자리 대여가 있으면 위치·회전을 쓰지 않음, 정리 목록 순서 자세 → **서는 자리** → 크기 → 플래시 → 클립(위 층을 먼저 되돌려 끝 자리가 빌리기 전 자리). 계약 `ICombatUnitPresentationTarget`에 `HasStand`·`BeginStand(취소, 제자리 유지 질문)`·`FoldStand`·`TryGetStand`.
- Tf2.3 `CombatUnitPresentationWindow`: `TryBeginStand`(켜진 표시·걸음 배치 중 아님, 제자리 유지 질문 = `HoldsDeath`)·`TryGetStandPose`, `KeepsPlacement`에 서는 자리(TP-18), `BeginPlacement`가 서는 자리를 접고 유닛마다 경고 한 번(TP-19, `_foldWarned`는 `Reset`이 비움), 요약 줄.
- Tf2.4 `CombatPieceChannel.Stand = 5`, 실행 대여 한 칸(`TryBeginStand`, 되돌리기 `RestoreStand`)·꺼내기 `Stand(unit)`.
- Tf2.5 `FieldUnitPresenter.BodyAt(facts, 서는 자세, mapper)`·`BodyCenterAt(facts, 서는 자세)`(상자 배치 식은 `PlaceBodyBox` 하나), `CombatShownFacts`의 ID로 묻는 셋(`TryGetBody`·`StandingBodyOf(id)`·`TryGetBodyCenter`)이 창구의 서는 자리를 먼저 봄(비공개 도우미 `TryGetStand`). 사실로 묻는 `StandingPoseOf`·`StandingBodyOf(facts)`·`TryGetBodyCenterOf(facts)`는 그대로.
- 계획과 다른 점: `TryGetBodyCenter(id)`(피격 이펙트 몸 중심)도 서는 자리를 보게 함(Tf2.5 표에는 둘만 적었음 — 수호자의 피격 이펙트가 옮긴 자리에 나와야 해서). 그 밖 없음.
- 다른 사람 줄: 바꾼 함수 줄은 모두 우리 커밋(`ec34d932`·`034b0b26`·`efa5e7d7`·`c417e9ee`, blame 확인). 공유 없음.
- 화면 변화 없음(서는 자리를 빌리는 조립이 아직 없음).
- 커밋: 새 `ICombatUnitStand.cs`(+ `.meta`)와 바꾼 일곱 파일을 한 커밋.

### 7.9 TS-f3 행동 조립 코드 완료 (2026-10-30, 사용자 "문제 없음, TS-f3" = Uf2 문제 없음, 컴파일 미확인, 새 시험 없음, 시험 변경 = 천명님 시험 가짜 한 줄, Uf3 대기)

- Tf3.1 새 `Presentation/Pieces/CombatUnitActionPlaces.cs`(+ `.meta`): `EndPose(자리, 거리, 보기, 빌릴 때 자리, 대상 자리?, 공격자 자리?, mapper)` 한 곳. 거리 = 칸 비율 × `WorldCellWidth`, 방향은 필드 평면에서, `GuardFront` = 지켜진 유닛 발밑 + 공격자 쪽(높이는 지켜진 칸 표면, P2), 기준점이 없으면 제자리. 방향은 공격 방향 계산 `CombatAttackFacing.Toward`를 같이 씀(`Keep` = 옮겨 가면 가는 쪽).
- Tf3.2 새 `CombatStandMovePiece.cs`(+ `.meta`): 서는 자리 대여로 지금 자리 → 끝 자리(시작할 때 한 번 정함), 위치 곧게 + 뛰기면 `CombatStepHeight.Arc(0, 0, 진행률, 뛰기 높이)`를 위쪽으로, 방향 Slerp. 자세 대여가 있으면 그 프레임은 시간을 보태지 않고 기다림, 대여를 쓸 수 없게 되면(접힘·표시 꺼짐) 쓰지 않고 끝. 조각 맥락에 `Stand(unit)` 한 줄.
- Tf3.3 새 `CombatUnitSettleWaitPiece.cs`(+ `.meta`): 끝 = 후속 정리 질문, 판정은 `Tick`에서만.
- Tf3.4 새 `Playback/Ordering/ICombatSourceSettlement.cs`(+ `.meta`), `OrderedCombatPresentation`이 구현(`IsSettled`: `_first` 뒤 미완료 사건 중 그 출처의 맞는 순간 후속이고 주체가 그 유닛인 것이 없으면 true), 동시 묶음 `CombatConcurrentGroups.IsHitFollowUpOf`(후속 목록의 `ReleasedByHit`만, 출처는 참조로). 넘기는 길: 재생기 `Attach` → 재생 계약 새 멤버 `ICombatEventPlayback.BindSettlement` → 재생 조정 `CombatSceneEventPlayback` → 사건 Presenter `BindSettlement`(필드 보관, TS-f4에서 공격 시작에 넘김 — 그때까지 "대입만 하고 안 읽음" 경고 CS0414가 날 수 있음).
- Tf3.5 새 `CombatUnitActionComposer.cs`(+ `.meta`): 값 `Guard`(수호자·지켜진 유닛·행동 값), `AddGuards`(빌릴 수 있는 수호자만 서는 자리 대여 + 나가기 항목, 뼈대 시작 = 가장 긴 나가기 끝 뒤, 넣은 수호자를 돌려줌), `AddReturns`(뼈대 Hit 뒤 대기 → 돌아오기), `ComposeMoment`(유닛 범위·일반 완료: 나가기 → 머묾 지연 → 돌아오기). 창구에 `CanBeginStand`(조립이 미리 물음), `TryBeginStand`가 같은 조건을 씀.
- 계획과 다른 점:
  - TP-13의 "그 유닛 연출 빔"은 따로 묻지 않는다. 피격·사망 반응이 끝난 뒤 남는 위치 소유는 자세 대여뿐이고, 돌아오기 조각이 자세가 빌 때까지 기다린다(`CanMove`). 대기 조각은 후속 정리 질문만 본다.
  - 후속 정리 질문은 맞는 순간에 풀리는 후속(`ReleasedByHit`)만 센다. 출처 전체 종료 때 풀리는 후속(관통 공격의 비접촉 대상)까지 세면, 그 후속은 공격 실행이 끝나야 풀리고 공격 실행은 대기가 끝나야 끝나 서로 멈춘다.
  - 넘기는 길이 재생 계약 멤버 하나(`BindSettlement`)를 더해, 그 계약을 구현한 천명님 시험 가짜 `PullAndMarkPresentationTests.Playback`(`:271`)에 전달 한 줄을 넣었다(컴파일 맞춤, Core 전달 문서 §5.61).
- 화면 변화 없음(조립기를 부르는 곳이 아직 없음).
- 커밋: 새 `.cs` 다섯(+ `.meta`)과 바꾼 파일(조각 맥락·창구·동시 묶음·순차 재생기·재생 계약·재생 조정·사건 Presenter·천명님 시험 한 줄)을 한 커밋(계약 멤버가 늘어 시험 한 줄이 없으면 시험 어셈블리가 컴파일 안 됨).

### 7.10 TS-f4 공격 반응 행동(희생) 코드 완료 (2026-10-30, 사용자 "문제 없음, TS-f4" = Uf3 문제 없음, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uf4 대기)

- Tf4.1 `Presentation/Attack/CombatAttackShape.cs`: 생성자 끝 인자 `reactions`, 속성 `Reactions`(공격 사건 = 공격 결과 발동 목록 중 가로채기가 있는 것, 추가 공격 = `ChildAttack.Reactions` 그대로)와 파생 `ContactTargetId`(주 대상을 지킨 가로채기가 있으면 실제로 맞은 유닛, 아니면 주 대상).
- Tf4.2 `CombatAttackPlayback.cs`: 직선 접촉 진행률이 `ContactTargetId` 몸으로. 몸 조회가 서는 자리를 먼저 보므로(TS-f2) 수호자가 옮긴 자리에서 맞는다.
- Tf4.3 `Presentation/Pieces/CombatCueAnchors.cs`: `StartAt(시점)` — 시작(Start) 큐를 실행 시작 대신 그 시점(+ 큐 지연)에. 실행 시작·지연 0을 넘기면 지금과 같다.
- Tf4.4 새 `Presentation/Pieces/CombatAttackLead.cs`(+ `.meta`, 앞 항목 = `Begin(조립)`이 뼈대 시작 시점을 돌려주고 `AfterHit(조립, 맞는 순간 항목)`이 그 뒤를 붙임). `CombatCodeMotionAttack.Compose`·`CombatClipAttack.ComposeMelee`·`ComposeRanged` 끝 선택 인자 `lead`: 조립을 만든 직후(클립은 자세·클립 대여 뒤) `Begin`, 뼈대 항목(모션 / 방향·클립)과 큐 표 `StartAt`이 그 시점, 맞는 순간 항목(모션 / 해제·발사)을 만든 뒤 `AfterHit`. 인자가 없으면 조립이 지금과 같다.
- Tf4.5 `Playback/Support/CombatAttackStart.cs`: 생성자 끝 인자 셋(큐 값 답·뛰기 높이·후속 정리 질문 `Func`), `LeadOf(모양)` — 가로채기마다 (특성, 발동 종류) 줄을 큐 값 답으로 찾아(연결 동안 보관) 반응 줄 큐와 행동을 모음, 앞 항목 `Begin`은 반응 줄 큐(실행 시작, P6, 자리는 나가기 전) → `AddGuards`(수호자 = 가로채기 보호자, 지켜진 유닛 = 보호 대상, 서는 자리를 못 빌리면 그 수호자만 뺌 P7), `AfterHit`은 넣은 수호자가 있으면 `AddReturns`. 두 공격 길 모두 넘김. `Playback/CombatSceneEventPresenter.cs` 생성 줄이 `cueSources`·`values.StepLook.HopHeight`·`() => _settlement`를 넘김(TS-f3의 CS0414 걱정 없어짐, Factory 무변경).
  - 같이 고친 것(SSOT): 특성 발동 큐 자리 계산을 `CombatCuePoints.ForActivation(발동 사실, 몸 조회, mapper)` 한 곳으로 옮기고 특성 처리기 `PointsOf`가 그것을 부름(공격 시작의 반응 줄 큐와 같은 규칙). 보인 사실에 `StandingPlaceOf(id)`(서는 자리가 있으면 그 자리, 아니면 사실대로 선 자세 — 행동 자리 계산의 기준점).
- Tf4.6 `Playback/Handlers/CombatRoarEventHandler.cs`: 포효 결과에 가로채기가 있으면 연결 동안 경고 한 번, 조립 무변경(P9).
- 계획에서 더 정한 것(확인 대상):
  - **TP-26** 가로채기 줄의 행동 유닛이 `Source`가 아니면(대신 맞기에서 뜻이 없음) 경고 한 번 뒤 행동을 뺀다(큐는 냄). 자리가 `GuardFront`가 아니면 경고 한 번 뒤 그 자리 그대로 간다(계획 Tf4.5의 "경고 한 번"을 이렇게 읽음).
  - **TP-27** 반응 줄 큐는 공격 조립 안에 넣는다(곁들임 실행을 따로 열지 않음). 시작 = 공격 실행 시작 = 나가기 시작이고, 큐 창구는 이펙트·흔들림만(특성 발동 큐와 같음, 소리는 소리 표).
  - **TP-28** 뛰기 높이는 걸음 모양 설정 값이라, 자발 이동을 걷기(`Walk`)로 두면 0이 되어 수호자가 미끄러지듯 나간다(기본 에셋은 Hop 0.3).
- 화면 변화: 적 턴에 희생이 발동하면 수호자가 나갔다가 맞고 돌아온다. 희생이 없는 공격은 조립이 지금과 같다.
- 커밋: 새 `CombatAttackLead.cs`(+ `.meta`)와 바꾼 파일(공격 모양·공격 재생·큐 표·공격 조립 둘·공격 시작·사건 Presenter·큐 자리·특성 처리기·보인 사실·포효 처리기)을 한 커밋. TS-f1~TS-f3이 커밋 전이면 함께.

### 7.11 TS-f5 발동 사건 행동 코드 완료 (2026-10-30, 사용자 "No issues, TS-f5" = Uf4 문제 없음, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Uf5 대기)

- Tf5.1 `Playback/Handlers/CombatTraitEventHandler.cs`: 줄 값에 유닛 행동(`ActivationRow.Action` = `TraitActivationVisual.UnitAction`). 발동마다 큐를 낸 뒤 행동이 있으면 `BeginUnitActions` — 가로채기는 하지 않음(공격 시작 몫), 추가 공격이면 경고 한 번 뒤 건너뜀(P9), `GuardFront`면 경고 한 번 뒤 `Stay`(P10), 행동 유닛 = `Source`(발동 유닛) 또는 `Targets`(대상마다), 유닛마다 `CombatUnitActionComposer.ComposeMoment` 유닛 실행을 완료 통지 없이 엶(TP-23, 사건은 그대로 바로 완료 — 처리기는 추가 공격이 아니면 지금처럼 false). 그 유닛의 유닛 실행이 이미 있거나 실행이 시작하지 않으면 경고 한 번 뒤 건너뜀(Q-AA6 A). 생성 인자 끝 둘: 조각 실행 재생기·뛰기 높이.
  - `Playback/Players/CombatPieceRunPlayer.cs`: 질문 `HasUnitRun(유닛)` 하나(주체 활동 계약의 `IsPlayingFor`도 이것을 부름).
  - `Playback/CombatSceneEventPresenter.cs`: 특성 처리기 생성 줄이 `piecePlayer`·`values.StepLook.HopHeight`를 넘김(Factory 무변경).
- 계획에서 더 정한 것(확인 대상): **TP-29** 사건 목록 안 행동의 대상(`Source`면 발동의 첫 대상, `Targets`면 발동 유닛), **TP-30** 바쁨 판정(`HasUnitRun` 먼저, 필드 위에 없는 유닛은 경고 없이 건너뜀). §1.1에 적음.
- 화면 변화 없음: 지금 행동 값이 있는 발동 줄은 희생(가로채기, TS-f4 몫) 하나뿐이다. 확인하려면 사건 목록 안에 나오는 발동 줄 하나에 임시 행동 값을 넣는다(Uf5).
- 커밋: 바꾼 파일 셋(특성 처리기·조각 실행 재생기·사건 Presenter). TS-f1~TS-f4가 커밋 전이면 함께.
- 고친 것(10-30, 사용자 보고 "'CombatUnitActionComposer' 관련 에러 주구장창"): TS-f3 때 `Presentation/Pieces/CombatUnitActionComposer.cs`를 만들며 셸 명령이 실패해 `.meta`만 먼저 생겼고, Unity가 "meta는 있는데 파일이 없다"(Editor.log)를 본 뒤 그다음 쓴 `.cs`를 스크립트로 가져오지 않아 컴파일 목록(`ProjectL.Systems.rsp`)에서 빠져 있었다. TS-f3까지는 아무도 부르지 않아 드러나지 않았고, TS-f4·TS-f5가 부르면서 "형식을 찾을 수 없음" 오류가 됐다. `.meta`를 다른 스크립트와 같은 꼴로 다시 쓰고(GUID `f190508c…` 그대로) `.cs` 시각을 갱신했다. 그 파일은 처음 컴파일되는 것이라 안의 오류가 새로 나올 수 있다(API 대조는 했음, 컴파일 미확인). 다른 새 `.cs` 열하나는 컴파일 목록에 있음을 확인했다. → 사용자 확인 "오류 없어짐"(10-30, Unity 컴파일 성공 — 시험·재생은 Uf4·Uf5로).

### 7.12 TS-g 문서·공유 (2026-10-30, 사용자 "TS-g 진행", 코드 변경 없음)

- Tg.1 문서:
  - [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md) — 머리 갱신 줄, §1.1 새 파일 자리(`Display/Unit/ICombatUnitStand`, `Display/Map/FieldTrailPresenter`, `Playback/Ordering/CombatAttackSource`·`ICombatSourceSettlement`, `Presentation/Pieces/CombatAttackLead`·유닛 행동 넷), §2 규칙: 이동 종류 판정 = `CombatMovementKinds.Of`(걸음 원인), 특성 발동 연출 줄을 고침(추가 공격 = `ChildAttack` 판정 `IsChildAttackStart`, `ForChildAttack`, 2연발 포함, 옛 `IsReplayedFollowUp`·`ForFollowUp` 걷음), 새 줄 둘(특성 발동 유닛 행동, 특성 흔적 지형).
  - [걸음 모양 설계](../specs/2026-10-28-combat-step-motion-style-design.md) 머리에 "2026-10-30 바뀜" 한 줄(판정 원본이 걸음 원인으로 옮겨짐, F6·ST1 문장은 그 전 기록).
  - [특성 시스템 View 설계](../specs/2026-10-29-combat-trait-system-view-design.md) 상태 줄(결정·구현 완료)·§10 구현 메모, [유닛 행동 설계](../specs/2026-10-30-combat-trait-unit-action-design.md) 상태 줄·§9 구현 메모.
- Tg.2 천명님(모두 미전달): 새 [천명님 공유](../handoffs/2026-10-30-combat-trait-system-view-cheon-share.md)(바꾼 천명님 줄 둘 — 표식 갈래·순차 재생기 클래스 선언 줄, 시험 두 줄 `:271`·`:446`, 병합 때 합친 `CombatMovementCalculator`, 요청 `WithHuntersMark`, View가 읽는 새 Core 사실 표, 따로 전달할 것), [Core 전달 문서](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) §5.59 덧붙임·§5.62(표식 줄)·§5.63(시험 `:446`)·§5.64(클래스 선언 줄)·§54(공유와 요청 R1).
  - 쓰며 바로잡음: 흔적 창구 인자는 `:446`이다(§7.6의 `:445`는 생성 줄 번호를 잘못 셈). 병합 합친 내용의 "지나가기만 되는 칸"은 도약(`LeapTraitRule.AllowsIntermediate`) 보유 유닛에만 생긴다(Core 코드 확인).
- Tg.3 현강님: [구조 메모](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md) §18(`CombatActionNotice` 사유 두 줄, 짧은 글자 둘은 우리 파일, 미전달).
- Tg.4 호진님: 새 [호진님 공유](../handoffs/2026-10-30-combat-trait-system-view-hojin-share.md)(상태 외형 여섯·표 등록, 흔적 칸 둘·에셋 둘 연결, 발동 줄 덮어쓰기 제한 풀림·`unitAction` 칸·희생 줄, 남은 에셋 작업, 미전달).
- Tg.5 민형님: [Factory 공유](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) §42(흔적 표시 연결 줄 셋, 미전달).
- 새 문서는 기존 공유 문서처럼 CRLF로 썼다.
- Tg.6 묶음 끝 검토(새 에이전트 하나, 7기준, 읽기만): **약 9.1** — SSOT 9.5·SOLID 9.0·FW 10·독립성 6.75·캡슐화 10·C#/Unity 9.25·If/Tick 9.25. 권장 반영 뒤 예상 약 9.6(높음·중간), 낮음까지 약 9.8. 높음·중간은 코드로 다시 확인했다(맞음).
  - **H1(높음, 결함):** 대신 맞은 수호자가 죽으면 전투가 멈춘다. 사망 반응이 끝나 사망 유지가 풀리면(`DeathHold.Release` → `ReleaseRental`) 표시가 풀로 돌아가고(`ResetForPool` → `ResetStand`), 서는 자리 칸 `Reset`이 공격 실행의 취소 콜백을 불러 실행이 사라짐이 되고 다음 Tick에 F1. 사망은 그 출처의 후속이라 돌아오기 대기 중에 늘 이 순서로 온다(Uf4 ④가 깨짐).
  - **M1(중간, 결함):** 돌아오기 대기(`IsSettled`)가 차례 재생 규칙을 보지 않아, 수호자 후속 앞에 다른 묶음의 미완료 사건이 있고 그 사건이 Gate 규칙 1(공격자 자신의 캐릭터 사건 — 예: 반사 피해)로 출처 전체 종료를 기다리면 서로 기다려 오류 없이 멈춘다(`CanStart` + `CombatHitGate.CanBegin` 규칙 1). TS-f4 전에는 공격 실행이 스스로 끝나 이 고리가 없었다. TP-13의 "서로 멈추지 않음"은 Gate만 보고 차례 규칙을 빠뜨렸다.
  - **M2(중간):** `CombatAttackStart`가 가로채기 줄 찾기·보관·검사·앞 항목 만들기까지 맡아 생성 인자 11개, (특성, 발동 종류) 줄 보관이 특성 처리기와 두 벌.
  - 낮음 L1~L10: 가로채기 판정 사본 넷·`ForChildAttack`은 반응을 거르지 않음(L1), GuardFront 기준점 문구 "몸 가운데" ↔ 코드 발밑(L2), 후속 정리 질문 전달 세 단계(L3), 같은 수호자 둘이면 `Lease` 예외(L4), `CanBeginStand`가 자세 대여를 안 봐 밀린 자리가 집이 될 수 있음(L5), 계약 검사가 큐·실행 시작 뒤(L6), `FieldUnitView` 플래시 대여 요약 주석 꼬임·`ICombatUnitStand`의 없는 `Home` 참조(L7), 읽는 곳 없는 `IsFolded`·늘 참인 검사 둘(L8), `CombatAttackShape.Completion` 늘 같은 값(L9, §7.3에 적은 것), 경고 기록 하나를 두 사유가 같이 씀(L10).
  - 반영은 사용자 결정 대기.

### 7.13 묶음 끝 검토 권장 반영 (2026-10-30, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

- **H1 (A안, 서는 자리 = 없어져도 되는 칸):** `FieldUnitView.ReleasePresentation`(다시 묶기·꺼짐·풀 반환)이 서는 자리를 다른 칸처럼 `Reset`(취소 콜백)하지 않고 `DropStand`(빌릴 때 자리로 되돌리기만, 사망 유지면 그 자리)로 내려놓는다. 빌린 실행은 이어 가고, 그 서는 자리 조각은 쓸 수 없으면 바로 끝나며, 실행 끝 되돌리기는 지난 대여라 아무 일도 하지 않는다. 수호자가 죽어 표시가 풀로 돌아가도 공격 실행이 멈추지 않는다. `ICombatUnitStand` 머리 설명에 한 단락.
- **M1:** `OrderedCombatPresentation.IsSettled`가 아직 시작하지 않았고 차례 규칙(`CanStart`) 때문에 지금 시작할 수 없는 후속은 세지 않는다. 그래서 수호자 후속 앞에 출처 전체 종료를 기다리는 다른 묶음 사건(공격자 자신의 반사 피해 등)이 있어도 서로 멈추지 않는다. 그때 수호자는 먼저 돌아오고 그 후속은 돌아온 자리에서 나온다. `ICombatSourceSettlement` 설명 고침. **TP-13 바뀜**(끝 조건에 "지금 시작할 수 있는 후속만").
- **M2:** 새 `Playback/Support/CombatActivationRows.cs`(발동 줄 보관 하나 + 값 `CombatActivationRow`)·`Playback/Support/CombatGuardLead.cs`(가로채기 앞 항목 만드는 곳, 옛 `CombatAttackStart.LeadOf`·`GuardActionOf`)(+ `.meta`). `CombatAttackStart` 생성 인자 11 → 9(끝 인자 = 앞 항목 만드는 곳), 특성 처리기는 큐 값 답 대신 발동 줄 보관을 받고 자기 줄 보관을 걷음. Presenter 생성 줄이 둘을 만들어 넘김(Factory 무변경).
- 낮음:
  - L1 가로채기 판정 한 곳 `CombatAttackShape.IsInterception`·`HasInterception`(공격 모양 반응 목록·포효 경고·특성 처리기가 부름), 추가 공격 반응 목록도 가로채기만 담음.
  - L2 `CombatUnitActionInput` 거리 툴팁: GuardFront 기준점 = 지켜진 유닛이 서 있는 자리(발밑)(코드와 같게, 동작 같음).
  - L4 한 공격에서 같은 수호자가 둘을 지키면 첫 가로채기만 행동(`AddGuards`).
  - L5 `CanBeginStand`·`TryBeginStand`가 자세 대여가 있으면 빌리지 않음(조건 한 곳 `TryGetStandTarget`). 그래서 자기 공격 자세 중인 유닛의 사건 순간 행동은 바쁨으로 건너뛴다(경고 한 번).
  - L6 특성 처리기가 추가 공격 계약 검사(발동 하나·방향)를 큐·행동을 내기 전에 한다(`ChildAttackOf`, 공격 모양을 먼저 만듦).
  - L7 `FieldUnitView` 플래시 대여 요약 주석을 제자리로, `ICombatUnitStand`의 `Home` 참조를 `HomePosition`·`HomeRotation`으로.
  - L8 `ICombatUnitStand.IsFolded`·`_folded` 걷음, 창구 `TryBeginStand`의 두 번째 조회와 `_keepsWhereItIs != null` 걷음(만들 때 null 검사).
  - L10 경고 기록을 사유마다 따로(특성 처리기 셋, 앞 항목 둘).
  - L3(후속 정리 질문 전달 세 단계)·L9(`Completion` 늘 같은 값)는 그대로.
- 작업 중 본 것: 19:52에 이 세션 밖에서 `Factory/Runtime/InGame/View/CombatEventPlaybackFactory.cs`(새, 사건 재생 조립을 "운영 전투와 연출 확인 환경" 같은 길로)·`CombatSceneDisplayFactory`·`CombatSceneEventPresenter`(`Refresh` 오버로드)·`CombatShownFacts`(`Replace`)·`CombatEffectWindow`(Scope를 빌리는 생성자)가 바뀌었다. 그 변경은 그대로 두고 그 위에 고쳤다(Presenter는 생성 줄만).
- 커밋: 새 `.cs` 둘(+ `.meta`, `.cs`를 먼저 쓴 뒤 `.meta`)과 바꾼 파일을 TS-f 묶음과 한 커밋.
- 확인(10-30 사용자): **Unity 컴파일 문제 없음.** 특성 연출 재생 확인(Ua~Ue, Uf1~Uf5와 이 반영분)은 **모두 보류** — 사용자가 특성 연출 시험 환경을 만드는 중이다(19:52의 `CombatEventPlaybackFactory` 등 사건 재생 조립 변경이 그 작업으로 보임). 그 환경이 생기면 재생 확인을 이어 간다.

### 7.14 Core 예고 — 특성 효과를 공통 버프·디버프로 (2026-10-30 천명님 메시지, 사용자 전달, 코드 변경 없음)

- 예고 요지: 특성이 주는 효과를 공통 버프·디버프로 관리하도록 Core 구조를 바꾼다(설계·계획만, 코드 착수 전). 발동 연출 계약(`TraitActivationSnapshot`의 주체·대상·대상 칸·피해 원인)은 보존하고, 상태 조회·표시 연결은 일부 바뀐다. 중첩·유효 값 고르기·턴 차감·해제는 Core가 맡고 View는 타이머·규칙을 두지 않는다. 기획 변경: 샷건의 사냥꾼의 표식 → **약점 노출**(일정 턴 동안 받는 수치 피해 증가, 출처별 기간·가장 높은 값 하나, 같은 출처 재부여는 교체, 대상 진영 턴 끝 차감, HUD 깨진 방패), 사냥꾼(Hunter)의 표식은 별개, 재생 복구, 우선도 감소·이동 후 공격 비용 할인 제거(공격력 감소는 유지), Focus·고정 사격은 이번에 안 바뀜, 사령술·가시밭 보류. 공통 읽기 계약은 Core 완료 전에 중간 인계한다.
- 대조(이 묶음 코드):
  - 그대로 진행 가능: 발동 연출(특성 처리기·큐·추가 공격 `ChildAttack`·대신 맞기 `Interception`·유닛 행동), 흔적, 이동 원인, HUD 공통 틀(상태 외형 표 = 효과 종류마다 외형 하나 — 예고의 "같은 효과는 같은 아이콘"과 같은 꼴).
  - 이전 대상, 이미 한 곳에 모여 있음: Core 칸을 직접 읽는 곳은 상태 줄 표 `Display/Unit/CombatUnitStatuses.Rows` 하나(디버프 여섯·특성 효과 둘·가속 보유 상태). 공통 목록이 오면 이 표의 읽기만 바꾼다(TS6에 적어 둔 길).
  - 옛 표식 규칙에 묶인 곳: 사실 처리기 표식 갈래(`CombatFactEventHandler`, 여섯 칸 옮김)와 형식 표의 `CombatHuntersMarkEvent` 줄, 상태 외형 `UnitStatusVisual_HuntersMark`(이름·"공격 시 소모" 문구), 상태 종류 `UnitStatusKind.HuntersMark`.
  - 우선도 감소 제거에 묶인 곳: 위압 줄의 둘째 값 `PriorityPenalty`와 `UnitStatusVisual_Intimidation` 문구 "우선도 {1} 감소".
  - View가 규칙을 두는 곳 없음: 남은 턴은 Core 칸을 읽고, 방어구 부수기는 겹친 수만 쓰며(감소율을 곱하지 않음), 공격력 부여·보호막은 Core 합 칸을 읽는다. 표식이 "공격할 때마다 사라지는" 연출은 만들지 않았다(HUD는 Core 상태를 그대로 보임).
  - 다른 사람 영역에서 표식을 직접 읽는 곳(모두 천명님 줄): 유닛 카드 `GlobalSystem/UISystem/Screens/Global/UnitCardTraitContent.cs`(`DescribeHuntersMark`)·`UnitCardUI.cs`, 덱 편집 `StageMap/Deck/DeckEditDisplayModel.cs`·`DeckEditModelBuilder.cs`·`UISystem/Screens/Stage/DeckEditUI.cs`.
  - 보내기 전 우리 문서: [천명님 공유](../handoffs/2026-10-30-combat-trait-system-view-cheon-share.md) §4와 Core 전달 문서 §54의 요청 R1 `WithHuntersMark`는 표식이 공통 효과로 바뀌면 뜻이 없어진다(미전달). [특성·발동 시기 표](../viewDocs/combat-trait-activation-timing.md)의 표식 "공격 시 소모" 설명도 옛 규칙이다.
- 사용자 결정(10-30 "R1 빼기"): 미전달 요청 R1 `WithHuntersMark`를 [천명님 공유](../handoffs/2026-10-30-combat-trait-system-view-cheon-share.md)·Core 전달 문서(§5.62·§54)에서 뺐다. 공유 §4는 "표식 줄 — 공통 효과 계약을 기다림"으로 바꾸고 View가 특성별 상태를 직접 읽는 곳(옮길 대상)을 적었다. 설계 §10 TS2·남은 것 줄, 사실 처리기 표식 갈래 주석 한 줄(코드 동작 무변경)도 맞췄다.
