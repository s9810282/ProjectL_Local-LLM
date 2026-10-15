# Core Stage, Combat and Run Integration Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to execute this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. This document authorizes neither implementation outside the user's requested Core scope nor automatic commit/push.

**Goal:** Establish evidence that existing Core services compose correctly across stage selection, battle preparation, commands, settlement, cleanup and subsequent progression, without taking ownership of View/Flow execution or production data authoring.

**Architecture:** Exercise registered Systems Core services and real Buffers through the existing Framework composition and lifecycle binding. Application/Domain calculations remain pure, services assemble current facts and publish decisions, and Buffers retain authoritative state. Test code supplies deterministic inputs and invokes public service boundaries; it does not introduce a production scheduler or a second Stage/Run state owner.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, existing Framework factories and service stores, `ProjectL.Tests.Core`, `ProjectL.Tests.Framework`, `ProjectL.Tests.CombatViewBoundary`.

**Spec and existing decisions:**
- [Character/Buffer migration master plan](2026-09-29-character-buffer-effect-migration-plan.md), especially confirmed ownership and Task 8 production gates.
- [Task 8R6 Core closure and View handoff](../handoffs/2026-10-02-task8r6-core-closure-and-view-handoff.md).
- [Task 8P map ownership retirement](../handoffs/2026-10-01-task8p-map-ownership-retirement.md).
- [Artifact migration design](../specs/2026-10-02-artifact-buffer-framework-migration-design.md) and [closure handoff](../handoffs/2026-10-02-artifact-core-migration-handoff.md).

**Status:** C1-C5 completed on 2026-10-03. **Core integration verified within fixture-backed scope.** The [baseline and coverage report](../handoffs/2026-10-03-core-stage-combat-run-integration-verification.md) records C1's 1300/1305 baseline, C2's 115/115, C3's 88/88 and C4's 155/155 focused verification, and C5's final 1307/1312 result. Five Economy failures are unchanged by exact identity, full message and stack; all seven added cases pass, with no missing/skipped cases or new failures. Independent final review found no actionable issues. Authored DB readiness and production execution remain open. This is a verification work package under the existing migration ledger, not a replacement for Task 8, a new Artifact A5, or a new gameplay design.

## Global constraints

- Core scope includes registered Systems processing services, DB-to-Contracts adapters, pure rules, and their Buffer publication contracts. A test living in `ProjectL.Tests.Framework` can verify Core responsibility; assembly names do not assign implementation ownership.
- View/Flow retains `CombatLifeCycle`, `CombatFlow`, `CombatPlan`, all four combat Jobs, their service wiring and related JobTypes. Framework retains Runner fault handling. This plan does not reassign noncombat Stage/Artifact Jobs.
- Production scenes, prefabs, ScriptableObject assets, balance values, UI, input scheduling, acknowledgements and transitions are outside this work package. In-memory test definitions are permitted and must be labeled as fixtures.
- Use the existing Factory -> InitData/PostBindService -> exported service route. Keep one physical Buffer per state domain and existing narrow permissions. Do not add session managers, state mirrors, generic transaction layers, broad reset access or test-only production APIs.
- Core does not submit, remove, clear or acknowledge View inputs. A test may act as an external input producer/consumer through existing public ports, with that simulated responsibility explicitly identified.
- Preserve the Artifact rule that its Factory knows the concrete calculator and its service consumes the Contracts calculation port. Do not turn this verification into an unrelated constructor/API migration for other services.
- Reuse existing tests before adding any. Add a test only for a demonstrable missing contract or an uncovered interaction. Fix a proven Core defect minimally; policy changes and non-Core failures become owner-specific follow-ups.
- New Run/restart, death reset, save/load, crash recovery and final Artifact reset authority are not defined here. Existing explicit NewRun behavior may run as regression coverage; it is not authorization to extend that behavior.
- Documents are English; any new code comments are Korean. Preserve incoming working-tree changes and Unity asset GUIDs.

## Review focus

1. Invalid stage/preparation data must not commit selection or partly install a battle (C2).
2. Rejected commands, previews and repeated processing must not change gameplay state or apply costs/effects twice (C3).
3. Terminal health, outcome and stage/map progress must agree, including simultaneous main death and enemy headquarters destruction (C4).
4. Cleanup and next preparation must preserve persistent ownership/HP/souls/Artifact state and counters while removing battle-local state (C4).
5. Fixture success must not hide missing production assets, unhandled old input, missing presentation acknowledgement or absent Job/Runner wiring (C1, C4, C5).

## Evidence boundaries and completion labels

| Level | Evidence | Can this plan close it? |
| --- | --- | --- |
| Core service integration | Real exported services, current Buffer reads/writes, pure calculations, deterministic external facts | Yes |
| DB adapter integration | Existing real adapter/table code supplied with in-memory valid/invalid definitions | Yes, explicitly fixture-backed |
| Authored DB readiness | Actual project assets and references load correctly | Observe/report; DB supplies missing authoring |
| Execution integration | Input -> Flow -> Plan -> Runner -> Jobs -> services, fault termination and exit ordering | No; View/Flow + Framework |
| Playable Stage/Run completion | Actual scene entry, result display/acknowledgement, transition, final Run UI and agreed restart behavior | No; joint acceptance |

Use `Verified`, `Missing coverage`, `Core defect`, `External dependency` or `Deferred policy` for each scenario. An external dependency is never a pass. Direct service calls do not prove production Job timing.

## Source and test map

All paths in this document are repository-relative. Prefixes below are path abbreviations, not new folders:

- `CoreRoot` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem`
- `BufferRoot` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`
- `Tests` = `Assets/Tests/EditMode/Framework`

| Boundary | Existing sources to inspect; change only for a demonstrated Core defect | Existing test files to reuse |
| --- | --- | --- |
| Composition and bootstrap | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`; `Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs`; `CoreRoot/InGameCoreSystemGroup.cs`; `CoreRoot/CharacterSystem/CharacterRosterService.cs` | `Tests/CharacterRosterServiceTests.cs`; `Tests/ArtifactEditingServiceTests.cs` |
| Chapter/selection | `CoreRoot/ChapterProgressionCoordinator.cs`; `CoreRoot/StageSystem/StageGeneratorSystem.cs` | `Tests/ChapterGraphPreparationTests.cs`; `Tests/ChapterGraphGenerationTests.cs` |
| DB/preparation | `CoreRoot/CombatSystem/DatabaseBattleInitializationSource.cs`; `CoreRoot/CombatSystem/BattleDefinitionAdapter.cs`; `CoreRoot/CombatSystem/CombatProcessingService.cs` | `Tests/BattleDefinitionAdapterTests.cs`; `Tests/CharacterBattlePreparationTests.cs` |
| Command/preview/publication | `CoreRoot/CombatSystem/CombatProcessingService.cs`; `BufferRoot/Core/BufferSystemGroup.cs` | `Tests/CombatCommandPublicationTests.cs`; `Tests/CombatPreviewPublicationTests.cs`; `Tests/CombatCommonPublicationTests.cs` |
| Settlement/retention | Same combat service and Buffer group; `CoreRoot/ArtifactSystem/ArtifactEditingService.cs` | `Tests/CharacterBattleSettlementTests.cs`; `Tests/CharacterRosterBattleCycleTests.cs`; `Tests/CombatStateBufferTests.cs`; `Tests/MapProgressBufferTests.cs`; `Tests/ArtifactEditingServiceTests.cs` |

Create one evidence report during execution: `docs/superpowers/handoffs/2026-10-03-core-stage-combat-run-integration-verification.md`. Its scenario table must include the exact test method, real components exercised, substituted inputs, result, and remaining dependency. Prefer extending the listed fixtures. Create `Tests/CoreStageCombatCycleTests.cs` and its Unity `.meta` only if C1 finds that an actual cross-service cycle cannot be expressed clearly in an existing fixture. Keep setup local; do not build a reusable simulation framework.

## C1: Establish baseline and identify actual coverage gaps

**Files:** Read the source/test map and linked handoffs. Create the evidence report; no production modification.

**Interfaces:** Consume existing assembly XML results and source contracts. Produce the scenario-to-test matrix used by C2-C4, with explicit gaps instead of a duplicate test suite.

- [x] Record branch, HEAD, working-tree status, Unity version and incoming modified file hashes. The planning checkout already contains unrelated audio asset and `CombatProcessingService.cs` changes; do not discard or silently attribute them to this work.
- [x] Run the baseline command below, record exit code, XML counts and full failure names/messages, and compare with the historical baseline rather than assuming it still applies.
- [x] Map the five review-focus items and C2-C4 scenarios to current test methods. Read their setup: a directly constructed service, fake progression coordinator or direct Buffer write must not be described as a fully registered service chain.
- [x] Identify at least one real factory-bound preparation -> command -> terminal publication -> cleanup -> next preparation path, or mark precisely which link lacks coverage. Reuse existing coverage where it already provides that evidence.
- [x] Record external blockers separately. Historical evidence is 1300/1305 passing, five Economy authoring failures and zero skipped cases; these numbers are reference data, not the expected fixed size of a future suite.

**Acceptance:** Every later scenario has either a current witness or a concrete missing assertion/component boundary. No runtime redesign is proposed just because tests are organized across multiple files.

## C2: Verify registration, data preparation and stage entry

**Files:** Existing composition, chapter and preparation files/tests in the source map. Extend those tests only for C1 gaps.

**Interfaces:** Use `ICharacterRosterService.InitializeRoster(int loadoutKey)` -> `RosterAcquireResult`; `IChapterProgressionCoordinator.EnterChapter(int chapterKey)`, `PrepareStageEntry(int chapterKey, StageNodeId targetNodeId)`, `ConfirmStageEntry(int chapterKey, StageNodeId targetNodeId)` -> `ChapterProgressionResult`; `ICombatProcessingService.PrepareBattle()` -> `void`. Artifact bootstrap uses `InitializeBoard(int width, int height)` -> `bool`.

**C1 clarification:** Default-source positive preparation is currently blocked by the intentionally absent Method_Key attack-profile query/schema. Preserve the passing fail-before-write witness. Use the existing injected initialization-source seam for successful registered-service cycle tests, label those as fixture-backed, and leave actual default-source success as an external dependency until approved DB support arrives. Do not create a fallback or redesign the DB to satisfy this verification task.

- [x] Verify factory export and InitData/PostBindService binding with real Buffer and Core groups, following `ChapterGraphPreparationTests.Prepare` without its optional Job runtime. Reuse default `DatabaseBattleInitializationSource` rejection coverage with the existing typed in-memory definition-query fixture; existing chapter fixtures exercise real in-memory DB tables. Label injected `IBattleInitializationSource` cases as fixture-backed service coverage.
- [x] Verify chapter generation/re-entry preserves the existing graph and previous completion. Verify preparation validation alone does not change Current selection; confirmation rechecks current state and rejects an invalid or already-cleared target.
- [x] Verify missing/invalid required preparation data and an already-retained battle fail without partial character/combat installation. Where stage preparation fails, the simulated caller must not confirm entry. Record this caller ordering as an execution obligation, not an atomic guarantee spanning all service calls.
- [x] Verify wounded-main state, owned roster and souls come from Buffers; preparation does not reacquire the starting roster or initialize a wallet on every battle. Treat initial wallet seeding and production loadout/caller timing as external bootstrap decisions; do not invent values.
- [x] Reuse Artifact binding-without-grant and initialize-once tests. Confirm a second service instance/rebind cannot grant inventory again for the same Buffer lifetime.
- [x] Run the C2 fixtures and record exact witnesses. Candidate names only if missing: `RegisteredPreparation_InvalidDefinitionPreservesCharacterAndCombatState` and `StageEntry_PreparationFailureLeavesSelectionUnconfirmed`; assert before/after snapshots and actual returned rejection/exception, not private call counts.

**C2 evidence:** Two methods (three cases) were added to `BattleDefinitionAdapterTests`; six fixture classes passed 115/115 with exit 0. `ChapterGraphGenerationTests.cs` is a partial file of `ChapterGraphPreparationTests`, whose 40 cases include generation coverage. No production code changed. The first run exposed a missing map-query member in the test double; after fixture repair all cases passed. This is not evidence of a production bug fix. The successful preparation tests explicitly bypass the default source's pending Method_Key gate, and their repeated-preparation setup uses direct Buffer cleanup; production entry ordering and C4 service cleanup remain open.

**Acceptance:** Real registration and fixture-backed data consumption are proven. Actual SO availability and production entry ordering remain separately labeled.

## C3: Verify commands, previews and publication boundaries

**Files:** Command/preview/publication sources and tests from the source map.

**Interfaces:** `ICombatProcessingService.ProcessNextCommand()`, `ProcessLatestPreview()` and `AdvanceCombat()` return `bool`; the authoritative accepted/rejected outcome is read from Buffers. `true` from command processing means handled, including a normal rejection; `false` means no eligible command/work, not a retry instruction.

- [x] Exercise an accepted command and a normal rejection through the registered service with real Buffers. For rejection, gameplay values remain unchanged while permitted handling/result metadata advances. Core must leave input removal and acknowledgement untouched.
- [x] Repeat processing of an already-handled command. Assert no second cost, health/soul effect, settlement or result publication. Exercise no-input/no-eligible-work behavior separately from rejection and exceptions.
- [x] Exercise preview and automatic advancement via existing fixtures. Preview may change display output but must preserve HP/AP, currency, occupancy, phase and terminal completion. Do not infer production hover freshness from a direct service test.
- [x] Retain malformed/stale proposal and counter-overflow witnesses proving that preparation fails before any participating gameplay store installs. Use real Buffer publication for those assertions; a throwing fake sink alone does not establish Buffer atomicity.
- [x] Verify existing manual-draw/currency scenarios using explicit fixture balances and costs. Reuse recovery/shop cost regressions where relevant; do not broaden this task into full noncombat stage execution.
- [x] Run the C3 fixtures. Close registration-to-publication coverage by strengthening the existing draw/rejection/preview/advance witnesses in `CombatCommonPublicationTests`, checking gameplay snapshots, currency and result/handled numbers before and after repeated calls; a duplicate named test is unnecessary.

**C3 evidence:** The existing common-command fixture now exports the real Core factory, resolves its registered group, and uses real roster acquisition/draw planning through public service interfaces. Six fixture classes passed 88/88 with exit 0; no cases were added/removed and no production code changed. The first run exposed an overstrict new test assertion about `CharacterData` reference identity, corrected to gameplay-value comparisons. Explicit initialization definitions, initial main/graph setup and test-driven input remain fixtures. Actual Jobs/Runner/Flow and terminal-to-next-battle service lifetime remain outside C3 completion.

**Acceptance:** Outcomes and publication are consistent across actual service/Buffer calls. No new scheduling, command queue, Buffer business rule or input acknowledgement is introduced.

## C4: Verify terminal settlement, cleanup and subsequent progression

**Files:** Settlement/retention sources and tests from the source map; conditional `Tests/CoreStageCombatCycleTests.cs` for a missing cross-service witness only.

**Interfaces:** `ResolveBattle(ArtifactLoadoutSnapshot loadout)` and `AbandonBattle(ArtifactLoadoutSnapshot loadout)` -> `bool`; `EndBattle()` -> `void`; `PlanStageResolution(int chapterKey, StageOutcome outcome)` and `ResolveStage(int chapterKey, StageOutcome outcome)` -> `ChapterProgressionResult`. Combat terminal publication must use its existing combined Buffer boundary, not an extra `ResolveStage` call that repeats settlement.

- [x] Verify normal victory, boss victory, main-death defeat and voluntary abandonment. Victory clears the appropriate stage; boss victory also clears the map; defeat does not fabricate a clear. Assert terminal health/outcome/progress together, including the existing main-death precedence case.
- [x] Verify duplicate completion/abandonment applies no second Chalice heal or other settlement. Use existing real Artifact-service-to-combat fixtures for supported effects; authored production effect acceptance remains external.
- [x] Verify `EndBattle` rejects an unfinished retained battle without cleanup. After terminal settlement, cleanup removes active battle entities, links, field/preview and temporary state while preserving owned roster, main persistent values, souls, Artifact state and applicable progress/currency.
- [x] Pin current result retention: `EndBattle` keeps completion, latest result and numbering; next preparation clears completion/preview but retains latest result and numbering. The service does not enforce presentation acknowledgement. Do not add an acknowledgement gate to Core to compensate for unfinished Flow integration.
- [x] Follow a legal subsequent stage/chapter path through the real progression service, then prepare the next battle. Assert no repeated initial acquisition, soul bonus accumulation, settlement replay or unintended Artifact reset. For a boss-to-next-chapter scenario, supply both chapters as fixture data. Do not infer a new Run policy from a defeat case.
- [x] In that cycle, explicitly simulate the external owner's old-input cleanup through the existing View port before new-battle input. Assert handled commands do not replay. Separately record that unhandled old input requires production lifecycle cleanup; do not claim Core alone prevents its replay.
- [x] Reuse existing tests, or add the missing `RegisteredCycle_VictoryCleanupAndNextPreparationPreservePersistentState` / `RegisteredCycle_AbandonmentSettlesOnceBeforeCleanup` witnesses. Capture state after every public boundary; do not seed the expected terminal result with direct Buffer writes in place of the service under test.
- [x] Run C4 fixtures together with C2/C3 changed fixtures and record results.

**C4 evidence:** The conditional `CoreStageCombatCycleTests.cs` adds three methods/four cases for the missing registered service lifetime and priority boundary. Ten fixture classes passed 155/155 with exit 0; all 151 existing case names are retained. The victory chain uses actual preparation, commands, real progression, service cleanup and legal next-stage/chapter entry. The priority case supplies dead/excluded main facts before a real sub attack destroys the headquarters; it proves competing terminal facts resolve to defeat, not production scheduling of simultaneous damage. Explicit typed DB definitions, graph edges, initialization profiles and external input cleanup remain fixtures. Existing nonzero temporary-buff and multi-Chalice regressions also passed. Test setup corrections did not change production behavior or Buffer validation. C5 final regression and production execution gates remain open.

**Acceptance:** The Core portion of a multi-stage cycle is proven with explicit fixture and caller assumptions. Final Run screen, transition timing, restart/reset and save semantics remain outside this completion statement.

## C5: Final regression, responsibility audit and Core handoff

**Files:** Evidence report and this checklist. Production/test edits are limited to earlier verified gaps or defects.

- [x] Audit the final diff: no View/Flow/Job implementation, production asset authoring, changed assembly boundaries, new Buffer gameplay responsibility or persistent service state has entered the scope. Review any Buffer fix against existing validation/publication responsibility.
- [x] Run all three EditMode assemblies once after the final change. Inspect the XML, logs and process exit code; a test process finishing is not proof of success. Investigate every new failure or unexpected missing/skipped test.
- [x] Compare known asset failures by exact test identity and full message. Report unchanged failures as unresolved DB readiness, not an all-green suite. Any changed failure needs investigation; never suppress validation or author placeholder production Economy assets to make this package pass.
- [x] Record real components, fixtures, new/fixed test witnesses, command/log/XML locations, current working-tree attribution and remaining gaps in the handoff. All Core-scoped failures must be resolved or explicitly leave Core verification incomplete.
- [x] Mark only proven checkboxes. Declare `Core integration verified within fixture-backed scope` only when C1-C4 evidence and applicable regressions pass with no new failures. Keep authored DB, execution integration and playable completion open.

**C5 evidence:** Unity 6000.0.73f1 ran the three requested assemblies after the final C4 test changes: Core 918/918, Framework 383/388 and CombatViewBoundary 6/6. Exit 2 reflects the same five unresolved Economy authoring failures. Exact case-set comparison retains all 1305 baseline cases and adds only the seven documented C2/C4 cases; all 54 named C1 witnesses (69 cases) remain passing. No production/test code changed during C5. All 32 protected incoming hashes were preserved, and an independent final review found no critical, important or minor actionable issue. The evidence directory is retained; no commit/push or merge was performed.

## Verification commands and defect workflow

Use the project-matched editor. Store local evidence outside Assets. Use distinct `baseline`, `c2`, `c3`, `c4` and `final` run names to preserve earlier evidence. Run only one Unity test process against this checkout at a time; if the Editor already owns it, use the established project test route rather than force-closing it.

```powershell
$evidenceDir = Join-Path (Get-Location) '.superpowers/sdd/2026-10-03-core-stage-combat-run-integration-verification-plan'
New-Item -ItemType Directory -Force -Path $evidenceDir | Out-Null
$runName = 'baseline'
$unityArgs = @('-batchmode', '-nographics', '-projectPath', (Get-Location).Path,
    '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
    '-testResults', (Join-Path $evidenceDir "$runName.xml"),
    '-logFile', (Join-Path $evidenceDir "$runName.log"))
& 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' @unityArgs
$LASTEXITCODE | Set-Content (Join-Path $evidenceDir "$runName.exit-code.txt")
```

For focused runs, use `-assemblyNames 'ProjectL.Tests.Framework'` and `-testFilter` with the semicolon-separated fully qualified fixture names from the task, such as `ProjectL.Tests.Framework.CharacterBattlePreparationTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests`. Include the conditional cycle fixture only if created. Confirm the XML contains the requested tests; zero discovered tests is a failure of the run.

For a missing witness, first run the new test against existing code. Passing is valid evidence and does not justify a production edit. For a discovered defect, capture the failing assertion, repair the smallest responsible Core boundary, then rerun that test and affected fixtures before the final suite. Do not alter gameplay policy to satisfy a speculative test. Preserve exact pre-run bytes for importer-touched unrelated assets and restore only proven incidental changes after checking for concurrent edits. Run `git diff --check` and inspect untracked additions before handoff. Commit/push only on user request.

## External dependencies to hand off, not implement here

| Dependency | Owner | Required future evidence |
| --- | --- | --- |
| Actual stage/Main/unit/Economy definitions and references; approved effect profiles | DB/designer, then Core adapter verification | Actual catalog and default preparation pass; historical five Economy failures resolved by real authoring |
| Production roster/wallet and Artifact bootstrap caller, dimensions and timing | Existing execution owner with DB/designer inputs | Startup occurs at agreed lifetime; no battle-entry reacquisition/reset |
| Remaining combat Job service wiring and real registration path | View/Flow | Register -> Plan -> Runner -> service -> Buffer with accepted/rejected/preview/automatic results |
| F1 fault termination | Framework | A faulted Job cannot execute again on a later runtime tick |
| F2 normal exit/voluntary defeat/result acknowledgement/cleanup/transition order | View/Flow with Framework and Core API support | Input and registration stop; outstanding Jobs finish; final result is presented/acknowledged; cleanup precedes transition |
| Artifact E3 intent/preview/result/popup lifetime and activation | Assigned execution/View owner | Actual input and output lifetime through existing Job/LifeCycle mechanisms |
| Actual playable Stage/Run closure and new-Run policy | Joint owner/design agreement | PlayMode entry, battle, next stage/chapter and end behavior; separately approved restart/reset semantics |

The deliverable is a Core evidence package and precise remaining integration obligations. It must not be presented as complete production Stage/Run gameplay.
