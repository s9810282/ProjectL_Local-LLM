# 이동 후 공격 허용 — 결정 목록과 권장안 (2026-10-27)

> 상태: **MA-a·MA-b 구현 완료(2026-10-27). Unity Test Runner로 둘 다 확인했다(3,899/3,900, 남은 1개는 관계없는 보스 스테이지 등록 누락).** 구현 메모는 §7.
> **결정 완료(2026-10-27 사용자 "Q2만 B로 바꾸고 나머지는 A로 해줘"):** Q1 A · Q2 B · Q3 A · Q4 A. Q2 B의 AI 설계는 §2 Q2 아래에 있다. 그중 P1~P3은 확인 대상이다.
> 요청: 지금은 한 유닛이 한 턴에 이동과 공격 중 하나만 할 수 있다. 이것을 "이동 후 공격"도 되게 바꾼다.

## 1. 지금 규칙

- 유닛의 한 턴 행동은 값 하나 `UnitActionUse`가 기록한다.
  - 값은 `Deferred` · `Available` · `Moved` · `Attacked` 넷이다.
  - 진영 턴이 시작되면 그 진영의 필드 유닛은 모두 `Available`이 된다(`CombatTurnCalculator.cs:86`).
- 이동이 끝나면 `Moved`가 된다(`MovementProjection.cs:115-117`). 공격이 끝나면 `Attacked`가 된다(`CombatAttackCalculator.cs:150·206`).
- 아래 세 곳이 "행동 전 상태는 `Available`이어야 한다"를 검사한다.

| 위치 | 검사 |
|---|---|
| `Application/Combat/CombatActionAdmission.cs:31` `Actor` | `Available`이 아니면 `ActionAlreadyUsed`. 이동 입장(`CombatMovementCalculator.cs:132`)과 공격 입장·범위 조회(`CombatAttackQueries.cs:21`)가 함께 쓴다. |
| `Contracts/Results/AttackResult.cs:251-253` | 공격 결과의 행동 정산은 `Available → Attacked`여야 한다. |
| `BufferSystems/Combat/CombatStateBuffer.cs:271-273` | Buffer 게시 때 정산의 행동 전 상태가 `Available`이어야 한다. |

- 행동 정산 값 `ActionCostSettlementSnapshot`은 행동 전·후 값을 열거 범위로만 검사한다. 그래서 고칠 필요가 없다.
- 적 AI(`EnemyAiTurnService.cs:28-45`)는 `Available`인 적을 하나 골라 공격을 시도하고, 공격이 안 되면 추격 이동을 시도한다. 행동이 하나 수락되면 그 Tick을 끝낸다.

## 2. 결정할 것과 권장안

### Q1. 허용하는 순서 — **결정 A**

| 안 | 내용 | 필요한 변경 |
|---|---|---|
| **A (권장)** | 이동 → 공격만 허용한다. 공격한 뒤에는 이동할 수 없다. | 값 하나로 된다. 공격 입장이 `Moved`도 받으면 된다. `Attacked`면 이동 입장이 거절하므로 공격 후 이동은 저절로 막힌다. |
| B | 이동 → 공격과 공격 → 이동을 모두 허용한다. | 값 하나로는 부족하다. "이동했음"과 "공격했음"을 따로 기록하는 구조로 바꿔야 한다. 그러면 Buffer·View HUD 문구·시험이 넓게 바뀐다. |

권장 이유: 기획 요청이 "이동 후 공격"이고, A는 기존 값을 그대로 쓴다. B가 필요해지면 그때 구조를 바꾼다.

### Q2. 적 AI — **결정 B (적도 이동 뒤 공격)**

| 안 | 내용 |
|---|---|
| A (처음 권장) | 적은 지금처럼 행동 하나만 한다. AI 코드는 바꾸지 않는다. 플레이어 쪽 규칙만 넓어진다. |
| **B (결정)** | 적도 이동 뒤 공격한다. 추격 이동이 수락된 뒤 같은 적의 공격을 한 번 더 시도한다. |

#### B의 AI 규칙

지금 AI는 `Advance` 한 번에 수락된 행동 하나만 돌려준다(`EnemyAiTurnService.cs:28-45`). 이 틀은 그대로 둔다. 바꾸는 것은 "어떤 적을 보고, 그 적에게 무엇을 시도하나" 하나다.

| 적의 행동 값 | 시도하는 것 |
|---|---|
| `Available` | 지금과 같다. 공격을 먼저 시도하고, 안 되면 추격 이동을 시도한다. |
| `Moved` (새로) | 공격만 시도한다. 안 되면 그 적은 건너뛴다. 다시 이동하지 않는다. |
| `Attacked` · `Deferred` | 보지 않는다(지금과 같음). |

- 순회 순서는 지금처럼 `EntityId` 순이다. 순회 대상만 `Available`과 `Moved`로 넓힌다.
- 결과: 적 X가 이동하면 다음 `Advance`에서 X가 다시 앞 순서로 공격을 시도한다. 앞 번호의 적은 이미 행동했거나 이번 상태에서 할 수 있는 행동이 없다. 그래서 같은 적의 "이동 → 공격"이 이어서 재생된다. 앞 번호 적이 X의 이동으로 길이 열려 먼저 움직이는 경우는 예외다. 이것은 지금도 일어나는 일이다.
- 끝: 수락되는 행동마다 행동 값이 `Available → Moved`, `Available → Attacked`, `Moved → Attacked` 중 하나로만 바뀐다. 그래서 루프는 반드시 끝난다. 수락 가능한 행동이 하나도 없으면 지금처럼 `EndEnemyPhase`를 부른다.
- 이동 뒤 공격은 이동한 자리에서 보이는 사실로 다시 고른다(`TrySelectAttackAim`). 숨은 사실 때문에 거절되면 그 적은 이번 턴에 공격하지 않는다.

#### 계획에서 정할 것 (사용자 확인 대상)

- **P1. 추격 이동은 지금 규칙 그대로 둔다.** "이동하면 공격할 수 있는 칸"을 골라 걷는 AI는 만들지 않는다. 지금 추격 목표·길찾기(`TrySelectPursuitTarget`·`EnemyMovementPathfinder`)가 대상에 다가가므로, 이동 뒤 사거리 안이면 공격하고 아니면 끝난다. 공격 위치를 노리는 이동은 효과가 크지만 AI 규칙이 바뀌므로 따로 설계한다.
- **P2. 적 공용 예산은 지금 규칙을 따른다.** 적 공용 코스트 설계(`2026-10-27-enemy-shared-actioncost-design.md`)에서 기본 이동은 2, 일반 공격은 3, Cannon 공격은 5다. 최대치는 그 설계에 10으로 적혀 있지만 코드 값은 8이다(`EnemyTurnRules.MaximumActionCost`, `c40fd37b`). 8이면 이동 + 공격을 모두 하는 적은 한 마리 정도다. 앞 번호 적이 예산을 먼저 쓰는 것도 그 설계 그대로다. 공평 분배는 넣지 않는다.
- **P3. 적 공용 코스트 설계 §3 5번 규칙을 이 설계가 대신한다.** 그 규칙은 "각 적은 기존처럼 한 턴에 일반 이동 또는 공격 한 번만 한다"이다. 구현 때 그 문서에 "대신함" 한 줄을 적는다.

#### 영향

- 난이도: 적의 위협 범위가 "이동 + 사거리"로 넓어진다. 레벨 디자인(적 수·배치·예산 10)이 지금 규칙에 맞춰져 있으면 다시 봐야 한다. 기획 확인을 권한다.
- 보스 포효 같은 대기 반응은 지금처럼 일반 행동 루프보다 먼저 처리된다(`EnemyAiTurnService.cs:24-26`). 바뀌지 않는다.

### Q3. 행동 비용(AP) — **결정 A**

| 안 | 내용 |
|---|---|
| **A (권장)** | 이동 비용과 공격 비용을 각각 그 행동 때 낸다. 지금 코드 그대로다. 공격 비용은 `CategoryActionRules.AttackCost`이고, `CanFund`가 행동마다 남은 예산을 본다. |
| B | 이동 + 공격 묶음 할인 같은 새 비용 규칙을 둔다. |

권장 이유: A는 변경이 없다. 할인은 기획 값이 나온 뒤 따로 정한다.

참고: Core에 "직전 성공 공격 이후 지불한 이동비" 기준의 공격 환급 문맥 `Domain/CombatEffects/Contexts/AttackActionCostRefundEffectContext.cs`가 남아 있다. 지금은 이 문맥을 만드는 호출처가 없고 효과 엔진의 서명만 받는다. 이번 변경에서는 건드리지 않는다.

### Q4. 손패 복귀 — **결정 A**

- 확인한 사실: 손패 복귀(`ReturnUnit`)는 배치 국면에서만 받는다(`CombatPlacementCalculator.cs:65`). 행동 값은 보지 않는다.
- 이동 후 공격은 전투 국면의 일이라 복귀와 만나지 않는다.
- **권장: 복귀 규칙은 바꾸지 않는다.** 결정할 것이 없다.

## 3. 결정안(Q1 A · Q2 B · Q3 A · Q4 A)일 때 바꿀 곳

Core만 바꾼다. 아래 1~3은 같은 사실("공격은 `Available` 또는 `Moved`에서 시작한다")을 검사하므로, 판정은 한 곳에 두고 나머지가 그것을 부른다. 4의 AI도 같은 판정을 읽는다.

1. **입장 규칙을 행동별로 나눈다.** `CombatActionAdmission`
   - 새 판정 하나를 둔다. 예: `CanStartAttack(UnitActionUse)` = `Available` 또는 `Moved`.
   - `Actor`를 행동 종류별로 부르게 한다. 이동은 `Available`만 받는다. 공격(실행·범위 조회·가능 조회)은 위 판정을 쓴다.
   - `Deferred`는 지금처럼 `ActionDeferred`로 거절한다.
2. **공격 결과 검사.** `AttackResult.cs:251` — 행동 전 값 검사를 위 판정으로 바꾼다. 행동 후 값은 지금처럼 `Attacked`다.
3. **Buffer 게시 검사.** `CombatStateBuffer.cs:271-273` — 공격 정산이면 행동 전 값으로 `Moved`도 받는다. 이동 정산은 지금처럼 `Available`만 받는다.

4. **적 AI.** `EnemyAiTurnService.cs:28-45`
   - 순회 조건 `ActionUse == Available`을 "이동이나 공격을 시작할 수 있음"으로 넓힌다. 공격 쪽은 1의 판정을 그대로 쓴다.
   - `Moved`인 적은 공격만 시도하고, 추격 이동 갈래로 가지 않는다.
   - 주석 "한 일반 행동만 실행한다"를 "Advance 한 번에 수락된 행동 하나"로 고친다. 규칙이 바뀐 것은 적 한 마리의 행동 수이고, Advance 한 번에 행동 하나는 그대로다.

`CombatStateBuffer`는 Buffer 쪽 파일이지만 Core 계약의 검사를 옮겨 둔 곳이라 같은 커밋에서 바꾼다.

바꾸지 않는 것:
- `UnitActionUse` 열거 값, 이동 정산, 턴 시작 초기화, 적 추격 목표·길찾기·조준 점수(P1), 적 예산 규칙(P2), 손패 복귀.
- `MovementPreviewContext`의 같은 입력 비교는 행동 값을 비교한다. 이동 뒤 값이 바뀌면 다른 입력이 되는 것이 맞으므로 그대로 둔다.

## 4. View 영향 (예상, Unity 미확인)

- 공격 버튼과 공격 거절 안내는 Core 가능 조회의 `AttackFailure`를 따른다. 그래서 이동 뒤에도 공격 모드에 들어갈 수 있게 될 것이다.
- HUD 행동 문구(`CombatActionStateText`)는 이동 뒤 "이동함"을 보인다. "이동함, 공격 가능"처럼 바꿀지는 View 쪽 결정이다.
- 이동 뒤 공격 모드로 자동으로 넘어가는 것 같은 편의 기능은 이번 범위가 아니다. 필요하면 View 작업자에게 따로 요청한다.
- 적 턴 재생: 한 적의 이동과 공격이 다른 결과로 연달아 온다. 지금도 결과마다 차례로 재생하므로 구조 변경은 없을 것으로 본다.
- **적 호버 공격 범위가 실제 위협보다 좁게 보인다(Q2 B의 결과).** 지금 적 호버는 Core `GetUnitAttackRange`로 "지금 자리에서의 사거리"만 보인다. 이제 적은 이동한 뒤 공격하므로 플레이어가 위험 칸을 잘못 읽을 수 있다. "이동 + 사거리" 위협 표시를 넣을지는 기획·View 쪽에서 정할 일이다. 넣으려면 Core 조회도 새로 필요하다. 이번 범위에는 넣지 않는다.

## 5. 시험 영향

`ActionAlreadyUsed`를 기대하는 시험은 19곳(12개 파일)이다. 이 중 "이동한 유닛의 공격이 거절된다"를 보는 사례만 기대값이 바뀐다. 아래 파일은 후보이며, 사례별 확인은 구현 Phase에서 한다.

- `PlayerTurnAttackTests.cs:59-67` — 이동·공격 가능 조회를 함께 단언한다. 가장 먼저 볼 곳이다.
- `TargetingAngleLifecycleTests.cs:54`, `TargetingPreviewParityTests.cs:208` — 범위 조회 거절이다. 이동 뒤 조회라면 바뀐다.
- `EnhancementActionCostTests.cs:147`, `CombatCommandEffectTests.cs:211·519·667`, `CombatCommonCommandTests.cs:366`, `CategoryEffectRetirementTests.cs:68`, `CheckpointReturnTests.cs:61`, `MineEnvironmentTests.cs:269`, `IntimidationScheduleTests.cs:48`, `PlayerTurnMovementTests.cs:107·253` — 앞 행동이 이동인지 공격인지에 따라 다르다.
- `Framework/CoreStageCombatCycleTests.cs:218` — Framework 시험이라 대상이 아니다.

Q2 B 때문에 적 턴을 끝까지 돌리는 시험도 결과가 바뀔 수 있다. 이동한 적이 이어서 공격하면 피해·사망·예산 잔액·Advance 횟수가 달라진다. 후보는 적 턴을 Advance로 돌리는 Core 시험이다. 예: `EnemyMovementPathTests`, `ControllerTurnCountTests`, `EightTurnNightTests`, `CombatCommonCommandTests`, `BossTurnTests`, `CoreIntegration/CombatAdvanceServiceTests`, `ControllerTurnCountPublicationTests`. `Framework/` 아래 시험은 대상이 아니다. 기대값이 바뀌는지는 구현 Phase에서 사례마다 확인한다.

새로 필요한 시험:
- 플레이어: 이동 → 공격 수락, 공격 → 이동 거절, 이동 → 이동 거절, 이동 → 공격 Buffer 게시 성공.
- 적: 이동한 적이 사거리 안이면 다음 Advance에서 공격, 사거리 밖이면 건너뛰고 다음 적으로, 이동한 적은 다시 이동하지 않음, 예산이 공격 비용보다 적으면 이동 뒤 공격 없음, 모든 적이 끝나면 `EndEnemyPhase`.

## 6. 다음 단계

결정은 끝났다. P1~P3도 그대로다(2026-10-27 사용자). Task 계획은 [move-then-attack-plan](../plans/2026-10-27-move-then-attack-plan.md)이다. Phase는 둘로 본다.
- **MA-a 플레이어:** 판정 함수 + 검사 세 곳 → 플레이어 시험 정리.
- **MA-b 적 AI:** 순회 조건·`Moved` 갈래 → 적 턴 시험 정리 → 적 공용 코스트 설계에 "대신함" 표시(P3) → View 작업자 공유 메모(적 호버 범위, §4).

적 난이도가 바뀌므로 MA-b 전에 기획 확인을 받는 것을 권한다.

## 7. 구현 메모 (2026-10-27)

- **규칙 원본:** `Contracts/Combat/UnitActionUseRules`(`CanStartMove`·`CanStartAttack`). 입장 검사(`CombatActionAdmission.Actor(input, action, out actor)`), `AttackResult` 계약, `CombatStateBuffer` 게시 검사, 적 AI 순회가 모두 이것을 읽는다. §3 1~4 그대로다.
- **적 AI(MA-b):** `EnemyAiTurnService` 순회 조건을 `CanStartAttack`으로 넓히고, 추격 이동 앞에 `CanStartMove`가 아니면 `continue`를 둔다. Advance 한 번에 행동 하나는 그대로다. 그래서 이동한 적의 공격은 다음 Advance에서 나온다.
- **설계와 다른 점:** 없다. 계획 단계에서 정한 TP-1~TP-8 그대로다.
- **같이 한 일(설계 밖, 사용자 지시):**
  - 적 공용 예산(`c40fd37b`) 도입으로 깨져 있던 Core 시험 81개를 정리했다. 적 턴 입력 도우미는 적 예산 Full, 무예산 단언은 새 규칙으로 바꿨다.
  - 기획 값을 숫자로 고정하던 운영 에셋 시험 다섯 개를 "구조만 확인"으로 바꿨다.
  - 기록은 계획 §6.2·§6.3이다.
- **수치(P2):** 적 예산 최대치는 코드 값 8이다. 이동 2 + 공격 3 = 5라서, 한 적 턴에 이동과 공격을 모두 하는 적은 많아야 한 마리다.
- **남은 것:**
  - View 공유(적 호버 범위가 실제 위협보다 좁음, `UnitHUD` 행동 가능 표시, "이동함" 문구)는 채팅 글로 넘긴다.
  - 공격 위치를 노리는 추격 AI(P1)는 따로 설계한다.
