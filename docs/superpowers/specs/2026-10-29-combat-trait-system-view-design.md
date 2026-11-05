# 전투 특성 시스템(새 Core 계약) — View 대응 설계 (초안)

- 날짜: 2026-10-29
- 상태: **결정 완료(2026-10-30 사용자 "진행해" = Q-TS1~Q-TS8 권장 A), 구현 TS-a~TS-f5 완료(컴파일 확인 — 사용자, 재생 확인 일부 대기), 구현 메모 §10.** TS8(대신 맞기)은 [유닛 행동 설계](2026-10-30-combat-trait-unit-action-design.md)가 대신했다.
- 2026-10-30 Task 계획 [trait-system-view-plan](../plans/2026-10-30-combat-trait-system-view-plan.md) 작성(Q-TS 권장 A 전제). 계획 §0 검토가 이 설계를 보완했다 — 특히 R1: TS4의 "맞는 순간 대응은 따로 고칠 것 없음"은 틀림(추가 공격 시작이 출처가 되지 않아 피해가 공격 끝까지 늦음 → 공격 출처 값 일반화), R2: 공격 모양에 공격 그룹을 넣지 않음, P7: 거절 안내가 없어 더함. 본문은 그대로 두고, 바뀐 곳의 원본은 계획 §0·§1이다.
- 요청: 사용자 "6b0755b와 병합하고 생각하자" → "설계 초안 써줘".
- 기준 판: `6b0755be2`(천명님 특성 22종·계약 개편 `f848f1f57` 포함) 위에 이 브랜치의 커밋 전 작업.
- 원본 문서:
  - Core → View 인계: [trait-system-core-view-handoff](../handoffs/2026-10-28-trait-system-core-view-handoff.md)(F1, 미전달 상태로 들어옴). 이 설계는 그 §9 "View가 할 일"에 답한다.
  - 특성 규칙: [trait-system-design](2026-10-28-trait-system-design.md) §2·§5.
  - 우리 앞 설계: [특성 발동 연출 설계](2026-10-29-combat-trait-activation-presentation-design.md)(TA1~TA9). 이 설계가 그 §3의 "Core 답 뒤" 항목과 계획 §4의 TB-1~TB-6을 대신한다.

## 1. 들어온 Core 계약 (요약)

### 1.1 사실이 어디에 오나 (한 사실은 한 곳)

| 일 | 원본 | 다시 재생하지 말 것 |
|---|---|---|
| 보통 발동(수치·상태·수명) | `CombatEffectsEvent.Traits`, 열쇠는 (특성 키, 발동 종류) | `AttackResult.TraitActivations`는 요약이다 |
| 대신 맞기(가로채기) | 공격 시작 안: `CombatAttackEvent.Attack.TraitActivations`의 `Interception` 있는 항목(포효는 `CombatRoarEvent.Attack`) | 발동 사건에는 나오지 않는다(Core가 거부) |
| 추가 공격(처치 연쇄·2연발·연쇄 폭발·행운의 탄환) | 그 발동 사실의 `ChildAttack`(실제 맞은 목록·반응) | 새 공격 사건·비용은 없다 |
| 걸린 상태가 바뀐 순간 | 새 사건 `CombatTraitStateEvent`(바뀌기 전·후 유닛 사실, 공격 그룹, 사건 시점 공격력·방어력) | 결과 끝 Buffer 사실로 미리 그리지 않는다 |
| 이동 원인 | 걸음 사건 `CombatCharacterEvent.MovementCause`(스스로/밀림 + 특성 키), 구조물 사건도 같음 | — |
| 끌어오기 | `CombatPullEvent.Movement.Steps`(전용 레인) | `Path`·`FinalAnchor`는 `Steps`의 투영이다 |
| 흔적 지형(땅 다지기·먼지 연막) | `CombatMovementSourceEvent` + 새 원본 `TraitTrailFact`(만들 때 `Expected` = null) | 정적 지형 종류가 아니다 |

### 1.2 발동 사실의 새 칸

`TraitActivationSnapshot`(`C/Contracts/Snapshots/Traits/TraitActivationSnapshot.cs`):

| 칸 | 뜻 |
|---|---|
| `SourceUnitId` | 특성을 가진 유닛. 피해를 준 쪽(creditor)과 다를 수 있다 |
| `TargetIds` / `TargetId` | 대상 목록. `TargetId`는 대상이 **정확히 하나일 때만** 값이 있다(계산 속성) |
| `TargetCells` | 영향 칸 |
| `ResultOrigin` | 이 발동이 만든 결과의 근원(칸·가해자·공격 그룹). null이면 피해와 묶이지 않음 |
| `AttackDirection` / `TargetCell` | 추가 공격의 방향·지정 칸(대상이 없어도) |
| `Interception` / `ChildAttack` | 가로채기 / 추가 공격 결과. 둘은 함께 오지 않는다 |
| `Magnitude` | 종류마다 단위가 다르다(인계 §7). 0이어도 의미가 있을 수 있다 |

발동 종류는 일반 꼴로 늘었다: 상태 적용·소모·해제·만료·리셋, 보호막 부여·흡수·제거·리셋, 이동 바뀜, 흔적 생성·변화·제거, 넉백 적용·막음, 끌어오기, 도발 선택, 사거리·방어 변화, 즉사, 포효, 사냥 표적 선택, 방어구 파괴, 회복, 공격력 부여, 추가 공격, 처치 연쇄, 피해 넘기기(`TraitActivationKind.cs`).

### 1.3 유닛 상태

- `CharacterDebuffState` 칸이 여섯이 됐다: 위압·방어 감소·표식·**무장 해제·발목 꺾기·방어구 파괴**.
- 새 `CharacterTraitEffects`(`CharacterCombatFacts.TraitEffects`, null 아님): 공격력 부여 목록·보호막 목록·결속 방어 보너스·켜진 보호막 합·공격력 보너스.
- 새 특성 22종의 보유 상태(`TraitStatusSnapshot` 하위 형식): 사냥꾼(표적 ID·보상 수)·희생·가속(쌓은 공격력)·고정 사격(이번 턴 이동 여부) 등.
- 사망 원인 `CombatDamageCause`(숫자 피해 / 즉사), 거절 사유 `Disarmed`·`AnkleBroken`.

### 1.4 Core에서 꺼 둔 특성

집중·이동 후 공격 할인·가시밭·강령술·재생은 꺼져 있다(`DeferredCombatFeatures`). 그 발동 종류는 나오지 않는다.

### 1.5 우리 요청과의 대응

| 우리 요청 | Core |
|---|---|
| R1 모든 발동이 사실을 냄 | 됨(인계 §7 표, 37개 특성) |
| R2 결과 바로 앞 | 됨 |
| R3 대상·칸 여럿 | 됨(`TargetIds`·`TargetCells`) |
| R4 공격 그룹 | 됨(`ResultOrigin`) |
| R5 상태 칸 + 바뀐 순간 | 됨(형식 칸 + `CombatTraitStateEvent`). **R5 보강(공통 목록)·Q14는 반영 안 됨** — 형식 칸 그대로 |
| R6 전용 사건과 함께 | 됨(끌어오기·표식·포효는 전용 사건 유지) |
| R7 추가 공격 방향·칸 | 됨, 2연발 포함(`AttackDirection`·`TargetCell`·`ChildAttack`) |
| R8 이동 원인 | 됨. 단 **보통 스스로 이동은 원인 칸이 비어 있다**(옛 계약과 맞춤) |
| R9 반응은 행동 결과 안 | 됨(가로채기는 공격 결과 안) |
| R10 가로막기 사실 | 됨(`AttackInterceptionSnapshot`: 수호자·지켜진 유닛·실제로 맞은 유닛·전달 번호). 수호자의 나갔다 돌아오기는 View 몫, Core 위치는 그대로 |

앞 대화에서 "덧붙일까" 물었던 R7 확대·상태 바뀐 순간 사실은 Core가 이미 구현해 와서 더 요청할 것이 없다.

## 2. 지금 View에서 일어나는 일 (병합 직후, 코드 대조)

| | 일 | 화면 영향 | 원인 자리 |
|---|---|---|---|
| A1 | `CombatTraitStateEvent`가 사건 형식 표에 없어 경고 뒤 건너뜀 | 위압·무장 해제·보호막 등 상태가 결과 끝에야 HUD에 붙음 | `CombatEventTypeTable`(줄 없음), `OrderedCombatPresentation.cs:75-81` |
| A2 | 표식 사건이 디버프를 셋째 칸까지만 넘겨 만듦 | 표식이 걸리거나 소모될 때 무장 해제·발목 꺾기·방어구 파괴가 보인 사실에서 지워짐(지금 HUD 줄이 없어 안 드러남) | `CombatFactEventHandler.cs:50-51`(천명님 줄) |
| A3 | 추가 공격을 "발동 종류 70·80 + 대상 하나"로 판정하고, 방향은 칸 차이로 다시 계산 | 2연발·대상 여럿은 안 그림. `ChildAttack`을 안 읽음 | `TraitActivationVisual.IsFollowUpAttackKind`, `CombatEventTypeTable.cs:279-280·360-368`, `CombatAttackShape.ForFollowUp` |
| A4 | 추가 공격 판정이 연출 줄 유무를 안 봄 | 줄이 없는 처치 연쇄도 공격은 안 그리면서 모든 연출이 끝날 때까지 기다림 | `OpensAttackSource`(`CombatHitGate.cs:140`, `CombatSubjectBlocking.cs:62`) vs 처리기 `:67` |
| A5 | 발동 사건에 근원이 없다고 보고 묶음 밖 장벽으로 둠 | 공격 중간에 낀 발동(피해 변화·방어 무시 등)이 동시 피격을 차례 재생으로 끊음 | `CombatEventTypeTable.cs:135-137`, `CombatConcurrentGroups` |
| A6 | 이동 원인을 안 읽고 "결과 행위자 + 공격 전"으로 추론 | 지금은 맞게 나옴. 원인 사실이 있는데 사본 규칙이 남음 | `CombatEnemyPresentationPacing.cs:54-56` |
| A7 | 가로채기를 안 읽음. 투사체 접촉은 원래 대상 몸, 피격은 멀리 있는 수호자 | 지켜진 유닛 쪽으로 쏜 탄이 닿는 순간, 멀리 선 수호자가 맞고 체력이 깎임 | `CombatAttackShape.Of`·`CombatAttackPlayback.cs:309-315` |
| A8 | 흔적 원본을 안 그림(결과 끝 지형 그리기도 정적 지형만) | 땅 다지기·먼지 연막이 화면에 없음 | `CombatFactEventHandler.cs:75-76`, `TerrainViewSet.cs:208-214` |
| A9 | HUD 걸린 상태 줄은 셋(표식·위압·방어 감소) | 무장 해제·발목 꺾기·방어구 파괴·공격력 부여·보호막이 HUD에 없음 | `CombatUnitStatuses.cs:29-37` |
| A10 | 공격 결과의 발동 요약을 안 읽음 | 맞음(중복 재생 없음). 가로채기만 읽어야 함 | — |

컴파일이 깨질 곳은 찾지 못했다(디버프 생성자는 선택 인자, `TargetId`는 그대로 있음). 끌어오기는 `Path`를 읽는데 `Steps`의 투영이라 동작이 같다.

## 3. 설계

원칙: **View 코드는 "결과 모양"(공격·이동·상태·지형·가로막기)마다 하나, 특성마다는 표 줄만 둔다.** 특성 수가 늘어도 코드는 늘지 않고, 새 모양이 생길 때만 는다. 무엇을 할지는 발동 종류 이름이 아니라 사실에 있는 칸(`ChildAttack`·`Interception`·`ResultOrigin`)으로 정한다.

### TS1. 상태 사건 줄 (A1)

- 사건 형식 표에 `CombatTraitStateEvent` 줄을 더한다. 처리기 칸은 사실(Facts), 주체는 `Final`의 유닛, 근원은 `Origin`(있을 때)이다.
- 사실 반영은 `Final`을 보인 사실에 넣는 것 하나다. 그 순간 HUD가 다시 그려진다.
- 근원이 있으면 같은 공격 그룹의 피격과 같은 묶음에 든다(보호막 흡수 → 체력 사건이 같은 순간에 이어짐). 근원이 없으면(턴 끝 만료·스테이지 정리) 지금처럼 장벽이다.
- 사건 시점 공격력·방어력(`FinalStats`)은 지금 그것을 읽는 화면이 없어 보관하지 않는다(Q-TS8).

### TS2. 표식 사건의 디버프 칸 보존 (A2)

- 표식 사건 반영 때 지금 디버프 여섯 칸을 모두 옮기고 표식만 바꾼다.
- View가 칸 목록을 아는 것은 Core가 칸을 늘리면 다시 깨지는 사본이다. 그래서 Core에 `CharacterDebuffState.WithHuntersMark(...)` 한 함수를 요청하고, 오면 그 한 줄로 바꾼다(Q-TS3). Core는 이미 `WithRestrictions`·`WithArmorBreak`를 같은 꼴로 갖고 있다.

### TS3. 발동을 같은 순간의 피격 묶음에 붙이기 (A5, 옛 TB-1)

- 발동 사건의 근원 = 그 안 발동들의 `ResultOrigin`(공격 그룹 > 0, 가해자 있음). 모두 같은 열쇠일 때만 근원으로 쓰고, 섞이면 지금처럼 장벽이다.
- 주체 규칙(P1):
  - 추가 공격(`ChildAttack` 있음) → 발동 유닛(쏘는 쪽).
  - 그 밖에 대상이 하나 → 그 대상(피해 변화·방어 무시·상태 적용은 맞는 쪽에 붙는 일이라 그 대상의 체력 사건과 같은 주체가 되어 차례로 이어짐).
  - 대상이 없거나 여럿 → 발동 유닛.
- 결과: 근원이 있는 발동은 공격의 맞는 순간 뒤 같은 묶음에서 나온다(맞는 순간 대응 `CombatHitFollowUps`가 근원으로 사건을 고르므로 따로 고칠 것 없음 — 계획에서 확인). 공격 사건과 피격 사이에 낀 발동이 동시 피격을 끊지 않는다.

### TS4. 추가 공격은 `ChildAttack`으로 (A3·A4, 옛 TB-2)

- 판정: 발동에 `ChildAttack`이 있으면 추가 공격이다. 발동 종류 목록(`IsFollowUpAttackKind`)과 "대상 하나" 조건, 칸 차이로 방향 구하기를 걷는다.
  - 걷는 것: `CombatEventTypeTable.IsReplayedFollowUp`·`HasTargetedFollowUp`, `TraitActivationVisual.IsFollowUpAttackKind`(Q-TS2), `CombatAttackShape.ForFollowUp`의 방향 계산, 추가 공격용 `CombatShownFacts.TryGetCell` 사용, 맞는 순간 완료 정책 `AnyRelease`(다른 쓰는 곳이 없으면).
- 공격 모양은 사실에서 만든다: 공격자 = `SourceUnitId`, 방향 = `AttackDirection`, 지정 칸 = `TargetCell`, 영향 칸 = `TargetCells`, 주 대상 = `TargetId`, 공격 그룹 = `ResultOrigin.AttackGroup`.
  - `CombatAttackShape`에 공격 그룹을 다시 넣는다(TA-f 검토 L9에서 "Core 그룹이 오면 더함"으로 걷었던 칸).
- 맞는 순간: 추가 공격도 보통 공격처럼 **첫 해제에 사건 완료**(`FirstRelease`)이고, 맞는 대상은 그 공격 그룹으로 뒤따르는 체력 사건이 정한다(지금 공격 사건 길과 같은 장치). 그래서 추가 공격만 다른 완료 정책이 필요 없다.
- 2연발: 대상이 없어도 방향·칸이 있어 그린다. 맞은 목록이 비면(빈 탄) 쏘고 맞는 순간 없이 끝난다(P9).
- 연출 줄이 없어도 추가 공격은 그 유닛의 일반 공격 표시로 그린다(Q-TS1). 줄은 큐와 공격 표시 덮어쓰기만 더한다. A4가 같이 풀린다.

### TS5. 이동 원인을 사실에서 읽기 (A6, 옛 TB-4 일부)

- 걸음 종류 판정 한 곳(`CombatMovementPresentation.KindOf`)이 `MovementCause`를 읽는다: 스스로 → Voluntary, 밀림 → Forced.
- 원인 칸이 비면 Core 계약상 스스로 이동이다(보통 이동·뛰어넘기). 이렇게 하면 추론 규칙(`_movementActor`·`_attackStarted`)을 걷는다(Q-TS4).
  - 조건: 밀림 걸음을 내는 Core 자리가 모두 원인을 채우는지 계획에서 확인한다(넉백·충격 볼트·공격 강제 이동·환경 이동은 채움을 확인함, 끌어오기는 걸음 사건 없음).
- 넉백과 충격 볼트는 원인의 특성 키로 갈리지만 걸음 모양은 같다(밀림). 키는 발동 큐가 이미 구분한다.
- "특성 자기 이동"(돌진 등 옛 TB-4의 셋째 종류)은 지금 그런 특성이 없어 만들지 않는다. 뛰어넘기는 스스로 이동 모양 그대로이고, 특별한 칸은 `MovementChanged` 발동의 `TargetCells`가 알려 준다(큐로 표시 가능).

### TS6. HUD 걸린 상태 넓히기 (A9)

TA9 구조(상태 외형 표 `UnitStatusVisual` + 상태 줄 표 `CombatUnitStatuses`)에 줄을 더한다. 코드는 줄 표의 줄과 열거 값뿐이다.

| 새 종류 | 구분 | 읽는 사실 | 틀에 넣는 값 |
|---|---|---|---|
| Disarm | 디버프 | `Debuffs.Disarm` | 남은 턴 끝 수 |
| AnkleBreaker | 디버프 | `Debuffs.AnkleBreaker` | 남은 턴 끝 수 |
| ArmorBreak | 디버프 | `Debuffs.ArmorBreak` | 겹친 수 |
| AttackBonus | 버프 | `TraitEffects.AttackBonus`(부여 합) | 공격력 |
| Shield | 버프 | `TraitEffects.ActiveShieldTotal` | 보호막 |

- 방어구 파괴의 합친 감소율은 Core가 칸으로 주지 않아, 띄우려면 View가 곱을 계산해야 한다(규칙 사본). 그래서 "겹친 수"만 쓰고, 실제 방어력은 Core가 주는 유효 방어력에 맡긴다(P8).
- 사냥꾼 표적(사냥꾼의 `TargetId`가 가리키는 적)을 그 적 HUD에 띄우는 일은 다른 유닛의 사실을 읽어야 해서 지금 줄 모양(유닛 하나의 사실)에 안 맞는다(Q-TS6).

### TS7. 흔적 지형 표시 (A8)

- 새 표시 부품 `Display/Map/FieldTrailPresenter`(원본 ID마다 인스턴스, 지형·오브젝트 표시와 같은 꼴):
  - 사건 시점: 이동 원본 사건의 `Final`이 `TraitTrailFact`면 — `Expected`가 null이면 만들고, 있으면 같은 ID를 바꾸고, `Active`가 거짓이면 지운다. 사실 처리기 갈래 하나.
  - 결과 끝: Buffer 환경 원본 가운데 플레이어에게 보이는(`KnownTo`) `TraitTrailFact`로 다시 맞춘다. Buffer가 흔적 원본을 싣는지는 계획에서 확인한다(지금 결과 끝 지형 그리기는 전투 시작 때 정한 지형 원본 ID 목록만 본다).
  - 한 칸에 흔적 여럿·바닥 지형 위에 겹친다. 바닥 지형은 건드리지 않는다.
- 외형 값 자리: 특성 외형 표 줄에 흔적 칸(프리팹·높이 보정)을 더한다(Q-TS5). 특성 하나의 표현이 한 줄에 모인다(TA2와 같은 이유).
- 남은 턴 글자·호버 설명은 이번 범위 밖으로 둔다(P10).

### TS8. 대신 맞기 (A7, 옛 TB-5)

> **2026-10-30 대신함:** [유닛 행동 설계](2026-10-30-combat-trait-unit-action-design.md)가 이 절을 "발동 줄의 세 번째 형태 = 유닛 행동"으로 넓혀 다시 정한다(대신 맞기는 그 공격 반응 입구). 아래는 옛 글이다.

흐름(요청 문서 §4.3와 같음):

1. 공격 처리기가 공격을 시작할 때 공격 결과의 발동 목록에서 `Interception` 있는 항목을 읽는다(포효는 그 공격, 추가 공격은 `ChildAttack.Reactions`).
2. 수호자 실행을 함께 시작한다: 지켜진 유닛 앞(공격자 쪽으로 일정 거리)으로 나가기.
3. 공격의 맞는 순간은 모든 수호자가 도착할 때까지 기다린다. 투사체가 닿는 몸은 수호자가 지금 서 있는 화면 자리다.
4. 수호자의 피격·사망은 그 자리에서 낸다(사건 순서는 Core 그대로).
5. 공격 출처가 끝나면 수호자가 제자리로 돌아온다(죽었으면 돌아오지 않음).

지금 구조에 없는 것 넷(계획의 한 Phase에서 만든다):

- 한 사건에서 두 유닛 실행(공격자 + 수호자). 지금 조립은 주체 하나다(`CombatComposition.ForUnit`).
- 맞는 순간 Gate의 "준비 조건"(수호자 도착).
- 접촉 몸 자리 바꾸기(원래 대상 → 수호자 화면 자리). 지금은 주 대상 몸이다.
- 자세 대여 위에서의 피격 반응(나간 자리에서 맞기), 그리고 돌아오기 시점.

- 값(나가기·돌아오기 시간, 서는 거리)은 희생 특성 외형 줄의 피해 넘기기(`DamageRedirected`) 발동 줄에 둔다.
- Core 위치는 바뀌지 않고, 앞 칸의 지뢰·가시도 발동하지 않는다(인계 §4). 넉백은 기록된 이동 대상을 따른다.

### TS9. 그 밖 (바꾸지 않거나 확인만)

- 공격 결과의 발동 요약(`AttackResult.TraitActivations`)은 가로채기 항목 말고는 읽지 않는다(P2). 다시 재생하면 발동 사건과 겹친다.
- 끌어오기는 `Path` 그대로 둔다(P3).
- 즉사(마무리 사격)는 사망 연출 그대로이고, 앞의 즉사 발동 큐로 구분한다(P4).
- 특성 발동 소리는 이번 범위 밖이다(P5).
- 경험치 사건은 지금도 형식 표에 없다(P11, 범위 밖).
- 새 거절 사유(무장 해제·발목 꺾기)의 안내 문구가 나오는지 계획에서 확인한다(P7).
- 발동 연출 에셋 줄(새 특성 22종의 큐)은 코드 밖이다. 어떤 특성이 어떤 종류를 내는지는 인계 §7 표가 원본이고, 계획 문서에 에셋 작업용 목록으로 옮긴다(P6).

## 4. 바뀌는 곳 (예상)

| 묶음 | 파일 | 누구 줄 |
|---|---|---|
| TS1 | `Playback/Ordering/CombatEventTypeTable.cs`, `Playback/Handlers/CombatFactEventHandler.cs` | 우리 |
| TS2 | `CombatFactEventHandler.cs:50-51` | 천명님(`4bd6b574`) → 공유 |
| TS3 | `CombatEventTypeTable.cs`(발동 줄 근원·주체), `CombatEventSubjects`(필요하면) | 우리 |
| TS4 | `CombatTraitEventHandler.cs`, `CombatAttackShape.cs`, `CombatAttackStart.cs`, `CombatHitGate.cs`, `CombatHitFollowUps.cs`, `TraitActivationVisual.cs` | 우리 |
| TS5 | `CombatMovementPresentation.cs`, `CombatEnemyPresentationPacing.cs` | 우리·천명님 섞임 — 계획에서 blame 확인 |
| TS6 | `UnitStatusKind.cs`, `CombatUnitStatuses.cs`, 상태 외형 에셋 다섯 | 우리 → 호진님 공유(표 넣는 법) |
| TS7 | 새 `Display/Map/FieldTrailPresenter.cs`, `TraitVisualData`(흔적 칸), 사실 처리기, 표시 조립(Factory 한 줄 예상) | 우리 → 민형님(Factory)·호진님(표 칸) 공유 |
| TS8 | 공격 처리기·공격 재생·Gate·조립(두 유닛)·유닛 표시 창구 | 우리 |

다른 세션(겹친 강조 RC-d)이 `Display/Field/`를 고치는 중이다. 이 설계는 그 폴더를 건드리지 않는다.

## 5. 원칙 점검 (7기준, 예상)

| 기준 | 지금(병합 직후) | 설계 뒤 |
|---|---|---|
| SSOT | 추가 공격 판정이 Core 규칙의 사본(종류 목록), 이동 종류도 추론 사본, 표식이 디버프 칸 목록 사본 | 판정을 사실 칸에서 읽음. 표식 칸 사본은 Core 함수가 오면 사라짐 |
| SOLID | 특성 처리기가 "종류 → 동작"을 앎 | 처리기는 큐 + `ChildAttack`이면 공격 시작. 새 특성은 표 줄(OCP) |
| FW | 무변경 | 무변경(Factory 연결 한 줄 예상, TS7) |
| 독립성 | 상태가 결과 끝에 몰려 반영 | TS1~TS3은 작은 독립 묶음, TS8은 마지막에 따로 |
| 캡슐화 | — | 흔적·상태 값은 DB 입력 계층 검사 |
| C#/Unity 권장 구조 | — | 새 MonoBehaviour 없음(흔적은 프리팹 인스턴스만) |
| 무의미한 If/Tick | 연출 줄이 없는 추가 공격이 쓸데없이 기다림(A4) | 사라짐. 새 Tick 없음 |

예상(알려진 문제 기준): 지금 약 7.8 → TS1~TS7 뒤 약 9.1 → TS8까지 약 9.3. 남는 낮음 예상: 방어구 파괴 합친 감소율을 View가 계산하면 사본(P8로 피함), 흔적 남은 턴 미표시.

## 6. 정할 것

| | 질문 | A | B | 권장 |
|---|---|---|---|---|
| Q-TS1 | 연출 줄이 없는 추가 공격 | 늘 그림(그 유닛의 일반 공격 표시) | 줄이 있을 때만 그림(지금) | **A** — `ChildAttack`은 실제로 일어난 공격이고, A4(쓸데없는 기다림)도 같이 풀림 |
| Q-TS2 | 공격 표시 덮어쓰기 칸 | 어느 발동 줄에나 둘 수 있게 하고 추가 공격 종류 목록을 걷음 | 지금처럼 처치 연쇄·추가 공격 줄에만 | **A** — Core가 추가 공격을 내는 종류를 늘려도 View 코드가 안 바뀜. 공격을 안 내는 줄의 덮어쓰기는 쓰이지 않을 뿐 |
| Q-TS3 | 표식 사건의 디버프 칸 | View가 여섯 칸을 옮기고, Core에 `WithHuntersMark` 요청 | View가 여섯 칸을 옮기기만 | **A** — Core가 칸을 늘려도 다시 안 깨짐 |
| Q-TS4 | 이동 종류 판정 | 원인 칸 우선, 원인 없음 = 스스로 이동(Core 계약), 추론 규칙 걷기 | 원인 칸 우선, 원인 없을 때 지금 추론 유지 | **A** — 계획에서 밀림 걸음이 모두 원인을 채우는지 확인한 뒤. 하나라도 비면 B |
| Q-TS5 | 흔적 외형 자리 | 특성 외형 표 줄의 흔적 칸 | 새 흔적 외형 표 | **A** — 특성 하나의 표현이 한 줄에 모임 |
| Q-TS6 | HUD 새 상태 범위 | 디버프 셋 + 버프 둘(§TS6 표) | 디버프 셋만 | **A**. 사냥 표적 표시는 다음 묶음(다른 유닛 사실을 읽는 줄 모양이 따로 필요) |
| Q-TS7 | 대신 맞기를 이번 묶음에 | 넣음(마지막 Phase, 계획에서 세부 정함) | 이번에는 수호자 큐만, 나가기·돌아오기는 따로 설계 | **A** — 사실은 다 왔고, 없으면 멀리 선 수호자가 맞는 그림이 남음. 대신 구조 추가가 가장 커서 맨 뒤에 둠 |
| Q-TS8 | 사건 시점 공격력·방어력 보관 | 읽는 화면이 생길 때까지 두지 않음 | 보인 사실 옆에 같이 둠 | **A** — 지금 HUD·정보창이 숫자를 안 보임(정보창은 결과 끝 Buffer 값). 안 읽는 칸은 만들지 않음 |

## 7. 설계에서 정한 것 (확인 대상)

- P1 발동 사건의 주체 규칙(§TS3): 추가 공격 → 발동 유닛, 대상 하나 → 대상, 그 밖 → 발동 유닛. 사건 하나에 발동이 여럿이면 모두 같은 대상일 때만 그 대상이다. 계획에서 피해 변화·상태 적용 발동이 실제로 대상 하나로 오는지(Core `DamageTraits`는 한 사건에 여러 기여를 담음) 확인한다.
- P2 공격 결과의 발동 요약은 가로채기 항목만 읽는다.
- P3 끌어오기는 `Path` 그대로(`Steps`의 투영과 같음).
- P4 즉사는 사망 연출 그대로.
- P5 특성 발동 소리는 범위 밖(다음 묶음, 사운드 작업자와 슬롯).
- P6 새 특성 22종의 발동 큐 에셋 줄은 코드 밖(사용자·기획이 채움). 계획에 "특성 → 내는 발동 종류" 목록을 둔다.
- P7 새 거절 사유 안내 문구는 계획에서 확인만.
- P8 방어구 파괴는 겹친 수만 HUD 값으로 쓴다(합친 감소율은 View 계산이 되므로).
- P9 빈 추가 공격(2연발이 아무도 안 맞음)은 공격을 그리고 맞는 순간 없이 끝난다.
- P10 흔적의 남은 턴 글자·호버 설명은 범위 밖.
- P11 경험치 사건(형식 표에 없음)은 범위 밖.
- Core 인계 §10의 View 시험 항목은 우리 규칙(View 작업은 시험 코드를 쓰지 않음)에 따라 Unity 확인 목록으로 대신한다.

## 8. Phase (안)

| Phase | 내용 | 화면 |
|---|---|---|
| TS-a | TS1 상태 사건 줄 + TS2 표식 칸 보존 + TS3 발동 근원·주체 | 상태가 사건 순간 HUD에, 공격 중 발동이 동시 피격을 안 끊음 |
| TS-b | TS4 추가 공격을 `ChildAttack`으로(하드코딩 걷기, 2연발) | 2연발·줄 없는 처치 연쇄가 그려짐 |
| TS-c | TS5 이동 원인 | 같음(추론 걷기) |
| TS-d | TS6 HUD 새 상태 다섯 + 에셋 | HUD에 무장 해제·발목·방어구 파괴·공격력·보호막 |
| TS-e | TS7 흔적 지형 | 땅 다지기·먼지 연막이 보임 |
| TS-f | TS8 대신 맞기 | 수호자가 나가서 맞고 돌아옴 |
| TS-g | 문서·공유(천명님·호진님·민형님)·묶음 끝 검토 | — |

TS-a~TS-c는 서로 파일이 겹쳐(`CombatEventTypeTable`) 차례로 한다. TS-d·TS-e는 따로 할 수 있다.

## 9. 계획 검토로 바뀐 것 (2026-10-30)

[Task 계획](../plans/2026-10-30-combat-trait-system-view-plan.md) §0 검토가 이 설계를 보완했다. 본문(§1~§8)은 그대로 두고 바뀐 곳만 적는다. 원본은 계획 §0·§1이다.

- TS4: "맞는 순간 대응은 따로 고칠 것 없음"은 틀렸다(계획 R1). 맞는 순간 대응이 행동 시작 사건에서만 출처를 열어, 추가 공격 시작이 출처가 되지 않는다. 공격 출처를 값 하나(`CombatAttackSource`)로 일반화하고 한 행동 구간 안에 출처를 여럿 둔다(TP-1·TP-2).
- TS4: 공격 모양에 공격 그룹 칸을 다시 넣지 않는다(R2, 그룹은 형식 표가 답함).
- TS3·TS1: 주체가 출처 공격자인 후속은 대응하되 해제 대상에 넣지 않는다(R3, TP-5). 주체 규칙은 TP-4, 근원 규칙은 TP-3(발동 모두에 근원이 있을 때만, 추가 공격 시작은 근원 없음).
- TS5: 판정 원본은 `CombatMovementKinds.Of` 하나이고 적 행동 간격도 그것을 부른다(R4). Q-TS4의 조건(밀림 걸음이 모두 원인을 채움)은 Core 코드로 확인했다(계획 §3.1).
- P7: 거절 안내가 없어 더한다(R5, TP-9).
- 더함: 발동 큐의 대상 여럿·칸 자리(R6), 추가 공격 시작이 둘이면 계약 오류(R7), `AnyRelease` 걷기(R8).
- TS8: 계획 Tf.0 상세 설계 먼저(R10). 한 공격에 수호자가 여럿일 수 있다(계획 §3.4).

## 10. 구현 메모 (2026-10-30, TS-a~TS-f5)

원본은 [계획](../plans/2026-10-30-combat-trait-system-view-plan.md) §7.1~§7.11이다. 이 절은 설계와 어떻게 맞춰졌는지만 적는다.

- TS1(상태 사건): 형식 표 줄 하나(사실 칸, 주체 = 바뀐 유닛, 근원 = `Origin`)와 사실 처리기의 `ApplyCharacter`로 됐다. HUD는 사건 순간에 바뀐다.
- TS2(표식 칸 보존): 사실 처리기가 디버프 여섯 칸을 모두 옮기고 표식만 바꾼다(천명님 줄). Core 요청 `WithHuntersMark`(Q-TS3 A)는 10-30 Core 예고(특성 효과를 공통 버프·디버프로, 표식 → 약점 노출)로 뜻이 없어져 공유에서 뺐다(사용자 "R1 빼기", 계획 §7.14). 표식 줄은 그 계약 인계가 오면 옮긴다.
- TS3(발동 근원): 발동 사건의 근원 `TraitOriginsOf`(발동 모두에 근원이 있을 때만)·주체 `TraitSubjectOf`(발동 유닛이 하나일 때)로 피격 묶음에 붙는다.
- TS4(추가 공격 판정): 판정 원본은 `CombatEventTypeTable.IsChildAttackStart`(= `ChildAttack` 있음) 하나다. 추가 공격 시작도 공격 출처 값 `CombatAttackSource`를 열고, 맞는 순간 대응이 한 행동 구간에 출처를 여럿 둔다(계획 R1). 칸·방향은 사실에서 옮긴다(`CombatAttackShape.ForChildAttack`). 2연발도 같은 길이다. `AnyRelease`·`ForFollowUp`·발동 종류 목록은 걷었다.
- TS5(이동 원인): `CombatMovementKinds.Of`가 걸음 원인으로 정하고, 걸음 표시·적 행동 간격이 함께 부른다. 옛 추론은 걷었다.
- TS6(HUD 상태): 다섯이 아니라 여섯이다(가속을 더함, 사용자 결정). 거절 안내 두 줄·짧은 글자 두 줄을 더했다(현강님 파일).
- TS7(흔적): 특성 외형 표 흔적 칸 둘 + 새 `Display/Map/FieldTrailPresenter`. 임시 아트는 반투명 Quad 둘이고 정렬 순서 −2다(계획에 없던 값).
- TS8(대신 맞기): 유닛 행동 설계 AA1~AA9로 넓혀 구현했다(발동 줄의 세 번째 형태). 그 구현 메모는 그 설계 §9.
- 남은 것: Core 요청(대신 맞은 공격의 넉백 §53, R5 보강·Q14), Core 공통 버프·디버프 계약 인계 뒤 상태 읽기 옮기기(계획 §7.14), 특성 22종 발동 줄 에셋 채우기(계획 §4), 사냥 표적 표시(다음 묶음).
