# Main Character Initial Deployment — View Handoff

Date: 2026-10-17

Status: Core implementation and focused native Unity acceptance complete. **2026-10-18 update:** combat-rewards R2 supersedes the former hand-size increase: the current default and operational loadout are total 4 (main + up to three subs). See [reward View handoff](2026-10-18-combat-rewards-view-handoff.md). Deck UI/input connection is pending with View. This is a local handoff, not an external delivery or a claim that the View connection is complete.

## 1. Approved behavior

The user is the Core programmer and explicitly authorized the Core changes in this session.

- At the beginning of each battle, the main character is available for manual deployment from Deck instead of being placed on the field automatically.
- The main character is guaranteed; it is not a random sub-deck draw.
- Current default initial sub draw is up to three (2026-10-18 reward-plan update). Draw only the available number of subs when the deck is smaller.
- Preserve the existing night-end policy: the main stays on the field; living deployed subs return to Hand and enemies follow their existing recall/redeployment rules.
- Do not begin combat before the main is placed. Preserve the existing main-placement lock after the first combat entry.

## 2. Core state and commands

The main character is prepared as `CharacterRuntimeArea.Hand`, with `ReserveSlotIndex = 0`, null Anchor/Rotation and no occupied cells. Initial subs use slots 1 through N. Owned identity, persistent health, soul contributions and combat entity allocation remain on the existing Core/Buffer paths.

`DrawOrder` and `DrawCursor` continue to describe subs only. The main consumes a Hand slot but does not advance the sub draw cursor. `ReserveCapacity` must fit the main plus all initially drawn subs; insufficient capacity rejects preparation before publication.

`StartingHandSize` keeps its existing meaning: total initial hand size including the main. The current default and operational loadout are 4, so `min(StartingHandSize - 1, available subs)` produces up to three subs. Custom explicitly configured hand sizes retain their meaning. Owning more subs does not raise this default cap. The original 2026-10-17 increase to 5 in the execution history below was superseded by combat-rewards R2 on 2026-10-18.

Use the existing input contracts:

- Preview: `CombatViewPreviewData.MainCharacterPlacement(cell, rotation)`.
- Commit: `CombatViewCommandData.PlaceMainCharacter(cell, rotation)`.
- Enter combat: the existing `EnterCombat` command, which rejects an unplaced main.

Do not fabricate a placement result, write CharacterBuffer from View, or assign a visual-only field position. `BattleInitialization.MainAnchor/MainRotation` and the corresponding serialized stage authoring fields remain for compatibility, but preparation no longer uses them to place the main.

## 3. View connection points

These inspected restrictions remain unchanged in this Core delivery and must be handled by View.

1. `GlobalSystem/UISystem/Screens/Global/DeckCardList.cs`: `RefreshDeck` currently filters out the main and uses three fixed designed cards. Include the main and provide enough visible cards for the intended Deck contents. Preserve owned-instance identity, card details, hover/pin behavior and existing layout ownership.
2. `ViewSystem/Combat/PlacementDragInteraction.cs`: `FindDeckUnit` currently accepts only live player subs in Hand. Permit the main in Hand, choose the main-placement preview, and call `BeginDrag` with the actual main role instead of `false`.
3. `CombatDeckPlacementInput` and `DeckCardDragBinding` already route Deck requests through the existing interaction and InputSystem paths. Reuse those boundaries; do not introduce UnityEvent listeners or a separate placement state machine.
4. `HandPresenter` already renders living player characters in Hand without a sub-only role filter. The main will therefore also appear in the existing Hand presentation. Keep Deck and Hand as views of the same Buffer identity, not two independently deployable characters.
5. Once placed, the main is no longer selectable from Deck's Hand-only placement path. Existing field relocation remains available before the main-placement lock; returning the main to Hand remains prohibited.

For test inputs that previously started with a preplaced main, submit and process a real `PlaceMainCharacter` command before `EnterCombat`. Do not weaken production admission or silently auto-place the main in the runtime bootstrap.

## 4. Focused acceptance

- Main and all available initial subs render from Buffer; no main field view exists before manual placement.
- Drag the main from Deck, preview a valid deployment cell and release: one main placement command, same OwnedId/EntityId, one field view.
- Cancel or release outside a valid cell: the main remains in Hand, with no duplicate, lost card or persistent-health change.
- Reject wrong deployment zones, occupied cells and blocking terrain through the existing Core preview/command response.
- Enter combat before main placement: rejected; after placement: accepted and main placement locked.
- Verify zero, fewer than three, exactly three and more than three owned subs. Shuffle order and cursor cover subs only.
- Finish a night: main stays placed and living deployed subs return using the existing policy.
- End the battle and enter another: the main is again available for manual deployment with retained persistent health.

## 5. Delivery and verification

### 5.1 Implemented scope

- `BattlePreparationCalculator`: prepare main in Hand slot 0, place initial subs in slots 1..N, and require capacity for the main plus the initial sub draw. Initial enemy, structure, obstacle and allocation rules remain on their existing paths.
- `InitialDrawPlan.DefaultStartingHandSize`: 4 to 5; `PlayerLoadoutDataSO` now references this shared default and explains the cap. `PlayerLoadout_1.asset.startingHandSize`: 4 to 5. The existing sub shuffle/cap calculation remains unchanged.
- Update existing preparation/environment/roster/reentry tests to the new initial state. Field-environment calculator tests explicitly construct Field facts in a test-only helper; this does not reinstate automatic deployment in production.
- Registered-service integration tests prepare combat through actual `PlaceMainCharacter` commands. Entry-result/queued-input preservation is asserted before manual deployment, so the test setup does not mask that contract.
- No production View code, scene, prefab, camera, Architecture, Framework registration or lifecycle implementation was changed. The two existing wall-delivery asset edits and both existing FMOD file edits were preserved byte-for-byte against the session snapshot.

### 5.2 Native verification

All runs used Unity **6000.0.73f1** through approved execution with AppData access. Raw local evidence is under [the task evidence directory](../../../.superpowers/sdd/2026-10-17-main-deck-placement/).

| Scope | Result | Evidence |
|---|---|---|
| Before implementation: Core plus directly affected Framework fixtures, including the first 11 new cases | 3,194 total: 3,182 pass, 12 fail. Nine new cases failed for the intended missing behavior; three failures were existing DB checks. | [RED/baseline XML](../../../.superpowers/sdd/2026-10-17-main-deck-placement/red-baseline.xml) |
| Final complete Core assembly | **3,098 / 3,098 pass** | [Core and direct Framework XML](../../../.superpowers/sdd/2026-10-17-main-deck-placement/green-3.xml) |
| Direct Framework initialization, roster, saved DB and reentry checks | **99 pass / 3 existing failures / 102 total** | Same XML |
| Registered Core service battle cycle and common publication | **45 / 45 pass** | [Integration XML](../../../.superpowers/sdd/2026-10-17-main-deck-placement/integration-3.xml) |

The new pure-Core behavior fixture is **12/12 passing**, including short/empty/large decks, main-inclusive capacity, ignored legacy main coordinates, manual placement admission, placement-time terrain bonuses without healing, and the unchanged night-end policy. The saved-loadout tests prove the production setting caps at the available sub count. A registered-service case proves preview, rejected unplaced combat entry, manual placement publication, stable identity/health and subsequent combat entry.

The three unchanged DB failures are:

1. `DataBaseAssetTests.EveryCombatStage_EnvironmentAndViewRotationsReachOperationalDefinition`: Stage1101 has an extra terrain source213 against that test's expectation.
2. `DataBaseAssetTests.EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`: Stage2101 rail expectation8 versus actual0.
3. `DataBaseAssetTests.LineMaterial_IsRegisteredInCatalog_AndSupportsColorOverride`: the expected `Assets/ProjectL/Script/Render/Shader/Line/LineShader.mat` is absent.

The first broad Core+Framework baseline attempt, before runtime edits, crashed in native rendering at `GfxDevice::DrawSharedGeometryJobs` during a play/render smoke case and produced no final results XML. See [baseline log](../../../.superpowers/sdd/2026-10-17-main-deck-placement/baseline.log). This is distinct from the previous sandbox/AppData `CurlRequestCache.db` startup failure: this run reached project tests. Do not interpret the scoped acceptance above as a completed full Framework or graphical-playback suite. No Unity upgrade or cache deletion was performed.

Earlier test-authoring compilation mistakes were corrected; the accepted XML runs contain no compilation error. Existing CS8632 (`CombatCommandTestData.cs`) and CS0105 (`LobbyDepartureIntegrationTests.cs`) warnings were observed. Manual Game View validation and full Deck interaction acceptance remain with View after section3 is implemented.

### 5.3 Final delivery boundary

Core work is complete for this request. Section3 and the graphical checks in section4 are the remaining View tasks. No external message, staging, commit or push was performed. The user handles commit/push.

## 6. Ownership and change isolation

Core owns initial character state, draw count, placement validity and night-end policy. Buffer remains the single source of runtime identity/location/health. View owns Deck cards, drag feedback and command submission. Existing calculators, publication transactions and input contracts are reused; no new service or interface is required, and Framework registration/lifecycle contracts remain unchanged.

The modification scope is initial preparation and its direct View consumer contract. Combat arithmetic, attack behavior, movement rules, camera and Architecture are outside this change. Shared initial-state assumptions require integration fixture updates, but they do not justify unrelated runtime refactoring.

SOLID review: preparation/draw and UI/input retain separate responsibilities (SRP); existing placement/command extension points are reused (OCP); the main uses the same documented Hand/Field location invariants as other owned actors and existing rejection/publication guarantees remain intact (LSP); no broad new interface is introduced (ISP); View will depend on the existing command/read contracts rather than concrete Core mutation (DIP). FW-001/002/003 registration/build behavior and FW-004 state boundaries are unchanged. The feature adds no Framework module or alternate source of character state.
