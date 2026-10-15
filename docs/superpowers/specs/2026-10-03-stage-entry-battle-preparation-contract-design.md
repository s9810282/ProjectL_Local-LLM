# Stage Entry and Battle Preparation Contract Design

Date: 2026-10-03.

Status: Design direction approved by the user on 2026-10-03 for implementation planning. The user explicitly requires separate completion conditions for Core/Buffer implementation and View/Flow caller migration. No API, production code, test or asset change is implemented by this document. See the [implementation plan](../plans/2026-10-03-stage-entry-battle-preparation-implementation-plan.md).

Implementation tracking: P1-P3 and gate C were completed on 2026-10-03. See the [final Core handoff](../handoffs/2026-10-03-stage-entry-battle-preparation-handoff.md), [P1 evidence report](../handoffs/2026-10-03-stage-entry-battle-preparation-p1-verification.md) and [P2 evidence report](../handoffs/2026-10-03-stage-entry-battle-preparation-p2-verification.md). Focused tests passed 222/222; full regression passed 1371/1376 with the same five baseline Economy failures, and independent whole-package review found no actionable defect. Core/Buffer fixture-backed implementation is complete; caller migration (V), authored DB readiness (D) and production activation (A) remain independently pending. Design-time descriptions below record the original problem and approved direction; implementation evidence is tracked separately.

## Purpose and scope

Make a requested Combat stage prepare from its own definition before it becomes the current stage. A successful Core call must leave both a prepared battle and the matching current selection. A rejected or failed call must leave the previous selection and battle-related stores unchanged.

This follows the user's Core-only scope, single-responsibility preference and existing Framework/Buffer ownership. Core validates gameplay intent, resolves definitions and calculates proposals. Buffer owns authoritative facts and validated publication. View/Flow owns input, scheduling, Jobs, lifecycle activation and presentation.

This design covers entry into regular, elite and boss Combat stages in an already-entered chapter. It does not implement recovery/shop entry, chapter routing, Run reset, rewards, actual DB authoring or production scene transitions. It does not create another state owner, execution manager or generic transaction framework.

## Design-time baseline evidence

Paths in this document are repository-relative.

| Source | Current behavior | Consequence |
| --- | --- | --- |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ChapterProgressionCoordinator.cs` | `PrepareStageEntry(chapterKey, nodeId)` validates without selecting; `ConfirmStageEntry` revalidates and selects after preparation | The validated target already includes its `StageKey`, but that target is not passed to combat preparation. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/CombatProcessingService.cs` | `PrepareBattle()` invokes a source with only the DB context, then installs character/combat state | Preparation has no explicit requested target. Selection is outside its publication boundary. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/DatabaseBattleInitializationSource.cs` | Requires `CurrentProgressBuffer.HasSelection` and reads its current stage | First entry has no selection; subsequent entry can still point at the previous stage. Supplying missing DB profiles alone would not solve this mismatch. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/BattleDefinitionAdapter.cs` | Already accepts `mapKey`, `stageKey` and owned roster; validates chapter/type consistency | Reuse this explicit-target conversion rather than inventing a new definition registry. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs` | `PrepareBattle` prepares character and combat stores before installing either | Extend this specific publication boundary to include selection; do not recreate preparation elsewhere. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Current/CurrentProgressBuffer.cs` | `SelectStage` validates and writes directly, clearing old shop/recovery state when the target changes | Calling it after battle installation introduces another fallible operation. Calling it before preparation commits location too early. |
| `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/CombatLifeCycle.cs`, `CombatFlow.cs`, `Jobs/CombatPrepareJob.cs` | On entry, Flow schedules a parameterless preparation Job; the Job calls `PrepareBattle()` | A caller migration is required. This document is not permission to implement the View-owned execution path. |

The [C1-C5 evidence report](../handoffs/2026-10-03-core-stage-combat-run-integration-verification.md) verifies fixture-backed preparation and progression. C2/C4 inject definitions, so they do not prove that the default source resolves an unconfirmed target. Their test-driven `PrepareStageEntry -> PrepareBattle -> ConfirmStageEntry` order is historical evidence, not the proposed production contract.

The supplied integrated design deck describes connected forward stage movement on slide 15 and Combat entry after selection on slides 29/46. It does not decide this API or publication boundary. Existing repository ownership decisions remain binding.

## Alternatives and recommendation

| Approach | Benefit | Limitation | Decision |
| --- | --- | --- | --- |
| Confirm selection, then call the existing parameterless preparation | Small caller change; current source sees the target | Missing data can leave a selected stage without a battle. Rollback would need to restore selection and cleared shop/recovery data. | Reject. |
| Pass an explicit target to preparation, then let the caller confirm separately | Fixes wrong-definition lookup with little Buffer change | Confirmation can reject or throw after a battle is installed. `EndBattle` correctly rejects unfinished battles, so it is not a rollback mechanism. | Not the production recommendation. |
| Pass an explicit target to one Core entry operation and publish battle plus selection together | Resolves target identity and the partial-entry failure window using the existing publication pattern | Requires a narrow Current store preparation/install extension and a coordinated caller migration. | Recommended. |

The third option deliberately strengthens the old caller-only guarantee. The recommendation is a proposed contract change, not a claim that C5 already proved three-store entry publication.

## Proposed Core contract

The approved operation on the existing `ICombatProcessingService`, now implemented in P2:

```csharp
ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId);
```

- The caller supplies intent: chapter and node identity. It does not supply trusted `StageKey`, definitions or a pre-approved progression result.
- Core calls the existing progression validation against live Buffers and resolves `StageKey` from the approved graph node. A prior UI precheck is advisory and is not reused as authorization.
- A normal navigation rejection returns the existing rejected `ChapterProgressionResult`, with no writes. Acceptance means both battle preparation and matching selection have been published before return; it is not merely a successful precheck.
- Invalid arguments, missing bindings, a retained battle, a non-Combat definition, missing/invalid DB data and publication invariant failures throw before publication. They are not disguised as accepted results or automatically retried.
- `HasBattle` remains the retained-battle guard, including deployment, active combat and terminal presentation. A second call after success does not acquire another roster, replace the battle or repeat preparation; it fails under that guard.
- No automatic roster, wallet or Artifact initialization occurs. Their existing bootstrap calls remain external prerequisites.
- `PrepareStageEntry` remains a read-only preflight API. Combat callers using `EnterBattle` must not also call `ConfirmStageEntry` or write selection themselves. The existing confirmation API remains for other consumers; this change does not redesign all stage types.

The operation belongs in `CombatProcessingService` because it already assembles preparation facts and publishes battle changes, while the progression coordinator already provides navigation decisions. No reciprocal coordinator-to-combat dependency or new entry manager is needed. The Factory continues to compose these services.

### Explicit definition lookup

Change the internal initialization-source contract to receive the resolved identity:

```csharp
BattleInitialization Read(
    IDataBaseQueryContext database, int chapterKey, StageKey stageKey);
```

The source may read the current owned roster from its existing Character read port. It must not obtain the requested definition from `CurrentProgressBuffer.CurrentStage`, mutate selection, initialize the roster or install a battle. The DB adapter continues to validate that the definition is a Combat stage belonging to the requested chapter.

This change removes the implicit target dependency; it does not create the missing Method_Key schema. The default source must continue to reject unsupported/missing profiles before publication. Source spies and supported injected definitions prove explicit identity delivery; actual DB success stays a separate acceptance gate.

### Compatibility during caller migration

Keep the public parameterless `PrepareBattle()` temporarily so the existing View-owned Job still compiles. Its limited meaning is preparation for an already-committed current selection; it is not a safe new-stage entry operation and must never guess a first/next stage. Internally it uses the explicit source signature with that committed identity. Missing selection must fail before publication.

Tests that prepared without any current selection must either set up a committed selection when testing this compatibility route or migrate to `EnterBattle` when testing new entry. Do not keep a second implicit source interface or silently substitute an arbitrary stage for tests. Command calculation, preview, advance and low-level Buffer setup tests may retain their documented direct fixture setup.

Any definition reload in `ReadInput` must also supply an explicit committed identity. Install the service's definition cache only after successful publication; a failed entry must not leave the failed target cached for a later attempt. The service still stores only static definitions, never a second runtime selection or battle state.

Retire the parameterless entry route only in a subsequent coordinated caller migration. Until that migration and DB support are complete, do not advertise production battle entry as activated.

## Publication and failure guarantees

Use the existing `IBattleBufferWriteSink` and `BufferSystemGroup` preparation path, with a combat-entry-specific operation or overload accepting the calculated preparation and an immutable selection proposal. The implementation plan must specify the exact members. Do not add an optional callback, generic transaction interface, persistent pending-entry object, global revision or new Buffer instance.

The selection proposal carries the expected current chapter/selection and the final chapter/node/stage identity. It is a detached, call-local publication value in the Systems Buffer boundary; it is not an authorization token or a retained gameplay owner. Core obtains the final identity from live progression validation, not from a caller-created proposal.

1. Core requires initialized services and no retained battle, captures current selection facts, and validates navigation using the existing coordinator.
2. Core reads the explicit target definition, verifies Combat type/chapter, and calculates preparation from current owned/main/soul facts using the existing calculators.
3. Before publication, Core rechecks the relevant live progression decision and rejects a changed target or origin. This supports deterministic failure tests; it does not claim concurrent gameplay writers are supported.
4. The Buffer group validates the proposal's expected current selection and target identity against canonical records. It prepares the Current selection, Character battle and Combat state before the first installation. Current selection preparation also stages the existing shop/recovery clearing behavior.
5. Only after all preparation succeeds, install all prepared values without DB calls, gameplay calculation, callbacks or new validation that can fail for ordinary input. Set selection consistently and make the retained battle visible as part of this synchronous publication; cache definitions after it succeeds.

`ICurrentProgressBufferStore` owns preparation/installation of its own selection data, following its existing internal purchase/recovery store pattern. The Buffer group coordinates publication across stores. Neither layer decides navigation legality, battle rules, stage type policy or which chapter comes next. Those remain Core responsibilities.

The guarantee is synchronous prepare-before-install under the existing serialized execution model. It does not cover process crashes, out-of-memory failures, reentrant callbacks or arbitrary multi-threaded writes. It requires no rollback/reset privilege and must not call `EndBattle`, `AbandonBattle` or `ResetRunProgress` to repair failed preparation.

| Boundary | Selection | Battle and persistent values |
| --- | --- | --- |
| Read-only preflight | Unchanged | Unchanged |
| Navigation rejection | Previous selection retained | No battle installation or acquisition |
| Missing definition/profile, invalid placement or any store preparation failure | Previous selection and shop/recovery state retained | Character/combat/soul state, allocator, results/counters, currency and Artifact state retained |
| Successful entry | Target chapter/node/stage is current | One prepared battle; persistent roster/main/souls/wallet/Artifact preserved according to existing preparation semantics |
| Duplicate entry while a battle is retained | Existing selection retained | Existing battle retained; no second draw or grant |

Keep the C4 completion/result lifetime contract: successful next preparation clears prior completion/preview and preserves latest result and numbering. Failed entry preserves them. Preparation is not a combat command result and must not fabricate a new command sequence to signal success.

## View and Flow handoff

The intended logical sequence is:

```text
Stage selection intent (chapter, node)
  -> View/Flow retains the explicit target and schedules its entry work
  -> one Core EnterBattle(chapter, node) call
       -> live navigation validation
       -> explicit-target DB conversion and calculation
       -> prepared selection + character + combat publication
  -> accepted: execution owner activates the prepared Combat lifetime
  -> rejected/faulted: execution owner stays out of active Combat and handles the outcome
```

Scheduling and scene-loading order are not implemented by Core. Once preparation has succeeded, activating Combat must not schedule a second parameterless preparation. The current `CombatLifeCycle.OnEnter -> BeginPreparation -> CombatPrepareJob` path therefore requires a View/Flow migration, not just replacing one service call and leaving all startup behavior unchanged.

View/Flow must define how its existing Job/Plan path carries the target until execution and how it observes rejection versus an exception. It must not overwrite current selection to transport intent. No new Core input queue or request Buffer is prescribed here. If transport needs an execution-owned contract change, that owner must implement it explicitly before activation.

The external owner also stops/drains old work and clears old View inputs at the appropriate lifetime boundary. Existing F1 fault termination and F2 result acknowledgement/cleanup obligations remain. A rejected request is not an instruction to retry until it succeeds; faults must not re-execute on every tick.

## Implementation boundaries

| Owner | Expected changes after design approval |
| --- | --- |
| Core | Add the entry orchestration operation; pass explicit target to the initialization source/adapter; preserve pure progression and preparation calculations; keep definition-cache lifetime correct. |
| Buffer publication | Extend the existing battle publication boundary and internal Current store preparation/install contract for selection. No new business logic or state owner. Review this narrow change against the existing Buffer pattern. |
| Core tests | Exercise real factory registration, explicit-target source calls, live progression, all three stores and failure preservation. Update affected source doubles and old preparation fixtures. |
| View/Flow | Carry intent, migrate the existing entry/preparation Job and startup sequence, handle rejection/fault and activate an already-prepared battle once. No duplicate Core Job implementation. |
| DB | Supply Method_Key/query/unit linkage and real required definitions/Economy references. Core adapts and verifies actual data when available. |

Framework composition, generic Runner behavior, production asset authoring, new-Run policy and unrelated services remain outside the Core implementation package. Any compile adjustment to View-owned tests or callers must be listed separately in the implementation plan rather than hidden as Core work.

## Required acceptance evidence

The implementation plan should extend existing Framework fixtures where clear and add only missing cases:

1. First entry with a selected chapter but no current stage reads the validated target's exact chapter/stage key and succeeds with matching selection and battle.
2. After a completed stage is cleaned up, entry into a different legal stage uses distinguishable target definitions. Assert that old-stage geometry/economy/profile inputs are not reused. C4's shared fixture geometry alone is insufficient for this assertion.
3. Wrong chapter, backward/unconnected node and already-cleared target reject without source lookup or writes; a non-Combat definition cannot enter through the Combat API.
4. Missing target data or the default source's existing Method_Key gate leaves selection, old shop/recovery data, persistent state, character allocation, combat completion/results/counters and caches unaffected; correcting inputs permits a clean retry.
5. Individually invalid Current, Character and Combat preparation proposals fail before any store installs. Use actual Buffer stores, not only throwing fakes, to prove the publication boundary.
6. A changed origin/target between preflight and publication fails without partial writes. Assert structural mismatch rejection separately from Core navigation rules.
7. Repeated accepted entry while `HasBattle` is true cannot prepare again. Existing settlement, cleanup, Artifact retention and next-stage/chapter regressions continue to pass.
8. Default-source tests prove explicit target lookup and fail-before-write for missing profiles. Supported injected sources are labeled as fixtures; they do not close actual DB readiness.
9. View/Flow separately proves target transport, one preparation only, rejected/faulted entry handling and no active Combat before successful publication through the real Job path.

Run focused preparation/progression/Buffer tests during implementation, then the three existing EditMode assemblies. Compare the five known Economy failures by exact name/message; do not suppress them. A document-only change needs link/source consistency checks, not a new Unity run.

## Decisions for review

The approved direction is explicit target identity, one synchronous Core entry call, joint selection/battle publication through existing Buffers, and a staged migration of the View-owned parameterless preparation path. Approval permits writing the implementation plan; it does not mean any of these APIs are implemented or that production entry is activated.

The implementation plan defines publication value/store signatures, affected fixtures, compile-compatible migration steps and separate View/Flow handoff tasks. Core/Buffer completion must never imply caller migration completion. Keep the DB production gate open throughout this work. No commit or push is included in this documentation request.
