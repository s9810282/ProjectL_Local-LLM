# Core Test Coverage Consolidation Implementation Plan

**Current execution status (2026-10-26): Tasks 1–4 complete.** Each task was separately authorized by the user. The planning-only and stop notices below are historical checkpoints; the final Task 4 record is authoritative for completion. Category/trait implementation is outside this completed Phase.

> **For agentic workers:** Use `superpowers:executing-plans` for inline execution, or `superpowers:subagent-driven-development` if delegated execution is selected. Track checkboxes. This delivery is planning only; implementation is not authorized.

**Goal:** Remove obsolete/duplicated checks and close the remaining Core–Buffer foundation gaps using current tests and valid fixture lifecycles.

**Architecture:** Keep `ProjectL.Tests.Core` pure and fast. Move selected Core service/Buffer checks into a new independent `ProjectL.Tests.CoreIntegration` assembly with test-local in-memory fixtures. Consolidate overlaps there. Production behavior and state ownership stay unchanged; the new test asmdef and one Systems friend declaration are the explicit assembly-boundary exception.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, existing Core services/Buffer transactions.

**Spec:** [Coverage-first design](../specs/2026-10-26-core-test-foundation-recovery-design.md). **Disposition:** [Exact case decisions](2026-10-26-core-test-foundation-disposition.md), which is part of this plan. Inspected HEAD: `1ba7aec4728e1dfa5199a5f6043cce10dde636b5`.

**Revision / immediate handoff:** Stop the original Task 1 session before proceeding under this revision. Do not finish the old Framework-folder extraction first. Preserve its current changes and recorded baseline; reuse the extracted helper where suitable. Old Task 1 evidence is historical and does not complete the revised assembly-separation gate. Resume only against this revised Task 1 and the user's actual execution authorization. This document edit does not itself stop another running session.

## Global constraints

- One bounded Phase, Tasks 1–4 sequentially, then report and stop. No implementation of category/trait changes, production fixes, commit or push.
- Edit only named tests/helpers, the new test asmdef/metas, documents, and `Assets/ProjectL/Script/Systems/AssemblyInfo.cs` to add `InternalsVisibleTo("ProjectL.Tests.CoreIntegration")`. Keep existing friends. No production behavior, SO, scene, prefab, Factory, Job, ServiceKey or existing asmdef changes. English documents; Korean comments; matching `.meta` for new/deleted assets and GUID preservation for whole-file moves.
- Keep new pure-rule and movement SO coverage. Do not restore unit initiative, per-unit AP, checkpoint return, disabled railway or other deferred effects.
- No global `Available -> Deferred` replacement, preparation auto-normalization, counter resets, fake accepted Attack transitions, skips or assembly exclusion. Keep intentional invalid-input tests invalid at the intended boundary.
- The disposition file selects methods/parameter cases, not entire legacy classes. All unlisted methods remain untouched/deferred unless a necessary helper dependency is recorded and reviewed. Do not aim to clear the old 99/89 failure totals.
- Never reference an existing test assembly from CoreIntegration. Do not move all Framework tests, disable their discovery, or broaden `ProjectL.Tests.Core` dependencies. Normal Core verification runs Core and CoreIntegration only; it does not run the View/Framework suite.

## Review focus

| Risk | Required check |
|---|---|
| Rejected Move coverage is mistaken for accepted Attack or direct Buffer replay coverage | Task 2 separates accepted-service replay, rejected-service replay and direct transaction replay |
| Setup results invalidate absolute `1`/`0` counters | Tasks 2–3 capture post-setup numbers and assert deltas; commands use the next actual number |
| A stale-source test fails at unrelated identity/settlement validation | Task 3 proves a valid control and the intended failing mutation |
| One constructed night baseline is mistaken for two live nights | Task 2 executes the real second EnterCombat/owner start and the eighth enemy response |
| Removing a parameter deletes its still-valid sibling | Task 2 removes only the two `timeline=true` cases; invalid-field cases remain deferred |
| A new folder still imports old test fixtures or runs slow scene tests | Task 1 audits references/helper closure and confirms assembly-filtered discovery; Task 4 records durations |

## Task 1 (revised): Separate the CoreIntegration assembly and migrate the working foundation

**Paths:** `F = Assets/Tests/EditMode/Framework/`; `I = Assets/Tests/EditMode/CoreIntegration/`; pure tests remain under `Assets/Tests/EditMode/` with their existing asmdef. Create `I/ProjectL.Tests.CoreIntegration.asmdef`, folder/asset metas, `I/CoreIntegrationTestData.cs`, and `I/MovementPublicationTests.cs`. Move the existing working-tree `F/ConfiguredMovementBattle.cs` and `.meta` to `I/`, preserving its GUID and useful probe coverage. Remove only the migrated K3/K4 methods and their exclusively used local helper from `F/MovementPublicationTests.cs`; other cases stay there. Add the one Systems friend declaration stated above.

**Assembly:** name/root namespace `ProjectL.Tests.CoreIntegration`; direct references `Framework`, `ProjectL.Systems`, `ProjectL.Core.Contracts`, `ProjectL.Core.Application`; Editor-only, `autoReferenced: false`, `overrideReferences: true`, `precompiledReferences: ["nunit.framework.dll"]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`, `noEngineReferences: false`. UnityEditor/UnityEngine are needed by the existing in-memory SO checks. No references to `ProjectL.Tests.Framework`, `ProjectL.Tests.Core`, View test assemblies, TMP/UI, Cinemachine, FMOD, or rendering test packages. Systems retains its existing transitive dependencies; this separates test execution, not all project compilation. Do not widen Framework/Core internals access without an identified necessary use and a separate scope decision.

**Fixture closure:** current `ConfiguredMovementBattle` references `BattleDefinitionAdapterTests.DefinitionDatabase`, `CharacterRosterDatabase` indirectly, `CharacterBufferTestData.InstallFixtureDeck`, and its probe references `BattleDefinitionAdapterTests.DatabaseSource`. Replace these links with minimal in-memory DTO/query/deck setup in `CoreIntegrationTestData`, using existing production contracts and entry services. Preserve the selected encounter values; do not copy whole test classes, load project assets/scenes, initialize View/render systems, or add a shared test framework. No duplicated production rules or runtime state store. A test-local data definition is not a new gameplay configuration authority.

**Fixture interface:** retain existing `Buffers`, `Core`, `Actor`, `Main`, `OwnedStats`, `Budget`, `Character(EntityKey)`, `PreviewMove(...)`, `Move(...)`, `Submit(...)`, and `Dispose()`. Constructor becomes `ConfiguredMovementBattle(CategoryConfigData config, IBattleInitializationSource battleSource = null, bool startPlayerTurn = true)`. Pass the optional source through the existing `InGameCoreSystemFactory.Create(battleSource)` seam. Overrides retain this fixture's board/roster/deployment positions and adjust only encounter facts needed by the test. Default behavior stays identical; false stops after EnterCombat at StartingPlayer. Add only `void AdvanceToPlayerOrDay(int maximumSteps = 256)` for real automatic steps, failing with phase/result evidence on exhaustion and stopping on Finished. This bound is a fixture watchdog, not a game scheduling rule.

- [x] Reconcile the stopped old Task 1's files and baseline with current HEAD; preserve unrelated/concurrent work. Do not restart its full Framework run. Reuse matching evidence with its date/scope recorded; refresh only the focused baseline if source drift requires it.
- [x] Create the new asmdef/friend boundary, move the helper, and migrate K3/K4 to `ProjectL.Tests.CoreIntegration.MovementPublicationTests`. Preserve all four parameter-level cases and existing meaningful Task 1 probe assertions; record old/new fully qualified names. Whole-file moves retain GUIDs; split files receive new metas.
- [x] Replace every old test-fixture reference with the minimum CoreIntegration-local setup. Keep the existing source/start/watchdog interface above; remove migration-probe coupling to old classes. Do not add CoreIntegration -> old test references to get compilation working.
- [x] Compile the project and run only the new CoreIntegration assembly plus pure Core. Require the migrated cases to pass, test discovery to contain no old View/Framework tests, and no new dependency on scenes/assets/render initialization. Record per-test and total execution time separately from compile/import time.
- [x] Report this revised Task 1 boundary and stop if execution was authorized for Task 1 only. Its completion means assembly separation and working migrated coverage, not completion of Tasks 2–4.

Do not modify the behavior of old shared preparation factories. Selected tests stop relying on those factories; their unrelated consumers are neither moved nor repaired. Existing Core/Framework production definitions remain the real code under test. The previous full Framework baseline crash is preserved as evidence, not a reason to retry rendering tests as part of this gate.

## Task 2: Consolidate command and controller coverage; perform bounded deletions

**Destinations:** `I/CombatCommandServiceTests.cs`, `I/CombatAdvanceServiceTests.cs`, `I/MovementPublicationTests.cs`, and the migrated helper. Move the command/advance class files with their metas if dependency inspection confirms no deferred methods remain; otherwise split selected methods. **Source-only removals:** named D cases in `F/CombatCommandPublicationTests.cs`, `F/CoreStageCombatCycleTests.cs`. Follow D1–D8/C1–C4 and the disposition's destination table; no new checks are added under F.

- [x] Reuse the four configured-movement cases unchanged as existing coverage. Extend `ConfiguredMovement_RejectedRequestPreservesWorldAndPublishesOneRejection` to assert queued input retention and unchanged `PresentedResultSequence`; it already checks stored facts, one rejection result and no second processing. Only after it passes, remove its mapped redundant rejection-service case D5.
- [x] Replace the old accepted-command bookkeeping case with `AcceptedAttack_PublishesAndSpendsExactlyOnce`: a normal source places a legal nonlethal target in range before entry; the real command spends the actor's configured attack price, installs `Attacked`, publishes once, and repeated processing changes no gameplay/result counters. Preserve the queued input and presentation-ack separation.
- [x] Replace retired actor inference with `EndPlayerTurn_PublishesActorFreeBoundaryOnce`: result actor is Empty, completed-player count rises once, AP is unchanged, controller becomes StartingEnemy, and stale-turn resubmission cannot end a later player turn. Reuse existing pure tests for other rule permutations.
- [x] Retain the no-input/no-battle and Run-ended service guards using the current helper. No-input checks snapshot all game stores and counters. This becomes the owner of D4's duplicate guarantee; delete D4 only after that control passes.
- [x] Retain the three advance-service guarantees with valid setup; rename `Automatic_AdvancesActiveTurnWithoutReadingViewCommands` to `Automatic_StartsControllerWithoutConsumingViewCommands`. Add `EnemyAction_UsesNoPlayerBudgetAndPreservesPendingInput` with one legal ordinary enemy action and assert published actor/action use, unchanged AP and unchanged View handled/ack numbers.
- [x] Add `TwoNights_ExpireSuppliesAfterEnemyResponseAndPreserveBadge` to I's advance tests: use a nonterminal encounter with no actionable enemies; grant supported supplies/badge through existing transactions. Across two actual nights, assert no cleanup at player end 8, one NightEnded after enemy completion, supplies cleared, badge retained, Main still on field, and reset night-local count after actual EnterCombat. Add `TerminalAction_PreventsFurtherControllerPublication`. Remove D6's old method only after replacement passes. Remove exclusively unused local helpers only after a reference check; do not rewrite the shared old CycleSource for the new tests.
- [x] Delete D1's obsolete method and only D2/D3's `timeline=true` attributes. Do not delete their `false` cases, revive timelines, or repair all neighboring tests. Keep the retirement ledger distinct from replacement-dependent removals.

## Task 3: Recover only unique transaction and terminal-publication guarantees

**Destinations:** `I/CombatCommandPublicationTests.cs` (R1), `I/CombatRewardPublicationTests.cs` (R2), `I/CombatTransitionPublisherTests.cs` (R3), `I/CombatCompletionPublicationTests.cs` (R4), `I/CombatItemPublicationTests.cs` (R5); add `I/CoreIntegrationSnapshot.cs` for the selected all-or-none assertions. Whole-file R3 movement may preserve its meta; split mixed R1/R2/R4/R5 files with new metas, retaining deferred methods in F. Never import `ThornHeartReflectionPublicationTests.Snapshot`, `CombatRequestTestParts` or `CycleDatabase/CycleSource` from the old test assembly.

**Setup interface:** construct the existing production `CombatInputAssembler`, `BattleProgressionPlanner` and `CombatTransitionPublisher` through small I-local helpers around the valid context; bind them through the existing reader. Supply the exact captured definition and explicit progression spy for these tests. `CoreIntegrationSnapshot(IBufferQuery buffers)` / `AssertUnchanged(IBufferQuery buffers)` capture the real membership Main ID, selected stage/Run identity, stores and publication counters without old test extensions or fixed IDs. Adapt the necessary assertions from the old snapshot, not its entire owning class. Keep old shared Input/Create helpers unchanged for deferred tests.

- [x] Migrate R1's six owned-state parameter cases and direct rejection replay. For accepted proposals, calculate a valid ordinary action first, preserve its identity/settlement, and vary only the expected owned level/experience, owned-change proposal or soul field. Include a passing control for every negative family. A lower-level owned-state test may use its real narrow transaction only if it does not claim command-publication coverage.
- [x] Migrate R2's selected reward cases using an explicit valid reward encounter. Two kills must occur through different eligible actors or actual successive owner turns. Preserve immediate gold, final XP, no replay/cleanup double payment, Run kill count and unchanged RunSpent. Exercise all seven invalid-publication variants; snapshot Character, Soul, Inventory, Currency, Fame, Combat/Run completion and result bookkeeping. Keep other reflection/roar/chalice cases deferred.
- [x] Migrate R3's five publisher cases with valid nonterminal/terminal calculator results and its existing progression spy. Verify no resettlement, planner call counts, rejection and no publication on RunEnded. Do not synthesize an accepted Attack without action settlement.
- [x] Port R4 into `I/CombatCompletionPublicationTests` with its own minimal in-memory stage lookup callback: Command uses a legal lethal attack; Advance performs an ordinary lethal enemy attack from the actual Enemy phase; preserve Abandon. Require exactly one lookup, actual Run termination and no combat publication. Do not copy or modify the old mixed View fixture/railway branch to implement this encounter. Delete the old selected method after the new cases pass; defer unused shared-fixture cleanup.
- [x] Migrate only R5's four item-source rejection cases, adding an unchanged-source passing control. Use a supported starting item on an ordinary enemy in an explicit source and reach its real Enemy phase; preserve item inventory/slot/effect/RNG checks. Do not reuse the old Rope/Frenzy setup or re-enable Rope. Snapshots include candidate character/item changes and result numbers, proving the intended stale item fact prevents installation.
- [x] Run each selected family after setup changes. If a new failure exposes production behavior, keep the failing evidence and stop for a separate decision; do not weaken validation or expand into unrelated gameplay fixes.

## Task 4: Verify the guarantee map and stop

- [x] Reconcile passing evidence for every replacement-dependent deletion with old/new fully qualified case identities. Compare parameter-level discovered counts with exact relocations/removals/additions; no unexplained dropped or skipped cases.
- [x] Run full Core and full CoreIntegration. Do not run full Framework/View as a default gate. Old shared helper behavior is unchanged; targeted old source checks are limited to moved/deleted method discovery and any demonstrated affected retained consumer. If an unrelated dependency cannot be separated without changing old shared behavior, report that dependency before expanding scope. Preserve existing failed/crashed broad-run evidence; no rendering rerun is required by this plan.
- [x] Verify existing configured movement, preparation/owner-start facts, accepted/rejected command handling, enemy funding, two live nights and cross-store rejection guarantees have an executed owner. Keep existing pure `PlayerTurn*`, `EnemyControllerPhaseTests`, `EightTurnNightTests`, `DeferredCombatFeatureTests`, and migrated `CharacterActionCostRulesTests` unchanged unless a demonstrated dependency requires a recorded adjustment.
- [x] Audit `git diff --check`, C#/.meta pairs/GUIDs, relocation/deletion ledger, production hashes (only the declared Systems friend-line exception), and asmdef boundaries. Search I for `ProjectL.Tests.Framework`, old fixture class names, Scene/Play/render setup and old test references. Record any slow test with its measured cause; do not silently skip or weaken it. Report Core and CoreIntegration counts/timings separately, exclude View test execution, and stop; no commit/push.

## Verification commands (later execution only)

Use the approved AppData-capable Unity path. Do not upgrade Unity or delete caches. From the repository root:

```powershell
$evidence = Join-Path (Get-Location) '.superpowers/core-test-foundation'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null
Start-Process 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -WindowStyle Hidden -Wait -PassThru -ArgumentList @(
    '-batchmode', '-nographics', '-projectPath', 'D:/My_ProjectWork/Github_desktop/Project_L',
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'ProjectL.Tests.Core',
    '-testResults', "$evidence/core-baseline.xml", '-logFile', "$evidence/core-baseline.log")
```

Run the same template separately with `-assemblyNames ProjectL.Tests.CoreIntegration` and integration-specific XML/log names. Focused development filters use `ProjectL.Tests.CoreIntegration.<Class>.<Method>`. Original `ProjectL.Tests.Framework.*` IDs remain in historical baseline/relocation records only; no full Framework run is a routine requirement. Reconcile relocated names and added/deleted cases, and require both selected assemblies to complete with XML and passing required cases. Unity still compiles project sources first; assembly separation reduces executed test scope and does not promise removal of all compile/import cost.

This revision changes documents only. Existing original-Task-1 code/results were produced in another authorized session and are preserved; they do not prove revised Task 1 complete. Stop that original task, preserve its working tree/evidence, and resume only against the revised boundary when authorized. No test execution, asmdef, friend declaration or source relocation was performed by this document revision.

## Revised Task 1 completion record (2026-10-26)

User-authorized revised Task 1 is complete. CoreIntegration: 5/5 passed (0.5744511 s NUnit; 31.091 s process). Core: 3540/3540 passed (9.6513834 s NUnit; 29.095 s process). No failed/skipped cases in either assembly; no Framework/View rerun. Exact namespace relocations, per-case timings, compilation/import cost, scope and limitations are recorded in the disposition's Revised Task 1 execution section. Existing baseline/crash evidence is preserved. Production exception is the single Systems friend declaration. Tasks 2–4 are not started; stop here. No commit/push.


## Task 2 completion record (2026-10-26)

The user authorized Task 2 only after confirming revised Task 1 complete. Task 2 is complete in the existing checkout; Tasks 3–4 remain pending. No commit or push was performed.

Command and advance tests now execute through the existing CoreIntegration fixture and production services. The accepted attack, actor-free player end/stale identity, enemy action funding, two actual eight-turn nights, and terminal publication stop passed. Movement rejection retains pending input and presentation acknowledgement. Replacement-dependent deletions were applied only after the 15-case replacement run passed. Both relocated service files retain their original meta GUIDs; only D2/D3's true parameters and the named retired/duplicate methods plus exclusive helpers were removed.

Final CoreIntegration: **15/15 passed**, 0 failed/skipped (0.9111285 s NUnit; 29.690 s process). Final Core: **3540/3540 passed**, 0 failed/skipped (9.950567 s NUnit; 35.952 s process). No Framework/View tests executed. Production/asset hashes match Task 2 start, including the pre-existing font asset content and Task 1 Systems friend declaration. Exact case mapping, counts, source audit and execution notes are in the disposition's Task 2 section and `.superpowers/core-test-foundation/task2/`.

The read-only Task 2 review found no actionable findings. The remaining direct Buffer replay/transaction cases belong to Task 3; this completion does not claim the whole Phase or Task 4 guarantee map is complete. Stop before Task 3.

## Task 3 completion record (2026-10-26)

The user authorized Task 3 after Task 2. Task 3 is complete in the same checkout; Task 4 remains pending. No commit or push was performed.

All selected R1–R5 cases now run in CoreIntegration using real preparation, turn progression, calculation and production transaction/publisher contracts. Negative families retain a successful control and assert the intended validation boundary. The exact entry definition is captured for local assembler/planner/publisher binding; the existing no-op snapshot logic is consolidated in `CoreIntegrationSnapshot`, including actual membership/Main identity and all selected stores/counters. No production behavior, API, assembly boundary or runtime state ownership changed.

The 28 selected old cases were removed only after their replacements passed; one unchanged-item control was added. Unlisted methods and old shared Input/Create helpers remain unchanged. The publisher file retains its original meta GUID; split files and the snapshot have fresh metas. Previous 15 CoreIntegration cases remain, giving **44/44 passed**, 0 failed/skipped (1.3241567 s NUnit; 31.826 s process). Pure Core: **3540/3540 passed**, 0 failed/skipped (10.9216249 s NUnit; 30.939 s process). Framework/View tests were not executed.

Source/dependency/GUID/count/production-hash checks passed, as did `git diff --check`; the separate read-only review found no actionable findings. Full evidence and exact identities are recorded in the disposition's Task 3 section and `.superpowers/core-test-foundation/task3/`. Task 4's whole-Phase guarantee map remains a separate next step. Stop before Task 4.

## Task 4 / Phase completion record (2026-10-26)

The user authorized Task 4. The final guarantee map, exact old/new case reconciliation, source/dependency audit and fresh regression gate are complete. Task 4 changed documentation and verification evidence only; it did not change test or production code.

- **CoreIntegration: 44/44 passed**, 0 failed/skipped; 0.9120024 s NUnit and 78.224 s process wall time. The 67.699 s project load includes import/domain reload/licensing startup, not a slow test; the slowest integration case was under 0.32 s.
- **Core: 3540/3540 passed**, 0 failed/skipped; 10.4443098 s NUnit and 32.259 s process wall time. Case identities match the original baseline.
- The first Core launch overlapped the still-running integration Editor and stopped at the project-open lock before test execution. Its log/process record is preserved. After confirming integration exit 0, the sequential Core rerun completed with exit 0. No cache deletion, version change or broader Framework/View execution was used.
- The original 44 selected Framework cases reconcile as 37 relocations/recoveries, one accepted-command replacement, three retired cases and three consolidated duplicates. The final 44 CoreIntegration cases comprise the 38 transferred/replaced owners plus six additions. Root discovery remains **10,393 before and after**, with no unmapped or skipped cases.
- Assembly/meta/GUID, unchanged pure tests and retained mixed helpers, production hashes and `git diff --check` passed. Across the Phase, the sole production source change is the authorized Systems friend declaration. Existing font content and concurrent category/trait documents were preserved.

The final fresh read-only Phase review found no actionable findings. The disposition's Task 4 section records the guarantee owners and remaining scope limits. Evidence is in `.superpowers/core-test-foundation/task4/`, especially `final-integration.xml`, `final-core-sequential.xml`, `phase-case-ledger.tsv`, `guarantee-owners.tsv` and `phase-verification.json`.

This bounded recovery Phase is complete. Deferred Framework/View/environment/trait/item cases retain their prior scope and are not claimed green. Work remains uncommitted on `feature/Cheon/Core/NewCombatSytem`; no merge, commit or push was performed. Stop here.
