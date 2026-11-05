# Ballista Piercing Shot: View and SO Integration Handoff

Date: 2026-10-11
Status: Core implementation complete through Tasks 1-3; Task 4 acceptance and authorized SO migration are recorded below. View/playback integration remains pending.

Design: Core-only design (Git history: `2026-10-11-ballista-piercing-shot-design.md`).

Core plan: Core implementation plan (Git history: `2026-10-11-ballista-piercing-shot-implementation-plan.md`).

## 1. Ownership and purpose

The original implementation was limited to Core. On 2026-10-11 the user additionally authorized the Ballista SO migration during Task 4. This document records that migration and the remaining View/integration work; it does not authorize presentation, scene, prefab, or runtime forwarding changes.

| Owner | Deliverable |
| --- | --- |
| Core | Single-aim admission, one ordered piercing route, falloff and damage, preview/AI parity, selected identity, immutable delivery records, and Core test evidence. |
| SO/database owner | Ballista's targeting-method configuration, explicit piercing cap, safe asset migration, and validation through the real database adapter. |
| View/integration owner | Delivery-record forwarding if necessary, aim/path feedback, one projectile with ordered impacts, lifetime/cancellation handling, UI/log correctness, and integrated tests. |

The API names below are implemented by commits 69686602, 7a8c7e6d, and ee2b909f. Read the [Core verification record](2026-10-11-ballista-piercing-shot-core-verification.md) for acceptance evidence and remaining integration limits.

## 2. Target player-visible behavior

```text
Ballista at O -> A -> B (selected) -> C
                1    2              3
```

The player selects B's cell from the eight-direction line range. A is hit first if it is an eligible target before B. The shot continues through B and beyond B until Core says it stops. Selecting B does not give it an extra base hit, priority in encounter order, or immunity to preceding falloff.

With common damage 10 and reduction 30, pre-target damage is 10, 7, 4, 1. Actual final damage may differ because of existing category, distance, defense, or incoming effects. Render the final damage facts from Core, not these example numbers as a local formula.

Without Piercing Shot, the same Single method attacks only the selected target. The new behavior must not be implemented as a View-only projectile whose collisions independently choose targets.

## 3. Activation order and known incompatibility

1. Completed: Core implements the linked plan and supplies test evidence and final public contract names.
2. Completed: Ballista content is migrated and validated through actual database loading against this Core version.
3. The View/integration owner consumes the new facts and verifies external forwarding and presentation.
4. Run the combined acceptance matrix in section 8 before declaring the gameplay feature integrated.

The production SO has been migrated to `EightDirectionLine + Single + None`, with explicit cap 2147483647. Pre-migration loaded-asset tests reproduced both failures: Direction profile rejection and an actual deserialized cap of 0. Other content that still constructs Direction-piercing profiles is unsupported; no conversion or fallback was added.

Do not silently coerce Direction to Single, retain a hidden legacy three-ray branch, reinterpret a zero cap as unlimited, or alter Core eligibility to hide incomplete integration. Coordinate the changes before activating the new Core in a playable production build. Core-only completion and end-to-end game readiness are distinct milestones.

## 4. SO/database owner: exact content changes

### 4.1 Targeting method

Current files:

- `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Unit_Ballista.asset`
- `Assets/ProjectL/DataBaseSO/InGameGroup/Targeting/Item/TargetingMethod_3.asset`
- `Assets/ProjectL/DataBaseSO/InGameGroup/Targeting/TargetingMethodDataTable.asset`

Method 3 GUID remains `fed398bfcedb40f6912dd5d8d61ed661`. The Task 4 reference scan found only Ballista and the targeting table; the existing asset was reused.

| Method field | Inspected value | Required target value |
| --- | --- | --- |
| `methodKey` | 3 | Keep 3 if still safe to reuse this asset. |
| `shape` | 3 / EightDirectionLine | 3 / EightDirectionLine |
| `choiceMode` | 2 / Direction | 1 / Single |
| `splash` | 0 / None | 0 / None |
| `splashPercent` | 0 | 0 |

- [x] Re-scan Method 3 references before editing. If it has gained other consumers that need three-ray Direction behavior, create/register a distinct method asset with a verified unused key and change only Ballista's reference.
- [x] Apply the Single configuration without renaming shape enum members or redefining shared `EightDirectionLine + Direction` geometry.
- [x] Preserve existing GUIDs and table membership when modifying an existing asset. If a new asset is necessary, include its metadata and table registration.

### 4.2 Piercing trait and stats

Keep the existing serialized `PiercingFalloffTraitAuthoring` class and trait key. "Piercing Shot" describes the gameplay behavior; it does not require a new trait ID, type rename, or additional delivery-mode setting.

| Field | Required value/action |
| --- | --- |
| `reductionPercentPerTarget` | Keep 30. |
| `maxTargetsPerRay` | Explicitly serialize 2147483647 (`int.MaxValue`); normal termination then follows the existing positive-damage budget. |
| `attackPower` | Preserve 10. |
| `attackRange` | Preserve 99; travel range is measured from the attacker. |
| `attackCost` | Preserve 4. |
| `moveCost` | Preserve 3. |
| Other unit stats, identity, eligibility, category | Preserve existing values. |

The original asset omitted `maxTargetsPerRay`; Unity loaded it as 0, confirmed by the RED test. The migrated asset explicitly contains 2147483647. The authoring default and meaning of explicit zero remain unchanged.

- [x] Author the explicit cap while preserving the managed-reference ID and trait class name.
- [x] Verify through the loaded database and `BattleDefinitionAdapter.ReadAttackProfiles` that the resulting Core profile is Single/no-splash with reduction 30 and the explicit cap.
- [x] Reconfirm that `cannonAreaType`, `cannonAreaRange`, and `pierceRayCount` are not consumed by current authoring code, then remove those obsolete Ballista YAML fields if appropriate. `pierceRayCount: 3` is not the current attack-ray control. Completed in the user-authorized migration.
- [x] Add/update loaded-asset coverage in `Assets/Tests/EditMode/Framework/DataBaseAssetTests.cs` and `BattleDefinitionAdapterTests.cs`, or a focused `BallistaPiercingShotAssetTests.cs`. Do not substitute a YAML-text assertion for a loaded profile assertion.

## 5. Core output contract for integration owners

These public members are implemented and covered by Core tests. Consumers should use these facts without adding a parallel View-owned damage model.

| Fact | Intended meaning |
| --- | --- |
| `AttackAim.ForCell(B)` | User's selected aim; do not replace it with a direction command. |
| `AttackResult/AttackPreview.PrimaryTargetId` | Originally selected eligible entity. It can be absent from Hits/Targets if the shot stopped early. Empty for a category-permitted empty aim. |
| `TargetCell` / `PrimaryCell` | Selected cell, even if it lies beyond the delivered endpoint. |
| `DesignatedCells` | Original method geometry: only the selected cell for this method. |
| `AdditionalCells` | Authored splash geometry; empty here. Never use it to carry the piercing route. |
| `AffectedCells` | Delivered traveled cells, truncated at the effective endpoint. It need not include selected B. |
| `Hits` / preview `Targets` | Actual damage facts. Their existing entity-ID order is not projectile travel order. |
| `AttackTargetRole.Primary` | A hit on the selected entity, if that entity is actually hit; its damage may already have fallen off. |
| `AttackTargetRole.Effect = 4` | A trait-delivered hit on another entity. Existing enum values remain unchanged. |
| `AppliedSplashPercent` | 100 for Primary/Effect hits; not the piercing falloff percentage. Do not multiply damage by the method's zero splash percent. |
| Optional `PiercingShot` | Immutable ordered delivery metadata on result and preview; null for ordinary attacks. |

Implemented `PiercingShotDelivery` properties:

- `Origin`: effect origin for attribution.
- `OriginCell`, `AimCell`, `Step`: attacker origin, selected aim, and the exact signed eight-way step.
- `TraveledCells`: cells in travel order, not Y/X sort order.
- `Encounters`: ordered `PiercingShotEncounter` records with `TargetId`, `EncounterIndex`, `Cell`, and `RemainingPercent`.
- `Endpoint`, `StopReason`: where and why delivery terminates. Reasons are TargetBudget, Wall, Structure, and BoardEdge. (Range was removed on 2026-10-20: travel no longer stops at the attacker's range, see [data boundary handoff §5.49](2026-09-24-view-core-data-boundary-handoff.md).)

Use the existing hit/preview record for final damage and health; the delivery record supplies order and spatial facts. `Rotation4 AttackDirection` remains the existing four-way facing/armor convention and is not accurate enough for diagonal projectile direction. Existing `RayHits` is not a substitute: it was designed around multiple rays hitting one entity and may be empty for a one-ray shot.

### 5.1 Concrete consumer examples

Use O=(1,3), A/ID9=(3,3), B/ID2=(5,3), C/ID7=(6,3), effective range 6, no blockers, and no target-specific damage modifiers.

| Input | Selected identity | Actual encounters | Display behavior |
| --- | --- | --- | --- |
| Aim B, ordinary attack | B / (5,3) | B only | No piercing metadata; ordinary presentation. |
| Aim B, piercing damage 10, cap unlimited | B / (5,3) | IDs 9,2,7 with damage 10,7,4 | One projectile reaches A first, then B, then C; continues to the recorded endpoint. |
| Aim B, piercing cap 1 | B / (5,3) | ID9 only | B stays selected; endpoint is A; no invented B hit/damage popup. |
| Legal empty aim, eligible route targets | Empty ID / selected cell | Core-supplied encounters | Keep the cell marker; every hit has Effect role. |
| Legal empty aim, no route targets | Empty ID / selected cell | None | Normal action cost; render only the recorded travel if nonempty. |
| Legal aim, zero cap or zero common damage | Selected ID/cell retained | None | Empty damaging route ending at origin; do not fabricate a projectile hit. |

An encounter whose final damage is reduced to zero by the target's defense is still a real encounter. It is different from an omitted encounter beyond the piercing budget. Read the record rather than infer stopping behavior from `DamageDealt == 0`.

## 6. View/integration owner: required work

### 6.1 Forwarding and input/preview

Relevant existing files:

- `Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Combat/CombatStateBuffer.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Combat/Jobs/CombatCommandJob.cs`, `CombatPreviewJob.cs`, and `CombatAdvanceJob.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/AttackTargetingInteraction.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs`
- `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/CombatLogUI.cs`

- [ ] Verify that command/preview publication retains `PiercingShot` and selected-but-unhit identities. Change forwarding only if an actual copy/adapter drops them; existing immutable forwarding may already suffice.
- [ ] Keep Core-supplied cell aims for hover/submit. No View-side snapping into a direction group or target selection based on projectile collisions.
- [ ] Display the selected/hovered cell separately from `AffectedCells`, including when B lies beyond the endpoint. Affected-route highlighting must not expand legal aim geometry.
- [ ] Display damage only for actual preview targets; do not assume `PrimaryTargetId` is present in the target list.
- [ ] Preserve request-number guards, stale-preview rejection, AP availability, pointer/keyboard behavior, and cleanup on mode exit/focus loss/rebind.
- [ ] Audit logs and role switches for the new Effect value and for assumptions that primary means first-hit or guaranteed-hit. Follow `Assets/AGENTS.md` input conventions.

### 6.2 Projectile and impact playback

Relevant existing files under `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`:

- `CombatSceneEventPresenter.cs`, `OrderedCombatPresentation.cs`, `CombatSceneRoot.cs`, `CombatSceneDisplay.cs`
- `Display/FieldGridMapper.cs`, `Display/FieldUnitPresenter.cs`, `Display/FieldUnitView.cs`

The inspected scene presenter interpolates movement but completes other events immediately. Its Tick currently returns when no movement is active. Simply adding metadata to the attack result will not make a projectile move.

- [ ] Add the smallest dedicated playback adapter for the piercing record. It owns one presentation projectile, an elapsed-time cursor, ordered impact callbacks, and cancellation; it does not become a new combat simulator.
- [ ] Convert `OriginCell`, ordered cells, exact step, and endpoint through `FieldGridMapper`. Use mapped vectors for diagonal orientation; do not derive it from Rotation4 or sort cells by coordinates.
- [ ] Resolve each encounter's final damage/health by ID from the result. A frame crossing several encounter points must emit every impact once and in recorded order, regardless of entity-ID order.
- [ ] Wait to complete the `CombatAttackEvent` until recorded travel/impacts finish. Extend ticking so a shot progresses even when `_moving == null`; preserve existing event queue/Flow ownership.
- [ ] Show each impact without mutating Core/buffer health or double-applying damage. If using temporary HP display overrides, reconcile them with later authoritative event snapshots and avoid early pool return followed by stale access on a visually lethal hit.
- [ ] Cancel/release/rebind clears the projectile, temporary cues, and callbacks. Guard against late completion, duplicate callbacks, and reentrant cancellation. A missing/dead target view must not leave playback waiting forever.
- [ ] Complete an empty zero-budget route once without inventing a projectile segment. Distinguish this from a nonempty route with no encounters.
- [ ] Wire the chosen prefab/root/timing through explicit scene references. Scene/prefab/art and timing choices belong to the View owner and are not new Core settings. Reuse an appropriate bolt mesh; do not give its colliders or physics damage authority.

## 7. Existing rules that integration must preserve

| Rule | Integration implication |
| --- | --- |
| 30 percentage-point falloff per eligible encounter | No View-side recalculation and no multiplicative decay. |
| Empty/dead/ineligible cells do not consume encounters | Follow Core order rather than counting occupied visual objects. |
| Cannon category permits empty/friendly/neutral aims | Do not impose an enemy-only/occupied-only input rule. |
| Cannon excludes adjacent aim/encounter cells | A physically nearer adjacent unit is not automatically a hit; use an eligible A in demo scenes. Walls in those cells are still checked. |
| Target-specific final mitigation does not set the piercing budget | A zero-damage impact can still be followed by later impacts. |
| Wall before selected B invalidates ordinary aim | Do not allow a substitute shot at A just because A is reachable. |
| Live physical structures terminate the shot, even if ineligible or destroyed by it | Do not animate continuing through them beyond the recorded endpoint. |
| Actor-origin range, board limit, and budget endpoint | Selecting a farther B does not grant more range or reset falloff. |
| Shared Direction geometry remains three-ray | Only Ballista's authored configuration changes; no blanket geometry redefinition. |

## 8. External validation checklist

The following is external acceptance, not a prerequisite to marking the isolated Core milestone complete. Full feature integration requires it.

- [x] Loaded SO/catalog/adapter produces Single/no-splash Ballista with explicit cap 2147483647 and unchanged stats. Legacy Direction-piercing content is migrated or diagnosed, not silently accepted.
- [ ] Framework command/preview publication preserves immutable metadata, Effect hits, unhit B, and empty selected identity without partial state publication.
- [ ] Cell input and preview tests cover B outside the delivered endpoint, late response rejection, focus loss, and mode exit.
- [ ] Scene/playback tests cover A/B/C travel order with reversed entity IDs, all eight directions, rotated/non-square field mapping, and a large delta crossing multiple impacts.
- [ ] Cancel/release/rebind tests verify no remaining projectile, stale cue, late callback, or premature result acknowledgement/Flow advance.
- [ ] Actual Combat scene playthrough shows one projectile, ordered impacts, correct selected marker and endpoint, AP charged once, and no side rays. Repeat with budget exhaustion, a legal empty aim, walls, and structures.
- [ ] Removing the trait in a temporary integration fixture restores B-only damage with identical targeting; restore temporary content after testing.
- [ ] Record actual commands, test counts, compile output, and manual observations. Skipped scene tests are not visual acceptance.

Suggested existing test surfaces are `Assets/Tests/EditMode/Framework/{DataBaseAssetTests,BattleDefinitionAdapterTests,CombatCommandPublicationTests,CombatPreviewPublicationTests,WallAttackInteractionTests,CombatSceneEventPresenterTests,CombatPresentationCleanupTests,CombatViewSystemTests}.cs`. Add focused fixtures where these do not express the new cases. View/publication fixtures remain future owner work. Only the focused loaded-asset tests in DataBaseAssetTests were added for the authorized SO migration.

## 9. Handoff completion record

Core implementation commits: 69686602 (contracts), 7a8c7e6d (single trace), ee2b909f (command/preview/AI). Task 4 adds acceptance evidence; SO migration commit is 874a7adc. Unity 6000.0.73f1 passed all 1871 Core tests and 87 focused loaded-asset/adapter tests. See the [verification record](2026-10-11-ballista-piercing-shot-core-verification.md).

The loaded SO/catalog/profile tests verify the content migration. View owners must still record forwarding, input/preview, projectile playback, lifecycle, and scene acceptance outcomes. Passing Core and database tests does not establish visual playback or full game readiness.
