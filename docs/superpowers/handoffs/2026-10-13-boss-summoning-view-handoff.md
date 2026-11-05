# 보스 전투 Core 변경 — View 인계 (2026-10-13)

작성: Core 작업(천명). 대상: 전투 View 작업자.
Core 기준: `feature/Cheon/refactor/core-system` 작업 트리(커밋 전). 계획 원본은 boss-summoning-core-plan (Git history: `2026-10-13-boss-summoning-core-plan.md`)이며 이 문서는 View가 읽어야 하는 계약만 모았다.

> 이 문서는 Core가 **무엇을 게시하는지**만 설명한다. View 코드는 Core 작업에서 바꾸지 않았다. 아래 "View에서 확인할 곳"은 Core가 찾은 후보이며, 표시 방식은 View가 결정한다.

## Phase 6 completion — 2026-10-14

The adapter forwards authored objective kind and per-placement target flags. Actual Stage SO -> default source -> Core preparation -> Buffer target binding is verified, including reordered same-key enemies and changing which placement is marked. The user's approved follow-up replaced UnitDefinition's per-unit whitelist with defined enum names beginning with ordinal `Unit_`; undefined/None/structure keys and invalid main-role combinations remain rejected. No per-boss dispatch is needed.

Final Unity EditMode: **287 passed, 1 failed, 288 total, 0 skipped**, exit 2. Adapter 111/111; key/static-stat tests 37/37; all 59 new Phase 6 cases pass. The five earlier key-rejection failures are resolved. The sole Stage 1101 terrain-coordinate assertion exactly matches the fresh baseline. This is not a fully green selection. Details: stage execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`). No operational asset/View changes or PlayMode verification. Phase 7 remains a separate, unexecuted content/visual activation checkpoint.

## Current integration status — 2026-10-14

**Threshold follow-up verification:** Unity 6000.0.73f1 EditMode **329/329 passed**, 0 skipped, exit 0 (`roar-threshold-final2.xml`). Covers authored thresholds, initial SO-to-Buffer publication, hit increments, roar resets, turn-end preservation, and related boss/character/Trait regressions. This is a targeted selection, not a full-suite or View/PlayMode result. The earlier Phase 6 terrain failure was not retested or fixed here. Execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#follow-up--roar-threshold-publication-and-view-ownership-2026-10-14`).

The summon/roar/objective event contracts remain unchanged. The follow-up adds `RoarTraitStatusSnapshot.HitThreshold` for View display (§5.5). Boss SO authoring and UnitData-to-Core conversion, including summon definitions/profiles, are complete. This supersedes the original statement that boss data did not exist. The boss and targeting assets are still **unregistered**, and the live boss stage has not been switched to a designated-target battle.

| Reference for View coordination | Current value/status |
|---|---|
| Boss key | `EntityKey.Unit_Stage1Boss = 18` |
| Summon key | `EntityKey.Unit_Gravekeeper = 17`; same definition as ordinary Gravekeeper |
| Prepared boss SO | `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_Stage1Boss.asset` |
| Prepared targeting SO | `Assets/ProjectL/DataBaseSO/InGameGroup/Targeting/Item/TargetingMethod_Stage1BossRoar.asset`, key 5 |
| Confirmed skill values | Summon cap 2 / first delay 7; Roar threshold 5; Knockback distance 3; Fan/All/None/0 |
| Generic stage connection | Phases 5-6 complete through actual Core preparation/Buffer target binding; design (Git history: `2026-10-14-boss-stage-objective-design.md`), Task plan (Git history: `2026-10-14-boss-stage-objective-plan.md`) |
| Operational activation | Phase 7 is View-owned SO/asset configuration, registration, presentation, and gameplay verification; no Core permission is needed for this content work |

The values/asset paths above are coordination information, not permission for View to query InGame SOs or hardcode rules. Target highlighting must use `Objective.TargetEntityId`, not key 18, stage name, or Boss category; repeated instances of one unit key are possible. Future boss stages use the same authored-objective path. Runtime event handling should remain generic for any summoner/roaring unit.

View can prepare UnitVisual, summon/roar event playback, movement event sequencing, objective presentation, and the `HitCount/HitThreshold` counter now. Generic stage connection is complete; end-to-end operational acceptance still needs asset activation and View playback.

### Starting View work

1. Use the working tree containing Phases 1-6. For another checkout, first receive the user's commit/merge of the code, prepared assets and their `.meta` files, and these documents; the current changes are uncommitted.
2. Implement the presentation contracts in §§1-6 and supply/register UnitVisual for key 18. The file list in §7 is a starting point to inspect, not a completed View audit.
3. Perform SO authoring, stage placement, and Unit/Targeting/UnitVisual registration using Phase 7 (Git history: `2026-10-14-boss-stage-objective-plan.md#phase-7--view-owned-stage-1-operational-configuration`), then verify the operational battle. View may create or modify these SOs and their authoring/data code without Core approval; record the chosen lineup and positions as content configuration.
4. Consult the Core programmer only if implementation requires changing code under `RuntimeSystem/InGameSystem/CoreSystem/` (including its Contracts and calculators). Existing SO fields, table registration, placement values, View playback, and use of published Buffer data are not Core approval gates. Runtime View code still reads rules through Buffer, not InGame SO queries.

## 0. 원칙(변경 없음)

- View는 Buffer를 읽기만 한다. 개체 수·위치·HP·타임라인·승패·포효 대기 여부를 **스스로 계산하지 않는다.**
- 사건(`CombatEvent`)은 계산 순서대로 나온다. 연출은 사건 순서를 따른다.

## 1. 한눈에 보기

| 기능 | View가 읽는 것 | 새 값 |
|---|---|---|
| 지정 보스 목표 | `CombatCommandState.Objective` | `BattleObjective.Kind`, `TargetEntityId` |
| 보스 승리 | `Completion.Outcome.Reason` | `StageOutcomeReason.DesignatedBossDefeated` |
| 소환(생성) | 사건 `CombatSummonEvent` | `CombatEventKind.Summoned` |
| 소환수 출처 | `CombatCommandState.Turn.Summons` | `CombatSummonRecord(SummonedId, SummonerId, Ability)` |
| 낮 동안 대기 | 캐릭터 `Area` | `CharacterRuntimeArea.EnemyWaiting`(소환수도 해당) |
| 거리 N 강제 이동 | 사건 `CombatCharacterEvent` | 칸마다 `Moved` 하나씩 |
| 포효 | 사건 `CombatRoarEvent` | `CombatEventKind.Roared` |
| 포효 카운터 | `Stats.TraitStatuses` | `RoarTraitStatusSnapshot.HitCount`, `HitThreshold` |
| 행동 제한 실패 사유 | 공격 범위·미리보기 실패 | `AttackFailureReason.BasicAttackDisabled`, `MovementFailureReason.VoluntaryMoveDisabled` |
| 보스 Category | `Stats.Category.Key` | `CategoryKey.Boss = 50` |

## 2. 지정 보스 목표와 승리

- Read the objective from `CombatCommandState.Objective`, independently of whether a Headquarters structure exists. Removing the headquarters is part of the proposed Stage 1 content setup, not a generic Core guarantee; the live stage asset has not yet been changed.
  - `Kind == BattleObjectiveKind.DesignatedBossDefeated`이면 `TargetEntityId`가 목표 보스다. 보스 표시(마커 등)는 이 ID로 고른다.
  - 일반 전투는 `Kind == EnemyHeadquartersDestroyed`(기존과 같음).
- 목표 보스가 죽으면 다른 적(소환수 포함)이 살아 있어도 승리한다. 완료 사유는 `StageOutcomeReason.DesignatedBossDefeated`(값 6)다.
  - 메인과 보스가 같은 처리에서 함께 죽으면 기존처럼 **패배가 우선**한다.
- 보스는 공격 직접 피해뿐 아니라 낙사·환경 폭발·철도 즉사로도 죽을 수 있으며, 어느 경로든 같은 승리 사유가 나온다.

## 3. 사령술 소환

### 3.1 생성 사건

- 보스 턴에 소환이 성공하면 `CombatSummonEvent`(`Kind == Summoned`)가 나온다.
  - `Record`: `SummonedId`(새 개체 ID), `SummonerId`(보스), `Ability`(`TraitKey.Necromancy`).
  - `Created`: **등장 효과를 받기 전** 생성 시점 사실이다. 등장 칸의 환경 효과(지뢰 등)는 이 사건 **뒤에** 일반 사건으로 나온다.
- 새 개체는 transition의 `Characters.Created`로 게시된다(기존 개체의 `CharacterStateChange`가 아니다). Buffer에서는 그냥 새 캐릭터가 생긴 것으로 보인다.
- 소환수는 지금 기존 유닛 `Unit_Gravekeeper`(묘지기)와 **같은 정의**를 쓴다. 일반 묘지기와 소환 묘지기는 `Turn.Summons`에 기록이 있는지로만 구분한다.
- 소환 직후 즉사(등장 칸 지뢰 등)도 가능하다. 그때도 `Summoned` 뒤에 `Died`가 나온다.
- 소환수의 첫 행동은 소환 시각 + N(행동 타임라인 시간)이다. 타임라인은 Core가 계산해 `Timeline`에 넣으므로 View는 그대로 읽는다.

### 3.2 낮·다음 밤

- 밤이 끝나면 보스와 살아 있는 소환수는 `Area == EnemyWaiting`이 된다(필드에서 사라짐). **같은 EntityId**가 유지된다.
- 다음 밤 전투 진입 때 다시 필드에 나온다. 이때는 생성이 아니라 기존 `Placed` 사건이다(`Summoned` 아님).
  - 보스는 원래 위치(막히면 가까운 적 배치 칸), 소환수는 보스 주변 8칸 중 하나로 다시 정해진다.
  - 소환수 자리가 없으면 `EnemyWaiting`으로 남고 사건이 없다. 전투 진입은 그대로 진행된다.
- 죽은 소환수는 돌아오지 않는다.

### 3.3 예시 사건 순서 — 보스 턴

```text
(대기 포효가 있으면) Roared → 포효 피해·넉백 사건들 (§5)
Summoned(Record, Created)
  → (등장 칸 효과가 있으면) HealthChanged / Died / ObjectActivated ...
TurnEnded
```

소환할 자리가 없거나 생존 상한(2)이면 `Summoned` 없이 `TurnEnded`만 나온다. 보스는 이동·기본 공격을 하지 않으므로 보스 턴에 `Moved`·`Attacked`가 없다.

## 4. 거리 N 강제 이동(넉백·타일 강제 이동)

- 넉백과 타일 강제 이동은 이제 2칸 이상 밀 수 있다. **한 칸마다 `Moved` 사건이 하나씩** 나온다. View는 요청 거리로 최종 위치를 추정하지 말고 사건을 따라간다.
- 지나가는 칸의 지뢰·깃발·보안관 배지·가시·낭떠러지가 그 칸에서 바로 발동한다. 도중에 죽거나 떨어지면 남은 칸의 `Moved`는 없다.
- 막히면 첫 막힌 칸 바로 앞에서 멈춘다(충돌 피해 없음).
- **강제 이동 면역 유닛**(보스)은 피해만 받고 `Moved`가 없다. 토네이도가 면역 유닛 칸에서 발동하면 토네이도만 옮겨진다.

## 5. 보스 포효

### 5.1 규칙(표시에 필요한 만큼)

- 밤에 보스가 5번 맞으면(공격 하나와 그 여파는 1회) 원래 행동이 끝난 뒤 포효한다.
- 포효는 보스 주변 4방향 부채꼴 전체를 한 번에 때리고, 맞은 적대 유닛을 3칸 민다. 정확한 대각선 칸은 범위 밖이다.
- 포효하면 카운터가 0이 된다.

### 5.2 결과 계약

- 포효 결과는 **transition의 `Attack`이 아니다.** 플레이어 공격으로 포효가 나도 결과의 `Attack`은 플레이어 공격 그대로다.
- 포효는 `CombatRoarEvent`(`Kind == Roared`) 하나로 표시되고, 그 뒤에 일반 `HealthChanged`·`Died`·칸별 `Moved` 사건이 이어진다.
  - `BossId`: 포효한 보스.
  - `Attack`(`AttackResult?`): 포효 공격 결과(맞은 대상·전후 HP 등). 범위 안에 대상이 없으면 **null**이다(포효는 했고 카운터는 0이 된다).
  - `AreaCells`(2026-10-15 추가, View 작업·사용자 지시, [데이터 경계 §5.27](2026-09-24-view-core-data-boundary-handoff.md)): 포효가 덮는 칸(보드 안, 벽 가림 적용). 대상이 없어도 채우고, `Attack`이 있으면 그 `AffectedCells`와 같다. 표시 전용이다.
  - 포효의 `AttackResult.ActionCostSettlement.Spent`는 **0**이다(비용 없는 반응). 일반 공격 결과는 기존처럼 1 이상이다.
- 포효가 현재 행동 중인 플레이어 유닛을 죽이면 `ActiveActorId`가 비고 국면이 `Automatic`이 된다.

### 5.3 예시 사건 순서 — 플레이어 공격이 5번째 피격

```text
EffectsActivated(공통 피해 발동이 있으면)
Attacked(플레이어 공격 결과)      ← transition.Attack도 이것
HealthChanged(보스) ...
(플레이어 공격의 넉백·도착 효과)
EffectsActivated(포효 쪽 공통 피해 발동이 있으면)
Roared(BossId, 포효 AttackResult)
HealthChanged(범위 안 적대 유닛들, ID 순)
Moved, Moved, Moved(유닛별 칸마다)
(지나간 칸 효과·사망·영혼 등)
StageEnded(포효로 전투가 끝난 경우만)
```

### 5.4 미리보기

- 공격 미리보기(`AttackPreview`)에는 포효가 **들어가지 않는다.** 5번째 피격이 될 공격이라도 미리보기는 그 공격만 보여준다.

### 5.5 카운터 표시

- 보스의 `Stats.TraitStatuses`에 `RoarTraitStatusSnapshot`이 있고 `HitCount`가 현재 횟수다(낮에도 유지).
- `HitThreshold` is now published in the same snapshot, copied from the Core Roar definition. Render the counter as `$"{status.HitCount}/{status.HitThreshold}"`; the prepared Stage 1 boss can show `4/5`, while another authored threshold displays its own value. Do not hardcode 5 or query InGame SOs from runtime View.
- The threshold remains unchanged when hit count increases or resets after a roar. Both values participate in `SameValues`, so snapshot comparisons detect threshold changes too. Counters may temporarily reach/exceed the threshold; View must not infer that a roar occurred from the counter. Play the `Roared` event.
- Constructor contract: `new RoarTraitStatusSnapshot(hitCount, hitThreshold)`; threshold is required and must be positive. There is no implicit Stage 1 default. Runtime state still stores only the count; the definition remains the rule source.

## 6. 새 실패 사유

- 보스는 자발 이동·기본 공격이 막힌 유닛이다(DB 값). 이런 유닛에 대해 범위·미리보기를 요청하면 다음 사유가 돌아온다.
  - 공격: `AttackFailureReason.BasicAttackDisabled`(24)
  - 이동: `MovementFailureReason.VoluntaryMoveDisabled`
- 플레이어 유닛에는 지금 해당이 없지만, 실패 사유를 문구로 바꾸는 곳이 있으면 두 값을 추가해 달라.

## 7. View에서 확인할 곳(Core가 찾은 후보, 수정하지 않음)

| 파일 | 확인할 점 |
|---|---|
| `ViewSystem/Combat/CombatSceneEventPresenter.cs` | `Summoned`·`Roared` 사건을 표시할지. 지금은 모르는 사건을 건너뛰는지 확인. `UnitPreset`은 적이면 5라 Boss Category와 무관 |
| `ViewSystem/Combat/OrderedCombatPresentation.cs`, `CombatMovementPlayback.cs` | 한 유닛의 연속 `Moved`(넉백 N칸)를 칸마다 재생하는지 |
| `ViewSystem/Combat/CombatEnemyPresentationPacing.cs` | 보스 턴(이동·공격 없이 소환만)과 `Roared` 간격 |
| `ViewSystem/Combat/CombatBallistaImpactMap.cs`, `GlobalSystem/UISystem/Screens/Stage/CombatLogUI.cs` | `CombatAttackEvent`만 공격으로 본다. 포효를 로그·충격 표시에 넣으려면 `CombatRoarEvent.Attack`을 따로 읽어야 한다 |
| `ViewSystem/Combat/Display/CombatHud.cs` | 완료 문구(지정 보스 승리도 Victory), 실패 사유 문구 |
| 보스 마커 | `Objective.TargetEntityId`로 표시. 본부가 없는 전투 처리 |

## 8. Remaining work and evidence (updated 2026-10-14)

- Core/data: generic Stage SO objectives and per-enemy-placement target flags now reach actual Core preparation and Buffer. No stage/boss-specific code branch. The proposed Stage 1 setup removes its current headquarters; that production asset edit has not happened.
- View: supply/register UnitVisual for key 18, consume Summoned/Roared and sequential Moved events, and use the runtime target EntityId for objective presentation. The candidate file list in §7 is historical guidance, not a verified current View implementation audit.
- Activation (View-owned Phase 7): author the lineup/coordinates, register Unit/Targeting/UnitVisual data, configure the boss stage, and verify gameplay. SO implementation and registration do not require Core approval. Only changes to InGame CoreSystem code need Core consultation. Current summons reuse the existing Gravekeeper identity.
- Latest Phase 6 selection: 287/288 passed, including all 59 new cases; adapter 111/111 and key/static-stat fixture 37/37. The existing Stage 1101 terrain-coordinate failure exactly matches baseline. Details: Stage execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`). This verifies generic SO-to-Core target binding and selected regressions, not View playback or real operational stage success.
- Core integration and the approved unit-key validation repair are complete. Operational assets, View code, and existing event contracts were preserved. No external delivery or PlayMode run.

### 8.1 View 상태 (2026-10-15, View 작업)

실행 원본은 [보스 View 활성화 계획](../plans/2026-10-15-stage1-boss-view-activation-plan.md) §6, 결정은 [설계](../specs/2026-10-15-stage1-boss-view-activation-design.md) D1~D8·D6a다. Core 계약은 바꾸지 않았다.

| 항목(§1~§7) | View 처리 |
|---|---|
| 보스 외형·등록 | `UnitVisual_StageBoss1`(key 18) 등록, 보스·지정 방식 표 등록, `Combat_Boss` 보스 단독 (4, 8) R0 |
| §2 목표·승리 | `Objective.TargetEntityId`로 대상 칸에 목표 강조 층, 유닛 HUD "격파 목표", `DesignatedBossDefeated` 승리 문구 "보스 격파 · 결과 확인" |
| §3 소환 | 생성 칸 재생 강조 → `Created` 사실 표시 → 바닥 아래에서 Y축으로 돌며 올라오는 등장 동작 → 등장 뒤 강조 끔. 간격에서 소환자의 행동으로 셈 |
| §4 넉백 N칸 | 칸별 `Moved`를 기존 이동 재생으로 처리. 포효 뒤 넉백을 앞 자발적 경로와 섞던 경계 문제를 고침 |
| §5 포효 | 영향 칸(대상이 없으면 `AreaCells`) 재생 강조 → 대기 → 보스 커짐(테스트 값) → 커진 채 보스 외형의 공격 모션(Melee) 한 번 → 작아짐 → 강조 끔 → 피해·넉백 사건. 대상이 없어도 같은 순서다. 공격 모션은 보스가 지금 보는 방향으로 재생한다. 전체 지정이라 `Attack.AttackDirection`이 R0 기본값이어서 쓰지 않는다. 효과음 없음(Audio 제외) |
| §5.5 카운터 | HUD 상태 칸 설명 "포효 {HitCount}/{HitThreshold}". `HUD.prefab` 설명 칸 미연결로 화면에는 아직 안 보임(현강님 메모) |
| §7 `CombatBallistaImpactMap`·`CombatLogUI` | 바꾸지 않음. Ballista 외형 포효는 공격 연출 없이 사실만 보이고, 전투 로그 포효 줄은 범위 밖 |

검증: MSBuild 오류 0, 관리 실행 회귀는 새 실패 0이다. 개발 진입에서 보스 등장을 사용자가 확인했다. **Unity EditMode·운영 보스 전투(진입·소환·포효·목표 처치·완료) 확인은 대기 중이다.**
