# 적 전용 특성 재구성·자폭·공포 설계 (초안)

- 작성: 2026-11-10, Core(천명)
- 상태: 3판, **Core 구현 완료(2026-11-10, Phase 0~F)**. 구현에서 바뀐 것은 §16. View 대응은 Core 전달 문서 §67(미전달).
  - 2026-11-17: 기차 즉사도 자폭·공포를 발동하도록 바꿨다(사용자 결정, §17).
  - Q-ED1~Q-ED8은 결정이 끝났다(2026-11-10 사용자, §12).
  - 사망 특성은 큐 대신 "능력 인터페이스 + 명령 끝 확인"으로 바꿨다(§5, 바뀐 이유는 §15).
  - 설계에서 정한 것 P1~P17은 확인 대상이다.
  - Task 계획: [적 전용 특성 Task 계획](../plans/2026-11-10-enemy-death-traits-plan.md).
- 범위: Core 규칙·계약·DB 입력. View 연출은 §9 인계 목록만 적는다(구현은 View 담당).

## 1. 요구 정리

기획 원문과 2026-11-10 사용자 답을 합친 것이다.

### 1.1 재구성

| 항목 | 내용 |
|---|---|
| 발동 | 피해로 HP가 0 이하가 될 때. 실제 사망은 일어나지 않는다. 전투당 1번. 사망 시 발동 특성보다 먼저 판단한다. |
| 발동하지 않는 것 | 즉사 스테이지 기믹: 기차, 낭떠러지 낙사(약한 지반 붕괴 낙사 포함) |
| 발동 순간 | 체력을 최대치(100%)로 바로 회복한다. Lv2부터는 최대 체력 50%만큼 보호막을 더 받는다(합계 150%). |
| 재구성 중 | 이동·공격 불가. 피해는 그대로 받는다. HP가 다시 0이 되면 진짜 사망이다. 넉백·끌어오기도 받는다. |
| 부활 | 시전자 진영의 턴 시작에 상태가 풀리고 그 턴에 바로 행동한다. 예: 플레이어 턴에 발동 → 적 턴(불가) → 플레이어 턴 → 적 턴 시작에 부활. |
| Lv3 | 부활할 때 주변 8칸의 모든 유닛을 1칸 넉백한다. |

### 1.2 자폭

| 항목 | 내용 |
|---|---|
| 발동 | 피해로 사망할 때. 낙사는 제외. 기차 즉사는 2026-11-17부터 발동한다(§17). |
| 효과 | 주변 8칸의 모든 대상(적·아군 진영 캐릭터, 구조물)에게 사망 직전 공격력의 50% / 75% / 110% 피해. |
| 피해 성질 | 고정 피해. 방어력 미적용, 보호막 적용. |
| 귀속 | 폭발로 생긴 처치·피해는 자폭한 유닛 자신에게 귀속한다. |
| 위치 | 사망 위치. 넉백 공격으로 밀려나 죽으면 밀려난 끝 칸. 끌어오기면 끌려온 칸. |

### 1.3 공포

| 항목 | 내용 |
|---|---|
| 발동 | 자폭과 같다(피해 사망, 즉사 기믹 제외). 위치 규칙도 같다. |
| 효과 | 주변 8칸의 모든 캐릭터에게 이동·공격 불가. |
| 기간 | 걸린 쪽의 다음 자기 턴 한 번. 걸리자마자 풀리면 안 된다. |
| 겹침 | 이미 무장 해제·이동 제한이 있으면 더 긴 쪽이 남는다. |

### 1.4 적 전용 제한

B안: 특성 규칙은 진영과 무관하게 짜고, 경계에서 플레이어 쪽 진입을 거절한다(§7).

## 2. 확인한 지금 코드 사실

| 사실 | 위치 |
|---|---|
| HP를 0으로 만드는 곳은 다섯 곳뿐이다: 공통 피해 경계 `ApplySteps`, 즉사 `DefeatSteps`(마무리 사격), 낙사 `ApplyFall`, 기차, 낙석. | `CombatDamageApplication.cs:171,293`, `MovementProjection.cs:405`, `RailwayGimmickRule.cs:70`, `RockfallGimmickRule.cs:108` |
| 낙석은 일부러 공통 피해 경계를 거치지 않는다(방어·보호막·귀속 없음, 최대 체력 비율 감소). | `RockfallGimmickRule.cs:105-107` |
| `HealthStateSnapshot.IsDead`는 `CurrentHealth == 0` 하나다. 최대치 초과 HP는 생성자가 거절한다. | `Contracts/Snapshots/HealthStateSnapshot.cs` |
| 공격 전달 순서는 직접 피해 → 넉백 → 가시밭 → 강제 도착 → 처치 연쇄다. 죽은 대상은 넉백하지 않는다. | `AttackDeliveryExecutor.cs`, `AttackKnockbackCalculator.cs:113` |
| 넉백에는 충돌 피해가 없다. 밀리는 도중 가시·지뢰로 죽으면 그 칸이 사망 위치가 된다. 끌어오기는 끈 뒤 피해를 준다. | `AttackForcedMovement.cs:97-101`, `PullAttackCalculator.cs` |
| 화약통 폭발은 명령 안 큐로 연쇄하고, 주변 8칸의 캐릭터·구조물(장애물 포함)을 친다. 피해는 환경 피해 경로(지형 엄폐 → 공통 경계)다. | `ObstacleReactionCalculator.cs:78-130` |
| 공격 불가 = `Disarm`(10), 이동 불가 = `Bind`(11). 행동 입장·AI·보스 포효가 `IsAttackRestricted`/`IsMovementRestricted`로 막는다. 같은 키 제한은 출처와 상관없이 새 것이 옛 것을 바꾼다. | `CombatActionAdmission.cs:44-50`, `AppliedStatusRules.cs:22` |
| 둔화는 "현재 진영 턴이 이미 시작했으면 다음 자기 턴 종료까지" 규칙을 쓴다. 낙석 속박은 진영 종료 기믹 창에서 걸리므로 "끝난 진영 == 걸린 쪽"이면 +1을 한다. | `PeriodicStatusRules.ApplySlow`, `RockfallGimmickRule.BindEndCount` |
| 진영 종료 기믹은 국면을 `StartingPlayer`/`StartingEnemy`로 둔 채 돌고, 바로 뒤 `CleanupControllerTurn`이 끝난 진영의 행동 제한을 1 줄인다. 이때 `CombatTurnFacts.Controller`는 다음 진영을 돌려준다. | `CombatTurnCalculator.cs:94-98,174-200`, `CombatTurnFacts.cs:33` |
| 공격 결과의 피격 사실은 "공격 후 체력 ≤ 공격 전 체력 + 기록된 회복"을 검사한다. 회복은 `RecordRecovery`로 남긴 것만 센다. | `Contracts/Results/AttackHitResult.cs:58` |
| 변화 근거 판독기(`CombatChangeEvidenceReader`)는 사건 종류·발동 종류·체력 사슬을 빠짐없이 검사하고, 모든 명령에서 불린다. 낙석 생존자에게는 낙석 출처의 속박이 꼭 있어야 한다. | `CombatChangeEvidenceReader.cs:56-57,173-177,400,444-469`, `MovementUndoPolicy.Reconcile:55` |
| 캐릭터가 죽으면 `Copy`가 위치·회전·점유 칸을 비우고 영역을 `Excluded`로 옮긴다. | `CombatCommandCalculator.cs:496-505` |
| 처치 연쇄는 귀속과 상관없이 이번 명령에서 새로 죽은 적대 캐릭터 수를 센다. | `KillChainAttackCalculator.cs:56-57` |
| 공개 공격 미리보기도 `arrival.BeginComplete`로 명령 완료 정산을 지난다. | `CombatAttackCalculator.cs:248` |
| 전투 진입은 여러 칸 유닛을 거절한다(`DeferredMultiCellUnit`). | `CombatTurnCalculator.cs:57` |
| 진영 턴 시작은 `BeginControllerTurn`: 특성 수명(`TraitTurnLifecycle.BeginAndRecord`) → 이동 투영 → 지속 피해(`PeriodicStatusApplication.BeginSide`) → 완료 → 넥서스 증원. | `CombatTurnCalculator.cs:100-150` |
| 밤 종료 회수(필드 캐릭터를 손패·대기실로 돌려보내고 다시 배치)는 2026-11-10 밤 삭제 작업으로 없어졌다. 적은 전투 동안 필드에서 사망 말고는 빠지지 않는다. | [밤 삭제 설계](2026-11-10-remove-night-cycle-design.md) |
| 희생은 치명 피해를 제한하고 보호막을 1회 주는 선례다(출처 = 그 특성, 소모될 때까지 남음). | `SacrificeDamageSettlement.GrantShield` |
| 처치 골드·경험치는 사망 사건 출처의 첫 귀속 캐릭터가 보유 아군일 때만 준다. | `CombatDamageCredit.Recipient`, `ConfirmedKillCalculator` |
| 이동 되돌리기는 허용 목록 방식이다. 체력·사망 사건이 낀 이동은 지금도 되돌릴 수 없다. | `MovementEffectUndo.ForwardEventsSupported` |
| 특성 입력은 DB `*TraitAuthoring` → `*TraitData` → `TraitSpecAdapter` → Core `*TraitSpec` → 정의·런타임 상태 팩토리 → `TraitRegistry`로 흐른다. 미구현 특성을 막는 문 `DeferredCombatFeatures.IsImplementationPending`이 있다(지금 비어 있음). | `TraitSpecAdapter.cs`, `RegenerationTraitFactories.cs` |
| 유닛 진영 제한은 `UnitEligibilityRules.Require` 하나가 판단하고, 전투 준비(플레이어는 성장·병과 특성을 반영한 최종 정의로)와 보유 능력치 조회가 부른다. | `UnitEligibilityRules.cs`, `BattlePreparationCalculator.cs:90-99` |
| 특성 훅은 두 층이다. 능력 인터페이스(`IEffectAttackSucceededRule`·`IEffectKillChainAttackRule` 등)는 값 기여나 자기 상태 계획만 돌려준다. 남에게 주는 피해·사건 기록은 정해진 시점의 실행자가 공통 피해 경계로 처리한다. | `CombatEffectCapabilities.cs`, `ICombatEffectEngine.cs` |
| 처치 연쇄가 그 선례다. 능력은 "활성인가"만 답하고, `KillChainAttackCalculator.Resolve`가 공격 전달의 `ActualKillChain` 단계에서 "명령 시작 때 살아 있고 지금 죽은 적"을 사실 비교로 센다. 미리보기에서는 부르지 않는다(`!plan.PublicPreview`). | `KillChainAttackCalculator.cs:49-56`, `AttackDeliveryExecutor.cs:66-72` |
| 명령 완료 정산 순서: 약한 지반 붕괴 → 처치 특성 정산 → 사냥꾼 맞춤 → 최종 관찰 → `Finish`(영혼·사망·보상·승패). | `MovementProjection.cs:765-816` |
| 새 사망은 이미 사실에서 찾는다: "명령 시작 때 살아 있음 ∧ 지금 죽음 ∧ 아직 정산 안 함". 사망 직전 사실은 그 유닛 `Died` 사건의 맞기 전 사실이고, 없으면 명령 시작 사실이다. | `CommandDeathSettlement.cs:44-59` |
| 낙사 구분도 사건에서 한다: 같은 투영의 `CombatFallEvent` 대상은 영혼을 남기지 않는다. | `MovementProjection.cs:569-577` |
| 이동 투영의 환경 피해 입구 `ChangeHealthSteps`는 공통 경계(`EnvironmentDamage`)·포효 피격 수·사망 위치·미리보기 의존 기록을 하고, 장애물이면 장애물 피해와 화약통 폭발 연쇄까지 처리한다. | `MovementProjection.cs:488-567` |
| 이동 명령이 아닌 전이도 이동 투영으로 끝날 수 있다. 턴 시작이 `MovementProjection.ForArrival(...)` → 지속 피해 → `Complete()`로 끝난다. | `CombatTurnCalculator.cs:139-143` |
| 낙석과 기차는 이동 투영 없이 `CombatCommandCalculator.Finish`를 직접 불러 끝난다. 기믹 단계는 `StageGimmickComposition`이 차례로 모아 `Merge`한다. | `RockfallGimmickRule.cs:129`, `RailwayGimmickRule.cs:79`, `StageGimmickComposition.cs:99-112` |

## 3. 설계 요지

| 번호 | 내용 |
|---|---|
| D1 | 특성 키 셋: `Reconstruction = 490`, `SelfDestruct = 500`, `Fear = 510`. 적 전용 규칙 원본 `TraitOwnerPolicy.IsEnemyOnly(key)`(Contracts/Common). |
| D2 | 재구성은 "죽지 않음"으로 표현한다. 치명 피해가 들어오면 그 자리에서 HP를 최대치로 바꾸고 새 상태 `ReconstructionStatus`를 붙인다. 사망 사건을 만들지 않으므로 처치 보상·처치 연쇄·영혼·아이템 폐기가 자연히 일어나지 않는다. |
| D3 | 자폭·공포는 처치 연쇄와 같은 꼴로 처리한다. 능력 인터페이스 `IEffectDeathTraitRule`이 "죽을 때 무엇을 하나"만 기여하고, 실행자 `DeathTraitApplication`이 명령 완료 정산에서 "이번 전이에서 새로 죽은 보유자"를 사실로 찾아 실행한다. 따로 담아 두는 큐는 없다(§5). |
| D4 | 공포는 새 상태를 만들지 않고 기존 `Disarm` + `Bind`를 출처 = 공포 특성으로 함께 건다. 같은 키 제한이 겹치면 남은 횟수가 긴 쪽이 남는다(모든 행동 제한에 같은 규칙). |
| D5 | 넉백 공격의 직접 피해로 죽은 자폭·공포 보유자는 넉백 경로 끝 칸을 사망 위치로 삼아 그 칸에서 발동한다. 죽은 캐릭터는 위치가 비므로 실제로 옮기지 않고, 끝 칸을 사망 위치(`DeathAnchors`)에 쓰고 경로는 발동 사건의 칸 목록으로만 남긴다(§6.3). |
| D7 | 사망 특성을 확인하는 곳은 이동 투영의 명령 완료 정산 한 곳이다. 이동 투영으로 끝나지 않던 낙석은 턴 시작처럼 `MovementProjection.ForArrival(...).Complete()`로 끝내게 바꿔 같은 확인을 탄다(Q-ED2 A). 기차는 그대로 `Finish`로 끝나 확인을 타지 않는다. **2026-11-17 바뀜: 기차도 `ForArrival(...).Complete()`로 끝나 확인을 탄다(§17).** |
| D6 | 적 전용은 세 경계에서 막는다: DB 그룹 조립, 특성 성장 카탈로그 검사, `UnitEligibilityRules.Require`. |

## 4. 재구성

### 4.1 상태와 값

- 새 상태 키 `StatusEffectKey.Reconstructing = 19`, 새 상태 `ReconstructionStatus(origin, remainingOwnTurnStarts)`.
  - 수명 종류는 기존 `AffectedSideTurnStarts`를 쓴다.
  - 분류는 버프로 둔다(P12). Core 규칙은 분류를 읽지 않고 표시 순서만 바뀐다.
- `CombatActionAdmission.IsAttackRestricted`·`IsMovementRestricted`가 이 상태도 본다. 이 두 함수를 쓰는 AI·포효·공격 조회가 그대로 따라온다.
  - 거절 이유는 새 값 `MovementFailureReason.Reconstructing`과 공격 쪽 `AttackFailureReason`의 같은 값(`CombatAttackQueries:69`, `CombatAttackCalculator:62`). 적 전용이라 플레이어 안내에는 나오지 않는다.
  - 낙석 예보(`StageHazardForecast:247,251`)는 `AppliedStatusRules.IsRestricted`를 직접 써서 재구성을 보지 않는다. 행동 입장 함수로 바꾼다. 이 파일은 지금 커밋 전 AI 작업과 겹친다(§11).
- 재구성 중에도 강제 이동은 그대로 받는다. 넉백·끌어오기·토네이도 코드는 행동 제한을 읽지 않고 생사(`IsDead`)와 넉백 면역만 본다(`AttackKnockbackCalculator`·`PullAttackCalculator`·`TornadoCalculator`, 2026-11-10 확인). 재구성 중인 유닛은 HP가 1 이상이라 산 유닛과 똑같이 밀리고 끌려온다. 면역이면 지금처럼 밀리지 않는다.
  - 칸 효과도 그대로다. 낭떠러지로 밀리면 진짜 사망한다(낙사는 재구성을 발동하지 않음). 가시·지뢰 피해가 치명이면 이미 재구성을 썼으므로 진짜 사망한다.
- 전투당 1번 사용 여부는 특성 진행값 `ReconstructionTraitStatusSnapshot.Used`에 둔다.
- 단계 값(Q-ED8 A 기준)은 Core 한 곳 `EnemyDeathTraitLevels`에 둔다.

| 단계 | 회복 | 보호막 | 부활 넉백 |
|---|---|---|---|
| Lv1 | 최대 체력 100% | 없음 | 없음 |
| Lv2 | 100% | 최대 체력 50%(내림) | 없음 |
| Lv3 | 100% | 50% | 주변 8칸 1칸 (Q-ED6 A: Lv2 포함) |

### 4.2 발동

공통 피해 경계 `ApplySteps`의 체력 적용 자리(`CombatDamageApplication.cs:167-175`)에서 판단한다.

1. 보호막까지 적용한 뒤 실제 HP 손실이 현재 HP와 같으면(치명) 규칙 함수 `ReconstructionRules.CanTrigger(recipient)`를 묻는다.
   - 조건: 재구성 특성 보유, 아직 쓰지 않음, 필드에 있음.
2. 참이면 `Hurt` 대신 `ReconstructionRules.Trigger(recipient, level)`의 결과를 쓴다.
   - HP = 최대치, `ReconstructionStatus(남은 자기 턴 시작 2)`, `Used = true`, Lv2 이상이면 보호막.
3. 회복량(최대 체력 전부)을 `RecordRecovery`로 기록한다. 공격 결과의 "공격 후 체력 ≤ 공격 전 + 회복" 검사(`AttackHitResult.cs:58`)가 이 기록으로 통과한다. 직접 피격·처치 연쇄 결과가 모두 이 검사를 지난다.
4. 사건은 체력 사건·사망 사건 대신 새 캐릭터 사건 `Reconstructed`(맞기 전 사실 → 재구성 사실, 피해 출처) 하나와 발동 사건(`ReconstructionStarted`, 크기 = 잃은 HP)을 남긴다.
   - 변화 근거 판독기의 체력 사슬(`HasHealthChain`)이 `Reconstructed`를 "손실 = 맞기 전 HP, 회복 = 최대치"로 읽게 고친다. 고치지 않으면 모든 명령의 `ReadAll`이 예외를 낸다.
5. 그 뒤 단계(아이템 피격 후 효과, 사망 폐기, 처치 정산)는 바뀐 사실로 그대로 진행한다.
   - 위스키 같은 저체력 회복 아이템은 HP가 가득이라 발동하지 않는다.
   - 사망 폐기·처치 정산은 죽지 않았으므로 아무것도 하지 않는다.
6. 스테이지 기록의 "받은·넣은 피해"는 실제로 잃은 HP(맞기 전 현재 HP)로 기록한다.

같은 판단을 낙석 충돌(Q-ED2 A)에서도 부른다. 판단 원본은 `ReconstructionRules` 하나다. 낙사·기차 경로는 부르지 않는다.

- 낙석은 공통 경계를 거치지 않으므로 `RockfallGimmickRule.Impact`의 수신자 반복 안에서 직접 부른다. 치명이면 `Hurt` 결과 대신 `Trigger` 결과를 쓰고, 재구성한 유닛은 살아 있으므로 다른 생존자처럼 낙석 속박을 받는다.
- 낙석 근거 검사 `ValidateRockfallRecipients`(`CombatChangeEvidenceReader.cs:166-170`)는 수신자 바로 뒤에 `HealthChanged`/`Died`를 요구한다. 그 자리에 `Reconstructed`도 받게 고친다(뒤의 속박 검사는 그대로).

**바뀜(2026-11-10, §16): 즉사도 재구성이 버틴다.** 처음 판단은 다음과 같았다 — 즉사(`DefeatSteps`, 마무리 사격)는 Q-ED1 결정 B로 재구성을 발동하지 않는다. 즉사는 피해가 아니고 사용자가 정한 "즉사는 제외" 원칙과 같다. 또 즉사 발동 사건이 `Defeat`보다 먼저 기록되고 판독기가 즉사 대상마다 사망 사건을 요구하므로(`AttackDirectDamageCalculator.cs:94`, `KillChainAttackCalculator.cs:107`, `CombatChangeEvidenceReader.cs:383-386`), 발동하게 하려면 즉사 판단 앞에서 가로채야 한다.

**최우선:** 재구성이 발동하면 그 유닛은 죽지 않았으므로 명령 끝 확인(§5)에서 "새로 죽은 유닛"으로 잡히지 않는다. 순서를 따로 정할 필요가 없다. 재구성 중 다시 치명 피해를 받으면 `Used`라서 진짜 사망하고, 그때 자폭·공포가 발동한다.

### 4.3 보호막

- 희생 보호막과 같은 꼴: 출처 = 재구성 특성, `ShieldStatus(origin, 예산, eligible: true, …)`, 소모될 때까지 남는다(P4).
- 부활 때 지우지 않는다. "150% 내구"라는 기획 의도에 맞춘다.

### 4.4 부활

- `BeginControllerTurn`에서 시전자 진영의 턴이 시작될 때 `ReconstructionTurnApplication.BeginSide(projection, side)`를 부른다.
  - 자리: 이동 투영을 만든 뒤, 지속 피해(`PeriodicStatusApplication.BeginSide`) 앞(P3). 부활한 유닛이 일반 유닛으로서 턴 시작 효과를 받는다.
  - 남은 횟수를 1 줄이고, 0이 되면 상태를 지우고 부활 사건을 남긴다.
- 셈 규칙(P3): 발동 시점과 상관없이 "발동 뒤 자기 턴 시작 2번째"에 부활한다.
  - 플레이어 턴 발동 → 자기 턴 시작 1(불가) → 2(부활). 기획 예시와 같다.
  - 자기 턴 중 발동(반사 피해, 턴 시작 지속 피해) → 이번 턴 남은 부분 + 다음 자기 턴 불가 → 그다음 자기 턴 시작에 부활. 둔화·낙석의 "이미 시작한 턴은 세지 않는다" 원칙과 같다.
- 재구성 중 상태(화상·출혈·디버프 등)는 그대로 둔다. 사망이 아니므로 비우지 않는다(P5).

### 4.5 Lv3 부활 넉백

- 부활 순간 주변 8칸에 있는 캐릭터를 EntityId 순으로 1칸씩 바깥 방향(8방향)으로 민다(P13).
  - 넉백 면역(병과·강제 이동 면역·굳건한 의지)은 지금 공격 넉백과 같은 판정을 쓴다.
  - 칸 효과(낙사·가시·지뢰·오브젝트·약한 지반·토네이도)는 공격 넉백과 같이 칸마다 정산한다.
  - 장애물·구조물은 밀지 않는다(기획 "유닛").
  - 전투가 끝나면(승패 확정) 멈춘다.
- 넉백 기능은 지금 공격 계획(`CommandAttackPlan`)·공격 투영에 묶여 있다(`AttackForcedMovement.ExecuteUnitSteps`). 칸마다 정산을 이동 투영(`MovementProjection`) 입구로 꺼내 공격 넉백과 부활 넉백이 같이 쓴다. 세 특성 중 가장 큰 작업이라 따로 Phase로 둔다(§10 Phase E).

## 5. 사망 특성 확인 (자폭·공포 공통)

처치 연쇄와 같은 두 층으로 짠다(§2): 능력은 "무엇을 원하나"만 답하고, 실행자가 정해진 시점에 사실을 보고 실행한다. "누가 죽었나"를 따로 담아 두지 않고 그때 사실에서 찾는다.

### 5.1 능력 인터페이스

- 새 능력 `IEffectDeathTraitRule.EvaluateDeath(in EffectRuleInput)` → `EffectDeathTraitContribution`(자폭 비율 %, 공포 여부)이다. 엔진 입구 `ICombatEffectEngine.EvaluateDeathTraits(bindings)`가 기여를 모은다(`EvaluateKillChainAttack`과 같은 꼴).
  - 상태를 바꾸지 않는 평가다.
  - 자폭·공포 규칙이 이 능력을 구현하고, 단계 값은 `EnemyDeathTraitLevels`에서 읽는다(Q-ED8 A).
- 실행자는 특성 키를 모른다. 나중에 "죽을 때 ○○" 특성이 늘면 기여 칸과 실행 한 갈래만 더한다.

### 5.2 확인 지점

- 단 한 곳: 이동 투영의 명령 완료 정산 `CompletionWork.Build`에서 약한 지반 붕괴 뒤, 처치 특성 정산(`SettleTraitKills`) 앞에 새 단계 `CompletionDeathTraits`를 둔다.
  - 처치 연쇄(공격 전달 `ActualKillChain`)보다 뒤라서, 처치 연쇄는 폭발 사망을 세지 않는다(H4 해결).
  - 피격 체력 고정(`CaptureHitHealth`, 공격 전달 안)보다 뒤라서, 폭발 피해가 원 공격 결과에 섞이지 않는다(M1 해결).
  - 넉백·끌어오기·강제 도착이 모두 끝난 뒤라서, 사망 위치가 이미 정해져 있다. 큐 설계의 "넉백 대기" 표시가 필요 없다(M2 해결).
  - 처치 특성 정산·`Finish`보다 앞이라서, 폭발 사망도 같은 명령에서 영혼·처치 보상·승패 정산을 받는다.
- 이 정산을 지나는 전이: 공격 명령, 이동 명령, 포효 반응, 턴 시작(지속 피해), 토네이도 기믹, 그리고 바꾼 뒤의 낙석(D7, §5.6).
- 이 정산을 지나지 않는 전이: 진영 종료 정리(지금은 재생 회복만 해서 사망이 없음). 기차는 2026-11-17부터 지난다(§17). 진영 종료 정리에 피해가 생기면 그때 이동 투영으로 끝내게 바꾼다.

### 5.3 새로 죽은 보유자 찾기

`SettleNewDeaths`(§2)와 같은 식으로 사실에서 찾는다.

- 대상: 투영 입력(명령 시작) 때 살아 있음 ∧ 지금 죽음 ∧ 이번 확인에서 아직 실행하지 않음 ∧ 낙사 아님 ∧ `EvaluateDeathTraits` 기여 있음.
  - 낙사 구분은 같은 투영의 `CombatFallEvent` 대상이다(`ResolveDeathBoundary`의 `fallen`과 같은 식). 약한 지반 붕괴 낙사도 `ApplyFall`을 지나므로 같이 빠진다.
  - 즉사(`Defeat`)는 `Died` 사건이 남고 낙사가 아니므로 대상이다(Q-ED1 (2) A).
  - 재구성으로 살아난 유닛은 죽지 않았으므로 대상이 아니다(§4.2).
- 사망 직전 사실: 그 유닛 `Died` 사건의 맞기 전 사실, 없으면 투영 입력 사실이다(`SettleNewDeaths`와 같음). 효과 공격력(`CombatEffectiveStatsCalculator`)과 특성 기여는 이 사실로 읽는다.
- 사망 위치: `DeathAnchors`, 없으면 사망 직전 사실의 위치다. 넉백 끝 칸(D5)·이동 중 가시/지뢰 칸·끌려온 칸은 이미 `DeathAnchors`에 있다.
- 이번 확인에서 실행했는지는 실행자 안의 집합 하나로 센다. 확인은 전이마다 한 번만 돌고, 다음 전이의 입력에서는 이미 죽어 있으므로 전이를 넘어 두 번 실행되지 않는다.

### 5.4 실행 순서와 연쇄

- 한 바퀴: 찾은 보유자를 EntityId 순으로 하나씩 실행한다(P15). 한 유닛 안에서는 자폭 → 공포(P8). 자폭으로 죽은 대상은 공포 대상에서 빠진다.
- 피해는 이동 투영의 환경 피해 입구 `ChangeHealthSteps`로 준다(§6.1). 이 입구가 공통 경계·포효 피격 수·사망 위치·미리보기 의존 기록, 장애물 피해·화약통 폭발 연쇄까지 처리한다. 그래서 화약통과 자폭이 서로를 일으키는 경우(L2)도 따로 번갈아 비울 필요가 없다.
- 바퀴가 끝나면 다시 찾는다. 폭발·화약통으로 새로 죽은 보유자가 있으면 다음 바퀴, 없으면 끝이다. 재구성 보유자를 치면 그 자리에서 재구성이 발동한다(피해 경계 안).
- 승패가 정해지면(`projection.Terminal`) 남은 실행을 멈춘다(P6). 같은 공격으로 보스와 자폭 적이 함께 죽으면 폭발은 일어나지 않는다.
- 바퀴 사이에 사망 경계 정산(깃발 떨어뜨리기·처치 특성)이 필요한지는 구현 때 확인한다. `ResolveDeathBoundary`는 이동 정지도 하므로 그대로 부르지 않고, 정지 없는 입구가 필요하면 나눈다(Phase B 확인 사항).

### 5.5 미리보기·AI 평가

- 이동 투영에 "사망 특성 실행" 스위치를 둔다. 기본은 켬이고, 공개 미리보기(공격·이동)와 AI 행동 평가(`CombatActionAssessment`)가 만드는 투영만 끈다(P16).
  - 공개 공격 미리보기도 명령 완료 정산을 지나므로(`CombatAttackCalculator.cs:248`) 스위치가 필요하다(Q-ED7 A). 처치 연쇄의 `!plan.PublicPreview`와 같은 뜻이다.
  - 기본을 켬으로 둔 이유: 끄는 곳을 빠뜨리면 미리보기에 폭발이 보이는 것으로 드러난다. 켜는 곳을 빠뜨리면 실제 전투에서 조용히 폭발이 빠진다.
- 재구성은 피해 경계 안이라 미리보기에도 반영된다(Q-ED7 A).

### 5.6 낙석 (Q-ED2 A)

- `RockfallGimmickRule.Impact`의 끝을 `CombatCommandCalculator.Finish` 직접 호출에서 `MovementProjection.ForArrival(input.WithActor(EntityId.Empty), projected, events, state).Complete()`로 바꾼다. 턴 시작과 같은 길이다(§2).
  - 투영 입력은 충돌 전 사실이고 캐릭터 사전은 충돌 뒤 사실이다. 그래서 낙석 사망자가 §5.3의 "새로 죽은 유닛"으로 잡힌다.
  - 사망 위치는 `Died` 사건의 맞기 전 위치로 나온다. 지금처럼 사망 위치 사전을 넘길 수 있으면 넘긴다.
  - 환경 효과라 대체 공격자를 두지 않는 지금 규칙(`WithActor(EntityId.Empty)`)을 그대로 쓴다.
- 폭발·공포 사건은 모든 낙석 수신자 사건(체력 → 속박) 뒤의 완료 정산에서 나온다. 그래서 `ValidateRockfallRecipients`의 순서 검사를 고치지 않는다. 고칠 곳은 재구성 한 줄(§4.2)과 M4 완화(§7)뿐이다.
- `Finish` → `Complete`로 바뀌면서 낙석에도 처치 특성 정산·사냥꾼 맞춤이 돈다. 결과 차이는 Phase F에서 낙석 시험으로 확인한다.

### 5.7 남는 모서리

- 폭발 칸은 사망 칸을 뺀 주변 8칸이다. 실행 때 다른 유닛이 사망 칸에 들어와 있으면 그 유닛은 맞지 않는다. 지금 흐름(명령 끝 확인)에서는 생기지 않는 경우로 보고 남긴다.
- 이동 중 죽은 보유자도 명령이 끝난 뒤 터진다. 사건 순서상 폭발은 그 이동의 남은 사건 뒤에 온다(View는 사건 순서대로 재생).

## 6. 자폭

### 6.1 피해

- 대상: 사망 칸 주변 8칸에 걸친 캐릭터(진영 무관, 살아 있는 필드 캐릭터)와 구조물(본부·장애물 포함). 화약통 폭발과 같은 대상 규칙이다.
- 양: `floor(사망 직전 효과 공격력 × 비율 / 100)`, 비율은 단계 표(50/75/110).
- 경로(Q-ED3 A): 화약통 폭발과 같은 환경 피해 경로다. 대상마다 이동 투영 입구 `ChangeHealthSteps`(출처 = §6.2)를 부르고, 그 안에서 `CombatDamageKind.EnvironmentDamage`로 공통 경계를 지난다.
  - 방어력은 원래 적용되지 않는다.
  - 보호막은 적용된다.
  - 지형 엄폐·피해 감소 특성·약점 노출·아이템 피격 효과도 다른 환경 피해처럼 적용된다.
- 구조물·장애물도 같은 입구가 처리한다. 장애물이면 장애물 피해 → 화약통 폭발 연쇄, 그 밖의 구조물은 숫자 피해다(`MovementProjection.cs:542-565`).
- 화약통 폭발이 함께 하는 일도 같은 입구가 이미 한다. 그래서 따로 부르지 않는다.
  - 보스 포효 피격 수 기록(`RoarHitCounter.Record`).
  - 사망 위치 기록(`DeathAnchors`).
  - 미리보기 의존 기록(`TerrainQueryCells`, 장애물이면 `EffectTargetIds`).
- 미리보기·AI 평가 투영에서는 실행하지 않는다(§5.5).

### 6.2 귀속

- 출처: `CombatDamageOrigin(사망 칸, 자폭한 유닛, 새 공격 그룹)`.
- 결과(사용자 결정):
  - 폭발로 죽은 적의 처치 골드·경험치는 아무도 받지 않는다(귀속이 보유 아군이 아님).
  - 플레이어 처치 특성(사냥꾼·처치 연쇄·피의 부름 등)도 발동하지 않는다.
  - 자폭한 유닛 자신의 사망은 원래대로 그 유닛을 죽인 쪽에 귀속한다.
  - 귀속된 유닛이 죽어 있으면 처치 특성 정산(`TraitKillSettlement`)을 건너뛴다. 지금 광폭화 갈래(`TraitKillSettlement.cs:28-31`)는 귀속 유닛의 생사를 보지 않아, 자폭 유닛이 광폭화도 가지면 죽은 자신에게 보너스가 붙을 수 있다.
  - 기획에 알릴 것: 자폭 적이 주변 적을 함께 죽이면 플레이어는 그 처치 보상을 잃는다.

### 6.3 넉백 사망 위치 (Q-ED4)

죽은 캐릭터는 `Copy`가 위치·회전·점유 칸을 비우고 `Excluded`로 옮긴다(§2). 그래서 시체를 실제로 옮기거나 강제 이동 사건·넉백 장부 기록을 남길 수 없다(판독기의 이동 검사·공격 결과 검사가 실패한다). 대신 경로만 계산한다.

- `AttackKnockbackCalculator.ResolveSteps`에서 이번 공격의 직접 피해로 죽은 보유자(Q-ED4 A: `EvaluateDeathTraits` 기여가 있는 유닛만)는 넉백 경로를 계산만 한다.
  - 시작 칸은 직접 피격 직전 위치다. 직접 피해 단계가 이미 `DeathAnchors`에 써 둔 값이다(`AttackDirectDamageCalculator.cs:101`).
  - 진입 판정은 산 대상과 같은 `CanEnterUnit`(벽·판 밖·산 캐릭터·구조물이 막음)이다.
  - 칸 효과(낙사·가시·지뢰·오브젝트·약한 지반·토네이도)는 정산하지 않는다.
  - 넉백 면역이면 경로가 없다(그 자리).
  - 끝 칸으로 `DeathAnchors`를 덮어쓴다. 넉백 장부·Moved 사건은 남기지 않는다. 명령 끝 확인(§5.3)이 이 값을 사망 위치로 읽는다.
- 발동 사건 `CorpseDisplaced = 400`(출처 = 죽은 유닛, 특성 = 자폭 또는 공포, 칸 목록 = 시작 → 끝)을 이 넉백 단계에서 남긴다. 그래서 사건 순서상 폭발보다 앞에 온다. View는 이 칸 목록으로 시체가 밀리는 연출을 한다.
- 영혼도 끝 칸에 떨어진다. 사망 위치 하나를 영혼·폭발이 함께 쓴다.
- 끝 칸이 낭떠러지면 그 칸에서 발동한다. 이미 피해로 죽었으므로 낙사가 아니다.
- 넉백 도중 가시·지뢰로 죽은 경우와 끌어오기는 지금 코드가 이미 그 칸을 사망 위치로 기록한다. 바꿀 것이 없다.

## 7. 공포

- 대상: 사망 칸 주변 8칸에 걸친 살아 있는 필드 캐릭터(진영 무관). 구조물 제외.
- 효과: `ActionRestrictionStatus(Disarm, 출처 = 공포, n)` + `ActionRestrictionStatus(Bind, 출처 = 공포, n)`. 명령 끝 확인(§5.4)이 이동 투영에 반영하고 대상마다 특성 상태 사건을 남긴다(`ChangeDebuffs`와 같은 꼴의 투영 입구).
- 기간 n(P2): `1 + (종료 감소가 아직 남은 진영 == 걸린 쪽 ? 1 : 0)`.
  - "종료 감소가 아직 남은 진영"은 이번 처리 뒤에 행동 제한을 1 줄일 진영이다.
    - 진영 턴 중(`Player`/`Enemy` 국면, 턴 시작 처리 포함): 그 진영.
    - 진영 종료 기믹 창(`StartingPlayer`/`StartingEnemy`, 바로 뒤에 `CleanupControllerTurn`): 방금 끝난 진영. 이때 `Controller`는 다음 진영을 돌려주므로 쓰지 않는다. 낙석 `BindEndCount`와 같은 근거다.
    - 그 밖(배치 국면, 전투 진입): 없음.
  - 이 판단은 규칙 함수 하나(`ActionRestrictionTiming.PendingEndSide`)에 두고, 낙석 `BindEndCount`·둔화 `ApplySlow`가 같은 함수를 쓰게 맞추는 것도 검토한다.
  - 예: 플레이어 턴에 적이 죽음 → 플레이어 유닛 n = 2(다음 플레이어 턴 끝에 풀림), 적 유닛 n = 1(다음 적 턴 끝에 풀림). 플레이어 턴 종료 기믹(토네이도 착지 지뢰 등)에서 죽음 → 플레이어 유닛 n = 2, 적 유닛 n = 1.
  - 자기 턴 시작 지속 피해로 죽은 경우 그 진영 유닛은 이번 턴과 다음 턴을 잃는다. 이미 시작한 턴은 세지 않는다는 원칙의 결과다(§11에 남김).
- 겹침(P1): `AppliedStatusRules.Apply`의 행동 제한 갈래를 "같은 키면 남은 횟수가 긴 쪽, 같으면 새 것"으로 바꾼다.
  - 무장 해제·발목 꺾기·낙석 속박에도 같이 적용된다. 한 규칙으로 둔다.
  - 기존 시험 `Restrictions_RefreshRatherThanAdd_…`(남은 1인 First 위에 2인 Second → Second)는 긴 쪽 규칙으로도 그대로 통과한다.
  - 낙석 근거 검사는 생존자에게 낙석 출처 속박을 요구한다(`CombatChangeEvidenceReader.cs:173-177`, 게시 `CombatStateBuffer.cs:238`). 다른 출처의 더 긴 속박이 남으면 게시가 예외로 멈추므로, 검사를 "속박이 있고 남은 횟수가 낙석 값 이상"으로 바꾼다.
- 출처 유닛이 죽어 있어도 제한은 남는다. 만료 사건(`TraitTurnLifecycle`)은 죽은 출처 유닛 ID로 남는다(View 인계).
- 사건: 대상마다 특성 상태 사건 + 발동 사건(공포, `StatusApplied`, 대상 목록, 사망 칸).

## 8. 적 전용 검사 (B안)

| 경계 | 검사 | 실패 |
|---|---|---|
| DB 그룹 조립 `InGameDataBaseFactory` | 유닛 표에서 적 전용 특성을 가진 유닛의 진영 제한이 `EnemyOnly`인지 | DB 생성 예외(부트 실패) |
| 특성 성장 카탈로그 `TraitGrowthCatalogValidator` | 성장 정의·병과 후보·병과 기본 특성에 적 전용 키가 없는지 | 카탈로그 검사 예외 |
| 진영 판단 `UnitEligibilityRules.Require` | `owner == Player`인데 최종 정의 특성에 적 전용 키가 있으면 거절 | 전투 준비·보유 능력치 조회 예외 |

- 성장 카탈로그 검사는 성장 표가 있을 때만 돈다(`InGameDataBaseFactory.cs:93-131`). 병과 기본 특성 검사는 성장 표가 없어도 돌도록 자리를 확인한다. 빠지더라도 `UnitEligibilityRules`가 전투 준비에서 막는다.

- 규칙 원본은 `TraitOwnerPolicy.IsEnemyOnly` 하나다. 나중에 제한을 풀면 이 함수와 세 검사만 걷는다. 규칙 본문은 진영 중립으로 짜 두었으므로 바꿀 것이 없다.
- 진영 `Both` 유닛도 거절한다(B안 그대로).

## 9. 계약·사건 (View 인계 목록)

| 바뀌는 것 | 내용 | View가 할 일 |
|---|---|---|
| 특성 키 | `Reconstruction 490`, `SelfDestruct 500`, `Fear 510` | 특성 외형 표 등록(아이콘·이름·설명) |
| 상태 키 | `Reconstructing 19`(버프) | 상태 외형 표·HUD 설명 갈래, 아이콘 |
| 사건 종류 | `CombatEventKind.Reconstructed = 49`(캐릭터 사건: 맞기 전 사실 → 재구성 사실, 피해 출처 포함). 그 맞음에는 체력 사건·사망 사건이 없다. | 사건 형식 표 한 줄, 쓰러졌다 일어나는 연출(HP가 0까지 떨어졌다 가득 차는 표시) |
| 발동 종류 | `ReconstructionStarted 370`, `ReconstructionEnded 380`, `SelfDestructExploded 390`, `CorpseDisplaced 400`, 공포는 기존 `StatusApplied 160` | 특성 발동 연출 줄(폭발 이펙트는 칸 목록으로) |
| 부활 넉백 | 강제 이동 사건(원인 = 재구성) + `KnockbackApplied` | 기존 넉백 연출 재사용 |
| 시체 밀기 | 사건 없이 `CorpseDisplaced` 발동의 칸 목록(시작 → 끝)만. 넉백 단계에서 나온다. | 사망 연출 뒤 시체를 끝 칸까지 미는 연출 |
| 사건 순서 | 자폭·공포 발동과 그 피해·상태 사건은 전이의 명령 완료 정산에서 나온다. 같은 결과 안에서 원 공격·이동·낙석 사건이 모두 끝난 뒤, 영혼·보상 사건 앞에 온다. 연쇄는 바퀴마다 이어 붙는다. | 사건 순서대로 재생하면 된다. 공격 동시 묶음 밖이므로 묶음에 섞지 않는다. |
| 공포 표시 | 무장 해제·발목 꺾기 두 상태, 출처 = 공포 특성 | 출처 특성 이름으로 묶어 보일지 결정. `Disarm` 설명 문구가 지금 "행동 제한"임 |
| 만료 사건 | 출처 유닛이 죽은 상태일 수 있음 | 죽은 출처 처리 확인 |
| 거절 이유 | `MovementFailureReason.Reconstructing` | 적 전용이라 안내 없음. 모르는 값에서 예외를 내는 switch가 있으면 갈래 추가 |

공유 문서는 Core 전달 문서(View 작업자용)에 절을 더한다. 전달은 사용자가 한다.

### 9.1 Core에서 함께 고칠 빠짐없는 분기

새 열거 값은 Core 안에서도 막힌다. 각 Phase에서 함께 고친다.

| 자리 | 할 일 |
|---|---|
| `CombatCharacterEvent` 허용 종류(`CombatEvent.cs:131-134`) | `Reconstructed` 추가 |
| `CombatChangeEvidenceReader` 캐릭터 사건 갈래(`:56-57`), 체력 사슬(`:444-469`), 발동 종류 기본 갈래(`:400`) | `Reconstructed` 읽기, 발동 370·380·390·400 읽기 |
| 낙석 근거 검사(`:173-177`) | §7의 완화 |
| `AppliedStatusRules.Apply` 지원 상태(`:25`), `StatusEffectCatalog`, 상태 읽기 모델, `StatusEffectSourceSnapshot.RemainingAffectedSideStarts` | `ReconstructionStatus` |
| `CharacterStatusCompatibility` | 재구성 상태 투영(옛 형식에 대응이 없으면 건너뜀) |
| 특성 상태 분기(`TraitEffectProvider:221`, `CombatCommandEffects.Capture`) | 새 특성 진행값 |
| 효과 엔진(`ICombatEffectEngine`·`CombatEffectEngine`), 능력 목록(`CombatEffectCapabilities.cs`) | `IEffectDeathTraitRule`·`EvaluateDeathTraits`·`EffectDeathTraitContribution` |
| 이동 투영 생성 자리(공개 공격·이동 미리보기, `CombatActionAssessment`) | 사망 특성 실행 스위치 끄기(§5.5) |
| 시험 | `CombatChangeEvidenceCoverageTests`(사건 종류 전체), `CombatChangeEvidenceVariantTests`(발동 종류 전체), `StatusEffectContractTests:147`(키 범위), 특성 등록 수 두 곳 |

## 10. 단계(Phase 안)

| Phase | 내용 | 비고 |
|---|---|---|
| A 기반 | 키 셋, 상태 키, `TraitOwnerPolicy`, DB Authoring·Data·Adapter·Spec·Definition·RuntimeState·Registry, 적 전용 검사 셋. 세 특성은 `IsImplementationPending`으로 막아 둔다. | 동작 변화 없음 |
| B 공포 + 확인 | 능력 `IEffectDeathTraitRule`·엔진 입구, 실행자 `DeathTraitApplication`과 완료 정산 단계 `CompletionDeathTraits`, 미리보기·AI 평가 스위치, 행동 제한 "긴 쪽" 규칙, 공포 | 확인 구조를 가장 단순한 특성으로 먼저 검증. 바퀴 사이 사망 경계 정산 필요 여부 확인(§5.4) |
| C 자폭 | 폭발 피해(`ChangeHealthSteps`)·귀속·연쇄, 넉백 시체 밀기 | 화약통 폭발 선례 |
| D 재구성 | 가로채기·상태·보호막·부활(Lv1·Lv2), 행동 입장 | AI 작업 커밋 뒤(§11) |
| E 부활 넉백 | 강제 이동 칸 정산을 이동 투영 입구로 꺼내기, Lv3 | 가장 큰 작업 |
| F 기믹 연결·문서 | 낙석 끝을 `ForArrival(...).Complete()`로, 낙석 재구성·근거 검사 한 줄·M4 완화(§5.6), 즉사 확인(Q-ED1 (2)는 B에서 저절로), View 인계 절, 기획 공유 | 낙석 시험으로 `Finish` → `Complete` 차이 확인 |

Phase마다 멈추고 보고한다. 시험은 Core 시험 어셈블리(`ProjectL.Tests.Core`·`CoreIntegration`)에만 쓰고, 확인은 Test Runner로 한다.

## 11. 영향·주의

- **AI 작업과 겹침:** 지금 커밋 전인 적 AI 파일(`EnemyActionCandidates`·`EnemyAiTurnService`·`StageHazardForecast` 등)과 시험이 있다. 재구성은 행동 입장 함수로만 막으므로 AI 코드는 낙석 예보 한 곳(§4.1)만 바꿀 예정이다. "이동·공격이 둘 다 막힌 행위자"를 AI가 오류 없이 넘기는지 Phase D 전에 확인한다. 무장 해제 + 낙석 속박으로 이미 같은 상황이 날 수 있다.
- **AI의 생사 추정:** AI는 HP 0 예상을 사망으로 본다. 재구성 적이 치명 피해를 받을 위치를 피하지 않거나, 자폭 피해를 계산하지 않는다. AI 행동 평가 투영은 사망 특성을 실행하지 않는다(§5.5). 오류가 아니라 판단 품질 문제라 이번 범위에서 빼고 남긴다.
- **넥서스 증원·필드 적 상한:** 재구성 중인 적은 살아 있는 적으로 센다.
- **보스:** 지정 보스가 재구성을 가지면 진짜 사망까지 승리하지 않는다. 데이터로 넣을지는 기획이 정한다.
- **공포 기간 모서리:** 자기 턴 시작 지속 피해로 죽은 공포 보유자 옆의 같은 진영 유닛은 2턴을 잃는다(§7).
- **이동 되돌리기:** 바꿀 것 없음. 피해·사망이 낀 이동은 지금도 되돌릴 수 없다.
- **8칸 모양:** 화약통 함수 `CombatHazardAreas.PowderBarrelCells`와 같은 모양이다. 이름을 일반화한 함수 하나를 셋이 함께 쓴다(화약통은 위임).

## 12. 결정 (2026-11-10 사용자)

| 번호 | 질문 | 결정 |
|---|---|---|
| Q-ED1 | 마무리 사격 즉사(HP를 바로 0으로)에서 (1) 재구성이 발동하나? (2) 자폭·공포가 발동하나? | (1) **A 발동(2026-11-10 사용자, B에서 바꿈 — §16).** 처음 결정은 B(발동 안 함)였다. (2) A 발동: 공격 처치이고 사망 사건이 그대로 남는다. 명령 끝 확인이 낙사만 빼므로 따로 연결하지 않아도 된다(§5.3). |
| Q-ED2 | 낙석(최대 체력 비율 감소)으로 HP가 0이 되면 발동하나? | A 셋 다 발동. 처음 권장은 비용 때문에 망설였으나(낙석을 이동 투영 위로 옮기기·근거 검사 순서), 명령 끝 확인으로 바꾸면서 비용이 "낙석 끝을 `ForArrival(...).Complete()`로 + 재구성 한 줄"로 줄었다(§5.6, §15). |
| Q-ED3 | 자폭 피해에 방어 말고 다른 보정(지형 엄폐·피해 감소 특성·약점 노출·아이템)도 적용하나? | A 화약통 폭발과 같은 환경 피해 경로(방어만 없음). |
| Q-ED4 | 넉백 공격으로 죽은 대상의 사망 위치를 넉백 끝 칸으로 옮기는 대상은? | A 자폭·공포 보유자만. |
| Q-ED5 | (없어짐) 재구성 중 밤 종료 회수 | 밤 삭제(2026-11-10)로 회수가 없어져 정할 것이 없다. |
| Q-ED6 | 재구성 Lv3는 Lv2의 보호막 50%를 포함하나? | A 포함(누적). |
| Q-ED7 | 플레이어 공격 미리보기에 자폭·공포 결과를 보일까? | A 보이지 않음(처치 연쇄와 같음). 재구성은 보임. |
| Q-ED8 | 특성 값을 DB에 어떻게 넣나? | A 단계(1~3)만 넣고 값 표는 Core 한 곳. |

## 13. 설계에서 정한 것 (확인 대상)

| 번호 | 내용 |
|---|---|
| P1 | 행동 제한 겹침은 키마다 남은 횟수가 긴 쪽, 같으면 새 것. 모든 행동 제한에 적용. |
| P2 | 공포 기간 = 1 + (종료 감소가 아직 남은 진영 == 걸린 쪽 ? 1 : 0). 진영 종료 기믹 창에서는 방금 끝난 진영(§7). |
| P3 | 재구성 = 발동 뒤 자기 턴 시작 2번째에 부활. 부활은 턴 시작의 특성 수명 뒤, 지속 피해 앞. |
| P4 | Lv2 보호막은 희생 보호막 꼴(소모될 때까지, 부활 때 지우지 않음). 예산은 최대 체력의 50% 내림. |
| P5 | 재구성 중 상태·아이템은 비우지 않는다. |
| P6 | 승패가 정해지면 남은 사망 특성 실행을 멈춘다. |
| P7 | 폭발 귀속 = 자폭한 유닛(처치 골드·경험치 없음). 귀속 유닛이 죽어 있으면 처치 특성 정산을 건너뛴다. |
| P8 | 한 유닛의 사망 특성 순서는 자폭 → 공포. |
| P9 | 사망 특성은 능력 인터페이스 + 명령 끝 확인으로 처리한다(큐 없음). 확인 지점은 이동 투영 완료 정산의 약한 지반 붕괴 뒤·처치 특성 정산 앞 한 곳, 대상은 사실에서 찾은 "새로 죽은 보유자(낙사 제외)", 같은 유닛은 한 번, 바퀴 단위 연쇄. |
| P15 | 한 바퀴 안의 실행 순서는 EntityId 순이다(사망 순서가 아님). 사실에서 다시 만들 수 있는 순서로 정했다. |
| P16 | 이동 투영의 사망 특성 실행 스위치는 기본 켬이고, 공개 미리보기(공격·이동)와 AI 행동 평가 투영만 끈다. |
| P17 | 낙석은 `ForArrival(...).Complete()`로 끝내 명령 끝 확인을 탄다. 진영 종료 정리는 지금처럼 확인을 타지 않는다. 기차는 2026-11-17부터 탄다(§17). |
| P10 | 적 전용 규칙 원본 `TraitOwnerPolicy.IsEnemyOnly`, 검사 자리 셋(§8), `Both` 거절. |
| P11 | 자폭 대상 = 화약통 폭발과 같은 대상 규칙(캐릭터·구조물·장애물). 공포 대상 = 캐릭터만. |
| P12 | `Reconstructing` 상태 분류는 버프. |
| P13 | 부활 넉백은 캐릭터만, 바깥 8방향 1칸, EntityId 순, 공격 넉백과 같은 면역·칸 정산. |
| P14 | 여러 칸 유닛은 전투 진입이 이미 거절한다(`DeferredMultiCellUnit`). 그래서 "주변 8칸"·넉백 방향은 1칸 기준으로만 정하고, 따로 DB 검사를 두지 않는다. 여러 칸 유닛을 허용할 때 함께 정한다. |

## 14. 독립 검토 반영 (2026-11-10, 검토 에이전트 하나)

높음 4·중간 7·낮음 9. 핵심 주장(공격 결과 체력 불변식, 사망 때 위치 비움, 처치 연쇄 사망 수, `Starting*` 국면의 `Controller`, 낙석 근거 검사, 캐릭터 사건 허용 종류, 여러 칸 진입 거절)은 코드로 다시 확인했다.

| 번호 | 지적 | 반영 |
|---|---|---|
| H1 | 재구성 회복이 공격 결과 체력 검사와 변화 근거 판독기 체력 사슬에 걸려 예외 | §4.2: 회복을 `RecordRecovery`로 기록, 판독기가 `Reconstructed`를 읽게 고침 |
| H2 | 즉사는 발동 사건이 먼저 기록되고 판독기가 사망 사건을 요구 | Q-ED1을 둘로 나누고 재구성은 권장 B(발동 안 함) |
| H3 | 죽으면 위치가 비어 시체를 옮기거나 이동 사건을 낼 수 없음 | §6.3: 경로만 계산해 사망 위치와 `CorpseDisplaced` 칸 목록으로 남김 |
| H4 | 처치 연쇄가 귀속과 상관없이 사망을 세서 폭발 처치까지 셈 | §5.2: 공격 정산점을 처치 연쇄 뒤로 |
| M1 | 정산점이 피격 체력 고정 앞이라 폭발 피해가 원 공격 결과에 섞임 | H4와 같은 자리로 옮겨 해결, 반사 사망 때도 돎 |
| M2 | 이동 경계에서 꺼내면 직접 피격 자폭이 넉백 전에 원래 칸에서 터짐 | §5.1·§5.2: 넉백 대기 표시, 이동 경계는 건너뜀 |
| M3 | 진영 종료 기믹 창에서 `Controller`가 다음 진영이라 공포가 바로 풀림 | §7·P2: 종료 감소가 남은 진영 기준 |
| M4 | 긴 쪽 규칙이 낙석 근거 검사(낙석 출처 속박 필수)를 깸 | §7: 검사를 "속박이 있고 남은 횟수 ≥ 낙석 값"으로 |
| M5 | 낙석 발동은 낙석 근거 검사의 사건 순서를 깸 | Q-ED2 비용에 적음 |
| M6 | 공개 미리보기도 완료 정산을 지남 | §5.1: 미리보기 피해 경계는 큐를 끔 |
| M7 | 새 열거 값이 Core 빠짐없는 분기·시험에 막힘 | §9.1 표 |
| L1 | 진영 종료 정리는 따로 피해 경계를 만듦 | §5.2 문장 고침 |
| L2 | 화약통 큐와 서로 연쇄 | §5.2: 둘이 빌 때까지 번갈아 |
| L3 | 늦게 꺼내면 사망 칸에 다른 유닛이 있을 수 있음 | §5.2에 남김 |
| L4 | 포효 피격 수·미리보기 의존 기록 | §6.1 |
| L5 | 밤 종료 회수가 Lv2 보호막 예산을 0으로 | 없어짐: 밤 삭제(2026-11-10)로 회수가 없음 |
| L6 | 공격 거절 이유·낙석 예보·AI 생사 추정 | §4.1·§11 |
| L7 | 죽은 귀속 유닛에 광폭화 보너스 | §6.2·P7 |
| L8 | 여러 칸 유닛은 이미 진입 거절 | P14 고침 |
| L9 | 성장 표가 없으면 병과 기본 특성 검사가 빠질 수 있음 | §8 |

H4·M1·M2·M6·L2의 "반영" 칸은 2판(큐) 기준이다. 3판에서 어떻게 풀리는지는 §15.

## 15. 3판: 큐 → 능력 인터페이스 + 명령 끝 확인 (2026-11-10)

사용자 제안: "특정 상황이 끝날 때 피해 입은 유닛이 죽었는지 확인하고, 죽었으면 자폭·공포를 발동하는 인터페이스를 열면 안 되나." 코드를 대조해 보니 그 구조가 낫다고 판단해 바꿨다.

### 15.1 바꾼 이유

| 기준 | 2판(큐) | 3판(명령 끝 확인) |
|---|---|---|
| SSOT | "누가 죽었나"를 큐에 한 벌 더 적는다. 넣는 곳이 셋(공통 경계·즉사·낙석)이다. | 사실에서 찾는다. 같은 식이 이미 `SettleNewDeaths`·처치 연쇄에 있다. |
| 원인 구분 | 넣는 곳이 원인을 안다(낙사·기차는 안 넣음). | 사건에서 구분한다(`CombatFallEvent`). `ResolveDeathBoundary`가 이미 같은 식을 쓴다. 기차는 확인을 타지 않는다. |
| 넉백 위치 | "넉백 대기" 표시와 이동 경계 건너뛰기가 필요하다(M2). | 명령 끝에는 넉백 위치가 이미 정해져 있어 필요 없다. |
| 확인 지점 | 공격·이동 경계·명령 완료·낙석 넷 | 명령 완료 하나 |
| 화약통과의 연쇄 | 두 큐를 번갈아 비운다(L2). | 폭발 피해를 장애물까지 처리하는 `ChangeHealthSteps`로 주므로 따로 맞출 일이 없다. |
| 확장(OCP) | 큐가 자폭·공포를 안다. | 실행자는 능력 기여만 안다. 처치 연쇄와 같은 두 층이다. |
| 낙석 비용 | 낙석 충돌을 이동 투영 위로 옮기고, 근거 검사 순서를 폭발이 끼는 순서로 고친다. | 끝을 `ForArrival(...).Complete()`로 바꾼다(턴 시작 선례). 폭발이 수신자 사건 뒤에 오므로 순서 검사는 그대로 둔다. |

### 15.2 대신 생긴 것

- 사망 직전 사실을 `Died` 사건에서 다시 찾는다(`SettleNewDeaths`와 같은 방식이라 새 규칙은 아니다).
- 이동 투영에 사망 특성 실행 스위치가 하나 생긴다(§5.5). 2판에서 "미리보기 피해 경계는 큐를 끈다"던 것과 같은 크기다.
- 이동 중 죽은 보유자도 명령이 끝난 뒤 터진다(§5.7). 2판은 이동 경계에서 바로 터졌다.
- 한 바퀴 안의 순서가 사망 순서에서 EntityId 순으로 바뀐다(P15).
- 바퀴 사이 사망 경계 정산(깃발·처치 특성)의 필요 여부는 Phase B에서 확인한다(§5.4).

### 15.3 2판 검토 지적이 어떻게 되나

| 번호 | 3판 |
|---|---|
| H4 처치 연쇄가 폭발 사망을 셈 | 확인 지점이 공격 전달 뒤 완료 정산이라 처치 연쇄보다 늦다. |
| M1 폭발 피해가 원 공격 결과에 섞임 | 확인 지점이 피격 체력 고정보다 늦다. |
| M2 넉백 전 원래 칸에서 터짐 | 이동 경계에서 확인하지 않는다. |
| M5 낙석 근거 검사 순서 | 폭발이 모든 수신자 사건 뒤에 온다. 재구성 한 줄만 고친다(§4.2). |
| M6 공개 미리보기도 완료 정산을 지남 | 투영 스위치로 끈다(§5.5). |
| L1 진영 종료 정리 | 지금은 사망이 없어 확인을 타지 않는다. 피해가 생기면 이동 투영으로 끝내게 바꾼다(§5.2). |
| L2 화약통과 서로 연쇄 | `ChangeHealthSteps`가 장애물 연쇄까지 처리하고, 바퀴 단위로 다시 찾는다. |
| L3 사망 칸에 다른 유닛 | 명령 끝이라 지금 흐름에서는 생기지 않는다(§5.7). |

## 16. 구현 결과와 설계에서 바뀐 것 (2026-11-10)

Phase 0~F로 Core를 구현했다. 실행 기록·시험 수는 [Task 계획](../plans/2026-11-10-enemy-death-traits-plan.md) §9가 원본이다. 설계와 다르게 된 것:

| 자리 | 설계 | 구현 | 근거 |
|---|---|---|---|
| 재구성 판단 원본 | `ReconstructionRules` | `Domain/Traits/Rules/ReconstructionTraitRule`(다른 특성 규칙과 같은 자리) | Phase D |
| 사망 특성 실행 스위치 | 이동 투영 생성 인자 | `MovementProjection.DeathTraitsEnabled` 속성(공유 생성자 호출자를 바꾸지 않음) | Phase B |
| 공포 발동 대상 | 범위 안 대상 | 실제로 상태가 바뀐 대상만(판독기가 대상마다 상태 변화를 요구) | Phase B |
| P7(죽은 귀속 유닛) | 처치 특성 보상 없음 | 광폭화만 막음. 피의 부름·사기 진작은 그대로(반사로 서로 죽는 경우의 기존 동작 보존) | Phase C 시험 결과 |
| 부활 넉백 칸 정산(§4.5) | 공격 넉백 칸 정산을 이동 투영 입구로 꺼냄 | 이미 있던 이동 투영 강제 이동(타일 효과 경로)을 8방향 걸음·이동 원인 입구 `ResolveForcedStepsSteps`로 넓혀 씀. 공격 넉백은 면역 판정만 `KnockbackImmunityCheck`로 공유 | 2026-11-10 사용자 결정(Phase E). 두 경로의 칸 효과는 실질적으로 같다. |
| 진영 턴 시작 | — | `CombatTurnCalculator.BeginControllerTurn(input, rules)` 필수 인자(부활 넉백의 칸 규칙) | 2026-11-10 사용자 결정 |
| 부활 넉백 대상 | 주변 8칸 캐릭터 | 한 칸 캐릭터만. 여러 칸 유닛은 밀지 않는다 | 2026-11-10 사용자 결정 |
| 낙석 재구성 발동 사건 | — | 모든 낙석 수신자 사건 뒤에 둔다(판독기가 충돌 바로 뒤 수신자 사건을 차례로 요구) | Phase F |

낙석은 `MovementProjection.ForArrival(...).Complete()`로 끝낸다(D7·P17 그대로). 특성화 사례(사망·생존 섞임, 메인 사망 패배)의 사건·최종 상태·영혼·승패·기믹 상태가 바꾸기 전과 같다. 코드상 차이는 완료 정산이 하는 처치 특성 정산·사냥꾼 대상 맞춤이 낙석에도 돌게 된 것이다(다른 명령·턴 시작과 같아짐).

### 16.1 즉사도 재구성 발동 (2026-11-10 사용자 결정, Q-ED1 (1) B → A)

- 마무리 사격 즉사로 HP가 0이 되는 순간도 재구성이 버틴다. 즉사 처리 `CombatDamageApplication.DefeatSteps` 한 곳에서 가로챈다. 즉사를 쓰는 공격 직접 피해·처치 연쇄·희생 계획이 모두 이 함수를 지난다.
- 사건: 즉사 발동(`InstantDefeat`)은 그대로 남고, 사망 사건 대신 재구성 발동(370) + `Reconstructed`(피해 출처 = 공격)가 나온다. 판독기의 즉사 검사가 `Reconstructed`도 받는다. 처치 정산을 하지 않고, 공격 직접 피해는 사망 위치를 남기지 않는다.
- 즉사는 보호막을 무시하는 성질 그대로다.
- **희생이 즉사보다 먼저(2026-11-10 사용자 결정):** 희생 수호자가 지킬 수 있는 타격이면 수호자가 숫자 피해를 대신 받고, 원래 대상은 맞지 않으므로 마무리 사격 즉사가 터지지 않는다. 수호자에게는 즉사를 판정하지 않는다. 희생 계획은 즉사가 걸릴 대상을 치명 타격으로 센다(숫자 피해로는 살아남아도 지킴). 공격 직접 피해·처치 연쇄 추가 공격·희생 모의 계산이 판단 함수 `FinishingShotApplication.FiresOnHit` 하나를 부른다. 이전에는 즉사 판정이 먼저라 희생이 넘기지 않았다.
- 공격 범위 응답의 즉사 대상 목록(`InstantDefeatTargetIds`)은 판정 식 그대로라 재구성 보유자도 들어간다(즉사는 일어나고 재구성이 버팀).
- 이미 쓴 재구성은 즉사를 버티지 못한다.

## 17. 기차 즉사도 자폭·공포 발동 (2026-11-17)

사용자 결정(2026-11-17, 가시 폭탄 Phase 4 중): 기차가 친 유닛도 자폭·공포를 발동한다. 위치는 죽은 그 자리, 곧 치이기 전 레일 칸이다. §1.2의 "기차 제외", D7·P17의 "기차는 확인을 타지 않는다"를 대신한다.

- **구현:** 기차 즉사(`RailwayGimmickRule.TryAdvance`)를 낙석처럼 `MovementProjection.ForArrival(...).Complete()`로 끝낸다. 옛 길은 `CombatCommandCalculator.Finish` 직접 호출이었다.
  - 사망 특성 확인은 기존 명령 끝 한 곳(`DeathTraitApplication`) 그대로다.
  - 발동 칸은 이 확인의 기존 규칙 그대로다. 강제 이동 시체 칸이 없으면 사망 직전 사실의 위치를 쓰고, 기차는 시체를 옮기지 않는다.
  - 같은 길로 가시 폭탄도 사망 칸에서 터진다([가시 폭탄 설계](2026-11-17-cactus-cannon-thorn-bomb-design.md) R16).
- **그대로인 것:**
  - 낙사는 계속 제외다(같은 투영의 `CombatFallEvent`로 거른다).
  - 재구성은 계속 기차를 버티지 않는다. 기차는 공통 피해 경계를 거치지 않는 즉사이고, 이번 결정은 자폭·공포에만 해당한다.
  - 기차의 피해자 연속 기록과 환경 귀속(행위자 비움)도 그대로다.
- **확인:** Core·CoreIntegration 배치 실행 결과는 [가시 폭탄 구현 계획](../plans/2026-11-17-cactus-cannon-thorn-bomb-implementation-plan.md) Phase 4 후속 기록에 있다. 기차에 치인 자폭·공포 유닛의 자동 시험은 없다. 기차 연출 뒤 자폭 깜빡임·폭발 재생은 Unity 확인 항목이다.
