# Nexus Enemy Reinforcement — Core Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` for inline execution or `superpowers:subagent-driven-development` if that execution method is explicitly selected. Execute task by task and stop after each phase for the user's review. This document does not authorize implementation, commits, or pushes. Steps use checkboxes for future evidence.

**Goal:** Add finite, stage-authored nexus reinforcements to ordinary enemy-turn starts, preserving existing battle ownership and exposing the geometric spawn range to View.

**Architecture:** A dedicated pure calculator owns reinforcement selection and creation. Existing turn orchestration, shared entity allocation, effect reconciliation, character creation changes, and atomic Buffer publication carry its results. Append-only provenance determines pool consumption and per-instance reward suppression; remaining supply is derived.

**Tech stack:** Unity 6000.0.73f1, existing C# Core Contracts/Domain/Application assemblies, ProjectL.Systems, existing NUnit EditMode assemblies. No new package, service, job, event bus, or framework layer.

**Spec:** [Nexus Enemy Reinforcement — Core Design](../specs/2026-11-05-nexus-enemy-reinforcement-core-design.md). Read the complete spec before execution. `C/`, `S/`, `B/`, `D/`, and `T/` below expand to the exact repository directories in its section 3.

**Status, 2026-11-06:** Phases 1–4 are implemented. The final Core and CoreIntegration regression found no new failing cases against the pre-Phase 3 baseline. The View handoff is ready for its owner.

## Global constraints

- Initial placement and the finite reinforcement pool are separate. Field enemy limit is 5; default batch size and outward padding are both 2.
- Successful creation permanently consumes supply; failure, queries, rejected publication, and redeployment consume none. Night changes never reset it.
- New actors are `Deferred` during their spawn turn and participate in existing presence-effect reconciliation immediately.
- Only reinforcement kill gold/experience are suppressed. Keep existing credited kill/statistic, death, kill-trigger, and clear-reward semantics.
- Core owns rules; existing Buffers own state. View only receives immutable range/event facts. Preserve SSOT, all five SOLID principles, Framework contracts, and the design's locality comparison.
- Documents are English; new code comments/XML documentation are Korean. Preserve paths, namespaces, `.meta` GUIDs, LF/UTF-8, and project coding conventions.
- No View/UI/scene/FieldCamera/Architecture implementation, View test creation, MSBuild run, production asset activation, dependency installation, commit, or push is included.
- Core tests use test-owned definitions and catalogs. Required regression/integration verification is not waived by a small diff. Other authors' runtime-failing tests require the repository's explicit review before modification.
- Run Unity only through an approved AppData-capable execution path. Never treat the known sandbox cache error as a reason to delete caches or upgrade Unity.
- Stop and report after each phase; a request to proceed applies only to the approved phase scope. Recheck concurrent working-tree changes before touching files.

## Review focus

1. A newly created enemy is lost by an existing-character-only change builder or becomes actionable during the same start: cover in tasks 5 and 6.
2. A stale/rejected publication consumes inventory, IDs, or random state, or a second advance repeats the same batch: cover in tasks 4 and 6.
3. A hidden blocker or a multi-cell actual ground effect is missed, or a warning marker incorrectly blocks a cell: cover in task 3.
4. A state copy/night transition erases provenance and restores rewards or supply: cover in tasks 2, 6, and 7.
5. Gold/XP suppression accidentally removes attributed kills from statistics, including a create-then-die composition: cover in task 7.

## File and responsibility map

### New production files

| File | Responsibility |
|---|---|
| `C/Contracts/Combat/NexusReinforcementSpec.cs` | Immutable pool entry and definition, constants, validation, stable pool order. |
| `D/NexusReinforcementAuthoring.cs` | Serializable designer input and SO-reference-to-key conversion. |
| `C/Contracts/Combat/NexusReinforcementState.cs` | Immutable nexus binding, RNG progress, append-only consumption/provenance record; derived counts. |
| `C/Contracts/Combat/NexusReinforcementGeometry.cs` | Shared read-only geometric range from actual nexus occupied cells. |
| `C/Contracts/Combat/NexusReinforcementEvent.cs` | Typed initial-creation evidence for a nexus reinforcement. |
| `C/Application/Combat/NexusReinforcementCalculator.cs` | Strict valid-cell filtering, bounded deterministic selection, creation transition and presence reconciliation. |

Each new file and any new directory requires a corresponding `.meta`; use fresh GUIDs for new assets. Related small record/entry types can stay in the named cohesive file. Do not add interfaces or factories solely to split these files.

### Existing production files to extend

- Definition flow: `D/CombatStageData.cs`, `D/CombatStageDetail.cs`, `S/BattleDefinitionAdapter.cs`, `C/Contracts/Combat/BattleInitialization.cs`, `C/Contracts/Combat/BattlePreparation.cs`, `C/Contracts/Combat/CombatBattleFacts.cs`, `C/Application/Combat/BattlePreparationCalculator.cs`, `S/CombatInputAssembler.cs`, `B/Combat/CombatStateData.cs`.
- Runtime state: `C/Contracts/Combat/CombatCommandState.cs` and actual reconstruction sites identified in task 2. `CombatTurnFacts.EnemyOrigins` remains the existing origin store; no parallel origin field is added.
- Integrity/event integration: `B/Combat/CombatStateBuffer.cs`, `C/Contracts/Combat/CombatEvent.cs`, `C/Contracts/Combat/CombatChangeEvidenceReader.cs`.
- Turn/reward integration: `C/Application/Combat/CombatTurnCalculator.cs`, `C/Application/Combat/Rewards/ConfirmedKillCalculator.cs`, `C/Application/Combat/CombatCommandCalculator.cs`.
- Existing reuse points: `BattleCharacterCalculator.CreateEnemy`, `CombatCommandComposition.Project/Merge`, `TraitPresenceReconciler.ApplyAndRecord`, `CombatPlacementCalculator.RecallForNight/RedeployEnemies`, `CellEnvironmentQuery`, `EnvironmentRandom.Choose`, existing Tornado/Haboob geometry/projection. Change these only where a named contract requires it; do not blanket-refactor them.
- The existing `S/CombatTransitionPublisher.cs`, Character Buffer, Framework registries, and Jobs remain the publication/ownership path, not new feature owners. Only add narrowly required validation if phase tests prove a gap in the specified contract.

### Tests and delivery document

New Core fixtures: `T/NexusReinforcementDefinitionTests.cs`, `T/NexusReinforcementStateTests.cs`, `T/NexusReinforcementGeometryTests.cs`, `T/NexusReinforcementCalculationTests.cs`, `T/NexusReinforcementLifecycleTests.cs`, `T/NexusReinforcementRewardTests.cs`. Share a feature-owned `T/NexusReinforcementTestData.cs` only where these tests genuinely need the same input arrangement.

New integration fixtures: `T/CoreIntegration/NexusReinforcementAuthoringTests.cs`, `T/CoreIntegration/NexusReinforcementPublicationTests.cs`. Reuse existing Core-owned authoring/publication fixture facilities and build test-owned assets.

Future handoff: `docs/superpowers/handoffs/2026-11-05-nexus-enemy-reinforcement-core-view-handoff.md`. This plan does not edit the old View-to-Core request document.

## Verification runner and evidence rules

For each new behavior, run its focused test before implementation and observe the intended assertion or missing-contract failure, then implement and rerun. A compiler or environment failure unrelated to the test does not count as red evidence.

At execution preflight resolve the installed 6000.0.73f1 editor into `$unityEditor`; do not assume a machine-specific path. Set `$projectRoot` to the approved checkout, `$resultsDirectory` to a unique writable log directory, and `$testFilter` to the fully qualified fixture names named in each task. Through the approved AppData-capable runner, the focused command shape is:

```powershell
& $unityEditor -batchmode -nographics -projectPath $projectRoot -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration' -testFilter $testFilter -testResults (Join-Path $resultsDirectory 'focused.xml') -logFile (Join-Path $resultsDirectory 'focused.log')
```

Full required regression at the final phase uses the same command without `-testFilter` and unique `regression.xml` / `regression.log` output paths. An existing Editor session may require the approved in-Editor runner instead; never launch a competing Editor on the same project. Record the actual invocation/selection, exit/runner status, test totals, failures, and XML/log paths. A missing/empty XML is not a pass. No Unity version/cache workaround is part of this plan.

The `assemblyNames` and `testFilter` semicolon-list syntax was checked against the installed `com.unity.test-framework` package's `Documentation~/reference-command-line.md`; this check did not execute the runner.

Fixture filter names use `ProjectL.Tests.Core.<Fixture>` or `ProjectL.Tests.CoreIntegration.<Fixture>`; semicolon-separate multiple selections. Check current namespace/class names before running pre-existing fixtures. For each phase, record source changes, focused results, required regression results, and remaining user Unity/View checks. If a prerequisite blocks verification, report that evidence without marking the phase verified or silently weakening tests.

## Phase 1 — Authoring and pinned definitions

**Deliverable:** The optional configuration can be authored and prepared through DB and direct definitions. Existing stages remain unchanged. Live spawning is not wired yet.

### Task 1: Define and carry the separate finite pool

**Files:** Create the spec/authoring files above. Modify the definition-flow files listed in the map. Create `NexusReinforcementDefinitionTests` and `NexusReinforcementAuthoringTests` with `.meta` files.

**Interfaces:** Produce the exact `NexusReinforcementEntry` and `NexusReinforcementSpec` signatures in design section 5.1, `NexusReinforcementAuthoring.ToSpec(int enemyLevel)`, and nullable `NexusReinforcements` properties on the definition-flow types. Append optional constructor parameters so existing call sites preserve behavior. There is no new table query or service.

- [x] Write `DefaultDefinition_IsSeparateAndImmutable`: pool A x 3/B x 2, enemy level 3; assert the exact defaults and defensive copies. Write invalid-row/duplicate/overflow/empty-pool cases.

```csharp
Assert.That(spec.SpawnCountPerTurn, Is.EqualTo(2));
Assert.That(spec.RangePadding, Is.EqualTo(2));
Assert.That(spec.EnemyLevel, Is.EqualTo(3));
Assert.That(NexusReinforcementSpec.FieldEnemyLimit, Is.EqualTo(5));
Assert.That(spec.Pool.Sum(row => row.Count), Is.EqualTo(5));
```

- [x] Write `Authoring_PreservesInitialPlacementAndCapturesStageLevel`, `DisabledAuthoring_PreservesLegacyDefinition`, and `PoolOnlyUnit_HasResolvedAttackProfile`. Arrange a pool unit that is in the stage roster but absent from initial placement. Assert pool editing does not change initial placements/loadout seed/results; captured definitions do not mutate when authoring changes later.
- [x] Write preparation rejection cases: boss objective, missing/multiple nexus, initial enemy count above 5 when enabled, unit missing from roster/profile, player-only/boss/multi-cell candidate. Exercise both DB assembly and direct `BattleInitialization`; disabled stages retain their prior validation behavior.
- [x] Run those two new fixtures and record the intended red result.
- [x] Implement authoring defaults, stable sorted definition rows and validation; carry the immutable optional definition through all listed assembly/preparation/copy paths. Feed stage `enemyLevel` into `ToSpec` without another editable level. Validate pool eligibility before installation.
- [x] Run the new fixtures plus existing `EnemySpawnCalculationTests`, `BattlePreparationRuleTests`, and integration `BossStagePreparationTests`. Assert no production assets were changed or enabled.

**Phase 1 stop:** Report the definition/API diff and evidence. Do not start Phase 2 automatically.

## Phase 2 — Runtime facts, range, and cell rules

**Deliverable:** Prepared battles hold disabled or initial reinforcement facts; read-only geometry and strict candidate selection are available. Queries do not consume state. Live spawning is still not wired.

### Task 2: Add provenance state and preserve it through the battle lifetime

**Files:** Create `NexusReinforcementState.cs`; modify `CombatCommandState.cs`, `BattlePreparation.cs`, `BattlePreparationCalculator.cs`, and verified reconstruction sites such as `CombatTurnCalculator.State`, `CombatCommandCalculator.Finish`, and `MovementProjection`. Create `NexusReinforcementStateTests`.

**Interfaces:** Produce the record/state members in design section 5.2. Constructors are `NexusReinforcementRecord(EntityId entityId, int poolEntryIndex)` and `NexusReinforcementState(EntityId nexusId, uint randomState, IReadOnlyList<NexusReinforcementRecord> records)`, plus `Disabled`, `IsEnabled`, `SameValues`, `Contains`, `RemainingCount`, and `RemainingTotal`. Add optional `NexusReinforcementState reinforcements = null` to `CombatCommandState` construction/copy and its `Reinforcements` property.

- [x] Write `RemainingSupply_IsDerivedFromAppendOnlyRecords`: A x 3/B x 2 with records A,A,B leaves A=1/B=1/total=2. Reject duplicate/empty entity IDs and invalid indices. Disabled state is empty and immutable.

```csharp
Assert.That(state.RemainingCount(spec, aIndex), Is.EqualTo(1));
Assert.That(state.RemainingCount(spec, bIndex), Is.EqualTo(1));
Assert.That(state.RemainingTotal(spec), Is.EqualTo(2));
Assert.That(state.Contains(createdId), Is.True);
```

- [x] Write `Copies_PreserveProvenanceAndRandomState`, `DifferentProvenance_ChangesGameplayEquality`, and `NewBattle_StartsWithFreshSupply`. Cover `With`, projection/merge, preparation expected-state validation, turn/night reconstruction, and terminal cleanup, using non-default records/RNG so loss is detectable.
- [x] Run the state fixture for the intended red result.
- [x] Implement immutable state, initial nexus binding, and seed `unchecked((uint)TimelineSeed ^ 0x4E585350u)`. Store no independent remaining inventory, alive count, or reward flag.
- [x] Audit direct and target-typed `CombatCommandState` constructions and all copy helpers; preserve the new member explicitly. Use `rg -n 'CombatCommandState|source\.Allocation|source\.Gimmicks|gimmicks:'` in the owning Core/Buffer directories; inspect results instead of assuming `With` covers every reconstruction. Do not refactor unrelated state fields.
- [x] Run the state fixture plus existing `CombatCommandInputCopyTests`, `CombatCreationCompositionTests`, `ControllerTurnCountTests`, and `EightTurnNightTests`.

### Task 3: Implement one range formula, strict candidates, and Buffer read access

**Files:** Create `NexusReinforcementGeometry.cs` and start `NexusReinforcementCalculator.cs` with its candidate function; modify `CombatStateData.cs`; create `NexusReinforcementGeometryTests`. Add query cases to `NexusReinforcementPublicationTests` using prepared test-owned battles.

**Interfaces:** `NexusReinforcementGeometry.Area(StructureCombatFacts nexus, int rangePadding, int boardWidth, int boardHeight)`; `internal static IReadOnlyList<CellCoord> NexusReinforcementCalculator.Candidates(CombatCommandInput input)`; `CombatStateData.ReadNexusReinforcementArea()` returning `IReadOnlyList<CellCoord>`. Candidate calculation consumes authoritative facts and no RNG. The public query consumes pinned definition/state and returns geometric cells only.

- [x] Write `TwoByTwoNexus_PaddingTwoHas32Cells` using occupied cells (4,4),(5,4),(4,5),(5,5) on a 12x12 board; assert area bounds 2..7 on both axes, excluding the four occupied cells. Padding 1 produces 12 cells; a corner nexus at (0,0)..(1,1) with padding 2 produces 12 clipped cells.

```csharp
Assert.That(area.Count, Is.EqualTo(32));
Assert.That(area.Distinct().Count(), Is.EqualTo(32));
Assert.That(area.Intersect(nexus.OccupiedCells), Is.Empty);
Assert.That(area, Is.EqualTo(area.OrderBy(cell => cell.Y).ThenBy(cell => cell.X)));
```

- [x] Write rotated-footprint and large-padding cases. Assert finite board-bounded results without overflow, not a time-based performance threshold.
- [x] Write `Candidates_ExcludeActualBlockersButNotUnitInfluence`: occupied entities, every static terrain kind, active/consumed/hidden objects, thorns/trails/modifiers, full active Tornado/Haboob footprints, and rail cells. Add separate warning-only and prospective-blast-neighbor cases that remain allowed. Use existing geometry/projection owners and enumerate the current supported environment variants in the test arrangement.
- [x] Write `Candidates_ExcludeAllFriendlyCrosses`: both main and sub characters, diagonal cell allowed, hand/dead/waiting positions ignored, and no extra player-deployment-zone exclusion.
- [x] Write `BufferRange_IsGeometricAndReadOnly`: range stays identical when hidden blockers/cap/exhaustion change, exists during deployment, disappears for disabled/destroyed/finished source, and repeated reads leave RNG/records unchanged.
- [x] Run the new focused fixtures for the intended red result; implement the geometry, candidate predicate, and query exactly as design sections 6 and 9 specify. Avoid observer-filtered authoritative facts and duplicate state caches.
- [x] Rerun new fixtures plus existing `CellEnvironmentQueryTests`, `CombatHazardAreasTests`, and relevant existing Tornado/Haboob geometry tests identified from their owning files. Record which integration is actually exercised.

**Phase 2 stop:** Report state propagation and range/filter results, including the difference between geometry and currently available cells.

## Phase 3 — Atomic spawning, turn timing, and reward/lifetime integration

**Deliverable:** Configured ordinary stages can reinforce correctly through the real Core command pipeline. All required gameplay behavior is integrated before this phase is reported complete. No production stage is automatically opted in.

### Task 4: Extend creation provenance validation and event evidence

**Files:** Create `NexusReinforcementEvent.cs`; modify `CombatEvent.cs`, `CombatChangeEvidenceReader.cs`, `B/Combat/CombatStateBuffer.cs`; add store/event cases to `NexusReinforcementPublicationTests` and composition/evidence cases to feature-owned Core fixtures. Inspect `T/CombatCreationCompositionTests.cs` as existing contract evidence, preserving its character-summon cases.

**Interfaces:** `CombatReinforcementEvent(EntityId nexusId, NexusReinforcementRecord record, CharacterCombatFacts created)` with the design's properties. Append `CombatEventKind.Reinforced` using unused stable value 48 if still available. Extend the existing internal creation validator to consume transition events, before/final state, pinned definition, and final character facts as needed for the specified consistency checks; it does not run the selection algorithm.

- [x] Write `ValidNexusCreation_PublishesAllStoresTogether` using a hand-built valid enemy-start transition: one deferred created fact, one provenance record, one origin, one creation event, and one contiguous ID increment. Assert all are installed under the same result/revision.
- [x] Write rejection cases for missing/duplicate/cross-source records, overspent pool, changed prior records/origins, wrong nexus/type/index/phase, missing or mismatched creation event/origin, non-deferred actor, allocation gap, and RNG change without nexus creation. Assert character/combat/currency/provenance/ID/RNG state is unchanged on every rejected publication.
- [x] Write `CharacterSummonContracts_RemainUnchanged` and evidence tests for new event kind/type mismatch. No structure may masquerade as a character summoner.
- [x] Run these focused cases for red, then extend existing validation and event evidence. Preserve existing atomic prepare-before-install behavior and enum values; do not add writes in the calculator.
- [x] Run focused cases plus existing `CombatCreationCompositionTests`, `CombatChangeEvidenceTests`, `CombatChangeEvidenceCoverageTests`, `CombatChangeEvidenceVariantTests`, integration `CombatCommandPublicationTests`, and `CombatTransitionPublisherTests`.

### Task 5: Calculate bounded deterministic reinforcement batches

**Files:** Complete `NexusReinforcementCalculator.cs`; create `NexusReinforcementCalculationTests` and shared test inputs only if needed. Reuse `BattleCharacterCalculator.CreateEnemy`, `UnitCatalog`, `CategoryRegistry`, and `TraitRegistry` instead of duplicating baseline/trait initialization.

**Interfaces:** `internal static CombatCommandTransition TryCalculate(CombatCommandInput startedEnemyTurn, int livingFieldEnemiesAtStart)`. It consumes task 1/2 definitions and facts, task 3 candidates, and task 4 evidence. Null is an ordinary no-spawn result. An accepted result carries final `Created`, origins/provenance, allocation/RNG, and effects; no live caller yet.

- [x] Write parameterized capacity cases for live counts 0,3,4,5,6, expecting default spawn counts 2,2,1,0,0 with sufficient supply/cells. Set batch=4 with count=2 and expect 3. Add empty supply, singleton, partial-cell, no-cell, and empty-pool cases.
- [x] Write `WithoutReplacement_ConsumesOnlySelectedIndividuals`, `SameInput_ProducesSameBatchAndFinalRandomState`, and `NoSpawn_PreservesAllProgress`. Use A x 3/B x 2 and enough turns/cells to consume exactly that multiset. Verify cumulative draw intervals using deterministic helper inputs, not a flaky statistical distribution test.

```csharp
Assert.That(created.Select(actor => actor.EntityId).Distinct().Count(), Is.EqualTo(created.Count));
Assert.That(created.SelectMany(actor => actor.OccupiedCells).Distinct().Count(), Is.EqualTo(created.Count));
Assert.That(created.All(actor => actor.ActionUse == UnitActionUse.Deferred), Is.True);
Assert.That(finalState.Reinforcements.Records.Count - beforeState.Reinforcements.Records.Count,
    Is.EqualTo(created.Count));
```

- [x] Write `Creation_UsesPinnedStageLevelAndPoolProfile` with a pool-only type and non-default level; assert normal category/trait initialization. Write `AllNewActors_ParticipateInPresenceReconciliation` for Taunt and Solo Action, including an existing actor whose state must change.
- [x] Run focused tests for red; implement the fixed budget and deterministic order: pool draw then cell draw per member, singleton draws do not advance RNG, each selected cell removed, temporary quantities derived from records. Append origins and creation evidence before the one full-world presence pass.
- [x] Ensure result construction includes new actors and modifications to old actors. Resolve definitions from the appropriate unowned pool profile; do not pick a player-owned specialization profile merely because its unit key matches. No edits to shared unit reward definitions.
- [x] Run the calculation and geometry fixtures plus existing `TauntTraitTests`, `CombatTraitRuleTests`, and `EnemySpawnCalculationTests`. Confirm item/environment/summon RNG streams and initial-placement outcomes remain unchanged.

### Task 6: Wire one enemy-start batch and preserve night behavior

**Files:** Modify `CombatTurnCalculator.cs`; modify `CombatPlacementCalculator.cs` only if a narrow contract adjustment is demonstrated by tests (origins should allow reuse without a new branch). Create `NexusReinforcementLifecycleTests`; add real-service cases to `NexusReinforcementPublicationTests`.

**Interfaces:** The live trigger is existing `BeginControllerTurn`; public command/service signatures stay unchanged. Capture the pre-spawn count, preserve the existing start transition, project it, call task 5 once, and merge an accepted creation step before one publication. Preserve an ordinary no-spawn start result.

- [x] Write `EnemyStart_SpawnsOnceBeforeAi`: old actors are available, new actors are deferred, and the next `Advance` does not create a second batch. Player-start, deployment-entry, completed, and boss flows create none. Validate event order and single result publication.
- [x] Write `NewActor_CannotMoveOrAttackUntilNextEnemyStart`, including status restrictions persisting afterward. Assert the next eligible turn permits existing action rules rather than automatically forcing an action.
- [x] Write `NightRecall_RedeploysSameActorWithoutConsumingSupply`: origin available and blocked-origin fallback; entity/provenance/remaining quantity persist, `Placed` is emitted, and no new `Reinforced` event appears. Fresh-spawn normal-tile/cross/radius restrictions do not replace the agreed existing redeployment policy.
- [x] Write duplicate/stale calculation publication rejection and fixed-budget create-then-die composition cases. A creation followed by effect/death must yield one final created fact, no refund, no extra same-start refill, and no authoritative progress on rejected publication.
- [x] Run the lifecycle/service cases for red; wire the existing start boundary without a separate Job/System. Use the new turn state for post-creation effects and avoid an existing-character-only change helper dropping created facts. Preserve the ordinary existing presence pass and add one complete-batch pass, never a pass per new member.
- [x] Rerun new lifecycle/publication cases plus existing `EightTurnNightTests`, `ControllerTurnCountTests`, `StageEnemyAiTurnTests`, `CombatSummonTests`, and integration `CombatAdvanceServiceTests`, `CombatActionUsePublicationTests`, `TraitTurnLifecyclePublicationTests`, `TauntPublicationTests`.

### Task 7: Suppress only reinforcement kill gold and experience

**Files:** Modify `Rewards/ConfirmedKillCalculator.cs` and `CombatCommandCalculator.cs` under `C/Application/Combat/`; create `NexusReinforcementRewardTests`; add publication checks to the feature integration fixture. Existing experience/statistic settlement files are reuse points, not another suppression implementation.

**Interfaces:** Extend `ConfirmedKillCalculator.Collect` to `Collect(CombatCommandInput input, IReadOnlyDictionary<EntityId, CharacterCombatFacts> projected, IReadOnlyList<CombatEvent> events, CombatCommandState sourceState)` and pass `Finish`'s effective `sourceState`. Preserve death/attribution/base eligibility checks; for a credited reinforcement victim retain the record with `Gold=0` and `Experience=0`.

- [x] Write `SameType_InitialKillPaysAndReinforcementKillDoesNot`, with nonzero base rewards, active progression, known killer, and test-owned currency. Assert initial rewards unchanged, reinforcement gold/earned XP delta zero, credited kill count/statistic still increases, and no zero-amount XP-earned event is emitted.
- [x] Write indirect/reflected/environmental kill cases through current attribution paths, `CreateThenDie_UsesProjectedProvenance`, `AfterNight_RetainsRewardExclusion`, and `ClearRewards_RemainUnchanged`. Assert the shared unit definition has not changed.
- [x] Run the reward cases for red, implement the single amount-suppression point, then run them plus existing `CombatKillRewardTests`, `CombatRewardSettlementTests`, `CombatExperienceSettlementTests`, `CombatStageRecordTests`, and integration `CombatRewardPublicationTests`.

**Phase 3 stop:** Report the integrated mechanic and required regression evidence. State explicitly that production activation and View presentation remain pending. Do not label a creation-only partial implementation complete.

## Phase 4 — Full regression and Core-to-View handoff

**Deliverable:** Verified Core feature and a precise consumer/authoring handoff. Production content and visual acceptance remain separate work.

### Task 8: Exercise the complete stage lifetime and document consumption

**Files:** Complete `NexusReinforcementPublicationTests`; write the handoff path above; update phase evidence/status in this plan and the design only to reflect observed results. No View or production scene/asset edits.

**Interfaces:** Public contracts are the nullable pinned definition, `CombatStateData.ReadNexusReinforcementArea()`, immutable state/provenance, and `CombatReinforcementEvent`. User-visible View behavior is supplied by its owner after this handoff.

- [x] Exercise one test-owned DB-backed ordinary stage through initial deployment, first enemy turn, partial capacity, death/refill next turn, blocked cells/unblocked next turn, night recall/redeployment, pool exhaustion, nexus victory, and a fresh stage. Assert initial enemies never consumed pool entries, no pool deaths refund supply, and no state leaks across battles.
- [x] Exercise disabled ordinary and boss stages through the same public pipeline to preserve their behavior. Add a range read before/after movement of blockers to prove that public geometry does not become an exact hidden-blocker preview.
- [x] Run all new feature fixtures and the full `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` EditMode regression assemblies through the approved runner. Inspect the result XML/log, not only the process exit. Record unrelated failures faithfully and preserve the repository's test-ownership rule before changing any existing tests.
- [x] Run static scope checks: `git diff --check`, `git diff --name-only`, new-file `.meta` pairing, pure Data/Contracts no Unity references, and no accidental FMOD/font/portfolio/foreign capability changes. If production DB values were separately authorized and changed, include `DataBaseAssetTests`; that activation is not assumed by this plan.
- [x] Write the handoff with exact authoring fields, initial versus finite pool distinction, cap/batch/normal-cell examples, geometric query semantics, immutable snapshot refresh timing, event order/initial versus final facts, deferred turn actions, night `Placed`, and reward/statistic behavior. Identify `CombatEventTypeTable`, its handler table, and spawn presentation/range display as View-owned integration work. Do not edit them.
- [x] Record the locality comparison with actual modified files and actual verification. Distinguish source-confirmed behavior, executed tests, unverified compile/playback, and design expectations. Do not claim performance or smaller understanding scope was measured unless it was.

**Phase 4 stop:** Deliver Core evidence and handoff. The user owns commit/push and production activation decisions. No automatic next project or cross-owner implementation follows.

## Requirement-to-task coverage

| Design requirements | Tasks |
|---|---|
| R1 separate initial placement; R16 level | 1, 5, 8 |
| R2 finite non-returning supply | 1, 2, 4, 5, 6, 8 |
| R3/R4 count/cap/configurable batch | 1, 5, 6 |
| R5 turn timing and action deferral | 4, 5, 6 |
| R6/R16 centered adjustable clipped range | 1, 3 |
| R7 random draw without replacement | 2, 5 |
| R8/R9 normal cells and friendly crosses | 3, 5, 8 |
| R10 immediate surrounding effects | 5, 6 |
| R11 partial/no-space retries | 4, 5, 6, 8 |
| R12 gold/experience suppression | 7, 8 |
| R13/R14 night lifetime and identity | 2, 4, 6, 7, 8 |
| R15 read-only View range | 3, 8 |
| Atomic creation, evidence, SSOT/FW/locality | 2, 4, 5, 6, 8 |

## Phase evidence log

| Phase | Implementation status | Verification evidence | Review / next step |
|---|---|---|---|
| 1 | Implemented | Unity 6000.0.73f1 EditMode exited 0. All 87 selected tests passed. | Independent read-only review found no required changes. Await user review. |
| 2 | Implemented | Unity 6000.0.73f1 EditMode ran 157 selected cases. All 12 new cases passed. Three existing `EightTurnNightTests` cases failed. | Independent read-only review found no required Phase 2 changes. Await user review. |
| 3 | Implemented | The final run passed all reinforcement creation, publication, lifecycle, and reward tests. | Independent read-only review found no actionable production issue. |
| 4 | Completed | Full Unity 6000.0.73f1 EditMode run passed 5,699 of 5,771 cases. The remaining 72 failures match the baseline. | Core-to-View handoff written. Production authoring and View acceptance remain pending. |

The original documentation session checked R1-R16 coverage, phase stops, source references, local links, encoding, and code fences. Its review predates implementation.

### Phase 1 evidence, 2026-11-05

- The pre-implementation run reached Unity script compilation and exited 1 on the missing reinforcement contract (`red.log`, no NUnit XML). The first post-implementation test run produced 84 passes and two multi-cell fixture failures. Both fixture paths now assert the actual rejection boundary. DB conversion rejects the multi-cell attack profile, while direct preparation rejects the pool footprint before it needs that profile.
- The approved Windows runner used the batch command above with the existing feature checkout and the two Core assemblies. The final report contains 26 definition cases, 13 authoring cases, 13 enemy placement cases, 33 battle preparation cases, and two boss preparation cases. It contains zero failures or skips. The output directory is `C:/CodexHome/visualizations/2026/11/05/01a1ac21-8785-7f81-997b-17da4fd4fb38/nexus-phase1/` (`verified.xml` and `verified.log`). The compiler still emitted the existing CS8632 warning in `CombatCommandEffects.cs`.
- The existing stage Inspector exposes the serializable field through its normal property drawing. `CombatStageData` captures the one authored enemy level. The DB adapter checks roster membership before it adds owned or directly placed units and reuses the existing profile assembly path. Preparation validates the enabled definition before installation. The Buffer snapshot and command assembler carry the same immutable instance.
- Editing pool entries, batch size, or padding involves the stage authoring input and its validation contract. Initial placement and weighted-loadout RNG remain owned by their existing code, with the unchanged seed and generated placements exercised by a test-owned loadout. This phase added an optional loadout table to the shared integration fixture for that regression. Source inspection found no new service, registration, Job, or mutable store. Existing unrelated asset and combat-effect changes retained their original hashes.
- Runtime spawning, range, PlayMode, and full-assembly regression evidence belong to the later phase gates.

### Phase 2 evidence, 2026-11-06

- The pre-implementation state and geometry runs reached Unity compilation and showed the missing contracts in `state-red.log` and `geometry-red.log`. No NUnit XML was produced for those runs. The new state fixture uses the configured night length and supplies battle facts so it executes the actual night recall path.
- The approved Windows runner used the existing feature checkout and selected 15 Core and CoreIntegration fixtures. The final report contains 154 passes, three failures, and zero skips. Unity exited 2. All six state cases, four geometry and candidate cases, and two Buffer range cases passed. Phase 1 definition and authoring cases passed again. The output directory is `C:/CodexHome/visualizations/2026/11/05/01a1ac21-8785-7f81-997b-17da4fd4fb38/nexus-phase2/` (`final.xml` and `final.log`).
- All three failures came from `EightTurnNightTests`. Those cases expect the night to end at turn 8. Commit `3a20dc9a3` had already changed `PlayerTurnRules.PlayerTurnsPerNight` to 99. The production rule and the existing fixture are outside the Phase 2 diff. The Phase 2 state fixture exercised a complete night with the current rule.
- Binding the actual nexus entity and seeding the feature RNG happen in `BattlePreparation` after the structure facts exist. State copy and equality tests cover turn start, night recall, composition, movement projection, terminal settlement, and fresh preparation. The final settlement already uses `With` and inherits preservation from that contract.
- The internal candidate rule reads existing occupancy, semantic bindings, environment types, and gimmick facts. Changing one spawn exclusion belongs in `NexusReinforcementCalculator` and its candidate tests. The geometric formula and bound-source lookup are shared with the Buffer range reader. The range tests enter a test-owned DB-backed stage through the normal runtime and use derived snapshots to exercise exhaustion, hidden blockers, and battle or nexus loss. These tests cover reads before Phase 3 introduces creation publication.
- Source review checked SSOT and existing Framework ownership. The phase adds no mutable inventory owner or Framework registration. The worktree check preserved the unrelated asset and combat-effect hashes. Live spawning and full-assembly regression remain later phase work.

### Phase 3 evidence, 2026-11-06

- The pre-implementation run reached Unity compilation and reported the missing `CombatReinforcementEvent` and `TryCalculate` contracts. The red log is `phase3-red.log` in the output directory below. The final run passed the rejection, stale publication, and accepted batch cases. It also passed deterministic weighted selection, full-batch Taunt and Solo Action reconciliation, action deferral, night redeployment and its fallback, and direct, indirect, reflected, and uncredited death coverage.
- `CombatTurnCalculator.BeginControllerTurn` captures capacity before ordinary start processing and projects that transition once. `NexusReinforcementCalculator` creates the bounded batch with the shared character factory and a local quantity copy. `CombatCommandCalculator.Finish` reconciles presence on the complete batch and carries final created facts. Existing composition and publisher code produce one result.
- `CombatStateBuffer` validates contiguous allocation, unchanged prefixes, distinct summon and reinforcement provenance, matching events and origins, and finite supply before any store installs. `ConfirmedKillCalculator` reads the effective state in `Finish`. The credited kill remains available to statistics, and only its gold and experience amounts become zero.
- Existing tests gained one event sample and the Phase 2 state property in the exhaustive guards. Four existing reward test calls now pass their source state. The DB lifetime test also exercises post-redeployment reward exclusion and unchanged clear gold.

### Phase 4 evidence, 2026-11-06

- `DbStage_CompletesReinforcementLifetimeAndStartsFreshOnNextNode` uses the normal DB source and public command and advance services. It covers initial deployment, one remaining field slot, a blocked start with unused supply, a later available cell, permanent consumption after death, exhaustion, a full night under the current turn rule, redeployment, nexus victory, and a fresh node in the same runtime. The disabled and boss cases use the same public path. The range is also checked across blocker movement and against hidden terrain.
- The approved Unity 6000.0.73f1 batch runner ran both full EditMode assemblies. The baseline before Phase 3 ran 5,722 cases with 5,649 passes and 73 failures. The final run ran 5,771 with 5,699 passes, 72 failures, and zero skips. Unity exited 2 for both. All 99 tests in the reinforcement fixtures and the added exhaustive event sample passed. Comparison by fully qualified test name found zero new failures and zero changed messages among the 72 remaining failures. The Phase 2 contract shape guard is the one resolved baseline failure.
- The baseline failures include tests that expect an eight-turn night, the Cinemachine camera-offset type load, and existing View and production database fixture failures. The output directory is `C:/CodexHome/visualizations/2026/11/05/01a1ac21-8785-7f81-997b-17da4fd4fb38/nexus-phase3-4/`. Use `baseline.xml` and `baseline.log` for the baseline and `regression.xml` and `regression.log` for the final run. The targeted DB lifetime and state verification also passed 33 of 33 cases in `lifetime-focused.xml`.
- Scope checks covered `git diff --check`, the changed file list, new C# and meta pairing, LF on the new files, and the Unity-free Core contracts and calculation. The unrelated FMOD, font, and combat-effect changes retained their original hashes. The handoff's local links resolve. The independent source review found no actionable production issue.

| Change scenario | Modification and understanding scope confirmed from source | Verification exercised |
|---|---|---|
| Tune supply, batch size, or padding | Author the stage field and read the immutable definition contract. Initial placement retains its existing source and RNG. | Authoring, initial placement, weighted-loadout independence, geometry, and capacity fixtures. |
| Change a spawn blocker | Edit `NexusReinforcementCalculator.Candidates` using the authoritative spatial and gimmick contracts. Existing movement and night placement rules retain their own policies. | Hidden and consumed sources, rail, Tornado and Haboob footprints, crosses, and DB-backed blocked and unblocked starts. |
| Change creation or rewards | The turn caller, creation validator, and settlement each own their separate contract. The only reward amount override is in `ConfirmedKillCalculator`. | Atomic publication, same-type rewards, death composition, recall and replay, credited kills, and clear settlement. |

The source review found one immutable definition and one Buffer state owner. Calculation depends on Core facts, the range API returns cells, and existing callers retain the disabled default. The feature extends the existing result and validation contracts without an extra Framework service, Builder registration, Job, or DB table. This documents the inspected boundaries and test coverage. The test run did not measure performance or developer effort.

The View owner has the event, range, and authoring contract in the [Core-to-View handoff](../handoffs/2026-11-05-nexus-enemy-reinforcement-core-view-handoff.md). View playback and PlayMode acceptance are pending.

### Whole-plan review and publication fixes, 2026-11-06

The follow-up review covered Phases 1 through 4, with additional attention to Phase 3. It found two publication defects missed by the earlier source review: final created actors with received Taunt or SoloAction effects were rejected by the freshness gate, and a final created position could differ from the creation event without a movement event. Both were reproduced before the fixes.

- `CharacterBuffer` now applies its existing freshness gate to the typed reinforcement event's initial fact. It still validates and installs the final fact. `BattleTransactions` passes those events through the internal preparation contract; general creation and character-summon callers keep their previous freshness checks.
- `CombatStateBuffer` validates the pinned initial enemy level and follows ordered character and trait-state events from creation to final placement. It rejects changes before creation, inconsistent expected positions, unexplained final positions, and changed identities. Recorded movement and death remain valid. Partial effect events are not treated as complete state snapshots.
- State ownership and atomic installation are unchanged. The fix touches the two existing validators and their transaction connection, rather than adding state or a second effect calculator. Understanding the fix requires the initial/final creation contract and existing event snapshots. Verification covers publication and the shared Buffer regressions; unrelated feature implementations need no edits.
- Ten regression cases were added to the existing publication fixture. Before the fix, that fixture had 30 passes and seven failures out of 37 cases. After the fix it passed all 37, including Taunt, SoloAction shields, dirty initial facts, malformed placement chains, and recorded movement/death.
- The independent whole-plan source review found no further actionable defect in authoring, pinned definitions, state copies, candidate geometry, rewards, night lifetime, or handoff contracts. This is source-review evidence, not View or PlayMode acceptance.

Review-fix test output is in `C:/CodexHome/visualizations/2026/11/05/01a1ac21-8785-7f81-997b-17da4fd4fb38/nexus-review-fixes/`. `red.xml` records the reproduced failures and `green-fixed.xml` records the passing publication fixture.

The full post-fix run (`regression.xml`) executed 5,781 Core/CoreIntegration cases: 5,709 passed, 72 failed, and zero skipped. All 109 reinforcement-related cases passed. Compared with the Phase 4 `regression.xml`, there were zero new failed test names and zero changed failure messages. The same 72 baseline failures remain; the assemblies are not globally green. `git diff --check` passed, and the unrelated FMOD, font, and combat-effect files retained their pre-fix hashes. View/PlayMode validation remains pending.
