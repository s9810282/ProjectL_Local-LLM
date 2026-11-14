# 진영 턴 반영: 판단 단위와 상황 형식 v2

작성일: 2026-11-06 · 상태: 정의 확정(Q1~Q4 사용자 결정 11-06, Q5는 Core 작업자 확인 대기). 평가기·상황 파일은 아직 바꾸지 않음

8주차 보고서 차주 계획 1번이다. 바뀐 게임 규칙에 맞춰 다음 다섯 가지를 정한다.

- 합법성: 적 유닛이 할 수 있는 행동
- 반격 모델: 다음 아군 턴에 받을 위험
- 판단 단위
- 상황 형식 v2
- 기존 상황 32개의 처리

규칙은 [진영 턴 방향 문서](2026-10-29-faction-turn-direction.md) §1.4의 10-29 확인 내용을 따른다. 사실은 갱신된 Core(`ecfd52b7b`, 이 저장소 커밋 `4ea2d5c0`)에서 확인했다.

룰·LLM의 판단 기준(가중치, 할인, 가치)과 기대 판단은 차주 계획 2번에서 정한다. 이 문서는 그 기준이 읽을 **입력의 모양**만 정한다.

## 1 갱신된 Core에서 확인한 사실

진영 턴 방향 문서 §1.1의 "진영 턴 구조는 아직 Core에 없다"는 이제 틀리다. develop에 이미 들어와 있다.

| 항목 | 지금 Core | 근거 |
|---|---|---|
| 턴 구조 | 아군 진영 턴과 적 진영 턴이 번갈아 진행된다. 턴 순서는 `CombatControllerPhase`가 정한다 | `CombatTurnCalculator.EnterCombat`·`EndPlayerTurn`·`EndEnemyPhase` |
| AP | 없다. 저장소 Core에서 AP 관련 코드가 검색되지 않는다 | — |
| 행동 횟수 | 유닛마다 `Available → Moved → Attacked` 상태를 가진다. 이동은 `Available`일 때만, 공격은 `Available`·`Moved`일 때 된다. 그래서 이동 1회 뒤 공격 1회만 가능하고, 공격한 뒤에는 이동할 수 없다 | `Contracts/Combat/UnitActionUse.cs`, `UnitActionUseRules.cs` |
| 이동 거리 | 유닛 병과 값에서 정해진다. 한 번 이동할 때 경로 비용이 이 값 이하여야 한다. 일부 지형 특성은 보너스를 준다 | `UnitDefinition.MoveRange`, `CombatActionAdmission.MovementAllowance`, `CategoryConfig_Default.asset` |
| 적 유닛별 내부 예산 | **Core에 없다.** 적도 이동 거리와 1회씩 규칙만 적용받는다 | §7 Q1 |
| 적 행동 순서 | 엔티티 ID 순이다. 부를 때마다 행동 하나만 돌려준다. 각 유닛은 지금 자리에서 공격할 수 있으면 이동 없이 공격하고, 공격할 수 없으면 추격 이동한 뒤 다음 호출에서 공격을 시도한다. 보스 포효가 먼저 처리된다 | `EnemyAi/EnemyAiTurnService.Calculate` |
| KillChain | 아군 특성 `KillChainAttack`이 있다. 공격 명령으로 적을 처치하면 처치 1명당 맵 전체의 **무작위 적 하나**를 추가로 공격한다. 추가 공격으로 처치하면 1회가 더 생긴다 | `KillChainAttackCalculator`, `KillChainAttackTraitSpec` |
| ThornArmor(밤 반사) | 저장소 Core에서 검색되지 않는다 | R18·R23·R24가 기대던 규칙 |

### 1.1 적·아군 유닛(DB 값)

이동 거리는 병과 설정 값이다. 병과 키와 병과 이름은 짝지어 추정한 것이라 확인이 필요하다. 10 = 메인류 3, 20 = 기병류 4, 30 = 중장류 3, 40 = 포병류 2, 50 = 보스 0.

| 진영 | 유닛 | 최대 HP | 공격력 | 사거리 | 이동 | 방어 |
|---|---|---|---|---|---|---|
| 적 | Demon | 15 | 6 | 1 | 3 | 5 |
| 적 | Gravekeeper | 23 | 7 | 1 | 4 | 5 |
| 적 | UndeadHeavy | 30 | 4 | 1 | 3 | 5 |
| 적 | UndeadCannonCart | 20 | 6 | 2 | 2 | 5 |
| 적 | Stage1Boss | 100 | 5 | 4 | 0 | 5 |
| 아군 | MainCharacter | 30 | 7 | 1 | 3 | 5 |
| 아군 | Sniper | 10 | 5 | 4 | 3 | 5 |
| 아군 | ShotGun | 35 | 7 | 2 | 3 | 5 |
| 아군 | Akimbo | 25 | 7 | 2 | 3 | 5 |
| 아군 | Cowboy | 25 | 10 | 1 | 4 | 5 |
| 아군 | Ballista | 15 | 8 | 4 | 2 | 5 |
| 아군 | CactusCannon | 20 | 15 | 3 | 2 | 5 |
| 아군 | Shielder | 40 | 5 | 1 | 3 | 5 |
| 아군 | DynamiteHammer | 20 | 6 | 1 | 4 | 5 |

이 표가 기존 상황에 주는 영향은 두 가지다.

- **Ballista는 이제 아군 전용이다.** 적 Ballista 에셋은 삭제됐다(CLAUDE.md 10-20 기록). 상황 13개가 적 Ballista를 행위자로 쓴다.
- **기존 상황의 수치는 10-22 DB 값이라 지금 값과 다르다.** 예를 들어 R19 Sniper는 공격력 1, Focus 스택당 +1이었는데 지금은 공격력 5다. 실제 피해는 방어력을 거친 Core 계산값이어야 하므로, 상황을 작성할 때 Core 미리보기나 계산식으로 확인해 넣는다.

## 2 합법성 모델

적 유닛 한 명이 진영 턴 안에서 고를 수 있는 후보는 네 종류다.

| 종류 | 조건 |
|---|---|
| `wait` | 항상 가능 |
| `move` | 이동 1회, 경로 비용 ≤ 이동 거리 |
| `attack` | 지금 자리에서 공격 1회 |
| `move_attack` | 이동 1회 뒤 도착한 자리에서 공격 1회 |

- 공격한 뒤 이동하는 후보는 만들지 않는다.
- 내부 예산은 Core에 없으므로 형식에는 `self.budget`을 선택 칸으로만 둔다. 칸이 없으면 위 조건만 본다(§7 Q1). 예산이 정해지면 각 후보에 `budgetCost`를 적고, 평가기가 `budgetCost ≤ budget`을 검사한다.
- 기존 평가기의 `actions[].cost`·`refund`·`ap` 검사는 v2에서 쓰지 않는다. 환급과 선지불이라는 개념 자체가 사라졌다.
- 경로와 좌표는 지금처럼 작성자가 결과를 적는다. `legal`·`moveSteps`·도착 칸 설명은 작성자가 Core 규칙으로 확인한 가정이다.

## 3 반격 모델(exposure)

v1의 `responseWindow`(다음 내 턴 전 타임라인, AP 예산 반복 공격)를 `exposure`로 바꾼다.

**기간:** 이번 적 진영 턴이 끝난 뒤 **다음 아군 진영 턴 한 번**이다.

**같은 적 진영 턴에서 뒤에 행동할 적 유닛**이 위험을 줄이는 경우는 v2 첫 단계에서 넣지 않는다. 뒤 유닛이 위협하는 플레이어를 처치하는 경우가 그렇다. 위험을 크게 보는 쪽으로 단순하게 시작한다.

**위협자:** 다음 아군 턴까지 살아 있는 플레이어마다, 행위자의 행동 뒤 자리를 공격할 수 있는지 적는다. 플레이어는 이동 1회 뒤 공격 1회만 하므로 공격은 최대 1회다.

| 칸 | 뜻 |
|---|---|
| `reach: "in_place"` | 이동하지 않고 지금 자리에서 닿는다 |
| `reach: "after_move"` | 이동 1회 뒤에 닿는다. 이동 경로가 막히는지는 작성 시점의 사실로만 판단한다 |
| `damage` | 그 플레이어의 공격 1회가 행위자에게 주는 피해(Core 계산값)다. 이동 거리에 따라 공격력이 바뀌는 특성(Cowboy 등)은 닿기 위해 필요한 이동을 기준으로 적는다 |
| `chains` (선택, `exposure` 아래 목록) | KillChain 같은 연쇄로 **추가로** 맞을 수 있는 피해와 확률 설명(opponentId·damage·note)이다. 무작위라 기대값으로 채우지 않고 따로 표시만 한다. 직접 닿지 않는 플레이어에게서도 생기므로 위협 항목과 따로 둔다(11-06 변경) |
| `nonThreats` | 닿지 않는 플레이어와 그 이유다. 살아 있는 플레이어는 모두 `threats`나 `nonThreats` 중 정확히 한 곳에 있어야 한다(v1 coverage 검사 유지) |

**합치는 법:** v2 형식에서는 위협자 전원이 행위자 한 명에게 몰리는 최악의 경우로 피해를 더한다. 행위자 HP를 넘는 피해는 버린다(v1과 같음).

`in_place`와 `after_move`, `chain`을 얼마나 할인할지는 2번 작업에서 정한다. 데이터는 셋을 나눠 담아서 할인 방식을 바꿔도 상황 파일을 다시 쓰지 않아도 되게 한다.

이번 후보로 처치된 플레이어는 위협에서 자동으로 빠진다(v1과 같음). 전투가 끝나거나 행위자가 죽는 후보는 위험이 0이다(v1과 같음).

**기믹:** 예고되기 전의 기믹은 상황에 넣지 않는다. 예고된 기믹은 `announcedHazards`에 공개 정보로 넣고, 행위자의 행동 뒤 자리가 해당되면 `exposure.hazardDamage`로 적는다.

**불완전한 정보:** `exposure.complete`가 `true`가 아니면 v1처럼 평가하지 않고 오류로 거른다(R15 규칙 유지).

## 4 판단 단위

진영 턴 방향 문서 §2.1의 추천대로 **A 유닛별 순차 판단**으로 시작한다.

- 판단 하나 = 적 유닛 한 명의 후보 하나 고르기다. 앞서 행동한 적 유닛의 결과는 상황의 현재 상태에 이미 반영돼 있다.
- **순서 규칙:** 첫 단계는 Core와 같은 **엔티티 ID 순**이다(§7 Q2). 실제 게임과 같은 순서라서, 상황을 Core 흐름에서 뽑아낼 때 어긋나지 않는다. 처치 가능한 유닛 먼저, 노출이 큰 유닛 먼저 같은 순서 판단은 다음 비교(C안)로 둔다.
- 상황에는 순서 정보를 담는다. `turn.enemyOrder`는 순서 전체, `turn.actingId`는 지금 판단하는 유닛, `turn.actedAllies`는 앞서 행동한 적 유닛과 결과 요약이다. 앞선 결과는 상태에 이미 반영돼 있으므로 이 정보는 평가기 계산에 쓰지 않는다. LLM 화면과 사람이 읽기 위한 것이다.

**Core AI와 다른 점:** 지금 Core AI는 제자리에서 공격할 수 있으면 이동을 고려하지 않는다. v2 후보에는 `move_attack`과 `move`도 있으므로, 룰·LLM은 "제자리 공격 대신 더 나은 자리로 이동한 뒤 공격"을 고를 수 있다. 이 차이가 비교 대상이다.

## 5 상황 형식 v2

v1과 같은 원칙을 지킨다. 사실만 적고, 작성자 가정은 `assumptions`에 둔다. 기대 판단은 계산 전에 적는다.

```json
{
  "schemaVersion": 2,
  "situationId": "F01",
  "title": "…",
  "source": "synthetic",
  "assumptions": ["…"],
  "turn": {
    "dayNight": "night",
    "enemyOrder": ["e_demon", "e_grave"],
    "actingId": "e_grave",
    "actedAllies": [{ "id": "e_demon", "summary": "메인에게 6 피해, 제자리" }]
  },
  "self": {
    "id": "e_grave", "unit": "Gravekeeper", "hp": 23, "maxHp": 23,
    "moveRange": 4, "attackRange": 1
  },
  "players": [
    { "id": "main", "role": "main", "unit": "MainCharacter", "hp": 24, "maxHp": 30,
      "moveRange": 3, "attackRange": 1, "traits": [] }
  ],
  "allies": [{ "id": "e_demon", "unit": "Demon", "hp": 15, "maxHp": 15 }],
  "headquarters": { "hp": 3, "maxHp": 3 },
  "announcedHazards": [],
  "approachCostBefore": 2,
  "candidates": [
    {
      "id": "a_move_attack",
      "label": "…",
      "legal": true,
      "kind": "move_attack",
      "moveSteps": 2,
      "selfHpAfter": 23,
      "headquartersHpAfter": 3,
      "playerHpAfter": { "main": 22 },
      "allyHpAfter": { "e_demon": 15 },
      "approachCostAfter": 0,
      "hitIds": ["main"],
      "roleFacts": { "canThreatenNext": true, "nearestPlayerDistance": 1, "knockbackDelays": [] },
      "exposure": {
        "complete": true,
        "assumptions": "다음 아군 진영 턴 한 번. 위협자 전원이 행위자에게 몰리는 최악의 경우.",
        "threats": [
          { "opponentId": "main", "reach": "in_place", "damage": 2 }
        ],
        "nonThreats": [],
        "hazardDamage": 0
      }
    }
  ],
  "expected": { "victory": ["…"], "role": ["…"], "reason": "…" }
}
```

v1에서 바뀌는 칸:

| v1 | v2 | 이유 |
|---|---|---|
| `self.ap`·`maxAp`·`moveRange` | `self.moveRange`·`attackRange`, 선택 `budget` | AP가 없어졌다 |
| `candidates[].actions[]`(kind·cost·refund·steps) | `kind` 하나와 `moveSteps` | 이동 1회·공격 1회라 행동 목록이 필요 없다 |
| `responseWindow`(availableAp·mode·repeatSafe·attackCost·damagePerAttack) | `exposure`(threats: reach·damage·chain, nonThreats, hazardDamage) | 반복 공격이 사라지고 다음 아군 턴 한 번만 본다 |
| `players[]`(id·role·hp) | 여기에 `unit`·`moveRange`·`attackRange`·`traits` 추가 | 위협 판단의 근거를 사람과 LLM이 볼 수 있게 한다 |
| — | `turn`, `announcedHazards` | 진영 순서와 예고된 기믹 |

그대로 두는 칸: `playerHpAfter`, `allyHpAfter`, `headquartersHpAfter`, `selfHpAfter`, `hitIds`, `approachCost*`, `roleFacts`, `expected`. 가치 항목 M·D·K·P·F·B와 역할 Q는 이 칸들로 계산하므로 2번 작업 전까지 바꾸지 않는다.

v2 파일은 새 폴더(`rule_fixtures_v2/`, `validation_fixtures_v2/`)에 두고 상황 ID 접두사를 `F`로 바꾼다. v1 파일과 v1 결과는 개별 턴 기준의 기록으로 남긴다. 평가기는 `schemaVersion`으로 v1과 v2를 나눠 읽게 고친다. 이 작업은 2번 작업에서 판단 기준과 함께 한다.

## 6 기존 상황 32개 분류

기준은 세 가지다. 행위자가 지금 적 유닛인지, v1에서 AP 반복 공격이나 환급에 기대는지, 사라진 규칙(ThornArmor)에 기대는지.

| 분류 | 상황 | 처리 |
|---|---|---|
| A 형식만 옮김 | R01, R02, R03, R06, R08, R10, R15, R16 | 질문은 그대로다(즉시 처치·종료 우선, 넉백 역할, 빈사 행위자, 불완전 정보). v2 형식으로 옮기고 수치를 지금 DB 값으로 다시 계산한다 |
| B 반격 다시 계산 | R17, R21, V03, V05, V06, V07, V08 | 질문은 남지만 v1 반격이 반복 공격(최대 2~5회)이나 환급을 가정했다. 위협자별 1회 피해로 `exposure`를 다시 쓴다. 기대 판단은 결과가 바뀔 수 있어 계산 전에 다시 적는다 |
| C 전제가 사라짐 | R05(상대 AP 예산 차이), R11(Ballista 이동 3 + 공격 4 > AP 5), R12(환급 선지불), R18·R23·R24(ThornArmor 밤 반사) | 시험하던 규칙이 없다. 보존만 하거나, 지금 규칙의 다른 질문으로 다시 쓴다(Q3). R11은 내부 예산이 생기면 그 예산 시험으로 다시 쓸 수 있다 |
| D1 유닛만 바꿈 | R09·V04(아군 피해 동반), R13(처치한 반격자 제외), R14(위협 없음), V01·V02(즉시 승리·패배 우선) | 행위자가 적 Ballista지만 질문은 관통과 무관하다. 지금 적 유닛(UndeadCannonCart 등)으로 다시 쓴다. R09·V04는 CannonCart 공격이 다른 적을 다치게 할 수 있는지부터 확인해야 한다 |
| D2 보존만 | R04, R07, R19, R20, R22 | 적 Ballista의 관통·사선·Focus 노출이 핵심이라 지금 적 구성에 대응하는 유닛이 없다. 개별 턴 기록으로만 남긴다 |

개수는 A 8, B 7, C 6, D1 6, D2 5로 합계 32다.

- **V 세트:** V01~V08은 이미 결과를 봤으므로 v2에서도 보류 세트로 쓰지 않는다. v2 보류 세트는 2번 작업 뒤 새로 만든다.
- **새로 필요한 상황:** v1에 없던 진영 턴 고유의 질문이다. 2번 작업에서 기대 판단과 함께 쓴다.
  - 앞선 적 유닛의 결과가 이번 후보를 바꾸는 경우
  - `after_move`로만 닿는 위협
  - KillChain 연쇄 위험
  - 예고된 기믹 칸
  - 제자리 공격 대 이동 뒤 공격(Core AI와 다른 선택)

## 7 확인할 것

| 번호 | 질문 | 권장 |
|---|---|---|
| Q1 | 적 유닛별 내부 예산이 Core에 아직 없다. 지금은 이동 거리와 1회씩 규칙만으로 합법성을 볼까? | 예. `budget` 칸만 선택으로 두고, 예산이 생기면 채운다 |
| Q2 | 적 행동 순서를 첫 단계에서 Core와 같은 엔티티 ID 순으로 고정할까? | 예. 순서 판단은 C안 비교로 미룬다 |
| Q3 | C 분류(사라진 규칙) 6개는 보존만 할까, 지금 규칙으로 다시 쓸까? | 보존만 한다. 대신 §6의 새 질문 상황을 쓴다 |
| Q4 | 위협 합산을 "위협자 전원이 행위자에게 몰리는 최악의 경우"로 시작할까? | 예. 할인은 2번 작업에서 정한다 |
| Q5 | 병과 키와 이동 거리의 대응(10 = 3, 20 = 4, 30 = 3, 40 = 2, 50 = 0)이 맞나? | Core 작업자에게 확인 |

**결정(2026-11-06 사용자 "권장안대로"):** Q1~Q4는 권장안으로 정했다.
- Q1: 합법성은 이동 거리와 1회씩 규칙만으로 보고, `budget`은 선택 칸으로 둔다.
- Q2: 적 행동 순서는 엔티티 ID 순으로 고정한다.
- Q3: C 분류 6개는 보존만 하고, 진영 턴 고유의 새 질문 상황을 쓴다.
- Q4: 위협은 최악의 집중 공격으로 합친다.

Q5(병과 키와 이동 거리의 대응)는 Core 작업자 확인이 남았다. 확인 전에는 §1.1 표 값을 쓴다.
