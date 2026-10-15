# Chapter Continuation and Final Run Completion — Core Design

Date: 2026-10-03  
Status: P1–P3 implemented and verified, 2026-10-04; P3 independent review complete. See the [current consumer handoff](../handoffs/2026-10-03-chapter-continuation-run-completion-handoff.md), [P1 verification](../handoffs/2026-10-03-chapter-continuation-run-completion-p1-verification.md) and [P2 verification](../handoffs/2026-10-03-chapter-continuation-run-completion-p2-verification.md).
Source baseline: `e8977adc` plus the existing uncommitted recovery/shop and temporary Core runtime work.

## 1. Intent and scope

The user wants Core development to continue with disposable data while the real Excel-imported DB is being prepared. The immediate outcome is a working, service-driven path from a cleared chapter into its configured successor, followed by final Run victory when the last chapter is cleared. Success includes preserving HP, currency, owned characters/deck, souls and Artifacts across the chapter boundary.

This is an architectural integration increment: it adds a missing continuation contract and an authoritative terminal Run fact. Existing chapter entry alone cannot determine the next chapter or prove final Run completion. This document records the selected design. P1–P3 execution is complete; disposable fixture data does not imply a new production gameplay policy.

The temporary battle source already supplies explicit targeting profiles and has passed real combat/recovery/shop cycles. Method_Key authoring remains a production replacement dependency, not a blocker for this temporary Core work. Temporary chapter links and fixtures must be removed after the real data adapter and its acceptance are ready; the general Core APIs and rule/service tests remain.

Included:

- Explicit chapter continuation, based on a supplied next-chapter or terminal definition.
- Persistent Run victory, duplicate/stale-request rejection and terminal gameplay admission checks.
- Preservation of existing battle settlement and caller-owned cleanup/presentation boundaries.
- A narrow mapping of existing accepted attempt-ending defeat to Run defeat, so a defeated Run cannot advance through the newly introduced continuation path. No new loss condition is proposed.
- An additional disposable two-chapter scenario through real registered services and Buffers.

Excluded: production Excel/importer/asset changes, production Method_Key mapping, reward distribution or result totals, new restart/continue/save policies, scene transitions, Job/Flow/View implementation, deck synergy, character growth, consumables and unrelated framework migration. Screen exit never implies defeat or reset. The existing explicit NewRun/reset route remains the reset authority.

## 2. Current source and reusable contracts

| Source | Observed behavior / consequence |
| --- | --- |
| [ChapterProgressionCoordinator](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ChapterProgressionCoordinator.cs) | EnterChapter accepts an explicit target; after the current chapter clears it does not choose or validate an authored successor. It generates a missing graph, then selects the map. |
| [ChapterProgressionCommands](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Chapter/ChapterProgressionCommands.cs) and [ChapterProgressionRules](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/StageMap/ChapterProgressionRules.cs) | Pure navigation and completion decisions already exist. Boss victory requests stage and map clear; defeat returns EndAttempt. |
| [BufferSystemGroup](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Core/BufferSystemGroup.cs) | Battle completion prepares and installs character/field/progress/completion changes through existing publication boundaries. EndBattle cleans active combat; ResetRunProgress is explicit and preserves Artifacts/map registrations. |
| [StageGeneratorSystem](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/StageSystem/StageGeneratorSystem.cs) | Generate currently reads definitions and registers the graph in one operation. Continuation needs a prepared graph before changing the selected chapter. |
| [StageMapData](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/Table/Map/Item/StageMapData.cs) | Contains graph-generation settings, not a next-chapter or final-chapter field. No Chapter_NextChapter adapter/query is currently wired. |
| [RunOutcome](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Common/RunOutcome.cs) | InProgress, Victory and Defeat already exist as contract values. Current Buffer/Core factories do not register an authoritative Run outcome store or active Run lifecycle service. Old Run snapshots/request types are not evidence of a connected implementation. |
| [ChapterGraphPreparationTests](../../../Assets/Tests/EditMode/Framework/ChapterGraphPreparationTests.cs) | Existing tests cover explicit chapter changes and preservation. They do not prove data-selected continuation and final Run victory through two real chapter completions. |

Reuse the current Framework contract: Systems services orchestrate; Core.Application/Domain calculate from values; Buffers own mutable runtime facts; Factory creates dependencies once; consumers resolve through `SystemGroupsService.InGame.CoreGroup`. Do not revive a mutable StageMapSession, revision-based Run graph or parallel GameFlow snapshot owner.

References: [Framework guidelines](../framework/Framework%20Development%20Guidelines.md), [overview](../framework/01_Framework_Overview.md), [services](../framework/02_Framework_Services.md), [creation/registration/query](../framework/03_System_Creation_Registration_Query.md), [programmer guide](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md), [architecture](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md), [Buffer ownership design](2026-09-22-run-map-buffer-ownership-framework-integration-design.md) and [shop handoff](../handoffs/2026-10-03-shop-stage-core-connection-handoff.md). The architecture pipeline document is not a prerequisite. Historical session-based Run documents do not override current Buffer ownership.

## 3. Approach and alternatives

Recommended: extend the existing chapter coordinator with one continuation operation, inject a read-only continuation-definition source, and add a minimal Buffer-owned Run outcome/terminal receipt. This keeps one progression service and allows temporary and future DB definitions to use the same calculation/publication path.

A fixture-only direct EnterChapter(2) test is smaller, but leaves next-chapter selection, terminal persistence and stale-input admission unimplemented. A new all-purpose Run/Flow manager or restoration of old session/revision architecture would duplicate current progression ownership and expand the work unnecessarily.

A dedicated minimal Run progress Buffer is justified here because Run completion must outlive battle cleanup and is neither a per-chapter IsClear flag nor a transient method return. It holds no graph, wallet, roster or View phase. Existing stores continue to own those facts.

## 4. Continuation definitions and composition

Proposed Systems dependency: `IChapterContinuationSource.Read(int chapterKey)`, returning an immutable Contracts definition with the requested chapter key and exactly one alternative:

- NextChapter: one positive next-chapter key different from the source.
- FinalChapter: an explicit terminal marker, with no target key.

Unavailable data, an unknown chapter, null, a mismatched source key or an invalid successor must fail explicitly. Missing data must never mean final chapter. Do not infer `current + 1`, use the highest registered key, choose the first available map or embed fixture IDs in production rules.

The future DB adapter translates the agreed Chapter_NextChapter representation into this contract. The actual spreadsheet column type and terminal sentinel remain the DB author's schema decision. This increment defines the Core meaning without inventing a production table. The temporary adapter explicitly supplies `1 -> 2` and `2 -> FinalChapter`. The normal default source reports definition unavailability until the real adapter is supplied; it must not silently select temporary data. Ordinary first-chapter entry does not require a continuation definition.

Factory injection is named explicitly, following the existing `CreateWithBattleSource` precedent; keep the existing parameterless Create name unique for reflective callers. Build-once composition, InitData/PostBindService ordering and interface-only exports remain intact. Supply the battle source and continuation source to the same runtime, not to a second set of services.

## 5. Selected consumer and result contract

Extend the existing `IChapterProgressionCoordinator` rather than add a second progression service:

```csharp
ChapterContinuationResult CompleteChapter(int chapterKey, StageNodeId bossNodeId);
```

The result has an Accepted flag, a rejection reason, the source chapter/node/key, and an accepted kind of NextChapterSelected or RunCompleted. NextChapterSelected includes its positive target chapter key; RunCompleted includes Victory and no next key. These names are implemented by P2. Do not force these meanings into ChapterProgressionResult.ClearMap or synthesize another battle outcome.

Consumer sequence:

1. Resolve boss victory through the existing combat commands and joint settlement publication. Boss node and chapter become clear once.
2. The consumer presents/acknowledges the result according to its own lifecycle, then legitimately calls Combat.EndBattle().
3. Call CompleteChapter with the completed chapter and selected boss node identity.
4. NextChapterSelected means its graph is ready, the Current map is the successor and no node/battle has been selected. The consumer can display that map and later request normal explicit stage entry.
5. RunCompleted means the terminal Run fact is committed and available for results display. No scene switch or resource disposal is performed by Core.

A retained active or terminal battle blocks CompleteChapter. The service does not force EndBattle, clear View inputs or reset progress. Selection of the successor is not automatic first-node/battle entry.

### Admission and retries

Require InProgress Run outcome; matching current chapter, selected node and key; a cleared source map and selected boss node; matching graph/root combat-boss identity, type and role; a real retained Victory completion whose progression identifies that same cleared boss/chapter; and no retained active battle. A forged clear flag alone is insufficient.

Protect that completion during the InProgress interval after boss settlement and caller EndBattle, before CompleteChapter. Before P1, parameterless Combat.PrepareBattle checked HasBattle and selection presence but not node/map clear facts; successful Buffer preparation clears the retained completion. P1 added a minimal service admission check requiring a matching, uncleared committed node/key and uncleared current map, before definition lookup and again after external lookups before publication/cache assignment. Reject without changing completion, selection, persistent facts, counters or definition cache. A legitimately committed uncleared selection remains preparable; do not rerun fresh-click navigation against it. No pending-continuation flag or new Buffer is needed. The implementation plan records the exact source trace; this observation is not a runtime reproduction.

Read/validate the continuation definition and the target map/root data. The target must be registered, distinct from the source and uncleared. Reject returning to a cleared chapter, which also prevents a linked cycle from revisiting completed progress. Revalidate captured selection, completion identity, Run outcome, source/target progress, graph state and battle absence after the last external lookup and again at publication. No external query follows the final storage guard.

Route lookup, graph preparation or validation failure leaves the boss settlement and source clear records committed, preserves source selection/HP/gold/roster/Artifacts and performs no continuation write. Correct the cause and retry only CompleteChapter. Never replay victory, reset the Run or reroll a previously installed target graph to recover from a continuation fault.

After a successful next-chapter transition, the old source request is stale and rejects without writing. After terminal publication, a duplicate request reports RunEnded without changing the existing terminal receipt. The consumer reads the receipt to display the already committed result. There is no generic transaction, retry session or persistent revision counter.

## 6. Safe target preparation and existing EnterChapter

The present Generate method publishes a graph. Calling it and then validating continuation would leave a registered graph behind on failure. Extract a small preparation path that returns detached, validated node data using the same existing generation algorithm/random-call order. Keep Generate as the existing prepare-and-publish wrapper for existing initial-entry consumers.

For continuation, reuse a valid registered target graph without rerolling. Otherwise prepare it off-store. Extend the existing Buffer group with a narrow chapter-continuation publication boundary: verify expected source/target state, prepare any target registration and map selection, then install them without further fallible queries. Reuse the current prepare/install pattern; do not introduce a generic transaction framework. Failed preparation must not change source selection or install a partial target graph. Current selection changes only on successful publication and clears old node/shop/recovery selection as SelectMap does today.

Public EnterChapter must not become a way to skip configured successors or escape a terminal Run. Proposed intentional tightening:

- Initial entry with no Current map remains supported and retains existing graph preparation behavior.
- Same-chapter calls preserve existing entry rules and never reset the Run or reroll a graph.
- Cross-chapter calls are validated through the same continuation path using the current selected boss identity; the requested target must match the configured successor. Rejection occurs before publication, not after entering a different map.
- A final definition cannot be turned into an arbitrary chapter jump through this legacy API; final completion uses CompleteChapter.
- All chapter/stage entry paths reject after Run termination.

Audit direct callers and update their fixtures to inject explicit route definitions when exercising cross-chapter behavior. Existing preservation tests remain; their former arbitrary-target acceptance is intentionally replaced with route-constrained acceptance. No permissive compatibility bypass is retained for terminal progress.

## 7. Run ownership, termination and command admission

Proposed `RunProgressBuffer` stores only Outcome (initially InProgress) and an immutable terminal receipt containing outcome and originating chapter/node/stage key. Reuse the existing RunOutcome enum. Do not reuse old RunProgressSnapshot as the storage model: its phase/session and death-only assumptions are not this Buffer contract.

The Buffer factory registers the store once; the group exposes a read sink and narrow internal prepared writes. Consumers read terminal state from Buffer-backed queries rather than a mutable Core-owned copy. Terminal writes are one-way until the existing explicit ResetRunProgress boundary resets outcome/receipt. Construction does not imply roster initialization; existing roster readiness remains a separate admission condition.

- Final victory: after successful CompleteChapter validation, commit Victory once, retaining the final chapter/boss selection and all final facts for display. Do not select a nonexistent next chapter or clear final records.
- Defeat: an accepted existing combat completion with EndAttempt commits Defeat and its receipt within the same existing prepared battle-completion publication. Cover both command-driven and explicit completion paths. No new route/DB query occurs inside publication, and failed battle publication cannot leave a terminal Run fact behind.
- Supported defeat publication includes ProcessNextCommand/AdvanceCombat through command publication and ResolveBattle/AbandonBattle/terminal FinalizeTurn through CompleteBattle. All use the same prepared completion extension. Pure rules and PlanStageResolution may return EndAttempt while InProgress without changing any state; this is only a proposal. The legacy ResolveStage wrapper currently writes victory clear flags directly and returns accepted defeat without settlement or a progress write. Preserve its planning rejections and legacy victory behavior, but reject an accepted EndAttempt with InvalidOperationException identifying that complete battle settlement is required, before writes or an accepted return. After termination, return RunEnded. Direct defeat resolution is unsupported; callers needing actual defeat use full combat settlement, never an independent Outcome setter. Legacy victory clear flags alone still cannot authorize CompleteChapter.
- This follows existing defeat/voluntary battle-abandonment decisions. It does not add Run abandonment on screen exit or require the main character to be dead for an already accepted voluntary defeat.
- EndBattle still works after either terminal outcome and does not reset the Run receipt. Latest combat result/counters retain their existing lifecycle; a Run receipt does not copy or indefinitely retain the full combat state.
- Explicit ResetRunProgress clears the new receipt/outcome as part of its existing scope. It continues to preserve Artifact ownership and map registrations; new-game economy/roster setup remains caller-owned and unchanged.

After Victory/Defeat, reject Run gameplay mutations before their writes: chapter/stage preparation and confirmation, battle entry and commands/advancement, manual draw/placement/combat mutations, shop entry/preparation/purchase/completion, recovery entry/application/completion and roster acquisition/reinitialization. Include compatibility shop/recovery entry points and narrow Buffer publication boundaries so stale prepared work cannot bypass the guard. Prefer existing appropriate failure results; define explicit RunEnded where existing contracts have no unambiguous representation. Read/query operations, result acknowledgement, caller-owned View input cleanup, EndBattle, Shutdown and explicit NewRun reset remain allowed. Persistent Artifact management is outside Run gameplay admission and retains its existing battle-lock/ownership contract; do not silently add a new meta-progression lock.

Command/result/preview handling must preserve its established reporting contract: rejecting gameplay must not invent an accepted command, clear pending inputs, or reset counters. The implementation plan must enumerate the actual public mutation methods and their result/exception mapping before coding guards. Raw owner-side Buffer setup ports are trusted infrastructure, not new user-facing gameplay APIs.

Preserve duplicate CompleteBattle/ResolveBattle/AbandonBattle false semantics, once-only settlement and cleanup availability. The source audit found no production caller of the direct ResolveStage wrapper under Assets/ProjectL; migrate its two direct-defeat test uses according to their purpose: planning/rejection evidence versus real defeat before explicit NewRun. Keep existing reset/preservation assertions. Internal store installation and test setup remain distinct from supported gameplay publication; they do not require a second defeat calculator or production Job changes.

Operations remain synchronous/serialized. No guarantee against identity reuse after explicit Run reset is added: rejecting an old request across a fresh Run would require a separately agreed Run identity contract. Do not claim this increment provides that protection.

## 8. Disposable two-chapter fixture

Add a named `TemporaryCoreRuntimeFactory.CreateWithChapterContinuation()` under the existing development guard. Keep Create() recovery and CreateWithShop() behavior available as separate regression fixtures.

Use the existing shop scenario as chapter 1: start 1 -> battle 2 (101) -> shop 5 (30) -> boss 4 (102). Add chapter 2: start 1 -> battle 2 (201) -> boss 3 (202), with per-chapter node identities, depths 0/1/2 and distinct stage keys. Supply valid map/root/field/economy definitions for chapter 2 and continuation definitions `1 -> 2`, `2 -> FinalChapter`. Reuse the temporary targeting source for the existing two unit types; no production Method_Key schema is invented. Validate the new roots against chapter 2 rather than reusing chapter-1 MapKey values accidentally.

Initial wallet remains 50 and roster remains main plus three Subs. Buy one cost-5 shop slot so the transition must preserve gold 45 and five distinct owned records. Damage the main through an existing health API and supply nonzero souls plus a real Artifact fixture through existing supported test seams. Resolve wins through actual combat commands, not direct clear writes. Fixture values are test or development data, not balance/content approval.

Do not reinitialize loadout, currency, soul bonuses or the Artifact board on entering chapter 2. Its battle preparation must read the existing roster and main state. Purchased Sub identities appear exactly once in its full draw order; initial hand size is not a test for full-deck inclusion.

When the real DB arrives, replace the continuation and targeting sources with real adapters, switch callers to normal composition/authored graph generation, run equivalent integration acceptance, then remove temporary factories/data and temporary scenario tests. Keep generic continuation/Run rules, Buffer storage and service tests. Missing production definitions must still fail explicitly.

## 9. Acceptance and failure witnesses

| Witness | Required evidence |
| --- | --- |
| Two-chapter success | First real boss settlement -> blocked continuation while battle retained -> caller EndBattle -> configured chapter 2 selected -> next battle -> final real boss settlement/cleanup -> one Victory receipt. |
| State retention | HP, persistent maximum, owned IDs, souls, wallet 45, Artifact contents/identity and prior chapter node/map clears survive transition; next battle contains the acquired Sub once. No repeated initial acquisition or settlement effects. |
| Final behavior | Final chapter remains selected/clear; no successor graph or battle is created; duplicate completion and all enumerated gameplay mutations add no gameplay writes; reads/cleanup remain usable. |
| Bad definitions | Missing source/target, implicit/ambiguous terminal, self-link, wrong key/map, cleared target/cycle and invalid target graph reject without interpreting failure as final victory. |
| Late mutation/fault | Source query or final graph-definition lookup mutates current identity, installs a battle, ends the Run or changes progress/registered graph: reject with no own partial graph/selection/receipt writes. Use existing query seams; no production fault hooks. |
| Retry after settlement | Boss victory already committed; continuation lookup/preparation fails. HP/gold/roster/clear flags remain; correct data and retry only CompleteChapter; transition or final receipt happens once. |
| Stale identity / legacy entry | Old chapter/node input cannot complete the next chapter; arbitrary cross-chapter EnterChapter cannot bypass the route or RunEnded guard. Same numeric node ID across chapters is not the same intent. For both Next/Final routes, genuine boss victory -> EndBattle -> rejected parameterless PrepareBattle preserves completion, selection, persistent facts, clears, results/counters and definition cache -> CompleteChapter still succeeds. Retain positive coverage for a committed uncleared selection. |
| Defeat coherence | Existing real loss/accepted voluntary defeat yields one Defeat receipt within completion publication, blocks continuation/entry and preserves cleanup/query contracts; rejected publication yields no receipt. While InProgress, PlanStageResolution(Defeat) remains read-only, but direct ResolveStage(Defeat) rejects without writes. Reuse existing command, explicit completion and turn-death tests; do not duplicate suites. |
| Explicit reset | Ordinary transition/cleanup never calls ResetRunProgress. Explicit reset alone restores InProgress/clears receipt and retains the existing Artifact/map-registration contract. |
| Existing behavior | Default temporary recovery and shop cycles, initial/same-chapter entry, graph algorithms/reflection/composition, roster/deck, recovery/shop retry, combat settlement/retention and both supported initialization orders remain covered. |

Reuse already sufficient coverage and add only witnesses for the new connection. Run meaningful RED/GREEN through real Unity EditMode using the approved AppData-capable path. Capture a fresh broad baseline at implementation time; the latest shop P3 evidence was 1666/1671, with five unchanged Economy failures, and is historical rather than a new result for this design. No whole-suite-green claim while those failures remain. Store raw logs/XML/launchers outside the repository.

Core acceptance proves these APIs and state transitions. Production authored data, nondevelopment player execution and actual Job/Flow/scene presentation/order remain distinct acceptance. View/Flow still owns result display/acknowledgement, transition timing and its pending-input cleanup.

## 10. Implementation phase boundaries

The implementation plan is authoritative for executable steps. P1–P3 are implemented and verified; independent review is complete:

1. Define minimal continuation/result/Run receipt contracts, pure decisions and Buffer-owned terminal publication/reset. Pin existing defeat integration and the terminal mutation admission matrix.
2. Extend the current coordinator and safe graph preparation/publication; inject continuation definitions, constrain cross-chapter EnterChapter, and prove retries/no partial writes.
3. Add the disposable two-chapter runtime and command-driven integration tests, then document consumer handoff and production replacement.

Expected code areas: current ChapterProgressionCoordinator; chapter Application/Domain/Contracts; StageGeneratorSystem; BufferSystemGroup, Current/Stage preparation stores and new minimal Run progress store; Buffer/Core factory and service/read-sink registration; existing combat completion publication; required gameplay entry/mutation guards; temporary development definitions and integration fixtures. Mechanical forwarding of new Buffer query members in test doubles is expected. Framework assemblies and production Job/Flow classes are outside the change.

Permanent P1/P2 API names and files are resolved in the implementation plan; the delivered P3 temporary composition and its evidence are specified there and in the current consumer handoff. Documentation remains English; new code comments remain Korean. Preserve the current uncommitted work and do not commit or push without user instruction.
