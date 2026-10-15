# Wall Attack Diagnostics: Implemented View Changes and Integration Handoff

Date: 2026-10-08

Baseline: `680f309f` plus the uncommitted wall attack implementation.

## Implementation status

The wall diagnostic presentation integration is implemented and remains in the working tree. This handoff records the completed changes, public contracts, and verification results for the View developer. It does not assign a new implementation task. Changes are uncommitted.

Core owns target eligibility, wall geometry, attack legality, damage, and AI feasibility. Buffer publishes immutable Core results. View submits existing aims and renders the supplied evidence. Keep wall intersections, angle-based obstruction, category exceptions, and source-ID lookup out of View.

## Published contract

| Value | Meaning for View |
| --- | --- |
| `AttackRangeResult.RangeCells` | Raw geometric range after existing range rules; not a selectable-cell list |
| `InRangeTargetIds` | Existing eligible identities in the raw range; not proof of wall reachability |
| `AttackAims` | Spatially legal Cell/Direction/All aims after Core admission; affordability is separate |
| `CanAffordAttack` | Core's effective cost check; insufficient AP does not remove geometric range diagnostics |
| Range `BlockedContacts` | Direct obstruction evidence across raw range cells; use even when no legal Single aim exists |
| `AttackPreview.BlockedContacts` | Aim-specific direct/propagation evidence, including partial valid attacks or a rejected preview |
| `CombatResult.BlockedContacts` | Published execution/rejection evidence; not the list of damaged targets |

Each `AttackBlockedContact` contains Origin, Contact, Phase (`Direct = 1`, `Propagation = 2`), WallSourceIds, and WallCells. Lists are defensively copied. IDs are ascending; cells are ordered by Y then X. IDs and cells describe the same complete blocking run, but are independently sorted collections, **not positional ID/cell pairs**. Highlight the supplied WallCells directly. Never turn diagnostic contacts into damage targets.

For legal segments, Core chooses the earliest intersected wall run exactly; ties use the smallest source ID of the run. Contacts on wall-occupied cells report the occupying run. Single aim can fail entirely; Direction/All can remain legal while only some contacts are blocked. Splash and immediate Thorn propagation originate at impact, not the attacker. View must not infer these rules from category names.

AttackAim no longer accepts entity aiming. `None = 0`; retired value 1 remains unused; `Cell = 2`, `Direction = 3`, `All = 4`. Runtime EntityIds still identify actors and victims. Ordinary empty/friendly admission remains enforced by Core; removing ForEntity does not remove ownership checks.

## Implemented interaction and rendering behavior

1. The interaction indexes the current range answer's diagnostics by Contact. A blocked Single absent from AttackAims has invalid hover and shows the supplied wall evidence without requesting an aim preview or submitting an attack.
2. For legal Direction/All or splash aims, the matching detailed preview replaces range evidence. Partially blocked attacks can still be valid. Actor identity and effective-cost preview continue to use Core results.
3. Responses must match the latest live request number. The owned hover request is invalidated on a new cell or pointer exit. Late responses cannot restore old highlights after invalid hover, exit, cancellation, result refresh, or teardown.
4. Result refresh and mode exit clear blocker/effect/cost state and the contact index. Pointer exit and a different hover clear blockers. Cleanup preserves unrelated request ownership.
5. The scene display unions WallCells before display. `BlockedWall = 1 << 4` uses the existing HoverInvalid color, with precedence pointer feedback > blocked wall > affected cells. FieldHighlightLayers owns this rendering priority.
6. The implementation reuses FieldView's existing SetLayerCells/ClearLayer lifecycle. It introduces no physics raycast, Unity collider, geometry service, per-unit branch, or wall-state store.

## Changed files

Production paths below are relative to `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`.

| File | Task 8 change |
| --- | --- |
| `AttackTargetingInteraction.cs` | Range-contact index, evidence forwarding, latest-request matching and hover cleanup |
| `ICombatInteractionFeedback.cs` | ShowAttackBlockers / ClearAttackBlockers |
| `CombatSceneDisplay.cs` | Union supplied WallCells and use the field highlight layer |
| `Display/FieldHighlightLayers.cs` | BlockedWall flag and existing-color priority |

Task 8 also added `Assets/Tests/EditMode/Framework/WallAttackInteractionTests.cs` and its metadata, and adapted feedback fakes in CombatInteractionTests and CombatViewSystemTests. FieldView, scene assets, and prefabs were not modified.

An earlier Task 2 compatibility change also updated `ViewSystem/Dev/DevBattleInitializationSource.cs`: its UnitSpec construction no longer passes the retired ImpactPatternSpec argument. This is a constructor migration, separate from the four presentation files above.

## Verification results

Task 8 final focused run passed 85/85, including 13 WallAttackInteraction cases. The authored Combat scene smoke uses actual display/field renderers and checks union, color precedence/restoration, empty replacement, and clear/reinstall. It preserves an already-open Combat scene by skipping that fixture.

Task 9 separately ran Core (1731/1731 passed), CombatViewBoundary (9/9 passed), and full Framework (1640 passed, 30 failed, one inconclusive). All 30 Framework failure names and messages match the pre-feature baseline. WallAttackInteraction passed 13/13, including the authored-scene smoke. Complete evidence is recorded in the [implementation plan](../plans/2026-10-08-wall-attack-blocking.md#task-9-regression-architecture-audit-and-content-handoff). No manual interactive appearance/usability review was performed.
