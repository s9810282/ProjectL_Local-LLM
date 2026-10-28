# 오프라인 입력과 실제 전투 값의 대응

작성일: 2026-10-22

7주차 차주 계획 2번(M2·M3)의 산출물이다. `rule_fixtures`의 각 필드를 실제 Core가 어디서 줄 수 있는지 대조하고, R01~R16의 수작업 가정이 현재 DB·Core 값과 어디서 다른지 정리한다. Core·View·DB 파일은 바꾸지 않았다. 아래 Core 요청은 초안이며, Core 전달 문서에 옮기는 것은 사용자 확인 뒤에 한다.

대조 기준은 저장소 `87aa411`의 Core 코드와 `Assets/ProjectL/DataBaseSO` 에셋이다. 근거 파일은 각 항목에 적었다. Unity를 실행해 값을 재생하지 않았으므로, 코드·에셋을 읽어서 확정할 수 없던 부분은 "확인 필요"로 표시했다.

## 1 지금 적 턴이 계산되는 경로

| 단계 | 위치 | 공개 범위 |
|---|---|---|
| Job이 자동 진행을 요청 | `ICombatProcessingService.AdvanceCombat()` | public (서비스) |
| 활성 행위자가 적이면 적 턴 계산 | `CombatCommandCalculator.Advance` → `EnemyAiTurnService.Calculate` | Advance는 public, 적 턴 서비스는 internal |
| 공격 선택 | `EnemyAiPolicy.TrySelectAttackAim`: 공격 범위의 모든 조준을 `Preview`로 투영해 `(아군 피격 수, -피해, -피격 수, 좌표, 방향)` 사전순 최소 | internal |
| 공격 불가 시 추적 | `TrySelectPursuitTarget`(맨해튼 최근접) + `EnemyMovementPathfinder.TryFind` | internal |
| 결과 | 이동 0~1회 + 공격 0~1회 + 턴 종료를 `Merge`한 전이 하나 | internal |

- 정책은 `EnemyAiTurnService` 안에서 `new EnemyAiPolicy(...)`로 고정 생성된다. 바깥에서 다른 정책을 끼울 자리가 없다.
- 후보를 만들고 결과를 투영하는 도구는 대부분 이미 있다: `GetAttackRange`(공개), `CombatCommandCalculator.Preview`(공개), `CombatMovementCalculator.Range/Preview`(공개), `CombatTurnCalculator.Forecast`(공개).
- 하지만 둘을 이어 붙이는 도구는 internal이다: `CombatCommandComposition.Intent/Project/Merge`, 투영 이벤트를 돌려주는 `Preview(..., out projection)`. 그래서 "이동한 뒤의 상태에서 공격하면?" 같은 조합 후보는 Core 밖에서 만들 수 없다.
- `CombatCommandInput`은 서비스의 private `ReadInput`이 Buffer에서 조립한다. Core 밖(View·실험 도구)이 같은 입력을 직접 만들 수 없다.

근거: [CombatProcessingService.cs](../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs), [CombatCommandCalculator.cs](../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandCalculator.cs), [EnemyAiTurnService.cs](../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/EnemyAiTurnService.cs), [EnemyAiPolicy.cs](../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/EnemyAiPolicy.cs).

## 2 필드 대응표

분류는 네 가지다.

- **A 그대로**: 현재 공개 Core 값에서 바로 읽는다.
- **B 조합**: 공개 값 여러 개를 합쳐 계산한다. 규칙을 새로 구현하지는 않는다.
- **C Core 요청**: 지금 공개 경계로는 얻을 수 없다. Core 쪽 조회 추가가 필요하다.
- **D 오프라인 전용**: 실험 기록용이며 실제 연결에서는 쓰지 않거나 다른 방식으로 대체한다.

### 2.1 상황 공통

| fixture 필드 | Core 원본 | 분류 | 메모 |
|---|---|---|---|
| `self.unit` | `CharacterCombatFacts.CharacterKey` | A | |
| `self.hp`, `self.maxHp` | `CharacterCombatFacts.Health.CurrentHealth/MaxHealth` | A | 영혼·Artifact 최대 HP 보너스는 `Stats.SoulMaxHealthBonus`, `TemporaryMaxHealthBonus`에 따로 있다. 설계는 행동 중 최대 HP 변화를 범위 밖으로 둔다. |
| `self.ap`, `self.maxAp` | `Stats.CurrentActionCost`, `Stats.MaxActionCost` | A | |
| `self.moveRange` | `Stats.RemainingMoveRange` | A | 이동 비용은 `Stats.MoveCost`(칸당). Ballista는 이동 범위 5지만 칸당 3이라 AP 5로 1칸만 간다. |
| `players[]`, `allies[]` | `CombatCommandInput.Characters` 중 `Area == Field`, 생존, `Owner`로 구분 | A | 메인 구분은 `OwnershipRole`. 손패·예비 유닛은 제외. |
| `headquarters` | `State.Structures` 중 `EntityKey.Structure_Headquarters` | A | DB 최대 HP 3. fixture와 같다. |
| `approachCostBefore` | 없음 | C | 메인을 공격할 수 있는 위치까지의 최소 이동 AP를 주는 공개 조회가 없다. 추적 경로 탐색(`EnemyMovementPathfinder`)은 internal이고 "최근접 대상까지"만 찾는다. |
| `expected`, `assumptions` | 없음 | D | 기획 기대·가정 기록. |

### 2.2 후보

| fixture 필드 | Core 원본 | 분류 | 메모 |
|---|---|---|---|
| 후보 목록 자체 | 이동: `MovementRangeResult.Destinations` / 공격: `AttackRangeResult.AttackAims` | B·C | 단일 이동·단일 공격 후보는 공개 조회로 만들 수 있다(B). **이동 후 공격**은 이동을 투영한 상태에서 다시 공격 범위를 구해야 하므로 C. |
| `actions[].cost`, `refund` | 공격: `AttackPreview.ActionCostSettlement.Spent/Refunded/Final` / 이동: `MovePreview.QuotedActionCost` | A | Rider(Gravekeeper)의 이동비 환급이 여기 포함된다. |
| `legal` | `AttackPreview.CanAttack`·`FailureReason`, `MovePreview.CanMove`·`FailureReason` | A·C | 단일 행동은 A. 조합 후보는 C. |
| `playerHpAfter` (직접 대상) | `AttackPreview.Targets[].TargetHealthAfter`, `TargetKilled` | A | 관통은 `RayHits`, 상성은 `MatchupNumerator/Denominator`로 분해돼 있다. |
| `selfHpAfter`, `allyHpAfter`, `headquartersHpAfter`, 2차 피해 | 투영 이벤트 `CombatCharacterEvent/CombatStructureEvent`의 `Expected → Final` | C | `EnemyAiPolicy.ProjectedDamage`가 이미 이 방식으로 합산한다. 하지만 이벤트를 주는 `Preview(..., out projection)`이 internal이다. |
| `hitIds` | `Targets[]` 중 `ExpectedDamage > 0`인 플레이어 | A | |
| 종료 결과(승·패·계속) | `CombatCommandTransition.Outcome`(`StageOutcomeSnapshot`) | C | 후보마다 Core 판정이 필요하다. 이름만 보고 승리를 추정하지 않는다는 설계 §4와 맞다. |
| `approachCostAfter` | 없음 | C | §2.1과 같다. |
| `roleFacts.canThreatenNext` | 행동 후 위치에서 다음 턴 자원으로 `GetAttackRange` | C | 투영 상태의 입력이 필요하다. |
| `roleFacts.nearestPlayerDistance` | 투영 후 `Anchor`/`OccupiedCells`의 맨해튼 거리 | B | 투영 위치만 있으면 거리 계산은 단순 산술이다. 위치 자체는 C(투영 필요). |
| `roleFacts.knockbackDelays` | 없음 | C | 피해 처리 후 넉백 직전·직후 위치와, 그 위치에서 적 본진 공격 위치까지의 비용이 필요하다. 설계 §7.1 `U`에서 이미 협의 항목으로 적었다. |

### 2.3 반격 일정(`responseWindow`)

| fixture 필드 | Core 원본 | 분류 | 메모 |
|---|---|---|---|
| 기회의 순서 | `ICombatProcessingService.GetTimeline()` → `CombatTurnCalculator.Forecast` | B | 행위자의 다음 턴 항목 전까지의 플레이어 항목이 기회 목록이다. 기회는 개체 ID·예정 시각·남은 턴 수로 나온다. |
| `availableAp`, `maxAp` | 상대의 `Stats.MaxActionCost` | B | 턴 시작 시 AP가 최대치로 채워진다고 가정했다. **확인 필요**(턴 시작 처리 코드 미대조). |
| `attackCost` | 상대의 `Stats.AttackCost` | A | |
| `damagePerAttack` | 상대 기준 `Preview` (공격자=플레이어, 대상=행위자의 행동 후 위치) | C | 투영 상태에서 반대 진영 미리보기가 필요하다. Focus 스택·상성·관통 감쇠를 Python이 재현하지 않으려면 Core 값이어야 한다. |
| `mode: fixed/sequence` | 특성 상태로 판단 | B | Focus는 한 턴 안에서는 스택이 그대로라 `fixed`가 맞다(이동하면 0으로 초기화). KillChain(Akimbo)은 처치하면 추가 공격이 이어져 `fixed`로 쓸 수 없다. |
| `nonThreats[].reason` | 상대 위치에서의 공격 범위 | C | "제자리 사거리 밖"은 투영된 행위자 위치에 대한 상대의 공격 범위가 필요하다. 상대가 **이동한 뒤** 공격하는 경우는 설계상 아직 범위 밖이다(D). |

## 3 수작업 가정과 실제 값의 차이

R01~R16 상황의 값을 DB 에셋과 Core 규칙에 대조했다. 대부분의 fixture는 처음부터 "기획 판단용 synthetic"으로 만든 것이라 차이가 있는 게 정상이다. 다만 **실제 값에서는 갈등 자체가 생기지 않는 상황**이 있어 1번 작업(변형 상황 보완)의 출발점이 된다.

### 3.1 현재 유닛 값

| 유닛 | 진영 | HP | AP | 공격비 | 이동(범위/칸당) | 공격력 | 카테고리 | 특성 |
|---|---|---:|---:|---:|---|---:|---|---|
| Demon | 적 | 5 | 3 | 1 | 3/1 | 1 | Base | 없음 |
| Gravekeeper | 적 | 10 | 5 | 2 | 5/1 | 10 | Rider (이동비 환급) | 넉백 1칸 |
| Ballista | 적 | 10 | 5 | 4 | 5/3 | 10 | Cannon | 관통 감쇠 대상당 30%, 3갈래 |
| MainCharacter | 플레이어 | 50 | 5 | 1 | 3/1 | 1 | Base | 없음 |
| Sniper | 플레이어 | 10 | 3 | 1 | 3/1 | 1 | Base | Focus (스택당 +1, 최대 3) |
| Akimbo | 플레이어 | 10 | 5 | 2 | 3/1 | 1 | Base | KillChain |
| ShotGun | 플레이어 | 10 | 3 | 1 | 3/1 | 1 | Base | 거리별 피해 비율 |

카테고리 상성(`CategoryMatchupSpec.CreateDefault`, DB 값 없음)은 위 유닛 조합에서 모두 1.0이다. Rider→Base, Cannon→Base, Base→Rider, Base→Cannon 모두 10/10이다. 적 본진 최대 HP는 3이다.

Artifact는 3개다. `HealingChalice`(스테이지 클리어 회복)와 `SoulDrop`(밤 사망 시 영혼 생성)은 이번 가치 모델에 직접 들어가지 않는다. `ThornArmor`는 밤에 피해를 받으면 마지막 공격자에게 3 피해를 돌려주는 것으로 읽힌다. 그렇다면 적이 밤에 공격할 때 자기 손실 `R`이 생긴다. 필터 의미(source faction=Enemy)의 정확한 해석은 **확인 필요**.

근거: `DataBaseSO/InGameGroup/Unit/Item/Unit_*.asset`, `Structure/Item/Structure_Headquarters.asset`, `Artifact/Item/Artifact_*.asset`, [CategoryMatchupSpec.cs](../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Commands/CategoryMatchupSpec.cs), [FocusTraitRule.cs](../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Rules/FocusTraitRule.cs).

### 3.2 차이 목록

| No | 대상 상황 | fixture 가정 | 실제 값 | 영향 |
|---|---|---|---|---|
| G1 | R02·R03·R05·R06·R08·R15 | Gravekeeper가 메인에게 6 또는 5 피해 | 공격력 10 × 상성 1.0 = **10** | 메인 피해 가정이 실제보다 작다. R08의 "메인 10"만 실제와 같다. |
| G2 | R02 | Gravekeeper가 HP 10 일반 유닛에게 8 피해 | 10 피해로 **처치** | 실제로는 "메인 10(30점) 대 일반 처치(20+30=50점)"가 되어 메인 압박과 일반 피해의 갈등이 생기지 않는다. 메인 계수 경계(약 26.67) 검증은 지금 유닛 값으로는 재현되지 않는 가설 상황이다. |
| G3 | R05·R06·R08 | Gravekeeper 이동 1칸 후 공격, 환급 0 | Rider 이동비 환급이 공격 뒤 생긴다 | 합법성과 점수에는 영향이 없지만, 행동 후 AP와 `canThreatenNext`가 달라질 수 있다. 환급 조건(턴당 1회, `AttackRefundEligibleMoveCost`)은 Core 값을 그대로 받아야 한다. |
| G4 | R07·R10·R13 | Sniper `availableAp 1 / maxAp 1` | Sniper 최대 AP **3** | 최대 AP 1인 플레이어 유닛은 없다. 실제 Sniper는 공격비 1로 한 턴에 최대 3번 공격한다. R07·R13의 반격 위험은 실제보다 작게 잡혀 있다. |
| G5 | R04·R07·R10·R13 | Sniper 1회 피해 3·4·1 | 공격력 1 + Focus 스택(0~3) = **1~4** | 값 자체는 가능한 범위다. 하지만 스택 수가 입력에 없다. 또 Sniper가 이동하면 스택이 0으로 초기화되므로, `fixed` 가정은 "제자리 공격"일 때만 맞다. |
| G6 | R05·R12 | `low_ap_attacker`(AP 1, 피해 3), `refund_attacker`(AP 3/5, 비용 4, 환급 2) | 대응하는 플레이어 유닛 없음 | 경계 검증용 가상 유닛이다. 실제 연결 검증에는 쓰지 않는다고 fixture에 표시하는 것이 좋다. |
| G7 | R04·R07·R13 | 플레이어 반격이 행위자를 3회 안에 제거 | 플레이어 공격력이 모두 1 | Focus 최대 스택 Sniper(4×3=12)만 한 턴에 Ballista(HP 10)를 제거할 수 있다. 다른 유닛은 한 턴 피해가 5 이하다(가장 큰 것이 메인 1×5회, ShotGun 거리 보정·Akimbo 연쇄는 Core 값 필요). 실제 반격 위험은 대부분 fixture보다 **작다**. |
| G8 | R07·R09 | Ballista 관통 피해 10 → 7 → 4 | 공격력 10, 대상당 30% 감쇠 | 일치. 감쇠 순서가 사선 순서를 따르는지는 `RayHits`로 받아야 한다. |
| G9 | R04·R11 | Ballista AP 5, 이동 3, 공격 4 | 같음 | 일치. 실제로도 이동 후 공격은 기본 AP로 불가다. |
| G10 | R10 | Demon HP 1/5, AP 3 | 같은 규격 | 일치. |
| G11 | 전체 | 낮/밤 구분 없음 | `CombatCommandState.IsNight`, ThornArmor 밤 반사 | 밤에는 적 공격마다 자기 손실이 생길 수 있다. 상황 입력에 낮/밤과 Artifact 효과가 없다. |
| G12 | 전체 | 메인 기준 HP 10(설계 상수) | 메인 실제 최대 HP 50 | 의도된 차이(설계 §5). 그대로 둔다. |
| G13 | 전체 | 이동 후 공격·대기 후보를 자유롭게 비교 | 현행 AI는 제자리 공격이 가능하면 이동하지 않고, 공격은 최대 1회 | 정책 차이가 아니라 후보 생성 차이다. 실제 연결에서는 후보 생성이 먼저 바뀌어야 비교가 의미 있다(설계 §2와 같음). |

### 3.3 1번 작업에 넘길 점

- **G1·G2**: 실제 Gravekeeper는 한 번에 일반 유닛을 처치한다. 메인 압박과 일반 피해의 갈등은 "대상이 Heavy이거나, 피해 감소가 있거나, 일반 유닛 HP가 높은" 변형에서만 생긴다. 지금 DB에 Heavy 플레이어 유닛은 없다.
- **G4·G5·G7**: 반격 위험 상황은 실제 Sniper 규격(AP 3, Focus 스택 명시, 제자리 조건)으로 다시 만들어야 한다.
- **G11**: ThornArmor 밤 반사를 넣은 변형이 "압박과 생존의 충돌" 사례로 쓸 만하다.
- **G6**: 가상 유닛 상황은 경계 검증용으로 남기고, 실제 값 변형과 구분한다.

## 4 입력 계약 초안

실제 연결에서 정책(룰·LLM)이 받는 입력이다. 형식은 오프라인 fixture와 맞추되, 값의 출처를 Core로 바꾼다. Core 요청이 필요한 항목은 §2의 C다.

```text
EnemyDecisionRequest
  key        : BattleInstanceId + 명령 번호 + 행위자 EntityId   ← 늦게 도착한 답을 버리는 기준
  snapshot   : self / players / allies / headquarters / isNight / timeline(다음 내 턴까지)
  candidates : [
    id            : snapshot 안에서 안정적인 문자열 (배열 순서와 무관)
    actions       : 순서 있는 이동·공격 의도 (Core가 다시 실행할 수 있는 형태)
    outcome       : Core 종료 판정 (계속 / 적 승리 / 적 패배)
    hpAfter       : 모든 캐릭터·구조물의 Expected → Final (투영 이벤트 합산)
    apAfter       : 비용·환급 정산 뒤 행위자 AP
    hitIds        : 실제 양의 피해를 받은 플레이어
    position      : 행동 후 행위자 위치
    approach      : 메인 공격 위치까지 비용 (전·후)        ← C
    knockback     : 대상별 넉백 전·후 본진 접근 비용       ← C
    response      : 다음 내 턴 전 상대별 공격 기회와 1회 피해 ← C
    unknowns      : 계산할 수 없던 항목과 이유 (0으로 채우지 않음)
  ]
```

- 후보는 Core가 만들고 Core가 투영한다. 정책은 점수만 매기고 ID 하나를 돌려준다. 정책 쪽에서 피해·합법성을 다시 계산하지 않는다(설계 §9.1).
- 공개 미리보기처럼 **적 진영이 볼 수 있는 환경만** 사용한다(`Environment.VisibleTo`). 숨은 지뢰 정보로 더 좋은 답을 만들지 않는다.
- LLM용 입력은 이 구조에서 필요한 필드만 뽑아 만든다. 3번 작업의 "공통 비교 입력"이 이 부분이다.

## 5 선택 적용 계약 초안

정책이 고른 후보 ID를 실제 전투에 반영하는 경계다. 지금은 `EnemyAiTurnService`가 선택과 실행을 한 번에 한다.

1. **판단과 실행 분리**: Core가 후보와 사실을 만들고(요청), 바깥 정책이 ID를 고르고(응답), Core가 다시 검증해 실행한다.
2. **비동기 허용**: LLM 응답은 수 초가 걸린다. `AdvanceCombat()`은 Job 안의 동기 호출이므로, "요청 게시 → 응답 대기 → 선택 적용"을 Job 두 단계로 나눠야 한다. 이는 View가 명령 Buffer에 쓰고 Core가 읽는 기존 흐름과 같은 모양이다.
3. **재검증**: 적용 시점의 snapshot key가 요청 때와 다르면 거절한다. 같으면 해당 후보의 행동 의도를 기존 `Calculate` 경로로 실행한다. 정책이 준 피해값을 그대로 반영하지 않는다.
4. **중복 실행 금지**: 정책 경로가 켜진 행위자에 대해 기존 자동 적 턴이 따로 돌지 않아야 한다.
5. **실패 복귀**: 시간 초과·형식 오류·없는 ID면 기존 `EnemyAiPolicy` 선택으로 진행한다. 복귀 사유를 기록한다.
6. **폐기**: 씬 종료·포기·전투 종료 뒤 도착한 응답은 버리고 자원을 정리한다(설계 §9.2).

## 6 Core 요청 후보

설계 §9.3의 5개 항목을 이번 대조로 구체화했다. 아직 Core 전달 문서에 넣지 않았다.

| No | 요청 | 지금 막히는 이유 | 관련 차이 |
|---|---|---|---|
| Q1 | 적 행위자 하나의 후보 목록과 후보별 투영 결과(종료 판정, 전체 HP 변화, AP 정산, 행동 후 위치) 조회 | 조합 후보 생성(`Intent/Project/Merge`)과 투영 이벤트가 internal | G13 |
| Q2 | 같은 비용 기준의 "메인 공격 위치까지 이동 AP" 조회, 넉백 전·후 위치의 본진 접근 비용 | 공개 경로 탐색 없음, 넉백 중간 상태 없음 | §2.1, §2.2 |
| Q3 | 투영된 행위자 위치에 대한 플레이어별 제자리 공격 가능 여부와 1회 피해 | 반대 진영의 투영 미리보기 없음 | G4·G5·G7 |
| Q4 | 결과가 무작위·미공개 효과(KillChain 연쇄 등)에 의존하는지 표시 | 공개 미리보기는 무작위 연쇄를 포함하지 않음 | §2.3 |
| Q5 | 선택 ID를 받아 재검증 후 기존 처리 경로로 적용하는 입력, 자동 적 턴과의 중복 방지 | 정책이 `EnemyAiTurnService` 안에 고정 | §5 |

## 7 남은 확인

- 턴 시작 시 AP가 최대치로 채워지는지(§2.3 `availableAp`).
- Rider 환급의 정확한 조건과 횟수(G3).
- ThornArmor 필터의 의미(G11).
- Ballista의 Cannon 카테고리 규칙(SafeBombardment)이 적 선택 가치에 영향을 주는지. 이번에 코드를 대조하지 않았다.
- 위 항목은 Unity에서 해당 상황을 한 번 재생하거나 Core 작업자에게 확인한다.
