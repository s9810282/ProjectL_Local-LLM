# Buffer-Authoritative Chapter and Map Progression Implementation Plan

Date: 2026-09-23
Revision: 5 — one active local work branch, explicit Core.Application invocation boundary.
**Goal:** Migrate one chapter's progression authority to existing Buffers, from initial entry through boss and chapter clear. Next-chapter unlocking/transition, the final V3 execution architecture, and combat-state migration are outside this increment.

**Spec:** [Chapter/map Buffer ownership design, revision 5](../specs/2026-09-22-run-map-buffer-ownership-framework-integration-design.md).

**Status and branch:** `codex/chapter-map-task3` is the single active local task branch. Task 3 (`1c5bb8b2`) and Task 4 (`8a7fad3a`) are committed there. Their separate focused managed NUnit runs passed 35/35 and 17/17; neither was a Unity Editor, real Buffer, Job, or View integration run. Task 5's source review and written command/Buffer/Job mapping are in the [Task 5 handoff](../handoffs/2026-09-27-chapter-map-task5-buffer-job-review.md); production integration remains for Tasks 6–8. Historical Task 1/2 commits `236e8ebd` and `7d87678f` remain recoverable under local tag `archive/run-map-task1-2-20260927` but are **not** merged, cherry-picked, or part of the active production path. Their 19/19 and 58/58 managed results are historical evidence only. No remote push is part of this consolidation.

## 1. Governing constraints and current source

**Controlling correction, 2026-09-28:** Buffer owns initialization scope and record retention. The unauthorized Core-owned InitializeRun implementation was removed; the 2026-09-27 clarification below records gameplay intent, not authority to compose Buffer resets in Core. No Run version, persistent termination flag, stale-request protocol, or battle-state migration is introduced as a prerequisite. PrepareStageEntry/ConfirmStageEntry replace validation-only entry recording. [Correction and pending Buffer API proposal](../handoffs/2026-09-28-buffer-contract-correction.md) take precedence over earlier conflicting handoff descriptions. Job/LifeCycle/View runtime connection remains incomplete.

**User clarification, 2026-09-27:** Defeat starts a fresh run with a newly generated map and first-node selection; permanent Artifact ownership survives. The user authorized a Core-only initialization function first. See [Core run initialization](../handoffs/2026-09-27-core-run-initialization.md). This supersedes the earlier exclusion of Core restart preparation and the proposed CurrentProgressBuffer terminal flag. Job/LifeCycle/Framework edits still require the user's approval; complete playable restart remains outside this implementation step.

Read the [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) first, then [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [development guidelines](../framework/Framework%20Development%20Guidelines.md), [overview](../framework/01_Framework_Overview.md), [services](../framework/02_Framework_Services.md), and [system creation guide](../framework/03_System_Creation_Registration_Query.md). Check relevant source immediately before implementation. Source/document disagreements require owner clarification.

- Current/Stage/Map Buffers are the runtime progression authority. `CurrentStage` is the last entered stage after an accepted node click, not a separate highlight state in this flow. Core holds no second mutable map graph, position, or clear records.
- View sends chapter/node/combat intent and reads Buffer results. It does not query DB to assemble input, provide Core snapshots, order Jobs, or write progression state.
- The Systems CoreGroup progression coordinator binds published Buffer services via `PostBindService`, passes plain command-local values to a Core.Application entry point, and applies accepted Buffer effects. Core.Application coordinates a game command and delegates individual rules to Core.Domain; neither is registered as a Framework service or Job. Systems/LifeCycle Plan and JobRunner handle only commands that actually need ordered or deferred Job execution.
- `Core.Contracts` has no Framework/Systems/Unity dependency. `Core.Domain` references Contracts, and `Core.Application` references Domain and Contracts. Core.Domain/Application do not reference Systems Buffer interfaces. Systems currently references Contracts only. For the integrated command boundary, prefer an **explicit one-way** `ProjectL.Systems → ProjectL.Core.Application` asmdef reference and a public Application API that accepts/returns Contracts values. Do not add a direct Systems → Domain reference merely to bypass Application or add a no-op Application wrapper. Record any changed dependency in the README and Framework architecture docs; do not change the generic Framework assembly.
- The requester can edit InGame/CoreSystem, including CoreCaculator. A narrow Factory and Systems asmdef change is in scope when justified and consistent with Framework registration rules. View is owned by another worker. Keep Buffer structure and Framework internals unchanged unless a concrete missing capability requires owner review.
- Framework lifetime phases are one-time phases. `InitData` supplies DB Query; `PostBindService` binds the service reader. A chapter entry must not repeat either phase or rebuild service registration.
- Use existing Buffer APIs first. This increment has one active chapter; boss clear completes it, and no unfinished-chapter switch/return or per-chapter entry history is required. Do not add Run ID, revision, restart history, separate completion record, generic lock/transaction, new Buffer, or Framework-wide change without an observed requirement.
- Keep implementation documentation in English and preserve Unity `.meta` GUIDs. Run only focused, meaningful checks for changed code.

Checked source on this revision: `CurrentProgressBuffer.SelectMap` clears `CurrentStage` when the map key changes, returns without resetting it for the same key, and does not clear Stage/Map records; `StageProgressBuffer` holds the node graph and clear flags; `MapProgressBuffer` holds chapter clear. `StageGeneratorSystem.InitData` captures DB Query, while `Generate(mapKey)` separately registers the graph after service binding. Its `RegisterStages` rejects duplicate chapter registration. `StageProgressBuffer.Clear` and `MapProgressBuffer.Clear` erase registrations, so neither is an ordinary chapter-entry operation. `StageProgressData` lacks node display names and execution kinds; `CurrentStage.StageName` covers the entered stage only. `StageServiceManager.Enter(stageKey)` currently resolves and validates stage data but does not start playable content. Current Job execution contains scaffolding, not a complete gameplay flow.

## 2. Contract change and decision points

The accepted flow for this increment is **initial chapter entry → immediate stage entry on an eligible node click → stage victory or attempt-ending defeat → choose one forward-connected next-depth stage → boss victory → boss-node and chapter clear**. A player cannot move backward, choose another node at an already entered depth, or enter a cleared stage. A later chapter becomes playable only after completion, but the next-chapter rule and transition are deferred. Graph edges, rather than a second Core graph, define reachability. Entry never marks a stage clear; victory does.

| Former plan item | Decision under this contract |
|---|---|
| BR-01: extend Buffer with Run ID, revision, phase, receipt, and new aggregate | Withdraw as a prerequisite. Inspect actual screen/command needs; ask only for a missing field or operation in an existing Buffer. |
| BR-02: adapter and composition seam | Use a Systems CoreGroup progression coordinator bound through `PostBindService`. Task 5 defines the Core.Application command API; Task 6 may explicitly add the one-way Systems → Core.Application asmdef reference and normal Factory registration. No generic Framework change is assumed. |
| BR-03: all-or-nothing expected-revision commit as a hard gate | Superseded. Do not introduce an optimistic Run commit or broad transaction protocol. Concrete multi-Buffer commands still cannot ship with an unresolved partial-failure path; validate those paths and request a narrow owner-side remedy only when needed. |
| BR-04: explicit Run start/end/restart and Run-scoped map clears | Withdraw. Use the existing `SelectMap` for initial chapter entry and preserve registered Stage/Map records. Do not invent a chapter-reset or per-chapter cursor API for this scope. |

The owner confirmation and clarified game flow settle ownership, not implementation. The requester has clarified CoreSystem and justified Factory/asmdef edit authority; View changes remain with its owner. The Systems coordinator, Application entry point, graph-generation timing, display fields, and multi-Buffer write behavior still require implementation or checking.

## 3. File and responsibility map

`C/` means `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/`. `S/` means `Assets/ProjectL/Script/Systems/`. `T/` means `Assets/Tests/EditMode/`.

| Area | Existing files to inspect/adapt | Result |
|---|---|---|
| Navigation rules | `C/Domain/StageMap/StageMapRunState.cs`, `StageMapSelectionPlan.cs`; `C/Application/StageMap/StageMapSession.cs`, `StageMapSessionFactory.cs`, `StageMapSnapshotBuilder.cs` | Rule evaluation consumes Buffer-supplied graph/entered-node/clear values. No integrated mutable Core graph or position. Preserve a standalone path only if a real caller needs it. |
| Progression commands | `C/Application/Run/RunProgressionService.cs`, `C/Domain/Run/RunProgressState.cs`; Task 3/4 rule/results and relevant `C/Contracts` types | Add a narrow Application command entry point that uses existing Domain rules and returns Contracts results. Keep Buffer execution in Systems. Do not use the legacy stateful Run service or carry obsolete Run ID/revision into the integrated path. |
| Related commands | `C/Application/Run/RunShopService.cs`, `RunRecoveryService.cs`, `RunWalletService.cs`, `RunCharacterDeckService.cs`; `C/Application/Artifact/StageArtifactLifecycleService.cs`, `StageArtifactLoadoutSession.cs` | Keep current ownership/invariants when a command also changes progression. Failed Artifact release retains cleanup ownership and blocks duplicate acquisition. |
| Existing Buffers | `S/RuntimeSystem/BufferSystems/{Current,Stage,Map}/` | Use published read/write sinks; owner changes only for proven missing data/operations. |
| Preparation and composition | `S/RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs`, `S/RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs`, `S/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`, `S/ProjectL.Systems.asmdef` | Prepare DB-derived graph into Buffer; bind one coordinator within CoreGroup through the existing Factory and lifecycle rules. Add an explicit Systems → Core.Application reference only when its public API is ready. |
| Job/Flow and View | `S/RuntimeSystem/GlobalSystem/JobSystem/`, relevant Plan/Job and View callers | Use Job execution where the command needs ordered or deferred work; simple map navigation need not become a Job by default. View reads Buffer only. Owner-controlled work, not assumed complete. |

**Task 1/2 disposition:** The historical `IRunProgressAccess`, `RunProgressRecord`, `RunMapRecord`, `RunMapDefinition`, `RunMapNodeDefinition`, `RunWriteStatus`, and their test provider/fixtures are superseded. The Task 2 Buffer-backed `StageMapSession` constructor and its supporting edits/tests depend on those contracts and are likewise excluded. None of these additions is present on the active task branch. The existing standalone `StageMapSession` and `StageMapRunState` remain from the base code for existing callers; they are not the integrated Buffer authority. Task 3's `ChapterNodeProgress` and stateless navigation rules replace the relevant plain graph/rule preparation, while Task 4 describes accepted Buffer effects. Preserve Task 1/2 commits as local historical evidence, without merging their older repository base or copying their obsolete production API.

## 4. Execution order and gates

Maintain Task numbering for traceability. Tasks 1 and 2 describe superseded work preserved only as historical commits. Tasks 3 and 4 are implemented on the single active local branch. Tasks 5–8 remain; no Task 1/2 merge or replacement implementation is required.

### Task 1: Historical immutable contracts and test provider — completed locally, superseded as a production contract

Commit `236e8ebd` added immutable Run/map values, `IRunProgressAccess`, a test provider, and 19 passing managed NUnit cases. Its Run ID/revision/receipt model and test provider are **not retained in the active implementation**. The current one-chapter contract does not need them; the commit remains local historical evidence, not a pending merge.

### Task 2: Historical read-only map session — completed locally, superseded as a production path

Commit `7d87678f` added a map session that reads through the Task 1 port and passed 58 focused managed NUnit cases. That constructor and its supporting production/test edits are **not retained in the active implementation**. The useful rule that previews must not mutate progress is carried forward as a behavior requirement, not as a copy of the old code. Neither historical task ran in Unity or reached real Buffers.

### Task 3: Establish the real chapter/navigation rule boundary — Core only

**Status: Core-only scope completed on local `codex/chapter-map-task3`.** Four new C# files plus Unity metadata provide a command-local node value, a decision result, stateless navigation rules, and six focused tests. The Core assemblies and Core test assembly compiled; **35/35 focused managed NUnit cases passed** (six new, 29 existing map cases). Unity Editor/EditMode, real Buffer, PostBindService, Job, View, and runtime bootstrap were not tested. Its `fromNodeId` remains a caller-supplied input; for this one-chapter flow the caller uses `CurrentStage.NodeId` after entry, or the start sentinel before the first entry. No Task 1/2 code was merged.

**Entry:** Read the actual Current/Stage/Map sink contracts and the current Core entry methods. Identify the minimum command inputs: current chapter, last entered node, graph edges/keys, node clear flags, and chapter clear. Verify whether names/kinds are needed for rule evaluation or View display. Distinguish facts supplied by Buffer from temporary Core calculation values.

- [x] Define plain node input and decision result without Run ID/version/closed state or a generic storage port.
- [x] Add stateless validation for chapter/graph key agreement, uncleared adjacent targets, and a cleared source stage. The result proposes entry without mutating `StageMapRunState` or a Core-owned clear set.
- [x] Keep `StageMapRunState` for existing standalone callers; replace its direct mutation/snapshot use in the integrated path when that path exists. No integrated path was wired in Task 3.
- [x] Run focused cases for eligible next node, cleared-node denial, nonadjacent/unknown node, repeated stage keys, and Buffer-shaped retained clear inputs. No real Buffer lifecycle was tested.

**Exit:** Core rule calculation takes supplied values, emits an accepted/rejected result, and owns no integrated mutable map position. An accepted click is immediate stage entry; `CurrentStage` becomes the entered-node source for the following command. No per-chapter cursor is required for this one-chapter flow.

### Task 4: Prepare progression and resource rules for command execution — Core only

**Status: Core-only preparation completed on local `codex/chapter-map-task3`.** `ChapterProgressionRules` and `ChapterProgressionResult` describe the accepted chapter-entry, node-click, victory, defeat, and boss-victory effects from caller-supplied values. They do not write Buffers, execute Jobs, dispatch a stage, or replace the legacy Run service. The boss classification remains a caller-supplied fact; the rule rejects a claimed boss node that has successors. Defeat reports an attempt-ending outcome without inventing a Buffer field; preventing a repeated defeat dispatch remains a Task 5/7 execution concern. Artifact lease release exceptions now retain the handle and put the stage lifecycle into a fail-closed pending state. The current lease interface has no authoritative release-confirmation API, so pending cleanup cannot be cleared by a blind retry; the owner must retain the stage and resolve that contract before reuse or transfer. **17/17 focused managed NUnit cases passed**, including the new progression and release-failure cases and existing navigation/Artifact lifetime cases. Unity Editor/EditMode, real Buffer, PostBindService, Job, View, and runtime bootstrap were not tested.

**Dependencies:** Task 3's real command inputs; preserve existing Core behavior while separating calculation from storage effects.

- [x] Map initial chapter entry, immediate stage entry on node click, victory, attempt-ending defeat, and boss victory to Core rule results. Entry does not set `IsClear`; victory does. Boss victory requests both boss-stage and map clear. Do not add next-chapter transition logic.
- [x] Preserve combat handoff, checkpoint/live health distinction, purchase/recovery rules, and repeated-command behavior where those features exist. Avoid a new Run receipt/history field unless a concrete repeated-execution path requires it.
- [x] If Artifact release/disposal fails or its outcome is uncertain, retain the handle and cleanup ownership in the existing resource owner. Block reacquisition for that owner until release is confirmed; an acknowledged ownership transfer must carry the pending resource and block. Do not treat disposal flags or retry as proof of release.
- [x] Test only affected command rules and the concrete resource-failure path. Do not design a global transaction or rollback around an unobserved failure.

**Exit:** Core can describe each accepted transition and its required Buffer effects without applying a parallel stored Run/map state. Resource failure cannot silently create duplicate live resources.

### Task 5: Define concrete Buffer write and Job handoff checks — Core preparation and integration review

**Status: source review and written mapping completed on local `codex/chapter-map-task3`.** The [Task 5 handoff](../handoffs/2026-09-27-chapter-map-task5-buffer-job-review.md) records the proposed Contracts-only Application command boundary, exact Task 6 composition files, Buffer write order, Job replay rule, and remaining owner questions. No production integration or Unity/runtime test is claimed. The current `StageServiceManager.Enter` validation-only handler still blocks playable entry. A stable temporary label can satisfy `SelectStage`'s name argument. The separate DB-SO branch already adds `MapRole` to `StageProgressData` and has the generator populate it, but that commit is not yet in this branch; verify the integrated Buffer API before deriving boss victory from it.

**Dependencies:** Task 4's effect list. CoreSystem and justified Factory/Systems asmdef edits are in scope; demonstrated Buffer or generic Framework changes need owner review, and View work stays with its worker.

- [x] Define the smallest Core.Application command-facing entry point for chapter entry, node click, victory, defeat, and boss victory. It must consume command-local Contracts values, use the Task 3/4 Domain rules, return Contracts decisions/effects, and retain no parallel Run/map state. Do not add a pass-through wrapper solely to justify an asmdef reference; retain direct Domain calls as a documented alternative if no Application-level command composition exists.
- [x] Confirm the one-way `ProjectL.Systems → ProjectL.Core.Application` reference has no cycle and that its public API exposes no Domain or Systems Buffer types. Record the exact Factory, CoreGroup, service-key, and asmdef edits needed in Task 6. Framework itself must not gain a Core reference.
- [x] Trace the actual Buffer writes for initial `SelectMap`, accepted `SelectStage` entry, stage victory, and boss victory. An accepted click must not record `CurrentStage` before a failing stage dispatch; check `StageServiceManager.Enter` and any later handler/callback against the `SelectStage` write order. Verify all inputs before the first write and identify any possible exception/callback after a preceding write. For boss victory, check the `Stage.SetClear` plus `Map.SetClear` sequence specifically.
- [x] Trace any command that also changes combat, wallet, deck, shop, recovery, or Artifact ownership. Retain existing invariants; require an owner-approved narrow coordination change only where a concrete partial-failure path remains.
- [x] Decide which accepted results can be applied synchronously by the coordinator and which require the existing Systems/LifeCycle Plan/Job model. For Job-backed commands, check `JobRunnerSystem` behavior: an incomplete Job may execute again on a later Tick. A completed command must not apply Buffer effects twice.
- [x] Do not equate synchronous execution with atomicity. An unresolved partial-failure path is a production blocker for that command, without reinstating the old BR-03 expected-revision protocol.

**Exit:** A written command-to-Application-to-Buffer/Job mapping identifies exact files, write ordering, and any concrete Buffer/Framework owner question. No production integration is claimed by this preparation.

### Task 6: Bind the real Buffers and DB-prepared graph — CoreSystem integration

**Dependencies:** Task 5 mapping and the clarified edit boundary. CoreSystem and justified Factory/Systems asmdef changes are permitted; View remains separately owned, while any demonstrated Buffer or generic Framework change needs owner review.

- [ ] Add the Task 5 Core.Application entry point, then bind Current/Stage/Map published sinks in one Systems CoreGroup coordinator through `PostBindService`. Make the one-way Systems → Core.Application asmdef reference explicit if that entry point is chosen; do not add a hidden Domain dependency or register Core.Application/Domain as Framework services.
- [ ] Use the DB Query in the approved generator/Core preparation path to register graph data into `StageProgressBuffer` before View needs it. Current `Generate(mapKey)` is a separate call after `InitData`/`PostBindService`; record the chosen timing instead of claiming generation is automatic.
- [ ] Register or reuse the one chapter graph, call `SelectMap` for initial entry, and call `SelectStage` only after an accepted node click. Preserve stage/map clear flags; do not use destructive `Clear()` as an entry operation. Request an existing-Buffer change only for a demonstrated gap in this flow.
- [ ] Add only demonstrated missing display data to the appropriate existing Buffer. Check stage names for all visible nodes and stage execution kind against actual screen/rule needs.
- [ ] Verify service registration separately from lifecycle module registration and parent forwarding. Build once and respect the existing runtime phase order.

**Exit:** Real Buffer-backed chapter/map data is available through the approved Systems path, with no duplicate Core authority. A focused owner-side composition check establishes actual binding; Core test doubles alone do not.

### Task 7: Connect command, Job, and View flow — CoreSystem plus View owner

**Dependencies:** Task 6 and closure of any Task 5 partial-failure blocker for the command being enabled.

- [ ] Route View intent to the progression coordinator. View reads Buffer for map, entered stage, stage clears, and chapter completion. Remove any integrated View path that queries DB for gameplay display data or accepts a Core-built progression snapshot.
- [ ] Execute only commands that require Jobs through the actual Systems/LifeCycle Plan and Job registration/runner path. Connect the approved stage-entry handler/flow after Core accepts the node; current `StageServiceManager.Enter` only validates stage data. Define when each Job-backed command finishes and ensure repeated `Execute()` does not repeat an already accepted Buffer write.
- [ ] Apply accepted stage and boss completion through the approved Buffer API. Do not add unfinished-chapter re-entry, next-chapter transition, Run restart, or per-Run teardown to this increment.
- [ ] Verify the current source's incomplete bootstrap/Flow areas explicitly; do not infer full playable integration from service composition alone.

**Exit:** One command path runs from View intent through Core.Application/Domain rule work to Buffer result and back to View display under the real runtime. View-owned edits require its worker; no generic Framework change is assumed.

### Task 8: Focused integration verification and cutover

**Audit status, 2026-09-27:** [Task 8 audit](../handoffs/2026-09-27-chapter-map-task8-audit.md) records fresh Core results (20/20) and Unity EditMode results (15/17). Two real-Buffer regressions reproduce premature entry recording and victory accepted after defeat. Task 7 execution and Task 8 production cutover remain open. The existing-Buffer terminal-state correction requires user approval; no View or Framework implementation change was made during this audit.

**Dependencies:** Tasks 3–7 and owner-controlled implementation for the production path.

- [ ] Verify initial chapter entry, immediate entry into an eligible next-depth node, rejection of same-depth/backward/cleared nodes, stage victory, attempt-ending defeat, boss victory, and the chapter-complete record using real Buffers. Do not infer next-chapter unlocking from a stored completion flag.
- [ ] Verify no integrated `StageMapRunState` or View/Core snapshot acts as another authoritative graph/progression store. Search all current callers before removing obsolete APIs.
- [ ] Verify concrete boss-clear plus map-clear failure handling and any affected combat/resource commands. Stop only the production command path whose partial failure remains unresolved.
- [ ] Run focused Core and owner-side composition tests, then the minimum Unity Editor/runtime check needed for changed integration. Report managed tests, Unity EditMode, and playable runtime evidence separately. Unity 6000.0.73f1 may need the approved AppData-access execution path; no cache deletion or upgrade is implied.
- [ ] Update the handoff with exact owner decisions, changed files, test evidence, and any still-unconnected runtime path. Cut over only the verified caller paths.

**Exit:** Buffer is the single progression source used by the live chapter/map path; documented remaining gaps are outside that path or explicitly block its release.

## 5. Current decision ledger

The clarified game flow settles `CurrentStage` as the entered stage for accepted clicks and removes unfinished-chapter switching and per-chapter cursor history from this increment. Before production integration, confirm graph registration timing, actual Buffer-backed display metadata, the coordinator-to-Core.Application call path, and concrete multi-Buffer failure behavior. These checks determine whether a **small** existing-Buffer or Systems API adjustment is needed. They are not grounds to restore the former Run aggregate by default. `MapProgressBuffer` stores completion, not a next-chapter transition policy.

Historical Task 1/2 commits remain locally recoverable through tag `archive/run-map-task1-2-20260927` after retiring their separate work branch. This plan supersedes their former production dependency chain; no Task 1/2 code should be merged into the active branch by default. The base worktree's unrelated uncommitted changes are outside this consolidation, and no remote branch is updated.
