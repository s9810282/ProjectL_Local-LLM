# Tornado Stage Gimmick — T3 Execution Report

2026-11-01 · Production Core cutover and acceptance

For implementation by the View owner, start with the [combined Tornado / Rockfall / Haboob Core-to-View handoff](2026-11-01-tornado-rockfall-haboob-core-view-handoff.md). It contains the current public contracts, actual View migration points, authoring guidance and acceptance checklist.

## Scope and resulting behavior

The user authorized T3 after T1 and T2. Work follows the [plan](../plans/2026-11-01-tornado-stage-gimmick-implementation-plan.md) and [design](../specs/2026-11-01-tornado-stage-gimmick-design.md). The selected checkout remains `feature/Cheon/Core/NewCombatSytem`, HEAD `c3daac1fcef74457cb92b2460e1628ddac9cc267`. No commit or push was made. The existing FMOD catalog change was preserved.

Normal stage preparation now installs the grouped moving square instead of single-cell Tornado terrain. Default completed-side ends warn at 1, travel at 2, then repeat at 3/4. Both accepted player and enemy ends increment once before scheduled effects; lethal enemy-end landings retain that event and count. Scheduling runs Tornado → Railway → Haboob → Rockfall through the existing composition and transaction.

Deployment placement, pre-entry enemy redeployment and combat activation do not synthesize contact. Subsequent actual combat entries through movement, both forward swap arrivals, shove/knockback, pull and tile displacement use the same contact calculation. A real throw interrupts the old route and settles the actual landing. State persists through ordinary deployment returns within the same battle.

## Preparation, rules and ownership

| Owning boundary | Change |
|---|---|
| `BattleDefinitionAdapter` | Appends the grouped definition after existing definitions, preserving their IDs; excludes old terrain instances when the typed group is authored. |
| `InitialTornadoPlacementCalculator` / `BattlePreparationCalculator` | Converts legacy placement inputs once when required, strips their terrain/bindings, reserves all source IDs, and rejects mixed duplicate authored routes. Preparation keeps authored visibility and custom intervals. |
| `StageGimmickComposition.Initialize` | Initializes Haboob and Tornado using one environmental RNG cursor in existing definition order. Random storm members initialize in source-ID order. Fixed starts use no storm draws unless their side is ambiguous. |
| `StageGimmickComposition.WithMovementRules` | Binds the caller's validated arrival rules to Tornado, then runs the other existing rules. The follow-up below unifies its registry with `Default`; execution bindings remain explicit. |
| Movement and attack arrival seams | Invoke the feature at actual reached cells after existing fall/thorn handling, preserving caller-specific object timing. Deferred arrivals and interrupted paths use the actual result. |
| Existing Buffer / battle transaction | Validates expected/final state and ordered evidence, then installs character, scheduling, RNG and turn facts atomically. No new store or transaction was added. |

The only intended content change is `Assets/ProjectL/DataBaseSO/InGameGroup/Terrain/Item/Terrain_Tornado.asset`: first/repeat 3/3 → 2/2. Other profiles retain authored tuning. Legacy `ResolveDue`, old terrain entry and respawn calculations were removed after auditing callers. Historical `TornadoSourceFact`, random placement input and `CombatTornadoEvent` payloads remain available for input conversion and downstream migration; normal prepared Tornado execution does not emit the old event.

## Consumer contract

`CombatStateData.ReadTornadoAreas(Owner observer)` returns immutable observer-filtered `TornadoAreaFact` entries. Actual members are `GimmickId`, `SourceId`, `Center`, `SideLength`, derived `Cells`, `Phase`, `NextBoundaryCount`, `WarningDirection` and `IsActive`. During deployment the permitted stationary footprint remains readable and inactive. Warning direction is exposed only for active warned state. The authoritative center and tuning remain in grouped state/definitions; the reader owns no independent mutable copy.

`MovementPreviewContext.VisibleTornadoAreas` records permitted geometry. Public movement and attack projections filter sources by the actor's owner. A visible contact captures existing landing/occupancy/environment-RNG dependencies; geometry or relevant blocker changes invalidate the old context. Preview uses a projected cursor and leaves stored RNG unchanged. Existing irreversible relocation evidence prevents movement undo checkpoints; undo remains restoration.

The event family from T2 is now emitted by production callers:

- `CombatTornadoGimmickEvent` (`TornadoAdvanced`, 44): `Expected`, `Final`, `Boundary`, `Advances`; one group summary per boundary.
- `TornadoMemberAdvance`: `Expected`, `Final`, `SideLength`, actual `Path`, derived `IsTravel`.
- `CombatTornadoCenterEvent` (`TornadoCenterReached`, 45): actual `Origin` and `Center`.
- `CombatTornadoEntryEvent` (`TornadoEntryStarted`, 46): `GimmickId`, `EntrantId`, actual `Cell`.
- `CombatTornadoMemberEvent` (`TornadoMemberAdvanced`, 47): source completion after its actual contacts, with `GimmickId` and `Advance`.
- `CombatCharacterEvent.TornadoContact`: `TornadoContactOrigin` with `GimmickId`, `SourceId`, `Center`, `StepIndex`. Stationary entry uses -1; sweep begins at 0. `TornadoSourceId` retains the same source identity.

Presentation still needs a separate owner to consume this contract. Existing consumers of the legacy shape include `ViewSystem/Combat/Display/Map/EnvironmentVisualKinds.cs`, `Input/Assist/CombatEnvironmentText.cs`, `Playback/Handlers/CombatFactEventHandler.cs`, `Playback/Support/CombatEventSounds.cs` and `Playback/Ordering/CombatEventTypeTable.cs`. Field terrain presentation currently derives visuals from old terrain instances. It must read permitted areas and handle the new event family, actual center paths and attributed relocations. No View, scene, UI or camera files were modified. Core acceptance does not establish PlayMode, visuals or authored-stage readiness; T1's inventory found no reference to the named default Tornado asset in production stage placements.

## Verification and review

Unity 6000.0.73f1 ran on the AppData-authorized path with EditMode assemblies `ProjectL.Tests.Core;ProjectL.Tests.CoreIntegration`. Final artifacts are `Library/TornadoT3/final-acceptance.xml` and `.log`.

| Final verification | Result |
|---|---|
| Full Core | **4,521/4,521 passed**, 0 skipped |
| Full CoreIntegration | **628/629 passed**, 0 skipped |
| Dedicated T3 runtime path / real service fixtures | **11/11 and 7/7 passed** |
| T1 geometry / preparation / authoring | **7/7, 18/18 and 6/6 passed** |
| T2 resolution / evidence / publication | **19/19, 14/14 and 5/5 passed** |
| `DataBaseAssetTests` | **9/9 passed** |
| Final diff / metadata / pure contract checks | `git diff --check` passed; all new C# files have metadata; no UnityEngine imports in new Core contracts |

The only full-suite failure is the unchanged `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", false)` namespace/type lookup (`TypeLoadException`, source line 247). The installed class uses the global namespace; the test requests the Cinemachine namespace. Test/package/camera files are unchanged. There are no C# compile errors. No View, scene or camera changes were introduced.

The final real-service acceptance also verifies stale off-path occupancy rejects the old command without consuming RNG, forged duplicate pull evidence leaves every store unchanged, and fatal scheduled enemy-end publication retains one `EnemyPhaseEnded`, count 2, one outcome and only the actual origin center. The post-review full run initially exposed a new test's invalid y=6 coordinate on the 8×6 fixture; the fixture was corrected to y=5 and the entire suite rerun. No product behavior was changed for that fixture correction.

T3 first runtime integration run was RED 0/2 because no production handler was registered. The first full cutover run exposed obsolete one-cell/respawn/3-6-9 assertions. Those tests were migrated to the new prepared/contact contract; shared Leap, regeneration and wall regressions use an explicit one-cell new test profile where their original point geometry matters. Production defaults remain 3×3. Superseded cases were replaced by T1/T2 coverage and normal service acceptance, so the total test count intentionally changes.

The migration full run passed 5,132/5,133. The only failure was the unchanged CameraOffset namespace lookup documented in T1/T2. The initial extended runtime group passed 10/10. Later acceptance includes actual knockback/pull/tile interruption, both forward swap arrivals, observer filtering, stale area/blocker rejection, combined initialization RNG, injected scheduled arrival rules, deployment returns and pre-entry redeployment.

Independent final review reported three Important issues. All three were fixed in one author pass; the related 56-test acceptance group then passed 56/56 (`review-green.xml`, `.log`). No re-review was dispatched.

1. `NestedThrowsReturningToFirstTilePushStepStillInterruptTheOldRoute` was RED because A → B → the first step resumed the old tile route. Contact now reports actual new throws from the existing per-resolution guard, independently of the final coordinate. Entry and forced-path callers stop immediately; interrupted attack paths suppress deferred arrivals, and swap arrivals respect fresh settlement markers.
2. `LethalObjectDuringRedeploymentEndsWithoutCombatEntered` was RED because an already terminal redeployment emitted `CombatEntered`. The existing terminal short-circuit after redeployment is restored, preserving the non-Tornado object/death contract.
3. `HistoricalPullPathCannotAuthorizeTheSameEntryAndThrowTwice` was RED because a forged duplicate entry/relocation pair reused historical pull placement. Evidence now validates the pull start, consumes path steps in forward order once, and discards the history when an actual placement/movement/relocation supersedes it. A real Buffer regression also checks rejection before installation and successful publication of the original proposal. Pull retains its existing single `CombatPullEvent` publication, avoiding duplicate individual movement events.

Two review minors are deferred: duplicate identical `TornadoObserver` assignment in `CommandAttackProjection`, and the default `stage` diagnostic context rather than a specific stage identifier in production preparation errors. Neither changes accepted gameplay. There are no design-policy rulings or scope expansions.

## Modification, understanding and verification scope

Before this cutover, Tornado scheduling and ordinary contact relied on legacy terrain paths; callers could bypass the new grouped rule and queries could describe obsolete one-cell data. After the cutover, Tornado policy remains in its calculator, preparation, geometry and focused evidence validator. Shared callers only invoke contact and respect interruption; composition supplies rule order/dependencies. The existing Framework, Buffer, Job, movement settlement and RNG remain the owners of their original responsibilities.

Changing Tornado travel/contact policy requires its feature contracts and implementation, without reading unrelated gimmick internals. Wiring a new caller still requires that caller's actual-entry and arrival-order contract. Verification therefore includes forced arrival, swap/undo, shared gimmick composition, statuses, authored DB assets and the full Core/CoreIntegration suites. View changes remain a separate consumer task.
T3 is complete. Stop here for user review. Presentation and PlayMode acceptance remain with the View owner. No commit or push was made.

## Follow-up: consistent registry and entry dependency

2026-11-01. The user explicitly authorized this small Core refactor after T3 review. The existing report records its scope; no separate design or plan was created.

Previously, `Default` registered Railway/Haboob/Rockfall while `WithMovementRules` also registered Tornado. A prepared Tornado passed normal production validation but a direct legacy scheduled call incorrectly reported an unregistered handler. Definition validation also constructed a bound composition with an empty rule list merely to obtain the complete registry.

`StageGimmickComposition.Create` now owns one registration list in Tornado → Railway → Haboob → Rockfall order. Both entry forms use it. `BattlePreparationCalculator` and `Initialize` validate definitions through that same registry. Adding a registration therefore changes one list, without creating separate supported-type sets.

The public overload signatures and existing ruleless Railway/Haboob/Rockfall behavior remain compatible:

- `TryAdvance(input)` retains the existing Railway-only boundary contract.
- `TryAdvance(input, boundary)` recognizes every registered definition. Tornado execution requires movement-rule bindings and reports an `InvalidOperationException` naming `IMovementTileRule` before calculating its changes.
- `TryAdvance(input, boundary, rules)` is the normal rule-bound execution path. Explicit null remains an `ArgumentNullException`. An explicitly supplied empty list retains its existing meaning; active terrain/object rules must still be supplied by the caller.

An unbound Tornado handler is sufficient for registration/definition validation; the handler checks its execution dependency locally. The normal Factory-owned arrival-rule list stays authoritative. No fallback list, store, service or interface was added. The change touches four production files and three focused tests in the existing progression fixture. Travel/contact policy, public state, allocation, RNG, event evidence and other gimmick internals are unchanged.

Verification: `Library/TornadoEntryContract/red.xml` observed 8/10 passing, with the intended two failures for inconsistent registry and misleading missing-handler error. `green.xml` passed 96/96 across common progression/composition, Tornado preparation/resolution/runtime integration and Railway/Haboob/Rockfall fixtures. Final `full.xml` / `.log`: **Core 4,524/4,524; CoreIntegration 628/629; DataBaseAssetTests 9/9; no skipped tests**. The sole failure is the same unchanged CameraOffset namespace/type lookup documented above. Independent read-only review of the four-file delta and three regressions found no issues. `git diff --check` passed. No commit or push was made.

## Cross-gimmick consistency audit

2026-11-01. The user requested verification of the other gimmicks after the registry follow-up. No product code was changed for this audit.

The four non-None `StageGimmickKey` values all appear in the single `StageGimmickComposition.Create` registration list. Production has one calculator entry call: `CombatTurnCalculator.ResolveEndGimmick` passes the accepted completed-side boundary and movement rules. Player commands reach it through `EndPlayerTurn`; automatic enemy processing reaches it through `EnemyAiTurnService` → `EndEnemyPhase`. Both use `CombatCommandCalculator` and the same Factory-supplied movement-rule list. Each rule returns the existing transition shape; common composition projects previous results, preserves turn facts, runs registration order and stops at terminal outcome. The shared publisher/battle transaction validates and installs the result.

| Gimmick | Preparation and timing | Entry and publication |
|---|---|---|
| Railway | Authored rail cells and first/repeat settings; derives due time from the shared completed-side count without a separate scheduled-state entry | Same rule/composition/turn boundary/transaction. The legacy boundary-free direct calculator overload is retained only for Railway. |
| Haboob | Initial area and warning deadline prepared through `Initialize`; warning/relocation uses its stored state and shared environment RNG | Same production entry and transaction; custom SO, relocation, read-only queries and stale/forged publication tested. |
| Rockfall | Initial cooldown prepared through `Initialize`; warning/impact uses stored warned cells and shared environment RNG | Same production entry and transaction; damage/bind, omitted due effects, stale state and terminal behavior tested. |
| Tornado | Member centers/deadlines prepared through `Initialize`; same shared cursor, with actual contact requiring injected movement rules | Same scheduled production entry and transaction; the additional actual-cell contact path reuses movement settlement. |

No registration or production-entry inconsistency was found. The different persistent facts and dependencies reflect the existing feature contracts. Preparation still has explicit per-type initialization and state-validation branches; adding a new gimmick also requires its DB/preparation/state wiring. The single execution registry does not make those distinct responsibilities automatic.

Fresh Unity EditMode verification: **265/265 passed, 0 skipped** (Core 202/202; CoreIntegration 63/63). Included Railway 25/25, Haboob rule/targeting/publication 6/6 + 9/9 + 5/5, Rockfall rule/effect/publication 8/8 + 10/10 + 11/11, common progression/composition/state-publication 10/10 + 3/3 + 3/3, authored gimmick/DB checks, Tornado regressions and complete event evidence coverage. Artifacts: `Library/StageGimmickConsistencyAudit/results.xml` and `editor.log`. This targeted run does not include the previously documented CameraOffset test. No full-suite rerun was needed after this read-only product audit; the preceding registry follow-up already records the full-suite result.
