# 보스 전투(소환·포효·거리 N 강제 이동·지정 보스 승리) Core 인계 (2026-10-13)

작성: Core 작업(천명). 브랜치 `feature/Cheon/refactor/core-system`, 커밋 전 작업 트리.
원본: 계획 (Git history: `2026-10-13-boss-summoning-core-plan.md`)(§0 결정 D1~D14, §1.3 Gate 답변, Phase 1~6 실행 기록). 설계 문서보다 계획이 우선한다.
View 담당 문서는 따로 있다: [View 인계](2026-10-13-boss-summoning-view-handoff.md).

> 인계 문서 작성은 외부 발송을 뜻하지 않는다. 전달은 사용자가 한다.

## Phase 6 completion — 2026-10-14

The adapter forwards authored objective kind and per-placement target flags. Actual Stage SO -> default source -> Core preparation -> Buffer target binding is verified, including reordered same-key enemies and changing which placement is marked. The user's approved follow-up replaced UnitDefinition's per-unit whitelist with defined enum names beginning with ordinal `Unit_`; undefined/None/structure keys and invalid main-role combinations remain rejected. No per-boss dispatch is needed.

Final Unity EditMode: **287 passed, 1 failed, 288 total, 0 skipped**, exit 2. Adapter 111/111; key/static-stat tests 37/37; all 59 new Phase 6 cases pass. The five earlier key-rejection failures are resolved. The sole Stage 1101 terrain-coordinate assertion exactly matches the fresh baseline. This is not a fully green selection. Details: stage execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`). No operational asset/View changes or PlayMode verification. Phase 7 remains a separate, unexecuted content/visual activation checkpoint.

## Current integration status — 2026-10-14

**Threshold follow-up verification:** Unity 6000.0.73f1 EditMode **329/329 passed**, 0 skipped, exit 0. This targeted boss/character/Trait/adapter selection does not rerun the earlier terrain failure or verify View/PlayMode. Execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#follow-up--roar-threshold-publication-and-view-ownership-2026-10-14`).

Follow-up: `RoarTraitStatusSnapshot` now publishes positive `HitThreshold` alongside `HitCount`, copied from RoarTraitDefinition and preserved through increments/resets. Constructor callers must pass both values; equality compares both. Rule evaluation still uses the definition/spec, and no new runtime-state setting or service was added. View can render `4/5` entirely from Buffer; see [View §5.5](2026-10-13-boss-summoning-view-handoff.md#55-카운터-표시).

**Ownership correction requested by the user:** View may implement/edit SO authoring/data types, author stage placements, register Unit/Targeting/UnitVisual tables, and verify gameplay without Core permission. Consult the Core programmer only for code changes under `RuntimeSystem/InGameSystem/CoreSystem/`, including Contracts/calculators. Runtime View continues to read rules through Buffer. This supersedes earlier references to coordinated activation or Core/data-owned registration.

Boss SO/Core-definition Phases 1-4 and Stage authoring/integration Phases 5-6 are complete; see stage design (Git history: `2026-10-14-boss-stage-objective-design.md`) and Task plan, Phases 5-7 (Git history: `2026-10-14-boss-stage-objective-plan.md`). These documents govern the integration work; the original 2026-10-13 plan remains the source for existing combat algorithms. This status supersedes older missing-SO/adapter statements. No operational stage activation or Unity PlayMode success is claimed.

Future bosses use authored objective kind and per-placement target flags. Never dispatch by stage ID/name, boss key, or category. The runtime target is the EntityId allocated to the marked placement; SOs do not store runtime IDs. Only one designated target per battle is currently supported.

## 1. 완료 범위

Core calculations and Buffer publication are implemented. Unit SO authoring, Trait/Behavior conversion, summon definition/profile assembly, Stage objective/placement authoring, forwarding, and preparation integration are complete as of 2026-10-14. View-owned live asset activation and gameplay verification remain pending.

| 기능 | 핵심 계약 |
|---|---|
| 지정 보스 승리 | `CombatCommandState.Objective`(`BattleObjective`), `StageOutcomeReason.DesignatedBossDefeated`. 메인 동시 사망은 패배 우선 |
| 행동 제한 | `UnitSpec.Behavior`(`UnitBehaviorSpec`: 자발 이동·기본 공격 가능 여부, 강제 이동 면역). 판정은 `UnitBehaviorRules` 한 곳 |
| Boss Category | `CategoryKey.Boss = 50`, 규칙 없음. 상성 Boss 양방향 9항목 1.0(`CategoryMatchupSpec.CreateDefault`) |
| 전투 중 생성 | `CombatCommandState.Allocation`(`NextEntityId`, 소환 난수), `CombatTurnFacts.Summons`, transition `Characters.Created` |
| 사령술 | `NecromancyTraitSpec(소환 유닛 키, 생존 상한, 첫 행동 지연 N)`, `TraitKey.Necromancy = 80`. 보스 턴 = (대기 포효) → 소환 1회 시도 → 턴 종료 |
| 밤·낮 | 보스·소환수는 낮에 `EnemyWaiting`, 같은 ID로 다음 밤 복귀(소환수는 보스 주변 재추첨) |
| 거리 N 강제 이동 | `ForcedDisplacementPathCalculator`(internal). 지나가는 칸마다 지형·가시·오브젝트, 칸마다 `Moved` |
| 포효 | `RoarTraitSpec(hitThreshold)`, `TraitKey.Roar = 90`, `RoarTraitStatusSnapshot.HitCount`. 반응 결과는 `CombatRoarEvent`(`Roared`)로만 |

## 2. DB authoring and approved values (updated 2026-10-14)

Values below were explicitly approved by the user, including first-action delay 7; they are no longer merely fixture proposals. Authoritative stats and schema: SO design §3 (Git history: `2026-10-14-boss-unit-so-design.md#3-boss-asset-and-prerequisite-decisions`).

| Item | Current state |
|---|---|
| Boss identity | `EntityKey.Unit_Stage1Boss = 18`; `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_Stage1Boss.asset` created, unregistered |
| Summon | Existing `Unit_Gravekeeper = 17`, cap 2, first-action delay 7 timeline units; Gravekeeper asset unchanged and registered |
| Roar | Threshold 5; distance 3 belongs to the existing Knockback Trait, not a separate Roar distance field |
| Targeting | Key 5, `TargetingMethod_Stage1BossRoar.asset`, Fan/All/None/0; created, unregistered. Existing method 4 remains unchanged |
| Behavior | SO/Data exports `(false, false, true)` for voluntary movement/basic attack/forced-movement immunity; forwarded to UnitSpec |
| Trait data | NecromancyTraitData/RoarTraitData and SO authoring classes exist; adapter maps them to existing Core Specs |
| Boss category | Existing CategoryKey.Boss 50; not a signal to infer target or behavior |
| Stage objective/target | Phase 5 SO/DTO fields and Phase 6 forwarding/actual target-instance binding verified |
| Actual boss stage | Still empty fixed enemies + EnemyLoadout_5 + headquarters. Proposed replacement needs confirmed lineup/placement and View readiness before Phase 7 |

Fan still excludes exact diagonal cells. No shape/skill algorithm changed. Supporting enemies, position, and facing are Stage SO entries; neither the adapter nor View may hardcode the Stage 1 layout. Fixed/generated mixing remains rejected. Future all-bosses victory or random target designation would require a separate gameplay design.

The earlier deferred balance note remains: summons currently grant the ordinary enemy reward (G12); planned enemy scaling over successive nights may limit repeated farming. No such balancing change is part of this integration.

## 3. Core assembly status and remaining work

1. **Done (Phase 3):** `ReadUnit` maps NecromancyTraitData/RoarTraitData to existing NecromancyTraitSpec/RoarTraitSpec.
2. **Done (Phase 3):** forwards immutable `UnitData.Behavior` to UnitSpec.
3. **Done (Phase 4):** expands roster/owned/placement roots through summon references, once per key, including transitive dependencies; sorted definitions feed the existing source/profile pipeline. No initial spawn or ownership is created by collecting definitions.
4. **Phases 5-6 done:** Stage SO exports `ObjectiveKind` and per-placement `IsVictoryTarget`; the adapter forwards both to existing Core contracts, and preparation binds the marked instance. Actual SO/default-source tests verify same-key/reordered placements, target-specific outcomes, and failure publication boundaries. Do not infer a target from its key/category.
5. **Already available:** Boss CategorySpec and existing objective/outcome/Buffer contracts. No new combat implementation is required for these existing mechanics.
6. **View-owned activation (Phase 7):** View authors the boss stage and SOs, registers Unit/Targeting/UnitVisual assets, and verifies the real DB/gameplay path. Record content choices using the design proposal as a starting point. No Core permission is required for content work; consult Core only for changes inside InGame CoreSystem code.

The user is the Core programmer and approved the generic key-validation repair and threshold-publication follow-up. Core integration is complete for this scope. Remaining Phase 7 is View-owned content configuration and gameplay verification, using the published Buffer contracts.

## 4. 계약 변경 요약(다른 Core 작업자용)

- 공개 계약 확장: `AttackResult.FromFacts(..., costFree = false)`. 비용 없는 반응 공격(포효)만 AP 지출 0을 허용한다. 기존 호출은 그대로.
- `BattleOutcomeCalculator.Resolve`는 목표 인자를 받는 3인자만 남았다(2인자 삭제).
- `CombatBattleFacts`·`CombatStateData`·`BattlePreparation`에서 `NextEntityId`를 지웠다. 원본은 `CombatCommandState.Allocation.NextEntityId` 하나다.
- `CombatCommandCalculator.Calculate`/`Advance`(공개)는 끝에 대기 포효 반응을 붙인다. 미리보기(`Preview`)는 붙이지 않는다. 포효 Trait가 정의에 없는 전투는 아무것도 하지 않는다.
- `KillChainAttackCalculator.Resolve`에 공격자 인자(`actorId`)가 생겼다(internal).
- 피격 원인 `CombatHitCause`와 경로 진행기 `ForcedDisplacementPath`는 Application internal이며 Contracts에 공개하지 않았다.

## 5. 검증 상태

- 신규·수정 테스트: Core(`ProjectL.Tests.Core`)와 Framework(`ProjectL.Tests.Framework`) EditMode. 통합: `BossCombatIntegrationTests`(실제 `CombatProcessingService` + Buffer로 준비 → 진입 → 보스 소환 → 소환수 턴 → 밤 종료 대기 → 같은 개체 복귀 → 보스 사망 승리·완료 1회 → 정리).
- 통합 테스트에서 챕터 진행 판정만 대역(probe)이다. 스테이지 진행·운영 DB·View·PlayMode는 검증하지 않았다.
- 전체 실행 결과와 사전 실패(58개, 이번 작업과 무관) 비교는 계획 Phase 6 실행 기록에 있다.

## 6. 범위 밖으로 남긴 것

- 포효 범위 도형(대각선) 변경.
- View-owned operational activation (§2-3); Unit SO and generic stage/Core integration are complete.
- View 표시(View 인계 문서).
- 작업 트리의 `Assets/ProjectL/Script/Render/Effect/MuzzleFlash/`, `Assets/ProjectL/Script/Tool/Editor/Vfx/`, `Assets/Scenes/EffectTest.unity`는 이 작업의 변경이 아니다(다른 작업자 파일로 보임).

### Earlier Phase 5 automated evidence — 2026-10-14

Phase 5 Unity EditMode selection: 161 passed, 1 failed, 162 total, 0 skipped, exit 2; new BossStageAuthoringTests 20/20 and adapter fixture 68/68. The single Stage 1101 terrain assertion (index 5 expected (7, 3), actual (6, 3)) exactly matches the fresh 141/142 baseline. Existing CS0105 warning remains; no compiler errors. Evidence: Stage plan execution record (Git history: `2026-10-14-boss-stage-objective-plan.md#execution-record`). Earlier Phase 4 selection was 159/160 with a different fixture selection; SO/Core record (Git history: `2026-10-14-boss-unit-so-plan.md#execution-record`) retains that evidence. The older 58-failure reference is historical. Phase 6/7 and real boss-stage gameplay are not verified by Phase 5.
