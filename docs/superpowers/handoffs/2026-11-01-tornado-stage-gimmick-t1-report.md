# Tornado Stage Gimmick — T1 Execution Report

2026-11-01 · Prepared data and geometry only

## Scope and baseline

User authorization covers Phase T1 of the [implementation plan](../plans/2026-11-01-tornado-stage-gimmick-implementation-plan.md), against the [approved Core design](../specs/2026-11-01-tornado-stage-gimmick-design.md).

- Checkout: `feature/Cheon/Core/NewCombatSytem`, HEAD `c3daac1fcef74457cb92b2460e1628ddac9cc267`.
- Pre-existing changes: `Assets/Audio/FMODplus/New Fmod Event Catalog.asset`; untracked plan and spec. These were preserved. No commit or push was made.
- Pre-change EditMode regression: **64/64 passed**, using `InitialTornadoPlacementTests`, `TornadoEnvironmentTests`, `StageGimmickProgressionTests`, `StageGimmickAuthoringTests`, `StageGimmickStatePublicationTests`, `StageGimmickCompositionTests`, and `DataBaseAssetTests`.
- Production still uses legacy Tornado preparation, contact, scheduling, events and preview. T1 does not register a Tornado runtime rule or install the new grouped state.

## Authoring inventory

The inventory searched serialized assets under `Assets`, the existing terrain/SO conversion route, the development battle source, and Core/CoreIntegration Tornado fixtures.

| Input | Observed state before implementation | T1 treatment |
|---|---|---|
| Named default terrain definition | `Assets/ProjectL/DataBaseSO/InGameGroup/Terrain/Item/Terrain_Tornado.asset`, GUID `9bd89c6552dc4b5d9bfe6e1533b80ff4`, kind Tornado, serialized first/repeat **3/3** | Preserved byte content. Deliberate default-asset migration remains T3. |
| Other Tornado terrain-definition assets | No other assets using the terrain-definition script with kind Tornado were found | No content conversion needed in T1. Other unrelated serialized `kind: 6` values are not terrain definitions. |
| References to the named default asset | No references from serialized `.asset` files were found | No production authored source IDs, fixed centers, custom profiles or visibility values to correct. No invalid production center was found. |
| Existing fixed authoring | `CombatStageEnvironmentInput.TerrainPlacementInput`: definition, source ID, cell, known owners | Center, source ID and visibility are retained in the new member spec. A per-placement horizontal sign resolves interior centers. |
| Existing random authoring | The same placement input with `randomizeInitialPosition` | New member has no initial center; preparation samples a fitting left/right edge and row, in source-ID order. |
| Development battle fixture | No authored Tornado in `ViewSystem/Dev` | Unchanged. |
| Existing test/fixture inputs | Fixed examples include source 91 at `(1,0)`, sources 31/32, and random sources 301/302. Existing tests also exercise custom first/repeat **2/5** and **1/1**, hidden visibility and narrow boards | Legacy fixtures remain on their existing path until T3. Centers such as `(1,0)` and `(0,0)` cannot fit a new 3×3 square; they are intentionally not moved or silently repaired. New preparation rejects them with stage/source context. |

The SO keeps the existing first/repeat serialized fields as the single interval authoring source. New SO instances start at 2/2. New `tornadoSideLength`, `tornadoDiagonalDistance` and `tornadoWarningLeadControllerTurns` fields default to 3/3/1. Existing serialized custom intervals remain authoritative. No `.asset` file was edited.

## Implemented contracts

Paths in this section are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`.

| Contract / implementation | Meaning |
|---|---|
| `Contracts/Common/StageGimmickKey.Tornado` | Numeric key 40; existing keys and definition indices remain unchanged. |
| `Contracts/Commands/StageGimmicks/TornadoMemberSpec` | Immutable source ID, copied/sorted `KnownTo`, nullable `InitialCenter`, nullable `InitialHorizontalSign`, `SideLength`, `DiagonalDistance`, `FirstActivationAfterControllerTurns`, `RepeatIntervalControllerTurns`, `WarningLeadControllerTurns`. New contract defaults: 3, 3, 2, 2, 1. |
| `TornadoGimmickSpec.Members` | Nonempty, immutable, unique-source collection sorted by source ID. One definition represents all authored storms. |
| `Contracts/Combat/TornadoMemberState` | Immutable `SourceId`, `Center`, `HorizontalSign`, `Phase`, `NextBoundaryCount`, nullable `LockedDirection`. Cooldown has no locked direction; Warning has a diagonal or explicit zero direction for stationary activation. |
| `TornadoGimmickState.Members` | Immutable, source-ordered member state. Group `NextBoundaryCount` is derived as the minimum member deadline. `SameValues` includes every member fact. |
| `StageGimmickStateSet.ValidateDefinitions` | Matches group/source identity, full-square board containment and locked warning direction, while keeping unknown variants fail-closed. |
| `Contracts/Combat/TornadoGeometry` | Pure `Fits`, row-major `Footprint`, and `Travel`; includes the origin and every reached center. It takes no terrain, unit or structure inputs. |
| `Application/Combat/StageGimmicks/TornadoGimmickPreparationCalculator` | Internal `Prepare`, `Warn`, and `CompleteTravel` primitives using the existing environment RNG cursor. They do not perform contacts, emit events, install Buffer state or execute a scheduled boundary. |

`TerrainDefinitionData.CreateTornadoMember` converts the existing SO tuning. `CombatStageEnvironmentInput.ToData` groups those members once and `CombatStageData` retains the prepared definition as `CombatStageDetail.Tornado`. Legacy fixed/random outputs are still supplied to the unchanged runtime adapter; they are migration inputs, not a second installed storm state. T3 must consume the grouped definition and stop installing corresponding legacy sources atomically.

Preparation validates all fixed centers before drawing and commits its local RNG cursor only on success. Fixed left/right edge starts point inward. Interior centers require a sign. If both horizontal edges coincide, an authored sign is used or a seeded side is chosen. Storms may share centers. A null definition returns null and spends no RNG. A valid stationary square remains valid on a board with no diagonal room.

Warning selects one feasible vertical sign and preserves it through travel. Travel shortens at full-square bounds, preserves a one-center path when stationary, and updates the horizontal sign for the next activation. A warning lead equal to repeat establishes the next warning immediately after travel. These primitives do not resolve a terminal sweep; T2 must publish only its actual reached prefix and must not blindly complete this normal scheduling result after terminal contact.

## Verification

Unity 6000.0.73f1 was launched with AppData-authorized execution. Artifacts are in the ignored `Library/TornadoT1/` directory.

| Run | Result | Artifacts |
|---|---|---|
| Pre-change targeted regression | 64 passed, 0 failed | `baseline.xml`, `baseline.log` |
| New preparation/geometry RED | 22 failed against deliberately unimplemented contracts/calculator | `red.xml`, `red.log` |
| New SO/input RED | 6 failed on missing authoring fields | `authoring-red.xml`, `authoring-red.log` |
| Initial new T1 GREEN | 28 passed, 0 failed | `targeted.xml`, `targeted.log` |
| Invalid warning state RED | 2 failed: validation accepted impossible zero/outward directions | `state-red.xml`, `state-red.log` |
| Final full Core | **4,491 passed, 0 failed, 0 skipped** | `full.xml`, `full.log` |
| Final full CoreIntegration | **616 passed, 1 failed, 0 skipped** (617 total) | `full.xml`, `full.log` |
| New T1 tests in full run | **30 passed, 0 failed** (17 preparation, 7 geometry, 6 authoring) | `full.xml` |
| DB asset tests in full run | **9 passed, 0 failed** | `full.xml` |

The one full-suite failure is the unrelated, unchanged `CombatCameraZoomPoseTests.PostProcessingExtensionIsAllowedButPoseWriterIsRejected("Cinemachine.CinemachineCameraOffset", false)`. Its reflection lookup throws `TypeLoadException` before constructing the test camera: the installed package does not expose that fully qualified type in the queried assembly. The test source and package manifest/lock are unchanged from HEAD. This fixture was not in the 64-test pre-change run; an unchanged-source/package incompatibility is the evidence for classifying it outside T1, rather than a full pre-change suite run. No camera or package changes were made. The full run has no C# compile errors. `BossStagePreparationTests` passed both tests in this checkout.

An independent read-only T1 review found no Critical, Important or Minor issues. It explicitly left T2 contact/arrival/evidence and T3 production/preview/Buffer acceptance to their authorized phases. The author inspected the final diff and checked Unity metadata, pure-contract dependencies, unchanged default asset and whitespace at the T1 stop.

Coverage includes fixed edges, explicit interior sign, seeded starts, stable multi-source ordering, copied visibility, duplicate IDs, invalid sizes/durations/leads, invalid fixed centers, no-storm RNG equality, narrow-board side choice, stationary travel, all diagonal signs, exact `(2,2) → (3,3) → (4,4) → (5,5)` history, one-step shortening, reflection, immediate next warning and a non-default SO profile (5×5 square, distance 2, first/repeat 5/4, lead 2).

## Change locality and remaining work

| Change example | Modification / understanding boundary | Verification boundary |
|---|---|---|
| Tune size/distance/interval | Existing terrain SO → immutable terrain data → member spec; no knowledge of Railway, Haboob or Rockfall algorithms required | Profile validation and preparation/geometry tests; retain DB asset checks |
| Change edge geometry | `TornadoGeometry` and feature-local warning/preparation primitives | Geometry, seeded preparation and state-definition validation |
| Add runtime behavior | T2/T3 use existing gimmick composition, movement projection, environment RNG and Buffer transactions | Contact/arrival/evidence tests in T2; real service/Buffer and shared regressions in T3 |

No new service, registry, state store, RNG implementation, Buffer transaction or Job responsibility was introduced. The shared state validator only gained typed Tornado integrity checks. No View, FieldCamera, Architecture, scene, prefab or presentation code was modified.

T2 remains responsible for combat-only contacts, nested throw guards, real landing settlement and ordered/validated evidence. T3 remains responsible for production cutover, observer-filtered preview, freshness/undo barriers, turn-end ordering, actual Buffer publication and named default-asset migration. Existing View consumers of `CombatTornadoEvent` and `Relocated.TornadoSourceId` remain consumers of the legacy path; the new runtime event/member handoff is not ready in T1.

Stop after T1 for user review.
