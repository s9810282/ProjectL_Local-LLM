# Nexus Reinforcement Spawn Interval Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` for inline execution or `superpowers:subagent-driven-development` if that method is selected. Implement task by task, use checkboxes for evidence, and stop after each phase for the user's review. This document is a plan, not an instruction to start implementation, commit, or push.

**Goal:** Add a stage-authored enemy-turn interval that retries blocked reinforcement batches and restarts after any successful batch, including a partial batch.

**Architecture:** The existing immutable reinforcement spec owns the interval. Existing Buffer-owned reinforcement state records the last successful spawn turn; one pure eligibility predicate is shared by the calculator and publication validation. Existing enemy-start orchestration, creation events, and atomic publication remain the execution path.

**Tech Stack:** Unity 6000.0.73f1, existing C# Contracts/Application and ProjectL.Systems assemblies, NUnit EditMode tests in `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`. No new dependencies.

**Spec:** [Nexus Reinforcement Spawn Interval Design](../specs/2026-11-16-nexus-reinforcement-spawn-interval-design.md). Read the full design before execution.

**Status, 2026-11-16:** Phase 3 Core regression, Unity runtime and authoring acceptance, and handoff are recorded. Combat scene visual acceptance remains with View.

## Global Constraints

- Preserve the existing field limit, finite pool, batch size, placement rules, rewards, and next-enemy-turn action eligibility.
- Interval 1 is the default. Interval N first becomes due on enemy turn N. Once due, retry at each enemy start until one or more actors are created; then restart from that successful turn.
- A partial batch counts as success. Unused pool entries remain available; blocked or partial batches never accumulate extra batch allowance.
- Current turn remains in `CombatTurnFacts`; last success belongs only to `NexusReinforcementState`. Do not add a second current-turn counter, pending flag, next-spawn value, or remaining-cooldown field.
- Core remains pure; Buffers install state atomically. Keep existing Framework service/Job registration and View contracts. No View/UI, scene, FieldCamera, Architecture, or initial-spawn refactor is included.
- Documents are English. New code comments, XML documentation, and authoring tooltips are Korean. Preserve existing paths, namespaces, LF/UTF-8, and `.meta` GUIDs.
- This is a Core/authoring implementation plan. Do not create View tests or run combat View MSBuild. Use test-owned inputs; preserve existing assertions. Before changing another author's runtime-failing test, follow the repository's explicit review requirement. A general request to proceed does not waive that rule.
- No blanket production-stage retuning, new packages, cache deletion, Unity upgrade, commit, or push. The user performs commits/pushes. Any decision outside this design must be raised before implementation.
- Stop and report at each phase boundary. Recheck concurrent changes before edits; preserve unrelated FMOD, font, and pillar-platform handoff changes observed during planning.

## Review Focus

1. An existing serialized SO omits the new field and loads as zero instead of 1: Task 1 verifies reimport plus explicit invalid values.
2. A synthetic fixture starts at enemy turn zero or repeatedly creates without advancing turns: Task 2 fixes only feature-relevant fixture facts and verifies actual sequencing.
3. A partial batch or an actor killed immediately after creation fails to restart the interval: Tasks 2 and 3 pin both cases to accepted creation evidence.
4. A rejected publication or a no-creation result advances/resets the last-success stamp: Task 3 checks all-store rollback and unchanged progress.
5. Waiting for the nexus interval delays ordinary enemy actions or changes pre-effect field counting: Task 2 covers next-turn eligibility and periodic-status ordering; Task 4 retains the related regressions.

## File and responsibility map

Aliases below expand to exact repository-relative directories:

| Alias | Directory |
|---|---|
| `C/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` |
| `B/` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/` |
| `D/` | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Stage/Combat/` |
| `T/` | `Assets/Tests/EditMode/` |

| File | Planned responsibility/change |
|---|---|
| `D/NexusReinforcementAuthoring.cs` | Serialize interval; pass it to the spec; explain retry semantics in the Inspector. |
| `C/Contracts/Combat/NexusReinforcementSpec.cs` | Positive interval, default constant, appended optional constructor parameter. |
| `C/Contracts/Combat/NexusReinforcementState.cs` | Last-success stamp, constructor/equality invariants, shared due predicate. |
| `C/Application/Combat/NexusReinforcementCalculator.cs` | Check due state and stamp actual creation batches. |
| `B/Combat/CombatStateBuffer.cs` | Validate the stamp together with creation evidence and reject premature creation. |
| `C/Contracts/Combat/BattlePreparation.cs` | Check zero initialization in `InitialReinforcements`; edit only if the new constructor requires it. |
| `T/NexusReinforcementTestData.cs` | Supply configured intervals and realistic enemy-turn facts to feature tests. |

Extend existing test files named in the Tasks. No new production file is expected. If a genuinely needed test file is added, add its `.meta` with a new GUID. The existing `CombatTurnCalculator`, `CombatStateData`, `CombatInputAssembler`, and `BattleDefinitionAdapter` carry the required facts already; inspect their contracts without restructuring them.

## Verification and evidence

Before production edits, capture a fresh Core/CoreIntegration baseline and record existing failures with test names and diagnostics. Do not promote an old report into current evidence. Reuse focused selections during the Tasks and run both full assemblies again in Phase 3.

Resolve the installed Unity 6000.0.73f1 editor to `$unityEditor`, the authorized checkout to `$projectRoot`, and a unique writable evidence directory to `$resultsDirectory`. Run Unity only through the approved AppData-capable path. If an Editor already owns the project, use the approved in-Editor runner or coordinate its lifecycle; do not launch a competing Editor. The known sandbox `CurlRequestCache.db` failure is not a reason to delete caches or upgrade Unity.

For each focused run set `$testFilter` to the fully qualified fixtures named in its Task and `$runName` to a unique evidence label:

```powershell
& $unityEditor -batchmode -nographics -projectPath $projectRoot -runTests -testPlatform EditMode -assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration' -testFilter $testFilter -testResults (Join-Path $resultsDirectory ($runName + '.xml')) -logFile (Join-Path $resultsDirectory ($runName + '.log'))
```

For baseline/final regression use the same command without `-testFilter`. Fixture names are `ProjectL.Tests.Core.<Name>` or `ProjectL.Tests.CoreIntegration.<Name>`; join multiple names with semicolons. Assembly names and filter syntax were checked against current asmdefs and the installed Test Framework 1.6.0 command-line documentation; no runner was executed during planning.

Expected evidence is a completed NUnit XML with a nonzero executed-test count, the intended selection, and no unexpected failures, accompanied by the actual runner status/log. Missing XML, compilation errors, or environment failures are not passing tests. Report baseline failures separately; do not call a failing full suite green. Run a focused new behavior before and after its implementation, recording the intended red/green evidence. A failure unrelated to that behavior is not useful red evidence.

## Phase 1 - SO interval and immutable definition

### Task 1: Add the setting without changing runtime spawn timing yet

**Modify:** `D/NexusReinforcementAuthoring.cs`, `C/Contracts/Combat/NexusReinforcementSpec.cs`.

**Tests:** `T/NexusReinforcementDefinitionTests.cs`, `T/CoreIntegration/NexusReinforcementAuthoringTests.cs`.

**Interfaces:**

- Add `public const int DefaultSpawnIntervalTurns = 1` and `public int SpawnIntervalTurns { get; }` to `NexusReinforcementSpec`.
- Append `int spawnIntervalTurns = DefaultSpawnIntervalTurns` after `fieldEnemyLimit` in the existing constructor; preserve every preceding parameter and default.
- Keep `NexusReinforcementAuthoring.ToSpec(int enemyLevel)` unchanged in signature; add serialized `spawnIntervalTurns`, initialized from that constant, with `[Min(1)]`.

- [x] Add focused assertions: `Interval_DefaultAndConfiguredValues` expects default 1 and configured 2/3; `Interval_RejectsNonPositiveValues` expects `ArgumentOutOfRangeException` for 0/-1. Preserve existing definition assertions.
- [x] Add `Authoring_CapturesIntervalInPreparedBattle`: set 2, prepare, change SO to 3, and assert the old prepared spec remains 2 while a newly prepared spec reads 3. Disabled authoring still returns null even with inactive invalid values.
- [x] Add `LegacySerializedStage_UsesIntervalOne`: use a test-owned serialized stage with the new field absent, unload/reimport it, and assert interval 1. Contrast explicit serialized 0/-1, which must fail when enabled. Do not substitute a new in-memory SO for the serialization case or make Core fixtures depend on mutable production assets.
- [x] Run the two named fixtures with the focused command and record the intended red result.
- [x] Implement the field, Korean tooltip, immutable property, constructor validation, and conversion. Do not clamp zero or silently retune existing stages.
- [x] Rerun those fixtures; expect the new assertions and existing tests to pass, subject to recorded baseline failures. Inspect an existing serialized stage read-only in Unity for the same default. If the missing-field default fails, stop to revise the plan with a targeted GUID-preserving migration; do not invent a broad normalization step.
- [x] Report Phase 1 changes and evidence, then stop. The setting is carried through the definition at this checkpoint; timing behavior is not implemented until Phase 2.

### Phase 1 execution evidence

| Unity 6000.0.73f1 EditMode run | Result | Evidence |
|---|---|---|
| Pre-change Core and CoreIntegration baseline | 7,169 passed, 0 failed, 19 skipped by opt-in diagnostic or corpus gates. Core had 6,013 passes and CoreIntegration had 1,156. | `baseline.xml`, `baseline.log` |
| Focused before implementation | 39 passed and all 9 new interval cases failed on the missing constructor, property, or serialized field. | `red.xml`, `red.log` |
| Focused after implementation | 30 definition and 18 authoring cases passed. One separate read-only stage check also passed. | `green-with-readonly-stage.xml`, `green-with-readonly-stage.log` |

The serialized test-owned stage loaded as interval 1 after the field was removed and the asset was unloaded and reimported. Explicit serialized values 0 and -1 were rejected when enabled. Prepared battles kept their captured value after the SO was edited.

The separate Unity check loaded `Combat_LD_P20.asset` with the interval field absent from its YAML. Unity reported 1 through both the serialized property and the converted spec, and exposed the Korean tooltip. The asset SHA-256 matched before and after the run. The temporary check was removed after execution.

The change is confined to the authoring conversion and immutable Core spec. Existing preparation carries that spec through its current contract. Independent review found no changes needed. Spawn timing and playback checks belong to Phases 2 and 3.

## Phase 2 - Success-relative timing and atomic state

Tasks 2 and 3 form one implementation phase. Do not claim runtime delivery or roll out interval values between them: successful calculation and publication integrity must agree.

### Task 2: Record successful spawn turns and retry blocked batches

**Modify:** `C/Contracts/Combat/NexusReinforcementState.cs`, `C/Application/Combat/NexusReinforcementCalculator.cs`, `T/NexusReinforcementTestData.cs`; check `C/Contracts/Combat/BattlePreparation.cs` initialization.

**Tests:** `T/NexusReinforcementStateTests.cs`, `T/NexusReinforcementCalculationTests.cs`, `T/NexusReinforcementLifecycleTests.cs`. Preserve related `T/NexusReinforcementRewardTests.cs`, `T/NexusReinforcementGeometryTests.cs`, and `T/PeriodicStatusTurnTests.cs` assertions when adapting fixture calls.

**Interfaces:**

- Add `public long LastSpawnEnemyTurn { get; }` to `NexusReinforcementState`.
- Append `long lastSpawnEnemyTurn = 0` to `NexusReinforcementState(EntityId nexusId, uint randomState, IReadOnlyList<NexusReinforcementRecord> records, ...)`. Zero is valid only for empty history; nonempty histories must explicitly supply their actual positive turn. Include the stamp in `SameValues`.
- Add `public bool IsSpawnDue(NexusReinforcementSpec spec, long enemyTurn)`. Null spec throws `ArgumentNullException`; negative turn throws `ArgumentOutOfRangeException`; a last-success stamp later than `enemyTurn` throws `InvalidOperationException`. Disabled state or turn zero returns false for otherwise valid inputs. Return `enemyTurn - LastSpawnEnemyTurn >= spec.SpawnIntervalTurns` for positive turns in enabled state.
- Retain `NexusReinforcementCalculator.TryCalculate(CombatCommandInput startedEnemyTurn, int livingFieldEnemiesAtStart)` and its nullable no-creation result.
- Append `int interval = 1` to the feature-owned `NexusReinforcementTestData.Pool(...)` helper and `long enemyTurn = 1` to its `Input(...)` helper. Set realistic enemy-start/active turn facts. Pass explicit `completed:` values at feature call sites using `EnemyControllerPhaseTests.Input`; do not change that shared helper's global defaults.

- [x] Inventory constructor/helper callers with `rg -n 'NexusReinforcementState|NexusReinforcementTestData|EnemyControllerPhaseTests.Input' Assets/Tests/EditMode Assets/ProjectL/Script --glob '*.cs'`. Keep changes to callers affected by the new history/turn contract, and check test ownership before editing others' runtime-failing tests.
- [x] Add `SpawnProgress_ValidatesHistoryAndEquality`: negative stamp fails; empty history + positive stamp fails; nonempty history + zero fails; identical state with last-success 2 versus 3 compares unequal. Copies/composition retain the stamp; new battle and disabled state use zero.
- [x] Add `SpawnDue_UsesElapsedEnemyTurns`: with interval 2 and empty history, turns 0/1 are false and 2/3 are true; with one recorded batch at turn 3, turns 3/4 are false and 5 is true. Check intervals 1/3, a future stamp, null input, and a near-`long.MaxValue` turn without addition overflow.
- [x] Add `BlockedBatch_RetriesAndRestartsFromSuccess` for full field and zero candidate cells: turn 2 produces no creation and keeps last-success 0; turn 3 with space produces one batch and stamps 3; turn 4 produces none; turn 5 can create. Also cover turns 2/3 blocked -> turn 4 success -> next due turn 6, plus prolonged blockage without increased batch size.
- [x] Add `PartialBatch_RestartsInterval`: interval 2, batch 2, supply 8, one valid cell at turn 2 produces one actor, last-success 2, remaining supply 7; turn 3 creates none; enough space on turn 4 produces at most two and leaves supply 5. An empty pool never changes progress.
- [x] Add `WaitingInterval_PreservesTurnLifecycle`: existing enemies remain eligible on waiting turns; an actor created on turn 2 becomes eligible on turn 3 even though reinforcement waits until turn 4. Preserve player/deployment/terminal guards, duplicate-start rejection, and pre-effect field counting through the periodic-status regression.
- [x] Run State/Calculation/Lifecycle fixtures to record the intended red cases. Adapt fixture construction to positive current turns and explicit stamps without removing original assertions; advance turns between repeated batches in `WithoutReplacement_ConsumesOnlySelectedIndividuals`.
- [x] Implement constructor invariants (`ArgumentOutOfRangeException` for negative stamps, `ArgumentException` for history mismatch), equality, and the shared predicate. Keep new/disabled state at zero. Query and ordinary state copies must not reset progress.
- [x] Invoke the predicate after existing phase/configuration checks and before capacity/cell/RNG work. On actual creations, pass the current `CompletedPlayerTurnCount` into the new reinforcement state; on no creation, preserve the existing state. Keep capture of living enemies and normal turn processing in `CombatTurnCalculator` unchanged.
- [x] Run State/Calculation/Lifecycle/Reward/Geometry/PeriodicStatusTurn fixtures; check deterministic draws, action delay, unchanged no-creation RNG/allocation, and existing effect/reward assertions. Proceed to Task 3 within the same phase.

### Task 3: Validate progress with the creation transaction

**Modify:** `B/Combat/CombatStateBuffer.cs`.

**Tests:** `T/CoreIntegration/NexusReinforcementPublicationTests.cs`, reusing its `CoreIntegrationSnapshot`, publication helpers, and test-owned battle setup.

**Consumes:** Task 2's `LastSpawnEnemyTurn` and `IsSpawnDue`; existing typed creation events and the validated `ControllerTurnStarted` event. No new event or public Buffer method is needed.

- [x] Add `NoCreation_CannotChangeSpawnProgress`: after a real prior batch, forge a no-creation transition with a different positive stamp; expect publication rejection and `CoreIntegrationSnapshot.AssertUnchanged`.
- [x] Add `Creation_RequiresDueInputAndMatchingStamp`: reject a batch before its interval elapses and reject a final stamp different from the validated enemy-start turn. Preserve all stores, query revision, result sequence, inventory, RNG, and last-success on rejection.
- [x] Extend successful and duplicate/stale publication assertions: creation and stamp become visible together; publishing the same proposal again cannot advance progress. Extend `CreationFollowedByDeath_PublishesOneFinalActorWithoutRefund` to assert the creation turn remains recorded despite the immediate death.
- [x] Add `IntervalBattle_BlockedThenSuccessfulBatchRestartsCycle` through the real start/advance/publisher path: interval 2, turn 2 blocked, turn 3 creates, turn 4 has no creation event, turn 5 creates if capacity allows. The same sequence verifies prepared interval propagation and ordinary no-creation result publication.
- [x] Run the publication fixture for the intended red cases. Update its manually constructed `CreationProposal`, exhausted-range state, and tampering fixtures to provide valid history stamps before introducing the particular fault under test; do not turn unrelated invalid setup into the apparent rejection reason.
- [x] In `ValidateReinforcementCreation`, require unchanged `LastSpawnEnemyTurn` when `addedCount == 0`. For appended records, keep existing checks, obtain the matched enemy-start turn, require its completed-player count to agree with the final turn, reuse `previous.IsSpawnDue`, and require the final stamp to equal that turn. No duplicated timing formula or parallel scheduler.
- [x] Rerun CoreIntegration Authoring/Publication and the Task 2 focused fixtures. Review source changes against SSOT, SOLID, existing Framework boundaries, and the spec's change/understanding/verification map.
- [x] Report Phase 2 implementation, focused evidence, and outstanding Unity checks, then stop. Do not mark overall acceptance complete before Phase 3.

### Phase 2 execution evidence (2026-11-16)

Implemented the last-success turn and shared due predicate in the existing reinforcement state, used them in the calculator, and validated progress atomically with creation evidence in CombatStateBuffer. Blocked attempts retain progress; partial creation restarts the interval; actor action eligibility remains independent.

Evidence directory: Logs/ReinforcementIntervalPhase2/ (Unity 6000.0.73f1, approved AppData-capable execution).

| Run | Actual result |
|---|---|
| baseline.xml | 131 focused tests passed before Phase 2 changes. |
| red.xml | 22 passed, 2 failed: interval-2/3 first-attempt assertions exposed the prior runtime behavior. |
| guard-red.xml | All 9 selected tests failed at intended assertions when timing/publication guards were temporarily removed. |
| final.xml and final.log | After restoring guards: 146 passed, 0 failed, 0 skipped. |

Coverage includes first attempts at intervals 1/2/3, blocked attempts until turns 3/4/9, partial-success cooldown with free cells available, history/equality/large-turn validation, unchanged progress without creation, premature creation and wrong-stamp rollback, create-then-die, and action eligibility during the waiting interval. Real Core advance/publication tests cover regular intervals and a full field through turn 2, an ordinary player attack freeing capacity on turn 3, success on turn 3, and eligibility returning on turn 5 (blocked again by the one-enemy cap).

Independent review found no production correctness defect and identified two test issues: a wrong-phase fixture constructing an invalid zero stamp, and a cooldown assertion that could pass because cells remained occupied. Both were corrected. The latter and the publication guards were proved by the nine intentional failures, followed by the final passing run. Added state and publication tests also passed.

Fixture adjustments preserve original assertions. Explicit historical stamps and positive current-turn facts replace synthetic impossible states; repeated batches advance turns. New multi-turn fixtures use stationary zero-range enemies with explicit rewards, avoiding unrelated combat termination. No shared helper defaults outside reinforcement changed.

Continued in the existing feature checkout to retain uncommitted Phase 1 work. Phase 1 files and unrelated user changes were preserved. No commit or push. Full-assembly regression, Inspector/playback acceptance, and final handoff remain Phase 3; no PlayMode or visual acceptance is claimed.

## Phase 3 - Regression, authoring acceptance, and handoff

### Task 4: Verify the complete behavior and document usage

**Production changes:** none planned. If new failures require changes, classify them against the baseline and stay within the approved scope.

**Documentation:** update this plan with evidence and create `docs/superpowers/handoffs/2026-11-16-nexus-reinforcement-spawn-interval-core-view-handoff.md`. Keep the older reinforcement handoff as historical context; link the new interval-specific handoff from this plan.

- [x] Run both complete EditMode assemblies using the final regression command. Compare failures with the fresh baseline; preserve required integration checks and separate any unresolved pre-existing failures from change-specific failures. Repeat only checks affected by a subsequent fix.
- [x] Verify in Unity with a test-owned stage: interval 1 and interval 2, batch 1 and batch 2, full capacity, one free cell, and reopening space before an enemy start. Observe the documented 2-blocked/3-success/5-next sequence and partial success restarting the interval. Newly spawned actors act only from their next enemy turn, independently of nexus timing.
- [x] Recheck an existing serialized SO's default interval after reload and the new Inspector tooltip. Do not save a blanket interval-2 retune. If an approved asset edit occurs, include `DataBaseAssetTests` in acceptance and preserve GUIDs and unrelated values.
- [x] Write the handoff: SO field names and examples, retry/partial-success rules, `LastSpawnEnemyTurn` ownership, default compatibility, unchanged `CombatEventKind.Reinforced` playback, no required new View timer, and exact remaining editor/playback checks. Report only actual validation evidence.
- [x] Check the scoped diff, changed-file ownership, whitespace, and incidental Unity modifications. Preserve other work; do not clean unrelated FMOD/font/assets. Record compile/test/PlayMode status separately; use "compilation unverified" wherever that evidence is absent.
- [x] Report Phase 3 and stop. Do not commit or push. Runtime completion requires the specified evidence; a handoff listing pending Unity checks is documentation completion only.

### Phase 3 execution evidence

Unity 6000.0.73f1 ran through the AppData-capable approval path. The files below are in the local `nexus-interval-phase3` evidence directory.

| Run | Actual result | Evidence |
|---|---|---|
| Both complete EditMode assemblies | 7,185 passed, 8 failed, 19 skipped. CoreIntegration had 1,167 passes and 8 opt-in skips. | `full-editmode.xml`, `full-editmode.log` |
| Complete Core after serializer compatibility fix | 6,026 passed, 0 failed, 11 opt-in skips. All eight new failures recovered. | `core-editmode-after-serializer-fix.xml`, `core-editmode-after-serializer-fix.log` |
| Unity stage acceptance | 2 passed. An Editor test entered Play Mode for the Core scenarios and a second test reloaded the existing stage SO. | `unity-stage-acceptance-rerun.xml`, `unity-stage-acceptance-rerun.log` |

The eight failures all passed in the pre-change baseline. `AttackPreviewParityTests`, `AttackPreviewResumptionTests`, `EnemyActionAssessmentTests`, and `EnemyDecisionReuseParityTests` share the same test-only reflection serializer. Its new disabled-state `LastSpawnEnemyTurn: 0` member changed the frozen strings and dependent hashes. Git history identified the serializer and affected fixtures as part of the same Core work. Applied the serializer's existing default-field compatibility rule to that one field in disabled state and kept the enabled-state field in serialized comparisons. The frozen reference files and the compared gameplay assertions were preserved. The Core rerun covers the only assembly affected by the serializer adjustment.

The stage acceptance harness created in-memory combat stage SOs and entered Unity Play Mode in an empty scene. It used real Core battle entry, player commands, enemy start, and Buffer result publication. Each creation followed its controller-start event and began with deferred action use. Actors became available at the following enemy start even while the nexus was waiting.

| Authored scenario | Observed enemy turns |
|---|---|
| Interval 1, batch 1, space available | One creation each on turns 1 and 2. |
| Interval 2, batch 2, space available | Two creations each on turns 2 and 4. |
| Interval 2, batch 1, field cap 1 | Blocked on turn 2. Player attacks opened capacity before turns 3 and 5, which each created one. Turn 4 retained last success 3. |
| Interval 2, batch 2, one free cell | One creation on turn 2. Opening another cell before turn 3 still gave no creation on turn 3. Turn 4 created in the opened cell. |

The first acceptance harness attempt reached compilation and needed the installed Editor test runner reference for `EnterPlayMode` and `ExitPlayMode`. The successful rerun used a temporary reference. The acceptance harness, its metadata, and the test-assembly reference were removed after verification. Both production suites compiled during regression. The existing `Combat_LD_P20.asset` was unloaded and reloaded in Unity. The serialized property and converted spec both reported 1, and Unity exposed the Korean tooltip. Checksums for the stage, pre-existing FMOD and font files, and pillar-platform handoff matched before and after the runs.

Core runtime and authoring acceptance are complete. Inspector hover and the Combat scene's visual playback remain for the View owner. [Interval Core-to-View handoff](../handoffs/2026-11-16-nexus-reinforcement-spawn-interval-core-view-handoff.md)

## Planning self-review and execution record

Source inspection confirmed the spec/authoring conversion path, existing turn-count increment, feature-state equality, and the Buffer creation-validation location. The current feature fixtures include zero-turn and manually built creation histories; Tasks 2/3 explicitly account for them. Existing runtime ownership and required regressions are retained.

| Design coverage | Owning Task |
|---|---|
| SO/default/invalid values, captured definition, legacy serialization | 1 |
| First delay, retry, partial success, current turn and historical stamp | 2 |
| Atomic success, no-creation preservation, stale/rejected result, create-then-die | 3 |
| Full regression, actual Inspector/playback acceptance, handoff | 4 |

The interface names and parameter types above are the implementation choices for this plan. No runtime work or test execution was performed while writing it. Recommended execution is inline because the state, calculator, and publication changes share one small contract; the user may choose a different method before implementation.

| Phase | Implementation | Verification | Review boundary |
|---|---|---|---|
| 1 - Authoring | Complete | Baseline and focused EditMode runs recorded above | Stop after Task 1 |
| 2 - Runtime and publication | Complete | 146 focused EditMode passes; review findings addressed | Stop after Task 3 |
| 3 - Acceptance and handoff | Complete | Core EditMode and Play Mode acceptance recorded. Scene visual checks remain with View. | Stop after Task 4 |
