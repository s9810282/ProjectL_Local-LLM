# 이동 되돌리기: 다시 계산되는 상태를 수명 분류로 판단

- 작성 2026-11-03, 기준 `968436c83`(1차 수정 "[천명] 도발 수정"), 이 설계의 구현은 커밋 전
- 지침: `feature-change-locality-guidelines.md`(변경 독립성·이해 범위)
- 범위: Core Contracts의 상태 수명 선언 자리, 이동 되돌리기 비교 한 곳. 게임 규칙·공개 결과·Buffer·View 동작은 바꾸지 않는다.

## 1. 배경

도발 보유자가 움직여 적의 도발당함이 바뀌면, 이동 직후 되돌리기 가능 여부를 묻는 미리보기에서
`MovementUndoResult.Validate`가 `ArgumentException`을 던져 전투가 멈췄다(2026-11-03 재생 보고).
원인은 도발당함을 넣을 때(CT-a `707fa54dc`, 계획 TP-C4) 되돌리기 쪽 두 곳은 고치고, 검사 줄
"영수증이 0개면 반환 사건뿐" 하나를 놓친 것이다.

같은 날 1차 수정에서 도발당함 예외를 `MovementUndoResult.SameExceptTaunted` 한 함수로 모아
검사 줄도 그 함수를 쓰게 했다. 터짐과 두 곳이 어긋나던 문제는 이것으로 풀렸다. 이 문서는 그 위에서
**되돌리기 계약이 `TauntedStatus`라는 이름을 아는 것**이 맞는지 다룬다.

## 2. 대표 변경 사례

| 사례 | 근거 |
|---|---|
| A. 위치로만 정해지는 상태를 하나 추가한다 | 실제로 일어난 변경이다(도발당함, CT-a). 같은 변경에서 되돌리기 계약을 놓쳐 이번 버그가 났다. |
| B. 상태를 하나 추가한다(수명 무관) | `StatusEffectCatalog`에 키는 있는데 상태 클래스가 없는 것이 여섯 개다(공격력·방어력 증가 외 `OutgoingDamageIncrease`·`IncomingDamageDecrease`·`MovementRangeIncrease`·`KnockbackImmunity`·`AttackRangeDecrease`, `DefenseIncrease`는 결속 보너스 조회값만). 근거 없는 미래 기능이 아니라 이미 자리를 잡아 둔 변경이다. |
| C. 도발 규칙 값(반경 등)을 바꾼다 | 대조용. 지금도 `TauntTraitRule` 한 곳이고, 되돌리기는 같은 규칙 함수로 다시 계산하므로 되돌리기 쪽을 볼 필요가 없다. 바꿀 것 없음. |

## 3. 지금 부담과 근거(1차 수정 뒤, 코드에서 확인)

### 사례 A
- 수정 범위: 상태 클래스(`AppliedStatus.cs`), 보유 조정(`TraitPresenceReconciler`), 조회 모델 수명 switch(`StatusEffectSourceSnapshot` 생성자), 조회 모델 `StatusEffectReadModel.Capture` 한 줄, 이동 사건 허용 목록(`MovementEffectUndo.ForwardEventsSupported`), **되돌리기 비교 함수(`MovementUndoResult.SameExceptTaunted`)**.
- 이해 범위: 마지막 항목 때문에 "되돌리기는 영수증으로 복원하고, 영수증이 없으면 반환 사건만 나와야 한다"는 되돌리기 내부 규칙을 알아야 한다. 상태를 만드는 사람이 보통 열어 볼 이유가 없는 파일이다. 빠뜨리면 그 상태를 바꾸는 이동은 되돌리기가 조용히 꺼진다(1차 수정 전에는 어긋나서 터졌다).
- 판단: **불필요한 결합.** 되돌리기가 알아야 하는 사실은 "이 상태는 지금 사실만으로 다시 계산되어 이전 값이 없다"는 상태의 성질 하나다. 그 성질은 상태 쪽이 이미 `StatusEffectLifetimeKind.CurrentEligibility`로 선언한다. 같은 사실을 되돌리기가 타입 이름으로 한 번 더 적고 있다(SSOT 위반).

### 사례 B
- 수정 범위: 상태 클래스, `StatusEffectReadModel.Capture`, **조회 모델 수명 switch**. switch를 빠뜨리면 컴파일은 되고 조회값을 만드는 순간 "아직 조회 계약이 없는 효과" 예외로 멈춘다.
- 이해 범위: 수명이 상태 클래스가 아니라 조회 모델(`Contracts.Snapshots`) 안에 있다는 것을 알아야 한다.
- 판단: **원본 자리 문제.** 수명은 상태 규칙의 사실인데, 원본이 읽기 모델에 있다. 읽기 모델을 원본처럼 쓰고 있어서, 되돌리기 같은 규칙 코드가 이 값을 쓰려면 읽기 모델을 거쳐야 한다(지침 §5).

### 필요한 연결로 남기는 것
- `ForwardEventsSupported`의 특성 발동 허용 목록: 특성마다 "이동 중 이 발동이 나도 되돌릴 수 있나"를 정하는 목록이다. 발동 하나만 보고는 어느 상태인지 알 수 없고, 같은 `StatusApplied`라도 영수증이 필요한 특성(정지 사격·정찰대)이 있다. 특성을 추가하는 사람이 되돌리기 가능 여부를 정해야 하는 것은 게임 규칙상 필요한 연결이다.
- `TraitPresenceReconciler`·`AppliedStatusRules.ReplaceTaunted`: 도발 규칙의 주인이라 도발을 아는 것이 맞다.
- 되돌리기 시험: 이동이 그 상태를 실제로 바꾸므로 사례 A의 검증 범위에는 되돌리기가 계속 들어간다.

## 4. 결정

- **D1 수명은 상태 타입이 선언한다.** `AppliedStatus`에 추상 속성 `Lifetime`(`StatusEffectLifetimeKind`)을 두고 일곱 상태가 각자 값을 낸다. 조회 모델은 `status.Lifetime`을 읽기만 한다. switch와 런타임 예외 대신 컴파일러가 빠뜨림을 막는다(사례 B).
  - 이름: `WeaknessExposureStatus`에 이미 `LifetimeKind`(`WeaknessExposureLifetimeKind`, 그 상태만의 좁은 선택)가 있어 겹치지 않게 `Lifetime`으로 한다. 약점 노출은 자기 `LifetimeKind`에서 `Lifetime`을 만든다(지금 switch와 같은 식).
  - 열거형 `StatusEffectLifetimeKind`는 `Contracts.Snapshots`(`StatusEffectSnapshot.cs`)에서 `Contracts.CombatEffects`(`AppliedStatus.cs`, 같은 파일의 `WeaknessExposureLifetimeKind` 옆)로 옮긴다. 처음에는 무관한 정리로 보고 두려 했으나, 그대로 두면 상태 원본(`CombatEffects`)이 읽기 모델 네임스페이스(`Snapshots`)를 참조하는 방향이 생겨 같은 날 옮기기로 했다(사용자 결정). 이 열거형을 쓰는 파일은 모두 `CombatEffects`를 이미 using하고 있어 다른 파일은 바뀌지 않는다. 값·이름은 그대로라 직렬화 영향도 없다(에셋에서 쓰는 곳 없음).
  - 결속 방어 보너스 조회값 `DefenseIncreaseEffectSnapshot.LifetimeKind`는 `AppliedStatus`가 아니라 특성 효과 값의 조회값이라 그대로 둔다.
- **D2 되돌리기 비교는 수명으로 판단한다.** `SameExceptTaunted` → `SameExceptCurrentEligibility`(수명이 `CurrentEligibility`인 상태 항목을 빼고 비교), 검사 줄의 `IsRecomputedTaunt` → `IsRecomputedEligibility`. 되돌리기 파일에서 `TauntedStatus` 이름이 사라진다.
- **D3 `CurrentEligibility`의 뜻을 계약으로 적는다.** "값·시계·예산 없이, 지금 사실이 조건을 만족하는 동안만 있는 상태. 보유 조정이 사실이 바뀔 때마다 다시 계산한다. 이동 되돌리기는 영수증 없이 되돌린 자리의 다시 계산으로 복원한다."
- **D4 안전망은 그대로다.** 무시하는 두 곳(`SupportsCharacterChange`, `Validate`)은 모양 검사다. 실제 복원은 체크포인트를 만들 때 `ForwardCharactersMatch`가 "되돌린 자리에서 다시 계산한 결과가 이동 전과 완전히 같다"를 요구하고(`MovementUndoPolicy.cs:150`), 게시 경계 `ValidatePublication`이 처음부터 다시 계산해 대조한다. 그래서 어떤 상태를 `CurrentEligibility`로 잘못 적어 다시 계산이 그 상태를 되돌리지 못하면, 되돌리기가 틀리게 되는 것이 아니라 그 이동의 체크포인트가 만들어지지 않는다.

## 5. 전후 차이

| | 1차 수정 뒤(지금) | 이 설계 뒤 |
|---|---|---|
| 사례 A 수정 | 상태·보유 조정·조회 switch·Capture·허용 목록·**되돌리기 비교** | 상태(수명 선언)·보유 조정·Capture·허용 목록 |
| 사례 A 이해 | 되돌리기 내부의 "영수증 없는 복원" 예외 목록 | `CurrentEligibility`의 뜻(계약) |
| 사례 B 수정 | 상태·Capture·조회 switch(빠뜨리면 런타임 예외) | 상태(수명 선언, 컴파일러가 강제)·Capture |
| 수명의 원본 | 조회 모델 switch | 상태 타입 |
| 검증 | 되돌리기 시험 | 그대로 필요(필요한 연결) |

## 6. 바꾸는 파일

| 파일 | 내용 |
|---|---|
| `Contracts/CombatEffects/AppliedStatus.cs` | 열거형 `StatusEffectLifetimeKind`(옮겨 옴, `CurrentEligibility` 뜻 주석), 추상 `Lifetime`, 일곱 상태 각 한 줄 |
| `Contracts/Snapshots/StatusEffectSnapshot.cs` | `LifetimeKind => Status.Lifetime`(switch·열거형 삭제) |
| `Contracts/Results/MovementUndoResult.cs` | `SameExceptCurrentEligibility`·`IsRecomputedEligibility` |
| `Application/Combat/MovementEffectUndo.cs` | 부르는 이름 하나 |
| 시험 넷의 `ProbeStatus`(`AppliedAttackStatusTests`·`AppliedRestrictionStatusTests`·`StatusEffectContractTests`·`CoreIntegration/StatusEffectPublicationTests`) | 추상 멤버 구현 한 줄(컴파일 맞춤, 확인 내용 그대로) |

Factory·ServiceKeys·Buffer·View·에셋 무변경. View `CombatStatusDescriptionFormatter`는 조회값 `LifetimeKind`를 그대로 읽는다.

## 7. 범위 밖 발견

- `CombatTemporaryStateCleanup.cs:64`: 사망·전투 끝 정리가 `StatusRemoved` 출처를 상태 종류마다 나열한다(약점 노출·도발당함 각 한 줄). 상태를 추가할 때 함께 고쳐야 하는 또 한 자리다. 정리 이유(사망)가 수명과 달라 이번에 묶지 않는다.
- `TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`: 도발당함 도입 뒤 `StatusApplied` 발동이 같이 나와 기대값 1과 어긋난다(이번 변경 전부터 실패).

## 8. 검증

- Test Runner(배치): `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration`. 기대: 1차 수정 뒤 결과(5,671개 중 실패 33개, 목록 같음)와 실패 목록이 같고, `MovementPassiveUndoTests.TauntRangeChange_UndoRecomputesTauntedWithoutReceipt` 두 사례와 상태 조회 시험(`StatusEffectReadModelTests` 등)이 통과.
- 결과(2026-11-03 배치 실행, 실행으로 검증): 5,671개 중 5,638개 통과·33개 실패, 실패 목록이 1차 수정 뒤 실행과 같음(새 실패·고쳐진 것 없음). 되돌리기·상태 관련 시험 302개 중 실패 2개는 둘 다 전부터 있던 것(`TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`, `DataBaseAssetTests.GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(Taunt)`). 열거형을 옮긴 뒤 다시 돌린 실행도 같은 결과(실패 33개, 목록 같음). 재생 확인은 하지 않음.
