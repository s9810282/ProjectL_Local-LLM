# 이동 후 공격 허용 — Task 계획 (2026-10-27)

> 설계: [move-then-attack-design](../specs/2026-10-27-move-then-attack-design.md). 결정은 Q1 A · Q2 B · Q3 A · Q4 A, P1~P3 그대로(2026-10-27 사용자).
> 상태: **MA-a 코드 완료(2026-10-27, 컴파일 미확인, Unity Test Runner 미실행, 기록 §6.1).** MA-b는 사용자 "진행" 대기(TP-7).
> Phase는 둘이다. 각 Phase가 끝나면 멈추고 보고한다.
> - MA-a: 플레이어 규칙과 검사 세 곳
> - MA-b: 적 AI

## 0. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | 규칙 원본은 Contracts의 새 정적 클래스 `Contracts/Combat/UnitActionUseRules.cs` 하나다. `CanStartMove(UnitActionUse)`는 `Available`만 참이고, `CanStartAttack(UnitActionUse)`는 `Available`·`Moved`가 참이다. | 검사 세 곳 중 `AttackResult`(Contracts)와 `CombatStateBuffer`(Buffer 어셈블리)는 Application을 참조할 수 없다. 둘 다 참조하는 가장 낮은 곳이 `ProjectL.Core.Contracts`이므로 원본을 그곳에 둔다(SSOT). |
| TP-2 | `CombatActionAdmission.Actor`는 행동 종류를 인자로 받는다: `Actor(input, CombatCommandKind action, out actor)`. `Move`·`Attack` 외의 값이면 `ArgumentOutOfRangeException`이다. | 호출처가 둘(이동 입장·공격 입장)뿐이고, 행동 값 외의 검사는 그대로 함께 쓴다. |
| TP-3 | 옛 규칙("이동한 유닛은 공격 못 함")을 확인하던 시험은 지우지 않는다. 기대값을 새 규칙으로 뒤집는다. 이름이 옛 규칙을 말하면 이름도 바꾼다. 목록은 §2 T5다. | 같은 장면이 새 규칙을 확인하는 시험이 된다. 다른 사람이 쓴 시험은 없다(모두 천명님 작성). |
| TP-4 | 새 시험은 새 파일 둘에 둔다. 플레이어는 `MoveThenAttackTests.cs`, 적은 `EnemyMoveThenAttackTests.cs`이고, 둘 다 `ProjectL.Tests.Core`(`Assets/Tests/EditMode/` 바로 아래)다. 적 시험은 `EnemyMovementPathTests.Input` 도우미를 그대로 빌려 쓴다. | 기존 파일은 각자 주제가 있다. 새 규칙을 한 곳에서 찾게 한다. |
| TP-5 | 검증은 MSBuild를 돌리지 않는다. Phase마다 Unity Test Runner에서 돌릴 대상만 적는다(§4). | 사용자 상시 지시. |
| TP-6 | View 작업자 공유는 채팅에 붙여 넣을 짧은 글로만 준다. handoff 파일은 만들지 않는다. | 사용자 상시 지시. |
| TP-7 | MA-a는 바로 착수할 수 있다. MA-b는 사용자 "진행" 뒤에 착수한다. | 설계 §6이 적 난이도 변화 때문에 기획 확인을 권했다. |
| TP-8 | 계획 중 찾은 것: 현강님 `UnitHUD.cs:152`는 `ActionUse == Available`일 때만 "행동 가능" 표시(`actionedImage`)를 켠다. 그래서 이동한 유닛이 아직 공격할 수 있어도 표시가 꺼진다. 고치지 않고 공유 글에 넣는다(T11). | 다른 작업자 UI 파일이다. 규칙(무엇이 "행동 가능"인가)은 View 쪽에서 정할 일이다. |

## 1. 바꾸지 않는 것

- `UnitActionUse` 열거 값과 숫자. `PlayerTurnMovementTests:40`이 `ActionAlreadyUsed = 31`을 확인한다.
- 이동 정산(`MovementProjection.cs:113-118`)과 공격 정산의 최종 값 `Attacked`.
- 턴 시작 초기화(`CombatTurnCalculator.cs:86`).
- `GetAvailability`(`CombatActionAdmission.cs:66-75`). 두 입장을 그대로 부르므로 결과가 저절로 바뀐다. 이동한 유닛은 `MoveFailure = ActionAlreadyUsed`이고 `AttackFailure`는 비용·대상에 따른다.
- 적 추격 목표·길찾기·조준 점수(설계 P1), 적 예산 규칙(P2).
- 손패 복귀, View 코드 전부.

## 2. Phase MA-a — 플레이어 규칙과 검사 세 곳

### T1. 규칙 원본 `UnitActionUseRules` (새 파일)

- 위치: `Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/UnitActionUseRules.cs`. `.meta`는 Unity가 만든다.
- `public static class UnitActionUseRules`, 멤버는 둘이다.
  - `CanStartMove(UnitActionUse use) => use == UnitActionUse.Available`
  - `CanStartAttack(UnitActionUse use) => use == UnitActionUse.Available || use == UnitActionUse.Moved`
- XML doc: "한 진영 턴에 한 유닛은 이동 → 공격 순서로 둘 다 할 수 있다. 공격한 뒤에는 아무것도 못 한다. 행동 시작 조건의 유일한 원본이다." 설계 링크를 단다.

### T2. 입장 규칙을 행동별로 — `CombatActionAdmission.cs:14-33`

- 서명: `Actor(CombatCommandInput input, CombatCommandKind action, out CharacterCombatFacts actor)`.
- `:30` `Deferred` → `ActionDeferred`는 그대로다.
- `:31`을 `if (!(action == Move ? UnitActionUseRules.CanStartMove(...) : UnitActionUseRules.CanStartAttack(...))) return ActionAlreadyUsed;`로 바꾼다. `action`이 둘 다 아니면 메서드 맨 앞에서 예외를 던진다.
- 호출처 둘을 맞춘다.
  - `CombatMovementCalculator.cs:132` → `CombatCommandKind.Move`
  - `CombatAttackQueries.cs:21` → `CombatCommandKind.Attack`
- 클래스 XML doc의 "일반 행동의 입장" 설명에 "행동 값 조건은 `UnitActionUseRules`"를 한 줄 더한다.

### T3. 공격 결과 검사 — `AttackResult.cs:251-253`

- `BeforeActionUse != Available`을 `!UnitActionUseRules.CanStartAttack(BeforeActionUse)`로 바꾼다. `FinalActionUse != Attacked` 검사는 그대로다.
- 예외 문구: "일반 공격은 공격을 시작할 수 있는 행동 값(미사용·이동함)을 공격으로 소비해야 합니다."

### T4. Buffer 게시 검사 — `CombatStateBuffer.cs:275-277`

- `settlement.BeforeActionUse != UnitActionUse.Available` 조건을 최종 값별 판정으로 바꾼다.
  - 최종 `Moved`이면 `CanStartMove(Before)`
  - 최종 `Attacked`이면 `CanStartAttack(Before)`
  - 그 밖의 최종 값은 지금처럼 예외다.
- `:279-280`(최종 값과 결과 종류 일치)은 그대로다.
- `CombatStateBuffer`는 Buffer 파일이지만 Core 계약 검사를 옮겨 둔 곳이라 같은 Phase에서 바꾼다.

### T5. 기대값이 뒤집히는 기존 시험 (TP-3)

| 파일:줄 | 시험 | 지금 기대 | 바꿀 기대 |
|---|---|---|---|
| `EnhancementActionCostTests.cs:145-149` | `CategoryGrowthRemainsOriginal_AndMovementUsesTheOneActionRule` | 이동 뒤 공격 거절, 예산 8 | 공격 수락, 예산은 8에서 공격 비용만큼 준다. 이름에서 `OneActionRule`을 `MoveThenAttackRule`로 바꾼다. |
| `CombatCommandEffectTests.cs:203-212` | `RiderMove_IsFinal_AndAttackCannotRefundIt` | 거절 `ActionAlreadyUsed`, 예산 8 | 수락, 이동비 환급 없이 공격 비용만 준다. 이름은 `RiderMove_ThenAttack_DoesNotRefundMove`로 바꾼다. |
| `CategoryEffectRetirementTests.cs:56-69` | `RiderMoveThenAttack_DoesNotRefundSharedBudget` | 거절, 예산 8 | 수락, 예산 = 8 − 공격 비용. 이름은 그대로다. |
| `CombatCommonCommandTests.cs:350-367` | `MainAlsoHasOneAction_PerControllerTurn(attackFirst)` | 두 사례 모두 둘째 행동 거절 | `attackFirst = true`(공격 → 이동)는 거절 그대로다. `false`(이동 → 공격)는 수락이다. 이름은 `Main_ActionOrder_PerControllerTurn`으로 바꾼다. |
| `TargetingAngleLifecycleTests.cs:54` | `EnemyPathProjection_ChangesOriginNotForward` | 이동 뒤 `GetAttackRange` 실패 `ActionAlreadyUsed` | `ActionAlreadyUsed`가 아님을 확인한다. 실제 값은 실행해서 보고 넣는다. 시험의 본 목적(출발점이 바뀌고 전방은 그대로)은 그대로다. |
| `TargetingPreviewParityTests.cs:207-208` | `AttackPreview_DoesNotReplaySettledMovementHazards` | `CanAttack` 거짓, `ActionAlreadyUsed` | 두 줄을 새 규칙의 실제 미리보기 결과로 바꾼다. 본 목적(가시밭·체력·예산을 다시 적용하지 않음, `:209-211`)은 그대로다. 이제 공격 미리보기가 실제로 계산되므로 이 시험이 더 의미 있어진다. |
| `PlayerTurnMovementTests.cs:108` | `ZeroCost_ConsumesActionWithoutSpendingBudget(budget)` | 이동 뒤 `CanAttack` 거짓 | 예산 0이면 거짓(비용 부족), 10이면 참이다. 사례 인자로 기대를 나눈다. |

그대로 두는 시험(둘째 행동이 다시 이동이거나 공격 뒤의 행동이라 새 규칙에서도 거절):
- `PlayerTurnAttackTests:51-68`
- `CombatCommandEffectTests:511-520`·`:655-668`
- `CheckpointReturnTests:53-62`
- `MineEnvironmentTests:260-270`
- `IntimidationScheduleTests:38-49`
- `PlayerTurnMovementTests:104-107`·`:243-254`

`Framework/CoreStageCombatCycleTests.cs:218`은 Framework 시험이라 대상이 아니다.

### T6. 새 시험 `MoveThenAttackTests.cs` (TP-4)

1. 이동 → 공격 수락. 정산은 `Before = Moved`, `Final = Attacked`이고 예산은 이동비 + 공격비만큼 준다.
2. 이동 → 이동 거절(`ActionAlreadyUsed`).
3. 공격 → 이동 거절, 공격 → 공격 거절.
4. 이동 뒤 `GetAvailability`: `CanMove` 거짓, `CanAttack` 참(예산 충분).
5. 이동 뒤 예산이 공격 비용보다 적으면 공격 거절(`ActionCostInsufficient`).
6. `UnitActionUseRules` 표: 네 값 × 두 함수.
7. `AttackResult` 생성: `Before = Moved`는 통과하고, `Before = Attacked`·`Deferred`는 예외다.

Buffer 게시 성공(이동 → 공격)은 `CoreIntegration`에 기존 게시 시험 꼴이 있으면 그 파일에 사례 하나를 더한다. 없으면 기록만 하고 넘어간다(MA-a 안에서 판단).

### T7. MA-a 확인과 보고

- Unity Test Runner 대상은 §4 MA-a다.
- 보고에 적을 것: 바꾼 파일, 뒤집은 시험, 새 시험, Unity 결과 대기.

## 3. Phase MA-b — 적 AI (TP-7: 사용자 "진행" 뒤)

### T8. `EnemyAiTurnService.cs:28-45`

- 순회 조건 `c.ActionUse == UnitActionUse.Available`을 `UnitActionUseRules.CanStartAttack(c.ActionUse)`로 바꾼다(`Available`·`Moved`).
- 공격 시도(`:31-38`)는 그대로다.
- 추격 이동 앞에 `if (!UnitActionUseRules.CanStartMove(actor.ActionUse)) continue;`를 둔다. 이동한 적은 다시 이동하지 않는다.
- 주석: `:43` "한 일반 행동만 실행한다"를 "Advance 한 번에 수락된 행동 하나만 돌려준다. 같은 적의 이동 뒤 공격은 다음 Advance에서 이 순회가 다시 고른다."로 바꾼다. `:17` 요약도 같은 뜻으로 맞춘다.
- 보스 포효 대기(`:24-26`)는 그대로다.

### T9. 적 턴 시험

- 새 시험 `EnemyMoveThenAttackTests.cs`(TP-4):
  1. 추격 이동이 사거리 안에서 끝나면, 다음 Advance에서 같은 적이 공격한다.
  2. 사거리 밖에서 끝나면 그 적은 건너뛰고 다음 적 또는 `EnemyPhaseEnded`로 간다.
  3. 이동한 적은 다시 이동하지 않는다.
  4. 적 예산이 공격 비용보다 적으면 이동 뒤 공격이 없다.
  5. 앞 번호 적이 이동 + 공격을 끝낸 뒤 뒤 번호 적이 행동한다(순서).
- 기존 시험 확인: Advance 한 번만 보는 시험은 바뀌지 않는다. 예: `EnemyMovementPathTests:132-166`은 첫 Advance의 결과만 단언한다. 적 턴을 끝까지 돌리는 시험만 결과가 바뀔 수 있다. Test Runner로 §4 MA-b 대상을 돌린다. 실패하는 사례는 같은 원인(이동한 적의 추가 공격)인지 확인한 뒤 기대값을 새 규칙으로 맞춘다. 다른 원인이면 멈추고 보고한다.

### T10. 적 공용 코스트 설계에 "대신함" (설계 P3)

- `docs/superpowers/specs/2026-10-27-enemy-shared-actioncost-design.md` §3 5번 줄 끝에 한 줄을 붙인다: "→ 2026-10-27 [이동 후 공격 설계](2026-10-27-move-then-attack-design.md) Q2 B로 대신함: 적도 이동 뒤 공격한다."

### T11. View 작업자 공유 글 (TP-6, 채팅으로)

받는 사람은 전투 View 작업자, 현강님(`UnitHUD`)이다. 들어갈 것은 넷이다.
- 이동한 유닛도 공격할 수 있다. 공격 버튼·거절 안내는 Core 가능 조회를 따르므로 코드 변경이 필요 없을 것이다.
- `UnitHUD.cs:152` `actionedImage`는 `Available`일 때만 켜져, 이동한 유닛이 공격할 수 있어도 꺼진다(TP-8).
- `CombatActionStateText` "이동함" 문구를 그대로 둘지 정해야 한다.
- 적 호버 공격 범위는 지금 자리 기준이라 실제 위협(이동 + 사거리)보다 좁다. 위협 표시가 필요하면 Core 조회가 새로 필요하다.

### T12. 설계 문서 구현 메모

설계 문서 끝에 "§7 구현 메모"를 둔다. 바꾼 파일, 뒤집은 시험, 계획과 다르게 한 것을 적는다.

## 4. Unity Test Runner 대상

- **MA-a:** `ProjectL.Tests.Core` 전체. 최소로는 다음 시험이다.
  - `MoveThenAttackTests`, `PlayerTurnAttackTests`, `PlayerTurnMovementTests`
  - `CombatCommandEffectTests`, `CombatCommonCommandTests`, `CategoryEffectRetirementTests`, `EnhancementActionCostTests`
  - `TargetingAngleLifecycleTests`, `TargetingPreviewParityTests`, `CheckpointReturnTests`, `MineEnvironmentTests`, `IntimidationScheduleTests`
  - `CombatResultContractTests`
  - 그리고 `ProjectL.Tests.CoreIntegration` 전체(Buffer 게시 검사)
- **MA-b:** `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체. 적 턴을 Advance로 돌리는 시험이 많아(20곳 이상) 묶음 전체가 안전하다.

## 5. 커밋 묶음 (커밋은 사용자)

- MA-a: 새 `UnitActionUseRules.cs`·`MoveThenAttackTests.cs`(+ `.meta`), `CombatActionAdmission`·`CombatMovementCalculator`·`CombatAttackQueries`·`AttackResult`·`CombatStateBuffer`, 뒤집은 시험 일곱 파일. 규칙과 검사 셋은 한 커밋이어야 한다. 하나라도 빠지면 이동 → 공격이 Buffer 게시에서 예외가 난다.
- MA-b: `EnemyAiTurnService`, 새 `EnemyMoveThenAttackTests.cs`(+ `.meta`), 맞춘 적 턴 시험, 두 설계 문서. MA-a 커밋 뒤에 올린다.

## 6. 실행 기록

### 6.1 MA-a 코드 완료 (2026-10-27, 컴파일 미확인, Unity Test Runner 미실행)

사용자 지시: "TP-1~TP-8 그대로 하고 MA-a 진행해". MSBuild는 돌리지 않았다(TP-5).

**코드**
- T1: 새 `Contracts/Combat/UnitActionUseRules.cs`. `.meta`는 Unity가 만든다.
- T2: `CombatActionAdmission.Actor(input, CombatCommandKind action, out actor)`. 호출처 둘을 맞췄다(`CombatMovementCalculator.cs:132`은 `Move`, `CombatAttackQueries.cs:21`은 `Attack`). 다른 호출처는 없다(시험 포함).
- T3: `AttackResult.cs:251-253`이 규칙을 읽고, 예외 문구를 바꿨다.
- T4: `CombatStateBuffer.cs:275-282`이 최종 값별로 규칙을 읽는다(`startable`).
- 적 AI는 바꾸지 않았다. 여전히 `Available`인 적만 고르므로 적 동작은 그대로다(MA-b 몫).

**뒤집은 시험 (T5, 계획 표와 같음)**

| 파일 | 바꾼 것 |
|---|---|
| `EnhancementActionCostTests` | 이름 `…_AndMovementUsesTheMoveThenAttackRule`. 수락, 정산 `Before = Moved`, 예산 5. |
| `CombatCommandEffectTests` | 이름 `RiderMove_ThenAttack_DoesNotRefundMove`. 수락, 예산 5. |
| `CategoryEffectRetirementTests` | `RiderMoveThenAttack_DoesNotRefundSharedBudget`. 수락, 예산 5. 다음 턴 부분은 그대로. |
| `CombatCommonCommandTests` | 이름 `Main_ActionOrder_PerControllerTurn`. 공격 먼저는 거절·7, 이동 먼저는 수락·5. |
| `TargetingAngleLifecycleTests` | 범위 실패 `None`. 자금 부족은 범위 실패가 아니다(`CombatAttackQueries.Range`). |
| `TargetingPreviewParityTests` | `CanAttack` 참, `None`. 가시밭·체력·예산을 다시 적용하지 않는다는 본 단언은 그대로. |
| `PlayerTurnMovementTests` | `ZeroCost_…:108`. 예산 0이면 `ActionCostInsufficient`, 10이면 공격 가능. |

계획 목록 밖에서 "이동 뒤 공격 불가"를 단언하는 곳을 다시 찾았다(`CanAttack, Is.False`, `AttackFailure`, `actionUse: Moved`로 만든 유닛). 바뀌는 곳은 없었다. 모두 공격 뒤의 행동이거나, 다른 이유로 거절되거나, 값 복사를 확인하는 시험이다.

**새 시험 (T6)**
- 새 `Assets/Tests/EditMode/MoveThenAttackTests.cs`(`.meta`는 Unity가 만든다). 사례는 다음과 같다.
  - 이동 → 공격 수락과 정산(`Moved → Attacked`, 공격 3, 예산 5).
  - 이동 → 공격 뒤 공격·이동 모두 거절.
  - 이동 뒤 가능 조회(이동 불가, 공격 가능).
  - 이동 뒤 예산 부족이면 `ActionCostInsufficient`.
  - 규칙 표(네 값 × 두 함수).
- 계획과 다른 점:
  - `AttackResult` 생성자 단독 시험은 쓰지 않았다. 생성자 인자가 많고, 수락 쪽은 첫 사례가 실제 계산으로 지난다. 거절 쪽(`Attacked`·`Deferred`)은 입장 검사가 먼저 막아 계산에서 닿지 않는다.
  - 공격 → 이동·이동 → 이동 거절은 기존 시험이 확인하므로 새 파일에 다시 쓰지 않았다(파일 머리 주석).
- Buffer 게시: `CoreIntegration/CombatCommandPublicationTests.MoveThenAttack_PassesPublicationAndSpendsBothCosts`. 기존 `ConfiguredMovementBattle` 꼴을 쓴다.
  - 적 Demon은 (2,3)에 둔다. Cowboy가 (0,1)에서 (1,1)로 이동한 뒤 (2,3)을 공격한다.
  - 거리 (1,2)는 `CombatAdvanceServiceTests.TerminalAction_…`이 (0,1) → (1,3)으로 공격이 됨을 이미 보인 거리다.

**계획 중·구현 중 찾은 것 (보고만)**
- 적 공용 예산 최대치: 코드 `EnemyTurnRules.MaximumActionCost`는 **8**이다(`c40fd37b`, 용근님). 적 공용 코스트 설계 문서는 "10으로 사용자 확정"이라 적혀 있다. 이동 후 공격 설계 P2의 수치를 8로 고쳤다.
- **기존 실패 가능성(MA-a와 무관):** `c40fd37b`가 적 일반 행동에 적 예산을 매기게 했다. 그러나 `EnemyMovementPathTests.Input` 같은 적 시험 입력은 `enemyBudget`을 넘기지 않아 0이다(`CombatCommandState` 기본값). 그러면 적 이동(비용 2)이 `ActionCostInsufficient`로 거절된다. 또 `ActionCostSpent = 0`(무예산)을 단언하는 적 시험도 새 규칙과 맞지 않는다. `EnemyMovementPathTests`, `TargetingAngleLifecycleTests.EnemyPathProjection_…`(첫 `moved.Accepted`) 등은 이미 실패하고 있을 수 있다. 실행해 확인하지는 않았다. MA-b 적 턴 시험 정리 전에 이 실패부터 가려야 한다.

**Unity Test Runner 결과 (2026-10-27 11:04, 사용자 실행, `TestResults.xml`로 확인).** 3,896개 중 86개가 실패했다(Core 80·CoreIntegration 6).
- **이번 변경으로 생긴 실패: 0.** 뒤집은 시험 일곱, 새 `MoveThenAttackTests` 8사례, 통합 게시 사례 1이 모두 통과했다.
- 실패 86개는 모두 오늘 오전 다른 커밋에서 왔다.
  - **적 예산(`c40fd37b` 용근님 08:04) 81개.** Core 80개 + `CombatAdvanceServiceTests.EnemyAction_UsesNoPlayerBudgetAndPreservesPendingInput`이다. 원인은 둘이다.
    - 적 턴 입력을 직접 만드는 시험 도우미가 적 예산을 넘기지 않아 0이다. 그래서 적 공격·이동이 `ActionCostInsufficient`로 거절된다.
    - "적 행동은 무예산"을 단언하는 시험이 새 규칙과 반대다. 예: `EnemyAttack_IsUnbudgeted_…`, `ActionCost.Spent` 0.
  - **에셋 값·등록 변경 5개.**
    - 샷건 표식 `bonusDamagePercent` 10 → 30(`1eed25ca` 용근님): `HuntersMarkCardContentTests` 2개, `RetiredTraitAuthoringTests` 1개.
    - 카우보이 `extraRangeCells` 1 → 2(`ba9b05fe` 재령님): `PullAuthoringTests` 1개.
    - 보스 스테이지 2101이 `StageRootDataTable`에서 빠짐(`ba9b05fe` 재령님): `BossStagePreparationTests` 1개.
- `TargetingAngleLifecycleTests.EnemyPathProjection_…`은 예상대로 첫 이동 단언(`:46`)에서 실패했다. 적 예산 때문이다.

**Unity Test Runner 대상**은 §4 MA-a와 같다.

### 6.2 적 예산 실패 81개 정리 (2026-10-27, 컴파일 미확인, Test Runner 재실행 대기)

사용자 지시: "적 예산 때문에 깨진 시험 81개를 여기서 정리해줘. 테스트 도우미는 적 예산 Full, 무예산을 단언하던 시험은 새 규칙으로". 이동 후 공격 계획 밖의 일이다. 하지만 MA-b 적 턴 시험을 보려면 먼저 풀어야 하므로 여기에 기록한다. 코드(Core)는 바꾸지 않고 시험만 바꿨다.

**적 턴 입력 도우미에 적 예산 Full.** 적 진영 시작이 이미 예산을 채운 상태를 뜻한다(적 공용 코스트 설계 §3 1번).
- `EnemyMovementPathTests.Input`
- `ThornHeartTestData.Input`(`ThornHeartReflectionDirectTests.cs`)
- `EnemyControllerPhaseTests.Input`: `Enemy` 단계일 때만. 다른 단계는 원본 그대로다.
- `TornadoEnvironmentTests.Input`
- `AttackTargetAdmissionTests` 공격 입력 도우미
- `CombatCommonCommandTests.AutomaticInput`
- `CombatStageRecordTests.EnemyAttack`
- `PlayerTurnAttackTests.EnemyZeroDamage…` 안의 상태

**상태를 복사하는 공용 도우미가 적 예산을 이어받게 했다.** 생성자로 복사하면서 `enemyBudget`을 빼먹어 0으로 돌아가던 곳이다. 대상은 다음과 같다.
- `StaticTerrainIntegrationTests.Terrain`
- `HighGroundWallBypassTests.WithHighGround`
- `WallMovementTests.WithWalls`
- `ObstacleReactionTests.Copy`
- `WallAttackAdmissionTests.Input`
- `CliffFallTests.WithEnvironment`
- `CombatExperienceSettlementTests.Battle`
- `ThornHeartReflectionCommandTests.MainAndDesignatedBossDie…` 안의 복사

실패하지 않던 시험 안의 개별 복사는 그대로 두었다.

**무예산을 단언하던 시험 → 새 규칙**

| 시험 | 바뀐 단언 |
|---|---|
| `PlayerTurnAttackTests.EnemyZeroDamageAttackPaysEnemyBudgetAndConsumesAction`(이름 바꿈) | `EnemyBudget` 지출 3, 잔액 8 → 5. 플레이어 예산은 그대로. |
| `CombatCommonCommandTests.EnemyAttack_PaysEnemyBudget_AndTerminalAttackDoesNotPublishDay`(이름 바꿈) | 지출 3, 적 잔액 −3. |
| `ThornHeartReflectionCommandTests.EnemyOrdinaryHit_ReflectsOnce_AndPaysOnce` | 지출 0 → 3, 적 잔액 −3. |
| 같은 파일 반사 사망 사례(`:77`) | 지출 0 → 3. |
| `EnemyMovementPathTests.InstalledAllowanceFive_…` | 이동 지출 4, 적 잔액 −4. |
| `PlayerBudgetDoesNotLimitEnemyMovement`·`HighGroundDoesNotAddAnEnemyBudgetCharge` | 이동 지출 = 기본 이동 비용(고지대가 더 얹지 않음). |
| `CoreIntegration/CombatAdvanceServiceTests.EnemyAction_UsesNoPlayerBudgetAndPreservesPendingInput` | `EnemyBudget` 지출 > 0, 적 잔액 = 전 − 지출. 플레이어 예산 그대로는 유지. |

포효 반응(`ThornHeartReflectionCommandTests:174`)은 지금도 무예산 반응이라 그대로 두었다.

**남은 실패 5개(에셋 값·등록, 이번 범위 밖)**는 그대로다. 샷건 표식 10 → 30, 카우보이 끌어오기 1 → 2, 보스 스테이지 2101 미등록이다.

**Test Runner 대상:** `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체. 기대는 실패 5개(위 에셋)만 남는 것이다.

**재실행 결과 (2026-10-27 11:17, `TestResults.xml`).** 3,896개 중 9개가 실패했다. 81개 중 77개가 풀렸다.
- 남은 4개는 `AttackTargetAdmissionTests.HostilePrimary_IsAllowedForBothOwners`(직군 넷)다. `:56`이 적 공격 비용을 0으로 기대했다(무예산 단언을 하나 놓침). 적도 직군 비용(Cannon 5, 나머지 3)을 적 예산에서 내도록 고쳤다. 플레이어 예산은 플레이어 공격만 줄고, 적 예산은 적 공격만 준다. 재실행 대기.
- 나머지 5개는 위 에셋 값·등록 문제 그대로다.

**재실행 결과 (11:19).** 실패 5개로, 적 예산 81개는 모두 통과했다.

### 6.3 기획 값 고정 시험 정리 (2026-10-27, 컴파일 미확인, Test Runner 재실행 대기)

사용자 지적: "SO로 기획자가 값을 바꾼다고 테스트가 틀려버리는 건 의도와 다르다". 지시: "권장안대로 고쳐줘. 원본 비교는 읽은 값과, 이전 기록용 수치 고정은 빼고 구조만 남겨줘. 보스 스테이지 시험은 그대로 두고".

원칙은 둘이다.
- 값이 끝까지 전달되는지는 시험이 만든 SO 복사본에 임의 값을 넣어 확인한다.
- 운영 에셋 시험은 규칙만 본다(필수 등록, 참조, `Validate`, 아군 자격, 1×1 크기). 기획 수치는 고정하지 않는다.

| 시험 | 바꾼 것 |
|---|---|
| `HuntersMarkCardContentTests.OwnedDescription_…` | 끝의 "원본은 그대로" 단언을 숫자 10 대신, 시작 때 읽어 둔 원본 값과 비교한다. 본문(복사본 10·25 전달)은 그대로다. |
| `PullAuthoringTests.ProductionCowboy_UsesRegisteredPull`(이름 바꿈) | 능력치·직군·끌어오기 칸 수·지정 방식 모양 고정을 뺐다. `Validate`, 1×1, 끌어오기 특성 있음·어댑터 읽기, 공격 프로필 읽기, 특성 외형 등록을 남겼다. |
| `RetiredTraitAuthoringTests.ProductionShotgun_UsesMarkTrait`(이름 바꿈) | 능력치·직군·지정 방식 키·행동 플래그·특성 수·표식 피해량 고정을 뺐다. `Validate`, 1×1, 표식 특성 있음·어댑터 읽기를 남겼다. |
| `RetiredTraitAuthoringTests.CowboyDefinition_NoFrenzyAndCatalogStillBuilds` | 같은 파일에서 지금은 통과하지만 같은 문제인 카우보이 능력치 고정(`:29-30`)을 뺐다. 특성·직군은 고정값 대신 "정의 → 보유 능력치로 그대로 이어짐"으로 바꿨다. 끌어오기 포함 여부는 남겼다. |
| `ImpactBoltAuthoringTests.ProductionBallista_UsesImpactBoltAsPlayerUnit`(이름 바꿈) | 지금은 통과하지만 같은 문제다. 능력치·지정 방식·스플래시·벽 정책·충격 거리·넉백 면역 고정을 뺐다. `Validate`, 키, 충격 볼트 특성 있음·어댑터 형식, 1×1, `Regular`·`PlayerOnly`를 남겼다. |

- 그대로 둔 것:
  - `PiercingShotCommandTests.ProductionBallistaProfile_…`: 이름과 달리 운영 에셋을 읽지 않고 시험이 값을 직접 만든다. 앞서 "함께 정리"를 권한 것은 잘못 읽은 것이었다.
  - `BossStagePreparationTests`: 지시대로 그대로 둔다. 실제 등록 누락(`ba9b05fe`)을 잡는 시험이다.
- 보고만: `PullAuthoringTests.SharedRadialMethodAndItsOtherConsumers_AreUnchanged`는 "어느 유닛이 지정 방식 1을 쓰는가"를 고정한다. 수치는 아니지만 기획 배정이라 같은 종류의 깨짐이 날 수 있다.

**Test Runner 기대:** 실패는 `BossStagePreparationTests` 하나만 남는다.

**재실행 결과 (2026-10-27 11:24, `TestResults.xml`).** 3,896개 중 3,895개가 통과했다. 실패는 `BossStagePreparationTests.ProductionBossStage_PreparesAndPlacesBoss` 하나로, 기대대로다. 원인은 보스 스테이지 2101이 `StageRootDataTable`에서 빠진 것(`ba9b05fe`)이며 재령님 확인이 필요하다. MA-a, 적 예산 정리, 기획 값 고정 정리가 모두 통과했다. 다음은 MA-b이고 사용자 "진행"을 기다린다(TP-7).

### 6.4 MA-b 코드 완료 (2026-10-27, 사용자 "MA-b 진행해", 컴파일 미확인, Test Runner 대기)

- **T8:** `EnemyAiTurnService.cs`의 순회 조건을 `UnitActionUseRules.CanStartAttack`으로 바꿨다(`Available`·`Moved`). 추격 이동 앞에 `if (!UnitActionUseRules.CanStartMove(actor.ActionUse)) continue;`를 두었다. 요약 주석과 "계산 한 번에 행동 하나" 주석도 고쳤다.
- **T9 기존 시험 확인:** 같은 적 턴에서 Advance를 다시 부르는 시험은 둘이다. `BossTurnTests.PendingRoar_…`은 보스가 일반 행동을 하지 않고, `WallAttackAiTests.EnemyAcrossWall_…`은 `NextEnemyTurn`으로 새 턴을 연다. 둘 다 영향이 없다고 봤다. 통합 시험의 Advance 횟수·순번 단언은 한 번 진행만 본다. 나머지는 Test Runner로 확인한다.
- **T9 새 시험:** 새 `Assets/Tests/EditMode/EnemyMoveThenAttackTests.cs`(`.meta`는 Unity가 만든다). 사례는 넷이다.
  - 사거리 안에서 멈춘 추격 → 다음 Advance에서 같은 적이 공격한다(정산 `Moved → Attacked`, 적 예산 8 − 2 − 3).
  - 사거리 밖에서 멈춤 → 다시 이동하지 않고 적 단계를 끝낸다.
  - 이동 뒤 적 예산이 2만 남으면 공격 없이 적 단계를 끝낸다.
  - 앞 번호 적이 이동·공격을 마친 뒤 다음 적이 행동한다.
- 시험 도우미 `EnemyMovementPathTests.Maze`를 `private` → `internal`로 바꿨다(새 시험이 같은 지뢰 미로를 쓴다).
- **T10:** 적 공용 코스트 설계 §3 5번에 "대신함"을 적었다.
- **T11:** View 공유 글은 채팅으로 넘긴다(TP-6).
- **T12:** 설계 §7 구현 메모.
- **Test Runner 대상:** `ProjectL.Tests.Core`·`ProjectL.Tests.CoreIntegration` 전체. 기대는 실패가 `BossStagePreparationTests` 하나뿐인 것이다.
- **커밋 묶음(MA-b):** `EnemyAiTurnService.cs`, 새 `EnemyMoveThenAttackTests.cs`(+ `.meta`), `EnemyMovementPathTests.cs`, 설계 문서 둘.
- **Test Runner 결과 (2026-10-27 11:30, `TestResults.xml`):** 3,900개 중 3,899개가 통과했다. 새 적 시험 4개가 모두 통과했고, 기존 적 턴 시험도 깨진 것이 없다. 실패는 `BossStagePreparationTests` 하나로 기대대로다(보스 스테이지 2101 미등록, `ba9b05fe`, 재령님 확인 필요). **이동 후 공격(MA-a·MA-b) 끝.**
- 결과에서 `EnemyMovementPathTests`처럼 적 예산 때문에 실패하는 시험은 위 항목(이번 변경 전부터)으로 나눠 본다.
- `TargetingAngleLifecycleTests.EnemyPathProjection_ChangesOriginNotForward`는 이번에 마지막 줄만 바꿨다. 실패한다면 첫 이동 단언에서 실패할 것이다.
