# 낮밤 전환(밤 종료 회수) 삭제 설계 (초안)

- 작성: 2026-11-10, Core(천명)
- 상태: 결정 완료(2026-11-10 사용자: Q-NR1 B, Q-NR2~Q-NR5 A). 코드 변경 없음. 설계에서 정한 것 P1~P9는 확인 대상. 독립 검토는 아직 돌리지 않았다.
- Task 계획: [remove-night-cycle-plan](../plans/2026-11-10-remove-night-cycle-plan.md)
- 요구(사용자 2026-11-10): 배치 → 전투 흐름은 그대로 두고, 밤 종료 때의 회수(손패·대기실로 돌려보내고 다시 배치)만 없앤다. 즉 낮밤이 바뀌는 순환을 지운다.

## 1. 지금 상태

- 한 밤의 플레이어 턴 수 `PlayerTurnRules.PlayerTurnsPerNight`가 2026-11-03 커밋 `3a20dc9a3`(용근)에서 8 → 99로 바뀌었다. 그래서 지금 실제로는 99번째 플레이어 턴까지 가지 않는 한 밤이 끝나지 않는다. 이 설계로 동작이 달라지는 경우는 99턴째뿐이다.
- 흐름: 배치(`StagePhase.Deployment`, `IsNight = false`) → 전투 진입(`EnterCombat`, `IsNight = true`) → 플레이어·적 턴 반복 → 99번째 플레이어 턴 뒤 적 국면이 끝나면 `EndNight` → 다시 배치.

### 1.1 밤 종료가 하는 일 (`CombatTurnCalculator.EndNight`, `EndEnemyPhase`)

| 일 | 위치 |
|---|---|
| 보급품 만료 | `HeldEnvironmentEffects.ExpireSupplies` |
| 장착 아이템·아이템 임시 효과 폐기(이유 `RoundEnded`) | `ItemLifetimeCalculator.ClearRoundItems` |
| `NightEnded` 사건 | `CombatTurnEvent` |
| 메인이 아닌 필드 캐릭터 회수: 아군 → 손패, 적 → 대기실(`ClearForReserve`로 상태·임시 진행값 비움) | `CombatPlacementCalculator.RecallForNight` |
| 배치 국면으로, `PreparationIndex + 1`, `IsNight = false` | `EndNight` |
| 사냥꾼 대상 다시 맞추기 | `HunterApplication.Reconcile` |
| 다음 전투 진입에서 대기실 적을 원래 자리로 재배치 | `CombatPlacementCalculator.RedeployEnemies`(전투 진입 때 늘 부름) |

### 1.2 밤 값에 기대는 곳

| 값 | 쓰는 곳 |
|---|---|
| `CombatCommandState.IsNight`(생성자 넷째 필수 인자) | 영혼 제단 영혼 생성(`SoulEffectCalculator.PlanDeaths`·`SoulAltarRule`), 가시 심장 반사(`ThornHeartAttackScope`·`ThornHeartRule`), 보스 포효 피격 수(`RoarHitCounter.Record` 다섯 곳), 보스 포효(`BossRoarCalculator:31`), 사망 정산(`CommandDeathSettlement`), 이동 미리보기 문맥(`MovementPreviewContext.IsNight`·`MovementContextCalculator:142`), 이동 되돌리기(`MovementUndoPolicy:70`), 상태 조립(`CombatTurnCalculator.State`·`MovementProjection.Complete` 인자), 전투 준비 기대값(`BattlePreparation:179`) |
| `CombatTurnFacts.NightStartPlayerTurnCount`·`PlayerTurnsPerNight`·`CompletedPlayerTurnsThisNight` | 밤 종료 판정 `EndsNightAfterEnemyPhase`, 턴 사실 생성자 검사(한 밤 99턴을 넘으면 예외), Buffer 검사(`CombatStateBuffer.cs:309-311`), UI `NightCheckerUI:70-71` |
| `CombatTurnFacts.MainPlacementLocked` | 배치 미리보기에서 메인 재배치 막기(`CombatPlacementCalculator.cs:26`). 전투 진입 때 켜지므로 두 번째 배치부터만 의미가 있다. |
| `CombatTurnFacts.PreparationIndex` | 밤마다 1씩 오름. 아이템 문맥(`ItemDamageContext`·`ItemAttackContext`), 아이템 임시 효과(`ItemTemporaryEffect`, 랜턴·로프), 피해 경계 검사(`MovementProjection.cs:115`), 미사용 계약 `StageSnapshot` |
| 유물 조건 `ArtifactConditionType.IsNight` | 영혼 제단·가시 심장 데이터가 이 조건이어야 한다(`ArtifactEffectCompiler.cs:192,223`). DB 에셋은 숫자로 저장한다. |
| `CombatEventKind.NightEnded` | 변화 근거 판독기, View 사건 형식 표(`Morning` 화면 전환)·사건 소리·적 연출 간격 |
| View 쪽 낮밤 | `CombatSceneDisplay:219` 배경음 `ApplyPhase(IsNight)`, View DB `nightBgmPreset`, 전투 진입 때 밤으로 바뀌는 화면 전환 |

## 2. 설계 요지

| 번호 | 내용 |
|---|---|
| N1 | 밤 종료 흐름을 지운다: `EndEnemyPhase`의 밤 종료 갈래, `EndNight`, `EndsNightAfterEnemyPhase`, `RecallForNight`, `PlayerTurnRules.PlayerTurnsPerNight`. 적 국면 종료는 늘 다음 플레이어 턴 시작으로 간다. |
| N2 | "밤인가"를 "전투 국면인가"로 바꾼다. 지금 도달할 수 있는 모든 상태에서 `IsNight == (Turn.StagePhase == StagePhase.Combat)`이므로 동작이 같다(밤 종료 뒤 배치 국면이 없어지기 때문). 저장 필드 `IsNight`는 지우고, 판정은 `CombatCommandState.InCombat`(국면에서 파생) 하나로 둔다. |
| N3 | 밤에만 의미가 있던 턴 값(`NightStartPlayerTurnCount`·`PlayerTurnsPerNight`·`CompletedPlayerTurnsThisNight`·`MainPlacementLocked`)과 준비 번호 `PreparationIndex`를 지운다(Q-NR3). |
| N4 | 밤 종료 때 끝나던 것은 전투가 끝날 때까지 이어진다: 보급품, 장착 아이템, 랜턴·로프 임시 효과(Q-NR2). 전투 종료 정리(`CombatTemporaryStateCleanup`)는 그대로다. |
| N5 | 공개 멤버는 한 번에 지운다(Q-NR1 B). 그 멤버를 읽는 View·UI 줄(다섯 파일)도 Core가 같은 커밋에서 최소로 고친다. 동작은 지금과 같게 맞추고, 화면을 어떻게 바꿀지는 담당자에게 넘긴다. 고친 줄은 담당자에게 변경 정리로 알린다. |

## 3. 바꾸는 것

### 3.1 밤 종료 흐름 (N1)

- `EndEnemyPhase`: `endsNight` 판단과 `EndNight` 합치기를 지운다. 기믹 종료·정리 뒤 결과를 그대로 돌려준다.
- `EndNight`, `EndsNightAfterEnemyPhase`, `RecallForNight`를 지운다.
  - `RedeployEnemies`는 남긴다. 전투 진입 때 대기실 적을 배치하는 일은 첫 진입에도 쓴다.
  - `CombatReserveReturnCleanup`은 남긴다. 배치 국면의 손패 복귀(`Return`)가 쓴다.
- `HunterApplication.Reconcile(input, …, state, events)`의 밤 종료 호출 자리가 사라진다. 다른 호출자가 없으면 그 겹치기(오버로드)도 지운다.
- `PlayerTurnRules.PlayerTurnsPerNight`를 지운다. 턴 사실 생성자의 "한 밤 99턴" 검사도 지운다. 플레이어 턴 수에 상한이 없어진다(Q-NR5).

### 3.2 밤 판정 → 전투 국면 판정 (N2)

- `CombatCommandState`에서 `IsNight` 필드·생성자 인자·`With(isNight)`를 지우고 `public bool InCombat => Turn.StagePhase == StagePhase.Combat`를 둔다.
- 규칙이 넘기던 `isNight` 인자를 `InCombat`으로 바꾼다.
  - 영혼 제단 `SoulAltarRule.CanSpawn(isNight, …)` → `inCombat`. `SoulEffectCalculator.PlanDeaths`·`CommandDeathSettlement.SettleNewDeaths`·`CombatCommandCalculator:329`.
  - 가시 심장 `ThornHeartRule.EvaluateDamage(isNight, …)` → `inCombat`. `ThornHeartAttackScope` 생성 자리(`AttackDeliveryExecutor:35`).
  - 포효 피격 수 `RoarHitCounter.Record(…, isNight)` 다섯 곳.
  - 보스 포효 `BossRoarCalculator:31`.
- 비교만 하던 곳은 지운다.
  - 이동 되돌리기 `MovementUndoPolicy:70`의 `IsNight` 비교. 같은 줄의 플레이어 턴 검사가 이미 국면을 본다.
  - 이동 미리보기 문맥 `MovementPreviewContext.IsNight`와 캐시 비교 `MovementContextCalculator:142`. 같은 비교에 `StagePhase`가 이미 있다.
- 상태 조립 인자 `CombatTurnCalculator.State(…, isNight)`, `MovementProjection.Complete/BeginComplete(…, isNight)`, `CompletionWork`의 `isNight`를 지운다. 전투 진입은 턴 사실만 `Combat`으로 바꾸면 된다.
- 유물 조건 `ArtifactConditionType.IsNight`는 이름만 `InCombat`으로 바꾼다(Q-NR4 A). 열거 숫자는 그대로라 DB 에셋을 고치지 않는다. 컴파일러 문구 "requires IsNight"도 바꾼다.

### 3.3 턴 값·준비 번호 정리 (N3, Q-NR3 A 기준)

- `CombatTurnFacts`: `PreparationIndex`·`MainPlacementLocked`·`NightStartPlayerTurnCount` 생성자 인자와 속성, `PlayerTurnsPerNight`, `CompletedPlayerTurnsThisNight`를 지운다. `SameValues`와 검사도 맞춘다.
  - 생성자 위치 인자가 바뀌므로 생성 자리 아홉 곳(`CombatTurnCalculator` 다섯, `NexusReinforcementCalculator:84`, `BattlePreparation`, `CombatCommandState` 기본값, `StageSnapshot`)을 함께 고친다. Buffer는 턴 사실을 만들지 않고 받기만 한다.
- 배치 미리보기의 `MainPlacementLocked` 조건(`CombatPlacementCalculator.cs:26`)을 지운다. 배치 국면은 이제 첫 전투 진입 전뿐이고, 그때 값은 늘 거짓이다.
- 준비 번호를 아이템 계약에서 지운다.
  - `ItemDamageContext`·`ItemAttackContext`의 `PreparationIndex`.
  - `ItemTemporaryEffect`의 `PreparationIndex`와 `CharacterItemState` 비교(`:38`).
  - 랜턴(`LanternItemRule:24`)·로프(`RopeItemRule:32`) 임시 효과 생성 인자.
  - `CombatDamageApplication.PreparationIndex`와 생성자 인자, `MovementProjection.cs:115` 검사.
  - 미사용 계약 `StageSnapshot.PreparationIndex`(생성 자리 0곳). 계약 자체를 지울지는 이 작업 범위 밖이라 보고만 한다.
- 아이템 폐기 이유 `ItemEventReason.RoundEnded`는 생산자가 없어진다. 번호를 비워 두고 멤버를 지운다. Core 밖에서 읽는 곳은 없다.

### 3.4 사건 (N5)

- `CombatEventKind.NightEnded = 15`를 지우고 "번호 15는 재사용하지 않음" 주석을 둔다(번호 30과 같은 방식).
- 변화 근거 판독기의 `NightEnded` 갈래와 `CombatTurnEvent` 허용 종류에서 뺀다.
- View에서 이 값을 읽는 세 줄은 §4대로 같은 커밋에서 지운다.

### 3.5 Buffer

- `CombatStateBuffer.cs:309-311`의 밤 시작 턴 수 검사를 지운다.

## 4. 다른 작업자 영역 (Core가 같은 커밋에서 고치는 줄)

Q-NR1 B(사용자 2026-11-10)로 아래 줄은 Core가 함께 고친다. 동작은 지금과 같게 맞춘다. 화면을 어떻게 바꿀지는 담당자가 나중에 정한다.

| 자리 | 지금 | 이번에 고치는 것 |
|---|---|---|
| UI `NightCheckerUI:70-71` | "국면 · 이번 밤 완료 턴/99"와 진행 막대 | "국면 · 완료 플레이어 턴 수"만 보이고(분모 없음), 진행 막대는 0으로 둔다(P9). 관련 필드(`_shownNightLength`)는 정리한다. UI를 없앨지·다시 꾸밀지는 담당자 |
| View `CombatSceneDisplay:219` | 배경음 `ApplyPhase(state.CommandState.IsNight)` | `ApplyPhase(state.CommandState.InCombat)`. 배치 = 낮 배경음, 전투 = 밤 배경음(지금과 같음) |
| View `CombatEventTypeTable:136` | `NightEnded` → `Morning` 화면 전환 | `NightEnded` 갈래만 지운다. 전투 진입 → 밤 전환은 그대로 |
| View `CombatEventSounds:134` | `NightEnded` → 낮 배경음 | 갈래 삭제 |
| View `CombatEnemyPresentationPacing:89` | `CombatEntered`·`NightEnded`면 간격 기록 비움 | `NightEnded` 조건만 삭제 |
| Buffer `CombatStateBuffer.cs:309-311` | 밤 시작 턴 수 검사 | 삭제(§3.5) |

고치지 않는 것(담당자 결정):

- View DB `CombatPresentationSettingsDataSO.nightBgmPreset`과 `CombatBgmPolicy`의 `Day`/`Night` 이름. 값은 그대로 쓰인다.
- 전투 진입 때 밤으로 바뀌는 화면 전환과 배경음. Core는 `CombatEntered` 사건을 그대로 낸다(P8).

고친 줄의 작성자는 구현 때 `git blame`으로 확인하고, Core 전달 문서와 해당 작업자 메모에 변경 정리를 적는다. 전달은 사용자가 한다.

## 5. 기획에 알릴 것

- 밤 순환이 없어져 전투는 승리·패배 조건으로만 끝난다. 턴 상한이 없다(Q-NR5).
- 회수·재배치가 없다. 손패 유닛은 첫 배치 이후 다시 낼 기회가 없다(지금도 99턴 때문에 사실상 같음).
- 보급품, 장착 아이템, 랜턴·로프의 불리한 임시 효과가 전투 끝까지 남는다(Q-NR2).
- 영혼 제단·가시 심장의 "밤에만" 조건은 "전투 중에만"이 된다(지금과 같은 동작).

## 6. 시험 영향

| 묶음 | 내용 |
|---|---|
| 지울 시험 | 밤 종료 자체를 보는 시험: `EightTurnNightTests`, `BossNightLifecycleTests`, 그리고 밤 종료 사례가 든 파일(약 25개: `CombatItemLifetimeTests`·`HeldEnvironmentEffectTests`·`HunterTraitTests`·`NexusReinforcementLifecycleTests`·`PlayerTurnRuleTests` 등)의 해당 사례 |
| 호출 맞춤 | `new CombatCommandState(…)` 139곳(74개 파일)에서 넷째 인자 삭제. `new CombatTurnFacts(…)` 89곳(55개 파일) 중 위치 인자 7개 이상 25곳이 크게 바뀜 |
| 의미 확인 | `isNight: true`를 넘기면서 턴을 기본값(배치 국면)으로 둔 시험은 영혼·가시 심장·포효 조건이 바뀐다. 턴을 전투 국면으로 바꿔야 같은 내용을 확인한다. 개수는 구현 때 센다 |
| Core 계약 시험 | `CombatChangeEvidenceCoverageTests`(사건 종류 전체), 턴 사실·상태 계약 시험 |

시험은 Core 시험 어셈블리만 고친다. 다른 사람이 쓴 시험이 있으면 고치기 전에 묻는다.

## 7. 단계(Phase 안)

| Phase | 내용 | 동작 변화 |
|---|---|---|
| NR-a 흐름 | §3.1 밤 종료 흐름 삭제, 턴 상한 삭제, 밤 종료 시험 정리 | 99턴째만 |
| NR-b 판정 | §3.2 `IsNight` → `InCombat`, 유물 조건 이름, View 배경음 줄(§4) | 없음 |
| NR-c 계약 | §3.3 턴 값·준비 번호 정리, §3.4 `NightEnded` 삭제, §3.5 Buffer, §4의 나머지 View·UI 줄 | 없음 |
| NR-d 공유 | §4 변경 정리(작성자 확인), §5 기획 공유, 적 전용 특성 설계에서 Q-ED5·§4.6 삭제 | — |

각 Phase는 그 Phase에서 지운 멤버를 읽는 View·UI 줄까지 함께 고쳐, Phase 끝마다 컴파일이 되게 한다.

Phase마다 멈추고 보고한다. 확인은 Test Runner로 한다.

## 8. 적 전용 특성 설계에 주는 영향

- [적 전용 특성 설계](2026-11-10-enemy-death-traits-design.md)의 Q-ED5(재구성 중 밤 종료 회수)와 §4.6, 검토 지적 L5가 없어진다.
- 재구성의 "자기 턴 시작 2번째"와 공포 기간은 진영 턴 기준이라 영향이 없다.
- 두 작업이 같은 파일(`CombatTurnCalculator`)을 고친다. 밤 삭제를 먼저 하면 재구성 부활 자리(턴 시작)를 정리된 코드 위에 넣을 수 있다.

## 9. 결정 대기

2026-11-10 사용자 결정.

| 번호 | 질문 | 결정 |
|---|---|---|
| Q-NR1 | View·UI가 읽는 공개 멤버를 어떻게 지우나? | **B** 한 번에 지우고 View·UI 줄까지 Core가 고친다(§4). |
| Q-NR2 | 밤 종료 때 끝나던 보급품·장착 아이템·랜턴/로프 임시 효과 | **A** 전투 끝까지 유지. |
| Q-NR3 | 준비 번호 `PreparationIndex` | **A** 이번에 지운다(아이템 계약 포함). |
| Q-NR4 | 유물 조건 `IsNight` | **A** 이름만 `InCombat`으로(숫자·에셋 그대로). |
| Q-NR5 | 플레이어 턴 수 상한 | **A** 두지 않는다(승패 조건으로만 끝남). |

## 10. 설계에서 정한 것 (확인 대상)

| 번호 | 내용 |
|---|---|
| P1 | `RedeployEnemies`(전투 진입 때 대기실 적 배치)는 남긴다. |
| P2 | `CombatReserveReturnCleanup`(배치 국면 손패 복귀)은 남긴다. |
| P3 | 밤 판정은 저장하지 않고 턴 국면에서 파생한 `InCombat` 하나로 둔다. |
| P4 | 이동 되돌리기·이동 미리보기 캐시의 밤 비교는 지운다(같은 줄의 국면 비교로 충분). |
| P5 | `MainPlacementLocked`는 지운다(두 번째 배치가 없음). |
| P6 | 지운 사건·이유 번호(`NightEnded = 15`, `RoundEnded`)는 다시 쓰지 않는다. |
| P7 | 미사용 계약 `StageSnapshot`은 지우지 않고 보고만 한다. |
| P8 | 전투 진입 때의 밤 화면 전환·밤 배경음은 지금 그대로 둔다. 없앨지는 View 담당이 정한다. Core는 `CombatEntered` 사건을 그대로 낸다. |
| P9 | `NightCheckerUI`는 "국면 · 완료 플레이어 턴 수"만 보이고 진행 막대는 0으로 둔다(UI 모양은 담당자가 나중에). |
