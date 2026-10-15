# Artifact Core Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task in the current session. Steps use checkbox (`- [ ]`) syntax for tracking. This document does not authorize implementation, other-owner changes, commits or pushes by itself.

**Goal:** Replace the standalone Artifact editing state owner with pure Core calculations and one registered Systems processing service that uses ArtifactBuffer as the runtime source of truth.

**Architecture:** ArtifactEditingService assembles Buffer/DB facts, checks battle admission once at the execution boundary, invokes a narrow Core.Contracts calculation port and publishes through narrow Buffer permissions. ArtifactEditCalculator implements that port in Core.Application and calls ArtifactPlacementRules in Core.Domain. InGameCoreSystemFactory alone knows the concrete calculator for construction and injects it as IArtifactEditCalculation; the editing service uses only the port. CombatProcessingService retains its existing placed-effect read path and gains no editing responsibility.

**Tech Stack:** Unity 6000.0.73f1, C#, existing ProjectL Framework composition, Core.Contracts/Domain/Application assemblies, NUnit EditMode tests.

**Spec:** [Artifact migration design](../specs/2026-10-02-artifact-buffer-framework-migration-design.md). Preserve its confirmed behavior and script inventory. The user decisions recorded below supersede the earlier Core-only scope and blanket prohibition on calculator construction in a Systems factory. The design addendum and A2 handoff reference this revision.

**Status:** A1, A2, E1, A3 and A4 are implemented and verified; see the [migration closure handoff](../handoffs/2026-10-02-artifact-core-migration-handoff.md), dated 2026-10-03. A3 delivers E2 exposure and E4 construction through the existing Framework route. A4 retires the legacy owners and replacement/reset bypass after caller/GUID audit. E3 uses the existing Job and LifeCycle systems, with production dimensions/bootstrap/input/UI hookup still pending. No production activation is claimed.

## Confirmed decisions — 2026-10-02

- **E1:** Add the missing Artifact Buffer publication/conversion work to this plan, with concrete files, contracts and tests. The existing Buffer already stores board/pool/placement snapshots; these capabilities extend that same Buffer.
- **E2:** Add only `IInGameCoreSystemGroupService.Artifacts` and its typed key. This exposes the editing service to the existing execution path; View continues to submit intent and read approved Buffer outputs.
- **E3:** Existing Jobs control command timing/completion and existing LifeCycle code owns flow entry/exit. No new scheduler, runner or lifecycle manager is needed. Initial board-size authoring and the exact Artifact input/bootstrap hookup remain feature details to supply before production activation.
- **E4:** Only factory construction code knows `ArtifactEditCalculator`; `ArtifactEditingService` depends on `IArtifactEditCalculation`. Use the existing Core factory and existing Application assembly reference, without a new composition assembly.
- A1 verification passed 31 tests. The final A2 filter passed 107 tests (59 A2, 27 A1, 21 retained service tests). E1 passed 79 Buffer/map/chapter tests, and its combined regression passed 186 tests including those A1/A2/retained service fixtures.

## Global constraints

- Allow editing in the lobby and stage-selection screen; reject mutations while CombatStateBuffer.HasBattle, including preparation and retained terminal presentation until EndBattle.
- Scope includes Core-owned processing/rules/composition and the specifically listed Artifact Buffer files, query/key exposure and tests in E1. Framework infrastructure, View, LifeCycle, Job and DB implementations and authored assets remain outside E1/A3/A4; E3 records their later integration work.
- ArtifactBuffer owns runtime inventory/layout, initialization and retention. No service-owned board, pool, map, pending request, revision, lease or persistent compiled loadout.
- Core.Application and Core.Domain must not reference Systems, Buffer, Framework or Unity. Their namespace prefix containing Systems does not change their actual asmdef boundary.
- ArtifactEditingService uses Framework and Core.Contracts for lifecycle/calculation contracts, plus the required Buffer/DB interfaces for fact assembly and publication. Only `F/InGameCoreSystemFactory.cs` may import and construct `ArtifactEditCalculator`; it must pass the instance through `IArtifactEditCalculation` and never call its calculation methods. No other new Systems consumer may depend on concrete Artifact Application/Domain types. Use the existing Application reference; add no asmdef reference, reflection bridge, implementation cast or friend-access expansion.
- Use the existing Factory/ExportServices/InitData/PostBindService/group propagation. Do not introduce a dispatcher, runner, service locator, parallel route or direct screen-to-calculator route.
- Core never writes, removes or acknowledges View input. Existing execution owners connect intent delivery and presentation; combat Plan/Job ownership stays unchanged.
- No editing endpoints or geometry in CombatProcessingService. No gameplay rules or DB lookup in ArtifactBuffer. No new generic transaction, lock or Run version.
- Preserve state on gameplay rejection and unexpected exceptions before publication. Prepare the full proposal and output before any write; do not report success until publication returns normally.
- Preserve IDs including zero, pool ordering, bounds-before-collision precedence, self-overlap moves, successful identical moves and the existing return-to-pool identity behavior.
- Chapter changes, popup close, battle cleanup and ordinary lobby return do not clear Artifact inventory. No save/load guarantee or new reward/price policy is introduced.
- Preserve incoming changes, including CombatProcessingService cleanup and unrelated audio assets. Do not stage the whole workspace, reset files to HEAD or run broad cleanup.
- Documentation is English; new code comments are Korean. Add `.meta` files for new Unity scripts and preserve GUIDs of retained scripts.

## Why this fits the Framework

The responsibility split follows existing roles, but does not alone prove a legal assembly connection. A Systems service implements IServiceModule-derived exposure and lifecycle binding. One plain `IArtifactEditCalculation` port in Core.Contracts covers the actual editing/preview use case; ArtifactEditCalculator implements it without Framework dependencies. ArtifactPlacementRules stays an internal-to-Core dependency with no extra interface or registration.

Current InGameCoreSystemFactory constructs services and calls ExportServices. InGameCoreSystemGroup forwards InitData and PostBindService to registered child modules. ArtifactEditingService follows the typed `ISystemInitData<IDataBaseQueryContext>` pattern, retaining the Artifact query, the Contracts calculation port and only the current/read and initialization/edit/preview publication permissions. E4 authorizes concrete calculator construction at this existing factory only; it grants no concrete calculation access to runtime services, Jobs or View.

Registration requires both `AddService<IArtifactEditingService>(this)` and `AddModule<ISystemModule>(this)` because the service participates in binding. Do not add another root group or modify FrameworkRuntime. The one Core group property/key addition is agreed by the user under E2. E1 lists the corresponding narrow Buffer query/key additions so their impact on existing consumers and test doubles is explicit.

Required references: `projectl_architecture_principles_v3_1.md`, [Framework guidelines](../framework/Framework%20Development%20Guidelines.md), [programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md). The programmer guide body is version 2.0. **Unverified source:** the V3.1 file was not found by a repository filename search, including ignored/hidden files; its boundary is available here only as a statement in the supplied feedback, not as an independently inspected document.

**Confirmed assembly fact:** ProjectL.Systems.asmdef already references GUID `42f479a350493ca4abd078910e78a9ad`, identified by ProjectL.Core.Application.asmdef.meta as Core.Application. The current framework-architecture.md also lists that reference. E4 now explicitly permits using it for factory construction. The runtime service boundary remains Contracts-only for calculations and is enforced by source review, since the existing shared Systems assembly cannot enforce that per-class restriction. The unavailable V3.1 document remains an unverified source, not an unresolved approval replacing the user's explicit decision.

### Responsibility matrix

| Component | Owns | Excludes |
| --- | --- | --- |
| ArtifactPlacementRules | Shape rotation, spatial bounds/collision, ignored moving instance | Inventory, DB, combat, Buffer, effects |
| ArtifactEditCalculator / IArtifactEditCalculation | Initialization, complete edit proposals, Artifact rules and spatial preview composition | Persistent state, Buffer, Framework, resolver, View or DB implementation dependency |
| ArtifactEditingService | Current facts, readiness/admission coordination, port invocation, completed publication | Persistent Board/Pool/Map, lease/revision, pending request, compiled loadout/catalog; concrete Application/Domain dependencies |
| InGameCoreSystemFactory | Construct the concrete calculator once per Core group, inject its Contracts port, construct/register the service | Calling calculation methods, gameplay decisions, retained runtime state |
| ArtifactBuffer | Canonical runtime data, initialization/retention, permitted state/result/preview storage | Artifact gameplay rules, battle admission, DB lookup |
| Plan/Job/input owner | Intent lifetime, scheduling, one-time execution/completion, required acknowledgement/correlation | Reimplemented placement or battle-admission rules |
| View | Send intent, read approved Buffer state/output, display state/preview | DB lookup, Core input snapshot construction, Buffer writes, authoritative admission checks |
| CombatProcessingService | Existing confirmed placed-effect consumption | Editing, inventory, geometry, preview or popup ownership |

Keep one cohesive editing service; multiple narrow permissions on one Buffer do not imply multiple state owners or per-button services.

## Review focus

1. Empty inventory still permits a spatially legal preview; full placement rejects NotInPool. A1/A2 tests separate geometry from ownership checks.
2. A placement can overlap its own old cells when moving; another item's cells still block it. A1/A2 exercise all four rotations and rejection preservation.
3. Returning an item issues a fresh pool ID while the result targets the removed placed ID; overflow must happen before publication. A2/A3 pin both behaviors.
4. Rebinding/recreating a service and reopening a screen must not grant InitialCount again; Finished does not unlock editing while HasBattle remains true. A3 tests the real Buffer boundary.
5. A successful calculation or service registration is not completed UI integration. A3/A4 distinguish publication, combat consumption, input/Job hookup and actual gameplay verification.

## Paths and deliverables

The aliases below expand to exact repository-relative directories; file names in task lists use these aliases consistently.

| Alias | Directory |
| --- | --- |
| S | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` |
| B | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| F | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core` |
| K | `Assets/ProjectL/Script/Systems/ServiceKeys` |
| T | `Assets/Tests/EditMode` |

| File | Responsibility / change |
| --- | --- |
| Create C/Domain/Artifact/ArtifactPlacementRules.cs | Stateless rotated shape and spatial validation; reuse TileFootprint |
| Create C/Application/Artifact/ArtifactEditCalculator.cs | Stateless initialization and aggregate edit proposals |
| Create C/Contracts/Artifact/IArtifactEditCalculation.cs | One pure edit/preview port implemented by ArtifactEditCalculator; no Framework base interface |
| Modify C/Contracts/Artifact/ArtifactState.cs | One shared detached-copy operation for this existing carrier, not a retained state owner |
| Create C/Contracts/Artifact/ArtifactEditProposal.cs | Accepted proposed state plus existing action result; no retained owner |
| Create C/Contracts/Artifact/ArtifactPlacementPreview.cs | Detached shape/anchor/spatial failure facts for the actual preview consumer |
| Create S/ArtifactSystem/ArtifactEditingService.cs | IServiceModule-derived public interface and internal sealed bound processing service |
| Modify F/InGameCoreSystemFactory.cs, S/InGameCoreSystemGroup.cs, K/SystemGroupsService.cs | A3: factory-only calculator construction, Contracts-port injection and one agreed Core service registration/property/key |
| Modify B/Artifact/ArtifactBuffer.cs, B/Artifact/ArtifactBufferSnapshot.cs | E1: narrow publication permissions, output retention, structural validation and snapshot-to-calculation conversion |
| Modify B/Core/BufferSystemGroup.cs, K/DataBaseGroupServiceKey.cs | E1: typed query properties and keys resolving the same physical ArtifactBuffer |
| Modify T/Framework/ArtifactBufferTests.cs, T/Framework/ChapterResetSpy.cs | E1: real Buffer behavior/composition coverage and forwarding properties for the existing query test double |
| Create T/ArtifactPlacementRulesTests.cs, T/ArtifactEditCalculatorTests.cs | Pure behavior coverage in ProjectL.Tests.Core |
| Create T/Framework/ArtifactEditingServiceTests.cs | A3 integration tests using existing ProjectL.Tests.Framework access and the completed E1 Buffer contracts |
| Conditional retirement: six state-owner scripts and obsolete Contracts | A4 only, after replacement/caller gates pass |
| Create docs/superpowers/handoffs/2026-10-02-artifact-core-migration-handoff.md | A4 delivery record, actual API and unresolved owner dependencies |

Keep the existing Artifact carriers/result meanings. The targeted ArtifactState copy helper addresses proposal ownership without changing its current constructor semantics. Do not create a generic request bus, per-operation registered service, forwarding DB adapter or new editor manager.

## Integration contracts and implementation order

### E1 — Implement Artifact Buffer publication and conversion

**Delivered API:** IArtifactBufferReadSink.Current, the five narrow publication/output permissions below and ArtifactBufferSnapshot.ToCalculationState(). ArtifactBuffer retains one canonical detached snapshot and validates storage structure at snapshot construction. The legacy IArtifactBufferWriteSink.SetSnapshot/Clear surface was temporarily retained by E1 and is now removed by A4, together with its export/query/key. A3 receives narrow permissions only.

Keep one physical ArtifactBuffer. **Delivered E1 permission split:** keep Current on IArtifactBufferReadSink; expose completed output through IArtifactEditResultReadSink and transient output through IArtifactPreviewReadSink. Separate initialization, edit publication and preview publication capabilities as IArtifactInitializationWriteSink, IArtifactEditWriteSink and IArtifactPreviewWriteSink. Each interface derives from IServiceModule; read interfaces expose no writes. The editing service receives only the necessary write capabilities, never Clear/final-reset authority; View receives reads only. Do not create a second Buffer or give a preview-only consumer inventory mutation access.

```csharp
bool TryInitialize(ArtifactState initialState);
void PublishEdit(ArtifactEditProposal proposal);
void PublishPreview(ArtifactPlacementPreview preview);
ArtifactActionResult? LastEditResult { get; }
ArtifactPlacementPreview? LastPreview { get; }
```

These members belong to their respective delivered permissions, not one broad sink. Null means no result/preview; never default ArtifactActionResult or a fabricated empty preview. Nullable reference annotations in delivered contracts use file-local `#nullable enable`, without a project/asmdef setting change. E1 supplies the delivered declarations for A3 consumers.

| Permission | Member | IBufferQuery property | ArtifactBufferKey member |
| --- | --- | --- | --- |
| IArtifactInitializationWriteSink | TryInitialize | ArtifactInitializationWriteSink | Initialize |
| IArtifactEditWriteSink | PublishEdit | ArtifactEditWriteSink | Edit |
| IArtifactPreviewWriteSink | PublishPreview | ArtifactPreviewWriteSink | Preview |
| IArtifactEditResultReadSink | LastEditResult | ArtifactEditResultReadSink | EditResults |
| IArtifactPreviewReadSink | LastPreview | ArtifactPreviewReadSink | Previews |

Keep `ArtifactReadSink` / `ArtifactBufferKey.Read` as the current-state read route. All added key types are `SystemGroupKey<TPermission>`. `ArtifactBuffer.ExportServices` registers the additional interfaces on its existing instance; BufferSystemFactory still constructs exactly one ArtifactBuffer.

TryInitialize returns true only for the first accepted initialization and false if already initialized, without replacing state. PublishEdit prepares structural validation and owned data before assignments, then installs accepted Current and matching LastEditResult as one completed synchronous operation. Rejection publishes its result and leaves Current untouched; both accepted and rejected edits clear LastPreview under the policy below. A validation/publication fault propagates; do not publish success before the operation completes. Buffer does not re-evaluate the command's gameplay rules.

#### State representations and copying

| Value / boundary | Ownership and conversion |
| --- | --- |
| ArtifactBufferSnapshot | Confirmed canonical stored representation and Current read type. Keep existing detached ArtifactData lists; reads may be retained as historical snapshots, never as authority for a later edit |
| ArtifactState | Command-local pure input; also the successful proposal's payload. It is not the Buffer store or a persistent service field |
| Snapshot -> calculation input | Delivered E1 `ArtifactBufferSnapshot.ToCalculationState()` performs the single mapping from ArtifactData to owned ArtifactItemState arrays, including nested cell arrays. This conversion belongs to the Buffer representation owner; the service only calls it, with no mapping/copy loops of its own |
| ArtifactState -> owned proposal | A2 adds `ArtifactState.DetachedCopyOf(ArtifactState source)`, which copies both item arrays and each nested cell array. ArtifactEditProposal uses that one helper; calculator operations do not reimplement deep-copy utilities |
| Proposal -> Current | Buffer alone uses the existing ArtifactBufferSnapshot/ArtifactData construction path to map and defensively copy accepted input into stored read values. Reuse/strengthen that path rather than a second Buffer copier |

ArtifactState's existing constructors borrow arrays; this plan does not silently make them immutable. Systems never retains the command input or proposal, and all commands obtain fresh Current. Copying at distinct ownership boundaries is intentional; duplicating the same mapping/helper in service, calculator and Buffer is prohibited. No mutable source array is retained by proposal or Buffer publication. The shared copy helper is Core.Contracts-only; the Buffer mapping contains no gameplay calculation. This E1 conversion is a delivered API.

#### Validation responsibility

| Owner | Required checks / behavior |
| --- | --- |
| Core calculation | Rotation/occupied cells, bounds, collision/self-overlap, inventory availability, supplied definition/instance lookup semantics, failure precedence, expansion validity, return-ID calculation and all existing edit behavior |
| Buffer publication | DTO structural shape, required/non-null fields, valid IDs/enums and membership representation, success/result/state consistency, defensive copying, initialize-once, output retention and no partial assignment on structural failure |
| Systems execution boundary | Current Buffer and DB facts, readiness and agreed admission, legal port call, exactly one approved publication, normal completion only after it returns |

Buffer must not re-run board-bound tests, collision, pool selection, DB definition lookup, expansion gameplay rules, returned-ID rules or HasBattle admission. Positive initialized dimensions and valid DTO fields are storage structure checks, not a second placement engine. Its owner tests structural rejection, rejected edits preserving Current, accepted state/result together, alias prevention, explicit absent preview and transient invalidation; no new bounds/collision test suite belongs in Buffer.

#### Preview/result lifetime and sequential publication

| Event | Required behavior / owner |
| --- | --- |
| New preview publication | Replace LastPreview only; no inventory/layout/result/character/combat mutation or input acknowledgement |
| Accepted edit, including a no-op | Publish state/result and clear LastPreview to null, preventing reuse of a pre-command proposal |
| Rejected edit | Delivered E1 policy: publish failure, preserve Current and clear LastPreview to null, so every completed edit invalidates the previous hover output. This policy is implemented and tested |
| Final Artifact reset | Clear Current and transient result/preview together at the agreed final data lifetime boundary; the owner/caller of that boundary is currently unresolved |
| Popup/screen close | E3 decides presentation clearing and its narrow authority. It must preserve Current; do not give UI the gameplay Clear capability |

Preview None means spatially valid only: it proves neither inventory ownership nor open mutation admission and cannot guarantee later Place success. Execution re-reads Current and calculates again. Preview never removes inventory, changes layout, activates effects, changes combat state or acknowledges input. Do not add sequence/history/revision fields unless a real E3 transport requires them.

Publication relies on synchronous sequential command execution through one authorized Artifact mutation boundary, with no external callbacks, yielding or re-entrant partial publication between preparation and completion. Normal rule failures are known before publication. This is not a lock/transaction guarantee. E3 must verify its real path; a later confirmed delayed/re-entrant/multi-Tick path requires only the minimum path-specific validation, not speculative revisions or a blocker for A1–A2.

Audit both SetSnapshot and Clear production consumers. SetSnapshot must not remain unrestricted mutation access. Clear must not run on popup close, battle cleanup, chapter change, stage transition or lobby return. Restrict or migrate those capabilities through their owner before A4 closure. Final reset authority/lifetime remains an external owner decision, with no invented profile/save/load/new-game/Run policy.

#### E1 implementation task

**Completed:** E1 implementation passed 79/79 Buffer/map/chapter tests and 186/186 combined regressions. See the E1 handoff for the delivered API and bypass audit. The checklist below records the completed task; A3 consumes these APIs without reopening E2/E4 decisions.

**Files:** Modify `B/Artifact/ArtifactBuffer.cs`, `B/Artifact/ArtifactBufferSnapshot.cs`, `B/Core/BufferSystemGroup.cs`, `K/DataBaseGroupServiceKey.cs`, `T/Framework/ArtifactBufferTests.cs` and the forwarding implementation in `T/Framework/ChapterResetSpy.cs`. Read `B/Artifact/ArtifactData.cs` and `Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs`; reuse their existing copying/construction route. No new Buffer, Framework implementation, DB lookup or assembly change is needed.

**Additional conversion signature:** `public ArtifactState ToCalculationState()` on ArtifactBufferSnapshot. It returns fresh item/cell arrays and preserves dimensions, ordering, IDs, membership, anchors, rotations and cells. Calling it on Empty throws InvalidOperationException. Buffer storage never retains this calculation input.

**Publication details:** Null method arguments throw ArgumentNullException without changing any field. TryInitialize checks for an already initialized Current after the null check and returns false without replacement or output changes; a first initialization validates/copies the complete snapshot before assigning it, with absent result/preview. PublishEdit and PublishPreview require initialized Current and otherwise throw InvalidOperationException. Accepted publication prepares the owned snapshot and matching result before any assignment; rejected publication changes only LastEditResult and the specified preview invalidation. Preview replaces only LastPreview. Reuse the immutable preview/shape contracts. Keep structural checks in snapshot construction/copying; do not invent generic transaction infrastructure or rerun Core geometry.

- [x] **Step 1 — Add initialization/conversion tests:** in ArtifactBufferTests, `TryInitialize_GrantsOnceAndPreservesOutputsOnRepeat` uses a 4x4 state with instance 0, then a different valid state and confirms the first aggregate is retained. `ToCalculationState_CopiesEveryFieldAndNestedCell` round-trips pool and placed items, mutates caller-owned arrays in both directions, and proves snapshots and earlier calculation inputs are detached. `EmptySnapshot_RejectsCalculationConversion` expects InvalidOperationException.
- [x] **Step 2 — Add publication/storage tests:** `PublishAcceptedEdit_StoresMatchingStateResultAndClearsPreview` covers placement and an accepted no-op; `PublishRejectedEdit_PreservesCurrentAndClearsPreview` covers a failure proposal and exact retained Current; `PublishPreview_ChangesOnlyPreview` preserves Current and LastEditResult. `InvalidInitialization_PreservesCurrentAndOutputs` covers dimensions, IDs, duplicate instances, enums, membership and empty/duplicate cells. `NullPublication_PreservesAllFields` and `UninitializedPublication_Throws` cover real pre-assignment faults. Use real public DTO construction; do not bypass A2 validation with reflection to fabricate invalid proposals, and do not add bounds/collision gameplay tests here.
- [x] **Step 3 — Add real composition tests:** `FactoryExportsNarrowArtifactPermissionsOnOneBuffer` resolves all six read/write permissions through the actual Buffer factory/group and checks they share the same Current/output state. Update ChapterResetSpy to forward the five new IBufferQuery properties. Preserve existing ArtifactReadSink consumers and run affected map/chapter regression fixtures.
- [x] **Step 4 — Run the E1 filter** in the verification table. Expected RED: the new members are absent. Preserve the existing Buffer fixture assertions; do not claim missing XML as test execution.
- [x] **Step 5 — Implement snapshot conversion and structural validation** in ArtifactBufferSnapshot using ArtifactData's existing defensive-copy path for storage and one inverse mapping for ToCalculationState. Validate storage representation, not placement rules. Preserve the existing snapshot/read types and GUIDs.
- [x] **Step 6 — Implement narrow permissions and publication** in ArtifactBuffer, then add the listed keys/query properties and exports on the same object. Prepare data before assignments. No callbacks/yields during publication. No initialization or inventory grant from binding, scene entry or service construction.
- [x] **Step 7 — Preserve legacy compatibility and record the bypass audit:** retain the existing SetSnapshot/Clear surface while its callers and test setup are migrated under A4; A3 must never receive it. Legacy SetSnapshot must clear obsolete result/preview output when replacing Current; Clear must clear Current and both outputs together. Add `LegacyReplacementAndClear_ResetTransientOutputs`. Audit both methods and record real consumers separately from fixture setup. Do not add an ordinary-flow Clear call or invent a final-reset caller.
- [x] **Step 8 — Run E1 and affected map/chapter tests** with nonzero executed counts, compare any failures to their current baseline, and check that only the listed Buffer/key/test files changed. Record the delivered signatures and remaining E3 presentation/reset decisions in the migration handoff before A3 consumes the APIs.

**Deliverable/gate:** The existing ArtifactBuffer supplies real narrow permissions, conversion, owned snapshots and consistent state/result/preview publication, with passing structural/alias/composition tests. A3 can then bind to these APIs. Final reset authority and popup output clearing remain E3/A4 lifetime decisions; they do not justify adding storage rules or a new lifecycle system to E1.

### E2 — Core group exposure

**Agreed by the user:** add public `IArtifactEditingService Artifacts { get; }` on IInGameCoreSystemGroupService and internal `InGameSystemServiceKey.Artifacts` of type `SystemGroupKey<IArtifactEditingService>`, plus ordinary Core factory/group wiring. This is a narrow feature service, not public access to concrete calculators or mutable Core state. No new root key, parent lifecycle contract or Framework implementation change is needed.

**Status:** Delivered and verified in A3 through CoreGroup.Artifacts and its typed key, preserving existing group properties and updating affected fake group implementations. E3 will connect Jobs/execution to the service; View submits intent and consumes Buffer reads instead of calling concrete calculation code.

### E3 — Startup and input/execution integration

**Agreed mechanism:** use the existing Job system for scheduling, one-time command execution and completion, and the existing LifeCycle system for flow entry/exit and returning to the owning screen. The current ArtifactPlan/ArtifactJob and ArtifactLifeCycle are skeletons to connect, not evidence of a missing scheduler or lifecycle framework. Neither Job nor LifeCycle becomes the inventory owner. Closing/reopening the flow preserves ArtifactBuffer.Current; first-initialization ownership is separate from repeated screen entry.

The current DB has definitions/InitialCount, but no confirmed initial board-size source or real bootstrap caller. The owner must supply positive authored dimensions and a legitimate initialization call before editing; no guessed production default. Core reads definitions, not a View-provided inventory snapshot.

The remaining feature wiring is the Artifact intent source/storage, registration in the existing Plan, initialization caller and authored dimensions, acknowledgement/result correlation, hover freshness and popup output clearing. Jobs call the Core group service and complete after synchronous publication returns; gameplay rejections are completed commands, while faults propagate through the existing execution fault path. LifeCycle owns flow entry/exit, not automatic inventory reset. Do not use CombatViewBuffer for lobby edits or add input fields to ArtifactBuffer. These are later E3 hookup changes, outside E1/A3 implementation files.

**Gate:** E3 blocks production activation, not explicit service integration tests with test dimensions. The handoff must distinguish these tests from real initialization and UI/Job connectivity. No Core purchase/drop/save functionality is inferred from missing authoring.

### E4 — Agreed factory-only concrete construction

**Agreed runtime dependency route:** Systems ArtifactEditingService -> Core.Contracts.IArtifactEditCalculation <- Core.Application.ArtifactEditCalculator -> Core.Domain.ArtifactPlacementRules. Only Application invokes concrete geometry. The pure port inherits no IServiceModule/Framework type and has no Buffer/DB/resolver parameter. Use one port for this cohesive editing/preview feature, not an interface per calculator or a Systems adapter duplicating rules.

**Exact creator:** `F/InGameCoreSystemFactory.cs` in ProjectL.Systems constructs `new ArtifactEditCalculator()` once per Core group, assigns it to `IArtifactEditCalculation`, then calls `new ArtifactEditingService(calculation).ExportServices(builder)`. The service constructor requires a non-null port and has no concrete/default fallback. The factory does not call any calculation method. This explicit factory exception replaces the previous blanket prohibition on concrete construction in Systems.

**Existing caller chain:** FrameworkRuntimeFactory -> InGameSystemFactory -> InGameCoreSystemFactory. Preserve the existing Create signature, optional battleSource argument and caller sites. Production-composition tests use the factory-created real calculator. Focused port-double tests construct ArtifactEditingService through its normal required-port constructor using existing test assembly access; they do not need a test-only factory parameter. No external creator, new assembly, service locator, reflection or additional factory layer is required.

**Status:** Delivered and verified in A3: the actual default factory constructs the calculator, injects its port and registers the service; ArtifactEditingService has no concrete calculator imports/calls. The existing ProjectL.Systems -> Core.Application reference is reused; pure Application/Domain assembly boundaries remain unchanged. This decision grants only the named factory construction exception and does not expand unrelated concrete calculation calls.

---

## A1 — Extract pure shared geometry

**Completed:** A1 implementation and its existing ArtifactMap regression filter passed 31/31 tests. Retain the completed checklist as implementation history; do not rerun implementation steps merely because A3 is pending.

**Files:** Create C/Domain/Artifact/ArtifactPlacementRules.cs and T/ArtifactPlacementRulesTests.cs. Read C/Domain/Artifact/ArtifactMap.cs and C/Domain/Tiles/TileFootprint.cs; do not delete or modify the old path yet.

**Interfaces:** `public static class ArtifactPlacementRules` exposes:

```csharp
ArtifactShapeSnapshot GetShape(ArtifactDefinition definition, Rotation4 rotation);
ArtifactActionFailureReason CheckPlacement(
    ArtifactState state, ArtifactShapeSnapshot shape, CellCoord anchor,
    int? ignoredInstanceId);
```

Consumes existing Core Contracts only. GetShape returns rotated local cells and swapped dimensions for R90/R270. CheckPlacement returns None, OutOfBounds or CellOccupied; it uses absolute placed-item Cells, ignoring exactly the optional moving instance. No inventory, DB, battle phase or effect checks. Shape data is copied; coordinates cannot wrap into a valid board cell on addition.

- [x] **Step 1 — Add tests:** `Rotation_UsesExistingTileFootprintConvention` asserts a horizontal two-cell shape at R90 becomes local (0,0)/(0,1), width 1, height 2, and covers R0/R180/R270. `BoundsTakePrecedenceOverCollision` combines an occupied cell and an out-of-board cell and expects OutOfBounds. `MovePreview_IgnoresOnlyOwnCells` permits overlap with the selected instance but rejects another instance. `EmptyInventory_DoesNotInvalidateSpatialPreview` expects None on a free board. `ExtremeAnchor_DoesNotWrapIntoBoard` expects OutOfBounds for int.MaxValue with a positive offset. `ShapeAndCheck_DoNotMutateInputs` checks supplied arrays/state after all calls; invalid Rotation4 throws.
- [x] **Step 2 — Run the A1 filter** using the command recipe below. New missing-type compilation failure is expected before implementation; once compilable, confirm assertions execute. Do not repeat unrelated historical RED tests.
- [x] **Step 3 — Implement the two methods** using existing TileFootprint rotation. Check all candidate bounds before testing any collision. Use widened coordinate arithmetic for validation and a command-local occupancy lookup only. Do not register this static class as a service or alter TileFootprint's shared API.
- [x] **Step 4 — Run A1 and existing ArtifactMapTests.** Both must execute with no new failures; old behavior remains intact while new geometry is independently available.

**Deliverable:** Pure shared spatial behavior, no runtime state or wiring change.

## A2 — Compose pure inventory/layout proposals

**Completed:** Final A2/A1/ArtifactService filter passed 107/107 tests. The [A2 handoff](../handoffs/2026-10-02-artifact-a2-calculation-handoff.md) records APIs, copy ownership, test mapping and the explicit changed-definition preview versus identical-move no-op compatibility exception.

**Files:** Create C/Application/Artifact/ArtifactEditCalculator.cs, C/Contracts/Artifact/IArtifactEditCalculation.cs, C/Contracts/Artifact/ArtifactEditProposal.cs, C/Contracts/Artifact/ArtifactPlacementPreview.cs and T/ArtifactEditCalculatorTests.cs. Modify only the targeted detached-copy helper in C/Contracts/Artifact/ArtifactState.cs. Read ArtifactServiceTests/ArtifactMapTests and existing ArtifactState/result definitions.

**Interfaces:** The implemented `public interface IArtifactEditCalculation` in Core.Contracts declares the following intent-specific methods. `public sealed class ArtifactEditCalculator : IArtifactEditCalculation` in Core.Application implements them without retained state. These are instance methods to cross the actual assembly boundary through one port; no generic command switch or forwarding implementation class is needed.

```csharp
ArtifactState CreateInitialState(int width, int height,
    IReadOnlyList<ArtifactDefinition> definitions);
ArtifactEditProposal Place(ArtifactState state, int artifactId,
    ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
ArtifactEditProposal Move(ArtifactState state, int instanceId,
    ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
ArtifactEditProposal ReturnToPool(ArtifactState state, int instanceId,
    ArtifactDefinition? definition);
ArtifactEditProposal ExpandBoard(ArtifactState state, int width, int height,
    IReadOnlyList<ArtifactDefinition> placedDefinitions);
ArtifactPlacementPreview PreviewPlace(ArtifactState state, int artifactId,
    ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
ArtifactPlacementPreview PreviewMove(ArtifactState state, int instanceId,
    ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
```

`ArtifactEditProposal(ArtifactActionResult result, ArtifactState? state)` exposes get-only `ArtifactActionResult Result` and `ArtifactState? State`. Success requires a structurally valid detached State; rejection requires null. Reject successful null, rejected non-null, invalid/default result metadata and structurally invalid state arguments. A successful no-op still carries valid detached unchanged data. Call the single `ArtifactState.DetachedCopyOf(source)` helper for owned item/cell arrays; do not duplicate it in calculator/service. This is a transfer proposal, never a service field. Use file-local nullable annotations on these new contracts/implementations; no project-wide nullability change.

`ArtifactPlacementPreview(ArtifactShapeSnapshot? shape, CellCoord anchor, ArtifactActionFailureReason failureReason)` exposes get-only `ArtifactShapeSnapshot? Shape`, Anchor and FailureReason. Accept only None, OutOfBounds, CellOccupied, DefinitionNotFound or InstanceNotFound. Missing-definition/instance outcomes use null Shape; None/OutOfBounds/CellOccupied require a rotated Shape. Reject every inconsistent combination. Reuse ArtifactShapeSnapshot instead of another cell model. Both preview methods invoke A1 inside Application and share the edit geometry; Systems never calls A1 directly. Preview has the strictly spatial meaning and lifetime defined in E1.

Definition is null for a missing requested definition in Place, yielding DefinitionNotFound. Move/Return first report InstanceNotFound when the target is absent; an existing target without its matching definition is a broken input/DB contract and throws. A non-null mismatched definition also throws. Expand receives one definition per distinct placed ArtifactId; missing or duplicate conflicting facts throw. These checks do not query DB or retain a catalog.

- [x] **Step 1 — Add calculation tests** with a 4x4 board, normalized two-cell and single-cell definitions, and instance zero. `InitialInventory_UsesInitialCountAndSuppliedOrder` verifies contiguous IDs starting at 0 and no placed items; reject duplicate definition IDs. `Place_PreservesFailurePrecedence` covers missing definition, missing inventory, bounds and collision in that order. `RejectedOperations_PreserveInputAndPriorProposals` compares full pool/placed data after each rejection. `Move_KeepsIdentityAndAllowsSelfOverlap` covers rotation and identical-move success. `Expansion_PreservesItemsAndRejectsInvalidShrinkUnchanged` preserves every ID/anchor/rotation and checks the existing three size failure reasons.
- [x] **Step 2 — Add identity/copy tests:** place and return instance 0 in an otherwise empty inventory; expect returned pool ID 1 and success InstanceId 0. Place that returned item, return again and expect pool ID 2. An existing int.MaxValue ID makes ReturnToPool throw OverflowException without changing input. Mutating arrays originally passed to the proposal constructor must not change the proposal. Invalid rotation with an available target throws before any mutation; a valid preview with empty inventory still produces NotInPool on Place.
- [x] **Step 2a — Merge contract cases into the same fixture:** `Proposal_RejectsInvalidResultStateCombinations` covers successful null, rejection with candidate state, malformed success state and default result; successful no-op has a detached state. `Preview_RequiresShapeOnlyForSpatialOutcomes` covers all allowed reasons and rejects inconsistent null/shape pairs. Invoke through IArtifactEditCalculation and compare PreviewPlace/PreviewMove with corresponding edit geometry; no separate generic-contract suite.
- [x] **Step 3 — Run A2** and confirm the new contracts/calculations are missing or the new assertions fail for their intended reason.
- [x] **Step 4 — Implement the port, proposal construction and operations.** Only the Application implementation calls A1 for edit/preview geometry. Preserve pool order and select its first matching ArtifactId. Pool cells are definition-local at zero anchor/R0; placed cells are rotated absolute coordinates. CreateInitialState consumes the supplied definition order; Systems will sort real DB values by ArtifactId. Check ID arithmetic before constructing outputs. Preserve existing Return success identity and append the new pool instance. No global allocator field or retained catalog is needed for this closed operation set.
- [x] **Step 5 — Preserve expansion behavior.** Validate size before rebuilding a local proposed placement list through A1 with DB definition facts. Preserve BoardRebuildFailed for a geometric rebuild failure and throw on malformed/missing definition facts. Never partially mutate the supplied state. Do not activate costs or player-facing upgrades.
- [x] **Step 6 — Run A1/A2 and existing ArtifactServiceTests.** New tests pass and the retained owner path still behaves as before. Record the old-test-to-new-test mapping for A4.

**Deliverable:** Independently testable Core rules and detached proposals. This is not Buffer publication or migration completion. Retain old owners until A3/A4 gates pass.

## A3 — Bind and register the focused processing service

**Prerequisites:** Delivered A1–A2 and completed E1 permissions/conversion/publication tests. E2 exposure and E4 factory-only construction are already agreed; implement those decisions in this task. Verify existing Framework registration/PostBindService propagation through integration tests. If E1 is incomplete, report that concrete dependency instead of reopening E2/E4 decisions or adding a temporary store, SetSnapshot workaround or nonfunctional facade.

**Files:** Create S/ArtifactSystem/ArtifactEditingService.cs, T/Framework/ArtifactEditingServiceTests.cs. Modify the three Core composition files listed above and affected existing Core-group test doubles. E1's listed Buffer changes are a completed preceding task; do not mix new storage responsibilities into A3.

**Interfaces:** In the new service file, `public interface IArtifactEditingService : IServiceModule` exposes:

```csharp
bool InitializeBoard(int width, int height);
void Place(int artifactId, CellCoord anchor, Rotation4 rotation);
void Move(int instanceId, CellCoord anchor, Rotation4 rotation);
void ReturnToPool(int instanceId);
void ExpandBoard(int width, int height);
void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation);
void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation);
```

InitializeBoard deliberately avoids the Framework lifecycle name Initialize. It returns false for an already initialized Buffer without regrant/reset; a first initialization during HasBattle throws InvalidOperationException as invalid bootstrap ordering. Calls before DB/binding or edits/previews before board initialization also throw InvalidOperationException. Runtime edits during HasBattle publish LayoutLocked before DB definition lookup or geometry. Preview is a read-only spatial calculation and may publish display output without changing gameplay state; production UI availability remains the execution owner's responsibility.

**Admission location:** keep this narrow battle-lifetime admission at the Systems execution boundary, consistent with the inspected CharacterShopService pattern. One private check of CombatStateBuffer.HasBattle serves all edit methods and first initialization; it is execution eligibility, not layout calculation or storage validation. Do not copy it into Buffer, Jobs or a second calculator policy. Commands enforce it even if UI submits an invalid intent. UI availability is presentation and cannot replace authoritative command admission. Preparation/active/retained terminal states are locked until EndBattle, while lobby/stage selection are available when no battle is retained.

**Result-channel choice — Option B:** use void for the four edit commands and publish LastEditResult as the single outcome channel. The inspected ArtifactJob currently has Execute(): void and marks completion after normal return; it contains no Artifact result-branching consumer, and E3 has not established one. Therefore a second production return solely for diagnostics/tests is unjustified. Normal return follows completed publication, including a gameplay rejection; faults propagate and prevent normal completion. View reads only Buffer. If E3 later demonstrates a synchronous branching need, explicitly amend the contract to Option A and add returned-versus-published semantic equality tests; do not pre-add that channel. InitializeBoard's bootstrap bool remains distinct from edit outcomes.

- [x] **Step 1 — Add real-composition tests:** call the default InGameCoreSystemFactory path so it constructs the real ArtifactEditCalculator, use the existing Buffer group, bind through the actual resolver/typed DB context and invoke through the Core group interface. No friend-access or test asmdef expansion. DB test definitions are labeled fixtures, not production authoring. `FactoryRegistersServiceAndBindingModule` verifies group access and parent InitData/PostBind propagation. `BindingDoesNotInitializeOrGrantInventory` verifies Current remains empty until InitializeBoard. `ServiceUsesInjectedCalculationPort` separately verifies the required constructor port; a port double is not the real calculation integration witness.
- [x] **Step 2 — Add publication tests:** `InitializeTwiceAndRecreateService_PreservesCurrentInventory` uses the same real Buffer across new service composition; InitializeBoard returns false after the first grant. `EveryMutationWhileHasBattle_PublishesLayoutLocked` covers Place/Move/Return/Expand during preparation, active combat and retained terminal outcome; Current is unchanged. After real battle cleanup, a valid edit succeeds. `ChapterAndBattleTransitions_PreserveArtifactAggregate` uses existing public progression/battle APIs and checks the full Artifact state is preserved. `PreviewPublishesOnlyDisplayOutput` checks Current, LastEditResult and character/combat/currency data are unchanged. `AcceptedEdit_PublishesMatchingStateResultAndClearsPreview` checks the actual final layout, matching published result and null LastPreview after normal completion. `RejectedEdit_PublishesFailureWithoutChangingCurrent` checks geometry/inventory failures, retained Current and null LastPreview according to E1's policy. Do not duplicate E1's structural unit tests.
- [x] **Step 3 — Add failure/consumption tests:** overflow and throwing DB queries preserve Current and do not publish success; a publication sink fault is propagated, never converted to a business rejection. Use a narrow throwing sink only for that fault-path test, not to claim real Buffer validation. `AcceptedPlacementFeedsExistingCombatEffectsOncePerInstance` places fixture artifacts through the service, then executes an existing real combat command path and checks the expected supported effect magnitude/multiplicity; pooled items contribute nothing. Do not expose or invoke private ReadArtifactLoadout using reflection.
- [x] **Step 4 — Run A3 tests** against the delivered E1 API; missing service/registration is expected before implementation. Unrelated pre-existing suite failures must be recorded separately.
- [x] **Step 5 — Implement binding:** receive IArtifactEditCalculation through E4's agreed constructor injection; no concrete fallback. Implement ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService and IExportSystemServices. InitData keeps `data.InGameQuery.ArtifactDefinitionTableQuery`. PostBind uses the existing Buffer query locally to obtain only current read, initialization/edit/preview publication and combat-state read permissions supplied by E1. No broad Artifact writer/Clear authority, entire context, snapshot, map or pool field. No Tick/Shutdown work or destructor clearing inventory.
- [x] **Step 6 — Implement command orchestration:** check readiness/admission, call the agreed snapshot-to-calculation conversion, query required definitions, invoke the Contracts port method, PublishEdit exactly once, then return normally with no duplicate result channel. For Move/Return, locating the target ID to obtain its definition is fact assembly; leave missing-instance semantics in the pure port implementation. Expand assembles distinct placed definitions after admission; size/rebuilding decisions remain in calculation. Malformed DB facts are exceptional input failures. No callbacks, yielding or retained proposal between calculation and publication; no direct concrete Application/Domain calls or duplicated rules.
- [x] **Step 7 — Implement initialization/preview:** check Current before reading InitialCount, sort GetAllDefinitions by ArtifactId, invoke port.CreateInitialState and Buffer TryInitialize. The Buffer owns the initialize-once guarantee without concurrency machinery. Preview invokes port.PreviewPlace/PreviewMove using freshly converted facts and then PublishPreview; shape/lookup-failure composition and A1 calls occur only inside Application. No inventory subtraction, effect activation, acknowledgement or combat cleanup.
- [x] **Step 8 — Add agreed Core composition:** preserve InGameCoreSystemFactory.Create's existing signature and callers. This factory alone constructs ArtifactEditCalculator, assigns its Contracts port and constructs one ArtifactEditingService(port). ExportServices adds the service/module once; the Core group exposes Artifacts through its typed key. Keep existing parent InitData/PostBindService propagation. No service-side concrete fallback or changes to upstream factories are needed.
- [x] **Step 9 — Run A3 and affected existing combat/publication suites.** Verify the default real-calculator factory path, required service constructor injection, narrow Buffer permissions, accepted publication, rejection and faults. The only new Systems concrete Artifact Application reference is the approved construction import/call in InGameCoreSystemFactory; no concrete imports/calls in ArtifactEditingService, no Domain calls in Systems, no new asmdef references or friend-access changes. Report real DB/Job/UI paths as unconnected until E3 acceptance is actually performed.

**Delivered — 2026-10-03:** Registered Core processing uses the real canonical Buffer through the specified permissions. All 30 service cases pass. The final affected regression passes 490/495 tests; the five failures exactly match the existing Economy authoring baseline. A fresh read-only review found no Critical/Important/Minor issues. See the [A3 handoff](../handoffs/2026-10-03-artifact-a3-service-handoff.md) for APIs, evidence and remaining gates. CombatProcessingService received no A3 changes. Production popup/startup integration remains E3; legacy retirement remains A4.

## A4 — Retire obsolete Core owners and deliver the handoff

**Prerequisites:** A3's real legal calculation connection/publication verified, migrated behavior coverage, live caller and GUID audit, and resolved SetSnapshot/Clear production bypasses. The canonical Buffer route must be the only active runtime inventory/layout owner. No production/shared consumer may be abandoned merely because a search on this branch found only tests; report branch integration dependencies through the existing handoff process.

**Files:** Source/meta retirement of C/Application/Artifact/ArtifactService.cs, ArtifactServiceFactory.cs; C/Domain/Artifact/ArtifactMap.cs, ArtifactPool.cs, ArtifactBoard.cs, ArtifactLayoutAccessState.cs, ArtifactCatalog.cs and ArtifactPlacementSnapshot.cs; C/Contracts/Artifact/IArtifactService.cs, IArtifactLoadoutLease.cs, IArtifactCatalogQuery.cs; C/Contracts/Results/ArtifactResult.cs. Each additional helper was proven orphaned after caller migration. Preserve C/Contracts/Snapshots/ArtifactSnapshot.cs because ArtifactEditCalculator uses ArtifactCreateSnapshot. Retire T/ArtifactServiceTests.cs, ArtifactMapTests.cs and ArtifactLayoutLeaseTests.cs after assertion mapping; update the named Buffer/query/key surfaces and affected fixtures for initialize-once setup. Create the handoff path listed above.

- [x] **Step 1 — Capture the targeted consumer/GUID and bypass inventory:** search for the six types, interfaces, factory calls and leases; inspect serialized references to each candidate script's `.meta` GUID. Follow typed Artifact write-sink consumers for both SetSnapshot and Clear, including variables obtained through the Buffer query. Record each live caller, owner and migration/restriction; separate historical docs/test setup from production. Ordinary popup, battle, chapter, stage and lobby transitions must not Clear inventory. Final reset authority remains an explicit external lifetime decision; an unavailable reset policy grants nobody implicit Clear permission. A4 cannot remove a live contract or close with unreviewed mutation/reset bypasses.
- [x] **Step 2 — Finish behavior coverage:** map initialization, spatial semantics, failure precedence, identity, expansion, copying and placed-effect multiplicity to A1–A3 tests. Preserve active battle read/settlement regressions. Explicitly retire lease disposal, stale revision and revision-overflow tests as obsolete ownership mechanics; retain their useful state-preservation assertions at current boundaries.
- [x] **Step 3 — Remove only obsolete Core ownership.** Delete the six owner files and paired metas only when their consumers are gone. Audit mixed ArtifactResult/ArtifactSnapshot, ArtifactCatalog/query and placement DTO members individually; preserve referenced values. Do not remove ArtifactLoadoutSnapshot, ArtifactPlacedEffectSnapshot, ArtifactEffectCompiler, SoulEffectCalculator, StageSoulService, ArtifactActionProjection, ArtifactAttackPowerEvaluator, ArtifactCombatFacts, ArtifactActivationCalculator or Domain rules just because their names mention Artifact/Stage.
- [x] **Step 4 — Run final affected suites and boundary checks** below; verify Core/Systems compilation and no new failures. Check diff scope against the incoming workspace: allow the listed E1 Buffer/key/test changes and A3 Core composition exception; no Framework/View/Job/DB/assets implementation changes or editing APIs in CombatProcessingService. Verify actual port injection and reference direction, one active canonical Buffer state owner, no persistent legacy state/lease owner and resolved SetSnapshot/Clear consumers. Compilation alone does not close the architectural gate.
- [x] **Step 5 — Write the English handoff:** actual service access/signatures, factory-only concrete construction and port injection, initialization caller requirements, delivered E1 Buffer permissions/conversion, confirmed lobby/stage availability, single admission guard, faults versus gameplay rejections, single published edit-result channel, preview lifetime decisions, return-ID compatibility, retired types and bypass/reset audit. Include exact verification and E3/G9/F1/F2 dependencies. Distinguish delivered Core calculation, E1 Buffer storage, A3 binding and remaining E3 execution/play verification. Record the E1 APIs actually used rather than describing them as an unavailable external contract.

**Delivered — 2026-10-03:** Core editing migration closure and the [English handoff](../handoffs/2026-10-02-artifact-core-migration-handoff.md). Twelve production source/meta pairs and three obsolete fixture pairs are retired. All old writer/export/query/key/SetSnapshot/Clear paths are removed, with no guessed reset owner. Full affected regression passes 1300/1305; the five names/messages exactly match pre-A4 Economy authoring failures. Production E3 remains pending. No automatic commit/push; when requested, stage only reviewed task files with a Korean `[천명] ...` commit title.

## Verification commands and evidence

These commands apply when executing the remaining tasks. Completed A1/A2/E1/A3 evidence is recorded in their sections and handoffs. Unity batch runs require the previously approved AppData-access execution path; do not delete caches or upgrade Unity for the known sandbox-only CurlRequestCache issue. Check for an existing editor using this project before launching another instance; do not terminate a user's editor.

Run this PowerShell recipe from the project root with a unique task/run output directory. `-testFilter` separates fixture names with semicolons; substitute only the filter shown in the table. Do not use `-quit` with the test-runner command.

```powershell
$artifactProject = (Get-Location).Path
$artifactEvidence = Join-Path $artifactProject '.superpowers/sdd/artifact-migration/A1-first'
New-Item -ItemType Directory -Path $artifactEvidence -Force | Out-Null
$artifactFilter = 'ProjectL.Tests.Core.ArtifactPlacementRulesTests'
$artifactArguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $artifactProject + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-testFilter', $artifactFilter,
    '-testResults', ('"' + (Join-Path $artifactEvidence 'results.xml') + '"'),
    '-logFile', ('"' + (Join-Path $artifactEvidence 'editor.log') + '"'))
$artifactProcess = Start-Process -FilePath 'D:/Program Files/Unity/Hub/Editor/6000.0.73f1/Editor/Unity.exe' -ArgumentList $artifactArguments -PassThru -WindowStyle Hidden
$artifactProcess.Id
```

Poll this spawned process and read its XML/log without a long blocking wait. Record exit code, nonzero intended test count, pass/fail/skip counts, failure names/messages and compilation/import errors. Missing XML is not a passing run. Preserve imported user files using an incoming snapshot comparison; do not restore unrelated files from HEAD.

| Task | Filter / verification |
| --- | --- |
| A1 | `ProjectL.Tests.Core.ArtifactPlacementRulesTests;ProjectL.Tests.Core.ArtifactMapTests` |
| A2 | `ProjectL.Tests.Core.ArtifactPlacementRulesTests;ProjectL.Tests.Core.ArtifactEditCalculatorTests;ProjectL.Tests.Core.ArtifactServiceTests` |
| E1 | `ProjectL.Tests.Framework.ArtifactBufferTests;ProjectL.Tests.Framework.MapProgressBufferTests;ProjectL.Tests.Framework.ChapterGraphPreparationTests` |
| A3 | `ProjectL.Tests.Framework.ArtifactEditingServiceTests;ProjectL.Tests.Framework.ArtifactBufferTests;ProjectL.Tests.Framework.CombatCommandPublicationTests;ProjectL.Tests.Framework.CombatCommonPublicationTests` |
| A4 | All `ProjectL.Tests.Core`, `ProjectL.Tests.Framework` and `ProjectL.Tests.CombatViewBoundary` assemblies once, using `-assemblyNames 'ProjectL.Tests.Core;ProjectL.Tests.Framework;ProjectL.Tests.CombatViewBoundary'` in place of `-testFilter` |

Useful read-only/static checks:

```powershell
rg -n 'ArtifactServiceFactory|IArtifactService|IArtifactMapService|IArtifactPoolService|ArtifactLayoutAccessState|IArtifactLoadoutLease' Assets/ProjectL/Script Assets/Tests
rg -n 'IArtifactBufferWriteSink|ArtifactWriteSink|SetSnapshot|\.Clear\(' Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem
git diff --check
git diff --name-status
git status --short
```

Review remaining search hits rather than requiring blindly empty output. The bypass query above is a starting point; follow actual typed-sink references to any direct production caller outside those directories. This is a targeted contract audit, not an instruction to inspect unrelated project code. Verify that new Systems references to the concrete ArtifactEditCalculator occur only in InGameCoreSystemFactory construction, never in runtime service methods. Systems must not call ArtifactPlacementRules. The existing Application asmdef reference is reused; no additional assembly/friend-access change is permitted. Verify registration and binding, pure calculations unable to write Buffer, and owned data at proposal/storage boundaries. One-time execution must be checked against the real E3 Job path when connected, without adding a generic Runner or speculative revision/lock tests.

Historical baseline evidence exists in `.superpowers/sdd/combat-processing-cleanup-2026-10-02/verification.json` and linked 8R6 evidence: the cleanup run had Framework 319/324 and boundary 6/6 with five failures matching its earlier baseline. These are historical numbers, not this plan's results. Re-evaluate the current baseline at implementation time, identify exact failures, and report new/changed failures rather than claiming all green from an unchanged count. Managed-only compilation/tests, if needed, must be reported separately from Unity and actual play verification.

## Completion boundary

| Status | Required evidence |
| --- | --- |
| Core calculations ready | A1 pure geometry and A2 detached proposals/Contracts port pass; no runtime hookup, Buffer publication or migration completion implied |
| Buffer publication ready | E1 implements the listed narrow permissions, conversion and sequential owned publication; structural/alias/composition tests pass; lifetime and legacy bypass status are recorded |
| Core processing ready — delivered A3 | Real default-factory/group tests pass through E1 APIs, narrow E2 exposure, factory-only concrete construction/port injection and existing Framework registration/PostBind propagation; 30/30 service cases pass |
| Legacy Core owner retirement ready — delivered A4 | Real caller/GUID and behavior audits complete; obsolete owners and SetSnapshot/Clear capability removed; Buffer is sole retained inventory/layout owner; Core 918/918 and boundary 6/6 pass with unchanged five Framework baseline failures |
| Production Artifact editing ready | E3 authored dimensions/bootstrap, approved Plan/Job execution, required input acknowledgement/correlation, View Buffer reads, preview freshness/close/rejection policy and sequential path connected and Unity play-verified by their owners |

Do not mark the last row complete from any earlier row. Existing incomplete Artifact DB authoring and G9 effect-profile decisions, F1 execution fault handling and F2 combat exit/acknowledgement remain their separately tracked work. This feature does not finish them or expand ArtifactEditingService to own them.

## Earlier document revision evidence

This user-directed revision updates this plan, the design decision addendum and the A2 handoff's next-step status. It adds an executable E1 task, records E2's narrow exposure agreement, assigns E3 timing/lifecycle work to the existing systems, and replaces E4's unresolved external-creator gate with factory-only concrete construction. A1/A2 implementation evidence is retained and their checklists are marked complete. The currently missing E1 APIs and pending E3 feature hookup are distinguished from decisions already resolved by the user.

ArtifactBuffer, ArtifactBufferSnapshot, ArtifactData, BufferSystemGroup/query, ArtifactBufferKey, Core factory and Artifact Job/LifeCycle declarations were inspected to ground paths and integration details. No production code, tests or assets were changed or executed in this revision; no commit/push is claimed. The rejected-edit preview-clearing behavior is an explicit planned E1 policy. Popup output clearing, authored board dimensions, exact intent delivery and final reset authority remain the documented E3/A4 integration details, not requests to invent new scheduling or lifecycle infrastructure.

## E1 implementation evidence — 2026-10-02

The later user request authorized E1 implementation while preserving the existing Buffer/framework responsibilities. The six listed source/test files now deliver the narrow permissions, structural validation, detached conversion and state/result/preview publication. Buffer remains the single state owner; no gameplay calculation, scheduling or lifecycle behavior was added.

Unity 6000.0.73f1: pre-E1 baseline 51/51 passed; RED compiled only missing planned members and executed no assertions; E1 GREEN 79/79 passed (31 ArtifactBuffer, 8 MapProgressBuffer, 40 ChapterGraphPreparation); combined E1/A1/A2/retained-service regression 186/186 passed, zero failures/skips, exit 0. Fresh read-only review found no Critical/Important/Minor issues. The English E1 handoff records exact APIs, legacy authority exposure and remaining E3/A4 details. Incoming changes are preserved; no commit/push is claimed.

## A3 implementation evidence — 2026-10-03

The user authorized A3 registration/binding after E1. The existing Core factory now constructs ArtifactEditCalculator through IArtifactEditCalculation and registers one ArtifactEditingService service/module. CoreGroup.Artifacts exposes only IArtifactEditingService. The service keeps narrow definition/read/publication permissions, obtains fresh Buffer calculation input, checks HasBattle before mutation fact assembly, and publishes a single result channel. It neither owns inventory nor clears it. Three affected test-double files supply the new complete-group interface/query contracts.

Unity 6000.0.73f1: affected baseline 84/84 passed; RED failed compilation only for the absent service/interface. Corrected initial GREEN passed 86/86. Final expanded filter passed 490/495 (30/30 new service cases, 107/107 Artifact calculation/retained-service cases, 6/6 CombatViewBoundary; Framework 377/382), zero skips, Unity exit 2. Exact names/messages of the five Economy authoring failures match the existing Task 8R6 baseline. No new A3 failure remains. A fresh read-only review found no Critical/Important/Minor issues. Incoming Assets hashes are preserved; normal git diff --check passes. See the dated A3 handoff for evidence, actual APIs and separate A4/E3 gates. No commit/push is claimed.

## A4 implementation evidence — 2026-10-03

The user authorized A4 retirement after the registered service delivery. Source callers and serialized GUID references were audited before removal. Twelve orphaned production source/meta pairs and three obsolete fixture pairs are retired, with retained compiler/rules/shared values preserved. ArtifactCreateSnapshot stays because CreatePoolItem consumes it. IArtifactBufferWriteSink and all replacement/reset export/query/key/implementation paths are gone; live fixture setup uses TryInitialize. No final reset authority or production caller was invented.

The capability regression executed RED on the old castable writer, then passed after removal. Full approved EditMode assemblies changed from 1329/1334 to 1300/1305, both with exactly the same five Economy failure names/messages and zero skips (Unity exit 2). Current Core 918/918, Framework 376/381, CombatViewBoundary 6/6; all 30 service cases pass. The 29-case decrease is explained by mapped/retired owner mechanics and the two migrated/additional cases. A fresh read-only final reviewer approved all 11 modified A4 files and 15 deleted script/meta pairs with no Critical/Important/Minor issues; all removed GUIDs have zero serialized references. Normal git diff --check passes, retained GUIDs and incoming non-A4 work are preserved. The English closure handoff records exact APIs, behavior coverage and E3/G9/F1/F2 dependencies. No commit/push is claimed.

The task-filter table above records the historical A1/A2/E1/A3 runs. A4 retires ArtifactMapTests and ArtifactServiceTests; current closure acceptance uses all three approved assemblies rather than those retired fixture names.