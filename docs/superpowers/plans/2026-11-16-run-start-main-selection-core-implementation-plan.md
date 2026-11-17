# Run Start Main Selection Core Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans for inline execution, or superpowers:subagent-driven-development if the user selects delegation. Steps use checkbox (`- [ ]`) syntax for tracking. Stop and report after every Phase; do not advance automatically.

**Goal:** Let Core callers start a new Run with a selected main while preserving the base loadout's starting subs, gold, hand size, and initial deck selection.

**Architecture:** Extend the existing `IRunStartService` with candidate lookup and an explicit-main start overload. Reuse the existing roster planner, definition adapter, rules, source validation and Buffer publication transaction. View/LifeCycle integration remains a separate handoff.

**Tech Stack:** Unity 6000.0.73f1, C#, existing Framework/Systems/Core assemblies, NUnit EditMode.

**Spec:** [Approved Core design](../specs/2026-11-15-run-start-main-selection-core-design.md), approved by the user on 2026-11-16.

**Handoff:** [View/LifeCycle integration](../handoffs/2026-11-15-run-start-main-selection-core-view-handoff.md).

**Status:** Core Phases A through C are complete for review. The View/LifeCycle handoff records the remaining integration work.

## Global Constraints

- Only the main changes. Preserve `Entries`, `StartingRunCurrency`, `StartingHandSize`, and `InitialDeckGrantIndices` from the same loadout.
- Preserve `StartNewRun(int loadoutKey)` and `InitializeRoster(int loadoutKey)` behavior. Explicit invalid main input must never fall back to the default.
- Return the existing `RosterAcquireResult`; preserve detailed `InitialDeckFailure` and configuration exceptions.
- Keep gameplay state in the existing Buffers. No new selected-main store, duplicate loadout, SO mutation, roster builder, or Framework layer.
- Do not edit View, LifeCycle, lobby input transport, scenes, production assets, or unrelated Core features. Required View integration is documented, not implemented.
- Keep existing Framework registration and group lookup paths. Use existing Core directories; pure contract DTOs have no Unity dependency.
- Implementation documents are English. Code/XML comments are Korean. New `.cs` files require `.meta`; preserve existing GUIDs, namespaces and typo identifiers.
- Test inputs are test-owned. No View tests or combat-View MSBuild. Do not repair another author's existing runtime tests without permission; minimal compile-only compatibility adjustments retain assertions.
- Unity execution requires the approved AppData-access path. Do not delete caches or upgrade Unity to work around the known sandbox CurlRequestCache problem.
- No commit or push. Each Phase ends with evidence and a user review stop. An out-of-design decision requires discussion before implementation.

## Review Focus

1. A regular unit already present in the starting subs is explicitly selected as main: return `InvalidDefinition`, not a later duplicate-ownership failure (Phase B).
2. The authored default differs from the first sorted candidate: the UI contract returns the authored default without silently replacing it (Phase A).
3. A database read reenters and changes captured state: reject with `InvalidRequest` without publishing or rolling back the external change (Phase B).
4. Invalid selection is submitted while a populated Run or retained battle exists: no resets or grants occur on failure (Phase B).
5. A previously upgraded main is followed by a new Run with another main: no old persistent state, skill upgrade or skill definition leaks (Phase C).

## File Map and Boundaries

All paths are repository-relative. The aliases below expand to exact paths throughout this plan:

| Alias | Path |
|---|---|
| `Core` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| `Contracts` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts` |
| `IntegrationTests` | `Assets/Tests/EditMode/CoreIntegration` |

| File | Responsibility / Phase |
|---|---|
| Create `Contracts/Roster/RunStartMainOptions.cs` and `.meta` | Immutable options DTO; A |
| Modify `Core/RunSystem/RunStartService.cs` | Options query in A; shared default/explicit start execution in B |
| Modify `Core/CharacterSystem/PlayerLoadoutRosterPlanner.cs` | Effective-main override while retaining one roster-planning algorithm; B |
| Create `IntegrationTests/RunStartMainSelectionTestRuntime.cs` and `.meta` | Test-only inputs and actual Framework/Buffer/Core composition without auto-starting a Run; A, extended in B/C |
| Create `IntegrationTests/RunStartMainSelectionTests.cs` and `.meta` | Options/contract and start/rejection tests; A/B |
| Create `IntegrationTests/RunStartMainSelectionBattleTests.cs` and `.meta` | Actual database battle initialization and main-state reset evidence; C |
| Update this plan and linked handoff | Per-Phase evidence and delivery status |

Read/reuse, do not rewrite: `CharacterDefinitionAdapter`, `CharacterRosterRules`, `DeckRules`, `RosterSourceVersion`, `NewRunPreparation`, `ProgressTransactions.PublishNewRun`, and `DatabaseBattleInitializationSource`.

No production change is currently expected in `InGameCoreSystemFactory`, `InGameCoreSystemGroup`, service keys, `RunDeckService`, or Buffer transactions. This is an expectation based on the existing service extension, not a blanket prohibition on composition changes.

- **Normal composition adjustment:** Within the authorized Core Phase, creation, registration or explicit dependency-wiring changes that preserve the approved behavior, public contracts, ownership and lifecycle are normal integration work. Record the actual files, why the connection is needed, the provider-to-consumer path and affected verification. Do not stop solely because a shared composition file changes; do not hide dependencies behind global lookup to avoid editing it.
- **Design change:** If the implementation needs to change behavior, public API semantics, state ownership, lifecycle/execution order, Framework registration boundaries or existing Buffer/Job responsibilities beyond the approved design, stop and explain the evidence and impact before implementing it. Moving rules into composition or duplicating state is not a wiring adjustment.
- **Scope still applies:** Normal wiring does not authorize View/LifeCycle/input transport edits in a Core Phase. Those remain with the receiving worker. Preserve unrelated work present at execution time.

## Verification Procedure

Phase-specific execution results are recorded below.

Before launching Unity, apply the `unity:unity-cli` skill, resolve the installed 6000.0.73f1 executable, and check whether this project is already open. Use the connected Editor test path when appropriate; never launch a competing Editor for the same project. For batch execution with no active Editor, run through approved AppData access. The following PowerShell template uses `$unityEditor` set to the verified executable and `$projectPath` set to the chosen checkout:

```powershell
$projectPath = 'D:\My_ProjectWork\Github_desktop\Project_L'
$testOutput = Join-Path $env:TEMP 'ProjectL-main-selection'
New-Item -ItemType Directory -Force -Path $testOutput | Out-Null
& $unityEditor -batchmode -nographics -projectPath $projectPath -runTests -testPlatform EditMode -assemblyNames ProjectL.Tests.CoreIntegration -testFilter ProjectL.Tests.CoreIntegration.RunStartMainSelectionTests -testResults (Join-Path $testOutput 'phase-a.xml') -logFile (Join-Path $testOutput 'phase-a.log')
```

- Phase 0 full baseline: omit `-testFilter` and use `-assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration'` with `baseline` output names.
- Phase B focused run: same class filter, distinct `phase-b` output names. It includes all Phase A cases.
- Phase C uses the user-approved focused scope across both assemblies. Select the two main-selection fixtures, the five Core policy/roster/deck fixtures, and the existing player-skill lifetime, player-skill end-to-end, and Run Gold journey fixtures listed in the execution evidence. Use `phase-c-focused` outputs.
- A test pass requires fresh XML containing the intended fixtures and nonzero executed cases, no relevant failures/skips, and no compilation errors in the Editor log. Process completion alone is insufficient. Check exact filter/assembly results before reporting success.
- Preserve any required repository-wide checks discovered during execution. Existing camera/placement failures are historical possibilities, not accepted current baselines; identify actual failures and compare to Phase 0 without hiding or weakening them.
- Record both focused and suite totals, artifact paths, failures and compilation status. If execution is unavailable, explicitly report unverified coverage and do not mark the affected gate complete.

## Phase 0 — Establish the Current Baseline

**Deliverable:** A current execution baseline and a confirmed Core-only change boundary. No product changes.

- [x] Read the approved spec, handoff, root/Assets instructions, Framework programmer guide and functional-independence guidance before execution. Resolve isolation using `using-git-worktrees` at execution time; do not create a worktree just to write this plan.
- [x] Record `git status --short`, branch, HEAD and Unity version. Preserve the existing untracked design/plan/handoff documents when choosing the execution checkout.
- [x] Recheck all `IRunStartService` implementations, `StartNewRun` callers and planner overload consumers. Current inspections found one production implementation; retain the one-argument route for Jobs, bootstrap, temporary runtime and existing tests.
- [x] Run the full Core/CoreIntegration baseline using the procedure above. Record failures and ownership without repairing unrelated tests or changing production content.
- [x] Confirm `CoreIntegrationTestData` still treats Sniper as a regular starting sub. Do not change its defaults or existing tests to fit this feature.
- [x] Report the baseline, anticipated files and any blocking drift. **STOP for Phase 0 review.**

### Phase 0 checkout and source evidence (2026-11-16)

- The baseline uses the existing checkout on `feature/Cheon/Core/NewCombatSytem` at `5d6fc282744d0d7f4f6ba1ee62ba93d6714a0196`. Initial `git status --short` showed only the untracked design, plan, and handoff. This baseline required no product edits and was run in place to retain those documents. The project and installed Editor both use Unity `6000.0.73f1 (a166abc3bf0e)`.
- `InGameCoreSystemFactory` creates the only implementation, `RunStartService`. Its export registers the interface and lifecycle module in Core. `InGameCoreSystemGroup` resolves it by the existing service key. Lobby and Stage use `RunStartJob`. That Job, development bootstrap, the temporary runtime, and seven integration-test call sites use the one-argument API. `RunStartService` uses the planner overload that returns definitions for deck evaluation. `CharacterRosterService.InitializeRoster` uses the shorter overload.
- `CoreIntegrationTestData` defines Akimbo as main and grants one `Unit_Sniper` as a `UnitRole.Regular` starting sub. Phase A's feature-owned fixture will supply its own Sniper main and regular sub definitions.
- The expected product scope is the options DTO, `RunStartService.cs`, and `PlayerLoadoutRosterPlanner.cs`. Existing registration, definition validation, roster/deck rules, and Buffer publication supply the necessary contracts. The focused fixtures and both full assemblies cover the shared start, roster/deck, reset, and battle/skill dependencies. View/LifeCycle integration requires the options, start result, and request lifetime contracts from the handoff.

### Phase 0 EditMode evidence (2026-11-16)

Unity ran with approved AppData access. The log confirms EditMode, both requested assemblies, no test-name filter, successful script compilation, and exit code 2 for test failures. The XML reports 649.5 seconds and 7,047 executed cases. All 14 skips are dedicated diagnostic captures that require explicit enablement.

| Assembly | Total | Passed | Failed | Skipped |
|---|---:|---:|---:|---:|
| `ProjectL.Tests.Core` | 5,908 | 5,895 | 2 | 11 |
| `ProjectL.Tests.CoreIntegration` | 1,153 | 1,116 | 34 | 3 |
| **Total** | **7,061** | **7,011** | **36** | **14** |

| Fixture | Failures | Owning area and observed result |
|---|---:|---|
| `TraitActivationContractTests` | 1 | Core traits. Interception payload identity assertion expected true and returned false. |
| `TraitActivationOutcomeTests` | 1 | Core traits. The applied Taunt case expected one activation and observed two. |
| `CombatCameraZoomPoseTests` | 1 | FieldCamera integration fixture. Reflection raised `TypeLoadException` for `Cinemachine.CinemachineCameraOffset`. |
| `CombatStructureInspectionTests` | 1 | Combat View fixture. `CombatUnitInfoView.Prepare` requires a parent Canvas. |
| `DataBaseAssetTests` | 8 | Production DB/content integration. Category defaults, missing stage 1101, Cannon trait history, Vanguard rank values, and rest options differ from test expectations. |
| `DeckMainChangePolicyTests` | 8 | Deck View fixture. `WithView` hit a null reference during UI registration/binding at line 195. |
| `DeckSlotInfoPanelTests` | 13 | Deck View fixture. `ScrollPanelPresentation` lacked the audio service for `ScrollOpen` during setup. |
| `EnemyAiTuningAssetTests` | 1 | Enemy AI tuning content. The asset returned `20/20/200` for an expectation of `50/50/150`. |
| `RunGoldFacilityTests` | 2 | Run recovery production integration. The installed rest option IDs differ from the expected sequence. |

The roster rules/contracts, main definition policy, and deck rules/contracts passed all 105 cases within the full run. Existing one-argument start coverage in Hunter persistence, Main specialization battle, player skill lifetime, Run Gold journey, and trait state publication passed all 39 cases in those fixtures. The source checks found no change that requires revising the Phase A design. Retain the 36 failures as the Phase C comparison baseline.

Artifacts `baseline.xml` and `baseline.log` are in `C:/CodexHome/visualizations/2026/11/15/01a1dfe2-e73c-7022-9645-43288d7bf483/projectl-main-selection-baseline-20261115T154757Z/`. The XML contains each failing test name, message, and stack trace.

The Unity-generated FMOD scan path was restored. The FMOD catalog and the TMP font touched during shutdown both match HEAD byte for byte. `git diff HEAD` is empty for tracked files. The design and handoff remain in the checkout alongside this plan.

## Phase A — Candidate Query and Immutable Contract

**Deliverable:** The existing service can read eligible mains before a Run exists. Existing starting behavior is unchanged.

**Files:** Create options DTO, test runtime, and main-selection tests; modify `RunStartService.cs` only to add the read method. Create `.meta` alongside each new C# file.

**Interfaces produced:**

```csharp
public RunStartMainOptions(int loadoutKey, EntityKey defaultMainCharacterKey,
    IReadOnlyList<EntityKey> candidateKeys);
RunStartMainOptions IRunStartService.ReadMainOptions(int loadoutKey);
```

The DTO exposes get-only `LoadoutKey`, `DefaultMainCharacterKey`, and `CandidateKeys`. Reject a null collection with `ArgumentNullException`, copy it, and expose read-only storage. Eligibility/default-membership checks belong to the query, not a duplicate policy in the DTO.

### A1. Establish Feature-Owned Inputs and Contract Evidence

- [x] Add `RunStartMainSelectionTestRuntime : IDisposable` with a parameterless default constructor, `Core`, `Buffers` and test-data accessors. Follow the real `BufferSystemFactory` / `InGameCoreSystemFactory` / `FrameworkRuntime.InitData` / `PostBindService` path used by existing fixtures, but do not acquire a roster or enter a chapter in the constructor.
- [x] Keep a feature-local DB context/table wrapper in that fixture file. Delegate unrelated table queries to a private `CoreIntegrationTestData` instance, override Unit/Loadout queries, and forward optional player-skill tables. Do not copy production calculation or modify the shared fixture. Add a one-shot unit-read hook only when needed by B's stale-state cases.
- [x] Author two main definitions with distinguishable stats: Akimbo HP 30 / attack 10 / defense 5; Sniper HP 10 / attack 5 / defense 2. Use one test loadout with default Akimbo, three regular subs (ShotGun, Cowboy, DynamiteHammer, one each), gold 137, hand size 4 and indices `[0,1,2,3]`. These are fixture values, not new production defaults. All supporting categories/targeting/stages come from test-owned inputs.
- [x] Add cases named `ReadMainOptions_BeforeRun_ReturnsSortedMainsAndAuthoredDefault`, `ReadMainOptions_UsesDefaultRatherThanFirstCandidate`, `ReadMainOptions_RejectsMissingLoadoutAndInvalidDefault`, `ReadMainOptions_RequiresInitializationAndBinding`, `ReadMainOptions_MalformedDefinitionThrows`, and `Options_DefensivelyCopyCandidateKeys`. Assert exact keys/default, no Run/roster/deck/currency changes, documented exception types, and immutable storage. Include a third valid test-only main under an existing non-retired key to prove there is no two-key allowlist.
- [x] Demonstrate the expected missing-API/DTO compilation failure, then supply the minimum signatures/DTO necessary to obtain a failing behavior assertion before implementing the query. Do not leave a throwing stub at the Phase boundary.

### A2. Implement the Read Contract

- [x] Add `ReadMainOptions(int loadoutKey)` to the existing interface and implementation. Require `_units`, `_loadouts`, `_buffers` readiness; use the existing loadout lookup/validation; validate enumerated definitions through their existing policy path and filter valid main roles; sort by numeric key; require the authored default to be among the keys. Do not catch malformed data to return an empty list.
- [x] Run the focused Phase A tests to green. Verify a separate instance/configuration with Sniper as the default still returns numeric candidate ordering and Sniper as default.
- [x] Run relevant existing `PlayerMainDefinitionPolicyTests`, `CharacterRosterRuleTests`, `CharacterRosterContractTests`, `RunDeckRuleTests` and `RunDeckContractTests` via their verified full class names. Confirm the original start service path remains intact; do not introduce the B overload early.
- [x] Check `git diff --check`, DTO Unity-dependency absence, registration preservation and `.meta` files. Record evidence and remaining Phase B/C coverage. **STOP for Phase A review.**

### Phase A execution evidence (2026-11-16)

The user approved Phase A with focused verification. Unity 6000.0.73f1 ran the new fixture and six existing regression fixtures by fully qualified test name. Phase 0 remains the full-assembly baseline.

| Run | Result |
|---|---|
| `phase-a-red-compile` | Exit 1. The tests exposed the missing `ReadMainOptions` API and DTO (CS1061 and CS0246). A nullable hand-size declaration in the test was corrected before the behavior run. |
| `phase-a-red-behavior` | Exit 2. The DTO case passed and seven query cases failed at the expected assertions with the temporary empty result. |
| `phase-a-green` | Exit 0. All 114 selected cases passed, with zero failures, skips, or compiler errors. The new fixture passed 8 cases, the five Core rule/contract fixtures passed 105, and `RunGoldJourneyTests` passed 1 through the existing start path. Test Runner duration was 5.14 seconds. |

XML and logs are in `C:/CodexHome/visualizations/2026/11/15/01a1dfe2-e73c-7022-9645-43288d7bf483/projectl-main-selection-phase-a/`. The compile run produced a log. The behavior and passing runs each produced XML and a log.

The query uses existing `PlayerLoadoutData.Validate` and `UnitData.Validate` rules and leaves Run state untouched. The default comes from the loadout and the DTO copies the sorted keys. The existing one-argument command body, planner, and service/module registration are identical to the Phase 0 revision. Repository whitespace checks and the three new GUID checks passed. The planned selected-start transaction, failure cases, and battle/skill integration remain Phase B/C coverage.

| New source file | Meta GUID |
|---|---|
| `RunStartMainOptions.cs` | `f129da3f51285f7d673e8a36dd847135` |
| `RunStartMainSelectionTestRuntime.cs` | `b1ff57198fcc202953ff5476e977c6bd` |
| `RunStartMainSelectionTests.cs` | `051fcd4f89547a426bc53fcf07341091` |

## Phase B — Start with an Explicit Main

**Deliverable:** Both start overloads use one planning/publication path; selected-main failures do not mutate Run state.

**Files:** Modify `RunStartService.cs`, `PlayerLoadoutRosterPlanner.cs`, and the feature-owned tests/runtime from A.

**Interfaces consumed:** Phase A options and existing `CharacterDefinitionAdapter.Read`, `CharacterRosterRules.PlanInitialRoster`, `DeckRules.EvaluateInitial`, `RosterSourceVersion`, and `PublishNewRun`.

**Interfaces produced:**

```csharp
RosterAcquireResult IRunStartService.StartNewRun(int loadoutKey, EntityKey mainCharacterKey);

internal static CharacterAcquisitionDecision Plan(
    IPlayerLoadoutTableQuery loadouts, IUnitTableQuery units,
    int loadoutKey, bool rosterIsEmpty, EntityKey? mainCharacterOverride,
    out PlayerLoadoutData loadout,
    out IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions);
```

Keep both existing planner overloads; delegate them with `mainCharacterOverride: null`. Use a private `StartNewRunCore(int loadoutKey, EntityKey? mainCharacterOverride)` for common service execution. Public omission maps to null; explicit `None` remains an invalid supplied value.

### B1. Pin Selection and Rejection Behavior

- [x] Add `StartNewRun_ExplicitMain_PreservesSharedLoadout` for both mains and `StartNewRun_DefaultOverload_UsesAuthoredMain`. Assert exactly one main, its definition's fresh full-health/base state, unchanged sub grant order/count, gold 137, hand size 4, and installed deck IDs matching initial grant indices. Also verify a changed authored default, not just Akimbo.
- [x] Add `StartNewRun_InvalidMain_DoesNotResetExistingRun` for None, undefined enum value, a defined-but-missing key, a regular starting sub and a valid enemy-only definition. Check `InvalidDefinition`, empty acquired results, and unchanged Run identity/revision, roster/main state, deck, currency, progress and items. Include a retained-battle case to catch premature reset. This checks failed calls, not new phase-admission policy.
- [x] Add cases for missing loadout, malformed selected definition, invalid sub definition and malformed initial deck. For indices `[1,2,3]`, require `InvalidInitialDeck` with `DeckFailureReason.MissingMain`; preserve the prior state. Missing/malformed data retain the spec's result-versus-exception distinction.
- [x] Add `StartNewRun_StateChangesDuringDefinitionRead_RejectsWithoutPublication`. The one-shot test hook changes, in separate cases, roster/deck source, currency, and Run outcome. Expect `InvalidRequest`; assert the hook's change remains but no new Run/reset/grant occurred. Compare against the state after the hook rather than incorrectly expecting the pre-hook state to be restored.
- [x] Capture expected failures before implementing behavior. Missing overload compilation is preliminary evidence; obtain failing behavior assertions once signatures exist.

### B2. Extend the Common Creation Path

The successful-start case includes these boundary assertions, in addition to the sub-order and persistent-state checks above:

```csharp
using var game = new RunStartMainSelectionTestRuntime();
var result = game.Core.RunStart.StartNewRun(1, EntityKey.Unit_Sniper);
Assert.That(result.Succeeded, Is.True);
Assert.That(result.Acquired.Single(x => x.Role == CharacterOwnershipRole.Main).CharacterKey,
    Is.EqualTo(EntityKey.Unit_Sniper));
Assert.That(game.Buffers.CurrencyReadSink.Amount, Is.EqualTo(137));
Assert.That(game.Buffers.RunProgressReadSink.StartingHandSize, Is.EqualTo(4));
```

- [x] Add the new planner overload and retain the old delegates. Resolve the loadout once, choose override versus default, and read the selected definition through the existing adapter. Reject a missing/non-main explicit selection before expanding subs so a regular key already in `Entries` cannot produce an ownership-limit error instead of `InvalidDefinition`.
- [x] Preserve main-at-grant-zero, unchanged sub expansion/order, existing ownership and initial-roster checks, and actual selected definitions in the returned map. Do not construct a replacement loadout or change `InitializeRoster` callers.
- [x] Add the public start overload and route both overloads through `StartNewRunCore`. Preserve capture → plan → initial-deck evaluation → source/currency/outcome recheck → one `NewRunPreparation` publication. Use the existing result and exception conventions.
- [x] Run all feature tests plus the relevant existing roster/deck and Run-gold reset checks. Verify query/start agreement for valid definitions, without turning options lookup into full departure preflight.
- [x] Review the diff against the planned file map and the composition-adjustment criteria above; record justified wiring changes rather than treating file-list differences alone as violations. Check `git diff --check`. Document default compatibility, no-mutation failures, remaining battle-integration coverage and compilation evidence. **STOP for Phase B review.**

### Phase B execution evidence (2026-11-16)

Unity 6000.0.73f1 ran the feature fixture and six directly related regression fixtures under the user's focused verification scope.

| Run | Result |
|---|---|
| `phase-b-red-compile` | Exit 1. New tests required the two-argument overload (CS1501). |
| `phase-b-red-behavior` | Exit 2. The temporary delegate to the default path passed 18 cases. Eight assertions failed as expected for the Sniper override, invalid selections, and a malformed selected definition. |
| `phase-b-green` | Exit 0. All 132 selected cases passed with zero failures, skips, or compiler errors. The feature fixture passed 26 cases (18 new), five Core policy/rule/contract fixtures passed 105, and `RunGoldJourneyTests` passed 1. Test Runner duration was 2.23 seconds. |

Artifacts are in `C:/CodexHome/visualizations/2026/11/15/01a1dfe2-e73c-7022-9645-43288d7bf483/projectl-main-selection-phase-b/`. Each run has a log. The behavior and passing runs also have XML results.

Phase B changes the start service and planner, with tests and the one-shot lookup hook in the Phase A fixture. Both public start paths converge before capture and share roster planning, deck evaluation, the stale-source recheck, and one publication. The existing registration, Buffer transaction, and roster initialization entry point continue to supply those contracts. Callers choosing a main need the new overload and existing result contract. Roster order, initial deck IDs, reset semantics, and the publication boundary required regression coverage.

The rejection cases compare Run identity, roster and main state, deck, currency, progress, item inventory, and retained battle through the existing integration snapshot. The four reentrant cases compare state after the observed roster, deck, currency, or outcome change. Selected-main battle definition and skill propagation and the later fresh-Run progression checks are Phase C evidence.

Independent Phase B review identified one minor coverage gap. The successful integration fixture uses authored indices `[0,1,2,3]`, while the Core rule suite covers normalization of a reordered selection. Successful publication of a reordered subset remains unverified at the integration boundary. The review found no blocking issues.

## Phase C — Battle Integration, Regression and Core Handoff

**Deliverable:** Evidence that the installed selected main reaches existing battle/skill paths, plus an accurate API handoff. No View implementation.

**Files:** Create `RunStartMainSelectionBattleTests.cs` and `.meta`; extend only the feature-owned runtime as needed; update plan/handoff delivery evidence. No expected additional production edits.

**Interfaces consumed:** A/B APIs, existing chapter/battle entry, database battle source, roster/deck and combat read contracts, existing player-skill upgrade API.

- [x] Add `SelectedMain_BattleInitializationUsesOwnDefinitionAndSkill` for both mains with test-owned definitions. Verify battle ownership, stats, traits, bound skill, and one real skill command per main.
- [x] Add `FreshRun_WithAnotherMain_DropsPreviousMainProgression` and `FreshRun_WithSameMain_StartsFreshRun`. Check the prior upgrade and persistent state, the fresh Run/roster IDs, and the loadout's subs, gold, hand size, and deck.
- [x] Run the selected-main battle and reset tests and confirm that the existing Core entry, skill, and publication paths satisfy their assertions.
- [x] Run the user-approved focused regression with fresh XML and log output. Compare overlapping cases against Phase 0 and retain the full-suite baseline for other cases.
- [x] Verify SSOT, SOLID, and Framework composition across the three Core phases. Trace service creation, interface/module registration, group exposure, and default callers.
- [x] Update the handoff with actual signatures, readiness, failure handling, evidence paths, and the remaining View/LifeCycle checklist. Retain the §3.1 draft owner, injection, and lifetime planning requirements.
- [x] Record source, verification, and maintenance boundaries, the new test GUID, and remaining evidence limits.
- [x] Report Core compilation and focused verification, with View compilation and PlayMode pending. **STOP for Phase C review. No commit or push.**

### Phase C execution evidence (2026-11-16)

Unity 6000.0.73f1 compiled the project scripts and executed all 150 selected EditMode cases in 7.59 seconds of Test Runner time. Unity exited 0 with zero failed or skipped cases and zero compiler errors.

| Focused coverage | Passed |
|---|---:|
| `RunStartMainSelectionBattleTests` | 4 |
| `RunStartMainSelectionTests` | 26 |
| `PlayerMainDefinitionPolicyTests`, `CharacterRosterRuleTests`, `CharacterRosterContractTests`, `RunDeckRuleTests`, `RunDeckContractTests` | 105 |
| `PlayerSkillLifetimeTests`, `PlayerSkillEndToEndTests` | 14 |
| `RunGoldJourneyTests` | 1 |
| **Total** | **150** |

All 120 existing cases shared with the Phase 0 XML passed in both runs. The 36 Phase 0 failures fall outside the focused result and remain listed under Phase 0. The full-suite verdict remains the Phase 0 baseline.

The battle tests select Akimbo and Sniper through Core, enter through `DatabaseBattleInitializationSource`, and match the installed actors, base stats, authored traits, and skill assignments. Each main casts its bound skill through the normal preview and command services. The published result contains a hit and consumes one use. Fresh Run cases replace a Bloom-upgraded and modified Akimbo with Sniper or a fresh Akimbo. They verify new Run and roster IDs, reset progress and items, restored base state and skill usage, and preserved loadout grants, gold, and hand size. A valid authored initial deck subset `[3,0,1]` installs the IDs of grants `[0,3,1]` through the real start transaction, closing the Phase B integration coverage gap.

Unity evidence directory is `C:/CodexHome/visualizations/2026/11/15/01a1dfe2-e73c-7022-9645-43288d7bf483/projectl-main-selection-phase-c/`. Results are in `phase-c-focused.xml` and the Editor output is in `phase-c-focused.log`. New source `RunStartMainSelectionBattleTests.cs` has meta GUID `80bb63208cacb159f050cfa5853b5d56`.

Phase C adds tests and extends their test-owned fixture. Core production edits across delivery remain confined to the DTO, start service, and roster planner. The factory creates `RunStartService`, the service exports its interface and lifecycle module, and `InGameCoreSystemGroup` resolves it by the existing key. The default Job, bootstrap, temporary runtime, and roster initialization callers still follow their original entry points. The loadout retains authored shared configuration and both start overloads publish through one existing Buffer transaction. A View consumer needs the options, start result, and its own request-lifetime contracts. The roster, deck, reset, and skill boundaries are covered by the named focused regressions.

Independent review found no blocking issues. Its minor coverage note records that fixture hand size 4 equals the system default. The focused integration cases verify that value. Their hand-size assertion does not distinguish authored values from the default. The planned View popup, draft and immutable request, and Job forwarding remain future work. View compilation and PlayMode/visual verification remain pending.

## Coverage and Execution Record

| Approved design requirement | Owner |
|---|---|
| Immutable options, eligible definitions, authored default, readiness/errors, no mutation | A1–A2 |
| Explicit versus omitted key, no fallback, old start/roster compatibility | B1–B2 |
| Shared loadout settings, selected initial state, deck grant zero | B1–B2 |
| Rejection/configuration errors, stale state, prepare-before-reset | B1–B2 |
| Selected skill/traits, fresh-Run reset and no progression leakage | C |
| Existing Framework/Buffer ownership and regression | 0, A2, B2, C |
| View request/draft/Job/UI wiring | Handoff only; not an implementation Phase |

| Phase | State | Evidence |
|---|---|---|
| 0 | Complete for review | Unity EditMode baseline recorded above. 7,011 passed, 36 failed, 14 diagnostic captures skipped. |
| A | Complete for review | Candidate query and immutable DTO implemented. 114 focused cases passed. |
| B | Complete for review | Both start overloads share preparation and publication. 132 focused cases passed. |
| C | Complete for review | Selected-main skill attacks, both fresh-Run cases, and all 150 focused cases passed. Core handoff updated. |

Self-review on 2026-11-16: spec coverage, API/type consistency, review-focus ownership, file boundaries and Phase stops checked. The shared-fixture Sniper role conflict is handled with feature-owned inputs. This record is plan review only, not implementation verification.

Locality-review clarification on 2026-11-16: normal in-scope composition adjustments are distinguished from design changes, and concrete View draft ownership/injection/lifetime must be resolved by the receiving worker. No implementation or execution status changed.
