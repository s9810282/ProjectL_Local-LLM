# 전투 특성 발동 연출 — Task 계획

- 작성: 2026-10-29, View 작업(용근)
- 상태: **TA-a 완료(Ua 문제 없음 — 사용자, 기록 §6.1), TA-b 완료(Ub 문제 없음 — 사용자, 기록 §6.2), TA-c 완료(Uc 문제 없음 — 사용자, 기록 §6.3), TA-d 코드·에셋 완료(2026-10-29, 결정 Q-TD1 A, 컴파일 미확인, 새 시험 없음, Ud 대기, 기록 §6.4), TA-e 코드·에셋 완료(2026-10-29, 범위를 걸린 상태 모두로 넓힘 — 결정 Q1~Q4, 컴파일 미확인, 새 시험 없음, Ue 대기, 기록 §6.5), TA-f 문서·공유·묶음 끝 검토 완료(2026-10-29, 코드 변경 없음, 공유 미전달, 검토 약 9.46 — 높음 0·중간 2·낮음 9, 기록 §6.6), 검토 권장 반영 완료(2026-10-29 사용자 "권장대로 반영", 컴파일 미확인, Uf 대기, 기록 §6.7).** 설계 결정 Q-TA1~Q-TA7은 권장안(Q-TA3 B, 나머지 A), 이 계획에서 정한 것 P1~P11은 그대로(2026-10-29 사용자 "권장안으로 TA-a").
- 설계: [특성 발동 연출 설계](../specs/2026-10-29-combat-trait-activation-presentation-design.md)
- Core 요청: [특성 발동 사실 — Core 요청](../handoffs/2026-10-29-combat-trait-activation-core-request.md)(10-29 전달, 답 대기)
- 기준: HEAD `65f6944d`. View 경로는 `ViewSystem/Combat/` 아래, DB 경로는 `DataBaseSystem/Data/View/` 아래입니다.

## 0. 진행 규칙

- 전투 View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않습니다. 보고에는 "컴파일 미확인"을 적고, 컴파일·재생 확인은 사용자가 Unity에서 합니다.
- 기존 시험은 우리 변경으로 컴파일이 깨지는 곳만 최소로 맞춥니다. 다른 사람 시험이 실행에서 깨지면 고치기 전에 묻습니다.
- Phase마다 멈추고 보고합니다. 검토 에이전트는 묶음 끝(TA-f)에 한 번 돌립니다.
- 커밋은 사용자가 합니다. 새 `.cs`는 `.meta`와 함께 커밋해야 합니다.

## 1. 전체 순서

설계 Q-TA1 A(Core 답 전에 Core와 상관없는 것부터)를 전제로 한 순서입니다.

| Phase | 내용 | Core 답 필요 | 화면 변화 |
|---|---|---|---|
| TA-a | 공격 모양 값으로 바꾸기(리팩토링) | 없음 | 없음 |
| TA-b | 특성 외형 표의 발동 연출 칸·큐 규칙·검사 | 없음 | 없음 |
| TA-c | 특성 처리기(큐만 있는 줄) | 없음 | 줄을 채운 특성만 |
| TA-d | 대상 있는 추가 공격 재생 | 없음 | 줄을 채운 특성만 |
| TA-e | HUD 걸린 상태(버프·디버프) — 10-29 범위 넓힘 | 없음(공통 목록은 R5 보강) | 위압·방어 감소가 걸린 유닛(표식은 그대로) |
| TA-f | 문서·공유·묶음 끝 검토 | 없음 | — |
| TB | Core 답 반영(아래 §4) | 있음 | Core가 사실을 낸 특성 |

TA-a를 먼저 하는 까닭은 동작이 같은 리팩토링이라 따로 확인할 수 있고, TA-d가 그 위에 서기 때문입니다.

## 2. 계획에서 정한 것 (사용자 확인 대상)

| | 정한 것 | 까닭 |
|---|---|---|
| P1 | 공격 모양 값의 자리는 `Presentation/Attack/CombatAttackShape.cs`(새 파일) | 종류 전략·재생이 있는 폴더(폴더 배치 안내) |
| P2 | 포효의 정면 공격처럼 공격 결과가 없는 동작은 "모양 없음"(지금 `null` 공격 사건과 같은 뜻)으로 넘김 | `CombatRoarEventHandler.cs:73`이 지금 공격 사건 없이 부름 |
| P3 | 천명님 시험 `PullAndMarkPresentationTests.cs:365`의 `FireShot(new CombatAttackEvent(…), …)` 호출은 공격 모양을 만드는 한 줄로 맞춤(컴파일 맞춤, 확인 내용 그대로). Core 전달 문서 §5에 기록 | 다른 입구를 남기면 공격 재생 입구가 둘이 됨(Q-TA7) |
| P4 | 발동 연출 줄의 형식 이름은 `TraitActivationVisual`, 자리는 `Table/Trait/Item/`(특성 외형 표 옆) | 특성 외형 표의 칸이라 같은 폴더 |
| P5 | 큐만 있는 줄의 큐 입력은 지금 사실 사건 칸과 같은 `CombatVisualCueInput`(이펙트·흔들림) + 소리 없음 | 소리는 소리 표가 따로 맡는 규칙(이펙트 창구 확장 설계 EC3). 특성 소리는 TB에서 소리 표 갈래로 |
| P6 | 처리기는 큐만 있는 줄이면 곁들임 실행을 걸고 `false`를 돌려줘 틀이 이어 가게 함. 추가 공격 줄이 하나라도 있으면 `true` | 처리기 계약(`CombatEventHandler`) 그대로 |
| P7 | 형식 표 `CombatEffectsEvent` 줄의 주체는 발동 목록의 발동 유닛이 모두 같을 때만 그 유닛 | 서로 다른 유닛이면 한 주체로 막을 수 없음(설계 TA1) |
| P8 | 추가 공격의 공격 표시 덮어쓰기는 공격 표시 SO(`CombatAttackPresentationSO`) 참조 하나 | 지금 공격 묶음을 그대로 다시 씀(Q-TA3 B) |
| P9 | 처리기에 넘길 특성 외형 조회는 표시 표(`CombatDisplayTables`)가 이미 가진 것을 씀. Factory는 바꾸지 않는 것을 목표로 하고, 바꿔야 하면 그때 멈추고 알림 | 민형님 연결 줄을 늘리지 않음 |
| P10 | 위협 HUD 상태의 키 이름은 `"IntimidationDebuff"`, 표식 바로 뒤 | 표식(`"HuntersMarkDebuff"`)과 같은 꼴 |
| P11 | 연쇄 폭발(키 180)·행운의 탄환(키 170)은 특성 외형 에셋이 아직 없습니다. TA-d에서 두 에셋을 새로 만들어 표 끝에 등록합니다(이름·설명 초안, 아이콘 없음 — 충격 볼트 외형을 만든 때와 같은 꼴) | 줄이 없으면 발동 연출을 붙일 곳이 없음. 증강 화면도 이 줄을 요구함(Core 전달 문서 §51) |

## 3. Phase별 Task

### TA-a. 공격 모양 값으로 바꾸기 (리팩토링, 화면 변화 없음)

- Ta.1 `CombatAttackShape` 값을 만듭니다. 담는 것: 출처 사건, 공격자, 방향, 전달 칸, 영향 칸, 주 대상, 공격 그룹. 만드는 입구 둘(공격 사건에서 / 칸·방향·대상에서)을 둡니다. 두 번째 입구는 TA-d에서 처음 씁니다.
- Ta.2 종류 전략의 입력을 바꿉니다: `CombatAttackStyle.cs`(`TryBeginCodeMotion`·`FireShot`·근접/직선 공통), `CombatLinearStyle.cs`, `CombatMeleeStyle.cs`, `CombatMortarStyle.cs`, `ICombatAttackStyleContext.cs`(`LinearContactProgress`).
- Ta.3 재생과 Gate의 입력을 바꿉니다: `CombatAttackPlayback.cs`(`TryBeginMotion`·`FireShot`·접촉 진행률·경고), `CombatHitGate.cs`(`Begin`).
- Ta.4 조립의 입력을 바꿉니다: `Presentation/Pieces/CombatCodeMotionAttack.cs`·`CombatClipAttack.cs`.
- Ta.5 부르는 곳을 맞춥니다: `CombatAttackEventHandler.cs`(공격 사건에서 모양을 만듦), `CombatRoarEventHandler.cs:73`(모양 없음, P2), `CombatPieceRun.cs:150`(확인만).
- Ta.6 컴파일 맞춤: 천명님 `PullAndMarkPresentationTests.cs:365` 한 줄(P3), Core 전달 문서 §5에 기록.
- **Ua 확인(사용자):** 근접·직선·곡사 공격, 클립 공격, 투사체 착탄·관통, 포효, 끌어오기·표식 화면이 전과 같음. 콘솔에 새 경고 없음.

### TA-b. 발동 연출 값 (화면 변화 없음)

- Tb.1 줄 형식 `TraitActivationVisual`(P4): 발동 종류, 큐 입력(P5), 공격 표시 덮어쓰기(선택, P8), "추가 공격 줄" 여부.
  - "추가 공격 줄" 여부는 따로 칸을 두지 않고 발동 종류에서 정합니다(지금 값: `KillChainAttack`·`FollowUpAttack`). 판정 원본은 함수 하나로 둡니다.
- Tb.2 `TraitVisualData`에 줄 목록 칸 `activations`를 더하고, 읽기 함수 `TryGetActivation(kind, out …)`를 둡니다.
- Tb.3 큐 규칙에 `CombatCueEvent.TraitActivation`(큐만 있는 줄)을 더합니다. 시점은 `Start`, 자리는 발동 유닛 몸·대상 몸·칸입니다. 추가 공격 줄은 공격 표시 SO의 큐 규칙을 그대로 따릅니다.
- Tb.4 검사: 같은 발동 종류 두 줄, 정의되지 않은 종류, 큐 규칙 위반, 추가 공격이 아닌 줄에 덮어쓰기가 있으면 예외(부트 DB 생성 때).
- 에셋 변경 없음. 지금 특성 외형 에셋은 목록이 비어 그대로 통과합니다.
- **Ub 확인(사용자):** Boot에서 DB 생성 통과, 특성 외형 인스펙터에 새 목록이 보임.

### TA-c. 특성 처리기 (큐만 있는 줄)

- Tc.1 처리기 칸 `CombatEventHandlerSlot.Trait`와 형식 표 줄(주체 P7)을 바꿉니다. 처리기 표 `Create`에 인자 하나를 더합니다.
- Tc.2 `Playback/Handlers/CombatTraitEventHandler.cs`(새 파일): 발동 목록을 차례로 보고 줄을 찾아, 큐만 있는 줄은 곁들임 실행을 겁니다(P6).
- Tc.3 큐 자리 여럿(설계 TA4): 발동 유닛 몸·대상 몸·칸 자리를 만드는 함수를 `CombatCuePoints` 쪽에 더합니다. 대상 몸은 보인 사실로 서 있는 몸(`CombatShownFacts.StandingBodyOf`)입니다.
- Tc.4 사건 Presenter 조립에 처리기를 더합니다(P9). Factory를 바꿔야 하면 멈추고 알립니다.
- **Uc 확인(사용자):** 처치 연쇄(`TraitVisual` 처치 연쇄 줄, 종류 `KillChainAttack`)에 임시 이펙트 큐를 넣으면 처치 연쇄 발동 순간에 나옴. 줄을 비우면 전과 같음.

### TA-d. 대상 있는 추가 공격 재생

- Td.1 처리기가 추가 공격 줄을 만나면, 발동 사실(발동 유닛·대상)로 공격 모양을 만듭니다(Ta.1의 두 번째 입구). 대상 칸 = 전달 칸·영향 칸, 방향 = 발동 유닛에서 대상 쪽입니다.
- Td.2 공격 표시는 줄의 덮어쓰기, 없으면 그 유닛의 일반 공격 표시입니다. 지금 공격 처리기와 같은 조립(코드 모션 또는 클립)을 부릅니다.
- Td.3 사건 완료 = 맞는 순간(첫 해제)입니다. 발동 사실은 지금 장벽이라 뒤따르는 피해는 그때부터 시작합니다(설계 TA6). 출처 수명은 발동 사건을 출처로 엽니다.
- Td.4 대상이 없는 추가 공격(2연발, `FollowUpAttack` 대상 없음)은 건너뜁니다(TB에서 R7 뒤). 줄이 있어도 경고 한 번만 남깁니다.
- Td.5 연쇄 폭발·행운의 탄환 특성 외형 에셋 두 개를 만들고 표에 등록합니다(P11, `DataBaseSO/ViewGroup/TraitVisual/Item/`).
- **Ud 확인(사용자):** 처치 연쇄·연쇄 폭발·행운의 탄환 줄에 추가 공격을 켜면, 원래 공격의 맞는 순간 뒤에 발동 유닛이 다시 쏘고 대상이 그 맞는 순간에 피격됨. 줄을 비우면 전과 같음(대상 피격만).
  - 확인용 유닛: 처치 연쇄는 아키보(Akimbo)·메인. 연쇄 폭발·행운의 탄환은 강화로 얻는 특성이라 확인 스테이지에서 강화를 받아야 합니다(어려우면 처치 연쇄만으로 확인).

### TA-e. HUD 걸린 상태 (버프·디버프)

- ~~Te.1 `FieldUnitPresenter.RenderUnitHud`에 위협 디버프 상태를 표식 바로 뒤에 더합니다(P10). 아이콘은 위협 특성 외형 줄의 아이콘, 설명은 공격 감소율과 우선순위 감소입니다.~~ → **바뀜(2026-10-29 사용자 "위협만이 아니라 버프 혹은 디버프 아이콘이 많이 생길 텐데 다 대응되게").** 결정: Q1 A 새 상태 외형 표, Q2 A 순서만 구분(격파 목표 → 아이템 → 디버프 → 버프 → 보유 특성, `UnitHUD` 무변경), Q3 "디버프 셋"(위압·방어 감소·사냥꾼의 표식 — 표식 하드코딩을 표로), Q4 A Core 요청 R5 보강. 설계 TA9.
- Te.1 View DB 상태 외형 표: `Table/UnitStatus/` 아래 표 SO·종류 `UnitStatusKind`·구분 `UnitStatusPolarity`·Data·SO, 조회 `UnitStatusVisualTable`, 연결 줄 셋(`ViewTableKey`·`ViewDataBase`·`ViewDataBaseFactory`), 에셋(표 + 상태 셋), `ViewGroup.asset` 등록.
- Te.2 상태 줄 표 `Display/Unit/CombatUnitStatuses`(줄 = 종류 + 값 수 + 값 읽기), `CombatDisplayTables.UnitStatuses`, `FieldUnitPresenter.Configure` 끝 선택 인자 `statuses`, `RenderUnitHud`가 아이템 뒤에서 부름(표식 하드코딩 삭제), Factory 연결 줄 `CombatSceneDisplayFactory:176` 인자 하나.
- Te.3 Core 요청 문서 R5 보강·§9 Q14, Core 전달 문서 §49 덧붙임.
- **Ue 확인(사용자):** §6.5.

### TA-f. 문서·공유·묶음 끝 검토

- Tf.1 폴더 배치 안내에 새 파일 줄(`CombatAttackShape`·`CombatTraitEventHandler`·`TraitActivationVisual`)과 "특성 발동 연출은 특성 외형 표 줄" 규칙.
- Tf.2 공유(모두 미전달로 작성):
  - 천명님: 시험 한 줄(P3), 특성 외형 에셋에 칸이 생김(천명님이 만든 외형 에셋 있음), TA-e에서 걷은 표식 HUD 줄(`4bd6b574`)과 실행에서 깨지는 표식 시험 둘(Core 전달 문서 §49에 적음), R5 보강·Q14 전달.
  - 호진님: 증강 화면이 읽는 특성 외형 표에 칸이 생김(읽는 쪽 영향 없음). TA-e의 새 View DB 표(상태 외형 표)와 에셋 넣는 법.
  - 민형님: TA-e 연결 줄 넷(`ViewTableKey` 키 하나·`ViewDataBase` 속성 하나·`ViewDataBaseFactory` 생성 줄 하나·`CombatSceneDisplayFactory:176` 인자 하나).
- Tf.3 설계 문서 구현 메모, CLAUDE.md 현재 상태.
- Tf.4 검토 에이전트 하나(7기준).

## 4. Core 답 뒤 (TB, 답을 보고 계획 보충)

> **2026-10-29 대신함:** Core 답(천명님 `f848f1f57`)이 와서 TB-1~TB-6은 [특성 시스템 View 대응 설계](../specs/2026-10-29-combat-trait-system-view-design.md)(TS1~TS9, Phase TS-a~TS-g)로 옮겼다. 아래 표는 이력이다.

| | 할 일 | 필요한 Core 사실 |
|---|---|---|
| TB-1 | 형식 표 줄에 근원(공격 그룹)을 넣어 동시 묶음에 붙이고, 맞는 순간 대응이 발동 사실도 출처로 보게 함 | R4 |
| TB-2 | 2연발: 발동 사실의 방향·지정 칸으로 공격 모양 | R7 |
| TB-3 | 넉백·충격 볼트·끌어오기·표식·위협·도어 브리칭의 발동 큐(에셋 줄 채우기), 특성 소리 갈래(소리 표) | R1·R6 |
| TB-4 | 걸음 종류 "특성 자기 이동", 걸음 원인 읽기, 특성 줄의 걸음 모양 | R8 |
| TB-5 | 대신 맞기(두 유닛 실행·맞는 순간 대기·접촉 몸 자리). 따로 설계 | R9·R10·Core 규칙 |
| TB-6 | 이동 중 발동(③ 지형 생성·④ 경로 주변 공격) 확인 — 구조는 TA-c·TA-d로 이미 됨 | R2·R7·Core 훅 |

- Core가 새 사건 형식(Q2 B)으로 답하면 TA-c의 형식 표 줄을 그 형식으로 옮깁니다. 처리기·값은 그대로 씁니다.
- Core가 발동 종류를 공통 단계 값(Q3 B)으로 답하면 TA-b의 줄 열쇠를 (단계 값)으로 바꿉니다. 특성 키는 표 줄이 이미 가져서 다른 것은 그대로입니다.

## 5. 커밋 묶음 (사용자)

- TA-a: 새 `CombatAttackShape.cs` + `.meta`, 바뀐 Attack·Pieces·Handlers 파일, 천명님 시험 한 줄. 한 커밋(값과 호출이 맞물림).
- TA-b~TA-d: 새 `TraitActivationVisual.cs`·`CombatTraitEventHandler.cs` + `.meta`, 바뀐 특성 외형·큐 규칙·형식 표·처리기 표·Presenter·큐 자리, 새 외형 에셋 둘(+ `.meta`)과 표 에셋. 확인용으로 채운 에셋 줄은 남길지 그때 정합니다.
- TA-e: 새 DB 코드 다섯(`Table/UnitStatus/` 표 SO·종류·구분·Data·SO)·조회 `UnitStatusVisualTable`·View `CombatUnitStatuses`(+ `.meta`·폴더 `.meta`), 연결 줄 넷(`ViewTableKey`·`ViewDataBase`·`ViewDataBaseFactory`·`CombatSceneDisplayFactory:176`), `CombatDisplayTables`·`FieldUnitPresenter`, 새 에셋 폴더 `DataBaseSO/ViewGroup/UnitStatusVisual/`(표 + 상태 셋, + `.meta`)와 `ViewGroup.asset` 한 줄. **코드와 에셋은 한 커밋**(표 에셋이 없으면 DB 생성 실패, 상태 에셋이 없으면 전투 진입 실패).

## 6. 실행 기록

### 6.1 TA-a 코드 완료 (2026-10-29, 컴파일 미확인, 새 시험 없음, 화면 변화 없음)

사용자 "권장안으로 TA-a". 바꾼 것:

- 새 `Presentation/Attack/CombatAttackShape.cs`(+ `.meta`): 출처 사건·공격자·방향·타격 칸·영향 칸·주 대상·공격 그룹. 입구는 공격 사건에서 만드는 `Of(attack)`와 생성자(TA-d가 발동 사실에서 만듦) 둘입니다. 타격 칸 규칙(끌어오기면 도착 칸)은 지금처럼 `CombatAttackTarget.DeliveryCell` 한 곳입니다.
- 종류 전략: `CombatAttackStyle`(`TryBeginCodeMotion`·`FireShot`·근접/직선 공통 `CreateAnimation`), `CombatLinearStyle`, `CombatMeleeStyle`, `CombatMortarStyle`가 공격 사건 대신 모양을 받습니다. `ICombatAttackStyleContext.LinearContactProgress`도 모양을 받습니다.
- 재생: `CombatAttackPlayback`의 `TryBeginMotion`·`FireShot`·접촉 진행률·경고가 모양을 받습니다. `TryBeginMotion`은 모양의 공격자가 행위자와 다르면 예외입니다(새 검사 한 줄).
- Gate: `CombatHitGate`의 공격 사건 전용 `Begin(CombatAttackEvent, …)`는 쓰는 곳이 없어 걷었습니다(출처 사건 입구 하나만 남음).
- 방향: `CombatAttackFacing.For(kind, shape, mapper, origin)`.
- 조립: `CombatCodeMotionAttack.Compose`·`CombatClipAttack.ComposeMelee`·`ComposeRanged`가 모양을 받고, 출처는 `shape.Source`, 사건 소리 열쇠도 `shape.Source`입니다.
- 부르는 곳: `CombatAttackEventHandler`가 공격 사건에서 모양을 한 번 만들어 넘깁니다. 포효(`CombatRoarEventHandler.cs:73`)는 지금처럼 모양 없이(null, P2) 부르므로 바뀌지 않았습니다.
- 시험: 천명님 `PullAndMarkPresentationTests.cs:365` 한 줄(P3, 첫 인자를 `CombatAttackShape.Of(…)`로 감쌈). 기록은 Core 전달 문서 §5.60.

계획과 다른 점: 없음. `CombatPieceRun.cs:150`은 확인만 했습니다(출처 사건을 그대로 받아 바꿀 것 없음).

동작이 같은 까닭: 모양의 값은 모두 옛 코드가 공격 사건에서 그 자리에서 읽던 값과 같습니다(타격 칸 `DeliveryCell(e.Attack)`, 방향 `AttackDirection`, 영향 칸 `AffectedCells`, 주 대상 `PrimaryTargetId`, 경고의 공격자 `AttackerId`). 해제·Gate의 출처는 지금도 공격 사건 그대로입니다.

**Ua 확인(사용자, Unity):**

1. 컴파일 오류 없음.
2. 근접(무덤지기)·직선(스나이퍼 등)·곡사(선인장 대포·언데드 대포·발리스타) 공격이 전과 같이 날아가고 맞음.
3. 클립 공격 유닛(근접·원거리)의 방향 돌기·발사·맞음이 전과 같음.
4. 끌어오기 공격의 투사체가 도착 칸으로 감, 표식 공격도 전과 같음.
5. 보스 포효(정면 공격 모션)가 전과 같음.
6. 콘솔에 새 경고·예외 없음.
7. (시험을 돌린다면) `PullAndMarkPresentationTests.Pull_PreviewAndAttackTargetUseArrival`가 전과 같은 결과.

**Ua 문제 없음(2026-10-29 사용자).**

### 6.2 TA-b 코드 완료 (2026-10-29, 컴파일 미확인, 새 시험 없음, 에셋 변경 없음, 화면 변화 없음)

사용자 "Ua 문제 없음, TA-b". 바꾼 것:

- 새 `DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs`(+ `.meta`): 발동 연출 줄(직렬화 칸 셋 — 발동 종류, 큐 `CombatVisualCueInput`, 공격 표시 덮어쓰기 `CombatAttackPresentationSO`).
  - 읽기: `Kind`, `Cues`(읽을 때마다 새로 만듦 — 쓰는 쪽이 보관, 유닛 외형과 같은 꼴), `AttackPresentation`(비면 null), `IsFollowUpAttack`.
  - 추가 공격 줄 판정 원본은 `IsFollowUpAttackKind(kind)` 하나입니다(처치 연쇄 `KillChainAttack`·추가 공격 `FollowUpAttack`). TA-d 특성 처리기도 이 함수를 씁니다.
  - 검사 `Validate(owner)`: 발동 종류가 비었거나 정의되지 않음, 큐가 큐 칸 규칙에 맞지 않음, 추가 공격이 아닌 줄의 공격 표시 → 예외. 공격 표시는 만들어 보아 스스로 검사하게 합니다.
- `TraitVisualData`(같은 폴더 `TraitVisualDataSO.cs`): 줄 목록 칸 `activations`, 읽기 `TryGetActivation(kind, out …)`, 검사에 빈 줄·같은 발동 종류 두 줄 → 예외. 표(`TraitVisualTable`)가 이미 DB 생성 때 줄마다 `Validate()`를 불러, 잘못된 줄은 Boot DB 생성에서 멈춥니다.
- 큐 규칙(`CombatPresentationCueRules.cs`): 새 큐 칸 `CombatCueEvent.TraitActivation = 12`. 시점은 Start만, 이펙트 자리는 `ActorBody`·`SubjectBody`·`EventCell`. 흔들림도 Start만입니다. 소리 시점·소리 목록·플래시는 받지 않습니다(기존 규칙 그대로 거름).

계획과 다른 점:
- Tb.1이 적은 "추가 공격 줄 여부"는 따로 칸을 두지 않고 발동 종류에서 정했습니다(계획에 이미 적은 대로, 칸 없음).
- 표 검사 위치는 `TraitVisualData.Validate()`입니다. 계획은 "부트 DB 생성 때"라고만 적었고, 표가 이미 이 함수를 부릅니다.

에셋: 바꾸지 않았습니다. 지금 특성 외형 에셋은 새 칸이 없어 빈 목록으로 읽히고 검사를 그대로 통과합니다.

**Ub 확인(사용자, Unity):**

1. 컴파일 오류 없음.
2. Boot에서 DB 생성이 통과하고 전투가 전과 같이 시작함.
3. 특성 외형 에셋(예: `TraitVisual_KillChainField`) 인스펙터에 "Activations" 목록이 보이고, 줄을 더하면 발동 종류·큐(이펙트·흔들림)·공격 표시 칸이 보임.
4. (선택) 검사 확인: 처치 연쇄 줄에 이펙트 큐를 Start·ActorBody로 넣으면 통과, 시점을 Hit으로 바꾸면 Boot DB 생성이 큐 칸 이유와 함께 멈춤. 확인 뒤 줄은 지우거나 TA-c 확인용으로 남깁니다.

**Ub 문제 없음(2026-10-29 사용자).**

### 6.3 TA-c 코드 완료 (2026-10-29, 컴파일 미확인, 새 시험 없음, 에셋 변경 없음)

사용자 "Ub 문제 없음, TA-c". 바꾼 것(경로는 `ViewSystem/Combat/` 아래):

- Tc.1 처리기 칸·형식 표·처리기 표
  - `Playback/Ordering/CombatEventTypeTable.cs`: 처리기 칸 `CombatEventHandlerSlot.Trait`를 더했습니다. `CombatEffectsEvent` 줄을 사실 칸 → `Trait`으로 옮기고, 주체 함수 `TraitActivatorOf`를 붙였습니다(P7: 특성 발동의 발동 유닛이 모두 같으면 그 유닛, 다르거나 특성 발동이 없으면 비어 있음).
  - 피해 근원은 없어 동시 묶음 밖(장벽)은 그대로입니다.
  - `Playback/Handlers/CombatEventHandlerTable.cs`: `Create` 끝 인자 `trait`, 칸 갈래 한 줄.
- Tc.2 새 `Playback/Handlers/CombatTraitEventHandler.cs`(+ `.meta`): 사실 반영 전에 사건의 특성 발동 목록을 차례로 보고 발동마다 큐를 냅니다. 늘 `false`를 돌려줘 틀이 사실 반영·완료로 이어 갑니다(P6, 큐만 있는 줄은 사건을 막지 않음).
- 큐 값·내는 길
  - `Playback/Support/CombatCueSources.cs`: 생성자에 특성 외형 표, 새 답 `ForActivation(특성 키, 발동 종류)`(줄이 없으면 빈 묶음).
  - `CombatDisplayTables.cs`: 그 생성 줄에 `Traits`를 넘김.
  - `Playback/Support/CombatMomentCuePlayback.cs`: 새 입구 `PlayActivation(발동)`, (특성 키, 발동 종류)별 보관, 곁들임 실행을 거는 곳을 `PlayAccent` 하나로 묶음(순간 큐도 같은 함수).
- Tc.3 큐 자리 여럿: `Presentation/Pieces/CombatCuePoints.cs`
  - 주체 몸·칸 자리를 목록으로 들고, 새 입구 `ForActivation(발동 유닛, 발동 유닛 몸, 대상 몸 목록, 칸 목록)`을 더했습니다.
  - `CreatePiece`를 `CountOf(큐)` + `CreatePiece(큐, 순번)`으로 바꿨습니다. 자리를 못 찾으면 0개, 여럿이면 자리마다 하나입니다.
  - `Presentation/Pieces/CombatCueComposer.cs`의 이펙트 갈래가 자리 수만큼 항목을 넣습니다. 옛 사건(공격·피격·사망·아이템·토네이도·순간)은 자리가 하나 또는 없음이라 전과 같습니다.
- Tc.4 `Playback/CombatSceneEventPresenter.cs`: 처리기 표 조립 끝에 `trait: new CombatTraitEventHandler(_momentCues)`. **Factory는 바꾸지 않았습니다**(P9 — 특성 외형 표는 표시 표가 이미 갖고 있음). 민형님 공유 없음.

계획과 다른 점·정한 것:

- 처리기가 조립을 직접 만들지 않고 순간 큐 내는 곳(`CombatMomentCuePlayback`)의 새 입구를 부릅니다. 곁들임 실행을 거는 규칙(빈 조립 막기·시작 못 하면 예외)이 두 벌이 되지 않게 했습니다.
- 칸 자리(EventCell)는 지금 비어 있습니다. Core 발동 사실(`TraitActivationSnapshot`)에 칸이 없어서입니다. 칸 자리를 고른 이펙트는 아무것도 내지 않고, Core가 칸 목록을 담으면(요청 R3) TB에서 채웁니다.
- 대상 몸은 지금 하나입니다(Core `TargetId` 하나). 대상이 죽었거나 필드 밖이면 그 자리는 빠집니다(지금 몸 자리 규칙).
- 추가 공격 줄(처치 연쇄·추가 공격)도 지금은 자기 큐만 냅니다. 다시 치기는 TA-d입니다.
- 주체는 특성 발동 목록만 봅니다. 직군 발동(`Categories`)은 보이지 않아 주체에 넣지 않았습니다. 특성 발동이 없는 사건은 주체가 없어 전과 같습니다.

보고만:

- 작업 트리에 `TraitVisual_DoubleShot`·`_KillChainField`·`_Knockback.asset` 변경이 있습니다. 새 칸 `activations: []` 한 줄(과 `DoubleShot`의 문자열 다시 쓰기)뿐이라 Unity가 인스펙터에서 다시 저장한 것으로 보입니다. 값은 같습니다. 커밋에 넣어도 되고 버려도 됩니다.

**Uc 확인(사용자, Unity):**

1. 컴파일 오류 없음.
2. 처치 연쇄 특성 외형(`TraitVisual_KillChainField`)의 Activations에 줄 하나(종류 `KillChainAttack`, 이펙트 큐 Start·ActorBody 또는 SubjectBody)를 넣고, 그 특성을 가진 유닛으로 적을 처치하면 처치 연쇄 발동 순간에 이펙트가 나옴.
3. 흔들림 큐를 넣으면 같은 순간에 카메라가 흔들림(흔들림 수신기가 있는 맵).
4. 줄을 비우면 전과 같음(이펙트 없음, 재생 순서·시간 같음). → 바로잡음(검토 L4, §6.7): 이펙트는 같지만 재생 시점은 조금 바뀜.
5. 공격·피격·사망·아이템·토네이도·화약통/지뢰 폭발 이펙트가 전과 같음(큐 자리 바꿈의 회귀 확인).

**Uc 문제 없음(2026-10-29 사용자).**

### 6.4 TA-d 코드·에셋 완료 (2026-10-29, 컴파일 미확인, 새 시험 없음)

사용자 "Uc 문제 없음, TA-d". 착수 전에 확인한 Core 순서(대조 `FollowUpAttackExecutor.cs:82-87`·`KillChainAttackCalculator.cs:78-88`): 처치 연쇄와 대상 있는 추가 공격(연쇄 폭발·행운의 탄환)은 발동 사건이 그 타격의 표식·보유 방어·체력 사건 **앞**에 끼워져 있습니다. 대상(`TargetId`)은 캐릭터 또는 구조물이고, 2연발(`FollowUpAttack`, 같은 방향)은 대상이 없습니다.

**구현 중 찾은 빈틈과 결정(Q-TD1, 2026-10-29 사용자 "A"):** 출처 Gate와 행동 실행은 하나씩인데, 특성 발동 사건은 새 행동 시작이 아니라 원래 공격의 맞는 순간 뒤 꼬리와 겹쳐 시작할 수 있었습니다. 그때 다시 친 공격이 출처를 열면 예외입니다. → 형식 표에 "출처를 여는 사건" 질문 `OpensAttackSource`를 두고(Core 사실만: 발동 목록에 대상 있는 추가 공격이 있으면 예), 공격 Gate 규칙 1이 이 사건도 이전 출처의 전체 종료까지 기다리게 했습니다. 행동 시작이 아니므로 강조 끄기·동시 묶음·이동 경로·간격은 그대로입니다(설계 TA6 문장 유지). 대가: 추가 공격 줄이 없어도 처치 연쇄 발동은 원래 공격이 다 끝난 뒤 시작합니다.

바꾼 것(경로는 `ViewSystem/Combat/` 아래):

- 공격 Gate `Presentation/Attack/CombatHitGate.cs`: 완료 정책 새 값 `AnyRelease`(첫 모두 해제 신호에 대응 대상이 없어도 완료 — 특성 추가 공격은 Core 공격 그룹이 없어 뒤따르는 피해를 출처에 맺지 못함), 규칙 1에 `OpensAttackSource`.
- 형식 표 `Playback/Ordering/CombatEventTypeTable.cs`: 칸 `opensAttackSource`·질문 `OpensAttackSource`, 특성 발동 줄에 `HasTargetedFollowUp`(추가 공격 판정 원본은 `TraitActivationVisual.IsFollowUpAttackKind` 하나).
- 공격 모양 `Presentation/Attack/CombatAttackShape.cs`: 완료 정책 `Completion`(공격 사건 = `FirstRelease`), 새 입구 `ForFollowUp`(대상 칸 = 타격 칸·영향 칸, 방향 = 공격자 칸 → 대상 칸 네 방향, 완료 = `AnyRelease`), 생성자는 private로.
- 조립 둘 `Presentation/Pieces/CombatCodeMotionAttack.cs`·`CombatClipAttack.cs`: 출처 완료 정책을 `shape.Completion`에서 읽음(공격 사건은 전과 같음).
- 공격 재생 `Presentation/Attack/CombatAttackPlayback.cs`: `TryBeginMotion` 끝 선택 인자 `settings`(공격 표시 덮어쓰기), 판정 `HasCodeMotion(settings, facing)` 하나를 행위자 판이 부름.
- 새 `Playback/Support/CombatAttackStart.cs`(+ `.meta`): 공격 모양 하나를 코드 모션·클립 조립으로 시작하는 한 곳(공격 처리기에서 옮김, 계획 Td.2). 덮어쓰기는 클립 유닛이면 같은 계열(근접/원거리)이어야 하고 다르면 예외.
- 공격 처리기 `Playback/Handlers/CombatAttackEventHandler.cs`: 공격 사건만의 판단(이동만·실패) 뒤 `CombatAttackStart`를 부름. 동작 같음.
- 특성 처리기 `Playback/Handlers/CombatTraitEventHandler.cs`: 줄이 추가 공격 줄이고 대상이 있으면 `ForFollowUp` 모양으로 공격을 시작하고 true(사건 완료 = 맞는 순간), 대상 없으면 경고 한 번(Td.4), 한 사건의 둘째 추가 공격도 경고 한 번 뒤 건너뜀. 줄의 큐는 전처럼 곁들임 실행. 줄 값은 (특성, 종류)별로 보관.
- `Playback/Support/CombatCueSources.cs`: 줄을 돌려주는 `TryGetActivation`(큐 답 `ForActivation`도 이것을 씀).
- `Playback/Support/CombatShownFacts.cs`: 대상 칸 `TryGetCell`(캐릭터·구조물, `TryGetBody`와 같은 찾기 규칙).
- `Playback/CombatSceneEventPresenter.cs`: `CombatAttackStart`를 한 번 만들어 공격·특성 처리기에 넘김. Factory 무변경.
- Td.5 에셋: 새 `VDB/TraitVisual/Item/TraitVisual_LuckyBullet.asset`(170, "행운의 탄환")·`TraitVisual_ChainExplosion.asset`(180, "연쇄 폭발")(+ `.meta`, 아이콘 없음, 설명은 규칙 코드에서 쓴 초안), `TraitVisualDataTable.asset` 끝에 둘 등록.

계획과 다른 점·정한 것:

- 사건 완료 = 맞는 순간을 지키려고 완료 정책 값 `AnyRelease`를 더했습니다(지금 Gate는 대응 대상이 없으면 해제 신호에 완료를 알리지 않음). 공격 사건·포효의 동작은 그대로입니다.
- 방향 식은 Core `CardinalDirectionResolver.FromDelta`와 같은 식을 View에 둡니다(View가 Core Domain 어셈블리를 참조하지 않음). Core가 추가 공격 방향·칸을 내면(요청 R7) 바꿉니다.
- 다시 친 공격의 소리는 아직 없습니다(소리 표에 특성 갈래가 없음, 계획 P5 — TB).
- 계획 Td.3의 "출처 수명은 발동 사건을 출처로 엽니다"는 그대로입니다. 출처에 맺힌 대상이 없어 Gate 규칙 2(후속은 해제 뒤)는 이 출처에 쓰이지 않고, 피해 사건은 발동 사건(장벽)의 완료 뒤 차례로 시작합니다.
- 작업 트리의 `TraitVisual_DoubleShot`·`_KillChainField`·`_Knockback.asset` 변경(새 칸 `activations: []`)은 TA-c 때와 같은 Unity 다시 저장입니다.

**Ud 확인(사용자, Unity):**

1. 컴파일 오류 없음, Boot DB 생성 통과(새 특성 외형 둘 포함).
2. 처치 연쇄: `TraitVisual_KillChainField` Activations에 줄(종류 `KillChainAttack`, 공격 표시는 비움)을 넣고 처치하면, 원래 공격 연출이 끝난 뒤 발동 유닛이 새 대상 쪽을 보고 다시 공격하고, 대상은 그 맞는 순간에 피격됨.
3. 줄을 비우면 전처럼 대상 피격만 나옴(다만 원래 공격 연출이 끝난 뒤 — 결정 Q-TD1 A).
4. 공격 표시 덮어쓰기: 줄의 공격 표시에 다른 공격 묶음을 넣으면 그 동작·투사체로 다시 침(클립 유닛은 같은 계열만, 다르면 예외 메시지).
5. 2연발 줄(`TraitVisual_DoubleShot`, 종류 `FollowUpAttack`)을 넣으면 다시 치지 않고 콘솔 경고가 한 번 남음.
6. 일반 공격·포효·클립 공격·투사체가 전과 같음(공격 시작을 옮긴 회귀 확인).
7. (선택) 연쇄 폭발·행운의 탄환은 강화로 얻는 특성이라 확인 스테이지에서 강화를 받아야 합니다. 어려우면 처치 연쇄만으로 확인합니다.

### 6.5 TA-e 코드·에셋 완료 (2026-10-29, 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

사용자 "TA-e 진행하는데 단순히 위협 디버프가 아니라 버프 혹은 디버프 아이콘이 많이 생길텐데 다 대응되게 해야하는거 알지?" → 결정(사용자): Q1 A 새 상태 외형 표(View DB, 종류마다 아이콘·이름·설명 틀 `{0}`·구분), Q2 A 순서만 구분(격파 목표 → 아이템 → 디버프 → 버프 → 보유 특성, `UnitHUD` 무변경), Q3 "디버프 셋"(위압·방어 감소·사냥꾼의 표식 — 표식 하드코딩을 표로), Q4 A Core 요청 R5 보강. 설계 TA9를 이 내용으로 고쳤습니다.

바꾼 것:

- 새 View DB 표(경로는 `DataBaseSystem/` 아래, + `.meta`·폴더 `.meta`): `Data/View/Table/UnitStatus/UnitStatusVisualDataTableSO.cs`, `…/UnitStatus/Item/UnitStatusKind.cs`(None 0·Intimidation 1·DefenseReduction 2·HuntersMark 3)·`UnitStatusPolarity.cs`(Debuff 0·Buff 1)·`UnitStatusVisualData.cs`(설명 `Describe(values)` = "이름: 틀에 값", 틀이 비면 이름만, 값 수 확인 `AcceptsValueCount`)·`UnitStatusVisualDataSO.cs`, 조회 `Queries/ViewDataBase/ViewTable/UnitStatusVisualTable.cs`(종류 검사·중복 검사).
- 연결 줄 셋(민형님 영역, 연결만): `ServiceKeys/DBFactory/Group/ViewTableKey.cs` 키 `UnitStatusVisual`, `Queries/ViewDataBase/ViewDataBase.cs` 속성 하나(계약·구현·생성자), `Factory/DBFactory/Group/ViewDataBaseFactory.cs` 생성 줄 하나 + using.
- 에셋(경로는 `DataBaseSO/ViewGroup/` 아래, + `.meta`): 새 폴더 `UnitStatusVisual/`, 표 `UnitStatusVisualDataTable.asset`, 상태 셋 `Item/UnitStatusVisual_HuntersMark`·`_Intimidation`·`_DefenseReduction.asset`(아이콘 없음, 모두 디버프), `ViewGroup.asset` 끝에 표 한 줄.
  - 사냥꾼의 표식: 이름 "사냥꾼의 표식", 틀 "다른 아군의 일반·추가 공격 피해 {0}% 증가. 공격 시 소모됩니다." → 화면 문구가 전과 같음.
  - 위압: 이름 "위압"(특성 외형 표의 위협 특성 이름), 틀 "다음 공격까지 공격력 {0}% 감소. 우선도 {1} 감소."
  - 방어 감소: 이름 "방어 감소", 틀 "물리 방어력 {0}% 감소. 자기 턴이 {1}번 더 끝나면 풀립니다."
- 새 `ViewSystem/Combat/Display/Unit/CombatUnitStatuses.cs`(+ `.meta`): 상태 줄 표. 줄 = 종류 + 값 수 + 값 읽기(Core `CharacterDebuffState`), 순서 표식 → 위압 → 방어 감소(P10 "위협은 표식 바로 뒤"), `AddTo`가 디버프 줄들 → 버프 줄들. 만들 때 줄마다 외형이 있는지·틀이 그 값 수를 받는지 확인하고 아니면 예외(전투 진입 때). 칸 키 = 종류 + 구분(`HuntersMarkDebuff`는 전과 같음).
- `ViewSystem/Combat/CombatDisplayTables.cs`: 생성자 끝 인자 `statuses`(필수)·속성 `UnitStatuses`, `From`이 `query.UnitStatusVisualTableQuery`를 넘김.
- `ViewSystem/Combat/Display/Unit/FieldUnitPresenter.cs`: `Configure` 끝 선택 인자 `statuses`(없으면 걸린 상태 칸 없음 — 시험 조립용), `RenderUnitHud`의 표식 하드코딩(천명님 `4bd6b574`)을 걷고 아이템 다음에 `_statuses?.AddTo`.
- Factory 연결 줄 `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:176`: `units.Configure(…, tables.Items, tables.UnitStatuses)`.
- Core 요청 문서 R5 보강·§9 Q14(전달 뒤 추가, 미전달), 같은 문서 §4.6 6번 줄, Core 전달 문서 §49 덧붙임(R5 보강·걷은 천명님 줄·깨지는 시험).

계획과 다른 점·정한 것:

- 계획 P9 "Factory는 바꾸지 않는 것을 목표"와 다름: 새 표(결정 Q1 A)라 연결 줄 넷(키·조회 묶음·DB Factory·표시 Factory 인자)을 바꿨습니다. 모두 연결 줄이고 기능·계약은 그대로입니다. 민형님 공유는 TA-f(Tf.2).
- 검사 자리: DB는 줄 모양(종류·구분·중복)만, "보이는 종류마다 줄이 있나"와 "틀이 값 수를 받나"는 값을 읽는 상태 줄 표가 만들 때 확인합니다(값 차례의 원본이 줄 표라서). 그래서 틀 잘못은 Boot DB 생성이 아니라 Combat 진입 때 멈춥니다.
- 아이콘은 셋 다 비웠습니다(HUD 기본 아이콘). 지금 표식도 특성 외형 표의 표식 아이콘이 비어 있어 화면이 같습니다. 위압의 특성 아이콘은 쓰지 않았습니다(건 쪽의 특성 아이콘을 맞은 쪽에 띄우면 헷갈릴 수 있음) — 아트가 오면 에셋 칸만 채웁니다.
- 한계: 틀은 값을 늘 넣어서, 위압의 우선도 감소가 0(처음 맞음)이어도 "우선도 0 감소"가 보입니다. 문구는 에셋에서 바꿀 수 있고, 조건부 문구가 필요하면 그때 정합니다.

**천명님 시험 한 줄 고침(2026-10-29 사용자 "한 줄 고쳐", 컴파일 미확인):** 천명님 `Assets/Tests/EditMode/CoreIntegration/PullAndMarkPresentationTests.cs`는 `_units.Configure(_visuals, 1f, hudLayer: layer)`로 상태 줄 표를 넘기지 않아 표식 칸이 나오지 않았습니다(실행에서 `MarkApplyConsume_OneIndicator`, `CancelOrSceneExit_ReleasesMotionAndMarkVisuals(false)`가 실패할 것). `:67` 한 줄에 `statuses: …CombatDisplayTables.From(PresentationDatabase().ViewQuery).UnitStatuses`(완전 이름)를 넣었습니다. 같은 파일 `:419`가 이미 같은 꼴로 실제 DB 표를 쓰고, 확인 내용은 그대로입니다. `SetUp`마다 DB를 한 번 더 만들어 시험 시간만 늘어납니다. 천명님 공유는 TA-f에서 합니다.

**Ue 확인(사용자, Unity):**

1. 컴파일 오류 없음, Boot DB 생성 통과(새 표 `UnitStatusVisualDataTable` 포함).
2. Combat 진입 통과(상태 줄 표가 상태 셋의 외형·틀을 확인).
3. 사냥꾼의 표식(샷건 `Unit_ShotGun`의 공격): 맞은 유닛 HUD에 전과 같은 칸·문구, 다른 아군이 공격해 소모되면 사라짐.
4. 방어 감소: 독 가시 강화(`Enhancement_PoisonThorn`, 30%)를 받은 선인장 대포의 가시밭에 찔린 유닛 HUD에 "방어 감소" 칸(기본 아이콘)과 "방어 감소: 물리 방어력 30% 감소. 자기 턴이 N번 더 끝나면 풀립니다.", 풀리면 사라짐. 강화 없는 선인장 대포는 감소율 0이라 걸리지 않습니다.
5. 위압: 위압 특성 유닛(아이비, 지금 유닛 표 미등록 — 확인하려면 임시 등록 필요)이 공격하면 대상 HUD에 "위압" 칸, 대상이 공격하면 사라짐. 어려우면 건너뜁니다.
6. 순서: 아이템을 장착한 유닛에 디버프가 걸리면 아이템 → 디버프 → 보유 특성 순.
7. (선택) 상태 에셋의 틀에 그 종류가 주지 않는 칸(예: 표식에 `{1}`)을 넣으면 Combat 진입 때 `[CombatUnitStatuses] … 설명 틀이 값 1개로 만들어지지 않습니다` 오류로 멈춤 — 확인 뒤 되돌림.

### 6.6 TA-f 문서·공유·묶음 끝 검토 (2026-10-29, 코드 변경 없음)

사용자 "TA-f 진행". 한 것(공유는 모두 미전달):

- Tf.1 폴더 배치 안내: 갱신 줄, §1.1 표에 새 파일 넷(`Presentation/Attack/CombatAttackShape`·`Playback/Handlers/CombatTraitEventHandler`·`Playback/Support/CombatAttackStart`·`Display/Unit/CombatUnitStatuses`), §2 규칙 줄 둘(특성 발동 연출은 특성 외형 표의 발동 연출 줄 + 처리기 하나, 걸린 상태 HUD는 상태 외형 표 + 상태 줄 표). DB 쪽 파일(`TraitActivationVisual`·`UnitStatus` 표)은 안내 범위(`ViewSystem/Combat/`) 밖이라 규칙 줄에 이름만 적었습니다.
- Tf.2 공유:
  - 천명님: 새 [천명님 공유](../handoffs/2026-10-29-combat-trait-activation-cheon-share.md)(시험 두 줄 `:365`·`:67`, 표로 옮긴 표식 줄, 특성 외형 표 새 칸·새 외형 에셋 둘 문구 확인, View가 기대는 Core 순서, 따로 전달할 R5 보강·Q14). Core 전달 문서 §49에 이 문서를 가리키는 줄.
  - 호진님: 새 [호진님 공유](../handoffs/2026-10-29-combat-trait-activation-hojin-share.md)(특성 외형 표 새 칸 — 증강 화면 영향 없음, 새 외형 에셋 둘, 상태 외형 표의 파일·에셋·고치고 더하는 법).
  - 민형님: Factory 공유 문서 §40(연결 줄 넷). 겹친 강조 RC 작업의 GlobalSystem 공유는 그 뒤 절(§41~)에 들어갑니다.
- Tf.3 설계 §6 구현 메모, CLAUDE.md·메모리 현재 상태.
- Tf.4 검토 에이전트 하나(7기준, 읽기 전용).

**묶음 끝 검토(약 9.46, 높음 0·중간 2·낮음 9):** 일반 공격 길은 호출 단위로 같은 값을 읽음, 읽어서 보이는 컴파일 오류 없음(남은 시험의 바뀐 API 호출은 `:67`·`:365` 둘, `IViewQuery` 시험 가짜 없음), 에셋 GUID 맞음.

| | 무엇 | 기준 | 자리 | 권장 |
|---|---|---|---|---|
| M1 | 대상 있는 추가 공격 발동 사건은 주체가 발동 유닛 하나라 다른 유닛 연출을 기다리지 않고 시작합니다. 그런데 행동 실행(`ForAction`)을 열어, 히트 전 동안 조각 실행 재생기가 false를 돌려주므로 걸음·반응 재생기가 그 프레임들에 진행하지 않습니다. 처치한 A의 사망 반응·맞은 B의 피격 반응·넉백이 다시 친 공격의 맞는 순간까지 멈췄다 갑니다(지금 에셋은 줄이 비어 안 드러남 — 잠재 결함). **코드로 직접 확인함**(`CombatSubjectBlocking.cs:57`, `CombatPieceRunPlayer` Tick 반환, `CombatPresentationPlayers.Tick`의 멈춤) | ④ | `Playback/Players/CombatSubjectBlocking.cs` | `Blocks` 조건에 `CombatEventTypeTable.OpensAttackSource(item)` 한 줄 — 출처를 여는 사건도 행동 시작처럼 모든 유닛 연출을 기다림("행동 실행은 모든 유닛이 쉴 때만 열린다"를 되돌림) |
| M2 | 공격 표시 덮어쓰기는 유닛 단위 검사(`CombatUnitAnimationRules.ValidateUnit`)를 지나지 않아, 데이터 조합 잘못이 첫 발동 때 전투를 멈춥니다. ① 클립 유닛에 다른 계열(근접/원거리) 덮어쓰기 → `CombatAttackStart` 예외(**코드로 확인함**, `:102-104`, 계획 §6.4에 "다르면 예외"로 적은 동작). ② 코드 모션 유닛에 행위자 지점(포구·이펙트 지점) 큐가 있는 덮어쓰기 → 클립 채널 대여 예외(검토 주장, 직접 대조 안 함) | ⑤ | `TraitActivationVisual.cs:59-71`, `CombatAttackStart.cs` | 맞지 않는 조합이면 경고 한 번 뒤 그 유닛 자신의 공격 표시로 되돌림(전투를 멈추지 않음). 부트에서는 어떤 유닛이 그 특성을 가졌는지 View DB가 모름 |
| L1 | 대상 찾기 규칙 세 갈래: `CombatShownFacts.TryGetCell`이 `TryGetBody`의 찾기를 복사, 순간 큐의 대상 몸은 캐릭터만 찾음 → 대상이 구조물(행운의 탄환)이면 다시 치기는 되고 대상 몸 큐는 조용히 빠짐 | ① | `CombatShownFacts.cs:169`, `CombatMomentCuePlayback.cs:82` | 찾기 하나로 모으고 구조물 몸 중심도 거기서 |
| L2 | 상태 값 차례가 둘: 줄 표 원본 + SO 툴팁 문장 사본, 줄 안 `ValueCount`와 `ValuesOf` 배열 길이 따로 | ① | `UnitStatusVisualDataSO.cs:26-28`, `CombatUnitStatuses.cs:28-39` | 값 읽기를 함수 배열로, 개수는 길이에서. 툴팁은 짧게 |
| L3 | 같은 발동 줄을 처리기(`_rows`)·순간 큐 재생(`_activationCues`)이 따로 보관, 순간 큐 재생이 특성 사실 모양을 앎 | ② | `CombatTraitEventHandler.cs:41`, `CombatMomentCuePlayback.cs:42` | 처리기가 줄을 한 번 읽고 자리를 만들어 `PlayAccent(cues, points)` |
| L4 | 줄이 비어도 재생 시점이 바뀜: 발동 사건에 주체가 생겨 옛 장벽(모든 유닛 기다림)이 "발동 유닛만 기다림"이 됨. 설계 TA1 "값이 없으면 지금과 같음"·Uc 4 "재생 순서·시간 같음"은 이 점에서 틀림 | ④ | `CombatEventTypeTable.cs:335` | 의도(P7)라면 문서를 고침, 아니면 R4 전까지 주체를 비움 |
| L5 | "다시 치는 대상 있는 추가 공격" 조건이 형식 표 `HasTargetedFollowUp`와 처리기 두 곳. 처리기만 넓히면(R7 뒤 2연발) Gate `Begin`이 예외 | ④ | `CombatEventTypeTable.cs:351`, `CombatTraitEventHandler.cs:63-70` | 술어 하나를 두 곳이 부름 |
| L6 | 형식 표(`Ordering`)가 Core 사실 분류를 DB 직렬화 행의 정적 함수(`TraitActivationVisual.IsFollowUpAttackKind`)에 기댐 | ⑤ | `CombatEventTypeTable.cs:4` | 그대로(기록) 또는 View 규칙 쪽으로 |
| L7 | `TraitVisualData.GetData()`가 직렬화 객체를 그대로 돌려줌(원래 꼴, 가변 목록이 더해짐, 설계 §4가 이미 적음) | ⑤ | `TraitVisualDataSO.cs` | 그대로 |
| L8 | 운영 입구 `FieldUnitPresenter.Configure`의 `statuses`가 선택 인자 — Factory가 빠뜨리면 표식 포함 걸린 상태 칸이 조용히 사라짐 | ⑥ | `FieldUnitPresenter.cs` | 필수로(V2-g "운영 입구 필수"와 같은 규칙), 시험은 실제 표를 넘김(`:67`이 이미 넘김) |
| L9 | `CombatAttackShape.AttackGroup`을 저장만 하고 읽는 곳 없음 | ⑦ | `CombatAttackShape.cs:64` | 지우고 R4가 오면 더함 |

감점하지 않은 참고: 방향 식 사본(Core Domain 미참조, R7 뒤 걷음), "우선도 0 감소", 다시 친 공격의 투사체 비행·착탄 소리는 슬롯이 연결되면 남(공격 소리 시점만 없음), `TraitVisual_LuckyBullet.asset`만 CRLF·원문 UTF-8(동작 무관).

기준별: SSOT 9.5 · SOLID 9.75 · FW 10 · 독립성 8.75 · 캡슐화 8.75 · C#/Unity 9.75 · If/Tick 9.75. 권장(M1·M2·L1·L2·L3·L5·L8·L9 반영, L4 문서, L6·L7 그대로) 뒤 예상 약 9.9. **반영함(사용자 "권장대로 반영", §6.7).**

### 6.7 묶음 끝 검토 반영 (2026-10-29, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음)

반영한 것(경로는 `ViewSystem/Combat/` 아래, DB는 `DataBaseSystem/Data/View/` 아래):

- **M1** `Playback/Players/CombatSubjectBlocking.cs`: `Blocks`의 "모든 유닛 연출을 보는" 조건에 `CombatEventTypeTable.OpensAttackSource(item)` 한 줄. 출처를 여는 사건(대상 있는 추가 공격 발동)도 행동 사건처럼 모든 유닛 연출이 끝난 뒤 시작해, 다른 유닛의 반응·넉백이 다시 친 공격의 맞는 순간까지 멈추지 않습니다. 형식 표 `OpensAttackSource` 문서 주석에 같은 뜻을 더했습니다.
- **M2**
  - DB: `CombatPresentation/CombatPresentationCueRules.cs`에 판정 `UsesActorPoint(cues)`(이펙트 큐가 이펙트 지점·발사 지점 자리를 쓰는지). `Table/Trait/Item/TraitActivationVisual.cs` `Validate`가 공격 표시 덮어쓰기의 큐가 이 자리를 쓰면 예외(Boot DB 생성에서 멈춤 — 어느 유닛이 쓸지 몰라서). 툴팁·예외 문서도 고쳤습니다.
  - View: `Playback/Support/CombatAttackStart.cs` 클립 길에서 덮어쓰기 계열(근접/원거리)이 공격자와 다르면 예외 대신 (공격자, 덮어쓰기)마다 경고 한 번 뒤 공격자 자신의 공격 표시로 칩니다.
- **L1** `Playback/Support/CombatShownFacts.cs`: 대상 찾기 `TryFindTarget` 하나(캐릭터 → 구조물)를 `TryGetBody`·`TryGetCell`·새 `StandingBodyOf(EntityId)`가 함께 씁니다. 구조물 몸 자리는 새 `Display/Unit/FieldUnitPresenter.BodyCenterOf(StructureCombatFacts, mapper)`(몸 상자 가운데, 구조물 접촉 몸과 같은 배치)와 그 표시 회전입니다. 그래서 대상이 구조물(행운의 탄환)이어도 대상 몸 큐가 나옵니다. 구조물을 겨누는 칸은 지금처럼 앵커 칸입니다.
- **L2** `Display/Unit/CombatUnitStatuses.cs`: 줄은 `StatusRow.Of(종류, 상태 읽기, 칸 차례 값 읽기…)`로만 만들고 값 수는 값 읽기 수입니다. `UnitStatusVisualDataSO` 틀 툴팁은 칸 뜻 목록 대신 줄 표를 가리킵니다.
- **L3** `Playback/Handlers/CombatTraitEventHandler.cs`: 줄 값(큐·추가 공격 여부·덮어쓰기)을 (특성, 발동 종류)마다 한 번 읽어 보관하고, 큐 자리를 만들어 `CombatMomentCuePlayback.PlayAccent`(이제 internal)를 부릅니다. `CombatMomentCuePlayback.PlayActivation`·특성 큐 보관, `CombatCueSources.ForActivation`(쓰는 곳 없어짐)을 걷었습니다.
- **L5** `Playback/Ordering/CombatEventTypeTable.cs`: 발동 하나의 판정 `IsReplayedFollowUp(trait)`(대상 있음 + 추가 공격 종류)를 형식 표의 "출처를 여는 사건"과 처리기의 다시 치기가 함께 부릅니다.
- **L8** `FieldUnitPresenter.Configure(visuals, unitScale, statuses, traitVisuals = null, hudLayer = null, itemVisuals = null)`: 상태 줄 표가 셋째 필수 인자(null이면 예외), `RenderUnitHud`는 `?.` 없이 부름. Factory 연결 줄 `CombatSceneDisplayFactory.cs:176`을 `units.Configure(tables.Units, fieldConfig.FieldUnitScale, tables.UnitStatuses, tables.Traits, unitHudLayer, tables.Items)`로 맞췄습니다(민형님 공유 §40 고침). 천명님 시험 `PullAndMarkPresentationTests.cs:67`은 이름 있는 인자라 그대로 컴파일됩니다.
- **L9** `Presentation/Attack/CombatAttackShape.cs`: 읽는 곳 없는 `AttackGroup`과 생성자 인자를 걷었습니다(R4가 오면 더함). §6.1의 "공격 그룹" 칸은 이제 없습니다.
- **L4** 문서만: 설계 TA1 "값이 없으면 지금과 같음"에 재생 시점 바로잡음, §6.3 Uc 4의 "재생 순서·시간 같음"은 이 점에서 맞지 않음(아래). L6·L7은 그대로입니다.

L4 바로잡음: 옛 특성 발동 사건은 주체 없는 장벽이라 모든 유닛 연출을 기다렸습니다. 지금은 대상 있는 추가 공격이 없는 발동 사건(큐만·2연발)은 발동 유닛 연출만 기다려 다른 유닛 반응과 겹쳐 더 일찍 시작할 수 있고, 대상 있는 추가 공격이 든 사건은 M1 뒤 모든 유닛 연출과 이전 공격 출처 전체 종료를 기다립니다. §6.4의 "줄이 없어도 처치 연쇄가 조금 늦게 시작"은 출처 전체 종료 대기만 보던 문장입니다.

계획과 다른 점·정한 것:

- M2는 검토의 두 권장을 함께 넣었습니다(행위자 지점 자리는 DB에서 막고, 계열 다름은 경고 뒤 되돌림). 계열은 DB가 어느 유닛인지 몰라 거를 수 없어 실행 때 되돌립니다. §6.4의 "다르면 예외"는 바뀌었습니다.
- L8의 상태 줄 표 인자 자리는 셋째입니다(앞의 선택 인자 뒤에 필수 인자를 둘 수 없음).

공유 고침(모두 미전달): 천명님 공유 §4·§3(재생 시점·덮어쓰기 검사), 호진님 공유 §1.1·§2.4(덮어쓰기 검사·틀 툴팁), 민형님 Factory 공유 §40(인자 자리). 폴더 배치 안내 §2 규칙 줄 둘.

**Uf 확인(사용자, Unity — Ud·Ue와 함께):**

1. 컴파일 오류 없음, Boot DB 생성 통과.
2. (Ud 2와 같은 장면) 처치 연쇄 줄을 채우고 범위 공격으로 하나를 처치·다른 하나를 맞히면, 맞은 유닛의 피격 반응·처치된 유닛의 사망 반응이 끝난 뒤 다시 치기가 시작하고, 그동안 반응이 멈추지 않음.
3. 행운의 탄환(대상 = 건물·보스)에 SubjectBody 이펙트 큐를 넣으면 대상 건물 몸에서 나옴(전에는 빠짐).
4. 특성 줄의 공격 표시 덮어쓰기에 ActorEffectPoint·ActorMuzzle 이펙트 큐가 있는 공격 묶음을 넣으면 Boot DB 생성이 `[TraitActivationVisual] … 행위자 지점 자리…`로 멈춤 — 확인 뒤 되돌림.
5. 클립 유닛 줄에 다른 계열 덮어쓰기를 넣으면 전투가 멈추지 않고 경고 한 번 뒤 그 유닛의 공격 표시로 다시 침.
6. 사냥꾼의 표식·위압·방어 감소 HUD 칸이 Ue처럼 나옴(줄 표 꼴 바꿈의 회귀 확인).
7. (시험을 돌린다면) 천명님 `PullAndMarkPresentationTests` 표식 시험 둘이 통과.
