# Stage Entry and Battle Preparation: Core Handoff

Date: 2026-10-03.

Scope: P1-P3 of the [implementation plan](../plans/2026-10-03-stage-entry-battle-preparation-implementation-plan.md), following the [approved design](../specs/2026-10-03-stage-entry-battle-preparation-contract-design.md). P1/P2 implementation evidence is recorded in the [P1 report](2026-10-03-stage-entry-battle-preparation-p1-verification.md) and [P2 report](2026-10-03-stage-entry-battle-preparation-p2-verification.md).

Status: P1-P3 and gate C are complete: the fixture-backed Core/Buffer entry and publication contract is implemented and verified. View/Flow caller migration (V), authored DB readiness (D) and production activation (A) remain independently pending. Verification was performed on the uncommitted package in `feature/Cheon/refactor/core-system`, based on HEAD `a9db1e5a24f7b6431b36fc2da39807943abaa83c`. Subsequent commit/push delivery is recorded in Git history and is not production activation.

## Consumer contract

The existing `IInGameCoreSystemGroupService.Combat` port exposes:

```csharp
ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId);
void PrepareBattle();
```

Use `EnterBattle` for Combat entry into an already-entered chapter. The caller supplies chapter/node intent; Core resolves the stage key from live progression. Existing roster/main/soul facts, chapter/graph registration and Framework initialization/binding are prerequisites. Entry does not bootstrap the roster, wallet or Artifact inventory.

`Accepted` means matching Current selection and prepared Character/Combat state were published before return. It does not mean the entry Job completed, a scene loaded or Combat lifecycle/input became active. A normal navigation rejection returns the existing rejected `ChapterProgressionResult` without source lookup or entry writes. Invalid arguments, missing dependencies, retained battle, noncombat/malformed data, source/calculation faults and publication invariant failures throw. A retained terminal battle also blocks entry until existing cleanup completes.

Core captures origin selection, performs live progression preflight and checks target DB root identity/chapter/Combat type. It reads the explicit definition and runs existing pure preparation calculation. It repeats progression preflight before publication; a rejection returns directly, while changed origin or changed accepted target returns `StageMismatch`. It then publishes through the existing Buffer group and caches definitions only after success. Source-induced external mutations themselves are not rolled back.

Do not also confirm or select the Combat target after an accepted call. Core has already published it. Parameterless `PrepareBattle()` remains a compatibility operation for an already-committed selection, preserving old caller compilation; it requires map/selection before source lookup. It is not a safe first/next-stage entry route. Retire it only after coordinated caller migration.

## Internal contracts and ownership

| Boundary | Exact contract |
| --- | --- |
| Internal `IBattleInitializationSource` | `BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)`; one signature, no implicit Current-target overload. |
| Public immutable `BattleEntrySelection` | Constructor `(int expectedChapterKey, bool expectedHasSelection, StageSelectionData expectedStage, StageNodeId targetNodeId, StageKey targetStageKey, string targetStageName)`; get-only `ExpectedChapterKey`, `ExpectedHasSelection`, `ExpectedStage`, `TargetNodeId`, `TargetStageKey`, `TargetStageName`. |
| Internal `ICurrentProgressBufferStore` | `PreparedCurrentSelection PrepareSelection(BattleEntrySelection selection)` and `void InstallSelection(PreparedCurrentSelection prepared)`. |
| Internal `PreparedCurrentSelection` | Constructor `(StageSelectionData stage, CurrentShopData shop, CurrentRecoveryData recovery)`; get-only `Stage`, `Shop`, `Recovery`. |
| Public `IBattleBufferWriteSink` | Existing `void PrepareBattle(BattlePreparation preparation)` plus `void PrepareBattle(BattlePreparation preparation, BattleEntrySelection selection)`. |

`BattleEntrySelection` lives in `ProjectL.Systems.RuntimeSystem.BufferSystems.Current`. It is detached call-local publication data, not a navigation authorization or pending request. Target chapter is the expected current chapter; this operation does not change chapters.

Current validates complete expected selection values, including name, and prepares target selection plus shop/recovery retention or clearing according to existing identity semantics. The Buffer group checks canonical node/stage binding, prepares Current/Character/Combat before any installation, then installs Character, Current and Combat. Both overloads share Character/Combat preparation. The group is the existing publication coordinator; individual stores retain their authoritative state.

Core owns navigation/type policy and calculation. Buffer owns expected-state/storage consistency and publication; it performs no DB lookup, navigation policy or Job scheduling. No new manager, state owner, singleton, service key, queue, revision counter, generic transaction/callback API or rollback/reset privilege was introduced. The service retains only its existing static-definition cache. Successful next preparation clears completion/preview response while retaining results and numbering; failed preparation preserves retained facts.

The guarantee is synchronous prepare-before-install under existing serialized execution. Concurrent/reentrant writers, process crashes and out-of-memory recovery are excluded. This is not a crash-safe transaction claim.

Factories continue to construct concrete implementations and export existing service contracts. The default source retains its Framework module and Character read binding; it no longer binds Current selection. Actual root `InitData/PostBindService`, registered Core/Buffer visibility and existing hierarchy are exercised by registered fixtures and `FrameworkRuntimeCompositionTests`. Production factories, service keys, assemblies and Job/Plan/Flow/LifeCycle/Runner code are unchanged.

## Changed file inventory

Paths below are repository-relative. `Core` expands to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem`, `Buffers` to `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems`, `Tests` to `Assets/Tests/EditMode/Framework`, and `CombatExecution` to `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat`.

| File | Responsibility |
| --- | --- |
| `Buffers/Current/BattleEntrySelection.cs` + `.meta` | New immutable expected/target proposal. |
| `Buffers/Current/CurrentProgressBuffer.cs` | Internal storage preparation/install; public selection semantics retained. |
| `Buffers/Core/BufferSystemGroup.cs` | Narrow joint publication overload and shared preparation. |
| `Core/CombatSystem/CombatProcessingService.cs` | Explicit entry, guards/revalidation, compatibility and cache lifetime. |
| `Core/CombatSystem/DatabaseBattleInitializationSource.cs` | Explicit identity and existing default-source gate. |
| `Tests/BattleEntryPublicationTests.cs` + `.meta` | Real-store three-boundary failure/success/retention evidence; P3 nonzero preview and View input preservation. |
| `Tests/BattleDefinitionAdapterTests.cs` | Explicit/default-source/compatibility/reload witnesses and C2 helper migration. |
| `Tests/CoreStageCombatCycleTests.cs` | Registered C4/new-entry tests, distinct next-stage profiles and P3 populated-preview retained-battle rejection. |
| `Tests/CharacterBattlePreparationTests.cs` | Explicit source signature and intentional compatibility selection setup; real old Prepare Job retained. |
| `Tests/ArtifactEditingServiceTests.cs`, `CombatCommandPublicationTests.cs`, `CombatCommonPublicationTests.cs`, `CombatPreviewPublicationTests.cs` | Mechanical source signature migration. |
| `Tests/CombatPlanTests.cs` | Unsupported `EnterBattle` member on existing probe; old Job expectations retained. |
| Plan/spec, P1/P2 verification reports and this handoff | Approved direction, task tracking and independent gate evidence. |

P3 modifies tests and documents only. Final verification and independent review found no P1/P2 production defect requiring a code change.

## Verification evidence

Evidence directory: `.superpowers/sdd/2026-10-03-stage-entry-battle-preparation-implementation-plan/`. Historical P1/P2 RED/GREEN details remain in their reports and the execution ledger. P3 uses Unity 6000.0.73f1, one hidden batch-mode Editor process at a time, via the existing licensed user-profile launcher.

| Run | Result | Exit | Meaning |
| --- | --- | --- | --- |
| `p1-baseline` | 1307/1312; five known failures | 2 | Original fresh package baseline. |
| `p1-regression` | 1336/1341 | 2 | P1 adds 29 cases; same five failures. |
| `p2-regression` | 1370/1375 | 2 | P2 adds 34 cases; same five failures. |
| `p3-focused` | 222/222; no skipped/inconclusive cases | 0 | One added success case and strengthened preview lifetime assertions; no production change. |
| `p3-regression` | 1371/1376; no skipped/inconclusive cases | 2 | One added P3 case, no missing cases; the same five baseline failures. |

Full P3 regression: Core 918/918, Framework 447/452, CombatViewBoundary 6/6. `P3/regression-summary.json` compares against the fresh P1 baseline and P1/P2 regressions: 64/35/1 added cases respectively, no missing cases and identical failure names/messages/stacks. `P3/regression-failures.json` retains the exact five failures, all reporting `Combat stage requires Economy` during DB catalog construction. The suite is not all-green; no failure is suppressed or reclassified as passing.

Exact commands are saved in `run-tests.ps1` and per-run `.args.txt` files, with XML/log/exit-code evidence. Reproduce P3 regression from the repository root:

```powershell
& '.superpowers/sdd/2026-10-03-stage-entry-battle-preparation-implementation-plan/run-tests.ps1' -RunName 'p3-regression'
```

This selects `ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary`. The focused run selects all eleven P2 fixtures from the plan; its exact filter is in `p3-focused.args.txt`. Do not run another Editor against an owned checkout or treat zero discovery/compilation errors as passing tests.

Fixtures use real factories, Framework runtime, progression, calculators and Buffer stores. Local DB/source doubles supply graph definitions and supported profiles. Successful injected-source entry is contract evidence, not authored DB/scene acceptance. The default-source fixture proves explicit unconfirmed-target lookup reaches the retained Method_Key rejection without publication. No guessed targeting profile or production fallback is supplied.

P3 closes the previously deferred preview evidence: six distinct Current/Character/Combat failure-and-retry cases start with a populated response and nonzero answered number. Failure preserves them; corrected preparation clears only the response, retaining answered number and View request. A separate success test verifies the same lifetime. A real published preview survives retained-battle rejection at the Buffer boundary; registered active and terminal Core battles also produce a real service response before rejected entry.

For the no-active-battle storage cases, history is seeded through real stores, then `EndBattle` correctly clears the response. The fixture deliberately reinstalls only a synthetic retained response through the internal Combat store to exercise defensive preparation preservation/clearing. This injection is not claimed as reachable production state after normal cleanup. The registered Core cases exercise public preparation/preview/retained-battle behavior separately. New assertions also preserve View request/commands/presentation; Core receives no input cleanup authority.

The known baseline failures remain reported individually:

- `ProjectL.Tests.Framework.ArtifactDataTableTests.Catalog_RegistersOnlyMigratedRulesAndExplicitlyPendingViews`
- `ProjectL.Tests.Framework.DataBaseAssetTests.Catalog_CreatesAllDataBaseGroups`
- `ProjectL.Tests.Framework.DataBaseAssetTests.CombatStages_ReferenceExistingInGameData`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryStage_HasNodeVisual`
- `ProjectL.Tests.Framework.DataBaseAssetTests.EveryUnitAndStructure_HasUnitVisual`

## Review and incoming-change preservation

A fresh read-only reviewer approved the actual P1-P3 working-tree package with no Critical, Important or Minor actionable finding. The reviewer inspected production collaborators, untracked source/tests/metas, fixtures, approved documents, ledger decisions, XML and exit codes. This closes the earlier deferred nonzero-preview evidence item. `P3/review-package.md`, `tracked-package.diff` and the execution ledger retain the reviewed scope; an empty HEAD-to-HEAD range was not used to represent uncommitted changes.

`git diff --check` and untracked source whitespace checks pass. Both new GUIDs have exactly one `.meta` match; new code comments are Korean. All package documentation links resolve, and the external V1-V5 checklist below matches the approved plan exactly.

All 1,124 captured production/meta/incoming asset paths match their incoming SHA256, confirming no P3 production change. Unity changed only the FMOD catalog scan timestamp; the exact saved incoming bytes were restored after scan-only and concurrent-change guards. The user's existing FMOD modification and unrelated attachment directory are preserved. `P3/structural-audit.json` and `P3/preservation-check.json` retain checks. HEAD and branch were unchanged during P3 verification. Evidence is retained for review.

Verification decisions: P3 adds characterization tests for existing behavior rather than claiming a new production RED/GREEN implementation. Synthetic response restoration verifies a defensive storage boundary, while real registered previews cover public retained-battle rejection. Benchmarks and concurrent/crash recovery are not certified. Recovery/shop entry, chapter routing, Run reset, rewards, terrain policy and asset authoring remain outside the authorized package. Misusing these exclusions as production acceptance would exceed the evidence; V/D/A and their owner checks must close separately.

## Independent completion gates

| Gate | Status | Scope of claim |
| --- | --- | --- |
| C: Core/Buffer implementation | Complete | P1-P3 implemented, tested and independently reviewed with fixture-backed definitions; caller migration is separate. |
| V: View/Flow caller migration | Pending | Real target transport, Job outcomes and one-time lifecycle activation must be verified by execution owner. |
| D: Authored DB readiness | Pending | Method_Key/query/unit linkage, Economy and required data must support default-source entry. |
| A: Production activation | Pending | Requires C + V + D and real scene/bootstrap/F1/F2 lifetime evidence. |

Core-only P1-P3 does not implement recovery/shop entry, chapter routing, Run reset, rewards, terrain policy, assets or production execution migration. The external-owner checklist below remains pending, even when C closes.

## Gate V: View and Flow migration handoff, not Core implementation

The user does not own the combat Jobs. The following checklist belongs to their execution owner. Relevant current files: `CombatExecution/Jobs/CombatPrepareJob.cs`, `Plans/CombatPlan.cs`, `CombatFlow.cs`, `CombatLifeCycle.cs`; registration is `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/JobSystem/JobRegister/JobRegisterSystem.cs`. Its current `Register(JobRootType, JobType)` has no chapter/node payload, and `CombatPlan.Register(service, jobType)` cannot supply one today. Do not describe migration as only replacing a single call.

- [ ] V1: The execution owner defines and implements target transport through its existing registration/Plan/Job path. Capture chapter/node intent until execution; do not store an intended target as committed Current selection, read a later unrelated click, add a Core queue, or bypass the Framework registration route with a competing runner. Any necessary shared Framework contract extension belongs to that owner review.
- [ ] V2: The selected entry Job calls `CoreGroup.Combat.EnterBattle(chapterKey, nodeId)` once and handles accepted, rejected and thrown outcomes distinctly. Normal return, including rejection, completes that Job once; only acceptance permits active Combat. The owner defines how the returned outcome reaches Flow; `HasBattle` alone must not turn a rejection, fault or an unrelated retained battle into success. Do not reuse a retained combat-command result/sequence as an entry acknowledgement.
- [ ] V3: Migrate lifecycle startup coherently. After accepted entry, activate the prepared battle without `OnEnter -> BeginPreparation -> old PrepareBattle()` running again. If scene resources load earlier, gate input and active processing until acceptance. Remove the redundant Combat-side confirmation call; Core has already published selection. Keep noncombat callers distinct.
- [ ] V4: Define cancellation/replacement and failure handling before activation. A stale request must not become an automatic retry; an accepted entry followed by scene activation failure must not be silently replayed or repaired with unrestricted resets. Stop/drain old work and clear old input through existing external-owner permissions. Framework F1 and View/Flow F2 remain necessary production gates.
- [ ] V5: Prove the actual `Register -> Plan -> Runner -> Job -> Core -> Buffer -> Flow` path with distinct first/next targets, normal rejection, fault, duplicate registration prevention and lifecycle startup exactly once. Then prove authored-data/scene acceptance under D/A. Fake Job tests or P2 direct service calls are insufficient for V.

Core may supply service/Buffer evidence and answer contract questions. It must not mark V complete by implementing look-alike Jobs inside Core. Existing `CombatCommandJob`, `CombatPreviewJob` and `CombatAdvanceJob` service wiring remains the execution owner's broader handoff, outside P1-P3.
