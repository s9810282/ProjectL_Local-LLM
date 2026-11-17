# Piercing Revision and Dead Hand — SoulEater Design

Date: 2026-11-14

Status: The approved Core, InGame SO and API implementation is in the working tree. The companion plan records phase verification and remaining View integration. See the [Core to View handoff](../handoffs/2026-11-14-piercing-dead-hand-core-view-handoff.md) for the current API and Unity acceptance checks.

Implementation plan: [Phase-by-phase implementation and verification](../plans/2026-11-14-piercing-and-dead-hand-implementation-plan.md).

## 1. Intent and authority

Revise Piercing for the Sniper and introduce Dead Hand, a reusable basic-attack trait initially carried by the player Heavy unit whose code name is `SoulEater`.

Both traits consume the carrier's basic attack geometry. Neither trait owns a second attack-range definition. SoulEater gathers enemies so another unit can exploit their alignment; Piercing delivers a shot through characters on its line.

The user's latest messages override the attached `design.md` prototype. In particular:

- SoulEater uses a **3-wide, 2-deep basic attack**, not a trait-owned 3x3 area.
- The unit code name is **SoulEater**. The existing enemy Gravekeeper is not renamed or repurposed.
- Dead Hand includes enemies behind walls when selecting attack targets, but forced movement never passes through walls.
- Dead Hand damage defaults to **100% of attack power**, is configurable through SO authoring, and uses half-up integer rounding.
- **Piercing also follows the carrier's basic attack range.** Sniper's basic attack type becomes cardinal cross; cardinal geometry is not hard-coded into the trait.

The attached document is reference material, not an instruction to deploy its web simulator. Its prototype undo control is not a combat undo contract.

### 1.1 Confirmed implementation scope

Include the Piercing revision, SoulEater unit SO and basic targeting definition, Dead Hand's rank-aware trait-growth data, Core calculation, shared compatibility/activation improvements described here, and command/preview/result APIs for View. Include Core and CoreIntegration verification and a handoff that explains the new API facts.

Exclude additions to shops, rewards, and level-up acquisition pools. Existing generic acquisition/equipment validation must still reject illegal combinations; that is compatibility enforcement, not new content availability. Do not add SoulEater to production loadouts or inventories to make the feature visible. Registration and data tests establish readiness without changing acquisition.

View interaction, character/hand visuals, prefab/art, UI descriptions/formatters, scene wiring, and playback implementation are a separate handoff. The code name remains SoulEater; the final player-facing unit name is not a blocker for this Core scope. Generate technical unique IDs and GUIDs without reusing the existing enemy Gravekeeper. Do not remove priority fields or perform unrelated cleanup.

## 2. Confirmed gameplay

### 2.1 Piercing

1. Eligible aim tiles come from the carrier's basic attack range and must contain an enemy. Empty and friendly-only tiles cannot be selected.
2. Sniper uses a cardinal-cross basic range. Another compatible carrier uses its own basic range; Piercing does not replace that shape with a cross.
3. The shot starts at the attacker and travels straight toward the selected tile, continuing beyond it. The basic attack range limits **aim selection**, not travel beyond the aim.
4. All allied and enemy characters encountered on that line can be hit, including characters before the selected enemy. The attacker is excluded.
5. Default damage factors, in physical encounter order, are **100%, 75%, 50%, 25%**. This is a subtraction of 25 percentage points per encounter, not repeated multiplication by 0.75.
6. Damage applies to at most **four targets total**, counting both allies and enemies. The selected enemy has no reserved hit slot: four preceding characters can exhaust the shot before it reaches that enemy.
7. The reduction percentage remains configurable through existing SO trait authoring. The existing maximum-target setting is retained with a default of four; changing falloff does not implicitly remove that cap.

**Preservation proposal:** retain existing damage rounding, defensive processing, wall/structure blocking, and stop-at-budget delivery evidence unless explicitly changed later. SoulEater's through-wall area targeting does not change Piercing's occlusion rules. Existing zero-damage early termination remains visible in the compatibility notes below.

### 2.2 SoulEater and Dead Hand

1. SoulEater is a **player Heavy** unit with maximum health **20**, attack power **6**, physical defense **5**, and retained authored priority **30**. Its basic attack is a direction-selected rectangle, three tiles across and two tiles forward from the unit. Priority is retained for existing data-contract compatibility, not to introduce a new initiative rule.
2. Dead Hand activates only as part of a basic attack. It does not activate again on extra attacks such as Kill Chain Attack. Its area comes from the basic attack, allowing the trait to be attached to other units without copying SoulEater's dimensions into the trait. Do not recursively run the sweep because this attack produced an additional hit or attack.
3. SoulEater's intended View interaction supports enemy tile selection, attack-direction selection, then a choice to sweep left or right. Where areas overlap, the player can select the intended attack direction. For this direction-area attack, the initially clicked enemy is an input aid, not a mandatory execution-time target. At attack acceptance, resolve the current attack targets; another eligible enemy in the area is sufficient if the initially clicked enemy is gone. If no eligible target remains, do not execute or consume the attack action. This does not change a Single attack into an area attack: a Single attack retains its selected-target validation.
4. Sweep directions are **Left or Right relative to the attacker's facing**, never absolute board/screen directions and never forward/backward choices. The API allows the caller to provide the side; when omitted, default to **facing-relative Right**. View decides how to offer that choice. SoulEater's attack area and side destinations rotate with its facing for the attack.
5. The intended destination is the tile immediately beyond the attack area's end in the chosen sweep direction, on the target's attack-local row. Derive it from the existing attack mask, regardless of the target's starting position. For SoulEater's rectangle, every affected row shares the same destination column. Dead Hand does not recenter the attack mask or require an odd width; a resolved 2x2 mask works by the same boundary rule.
6. Capture the basic attack's actual eligible enemy targets at execution time, including enemies behind walls and enemies immune to knockback. Single attacks move only their selected target; direction-area attacks move their actual targets in that area; All attacks move their actual targets. Do not expand a Single attack to every enemy in its basic range. The basic attack mask supplies the destination boundary, while its resolved target set supplies who moves and takes damage. Allies and non-target units are not Dead Hand victims.
7. Each movable enemy retains its attack-local row and moves toward the chosen destination column. This is destination-based movement, not a fixed-distance knockback.
8. For a right sweep, process each row from right to left. For a left sweep, process each row from left to right. Later enemies can occupy cells vacated by earlier enemies.
9. Enemies cannot overlap, pass through each other, or push another enemy again. Multiple enemies pack behind the first enemy, which reaches the destination column if unobstructed.
10. Walls, knockback-immune units, allies, and units outside the captured target set block movement. A target stops at the last free tile before the blocker. Immune targets remain at their starting positions. In a Single attack, other units can therefore block movement even when they stand inside the geometric basic range.
11. A blocker does not cancel targets beyond it. Each target still attempts to reach its destination on its own side of that blocker.
12. Complete the movement stage before applying this attack's damage. Each captured target still alive at the damage stage receives one attack hit, including targets that moved zero tiles or are immune to knockback, unless combat termination has stopped the attack.
13. Removing a blocker through this attack's damage does not reopen or rerun the completed movement stage.
14. Damage authoring exposes a percentage of the carrier's attack power **for each trait level**, with the initial values **Lv1 100%, Lv2 100%, Lv3 100%**. Each level's value is independently configurable through the existing trait-growth SO. Use the carrier's current trait rank to resolve the percentage. Round fractional results half up: attack power 15 at 50% becomes 8, not 7.
15. Direction-choice previews show each enemy's actual route and destination, including blocked and immune outcomes.
16. Presentation can move the targets together even though the authoritative calculation is ordered. A right sweep shows the hand entering from the left; a left sweep shows it entering from the right.
17. Movement-triggered terrain effects follow existing forced-movement rules at their existing timing. "Movement before damage" orders Dead Hand's own attack damage; it does not defer terrain damage.
18. A target that dies during movement stops moving and receives no later Dead Hand attack hit. While combat continues, remaining targets continue in order and may use cells vacated by that death. Previously completed moves are never replayed.
19. Confirmed combat termination stops all remaining movement and attack damage. Otherwise, after the movement stage, apply the attack to the surviving captured targets.
20. Do not restrict Dead Hand equipment to direction-selected basic attacks. The user reconsidered that temporary restriction: the trait consumes the actual basic attack targets and a facing-relative sweep side. It must not invent a separate area to support another targeting mode.
21. Sacrifice redirects damage, not the preceding sweep. Move the original attack targets first. After all movement, evaluate existing Sacrifice eligibility using the post-movement world and apply valid damage redirection normally. A protector does not move merely because it receives redirected damage; if it was itself an original attack target, it participates in its own normal movement once.

### 2.3 Trait equipment exclusions — confirmed

The user confirmed that traits which reposition targets as part of an attack cannot be equipped together on the same unit. Dead Hand, ordinary Knockback, and Pull are mutually exclusive; do not define an execution priority or silently suppress one of them to make a conflicting loadout work.

Apply this same rule to attack-displacement variants. Current code mappings include `CrashImpact` (collision knockback) and `ImpactBolt` (impact-centered knockback), alongside `DeadHand`, `Knockback`, and `Pull`. The implementation plan must verify the effective trait sources and the complete membership against actual behavior. This restriction does not by itself prohibit Piercing, knockback immunity, or unrelated voluntary/self-movement abilities: those are not multiple target-displacement effects in the same attack.

**Additional confirmed pair exclusion:** Piercing (`PiercingFalloff`) and Dead Hand cannot be equipped together on the same unit. Declare this exact pair in the shared compatibility rules. Do not classify Piercing as an attack-displacement trait or infer additional bans on its combinations solely from this decision. Separate units may still use Dead Hand to align targets and Piercing to attack them afterward.

**Proposed enforcement:** keep one compatibility rule and use it at trait acquisition/equipment and authoring/profile validation boundaries. Validate the effective combined trait set, including category defaults and restored definitions, before mutating state. Reject invalid combinations explicitly instead of choosing a winner. Existing incompatible content requires explicit correction; do not silently remove a persisted trait.

## 3. Current implementation evidence

Paths below are relative to the repository root. Let `C` denote `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/` and `D` denote `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/InGame/`.

| Source inspected | Current behavior | Consequence |
|---|---|---|
| `C/Domain/Traits/Rules/PiercingFalloffTraitRule.cs` | Subtractive percent falloff, half-up damage scaling, configured target cap, early termination when rounded damage becomes zero | Reuse; no new falloff engine |
| `C/Domain/Combat/PierceTargetCollector.cs` | Straight cardinal or diagonal trace beyond the aim; ordered encounters; wall, live structure, and board-edge stops | Reuse travel; do not globally remove diagonal support merely because Sniper becomes cardinal |
| `C/Application/Combat/CombatAttackDelivery.cs` | Passes selection eligibility into the trace, applies the target budget, publishes ordered encounters and endpoint | Introduce the necessary friendly-fire delivery eligibility without widening aim eligibility |
| `C/Application/Combat/CombatAttackQueries.cs`, `C/Domain/Combat/AttackTargetEligibility.cs` | General target eligibility and primary-cell validation precede delivery | Require a hostile occupied aim for Piercing independently of the wider set of characters its projectile can hit |
| `C/Contracts/Combat/CombatAttackProfile.cs` | Piercing currently requires Single selection and no splash; checks incompatible traits | Preserve supported profile contracts; the request does not define arbitrary curved, area, or multi-ray Piercing delivery |
| `D/Table/Unit/Trait/UnitTraitAuthoring.cs` | Existing Piercing fields are `reductionPercentPerTarget` (initializer 50) and `maxTargetsPerRay` (initializer 0) | Set intended defaults to 25 and 4, and explicitly check persisted definitions; changing initializers is not an asset migration |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_Sniper.asset` | Inspected asset has range 4 and `traits: []`; its targeting GUID resolves to `TargetingMethod_1.asset` (RadialSquare, Single, no splash) | Update the Sniper definition to the newly requested cardinal basic range. Do not claim the production unit already carries Piercing; inspect effective trait assembly before activation, without adding duplicates |
| `C/Domain/Combat/AppointShapeCalculator.cs` | Type 5, `RadialSquare`, direction mode and range 2 produce a **5x2** forward area | Existing four suggested targeting values alone cannot express SoulEater's 3x2 rectangle |
| `D/Table/Targeting/Item/TargetingMethodDataSO.cs`, `C/Contracts/Commands/TargetingMethodSpec.cs` | Shape, choice mode, splash, angle; no independent width | A basic-attack geometry extension is required |
| `C/Application/Combat/AttackDeliveryExecutor.cs` | Ordinary damage occurs before ordinary knockback | Dead Hand needs a distinct pre-damage movement stage; do not reverse the order for every attack |
| `C/Application/Combat/PullAttackCalculator.cs`, `CombatAttackCalculator.cs` | Existing pre-hit forced-movement integration and shared arrival projection | Reuse established command/projection boundaries, without pretending Pull's one-target success conditions implement a group sweep |
| `C/Contracts/Commands/AttackAim.cs` | Cell, direction, or all targeting; no independent sweep-side selection | Extend command/preview intent without overloading attack direction or screen coordinates |
| `C/Domain/Combat/CategoryMatchupDamageCalculator.cs` | Checked nonnegative half-up integer scaling | Reuse for percentage scaling; avoid floating-point or banker's rounding |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/EnemyBaseSO/Unit_Gravekeeper.asset` | Enemy-only, range 1, Knockback trait | Keep the existing enemy definition intact; add SoulEater separately |

These are static source observations, not newly executed runtime or test results.

## 4. Approaches and proposed direction

### 4.1 Piercing

**Recommended: extend the existing delivery path narrowly.** Keep trace geometry, falloff, encounter evidence, and result publication. Separate hostile aim selection from allied/enemy projectile encounters. Configure Sniper's basic targeting as cardinal cross.

A standalone replacement would duplicate working contracts. Globally enabling friendly targeting would incorrectly make allies selectable. Neither is appropriate. The latest user correction explicitly rules out a trait-owned cardinal range.

### 4.2 Basic attack geometry

**Recommended: add a forward-rectangle basic targeting shape** with an authored width and the unit's existing attack range as forward depth. Keep `RadialSquare = 5` behavior and existing serialized values intact. The new enum value/name is assigned during implementation after checking identifiers; this document does not silently redefine type 5.

Extending type 5 with optional width could preserve old defaults, but introduces shape-dependent default semantics for existing content. A new named shape makes the distinction explicit. Hard-coding 3x2 inside Dead Hand contradicts reuse and SSOT.

The user authorized writing this design based on the conversation; this geometry choice remains a proposal for design approval, not a previously confirmed numeric data assignment.

### 4.3 Dead Hand execution

**Recommended: a focused Dead Hand pre-hit movement calculation integrated into the existing attack command.** Reuse forced-displacement legality, immunity, occupancy, event publication, and damage settlement. Keep the ordered packing rule together; orchestration only selects and orders stages.

The user's subsequent structure request is included in this design: keep activation, compatibility, and default-direction rules in explicit owning contracts instead of repeating trait-name exceptions throughout query, preview, damage, movement, and View code. Section 9.1 defines the proposed bounded improvement; implementation still follows the separate plan after gameplay decisions are settled.

Repeatedly invoking ordinary Knockback would retain the wrong damage-first order and radial direction semantics. A new persistent world, bespoke movement engine, or independent Job queue would duplicate existing ownership.

## 5. Data and ownership

### 5.1 Proposed authoring

| Owner | Field / value | Responsibility |
|---|---|---|
| Sniper unit / targeting SO | CardinalLine, Single, no splash; retain current attack range unless separately changed | Sniper's legal basic aim region |
| Existing Piercing authoring | Reduction 25%, target cap 4 | Projectile falloff and maximum hit count |
| SoulEater unit SO | New unique key; PlayerOnly; Heavy; basic attack enabled | Unit identity and category |
| SoulEater unit SO | MaxHealth 20; AttackPower 6; PhysicalDefense 5; Priority 30 | Confirmed base stats; priority remains a compatibility value, not active turn initiative |
| SoulEater targeting SO | Forward rectangle; width 3; Direction; choice angle 0; no splash | Basic attack geometry and selection |
| SoulEater unit SO | AttackRange 2 | Forward depth, using the existing source of attack range |
| Dead Hand trait-growth SO | Proposed code name `DeadHand`; Normal/Lv1 `damagePercent = 100`; Rank2 100; Rank3 100 | Independently authored damage coefficient per rank; no width, depth, target-column coordinates, or runtime positions |

Use the current `UnitTraitAuthoring -> UnitTraitData -> TraitSpec -> definition/registry` path and existing `TraitGrowthDefinitionDataSO` Normal/Rank2/Rank3 slots. Normal supplies Lv1, and the existing rank-resolution flow supplies the current level's coefficient. Add the required typed Dead Hand rank values through that flow; do not create another level table, use Enhanced as Lv2/Lv3, or compute higher levels from an implicit increment. The initial three values are all 100%, but they must remain independently editable.

Ensure the unit's initial trait definition and its resolved runtime trait follow the established definition/rank assembly path. Do not add a competing runtime percentage override to the unit, Buffer, or View. Copy, restore, training/growth, effective-spec creation, and description/snapshot routes must preserve the current rank and resolved coefficient. A change to the growth SO should not require a code constant or View lookup change. Registration of rank data does not by itself decide shop availability, acquisition pools, or final View presentation.

**Planning finding:** `TraitGrowthDefinition` currently rejects adjacent ranks whose resolved specs have equal values. The approved 100%/100%/100% data therefore requires a narrow validation extension. Proposed engineering choice: add an explicit immutable `AllowUnchangedRankValues` policy to the growth definition/data/SO, default false for existing content, true for the new Dead Hand definition. Preserve rank-slot/key validation and actual rank advancement. Do not encode `if (TraitKey == DeadHand)` in the common validator or silently relax every existing definition. Carry the policy through adapters and definition comparison/snapshots. Training that already supports an owned ranked trait retains its generic behavior; no new acquisition-pool entry is added.

**Current priority usage, source-verified on 2026-11-14:** `CombatTurnCalculator` uses controller-side turns, and `EnemyAiTurnService` / `EnemyDecisionSession` choose enemy actions through decision evaluation rather than sorting units by authored Priority. `CombatEffectiveStatsCalculator.Priority` returns the stored stat, and `BufferSystems/Character/CharacterData` still exposes it as `EffectivePriority`. The field remains in SO/data/spec/snapshot/copy contracts; `UnitData.Validate` still requires a positive value. Legacy priority-related helpers and data also remain, but no current unit-turn ordering consumer was found. Keep SoulEater's requested value 30 without reviving priority scheduling. Removal of the legacy field and unrelated priority remnants is outside this trait task; this observation is static inspection, not a new runtime test.

**Proposed numeric validation:** nonnegative integer damage percentage, including values above 100; checked arithmetic; negative inputs rejected. At 0%, movement still occurs. Geometry must have positive width and depth. SoulEater's width is three. Placement of any other basic attack mask belongs to that attack's targeting definition; Dead Hand consumes the resolved mask without imposing a separate centering or odd-width rule.

### 5.2 Geometry definition

Let `F` be the attacker's forward facing for the attack and `R` its clockwise perpendicular. For SoulEater, `F` is cardinal and the six local offsets are:

`origin + d * F + s * R`, where `d in {1, 2}` and `s in {-1, 0, 1}`.

Left destinations use `s = -2`; right destinations use `s = +2`, retaining `d`. The four attack directions together cover 20 tiles: a 5x5 square excluding its four corners and the actor cell. The attacker cell is not an attack tile.

For another resolved attack mask, use that row's attack-area boundary in the selected sweep direction and the immediately adjacent outside tile. This calculation does not require the mask's center or the enemy's distance from the attacker. A different row width changes that row's destination, not the trait's range. Obstacles and packed targets can stop actual movement before that intended destination.

For cardinal facing, relative Right maps as follows: Up -> board Right; Right -> board Down; Down -> board Left; Left -> board Up. Relative Left is the opposite. An omitted side resolves to this relative Right, not a fixed positive board axis. Use one facing basis consistently for preview, execution, and publication.

**Planning resolution:** the current command already derives a `Rotation4` attack-facing value through `CombatCommandCalculator.AttackRotation` for Cell/Direction aiming. Reuse that established facing convention, including its existing diagonal quantization; do not add eight-way facing to this task. For All targeting there is no aim direction, so use the field actor's existing `Rotation4` instead of the old helper's no-aim R0 fallback. Put this in one sweep-facing resolver, publish the resolved facing, and do not change unrelated attacks' facing conventions or mutate presentation-only state. This is an engineering mapping of the confirmed facing-relative rule, not a new player input requirement.

Bounds are derived from the complete chosen basic-attack shape, not from where enemies happen to stand. Board clipping does not shift the nominal destination inward: if a destination is outside the board, movement stops at the last legal cell under the existing movement legality rules. These clipping semantics are a design proposal.

### 5.3 Runtime facts

- SOs own design input, not mutable combat state.
- Existing Core command projections own transient ordered simulation. They are not a second authoritative state store.
- Existing combat Buffers retain authoritative state and published results. View consumes those facts.
- View owns temporary enemy/direction/side UI selection and presentation resources. It does not calculate packing, wall passage, immunity, or damage.
- Selection, preview, and execution use the same resolved basic-attack geometry and Core rules.

## 6. Piercing change boundary

The expected implementation is **limited, but not necessarily data-only**:

1. Resolve the carrier's basic attack range normally. Update Sniper's targeting reference/content to cardinal cross, preserving any shared targeting asset used by other units.
2. Retain Single/no-splash profile compatibility. Preserve the existing cardinal/diagonal straight-line support for other compatible carriers. Do not accept non-aligned aim cells simply because a basic mask contains them.
3. Validate the selected tile as hostile and occupied. Do not let another trait's friendly or empty-target permission accidentally weaken this Piercing requirement.
4. Collect physical projectile victims separately: living allied and enemy characters along the line, excluding the actor. Maintain existing neutral/structure policies unless explicitly revised.
5. Feed that ordered list to the existing budget, rounding, and damage pipeline. Shielding or a zero final HP loss must not reorder physical encounters or grant an extra hit slot.
6. Publish the existing `PiercingShotDelivery`/encounter evidence for preview and execution, including a selected target that may not actually be reached.
7. Update intended authoring values and effective production trait assembly deliberately. Do not assume the inspected Sniper SO's empty trait list represents all runtime defaults.

At 25% falloff and cap four, an attack power of 100 produces nominal hits 100/75/50/25. With lower attack power, existing half-up scaling can round a later hit to zero and stop the shot before four encounters. Preserving that behavior is proposed; changing it would be an additional requirement, not an incidental refactor.

## 7. Dead Hand command, movement, and damage

### 7.1 Input and command contract

The Core scope exposes attack and preview APIs; View owns the interaction used to select a side. SoulEater can retain attack targeting, attack-direction selection, sweep-side preview, and one final submit. Other basic targeting modes keep their own targeting semantics. Direction-area enemy clicks help choose valid attack directions; reuse the current direction-based aiming interaction where applicable.

The final command and preview request carry the original attack aim plus an **optional facing-relative sweep side (Left/Right)**. Resolve an omitted value to Right in one shared Core rule used by preview and execution. Preserve the distinction between omitted and invalid values: missing input is valid and uses the default; an unsupported explicit value is rejected.

For direction-area targeting, the initially clicked enemy is only an interaction aid: its identity and original tile are not additional execution-time preconditions. For Single targeting, retain the existing actual target validation. Resolve current eligible attack targets when accepting the attack; never expand the target set simply because Dead Hand is attached. If no eligible target remains, reject before movement, damage, or attack-action consumption. The preview must not lock in a stale target list.

Ordinary attacks retain their existing request behavior. Cancel before final submission consumes no action and makes no combat mutation. After submission, the existing command gate and result-consumption lifetime apply. Supplying no side does not open a new Core input wait or silently disable the trait.

### 7.2 Ordered movement stage

Evaluate basic-attack eligibility through the effect's own activation rule before preparing this stage. Extra attacks must not prepare a sweep, inherit its target list, or apply its attack-power coefficient again. This must not disable unrelated trait contributions on an extra attack.

1. Validate attack intent and resolve the current basic attack mask.
2. Capture the actual eligible enemy identities resolved by the basic attack before any movement, respecting Single/Direction/All selection. Wall line-of-sight filtering must not discard otherwise valid Dead Hand targets. Do not disable movement wall collision to achieve this.
3. Derive rows and side destinations from the selected geometry.
4. Use one shared command projection. Process rows in a deterministic order; within each row, process the leading enemy first. A stable ID tie-break is a technical fallback, not a substitute for spatial ordering.
5. Check immunity using the existing attack-knockback immunity policy. Keep immune enemies in the damage set and record their unchanged positions as blockers.
6. Move each other enemy along its local row one legal cell at a time toward the destination. Reuse the existing forced-movement path legality and occupancy source. Record start, ordered path, final cell, and stop reason, including zero-step outcomes.
7. Process movement-triggered terrain using the existing forced-movement rules. If a target dies, end its movement and omit its later attack hit. Later enemies observe previous moves and death-vacated cells in that same projection. No swaps, overlap, tunneling, recursive pushing, or teleporting past blockers are allowed; do not replay completed moves.
8. Stop remaining movement and attack damage if combat termination is confirmed. Otherwise finish the movement stage, then damage surviving captured targets. Do not recollect targets by their new location or remove a living target merely because it moved outside the initial attack mask.

### 7.3 Damage stage

Confirmed scaling is `round_half_up(attackPower * damagePercent / 100)`, with `damagePercent` resolved from the unit's current Dead Hand trait rank. Initial Lv1/Lv2/Lv3 values are 100%/100%/100%, independently configurable in the trait-growth SO. Examples: 15 at 50% is 8; 15 at 150% is 23; 10 at 25% is 3.

**Proposed integration:** obtain effective attack power through the existing attack-stat path, apply this trait's coefficient once to the attack-power damage base, then use normal attack damage settlement for existing modifiers, defense, shields, attribution, death, and follow-up effects. Do not calculate a replacement damage pipeline or apply both an ordinary hit and an extra Dead Hand hit. The implementation plan must identify the exact existing stage so rounding and modifier order are explicit.

Use the post-movement world for defense, position-dependent effects, and result locations, while retaining the captured target identities. **Confirmed Sacrifice handling:** movement applies to the original attack targets, then the existing Sacrifice damage-recipient rules run against post-movement positions. If A was targeted and B validly protects A, move A first and let B receive the redirected damage afterward. Do not move B because it is the damage recipient. If B was also in the original attack target set, its own movement is processed once under the same ordered sweep rules. Preserve existing Sacrifice eligibility, consumption, capacity, damage settlement, and attribution; do not create a second protection resolver or rerun movement when redirection occurs.

The phrase "one attack hit per captured target" describes attack delivery, not a guarantee that each target loses HP itself or that a protector can receive only one redirected hit. Existing protection rules determine those outcomes. Terminal/death behavior remains as confirmed in section 2.2.

## 8. Preview, publication, and View handoff

Preview and execution must share the same sweep calculation and occupancy sequence. A preview is speculative; final execution validates against its accepted current state. A stale preview never authorizes committing stored positions.

Public evidence must provide enough information for View to display:

- Chosen basic attack cells, the authoritative facing basis, and the resolved Left/Right side (including the default when omitted).
- Captured target identities; per-target start, path, destination, and blocked/immune outcome.
- The movement stage as one sweep group before its damage stage, with correct attack/effect attribution.
- Damage expectations and final results through the existing attack result contracts.

Reuse existing result/snapshot structures where their semantics fit. If a new sweep snapshot or event is necessary, make it immutable, typed, and carried through the existing Buffer publication adapters. Do not label pre-damage sweeping as ordinary post-damage knockback if that would misorder playback.

View may animate movements concurrently within the sweep group but applies damage presentation after the movement group completes. It must consume authoritative routes rather than simulate collisions. Interruption, cancellation, error, scene exit, and pooled-resource release remain under existing playback ownership.

Input uses established InputSystem and UI contracts. If a new independent panel is necessary, use `UIBase` and UISystem registration; child arrows within an existing registered combat UI need not become independent systems. UI and production visual assets are later integration work, not delivered by this design task.

## 9. Architecture and change independence

The proposal follows the current framework programmer guide, Framework/Systems ownership, and `2026-09-30-combat-view-design-session-handoff.md` section 4.6.

| Principle | Application |
|---|---|
| SSOT | Basic geometry is authored once; positions/health remain in existing state; Core calculates preview and execution; View never recreates packing rules |
| SRP | Geometry builds cells; Dead Hand orders and resolves its movement; damage settlement handles damage; Factory/registry assemble dependencies |
| OCP | Add the new shape and trait through existing contracts/registration; limited shared dispatch changes are allowed; no unrelated feature refactor |
| LSP | Existing attacks preserve aim validation, no-preview-mutation, result publication, and ordinary damage/knockback order |
| ISP | Expose only intent and result facts required by consumers; no general-purpose mutable world access for View |
| DIP | Keep Core rules independent of Unity types and SOs; use current pure data/spec boundaries and injected facts |
| FW-001 to FW-004 | No duplicate service registration or mutable state bypass; no new Framework service for each internal helper; normal Factory/Group registration changes remain allowed |
| FW-005 to FW-009 | Preserve input binding/Submit and UI registration/lifetime paths; use existing command ownership |
| Remaining FW contracts | Preserve existing render handles, event typing, resource lifetimes, queues, and runtime initialization; do not introduce parallel scheduling |

| Real change | Existing / tempting implementation burden | Proposed edit and understanding boundary | Necessary verification |
|---|---|---|---|
| Change Sniper's aim geometry | Hard-coded trait shape would require delivery changes too | Unit/targeting authoring; understand standard mask and Piercing straight-line compatibility | Aim mask, accepted/rejected aims, delivery continuation |
| Tune Piercing falloff | Replacing delivery would reopen trace, damage, playback, and AI internals | Existing authoring and falloff rule; no Dead Hand internals | Budget, rounding, encounter order, preview parity |
| Add Piercing friendly fire | Global target-policy change risks friendly aim selection | Explicit aim-vs-delivery eligibility boundary in existing queries/delivery | Enemy-only aim, allied hits, other targeting exceptions, AI assessment |
| Change SoulEater from 3x2 to 3x3 | Trait-owned mask would duplicate range and View logic | Basic-attack data; Dead Hand consumes resolved geometry | Geometry rotation, destination derivation, preview/execute parity |
| Change packing behavior | Embedding rules in generic damage or View would couple unrelated attacks | Focused Dead Hand calculation plus shared legality contract | Blockers, immunity, sequencing, movement-trigger integration |
| Change hand animation | Recalculating routes in View would require combat rule knowledge | View presentation consuming typed evidence | Playback stage order, cancel/release, correct side |
| Add a dependency or public result field | Ad hoc consumer construction would spread wiring | Existing Factory/registry and Buffer/result adapters | Registration, serialization/copy coverage, integration consumers |

These are expected boundaries, not measured implementation improvements. Shared geometry, command payload, result, and movement changes still require real regression coverage. Independence does not mean skipping necessary shared tests.

### 9.1 Preventing scattered trait exceptions — proposed structural improvement

The user requested an improvement to avoid accumulating hard-coded exception branches. The aim is to make each gameplay condition have one identifiable owner and shared consumers. Necessary validation and state-dependent branches remain ordinary rule code; replacing every branch with an abstraction is not a success criterion.

#### Evidence in the current code

- `Domain/CombatEffects/Rules/CombatEffectCapabilities.cs` already separates effect capabilities, and `CombatEffectEngine` dispatches to the relevant rule through `EffectRuleBinding`. Reuse that extension model.
- `Contracts/Combat/CombatDamageKind.cs` distinguishes `OrdinaryAttack`, `ExtraAttack`, `ReactionAttack`, and non-attack damage. `CommandAttackPlan.DamageKind` carries this fact; do not derive a second "is extra attack" flag from caller names or a new mutable state source.
- `EffectRuleInput` currently holds origin and state, while per-capability contexts carry evaluation facts. `IEffectKnockbackRule.EvaluateKnockback` has no attack-kind context. Adding a branch to every caller would duplicate a fact that can be passed explicitly at the relevant evaluation boundary.
- `CommonAttackDamage()` and `AttackDamageEffectContext` currently have no attack-kind parameter, and `KillChainAttackCalculator` calls `CommonAttackDamage()` again. Gating only the sweep through `CommandAttackPlan.DamageKind` is insufficient if the Dead Hand coefficient is introduced into this common damage evaluation. Carry the actual attack kind through coefficient evaluation as well, or apply the coefficient at an already kind-aware stage without duplicating the rule. Add explicit coverage for an extra attack retaining ordinary damage while neither sweeping nor inheriting/reapplying the Dead Hand percentage.
- `CombatAttackProfile` currently contains explicit trait-pair and method checks. Adding every pair of mutually exclusive displacement traits would multiply the places updated when a new displacement trait is introduced.
- `AttackDeliveryExecutor` owns shared damage/post-damage stages; `CombatAttackCalculator` already integrates pre-hit Pull. Keep stage order visible here without putting Dead Hand packing or damage eligibility inside the orchestrators.

#### Proposed minimal boundaries

| Concern | Rule owner / shared use | Avoid |
|---|---|---|
| Basic-only activation | Dead Hand effect rule receives the existing attack kind through its evaluation context and returns no sweep contribution for an ineligible attack. Query/preview/execution consume the same evaluation result | Repeated `if trait == DeadHand && kind != ...` checks across calculators, AI, and View |
| Pre-damage sweep | A focused effect contribution/evaluation identifies the needed pre-damage operation; the established attack stage invokes its calculator against the existing projection. The calculator owns packing and reuses forced movement | A generic event bus, a second combat engine, an unordered chain of arbitrary callbacks, or trait-specific packing inside the common damage loop |
| Mutual exclusion | One pure compatibility declaration maps attack-displacement traits to one exclusive group and records the specifically approved Piercing/Dead Hand pair exclusion. One validator checks the effective trait set and is reused by acquisition/equipment and profile validation | Expanding a group into repeated pairwise checks, duplicating the explicit pair in multiple callers, separate UI/authoring/runtime exclusion lists, or using a runtime service registry from Contracts |
| Relative-side default | One request-normalization rule resolves omission to facing-relative Right, and preview/execution share it | Separate defaults in View, preview, and execution |
| Damage percentage | The Dead Hand effect contributes its coefficient once at the documented basic-attack damage stage, using existing rounding and settlement | Reapplying the coefficient to Kill Chain hits or embedding the trait key in several damage calculators |

Concrete contribution/context names are builder-owned. Extend the existing capability pattern only as much as needed for the real pre-damage consumer; do not introduce a project-wide policy DSL, new framework services, generic trigger graph, or rewrite every existing trait. The compatibility declaration must remain a pure contract usable without reversing the current Contracts/Domain/Application dependencies. Game-rule membership stays in code-owned definitions; SOs configure approved numeric values, not whether mandatory mutual exclusion or basic-only activation can be bypassed.

Do not globally filter out a trait's entire binding merely because one operation is inactive for the current attack kind. Scope activation to the relevant contribution, preserve existing state snapshots and origins, and keep unrelated damage/defensive effects intact. Reuse existing normal-failure, invalid-profile, and immutable-evaluation conventions. Preserve existing behavior for other traits; any migration of old checks must demonstrate equivalent compatibility results, except where the user expressly approved the new displacement exclusivity.

#### Before/after change scope and validation

| Change scenario | Risk of scattered conditions | Intended ownership after improvement | Evidence required |
|---|---|---|---|
| Change Dead Hand's eligible attack kinds | Read/edit query, preview, damage, and automatic-attack call sites separately | Change one activation rule; consumers pass the existing attack-kind fact | Basic attack activates; extra attack does not; preview/execute agree; other effects still run |
| Add another attack-displacement trait | Add multiple pairwise checks at every entry point | Declare membership once; existing group validation rejects conflicts | Effective defaults/equipment/restored profiles produce consistent acceptance and rejection |
| Change omitted-side behavior | Update several UI/calculator defaults | Change one normalization rule | Omitted/explicit direction parity and all facing rotations |
| Change packing or animation | Require knowledge of unrelated trait exceptions | Packing remains in its calculator; View uses published routes | Existing movement legality, timing, target capture, and playback regression |

These are proposed improvements, not a claim that refactoring or tests have been performed. The implementation plan must name the actual edited functions and preserve required integration checks. Commit/push, broad cleanup of unrelated exceptions, and implementation before decision closure remain outside this phase.

## 10. Explicit limits and decisions before implementation

The confirmed SoulEater 3x2 behavior is specified above. The list below distinguishes resolved decisions from remaining questions; unresolved behavior must not be silently presented as approved:

1. **Direction compatibility — resolved:** the temporary direction-only equipment restriction was reconsidered. Dead Hand moves actual Single/Direction/All targets toward the basic mask's side boundary plus one tile. The optional side is facing-relative Left/Right, default Right. Section 5.2 records the source-based Rotation4 mapping for the implementation plan. There is no trait-specific centering, odd-width, or SoulEater-only restriction.
2. **Hazard and terminal timing — resolved:** the user accepted existing forced-movement terrain timing, stopping a dead target without a later attack hit, continuing other targets while combat continues, allowing later targets to use death-vacated cells, never replaying completed moves, and stopping remaining processing on confirmed combat termination. See sections 2.2 and 7.2.
3. **Combined attack effects — gameplay decisions resolved:** attack-displacement traits cannot be equipped together, and the Piercing/Dead Hand pair is separately prohibited (section 2.3). Dead Hand is basic-attack-only and does not reactivate on Kill Chain or other extra attacks. Proposed code mapping is `OrdinaryAttack` eligibility, with `ExtraAttack` and `ReactionAttack` not activating the sweep; verify the existing entry classifications rather than adding caller-specific exceptions. Sacrifice is evaluated after movement against post-movement positions and redirects only damage; it does not change or repeat the original-target sweep (section 7.3).
4. **Enemy-click lifetime — resolved with targeting-mode scope:** for direction-area attacks, the initially clicked enemy is only an input aid; capture current targets without requiring that enemy to remain. Single attacks retain their actual selected-target validation. An empty eligible-target set prevents execution and attack-action consumption. See sections 2.2 and 7.1.
5. **Content and delivery scope — resolved:** SoulEater's stats and Dead Hand ranks are fixed in section 5.1. Section 1.1 records the confirmed Core/SO/API scope and excludes acquisition-list additions and View implementation. Final display naming/art are deferred with View, not invented as part of Core. Unique IDs/GUIDs are builder-owned; the existing enemy Gravekeeper is preserved.

No unresolved gameplay question blocks the requested implementation plan. New source evidence that contradicts confirmed rules must be reported rather than silently changing them. Existing compatibility behavior not expressly changed by this design remains intact, including Piercing's current low-power rounded-zero stop behavior.

Builder-owned technical decisions include concrete helper names, bounded iteration/chunking, adapter placement, stable row order, and reuse of existing result types where semantics match. All deviations from confirmed gameplay require user review.

## 11. Validation and delivery boundaries

### 11.1 Required behavioral evidence for later implementation

| Area | Cases |
|---|---|
| Piercing aim | Sniper cardinal cross and authored range; reject empty/friendly aim; another compatible carrier keeps its own shape; unsupported non-aligned aim fails explicitly |
| Piercing delivery | Allies before/after the aim; enemy beyond basic range; four-character cap shared by both sides; aim beyond exhausted budget; wall/structure stop; dead/actor exclusion |
| Piercing numbers | 100/75/50/25; changed reduction with cap retained; half-up and low-power early zero behavior; shields do not alter encounter numbering |
| Rectangle | SoulEater six tiles per direction and 20-tile union; rotation; range/width edits; board edges; old RadialSquare unchanged |
| Sweep packing | One enemy reaches the side column; multiple enemies pack; both directions; different rows; vacated cells reused; no overlap/push-through |
| Blockers | Wall between attacker and victim does not remove victim; wall along movement blocks; immune enemy takes damage and blocks; ally/outside unit blocks; opposite-side enemy still moves |
| Damage | Zero-step and immune targets hit once; all movement precedes attack damage; moved targets remain captured; dead blocker does not cause repacking; 15x50%=8 and 15x150%=23 |
| Trait compatibility | Reject multiple attack-displacement traits and the explicit Piercing/Dead Hand pair through acquisition/equipment and effective-profile validation; include category/default/restored sources; preserve separate-unit use; do not silently suppress traits or reject immunity/self-movement solely because of this rule |
| Activation ownership | Basic attack activates Dead Hand; Kill Chain/extra attacks do not repeat its movement or damage coefficient; preview/execution use the same eligibility; unrelated effect contributions remain active; shared compatibility validation replaces duplicate new checks |
| Trait ranks | Lv1/Lv2/Lv3 initialize to 100% each; deliberately distinct test-owned percentages prove rank-specific resolution rather than accidentally passing with identical values; validate rounding, training/rank changes, effective specs, copying/restoring, and preview/execution using the resolved current-rank value |
| Sacrifice | Original target moves before damage-recipient selection; protection becomes available/unavailable according to post-movement positions; valid protector receives damage without an extra move; a protector also captured as an attack target moves once for its own targeting; existing capacity/consumption rules and attribution are preserved |
| Initial enemy selection | Originally clicked enemy disappears but another eligible enemy remains: execute against current area targets; no eligible enemies remain: no movement, damage, or attack-action consumption; never reuse the preview target list as authoritative |
| Targeting modes and side API | Single moves only its actual target; other units in the geometric range remain blockers; Direction/All preserve their actual target sets; omitted side equals explicit facing-relative Right in preview and execution; test all four cardinal facings, explicit Left, and invalid side rejection |
| Integration | Preview/command parity, stale preview, duplicate submit, cancel, Buffer publication, save/spec copies, movement undo invalidation, approved hazards and trait combinations |
| Presentation | Side selection and rotating labels, actual paths/stops, hand opposite movement, concurrent sweep before damage, interruption and cleanup |

Use existing Core/CoreIntegration test families as regression anchors (`PiercingShot*`, `PiercingFalloffTraitTests`, `AttackPreviewParityTests`, `AttackKnockbackSnapshotTests`, `KnockbackImmunityPolicyTests`, and relevant targeting/forced-movement tests). Tests must use owned inputs rather than mutable production SO values. Production asset changes additionally require `DataBaseAssetTests`.

Repository restrictions remain in force: no View test code is authored; combat View MSBuild is not run; user Unity compile/play checks are handed off. If Core validation is later authorized, use the approved Unity launch route that allows AppData access; the previously diagnosed CurlRequestCache sandbox issue is not a reason to delete caches or upgrade Unity.

### 11.2 Suggested phase boundaries, not an execution plan

1. Review this design and settle the implementation-relevant decisions in section 10.
2. Write a separate Task/Phase plan after design approval, respecting Core/View ownership.
3. Implement the narrow Piercing revision and report at its review point.
4. Implement basic rectangle geometry and Dead Hand Core contracts/calculation in reviewed phases.
5. Register the approved InGame content, verify Core/publication/authoring, and deliver the View handoff. View input/preview/playback implementation and user Unity visual acceptance remain outside this implementation plan.

This design does not authorize implementation, modification of other workers' areas, commit, or push. No compile, play, or test pass is claimed by the design-writing task.

### 11.3 Design review record

An independent builder review on 2026-11-14 found no additional material contradiction or unsupported current-code claim in the draft. It confirmed that section 10 accurately preserves the important pre-implementation decisions: movement-triggered death/termination, initial shape/effect compatibility, and the lifetime of the initially clicked enemy. This is a source-based design review, not approval of those open decisions or runtime verification.

Subsequent user clarification on 2026-11-14 resolved movement-triggered death/termination and clarified that sweep destinations follow the existing attack mask's boundary, without any trait-specific centering or odd-width restriction. The review paragraph above describes the earlier draft, not the remaining decision list after this clarification.

The user subsequently confirmed mutual exclusion of attack-displacement traits. Section 2.3 records that decision and section 10 retains only the other combination questions as unresolved. No implementation plan or implementation is started until the remaining decisions are settled.

The user also confirmed that the initially clicked enemy is only a direction-selection aid. Current area targets are captured at attack acceptance; an empty eligible-target set prevents execution and attack-action consumption. Sections 2.2, 7.1, 10, and the validation cases now record this decision.

The later targeting clarification supersedes the temporary direction-only equipment decision: move actual basic attack targets rather than broadening the target set. The user then proposed a Core direction API with a default and clarified that only Left/Right relative to the attacker's facing are allowed. The design records omission as facing-relative Right; a board-right default and four-way sweep input are explicitly discarded. The earlier enemy-click-aid decision applies to direction-area input, not Single attack target validation.

The user confirmed basic-attack-only activation with no reactivation on extra attacks such as Kill Chain, and requested a structural improvement to avoid scattered hard-coded exceptions. Sections 2.2, 7.2, 9.1, 10, and validation now record the confirmed trigger rule and the source-based improvement proposal. Product code has not been changed.

A follow-up independent review of section 9.1 found no material structural flaw or overgeneralization. Its specific warning about Kill Chain reusing the attack-kind-unaware common damage calculation was verified in source and added to the evidence above. This remains a reviewed design proposal, not a completed refactor.

The user subsequently prohibited equipping Piercing and Dead Hand together. This exact pair is recorded alongside the existing displacement group rule in the shared compatibility design, without broadening the group to unrelated Piercing combinations.

The user confirmed the proposed Sacrifice interaction: sweep original targets, evaluate protection against post-movement positions, redirect eligible damage through the existing rules, and never move a protector merely because it received redirected damage. Sections 2.2, 7.3, 10, and validation reflect this decision.

The user supplied SoulEater base stats: health 20, attack 6, physical defense 5, priority 30, and asked whether priority is still used. Source tracing found that current turn/action ordering does not consume the unit stat, while data contracts and Buffer exposure retain it. The design keeps 30 for compatibility and does not add a priority-removal refactor to this task.

The user confirmed independently SO-configurable damage percentages at each Dead Hand trait level, initially 100% at every level. The design maps these to the existing Normal/Rank2/Rank3 trait-growth authoring and resolution flow, with no parallel rank table or View-side rule calculation.

The user confirmed Core, SoulEater/targeting/trait-growth SOs, and View-facing APIs as the implementation scope, explicitly excluding shop/reward/level-up acquisition-list additions. Final planning inspection resolved facing through existing Rotation4 contracts and identified the equal-rank validation policy required for the confirmed defaults. The companion implementation plan is the next artifact; implementation has not started.
