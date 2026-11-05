# Tornado Stage Gimmick — T2 Execution Report

2026-11-01 · Core resolution, landing and ordered evidence

## Scope

The user authorized T2 after [T1 completion](2026-11-01-tornado-stage-gimmick-t1-report.md). The [implementation plan](../plans/2026-11-01-tornado-stage-gimmick-implementation-plan.md) and [Core design](../specs/2026-11-01-tornado-stage-gimmick-design.md) remain the requirements. HEAD is still `c3daac1fcef74457cb92b2460e1628ddac9cc267` on `feature/Cheon/Core/NewCombatSytem`. T1 changes and the unrelated FMOD asset change were preserved. No commit or push was made.

T2 exercises the real new rule and contact calculator on prepared facts. It adds the nested-arrival seam only while an explicit Tornado resolution is active. Default rule registration, production initial preparation, ordinary root-entry callers, enemy-end order and preview migration remain T3. The named default asset remains serialized at 3/3.

## Resolution and state ownership

| Owning code | Implemented behavior |
|---|---|
| `Application/Combat/StageGimmicks/TornadoGimmickRule` | Keeps `IStageGimmickRule.TryAdvance` and binds the supplied validated movement-rule list. Due members run in source-ID order against one movement projection. Warning uses T1 direction selection; travel uses its locked direction and actual reached centers. A terminal landing stops later centers/sources and keeps the existing deadline rather than reserving a new cycle. |
| `Application/Combat/TornadoCalculator.ResolveGroupedEntry` | Uses ongoing `StagePhase.Combat` for contact eligibility. Deployment, preparation and idle reads do not create contacts. An actual entrant is evaluated against the projected current center. Entry leaves schedule facts unchanged. Legacy methods remain isolated for T3's atomic cutover. |
| `TornadoCalculator.ResolveSweepCenter` | Enumerates unique living field characters intersecting the footprint, ordered by entity ID. Rechecks each against the current projection before throwing. |
| `TornadoCalculator.GroupedLandingCandidates` | Uses shared `MovementRouteValidator.CanOccupy` with the thrown unit excluded, removes its departure cell and enumerates row-major cells. Walls, cliffs, living units and structures cannot be initial landings. |
| `TornadoContactResolution` | Resolution-local `(source ID, entity ID)` guard and current center-step index. It survives A → B → A nested landing chains and is discarded after the sweep or independent entry. Successful throws are marked before running landing effects. |
| `MovementProjection` | Holds current projected `Gimmicks` beside existing projected character/environment facts, while `Input.State` remains the immutable expected state. Records correlated forced moves and continues using the existing environment RNG cursor and transaction completion. |
| Existing forced-arrival pipeline | Handles weak-ground/fall entry, position normalization, tile/object effects, souls and death/outcome settlement. Outer callers recheck the actual anchor before deferred settlement. A nested tile push into another storm interrupts its remaining steps. |

Forced-movement immunity precedes unsupported multi-cell displacement. No legal destination and immunity consume no destination draw. Movable multi-cell facts retain the explicit unsupported boundary; T2 does not invent whole-footprint placement. A zero-step sweep checks its one current footprint.

No new service, Buffer store, Job, RNG algorithm or persistent character status was added. The mutable facts are command-local projections; the existing transaction owns publication.

## Event and integrity contracts

New names are under `CoreCaculator/Contracts/Combat`:

| Member | Meaning |
|---|---|
| `CombatEventKind.TornadoAdvanced` (44), `CombatTornadoGimmickEvent` | One expected/final grouped-state change for a completed-side boundary. `Advances` contains the source-ordered member records actually executed. |
| `TornadoMemberAdvance` | Immutable `Expected`, `Final`, `SideLength`, actual `Path`, and derived `IsTravel`. A warning has only the current center; a travel path includes departure and every reached center. |
| `CombatEventKind.TornadoCenterReached` (45), `CombatTornadoCenterEvent` | Interleaved actual center marker before contacts at that center. No unperformed center is emitted after terminal contact. |
| `CombatEventKind.TornadoEntryStarted` (46), `CombatTornadoEntryEvent` | Starts one independent actual-entry chain without changing storm scheduling. Nested landings share its guard rather than starting another chain. |
| `CombatEventKind.TornadoMemberAdvanced` (47), `CombatTornadoMemberEvent` | Source completion after its actual centers and landing settlement. Matches the group summary and makes warning completion observable in order, including terminal-at-endpoint checks. |
| `CombatCharacterEvent.TornadoContact` / `TornadoContactOrigin` | Carries `GimmickId`, stable `SourceId`, actual `Center`, and `StepIndex`. A sweep starts at 0; stationary entry uses -1. Existing `TornadoSourceId` remains the same source ID on the actual `Relocated` event. |

`CombatChangeEvidenceReader` recognizes every new variant. `TornadoChangeEvidenceValidator` checks prepared identity, one grouped update, a preceding accepted side-end increment, due-source prefixes, immutable expected/final member correspondence, legal diagonal path prefixes, deadlines and horizontal successors, center markers, relocation origin/step correlation, per-chain throw uniqueness and installed character position/health correspondence. It does not select victims, enumerate landing candidates or rerun RNG. The shared pure geometry owns containment, travel and inward horizontal reflection.

The ordinary `CombatStateBuffer.PrepareCommand` invokes these integrity checks before installation. Stale state/RNG and forged evidence therefore use the existing transaction rejection path. A previously handled command number uses the existing idempotent no-op behavior; the retry test verifies unchanged stores, result sequence and command facts.

## Verification evidence

Unity 6000.0.73f1 ran on the AppData-authorized path. XML and logs are retained under ignored `Library/TornadoT2/`.

| Run | Result | Artifacts |
|---|---|---|
| Contact/rule RED | 0/11 passed against unimplemented methods | `resolution-red.xml`, `.log` |
| First contact/rule GREEN | 11/11 passed | `resolution-green.xml`, `.log` |
| Expanded T1/T2 rule and evidence GREEN | 49/49 passed | `evidence-green.xml`, `.log` |
| Horizontal-successor integrity RED | 7/8 passed; wrong horizontal sign was accepted | `direction-red.xml`, `.log` |
| Whole event-family coverage RED | 42/43 passed; new samples missing | `coverage-red.xml`, `.log` |
| Targeted gate | 100/100 passed | `gate.xml`, `.log` |
| Pre-review full run | Core 4,522/4,522; CoreIntegration 621/622 | `full.xml`, `.log` |
| Final full Core | **4,527 passed, 0 failed, 0 skipped** | `final-full.xml`, `.log` |
| Final full CoreIntegration | **621 passed, 1 failed, 0 skipped** (622 total) | `final-full.xml`, `.log` |
| Dedicated T2 fixtures in final run | **37/37 passed** (18 resolution, 14 evidence, 5 publication) | `final-full.xml` |
| Complete event coverage / DB assets | **47/47 and 9/9 passed** | `final-full.xml` |

The only full-suite failure is unchanged from T1: `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", false)`, a `TypeLoadException` from the installed package/type namespace mismatch. Its source and package files are unchanged; no camera or package fix was included. The final run has no C# compile errors. `git diff --check` passed, new C# files have Unity metadata, and the named default asset remains unchanged.

Independent read-only review found two Important issues and no Critical/Minor issues. Both entered one fix pass:

1. A historical `ArrivalCells` marker could suppress a later real landing at the same anchor. `ReturningToHistoricalArrivalAnchorCollectsNewlyAvailableSoul` failed with no soul growth, then passed after invalidating the old marker on each real relocation. A nested landing's newly written marker is still reused, preventing duplicate outer settlement.
2. Endpoint-fatal group metadata could claim a normal next deadline or a later source warning. Both `FatalEndpointCannotReserveNextCycleOrInventALaterWarning` cases failed before the fix. The new per-source completion record correlates the actual fatal event position, terminal member deadline and final executed-source prefix; warnings cannot exist only in a group summary.

The post-review targeted regression passed **105/105** before the additional soul-publication correspondence assertion. That assertion then failed because typed soul character changes were missing from event replay; replay now includes `CombatSoulEvent.Characters`. A final ongoing-combat check was also observed RED for a `Finished` input whose character facts still describe living units. Both pass in the final full run. The review's two Important findings are fixed and regression-tested; no deferred review minors remain. T3 exclusions were retained as phase obligations, not treated as T2 implementation omissions.

The gate includes new rule/evidence/real-Buffer tests, `MovementEnvironmentTests` and complete event-type/kind scope coverage. A stale-RNG case was added after the 100-test gate and is included in the full run. Existing integration test setup was aligned with the actual initial-preparation contract and current battle-instance definition reader; product preparation and lifetime policy were not changed.

Tests cover origin/intermediate/endpoint contact, stationary entry, stable victim/source ordering, landing inside the same footprint and ahead of the sweep, A → B → A termination, later independent re-entry, freed/occupied cells, cliffs/walls, no destination, immunity and the multi-cell support guard. An actual custom tile rule drives A throw → tile push → B throw; it verifies interruption and the final settled anchor. A lethal mine landing at the first intermediate center verifies the actual terminal path prefix, one outcome and no later source. Mutations exercise missing/reordered centers, missing relocation origin, duplicate throw, wrong source, stale group, wrong deadline/horizontal sign and a center invented after fatal contact. Real Buffer tests exercise successful explicit contact publication, stale expected group/RNG, forged final group, missing origin and idempotent retries.

## Locality and next phase

The feature's victim/landing/sweep policy lives in Tornado calculation and its focused tests. Geometry changes use one pure helper. Shared movement code only exposes current gimmick facts and conditionally calls the feature during an already active nested resolution. Shared evidence dispatch recognizes the typed family and delegates detailed integrity to the Tornado validator. Other gimmick internals were not changed.

T3 still owns the real production cutover, caller-wide forced arrival hooks, combined initialization RNG ordering, authored-ID reservation, default asset migration, enemy-end retention, observer-filtered preview/freshness, undo integration and normal service/Buffer acceptance. T2's explicit test invocation is not a claim that those normal production paths already use the new feature. Presentation consumers still use the legacy runtime event shape until that coordinated migration.

Stop after T2 for user review.
