# 작렬(CrashImpact) 특성 설계

- 작성: 2026-11-10, Core(천명) 세션
- 상태: 설계 초안. 코드 변경 없음. 아래 "설계에서 정한 것"(P1~P12)은 사용자 확인 대상이다.
- 이름: 한국어 **작렬**, 코드 `CrashImpact`, 특성 키 `TraitKey.CrashImpact = 520`
- 처음 갖는 유닛: 다이너마이트 해머(`Unit_DynamiteHammer`, 유닛 키 24)

## 0. 요청과 결정

사용자 요청(2026-11-10): 작렬은 넉백을 강화한 특성이다. 앞으로 넉백을 바꾼 특성이 더 생길 예정이고, 작렬이 그 첫 번째다.

1. 기본 공격으로 피해를 입힌 대상이 일직선으로 날아간다(거리 조절 가능, 기본 3칸).
2. 날아가다 벽에 부딪히면, 날아간 대상이 **자기 최대 체력의 n%** 피해를 입는다(조절 가능, 기본 10%).
3. 날아가다 유닛에 부딪히면, 날아간 대상과 부딪힌 유닛 **둘 다** 피해를 입는다. 피해량은 **날아간 대상의 최대 체력 n%**다(조절 가능, 기본 10%).

질문에 대한 답(2026-11-10 사용자):

| 질문 | 결정 |
|---|---|
| "초기 피해량" | 기본 공격 피해를 말한 것이다. 조절 값이 아니다. |
| 구조물에 부딪힘 | 날아간 대상만 다친다(벽과 같음). |
| 맵 끝·고지대 턱 | 둘 다 벽으로 본다. |
| 부딪힘 피해와 방어력 | 방어력의 영향을 받는다. 고정 피해가 아니다. |
| View 연출 | 발동 신호가 필요하다(이펙트가 여러 가지일 예정). 날아가는 동안의 상태도 알 수 있으면 좋다. |
| 유닛 % 기본값 | 10% |
| 누가 갖나 | 다이너마이트 해머 |
| 나머지 | 권장안대로(아래 §2) |

조절 값은 셋이다: **거리(칸)**, **벽 피해 %**, **유닛 피해 %**.

## 1. 지금 코드 사실

### 1.1 넉백이 도는 곳

- `Application/Combat/AttackKnockbackCalculator.ResolveSteps`가 공격 넉백을 처리한다.
  - 거리 = 넉백 기여(`EffectKnockbackContribution`) 중 가장 긴 것.
  - 대상 = 피해를 입힌 대상(`ledger.Roles`), 충격 볼트면 직접 전달된 대상. 희생이 대신 맞았으면 수호자.
  - ID 순서로 한 명씩 민다. 앞 대상의 이동으로 전투가 끝나면(`arrival.Terminal`) 멈춘다.
  - 넉백 면역이면(`KnockbackImmunityCheck`) 밀지 않는다. 기본 공격으로 죽은 대상은 시체 경로만 계산한다.
- 한 칸씩 미는 것은 `AttackForcedMovement.ExecuteUnitSteps`다. 들어갈 수 있는지는 `CanEnterUnit` 하나가 정한다.
- 결과는 `AttackKnockbackSnapshot`(시작 칸·방향·요청 거리·실제 경로·멈춘 이유 `Completed`/`Blocked`/`Interrupted`/`Immune`)으로 장부에 남는다. 공격 결과와 공격 미리보기(`AttackPreview.Knockbacks`)가 이것을 낸다.

### 1.2 넉백이 멈추는 경우 (`CanEnterUnit`)

| 다음 칸 | 멈춤 |
|---|---|
| 맵 밖 | 멈춤 |
| 벽 지형 | 멈춤 (`EnvironmentTerrainRules.CanKnockback`) |
| 고지대인데 지금 칸은 고지대가 아님 | 멈춤 (같은 함수) |
| 살아 있는 필드 캐릭터가 있음 | 멈춤 |
| 살아 있는 구조물이 있음(장애물·화약통·본부 등) | 멈춤 |
| 낭떠러지 | 들어가서 떨어짐 → 부딪힘 아님 |

다칸 대상도 다음 **앵커 칸**만 검사한다(지금 넉백의 성질, 바꾸지 않음).

### 1.3 View가 이미 받는 것

- 넉백 특성이 밀면 발동 사건 `KnockbackApplied`가 이동 사건들 **앞에** 들어간다(`CombatEffectsEvent`, 출처 = 특성 보유자, 대상 = 밀린 유닛들, 칸 = 시작 → 경로).
- 한 칸 이동마다 `Moved` 사건이 나오고, 이동 원인 `CombatMovementCause`가 `Forced` + **특성 키**를 갖는다. 그래서 View는 이 걸음이 어느 특성 때문인지 안다.

### 1.4 방어력

- `TargetDamagePipeline.Calculate`: 실효 방어력 1당 피해 1% 감소(100 상한). 공격 상성 단계가 앞에 있다.
- 실효 방어력은 `CombatEffectiveStatsCalculator.PhysicalDefense(input, victim, characters)`(방어구 파괴 등 반영).
- 피해 적용 공통 입구는 `CombatDamageApplication.ApplySteps`다(보호막·받는 피해 감소 특성·사망 처리). 가시밭 피해는 `EnvironmentDamage` 종류로 여기를 지난다.

### 1.5 다이너마이트 해머

`DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_DynamiteHammer.asset`: 최대 체력 20, 방어 5, 공격력 6, 사거리 1, `traits: []`.

## 2. 규칙 (CI1~CI8)

### CI1 발동 조건

작렬은 넉백 기여의 한 종류다. 기존 넉백과 같은 때에 같은 대상에게 적용된다. 방향은 공격자 기준 8방향(`ForcedDisplacementGeometry.ResolveBranchStep`)이다.

- 넉백 면역이면 날지 않고 부딪힘도 없다.
- 기본 공격으로 죽은 대상은 날지 않는다(시체 경로 규칙은 그대로, 부딪힘 없음).

### CI2 부딪힘 = 멈춘 이유가 `Blocked`

요청 거리를 다 가기 전에 다음 칸이 막혀 멈추면 부딪힌 것이다.

- 한 칸도 못 날아가도(바로 옆이 막힘) 부딪힘이다.
- 요청 거리를 다 날아갔으면, 그 다음 칸에 벽이 있어도 부딪힘이 아니다.
- 가는 도중 죽거나(가시 등), 낭떠러지에 떨어지거나, 토네이도에 휩쓸리거나, 전투가 끝나 멈추면(`Interrupted`) 부딪힘이 아니다.
- 부딪힘 칸 = 마지막 도착 칸 + 방향(경로가 비면 시작 칸 + 방향).
- 부딪힘은 대상마다 **한 번**이다.

### CI3 무엇에 부딪혔나

막은 이유를 셋으로 나눈다. 판정 원본은 `CanEnterUnit`과 같은 함수 하나다(§3.2).

| 막은 것 | 종류 |
|---|---|
| 맵 밖, 벽 지형, 고지대 턱 | 벽 |
| 살아 있는 구조물 | 벽 (구조물은 다치지 않음) |
| 살아 있는 필드 캐릭터 | 유닛 (그 캐릭터 ID) |

### CI4 피해량

- 벽: 날아간 대상이 `날아간 대상 최대 체력 × 벽 % ÷ 100`(내림)을 기준 피해로 받는다.
- 유닛: 날아간 대상과 부딪힌 유닛이 각각 `날아간 대상 최대 체력 × 유닛 % ÷ 100`(내림)을 기준 피해로 받는다.
- 최대 체력은 부딪히는 순간 날아간 대상의 `Health.MaxHealth`다.
- 부딪힌 유닛은 편을 가리지 않는다(공격자의 아군도 다침).

### CI5 방어력

기준 피해에 **받는 쪽 자신의** 실효 방어력을 적용한다(1당 1%, 100 상한, 내림). 공격 상성은 적용하지 않는다. 기준 피해가 1 이상이면 방어 뒤 최소 1이다. 그 뒤는 공통 입구(`ApplySteps`)가 보호막·받는 피해 감소 특성·사망을 처리한다.

예: 해머가 최대 체력 20, 방어 5인 적을 벽에 날림 → 기준 2 → 방어 뒤 1.9 → 1.

### CI6 순서

대상마다: 날아감 → 부딪힘 신호 → 날아간 대상 피해 → (유닛이면) 부딪힌 유닛 피해 → 다음 대상.

- 여러 대상은 지금처럼 ID 순서다. 먼저 날아간 대상에게 다음 대상이 부딪힐 수 있다.
- 피해 도중 전투가 끝나면(메인 사망, 목표 달성) 남은 피해와 다음 대상을 멈춘다.
- 부딪힌 유닛이 아직 날지 않은 대상이면, 부딪힘 피해를 먼저 받고 자기 차례에 날아간다(살아 있으면).

### CI7 다른 넉백과 함께

- 거리는 지금처럼 모든 넉백 기여 중 가장 긴 값이다.
- 부딪힘 피해는 작렬 기여가 하나라도 있을 때만이다. 작렬이 여럿이면 % 가 가장 큰 것을 쓴다(벽·유닛 따로).
- 이동 원인 특성 키·발동 사건의 출처는, 가장 긴 거리 기여가 여럿이면 작렬을 먼저 고른다(View가 작렬 연출을 고를 수 있게).
- 충격 볼트(충돌 중심 모드)와는 함께 가질 수 없다. 넉백 평가가 방향 모드가 다른 기여를 예외로 막기 때문이다. 지금 "충격 볼트 + 넉백"을 전투 준비에서 거절하는 줄(`CombatAttackProfile`)에 작렬을 더한다. (계획 때 코드 확인으로 고침, 2026-11-10)

### CI8 밀린 장애물은 대상 아님

넉백으로 밀리는 장애물(`KnockbackObstacleSteps`)은 작렬 부딪힘 피해를 받지 않는다. 지금과 같다.

## 3. 구조

### 3.1 값 (기존 충격 볼트와 같은 꼴)

| 층 | 새 파일 |
|---|---|
| DB 입력 | `DataBaseSystem/Data/InGame/Table/Unit/Trait/CrashImpactTraitAuthoring.cs`(직렬화: `distanceCells` 3, `wallDamagePercent` 10, `unitDamagePercent` 10) |
| DB Data | `…/Trait/CrashImpactTraitData.cs`(검사: 거리 ≥ 1, % 0~100) |
| Core 계약 | `Contracts/Commands/Traits/CrashImpactTraitSpec.cs`, `Contracts/Snapshots/Traits/CrashImpactTraitStatusSnapshot.cs` |
| Core 도메인 | `Domain/Traits/Definitions/CrashImpactTraitDefinition.cs`, `Rules/CrashImpactTraitRule.cs`, `Effects/CrashImpactEffectRule.cs`, `Effects/CrashImpactEffectStateSnapshot.cs`, `States/CrashImpactRuntimeState.cs` |
| Core 조립 | `Application/Combat/Traits/CrashImpactTraitFactories.cs` |

연결 줄을 더하는 곳(충격 볼트가 있는 자리마다 한 줄): `TraitKey`, `TraitSpecAdapter`, `TraitRegistry`, `TraitEffectProvider`, `TraitEffectStateCodec`, `CombatCommandEffects`, `CombatAttackProfile`, `MainSpecializationDefinitionSnapshot`.

보유 카드 스냅샷(`OwnedCharacterStatsSnapshot`)에는 특성 전용 설명 칸을 더하지 않는다. 설명 값은 이미 `TraitSettingsSet`(Spec)으로 나가고 View 설명 토큰도 Spec을 읽는다. (계획 때 고침)

### 3.2 막은 이유 판정 하나 (SSOT)

`AttackForcedMovement.CanEnterUnit`의 조건을 새 함수 `FindKnockbackBlocker`로 옮기고, `CanEnterUnit`은 "막은 것 없음"인지만 묻는다.

```text
KnockbackBlocker { None, Wall, Structure(id), Character(id) }
맵 밖 → Wall / 벽 지형·고지대 턱(CanKnockback 거짓) → Wall / 캐릭터 → Character(id) / 구조물 → Structure(id)
```

부딪힘 종류(CI3)는 이 값에서 나온다. 같은 조건을 두 번 쓰지 않는다. 지금 넉백·끌어오기·사전 강제 이동의 동작은 그대로다.

### 3.3 넉백 기여에 부딪힘 값

`EffectKnockbackContribution`에 선택 값 `Collision`(벽 %, 유닛 %)을 더한다. 작렬 효과 규칙만 채운다. `EffectKnockbackEvaluation`(또는 계산기)이 기여 중 가장 큰 %를 고른다(CI7).

### 3.4 부딪힘 처리

새 `Application/Combat/CrashImpactCollision.cs`(정적):

- 순수 규칙 `BaseDamage(maxHealth, percent)`, `AfterDefense(baseDamage, defense)` — CI4·CI5 식 한 곳.
- `ResolveSteps(...)`: `Blocked`로 멈춘 대상에게 부딪힘 신호 → 피해를 낸다. 피해는 `world.Damage.ApplySteps`(종류 `EnvironmentDamage`, 근원 = 부딪힘 칸 + 공격자 + 이 공격 그룹)로 넣고 `ledger.AddDamage`로 미리보기에 반영한다.

`AttackKnockbackCalculator.ResolveSteps`는 대상 하나를 민 뒤 `Record` 다음에 이것을 부른다.

## 4. View에 주는 것

사용자 요청: 발동 신호 + 날아가는 동안의 상태.

| 언제 | 무엇 | 새것? |
|---|---|---|
| 날기 시작 | 발동 사건 `KnockbackApplied`, 특성 키 `CrashImpact`(출처 = 해머, 대상 = 날아간 유닛들, 칸 = 시작 → 경로). 이동 사건들 앞에 온다. | 있는 것 (키만 새로) |
| 날아가는 동안 | 걸음마다 `Moved` 사건, 이동 원인 = `Forced` + `CrashImpact` | 있는 것 (키만 새로) |
| 부딪힘 | 새 발동 종류 `TraitActivationKind.CrashImpactCollided = 410`. 출처 = 해머, 대상 = 날아간 유닛(+ 부딪힌 유닛), 칸 = 부딪힘 칸, Magnitude = 날아간 대상이 받은 최종 피해 | **새것** |
| 부딪힘 피해 | 보통 피해 사건(근원 칸 = 부딪힘 칸) | 있는 것 |
| 공격 결과·미리보기 | `AttackKnockbackSnapshot`에 선택 값 `Collision`(종류 벽/구조물/유닛, 부딪힌 개체 ID, 부딪힘 칸) | **새것** |

"날아가는 동안의 상태"는 새 상태 효과를 만들지 않고, 이동 원인의 특성 키로 준다(이미 걸음마다 붙어 있음). View는 `Moved`의 원인이 `CrashImpact`인 걸음을 "날아가는 중"으로 보면 된다.

미리보기: 공격 호버 때 `AttackPreview.Knockbacks[i].Collision`으로 "여기서 부딪힘"을, 대상 예상 체력에 부딪힘 피해가 들어간다(장부를 거쳐서). 부딪힌 유닛이 공격 대상이 아니면 넉백 보조 행과 같은 꼴(직접 피격 아님, 역할 Additional)로 미리보기 대상 목록에 들어간다.

## 5. View 짝 (Core만 넣으면 생길 일)

해머에 특성을 붙이면 View가 이 특성 키를 처음 본다. 구현 때 아래를 확인하고, 필요하면 같은 커밋에 넣거나 View 담당에게 넘긴다.

- 특성 외형 `TraitVisual_CrashImpact`(키 520)와 표 등록. 외형이 없으면 HUD·정보창의 특성 표시가 빠지거나 멈출 수 있다(구현 때 확인).
- 설명 토큰 `TraitDescriptionBindings`에 `CrashImpact` 줄(거리·벽 %·유닛 %).
- 새 발동 종류 410의 연출 줄(특성 외형 표 발동 연출). 없으면 연출 없이 지나간다.

View 전달 문서에 §4 표를 그대로 적는다.

## 6. 설계에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 |
|---|---|
| P1 | 부딪힘 피해 종류는 새 열거 값을 만들지 않고 `EnvironmentDamage`를 쓴다(보호막·받는 피해 감소 특성은 가시밭과 같게 적용). |
| P2 | 피해 근원의 공로자 = 공격자(해머). 부딪힘으로 죽으면 해머의 처치로 센다(보상 포함). 구현 때 처치 보상이 근원 공로자로 셈해지는지 확인한다. |
| P3 | 공격 상성은 적용하지 않고 방어력만 적용한다. |
| P4 | 기준 피해 ≥ 1이면 방어 뒤 최소 1. % 가 0이면 피해는 없고 부딪힘 신호만 낸다. |
| P5 | 최대 체력 = 부딪히는 순간의 `Health.MaxHealth`. |
| P6 | 날아간 대상 피해 → 부딪힌 유닛 피해 순서(CI6). |
| P7 | 보스 포효 피격 수는 부딪힘 피해도 한 번으로 센다(가시 피해와 같음). |
| P8 | 장애물 넉백·시체 경로에는 부딪힘이 없다(CI8). |
| P9 | % 입력 범위 0~100, 거리 ≥ 1. |
| P10 | 새 특성 키 520, 새 발동 종류 410. |
| P11 | 해머 에셋 `traits`에 작렬 한 줄(거리 3, 벽 10, 유닛 10). 증강(`TraitGrowth`) 연결은 이번 범위 밖. |
| P12 | 넉백 결과 요약에 `Collision` 선택 값을 더한다(생성자 끝 선택 인자, 기존 호출 그대로). |

## 7. 시험 (Core 시험 어셈블리)

`ProjectL.Tests.Core`·`CoreIntegration`에 둔다. View 시험은 쓰지 않는다.

- 규칙: 기준 피해·방어 뒤 피해(내림, 최소 1, 0%).
- 막은 이유 판정: 맵 밖·벽·고지대 턱·구조물·캐릭터·없음.
- 통합: 벽 부딪힘(3칸 안에 벽), 유닛 부딪힘(둘 다 피해, 각자 방어), 구조물 부딪힘(구조물 무피해), 바로 옆이 벽, 거리를 다 날아감(부딪힘 없음), 면역, 낭떠러지 떨어짐(부딪힘 없음), 두 대상 ID 순서로 서로 부딪힘, 부딪힘으로 메인 사망 시 멈춤, 미리보기에 `Collision`과 예상 피해.
- 기존 넉백 시험이 그대로 통과하는지(`CanEnterUnit` 옮김).

## 8. Phase(안)

| Phase | 내용 | 끝나면 |
|---|---|---|
| CI-a 값 | 특성 키·Spec·Data·Authoring·정의·규칙·효과(부딪힘 값 없이 넉백만)·조립 연결, 해머 에셋 | 해머가 기본 3칸 넉백. 부딪힘 없음 |
| CI-b 부딪힘 | `FindKnockbackBlocker`, 기여 `Collision`, `CrashImpactCollision`, 계산기 연결, 규칙·통합 시험 | 부딪힘 피해가 남 |
| CI-c 신호·미리보기·문서 | 발동 종류 410, `AttackKnockbackSnapshot.Collision`, 미리보기 확인, View 전달 문서(§4·§5) | View가 받을 것 준비 |

Phase마다 멈추고 보고한다. 각 Phase 끝에 Test Runner로 Core 시험을 돌린다.

## 9. 구현 메모 (2026-11-10)

계획대로 CI-a~CI-c를 이어서 구현했다. 설계와 다르게 한 점·정한 점:

- **진입 판정 함수 하나**: `AttackForcedMovement.FindBlocker`가 막은 것(`ForcedMovementBlocker`: 벽/구조물/캐릭터)을 돌려주고, `CanEnterUnit`은 "막은 것 없음"만 묻는다. 부딪힘 종류는 같은 함수를 다시 부른다(경로가 막힌 직후라 점유가 같다). 동작은 기존 `CanEnterUnit`과 같다.
- **방어 식 하나**: 공격 산식의 방어 단계를 `TargetDamagePipeline.ApplyPhysicalDefense`로 꺼내 공격과 부딪힘이 함께 쓴다.
- **부딪힘 사실 종류 열거 하나**: 공개 `AttackKnockbackCollisionKind`를 내부 막은 것 값도 쓴다.
- **부딪힘 출처**: 넉백 평가가 부딪힘 값(`EffectKnockbackEvaluation.Collision`, 비율 최댓값)과 출처(`CollisionOrigin`)를 낸다. 넉백 특성의 거리가 더 길어도 부딪힘 신호의 출처는 작렬이다.
- **충격 볼트와 함께 불가**(CI7 고침): 전투 준비(`CombatAttackProfile`)에서 거절한다.
- **보유 카드 스냅샷에 설명 칸을 더하지 않음**: 설명 값은 `TraitSettingsSet`(Spec)으로 나간다.
- 파일: 새 `Application/Combat/CrashImpactCollision.cs`·`CrashImpactCollisionRules.cs`·`ForcedMovementBlocker.cs`, 특성 파일 일곱(Spec·상태 스냅샷·정의·규칙·효과 규칙·효과 상태·런타임 상태)·조립 하나, DB 둘. 시험 `Tests/EditMode/CrashImpactTests.cs`.
