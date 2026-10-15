# Stage Entry and Battle Preparation Implementation Plan

> **For agentic workers:** Use superpowers:executing-plans for sequential inline execution, or superpowers:subagent-driven-development if the user selects that method. Steps use checkbox (`- [ ]`) syntax. Execute only the user-authorized tasks. External owner checklists are handoff requirements, not authorization to implement their systems. Commit/push only on explicit user request.

**Goal:** Implement explicit-target Combat entry that publishes selection and prepared battle together, while keeping Core/Buffer completion independent of View/Flow caller migration.

**Architecture:** The existing Combat service orchestrates live progression validation, explicit DB conversion and pure preparation calculations. The existing Buffer group prepares Current, Character and Combat stores before installing them. Existing Framework factories/lifecycle binding remain authoritative; View/Flow retains Jobs, request transport, scheduling and activation.

**Tech Stack:** Unity 6000.0.73f1, C#, NUnit EditMode, FrameworkRuntime/SystemBuilderHelper, existing Core Contracts/Application/Domain and Systems assemblies.

**Spec:** [Approved entry and preparation design](../specs/2026-10-03-stage-entry-battle-preparation-contract-design.md).

**Status:** P1-P3 and gate C completed on 2026-10-03; V/D/A remain independently pending. See the [final Core handoff](../handoffs/2026-10-03-stage-entry-battle-preparation-handoff.md), [P1 evidence report](../handoffs/2026-10-03-stage-entry-battle-preparation-p1-verification.md) and [P2 evidence report](../handoffs/2026-10-03-stage-entry-battle-preparation-p2-verification.md). P3 focused tests passed 222/222; full regression passed 1371/1376 with the same five Economy authoring failure names/messages/stacks as the fresh P1 baseline and P1/P2 regressions. Final independent review found no actionable defect; the deferred preview evidence is closed. Verification was completed before Git delivery; subsequent commit/push is recorded in Git history.

## Separate completion gates

| Gate | Owner | Evidence needed | Permitted completion statement |
| --- | --- | --- | --- |
| C: Core and Buffer implementation | Core, including the narrow Buffer publication extension | P1-P3 complete; real registered entry tests pass; existing Job callers compile; no new regression | Core/Buffer entry contract implemented and verified with fixture definitions; caller migration remains open. |
| V: Caller migration | View/Flow; Framework consulted for registration/fault contracts | Explicit target and outcome travel through the real Register/Plan/Runner/Job route; one preparation; activation waits for acceptance; rejection/fault has a terminal handling path | View/Flow caller migration verified. This may use injected definitions and does not close DB readiness. |
| D: Actual definition readiness | DB supplies definitions; Core adapts and verifies | Approved Method_Key/query/unit linkage, real Economy and required definitions, successful default-source entry with authored data | Actual DB-backed entry verified. |
| A: Production activation | Joint execution, Framework, DB and Core acceptance | C + V + D; real scene/bootstrap path; F1/F2 and input/result lifetime evidence | Production Combat entry activated. |

Gate C can close while V/D/A remain pending. Gate V cannot be inferred from a compatibility Job smoke test or a direct Core call. Gate D cannot be inferred from an injected source. Removing the compatibility API is separate follow-up work after V and all remaining callers are accounted for; it is not required to close C.

## Global constraints

- Preserve existing factories, exported service keys, parent lifecycle propagation and one physical Buffer per state domain. No new singleton, service locator, coordinator graph or execution framework.
- Core validates navigation and Combat type, resolves target definitions and calculates proposals. Buffer validates storage/expected-state consistency and installs prepared facts. No DB lookup, pathfinding, navigation policy or Job registration inside Buffer.
- No persistent pending-entry state, selection mirror, generic transaction/callback API, revision counter or rollback/reset permission. Expected facts and prepared values are local to one synchronous call.
- The guarantee uses the existing serialized execution model; arbitrary concurrent/reentrant writers, process failure and out-of-memory recovery are outside it. Never use `EndBattle`, `AbandonBattle` or `ResetRunProgress` as failed-entry rollback.
- Only regular/elite/boss Combat entry in an already-entered chapter is covered. Recovery/shop workflows, chapter routing, Run reset, rewards, terrain policy and authored asset creation remain outside this plan.
- Preserve the default source's missing Method_Key rejection. Do not infer profiles from old enums or introduce a production fallback.
- Core never submits, clears, removes or acknowledges View input. It does not initialize the roster, wallet or Artifact inventory during entry.
- Documents are English; new code comments are Korean. Preserve incoming changes and existing Unity GUIDs; pair new C# files with unique `.meta` files.
- Do not change production Job/Plan/Flow/LifeCycle/Runner code in P1-P3. An explicitly listed test-double interface update is compilation maintenance, not caller migration.

## Review focus

1. First entry without selection, and next entry while the old selection remains, must load the requested stage rather than a cached/previous definition: P2.
2. Rejected navigation, malformed definitions and late store validation must not partially select or install a battle, or erase old noncombat state: P1/P2.
3. A source/test seam that changes origin/target during preparation must not make a stale preflight decision authoritative: P1/P2. External mutations themselves are not rolled back.
4. Repeated entry, retained terminal battles and definition-cache reuse must not duplicate preparation, reset persistent values or use the failed target on retry: P2.
5. An old compiling Job must not be mistaken for a migrated Job; activating the new route must not also execute `OnEnter -> BeginPreparation`: P3 and gate V evidence.

## File and interface map

Path abbreviations below expand to repository-relative paths:

- `Core` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem`
- `Buffers` = `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`
- `CombatExecution` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat`
- `Tests` = `Assets/Tests/EditMode/Framework`

| File | Action and single responsibility |
| --- | --- |
| `Buffers/Current/BattleEntrySelection.cs` + `.meta` | Create a detached expected/current-to-target selection proposal. No behavior that decides entry eligibility. |
| `Buffers/Current/CurrentProgressBuffer.cs` | Add internal prepare/install selection support and its internal prepared value. Preserve existing public selection semantics. |
| `Buffers/Core/BufferSystemGroup.cs` | Extend the existing battle writer with joint selection/battle publication and reuse its existing battle preparation validation. |
| `Core/CombatSystem/CombatProcessingService.cs` | Add explicit-target entry orchestration, update its internal source interface, compatibility preparation and definition reload. |
| `Core/CombatSystem/DatabaseBattleInitializationSource.cs` | Resolve the supplied identity, retaining Character read binding and the missing-profile gate; remove the implicit Current read dependency. |
| `Core/CombatSystem/BattleDefinitionAdapter.cs` | Reuse its existing `Read(database, int mapKey, int stageKey, owned)` conversion; no new schema or formulas. |
| `Tests/BattleEntryPublicationTests.cs` + `.meta` | Create real-store publication tests, with fixture setup local to this file. |
| `Tests/BattleDefinitionAdapterTests.cs` | Explicit-source identity, default-source gate and preparation compatibility tests. |
| `Tests/CoreStageCombatCycleTests.cs` | Extend the registered fixture to cover the new entry API and distinguishable next-stage definitions. |
| Listed fixture doubles below | Mechanical signature/setup migration only, preserving each existing behavioral assertion. |
| `docs/superpowers/handoffs/2026-10-03-stage-entry-battle-preparation-handoff.md` | Create during P3: actual API names, evidence and independently tracked C/V/D/A statuses. |

No new public Core group member or service key is needed: the public route remains `IInGameCoreSystemGroupService.Combat`. `InGameCoreSystemFactory`, `BufferSystemFactory` and `InGameCoreSystemGroup` should retain their existing composition; change them only if a demonstrated binding need requires it, and document that deviation.

## P1: Prepare and publish selection with the existing battle stores

**Files:** Create `Buffers/Current/BattleEntrySelection.cs` and `Tests/BattleEntryPublicationTests.cs` with metas. Modify `Buffers/Current/CurrentProgressBuffer.cs` and `Buffers/Core/BufferSystemGroup.cs`. Reuse existing `CharacterBattlePreparationTests`, `CombatStateBufferTests` and `StageBufferInteractionTests` regressions.

**Interfaces produced:**

```csharp
public sealed class BattleEntrySelection
{
    public int ExpectedChapterKey { get; }
    public bool ExpectedHasSelection { get; }
    public StageSelectionData ExpectedStage { get; }
    public StageNodeId TargetNodeId { get; }
    public StageKey TargetStageKey { get; }
    public string TargetStageName { get; }
    public BattleEntrySelection(int expectedChapterKey, bool expectedHasSelection,
        StageSelectionData expectedStage, StageNodeId targetNodeId,
        StageKey targetStageKey, string targetStageName);
}
```

Namespace: `ProjectL.Systems.RuntimeSystem.BufferSystems.Current`. Constructor declarations above describe the API, not a compilable implementation. Target chapter is `ExpectedChapterKey`: this operation never changes chapters. Require a positive chapter, nonempty typed target IDs and nonblank target name. When no previous selection is expected, require an empty/default `ExpectedStage`; otherwise require valid previous identity/name.

Add `PreparedCurrentSelection PrepareSelection(BattleEntrySelection selection)` and `void InstallSelection(PreparedCurrentSelection prepared)` to internal `ICurrentProgressBufferStore`. Define internal sealed `PreparedCurrentSelection` in `CurrentProgressBuffer.cs`, with constructor `(StageSelectionData stage, CurrentShopData shop, CurrentRecoveryData recovery)` and get-only properties `Stage`, `Shop`, `Recovery` of those respective types. Compare the expected chapter/selection flag and all selected stage fields, including name; preserve shop/recovery only for the same node/stage, matching current `SelectStage` semantics. `InstallSelection` assigns already-prepared values and `HasSelection = true`; it does not clear anything through another public call.

Add `void PrepareBattle(BattlePreparation preparation, BattleEntrySelection selection)` to `IBattleBufferWriteSink`. Keep the existing one-argument overload for compatibility/low-level fixtures. Both paths reuse the same private battle-validation/preparation work; do not duplicate the Character/Combat validation body.

- [x] Record HEAD/branch/status and incoming hashes under `.superpowers/sdd/2026-10-03-stage-entry-battle-preparation-implementation-plan/`; capture a fresh three-assembly baseline using the command section. Compare it with C5 rather than assuming fixed counts. Do not overwrite unrelated modified files or prior evidence.
- [x] Add failing real-store tests `EntryPublication_SelectsTargetAndInstallsBattle`, `EntryPublication_InvalidProposalPreservesAllStores` (Current/Character/Combat cases), `EntryPublication_ChangedOriginOrNodeBindingRejects`, and `EntryPublication_PreservesOrClearsNoncombatDataBySelectionIdentity`. Use chapter 1, previous node 2/stage 101 and target node 3/stage 102; include a first-entry case with no prior selection. Assert the original Current selection/shop/recovery, Character facts/ownership/souls, Combat completion/result/counters, currency and Artifact snapshot survive failure. Wrong expected selection, a valid-shaped unowned character link, and a malformed command timeline should exercise distinct validation boundaries; confirm each failure's cause rather than counting any thrown exception as coverage.
- [x] Run filter P1 and record RED. A missing new symbol may be the initial compile failure; once declarations exist, require behavioral failures before considering publication implemented.
- [x] Implement the proposal and Current prepare/install members. Share target-value validation with `SelectStage` where useful while retaining its public behavior. Do not add a second Current store registration or copy live state into the Combat service.
- [x] Implement the two-argument writer. Before the first install, validate expected Current facts and that target node/stage binding exists in `StageProgressReadSink.GetStages(ExpectedChapterKey)`; then prepare all three stores. Do not evaluate forward movement, stage clear eligibility or Combat DB type in Buffer. Install Character, Current and Combat prepared values, with no source calls or new ordinary-input failure after installation starts. Existing install-time programmer-invariant guards may remain under serialized execution; do not weaken them to manufacture atomicity.
- [x] Run filter P1; expected all focused cases pass. Ensure the malformed Character/Combat cases preserve the old Current shop/recovery even when selection preparation has already succeeded. Existing public `SelectStage` and one-argument `PrepareBattle` regressions must remain passing. Record changed files and evidence; do not commit automatically.

**P1 completion:** Only the validated publication capability exists. No Core caller or production entry is claimed complete.

Representative success assertions in `EntryPublication_SelectsTargetAndInstallsBattle`, after arranging real stores plus valid `preparation`/`selection` for node 3/stage 102 (`buffers` is the fixture's real `IBufferQuery`):

```csharp
buffers.BattleWriteSink.PrepareBattle(preparation, selection);
Assert.That(buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(1));
Assert.That(buffers.CurrentProgressReadSink.HasSelection, Is.True);
Assert.That(buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(3));
Assert.That(buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
Assert.That(buffers.CurrentProgressReadSink.Shop, Is.Null);
Assert.That(buffers.CurrentProgressReadSink.Recovery, Is.Null);
Assert.That(buffers.CombatStateReadSink.HasBattle, Is.True);
```

Run shop and recovery arrangements separately: Current intentionally forbids installing both at once. In each failure case, additionally compare the captured selection value and shop/recovery references, Character snapshots and allocator's next observable identity, Combat retained results/counters and persistent balances before/after; a `HasBattle == false` assertion alone cannot prove publication safety.

## P2: Deliver explicit-target entry and migrate dependent fixtures together

P2A and P2B form one implementation/review unit. Changing the source signature and restricting compatibility preparation requires migrating the C2/C4 entry helpers to `EnterBattle` in the same change. Do not introduce an intermediate passing checkpoint by preselecting targets in new-entry tests or weakening their failure-preservation assertions. Write the tests from both sections, record RED, implement both sections, then run the combined P2 filter before declaring GREEN.

### P2A: Explicit source identity and compatibility

**Files:** Modify `Core/CombatSystem/CombatProcessingService.cs`, `Core/CombatSystem/DatabaseBattleInitializationSource.cs`, `Tests/BattleDefinitionAdapterTests.cs`, and source doubles/setup in `Tests/CharacterBattlePreparationTests.cs`, `CombatCommandPublicationTests.cs`, `CombatCommonPublicationTests.cs`, `CombatPreviewPublicationTests.cs`, `ArtifactEditingServiceTests.cs`, `CoreStageCombatCycleTests.cs`. Recheck the interface implementer inventory before editing.

**Consumes:** Existing adapter conversion, committed `CurrentProgressReadSink` values and Character facts.

**Produces:** `IBattleInitializationSource.Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey) -> BattleInitialization`. There is one source interface signature, no optional identity or second implicit-source overload. Public `void PrepareBattle()` remains present. Its meaning is preparation for an already-committed current selection, with no navigation or selection writes.

- [x] Add failing `ExplicitSource_UsesRequestedTargetWithoutCurrentSelection`, `ExplicitSource_IgnoresDifferentCurrentStage`, `CompatibilityPreparation_MissingSelectionPreservesState`, and `CompatibilityPreparation_UsesCommittedIdentity`. Record observed query/source identity and unchanged Buffers. The actual default source is expected to reach the unchanged Method_Key rejection for valid fixture data; a supported injected source covers successful preparation. Missing/wrong chapter/type/data must still reject before any write.
- [x] Inventory all source/service implementers and stage-entry helpers. Include P2B tests in the same RED run, then change signatures and test doubles together. Keep existing assertions; seed a committed fixture selection only where tests intentionally exercise the compatibility path. New-entry failure tests instead migrate to `EnterBattle` in P2B.
- [x] Update `DatabaseBattleInitializationSource` to use supplied chapter and `stageKey.Value` with the existing adapter. Remove its Current sink field/binding; keep its existing Framework module/Character binding and missing Method_Key exception. Validate supplied identity; the method must not require any currently selected stage.
- [x] Update `PrepareBattle()` to check bindings, retained-battle guard and committed selection before source lookup, then use its explicit identity. Reuse existing calculation/publication and assign `_definitions` only after successful installation. Keep this wrapper separate from new-stage navigation so it does not reject a legitimately already-selected stage as a fresh click.
- [x] Update uncached `ReadInput` definition loading to require and pass committed identity. Cache only successful source results. Add `DefinitionReload_RequiresCommittedIdentity` and retry assertions; do not silently guess a target for command-only test fixtures. Fixtures with intentionally null sources and fully supplied low-level facts retain their declared scope.
The final P2 run includes the old `CombatPlanTests` expectations and real existing Prepare Job witness in `CharacterBattlePreparationTests`. Production `CombatPrepareJob`, `CombatPlan`, `CombatFlow`, `CombatLifeCycle` and `JobRegisterSystem` remain unchanged. Compilation/old-route execution is not gate V completion.

### P2B: Registered Core entry and integrated verification

**Files:** Modify `Core/CombatSystem/CombatProcessingService.cs`, `Tests/CoreStageCombatCycleTests.cs`, `Tests/BattleDefinitionAdapterTests.cs`. Modify only the `PreparationProbe` interface implementation in `Tests/CombatPlanTests.cs` for compilation; its new unsupported `EnterBattle` member throws `NotSupportedException`, and its existing Job expectations remain unchanged.

**Consumes:** P1 two-argument battle writer, P2 source signature, `IChapterProgressionCoordinator.PrepareStageEntry(int, StageNodeId)` and existing `ChapterProgressionResult`/failure reasons.

**Produces:** `ChapterProgressionResult ICombatProcessingService.EnterBattle(int chapterKey, StageNodeId targetNodeId)` implemented on the existing service. No default interface body or test-only production method.

- [x] Add failing registered tests `EnterBattle_FirstEntryPublishesMatchingTarget`, `EnterBattle_NavigationRejectedBeforeSourceRead`, `EnterBattle_NoncombatTargetFaultsBeforePublication`, `EnterBattle_SourceOrPreparationFailurePreservesStateAndRetries`, `EnterBattle_ChangedOriginOrTargetDoesNotCommit`, `EnterBattle_RetainedBattleRejectsWithoutReinitialization`, and `EnterBattle_NextStageUsesNewDefinitionsAndRetainsResults`. Use the real factories, `FrameworkRuntime.InitData/PostBindService`, real progression and Buffers. Keep query/source doubles local to the existing fixtures. Cover an existing but inactive chapter, backward/unconnected nodes and an already-cleared target as navigation rejections; an absent chapter definition is a configuration fault, not necessarily the same result.
- [x] Give two legal stages distinguishable definitions: stage 101 uses width 8/manual draw cost 2; stage 102 uses width 10/cost 5, both height 6 with valid deployment depths 2/2. Preserve fixture wallet 91. Supply distinct supported attack profiles and assert an observable range/selection difference after entering stage 102, so an old `_definitions` cache cannot pass by matching geometry alone. Use chapter 1 nodes 2 -> 3; a separate next-chapter case retains the existing C4 coverage.
- [x] Run filter P2 and record RED. Adding the public member requires updating `CombatPlanTests.PreparationProbe` in the same compiling change. Do not replace or weaken the old Job's call-count/completion/fault tests.
- [x] Implement `EnterBattle` in this order: validate positive chapter/nonempty node and bindings/source/progression; reject retained battle by exception; capture origin selection; obtain live `PrepareStageEntry` decision; return a normal navigation rejection immediately; verify DB root identity/chapter and Combat type; read the explicit target definition; calculate from current owned/main/soul facts. Source injection must not bypass the service's Combat-type check. Use private helpers inside the service to share preparation work with the compatibility route; do not introduce a new service or change pure calculator APIs.
- [x] Before publication, call progression preflight again. If it rejects, return that rejection. If the origin selection changed, or an accepted decision now identifies a different chapter/node/stage, return `ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch)`. Compare origin values explicitly, not object references. Construct P1's proposal with the captured origin and revalidated target; use the existing `Stage {stageKey}` label convention for storage, not DB/View localization. Publish through P1 and assign the definition cache after success. Return the accepted target decision only after publication; never separately call `ConfirmStageEntry` or public `SelectStage`.
- [x] For controlled source-induced origin/graph changes, assert no additional entry writes relative to the deliberately mutated state and no prepared battle. Do not require the service to undo that external test mutation. Separately test stale publication directly at the real Buffer boundary in P1; do not add a production callback/interleaving hook.
- [x] Migrate C2/C4 new-entry helpers from the old three-call chain to `EnterBattle`. Keep separate compatibility tests for P2. Verify the next-stage cache with actual commands/previews, failed-target correction/retry, once-only draws, HP/soul/Artifact/currency retention, and unchanged result counters. A retained terminal battle must still reject until existing `EndBattle` cleanup occurs. Old unhandled View commands must remain untouched by Core.
- [x] Run filter P2; expected all focused cases pass. No scene transition, new Job, acknowledgement, queue clearing, default DB fallback or roster grant may appear in the diff.

**P2 completion:** The fixture-backed Core API works. P2 alone does not close gate C; its final status requires P3. Gate V remains independent.

Representative assertions for `EnterBattle_FirstEntryPublishesMatchingTarget`, using the existing registered C4 setup before its first stage selection:

```csharp
Assert.That(_buffers.CurrentProgressReadSink.HasSelection, Is.False);
var result = _core.Combat.EnterBattle(1, new StageNodeId(2));
Assert.That(result.Accepted, Is.True);
Assert.That(result.ChapterKey, Is.EqualTo(1));
Assert.That(result.NodeId.Value, Is.EqualTo(2));
Assert.That(result.StageKey.Value, Is.EqualTo(101));
Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(101));
Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.True);
Assert.That(_buffers.CombatStateReadSink.Current.ManualDrawGoldCost, Is.EqualTo(2));
Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
Assert.Throws<InvalidOperationException>(() => _core.Combat.EnterBattle(1, new StageNodeId(2)));
```

Also assert the source spy received `(1, new StageKey(101))` exactly once. The separate next-stage case performs real victory settlement and `EndBattle` before entering node 3, then asserts stage 102, cost 5, width 10 and the new profile's observable preview. Keep existing C4 settlement expectations valid; isolate distinguishable profile variants to the dedicated regression if shared fixture changes would alter those expectations.

## P3: Full regression and Core handoff

**Files:** This plan, the handoff file in the file map and local evidence. Production fixes only for demonstrated defects in P1-P2.

- [x] Audit the diff against all global constraints. Verify no new state owner/service key/assembly dependency or production execution edit. Confirm the Current store extension is storage preparation, the group coordinates installation, and gameplay decisions remain Core-owned. Verify actual factory export, root InitData/PostBindService and same-instance read/write visibility through the P2 registered fixture and existing `FrameworkRuntimeCompositionTests`.
- [x] Run all three EditMode assemblies after final code changes. Compare case-name sets, new/retired witnesses and exact failure names/messages against P1's fresh baseline; expected no new failures, missing or skipped cases. Known Economy failures remain reported. If behavior changes during review, rerun the affected tests and full regression after the final fix.
- [x] Record exact public/internal signatures, changed files, real components versus fixtures, run commands/XML/log/exit codes, incoming-change preservation and default DB limitations in the handoff. State that `ChapterProgressionResult.Accepted` from `EnterBattle` means published entry; it does not mean the Job completed or the lifecycle activated.
- [x] Run a final whole-package review under the selected execution workflow, resolve actionable Core/Buffer defects and check `git diff --check`, unique new GUIDs and preserved unrelated assets. Do not claim an all-green suite if known asset failures remain.
- [x] Close the P1 review's deferred test-evidence improvement: seed a nonzero preview request/answered number, prove failed preparation preserves it and successful preparation retains the answered counter while clearing the preview; include a retained-battle rejection with a populated preview. Existing P1 tests compare these values only at zero/null; production inspection found no defect.
- [x] Mark gate C complete only with P1-P3 evidence. Leave V, D and A explicitly pending unless separately supplied owner evidence proves each. Include the exact external checklist below; do not delegate its implementation to Core by implication.

**P3 completion:** Core/Buffer contract delivered; external caller migration and production readiness statuses remain separate and visible.

## Gate V: View and Flow migration handoff, not Core implementation

The user does not own the combat Jobs. The following checklist belongs to their execution owner. Relevant current files: `CombatExecution/Jobs/CombatPrepareJob.cs`, `Plans/CombatPlan.cs`, `CombatFlow.cs`, `CombatLifeCycle.cs`; registration is `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`. Its current `Register(JobRootType, JobType)` has no chapter/node payload, and `CombatPlan.Register(service, jobType)` cannot supply one today. Do not describe migration as only replacing a single call.

- [ ] V1: The execution owner defines and implements target transport through its existing registration/Plan/Job path. Capture chapter/node intent until execution; do not store an intended target as committed Current selection, read a later unrelated click, add a Core queue, or bypass the Framework registration route with a competing runner. Any necessary shared Framework contract extension belongs to that owner review.
- [ ] V2: The selected entry Job calls `CoreGroup.Combat.EnterBattle(chapterKey, nodeId)` once and handles accepted, rejected and thrown outcomes distinctly. Normal return, including rejection, completes that Job once; only acceptance permits active Combat. The owner defines how the returned outcome reaches Flow; `HasBattle` alone must not turn a rejection, fault or an unrelated retained battle into success. Do not reuse a retained combat-command result/sequence as an entry acknowledgement.
- [ ] V3: Migrate lifecycle startup coherently. After accepted entry, activate the prepared battle without `OnEnter -> BeginPreparation -> old PrepareBattle()` running again. If scene resources load earlier, gate input and active processing until acceptance. Remove the redundant Combat-side confirmation call; Core has already published selection. Keep noncombat callers distinct.
- [ ] V4: Define cancellation/replacement and failure handling before activation. A stale request must not become an automatic retry; an accepted entry followed by scene activation failure must not be silently replayed or repaired with unrestricted resets. Stop/drain old work and clear old input through existing external-owner permissions. Framework F1 and View/Flow F2 remain necessary production gates.
- [ ] V5: Prove the actual `Register -> Plan -> Runner -> Job -> Core -> Buffer -> Flow` path with distinct first/next targets, normal rejection, fault, duplicate registration prevention and lifecycle startup exactly once. Then prove authored-data/scene acceptance under D/A. Fake Job tests or P2 direct service calls are insufficient for V.

Core may supply service/Buffer evidence and answer contract questions. It must not mark V complete by implementing look-alike Jobs inside Core. Existing `CombatCommandJob`, `CombatPreviewJob` and `CombatAdvanceJob` service wiring remains the execution owner's broader handoff, outside P1-P3.

## Verification commands

Use Unity 6000.0.73f1, one Editor/test process per checkout. If an Editor owns the checkout, use its established test route; do not terminate the user's Editor. Keep evidence outside Assets and use the established licensed user-profile launch when required.

```powershell
$entryEvidence = Join-Path (Get-Location) '.superpowers/sdd/2026-10-03-stage-entry-battle-preparation-implementation-plan'
New-Item -ItemType Directory -Force -Path $entryEvidence | Out-Null
$runName = 'p1-baseline'
$entryArgs = @('-batchmode', '-nographics', '-projectPath', (Get-Location).Path,
    '-runTests', '-testPlatform', 'EditMode',
    '-assemblyNames', 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary',
    '-testResults', (Join-Path $entryEvidence "$runName.xml"),
    '-logFile', (Join-Path $entryEvidence "$runName.log"))
$entryArgs | Set-Content (Join-Path $entryEvidence "$runName.args.txt")
$entryProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -ArgumentList $entryArgs -WindowStyle Hidden -PassThru
$entryProcess.WaitForExit()
$entryProcess.ExitCode | Set-Content (Join-Path $entryEvidence "$runName.exit-code.txt")
```

For each RED/GREEN focused run, use a distinct run name, `-assemblyNames ProjectL.Tests.Framework`, and append `-testFilter` with the following fully qualified class names separated by semicolons. All names below have prefix `ProjectL.Tests.Framework.`:

| Filter | Fixtures |
| --- | --- |
| P1 | BattleEntryPublicationTests; CharacterBattlePreparationTests; CombatStateBufferTests; StageBufferInteractionTests; ChapterGraphPreparationTests |
| P2 | CoreStageCombatCycleTests; BattleDefinitionAdapterTests; BattleEntryPublicationTests; ChapterGraphPreparationTests; CharacterBattlePreparationTests; CombatCommandPublicationTests; CombatCommonPublicationTests; CombatPreviewPublicationTests; ArtifactEditingServiceTests; CombatPlanTests; FrameworkRuntimeCompositionTests |

Expected focused GREEN: all selected cases discovered and passing, zero skipped/inconclusive, exit 0. Expected full regression: no new failures against the freshly measured baseline; explicitly record unchanged external failures and exit code. Zero discovered cases, compilation failures or missing old witnesses never count as success. Preserve original importer-touched bytes only after confirming the delta is incidental and not a concurrent user edit.

## Plan self-review and execution handoff

Spec coverage: explicit identity/compatibility/cache -> P2; first/next target and live navigation -> P2; three-store publication/failure preservation -> P1/P2; Framework binding and regressions -> P3; Job/Flow transport and one-time activation -> separate V1-V5; actual source readiness -> D; final production activation -> A. No approved scope is silently treated as completed by another gate.

Implement P1 -> P2 -> P3 sequentially. Inline execution is recommended because the source, publication and service signatures are tightly coupled; use one final independent review rather than parallel edits to the same service/fixtures. This recommendation does not start implementation. Review this plan and select/confirm the execution method before code work. No commit, push or production activation is authorized by the existence of this plan.
