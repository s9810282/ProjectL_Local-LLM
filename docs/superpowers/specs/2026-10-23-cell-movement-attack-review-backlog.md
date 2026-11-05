# Cell Movement and Attack Review Backlog

Date: 2026-10-23  
Status: Consolidated review; no implementation approved or performed

Priority update: The user subsequently clarified that hidden Cliffs are not planned gameplay content and selected enhancement trait presentation first. R01 is therefore deferred; the next scoped plan is [Enhanced Trait Display Fix](../plans/2026-10-23-enhanced-trait-display-fix-plan.md).

## 1. Purpose and current product boundary

This document consolidates the movement and attack reviews already completed in this chat. It is a selection backlog for later refactoring, not an implementation plan.

**The user clarified that 2x2 and larger characters are not currently designed content. Their rejection is deliberate.** Consequently:

- Keep the current multi-cell character admission guards.
- Do not classify those guards, or assumptions protected by them, as current gameplay defects.
- Do not start multi-cell infrastructure work merely because the underlying facts can represent multiple cells.
- Reopen deferred items only after an actual character design establishes the required behavior.
- Supported multi-cell structures are distinct from unsupported multi-cell characters.

The attack review covered all 18 registered traits. Only the simultaneous use of PiercingFalloff and ThornField, the Cactus Cannon trait, was excluded. Each trait's standalone path was included. No claim is made that every other combination is supported.

Implementation documents remain in English and code comments remain in Korean. Selecting an item later does not authorize adjacent items, commits, or pushes.

## 2. Priority and selection index

Priorities describe work worth considering under the **current** product scope. Deferred entries have no current implementation priority.

| Priority | Meaning |
|---|---|
| P0 / P1 | Critical or high-priority current defects. None established by these reviews. |
| P2 | A reproduced inconsistency in an existing contract; reasonable first selection. |
| P3 | Optional robustness or authoring validation improvement. No regression in the existing production recipes was established. |
| Deferred | Depends on a future gameplay requirement or policy decision. Keep current behavior until that requirement exists. |

| ID | Priority | Candidate | Evidence | Selection condition |
|---|---|---|---|---|
| R01 | Deferred | Use consistent visible terrain in movement range and AI path queries | Player range/preview mismatch reproduced; AI shares the omission in code | Reopen only if hidden blocking terrain becomes planned content |
| R02 | P3 | Diagnose trait prerequisites and incompatible aiming combinations earlier | Accepted but inactive combination reproduced; other dependencies traced | Useful when adding recipes or allowing arbitrary trait composition |
| R03 | P3 | Validate distance-damage coverage for extended deliveries | Out-of-table exception reproduced with 1x1 characters | Useful when combining distance scaling with piercing, remote extra hits, or displacement |
| D01 | Deferred | Preserve full character occupancy across state changes | Lower-level 4-cell to 1-cell copy reproduced | A multi-cell character is approved |
| D02 | Deferred | Validate complete placement and movement footprints | Anchor-only checks traced | Multi-cell movement/deployment rules are approved |
| D03 | Deferred | Define attack origins, distance, and representative contact cells | Mixed anchor/footprint rules traced; distance probe reproduced | Multi-cell attack geometry is specified |
| D04 | Deferred | Define multi-cell terrain and ThornField contact cardinality | Three immediate thorn hits on one 2x2 target reproduced | Contact and damage-count policies are specified |
| D05 | Deferred | Extend displacement, summon, and reaction paths consistently | Independent guards and anchor calculations traced | Corresponding multi-cell abilities are approved |
| D06 | Deferred | Support picking and previewing multi-cell units in View | Anchor-only input and pointer-centered ghost traced | Core multi-cell placement/movement contracts exist |
| D07 | Deferred | Separate general unit-definition lookup from attack capability if needed | Placement and other non-attack paths currently obtain definitions through attack profiles | A concrete non-attacking or otherwise incompatible unit requires this separation |

Current selection order: **enhancement presentation ES01/ES02 first; R02/R03 when new content composition needs them. Leave R01 and D01-D07 deferred.** The index is not a commitment to implement every entry.

## 3. Query and validation findings

### R01 — Visible-terrain consistency in movement queries

**Current disposition: Deferred.** The user stated that hidden Cliffs are not planned. Retain the technical evidence below without treating the diagnostic configuration as an active product requirement. Do not implement this ahead of the selected enhancement display work.

**Problem.** `CombatMovementCalculator.Range` computes the observer's visible environment but calls `MovementRouteValidator.CanEnter` without passing it. The default uses the full environment. `EnemyMovementPathfinder` has the same omission. Path preview explicitly passes the visible environment.

**Reproduction.** With a Cliff hidden from the player at the adjacent destination, range excludes that destination, preview reports it as movable, and actual execution rejects it with `DestinationOccupied`. A hidden WeakGround can also collapse into a Cliff while preserving its visibility. Hidden Walls are not part of this finding: Walls have a separate public-terrain contract.

**Minimum change to consider.** Pass the already-computed visible environment to the two query-time entry checks. Preserve authoritative environment checks during execution.

**Change / understanding / validation scope.** The player range query and enemy pathfinder need the same visibility contract; this is a necessary shared rule. No View terrain logic, Buffer ownership, Factory, or Framework change is indicated. Test hidden Cliff additions/removals against range, preview, and AI choices; verify actual execution still respects the Cliff. Retain visible Wall/Cliff rejection and hidden HighGround coverage.

**Evidence level.** Player mismatch executed in a managed diagnostic; AI omission confirmed in source, not separately reproduced.

Sources: [CombatMovementCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatMovementCalculator.cs) lines 47, 63, 105-106; [MovementRouteValidator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Movement/MovementRouteValidator.cs) lines 71-76; [EnemyMovementPathfinder.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnemyAi/EnemyMovementPathfinder.cs) lines 23, 45.

### R02 — Trait composition diagnostics

**Problem.** Individual trait validation and profile construction do not express every prerequisite or aiming incompatibility.

| Configuration | Existing behavior |
|---|---|
| PiercingFalloff + DoubleShot | Profile is accepted, but Piercing requires Single aiming while DoubleShot requires Direction; DoubleShot does not trigger. |
| ThornField + DoubleShot | ThornField requires Single aiming, so the same Direction requirement prevents DoubleShot. |
| LuckyBullet without KillChainAttack | No actual kill-chain activation exists to dispatch LuckyBullet. |
| ChainExplosion without a successful knockback | No displacement completion exists to dispatch the extra hit. Blocked movement, immunity, and terrain death correctly suppress it. |

The PiercingFalloff + ThornField combination remains excluded from this review and retains its explicit prohibition. The former two-trait player limit is not present in the reviewed current code; do not restore it as a workaround.

**Classification.** These are composition constraints and opportunities for earlier diagnostics. Existing intended enhancement recipes preserve their prerequisite traits; no shipped-recipe failure was established. A conditional trait being inactive when its trigger does not occur is not itself a bug.

**Minimum change to consider.** When a new recipe or composition system is selected, validate its required aiming mode and prerequisite capabilities at the existing definition/recipe composition boundary. Decide whether an incompatible combination should be rejected or given new semantics before changing it. Avoid adding trait-key branches throughout the attack executor.

**Change / understanding / validation scope.** Keep individual effect rules and the shared executor intact. Validate base definitions and enhanced per-instance definitions consistently; test that existing recipes remain accepted and invalid compositions receive a useful diagnostic. No new service-registration layer is justified.

**Evidence level.** Accepted PiercingFalloff + DoubleShot with an adjacent kill produced zero DoubleShot requests in the diagnostic. Other prerequisites were traced in source.

Sources: [CombatAttackProfile.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatAttackProfile.cs) lines 28-39; [DoubleShotEffectRule.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Effects/DoubleShotEffectRule.cs) lines 28-37; [KillChainAttackCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/KillChainAttackCalculator.cs) lines 94-99; [AttackKnockbackCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/AttackKnockbackCalculator.cs) lines 133-144.

### R03 — Distance table coverage beyond the initial aim range

**Problem.** The distance-damage table must cover every distance a delivery can hit. The initial aim range alone does not provide that bound: piercing travels beyond the aim, LuckyBullet can select remote targets, and ChainExplosion hits after displacement.

**Reproduction.** With 1x1 characters, aim range 4 and four distance percentages, PiercingFalloff + DistanceDamageScale reached a target at distance 5 and threw `InvalidOperationException` for the missing percentage.

**Classification.** Throwing for an incomplete table is an explicit existing configuration contract. The review identified a late-detection risk, not a requirement to silently clamp the distance or invent a fallback percentage. Production use of this incomplete combination was not established.

**Minimum change to consider.** Validate the selected combination against its actual delivery reach at the appropriate definition/battle composition boundary, or explicitly specify a new range policy. Also document which effects apply to each extra-attack type: the current kill-chain path omits `ScaleByTargetDistance`, while fixed/random follow-up execution applies it. Whether that difference should change requires a gameplay decision.

**Change / understanding / validation scope.** The relevant dependency is delivery reach, not the internals of unrelated traits. Cover the table boundary, piercing beyond aim range, a remote LuckyBullet target, a pushed ChainExplosion target, and unchanged ordinary distance damage. Preserve current rounding, defense ordering, and failure behavior unless explicitly changed.

Sources: [DistanceDamageScaleTraitRule.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Rules/DistanceDamageScaleTraitRule.cs) lines 19-25; [CombatAttackDelivery.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackDelivery.cs) lines 56-58; [FollowUpAttackExecutor.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/FollowUpAttackExecutor.cs) lines 76-81; [KillChainAttackCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/KillChainAttackCalculator.cs) lines 73-74.

## 4. Deferred multi-cell compatibility notes

These entries record what the earlier reviews found. **They are not current defect tickets and do not authorize removing guards.** The current guards include profile construction, player placement, combat entry, movement, attack queries, and summon creation. In particular, attack admission rejects a board containing any multi-cell field character, not only a multi-cell attacker.

### D01 — Occupancy preservation

`CombatMovementCalculator.CopyMovement`, `CombatCommandCalculator.Copy`, and `CombatPlacementCalculator.Location` can rebuild occupancy as an anchor singleton. This affects movement bookkeeping, return, attack settlement, and future recall/redeployment. A lower-level diagnostic confirmed that same-position copy and movement sealing reduce four occupied cells to one.

If multi-cell support is later selected, distinguish state-only updates, which preserve occupancy, from actual placement changes, which derive the complete destination footprint. Keep geometry in Core; Buffer should validate proposed facts rather than duplicate game-rule calculation. Verify health/stat updates, movement, attack, turn end, checkpoint return, night recall, and redeployment retain the intended shape.

Sources: [CombatMovementCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatMovementCalculator.cs) lines 161-167; [CombatCommandCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatCommandCalculator.cs) lines 259-272; [CombatPlacementCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatPlacementCalculator.cs) lines 94-111.

### D02 — Complete movement and placement legality

Current character movement, return, and placement check an anchor cell. After a future shape-preservation change, those checks would also need the destination's complete occupied area: bounds, deployment zones, terrain, other entities, and overlap with the mover's own previous position. Range output currently constructs singleton destinations too.

Reuse the existing `TileFootprint` rotation/placement primitives. Full destination occupancy for collision and newly entered cells for effects are different concepts and need explicit contracts. Cover 2x2 and rotated 2x1 cases, narrow passages, board edges, self-overlap, checkpoint return, and night redeployment. The current rectangular SO dimensions already express 2x2; irregular-shape authoring should wait for an actual requirement.

Sources: [MovementRouteValidator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Movement/MovementRouteValidator.cs) lines 31-36, 71-76; [CombatPlacementCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatPlacementCalculator.cs) lines 28-34; [TileFootprint.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Tiles/TileFootprint.cs) lines 79-116.

### D03 — Attack geometry and contact facts

Range, aiming, and wall checks originate at the attacker anchor. Distance effects use attacker anchor to nearest target occupied cell, while Cannon adjacent-cell exclusion uses the entire attacker footprint. A diagnostic measured distance 2 for a target touching a 2x2 attacker's outer edge. DoubleShot similarly compares attacker anchor with the killed entity's death anchor.

Target selection understands occupied cells, but normal hit delivery retains target ID/role and physical origin without a representative receiving cell. Piercing records an encounter cell, then discards it when creating the damage hit. Terrain defense uses the victim anchor. A future policy based on the contacted cell therefore cannot be implemented consistently by changing range alone.

Before implementation, choose firing origin, range metric, adjacency, and the representative receiving cell when several cells are touched. Share those facts across ordinary attacks, preview, AI, and the relevant follow-ups. A corresponding DoubleShot question exists for already-supported large structures: whether adjacency means their anchor or any occupied edge. That policy was not resolved or executed in these reviews, so it is not recorded as a confirmed current bug.

Sources: [CombatAttackQueries.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackQueries.cs) lines 54-55, 85, 184-188; [CombatDamageCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatDamageCalculator.cs) lines 43-44, 70; [DeliveredAttackHit.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/DeliveredAttackHit.cs); [CombatAttackDelivery.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatAttackDelivery.cs) lines 99-101.

### D04 — Terrain contacts and ThornField hit counts

Decisions are needed for newly covered versus continuously occupied cells, partial HighGround contact and cost, partial Cliff contact and falling, simultaneous object order, and multiple position modifiers. Existing multi-cell position-effect exceptions deliberately avoid inventing these rules.

ThornField creates the eight neighboring cells excluding the selected center. With a 2x2 target covering the center and three neighbors, the lower-level diagnostic produced HP `100 -> 80 -> 75 -> 70 -> 65`: one direct hit of 20 and three thorn hits of 5. Result targets remain deduplicated, but damage/item/held-defense processing occurs per contact. Poison defense reduction refreshes rather than stacking additively.

This is a policy question, not proof that per-cell damage is wrong. If the approved rule becomes one immediate hit per entity, deduplicate immediate damage per contribution while preserving cell-based field creation. Retain captured occupancy and corpse-handling behavior, existing death deduplication, and terrain source ordering.

Sources: [MovementEnvironmentCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/MovementEnvironmentCalculator.cs) lines 75-86; [EnvironmentPresenceReconciler.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/EnvironmentPresenceReconciler.cs) lines 35-41; [ThornFieldApplicationCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/ThornFieldApplicationCalculator.cs) lines 46-70.

### D05 — Displacement, summons, and follow-ups

- Character knockback checks the destination anchor and anchor-based terrain contacts. Structure knockback already checks and preserves the complete footprint; reuse the relevant geometric pattern without merging different gameplay policies.
- Tile forced movement and Tornado have explicit multi-cell exceptions. Candidate generation and arrival processing need the approved footprint/contact policy before those guards can change.
- Necromancy uses eight neighbors around the summoner anchor and separately rejects multi-cell summons. A larger summoner's perimeter, a larger summon's placement, and next-night redeployment need one agreed candidate rule.
- Frenzy has its own anchor-based range/occlusion calculation; updating ordinary attack geometry alone would leave this path unchanged.
- Roar's count and repeat suppression are entity-based, but reset copies, reaction geometry, and knockback still depend on the common future changes.

Validate blocked and partial displacement, immunity, contact death, summon space exhaustion, next-night return, and reaction termination. Preserve separate attack groups, causal suppression, kill attribution, and single death/reward settlement. No new universal movement or attack engine is required by the review evidence.

Sources: [AttackKnockbackCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/AttackKnockbackCalculator.cs) lines 60-86, 159-174; [TornadoCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/TornadoCalculator.cs) lines 29-35; [CombatSummonCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Summoning/CombatSummonCalculator.cs) lines 113-140; [FrenzyAttackCalculator.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/FrenzyAttackCalculator.cs) lines 29-41.

### D06 — View selection and destination presentation

Movement drag starts only from the actor anchor, although ordinary hover/picking can inspect all occupied cells. Merely broadening the press check would be insufficient: the current interaction can interpret an adjacent starting pointer cell as the first step. Future input should preserve the picked-cell offset while continuing to submit an anchor path.

The movement ghost follows the raw pointer, whereas the actual unit is centered over its occupied cells. Use accepted Core preview occupancy for destination outlines and the established presentation center calculation. Do not reconstruct footprint legality from SO data in View. Placement ghost rotation should also be checked for a future non-square unit.

Actual unit display and movement playback already consume complete character facts. Range highlighting intentionally marks possible anchors, not the union of destination footprints; that tested behavior is not a defect. Validate each occupied start cell, no movement on selection alone, offset-preserving drag/return/mode switching, and agreement between preview and final pose.

Sources: [CombatPathInput.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatPathInput.cs) lines 125, 403; [CombatMovementInteraction.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/MovePath/CombatMovementInteraction.cs) lines 112-131; [CombatMovementPlanningPresenter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Feedback/CombatMovementPlanningPresenter.cs) lines 53-60, 110-117; [FieldUnitPresenter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Unit/FieldUnitPresenter.cs) lines 558-570.

### D07 — General definition lookup versus attack capability

Placement, hit handling, and turn processing currently obtain unit definitions through attack profiles. This connects general unit availability to attack-profile compatibility. The review does not establish a present need to split them. If a future non-attacking unit or shape policy requires independent availability, introduce the narrow definition lookup needed by those consumers while keeping attack validation with attack composition.

Source: [BattleDefinitionAdapter.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CombatSystem/BattleDefinitionAdapter.cs) lines 119-145.

## 5. Trait coverage register

This table describes the rule implementation, not current multi-cell product support. All multi-cell character rows remain behind the intentional admission boundary.

| Trait | Review result / relevant dependencies |
|---|---|
| Focus | Entity-level stacks and movement flags; no direct footprint dependency. Preserve state and once-per-action transitions. |
| MoveAttackDiscount | Entity-level first-move/cost contribution state; depends on correct movement and attack settlement. |
| PiercingFalloff | First encounter per entity and exclusion of all actor cells already exist. D03 geometry and R03 distance combinations remain relevant. Single aiming, no splash. |
| ThornField | Single aiming; D04 defines multi-contact damage policy. PoisonThorn extends this existing trait rather than adding a nineteenth TraitKey. |
| Knockback | D02/D04/D05: full footprint legality, direction, and contact processing. |
| DistanceDamageScale | D03 origin/distance policy and R03 coverage of every reachable delivery distance. |
| KillChainAttack | Targets and kills are counted per entity. Preserve recursion, RNG, terminal checks, and attribution; extra-hit effect policy belongs to R03. |
| Necromancy | D05: larger summoner perimeter, summon footprint, and redeployment. |
| Roar | Hit-cause and reaction deduplication are entity-based. D01/D03/D05 still apply; its reaction aiming requires All. |
| Regeneration | Entity-level health restoration; preserve the existing accepted-turn-end schedule and location facts. |
| Intimidation | Delivered victim IDs are deduplicated before applying to surviving enemies. Preserve that boundary. |
| Frenzy | Entity-level candidates and credited-kill deduplication are present; its independent geometry needs D03/D05. |
| DoorBreaching | Numeric distance condition is reusable; its distance input shares D03. |
| ContemptForWeak | Compares current effective attack by entity; structures are excluded from that comparison. No direct shape calculation. |
| PrecisionShot | Uses actual-recipient ordinal per delivery. Preserve entity deduplication, absorbed-hit handling, and zero-percent-contact rules. |
| DoubleShot | Direction requirement and own-descendant suppression; R02 composition and D03 adjacency/death-location facts. |
| LuckyBullet | Unique living hostile boss/structure ID union. Requires actual KillChainAttack activation; R02/R03. |
| ChainExplosion | One surviving displaced target ID after actual movement. Depends on Knockback and D05; R03 applies to distance scaling after the push. |

## 6. Architecture assessment and boundaries to preserve

| Criterion | Consolidated assessment |
|---|---|
| SSOT | Core computes changes, Buffer validates/publishes immutable facts, and View consumes them. Keep one occupancy source and one owner for any future geometry policy. |
| SRP | State-only copying and placement calculation become distinct responsibilities if multi-cell support is approved. Definition lookup versus attack capability is a conditional concern, not an immediate split request. |
| OCP | Typed trait capabilities already provide useful extension points. Composition validation should not require scattering trait-specific branches through unrelated executors. Intentional unsupported-content guards are not automatically an OCP violation. |
| LSP | No concrete substitution-contract violation was established. Do not infer one merely from deliberate unsupported-content checks. |
| ISP | The review did not establish a need for additional broad service interfaces. Keep rule inputs/outputs specific to their consumers. |
| DIP | Core geometry and effect calculation remain independent of Unity presentation. Future View work should consume Core facts rather than duplicate rules. |
| Framework contracts | These findings do not require a new service, altered Group exports, Factory layers, or changed assembly boundaries. No repository-wide Framework conformance audit is claimed. |
| Change locality | R01 is narrow. R02/R03 belong at composition/delivery contracts. Future shared geometry must reach ordinary, preview, AI, and relevant follow-up consumers, while preserving independently owned cost/effect policies. |

Existing useful foundations include `TileFootprint`, complete occupied-cell queries, Buffer bounds/overlap validation, entity-deduplicated ordinary/piercing target selection, movement playback from full facts, and entity-based death/kill/Roar bookkeeping. Preserve these rather than rebuilding them.

## 7. Verification record and limits

The following results were obtained during the two reviews in this chat. They were **not rerun merely to assemble this document**.

| Check | Recorded result | Limit |
|---|---|---|
| Movement-focused tests | 191 passed, 0 failed | Pure C#/.NET 10 diagnostic harness; not Unity EditMode or manual Play |
| Attack/trait-focused tests | 79 fixtures, 1,313 passed, 0 failed | Same managed-harness limitation; the movement and attack selections overlap and must not be summed as unique coverage |
| Hidden Cliff diagnostic | Range excluded destination; preview allowed it; execution rejected it | Player query inconsistency reproduced; AI was source-reviewed |
| Multi-cell admission/copy diagnostics | Ordinary attack rejected; lower-level selection returned one target; copy/seal reduced four cells to one | Unsupported facts used to inspect lower layers; not an operational multi-cell battle |
| Geometry/Thorn diagnostics | Edge-adjacent distance reported 2; DoubleShot request count 0; direct 20 plus three thorn hits of 5 | Future policy questions, not proof that a specific new gameplay policy is correct |
| Trait composition diagnostics | Accepted Piercing + DoubleShot produced no request; Piercing + four-entry distance table threw at distance 5 | Diagnostic configurations; no current production recipe regression established |

Scratch harnesses and XML results were kept under ignored `.utmp/cell-movement-review/` and `.utmp/attack-multicell-review/`. They are local review aids, not durable repository tests. Recreate the selected case as an appropriate maintained regression test when implementing a chosen item.

No Unity editor compile, Unity EditMode run, manual View acceptance, or complete product-readiness claim follows from these results. Production code was not changed for the reviews or this consolidation.

## 8. Selecting later work

Select an ID and an explicit scope, for example, "Implement R01 only." Before implementing R02/R03, identify the actual content combinations to support. Before reopening a D item, establish the corresponding gameplay policy and keep unrelated guards in place.

For each selected item, prepare the smallest design/task plan that states ownership, preserved behavior, changed contracts, and required checks. Finish and report at that approved boundary. This document itself authorizes no refactoring.
