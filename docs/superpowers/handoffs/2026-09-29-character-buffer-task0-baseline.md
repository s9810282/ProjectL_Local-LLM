# CharacterBuffer Migration — Task 0 Baseline

Date: 2026-09-29

Plan: [CharacterBuffer and Effect State Migration](../plans/2026-09-29-character-buffer-effect-migration-plan.md)

Status: **Task 0 complete: workspace inspection, Core compilation, and both requested Unity EditMode suites executed. The Framework baseline is not green.** No Task 1 implementation, runtime-code changes, test deletion, commit, or branch integration was performed in this task.

## 1. Checkout and reproducibility

### Workspace consolidation after baseline collection

On 2026-09-29, the user requested one working checkout for this task. Subsequent work uses `D:/My_ProjectWork/Github_desktop/Project_L` on `feature/Cheon/refactor/core-system`, without creating another branch or worktree. The three changed C# files and both migration documents matched the additional checkout exactly before consolidation. The only substantive asset difference was FMOD's automatic scan timestamp and checkout-specific scan path; the main checkout's values were retained. All 27 test evidence files were copied with SHA-256 verification to `D:/My_ProjectWork/Github_desktop/Project_L/Logs/framework-test-verification-archive-20260929/`, including the `cbm-task0-20260929-01` directory. The progress ledger was also preserved in the main checkout. The paths and commands below document where the original baseline actually ran; they are not instructions to recreate that checkout.

The app confirmed `framework-test-verification` as an archived worktree, and its checkout directory and active Git worktree registration were removed. The pre-existing `chapter-map-task3` checkout belongs to earlier work, is clean, and its branch tip is already an ancestor of the current branch; it was not used or modified by this consolidation.

```powershell
$migrationProjectPath = 'C:\CodexHome\worktrees\framework-test-verification\Project_L'
$migrationEvidence = Join-Path $migrationProjectPath 'Logs/cbm-task0-20260929-01'
$migrationEditor = 'D:\Program Files\Unity\Hub\Editor\6000.0.73f1\Editor\Unity.exe'
```

- Reused the managed worktree already attached to this chat. Its existing name is retained; no second checkout was created.
- Verified `git rev-parse --show-toplevel` against the absolute path above. No Unity Editor was using that checkout when the batch runs started.
- Tested HEAD: `73e50be92232b583b5cfa25aec3ababe207816c7` (detached worktree).
- Source checkout: `D:\My_ProjectWork\Github_desktop\Project_L`, branch `feature/Cheon/refactor/core-system`.
- Refreshed plan base: `382a5da3405f29b96ad1e61f58d2250feb254650`. The committed delta to tested HEAD contains only the character migration plan revision. The Core subtree is unchanged between the original investigation `655607206d2aa58a3c4a6a67340b736592e0caac` and the refreshed base.
- This is **HEAD plus the existing local fixes**, not a pristine-commit baseline. The three C# files below and the plan/spec were hash-compared with the source checkout before testing and matched:
  - `CoreCaculator/Application/Run/RunProgressionService.cs`: the previously made disposed-stage validation-order fix.
  - `Assets/Tests/EditMode/Framework/ChapterGraphPreparationTests.cs`: existing API/fixture adaptations; the obsolete table lookup still fails.
  - `Assets/Tests/EditMode/Framework/MapProgressBufferTests.cs`: existing Artifact snapshot API adaptation.
- A pre-existing FMOD catalog modification was preserved. Git also reported EditorBuildSettings, Unity Sequences settings, and ShaderGraph settings in the worktree; they are recorded rather than silently discarded. The pre-run semantic diff contained the three C# files and the FMOD asset. Unity's automatic FMOD catalog scan ran during import.
- `status-before.txt`, `head.txt`, and `local-baseline.patch` in the evidence directory capture the starting state. The binary-capable patch records the local tracked delta. Test outputs below belong to this checkout and this execution.
- `ProjectL.Tests.Core.csproj`, `ProjectL.Systems.csproj`, and `ProjectL.Tests.Framework.csproj` exist. The Framework project lists all 13 current C# files in its Framework test directory, including the test helper. The plan's older “2 of 7 files / no Systems project” inventory is superseded. Generated projects were not hand-edited.

## 2. Documents and execution boundary

The plan, its character migration design, and these required documents were read during this task's preceding readiness review and checked against current source:

- `Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md`
- `Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md`
- `docs/superpowers/framework/Framework Development Guidelines.md`
- `Assets/ProjectL/Script/README.md`
- `docs/superpowers/handoffs/2026-09-29-view-input-buffer-core-contract-ko.md`
- `docs/superpowers/plans/2026-09-23-buffer-authoritative-run-map-implementation-plan.md`

The historical draft path `C:/Users/hyeon2-400/Downloads/CharacterBuffer.txt` is unavailable. As explicitly permitted by the plan, its restated ownership and immutable-data requirements govern the work. Historical reference-document instructions do not expand Task 0 authorization.

The Framework documents contain historical inventory and scaffolding descriptions. Current source confirms the Stage NewRun/Start changes and Systems-to-Application dependency. Use actual registration signatures when implementing: Factory creates services, ExportServices registers service/module interfaces, PostBindService binds dependencies, Plan constructs Jobs, and Runner executes them. Core.Contracts/Application/Domain must not depend on Systems Buffer interfaces.

Execution rulings:

1. Execute Task 0 only, per the user's explicit request; continuous execution of the entire migration plan is not authorized by this turn.
2. Preserve the existing local fixes and record them in the baseline. Testing clean HEAD would omit the user's ongoing fix and would not represent the inspected checkout.
3. Use the installed Editor on drive D. The plan's sample drive-C executable is absent. The existing direct batch command requires no custom runner, assembly, or CLI installation.
4. Record pre-existing test failures rather than repairing or deleting them during baseline collection. Step 3 explicitly requires the existing failure list; Task 0 completion does not imply a green Framework suite.

## 3. Fresh verification

All commands ran with the working directory equal to `$migrationProjectPath`.

```powershell
dotnet build ProjectL.Tests.Core.csproj --nologo

& $migrationEditor -batchmode -nographics -projectPath $migrationProjectPath `
  -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Core `
  -testResults "$migrationEvidence/core-editmode.xml" `
  -logFile "$migrationEvidence/core-editmode.log"

& $migrationEditor -batchmode -nographics -projectPath $migrationProjectPath `
  -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.Framework `
  -testResults "$migrationEvidence/framework-editmode.xml" `
  -logFile "$migrationEvidence/framework-editmode.log"
```

The actual Windows launches used `Start-Process -WindowStyle Hidden -PassThru -Wait` with these arguments and recorded the process exit codes. The suites ran sequentially. No existing Editor was closed.

| Verification | Result | Evidence |
|---|---|---|
| Core test-project build | Exit 0; 0 warnings, 0 errors | `core-build.log`, `core-build.exit.txt` |
| Unity Core EditMode | **1080 passed / 1080 total**, 0 failed, 0 skipped; exit 0 | `core-editmode.xml`, `core-editmode.log`, `core-editmode.exit.txt` |
| Unity Framework EditMode | **68 passed / 93 total**, 25 failed, 0 skipped; exit 2 | `framework-editmode.xml`, `framework-editmode.log`, `framework-editmode.exit.txt` |
| Core `RunRecoveryTests` subset | 44/44 passed, included in the 1080 above | Core XML |
| Core `MainCharacterRunProgressionTests` subset | 33/33 passed, included in the 1080 above | Core XML |
| PlayMode / real View integration | Not run | Outside Task 0 |
| Yonggeun CombatViewBuffer tests | Not run in this baseline; branch not merged | Separate input-contract review only |

Core XML execution window: `2026-09-29 13:29:27Z` to `13:29:30Z`. Framework XML execution window: `2026-09-29 13:30:30Z`. Both XML files contain the expected assembly and a nonzero executed count. Matching logs report completed runs, with no C# compile failure or test-runner abort. Framework exit 2 is a completed failing test run, not a runner infrastructure failure.

### Existing failure groups

| Group | Count | Observed first failure |
|---|---:|---|
| `ChapterGraphPreparationTests` | 20 | `TypeLoadException`: fixture looks up `ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable.StageViewPropTable`; the table moved to ViewDataBase |
| `ArtifactDataTableTests` | 1 | `[CombatStageDetail] Combat stage requires Economy.` during catalog construction |
| `DataBaseAssetTests` | 4 | Same missing Economy validation during DB asset loading |

These are observed baseline causes. The failed setup can mask further outdated fixture/data assumptions; correcting the first exception is not proof that all affected cases will pass. No failed test was skipped, weakened, or deleted.

The exact 25 test-case names and messages are preserved in `framework-failures.json` and the XML, and are listed in Appendix A below.

## 4. Current callers and state ownership

Paths prefixed `Systems/` are relative to `Assets/ProjectL/Script/`; `Core/` denotes `Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`.

| Area | Current evidence and implication |
|---|---|
| Character storage | `BufferSystems/Character/CharacterData.cs` is empty. `CharacterBuffer` exposes only Characters/Add/Clear. The only external Add callers found in Assets are `MapProgressBufferTests` and `ChapterGraphPreparationTests`. There is no implemented owned-card ID/health/soul store. |
| Run reset | `BufferSystemGroup` implements `IRunProgressBufferWriteSink.ResetRunProgress()`. `StagePlan.NewRun` registers its reset Job followed by chapter preparation. This API is not a main-health or noncombat-stage state store. |
| Current progress | `CurrentProgressBuffer` stores map and entered-node selection only. Shop consumed offers and recovery usage/receipts are not stored there yet. Task 3 must account for those fields. |
| Registered Core services | `InGameCoreSystemFactory` creates StageGeneratorSystem, StageServiceManager, and ChapterProgressionCoordinator. No registered roster, recovery, or combat processing service was found. |
| Stage entry | `StageServiceCollection.Handle` validates data. `PrepareStageEntry`/`ConfirmStageEntry` validate and record selection; current production source has no external caller completing real combat/recovery preparation through these APIs. |
| Legacy runtime | Searches across Assets, excluding Core and tests, found no callers of StageSessionFactory, StageServices, RunProgressionService, RunCharacterDeckService, or RunRecoveryService. Those classes still serve legacy/reference tests. |
| Recovery | `Core/Application/Run/RunRecoveryService.cs` reads/writes RunProgressState.CheckpointHealth, PlayerWalletState, and RecoveryStageState. Its 44 passing tests establish the legacy rules, not a migrated Buffer path. Keep one-use/payment/health preservation and exit-with-no-available-option behavior in the migration. |
| Artifact Buffer | Current/SetSnapshot APIs and copied ArtifactData are implemented. No production SetSnapshot writer was found outside the Buffer declaration. Its snapshot constructor consumes Core ArtifactState; that projection is not evidence that Buffer is already authoritative. |
| Combat execution | CombatPlan.Start and CombatJob remain scaffolding in the tested checkout. CharacterInstanceId, CombatStateBuffer, and CombatProcessingService are not yet implemented. |

## 5. Initial data and Artifact mapping gates

### G7 — Initial roster source

The DB schema already has `PlayerLoadoutData.MainCharacterKey`, ordered kind/count entries, StartingHandSize, and RandomSeed. `PlayerLoadout_1.asset` supplies four subs (counts 2/1/1), StartingHandSize 4, and seed 12345, but does not serialize the new main-character reference. No production initialization call that acquires these records into CharacterBuffer was found. This is an available schema/source candidate with incomplete authoring/wiring, not a missing reason to invent a second roster. The agreed initial main-plus-min(3, subs) rule remains unchanged.

### G9 — DB-to-Core Artifact mapping

The current path is:

```text
ArtifactDefinitionDataSO.GetData()
  -> ArtifactDefinitionData
  -> Core.Contracts.ArtifactDefinition
  -> ArtifactDefinitionTable queried by ID
```

The SO passes its existing `ArtifactEffectData[]` into the Contracts definition. The table validates construction and duplicate IDs; it does not translate legacy effect payloads into the compiler's supported semantic schemas. No production DB-to-compiled-loadout binding was found.

| Authored asset | Current payload | Compiler requirement / pending reconciliation |
|---|---|---|
| `Artifact_SoulDrop.asset` (1003) | Two ModifyStat entries, AttackPower and MaxHealth, amount 1 each, duration 0; source Any; target AllMatchingUnits/Enemy; no authored soulGrowth payload | One canonical SpawnSoul payload with Collector/Ally target, Enemy source, death position, soulGrowth fields, and duration 5. Two stat entries must not become two collectible souls. |
| `Artifact_HealingChalice.asset` (1001) | Heal, AllMatchingUnits/Ally target, amount 10, duration 0 | MainCharacter/Ally target, heal 3, duration 0. |

These mismatches were confirmed by inspecting assets, enum values, and ArtifactEffectCompiler validation. They were not rewritten and are not claimed to be production compiler-tested: catalog loading currently stops earlier on missing Economy. Canonical fixture success does not resolve G9.

Economy/Recovery/Category configuration assets and table registration are still unfinished in this baseline. CombatStageData requires an Economy reference, and EventStageData represents recovery options. Keep actual DB authoring as a separate integration dependency.

## 6. Incoming View contract and next-task boundary

Reviewed local remote-tracking ref `origin/feature/Yonggeun/Stage` at `0fef7a76cc275c3e8b5304a64ac2c7d0a7a49e6e`, including its CombatViewBuffer source, design, plan, Core requests, and Framework-applied handoff. The branch was inspected with `git show`; it was not merged into the tested checkout and no fresh remote fetch is claimed.

- Input Buffer, value types, Factory/key/group registration are implemented there. Commands and latest-only previews share a monotonically increasing int counter. Clear removes commands/previews while preserving the counter and presentation acknowledgement.
- Core receives a read interface. View owns Submit, input removal, preview clearing, and presentation acknowledgement.
- The latest branch documents separate combat Flow into a later design. The older request document's Flow ownership/schedule should not be treated as final.
- User clarification in this chat: **Core programmer owns CombatPlan and processing Jobs**, plus processing services and Core-side state/results. View owns user input and presentation. The synchronization/transition contract is shared.
- G5 is partially informed, not closed. R1-R6 still require agreed output types, read APIs, command/advance/preview entry signatures, ordered event facts, result retention, preview invalidation, and battle-boundary cleanup timing.
- A last-handled command number prevents replay of the same input; it does not by itself prevent duplicate Jobs from consuming two different queued commands. Flow admission must serialize the processing/presentation sequence.
- G1 remains open: CombatViewBuffer is an input store and does not replace the required Core-owned combat-state storage.
- StageInputBuffer and recovery/shop input are outside the incoming Buffer's scope.

Before Task 1, reflect the agreed early delivery of View-facing contracts in Tasks 1-2 and G5, with actual processing filled in later Tasks 3-7 and integrated in Task 8. That follow-up contract work is not implemented or marked complete by Task 0. DB gates do not block defining pure identity/contracts, but the 25 existing Framework failures limit current integration regression coverage.

## Appendix A — Exact baseline failures

All names below are prefixed with `ProjectL.Tests.Framework.`.

Missing legacy StageViewPropTable type (20):

1. `ChapterGraphPreparationTests.BufferReset_LeavesGraphEmptyUntilExplicitChapterPreparation`
2. `ChapterGraphPreparationTests.ChapterEntry_PreservesOtherChapterCompletionAndUnrelatedBuffers`
3. `ChapterGraphPreparationTests.Entry_GeneratesFromRealDbQueries_AndReusesBufferGraph("Nearest")`
4. `ChapterGraphPreparationTests.Entry_GeneratesFromRealDbQueries_AndReusesBufferGraph("Random")`
5. `ChapterGraphPreparationTests.Entry_GeneratesFromRealDbQueries_AndReusesBufferGraph("WeightedRandom")`
6. `ChapterGraphPreparationTests.Entry_MissingBossFailsBeforeGraphRegistrationAndCurrentSelection`
7. `ChapterGraphPreparationTests.NewRun_FromLaterChapter_ReturnsToChapterOne`
8. `ChapterGraphPreparationTests.Runtime_ChapterProgressionRecordsBossCompletionAndRejectsCompletedEntry("Nearest")`
9. `ChapterGraphPreparationTests.Runtime_ChapterProgressionRecordsBossCompletionAndRejectsCompletedEntry("Random")`
10. `ChapterGraphPreparationTests.Runtime_ChapterProgressionRecordsBossCompletionAndRejectsCompletedEntry("WeightedRandom")`
11. `ChapterGraphPreparationTests.Runtime_DefeatThenExplicitNewRun_RebuildsGraphAndPreservesArtifact`
12. `ChapterGraphPreparationTests.StageConfirmation_RechecksBufferAndRejectsAlreadyClearedTarget`
13. `ChapterGraphPreparationTests.StageDefeat_RequestsAttemptEndWithoutClearingProgress`
14. `ChapterGraphPreparationTests.StageEntry_ExplicitConfirmationRecordsSelectionAndRejectsRepeat`
15. `ChapterGraphPreparationTests.StageJobs_ResetOnlyForNewRun_AndPrepareMapWithoutSelectingNode(NewRun)`
16. `ChapterGraphPreparationTests.StageJobs_ResetOnlyForNewRun_AndPrepareMapWithoutSelectingNode(Start)`
17. `ChapterGraphPreparationTests.StageNone_DoesNotEnqueueOrChangeCurrentProgress`
18. `ChapterGraphPreparationTests.StagePreparation_InvalidDataDoesNotCommitSelection`
19. `ChapterGraphPreparationTests.StagePreparation_ValidationAloneDoesNotRecordPlayableEntry`
20. `ChapterGraphPreparationTests.StageVictory_ClearsStageAndBossAlsoClearsMap`

Missing Economy during catalog loading (5):

21. `ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
22. `DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
23. `DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
24. `DataBaseAssetTests.EveryStage_HasNodeVisual`
25. `DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`
