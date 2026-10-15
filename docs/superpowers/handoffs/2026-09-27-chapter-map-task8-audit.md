# Task 8: Integration and Logic Audit

Date: 2026-09-27

Branch: `codex/chapter-map-task3`

Reviewed baseline: `7f4b9901`, following Task 6 composition and Task 7 command integration.

## Status

**2026-09-28 correction:** [Buffer contract correction](2026-09-28-buffer-contract-correction.md) supersedes the mandatory attempt-termination gate and the Core-owned reset implementation discussed below. Preparation and explicit entry confirmation are now separate. Earlier counts and conclusions below are historical audit records, not the current release status. No complete runtime integration is claimed.

**Later user clarification:** Defeat resets the run and regenerates its map before first-node selection. The CurrentProgressBuffer termination extension proposed below is superseded, not an outstanding approval request. See [Core run initialization](2026-09-27-core-run-initialization.md) for the implemented Core preparation boundary and remaining runtime integration.

Task 8 verification found two reproducible release blockers. Production cutover is not complete. Task 6's graph preparation and service composition remain verified; Task 7's playable entry and attempt-ending execution do not satisfy the approved design. This is not solely a missing View hookup or a missing DB asset.

The governing contract remains the revision 5 implementation plan and linked design, subject to the Framework documentation and the user's current ownership restrictions. The earlier Task 7 handoff incorrectly treated validation-only dispatch as successful entry. That interpretation is withdrawn.

## Reproduced blockers

### 1. CurrentStage is written before playable entry

`ChapterProgressionCoordinator.EnterStage` calls `StageServiceManager.Enter` and immediately calls `CurrentProgressBuffer.SelectStage`. `StageServiceCollection` only validates stage data; it does not start Combat, Shop, or Event content. Successful validation therefore produces a false entered-stage record.

`Audit_UnwiredStageHandlerMustNotRecordPlayableEntry` uses the real factories, DB fixture, and Buffers. It fails because `HasSelection` becomes true without playable entry.

Required correction: separate preparation/validation from actual entry success. Keep the `SelectStage` write after confirmed playable entry. An unwired handler must reject entry without writing Current. Connect actual Core stage execution before claiming playable completion; scene-loading success alone is insufficient. View changes are not required to reproduce or correct the false success boundary. Any necessary LifeCycle/Job edits outside CoreSystem require separate approval and a concrete diff.

### 2. Defeat does not end the active attempt

`ChapterProgressionCoordinator.ResolveStage` returns `EndAttempt` but stores no terminal state and invokes no terminal owner. Sending Victory after Defeat for the same entered node is accepted and clears the stage.

`Audit_DefeatMustPreventLaterVictoryForSameAttempt` reproduces this using real Buffers. The result is accepted where the contract requires rejection. The existing defeat test only checks the returned flag and unchanged clear flags immediately after defeat; it does not prove attempt termination.

Initial candidate (not approved; see the termination ownership follow-up below) would extend this existing Buffer file:

`Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentProgressBuffer.cs`

Proposed minimal contract:

- Add an authoritative `IsAttemptEnded` read property and an idempotent `EndAttempt()` write operation to its existing sinks and implementation.
- Ending an attempt preserves the selected stage, graph, and stage/map clear records. It does not clear or regenerate progression.
- The coordinator rejects chapter entry, stage entry, and outcome commands after terminal defeat. A same-map selection must not reset termination. Direct `SelectMap`/`SelectStage` writes must not silently reopen an ended attempt.
- Existing explicit `Clear()` resets Current state including termination; no new restart feature is introduced or invoked by this increment.
- Core command inputs/results consume this Buffer fact without adding a coordinator-owned mutable flag, Run ID, revision, transaction mechanism, or a new Buffer.

This is a proposal, not an implemented or approved Buffer change. It is needed because the demonstrated terminal-state gap cannot be fixed by returning another unconsumed result flag.

## Other reviewed paths

| Area | Evidence and conclusion |
| --- | --- |
| Authority and dependency direction | Coordinator snapshots are command-local. The integrated path does not instantiate `StageMapRunState`, `StageMapSession`, or `RunProgressionService`. Systems references Core.Application and Contracts; Application references Domain and Contracts. No second integrated mutable graph was found. |
| Framework composition | DB Query is captured during InitData, Buffer sinks during PostBindService, and graph generation occurs explicitly at chapter entry. Entry does not repeat Framework lifetime phases. Existing composition tests pass. |
| Navigation | Domain rules check graph/chapter agreement, eligible adjacency and depth, cleared source, and uncleared target. Focused Core tests cover rejection cases; this does not establish playable entry. |
| Generation strategy | `Nearest`, `Random`, and `WeightedRandom` are selectable and each passes the real DB fixture graph-generation test. This proves supported selection and basic graph invariants for the fixture, not exhaustive random-seed coverage. |
| Boss plus chapter clear | Current coordinator resolves map/node and rule validity before the first write. The current synchronous Stage/Map setters have no callbacks between writes; Map.SetClear re-reads the already-validated map and replaces its record. No ordinary second-write validation failure was found under this single-threaded path. This is not a general transaction or concurrency guarantee. |
| Boss metadata | This branch reads immutable DB `StageRootData.MapRole` at outcome time. The separate DB-SO work adds role/type metadata to Stage Buffer records, but it is not integrated here. Recheck the Buffer contract when that work is approved for integration; do not claim the final metadata handoff is complete. |
| Runtime routing | StageLifeCycle's entry hook and StageJob remain placeholders without a selected-stage payload. No View-to-coordinator-to-playable-content round trip was demonstrated. A generic new loading system is not justified by this finding. |
| Outcome identity | ResolveStage accepts chapter and outcome and resolves the currently selected node. Before connecting delayed completion callbacks, bind them to the originating node/stage and reject stale results. This is an integration risk identified by source review, not a reproduced live asynchronous failure. |
| Production DB assets | The checked-in StageMapChapter1 asset references a script GUID not found among this checkout's ProjectL script metadata. Fixture-based evidence does not prove the production asset is usable. Do not merge the separate DB-SO branch without authorization. |

## Legacy Core retention

Caller searches found no production caller outside CoreCaculator for the legacy RunProgressionService, StageMapSession, and StageMapRunState path. However, RunProgressionService still coordinates wallet, deck, shop, recovery, health, and stage resources; the new coordinator does not replace those responsibilities. Existing legacy tests and supporting Core types depend on that path.

Do not delete the entire Run/map family on the basis of external caller absence. The approved design explicitly preserves these resource owners until their responsibilities are migrated. No obsolete-code removal was justified during this audit. This retention does not make legacy state authoritative in the new coordinator path.

## Verification evidence

- Fresh focused managed Core run: **20 passed, 0 failed**. Fixtures: ChapterProgressionRules, ChapterNavigationRules, ChapterProgressionCommands, ArtifactReleaseFailure, and ArtifactStageLifetime.
- Fresh Unity 6000.0.73f1 EditMode run: **17 total, 15 passed, 2 failed**. ChapterGraphPreparation: 8 existing cases passed, 2 added audit regressions failed. FrameworkRuntimeComposition: 7 passed.
- The two failing tests are intentionally retained as open defect evidence, not disabled or presented as passing. Existing entry tests describe the current incorrect boundary and must be revised alongside its correction.
- Real in-memory DB SO fixtures and real Buffer/factory runtime composition were exercised. No production-asset playthrough, View test, player build, or end-to-end playable run is claimed.
- No full-repository test suite was run. This audit covers the plan's progression path, its callers, ownership, and adjacent resource responsibilities.

Local Unity artifacts:

- `C:/CodexHome/visualizations/2026/09/27/01a0e23d-3048-7a21-9127-a1fea325480a/task8-audit.xml`
- `C:/CodexHome/visualizations/2026/09/27/01a0e23d-3048-7a21-9127-a1fea325480a/task8-audit.log`

Reproduction: run Unity EditMode fixtures `ProjectL.Tests.Framework.ChapterGraphPreparationTests` and `ProjectL.Tests.Framework.FrameworkRuntimeCompositionTests` in the task worktree. The two named audit cases reproduce the blockers on this baseline.

## Change boundary and next gate

This audit changes tests and English handoff/plan documentation only. It does not change production Core code, Buffers, generic Framework, LifeCycle, Jobs, View, or DB assets. Unity-generated audio scan metadata and settings changes were restored after inspection. The base checkout and remote were not modified.

Next: settle the termination ownership contract described below before requesting a Buffer change. Any actual playable connection that needs non-Core changes must be presented separately. Re-run the affected tests and retain Task 8's production gate until the owner-controlled runtime path exists.

## Termination ownership follow-up

The user requested investigation before approving changes. No production code was modified for this follow-up.

### Existing responsibilities

- Framework architecture defines CurrentProgressBuffer as the current map and selected-stage record. Its API has no attempt lifecycle contract. StageUI reads HasSelection and CurrentStage.StageName through its ReadSink. Adding termination would extend a shared contract, not merely an internal Core implementation detail.
- RunProgressionService.TryCompleteStage validates Run/entry identity and reads the active Stage's actual terminal snapshot and main-character health. It prepares and commits RunProgressState, retains a completion receipt, and detaches its active Stage binding. Repeated completion uses the receipt.
- RunProgressState.PrepareCompletion records RunEnded and Defeat when the Stage reports defeat. Entry, wallet, and deck operations consult the legacy Run state to reject further mutation. Final-node victory also ends that legacy Run; that policy must not be copied blindly into this increment, whose boss victory completes one chapter.
- RunProgressionService is coupled to StageMapSession, its mutable position, RunId, entry IDs, revision, and snapshot contracts. PrepareCompletion explicitly checks its own map cursor. Calling it from the new coordinator would require maintaining the legacy progression alongside Buffers. It is not a standalone reusable EndAttempt operation.
- RunProgressionService.Dispose only detaches bindings and marks the service disposed. It does not dispose StageServices. StageServices.Dispose releases Souls, ArtifactLifecycle, then the StageSession. Artifact release failure preserves pending ownership and must not be treated as successful cleanup.
- StageLifeCycle and StageJob are placeholders. No implemented attempt-end route was found there. No RunOutcome/RunEnded consumer was found in the current ViewSystem or UI screen source. Scene exit is not evidence that progression or stage resources have ended.

### Recommended connection contract

Reuse existing Stage outcome evaluation, Stage query, and resource-owner disposal APIs. Do not instantiate the legacy Run service just to borrow termination, and do not create a second coordinator-owned map or progression flag.

The CoreSystem execution owner should validate the active Stage's actual outcome, apply the approved progression effects, and retain responsibility for Stage resource cleanup. A caller-supplied Defeat/Victory enum alone is insufficient evidence once the real Stage is connected. The terminal record must continue blocking commands even if cleanup fails; cleanup failure must retain the owning Stage.

For View, publish a durable terminal fact through an owner-approved Buffer contract, then let LifeCycle/UI perform screen transitions separately. An event or a successful scene change cannot replace retained authoritative state. The current source has no published terminal record to reuse. Extending CurrentProgressBuffer remains a possible minimal solution because it already identifies the active chapter/stage, but the Framework documentation does not mandate that location. Its name, scope (attempt versus chapter), reset lifetime, and read semantics must be agreed before implementation. The earlier assertion that EndAttempt must be added to that particular Buffer is withdrawn.

This investigation therefore rules out a simple existing-method hookup. The required integration consists of Stage outcome/cleanup reuse plus an explicit terminal-state contract. It does not justify migrating wallet/deck/shop or restoring legacy Run IDs in this increment.

### Focused legacy verification

Re-executed the previously compiled, unchanged Core assembly with MainCharacterRunProgressionTests, ArtifactReleaseFailureTests, and ArtifactStageLifetimeTests: **39 cases, 38 passed, 1 failed**. The failure is InvalidActivation_LeavesRunAndMapUnchanged_AndValidPreparationCanRetry("disposed", InvalidStage): actual reason ArtifactLoadoutUnavailable instead of InvalidStage. It concerns disposed-stage activation error precedence, not the new Buffer defeat regression. No fix or historical baseline classification is claimed for it. The remaining cases, including legacy termination and Artifact lifetime checks, passed. This managed run is not a Unity/runtime integration test.

Evidence: `C:/CodexHome/visualizations/2026/09/27/01a0e23d-3048-7a21-9127-a1fea325480a/task8-legacy-termination.xml`.
