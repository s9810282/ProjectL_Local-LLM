# Optional Development Tools Implementation Plan

> **For agentic workers:** Use `superpowers:executing-plans` or `superpowers:subagent-driven-development` for an explicitly authorized phase. Repository ownership rules and this plan's stop points override generic skill instructions. Never commit or push. Reading this plan, or being asked to write it, is not implementation authorization.

**Goal:** Add optional development APIs for grants and reward-free forward stage bypass while preserving the original game's responsibilities and normal execution paths.

**Architecture:** A small View development facade calls a Core development service. The Core service reuses roster acquisition and existing Buffer stores through narrowly scoped development transactions. Ordinary stage entry remains unchanged; development navigation only prepares a legitimate predecessor in the existing graph.

**Tech stack:** Unity 6000.0.73f1, existing C# assemblies, NUnit EditMode tests. No package, framework, event bus, network server, command middleware or new asmdef.

**Spec:** [Optional Development Tools Design](../specs/2026-10-23-development-tools-design.md).

**Status:** A/B and the authorized C implementation are delivered. C1/C2 focused checks and actual Development/Release Player builds are verified; final Phase C acceptance remains open for actual-scene round-trip and manual View checks. Baseline reference: `8644c702` plus delivered prior phases. Exact scope, baseline failures and acceptance limits are recorded in §13–15.

**Second review:** 2026-10-23. Corrections and evidence are recorded in §12 and design §12. The allowed composition additions below are explicit exceptions, not permission to change gameplay signatures.

## 1. Non-negotiable execution rules

1. Read the spec, this plan, repository `AGENTS.md`, `Assets/AGENTS.md`, and the current code for the authorized task. Older feature documents are not authority for changed code.
2. The feature is optional. Existing production modules must not require development services or learn development policies.
3. `Assets/ProjectL/Script/Framework/**`, all `*.asmdef`/`*.asmref`, FieldCamera, Render, existing Architecture infrastructure, packages, scene files, prefabs and SO assets are outside this plan's modification scope.
4. Do not add development cases to `CombatPlan`, `StageFlow`, ordinary `ChapterNavigationRules`, normal completion calculators, `StageViewCommandKind`, `JobType`, `StageOutcome`, or `RunOutcome`.
5. No Core-to-View dependency, pure-Core-to-Systems dependency, root export of child services, public raw-store exposure, reflection-based access, service locator hidden in helpers, or direct gameplay Buffer mutation from View.
6. Reuse existing features through the named routes below. Do not replace a route merely because constructing its fixture or satisfying its preconditions is inconvenient.
7. New code and its references use `UNITY_EDITOR || DEVELOPMENT_BUILD`. An optional absence is explicit; a required dependency unexpectedly missing throws. Do not hide misconfiguration with empty successes.
8. Documents are English. New code/XML comments are Korean. Preserve namespace/folder alignment, Allman braces, 4 spaces, UTF-8/LF, and `.meta` GUID rules.
9. Preserve unrelated work. Do not stage, commit, push, send handoffs externally, or start the next phase automatically.
10. Core/Buffer work in A/B requires explicit authorization for that owner. View-only authorization does not grant it; record a request in the existing boundary handoff instead.

### 1.1 Mandatory stop protocol

Stop the affected task before changing code if any of the following is necessary:

- A file outside that phase's allowlist must change, except documentation/test evidence files expressly listed here.
- An existing signature (beyond the exact additive Group parameters and composition-only options in §3.2), required constructor argument, normal result meaning, ownership rule, execution order or service hierarchy must change.
- A task would need Framework/asmdef changes, a normal force-entry flag, a synthetic victory, or a new persistent source of gameplay truth.
- The current checkout no longer supports a named reused API, or an approved invariant is contradicted by current code.
- A supported scenario cannot be implemented without silently broadening the API's timing or stage-type scope.

Report the exact task, contradictory code, smallest candidate change, affected owners and necessary regression scope. Propose a spec/plan amendment and obtain authorization before implementing that deviation. Do not rewrite this plan after implementing an alternative and claim compliance. Routine local variable names and equivalent algorithms within the specified contract do not require a new decision.

### 1.2 Guideline source and how it affects execution

The user supplied `D:/My_ProjectWork/학교 관련 문서/졸업작품_프로젝트L/2학기 내용/AI 관련 문서/feature-change-locality-guidelines.md`. Treat it as the design-review reference requested by the user, not as independent permission to modify code.

Every task report must name: the actual change scenario; edit scope; required understanding; verification scope; whether coupling is required by shared state or introduced by the implementation. Interface count, fewer files, and fewer lines alone do not establish improvement. Do not skip necessary regression checks to claim locality.

Project criteria also apply: [handoff §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙), [Framework guidelines](../framework/Framework%20Development%20Guidelines.md), and [FW contracts](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md). These references do not authorize editing Framework.

## 2. Fixed route map and prohibited substitutes

| Feature | Required route | Forbidden substitute |
|---|---|---|
| Unit grant | facade → Core development admission → `ICharacterRosterService.Acquire(key)` | Copying `CharacterRosterRules`, `InitializeRoster`, direct CharacterBuffer writes, forced acquisition, auto deck insertion |
| Gold grant | facade → Core checked addition → development transaction → `ICurrencyBufferStore.PrepareAmount/InstallAmount` | `SetAmount`, reward simulation, editing shop prices, alternate gold balance |
| Skip | facade → development planner → atomic selection + node-clear publication → `RequestMapRedraw` | Battle victory/abandonment, reset Run, public `SelectStage/SetClear` loop |
| Jump preparation | planner follows real forward edges and bypasses eligible intermediates → same atomic publication | Arbitrary scene change, direct `EnterBattle`, graph regeneration, fake predecessor, content override disguised as progress jump |
| Target entry after preparation | existing StageSelect interaction → `StageViewWriteSink.Submit(StageViewCommandData.SelectStageNode(...))` → existing Flow/entry/result observer | New entry-command kind, duplicate input queue, dev-specific battle loader, direct Job/scene calls |
| Availability | existing `IDeckEditAvailabilityRead` plus authoritative Run/battle checks; View-owned presentation facts | Widening deck-edit policy, Core reading View classes, a cached enabled-button flag as authority |
| Map refresh | existing `StageMapViewSystem.RequestMapRedraw()` | Scene reload, alternate map renderer, swallowing Event restoration exceptions |
| Unexpected facade exception | the existing shared `RuntimeFaultBoundary.Report`, then rethrow | New error system, a second StopInput connection, success-shaped fallback, retry after a committed mutation |

`PrepareJumpToNode` intentionally stops before ordinary target entry. The host waits for redraw and selects the target normally. Do not add an asynchronous auto-entry controller in this implementation.

Additive gold and pre-entry bypass are new development mutation operations because no complete existing command performs them. Their justified extension is at the current owners' narrow transaction boundary. This is not permission to build a second roster, reward, entry or completion implementation.

## 3. File placement and ownership

The aliases below are exact repository-relative roots, used to keep task paths readable. Expand them before editing. Every new `.cs` and folder under Assets needs its `.meta`; no new assembly boundary is needed.

| Alias | Exact root |
|---|---|
| `SYS` | `Assets/ProjectL/Script/Systems` |
| `RT` | `Assets/ProjectL/Script/Systems/RuntimeSystem` |
| `CORE` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem` |
| `CONTRACTS` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts` |
| `APP` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application` |
| `BUFFER` | `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems` |
| `VIEW` | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem` |
| `TEST` | `Assets/Tests/EditMode` |

### 3.1 New code responsibilities

| File | Owner and contents |
|---|---|
| `CONTRACTS/Development/DevelopmentContext.cs` | Immutable caller snapshot; no services or Unity types |
| `CONTRACTS/Development/DevelopmentResults.cs` | Failure enum and immutable operation results; no mutable global history |
| `CONTRACTS/Development/DevelopmentNavigationInput.cs` | Immutable pure planner input, node descriptors and plan output |
| `APP/Development/DevelopmentNavigationPlanner.cs` | Only development path/bypass policy; no storage or normal gameplay policy replacement |
| `CORE/Development/IDevelopmentStateCommands.cs` | Internal runtime Core command contract plus internal `IDevelopmentStateProvider` accessor implemented by the existing Core Group; `IServiceModule` belongs here, not in pure Contracts |
| `CORE/Development/DevelopmentStateService.cs` | Lifecycle binding, common admission and grant operations; delegates path calculation to the planner |
| `CORE/Development/DevelopmentNavigationActions.cs` | Runtime read-model/DB conversion, planner invocation and navigation publication; same service instance delegates to this concrete helper |
| `BUFFER/Core/DevelopmentTransactions.cs` | Internal narrow transaction interface/implementation and internal `IDevelopmentBufferProvider` accessor implemented by the existing Buffer Group; no pathfinding or DB lookups |
| `BUFFER/Current/DevelopmentProgressPreparation.cs` | Runtime immutable expected graph/selection and prepared intent; may use Buffer DTOs because it stays in Systems |
| `VIEW/Dev/IDevelopmentTools.cs` | Public tool-host facade contract |
| `VIEW/Dev/DevelopmentTools.cs` | View readiness, Core delegation, existing map redraw and shared fault reporting; no gameplay calculations |

Do not register `DevelopmentNavigationActions` or the stateless planner as Framework services. Do not create a factory, scope, plugin registry or coordinator for each command.

The two internal provider interfaces are capabilities of existing Group objects, not extra services or forwarding objects. Do not register/export them separately or generalize them into a capability framework. They keep optional tooling out of `IInGameCoreSystemGroupService` and `IBufferQuery`, which remain unchanged. The only new public host surface is ViewGroup's facade.

### 3.2 Existing files allowed for narrow integration only

| File | Exact allowed change |
|---|---|
| `BUFFER/Core/BufferSystemGroup.cs` | Guarded implementation of internal `IDevelopmentBufferProvider.DevelopmentWriteSink` and construction of `DevelopmentTransactions` alongside existing aggregates; no change to `IBufferQuery` |
| `BUFFER/Current/CurrentProgressBuffer.cs` | Guarded store-only `PrepareDevelopmentSelection` method; reuse `PreparedCurrentSelection` and existing install path; normal methods unchanged |
| `SYS/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs` | Append guarded `bool includeDevelopmentTools = false` to `Create`; construct/register the optional service only when true, using existing `characters`, `stageService`, and `deckEditAvailability` |
| `CORE/InGameCoreSystemGroup.cs` | Implement internal `IDevelopmentStateProvider`; append guarded optional constructor parameter `IDevelopmentStateCommands developmentState = null`; no new member on the ordinary public Core Group interface |
| `SYS/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` | Append guarded `bool includeDevelopmentTools = false` to `CreateComposition`; compose facade only when true, with existing map readiness/redraw methods and shared `faults` |
| `VIEW/InGameViewSystemGroup.cs` | Guarded `DevelopmentTools` accessor; append optional final constructor parameter `IDevelopmentTools developmentTools = null` and store that interface reference |
| `VIEW/StageMap/StageMapViewSystem.cs` | Guarded read-only `CanUseDevelopmentTools` accessor forwarding to the attached scope; reuse existing redraw method |
| `VIEW/StageMap/StageMapViewScope.cs` | Guarded read-only readiness accessor over existing fields only; no development service reference or new lifecycle state |
| `SYS/Factory/Runtime/InGame/InGameSystemFactory.cs` | Append guarded `bool includeDevelopmentTools = false` to `CreateComposition` and private `Compose`; forward the same value to Core/View factories, leaving existing helper calls at false |
| `SYS/Factory/Runtime/RunTimeSystemFactory.cs` | In the full runtime's existing InGame composition call, pass `includeDevelopmentTools: true` only under the development compile condition; no new runtime-factory parameter or runtime toggle |

No new lookup key is required. Factories pass the same locally registered instance into the owning Group; omitted optional composition uses null, never a failed `GetService` call. Lower-level factory calls default to omission, so ordinary isolated tests do not unexpectedly require development dependencies. The paired InGame factory includes or omits both services together. Explicit inclusion requires the existing availability authority and internal providers; absence is a configuration error, not silent disabling. All original call sites remain source-compatible. Guard both new parameters and references in release compilation.

`RunTimeSystemFactory.cs` is **Systems composition**, despite its class being named `FrameworkRuntimeFactory`; this limited call-site edit is allowed. `Assets/ProjectL/Script/Framework/**` remains prohibited. `BufferSystemFactory.cs` needs no change: aggregate construction remains in BufferSystemGroup. The development Buffer capability is inert without the Core service. No Bootstrap edit, new composition root or runtime enable/disable state is needed.

### 3.3 Narrow existing-test compatibility allowance

Phase C adds a member only to the existing public View Group interface. Permit a guarded `DevelopmentTools => null` on an intentionally minimal test double, or a pass-through accessor when wrapping a real Group, in these existing files under `TEST/Framework/`: `CombatEntryIntegrationTests.cs`, `CombatProgressJobTests.cs`, `CombatProgressTestDriver.cs`, `LobbyDepartureIntegrationTests.cs`, `RecoveryRequestLifetimeIntegrationTests.cs`, `RecoverySceneConnectionTests.cs`, `RecoveryUITests.cs`, and `TutorialPlanTestKit.cs`.

This allowance changes interface conformance only: no assertion removal, altered gameplay expectation, new fake success, or production workaround. Compile the entire affected test assembly. Ordinary Buffer/Core Group interfaces remain unchanged, so their unrelated test doubles need no new members. A test explicitly opting into development composition must supply provider-preserving parents; for example, a Buffer wrapper must forward the internal capability in that development-specific fixture. Do not make ordinary factory tests opt in merely to satisfy the feature. If another current implementer is discovered, record it and obtain a conformance-only allowlist amendment before editing it.

## 4. Contract ledger

Create these types only in the task that first needs them. The following is the final API after C; A need not ship unused navigation members or throwing stubs.

### 4.1 Caller and result contracts

- `DevelopmentContext`: sealed immutable class with `RunId RunId`, `int ChapterKey`, `bool HasSelection`, `StageNodeId NodeId`, `StageKey StageKey`, `DevelopmentFailureReason AvailabilityReason`. `IsAvailable` derives from `AvailabilityReason == None`. Absent selection uses empty IDs. Unbound/stopped reads return an unavailable snapshot; mutation never treats it as valid.
- `DevelopmentFailureReason`: `None`, `Unavailable`, `Busy`, `NoActiveRun`, `RunEnded`, `BattleRetained`, `StaleContext`, `InvalidAmount`, `Overflow`, `InvalidTarget`, `AlreadyPassed`, `UnsupportedNode`, `NoSupportedPath`, `RosterRejected`.
- `DevelopmentGoldResult`: `bool Applied`, `DevelopmentFailureReason Reason`, `int? Before`, `int? After`. Refused calls have no claimed change; applied calls contain exact before/after.
- `DevelopmentUnitResult`: `bool Applied`, `DevelopmentFailureReason Reason`, `RosterAcquireResult Acquisition`. Early admission refusal has no acquisition; a called acquisition preserves its original result, including failure detail.
- `DevelopmentNavigationResult`: `bool Applied`, `bool Changed`, `DevelopmentFailureReason Reason`, `ChapterSelectionFailureReason? NavigationFailureReason`, `StageNodeId TargetNodeId`, `StageKey TargetStageKey`, `IReadOnlyList<StageNodeId> BypassedNodes`, `DevelopmentContext Current`. Copy collections. Preserve a normal-rule reason when it rejects the command; a search with no supported path returns `NoSupportedPath` without fabricating a normal-rule reason. Adjacent jump preparation is applied with `Changed == false`; it does not write stores.

Both `IDevelopmentTools` and `IDevelopmentStateCommands` use the signatures shown in spec §5. `ReadContext()` captures live observations without mutating gameplay. Failures are explicit; do not catch all exceptions and convert configuration faults into refusals.

`IDevelopmentTools` is public; `IDevelopmentStateCommands`, `IDevelopmentStateProvider`, `IDevelopmentBufferProvider` and `IDevelopmentBufferWriteSink` are internal to Systems. The Core provider contains only `IDevelopmentStateCommands DevelopmentState { get; }`; the Buffer provider contains only `IDevelopmentBufferWriteSink DevelopmentWriteSink { get; }`. Existing Group objects implement these only under the development condition. Facade PostBind checks Core provider/non-null commands through the existing InGame → CoreGroup path. Core PostBind checks Buffer provider/non-null transaction through existing `BufferSystemKey.Buffer`. No new root service, mandatory lookup for an omitted leaf, downcast to a concrete Group, or resolver retained by a leaf helper is permitted.

Both new runtime components implement only `ISystemPostBindService` and `ISystemShutdown` as lifecycle interfaces, plus their command/export contracts. They need no Initialize or Tick. Bind once; release dependencies and mark unavailable on shutdown; never reuse a stopped instance. CoreGroup's lifecycle forwarding stays unchanged. The factory passes the shared `RuntimeFaultBoundary` to the facade; expected refusals are returned, unexpected command/readiness/redraw errors are reported and rethrown. Do not connect another StopInput callback. A post-publication redraw fault must retain the committed state and stop further commands, not masquerade as an unapplied refusal.

### 4.2 Planner and publication contracts

- `DevelopmentNavigationInput`: expected `DevelopmentContext`, `bool IsChapterClear`, and copied `IReadOnlyList<DevelopmentNode>`.
- `DevelopmentNode`: existing pure `ChapterNodeProgress Progress` plus `bool CanBypass`. The runtime adapter sets `CanBypass` only for `StageType.Combat`, `StageMapRole.Regular`, and a node with successors. The planner also rejects already-cleared nodes and invalid edges. No DB enum enters pure Contracts.
- `DevelopmentNavigationPlan`: `Reason`, nullable existing `NavigationFailureReason`, resolved target node/stage IDs, copied bypass node IDs and optional final selected node ID. An empty bypass list means no new selection.
- `DevelopmentNavigationPlanner.PlanSkip(DevelopmentNavigationInput input, StageNodeId nodeId)` and `.PlanJump(DevelopmentNavigationInput input, StageNodeId targetNodeId)` return `DevelopmentNavigationPlan`.
- `DevelopmentProgressPreparation` retains the expected Run/context, exact `StageSelectionData`, copied `StageProgressData` graph, expected map-clear fact, planned bypass IDs, and resolved final selection name/key. Do not put this Systems DTO in pure Contracts.
- `IDevelopmentBufferWriteSink.TryGrantGold(DevelopmentContext expected, int expectedBalance, int finalBalance)` returns `DevelopmentFailureReason`.
- `IDevelopmentBufferWriteSink.TryPublishProgress(DevelopmentProgressPreparation preparation)` returns `DevelopmentFailureReason`.
- `ICurrentProgressBufferStore.PrepareDevelopmentSelection(int expectedChapterKey, bool expectedHasSelection, StageSelectionData expectedStage, StageNodeId targetNodeId, StageKey targetStageKey, string targetStageName)` returns existing `PreparedCurrentSelection`. Reuse `StageSelectionData.RequireEntry` and the store's `MatchesExpected`; construct no shop/recovery payload. Do not pass the whole route preparation into this method. The transaction owns full graph/Run/map comparisons.

Buffer validates expected source identity and structural consistency; it does not decide which route to skip. Capture/recompare every graph field used by policy, including node kind/role and links. A read-only wrapper around mutable nodes is not an immutable captured graph.

## 5. Phase overview and authorization

| Phase | Deliverable | Owner | Completion does not mean |
|---|---|---|---|
| A | Core grant APIs, safe admission, optional composition | Core/Buffer, explicit authorization required | Navigation or View host is delivered |
| B | Pure bypass planner and atomic progress publication | Core/Buffer, explicit authorization required | Active-combat skip, cross-chapter teleport, or automatic scene entry exists |
| C | Public View facade, map readiness/redraw, end-to-end and release checks | View plus specifically listed composition seams | A runtime panel/hotkey exists or manual play acceptance was performed when it was not |

Stop and report after each phase. Do not treat “continue A” as permission for B/C. Before the first phase, record `git status --short`, current branch/HEAD, ownership authorization and applicable concurrent edits. A changed file in the allowlist is not permission to overwrite another worker's changes.

## 6. Phase A — grant APIs without altering ordinary grants

### A1. Core admission and additive gold transaction

**Files:** create Context, Results, Core command contract/service and DevelopmentTransactions from §3.1; modify only the Buffer group and Core factory/group entries in §3.2. Test: `TEST/Framework/DevelopmentGrantServiceTests.cs`, `TEST/Framework/DevelopmentTransactionsTests.cs`.

**Consumes:** `IDeckEditAvailabilityRead.ReadDeckEditAvailability()`, `DeckEditAvailability.CanEdit`, existing Run/current/combat read sinks and `ICurrencyBufferStore`.

**Produces:** `ReadContext`, `GrantGold`, optional internal Core service/provider and `IDevelopmentBufferProvider.DevelopmentWriteSink`. New methods in ordinary CurrencyBuffer and members on `IBufferQuery`/the public Core Group interface are not needed.

- [x] Record baseline focused results for `CurrencyBufferTests`, `RunDeckRuntimeCompositionTests`, and `FrameworkRuntimeCompositionTests` before touching these integration seams.
- [x] Add failing cases: `GrantGold_PreservesRunSpent` (70 + 50 = 120, spent remains 30); positive boundary at `int.MaxValue`; zero/negative rejection; overflow rejection; wrong Run and changed selection rejection; no active Run; ended Run; retained completed battle; loading/pending input/open interaction; unavailable authority. Assert all gameplay facts unchanged on refusal.
- [x] Implement Core admission in this order: bound/not stopped → nonempty Run → matching context → Run in progress → no retained battle → current deck-edit availability. Bind through existing PostBind and stop through Shutdown only. A missing required authority on explicit inclusion, or a configured authority returning null, is a composition fault. Empty-roster recovery is outside version 1 because the existing Stage window requires a roster; do not alter that predicate.
- [x] Compute positive checked addition in Core. `TryGrantGold` rechecks expected Run/current/balance and in-progress/no-battle facts, then uses `PrepareAmount`/`InstallAmount` with no callback between them. Revalidate the authority after any external read and before mutation.
- [x] Compose the transaction in the existing Buffer group constructor and expose its internal provider. Add Core factory's guarded default-false inclusion option. When explicitly true, register the internal command service before Build and pass that same instance to the Group's optional constructor argument. Obtain transaction access through the checked provider during PostBind. Ordinary factory calls keep their old composition; explicit inclusion with missing provider/authority fails rather than silently disabling the service.
- [x] Test factory omission separately from inclusion. Existing `ChapterGraphPreparationTests` with its reset-spy Buffer wrapper must still run without opting in. A new development-specific wrapper fixture explicitly opts in and forwards the provider; a non-forwarding wrapper must produce a clear configuration fault. Do not modify all ordinary Buffer/Core test doubles.
- [x] Run new tests plus the baseline fixtures. Record exact result counts and any pre-existing failures by identity/message.

**Do not:** call `SetAmount`; add a currency mode enum; invoke combat/shop settlement; widen `StageDeckEditWindow`; make Framework aware of grants; build a command queue.

**Locality:** understanding is limited to idle admission, Run identity, currency prepare/install and composition. Combat damage, shop pricing and reward internals are not part of this task. Shared composition tests remain necessary because registration changes.

### A2. Delegate unit acquisition and preserve deck selection

**Files:** extend Core service/contract/results from A1; add cases to `DevelopmentGrantServiceTests.cs`. No edits to `CharacterRosterService`, `CharacterRosterRules`, `CharacterBuffer`, `RunDeckService` or their ordinary contracts.

**Consumes:** A1 admission and `ICharacterRosterService.Acquire(EntityKey key, int initialLevel = 1)`.

**Produces:** `GrantUnit(DevelopmentContext expected, EntityKey key)`.

- [x] Add failing cases for a valid unowned player unit; duplicate key; enemy-only/invalid key; existing category limit; stale Run; retained battle and unavailable window.
- [x] Assert the success contains the acquired ID, roster revision advances according to the existing service, and selected RunDeck IDs/revision, current selection and battle membership are unchanged. Preserve native acquisition failure reasons.
- [x] Invoke `Acquire(key)` once after admission. Do not expose initial level or count in version 1 and do not precompute ownership rules in the facade.
- [x] Run `DevelopmentGrantServiceTests`, `CharacterRosterServiceTests`, `RunRosterLimitTransactionTests`, and the necessary deck composition fixture.
- [x] Inspect the diff to confirm ordinary roster/deck code is unchanged; record A's actual file list, tests and remaining C integration work.

**Phase A stop:** report grant behavior and release-compilation status achieved so far. No navigation implementation or panel creation begins automatically.

## 7. Phase B — progress-only bypass; ordinary entry stays intact

### B1. Pure path planning with explicit supported cases

**Files:** create `CONTRACTS/Development/DevelopmentNavigationInput.cs` and `APP/Development/DevelopmentNavigationPlanner.cs`; extend development results if needed; test `TEST/DevelopmentNavigationPlannerTests.cs`. Ordinary `ChapterNavigationRules.cs` is read-only.

**Consumes:** immutable node facts; source selection; existing pure navigation contract for validation.

**Produces:** `PlanSkip` and `PlanJump` from §4.2.

- [x] Add a graph fixture with start 1, Combat 2, Combat 3, target 4. `PlanSkip(2)` bypasses only 2; `PlanJump(4)` bypasses `[2,3]` and selects 3; target 4 remains uncleared.
- [x] Add tests for already adjacent target (no mutation, including synthetic start), branching/merging with deterministic lexicographic path, invalid depth edges, missing nodes, uncleared source, completed chapter, already-cleared target and absent path.
- [x] Add tests where every path contains Event/Shop/boss intermediates: reject with no plan writes. Allow a boss/Event/Shop as the final ordinary-entry target when intermediates are supported. A boss or no-successor node cannot be `PlanSkip`'s target.
- [x] Implement a bounded search of actual forward edges in ascending node-ID order. **Every candidate hop and final target must call `ChapterNavigationRules.Evaluate`** on command-local simulated progress; only preceding planned bypasses are marked passed in that copy. Copying its adjacency/depth/source-clear/target-clear policy is prohibited. Add only bypass-kind filtering, search and deterministic route choice. Track visited nodes for malformed graphs; do not mutate or regenerate the real graph.
- [x] Add parity tests asserting direct-skip normal-rule refusals preserve the existing `ChapterSelectionFailureReason`; test that a chosen jump's simulated final hop is accepted by that same rule. A failed search returns `NoSupportedPath`, not a fabricated victory or relaxed admission.
- [x] Verify defensive copies: mutating caller-owned lists after input construction cannot change the plan. Run new planner tests plus `ChapterNavigationRulesTests`.

**Do not:** introduce `Skipped` into ordinary outcome enums; synthesize completion receipts; put Buffer/DB/Unity references in pure Core; use reflection to call an unrelated internal entry implementation.

### B2. Atomic bypass publication and runtime adapter

**Files:** create `CORE/Development/DevelopmentNavigationActions.cs` and `BUFFER/Current/DevelopmentProgressPreparation.cs`; extend development command/service/transaction files; add only the guarded current-store method in `CurrentProgressBuffer.cs`; modify already-listed composition wiring as necessary. Tests: `TEST/Framework/DevelopmentNavigationServiceTests.cs`, existing new `DevelopmentTransactionsTests.cs`.

**Consumes:** B1 planner, current graph/selection/read facts, existing `StageServiceManager.GetStage/ValidateEntry` for definition lookup/validation, and store prepare/install methods. Inject the existing concrete stage service into this internal runtime helper; do not widen its public interface for View.

**Produces:** Core `SkipNextNode` and `PrepareJumpToNode`, plus atomic `TryPublishProgress`.

- [x] Add integration tests proving selection and all bypass flags change together; target/unrelated branches remain unchanged; no currency, roster, deck, Fame, inventory, completion, kills or Run summary changes occur.
- [x] Add rejection tests with changed Run, selection, target binding, graph kind/role/link/clear facts, map completion and retained battle. Cause a later preparation to reject and assert earlier clears were not installed.
- [x] Capture current facts, produce the pure input and plan, then resolve definitions for the chosen bypass nodes and target using existing `StageServiceManager.GetStage`, `StageRootData.Validate`, and `StageServiceManager.ValidateEntry`. Explicitly compare root StageKey/MapKey/StageType/MapRole to captured node binding; bypassed nodes must have regular Combat details. `ValidateEntry` alone does not check those identities. Do not invoke an entry handler, validate unrelated branches' full content, or promise preflight covers every later deck/battle admission.
- [x] Add separate fixtures for an initially inconsistent DB/graph binding (configuration exception before mutation) and graph mutation during an external read (`StaleContext` with no mutation). Do not hide the former with a default result or repair the graph automatically.
- [x] Implement the selection-only `PrepareDevelopmentSelection` signature from §4.2, reusing `StageSelectionData.RequireEntry` and private `MatchesExpected`. Never pass it the full graph/path preparation or a fake `BattleEntrySelection`. Keep normal `PrepareSelection`, `InstallSelection` and their callers unchanged.
- [x] Transaction checks expected Run/current/graph/map facts, prepares every `PreparedStageClear` and final `PreparedCurrentSelection`, then performs assignment-only installation. Empty-path success calls no store setter. No awaits, external callbacks or rollback-by-reset are allowed.
- [x] Compare captured facts after DB/external reads and immediately before publication. Return a precise expected refusal; unexpected storage/composition corruption propagates to the fault boundary.
- [x] Run new service/transaction/planner fixtures plus `ChapterNavigationRulesTests`, `StageFlowTests`, and affected progression transaction tests found from their live callers. Do not claim visual acceptance yet.

**Do not:** call public `SelectStage`/`SetClear` in a loop; put planner rules in `BufferSystemGroup`; edit `BattleLifetime`, `BattleCompletionService`, `CombatPlan`, `StageFlow`, or normal chapter continuation to accommodate a bypass.

**Locality:** B needs navigation contracts and progress stores, not combat settlement internals. Normal entry regression is required because its source facts are deliberately changed, even though its implementation is unchanged.

**Phase B stop:** report the supported stage-type/path limits prominently. Forward-path preparation is not full arbitrary teleport and not active-combat skip. C must still verify presentation restoration and actual normal entry.

## 8. Phase C — optional facade, map integration and release absence

### C1. Public facade and read-only presentation admission

**Files:** create View facade interface/implementation; modify View factory/group, paired InGame factory, the single full-runtime composition call, and StageMapViewSystem/Scope entries in §3.2. Tests: `TEST/Framework/DevelopmentToolsTests.cs`, `TEST/Framework/StageMapViewSystemTests.cs` for the guarded seam, and only the conformance edits enumerated in §3.3.

**Consumes:** A/B Core service, existing `RequestMapRedraw`, and current scope facts. No gameplay Buffer writer is injected into the facade.

**Produces:** final `IDevelopmentTools` API and `ViewGroup.DevelopmentTools`.

- [x] Add facade tests: no attached scope, not ready, fault, marker movement, shop/recovery/deck popup open, stopped runtime and Core refusal produce no state mutation or navigation redraw.
- [x] Implement guarded `CanUseDevelopmentTools` in scope from `CanWork`, `_readyForTick`, `_isShown`, `_movement.IsPresenting`, existing popup `IsOpen` values, and `!_requests.TryGetNextCommand(0, out _)`. This last condition covers handled but View-unconsumed input. Forward through the map system; do not modify Flow or deck availability. A pending redraw closes readiness until the existing display work completes. Do not add mutable readiness flags.
- [x] Inject `Func<bool>` for existing-object readiness, `Action` for `RequestMapRedraw`, and the existing shared `faults` instance into `DevelopmentTools`. Delegates contain no grant/path rules. During PostBind, resolve only the existing InGame parent and explicitly check its CoreGroup's internal provider/non-null commands; do not expose Core commands through the public Core Group interface. Core never retrieves the facade.
- [x] Delegate grants once; after a changed navigation result, invoke existing redraw once. An adjacent no-op produces no unnecessary redraw. Do not perform ordinary target entry automatically.
- [x] Register `IDevelopmentTools` once in the View builder and its module only for the actual bind/shutdown lifecycle. Pass that same instance into the View Group's optional final constructor argument. Do not re-export the service at the runtime root or add optional lookup to Framework.
- [x] Add the guarded default-false construction options from §3.2; the paired factory forwards one value to both leaf factories. The full Systems runtime call opts in under `UNITY_EDITOR || DEVELOPMENT_BUILD`. No bootstrap, Framework or runtime toggle is added. Apply §3.3 conformance-only test edits and compile the affected test assembly.
- [x] Inject failures before publication and during post-publication redraw. Verify the facade reports through the same fault boundary once, rethrows, and rejects retained calls afterward. Preserve committed facts if redraw fails; do not return an ordinary `Applied == false` result, retry, or connect an additional StopInput callback.
- [x] In tests, verify constructor/registration → child Group → parent export → consumer lookup, and shutdown makes retained facade calls unavailable. Verify ordinary map object has no reference to the facade.

### C2. Navigation presentation and normal entry integration

**Files:** tests `TEST/Framework/DevelopmentNavigationIntegrationTests.cs` and `TEST/Framework/DevelopmentToolsTests.cs`; fixes only inside C1/new development code unless the stop protocol authorizes an amendment.

- [x] Exercise a real map scope: skip 2, tick existing View, verify cleared indicator and marker at 2, then detach/rebind and verify the same state with no fabricated completion/recovery records.
- [x] Prepare jump to 4 through 2/3, apply existing redraw, then submit `StageViewCommandData.SelectStageNode(chapter, 4)` through the ordinary StageView writer. Observe the existing request number/result/entry observer; verify normal entry prepares the target exactly once.
- [x] Repeat ordinary target entry with a boss, Shop and Event/recovery target. Verify exactly one appropriate battle/visit context and the existing result identity/restore behavior. Also test an already-adjacent target from the synthetic start: no bypass write, then successful ordinary entry.
- [x] Verify changed navigation success immediately closes `ReadContext().IsAvailable` while redraw is pending, a real display tick reopens it, and repeated mutation before that tick is refused. An adjacent no-op keeps availability unchanged because it requests no redraw. Observing readiness, not assuming that one arbitrary frame elapsed, is the host's redraw-completion contract.
- [x] Make target entry fail due to an invalid deck or content. Assert bypassed flags/current predecessor remain committed, no battle/scene transition succeeds, and the original failure reason is retained. The failure message must distinguish progress preparation from entry.
- [x] Test Event/Shop intermediates are rejected without redraw/rebind exceptions; do not change `RestoreNoncombat` or fabricate recovery/shop payloads to make this pass.
- [x] Test both an unregistered normal request and a handled rejection still awaiting `StageMapEntryObserver` consumption. Both must close development admission. Let the existing observer remove the request; only then may readiness recover. Test marker movement independently from Runner idle and a completed but retained battle separately. Do not clear request/result buffers from development code.
- [x] Run the new fixtures plus `StageMapViewSystemTests`, `StageMapEntryObserverTests`, `StageFlowTests`, `StageMapCombatEntryBootSmokeTests`, `RecoveryStageMapViewTests`, and ordinary roster/currency/deck checks affected by the integrated composition.

**Do not:** copy ordinary command processing into the facade; clear an existing request/result buffer; reset Flow; silently enter through the boot content override; suppress restoration exceptions.

### C3. Development/release verification and handoff

**Files:** new `TEST/Framework/DevelopmentCompositionTests.cs`; this plan's execution ledger; existing Core boundary handoff §30. Production fixes remain bounded by the phase allowlist.

- [x] Verify the full composition with development enabled contains one Core command service and one View facade; ordinary services retain their existing registration/lookup locations.
- [x] Verify neither internal provider is registered as a service, the ordinary Buffer/public Core Group interfaces are unchanged, and only the facade plus focused development tests consume Core development commands. Existing ordinary tests remain opted out at lower-factory defaults; explicitly included tooling rejects missing providers.
- [x] Compile Editor, Development Player, and non-Development Player configurations. Verify release assembly/service surfaces contain no development command registration or reachable facade. Do not use an Editor-only test as proof of player exclusion.
- [ ] Run an ordinary startup/StageSelect → stage → return smoke path with no development calls, and compare its observable route with the pre-change baseline. Run the same normal path with paired `includeDevelopmentTools: false` in a test composition. Both command services are absent in that case; the development-build Buffer provider/aggregate may remain inert. Do not claim that all development objects are absent except in release compilation. No original gameplay constructor or rule may require the feature.
- [x] Check final source dependencies and diff using §10. Any Framework/asmdef edit, normal rule change, or new hidden entry path fails review regardless of passing feature tests.
- [ ] Record manual play checks separately: granted unit is selectable through the existing deck editor; displayed gold refreshes through existing reads; skipped markers survive rebind; target enters through ordinary interaction. If not performed, mark manual acceptance unverified.
- [x] Update §11 with exact evidence and the local handoff. Stop; no commit/push and no panel/hotkey implementation.

**Locality:** C changes presentation admission and optional composition, so map scope, registration and normal entry are relevant integration boundaries. This does not authorize unrelated UI, camera, Framework or Flow cleanup.

## 9. Review focus and acceptance gates

| Failure mode likely to be missed | Required evidence | Owning task |
|---|---|---|
| Correct gold amount but reset historical spending | 70/30 + 50 yields 120/30; later spending increments normally | A1 |
| Grant changes selected deck or live battle identity | Acquired ID exists; deck/membership unchanged; retained battle rejected | A2 |
| Failure after first bypass leaves half a path | Later prepare rejection leaves selection/all flags untouched | B2 |
| Map restore requires state the bypass never created | Event bypass rejected; Combat skip survives redraw/rebind | C2 |
| Feature works only by creating a hidden normal-entry route | Existing input number/result observer used; normal smoke route unchanged; release contains no tooling | C2/C3 |

For every phase, review SSOT and all SOLID principles against actual dependencies. Check FW-001/002/003 registration/lifecycle, FW-004 internal access, and FW-016 runtime lifetime. Check project hierarchy separately from duplicate-service registration: a unique root export of a child service can still violate the architecture.

## 10. Verification procedure

### 10.1 Static checks

Run from the repository root; inspect results, do not use a pattern match alone as a verdict.

```powershell
git status --short
git diff --check
git diff --name-only
git diff -- Assets/ProjectL/Script/Framework
rg -n 'StageGold|IsDeveloperMode|ForceVictory|ForceEnter' Assets/ProjectL/Script/Systems
rg -n 'UnityEngine|BufferSystems|ViewSystem|Framework' Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Development Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Development
```

Expected: no new prohibited paths/dependencies, no whitespace errors, no changed Framework or asmdef files. Existing matches outside the change are evidence to inspect, not permission for unrelated cleanup. Inspect new untracked files as well as `git diff`.

### 10.2 Unity execution

Use Unity **6000.0.73f1** and the existing approved execution route with AppData access. The known local `CurlRequestCache.db` startup failure is a sandbox-access issue; do not upgrade Unity, delete caches, or modify permissions as a feature fix.

Prefer the running Editor's EditMode Test Runner for an already-open project. If batch execution is used, resolve the installed executable first, use the approved AppData-capable launch, avoid launching against a locked checkout, and save complete XML/logs in the approved scratch output. No fixed executable path was verified during this documentation task.

Run the named fixture filters from each task and record the full filter, Unity version, assembly, counts, failures and XML/log locations. Pure planner tests belong in `ProjectL.Tests.Core`; runtime/facade tests belong in `ProjectL.Tests.Framework`. New tests stay in existing assemblies. A crash or absent XML is not a test pass. Compare pre-existing failures by exact identity/message; focused green is not repository-wide green.

Developer/release player compilation and manual View acceptance are separate gates. If either cannot run, report it as unverified and do not mark C complete. Apply existing required repository regressions in addition to the focused lists when their documented impact requires them; do not broaden repeatedly without a new concern.

## 11. Execution ledger

| Phase | Authorization | Changed files | Verification | Status |
|---|---|---|---|---|
| A | Explicitly granted, 2026-10-23 | Five new production files, three approved integration seams, two test files and metadata; see §13 | 63/63 new tests; 161/165 combined, four exact baseline failures; Development/release managed compilation | Complete; stopped for phase review |
| B | Explicitly granted after Phase A review, 2026-10-23 | Four new production files, development extensions, three approved composition/store seams and listed tests; see §14 | 103/103 new cases; 372/409 combined, 37 exact baseline failures; Development/release managed compilation | Complete; stopped for phase review |
| C | Explicitly granted after Phase B review, 2026-10-23 | Two facade files, six approved production seams, three test files, eight conformance edits and metadata; see §15 | 57/57 new cases; combined 318/350 with 32 exact baseline failures; actual Development/Release Player builds and packaged-assembly audit | Implementation delivered; final scene/manual acceptance pending |

At each stop append: task IDs completed; unresolved failures; actual dependency/route changes; deviations proposed and approved; Core/View handoff ownership; manual acceptance status. Keep old evidence intact and date new records. No implementation claim may be inferred from this document's existence.

## 12. Second review record — 2026-10-23

Review against current source and the supplied locality guideline found implementation latitude that required correction. The design's §12 records rationale; the following gates make the corrections executable.

| Finding / risk | Plan correction | Required task evidence |
|---|---|---|
| Optional reuse language permits cloned navigation rules | Existing Evaluate call is mandatory for every hop and final target | B1 parity cases and source-call inspection |
| Public Core development entry bypasses presentation admission | Internal provider/command on existing Core Group; only facade is external | C1/C3 visibility, consumer and registration checks |
| Broad interface additions spread into unrelated tests | Keep IBufferQuery/public CoreGroup unchanged; enumerate only View conformance edits | A1 old default composition; C1 test assembly compile |
| Automatic lower-factory inclusion creates required dev dependencies | Default-false leaf/paired options; full Systems composition opts in | A1/C1/C3 included, omitted and missing-provider cases |
| Flow-idle does not imply View has consumed every request | Require existing stage-input Buffer empty at facade readiness | C2 handled rejection before/after observer consumption |
| Unexpected exceptions lack a defined fault owner | Same shared boundary injected into facade; report and rethrow | C1 failure before publication and after commit/redraw |
| Selection store receives unrelated route/graph data | Narrow selection-only signature reusing existing checks | B2 method dependencies and atomicity tests |
| Definition preflight omits graph binding comparison | Verify chosen node key/map/type/role against current definitions | B2 inconsistent-input versus stale-source fixtures |
| Lifecycle behavior is vague | PostBind/Shutdown only; no CoreGroup Initialize/Tick extension | A1/C1 retained-reference tests and diff audit |

Verification level at the second design review: current-source inspection and document review only. Later Phase A implementation and verification are recorded separately in §13; B/C and manual acceptance remain unverified.

## 13. Phase A execution record — 2026-10-23

**Authorization and boundary:** The user explicitly authorized Phase A, including its Core/Buffer ownership exception. A1 and A2 are complete. B and C remain unstarted and require separate authorization. No staging, commit, push or external handoff delivery occurred.

**Source and delivery:** Implementation and Unity verification used an isolated managed worktree at `C:/CodexHome/worktrees/development-tools-phase-a/Project_L`, based on `8644c702`. The verified Phase A files were then copied into the original checkout after checking that its three integration files were unchanged; file hashes were compared. Concurrent documentation edits were preserved. Unity-generated FMOD catalog and project-setting changes in the isolated worktree were restored and are not part of the delivery.

### 13.1 Implemented behavior and actual files

- `GrantGold` admits the current Run/selection and idle authority, checks a positive addition for overflow, rechecks authority and state, and submits the expected balance to the development Buffer transaction. That transaction uses existing `PrepareAmount/InstallAmount`, preserving `RunSpent`. No reward, Fame, kill count or summary operation is involved.
- `GrantUnit` uses the same admission and calls existing `Characters.Acquire(key)` exactly once at default level 1. Its native result and failure detail are retained. The selected deck, existing owned records, persistent states and battle membership are preserved.
- Binding and shutdown follow the existing Core Group lifecycle. Explicit inclusion requires a non-null authority and a provider-preserving Buffer Group. Ordinary factory calls omit the feature by default. No public ordinary Group interface or `IBufferQuery` member changed.
- The approved synchronous, non-reentrant state-transaction contract in design §4 remains applicable to delegated acquisition. The production unit query is a direct dictionary lookup. No callback interception or new ordinary roster API was introduced.

New production files, using §3 aliases:

1. `CONTRACTS/Development/DevelopmentContext.cs`
2. `CONTRACTS/Development/DevelopmentResults.cs`
3. `CORE/Development/IDevelopmentStateCommands.cs`
4. `CORE/Development/DevelopmentStateService.cs`
5. `BUFFER/Core/DevelopmentTransactions.cs`

Existing production files changed only at the approved composition seams:

1. `BUFFER/Core/BufferSystemGroup.cs`: constructs and exposes the guarded internal transaction capability.
2. `SYS/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs`: default-false inclusion, child registration and instance forwarding.
3. `CORE/InGameCoreSystemGroup.cs`: guarded provider and optional final constructor argument; lifecycle forwarding unchanged.

Tests: new `TEST/Framework/DevelopmentGrantServiceTests.cs` and `DevelopmentTransactionsTests.cs`. All seven new C# files and both new folders have `.meta` files. The design, this plan and boundary handoff §30 record delivery. No ordinary roster, deck, currency, navigation or combat implementation changed.

### 13.2 Verification evidence

The initial fresh worktree contained unhydrated LFS pointers. Local LFS objects were checked out before collecting the baseline; that first import attempt is not test evidence. Unity 6000.0.73f1 ran through the approved AppData-capable route, with no upgrade or cache deletion.

| Check | Result |
|---|---|
| Pre-implementation targeted baseline: Currency, RunDeck composition, Framework composition | 27 total, 25 passed, 2 failed |
| Initial pre-API red test | 1 failed as expected: the factory had no explicit development inclusion option |
| Expanded baseline without Phase A production changes | 102 total, 98 passed, 4 failed |
| Final Phase A grant tests | 48/48 passed |
| Final Phase A transaction tests | 15/15 passed |
| Final combined required regression set | 165 total, 161 passed, 4 failed; all four names and complete failure messages match the expanded baseline |
| Non-Editor `DEVELOPMENT_BUILD` managed compilation | Exit 0; 0 errors, 8 warnings in unchanged files |
| Non-Editor release managed compilation | Exit 0; 0 errors, the same 8 warnings |
| DLL metadata inspection | Development: 4 new Contracts types and 6 Systems types present. Release: all 10 absent; development factory/constructor parameters, provider interfaces and properties also absent |
| Independent static review | No actionable P1/P2 findings for the approved Phase A contract |

The combined Unity filter covers the two new fixtures plus `CurrencyBufferTests`, `RunDeckRuntimeCompositionTests`, `FrameworkRuntimeCompositionTests`, `ChapterGraphPreparationTests`, `CharacterRosterServiceTests`, and `RunRosterLimitTransactionTests`.

The unchanged failures are:

| Fixture and test | Exact failure identity / cause |
|---|---|
| `CharacterRosterServiceTests.LevelThreeSub_FromDirectAcquisitionAndShop_ReadsSameGrownStats` | `System.NullReferenceException : Object reference not set to an instance of an object` |
| `CharacterRosterServiceTests.UnknownShopSlot_DoesNotConsumeValidSlot_AndFreeUnownedPurchaseIsOneUse` | `System.InvalidOperationException : 상점 유닛은 획득 가능한 플레이어 유닛이어야 합니다.` |
| `FrameworkRuntimeCompositionTests.Create_ConnectsSharedViewCleanup_AndDisconnectsOnShutdown` | Expected null; cleanup AggregateException caused by `[UITextPresentation] Notice TextNoticeUI가 등록되어야 합니다.` |
| `FrameworkRuntimeCompositionTests.SharedRuntimeFault_BlocksStageLoadedCallbackBeforeQueuingJobs` | Shutdown AggregateException caused by the same missing Notice TextNoticeUI registration; complete message compared separately |

Local evidence is preserved in the original checkout's ignored `Temp/DevelopmentToolsPhaseA/Evidence/`: `red.xml`, `baseline-expanded.xml`, `phase-a-final.xml`, their logs and `baseline-final-comparison.json`. Conditional compilation logs, symbol and DLL metadata inspection are in `Temp/DevelopmentToolsPhaseA/Compile/Report.md` and `metadata.json`. This is managed compilation against Unity-generated project references, not an actual Player build. No repository-wide green, runtime UI acceptance or manual play acceptance is claimed.

### 13.3 Ownership, architecture and locality audit

| Check | Evidence |
|---|---|
| SSOT | Run/currency/roster/deck facts remain in existing Buffers; command-local context contains identity, not an alternative state store |
| SRP | Development service owns admission and grant orchestration; existing roster owns acquisition; Buffer transaction owns expected-state validation and installation |
| OCP | Optional guarded composition extends the existing groups without changing ordinary command behavior |
| LSP | Ordinary Core/Buffer interfaces and old factory defaults remain compatible; the reset-spy and omitted-provider fixtures pass |
| ISP | Two internal capability interfaces avoid widening ordinary shared Group queries; narrow service dependencies are retained after binding |
| DIP | Service depends on roster, availability and Buffer contracts; pure Contracts reference neither Unity nor Systems runtime services |
| FW registration and hierarchy | Core factory registers the command once inside its child builder and passes the same instance to Core Group; parent exports remain unchanged. Providers are not separately registered or exported at root |
| FW lifecycle | Existing PostBind/Shutdown forwarding is reused; no Initialize/Tick forwarding, resolver retention, static runtime handle or new composition root |
| Structural exclusions | Framework, ServiceKeys, asmdef/asmref, View/Flow, ordinary gameplay implementations, scenes, prefabs, SO assets and packages: zero delivered changes |

For changing gold validation, required understanding is limited to development admission, context identity, currency prepare/install and the three composition seams. Unit grants require the existing acquisition contract and its native failure meanings. Verification includes shared composition because that coupling is inherent in registration, and roster/deck regression because those authoritative facts are reused. The implementation adds no dependency on combat damage, shop prices, rewards or navigation internals.

**Phase A stop:** Internal grant APIs and optional lower Core composition are delivered. Navigation, View facade, full-runtime opt-in, stage-map presentation readiness/redraw, panels/hotkeys, actual Player builds and manual acceptance remain outside this completed phase. No deviation from the Phase A allowlist was required.

## 14. Phase B execution record — 2026-10-23

**Authorization:** The user separately authorized Phase B after reviewing Phase A. B1/B2 are complete; C is not authorized or started. Work reused the attached isolated worktree, based on `8644c702` plus the exact delivered Phase A files. Phase B source files were copied into the original checkout after conflict checks and hash verification. The one baseline byte difference in `CurrentProgressBuffer.cs` was verified to be line endings only before copying. Concurrent documentation was preserved. No staging, commit, push or external message occurred.

### 14.1 Delivered behavior and limits

- `SkipNextNode` accepts only an ordinary eligible next node, marks that single regular Combat node passed and selects it. Boss, Event, Shop and terminal nodes cannot be bypassed.
- `PrepareJumpToNode` searches real forward edges in ascending node-ID order. Every hop and the final target use existing `ChapterNavigationRules.Evaluate`. It selects the lexicographically first supported path, passes only intermediate nodes and selects the last predecessor. The target remains uncleared. A boss, Event or Shop may be the final ordinary-entry target.
- An already-adjacent jump succeeds with `Changed == false`, an empty bypass list and no store preparation or installation. It does not create a synthetic start selection.
- Definitions are resolved only for the chosen bypasses and target. Core calls existing `GetStage`, `StageRootData.Validate` and `ValidateEntry`, and checks the stage key, chapter key, type and role against the captured graph. Bypassed definitions require real `CombatStageDetail`. Selection names use the existing ordinary-entry `Stage {key}` format; `StageRootData` has no name field.
- Run/selection identity, the exact selection name, map completion and every captured graph field are compared after external reads and again at the transaction boundary. Changed source facts refuse without new writes. Initially invalid configuration and unchanged-source lookup errors remain exceptions.
- The transaction prepares every node clear and the final selection before installation. Later clear or selection preparation failure installs nothing. Currency, spending, roster/deck, Fame, inventory, battle facts, completion receipts, kills and Run summaries remain unchanged.

This is same-chapter forward-path preparation during the existing idle Stage window. It is not active-combat skipping, arbitrary teleport, automatic target entry or a runtime panel. View presentation admission, redraw/restoration and actual ordinary Flow entry remain C verification work.

### 14.2 Actual edit scope

New production files:

| File using §3 aliases | Responsibility |
|---|---|
| `CONTRACTS/Development/DevelopmentNavigationInput.cs` | Immutable node/input/plan contracts and copied collections |
| `APP/Development/DevelopmentNavigationPlanner.cs` | Supported-path search using ordinary navigation decisions |
| `CORE/Development/DevelopmentNavigationActions.cs` | Runtime capture, chosen-definition validation and publication orchestration |
| `BUFFER/Current/DevelopmentProgressPreparation.cs` | Immutable expected graph/selection and final installation intent |

Extended development files: `DevelopmentResults.cs`, `IDevelopmentStateCommands.cs`, `DevelopmentStateService.cs`, and `DevelopmentTransactions.cs`. Existing integration edits: `BufferSystemGroup.cs` passes current/map/stage stores, `InGameCoreSystemFactory.cs` passes the already-created stage manager, and `CurrentProgressBuffer.cs` adds only the guarded internal preparation method plus its interface declaration. Normal methods are unchanged. `InGameCoreSystemGroup.cs` has no Phase B delta.

Tests: new `TEST/DevelopmentNavigationPlannerTests.cs` and `TEST/Framework/DevelopmentNavigationServiceTests.cs`; extended `DevelopmentTransactionsTests.cs` while preserving its 15 Phase A cases. The two manual `DevelopmentStateService` constructions in the already-listed `DevelopmentGrantServiceTests.cs` were given the existing stage manager; their assertions are unchanged. This is the plan §1.1 listed-test-file compatibility exception, not a change to an ordinary production constructor. All six new C# files and the new Application folder have metadata. Delivery comprises 22 Phase B source/metadata files plus the three existing feature documents.

### 14.3 Verification and review

Unity 6000.0.73f1 ran with approved AppData access. Evidence was kept outside the running Unity project's temporary directory, under the original checkout's ignored `Temp/DevelopmentToolsPhaseB/`.

| Check | Result |
|---|---|
| Pre-B baseline, including delivered Phase A | 306 total, 269 passed, 37 failed |
| Initial pre-API probes | Two expected failures: missing planner and missing runtime navigation command |
| B1 planner plus ordinary rule | 41/41 passed |
| Final planner fixture | 35/35 passed |
| Final runtime navigation fixture | 43/43 passed |
| Final transaction fixture | 40/40 passed, comprising 15 A cases and 25 B cases |
| Existing grant fixture after dependency wiring | 48/48 passed |
| Final combined required set | 409 total, 372 passed, 37 failed; no added or changed failure |
| Added Phase B cases | 103/103 passed; A+B development cases total 166/166 |
| Non-Editor Development and release managed rebuilds | Exit 0, zero errors and eight warnings each; warning text matches Phase A |
| DLL metadata | All 17 development types present in Development and absent in release; development selection preparation absent on both the internal store interface and implementation in release |
| Release compatibility | Member signatures of seven normal types match Phase A exactly |
| Independent final review | One P2 reproduced and corrected; no remaining P1/P2 findings |

The existing regression fixtures are `ChapterNavigationRulesTests`, `StageFlowTests`, `ProgressBufferTests`, `RunProgressBufferTests`, `BattleEntryPublicationTests`, `ChapterGraphPreparationTests`, `ChapterContinuationPublicationTests`, `ChapterContinuationServiceTests`, `RecoveryEntryPublicationTests` and `ShopEntryPublicationTests`. Integration assertions also call the existing runtime `Core.ChapterProgression.PrepareStageEntry` to verify that the prepared target is accepted without entering it. The Framework test assembly uses this existing API rather than adding a Domain assembly reference.

All 37 failures belong to the existing `ChapterContinuationServiceTests` fixture. Their complete message is `System.InvalidOperationException : Nullable object must have a value.` Each exact test name and full message matches the pre-B baseline; the detailed comparison is `Evidence/baseline-final-comparison.json`. These baseline failures were not repaired in this phase, and the result is not repository-wide green.

The reviewed P2 was an exception handler that could suppress a DB error as `Busy` when availability closed despite unchanged captured progress. `DefinitionLookupException_RemainsAnExceptionWhenOnlyAvailabilityCloses` first failed with “expected exception, but none.” The repair restricts conversion to direct captured-progress comparisons around DB reads; unchanged-source exceptions propagate. That test and the changed-progress exception counterpart pass in the final run.

The first B2 compilation exposed an insertion-script omission of an existing declaration; the exact declaration was restored. A test-only Domain reference was replaced by the existing runtime entry-validation API. These were setup/compile failures, not passing test runs; no normal implementation or assembly definition was changed to accommodate them.

Evidence: `Evidence/baseline.xml`, `red.xml`, `b1.xml`, `review-red.xml`, `final.xml`, their logs, `baseline-final-comparison.json` and `delivery-manifest.json`; `Compile/Report.md`, `compile-summary.json`, `metadata.json`, `release-conformance.json` and build logs. Conditional checks compile seven managed projects against Unity-generated references. Actual Player packaging/builds, rendering, map restoration and manual play were not executed.

### 14.4 Architecture and locality audit

| Criterion | Result |
|---|---|
| SSOT / SRP | Existing Buffers retain facts. Planner owns only development route policy; existing navigation rules own ordinary admission; the runtime helper validates definitions; the transaction owns source comparison and atomic installation |
| OCP / LSP | Optional guarded code adds operations without altering ordinary entry or completion semantics. Normal store methods and public Group contracts remain unchanged; release signatures match A |
| ISP / DIP | Existing internal capabilities carry the new operations. The helper receives narrow reads, the existing stage manager and admission callback; it retains no resolver. Pure input/planner code uses no Unity, Buffer or Framework types |
| FW registration / hierarchy / lifecycle | The same Core factory-owned service receives the existing stage manager. The helper is not registered as a service or exported at root. Existing PostBind/Shutdown remain the only lifecycle path |
| Excluded areas | Framework, ServiceKeys, asmdef/asmref, View/Flow, ordinary navigation/completion/roster/deck/currency implementations, scenes, prefabs, SO assets and packages have no delivered Phase B changes |

The actual change scenario requires understanding ordinary navigation decisions, immutable progress snapshots, stage definition contracts and current/stage store preparation. Verification includes their existing entry/publication consumers because progress facts are deliberately shared. It requires no combat damage, reward settlement, shop pricing or View rendering internals. The added coupling to map/stage reads and the stage manager follows that shared-state requirement; no alternate entry path or generic coordination framework was added.

**Phase B stop:** Supported progress bypass and target preparation are delivered. C must still wire the public View facade and full-runtime inclusion, verify map redraw/restoration and consume ordinary target entry through existing input/Flow paths. No C work, panel/hotkey work, commit or push begins automatically.

## 15. Phase C delivery and outstanding acceptance — 2026-10-23

**Authorization:** The user explicitly authorized Phase C after reviewing Phase B. C1/C2 implementation and focused integration checks are delivered. C3 actual Player builds and release exclusion are verified; final Phase C acceptance remains open for manual View checks and a successful full scene round-trip. This is not a claim that Phase C or the repository is fully green.

### 15.1 Delivered behavior

- `ViewGroup.DevelopmentTools` exposes `IDevelopmentTools` only under `UNITY_EDITOR || DEVELOPMENT_BUILD`. It resolves the existing InGame parent's internal Core provider during PostBind and retains only the command reference, map callbacks and shared fault boundary.
- Admission reads the attached scope's existing lifetime, display, movement, popup and pending-input facts. Both unregistered commands and handled-but-unconsumed commands close admission. A changed navigation result requests the existing redraw once; actual display completion reopens admission. An adjacent no-op does not request redraw.
- Grants and navigation delegate exactly once to the A/B Core commands. View writes no gameplay Buffer, computes no path or grant rule, and performs no automatic target entry.
- Unexpected readiness, Core or redraw errors report through the existing shared boundary and rethrow. A redraw failure preserves already-published progress; subsequent calls are unavailable. Shutdown releases the command and map callback references.
- Lower View and paired factories default to omission. The paired factory passes the same inclusion value to Core and View; the existing full Systems runtime call opts in for Editor/Development. The facade is registered once inside View and only participates in PostBind/Shutdown. No child service is promoted to the runtime root.

### 15.2 Actual edit scope

New production files: `VIEW/Dev/IDevelopmentTools.cs` and `VIEW/Dev/DevelopmentTools.cs`, with metadata.

The six existing production seams are exactly `InGameViewSystemFactory.cs`, `InGameViewSystemGroup.cs`, `StageMapViewSystem.cs`, `StageMapViewScope.cs`, `InGameSystemFactory.cs` and `RunTimeSystemFactory.cs`. These changes add guarded construction/lookup and read-only readiness only. A/B production code has no Phase C delta.

New tests: `DevelopmentToolsTests.cs`, `DevelopmentNavigationIntegrationTests.cs` and `DevelopmentCompositionTests.cs`, with metadata. The last file also contains an Editor-only Player-build validation entry point that takes explicit output arguments, builds the existing enabled scenes, records the real BuildReport and checks the executable. It changes no Player settings.

The eight pre-enumerated ordinary ViewGroup test doubles received only guarded null facade properties. `StageMapViewSystemTests.cs` required no edit: the authorized new integration fixture exercises its real scope instead. Delivery comprises 24 C source/metadata files: two production files, six production seams, three test files, eight conformance edits and five new metadata files.

### 15.3 Verification and limits

Unity 6000.0.73f1 used the approved AppData-capable execution route. Persistent evidence is in the original checkout's ignored `Temp/DevelopmentToolsPhaseC/`, outside the running managed worktree's temporary directory.

| Check | Result |
|---|---|
| Pre-C primary regression baseline | 285 total, 254 passed, 31 failed |
| Initial missing-API probes | Two expected missing-member failures |
| Facade and composition fixtures | 30/30 and 8/8 passed |
| Real map and ordinary-entry integration fixture | 19/19 passed |
| Added Phase C cases | 57/57 passed; A+B+C feature cases total 223/223 |
| Final principal run | 343 total, 311 passed, 32 failed |
| Required entry-observer cases | 7/7 on pre-C and final code |
| Combined final evidence | 350 total, 318 passed, 32 failed; all existing outcomes match the expanded baseline by exact name and complete message |
| Actual Development Windows Player | Succeeded, 0 errors, 3 warnings; executable created; Standalone Mono (`Mono2x`) |
| Actual non-Development Windows Player | Succeeded, 0 errors, 3 warnings; executable created; Standalone Mono (`Mono2x`) |
| Packaged Player assembly audit | All 19 A+B+C declared feature types present in Development and absent in Release; Release has no feature type/member/IL references or guarded members across the 14 audited integration types |
| Player packaging boundaries | No test assemblies or Editor/test assembly references in either packaged project assembly set |
| Independent review | No production P1/P2 findings; one missing test namespace fixed before its first compile |

The initial 31 failures comprise two `CharacterRosterServiceTests` failures, two `FrameworkRuntimeCompositionTests` failures, 16 `StageMapViewSystemTests` failures and 11 `RecoveryStageMapViewTests` failures. The 27 map failures report the existing missing current-marker prefab; the new integration fixture supplies its own valid marker without changing those old fixtures.

The first pre-C Boot smoke was inconclusive because its generated first reachable node was Event. The final run generated a Combat route: Boot → StageSelect → ordinary click → Combat and battle preparation succeeded, but the existing subsequent audio assertion expected multi-instance preset indices `[0, 2]` and received `-1` at index zero. A fresh pre-C source replay reproduced that exact failure name and complete message. All 24 C files were backed up, temporarily removed/restored as needed for the baseline, and restored with exact SHA256 checks. The audio failure was not repaired or hidden.

`StageMapEntryObserverTests.cs` declares methods in the partial `ShopStageServiceTests` class, so its filename is not a runnable fixture filter. The seven cases were additionally run using their actual method names on both source states. `observer-filter.txt` records those names; this corrects test selection, not implementation scope.

The initial integration run had two test-preparation failures: EditMode deck UI setup had not invoked its existing Awake binding, and `AbandonBattle` ended the Run before the intended retained-battle guard could be tested. The fixture now initializes the real button binding and creates a normal victory through actual commands while retaining an in-progress Run. Both assertions pass without product changes.

The integration fixture verifies normal request numbering, existing Flow/Job/Core entry, result and observer consumption, real scope redraw/rebind, Combat/Boss/Shop/Event contexts, rejection after successful bypass, popup/movement admission, and active/completed retained-battle refusal. Its ordinary entry → victory → Stage-return cases use Core instances from the paired factory with inclusion both true and false. Loading and combat presentation are test stand-ins; the paired View is inspected and disposed but not initialized. These tests do not establish full scene/manual acceptance.

Actual Player builds use the existing enabled Boot/Lobby/StageSelect/Combat scenes and installed Windows Standalone module. Each packaged Managed directory contains 157 DLLs; the audit inspects all six project assemblies, records their hashes and checks references. The raw Development BuildReport option string is preserved in evidence; the actual backend is recorded separately as `Mono2x`. The three build warnings are in unchanged combat nullable annotations and the existing VolmetrickLight compute shader. No settings or assets were manually altered for these builds. Generated editor/build changes were excluded from delivery and restored in the managed worktree, with their prior contents preserved in scratch evidence.

Evidence: `Evidence/baseline.xml`, `red-and-smoke-baseline.xml`, `c1.xml`, `c2.xml`, `final.xml`, `smoke-baseline-expanded-1.xml`, `observer-baseline.xml`, `observer-final.xml`, corresponding logs, filters, `baseline-final-comparison.json`, `restored-c-source.json` and `delivery-manifest.json`; `PlayerBuilds/Development.log`, `Release.log`, each executable's preflight/summary files and `artifact-audit.json`. The initial comparison before the Boot baseline replay is retained separately. None of these results is repository-wide green or manual product acceptance.

### 15.4 Architecture and locality audit

| Criterion | Result |
|---|---|
| SSOT / SRP | Buffers retain gameplay facts, Core retains policy, and the scope retains presentation facts. The facade owns only admission, delegation and redraw/error forwarding |
| OCP / LSP | Optional guarded composition adds the feature without changing normal entry/completion rules. Omitted compositions exercise the same Core route; release contains no feature API |
| ISP / DIP | Consumers receive one focused facade. The facade retains narrow commands/callbacks rather than a resolver, DB or gameplay writer. Ordinary map objects have no facade dependency |
| FW-001/002/003 | One child service/module registration, existing parent export/lookup hierarchy and only the actual PostBind/Shutdown lifecycle |
| FW-004/016 | Internal Core providers remain internal and unregistered. No Framework internals changed; retained calls reject faulted/stopped runtime lifetimes |
| Excluded areas | No Framework, ServiceKeys, asmdef/asmref, ordinary Flow/roster/deck/currency/reward rule, scene, prefab, SO, package, camera or renderer change is delivered |

The necessary integration knowledge is limited to the View group's construction/lifetime, map scope presentation admission, and existing ordinary entry/observer behavior. Gameplay grant/path rules remain in A/B. No new runtime switch, hotkey, panel, root service export, mutable readiness flag, ordinary entry route or generic coordination framework was introduced.

### 15.5 Remaining acceptance and consumer usage

The [View handoff](../handoffs/2026-10-23-development-tools-view-handoff.md) provides the consumer lookup, guarded call examples, result/error handling, UI ownership and manual acceptance checklist. It records existing implementation contracts and does not authorize or implement a new UI.

The code is delivered to the primary checkout. No commit, push or external handoff message was made. Stop at this review boundary.

The following remain unverified and keep final Phase C acceptance open:

1. A successful actual-scene startup → stage → return smoke, including the existing audio assertions. The current ordinary Boot smoke reaches Combat but fails afterward in the same way without C.
2. Manual View checks: granted units appear and can be selected in the existing deck editor; displayed gold refreshes; skipped markers survive an actual scene rebind; ordinary interaction enters the prepared target.

An Editor/Development consumer gets `runtime.GetService(SystemGroupsService.InGame).ViewGroup.DevelopmentTools`, reads a context, then submits one of the four methods with that context. A null facade means the lower composition intentionally omitted the feature. An unavailable context means the consumer must wait for actual readiness. After a changed skip/jump, read readiness again after existing display work; do not assume an arbitrary frame completes redraw. `PrepareJumpToNode` only prepares access: enter its final target through the ordinary map input.
