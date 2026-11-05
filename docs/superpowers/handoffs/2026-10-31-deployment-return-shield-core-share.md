# 배치 단계 손패 왕복 — 방어막 예산 유지 (Core 변경 공유)

- 날짜: 2026-10-31
- 받는 사람: 천명님(Core)
- 작업: 용근(View), 사용자 명시 지시 "코어 고치고"로 View 작업에서 Core를 고침
- 상태: 코드 완료, **컴파일 미확인**, 새 시험 없음, 기존 시험 변경 없음, 커밋 전, **미전달**
- 기록: [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §5.73(변경 내역), §56(질문 → 처리)

## 1. 요지

배치 단계에서 유닛을 손패에 넣었다가 다시 놓으면 방어막(단독 행동·결속)이 사라지던 것을 고쳤습니다.

- **배치 단계에서 손으로 손패에 넣을 때(`ReturnUnit`)만** 방어막의 남은 예산을 지우지 않고 자격만 끕니다.
- 다시 놓아 조건을 만족하면 넣기 전 예산 그대로 `ShieldGranted`가 납니다. 필드에서 끌어서 옮긴 것과 같은 결과입니다.
- 밤 종료 회수 등 다른 예비 구역 진입은 지금 규칙(예산 0) 그대로입니다.

## 2. 문제

| 같은 자리 바꾸기 | 바꾸기 전 | 바꾼 뒤 |
|---|---|---|
| 끌어서 옮기기 (`PlaceUnit` 필드 → 필드) | 방어막 있음 | 방어막 그대로 |
| 손패에 넣었다 다시 놓기 (`ReturnUnit` → `PlaceUnit`) | 방어막 있음 | **방어막 없음** (자기 진영 턴 끝 리셋 전까지) |

배치 단계에서는 자리를 고르는 중이라, 방법에 따라 결과가 달라지는 것은 플레이어가 예상하기 어렵습니다. View는 사실대로 HUD 아이콘·유지 이펙트를 비워 보였습니다.

## 3. 원인 (코드 경로)

1. `CombatPlacementCalculator.Return`(배치 단계 전용)
   → `Location(actor, Hand, …)`
   → `CombatReserveReturnCleanup.Clear`
   → `AppliedStatusRules.ClearForReserve`
2. `ClearForReserve`가 모든 `ShieldStatus`를 `(남은 예산 0, Eligible = false, 리셋 횟수 그대로)`로 남깁니다.
3. 다시 놓으면 `ShieldStatusRules.Reconcile`이 이전 예산 0을 그대로 씁니다(리셋 경계가 아니므로).
4. `TraitPresenceReconciler.RecordShieldActivations`는 `next.RemainingBudget == 0`이라 `ShieldGranted`를 내지 않습니다.

[위치 교대·정찰대·되돌리기 인계](2026-10-31-position-swap-recon-undo-core-view-handoff.md) §6의 "Visible shields are removed by retaining canonical `ShieldStatus` history with zero budget … Redeployment does not reconstruct a new budget."대로의 동작입니다.

## 4. 바꾼 것 (Core 세 파일)

| 파일 | 변경 |
|---|---|
| `Domain/CombatEffects/Rules/AppliedStatusRules.cs` | `ClearForReserve(current, keepShieldBudget)` 오버로드. 참이면 `RemainingBudget`을 그대로 두고 `Eligible = false`, 리셋 횟수 그대로. 옛 `ClearForReserve(current)`는 `false`를 넘김(옛 동작 그대로). |
| `Application/Combat/CombatReserveReturnCleanup.cs` | `Clear(character, events, bool keepShieldBudget = false)`가 위 함수에 넘김. |
| `Application/Combat/CombatPlacementCalculator.cs` | `Location(…, events = null, bool keepShieldBudget = false)`가 정리에 넘김. **`Return`만** `keepShieldBudget: true`로 부름. |

`Location`을 부르는 다른 곳(`RecallForNight`, `Place`, `RedeployEnemies`)은 인자를 넘기지 않아 기본값(옛 동작)입니다.

핵심 줄:

```csharp
// AppliedStatusRules.ClearForReserve
new ShieldStatus(s.Origin.Value, keepShieldBudget ? s.RemainingBudget : 0, false, s.LastResetControllerTurnCount)

// CombatPlacementCalculator.Return
var final = Location(actor, CharacterRuntimeArea.Hand, slot, null, null, events, keepShieldBudget: true);
```

## 5. 동작

| 경우 | 전 | 후 |
|---|---|---|
| 배치 단계, 손패에 넣는 순간 | `ShieldRemoved`(양 = 남은 예산), 예산 0·자격 꺼짐 | `ShieldRemoved`(양 = 남은 예산), **예산 그대로**·자격 꺼짐 |
| 손패에 있는 동안 | 흡수·표시 없음 | 흡수·표시 없음(자격이 없음 — 단독 행동·결속 모두 필드에 있어야 자격, `Absorb`·`ActiveShieldTotal`은 `Eligible`만 셈) |
| 다시 놓기, 조건 만족 | 예산 0 → `ShieldGranted` 없음 | 자격 꺼짐 → 켜짐 → `ShieldGranted`(양 = 남은 예산) |
| 다시 놓기, 조건 안 맞음 | 자격 없는 이력 | 자격 없는 이력(같음) |
| 밤 종료 회수 | 예산 0 | 예산 0(같음) |
| 밤 회수 뒤 배치 단계에서 손패 왕복 | 0 → 0 | 0 → 0(끌기와 같음) |
| 결속 이웃(필드에 남은 쪽)의 방어막 | 출처가 손패면 자격만 꺼지고 예산 유지 | 같음(이 경로를 지나지 않음) |

이벤트 순서(`Returned` → 정리 `TraitStateChanged`/발동 요약 → 장비 환급)는 바뀌지 않습니다.

## 6. 새 예산이 아니라 "넣기 전 예산"을 고른 이유

Core 전달 문서 §56 선택지 A에는 "다시 놓을 때 새 예산으로 시작"도 있었습니다. 이 방법은 고르지 않았습니다.

- 첫 배치에서는 예산이 보통 가득이라 두 방법의 결과가 같습니다.
- 밤 종료 회수 뒤의 배치 단계에서는 예산이 0인 유닛이 있습니다. 새 예산이면 손패 왕복만으로 보호막이 되살아나 끌어서 옮기기와 결과가 달라지고, 회수 규칙("다음 리셋까지 비어 있음")을 피하는 길이 됩니다.
- 손패에 계속 둔 유닛도 리셋 경계에서는 지금 규칙대로 다시 채워집니다(`Reconcile`은 자격이 없는 이력도 리셋함). 한 번도 놓지 않은 유닛은 처음 놓을 때 새 예산이므로, 남겨 둔 예산이 그보다 유리해지지는 않습니다.

## 7. 시험

- 새 시험 없음(View 작업 규칙).
- 기존 `ReserveReturnEffectResetTests`(`2bd95ad34`)의 두 시험은 `Location`을 직접 불러 기본값(예산 0)을 타므로 기대값 그대로 맞습니다.
  - `HandAndEnemyWaiting_ClearTemporaryEffects_KeepEquipmentAndHistory` — `(0, false, 4)` 그대로
  - `SameTurnRedeploy_DoesNotRefreshActionsTriggersOrShields` — `ActiveShieldTotal == 0` 그대로
- 배치 단계 `ReturnUnit`의 방어막을 확인하는 기존 시험은 찾지 못했습니다(`CombatCommonCommandTests`의 `shielded`는 보유 환경).
- 손패 왕복 사례 시험(`Return` → `Place` 뒤 예산·`ShieldGranted`)을 더할지는 천명님 판단에 맡깁니다.

## 8. View 쪽

- View 코드 변경 없음. 유지 이펙트는 상태를 따르므로([이펙트 따라가기·회수 설계](../specs/2026-10-31-combat-effect-follow-recall-design.md) FE5·FE10) 다시 놓으면 HUD 아이콘·유지 이펙트가 다시 나옵니다.
- 손패 카드의 상태 표시는 `RemainingEligibleBudget`(= 0)을 읽어 그대로 비어 있습니다.

## 9. 확인 부탁

1. 배치 단계 손 복귀만 예산을 남기는 이 규칙이 Core 방향과 맞는지.
2. 위치 교대·정찰대·되돌리기 인계 §6 문장을 고칠지(이 문서는 고치지 않았습니다).
3. 손패 왕복 시험을 Core 쪽에 더할지.

## 10. Unity 확인 순서(사용자)

1. 배치 단계에서 단독 행동 유닛을 놓아 보호막 HUD 아이콘·유지 이펙트가 나오는지 봅니다.
2. 우클릭으로 손패에 넣습니다 → 아이콘·이펙트가 사라집니다.
3. 조건을 만족하는 자리에 다시 놓습니다 → 아이콘·이펙트가 다시 나옵니다.
4. 조건이 깨지는 자리에 놓습니다 → 둘 다 없습니다.
5. 전투를 진행해 밤 종료 회수 뒤 배치 단계에서 같은 왕복을 합니다 → 회수로 비운 보호막은 다시 놓아도 없습니다(리셋 전까지).
