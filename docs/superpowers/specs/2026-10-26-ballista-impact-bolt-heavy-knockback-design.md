# Ballista Impact Bolt and Heavy Knockback Immunity

Date: 2026-10-26
Status: Core/InGame implementation, allowed-assembly verification, and local View handoff completed on 2026-10-26. View implementation and manual acceptance remain pending. Earlier source-observation and planning sections below record their pre-implementation context.

## 1. Confirmed requirements

- Ballista remains a Cannon-category unit (`EntityKey.Unit_Ballista`). This is not a category-wide attack replacement or a change to the enemy Cannon unit.
- Replace its piercing basic trait with Impact Bolt (display name: `충격 볼트`). Its attack becomes an arcing, non-piercing shot.
- Use the existing radial targeting shape. The user confirmed using the current value **5**, not changing global numeric mappings. In the current `TargetingShape` contract, 0 is None, 4 is Fan, and 5 is RadialSquare.
- Initially select one attack cell with that cell plus its eight neighbors: a 3x3 square before board and occlusion filtering. The method's splash pattern remains editable.
- The center receives ordinary direct damage. Initially the surrounding eight cells use the existing splash damage pipeline at **70%**; the authored percentage remains editable. Defense, rounding, and other damage modifiers retain their existing behavior.
- Surviving enemies around the center are knocked in the direction from the impact center to their own cell, initially one cell and subsequently using the authored distance. The enemy occupying the center is not knocked back. The user explicitly confirmed this eight-direction rule; the Ballista's position or facing does not determine the displacement vector.
- Disable Heavy-category attack-knockback immunity without deleting its implementation.
- Keep unrelated behavior and state ownership intact. Do not commit or push.
- The user is the Core programmer. This implementation covers Core logic, InGame authoring, and Core-facing integration. View code, View assets, presentation, and visual verification belong to the View programmer and must be handed off.
- Modify and execute tests only in `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration`. Do not modify or run Framework, View, CombatViewBoundary, or other test assemblies. This explicit restriction supersedes the repository's general instruction to run `DataBaseAssetTests` after asset changes.

The user confirmed the following **initial SO configuration**, then clarified that these are editable balancing defaults, not a mandatory combination enforced by code:

| Authoring field | Value | Meaning in the current Core contract |
| --- | --- | --- |
| `Method_Type` | 5 | RadialSquare |
| `Method_ChoiceRange` | 1 | Single-cell selection; not attack distance |
| `Method_ChoiceAngle` | 0 | Unrestricted facing |
| `Method_Splash` | 3 | Radial splash: eight neighbors |
| `Method_SplashValue` | 70 | 70% surrounding damage |

Targeting shape, choice angle, splash pattern, splash percentage, and attack distance remain owned by existing SO fields. Impact Bolt adds a serialized `distanceCells` parameter, initially 1, propagated through immutable data/spec/definition and published snapshots. The distance must be positive, matching the existing knockback distance contract. Initial values 5/1/0/3/70, distance 1, and attack range 4 must not become equality checks or hidden overrides in attack code. Shape, splash, and damage balance edits affect the existing targeting/damage paths and therefore which enemies the trait can displace.

## 2. Source observations

Paths below are relative to the repository. `C` abbreviates `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator`; `V` abbreviates `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat`.

| Source | Observed current behavior |
| --- | --- |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Unit/Item/Playerable/Unit_Ballista.asset` | Cannon, range 4, attack power 13; PiercingFalloff with 30% reduction and unlimited target count; wall delivery 1. |
| `Assets/ProjectL/DataBaseSO/InGameGroup/Targeting/Item/TargetingMethod_BallistaAngle.asset` | Dedicated method key 7; EightDirectionLine, Single, no splash, Front angle. |
| `C/Contracts/Common/TargetingShape.cs`, `TargetingSplash.cs` | RadialSquare and Radial splash already exist. Radial splash geometry is supplied by `Domain/Combat/AppointShapeCalculator.cs`. |
| `C/Application/Combat/AttackKnockbackCalculator.cs` | Evaluates distance contributions, then moves surviving targets in entity-ID order after direct damage. Direction currently comes from the attacker anchor. Movement/terrain/arrival resolution already exists. |
| `C/Domain/Movement/ForcedDisplacementGeometry.cs` | Uses sign-based eight-direction steps; a zero step throws. The center must be excluded before direction resolution. |
| `C/Domain/Combat/KnockbackImmunityPolicy.cs` | One policy combines Heavy category immunity and explicit forced-movement immunity. |
| `C/Application/Combat/CommandAttackPlan.cs` | Already exposes selection, requested cells, delivery, and immutable command facts. No persistent impact-center state is needed. |
| `V/Presentation/Attack/CombatAttackPlayback.cs` | Ballista presentation requires `Attack.PiercingShot` and throws without it. A Mortar shot path already supports arcing flight to the delivery cell. |
| `Assets/Tests/EditMode/Framework/DataBaseAssetTests.cs`, `TargetingAngleAssetTests.cs` | Assert the old Ballista piercing profile and targeting data. Leave them unchanged under the user's test-scope restriction; record their outdated expectations in the handoff. |
| `Assets/Tests/EditMode/KnockbackImmunityPolicyTests.cs`, `KnockbackPathTests.cs` | Explicitly assert Heavy immunity, including preview and destination effects. These expectations must change selectively. |

These observations come from source inspection, not a new Unity run. Existing unrelated designs and their implementation status are not authority for this change.

## 3. Approach

**Selected by the user:** extend existing trait and knockback processing, using the current radial shape value 5.

Add a distinct Impact Bolt trait through the existing authoring/spec/definition/registration/snapshot path. Extend the internal knockback contribution contract only enough to distinguish attacker-origin knockback from impact-center knockback. Keep Impact Bolt's target/direction decisions in the already-required `ImpactBoltTraitRule`, using existing hostility and displacement geometry. The contribution describes the movement mode/distance; the shared executor dispatches the mode and retains responsibility for displacement, occupancy, terrain, arrival effects, and publication. Do not add a new generic movement interface or repeat this feature's eligibility rules across registry, executor, preview, and View.

Alternatives considered:

1. Hard-code Ballista's entity key in the shared attack calculator. Rejected: unit identity would become coupled to generic damage and movement implementation, and ordinary Knockback would be harder to change safely.
2. Implement a second explosion/movement engine. Rejected: it would duplicate preview, collision, terrain, and event ownership.
3. Add a trait through existing extension points and reuse forced displacement. Selected: localized feature rules with ordinary registration changes and explicit shared contracts.

Heavy's temporary switch belongs in the existing immunity policy. No new service, UI setting, global feature-flag framework, or duplicated category state is needed.

## 4. Proposed detailed behavior

The initial method configuration, SO-based balancing, and impact-centered displacement are user-confirmed. Other edge decisions below are implementation defaults carried into the plan.

### Targeting and damage

- Keep attack distance 4 and current stats as unchanged defaults. `Method_ChoiceRange = 1` selects the Single choice mode; it does not change attack distance to one cell.
- Initialize Ballista's dedicated method to RadialSquare / Single / Unrestricted / Radial splash / 70%. Remove the old Front restriction. Keep its key and GUID; do not modify shared Method 4 or renumber enums. Later valid SO edits must take effect without code changes.
- Match the existing arcing Cannon delivery policy: delivery ignores intervening walls; splash propagation retains the current wall policy. Board boundaries and blocked propagation can reduce the effective 3x3 area.
- Preserve Cannon's existing adjacent-cell exclusion and target eligibility. Empty-center targeting must continue to work through the existing Cannon contract.
- Remove PiercingFalloff from Ballista's authored base traits only. Keep the generic piercing implementation, other consumers, and historical keys available.
- Keep the existing Precision Shot enhancement behavior; verify its existing recipient/damage semantics with the new splash profile instead of redesigning that enhancement here.

### Impact Bolt displacement

- The origin is the resolved attack delivery cell, which is also the impact point used by result presentation. Do not use the attacker anchor or infer the center from the first damaged enemy. If direct delivery is blocked and there are no delivered hits, no displacement occurs; this is a normal blocked attack, not a missing-center configuration error.
- Only eligible, directly hit hostile units outside the center receive this trait's displacement. The initial Radial splash selects the eight neighbors; the trait must use delivered hit membership rather than reconstructing a hard-coded 3x3 region. Do not move friendly units, unrelated terrain victims, or structures/obstacles through this new trait. Ordinary Knockback keeps its existing structure behavior.
- This hostile-unit filter applies to displacement only. It must not remove targets from the shared delivered-hit/damage collection, change Cannon's current friendly/neutral damage eligibility, or skip the center's ordinary damage.
- Apply direct/splash damage and deaths first. Dead targets do not move. A surviving center target still receives its ordinary damage but is explicitly excluded from displacement.
- For a noncentral target at `(x, y)` and center `(cx, cy)`, the step is `(sign(x-cx), sign(y-cy))`. Repeat that direction for the configured distance through the existing stepwise movement executor; the initial distance is one grid step, including for diagonals.

Confirmed directions, viewed from above (`C` is the impact cell; each arrow is the movement direction of a target in that neighboring cell):

```text
NW   N   NE
 W   C    E
SW   S   SE
```

For example, an enemy one cell north of C moves a further cell north; an enemy northeast of C moves a further cell northeast. An enemy south of C moves south, even if that brings it closer to the firing Ballista. Rotating or relocating the Ballista does not change these directions for the same impact and target cells.

- Reuse the existing deterministic entity-ID order, occupancy checks, board limits, walls, terrain, cliff, mine, thorn, terminal-state, and arrival-effect rules. A blocked destination stops movement; it does not create new collision damage or chain-push another unit.
- Exclude any target whose occupied footprint contains the center before direction calculation. Do not expand currently supported multi-cell-unit admission. Existing supported footprint rules remain authoritative.
- Preview and execution consume the same rule and movement projection. Publish through existing attack/movement results and Buffers; do not create a second effect-state store.
- Require Single choice mode because the effect needs one impact center, following the existing center-based ThornField validation pattern. Allow every existing shape, choice angle, and splash pattern (including None) compatible with Single, without adding new restrictions to existing percentage contracts. The DB authoring boundary currently requires positive percent for non-None splash and 0 for None; Core's lower-level `TargetingMethodSpec` also accepts non-None + 0. Preserve both contracts rather than weakening DB validation to satisfy a synthetic Core test. Direction/All have no unique center and remain invalid for this trait; do not silently infer a center. A no-splash profile is valid and normally has no noncentral targets to displace.
- Reject Impact Bolt combined with ordinary Knockback or piercing in one profile rather than silently choosing conflicting origins. Other units' existing valid compositions remain unchanged. These semantic guards must not pin the initial method tuple.
- The serialized trait distance is the sole balancing source. Publish it in combat trait snapshots and the owned-character description contract so View can display the configured value without reading InGame assets. Do not add a persistent distance counter or hard-code the description to one cell.
- Read configuration through the existing initialization/definition lifecycle. Produced data and installed snapshots remain immutable if the SO is edited afterward; the next normal initialization reads the edited configuration. Live hot reload and new mutable configuration caches are outside scope.

### Heavy immunity

- Retain the Heavy-category condition behind one named policy enable flag, initially disabled. Keep the public behavior of the existing policy entry point and preserve the condition so restoring the flag restores the category rule.
- Continue honoring `isForcedMovementImmune` independently of that flag. Explicit immunity on bosses or individual units remains effective.
- Apply the disabled category rule to both ordinary attack knockback and Impact Bolt. This is not merely a Ballista exception.
- Do not change Heavy stats, ally protection, movement costs, or other forced-movement policies. Tile movement already uses its own explicit-immunity contract.

## 5. Presentation and ownership boundary

Core owns eligibility, damage, displacement, and recorded facts. View reads published results and ViewGroup assets; it must not read InGame rules or recompute knockback.

Ballista's current `kind: Ballista` presentation cannot remain active after piercing is removed. The View programmer must route this unit to the existing arcing-shot semantics, preserve its weapon/art identity, and release hit reactions at impact. Both trajectory preview and playback must use the same delivery cell. Existing projectile, animation, muzzle, and cancellation contracts must be inspected by that owner before choosing whether a presentation setting change is sufficient; an unverified enum-only asset edit is not a completed conversion. This session does not edit those files or settings.

Hand off the new trait key, published snapshot, display name, and approved semantics so the View programmer can register TraitVisual data and use the shared card display path. Do not relabel the global Knockback trait as Impact Bolt. Icon selection and visual asset changes belong to that owner.

Respect other View owners: do not modify ViewSystem, ViewGroup assets, View UI implementations, FieldCamera, or Architecture. Prepare a dedicated handoff during the Core work, including the removal of `PiercingShot`, delivery-cell use, trait identity, movement events, impact timing expectations, and excluded test updates. A Core-only phase is not end-to-end completion, and production play acceptance remains pending until the View programmer verifies consumption of the non-piercing result. Drafting this handoff does not send a message externally.

## 6. Change, understanding, and verification scope

| Scenario | Before | Proposed boundary and required verification |
| --- | --- | --- |
| Change Ballista balance values | Existing SO targeting/stats; Impact Bolt absent | After initial integration, edit only method/unit SO values, including trait distance. CoreIntegration checks changed values reaching queries, preview, execution, and snapshots. No unrelated feature implementation needs editing. |
| Change Ballista's base trait | Unit asset plus piercing assumptions in targeting tests and playback | Unit/method assets and Impact Bolt authoring/registration; add focused authoring/profile checks in CoreIntegration. Hand off trait visuals and playback; leave excluded tests unchanged. Other units' piercing assets remain unchanged. |
| Change impact-origin knockback | Shared executor assumes the attacker origin | Impact Bolt contribution plus explicit origin handling in the existing executor. Understand contribution, delivery, target membership, and movement contracts; regression-test ordinary Knockback because that executor is shared. |
| Temporarily disable Heavy immunity | Category and explicit immunity share one unconditional expression | One named policy switch. Test both flag branches at the policy boundary where feasible, disabled production behavior, explicit immunity, preview, and destination effects. |
| Change arcing presentation | Ballista-specific code requires piercing encounters | View handoff only: consume existing Core delivery/movement facts through arcing flight and the hit gate. Impact timing, preview, cancellation, and resource release are the View owner's verification scope. |

SSOT: SO owns targeting parameters and knockback distance, the trait owns impact knockback semantics, the policy owns immunity, and combat facts own positions/results. Immutable snapshots expose derived configuration without becoming a competing editable source. Do not copy these rules into View or Buffer writers.

SOLID: separate the new trait's reason to change from the generic movement mechanism (SRP); use the existing registered effect extension (OCP); preserve ordinary Knockback defaults and other profiles (LSP); keep the contribution contract limited to displacement needs (ISP); retain contract-based Core/View dependencies (DIP).

Framework: no new root services or lifecycle modules are needed. Preserve Factory/Group/Builder registration contracts, Buffer write ownership, and existing Job/Flow order. Normal trait registry changes are expected assembly work, not forbidden coupling.

## 7. Verification and review gates

After design approval, write a separate task plan with phase stops. Suggested boundaries are Core rules and focused regression; InGame authoring and CoreIntegration publication; Core regression and View handoff. View implementation and manual acceptance are external follow-up work, not phases implemented by this session. Plan phase ordering so changed production assets are not mistaken for a playable integration while the old presentation is still active.

Required checks during implementation:

- Eight outward directions, center exclusion, empty center, dead target, zero final damage, and one displacement per eligible unit.
- 70% surrounding damage using existing rounding/defense rules; no piercing or old per-target falloff.
- Alternate SO values: distance 2, attack range 6, splash percentage 40, Cross/None splash, non-radial shape, and restricted facing. Verify actual changes through SO → Data → production adapters/source → Core query/preview/execution and configured snapshots; keep the initial asset assertions separate from runtime-contract tests. Core-only synthetic non-None + 0 damage must not suppress otherwise eligible displacement, while SO authoring rejects that combination under its existing contract. Reject nonpositive distance and centerless choice modes explicitly.
- Attack distance 4, unrestricted facing (including side/rear selection), adjacent exclusion, wall delivery/propagation, board edges, occupied destinations, terrain/arrival effects, and deterministic processing. Verify that changing the attacker position does not change impact-centered knockback directions.
- Heavy now moves under ordinary Knockback and Impact Bolt; explicit immunity still prevents movement. Preview agrees with committed results. Existing tile-movement and non-Heavy behavior remain covered.
- Existing Knockback, Pull, piercing, Precision Shot, attack/result publication, and supported effect-composition regressions within the two allowed assemblies.
- Focused InGame authoring/profile checks in CoreIntegration, using existing Core-facing fixtures. Verify trait propagation and attack/movement publication without adding View code or copying the Framework test suite. Leave `DataBaseAssetTests` and other excluded tests unchanged and unexecuted.
- Unity compilation as needed to run the explicitly selected `ProjectL.Tests.Core` and `ProjectL.Tests.CoreIntegration` EditMode assemblies. Never use an unrestricted all-EditMode run for this task.

Report those two assemblies separately. Framework asset regressions, visual card display, trajectory preview, projectile flight, hit timing, and manual View acceptance remain unverified here and must be listed in the handoff. Do not claim a green whole-project baseline from the allowed test results.

Run Unity through an approved execution path with AppData access. The documented sandbox CurlRequestCache.db problem is not grounds for a Unity upgrade or cache deletion.

No runtime verification or implementation success is claimed by this document. The implementation plan is [Ballista Impact Bolt and Heavy Knockback Immunity](../plans/2026-10-26-ballista-impact-bolt-heavy-knockback-implementation-plan.md). SO editability supersedes the earlier fixed-distance and radial-only proposals; wall delivery, enemy-only displacement, and unchanged Precision Shot semantics remain the documented implementation defaults.

## 8. Locality review and corrections (2026-10-26)

The user's external `feature-change-locality-guidelines.md` was reviewed as criteria for this document/plan review, not as code-edit authorization. Only these two documents were updated. Evidence below is source inspection; implementation and execution remain pending.

| Representative change / source evidence | Gap in the earlier plan | Minimum correction and expected boundary |
| --- | --- | --- |
| Tune splash percentage: `Systems/DataBaseSystem/Data/InGame/Table/Targeting/Item/TargetingMethodData.Validate` rejects non-None + 0; `C/Contracts/Commands/TargetingMethodSpec` permits it | The plan demanded an invalid SO case succeed, which could encourage relaxing an unrelated shared validator | Preserve the DB rule; test synthetic zero-percent behavior only at the Core contract and test rejection at the SO boundary. Balancing stays within existing authored validity. |
| Propagate distance to combat and roster: `Assets/Tests/EditMode/CoreIntegration/ConfiguredMovementBattle` captures `OwnedStats` before battle entry, while a custom battle source changes only battle definitions | A custom-source test could pass while the real SO/roster path remained disconnected | Add a small pre-initialization input seam to the existing test DB and prove the production adapter/source/service path. Keep synthetic combat fixtures separately identified; do not overwrite snapshots or install final facts to claim propagation. |
| Change Impact Bolt target/direction rules: `AttackKnockbackCalculator` coordinates movement; `AttackForcedMovement` already owns collision, terrain, and normalization | The plan named reuse but left feature-specific branches free to spread inside the shared executor | Put the pure target/direction decision in the existing planned trait Rule, delegate hostility/geometry, and leave only mode dispatch in the executor. No extra layer or duplicate movement engine. |
| Preserve shared execution order: `AttackDeliveryExecutor.Execute` owns reflection suppression, direct damage, knockback, settlement, hit-health capture, and arrivals | Input/output descriptions alone did not protect the implicit order from an alternative execution path | State the order and failure behavior as a contract; verify through existing Core services and publication. Necessary Knockback/Pull/terrain regression remains in scope. |

The expected improvement is narrower feature-rule knowledge and a verifiable configuration path, not a promised reduction in total files or tests. Registration and required shared-engine regression are legitimate dependencies. The plan now includes phase-level structural checks and explicitly rejects default-value overrides, unit-ID branches, invalid-data fallbacks, duplicate stores/executors, and unrelated refactors. Their absence must still be confirmed in the implementation diff; this review cannot guarantee a future implementation follows the plan.

## 9. Implementation clarifications and closure — 2026-10-26

The implemented behavior follows the approved outward-center rule and SO-driven balancing contract above. The current execution record is in the [implementation plan](../plans/2026-10-26-ballista-impact-bolt-heavy-knockback-implementation-plan.md); the actual public facts and external work are in the [Core-to-View handoff](../handoffs/2026-10-26-ballista-impact-bolt-core-to-view-handoff.md).

Source inspection during implementation clarified three assumptions without changing the approved feature semantics:

- Method 7 was also used by DynamiteHammer. Preserving that unit's effective behavior required moving its reference to existing identical method 9 before changing method 7 for Ballista. No generic targeting logic or other method values were changed.
- The real Precision Shot recipe attempted to replace the removed PiercingFalloff base trait. The obsolete replacement was removed while retaining its added PrecisionShot effect and recipe identity/balance. This preserves the existing first-recipient defense bypass without changing the enhancement resolver.
- Public `AttackPreview` exposes original target occupancy and does not publish final displacement anchors. Existing movement events and final character facts remain the committed movement contract. Core tests verify internal preview projection; integration tests verify public preview purity/data and calculated-to-published movement without expanding assembly access or adding a new presentation payload.

Fresh Phase C verification passed Core 3,599/3,599 and CoreIntegration 135/135 with exit 0. The earlier phase-level baseline was 3,534 and 105 respectively. Framework/View suites were not executed. Current Ballista presentation still requires piercing data and must be converted by the View owner before visual/manual acceptance. This closure covers Core/InGame and the prepared local handoff only; no commit, push, or external delivery was performed.