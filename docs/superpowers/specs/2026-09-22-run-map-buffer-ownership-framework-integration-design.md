# Buffer-Authoritative Chapter and Map Progression: Core Integration Design

Date: 2026-09-23
Revision: 5 — one-chapter scope, single active task branch, and Core.Application invocation boundary.
Status: Design for the Core migration; owner-controlled integration is not implemented by this document.

## 1. Authority and scope

The Framework programmer guide and architecture govern assembly, lifecycle, service, and Job rules. The Framework owner's 2026-09-23 contract confirmation establishes the gameplay and ownership decisions below. Earlier revisions used a proposed Run identity/version/commit model; that proposal is superseded for this game flow.

The flow in this increment is: **enter one chapter → click an eligible node and immediately enter its stage → on victory, clear that stage and choose one connected node at the next depth → on defeat, end the attempt → on boss victory, record both boss and chapter clear**. A player cannot move backward, choose another node at the same depth after entry, or enter a cleared stage. The next chapter is not playable until the current chapter is complete, but choosing or unlocking it is outside this increment. No cross-chapter switch or return to an unfinished chapter is required. There is no Run restart, separate Run completion history, or Run identity requirement in this scope.

The requester can edit InGame/CoreSystem, including CoreCaculator, and permits a narrow Factory or Systems asmdef change when justified and consistent with the Framework's registration model. View remains another worker's area. Existing Buffer APIs are the starting point; extend an existing Buffer only for a demonstrated missing operation or display value with its owner. No new Buffer, general Framework change, generic transaction system, or Core → Systems dependency reversal is assumed.

## 2. Sources and checked implementation

Read [Framework programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [Framework architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), and the supporting [development guidelines](../framework/Framework%20Development%20Guidelines.md), [overview](../framework/01_Framework_Overview.md), [services](../framework/02_Framework_Services.md), and [system creation guide](../framework/03_System_Creation_Registration_Query.md) before implementation. Current source determines which published operations actually exist; a source/owner-contract gap is an integration question, not permission to cross an assembly boundary.

| Buffer | Existing source capability | Consequence |
|---|---|---|
| `CurrentProgressBuffer` | `CurrentMapKey`, `CurrentStage`, `SelectMap`, `SelectStage`; selected stage includes a name | Owns the active chapter and last entered stage for this flow. A node click is immediate entry, not a separate UI selection step. The published API does not itself validate eligibility. |
| `StageProgressBuffer` | Per-chapter `StageProgressData`: node ID, stage key, depth, width, outgoing edges, `IsClear`; `GetStages`, `IsCleared`, `RegisterStages`, `SetClear` | Owns generated topology and stage clears. |
| `MapProgressBuffer` | Per-chapter `MapProgressData.IsClear`, `GetMap`, `SetClear` | Owns chapter completion. |
| `StageGeneratorSystem` | Receives DB Query in `InitData`, Buffer sink in `PostBindService`; `Generate(mapKey)` later writes graph via `RegisterStages` | The target is generator/Core preparation into Buffer from DB data. Current generation is a separate call after binding; do not claim it already runs inside `InitData`. |
| `StageServiceManager` | `Enter(stageKey)` resolves stage data and calls type-specific handlers; current handlers only validate data | An accepted node click is not playable stage entry until the owner-controlled stage handler/flow is connected. Its possible failure must be coordinated with the `CurrentStage` write. |
| `StageUI` | Reads `CurrentStage.StageName` from `CurrentProgressBuffer` on open | This entered-stage screen already reads Buffer. A map screen needing names for **all** nodes must demonstrate where it gets them without a View DB query. |

`StageProgressData` does not carry stage names or explicit combat/non-combat kind. `CurrentStage.StageName` covers only the entered stage. Check actual screen requirements and the approved DB-to-Buffer preparation path before requesting a narrow metadata addition. `CurrentProgressBuffer.SelectMap` clears the entered stage when the map key changes but leaves it intact for the same key; do not treat a repeated call as a reset. `StageProgressBuffer.RegisterStages` rejects duplicate registration; `StageProgressBuffer.Clear` and `MapProgressBuffer.Clear` erase registrations. Initial chapter entry must not invoke those destructive clears. This one-chapter flow does not need per-chapter entry history or a new chapter-reset API.

## 3. Responsibility and data flow

```text
View command (enter chapter / click node / attack)
  → Systems Core progression coordinator reads Buffer through its published sinks
  → Core.Contracts values enter a Core.Application command entry point
  → Core.Application coordinates relevant Core.Domain rules and returns a Contracts result
  → when ordered or deferred work is needed, Systems/LifeCycle Plan supplies Jobs to JobRunner
  → progression coordinator writes the accepted result through Buffer APIs
  → View reads Buffer and renders the result
```

View sends intent only. It does not query the DB, assemble a Core snapshot for input, choose Job order, apply rules, or write progression state. Core.Domain owns individual rules; Core.Application is the default command-facing entry point when one operation coordinates those rules and their result. The Systems Core progression coordinator owns Buffer access and application of calculated changes. Buffer owns runtime storage, its public API, and persistence/reset of its records. Core may hold command-local copied data or an immutable lookup during calculation; it does not retain a second mutable map graph, current node, or clear set. A Core-owned presentation snapshot is not the View's source of progression data in this integrated path.

`Core.Application` and `Core.Domain` cannot reference Systems Buffer sink interfaces. `Core.Contracts` has no Framework/Systems/Unity dependency; `Core.Domain` references Contracts; `Core.Application` references Domain and Contracts. Systems currently references Contracts only. The preferred integrated direction is an **explicit one-way** `ProjectL.Systems → ProjectL.Core.Application` asmdef reference once a real Application command API exists. That API takes/returns Contracts values and calls Domain rules; it neither stores Buffer-owned progression nor merely forwards every Domain call without command-level work. A direct Domain call remains a documented narrow alternative for a pure calculation that has no Application orchestration. Bind Buffer services in the Systems CoreGroup coordinator through the existing `PostBindService` path, and register only that Systems service through the normal Factory. Core.Application/Domain are not Framework services or Jobs. Update the README and Framework architecture dependency description if the asmdef changes; keep the generic Framework assembly independent of Core.

The old `IRunProgressAccess.TryRead(RunId)` / `TryCommit(expectedRevision, RunMapRecord)` contract and Task 2 Buffer-backed `StageMapSession` constructor are historical Task 1/2 work, not the accepted production interface. They are excluded from the active branch and must not be wired to real Buffers merely to preserve previous code. Their commits remain local historical evidence. The base standalone `StageMapSession` remains for existing callers until cutover; Task 3/4 command-local values and rules supply the new progression preparation.

## 4. Progression rules

At initial chapter entry, use the existing `CurrentProgressBuffer.SelectMap` behavior after the graph is prepared. Generate/register the graph from DB data once under the agreed content lifetime; do not regenerate it to reset progress. Do not switch to another chapter during an unfinished chapter or model return to one in this increment. Chapter progression after completion is deferred.

For a node click, read the active chapter, graph edges, last entered node, node clear state, and chapter-complete state from Buffer. Core rules accept only an uncleared target connected from the current node at the next depth. The accepted click is a request for immediate stage entry, not a UI-only selection; the Systems coordinator invokes the approved entry flow and records the entered stage through `CurrentProgressBuffer.SelectStage` only when that entry succeeds. It does not mark the stage clear. While that stage is uncleared, no different node may be entered; after victory, only its forward connected successors are eligible. Core must not infer a new current position by replaying its old `StageMapRunState`. No Run ID, revision, lock, or stale-request protocol is added solely for this synchronous flow; handle any concrete asynchronous or failure path where it is introduced.

Core calculates command rules and required effects without implementing the Framework Job interface. Core.Application coordinates command-level calculations, delegating individual rules to Domain; the Systems coordinator applies accepted progression effects through Buffer APIs. For commands that need ordered or deferred execution, the existing Systems/LifeCycle Plan builds the necessary Job group and JobRunner executes it; those Jobs must still route progression effects through the coordinator. This does not make every map click a Job. Related effects such as attack, death, turn change, stage victory, and chapter victory may be part of one command result. View neither orders Jobs nor decides the state transition. The existing `JobRunnerSystem` currently repeats incomplete Jobs on later Ticks, and its concrete gameplay Jobs remain scaffolding; implementing this flow needs explicit integration work and must not be described as already playable. A completed operation must not be replayed merely because a Job is observed again.

On stage victory, mark that stage clear in `StageProgressBuffer`. On boss victory, mark the boss stage clear and its chapter complete in `MapProgressBuffer`. Defeat ends the attempt without marking either clear; an unsuccessful entry also marks nothing. `MapProgressBuffer` records chapter completion, not a next-chapter unlock decision in this increment. The coordinator must inspect the actual writes and failure behavior before enabling a multi-Buffer command: the existing separate setters do not document an all-or-nothing guarantee. Do not assume single-thread execution implies rollback or atomicity. Prepare and validate all known inputs before the first write; if a real partial-failure path remains, resolve that specific path with the owner before production. This is a focused integration check, not a prerequisite to invent a Run transaction protocol.

The Buffer owns initialization range and preservation rules behind its public APIs. Core calculates the accepted command result; the Systems coordinator applies it through Buffer APIs. Neither Core nor the coordinator assembles an initial chapter state by calling `Current.Clear`, `Stage.Clear`, and `Map.Clear` in sequence. Existing `SelectMap` expresses the current-chapter choice for this scope; no new chapter-entry/reset operation is assumed. Framework `InitData` and `PostBindService` are one-time runtime phases, not chapter reset buttons.

Combat unit state, wallet/deck, shop/recovery, and Artifact/resource leases keep their existing owners until separately migrated. When a command touches both those owners and progression Buffers, preserve the existing command invariants and resolve only observed coordination gaps. A release/disposal failure must retain cleanup ownership and block duplicate resource acquisition until resolution; do not equate a disposed flag or uncertain release with successful cleanup.

## 5. Open integration checks, not preapproved changes

1. **Initial entry and graph lifetime:** Confirm when `Generate(mapKey)` runs relative to the initial `SelectMap` and first View read. Register the chapter graph once. `Clear()` removes registrations and must not be treated as an entry operation. An accepted node click maps to `SelectStage` only after Core validation; no separate selection state is required.
2. **Display metadata:** List the actual View fields needed for a map and stage screen. `CurrentStage.StageName` exists; names or execution kind for all graph nodes are not in `StageProgressData`. Request only the data that the approved View path cannot display from Buffer.
3. **Coordinator/Job composition:** Define a minimal Core.Application command API, then identify the Systems CoreGroup coordinator, its `PostBindService` binding, the explicit one-way Systems asmdef reference, normal Factory registration, and how the current Job scaffolding receives real commands. The requester can edit CoreSystem and justified Factory/asmdef files; View integration belongs to its worker. Only a demonstrated Buffer or generic Framework change needs separate owner review.
4. **Concrete multi-Buffer failure paths:** Verify the boss-stage clear plus chapter clear sequence and any connected combat/resource writes. If a setter or callback can fail after an earlier write, agree on a narrow prevalidation or coordinated write mechanism before that production path is enabled. No universal transaction guarantee is inferred from the contract confirmation.

The owner confirmation resolves the old BR-01 request for Run ID/version/history and the old BR-04 Run restart policy by removing them from scope. The clarified one-chapter flow also removes the proposed per-chapter cursor/history extension. It does not claim the integration checks above are already implemented. The prior BR-03 all-or-nothing expected-revision gate is superseded by concrete failure-path validation; the prior BR-02 connection question remains as a narrower Systems composition task.

## 6. Acceptance

- The integrated map screen and stage screen read their displayed progression data from Buffer; View supplies commands only.
- A node click immediately enters an eligible next-depth stage. An entered stage blocks same-depth alternatives and backward movement; defeat ends the attempt. Cleared stages cannot be re-entered. Boss victory records both boss and chapter completion in Buffer.
- Graph node IDs, keys, edges, and clear flags come from `StageProgressBuffer`; Core has no parallel mutable map authority.
- The Systems coordinator reads/writes through published Buffer sinks after service binding and invokes Core.Application through a documented one-way asmdef reference if its command API is needed. Core.Application calls Domain rules and never resolves Buffer services.
- A failed command does not falsely report victory or clear a stage. Actual multi-Buffer partial-failure risks are resolved before production use.
- Focused Core tests and owner-side composition/Unity checks are reported separately. Passing managed tests do not establish real Buffer integration or a complete game bootstrap.

No code or Framework/Systems approval is implied by this document revision.
