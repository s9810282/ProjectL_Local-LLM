# Pillar and Moving Platform Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task after the user authorizes implementation. Steps use checkbox (`- [ ]`) syntax for tracking. Stop and report at every phase boundary. Codex must not commit or push.

**Goal:** Implement hit-count obstacles, crushing/terrain-changing pillars, and sequential moving platforms with authoritative previews and movement playback before controller-turn start.

**Architecture:** Extend the existing structure and environment facts, calculation projections, Buffer validation and preview services. Pillar reactions and platform motion have focused calculators, share the effective surface contract, and publish through existing result sequences. View owns animation and acknowledges it before Core begins the next turn.

**Tech Stack:** Unity 6000.0.73f1, C#, existing ProjectL.Core.Contracts/Domain/Application and ProjectL.Systems assemblies, existing NUnit EditMode/CoreIntegration fixtures. No new package dependencies.

**Spec:** [Pillar/platform design](../specs/2026-11-16-pillar-platform-design.md); [confirmed gameplay decisions](../specs/2026-11-16-pillar-platform-decisions.md).

## Global Constraints

- Phases 0–6 Core implementation and selected verification are recorded below. Production content approval and View acceptance are outstanding.
- Documents are English. New code comments/XML documentation are Korean.
- Codex must not commit or push.
- Finish and report each phase separately; do not automatically execute the next phase.
- The user is the Core programmer and authorized Phase 3–4 Core, contract, Buffer, and verification work. Phase 3 was reported and stopped; the user's subsequent instruction authorized Phase 4. Stop and report after Phase 4. Phase 5 onward, View, Architecture, and FieldCamera are outside this continuation's scope.
- No View implementation, new View tests, or combat View MSBuild. View implementation and visual checks are later handoff work for the View worker; Core verification remains this plan's responsibility when implementation is authorized.
- Preserve existing paths, namespace spelling, assembly ownership, `.meta`/GUID identity and Framework/Buffer/Job contracts.
- Preserve pre-existing working-tree changes. Do not reset, stage, reformat, or silently absorb them.
- New tests use test-owned definitions and inputs; mutable production assets are not rule-test fixtures.
- Preserve regression intent. Determine authorship/ownership before semantic changes to existing tests; if another worker's test requires a behavioral rewrite, obtain the required permission and record the conflict rather than masking a failure.
- Unity batch execution must use an AppData-access-approved path. Do not delete caches or upgrade Unity to work around sandbox denial.

## Ownership and Completion Boundaries

| Owner | Work in this plan | Completion evidence |
| --- | --- | --- |
| Core programmer / this implementation plan | Rule calculation, SO pure-data conversion, command-local state, Buffer publication/evidence, typed read/forecast outputs, required Core regression and later handoff document | Core calculation/publication/authoring test results and clear API/event examples. |
| Existing LifeCycle/Job orchestration | Preserve its result-admission/presentation gate; inspect its call contract | Record the existing gate and a targeted handoff issue if it needs owner changes. Do not implement a competing Core gate. |
| View worker after handoff | Animation, debris/platform appearance, hit-count display, direction/path arrows, presentation completion and visual/scene checks | View-owned implementation and acceptance, tracked separately from Core completion. |

Core phases must not acquire View implementation tasks to claim end-to-end completion. Conversely, later View work does not invalidate completed Core evidence. Overall production activation remains pending until both sides and any reviewed asset migration are ready.

## Review Focus

1. A zero-damage landed hit must consume exactly one count, whereas a non-hit reporting placeholder must not (Phase 1).
2. Cyclic barrel/pillar overlap must resolve each destroyed obstacle once, retain source attribution, and suppress supply buffs throughout a crush-origin chain (Phase 3).
3. All riders and the platform must move together before cell-entry effects, including self-overlap of old/new footprints and a rider thrown off by a tornado (Phase 4).
4. An unpresented platform result must prevent turn-start effects/action refresh and AI/input admission; initial-player and completely blocked cases must not replay indefinitely (Phase 4 Core boundary assertions; Phase 6 hands the presentation-gate acceptance case to the View/flow owner).
5. Equal-size random ordering and state changes must not let forecast mutate RNG or present a stale/conditional path as exact (Phase 5).

## Path Map and Responsibility

Use the exact `C`, `S`, `B`, `D`, `L`, and read-only `V` path roots defined in design section 2. Additionally:

- `T = Assets/Tests/EditMode`
- `I = Assets/Tests/EditMode/CoreIntegration`
- `E = Assets/ProjectL/Script/Systems/DataBaseSystem/Editor`
- `Tool = Assets/ProjectL/Script/Tool/Editor`

New files listed below are proposed files, not existing APIs. Create their `.meta` files when implementing, not during document preparation.

File lists identify contract owners and impact candidates, not a requirement to edit every listed file. For each task, verify the actual caller first; retain an already-correct adapter and record that it needed inspection/regression only. Move rules or share existing collaborators only where the design's change scenario demonstrates a benefit. Do not bulk-reformat feature neighbors or introduce interfaces merely to satisfy a file-count target.

| New file | Responsibility | Phase |
| --- | --- | --- |
| `C/Contracts/Combat/ObstacleDurabilitySnapshot.cs` | Immutable authoritative obstacle hit counts. | 1 |
| `C/Contracts/Combat/ObstacleDurabilityEvents.cs` | Typed durability/destruction cause and count-change evidence. | 1 |
| `C/Contracts/Combat/ObstacleDurabilityEvidenceValidator.cs` | Count conservation, one-destruction and compatibility-liveness validation. | 1 |
| `C/Application/Combat/ObstacleCommandCalculator.cs` | Public pure barrel-detonation calculation entry. | 1 |
| `C/Contracts/Commands/MovingPlatformSpec.cs` | Immutable route, initial placement and shape input. | 2 |
| `C/Contracts/Combat/MovingPlatformFact.cs` | Single current anchor/sign and derived footprint. | 2 |
| `C/Domain/Movement/BoardSurfaceRules.cs` | Shared supported-cliff and platform-route surface interpretation. | 2 |
| `C/Contracts/Combat/PillarEvents.cs` | Pillar impact and terrain-change evidence. | 3 |
| `C/Application/Combat/PillarFallCalculator.cs` | Eight-way footprint and pillar impact execution. | 3 |
| `C/Contracts/Combat/PillarTerrainEvidenceValidator.cs` | Allowed wall creation/fill successors and source-ID evidence. | 3 |
| `C/Contracts/Combat/PlatformMotionEvents.cs` | Motion steps, turns, passenger steps and boundary evidence. | 4 |
| `C/Application/Combat/PlatformMotionCalculator.cs` | Ordered pre-turn batch, collisions and transport. | 4 |
| `C/Contracts/Combat/PlatformMotionEvidenceValidator.cs` | State/step/turn-boundary conservation without recomputing gameplay. | 4 |
| `C/Contracts/Results/PillarPlatformForecasts.cs` | Typed pillar and platform forecast payloads/stamps. | 5 |
| `D/Stage/Combat/MovingPlatformAuthoring.cs` | Inspector fields to immutable platform specs. | 6 |

Do not split existing barrel/supply logic merely to obtain one class per kind. The reaction coordinator can retain its explicit kind dispatch; pillar-specific geometry/terrain is separate because it changes independently.

## Phase Summary and Review Gates

| Phase | Deliverable | Depends on | Stop condition |
| --- | --- | --- | --- |
| 0 | Current baseline, ownership and design choices recorded | Document approval | Report any drift/authorization limits before code |
| 1 | Hit durability and explicit barrel detonation | 0 | Focused Core/publication evidence reported |
| 2 | Platform state and common effective-surface rules | 0; executed after 1 in the agreed sequence | Stationary support/boarding/fall parity reported |
| 3 | Pillar destruction, crushing and terrain changes | 1, 2 | Chain/publication/terrain evidence reported |
| 4 | Sequential pre-turn platform motion and result boundary | 2; 3 for combined pillar/blocker regression | Atomic transport and two-result turn-start contract reported |
| 5 | Pillar/platform forecasts and View data contract | 3, 4 | Preview/execution and stale/RNG evidence reported |
| 6 | SO integration, reviewed migration, Core regression and handoff | 1–5 | Core delivery status; later View-owned acceptance separately listed |

This is one coordinated feature plan because generated terrain changes platform routes and both depend on shared support/publication contracts. Execution remains phase-by-phase; the table distinguishes real data/contract dependencies from that chosen execution order. Stationary platform support does not need to understand barrel hit processing. Phases are reviewable increments, not claims that production content or later View implementation is ready.

## Phase 0 — Approval and Baseline

- [x] Read the design, confirmed decisions, and feature-change locality guidelines. Record the user's authorized scope and execution method.
- [x] Capture the branch, HEAD, working tree, applicable AGENTS instructions, and concurrent test work. Recheck concrete source paths.
- [x] Inspect test provenance and the approved test execution route in the existing workspace.
- [x] Run the current Core and relevant CoreIntegration baseline and record the results.
- [x] Verify and record the installed Unity executable path.
- [x] Record the baseline at the Phase 0 review point.

### Phase 0 execution record — 2026-11-16

- Ran Phase 0 inline on `feature/Cheon/Core/NewCombatSytem` at `d84e05bf959509197c8b18a4c69e7c6fc3691b41`, even with the local origin-tracking ref. The specified checkout was the only registered worktree. The other recent Codex chats for this workspace, including test cleanup, were idle or unloaded. No Unity Editor or project lock was present before the run.
- Read the root and Assets AGENTS files, the Framework architecture and programmer guide (FW-001–004, FW-012–013, FW-016), and the combat View handoff section 4. The latest commit since the design snapshot `4730de720` changed test inputs and the ShotGun asset. Owning production C# paths were unchanged. All 54 named existing source/test files were found. The 20 absent files are planned additions. The simulator referenced for Phase 3 was not found in the repository search.
- Starting changes were the FMOD catalog, SCDream7 font asset, this plan, and both pillar/platform specs. The test tree and index had no pending changes. Unity rewrote the FMOD catalog's scan path. Restored its exact pre-run bytes and verified SHA-256 for both assets and all three documents before updating this plan. The recent history of `ObstacleReactionTests`, `PowderBlossomSkillTests`, `PowderBlossomParityTests`, and `PullObstacleTests` identifies Core authorship. Integration provenance must still be checked before any semantic test edit.
- Used the AppData-approved Unity batch route with `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe` (revision `a166abc3bf0e`), `-batchmode -nographics -runTests -testPlatform EditMode`, and the assembly filters below. Both runs exited 0. Their logs contained no compilation error or recurrence of the CurlRequestCache.db or TypeLoadException failures. The CoreIntegration fixture filter is recorded in the Unity log.

| Scope | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full default `ProjectL.Tests.Core` | 5,896 | 0 | 11 | `core.xml`, `core.log`. The 11 cases are opt-in diagnostic captures. |
| `ProjectL.Tests.CoreIntegration`, 15 selected Core fixtures | 132 | 0 | 0 | `core-integration.xml`, `core-integration.log`. Includes preparation, command/publisher, movement/effects/undo, deployment, pull, swap, tornado, skill/preview, and AI utility. |

Evidence directory is `C:/Temp/ProjectL-pillar-platform-phase0-d84e05bf-01a1e0d2`. Non-selected integration fixtures, optional Core captures, and View/PlayMode acceptance remain unverified in this run.

- Phase 1 source check confirmed numeric obstacle HP writes, independent attack/arrival reaction calculators, and replacement at `RefreshObstacles`. The migration must account for numeric consumers in `CommandAttackLedger`, `CombatActionAssessment` and AI/finishing-shot paths, and preserve durability on copies in `AttackKnockbackCalculator` and `CombatStageEnvironmentAuthoringValidation` wherever they construct obstacle facts. Verify delivered contact, ordered reactions, per-hit summaries, general structure HP, and atomic Buffer evidence together. The native barrel and supplies templates have old HP values 15 and 10. No serialized references to their GUIDs were found in repository assets. Use explicit test-owned hit counts and keep the template values pending reviewed authoring in Phase 6.
- Phase 2 source check confirmed that `EnvironmentTerrainRules.BlocksVoluntaryEntry` already serves placement, spawn, summoning, undo, and trails. Route search and AI engagement also build masks from raw cliff cells. `CliffFallCalculator` and initial-placement validation read raw cliffs independently. Those paths need effective support, and cell inspection must cover derived footprint cells beyond the anchor. Verify movement/fall/placement agreement and stale movement contexts after anchor changes. The prelude boundary counter belongs with Phase 4 processing.
- View handoff must cover numeric HP reads in `FieldUnitPresenter`, `CombatStructureHitFeedback`, and `CombatVolleyDamageNumbers`. The existing check of `HasUnpresentedResult` in `CombatPlan.Decide` is the later admission boundary for movement playback and turn start. Phase 0 made no gameplay/API/ownership changes and found no decision that blocks Phase 1.

## Phase 1 — Obstacle Hit Durability and Barrel Operation

### Task 1.1 — One authoritative durability representation

**Create:** the three Phase 1 durability contract/validator files; `T/ObstacleHitDurabilityTests.cs`, `I/ObstacleDurabilityPublicationTests.cs`.

**Modify:** `C/Contracts/Combat/StructureCombatFacts.cs`, `ObstacleContentProfile.cs`, `InitialObstaclePlacementSpec.cs`, `CombatEvent.cs`, `CombatHealthEvidenceValidator.cs`, `CombatChangeEvidenceReader.cs`; `C/Application/Combat/BattlePreparationCalculator.cs`, `BattleEnvironmentPreparation.cs`, `CommandAttackProjection.cs`, `MovementProjection.cs`; `B/Combat/CombatStateBuffer.cs`.

**Interfaces:**

- Produce `ObstacleDurabilitySnapshot(int remainingHits, int requiredHits)`, read-only `RemainingHits`, `RequiredHits`, `IsDestroyed`.
- Produce `StructureCombatFacts.ObstacleDurability` and `IsDestroyed`; existing `Health` is derived `(1,1)/(0,1)` for obstacles only. Add an explicit obstacle construction/copy entry `WithObstacleDurability(ObstacleDurabilitySnapshot durability)`; never rebuild hit state from `Health`.
- Produce `ObstacleDurabilityEvidenceValidator.Validate(CombatCommandTransition transition)` and typed count-change evidence with expected/final snapshots, physical source, reaction cause and context.
- Consume existing obstacle IDs/profiles and existing publisher/health evidence. Do not add a second HP/count store.

- [x] Add `OneHitConsumesOneRegardlessOfDamage`, `AkimboConsumesTwoDeliveredHits`, `InvalidCountsRejectBeforeMutation`, `CopyAndCompositionPreserveDurability`, and `OrdinaryStructureStillUsesHp` tests. Assertions include remaining hits `3 -> 2 -> 1`, max unchanged, and ordinary HP `20 -> 13` for numeric damage 7.
- [x] Add publication tests rejecting missing/reordered/forged hit evidence, changes to required-hit capacity, and mismatched alive/destroyed compatibility projections; valid counts publish atomically and replay fails stale-state validation.
- [x] Inventory concrete Core and external consumers of structure `Health`, separating liveness checks, numeric scoring/receipts, and display. Record the old/new obstacle contract and general-structure preservation. Existing View numeric readers are handoff items only; do not claim `(1,1)` gives them numeric backward compatibility or edit those files here.
- [x] Verify the pre-change high-damage hit reproduces the old obstacle HP behavior.
- [x] Implement contracts and factory/copy updates; route obstacle validation outside numeric HP accounting without weakening character/general-structure checks. Search every `new StructureCombatFacts` and structure HP consumer for loss of the new discriminator.
- [x] Run focused tests; verify `Health` remains a derived compatibility view and obstacles do not contribute fake 1-HP/count damage to receipts/statistics. Do not rewrite unrelated tests to accept weaker evidence.

### Task 1.2 — Landed hits and immediate barrel chains

**Create:** `C/Application/Combat/ObstacleCommandCalculator.cs`.

**Modify:** `C/Application/Combat/DeliveredAttackHit.cs`, `AttackDirectDamageCalculator.cs`, `ObstacleReactionCalculator.cs`, `CommandAttackLedger.cs`, `MovementProjection.cs`, `CombatActionAssessment.cs`; relevant fixture callers in `T/ObstacleReactionTests.cs`, `PowderBlossomSkillTests.cs`, `PowderBlossomParityTests.cs`, `PullObstacleTests.cs` only after provenance review.

**Interfaces:**

- `ObstacleReactionCalculator.ApplyHit(EntityId id, CombatDamageOrigin origin)` consumes one landed hit.
- `DestroyObstacle(EntityId id, CombatDamageOrigin origin, ObstacleDestructionCause cause)` marks destroyed and queues one consequence.
- `ObstacleCommandCalculator.DetonatePowderBarrel(CombatCommandInput input, EntityId barrelId, CombatDamageOrigin origin, IReadOnlyList<IMovementTileRule> rules)` returns a normal transition.
- Explicit hit delivery distinguishes a landed zero-damage hit from an unlanded/reporting-only placeholder. Preserve direct actor position separately from splash effect origins.

- [x] Add `ZeroDamageLandedHitStillCounts`, `ZeroPercentNonHitDoesNotCount`, `BarrelExplosionHitsObstacleOnce`, `BarrelExplosionDetonatesBarrelWithRemainingHits`, `ExplicitDetonationRejectsNonBarrel`, and `RepeatedDetonationDoesNotRepeatEffects`. Include multiple occupied cells touching one explosion but only one decrement.
- [x] Record focused regressions and verify typed obstacle hit evidence. Keep ordinary structures on numeric damage and finishing-shot/instant-damage modifiers outside obstacle durability.
- [x] Refactor the existing explosion queue into the minimal shared iterative consequence queue needed for later pillar reactions, preserving delivered-hit and reaction ordering. Follow design 5.4: one command-local `CommandEnvironmentState` and reaction calculator are passed explicitly through the existing attack/arrival constructors. Independent commands/previews create independent instances. Do not add a context/service layer, general event bus or strategy registry.
- [x] Replace `RefreshObstacles` instance replacement with a documented drained-queue hit-scope refresh. Preserve command-wide destruction identity/current environment; reset only per-hit summaries after ledger consumption. Test two attack hit scopes with an intervening arrival reaction: no repeated consequence, no lost queue item, no stale terrain read and no duplicated summary damage.
- [x] Retain ordinary supply hit/destruction rewards for ordinary qualifying hits; carry an explicit destruction cause for later crush suppression. Future non-hit status ticks do not consume obstacle hits by inference.
- [x] Migrate non-attack paths in `MovementProjection.ChangeHealthSteps` and `ApplyFall`: delivered environmental contacts consume one count, numeric healing does not repair obstacles, and `RemoveFallenObstacle(EntityId id, CombatFallEvent cause)` records zero remaining hits with no destruction consequences. Test `EnvironmentalContactConsumesOne`, `HealingDoesNotRestoreHits`, and `FallingObstacleEmitsNoExplosionOrSupplyReward` with no fabricated direction origin; ordinary structure healing remains unchanged.
- [x] Verify focused and dependent barrel/obstacle/attack evidence tests. Report Phase 1 results and stop.

### Phase 1 execution record — 2026-11-16

- The immutable durability snapshot is the authoritative obstacle value in `StructureCombatFacts`. `Health` exposes only obstacle liveness as 1/1 or 0/1. Ordinary structure HP remains numeric. Battle preparation and authored placement construct explicit durability, knockback and movement context preserve it, and state equality includes remaining and required hits.
- `DeliveredAttackHit` supplies the contact decision. `AttackDirectDamageCalculator` sends landed obstacle contacts to `ObstacleReactionCalculator` and retains the existing HP path for ordinary structures. `AttackHitResult.IsObstacle` identifies liveness-only receipt values. Numeric damage, health executions, finishing-shot checks, and AI HP scoring exclude obstacle counts. Existing health evidence still validates ordinary structures and characters.
- Attack and arrival share the command-local environment and reaction instance. A barrel's destroyed fact is installed before the FIFO enqueues its blast. Each blast observes current terrain and target liveness. Direct hit ledgers consume their own blast totals, and movement consumes the blast target and terrain dependencies for its own effect. Each consumer then clears those summaries. The two-scope regression interleaves an arrival blast and a terrain-source change and checks both damage totals and exactly-once reactions.
- `ObstacleCommandCalculator.DetonatePowderBarrel` returns a normal existing-publisher transition. The inputs prohibit the reserved `None` value, so the facade uses `DetonatePowderBarrel = 17` for result classification with the original battle and command stamps. The View command router has no mapping for it. `ObstacleDurabilityChanged = 52` publishes ordered expected/final durability, source, and reason. The existing evidence reader and Buffer publication reject missing, reordered, forged, resized, numeric-only, and stale state in the covered cases.
- Authoring runtime conversion requires an explicit positive `requiredHits` field. Omission fails validation, which prevents old unreviewed template HP values from turning into hits. The two existing template assets and their GUIDs remain untouched. Production count review and activation remain in Phase 6.
- The pre-change RED run reproduced a one-hit destruction under the HP rules. During implementation, the focused tests exposed the reserved command kind, and the first full Core run exposed the new receipt fields in frozen output comparisons and remaining HP fixtures. The existing serializer now omits default obstacle-only fields for ordinary targets. The candidate capture matched 85 of 86 old preview vectors byte for byte after decompression. Only the deliberately changed barrel-cascade vector was updated. The first broad integration run exposed an incorrect single-structure assumption in the new test fixture, which also contains a headquarters. The corrected proof tests retain that structure and verify both the existing duplicate-number no-op and the rejection of an older snapshot under a new number.
- View handoff includes `FieldUnitPresenter`, `CombatStructureHitFeedback`, and `CombatVolleyDamageNumbers`. They need typed durability reads and event 52 for obstacle hit/destruction presentation. Ordinary HP presentation still consumes the existing HP contracts. View playback and non-selected integration fixtures remain unverified. No commits or pushes were made.

| Unity EditMode scope | Passed | Failed | Skipped | Result file |
| --- | ---: | ---: | ---: | --- |
| Full default Core | 5,909 | 0 | 11 | `core-verified.xml` |
| CoreIntegration, 21 selected fixtures | 170 | 0 | 0 | `integration-verified-final.xml` |

The final Core run includes 12 obstacle durability cases. CoreIntegration includes all 7 new durability publication and authoring cases. The 11 skips are existing opt-in Core diagnostic captures. Results and logs are in `C:/Temp/ProjectL-pillar-platform-phase1-01a1e0d2`. The editor was Unity 6000.0.73f1 at the approved path recorded in Phase 0. No baseline environment failure reappeared in these runs.

## Phase 2 — Platform Facts and Shared Surface Semantics

### Task 2.1 — Immutable platform/support contracts

**Create:** the Phase 2 runtime files in the path map; `T/PlatformTestData.cs`, `T/PlatformSurfaceTests.cs`, `I/PillarPlatformPublicationTests.cs`.

**Modify:** `C/Contracts/Combat/StaticTerrainFact.cs`, `EnvironmentSemanticBindings.cs`, `MovementEnvironmentFacts.cs`; `C/Application/Combat/EnvironmentContentSupport.cs`, `CellEnvironmentQuery.cs`, `BattleEnvironmentPreparation.cs`, `BattlePreparationCalculator.cs`; `B/Combat/BattlePreparationValidator.cs`.

**Interfaces:** `MovingPlatformSpec` and `MovingPlatformFact` from design section 5.2; `BoardSurfaceRules.HasPlatformSupport`, `IsUnsupportedCliff`, `IsPlatformRouteSurface` from section 5.3. Platform state is held in `Environment.Tiles`; current footprint derives from `Cell`, shape axis and length.

- [x] Test lengths 1/2/3, independent shape/travel axes, endpoint/initial-anchor validation, duplicate source IDs, initially overlapping footprints, and visibility/equality copying. Shared routes with separate initial footprints must succeed.
- [x] Test that `(cliff + platform)` supports a unit, raw cliff does not, and `FilledCliff` supports a unit but rejects platform travel. A horizontal three-cell shape moving vertically must test all three destination cells.
- [x] Observe focused failures; implement the immutable contracts and precise allowed overlap in preparation. Do not globally allow overlapping exclusive terrain.
- [x] Verify footprint is derived, terrain bindings contain the same source IDs, and no passenger/seat state is independently stored.

### Task 2.2 — Route existing consumers through effective support

**Owning rule edits:** `C/Domain/Movement/EnvironmentTerrainRules.cs`, `C/Application/Combat/CliffFallCalculator.cs`, and the new `BoardSurfaceRules`.

**Inspect consumers; modify only when an existing adapter does not already reach effective support:** `C/Domain/Movement/MovementRouteValidator.cs`; `C/Application/Combat/CombatPlacementCalculator.cs`, `EnemySpawnCalculator.cs`, `MovementContextCalculator.cs`, `MovementEffectUndo.cs`, `TraitTrailCalculator.cs`, `Summoning/CombatSummonCalculator.cs`, `EnemyAi/EnemyEngagementField.cs`. Retain raw-cliff reads for authored underlying terrain. Record why each changed consumer requires effective support rather than assuming every raw terrain read is wrong.

- [x] Extend `PlatformSurfaceTests`: ordinary boarding into a vacant cell, disembarking onto valid floor, occupied cell rejection, no voluntary move into unsupported cliff, forced displacement off the platform causing normal fall, and knockback immunity not granting immunity to loss of support.
- [x] Verify affected placement/spawn/AI move previews agree on support while retaining their separate eligibility rules. Test stale movement context after changing a platform position in a test-owned projected state.
- [x] Implement support queries and integrate each required consumer; search raw `StaticTerrainKind.Cliff`/terrain-kind scans to classify rather than blanket-replace them.
- [x] Run wall/cliff/weak-ground, movement/undo, placement and focused platform tests. Publish/prepare a stationary platform fixture through the real Buffer to prove validation supports it.
- [x] Report Phase 2 changes, remaining caller exceptions and evidence; stop.

### Phase 2 execution record — 2026-11-16

- `MovingPlatformSpec` defines IDs, length, positive-axis shape, route and initial state. `MovingPlatformFact` holds the current anchor and sign in `MovementEnvironmentFacts.Tiles`. The footprint is derived when queried. The existing environment validates unique IDs and the same terrain bindings classify the platform. The Character Buffer owns unit positions, which also determine who stands on a platform.
- `BoardSurfaceRules` supplies the common effective cliff check and whole-footprint test. `EnvironmentTerrainRules`, `MovementRouteValidator`, and `EnemyEngagementField` use it for voluntary entry and indexed route/engagement masks. The mask keeps the existing wall and cliff work steps and collects platform support while scanning walls. `CliffFallCalculator` uses the same support check and preserves the original cliff source ID in fall evidence. `FilledCliff` is walkable and is excluded from platform routes.
- `CellEnvironmentQuery`, `EnvironmentContentSupport`, `BattleEnvironmentPreparation`, and `BattlePreparationValidator` admit only the supported cliff overlap for initial platform footprints. They check both route endpoints, overlapping footprints, terrain IDs and public state, and reject initial bosses and structures on platforms. The spatial query exposes a platform on each footprint cell. Existing `BattleInitialization` and `MovementEnvironmentFacts` already pass and store the required facts.
- `EnemySpawnCalculator` admits supported cells and preserves the weighted picks and shuffle. If a drawn boss is assigned a platform cell, it exchanges that cell with the first available non-platform slot assigned to a normal unit or left unused. An unavailable slot fails explicitly. Placement, enemy redeployment, summoning, Rockfall candidate selection, trails, and undo legality already call `BlocksVoluntaryEntry`. Nexus reinforcements deliberately exclude all active terrain/effect cells. The cliff beneath each stationary footprint satisfies that existing restriction. Authored terrain conversion and weak-ground collapse retain the raw cliff kind.
- `MovementContextCalculator` and `MovementUndoOriginFacts` capture the typed platform across relevant footprint cells, including non-anchor cells. Two still-legal routes can have different source stamps when the platform anchor changes in a projected test input. `CombatStateBuffer` validates the complete stationary platform facts before publishing and rejects state edits and generic source events that attempt to change them. The existing ground validation also conserves filled cliffs.
- A support-rule change now follows `BoardSurfaceRules` into voluntary admission and route masks, forced arrival and fall, and initial layout. Geometry changes follow the immutable spec/fact, spatial query and both preparation boundaries. Publication and stale-input tests verify these connections. The existing Framework exports, command entry points, and jobs handle dispatch and scheduling.
- The pre-change RED case reproduced rejection of the filled-cliff discriminator. The first implementation run caught a missing LINQ import in the modified fall calculator. The new fixtures initially lacked required placement context, used an invalid boss move definition, attempted two moves for the same action use, and mixed invalid terrain publication with movement-undo proof. The corrected fixtures supply real context and isolate each proof. Phase 2 added test-owned fixtures for the new behavior.
- **View handoff:** Read the current `MovingPlatformFact` (`Id`, `Spec`, `Cell`, `TravelSign`, and `OccupiedCells`) from the Combat Buffer and `FilledCliff` as a distinct terrain kind. `EnvironmentVisualKinds` and `TerrainViewSet` need a mapping for these sources before View activation. Phases 3–6 own pillar impact and terrain generation, automatic platform motion, the forecast service, stage authoring, and playback acknowledgment before controller-turn start. The Phase 1 obstacle HP display handoff is still pending.

| Unity EditMode scope | Passed | Failed | Skipped | Result file |
| --- | ---: | ---: | ---: | --- |
| Full default Core, including 21 platform-surface cases | 5,930 | 0 | 11 | `core-final.xml` |
| CoreIntegration, 28 selected fixtures, including 7 platform publication cases | 288 | 0 | 0 | `integration-final.xml` |

Evidence and runner logs are in `C:/Temp/ProjectL-pillar-platform-phase2-01a1e0d2`. Both final runs used the AppData-approved Unity 6000.0.73f1 batch route. The 11 skipped tests are the same existing opt-in Core diagnostic captures. The selected integration fixtures cover command/publication, movement/undo, deployment, enemy preparation/decision, Nexus reinforcement, and affected gimmicks. The prior CurlRequestCache and Cinemachine errors did not recur. View playback, PlayMode, View MSBuild, and integration fixtures outside the selected scope remain unverified. The checkpoint ends at Phase 2.

Unity rewrote the FMOD scan path during the batch runs. Restored that file to its pre-run bytes and verified the original SHA-256 values for both the FMOD catalog and the existing font asset modification.

## Phase 3 — Pillar Impact, Guaranteed Crushing, and Terrain Publication

### Task 3.1 — Direction and exactly-once destruction chains

**Create:** Phase 3 runtime files; `T/PillarTestData.cs`, `T/PillarResolutionTests.cs`.

**Modify:** `C/Contracts/Ids/EntityKey.cs`, `C/Contracts/Combat/ObstacleContentProfile.cs`, `InitialObstaclePlacementSpec.cs`, `CombatEvent.cs`; `C/Application/Combat/ObstacleReactionCalculator.cs`, `CombatDamageApplication.cs`, `CommandDeathSettlement.cs`, `CommandAttackProjection.cs`, `MovementProjection.cs`.

**Interfaces:**

- `PillarFallCalculator.GetCells(CellCoord pillar, CellCoord source, int length, int boardWidth, int boardHeight)` returns ordered cells excluding the original.
- `CombatDamageApplication.CrushSteps(EntityId recipientId, CombatDamageOrigin origin, CombatEvent contextEvent)` uses normal death/equipment/credit settlement while bypassing Reconstruction and numeric protection.
- `CombatPillarFallEvent` records source, pillar, impact and affected entities; queued children retain direct source and originating reward-suppression context.

- [x] Add `EightDirectionMatchesHtmlCoordinates`, `LengthThreeExcludesOriginalCell`, `BoardEdgeClipsWithoutExtending`, `WallDoesNotTruncateImpact`, `DirectHitUsesDestroyingActor`, and `IndirectHitUsesEffectSource`. Include off-axis `(4,2)` and `(4,1)` vectors, all sign quadrants, length 1 and >3, and a missing/zero direction rejection.
- [x] Add `CrushBypassesReconstructionAndProtection`, `PlatformPassengerDiesButPlatformSurvives`, `PillarBarrelPillarCycleResolvesOnce`, `CrushChainDoesNotGrantSupplyBuff`, and `DestroyedSourceStillProvidesOrigin`. Validate death/credit/item cleanup and no duplicate terrain/death events.
- [x] Record expected failures; add explicit pillar kind/keys and the focused geometry/impact calculation. Do not invoke ordinary `DefeatSteps` as the crushing implementation.
- [x] Drain queued effects iteratively in documented order. Preserve directly landed hit origins separately from any changed credited actor or splash center. Ensure surviving non-Obstacle structures follow the reviewed edge default and cannot become illegally overlapped by new walls.
- [x] Verify phase-local geometry/death/chain tests and existing ordinary Reconstruction/instant-defeat tests remain unchanged in meaning.

### Task 3.2 — Mutable terrain with strict publication evidence

**Modify:** `C/Application/Combat/CommandEnvironmentState.cs`, `MovementProjection.cs`, `CommandAttackProjection.cs`; `C/Contracts/Combat/StaticTerrainFact.cs`, `CombatChangeEvidenceReader.cs`, `CombatCommandTransition.cs`; `B/Combat/CombatStateBuffer.cs`. Extend `I/PillarPlatformPublicationTests.cs`.

**Interfaces:** `CommandEnvironmentState.ApplyPillarTerrainChanges(...)` accepts expected current facts and typed impact-backed successors/additions; `PillarTerrainEvidenceValidator.Validate(CombatCommandTransition transition)` conserves source IDs/classification and ordered terrain events. Exact mutation argument collections are feature-local implementation details, not a new public service.

- [x] Test original pillar cell unchanged, ordinary floor -> wall, cliff -> filled floor, existing wall/platform -> no debris, repeat filled impact idempotent, and wall beyond a wall still created. Test generated walls immediately affect later movement/line-of-sight queries.
- [x] Add atomic-publication rejection cases: missing fall cause, forged fill from ordinary floor, reused/gapped source ID, unauthorized old wall rewrite, mismatched bindings, and final state differing from the event chain. Valid multi-pillar changes must publish in one coherent transition.
- [x] Observe focused failures, then extend environment mutation and Buffer validators narrowly. Add generated walls to the same existing source allocator; preserve weak-ground, object-drop and trait-trail validation.
- [x] Ensure every queued reaction sees current terrain, not the `ObstacleReactionCalculator` constructor's stale environment snapshot. Reuse the command's current projection rather than creating competing worlds.
- [x] Keep impact logic dependent on `BoardSurfaceRules` and typed terrain writes only; do not call `PlatformMotionCalculator` to decide debris suppression. A later change to platform ordering must not require editing the pillar impact implementation.
- [x] Run focused pillar/publication tests plus static-terrain, health-evidence, barrel and relevant terminal/reward regressions. Report Phase 3 and stop.

### Phase 3 execution record — 2026-11-16

- Continued inline in the requested checkout on `feature/Cheon/Core/NewCombatSytem` at `d84e05bf959509197c8b18a4c69e7c6fc3691b41`. Inherited the actual Phase 1–2 implementation and its uncommitted changes. Saved 92 pre-existing code, metadata and documentation files before editing; asset preservation is recorded below. The fresh pre-change Core run reproduced the Phase 2 baseline: 5,930 passed, 11 existing diagnostic skips; four new pillar cases failed because the pillar key was unsupported. No completed phase was reimplemented.
- Added `EntityKey.Structure_Pillar = 28`, `ObstacleContentKind.Pillar`, and immutable `FallLength` on the obstacle profile/initial-placement contract. Battle preparation carries the configured length. Runtime SO fields and production activation remain Phase 6 work. The user supplied `C:/Users/권천명/Downloads/pillar-simulator.html`; its line 1807 formula is covered by literal axes, diagonals, off-axis vectors in all quadrants, lengths, and edge clipping, with HTML row-down mapped to Core Y-up.
- `PillarFallCalculator` owns geometry, impact recipients, and debris decisions. The existing obstacle coordinator now drains one FIFO queue of barrel/pillar consequences, preserving the command-wide destroyed-ID guard and current shared environment. Destroying-hit origin and child-effect origin remain distinct from kill credit. `SuppressSupplyRewards` propagates through crush-origin barrel/pillar descendants, including nonlethal supply contacts. Units on a platform are crushed while its surface and underlying cliff remain. Existing walls do not truncate impact; filled cliffs remain filled; surviving ordinary structures retain their HP and receive no debris wall.
- `CombatDamageApplication.CrushSteps` records `CombatDamageCause.PillarCrush` and bypasses numeric protection, before/after-damage item behavior, and Reconstruction. It reuses equipment discard, death facts, kill credit, health executions, and normal completion settlement. Tests cover equipment disposal, protection/Reconstruction bypass, multi-cell recipient deduplication, and completion of queued terrain/consequences after a main-character death. Multi-cell coverage invokes the real impact/damage boundary directly because the existing attack-profile contract does not admit multi-cell attackers; that unrelated contract was preserved.
- `CommandEnvironmentState.ApplyPillarTerrainChanges` updates terrain, classifications, and the existing shared source allocator. A new wall consumes the next ID; filling a cliff preserves its ID and visibility. `PillarTerrainEvidenceValidator` verifies ordered impact, death/destruction recipients and terrain evidence; the Buffer retains old-wall, weak-ground, object, trail, allocation and stale-state checks. Publication tests reject missing fall/terrain evidence, forged fills, reused/gapped IDs, reordered terrain events, old-wall rewrites, wrong classifications, and mismatched final state without changing any Buffer.
- **Approved deviation:** the existing wall builder rejected diagonal contact between separate wall components. The user explicitly approved accepting that topology and changing the Droute/Yonggeun-authored `WallGeometryTests.DiagonalContact` expectation. Separate components use the existing wall shapes and clipping rules; duplicate-cell, bounds, visibility and classification checks remain. The new acceptance case failed under the old restriction, then passed with the change. Generated walls are tested through movement admission and an actual subsequent attack line-of-sight query.
- **Independent review:** one read-only reviewer found a same-ID `HighGround/Sandstorm -> Wall` publication bypass introduced by relaxing wall-count conservation. Two publisher tests reproduced the acceptance; the Buffer now rejects any final wall that reuses an existing non-wall source. Both cases pass in the final 300-case integration run. No other actionable or deferred minor finding remained. Later motion/forecast/authoring and View acceptance were outside this review.
- Existing `CommandAttackProjection`, `MovementProjection`, `CommandDeathSettlement`, transition composition and publisher paths already share/preserve the required state and settlement contracts from Phase 1; no duplicate collaborators or extra service/Job were added. A fall-geometry change now concerns the pillar calculator and its evidence contract; platform support is consumed through `BoardSurfaceRules`, without platform ordering internals. Changes to shared terrain storage still require the environment/Buffer boundary and common terrain, health, reward, barrel, movement and attack regression. These are inspected dependency boundaries, not a measured claim about future editing effort.
- Full regression exposed two expected test-contract updates: new event samples in the common evidence-coverage fixture and new default public fields in frozen preview serialization. GwonCheonmyeong-authored fixtures retain their original assertions; the existing default-field normalization convention now omits only non-pillar `FallLength = 0` and `SuppressSupplyRewards = false`. Nondefault values remain visible. No frozen reference artifact was recaptured or rewritten. Compilation iterations also caught and corrected local enum/test-reference mistakes before the final runs.

| Final Unity EditMode scope | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full default `ProjectL.Tests.Core`, including 34 pillar cases | 5,966 | 0 | 11 | `core-final-3.xml`, `core-final-3.log` |
| `ProjectL.Tests.CoreIntegration`, the same 28 selected Core fixtures plus the new cases | 300 | 0 | 0 | `integration-final.xml`, `integration-final.log` |

Evidence directory: `C:/CodexHome/visualizations/2026/11/16/01a1e33c-8b27-7871-beb7-9ffbc4cc465f/phase3`. Both final runs exited 0 using the AppData-approved installed `D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe`. There were no compilation errors or recurring CurlRequestCache/Cinemachine TypeLoad failures in those runs. The 11 skipped cases are the existing opt-in performance/diagnostic captures. Non-selected integration fixtures, View compilation/playback, PlayMode, and visual acceptance were not run. No combat View MSBuild was used.

**View handoff for this phase:**

- Consume `CombatEventKind.PillarFell = 53` / `CombatPillarFallEvent`: pillar ID/cell, destroying `Origin`, `Reaction.Origin` for the pillar's effect, `FallLength`, `Direction`, ordered `ImpactCells`, `CrushedUnitIds`, and `DestroyedObstacleIds`. Read the recorded origins even when their source has already disappeared.
- Consume `CombatEventKind.PillarTerrainChanged = 54` / `CombatPillarTerrainEvent`. `Expected == null` means a newly allocated ordinary wall; otherwise it is the same cliff source becoming `FilledCliff`. These events identify debris/fill cells separately from all impact cells. The final Combat Buffer remains authoritative for terrain.
- Present the recorded order: obstacle destruction/reaction, pillar impact, casualty and child-obstacle destruction facts, terrain changes, then queued child consequences. Platform cells can contain casualties without debris or platform removal. Existing numeric obstacle-HP display migration from Phase 1 remains a View handoff item.
- Phase 4's platform motion and separate result-before-turn-start boundary are not implemented here; Phase 5 forecasts and Phase 6 authoring/full handoff remain pending. No View, FieldCamera, Architecture, scene, or production asset implementation was added.

The final asset check found only Unity's FMOD scan-path rewrite relative to the recorded Phase 2 asset checkpoint. Restored that one line and verified exact SHA-256 equality for both the FMOD catalog and font asset against that checkpoint. The frozen preview reference remains byte-identical to this session's starting copy. All inherited files remain present; the index and HEAD are unchanged. New C# files have their own metadata, LF text, no replacement characters and no UnityEngine dependency. `git diff --check` passed.

Phase 3 is complete. Stop here and wait for the user's instruction before Phase 4. No commit or push was performed.


## Phase 4 — Platform Motion Before Turn Start


### Task 4.1 — Sequential atomic transport

**Create:** Phase 4 runtime files; `T/PlatformMotionTests.cs`.

**Modify:** `C/Contracts/Combat/CombatCommandState.cs`, `CombatEvent.cs`, `CombatChangeEvidenceReader.cs`; `C/Application/Combat/CommandEnvironmentState.cs`, `MovementProjection.cs`, `MovementEffectUndo.cs`; `B/Combat/CombatStateBuffer.cs`. Extend the platform publication fixture.

**Interfaces:**

- `PlatformMotionCalculator.TryAdvanceBeforeTurn(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)` returns null or a complete platform prelude transition.
- `CombatCommandState.LastPlatformBoundaryCount` defaults to 0 and is preserved in every copy/composition path.
- `CombatPlatformMotionEvent`: `BoundaryCount`, `PlatformSourceId`, `OrderIndex`, initial/final anchor and sign, ordered translation/turn steps, passenger moves and stop reason. A step's passenger positions are atomic before effects.
- `PlatformMotionEvidenceValidator.Validate(CombatCommandTransition transition)` validates event/state conservation, not a duplicated movement solver.

- [x] Add `ThreeOneReboundConsumesRemainingTwo` (start X=3, rightmost anchor X=4, budget 3 -> path `3,4,3,2`), `ExactEndpointStoresInwardSign`, `BothSidesBlockedTerminates`, `LongerPlatformsMoveFirst`, and `EqualLengthOrderIsSeededRandom`. Assert paths, signs, boundary stamp and RNG, not only final anchors.
- [x] Test crossed/shared routes using current occupancy, full-footprint collision, own old footprint/rider exclusion, a filled-cliff blocker, and unexpected non-passenger occupancy causing reversal.
- [x] Test 2 and 3 distinct riders, no ordinary action-use consumption, no boss transport, stable per-step entry-effect order, tornado displacement ending carrying, a net-zero journey with nonempty events, and a terminal effect leaving all positions/effects coherent.
- [x] Test `RuntimeBossOccupancyKeepsPlatformStationaryAndSupported`: a forced-entry boss/current nontransportable occupant prevents that platform's entire move, preserves anchor/sign/support, and is never transported. Once the occupant leaves, the next prelude moves normally. Destination occupancy still follows the ordinary reversal rule.
- [x] Record failures, then implement ordering with existing `EnvironmentRandom`, finite one-step movement, atomic translation frames and existing entry/death effects. No UI input or adjacency pickup during processing.
- [x] Validate unsupported forged motion (changed route/shape, missing step, duplicate boundary, rider teleport, RNG mismatch) is rejected before publication. Preserve SourceIds/immutable definitions.

### Task 4.2 — Separate prelude and actual turn-start results

**Modify:** `C/Application/Combat/CombatCommandCalculator.cs`, `CombatTurnCalculator.cs`, `CombatFirstUseRehearsal.cs`, `MovementProjection.cs`, `DeathTraitApplication.cs`, `C/Domain/CombatEffects/Rules/ActionRestrictionTiming.cs`; inspect/update only as required `S/CombatAdvanceService.cs`, `EnemyAdvancePreparation.cs`, `CombatInputAssembler.cs`, `CombatTransitionPublisher.cs`. Do not implement a replacement `L/Plans/CombatPlan.cs` gate or modify View playback here.

**Consumes:** Task 4.1's boundary stamp and nullable prelude transition; existing result sequence/presentation acknowledgment.

**Produces:** For an eligible `StartingEnemy/StartingPlayer`, the first advance result remains `Automatic` with the same starting-side phase and unchanged turn sequence. The next admitted advance begins the turn. Initial player and no-platform states start normally.

- [x] Add Core tests `FirstPlayerTurnSkipsPlatformPrelude`, `FirstEnemyTurnMovesBeforeTurnStart`, `PreludeDoesNotRefreshActionsOrRunStartEffects`, `BlockedPreludeStillMarksBoundary`, `PreparedAndImmediateAdvanceMatch`, and `TerminalPreludeDoesNotBeginTurn`.
- [x] CoreIntegration assertions: prelude increments result sequence once, leaves `ControllerTurnSequence` unchanged, produces no `ControllerTurnStarted`, and the subsequent turn-start transition increments the controller sequence once without moving platforms again.
- [x] Route both ordinary and prepared advance through the same prelude check. Test stale prepared work after a platform/terrain change; no cached enemy decision can bypass the boundary.
- [x] Distinguish `CombatResolutionWindow.BeforeControllerTurn` from normal/end-gimmick calculation in `MovementProjection`; expose `PendingEndCleanupSide` and add a duration overload `ActionRestrictionTiming.Duration(int baseAffectedSideTurns, Owner affectedSide, Owner? pendingEndCleanupSide)`. Keep the existing turn-based overload delegating to the same formula. Prelude effects pass null because outgoing cleanup already ran; nested projections preserve the window. Test `PreludeFearDoesNotReceiveEndCleanupCompensation` against an end-gimmick case with the same `StartingPlayer/StartingEnemy` phase. Never rerun cleanup to compensate.
- [x] Inspect existing `CombatPlan.Decide`: unpresented platform result must remain behind `HasUnpresentedResult`. Record the caller contract and the later View/flow acceptance case instead of editing View/Flow or adding forbidden tests. Core tests establish two separately published transitions; the existing caller owns admission of the next advance. If that owner needs a change, place a concrete request in the handoff rather than adding a duplicate Core presentation flag/timer.
- [x] Run the focused Core motion/publication tests and controller-count, turn/status/Reconstruction, movement undo, railway/end-gimmick and automatic progression regressions. Report Core versus unverified presentation status; stop.

### Phase 4 execution record — 2026-11-16

- Continued inline after the explicit instruction to proceed with Phase 4, in the same checkout at `d84e05bf959509197c8b18a4c69e7c6fc3691b41`. Preserved the inherited Phase 1–3 implementation and saved Phase 4 starting copies, including the FMOD/font assets. The pre-implementation Core run retained the Phase 3 baseline; three new cases failed because the old advance started the controller before moving platforms. The initial-player exception already passed.
- Added `PlatformMotionCalculator.TryAdvanceBeforeTurn`, called by both immediate and prepared advance before the existing turn-start calculation. `CombatCommandState.LastPlatformBoundaryCount` is the sole processed-boundary fact; constructor validation, equality, `With`, turn-state reconstruction and movement projection preserve it. A blocked prelude still stamps the boundary. Initial player and zero-platform cases proceed directly to turn start. The subsequent result alone refreshes actions, processes Reconstruction/periodic statuses and advances the controller sequence.
- Motion groups start in source-ID order, run longest first and use the existing environment RNG for equal lengths. Each platform finishes its finite budget before the next uses current occupancy. The whole destination footprint is checked against route surface, bounds, other platforms, structures and non-passengers. Reversal consumes no movement budget; exact endpoints retain inward direction, and two-sided blockage retains the incoming direction. Tests include `3 -> 4 -> 3 -> 2`, net-zero travel, crossed/shared routes, filled-cliff blockage and self-overlapping footprints.
- `MovementProjection.ApplyPlatformFrame` publishes the platform and all rider positions together before stable entity-ID entry effects. Existing presence, thorn, tornado, forced-entry, death and arrival operations perform settlement. Transport spends no ordinary action use, never picks up adjacent characters, and removes dead/displaced riders from later steps. Runtime boss/nontransportable occupancy keeps the platform stationary and supported; a later prelude reevaluates occupancy. Two/three-rider frames, tornado ejection and terminal-frame coherence are covered.
- `CombatResolutionWindow.BeforeControllerTurn` is invocation context carried by the existing input-copy boundary, not persisted presentation state. Nested projections retain it; `PendingEndCleanupSide` is null because outgoing cleanup already ran. Fear consumes that explicit timing. Tests compare actual deaths in the same published starting phase: normal end-window Fear has duration 2, prelude Fear has duration 1. A dedicated final check verifies that Reconstruction and periodic damage remain untouched in the prelude and execute in the next result.
- Added immutable boundary/motion/step/RNG events and `PlatformMotionEvidenceValidator`. It checks boundary, definition/ID conservation, complete atomic rider frames, ordered steps, summary linkage, final positions/signs and random progression without duplicating collision/effect calculation. Publisher tests reject missing steps/motion, duplicated boundaries, changed routes, rider teleportation, wrong RNG progress and wrong boundary stamps atomically. A prepared source stamp becomes stale after platform publication, and replaying the old proposal is rejected without Buffer writes.
- Independent read-only review found two issues; both were reproduced in `focused-5.xml` (46 passed, 2 failed) and fixed. Attack-only departure normalization now emits the existing trait-state evidence when HP does not change. Its regression supplies a current calculation state; initial authoring still rejects a cliff/modifier overlap, and this phase does not relax that rule. RNG events now validate their exact next state and choice through `EnvironmentRandomSequence`, which owns the unchanged LCG/rejection-sampling step. Existing `Domain/Combat/EnvironmentRandom` keeps its path/API, seed mixing and singleton behavior and delegates that step; the algorithm is not duplicated. `focused-6.xml` passed all 48 selected cases. No unresolved reviewer finding remains.
- Inspected existing `CombatPlan.Decide`: `HasUnpresentedResult` is checked before automatic advance registration. The service, assembler, prepared-source/cancellation path and first-use rehearsal retain their existing contracts; starting phases do not request enemy decisions. No new service, Job, timer or presentation boolean was added. `MovementUndoPolicy` already clears receipts in automatic processing; no separate transport undo implementation was necessary. Actual display acknowledgment and playback are View/flow-owner acceptance work.
- Full regression initially identified only new-contract bookkeeping: event-coverage samples, the public-property guard and frozen serialization of the new zero-valued counter. Verified Core authorship before updating those fixtures. Their assertions remain; serialization omits only `LastPlatformBoundaryCount == 0`, while nonzero values remain observable. No frozen reference file was regenerated. Earlier test compilation iterations corrected test namespace references; final logs contain no compilation errors.

| Final Unity EditMode scope | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full default `ProjectL.Tests.Core`, including 21 platform-motion cases | 5,991 | 0 | 11 | `core-final-2.xml`, `core-final-2.log` |
| Same 28 selected CoreIntegration fixtures, including 8 new platform publication cases | 308 | 0 | 0 | `integration-final.xml`, `integration-final.log` |
| Final platform-motion rerun after strengthening actual Reconstruction/periodic/Fear assertions; no production changes after full regression | 21 | 0 | 0 | `motion-final.xml`, `motion-final.log` |

Evidence directory: `C:/CodexHome/visualizations/2026/11/16/01a1e33c-8b27-7871-beb7-9ffbc4cc465f/phase4`. All final processes exited 0 via the approved AppData-access route and installed Unity 6000.0.73f1. The full Core run includes controller counters, enemy progression, status/Reconstruction, undo, railway and end-gimmick regression. The 11 skips remain existing opt-in diagnostic captures. Non-selected integration fixtures, View compilation, PlayMode, scenes and visual acceptance were not run. No combat View MSBuild was used.

**Change/understanding/verification boundary:** Motion policy is edited in the platform calculator and its typed evidence contract; it consumes the common surface and movement-entry boundaries without learning pillar geometry or reimplementing other effects. Atomic placement and effect timing necessarily touch the shared projection, input-copy contract and Fear duration caller. Buffer owns installation/evidence checks, while LifeCycle retains admission and View retains presentation. Regression therefore includes shared movement, death/status, RNG, undo, controller progression and publication. This is an inspected dependency boundary, not a measured reduction in future editing cost. The shared RNG step is the only additional contract helper beyond the planned file list.

**View handoff for this phase:**

- Read current platform footprint/sign from the existing `CommandState.Environment` facts. Do not derive authoritative state from animation or retain a second passenger store.
- `PlatformPrelude = 55` / `CombatPlatformBoundaryEvent` identifies the completed-controller boundary and ordered platform IDs. `PlatformMotionResolved = 56` / `CombatPlatformMotionEvent` gives order index, expected/final facts, all ordered steps and stop reason. `PlatformStepped = 57` / `CombatPlatformStepEvent` gives translations/turnarounds and passenger `From`, `To`, `TransportEnded`. `EnvironmentRandomSelected = 58` is computation evidence; View does not reshuffle.
- Motion and step events summarize linked occurrences in the same stream. Each step links `SourceChange` and `CompletionEvent`; display the atomic passenger/platform frame and its following effects once, in recorded order. Net-zero paths still animate every recorded translation. Current signs support persistent direction indicators.
- Required sequence remains: previous result acknowledged -> platform result presented completely -> acknowledge that result -> admit the next advance -> controller-turn-start result -> eligible input/AI. CoreIntegration proves two separately published results and unchanged sequence/actions during the first. The existing Plan gate was inspected, but actual animation/input blocking and acknowledgment must be accepted by the View/flow owner. Core callers that bypass that owner do not acquire a replacement presentation gate here.
- Phase 5 forecasts, Phase 6 authoring/migration/final handoff, and View implementation remain pending. View compilation/playback is unverified.

The final preservation check restored only Unity's FMOD scan-path rewrite and verified exact starting hashes for FMOD, font and the frozen preview reference. All inherited files remain present, new C# files have metadata, and the index/HEAD are unchanged. Phase 4 is complete; stop here before Phase 5. No commit or push was performed.

## Phase 5 — Forecast Parity and View Read Contract

### Task 5.1 — Pillar outcomes in the attack forecast

**Create:** `C/Contracts/Results/PillarPlatformForecasts.cs`, `T/PillarPlatformPreviewTests.cs`.

**Modify:** `C/Contracts/Results/AttackPreview.cs`; `C/Application/Combat/CombatCommandCalculator.cs`, `CombatActionAssessment.cs`, `CombatHazardAreas.cs` only where the current forecast extraction uses them; `C/Contracts/CombatEffects/CombatAssessmentCoverage.cs` and AI context/cache consumers as needed for valid coverage/revisions.

**Interfaces:** `PillarFallPreview` contains source pillar/direction, distinct impact/debris cells, crushed IDs, terrain changes and chained consequences. It is extracted from the hypothetical execution transition, not a second impact algorithm.

- [x] Test exact forecast/execution parity for a zero-damage destroying hit, a non-destroying hit, platform passengers, walls in the footprint, and a pillar/barrel chain. Forecast must leave input state, RNG, rewards and event counters unchanged.
- [x] Remove old HP-based obstacle assumptions from AI assessment and coverage. Test that a pillar collateral path is assessed or explicitly marked incomplete, never claimed harmless because only barrels were scanned.
- [x] Verify old no-pillar attack forecasts and general HP evidence retain their contracts; declare coverage gaps for unsupported hidden/future conditions rather than inventing outcomes.

### Task 5.2 — Platform forecast request and persistent direction read

**Modify:** `C/Contracts/Combat/CombatPreviewResult.cs`; `B/CombatView/CombatViewPreviewKind.cs`, `CombatViewPreview.cs`, `CombatViewBuffer.cs`; `B/Combat/CombatStateData.cs`; `S/CombatPreviewService.cs`, `CombatInputAssembler.cs`. Extend `I/PillarPlatformPublicationTests.cs` for preview publication only.

**Interfaces:** `PlatformForecastResult` with battle/query/turn/boundary stamp, ordered platform/passenger paths, directions, and coverage. Add `CombatPreviewResult.ForPlatformForecast(int number, PlatformForecastResult forecast)` and append `CombatViewPreviewKind.PlatformForecast`. Add `CombatStateData.ReadPlatforms()` if needed as a read-only typed projection of existing environment facts.

- [x] Test repeated forecast returns the same order/path from the same source stamp without advancing real RNG. Execution from that identical source must match; changing terrain/occupancy or a preceding RNG consumer invalidates the stamp.
- [x] Test conditional forecast when end-turn hazards can intervene, cancellation during work, obsolete battle/request numbers, and no-platform empty success. No independent random shuffle in View or on every redraw.
- [x] Wire the forecast into the existing one-payload preview contract and service lifecycle. Verify both current direction reads and per-result turnarounds are available without interpreting model animation.
- [x] Run focused preview/publication, AI legality/cache and attack-preview resumption regressions. Prepare an example payload for View and report Phase 5; stop.

### Phase 5 execution record — 2026-11-16

- The existing attack projection builds `AttackPreview.PillarFalls` from its ordered fall, terrain, and attributed chain events in `AttackResultFactory`. Forecast debris exposes cells/kinds. Playback source IDs live in authoritative event 54. The frozen ordinary attack fixtures retain the same expected values, with the existing serializer omitting the new empty list.
- `PlatformForecastCalculator` applies player visibility and the installed environment RNG to a local input for the existing `PlatformMotionCalculator`. It publishes typed paths and ordered turnaround/passenger steps. The last committed boundary and platform direction are still read from the existing state. A pending start uses the current boundary; an active-turn query assumes today's board at the next boundary. Paths use `KnownInformationConditional` uniformly so the coverage itself cannot disclose a hidden object. Exact empty answers cover ineligible boundaries and stages with no platforms.
- `CombatViewPreviewData.PlatformForecast()` uses the existing latest request slot and preview service (kind 15). The assembler already accepts the existing start-turn calculation intent. The response contains exactly one payload and embeds battle, query revision, controller sequence, completed count and processed boundary. The service checks currentness on publication, the Buffer validates the kind/stamp and hides stale results, and `CombatStateData.ReadPlatforms()` projects the live immutable facts. No additional job or service registration is required.
- AI already skips obstacle compatibility HP in the numeric paths. A reachable pillar from attack, barrel or mine/arrival evidence now marks unsupported collateral and retains a conservative potential loss bound. The bound includes living allied units and ordinary structures when a pillar chain is possible. This keeps chain direction and damage rules in the actual calculation.
- The initial RED run showed the missing attack payload. Independent read-only review found two hidden-information leaks and a mine/pillar collateral undercount. `privacy-red.xml` reproduced both leaks, and `ai-red.xml` reproduced the undercount and passed the two privacy regressions after their fixes. Forecast now carries player visibility into grouped tornado contact, and debris DTOs expose no allocator ID. The following final regression checks these corrections alongside the existing simulation and frozen attack parity.
- The View example and caller protocol are in the [Core-to-View handoff](../handoffs/2026-11-16-pillar-platform-core-view-handoff.md#forecast-requests). Active-turn previews go through the usual plan. Direct Core tests exercise due-boundary calculation, cancellation/replacement during simulation, publication, and source invalidation. The synchronous preview Job contract and visual acceptance belong to View/Flow.

## Phase 6 — SO Input, Migration, Regression, and View Handoff

### Task 6.1 — Authoring and preparation

**Create:** `D/Stage/Combat/MovingPlatformAuthoring.cs`, `I/PillarPlatformAuthoringTests.cs`.

**Modify:** `D/Obstacle/Item/ObstacleDefinitionDataSO.cs`, `ObstacleDefinitionData.cs`; `D/Stage/Combat/CombatStageEnvironmentInput.cs`, `CombatStageEnvironmentAuthoringValidation.cs`, `CombatStageData.cs`, `CombatStageDetail.cs`; `S/DatabaseBattleInitializationSource.cs`; `C/Contracts/Combat/BattleInitialization.cs`, `BattlePreparation.cs`; `C/Application/Combat/BattlePreparationCalculator.cs`. Inspect `E/CombatStageDataEditor.cs`, `Tool/CombatStageSoDocument.cs`, `CombatStageSoDraftStore.cs` for field retention; graphical editor features are excluded.

**Interfaces:** SO `requiredHits` (positive), pillar `fallLength` (positive, default 3); serializable platform input maps sourceId, length, shape axis, route endpoints, initial anchor/sign into `MovingPlatformSpec`. `GetData()` returns new immutable pure data with no UnityEngine types.

- [x] Add authored-fixture tests for valid 1–3 lengths, independent axes, invalid routes/signs/counts/IDs/initial footprint, two platforms sharing a route, and first-turn initial placement. Add SO round-trip coverage only for the data serializer, not View UI interaction.
- [x] Implement conversion/validation and DB preparation propagation; keep Inspector labels clear about hit count versus damage and platform shape versus route. Confirm no new DB table/service registration is needed.
- [x] Inventory referenced obstacle assets and create `docs/superpowers/handoffs/2026-11-16-obstacle-hit-count-migration.md` with exact paths/GUIDs, old HP, proposed hit counts and approval status. Do not invent production values or use old HP as hits silently. Review concrete proposed asset edits with the content owner before applying them.
- [ ] Apply only approved migration values, preserving asset identity and unrelated values. New test-owned fixtures may use explicit counts immediately. Production activation remains incomplete while legacy referenced assets are unresolved; report that state accurately.
- [x] Run authored-fixture/CoreIntegration checks. Include `DataBaseAssetTests` in the user's Unity confirmation when production asset values change. Missing Script legacy assets remain untouched.

### Task 6.2 — Final contract handoff and necessary regression

**Create:** `docs/superpowers/handoffs/2026-11-16-pillar-platform-core-view-handoff.md` after implementation, with actual API names/event IDs and evidence rather than this proposal's unverified names.

- [x] Run the relevant full Core suite and the Core-only integration fixtures when execution is permitted. Reconcile failures with the recorded baseline; do not hide unrelated known failures or claim View acceptance from these results.
- [x] Check frozen definition/ID conservation, snapshots/copy paths, before-turn boundary stamp, random parity, undo/context invalidation, reaction recursion bounds, and stage-terminal cleanup with the final combined implementation.
- [x] Write the handoff: SO fields and initial placement; live platform/durability/terrain reads; ordered impact/motion/passenger events; forecasts and conditional status; persistent direction indicators; result acknowledgment; first-player exception; no input during motion; no authoritative View writes.
- [x] Include a concrete View sequence: previous result presented -> platform result shown in order -> presentation acknowledged -> controller-turn-start result -> input/AI. Include a 3->4->3->2 motion example, a net-zero journey, two/three passengers, crushing on a platform, and cliff-fill detouring a later platform.
- [x] Put manual Unity checks for animation/turn-start ordering, platform arrows and refresh, ordinary boarding/disembarking, forced ejection, casualties and terrain appearance in the **View-owned follow-up** section of the handoff. Include the observed `FieldUnitPresenter`, `CombatStructureHitFeedback`, and `CombatVolleyDamageNumbers` numeric-HP readers. Mark View compile/playback unverified until the View worker checks them. Do not implement these changes, write View tests, or run combat View MSBuild here.
- [x] Report Core phase results, remaining content approvals, and separate View handoff acceptance status. Core completion is based on its approved deliverables and required checks; integrated activation awaits View and content readiness. Stop without committing/pushing or taking over View work.

### Phase 6 execution record — 2026-11-16

- `CombatStageEnvironmentInput` converts the new serializable `MovingPlatformAuthoring` array to pure immutable specs in `CombatStageDetail`. `StageEnvironmentAdapter` turns each spec into its public initial terrain fact for both SO validation and the production `BattleDefinitionAdapter`. Existing `BattleInitialization`, preparation, Buffer validation and ID allocation consume those facts. This preserves the raw cliff underneath a platform and admits separate initial footprints on shared routes.
- The generic stage Inspector already draws the environment fields. The layout serializer retains those fields, reserves platform IDs for its other placement edits, reads validated platform footprints for unit overlap checks and clears the specs in the explicit empty-template-copy path. Shape geometry comes from the same spec. The graphical layout tool remains an authoring follow-up if platform route editing is needed.
- `ObstacleDefinitionDataSO` and its pure Data carry positive pillar `fallLength` (default 3). New assets created through the existing Unity asset menu receive one hit. Older assets with an absent `requiredHits` still require an explicit authored value. The authored fixtures own their counts and validate the first-turn DB/Buffer path.
- The [migration inventory](../handoffs/2026-11-16-obstacle-hit-count-migration.md) records both unused templates, old HP and exact GUIDs. Their GUIDs occur only in their own metadata in the searched assets. Production hit counts remain pending for future activation. No production obstacle or stage asset values were applied, so `DataBaseAssetTests` was outside these runs.
- The [View handoff](../handoffs/2026-11-16-pillar-platform-core-view-handoff.md) documents the actual event IDs, typed state and forecast reads, platform playback ordering, and manual acceptance cases. Core-owned verification and handoff are complete for the selected scope. View implementation, scene/playback acceptance, and production activation remain open.

| Final Unity 6000.0.73f1 EditMode scope | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full default `ProjectL.Tests.Core`, including 10 forecast cases | 6,001 | 0 | 11 | `core-verified-2.xml`, `core-verified-2.log` |
| `ProjectL.Tests.CoreIntegration`, 34 selected fixtures | 363 | 0 | 0 | `integration-verified.xml`, `integration-verified.log` |

The evidence directory is `C:/Temp/ProjectL-pillar-platform-phase56`. The final integration includes all 15 authoring cases and all 31 pillar/platform publication cases. The two final Unity processes exited 0 via the approved AppData-access route. The 11 skips are existing opt-in Core diagnostics. The recorded CurlRequestCache and Cinemachine errors did not recur. View-specific tests, MSBuild, PlayMode, and scenes were outside this scope.

Phase 3 and 4 work was already present at `d84e05bf959509197c8b18a4c69e7c6fc3691b41`. Unity rewrote only the FMOD scan path in the backed-up assets. Restored the starting bytes and confirmed SHA-256 for FMOD and the preexisting font edit. The existing HEAD and index were preserved.

### P2 follow-up — runtime obstacles on platforms — 2026-11-16

- The user authorized the review fix, publication validation and related Core regression, without committing or pushing. Preserved the existing uncommitted Phase 1–6 work and saved all 158 starting changed/untracked files before editing.
- Reproduced the real attack-knockback -> end-turn -> platform-prelude path for a barrel and a pillar. Previously the platform moved away from a living obstacle without a fall, and the motion validator accepted the unsupported state. The new focused run before the fix had 54 passes and six expected failures; an earlier test compilation pass corrected references to internal structure helpers without changing production accessibility.
- `PlatformMotionCalculator` now checks living structures across the current full platform footprint alongside nontransportable characters. It uses existing `IsDestroyed` and occupancy facts. A live occupant preserves anchor/sign/support and reports `NontransportableOccupant`; destroyed/departed obstacles do not leave a persistent lock. Destination occupancy keeps its existing reversal rule.
- `PlatformMotionEvidenceValidator` tracks structure movement/durability events in stream order. It rejects moving/reversing away from an initially occupied platform and a forged ordinary-blockage reason. Stop evidence captures occupancy at the motion boundary/last completed frame, so later reactions cannot retroactively invalidate an earlier stop. This is a validation-local projection, not another authoritative state store or gameplay solver.
- Added six Core cases and two CoreIntegration cases: barrel/pillar knockback onto a platform, an occupied non-anchor cell, destroyed/departed obstacles, an earlier platform's death-trait chain destroying the later platform's blocker, forged motion/reason rejection without Buffer changes, and the subsequent separate controller-turn start. Existing preview tests exercise the shared calculator. The new focused suite passed 60/60.
- The implementation change is limited to the platform calculator and evidence validator. Future edits need the common structure liveness/footprint and ordered-event contracts; they do not need barrel or pillar destruction internals. Verification retains movement, death/effects, tornado, controller timing, undo, forecasts and publication. The [View handoff](../handoffs/2026-11-16-pillar-platform-core-view-handoff.md#authoring-and-live-reads) records the additional stationary case; no View code, assets, public payload shape, service registration or initial-authoring rule changed.

| Unity 6000.0.73f1 EditMode scope | Passed | Failed | Skipped | Evidence |
| --- | ---: | ---: | ---: | --- |
| Full default `ProjectL.Tests.Core` | 6,007 | 0 | 11 | `core-final.xml`, `core-final.log` |
| Same 34 selected CoreIntegration fixtures | 365 | 0 | 0 | `integration-final.xml`, `integration-final.log` |

Evidence directory: `C:/CodexHome/visualizations/2026/11/16/01a1e33c-8b27-7871-beb7-9ffbc4cc465f/p2-fix`. Both final runs exited 0 using the approved AppData-access route; their logs contain no compiler, CurlRequestCache or Cinemachine TypeLoad failure. The 11 skips remain the existing opt-in diagnostic cases. Restored only Unity's FMOD scan-path rewrite and verified exact starting hashes for the FMOD/font assets and frozen preview reference. All inherited files remain present; HEAD and the empty index are unchanged, and `git diff --check` passes. View compilation/PlayMode and non-selected integration fixtures are outside this fix; no commit or push was performed.

### Delivery authorization — 2026-11-16

After the completed P2 report, the user explicitly requested committing and pushing this plan's work to `feature/Cheon/Core/NewCombatSytem`. This later instruction authorizes delivery of Phases 1–6 and the P2 follow-up despite the earlier implementation-only no-commit instructions. The delivery includes the Phase 1 barrel-cascade frozen-vector update. The unrelated FMOD catalog and font changes remain outside the commit. Final verification is the P2 record above; no behavioral source or test changes were made after those runs. Final staging removed only trailing whitespace. View acceptance and production asset activation remain separate follow-up work.

### Follow-up — prevent obstacle boarding — 2026-11-16

- After delivery commit `0d92812d09f035e3e23dde2d3522f75206d3e26f`, the user explicitly requested rejecting obstacle knockback onto platforms while retaining the P2 stationary defense, with Core regression and no commit/push.
- Added one admission condition in the existing `AttackKnockbackCalculator.KnockbackObstacleSteps`: no proposed footprint cell may have platform support. The existing `BoardSurfaceRules` owns the support classification, and the existing step solver owns stopping/blocked evidence. Damage/durability still resolves before knockback. No general unit-movement rule, platform runtime, publication validator, state owner or public API changed.
- Added five Core entry cases covering barrels, pillars, supplies, multi-cell obstacle overlap and a non-anchor platform cell, including preview/execution agreement. All five failed before the condition and pass afterward. The previous real-boarding cases now verify the authorized rejection through published attacks and subsequent platform/controller progression. This conversation's existing fallback tests now install an explicit exceptional occupancy instead of relying on the newly prohibited knockback; stationary support, release after departure and forged-motion checks remain. The wrong-stop-reason check is retained at the Core validator boundary. Focused verification passed 87/87.
- Modification/understanding scope is the obstacle step-admission rule plus existing surface semantics; it does not require pillar destruction or platform ordering internals. Verification still includes normal obstacle knockback/falls, unit boarding, stationary defense, previews, result publication, movement/undo and controller progression. The design and View handoff record the new policy. No production asset values or View code were edited.
- Full Core passed 6,013 cases with zero failures and the existing 11 diagnostic skips (`core-final.xml/.log`). The same 34 selected CoreIntegration fixtures passed 365/365 (`integration-final.xml/.log`). After removing an unrelated overlapping enemy from the exceptional-occupancy fixture, both fallback cases passed again (`fallback-final.xml/.log`); production code did not change after the full regression. All three final Unity processes exited 0 through the approved AppData-access route, without compiler/CurlRequestCache/TypeLoad failures.
- Evidence directory: `C:/CodexHome/visualizations/2026/11/16/01a1e33c-8b27-7871-beb7-9ffbc4cc465f/no-obstacle-boarding`. Restored only Unity's FMOD scan-path rewrite; exact starting hashes match for FMOD and font. `git diff --check` passes, and HEAD/index remain unchanged. No commit/push, View compilation, PlayMode, or non-selected integration run was performed for this follow-up.

- After the completed follow-up report, the user separately authorized committing and pushing this obstacle-boarding restriction to the same Core branch, explicitly excluding the FMOD/font changes. This later delivery authorization supersedes the follow-up's earlier no-commit instruction; the implementation and verification scope above is unchanged.

## Verification Commands and Evidence Rules

At execution time, use the actual approved Unity 6000.0.73f1 path discovered in Phase 0. The following is a command template, not a claim it was run. `$unityEditor` must be the verified executable; `$resultsDir` must be a task-owned output directory. Request the execution environment's approved AppData-access route when needed.

```powershell
& $unityEditor -batchmode -nographics -projectPath 'D:/My_ProjectWork/Github_desktop/Project_L' -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core -testFilter 'ProjectL.Tests.Core.ObstacleHitDurabilityTests' -testResults "$resultsDir/phase1-core.xml" -logFile "$resultsDir/phase1-core.log"
```

Use the same command with each named focused fixture and `ProjectL.Tests.CoreIntegration` for the new Core integration fixtures. Run the full `ProjectL.Tests.Core` assembly by omitting `-testFilter`. Do not blindly run all integration fixtures as a substitute for the View restrictions; distinguish Core publication/authoring fixtures from View-owned tests and user checks.

Pass evidence means the process completed, the expected fixture/test cases actually ran, result XML reports no failures for the selected scope, and logs contain no compilation/import exceptions preventing execution. A zero exit code or missing XML is not sufficient. If no approved runner is available, leave verification pending and report exactly what was not run; do not substitute prohibited MSBuild or weaken tests.

## Document Review Record

- Gameplay numbered questions: resolved in the confirmed-decision record, including ordinary-action boarding/disembarking and the separate movement-playback-before-turn-start requirement.
- Plan self-review: design sections 4/5.1/6.1 -> Phase 1; 5.2/5.3 -> Phase 2; 4.2/6.2 -> Phase 3; 4.3/7 -> Phase 4; 8 -> Phase 5; 9/10 -> Phase 6 and global constraints. No View implementation or commit steps.
- Builder review completed against owning source. Finding 1 (pending end-cleanup timing) -> design 7.1 / Task 4.2 explicit resolution window and Fear regression. Finding 2 (non-attack obstacle damage/healing/fall) -> design 5.1 / Task 1.2 explicit contact/no-repair/fall-without-reaction mapping. Finding 3 (boss/current nontransportable occupancy) -> design 7.3 / Task 4.1 stationary-support regression.
- Targeted follow-up builder review confirmed those three gaps are addressed; this was document review, not implementation verification.
- Locality-guideline follow-up: design 5.4 and Task 1.2 make command-local environment/queue lifetime explicit; design 5.1 and Task 1.1 inventory the deliberately limited HP projection; design 10 records observed versus proposed change/understanding/verification scope. Ownership and Phase 6 separate Core delivery from later View work. Phase dependencies distinguish actual rule dependencies from execution order, and Task 2.2 no longer mandates unrelated caller edits.
- At each phase report, list actual changed owners, the contracts a future editor must understand, the required regression boundaries and evidence level. Do not claim reduced understanding burden from file/class counts or claim proposed locality gains are runtime-verified.
- Locality revision document checks passed: local links resolve, 68 source references were checked with planned files distinguished, fences balance, English text contains no replacement characters, all seven phases are present, and implementation checkboxes remain unchecked. Documentation checks are not gameplay tests.
- The initial document review left all phases unchecked. The Phase 0 execution record now provides the current test baseline.
